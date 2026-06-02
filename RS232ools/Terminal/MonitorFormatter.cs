using System.Collections.Generic;

namespace RS232ools.Terminal
{
    /// <summary>One piece of terminal output: some text and its direction.</summary>
    public readonly record struct TerminalSegment(string Text, bool IsTx);

    /// <summary>
    /// Turns an interleaved stream of sent (TX) and received (RX) chunks into a
    /// sequence of direction-tagged segments for the monitor view. Each new
    /// visual line — and every switch of direction — is started with a direction
    /// prefix, so the rendered terminal reads like a tagged conversation. Newline
    /// markers in the output are always "\n" and carriage returns are dropped.
    ///
    /// The class is deliberately free of any UI dependency so the prefixing
    /// state machine can be unit-tested directly.
    /// </summary>
    public sealed class MonitorFormatter
    {
        public const string TxPrefix = "TX > ";
        public const string RxPrefix = "RX < ";

        private bool _atLineStart = true;
        private bool _lastWasTx;

        /// <summary>Appends a chunk and returns the segments it produced.</summary>
        public IReadOnlyList<TerminalSegment> Append(string text, bool isTx)
        {
            var segments = new List<TerminalSegment>();
            if (string.IsNullOrEmpty(text)) return segments;

            int i = 0;
            while (i < text.Length)
            {
                if (_atLineStart || _lastWasTx != isTx)
                {
                    // Break to a new line before tagging a change of direction.
                    if (!_atLineStart) segments.Add(new TerminalSegment("\n", isTx));
                    segments.Add(new TerminalSegment(isTx ? TxPrefix : RxPrefix, isTx));
                    _atLineStart = false;
                    _lastWasTx = isTx;
                }

                int newline = text.IndexOf('\n', i);
                if (newline < 0)
                {
                    string tail = StripCr(text.Substring(i));
                    if (tail.Length > 0) segments.Add(new TerminalSegment(tail, isTx));
                    break;
                }

                string line = StripCr(text.Substring(i, newline - i)) + "\n";
                segments.Add(new TerminalSegment(line, isTx));
                _atLineStart = true;
                i = newline + 1;
            }

            return segments;
        }

        /// <summary>Resets the line/direction state (e.g. when the view is cleared).</summary>
        public void Reset()
        {
            _atLineStart = true;
            _lastWasTx = false;
        }

        private static string StripCr(string s) => s.Replace("\r", string.Empty);
    }
}
