using System;
using System.Collections.Generic;

namespace RS232ools.Simulation
{
    /// <summary>The outcome of parsing one received line against a format.</summary>
    public sealed class ParsedMessage
    {
        /// <summary>False when the line did not match the expected shape.</summary>
        public bool Success { get; init; }

        /// <summary>A human-readable reason when <see cref="Success"/> is false.</summary>
        public string? Error { get; init; }

        /// <summary>The field values in order.</summary>
        public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();

        /// <summary>
        /// For NMEA with a checksum present: whether it matched. Null when the
        /// format is CSV or no checksum was present to verify.
        /// </summary>
        public bool? ChecksumValid { get; init; }

        internal static ParsedMessage Fail(string error)
            => new() { Success = false, Error = error };
    }

    /// <summary>
    /// Parses a received line into field values using the same
    /// <see cref="MessageFormat"/> that generates them, so a customised format
    /// round-trips. For NMEA it strips the <c>$</c>/<c>*CS</c> framing and
    /// verifies the checksum; for CSV it simply splits on the delimiter.
    /// </summary>
    public static class MessageParser
    {
        public static ParsedMessage Parse(MessageFormat format, string line)
        {
            if (format is null) throw new ArgumentNullException(nameof(format));
            if (line is null) return ParsedMessage.Fail("Line was null.");

            string trimmed = line.Trim();
            if (trimmed.Length == 0) return ParsedMessage.Fail("Line was empty.");

            return format.Kind == MessageFormatKind.Nmea
                ? ParseNmea(format, trimmed)
                : ParseCsv(format, trimmed);
        }

        private static ParsedMessage ParseCsv(MessageFormat format, string line)
        {
            string[] values = line.Split(new[] { format.Delimiter }, StringSplitOptions.None);
            return new ParsedMessage { Success = true, Values = values, ChecksumValid = null };
        }

        private static ParsedMessage ParseNmea(MessageFormat format, string line)
        {
            if (!line.StartsWith("$", StringComparison.Ordinal))
            {
                return ParsedMessage.Fail("NMEA sentence must start with '$'.");
            }

            string body = line.Substring(1);

            string payload;
            bool? checksumValid = null;

            int star = body.LastIndexOf('*');
            if (star >= 0)
            {
                payload = body.Substring(0, star);
                string expected = body.Substring(star + 1);
                checksumValid = NmeaChecksum.Validate(payload, expected);
            }
            else
            {
                payload = body;
            }

            string[] values = payload.Split(new[] { format.Delimiter }, StringSplitOptions.None);
            return new ParsedMessage { Success = true, Values = values, ChecksumValid = checksumValid };
        }
    }
}
