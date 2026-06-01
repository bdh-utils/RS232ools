using System;
using System.Threading.Tasks;
using RS232ools.Serial;

namespace RS232ools.Tests
{
    /// <summary>
    /// Tests that can run without a live COM port. Anything that requires an
    /// actually-open port (successful send, DataReceived/ErrorOccurred event
    /// firing, double-open guard) is deliberately not covered because no real
    /// serial hardware is available in the test environment.
    /// </summary>
    public class SerialPortServiceTests
    {
        [Fact]
        public void IsOpen_BeforeOpening_IsFalse()
        {
            using var service = new SerialPortService();
            Assert.False(service.IsOpen);
        }

        [Fact]
        public void PortName_BeforeOpening_IsNull()
        {
            using var service = new SerialPortService();
            Assert.Null(service.PortName);
        }

        [Fact]
        public void Close_WhenNeverOpened_DoesNotThrow()
        {
            var service = new SerialPortService();
            var ex = Record.Exception(() => service.Close());
            Assert.Null(ex);
        }

        [Fact]
        public void Dispose_WhenNeverOpened_DoesNotThrow()
        {
            var service = new SerialPortService();
            var ex = Record.Exception(() => service.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void Close_CalledTwice_DoesNotThrow()
        {
            var service = new SerialPortService();
            service.Close();
            var ex = Record.Exception(() => service.Close());
            Assert.Null(ex);
        }

        [Fact]
        public void Open_WithNullSettings_ThrowsArgumentNullException()
        {
            using var service = new SerialPortService();
            var ex = Assert.Throws<ArgumentNullException>(() => service.Open(null!));
            Assert.Equal("settings", ex.ParamName);
        }

        [Fact]
        public void Open_WithNonExistentPort_ThrowsSerialPortException()
        {
            using var service = new SerialPortService();
            var settings = new SerialSettings { PortName = "COM_DOESNOTEXIST" };

            // The service wraps the raw framework exception in its typed wrapper.
            var ex = Assert.Throws<SerialPortException>(() => service.Open(settings));
            Assert.NotNull(ex.InnerException);
            Assert.False(service.IsOpen);
            Assert.Null(service.PortName);
        }

        // SendBytesAsync validates its arguments and the open state synchronously,
        // *before* returning the Task that does the I/O. So these throw directly
        // from the call, not from awaiting the Task -> use Assert.Throws.

        [Fact]
        public void SendBytesAsync_WithNullData_ThrowsArgumentNullExceptionSynchronously()
        {
            using var service = new SerialPortService();
            // The guard runs before the Task is created, so the exception is
            // thrown synchronously from the call itself. We invoke through an
            // Action (discarding the would-be Task) to assert that.
            var ex = Assert.Throws<ArgumentNullException>(
                () => { _ = service.SendBytesAsync(null!); });
            Assert.Equal("data", ex.ParamName);
        }

        [Fact]
        public void SendBytesAsync_WhenPortClosed_ThrowsInvalidOperationExceptionSynchronously()
        {
            using var service = new SerialPortService();
            var ex = Assert.Throws<InvalidOperationException>(
                () => { _ = service.SendBytesAsync(new byte[] { 0x01, 0x02 }); });
            Assert.Equal("Port is not open.", ex.Message);
        }

        [Fact]
        public void SendAsync_WhenPortClosed_ThrowsInvalidOperationExceptionSynchronously()
        {
            using var service = new SerialPortService();
            // SendAsync encodes the text and delegates to SendBytesAsync, whose
            // closed-port guard runs synchronously before any Task is returned.
            var ex = Assert.Throws<InvalidOperationException>(
                () => { _ = service.SendAsync("hello"); });
            Assert.Equal("Port is not open.", ex.Message);
        }

        [Fact]
        public void SendAsync_WithNullText_WhenPortClosed_StillHitsClosedGuard()
        {
            // SendAsync maps null text to an empty byte array, so the failure is
            // the closed-port guard rather than an ArgumentNullException.
            using var service = new SerialPortService();
            var ex = Assert.Throws<InvalidOperationException>(
                () => { _ = service.SendAsync(null!); });
            Assert.Equal("Port is not open.", ex.Message);
        }

        [Fact]
        public void GetAvailablePortNames_ReturnsNonNullArray()
        {
            // May legitimately be empty on a machine with no COM ports; only the
            // non-null contract is asserted.
            string[] names = SerialPortService.GetAvailablePortNames();
            Assert.NotNull(names);
        }
    }
}
