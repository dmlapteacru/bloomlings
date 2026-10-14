using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Bloomlings.Playtest.Design
{
    /// <summary>Losslessly packed indexed hero frames. The runtime still holds one byte per pixel, not RGBA.</summary>
    public static class PackedPalette
    {
        private static readonly byte[] Magic = { 66, 80, 70, 49 }; // BPF1

        public static byte[] Encode(int width, int height, byte[] palette, byte[] indices)
        {
            if (palette.Length != 1024 || indices.Length != checked(width * height)) throw new ArgumentException("Invalid indexed frame");
            using var output = new MemoryStream();
            output.Write(Magic);
            var dimensions = new byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(dimensions.AsSpan(0, 4), width);
            BinaryPrimitives.WriteInt32LittleEndian(dimensions.AsSpan(4, 4), height);
            output.Write(dimensions);
            using (var packed = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                packed.Write(palette);
                packed.Write(indices);
            }
            return output.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out int width, out int height, out byte[] palette, out byte[] indices)
        {
            width = height = 0;
            palette = indices = Array.Empty<byte>();
            if (bytes.Length < 12 || !bytes.AsSpan(0, 4).SequenceEqual(Magic)) return false;
            width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4));
            height = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8, 4));
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096) throw new InvalidDataException("Invalid packed frame dimensions");
            palette = new byte[1024];
            indices = new byte[checked(width * height)];
            using var input = new MemoryStream(bytes, 12, bytes.Length - 12);
            using var packed = new BrotliStream(input, CompressionMode.Decompress);
            packed.ReadExactly(palette);
            packed.ReadExactly(indices);
            if (packed.ReadByte() != -1) throw new InvalidDataException("Extra packed frame data");
            return true;
        }
    }
}
