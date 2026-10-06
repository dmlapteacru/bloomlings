using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>A text style of the board (contracts/design-tokens.md, "Type"). Sizes are reference units (1080-wide).</summary>
    public sealed record TypeStyle(string Name, float Size, bool Bold, bool Upper, float Outline, float Min);

    /// <summary>A soft motion of the board (contracts/design-tokens.md, "Motion").</summary>
    public sealed record MotionToken(string Name, float Seconds, float Scale);

    /// <summary>The backdrop colors of one level band's theme (research R8).</summary>
    public sealed record BackdropColors(Rgba SkyTop, Rgba SkyBottom, Rgba HillFar, Rgba HillNear, Rgba Bush, Rgba Blossom, Rgba Ruin);

    /// <summary>
    /// The named values of the design board's visual language (FR-005, research R6), implemented once for the Unity
    /// client and the full playtest. The values are sampled from <c>specs/002-ux-design-board/ux-design-board.webp</c>
    /// and documented in <c>contracts/design-tokens.md</c>; screens use these names, never literal colors or sizes.
    /// Spec 005 adds the reference look's materials (wood, stone, parchment, cream, lotus, lawn, ivy) from
    /// <c>specs/005-reference-look/contracts/look.md</c> §1.2. Engine-free.
    /// </summary>
    public static class DesignTokens
    {
        /// <summary>The reference screen width: sizes are in pixels of a 1080-px-wide portrait screen.</summary>
        public const float ReferenceWidth = 1080f;

        /// <summary>Pixels per reference unit for a screen, capped so tablets do not blow the UI up.</summary>
        public static float ScaleFor(float width, float height) => Math.Min(width / ReferenceWidth, height / 1700f);

        public static class Colors
        {
            public static readonly Rgba SurfacePanel = Rgba.FromHex("#FFF9EE");
            public static readonly Rgba SurfacePanelEdge = Rgba.FromHex("#E8DCC4");
            public static readonly Rgba SurfaceSunk = Rgba.FromHex("#F1E8D6");
            public static readonly Rgba SurfaceRowHighlight = Rgba.FromHex("#DDF2CF");
            public static readonly Rgba SurfaceScrim = Rgba.FromHex("#2A1708").WithAlpha(0.5f);
            public static readonly Rgba TextPrimary = Rgba.FromHex("#2E3440");
            public static readonly Rgba TextSecondary = Rgba.FromHex("#6B7280");
            public static readonly Rgba TextOnColor = Rgba.FromHex("#FFFFFF");
            public static readonly Rgba TextOutline = Rgba.FromHex("#2E3440").WithAlpha(0.35f);
            public static readonly Rgba ButtonPrimary = Rgba.FromHex("#62B83A");
            public static readonly Rgba ButtonPrimaryTop = Rgba.FromHex("#ADE162");
            public static readonly Rgba ButtonPrimaryEdge = Rgba.FromHex("#378F24");
            public static readonly Rgba ButtonSecondary = Rgba.FromHex("#F4EAD5");
            public static readonly Rgba ButtonSecondaryEdge = Rgba.FromHex("#D9C9A6");
            public static readonly Rgba ButtonIcon = Rgba.FromHex("#FFFFFF");
            public static readonly Rgba ButtonIconEdge = Rgba.FromHex("#C9CED8");
            public static readonly Rgba ButtonIconGlyph = Rgba.FromHex("#3A4050");
            public static readonly Rgba ButtonDark = Rgba.FromHex("#3A4050");
            public static readonly Rgba PillLevel = Rgba.FromHex("#8FC6F0");
            public static readonly Rgba PillLevelEdge = Rgba.FromHex("#5E9FD3");
            public static readonly Rgba PillLevelSuperHard = Rgba.FromHex("#B59AF0");
            public static readonly Rgba PillPetals = Rgba.FromHex("#FFFFFF").WithAlpha(0.88f);
            public static readonly Rgba PillPetalsEdge = Rgba.FromHex("#E3DCEF");
            public static readonly Rgba BadgeHard = Rgba.FromHex("#E5484D");
            public static readonly Rgba BadgeSuperHard = Rgba.FromHex("#8E4FD8");
            public static readonly Rgba BadgeCount = Rgba.FromHex("#2F3A4A");
            public static readonly Rgba AccentPlus = Rgba.FromHex("#5DBB46");
            public static readonly Rgba PetalFill = Rgba.FromHex("#F58DB8");
            public static readonly Rgba PetalCenter = Rgba.FromHex("#FFD35C");
            public static readonly Rgba PetalEdge = Rgba.FromHex("#D8639A");
            public static readonly Rgba StateDanger = Rgba.FromHex("#E5484D");
            public static readonly Rgba StateLock = Rgba.FromHex("#8C8F99");
            public static readonly Rgba StateLockBg = Rgba.FromHex("#C4C7CF");
            public static readonly Rgba StateStuck = Rgba.FromHex("#9AA0AA");
            public static readonly Rgba StateLink = Rgba.FromHex("#6CC4B8");
            public static readonly Rgba StateLink2 = Rgba.FromHex("#6FB6E8");
            public static readonly Rgba StateLink3 = Rgba.FromHex("#E67FB0");
            public static readonly Rgba MedalGold = Rgba.FromHex("#F5C542");
            public static readonly Rgba MedalSilver = Rgba.FromHex("#C9D1DC");
            public static readonly Rgba MedalBronze = Rgba.FromHex("#DA9A62");
            public static readonly Rgba BoosterExtraSlot = Rgba.FromHex("#4CAF50");
            public static readonly Rgba BoosterShuffle = Rgba.FromHex("#5B6CE0");
            public static readonly Rgba BoosterReturn = Rgba.FromHex("#3A8EDB");
            public static readonly Rgba BoosterBloomBurst = Rgba.FromHex("#F2622E");
            public static readonly Rgba BackdropSkyTop = Rgba.FromHex("#BFE3F8");
            public static readonly Rgba BackdropSkyBottom = Rgba.FromHex("#EAF6F2");
            public static readonly Rgba BackdropHillFar = Rgba.FromHex("#CFE6C0");
            public static readonly Rgba BackdropHillNear = Rgba.FromHex("#A9D68C");
            public static readonly Rgba BackdropBush = Rgba.FromHex("#86C470");
            public static readonly Rgba BackdropBlossom = Rgba.FromHex("#F9B8D0");
            public static readonly Rgba BackdropRuin = Rgba.FromHex("#DCD6E8");
            public static readonly Rgba WordmarkFill = Rgba.FromHex("#7CCB52");
            public static readonly Rgba WordmarkOutline = Rgba.FromHex("#2F7A2A");
            public static readonly Rgba TileGround = Rgba.FromHex("#EFE6D2");
            public static readonly Rgba TileStone = Rgba.FromHex("#A3A6AE");
            public static readonly Rgba TileStoneEdge = Rgba.FromHex("#7D818B");
            public static readonly Rgba TileMystery = Rgba.FromHex("#B8AFCB");
            public static readonly Rgba PodMystery = Rgba.FromHex("#F7D6E6");
            public static readonly Rgba PodMysteryMark = Rgba.FromHex("#C0508A");
            public static readonly Rgba SpecialGate = Rgba.FromHex("#6E9A5B");
            public static readonly Rgba SpecialFountain = Rgba.FromHex("#8FB4D6");
            public static readonly Rgba SpecialChest = Rgba.FromHex("#C08A57");
            public static readonly Rgba SpecialStatue = Rgba.FromHex("#A7A9BA");
            public static readonly Rgba SpecialBridge = Rgba.FromHex("#A57C58");
            public static readonly Rgba RewardBasket = Rgba.FromHex("#B87B4B");

            // ---- The Garden look (spec 003 contracts/garden-tokens.md) ----
            public static readonly Rgba GardenPlateTop = Rgba.FromHex("#FCF5E4");
            public static readonly Rgba GardenPlateBottom = Rgba.FromHex("#EBDDBE");
            public static readonly Rgba GardenPlateDepth = Rgba.FromHex("#A88A5C");
            public static readonly Rgba GardenOutline = Rgba.FromHex("#8C6B45");
            public static readonly Rgba GardenWood = Rgba.FromHex("#8C6B45");
            public static readonly Rgba GardenWoodDepth = Rgba.FromHex("#A88A5C");
            public static readonly Rgba GardenPaperTop = Rgba.FromHex("#FFF9EC");
            public static readonly Rgba GardenPaperBottom = Rgba.FromHex("#F6EBD3");
            public static readonly Rgba GardenWell = Rgba.FromHex("#E6D6B3");
            public static readonly Rgba GardenWellEdge = Rgba.FromHex("#B39668");
            public static readonly Rgba GardenTabSunk = Rgba.FromHex("#E9D9B7");
            public static readonly Rgba GardenBadge = Rgba.FromHex("#3B2A1A");
            public static readonly Rgba GardenBadgeRing = Rgba.FromHex("#FBF3E1");
            public static readonly Rgba GardenShadow = Rgba.FromHex("#3C2814");
            public static readonly Rgba GardenLabelFillTop = Rgba.FromHex("#FFFFFF");
            public static readonly Rgba GardenLabelFillBottom = Rgba.FromHex("#EEF2DA");
            public static readonly Rgba GardenLabelPlain = Rgba.FromHex("#5A3F24");
            public static readonly Rgba GardenLeaf1 = Rgba.FromHex("#6DBE45");
            public static readonly Rgba GardenLeaf2 = Rgba.FromHex("#8BD35A");
            public static readonly Rgba GardenLeaf3 = Rgba.FromHex("#5BAA3A");
            public static readonly Rgba GardenLeafLine = Rgba.FromHex("#2F6B22");
            public static readonly Rgba GardenFlower = Rgba.FromHex("#FFFFFF");
            public static readonly Rgba GardenFlowerLine = Rgba.FromHex("#B9B09A");
            public static readonly Rgba GardenFlowerCenter = Rgba.FromHex("#FFD35C");
            public static readonly Rgba GardenFlowerCenterLine = Rgba.FromHex("#D29B2E");
            public static readonly Rgba GardenGlow = Rgba.FromHex("#FFD54A");

            // ---- The clearing styles (spec 005 FR-038, contracts/look.md §6.12) ----

            /// <summary>A soap bubble's glass (Bubbles), at a low alpha.</summary>
            public static readonly Rgba FxGlass = Rgba.FromHex("#DDF4FF");

            /// <summary>A soap bubble's pink rainbow rim.</summary>
            public static readonly Rgba FxGlassRim = Rgba.FromHex("#F7B6E4");

            /// <summary>A firefly's golden core (Fireflies).</summary>
            public static readonly Rgba FxFirefly = Rgba.FromHex("#FFC23A");

            /// <summary>A firefly's soft glow, and a glowing tile.</summary>
            public static readonly Rgba FxFireflyGlow = Rgba.FromHex("#FFF3A6");

            /// <summary>The pink confetti (Confetti Parade).</summary>
            public static readonly Rgba FxConfettiPink = Rgba.FromHex("#FF7FB0");

            // ---- The reference look (spec 005 contracts/look.md §1.2) ----

            /// <summary>Sign / rim face top.</summary>
            public static readonly Rgba WoodLight = Rgba.FromHex("#FBE2BC");

            /// <summary>Sign / rim face bottom.</summary>
            public static readonly Rgba WoodMid = Rgba.FromHex("#F1CD98");

            /// <summary>Grain lines (alpha 0.25–0.45).</summary>
            public static readonly Rgba WoodGrain = Rgba.FromHex("#C99863");

            /// <summary>Sign lower lip.</summary>
            public static readonly Rgba WoodEdge = Rgba.FromHex("#DDB27C");

            /// <summary>Sign / rim outline.</summary>
            public static readonly Rgba WoodLine = Rgba.FromHex("#8B5A2B");

            /// <summary>Pod frame face.</summary>
            public static readonly Rgba WoodDark = Rgba.FromHex("#8A5634");

            /// <summary>Pod frame top light.</summary>
            public static readonly Rgba WoodDarkTop = Rgba.FromHex("#A86F45");

            /// <summary>Pod frame outline.</summary>
            public static readonly Rgba WoodDarkLine = Rgba.FromHex("#4A2A14");

            /// <summary>Stone block top light.</summary>
            public static readonly Rgba StoneTop = Rgba.FromHex("#FCE8C6");

            /// <summary>Stone block face.</summary>
            public static readonly Rgba StoneFace = Rgba.FromHex("#F1D5A8");

            /// <summary>Stone block lower edge.</summary>
            public static readonly Rgba StoneLip = Rgba.FromHex("#D9B585");

            /// <summary>Stone outline and joints.</summary>
            public static readonly Rgba StoneLine = Rgba.FromHex("#7E6844");

            /// <summary>Moss patches.</summary>
            public static readonly Rgba StoneMoss = Rgba.FromHex("#7DB24A");

            /// <summary>Card / tray top.</summary>
            public static readonly Rgba ParchmentTop = Rgba.FromHex("#FFF8E8");

            /// <summary>Card / tray bottom.</summary>
            public static readonly Rgba ParchmentBottom = Rgba.FromHex("#F5E4C3");

            /// <summary>Inner border line, plate depth.</summary>
            public static readonly Rgba ParchmentEdge = Rgba.FromHex("#EBCB9A");

            /// <summary>Card outline.</summary>
            public static readonly Rgba ParchmentLine = Rgba.FromHex("#B48552");

            /// <summary>Inset wells (jam row, sunk tabs).</summary>
            public static readonly Rgba ParchmentWell = Rgba.FromHex("#F3D7AB");

            /// <summary>Cream button / slot / booster face.</summary>
            public static readonly Rgba CreamFace = Rgba.FromHex("#FCE7C8");

            /// <summary>Cream face top.</summary>
            public static readonly Rgba CreamTop = Rgba.FromHex("#FFF6E6");

            /// <summary>Cream lower lip.</summary>
            public static readonly Rgba CreamLip = Rgba.FromHex("#E6C69B");

            /// <summary>Cream outline.</summary>
            public static readonly Rgba CreamLine = Rgba.FromHex("#C79F6F");

            /// <summary>Sign letters, counts, glyphs on cream, row labels: a near-black warm brown.</summary>
            public static readonly Rgba InkBrown = Rgba.FromHex("#3A2416");

            /// <summary>Big card and sheet titles and the win and milestone signs: a warmer red-brown.</summary>
            public static readonly Rgba InkTitle = Rgba.FromHex("#6E3416");

            /// <summary>Body text on parchment.</summary>
            public static readonly Rgba InkBrownSoft = Rgba.FromHex("#7B5A3A");

            /// <summary>Lotus petals.</summary>
            public static readonly Rgba LotusFill = Rgba.FromHex("#F7739F");

            /// <summary>Lotus petal light.</summary>
            public static readonly Rgba LotusTip = Rgba.FromHex("#FFE4EE");

            /// <summary>Lotus outline.</summary>
            public static readonly Rgba LotusLine = Rgba.FromHex("#D14F7A");

            /// <summary>Count badge disc.</summary>
            public static readonly Rgba BadgeGreen = Rgba.FromHex("#245C34");

            /// <summary>Gameplay lawn.</summary>
            public static readonly Rgba LawnLight = Rgba.FromHex("#9CC842");

            /// <summary>Lawn shade, grass strokes.</summary>
            public static readonly Rgba LawnDark = Rgba.FromHex("#64982D");

            /// <summary>The gameplay garden's deepest foliage: the hedge and the bushes' shade (spec 005 FR-020, §4.2).</summary>
            public static readonly Rgba FoliageDeep = Rgba.FromHex("#1F4D17");

            /// <summary>The gameplay garden's foliage.</summary>
            public static readonly Rgba Foliage = Rgba.FromHex("#3A8526");

            /// <summary>The gameplay garden's sunlit leaves.</summary>
            public static readonly Rgba FoliageLight = Rgba.FromHex("#7DC443");

            /// <summary>Ivy / clover leaves on signs.</summary>
            public static readonly Rgba IvyLeaf = Rgba.FromHex("#96D03C");

            /// <summary>Ivy outline.</summary>
            public static readonly Rgba IvyLine = Rgba.FromHex("#2F6A18");

            /// <summary>Jam Return / Bloom Burst buttons.</summary>
            public static readonly Rgba ButtonBlue = Rgba.FromHex("#45A3EE");

            /// <summary>Orange buttons ("Next" in the strip).</summary>
            public static readonly Rgba ButtonOrange = Rgba.FromHex("#F6B021");

            /// <summary>Win light rays (alpha).</summary>
            public static readonly Rgba RayLight = Rgba.FromHex("#FFF4C8");

            /// <summary>Every color token by its contract name (tests and docs).</summary>
            public static IReadOnlyDictionary<string, Rgba> All { get; } = new Dictionary<string, Rgba>(StringComparer.Ordinal)
            {
                ["surface.panel"] = SurfacePanel,
                ["surface.panel_edge"] = SurfacePanelEdge,
                ["surface.sunk"] = SurfaceSunk,
                ["surface.row_highlight"] = SurfaceRowHighlight,
                ["surface.scrim"] = SurfaceScrim,
                ["text.primary"] = TextPrimary,
                ["text.secondary"] = TextSecondary,
                ["text.on_color"] = TextOnColor,
                ["text.outline"] = TextOutline,
                ["button.primary"] = ButtonPrimary,
                ["button.primary_top"] = ButtonPrimaryTop,
                ["button.primary_edge"] = ButtonPrimaryEdge,
                ["button.secondary"] = ButtonSecondary,
                ["button.secondary_edge"] = ButtonSecondaryEdge,
                ["button.icon"] = ButtonIcon,
                ["button.icon_edge"] = ButtonIconEdge,
                ["button.icon_glyph"] = ButtonIconGlyph,
                ["button.dark"] = ButtonDark,
                ["pill.level"] = PillLevel,
                ["pill.level_edge"] = PillLevelEdge,
                ["pill.level_super_hard"] = PillLevelSuperHard,
                ["pill.petals"] = PillPetals,
                ["pill.petals_edge"] = PillPetalsEdge,
                ["badge.hard"] = BadgeHard,
                ["badge.super_hard"] = BadgeSuperHard,
                ["badge.count"] = BadgeCount,
                ["accent.plus"] = AccentPlus,
                ["petal.fill"] = PetalFill,
                ["petal.center"] = PetalCenter,
                ["petal.edge"] = PetalEdge,
                ["state.danger"] = StateDanger,
                ["state.lock"] = StateLock,
                ["state.lock_bg"] = StateLockBg,
                ["state.stuck"] = StateStuck,
                ["state.link"] = StateLink,
                ["state.link_2"] = StateLink2,
                ["state.link_3"] = StateLink3,
                ["medal.gold"] = MedalGold,
                ["medal.silver"] = MedalSilver,
                ["medal.bronze"] = MedalBronze,
                ["booster.extra_slot"] = BoosterExtraSlot,
                ["booster.shuffle"] = BoosterShuffle,
                ["booster.return"] = BoosterReturn,
                ["booster.bloom_burst"] = BoosterBloomBurst,
                ["backdrop.sky_top"] = BackdropSkyTop,
                ["backdrop.sky_bottom"] = BackdropSkyBottom,
                ["backdrop.hill_far"] = BackdropHillFar,
                ["backdrop.hill_near"] = BackdropHillNear,
                ["backdrop.bush"] = BackdropBush,
                ["backdrop.blossom"] = BackdropBlossom,
                ["backdrop.ruin"] = BackdropRuin,
                ["wordmark.fill"] = WordmarkFill,
                ["wordmark.outline"] = WordmarkOutline,
                ["tile.ground"] = TileGround,
                ["tile.stone"] = TileStone,
                ["tile.stone_edge"] = TileStoneEdge,
                ["tile.mystery"] = TileMystery,
                ["pod.mystery"] = PodMystery,
                ["pod.mystery_mark"] = PodMysteryMark,
                ["special.gate"] = SpecialGate,
                ["special.fountain"] = SpecialFountain,
                ["special.chest"] = SpecialChest,
                ["special.statue"] = SpecialStatue,
                ["special.bridge"] = SpecialBridge,
                ["currency.reward_basket"] = RewardBasket,
                ["garden.plate_top"] = GardenPlateTop,
                ["garden.plate_bottom"] = GardenPlateBottom,
                ["garden.plate_depth_color"] = GardenPlateDepth,
                ["garden.outline"] = GardenOutline,
                ["garden.wood"] = GardenWood,
                ["garden.wood_depth"] = GardenWoodDepth,
                ["garden.paper_top"] = GardenPaperTop,
                ["garden.paper_bottom"] = GardenPaperBottom,
                ["garden.well"] = GardenWell,
                ["garden.well_edge"] = GardenWellEdge,
                ["garden.tab_sunk"] = GardenTabSunk,
                ["garden.badge"] = GardenBadge,
                ["garden.badge_ring"] = GardenBadgeRing,
                ["garden.shadow"] = GardenShadow,
                ["garden.label_fill_top"] = GardenLabelFillTop,
                ["garden.label_fill_bottom"] = GardenLabelFillBottom,
                ["garden.label_plain"] = GardenLabelPlain,
                ["garden.leaf_1"] = GardenLeaf1,
                ["garden.leaf_2"] = GardenLeaf2,
                ["garden.leaf_3"] = GardenLeaf3,
                ["garden.leaf_line"] = GardenLeafLine,
                ["garden.flower"] = GardenFlower,
                ["garden.flower_line"] = GardenFlowerLine,
                ["garden.flower_center"] = GardenFlowerCenter,
                ["garden.flower_center_line"] = GardenFlowerCenterLine,
                ["garden.glow"] = GardenGlow,
                ["fx.glass"] = FxGlass,
                ["fx.glass_rim"] = FxGlassRim,
                ["fx.firefly"] = FxFirefly,
                ["fx.firefly_glow"] = FxFireflyGlow,
                ["fx.confetti_pink"] = FxConfettiPink,
                ["wood.light"] = WoodLight,
                ["wood.mid"] = WoodMid,
                ["wood.grain"] = WoodGrain,
                ["wood.edge"] = WoodEdge,
                ["wood.line"] = WoodLine,
                ["wood.dark"] = WoodDark,
                ["wood.dark_top"] = WoodDarkTop,
                ["wood.dark_line"] = WoodDarkLine,
                ["stone.top"] = StoneTop,
                ["stone.face"] = StoneFace,
                ["stone.lip"] = StoneLip,
                ["stone.line"] = StoneLine,
                ["stone.moss"] = StoneMoss,
                ["parchment.top"] = ParchmentTop,
                ["parchment.bottom"] = ParchmentBottom,
                ["parchment.edge"] = ParchmentEdge,
                ["parchment.line"] = ParchmentLine,
                ["parchment.well"] = ParchmentWell,
                ["cream.face"] = CreamFace,
                ["cream.top"] = CreamTop,
                ["cream.lip"] = CreamLip,
                ["cream.line"] = CreamLine,
                ["ink.brown"] = InkBrown,
                ["ink.title"] = InkTitle,
                ["ink.brown_soft"] = InkBrownSoft,
                ["lotus.fill"] = LotusFill,
                ["lotus.tip"] = LotusTip,
                ["lotus.line"] = LotusLine,
                ["badge.green"] = BadgeGreen,
                ["lawn.light"] = LawnLight,
                ["lawn.dark"] = LawnDark,
                ["foliage.deep"] = FoliageDeep,
                ["foliage.mid"] = Foliage,
                ["foliage.light"] = FoliageLight,
                ["ivy.leaf"] = IvyLeaf,
                ["ivy.line"] = IvyLine,
                ["button.blue"] = ButtonBlue,
                ["button.orange"] = ButtonOrange,
                ["ray.light"] = RayLight,
            };

            /// <summary>The medal color of ranks 1–3, or null for other ranks.</summary>
            public static Rgba? Medal(int rank) => rank switch
            {
                1 => MedalGold,
                2 => MedalSilver,
                3 => MedalBronze,
                _ => null,
            };
        }

        /// <summary>Corner radii as a fraction of the shape's shorter side.</summary>
        public static class Radius
        {
            public const float Pill = 0.5f;
            public const float Card = 0.08f;
            public const float CardMin = 40f;
            public const float Tile = 0.22f;
            public const float Pod = 0.2f;
            public const float Slot = 0.22f;
            public const float Row = 0.25f;
        }

        /// <summary>
        /// The board's text styles. Since spec 003 (Clarifications, 2A) every label is sentence case ("Play"); only
        /// <see cref="Badge"/> (HARD, SUPER HARD) stays uppercase. Bold styles use Nunito ExtraBold, the others Nunito
        /// SemiBold (spec 003 contracts/fonts.md).
        /// </summary>
        public static class Type
        {
            public static readonly TypeStyle Wordmark = new TypeStyle("type.wordmark", 170f, true, false, 12f, 110f);
            public static readonly TypeStyle Title = new TypeStyle("type.title", 64f, true, false, 0f, 44f);
            public static readonly TypeStyle TitleCaps = new TypeStyle("type.title_caps", 60f, true, false, 0f, 42f);
            public static readonly TypeStyle LevelHome = new TypeStyle("type.level_home", 84f, true, false, 0f, 60f);
            public static readonly TypeStyle LevelPill = new TypeStyle("type.level_pill", 52f, true, false, 3f, 38f);
            public static readonly TypeStyle ButtonLarge = new TypeStyle("type.button_large", 92f, true, false, 4f, 60f);
            public static readonly TypeStyle Button = new TypeStyle("type.button", 60f, true, false, 3f, 40f);
            public static readonly TypeStyle ButtonSecondary = new TypeStyle("type.button_secondary", 48f, true, false, 0f, 34f);
            public static readonly TypeStyle Body = new TypeStyle("type.body", 40f, false, false, 0f, 30f);
            public static readonly TypeStyle Caption = new TypeStyle("type.caption", 32f, false, false, 0f, 26f);
            public static readonly TypeStyle Count = new TypeStyle("type.count", 40f, true, false, 3f, 30f);
            public static readonly TypeStyle Badge = new TypeStyle("type.badge", 28f, true, true, 0f, 22f);
            public static readonly TypeStyle Reward = new TypeStyle("type.reward", 72f, true, false, 3f, 48f);

            public static IReadOnlyList<TypeStyle> All { get; } = new[]
            {
                Wordmark, Title, TitleCaps, LevelHome, LevelPill, ButtonLarge, Button, ButtonSecondary, Body, Caption, Count, Badge, Reward,
            };
        }

        public static class Space
        {
            public const float Xs = 8f;
            public const float S = 16f;
            public const float M = 28f;
            public const float L = 44f;
            public const float Xl = 72f;
        }

        public static class Size
        {
            public const float TouchMin = 132f;
            public const float IconButton = 132f;
            public const float BoosterButton = 150f;
            public const float PrimaryHeight = 150f;
            public const float PrimaryHeightSmall = 126f;
            public const float SecondaryHeight = 110f;
            public const float Margin = 44f;

            /// <summary>PLAY on Home: shorter and taller than spec 002, about 2.6 : 1 (spec 003 FR-011).</summary>
            public const float PlayWidth = 540f;

            public const float PlayHeight = 204f;

            /// <summary>The main button of a card (NEXT, RESUME, CLAIM, CONTINUE), narrower and centered.</summary>
            public const float CardPrimaryWidth = 620f;

            /// <summary>Its height (the owner, 2026-10-06: the pause card's buttons felt thin; it was 140).</summary>
            public const float CardPrimaryHeight = 160f;

            /// <summary>The height of a card's secondary buttons (it was <see cref="SecondaryHeight"/>, 110).</summary>
            public const float CardSecondaryHeight = 136f;

            /// <summary>The secondary buttons of a card (HOME, RESTART, SETTINGS).</summary>
            public const float CardSecondaryWidth = 580f;

            /// <summary>A booster tile of the booster bar (spec 003 FR-031, contracts/booster-tile.md).</summary>
            public const float BoosterTileWidth = 152f;

            public const float BoosterTileHeight = 156f;

            /// <summary>The wooden sign over the win and milestone cards (spec 005 contracts/look.md §4.4).</summary>
            public const float WinSignHeight = 146f;

            /// <summary>The finished picture on the win card (spec 005 §4.4).</summary>
            public const float WinPictureHeight = 520f;

            /// <summary>The reward pill of the win and milestone cards (spec 005 §4.4).</summary>
            public const float RewardPillHeight = 104f;
        }

        /// <summary>
        /// The finger on a page that scrolls (spec 005 FR-041, the owner's request of 2026-10-06; <see cref="TouchGesture"/>),
        /// in density-independent pixels (dp: 160 to the inch), so a finger moves as far on every phone: each host turns them
        /// into its pixels with its screen's density (<see cref="Pixels"/>).
        /// </summary>
        public static class Touch
        {
            /// <summary>
            /// <c>touch.slop</c>: a finger that moves farther than this from where it went down on a page that scrolls drags
            /// it and never taps (Android's own touch slop is 8 dp; Unity's <c>EventSystem.pixelDragThreshold</c>).
            /// </summary>
            public const float SlopDp = 10f;

            /// <summary><c>touch.swipe</c>: a drag at least this long along its main direction turns a paged list's page.</summary>
            public const float SwipeDp = 40f;

            /// <summary>The dp of a phone 360 dp wide, for a host that cannot tell its screen's density (<see cref="Pixels"/>).</summary>
            public const float FallbackWidthDp = 360f;

            /// <summary>
            /// <paramref name="dp"/> in a host's pixels: with its screen's <paramref name="dpi"/> (dots per inch), else (0 or
            /// less: unknown) as on a phone <see cref="FallbackWidthDp"/> wide over <paramref name="screenWidth"/> pixels.
            /// </summary>
            public static float Pixels(float dp, float dpi, float screenWidth) =>
                dpi > 0f ? dp * dpi / 160f : dp * Math.Max(1f, screenWidth) / FallbackWidthDp;
        }

        public static class Elevation
        {
            /// <summary>The darker lower edge of raised elements (buttons, pills, tiles, pods), in reference units.</summary>
            public const float RaisedEdge = 8f;

            public const float CardShadowOffset = 10f;
            public const float CardShadowAlpha = 0.18f;
        }

        public static class Motion
        {
            /// <summary>The press: down within a frame, then a 0.36 s spring back with one overshoot (spec 003 FR-017).</summary>
            public static readonly MotionToken Press = new MotionToken("motion.press", 0.36f, 0.95f);

            /// <summary>The idle breath of the one waiting button (spec 003 FR-019): at most 4%, at least 1.2 s.</summary>
            public static readonly MotionToken Breathe = new MotionToken("motion.breathe", 1.6f, 1.03f);

            /// <summary>Earned Petals count up to their value (spec 003 FR-020).</summary>
            public static readonly MotionToken CountUp = new MotionToken("motion.count_up", 0.8f, 1f);
            public static readonly MotionToken Pop = new MotionToken("motion.pop", 0.22f, 0.85f);
            public static readonly MotionToken Sheet = new MotionToken("motion.sheet", 0.26f, 1f);
            public static readonly MotionToken Reward = new MotionToken("motion.reward", 0.3f, 1f);

            /// <summary>The pulse of a selected booster's glow (spec 003 FR-031).</summary>
            public static readonly MotionToken Glow = new MotionToken("motion.glow", 1.2f, 1f);
        }

        /// <summary>
        /// The Garden recipe's geometry in reference units (spec 003 FR-006 to FR-008, FR-022, FR-023, FR-028;
        /// contracts/garden-tokens.md). The colors are in <see cref="Colors"/> (<c>garden.*</c>) and the color sets in
        /// <see cref="GardenLook"/>.
        /// </summary>
        public static class Garden
        {
            /// <summary>The outline of plates and buttons; small elements use <see cref="OutlineWidthSmall"/>.</summary>
            public const float OutlineWidth = 3f;

            public const float OutlineWidthSmall = 2f;

            /// <summary>How far the button sits inside its plate, by element size.</summary>
            public const float PlateInsetLarge = 11f;

            public const float PlateInsetMedium = 8f;

            public const float PlateInsetSmall = 5f;

            /// <summary>The plate's visible thickness below it.</summary>
            public const float PlateDepth = 6f;

            public const float PlateDepthSmall = 3f;

            /// <summary>The darker band along a button's bottom edge, by element size.</summary>
            public const float LipLarge = 13f;

            public const float LipMedium = 10f;

            public const float LipSmall = 6f;

            /// <summary>The white highlight band across a button's upper part.</summary>
            public const float HighlightAlpha = 0.5f;

            public const float HighlightHeight = 0.36f;

            /// <summary>The wooden frame of cards and the board, and of the slot row.</summary>
            public const float FrameWidth = 6f;

            public const float FrameWidthSlots = 5f;

            public const float FrameDepthCard = 12f;

            public const float FrameDepthBoard = 10f;

            public const float FrameDepthSlots = 8f;

            /// <summary>The volumetric board cells (FR-023).</summary>
            public const float CellLip = 14f;

            public const float CellHighlightAlpha = 0.4f;

            /// <summary>The volumetric pods (FR-022).</summary>
            public const float PodLip = 20f;

            /// <summary>The soft drop shadow under plates and frames.</summary>
            public const float ShadowAlpha = 0.28f;

            /// <summary>The leaves and flowers on the main buttons (FR-011a), as a fraction of the button height.</summary>
            public const float DecorationSize = 0.75f;

            /// <summary>Whether main buttons carry the leaves and flowers (one switch, FR-011a).</summary>
            public static bool Decorations { get; set; } = true;

            /// <summary>The label look (FR-009): outline, extrusion and shadow, as fractions of the font size.</summary>
            public const float LabelOutlineEm = 0.036f;

            public const float LabelExtrudeEm = 0.09f;

            public const float LabelShadowAlpha = 0.3f;

            /// <summary>The plate inset of an element of this height (large ≥ 140, medium ≥ 90, small below).</summary>
            public static float PlateInset(float heightUnits) => heightUnits >= 140f ? PlateInsetLarge : heightUnits >= 90f ? PlateInsetMedium : PlateInsetSmall;

            /// <summary>The lip of an element of this height.</summary>
            public static float Lip(float heightUnits) => heightUnits >= 140f ? LipLarge : heightUnits >= 90f ? LipMedium : LipSmall;

            /// <summary>The outline width of an element of this height.</summary>
            public static float Outline(float heightUnits) => heightUnits >= 90f ? OutlineWidth : OutlineWidthSmall;

            /// <summary>The plate thickness of an element of this height.</summary>
            public static float Depth(float heightUnits) => heightUnits >= 90f ? PlateDepth : PlateDepthSmall;
        }

        /// <summary>The backdrop of a level band, mixed 35% toward its theme (FR-008, FR-066); null theme colors keep the defaults.</summary>
        public static BackdropColors Backdrop(string? themeBackgroundHex, string? themeAccentHex)
        {
            Rgba background = themeBackgroundHex != null ? Rgba.FromHex(themeBackgroundHex) : Colors.BackdropSkyBottom;
            Rgba accent = themeAccentHex != null ? Rgba.FromHex(themeAccentHex) : Colors.BackdropHillFar;
            const float t = 0.35f;
            return new BackdropColors(
                Colors.BackdropSkyTop.Mix(accent, t),
                Colors.BackdropSkyBottom.Mix(background, t),
                Colors.BackdropHillFar.Mix(accent, t),
                Colors.BackdropHillNear.Mix(accent, t * 0.6f),
                Colors.BackdropBush.Mix(accent, t * 0.5f),
                Colors.BackdropBlossom,
                Colors.BackdropRuin.Mix(background, t));
        }

        /// <summary>The lighter top half of a raised tile in a variant color (research R7).</summary>
        public static Rgba TileTop(Rgba variant) => variant.Lighten(0.1f);

        /// <summary>The darker lower edge of a raised tile in a variant color.</summary>
        public static Rgba TileEdge(Rgba variant) => variant.Darken(0.28f);

        /// <summary>The pod card: the variant color mixed 70% toward white (frame 12).</summary>
        public static Rgba PodCard(Rgba variant) => variant.Lighten(0.7f);

        /// <summary>The lower edge of a pod card.</summary>
        public static Rgba PodCardEdge(Rgba variant) => variant.Lighten(0.35f);

        /// <summary>
        /// A pod still waiting in its stack (spec 003 FR-022a): its variant color muted toward grey, so the player can read
        /// what comes next while the exposed pods stay the bright, tappable ones.
        /// </summary>
        public static Rgba PodQueued(Rgba variant) => variant.Mix(Colors.StateStuck, 0.38f);

        /// <summary>
        /// How far a board tile's face is lightened toward <c>garden.paper_top</c> under its character (spec 004 FR-012,
        /// FR-013): 0 is the full variant color, 1 a plain cream tile as in the owner's reference.
        /// </summary>
        public const float CharacterTileMix = 0.7f;

        /// <summary>A target tile's face under its character: the variant color lightened toward the paper (spec 004 R6).</summary>
        public static Rgba CharacterTile(Rgba variant) => variant.Mix(Colors.GardenPaperTop, CharacterTileMix);

        /// <summary>The theme color of a booster's round button.</summary>
        public static Rgba BoosterColor(string boosterId) => boosterId switch
        {
            "extra_slot" => Colors.BoosterExtraSlot,
            "shuffle" => Colors.BoosterShuffle,
            "return" => Colors.BoosterReturn,
            _ => Colors.BoosterBloomBurst,
        };
    }
}
