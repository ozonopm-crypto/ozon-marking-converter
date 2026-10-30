using System;
using OzonMarkingDesktop;

internal static class AltNumpadTests
{
    private static int count;
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        count++;
    }
    private static void Digits(AltNumpadSeparator collector, string digits)
    {
        foreach (char digit in digits)
        {
            uint key = (uint)(0x60 + digit - '0');
            collector.Feed(key, 0, false, false);
            collector.Feed(key, 0, false, true);
        }
    }
    private static long Text(ScanBuffer buffer, string value, long now, IntPtr window)
    {
        foreach (char ch in value) { buffer.Character(ch, now, window, false); now += 7; }
        return now;
    }
    private static long Separator(ScanBuffer buffer, string digits, long now, IntPtr window, bool numLock, int step)
    {
        Check(buffer.ShouldSuppressControl("GS", now, window), "Alt code starts only at a GS boundary");
        AltNumpadSeparator collector = new AltNumpadSeparator();
        collector.Start();
        Check(buffer.KeepAlive(now, window), "Alt key-down keeps the existing frame alive");
        int[] scans = { 0x52, 0x4f, 0x50, 0x51, 0x4b, 0x4c, 0x4d, 0x47, 0x48, 0x49 };
        // RU: С выключенным Num Lock сканер передаёт VK_INSERT/DOWN/PRIOR для 0/2/9.
        // EN: With Num Lock off the scanner sends VK_INSERT/DOWN/PRIOR for 0/2/9.
        int[] navigation = { 0x2d, 0x23, 0x28, 0x22, 0x25, 0x0c, 0x27, 0x24, 0x26, 0x21 };
        foreach (char ch in digits)
        {
            int digit = ch - '0';
            uint key = (uint)(numLock ? 0x60 + digit : navigation[digit]);
            now += step;
            Check(buffer.KeepAlive(now, window) && collector.Feed(key, (uint)scans[digit], false, false), "numpad digit advances the separator without adding payload");
            now += step;
            Check(buffer.KeepAlive(now, window) && collector.Feed(key, (uint)scans[digit], false, true), "numpad release does not duplicate a digit");
        }
        now += step;
        Check(buffer.KeepAlive(now, window) && collector.Finish() && buffer.Control("GS", now, window), "Alt release inserts exactly one supported separator");
        return now + 7;
    }
    private static void Integration()
    {
        IntPtr first = new IntPtr(1), second = new IntPtr(2);
        string raw = MarkingCode.DemoRaw, expected = MarkingCode.Normalize(raw).Code;
        foreach (bool numLock in new bool[] { true, false })
        foreach (int step in new int[] { 7, 100 })
        {
            ScanBuffer buffer = new ScanBuffer { GapMilliseconds = 220, IdleMilliseconds = 260 };
            long now = Text(buffer, raw.Substring(0, 31), 1000, first);
            now = Separator(buffer, "029", now, first, numLock, step);
            now = Text(buffer, raw.Substring(31, 6), now, first);
            now = Separator(buffer, "029", now, first, numLock, step);
            now = Text(buffer, raw.Substring(37), now, first);
            Check(buffer.Control("Enter", now, first), "realistic full scan terminator accepted");
            ScanPacket packet = buffer.Poll(now + 50, 1);
            Check(packet != null && packet.Raw.Length == 85 && packet.Typed == raw && MarkingCode.Normalize(packet.Raw).Code == expected, "two Alt029 separators preserve the complete synthetic payload and typed count");
            Check(buffer.Poll(now + 500, 1) == null && buffer.EmittedPackets == 1, "one physical scan emits once");
        }

        ScanBuffer lost = new ScanBuffer();
        long stamp = Text(lost, raw.Substring(0, 31), 5000, first);
        lost.Clear("unsupported Alt separator");
        Text(lost, raw.Substring(31), stamp, first);
        ScanPacket rejected = lost.Poll(stamp + 1000, 1);
        Check(rejected == null || !MarkingCode.Normalize(rejected.Raw).Ok, "discarded Alt sequence cannot yield a complete replacement frame");
        Check(lost.LastResetLength == 31 && lost.LastResetReason == "unsupported Alt separator", "later one-character prefix resync keeps the largest frame loss reason");

        ScanBuffer focus = new ScanBuffer();
        stamp = Text(focus, raw.Substring(0, 31), 7000, first);
        Check(!focus.KeepAlive(stamp, second), "Alt sequence cannot extend a candidate in another window");
        focus.Reset("Alt separator focus changed");
        Text(focus, raw.Substring(31), stamp, second);
        rejected = focus.Poll(stamp + 1000, 1);
        Check(rejected == null || !MarkingCode.Normalize(rejected.Raw).Ok, "focus-changed tail cannot complete the previous field's frame");

        ScanBuffer timeout = new ScanBuffer();
        stamp = Text(timeout, raw.Substring(0, 31), 9000, first);
        Check(!timeout.KeepAlive(stamp + timeout.GapMilliseconds + 1, first), "a delayed Alt event cannot revive an expired frame");
        timeout.Reset("Alt separator timeout");
        Check(!timeout.KeepAlive(stamp + 500, first), "keep-alive cannot create an empty candidate");
        Text(timeout, raw.Substring(31), stamp + 500, first);
        rejected = timeout.Poll(stamp + 1500, 1);
        Check(rejected == null || !MarkingCode.Normalize(rejected.Raw).Ok, "expired sequence tail cannot become a valid full code");

        ScanBuffer boundary = new ScanBuffer();
        stamp = Text(boundary, raw.Substring(0, 30), 12000, first);
        Check(!boundary.ShouldSuppressControl("GS", stamp, first), "Alt029 is not interpreted inside a serial");
        stamp = Text(boundary, raw.Substring(30, 2), stamp, first);
        Check(!boundary.ShouldSuppressControl("GS", stamp, first), "Alt029 is not interpreted inside AI91");

        foreach (string invalid in new string[] { "28", "030", "", "000029" })
        {
            AltNumpadSeparator collector = new AltNumpadSeparator();
            collector.Start(); Digits(collector, invalid);
            Check(!collector.Finish(), "unsupported Alt code cannot request a GS insertion");
        }
    }
    public static void Main()
    {
        AltNumpadSeparator collector = new AltNumpadSeparator();
        Check(!collector.Active && !collector.Finish(), "inactive collector emits nothing");
        Check(!collector.Feed(0x62, 0, false, false), "inactive input cannot start collection");
        foreach (string valid in new string[] { "29", "029", "0029", "00029" })
        {
            collector.Start(); Digits(collector, valid);
            Check(collector.Finish() && !collector.Active, "decimal 29 with supported leading zeros");
            Check(!collector.Finish(), "a separator is emitted once");
        }

        // RU: Во всём диапазоне однобайтных Alt-кодов допустим только GS (29).
        // EN: Across the entire byte-sized Alt-code range, only GS (29) is accepted.
        for (int value = 0; value <= 255; value++)
        {
            collector.Start(); Digits(collector, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Check(collector.Finish() == (value == 29), "only decimal 29 is a separator: " + value);
        }
        foreach (string invalid in new string[] { "", "290", "329", "99999", "000029", "00000029" })
        {
            collector.Start(); Digits(collector, invalid);
            Check(!collector.Finish() && !collector.Active, "empty, wrong or overlong Alt code rejected");
        }

        int[] scans = { 0x52, 0x4f, 0x50, 0x51, 0x4b, 0x4c, 0x4d, 0x47, 0x48, 0x49 };
        for (int value = 0; value <= 9; value++)
        {
            int digit;
            Check(AltNumpadSeparator.TryDigit((uint)(0x60 + value), 0, false, out digit) && digit == value, "VK_NUMPAD digit mapping");
            Check(AltNumpadSeparator.TryDigit(0, (uint)scans[value], false, out digit) && digit == value, "Num Lock off physical scan mapping");
            Check(!AltNumpadSeparator.TryDigit(0, (uint)scans[value], true, out digit), "extended navigation scan is not numpad");
        }
        int ignored;
        Check(!AltNumpadSeparator.TryDigit(0x32, 0x03, false, out ignored), "top-row 2 is not a numpad digit");
        Check(!AltNumpadSeparator.TryDigit(0x25, 0x4b, true, out ignored), "dedicated Left arrow is not numpad 4");
        Check(!AltNumpadSeparator.TryDigit(0x24, 0x47, true, out ignored), "dedicated Home is not numpad 7");

        collector.Start();
        Check(collector.Feed(0x28, 0x50, false, false), "Num Lock off Down position collects 2");
        Check(collector.Feed(0x21, 0x49, false, false), "Num Lock off PageUp position collects 9");
        Check(collector.Finish(), "Num Lock off 29 accepted");
        collector.Start();
        Check(collector.Feed(0x41, 0x1e, false, true), "unrelated key-up does not add or invalidate digits");
        Digits(collector, "29"); Check(collector.Finish(), "key-up ignored while collecting");
        collector.Start(); Digits(collector, "2");
        Check(!collector.Feed(0x41, 0x1e, false, false), "nondigit key-down invalidates collection");
        Digits(collector, "9"); Check(!collector.Finish(), "later digits cannot repair invalid input");
        collector.Start(); Digits(collector, "00000");
        Check(!collector.Feed(0x62, 0, false, false), "sixth digit reports overflow");
        Check(!collector.Finish(), "overflow cannot produce a separator");
        collector.Start(); Digits(collector, "2"); collector.Cancel();
        Check(!collector.Active && !collector.Finish(), "cancel discards incomplete input");
        collector.Start(); Digits(collector, "9"); collector.Start(); Digits(collector, "29");
        Check(collector.Finish(), "start resets previous digits and invalid state");
        collector.Start(); collector.Feed(0x41, 0x1e, false, false); collector.Finish();
        collector.Start(); Digits(collector, "29"); Check(collector.Finish(), "finish clears an invalid collection");
        Integration();
        Console.WriteLine("PASS: " + count + " Alt-numpad GS, full-frame, timing, focus, rejection, scan-code and reset checks.");
    }
}
