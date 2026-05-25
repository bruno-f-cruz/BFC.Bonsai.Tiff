using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using BitMiracle.LibTiff.Classic;
using Bonsai;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff
{
    /// <summary>
    /// Writes each <see cref="IplImage"/> as a single-page TIFF file, opening and closing the file per frame.
    /// Passes each frame downstream unchanged.
    /// </summary>
    [Combinator]
    [Description("Writes each IplImage as a single-page TIFF file. The file is opened and closed per frame.")]
    [WorkflowElementCategory(ElementCategory.Sink)]
    public class SaveTiff
    {
        /// <summary>Gets or sets the path to the output TIFF file.</summary>
        [Description("The path to the output TIFF file.")]
        [FileNameFilter("TIFF Files|*.tif;*.tiff|All Files|*.*")]
        [Editor("Bonsai.Design.SaveFileNameEditor, Bonsai.Design", typeof(UITypeEditor))]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether to use BigTIFF format.</summary>
        [Description("Specifies whether to use BigTIFF format.")]
        public bool UseBigTiff { get; set; } = false;

        /// <summary>Gets or sets the compression algorithm.</summary>
        [Description("The compression algorithm to use.")]
        public Compression Compression { get; set; } = Compression.NONE;

        /// <summary>
        /// Gets or sets how the file is opened for writing.
        /// <see cref="TiffWriteMode.Append"/> is not supported; use <see cref="TiffWriter"/> for stacks.
        /// </summary>
        [Description("Specifies whether to create a new file or overwrite an existing one. Append is not supported for per-frame writes.")]
        [TypeConverter(typeof(SaveTiffWriteModeConverter))]
        public TiffWriteMode WriteMode { get; set; } = TiffWriteMode.CreateNew;

        /// <summary>Writes each <see cref="IplImage"/> as a single-page TIFF and passes it downstream.</summary>
        public IObservable<IplImage> Process(IObservable<IplImage> source) =>
            source.Do(image => WriteSingle(image, null));

        /// <summary>Writes each <see cref="Tuple{IplImage, TiffMetadata}"/> as a single-page TIFF with metadata.</summary>
        public IObservable<Tuple<IplImage, TiffMetadata>> Process(
            IObservable<Tuple<IplImage, TiffMetadata>> source) =>
            source.Do(item => WriteSingle(item.Item1, item.Item2));

        private void WriteSingle(IplImage image, TiffMetadata metadata)
        {
            if (WriteMode == TiffWriteMode.Append)
                throw new InvalidOperationException(
                    "SaveTiff does not support Append mode. Use TiffWriter for multi-frame stacks.");

            using var writer = new IO.TiffStreamWriter(FileName, UseBigTiff, Compression, WriteMode, chunkSize: null);
            writer.WriteFrame(image, metadata);
        }
    }
}
