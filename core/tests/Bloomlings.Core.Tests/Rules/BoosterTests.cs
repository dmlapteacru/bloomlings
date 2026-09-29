using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Core.Tests.Rules
{
    /// <summary>The four boosters (FR-043 to FR-050, T113). Every command is checked against the incremental hash.</summary>
    public class BoosterTests
    {
        private static string[] Rows(params string[] bottom) => Enumerable.Repeat("#######", 8 - bottom.Length).Concat(bottom).ToArray();

        private static LevelSession Load(string[] rows, LevelDefinition level)
        {
            LevelSession session = LevelSession.Load(level, Picture(rows), Options);
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()));
            return session;
        }

        private static CommandResult Do(LevelSession session, Command command)
        {
            CommandResult result = session.Apply(command);
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()), "incremental hash after " + CommandText.Format(command));
            return result;
        }

        private static CommandResult Do(LevelSession session, string command) => Do(session, CommandText.Parse(command));

        private static void Refused(LevelSession session, string command)
        {
            CommandCheck check = session.Check(CommandText.Parse(command));
            Assert.That(check.IsAllowed, Is.False, command);
            Assert.That(check.Reason, Is.EqualTo(RejectReason.BoosterNotApplicable), command);
            ulong before = session.StateHash;
            Assert.That(session.Apply(CommandText.Parse(command)).Accepted, Is.False);
            Assert.That(session.StateHash, Is.EqualTo(before));
        }

        private static LevelSession Jammed()
        {
            // Five Flower pods wait behind the Water tile and fill the buffer.
            string[] rows = Rows("###f###", "###f###", "###f###", "###f###", "###f###", "##mwm##", ".......");
            LevelDefinition level = Definition(rows, new[]
            {
                Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1), Pod("f5", VariantId.Flower, 1),
                Pod("moss", VariantId.Moss, 2), Pod("water", VariantId.Water, 1),
            }, stacks: new[] { new[] { "f1", "moss" }, new[] { "f2", "water" }, new[] { "f3" }, new[] { "f4" }, new[] { "f5" } });
            LevelSession session = Load(rows, level);
            foreach (string pod in new[] { "f1", "f2", "f3", "f4", "f5" })
            {
                Do(session, "tap:" + pod);
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Jammed));
            return session;
        }

        [Test]
        public void ExtraSlot_RecoversAJam_AndWorksOncePerLevel()
        {
            LevelSession session = Jammed();
            Assert.That(session.EligibleRecoveries(), Does.Contain(Recovery.ExtraSlot));

            CommandResult result = Do(session, "extra_slot");

            Assert.That(result.Events.OfType<ExtraSlotAdded>().Single().SlotIndex, Is.EqualTo(WaitingSlots.ExtraSlotIndex));
            Assert.That(session.View.SlotStateOf(WaitingSlots.ExtraSlotIndex), Is.EqualTo(SlotState.Free));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Refused(session, "extra_slot");

            Do(session, "tap:water");
            Do(session, "tap:moss");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.View.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Return_PutsThePodOnTopOfItsOriginalStack()
        {
            string[] rows = Rows("###l###", "..mmm..", "...m...");
            LevelSession session = Load(rows, Definition(rows, new[] { Pod("moss", VariantId.Moss, 4), Pod("leaf", VariantId.Leaf, 1) }, stacks: new[] { new[] { "leaf" }, new[] { "moss" } }));
            Do(session, "tap:leaf");
            int slot = session.View.Pod("leaf").SlotIndex;

            CommandResult result = Do(session, "return:" + slot.ToString(System.Globalization.CultureInfo.InvariantCulture));

            Assert.That(result.Events.OfType<PodReturned>().Single(), Is.EqualTo(new PodReturned(0, "leaf", 0)));
            Assert.That(session.View.Stack(0)[0], Is.EqualTo("leaf"));
            Assert.That(session.View.Pod("leaf").Remaining, Is.EqualTo(1));
            Assert.That(session.View.SlotStateOf(slot), Is.EqualTo(SlotState.Free));
        }

        [Test]
        public void Return_KeepsPartialProgressAndClearedTiles()
        {
            // The Moss pod (count 6) clears the 4 reachable Moss tiles and waits for 2 behind the Leaf tile.
            string[] rows = Rows("###m###", "###m###", "###l###", "..mmm..", "...m...");
            LevelSession session = Load(rows, Definition(rows, new[] { Pod("moss", VariantId.Moss, 6), Pod("leaf", VariantId.Leaf, 1) }, stacks: new[] { new[] { "moss" }, new[] { "leaf" } }));
            Do(session, "tap:moss");
            Assert.That(session.View.Pod("moss").Remaining, Is.EqualTo(2));
            int remainingWork = session.View.RemainingWork;

            Do(session, "return:0");

            Assert.That(session.View.Pod("moss").Remaining, Is.EqualTo(2));
            Assert.That(session.View.Pod("moss").Location, Is.EqualTo(PodLocation.Tray));
            Assert.That(session.View.RemainingWork, Is.EqualTo(remainingWork), "cleared tiles stay cleared");
            Do(session, "tap:leaf");
            Do(session, "tap:moss");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Return_OnAConnectedMember_LeavesTheOtherInItsSlot()
        {
            // Both members wait: the Leaf member behind the Water tile, the Dew member behind the Flower column.
            string[] rows = Rows("###d###", "###f###", "###l###", "###w###", ".......");
            LevelDefinition level = Definition(rows, new[]
            {
                new PodDef("cLeaf", VariantId.Leaf, 1, false, null, "g"), new PodDef("cDew", VariantId.Dew, 1, false, null, "g"),
                Pod("water", VariantId.Water, 1), Pod("flower", VariantId.Flower, 1),
            }, stacks: new[] { new[] { "cLeaf", "water" }, new[] { "cDew", "flower" } });
            LevelSession session = Load(rows, level);
            Do(session, "tap:cLeaf");
            int leafSlot = session.View.Pod("cLeaf").SlotIndex;

            Do(session, "return:" + leafSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));

            Assert.That(session.View.Pod("cLeaf").Location, Is.EqualTo(PodLocation.Tray));
            Assert.That(session.View.Pod("cDew").Location, Is.EqualTo(PodLocation.Slot), "the other member stays");
            Assert.That(session.View.ConnectedGroup("cLeaf"), Is.EqualTo(new[] { "cLeaf", "cDew" }));
            Assert.That(session.Check(new TapPod("cLeaf")).IsAllowed, Is.True, "the returned member now commits alone");
        }

        [Test]
        public void BloomBurst_RemovesEveryLayerAndPodOfTheVariant_AndAccountingReconciles()
        {
            // Leaf on top of two cells, one with a hidden Leaf under Moss; a Leaf pod waits in a slot.
            string[] rows = Rows("###l###", "###m###", ".lmlm..");
            LevelDefinition level = Definition(rows, new[]
            {
                Pod("leafA", VariantId.Leaf, 2), Pod("leafB", VariantId.Leaf, 2), Pod("moss", VariantId.Moss, 3),
            }, stacks: new[] { new[] { "moss", "leafB" }, new[] { "leafA" } },
                overlays: new[] { new CellOverlay(new CellPos(4, 0), new[] { VariantId.Leaf }, false, false, false, null) });
            LevelSession session = Load(rows, level);

            CommandResult result = Do(session, new UseBloomBurst(VariantId.Leaf));

            VariantBurst burst = result.Events.OfType<VariantBurst>().Single();
            Assert.That(burst.PodIds, Is.EquivalentTo(new[] { "leafA", "leafB" }));
            Assert.That(burst.Cells.Count, Is.EqualTo(4), "three visible Leaf tiles and the hidden Leaf layer under Moss");
            Assert.That(session.View.Pod("leafA").Location, Is.EqualTo(PodLocation.Removed));
            Assert.That(session.View.Pod("leafB").Location, Is.EqualTo(PodLocation.Removed));

            int mossLayers = Enumerable.Range(0, 7 * 8).Count(i => session.View.Cell(i).Kind == CellKind.Target);
            Assert.That(session.View.Pod("moss").Remaining, Is.EqualTo(3));
            Assert.That(mossLayers, Is.EqualTo(3), "only Moss is left, one layer per cell");
            Assert.That(session.View.Cell(new CellPos(4, 0)).Next, Is.Null, "the hidden Leaf under Moss is gone");
            Do(session, "tap:moss");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void BloomBurst_NeedsAVisibleVariant()
        {
            string[] rows = Rows("###l###", "..mmm..", ".......");
            LevelSession session = Load(rows, Definition(rows, new[] { Pod("moss", VariantId.Moss, 3), Pod("leaf", VariantId.Leaf, 1) }));

            Refused(session, "burst:water");
            Assert.That(session.Check(new UseBloomBurst(VariantId.Leaf)).IsAllowed, Is.True, "the Leaf tile is visible though not reachable");
        }

        private static readonly string[] ShuffleBoard = Rows("###f###", "###d###", "###o###", "###m###", "##lwl##", ".......");

        /// <summary>A column to dig from the bottom: Water, then Moss, Wood, Dew and Flower; the tray buries it upside down.</summary>
        private static LevelDefinition ShuffleLevel() => Definition(ShuffleBoard, new[]
        {
            Pod("water", VariantId.Water, 1), Pod("leaf", VariantId.Leaf, 2), Pod("moss", VariantId.Moss, 1), Pod("wood", VariantId.Wood, 1),
            Pod("dew", VariantId.Dew, 1), Pod("flower", VariantId.Flower, 1),
        }, stacks: new[] { new[] { "flower", "dew", "wood" }, new[] { "moss", "leaf", "water" } });

        [Test]
        public void Shuffle_TurnsALosingTrayIntoAWinnableOne()
        {
            // Everything is buried in the wrong order: only Flower and Moss are exposed, and both must wait.
            LevelSession session = Load(ShuffleBoard, ShuffleLevel());
            Do(session, "tap:flower");
            Do(session, "tap:moss");
            Do(session, "tap:dew");

            CommandResult result = Do(session, "shuffle");

            Assert.That(result.Events.OfType<TrayShuffled>(), Is.Not.Empty);
            Assert.That(session.View.Pod("flower").Location, Is.EqualTo(PodLocation.Slot), "waiting pods are untouched");
            Assert.That(StateSearch.Find(session, StateSearch.IsWon, MoveOrder.ProgressFirst, 10_000).Outcome, Is.EqualTo(SearchOutcome.Found));
        }

        [Test]
        public void Shuffle_IsDeterministic()
        {
            LevelSession a = Load(ShuffleBoard, ShuffleLevel());
            LevelSession b = Load(ShuffleBoard, ShuffleLevel());
            foreach (LevelSession s in new[] { a, b })
            {
                Do(s, "tap:flower");
                Do(s, "shuffle");
                Do(s, "shuffle");
            }

            Assert.That(b.StateHash, Is.EqualTo(a.StateHash));
            Assert.That(Enumerable.Range(0, b.View.StackCount).Select(b.View.Stack), Is.EqualTo(Enumerable.Range(0, a.View.StackCount).Select(a.View.Stack)));
        }

        [Test]
        public void Shuffle_KeepsLocksAttachedAndConnectedPodsTogether()
        {
            string[] rows = Rows("###f###", "###d###", "###m###", "##lwl##", ".......");
            LevelDefinition level = Definition(rows, new[]
            {
                new PodDef("flower", VariantId.Flower, 1, false, "k1", null),
                Pod("dew", VariantId.Dew, 1), Pod("moss", VariantId.Moss, 1),
                new PodDef("cLeaf", VariantId.Leaf, 2, false, null, "g"), new PodDef("cWater", VariantId.Water, 1, false, null, "g"),
            }, stacks: new[] { new[] { "dew", "cLeaf" }, new[] { "moss", "cWater" }, new[] { "flower" } },
                overlays: new[] { new CellOverlay(new CellPos(3, 2), Array.Empty<VariantId>(), false, false, false, "k1") }) with
            {
                Locks = new[] { new LockDef("k1", LockTargetKind.Pod, "flower") },
            };
            LevelSession session = Load(rows, level);

            Do(session, "shuffle");

            Assert.That(session.View.Pod("flower").Locked, Is.True);
            int stackLeaf = Enumerable.Range(0, session.View.StackCount).Single(s => session.View.Stack(s).Contains("cLeaf"));
            int stackWater = Enumerable.Range(0, session.View.StackCount).Single(s => session.View.Stack(s).Contains("cWater"));
            Assert.That(stackLeaf, Is.Not.EqualTo(stackWater));
            Assert.That(session.View.Stack(stackLeaf).ToList().IndexOf("cLeaf"), Is.EqualTo(session.View.Stack(stackWater).ToList().IndexOf("cWater")), "same depth");
            Assert.That(StateSearch.Find(session, StateSearch.IsWon, MoveOrder.ProgressFirst, 10_000).Outcome, Is.EqualTo(SearchOutcome.Found));
        }

        [Test]
        public void DisabledBoosters_AreRefusedWithBoosterNotApplicable()
        {
            string[] rows = Rows("###l###", "..mmm..", ".......");
            LevelSession session = Load(rows, Definition(rows, new[] { Pod("moss", VariantId.Moss, 3), Pod("leaf", VariantId.Leaf, 1) }));

            Refused(session, "return:0");
            Refused(session, "return:5");

            Do(session, "tap:moss");
            Refused(session, "shuffle");
            Do(session, "tap:leaf");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.Check(new UseExtraSlot()).Reason, Is.EqualTo(RejectReason.LevelNotPlaying));
        }

        [Test]
        public void BoostersUsed_IsReportedWithTheWin()
        {
            LevelSession session = Jammed();
            Do(session, "extra_slot");
            Do(session, "tap:water");
            CommandResult win = Do(session, "tap:moss");

            Assert.That(win.Events.OfType<LevelWon>().Single().BoostersUsed, Is.EqualTo(1));
        }
    }
}
