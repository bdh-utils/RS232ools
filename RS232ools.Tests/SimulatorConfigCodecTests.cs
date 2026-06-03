using System;
using System.Collections.Generic;
using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    public class SimulatorConfigCodecTests
    {
        private static SimulatorConfig Sample() => new()
        {
            Kind = MessageFormatKind.Nmea,
            Delimiter = ",",
            IncludeChecksum = true,
            StreamIntervalMs = 500,
            Fields = new List<FieldDefinition>
            {
                new() { Name = "talker", Type = FieldType.FixedText, FixedValue = "GPGGA" },
                new() { Name = "seq", Type = FieldType.IncrementingCounter, Min = 1 },
                new() { Name = "temp", Type = FieldType.RandomDecimal, Min = 15, Max = 30, Precision = 3 },
            },
        };

        [Fact]
        public void RoundTrip_PreservesFormatAndFields()
        {
            string json = SimulatorConfigCodec.Serialize(Sample());
            var back = SimulatorConfigCodec.Deserialize(json);

            Assert.Equal(MessageFormatKind.Nmea, back.Kind);
            Assert.Equal(",", back.Delimiter);
            Assert.True(back.IncludeChecksum);
            Assert.Equal(500, back.StreamIntervalMs);

            Assert.Equal(3, back.Fields.Count);

            Assert.Equal("talker", back.Fields[0].Name);
            Assert.Equal(FieldType.FixedText, back.Fields[0].Type);
            Assert.Equal("GPGGA", back.Fields[0].FixedValue);

            Assert.Equal(FieldType.IncrementingCounter, back.Fields[1].Type);
            Assert.Equal(1, back.Fields[1].Min);

            Assert.Equal(FieldType.RandomDecimal, back.Fields[2].Type);
            Assert.Equal(30, back.Fields[2].Max);
            Assert.Equal(3, back.Fields[2].Precision);
        }

        [Fact]
        public void RoundTrip_RegeneratesAnEquivalentMessage()
        {
            var original = Sample();
            var format = new MessageFormat
            {
                Kind = original.Kind,
                Delimiter = original.Delimiter,
                IncludeChecksum = original.IncludeChecksum,
                Fields = original.Fields,
            };
            string before = new MessageGenerator(new Random(1)).Generate(format);

            var loaded = SimulatorConfigCodec.Deserialize(SimulatorConfigCodec.Serialize(original));
            var loadedFormat = new MessageFormat
            {
                Kind = loaded.Kind,
                Delimiter = loaded.Delimiter,
                IncludeChecksum = loaded.IncludeChecksum,
                Fields = loaded.Fields,
            };
            string after = new MessageGenerator(new Random(1)).Generate(loadedFormat);

            Assert.Equal(before, after);
        }

        [Fact]
        public void Serialize_WritesEnumsAsNames()
        {
            string json = SimulatorConfigCodec.Serialize(Sample());
            Assert.Contains("Nmea", json);
            Assert.Contains("RandomDecimal", json);
            Assert.DoesNotContain("\"Kind\": 1", json);
        }

        [Fact]
        public void Deserialize_HandWrittenJson_Works()
        {
            const string json = """
            {
              "Kind": "Hex",
              "Delimiter": "",
              "IncludeChecksum": false,
              "StreamIntervalMs": 250,
              "Fields": [
                { "Name": "a", "Type": "FixedText", "FixedValue": "X" }
              ]
            }
            """;

            var config = SimulatorConfigCodec.Deserialize(json);
            Assert.Equal(MessageFormatKind.Hex, config.Kind);
            Assert.Equal(string.Empty, config.Delimiter);
            Assert.False(config.IncludeChecksum);
            Assert.Equal(250, config.StreamIntervalMs);
            Assert.Single(config.Fields);
            Assert.Equal("X", config.Fields[0].FixedValue);
        }

        [Fact]
        public void Serialize_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SimulatorConfigCodec.Serialize(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{ not json")]
        [InlineData("[1,2,3]")]
        public void Deserialize_InvalidJson_ThrowsFormatException(string json)
        {
            Assert.Throws<FormatException>(() => SimulatorConfigCodec.Deserialize(json));
        }
    }
}
