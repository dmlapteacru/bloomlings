using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Meta.DailyChallenge
{
    /// <summary>
    /// The Daily Challenge (FR-064, research R19, T144): one optional puzzle per UTC day, the same for every player. The
    /// puzzles come from the daily pack that <c>publish</c> writes; the UTC date maps to a pool index by whole days
    /// since 2026-01-01, modulo the pool size, so every device picks the same puzzle offline. It unlocks at L50
    /// (<c>system.daily_challenge</c>) behind the remote flag <c>feature.dailyChallenge</c>, pays its own reward once per
    /// day, and never changes Level N. Engine-free.
    /// </summary>
    public sealed class DailyChallengeService
    {
        public const string UnlockId = "system.daily_challenge";

        /// <summary>
        /// The separate daily reward, in Petals. REMOTE-CONFIG-DEFERRED: local on purpose; whether it becomes a Remote
        /// Config key (FR-085) is decided at the end (tasks.md, "Local values, Remote Config decided at the end").
        /// </summary>
        public const int RewardPetals = 30;

        public static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly PlayerSave _save;
        private readonly IClock _clock;
        private readonly IRemoteConfigService _config;
        private readonly CatalogService _catalog;
        private readonly EconomyService _economy;
        private readonly Action _persist;

        public DailyChallengeService(PlayerSave save, IClock clock, IRemoteConfigService config, CatalogService catalog, EconomyService economy, Action persist)
        {
            _save = save;
            _clock = clock;
            _config = config;
            _catalog = catalog;
            _economy = economy;
            _persist = persist;
        }

        public bool IsUnlocked => _save.Unlocks.IsSet(UnlockId);

        /// <summary>Unlocked, enabled remotely, and the content has a daily pool.</summary>
        public bool IsAvailable => IsUnlocked && _config.Get(RemoteConfigKeys.DailyChallengeEnabled) && Pool.Count > 0;

        public string Today => FormatDate(_clock.UtcToday);

        public bool CompletedToday => _save.Daily.ChallengeLastCompletedUtcDate == Today;

        private IReadOnlyList<DailyPoolEntry> Pool => _catalog.Content.DailyPool;

        /// <summary>The pool index of a UTC date: whole days since 2026-01-01, modulo the pool size (R19).</summary>
        public static int PoolIndex(DateTime utcDate, int poolSize)
        {
            if (poolSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(poolSize), "The daily pool is empty.");
            }

            DateTime date = utcDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(utcDate, DateTimeKind.Utc) : utcDate.ToUniversalTime();
            long days = (date.Date.Ticks - Epoch.Ticks) / TimeSpan.TicksPerDay;
            return (int)(((days % poolSize) + poolSize) % poolSize);
        }

        /// <summary>Today's puzzle, pinned to the current content version; null when the challenge is not available.</summary>
        public LevelAttempt? BeginAttempt()
        {
            if (!IsAvailable)
            {
                return null;
            }

            ContentSet content = _catalog.Content;
            DailyPoolEntry entry = content.DailyPool[PoolIndex(_clock.UtcToday, content.DailyPool.Count)];
            return new LevelAttempt(
                entry.Level.LevelNumber,
                entry.Level,
                content.GetPicture(entry.Level.Picture),
                new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget),
                Today);
        }

        /// <summary>
        /// Records a won daily attempt and pays its reward once per date; returns the Petals paid (0 when that date was
        /// already completed). Level N is not touched.
        /// </summary>
        public int Complete(LevelAttempt attempt)
        {
            string? date = attempt.DailyUtcDate;
            if (date == null || string.CompareOrdinal(date, _save.Daily.ChallengeLastCompletedUtcDate ?? string.Empty) <= 0)
            {
                return 0;
            }

            _save.Daily.ChallengeLastCompletedUtcDate = date;
            _save.Stats.Increment("dailyChallengesWon");
            _economy.Grant(RewardPetals, null);
            _persist();
            return RewardPetals;
        }

        private static string FormatDate(DateTime utcDate) => utcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
