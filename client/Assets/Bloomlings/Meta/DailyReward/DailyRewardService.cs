using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Meta.DailyReward
{
    /// <summary>What a Daily Reward step shows on today's card.</summary>
    public enum DailyStepState
    {
        /// <summary>Paid today: the green check.</summary>
        Claimed,

        /// <summary>The next one: Claim, or the ad button of an ad step.</summary>
        Ready,

        /// <summary>After the next one: the padlock, until the steps before it are claimed.</summary>
        Locked,
    }

    /// <summary>One of the Daily Reward's steps (FR-055 as amended on 2026-10-09): its number from 1, its Petals and whether a rewarded ad pays it.</summary>
    public readonly struct DailyRewardStep
    {
        public DailyRewardStep(int number, int petals, bool ad)
        {
            Number = number;
            Petals = petals;
            Ad = ad;
        }

        public int Number { get; }

        public int Petals { get; }

        /// <summary>Paid after a rewarded ad the player starts (FR-052); else claimed as it is.</summary>
        public bool Ad { get; }
    }

    /// <summary>
    /// The Daily Reward (FR-055 as amended on 2026-10-09 by the product owner, T134): unlocked at L7
    /// (<c>system.daily_reward</c>), five steps every UTC calendar day, claimed in order: 1 claimed as it is for
    /// <c>daily.reward.step1</c> Petals (20), 2 and 3 after a rewarded ad for <c>step2</c> and <c>step3</c> (30, 40), 4 claimed
    /// as it is for <c>step4</c> (50) and 5 after an ad for <c>step5</c> (80). The next day the card starts again from step 1,
    /// whatever was left. Persisted in the save's <c>daily</c> section: the day of the last claim, the steps claimed on it
    /// and the day the card was last opened, which hides Home's "!" until the next day (spec 005 FR-050). Engine-free,
    /// driven by <see cref="IClock"/>. The claim date only moves forward (<see cref="DailyDates.IsLater"/>): with the device
    /// clock set back before the last claim nothing is claimable until a later day, and setting it forth again gives the
    /// steps of that day once. The day is the UTC day, the same on every device, so cloud saves from different time zones
    /// merge by date. <see cref="Streak"/> still counts the days in a row with a claim, for analytics.
    /// </summary>
    public sealed class DailyRewardService
    {
        public const string UnlockId = "system.daily_reward";

        /// <summary>The steps of a day.</summary>
        public const int StepCount = 5;

        /// <summary>The steps a rewarded ad pays: 2, 3 and 5 (the owner, 2026-10-09).</summary>
        private static readonly bool[] AdSteps = { false, true, true, false, true };

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

        /// <summary>
        /// The stand-in for the steps' rewarded ads until they are wired (the owner, 2026-10-09: "the ads' reward is not
        /// there yet, put stubs"): when no rewarded ad can show, a host pays an ad step as if its ad was watched. Turn it off
        /// once the ads pay; then an ad step waits for a ready ad.
        /// </summary>
        public static bool AdStub { get; } = true;

        public bool IsUnlocked => _save.Unlocks.IsSet(UnlockId);

        /// <summary>Today's steps, in order.</summary>
        public IReadOnlyList<DailyRewardStep> Steps
        {
            get
            {
                var steps = new DailyRewardStep[StepCount];
                for (int i = 0; i < StepCount; i++)
                {
                    steps[i] = new DailyRewardStep(i + 1, _config.Get(RemoteConfigKeys.DailyRewardSteps[i]), AdSteps[i]);
                }

                return steps;
            }
        }

        /// <summary>
        /// The steps claimed today: 0 on a day without a claim, and all of them while the device clock is set back before
        /// the last claim (nothing is claimable before a later day).
        /// </summary>
        public int ClaimedToday
        {
            get
            {
                string? last = _save.Daily.RewardLastClaimUtcDate;
                if (DailyDates.IsLater(Today, last))
                {
                    return 0;
                }

                return last == Today ? Math.Max(0, Math.Min(StepCount, _save.Daily.RewardClaimed)) : StepCount;
            }
        }

        /// <summary>A step can be claimed now (the unlock reached and a step of today left).</summary>
        public bool CanClaim => IsUnlocked && ClaimedToday < StepCount;

        /// <summary>The step a claim now pays, or null when today's are all claimed.</summary>
        public DailyRewardStep? NextStep => ClaimedToday < StepCount ? Steps[ClaimedToday] : (DailyRewardStep?)null;

        /// <summary>What step <paramref name="number"/> (from 1) shows on today's card.</summary>
        public DailyStepState StateOf(int number)
        {
            int claimed = ClaimedToday;
            return number <= claimed ? DailyStepState.Claimed : number == claimed + 1 && IsUnlocked ? DailyStepState.Ready : DailyStepState.Locked;
        }

        /// <summary>The days in a row with a claim, today's included once claimed (1 after a gap).</summary>
        public int Streak => _save.Daily.RewardStreak;

        /// <summary>Whether the card was opened today (or on a later day, with the device clock set back).</summary>
        public bool SeenToday => _save.Daily.RewardSeenUtcDate != null && !DailyDates.IsLater(Today, _save.Daily.RewardSeenUtcDate);

        /// <summary>Home's "!" on the Daily scene (spec 005 FR-050): a step waits and the card was not opened today.</summary>
        public bool ShowsBadge => CanClaim && !SeenToday;

        /// <summary>The card opened (by a tap on Home's Daily scene): the "!" hides until the next day.</summary>
        public void MarkSeen()
        {
            if (SeenToday)
            {
                return;
            }

            _save.Daily.RewardSeenUtcDate = Today;
            _persist();
        }

        /// <summary>
        /// Claims step <paramref name="number"/> (from 1) when it is the next one; an ad step only with
        /// <paramref name="adWatched"/> (its rewarded ad paid, or <see cref="AdStub"/>). Returns the Petals paid, or 0 when
        /// nothing was claimed (another step is next, the unlock is not reached, or an ad step without its ad), so a late
        /// ad callback or a double tap never pays twice.
        /// </summary>
        public int Claim(int number, bool adWatched = false)
        {
            DailyRewardStep? next = NextStep;
            if (!IsUnlocked || next == null || next.Value.Number != number || (next.Value.Ad && !adWatched))
            {
                return 0;
            }

            string? last = _save.Daily.RewardLastClaimUtcDate;
            if (last != Today)
            {
                // The day's first claim: the streak goes on from yesterday or starts again.
                string yesterday = DailyDates.Format(_clock.UtcToday.AddDays(-1));
                _save.Daily.RewardStreak = last == yesterday ? _save.Daily.RewardStreak + 1 : 1;
                _save.Daily.RewardClaimed = 0;
                _save.Daily.RewardLastClaimUtcDate = Today;
            }

            _save.Daily.RewardClaimed = number;
            _economy.Grant(next.Value.Petals, null);
            _persist();
            return next.Value.Petals;
        }

        /// <summary>The whole minutes until today's steps give way to the next day's (the card's caption).</summary>
        public int MinutesToNextDay => DailyDates.MinutesToNextDay(_clock.UtcNow);

        private string Today => DailyDates.Format(_clock.UtcToday);
    }

    /// <summary>The <c>yyyy-MM-dd</c> UTC dates of the save's <c>daily</c> section.</summary>
    public static class DailyDates
    {
        public static string Format(DateTime utcDate) => utcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Whether <paramref name="today"/> is a later day than <paramref name="last"/> (true when never).</summary>
        public static bool IsLater(string today, string? last) => last == null || string.CompareOrdinal(today, last) > 0;

        /// <summary>The whole minutes until the next UTC day, at least 1 (the Daily Reward's steps come again at midnight UTC).</summary>
        public static int MinutesToNextDay(DateTime utcNow) =>
            Math.Max(1, (int)Math.Ceiling((utcNow.Date.AddDays(1) - utcNow).TotalMinutes));
    }

    /// <summary>
    /// The free-booster rewarded ad on Home (FR-052): once per UTC day, remembered in the save so a relaunch does not
    /// bring it back, and like the Daily Reward it waits for a later day when the device clock is set back.
    /// REMOTE-CONFIG-DEFERRED: the once-a-day cap (and the daily bonus's, one per claim) is local on purpose; whether it
    /// becomes Remote Config (FR-085) is decided at the end (tasks.md, "Local values, Remote Config decided at the end").
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
