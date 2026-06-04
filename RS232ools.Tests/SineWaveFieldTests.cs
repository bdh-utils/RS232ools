using System;
using System.Globalization;
using RS232ools.Simulation;
using Xunit;

namespace RS232ools.Tests
{
    public class SineWaveFieldTests
    {
        private static MessageGenerator NewGen() => new(new Random(1));

        private static double Value(MessageGenerator gen, FieldDefinition f)
            => double.Parse(gen.GenerateValue(f), CultureInfo.InvariantCulture);

        [Fact]
        public void Sine_StartsAtMidpoint()
        {
            var gen = NewGen();
            var field = new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 10, Period = 4, Precision = 1 };
            Assert.Equal("5.0", gen.GenerateValue(field));
        }

        [Fact]
        public void Sine_TracesQuarterPeriodPeaksAndTroughs()
        {
            var gen = NewGen();
            var field = new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 10, Period = 4, Precision = 1 };

            Assert.Equal("5.0", gen.GenerateValue(field));   // sample 0  -> midpoint
            Assert.Equal("10.0", gen.GenerateValue(field));  // sample 1  -> max (quarter)
            Assert.Equal("5.0", gen.GenerateValue(field));   // sample 2  -> midpoint (half)
            Assert.Equal("0.0", gen.GenerateValue(field));   // sample 3  -> min (three-quarter)
            Assert.Equal("5.0", gen.GenerateValue(field));   // sample 4  -> back to midpoint
        }

        [Fact]
        public void Sine_StaysWithinMinMax()
        {
            var gen = NewGen();
            var field = new FieldDefinition { Type = FieldType.SineWave, Min = -2.5, Max = 7.5, Period = 37, Precision = 4 };

            for (int i = 0; i < 1000; i++)
            {
                double v = Value(gen, field);
                Assert.InRange(v, -2.5, 7.5);
            }
        }

        [Fact]
        public void Sine_RespectsPrecision()
        {
            var gen = NewGen();
            var field = new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 1, Period = 13, Precision = 3 };
            Assert.Matches(@"^\d+\.\d{3}$", gen.GenerateValue(field));
        }

        [Fact]
        public void Sine_IsDeterministicAcrossGenerators()
        {
            var a = NewGen();
            var b = new MessageGenerator(new Random(999)); // different RNG seed must not matter
            var fa = new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 10, Period = 8, Precision = 2 };
            var fb = new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 10, Period = 8, Precision = 2 };

            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(a.GenerateValue(fa), b.GenerateValue(fb));
            }
        }

        [Fact]
        public void Sine_SwappedMinMax_IsHandled()
        {
            var gen = NewGen();
            var field = new FieldDefinition { Type = FieldType.SineWave, Min = 10, Max = 0, Period = 4, Precision = 1 };
            // Still oscillates within the range; first sample is the midpoint.
            Assert.Equal("5.0", gen.GenerateValue(field));
        }

        [Fact]
        public void Sine_NonPositivePeriod_DoesNotThrow()
        {
            var gen = NewGen();
            var field = new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 10, Period = 0, Precision = 1 };
            var ex = Record.Exception(() => gen.GenerateValue(field));
            Assert.Null(ex);
        }

        [Fact]
        public void Sine_AdvancesPerMessage_WhenUsedInAFormat()
        {
            var gen = NewGen();
            var format = new MessageFormat
            {
                Kind = MessageFormatKind.Csv,
                Delimiter = ",",
                Fields = { new FieldDefinition { Type = FieldType.SineWave, Min = 0, Max = 10, Period = 4, Precision = 1 } },
            };

            Assert.Equal("5.0", gen.Generate(format));
            Assert.Equal("10.0", gen.Generate(format));
            Assert.Equal("5.0", gen.Generate(format));
            Assert.Equal("0.0", gen.Generate(format));
        }
    }
}
