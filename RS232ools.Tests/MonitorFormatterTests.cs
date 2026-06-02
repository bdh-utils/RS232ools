using System.Linq;
using RS232ools.Terminal;
using Xunit;

namespace RS232ools.Tests
{
    public class MonitorFormatterTests
    {
        private static string Render(System.Collections.Generic.IEnumerable<TerminalSegment> segs)
            => string.Concat(segs.Select(s => s.Text));

        [Fact]
        public void Append_FirstReceivedChunk_StartsWithRxPrefix()
        {
            var f = new MonitorFormatter();
            var segs = f.Append("OK\n", isTx: false);
            Assert.Equal("RX < OK\n", Render(segs));
            Assert.All(segs, s => Assert.False(s.IsTx));
        }

        [Fact]
        public void Append_FirstSentChunk_StartsWithTxPrefix()
        {
            var f = new MonitorFormatter();
            var segs = f.Append("AT\n", isTx: true);
            Assert.Equal("TX > AT\n", Render(segs));
            Assert.All(segs, s => Assert.True(s.IsTx));
        }

        [Fact]
        public void Append_EachLineGetsItsOwnPrefix()
        {
            var f = new MonitorFormatter();
            var segs = f.Append("a\nb\n", isTx: false);
            Assert.Equal("RX < a\nRX < b\n", Render(segs));
        }

        [Fact]
        public void Append_DirectionChangeMidLine_BreaksToNewLineFirst()
        {
            var f = new MonitorFormatter();
            // RX with no trailing newline, then a TX chunk arrives.
            f.Append("partial", isTx: false);
            var segs = f.Append("SENT\n", isTx: true);
            Assert.Equal("\nTX > SENT\n", Render(segs));
        }

        [Fact]
        public void Append_SameDirectionContinuation_DoesNotRepeatPrefix()
        {
            var f = new MonitorFormatter();
            f.Append("abc", isTx: false);          // no newline yet
            var segs = f.Append("def\n", isTx: false); // continues the same line
            Assert.Equal("def\n", Render(segs));
        }

        [Fact]
        public void Append_StripsCarriageReturns()
        {
            var f = new MonitorFormatter();
            var segs = f.Append("hi\r\n", isTx: false);
            Assert.Equal("RX < hi\n", Render(segs));
        }

        [Fact]
        public void Append_PreservesInteriorSpaces()
        {
            var f = new MonitorFormatter();
            var segs = f.Append("48 65 6C\n", isTx: true);
            Assert.Equal("TX > 48 65 6C\n", Render(segs));
        }

        [Fact]
        public void Append_EmptyOrNull_ProducesNothing()
        {
            var f = new MonitorFormatter();
            Assert.Empty(f.Append("", isTx: true));
            Assert.Empty(f.Append(null!, isTx: false));
        }

        [Fact]
        public void Reset_ReturnsToLineStart()
        {
            var f = new MonitorFormatter();
            f.Append("midline", isTx: false); // leaves cursor mid-line
            f.Reset();
            var segs = f.Append("TX!\n", isTx: true);
            // After reset, a fresh prefix with no leading line break.
            Assert.Equal("TX > TX!\n", Render(segs));
        }

        [Fact]
        public void Append_SegmentsCarryCorrectDirectionFlag()
        {
            var f = new MonitorFormatter();
            f.Append("rx", isTx: false);
            var segs = f.Append("tx\n", isTx: true);
            // The leading break + prefix + content should all be flagged TX.
            Assert.All(segs, s => Assert.True(s.IsTx));
        }
    }
}
