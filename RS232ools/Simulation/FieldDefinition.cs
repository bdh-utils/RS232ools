namespace RS232ools.Simulation
{
    /// <summary>
    /// One user-customisable field in a message format. The same definition is
    /// used both to generate a random value (when sending) and to label the
    /// column the parsed value lands in (when receiving). Plain mutable
    /// properties so it binds directly to the editing DataGrid.
    /// </summary>
    public sealed class FieldDefinition
    {
        /// <summary>Column header / field name.</summary>
        public string Name { get; set; } = "field";

        /// <summary>How the value is generated.</summary>
        public FieldType Type { get; set; } = FieldType.RandomInteger;

        /// <summary>Lower bound for random/counter types (inclusive).</summary>
        public double Min { get; set; } = 0;

        /// <summary>Upper bound for random types (inclusive).</summary>
        public double Max { get; set; } = 100;

        /// <summary>Decimal places for <see cref="FieldType.RandomDecimal"/>.</summary>
        public int Precision { get; set; } = 2;

        /// <summary>The constant text for <see cref="FieldType.FixedText"/>.</summary>
        public string FixedValue { get; set; } = string.Empty;

        /// <summary>.NET date/time format for <see cref="FieldType.Timestamp"/>.</summary>
        public string TimestampFormat { get; set; } = "HHmmss.ff";
    }
}
