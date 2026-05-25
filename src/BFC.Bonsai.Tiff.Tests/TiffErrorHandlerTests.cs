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
        public void ThrowingErrorHandler_ErrorHandler_ThrowsInvalidOperationException()
        {
            // Directly invoke the error handler - it must raise InvalidOperationException
            var handler = ThrowingErrorHandler.Instance;
            var ex = Assert.ThrowsException<InvalidOperationException>(
                () => handler.ErrorHandler(null, "TestModule", "Error {0}", "msg"));
            StringAssert.Contains(ex.Message, "TestModule");
            StringAssert.Contains(ex.Message, "Error msg");
        }
    }
}
