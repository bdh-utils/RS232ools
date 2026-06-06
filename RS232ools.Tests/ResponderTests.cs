using System.Collections.Generic;
using RS232ools.Devices;
using Xunit;

namespace RS232ools.Tests
{
    public class ResponderTests
    {
        private static ResponderRule Rule(string name, string pattern, string response,
            params (string Name, string Expr)[] vars)
        {
            var rule = new ResponderRule { Name = name, Pattern = pattern, Response = response };
            foreach (var (n, e) in vars)
            {
                rule.Variables.Add(new DerivedVariable { Name = n, Expression = e });
            }
            return rule;
        }

        [Fact]
        public void NoRules_NoMatch()
        {
            var result = Responder.Respond(new List<ResponderRule>(), "anything");
            Assert.False(result.Matched);
        }

        [Fact]
        public void SimpleEcho_RepliesFixedString()
        {
            var rules = new[] { Rule("ping", "PING", "PONG") };
            var result = Responder.Respond(rules, "PING");
            Assert.True(result.Matched);
            Assert.Equal("PONG", result.Response);
            Assert.Equal("ping", result.RuleName);
        }

        [Fact]
        public void CapturedValue_SubstitutedIntoResponse()
        {
            var rules = new[] { Rule("id", "WHO {id}", "ID={id}") };
            var result = Responder.Respond(rules, "WHO 7");
            Assert.Equal("ID=7", result.Response);
        }

        [Fact]
        public void DerivedVariable_TransformsCapture()
        {
            var rules = new[] { Rule("scale", "READ {raw}", "VAL={scaled}", ("scaled", "raw * 0.1")) };
            var result = Responder.Respond(rules, "READ 100");
            Assert.Equal("VAL=10", result.Response);
        }

        [Fact]
        public void DerivedVariable_UnitConversion()
        {
            var rules = new[] { Rule("temp", "TEMP {c}", "F={f}", ("f", "c * 9 / 5 + 32")) };
            var result = Responder.Respond(rules, "TEMP 20");
            Assert.Equal("F=68", result.Response);
        }

        [Fact]
        public void DerivedVariable_BooleanAndTernary()
        {
            var rules = new[]
            {
                Rule("alarm", "LVL {level}", "ALARM={state}", ("state", "level > 50 ? 1 : 0")),
            };
            Assert.Equal("ALARM=1", Responder.Respond(rules, "LVL 80").Response);
            Assert.Equal("ALARM=0", Responder.Respond(rules, "LVL 20").Response);
        }

        [Fact]
        public void DerivedVariables_CanReferenceEarlierOnes()
        {
            var rules = new[]
            {
                Rule("chain", "X {v}", "R={b}", ("a", "v + 1"), ("b", "a * 2")),
            };
            // v=4 -> a=5 -> b=10
            Assert.Equal("R=10", Responder.Respond(rules, "X 4").Response);
        }

        [Fact]
        public void FirstEnabledMatchingRule_Wins()
        {
            var rules = new[]
            {
                Rule("first", "GET {x}", "FIRST {x}"),
                Rule("second", "GET {x}", "SECOND {x}"),
            };
            Assert.Equal("FIRST 1", Responder.Respond(rules, "GET 1").Response);
        }

        [Fact]
        public void DisabledRule_IsSkipped()
        {
            var disabled = Rule("off", "GET {x}", "OFF {x}");
            disabled.Enabled = false;
            var rules = new[] { disabled, Rule("on", "GET {x}", "ON {x}") };
            Assert.Equal("ON 1", Responder.Respond(rules, "GET 1").Response);
        }

        [Fact]
        public void ExpressionError_ReturnsFailure()
        {
            var rules = new[] { Rule("bad", "GET {x}", "{y}", ("y", "x / 0")) };
            var result = Responder.Respond(rules, "GET 1");
            Assert.True(result.Matched);
            Assert.Null(result.Response);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void InvalidRegexRule_IsSkipped_ButErrorSurfacedIfNothingMatches()
        {
            var bad = new ResponderRule { Name = "bad", IsRegex = true, Pattern = "(?<x>", Response = "x" };
            var result = Responder.Respond(new[] { bad }, "anything");
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void UnknownPlaceholderInResponse_LeftLiteral()
        {
            var rules = new[] { Rule("r", "GET {x}", "{x} {missing}") };
            Assert.Equal("1 {missing}", Responder.Respond(rules, "GET 1").Response);
        }

        [Theory]
        [InlineData(10, "10")]
        [InlineData(2.5, "2.5")]
        [InlineData(15.0, "15")]
        public void FormatNumber_TrimsWholeNumbers(double value, string expected)
        {
            Assert.Equal(expected, Responder.FormatNumber(value));
        }
    }
}
