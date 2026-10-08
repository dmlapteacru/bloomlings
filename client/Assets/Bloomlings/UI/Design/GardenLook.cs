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

        /// <summary>
        /// A label on a glossy face (spec 005 §3.3: the primary button, the jam choices): <see cref="OnColor"/> with a
        /// lighter extrusion (60%), so the label reads crisp on the satin gloss.
        /// </summary>
        public static TextLook OnGloss(ColorSet set) => OnColor(set) with { ExtrudeEm = G.LabelExtrudeEm * 0.6f };

        /// <summary>Whether the look draws an outline and an extrusion.</summary>
        public bool Volumetric => OutlineEm > 0f || ExtrudeEm > 0f;
    }

    /// <summary>
    /// One layer of a multi-part icon (spec 005 contracts/look.md §3.4, §3.8): the <see cref="ShapeLibrary"/> shape
    /// <see cref="ShapeId"/> in <see cref="Fill"/>, over its outline, which is the same shape grown by <see cref="Grow"/>
    /// shape units in <see cref="Line"/> (no outline when <see cref="Line"/> is null). Parts are drawn in list order.
    /// </summary>
    public sealed record IconPart(string ShapeId, Rgba Fill, Rgba? Line = null, float Grow = 0f);

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

    /// <summary>The leaves on a wooden sign's ends (spec 005 contracts/look.md §3.2).</summary>
    public enum SignDecor
    {
        /// <summary>A plain plank: the Home level plaque, card headers.</summary>
        None,

        /// <summary>Clover-like ivy over both ends: the gameplay level sign, the Wardrobe and Store banners.</summary>
        Ivy,

        /// <summary>Big leaves and a white flower at the top-left and bottom-right ends: the win sign.</summary>
        Flowers,
    }

    /// <summary>What a cost pill shows (spec 005 contracts/look.md §3.4).</summary>
    public enum CostKind
    {
        /// <summary>The lotus and a price in Petals.</summary>
        Petals,

        /// <summary>A green ▶ square and "Free" (a rescue or a rewarded choice).</summary>
        Free,

        /// <summary>"×N": the charges the player owns.</summary>
        Charges,
    }

    /// <summary>The contents of a cost pill: a price, Free, or a number of charges (spec 005 contracts/look.md §3.4).</summary>
    public readonly struct Cost
    {
        private Cost(CostKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }

        public CostKind Kind { get; }

        /// <summary>The price in Petals or the number of charges; 0 for <see cref="CostKind.Free"/>.</summary>
        public int Amount { get; }

        /// <summary>A free choice (▶ Free).</summary>
        public static Cost Free => new Cost(CostKind.Free, 0);

        /// <summary>A price in Petals, shown with the lotus.</summary>
        public static Cost Petals(int price) => new Cost(CostKind.Petals, price);

        /// <summary>Charges the player owns, shown as "×N".</summary>
        public static Cost Charges(int count) => new Cost(CostKind.Charges, count);
    }

    /// <summary>
    /// What a Waiting Slot plate shows (spec 005 contracts/look.md §3.7; the states of spec 002 FR-013 and spec 003
    /// FR-022a). The extra slot's green "+" is a mark on any of them.
    /// </summary>
    public enum SlotPlateState
    {
        /// <summary>A free slot: the dashed inner outline on a slightly sunk face.</summary>
        Empty,

        /// <summary>A pod whose Bloomlings work: the sticker tile and its count.</summary>
        Working,

        /// <summary>A pod that waits: the tile in grey with the hourglass badge.</summary>
        Stuck,

        /// <summary>The last free usable slot: the dashed outline in <c>state.danger</c> with "!".</summary>
        Danger,

        /// <summary>A locked slot: a grey face with the padlock.</summary>
        Locked,
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
        /// <summary>
        /// The primary button (PLAY, NEXT, RESUME, CLAIM, CONTINUE), the selected tab and the "+": the reference's green
        /// with its explicit shades (spec 005 contracts/look.md §1.3).
        /// </summary>
        public static readonly ColorSet Green = new ColorSet("set.green", C.ButtonPrimary, C.ButtonPrimaryTop, C.ButtonPrimaryEdge, Rgba.FromHex("#24661A"));

        /// <summary>The secondary buttons, slots and booster tiles: the reference's cream with a brown outline (spec 005 §1.3).</summary>
        public static readonly ColorSet Cream = new ColorSet("set.cream", C.CreamFace, C.CreamTop, C.CreamLip, C.CreamLine);

        /// <summary>
        /// The round icon buttons and the Petals pill: since spec 005 the same cream as <see cref="Cream"/>, kept under its
        /// own name so callers and <see cref="LabelOn"/> stay as they are.
        /// </summary>
        public static readonly ColorSet White = new ColorSet("set.white", C.CreamFace, C.CreamTop, C.CreamLip, C.CreamLine);

        /// <summary>The jam's blue choices (Return, Bloom Burst; spec 005 §1.3). The gameplay level pill becomes a wooden sign.</summary>
        public static readonly ColorSet Blue = new ColorSet("set.blue", C.ButtonBlue, C.ButtonBlue.Lighten(0.3f), C.ButtonBlue.Darken(0.25f), C.ButtonBlue.Darken(0.42f));

        /// <summary>The orange buttons: a highlighted secondary call to action ("Next" in the reference strip, spec 005 §1.3).</summary>
        public static readonly ColorSet Orange = new ColorSet("set.orange", C.ButtonOrange, C.ButtonOrange.Lighten(0.3f), C.ButtonOrange.Darken(0.22f), C.ButtonOrange.Darken(0.42f));

        /// <summary>
        /// How faint the owner's booster icon picture is on a disabled tile (spec 005 pictures.md D: a picture is not
        /// greyed, it fades).
        /// </summary>
        public const float PictureDisabledAlpha = 0.45f;

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
            Green, Cream, White, Blue, Orange, Lilac, Dark, Red, Purple,
            Booster("extra_slot"), Booster("shuffle"), Booster("return"), Booster("bloom_burst"),
        };

        /// <summary>The colored sets, whose labels use <see cref="TextLook.OnColor"/>.</summary>
        public static IReadOnlyList<ColorSet> ColoredSets { get; } = new[]
        {
            Green, Blue, Orange, Lilac, Dark, Red, Purple,
            Booster("extra_slot"), Booster("shuffle"), Booster("return"), Booster("bloom_burst"),
        };

        /// <summary>A booster tile's set, from its spec 002 color.</summary>
        public static ColorSet Booster(string boosterId) => ColorSet.From("set.booster." + boosterId, DesignTokens.BoosterColor(boosterId));

        /// <summary>The label look on a set's face: plain <c>ink.brown</c> on cream and white (spec 005 §1.3), volumetric on colors.</summary>
        public static TextLook LabelOn(ColorSet set) =>
            ReferenceEquals(set, Cream) || ReferenceEquals(set, White) || set.Name.StartsWith("set.cream", StringComparison.Ordinal) || set.Name.StartsWith("set.white", StringComparison.Ordinal)
                ? TextLook.Plain(C.InkBrown)
                : TextLook.OnColor(set);

        /// <summary>The glyph color on a set's face (FR-010): light on colors, <c>ink.brown</c> on cream and white (spec 005 §3.3).</summary>
        public static Rgba GlyphOn(ColorSet set) => LabelOn(set).Volumetric ? Rgba.FromHex("#FFFBEF") : C.InkBrown;

        // ---- Materials and labels of the reference look (spec 005 contracts/look.md §3) ----

        /// <summary>
        /// The letters of a wooden sign (§3.2): one dark color, no outline, with a light emboss line under them as if cut
        /// into the plank. Signs pass <c>ink.brown</c>; a Super Hard level's sign passes <c>badge.super_hard</c>.
        /// </summary>
        public static TextLook SignLetters(Rgba ink) => new TextLook(ink, ink, ink, 0f, 0f, 0f, C.WoodLight.Lighten(0.4f));

        /// <summary>
        /// The wooden wordmark's letters (§4.5): a pale cream-yellow wood fill, from <c>#FFF0C8</c> to a honey
        /// <c>#E9B874</c>, outlined in <c>wood.line</c> with a darker extrusion.
        /// </summary>
        public static TextLook WoodLetters { get; } = new TextLook(Rgba.FromHex("#FFF0C8"), Rgba.FromHex("#E9B874"), C.WoodLine, 0.05f, 0.11f, 0.32f);

        /// <summary>The booster tile's cream-white bezel with a faint silver tint (§3.7).</summary>
        public static Rgba BoosterRim => C.CreamTop.Mix(C.StateStuck, 0.25f);

        /// <summary>The booster tile's lower lip under its bezel: cream, faintly silver, never a grey band (§3.7).</summary>
        public static Rgba BoosterLip => C.CreamLip.Mix(C.StateStuck, 0.3f);

        /// <summary>The booster tile's soft tan outline (§3.7).</summary>
        public static Rgba BoosterLine => C.CreamLine.Mix(C.StateStuck, 0.35f).Darken(0.1f);

        /// <summary>
        /// A special's candy block color (§4.1): its <c>special.*</c> token made vivid (twice as far from its own grey) and
        /// lightened 0.05, so the block reads as candy-bright beside the tiles.
        /// </summary>
        public static Rgba SpecialFace(Rgba token)
        {
            Rgba grey = token.Grey();
            byte Push(byte c, byte g) => (byte)Math.Max(0, Math.Min(255, c + (c - g)));
            return new Rgba(Push(token.R, grey.R), Push(token.G, grey.G), Push(token.B, grey.B), token.A).Lighten(0.05f);
        }

        /// <summary>The dark lines between a board's tiles and around them, inside the stone border (§3.6).</summary>
        public static Rgba BoardGap => C.LawnDark.Darken(0.55f);

        /// <summary>A small pink flower over the wordmark (§4.5): petals and center.</summary>
        public static (Rgba Petals, Rgba Line, Rgba Center) PinkFlower => (C.LotusFill, C.LotusLine, C.GardenFlowerCenter);

        /// <summary>
        /// An ivy leaf's fill on a sign (§3.9): <c>ivy.leaf</c>, a little lighter or darker by its place in the cluster so
        /// the leaves read apart.
        /// </summary>
        public static Rgba IvyShade(int leaf) => (leaf % 3) switch
        {
            0 => C.IvyLeaf,
            1 => C.IvyLeaf.Darken(0.2f),
            _ => C.IvyLeaf.Lighten(0.2f),
        };

        /// <summary>The falling petals' colors (§3.9), alternating.</summary>
        public static Rgba PetalShade(int petal) => petal % 2 == 0 ? C.LotusFill : C.LotusTip;

        // ---- Icons (spec 005 contracts/look.md §3.4, §3.8) ----

        /// <summary>
        /// The outline of the round and squircle icon buttons, the speed pill and the booster tiles, as a share of their
        /// shorter side (the owner, 2026-10-06: thicker, so the round buttons stand out; it was 2% on cream sets and 2.4%
        /// on colored ones).
        /// </summary>
        public const float IconLineCream = 0.045f;

        public const float IconLineColored = 0.04f;

        /// <summary>
        /// The corner radius of every icon button, of the speed pill (of its height) and of the profile avatar, as a share
        /// of the shorter side (the owner, 2026-10-06: "our layout and main buttons are rectangular", so every round button
        /// and the avatar became a rounded square; they were circles, and only Pause a squircle).
        /// </summary>
        public const float IconRadiusShare = 0.34f;

        /// <summary>
        /// The border of an icon button's wooden plate, of the speed pill's and of the profile avatar's, as a share of the
        /// shorter side (the owner, 2026-10-06: a bigger border, like Play's wood rim; a plate whose border rounds over since
        /// spec 005 FR-044, <see cref="UiRaster.RaisedPlate"/>); the raised face fills the rest.
        /// </summary>
        public const float IconRimShare = 0.1f;

        /// <summary>The share of an icon button its glyph keeps inside the rim (the glyphs were sized to the whole button).</summary>
        public const float IconRimGlyph = 0.88f;

        /// <summary>The cushion's shorter side inside the rim, as a share of the button's.</summary>
        public const float IconRimFaceShare = 1f - (2f * IconRimShare);

        /// <summary>
        /// A glyph's share of the cushion inside the rim for the glyph share it had of the whole button: the glyph keeps
        /// <see cref="IconRimGlyph"/> of its size (Unity sizes glyphs from the cushion's side).
        /// </summary>
        public const float IconRimGlyphOfFace = IconRimGlyph / IconRimFaceShare;

        /// <summary>An icon button's corner radius: <see cref="IconRadiusShare"/> of its shorter side.</summary>
        public static float IconRadius(Box box) => Math.Min(box.Width, box.Height) * IconRadiusShare;

        /// <summary>The cushion inside an icon button's wood rim: the box less <see cref="IconRimShare"/> of its shorter side all round.</summary>
        public static Box IconRimFace(Box box) => box.Inset(Math.Min(box.Width, box.Height) * IconRimShare);

        /// <summary>
        /// The corner radius of the cushion inside a rim, from the cushion's box: the rim's corners less the rim
        /// (0.34 - 0.1 of the button over its 0.8), so the cushion follows the rim.
        /// </summary>
        public static float IconRimFaceRadius(Box face) => Math.Min(face.Width, face.Height) * (IconRadiusShare - IconRimShare) / IconRimFaceShare;

        /// <summary>The speed pill's glyph while fast forward is off: three brown chevrons (▶▶▶), no number.</summary>
        public static IconPart FastGlyph { get; } = new IconPart("ui.fast", C.InkBrown);

        /// <summary>
        /// The speed pill's glyph while fast forward is on (the owner, 2026-10-06: it lights up as switched on): the
        /// chevrons in the green of a switched-on toggle, inside a <c>garden.glow</c> halo (<see cref="SpeedGlowAlpha"/>).
        /// </summary>
        public static IconPart FastGlyphOn { get; } = new IconPart("ui.fast", C.ButtonPrimary);

        /// <summary>The lit speed pill's halo: <see cref="SpeedGlowLayers"/> rings of <c>garden.glow</c> sharing this alpha.</summary>
        public const float SpeedGlowAlpha = 0.85f;

        public const int SpeedGlowLayers = 4;

        /// <summary>How far each halo ring grows past the pill's face, as a share of its height.</summary>
        public const float SpeedGlowGrow = 0.045f;

        /// <summary>The speed pill's glyph box: a square this share of the pill's height, centered (no label beside it).</summary>
        public const float SpeedGlyphShare = 0.74f;

        /// <summary>The back button's glyph: a brown left arrow.</summary>
        public static IconPart BackGlyph { get; } = new IconPart("ui.back", C.InkBrown);

        /// <summary>
        /// The Petals currency as a lotus bud (§3.4), back to front: all five petals in a slightly deeper pink with the
        /// lotus outline (the two back petals show around the front), the three front petals with their own outline, and
        /// each petal's near-white middle over a softer pink band, so the petals are deep pink at their edges.
        /// </summary>
        public static IReadOnlyList<IconPart> Lotus { get; } = new[]
        {
            new IconPart("currency.petal", C.LotusFill.Darken(0.08f), C.LotusLine, 0.07f),
            new IconPart("currency.petal.front", C.LotusFill, C.LotusLine, 0.035f),
            new IconPart("currency.petal.tips", C.LotusTip, C.LotusFill.Mix(C.LotusTip, 0.5f), 0.06f),
        };

        /// <summary>
        /// A booster's colored icon (§3.8), back to front: Extra Slot a white "+" on a blue disc; Shuffle two arrows chasing
        /// each other, orange and green with a white outline; Return a fat yellow arrow pointing left with an orange
        /// outline; Bloom Burst a pink five-petal flower with a yellow center. One-color contexts draw the main silhouette
        /// <c>booster.{id}</c> instead.
        /// </summary>
        public static IReadOnlyList<IconPart> BoosterIcon(string boosterId) =>
            BoosterIcons.TryGetValue(boosterId, out IReadOnlyList<IconPart>? parts) ? parts : BoosterIcons["bloom_burst"];

        /// <summary>
        /// The booster whose icon these parts are (<see cref="BoosterIcon"/> returns the same list each time), or null.
        /// Both kits draw the owner's icon picture for it when it exists (spec 005 FR-027, pictures.md D1–D4), wherever
        /// the icon shows: booster tiles, jam choices, cost pills, the Store, drops and rewards.
        /// </summary>
        public static string? BoosterOf(IReadOnlyList<IconPart>? parts)
        {
            if (parts == null)
            {
                return null;
            }

            foreach (KeyValuePair<string, IReadOnlyList<IconPart>> pair in BoosterIcons)
            {
                if (ReferenceEquals(pair.Value, parts))
                {
                    return pair.Key;
                }
            }

            return null;
        }

        private static readonly Dictionary<string, IReadOnlyList<IconPart>> BoosterIcons = BuildBoosterIcons();

        private static Dictionary<string, IReadOnlyList<IconPart>> BuildBoosterIcons()
        {
            Rgba disc = Rgba.FromHex("#3E9BEA");
            Rgba petals = Rgba.FromHex("#F58CC8");
            return new Dictionary<string, IReadOnlyList<IconPart>>(StringComparer.Ordinal)
            {
                ["extra_slot"] = new[]
                {
                    new IconPart("booster.extra_slot.disc", disc, disc.Darken(0.35f), 0.07f),
                    new IconPart("booster.extra_slot.plus", Rgba.White, disc.Darken(0.18f), 0.05f),
                },
                ["shuffle"] = new[]
                {
                    new IconPart("booster.shuffle.a", Rgba.FromHex("#F2A33A"), Rgba.White, 0.08f),
                    new IconPart("booster.shuffle.b", Rgba.FromHex("#57B847"), Rgba.White, 0.08f),
                },
                ["return"] = new[] { new IconPart("booster.return", Rgba.FromHex("#FFC23D"), Rgba.FromHex("#E08A1E"), 0.08f) },
                ["bloom_burst"] = new[]
                {
                    new IconPart("booster.bloom_burst.petals", petals, petals.Darken(0.3f), 0.07f),
                    new IconPart("booster.bloom_burst.center", C.GardenFlowerCenter, C.GardenFlowerCenterLine, 0.04f),
                },
            };
        }

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

        // ---- A wooden sign's leaves (spec 005 contracts/look.md §3.2) ----

        /// <summary>An ivy cluster's side, as a share of its sign's height.</summary>
        public const float IvyShare = 1.25f;

        /// <summary>How far an ivy cluster's middle sits outside its plank's end, as a share of the sign's height.</summary>
        public const float IvyOut = 0.04f;

        /// <summary>
        /// How far an ivy cluster reaches beyond its plank's end, as a share of the sign's height (half the cluster plus
        /// <see cref="IvyOut"/>): what a row beside a sign keeps free (<see cref="PageHeader"/>).
        /// </summary>
        public const float IvyReach = (IvyShare / 2f) + IvyOut;

        /// <summary>How far ivy clusters drawn at <paramref name="scale"/> of their size reach beyond the plank's end (<see cref="IvyReach"/> at 1).</summary>
        public static float IvyReachAt(float scale) => (IvyShare * scale / 2f) + IvyOut;

        /// <summary>A flower cluster's side, as a share of its sign's height (the bottom-right one is 0.92 of it).</summary>
        public const float FlowerShare = 1.35f;

        /// <summary>
        /// The box of a sign end's ivy cluster (<c>ui.sign.ivy</c>; both builds' wooden signs): a square 1.25 × the sign's
        /// height on its middle line, centered 0.04 × its height outside the plank's end, so the leaves cling to its
        /// corners and most of the plank shows, as on the reference's gameplay sign. <paramref name="scale"/> sizes the
        /// square (the pages' banners: <see cref="PageHeader.IvyScale"/>).
        /// </summary>
        public static Box IvyBox(Box sign, bool left, float scale = 1f)
        {
            float h = sign.Height;
            float x = left ? sign.Left - (h * IvyOut) : sign.Right + (h * IvyOut);
            return Box.FromCenter(x, sign.CenterY, h * IvyShare * scale, h * IvyShare * scale);
        }

        /// <summary>
        /// The box of a flower cluster on the win and milestone sign (<c>ui.sign.flowers</c>): 1.35 × the sign's height
        /// over its top-left end, and 0.92 of that over its bottom-right end.
        /// </summary>
        public static Box FlowerBox(Box sign, bool left)
        {
            float h = sign.Height;
            float size = h * FlowerShare;
            return left
                ? Box.FromCenter(sign.Left + (h * 0.1f), sign.Top + (h * 0.08f), size, size)
                : Box.FromCenter(sign.Right - (h * 0.08f), sign.Bottom - (h * 0.04f), size * 0.92f, size * 0.92f);
        }

        /// <summary>
        /// A wooden sign's plank together with its decoration (<see cref="IvyBox"/> at <paramref name="ivyScale"/>,
        /// <see cref="FlowerBox"/>).
        /// </summary>
        public static Box SignExtent(Box sign, SignDecor decor, float ivyScale = 1f)
        {
            switch (decor)
            {
                case SignDecor.Ivy:
                    return Union(sign, Union(IvyBox(sign, true, ivyScale), IvyBox(sign, false, ivyScale)));
                case SignDecor.Flowers:
                    return Union(sign, Union(FlowerBox(sign, true), FlowerBox(sign, false)));
                default:
                    return sign;
            }
        }

        private static Box Union(Box a, Box b) =>
            new Box(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
    }
}
