using System.IO.Ports;
using System.Text;
using RS232ools.Serial;

namespace RS232ools.Tests
{
    public class SerialSettingsTests
    {
        [Fact]
        public void Defaults_AreCommon9600_8_N_1_AsciiSetup()
        {
            // Arrange / Act
            var settings = new SerialSettings();

            // Assert
            Assert.Equal("COM1", settings.PortName);
            Assert.Equal(9600, settings.BaudRate);
            Assert.Equal(8, settings.DataBits);
            Assert.Equal(Parity.None, settings.Parity);
            Assert.Equal(StopBits.One, settings.StopBits);
            Assert.Equal(Handshake.None, settings.Handshake);
            Assert.Same(Encoding.ASCII, settings.Encoding);
        }

        [Fact]
        public void Properties_AreMutable_AndRetainAssignedValues()
        {
            // Arrange
            var settings = new SerialSettings();
            var utf8 = Encoding.UTF8;

            // Act
            settings.PortName = "COM7";
            settings.BaudRate = 115200;
            settings.DataBits = 7;
            settings.Parity = Parity.Even;
            settings.StopBits = StopBits.Two;
            settings.Handshake = Handshake.RequestToSend;
            settings.Encoding = utf8;

            // Assert
            Assert.Equal("COM7", settings.PortName);
            Assert.Equal(115200, settings.BaudRate);
            Assert.Equal(7, settings.DataBits);
            Assert.Equal(Parity.Even, settings.Parity);
            Assert.Equal(StopBits.Two, settings.StopBits);
            Assert.Equal(Handshake.RequestToSend, settings.Handshake);
            Assert.Same(utf8, settings.Encoding);
        }
    }
}
