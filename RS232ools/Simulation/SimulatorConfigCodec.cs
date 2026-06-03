using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RS232ools.Simulation
{
    /// <summary>
    /// Serialises a <see cref="SimulatorConfig"/> to and from JSON. Enums are
    /// written as names so saved files are human-readable and stable.
    /// </summary>
    public static class SimulatorConfigCodec
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public static string Serialize(SimulatorConfig config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            return JsonSerializer.Serialize(config, Options);
        }

        /// <summary>
        /// Parses a config from JSON.
        /// </summary>
        /// <exception cref="FormatException">The JSON was missing or not a valid config.</exception>
        public static SimulatorConfig Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("The configuration file was empty.");
            }

            SimulatorConfig? config;
            try
            {
                config = JsonSerializer.Deserialize<SimulatorConfig>(json, Options);
            }
            catch (JsonException ex)
            {
                throw new FormatException($"Not a valid simulator configuration: {ex.Message}", ex);
            }

            if (config is null)
            {
                throw new FormatException("The configuration file did not contain a configuration.");
            }

            // Guard against nulls from a hand-edited or partial file.
            config.Delimiter ??= ",";
            config.Fields ??= new();
            return config;
        }
    }
}
