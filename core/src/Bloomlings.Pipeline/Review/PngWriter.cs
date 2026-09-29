using System.IO;
using System.IO.Compression;
using System.Text;
using Bloomlings.Pipeline.Pictures;

namespace Bloomlings.Pipeline.Review
{
    /// <summary>Writes PNGs deterministically (T082): RGBA images for review sheets and indexed images for authoring.</summary>
    public static class PngWriter
    {
        /// <summary>Writes 8-bit RGBA pixels, rows top first, four bytes per pixel.</summary>
        public static byte[] WriteRgba(int width, int height, byte[] rgba)
        {
            var raw = new byte[height * ((width * 4) + 1)];
            for (int y = 0; y < height; y++)
            {
                int row = y * ((width * 4) + 1);
                raw[row] = 0;
                System.Array.Copy(rgba, y * width * 4, raw, row + 1, width * 4);
            }

            return Assemble(width, height, bitDepth: 8, colorType: 6, palette: null, raw);
        }

        /// <summary>Writes an indexed image (color type 3) with bit depth 1, 2, 4 or 8.</summary>
        public static byte[] WriteIndexed(IndexedImage image, byte bitDepth = 8)
        {
            int perByte = 8 / bitDepth;
            int stride = ((image.Width * bitDepth) + 7) / 8;
            var raw = new byte[image.Height * (stride + 1)];
            for (int y = 0; y < image.Height; y++)
            {
                int row = y * (stride + 1);
                for (int x = 0; x < image.Width; x++)
                {
                    int index = image.Indexes[y][x];
                    if (index >= 1 << bitDepth)
                    {
                        throw new System.ArgumentException($"Index {index} does not fit bit depth {bitDepth}.", nameof(image));
                    }

                    int shift = 8 - (bitDepth * ((x % perByte) + 1));
                    raw[row + 1 + (x / perByte)] |= (byte)(index << shift);
                }
            }

            var palette = new byte[image.Palette.Count * 3];
            for (int i = 0; i < image.Palette.Count; i++)
            {
                palette[i * 3] = image.Palette[i].R;
                palette[(i * 3) + 1] = image.Palette[i].G;
                palette[(i * 3) + 2] = image.Palette[i].B;
            }

            return Assemble(image.Width, image.Height, bitDepth, colorType: 3, palette, raw);
        }

        private static byte[] Assemble(int width, int height, byte bitDepth, byte colorType, byte[]? palette, byte[] raw)
        {
            using var output = new MemoryStream();
            output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var header = new byte[13];
            WriteInt(header, 0, width);
            WriteInt(header, 4, height);
            header[8] = bitDepth;
            header[9] = colorType;
            WriteChunk(output, "IHDR", header);
            if (palette != null)
            {
                WriteChunk(output, "PLTE", palette);
            }

            using (var compressed = new MemoryStream())
            {
                using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(raw);
                }

                WriteChunk(output, "IDAT", compressed.ToArray());
            }

            WriteChunk(output, "IEND", System.Array.Empty<byte>());
            return output.ToArray();
        }

        private static void WriteChunk(Stream output, string type, byte[] body)
        {
            var chunk = new byte[body.Length + 4];
            Encoding.ASCII.GetBytes(type, 0, 4, chunk, 0);
            System.Array.Copy(body, 0, chunk, 4, body.Length);
            var length = new byte[4];
            WriteInt(length, 0, body.Length);
            output.Write(length);
            output.Write(chunk);
            var crc = new byte[4];
            WriteInt(crc, 0, (int)Crc32.Compute(chunk, 0, chunk.Length));
            output.Write(crc);
        }

        private static void WriteInt(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
