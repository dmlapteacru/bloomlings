using System;
using System.Globalization;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The latch's padlock, its key and their soft shadows (rules 17 §8 «Засов и ключ»; the owner's look of 2026-11-02: spec 006
    /// FR-068, contracts/presentation.md): engine-free flat 2D pictures lit from the upper left, straight alpha, deterministic.
    /// The padlock (<see cref="Padlock"/>) is held by our Twig hero in the place of the pod the latch holds, its body in that
    /// pod's colour; the board's key (<see cref="UprightKey"/>) is an ordinary big key standing upright on its tip in the same
    /// colour, its turning back half the same picture in a darker metal (<see cref="RoundLook.KeyBack"/>). Two latches whose
    /// pods share a colour carry 1–3 white pips on both their padlock and their key (<see cref="RoundAnimator.LatchPips"/>);
    /// otherwise no mark.
    /// </summary>
    public static partial class UiRaster
    {
        /// <summary>
        /// A padlock (<c>pod.round.padlock</c>) in a square of side <paramref name="size"/>, as <see cref="RoundLook.HeroLock"/>
        /// places it in front of the hero's belly: a body in <paramref name="body"/> (the held pod's colour,
        /// <see cref="RoundLook.KeyMetal"/>; a thick front face, a darker base edge, a lit rim, a soft shine and a dark keyhole
        /// in a light steel ring so it reads on any colour) under a steel shackle (<see cref="RoundLook.PadlockSteel"/>), outlined
        /// in dark ink (<see cref="RoundLook.PadlockInk"/>); <paramref name="pips"/> white pips (0–3) in a row under the keyhole;
        /// <paramref name="open"/> lifts the shackle out of the body, one leg free (the opening).
        /// </summary>
        public static byte[] Padlock(int size, Rgba body, int pips, bool open)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
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
            float kx = s * 0.5f;
            float ky = s * RoundLook.KeyholeShare(pips);

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
                    float bodyD = RoundRect(x, y, bl, bt, br, bb, bodyR);
                    if (bodyD - line < 0.5f)
                    {
                        Over(ref c, ref a, ink, Coverage(bodyD - line));
                        float cover = Coverage(bodyD);
                        if (cover > 0f)
                        {
                            bool front = y > bb - depth;
                            float shade = Clamp01((x - bl) / (br - bl));
                            Rgba col = front
                                ? body.Darken(0.28f).Mix(body.Darken(0.42f), shade)
                                : body.Lighten(0.22f).Mix(body.Darken(0.12f), Clamp01((shade * 0.7f) + ((y - bt) / (bb - bt) * 0.5f)));

                            // a lit rim along the top and the left
                            float rim = -bodyD;
                            if (!front && rim < s * 0.05f)
                            {
                                float k = Clamp01(1f - (rim / (s * 0.05f)));
                                bool litRim = (y - bt) < (s * 0.1f) || (x - bl) < (s * 0.1f);
                                col = col.Mix(litRim ? Rgba.White : body.Darken(0.3f), (litRim ? 0.45f : 0.25f) * k);
                            }

                            // a soft shine on the upper left of the face
                            float shine = Length((x - (bl + (s * 0.13f))) / (s * 0.08f), (y - (bt + (s * 0.09f))) / (s * 0.04f));
                            col = col.Mix(Rgba.White, 0.5f * Clamp01(1f - shine));
                            Over(ref c, ref a, col, cover);

                            // the keyhole: a round head over a tapering slot, in a light steel ring
                            float head = Length(x - kx, y - ky) - (s * 0.05f);
                            float slotHalf = (s * 0.025f) + ((y - ky) * 0.18f);
                            float slot = Math.Max(Math.Abs(x - kx) - slotHalf, Math.Max(ky - y, y - (ky + (s * 0.12f))));
                            float hole = Math.Min(head, slot);
                            Over(ref c, ref a, steel.Lighten(0.35f), Coverage(hole - Math.Max(1.2f, s * 0.022f)));
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

                    if (a > 0f)
                    {
                        Put(pixels, size, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a padlock's picture (<see cref="Padlock"/>), the same in both builds.</summary>
        public static string PadlockKey(Rgba body, int pips, bool open) =>
            RoundLook.PadlockSlot + "/" + Hex(body) + "/" + pips.ToString(CultureInfo.InvariantCulture) + (open ? "/open" : string.Empty);

        /// <summary>
        /// The board's key (<c>board.round.key</c>) in <paramref name="width"/> × <paramref name="height"/> (the box of
        /// <see cref="RoundLook.KeyBox"/>): an ordinary key standing upright, its bow (a round ring with its hole) at the top, a
        /// collar, a shaft and a bit with two teeth to the left, its tip pointing down
        /// (<see cref="RoundLook.KeyTipShare"/> of a key length under the picture's middle), in <paramref name="metal"/> (the held
        /// pod's colour, or <see cref="RoundLook.KeyBrass"/>; the turning key's back is the same picture in
        /// <see cref="RoundLook.KeyBack"/>), volumetric as the cubes: a darker side face below right, the face lit from the upper
        /// left with a bevel and a shine down the shaft, a dark outline and a thin light halo so it reads on cubes and surface
        /// alike; <paramref name="pips"/> (0–3) white pips across the bow under its hole.
        /// </summary>
        public static byte[] UprightKey(int width, int height, Rgba metal, int pips)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float len = Math.Min(width / RoundLook.KeyBoxWidth, height / RoundLook.KeyBoxHeight);
            float cx = width / 2f;
            float cy = height / 2f;
            float line = Math.Max(1.2f, len * 0.02f);
            float halo = len * 0.05f;
            float depthX = len * 0.04f;
            float depthY = len * 0.035f;
            Rgba ink = metal.Darken(0.62f);
            Rgba side = metal.Darken(0.38f);
            float bevel = len * 0.035f;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float face = KeyShape(x, y, cx, cy, len);
                    if (face > halo + line + depthX + depthY)
                    {
                        continue;
                    }

                    // The side face: the key's shape swept down and to the right.
                    float sweep = float.MaxValue;
                    for (int k = 1; k <= 8; k++)
                    {
                        float t = k / 8f;
                        sweep = Math.Min(sweep, KeyShape(x - (depthX * t), y - (depthY * t), cx, cy, len));
                    }

                    float all = Math.Min(face, sweep);
                    if (all > halo + line)
                    {
                        continue;
                    }

                    var c = new Color(Rgba.White);
                    float a = 0f;

                    // The halo, the outline, the side face, then the face.
                    float glow = Clamp01(1f - ((all - line) / halo));
                    Over(ref c, ref a, RoundLook.KeyHalo, 0.9f * (float)Math.Sqrt(glow) * glow);
                    Over(ref c, ref a, ink, Coverage(all - line));
                    Over(ref c, ref a, side, Coverage(sweep));
                    float cover = Coverage(face);
                    if (cover > 0f)
                    {
                        // across the key, light from the upper left
                        float tone = Clamp01(0.5f + ((x - cx) / (len * 0.25f)) + ((y - cy) / (len * 3f)));
                        Rgba col = metal.Lighten(0.2f).Mix(metal.Darken(0.12f), tone);

                        // the bevel: lit where the edge faces the upper left, shaded where it faces the lower right
                        float inside = -face;
                        if (inside < bevel)
                        {
                            float gx = KeyShape(x + 1f, y, cx, cy, len) - KeyShape(x - 1f, y, cx, cy, len);
                            float gy = KeyShape(x, y + 1f, cx, cy, len) - KeyShape(x, y - 1f, cx, cy, len);
                            float gl = Math.Max(0.001f, Length(gx, gy));
                            float facing = -((gx / gl) + (gy / gl)) * 0.7071f;
                            float k = Clamp01(1f - (inside / bevel));
                            col = facing > 0f ? col.Mix(Rgba.White, 0.55f * facing * k) : col.Mix(metal.Darken(0.35f), -0.5f * facing * k);
                        }

                        // a shine down the shaft's left and on the bow's upper left
                        float u = y - cy;
                        float shaftShine = (u > -len * 0.1f && u < len * 0.42f) ? Clamp01(1f - (Math.Abs((x - cx) + (len * 0.022f)) / (len * 0.012f))) : 0f;
                        float bowShine = Length((y - cy) + (len * 0.36f), ((x - cx) + (len * 0.1f)) * 1.6f) / (len * 0.06f);
                        col = col.Mix(Rgba.White, Math.Max(0.55f * shaftShine, 0.6f * Clamp01(1f - bowShine)));
                        Over(ref c, ref a, col, cover);

                        if (pips > 0)
                        {
                            // Across the bow's ring under its hole, a row of white pips in a dark ring.
                            float pr = len * 0.034f;
                            float pu = cy - (len * 0.155f);
                            for (int k = 0; k < pips; k++)
                            {
                                float pcx = cx + ((k - ((pips - 1) / 2f)) * pr * 2.5f);
                                float d = Length(x - pcx, y - pu);
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

        /// <summary>The cache key of an upright key's picture (<see cref="UprightKey"/>), the same in both builds.</summary>
        public static string UprightKeyKey(Rgba metal, int pips) =>
            RoundLook.KeySlot + "/up/" + Hex(metal) + "/" + pips.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// A soft oval contact shadow in <paramref name="width"/> × <paramref name="height"/> (the key's on its cell, the hero's
        /// under his feet): black, <see cref="RoundLook.ShadowAlpha"/> at its middle, fading smoothly to nothing at its rim.
        /// </summary>
        public static byte[] SoftOval(int width, int height)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float rx = width / 2f;
            float ry = height / 2f;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = Length((px + 0.5f - rx) / rx, (py + 0.5f - ry) / ry);
                    float k = Clamp01((d - 0.3f) / 0.7f);
                    float alpha = RoundLook.ShadowAlpha * (1f - (k * k * (3f - (2f * k))));
                    if (alpha > 0f)
                    {
                        Put(pixels, width, px, py, new Color(Rgba.Black), alpha);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of the soft oval shadow (<see cref="SoftOval"/>).</summary>
        public const string SoftOvalKey = RoundLook.KeySlot + "/shadow";

        private static string Hex(Rgba c) =>
            c.R.ToString("X2", CultureInfo.InvariantCulture) + c.G.ToString("X2", CultureInfo.InvariantCulture) + c.B.ToString("X2", CultureInfo.InvariantCulture);

        // The upright key's signed distance (pixels) at (x, y): its length len along the vertical axis through (cx, cy), the
        // middle of the key at (cx, cy), the bow up. Bow (a ring), collar, shaft and a bit with two teeth to the left near the tip.
        private static float KeyShape(float x, float y, float cx, float cy, float len)
        {
            float u = (y - cy) / len;
            float v = -(x - cx) / len;
            const float BowCx = -0.29f;
            float ring = Math.Max(Length(u - BowCx, v) - 0.21f, 0.08f - Length(u - (BowCx - 0.015f), v + 0.01f));
            float collar = RoundRect(u, v, -0.12f, -0.085f, -0.05f, 0.085f, 0.03f);
            float shaft = RoundRect(u, v, -0.12f, -0.058f, 0.48f, 0.058f, 0.05f);
            float bit = RoundRect(u, v, 0.2f, -0.01f, 0.46f, 0.22f, 0.03f);
            float notch = RoundRect(u, v, 0.285f, 0.11f, 0.355f, 0.32f, 0.018f);
            float bitCut = Math.Max(bit, -notch);
            return Math.Min(Math.Min(ring, collar), Math.Min(shaft, bitCut)) * len;
        }

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
    }
}
