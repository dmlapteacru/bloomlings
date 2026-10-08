using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- The laminate signs and the popup's frame (spec 005 FR-045, the owner, 2026-10-08; contracts/look.md §6.19) ----

        /// <summary>A wooden sign's front side under its top, as a share of its height.</summary>
        public const float SignSide = 0.07f;

        /// <summary>A popup card's wooden frame, as a share of its shorter side's reference units (<see cref="CardFrameBorder"/>).</summary>
        public const float CardFrameUnits = 26f;

        /// <summary>
        /// A wooden sign's plank (the owner, 2026-10-08: the popup's title plank "as on every page, only in the color we made
        /// and the texture of the buttons' rim"; every sign: the pages' banners, the level label, Home's plaque, the win's
        /// sign), filling <paramref name="width"/> × <paramref name="height"/> with corners of <paramref name="radius"/> px:
        /// the buttons' plate laminate (<see cref="RaisedPlate"/>) as a slab, its top's edge rounding over 14% of its height,
        /// lit from the upper left and catching the light along its top, over its front side of <see cref="SignSide"/> of the
        /// height, two nails (near the top at the left end, near the bottom at the right one) when taller than 48 px, in the
        /// plate's deeper outline, colored as the plate (<see cref="PlateDepth"/>, <see cref="PlateVivid"/>).
        /// </summary>
        public static byte[] LaminateSign(int width, int height, float radius)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float r = Math.Min(radius, Math.Min(w, h) / 2f);
            float side = h * SignSide;
            float topBottom = h - side;
            float bevel = Math.Max(1.5f, h * 0.14f);
            float line = Math.Max(1f, h * 0.02f);
            float nail = Math.Max(1.4f, h * 0.035f);
            float nailInset = Math.Max(r * 0.7f, h * 0.3f);
            Rows(width, height, py =>
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float body = RoundRect(x, y, 0f, 0f, w, h, r);
                    float cover = Coverage(body);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    Color c = Laminate(x, y, h);
                    float top = RoundRect(x, y, 0f, 0f, w, topBottom, r);
                    if (top < 0.5f)
                    {
                        float tilt = 1f - Clamp01(-top / bevel);
                        if (tilt > 0f)
                        {
                            // Only the bevel is lit: on the flat top the light changes nothing (Shade of a flat normal).
                            (float gx, float gy) = RoundRectGradient(x, y, 0f, 0f, w, topBottom, r);
                            (float nx, float ny, float nz) = BevelNormal(gx, gy, tilt);
                            Shade(ref c, C.WoodGrain, nx, ny, nz, 0.45f, 0.45f, 0.18f);
                        }

                        c.Mix(C.WoodMid.Lighten(0.12f), 0.45f * Clamp01(1f - (y / (topBottom * 0.35f))));
                        c.Mix(C.WoodEdge.Darken(0.12f), Clamp01(top + 0.5f) * 0.5f);

                        // The nails: near the top at the left end, near the bottom at the right one.
                        bool left = x < w / 2f;
                        float cx = left ? nailInset : w - nailInset;
                        float cy = topBottom * (left ? 0.3f : 0.7f);
                        if (height > 48 && Math.Abs(x - cx) < nail + 2f && Math.Abs(y - cy) < nail + 2f)
                        {
                            c.Mix(C.WoodLine.Lighten(0.1f), Coverage(Length(x - cx, y - cy) - nail));
                            c.Mix(C.WoodLight, 0.6f * Coverage(Length(x - cx + (nail * 0.35f), y - cy + (nail * 0.35f)) - (nail * 0.35f)));
                        }
                    }
                    else
                    {
                        float t = Clamp01((y - (topBottom - r)) / Math.Max(1f, h - (topBottom - r)));
                        c.Mix(C.WoodEdge.Darken(0.06f), 0.45f + (0.25f * t));
                    }

                    Finish(ref c, y / h, body, line);
                    Put(pixels, width, px, py, c, cover);
                }
            });

            return pixels;
        }

        /// <summary>A popup card's frame width in pixels for a card <paramref name="width"/> px wide (<see cref="CardFrameUnits"/> of the reference's 920-unit card).</summary>
        public static float CardFrameBorder(float width) => Math.Max(3f, width * CardFrameUnits / 920f);

        /// <summary>
        /// A popup card (spec 005 FR-045; the owner, 2026-10-08: "a rectangle with rounded corners"), filling
        /// <paramref name="width"/> × <paramref name="height"/> with corners of <paramref name="radius"/> px: a wooden frame
        /// of <see cref="CardFrameBorder"/> in the buttons' plate laminate (the grain along each member), its border rounding
        /// over at the outside and down into the panel, lit from the upper left, over a front side of a third of its width
        /// along the bottom, in the plate's outline; inside it a cream panel (<c>cream.top</c> to <c>cream.face</c> lightened)
        /// with a soft shadow under the frame's inner edge, deeper along the top.
        /// </summary>
        public static byte[] CardFrame(int width, int height, float radius)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float border = CardFrameBorder(w);
            float side = border * 0.3f;
            float topBottom = h - side;
            float r = Math.Min(radius, Math.Min(w, h) / 2f);
            float innerRadius = Math.Max(0f, r - border);
            float line = Math.Max(1f, border * 0.08f);
            float grain = border * 9f;
            Rgba panelTop = C.CreamTop;
            Rgba panelBottom = C.CreamFace.Mix(C.CreamTop, 0.45f);
            Rgba panelShade = C.WoodLine.Darken(0.2f);

            // The panel's open middle, where the frame's shadow (Math.Max(1, 0.7 border) deep) no longer reaches: there every
            // pixel is its row's panel color, fully covered. The hole's box as RoundRect measures it, and how far inside it
            // that middle starts (two pixels more, so rounding never decides it); a hole too small for its corners (its
            // radius cut to its half size, a tiny card) has none.
            float holeCx = (border + (w - border)) / 2f;
            float holeHx = ((w - border) - border) / 2f;
            float holeCy = (border + (topBottom - border)) / 2f;
            float holeHy = ((topBottom - border) - border) / 2f;
            float holeRadius = Math.Min(innerRadius, Math.Min(holeHx, holeHy));
            float open = Math.Max(1f, border * 0.7f) + 2f;
            Rows(width, height, py =>
            {
                float y = py + 0.5f;

                // The panel's color and the strength of the frame's shadow on it change only down the card.
                Rgba panelRow = panelTop.Mix(panelBottom, Clamp01((y - border) / Math.Max(1f, topBottom - (2f * border))));
                float topness = Clamp01(1f - ((y - border) / Math.Max(1f, border * 2.5f)));
                float shadowRow = 0.08f + (0.14f * topness);

                // This row's open middle, if it has one (away from the rounded corners, as the hole's straight sides go).
                int openFrom = width;
                int openTo = -1;
                float dy = Math.Abs(y - holeCy);
                if (holeRadius == innerRadius && dy <= holeHy - open)
                {
                    float half = dy <= holeHy - holeRadius ? holeHx - open : holeHx - Math.Max(open, holeRadius);
                    if (half > 0f)
                    {
                        openFrom = Math.Max(0, (int)Math.Ceiling(holeCx - half - 0.5f) + 1);
                        openTo = Math.Min(width - 1, (int)Math.Floor(holeCx + half - 0.5f) - 1);
                    }
                }

                var open4 = new Color(panelRow);
                byte openR = Byte(open4.R);
                byte openG = Byte(open4.G);
                byte openB = Byte(open4.B);
                for (int px = 0; px < width; px++)
                {
                    if (px >= openFrom && px <= openTo)
                    {
                        // As the panel below: its row's color, no shadow (its edge is 0 there), fully covered.
                        int i = ((py * width) + px) * 4;
                        pixels[i] = openR;
                        pixels[i + 1] = openG;
                        pixels[i + 2] = openB;
                        pixels[i + 3] = 255;
                        continue;
                    }

                    float x = px + 0.5f;
                    float body = RoundRect(x, y, 0f, 0f, w, h, r);
                    float cover = Coverage(body);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float hole = RoundRect(x, y, border, border, w - border, topBottom - border, innerRadius);
                    if (hole < -0.5f)
                    {
                        // The panel, with the frame's soft shadow along its edge, deeper at the top.
                        var panel = new Color(panelRow);
                        float edge = 1f - Smooth(Clamp01(-hole / Math.Max(1f, border * 0.7f)));
                        panel.Mix(panelShade, shadowRow * edge);
                        Put(pixels, width, px, py, panel, cover);
                        continue;
                    }

                    // The members' grain runs along them: across on the top and bottom, down on the sides.
                    bool upright = Math.Min(x, w - x) < Math.Min(y, h - y);
                    Color c = upright ? Laminate(y + (w * 3f), x, grain) : Laminate(x, y, grain);
                    float top = RoundRect(x, y, 0f, 0f, w, topBottom, r);
                    if (top < 0.5f)
                    {
                        // Across the border: rounding over at the outside, flat on its middle, rounding down into the panel.
                        float q = Clamp01(-top / Math.Max(1f, border));
                        float f = q < 0.4f ? (float)Math.Cos(q / 0.4f * Math.PI / 2f) : q > 0.7f ? -(float)Math.Cos((1f - Math.Min(1f, q)) / 0.3f * Math.PI / 2f) : 0f;
                        if (f != 0f)
                        {
                            // On the flat middle the light changes nothing (Shade of a flat normal).
                            (float gx, float gy) = RoundRectGradient(x, y, 0f, 0f, w, topBottom, r);
                            (float nx, float ny, float nz) = (gx * f, gy * f, (float)Math.Sqrt(Math.Max(0f, 1f - (f * f))));
                            Shade(ref c, C.WoodGrain, nx, ny, nz, 0.45f, 0.45f, 0.18f);
                        }

                        c.Mix(C.WoodEdge.Darken(0.12f), Clamp01(top + 0.5f) * 0.5f);
                    }
                    else
                    {
                        c.Mix(C.WoodEdge.Darken(0.06f), 0.6f);
                    }

                    // A thin line where the frame meets the panel.
                    c.Mix(C.WoodLine, 0.4f * Clamp01(1f - (Math.Abs(hole) / Math.Max(1f, line))));
                    Finish(ref c, y / h, body, line);
                    Put(pixels, width, px, py, c, cover);
                }
            });

            return pixels;
        }

        /// <summary>The plate's finish on any laminate (<see cref="RaisedPlate"/>): deeper toward <c>wood.grain</c> lower down, the outline, the saturation.</summary>
        private static void Finish(ref Color c, float down, float body, float line)
        {
            c.Mix(C.WoodGrain, PlateDepth * (0.45f + (0.45f * Clamp01(down - 0.4f))));
            c.Mix(C.WoodLine, (0.55f + (0.35f * PlateDepth)) * Clamp01(0.5f - (-body - line)));
            c = new Color(Vivid(c.ToRgba(), PlateVivid));
        }
    }
}
