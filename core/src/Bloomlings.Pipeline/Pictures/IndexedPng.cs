using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bloomlings.Pipeline.Pictures
{
    /// <summary>An indexed-color image: palette indexes by row, top row first.</summary>
    public sealed record IndexedImage(int Width, int Height, int[][] Indexes, IReadOnlyList<(byte R, byte G, byte B)> Palette);

    /// <summary>
    /// Reads indexed-color PNGs (T072): color type 3, bit depth 1, 2, 4 or 8, no interlacing. The palette index is the
    /// picture role (R7). Chunk CRCs are verified.
    /// </summary>
    public static class IndexedPngReader
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static IndexedImage Read(string path) => Read(File.ReadAllBytes(path), path);

        public static IndexedImage Read(byte[] data, string name = "png")
        {
            if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(Signature))
            {
                throw new InvalidDataException($"{name}: not a PNG file.");
            }

            int width = 0;
            int height = 0;
            int bitDepth = 0;
            var palette = new List<(byte, byte, byte)>();
            using var idat = new MemoryStream();
            int offset = 8;
            bool end = false;
            while (!end)
            {
                if (offset + 12 > data.Length)
                {
                    throw new InvalidDataException($"{name}: truncated chunk.");
                }

                int length = ReadInt(data, offset);
                string type = Encoding.ASCII.GetString(data, offset + 4, 4);
                if (length < 0 || offset + 12 + length > data.Length)
                {
                    throw new InvalidDataException($"{name}: bad {type} chunk length.");
                }

                uint crc = (uint)ReadInt(data, offset + 8 + length);
                if (Crc32.Compute(data, offset + 4, length + 4) != crc)
                {
                    throw new InvalidDataException($"{name}: CRC mismatch in {type}.");
                }

                int body = offset + 8;
                switch (type)
                {
                    case "IHDR":
                        width = ReadInt(data, body);
                        height = ReadInt(data, body + 4);
                        bitDepth = data[body + 8];
                        int colorType = data[body + 9];
                        int interlace = data[body + 12];
                        if (colorType != 3)
                        {
                            throw new InvalidDataException($"{name}: color type {colorType}; pictures must be indexed-color (type 3).");
                        }

                        if (bitDepth != 1 && bitDepth != 2 && bitDepth != 4 && bitDepth != 8)
                        {
                            throw new InvalidDataException($"{name}: bit depth {bitDepth} is not 1, 2, 4 or 8.");
                        }

                        if (interlace != 0)
                        {
                            throw new InvalidDataException($"{name}: interlaced PNGs are not supported.");
                        }

                        break;
                    case "PLTE":
                        for (int i = 0; i + 2 < length; i += 3)
                        {
                            palette.Add((data[body + i], data[body + i + 1], data[body + i + 2]));
                        }

                        break;
                    case "IDAT":
                        idat.Write(data, body, length);
                        break;
                    case "IEND":
                        end = true;
                        break;
                }

                offset += 12 + length;
            }

            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException($"{name}: missing IHDR.");
            }

            byte[] raw = Inflate(idat.ToArray());
            int stride = ((width * bitDepth) + 7) / 8;
            if (raw.Length < height * (stride + 1))
            {
                throw new InvalidDataException($"{name}: image data is too short.");
            }

            var rows = new int[height][];
            var previous = new byte[stride];
            var current = new byte[stride];
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * (stride + 1);
                byte filter = raw[rowStart];
                Array.Copy(raw, rowStart + 1, current, 0, stride);
                Unfilter(filter, current, previous, name);
                rows[y] = Unpack(current, width, bitDepth);
                (previous, current) = (current, previous);
            }

            return new IndexedImage(width, height, rows, palette);
        }

        private static void Unfilter(byte filter, byte[] line, byte[] prior, string name)
        {
            // Indexed images have one byte per complete pixel for filtering (bpp rounds up to 1).
            for (int i = 0; i < line.Length; i++)
            {
                int a = i > 0 ? line[i - 1] : 0;
                int b = prior[i];
                int c = i > 0 ? prior[i - 1] : 0;
                int value = filter switch
                {
                    0 => line[i],
                    1 => line[i] + a,
                    2 => line[i] + b,
                    3 => line[i] + ((a + b) / 2),
                    4 => line[i] + Paeth(a, b, c),
                    _ => throw new InvalidDataException($"{name}: unknown filter type {filter}."),
                };
                line[i] = (byte)value;
            }
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        private static int[] Unpack(byte[] line, int width, int bitDepth)
        {
            var pixels = new int[width];
            int perByte = 8 / bitDepth;
            int mask = (1 << bitDepth) - 1;
            for (int x = 0; x < width; x++)
            {
                int b = line[x / perByte];
                int shift = 8 - (bitDepth * ((x % perByte) + 1));
                pixels[x] = (b >> shift) & mask;
            }

            return pixels;
        }

        private static byte[] Inflate(byte[] compressed)
        {
            using var input = new MemoryStream(compressed);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }

        private static int ReadInt(byte[] data, int offset) =>
            (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    /// <summary>CRC-32 as used by PNG chunks (ISO 3309).</summary>
    public static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        public static uint Compute(byte[] data, int offset, int count)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = offset; i < offset + count; i++)
            {
                crc = Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }
    }
}
