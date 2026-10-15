using System;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Board
{
    public class CountedStackTests
    {
        [TestCase(14, 16)]
        [TestCase(18, 20)]
        [TestCase(21, 26)]
        [TestCase(24, 24)]
        [TestCase(30, 32)]
        [TestCase(36, 36)]
        public void PublicStackCountsDecreaseAndTheCellOpensOnlyAfterItsLastTile(int width, int height)
        {
            BasePicture picture = TestContent.Picture("stack", TestContent.GardenLegend,
                Enumerable.Range(0, height).Select(y => new string(y == 0 ? 'l' : 'b', width)).ToArray());
            int water = width * (height - 1);
            var pods = new[] { new PodDef("first", VariantId.Water, 3, false, null, null),
                new PodDef("rest", VariantId.Water, water + 5, false, null, null),
                new PodDef("leaf", VariantId.Leaf, width, false, null, null) };
            var cell = new CellPos(0, 0);
            LevelDefinition level = TestContent.Level(picture, TestContent.GardenMapping(),
                entries: new[] { new EntryDef(cell, EntrySide.Bottom) },
                overlays: new[] { new CellOverlay(cell, Array.Empty<VariantId>(), false, false, false, null, 9) },
                pods: pods, stacks: new[] { new[] { "first", "rest" }, new[] { "leaf" } });
            LevelSession session = LevelSession.Load(level, picture, new SessionOptions(1, 20000));
            Assert.That(session.View.Cell(cell).StackRemaining, Is.EqualTo(9));
            LevelSession clone = session.Clone();
            Assert.That(session.Apply(new TapPod("first")).Accepted, Is.True);
            Assert.That(session.View.Cell(cell).StackRemaining, Is.EqualTo(6));
            Assert.That(session.View.Cell(cell).Kind, Is.EqualTo(CellKind.Target));
            Assert.That(clone.View.Cell(cell).StackRemaining, Is.EqualTo(9));
            Assert.That(session.State.ComputeFullHash(), Is.EqualTo(session.StateHash));
            Assert.That(session.Apply(new TapPod("rest")).Accepted, Is.True);
            Assert.That(session.View.Cell(cell).StackRemaining, Is.Zero);
            Assert.That(session.View.Cell(cell).Kind, Is.EqualTo(CellKind.Open));
            Assert.That(session.Apply(new TapPod("leaf")).Accepted, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.State.ComputeFullHash(), Is.EqualTo(session.StateHash));
            Assert.That(clone.Apply(new UseBloomBurst(VariantId.Water)).Accepted, Is.True);
            Assert.That(clone.View.Cell(cell).StackRemaining, Is.Zero, "Bloom Burst removes every counted item of its variant");
            Assert.That(clone.View.RemainingWork, Is.EqualTo(width));
            Assert.That(clone.Apply(new TapPod("leaf")).Accepted, Is.True);
            Assert.That(clone.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(clone.State.ComputeFullHash(), Is.EqualTo(clone.StateHash));
        }

        [TestCase(0, false, false)]
        [TestCase(10, false, false)]
        [TestCase(3, true, false)]
        [TestCase(3, false, true)]
        public void CountedStacksRejectAmbiguousOrInvalidOverlays(int count, bool hiddenLayer, bool key)
        {
            BasePicture picture = TestContent.Picture("invalid_stack", TestContent.GardenLegend,
                Enumerable.Repeat("bbbbbbb", 8).ToArray());
            var overlay = new CellOverlay(new CellPos(0, 0), hiddenLayer ? new[] { VariantId.Water } : Array.Empty<VariantId>(),
                false, false, false, key ? "k" : null, count);
            LevelDefinition level = TestContent.Level(picture, TestContent.GardenMapping(), overlays: new[] { overlay });
            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(level, picture, VariantCatalog.Default));
        }
    }
}
