using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BitMiracle.LibTiff.Classic;
using LibTiffClass = BitMiracle.LibTiff.Classic.Tiff;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.IO
{
    /// <summary>
    /// Low-level TIFF reader. Wraps LibTiff to provide random-access page reading for TIFF stacks.
    /// </summary>
    public sealed class TiffStreamReader : IDisposable
    {
        static TiffStreamReader()
        {
            LibTiffClass.SetErrorHandler(ThrowingErrorHandler.Instance);
        }

        private readonly LibTiffClass _tiff;
        private readonly int _pageCount;

        /// <summary>Opens a TIFF file for reading.</summary>
        /// <exception cref="TiffException">Thrown if the file cannot be opened.</exception>
        public TiffStreamReader(string path)
        {
            _tiff = LibTiffClass.Open(path, "r")
                ?? throw new TiffException(string.Format("Failed to open TIFF file: {0}", path));
            _pageCount = _tiff.NumberOfDirectories();
        }

        /// <summary>Total number of pages (IFD directories) in the file.</summary>
        public int PageCount => _pageCount;

        /// <summary>Returns tag metadata for the given page without decoding pixels.</summary>
        public TiffPageInfo GetPageInfo(int index)
        {
            SeekPage(index);
            return ReadPageInfo();
        }

        /// <summary>Returns <see cref="TiffMetadata"/> for the given page.</summary>
        public TiffMetadata GetMetadata(int index)
        {
            SeekPage(index);
            return ReadMetadata();
        }

        /// <summary>Decodes and returns a page as an <see cref="IplImage"/>.</summary>
        public IplImage ReadPage(int index)
        {
            SeekPage(index);
            return DecodePage();
        }

        /// <summary>Reads all pages sequentially.</summary>
        public IEnumerable<IplImage> ReadAllPages()
        {
            for (int i = 0; i < _pageCount; i++)
                yield return ReadPage(i);
        }

        /// <inheritdoc/>
        public void Dispose() => _tiff.Dispose();

        private void SeekPage(int index)
        {
            if (index < 0 || index >= _pageCount)
                throw new ArgumentOutOfRangeException("index", string.Format("Page index {0} is out of range [0, {1}).", index, _pageCount));
            _tiff.SetDirectory((short)index);
        }

        private TiffPageInfo ReadPageInfo()
        {
            int w = GetInt(TiffTag.IMAGEWIDTH);
            int h = GetInt(TiffTag.IMAGELENGTH);
            int spp = GetIntOr(TiffTag.SAMPLESPERPIXEL, 1);
            int bps = GetInt(TiffTag.BITSPERSAMPLE);
            int sfInt = GetIntOr(TiffTag.SAMPLEFORMAT, (int)SampleFormat.UINT);
            var compression = (Compression)GetInt(TiffTag.COMPRESSION);
            var photometric = (Photometric)GetInt(TiffTag.PHOTOMETRIC);

            return new TiffPageInfo
            {
                Width = w,
                Height = h,
                Channels = spp,
                Depth = MapDepth(bps, (SampleFormat)sfInt),
                Compression = compression,
                Photometric = photometric,
                Tiled = _tiff.IsTiled()
            };
        }

        private TiffMetadata ReadMetadata()
        {
            return new TiffMetadata
            {
                Description = GetString(TiffTag.IMAGEDESCRIPTION),
                Artist = GetString(TiffTag.ARTIST),
                Software = GetString(TiffTag.SOFTWARE),
                Copyright = GetString(TiffTag.COPYRIGHT),
                PageName = GetString(TiffTag.PAGENAME),
                ResolutionX = GetFloat(TiffTag.XRESOLUTION),
                ResolutionY = GetFloat(TiffTag.YRESOLUTION),
                ResolutionUnit = GetResolutionUnit(),
                DateTime = GetDateTime()
            };
        }

        private IplImage DecodePage()
        {
            var info = ReadPageInfo();

            if (info.Photometric == Photometric.PALETTE)
                throw new NotSupportedException(
                    "Palette-colour TIFFs are not supported. Convert to grayscale or RGB before reading.");

            var img = new IplImage(new Size(info.Width, info.Height), info.Depth, info.Channels);

            if (_tiff.IsTiled())
                DecodeTiled(img, info);
            else
                DecodeStrips(img, info);

            return img;
        }

        private void DecodeStrips(IplImage img, TiffPageInfo info)
        {
            int bytesPerSample = info.Depth.ToByteCount();
            int rowBytes = info.Width * bytesPerSample * info.Channels;
            var rowBuf = new byte[_tiff.ScanlineSize()];

            for (int row = 0; row < info.Height; row++)
            {
                _tiff.ReadScanline(rowBuf, row);
                Marshal.Copy(rowBuf, 0, IntPtr.Add(img.ImageData, row * img.WidthStep), rowBytes);
            }
        }

        private void DecodeTiled(IplImage img, TiffPageInfo info)
        {
            int tw = GetInt(TiffTag.TILEWIDTH);
            int th = GetInt(TiffTag.TILELENGTH);
            int bytesPerSample = info.Depth.ToByteCount();
            int tileBytes = tw * th * bytesPerSample * info.Channels;
            var tileBuf = new byte[tileBytes];

            for (int y = 0; y < info.Height; y += th)
            for (int x = 0; x < info.Width; x += tw)
            {
                _tiff.ReadTile(tileBuf, 0, x, y, 0, 0);
                int copyW = Math.Min(tw, info.Width - x);
                int copyH = Math.Min(th, info.Height - y);
                int tileRowBytes = tw * bytesPerSample * info.Channels;
                int imgRowBytes = copyW * bytesPerSample * info.Channels;
                for (int row = 0; row < copyH; row++)
                {
                    var dest = IntPtr.Add(img.ImageData, (y + row) * img.WidthStep + x * bytesPerSample * info.Channels);
                    Marshal.Copy(tileBuf, row * tileRowBytes, dest, imgRowBytes);
                }
            }
        }

        private int GetInt(TiffTag tag) => _tiff.GetField(tag)[0].ToInt();

        private int GetIntOr(TiffTag tag, int fallback)
        {
            var f = _tiff.GetField(tag);
            return f != null ? f[0].ToInt() : fallback;
        }

        private string GetString(TiffTag tag)
        {
            var f = _tiff.GetField(tag);
            return f != null ? f[0].ToString() : null;
        }

        private float? GetFloat(TiffTag tag)
        {
            var f = _tiff.GetField(tag);
            return f != null ? (float?)f[0].ToFloat() : null;
        }

        private TiffResolutionUnit? GetResolutionUnit()
        {
            var f = _tiff.GetField(TiffTag.RESOLUTIONUNIT);
            return f != null ? (TiffResolutionUnit?)f[0].ToInt() : null;
        }

        private DateTime? GetDateTime()
        {
            var s = GetString(TiffTag.DATETIME);
            if (s == null) return null;
            DateTime dt;
            return DateTime.TryParseExact(s, "yyyy:MM:dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out dt) ? dt : (DateTime?)null;
        }

        private static IplDepth MapDepth(int bps, SampleFormat sf)
        {
            if (bps == 8  && sf == SampleFormat.UINT)   return IplDepth.U8;
            if (bps == 8  && sf == SampleFormat.INT)    return IplDepth.S8;
            if (bps == 16 && sf == SampleFormat.UINT)   return IplDepth.U16;
            if (bps == 16 && sf == SampleFormat.INT)    return IplDepth.S16;
            if (bps == 32 && sf == SampleFormat.INT)    return IplDepth.S32;
            if (bps == 32 && sf == SampleFormat.IEEEFP) return IplDepth.F32;
            if (bps == 64 && sf == SampleFormat.IEEEFP) return IplDepth.F64;
            throw new NotSupportedException(string.Format("Unsupported bps={0}/sf={1} combination.", bps, sf));
        }
    }
}
