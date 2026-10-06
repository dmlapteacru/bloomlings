using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The profile's achievements (spec 005 FR-037 as amended on 2026-10-06).</summary>
    public class AchievementsTests
    {
        [Test]
        public void ThereAreThreeAchievements_EachWithBronzeSilverAndGold()
        {
            Assert.That(Achievements.All.Count, Is.EqualTo(ReferenceProfileRegions.AchievementCount), "one per tile");
            Assert.That(Achievements.All.Select(a => a.Kind), Is.EqualTo(new[] { AchievementKind.LevelsWon, AchievementKind.Pictures, AchievementKind.DailyChallenges }));
            foreach (AchievementDef def in Achievements.All)
            {
                Assert.That(def.Tiers.Count, Is.EqualTo(3), def.NameKey);
                Assert.That(def.Tiers, Is.Ordered.Ascending, def.NameKey);
                Assert.That(def.Tiers[0], Is.GreaterThan(0), def.NameKey);
            }

            Assert.That(AchievementLook.TierColor(0), Is.Null);
            Assert.That(AchievementLook.TierColor(1), Is.EqualTo(DesignTokens.Colors.MedalBronze));
            Assert.That(AchievementLook.TierColor(2), Is.EqualTo(DesignTokens.Colors.MedalSilver));
            Assert.That(AchievementLook.TierColor(3), Is.EqualTo(DesignTokens.Colors.MedalGold));
        }

        [Test]
        public void AState_CountsTowardItsNextTier_AndEndsAtGold()
        {
            AchievementDef levels = Achievements.All[0];
            var none = new AchievementState(levels, 14);
            Assert.That((none.Tier, none.Goal, none.Shown, none.Complete), Is.EqualTo((0, 50L, 14L, false)));

            var bronze = new AchievementState(levels, 50);
            Assert.That((bronze.Tier, bronze.Goal), Is.EqualTo((1, 500L)), "bronze at exactly its count");

            var silver = new AchievementState(levels, 1200);
            Assert.That((silver.Tier, silver.Goal, silver.Shown), Is.EqualTo((2, 2500L, 1200L)));

            var gold = new AchievementState(levels, 3000);
            Assert.That((gold.Tier, gold.Goal, gold.Shown, gold.Complete), Is.EqualTo((3, 2500L, 2500L, true)));
            Assert.That(new AchievementState(levels, -5).Value, Is.EqualTo(0));
        }

        [Test]
        public void TheStates_ComeFromTheSavesCounters()
        {
            PlayerSave save = PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));
            Assert.That(Achievements.Count(save, Achievements.DailyCounter), Is.EqualTo(0));
            save.Stats.Increment(Achievements.LevelsCounter, 60);
            save.Stats.Increment(Achievements.DailyCounter, 7);
            IReadOnlyList<AchievementState> states = Achievements.Of(Achievements.Count(save, Achievements.LevelsCounter), 3, Achievements.Count(save, Achievements.DailyCounter));
            Assert.That(states.Select(s => s.Tier), Is.EqualTo(new[] { 1, 0, 1 }));
            Assert.That(states[1].Goal, Is.EqualTo(10));
        }
    }
}
