using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Turns a <see cref="ShapeLibrary"/> shape into an anti-aliased alpha mask (research R2). The Unity client wraps
    /// the mask in a texture, the playtest in a bitmap, and both tint it with the draw color. Clients cache masks by
    /// (id, size). Engine-free.
    /// </summary>
    public static class ShapeRaster
    {
        /// <summary>The margin around a shape: the unit square is sampled slightly larger so outlines are not clipped.</summary>
        public const float Margin = 1.08f;

        /// <summary>
        /// A <paramref name="size"/> × <paramref name="size"/> alpha mask (0–255), row by row. With
        /// <paramref name="topDown"/> the first row is the top of the shape (bitmaps); otherwise the bottom (Unity
        /// textures).
        /// </summary>
        public static byte[] Mask(string id, int size, bool topDown) => Mask(ShapeLibrary.Get(id), size, topDown);

        public static byte[] Mask(Func<float, float, float> sdf, int size, bool topDown)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var mask = new byte[size * size];
            float pixel = 2f / size;
            for (int row = 0; row < size; row++)
            {
                int py = topDown ? size - 1 - row : row;
                float y = (((py + 0.5f) * pixel) - 1f) * Margin;
                for (int px = 0; px < size; px++)
                {
                    float x = (((px + 0.5f) * pixel) - 1f) * Margin;
                    float d = sdf(x, y);
                    float alpha = Math.Max(0f, Math.Min(1f, 0.5f - (d / pixel)));
                    mask[(row * size) + px] = (byte)(alpha * 255f);
                }
            }

            return mask;
        }

        /// <summary>The share of the mask that is at least half covered (tests: non-empty and distinct shapes).</summary>
        public static float Coverage(byte[] mask)
        {
            int inside = 0;
            foreach (byte a in mask)
            {
                if (a >= 128)
                {
                    inside++;
                }
            }

            return mask.Length == 0 ? 0f : (float)inside / mask.Length;
        }

        /// <summary>The share of pixels where two masks of the same size disagree (one inside, the other outside).</summary>
        public static float Difference(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                throw new ArgumentException("Masks differ in size.");
            }

            int differ = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if ((a[i] >= 128) != (b[i] >= 128))
                {
                    differ++;
                }
            }

            return a.Length == 0 ? 0f : (float)differ / a.Length;
        }

        /// <summary>A mask size rounded up to a multiple of 16 (fewer cache entries across screen sizes), at least 16.</summary>
        public static int Quantize(float pixels) => Math.Max(16, ((int)Math.Ceiling(pixels / 16f)) * 16);
    }
}
