using System;
using System.IO;
using SkiaSharp;

namespace Bloomlings.ArtGen
{
    /// <summary>PNG encoding and decoding as straight-alpha RGBA, so committed files and fresh renders compare pixel by pixel.</summary>
    public static class Png
    {
        public static byte[] Encode(SKBitmap bitmap)
        {
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        /// <summary>
        /// The pixels of a PNG as straight-alpha RGBA rows from the top, decoded directly (no premultiplied step, which
        /// would round the colors of faint pixels).
        /// </summary>
        public static (int Width, int Height, byte[] Rgba) Decode(byte[] png)
        {
            using var stream = new SKMemoryStream(png);
            using SKCodec codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Not a PNG.");
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            byte[] bytes = new byte[info.BytesSize];
            var handle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                SKCodecResult result = codec.GetPixels(info, handle.AddrOfPinnedObject());
                if (result != SKCodecResult.Success)
                {
                    throw new InvalidDataException("PNG decode failed: " + result);
                }
            }
            finally
            {
                handle.Free();
            }

            return (info.Width, info.Height, bytes);
        }

        public static (int Width, int Height) Size(string file)
        {
            using var codec = SKCodec.Create(file) ?? throw new InvalidDataException("Not a PNG: " + file);
            return (codec.Info.Width, codec.Info.Height);
        }

        /// <summary>A bitmap from straight-alpha RGBA rows from the top (the 3D renders).</summary>
        public static SKBitmap FromRgba(int width, int height, byte[] rgba)
        {
            var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
            return bitmap;
        }

        /// <summary>An alpha mask (inside = alpha ≥ 128) of a picture scaled to <paramref name="size"/> pixels (readability check).</summary>
        public static bool[] Mask(byte[] png, int size)
        {
            using SKBitmap decoded = SKBitmap.Decode(png) ?? throw new InvalidDataException("Not a PNG.");
            using SKBitmap scaled = decoded.Resize(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
                ?? throw new InvalidOperationException("Resize failed.");
            var mask = new bool[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    mask[(y * size) + x] = scaled.GetPixel(x, y).Alpha >= 128;
                }
            }

            return mask;
        }
    }
}
