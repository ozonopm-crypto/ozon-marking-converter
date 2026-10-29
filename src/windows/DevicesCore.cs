using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OzonMarkingDesktop
{
    public sealed class BaudProbe
    {
        private static readonly int[] Speeds = { 9600, 115200, 38400, 19200, 57600, 4800, 2400, 1200 };
        private int index;
        private long since, received;
        public bool Confirmed { get; private set; }
        public int Speed { get { return Speeds[index]; } }
        public BaudProbe(int saved)
        {
            int found = Array.IndexOf(Speeds, saved);
            index = found >= 0 ? found : 0; Confirmed = found >= 0;
        }
        public void Received(long now) { received = now; }
        public void Confirm() { Confirmed = true; received = 0; }
        public bool ShouldAdvance(long now)
        {
            if (since == 0) since = now;
            if (received == 0 && Confirmed) return false;
            if (received != 0 && now - received < 1600) return false;
            if (received == 0 && now - since < 4000) return false;
            index = (index + 1) % Speeds.Length; since = now; received = 0; Confirmed = false;
            return true;
        }
    }

    public sealed class ScannerDevice
    {
        public string Id, Name, Port;
        public bool Keyboard;
        public bool LikelyScanner { get { return DevicePolicy.IsScannerName(Name); } }
        public override string ToString() { return (Keyboard ? "USB/HID: " : Port + ": ") + Name; }
    }

    public static class DevicePolicy
    {
        public static string RateKey(string identity)
        {
            using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
                return "baud_" + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-", "");
        }
        public static bool IsScannerName(string value)
        {
            return Regex.IsMatch(value ?? "", @"(?i)(\bATOL\b|АТОЛ|SB[ -]?3100|barcode|bar code|сканер|scanner)");
        }
        public static ScannerDevice ChoosePort(IEnumerable<ScannerDevice> devices, string selection, string identity)
        {
            List<ScannerDevice> candidates = new List<ScannerDevice>();
            foreach (ScannerDevice device in devices)
            {
                if (device.Keyboard) continue;
                // Saved PnP identity follows a scanner if Windows assigns a new COM number.
                if (!String.IsNullOrEmpty(identity) && selection != "AUTO" && selection != "OFF")
                {
                    if (String.Equals(device.Id, identity, StringComparison.OrdinalIgnoreCase)) return device;
                    continue; // Never fall back to a reused COM number belonging to another device.
                }
                if (String.Equals(device.Port, selection, StringComparison.OrdinalIgnoreCase)) return device;
                if (device.LikelyScanner) candidates.Add(device);
            }
            return selection == "AUTO" && candidates.Count == 1 ? candidates[0] : null;
        }
    }

    public sealed class SerialScan
    {
        public string Raw;
        public IntPtr Window;
        public long Generation;
    }

    // A COM scan can arrive in many chunks; never treat an idle truncated scan as a new code.
    public sealed class SerialFramer
    {
        private readonly StringBuilder text = new StringBuilder();
        private long last;
        private IntPtr window;
        private long generation;
        private bool overflow;
        public void Reset() { text.Clear(); last = 0; overflow = false; }
        public List<SerialScan> Feed(string chunk, long now, IntPtr target, long inputGeneration)
        {
            List<SerialScan> scans = new List<SerialScan>();
            if (last != 0 && now - last > 1500) Reset();
            foreach (char ch in chunk)
            {
                if (ch == '\r' || ch == '\n')
                {
                    if (text.Length > 0 && !overflow) scans.Add(new SerialScan { Raw = text.ToString(), Window = window, Generation = generation });
                    Reset(); continue;
                }
                if (overflow) continue;
                if (text.Length == 0) { window = target; generation = inputGeneration; }
                if (ch != '\x1d' && ch != '\t' && (ch < ' ' || ch > '~')) { text.Clear(); overflow = true; continue; }
                text.Append(ch); last = now;
                if (text.Length > 180) { text.Clear(); overflow = true; }
            }
            return scans;
        }
        public SerialScan Poll(long now)
        {
            if (text.Length == 0 || now - last < 350) return null;
            SerialScan result = !overflow && MarkingCode.Normalize(text.ToString()).Ok ? new SerialScan { Raw = text.ToString(), Window = window, Generation = generation } : null;
            if (result != null || now - last > 1500) Reset();
            return result;
        }
    }
}
