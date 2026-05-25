using System;
using BitMiracle.LibTiff.Classic;
using LibTiff = BitMiracle.LibTiff.Classic.Tiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class MetadataWriteTests
    {
        [TestMethod]
        public void Metadata_Description_SurvivesWrite()
        {
            using var tmp = TempTiff.New();
            var meta = new TiffMetadata { Description = "hello world" };

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(8, 8), meta);

            using var tiff = LibTiff.Open(tmp.Path, "r");
            var field = tiff!.GetField(TiffTag.IMAGEDESCRIPTION);
            var desc = field != null ? field[0].ToString() : null;
            Assert.AreEqual("hello world", desc);
        }

        [TestMethod]
        public void Metadata_Artist_SurvivesWrite()
        {
            using var tmp = TempTiff.New();
            var meta = new TiffMetadata { Artist = "Test Artist" };

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(8, 8), meta);

            using var tiff = LibTiff.Open(tmp.Path, "r");
            var field = tiff!.GetField(TiffTag.ARTIST);
            var val = field != null ? field[0].ToString() : null;
            Assert.AreEqual("Test Artist", val);
        }

        [TestMethod]
        public void Metadata_ThreeFrames_DistinctPageNames()
        {
            using var tmp = TempTiff.New();
            string[] names = { "FrameA", "FrameB", "FrameC" };

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
            {
                foreach (var name in names)
                    writer.WriteFrame(TestImages.Gray8(8, 8), new TiffMetadata { PageName = name });
            }

            using var tiff = LibTiff.Open(tmp.Path, "r");
            for (short i = 0; i < names.Length; i++)
            {
                tiff!.SetDirectory(i);
                var pnField = tiff.GetField(TiffTag.PAGENAME);
                var pageName = pnField != null ? pnField[0].ToString() : null;
                Assert.AreEqual(names[i], pageName, $"Page {i} name mismatch");
            }
        }

        [TestMethod]
        public void Metadata_ResolutionXY_SurvivesWrite()
        {
            using var tmp = TempTiff.New();
            var meta = new TiffMetadata
            {
                ResolutionX = 96f,
                ResolutionY = 96f,
                ResolutionUnit = TiffResolutionUnit.Inch
            };

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(8, 8), meta);

            using var tiff = LibTiff.Open(tmp.Path, "r");
            float rx = tiff!.GetField(TiffTag.XRESOLUTION)[0].ToFloat();
            float ry = tiff.GetField(TiffTag.YRESOLUTION)[0].ToFloat();
            int ru = tiff.GetField(TiffTag.RESOLUTIONUNIT)[0].ToInt();
            Assert.AreEqual(96f, rx, 0.001f);
            Assert.AreEqual(96f, ry, 0.001f);
            Assert.AreEqual((int)TiffResolutionUnit.Inch, ru);
        }

        [TestMethod]
        public void Metadata_DateTime_SurvivesWrite()
        {
            using var tmp = TempTiff.New();
            var dt = new DateTime(2025, 6, 1, 12, 30, 0);
            var meta = new TiffMetadata { DateTime = dt };

            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(8, 8), meta);

            using var tiff = LibTiff.Open(tmp.Path, "r");
            var dtField = tiff!.GetField(TiffTag.DATETIME);
            var s = dtField != null ? dtField[0].ToString() : null;
            Assert.AreEqual("2025:06:01 12:30:00", s);
        }

        [TestMethod]
        public void Metadata_NullMetadata_DoesNotThrow()
        {
            using var tmp = TempTiff.New();
            using (var writer = new TiffStreamWriter(tmp.Path, false, Compression.NONE, TiffWriteMode.CreateNew, null))
                writer.WriteFrame(TestImages.Gray8(8, 8), null);

            using var tiff = LibTiff.Open(tmp.Path, "r");
            Assert.AreEqual(1, tiff!.NumberOfDirectories());
        }
    }
}
