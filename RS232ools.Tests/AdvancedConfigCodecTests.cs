using System;
using RS232ools.Devices;
using Xunit;

namespace RS232ools.Tests
{
    public class AdvancedConfigCodecTests
    {
        private static AdvancedConfig Sample()
        {
            var config = new AdvancedConfig { ReplyLineEnding = "\r\n" };

            config.Rules.Add(new ResponderRule { Name = "ping", Pattern = "PING", Response = "PONG" });

            var scale = new ResponderRule
            {
                Name = "scale",
                IsRegex = false,
                Pattern = "READ {raw}",
                Response = "VAL={scaled}",
            };
            scale.Variables.Add(new DerivedVariable { Name = "scaled", Expression = "raw * 0.1" });
            config.Rules.Add(scale);

            var off = new ResponderRule { Name = "off", Enabled = false, IsRegex = true, Pattern = "^X$", Response = "Y" };
            config.Rules.Add(off);

            return config;
        }

        [Fact]
        public void RoundTrip_PreservesRulesAndVariables()
        {
            var back = AdvancedConfigCodec.Deserialize(AdvancedConfigCodec.Serialize(Sample()));

            Assert.Equal("\r\n", back.ReplyLineEnding);
            Assert.Equal(3, back.Rules.Count);

            Assert.Equal("ping", back.Rules[0].Name);
            Assert.Equal("PING", back.Rules[0].Pattern);
            Assert.Equal("PONG", back.Rules[0].Response);
            Assert.True(back.Rules[0].Enabled);
            Assert.False(back.Rules[0].IsRegex);
            Assert.Empty(back.Rules[0].Variables);

            Assert.Equal("scale", back.Rules[1].Name);
            Assert.Single(back.Rules[1].Variables);
            Assert.Equal("scaled", back.Rules[1].Variables[0].Name);
            Assert.Equal("raw * 0.1", back.Rules[1].Variables[0].Expression);

            Assert.False(back.Rules[2].Enabled);
            Assert.True(back.Rules[2].IsRegex);
        }

        [Fact]
        public void RoundTrip_RuleStillRespondsIdentically()
        {
            string before = Responder.Respond(Sample().Rules, "READ 100").Response!;

            var back = AdvancedConfigCodec.Deserialize(AdvancedConfigCodec.Serialize(Sample()));
            string after = Responder.Respond(back.Rules, "READ 100").Response!;

            Assert.Equal("VAL=10", before);
            Assert.Equal(before, after);
        }

        [Fact]
        public void Deserialize_HandWrittenJson_Works()
        {
            const string json = """
            {
              "ReplyLineEnding": "\n",
              "Rules": [
                {
                  "Enabled": true,
                  "Name": "temp",
                  "IsRegex": false,
                  "Pattern": "TEMP {c}",
                  "Response": "F={f}",
                  "Variables": [ { "Name": "f", "Expression": "c * 9 / 5 + 32" } ]
                }
              ]
            }
            """;

            var config = AdvancedConfigCodec.Deserialize(json);
            Assert.Equal("\n", config.ReplyLineEnding);
            Assert.Single(config.Rules);
            Assert.Equal("F=68", Responder.Respond(config.Rules, "TEMP 20").Response);
        }

        [Fact]
        public void Deserialize_MissingCollections_DefaultToEmpty()
        {
            var config = AdvancedConfigCodec.Deserialize("{}");
            Assert.NotNull(config.Rules);
            Assert.Empty(config.Rules);
            Assert.Equal("\r\n", config.ReplyLineEnding);
        }

        [Fact]
        public void Serialize_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AdvancedConfigCodec.Serialize(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{ not json")]
        [InlineData("[1,2,3]")]
        public void Deserialize_InvalidJson_ThrowsFormatException(string json)
        {
            Assert.Throws<FormatException>(() => AdvancedConfigCodec.Deserialize(json));
        }
    }
}
