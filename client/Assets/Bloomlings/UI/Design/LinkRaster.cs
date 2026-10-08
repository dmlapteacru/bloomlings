using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        /// <summary>The link badge's disc, its outline included, as a share of its picture's side (a soft shadow takes the rest).</summary>
        public const float LinkBadgeDisc = 0.92f;

        /// <summary>
        /// The link badge of a connected pod (<c>pod.link</c>, spec 005 FR-043, contracts/look.md §6.17) in a square picture of
        /// side <paramref name="size"/>: the count badge's disc (§3.4: a thin <c>garden.shadow</c> outline, a white ring of a
        /// sixth of its radius, the face lit from the top) in the group's <paramref name="color"/> instead of green, over a
        /// soft shadow, and on it a white chain of two links along the rising diagonal, each a stadium outline with a thin
        /// dark edge, woven through each other (the lower link passes over the upper one on the left of the chain and under
        /// it on the right).
        /// </summary>
        public static byte[] LinkBadge(int size, Rgba color)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float c0 = s / 2f;
            float outer = s * LinkBadgeDisc / 2f;
            float line = Math.Max(1f, outer * 0.05f);
            float face = outer * 0.79f;
            Rgba edge = color.Darken(0.45f);

            // The chain, in shares of the face's radius: two links along the rising diagonal (up and to the right).
            const float axisX = 0.70710677f;
            const float axisY = -0.70710677f;
            float offset = face * 0.25f;
            float half = face * 0.2f;
            float loop = face * 0.2f;
            float stroke = Math.Max(1.2f, face * 0.12f);
            float rim = Math.Max(0.8f, face * 0.04f);
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
                    float shadow = Length(dx, dy - (s * 0.03f)) - outer;
                    if (shadow < s * 0.04f)
                    {
                        paint.Over(0.3f * (1f - Smooth(Clamp01((shadow + (s * 0.01f)) / (s * 0.05f)))), C.GardenShadow);
                    }

                    // The disc: its outline, the white ring, the face lit from the top.
                    paint.Over(Coverage(dist - outer), C.GardenShadow.WithAlpha(0.8f));
                    paint.Over(Coverage(dist - (outer - line)), Rgba.White);
                    float lit = Clamp01((dy + face) / (2f * face));
                    paint.Over(Coverage(dist - face), color.Lighten(0.16f).Mix(color, lit));
                    paint.Over(0.35f * Coverage(Length(dx, dy + (face * 0.55f)) - (face * 0.5f)) * Coverage(dist - face), Rgba.White.WithAlpha(0.5f));

                    if (dist < face)
                    {
                        // Along the chain (u) and across it (v).
                        float u = (dx * axisX) + (dy * axisY);
                        float v = (dx * -axisY) + (dy * axisX);
                        float a = Math.Abs(Segment(u, v, -offset - half, 0f, -offset + half, 0f) - loop) - (stroke / 2f);
                        float b = Math.Abs(Segment(u, v, offset - half, 0f, offset + half, 0f) - loop) - (stroke / 2f);
                        Link(ref paint, a, rim, edge);
                        Link(ref paint, b, rim, edge);

                        // Woven: on one side of the chain the lower link lies over the upper one.
                        if (v < 0f && Math.Abs(u) < loop + half)
                        {
                            Link(ref paint, a, rim, edge);
                        }
                    }

                    paint.Write(pixels, ((py * size) + px) * 4);
                }
            }

            return pixels;
        }

        /// <summary>One link of the chain at outline distance <paramref name="d"/>: a white stroke in a thin dark <paramref name="edge"/>.</summary>
        private static void Link(ref NavPaint paint, float d, float rim, Rgba edge)
        {
            paint.Over(Coverage(d - rim), edge);
            paint.Over(Coverage(d), Rgba.White);
        }
    }
}
