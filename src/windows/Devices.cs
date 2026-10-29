using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OzonMarkingDesktop
{
    internal sealed class RawKeyboardWindow : NativeWindow, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] private struct Registration { public ushort Page, Usage; public uint Flags; public IntPtr Window; }
        [StructLayout(LayoutKind.Sequential)] private struct DeviceEntry { public IntPtr Device; public uint Type; }
        [StructLayout(LayoutKind.Sequential)] private struct Header { public uint Type, Size; public IntPtr Device, Param; }
        [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterRawInputDevices(Registration[] devices, uint count, uint size);
        [DllImport("user32.dll")] private static extern uint GetRawInputDeviceList([Out] DeviceEntry[] list, ref uint count, uint size);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, StringBuilder data, ref uint size);
        [DllImport("user32.dll")] private static extern uint GetRawInputData(IntPtr input, uint command, IntPtr data, ref uint size, uint headerSize);
        public event Action Changed;
        public string LastPath = "";
        public bool Available;
        public RawKeyboardWindow()
        {
            CreateHandle(new CreateParams { Caption = "PEI scanner device monitor", Parent = new IntPtr(-3) });
            Available = RegisterRawInputDevices(new Registration[] { new Registration { Page = 1, Usage = 6, Flags = 0x2100, Window = Handle } }, 1, (uint)Marshal.SizeOf(typeof(Registration)));
        }
        private static string PathOf(IntPtr device)
        {
            uint size = 0;
            if (GetRawInputDeviceInfo(device, 0x20000007, null, ref size) == UInt32.MaxValue || size == 0 || size > 4096) return "";
            StringBuilder text = new StringBuilder((int)size + 1);
            return GetRawInputDeviceInfo(device, 0x20000007, text, ref size) == UInt32.MaxValue ? "" : text.ToString();
        }
        public static List<ScannerDevice> Enumerate()
        {
            List<ScannerDevice> result = new List<ScannerDevice>();
            uint count = 0, size = (uint)Marshal.SizeOf(typeof(DeviceEntry));
            if (GetRawInputDeviceList(null, ref count, size) == UInt32.MaxValue || count > 512) return result;
            DeviceEntry[] entries = new DeviceEntry[count];
            uint read = GetRawInputDeviceList(entries, ref count, size);
            if (read == UInt32.MaxValue) return result;
            for (int i = 0; i < read; i++)
                if (entries[i].Type == 1)
                {
                    string path = PathOf(entries[i].Device);
                    if (path.Length != 0) result.Add(new ScannerDevice { Id = path, Name = path, Keyboard = true });
                }
            return result;
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0xfe) { Action action = Changed; if (action != null) action(); }
            if (message.Msg == 0xff)
            {
                uint size = (uint)Marshal.SizeOf(typeof(Header));
                IntPtr data = Marshal.AllocHGlobal((int)size);
                try
                {
                    if (GetRawInputData(message.LParam, 0x10000005, data, ref size, size) != UInt32.MaxValue)
                    {
                        Header header = (Header)Marshal.PtrToStructure(data, typeof(Header));
                        if (header.Type == 1 && header.Device != IntPtr.Zero) LastPath = PathOf(header.Device);
                    }
                }
                finally { Marshal.FreeHGlobal(data); }
            }
            base.WndProc(ref message);
        }
        public void Dispose()
        {
            if (Available) RegisterRawInputDevices(new Registration[] { new Registration { Page = 1, Usage = 6, Flags = 1, Window = IntPtr.Zero } }, 1, (uint)Marshal.SizeOf(typeof(Registration)));
            DestroyHandle();
        }
    }

    internal sealed class DeviceMonitor : IDisposable
    {
        private readonly RawKeyboardWindow raw;
        private readonly ScannerHook hook;
        private readonly System.Threading.Timer timer;
        private volatile bool disposed, active;
        private int working, discovering, refresh = 1;
        private long nextDiscovery;
        private volatile ScannerDevice[] devices = new ScannerDevice[0];
        private readonly object gate = new object();
        private readonly Dictionary<string, AutoSerialChannel> channels = new Dictionary<string, AutoSerialChannel>();
        private readonly Dictionary<string, int> rates;
        private string lastStatus = "";
        public event Action<ScannerDevice[]> DevicesChanged;
        public event Action<string> Connection;
        public event Action<string, int> Learned;
        public DeviceMonitor(ScannerHook scanner, Dictionary<string, int> savedRates)
        {
            hook = scanner; rates = new Dictionary<string, int>(savedRates);
            raw = new RawKeyboardWindow(); raw.Changed += delegate { Refresh(); };
            timer = new System.Threading.Timer(Tick, null, 100, 500);
        }
        public void SetActive(bool value) { active = value; }
        public void Refresh() { Interlocked.Exchange(ref refresh, 1); }
        public void ScanSucceeded()
        {
            string path = raw.LastPath;
            foreach (ScannerDevice device in devices)
                if (device.Keyboard && device.Id == path) { Emit("Последний USB/HID-ввод: " + device.Name); return; }
        }
        private void Emit(string text)
        {
            lock (gate)
            {
                if (text == lastStatus || disposed) return;
                lastStatus = text;
            }
            Action<string> action = Connection; if (action != null) action(text);
        }
        private void Discover()
        {
            if (Interlocked.CompareExchange(ref discovering, 1, 0) != 0) return;
            Interlocked.Exchange(ref refresh, 0);
            Task.Factory.StartNew(delegate
            {
                try
                {
                    List<ScannerDevice> found = RawKeyboardWindow.Enumerate();
                    Dictionary<string, ScannerDevice> ports = new Dictionary<string, ScannerDevice>(StringComparer.OrdinalIgnoreCase);
                    foreach (string name in SerialPort.GetPortNames()) ports[name] = new ScannerDevice { Port = name, Id = "", Name = "Устройство без названия" };
                    try
                    {
                        using (ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT Name, PNPDeviceID, PNPClass FROM Win32_PnPEntity WHERE PNPClass='Ports' OR PNPClass='Keyboard' OR PNPClass='HIDClass'"))
                        {
                            query.Options.Timeout = TimeSpan.FromSeconds(4);
                            using (ManagementObjectCollection rows = query.Get())
                                foreach (ManagementObject row in rows)
                                    using (row)
                                    {
                                        string name = Convert.ToString(row["Name"]), id = Convert.ToString(row["PNPDeviceID"]);
                                        foreach (ScannerDevice device in ports.Values)
                                            if (name.IndexOf("(" + device.Port + ")", StringComparison.OrdinalIgnoreCase) >= 0) { device.Id = id; device.Name = name; }
                                        foreach (ScannerDevice device in found)
                                            if (device.Id.Replace('#', '\\').IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 && id.Length > 0) device.Name = name;
                                    }
                        }
                    }
                    catch { /* USB input and named COM selection remain available without WMI. */ }
                    foreach (ScannerDevice device in ports.Values) found.Add(device);
                    devices = found.ToArray();
                    Action<ScannerDevice[]> action = DevicesChanged; if (!disposed && action != null) action(devices);
                }
                catch { Emit("Не удалось прочитать список устройств. Поиск будет повторён автоматически."); }
                finally { Interlocked.Exchange(ref nextDiscovery, DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond); Interlocked.Exchange(ref discovering, 0); }
            });
        }
        private void Tick(object state)
        {
            if (disposed || Interlocked.CompareExchange(ref working, 1, 0) != 0) return;
            try
            {
                long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                if (refresh == 1 || now - Interlocked.Read(ref nextDiscovery) > 5000) Discover();
                List<AutoSerialChannel> removed = new List<AutoSerialChannel>();
                lock (gate)
                {
                    if (disposed) return;
                    HashSet<string> live = new HashSet<string>();
                    if (active)
                        foreach (ScannerDevice device in devices)
                        {
                            if (device.Keyboard) continue;
                            string channelKey = device.Port + "|" + device.Id;
                            live.Add(channelKey);
                            if (channels.ContainsKey(channelKey)) continue;
                            string identity = DevicePolicy.RateKey(device.Id.Length > 0 ? device.Id : device.Port);
                            int speed; rates.TryGetValue(identity, out speed);
                            channels[channelKey] = new AutoSerialChannel(device, hook, speed, Emit, delegate(string id, int baud)
                            {
                                string key = DevicePolicy.RateKey(id);
                                lock (gate) { rates[key] = baud; }
                                Action<string, int> learned = Learned; if (!disposed && learned != null) learned(key, baud);
                            });
                        }
                    foreach (string key in new List<string>(channels.Keys))
                        if (!live.Contains(key)) { removed.Add(channels[key]); channels.Remove(key); }
                }
                foreach (AutoSerialChannel channel in removed) channel.Dispose();
                if (!active) Emit("Обработка на паузе. Устройства отслеживаются.");
                else if (channels.Count == 0) Emit("USB/HID готов. COM-устройств пока нет. Поиск выполняется автоматически.");
            }
            finally { Interlocked.Exchange(ref working, 0); }
        }
        public void Dispose()
        {
            disposed = true; timer.Dispose(); raw.Dispose();
            AutoSerialChannel[] closing;
            lock (gate) { closing = new List<AutoSerialChannel>(channels.Values).ToArray(); channels.Clear(); }
            Task.Factory.StartNew(delegate { foreach (AutoSerialChannel channel in closing) channel.Dispose(); });
        }
    }
}
