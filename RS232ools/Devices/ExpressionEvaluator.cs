using System;
using System.Collections.Generic;
using System.Globalization;

namespace RS232ools.Devices
{
    /// <summary>Thrown when an expression cannot be parsed or evaluated.</summary>
    public sealed class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message) { }
    }

    /// <summary>
    /// Evaluates arithmetic and boolean expressions over a set of numeric
    /// variables. Booleans are represented as 1 (true) / 0 (false); any non-zero
    /// operand counts as true.
    ///
    /// Supported, lowest to highest precedence:
    ///   ternary   cond ? a : b
    ///   logical   ||  &amp;&amp;
    ///   equality  ==  !=
    ///   compare   &lt;  &lt;=  &gt;  &gt;=
    ///   additive  +  -
    ///   multipl.  *  /  %
    ///   unary     -  +  !
    ///   primary   number, variable, true, false, ( ... )
    ///
    /// Throws <see cref="ExpressionException"/> on any syntax or evaluation error.
    /// </summary>
    public static class ExpressionEvaluator
    {
        private static readonly IReadOnlyDictionary<string, double> NoVars =
            new Dictionary<string, double>();

        public static double Evaluate(string expression, IReadOnlyDictionary<string, double>? variables = null)
        {
            if (expression is null) throw new ExpressionException("Expression is null.");
            var parser = new Parser(expression, variables ?? NoVars);
            double result = parser.ParseExpression();
            parser.ExpectEnd();
            return result;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private readonly IReadOnlyDictionary<string, double> _vars;
            private int _pos;

            public Parser(string s, IReadOnlyDictionary<string, double> vars)
            {
                _s = s;
                _vars = vars;
            }

            // ---- lexer helpers ----

            private void SkipWs()
            {
                while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++;
            }

            private char Peek
            {
                get { SkipWs(); return _pos < _s.Length ? _s[_pos] : '\0'; }
            }

            private bool Match(string op)
            {
                SkipWs();
                if (_pos + op.Length <= _s.Length &&
                    string.CompareOrdinal(_s, _pos, op, 0, op.Length) == 0)
                {
                    _pos += op.Length;
                    return true;
                }
                return false;
            }

            public void ExpectEnd()
            {
                SkipWs();
                if (_pos < _s.Length)
                {
                    throw new ExpressionException($"Unexpected '{_s.Substring(_pos)}'.");
                }
            }

            // ---- grammar ----

            public double ParseExpression() => ParseTernary();

            private double ParseTernary()
            {
                double cond = ParseOr();
                if (Match("?"))
                {
                    double whenTrue = ParseTernary();
                    if (!Match(":")) throw new ExpressionException("Expected ':' in '?:' expression.");
                    double whenFalse = ParseTernary();
                    return cond != 0 ? whenTrue : whenFalse;
                }
                return cond;
            }

            private double ParseOr()
            {
                double left = ParseAnd();
                while (Match("||"))
                {
                    double right = ParseAnd();
                    left = (left != 0 || right != 0) ? 1 : 0;
                }
                return left;
            }

            private double ParseAnd()
            {
                double left = ParseEquality();
                while (Match("&&"))
                {
                    double right = ParseEquality();
                    left = (left != 0 && right != 0) ? 1 : 0;
                }
                return left;
            }

            private double ParseEquality()
            {
                double left = ParseComparison();
                while (true)
                {
                    if (Match("==")) left = left == ParseComparison() ? 1 : 0;
                    else if (Match("!=")) left = left != ParseComparison() ? 1 : 0;
                    else return left;
                }
            }

            private double ParseComparison()
            {
                double left = ParseAdditive();
                while (true)
                {
                    // Longer operators first so "<=" is not read as "<".
                    if (Match("<=")) left = left <= ParseAdditive() ? 1 : 0;
                    else if (Match(">=")) left = left >= ParseAdditive() ? 1 : 0;
                    else if (Match("<")) left = left < ParseAdditive() ? 1 : 0;
                    else if (Match(">")) left = left > ParseAdditive() ? 1 : 0;
                    else return left;
                }
            }

            private double ParseAdditive()
            {
                double left = ParseMultiplicative();
                while (true)
                {
                    if (Match("+")) left += ParseMultiplicative();
                    else if (Match("-")) left -= ParseMultiplicative();
                    else return left;
                }
            }

            private double ParseMultiplicative()
            {
                double left = ParseUnary();
                while (true)
                {
                    if (Match("*"))
                    {
                        left *= ParseUnary();
                    }
                    else if (Match("/"))
                    {
                        double right = ParseUnary();
                        if (right == 0) throw new ExpressionException("Division by zero.");
                        left /= right;
                    }
                    else if (Match("%"))
                    {
                        double right = ParseUnary();
                        if (right == 0) throw new ExpressionException("Modulo by zero.");
                        left %= right;
                    }
                    else
                    {
                        return left;
                    }
                }
            }

            private double ParseUnary()
            {
                if (Match("!")) return ParseUnary() == 0 ? 1 : 0;
                if (Match("-")) return -ParseUnary();
                if (Match("+")) return ParseUnary();
                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                if (Match("("))
                {
                    double value = ParseExpression();
                    if (!Match(")")) throw new ExpressionException("Expected ')'.");
                    return value;
                }

                char c = Peek;
                if (char.IsDigit(c) || c == '.') return ParseNumber();
                if (char.IsLetter(c) || c == '_') return ParseIdentifier();
                if (c == '\0') throw new ExpressionException("Unexpected end of expression.");
                throw new ExpressionException($"Unexpected character '{c}'.");
            }

            private double ParseNumber()
            {
                SkipWs();
                int start = _pos;
                while (_pos < _s.Length && (char.IsDigit(_s[_pos]) || _s[_pos] == '.')) _pos++;
                string number = _s.Substring(start, _pos - start);
                if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    throw new ExpressionException($"Invalid number '{number}'.");
                }
                return value;
            }

            private double ParseIdentifier()
            {
                SkipWs();
                int start = _pos;
                while (_pos < _s.Length && (char.IsLetterOrDigit(_s[_pos]) || _s[_pos] == '_')) _pos++;
                string name = _s.Substring(start, _pos - start);

                if (name == "true") return 1;
                if (name == "false") return 0;
                if (_vars.TryGetValue(name, out double value)) return value;
                throw new ExpressionException($"Unknown variable '{name}'.");
            }
        }
    }
}
