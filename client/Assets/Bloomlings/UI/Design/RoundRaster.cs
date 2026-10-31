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
        public static byte[] Water(int size, int seed) => Water(size, size, seed);

        /// <summary><see cref="Water(int, int)"/> filling a cell <paramref name="width"/> × <paramref name="height"/> (the oblique rows' cells are wider than tall).</summary>
        public static byte[] Water(int width, int height, int seed)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float s = width;
            float sh = height;
            float phase = seed * 0.37f;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                float v = y / sh;

                // Across the stream: light by the banks, deep in the middle.
                float middle = 1f - (Math.Abs(v - 0.5f) * 2f);
                var row = new Color(C.WaterTop.Mix(C.WaterDeep, Smooth(Clamp01(middle * 1.3f))));
                for (int px = 0; px < width; px++)
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

                    Put(pixels, width, px, py, c, 1f);
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
        public static byte[] WallStone(int size, int seed) => WallStone(size, size, seed);

        /// <summary><see cref="WallStone(int, int)"/> filling a cell <paramref name="width"/> × <paramref name="height"/> (the oblique rows' cells are wider than tall).</summary>
        public static byte[] WallStone(int width, int height, int seed)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float s = Math.Min(width, height);
            float half = width / 2f;
            float halfY = height / 2f;
            float gap = Math.Max(1f, s * 0.05f);
            float radius = s * 0.09f;
            float bevel = Math.Max(1f, s * 0.04f);
            float lip = s * 0.08f;
            Rgba mortar = C.StoneLine.Mix(C.StoneLip, 0.35f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                int course = y < halfY ? 0 : 1;
                float offset = course == 0 ? 0f : width * 0.25f;
                float top = course * halfY;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    int block = (int)Math.Floor((x + offset) / half);
                    float left = (block * half) - offset;
                    float l = left + (gap / 2f);
                    float t = top + (gap / 2f);
                    float r = left + half - (gap / 2f);
                    float b = top + halfY - (gap / 2f);
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

                    Put(pixels, width, px, py, c, 1f);
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
        /// A round board's cube (spec 006 FR-064, the owner's "like the reference game, somehow at an angle" of 2026-10-28): one flat
        /// 2D picture <paramref name="width"/> (a column) × <paramref name="height"/> (a row's pitch and the front face below
        /// it, <see cref="RoundBoardLayout.CubeBox"/>) of the oblique cube (<see cref="ObliqueCube"/>) in the material's
        /// <paramref name="color"/>: its top <see cref="RoundScreenLook.BoardRowShare"/> of the width tall, its front
        /// <see cref="RoundScreenLook.CubeDepthShare"/>, the cubes touching but for the owner's hairline
        /// (<see cref="RoundScreenLook.BoardCubeFill"/>), and the material's symbol on its top in the symbols mode. Drawn from
        /// the far row to the near one, a nearer cube hides the front of the one behind it.
        /// </summary>
        public static byte[] BoardCube(int width, int height, Rgba color, string? symbolId)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float m = w * (1f - RoundScreenLook.BoardCubeFill) / 2f;
            float front = RoundScreenLook.BoardFrontShare / (RoundScreenLook.BoardRowShare + RoundScreenLook.BoardFrontShare);
            float bevel = w * RoundScreenLook.BoardBevelShare;
            Func<float, float, float>? mark = symbolId != null ? ShapeLibrary.Get(symbolId) : null;
            float topBottom = h - ((h - m) * front);
            float markUnit = Math.Min(w, topBottom) * MaterialSymbolShare(TileStyle.BoardColor) / 2f / ShapeRaster.Margin;
            Rgba ink = RoundLook.SymbolInk(color);
            Rgba halo = RoundLook.SymbolHalo(color);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    (Color c, float cover, float _) = ObliqueCube(x, y, w, h, color, front, m, 0f, bevel);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    if (mark != null)
                    {
                        MaterialMark(ref c, x, y, w / 2f, (m + topBottom) / 2f, markUnit, mark, ink, halo, Math.Max(0.8f, markUnit * 0.1f));
                    }

                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A cube's side face (<c>pod.cube.side</c>, constitution 1.2.0 VII, the owner's "with side faces" of 2026-10-28): a
        /// strip <paramref name="width"/> × <paramref name="height"/> beside a cube on its outer side, in a shade of the hue
        /// between its top and its front (<paramref name="topShare"/> of it beside the top face, the rest beside the front),
        /// a left side (<paramref name="left"/>) a little lighter than a right one (the light from the upper left), its outer
        /// corners rounded and a soft seam where it meets the cube. Cached by colour, side and size (a few widths a colour,
        /// <see cref="RoundLook.SideStep"/>).
        /// </summary>
        public static byte[] SideFace(int width, int height, Rgba col, bool left, float topShare)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float r = Math.Min(w * 0.6f, h * 0.12f);
            Rgba deep = Vivid(col, 1.25f);
            Rgba upper = left ? col.Darken(0.04f) : col.Darken(0.14f);
            Rgba lower = left ? deep.Darken(0.2f) : deep.Darken(0.32f);
            float split = h * Clamp01(topShare);
            float blend = Math.Max(1f, h * 0.06f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                var row = new Color(upper.Mix(lower, Smooth(Clamp01((y - split + blend) / (2f * blend)))));
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float cover = Coverage(RoundRect(x, y, left ? 0f : -r, 0f, left ? w + r : w, h, r));
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    Color c = row;
                    float inner = left ? w - x : x;
                    c.Mix(deep.Darken(0.42f), 0.35f * Clamp01(1f - (inner / Math.Max(1f, w * 0.25f))));
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a side face's picture (<see cref="SideFace"/>), by colour and side; the painters add the size.</summary>
        public static string SideFaceKey(char code, bool left, float topShare, bool grey = false) =>
            RoundLook.SideSlot + "/" + code + "/" + (left ? "left" : "right") + "/" + ((int)Math.Round(topShare * 100f)).ToString(System.Globalization.CultureInfo.InvariantCulture) + (grey ? "/grey" : string.Empty);

        /// <summary>The cache key of a board cube's picture (<see cref="BoardCube"/>), the same in both builds.</summary>
        public static string BoardCubeKey(char code, bool symbol) => "tile.round/oblique/" + code + (symbol ? "/symbol" : string.Empty);

        /// <summary>
        /// A round level's pod (<c>pod.cube</c>, spec 006 FR-064, the owner's pods of 2026-10-28, as the reference game's): no frame or
        /// box round it, the board's oblique cube in a square of side <paramref name="size"/> in the material's
        /// <paramref name="color"/>, with a thicker base: a top-row pod (<see cref="RoundPodLook.Top"/>, the one that takes the
        /// tap) in a white outline (<see cref="RoundScreenLook.PodRingShare"/>) with a base of
        /// <see cref="RoundScreenLook.CubeDepthShare"/>; a waiting one (rows 2 and 3) a little lighter
        /// (<see cref="RoundLook.WaitingColor"/>) with a flatter base (<see cref="RoundScreenLook.WaitingDepthShare"/>); one in
        /// a place as the top row's without the outline. Its number is text the hosts centre on its top
        /// (<see cref="PodChip.Cube"/>); in the symbols mode the material's symbol <paramref name="symbolId"/> stands small in
        /// the top's upper left. A hidden pod (<paramref name="hidden"/>) is the neutral mystery cube with its white "?".
        /// </summary>
        public static byte[] PodCube(int size, Rgba color, string? symbolId, bool hidden, RoundPodLook look)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float s = size;
            float ring = s * RoundScreenLook.PodRingShare;
            float m = ring;
            Rgba col = hidden ? C.TileMystery : look == RoundPodLook.Waiting ? RoundLook.WaitingColor(color) : color;
            float front = look == RoundPodLook.Waiting ? RoundScreenLook.WaitingDepthShare : RoundScreenLook.CubeDepthShare;
            float bevel = s * RoundScreenLook.BoardBevelShare * 0.8f;
            float topBottom = (s - m) - ((s - (2f * m)) * front);
            Func<float, float, float>? mystery = hidden ? ShapeLibrary.Get("tile.mystery") : null;
            float mysteryUnit = (s * 0.5f) / 2f / ShapeRaster.Margin;
            Func<float, float, float>? mark = symbolId != null && !hidden ? ShapeLibrary.Get(symbolId) : null;
            float markUnit = s * 0.22f / 2f / ShapeRaster.Margin;
            Rgba ink = RoundLook.SymbolInk(col);
            Rgba halo = RoundLook.SymbolHalo(col);
            float radius = Math.Max(1f, s * RoundScreenLook.CubeCornerShare);
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    (Color c, float cover, float _) = ObliqueCube(x, y, s, s, col, front, m, m, bevel, 0f);
                    if (mystery != null && cover > 0f)
                    {
                        float u = (x - (s / 2f)) / mysteryUnit;
                        float v = -(y - ((m + topBottom) / 2f)) / mysteryUnit;
                        if (Math.Abs(u) < 1.3f && Math.Abs(v) < 1.3f)
                        {
                            c.Mix(Rgba.White, Coverage(mystery(u, v) * mysteryUnit));
                        }
                    }

                    if (mark != null && cover > 0f)
                    {
                        MaterialMark(ref c, x, y, m + (s * 0.2f), m + ((topBottom - m) * 0.3f), markUnit, mark, ink, halo, Math.Max(0.8f, markUnit * 0.1f));
                    }

                    if (look == RoundPodLook.Top)
                    {
                        // The white outline round the body, under it.
                        float d = RoundRect(x, y, m, m, s - m, s - m, radius);
                        float outline = Coverage(d - (ring * 0.85f)) * (1f - cover);
                        if (outline > 0f)
                        {
                            float a = cover + outline;
                            var white = new Color(Rgba.White);
                            white.Mix(c.ToRgba(), cover / Math.Max(0.001f, a));
                            Put(pixels, size, px, py, white, Math.Min(1f, a));
                            continue;
                        }
                    }

                    if (cover > 0f)
                    {
                        Put(pixels, size, px, py, c, cover);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a pod cube's picture (<see cref="PodCube"/>), the same in both builds.</summary>
        public static string PodCubeKey(char code, bool hidden, RoundPodLook look, bool symbol, bool grey = false) =>
            RoundLook.PodCubeSlot + "/" + (hidden ? "hidden" : code.ToString()) + "/" + look + (symbol && !hidden ? "/symbol" : string.Empty) + (grey ? "/grey" : string.Empty);

        /// <summary>
        /// A round level's board surface (<c>board.surface</c>, the owner's board of 2026-10-28, "like the reference game"): one
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
