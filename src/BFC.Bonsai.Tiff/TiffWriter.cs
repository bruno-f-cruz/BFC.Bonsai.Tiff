using Bonsai;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using OpenCV.Net;
using BitMiracle.LibTiff.Classic;

namespace BFC.Bonsai.Tiff
{
    /// <summary>
    /// Writes a sequence of <see cref="IplImage"/> frames to a multi-page TIFF stack file.
    /// </summary>
    /// <remarks>
    /// Each frame in the input sequence is appended as a new page in the TIFF file.
    /// Supports both standard TIFF and BigTIFF formats, multiple compression algorithms,
    /// and optional chunked (rolling) output where frames are split across multiple files.
    /// </remarks>
    [Combinator]
    [Description("Writes a sequence of IplImage frames to a multi-page TIFF stack file.")]
    [WorkflowElementCategory(ElementCategory.Sink)]
    public class TiffWriter
    {
        private string fileName = string.Empty;
        private bool useBigTiff = true;
        private Compression compression = Compression.NONE;
        private bool overwrite = false;
        private int? chunkSize = null;

        /// <summary>Gets or sets the path to the output TIFF file.</summary>
        [Description("The path to the output file.")]
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use BigTIFF format.
        /// </summary>
        /// <remarks>BigTIFF removes the 4 GB file size limit of standard TIFF. Enabled by default.</remarks>
        [Description("Specifies whether to use BigTIFF format.")]
        public bool UseBigTiff
        {
            get { return useBigTiff; }
            set { useBigTiff = value; }
        }

        /// <summary>Gets or sets the compression algorithm applied to each frame.</summary>
        [Description("The compression algorithm to use.")]
        public Compression Compression
        {
            get { return compression; }
            set { compression = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output file if it already exists.
        /// </summary>
        /// <remarks>
        /// When <see langword="false"/> (the default), an <see cref="InvalidOperationException"/> is thrown
        /// if the target file already exists.
        /// </remarks>
        [Description("Specifies whether to overwrite the file if it already exists.")]
        public bool Overwrite
        {
            get { return overwrite; }
            set { overwrite = value; }
        }

        /// <summary>Gets or sets the number of frames per chunk file.</summary>
        /// <remarks>
        /// When set, the writer operates in rolling mode: after every <c>ChunkSize</c> frames the current
        /// file is closed and a new one is opened with a zero-padded four-digit suffix
        /// (e.g., <c>output_0000.tiff</c>, <c>output_0001.tiff</c>, …).
        /// Leave <see langword="null"/> to write all frames to a single file.
        /// </remarks>
        [Description("The number of frames per chunk file. When set, the writer operates in rolling mode, creating a new file after each chunk with '_####' suffix (e.g., 'output_0000.tiff'). Leave null to write all frames to a single file.")]
        public int? ChunkSize
        {
            get { return chunkSize; }
            set { chunkSize = value; }
        }

        /// <summary>
        /// Writes each <see cref="IplImage"/> in <paramref name="source"/> as a page in a multi-page TIFF file
        /// and passes each image downstream unchanged.
        /// </summary>
        /// <param name="source">A sequence of images to write.</param>
        /// <returns>
        /// A sequence identical to <paramref name="source"/>, produced as a side-effect of writing each frame.
        /// </returns>
        public IObservable<IplImage> Process(IObservable<IplImage> source)
        {
            return Observable.Using(
                () => new IO.TiffStreamWriter(fileName, useBigTiff, compression, overwrite, chunkSize),
                writer => source.Do(writer.WriteFrame));
        }
    }
}

