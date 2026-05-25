using System;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.IO
{
    internal static class IplDepthExtensions
    {
        internal static int ToByteCount(this IplDepth depth)
        {
            switch (depth)
            {
                case IplDepth.U8:
                case IplDepth.S8:  return 1;
                case IplDepth.U16:
                case IplDepth.S16: return 2;
                case IplDepth.S32:
                case IplDepth.F32: return 4;
                case IplDepth.F64: return 8;
                default: throw new NotSupportedException(string.Format("Unknown depth {0}", depth));
            }
        }
    }
}
