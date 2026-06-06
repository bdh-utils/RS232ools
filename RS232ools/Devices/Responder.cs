using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RS232ools.Devices
{
    /// <summary>The outcome of running an incoming line through the rules.</summary>
    public sealed class ResponderResult
    {
        /// <summary>True if a rule matched the line.</summary>
        public bool Matched { get; private init; }

        /// <summary>The matched rule's name, when applicable.</summary>
        public string? RuleName { get; private init; }

        /// <summary>The reply to send, when a rule matched successfully.</summary>
        public string? Response { get; private init; }

        /// <summary>A problem (bad pattern or expression), when applicable.</summary>
        public string? Error { get; private init; }

        public static ResponderResult NoMatch() => new() { Matched = false };

        public static ResponderResult Reply(string ruleName, string response)
            => new() { Matched = true, RuleName = ruleName, Response = response };

        public static ResponderResult Failure(string? ruleName, string error)
            => new() { Matched = true, RuleName = ruleName, Error = error };
    }

    /// <summary>
    /// Runs an incoming line against an ordered set of rules: the first enabled
    /// rule whose pattern matches wins. Its captured values seed the variable set,
    /// each derived variable's expression is evaluated in turn, and the reply
    /// template's {placeholders} are filled in. Pure and UI-free, so it is fully
    /// unit-testable.
    /// </summary>
    public static class Responder
    {
        private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled);

        public static ResponderResult Respond(IEnumerable<ResponderRule> rules, string line)
        {
            if (rules is null || line is null) return ResponderResult.NoMatch();

            string? firstError = null;

            foreach (var rule in rules)
            {
                if (rule is null || !rule.Enabled) continue;

                Dictionary<string, string>? captures;
                try
                {
                    captures = RuleMatcher.TryMatch(rule, line);
                }
                catch (FormatException ex)
                {
                    firstError ??= $"Rule '{rule.Name}': {ex.Message}";
                    continue;
                }

                if (captures is null) continue;

                // Captured values seed the variable set as text; numeric-looking
                // ones still coerce to numbers inside expressions when needed.
                var variables = new Dictionary<string, ExprValue>();
                foreach (var kv in captures)
                {
                    variables[kv.Key] = ExprValue.Text(kv.Value);
                }

                // String view for template substitution (captures + derived).
                var substitutions = new Dictionary<string, string>(captures);

                try
                {
                    foreach (var variable in rule.Variables)
                    {
                        if (variable is null || string.IsNullOrWhiteSpace(variable.Name)) continue;
                        ExprValue value = ExpressionEvaluator.EvaluateValue(variable.Expression, variables);
                        variables[variable.Name] = value;
                        substitutions[variable.Name] = value.AsText();
                    }
                }
                catch (ExpressionException ex)
                {
                    return ResponderResult.Failure(rule.Name, ex.Message);
                }

                return ResponderResult.Reply(rule.Name, Substitute(rule.Response ?? string.Empty, substitutions));
            }

            return firstError is null ? ResponderResult.NoMatch() : ResponderResult.Failure(null, firstError);
        }

        /// <summary>Formats a result: whole numbers without a decimal point.</summary>
        public static string FormatNumber(double value) => ExprValue.FormatNumber(value);

        private static string Substitute(string template, IReadOnlyDictionary<string, string> values)
            => Placeholder.Replace(template, m =>
                values.TryGetValue(m.Groups[1].Value, out string? v) ? v : m.Value);
    }
}
