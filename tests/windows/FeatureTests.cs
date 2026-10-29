using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using OzonMarkingDesktop;

internal static class FeatureTests
{
    private static int count;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
    private static void Reject(Action action, string name)
    {
        bool failed = false; try { action(); } catch { failed = true; }
        Check(failed, name);
    }
    public static void Main(string[] args)
    {
        SerialFramer stream = new SerialFramer();
        IntPtr first = new IntPtr(10), other = new IntPtr(20);
        string raw = "]d2" + MarkingCode.DemoRaw;
        Check(stream.Feed(raw.Substring(0, 20), 1000, first, 1).Count == 0, "partial frame");
        Check(stream.Feed(raw.Substring(20), 1100, other, 2).Count == 0, "frame waits for delimiter or idle");
        SerialScan scan = stream.Poll(1500);
        Check(scan != null && scan.Raw == raw && scan.Window == first && scan.Generation == 1, "initial target and input retained across chunks");
        Check(stream.Poll(2000) == null, "no duplicate idle scan");
        stream.Feed(raw, 2001, first, 1);
        Check(stream.Poll(2020) == null, "COM waits for frame boundary before fast delivery");
        Check(stream.Poll(2041) != null, "complete COM scan avoids 350 ms idle delay");
        Check(stream.Feed(raw + "\r\n" + raw + "\n", 2100, first, 1).Count == 2, "CRLF and multiple packets");
        stream.Reset();
        Check(stream.Feed(new string('X', 200) + "\r\n" + raw + "\r", 2200, first, 1).Count == 1, "overflow resync");
        stream.Feed(raw.Substring(0, 10), 2300, first, 1);
        Check(stream.Poll(4000) == null, "truncated frame never emitted");
        Check(stream.Feed(raw + "\r", 4100, first, 1)[0].Raw == raw, "resync after truncation");
        stream.Feed("garbage\0" + raw, 4200, first, 1);
        Check(stream.Poll(4600) == null, "corrupt bytes never repaired");
        Check(stream.Feed("\r" + raw + "\r", 4700, first, 1).Count == 1, "resync after invalid byte");
        ScanPacket direct = new ScanPacket { DirectInput = true, Typed = "" };
        Check(ScanBuffer.CanReplace("", direct), "COM can fill empty field");
        Check(!ScanBuffer.CanReplace("existing text", direct), "COM cannot erase existing text");
        Check(!ScanBuffer.CanReplace(null, direct), "COM cannot fill unreadable field");
        string prepared = MarkingCode.Normalize(MarkingCode.DemoRaw).Code;
        ScanPacket keyboard = new ScanPacket { Typed = MarkingCode.DemoRaw };
        ReplacementPlan plan = ReplacementPlan.Create(keyboard.Typed, keyboard, prepared, true);
        Check(plan != null && plan.Matches(prepared), "full-field replacement verified");
        Check(plan.Matches(prepared.Replace(MarkingCode.Marker, "\x1d")), "decoded GS from Ozon accepted");
        Check(!plan.Matches(MarkingCode.DemoRaw), "unmodified raw code is never success");
        Check(!plan.Matches(prepared.Substring(0, prepared.Length - 1)), "truncated result is never success");
        Check(ReplacementPlan.Create("prefix" + keyboard.Typed, keyboard, prepared, true) == null, "MSAA must not select unrelated prefix");
        plan = ReplacementPlan.Create("prefix" + keyboard.Typed, keyboard, prepared, false);
        Check(plan != null && plan.Matches("prefix" + prepared), "UIA suffix preserves verified prefix");
        Check(!plan.Matches(prepared), "lost prefix rejected");
        Check(ReplacementPlan.Create("existing", direct, prepared, true) == null, "COM never replaces existing text");
        keyboard.Typed = "different transport representation";
        keyboard.Raw = MarkingCode.DemoRaw;
        plan = ReplacementPlan.Create(MarkingCode.DemoRaw, keyboard, prepared, true);
        Check(plan != null && plan.ReplaceWhole, "exact decoded code supports whole-field replacement without caret dependency");
        plan = ReplacementPlan.Create(prepared.Replace(MarkingCode.Marker, "\x1d"), keyboard, prepared, true);
        Check(plan != null && plan.ReplaceWhole, "browser-decoded separators do not cause a false changed-field error");
        Check(ReplacementPlan.Create(MarkingCode.DemoRaw.Replace("TEST123", "Test123"), keyboard, prepared, true) == null, "different letter case is not accepted as equivalent code");
        System.Collections.Generic.Dictionary<string, int> remembered = new System.Collections.Generic.Dictionary<string, int>();
        ScannerDevice unrelated = new ScannerDevice { Port = "COM1", Id = "UPS-ID", Name = "UPS Serial Port" };
        Check(!DevicePolicy.ShouldOpenSerial(unrelated, remembered), "unrelated COM port must not be opened");
        Check(!DevicePolicy.ShouldOpenSerial(new ScannerDevice { Port = "COM4", Id = "FISCAL-ID", Name = "АТОЛ 30Ф" }, remembered), "ATOL manufacturer alone does not identify a scanner");
        ScannerDevice scannerPort = new ScannerDevice { Port = "COM2", Id = "SCANNER-ID", Name = "ATOL SB3100" };
        Check(DevicePolicy.ShouldOpenSerial(scannerPort, remembered), "named scanner is discovered automatically");
        ScannerDevice generic = new ScannerDevice { Port = "COM7", Id = "REMEMBERED-ID", Name = "USB Serial" };
        remembered[DevicePolicy.RateKey(generic.Id)] = 38400;
        Check(DevicePolicy.ShouldOpenSerial(generic, remembered), "previously confirmed scanner is remembered by identity");
        Check(!DevicePolicy.ShouldOpenSerial(new ScannerDevice { Port = "COM7", Id = "OTHER-ID", Name = "USB Serial" }, remembered), "COM number reuse does not claim unrelated equipment");
        Check(!DevicePolicy.ShouldOpenSerial(new ScannerDevice { Keyboard = true, Name = "ATOL" }, remembered), "HID scanner never opens a COM channel");
        ScanBuffer layoutBuffer = new ScanBuffer();
        long stamp = 1000;
        foreach (char ch in MarkingCode.DemoRaw)
        {
            // The scanner stream is decoded as US while the actual field may contain Cyrillic/caps.
            char typed = ch == 'T' ? 'Е' : ch == 'A' ? 'a' : ch;
            layoutBuffer.Character(ch, typed, stamp, first, false); stamp += 7;
        }
        ScanPacket layoutPacket = layoutBuffer.Poll(stamp + 300, 5);
        Check(MarkingCode.Normalize(layoutPacket.Raw).Ok, "US decoding independent of field layout/caps");
        Check(layoutPacket.Typed.Contains("Е") && ScanBuffer.CanReplace(layoutPacket.Typed, layoutPacket), "actual Cyrillic suffix verified before replacement");

        BaudProbe probe = new BaudProbe(0);
        Check(probe.Speed == 9600 && !probe.Confirmed, "auto starts at common baud");
        Check(!probe.ShouldAdvance(1000), "no immediate rate switch");
        Check(!probe.ShouldAdvance(5100) && probe.Speed == 9600, "idle port does not reconnect or change baud");
        probe.Received(6000);
        Check(!probe.ShouldAdvance(6300), "do not change baud mid scan");
        Check(probe.ShouldAdvance(7700) && probe.Speed == 115200, "invalid scan triggers next rate");
        Check(!probe.ShouldAdvance(100000), "no endless cycling after a failed scan");
        probe.Confirm();
        Check(!probe.ShouldAdvance(1000000), "successful baud stays locked while idle");
        probe = new BaudProbe(38400);
        Check(probe.Speed == 38400 && probe.Confirmed, "remembered baud restored");
        probe.Received(1000); probe.Confirm();
        Check(!probe.ShouldAdvance(4000), "confirmed scan doesn't trigger rotation");
        probe.Received(5000);
        Check(probe.ShouldAdvance(6800), "changed scanner configuration can be rediscovered");
        Check(DevicePolicy.RateKey("scanner") == DevicePolicy.RateKey("scanner") && DevicePolicy.RateKey("other") != DevicePolicy.RateKey("scanner"), "stable identity without storing name");

        string json = "{\"version\":\"1.3.0\",\"url\":\"" + UpdatePolicy.Root + "downloads/OzonMarkingConverter-Setup-1.3.0.exe\",\"sha256\":\"" + new string('a', 64) + "\",\"size\":100000,\"notes\":\"Test\"}";
        UpdateRelease release = UpdatePolicy.Parse(json);
        Check(UpdatePolicy.Newer(release.version, new Version(1, 2, 0, 0)), "newer update");
        Check(!UpdatePolicy.Newer("1.2.0", new Version(1, 2, 0, 0)), "equal release ignored");
        Check(!UpdatePolicy.Newer("1.1.9", new Version(1, 2, 0, 0)), "downgrade ignored");
        Reject(delegate { UpdatePolicy.Parse(json.Replace("raw.githubusercontent.com", "example.com")); }, "external installer rejected");
        Reject(delegate { UpdatePolicy.Parse(json.Replace("/main/", "/other/")); }, "wrong repo path rejected");
        Reject(delegate { UpdatePolicy.Parse(json.Replace("https:", "http:")); }, "unencrypted installer rejected");
        Reject(delegate { UpdatePolicy.Parse(json.Replace("Setup-1.3.0", "Setup-9.0.0")); }, "version URL mismatch rejected");
        Reject(delegate { UpdatePolicy.Parse(json.Replace("\"size\":100000", "\"size\":99999999")); }, "oversize rejected");
        Reject(delegate { UpdatePolicy.Parse(json.Replace(new string('a', 64), "invalid")); }, "invalid hash rejected");
        Reject(delegate { UpdatePolicy.Parse("{}"); }, "missing fields rejected");
        Reject(delegate { UpdatePolicy.Parse("not json"); }, "invalid JSON rejected");

        string fixture = Path.Combine(Path.GetTempPath(), "PEI-update-fixture-" + Guid.NewGuid().ToString("N") + ".exe");
        try
        {
            byte[] bytes = new byte[100000]; bytes[0] = (byte)'M'; bytes[1] = (byte)'Z'; File.WriteAllBytes(fixture, bytes);
            using (SHA256 hash = SHA256.Create()) release.sha256 = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
            UpdatePolicy.Verify(fixture, release); Check(true, "full download verification");
            bytes[50] = 1; File.WriteAllBytes(fixture, bytes);
            Reject(delegate { UpdatePolicy.Verify(fixture, release); }, "tampered download rejected");
            File.WriteAllBytes(fixture, new byte[99]);
            Reject(delegate { UpdatePolicy.Verify(fixture, release); }, "truncated download rejected");
        }
        finally { File.Delete(fixture); }
        Console.WriteLine("PASS: " + count + " serial framing, automatic baud, safe field insertion and updater checks.");
        if (args.Length > 0 && args[0] == "--network")
        {
            Type type = typeof(UpdatePolicy).Assembly.GetType("OzonMarkingDesktop.Updates");
            UpdateRelease live = (UpdateRelease)type.GetMethod("Check", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            string download = (string)type.GetMethod("Download", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { live, null });
            UpdatePolicy.Verify(download, live);
            File.Delete(download);
            Console.WriteLine("PASS: live GitHub feed, installer download and SHA256. No installer was executed.");
        }
    }
}
