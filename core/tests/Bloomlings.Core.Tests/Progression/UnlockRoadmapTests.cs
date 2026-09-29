using System;
using System.Linq;
using Bloomlings.Core.Progression;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Progression
{
    public class UnlockRoadmapTests
    {
        [TestCase("mechanic.key", 8)]
        [TestCase("mechanic.stone", 11)]
        [TestCase("booster.extra_slot", 3)]
        [TestCase("booster.shuffle", 4)]
        [TestCase("profile.hard", 5)]
        [TestCase("booster.return", 6)]
        [TestCase("booster.bloom_burst", 9)]
        [TestCase("profile.super_hard", 10)]
        [TestCase("system.leaderboard", 10)]
        [TestCase("system.store", 12)]
        public void Default_MatchesTheSpecRoadmap(string unlockId, int level)
        {
            Assert.That(UnlockRoadmap.Default.LevelOf(unlockId), Is.EqualTo(level));
        }

        [Test]
        public void Default_HasAtMostOneMechanicPerLevel()
        {
            var perLevel = UnlockRoadmap.Default.Entries.Where(e => e.Kind == UnlockKind.Mechanic).GroupBy(e => e.Level);
            Assert.That(perLevel.All(g => g.Count() == 1), Is.True);
        }

        [Test]
        public void ReachedBetween_IsExclusiveInclusive()
        {
            var reached = UnlockRoadmap.Default.ReachedBetween(9, 11).Select(e => e.UnlockId);

            Assert.That(reached, Is.EqualTo(new[] { "system.leaderboard", "profile.super_hard", "mechanic.stone" }));
        }

        [Test]
        public void IsUnlockedAt_OpensOnTheUnlockLevel()
        {
            Assert.That(UnlockRoadmap.Default.IsUnlockedAt("system.store", 11), Is.False);
            Assert.That(UnlockRoadmap.Default.IsUnlockedAt("system.store", 12), Is.True);
            Assert.That(UnlockRoadmap.Default.IsUnlockedAt("unknown", 1000), Is.False);
        }

        [Test]
        public void TwoMechanicsOnOneLevel_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new UnlockRoadmap(new[]
            {
                new UnlockEntry(8, "mechanic.key", UnlockKind.Mechanic, 0, false),
                new UnlockEntry(8, "mechanic.mystery_pod", UnlockKind.Mechanic, 0, false),
            }));
        }

        [Test]
        public void DemoWindowAboveTwo_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new UnlockRoadmap(new[] { new UnlockEntry(3, "booster.x", UnlockKind.Booster, 3, false) }));
        }
    }
}
