using System;
using System.Text.Json;

namespace RS232ools.Devices
{
    /// <summary>
    /// Serialises an <see cref="AdvancedConfig"/> (responder rules + reply ending)
    /// to and from human-readable JSON.
    /// </summary>
    public static class AdvancedConfigCodec
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
        };

        public static string Serialize(AdvancedConfig config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            return JsonSerializer.Serialize(config, Options);
        }

        /// <summary>Parses a config from JSON.</summary>
        /// <exception cref="FormatException">The JSON was missing or not a valid config.</exception>
        public static AdvancedConfig Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("The configuration file was empty.");
            }

            AdvancedConfig? config;
            try
            {
                config = JsonSerializer.Deserialize<AdvancedConfig>(json, Options);
            }
            catch (JsonException ex)
            {
                throw new FormatException($"Not a valid advanced configuration: {ex.Message}", ex);
            }

            if (config is null)
            {
                throw new FormatException("The configuration file did not contain a configuration.");
            }

            // Guard against nulls from a hand-edited or partial file.
            config.ReplyLineEnding ??= "\r\n";
            config.Rules ??= new();
            foreach (var rule in config.Rules)
            {
                if (rule is null) continue;
                rule.Name ??= string.Empty;
                rule.Pattern ??= string.Empty;
                rule.Response ??= string.Empty;
                rule.Variables ??= new();
            }
            return config;
        }
    }
}
