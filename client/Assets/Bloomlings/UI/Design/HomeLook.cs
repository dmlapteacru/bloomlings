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
        /// The reference Home's diorama (spec 005 FR-024, contracts/look.md §6.4; measured on the reference's Home) in
        /// <paramref name="stage"/>, Home's diorama region (<see cref="ReferenceHomeRegions.Diorama"/>): the well's stone
        /// ring 0.78 u wide near the stage's bottom, the lotus fountain on it, and the four heroes as large as the
        /// reference's around it: Bloom raised behind the fountain, Drop at the right back, Sprig at the left, Twig in front
        /// at the right; with <paramref name="guest"/>, the guest small at the left front. u is 0.88 of the stage's width,
        /// or its height over 1.09 (the reference stage's shape) on a shorter stage, so the heroes (their pictures less a
        /// 4% margin) stay inside it. Heroes are listed back to front, the fountain going before the fourth (as
        /// <see cref="Diorama"/>); hero boxes are 512 × 576 pictures whose feet stand at <see cref="FeetShare"/>.
        /// </summary>
        public static HomeDiorama ReferenceDiorama(Box stage, bool guest = true)
        {
            float u = Math.Min(stage.Width * 0.88f, stage.Height / 1.09f);
            float cx = stage.CenterX;
            float Y(float up) => stage.Bottom - (up * u);
            var pedestal = new Box(cx - (0.39f * u), Y(0.385f), cx + (0.39f * u), Y(0.091f));
            var heroes = new List<(Family Family, Box Box)>
            {
                (Family.Bloom, Figure(cx + (0.06f * u), Y(0.45f), 0.7f * u)),
                (Family.Drop, Figure(cx + (0.21f * u), Y(0.43f), 0.5f * u)),
                (Family.Sprig, Figure(cx - (0.21f * u), Y(0.317f), 0.8f * u)),
                (Family.Twig, Figure(cx + (0.36f * u), Y(0.22f), 0.51f * u)),
            };

            var fountain = Box.FromCenter(cx, Y(0.364f), 0.31f * u, 0.12f * u);
            Box guestBox = Figure(cx - (0.4f * u), Y(0f), 0.34f * u);
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

        /// <summary>The lotus fountain's middle in the owner's Home picture (pictures.md B1), as shares of its width and height.</summary>
        public const float FountainX = 0.5f;

        /// <summary><see cref="FountainX"/>'s height share.</summary>
        public const float FountainY = 0.455f;

        /// <summary>The lotus's width as a share of the owner's Home picture's width: the unit of <see cref="AroundFountain"/>.</summary>
        public const float LotusShare = 0.3f;

        /// <summary>A hero's head top below its picture box's top, as a share of the box's height (the owner's heroes).</summary>
        public const float HeadTopShare = 0.12f;

        /// <summary>
        /// The four solo heroes around the lotus fountain of the owner's Home picture (pictures.md B1; the reference's Home),
        /// drawn back to front: Bloom raised behind the lotus, Drop at the right back, Sprig large at the left front and Twig
        /// at the right front. The picture (<paramref name="picW"/> × <paramref name="picH"/>) is cover-fitted and centered
        /// on <paramref name="screen"/> as the backdrop draws it; the anchor F is the lotus's middle
        /// (<see cref="FountainX"/>, <see cref="FountainY"/>) and the unit L the lotus's width (<see cref="LotusShare"/> of the
        /// drawn picture). Each box is a <see cref="Figure"/> (feet x, feet y, height): Bloom (F.x, F.y − 0.27 L, 1.47 L),
        /// Drop (F.x + 0.70 L, F.y + 0.55 L, 1.30 L), Sprig (F.x − 0.97 L, F.y + 0.86 L, 1.81 L), Twig (F.x + 1.13 L,
        /// F.y + 1.05 L, 1.40 L). When a head top (<see cref="HeadTopShare"/> below a box's top) would rise above
        /// <paramref name="ceiling"/> (the logo's bottom), all four boxes shrink about F until it does not.
        /// </summary>
        public static IReadOnlyList<(Family Family, Box Box)> AroundFountain(Box screen, int picW = 852, int picH = 1846, float? ceiling = null)
        {
            float s = Math.Max(screen.Width / Math.Max(1f, picW), screen.Height / Math.Max(1f, picH));
            float width = picW * s;
            float height = picH * s;
            float fx = screen.CenterX - (width / 2f) + (FountainX * width);
            float fy = screen.CenterY - (height / 2f) + (FountainY * height);
            float l = LotusShare * width;
            var heroes = new List<(Family Family, Box Box)>
            {
                (Family.Bloom, Figure(fx, fy - (0.27f * l), 1.47f * l)),
                (Family.Drop, Figure(fx + (0.7f * l), fy + (0.55f * l), 1.3f * l)),
                (Family.Sprig, Figure(fx - (0.97f * l), fy + (0.86f * l), 1.81f * l)),
                (Family.Twig, Figure(fx + (1.13f * l), fy + (1.05f * l), 1.4f * l)),
            };

            if (ceiling.HasValue)
            {
                float top = float.MaxValue;
                foreach ((Family _, Box box) in heroes)
                {
                    top = Math.Min(top, box.Top + (box.Height * HeadTopShare));
                }

                if (top < ceiling.Value && fy > ceiling.Value)
                {
                    float k = (fy - ceiling.Value) / Math.Max(1f, fy - top);
                    for (int i = 0; i < heroes.Count; i++)
                    {
                        Box b = heroes[i].Box;
                        heroes[i] = (heroes[i].Family, new Box(fx + ((b.Left - fx) * k), fy + ((b.Top - fy) * k), fx + ((b.Right - fx) * k), fy + ((b.Bottom - fy) * k)));
                    }
                }
            }

            return heroes;
        }
    }

    /// <summary>The drawn early Home stage (<see cref="HomeStage.Diorama"/>): heroes in drawing order, back to front.</summary>
    public sealed record HomeDiorama(Box Pedestal, Box Fountain, IReadOnlyList<(Family Family, Box Box)> Heroes, Box Guest);
}
