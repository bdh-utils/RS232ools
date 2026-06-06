using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RS232ools.Devices
{
    /// <summary>Thrown when an expression cannot be parsed or evaluated.</summary>
    public sealed class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message) { }
    }

    /// <summary>
    /// Evaluates arithmetic, boolean and string expressions over a set of
    /// variables. Values are numbers or strings (see <see cref="ExprValue"/>);
    /// booleans are numbers 1 (true) / 0 (false) and any non-zero number or
    /// non-empty string counts as true.
    ///
    /// Supported, lowest to highest precedence:
    ///   ternary   cond ? a : b
    ///   logical   ||  &amp;&amp;
    ///   equality  ==  !=        (numeric if both sides are numbers, else textual)
    ///   compare   &lt;  &lt;=  &gt;  &gt;=   (numeric)
    ///   additive  +  -          (+ adds two numbers, otherwise concatenates)
    ///   multipl.  *  /  %
    ///   unary     -  +  !
    ///   primary   number, "string", variable, true, false, name(args...), ( ... )
    ///
    /// String functions: contains, startsWith, endsWith, indexOf, replace,
    /// substring, upper, lower, trim, len, concat, padLeft, padRight, str.
    /// Numeric functions: abs, round, floor, ceil, min, max, number.
    ///
    /// Throws <see cref="ExpressionException"/> on any syntax or evaluation error.
    /// </summary>
    public static class ExpressionEvaluator
    {
        /// <summary>Numeric convenience overload (used where only numbers apply).</summary>
        public static double Evaluate(string expression, IReadOnlyDictionary<string, double>? variables = null)
        {
            IReadOnlyDictionary<string, ExprValue>? vars = null;
            if (variables is not null)
            {
                var map = new Dictionary<string, ExprValue>(variables.Count);
                foreach (var kv in variables) map[kv.Key] = ExprValue.Number(kv.Value);
                vars = map;
            }
            return EvaluateValue(expression, vars).AsNumber();
        }

        /// <summary>Evaluates an expression to a number-or-string value.</summary>
        public static ExprValue EvaluateValue(string expression, IReadOnlyDictionary<string, ExprValue>? variables = null)
        {
            if (expression is null) throw new ExpressionException("Expression is null.");
            var parser = new Parser(expression, variables ?? Empty);
            ExprValue result = parser.ParseExpression();
            parser.ExpectEnd();
            return result;
        }

        private static readonly IReadOnlyDictionary<string, ExprValue> Empty = new Dictionary<string, ExprValue>();

        private sealed class Parser
        {
            private readonly string _s;
            private readonly IReadOnlyDictionary<string, ExprValue> _vars;
            private int _pos;

            public Parser(string s, IReadOnlyDictionary<string, ExprValue> vars)
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
                if (_pos < _s.Length) throw new ExpressionException($"Unexpected '{_s.Substring(_pos)}'.");
            }

            // ---- grammar ----

            public ExprValue ParseExpression() => ParseTernary();

            private ExprValue ParseTernary()
            {
                ExprValue cond = ParseOr();
                if (Match("?"))
                {
                    ExprValue whenTrue = ParseTernary();
                    if (!Match(":")) throw new ExpressionException("Expected ':' in '?:' expression.");
                    ExprValue whenFalse = ParseTernary();
                    return cond.IsTruthy() ? whenTrue : whenFalse;
                }
                return cond;
            }

            private ExprValue ParseOr()
            {
                ExprValue left = ParseAnd();
                while (Match("||"))
                {
                    ExprValue right = ParseAnd();
                    left = Bool(left.IsTruthy() || right.IsTruthy());
                }
                return left;
            }

            private ExprValue ParseAnd()
            {
                ExprValue left = ParseEquality();
                while (Match("&&"))
                {
                    ExprValue right = ParseEquality();
                    left = Bool(left.IsTruthy() && right.IsTruthy());
                }
                return left;
            }

            private ExprValue ParseEquality()
            {
                ExprValue left = ParseComparison();
                while (true)
                {
                    if (Match("==")) left = Bool(AreEqual(left, ParseComparison()));
                    else if (Match("!=")) left = Bool(!AreEqual(left, ParseComparison()));
                    else return left;
                }
            }

            private static bool AreEqual(ExprValue a, ExprValue b)
            {
                if (a.TryAsNumber(out double x) && b.TryAsNumber(out double y)) return x == y;
                return string.Equals(a.AsText(), b.AsText(), StringComparison.Ordinal);
            }

            private ExprValue ParseComparison()
            {
                ExprValue left = ParseAdditive();
                while (true)
                {
                    if (Match("<=")) left = Bool(left.AsNumber() <= ParseAdditive().AsNumber());
                    else if (Match(">=")) left = Bool(left.AsNumber() >= ParseAdditive().AsNumber());
                    else if (Match("<")) left = Bool(left.AsNumber() < ParseAdditive().AsNumber());
                    else if (Match(">")) left = Bool(left.AsNumber() > ParseAdditive().AsNumber());
                    else return left;
                }
            }

            private ExprValue ParseAdditive()
            {
                ExprValue left = ParseMultiplicative();
                while (true)
                {
                    if (Match("+"))
                    {
                        ExprValue right = ParseMultiplicative();
                        // Add when both sides are numbers; otherwise concatenate.
                        left = left.TryAsNumber(out double a) && right.TryAsNumber(out double b)
                            ? ExprValue.Number(a + b)
                            : ExprValue.Text(left.AsText() + right.AsText());
                    }
                    else if (Match("-"))
                    {
                        left = ExprValue.Number(left.AsNumber() - ParseMultiplicative().AsNumber());
                    }
                    else
                    {
                        return left;
                    }
                }
            }

            private ExprValue ParseMultiplicative()
            {
                ExprValue left = ParseUnary();
                while (true)
                {
                    if (Match("*"))
                    {
                        left = ExprValue.Number(left.AsNumber() * ParseUnary().AsNumber());
                    }
                    else if (Match("/"))
                    {
                        double right = ParseUnary().AsNumber();
                        if (right == 0) throw new ExpressionException("Division by zero.");
                        left = ExprValue.Number(left.AsNumber() / right);
                    }
                    else if (Match("%"))
                    {
                        double right = ParseUnary().AsNumber();
                        if (right == 0) throw new ExpressionException("Modulo by zero.");
                        left = ExprValue.Number(left.AsNumber() % right);
                    }
                    else
                    {
                        return left;
                    }
                }
            }

            private ExprValue ParseUnary()
            {
                if (Match("!")) return Bool(!ParseUnary().IsTruthy());
                if (Match("-")) return ExprValue.Number(-ParseUnary().AsNumber());
                if (Match("+")) return ParseUnary();
                return ParsePrimary();
            }

            private ExprValue ParsePrimary()
            {
                if (Match("("))
                {
                    ExprValue value = ParseExpression();
                    if (!Match(")")) throw new ExpressionException("Expected ')'.");
                    return value;
                }

                char c = Peek;
                if (c == '"' || c == '\'') return ExprValue.Text(ParseStringLiteral());
                if (char.IsDigit(c) || c == '.') return ExprValue.Number(ParseNumber());
                if (char.IsLetter(c) || c == '_') return ParseIdentifierOrCall();
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

            private string ParseStringLiteral()
            {
                SkipWs();
                char quote = _s[_pos++];
                var sb = new StringBuilder();
                while (_pos < _s.Length)
                {
                    char ch = _s[_pos++];
                    if (ch == quote) return sb.ToString();
                    if (ch == '\\' && _pos < _s.Length)
                    {
                        char esc = _s[_pos++];
                        sb.Append(esc switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            _ => esc, // \\  \"  \'  and anything else: literal
                        });
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                throw new ExpressionException("Unterminated string literal.");
            }

            private ExprValue ParseIdentifierOrCall()
            {
                SkipWs();
                int start = _pos;
                while (_pos < _s.Length && (char.IsLetterOrDigit(_s[_pos]) || _s[_pos] == '_')) _pos++;
                string name = _s.Substring(start, _pos - start);

                if (Peek == '(') return CallFunction(name);

                if (name == "true") return ExprValue.Number(1);
                if (name == "false") return ExprValue.Number(0);
                if (_vars.TryGetValue(name, out ExprValue value)) return value;
                throw new ExpressionException($"Unknown variable '{name}'.");
            }

            private ExprValue CallFunction(string name)
            {
                Match("("); // consume '('
                var args = new List<ExprValue>();
                if (Peek != ')')
                {
                    do { args.Add(ParseExpression()); } while (Match(","));
                }
                if (!Match(")")) throw new ExpressionException($"Expected ')' after arguments to '{name}'.");
                return Functions.Call(name, args);
            }

            private static ExprValue Bool(bool b) => ExprValue.Number(b ? 1 : 0);
        }

        // ---- function library ---------------------------------------------

        private static class Functions
        {
            public static ExprValue Call(string name, List<ExprValue> a)
            {
                switch (name.ToLowerInvariant())
                {
                    case "len": Need(name, a, 1); return ExprValue.Number(a[0].AsText().Length);
                    case "upper": Need(name, a, 1); return ExprValue.Text(a[0].AsText().ToUpperInvariant());
                    case "lower": Need(name, a, 1); return ExprValue.Text(a[0].AsText().ToLowerInvariant());
                    case "trim": Need(name, a, 1); return ExprValue.Text(a[0].AsText().Trim());
                    case "str": Need(name, a, 1); return ExprValue.Text(a[0].AsText());
                    case "number": Need(name, a, 1); return ExprValue.Number(a[0].AsNumber());

                    case "contains": Need(name, a, 2);
                        return Bool(a[0].AsText().Contains(a[1].AsText(), StringComparison.Ordinal));
                    case "startswith": Need(name, a, 2);
                        return Bool(a[0].AsText().StartsWith(a[1].AsText(), StringComparison.Ordinal));
                    case "endswith": Need(name, a, 2);
                        return Bool(a[0].AsText().EndsWith(a[1].AsText(), StringComparison.Ordinal));
                    case "indexof": Need(name, a, 2);
                        return ExprValue.Number(a[0].AsText().IndexOf(a[1].AsText(), StringComparison.Ordinal));

                    case "replace": Need(name, a, 3);
                        return ExprValue.Text(a[0].AsText().Replace(a[1].AsText(), a[2].AsText()));

                    case "substring": return Substring(name, a);
                    case "concat": NeedAtLeast(name, a, 1); return Concat(a);
                    case "padleft": return Pad(name, a, left: true);
                    case "padright": return Pad(name, a, left: false);

                    case "abs": Need(name, a, 1); return ExprValue.Number(Math.Abs(a[0].AsNumber()));
                    case "floor": Need(name, a, 1); return ExprValue.Number(Math.Floor(a[0].AsNumber()));
                    case "ceil": Need(name, a, 1); return ExprValue.Number(Math.Ceiling(a[0].AsNumber()));
                    case "round": return Round(name, a);
                    case "min": Need(name, a, 2); return ExprValue.Number(Math.Min(a[0].AsNumber(), a[1].AsNumber()));
                    case "max": Need(name, a, 2); return ExprValue.Number(Math.Max(a[0].AsNumber(), a[1].AsNumber()));

                    default: throw new ExpressionException($"Unknown function '{name}'.");
                }
            }

            private static ExprValue Substring(string name, List<ExprValue> a)
            {
                if (a.Count is not (2 or 3)) throw new ExpressionException($"{name} takes 2 or 3 arguments.");
                string s = a[0].AsText();
                int start = Math.Clamp((int)a[1].AsNumber(), 0, s.Length);
                if (a.Count == 3)
                {
                    int length = Math.Clamp((int)a[2].AsNumber(), 0, s.Length - start);
                    return ExprValue.Text(s.Substring(start, length));
                }
                return ExprValue.Text(s.Substring(start));
            }

            private static ExprValue Concat(List<ExprValue> a)
            {
                var sb = new StringBuilder();
                foreach (var v in a) sb.Append(v.AsText());
                return ExprValue.Text(sb.ToString());
            }

            private static ExprValue Pad(string name, List<ExprValue> a, bool left)
            {
                if (a.Count is not (2 or 3)) throw new ExpressionException($"{name} takes 2 or 3 arguments.");
                string s = a[0].AsText();
                int width = (int)a[1].AsNumber();
                char pad = a.Count == 3 && a[2].AsText().Length > 0 ? a[2].AsText()[0] : ' ';
                return ExprValue.Text(left ? s.PadLeft(width, pad) : s.PadRight(width, pad));
            }

            private static ExprValue Round(string name, List<ExprValue> a)
            {
                if (a.Count is not (1 or 2)) throw new ExpressionException($"{name} takes 1 or 2 arguments.");
                int digits = a.Count == 2 ? Math.Clamp((int)a[1].AsNumber(), 0, 15) : 0;
                return ExprValue.Number(Math.Round(a[0].AsNumber(), digits));
            }

            private static void Need(string name, List<ExprValue> a, int count)
            {
                if (a.Count != count)
                    throw new ExpressionException($"{name} takes {count} argument(s), got {a.Count}.");
            }

            private static void NeedAtLeast(string name, List<ExprValue> a, int count)
            {
                if (a.Count < count)
                    throw new ExpressionException($"{name} takes at least {count} argument(s).");
            }

            private static ExprValue Bool(bool b) => ExprValue.Number(b ? 1 : 0);
        }
    }
}
