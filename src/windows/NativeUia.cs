using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OzonMarkingDesktop
{
    // RU: Нативный клиент COM не использует управляемые прокси System.Windows.Automation.
    // EN: This COM client bypasses the managed System.Windows.Automation client wrappers.
    // ABI: Microsoft Windows SDK UIAutomationClient.h (microsoft/win32metadata).
    internal static class NativeUia
    {
        [ThreadStatic] private static IAutomation client;
        [ThreadStatic] public static string LastTrace;
        private static readonly Guid ClientClass = new Guid("e22ad333-b25f-460c-83d0-0581107395c9");

        [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }

        // RU: Объявлены точные слоты до последнего используемого метода, без вызова пропусков.
        // EN: Keep SDK vtable order through the last method used; placeholder slots are never called.
        [ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAutomation
        {
            void CompareElements(IElement first, IElement second, [MarshalAs(UnmanagedType.Bool)] out bool same);
            void CompareRuntimeIds([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[] first, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[] second, [MarshalAs(UnmanagedType.Bool)] out bool same);
            void GetRootElement(out IElement element);
            void ElementFromHandle(IntPtr window, out IElement element);
            void ElementFromPoint(Point point, out IElement element);
            void GetFocusedElement(out IElement element);
        }

        [ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IElement
        {
            void SetFocus();
            void GetRuntimeId([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[] runtimeId);
            void FindFirst(int scope, IntPtr condition, out IElement found);
            void FindAll(int scope, IntPtr condition, out IntPtr found);
            void FindFirstBuildCache(int scope, IntPtr condition, IntPtr cache, out IElement found);
            void FindAllBuildCache(int scope, IntPtr condition, IntPtr cache, out IntPtr found);
            void BuildUpdatedCache(IntPtr cache, out IElement updated);
            void GetCurrentPropertyValue(int property, [MarshalAs(UnmanagedType.Struct)] out object value);
            void GetCurrentPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault, [MarshalAs(UnmanagedType.Struct)] out object value);
            void GetCachedPropertyValue(int property, [MarshalAs(UnmanagedType.Struct)] out object value);
            void GetCachedPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault, [MarshalAs(UnmanagedType.Struct)] out object value);
            void GetCurrentPatternAs(int pattern, ref Guid iid, out IntPtr result);
            void GetCachedPatternAs(int pattern, ref Guid iid, out IntPtr result);
            void GetCurrentPattern(int pattern, [MarshalAs(UnmanagedType.IUnknown)] out object result);
        }

        [ComImport, Guid("a94cd8b1-0844-4cd6-9d2d-640537ab39e9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IValue
        {
            void SetValue([MarshalAs(UnmanagedType.BStr)] string value);
            void GetCurrentValue([MarshalAs(UnmanagedType.BStr)] out string value);
            void GetCurrentIsReadOnly([MarshalAs(UnmanagedType.Bool)] out bool readOnly);
        }

        [ComImport, Guid("32eba289-3583-42c9-9c59-3b6d9a1e9b6a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IText
        {
            void RangeFromPoint(Point point, out IRange range);
            void RangeFromChild(IElement child, out IRange range);
            void GetSelection(out IntPtr ranges);
            void GetVisibleRanges(out IntPtr ranges);
            void GetDocumentRange(out IRange range);
        }

        [ComImport, Guid("a543cc6a-f4ae-494b-8239-c814481187a8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IRange
        {
            void Clone(out IRange range);
            void Compare(IRange range, [MarshalAs(UnmanagedType.Bool)] out bool same);
            void CompareEndpoints(int sourceEndpoint, IRange range, int targetEndpoint, out int comparison);
            void ExpandToEnclosingUnit(int unit);
            void FindAttribute(int attribute, [MarshalAs(UnmanagedType.Struct)] object value, [MarshalAs(UnmanagedType.Bool)] bool backward, out IRange range);
            void FindText([MarshalAs(UnmanagedType.BStr)] string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase, out IRange range);
            void GetAttributeValue(int attribute, [MarshalAs(UnmanagedType.Struct)] out object value);
            void GetBoundingRectangles([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_R8)] out double[] rectangles);
            void GetEnclosingElement(out IElement element);
            void GetText(int maximum, [MarshalAs(UnmanagedType.BStr)] out string text);
        }

        private static object Property(IElement element, int id)
        {
            object value;
            element.GetCurrentPropertyValueEx(id, true, out value);
            return value;
        }
        private static bool Is(IElement element, int id, bool expected)
        {
            object value = Property(element, id);
            return value is bool && (bool)value == expected;
        }
        private static int Integer(IElement element, int id)
        {
            object value = Property(element, id);
            return value is int ? (int)value : 0;
        }
        private static string StringProperty(IElement element, int id)
        {
            try { return Property(element, id) as string ?? ""; } catch { return ""; }
        }
        private static string ProviderCategory(string description)
        {
            string lower = description.ToLowerInvariant();
            string category = lower.Contains("chrome") || lower.Contains("chromium") ? "Chromium" : "other / другой";
            if (lower.Contains("msaa")) category += "+MSAA";
            if (lower.Contains("proxy")) category += "+proxy";
            if (lower.Contains("hwnd")) category += "+HWND";
            return description.Length == 0 ? "unavailable / недоступен" : category;
        }
        private static string ClassCategory(string name)
        {
            foreach (string known in new string[] { "Chrome_WidgetWin_1", "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window", "Edit", "RichEdit20W", "RICHEDIT50W" })
                if (String.Equals(name, known, StringComparison.OrdinalIgnoreCase)) return known;
            return name.Length == 0 ? "empty / пусто" : "other / другой";
        }
        private static string FrameworkCategory(string name)
        {
            foreach (string known in new string[] { "Chrome", "Win32", "WinForm", "WPF", "XAML" })
                if (String.Equals(name, known, StringComparison.OrdinalIgnoreCase)) return known;
            return name.Length == 0 ? "empty / пусто" : "other / другой";
        }
        private static string Length(string value) { return value == null ? "unavailable / недоступен" : value.Length.ToString(); }
        private static FocusFrame Fail(string reason) { LastTrace += "; " + reason; return null; }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static bool PointFor(IElement element, out int x, out int y)
        {
            x = y = 0;
            try
            {
                Array bounds = Property(element, 30001) as Array;
                if (bounds == null || bounds.Rank != 1 || bounds.Length != 4) return false;
                int start = bounds.GetLowerBound(0);
                double left = Convert.ToDouble(bounds.GetValue(start)), top = Convert.ToDouble(bounds.GetValue(start + 1));
                double width = Convert.ToDouble(bounds.GetValue(start + 2)), height = Convert.ToDouble(bounds.GetValue(start + 3));
                double centerX = left + width / 2, centerY = top + height / 2;
                if (!Finite(left) || !Finite(top) || !Finite(width) || !Finite(height) || width <= 0 || height <= 0 ||
                    !Finite(centerX) || !Finite(centerY) || centerX < Int32.MinValue || centerX > Int32.MaxValue || centerY < Int32.MinValue || centerY > Int32.MaxValue) return false;
                x = (int)centerX; y = (int)centerY; return true;
            }
            catch { return false; }
        }

        public static FocusFrame Read(bool ownTest, string preferredSource)
        {
            LastTrace = "Native COM UIA / Нативный COM UIA";
            try
            {
                IntPtr window = Native.GetForegroundWindow();
                if (!Native.BrowserWindow(window) && !(ownTest && Native.OwnWindow(window))) return Fail("foreground rejected / Окно отклонено");
                if (client == null) client = (IAutomation)Activator.CreateInstance(Type.GetTypeFromCLSID(ClientClass, true));
                IElement element;
                client.GetFocusedElement(out element);
                if (element == null) return Fail("no focused element / Нет элемента в фокусе");
                string provider = ProviderCategory(StringProperty(element, 30107));
                string className = ClassCategory(StringProperty(element, 30012));
                string framework = FrameworkCategory(StringProperty(element, 30024));
                LastTrace += "; provider / поставщик=" + provider + "; class / класс=" + className + "; framework / среда=" + framework;
                int process = Integer(element, 30002), control = Integer(element, 30003);
                bool own = ownTest && process == Process.GetCurrentProcess().Id;
                if (control != 50004 && !(own && control == 50030)) return Fail("not an edit / Не текстовое поле");
                if (!Native.BrowserProcess(process) && !own) return Fail("field process rejected / Процесс поля отклонён");
                if (!Is(element, 30008, true) || !Is(element, 30010, true) || !Is(element, 30019, false) || !Is(element, 30022, false))
                    return Fail("field state rejected / Состояние поля отклонено");
                string name = StringProperty(element, 30005).ToLowerInvariant(), id = StringProperty(element, 30011).ToLowerInvariant();
                if (name.Contains("адрес") || name.Contains("address") || name.Contains("поиск") || name.Contains("search") || id.Contains("omnibox"))
                    return Fail("address/search rejected / Адресное или поисковое поле отклонено");
                string value = null, text = null;
                string valueStatus = "unavailable / недоступен", textStatus = "unavailable / недоступен";
                IValue writer = null;
                bool writableValue = false;
                try
                {
                    object pattern;
                    element.GetCurrentPattern(10002, out pattern);
                    IValue valuePattern = pattern as IValue;
                    if (valuePattern != null)
                    {
                        bool readOnly;
                        valuePattern.GetCurrentIsReadOnly(out readOnly);
                        if (readOnly) return Fail("ValuePattern read-only / Только чтение");
                        writableValue = true;
                        valuePattern.GetCurrentValue(out value);
                        valueStatus = Length(value); writer = valuePattern;
                    }
                }
                catch { valueStatus = "error / ошибка"; value = null; writer = null; }
                try
                {
                    object pattern;
                    element.GetCurrentPattern(10014, out pattern);
                    IText textPattern = pattern as IText;
                    if (textPattern != null)
                    {
                        IRange range; textPattern.GetDocumentRange(out range);
                        object readOnly; range.GetAttributeValue(40015, out readOnly);
                        if (readOnly is bool && (bool)readOnly) return Fail("TextPattern read-only / Только чтение");
                        if (readOnly is bool || writableValue)
                        {
                            range.GetText(1025, out text);
                            if (text != null) text = text.TrimEnd('\r', '\n');
                            textStatus = Length(text);
                        }
                        else textStatus = "writability unknown / Запись не подтверждена";
                    }
                }
                catch { textStatus = "error / ошибка"; text = null; }
                LastTrace += "; ValuePattern=" + valueStatus + "; TextPattern=" + textStatus + "; embedded placeholder / Встроенный маркер=" + (text == "\ufffc");
                if (value != null && value.Length > 1024 || text != null && text.Length > 1024) return Fail("oversized / Слишком длинное поле");
                FieldValueSelection chosen = FieldValuePolicy.Select(value, text, preferredSource);
                LastTrace += "; selected / Выбран=" + (chosen == null ? "unavailable / недоступен" : chosen.Source);
                int[] runtime;
                element.GetRuntimeId(out runtime);
                if (runtime == null || runtime.Length == 0) return Fail("identity unavailable / Нет идентификатора");
                if (window != Native.GetForegroundWindow()) return Fail("foreground changed / Окно изменилось");
                int pointX, pointY;
                bool hasPoint = PointFor(element, out pointX, out pointY);
                IValue capturedWriter = writer;
                return new FocusFrame
                {
                    NativeUiaRead = true,
                    Identity = "NativeUIA:" + String.Join(".", Array.ConvertAll(runtime, n => n.ToString())),
                    Value = chosen == null ? null : chosen.Value,
                    Conflicted = chosen != null && chosen.Conflict,
                    UiaSource = preferredSource ?? (chosen == null || chosen.Conflict ? null : chosen.Source),
                    Reader = "Native COM UIA/" + (chosen == null ? "unavailable" : chosen.Source), Trace = LastTrace,
                    HasNonEmptyRepresentation = FieldValuePolicy.HasContent(value, text),
                    CaretAtEnd = true, WholeValueOnly = true,
                    WriteValue = capturedWriter != null && chosen != null && chosen.Source == "ValuePattern" ? (Action<string>)(v => capturedWriter.SetValue(v)) : null,
                    HasPoint = hasPoint, PointX = pointX, PointY = pointY
                };
            }
            catch (COMException error) { return Fail("COM error / Ошибка COM 0x" + error.ErrorCode.ToString("X8")); }
            catch { return Fail("read error / Ошибка чтения"); }
        }
    }
}
