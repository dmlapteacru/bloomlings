using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- Round levels (spec 006 contracts/presentation.md): the stream's water and the link tube ----

        /// <summary>How many water pictures the stream cycles through along its run (<see cref="WaterSeed"/>).</summary>
        public const int WaterVariants = 4;

        /// <summary>The water picture of a stream cell at column <paramref name="x"/>, so a run does not repeat one tile.</summary>
        public static int WaterSeed(int x) => ((x % WaterVariants) + WaterVariants) % WaterVariants;

        /// <summary>
        /// One cell of a stream inside a round level's frame (<c>board.water</c>): water filling the whole square, so cells in a
        /// row join into one stream, lighter near its banks (<c>water.top</c>) and deeper in its middle (<c>water.deep</c>), a
        /// soft dark band under each bank, short <c>water.foam</c> ripples drifting across it (their phase from
        /// <paramref name="seed"/>, <see cref="WaterSeed"/>) and a thin foam line along both banks.
        /// </summary>
        public static byte[] Water(int size, int seed)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float phase = seed * 0.37f;
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                float v = y / s;

                // Across the stream: light by the banks, deep in the middle.
                float middle = 1f - (Math.Abs(v - 0.5f) * 2f);
                var row = new Color(C.WaterTop.Mix(C.WaterDeep, Smooth(Clamp01(middle * 1.3f))));
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    float u = x / s;
                    Color c = row;

                    // The banks' shade and their foam lines.
                    c.Mix(C.WaterDeep.Darken(0.35f), 0.35f * Clamp01(1f - (v / 0.14f)));
                    c.Mix(C.WaterFoam, 0.7f * Clamp01(1f - (Math.Abs(v - 0.08f) / 0.035f)));
                    c.Mix(C.WaterFoam, 0.55f * Clamp01(1f - (Math.Abs(v - 0.94f) / 0.03f)));

                    // Two rows of short ripples, offset by the seed.
                    for (int k = 0; k < 2; k++)
                    {
                        float ry = 0.36f + (k * 0.28f);
                        float rx = (u * 2f) + phase + (k * 0.5f);
                        float wave = rx - (float)Math.Floor(rx);
                        float dash = Math.Abs(wave - 0.5f) * 2f;
                        float line = Clamp01(1f - (Math.Abs(v - ry - (0.03f * (float)Math.Sin((u + phase) * Math.PI * 2f))) / 0.028f));
                        c.Mix(C.WaterFoam, 0.55f * line * Clamp01((0.6f - dash) / 0.2f));
                    }

                    Put(pixels, size, px, py, c, 1f);
                }
            }

            return pixels;
        }

        /// <summary>How many wall pictures a wall laid over a picture cycles through (<see cref="WallSeed"/>).</summary>
        public const int WallVariants = 4;

        /// <summary>The wall picture of a cell at (<paramref name="x"/>, <paramref name="y"/>), so a wall does not repeat one tile.</summary>
        public static int WallSeed(int x, int y) => ((((x * 3) + (y * 5)) % WallVariants) + WallVariants) % WallVariants;

        /// <summary>
        /// One cell of a wall laid over a round level's picture (<c>board.wall</c>, 17 §8 as amended on 2026-10-28): a dry
        /// stone wall filling the whole square, so cells join into one wall whichever way it runs. Two courses of blocks, the
        /// lower one offset by a quarter cell, so the joints line up across neighbouring cells; each block a rounded slab from
        /// <c>stone.top</c> to <c>stone.face</c> (its tone nudged per block by <paramref name="seed"/>), lit along its top,
        /// a <c>stone.lip</c> band along its foot, in deep mortar joints of <c>stone.line</c>.
        /// </summary>
        public static byte[] WallStone(int size, int seed)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float half = s / 2f;
            float gap = Math.Max(1f, s * 0.05f);
            float radius = s * 0.09f;
            float bevel = Math.Max(1f, s * 0.04f);
            float lip = s * 0.08f;
            Rgba mortar = C.StoneLine.Mix(C.StoneLip, 0.35f);
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                int course = y < half ? 0 : 1;
                float offset = course == 0 ? 0f : s * 0.25f;
                float top = course * half;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    int block = (int)Math.Floor((x + offset) / half);
                    float left = (block * half) - offset;
                    float l = left + (gap / 2f);
                    float t = top + (gap / 2f);
                    float r = left + half - (gap / 2f);
                    float b = top + half - (gap / 2f);
                    float d = RoundRect(x, y, l, t, r, b, radius);
                    var c = new Color(mortar);
                    float cover = Coverage(d);
                    if (cover > 0f)
                    {
                        float tone = 0.95f + (0.09f * Hash(block + (course * 7), course, seed + 41));
                        var face = new Color(C.StoneTop.Mix(C.StoneFace, Smooth(Clamp01((y - t) / (b - t)))));
                        face.Scale(tone);
                        face.Mix(C.StoneLip, Outside(RoundRect(x, y + lip, l, t, r, b, radius), 1.5f));
                        face.Mix(C.StoneTop.Lighten(0.5f), 0.7f * Outside(RoundRect(x, y - bevel, l, t, r, b, radius), 1f));
                        c.Mix(face.ToRgba(), cover);
                    }

                    Put(pixels, size, px, py, c, 1f);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The link tube between two linked pods (<c>pod.link.tube</c>, spec 006 FR-043) as a horizontal
        /// picture <paramref name="width"/> long and <paramref name="height"/> thick, drawn rotated between the pods' front
        /// faces by the kit's <c>LinkTube</c> geometry: a round, glossy hose (lit from the top: a lighter upper side, a white streak, a
        /// deeper underside and a thin dark outline), its left half in <paramref name="left"/> and its right half in
        /// <paramref name="right"/>, a cream coupling ring where the halves meet, and at each end a collar a little wider than
        /// the hose in its own half's colour, where it plugs into the pod.
        /// </summary>
        public static byte[] LinkTube(int width, int height, Rgba left, Rgba right)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float hose = h * 0.36f;
            float collar = h * 0.48f;
            float collarLength = Math.Min(w * 0.18f, h * 0.7f);
            float ring = Math.Min(w * 0.12f, h * 0.55f);
            float line = Math.Max(0.8f, h * 0.06f);
            float cy = h / 2f;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    bool leftHalf = x < w / 2f;
                    Rgba color = leftHalf ? left : right;

                    // The hose, the collars at its ends and the coupling ring in its middle.
                    float body = RoundRect(x, y, 0f, cy - hose, w, cy + hose, hose);
                    float ends = Math.Min(
                        RoundRect(x, y, 0f, cy - collar, collarLength, cy + collar, collar * 0.5f),
                        RoundRect(x, y, w - collarLength, cy - collar, w, cy + collar, collar * 0.5f));
                    float coupling = RoundRect(x, y, (w - ring) / 2f, cy - collar, (w + ring) / 2f, cy + collar, collar * 0.4f);
                    float shape = Math.Min(body, Math.Min(ends, coupling));
                    float cover = Coverage(shape);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    bool isCoupling = coupling <= Math.Min(body, ends) + 0.5f && coupling < 0.5f;
                    bool isCollar = !isCoupling && ends < 0.5f && ends <= body + 0.5f;
                    float half = isCoupling || isCollar ? collar : hose;
                    Rgba face = isCoupling ? C.CreamFace : isCollar ? color.Darken(0.12f) : color;

                    // Round across: lighter on top, deeper underneath, a white streak along the upper third.
                    float across = Clamp((y - cy) / half, -1f, 1f);
                    var c = new Color(face.Lighten(0.22f).Mix(face.Darken(0.3f), Smooth((across + 1f) / 2f)));
                    c.Mix(Rgba.White, 0.55f * Clamp01(1f - (Math.Abs(across + 0.45f) / 0.18f)));
                    c.Mix(face.Darken(0.55f), Clamp01(0.5f - (-shape - line)));
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A round level's board surface (<c>board.surface</c>, the owner's board of 2026-10-28, "like Colony Flow"): one
        /// solid neutral panel filling <paramref name="width"/> × <paramref name="height"/> inside the stone frame, never a
        /// grid of cells. Flat <c>board.surface</c>, set a little into the frame: a soft <c>board.surface_shade</c> along its
        /// top (4.5% of its shorter side) and left edge (2.5%), the light coming from the upper left, and a faint lighter lip
        /// along its bottom; its corners rounded by <paramref name="radiusShare"/> of its shorter side. Opaque inside its
        /// corners. Hosts cache it by size: one picture a screen.
        /// </summary>
        public static byte[] BoardSurface(int width, int height, float radiusShare)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float s = Math.Min(width, height);
            float radius = Math.Max(0f, s * radiusShare);
            float top = Math.Max(1f, s * 0.045f);
            float side = Math.Max(1f, s * 0.025f);
            Rgba lip = C.BoardSurface.Lighten(0.35f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                float shadeTop = Clamp01(1f - (y / top));
                float lipBottom = Clamp01(1f - ((height - y) / side));
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float cover = Coverage(RoundRect(x, y, 0f, 0f, width, height, radius));
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float shadeLeft = Clamp01(1f - (x / side));
                    var c = new Color(C.BoardSurface);
                    c.Mix(C.BoardSurfaceShade, Clamp01((0.6f * shadeTop * shadeTop) + (0.35f * shadeLeft * shadeLeft)));
                    c.Mix(lip, 0.3f * lipBottom * lipBottom);
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }
    }
}
