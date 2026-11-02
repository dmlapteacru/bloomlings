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
    /// tile's ink (<see cref="SymbolInk"/>), so colour-blind players can tell the 24 colours apart.</description></item>
    /// <item><description>Pods and places are the same soft cubes (<see cref="PodStyle"/>, <see cref="UiRaster.PodCube"/>); a
    /// waiting pod is only a little lighter (<see cref="WaitingColor"/>).</description></item>
    /// <item><description>A hidden pod that has just revealed glows for <see cref="RevealGlowSeconds"/>
    /// (<see cref="RevealGlow"/>).</description></item>
    /// <item><description>A hidden cell (17 §8 «Скрытые клетки») is the neutral "?" cube (<see cref="HiddenCellSlot"/>) until
    /// it reveals, then pops into its material (<see cref="CellRevealScale"/>, <see cref="CellRevealVeil"/>).</description></item>
    /// <item><description>A latch (17 §8, FR-068; the owner's look of 2026-11-02, "Twig hero 100%") hides the pod it holds:
    /// in its place stands our Twig hero in the player's outfit holding a padlock in that pod's colour (<see cref="PadlockSlot"/>,
    /// <see cref="HeroCell"/>, <see cref="HeroLock"/>, <see cref="HeroHands"/>); its key (<see cref="KeySlot"/>) is an ordinary
    /// big key in that pod's colour standing upright on its tip over its board cell, hovering, bobbing and turning
    /// (<see cref="KeyBox"/>, <see cref="KeySpin"/>, <see cref="KeyMetal"/>). When it opens the key flies to the keyhole and
    /// turns (<see cref="KeyFlight"/>), the shackle springs open and the padlock drops (<see cref="PadlockOpen"/>), and the hero
    /// steps aside from the pod (<see cref="HeroStep"/>).</description></item>
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
        /// The latch's asset slot (rules 17 §8, spec 006 FR-068; the owner's look of 2026-11-02, "Twig hero 100%"): while a latch is
        /// closed the pod it holds (the pod right behind it in its column, <see cref="RoundAnimator.HeldPod"/>) is not drawn at
        /// all; in its place stands our Twig hero, the owner's modular Twig V2 frames in the player's equipped outfit
        /// (<see cref="HeroGearArt.Layer"/>, stock Twig when none; <see cref="HeroCell"/>), holding in front of his belly a
        /// padlock whose body is the held pod's colour (<see cref="UiRaster.Padlock"/>, <see cref="KeyMetal"/>; brass for a
        /// hidden pod or a latch with no pod of its own; <see cref="HeroLock"/>), his hands over its sides
        /// (<see cref="HeroHands"/>: the hero's own layers again, clipped to two discs), a soft contact shadow under his feet
        /// (<see cref="HeroShadow"/>). In rows 2 and 3 he takes the waiting rows' lightening (<see cref="HeroWaitingVeil"/>).
        /// </summary>
        public const string PadlockSlot = "pod.round.padlock";

        /// <summary>
        /// The key's asset slot (17 §8, the owner's look of 2026-11-02): an ordinary big key standing upright on its tip
        /// (<see cref="UiRaster.UprightKey"/>), bow up, hovering a little over its empty cell (<see cref="KeyBox"/>) with a soft oval
        /// shadow on the cell under the tip (<see cref="KeyShadow"/>), bobbing (<see cref="KeyBob"/>) and turning round its own
        /// upright axis (<see cref="KeySpin"/>), in the colour of the pod its latch holds (<see cref="KeyMetal"/>).
        /// </summary>
        public const string KeySlot = "board.round.key";

        /// <summary>The key's length in cells (the owner, 2026-11-01: "a big key on the board"), its picture's height about this.</summary>
        public const float KeyLengthCells = 2.4f;

        /// <summary>The upright key picture's width in key lengths (the bow, the bit's teeth, the side face and a thin halo).</summary>
        public const float KeyBoxWidth = 0.66f;

        /// <summary>The upright key picture's height in key lengths.</summary>
        public const float KeyBoxHeight = 1.18f;

        /// <summary>
        /// The key's shortest length as a share of the board surface's width, so it reads at phone scale on a board of small
        /// cubes too (the owner, 2026-11-01: "a big key"): on a board of more than about 18 columns it is longer than
        /// <see cref="KeyLengthCells"/>.
        /// </summary>
        public const float KeyBoardShare = 0.13f;

        /// <summary>How far the key's tip hovers over its cell's middle, in cells (the owner, 2026-11-02: "not touching the board").</summary>
        public const float KeyHoverCells = 0.32f;

        /// <summary>Where the key's tip is along the picture's height, from its middle, in key lengths (<see cref="UiRaster.UprightKey"/>).</summary>
        public const float KeyTipShare = 0.48f;

        /// <summary>How long a waiting key takes for one turn round its upright axis, in seconds.</summary>
        public const float KeySpinSeconds = 1.8f;

        /// <summary>The narrowest the turning key is drawn, as a share of its width, so it never vanishes edge-on.</summary>
        public const float KeyMinWidth = 0.12f;

        /// <summary>How much darker the turning key's back half is (<see cref="KeyBack"/>).</summary>
        public const float KeyBackDarken = 0.16f;

        /// <summary>A key's length in pixels over <paramref name="cell"/> on the board surface <paramref name="surface"/>.</summary>
        public static float KeyLength(Box cell, Box? surface = null) =>
            Math.Max(cell.Width * KeyLengthCells, surface.HasValue ? surface.Value.Width * KeyBoardShare : 0f);

        /// <summary>
        /// The upright key's picture box over its cell <paramref name="cell"/> (a column wide) on the board surface
        /// <paramref name="surface"/> (null: none, the cell alone): <see cref="KeyLength"/> tall (<see cref="KeyBoxHeight"/>),
        /// <see cref="KeyBoxWidth"/> wide, its tip <see cref="KeyHoverCells"/> over the cell's middle; moved in so the key stays
        /// on the surface (its halo may stand over the frame).
        /// </summary>
        public static Box KeyBox(Box cell, Box? surface = null)
        {
            float len = KeyLength(cell, surface);
            float tip = cell.CenterY - (cell.Width * KeyHoverCells);
            Box box = Box.FromCenter(cell.CenterX, tip - (len * KeyTipShare), len * KeyBoxWidth, len * KeyBoxHeight);
            if (!surface.HasValue)
            {
                return box;
            }

            Box s = surface.Value;
            float m = len * 0.06f;
            float dx = Math.Max(0f, (s.Left - m) - box.Left) - Math.Max(0f, box.Right - (s.Right + m));
            float dy = Math.Max(0f, (s.Top - m) - box.Top);
            return box.Offset(dx, dy);
        }

        /// <summary>The soft oval shadow on the key's cell under its tip (<see cref="UiRaster.SoftOval"/>), at rest.</summary>
        public static Box KeyShadow(Box cell, Box? surface = null)
        {
            float w = KeyLength(cell, surface) * 0.34f;
            return Box.FromCenter(cell.CenterX, cell.CenterY, w, w * 0.38f);
        }

        /// <summary>The key's shadow's scale while the key bobs <paramref name="rise"/> cells up: a little smaller the higher it is.</summary>
        public static float KeyShadowScale(float rise) => 1f - (1.5f * rise);

        /// <summary>A waiting key's turns round its upright axis at <paramref name="seconds"/> of display time.</summary>
        public static float KeyTurns(float seconds) => seconds / KeySpinSeconds;

        /// <summary>
        /// The turning key at <paramref name="turns"/> (<see cref="KeyTurns"/>), as a flat 2D picture: its horizontal scale
        /// (cos of the turn, never under <see cref="KeyMinWidth"/>, negative on the back half: the picture mirrored) and whether
        /// its back faces us (drawn <see cref="KeyBackDarken"/> darker, <see cref="KeyBack"/>).
        /// </summary>
        public static (float ScaleX, bool Back) KeySpin(float turns)
        {
            float c = (float)Math.Cos(turns * 2.0 * Math.PI);
            bool back = c < 0f;
            float m = Math.Max(KeyMinWidth, Math.Abs(c));
            return (back ? -m : m, back);
        }

        /// <summary>The metal of a turning key's back half: <paramref name="metal"/> <see cref="KeyBackDarken"/> darker.</summary>
        public static Rgba KeyBack(Rgba metal) => metal.Darken(KeyBackDarken);

        /// <summary>The waiting key's gentle bob at <paramref name="seconds"/> of display time: its rise in cells.</summary>
        public static float KeyBob(float seconds) => 0.08f * (0.5f + (0.5f * (float)Math.Sin(seconds * 2f * Math.PI / 1.5f)));

        /// <summary>A key's metal: the held pod's colour (<paramref name="material"/>), or <see cref="KeyBrass"/> for a hidden pod (-1).</summary>
        public static Rgba KeyMetal(int material) => material < 0 ? KeyBrass : ColorOf(material);

        /// <summary>The key of a latch holding a hidden pod (and the padlock of a latch with no pod of its own): neutral brass.</summary>
        public static readonly Rgba KeyBrass = Rgba.FromHex("#D9A23A");

        /// <summary>The soft light halo round the key.</summary>
        public static readonly Rgba KeyHalo = Rgba.FromHex("#FFF6D8");

        /// <summary>The padlock's brass (the brass key's shackle light).</summary>
        public static readonly Rgba PadlockBrass = Rgba.FromHex("#E2AE3E");

        /// <summary>The padlock's steel shackle, the same on every body colour.</summary>
        public static readonly Rgba PadlockSteel = Rgba.FromHex("#9BA4AE");

        /// <summary>The padlock's dark outline and keyhole.</summary>
        public static readonly Rgba PadlockInk = Rgba.FromHex("#3A2A16");

        /// <summary>The contact shadows' colour (the hero's feet, the key's tip): soft black at this peak opacity.</summary>
        public const float ShadowAlpha = 0.3f;

        /// <summary>
        /// The hero's cell's height (a <see cref="HeroMotion.CellWidth"/> × <see cref="HeroMotion.CellHeight"/> cell, as the meta
        /// screens draw him) in place heights: his figure about 1.2 places tall, his head a little over the place.
        /// </summary>
        public const float HeroCellShare = 1.34f;

        /// <summary>How far over the place's bottom the hero's feet stand, in place heights.</summary>
        public const float HeroFootShare = 0.04f;

        /// <summary>The padlock's side in the hero's cell, in cell pixels (the owner's mock of 2026-11-02, padlock in front of his belly).</summary>
        public const float HeroLockPixels = 250f;

        /// <summary>The padlock square's top in the hero's cell, in cell pixels: its body over his belly and legs, the shackle under his chin.</summary>
        public const float HeroLockTopPixels = 230f;

        /// <summary>His hands' middles in the cell, in cell pixels (the V2 frames' rest pose), and their discs' radius.</summary>
        public const float HeroHandX = 138f;

        public const float HeroHandY = 378f;

        public const float HeroHandRadius = 34f;

        /// <summary>How far toward white the hero is drawn in rows 2 and 3, as the waiting pods (<see cref="RoundScreenLook.InactiveLighten"/>).</summary>
        public const float HeroWaitingVeil = RoundScreenLook.InactiveLighten;

        /// <summary>
        /// The hero's cell in a pod's place <paramref name="place"/> (<see cref="ReferenceGameplayRegions.Pod"/>): <see cref="HeroCellShare"/>
        /// places tall, centred on the place, his feet (the cell's <see cref="HeroMotion.FootLine"/>) <see cref="HeroFootShare"/> over
        /// its bottom. His layers go in it by <see cref="HeroMotion.PictureBox"/>.
        /// </summary>
        public static Box HeroCell(Box place)
        {
            float h = place.Height * HeroCellShare;
            float w = h * HeroMotion.CellWidth / HeroMotion.CellHeight;
            float top = place.Bottom - (place.Height * HeroFootShare) - (h * HeroMotion.FootLine);
            return new Box(place.CenterX - (w / 2f), top, place.CenterX + (w / 2f), top + h);
        }

        /// <summary>The padlock's square in the hero's cell <paramref name="cell"/> (<see cref="HeroLockPixels"/>, <see cref="HeroLockTopPixels"/>).</summary>
        public static Box HeroLock(Box cell)
        {
            float k = cell.Width / HeroMotion.CellWidth;
            float s = HeroLockPixels * k;
            float top = cell.Top + (HeroLockTopPixels * cell.Height / HeroMotion.CellHeight);
            return new Box(cell.CenterX - (s / 2f), top, cell.CenterX + (s / 2f), top + s);
        }

        /// <summary>
        /// His two hands' discs in the cell <paramref name="cell"/> (squares; the hosts clip the hero's layers to the disc in each
        /// and draw them again over the padlock's sides, so he holds it).
        /// </summary>
        public static (Box Left, Box Right) HeroHands(Box cell)
        {
            float kx = cell.Width / HeroMotion.CellWidth;
            float ky = cell.Height / HeroMotion.CellHeight;
            float r = HeroHandRadius * kx;
            float y = cell.Top + (HeroHandY * ky);
            float l = cell.Left + (HeroHandX * kx);
            float rx = cell.Left + ((HeroMotion.CellWidth - HeroHandX) * kx);
            return (Box.FromCenter(l, y, 2f * r, 2f * r), Box.FromCenter(rx, y, 2f * r, 2f * r));
        }

        /// <summary>The soft contact shadow under the hero's feet in his cell <paramref name="cell"/>.</summary>
        public static Box HeroShadow(Box cell)
        {
            float w = cell.Width * 0.6f;
            return Box.FromCenter(cell.CenterX, cell.Top + (cell.Height * HeroMotion.FootLine), w, w * 0.24f);
        }

        /// <summary>
        /// The keyhole's middle in a padlock's square <paramref name="lockBox"/> (<see cref="UiRaster.Padlock"/>, a little higher
        /// with <paramref name="pips"/> under it): the end of the key's flight.
        /// </summary>
        public static (float X, float Y) Keyhole(Box lockBox, int pips) =>
            (lockBox.CenterX, lockBox.Top + (Math.Min(lockBox.Width, lockBox.Height) * KeyholeShare(pips)));

        /// <summary>The keyhole's height in the padlock picture, as a share of its side (<see cref="Keyhole"/>).</summary>
        public static float KeyholeShare(int pips) => 0.53f + ((0.95f - 0.075f - 0.53f) * (pips > 0 ? 0.33f : 0.42f));

        /// <summary>The key's picture box in the keyhole of <paramref name="lockBox"/>: half the padlock long, its tip in the hole.</summary>
        public static Box KeyInHole(Box lockBox, int pips)
        {
            float len = Math.Min(lockBox.Width, lockBox.Height) * 0.5f;
            (float x, float y) = Keyhole(lockBox, pips);
            return Box.FromCenter(x, y + (len * 0.05f) - (len * KeyTipShare), len * KeyBoxWidth, len * KeyBoxHeight);
        }

        /// <summary>How long a latch's opening plays, in seconds of display time (<see cref="RoundAnimator.LatchOpenUs"/>).</summary>
        public const float LatchOpenSeconds = RoundAnimator.LatchOpenUs / 1_000_000f;

        /// <summary>The key's flight from its cell to the keyhole, from the latch's opening on, in seconds.</summary>
        public const float KeyFlightSeconds = 0.48f;

        /// <summary>The narrowest the flying key is drawn (its turn faster than the eye), so it reads all the way.</summary>
        public const float KeyFlightMinWidth = 0.3f;

        /// <summary>The key's quarter turn in the keyhole after it arrives, in seconds.</summary>
        public const float KeyTurnSeconds = 0.1f;

        /// <summary>The key's fade after its turn, in seconds.</summary>
        public const float KeyFadeSeconds = 0.1f;

        /// <summary>When the shackle springs open (the click), in seconds after the opening began.</summary>
        public const float ShackleOpenAt = 0.56f;

        /// <summary>When the open padlock starts to drop and fade, in seconds after the opening began.</summary>
        public const float PadlockDropAt = 0.66f;

        /// <summary>When the hero steps aside and fades, in seconds after the opening began.</summary>
        public const float HeroLeaveAt = 0.6f;

        /// <summary>
        /// The key <paramref name="seconds"/> after its latch opened (-1: closed, at rest), flying from its picture box on the
        /// board <paramref name="from"/> to <paramref name="to"/> in the keyhole (<see cref="KeyInHole"/>), its turn
        /// <paramref name="turns"/> when the opening began (<see cref="KeyTurns"/>): it lifts off, turns faster and flies on a
        /// gentle arc, shrinking to the keyhole's scale, by <see cref="KeyFlightSeconds"/> facing us; then makes a quarter turn
        /// in the hole (<see cref="KeyTurnSeconds"/>) and fades (<see cref="KeyFadeSeconds"/>). The picture box, its horizontal
        /// scale and back (<see cref="KeySpin"/>) and its opacity.
        /// </summary>
        public static (Box Box, float ScaleX, bool Back, float Alpha) KeyFlight(float seconds, Box from, Box to, float turns)
        {
            if (seconds < 0f)
            {
                (float sx, bool back) = KeySpin(turns);
                return (from, sx, back, 1f);
            }

            if (seconds < KeyFlightSeconds)
            {
                float q = seconds / KeyFlightSeconds;
                float e = q * q * (3f - (2f * q));
                float lift = (float)Math.Sin(Math.PI * q);
                float dist = (float)Math.Sqrt(((to.CenterX - from.CenterX) * (to.CenterX - from.CenterX)) + ((to.CenterY - from.CenterY) * (to.CenterY - from.CenterY)));
                float arc = Math.Max(from.Height * 0.3f, dist * 0.16f);
                float x = from.CenterX + ((to.CenterX - from.CenterX) * e);
                float y = from.CenterY + ((to.CenterY - from.CenterY) * e) - (arc * lift);
                // big for most of the way, shrinking to the keyhole's scale as it comes in
                float shrink = q * q;
                float grow = 1f + (0.1f * (float)Math.Sin(Math.PI * Math.Min(1f, q * 2f)));
                float w = (from.Width + ((to.Width - from.Width) * shrink)) * grow;
                float h = (from.Height + ((to.Height - from.Height) * shrink)) * grow;
                float end = (float)Math.Floor(turns) + 2f;
                float spun = turns + ((end - turns) * (1f - ((1f - q) * (1f - q))));
                (float sx, bool back) = KeySpin(spun);
                sx = Math.Sign(sx) * Math.Max(KeyFlightMinWidth, Math.Abs(sx));
                return (Box.FromCenter(x, y, w, h), sx, back, 1f);
            }

            float t = seconds - KeyFlightSeconds;
            if (t < KeyTurnSeconds)
            {
                return (to, Math.Max(KeyMinWidth, (float)Math.Cos(Math.PI * 0.5 * t / KeyTurnSeconds)), false, 1f);
            }

            float fade = Math.Min(1f, (t - KeyTurnSeconds) / KeyFadeSeconds);
            return (to, KeyMinWidth, false, 1f - fade);
        }

        /// <summary>
        /// The click's ring round the keyhole <paramref name="seconds"/> after the opening began: its radius (a share of the
        /// padlock's side) and opacity, from just before <see cref="ShackleOpenAt"/> for a quarter second; (0, 0) otherwise.
        /// </summary>
        public static (float Radius, float Alpha) KeyClick(float seconds)
        {
            float k = (seconds - (ShackleOpenAt - 0.02f)) / 0.25f;
            if (seconds < 0f || k < 0f || k >= 1f)
            {
                return (0f, 0f);
            }

            return (0.2f + (0.5f * (1f - ((1f - k) * (1f - k)))), 0.9f * (1f - k) * (1f - k));
        }

        /// <summary>
        /// The padlock <paramref name="seconds"/> after its <c>latchOpen</c> (-1: closed): whether its shackle is open, how far
        /// it has dropped (a share of its side), its scale and opacity: a small jolt as the key arrives, the shackle springs
        /// open at <see cref="ShackleOpenAt"/> with a swell, then it drops and fades from <see cref="PadlockDropAt"/> to
        /// <see cref="LatchOpenSeconds"/>.
        /// </summary>
        public static (bool Open, float Drop, float Scale, float Alpha) PadlockOpen(float seconds)
        {
            if (seconds < 0f)
            {
                return (false, 0f, 1f, 1f);
            }

            float scale = 1f;
            float jolt = (seconds - KeyFlightSeconds) / 0.08f;
            if (jolt >= 0f && jolt < 1f)
            {
                scale += 0.05f * (float)Math.Sin(Math.PI * jolt);
            }

            float swell = (seconds - ShackleOpenAt) / 0.12f;
            if (swell >= 0f && swell < 1f)
            {
                scale += 0.15f * (float)Math.Sin(Math.PI * swell);
            }

            float fall = Clamp01((seconds - PadlockDropAt) / (LatchOpenSeconds - PadlockDropAt));
            return (seconds >= ShackleOpenAt, 0.5f * fall * fall, scale, 1f - (fall * fall));
        }

        /// <summary>
        /// The hero <paramref name="seconds"/> after his latch opened (-1: closed): how far he has stepped aside (place widths
        /// toward <paramref name="side"/>, -1 left or 1 right), his little hop (place heights, up) and his opacity: he stays
        /// until <see cref="HeroLeaveAt"/>, then steps out of the place and fades by <see cref="LatchOpenSeconds"/>, the pod
        /// behind him.
        /// </summary>
        public static (float X, float Y, float Alpha) HeroStep(float seconds, int side)
        {
            float f = Clamp01((seconds - HeroLeaveAt) / (LatchOpenSeconds - HeroLeaveAt));
            if (seconds < 0f || f <= 0f)
            {
                return (0f, 0f, 1f);
            }

            float e = f * f * (3f - (2f * f));
            return (side * 0.6f * e, 0.08f * (float)Math.Sin(Math.PI * f), 1f - e);
        }

        /// <summary>The side a hero in column <paramref name="column"/> of <paramref name="columns"/> steps to: outward, toward the nearer edge.</summary>
        public static int HeroSide(int column, int columns) => (column * 2) + 1 < columns ? -1 : 1;

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

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
