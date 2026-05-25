using System;

namespace BFC.Bonsai.Tiff
{
    /// <summary>Specifies tile dimensions for tiled TIFF output.</summary>
    public readonly struct TileSize
    {
        /// <summary>Tile width in pixels. Must be a multiple of 16.</summary>
        public int Width { get; }
        /// <summary>Tile height in pixels. Must be a multiple of 16.</summary>
        public int Height { get; }

        /// <summary>Initializes a new <see cref="TileSize"/>.</summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="width"/> or <paramref name="height"/> is not a multiple of 16.
        /// </exception>
        public TileSize(int width, int height)
        {
            if (width % 16 != 0) throw new ArgumentException("Tile width must be a multiple of 16.", nameof(width));
            if (height % 16 != 0) throw new ArgumentException("Tile height must be a multiple of 16.", nameof(height));
            Width = width;
            Height = height;
        }
    }
}
