using System;
using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    public class NmeaChecksumTests
    {
        [Fact]
        public void Compute_SingleCharacter_IsItsHexCode()
        {
            // 'A' is 0x41.
            Assert.Equal("41", NmeaChecksum.Compute("A"));
        }

        [Fact]
        public void Compute_KnownGgaSentence_MatchesPublishedChecksum()
        {
            // The canonical example sentence $GPGGA,...*47.
            const string payload = "GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,";
            Assert.Equal("47", NmeaChecksum.Compute(payload));
        }

        [Fact]
        public void Compute_EmptyPayload_IsZero()
        {
            Assert.Equal("00", NmeaChecksum.Compute(string.Empty));
        }

        [Fact]
        public void Compute_NullPayload_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => NmeaChecksum.Compute(null!));
        }

        [Fact]
        public void Validate_CorrectChecksum_IsTrue()
        {
            Assert.True(NmeaChecksum.Validate("GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,", "47"));
        }

        [Fact]
        public void Validate_WrongChecksum_IsFalse()
        {
            Assert.False(NmeaChecksum.Validate("GPGGA,1,2,3", "00"));
        }

        [Fact]
        public void Validate_IsCaseInsensitive_AndWhitespaceTolerant()
        {
            const string payload = "GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,";
            string computed = NmeaChecksum.Compute(payload); // "47"
            Assert.True(NmeaChecksum.Validate(payload, " " + computed.ToLowerInvariant() + " "));
        }

        [Fact]
        public void Validate_NullExpected_IsFalse()
        {
            Assert.False(NmeaChecksum.Validate("A", null));
        }
    }
}
