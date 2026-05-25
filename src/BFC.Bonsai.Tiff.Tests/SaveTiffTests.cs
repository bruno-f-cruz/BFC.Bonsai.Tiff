using System;
using System.IO;
using System.Reactive.Linq;
using BitMiracle.LibTiff.Classic;
using LibTiff = BitMiracle.LibTiff.Classic.Tiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.Tests
{
    [TestClass]
    public class SaveTiffTests
    {
        [TestMethod]
        public void SaveTiff_Overwrite_ConstantPath_OnlyLastFrameInFile()
        {
            using var tmp = TempTiff.New();
            var op = new SaveTiff { FileName = tmp.Path, WriteMode = TiffWriteMode.Overwrite };

            new[] { TestImages.Gray8(8, 8, 1), TestImages.Gray8(8, 8, 2), TestImages.Gray8(8, 8, 3) }
                .ToObservable()
                .Do(img => op.Process(Observable.Return(img)).Wait())
                .Wait();

            Assert.AreEqual(1, GetPageCount(tmp.Path));
        }

        [TestMethod]
        public void SaveTiff_CreateNew_SecondEmissionThrows()
        {
            using var tmp = TempTiff.New();
            var op = new SaveTiff { FileName = tmp.Path, WriteMode = TiffWriteMode.CreateNew };

            // First write succeeds
            op.Process(Observable.Return(TestImages.Gray8(4, 4))).Wait();

            // Second write to same file must throw
            Assert.ThrowsException<InvalidOperationException>(
                () => op.Process(Observable.Return(TestImages.Gray8(4, 4))).Wait());
        }

        [TestMethod]
        public void SaveTiff_UniquePathsPerFrame_Produces3SeparateFiles()
        {
            using var tmp0 = TempTiff.New();
            using var tmp1 = TempTiff.New();
            using var tmp2 = TempTiff.New();
            var paths = new[] { tmp0.Path, tmp1.Path, tmp2.Path };

            for (int i = 0; i < paths.Length; i++)
            {
                var op = new SaveTiff { FileName = paths[i], WriteMode = TiffWriteMode.CreateNew };
                op.Process(Observable.Return(TestImages.Gray8(4, 4, (byte)i))).Wait();
            }

            foreach (var p in paths)
            {
                Assert.IsTrue(File.Exists(p), $"Expected file: {p}");
                Assert.AreEqual(1, GetPageCount(p));
            }
        }

        [TestMethod]
        public void SaveTiff_Append_ThrowsInvalidOperationException()
        {
            using var tmp = TempTiff.New();
            var op = new SaveTiff { FileName = tmp.Path, WriteMode = TiffWriteMode.Append };
            Assert.ThrowsException<InvalidOperationException>(
                () => op.Process(Observable.Return(TestImages.Gray8(4, 4))).Wait());
        }

        [TestMethod]
        public void SaveTiff_WithMetadata_DescriptionSurvives()
        {
            using var tmp = TempTiff.New();
            var op = new SaveTiff { FileName = tmp.Path, WriteMode = TiffWriteMode.CreateNew };
            var item = Tuple.Create(TestImages.Gray8(8, 8), new TiffMetadata { Description = "saved!" });

            op.Process(Observable.Return(item)).Wait();

            using var tiff = LibTiff.Open(tmp.Path, "r");
            var field = tiff!.GetField(TiffTag.IMAGEDESCRIPTION);
            Assert.AreEqual("saved!", field != null ? field[0].ToString() : null);
        }

        private static int GetPageCount(string path)
        {
            using var tiff = LibTiff.Open(path, "r");
            return tiff!.NumberOfDirectories();
        }
    }
}
