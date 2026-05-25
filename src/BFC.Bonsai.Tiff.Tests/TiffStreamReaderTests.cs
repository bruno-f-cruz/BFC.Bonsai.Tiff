using System;
using System.Linq;
using System.Runtime.InteropServices;
using BitMiracle.LibTiff.Classic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCV.Net;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class TiffStreamReaderTests
    {
        private static string WritePages(params IplImage[] images)
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".tiff");
            using (var w = new TiffStreamWriter(path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                foreach (var img in images) w.WriteFrame(img);
            return path;
        }

        [TestMethod]
        public void Read_SingleFrame_Gray8_PixelsExact()
        {
            var src = TestImages.Gray8(32, 16, 42);
            var expected = TestImages.ToPackedBytes(src);
            var path = WritePages(src);

            using var reader = new TiffStreamReader(path);
            var result = TestImages.ToPackedBytes(reader.ReadPage(0));
            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Read_Gray16_PixelsExact()
        {
            var src = TestImages.Gray16(32, 16, 1000);
            var expected = TestImages.ToPackedBytes(src);
            var path = WritePages(src);

            using var reader = new TiffStreamReader(path);
            CollectionAssert.AreEqual(expected, TestImages.ToPackedBytes(reader.ReadPage(0)));
        }

        [TestMethod]
        public void Read_Float32_PixelsExact()
        {
            var src = TestImages.Float32(16, 8, 1.5f);
            var expected = TestImages.ToPackedBytes(src);
            var path = WritePages(src);

            using var reader = new TiffStreamReader(path);
            CollectionAssert.AreEqual(expected, TestImages.ToPackedBytes(reader.ReadPage(0)));
        }

        [TestMethod]
        public void Read_Rgb8_PixelsExact()
        {
            var src = TestImages.Rgb8(32, 16);
            var expected = TestImages.ToPackedBytes(src);
            var path = WritePages(src);

            using var reader = new TiffStreamReader(path);
            var page = reader.ReadPage(0);
            Assert.AreEqual(3, page.Channels);
            CollectionAssert.AreEqual(expected, TestImages.ToPackedBytes(page));
        }

        [TestMethod]
        public void Read_PageCount_MatchesFramesWritten()
        {
            var path = WritePages(
                TestImages.Gray8(8, 8, 0),
                TestImages.Gray8(8, 8, 1),
                TestImages.Gray8(8, 8, 2),
                TestImages.Gray8(8, 8, 3),
                TestImages.Gray8(8, 8, 4));

            using var reader = new TiffStreamReader(path);
            Assert.AreEqual(5, reader.PageCount);
        }

        [TestMethod]
        public void GetPageInfo_ReturnsCorrectDimensions()
        {
            var path = WritePages(TestImages.Gray8(32, 16));

            using var reader = new TiffStreamReader(path);
            var info = reader.GetPageInfo(0);
            Assert.AreEqual(32, info.Width);
            Assert.AreEqual(16, info.Height);
            Assert.AreEqual(1, info.Channels);
            Assert.AreEqual(IplDepth.U8, info.Depth);
        }

        [TestMethod]
        public void RandomAccess_Page3_Of5_PixelsExact()
        {
            var frames = Enumerable.Range(0, 5).Select(i => TestImages.Gray8(8, 8, (byte)(i * 10))).ToArray();
            var path = WritePages(frames);
            var expected = TestImages.ToPackedBytes(frames[3]);

            using var reader = new TiffStreamReader(path);
            CollectionAssert.AreEqual(expected, TestImages.ToPackedBytes(reader.ReadPage(3)));
        }

        [TestMethod]
        public void ReadAllPages_ReturnsAllInOrder()
        {
            var frames = Enumerable.Range(0, 3).Select(i => TestImages.Gray8(8, 8, (byte)(i + 1))).ToArray();
            var path = WritePages(frames);
            var expected = frames.Select(TestImages.ToPackedBytes).ToArray();

            using var reader = new TiffStreamReader(path);
            var results = reader.ReadAllPages().Select(TestImages.ToPackedBytes).ToArray();
            Assert.AreEqual(3, results.Length);
            for (int i = 0; i < 3; i++)
                CollectionAssert.AreEqual(expected[i], results[i], $"Frame {i} mismatch");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ReadPage_OutOfRange_ThrowsArgumentOutOfRange()
        {
            var path = WritePages(TestImages.Gray8(4, 4));
            using var reader = new TiffStreamReader(path);
            reader.ReadPage(5);
        }
    }
}
