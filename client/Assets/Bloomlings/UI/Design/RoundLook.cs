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

        /// <summary>
        /// The pod a closed latch holds (17 §8, FR-068): the placed cube a little darker (<see cref="RoundLook.LockedDarken"/>),
        /// under its padlock and acorn hero; it takes no tap.
        /// </summary>
        Locked,
    }

    /// <summary>
    /// How round levels look (spec 006 FR-040, FR-041, FR-045, FR-047; contracts/presentation.md), shared by both builds.
    /// Presentation only.
    /// <list type="bullet">
    /// <item><description>Board cubes are the game's soft cubes (<see cref="UiRaster.MaterialTile"/>, <see cref="CubeStyle"/>)
    /// in the material's normal palette colour, with no variant icon.</description></item>
    /// <item><description>The symbols mode (a Settings switch, off by default; the save's <c>settings.symbols</c>) puts the
    /// material's symbol (<see cref="SymbolIds"/>, the shapes <c>symbol.material.*</c>) on cubes, pods and places, in the
    /// tile's ink (<see cref="SymbolInk"/>), so colour-blind players can tell the 24 colours apart.</description></item>
    /// <item><description>Pods and places are the same soft cubes (<see cref="PodStyle"/>, <see cref="UiRaster.PodCube"/>); a
    /// waiting pod is only a little lighter (<see cref="WaitingColor"/>).</description></item>
    /// <item><description>A hidden pod that has just revealed glows for <see cref="RevealGlowSeconds"/>
    /// (<see cref="RevealGlow"/>).</description></item>
    /// <item><description>A hidden cell (17 §8 «Скрытые клетки») is the neutral "?" cube (<see cref="HiddenCellSlot"/>) until
    /// it reveals, then pops into its material (<see cref="CellRevealScale"/>, <see cref="CellRevealVeil"/>).</description></item>
    /// <item><description>A latch (17 §8, FR-068; the owner's look of 2026-11-01, as the reference game's) is drawn on the pod it holds:
    /// that pod a little darker with a padlock (<see cref="PadlockSlot"/>) and our acorn hero hugging it
    /// (<see cref="LatchParts"/>); its key (<see cref="KeySlot"/>) is an ordinary big key in that pod's colour on its board cell
    /// (<see cref="KeyMetal"/>, <see cref="KeyBox"/>). When it opens the key lifts and fades (<see cref="KeyOpen"/>), the
    /// padlock springs open and drops (<see cref="PadlockOpen"/>) and the acorn hops off (<see cref="AcornHop"/>).</description></item>
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

        /// <summary>
        /// A hidden cell's asset slot (rules 17 §8 «Скрытые клетки», the owner's amendment of 2026-11-01; spec 006 FR-066): an
        /// unrevealed hidden cell is drawn as the board's oblique cube in <see cref="HiddenPodColor"/> with the hidden pod's
        /// white "?" marks (<see cref="UiRaster.HiddenBoardCube"/>), its side face in the same grey, until its
        /// <c>cellReveal</c> event plays; then it pops into its material's cube (<see cref="CellRevealScale"/>).
        /// </summary>
        public const string HiddenCellSlot = "tile.round.hidden";

        /// <summary>
        /// The padlock's asset slot (rules 17 §8, the owner's look of 2026-11-01 as the reference game's; spec 006 FR-068): a latch
        /// is drawn on the pod it holds (the pod right behind it in its column, <see cref="RoundAnimator.HeldPod"/>), in that
        /// pod's place: its cube and number a little darker (<see cref="RoundPodLook.Locked"/>), a brass padlock
        /// (<see cref="UiRaster.Padlock"/>) on its top's right and our acorn hero (the variant's 2D character
        /// <c>2d/acorn-happy</c>, <c>char.v.acorn</c>; <see cref="UiRaster.AcornFigure"/> while it is missing) sitting on the
        /// corner hugging it, as if he had locked the pod himself (<see cref="LatchParts"/>).
        /// </summary>
        public const string PadlockSlot = "pod.round.padlock";

        /// <summary>
        /// The key's asset slot (17 §8): an ordinary big key (<see cref="UiRaster.RoundKey"/>) lying on its empty board cell,
        /// <see cref="KeyLengthCells"/> long over the cubes round it, in the colour of the pod its latch holds
        /// (<see cref="KeyMetal"/>), breathing up and down (<see cref="KeyBob"/>).
        /// </summary>
        public const string KeySlot = "board.round.key";

        /// <summary>The key's length in cells (the owner, 2026-11-01: "a big key on the board").</summary>
        public const float KeyLengthCells = 2.4f;

        /// <summary>The key picture's width in key lengths (the tilted key with its side face and halo).</summary>
        public const float KeyBoxWidth = 1.34f;

        /// <summary>The key picture's height in key lengths.</summary>
        public const float KeyBoxHeight = 1.1f;

        /// <summary>
        /// The key's shortest length as a share of the board surface's width, so it reads at phone scale on a board of small
        /// cubes too (the owner, 2026-11-01: "a big key"): on a board of more than about 18 columns it is longer than
        /// <see cref="KeyLengthCells"/>.
        /// </summary>
        public const float KeyBoardShare = 0.13f;

        /// <summary>
        /// The key's picture box over its cell <paramref name="cell"/> (a column wide) on the board surface
        /// <paramref name="surface"/> (null: none, the cells alone): <see cref="KeyLengthCells"/> cells long, at least
        /// <see cref="KeyBoardShare"/> of the surface's width; the key's middle on the cell's, moved in so the key stays on
        /// the surface (a key by the edge never lies over the frame).
        /// </summary>
        public static Box KeyBox(Box cell, Box? surface = null)
        {
            float len = Math.Max(cell.Width * KeyLengthCells, surface.HasValue ? surface.Value.Width * KeyBoardShare : 0f);
            Box box = Box.FromCenter(cell.CenterX, cell.CenterY, len * KeyBoxWidth, len * KeyBoxHeight);
            if (!surface.HasValue)
            {
                return box;
            }

            // The picture's margin (the halo and the tilt's corners) may stand over the frame; the key itself stays inside.
            Box s = surface.Value;
            float mx = len * 0.08f;
            float my = len * 0.12f;
            float dx = Math.Max(0f, (s.Left - mx) - box.Left) - Math.Max(0f, box.Right - (s.Right + mx));
            float dy = Math.Max(0f, (s.Top - my) - box.Top) - Math.Max(0f, box.Bottom - (s.Bottom + my));
            return box.Offset(dx, dy);
        }

        /// <summary>A key's metal: the held pod's colour (<paramref name="material"/>), or <see cref="KeyBrass"/> for a hidden pod (-1).</summary>
        public static Rgba KeyMetal(int material) => material < 0 ? KeyBrass : ColorOf(material);

        /// <summary>The key of a latch holding a hidden pod: neutral brass.</summary>
        public static readonly Rgba KeyBrass = Rgba.FromHex("#D9A23A");

        /// <summary>The soft light halo round the key.</summary>
        public static readonly Rgba KeyHalo = Rgba.FromHex("#FFF6D8");

        /// <summary>The padlock's brass body.</summary>
        public static readonly Rgba PadlockBrass = Rgba.FromHex("#E2AE3E");

        /// <summary>The padlock's steel shackle.</summary>
        public static readonly Rgba PadlockSteel = Rgba.FromHex("#9BA4AE");

        /// <summary>The padlock's dark outline and keyhole.</summary>
        public static readonly Rgba PadlockInk = Rgba.FromHex("#3A2A16");

        /// <summary>The acorn's nut, cap and outline (the hero's arms and the drawn stand-in).</summary>
        public static readonly Rgba AcornNut = Rgba.FromHex("#C07A3A");

        public static readonly Rgba AcornCap = Rgba.FromHex("#6E4A26");

        public static readonly Rgba AcornInk = Rgba.FromHex("#3F2810");

        /// <summary>How much darker the pod a closed latch holds is drawn (<see cref="RoundPodLook.Locked"/>).</summary>
        public const float LockedDarken = 0.2f;

        /// <summary>The locked pod's number's opacity.</summary>
        public const float LockedCountAlpha = 0.8f;

        /// <summary>
        /// Where the acorn hero and the padlock stand on the held pod's cube <paramref name="frame"/> (<see cref="PodChip.Frame"/>):
        /// the acorn sitting on the top's right corner (rising above the cube, a little out over its right side), the padlock
        /// hanging from his arms below his face over the top's right, clear of the number in the middle.
        /// </summary>
        public static (Box Acorn, Box Lock) LatchParts(Box frame)
        {
            float s = Math.Min(frame.Width, frame.Height);
            float ax = frame.Left + (s * 0.9f);
            var acorn = new Box(ax - (s * 0.28f), frame.Top - (s * 0.28f), ax + (s * 0.28f), frame.Top + (s * 0.28f));
            var padlock = new Box(ax - (s * 0.27f), frame.Top + (s * 0.045f), ax + (s * 0.27f), frame.Top + (s * 0.585f));
            return (acorn, padlock);
        }

        /// <summary>How long a latch's opening plays, in seconds of display time (<see cref="RoundAnimator.LatchOpenUs"/>).</summary>
        public const float LatchOpenSeconds = RoundAnimator.LatchOpenUs / 1_000_000f;

        /// <summary>
        /// The padlock <paramref name="seconds"/> after its <c>latchOpen</c> (-1: closed): whether its shackle is open, how far
        /// it has dropped (a share of the cube's side), its scale and opacity: it springs open with a small swell, then drops
        /// and fades over <see cref="LatchOpenSeconds"/>.
        /// </summary>
        public static (bool Open, float Drop, float Scale, float Alpha) PadlockOpen(float seconds)
        {
            if (seconds < 0f)
            {
                return (false, 0f, 1f, 1f);
            }

            float t = Math.Min(1f, seconds / LatchOpenSeconds);
            float swell = t < 0.25f ? (float)Math.Sin(Math.PI * t / 0.25f) * 0.15f : 0f;
            float fall = t < 0.25f ? 0f : (t - 0.25f) / 0.75f;
            return (true, 0.55f * fall * fall, 1f + swell, 1f - (fall * fall));
        }

        /// <summary>
        /// The acorn hero <paramref name="seconds"/> after his latch opened (-1: closed): how far he has moved (shares of the
        /// cube's side, right and up) and his opacity: he hops up and off to the right and fades.
        /// </summary>
        public static (float X, float Y, float Alpha) AcornHop(float seconds)
        {
            if (seconds < 0f)
            {
                return (0f, 0f, 1f);
            }

            float t = Math.Min(1f, seconds / LatchOpenSeconds);
            float hop = (float)Math.Sin(Math.PI * Math.Min(1f, t * 1.4f)) * 0.32f;
            return (0.45f * t, hop - (0.15f * t * t), 1f - (t * t * (3f - (2f * t))));
        }

        /// <summary>
        /// The key <paramref name="seconds"/> after its latch opened (-1: closed): how far it rises (cells), its scale and its
        /// opacity: it swells as it lifts off its cell, then shrinks away and fades.
        /// </summary>
        public static (float Lift, float Scale, float Alpha) KeyOpen(float seconds)
        {
            if (seconds < 0f)
            {
                return (0f, 1f, 1f);
            }

            float t = Math.Min(1f, seconds / LatchOpenSeconds);
            float scale = t < 0.3f ? 1f + (0.3f * (t / 0.3f)) : 1.3f - (0.8f * ((t - 0.3f) / 0.7f));
            float fade = t < 0.35f ? 0f : (t - 0.35f) / 0.65f;
            return (1.1f * (1f - ((1f - t) * (1f - t))), scale, 1f - (fade * fade));
        }

        /// <summary>The waiting key's gentle bob at <paramref name="seconds"/> of display time: its rise in cells.</summary>
        public static float KeyBob(float seconds) => 0.07f * (0.5f + (0.5f * (float)Math.Sin(seconds * 2f * Math.PI / 1.5f)));

        /// <summary>How long a hidden cell's reveal pops, in seconds of display time (<see cref="RoundAnimator.CellRevealUs"/>).</summary>
        public const float CellRevealSeconds = RoundAnimator.CellRevealUs / 1_000_000f;

        /// <summary>
        /// A revealing hidden cell's cube scale <paramref name="seconds"/> after its reveal: a short swell to 1.2 and back over
        /// <see cref="CellRevealSeconds"/>, 1 before and after.
        /// </summary>
        public static float CellRevealScale(float seconds)
        {
            if (seconds < 0f || seconds >= CellRevealSeconds)
            {
                return 1f;
            }

            return 1f + (0.2f * (float)Math.Sin(Math.PI * seconds / CellRevealSeconds));
        }

        /// <summary>
        /// The "?" cube's opacity over the revealing cube <paramref name="seconds"/> after the reveal: it fades out over the first
        /// 60% of <see cref="CellRevealSeconds"/> (1 before the reveal, 0 after the fade).
        /// </summary>
        public static float CellRevealVeil(float seconds)
        {
            if (seconds < 0f)
            {
                return 1f;
            }

            float t = seconds / (CellRevealSeconds * 0.6f);
            if (t >= 1f)
            {
                return 0f;
            }

            return 1f - (t * t * (3f - (2f * t)));
        }

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
        /// <see cref="RoundMaterial.Codes"/> order: ♧ ✿ ◆ ● ◇ ▰ ✦ ☀ ▲ ✚ ♣ ♥ ❀ ▼ ○ ■ ❦ ✕ ★ ☾ ☁ ❄ ⧗ ⬢ (the last 8 the owner's of 2026-11-01).
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
            "symbol.material.mint",
            "symbol.material.olive",
            "symbol.material.fuchsia",
            "symbol.material.lavender",
            "symbol.material.fog",
            "symbol.material.snow",
            "symbol.material.sand",
            "symbol.material.coal",
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
