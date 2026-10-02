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
    /// stone pedestal, the lotus fountain, the four heroes and the guest stand, and the warmer garden colors of the Home
    /// and splash backdrops. Both builds lay Home out with it. Engine-free.
    /// </summary>
    public static class HomeStage
    {
        /// <summary>The share of a hero picture's height above its feet (the feet stand at 90% of the picture).</summary>
        public const float FeetShare = 0.9f;

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
        /// The early Home diorama in <paramref name="stage"/> (frame 2 and the splash; the reference's heroes around the
        /// lotus fountain): a wide stone pedestal in the middle with the fountain's basin on its top and the heroes in an
        /// arc around it (Sprig at the left, Bloom raised behind the fountain, Drop, Twig at the right edge in front). With
        /// <paramref name="guest"/> the arc moves right and the guest stands at the left front. Heroes are listed back to
        /// front; hero boxes are 512 × 576 pictures whose feet stand at <see cref="FeetShare"/>.
        /// </summary>
        public static HomeDiorama Diorama(Box stage, bool guest = true)
        {
            float width = Math.Min(stage.Width * 0.84f, stage.Height * 1.12f);
            float height = width * 0.34f;
            float cx = stage.CenterX;
            var pedestal = Box.FromCenter(cx, stage.Bottom - (stage.Height * 0.03f) - (height / 2f), width, height);
            (float topY, float ry) = PedestalTop(pedestal);
            float hero = width * 0.5f;

            // An arc as in the reference: Sprig at the left, Bloom raised behind the fountain, Drop, and Twig at the right
            // edge in front. With the guest, the arc moves right and the guest stands at the left front.
            float middle = cx + (width * (guest ? 0.05f : -0.02f));
            var heroes = new List<(Family Family, Box Box)>
            {
                (Family.Bloom, Figure(middle, topY - (ry * 0.62f), hero * 1.02f)),
                (Family.Sprig, Figure(cx - (width * (guest ? 0.25f : 0.32f)), topY - (ry * 0.12f), hero * 0.92f)),
                (Family.Drop, Figure(cx + (width * (guest ? 0.27f : 0.22f)), topY - (ry * 0.25f), hero * 0.9f)),
                (Family.Twig, Figure(cx + (width * (guest ? 0.43f : 0.4f)), topY + (ry * 0.88f), hero * 0.78f)),
            };

            var fountain = Box.FromCenter(middle, topY + (ry * 0.45f), width * 0.34f, width * 0.13f);
            Box guestBox = Figure(cx - (width * 0.46f), topY + (ry * 1.0f), hero * 0.72f);
            return new HomeDiorama(pedestal, fountain, heroes, guestBox);
        }

        /// <summary>
        /// The progressed Home (frame 3): the player's hero, as large as <paramref name="heroArea"/> allows, standing on a
        /// stone pedestal at the bottom of it, and the guest on the grass at its right. Returns the pedestal, the hero's
        /// picture box and the guest's.
        /// </summary>
        public static (Box Pedestal, Box Hero, Box Guest) HeroOnPedestal(Box heroArea)
        {
            float width = Math.Min(heroArea.Width * 0.56f, heroArea.Height * 0.9f);
            float height = width * 0.34f;
            var pedestal = Box.FromCenter(heroArea.CenterX, heroArea.Bottom - (height / 2f), width, height);
            float topY = PedestalTop(pedestal).CenterY;
            float room = topY - heroArea.Top;
            float hero = Math.Min(room / FeetShare, width * 1.25f * CharacterArt.HeroHeight / CharacterArt.HeroWidth);
            Box heroBox = Figure(heroArea.CenterX, topY, hero);
            float guest = hero * 0.5f;
            float guestWidth = guest * CharacterArt.HeroWidth / CharacterArt.HeroHeight;
            float right = Math.Min(heroArea.Right, pedestal.Right + (guestWidth * 0.95f));
            float feet = pedestal.Bottom - (height * 0.04f);
            var guestBox = new Box(right - guestWidth, feet - (guest * FeetShare), right, feet + (guest * (1f - FeetShare)));
            return (pedestal, heroBox, guestBox);
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

    /// <summary>The drawn early Home stage (<see cref="HomeStage.Diorama"/>): heroes in drawing order, back to front.</summary>
    public sealed record HomeDiorama(Box Pedestal, Box Fountain, IReadOnlyList<(Family Family, Box Box)> Heroes, Box Guest);
}
