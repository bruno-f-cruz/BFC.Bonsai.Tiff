using Bonsai;
using System;
using System.ComponentModel;
using System.Drawing.Design;
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
        private TiffWriteMode writeMode = TiffWriteMode.CreateNew;
        private int? chunkSize = null;

        /// <summary>Gets or sets the path to the output TIFF file.</summary>
        [Description("The path to the output file.")]
        [FileNameFilter("TIFF Files|*.tif;*.tiff|All Files|*.*")]
        [Editor("Bonsai.Design.SaveFileNameEditor, Bonsai.Design", typeof(UITypeEditor))]
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

        /// <summary>Gets or sets how the file is opened for writing.</summary>
        /// <remarks>
        /// <see cref="TiffWriteMode.CreateNew"/> throws if the target file already exists.
        /// <see cref="TiffWriteMode.Overwrite"/> replaces any existing file.
        /// <see cref="TiffWriteMode.Append"/> adds pages to an existing file, or creates it if absent.
        /// </remarks>
        [Description("Specifies whether to create, overwrite, or append to the TIFF file.")]
        public TiffWriteMode WriteMode
        {
            get { return writeMode; }
            set { writeMode = value; }
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

        /// <summary>Gets or sets the number of rows per strip. <see langword="null"/> writes the whole image as one strip.</summary>
        [Description("Number of rows per TIFF strip. Leave null for a single strip per frame.")]
        public int? RowsPerStrip { get; set; }

        /// <summary>Gets or sets tile dimensions for tiled TIFF output. Leave both Width and Height at 0 (the default) to use strip layout.</summary>
        [Description("Tile dimensions for tiled TIFF output (width and height must be multiples of 16). Leave both at 0 to use strip-based output.")]
        public TileSize Tiles { get; set; } = new TileSize();

        /// <summary>Gets or sets the compression predictor. Only effective with LZW or Deflate compression.</summary>
        [Description("Compression predictor. Horizontal is effective for LZW/Deflate on image data; FloatingPoint for float images.")]
        public TiffPredictor Predictor { get; set; } = TiffPredictor.None;

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
                () => new IO.TiffStreamWriter(fileName, useBigTiff, compression, writeMode, chunkSize)
                {
                    RowsPerStrip = RowsPerStrip,
                    Tiles = Tiles,
                    Predictor = Predictor
                },
                writer => source.Do(writer.WriteFrame));
        }

        /// <summary>
        /// Writes each <see cref="Tuple{IplImage, TiffMetadata}"/> as a page with per-frame metadata.
        /// </summary>
        public IObservable<Tuple<IplImage, TiffMetadata>> Process(
            IObservable<Tuple<IplImage, TiffMetadata>> source)
        {
            return Observable.Using(
                () => new IO.TiffStreamWriter(fileName, useBigTiff, compression, writeMode, chunkSize)
                {
                    RowsPerStrip = RowsPerStrip,
                    Tiles = Tiles,
                    Predictor = Predictor
                },
                writer => source.Do(item => writer.WriteFrame(item.Item1, item.Item2)));
        }
    }
}
