using System;
using System.IO;
using BitMiracle.LibTiff.Classic;
using LibTiff = BitMiracle.LibTiff.Classic.Tiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class RowsPerStripTests
    {
        [TestMethod]
        public void RowsPerStrip_Explicit_SurvivesWrite()
        {
            using var tmp = TempTiff.New();
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null) { RowsPerStrip = 16 })
                writer.WriteFrame(TestImages.Gray8(64, 64));

            using var tiff = LibTiff.Open(tmp.Path, "r");
            int rps = tiff!.GetField(TiffTag.ROWSPERSTRIP)[0].ToInt();
            Assert.AreEqual(16, rps);
        }

        [TestMethod]
        public void RowsPerStrip_Null_UsesFullHeight()
        {
            using var tmp = TempTiff.New();
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(64, 32));

            using var tiff = LibTiff.Open(tmp.Path, "r");
            int rps = tiff!.GetField(TiffTag.ROWSPERSTRIP)[0].ToInt();
            Assert.AreEqual(32, rps);
        }

        [TestMethod]
        public void RowsPerStrip_16_PixelsExact()
        {
            using var tmp = TempTiff.New();
            var src = TestImages.Gray8(64, 64, 42);
            var expected = TestImages.ToPackedBytes(src);

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null) { RowsPerStrip = 16 })
                writer.WriteFrame(src);

            CollectionAssert.AreEqual(expected, ReadPackedPixels(tmp.Path));
        }

        private static byte[] ReadPackedPixels(string path, int page = 0)
        {
            using var tiff = LibTiff.Open(path, "r");
            tiff!.SetDirectory((short)page);
            int w = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int bps = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int spp = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int rowBytes = w * (bps / 8) * spp;
            var result = new byte[rowBytes * h];
            var buf = new byte[tiff.ScanlineSize()];
            for (int row = 0; row < h; row++)
            {
                tiff.ReadScanline(buf, row);
                Buffer.BlockCopy(buf, 0, result, row * rowBytes, rowBytes);
            }
            return result;
        }
    }

    [TestClass]
    public class TiledWriteTests
    {
        [TestMethod]
        public void Tiles_256x256_ProducesTiledTiff()
        {
            using var tmp = TempTiff.New();
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null) { Tiles = new TileSize(256, 256) })
                writer.WriteFrame(TestImages.Gray8(512, 512));

            using var tiff = LibTiff.Open(tmp.Path, "r");
            Assert.IsTrue(tiff!.IsTiled(), "Expected tiled TIFF");
        }

        [TestMethod]
        public void Tiles_256x256_PixelsExact()
        {
            using var tmp = TempTiff.New();
            var src = TestImages.Gray8(512, 512, 33);
            var expected = TestImages.ToPackedBytes(src);

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null) { Tiles = new TileSize(256, 256) })
                writer.WriteFrame(src);

            CollectionAssert.AreEqual(expected, ReadTiledPixels(tmp.Path));
        }

        [TestMethod]
        public void Tiles_NonMultipleOf16_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(
                () => new TileSize(100, 100));
        }

        [TestMethod]
        public void Tiles_NonSquareImage_PixelsExact()
        {
            using var tmp = TempTiff.New();
            var src = TestImages.Gray8(320, 256, 10);
            var expected = TestImages.ToPackedBytes(src);

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null) { Tiles = new TileSize(64, 64) })
                writer.WriteFrame(src);

            CollectionAssert.AreEqual(expected, ReadTiledPixels(tmp.Path));
        }

        private static byte[] ReadTiledPixels(string path)
        {
            using var tiff = LibTiff.Open(path, "r");
            int w = tiff!.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int bps = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int spp = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int tw = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int th = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
            int bytesPerSample = bps / 8;
            int tileBytes = tw * th * bytesPerSample * spp;
            var tileBuf = new byte[tileBytes];
            int rowBytes = w * bytesPerSample * spp;
            var result = new byte[rowBytes * h];

            for (int y = 0; y < h; y += th)
            for (int x = 0; x < w; x += tw)
            {
                tiff.ReadTile(tileBuf, 0, x, y, 0, 0);
                int copyW = Math.Min(tw, w - x);
                int copyH = Math.Min(th, h - y);
                int tileRowBytes = tw * bytesPerSample * spp;
                int imgRowBytes = copyW * bytesPerSample * spp;
                for (int row = 0; row < copyH; row++)
                    Buffer.BlockCopy(tileBuf, row * tileRowBytes, result, (y + row) * rowBytes + x * bytesPerSample * spp, imgRowBytes);
            }
            return result;
        }
    }

    [TestClass]
    public class PredictorTests
    {
        [TestMethod]
        public void Predictor_Horizontal_WithLzw_RoundTrip_PixelsExact()
        {
            using var tmp = TempTiff.New();
            var src = TestImages.Gray8(128, 128, 20);
            var expected = TestImages.ToPackedBytes(src);

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.LZW, TiffWriteMode.CreateNew, null) { Predictor = TiffPredictor.Horizontal })
                writer.WriteFrame(src);

            CollectionAssert.AreEqual(expected, ReadScanlinePixels(tmp.Path));
        }

        [TestMethod]
        public void Predictor_Horizontal_WithLzw_SmallerThanLzwAlone()
        {
            using var noPredict = TempTiff.New();
            using var withPredict = TempTiff.New();
            var src = TestImages.Gray8(256, 256, 0); // smooth gradient — predictor helps

            using (var w = new TiffStreamWriter(noPredict.Path, false, Compression.LZW, TiffWriteMode.CreateNew, null) { Predictor = TiffPredictor.None })
                w.WriteFrame(src);

            using (var w = new TiffStreamWriter(withPredict.Path, false, Compression.LZW, TiffWriteMode.CreateNew, null) { Predictor = TiffPredictor.Horizontal })
                w.WriteFrame(src);

            Assert.IsTrue(
                new FileInfo(withPredict.Path).Length <= new FileInfo(noPredict.Path).Length,
                "Expected horizontal predictor to produce same or smaller file with LZW on gradient image");
        }

        [TestMethod]
        public void Predictor_None_WithDeflate_RoundTrip_PixelsExact()
        {
            using var tmp = TempTiff.New();
            var src = TestImages.Gray8(64, 64, 5);
            var expected = TestImages.ToPackedBytes(src);

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.DEFLATE, TiffWriteMode.CreateNew, null) { Predictor = TiffPredictor.None })
                writer.WriteFrame(src);

            CollectionAssert.AreEqual(expected, ReadScanlinePixels(tmp.Path));
        }

        private static byte[] ReadScanlinePixels(string path)
        {
            using var tiff = LibTiff.Open(path, "r");
            int w = tiff!.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int bps = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int spp = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int rowBytes = w * (bps / 8) * spp;
            var result = new byte[rowBytes * h];
            var buf = new byte[tiff.ScanlineSize()];
            for (int row = 0; row < h; row++)
            {
                tiff.ReadScanline(buf, row);
                Buffer.BlockCopy(buf, 0, result, row * rowBytes, rowBytes);
            }
            return result;
        }
    }
}
