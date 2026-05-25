namespace BFC.Bonsai.Tiff
{
    /// <summary>Specifies the compression predictor applied before encoding.</summary>
    public enum TiffPredictor
    {
        /// <summary>No predictor.</summary>
        None = 1,
        /// <summary>Horizontal differencing. Effective for LZW and Deflate on continuous-tone images.</summary>
        Horizontal = 2,
        /// <summary>Floating-point horizontal differencing. For Float32/Float64 data.</summary>
        FloatingPoint = 3
    }
}
