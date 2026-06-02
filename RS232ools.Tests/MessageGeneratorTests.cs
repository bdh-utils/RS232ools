using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    public class MessageGeneratorTests
    {
        private static MessageGenerator Seeded(DateTime? clock = null)
            => new(new Random(12345), clock is null ? null : () => clock.Value);

        [Fact]
        public void GenerateValue_FixedText_ReturnsTheText()
        {
            var gen = Seeded();
            var field = new FieldDefinition { Type = FieldType.FixedText, FixedValue = "GPGGA" };
            Assert.Equal("GPGGA", gen.GenerateValue(field));
        }

        [Fact]
        public void GenerateValue_RandomInteger_StaysWithinInclusiveRange()
        {
            var gen = Seeded();
            var field = new FieldDefinition { Type = FieldType.RandomInteger, Min = 3, Max = 7 };

            for (int i = 0; i < 1000; i++)
            {
                int v = int.Parse(gen.GenerateValue(field), CultureInfo.InvariantCulture);
                Assert.InRange(v, 3, 7);
            }
        }

        [Fact]
        public void GenerateValue_RandomInteger_EqualMinMax_IsConstant()
        {
            var gen = Seeded();
            var field = new FieldDefinition { Type = FieldType.RandomInteger, Min = 5, Max = 5 };
            Assert.Equal("5", gen.GenerateValue(field));
        }

        [Fact]
        public void GenerateValue_RandomDecimal_HasRequestedPrecision_AndRange()
        {
            var gen = Seeded();
            var field = new FieldDefinition { Type = FieldType.RandomDecimal, Min = 0, Max = 10, Precision = 3 };

            for (int i = 0; i < 500; i++)
            {
                string s = gen.GenerateValue(field);
                Assert.Matches(new Regex(@"^\d+\.\d{3}$"), s);
                double v = double.Parse(s, CultureInfo.InvariantCulture);
                Assert.InRange(v, 0.0, 10.0);
            }
        }

        [Fact]
        public void GenerateValue_IncrementingCounter_AdvancesFromMin()
        {
            var gen = Seeded();
            var field = new FieldDefinition { Type = FieldType.IncrementingCounter, Min = 10 };

            Assert.Equal("10", gen.GenerateValue(field));
            Assert.Equal("11", gen.GenerateValue(field));
            Assert.Equal("12", gen.GenerateValue(field));
        }

        [Fact]
        public void GenerateValue_Timestamp_UsesClockAndFormat()
        {
            var when = new DateTime(2026, 6, 2, 13, 35, 19, 250);
            var gen = Seeded(when);
            var field = new FieldDefinition { Type = FieldType.Timestamp, TimestampFormat = "HHmmss.ff" };
            Assert.Equal("133519.25", gen.GenerateValue(field));
        }

        [Fact]
        public void Generate_Csv_JoinsWithDelimiter()
        {
            var gen = Seeded();
            var format = new MessageFormat
            {
                Kind = MessageFormatKind.Csv,
                Delimiter = ",",
                Fields = new List<FieldDefinition>
                {
                    new() { Type = FieldType.FixedText, FixedValue = "A" },
                    new() { Type = FieldType.FixedText, FixedValue = "B" },
                    new() { Type = FieldType.IncrementingCounter, Min = 0 },
                },
            };

            Assert.Equal("A,B,0", gen.Generate(format));
        }

        [Fact]
        public void Generate_Csv_RespectsCustomDelimiter()
        {
            var gen = Seeded();
            var format = new MessageFormat
            {
                Kind = MessageFormatKind.Csv,
                Delimiter = ";",
                Fields = new List<FieldDefinition>
                {
                    new() { Type = FieldType.FixedText, FixedValue = "X" },
                    new() { Type = FieldType.FixedText, FixedValue = "Y" },
                },
            };

            Assert.Equal("X;Y", gen.Generate(format));
        }

        [Fact]
        public void Generate_Nmea_FramesWithDollarAndChecksum()
        {
            var gen = Seeded();
            var format = new MessageFormat
            {
                Kind = MessageFormatKind.Nmea,
                Delimiter = ",",
                IncludeChecksum = true,
                Fields = new List<FieldDefinition>
                {
                    new() { Type = FieldType.FixedText, FixedValue = "GPTST" },
                    new() { Type = FieldType.FixedText, FixedValue = "1" },
                },
            };

            string expectedChecksum = NmeaChecksum.Compute("GPTST,1");
            Assert.Equal($"$GPTST,1*{expectedChecksum}", gen.Generate(format));
        }

        [Fact]
        public void Generate_Nmea_WithoutChecksum_OmitsStar()
        {
            var gen = Seeded();
            var format = new MessageFormat
            {
                Kind = MessageFormatKind.Nmea,
                IncludeChecksum = false,
                Fields = new List<FieldDefinition>
                {
                    new() { Type = FieldType.FixedText, FixedValue = "GPTST" },
                },
            };

            Assert.Equal("$GPTST", gen.Generate(format));
        }

        [Fact]
        public void Generate_NullFormat_Throws()
        {
            var gen = Seeded();
            Assert.Throws<ArgumentNullException>(() => gen.Generate(null!));
        }
    }
}
