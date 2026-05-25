using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class MetadataRoundTripTests
    {
        [TestMethod]
        [Ignore("Pending TiffStreamReader (Phase 6)")]
        public void AllMetadataFields_SurviveWriteRead_RoundTrip()
        {
            Assert.Inconclusive("Not yet implemented — requires TiffStreamReader");
        }
    }
}
