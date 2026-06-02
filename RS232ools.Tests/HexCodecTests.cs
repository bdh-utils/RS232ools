using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    public class HexCodecTests
    {
        [Fact]
        public void Encode_ProducesSpaceSeparatedUppercasePairs()
        {
            // "Hi" -> 0x48 0x69
            Assert.Equal("48 69", HexCodec.Encode("Hi"));
        }

        [Fact]
        public void Encode_EmptyString_IsEmpty()
        {
            Assert.Equal(string.Empty, HexCodec.Encode(string.Empty));
        }

        [Fact]
        public void TryDecode_SpaceSeparated_RoundTrips()
        {
            Assert.True(HexCodec.TryDecode("48 69", out string text));
            Assert.Equal("Hi", text);
        }

        [Fact]
        public void TryDecode_Contiguous_AlsoWorks()
        {
            Assert.True(HexCodec.TryDecode("4869", out string text));
            Assert.Equal("Hi", text);
        }

        [Fact]
        public void TryDecode_LowercaseAndExtraWhitespace_Works()
        {
            Assert.True(HexCodec.TryDecode("  48\t69 ", out string text));
            Assert.Equal("Hi", text);
        }

        [Fact]
        public void TryDecode_OddDigitCount_Fails()
        {
            Assert.False(HexCodec.TryDecode("486", out _));
        }

        [Fact]
        public void TryDecode_NonHex_Fails()
        {
            Assert.False(HexCodec.TryDecode("4G", out _));
        }

        [Fact]
        public void TryDecode_Null_Fails()
        {
            Assert.False(HexCodec.TryDecode(null!, out _));
        }

        [Fact]
        public void EncodeThenDecode_RoundTripsAllByteValues()
        {
            var chars = new char[256];
            for (int i = 0; i < 256; i++) chars[i] = (char)i;
            string original = new string(chars);

            string hex = HexCodec.Encode(original);
            Assert.True(HexCodec.TryDecode(hex, out string back));
            Assert.Equal(original, back);
        }
    }
}
