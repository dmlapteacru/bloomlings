using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The latch and its acorn key (rules 17 §8 «Засов и жёлудь-ключ», the owner, 2026-11-01; spec 006 FR-068,
    /// contracts/presentation.md): engine-free flat 2D pictures in the round pods' oblique look, lit from the upper left,
    /// straight alpha, deterministic. Each latch has its mark (<see cref="RoundLook.LatchTag"/> colour and
    /// <see cref="RoundLook.LatchPips"/> white pips), the same on its block and on its key, so a level with several latches
    /// pairs each key with its latch.
    /// </summary>
    public static partial class UiRaster
    {
        /// <summary>
        /// A latch block (<c>pod.round.latch</c>): a pod-sized oblique piece in a square of side <paramref name="size"/>, the
        /// round pods' cube (<see cref="PodCube"/>'s geometry, its full base) in warm wood with plank grooves along its top,
        /// a dark iron bolt across it and an acorn padlock in the middle, and the latch's mark (a disc of its tag colour with
        /// its white pips) at the top's lower right. It never takes a tap, so it has no white outline.
        /// </summary>
        public static byte[] LatchBlock(int size, int tag)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float m = s * RoundScreenLook.PodRingShare;
            float front = RoundScreenLook.CubeDepthShare;
            float bevel = s * RoundScreenLook.BoardBevelShare * 0.8f;
            float topBottom = (s - m) - ((s - (2f * m)) * front);
            float topH = topBottom - m;
            float cx = s / 2f;
            float cy = (m + topBottom) / 2f;
            Rgba wood = RoundLook.LatchWood;
            Rgba groove = wood.Darken(0.28f);
            Rgba iron = RoundLook.LatchIron;
            float plank = (s - (2f * m)) / 4f;
            float boltH = topH * 0.16f;
            float acornUnit = Math.Min(s * 0.42f, topH * 0.9f) / 2f;
            float discR = s * 0.13f;
            float discX = s - m - (s * 0.2f);
            float discY = topBottom - (topH * 0.24f);
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    (Color c, float cover, float tb) = ObliqueCube(x, y, s, s, wood, front, m, m, bevel, 0.12f);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    bool onTop = y < tb - (bevel * 0.5f);
                    if (onTop)
                    {
                        // Plank grooves along the top (four planks), a soft dark line each.
                        float gx = (x - m) / plank;
                        float dist = Math.Abs(gx - (float)Math.Round(gx)) * plank;
                        if (gx > 0.5f && gx < 3.5f)
                        {
                            c.Mix(groove, 0.55f * Clamp01(1f - (dist / Math.Max(0.8f, s * 0.012f))));
                        }

                        // The iron bolt across the middle, rounded, lit along its top.
                        float bolt = RoundRect(x, y, m + (s * 0.08f), cy - (boltH / 2f), s - m - (s * 0.08f), cy + (boltH / 2f), boltH / 2f);
                        float boltCover = Coverage(bolt);
                        if (boltCover > 0f)
                        {
                            float t = Clamp01((y - (cy - (boltH / 2f))) / Math.Max(1f, boltH));
                            c.Mix(iron.Lighten(0.25f).Mix(iron.Darken(0.25f), t), boltCover);
                        }
                    }
                    else
                    {
                        // The front: two darker bands where the bolt's brackets hold it.
                        float band = Math.Min(Math.Abs(x - (m + (s * 0.2f))), Math.Abs(x - (s - m - (s * 0.2f))));
                        c.Mix(iron.Darken(0.1f), 0.7f * Clamp01(1f - (band / Math.Max(1f, s * 0.035f))));
                    }

                    if (onTop)
                    {
                        Acorn(ref c, x, y, cx, cy - (topH * 0.02f), acornUnit, s * 0.018f);
                        Mark(ref c, x, y, discX, discY, discR, tag);
                    }

                    Put(pixels, size, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a latch block's picture (<see cref="LatchBlock"/>), the same in both builds.</summary>
        public static string LatchBlockKey(int tag) => RoundLook.LatchSlot + "/" + RoundLook.TagIndex(tag).ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// The acorn key (<c>board.round.key</c>) in a square of side <paramref name="size"/>: an acorn standing on the board's
        /// empty surface (a nut under a darker scaled cap with its stem, outlined, a light spot on the nut, a soft shadow under
        /// it), with its latch's mark (the same disc and pips as <see cref="LatchBlock"/>) at its lower right.
        /// </summary>
        public static byte[] AcornKey(int size, int tag)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float unit = s * 0.36f;
            float cx = s * 0.46f;
            float cy = s * 0.47f;
            float discR = s * 0.15f;
            float discX = s * 0.76f;
            float discY = s * 0.76f;
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;

                    // A soft shadow on the surface under the nut.
                    float sh = Length((x - cx) / (s * 0.3f), (y - (s * 0.86f)) / (s * 0.07f));
                    float shadow = 0.28f * Clamp01(1.2f - sh);
                    var c = new Color(Rgba.FromHex("#3B2A18"));
                    float alpha = shadow;

                    float u = (x - cx) / unit;
                    float v = -(y - cy) / unit;
                    float solid = ShapeLibrary.AcornSolid(u, v) * unit;
                    float outline = Coverage(solid - Math.Max(1f, s * 0.035f));
                    if (outline > 0f)
                    {
                        Color body = c;
                        Acorn(ref body, x, y, cx, cy, unit, s * 0.035f);
                        c = body;
                        alpha = alpha + ((1f - alpha) * outline);
                    }

                    float disc = Length(x - discX, y - discY) - discR - Math.Max(1f, s * 0.025f);
                    float discCover = Coverage(disc);
                    if (discCover > 0f)
                    {
                        Mark(ref c, x, y, discX, discY, discR, tag);
                        alpha = alpha + ((1f - alpha) * discCover);
                    }

                    if (alpha > 0f)
                    {
                        Put(pixels, size, px, py, c, alpha);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of an acorn key's picture (<see cref="AcornKey"/>), the same in both builds.</summary>
        public static string AcornKeyKey(int tag) => RoundLook.KeySlot + "/" + RoundLook.TagIndex(tag).ToString(System.Globalization.CultureInfo.InvariantCulture);

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

        // A latch's mark: a disc of its tag colour with a white rim and its white pips (1–3) in a row.
        private static void Mark(ref Color c, float x, float y, float cx, float cy, float r, int tag)
        {
            float d = Length(x - cx, y - cy);
            if (d > r * 1.4f)
            {
                return;
            }

            float rim = Math.Max(1f, r * 0.2f);
            c.Mix(Rgba.White, Coverage(d - r - rim));
            Rgba col = RoundLook.LatchTag(tag);
            c.Mix(col, Coverage(d - r));
            int pips = RoundLook.LatchPips(tag);
            float pr = r * (pips == 1 ? 0.34f : pips == 2 ? 0.27f : 0.22f);
            float step = pr * 2.3f;
            for (int k = 0; k < pips; k++)
            {
                float px = cx + ((k - ((pips - 1) / 2f)) * step);
                c.Mix(Rgba.White, Coverage(Length(x - px, y - cy) - pr));
            }
        }
    }
}
