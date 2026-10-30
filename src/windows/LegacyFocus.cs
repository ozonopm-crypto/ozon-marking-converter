using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Accessibility;

namespace OzonMarkingDesktop
{
    // RU: Chromium не всегда предоставляет нативный UIA. Читаем только фокус через MSAA.
    // EN: Chromium may expose only MSAA. Follow the focused object; never search unrelated fields.
    internal static class LegacyFocus
    {
        [ThreadStatic] public static string LastTrace;
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct GuiInfo
        {
            public uint Size, Flags;
            public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
            public Rect CaretRect;
        }
        [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread, ref GuiInfo info);
        [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IAccessible result);
        [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromPoint(Point point, [MarshalAs(UnmanagedType.Interface)] out IAccessible result, [MarshalAs(UnmanagedType.Struct)] out object child);
        [DllImport("oleacc.dll")] private static extern int WindowFromAccessibleObject([MarshalAs(UnmanagedType.Interface)] IAccessible accessible, out IntPtr window);
        private static long Identity(IAccessible value)
        {
            IntPtr pointer = Marshal.GetIUnknownForObject(value);
            try { return pointer.ToInt64(); } finally { Marshal.Release(pointer); }
        }
        private static FocusFrame Fail(string reader, string stage)
        {
            LastTrace = reader + ": " + stage;
            return null;
        }
        private static bool SameOwner(IAccessible accessible, IntPtr foreground, out bool verified)
        {
            verified = false;
            IntPtr owner;
            // RU: Некоторые поставщики не возвращают HWND; чужой известный процесс запрещён.
            // EN: Some providers expose no HWND; a known foreign process is always rejected.
            if (WindowFromAccessibleObject(accessible, out owner) != 0 || owner == IntPtr.Zero) return true;
            uint expected, actual;
            Native.GetWindowThreadProcessId(foreground, out expected);
            Native.GetWindowThreadProcessId(owner, out actual);
            verified = expected != 0 && actual != 0 && expected == actual;
            return verified;
        }
        private static FocusFrame Frame(IAccessible current, object child, IntPtr window, string reader, bool atPoint, int x, int y)
        {
            LastTrace = reader + ": role/state";
            int role = Convert.ToInt32(current.get_accRole(child));
            int state = Convert.ToInt32(current.get_accState(child));
            if (role != 42 || (state & 4) == 0) return Fail(reader, "no focused text");
            // RU: Недоступное, защищённое, скрытое, вне экрана или только для чтения поле запрещено.
            // EN: Reject unavailable, protected, hidden, offscreen, and read-only fields.
            if ((state & (1 | 0x40 | 0x8000 | 0x10000 | 0x20000000)) != 0) return Fail(reader, "ineligible text state");
            LastTrace = reader + ": owner";
            bool ownerVerified;
            if (!SameOwner(current, window, out ownerVerified)) return Fail(reader, "foreign owner");
            LastTrace = reader + ": eligibility";
            string name = (current.get_accName(child) ?? "").ToLowerInvariant();
            if (name.Contains("адрес") || name.Contains("address") || name.Contains("поиск") || name.Contains("search") || name.Contains("omnibox"))
                return Fail(reader, "address/search field");
            LastTrace = reader + ": value";
            string value = current.get_accValue(child);
            if (value == null || value.Length > 1024) return Fail(reader, "value unavailable/out of bounds");
            LastTrace = reader + ": bounds";
            int left, top, width, height;
            current.accLocation(out left, out top, out width, out height, child);
            if (width <= 0 || height <= 0) return Fail(reader, "empty bounds");
            if (atPoint && (x < left || y < top || (long)x >= (long)left + width || (long)y >= (long)top + height))
                return Fail(reader, "point outside field");
            if (Native.GetForegroundWindow() != window) return Fail(reader, "foreground changed");
            LastTrace = reader + ": length=" + value.Length + "; owner=" + (ownerVerified ? "verified" : "unavailable");
            return new FocusFrame
            {
                Identity = "MSAA:" + window + ":" + Identity(current) + ":" + child,
                Value = value, CaretAtEnd = true, WholeValueOnly = true, HasNonEmptyRepresentation = value.Length > 0,
                LegacyObject = current, LegacyChild = child,
                LegacyAtPoint = atPoint, PointX = x, PointY = y,
                Reader = reader, Trace = LastTrace
            };
        }
        public static FocusFrame Read()
        {
            const string reader = "MSAA(focus)";
            LastTrace = reader + ": start";
            try { return ReadFocus(reader); }
            catch { LastTrace += "; provider error"; return null; }
        }
        private static FocusFrame ReadFocus(string reader)
        {
            IntPtr window = Native.GetForegroundWindow();
            if (!Native.BrowserWindow(window)) return Fail(reader, "foreground is not a supported browser");
            uint pid; uint thread = Native.GetWindowThreadProcessId(window, out pid);
            GuiInfo info = new GuiInfo { Size = (uint)Marshal.SizeOf(typeof(GuiInfo)) };
            if (!GetGUIThreadInfo(thread, ref info) || info.Focus == IntPtr.Zero) return Fail(reader, "focused HWND unavailable");
            Guid iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
            IAccessible current;
            LastTrace = reader + ": focused accessible object";
            if (AccessibleObjectFromWindow(info.Focus, 0xfffffffc, ref iid, out current) != 0 || current == null) return Fail(reader, "focused accessible object unavailable");
            object child = 0;
            for (int depth = 0; depth < 24; depth++)
            {
                LastTrace = reader + ": focus traversal";
                int role = Convert.ToInt32(current.get_accRole(child));
                int state = Convert.ToInt32(current.get_accState(child));
                if (role == 42 && (state & 4) != 0)
                    return Frame(current, child, window, reader, false, 0, 0);
                object focus = current.accFocus;
                IAccessible next = focus as IAccessible;
                if (next != null)
                {
                    if (Identity(next) == Identity(current)) return Fail(reader, "focus traversal cycle");
                    current = next; child = 0; continue;
                }
                if (focus is int && (int)focus != 0)
                {
                    object descendant = current.get_accChild(focus);
                    next = descendant as IAccessible;
                    if (next != null) { current = next; child = 0; }
                    else { if (Object.Equals(child, focus)) return Fail(reader, "focus traversal cycle"); child = focus; }
                    continue;
                }
                return Fail(reader, "focused descendant unavailable");
            }
            return Fail(reader, "focus traversal limit");
        }
        public static FocusFrame ReadAt(int x, int y)
        {
            const string reader = "MSAA(point)";
            LastTrace = reader + ": start";
            try
            {
                IntPtr window = Native.GetForegroundWindow();
                if (!Native.BrowserWindow(window)) return Fail(reader, "foreground is not a supported browser");
                IAccessible current;
                object child;
                LastTrace = reader + ": hit object";
                if (AccessibleObjectFromPoint(new Point { X = x, Y = y }, out current, out child) != 0 || current == null || !(child is int))
                    return Fail(reader, "hit object unavailable");
                for (int parents = 0; parents <= 4; parents++)
                {
                    LastTrace = reader + ": hit owner";
                    bool hitOwnerVerified;
                    if (!SameOwner(current, window, out hitOwnerVerified)) return Fail(reader, "foreign hit owner");
                    LastTrace = reader + ": parent traversal";
                    int role = Convert.ToInt32(current.get_accRole(child));
                    int state = Convert.ToInt32(current.get_accState(child));
                    if ((state & (1 | 0x8000 | 0x10000 | 0x20000000)) != 0)
                        return Fail(reader, "ineligible hit state");
                    if (role == 42 && (state & 4) != 0)
                        return Frame(current, child, window, reader, true, x, y);
                    if (parents == 4) break;
                    // RU: Только родитель объекта под точкой, без поиска по соседним веткам.
                    // EN: Follow only the hit object's parent, never search sibling branches.
                    if ((int)child != 0) { child = 0; continue; }
                    IAccessible parent = current.accParent as IAccessible;
                    if (parent == null) return Fail(reader, "hit parent unavailable");
                    if (Identity(parent) == Identity(current)) return Fail(reader, "parent traversal cycle");
                    current = parent; child = 0;
                }
                return Fail(reader, "no focused text within parent limit");
            }
            catch { LastTrace += "; provider error"; return null; }
        }
    }
}
