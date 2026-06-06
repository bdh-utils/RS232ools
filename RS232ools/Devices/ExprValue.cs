using System;
using System.Globalization;

namespace RS232ools.Devices
{
    /// <summary>
    /// A value produced by the expression engine: either a number or a string.
    /// Numeric strings coerce to numbers on demand, so captured text like "100"
    /// can be used in arithmetic, while non-numeric text flows through string
    /// operations and functions.
    /// </summary>
    public readonly struct ExprValue
    {
        private readonly double _number;
        private readonly string? _text;

        private ExprValue(double number, string? text)
        {
            _number = number;
            _text = text;
        }

        public static ExprValue Number(double value) => new(value, null);

        public static ExprValue Text(string? value) => new(0, value ?? string.Empty);

        /// <summary>True if this value is held as text (even if it looks numeric).</summary>
        public bool IsText => _text is not null;

        public bool TryAsNumber(out double value)
        {
            if (_text is null)
            {
                value = _number;
                return true;
            }
            return double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <exception cref="ExpressionException">The value is not numeric.</exception>
        public double AsNumber()
        {
            if (TryAsNumber(out double value)) return value;
            throw new ExpressionException($"'{_text}' is not a number.");
        }

        public string AsText() => _text ?? FormatNumber(_number);

        /// <summary>Truthiness: non-zero number or non-empty string.</summary>
        public bool IsTruthy() => _text is null ? _number != 0 : _text.Length != 0;

        /// <summary>Formats a number: whole values without a decimal point.</summary>
        public static string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
            if (value == Math.Floor(value) && Math.Abs(value) < 1e15)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
