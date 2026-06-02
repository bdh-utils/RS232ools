using System;
using System.Collections.Generic;
using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    public class MessageParserTests
    {
        private static MessageFormat Csv(string delimiter = ",")
            => new() { Kind = MessageFormatKind.Csv, Delimiter = delimiter };

        private static MessageFormat Nmea()
            => new() { Kind = MessageFormatKind.Nmea, Delimiter = "," };

        [Fact]
        public void Parse_Csv_SplitsValues()
        {
            var result = MessageParser.Parse(Csv(), "1,2,3");
            Assert.True(result.Success);
            Assert.Equal(new[] { "1", "2", "3" }, result.Values);
            Assert.Null(result.ChecksumValid);
        }

        [Fact]
        public void Parse_Csv_RespectsCustomDelimiter()
        {
            var result = MessageParser.Parse(Csv(";"), "a;b;c");
            Assert.Equal(new[] { "a", "b", "c" }, result.Values);
        }

        [Fact]
        public void Parse_Csv_TrimsSurroundingWhitespaceAndNewline()
        {
            var result = MessageParser.Parse(Csv(), "  10,20\r\n");
            Assert.Equal(new[] { "10", "20" }, result.Values);
        }

        [Fact]
        public void Parse_Nmea_ValidChecksum_IsReportedValid()
        {
            const string payload = "GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,";
            var result = MessageParser.Parse(Nmea(), $"${payload}*47\r\n");

            Assert.True(result.Success);
            Assert.True(result.ChecksumValid);
            Assert.Equal("GPGGA", result.Values[0]);
            Assert.Equal("123519", result.Values[1]);
        }

        [Fact]
        public void Parse_Nmea_WrongChecksum_IsReportedInvalid()
        {
            var result = MessageParser.Parse(Nmea(), "$GPGGA,123519*00");
            Assert.True(result.Success);
            Assert.False(result.ChecksumValid);
        }

        [Fact]
        public void Parse_Nmea_NoChecksum_LeavesValidityNull()
        {
            var result = MessageParser.Parse(Nmea(), "$GPGGA,123519");
            Assert.True(result.Success);
            Assert.Null(result.ChecksumValid);
            Assert.Equal(new[] { "GPGGA", "123519" }, result.Values);
        }

        [Fact]
        public void Parse_Nmea_MissingDollar_Fails()
        {
            var result = MessageParser.Parse(Nmea(), "GPGGA,1,2*47");
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Parse_EmptyLine_Fails()
        {
            Assert.False(MessageParser.Parse(Csv(), "   ").Success);
        }

        [Fact]
        public void Parse_NullLine_Fails()
        {
            Assert.False(MessageParser.Parse(Csv(), null!).Success);
        }

        [Fact]
        public void Parse_NullFormat_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MessageParser.Parse(null!, "1,2"));
        }

        [Fact]
        public void GenerateThenParse_Nmea_RoundTripsValuesAndChecksum()
        {
            var format = new MessageFormat
            {
                Kind = MessageFormatKind.Nmea,
                Delimiter = ",",
                IncludeChecksum = true,
                Fields = new List<FieldDefinition>
                {
                    new() { Type = FieldType.FixedText, FixedValue = "GPTST" },
                    new() { Type = FieldType.FixedText, FixedValue = "42" },
                    new() { Type = FieldType.IncrementingCounter, Min = 7 },
                },
            };
            var gen = new MessageGenerator(new Random(1));

            string sentence = gen.Generate(format);
            var parsed = MessageParser.Parse(format, sentence);

            Assert.True(parsed.Success);
            Assert.True(parsed.ChecksumValid);
            Assert.Equal(new[] { "GPTST", "42", "7" }, parsed.Values);
        }
    }
}
