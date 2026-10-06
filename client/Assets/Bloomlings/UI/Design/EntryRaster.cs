using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- The Garden Entry's arch and the guided spotlight's scrim (spec 005 FR-034, FR-035) ----

        /// <summary>
        /// The Garden Entry's small stone arch (spec 005 FR-034, contracts/look.md §3 "Garden Entry"; the owner's choice
        /// "B" of 2026-10-05), upright for an entry at the bottom: a picture <see cref="BoardLayout.ArchWidth"/> ×
        /// <see cref="BoardLayout.ArchHeight"/> cells whose top reaches 0.36 cell over the entry cell and whose bottom is
        /// the border's outer edge. Two <c>stone.*</c> pillars carry an arch of keystones round a dark opening
        /// (<c>garden.shadow</c> deepened at the top, warm <c>ray.light</c> from beyond at its foot) where two eyes peep out;
        /// <c>ivy.leaf</c> leaves climb it, a pink flower (<c>petal.*</c>) sits on the keystone, a soft
        /// <c>garden.glow</c> light is round it and its shadow falls on the border. Hosts turn it for the other sides
        /// (<see cref="BoardLayout.ArchOf"/>). Deterministic, straight alpha.
        /// </summary>
        public static byte[] EntryArch(int width, int height)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float c = width / BoardLayout.ArchWidth;
            float cx = 0.56f * c;
            float spring = 0.53f * c;
            float inner = 0.29f * c;
            float outer = 0.44f * c;
            float foot = 0.84f * c;
            float line = Math.Max(1f, 0.018f * c);
            Rgba deep = C.GardenShadow.Darken(0.6f);
            Rgba dim = C.GardenShadow.Darken(0.15f);
            (float X, float Y, float Angle, float Size)[] leaves =
            {
                (cx + (float)(Math.Cos(Math.PI * 1.06) * 0.4 * c), spring + (float)(Math.Sin(Math.PI * 1.06) * 0.4 * c), -60f, 1f),
                (cx + (float)(Math.Cos(Math.PI * 1.22) * 0.42 * c), spring + (float)(Math.Sin(Math.PI * 1.22) * 0.42 * c), -30f, 0.9f),
                (cx + (float)(Math.Cos(Math.PI * 1.38) * 0.43 * c), spring + (float)(Math.Sin(Math.PI * 1.38) * 0.43 * c), -12f, 0.75f),
                (cx + (float)(Math.Cos(Math.PI * 1.7) * 0.42 * c), spring + (float)(Math.Sin(Math.PI * 1.7) * 0.42 * c), 30f, 0.85f),
                (cx + (float)(Math.Cos(Math.PI * 1.88) * 0.41 * c), spring + (float)(Math.Sin(Math.PI * 1.88) * 0.41 * c), 62f, 1f),
                (0.15f * c, 0.66f * c, -80f, 0.85f),
                (0.17f * c, 0.76f * c, -100f, 0.7f),
            };

            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    var p = default(Layered);

                    // The warm light round the arch, and its shadow on the border.
                    float glow = Clamp01(1f - (Length(x - cx, y - (0.58f * c)) / (0.5f * c)));
                    p.Over(C.GardenGlow, 0.32f * glow * glow);
                    p.Over(C.GardenShadow, 0.3f * Clamp01(0.5f - (Ellipse(x - cx, y - foot, 0.5f * c, 0.045f * c) / (0.03f * c))));

                    // The opening: the arch's inner round top over the rectangle down to its foot.
                    float open = Math.Min(Length(x - cx, y - spring) - inner, RoundRect(x, y, cx - inner, spring, cx + inner, foot, 0f));
                    float silhouette = Math.Min(Math.Max(Length(x - cx, y - spring) - outer, y - spring), RoundRect(x, y, cx - outer, spring - 1f, cx + outer, foot + (0.01f * c), 0f));
                    if (open < 1f)
                    {
                        var hole = new Color(deep);
                        hole.Mix(dim, Smooth(Clamp01((y - (spring - inner)) / (foot - spring + inner))));
                        hole.Mix(C.RayLight, 0.6f * Clamp01(1f - (Length(x - cx, (y - foot) * 1.4f) / (0.24f * c))));

                        // Two eyes peep out of the dark.
                        foreach (float ex in new[] { cx - (0.085f * c), cx + (0.085f * c) })
                        {
                            float white = Ellipse(x - ex, y - (0.56f * c), 0.045f * c, 0.055f * c);
                            hole.Mix(Rgba.White, Coverage(white));
                            hole.Mix(C.InkBrown, Coverage(Length(x - ex - (0.008f * c), y - (0.578f * c)) - (0.025f * c)));
                        }

                        p.Over(hole, Coverage(open));
                    }

                    // The stone: pillars and the arch band, shaded, with joints and an outline.
                    float stone = Math.Max(silhouette, -open);
                    if (stone < 1f)
                    {
                        bool band = y < spring;
                        var s = new Color(C.StoneTop);
                        if (band)
                        {
                            float r = Length(x - cx, y - spring);
                            s.Mix(C.StoneFace, Smooth(Clamp01((outer - r) / (outer - inner))));
                        }
                        else
                        {
                            float across = Math.Abs(x - cx) > inner ? (Math.Abs(x - cx) - inner) / (outer - inner) : 0f;
                            s.Mix(C.StoneFace, 0.35f + (0.4f * across));
                            s.Mix(C.StoneLip, 0.6f * Clamp01((y - (0.74f * c)) / (0.1f * c)));
                        }

                        s.Scale(1f + ((Fbm(x / (0.12f * c), y / (0.12f * c), 41, 3) - 0.5f) * 0.08f));

                        // Joints: four between the keystones, one where the band meets each pillar, one across each pillar.
                        float joint = float.MaxValue;
                        if (band)
                        {
                            // Above the spring line only the upper half of each joint's line lies in the band.
                            for (int k = 1; k < 5; k++)
                            {
                                double a = Math.PI * (1.0 + (k / 5.0));
                                joint = Math.Min(joint, Math.Abs(((x - cx) * (float)Math.Sin(a)) - ((y - spring) * (float)Math.Cos(a))));
                            }
                        }

                        joint = Math.Min(joint, Math.Abs(y - spring) + (Math.Abs(x - cx) < inner ? c : 0f));
                        joint = Math.Min(joint, Math.Abs(y - (0.7f * c)) + (Math.Abs(x - cx) < inner ? c : 0f));
                        s.Mix(C.StoneLine, 0.55f * Coverage(joint - (line * 0.5f)));
                        s.Mix(C.StoneTop.Lighten(0.5f), 0.5f * Coverage(Math.Abs(silhouette + (line * 2f)) - (line * 0.6f)) * (band ? 1f : 0f));
                        s.Mix(C.StoneLine, Clamp01(0.5f + stone + line));
                        p.Over(s, Coverage(stone));
                    }

                    // Ivy climbing the arch.
                    foreach ((float lx, float ly, float deg, float size) in leaves)
                    {
                        double rad = deg * Math.PI / 180.0;
                        float ux = (float)(((x - lx) * Math.Cos(rad)) + ((y - ly) * Math.Sin(rad)));
                        float uy = (float)((-(x - lx) * Math.Sin(rad)) + ((y - ly) * Math.Cos(rad)));
                        float leaf = Ellipse(ux, uy, 0.075f * c * size, 0.042f * c * size);
                        if (leaf < 1f)
                        {
                            var g = new Color(C.IvyLeaf);
                            g.Mix(C.Foliage, 0.5f * Clamp01((uy / (0.042f * c * size)) + 0.3f));
                            g.Mix(C.IvyLine, Coverage(Math.Abs(uy) - (line * 0.35f)) * Clamp01(1f - Math.Abs(ux / (0.075f * c * size))));
                            g.Mix(C.IvyLine, Clamp01(0.5f + leaf + line));
                            p.Over(g, Coverage(leaf));
                        }
                    }

                    // The flower on the keystone.
                    float fx = cx;
                    float fy = 0.12f * c;
                    float petals = float.MaxValue;
                    for (int k = 0; k < 5; k++)
                    {
                        double a = (k * 2.0 * Math.PI / 5.0) - (Math.PI / 2.0);
                        petals = Math.Min(petals, Length(x - fx - (float)(Math.Cos(a) * 0.055f * c), y - fy - (float)(Math.Sin(a) * 0.055f * c)) - (0.05f * c));
                    }

                    if (petals < 1f)
                    {
                        var f = new Color(C.PetalFill);
                        f.Mix(C.PetalFill.Lighten(0.35f), Clamp01(1f - (Length(x - fx, y - fy) / (0.1f * c))));
                        f.Mix(C.PetalEdge, Clamp01(0.5f + petals + (line * 0.8f)));
                        f.Mix(C.PetalCenter, Coverage(Length(x - fx, y - fy) - (0.035f * c)));
                        p.Over(f, Coverage(petals));
                    }

                    p.Put(pixels, width, px, py);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The guided spotlight's scrim (spec 005 FR-035): <c>surface.scrim</c>'s color at <see cref="Spotlight.ScrimAlpha"/>
        /// over the whole picture but for the rounded <paramref name="holes"/> (picture pixels; several for the tiles that
        /// block the way), each with a soft edge of <paramref name="feather"/> pixels. Straight alpha.
        /// </summary>
        public static byte[] SpotlightScrim(int width, int height, System.Collections.Generic.IReadOnlyList<Box> holes, float radius, float feather)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            var color = new Color(C.SurfaceScrim);
            float soft = Math.Max(1f, feather);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float d = soft;
                    foreach (Box hole in holes)
                    {
                        if (!hole.IsEmpty && x > hole.Left - soft && x < hole.Right + soft && y > hole.Top - soft && y < hole.Bottom + soft)
                        {
                            d = Math.Min(d, RoundRect(x, y, hole.Left, hole.Top, hole.Right, hole.Bottom, Math.Min(radius, Math.Min(hole.Width, hole.Height) / 2f)));
                        }
                    }

                    Put(pixels, width, px, py, color, Spotlight.ScrimAlpha * Smooth(Clamp01(0.5f + (d / soft))));
                }
            }

            return pixels;
        }

        /// <summary>A pixel built up in layers: premultiplied color and alpha, each layer laid over the ones below.</summary>
        private struct Layered
        {
            private float _r;
            private float _g;
            private float _b;
            private float _a;

            public void Over(Rgba c, float alpha) => Over(new Color(c), alpha * (c.A / 255f));

            public void Over(Color c, float alpha)
            {
                float a = Clamp01(alpha);
                if (a <= 0f)
                {
                    return;
                }

                _r = (c.R * a) + (_r * (1f - a));
                _g = (c.G * a) + (_g * (1f - a));
                _b = (c.B * a) + (_b * (1f - a));
                _a = a + (_a * (1f - a));
            }

            public void Put(byte[] pixels, int width, int px, int py)
            {
                int i = ((py * width) + px) * 4;
                if (_a <= 0f)
                {
                    return;
                }

                pixels[i] = Byte(_r / _a);
                pixels[i + 1] = Byte(_g / _a);
                pixels[i + 2] = Byte(_b / _a);
                pixels[i + 3] = Byte(_a * 255f);
            }
        }
    }
}
