using System.Collections.Generic;

namespace RS232ools.Devices
{
    /// <summary>
    /// A saveable/loadable snapshot of the Advanced tab: the responder rules and
    /// the reply line ending. Serialised to JSON by <see cref="AdvancedConfigCodec"/>.
    /// </summary>
    public sealed class AdvancedConfig
    {
        /// <summary>The reply terminator value ("", "\r", "\n" or "\r\n").</summary>
        public string ReplyLineEnding { get; set; } = "\r\n";

        public List<ResponderRule> Rules { get; set; } = new();
    }
}
