using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;

namespace Bloomlings.Client.Art
{
    /// <summary>
    /// The round levels' pictures as cached sprites (spec 006 contracts/presentation.md; the playtest's <c>KitRound</c>),
    /// rendered from the kit's engine-free <see cref="UiRaster"/> with the same keys: material tiles without a variant
    /// icon (the material's symbol only in the symbols mode), the board's neutral surface, the stream's water, an overlay's stone wall, the round pod's
    /// cube (spec 006 FR-064) and the link tube. Each picture is cached by key and pixel size.
    /// </summary>
    public static class RoundSprites
    {
        /// <summary>
        /// A round material's tile (FR-040, FR-041, FR-047): a board cube (<see cref="RoundLook.CubeStyle"/>) or a pod's or
        /// place's tile (<see cref="RoundLook.PodStyle"/>) in the material's colour, with its symbol when
        /// <paramref name="symbolsMode"/> is on.
        /// </summary>
        public static Sprite MaterialTile(int material, TileStyle style, bool symbolsMode, int size, TileState state = TileState.Normal)
        {
            RoundMaterial m = RoundMaterial.All[material];
            bool symbol = symbolsMode && state != TileState.Mystery;
            Rgba color = RoundLook.ColorOf(m);
            string? mark = symbol ? RoundLook.SymbolOf(m) : null;
            string key = "tile.round/" + style + "/" + state + "/" + m.Code + (symbol ? "/symbol" : string.Empty);
            int s = Math.Max(1, size);
            return ProceduralSprites.Picture(key, s, s, (w, h) => UiRaster.MaterialTile(Math.Min(w, h), color, mark, style, state));
        }

        /// <summary>A stream cell (<c>board.water</c>), its picture by its column (<see cref="UiRaster.WaterSeed"/>).</summary>
        public static Sprite Water(int x, int size)
        {
            int k = UiRaster.WaterSeed(x);
            int s = Math.Max(1, size);
            return ProceduralSprites.Picture(RoundBoardLayout.WaterSlot + "/" + k, s, s, (w, h) => UiRaster.Water(Math.Min(w, h), k));
        }

        /// <summary>
        /// A cell of a wall laid over the picture (<c>board.wall</c>, 17 §8 as amended on 2026-10-28), its picture by its place
        /// (<see cref="UiRaster.WallSeed"/>).
        /// </summary>
        public static Sprite Wall(int x, int y, int size)
        {
            int k = UiRaster.WallSeed(x, y);
            int s = Math.Max(1, size);
            return ProceduralSprites.Picture(RoundBoardLayout.WallSlot + "/" + k, s, s, (w, h) => UiRaster.WallStone(Math.Min(w, h), k));
        }

        /// <summary>
        /// A round board's solid neutral surface (<c>board.surface</c>, the owner's board of 2026-10-28) at a pixel size, its
        /// corners rounded by <paramref name="radiusShare"/> of its shorter side; the same key as the playtest's <c>Kit.BoardSurface</c>.
        /// </summary>
        public static Sprite Surface(int width, int height, float radiusShare)
        {
            float share = (float)Math.Round(radiusShare, 4);
            string key = RoundBoardLayout.SurfaceSlot + "/" + share.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return ProceduralSprites.Picture(key, Math.Max(1, width), Math.Max(1, height), (w, h) => UiRaster.BoardSurface(w, h, share));
        }

        /// <summary>
        /// A round pod (<c>pod.cube</c>, spec 006 FR-064; the playtest's <c>Kit.PodCubePicture</c>): the oblique cube in its
        /// material's colour with no frame round it, in <paramref name="look"/> (the top row's white outline, a waiting pod's
        /// flatter base, a place's), its symbol in the symbols mode; -1 is the hidden pod's neutral "?" cube; greyed while a
        /// place is stuck (<see cref="UiRaster.PodCube"/>, the same key as the playtest's).
        /// </summary>
        public static Sprite PodCube(int material, RoundPodLook look, bool symbolsMode, int size, bool grey = false)
        {
            bool hidden = material < 0;
            RoundMaterial m = RoundMaterial.All[Math.Max(0, material)];
            bool symbol = symbolsMode && !hidden;
            Rgba color = grey ? RoundLook.ColorOf(m).Grey() : RoundLook.ColorOf(m);
            string? mark = symbol ? RoundLook.SymbolOf(m) : null;
            int s = Math.Max(1, size);
            return ProceduralSprites.Picture(UiRaster.PodCubeKey(m.Code, hidden, look, symbol, grey), s, s, (w, h) => UiRaster.PodCube(Math.Min(w, h), color, mark, hidden, look));
        }

        /// <summary>
        /// A cube's side face (<c>pod.cube.side</c>, constitution 1.2.0 VII; the playtest's <c>Kit.SideFace</c>): material -1
        /// is a hidden pod's; <paramref name="topShare"/> of it stands beside the top face (<see cref="UiRaster.SideFace"/>).
        /// </summary>
        public static Sprite SideFace(int material, bool left, float topShare, int width, int height, bool grey = false)
        {
            char code = material >= 0 ? RoundMaterial.All[material].Code : '?';
            Rgba color = material >= 0 ? RoundLook.ColorOf(material) : DesignTokens.Colors.TileMystery;
            if (grey)
            {
                color = color.Grey();
            }

            float share = (float)Math.Round(topShare * 100f) / 100f;
            return ProceduralSprites.Picture(UiRaster.SideFaceKey(code, left, share, grey), Math.Max(1, width), Math.Max(1, height), (w, h) => UiRaster.SideFace(w, h, color, left, share));
        }

        /// <summary>
        /// A round board's oblique cube (spec 006 FR-064; the playtest's <c>Kit.BoardCube</c>) at a pixel size of a column wide
        /// and a row's pitch with the front face tall (<see cref="RoundBoardLayout.CubeBox"/>; <see cref="UiRaster.BoardCube"/>).
        /// </summary>
        public static Sprite BoardCube(int material, bool symbolsMode, int width, int height)
        {
            RoundMaterial m = RoundMaterial.All[material];
            Rgba color = RoundLook.ColorOf(m);
            string? mark = symbolsMode ? RoundLook.SymbolOf(m) : null;
            return ProceduralSprites.Picture(UiRaster.BoardCubeKey(m.Code, symbolsMode), Math.Max(1, width), Math.Max(1, height), (w, h) => UiRaster.BoardCube(w, h, color, mark));
        }

        /// <summary>The link tube's picture (<see cref="UiRaster.LinkTube"/>) of <paramref name="shape"/>'s colours at a pixel size.</summary>
        public static Sprite LinkTube(LinkTubeShape shape, int width, int height)
        {
            Rgba start = shape.StartColor;
            Rgba end = shape.EndColor;
            return ProceduralSprites.Picture(shape.PictureKey, Math.Max(1, width), Math.Max(1, height), (w, h) => UiRaster.LinkTube(w, h, start, end));
        }
    }
}
