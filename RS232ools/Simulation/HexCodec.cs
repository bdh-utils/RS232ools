using System;
using System.Globalization;
using System.Text;

namespace RS232ools.Simulation
{
    /// <summary>
    /// Converts between a byte-faithful string and its hexadecimal text
    /// representation (space-separated uppercase byte pairs, e.g. "48 65 6C").
    /// Latin-1 is used so every byte 0x00-0xFF maps 1:1 to a character.
    /// </summary>
    public static class HexCodec
    {
        private static readonly Encoding ByteEncoding = Encoding.Latin1;

        /// <summary>Encodes a string's bytes as space-separated hex pairs.</summary>
        public static string Encode(string text)
        {
            byte[] bytes = ByteEncoding.GetBytes(text ?? string.Empty);
            var sb = new StringBuilder(bytes.Length * 3);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Decodes hex text (whitespace between pairs is ignored) back into the
        /// original string. Returns false for an odd digit count or non-hex
        /// characters.
        /// </summary>
        public static bool TryDecode(string hex, out string text)
        {
            text = string.Empty;
            if (hex is null) return false;

            // Strip all whitespace so "48 65" and "4865" both work.
            var digits = new StringBuilder(hex.Length);
            foreach (char c in hex)
            {
                if (!char.IsWhiteSpace(c)) digits.Append(c);
            }

            if (digits.Length % 2 != 0) return false;

            var bytes = new byte[digits.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                if (!byte.TryParse(digits.ToString(i * 2, 2), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out bytes[i]))
                {
                    return false;
                }
            }

            text = ByteEncoding.GetString(bytes);
            return true;
        }
    }
}
