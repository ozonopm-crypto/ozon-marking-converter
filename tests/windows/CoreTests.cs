using System;
using OzonMarkingDesktop;

internal static class CoreTests
{
    private static int count;
    private static void Check(bool pass, string label) { if (!pass) throw new Exception(label); count++; }
    private static string Base = "010474334896754821TEST123456789";
    private static string Tail = new string('A', 43) + "=";
    private static string Expected = Base + MarkingCode.Marker + "91TEST" + MarkingCode.Marker + "92" + Tail;
    private static long Feed(ScanBuffer buffer, string value, long now, IntPtr window)
    {
        foreach (char ch in value) { buffer.Character(ch, now, window, false); now += 7; }
        return now;
    }
    public static void Main()
    {
        string[] separators = { "", "\x1d", "\t", " ", "   ", @"\u001d", @"\u001D", "[GS]", "<GS>", "{GS}", "#GS#", "#29", "#29F" };
        foreach (string separator in separators)
        {
            string raw = Base + separator + "91TEST" + separator + "92" + Tail;
            CodeResult value = MarkingCode.Normalize(raw);
            Check(value.Ok && value.Code == Expected, "separator " + separator);
            Check(MarkingCode.CanInsertSeparator(Base), "first boundary");
            Check(MarkingCode.CanInsertSeparator(Base + separator + "91TEST"), "second boundary");
        }
        Check(MarkingCode.Normalize("]d2" + MarkingCode.DemoRaw + "\r\n").Code == Expected, "AIM prefix and newline");
        Check(!MarkingCode.Normalize(Expected).Changed, "idempotent");
        foreach (string broken in new string[] { "", "hello", MarkingCode.DemoRaw.Substring(1), MarkingCode.DemoRaw + "X", MarkingCode.DemoRaw.Replace("04743348967548", "04743348967549"), MarkingCode.DemoRaw.Replace("TEST123", "ТЕСТ123"), MarkingCode.DemoRaw.Replace("91TEST", "1TEST"), MarkingCode.DemoRaw.Replace("92", "93") })
            Check(!MarkingCode.Normalize(broken).Ok, "malformed scan rejected");
        Check(MarkingCode.PossiblePrefix("]d2"), "partial prefix");
        Check(!MarkingCode.PossiblePrefix("]d9"), "wrong prefix");
        Check(!MarkingCode.PossiblePrefix("010474334896754823"), "wrong AI 21");
        Check(!MarkingCode.CanInsertSeparator(Expected), "no separator at end");
        string specialSerial = "91Ab92CDEfgHi";
        Check(MarkingCode.Normalize("010474334896754821" + specialSerial + "91919292" + Tail).Ok, "AI-like serial is not split");

        IntPtr first = new IntPtr(1), second = new IntPtr(2);
        ScanBuffer buffer = new ScanBuffer();
        long now = Feed(buffer, Base, 1000, first);
        Check(buffer.Control("Tab", now, first), "first Tab trapped");
        now = Feed(buffer, "91TEST", now + 7, first);
        Check(buffer.Control("F8", now, first), "second F8 trapped");
        now = Feed(buffer, "92" + Tail, now + 7, first);
        Check(buffer.Control("Enter", now, first), "scanner Enter trapped");
        Check(buffer.Poll(now + 50, 9) == null, "wait for scan idle");
        ScanPacket scan = buffer.Poll(now + 300, 9);
        Check(scan != null && MarkingCode.Normalize(scan.Raw).Code == Expected, "separator scan completes");
        Check(scan.Typed == MarkingCode.DemoRaw, "backspace count excludes intercepted controls");
        Check(scan.Generation == 9 && scan.Window == first, "scan target retained");
        Check(buffer.Control("Enter", now + 320, first), "late scanner Enter trapped");
        Check(!buffer.Control("Enter", now + 320, second), "late Enter in another window untouched");
        Check(!buffer.Control("Enter", now + 1500, first), "ordinary Enter after suffix window untouched");
        Check(ScanBuffer.CanReplace("prefix" + scan.Typed, scan), "only typed suffix can be replaced");
        Check(!ScanBuffer.CanReplace(scan.Typed + "other", scan), "changed field rejected");
        Check(!ScanBuffer.CanReplace(null, scan), "unreadable field rejected");

        buffer = new ScanBuffer();
        Check(!buffer.Control("Tab", 1000, first), "ordinary Tab untouched");
        buffer.Character('0', 1000, first, false);
        buffer.Character('1', 1400, first, false);
        Check(!buffer.HasCandidate, "slow typing not captured");
        Feed(buffer, MarkingCode.DemoRaw, 2000, first);
        buffer.Character('x', 3000, second, false);
        Check(!buffer.HasCandidate, "focus/window change resets candidate");
        buffer = new ScanBuffer();
        now = Feed(buffer, "]d2" + MarkingCode.DemoRaw, 1000, first);
        scan = buffer.Poll(now + 300, 1);
        Check(scan != null && scan.Typed.StartsWith("]d2"), "AIM prefix included in deletion count");
        Check(MarkingCode.Normalize(scan.Raw).Code == Expected, "AIM prefix conversion");
        buffer = new ScanBuffer();
        now = Feed(buffer, Base, 1000, first);
        Check(buffer.Control("GS", now, first), "Ctrl+] represented as separator");
        buffer.Reset(); Check(!buffer.HasCandidate, "pause clears candidate");
        now = Feed(buffer, "Ordinary text", 2000, first);
        Check(buffer.Poll(now + 300, 1) == null, "ordinary text never forms a packet");
        Console.WriteLine("PASS: " + count + " normalization, scan timing, boundary, focus and replacement checks.");
    }
}
