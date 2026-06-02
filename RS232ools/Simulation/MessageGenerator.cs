using System;
using System.Collections.Generic;
using System.Globalization;

namespace RS232ools.Simulation
{
    /// <summary>
    /// Produces a message string from a <see cref="MessageFormat"/>, generating a
    /// random (or counter/timestamp/fixed) value per field and assembling them
    /// into either a CSV line or a framed NMEA sentence.
    ///
    /// The random source and clock are injectable so generation is deterministic
    /// and unit-testable. Incrementing-counter state is held per field instance,
    /// so reusing the same <see cref="MessageGenerator"/> across calls advances
    /// the counters.
    /// </summary>
    public sealed class MessageGenerator
    {
        private readonly Random _rng;
        private readonly Func<DateTime> _clock;
        private readonly Dictionary<FieldDefinition, long> _counters = new();

        public MessageGenerator(Random? rng = null, Func<DateTime>? clock = null)
        {
            _rng = rng ?? new Random();
            _clock = clock ?? (() => DateTime.Now);
        }

        /// <summary>Generates one message (no trailing newline) for the format.</summary>
        public string Generate(MessageFormat format)
        {
            if (format is null) throw new ArgumentNullException(nameof(format));

            var parts = new List<string>(format.Fields.Count);
            foreach (var field in format.Fields)
            {
                parts.Add(GenerateValue(field));
            }

            switch (format.Kind)
            {
                case MessageFormatKind.Nmea:
                {
                    string payload = string.Join(SeparatedDelimiter(format), parts);
                    string sentence = "$" + payload;
                    if (format.IncludeChecksum)
                    {
                        sentence += "*" + NmeaChecksum.Compute(payload);
                    }
                    return sentence;
                }

                case MessageFormatKind.Plain:
                    // Simple string with no separators at all.
                    return string.Concat(parts);

                case MessageFormatKind.Hex:
                    // Join with the delimiter as typed (may be empty) then encode.
                    return HexCodec.Encode(string.Join(format.Delimiter ?? string.Empty, parts));

                case MessageFormatKind.Csv:
                default:
                    return string.Join(SeparatedDelimiter(format), parts);
            }
        }

        // CSV and NMEA are separator-based, so an empty delimiter falls back to
        // a comma. Plain and Hex handle the delimiter themselves.
        private static string SeparatedDelimiter(MessageFormat format)
            => string.IsNullOrEmpty(format.Delimiter) ? "," : format.Delimiter;

        /// <summary>Generates the value for a single field.</summary>
        public string GenerateValue(FieldDefinition field)
        {
            if (field is null) throw new ArgumentNullException(nameof(field));

            switch (field.Type)
            {
                case FieldType.FixedText:
                    return field.FixedValue ?? string.Empty;

                case FieldType.RandomInteger:
                {
                    int min = (int)Math.Round(field.Min);
                    int max = (int)Math.Round(field.Max);
                    if (max < min) (min, max) = (max, min);
                    // Random.Next's upper bound is exclusive; make Max inclusive.
                    int upper = max == int.MaxValue ? max : max + 1;
                    return _rng.Next(min, upper).ToString(CultureInfo.InvariantCulture);
                }

                case FieldType.RandomDecimal:
                {
                    double min = field.Min;
                    double max = field.Max;
                    if (max < min) (min, max) = (max, min);
                    int precision = Math.Max(0, field.Precision);
                    double value = min + (_rng.NextDouble() * (max - min));
                    return Math.Round(value, precision)
                        .ToString("F" + precision, CultureInfo.InvariantCulture);
                }

                case FieldType.IncrementingCounter:
                {
                    long start = (long)Math.Round(field.Min);
                    if (!_counters.TryGetValue(field, out long current))
                    {
                        current = start;
                    }
                    _counters[field] = current + 1;
                    return current.ToString(CultureInfo.InvariantCulture);
                }

                case FieldType.Timestamp:
                {
                    string fmt = string.IsNullOrEmpty(field.TimestampFormat)
                        ? "HHmmss.ff"
                        : field.TimestampFormat;
                    return _clock().ToString(fmt, CultureInfo.InvariantCulture);
                }

                default:
                    return string.Empty;
            }
        }
    }
}
