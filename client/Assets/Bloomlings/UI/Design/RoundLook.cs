using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// How round levels look (spec 006 FR-040, FR-041, FR-045, FR-047; contracts/presentation.md), shared by both builds.
    /// Presentation only.
    /// <list type="bullet">
    /// <item><description>Board cubes are the game's soft cubes (<see cref="UiRaster.MaterialTile"/>, <see cref="CubeStyle"/>)
    /// in the material's normal palette colour, with no variant icon.</description></item>
    /// <item><description>The symbols mode (a Settings switch, off by default; the save's <c>settings.symbols</c>) puts the
    /// material's symbol (<see cref="SymbolIds"/>, the shapes <c>symbol.material.*</c>) on cubes, pods and places, in the
    /// tile's ink (<see cref="SymbolInk"/>), so colour-blind players can tell the 16 colours apart.</description></item>
    /// <item><description>A hidden pod that has just revealed glows for <see cref="RevealGlowSeconds"/>
    /// (<see cref="RevealGlow"/>).</description></item>
    /// </list>
    /// Engine-free.
    /// </summary>
    public static class RoundLook
    {
        /// <summary>The board's cubes: the soft cube with colour alone (the symbol, when on, is drawn by <see cref="UiRaster.MaterialTile"/>).</summary>
        public const TileStyle CubeStyle = TileStyle.BoardColor;

        /// <summary>Pods and places: the sticker tile's face, without a variant picture.</summary>
        public const TileStyle PodStyle = TileStyle.Sticker;

        /// <summary>How long a just-revealed hidden pod glows, in seconds of real time (FR-045: "about 1.6 s").</summary>
        public const float RevealGlowSeconds = 1.6f;

        /// <summary>The reveal glow's asset slot (soft rings round a just-revealed pod's frame).</summary>
        public const string RevealGlowSlot = "pod.reveal.glow";

        /// <summary>How fast the glow rises to its full strength, in seconds.</summary>
        public const float RevealGlowRise = 0.18f;

        /// <summary>
        /// The symbol shape (a <see cref="ShapeLibrary"/> id and an asset slot) of each material, in
        /// <see cref="RoundMaterial.Codes"/> order: ♧ ✿ ◆ ● ◇ ▰ ✦ ☀ ▲ ✚ ♣ ♥ ❀ ▼ ○ ■.
        /// </summary>
        public static IReadOnlyList<string> SymbolIds { get; } = new[]
        {
            "symbol.material.leaf",
            "symbol.material.petal",
            "symbol.material.water",
            "symbol.material.amber",
            "symbol.material.pearl",
            "symbol.material.earth",
            "symbol.material.plum",
            "symbol.material.sun",
            "symbol.material.pumpkin",
            "symbol.material.lime",
            "symbol.material.moss",
            "symbol.material.berry",
            "symbol.material.blush",
            "symbol.material.deep",
            "symbol.material.sky",
            "symbol.material.bark",
        };

        private static readonly Rgba[] Colors = BuildColors();

        /// <summary>The material's normal palette colour (FR-047: readability-checked under normal vision).</summary>
        public static Rgba ColorOf(RoundMaterial material) => Colors[material.Index];

        /// <summary>The colour of a material code (<see cref="RoundMaterial.Codes"/>).</summary>
        public static Rgba ColorOf(char code) => ColorOf(RoundMaterial.Of(code));

        /// <summary>The colour of a material index (a board cell's value, a pod's colour).</summary>
        public static Rgba ColorOf(int index) => Colors[index];

        /// <summary>The material's symbol shape (<see cref="SymbolIds"/>).</summary>
        public static string SymbolOf(RoundMaterial material) => SymbolIds[material.Index];

        /// <summary>The symbol to draw on the material's cube, pod or place: its symbol in the symbols mode, none otherwise (FR-041).</summary>
        public static string? Mark(RoundMaterial material, bool symbolsMode) => symbolsMode ? SymbolOf(material) : null;

        /// <summary>The symbol's ink on a tile of <paramref name="tile"/> colour: white or the dark ink, whichever reads (<see cref="Rgba.Ink"/>).</summary>
        public static Rgba SymbolInk(Rgba tile) => tile.Ink;

        /// <summary>The thin halo round the symbol, in the tone opposite its ink, so its edge stays crisp on the cube's light and shade.</summary>
        public static Rgba SymbolHalo(Rgba tile) => SymbolInk(tile).Luminance > 0.5 ? tile.Darken(0.45f) : tile.Lighten(0.45f);

        /// <summary>
        /// The reveal glow's strength <paramref name="seconds"/> after a hidden pod revealed: it rises to 1 over
        /// <see cref="RevealGlowRise"/>, then fades out smoothly by <see cref="RevealGlowSeconds"/>; 0 before and after.
        /// </summary>
        public static float RevealGlow(float seconds)
        {
            if (seconds < 0f || seconds >= RevealGlowSeconds)
            {
                return 0f;
            }

            if (seconds < RevealGlowRise)
            {
                float t = seconds / RevealGlowRise;
                return t * t * (3f - (2f * t));
            }

            float k = (seconds - RevealGlowRise) / (RevealGlowSeconds - RevealGlowRise);
            float fade = 1f - k;
            return fade * fade * (3f - (2f * fade));
        }

        private static Rgba[] BuildColors()
        {
            var colors = new Rgba[RoundMaterial.All.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Rgba.FromHex(RoundMaterial.All[i].ColorHex);
            }

            if (colors.Length != RoundMaterial.Codes.Length)
            {
                throw new InvalidOperationException("Every round material needs its colour.");
            }

            return colors;
        }
    }
}
