namespace RS232ools.Simulation
{
    /// <summary>
    /// How a single field's value is produced when generating a message (and,
    /// for display, how it is labelled when parsing one back).
    /// </summary>
    public enum FieldType
    {
        /// <summary>A constant string (e.g. an NMEA talker/sentence id like "GPGGA").</summary>
        FixedText,

        /// <summary>A random whole number in the inclusive range [Min, Max].</summary>
        RandomInteger,

        /// <summary>A random real number in [Min, Max], formatted to Precision decimals.</summary>
        RandomDecimal,

        /// <summary>A counter starting at Min that increments by one each message.</summary>
        IncrementingCounter,

        /// <summary>The current time formatted with the field's TimestampFormat.</summary>
        Timestamp,

        /// <summary>
        /// A sine wave oscillating between Min and Max, advancing one sample per
        /// message; Period is the number of messages per full cycle.
        /// </summary>
        SineWave,
    }
}
