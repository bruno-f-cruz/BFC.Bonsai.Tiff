using System;
using System.IO;
using System.Reactive.Linq;
using BitMiracle.LibTiff.Classic;
using LibTiff = BitMiracle.LibTiff.Classic.Tiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class TiffWriteModeTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteMode_CreateNew_ThrowsWhenFileExists()
        {
            using var tmp = TempTiff.New();
            File.WriteAllText(tmp.Path, "existing");

            using var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null);
            writer.WriteFrame(TestImages.Gray8(4, 4));
        }

        [TestMethod]
        public void WriteMode_Overwrite_ReplacesExistingFile()
        {
            using var tmp = TempTiff.New();
            // Write initial 3-page file
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                for (int i = 0; i < 3; i++) writer.WriteFrame(TestImages.Gray8(4, 4, (byte)i));

            // Overwrite with 1 page
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.Overwrite, null))
                writer.WriteFrame(TestImages.Gray8(4, 4, 99));

            Assert.AreEqual(1, GetPageCount(tmp.Path));
        }

        [TestMethod]
        public void WriteMode_Append_AddsPagesToExistingFile()
        {
            using var tmp = TempTiff.New();
            // Write 2 initial pages
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                for (int i = 0; i < 2; i++) writer.WriteFrame(TestImages.Gray8(4, 4, (byte)i));

            // Append 3 more
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.Append, null))
                for (int i = 0; i < 3; i++) writer.WriteFrame(TestImages.Gray8(4, 4, (byte)(10 + i)));

            Assert.AreEqual(5, GetPageCount(tmp.Path));
        }

        [TestMethod]
        public void WriteMode_Append_NonExistentFile_CreatesIt()
        {
            using var tmp = TempTiff.New();
            // File does not exist; Append should create it
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.Append, null))
                writer.WriteFrame(TestImages.Gray8(4, 4));

            Assert.IsTrue(File.Exists(tmp.Path));
            Assert.AreEqual(1, GetPageCount(tmp.Path));
        }

        [TestMethod]
        public void WriteMode_Append_BigTiff_UsesA8Mode()
        {
            using var tmp = TempTiff.New();
            // Write initial BigTIFF file
            using (var writer = new TiffStreamWriter(tmp.Path, true, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(4, 4));

            // Append in BigTIFF mode
            using (var writer = new TiffStreamWriter(tmp.Path, true, Compression.NONE, TiffWriteMode.Append, null))
                writer.WriteFrame(TestImages.Gray8(4, 4, 1));

            Assert.AreEqual(2, GetPageCount(tmp.Path));

            // Verify BigTIFF magic (43)
            var header = new byte[4];
            using var fs = File.OpenRead(tmp.Path);
            fs.Read(header, 0, 4);
            bool isLittleEndian = header[0] == 0x49 && header[1] == 0x49;
            int magic = isLittleEndian ? (header[3] << 8 | header[2]) : (header[2] << 8 | header[3]);
            Assert.AreEqual(43, magic, "Expected BigTIFF magic 43");
        }

        private static int GetPageCount(string path)
        {
            using var tiff = LibTiff.Open(path, "r");
            return tiff!.NumberOfDirectories();
        }
    }
}
