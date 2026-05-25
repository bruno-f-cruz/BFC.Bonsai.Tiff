using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using BitMiracle.LibTiff.Classic;
using BFC.Bonsai.Tiff.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BFC.Bonsai.Tiff.Tests
{
    /// <summary>
    /// Smoke-level contracts for the read-side operators. These exercise the operator wrappers,
    /// not the underlying LibTiff decoder (which is the library's responsibility).
    /// </summary>
    [TestClass]
    public class ReadOperatorTests
    {
        private static string WriteStack(int pageCount, byte fillStep = 10)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null);
            for (int i = 0; i < pageCount; i++)
                w.WriteFrame(TestImages.Gray8(4, 4, (byte)(i * fillStep)));
            return path;
        }

        [TestMethod]
        public void LoadTiff_HonoursPageIndex()
        {
            var path = WriteStack(3);
            var img = new LoadTiff { FileName = path, PageIndex = 1 }.Process().ToList().Wait()[0];
            CollectionAssert.AreEqual(
                TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 10)),
                TestImages.ToPackedBytes(img));
        }

        [TestMethod]
        public void TiffReader_AsSource_EmitsPagesFromStartToEnd()
        {
            var path = WriteStack(5);
            var results = new TiffReader { FileName = path, StartPageIndex = 2 }.Process().ToList().Wait();
            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public void TiffReader_Triggered_EmitsOnePagePerTick_AdvancingFromStart()
        {
            var path = WriteStack(5);
            var op = new TiffReader { FileName = path, StartPageIndex = 1 };
            var results = op.Process(Observable.Range(0, 3)).ToList().Wait();
            CollectionAssert.AreEqual(TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 10)), TestImages.ToPackedBytes(results[0]));
            CollectionAssert.AreEqual(TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 20)), TestImages.ToPackedBytes(results[1]));
            CollectionAssert.AreEqual(TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 30)), TestImages.ToPackedBytes(results[2]));
        }

        [TestMethod]
        public void GetTiffPageInfo_ReturnsPageDimensions()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                w.WriteFrame(TestImages.Gray8(48, 32));

            var info = new GetTiffPageInfo { FileName = path }.Process().ToList().Wait()[0];
            Assert.AreEqual(48, info.Width);
            Assert.AreEqual(32, info.Height);
            Assert.AreEqual(1, info.Channels);
        }

        [TestMethod]
        public void GetTiffMetadata_ReturnsPageMetadata()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                w.WriteFrame(TestImages.Gray8(4, 4), new TiffMetadata { Description = "hello" });

            var meta = new GetTiffMetadata { FileName = path }.Process().ToList().Wait()[0];
            Assert.AreEqual("hello", meta.Description);
        }
    }

    [TestClass]
    public class CreateTiffMetadataTests
    {
        [TestMethod]
        public void CreateTiffMetadata_PopulatesFromProperties()
        {
            var op = new CreateTiffMetadata
            {
                Description = "desc",
                Artist = "alice",
                ResolutionX = 300f,
                ResolutionUnit = TiffResolutionUnit.Inch
            };

            var meta = op.Process().ToList().Wait()[0];
            Assert.AreEqual("desc", meta.Description);
            Assert.AreEqual("alice", meta.Artist);
            Assert.AreEqual(300f, meta.ResolutionX);
            Assert.AreEqual(TiffResolutionUnit.Inch, meta.ResolutionUnit);
        }

        [TestMethod]
        public void CreateTiffMetadata_Triggered_EmitsFreshInstancePerTick()
        {
            var op = new CreateTiffMetadata { Description = "x" };
            var results = op.Process(Observable.Range(0, 2)).ToList().Wait();
            Assert.AreEqual(2, results.Count);
            Assert.AreNotSame(results[0], results[1]);
        }

        [TestMethod]
        public void TiffMetadata_ToString_OmitsNullAndEmptyFields()
        {
            var s = new TiffMetadata { Description = "scan-001", ResolutionX = 300f }.ToString();
            StringAssert.Contains(s, "Description=scan-001");
            StringAssert.Contains(s, "ResolutionX=300");
            Assert.IsFalse(s.Contains("Artist"));
            Assert.IsFalse(s.Contains("ResolutionY"));
        }
    }
}
