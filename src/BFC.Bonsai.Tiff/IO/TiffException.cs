using System;

namespace BFC.Bonsai.Tiff.IO
{
    /// <summary>Thrown when LibTiff reports an error condition.</summary>
    public sealed class TiffException : InvalidOperationException
    {
        /// <inheritdoc/>
        public TiffException(string message) : base(message) { }
    }
}
