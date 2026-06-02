using System;
using System.Globalization;

namespace RS232ools.Simulation
{
    /// <summary>
    /// NMEA 0183 checksum: the XOR of every character of the sentence payload
    /// (everything between the leading <c>$</c> and the <c>*</c>), expressed as
    /// two uppercase hex digits.
    /// </summary>
    public static class NmeaChecksum
    {
        /// <summary>Computes the two-hex-digit checksum for a sentence payload.</summary>
        public static string Compute(string payload)
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));

            byte checksum = 0;
            foreach (char c in payload)
            {
                checksum ^= (byte)c;
            }

            return checksum.ToString("X2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// True if <paramref name="expectedHex"/> matches the computed checksum
        /// for <paramref name="payload"/> (case-insensitive, whitespace-tolerant).
        /// </summary>
        public static bool Validate(string payload, string? expectedHex)
            => expectedHex is not null
               && Compute(payload).Equals(expectedHex.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
