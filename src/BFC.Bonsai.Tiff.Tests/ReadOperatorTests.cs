using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using BitMiracle.LibTiff.Classic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class LoadTiffTests
    {
        private static string WriteFile(params (byte fill, int w, int h)[] pages)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                foreach (var (fill, width, height) in pages) w.WriteFrame(TestImages.Gray8(width, height, fill));
            return path;
        }

        [TestMethod]
        public void LoadTiff_EmitsOnePage_ThenCompletes()
        {
            var path = WriteFile((10, 8, 8));
            var op = new LoadTiff { FileName = path };
            var items = op.Process().ToList().Wait();
            Assert.AreEqual(1, items.Count);
        }

        [TestMethod]
        public void LoadTiff_PageIndex1_ReadsCorrectPage()
        {
            var path = WriteFile((10, 8, 8), (20, 8, 8), (30, 8, 8));
            var expected = TestImages.ToPackedBytes(TestImages.Gray8(8, 8, 20));

            var op = new LoadTiff { FileName = path, PageIndex = 1 };
            var img = op.Process().ToList().Wait()[0];
            CollectionAssert.AreEqual(expected, TestImages.ToPackedBytes(img));
        }

        [TestMethod]
        public void LoadTiff_ReTrigger_ReadsEachTime()
        {
            var path = WriteFile((5, 4, 4));
            var op = new LoadTiff { FileName = path };
            var results = op.Process(Observable.Range(0, 3)).ToList().Wait();
            Assert.AreEqual(3, results.Count);
        }
    }

    [TestClass]
    public class TiffReaderTests
    {
        [TestMethod]
        public void TiffReader_5PageFile_Emits5FramesInOrder()
        {
            var frames = Enumerable.Range(0, 5).Select(i => TestImages.Gray8(4, 4, (byte)i)).ToArray();
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                foreach (var f in frames) w.WriteFrame(f);

            var op = new TiffReader { FileName = path };
            var results = op.Process().ToList().Wait();
            Assert.AreEqual(5, results.Count);
            for (int i = 0; i < 5; i++)
                CollectionAssert.AreEqual(TestImages.ToPackedBytes(frames[i]), TestImages.ToPackedBytes(results[i]), $"Frame {i} mismatch");
        }

        [TestMethod]
        public void TiffReader_CompletesAfterLastPage()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                for (int i = 0; i < 3; i++) w.WriteFrame(TestImages.Gray8(4, 4));

            var completed = false;
            new TiffReader { FileName = path }.Process().Subscribe(_ => { }, () => completed = true).Dispose();
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void TiffReader_StartPageIndex_SkipsLeadingPages()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                for (int i = 0; i < 5; i++) w.WriteFrame(TestImages.Gray8(4, 4, (byte)i));

            var op = new TiffReader { FileName = path, StartPageIndex = 2 };
            var results = op.Process().ToList().Wait();
            Assert.AreEqual(3, results.Count);
            CollectionAssert.AreEqual(
                TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 2)),
                TestImages.ToPackedBytes(results[0]));
        }

        [TestMethod]
        public void TiffReader_Triggered_EmitsOnePagePerTick_AdvancingFromStart()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                for (int i = 0; i < 5; i++) w.WriteFrame(TestImages.Gray8(4, 4, (byte)(i * 10)));

            var op = new TiffReader { FileName = path, StartPageIndex = 1 };
            var results = op.Process(Observable.Range(0, 3)).ToList().Wait();

            Assert.AreEqual(3, results.Count);
            CollectionAssert.AreEqual(TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 10)), TestImages.ToPackedBytes(results[0]));
            CollectionAssert.AreEqual(TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 20)), TestImages.ToPackedBytes(results[1]));
            CollectionAssert.AreEqual(TestImages.ToPackedBytes(TestImages.Gray8(4, 4, 30)), TestImages.ToPackedBytes(results[2]));
        }
    }

    [TestClass]
    public class CreateTiffMetadataTests
    {
        [TestMethod]
        public void CreateTiffMetadata_AsSource_EmitsSingleConfiguredInstance()
        {
            var op = new CreateTiffMetadata
            {
                Description = "desc",
                Artist = "alice",
                ResolutionX = 300f,
                ResolutionY = 300f,
                ResolutionUnit = TiffResolutionUnit.Inch
            };

            var results = op.Process().ToList().Wait();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("desc", results[0].Description);
            Assert.AreEqual("alice", results[0].Artist);
            Assert.AreEqual(300f, results[0].ResolutionX);
            Assert.AreEqual(TiffResolutionUnit.Inch, results[0].ResolutionUnit);
        }

        [TestMethod]
        public void CreateTiffMetadata_Triggered_EmitsOnePerTick()
        {
            var op = new CreateTiffMetadata { Description = "x" };
            var results = op.Process(Observable.Range(0, 4)).ToList().Wait();
            Assert.AreEqual(4, results.Count);
            foreach (var m in results)
                Assert.AreEqual("x", m.Description);
        }

        [TestMethod]
        public void CreateTiffMetadata_EachTick_ReturnsNewInstance()
        {
            var op = new CreateTiffMetadata();
            var results = op.Process(Observable.Range(0, 2)).ToList().Wait();
            Assert.AreNotSame(results[0], results[1]);
        }
    }

    [TestClass]
    public class GetTiffPageInfoTests
    {
        [TestMethod]
        public void GetTiffPageInfo_ReturnsDimensions()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                w.WriteFrame(TestImages.Gray8(48, 32));

            var op = new GetTiffPageInfo { FileName = path, PageIndex = 0 };
            var info = op.Process().ToList().Wait()[0];
            Assert.AreEqual(48, info.Width);
            Assert.AreEqual(32, info.Height);
            Assert.AreEqual(1, info.Channels);
        }

        [TestMethod]
        public void GetTiffPageInfo_ReTrigger_ReadsEachTime()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                w.WriteFrame(TestImages.Gray8(4, 4));

            var op = new GetTiffPageInfo { FileName = path };
            var results = op.Process(Observable.Range(0, 3)).ToList().Wait();
            Assert.AreEqual(3, results.Count);
        }
    }

    [TestClass]
    public class GetTiffMetadataTests
    {
        [TestMethod]
        public void GetTiffMetadata_ReturnsDescription()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                w.WriteFrame(TestImages.Gray8(4, 4), new TiffMetadata { Description = "test desc" });

            var op = new GetTiffMetadata { FileName = path, PageIndex = 0 };
            var meta = op.Process().ToList().Wait()[0];
            Assert.AreEqual("test desc", meta.Description);
        }

        [TestMethod]
        public void GetTiffMetadata_ReTrigger_ReadsEachTime()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                w.WriteFrame(TestImages.Gray8(4, 4), new TiffMetadata { Description = "x" });

            var op = new GetTiffMetadata { FileName = path };
            var results = op.Process(Observable.Range(0, 2)).ToList().Wait();
            Assert.AreEqual(2, results.Count);
            foreach (var m in results)
                Assert.AreEqual("x", m.Description);        }
    }
}
