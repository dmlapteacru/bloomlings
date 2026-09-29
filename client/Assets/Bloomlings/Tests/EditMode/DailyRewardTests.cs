using System;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>One Daily Reward claim per UTC calendar day, with a capped streak bonus (FR-055; T125).</summary>
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
            Assert.That(_daily.Claim(), Is.Zero);
        }

        [Test]
        public void OneClaimPerUtcCalendarDay()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;

            Assert.That(_daily.Claim(), Is.EqualTo(20));
            Assert.That(_daily.CanClaim, Is.False);
            Assert.That(_daily.Claim(), Is.Zero);

            _clock.UtcNow = _clock.UtcNow.AddMinutes(31); // 00:01 UTC the next day
            Assert.That(_daily.CanClaim, Is.True);
            Assert.That(_daily.Claim(), Is.EqualTo(25), "day 2 of the streak");
            Assert.That(_economy.Petals, Is.EqualTo(45));
        }

        [Test]
        public void StreakBonus_IsCapped_AndAGapRestartsIt()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            int last = 0;
            for (int day = 0; day < 10; day++)
            {
                last = _daily.Claim();
                _clock.UtcNow = _clock.UtcNow.AddDays(1);
            }

            Assert.That(last, Is.EqualTo(20 + (5 * 6)), "capped at 7 days");

            _clock.UtcNow = _clock.UtcNow.AddDays(1);
            Assert.That(_daily.Claim(), Is.EqualTo(20), "a missed day restarts the streak");
        }

        [Test]
        public void SettingTheClockBackAndForth_GivesNoExtraClaims()
        {
            _save.Unlocks.Flags[DailyRewardService.UnlockId] = true;
            DateTime today = _clock.UtcNow;
            Assert.That(_daily.Claim(), Is.EqualTo(20));

            _clock.UtcNow = today.AddDays(1);
            Assert.That(_daily.Claim(), Is.EqualTo(25), "the clock moved to tomorrow: one claim");

            _clock.UtcNow = today;
            Assert.That(_daily.CanClaim, Is.False, "back to today: already claimed a later day");
            _clock.UtcNow = today.AddDays(-3);
            Assert.That(_daily.CanClaim, Is.False);
            _clock.UtcNow = today.AddDays(1);
            Assert.That(_daily.CanClaim, Is.False, "and forward again: that day was claimed");
            Assert.That(_economy.Petals, Is.EqualTo(45));
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
