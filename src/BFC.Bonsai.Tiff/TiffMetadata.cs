using System;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;

namespace BFC.Bonsai.Tiff
{
    /// <summary>Metadata tags written to or read from a TIFF page directory.</summary>
    public sealed class TiffMetadata
    {
        /// <summary>Horizontal resolution in pixels per <see cref="ResolutionUnit"/>.</summary>
        public float? ResolutionX { get; set; }
        /// <summary>Vertical resolution in pixels per <see cref="ResolutionUnit"/>.</summary>
        public float? ResolutionY { get; set; }
        /// <summary>Unit of measure for <see cref="ResolutionX"/> and <see cref="ResolutionY"/>.</summary>
        public TiffResolutionUnit? ResolutionUnit { get; set; }
        /// <summary>Free-text description of the image.</summary>
        public string Description { get; set; }
        /// <summary>Person who created the image.</summary>
        public string Artist { get; set; }
        /// <summary>Name and version of the software that created the image.</summary>
        public string Software { get; set; }
        /// <summary>Copyright notice.</summary>
        public string Copyright { get; set; }
        /// <summary>Date and time of image creation.</summary>
        public DateTime? DateTime { get; set; }
        /// <summary>Name of the page (e.g. channel label or z-slice index).</summary>
        public string PageName { get; set; }
        /// <summary>
        /// Escape hatch for arbitrary TIFF tags. Key is the integer value of <see cref="TiffTag"/>.
        /// </summary>
        public IDictionary<int, object> CustomTags { get; set; }
    }
}
