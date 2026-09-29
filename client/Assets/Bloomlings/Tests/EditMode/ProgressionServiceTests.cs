using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Progression;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Linear progression and roadmap unlocks (T055).</summary>
    public class ProgressionServiceTests
    {
        private PlayerSave _save = null!;
        private int _saves;
        private List<UnlockEntry> _events = null!;
        private ProgressionService _progression = null!;

        [SetUp]
        public void SetUp()
        {
            _save = PlayerSave.CreateNew("p1", new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc));
            _saves = 0;
            _events = new List<UnlockEntry>();
            _progression = new ProgressionService(_save, UnlockRoadmap.Default, () => _saves++);
            _progression.UnlockReached += _events.Add;
        }

        [Test]
        public void WinAtN_SetsHighestToN_AndCurrentToNPlusOne()
        {
            _progression.Initialize();

            Assert.That(_progression.CompleteLevel(1), Is.True);
            Assert.That(_progression.HighestCompletedLevel, Is.EqualTo(1));
            Assert.That(_progression.CurrentLevel, Is.EqualTo(2));
            Assert.That(_saves, Is.GreaterThanOrEqualTo(2), "Saved on first launch and after the win.");

            Assert.That(_progression.CompleteLevel(5), Is.False, "Only the current level can be completed (FR-057).");
            Assert.That(_progression.CurrentLevel, Is.EqualTo(2));
        }

        [Test]
        public void UnlockEvents_FireExactlyAtRoadmapLevels()
        {
            _progression.Initialize();
            var firedAt = new List<(int Current, string Id)>();
            _progression.UnlockReached += e => firedAt.Add((_progression.CurrentLevel, e.UnlockId));

            for (int level = 1; level <= 11; level++)
            {
                _progression.CompleteLevel(level);
            }

            Assert.That(_events.First().UnlockId, Is.EqualTo("system.core"), "Level 1 unlocks on first launch.");
            var expected = UnlockRoadmap.Default.Entries.Where(e => e.Level >= 2 && e.Level <= 12).Select(e => (e.Level, e.UnlockId));
            Assert.That(firedAt, Is.EqualTo(expected), "Each unlock fires when its level is reached, in roadmap order.");
            Assert.That(_events.Select(e => e.UnlockId), Is.Unique);

            // Reloading the same save never fires them again.
            var again = new List<UnlockEntry>();
            var reloaded = new ProgressionService(_save, UnlockRoadmap.Default, () => { });
            reloaded.UnlockReached += again.Add;
            reloaded.Initialize();
            Assert.That(again, Is.Empty);
        }

        [Test]
        public void Demo_IsDueOnlyWithinItsWindow()
        {
            var roadmap = new UnlockRoadmap(new[]
            {
                new UnlockEntry(1, "system.core", UnlockKind.System, 0, false),
                new UnlockEntry(3, "booster.extra_slot", UnlockKind.Booster, 0, false),
                new UnlockEntry(5, "variant.new", UnlockKind.Variant, 2, false),
            });
            var progression = new ProgressionService(_save, roadmap, () => { });
            progression.Initialize();
            progression.MarkDemoSeen("system.core");

            progression.FastForward(2);
            Assert.That(progression.DemoDue(3)?.UnlockId, Is.EqualTo("booster.extra_slot"));
            progression.MarkDemoSeen("booster.extra_slot");
            Assert.That(progression.DemoDue(3), Is.Null, "Shown once.");

            progression.FastForward(4);
            Assert.That(progression.DemoDue(5)?.UnlockId, Is.EqualTo("variant.new"));
            Assert.That(progression.DemoDue(7)?.UnlockId, Is.EqualTo("variant.new"), "Still due 2 levels later.");
            Assert.That(progression.DemoDue(8), Is.Null, "Past the 0–2 level window (FR-031).");
        }

        [Test]
        public void FastForward_FiresEveryUnlockOnTheWay()
        {
            _progression.Initialize();

            IReadOnlyList<UnlockEntry> reached = _progression.FastForward(24);

            Assert.That(_progression.CurrentLevel, Is.EqualTo(25));
            Assert.That(reached.Select(e => e.UnlockId), Is.EqualTo(UnlockRoadmap.Default.ReachedBetween(1, 25).Select(e => e.UnlockId)));
        }
    }
}
