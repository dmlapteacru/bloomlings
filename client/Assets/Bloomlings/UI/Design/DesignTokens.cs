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
    /// Engine-free.
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
            public static readonly Rgba SurfaceScrim = Rgba.FromHex("#1E2430").WithAlpha(0.55f);
            public static readonly Rgba TextPrimary = Rgba.FromHex("#2E3440");
            public static readonly Rgba TextSecondary = Rgba.FromHex("#6B7280");
            public static readonly Rgba TextOnColor = Rgba.FromHex("#FFFFFF");
            public static readonly Rgba TextOutline = Rgba.FromHex("#2E3440").WithAlpha(0.35f);
            public static readonly Rgba ButtonPrimary = Rgba.FromHex("#5DBB46");
            public static readonly Rgba ButtonPrimaryTop = Rgba.FromHex("#7ED35F");
            public static readonly Rgba ButtonPrimaryEdge = Rgba.FromHex("#3D8B2F");
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

        public static class Type
        {
            public static readonly TypeStyle Wordmark = new TypeStyle("type.wordmark", 170f, true, false, 12f, 110f);
            public static readonly TypeStyle Title = new TypeStyle("type.title", 64f, true, false, 0f, 44f);
            public static readonly TypeStyle TitleCaps = new TypeStyle("type.title_caps", 60f, true, true, 0f, 42f);
            public static readonly TypeStyle LevelHome = new TypeStyle("type.level_home", 84f, true, true, 0f, 60f);
            public static readonly TypeStyle LevelPill = new TypeStyle("type.level_pill", 52f, true, true, 3f, 38f);
            public static readonly TypeStyle ButtonLarge = new TypeStyle("type.button_large", 76f, true, true, 4f, 52f);
            public static readonly TypeStyle Button = new TypeStyle("type.button", 54f, true, true, 3f, 38f);
            public static readonly TypeStyle ButtonSecondary = new TypeStyle("type.button_secondary", 44f, true, true, 0f, 32f);
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
            public static readonly MotionToken Press = new MotionToken("motion.press", 0.09f, 0.94f);
            public static readonly MotionToken Pop = new MotionToken("motion.pop", 0.22f, 0.85f);
            public static readonly MotionToken Sheet = new MotionToken("motion.sheet", 0.26f, 1f);
            public static readonly MotionToken Reward = new MotionToken("motion.reward", 0.3f, 1f);
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
