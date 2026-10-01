using System;
using System.Collections.Generic;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using G = Bloomlings.Client.UI.Design.DesignTokens.Garden;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The shades of one element color (spec 003 FR-008, data-model.md "ColorSet"): the face, its lighter top, the darker
    /// lip along its bottom edge, and the outline (never pure black).
    /// </summary>
    public sealed record ColorSet(string Name, Rgba Face, Rgba Top, Rgba Lip, Rgba Line)
    {
        /// <summary>A set derived from one base color: top lighten 0.18, lip darken 0.25, line darken 0.42.</summary>
        public static ColorSet From(string name, Rgba face) => new ColorSet(name, face, face.Lighten(0.18f), face.Darken(0.25f), face.Darken(0.42f));

        /// <summary>The greyed set of a disabled element (FR-012): the same shapes, no color.</summary>
        public ColorSet Disabled() => new ColorSet(Name + ".disabled", Face.Grey().Lighten(0.2f), Top.Grey().Lighten(0.2f), Lip.Grey().Lighten(0.1f), Line.Grey());
    }

    /// <summary>
    /// How a label is drawn (spec 003 FR-009, FR-025, data-model.md "TextLook"): a vertical gradient fill, an outline,
    /// an extrusion below the letters and a soft shadow, all in fractions of the font size. <see cref="Emboss"/> is the
    /// light line under dark labels on cream faces.
    /// </summary>
    public sealed record TextLook(Rgba FillTop, Rgba FillBottom, Rgba Outline, float OutlineEm, float ExtrudeEm, float ShadowAlpha, Rgba? Emboss = null)
    {
        /// <summary>A label on a colored face: a cream-white gradient, outlined and extruded in the set's line.</summary>
        public static TextLook OnColor(ColorSet set) =>
            new TextLook(C.GardenLabelFillTop, C.GardenLabelFillBottom, set.Line, G.LabelOutlineEm, G.LabelExtrudeEm, G.LabelShadowAlpha);

        /// <summary>A label on a cream or white face: one dark color, no outline, no extrusion, a light emboss below.</summary>
        public static TextLook Plain(Rgba color) => new TextLook(color, color, color, 0f, 0f, 0f, Rgba.White.WithAlpha(0.7f));

        /// <summary>A headline over the garden backdrop (Home's "Level N"): cream-white letters outlined in dark brown.</summary>
        public static TextLook Headline => new TextLook(C.GardenLabelFillTop, C.GardenLabelFillBottom, C.GardenLabelPlain, G.LabelOutlineEm, G.LabelExtrudeEm, G.LabelShadowAlpha);

        /// <summary>Whether the look draws an outline and an extrusion.</summary>
        public bool Volumetric => OutlineEm > 0f || ExtrudeEm > 0f;
    }

    /// <summary>What a booster tile shows (spec 003 FR-031, contracts/booster-tile.md, data-model.md "BoosterTileState").</summary>
    public readonly struct BoosterTileState
    {
        public BoosterTileState(int charges, int price, bool selected, bool usable, bool affordable)
        {
            Charges = charges;
            Price = price;
            Selected = selected;
            Usable = usable;
            Affordable = affordable;
        }

        public int Charges { get; }

        public int Price { get; }

        /// <summary>The level waits for this booster's target (Return, Bloom Burst only).</summary>
        public bool Selected { get; }

        /// <summary>The booster can have an effect now (spec 001 FR-046).</summary>
        public bool Usable { get; }

        /// <summary>Charges are left, or the Petals cover the price.</summary>
        public bool Affordable { get; }

        /// <summary>The "×N" badge shows.</summary>
        public bool ShowsCharges => Charges > 0;

        /// <summary>The price tag and the green "+" show.</summary>
        public bool ShowsPrice => Charges <= 0;

        /// <summary>Greyed and not pressable; a selected tile is never disabled.</summary>
        public bool Disabled => !Selected && (!Usable || (Charges <= 0 && !Affordable));
    }

    /// <summary>One part of the leaves-and-flower decoration (FR-011a), drawn back to front.</summary>
    public enum DecorationPart
    {
        LeafA,
        LeafB,
        LeafC,
        Petals,
        Center,
    }

    /// <summary>
    /// The Garden look's engine-free rules, shared by the Unity client and the playtest (spec 003, data-model.md): the
    /// named color sets, the press, breathe, count-up and glow curves, and where the decoration goes.
    /// </summary>
    public static class GardenLook
    {
        /// <summary>The primary button (PLAY, NEXT, RESUME, CLAIM, CONTINUE), the selected tab and the "+".</summary>
        public static readonly ColorSet Green = ColorSet.From("set.green", C.ButtonPrimary);

        /// <summary>The secondary buttons: cream with the brown garden outline.</summary>
        public static readonly ColorSet Cream = new ColorSet("set.cream", Rgba.FromHex("#F7EDD6"), Rgba.FromHex("#FFF9EC"), Rgba.FromHex("#D8C29A"), C.GardenOutline);

        /// <summary>The round icon buttons and the Petals pill.</summary>
        public static readonly ColorSet White = new ColorSet("set.white", Rgba.FromHex("#F4EFE4"), Rgba.FromHex("#FFFFFF"), Rgba.FromHex("#CFC6B4"), Rgba.FromHex("#7A6E58"));

        /// <summary>The level pill and the pause header.</summary>
        public static readonly ColorSet Blue = ColorSet.From("set.blue", C.PillLevel);

        /// <summary>The level pill of a Super Hard level.</summary>
        public static readonly ColorSet Lilac = ColorSet.From("set.lilac", C.PillLevelSuperHard);

        /// <summary>The 2× pill.</summary>
        public static readonly ColorSet Dark = ColorSet.From("set.dark", C.ButtonDark);

        /// <summary>The close button and HARD.</summary>
        public static readonly ColorSet Red = ColorSet.From("set.red", C.BadgeHard);

        /// <summary>SUPER HARD and its level pill.</summary>
        public static readonly ColorSet Purple = ColorSet.From("set.purple", C.BadgeSuperHard);

        /// <summary>Every named set (tests and docs).</summary>
        public static IReadOnlyList<ColorSet> Sets { get; } = new[]
        {
            Green, Cream, White, Blue, Lilac, Dark, Red, Purple,
            Booster("extra_slot"), Booster("shuffle"), Booster("return"), Booster("bloom_burst"),
        };

        /// <summary>The colored sets, whose labels use <see cref="TextLook.OnColor"/>.</summary>
        public static IReadOnlyList<ColorSet> ColoredSets { get; } = new[]
        {
            Green, Blue, Lilac, Dark, Red, Purple,
            Booster("extra_slot"), Booster("shuffle"), Booster("return"), Booster("bloom_burst"),
        };

        /// <summary>A booster tile's set, from its spec 002 color.</summary>
        public static ColorSet Booster(string boosterId) => ColorSet.From("set.booster." + boosterId, DesignTokens.BoosterColor(boosterId));

        /// <summary>The label look on a set's face: plain dark brown on cream and white, volumetric on colors.</summary>
        public static TextLook LabelOn(ColorSet set) =>
            ReferenceEquals(set, Cream) || ReferenceEquals(set, White) || set.Name.StartsWith("set.cream", StringComparison.Ordinal) || set.Name.StartsWith("set.white", StringComparison.Ordinal)
                ? TextLook.Plain(C.GardenLabelPlain)
                : TextLook.OnColor(set);

        /// <summary>The glyph color on a set's face (FR-010): light on colors, dark on cream and white.</summary>
        public static Rgba GlyphOn(ColorSet set) => LabelOn(set).Volumetric ? Rgba.FromHex("#FFFBEF") : Rgba.FromHex("#4A4436");

        // ---- Motion ----

        /// <summary>
        /// How deep a button is pressed (FR-017): 1 while the finger is down; after release it springs back to 0 in
        /// <c>motion.press</c> with one overshoot below 0, then rests. <paramref name="sinceRelease"/> is in seconds;
        /// a negative value means never released.
        /// </summary>
        public static float PressDepth(bool down, float sinceRelease)
        {
            if (down)
            {
                return 1f;
            }

            float seconds = DesignTokens.Motion.Press.Seconds;
            if (sinceRelease < 0f || sinceRelease >= seconds)
            {
                return 0f;
            }

            // 1 − easeOutBack: falls from 1 past 0 to about −0.1, then settles at 0.
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = sinceRelease / seconds;
            float back = 1f + (c3 * (float)Math.Pow(x - 1f, 3)) + (c1 * (float)Math.Pow(x - 1f, 2));
            return 1f - back;
        }

        /// <summary>The squash of a pressed element (FR-017): wider and lower by the press depth.</summary>
        public static (float ScaleX, float ScaleY) Squash(float depth, bool tile = false) =>
            tile ? (1f + (0.05f * depth), 1f - (0.07f * depth)) : (1f + (0.03f * depth), 1f - (0.05f * depth));

        /// <summary>The idle breath of the one waiting button (FR-019): 1 → <c>motion.breathe</c> scale and back.</summary>
        public static float Breathe(float seconds)
        {
            MotionToken breathe = DesignTokens.Motion.Breathe;
            double phase = 2.0 * Math.PI * seconds / breathe.Seconds;
            return 1f + ((breathe.Scale - 1f) * (float)(0.5 - (0.5 * Math.Cos(phase))));
        }

        /// <summary>The amount shown while earned Petals count up (FR-020): from 0 to the amount, easing out.</summary>
        public static long CountUp(long amount, float seconds)
        {
            float k = Math.Max(0f, Math.Min(1f, seconds / DesignTokens.Motion.CountUp.Seconds));
            float eased = 1f - ((1f - k) * (1f - k) * (1f - k));
            return (long)Math.Round(amount * eased);
        }

        /// <summary>The selected booster's glow alpha (FR-031): 55% to 100% every <c>motion.glow</c>.</summary>
        public static float Glow(float seconds)
        {
            double phase = 2.0 * Math.PI * seconds / DesignTokens.Motion.Glow.Seconds;
            return 0.775f + (0.225f * (float)Math.Sin(phase));
        }

        // ---- Decoration (FR-011a) ----

        /// <summary>
        /// Where the leaves and flower go on a main button: a cluster over its top-left corner and a smaller one, turned
        /// half way, over its bottom-right corner. Each box is square; the shape fills its middle 100/120 rows.
        /// </summary>
        public static (Box TopLeft, Box BottomRight) DecorationBoxes(Box button)
        {
            float h = button.Height;
            float big = h * G.DecorationSize;
            float small = big * 0.9f;
            var topLeft = new Box(button.Left - (h * 0.24f), button.Top - (h * 0.28f), button.Left - (h * 0.24f) + big, button.Top - (h * 0.28f) + big);
            var bottomRight = new Box(button.Right + (h * 0.2f) - small, button.Bottom + (h * 0.24f) - small, button.Right + (h * 0.2f), button.Bottom + (h * 0.24f));
            return (topLeft, bottomRight);
        }

        /// <summary>The fill and outline colors of a decoration part.</summary>
        public static (Rgba Fill, Rgba Line) DecorationColors(DecorationPart part) => part switch
        {
            DecorationPart.LeafA => (C.GardenLeaf1, C.GardenLeafLine),
            DecorationPart.LeafB => (C.GardenLeaf2, C.GardenLeafLine),
            DecorationPart.LeafC => (C.GardenLeaf3, C.GardenLeafLine),
            DecorationPart.Petals => (C.GardenFlower, C.GardenFlowerLine),
            _ => (C.GardenFlowerCenter, C.GardenFlowerCenterLine),
        };

        /// <summary>Every part, back to front.</summary>
        public static IReadOnlyList<DecorationPart> DecorationParts { get; } = new[]
        {
            DecorationPart.LeafA, DecorationPart.LeafB, DecorationPart.LeafC, DecorationPart.Petals, DecorationPart.Center,
        };
    }
}
