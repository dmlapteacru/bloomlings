using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The latch's padlock and its key (rules 17 §8 «Засов и ключ»; the owner's look of 2026-11-01, as the reference game's: spec 006
    /// FR-068, contracts/presentation.md): engine-free flat 2D pictures lit from the upper left, straight alpha,
    /// deterministic. The padlock (<see cref="Padlock"/>) hangs on the pod the latch holds, our acorn hero (the variant's 2D
    /// character, drawn by the host) hugging it as if he had locked the pod himself; the board's key
    /// (<see cref="RoundKey"/>) is an ordinary big key in the held pod's colour. Two latches whose pods share a colour carry
    /// 1–3 white pips on both their padlock and their key (<see cref="RoundAnimator.LatchPips"/>); otherwise no mark.
    /// </summary>
    public static partial class UiRaster
    {
        /// <summary>
        /// A padlock (<c>pod.round.padlock</c>) in a square of side <paramref name="size"/>, as <see cref="RoundLook.LatchParts"/>
        /// places it on the held pod: a brass body (<see cref="RoundLook.PadlockBrass"/>, a thick front face, a darker base edge,
        /// a lit rim and a dark keyhole) under a steel shackle (<see cref="RoundLook.PadlockSteel"/>), outlined in dark ink;
        /// <paramref name="pips"/> white pips (0–3) in a row under the keyhole; closed, the acorn hero's two little arms reach
        /// round the shackle (<paramref name="arms"/>); <paramref name="open"/> lifts the shackle out of the body (the opening).
        /// </summary>
        public static byte[] Padlock(int size, int pips, bool open, bool arms)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            Rgba brass = RoundLook.PadlockBrass;
            Rgba steel = RoundLook.PadlockSteel;
            Rgba ink = RoundLook.PadlockInk;
            float line = Math.Max(1f, s * 0.03f);

            // The body: a rounded box in the lower half, its front face (the bottom band) darker.
            float bl = s * 0.24f;
            float br = s * 0.76f;
            float bt = s * 0.53f;
            float bb = s * 0.95f;
            float bodyR = s * 0.09f;
            float depth = s * 0.075f;

            // The shackle: a U of a thick round bar over the body, its legs going into the body.
            float lift = open ? s * 0.12f : 0f;
            float shCx = s * 0.5f;
            float shR = s * 0.15f;
            float shBar = s * 0.07f;
            float shCy = (s * 0.45f) - lift;
            float legBottom = bt + (s * 0.04f) - lift;

            // The arms: from the acorn's sides (at the picture's top) down round to the shackle's sides.
            Rgba nut = RoundLook.AcornNut;
            float armW = s * 0.06f;
            (float X, float Y)[] armA = { (s * 0.17f, s * 0.05f), (s * 0.2f, s * 0.3f), (s * 0.33f, s * 0.44f) };
            (float X, float Y)[] armB = { (s * 0.83f, s * 0.05f), (s * 0.8f, s * 0.3f), (s * 0.67f, s * 0.44f) };

            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    var c = new Color(ink);
                    float a = 0f;

                    // The shackle (behind the body): a ring's upper half and two legs.
                    float ring = Math.Abs(Length(x - shCx, y - shCy) - shR) - (shBar / 2f);
                    if (y > shCy)
                    {
                        float legL = Math.Abs(x - (shCx - shR)) - (shBar / 2f);
                        float legR = Math.Abs(x - (shCx + shR)) - (shBar / 2f);
                        float legY = y - legBottom;
                        ring = Math.Max(Math.Min(legL, open ? Math.Max(legR, y - (shCy + (s * 0.06f))) : legR), legY);
                    }

                    if (ring - line < 0.5f)
                    {
                        Over(ref c, ref a, ink, Coverage(ring - line));
                        float sc = Coverage(ring);
                        if (sc > 0f)
                        {
                            // lit along its upper left
                            float lit = Clamp01(0.5f - ((x - shCx) / (shR * 2.4f)) - ((y - shCy) / (shR * 3f)));
                            Rgba col = steel.Darken(0.25f).Mix(steel.Lighten(0.45f), lit);
                            float core = Math.Abs(Length(x - shCx, y - shCy) - shR);
                            col = col.Mix(Rgba.White, 0.35f * Clamp01(1f - (core / (shBar * 0.22f))) * (y < shCy ? 1f : 0.6f));
                            Over(ref c, ref a, col, sc);
                        }
                    }

                    // The body with its front face and outline.
                    float body = RoundRect(x, y, bl, bt, br, bb, bodyR);
                    if (body - line < 0.5f)
                    {
                        Over(ref c, ref a, ink, Coverage(body - line));
                        float cover = Coverage(body);
                        if (cover > 0f)
                        {
                            bool front = y > bb - depth;
                            float shade = Clamp01((x - bl) / (br - bl));
                            Rgba col = front
                                ? brass.Darken(0.28f).Mix(brass.Darken(0.42f), shade)
                                : brass.Lighten(0.22f).Mix(brass.Darken(0.12f), Clamp01((shade * 0.7f) + ((y - bt) / (bb - bt) * 0.5f)));

                            // a lit rim along the top and the left
                            float rim = -body;
                            if (!front && rim < s * 0.05f)
                            {
                                float k = Clamp01(1f - (rim / (s * 0.05f)));
                                bool lit = (y - bt) < (s * 0.1f) || (x - bl) < (s * 0.1f);
                                col = col.Mix(lit ? Rgba.White : brass.Darken(0.3f), (lit ? 0.45f : 0.25f) * k);
                            }

                            // a soft shine on the upper left of the face
                            float shine = Length((x - (bl + (s * 0.13f))) / (s * 0.08f), (y - (bt + (s * 0.09f))) / (s * 0.04f));
                            col = col.Mix(Rgba.White, 0.5f * Clamp01(1f - shine));
                            Over(ref c, ref a, col, cover);

                            // the keyhole: a round head over a tapering slot
                            float kx = s * 0.5f;
                            float ky = bt + ((bb - depth - bt) * (pips > 0 ? 0.33f : 0.42f));
                            float head = Length(x - kx, y - ky) - (s * 0.05f);
                            float slotHalf = (s * 0.025f) + ((y - ky) * 0.18f);
                            float slot = Math.Max(Math.Abs(x - kx) - slotHalf, Math.Max(ky - y, y - (ky + (s * 0.12f))));
                            float hole = Math.Min(head, slot);
                            Over(ref c, ref a, brass.Lighten(0.35f), Coverage(hole - Math.Max(0.8f, s * 0.012f)) * Clamp01((y - ky + (s * 0.08f)) / (s * 0.12f)));
                            Over(ref c, ref a, ink, Coverage(hole));

                            // the pips: white dots in a dark ring, in a row under the keyhole
                            if (pips > 0)
                            {
                                float pr = s * 0.035f;
                                float py2 = bb - depth - (s * 0.055f);
                                for (int k = 0; k < pips; k++)
                                {
                                    float pcx = kx + ((k - ((pips - 1) / 2f)) * pr * 2.9f);
                                    float d = Length(x - pcx, y - py2);
                                    Over(ref c, ref a, ink, Coverage(d - pr - Math.Max(0.8f, s * 0.012f)));
                                    Over(ref c, ref a, Rgba.White, Coverage(d - pr));
                                }
                            }
                        }
                    }

                    // The acorn hero's arms, hugging the shackle (in front of it, behind the body's top).
                    if (arms)
                    {
                        foreach ((float X, float Y)[] arm in new[] { armA, armB })
                        {
                            float d = Math.Min(Segment(x, y, arm[0].X, arm[0].Y, arm[1].X, arm[1].Y), Segment(x, y, arm[1].X, arm[1].Y, arm[2].X, arm[2].Y)) - armW;
                            float hand = Length(x - arm[2].X, y - arm[2].Y) - (armW * 1.35f);
                            float limb = Math.Min(d, hand);
                            if (limb - line < 0.5f)
                            {
                                Over(ref c, ref a, RoundLook.AcornInk, Coverage(limb - line));
                                float lit = Clamp01(0.5f - ((y - arm[2].Y) / (s * 0.3f)));
                                Over(ref c, ref a, nut.Darken(0.1f).Mix(nut.Lighten(0.2f), lit), Coverage(limb));
                            }
                        }
                    }

                    if (a > 0f)
                    {
                        Put(pixels, size, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a padlock's picture (<see cref="Padlock"/>), the same in both builds.</summary>
        public static string PadlockKey(int pips, bool open, bool arms) =>
            RoundLook.PadlockSlot + "/" + pips.ToString(System.Globalization.CultureInfo.InvariantCulture) + (open ? "/open" : string.Empty) + (arms ? "/arms" : string.Empty);

        /// <summary>
        /// The board's key (<c>board.round.key</c>) in <paramref name="width"/> × <paramref name="height"/> (the box of
        /// <see cref="RoundLook.KeyBox"/>): an ordinary key (a round bow with its hole, a collar, a shaft, a bit with two teeth)
        /// tilted up to the right, its middle at the picture's middle, in <paramref name="metal"/> (the held pod's colour, or
        /// <see cref="RoundLook.KeyBrass"/>), volumetric as the cubes: a darker side face below right, the face lit from the
        /// upper left with a rim light and a shine along the shaft, a dark outline and a soft light halo round it so it reads on
        /// cubes and surface alike; <paramref name="pips"/> (0–3) white pips on the bow.
        /// </summary>
        public static byte[] RoundKey(int width, int height, Rgba metal, int pips)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float len = Math.Min(width / RoundLook.KeyBoxWidth, height / RoundLook.KeyBoxHeight);
            float cx = width / 2f;
            float cy = height / 2f;
            const float Cos = 0.93358f; // cos 21°
            const float Sin = 0.35837f; // sin 21°
            float line = Math.Max(1.2f, len * 0.02f);
            float halo = len * 0.1f;
            float depthX = len * 0.026f;
            float depthY = len * 0.05f;
            Rgba ink = metal.Darken(0.62f);
            Rgba side = metal.Darken(0.38f);
            float bevel = len * 0.035f;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;

                    // The side face: the key's shape swept down and to the right.
                    float sweep = float.MaxValue;
                    for (int k = 1; k <= 4; k++)
                    {
                        float t = k / 4f;
                        sweep = Math.Min(sweep, KeyShape(x - (depthX * t), y - (depthY * t), cx, cy, len, Cos, Sin));
                    }

                    float face = KeyShape(x, y, cx, cy, len, Cos, Sin);
                    float all = Math.Min(face, sweep);
                    if (all > halo + line)
                    {
                        continue;
                    }

                    var c = new Color(Rgba.White);
                    float a = 0f;

                    // The halo, the outline, the side face, then the face.
                    float glow = Clamp01(1f - ((all - line) / halo));
                    Over(ref c, ref a, RoundLook.KeyHalo, 0.95f * (float)Math.Sqrt(glow) * glow);
                    Over(ref c, ref a, ink, Coverage(all - line));
                    Over(ref c, ref a, side, Coverage(sweep));
                    float cover = Coverage(face);
                    if (cover > 0f)
                    {
                        // across the key, light from the upper left
                        float u = ((x - cx) * Cos) - ((y - cy) * Sin);
                        float v = ((x - cx) * Sin) + ((y - cy) * Cos);
                        float tone = Clamp01(0.5f + (v / (len * 0.4f)) + (u / (len * 3f)));
                        Rgba col = metal.Lighten(0.2f).Mix(metal.Darken(0.12f), tone);

                        // the bevel: lit where the edge faces the upper left, shaded where it faces the lower right
                        float inside = -face;
                        if (inside < bevel)
                        {
                            float gx = KeyShape(x + 1f, y, cx, cy, len, Cos, Sin) - KeyShape(x - 1f, y, cx, cy, len, Cos, Sin);
                            float gy = KeyShape(x, y + 1f, cx, cy, len, Cos, Sin) - KeyShape(x, y - 1f, cx, cy, len, Cos, Sin);
                            float gl = Math.Max(0.001f, Length(gx, gy));
                            float facing = -((gx / gl) + (gy / gl)) * 0.7071f;
                            float k = Clamp01(1f - (inside / bevel));
                            col = facing > 0f ? col.Mix(Rgba.White, 0.55f * facing * k) : col.Mix(metal.Darken(0.35f), -0.5f * facing * k);
                        }

                        // a shine along the shaft's top and on the bow's upper left
                        float shaftShine = (u > -len * 0.1f && u < len * 0.42f) ? Clamp01(1f - (Math.Abs(v + (len * 0.022f)) / (len * 0.012f))) : 0f;
                        float bowShine = Length(((x - cx) * Cos) - ((y - cy) * Sin) + (len * 0.36f), (((x - cx) * Sin) + ((y - cy) * Cos) + (len * 0.1f)) * 1.6f) / (len * 0.06f);
                        col = col.Mix(Rgba.White, Math.Max(0.55f * shaftShine, 0.6f * Clamp01(1f - bowShine)));
                        Over(ref c, ref a, col, cover);

                        if (pips > 0)
                        {
                            // On the bow's ring below its hole, a row of white pips in a dark ring.
                            float pr = len * 0.034f;
                            for (int k = 0; k < pips; k++)
                            {
                                float pu = -len * 0.295f + ((k - ((pips - 1) / 2f)) * pr * 2.7f);
                                float pv = len * 0.13f;
                                float d = Length(u - pu, v - pv);
                                Over(ref c, ref a, ink, Coverage(d - pr - Math.Max(0.8f, len * 0.01f)));
                                Over(ref c, ref a, Rgba.White, Coverage(d - pr));
                            }
                        }
                    }

                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a key's picture (<see cref="RoundKey"/>), the same in both builds.</summary>
        public static string RoundKeyKey(Rgba metal, int pips) =>
            RoundLook.KeySlot + "/" + metal.R.ToString("X2", System.Globalization.CultureInfo.InvariantCulture) + metal.G.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)
            + metal.B.ToString("X2", System.Globalization.CultureInfo.InvariantCulture) + "/" + pips.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// The drawn acorn (the stand-in for the acorn hero's picture <c>2d/acorn-happy</c> when it is missing) in a square of
        /// side <paramref name="size"/>: a warm nut under its darker scaled cap with a stem, outlined, two dot eyes and a smile.
        /// </summary>
        public static byte[] AcornFigure(int size)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float unit = s * 0.42f;
            float cx = s * 0.5f;
            float cy = s * 0.52f;
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    float u = (x - cx) / unit;
                    float v = -(y - cy) / unit;
                    float solid = ShapeLibrary.AcornSolid(u, v) * unit;
                    float outline = Coverage(solid - Math.Max(1f, s * 0.035f));
                    if (outline <= 0f)
                    {
                        continue;
                    }

                    var c = new Color(RoundLook.AcornInk);
                    Acorn(ref c, x, y, cx, cy, unit, s * 0.035f);
                    float eye = Math.Min(Length(x - (cx - (s * 0.1f)), y - (s * 0.66f)), Length(x - (cx + (s * 0.1f)), y - (s * 0.66f))) - (s * 0.035f);
                    c.Mix(RoundLook.AcornInk, Coverage(eye));
                    float smile = Math.Abs(Length(x - cx, y - (s * 0.66f)) - (s * 0.08f)) - (s * 0.014f);
                    c.Mix(RoundLook.AcornInk, Coverage(smile) * (y > s * 0.7f ? 1f : 0f));
                    Put(pixels, size, px, py, c, outline);
                }
            }

            return pixels;
        }

        /// <summary>The cache key of the drawn acorn (<see cref="AcornFigure"/>).</summary>
        public const string AcornFigureKey = RoundLook.PadlockSlot + "/acorn";

        // The key's signed distance (pixels) at (x, y): its length len along an axis tilted up to the right through (cx, cy),
        // the middle of the key at (cx, cy). Bow (a ring), collar, shaft and a bit with two teeth below the shaft's end.
        private static float KeyShape(float x, float y, float cx, float cy, float len, float cos, float sin)
        {
            float u = (((x - cx) * cos) - ((y - cy) * sin)) / len;
            float v = (((x - cx) * sin) + ((y - cy) * cos)) / len;
            float bowCx = -0.29f;
            float ring = Math.Max(Length(u - bowCx, v) - 0.21f, 0.08f - Length(u - (bowCx - 0.015f), v + 0.01f));
            float collar = RoundRectUnit(u, v, -0.12f, -0.085f, -0.05f, 0.085f, 0.03f);
            float shaft = RoundRectUnit(u, v, -0.12f, -0.058f, 0.48f, 0.058f, 0.05f);
            float bit = RoundRectUnit(u, v, 0.2f, -0.01f, 0.46f, 0.22f, 0.03f);
            float notch = RoundRectUnit(u, v, 0.285f, 0.11f, 0.355f, 0.32f, 0.018f);
            float bitCut = Math.Max(bit, -notch);
            return Math.Min(Math.Min(ring, collar), Math.Min(shaft, bitCut)) * len;
        }

        private static float RoundRectUnit(float x, float y, float l, float t, float r, float b, float radius) => RoundRect(x, y, l, t, r, b, radius);

        // Straight-alpha "over": paints col at coverage t over the pixel so far (c, a).
        private static void Over(ref Color c, ref float a, Rgba col, float t)
        {
            t = Clamp01(t) * (col.A / 255f);
            if (t <= 0f)
            {
                return;
            }

            float na = t + (a * (1f - t));
            float k = t / na;
            c.R += (col.R - c.R) * k;
            c.G += (col.G - c.G) * k;
            c.B += (col.B - c.B) * k;
            a = na;
        }

        // The acorn (ShapeLibrary's acorn: a cap over a round nut with a soft point) at (cx, cy), unit px a shape unit: a dark
        // outline, the nut in warm acorn brown with a light spot at its upper left, the cap darker with a lit rim.
        private static void Acorn(ref Color c, float x, float y, float cx, float cy, float unit, float line)
        {
            float u = (x - cx) / unit;
            float v = -(y - cy) / unit;
            if (Math.Abs(u) > 1.4f || Math.Abs(v) > 1.4f)
            {
                return;
            }

            float solid = ShapeLibrary.AcornSolid(u, v) * unit;
            float cap = ShapeLibrary.AcornCap(u, v) * unit;
            Rgba ink = RoundLook.AcornInk;
            c.Mix(ink, Coverage(solid - line));
            float inside = Coverage(solid);
            if (inside <= 0f)
            {
                return;
            }

            Rgba nut = RoundLook.AcornNut;
            Rgba capColor = RoundLook.AcornCap;
            float shade = Clamp01((u + 0.6f) / 1.4f);
            var body = new Color(nut.Lighten(0.12f).Mix(nut.Darken(0.12f), shade));
            float spot = Length((u + 0.22f) / 0.13f, (v + 0.12f) / 0.22f);
            body.Mix(Rgba.White, 0.55f * Clamp01(1f - spot));
            float capCover = Coverage(cap);
            if (capCover > 0f)
            {
                body.Mix(capColor.Lighten(0.1f).Mix(capColor.Darken(0.15f), shade), capCover);

                // the cap's scales: a faint cross-hatch
                float hatch = Math.Abs((float)Math.Sin(((u + v) * 14f)) * (float)Math.Sin(((u - v) * 14f)));
                body.Mix(capColor.Darken(0.35f), 0.25f * capCover * Clamp01(1f - (hatch * 3f)));
            }

            c.Mix(body.ToRgba(), inside);
        }
    }
}
