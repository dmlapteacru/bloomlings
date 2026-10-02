using System;
using System.Globalization;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Client.Art
{
    /// <summary>The fill of a <see cref="PicturePixels.RoundRect"/> picture.</summary>
    public enum RoundFill
    {
        /// <summary>The whole rounded rectangle.</summary>
        Solid,

        /// <summary>A band along the edge, <c>band</c> pixels deep (an outline).</summary>
        Ring,

        /// <summary>A band along the edge fading from opaque at the edge to clear at <c>band</c> pixels deep (aged edges, soft rims).</summary>
        Fade,
    }

    /// <summary>
    /// The engine-free half of <see cref="ProceduralSprites.Picture"/> (spec 005 contracts/look.md §2.1): cache keys, the
    /// row flip from the kit's top-down pixels to Unity's bottom-up textures, the edge bleed that keeps bilinear filtering
    /// free of dark fringes on straight-alpha pictures, and the small UI pictures the Unity kit slices (rounded
    /// rectangles, rings, soft edges, dashed outlines, light rays). Every picture is straight-alpha RGBA, rows from the
    /// top, <c>width * height * 4</c> bytes, deterministic.
    /// </summary>
    public static class PicturePixels
    {
        /// <summary>The side of the rounded-rectangle sprites the Unity kit slices; their border is half of it.</summary>
        public const int RoundSize = 128;

        /// <summary>The corner radius (or band) in pixels of the rounded-rectangle sprites: the 9-slice border.</summary>
        public const int RoundUnit = RoundSize / 2;

        /// <summary>
        /// The cache key of a picture (<c>key@WxH</c>, <see cref="UiRaster.CacheKey"/>), with its 9-slice border (left,
        /// bottom, right, top in pixels, Unity's order) appended when it has one, so the same picture sliced differently
        /// never shares a sprite.
        /// </summary>
        public static string CacheKey(string key, int width, int height, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            string plain = UiRaster.CacheKey(key, width, height);
            if (left == 0f && bottom == 0f && right == 0f && top == 0f)
            {
                return plain;
            }

            return plain + "/" + Text(left) + "," + Text(bottom) + "," + Text(right) + "," + Text(top);
        }

        /// <summary>The rows of a top-down RGBA picture in Unity's bottom-up order (a new array).</summary>
        public static byte[] FlipRows(byte[] rgba, int width, int height)
        {
            Check(rgba, width, height);
            var flipped = new byte[rgba.Length];
            int stride = width * 4;
            for (int row = 0; row < height; row++)
            {
                Buffer.BlockCopy(rgba, row * stride, flipped, (height - 1 - row) * stride, stride);
            }

            return flipped;
        }

        /// <summary>
        /// Gives every fully transparent pixel next to a visible one the alpha-weighted color of its visible neighbors
        /// (its alpha stays 0), in place. Bilinear filtering then blends an edge with its own color instead of the
        /// transparent black around it, which straight-alpha textures would show as a dark fringe.
        /// </summary>
        public static void BleedEdges(byte[] rgba, int width, int height)
        {
            Check(rgba, width, height);
            var source = (byte[])rgba.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = ((y * width) + x) * 4;
                    if (source[i + 3] != 0)
                    {
                        continue;
                    }

                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    float weight = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= height)
                        {
                            continue;
                        }

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if ((dx == 0 && dy == 0) || nx < 0 || nx >= width)
                            {
                                continue;
                            }

                            int n = ((ny * width) + nx) * 4;
                            float a = source[n + 3];
                            if (a <= 0f)
                            {
                                continue;
                            }

                            r += source[n] * a;
                            g += source[n + 1] * a;
                            b += source[n + 2] * a;
                            weight += a;
                        }
                    }

                    if (weight > 0f)
                    {
                        rgba[i] = Byte(r / weight);
                        rgba[i + 1] = Byte(g / weight);
                        rgba[i + 2] = Byte(b / weight);
                    }
                }
            }
        }

        /// <summary>A kit picture ready for a Unity texture: edges bled, rows flipped (a new array).</summary>
        public static byte[] ForTexture(byte[] rgba, int width, int height)
        {
            byte[] flipped = FlipRows(rgba, width, height);
            BleedEdges(flipped, width, height);
            return flipped;
        }

        /// <summary>
        /// A white rounded rectangle filling a <paramref name="width"/> × <paramref name="height"/> picture, in alpha:
        /// solid, a ring <paramref name="band"/> pixels wide, or a band fading inward over <paramref name="band"/> pixels.
        /// Corners are <paramref name="radius"/> pixels round; edges are anti-aliased over one pixel. The inner edge of a ring
        /// follows the outline inward, so its corners are <c>radius - band</c> round (square once the band is wider).
        /// </summary>
        public static byte[] RoundRect(int width, int height, float radius, RoundFill fill = RoundFill.Solid, float band = 0f)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            var pixels = new byte[width * height * 4];
            float r = Math.Max(0f, Math.Min(radius, Math.Min(width, height) / 2f));
            float depth = Math.Max(0.5f, band);
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = RoundRectDistance(px + 0.5f, py + 0.5f, width, height, r);
                    float alpha = Clamp01(0.5f - d);
                    switch (fill)
                    {
                        case RoundFill.Ring:
                            alpha *= Clamp01(0.5f + d + depth);
                            break;
                        case RoundFill.Fade:
                            alpha *= Clamp01(1f + (d / depth));
                            break;
                    }

                    int i = ((py * width) + px) * 4;
                    pixels[i] = 255;
                    pixels[i + 1] = 255;
                    pixels[i + 2] = 255;
                    pixels[i + 3] = Byte(alpha * 255f);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A white dashed outline of a rounded rectangle (the empty Waiting Slot, spec 005 contracts/look.md §3.7): the
        /// stroke <paramref name="stroke"/> pixels wide centered on the outline of the rectangle inset by
        /// <paramref name="inset"/>, whose corners are <paramref name="radius"/> round, with dashes of
        /// <paramref name="on"/> pixels and gaps of <paramref name="off"/> pixels along it, starting where the top edge
        /// leaves the top-left corner and going clockwise (as the playtest's dashed stroke does).
        /// </summary>
        public static byte[] DashedOutline(int width, int height, float inset, float radius, float stroke, float on, float off)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            var pixels = new byte[width * height * 4];
            float left = inset;
            float top = inset;
            float right = width - inset;
            float bottom = height - inset;
            float r = Math.Max(0f, Math.Min(radius, Math.Min(right - left, bottom - top) / 2f));
            float half = stroke / 2f;
            float period = Math.Max(1f, on + off);
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    (float distance, float along) = OutlinePoint(px + 0.5f, py + 0.5f, left, top, right, bottom, r);
                    float cover = Clamp01(half + 0.5f - distance);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float phase = along % period;
                    float dash = Math.Min(Clamp01(phase + 0.5f), Clamp01(on + 0.5f - phase));
                    float alpha = cover * dash;
                    int i = ((py * width) + px) * 4;
                    pixels[i] = 255;
                    pixels[i + 1] = 255;
                    pixels[i + 2] = 255;
                    pixels[i + 3] = Byte(alpha * 255f);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The win's light rays (spec 005 contracts/look.md §3.9, the playtest's <c>Kit.LightRays</c>) in a square picture
        /// of side <paramref name="size"/>, in <paramref name="color"/> with straight alpha: a soft radial glow of eight faint
        /// discs (0.06 to 0.48 of the radius) and ten rays from the center, alternately full and 0.7 wide, each five faint
        /// strokes to the edge and five shorter ones. The Unity kit turns the picture 0.05 turn per second.
        /// </summary>
        public static byte[] LightRays(int size, Rgba color)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var pixels = new byte[size * size * 4];
            float c = size / 2f;
            float radius = size / 2f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f - c;
                    float y = py + 0.5f - c;
                    float rho = (float)Math.Sqrt((x * x) + (y * y));
                    float clear = 1f;
                    for (int disc = 7; disc >= 0; disc--)
                    {
                        // A soft radial glow: eight faint discs (0.06 to 0.48 of the radius), so no edge shows.
                        clear *= 1f - (0.035f * Clamp01(0.5f - (rho - (radius * (0.06f + (0.06f * disc))))));
                    }

                    if (rho <= radius + 1f)
                    {
                        double theta = Math.Atan2(y, x);
                        for (int i = 0; i < 10; i++)
                        {
                            float wide = i % 2 == 0 ? 1f : 0.7f;
                            double ray = i * Math.PI / 5.0;
                            double gapAngle = AngleBetween(theta, ray);
                            // Far from a ray's angle none of its strokes reach the pixel (the widest is 0.025 of the
                            // radius on either side, at most 0.068 rad off the ray).
                            if (rho > radius * 0.12f && gapAngle > 0.35)
                            {
                                continue;
                            }

                            for (int j = -2; j <= 2; j++)
                            {
                                double a = ray + (j * 0.034 * wide);
                                float ux = (float)Math.Cos(a);
                                float uy = (float)Math.Sin(a);
                                clear *= 1f - (0.09f * Capsule(x, y, ux, uy, radius, radius * 0.025f * wide));
                                clear *= 1f - (0.11f * Capsule(x, y, ux, uy, radius * 0.62f, radius * 0.0225f * wide));
                            }
                        }
                    }

                    float alpha = 1f - clear;
                    int k = ((py * size) + px) * 4;
                    pixels[k] = color.R;
                    pixels[k + 1] = color.G;
                    pixels[k + 2] = color.B;
                    pixels[k + 3] = Byte(alpha * 255f * (color.A / 255f));
                }
            }

            return pixels;
        }

        /// <summary>
        /// The texture UV rectangle (x, y, width, height in 0–1) that shows a <paramref name="textureWidth"/> ×
        /// <paramref name="textureHeight"/> picture cover-fitted into a <paramref name="width"/> × <paramref name="height"/>
        /// area: the picture fills the area, centered, and the overflow on one axis is cropped (the owner's backgrounds,
        /// spec 005 pictures.md B).
        /// </summary>
        public static (float X, float Y, float Width, float Height) CoverUv(float textureWidth, float textureHeight, float width, float height)
        {
            if (textureWidth <= 0f || textureHeight <= 0f || width <= 0f || height <= 0f)
            {
                return (0f, 0f, 1f, 1f);
            }

            float picture = textureWidth / textureHeight;
            float area = width / height;
            if (picture > area)
            {
                float w = area / picture;
                return ((1f - w) / 2f, 0f, w, 1f);
            }

            float h = picture / area;
            return (0f, (1f - h) / 2f, 1f, h);
        }

        /// <summary>The signed distance from (x, y) to a rounded rectangle filling (0, 0)–(w, h); negative inside.</summary>
        public static float RoundRectDistance(float x, float y, float w, float h, float r)
        {
            float qx = Math.Abs(x - (w / 2f)) - ((w / 2f) - r);
            float qy = Math.Abs(y - (h / 2f)) - ((h / 2f) - r);
            float outside = (float)Math.Sqrt((Math.Max(qx, 0f) * Math.Max(qx, 0f)) + (Math.Max(qy, 0f) * Math.Max(qy, 0f)));
            return outside + Math.Min(Math.Max(qx, qy), 0f) - r;
        }

        /// <summary>
        /// The distance from a point to a rounded rectangle's outline and the arc length along it of the nearest outline
        /// point, from where the top edge leaves the top-left corner, clockwise (y down).
        /// </summary>
        private static (float Distance, float Along) OutlinePoint(float x, float y, float left, float top, float right, float bottom, float r)
        {
            float topLength = Math.Max(0f, right - left - (2f * r));
            float sideLength = Math.Max(0f, bottom - top - (2f * r));
            float arc = (float)(Math.PI * r / 2.0);
            float best = float.MaxValue;
            float along = 0f;

            void Segment(float ax, float ay, float bx, float by, float start)
            {
                float dx = bx - ax;
                float dy = by - ay;
                float length2 = (dx * dx) + (dy * dy);
                float t = length2 > 0f ? Clamp01((((x - ax) * dx) + ((y - ay) * dy)) / length2) : 0f;
                float px = ax + (dx * t) - x;
                float py = ay + (dy * t) - y;
                float d = (float)Math.Sqrt((px * px) + (py * py));
                if (d < best)
                {
                    best = d;
                    along = start + (t * (float)Math.Sqrt(length2));
                }
            }

            void Corner(float cx, float cy, double from, float start)
            {
                if (r <= 0f)
                {
                    return;
                }

                // The quarter arc from the angle `from` clockwise on screen (y down): angles grow clockwise.
                double angle = Math.Atan2(y - cy, x - cx);
                double t = NormalizeAngle(angle - from);
                if (t > Math.PI / 2.0)
                {
                    t = t > (Math.PI * 1.25) ? 0.0 : Math.PI / 2.0;
                }

                double a = from + t;
                float px = cx + (float)(Math.Cos(a) * r) - x;
                float py = cy + (float)(Math.Sin(a) * r) - y;
                float d = (float)Math.Sqrt((px * px) + (py * py));
                if (d < best)
                {
                    best = d;
                    along = start + (float)(t * r);
                }
            }

            float s = 0f;
            Segment(left + r, top, right - r, top, s);
            s += topLength;
            Corner(right - r, top + r, -Math.PI / 2.0, s);
            s += arc;
            Segment(right, top + r, right, bottom - r, s);
            s += sideLength;
            Corner(right - r, bottom - r, 0.0, s);
            s += arc;
            Segment(right - r, bottom, left + r, bottom, s);
            s += topLength;
            Corner(left + r, bottom - r, Math.PI / 2.0, s);
            s += arc;
            Segment(left, bottom - r, left, top + r, s);
            s += sideLength;
            Corner(left + r, top + r, Math.PI, s);
            return (best, along);
        }

        /// <summary>The coverage of a stroke from the center along (ux, uy) to <paramref name="length"/>, with round caps.</summary>
        private static float Capsule(float x, float y, float ux, float uy, float length, float half)
        {
            float t = Math.Max(0f, Math.Min(length, (x * ux) + (y * uy)));
            float dx = x - (ux * t);
            float dy = y - (uy * t);
            float d = (float)Math.Sqrt((dx * dx) + (dy * dy)) - half;
            return Clamp01(0.5f - d);
        }

        private static double AngleBetween(double a, double b)
        {
            double d = NormalizeAngle(a - b);
            return d > Math.PI ? (2.0 * Math.PI) - d : d;
        }

        private static double NormalizeAngle(double a)
        {
            double twoPi = 2.0 * Math.PI;
            a %= twoPi;
            return a < 0.0 ? a + twoPi : a;
        }

        private static void Check(byte[] rgba, int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (rgba.Length != width * height * 4)
            {
                throw new ArgumentException("A picture of " + width + "x" + height + " needs " + (width * height * 4) + " bytes, not " + rgba.Length + ".", nameof(rgba));
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static byte Byte(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));

        private static string Text(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
