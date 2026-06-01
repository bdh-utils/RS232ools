using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace RS232ools.Serial
{
    /// <summary>
    /// Owns a single <see cref="SerialPort"/> and is the only place in the app
    /// that touches it. Provides open/close, asynchronous send (text and raw
    /// bytes), and surfaces received data via the <see cref="DataReceived"/>
    /// event. Contains no WPF dependencies so it can be unit-tested.
    /// </summary>
    public class SerialPortService : IDisposable
    {
        private readonly object _sync = new();
        private SerialPort? _port;
        private Encoding _encoding = Encoding.ASCII;

        /// <summary>
        /// Raised when text has been received. The argument is the decoded text.
        /// IMPORTANT: this fires on a background/threadpool thread (it originates
        /// from the underlying port's own DataReceived event), so subscribers
        /// must marshal to the UI thread themselves before touching UI.
        /// </summary>
        public event EventHandler<string>? DataReceived;

        /// <summary>
        /// Raised when an asynchronous read fails (for example the cable is
        /// unplugged). Also fires on a background thread.
        /// </summary>
        public event EventHandler<Exception>? ErrorOccurred;

        /// <summary>Enumerates the COM ports currently present on the machine.</summary>
        public static string[] GetAvailablePortNames() => SerialPort.GetPortNames();

        /// <summary>True while a port is open.</summary>
        public bool IsOpen => _port?.IsOpen == true;

        /// <summary>The name of the open port, or null when closed.</summary>
        public string? PortName => _port?.PortName;

        /// <summary>
        /// Configures and opens the port described by <paramref name="settings"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">A port is already open.</exception>
        /// <exception cref="SerialPortException">The port could not be opened.</exception>
        public void Open(SerialSettings settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            lock (_sync)
            {
                if (_port is not null && _port.IsOpen)
                {
                    throw new InvalidOperationException("A port is already open. Close it first.");
                }

                var port = new SerialPort(settings.PortName, settings.BaudRate,
                    settings.Parity, settings.DataBits, settings.StopBits)
                {
                    Handshake = settings.Handshake,
                    Encoding = settings.Encoding,
                };

                try
                {
                    port.DataReceived += OnPortDataReceived;
                    port.Open();
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException or IOException or
                          ArgumentException or InvalidOperationException)
                {
                    port.DataReceived -= OnPortDataReceived;
                    port.Dispose();
                    throw new SerialPortException(
                        $"Could not open {settings.PortName}: {ex.Message}", ex);
                }

                _encoding = settings.Encoding;
                _port = port;
            }
        }

        /// <summary>
        /// Closes and disposes the port if open. Safe to call when already
        /// closed. Never throws.
        /// </summary>
        public void Close()
        {
            SerialPort? port;
            lock (_sync)
            {
                port = _port;
                _port = null;
            }

            if (port is null) return;

            try
            {
                port.DataReceived -= OnPortDataReceived;
                if (port.IsOpen) port.Close();
                port.Dispose();
            }
            catch
            {
                // Closing a port that has already gone away (e.g. unplugged USB
                // adapter) can throw; there is nothing useful to do about it.
            }
        }

        /// <summary>
        /// Encodes <paramref name="text"/> with the configured encoding and
        /// writes it to the port, off the calling thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">The port is not open.</exception>
        /// <exception cref="SerialPortException">The write failed.</exception>
        public Task SendAsync(string text)
        {
            var bytes = _encoding.GetBytes(text ?? string.Empty);
            return SendBytesAsync(bytes);
        }

        /// <summary>
        /// Writes raw <paramref name="data"/> to the port, off the calling
        /// thread. Used both for typed text and for file streaming.
        /// </summary>
        /// <exception cref="InvalidOperationException">The port is not open.</exception>
        /// <exception cref="SerialPortException">The write failed.</exception>
        public Task SendBytesAsync(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));

            var port = _port;
            if (port is null || !port.IsOpen)
            {
                throw new InvalidOperationException("Port is not open.");
            }

            return Task.Run(() =>
            {
                try
                {
                    port.Write(data, 0, data.Length);
                }
                catch (Exception ex) when (
                    ex is IOException or TimeoutException or
                          InvalidOperationException or UnauthorizedAccessException)
                {
                    throw new SerialPortException($"Send failed: {ex.Message}", ex);
                }
            });
        }

        public void Dispose() => Close();

        private void OnPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort? port;
            lock (_sync)
            {
                port = _port;
            }

            if (port is null || !port.IsOpen) return;

            try
            {
                int toRead = port.BytesToRead;
                if (toRead <= 0) return;

                var buffer = new byte[toRead];
                int read = port.Read(buffer, 0, toRead);
                if (read <= 0) return;

                string text = _encoding.GetString(buffer, 0, read);
                DataReceived?.Invoke(this, text);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
    }
}
