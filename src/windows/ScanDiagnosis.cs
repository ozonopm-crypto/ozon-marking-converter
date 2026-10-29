using System;
using System.Text;

namespace OzonMarkingDesktop
{
    public static class ScanDiagnosis
    {
        public static string Describe(ScanPacket packet, string field, string reader, ReplacementPlan plan)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("by PEI · RU / EN");
            text.AppendLine("Чтение / Reader: " + reader);
            text.AppendLine("Клавиши / Captured: " + (packet.Raw ?? "").Length);
            text.AppendLine("Напечатано / Typed: " + (packet.Typed ?? "").Length);
            text.AppendLine("Поле / Field: " + (field == null ? "недоступно / unavailable" : field.Length.ToString()));
            text.AppendLine("Коррекция языка / Keyboard repair: " + packet.KeyboardNeedsRepair);
            text.AppendLine("Совпадение текста / Exact typed match: " + (field == packet.Typed));
            text.AppendLine("Совпадение клавиш / Physical-key correlation: " + MarkingCode.SamePhysicalPayload(packet.Raw, field));
            text.AppendLine("Формат поля / Field format: " + MarkingCode.Normalize(field).Ok);
            text.AppendLine("Источник / Source: " + (plan == null ? "отказ / rejected" : plan.UsesField ? "текст поля / visible field" : "клавиши / key events"));
            text.AppendLine("Сам код и символы кода не включены. / No code contents are included.");
            return text.ToString();
        }
    }
}
