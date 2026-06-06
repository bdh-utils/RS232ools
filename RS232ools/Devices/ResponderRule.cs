using System.Collections.ObjectModel;

namespace RS232ools.Devices
{
    /// <summary>
    /// A value derived from a rule's captured variables via an
    /// <see cref="ExpressionEvaluator"/> expression. Plain properties so it binds
    /// directly to the editing grid.
    /// </summary>
    public sealed class DerivedVariable
    {
        public string Name { get; set; } = "var";

        /// <summary>An expression over the captured (and earlier derived) variables.</summary>
        public string Expression { get; set; } = string.Empty;
    }

    /// <summary>
    /// One request/response rule for the advanced device simulator: when an
    /// incoming line matches <see cref="Pattern"/>, capture named values, compute
    /// any <see cref="Variables"/>, and send <see cref="Response"/> with the
    /// values substituted into its {placeholders}.
    /// </summary>
    public sealed class ResponderRule
    {
        public bool Enabled { get; set; } = true;

        public string Name { get; set; } = "rule";

        /// <summary>Treat <see cref="Pattern"/> as a raw regular expression
        /// (with named groups) instead of a {placeholder} template.</summary>
        public bool IsRegex { get; set; }

        /// <summary>The expected RX string: a template like <c>GET {id}</c> or,
        /// when <see cref="IsRegex"/>, a regex with named groups.</summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>The TX reply, with <c>{name}</c> placeholders filled from the
        /// captured and derived variables.</summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>Derived variables computed from the captures, in order.</summary>
        public ObservableCollection<DerivedVariable> Variables { get; set; } = new();
    }
}
