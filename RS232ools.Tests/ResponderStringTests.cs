using RS232ools.Devices;
using Xunit;

namespace RS232ools.Tests
{
    /// <summary>Covers string variables and string-processing in responder rules.</summary>
    public class ResponderStringTests
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
        public void ReqToRes_ViaReplace()
        {
            // RX "abcde[req]123"  ->  TX "abcde[res]123"
            var rules = new[]
            {
                Rule("reqres", "abcde[{tag}]123", "abcde[{out}]123",
                    ("out", "replace(tag, \"req\", \"res\")")),
            };
            Assert.Equal("abcde[res]123", Responder.Respond(rules, "abcde[req]123").Response);
        }

        [Fact]
        public void ReqToRes_ViaContainsTernary()
        {
            var rules = new[]
            {
                Rule("reqres", "abcde[{tag}]123", "abcde[{out}]123",
                    ("out", "contains(tag, \"req\") ? \"res\" : tag")),
            };
            Assert.Equal("abcde[res]123", Responder.Respond(rules, "abcde[req]123").Response);
            // A different tag passes through unchanged.
            Assert.Equal("abcde[ack]123", Responder.Respond(rules, "abcde[ack]123").Response);
        }

        [Fact]
        public void StringVariable_BuiltByConcatenation()
        {
            var rules = new[]
            {
                Rule("greet", "HELLO {who}", "{msg}",
                    ("msg", "concat(\"HI \", upper(who), \"!\")")),
            };
            Assert.Equal("HI WORLD!", Responder.Respond(rules, "HELLO world").Response);
        }

        [Fact]
        public void MixedStringAndNumericVariables()
        {
            var rules = new[]
            {
                Rule("mix", "M {label} {raw}", "{label}={scaled}",
                    ("scaled", "raw * 2")),
            };
            // label stays a string capture; raw is used numerically.
            Assert.Equal("temp=20", Responder.Respond(rules, "M temp 10").Response);
        }

        [Fact]
        public void StringExpressionError_IsReportedNotThrown()
        {
            var rules = new[] { Rule("bad", "X {t}", "{y}", ("y", "substring(t)")) };
            var result = Responder.Respond(rules, "X hello");
            Assert.True(result.Matched);
            Assert.Null(result.Response);
            Assert.NotNull(result.Error);
        }
    }
}
