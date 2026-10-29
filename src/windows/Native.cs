using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace OzonMarkingDesktop
{
    internal static class Native
    {
        public delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        [StructLayout(LayoutKind.Sequential)] public struct KeyboardData { public uint Key, Scan, Flags, Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KeyboardInput Keyboard; [FieldOffset(0)] public MouseInput Mouse; }
        [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int hook, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern short GetKeyState(int key);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadKeyboardLayout(string layout, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr param, IntPtr value);
        [DllImport("user32.dll")] private static extern bool GetKeyboardState(byte[] state);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int ToUnicodeEx(uint key, uint scan, byte[] state, StringBuilder output, int length, uint flags, IntPtr layout);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
        public static bool Down(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
        public static bool EnglishAndCapsOff(IntPtr window)
        {
            uint pid; uint thread = GetWindowThreadProcessId(window, out pid);
            int language = (int)(GetKeyboardLayout(thread).ToInt64() & 0xffff);
            return (language & 0x3ff) == 9 && (GetKeyState(0x14) & 1) == 0;
        }
        public static char Translate(KeyboardData key, IntPtr window)
        {
            return TranslateWithLayout(key, window, false);
        }
        private static IntPtr englishLayout;
        public static char TranslateScanner(KeyboardData key, IntPtr window)
        {
            return TranslateWithLayout(key, window, true);
        }
        private static char TranslateWithLayout(KeyboardData key, IntPtr window, bool scanner)
        {
            uint pid; uint thread = GetWindowThreadProcessId(window, out pid);
            byte[] state = new byte[256];
            if (!GetKeyboardState(state)) return '\0';
            state[0x10] = (byte)(Down(0x10) ? 0x80 : 0);
            state[0x11] = (byte)(Down(0x11) ? 0x80 : 0);
            state[0x12] = (byte)(Down(0x12) ? 0x80 : 0);
            state[0x14] = scanner ? (byte)0 : (byte)(GetKeyState(0x14) & 1);
            if (key.Key < 256) state[key.Key] |= 0x80;
            StringBuilder result = new StringBuilder(8);
            if (scanner && englishLayout == IntPtr.Zero) englishLayout = LoadKeyboardLayout("00000409", 0);
            IntPtr layout = scanner ? englishLayout : GetKeyboardLayout(thread);
            if (layout == IntPtr.Zero) return '\0';
            int count = ToUnicodeEx(key.Key, key.Scan, state, result, 8, 4, layout);
            return count == 1 ? result[0] : '\0';
        }
        public static void PrepareScannerKeyboard(IntPtr window)
        {
            if (window != GetForegroundWindow()) return;
            if (englishLayout == IntPtr.Zero) englishLayout = LoadKeyboardLayout("00000409", 0);
            if (englishLayout != IntPtr.Zero) PostMessage(window, 0x50, IntPtr.Zero, englishLayout);
            if ((GetKeyState(0x14) & 1) != 0)
            {
                Input[] keys = { Key(0x14, 0, 0), Key(0x14, 0, 2) };
                SendInput(2, keys, Marshal.SizeOf(typeof(Input)));
            }
        }
        public static bool BrowserProcess(int pid)
        {
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    string name = process.ProcessName.ToLowerInvariant();
                    return name == "browser" || name == "chrome" || name == "msedge" || name == "opera";
                }
            }
            catch { return false; }
        }
        public static bool BrowserWindow(IntPtr window)
        {
            uint pid; GetWindowThreadProcessId(window, out pid); return BrowserProcess((int)pid);
        }
        public static bool OwnWindow(IntPtr window)
        {
            uint pid; GetWindowThreadProcessId(window, out pid); return pid == (uint)Process.GetCurrentProcess().Id;
        }
        private static Input Key(ushort key, ushort scan, uint flags)
        {
            Input value = new Input(); value.Type = 1;
            value.Data.Keyboard = new KeyboardInput { Key = key, Scan = scan, Flags = flags, Extra = UIntPtr.Zero };
            return value;
        }
        public static bool ReplaceTypedSuffix(int count, string result)
        {
            List<Input> inputs = new List<Input>();
            for (int i = 0; i < count; i++) { inputs.Add(Key(8, 0, 0)); inputs.Add(Key(8, 0, 2)); }
            foreach (char ch in result) { inputs.Add(Key(0, ch, 4)); inputs.Add(Key(0, ch, 6)); }
            Input[] array = inputs.ToArray();
            return SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(Input))) == array.Length;
        }
    }

    internal sealed class FocusFrame
    {
        public static string UnavailableReason;
        public string Identity;
        public string Value;
        public bool CaretAtEnd;
        public static FocusFrame Read(bool ownTest)
        {
            UnavailableReason = "нет фокуса";
            AutomationElement element = AutomationElement.FocusedElement;
            if (element == null) return null;
            AutomationElement.AutomationElementInformation info = element.Current;
            UnavailableReason = "тип поля: " + info.ControlType.ProgrammaticName;
            bool ownDocument = ownTest && info.ProcessId == Process.GetCurrentProcess().Id && info.ControlType == ControlType.Document;
            if ((info.ControlType != ControlType.Edit && !ownDocument) || info.IsPassword || !info.IsEnabled) return null;
            UnavailableReason = "процесс поля не совпадает";
            if (!Native.BrowserProcess(info.ProcessId) && !(ownTest && info.ProcessId == Process.GetCurrentProcess().Id)) return null;
            string name = (info.Name ?? "").ToLowerInvariant();
            string id = (info.AutomationId ?? "").ToLowerInvariant();
            UnavailableReason = "поле адреса или поиска";
            if (name.Contains("адрес") || name.Contains("address") || name.Contains("поиск") || name.Contains("search") || id.Contains("omnibox")) return null;
            object valueObject;
            string value = null;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valueObject))
            {
                ValuePattern pattern = (ValuePattern)valueObject;
                UnavailableReason = "поле только для чтения";
                if (pattern.Current.IsReadOnly) return null;
                value = pattern.Current.Value;
            }
            bool end = true;
            object textObject;
            if (element.TryGetCurrentPattern(TextPattern.Pattern, out textObject))
            {
                TextPattern pattern = (TextPattern)textObject;
                if (value == null)
                {
                    object readOnly = pattern.DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute);
                    if (!(readOnly is bool) || (bool)readOnly) return null;
                    value = pattern.DocumentRange.GetText(2048);
                    // Some UIA providers add a document terminator not present in an edit value.
                    value = value.TrimEnd('\r', '\n');
                }
                TextPatternRange[] selection = pattern.GetSelection();
                if (selection.Length != 1) end = false;
                else end = selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) == 0 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.End, pattern.DocumentRange, TextPatternRangeEndpoint.End) == 0;
            }
            UnavailableReason = "не предоставлены ValuePattern и TextPattern";
            if (value == null) return null;
            return new FocusFrame { Value = value, Identity = String.Join(".", Array.ConvertAll(element.GetRuntimeId(), n => n.ToString())), CaretAtEnd = end };
        }
    }

    internal sealed class ScannerHook : IDisposable
    {
        private readonly Native.HookProc callback;
        private readonly ScanBuffer buffer = new ScanBuffer();
        private readonly Func<bool> ownTestFocus;
        private readonly HashSet<uint> suppressed = new HashSet<uint>();
        private readonly System.Windows.Forms.Timer timer;
        private IntPtr handle;
        private IntPtr cachedWindow;
        private bool cachedBrowser;
        private long cachedAt;
        private long generation;
        private volatile bool enabled;
        private volatile bool busy;
        private volatile bool disposed;
        private long busySince;
        private long hookRenewed;
        private readonly SynchronizationContext context;
        public event Action<string, bool> Status;
        public event Action<string, bool> Converted;
        public event Action FailedInput;
        public long Generation { get { return Interlocked.Read(ref generation); } }
        public bool Enabled { get { return enabled; } set { enabled = value; Interlocked.Increment(ref generation); buffer.Reset(); } }
        private static long Now { get { return Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency; } }
        public ScannerHook(Func<bool> testFocus)
        {
            context = SynchronizationContext.Current;
            ownTestFocus = testFocus; callback = OnKey;
            handle = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
            if (handle == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            hookRenewed = Now;
            timer = new System.Windows.Forms.Timer { Interval = 35 };
            timer.Tick += Tick; timer.Start();
        }
        public void SetTiming(int gap, int idle) { buffer.GapMilliseconds = gap; buffer.IdleMilliseconds = idle; }
        private void Report(string value, bool error) { Action<string, bool> callback = Status; if (!disposed && callback != null) callback(value, error); }
        private IntPtr Next(int code, IntPtr message, IntPtr data) { return Native.CallNextHookEx(handle, code, message, data); }
        private IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            if (code < 0 || disposed) return Next(code, message, data);
            try
            {
                Native.KeyboardData key = (Native.KeyboardData)Marshal.PtrToStructure(data, typeof(Native.KeyboardData));
                if ((key.Flags & 0x12) != 0) return Next(code, message, data); // Never recapture simulated input.
                bool up = message.ToInt64() == 0x101 || message.ToInt64() == 0x105;
                if (up) { if (suppressed.Remove(key.Key)) return new IntPtr(1); return Next(code, message, data); }
                if (message.ToInt64() != 0x100 && message.ToInt64() != 0x104) return Next(code, message, data);
                if (suppressed.Contains(key.Key)) return new IntPtr(1);
                Interlocked.Increment(ref generation);
                if (!enabled) return Next(code, message, data);
                long now = Now;
                IntPtr foreground = Native.GetForegroundWindow();
                bool own = ownTestFocus() && Native.OwnWindow(foreground);
                if (cachedWindow != foreground || now - cachedAt > 500)
                {
                    cachedWindow = foreground; cachedBrowser = Native.BrowserWindow(foreground); cachedAt = now;
                }
                if (!cachedBrowser && !own) { buffer.Reset(); return Next(code, message, data); }
                string control = key.Key == 13 ? "Enter" : key.Key == 9 ? "Tab" : key.Key == 119 ? "F8" : null;
                bool ctrl = Native.Down(0x11), alt = Native.Down(0x12), shift = Native.Down(0x10), win = Native.Down(0x5b) || Native.Down(0x5c);
                if (ctrl && !alt && !win && key.Key == 0xdd) control = "GS";
                if (control != null && !alt && !win && (control == "GS" || (!ctrl && !shift)))
                {
                    if (buffer.Control(control, now, foreground)) { suppressed.Add(key.Key); return new IntPtr(1); }
                    return Next(code, message, data);
                }
                if (key.Key == 0x10 || key.Key == 0xa0 || key.Key == 0xa1 || key.Key == 0x11 || key.Key == 0xa2 || key.Key == 0xa3) return Next(code, message, data);
                if (busy || ctrl || alt || win) { buffer.Clear(); return Next(code, message, data); }
                char ch = Native.TranslateScanner(key, foreground), actual = Native.Translate(key, foreground);
                if (ch == '\0' || actual == '\0') buffer.Clear();
                else
                {
                    buffer.Character(ch, actual, now, foreground, own);
                    if (MarkingCode.Clean(buffer.Candidate).Length == 18 && MarkingCode.LooksLikeCode(buffer.Candidate))
                        Native.PrepareScannerKeyboard(foreground);
                }
            }
            catch { buffer.Reset(); }
            return Next(code, message, data);
        }
        private void Tick(object sender, EventArgs args)
        {
            if (!enabled || disposed) return;
            if (busy)
            {
                if (Now - busySince > 3000) { enabled = false; Action fail = FailedInput; if (fail != null) fail(); Report("Проверка поля не ответила вовремя. Обработка приостановлена. Перезапустите программу.", true); }
                return;
            }
            ScanPacket packet = buffer.Poll(Now, Interlocked.Read(ref generation));
            if (packet == null)
            {
                // Windows may silently remove a timed-out hook. Renew only between scans.
                if (!buffer.HasCandidate && suppressed.Count == 0 && Now - hookRenewed > 60000) RenewHook();
                return;
            }
            ProcessPacket(packet);
        }
        public void RenewHook()
        {
            if (disposed || busy || buffer.HasCandidate || suppressed.Count > 0) return;
            IntPtr replacement = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
            if (replacement == IntPtr.Zero) { Report("Не удалось восстановить обработчик USB/HID. Повторяю автоматически.", true); hookRenewed = Now - 50000; return; }
            IntPtr old = handle; handle = replacement;
            if (old != IntPtr.Zero) Native.UnhookWindowsHookEx(old);
            hookRenewed = Now;
        }
        public void SubmitSerial(SerialScan scan)
        {
            if (disposed || !enabled || context == null) return;
            context.Post(delegate
            {
                if (disposed || !enabled) return;
                if (busy) { Report("Предыдущий код ещё обрабатывается. Повторите следующий скан.", true); return; }
                bool own = Native.OwnWindow(scan.Window) && ownTestFocus();
                if (!own && !Native.BrowserWindow(scan.Window)) { Report("COM-код получен. Сначала выберите пустое поле маркировки в браузере.", true); return; }
                if (scan.Window == Native.GetForegroundWindow()) Native.PrepareScannerKeyboard(scan.Window);
                ProcessPacket(new ScanPacket { Raw = scan.Raw, Typed = "", Window = scan.Window, AllowOwnTest = own, Generation = scan.Generation, DirectInput = true });
            }, null);
        }
        public void TestReplacement(string value, IntPtr window)
        {
            if (!enabled || busy || disposed) return;
            ProcessPacket(new ScanPacket { Raw = value, Typed = value, Window = window, AllowOwnTest = true, Generation = Interlocked.Read(ref generation) });
        }
        private void ProcessPacket(ScanPacket packet)
        {
            CodeResult result = MarkingCode.Normalize(packet.Raw);
            if (!result.Ok) { if (MarkingCode.LooksLikeCode(packet.Raw)) Report(result.Message, true); return; }
            if (!result.Changed && !packet.DirectInput) { Report("Код уже содержит нужные разделители. Проверьте ответ Ozon.", false); return; }
            busy = true; busySince = Now;
            Task.Factory.StartNew(delegate
            {
                try
                {
                    if (!Ready(packet)) { Report("Ввод или активное окно изменились. Код не заменён. Повторите сканирование.", true); return; }
                    FocusFrame first = FocusFrame.Read(packet.AllowOwnTest);
                    if (first == null) { Report("Текстовое поле недоступно для проверки (" + FocusFrame.UnavailableReason + "). Код не изменён.", true); return; }
                    if (!first.CaretAtEnd || !ScanBuffer.CanReplace(first.Value, packet)) { Report("Поле или положение курсора изменилось. Код не заменён. Очистите поле и повторите сканирование.", true); return; }
                    FocusFrame second = FocusFrame.Read(packet.AllowOwnTest);
                    if (second == null || second.Identity != first.Identity || second.Value != first.Value || !second.CaretAtEnd || !Ready(packet)) { Report("Во время проверки изменился ввод. Повторите сканирование.", true); return; }
                    if (Native.Down(0x10) || Native.Down(0x11) || Native.Down(0x12) || Native.Down(0x5b) || Native.Down(0x5c)) { Report("Отпустите клавиши-модификаторы и повторите сканирование.", true); return; }
                    if (!Native.ReplaceTypedSuffix(packet.Typed.Length, result.Code))
                    {
                        enabled = false; Action fail = FailedInput; if (fail != null) fail();
                        Report("Windows не выполнила ввод полностью. Обработка остановлена. Проверьте поле вручную.", true); return;
                    }
                    Action<string, bool> callback = Converted; if (!disposed && callback != null) callback(result.Code, packet.DirectInput);
                    Report("Разделители подготовлены. Проверьте ответ Ozon. Подтверждение выполняется вручную.", false);
                }
                catch { Report("Не удалось проверить активное поле. Код не изменён. Повторите сканирование.", true); }
                finally { busy = false; }
            });
        }
        private bool Ready(ScanPacket packet)
        {
            return !disposed && enabled && Now - busySince < 2500 && packet.Window == Native.GetForegroundWindow() && packet.Generation == Interlocked.Read(ref generation);
        }
        public void Dispose()
        {
            disposed = true; enabled = false;
            if (timer != null) timer.Dispose();
            if (handle != IntPtr.Zero) { Native.UnhookWindowsHookEx(handle); handle = IntPtr.Zero; }
            buffer.Reset();
        }
    }
}
