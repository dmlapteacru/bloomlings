using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- The switches (spec 005 FR-048, the owner's Settings mockup of 2026-10-08; contracts/look.md §6.21) ----

        /// <summary>The knob's side as a share of the track's height: it fills the track's round end.</summary>
        public const float KnobShare = 0.98f;

        /// <summary>The mark's side in a switch's free end as a share of the track's height.</summary>
        public const float ToggleMarkShare = 0.38f;

        /// <summary>
        /// A switch's track (the owner's Settings mockup: "the toggles volumetric too") filling <paramref name="width"/> ×
        /// <paramref name="height"/> with round ends, pressed into the row (<see cref="SunkPill"/>: deeper under its upper
        /// edge, catching the light along its lower one): on, green (<see cref="GardenLook.Green"/>) with a faint lighter
        /// check in a lighter disc in its left end, where the knob is not, so the state never rests on the hue alone; off,
        /// deeper wood (<see cref="GardenLook.TanDeep"/>) with a faint leaf pressed into its right end.
        /// </summary>
        public static byte[] ToggleTrack(int width, int height, bool on)
        {
            ColorSet set = on ? GardenLook.Green : GardenLook.TanDeep;
            float h = height;
            float r = h / 2f;
            float side = h * ToggleMarkShare;
            float cx = on ? r + (h * 0.02f) : width - r - (h * 0.02f);
            float cy = r;
            Func<float, float, float> mark = ShapeLibrary.Get(on ? "ui.check" : ShapeLibrary.SymbolId("leaf"));
            float unit = side / 2f / ShapeRaster.Margin;
            Rgba disc = set.Top.Lighten(0.3f);
            Rgba ink = on ? set.Top.Lighten(0.55f) : set.Lip.Mix(set.Line, 0.3f);
            return SunkPill(width, height, set, (ref Color c, float x, float y) =>
            {
                if (on)
                {
                    // The disc: a lighter green, a little deeper toward its lower right.
                    float dd = Length(x - cx, y - cy) - (side / 2f);
                    float inDisc = Coverage(dd);
                    c.Mix(disc, 0.42f * inDisc);
                    c.Mix(set.Lip, 0.25f * inDisc * Clamp01(((x - cx) + (y - cy)) / side));
                    float d = mark((x - cx) / (unit * 1.55f), -(y - cy) / (unit * 1.55f)) * unit * 1.55f;
                    c.Mix(ink, 0.7f * Coverage(d));
                }
                else
                {
                    // The leaf pressed in: a deeper shape with a light edge under it.
                    float d = mark((x - cx) / unit, -(y - cy) / unit) * unit;
                    float below = mark((x - cx) / unit, -(y - cy - Math.Max(1f, h * 0.03f)) / unit) * unit;
                    c.Mix(set.Top.Lighten(0.35f), 0.35f * Coverage(below) * (1f - Coverage(d)));
                    c.Mix(ink, 0.55f * Coverage(d));
                }
            });
        }

        /// <summary>
        /// A switch's knob in a square picture of side <paramref name="size"/>: a round cream button (<see cref="Dome"/>,
        /// <c>cream.top</c> toward <c>cream.face</c>, a rounded edge lit from the upper left, a <c>cream.line</c> outline).
        /// Its shadow is the host's (a soft shadow a little lower).
        /// </summary>
        public static byte[] ToggleKnob(int size) => Dome(size, C.CreamTop.Mix(C.CreamFace, 0.4f), C.CreamFace, C.CreamLine);

        /// <summary>
        /// A glossy ball in a square picture of side <paramref name="size"/> (spec 005 FR-048: the check and padlock
        /// badges): <paramref name="fill"/> lit from the upper left toward <paramref name="shade"/>'s lightened color,
        /// deeper toward <paramref name="shade"/> darkened away from the light, toward <paramref name="lip"/> at its lower
        /// right rim, a white highlight, and a thin <paramref name="line"/> outline (2.5% of the side).
        /// </summary>
        public static byte[] Ball(int size, Rgba fill, Rgba shade, Rgba lip, Rgba line)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float r = size / 2f;
            float outline = Math.Max(1f, size * 0.025f);
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    float dx = (x - r) / r;
                    float dy = (y - r) / r;
                    float d = Length(x - r, y - r) - r + 0.5f;
                    float cover = Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    // The ball's normal: out of the picture at the middle, along the edge at the rim.
                    float rr = Math.Min(1f, (dx * dx) + (dy * dy));
                    float nz = (float)Math.Sqrt(Math.Max(0f, 1f - rr));
                    var c = new Color(fill);
                    Shade(ref c, shade, dx, dy, nz, 0.4f, 0.55f, 0.55f);
                    c.Mix(lip, 0.35f * Clamp01((dx + dy) * 0.6f) * (1f - nz));
                    c.Mix(line, 0.8f * Clamp01(0.5f - (-d - outline)));
                    Put(pixels, size, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A round button in a square picture of side <paramref name="size"/>: a flat <paramref name="face"/> whose edge
        /// rounds off over 30% of the radius (lit from the upper left toward <paramref name="shade"/>'s lightened color,
        /// deeper away from it) in a thin <paramref name="line"/> outline (3% of the side).
        /// </summary>
        private static byte[] Dome(int size, Rgba face, Rgba shade, Rgba line)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float r = size / 2f;
            float bevel = r * 0.3f;
            float outline = Math.Max(1f, size * 0.03f);
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    float dist = Length(x - r, y - r);
                    float body = dist - r + 0.5f;
                    float cover = Coverage(body);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float gx = dist < 1e-4f ? 0f : (x - r) / dist;
                    float gy = dist < 1e-4f ? 0f : (y - r) / dist;
                    (float nx, float ny, float nz) = BevelNormal(gx, gy, 1f - Clamp01(-body / bevel));
                    var c = new Color(face);
                    Shade(ref c, shade, nx, ny, nz, 0.45f, 0.6f, 0.3f);
                    c.Mix(line, 0.85f * Clamp01(0.5f - (-body - outline)));
                    Put(pixels, size, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A light wood groove filling <paramref name="width"/> × <paramref name="height"/> with round ends (spec 005
        /// FR-048: the Leaderboard's placeholder names and scores): <see cref="GardenLook.Tan"/> pressed in.
        /// </summary>
        public static byte[] Groove(int width, int height) => SunkPill(width, height, GardenLook.Tan, null);

        /// <summary>Paints a mark over a sunk pill's pixel at (x, y) before its outline.</summary>
        private delegate void PillMark(ref Color c, float x, float y);

        /// <summary>
        /// A pill pressed into its surface in <paramref name="set"/>: its face, a bevel round its edge turned inward (so the
        /// upper edge faces away from the light and the lower one toward it), a soft shade under its upper edge, an
        /// optional <paramref name="mark"/> and the set's lip as an outline.
        /// </summary>
        private static byte[] SunkPill(int width, int height, ColorSet set, PillMark? mark)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float radius = h / 2f;
            float bevel = Math.Max(1.5f, h * 0.16f);
            float line = Math.Max(1f, h * 0.03f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float body = RoundRect(x, y, 0f, 0f, w, h, radius);
                    float cover = Coverage(body);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    var c = new Color(set.Face.Mix(set.Top, 0.25f));
                    (float gx, float gy) = Gradient(x, y, (ax, ay) => RoundRect(ax, ay, 0f, 0f, w, h, radius));
                    (float nx, float ny, float nz) = BevelNormal(-gx, -gy, 1f - Clamp01(-body / bevel));
                    Shade(ref c, set.Face, nx, ny, nz, 0.4f, 0.5f, 0f);
                    c.Mix(C.GardenShadow, 0.18f * Clamp01(1f - (y / (h * 0.45f))) * Clamp01(1f - (-body / (h * 0.35f))));
                    mark?.Invoke(ref c, x, y);
                    c.Mix(set.Lip, 0.75f * Clamp01(0.5f - (-body - line)));
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }
    }
}
