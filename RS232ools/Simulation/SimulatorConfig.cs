using System.Collections.Generic;

namespace RS232ools.Simulation
{
    /// <summary>
    /// A saveable/loadable snapshot of the Simulator's setup: the message format,
    /// the ordered field list, and the streaming interval. Serialised to JSON by
    /// <see cref="SimulatorConfigCodec"/>.
    /// </summary>
    public sealed class SimulatorConfig
    {
        public MessageFormatKind Kind { get; set; } = MessageFormatKind.Csv;

        public string Delimiter { get; set; } = ",";

        public bool IncludeChecksum { get; set; } = true;

        public double StreamIntervalMs { get; set; } = 1000;

        public List<FieldDefinition> Fields { get; set; } = new();
    }
}
