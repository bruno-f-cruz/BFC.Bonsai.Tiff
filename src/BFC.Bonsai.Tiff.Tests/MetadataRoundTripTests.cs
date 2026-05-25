using System;
using BitMiracle.LibTiff.Classic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class MetadataRoundTripTests
    {
        [TestMethod]
        public void AllMetadataFields_SurviveWriteRead_RoundTrip()
        {
            using var tmp = TempTiff.New();
            var dt = new DateTime(2025, 1, 15, 8, 30, 0);
            var meta = new TiffMetadata
            {
                Description = "round-trip test",
                Artist = "Test Artist",
                Software = "BFC.Bonsai.Tiff Tests",
                Copyright = "2025",
                PageName = "Page0",
                DateTime = dt,
                ResolutionX = 72f,
                ResolutionY = 72f,
                ResolutionUnit = TiffResolutionUnit.Inch
            };

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(8, 8), meta);

            using var reader = new TiffStreamReader(tmp.Path);
            var readMeta = reader.GetMetadata(0);

            Assert.AreEqual(meta.Description, readMeta.Description);
            Assert.AreEqual(meta.Artist, readMeta.Artist);
            Assert.AreEqual(meta.Software, readMeta.Software);
            Assert.AreEqual(meta.Copyright, readMeta.Copyright);
            Assert.AreEqual(meta.PageName, readMeta.PageName);
            Assert.AreEqual(meta.DateTime, readMeta.DateTime);
            Assert.AreEqual(meta.ResolutionX.Value, readMeta.ResolutionX.Value, 0.01f);
            Assert.AreEqual(meta.ResolutionY.Value, readMeta.ResolutionY.Value, 0.01f);
            Assert.AreEqual(meta.ResolutionUnit, readMeta.ResolutionUnit);
        }
    }
}
