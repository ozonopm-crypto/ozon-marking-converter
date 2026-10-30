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
        public static readonly UIntPtr OwnInputTag = new UIntPtr(0x5045494d);
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
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int ToUnicodeEx(uint key, uint scan, byte[] state, StringBuilder output, int length, uint flags, IntPtr layout);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
        public static bool Down(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
        public static bool EnglishAndCapsOff(IntPtr window)
        {
            uint pid; uint thread = GetWindowThreadProcessId(window, out pid);
            int language = (int)(GetKeyboardLayout(thread).ToInt64() & 0xffff);
            return (language & 0x3ff) == 9 && (GetKeyState(0x14) & 1) == 0;
        }
        private static IntPtr englishLayout;
        public static void InitializeScannerKeyboard()
        {
            if (englishLayout == IntPtr.Zero) englishLayout = LoadKeyboardLayout("00000409", 0);
        }
        public static char TranslateActual(KeyboardData key, IntPtr window, byte[] state, out bool usLayout)
        {
            uint pid; uint thread = GetWindowThreadProcessId(window, out pid);
            StringBuilder result = new StringBuilder(8);
            IntPtr layout = GetKeyboardLayout(thread);
            usLayout = (layout.ToInt64() & 0xffff) == 0x0409;
            if (layout == IntPtr.Zero) return '\0';
            int count = ToUnicodeEx(key.Key, key.Scan, state, result, 8, 4, layout);
            return count == 1 ? result[0] : '\0';
        }
        public static char TranslateUs(KeyboardData key, byte[] state)
        {
            StringBuilder result = new StringBuilder(8);
            int count = ToUnicodeEx(key.Key, key.Scan, state, result, 8, 4, englishLayout);
            return count == 1 ? result[0] : '\0';
        }
        public static void PrepareScannerKeyboard(IntPtr window, bool capsOn)
        {
            if (window != GetForegroundWindow()) return;
            if (englishLayout == IntPtr.Zero) englishLayout = LoadKeyboardLayout("00000409", 0);
            uint pid; uint thread = GetWindowThreadProcessId(window, out pid);
            if (englishLayout != IntPtr.Zero && (GetKeyboardLayout(thread).ToInt64() & 0xffff) != 0x0409) PostMessage(window, 0x50, IntPtr.Zero, englishLayout);
            if (capsOn)
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
            value.Data.Keyboard = new KeyboardInput { Key = key, Scan = scan, Flags = flags, Extra = OwnInputTag };
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
        public static bool ReplaceWhole(string result)
        {
            List<Input> inputs = new List<Input>();
            inputs.Add(Key(0x11, 0, 0)); inputs.Add(Key(0x41, 0, 0));
            inputs.Add(Key(0x41, 0, 2)); inputs.Add(Key(0x11, 0, 2));
            foreach (char ch in result) { inputs.Add(Key(0, ch, 4)); inputs.Add(Key(0, ch, 6)); }
            Input[] array = inputs.ToArray();
            return SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(Input))) == array.Length;
        }
    }

    internal sealed class FocusFrame
    {
        public static string UnavailableReason;
        [ThreadStatic] public static string LastTrace;
        public string Identity;
        public string Value;
        public bool CaretAtEnd;
        public bool WholeValueOnly;
        public bool HasNonEmptyRepresentation;
        public bool Conflicted;
        public string Reader;
        public string Trace;
        public string UiaSource;
        public bool HasPoint;
        public bool LegacyAtPoint;
        public bool NativeUiaRead;
        public int PointX, PointY;
        public Accessibility.IAccessible LegacyObject;
        public object LegacyChild;
        public Action<string> WriteValue;
        public static FocusFrame Alternative(FocusFrame previous, bool ownTest, int attempt)
        {
            LastTrace = "Alternative reader unavailable / Резервное чтение недоступно";
            if (previous.Conflicted) { LastTrace = "UIA source conflict / Конфликт источников UIA"; return null; }
            if (!Native.BrowserWindow(Native.GetForegroundWindow())) return null;
            try
            {
                if (attempt == 0)
                {
                    if (previous.NativeUiaRead) return ReadUia(ownTest, null);
                    FocusFrame result = NativeUia.Read(ownTest, null); LastTrace = NativeUia.LastTrace; return result;
                }
                if (attempt == 1)
                {
                    if (previous.LegacyObject != null) return ReadUia(ownTest, null);
                    FocusFrame result = LegacyFocus.Read(); LastTrace = LegacyFocus.LastTrace; return result;
                }
                if (previous.HasPoint)
                {
                    FocusFrame result = LegacyFocus.ReadAt(previous.PointX, previous.PointY); LastTrace = LegacyFocus.LastTrace; return result;
                }
                LastTrace = "No focused-field point / Нет точки проверенного поля";
            }
            catch { LastTrace = "Alternative reader error / Ошибка резервного чтения"; }
            return null;
        }
        public static FocusFrame Read(bool ownTest)
        {
            FocusFrame native = NativeUia.Read(ownTest, null);
            LastTrace = NativeUia.LastTrace;
            if (native != null) return native;
            string nativeTrace = LastTrace;
            try
            {
                FocusFrame uia = ReadUia(ownTest, null);
                LastTrace = nativeTrace + "\r\n" + LastTrace;
                if (uia != null) { uia.Trace = LastTrace; return uia; }
            }
            catch { LastTrace = nativeTrace + "\r\nUIA read error / Ошибка чтения UIA"; }
            string uiaTrace = LastTrace;
            if (Native.BrowserWindow(Native.GetForegroundWindow()))
            {
                try
                {
                    FocusFrame legacy = LegacyFocus.Read();
                    LastTrace = uiaTrace + "\r\n" + LegacyFocus.LastTrace;
                    if (legacy != null) { legacy.Trace = LastTrace; return legacy; }
                }
                catch { LastTrace = uiaTrace + "\r\nMSAA read error / Ошибка чтения MSAA"; }
            }
            return null;
        }
        public static FocusFrame ReadLike(FocusFrame previous, bool ownTest)
        {
            if (previous.NativeUiaRead)
            {
                FocusFrame native = NativeUia.Read(ownTest, previous.UiaSource); LastTrace = NativeUia.LastTrace; return native;
            }
            if (previous.LegacyObject == null) return ReadUia(ownTest, previous.UiaSource);
            FocusFrame result = previous.LegacyAtPoint ? LegacyFocus.ReadAt(previous.PointX, previous.PointY) : LegacyFocus.Read();
            LastTrace = LegacyFocus.LastTrace;
            return result;
        }
        private static FocusFrame ReadUia(bool ownTest, string preferredSource)
        {
            LastTrace = "UIA: no focused field / Нет поля в фокусе";
            UnavailableReason = "нет фокуса";
            AutomationElement element = AutomationElement.FocusedElement;
            if (element == null) return null;
            AutomationElement.AutomationElementInformation info = element.Current;
            UnavailableReason = "тип поля: " + info.ControlType.ProgrammaticName;
            bool ownDocument = ownTest && info.ProcessId == Process.GetCurrentProcess().Id && info.ControlType == ControlType.Document;
            if ((info.ControlType != ControlType.Edit && !ownDocument) || info.IsPassword || !info.IsEnabled || info.IsOffscreen || !info.HasKeyboardFocus)
            { LastTrace = "UIA: focused editable field not eligible / Поле недоступно для изменения"; return null; }
            UnavailableReason = "процесс поля не совпадает";
            if (!Native.BrowserProcess(info.ProcessId) && !(ownTest && info.ProcessId == Process.GetCurrentProcess().Id))
            { LastTrace = "UIA: field process rejected / Процесс поля отклонён"; return null; }
            string name = (info.Name ?? "").ToLowerInvariant();
            string id = (info.AutomationId ?? "").ToLowerInvariant();
            UnavailableReason = "поле адреса или поиска";
            if (name.Contains("адрес") || name.Contains("address") || name.Contains("поиск") || name.Contains("search") || id.Contains("omnibox"))
            { LastTrace = "UIA: address or search field rejected / Адресное или поисковое поле отклонено"; return null; }
            object valueObject;
            string value = null, textValue = null;
            string valueState = "unsupported / нет", textState = "unsupported / нет";
            Action<string> writer = null;
            bool writableValue = false;
            try
            {
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valueObject))
                {
                    ValuePattern pattern = (ValuePattern)valueObject;
                    if (pattern.Current.IsReadOnly) { LastTrace = "UIA ValuePattern: read-only / Только чтение"; return null; }
                    writableValue = true;
                    value = pattern.Current.Value;
                    valueState = value == null ? "null" : value.Length.ToString();
                    writer = pattern.SetValue;
                }
            }
            catch { value = null; valueState = "error / ошибка"; writer = null; }
            bool end = false;
            object textObject;
            try
            {
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out textObject))
                {
                    TextPattern pattern = (TextPattern)textObject;
                    object readOnly = pattern.DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute);
                    if (readOnly is bool && (bool)readOnly)
                    { LastTrace = "UIA TextPattern: read-only / Только чтение"; return null; }
                    if (readOnly is bool || writableValue)
                    {
                        // RU: Читаем TextPattern и при пустом ValuePattern того же поля.
                        // EN: Read TextPattern even when the same field exposes an empty ValuePattern.
                        textValue = pattern.DocumentRange.GetText(2048);
                        if (textValue != null) textValue = textValue.TrimEnd('\r', '\n');
                        textState = textValue == null ? "null" : textValue.Length.ToString();
                    }
                    else textState = "writability unknown / Запись не подтверждена";
                    try
                    {
                        TextPatternRange[] selection = pattern.GetSelection();
                        end = selection.Length == 1 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) == 0 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.End, pattern.DocumentRange, TextPatternRangeEndpoint.End) == 0;
                    }
                    catch { end = false; }
                }
            }
            catch { textValue = null; textState = "error / ошибка"; }
            LastTrace = "UIA ValuePattern: " + valueState + "; TextPattern: " + textState;
            if (value != null && value.Length > 1024 || textValue != null && textValue.Length > 1024)
            { LastTrace += "; oversized / Слишком длинное поле"; return null; }
            FieldValueSelection chosen = FieldValuePolicy.Select(value, textValue, preferredSource);
            LastTrace += "; selected / Выбран: " + (chosen == null ? "unavailable / недоступен" : chosen.Source);
            var bounds = info.BoundingRectangle;
            bool hasPoint = !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0 && !Double.IsInfinity(bounds.X) && !Double.IsInfinity(bounds.Y);
            return new FocusFrame
            {
                Value = chosen == null ? null : chosen.Value,
                Conflicted = chosen != null && chosen.Conflict,
                Identity = String.Join(".", Array.ConvertAll(element.GetRuntimeId(), n => n.ToString())),
                CaretAtEnd = end, WholeValueOnly = !end,
                Reader = "UIA/" + (chosen == null ? "unavailable" : chosen.Source), Trace = LastTrace,
                UiaSource = preferredSource ?? (chosen == null || chosen.Conflict ? null : chosen.Source),
                HasNonEmptyRepresentation = FieldValuePolicy.HasContent(value, textValue),
                WriteValue = chosen != null && chosen.Source == "ValuePattern" ? writer : null,
                HasPoint = hasPoint,
                PointX = hasPoint ? (int)(bounds.X + bounds.Width / 2) : 0,
                PointY = hasPoint ? (int)(bounds.Y + bounds.Height / 2) : 0
            };
        }
    }

    internal sealed class ScannerHook : IDisposable
    {
        private readonly Native.HookProc callback;
        private readonly Native.HookProc mouseCallback;
        private readonly OrderedKeyboardState keyboard = new OrderedKeyboardState();
        private readonly OrderedKeyboardState rawKeyboard = new OrderedKeyboardState();
        private readonly AltNumpadSeparator altSeparator = new AltNumpadSeparator();
        private IntPtr altWindow;
        private long altStarted, altSeparators;
        private bool rawPrimary;
        private long llEvents, rawEvents, decodedEvents, lastLl, healthAt;
        private string inputReason = "Ожидаю физический ввод / Waiting for hardware input";
        private readonly ScanBuffer buffer = new ScanBuffer();
        private readonly Func<bool> ownTestFocus;
        private readonly HashSet<uint> suppressed = new HashSet<uint>();
        private readonly System.Windows.Forms.Timer timer;
        private IntPtr handle;
        private IntPtr mouseHandle;
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
        private readonly bool captureScans;
        private readonly bool ownOnly;
        public event Action<string, bool> Status;
        public event Action<string, bool> Converted;
        public event Action FailedInput;
        public event Action<string> Diagnosis;
        public event Action<string> InputHealth;
        public long Generation { get { return Interlocked.Read(ref generation); } }
        public bool Enabled { get { return enabled; } set { enabled = value; Interlocked.Increment(ref generation); altSeparator.Cancel(); buffer.Reset(); } }
        private static long Now { get { return Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency; } }
        public ScannerHook(Func<bool> testFocus, bool capture = true, bool onlyOwnTest = false)
        {
            ownOnly = onlyOwnTest;
            captureScans = capture;
            Native.InitializeScannerKeyboard();
            SyncKeyboard();
            context = SynchronizationContext.Current;
            ownTestFocus = testFocus; callback = OnKey;
            handle = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
            if (handle == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            mouseCallback = OnMouse;
            mouseHandle = Native.SetWindowsHookEx(14, mouseCallback, Native.GetModuleHandle(null), 0);
            hookRenewed = Now;
            timer = new System.Windows.Forms.Timer { Interval = 35 };
            timer.Tick += Tick; timer.Start();
        }
        public void SetTiming(int gap, int idle) { buffer.GapMilliseconds = gap; buffer.IdleMilliseconds = idle; }
        public void SyncKeyboard()
        {
            if (disposed || busy || buffer.HasCandidate) return;
            foreach (int key in new int[] { 0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5, 0x5b, 0x5c }) { bool down = Native.Down(key); keyboard.Seed(key, down); rawKeyboard.Seed(key, down); }
            keyboard.SeedCaps((Native.GetKeyState(0x14) & 1) != 0);
            keyboard.SeedNumLock((Native.GetKeyState(0x90) & 1) != 0);
            rawKeyboard.SeedCaps(keyboard.CapsLock); rawKeyboard.SeedNumLock(keyboard.NumLock);
        }
        public void UseRawInput(bool available) { rawPrimary = available; altSeparator.Cancel(); buffer.Reset(); }
        public void RawKey(Native.KeyboardData key, bool up)
        {
            try { CaptureRawKey(key, up); }
            catch { altSeparator.Cancel(); buffer.Reset("raw input exception"); inputReason = "Ошибка приёма Raw Input / Raw Input error"; }
        }
        private void CaptureRawKey(Native.KeyboardData key, bool up)
        {
            rawEvents++;
            rawKeyboard.Update((int)key.Key, !up, key.Scan, (key.Flags & 1) != 0);
            bool altKey = key.Key == 0x12 || key.Key == 0xa4 || key.Key == 0xa5;
            if (rawPrimary && enabled && captureScans)
            {
                if (altKey && !up && !altSeparator.Active && !rawKeyboard.Control && !rawKeyboard.Windows && buffer.ShouldSuppressControl("GS", Now, Native.GetForegroundWindow()))
                {
                    altSeparator.Start(); altWindow = Native.GetForegroundWindow(); altStarted = Now;
                    buffer.KeepAlive(altStarted, altWindow);
                    Interlocked.Increment(ref generation); return;
                }
                if (altSeparator.Active)
                {
                    if (altWindow != Native.GetForegroundWindow())
                    { altSeparator.Cancel(); buffer.Reset("Alt separator focus changed"); return; }
                    if (Now - altStarted >= 2000 || !buffer.KeepAlive(Now, altWindow))
                    { altSeparator.Cancel(); buffer.Reset("Alt separator interrupted"); return; }
                    if (altKey && up)
                    {
                        bool gs = altSeparator.Finish();
                        if (gs && buffer.Control("GS", Now, altWindow)) { altSeparators++; inputReason = "Разделитель Alt+29 принят / Alt+29 separator received"; }
                        else { buffer.Clear("unsupported Alt separator"); inputReason = "Неизвестный разделитель Alt / Unsupported Alt separator"; }
                        Interlocked.Increment(ref generation); return;
                    }
                    if (!altKey && !altSeparator.Feed(key.Key, key.Scan, (key.Flags & 1) != 0, up))
                    { altSeparator.Cancel(); buffer.Clear("unsupported Alt separator key"); }
                    if (!up) Interlocked.Increment(ref generation);
                    return;
                }
            }
            if (up || !rawPrimary || disposed) return;
            long now = Now;
            if (now - lastLl > 500 && now - hookRenewed > 1000) RenewHook(true);
            if (!enabled || !captureScans) { inputReason = "Обработка выключена / Capture disabled"; return; }
            IntPtr foreground = Native.GetForegroundWindow();
            bool own = ownTestFocus() && Native.OwnWindow(foreground);
            if (ownOnly && !own) { buffer.Reset(); return; }
            if (cachedWindow != foreground || now - cachedAt > 500)
            { cachedWindow = foreground; cachedBrowser = Native.BrowserWindow(foreground); cachedAt = now; }
            if (!cachedBrowser && !own) { Interlocked.Increment(ref generation); buffer.Reset(); inputReason = "Фокус вне браузера / Outside browser"; return; }
            string control = key.Key == 13 ? "Enter" : key.Key == 9 ? "Tab" : key.Key == 119 ? "F8" : null;
            bool ctrl = rawKeyboard.Control, alt = rawKeyboard.Alt, shift = rawKeyboard.Shift, win = rawKeyboard.Windows;
            if (ctrl && !alt && !win && key.Key == 0xdd) control = "GS";
            if (control != null && !alt && !win && (control == "GS" || !ctrl && !shift))
            {
                if (buffer.Control(control, now, foreground)) return;
                Interlocked.Increment(ref generation); return;
            }
            if (key.Key == 0x14 || key.Key == 0x90 || key.Key == 0x91) return;
            Interlocked.Increment(ref generation);
            if (key.Key == 0x10 || key.Key == 0xa0 || key.Key == 0xa1 || key.Key == 0x11 || key.Key == 0xa2 || key.Key == 0xa3) return;
            if (busy || ctrl || alt || win) { buffer.Clear(busy ? "busy" : "modifier held: Ctrl=" + ctrl + ", Alt=" + alt + ", Win=" + win + ", key=0x" + key.Key.ToString("X")); inputReason = busy ? "Обработка предыдущего / Busy" : "Нажат модификатор / Modifier held"; return; }
            bool usLayout;
            char ch = rawKeyboard.DecodeUs((int)key.Key);
            if (ch == '\0') ch = Native.TranslateUs(key, rawKeyboard.Snapshot(true));
            char actual = Native.TranslateActual(key, foreground, rawKeyboard.Snapshot(false), out usLayout);
            if (ch == '\0' || actual == '\0') { buffer.Clear("key decode failed"); inputReason = "Клавиша не переведена / Key decode failed"; return; }
            decodedEvents++;
            buffer.Character(ch, actual, now, foreground, own, !usLayout || rawKeyboard.CapsLock);
            inputReason = buffer.HasCandidate ? "Собираю скан / Collecting scan" : "Нет начала маркировки / No marking prefix";
        }
        private void Report(string value, bool error) { Action<string, bool> callback = Status; if (!disposed && callback != null) callback(value, error); }
        private IntPtr Next(int code, IntPtr message, IntPtr data) { return Native.CallNextHookEx(handle, code, message, data); }
        private IntPtr OnMouse(int code, IntPtr message, IntPtr data)
        {
            long kind = message.ToInt64();
            if (code >= 0 && (kind == 0x201 || kind == 0x204 || kind == 0x207 || kind == 0x20a || kind == 0x20e))
            {
                Interlocked.Increment(ref generation); altSeparator.Cancel(); buffer.Reset("mouse input");
                if (context != null) context.Post(delegate { SyncKeyboard(); }, null);
            }
            return Native.CallNextHookEx(mouseHandle, code, message, data);
        }
        private IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            if (code < 0 || disposed) return Next(code, message, data);
            try
            {
                Native.KeyboardData key = (Native.KeyboardData)Marshal.PtrToStructure(data, typeof(Native.KeyboardData));
                llEvents++; lastLl = Now;
                bool up = message.ToInt64() == 0x101 || message.ToInt64() == 0x105;
                if (up || message.ToInt64() == 0x100 || message.ToInt64() == 0x104)
                    keyboard.Update((int)key.Key, !up, key.Scan, (key.Flags & 1) != 0);
                if (key.Extra == Native.OwnInputTag)
                {
                    if (key.Key == 0x14) rawKeyboard.SeedCaps(keyboard.CapsLock);
                    return Next(code, message, data); // Never recapture our own replacement.
                }
                if (up) { if (suppressed.Remove(key.Key)) return new IntPtr(1); return Next(code, message, data); }
                if (message.ToInt64() != 0x100 && message.ToInt64() != 0x104) return Next(code, message, data);
                if (suppressed.Contains(key.Key)) return new IntPtr(1);
                if (rawPrimary)
                {
                    if (!enabled || !captureScans) return Next(code, message, data);
                    string rawControl = key.Key == 13 ? "Enter" : key.Key == 9 ? "Tab" : key.Key == 119 ? "F8" : keyboard.Control && key.Key == 0xdd ? "GS" : null;
                    if (rawControl != null && !keyboard.Alt && !keyboard.Windows && (rawControl == "GS" || !keyboard.Control && !keyboard.Shift) && buffer.ShouldSuppressControl(rawControl, Now, Native.GetForegroundWindow()))
                    { suppressed.Add(key.Key); return new IntPtr(1); }
                    return Next(code, message, data);
                }
                if (!enabled || !captureScans) { Interlocked.Increment(ref generation); return Next(code, message, data); }
                long now = Now;
                IntPtr foreground = Native.GetForegroundWindow();
                bool own = ownTestFocus() && Native.OwnWindow(foreground);
                if (ownOnly && !own) { Interlocked.Increment(ref generation); buffer.Reset(); return Next(code, message, data); }
                if (cachedWindow != foreground || now - cachedAt > 500)
                {
                    cachedWindow = foreground; cachedBrowser = Native.BrowserWindow(foreground); cachedAt = now;
                }
                if (!cachedBrowser && !own) { Interlocked.Increment(ref generation); buffer.Reset(); return Next(code, message, data); }
                bool unicodePacket = key.Key == 0xe7;
                string control = key.Key == 13 || unicodePacket && key.Scan == 13 ? "Enter" : key.Key == 9 || unicodePacket && key.Scan == 9 ? "Tab" : key.Key == 119 ? "F8" : unicodePacket && key.Scan == 29 ? "GS" : null;
                bool ctrl = keyboard.Control, alt = keyboard.Alt, shift = keyboard.Shift, win = keyboard.Windows;
                if (ctrl && !alt && !win && key.Key == 0xdd) control = "GS";
                if (control != null && !alt && !win && (control == "GS" || (!ctrl && !shift)))
                {
                    if (buffer.Control(control, now, foreground)) { suppressed.Add(key.Key); return new IntPtr(1); }
                    Interlocked.Increment(ref generation);
                    return Next(code, message, data);
                }
                if (key.Key == 0x14 || key.Key == 0x90 || key.Key == 0x91) return Next(code, message, data);
                Interlocked.Increment(ref generation);
                if (key.Key == 0x10 || key.Key == 0xa0 || key.Key == 0xa1 || key.Key == 0x11 || key.Key == 0xa2 || key.Key == 0xa3) return Next(code, message, data);
                if (busy || ctrl || alt || win) { buffer.Clear(); return Next(code, message, data); }
                bool usLayout = true;
                char ch = unicodePacket ? (char)key.Scan : keyboard.DecodeUs((int)key.Key);
                if (!unicodePacket && ch == '\0') ch = Native.TranslateUs(key, keyboard.Snapshot(true));
                char actual = unicodePacket ? ch : Native.TranslateActual(key, foreground, keyboard.Snapshot(false), out usLayout);
                if (ch == '\0' || actual == '\0') buffer.Clear();
                else
                {
                    buffer.Character(ch, actual, now, foreground, own, !unicodePacket && (!usLayout || keyboard.CapsLock));
                }
            }
            catch { buffer.Reset(); }
            return Next(code, message, data);
        }
        private void Tick(object sender, EventArgs args)
        {
            if (Now - healthAt > 1000)
            {
                SyncKeyboard();
                healthAt = Now;
                Action<string> health = InputHealth;
                if (health != null) health("Физические клавиши / Hardware keys: RAW=" + rawEvents + "; LL=" + llEvents + "; переведено / decoded=" + decodedEvents + "; кадр / frame=" + buffer.Candidate.Length + "\r\n" + (rawPrimary ? "Raw Input" : "LL fallback") + ": " + inputReason + "\r\nMax=" + buffer.MaximumCandidateLength + "; reset=" + buffer.LastResetReason + "; reset length=" + buffer.LastResetLength + "; rejected at=" + buffer.LastRejectedPrefixLength + "; packets=" + buffer.EmittedPackets + "; last packet=" + buffer.LastPacketLength + "; Alt29=" + altSeparators);
            }
            if (!enabled || disposed) return;
            if (altSeparator.Active)
            {
                if (Now - altStarted < 2000) return;
                altSeparator.Cancel(); buffer.Reset("Alt separator timeout");
            }
            if (busy)
            {
                if (Now - busySince > 6000) { enabled = false; Action fail = FailedInput; if (fail != null) fail(); Report("Проверка поля не ответила вовремя. Обработка приостановлена. Перезапустите программу.", true); }
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
        public void RenewHook(bool allowDuringRaw = false)
        {
            if (disposed || busy || buffer.HasCandidate && !(allowDuringRaw && rawPrimary) || suppressed.Count > 0) return;
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
                ProcessPacket(new ScanPacket { Raw = scan.Raw, Typed = "", Window = scan.Window, AllowOwnTest = own, Generation = scan.Generation, DirectInput = true, Origin = "COM" });
            }, null);
        }
        public void TestReplacement(string value, IntPtr window)
        {
            if (!enabled || busy || disposed) return;
            ProcessPacket(new ScanPacket { Raw = value, Typed = value, Window = window, AllowOwnTest = true, Generation = Interlocked.Read(ref generation), Origin = "TEST / Искусственный пример" });
        }
        public void TestBrowserReplacement(IntPtr window)
        {
            if (!enabled || busy || disposed || !Native.BrowserWindow(window)) return;
            ProcessPacket(new ScanPacket { Raw = MarkingCode.DemoRaw, Typed = MarkingCode.DemoRaw, Window = window, Generation = Interlocked.Read(ref generation), Origin = "TEST / Искусственный пример в браузере" });
        }
        public void TestMismatch(IntPtr window)
        {
            if (!enabled || busy || disposed) return;
            string captured = MarkingCode.DemoRaw.ToLowerInvariant();
            ProcessPacket(new ScanPacket { Raw = captured, Typed = captured, Window = window, AllowOwnTest = true, Generation = Interlocked.Read(ref generation), Origin = "TEST / Искусственное несовпадение" });
        }
        private static void RecordRead(StringBuilder trace, string stage, FocusFrame frame, ScanPacket packet)
        {
            trace.AppendLine(stage + ": " + (frame == null ? FocusFrame.LastTrace : frame.Trace));
            if (frame != null)
            {
                CodeResult format = MarkingCode.Normalize(frame.Value);
                trace.AppendLine("Field validation / Проверка поля: " + format.Ok + "; " + format.Message);
                trace.AppendLine("Exact typed / Точный ввод=" + (frame.Value == packet.Typed) + "; same product frame / Кадр того же товара=" + MarkingCode.SameProductFrame(packet.Raw, frame.Value));
            }
        }
        private void PublishDiagnosis(ScanPacket packet, FocusFrame frame, ReplacementPlan plan, StringBuilder reads)
        {
            Action<string> diagnosis = Diagnosis;
            if (diagnosis != null) diagnosis(ScanDiagnosis.Describe(packet, frame == null ? null : frame.Value, frame == null ? "unavailable / недоступен" : frame.Reader, plan, reads.ToString()));
        }
        private void ProcessPacket(ScanPacket packet)
        {
            CodeResult result = MarkingCode.Normalize(packet.Raw);
            if (!result.Ok && (packet.DirectInput || !MarkingCode.LooksLikeCode(packet.Raw))) { if (MarkingCode.LooksLikeCode(packet.Raw)) Report(result.Message, true); return; }
            busy = true; busySince = Now;
            Task.Factory.StartNew(delegate
            {
                StringBuilder reads = new StringBuilder();
                try
                {
                    if (!Ready(packet)) { Report("Ввод или активное окно изменились. Код не заменён. Повторите сканирование.", true); return; }
                    FocusFrame first = FocusFrame.Read(packet.AllowOwnTest);
                    RecordRead(reads, "Initial / Первое чтение", first, packet);
                    for (int attempt = 0; first == null && attempt < 4 && Ready(packet); attempt++)
                    { Thread.Sleep(100); first = FocusFrame.Read(packet.AllowOwnTest); }
                    if (first == null)
                    {
                        RecordRead(reads, "Retry / Повтор", null, packet);
                        PublishDiagnosis(packet, null, null, reads);
                        Report("Браузер не предоставил доступ к активному полю. Подробности на вкладке «Диагностика».", true); return;
                    }
                    ReplacementPlan plan = packet.DirectInput && first.HasNonEmptyRepresentation ? null : ReplacementPlan.Resolve(first.Value, packet, first.WholeValueOnly);
                    long settleUntil = Now + 600;
                    while (plan == null && Ready(packet) && Now < settleUntil)
                    {
                        Thread.Sleep(60);
                        FocusFrame settled = FocusFrame.ReadLike(first, packet.AllowOwnTest);
                        if (!Ready(packet))
                        {
                            RecordRead(reads, "New input cancelled settling / Новый ввод отменил ожидание", settled, packet);
                            PublishDiagnosis(packet, first, null, reads);
                            Report("Во время обработки начался новый ввод или изменилось окно. Повторите один скан в пустое поле.", true); return;
                        }
                        if (settled == null || settled.Identity != first.Identity) break;
                        first = settled;
                        plan = packet.DirectInput && first.HasNonEmptyRepresentation ? null : ReplacementPlan.Resolve(first.Value, packet, first.WholeValueOnly);
                    }
                    RecordRead(reads, "Settled / После ожидания", first, packet);
                    for (int alternativeIndex = 0; plan == null && alternativeIndex < 3 && !(packet.DirectInput && first.HasNonEmptyRepresentation) && Ready(packet); alternativeIndex++)
                    {
                        FocusFrame alternative = FocusFrame.Alternative(first, packet.AllowOwnTest, alternativeIndex);
                        RecordRead(reads, "Alternative " + (alternativeIndex + 1) + " / Резерв", alternative, packet);
                        if (alternative != null)
                        {
                            ReplacementPlan alternativePlan = packet.DirectInput && alternative.HasNonEmptyRepresentation ? null : ReplacementPlan.Resolve(alternative.Value, packet, alternative.WholeValueOnly);
                            if (alternativePlan != null) { first = alternative; plan = alternativePlan; }
                        }
                    }
                    PublishDiagnosis(packet, first, plan, reads);
                    if (plan == null || (!plan.ReplaceWhole && !first.CaretAtEnd))
                    {
                        Report(packet.Origin.StartsWith("TEST", StringComparison.Ordinal) ? "Искусственный пример не совпадает с кодом товара. Для обычного сканирования кнопки теста не нужны." : "Не удалось подтвердить текст поля. Причина записана на вкладке «Диагностика». Код не изменён.", true); return;
                    }
                    FocusFrame second = FocusFrame.ReadLike(first, packet.AllowOwnTest);
                    if (second == null || second.Identity != first.Identity || second.Value != first.Value || (packet.DirectInput && second.HasNonEmptyRepresentation) || (!plan.ReplaceWhole && !second.CaretAtEnd) || !Ready(packet))
                    {
                        RecordRead(reads, "Before write / Перед записью", second, packet); PublishDiagnosis(packet, second, plan, reads);
                        Report("Во время проверки изменился ввод. Повторите сканирование.", true); return;
                    }
                    if (Native.Down(0x10) || Native.Down(0x11) || Native.Down(0x12) || Native.Down(0x5b) || Native.Down(0x5c)) { Report("Отпустите клавиши-модификаторы и повторите сканирование.", true); return; }
                    bool sent = plan.Matches(second.Value);
                    // RU: В браузере вводим через Windows: событие input обновляет состояние формы.
                    // EN: Browser replacement uses Windows input events to update controlled form state.
                    if (!sent && !Native.BrowserWindow(packet.Window) && (second.WriteValue != null || second.LegacyObject != null))
                    {
                        try
                        {
                            if (second.WriteValue != null) second.WriteValue(plan.Expected);
                            else second.LegacyObject.set_accValue(second.LegacyChild, plan.Expected);
                            sent = true;
                        }
                        catch
                        {
                            FocusFrame unchanged = FocusFrame.ReadLike(first, packet.AllowOwnTest);
                            if (unchanged == null || unchanged.Identity != first.Identity || unchanged.Value != first.Value || !Ready(packet))
                            { Report("Браузер не разрешил замену. Поле изменилось; автоматический повтор отменён.", true); return; }
                        }
                    }
                    if (!sent && !Ready(packet)) { Report("Перед записью изменился ввод. Повторите сканирование.", true); return; }
                    if (!sent) sent = plan.ReplaceWhole ? Native.ReplaceWhole(plan.Expected) : Native.ReplaceTypedSuffix(packet.Typed.Length, result.Code);
                    if (!sent)
                    {
                        enabled = false; Action fail = FailedInput; if (fail != null) fail();
                        Report("Windows не выполнила ввод полностью. Обработка остановлена. Проверьте поле вручную.", true); return;
                    }
                    int stable = 0;
                    FocusFrame lastRead = null;
                    long verifyUntil = Now + 1500;
                    while (Now < verifyUntil && Ready(packet))
                    {
                        Thread.Sleep(50);
                        FocusFrame actual = FocusFrame.ReadLike(first, packet.AllowOwnTest);
                        lastRead = actual;
                        if (actual == null || actual.Identity != first.Identity) break;
                        stable = plan.Matches(actual.Value) ? stable + 1 : 0;
                        if (stable >= 2) break;
                    }
                    if (stable < 2 || !Ready(packet))
                    {
                        RecordRead(reads, "Readback / Проверка записи", lastRead, packet); PublishDiagnosis(packet, lastRead, plan, reads);
                        Report("Замена не подтверждена: браузер не сохранил разделители или фокус изменился. Проверьте поле и повторите сканирование.", true);
                        return;
                    }
                    RecordRead(reads, "Readback / Проверка записи", lastRead, packet); PublishDiagnosis(packet, lastRead, plan, reads);
                    if (context != null) context.Post(delegate
                    {
                        if (!disposed && enabled && packet.Window == Native.GetForegroundWindow() && packet.Generation == Interlocked.Read(ref generation))
                            Native.PrepareScannerKeyboard(packet.Window, keyboard.CapsLock);
                    }, null);
                    Action<string, bool> callback = Converted; if (!disposed && callback != null) callback(plan.Expected, packet.DirectInput);
                    Report("Разделители записаны и проверены в поле. Проверьте ответ Ozon. Подтверждение — вручную.", false);
                }
                catch { Report("Не удалось завершить проверку поля. Проверьте содержимое поля вручную перед повторным сканированием.", true); }
                finally { busy = false; }
            });
        }
        private bool Ready(ScanPacket packet)
        {
            return !disposed && enabled && Now - busySince < 5500 && packet.Window == Native.GetForegroundWindow() && packet.Generation == Interlocked.Read(ref generation);
        }
        public void Dispose()
        {
            disposed = true; enabled = false;
            if (timer != null) timer.Dispose();
            if (handle != IntPtr.Zero) { Native.UnhookWindowsHookEx(handle); handle = IntPtr.Zero; }
            if (mouseHandle != IntPtr.Zero) { Native.UnhookWindowsHookEx(mouseHandle); mouseHandle = IntPtr.Zero; }
            altSeparator.Cancel(); buffer.Reset();
        }
    }
}
