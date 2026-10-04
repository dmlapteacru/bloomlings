using System;
using System.Collections.Generic;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <content>
    /// The bottom menu's wooden pictures (spec 005 FR-030, contracts/look.md §6.7): the plank across the screen with its
    /// vines (<see cref="NavBar"/>) and the raised medallion of the active place (<see cref="NavMedallion"/>). Like the
    /// other material pictures they are straight-alpha RGBA rows from the top, anti-aliased over one pixel and the same
    /// for the same arguments.
    /// </content>
    public static partial class UiRaster
    {
        /// <summary>The seed of the bar's grain.</summary>
        public const int NavBarSeed = 11;

        /// <summary>The seeds of the medallion's rim and face grain.</summary>
        public const int NavRimSeed = 13;

        public const int NavFaceSeed = 5;

        /// <summary>
        /// The bar of the bottom menu (<c>ui.nav.bar</c>) in a <paramref name="width"/> × <paramref name="height"/> picture
        /// laid out by <paramref name="shape"/> (<see cref="BottomNavRegions.Shape"/>):
        /// <list type="bullet">
        /// <item><description>a warm brown plank (<c>wood.dark_top</c> to <c>wood.dark</c>, grained along its length, a
        /// light bevel inside its top edge and ends, its <c>wood.dark_line</c> outline) from its top to the picture's bottom,
        /// its top corners rounded by 0.42 of the band's height (the bottom ones lie below the picture);</description></item>
        /// <item><description>a little darker below the band (the wood behind the bottom inset);</description></item>
        /// <item><description>a thin carved groove between two places (half the band tall, a dark line with a light line
        /// beside it);</description></item>
        /// <item><description>at each end, a green vine curling around the plank's end with a tendril over its top, eight
        /// almond leaves in the <c>garden.leaf_*</c> greens with their <c>garden.leaf_line</c> outline and midrib, and two
        /// white five-petal flowers with yellow middles; mirrored at the right end.</description></item>
        /// </list>
        /// The vines are sized by the band's height, so they keep their shape on every phone.
        /// </summary>
        public static byte[] NavBar(int width, int height, NavBarShape shape)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float left = shape.PlankLeft * width;
            float right = shape.PlankRight * width;
            float top = shape.PlankTop * height;
            float band = Math.Max(4f, (shape.BandBottom - shape.PlankTop) * height);
            float bandBottom = top + band;
            float r = band * 0.42f;
            float bottom = height + r + 2f;
            float line = Math.Max(1.5f, band * 0.025f);
            float bevel = Clamp(band * 0.045f, 1f, 6f);
            int woodTop = Math.Max(0, (int)Math.Floor(top));
            int woodHeight = Math.Max(1, height - woodTop);
            Wood wood = Wood.Of(WoodTone.Dark, width, woodHeight, NavBarSeed);

            float grooveTop = top + (band * (0.5f - (BottomNav.GrooveShare / 2f)));
            float grooveBottom = top + (band * (0.5f + (BottomNav.GrooveShare / 2f)));
            float groove = Math.Max(1f, band * 0.016f);
            var grooves = new float[shape.Grooves.Count];
            for (int i = 0; i < grooves.Length; i++)
            {
                grooves[i] = shape.Grooves[i] * width;
            }

            float unit = 1f / band;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                float v = (y - top) * unit;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    var paint = default(NavPaint);
                    float d = RoundRect(x, y, left, top, right, bottom, r);
                    float cover = Coverage(d);
                    if (cover > 0f)
                    {
                        var c = new Color(wood.Face(px, Math.Max(0, Math.Min(woodHeight - 1, py - woodTop)), horizontal: true));

                        // A warmer face, long grain streaks, darker and lighter, and the band lit from above.
                        c.Mix(C.WoodDarkTop, 0.3f);
                        float streak = Fbm(x / (band * 2.6f), y / (band * 0.07f), NavBarSeed + 7, 3);
                        c.Mix(wood.Line, 0.2f * Clamp01((streak - 0.56f) * 4f));
                        c.Mix(wood.Light, 0.14f * Clamp01((0.42f - streak) * 4f));
                        c.Mix(wood.Light, 0.16f * Clamp01(1f - (v * 2f)));
                        float inner = d + line;
                        if (inner > -(bevel + 3f))
                        {
                            c.Mix(wood.Light, 0.55f * Coverage(inner) * Outside(RoundRect(x, y - bevel, left, top, right, bottom, r) + line, 1f));
                        }

                        // The band's lower edge a little deeper, the wood under it (the bottom inset) darker.
                        c.Mix(wood.Lip, 0.4f * Smooth(Clamp01((y - (bandBottom - (band * 0.1f))) / (band * 0.1f))));
                        if (y > bandBottom)
                        {
                            c.Mix(wood.Lip, 0.3f * Smooth(Clamp01((y - bandBottom) / (band * 0.3f))));
                        }

                        if (y > grooveTop - groove - 2f && y < grooveBottom + groove + 2f)
                        {
                            for (int i = 0; i < grooves.Length; i++)
                            {
                                float gx = grooves[i];
                                if (Math.Abs(x - gx) > (groove * 4f) + 2f)
                                {
                                    continue;
                                }

                                c.Mix(wood.Line, 0.6f * Coverage(Segment(x, y, gx, grooveTop, gx, grooveBottom) - groove));
                                float lx = gx + (groove * 2.2f);
                                c.Mix(wood.Light, 0.35f * Coverage(Segment(x, y, lx, grooveTop, lx, grooveBottom) - (groove * 0.6f)));
                            }
                        }

                        c.Mix(wood.Line, Clamp01(0.5f + inner));
                        paint.Over(cover, c.ToRgba());
                    }

                    if (v > VineTop && v < VineBottom)
                    {
                        float ul = (x - left) * unit;
                        if (ul > VineLeft && ul < VineRight)
                        {
                            PaintVine(ref paint, ul, v, unit);
                        }

                        float ur = (right - x) * unit;
                        if (ur > VineLeft && ur < VineRight)
                        {
                            PaintVine(ref paint, ur, v, unit);
                        }
                    }

                    paint.Write(pixels, ((py * width) + px) * 4);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The raised medallion of the active place (<c>ui.nav.medallion</c>) in a square picture of side
        /// <paramref name="size"/>: a soft shadow under it; a wooden disc <see cref="BottomNav.DiscShare"/> of the picture
        /// wide, its rim in the bar's warm brown wood (a fifth of its radius, a light bevel at its top, the
        /// <c>wood.dark_line</c> outline and a thin line inside), its face lighter honey wood (<c>wood.light</c> grained,
        /// mixed toward <c>wood.grain</c>, lit from the upper left, a shadow under the rim's top and a faint growth ring);
        /// short vines along the rim with eight leaves at its four corners, and two small white flowers, at the lower left
        /// and the right.
        /// </summary>
        public static byte[] NavMedallion(int size)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float c0 = s / 2f;
            float radius = s * BottomNav.DiscShare / 2f;
            float rim = radius * 0.8f;
            float line = Math.Max(1.2f, s * 0.012f);
            Wood dark = Wood.Of(WoodTone.Dark, size, size, NavRimSeed);
            Wood light = Wood.Of(WoodTone.Light, size, size, NavFaceSeed);
            Rgba honey = C.WoodGrain;
            float unit = 1f / s;
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    var paint = default(NavPaint);
                    float dx = x - c0;
                    float dy = y - c0;
                    float dist = Length(dx, dy);

                    // The soft shadow under the disc, a little lower.
                    float shadow = Length(dx, dy - (s * 0.035f)) - radius;
                    if (shadow < s * 0.05f)
                    {
                        paint.Over(0.3f * (1f - Smooth(Clamp01((shadow + (s * 0.01f)) / (s * 0.06f)))), C.GardenShadow);
                    }

                    float cover = Coverage(dist - radius);
                    if (cover > 0f)
                    {
                        Color c;
                        float up = Clamp01(-dy / radius);
                        if (dist > rim)
                        {
                            // The rim: the bar's wood, lighter at its top, with its outline and a thin line inside.
                            c = new Color(dark.Face(px, py, horizontal: true));
                            c.Mix(dark.Light, 0.45f * up * Clamp01(1f - (Math.Abs(dist - ((radius + rim) / 2f)) / ((radius - rim) * 0.6f))));
                            c.Mix(dark.Lip, 0.35f * Clamp01(dy / radius));
                        }
                        else
                        {
                            // The face: honey wood lit from the upper left, a shadow under the rim's top, a faint ring.
                            c = new Color(light.Face(px, py, horizontal: true));
                            c.Mix(honey, 0.35f);
                            c.Mix(C.WoodLight, 0.35f * Clamp01(-(dx + dy) / (radius * 1.4f)));
                            c.Mix(C.WoodEdge, 0.3f * Clamp01((dx + dy) / (radius * 1.4f)));
                            c.Mix(C.GardenShadow, 0.22f * up * (1f - Smooth(Clamp01((rim - dist) / (s * 0.08f)))));
                            c.Mix(C.WoodGrain, 0.3f * Coverage(Math.Abs(dist - (rim * 0.64f)) - 0.6f));
                        }

                        c.Mix(dark.Line, Coverage(Math.Abs(dist - rim) - (line * 0.45f)));
                        c.Mix(dark.Line, Clamp01(0.5f + (dist - radius) + line));
                        paint.Over(cover, c.ToRgba());
                    }

                    // The vines over the rim, in shares of the picture from its middle.
                    float u = dx * unit;
                    float v = dy * unit;
                    if (Math.Abs(u) > 0.2f || Math.Abs(v) > 0.2f)
                    {
                        PaintMedallionVines(ref paint, u, v, unit);
                    }

                    paint.Write(pixels, ((py * size) + px) * 4);
                }
            }

            return pixels;
        }

        // ---- The vines at the bar's ends (units: the band's height; u inward from the plank's end, v down from its top) ----

        private const float VineLeft = -0.4f;

        private const float VineRight = 1f;

        private const float VineTop = -0.42f;

        private const float VineBottom = 1.2f;

        /// <summary>The stem curling around the plank's end: in over the top, down around the end, back along the bottom.</summary>
        private static readonly NavStroke Stem = NavStroke.Through(
            new[] { 0.8f, 0.5f, 0.2f, -0.02f, -0.1f, -0.05f, 0.15f, 0.45f, 0.74f },
            new[] { 0.03f, -0.06f, -0.05f, 0.12f, 0.42f, 0.72f, 0.93f, 1f, 0.96f },
            0.028f,
            0.018f);

        /// <summary>The tendril curling up from the stem over the plank's top.</summary>
        private static readonly NavStroke Tendril = NavStroke.Spiral(0.42f, -0.19f, 0.12f, 75f, -480f, 0.7f, 0.014f, 0.012f);

        private static readonly NavLeaf[] VineLeaves =
        {
            NavLeaf.At(0.62f, -0.03f, -55f, 0.27f, 0.075f, 0),
            NavLeaf.At(0.28f, -0.06f, -118f, 0.28f, 0.078f, 2),
            NavLeaf.At(0.02f, 0.07f, -165f, 0.25f, 0.072f, 1),
            NavLeaf.At(-0.09f, 0.32f, 228f, 0.23f, 0.07f, 0),
            NavLeaf.At(-0.08f, 0.6f, 132f, 0.23f, 0.07f, 2),
            NavLeaf.At(0.3f, 0.98f, -14f, 0.26f, 0.075f, 1),
            NavLeaf.At(0.62f, 0.97f, -42f, 0.21f, 0.065f, 0),
            NavLeaf.At(0.1f, 0.9f, 98f, 0.18f, 0.06f, 2),
        };

        private static readonly (float X, float Y, float R, float Turn)[] VineFlowers =
        {
            (-0.03f, 0.62f, 0.16f, 0.3f),
            (0.5f, -0.15f, 0.11f, 0.9f),
        };

        private static void PaintVine(ref NavPaint paint, float u, float v, float pixel)
        {
            Stem.Paint(ref paint, u, v, pixel);
            Tendril.Paint(ref paint, u, v, pixel);
            foreach (NavLeaf leaf in VineLeaves)
            {
                leaf.Paint(ref paint, u, v, pixel);
            }

            foreach ((float fx, float fy, float fr, float turn) in VineFlowers)
            {
                PaintFlower(ref paint, fx, fy, fr, turn, u, v, pixel);
            }
        }

        // ---- The medallion's vines (units: the picture's side, from its middle) ----

        private static readonly NavStroke[] RimStems =
        {
            NavStroke.Arc(0f, 0f, 0.43f, -165f, -110f, 0.012f, 0.008f),
            NavStroke.Arc(0f, 0f, 0.43f, -70f, -15f, 0.012f, 0.008f),
            NavStroke.Arc(0f, 0f, 0.43f, 110f, 165f, 0.012f, 0.008f),
            NavStroke.Arc(0f, 0f, 0.43f, 15f, 70f, 0.012f, 0.008f),
        };

        private static readonly NavLeaf[] RimLeaves =
        {
            NavLeaf.At(-0.24f, -0.344f, -175f, 0.15f, 0.052f, 0),
            NavLeaf.At(-0.24f, -0.344f, -95f, 0.12f, 0.045f, 1),
            NavLeaf.At(0.27f, -0.32f, -10f, 0.15f, 0.052f, 2),
            NavLeaf.At(0.27f, -0.32f, -80f, 0.12f, 0.045f, 0),
            NavLeaf.At(-0.32f, 0.27f, 175f, 0.14f, 0.05f, 1),
            NavLeaf.At(-0.32f, 0.27f, 100f, 0.12f, 0.045f, 2),
            NavLeaf.At(0.32f, 0.27f, 5f, 0.14f, 0.05f, 0),
            NavLeaf.At(0.32f, 0.27f, 80f, 0.12f, 0.045f, 1),
        };

        private static readonly (float X, float Y, float R, float Turn)[] RimFlowers =
        {
            (-0.3f, 0.33f, 0.075f, 0.4f),
            (0.38f, 0.17f, 0.065f, 1.1f),
        };

        private static void PaintMedallionVines(ref NavPaint paint, float u, float v, float pixel)
        {
            foreach (NavStroke stem in RimStems)
            {
                stem.Paint(ref paint, u, v, pixel);
            }

            foreach (NavLeaf leaf in RimLeaves)
            {
                leaf.Paint(ref paint, u, v, pixel);
            }

            foreach ((float fx, float fy, float fr, float turn) in RimFlowers)
            {
                PaintFlower(ref paint, fx, fy, fr, turn, u, v, pixel);
            }
        }

        // ---- Leaves, flowers and stems ----

        /// <summary>The share of a pixel a shape covers at distance <paramref name="d"/> (units), one pixel <paramref name="pixel"/> wide.</summary>
        private static float Cover(float d, float pixel) => Clamp01(0.5f - (d / pixel));

        /// <summary>
        /// A white five-petal flower (<c>garden.flower</c>, its <c>garden.flower_line</c> outline) with a yellow middle
        /// (<c>garden.flower_center</c>, <c>garden.flower_center_line</c>), centered at (<paramref name="cx"/>,
        /// <paramref name="cy"/>), its petals out to about <paramref name="r"/>, turned by <paramref name="turn"/> radians.
        /// </summary>
        private static void PaintFlower(ref NavPaint paint, float cx, float cy, float r, float turn, float x, float y, float pixel)
        {
            float dx = x - cx;
            float dy = y - cy;
            float grow = Math.Max(pixel, r * 0.09f);
            if (Math.Abs(dx) > r + grow + pixel || Math.Abs(dy) > r + grow + pixel)
            {
                return;
            }

            float d = Length(dx, dy) - (0.45f * r);
            for (int i = 0; i < 5; i++)
            {
                float a = turn + (i * 2f * (float)Math.PI / 5f);
                d = Math.Min(d, Length(dx - (0.55f * r * (float)Math.Cos(a)), dy - (0.55f * r * (float)Math.Sin(a))) - (0.42f * r));
            }

            paint.Over(Cover(d - grow, pixel), C.GardenFlowerLine);
            paint.Over(Cover(d, pixel), C.GardenFlower);
            float m = Length(dx, dy) - (0.3f * r);
            paint.Over(Cover(m - (r * 0.07f), pixel), C.GardenFlowerCenterLine);
            paint.Over(Cover(m, pixel), C.GardenFlowerCenter);
        }

        /// <summary>One almond leaf: a lens from its base to its tip, its fill one of the garden greens.</summary>
        private readonly struct NavLeaf
        {
            private NavLeaf(float ax, float ay, float bx, float by, float half, Rgba fill)
            {
                Ax = ax;
                Ay = ay;
                Bx = bx;
                By = by;
                Half = half;
                Fill = fill;
                float dx = bx - ax;
                float dy = by - ay;
                Length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                Radius = (((Length / 2f) * (Length / 2f)) + (half * half)) / (2f * half);
                MinX = Math.Min(ax, bx) - half;
                MaxX = Math.Max(ax, bx) + half;
                MinY = Math.Min(ay, by) - half;
                MaxY = Math.Max(ay, by) + half;
            }

            private float Ax { get; }

            private float Ay { get; }

            private float Bx { get; }

            private float By { get; }

            private float Half { get; }

            private Rgba Fill { get; }

            private float Length { get; }

            private float Radius { get; }

            private float MinX { get; }

            private float MaxX { get; }

            private float MinY { get; }

            private float MaxY { get; }

            /// <summary>A leaf from (x, y), <paramref name="degrees"/> clockwise from the right (y down), in green <paramref name="shade"/>.</summary>
            public static NavLeaf At(float x, float y, float degrees, float length, float half, int shade)
            {
                float a = degrees * (float)Math.PI / 180f;
                Rgba fill = (shade % 3) switch
                {
                    0 => C.GardenLeaf1,
                    1 => C.GardenLeaf2,
                    _ => C.GardenLeaf3,
                };
                return new NavLeaf(x, y, x + (length * (float)Math.Cos(a)), y + (length * (float)Math.Sin(a)), half, fill);
            }

            public void Paint(ref NavPaint paint, float x, float y, float pixel)
            {
                float grow = Math.Max(pixel * 1.1f, Half * 0.2f);
                float reach = grow + pixel;
                if (x < MinX - reach || x > MaxX + reach || y < MinY - reach || y > MaxY + reach)
                {
                    return;
                }

                float dx = (Bx - Ax) / Length;
                float dy = (By - Ay) / Length;
                float along = ((x - Ax) * dx) + ((y - Ay) * dy);
                float across = (-(x - Ax) * dy) + ((y - Ay) * dx);
                float l = Length / 2f;
                float offset = Radius - Half;
                float a = along - l;
                float d = Math.Max(UiRaster.Length(a, across - offset) - Radius, UiRaster.Length(a, across + offset) - Radius);
                if (d - grow > pixel)
                {
                    return;
                }

                paint.Over(Cover(d - grow, pixel), C.GardenLeafLine);
                float inside = Cover(d, pixel);
                if (inside <= 0f)
                {
                    return;
                }

                // One side of the midrib lighter, the other a little deeper, and the midrib itself.
                Rgba fill = across < 0f ? Fill.Lighten(0.22f * Clamp01(-across / Half)) : Fill.Darken(0.16f * Clamp01(across / Half));
                paint.Over(inside, fill);
                float t = Clamp01(along / Length);
                if (t > 0.08f && t < 0.88f)
                {
                    float rib = Math.Abs(across) - Math.Max(pixel * 0.5f, Half * 0.09f);
                    paint.Over(0.5f * inside * Cover(rib, pixel), C.GardenLeafLine);
                }
            }
        }

        /// <summary>A green stem: a polyline with a half width, outlined in <c>garden.leaf_line</c>, filled <c>garden.leaf_3</c>.</summary>
        private sealed class NavStroke
        {
            private readonly float[] _x;
            private readonly float[] _y;
            private readonly float _half;
            private readonly float _line;
            private readonly float _minX;
            private readonly float _maxX;
            private readonly float _minY;
            private readonly float _maxY;

            private NavStroke(float[] x, float[] y, float half, float line)
            {
                _x = x;
                _y = y;
                _half = half;
                _line = line;
                _minX = float.MaxValue;
                _maxX = float.MinValue;
                _minY = float.MaxValue;
                _maxY = float.MinValue;
                for (int i = 0; i < x.Length; i++)
                {
                    _minX = Math.Min(_minX, x[i]);
                    _maxX = Math.Max(_maxX, x[i]);
                    _minY = Math.Min(_minY, y[i]);
                    _maxY = Math.Max(_maxY, y[i]);
                }
            }

            /// <summary>A smooth stem through the points (a Catmull-Rom curve, eight samples between two points).</summary>
            public static NavStroke Through(float[] xs, float[] ys, float half, float line)
            {
                const int Steps = 8;
                var x = new List<float>();
                var y = new List<float>();
                int n = xs.Length;
                for (int i = 0; i < n - 1; i++)
                {
                    int i0 = Math.Max(0, i - 1);
                    int i3 = Math.Min(n - 1, i + 2);
                    for (int k = 0; k < Steps; k++)
                    {
                        float t = k / (float)Steps;
                        x.Add(CatmullRom(xs[i0], xs[i], xs[i + 1], xs[i3], t));
                        y.Add(CatmullRom(ys[i0], ys[i], ys[i + 1], ys[i3], t));
                    }
                }

                x.Add(xs[n - 1]);
                y.Add(ys[n - 1]);
                return new NavStroke(x.ToArray(), y.ToArray(), half, line);
            }

            /// <summary>
            /// A tendril: a spiral about (<paramref name="cx"/>, <paramref name="cy"/>) from <paramref name="radius"/> at
            /// <paramref name="from"/> degrees, turning by <paramref name="turn"/> degrees while its radius shrinks by
            /// <paramref name="shrink"/> of itself.
            /// </summary>
            public static NavStroke Spiral(float cx, float cy, float radius, float from, float turn, float shrink, float half, float line)
            {
                const int Steps = 28;
                var x = new float[Steps + 1];
                var y = new float[Steps + 1];
                for (int k = 0; k <= Steps; k++)
                {
                    float t = k / (float)Steps;
                    float a = (from + (turn * t)) * (float)Math.PI / 180f;
                    float r = radius * (1f - (shrink * t));
                    x[k] = cx + (r * (float)Math.Cos(a));
                    y[k] = cy + (r * (float)Math.Sin(a));
                }

                return new NavStroke(x, y, half, line);
            }

            /// <summary>An arc about (<paramref name="cx"/>, <paramref name="cy"/>) from <paramref name="from"/> to <paramref name="to"/> degrees.</summary>
            public static NavStroke Arc(float cx, float cy, float radius, float from, float to, float half, float line) =>
                Spiral(cx, cy, radius, from, to - from, 0f, half, line);

            public void Paint(ref NavPaint paint, float x, float y, float pixel)
            {
                float reach = _half + _line + pixel;
                if (x < _minX - reach || x > _maxX + reach || y < _minY - reach || y > _maxY + reach)
                {
                    return;
                }

                float d = float.MaxValue;
                for (int i = 0; i < _x.Length - 1; i++)
                {
                    d = Math.Min(d, Segment(x, y, _x[i], _y[i], _x[i + 1], _y[i + 1]));
                }

                d -= _half;
                if (d - _line > pixel)
                {
                    return;
                }

                paint.Over(Cover(d - _line, pixel), C.GardenLeafLine);
                paint.Over(Cover(d, pixel), C.GardenLeaf3);
                paint.Over(0.45f * Cover(d + (_half * 0.55f), pixel), C.GardenLeaf2);
            }

            private static float CatmullRom(float p0, float p1, float p2, float p3, float t) =>
                0.5f * ((2f * p1) + ((p2 - p0) * t) + (((2f * p0) - (5f * p1) + (4f * p2) - p3) * t * t) + (((3f * p1) - p0 - (3f * p2) + p3) * t * t * t));
        }

        /// <summary>A pixel being painted, in premultiplied floats: shapes over shapes, then straight-alpha bytes.</summary>
        private struct NavPaint
        {
            private float _r;
            private float _g;
            private float _b;
            private float _a;

            /// <summary>Paints <paramref name="color"/> over the pixel where it covers <paramref name="cover"/> of it.</summary>
            public void Over(float cover, Rgba color)
            {
                float a = Clamp01(cover) * (color.A / 255f);
                if (a <= 0f)
                {
                    return;
                }

                float keep = 1f - a;
                _r = (color.R * a) + (_r * keep);
                _g = (color.G * a) + (_g * keep);
                _b = (color.B * a) + (_b * keep);
                _a = a + (_a * keep);
            }

            /// <summary>Writes the pixel as straight-alpha bytes.</summary>
            public void Write(byte[] rgba, int at)
            {
                if (_a <= 0f)
                {
                    return;
                }

                rgba[at] = Byte(_r / _a);
                rgba[at + 1] = Byte(_g / _a);
                rgba[at + 2] = Byte(_b / _a);
                rgba[at + 3] = Byte(_a * 255f);
            }
        }
    }
}
