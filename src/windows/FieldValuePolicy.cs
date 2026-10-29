using System;

namespace OzonMarkingDesktop
{
    public sealed class FieldValueSelection
    {
        public string Value;
        public string Source;
        public bool Conflict;
    }

    public static class FieldValuePolicy
    {
        public static bool HasContent(string valuePattern, string textPattern)
        {
            // RU: Пустота для COM проверяется по обоим источникам, даже при закреплённом чтении.
            // EN: COM emptiness checks both representations even when readback is pinned.
            return !String.IsNullOrEmpty(valuePattern) || !String.IsNullOrEmpty(textPattern);
        }

        public static FieldValueSelection Select(string valuePattern, string textPattern, string preferredSource)
        {
            // RU: Пустая строка поддерживается; null означает, что источник недоступен.
            // EN: An empty string is supported; null means the source is unavailable.
            if (!String.IsNullOrEmpty(valuePattern) && !String.IsNullOrEmpty(textPattern) &&
                !String.Equals(valuePattern, textPattern, StringComparison.Ordinal))
                return new FieldValueSelection { Value = null, Source = "conflict", Conflict = true };

            // RU: Проверка записи остаётся на выбранном источнике, даже если он стал пустым.
            // EN: Readback stays on the selected source, even when that source becomes empty.
            if (preferredSource != null)
            {
                if (preferredSource == "ValuePattern")
                    return valuePattern == null ? null : Selection(valuePattern, preferredSource);
                if (preferredSource == "TextPattern")
                    return textPattern == null ? null : Selection(textPattern, preferredSource);
                return null;
            }

            if (!String.IsNullOrEmpty(valuePattern)) return Selection(valuePattern, "ValuePattern");
            if (!String.IsNullOrEmpty(textPattern)) return Selection(textPattern, "TextPattern");
            if (valuePattern != null) return Selection(valuePattern, "ValuePattern");
            if (textPattern != null) return Selection(textPattern, "TextPattern");
            return null;
        }

        private static FieldValueSelection Selection(string value, string source)
        {
            return new FieldValueSelection { Value = value, Source = source };
        }
    }
}
