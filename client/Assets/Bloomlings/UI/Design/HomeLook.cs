using System;
using System.Collections.Generic;

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
}
