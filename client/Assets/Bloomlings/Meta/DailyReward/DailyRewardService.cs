using System;
using System.Globalization;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Meta.DailyReward
{
    /// <summary>
    /// The Daily Reward (FR-055, T134): unlocked at L7 (<c>system.daily_reward</c>), one claim per UTC calendar day,
    /// persisted in the save's <c>daily</c> section. A claim pays <c>daily.reward.petals</c> plus
    /// <c>daily.reward.streakBonusPetals</c> for each consecutive day after the first, capped at
    /// <c>daily.reward.streakMaxDays</c>; a missed day starts the streak again. Engine-free, driven by <see cref="IClock"/>.
    /// The claim date only moves forward (<see cref="DailyDates.IsLater"/>): setting the device clock back and forth
    /// gives nothing, as a claim waits until a day after the last claimed one. The day is the UTC day, the same on every
    /// device, so cloud saves from different time zones merge by date.
    /// </summary>
    public sealed class DailyRewardService
    {
        public const string UnlockId = "system.daily_reward";

        private readonly PlayerSave _save;
        private readonly IClock _clock;
        private readonly IRemoteConfigService _config;
        private readonly EconomyService _economy;
        private readonly Action _persist;

        public DailyRewardService(PlayerSave save, IClock clock, IRemoteConfigService config, EconomyService economy, Action persist)
        {
            _save = save;
            _clock = clock;
            _config = config;
            _economy = economy;
            _persist = persist;
        }

        public bool IsUnlocked => _save.Unlocks.IsSet(UnlockId);

        public bool CanClaim => IsUnlocked && DailyDates.IsLater(Today, _save.Daily.RewardLastClaimUtcDate);

        /// <summary>The streak day a claim now would be (1 after a gap or on the first claim).</summary>
        public int NextStreak
        {
            get
            {
                string? last = _save.Daily.RewardLastClaimUtcDate;
                string yesterday = DailyDates.Format(_clock.UtcToday.AddDays(-1));
                return last == yesterday ? _save.Daily.RewardStreak + 1 : 1;
            }
        }

        /// <summary>What a claim now pays.</summary>
        public int NextPetals
        {
            get
            {
                int capped = Math.Min(NextStreak, _config.Get(RemoteConfigKeys.DailyStreakMaxDays));
                return _config.Get(RemoteConfigKeys.DailyRewardPetals) + (_config.Get(RemoteConfigKeys.DailyStreakBonusPetals) * (capped - 1));
            }
        }

        /// <summary>Claims today's reward; returns the Petals paid, or 0 when not claimable.</summary>
        public int Claim()
        {
            if (!CanClaim)
            {
                return 0;
            }

            int petals = NextPetals;
            _save.Daily.RewardStreak = NextStreak;
            _save.Daily.RewardLastClaimUtcDate = Today;
            _economy.Grant(petals, null);
            _persist();
            return petals;
        }

        private string Today => DailyDates.Format(_clock.UtcToday);
    }

    /// <summary>The <c>yyyy-MM-dd</c> UTC dates of the save's <c>daily</c> section.</summary>
    public static class DailyDates
    {
        public static string Format(DateTime utcDate) => utcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Whether <paramref name="today"/> is a later day than <paramref name="last"/> (true when never).</summary>
        public static bool IsLater(string today, string? last) => last == null || string.CompareOrdinal(today, last) > 0;
    }

    /// <summary>
    /// The free-booster rewarded ad on Home (FR-052): once per UTC day, remembered in the save so a relaunch does not
    /// bring it back, and like the Daily Reward it waits for a later day when the device clock is set back.
    /// </summary>
    public sealed class FreeBoosterAd
    {
        private readonly PlayerSave _save;
        private readonly IClock _clock;
        private readonly Action _persist;

        public FreeBoosterAd(PlayerSave save, IClock clock, Action persist)
        {
            _save = save;
            _clock = clock;
            _persist = persist;
        }

        public bool IsAvailable => DailyDates.IsLater(DailyDates.Format(_clock.UtcToday), _save.Daily.FreeBoosterAdUtcDate);

        public void MarkTaken()
        {
            _save.Daily.FreeBoosterAdUtcDate = DailyDates.Format(_clock.UtcToday);
            _persist();
        }
    }
}
