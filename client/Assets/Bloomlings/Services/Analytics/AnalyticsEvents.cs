using System.Collections.Generic;

namespace Bloomlings.Client.Services.Analytics
{
    /// <summary>
    /// The event catalog of contracts/analytics-events.md: each event name with its extra parameters and whether the level
    /// parameters are attached. A test keeps this table equal to the contract.
    /// </summary>
    public static class AnalyticsEvents
    {
        public const string LevelStart = "level_start";
        public const string LevelWin = "level_win";
        public const string LevelJam = "level_jam";
        public const string LevelRecover = "level_recover";
        public const string LevelRestart = "level_restart";
        public const string LevelQuit = "level_quit";
        public const string BoosterUse = "booster_use";
        public const string TutorialStep = "tutorial_step";
        public const string Unlock = "unlock";
        public const string MilestoneClaim = "milestone_claim";
        public const string DailyRewardClaim = "daily_reward_claim";
        public const string DailyChallengeComplete = "daily_challenge_complete";
        public const string LeaderboardView = "leaderboard_view";
        public const string CosmeticEquip = "cosmetic_equip";
        public const string CollectionOpen = "collection_open";
        public const string StoreOpen = "store_open";
        public const string Purchase = "purchase";
        public const string AdRewarded = "ad_rewarded";
        public const string AdInterstitial = "ad_interstitial";
        public const string Consent = "consent";
        public const string ContentUpdate = "content_update";
        public const string ContentError = "content_error";

        /// <summary>Common parameters on every event.</summary>
        public static readonly string[] Common = { "app_version", "content_version", "player_level", "session_id" };

        /// <summary>Level parameters, attached where a level is involved.</summary>
        public static readonly string[] Level = { "level_number", "definition_version", "difficulty_class", "picture_id" };

        /// <summary>Event → extra parameters.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Extra = new Dictionary<string, string[]>
        {
            [LevelStart] = new[] { "attempt_index" },
            [LevelWin] = new[] { "duration_ms", "taps", "boosters_used", "clean_clear", "peak_slots", "attempt_index" },
            [LevelJam] = new[] { "kind", "duration_ms", "taps", "slots_used", "remaining_work" },
            [LevelRecover] = new[] { "method" },
            [LevelRestart] = new[] { "from" },
            [LevelQuit] = new[] { "duration_ms" },
            [BoosterUse] = new[] { "booster", "source" },
            [TutorialStep] = new[] { "unlock_id", "step", "completed" },
            [Unlock] = new[] { "unlock_id", "kind" },
            [MilestoneClaim] = new[] { "milestone_level", "bundle_id" },
            [DailyRewardClaim] = new[] { "step", "ad", "streak" },
            [DailyChallengeComplete] = new[] { "utc_date" },
            [LeaderboardView] = new[] { "rank" },
            [CosmeticEquip] = new[] { "family", "skin_id" },
            [CollectionOpen] = new[] { "entries" },
            [StoreOpen] = new[] { "from" },
            [Purchase] = new[] { "product_id", "price_micros", "currency", "transaction_id" },
            [AdRewarded] = new[] { "placement" },
            [AdInterstitial] = new[] { "levels_since_last", "seconds_since_last" },
            [Consent] = new[] { "gdpr", "att" },
            [ContentUpdate] = new[] { "from_version", "to_version" },
            [ContentError] = new[] { "pack_id", "level_number", "error" },
        };

        /// <summary>Events that carry the level parameters.</summary>
        public static readonly ISet<string> WithLevel = new HashSet<string>
        {
            LevelStart, LevelWin, LevelJam, LevelRecover, LevelRestart, LevelQuit, BoosterUse, TutorialStep,
        };
    }
}
