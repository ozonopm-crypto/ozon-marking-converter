using System;

namespace OzonMarkingDesktop
{
    // RU: Состояние берётся из порядка событий, а не из запаздывающего AsyncKeyState.
    // EN: Decode event-ordered state instead of querying asynchronous state inside a hook.
    public sealed class OrderedKeyboardState
    {
        private readonly bool[] keys = new bool[256];
        public bool CapsLock { get; private set; }
        public bool NumLock { get; private set; }
        public bool Shift { get { return keys[0xa0] || keys[0xa1]; } }
        public bool Control { get { return keys[0xa2] || keys[0xa3]; } }
        public bool Alt { get { return keys[0xa4] || keys[0xa5]; } }
        public bool Windows { get { return keys[0x5b] || keys[0x5c]; } }
        public void Seed(int key, bool down) { if (key >= 0 && key < keys.Length) keys[key] = down; }
        public void SeedCaps(bool on) { CapsLock = on; }
        public void SeedNumLock(bool on) { NumLock = on; }
        public void Update(int key, bool down, uint scan, bool extended)
        {
            if (key == 0x10) key = scan == 0x36 ? 0xa1 : 0xa0;
            if (key == 0x11) key = extended ? 0xa3 : 0xa2;
            if (key == 0x12) key = extended ? 0xa5 : 0xa4;
            if (key < 0 || key >= keys.Length) return;
            if (key == 0x14 && down && !keys[key]) CapsLock = !CapsLock;
            if (key == 0x90 && down && !keys[key]) NumLock = !NumLock;
            keys[key] = down;
        }
        public byte[] Snapshot(bool scanner)
        {
            byte[] state = new byte[256];
            for (int i = 0; i < state.Length; i++) if (keys[i]) state[i] = 0x80;
            state[0x10] = Shift ? (byte)0x80 : (byte)0;
            state[0x11] = Control ? (byte)0x80 : (byte)0;
            state[0x12] = Alt ? (byte)0x80 : (byte)0;
            state[0x14] = !scanner && CapsLock ? (byte)1 : (byte)0;
            state[0x90] = NumLock ? (byte)1 : (byte)0;
            return state;
        }
        public static char PhysicalGlyph(char value)
        {
            if (value >= 'A' && value <= 'Z') return (char)(value + 32);
            int digit = ")!@#$%^&*(".IndexOf(value);
            if (digit >= 0) return (char)('0' + digit);
            switch (value)
            {
                case ':': return ';'; case '+': return '='; case '<': return ',';
                case '_': return '-'; case '>': return '.'; case '?': return '/';
                case '~': return (char)96; case '{': return '['; case '|': return '\\';
                case '}': return ']'; case '"': return '\'';
                default: return value;
            }
        }
        public char DecodeUs(int key)
        {
            if (key >= 0x41 && key <= 0x5a) return (char)(Shift ? key : key + 32);
            if (key >= 0x30 && key <= 0x39) return Shift ? ")!@#$%^&*("[key - 0x30] : (char)key;
            if (key >= 0x60 && key <= 0x69) return (char)('0' + key - 0x60);
            switch (key)
            {
                case 0x20: return ' ';
                case 0xba: return Shift ? ':' : ';';
                case 0xbb: return Shift ? '+' : '=';
                case 0xbc: return Shift ? '<' : ',';
                case 0xbd: return Shift ? '_' : '-';
                case 0xbe: return Shift ? '>' : '.';
                case 0xbf: return Shift ? '?' : '/';
                case 0xc0: return Shift ? '~' : (char)96;
                case 0xdb: return Shift ? '{' : '[';
                case 0xdc: return Shift ? '|' : '\\';
                case 0xdd: return Shift ? '}' : ']';
                case 0xde: return Shift ? '"' : '\'';
                case 0x6a: return '*';
                case 0x6b: return '+';
                case 0x6d: return '-';
                case 0x6e: return '.';
                case 0x6f: return '/';
                default: return '\0';
            }
        }
    }
}
