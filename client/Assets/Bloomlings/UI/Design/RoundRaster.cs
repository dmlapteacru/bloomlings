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
        /// A round level's cleared cell (<c>tile.ground</c>, spec 006 T049), as the game's restored cells: the finished
        /// picture as a pale flat cell in <paramref name="color"/> (the material's colour lightened), a slightly deeper rim
        /// round it and a faint shadow along its top, filling <paramref name="width"/> × <paramref name="height"/>. One
        /// cached picture a material, so a cleared board costs one picture a cell.
        /// </summary>
        public static byte[] ClearedCell(int width, int height, Rgba color)
        {
            Check(width, height);
            float s = Math.Min(width, height);
            float inset = s * 0.025f;
            float r = (s - (2f * inset)) * 0.1f;
            Rgba rim = color.Darken(0.12f);
            var pixels = new byte[width * height * 4];
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    var c = new Color(rim);
                    float face = Coverage(RoundRect(x, y, inset, inset, width - inset, height - inset, r));
                    if (face > 0f)
                    {
                        var g = new Color(color);
                        g.Mix(C.GardenShadow, 0.12f * Clamp01(1f - ((y - inset) / (height * 0.24f))));
                        c.Mix(g.ToRgba(), face);
                    }

                    Put(pixels, width, px, py, c, 1f);
                }
            }

            return pixels;
        }
    }
}
