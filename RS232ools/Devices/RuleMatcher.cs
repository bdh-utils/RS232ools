using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RS232ools.Devices
{
    /// <summary>
    /// Matches an incoming line against a <see cref="ResponderRule"/> and returns
    /// the captured named values. A rule's pattern is either a {placeholder}
    /// template (compiled to an anchored regex, each placeholder capturing one
    /// non-empty token) or — when the rule opts in — a raw regex with named groups.
    /// </summary>
    public static class RuleMatcher
    {
        private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        /// <summary>Converts a {placeholder} template into an anchored regex.</summary>
        public static string TemplateToRegex(string template)
        {
            template ??= string.Empty;
            var sb = new StringBuilder("^");
            int last = 0;
            foreach (Match m in Placeholder.Matches(template))
            {
                sb.Append(Regex.Escape(template.Substring(last, m.Index - last)));
                sb.Append("(?<").Append(m.Groups[1].Value).Append(">.+?)");
                last = m.Index + m.Length;
            }
            sb.Append(Regex.Escape(template.Substring(last)));
            sb.Append('$');
            return sb.ToString();
        }

        /// <summary>
        /// Returns the captured variables if <paramref name="line"/> matches the
        /// rule, or null if it does not.
        /// </summary>
        /// <exception cref="FormatException">The rule's regex pattern is invalid.</exception>
        public static Dictionary<string, string>? TryMatch(ResponderRule rule, string line)
        {
            if (rule is null || line is null) return null;

            string pattern = rule.IsRegex ? (rule.Pattern ?? string.Empty) : TemplateToRegex(rule.Pattern);

            Regex regex;
            try
            {
                regex = new Regex(pattern, RegexOptions.CultureInvariant, MatchTimeout);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException($"invalid pattern: {ex.Message}", ex);
            }

            Match match;
            try
            {
                match = regex.Match(line);
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }

            if (!match.Success) return null;

            var captures = new Dictionary<string, string>();
            foreach (string name in regex.GetGroupNames())
            {
                // Skip the implicit whole-match group ("0") and any unnamed groups.
                if (int.TryParse(name, out _)) continue;
                captures[name] = match.Groups[name].Value;
            }
            return captures;
        }
    }
}
