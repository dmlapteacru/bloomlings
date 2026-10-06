using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Core.Tests.Rules
{
    /// <summary>The doc 01 §20 pre-lock tests that need no later mechanics (T028).</summary>
    public class CoreLoopTests
    {
        /// <summary>
        /// Moss rows at the bottom (12 tiles, reachable as they open), a Water wall, then 8 more Moss behind it.
        /// </summary>
        private static readonly string[] PartialBoard =
        {
            "wwwwwww",
            "wwwwwww",
            "wwwwwww",
            "mmmmwww",
            "mmmmwww",
            "wwwwwww",
            "mmmmmm.",
            "mmmmmm.",
        };

        [Test]
        public void SameFamilyVariants_AreActiveAtTheSameTime()
        {
            // Leaf on the left, Moss on the right, both reachable from the open bottom row.
            var session = Session(
                new[] { "#######", "#######", "#######", "#######", "#######", "#######", "lllmmmm", "......." },
                new[] { Pod("leaf", VariantId.Leaf, 3), Pod("moss", VariantId.Moss, 4) });

            session.Tap("leaf");
            CommandResult result = session.Tap("moss");

            Assert.That(result.ClearedBy("moss").Count(), Is.EqualTo(4));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void LeafPod_NeverClearsAReachableMossTile()
        {
            // The entry cell and its neighbours are Moss; the only Leaf tile sits behind them.
            var session = Session(
                new[] { "#######", "#######", "#######", "#######", "#######", "#######", "###l###", "..mmm.." },
                new[] { Pod("leaf", VariantId.Leaf, 1), Pod("moss", VariantId.Moss, 3) });

            CommandResult result = session.Tap("leaf");

            Assert.That(result.Clears(), Is.Empty, "Leaf and Moss share a family, but the family is never a wildcard (FR-003).");
            Assert.That(session.View.Pod("leaf").Remaining, Is.EqualTo(1));
            Assert.That(session.View.Pod("leaf").Location, Is.EqualTo(PodLocation.Slot));
        }

        [Test]
        public void TheView_ListsTheReachableTargets_NearestFirst_AndTheyGrowAsTilesClear()
        {
            // The entry cell and its neighbours are Moss; the only Leaf tile sits behind them.
            var session = Session(
                new[] { "#######", "#######", "#######", "#######", "#######", "#######", "###l###", "..mmm.." },
                new[] { Pod("leaf", VariantId.Leaf, 1), Pod("moss", VariantId.Moss, 3) });

            var before = session.View.ReachableTargets();
            Assert.That(before.Select(c => session.View.Cell(c).Visible), Is.All.EqualTo(VariantId.Moss), "the Leaf tile is behind the Moss");
            Assert.That(before.First(), Is.EqualTo(new CellPos(3, 0)), "the entry cell comes first");

            session.Tap("moss");
            Assert.That(session.View.ReachableTargets(), Does.Contain(new CellPos(3, 1)), "the Leaf tile opens once the Moss is cleared");
        }

        [Test]
        public void ExactAccounting_IsCheckedPerVariantOnLoad()
        {
            string[] board = { "#######", "#######", "#######", "#######", "#######", "#######", "lllmmmm", "......." };

            // 7 green tiles in total, but split Leaf 3 / Moss 4: family totals do not count (FR-023).
            Assert.Throws<InvalidLevelException>(() => Session(board, new[] { Pod("a", VariantId.Leaf, 4), Pod("b", VariantId.Moss, 3) }));
            Assert.DoesNotThrow(() => Session(board, new[] { Pod("a", VariantId.Leaf, 3), Pod("b", VariantId.Moss, 4) }));
        }

        [Test]
        public void PartialPod_WaitsAndResumesAutomatically()
        {
            var session = Session(PartialBoard, new[] { Pod("moss", VariantId.Moss, 20), Pod("water", VariantId.Water, 34) });

            CommandResult first = session.Tap("moss");

            Assert.That(first.ClearedBy("moss").Count(), Is.EqualTo(12));
            Assert.That(session.View.Pod("moss").Remaining, Is.EqualTo(8), "Moss ×20 with 12 reachable tiles becomes ×8.");
            Assert.That(session.View.PodInSlot(0), Is.EqualTo("moss"), "A partial pod stays in its slot (FR-019).");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            CommandResult second = session.Tap("water");

            Assert.That(second.ClearedBy("moss").Count(), Is.EqualTo(8), "The waiting Moss pod resumes without a tap.");
            Assert.That(second.Events.OfType<PodCompleted>().Select(e => e.PodId), Does.Contain("moss"));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void SameVariant_OldestSlotHasPriority_YoungerGetsLeftovers()
        {
            // The entry cell is Leaf; behind it an open row, then 7 Water tiles that become reachable together.
            var session = Session(
                new[] { "#######", "#######", "#######", "#######", "#######", "###w###", "wwwwwww", "...l..." },
                new[] { Pod("a", VariantId.Water, 4), Pod("b", VariantId.Water, 4), Pod("leaf", VariantId.Leaf, 1) });

            Assert.That(session.Tap("a").Clears(), Is.Empty);
            Assert.That(session.Tap("b").Clears(), Is.Empty);
            CommandResult result = session.Tap("leaf");

            TileCleared[] waterRound = result.Clears().Where(c => c.Variant == VariantId.Water).GroupBy(c => c.Round).First().ToArray();
            Assert.That(waterRound.Length, Is.EqualTo(7));
            Assert.That(
                waterRound.Where(c => c.PodId == "a").Select(c => c.Cell),
                Is.EqualTo(new[] { new CellPos(3, 1), new CellPos(2, 1), new CellPos(4, 1), new CellPos(1, 1) }),
                "The oldest pod takes its 4 nearest tiles first (FR-020, FR-021).");
            Assert.That(
                waterRound.Where(c => c.PodId == "b").Select(c => c.Cell),
                Is.EqualTo(new[] { new CellPos(5, 1), new CellPos(0, 1), new CellPos(6, 1) }),
                "The younger pod only gets the leftovers.");
            Assert.That(result.ClearedBy("b").Last(), Is.EqualTo(new CellPos(3, 2)), "It finishes one round later.");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void FullBufferWithAProgressingPod_IsNotAJam()
        {
            var session = Session(JamBoard, JamPods);
            foreach (string id in new[] { "f", "o", "m", "d" })
            {
                session.Tap(id);
            }

            // The fifth commit fills the buffer, but the Water pod clears its tiles and leaves.
            CommandResult result = session.Tap("w");

            Assert.That(result.Events.OfType<LevelJammed>(), Is.Empty);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.View.SlotStateOf(4), Is.EqualTo(SlotState.Free));
        }

        [Test]
        public void TrueJam_WhenEveryUsableSlotHoldsAWaitingPod()
        {
            var session = Session(JamBoard, JamPods);
            foreach (string id in new[] { "f", "o", "m", "d" })
            {
                Assert.That(session.Tap(id).Events.OfType<LevelJammed>(), Is.Empty);
            }

            CommandResult result = session.Tap("l2");

            Assert.That(result.Events.Last(), Is.TypeOf<LevelJammed>());
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Jammed));
            Assert.That(session.Apply(new TapPod("w")).Reason, Is.EqualTo(RejectReason.NoFreeSlot));
        }

        [Test]
        public void Win_TakesPrecedence_WhenTheLastCommitFillsTheBuffer()
        {
            // Four pods wait behind a Leaf wall; the fifth pod, committed into the last free slot, opens it and every
            // pod finishes in the same settle.
            var session = Session(
                new[] { "#######", "#######", "#######", "#######", "#######", "#######", "fodwlll", "lllllll" },
                new[]
                {
                    Pod("f", VariantId.Flower, 1), Pod("o", VariantId.Wood, 1), Pod("d", VariantId.Dew, 1),
                    Pod("w", VariantId.Water, 1), Pod("l", VariantId.Leaf, 10),
                });
            foreach (string id in new[] { "f", "o", "d", "w" })
            {
                Assert.That(session.Tap(id).Clears(), Is.Empty);
            }

            CommandResult result = session.Tap("l");

            Assert.That(result.Events.OfType<PodCommitted>().Single().SlotIndex, Is.EqualTo(4), "The buffer is full at commit.");
            Assert.That(result.Events.OfType<LevelJammed>(), Is.Empty);
            Assert.That(result.Events.Last(), Is.TypeOf<LevelWon>());
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void RejectedTaps_ChangeNothing()
        {
            var session = Session(JamBoard, JamPods, new[] { new[] { "f", "o" }, new[] { "m" }, new[] { "d" }, new[] { "l2" }, new[] { "w" } });
            ulong before = session.StateHash;

            CommandResult buried = session.Tap("o");
            CommandResult unknown = session.Tap("nope");

            Assert.That(buried.Accepted, Is.False);
            Assert.That(buried.Reason, Is.EqualTo(RejectReason.NotExposed));
            Assert.That(buried.Events, Is.Empty);
            Assert.That(unknown.Reason, Is.EqualTo(RejectReason.NotExposed));
            Assert.That(session.StateHash, Is.EqualTo(before));
            Assert.That(session.CommandLog, Is.Empty);
            Assert.That(session.Check(new TapPod("o")).Reason, Is.EqualTo(RejectReason.NotExposed));
            Assert.That(session.Check(new TapPod("f")).IsAllowed, Is.True);
        }

        [Test]
        public void NoFreeSlot_IsRefusedWithoutStateChange()
        {
            var session = Session(JamBoard, JamPods);
            foreach (string id in new[] { "f", "o", "m", "d", "l2" })
            {
                session.Tap(id);
            }

            ulong before = session.StateHash;
            CommandResult result = session.Tap("w");

            Assert.That(result.Reason, Is.EqualTo(RejectReason.NoFreeSlot));
            Assert.That(session.StateHash, Is.EqualTo(before));
            Assert.That(session.CommandLog.Count, Is.EqualTo(5));
        }

        [Test]
        public void Slot_FreesAtOnceWhenTheCountReachesZero()
        {
            var session = Session(JamBoard, JamPods);
            session.Tap("f");
            CommandResult result = session.Tap("w");

            GameEvent[] events = result.Events.ToArray();
            int lastClear = System.Array.FindLastIndex(events, e => e is TileCleared c && c.PodId == "w");
            Assert.That(events[lastClear + 2], Is.EqualTo(new PodCompleted(events[lastClear].Round, "w", 1)));
            Assert.That(events[lastClear + 3], Is.EqualTo(new SlotFreed(events[lastClear].Round, 1)));
            Assert.That(session.View.SlotStateOf(1), Is.EqualTo(SlotState.Free));

            // The next commit takes the leftmost free slot again (FR-014).
            Assert.That(session.Tap("o").Events.OfType<PodCommitted>().Single().SlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void Restart_RebuildsTheSameLevel()
        {
            var session = Session(PartialBoard, new[] { Pod("moss", VariantId.Moss, 20), Pod("water", VariantId.Water, 34) });
            ulong start = session.StateHash;
            session.Tap("moss");

            CommandResult result = session.Apply(new Restart());

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.StateHash, Is.EqualTo(start));
            Assert.That(session.CommandLog, Is.Empty);
            Assert.That(session.View.Pod("moss").Remaining, Is.EqualTo(20));
        }

        [Test]
        public void IncrementalHash_MatchesAFullRecompute()
        {
            var session = Session(PartialBoard, new[] { Pod("moss", VariantId.Moss, 20), Pod("water", VariantId.Water, 34) });
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()));
            session.Tap("moss");
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()));
            session.Tap("water");
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()));
        }

        [Test]
        public void Routes_RunFromTheEntryToTheTarget()
        {
            var session = Session(PartialBoard, new[] { Pod("moss", VariantId.Moss, 20), Pod("water", VariantId.Water, 34) });

            TileCleared first = session.Tap("moss").Clears().First();

            Assert.That(first.Cell, Is.EqualTo(new CellPos(3, 0)));
            Assert.That(first.RouteFromEntry, Is.EqualTo(new[] { new CellPos(3, 0) }), "The entry cell itself is at distance 1.");
        }

        /// <summary>
        /// Row 1: Flower, Wood, Moss, Dew and a second Leaf sit behind a Leaf wall (row 0) that nobody in the jam
        /// scenario clears; one Water tile is reachable from the entry.
        /// </summary>
        private static readonly string[] JamBoard =
        {
            "#######", "#######", "#######", "#######", "#######", "#######", "fomdlll", "#llwll#",
        };

        private static readonly PodDef[] JamPods =
        {
            Pod("f", VariantId.Flower, 1),
            Pod("o", VariantId.Wood, 1),
            Pod("m", VariantId.Moss, 1),
            Pod("d", VariantId.Dew, 1),
            Pod("l2", VariantId.Leaf, 7),
            Pod("w", VariantId.Water, 1),
        };
    }
}
