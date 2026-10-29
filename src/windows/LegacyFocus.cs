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
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct GuiInfo
        {
            public uint Size, Flags;
            public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
            public Rect CaretRect;
        }
        [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread, ref GuiInfo info);
        [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IAccessible result);
        private static long Identity(IAccessible value)
        {
            IntPtr pointer = Marshal.GetIUnknownForObject(value);
            try { return pointer.ToInt64(); } finally { Marshal.Release(pointer); }
        }
        public static FocusFrame Read()
        {
            IntPtr window = Native.GetForegroundWindow();
            if (!Native.BrowserWindow(window)) return null;
            uint pid; uint thread = Native.GetWindowThreadProcessId(window, out pid);
            GuiInfo info = new GuiInfo { Size = (uint)Marshal.SizeOf(typeof(GuiInfo)) };
            if (!GetGUIThreadInfo(thread, ref info) || info.Focus == IntPtr.Zero) return null;
            Guid iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
            IAccessible current;
            if (AccessibleObjectFromWindow(info.Focus, 0xfffffffc, ref iid, out current) != 0 || current == null) return null;
            object child = 0;
            for (int depth = 0; depth < 24; depth++)
            {
                int role = Convert.ToInt32(current.get_accRole(child));
                int state = Convert.ToInt32(current.get_accState(child));
                if (role == 42 && (state & 4) != 0)
                {
                    // Unavailable, protected, invisible, or read-only text is never eligible.
                    if ((state & (1 | 0x40 | 0x8000 | 0x20000000)) != 0) return null;
                    string name = (current.get_accName(child) ?? "").ToLowerInvariant();
                    if (name.Contains("адрес") || name.Contains("address") || name.Contains("поиск") || name.Contains("search") || name.Contains("omnibox")) return null;
                    string value = current.get_accValue(child);
                    if (value == null || value.Length > 1024) return null;
                    int left, top, width, height;
                    current.accLocation(out left, out top, out width, out height, child);
                    if (width <= 0 || height <= 0) return null;
                    return new FocusFrame
                    {
                        Identity = "MSAA:" + window + ":" + Identity(current) + ":" + child,
                        Value = value, CaretAtEnd = true, WholeValueOnly = true,
                        LegacyObject = current, LegacyChild = child
                    };
                }
                object focus = current.accFocus;
                IAccessible next = focus as IAccessible;
                if (next != null)
                {
                    if (Identity(next) == Identity(current)) return null;
                    current = next; child = 0; continue;
                }
                if (focus is int && (int)focus != 0)
                {
                    object descendant = current.get_accChild(focus);
                    next = descendant as IAccessible;
                    if (next != null) { current = next; child = 0; }
                    else { if (Object.Equals(child, focus)) return null; child = focus; }
                    continue;
                }
                return null;
            }
            return null;
        }
    }
}
