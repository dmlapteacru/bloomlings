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
            string name = "default")
        {
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

        /// <summary>The carried cube's size as a share of the walker, at least and at most (else the board's cube).</summary>
        public const float CarriedMin = 0.36f;

        /// <inheritdoc cref="CarriedMin"/>
        public const float CarriedMax = 0.52f;

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
        /// The mock's variant 4: <see cref="Contrast"/> with the Sprig family's walkers a drawn running creature (a leaf with its
        /// veins), and the Drop family's the same body as a glossy drop, so that it shows on L1 (no Sprig cube) too.
        /// </summary>
        public static WalkerLook Creature { get; } = new WalkerLook(MockBoardShare, MockOutline, MockShadowAlpha, MockShade, true, new[] { Family.Sprig, Family.Drop }, "creature");

        /// <summary>The look the hosts draw now: <see cref="Default"/> in the game; the preview's walker mock sets another.</summary>
        public static WalkerLook Current { get; set; } = Default;

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
