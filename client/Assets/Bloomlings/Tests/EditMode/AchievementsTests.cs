using System;
using System.Linq;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class AchievementsTests
    {
        private static PlayerSave NewSave() => PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));

        [Test]
        public void Catalog_ContainsEveryIllustratedBadge_WithUniqueStableIds()
        {
            Assert.That(Achievements.All.Count, Is.EqualTo(23));
            Assert.That(Achievements.All.Select(a => a.Id).Distinct().Count(), Is.EqualTo(23));
            foreach (AchievementDef def in Achievements.All)
            {
                Assert.That(def.Tiers.Count, Is.GreaterThan(0), def.Id);
                Assert.That(def.Tiers, Is.Ordered.Ascending, def.Id);
                Assert.That(def.Tiers[0], Is.GreaterThan(0), def.Id);
                Assert.That(def.BadgeId, Is.EqualTo("badge.achievement." + def.Id));
            }
        }

        [Test]
        public void OldSave_ReconstructsFirstWinDailyPicturesAndPrestige_WithoutInventingMastery()
        {
            PlayerSave save = NewSave();
            save.Progression.HighestCompletedLevel = 101;
            save.Stats.Increment(Achievements.DailyCounter, 7);
            save.Collection.Add(new CollectionEntry("forest-fox", 1, "a", 1));
            save.Collection.Add(new CollectionEntry("forest-fox", 2, "b", 2));
            save.Collection.Add(new CollectionEntry("apple", 1, "c", 3));
            var states = Achievements.Of(save).ToDictionary(s => s.Def.Id);
            Assert.That(states["first_bloom"].Tier, Is.EqualTo(1));
            Assert.That(states["picture_explorer"].Value, Is.EqualTo(2));
            Assert.That(states["daily_bloom"].Tier, Is.EqualTo(2));
            Assert.That(states["seasoned_gardener"].Tier, Is.EqualTo(1));
            Assert.That(states["natural_talent"].Value, Is.Zero);
        }

        [Test]
        public void PerLevelFacts_CountOncePerLevel_AndUnlockWearableBadge()
        {
            PlayerSave save = NewSave();
            save.Stats.Increment(Achievements.FactPrefix + "key_keeper", "11", 3);
            save.Stats.Increment(Achievements.FactPrefix + "key_keeper", "12", 2);
            var state = Achievements.Of(save).Single(s => s.Def.Id == "key_keeper");
            Assert.That((state.Value, state.Tier, state.Goal), Is.EqualTo((5L, 1, 25L)));
            Assert.That(Achievements.Unlocked(save, "key_keeper"), Is.True);
            Assert.That(Achievements.Unlocked(save, "unknown"), Is.False);
        }

        [Test]
        public void OfflineMerge_UnionsDifferentWinningLevels_WithoutDuplicatingTheSameLevel()
        {
            PlayerSave local = NewSave();
            PlayerSave remote = NewSave();
            local.Stats.Increment(Achievements.FactPrefix + "natural_talent", "11");
            remote.Stats.Increment(Achievements.FactPrefix + "natural_talent", "11");
            remote.Stats.Increment(Achievements.FactPrefix + "natural_talent", "12");
            PlayerSave merged = SaveMerge.Merge(local, remote);
            Assert.That(Achievements.Of(merged).Single(s => s.Def.Id == "natural_talent").Value, Is.EqualTo(2));
        }
    }
}
