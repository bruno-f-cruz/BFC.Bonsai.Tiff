using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BFC.Bonsai.Tiff.Tests;

[TestClass]
public class SmokeTests
{
    [TestMethod]
    public void TiffWriter_Type_Exists() =>
        Assert.IsNotNull(typeof(global::BFC.Bonsai.Tiff.TiffWriter));
}
