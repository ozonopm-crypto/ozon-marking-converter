using System;
using OzonMarkingDesktop;

internal static class FieldValueTests
{
    private static int count;
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        count++;
    }

    public static void Main()
    {
        string raw = MarkingCode.DemoRaw;
        string prepared = MarkingCode.Normalize(raw).Code;
        ScanPacket usb = new ScanPacket { Raw = raw, Typed = raw };
        ScanPacket com = new ScanPacket { Raw = raw, Typed = "", DirectInput = true };

        // RU: Регрессия: ValuePattern пустой, хотя TextPattern содержит весь скан.
        // EN: Regression: ValuePattern is empty while TextPattern exposes the full scan.
        FieldValueSelection selected = FieldValuePolicy.Select("", raw, null);
        Check(raw.Length == 83, "synthetic regression reproduces the 83-character scan");
        Check(selected != null && !selected.Conflict && selected.Source == "TextPattern" && selected.Value == raw,
            "nonempty focused text is selected over an empty value");
        ReplacementPlan plan = ReplacementPlan.Resolve(selected.Value, usb, true);
        Check(plan != null && plan.Expected == prepared, "selected text permits the verified USB separator repair");
        Check(ReplacementPlan.Resolve(selected.Value, com, true) == null, "existing text cannot qualify as an empty COM target");

        selected = FieldValuePolicy.Select("", "unrelated visible text", null);
        Check(selected != null && selected.Value == "unrelated visible text", "unrelated visible content is preserved by selection");
        Check(ReplacementPlan.Resolve(selected.Value, usb, true) == null, "USB cannot replace unrelated visible text");
        Check(ReplacementPlan.Resolve(selected.Value, com, true) == null, "COM cannot erase unrelated visible text behind an empty value");

        // RU: Несогласованные непустые источники не позволяют выбрать подходящий наугад.
        // EN: Conflicting nonempty sources cannot be resolved by choosing whichever matches.
        selected = FieldValuePolicy.Select(raw, raw.Replace("TEST123", "TAST123"), null);
        Check(selected != null && selected.Conflict && selected.Value == null && selected.Source == "conflict",
            "different nonempty payloads produce an explicit conflict");
        selected = FieldValuePolicy.Select(raw, prepared, "TextPattern");
        Check(selected != null && selected.Conflict && selected.Value == null, "pinning does not bypass a nonempty conflict");
        selected = FieldValuePolicy.Select(" ", raw, null);
        Check(selected != null && selected.Conflict, "whitespace remains content rather than being silently treated as empty");

        selected = FieldValuePolicy.Select(raw, raw, null);
        Check(selected != null && selected.Source == "ValuePattern" && selected.Value == raw && !selected.Conflict,
            "equal nonempty sources choose ValuePattern");
        selected = FieldValuePolicy.Select(raw, "", null);
        Check(selected != null && selected.Source == "ValuePattern" && selected.Value == raw, "nonempty value wins over empty text");
        selected = FieldValuePolicy.Select(raw, null, null);
        Check(selected != null && selected.Source == "ValuePattern" && selected.Value == raw, "unsupported text does not hide a supported value");
        selected = FieldValuePolicy.Select(null, raw, null);
        Check(selected != null && selected.Source == "TextPattern" && selected.Value == raw, "unsupported value does not hide supported text");
        Check(FieldValuePolicy.Select(null, null, null) == null, "two unsupported sources are unavailable rather than an empty field");

        // RU: Повторное чтение не переключается на другой источник после записи.
        // EN: Verification never switches sources after writing.
        selected = FieldValuePolicy.Select("", prepared, "TextPattern");
        Check(selected != null && selected.Source == "TextPattern" && plan.Matches(selected.Value), "written separators are verified through the pinned TextPattern");
        selected = FieldValuePolicy.Select(raw, "", "TextPattern");
        Check(selected != null && selected.Source == "TextPattern" && selected.Value == "" && !plan.Matches(selected.Value),
            "empty pinned text cannot fall back to another nonempty representation");
        selected = FieldValuePolicy.Select("", raw, "ValuePattern");
        Check(selected != null && selected.Source == "ValuePattern" && selected.Value == "", "empty pinned value remains empty");
        Check(FieldValuePolicy.HasContent("", raw), "a newly populated TextPattern is content despite an empty pinned value");
        Check(!(!FieldValuePolicy.HasContent("", raw) && ScanBuffer.CanReplace(selected.Value, com)),
            "COM prewrite guard rejects newly visible text without switching the pinned reader");
        Check(FieldValuePolicy.Select(raw, null, "TextPattern") == null, "unavailable pinned text does not switch to value");
        Check(FieldValuePolicy.Select(null, raw, "ValuePattern") == null, "unavailable pinned value does not switch to text");
        Check(FieldValuePolicy.Select(raw, raw, "unknown") == null, "unknown pinned sources fail closed");
        selected = FieldValuePolicy.Select(raw, raw, "TextPattern");
        Check(selected != null && selected.Source == "TextPattern", "equal sources retain an explicit TextPattern pin");

        selected = FieldValuePolicy.Select("", "", null);
        Check(selected != null && selected.Source == "ValuePattern" && selected.Value == "" && !selected.Conflict,
            "two genuinely empty sources select supported ValuePattern");
        Check(ReplacementPlan.Resolve(selected.Value, com, true) != null, "a genuinely empty field still accepts COM input");
        selected = FieldValuePolicy.Select(null, "", null);
        Check(selected != null && selected.Source == "TextPattern" && selected.Value == "", "supported empty text remains distinguishable from unavailable");
        selected = FieldValuePolicy.Select("", null, null);
        Check(selected != null && selected.Source == "ValuePattern" && selected.Value == "", "supported empty value remains distinguishable from unavailable");
        Check(!FieldValuePolicy.HasContent(null, null) && FieldValuePolicy.Select(null, null, null) == null,
            "lack of reported content does not make two unsupported sources eligible");
        Check(!FieldValuePolicy.HasContent("", "") && FieldValuePolicy.Select("", "", null) != null,
            "two supported empty representations remain a genuinely empty candidate");
        Check(FieldValuePolicy.HasContent(raw, null), "content is detected when only ValuePattern is supported");
        Check(FieldValuePolicy.HasContent(null, " "), "whitespace in either source counts as existing content");

        Console.WriteLine("PASS: " + count + " focused-field source, empty-value regression, conflict and pinned-readback checks.");
    }
}
