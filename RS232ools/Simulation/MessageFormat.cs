using System.Collections.Generic;

namespace RS232ools.Simulation
{
    /// <summary>The wire shape of a generated/parsed message.</summary>
    public enum MessageFormatKind
    {
        /// <summary>Delimiter-separated values.</summary>
        Csv,

        /// <summary>NMEA 0183 sentence: <c>$&lt;payload&gt;*&lt;checksum&gt;</c>.</summary>
        Nmea,

        /// <summary>Field values concatenated with no separator at all.</summary>
        Plain,

        /// <summary>
        /// The payload's bytes written as space-separated uppercase hex pairs
        /// (e.g. "48 65 6C") — a binary string represented in hexadecimal.
        /// </summary>
        Hex,
    }

    /// <summary>
    /// A complete, user-customisable message definition: the format kind, the
    /// field separator, whether to append an NMEA checksum, and the ordered list
    /// of fields. Drives both generation (sending) and parsing (receiving).
    /// </summary>
    public sealed class MessageFormat
    {
        public MessageFormatKind Kind { get; set; } = MessageFormatKind.Csv;

        /// <summary>Separator between fields. Defaults to a comma.</summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// For <see cref="MessageFormatKind.Nmea"/>: append <c>*CS</c> to generated
        /// sentences and verify it when parsing. Ignored for CSV.
        /// </summary>
        public bool IncludeChecksum { get; set; } = true;

        /// <summary>The ordered fields that make up the message.</summary>
        public IList<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
    }
}
