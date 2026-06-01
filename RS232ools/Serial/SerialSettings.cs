using System.IO.Ports;
using System.Text;

namespace RS232ools.Serial
{
    /// <summary>
    /// Configuration used to open a serial port. All values are mutable so the
    /// UI can bind directly to them. Defaults are the common 9600-8-N-1 setup.
    /// </summary>
    public class SerialSettings
    {
        /// <summary>The COM port name, e.g. "COM1".</summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>Baud rate in bits per second.</summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>Number of data bits per frame (typically 7 or 8).</summary>
        public int DataBits { get; set; } = 8;

        /// <summary>Parity checking scheme.</summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>Number of stop bits per frame.</summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>Flow-control / handshaking scheme.</summary>
        public Handshake Handshake { get; set; } = Handshake.None;

        /// <summary>
        /// Encoding used to decode received bytes and encode sent text.
        /// Defaults to ASCII.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.ASCII;
    }
}
