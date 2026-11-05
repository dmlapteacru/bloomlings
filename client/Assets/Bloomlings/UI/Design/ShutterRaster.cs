using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The Twig wooden shutters' pictures (rules 17 §8 «Twig Деревянные створки», spec 006 FR-070; the owner's look B of
    /// 2026-11-05, <see cref="ShutterLook"/>): the crate lid's shadow and front edge, its upright side battens with their nails,
    /// the tag's nail and strings, a counted cube's spark and the opening's dust. The slats are the signs' plank
    /// (<see cref="ShutterLook.SlatPicture"/>) and the tag the pods' cube. Straight-alpha RGBA, deterministic. Engine-free.
    /// </summary>
    public static partial class UiRaster
    {
        /// <summary>
        /// A lid's base in <paramref name="width"/> × <paramref name="height"/> (<see cref="ShutterLook.Base"/>): for a lid top
        /// <paramref name="wc"/> × <paramref name="hc"/> cells with a front edge <paramref name="fc"/> cells tall, its soft shadow
        /// below right and its dark rounded front edge (<c>wood.dark</c> in a <c>wood.dark_line</c> outline).
        /// </summary>
        public static byte[] ShutterBase(int width, int height, float wc, float hc, float fc)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float bw = wc + 0.9f;
            float s = width / bw;
            float ox = 0.3f * s;
            float oy = 0.3f * s;
            float l = ox;
            float t = oy;
            float r = ox + (wc * s);
            float b = oy + (hc * s);
            float f = fc * s;
            float blur = Math.Max(1f, 0.25f * s);
            float line = Math.Max(1f, 0.06f * s);
            Rows(width, height, py =>
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    var c = new Color(Rgba.White);
                    float a = 0f;
                    float sh = RoundRect(x, y, l + (0.1f * s), t + (0.3f * s), r + (0.2f * s), b + f + (0.3f * s), 0.3f * s);
                    Over(ref c, ref a, C.WoodDarkLine, 0.3f * (1f - Smooth(Clamp01((sh + blur) / (2f * blur)))));
                    float edge = RoundRect(x, y, l, b - (0.05f * s), r, b + f, 0.14f * s);
                    Over(ref c, ref a, C.WoodDarkLine, Coverage(edge));
                    Over(ref c, ref a, C.WoodDark, Coverage(edge + line));
                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            });

            return pixels;
        }

        /// <summary>
        /// A lid's side batten in <paramref name="width"/> × <paramref name="height"/> (upright): the dark wood plank turned on
        /// its end, with a nail head near each end.
        /// </summary>
        public static byte[] ShutterBatten(int width, int height)
        {
            Check(width, height);
            float m = Math.Min(width, height);
            byte[] plank = Plank(height, width, m * 0.3f, Math.Max(1f, m * 0.09f), WoodTone.Dark, 2);
            var pixels = new byte[width * height * 4];
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    // the plank's (x, y) = (py, width - 1 - px): turned a quarter clockwise
                    int sx = py;
                    int sy = width - 1 - px;
                    int si = ((sy * height) + sx) * 4;
                    int ti = ((py * width) + px) * 4;
                    pixels[ti] = plank[si];
                    pixels[ti + 1] = plank[si + 1];
                    pixels[ti + 2] = plank[si + 2];
                    pixels[ti + 3] = plank[si + 3];
                }
            }

            float nail = Math.Max(1.5f, width * 0.17f);
            var head = new Rgba(205, 200, 190);
            foreach (float at in new[] { 0.12f, 0.88f })
            {
                float cy = height * at;
                float cx = width / 2f;
                for (int py = Math.Max(0, (int)(cy - nail - 2)); py < Math.Min(height, (int)(cy + nail + 2)); py++)
                {
                    for (int px = Math.Max(0, (int)(cx - nail - 2)); px < Math.Min(width, (int)(cx + nail + 2)); px++)
                    {
                        float k = Coverage(Length(px + 0.5f - cx, py + 0.5f - cy) - nail);
                        if (k <= 0f)
                        {
                            continue;
                        }

                        int i = ((py * width) + px) * 4;
                        var c = new Color(new Rgba(pixels[i], pixels[i + 1], pixels[i + 2]));
                        float a = pixels[i + 3] / 255f;
                        Over(ref c, ref a, head, k);
                        Put(pixels, width, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a side batten (<see cref="ShutterBatten"/>).</summary>
        public const string ShutterBattenKey = ShutterLook.Slot + "/batten";

        /// <summary>
        /// The tag's hanger in <paramref name="width"/> × <paramref name="height"/> (<see cref="ShutterLook.Hanger"/>): the nail at
        /// its top middle (a dark head with a grey cap) and two strings down to the tag's top, at 0.3 and 0.7 of its width.
        /// </summary>
        public static byte[] ShutterHanger(int width, int height)
        {
            Check(width, height);
            float nailSize = width * ShutterLook.NailShare;
            var pixels = new byte[width * height * 4];
            float nx = width / 2f;
            float ny = nailSize / 2f;
            float sw = Math.Max(1.5f, nailSize * 0.18f);
            float lx = width * 0.3f;
            float rx = width * 0.7f;
            float by = height - (sw / 2f);
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float y = py + 0.5f;
                    var c = new Color(Rgba.White);
                    float a = 0f;
                    float str = Math.Min(Segment(x, y, nx, ny, lx, by), Segment(x, y, nx, ny, rx, by));
                    Over(ref c, ref a, C.WoodDarkLine, Coverage(str - (sw / 2f)));
                    float d = Length(x - nx, y - ny);
                    Over(ref c, ref a, C.WoodDarkLine, Coverage(d - (nailSize * 0.5f)));
                    Over(ref c, ref a, C.StoneTop.Mix(C.StoneLine, 0.4f), Coverage(d - (nailSize * 0.33f)));
                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of the tag's hanger (<see cref="ShutterHanger"/>).</summary>
        public const string ShutterHangerKey = ShutterLook.Slot + "/hanger";

        /// <summary>A counted cube's spark in a <paramref name="size"/> square: a white glow round a disc of the material's colour.</summary>
        public static byte[] ShutterSpark(int size, Rgba color) => Discs(size, (Rgba.White, 0.85f, 1f), (color, 1f, 0.71f));

        /// <summary>A puff of the opening's dust in a <paramref name="size"/> square: a soft disc of the frame's stone.</summary>
        public static byte[] ShutterDust(int size)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float c0 = size / 2f;
            Rgba col = C.StoneTop.Mix(C.StoneFace, 0.5f);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float d = Length(px + 0.5f - c0, py + 0.5f - c0) / c0;
                    float k = 0.4f * (1f - Smooth(Clamp01((d - 0.75f) / 0.25f)));
                    if (k > 0f)
                    {
                        Put(pixels, size, px, py, new Color(col), k);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a dust puff (<see cref="ShutterDust"/>).</summary>
        public const string ShutterDustKey = ShutterLook.Slot + "/dust";
    }
}
