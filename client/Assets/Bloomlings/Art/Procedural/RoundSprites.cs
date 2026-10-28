using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;

namespace Bloomlings.Client.Art
{
    /// <summary>
    /// The round levels' pictures as cached sprites (spec 006 contracts/presentation.md; the playtest's <c>KitRound</c>),
    /// rendered from the kit's engine-free <see cref="UiRaster"/> with the same keys: material tiles without a variant
    /// icon (the material's symbol only in the symbols mode), the stream's water, the cleared cell, the round pod's
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

        /// <summary>A cleared cell (<c>tile.ground</c>): the material's colour lightened, as the game's restored cells.</summary>
        public static Sprite Cleared(int material, int size)
        {
            Rgba color = RoundLook.ColorOf(material).Lighten(0.55f);
            int s = Math.Max(1, size);
            return ProceduralSprites.Picture("tile.ground/round/" + RoundMaterial.All[material].Code, s, s, (w, h) => UiRaster.ClearedCell(w, h, color));
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
