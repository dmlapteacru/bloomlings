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
    /// wooden frame tinted by its material and the link tube. Each picture is cached by key and pixel size.
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

        /// <summary>A round pod's wooden frame tinted by its material (null: a hidden pod, <see cref="GameplaySkin.Pod(int, int, Rgba?, bool, bool)"/>).</summary>
        public static Sprite Pod(int material, bool waiting, bool locked, int width, int height)
        {
            Rgba? color = material >= 0 ? RoundLook.ColorOf(material) : (Rgba?)null;
            string name = material >= 0 ? RoundMaterial.All[material].Code.ToString() : "mystery";
            return ProceduralSprites.Picture("gameplay.pod.round/" + name + "/" + waiting + "/" + locked, Math.Max(1, width), Math.Max(1, height), (w, h) => GameplaySkin.Pod(w, h, color, waiting, locked));
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
