using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder("TiffMetadata { ");
            var first = true;
            void Append(string name, object value)
            {
                if (value == null) return;
                if (value is string s && string.IsNullOrEmpty(s)) return;
                if (!first) sb.Append(", ");
                sb.Append(name).Append('=').Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                first = false;
            }

            Append(nameof(Description), Description);
            Append(nameof(Artist), Artist);
            Append(nameof(Software), Software);
            Append(nameof(Copyright), Copyright);
            Append(nameof(PageName), PageName);
            Append(nameof(ResolutionX), ResolutionX);
            Append(nameof(ResolutionY), ResolutionY);
            Append(nameof(ResolutionUnit), ResolutionUnit);
            Append(nameof(DateTime), DateTime?.ToString("o", CultureInfo.InvariantCulture));
            if (CustomTags != null && CustomTags.Count > 0)
                Append(nameof(CustomTags), $"[{CustomTags.Count} tag(s)]");

            sb.Append(" }");
            return sb.ToString();
        }
    }
}
