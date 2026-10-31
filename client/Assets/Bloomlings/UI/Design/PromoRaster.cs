using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- Home's promo scenes on their plates (spec 005 FR-036, the owner's tuning of 2026-10-05) ----

        /// <summary>
        /// The soft shadow of a picture's silhouette (<c>ui.promo.no_ads</c>, <c>ui.promo.daily</c>): <c>garden.shadow</c> at
        /// <paramref name="alpha"/> times the silhouette blurred by a Gaussian of <paramref name="sigma"/> pixels, in a picture of
        /// <paramref name="width"/> × <paramref name="height"/> whose inner box (<paramref name="padX"/>, <paramref name="padY"/>
        /// pixels in from every side) holds the source's alpha (<paramref name="mask"/>, <paramref name="maskWidth"/> ×
        /// <paramref name="maskHeight"/>, rows from the top) stretched. Three box blurs stand for the Gaussian. Deterministic,
        /// straight alpha.
        /// </summary>
        public static byte[] SilhouetteShadow(byte[] mask, int maskWidth, int maskHeight, int width, int height, float padX, float padY, float sigma, float alpha)
        {
            Check(width, height);
            var a = new float[width * height];
            float innerWidth = Math.Max(1f, width - (2f * padX));
            float innerHeight = Math.Max(1f, height - (2f * padY));
            for (int py = 0; py < height; py++)
            {
                float v = ((py + 0.5f - padY) / innerHeight * maskHeight) - 0.5f;
                if (v < -0.5f || v > maskHeight - 0.5f)
                {
                    continue;
                }

                for (int px = 0; px < width; px++)
                {
                    float u = ((px + 0.5f - padX) / innerWidth * maskWidth) - 0.5f;
                    if (u >= -0.5f && u <= maskWidth - 0.5f)
                    {
                        a[(py * width) + px] = Bilinear(mask, maskWidth, maskHeight, u, v);
                    }
                }
            }

            foreach (int radius in GaussBoxes(sigma))
            {
                BoxBlur(a, width, height, radius, horizontal: true);
                BoxBlur(a, width, height, radius, horizontal: false);
            }

            var pixels = new byte[width * height * 4];
            var color = new Color(C.GardenShadow);
            for (int i = 0; i < a.Length; i++)
            {
                Put(pixels, width, i % width, i / width, color, alpha * a[i]);
            }

            return pixels;
        }

        /// <summary>
        /// The soft shadow of a rounded rectangle (the promo plate's, spec 005 FR-036): <c>garden.shadow</c> at
        /// <paramref name="alpha"/> times a rounded box (<paramref name="padX"/>, <paramref name="padY"/> pixels in from every
        /// side, corners of <paramref name="radius"/>) blurred by a Gaussian of <paramref name="sigma"/> pixels, from its signed
        /// distance. Deterministic, straight alpha.
        /// </summary>
        public static byte[] RoundShadow(int width, int height, float padX, float padY, float radius, float sigma, float alpha)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            var color = new Color(C.GardenShadow);
            float l = padX, t = padY, r = width - padX, b = height - padY;
            float rr = Math.Max(0f, Math.Min(radius, Math.Min(r - l, b - t) / 2f));
            float s = Math.Max(0.5f, sigma) * (float)Math.Sqrt(2.0);
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = RoundRect(px + 0.5f, py + 0.5f, l, t, r, b, rr);
                    Put(pixels, width, px, py, color, alpha * 0.5f * Erfc(d / s));
                }
            }

            return pixels;
        }

        /// <summary>
        /// Home's floating button (the owner, 2026-10-31, as the reference game's): a round badge in one colour
        /// (<paramref name="fill"/>, a touch lighter at the top) in a gentle white outline of <paramref name="ringShare"/> of its
        /// diameter, with a soft shadow under it, in a <paramref name="size"/> square. Deterministic, straight alpha.
        /// </summary>
        public static byte[] PromoBadge(int size, Rgba fill, float ringShare)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float c = s / 2f;
            float r = s * 0.46f;
            float ring = Math.Max(1f, s * ringShare);
            var white = new Color(Rgba.White);
            var shade = new Color(C.GardenShadow);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f, y = py + 0.5f;
                    float d = (float)Math.Sqrt(((x - c) * (x - c)) + ((y - c) * (y - c))) - r;
                    float sd = (float)Math.Sqrt(((x - c) * (x - c)) + ((y - c - (s * 0.025f)) * (y - c - (s * 0.025f)))) - r;
                    float shadow = 0.28f * 0.5f * Erfc(sd / Math.Max(1f, s * 0.03f));
                    float body = Coverage(d);
                    if (body <= 0f)
                    {
                        if (shadow > 0.004f)
                        {
                            Put(pixels, size, px, py, shade, shadow);
                        }

                        continue;
                    }

                    // One colour, a touch lighter toward the top; the gentle white outline inside the edge.
                    var col = new Color(fill);
                    col.Mix(Rgba.White, 0.12f * Clamp01((c - y) / r));
                    float inRing = Coverage(d + ring) < 1f ? 1f - Coverage(d + ring) : 0f;
                    col.Mix(Rgba.White, 0.92f * inRing);
                    Put(pixels, size, px, py, col, body);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The floating button's label pill (the owner, 2026-10-31): a rounded bar of <paramref name="fill"/> in the same gentle
        /// white outline (<paramref name="ring"/> pixels), <paramref name="width"/> × <paramref name="height"/>. Straight alpha.
        /// </summary>
        public static byte[] PromoBadgePill(int width, int height, Rgba fill, float ring)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float rr = height / 2f;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = RoundRect(px + 0.5f, py + 0.5f, 0.5f, 0.5f, width - 0.5f, height - 0.5f, rr);
                    float body = Coverage(d);
                    if (body <= 0f)
                    {
                        continue;
                    }

                    var col = new Color(fill);
                    float inRing = 1f - Coverage(d + Math.Max(1f, ring));
                    col.Mix(Rgba.White, 0.92f * inRing);
                    Put(pixels, width, px, py, col, body);
                }
            }

            return pixels;
        }

        /// <summary>The source alpha (0 to 1) between four pixels, clamped at the edges.</summary>
        private static float Bilinear(byte[] mask, int w, int h, float u, float v)
        {
            int x0 = (int)Math.Floor(u), y0 = (int)Math.Floor(v);
            float fx = u - x0, fy = v - y0;
            float At(int x, int y) => mask[(Math.Max(0, Math.Min(h - 1, y)) * w) + Math.Max(0, Math.Min(w - 1, x))] / 255f;
            float top = At(x0, y0) + ((At(x0 + 1, y0) - At(x0, y0)) * fx);
            float bottom = At(x0, y0 + 1) + ((At(x0 + 1, y0 + 1) - At(x0, y0 + 1)) * fx);
            return top + ((bottom - top) * fy);
        }

        /// <summary>The three box radii whose blurs, one after another, stand for a Gaussian of <paramref name="sigma"/>.</summary>
        private static int[] GaussBoxes(float sigma)
        {
            if (sigma < 0.5f)
            {
                return Array.Empty<int>();
            }

            double ideal = Math.Sqrt((12.0 * sigma * sigma / 3.0) + 1.0);
            int lower = (int)Math.Floor(ideal);
            if (lower % 2 == 0)
            {
                lower--;
            }

            int upper = lower + 2;
            double m = Math.Round(((12.0 * sigma * sigma) - (3 * lower * lower) - (12 * lower) - 9) / ((-4 * lower) - 4));
            var radii = new int[3];
            for (int i = 0; i < 3; i++)
            {
                radii[i] = ((i < m ? lower : upper) - 1) / 2;
            }

            return radii;
        }

        /// <summary>One box blur of <paramref name="radius"/> along rows or columns, in place, zero outside the picture.</summary>
        private static void BoxBlur(float[] a, int width, int height, int radius, bool horizontal)
        {
            if (radius < 1)
            {
                return;
            }

            int lines = horizontal ? height : width;
            int length = horizontal ? width : height;
            var line = new float[length];
            float scale = 1f / ((2 * radius) + 1);
            for (int k = 0; k < lines; k++)
            {
                for (int i = 0; i < length; i++)
                {
                    line[i] = a[horizontal ? (k * width) + i : (i * width) + k];
                }

                float sum = 0f;
                for (int i = 0; i <= radius && i < length; i++)
                {
                    sum += line[i];
                }

                for (int i = 0; i < length; i++)
                {
                    a[horizontal ? (k * width) + i : (i * width) + k] = sum * scale;
                    int add = i + radius + 1;
                    int drop = i - radius;
                    if (add < length)
                    {
                        sum += line[add];
                    }

                    if (drop >= 0)
                    {
                        sum -= line[drop];
                    }
                }
            }
        }

        /// <summary>The complementary error function (Abramowitz and Stegun 7.1.26, within 1.5e-7).</summary>
        private static float Erfc(float x)
        {
            double z = Math.Abs(x);
            double t = 1.0 / (1.0 + (0.3275911 * z));
            double y = t * (0.254829592 + (t * (-0.284496736 + (t * (1.421413741 + (t * (-1.453152027 + (t * 1.061405429))))))));
            double erfc = y * Math.Exp(-z * z);
            return (float)(x >= 0 ? erfc : 2.0 - erfc);
        }
    }
}
