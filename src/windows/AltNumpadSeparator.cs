using System;

namespace OzonMarkingDesktop
{
    // RU: Вызывается только для Alt-кода на известной границе разделителя маркировки.
    // EN: The caller activates this collector only at a known marking-code separator boundary.
    public sealed class AltNumpadSeparator
    {
        private string digits = "";
        private bool invalid;
        public bool Active { get; private set; }

        public static bool TryDigit(uint key, uint scan, bool extended, out int digit)
        {
            if (key >= 0x60 && key <= 0x69) { digit = (int)(key - 0x60); return true; }
            digit = -1;
            // RU: При выключенном Num Lock виртуальные клавиши меняются, скан-коды остаются.
            // EN: With Num Lock off the virtual keys change, but physical numpad scan codes remain.
            if (extended) return false;
            switch (scan)
            {
                case 0x47: digit = 7; break;
                case 0x48: digit = 8; break;
                case 0x49: digit = 9; break;
                case 0x4b: digit = 4; break;
                case 0x4c: digit = 5; break;
                case 0x4d: digit = 6; break;
                case 0x4f: digit = 1; break;
                case 0x50: digit = 2; break;
                case 0x51: digit = 3; break;
                case 0x52: digit = 0; break;
            }
            return digit >= 0;
        }

        public void Start() { digits = ""; invalid = false; Active = true; }

        public bool Feed(uint key, uint scan, bool extended, bool up)
        {
            if (!Active) return false;
            if (up) return true;
            int digit;
            if (!TryDigit(key, scan, extended, out digit) || digits.Length >= 5)
            { invalid = true; return false; }
            digits += (char)('0' + digit);
            return !invalid;
        }

        public bool Finish()
        {
            bool valid = Active && !invalid && digits.Length > 0;
            int number = 0;
            if (valid) foreach (char digit in digits) number = number * 10 + digit - '0';
            bool separator = valid && number == 29;
            Cancel();
            return separator;
        }

        public void Cancel() { digits = ""; invalid = false; Active = false; }
    }
}
