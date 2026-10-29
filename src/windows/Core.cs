using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OzonMarkingDesktop
{
    public sealed class CodeResult
    {
        public bool Ok;
        public string Code = "";
        public string Message = "";
        public bool Changed;
    }

    public static class MarkingCode
    {
        public const string Marker = @"\u001d";
        private const string Separator = @"(?:\x1d|\t| +|\\u001[dD]|\[GS\]|<GS>|\{GS\}|#GS#|#29F|#29)?";
        private static readonly Regex Complete = new Regex(@"\A01([0-9]{14})21([!-~]{13})" + Separator + @"91([!-~]{4})" + Separator + @"92([A-Za-z0-9+/]{43}=)\z", RegexOptions.CultureInvariant);
        private static readonly Regex Boundary = new Regex(@"\A01[0-9]{14}21[!-~]{13}(?:" + Separator + @"91[!-~]{4})?\z", RegexOptions.CultureInvariant);
        private static readonly Regex Start = new Regex(@"\A01[0-9]{14}21", RegexOptions.CultureInvariant);
        public static string Clean(string value)
        {
            value = (value ?? "").Trim('\r', '\n');
            return value.StartsWith("]d2", StringComparison.Ordinal) ? value.Substring(3) : value;
        }
        public static CodeResult Normalize(string raw)
        {
            string value = Clean(raw);
            Match m = Complete.Match(value);
            if (!m.Success) return new CodeResult { Message = value.Length == 0 ? "Ожидаю код маркировки товара." : "Код не соответствует поддерживаемому формату. Повторите сканирование." };
            string gtin = m.Groups[1].Value;
            int sum = 0;
            for (int i = 0; i < 13; i++) sum += (gtin[i] - '0') * (i % 2 == 0 ? 3 : 1);
            if ((10 - sum % 10) % 10 != gtin[13] - '0') return new CodeResult { Message = "Не совпадает контрольная цифра GTIN. Код не изменён." };
            string code = "01" + gtin + "21" + m.Groups[2].Value + Marker + "91" + m.Groups[3].Value + Marker + "92" + m.Groups[4].Value;
            return new CodeResult { Ok = true, Code = code, Changed = code != raw, Message = "Разделители подготовлены." };
        }
        public static bool CanInsertSeparator(string raw) { return Boundary.IsMatch(Clean(raw)); }
        public static bool LooksLikeCode(string raw) { return Start.IsMatch(Clean(raw)); }
        public static bool PossiblePrefix(string raw)
        {
            if (raw.Length == 0) return false;
            if (raw[0] == ']')
            {
                if (raw.Length <= 3) return "]d2".StartsWith(raw, StringComparison.Ordinal);
                if (!raw.StartsWith("]d2", StringComparison.Ordinal)) return false;
                raw = raw.Substring(3);
            }
            string header = "01";
            if (raw.Length <= 2) return header.StartsWith(raw, StringComparison.Ordinal);
            if (!raw.StartsWith(header, StringComparison.Ordinal)) return false;
            for (int i = 2; i < Math.Min(16, raw.Length); i++) if (raw[i] < '0' || raw[i] > '9') return false;
            if (raw.Length > 16 && raw[16] != '2') return false;
            if (raw.Length > 17 && raw[17] != '1') return false;
            return true;
        }
        public static string DemoRaw { get { return "010474334896754821TEST12345678991TEST92" + new string('A', 43) + "="; } }
    }

    public sealed class ScanPacket
    {
        public string Raw;
        public string Typed;
        public IntPtr Window;
        public bool AllowOwnTest;
        public long Generation;
        public bool DirectInput;
    }

    public sealed class ReplacementPlan
    {
        public string Expected;
        public static ReplacementPlan Create(string field, ScanPacket packet, string prepared, bool wholeOnly)
        {
            if (!ScanBuffer.CanReplace(field, packet)) return null;
            if (wholeOnly && !packet.DirectInput && field != packet.Typed) return null;
            string prefix = packet.DirectInput ? "" : field.Substring(0, field.Length - packet.Typed.Length);
            return new ReplacementPlan { Expected = prefix + prepared };
        }
        public bool Matches(string field)
        {
            // Ozon may decode escaped GS in its own state. A raw separator-free code is not success.
            return field == Expected || field == Expected.Replace(MarkingCode.Marker, "\x1d");
        }
    }

    // Stores only a short candidate scan in RAM. No ordinary typing history is retained.
    public sealed class ScanBuffer
    {
        private readonly StringBuilder raw = new StringBuilder();
        private readonly StringBuilder typed = new StringBuilder();
        private long last;
        private IntPtr window;
        private bool ownTest;
        private long suffixUntil;
        private IntPtr suffixWindow;
        public int GapMilliseconds = 220;
        public int IdleMilliseconds = 260;
        public bool HasCandidate { get { return raw.Length != 0; } }
        public string Candidate { get { return raw.ToString(); } }
        public void Clear() { raw.Clear(); typed.Clear(); window = IntPtr.Zero; last = 0; }
        public void Reset() { Clear(); suffixUntil = 0; suffixWindow = IntPtr.Zero; }
        public void Character(char value, long now, IntPtr foreground, bool allowOwnTest)
        {
            Character(value, value, now, foreground, allowOwnTest);
        }
        public void Character(char value, char actualTyped, long now, IntPtr foreground, bool allowOwnTest)
        {
            if (window != foreground || (last != 0 && now - last > GapMilliseconds)) Clear();
            if (value < ' ' || value > '~') { Clear(); return; }
            string next = raw.ToString() + value;
            if (!MarkingCode.PossiblePrefix(next) || next.Length > 150)
            {
                Clear();
                if (value != '0' && value != ']') return;
            }
            raw.Append(value); typed.Append(actualTyped);
            window = foreground; ownTest = allowOwnTest; last = now;
        }
        public bool Control(string key, long now, IntPtr foreground)
        {
            if (foreground == suffixWindow && now < suffixUntil && (key == "Enter" || key == "Tab" || key == "F8")) return true;
            if (raw.Length == 0 || foreground != window || now - last > GapMilliseconds) { Clear(); return false; }
            string value = raw.ToString();
            if ((key == "Tab" || key == "F8" || key == "GS") && MarkingCode.CanInsertSeparator(value))
            {
                raw.Append('\x1d'); last = now; return true;
            }
            if ((key == "Enter" || key == "Tab") && MarkingCode.LooksLikeCode(value))
            {
                last = now; return true;
            }
            Clear(); return false;
        }
        public ScanPacket Poll(long now, long generation)
        {
            if (raw.Length == 0 || now - last < IdleMilliseconds) return null;
            ScanPacket packet = new ScanPacket { Raw = raw.ToString(), Typed = typed.ToString(), Window = window, AllowOwnTest = ownTest, Generation = generation };
            Clear();
            if (MarkingCode.LooksLikeCode(packet.Raw))
            {
                suffixUntil = now + 700; suffixWindow = packet.Window;
            }
            return packet;
        }
        public static bool CanReplace(string fieldValue, ScanPacket packet)
        {
            if (packet != null && packet.DirectInput) return fieldValue == "";
            return packet != null && packet.Typed != null && packet.Typed.Length >= 18 && fieldValue != null && fieldValue.EndsWith(packet.Typed, StringComparison.Ordinal);
        }
    }
}
