using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Daily Reward's five steps a UTC calendar day, claimed in order, two as they are and three after a rewarded ad
    /// (FR-055 as amended on 2026-10-09; T125), and Home's "!" until the card opens that day (spec 005 FR-050).
    /// </summary>
    public class DailyRewardTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime UtcNow { get; set; }

            public DateTime UtcToday => UtcNow.Date;
        }

        private FakeClock _clock = null!;
        private PlayerSave _save = null!;
        private EconomyService _economy = null!;
        private DailyRewardService _daily = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock { UtcNow = new DateTime(2026, 9, 29, 23, 30, 0, DateTimeKind.Utc) };
            _save = PlayerSave.CreateNew("p1", _clock.UtcNow);
            _economy = new EconomyService(_save, EconomyConfig.Bundled, () => { });
            _daily = new DailyRewardService(_save, _clock, new BundledRemoteConfigService(), _economy, () => { });
        }

        [Test]
        public void LockedBeforeLevel7()
        {
            Assert.That(_daily.CanClaim, Is.False);
            Assert.That(_daily.ShowsBadge, Is.False, "no \"!\" before the unlock");
            Assert.That(_daily.Claim(1), Is.Zero);
            Assert.That(_daily.StateOf(1), Is.EqualTo(DailyStepState.Locked));
        }

        [Test]
        public void FiveStepsADay_TheOwnersAmounts_TwoFreeAndThreeAds()
        {
            IReadOnlyList<DailyRewardStep> steps = _daily.Steps;
            Assert.That(steps.Select(s => s.Petals), Is.EqualTo(new[] { 20, 30, 40, 50, 80 }));
            Assert.That(steps.Select(s => s.Ad), Is.EqualTo(new[] { false, true, true, false, true }));
            Assert.That(steps.Select(s => s.Number), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void TheSteps_AreClaimedInOrder_AnAdStepOnlyAfterItsAd()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            Assert.That(_daily.StateOf(1), Is.EqualTo(DailyStepState.Ready));
            Assert.That(_daily.StateOf(2), Is.EqualTo(DailyStepState.Locked));
            Assert.That(_daily.Claim(2, adWatched: true), Is.Zero, "step 2 waits for step 1");

            Assert.That(_daily.Claim(1), Is.EqualTo(20));
            Assert.That(_daily.Claim(1), Is.Zero, "a double tap pays once");
            Assert.That(_daily.StateOf(1), Is.EqualTo(DailyStepState.Claimed));
            Assert.That(_daily.StateOf(2), Is.EqualTo(DailyStepState.Ready));

            Assert.That(_daily.Claim(2), Is.Zero, "an ad step without its ad");
            Assert.That(_daily.Claim(2, adWatched: true), Is.EqualTo(30));
            Assert.That(_daily.Claim(3, adWatched: true), Is.EqualTo(40));
            Assert.That(_daily.Claim(4), Is.EqualTo(50), "step 4 is claimed as it is");
            Assert.That(_daily.CanClaim, Is.True);
            Assert.That(_daily.Claim(5, adWatched: true), Is.EqualTo(80));

            Assert.That(_daily.CanClaim, Is.False, "all five claimed today");
            Assert.That(_daily.NextStep, Is.Null);
            Assert.That(_daily.ClaimedToday, Is.EqualTo(5));
            Assert.That(_economy.Petals, Is.EqualTo(220));
        }

        [Test]
        public void TheNextDay_StartsAgainFromStep1_WhateverWasLeft()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            _daily.Claim(1);
            _daily.Claim(2, adWatched: true);
            Assert.That(_daily.ClaimedToday, Is.EqualTo(2));
            Assert.That(_daily.Streak, Is.EqualTo(1));

            _clock.UtcNow = _clock.UtcNow.AddMinutes(31); // 00:01 UTC the next day
            Assert.That(_daily.ClaimedToday, Is.Zero, "the progress resets");
            Assert.That(_daily.StateOf(1), Is.EqualTo(DailyStepState.Ready));
            Assert.That(_daily.Claim(3, adWatched: true), Is.Zero, "yesterday's step 3 is gone");
            Assert.That(_daily.Claim(1), Is.EqualTo(20));
            Assert.That(_daily.Streak, Is.EqualTo(2), "two days in a row");

            _clock.UtcNow = _clock.UtcNow.AddDays(2);
            _daily.Claim(1);
            Assert.That(_daily.Streak, Is.EqualTo(1), "a missed day starts the streak again");
        }

        [Test]
        public void SettingTheClockBackAndForth_GivesNoExtraClaims()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            DateTime today = _clock.UtcNow;
            Assert.That(_daily.Claim(1), Is.EqualTo(20));

            _clock.UtcNow = today.AddDays(1);
            Assert.That(_daily.Claim(1), Is.EqualTo(20), "the clock moved to tomorrow: that day's steps");

            _clock.UtcNow = today;
            Assert.That(_daily.CanClaim, Is.False, "back to today: a later day was claimed");
            Assert.That(_daily.StateOf(2), Is.EqualTo(DailyStepState.Claimed));
            _clock.UtcNow = today.AddDays(-3);
            Assert.That(_daily.CanClaim, Is.False);
            _clock.UtcNow = today.AddDays(1);
            Assert.That(_daily.ClaimedToday, Is.EqualTo(1), "and forward again: that day's step 1 was claimed");
            Assert.That(_daily.Claim(1), Is.Zero);
            Assert.That(_economy.Petals, Is.EqualTo(40));
        }

        [Test]
        public void TheBadge_ShowsUntilTheCardOpens_ThenComesBackTheNextDay()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            Assert.That(_daily.ShowsBadge, Is.True, "a step waits and the card was not opened today");
            _daily.MarkSeen();
            Assert.That(_daily.ShowsBadge, Is.False, "opened: gone for the day");
            Assert.That(_daily.CanClaim, Is.True, "the steps still wait");

            var relaunched = new DailyRewardService(SaveMerge.Clone(_save), _clock, new BundledRemoteConfigService(), _economy, () => { });
            Assert.That(relaunched.ShowsBadge, Is.False, "the day is in the save");

            _clock.UtcNow = _clock.UtcNow.AddDays(1);
            Assert.That(_daily.ShowsBadge, Is.True, "the next day it is back");
            for (int step = 1; step <= DailyRewardService.StepCount; step++)
            {
                _daily.Claim(step, adWatched: true);
            }

            Assert.That(_daily.ShowsBadge, Is.False, "nothing left to claim: no badge, even unopened");
            _clock.UtcNow = _clock.UtcNow.AddDays(-2);
            Assert.That(_daily.SeenToday, Is.True, "the clock set back before the day it was opened: seen");
        }

        [Test]
        public void TheCaption_CountsTheMinutesToMidnightUtc()
        {
            Assert.That(_daily.MinutesToNextDay, Is.EqualTo(30), "23:30 UTC");
            Assert.That(DailyDates.MinutesToNextDay(new DateTime(2026, 9, 29, 23, 59, 30, DateTimeKind.Utc)), Is.EqualTo(1));
            Assert.That(DailyDates.MinutesToNextDay(new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc)), Is.EqualTo(24 * 60));
        }

        [Test]
        public void AnOlderSave_WithItsOneClaimToday_HasStep1Claimed()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            _daily.Claim(1);
            JObject document = SaveSerializer.ToJObject(_save);
            var daily = (JObject)document["daily"]!;
            daily.Remove("rewardClaimed");
            daily.Remove("rewardSeenUtcDate");
            PlayerSave older = SaveSerializer.Read(document);
            Assert.That(older.Daily.RewardClaimed, Is.EqualTo(1));
            var service = new DailyRewardService(older, _clock, new BundledRemoteConfigService(), _economy, () => { });
            Assert.That(service.StateOf(2), Is.EqualTo(DailyStepState.Ready));
            Assert.That(service.ShowsBadge, Is.True);
        }

        [Test]
        public void TheFreeBoosterAd_IsOnceADay_AndSurvivesARelaunch()
        {
            var offer = new FreeBoosterAd(_save, _clock, () => { });
            Assert.That(offer.IsAvailable, Is.True);
            offer.MarkTaken();

            var relaunched = new FreeBoosterAd(SaveMerge.Clone(_save), _clock, () => { });
            Assert.That(relaunched.IsAvailable, Is.False, "the date is in the save");

            _clock.UtcNow = _clock.UtcNow.AddDays(-1);
            Assert.That(offer.IsAvailable, Is.False, "a clock set back waits for a later day");
            _clock.UtcNow = _clock.UtcNow.AddDays(2);
            Assert.That(offer.IsAvailable, Is.True);
        }
    }
}
