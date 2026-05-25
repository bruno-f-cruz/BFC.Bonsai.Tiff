using System;
using System.IO;
using System.Runtime.InteropServices;
using BitMiracle.LibTiff.Classic;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.IO
{
    /// <summary>
    /// Low-level TIFF multi-page writer. Wraps LibTiff to write <see cref="IplImage"/> frames
    /// as pages into a TIFF stack file.
    /// </summary>
    public sealed class TiffStreamWriter : IDisposable
    {
        static TiffStreamWriter()
        {
            BitMiracle.LibTiff.Classic.Tiff.SetErrorHandler(ThrowingErrorHandler.Instance);
        }

        private BitMiracle.LibTiff.Classic.Tiff _tiff;
        private int _currentFrameIdx;
        private int _currentChunkIdx;
        private byte[] _rowBuffer;
        private byte[] _stripBuffer;

        private readonly string _fileNamePattern;
        private readonly bool _useBigTiff;
        private readonly Compression _compression;
        private readonly TiffWriteMode _writeMode;
        private readonly int? _chunkSize;

        /// <summary>Gets or sets the number of rows per strip. <see langword="null"/> writes the whole image as one strip.</summary>
        public int? RowsPerStrip { get; set; }

        /// <summary>Gets or sets tile dimensions for tiled output. <see langword="null"/> or a disabled <see cref="TileSize"/> uses strip layout.</summary>
        public TileSize Tiles { get; set; }

        /// <summary>Gets or sets the compression predictor. Only effective with LZW or Deflate compression.</summary>
        public TiffPredictor Predictor { get; set; } = TiffPredictor.None;

        /// <summary>
        /// Initializes a new <see cref="TiffStreamWriter"/>.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="useBigTiff">When <see langword="true"/> uses BigTIFF format (removes 4 GB limit).</param>
        /// <param name="compression">Compression algorithm.</param>
        /// <param name="writeMode">Controls whether to create, overwrite, or append to the file.</param>
        /// <param name="chunkSize">Frames per chunk file, or <see langword="null"/> for a single file.</param>
        public TiffStreamWriter(string path, bool useBigTiff, Compression compression, TiffWriteMode writeMode, int? chunkSize)
        {
            _useBigTiff = useBigTiff;
            _compression = compression;
            _writeMode = writeMode;
            _chunkSize = chunkSize;

            if (chunkSize.HasValue)
            {
                var directory = Path.GetDirectoryName(path);
                var baseName = Path.GetFileNameWithoutExtension(path);
                var extension = Path.GetExtension(path);
                _fileNamePattern = string.IsNullOrEmpty(directory)
                    ? string.Format("{0}_{{0:D4}}{1}", baseName, extension)
                    : Path.Combine(directory, string.Format("{0}_{{0:D4}}{1}", baseName, extension));
            }
            else
            {
                _fileNamePattern = path;
            }

            _currentFrameIdx = 0;
            _currentChunkIdx = 0;

            OpenNewFile();
        }

        /// <summary>Writes an <see cref="IplImage"/> as the next page in the TIFF file.</summary>
        public void WriteFrame(IplImage image) => WriteFrame(image, null);

        /// <summary>Writes an <see cref="IplImage"/> as the next page, embedding optional metadata tags.</summary>
        public void WriteFrame(IplImage image, TiffMetadata metadata)
        {
            if (_chunkSize.HasValue && _currentFrameIdx >= _chunkSize.Value)
            {
                CloseCurrentFile();
                _currentChunkIdx++;
                OpenNewFile();
            }

            var width = image.Width;
            var height = image.Height;
            var channels = image.Channels;
            var depth = image.Depth;

            GetTiffSampleInfo(depth, out int bitsPerSample, out SampleFormat sampleFormat);

            _tiff.SetField(TiffTag.IMAGEWIDTH, width);
            _tiff.SetField(TiffTag.IMAGELENGTH, height);
            _tiff.SetField(TiffTag.SAMPLESPERPIXEL, channels);
            _tiff.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
            _tiff.SetField(TiffTag.SAMPLEFORMAT, sampleFormat);
            _tiff.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
            _tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            _tiff.SetField(TiffTag.COMPRESSION, _compression);

            if (channels == 1)
                _tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
            else if (channels == 3)
                _tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
            else if (channels == 4)
            {
                _tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                _tiff.SetField(TiffTag.EXTRASAMPLES, 1, new short[] { (short)ExtraSample.UNASSALPHA });
            }
            else
                _tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);

            if (Predictor != TiffPredictor.None &&
                (_compression == Compression.LZW || _compression == Compression.DEFLATE))
                _tiff.SetField(TiffTag.PREDICTOR, (int)Predictor);

            _tiff.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
            _tiff.SetField(TiffTag.PAGENUMBER, _currentFrameIdx, 0);

            var bytesPerPixel = (bitsPerSample / 8) * channels;
            var rowBytes = width * bytesPerPixel;

            if (Tiles != null && Tiles.IsEnabled)
            {
                Tiles.Validate();
                WriteTiled(image, width, height, rowBytes, bytesPerPixel);
            }
            else
            {
                WriteStrips(image, width, height, rowBytes);
            }

            if (metadata != null)
                ApplyMetadata(metadata);

            _tiff.WriteDirectory();
            _currentFrameIdx++;
        }

        private void WriteStrips(IplImage image, int width, int height, int rowBytes)
        {
            int rps = RowsPerStrip ?? height;
            _tiff.SetField(TiffTag.ROWSPERSTRIP, rps);

            var widthStep = image.WidthStep;
            var imageData = image.ImageData;

            if (rps >= height && widthStep == rowBytes)
            {
                // Fast path: single contiguous strip
                var totalBytes = height * rowBytes;
                if (_stripBuffer == null || _stripBuffer.Length < totalBytes)
                    _stripBuffer = new byte[totalBytes];
                Marshal.Copy(imageData, _stripBuffer, 0, totalBytes);
                _tiff.WriteEncodedStrip(0, _stripBuffer, totalBytes);
            }
            else if (rps >= height)
            {
                // Whole image, non-contiguous rows
                if (_rowBuffer == null || _rowBuffer.Length < rowBytes)
                    _rowBuffer = new byte[rowBytes];
                for (int row = 0; row < height; row++)
                {
                    Marshal.Copy(IntPtr.Add(imageData, row * widthStep), _rowBuffer, 0, rowBytes);
                    _tiff.WriteScanline(_rowBuffer, row);
                }
            }
            else
            {
                // Multi-strip: write strip-by-strip
                int stripBytes = rps * rowBytes;
                if (_stripBuffer == null || _stripBuffer.Length < stripBytes)
                    _stripBuffer = new byte[stripBytes];
                if (_rowBuffer == null || _rowBuffer.Length < rowBytes)
                    _rowBuffer = new byte[rowBytes];
                int stripIdx = 0;
                for (int startRow = 0; startRow < height; startRow += rps, stripIdx++)
                {
                    int rowsInStrip = Math.Min(rps, height - startRow);
                    int actualBytes = rowsInStrip * rowBytes;
                    for (int r = 0; r < rowsInStrip; r++)
                    {
                        Marshal.Copy(IntPtr.Add(imageData, (startRow + r) * widthStep), _rowBuffer, 0, rowBytes);
                        Buffer.BlockCopy(_rowBuffer, 0, _stripBuffer, r * rowBytes, rowBytes);
                    }
                    _tiff.WriteEncodedStrip(stripIdx, _stripBuffer, actualBytes);
                }
            }
        }

        private void WriteTiled(IplImage image, int width, int height, int rowBytes, int bytesPerPixel)
        {
            var tile = Tiles!;
            _tiff.SetField(TiffTag.TILEWIDTH, tile.Width);
            _tiff.SetField(TiffTag.TILELENGTH, tile.Height);

            int tileBytes = tile.Width * tile.Height * bytesPerPixel;
            var tileBuf = new byte[tileBytes];
            var imageData = image.ImageData;
            int widthStep = image.WidthStep;
            int channels = image.Channels;

            for (int y = 0; y < height; y += tile.Height)
            for (int x = 0; x < width; x += tile.Width)
            {
                int copyW = Math.Min(tile.Width, width - x);
                int copyH = Math.Min(tile.Height, height - y);
                int tileRowBytes = tile.Width * bytesPerPixel;
                int srcRowBytes = copyW * bytesPerPixel;

                // Zero-fill (handles partial tiles at edges)
                Array.Clear(tileBuf, 0, tileBytes);
                for (int row = 0; row < copyH; row++)
                {
                    var src = IntPtr.Add(imageData, (y + row) * widthStep + x * bytesPerPixel);
                    Marshal.Copy(src, tileBuf, row * tileRowBytes, srcRowBytes);
                }
                _tiff.WriteTile(tileBuf, x, y, 0, 0);
            }
        }

        /// <inheritdoc/>
        public void Dispose() => CloseCurrentFile();

        private void ApplyMetadata(TiffMetadata meta)
        {
            if (meta.Description != null) _tiff.SetField(TiffTag.IMAGEDESCRIPTION, meta.Description);
            if (meta.Artist != null) _tiff.SetField(TiffTag.ARTIST, meta.Artist);
            if (meta.Software != null) _tiff.SetField(TiffTag.SOFTWARE, meta.Software);
            if (meta.Copyright != null) _tiff.SetField(TiffTag.COPYRIGHT, meta.Copyright);
            if (meta.PageName != null) _tiff.SetField(TiffTag.PAGENAME, meta.PageName);
            if (meta.DateTime.HasValue)
                _tiff.SetField(TiffTag.DATETIME,
                    meta.DateTime.Value.ToString("yyyy:MM:dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture));
            if (meta.ResolutionX.HasValue) _tiff.SetField(TiffTag.XRESOLUTION, meta.ResolutionX.Value);
            if (meta.ResolutionY.HasValue) _tiff.SetField(TiffTag.YRESOLUTION, meta.ResolutionY.Value);
            if (meta.ResolutionUnit.HasValue)
                _tiff.SetField(TiffTag.RESOLUTIONUNIT, (int)meta.ResolutionUnit.Value);
            if (meta.CustomTags != null)
                foreach (var kv in meta.CustomTags)
                    _tiff.SetField((TiffTag)kv.Key, kv.Value);
        }

        private string GetCurrentFilePath()
            => _chunkSize.HasValue ? string.Format(_fileNamePattern, _currentChunkIdx) : _fileNamePattern;

        private void OpenNewFile()
        {
            var path = GetCurrentFilePath();

            if (_writeMode == TiffWriteMode.CreateNew && File.Exists(path))
                throw new InvalidOperationException(string.Format("File already exists: {0}", path));

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string mode;
            if (_writeMode == TiffWriteMode.Append && File.Exists(path))
                mode = _useBigTiff ? "a8" : "a";
            else
                mode = _useBigTiff ? "w8" : "w";

            _tiff = BitMiracle.LibTiff.Classic.Tiff.Open(path, mode);
            if (_tiff == null)
                throw new TiffException(string.Format("Failed to create TIFF file: {0}", path));
            _currentFrameIdx = _writeMode == TiffWriteMode.Append ? _tiff.NumberOfDirectories() : 0;
        }

        private void CloseCurrentFile()
        {
            _tiff?.Dispose();
            _tiff = null;
        }

        private static void GetTiffSampleInfo(IplDepth depth, out int bitsPerSample, out SampleFormat sampleFormat)
        {
            switch (depth)
            {
                case IplDepth.U8:  bitsPerSample = 8;  sampleFormat = SampleFormat.UINT;   break;
                case IplDepth.S8:  bitsPerSample = 8;  sampleFormat = SampleFormat.INT;    break;
                case IplDepth.U16: bitsPerSample = 16; sampleFormat = SampleFormat.UINT;   break;
                case IplDepth.S16: bitsPerSample = 16; sampleFormat = SampleFormat.INT;    break;
                case IplDepth.S32: bitsPerSample = 32; sampleFormat = SampleFormat.INT;    break;
                case IplDepth.F32: bitsPerSample = 32; sampleFormat = SampleFormat.IEEEFP; break;
                case IplDepth.F64: bitsPerSample = 64; sampleFormat = SampleFormat.IEEEFP; break;
                default:
                    throw new NotSupportedException(string.Format("Unsupported image depth: {0}", depth));
            }
        }
    }
}
