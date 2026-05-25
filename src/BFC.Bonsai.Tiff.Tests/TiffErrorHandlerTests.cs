using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitMiracle.LibTiff.Classic;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class TiffErrorHandlerTests
    {
        [TestMethod]
        public void ThrowingErrorHandler_ErrorHandler_ThrowsTiffException()
        {
            // Directly invoke the error handler - it must raise TiffException
            var handler = ThrowingErrorHandler.Instance;
            var ex = Assert.ThrowsException<TiffException>(
                () => handler.ErrorHandler(null, "TestModule", "Error {0}", "msg"));
            StringAssert.Contains(ex.Message, "TestModule");
            StringAssert.Contains(ex.Message, "Error msg");
        }

        [TestMethod]
        public void TiffException_IsInvalidOperationException()
        {
            var ex = new TiffException("test error");
            Assert.IsInstanceOfType(ex, typeof(System.InvalidOperationException));
            Assert.AreEqual("test error", ex.Message);
        }
    }
}
