using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff.Tests;

internal static class TestImages
{
    /// <summary>Creates a grayscale 8-bit image filled with a deterministic gradient seeded by <paramref name="fill"/>.</summary>
    public static IplImage Gray8(int w, int h, byte fill = 0)
    {
        var img = new IplImage(new Size(w, h), IplDepth.U8, 1);
        FillGradient8(img, fill);
        return img;
    }

    /// <summary>Creates a grayscale 16-bit image filled with a deterministic gradient seeded by <paramref name="fill"/>.</summary>
    public static IplImage Gray16(int w, int h, ushort fill = 0)
    {
        var img = new IplImage(new Size(w, h), IplDepth.U16, 1);
        FillGradient16(img, fill);
        return img;
    }

    /// <summary>Creates an RGB 8-bit image filled with a deterministic color gradient.</summary>
    public static IplImage Rgb8(int w, int h)
    {
        var img = new IplImage(new Size(w, h), IplDepth.U8, 3);
        FillRgbGradient(img);
        return img;
    }

    /// <summary>Creates a 32-bit floating-point grayscale image filled with sequential values starting at <paramref name="fill"/>.</summary>
    public static IplImage Float32(int w, int h, float fill = 0f)
    {
        var img = new IplImage(new Size(w, h), IplDepth.F32, 1);
        FillFloat32(img, fill);
        return img;
    }

    private static void FillGradient8(IplImage img, byte baseVal)
    {
        int stride = img.WidthStep;
        var buf = new byte[stride * img.Height];
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                buf[y * stride + x] = (byte)((baseVal + x + y) & 0xFF);
        Marshal.Copy(buf, 0, img.ImageData, buf.Length);
    }

    private static void FillGradient16(IplImage img, ushort baseVal)
    {
        int stride = img.WidthStep;
        var buf = new byte[stride * img.Height];
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                ushort val = (ushort)(baseVal + x + y);
                int offset = y * stride + x * 2;
                buf[offset] = (byte)(val & 0xFF);
                buf[offset + 1] = (byte)(val >> 8);
            }
        Marshal.Copy(buf, 0, img.ImageData, buf.Length);
    }

    private static void FillRgbGradient(IplImage img)
    {
        int stride = img.WidthStep;
        var buf = new byte[stride * img.Height];
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                buf[y * stride + x * 3 + 0] = (byte)(x & 0xFF);
                buf[y * stride + x * 3 + 1] = (byte)(y & 0xFF);
                buf[y * stride + x * 3 + 2] = (byte)((x + y) & 0xFF);
            }
        Marshal.Copy(buf, 0, img.ImageData, buf.Length);
    }

    private static void FillFloat32(IplImage img, float baseVal)
    {
        int w = img.Width, h = img.Height;
        int stride = img.WidthStep;
        var buf = new byte[stride * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float val = baseVal + y * w + x;
                var bytes = BitConverter.GetBytes(val);
                int offset = y * stride + x * 4;
                buf[offset + 0] = bytes[0];
                buf[offset + 1] = bytes[1];
                buf[offset + 2] = bytes[2];
                buf[offset + 3] = bytes[3];
            }
        }
        Marshal.Copy(buf, 0, img.ImageData, buf.Length);
    }

    /// <summary>
    /// Returns the raw pixel bytes of an <see cref="IplImage"/>, row-packed (no padding).
    /// Use for byte-level comparison in round-trip tests.
    /// </summary>
    public static byte[] ToPackedBytes(IplImage img)
    {
        int bitsPerChannel = img.Depth switch
        {
            IplDepth.U8 or IplDepth.S8 => 8,
            IplDepth.U16 or IplDepth.S16 => 16,
            IplDepth.S32 or IplDepth.F32 => 32,
            IplDepth.F64 => 64,
            _ => throw new NotSupportedException($"Unsupported depth {img.Depth}")
        };
        int rowBytes = img.Width * (bitsPerChannel / 8) * img.Channels;
        var result = new byte[rowBytes * img.Height];
        for (int row = 0; row < img.Height; row++)
        {
            var src = IntPtr.Add(img.ImageData, row * img.WidthStep);
            Marshal.Copy(src, result, row * rowBytes, rowBytes);
        }
        return result;
    }
}

/// <summary>
/// A disposable wrapper around a unique temp-directory TIFF path.
/// The file (if created) is deleted on <see cref="Dispose"/>.
/// </summary>
internal sealed class TempTiff : IDisposable
{
    /// <summary>Full path to the unique temp TIFF file (not yet created).</summary>
    public string Path { get; }

    private TempTiff()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            Guid.NewGuid().ToString("N") + ".tiff");
    }

    /// <summary>Creates a new <see cref="TempTiff"/> with a unique path.</summary>
    public static TempTiff New() => new TempTiff();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (File.Exists(Path)) File.Delete(Path);
        // Also clean up chunk files (e.g. _0000.tiff, _0001.tiff)
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        var stem = System.IO.Path.GetFileNameWithoutExtension(Path);
        foreach (var f in Directory.GetFiles(dir, stem + "_????.tiff"))
            File.Delete(f);
    }
}
