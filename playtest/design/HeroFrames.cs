using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// A palette picture as decoded from an 8-bit indexed PNG: one byte a pixel and its 256 colors (straight-alpha RGBA).
    /// The animated heroes' frames are such PNGs (spec 005 FR-028, <c>tools/heroanim</c>), so a painter keeps the frames
    /// it drew at a quarter of their RGBA size (<see cref="HeroFrameStore"/>) and expands only those on screen
    /// (<see cref="Expand"/>). Engine-free.
    /// </summary>
    public sealed class PalettePicture
    {
        public PalettePicture(int width, int height, byte[] pixels, byte[] palette)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            Palette = palette;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>The palette index of each pixel, rows from the top.</summary>
        public byte[] Pixels { get; }

        /// <summary>256 colors as straight-alpha RGBA bytes (entries past the PNG's palette are clear).</summary>
        public byte[] Palette { get; }

        /// <summary>The bytes the picture holds.</summary>
        public long Bytes => Pixels.Length + Palette.Length;

        /// <summary>
        /// Writes the picture as RGBA bytes into <paramref name="rgba"/> from its top-left, rows <paramref name="stride"/>
        /// pixels apart (at least <see cref="Width"/>), premultiplied by alpha when asked (an Android bitmap's pixels).
        /// What lies right of and below the picture is left as it was.
        /// </summary>
        public void Expand(byte[] rgba, int stride, bool premultiplied)
        {
            if (stride < Width || rgba.Length < ((Height - 1) * stride * 4) + (Width * 4))
            {
                throw new ArgumentException("The target is too small for a " + Width + " x " + Height + " picture.", nameof(rgba));
            }

            // Each color as one 32-bit word in memory order, so a pixel is one store.
            var colors = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                byte r = Palette[i * 4];
                byte g = Palette[(i * 4) + 1];
                byte b = Palette[(i * 4) + 2];
                byte a = Palette[(i * 4) + 3];
                if (premultiplied && a < 255)
                {
                    r = (byte)(((r * a) + 127) / 255);
                    g = (byte)(((g * a) + 127) / 255);
                    b = (byte)(((b * a) + 127) / 255);
                }

                colors[i] = BitConverter.IsLittleEndian
                    ? r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24)
                    : ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | a;
            }

            Span<uint> target = MemoryMarshal.Cast<byte, uint>(rgba.AsSpan());
            for (int y = 0; y < Height; y++)
            {
                ReadOnlySpan<byte> row = Pixels.AsSpan(y * Width, Width);
                Span<uint> line = target.Slice(y * stride, Width);
                for (int x = 0; x < row.Length; x++)
                {
                    line[x] = colors[row[x]];
                }
            }
        }
    }

    /// <summary>
    /// Decodes 8-bit palette PNGs (color type 3, not interlaced: the hero frames <c>tools/heroanim/png8.mjs</c> writes)
    /// into <see cref="PalettePicture"/>s. Any other PNG gives null, and the painter decodes it its own way. Engine-free.
    /// </summary>
    public static class PalettePng
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static PalettePicture? Decode(Stream stream)
        {
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return Decode(memory.ToArray());
        }

        public static PalettePicture? Decode(byte[] png)
        {
            if (png.Length < 8 || !png.AsSpan(0, 8).SequenceEqual(Signature))
            {
                return null;
            }

            int width = 0;
            int height = 0;
            var palette = new byte[256 * 4];
            using var data = new MemoryStream();
            int at = 8;
            while (at + 12 <= png.Length)
            {
                int length = BigEndian(png, at);
                string type = System.Text.Encoding.ASCII.GetString(png, at + 4, 4);
                int body = at + 8;
                if (length < 0 || body + length > png.Length)
                {
                    return null;
                }

                switch (type)
                {
                    case "IHDR":
                        width = BigEndian(png, body);
                        height = BigEndian(png, body + 4);
                        // Bit depth 8, palette colors, deflate, adaptive filters, no interlace.
                        if (length < 13 || png[body + 8] != 8 || png[body + 9] != 3 || png[body + 10] != 0 || png[body + 11] != 0 || png[body + 12] != 0 || width <= 0 || height <= 0)
                        {
                            return null;
                        }

                        break;
                    case "PLTE":
                        for (int i = 0; i < Math.Min(256, length / 3); i++)
                        {
                            palette[i * 4] = png[body + (i * 3)];
                            palette[(i * 4) + 1] = png[body + (i * 3) + 1];
                            palette[(i * 4) + 2] = png[body + (i * 3) + 2];
                            palette[(i * 4) + 3] = 255;
                        }

                        break;
                    case "tRNS":
                        for (int i = 0; i < Math.Min(256, length); i++)
                        {
                            palette[(i * 4) + 3] = png[body + i];
                        }

                        break;
                    case "IDAT":
                        data.Write(png, body, length);
                        break;
                }

                if (type == "IEND")
                {
                    break;
                }

                at = body + length + 4;
            }

            if (width == 0 || data.Length < 2)
            {
                return null;
            }

            // Each row is a filter byte and one index a pixel.
            var raw = new byte[height * (width + 1)];
            data.Position = 0;
            using (var inflate = new ZLibStream(data, CompressionMode.Decompress))
            {
                int read = 0;
                while (read < raw.Length)
                {
                    int n = inflate.Read(raw, read, raw.Length - read);
                    if (n <= 0)
                    {
                        return null;
                    }

                    read += n;
                }
            }

            var pixels = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                int src = y * (width + 1);
                byte filter = raw[src];
                int row = y * width;
                int up = row - width;
                for (int x = 0; x < width; x++)
                {
                    int a = x > 0 ? pixels[row + x - 1] : 0;
                    int b = y > 0 ? pixels[up + x] : 0;
                    int c = x > 0 && y > 0 ? pixels[up + x - 1] : 0;
                    int value = raw[src + 1 + x];
                    pixels[row + x] = filter switch
                    {
                        0 => (byte)value,
                        1 => (byte)(value + a),
                        2 => (byte)(value + b),
                        3 => (byte)(value + ((a + b) / 2)),
                        4 => (byte)(value + Paeth(a, b, c)),
                        _ => throw new InvalidDataException("PNG row filter " + filter),
                    };
                }
            }

            return new PalettePicture(width, height, pixels, palette);
        }

        private static int BigEndian(byte[] bytes, int at) => (bytes[at] << 24) | (bytes[at + 1] << 16) | (bytes[at + 2] << 8) | bytes[at + 3];

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }
    }

    /// <summary>
    /// The animated heroes' frames of a painter (spec 005 FR-028): each is read from the embedded resources and decoded
    /// the first time it is drawn (<see cref="PalettePng"/>), then kept in a <see cref="PictureCache{T}"/> bounded by
    /// bytes, so the 288 frames are never all decoded up front and the cache never grows past its budget (the least
    /// recently drawn frames are dropped first, never one drawn in the last two frames). The painter calls
    /// <see cref="NextFrame"/> at the start of each frame. Engine-free; callers lock it when painters share it across threads.
    /// </summary>
    public sealed class HeroFrameStore
    {
        private readonly Assembly _assembly;
        private readonly PictureCache<PalettePicture> _frames;
        private readonly HashSet<string> _resources;
        private readonly HashSet<string> _undecodable = new HashSet<string>(StringComparer.Ordinal);

        public HeroFrameStore(Assembly assembly, long budgetBytes)
        {
            _assembly = assembly;
            _frames = new PictureCache<PalettePicture>(budgetBytes, _ => { });
            _resources = new HashSet<string>(assembly.GetManifestResourceNames(), StringComparer.Ordinal);
        }

        /// <summary>The bytes of the frames held now.</summary>
        public long Bytes => _frames.Bytes;

        /// <summary>How many frames are held now.</summary>
        public int Count => _frames.Count;

        /// <summary>How many frames were decoded so far (a frame dropped and drawn again counts again).</summary>
        public int Decoded { get; private set; }

        /// <summary>Whether the frame is embedded (nothing is decoded).</summary>
        public bool Has(string name) => _resources.Contains(PainterBase.SpriteResource(name));

        /// <summary>The frame, decoded now when it is not held; null when it is missing or not a palette PNG.</summary>
        public PalettePicture? Get(string name)
        {
            if (_frames.TryGet(name, 0, 0, out PalettePicture picture))
            {
                return picture;
            }

            if (!Has(name) || _undecodable.Contains(name))
            {
                return null;
            }

            using Stream? stream = _assembly.GetManifestResourceStream(PainterBase.SpriteResource(name));
            PalettePicture? decoded = stream != null ? PalettePng.Decode(stream) : null;
            if (decoded == null)
            {
                _undecodable.Add(name);
                return null;
            }

            Decoded++;
            _frames.Add(name, 0, 0, decoded, decoded.Bytes);
            return decoded;
        }

        /// <summary>Starts a frame (drops the least recently drawn frames while over budget).</summary>
        public void NextFrame() => _frames.NextFrame();
    }
}
