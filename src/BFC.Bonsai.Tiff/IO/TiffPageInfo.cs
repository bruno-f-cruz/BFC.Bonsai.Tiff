using BitMiracle.LibTiff.Classic;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.IO
{
    /// <summary>Tag metadata describing a single TIFF page without decoding pixels.</summary>
    public sealed class TiffPageInfo
    {
        /// <summary>Image width in pixels.</summary>
        public int Width { get; internal set; }
        /// <summary>Image height in pixels.</summary>
        public int Height { get; internal set; }
        /// <summary>Number of samples (channels) per pixel.</summary>
        public int Channels { get; internal set; }
        /// <summary>Bit depth mapped to the corresponding <see cref="IplDepth"/>.</summary>
        public IplDepth Depth { get; internal set; }
        /// <summary>Compression scheme used for this page.</summary>
        public Compression Compression { get; internal set; }
        /// <summary>Photometric interpretation.</summary>
        public Photometric Photometric { get; internal set; }
        /// <summary>Whether the page uses tile-based layout.</summary>
        public bool Tiled { get; internal set; }
    }
}
