using System;

namespace RS232ools.Serial
{
    /// <summary>
    /// Raised when a serial-port operation (open, write) fails. Wraps the
    /// underlying framework exception in a message suitable for showing to the
    /// user, with the original available via <see cref="Exception.InnerException"/>.
    /// </summary>
    public class SerialPortException : Exception
    {
        public SerialPortException(string message) : base(message)
        {
        }

        public SerialPortException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
