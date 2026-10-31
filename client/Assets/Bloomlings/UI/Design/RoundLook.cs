using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>How a round pod's cube is drawn (spec 006 FR-064, <see cref="UiRaster.PodCube"/>).</summary>
    public enum RoundPodLook
    {
        /// <summary>The top row's pod, the one that takes the tap: in a white outline, its full base.</summary>
        Top,

        /// <summary>A pod waiting in rows 2 and 3: barely lighter, a flatter base, its number half clear.</summary>
        Waiting,

        /// <summary>A pod in a place (or flying there): the top row's cube without the outline.</summary>
        Placed,
    }

    /// <summary>
    /// How round levels look (spec 006 FR-040, FR-041, FR-045, FR-047; contracts/presentation.md), shared by both builds.
    /// Presentation only.
    /// <list type="bullet">
    /// <item><description>Board cubes are the game's soft cubes (<see cref="UiRaster.MaterialTile"/>, <see cref="CubeStyle"/>)
    /// in the material's normal palette colour, with no variant icon.</description></item>
    /// <item><description>The symbols mode (a Settings switch, off by default; the save's <c>settings.symbols</c>) puts the
    /// material's symbol (<see cref="SymbolIds"/>, the shapes <c>symbol.material.*</c>) on cubes, pods and places, in the
    /// tile's ink (<see cref="SymbolInk"/>), so colour-blind players can tell the 16 colours apart.</description></item>
    /// <item><description>Pods and places are the same soft cubes (<see cref="PodStyle"/>, <see cref="UiRaster.PodCube"/>); a
    /// waiting pod is only a little lighter (<see cref="WaitingColor"/>).</description></item>
    /// <item><description>A hidden pod that has just revealed glows for <see cref="RevealGlowSeconds"/>
    /// (<see cref="RevealGlow"/>).</description></item>
    /// </list>
    /// Engine-free.
    /// </summary>
    public static class RoundLook
    {
        /// <summary>
        /// The 2D character a round level's walker of <paramref name="family"/> is drawn as, tinted in its cube's colour: the
        /// leaf, the flower, the drop and the acorn (the owner, 2026-10-31: the acorn instead of the log, nearer Twig's acorn
        /// hat; the walkers stay these characters, not the meta screens' heroes).
        /// </summary>
        /// <summary>
        /// A hidden pod's cube (the owner, 2026-10-31: neutral, as the reference game's): a dark neutral grey in every row,
        /// white "?" marks on its top, no material colour until it reveals.
        /// </summary>
        public static readonly Rgba HiddenPodColor = Rgba.FromHex("#4A4C55");

        public static VariantId WalkerVariant(Family family) => family switch
        {
            Family.Bloom => VariantId.Flower,
            Family.Drop => VariantId.Water,
            Family.Twig => VariantId.Acorn,
            _ => VariantId.Leaf,
        };

        /// <summary>The board's cubes: the soft cube with colour alone (the symbol, when on, is drawn by <see cref="UiRaster.MaterialTile"/>).</summary>
        public const TileStyle CubeStyle = TileStyle.BoardColor;

        /// <summary>
        /// Pods, places, the flights between them and the jam card's well: the same soft cube as the board's (the owner's pods
        /// of 2026-10-28, FR-064: "volumetric, like cubes"), without a variant picture.
        /// </summary>
        public const TileStyle PodStyle = TileStyle.BoardColor;

        /// <summary>A round level's pod cube's asset slot (<see cref="UiRaster.PodCube"/>): no frame or box round it.</summary>
        public const string PodCubeSlot = "pod.cube";

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

        /// <summary>
        /// A waiting pod's colour (rows 2 and 3 of its column, FR-064): its material's colour lightened
        /// <see cref="RoundScreenLook.InactiveLighten"/> toward white (the owner's value), dimmer than the top row.
        /// </summary>
        public static Rgba WaitingColor(Rgba color) => WaitingColor(color, RoundScreenLook.InactiveLighten);

        /// <summary>
        /// The side face's step of a cube <paramref name="offset"/> from the centre of the board (or the screen, for pods and
        /// places), <paramref name="reach"/> being the offset of the outermost column: from -<see cref="RoundScreenLook.SideSteps"/>
        /// (the left side, at the left edge) through 0 (no side, at the centre) to +<see cref="RoundScreenLook.SideSteps"/>
        /// (the right side). Quantised so a colour has few side pictures (constitution 1.2.0 VII: one fixed viewpoint, flat
        /// pictures, nothing projected at run time).
        /// </summary>
        public static int SideStep(float offset, float reach)
        {
            if (reach <= 0f)
            {
                return 0;
            }

            float k = Math.Max(-1f, Math.Min(1f, offset / reach));
            return (int)Math.Round(k * RoundScreenLook.SideSteps, MidpointRounding.AwayFromZero);
        }

        /// <summary>A side face's width for a cube <paramref name="width"/> wide at <paramref name="step"/> (<see cref="SideStep"/>).</summary>
        public static float SideWidth(float width, int step) => width * RoundScreenLook.SideShare * Math.Abs(step) / RoundScreenLook.SideSteps;

        /// <summary>
        /// Where a side face stands beside a cube's <paramref name="body"/> (its top and front, without a pod's outline):
        /// outside its left edge for a negative <paramref name="step"/>, its right for a positive one, as tall as the body
        /// less its rounded top corner, tucked a hair under it. Empty at step 0.
        /// </summary>
        public static Box SideBox(Box body, int step)
        {
            float w = SideWidth(body.Width, step);
            if (w <= 0f)
            {
                return new Box(body.Left, body.Top, body.Left, body.Top);
            }

            float tuck = body.Width * 0.03f;
            float top = body.Top + (body.Width * RoundScreenLook.CubeCornerShare * 0.5f);
            return step < 0
                ? new Box(body.Left - w, top, body.Left + tuck, body.Bottom)
                : new Box(body.Right - tuck, top, body.Right + w, body.Bottom);
        }

        /// <summary>A pod's or place's cube body in its square <paramref name="box"/> (inside the top row's outline, <see cref="RoundScreenLook.PodRingShare"/>).</summary>
        public static Box PodBody(Box box)
        {
            float s = Math.Min(box.Width, box.Height);
            return Box.FromCenter(box.CenterX, box.CenterY, s, s).Inset(s * RoundScreenLook.PodRingShare);
        }

        /// <summary>The side face's asset slot (<see cref="UiRaster.SideFace"/>).</summary>
        public const string SideSlot = "pod.cube.side";

        /// <summary>A waiting pod's colour at <paramref name="lighten"/> toward white (the constructor's <c>inactiveLighten</c>).</summary>
        public static Rgba WaitingColor(Rgba color, float lighten) => color.Lighten(lighten);

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
