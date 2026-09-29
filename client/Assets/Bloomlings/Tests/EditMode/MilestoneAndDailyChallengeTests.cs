using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.DailyChallenge;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Milestones are granted once, largest cadence only (FR-061); the daily pool index is device-independent (R19). T137.</summary>
    public class MilestoneAndDailyChallengeTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime UtcNow { get; set; }

            public DateTime UtcToday => UtcNow.Date;
        }

        private sealed class FlagConfig : IRemoteConfigService
        {
            private readonly BundledRemoteConfigService _defaults = new BundledRemoteConfigService();

            public bool DailyChallenge { get; set; } = true;

            public int Get(IntKey key) => _defaults.Get(key);

            public bool Get(BoolKey key) => key == RemoteConfigKeys.DailyChallengeEnabled ? DailyChallenge : _defaults.Get(key);

            public string Get(StringKey key) => _defaults.Get(key);
        }

        private PlayerSave _save = null!;
        private EconomyService _economy = null!;
        private MilestoneService _milestones = null!;

        [SetUp]
        public void SetUp()
        {
            _save = PlayerSave.CreateNew("p1", new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc));
            _economy = new EconomyService(_save, EconomyConfig.Bundled, () => { });
            foreach ((string unlockId, BoosterKind _) in EconomyService.BoosterUnlocks)
            {
                _save.Unlocks.Flags[unlockId] = true;
            }

            _milestones = new MilestoneService(_save, MilestoneTable.Default, _economy, () => { });
        }

        [Test]
        public void EachMilestone_IsGrantedExactlyOnce()
        {
            MilestoneGrant? first = _milestones.OnLevelCompleted(25);

            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Cadence.Tier, Is.EqualTo(MilestoneTier.Bundle));
            Assert.That(_economy.Petals, Is.EqualTo(50));
            Assert.That(_milestones.OnLevelCompleted(25), Is.Null, "a second completion of L25 grants nothing");
            Assert.That(_economy.Petals, Is.EqualTo(50));
            Assert.That(_milestones.OnLevelCompleted(26), Is.Null, "L26 is not a milestone");
            Assert.That(_save.Milestones.Claimed, Is.EqualTo(new[] { 25 }));
        }

        [Test]
        public void L100_GrantsOnlyTheLargestCadence()
        {
            MilestoneGrant grant = _milestones.OnLevelCompleted(100)!;

            Assert.That(grant.Cadence.Every, Is.EqualTo(100));
            Assert.That(_economy.Petals, Is.EqualTo(300), "not 50 + 100 + 300");
            Assert.That(grant.Boosters, Is.EqualTo(new BoosterGrant(1, 1, 1, 1)), "one charge of each booster");
            Assert.That(grant.Item, Is.EqualTo("hat.straw_hat"));
            Assert.That(_save.Cosmetics.Owned, Does.Contain("hat.straw_hat"));
            Assert.That(MilestoneTable.Default.CadenceFor(1000)!.Every, Is.EqualTo(1000));
            Assert.That(MilestoneTable.Default.CadenceFor(750)!.Every, Is.EqualTo(250));
            Assert.That(MilestoneTable.Default.CadenceFor(1500)!.Every, Is.EqualTo(500));
        }

        [Test]
        public void Items_SkipWhatIsOwned_AndTheBundleBoosterGoesToTheFewestCharges()
        {
            _save.Cosmetics.Owned.Add("hat.leaf_cap");
            Assert.That(_milestones.OnLevelCompleted(50)!.Item, Is.EqualTo("trail.petal_sparkle"));

            _save.Boosters.Add(BoosterKind.ExtraSlot, 3);
            _save.Boosters.Add(BoosterKind.Shuffle, 3);
            _save.Boosters.Add(BoosterKind.BloomBurst, 3);
            Assert.That(_milestones.OnLevelCompleted(75)!.Boosters, Is.EqualTo(new BoosterGrant(0, 0, 1, 0)));
        }

        [Test]
        public void NextMilestone_FeedsTheHomeTeaser()
        {
            (int level, MilestoneCadence cadence, int toGo) = _milestones.Next(88)!.Value;
            Assert.That((level, cadence.Every, toGo), Is.EqualTo((100, 100, 12)), "\"Level 100 reward in 12\"");
            Assert.That(_milestones.Next(0)!.Value.Level, Is.EqualTo(25));
            Assert.That(_milestones.Next(100)!.Value.Level, Is.EqualTo(125));
        }

        [Test]
        public void DefaultTable_MirrorsTheRoadmapFile()
        {
            string path = Path.Combine(DevContent.RepositoryContentFolder, "roadmap", "milestones.json");
            MilestoneTable file = MilestoneTable.Parse(File.ReadAllText(path));

            Assert.That(file.Cadences.Count, Is.EqualTo(MilestoneTable.Default.Cadences.Count));
            for (int i = 0; i < file.Cadences.Count; i++)
            {
                MilestoneCadence a = file.Cadences[i];
                MilestoneCadence b = MilestoneTable.Default.Cadences[i];
                Assert.That((a.Every, a.Tier, a.Petals, a.BoosterCharges, a.EachBooster), Is.EqualTo((b.Every, b.Tier, b.Petals, b.BoosterCharges, b.EachBooster)));
                Assert.That(a.Items, Is.EqualTo(b.Items), $"items of cadence {a.Every}");
            }
        }

        [Test]
        public void DailyPoolIndex_IsTheSameOnEveryDevice()
        {
            Assert.That(DailyChallengeService.PoolIndex(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 30), Is.EqualTo(0));
            Assert.That(DailyChallengeService.PoolIndex(new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc), 30), Is.EqualTo(0), "day 30 wraps");
            Assert.That(DailyChallengeService.PoolIndex(new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc), 365), Is.EqualTo(271));
            Assert.That(DailyChallengeService.PoolIndex(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), 30), Is.EqualTo(29), "dates before the epoch wrap too");

            // The same instant seen from two time zones is one UTC date, so one index.
            var tokyo = new DateTimeOffset(2026, 9, 30, 8, 0, 0, TimeSpan.FromHours(9));
            var losAngeles = new DateTimeOffset(2026, 9, 29, 16, 0, 0, TimeSpan.FromHours(-7));
            Assert.That(DailyChallengeService.PoolIndex(tokyo.UtcDateTime, 97), Is.EqualTo(DailyChallengeService.PoolIndex(losAngeles.UtcDateTime, 97)));
        }

        [Test]
        public void DailyChallenge_HasItsOwnReward_AndNeverChangesLevelN()
        {
            var clock = new FakeClock { UtcNow = new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc) };
            var config = new FlagConfig();
            var daily = new DailyChallengeService(_save, clock, config, new CatalogService(WithDailyPool(3)), _economy, () => { });
            _save.Progression.HighestCompletedLevel = 60;

            Assert.That(daily.IsAvailable, Is.False, "locked before L50");
            _save.Unlocks.Flags[DailyChallengeService.UnlockId] = true;
            config.DailyChallenge = false;
            Assert.That(daily.IsAvailable, Is.False, "switched off by feature.dailyChallenge");
            config.DailyChallenge = true;

            LevelAttempt attempt = daily.BeginAttempt()!;
            Assert.That(attempt.IsDaily, Is.True);
            Assert.That(attempt.DailyUtcDate, Is.EqualTo("2026-09-29"));
            Assert.That(attempt.Definition.LevelNumber, Is.EqualTo(DailyChallengeService.PoolIndex(clock.UtcToday, 3) + 1));

            Assert.That(daily.Complete(attempt), Is.EqualTo(DailyChallengeService.RewardPetals));
            Assert.That(daily.Complete(attempt), Is.Zero, "one reward per day");
            Assert.That(daily.CompletedToday, Is.True);
            Assert.That(_save.Progression.HighestCompletedLevel, Is.EqualTo(60));

            clock.UtcNow = clock.UtcNow.AddDays(1);
            Assert.That(daily.CompletedToday, Is.False);
            Assert.That(daily.Complete(daily.BeginAttempt()!), Is.EqualTo(DailyChallengeService.RewardPetals));
        }

        /// <summary>The curated levels, with the first <paramref name="count"/> of them also as the daily pool.</summary>
        private static ContentSet WithDailyPool(int count)
        {
            ContentSet curated = DevContent.LoadCurated();
            List<LevelDefinition> levels = curated.LevelNumbers.Select(curated.GetLevel).ToList();
            var pictures = levels
                .Select(l => curated.GetPicture(l.Picture))
                .GroupBy(p => (p.Id, p.Version))
                .Select(g => g.First())
                .ToList();
            IEnumerable<DailyPoolEntry> pool = levels.Take(count).Select((l, i) => new DailyPoolEntry(i, l with { LevelNumber = i + 1 }));
            return new ContentSet(1, curated.ShuffleNodeBudget, levels, pictures, pool);
        }
    }
}
