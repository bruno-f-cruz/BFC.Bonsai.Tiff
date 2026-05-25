using System;
using System.IO;
using System.Reactive.Linq;
using BitMiracle.LibTiff.Classic;
using LibTiff = BitMiracle.LibTiff.Classic.Tiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.Tests;

/// <summary>
/// Characterization tests that lock in current <see cref="TiffWriter"/> behavior before refactoring.
/// These tests must pass before and after the extraction of <see cref="IO.TiffStreamWriter"/>.
/// </summary>
[TestClass]
public class TiffWriterCharacterizationTests
{
    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static void WriteViaOperator(
        TiffWriter op,
        params IplImage[] frames)
    {
        op.Process(frames.ToObservable()).Wait();
    }

    private static byte[] ReadPackedPixels(string path, int page = 0)
    {
        using var tiff = LibTiff.Open(path, "r");
        Assert.IsNotNull(tiff, $"Could not open TIFF: {path}");
        tiff.SetDirectory((short)page);
        int w = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
        int h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
        int bps = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
        int spp = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
        int rowBytes = w * (bps / 8) * spp;
        var result = new byte[rowBytes * h];
        var scanBuf = new byte[tiff.ScanlineSize()];
        for (int row = 0; row < h; row++)
        {
            tiff.ReadScanline(scanBuf, row);
            Buffer.BlockCopy(scanBuf, 0, result, row * rowBytes, rowBytes);
        }
        return result;
    }

    private static int GetPageCount(string path)
    {
        using var tiff = LibTiff.Open(path, "r");
        return tiff!.NumberOfDirectories();
    }

    // ---------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------

    [TestMethod]
    public void SingleFrame_Gray8_RoundTrip()
    {
        using var tmp = TempTiff.New();
        var src = TestImages.Gray8(32, 16);
        var expected = TestImages.ToPackedBytes(src);

        WriteViaOperator(new TiffWriter { FileName = tmp.Path }, src);

        CollectionAssert.AreEqual(expected, ReadPackedPixels(tmp.Path));
    }

    [TestMethod]
    public void MultiFrame_Stack_PageCountEquals5()
    {
        using var tmp = TempTiff.New();
        var frames = new[] {
            TestImages.Gray8(8, 8, 0),
            TestImages.Gray8(8, 8, 1),
            TestImages.Gray8(8, 8, 2),
            TestImages.Gray8(8, 8, 3),
            TestImages.Gray8(8, 8, 4),
        };

        WriteViaOperator(new TiffWriter { FileName = tmp.Path }, frames);

        Assert.AreEqual(5, GetPageCount(tmp.Path));
    }

    [TestMethod]
    public void BigTiff_False_ProducesStandardTiff()
    {
        using var tmp = TempTiff.New();
        WriteViaOperator(
            new TiffWriter { FileName = tmp.Path, UseBigTiff = false },
            TestImages.Gray8(8, 8));

        // Standard TIFF header magic: 0x49 0x49 or 0x4D 0x4D, followed by 0x2A
        var header = new byte[4];
        using var fs = File.OpenRead(tmp.Path);
        fs.Read(header, 0, 4);
        bool isLittleEndian = header[0] == 0x49 && header[1] == 0x49;
        bool isBigEndian    = header[0] == 0x4D && header[1] == 0x4D;
        Assert.IsTrue(isLittleEndian || isBigEndian, "Expected TIFF byte-order mark");
        int magic = isLittleEndian ? (header[3] << 8 | header[2]) : (header[2] << 8 | header[3]);
        Assert.AreEqual(42, magic, "Expected standard TIFF magic 42");
    }

    [TestMethod]
    public void Compression_LZW_RoundTrip()
    {
        using var tmp = TempTiff.New();
        var src = TestImages.Gray8(64, 64, 50);
        var expected = TestImages.ToPackedBytes(src);

        WriteViaOperator(
            new TiffWriter { FileName = tmp.Path, Compression = Compression.LZW },
            src);

        CollectionAssert.AreEqual(expected, ReadPackedPixels(tmp.Path));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Overwrite_False_ThrowsWhenFileExists()
    {
        using var tmp = TempTiff.New();
        File.WriteAllText(tmp.Path, "existing");

        WriteViaOperator(
            new TiffWriter { FileName = tmp.Path, Overwrite = false },
            TestImages.Gray8(4, 4));
    }

    [TestMethod]
    public void Overwrite_True_Succeeds()
    {
        using var tmp = TempTiff.New();
        File.WriteAllText(tmp.Path, "existing");

        WriteViaOperator(
            new TiffWriter { FileName = tmp.Path, Overwrite = true },
            TestImages.Gray8(4, 4));

        Assert.AreEqual(1, GetPageCount(tmp.Path));
    }

    [TestMethod]
    public void Gray16_RoundTrip()
    {
        using var tmp = TempTiff.New();
        var src = TestImages.Gray16(32, 16, 1000);
        var expected = TestImages.ToPackedBytes(src);

        WriteViaOperator(new TiffWriter { FileName = tmp.Path }, src);

        CollectionAssert.AreEqual(expected, ReadPackedPixels(tmp.Path));
    }

    [TestMethod]
    public void Float32_RoundTrip()
    {
        using var tmp = TempTiff.New();
        var src = TestImages.Float32(16, 8, 1.5f);
        var expected = TestImages.ToPackedBytes(src);

        WriteViaOperator(new TiffWriter { FileName = tmp.Path }, src);

        CollectionAssert.AreEqual(expected, ReadPackedPixels(tmp.Path));
    }

    [TestMethod]
    public void Rgb8_RoundTrip_ChannelsPreserved()
    {
        using var tmp = TempTiff.New();
        var src = TestImages.Rgb8(32, 16);
        var expected = TestImages.ToPackedBytes(src);

        WriteViaOperator(new TiffWriter { FileName = tmp.Path }, src);

        // Verify LibTiff reports 3 channels
        using var tiff = LibTiff.Open(tmp.Path, "r");
        int spp = tiff!.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
        Assert.AreEqual(3, spp);
        CollectionAssert.AreEqual(expected, ReadPackedPixels(tmp.Path));
    }

    [TestMethod]
    public void ChunkSize2_Across5Frames_Produces3Files()
    {
        using var tmp = TempTiff.New();
        var frames = new[] {
            TestImages.Gray8(8, 8, 0),
            TestImages.Gray8(8, 8, 1),
            TestImages.Gray8(8, 8, 2),
            TestImages.Gray8(8, 8, 3),
            TestImages.Gray8(8, 8, 4),
        };

        WriteViaOperator(
            new TiffWriter { FileName = tmp.Path, ChunkSize = 2 },
            frames);

        var dir = Path.GetDirectoryName(tmp.Path)!;
        var stem = Path.GetFileNameWithoutExtension(tmp.Path);
        var ext = Path.GetExtension(tmp.Path);
        Assert.IsTrue(File.Exists(Path.Combine(dir, $"{stem}_0000{ext}")), "chunk 0 missing");
        Assert.IsTrue(File.Exists(Path.Combine(dir, $"{stem}_0001{ext}")), "chunk 1 missing");
        Assert.IsTrue(File.Exists(Path.Combine(dir, $"{stem}_0002{ext}")), "chunk 2 missing");
        Assert.AreEqual(2, GetPageCount(Path.Combine(dir, $"{stem}_0000{ext}")));
        Assert.AreEqual(2, GetPageCount(Path.Combine(dir, $"{stem}_0001{ext}")));
        Assert.AreEqual(1, GetPageCount(Path.Combine(dir, $"{stem}_0002{ext}")));
    }

    [TestMethod]
    public void BigTiff_True_ProducesBigTiff()
    {
        using var tmp = TempTiff.New();
        WriteViaOperator(
            new TiffWriter { FileName = tmp.Path, UseBigTiff = true },
            TestImages.Gray8(8, 8));

        // BigTIFF header: same byte-order mark, then magic = 43 (0x2B)
        var header = new byte[4];
        using var fs = File.OpenRead(tmp.Path);
        fs.Read(header, 0, 4);
        bool isLittleEndian = header[0] == 0x49 && header[1] == 0x49;
        int magic = isLittleEndian ? (header[3] << 8 | header[2]) : (header[2] << 8 | header[3]);
        Assert.AreEqual(43, magic, "Expected BigTIFF magic 43");
    }
}
