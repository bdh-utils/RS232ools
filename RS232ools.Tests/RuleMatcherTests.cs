using System;
using RS232ools.Devices;
using Xunit;

namespace RS232ools.Tests
{
    public class RuleMatcherTests
    {
        private static ResponderRule Template(string pattern)
            => new() { IsRegex = false, Pattern = pattern };

        private static ResponderRule Regex(string pattern)
            => new() { IsRegex = true, Pattern = pattern };

        [Fact]
        public void TemplateToRegex_AnchorsAndCaptures()
        {
            Assert.Equal(@"^GET\ TEMP\ (?<id>.+?)$", RuleMatcher.TemplateToRegex("GET TEMP {id}"));
        }

        [Fact]
        public void Template_CapturesSingleToken()
        {
            var caps = RuleMatcher.TryMatch(Template("GET TEMP {id}"), "GET TEMP 3");
            Assert.NotNull(caps);
            Assert.Equal("3", caps!["id"]);
        }

        [Fact]
        public void Template_CapturesMultipleDelimitedTokens()
        {
            var caps = RuleMatcher.TryMatch(Template("SET,{addr},{val}"), "SET,5,100");
            Assert.NotNull(caps);
            Assert.Equal("5", caps!["addr"]);
            Assert.Equal("100", caps["val"]);
        }

        [Fact]
        public void Template_RequiresFullLineMatch()
        {
            Assert.Null(RuleMatcher.TryMatch(Template("PING"), "PING EXTRA"));
            Assert.Null(RuleMatcher.TryMatch(Template("GET {x}"), "SET 1"));
        }

        [Fact]
        public void Template_LiteralWithNoPlaceholders()
        {
            var caps = RuleMatcher.TryMatch(Template("PING"), "PING");
            Assert.NotNull(caps);
            Assert.Empty(caps!);
        }

        [Fact]
        public void Regex_CapturesNamedGroups()
        {
            var caps = RuleMatcher.TryMatch(Regex(@"^GET (?<id>\d+)$"), "GET 42");
            Assert.NotNull(caps);
            Assert.Equal("42", caps!["id"]);
        }

        [Fact]
        public void Regex_NonMatch_ReturnsNull()
        {
            Assert.Null(RuleMatcher.TryMatch(Regex(@"^GET (?<id>\d+)$"), "GET abc"));
        }

        [Fact]
        public void Regex_Invalid_Throws()
        {
            Assert.Throws<FormatException>(() => RuleMatcher.TryMatch(Regex("(?<bad>"), "anything"));
        }
    }
}
