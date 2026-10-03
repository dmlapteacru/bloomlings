using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Which Home elements show (FR-017, data-model "HomeLook"). Frame 2 always shows the Petals pill, Settings, Level N
    /// and PLAY. Each frame 3 element shows once its feature is unlocked, and a player who has not unlocked a feature
    /// never sees its button, card or badge (spec edge cases). Derived from progression, never stored. Engine-free.
    /// </summary>
    public sealed record HomeLook(
        bool Store,
        bool Teaser,
        bool Hero,
        bool Wardrobe,
        bool Collection,
        bool Rank,
        bool DailyChallenge,
        bool FreeBoosterOffer)
    {
        public const string StoreUnlock = "system.store";
        public const string LeaderboardUnlock = "system.leaderboard";
        public const string WardrobeUnlock = "system.wardrobe";
        public const string DailyChallengeUnlock = "system.daily_challenge";

        /// <summary>The early look of frame 2: nothing beyond the essentials.</summary>
        public static HomeLook Early { get; } = new HomeLook(false, false, false, false, false, false, false, false);

        /// <param name="isUnlocked">Whether a roadmap unlock id is reached.</param>
        /// <param name="collectionCount">Pictures in the Collection.</param>
        /// <param name="hasNextMilestone">A next milestone exists (the teaser "N levels to reward").</param>
        /// <param name="dailyChallengeAvailable">Today's Daily Challenge exists (hidden when its content is missing).</param>
        /// <param name="freeBoosterOffer">The optional free-booster ad offer is available (spec 001 FR-052).</param>
        public static HomeLook From(Func<string, bool> isUnlocked, int collectionCount, bool hasNextMilestone, bool dailyChallengeAvailable, bool freeBoosterOffer)
        {
            bool wardrobe = isUnlocked(WardrobeUnlock);
            return new HomeLook(
                Store: isUnlocked(StoreUnlock),
                Teaser: hasNextMilestone,
                Hero: wardrobe,
                Wardrobe: wardrobe,
                Collection: collectionCount > 0,
                Rank: isUnlocked(LeaderboardUnlock),
                DailyChallenge: isUnlocked(DailyChallengeUnlock) && dailyChallengeAvailable,
                FreeBoosterOffer: freeBoosterOffer);
        }

        /// <summary>The same look from a set of reached unlock ids.</summary>
        public static HomeLook From(ISet<string> unlocked, int collectionCount, bool hasNextMilestone, bool dailyChallengeAvailable, bool freeBoosterOffer) =>
            From(unlocked.Contains, collectionCount, hasNextMilestone, dailyChallengeAvailable, freeBoosterOffer);
    }

    /// <summary>
    /// The drawn Home stage until the owner's pictures arrive (spec 005 contracts/look.md §4.5, research D16): where the
    /// stone pedestal, the lotus fountain and the four heroes stand, and the warmer garden colors of the Home and splash
    /// backdrops. Both builds lay Home out with it. Engine-free.
    /// </summary>
    public static class HomeStage
    {
        /// <summary>The share of a hero picture's height above its feet (the feet stand at 90% of the picture).</summary>
        public const float FeetShare = 0.9f;

        /// <summary>
        /// Whether Home and the splash stand heroes (spec 005 FR-024, FR-028, contracts/look.md §4.5, §6.4). Over the
        /// owner's layered Home (<paramref name="layered"/>: its garden picture, pictures.md B1, with the fountain's back,
        /// the lotus and the fountain's front of <see cref="HomeLayers"/>) the four animated heroes stand around the painted
        /// lotus fountain (the owner, 2026-10-02: <see cref="HomeMotion"/>, <see cref="HomeLayers.HeroCell"/>); on the drawn
        /// stand-in (no owner picture) the still heroes stand around the drawn fountain (<see cref="ReferenceDiorama"/>).
        /// Over the owner's garden picture without its layers (or the splash's own B6, which has none) they show none: no
        /// fountain to stand them in, so the picture shows alone with the logo and the buttons.
        /// </summary>
        public static bool ShowsHeroes(bool ownerPicture, bool layered = false) => !ownerPicture || layered;

        /// <summary>
        /// The Home and splash garden (spec 005 §4.2: "the sky, arches and hills but warmer"): a clearer blue sky that
        /// turns to warm sunlight at the horizon, sunlit hills, lush bushes with pink blossoms and warm sandy arches. A
        /// quarter of the level band's theme tint (<paramref name="theme"/>) stays.
        /// </summary>
        public static BackdropColors Garden(BackdropColors theme)
        {
            const float keep = 0.15f;
            return new BackdropColors(
                C.BackdropSkyTop.Mix(C.ButtonBlue, 0.32f).Mix(theme.SkyTop, keep),
                C.RayLight.Mix(C.BackdropSkyBottom, 0.25f).Mix(theme.SkyBottom, keep),
                C.LawnLight.Mix(C.RayLight, 0.4f).Mix(theme.HillFar, keep),
                C.LawnLight.Mix(C.RayLight, 0.05f).Mix(theme.HillNear, keep),
                C.LawnDark.Mix(C.LawnLight, 0.3f).Mix(theme.Bush, keep),
                C.LotusFill.Mix(C.LotusTip, 0.25f),
                C.StoneFace.Mix(C.StoneLip, 0.45f).Mix(theme.Ruin, keep));
        }

        /// <summary>
        /// The reference Home's diorama (spec 005 FR-024, contracts/look.md §6.4; measured on the reference's Home) in
        /// <paramref name="stage"/>, Home's diorama region (<see cref="ReferenceHomeRegions.Diorama"/>): the well's stone
        /// ring 0.78 u wide near the stage's bottom, the lotus fountain on it, and the four heroes as large as the
        /// reference's around it: Bloom raised behind the fountain, Drop at the right back, Sprig at the left, Twig in front
        /// at the right. u is 0.88 of the stage's width, or its height over 1.09 (the reference stage's shape) on a shorter
        /// stage, so the heroes (their pictures less a 4% margin) stay inside it. Heroes are listed back to front, the
        /// fountain going before the fourth; hero boxes are 512 × 576 pictures whose feet stand at <see cref="FeetShare"/>.
        /// The hosts draw it only without the owner's garden picture (<see cref="ShowsHeroes"/>); over the owner's layered
        /// Home the heroes stand by <see cref="HomeLayers.HeroCell"/> instead.
        /// </summary>
        public static HomeDiorama ReferenceDiorama(Box stage)
        {
            float u = Math.Min(stage.Width * 0.88f, stage.Height / 1.09f);
            float cx = stage.CenterX;
            float Y(float up) => stage.Bottom - (up * u);
            var pedestal = new Box(cx - (0.39f * u), Y(0.385f), cx + (0.39f * u), Y(0.091f));
            var heroes = new List<(Family Family, Box Box)>
            {
                (Family.Bloom, Figure(cx, Y(0.51f), 0.64f * u)),
                (Family.Drop, Figure(cx + (0.3f * u), Y(0.4f), 0.44f * u)),
                (Family.Sprig, Figure(cx - (0.26f * u), Y(0.317f), 0.74f * u)),
                (Family.Twig, Figure(cx + (0.37f * u), Y(0.2f), 0.48f * u)),
            };

            var fountain = Box.FromCenter(cx, Y(0.364f), 0.31f * u, 0.12f * u);
            return new HomeDiorama(pedestal, fountain, heroes);
        }

        /// <summary>
        /// A hero on its stone pedestal (the Wardrobe's stage): the hero, as large as <paramref name="heroArea"/> allows,
        /// standing on a stone pedestal at the bottom of it. Returns the pedestal and the hero's picture box.
        /// </summary>
        public static (Box Pedestal, Box Hero) HeroOnPedestal(Box heroArea)
        {
            float width = Math.Min(heroArea.Width * 0.56f, heroArea.Height * 0.9f);
            float height = width * 0.34f;
            var pedestal = Box.FromCenter(heroArea.CenterX, heroArea.Bottom - (height / 2f), width, height);
            float topY = PedestalTop(pedestal).CenterY;
            float room = topY - heroArea.Top;
            float hero = Math.Min(room / FeetShare, width * 1.25f * CharacterArt.HeroHeight / CharacterArt.HeroWidth);
            return (pedestal, Figure(heroArea.CenterX, topY, hero));
        }

        /// <summary>
        /// The center line and the half height of a pedestal's top ellipse (the kit's <c>UiRaster.Pedestal</c> picture),
        /// where the heroes' feet go.
        /// </summary>
        public static (float CenterY, float RadiusY) PedestalTop(Box pedestal)
        {
            float rx = Math.Max(2f, (pedestal.Width / 2f) - 1f);
            float ry = Math.Max(1f, Math.Min(rx * 0.28f, (pedestal.Height - 4f) * 0.3f));
            return (pedestal.Top + ry + 1f, ry);
        }

        /// <summary>
        /// The win and milestone celebration (spec 005 §4.4) in <paramref name="stage"/>: a stone pedestal at its bottom,
        /// 80% of the group's width, and the group picture standing on its top (the heroes' feet just below the middle of
        /// the top ellipse), as large as the stage allows from the heroes' heads to the pedestal's foot (at most 98% of the
        /// stage's width). Returns the pedestal, the group's picture box and where the light rays turn (behind the heroes'
        /// bodies).
        /// </summary>
        public static (Box Pedestal, Box Group, float RaysX, float RaysY) Celebration(Box stage)
        {
            const float pedestalShare = 0.8f;
            const float pedestalAspect = 0.3f;
            float groupAspect = (float)CharacterArt.GroupHeight / CharacterArt.GroupWidth;

            // From the heads to the feet, then from the feet to the pedestal's foot: the top ellipse's half height is 9% of
            // the pedestal's width (PedestalTop), the feet stand a tenth of it below its middle.
            float feetToFoot = pedestalAspect - (0.09f * 1.1f);
            float span = ((CharacterArt.GroupFeetShare - CharacterArt.GroupHeadShare) * groupAspect) + (pedestalShare * feetToFoot);
            float width = Math.Min(stage.Width * 0.98f, stage.Height / span);
            float pw = width * pedestalShare;
            float ph = pw * pedestalAspect;
            var pedestal = new Box(stage.CenterX - (pw / 2f), stage.Bottom - ph, stage.CenterX + (pw / 2f), stage.Bottom);
            (float topY, float ry) = PedestalTop(pedestal);
            float feet = topY + (ry * 0.1f);
            Box group = CharacterArt.GroupStanding(stage.CenterX, feet, width);
            return (pedestal, group, stage.CenterX, feet - (group.Height * 0.22f));
        }

        /// <summary>A hero picture box whose feet stand at (<paramref name="x"/>, <paramref name="feet"/>).</summary>
        public static Box Figure(float x, float feet, float height)
        {
            float width = height * CharacterArt.HeroWidth / CharacterArt.HeroHeight;
            return new Box(x - (width / 2f), feet - (height * FeetShare), x + (width / 2f), feet + (height * (1f - FeetShare)));
        }
    }

    /// <summary>The drawn Home stage (<see cref="HomeStage.ReferenceDiorama"/>): heroes in drawing order, back to front.</summary>
    public sealed record HomeDiorama(Box Pedestal, Box Fountain, IReadOnlyList<(Family Family, Box Box)> Heroes);
}
