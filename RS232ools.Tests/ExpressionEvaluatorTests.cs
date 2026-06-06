using System.Collections.Generic;
using RS232ools.Devices;
using Xunit;

namespace RS232ools.Tests
{
    public class ExpressionEvaluatorTests
    {
        private static double Eval(string expr, Dictionary<string, double>? vars = null)
            => ExpressionEvaluator.Evaluate(expr, vars);

        [Theory]
        [InlineData("1 + 2", 3)]
        [InlineData("2 * 3 + 4", 10)]       // precedence
        [InlineData("2 + 3 * 4", 14)]
        [InlineData("(2 + 3) * 4", 20)]     // parentheses
        [InlineData("10 / 4", 2.5)]
        [InlineData("10 % 3", 1)]
        [InlineData("-5 + 2", -3)]          // unary minus
        [InlineData("2 * -3", -6)]
        [InlineData("1.5 * 2", 3)]
        public void Arithmetic(string expr, double expected)
        {
            Assert.Equal(expected, Eval(expr), 9);
        }

        [Theory]
        [InlineData("3 > 2", 1)]
        [InlineData("2 > 3", 0)]
        [InlineData("2 >= 2", 1)]
        [InlineData("2 <= 1", 0)]
        [InlineData("2 == 2", 1)]
        [InlineData("2 != 2", 0)]
        [InlineData("1 && 0", 0)]
        [InlineData("1 || 0", 1)]
        [InlineData("!0", 1)]
        [InlineData("!5", 0)]
        [InlineData("(3 > 2) && (1 < 2)", 1)]
        [InlineData("true", 1)]
        [InlineData("false", 0)]
        public void Boolean(string expr, double expected)
        {
            Assert.Equal(expected, Eval(expr));
        }

        [Theory]
        [InlineData("1 ? 10 : 20", 10)]
        [InlineData("0 ? 10 : 20", 20)]
        [InlineData("5 > 3 ? 100 : 0", 100)]
        public void Ternary(string expr, double expected)
        {
            Assert.Equal(expected, Eval(expr));
        }

        [Fact]
        public void Variables_AreResolved()
        {
            var vars = new Dictionary<string, double> { ["celsius"] = 20 };
            Assert.Equal(68, Eval("celsius * 9 / 5 + 32", vars));
        }

        [Fact]
        public void Variables_BooleanOverCaptures()
        {
            var vars = new Dictionary<string, double> { ["level"] = 80, ["mode"] = 1 };
            Assert.Equal(1, Eval("level > 50 && mode == 1", vars));
        }

        [Theory]
        [InlineData("")]
        [InlineData("1 +")]
        [InlineData("(1 + 2")]
        [InlineData("1 ? 2")]
        [InlineData("@")]
        public void SyntaxErrors_Throw(string expr)
        {
            Assert.Throws<ExpressionException>(() => Eval(expr));
        }

        [Fact]
        public void UnknownVariable_Throws()
        {
            Assert.Throws<ExpressionException>(() => Eval("missing + 1"));
        }

        [Fact]
        public void DivisionByZero_Throws()
        {
            Assert.Throws<ExpressionException>(() => Eval("1 / 0"));
            Assert.Throws<ExpressionException>(() => Eval("1 % 0"));
        }

        [Fact]
        public void TrailingTokens_Throw()
        {
            Assert.Throws<ExpressionException>(() => Eval("1 2"));
        }
    }
}
