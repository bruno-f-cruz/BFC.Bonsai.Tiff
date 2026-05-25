using LibTiff = BitMiracle.LibTiff.Classic.Tiff;
using BitMiracle.LibTiff.Classic;

namespace BFC.Bonsai.Tiff.IO
{
    internal sealed class ThrowingErrorHandler : TiffErrorHandler
    {
        internal static readonly ThrowingErrorHandler Instance = new ThrowingErrorHandler();

        public override void ErrorHandler(LibTiff tiff, string module, string fmt, params object[] ap)
            => throw new TiffException(string.Format("[{0}] {1}", module, string.Format(fmt, ap)));

        public override void WarningHandler(LibTiff tiff, string module, string fmt, params object[] ap)
        {
            // Suppress warnings — do not log to stderr
        }
    }
}
