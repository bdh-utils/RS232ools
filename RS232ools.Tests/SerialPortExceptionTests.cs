using System;
using RS232ools.Serial;

namespace RS232ools.Tests
{
    public class SerialPortExceptionTests
    {
        [Fact]
        public void MessageOnlyConstructor_PreservesMessage_AndHasNoInnerException()
        {
            // Arrange / Act
            var ex = new SerialPortException("something broke");

            // Assert
            Assert.Equal("something broke", ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void MessageAndInnerConstructor_PreservesMessageAndInnerException()
        {
            // Arrange
            var inner = new InvalidOperationException("underlying failure");

            // Act
            var ex = new SerialPortException("wrapped message", inner);

            // Assert
            Assert.Equal("wrapped message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void IsAnException()
        {
            // Confirms it can be caught as a generic Exception by callers.
            var ex = new SerialPortException("x");
            Assert.IsAssignableFrom<Exception>(ex);
        }
    }
}
