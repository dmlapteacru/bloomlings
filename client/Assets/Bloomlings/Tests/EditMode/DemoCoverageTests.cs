using System.Linq;
using Bloomlings.Client.UI.Tutorial;
using Bloomlings.Client.UI.Tutorial.Demos;
using Bloomlings.Core.Progression;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Every roadmap unlock is demonstrated (FR-031, spec US4: "the new system is demonstrated").</summary>
    public class DemoCoverageTests
    {
        [Test]
        public void EveryMechanicAndBooster_OnBothLevel8Roadmaps_HasADemo()
        {
            foreach (UnlockRoadmap roadmap in new[] { UnlockRoadmap.Default, UnlockRoadmap.MysteryPodAtLevel8 })
            {
                foreach (UnlockEntry entry in roadmap.Entries)
                {
                    if (entry.Kind == UnlockKind.Mechanic)
                    {
                        DemoScript? demo = MechanicDemos.For(entry.UnlockId, new DemoTargets());
                        Assert.That(demo, Is.Not.Null, entry.UnlockId);
                        Assert.That(demo!.DemoId, Is.EqualTo(entry.UnlockId));
                    }
                    else if (entry.Kind == UnlockKind.Booster)
                    {
                        Assert.That(BoosterDemos.For(entry.UnlockId, () => null), Is.Not.Null, entry.UnlockId);
                    }
                }
            }
        }

        [Test]
        public void TheHomeSystems_AreRoadmapSystems_WithADemoEach()
        {
            var systems = UnlockRoadmap.Default.Entries.Where(e => e.Kind == UnlockKind.System).Select(e => e.UnlockId).ToList();
            foreach (string id in DemoScripts.HomeSystems)
            {
                Assert.That(systems, Does.Contain(id));
                Assert.That(DemoScripts.HomeSystem(id, () => null)!.DemoId, Is.EqualTo(id));
            }

            // The others are shown where they happen: the core tap in Level 1, the Daily Reward popup, and the later
            // milestones on the Win screen.
            var elsewhere = new[] { "system.core", "system.daily_reward", "system.milestone_75", "system.milestone_100", "system.milestone_200", "system.milestone_500" };
            Assert.That(systems.Except(DemoScripts.HomeSystems).Except(elsewhere), Is.Empty);
        }
    }
}
