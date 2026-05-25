using System;
using System.ComponentModel;
using System.Globalization;

namespace BFC.Bonsai.Tiff
{
    /// <summary>
    /// Specifies tile dimensions for tiled TIFF output. Both dimensions must be multiples of 16.
    /// Leave both <see cref="Width"/> and <see cref="Height"/> at 0 to disable tiling (use strips instead).
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TileSize
    {
        /// <summary>Tile width in pixels. Must be a multiple of 16 when tiling is enabled.</summary>
        [Description("Tile width in pixels. Must be a multiple of 16. Set both Width and Height to 0 to disable tiling.")]
        public int Width { get; set; }

        /// <summary>Tile height in pixels. Must be a multiple of 16 when tiling is enabled.</summary>
        [Description("Tile height in pixels. Must be a multiple of 16. Set both Width and Height to 0 to disable tiling.")]
        public int Height { get; set; }

        /// <summary>Default constructor (Width = Height = 0, tiling disabled).</summary>
        public TileSize() { }

        /// <summary>Initializes a new <see cref="TileSize"/>.</summary>
        public TileSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>True when both dimensions are positive (tiling enabled).</summary>
        internal bool IsEnabled => Width > 0 && Height > 0;

        /// <summary>Validates that the dimensions are usable for tiling.</summary>
        /// <exception cref="ArgumentException">If width/height are not multiples of 16.</exception>
        internal void Validate()
        {
            if (Width % 16 != 0) throw new ArgumentException("Tile width must be a multiple of 16.", nameof(Width));
            if (Height % 16 != 0) throw new ArgumentException("Tile height must be a multiple of 16.", nameof(Height));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", Width, Height);
        }
    }
}
