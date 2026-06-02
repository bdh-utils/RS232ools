using System;
using System.Collections.Generic;
using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    /// <summary>Covers the no-separator (Plain) and hexadecimal (Hex) formats.</summary>
    public class PlainAndHexFormatTests
    {
        private static MessageFormat Make(MessageFormatKind kind, string delimiter, params FieldDefinition[] fields)
            => new() { Kind = kind, Delimiter = delimiter, Fields = new List<FieldDefinition>(fields) };

        private static FieldDefinition Fixed(string value)
            => new() { Type = FieldType.FixedText, FixedValue = value };

        // ---- Plain --------------------------------------------------------

        [Fact]
        public void Generate_Plain_ConcatenatesWithNoSeparator()
        {
            var gen = new MessageGenerator(new Random(1));
            var format = Make(MessageFormatKind.Plain, ",", Fixed("AB"), Fixed("CD"), Fixed("EF"));
            Assert.Equal("ABCDEF", gen.Generate(format));
        }

        [Fact]
        public void Generate_Plain_IgnoresTheDelimiterEntirely()
        {
            var gen = new MessageGenerator(new Random(1));
            var format = Make(MessageFormatKind.Plain, ";", Fixed("1"), Fixed("2"));
            Assert.Equal("12", gen.Generate(format));
        }

        [Fact]
        public void Parse_Plain_ReturnsWholeLineAsSingleValue()
        {
            var format = Make(MessageFormatKind.Plain, ",");
            var parsed = MessageParser.Parse(format, "ABCDEF");
            Assert.True(parsed.Success);
            Assert.Equal(new[] { "ABCDEF" }, parsed.Values);
            Assert.Null(parsed.ChecksumValid);
        }

        // ---- Hex ----------------------------------------------------------

        [Fact]
        public void Generate_Hex_EncodesPayloadBytesAsHexPairs()
        {
            var gen = new MessageGenerator(new Random(1));
            // "Hi" with no delimiter -> "48 69"
            var format = Make(MessageFormatKind.Hex, "", Fixed("H"), Fixed("i"));
            Assert.Equal("48 69", gen.Generate(format));
        }

        [Fact]
        public void Generate_Hex_WithDelimiter_IncludesTheDelimiterByte()
        {
            var gen = new MessageGenerator(new Random(1));
            // "A,B" -> 0x41 0x2C 0x42
            var format = Make(MessageFormatKind.Hex, ",", Fixed("A"), Fixed("B"));
            Assert.Equal("41 2C 42", gen.Generate(format));
        }

        [Fact]
        public void Parse_Hex_DecodesAndSplitsByDelimiter()
        {
            var format = Make(MessageFormatKind.Hex, ",");
            var parsed = MessageParser.Parse(format, "41 2C 42");
            Assert.True(parsed.Success);
            Assert.Equal(new[] { "A", "B" }, parsed.Values);
        }

        [Fact]
        public void Parse_Hex_NoDelimiter_ReturnsSingleDecodedValue()
        {
            var format = Make(MessageFormatKind.Hex, "");
            var parsed = MessageParser.Parse(format, "48 69");
            Assert.True(parsed.Success);
            Assert.Equal(new[] { "Hi" }, parsed.Values);
        }

        [Fact]
        public void Parse_Hex_InvalidHex_Fails()
        {
            var format = Make(MessageFormatKind.Hex, "");
            var parsed = MessageParser.Parse(format, "4G 21");
            Assert.False(parsed.Success);
            Assert.NotNull(parsed.Error);
        }

        [Fact]
        public void GenerateThenParse_Hex_RoundTripsValues()
        {
            var gen = new MessageGenerator(new Random(2));
            var format = Make(MessageFormatKind.Hex, ",",
                Fixed("GPS"),
                new FieldDefinition { Type = FieldType.IncrementingCounter, Min = 5 });

            string hex = gen.Generate(format);
            var parsed = MessageParser.Parse(format, hex);

            Assert.True(parsed.Success);
            Assert.Equal(new[] { "GPS", "5" }, parsed.Values);
        }
    }
}
