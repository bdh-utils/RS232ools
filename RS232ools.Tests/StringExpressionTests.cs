using System;
using System.Collections.Generic;
using RS232ools.Devices;
using Xunit;

namespace RS232ools.Tests
{
    public class StringExpressionTests
    {
        private static ExprValue Eval(string expr, Dictionary<string, ExprValue>? vars = null)
            => ExpressionEvaluator.EvaluateValue(expr, vars);

        private static string Text(string expr, Dictionary<string, ExprValue>? vars = null)
            => Eval(expr, vars).AsText();

        [Fact]
        public void StringLiteral_DoubleAndSingleQuotes()
        {
            Assert.Equal("res", Text("\"res\""));
            Assert.Equal("res", Text("'res'"));
        }

        [Fact]
        public void StringLiteral_Escapes()
        {
            Assert.Equal("a\tb\n", Text("\"a\\tb\\n\""));
            Assert.Equal("say \"hi\"", Text("\"say \\\"hi\\\"\""));
        }

        [Fact]
        public void Concat_WithPlus_WhenNonNumeric()
        {
            Assert.Equal("abcde[res]123", Text("\"abcde[\" + \"res\" + \"]123\""));
        }

        [Fact]
        public void Plus_AddsWhenBothNumeric()
        {
            Assert.Equal(3d, Eval("1 + 2").AsNumber());
            // numeric-looking strings still add
            var vars = new Dictionary<string, ExprValue> { ["v"] = ExprValue.Text("4") };
            Assert.Equal(5d, Eval("v + 1", vars).AsNumber());
        }

        [Fact]
        public void Concat_Function_AlwaysJoinsAsText()
        {
            Assert.Equal("1234", Text("concat(\"12\", \"34\")"));
        }

        [Theory]
        [InlineData("contains(\"abcde[req]123\", \"req\")", "1")]
        [InlineData("contains(\"abcde[req]123\", \"xyz\")", "0")]
        [InlineData("startsWith(\"abcde\", \"abc\")", "1")]
        [InlineData("endsWith(\"abcde\", \"de\")", "1")]
        [InlineData("indexOf(\"abcde\", \"cd\")", "2")]
        [InlineData("len(\"abcde\")", "5")]
        public void StringPredicateFunctions(string expr, string expected)
        {
            Assert.Equal(expected, Text(expr));
        }

        [Theory]
        [InlineData("replace(\"abcde[req]123\", \"req\", \"res\")", "abcde[res]123")]
        [InlineData("upper(\"abc\")", "ABC")]
        [InlineData("lower(\"ABC\")", "abc")]
        [InlineData("trim(\"  hi  \")", "hi")]
        [InlineData("substring(\"abcdef\", 2)", "cdef")]
        [InlineData("substring(\"abcdef\", 1, 3)", "bcd")]
        [InlineData("substring(\"abc\", 10)", "")]
        [InlineData("padLeft(\"7\", 3, \"0\")", "007")]
        [InlineData("padRight(\"7\", 3)", "7  ")]
        public void StringTransformFunctions(string expr, string expected)
        {
            Assert.Equal(expected, Text(expr));
        }

        [Fact]
        public void Ternary_CanReturnStrings()
        {
            var vars = new Dictionary<string, ExprValue> { ["tag"] = ExprValue.Text("req") };
            Assert.Equal("res", Text("contains(tag, \"req\") ? \"res\" : tag", vars));
            vars["tag"] = ExprValue.Text("abc");
            Assert.Equal("abc", Text("contains(tag, \"req\") ? \"res\" : tag", vars));
        }

        [Fact]
        public void Equality_Textual()
        {
            var vars = new Dictionary<string, ExprValue> { ["tag"] = ExprValue.Text("req") };
            Assert.Equal("1", Text("tag == \"req\"", vars));
            Assert.Equal("0", Text("tag == \"res\"", vars));
        }

        [Fact]
        public void NumericFunctions()
        {
            Assert.Equal(5d, Eval("abs(-5)").AsNumber());
            Assert.Equal(3d, Eval("round(2.7)").AsNumber());
            Assert.Equal(2.46d, Eval("round(2.456, 2)").AsNumber());
            Assert.Equal(1d, Eval("min(1, 9)").AsNumber());
            Assert.Equal(9d, Eval("max(1, 9)").AsNumber());
        }

        [Fact]
        public void UnknownFunction_Throws()
        {
            Assert.Throws<ExpressionException>(() => Eval("bogus(1)"));
        }

        [Fact]
        public void WrongArgCount_Throws()
        {
            Assert.Throws<ExpressionException>(() => Eval("upper(\"a\", \"b\")"));
            Assert.Throws<ExpressionException>(() => Eval("replace(\"a\", \"b\")"));
        }

        [Fact]
        public void UnterminatedString_Throws()
        {
            Assert.Throws<ExpressionException>(() => Eval("\"abc"));
        }

        [Fact]
        public void NonNumericInArithmetic_Throws()
        {
            Assert.Throws<ExpressionException>(() => Eval("\"abc\" * 2"));
        }
    }
}
