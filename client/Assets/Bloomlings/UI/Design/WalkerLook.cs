using System;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// How a round level's clearing walkers are drawn (presentation only; the rules, their times and every outcome stay): their
    /// size, outline, contact shadow, body shade, Munchers' act (chew the cube, or carry it home) and, for some families, the
    /// drawn running creature instead of the family's flat picture. <see cref="Default"/> is the game's look (FR-048 and the
    /// owner's sizes of 2026-10-31 and 2026-11-02) and changes nothing; <see cref="Current"/> is what the hosts draw. The
    /// preview's walker mock (the owner's question of 2026-11-03: the walkers cannot be seen breaking the cubes on small
    /// cubes) sets <see cref="Contrast"/> or <see cref="Creature"/>; making one the game's look is changing
    /// <see cref="Current"/>'s default (and Unity's <c>ClearFxView</c> drawing the outline, shade and creature). Engine-free.
    /// </summary>
    public sealed class WalkerLook
    {
        public WalkerLook(
            float boardShare = 0f,
            float outline = 0f,
            float shadowAlpha = DefaultShadowAlpha,
            float shade = 0f,
            bool carry = false,
            Family[]? creatures = null,
            string name = "default",
            bool hands = false,
            bool heroes = false,
            float linger = ClearStyles.LiveLinger)
        {
            Hands = hands;
            Heroes = heroes;
            Linger = linger;
            BoardShare = boardShare;
            Outline = outline;
            ShadowAlpha = shadowAlpha;
            Shade = shade;
            Carry = carry;
            CreatureFamilies = creatures ?? Array.Empty<Family>();
            Name = name;
        }

        /// <summary>The walkers' own flat shadow's opacity (<see cref="FxList.Shadow"/>).</summary>
        public const float DefaultShadowAlpha = 0.13f;

        /// <summary>
        /// The mock's walker box as a share of the board surface's width, whatever its cubes (the reference game's ants are about
        /// 7.5% of its board box long and 4% wide, about 6% for a round figure of the same area).
        /// </summary>
        public const float MockBoardShare = 0.06f;

        /// <summary>The mock's dark outline, a share of the walker's box (at least a pixel and a half).</summary>
        public const float MockOutline = 0.035f;

        /// <summary>The mock's contact shadow's opacity under the walker (on the board and on the cubes).</summary>
        public const float MockShadowAlpha = 0.34f;

        /// <summary>
        /// The mock's body shade: a light material's walker is this much darker than its cubes, a dark one this much lighter, so it
        /// never hides on cubes of its own colour.
        /// </summary>
        public const float MockShade = 0.25f;

        /// <summary>The run cycle's frames of the drawn creature (<see cref="UiRaster.Creature"/>).</summary>
        public const int RunFrames = 8;

        /// <summary>A posed walker standing seen from the side (<see cref="FxItem.Pose"/>; 0 to <see cref="RunFrames"/> − 1 are the run).</summary>
        public const int PoseSide = RunFrames;

        /// <summary>A posed walker standing facing the player, arms down.</summary>
        public const int PoseFront = RunFrames + 1;

        /// <summary>A posed walker facing the player holding a cube in front of its belly, arms forward (its hands drawn by <see cref="PoseHands"/>).</summary>
        public const int PoseHold = RunFrames + 2;

        /// <summary>Only the hands of <see cref="PoseHold"/>, drawn over the held cube.</summary>
        public const int PoseHands = RunFrames + 3;

        /// <summary>
        /// The "in hands" looks (the owner, 2026-11-03: small cubes make poor animations, but once a hero takes one in his hands it is
        /// big and nice to watch): the held cube's side as a share of the walker's box.
        /// </summary>
        public const float HeldShare = 0.65f;

        /// <summary>How far above the walker's feet the held cube's middle is, in walker boxes (its bottom a little in front of the feet, so the face shows over it).</summary>
        public const float HeldUp = (HeldShare / 2f) - 0.05f;

        /// <summary>
        /// The "in hands" looks' act after the rules' clear, seconds (presentation only, like <see cref="ClearStyles.LiveLinger"/>): the
        /// act runs from the arrival to the clear plus this (about 1.1 s), long enough for a gag.
        /// </summary>
        public const float HandsLinger = 1f;

        /// <summary>Within an "in hands" act (0–1): the cube jumps into the hands, growing, until this; the gag follows.</summary>
        public const float JumpEnd = 0.2f;

        /// <summary>Within an "in hands" act: the gag is over by this; the walker then turns for home empty-handed.</summary>
        public const float GagEnd = 0.92f;

        /// <summary>A real hero walker's cell height (<see cref="HeroMotion.CellHeight"/>) as a share of the walker's box: upright, a little taller than the round figures.</summary>
        public const float HeroCellScale = 1.45f;

        /// <summary>
        /// How much bigger than the walker's box the creature's picture is drawn (on the same feet): its body fills less of its
        /// picture than the family pictures do, so it matches their size.
        /// </summary>
        public const float CreatureScale = 1.3f;

        /// <summary>Route cells a creature runs in one stride (its run cycle).</summary>
        public const float CellsPerStride = 2.6f;

        /// <summary>
        /// How far before its cube's center a carrying walker stops, in cells at most (and a third of its own size): it stands at
        /// the cube instead of on it, so the cube is seen being lifted.
        /// </summary>
        public const float CarryStopCells = 0.9f;

        /// <summary>
        /// The carried cube's least size as a share of the walker's box (the owner, 2026-11-03: a carried cube never gets smaller
        /// than the board's cube, and on small cubes it grows so it is seen): its size is the larger of the board's cube and this
        /// (about 26 px at a 53 px walker; the reference game's carried pieces are about 0.4 of its ants' length, 0.75 of their
        /// width). Drawn as the board's own cube picture.
        /// </summary>
        public const float CarriedMin = 0.5f;

        /// <summary>How deep the carried cube's bottom rests into the walker's head, a share of the walker's box.</summary>
        public const float CarriedSink = 0.1f;

        /// <summary>Within the act (0–1): the walker grabs the cube until <see cref="GrabEnd"/>, it flies onto its head until <see cref="LiftEnd"/>, then the walker turns for home.</summary>
        public const float GrabEnd = 0.22f;

        /// <inheritdoc cref="GrabEnd"/>
        public const float LiftEnd = 0.62f;

        /// <summary>The game's look: every walker as before.</summary>
        public static WalkerLook Default { get; } = new WalkerLook();

        /// <summary>
        /// The mock's proposal 1 + 2 + 3: a fixed size on screen (<see cref="MockBoardShare"/>), a dark outline, a contact shadow,
        /// a shade off its cubes' colour, and Munchers carrying the cube home on its head instead of chewing it.
        /// </summary>
        public static WalkerLook Contrast { get; } = new WalkerLook(MockBoardShare, MockOutline, MockShadowAlpha, MockShade, true, null, "contrast");

        /// <summary>
        /// The "in hands" mock with the drawn creature for all four families (arms and legs): the walker takes its cube in its
        /// hands, where it grows, plays a gag with it (Munchers) or its style's flavour, and runs home empty-handed.
        /// </summary>
        public static WalkerLook HandsCreature { get; } = new WalkerLook(MockBoardShare, MockOutline, MockShadowAlpha, MockShade, false,
            new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig }, "hands-creature", hands: true, linger: HandsLinger);

        /// <summary>
        /// The "in hands" mock with the owner's real modular V2 heroes as small walkers (an experiment for comparison only: hero
        /// frames inside a level as walkers would need the constitution's principle VII amended, which allows them only as a
        /// mechanic's figure).
        /// </summary>
        public static WalkerLook HandsHeroes { get; } = new WalkerLook(MockBoardShare, 0f, MockShadowAlpha, 0f, false, null, "hands-heroes",
            hands: true, heroes: true, linger: HandsLinger);

        /// <summary>
        /// The mock's variant 4: <see cref="Contrast"/> with the Sprig family's walkers a drawn running creature (a leaf with its
        /// veins), and the Drop family's the same body as a glossy drop, so that it shows on L1 (no Sprig cube) too.
        /// </summary>
        public static WalkerLook Creature { get; } = new WalkerLook(MockBoardShare, MockOutline, MockShadowAlpha, MockShade, true, new[] { Family.Sprig, Family.Drop }, "creature");

        /// <summary>The look the hosts draw now: <see cref="Default"/> in the game; the preview's walker mock sets another.</summary>
        public static WalkerLook Current
        {
            get => _current;
            set
            {
                _current = value ?? Default;
                ClearStyles.Linger = _current.Linger;
            }
        }

        private static WalkerLook _current = Default;

        /// <summary>The walker box as a share of the board surface's width, or 0 for the size by cubes (<see cref="ClearStyles.WalkerCubes"/>).</summary>
        public float BoardShare { get; }

        /// <summary>The dark outline round the walker, a share of its box (0: none).</summary>
        public float Outline { get; }

        /// <summary>The shadow's opacity under the walker.</summary>
        public float ShadowAlpha { get; }

        /// <summary>How far the walker's body is shaded off its cubes' colour (0: as drawn).</summary>
        public float Shade { get; }

        /// <summary>Munchers carries the cube home on its head instead of chewing it (round levels).</summary>
        public bool Carry { get; }

        /// <summary>The families whose walkers are the drawn running creature (<see cref="UiRaster.Creature"/>); none in the game.</summary>
        public System.Collections.Generic.IReadOnlyList<Family> CreatureFamilies { get; }

        public string Name { get; }

        /// <summary>The "in hands" act (<see cref="HandsCreature"/>, <see cref="HandsHeroes"/>).</summary>
        public bool Hands { get; }

        /// <summary>The walkers are the owner's real modular V2 heroes (<see cref="HandsHeroes"/>).</summary>
        public bool Heroes { get; }

        /// <summary>How long a walker's act lasts after the rules' clear (presentation only; <see cref="ClearStyles.LiveLinger"/> in the game).</summary>
        public float Linger { get; }

        public bool IsDefault => ReferenceEquals(this, Default);

        /// <summary>A round walker's size in cubes when <paramref name="cubesAcross"/> cubes span the board surface.</summary>
        public float WalkerCubes(float cubesAcross) => BoardShare > 0f ? BoardShare * cubesAcross : ClearStyles.WalkerCubes(cubesAcross);

        /// <summary>Whether walkers of <paramref name="material"/> (a round material's index) are the drawn creature.</summary>
        public bool DrawsCreature(int material) =>
            material >= 0 && material < RoundMaterial.All.Count && DrawsCreature(RoundMaterial.All[material].Family);

        /// <summary>Whether walkers of <paramref name="family"/> are the drawn creature.</summary>
        public bool DrawsCreature(Family family)
        {
            foreach (Family f in CreatureFamilies)
            {
                if (f == family)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The colour a walker's body is shaded with, and how much: black over a light material, white over a dark one.</summary>
        public (Rgba Tint, float Amount) ShadeOf(Rgba material)
        {
            bool light = Luma(material) > 0.56f;
            return (light ? Rgba.Black : Rgba.White, light ? Shade : Shade * 1.2f);
        }

        /// <summary>A material's colour shaded off itself (<see cref="ShadeOf"/>).</summary>
        public Rgba BodyOf(Rgba material)
        {
            (Rgba tint, float amount) = ShadeOf(material);
            return material.Mix(tint, amount);
        }

        /// <summary>The run frame for a walker <paramref name="cells"/> route cells along its way (0 to <see cref="RunFrames"/> − 1).</summary>
        public static int RunFrame(float cells)
        {
            float k = cells / CellsPerStride;
            k -= (float)Math.Floor(k);
            return Math.Min(RunFrames - 1, (int)(k * RunFrames));
        }

        private static float Luma(Rgba c) => ((0.299f * c.R) + (0.587f * c.G) + (0.114f * c.B)) / 255f;
    }
}
