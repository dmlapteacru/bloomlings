using System.Linq;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>The orderings of spec 006 research R3 that decide which cell a courier takes and which event comes first.</summary>
    public class RoundRulesOrderTests
    {
        // 5 × 3, rows top first; the entry is the bottom row's middle cell (index 2). The wall above the entry makes the
        // middle target reachable only from the left and the right at the same distance.
        private static RoundLevel TieBoard(params RoundPod[] pods) => RoundTestLevels.Board(
            new[]
            {
                "G.G.G",
                "..#..",
                ".....",
            },
            pods.Length > 0 ? pods : new[] { new RoundPod('G', 3) },
            new[] { new[] { 0 }, new int[0] },
            entryX: 2);

        [Test]
        public void NeighboursAreTheRowBelowThenLeftRightThenTheRowAbove()
        {
            RoundLevel level = TieBoard();
            int[] n = level.Prepare().Neighbours;
            int middle = RoundTestLevels.Cell(level, 2, 1);
            Assert.That(n.Skip(middle * 4).Take(4), Is.EqualTo(new[] { middle - 5, middle - 1, middle + 1, middle + 5 }));
            int corner = RoundTestLevels.Cell(level, 0, 0);
            Assert.That(n.Skip(corner * 4).Take(4), Is.EqualTo(new[] { -1, -1, 1, 5 }));
        }

        [Test]
        public void ReachIsBreadthFirstFromTheEntryAndTargetsSortByDistanceThenIndex()
        {
            // Rows top first: a G right above the open approach (distance 3) and two G further up (distance 5).
            RoundLevel level = RoundTestLevels.Board(
                new[]
                {
                    "GGG",
                    ".G.",
                    "...",
                    "#.#",
                },
                new[] { new RoundPod('G', 4) },
                new[] { new[] { 0 }, new int[0] },
                entryX: 1);
            RoundReach reach = RoundRules.Create(level).Reach();
            Assert.That(reach.Distance[1], Is.EqualTo(1), "the entry is at distance 1");
            Assert.That(reach.Distance[4], Is.EqualTo(2));
            Assert.That(reach.Distance[3], Is.EqualTo(3));
            Assert.That(reach.Distance[6], Is.EqualTo(4));
            Assert.That(reach.Targets, Is.EqualTo(new[] { 7, 9, 11 }), "cell 10 touches no open cell");
            Assert.That(reach.TargetDistance[7], Is.EqualTo(3));
            Assert.That(reach.TargetDistance[9], Is.EqualTo(5));
            Assert.That(RoundRules.RouteTo(RoundRules.Create(level), 9), Is.EqualTo(new[] { 1, 4, 3, 6, 9 }));
            Assert.That(RoundRules.RouteTo(RoundRules.Create(level), 11), Is.EqualTo(new[] { 1, 4, 5, 8, 11 }));
        }

        [Test]
        public void EqualTargetsSortByIndexAndARouteTakesTheFirstNearestNeighbour()
        {
            RoundLevel level = TieBoard();
            RoundState s = RoundRules.Create(level);
            Assert.That(s.Reach().Targets, Is.EqualTo(new[] { 10, 12, 14 }));
            Assert.That(s.Reach().Targets.Select(c => s.Reach().TargetDistance[c]), Is.All.EqualTo(5));
            // Cell 12's left (11) and right (13) neighbours are both at distance 4: the left one, first in order, wins.
            Assert.That(RoundRules.RouteTo(s, 12), Is.EqualTo(new[] { 2, 1, 6, 11, 12 }));
        }

        [Test]
        public void AMaterialEntryCellIsATargetAtDistanceOne()
        {
            RoundLevel level = RoundTestLevels.Board(new[] { "...", "...", ".G." }, new[] { new RoundPod('G', 1) }, new[] { new[] { 0 }, new int[0] }, entryX: 1);
            RoundState s = RoundRules.Create(level);
            Assert.That(s.Reach().Targets, Is.EqualTo(new[] { 1 }));
            Assert.That(s.Reach().TargetDistance[1], Is.EqualTo(1));
            Assert.That(RoundRules.RouteTo(s, 1), Is.EqualTo(new[] { 1 }));
        }

        // Pods: 0 X, 1 and 2 a link (A), 3 Y, 4 a single G; columns [0, 1], [3, 2], [4]. Nothing of X or Y is reachable, so
        // committed pods stay in their places.
        private static RoundLevel LinkBoard() => RoundTestLevels.Board(
            new[]
            {
                "#####",
                "PBAC#",
                "#####",
                "#G.G#",
                "G...G",
            },
            new[] { new RoundPod('P', 1), new RoundPod('B', 1, group: "A"), new RoundPod('A', 1, group: "A"), new RoundPod('C', 1), new RoundPod('G', 4) },
            new[] { new[] { 0, 1 }, new[] { 3, 2 }, new[] { 4 } },
            entryX: 2);

        [Test]
        public void CanTapGivesTheReferenceReasons()
        {
            RoundLevel level = LinkBoard();
            var t = new RoundTimeline(level);
            Assert.That(RoundRules.CanTap(t.State, 1), Is.EqualTo(RoundRefusal.NotExposed));
            Assert.That(RoundRules.CanTap(t.State, 9), Is.EqualTo(RoundRefusal.UnknownPod));
            Assert.That(t.Commit(RoundAction.Tap(0)).Accepted);
            // Pod 1 is on top now, but its partner 2 stands under pod 3, which is not of the link.
            Assert.That(RoundRules.CanTap(t.State, 1), Is.EqualTo(RoundRefusal.PartnerNotExposed));
            Assert.That(RoundRules.ReasonText(RoundRefusal.PartnerNotExposed), Is.EqualTo("partner-not-exposed"));
            Assert.That(t.Commit(RoundAction.Tap(3)).Accepted);
            Assert.That(RoundRules.CanTap(t.State, 1), Is.EqualTo(RoundRefusal.None));
            Assert.That(RoundRules.CanTap(t.State, 2), Is.EqualTo(RoundRefusal.None), "either member launches the link");

            RoundState full = t.State.Clone();
            full.Slots[2] = 4;
            full.Slots[3] = 4;
            Assert.That(RoundRules.CanTap(full, 1), Is.EqualTo(RoundRefusal.PairNeedsTwoSlots));
            full.Slots[4] = 4;
            Assert.That(RoundRules.CanTap(full, 4), Is.EqualTo(RoundRefusal.NoSlot));
        }

        [Test]
        public void APartnerDirectlyUnderAMemberOfItsLinkMayLaunch()
        {
            // The link's members stand over each other in one column: the second is exposed by the first.
            RoundLevel level = RoundTestLevels.Board(
                new[] { "#####", "#PBP#", "#####", "#...#", "#...#" },
                new[] { new RoundPod('P', 1, group: "A"), new RoundPod('B', 1, group: "A"), new RoundPod('P', 1) },
                new[] { new[] { 0, 1 }, new[] { 2 } },
                entryX: 2);
            var t = new RoundTimeline(level);
            Assert.That(RoundRules.CanTap(t.State, 0), Is.EqualTo(RoundRefusal.None));
            RoundCommitResult r = t.Commit(RoundAction.Tap(0));
            Assert.That(r.Accepted);
            Assert.That(t.State.Slots.Take(2), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(t.State.Heads[0], Is.EqualTo(2));
        }

        [Test]
        public void LegalListsOneTapPerLinkByItsFirstMemberThenBridges()
        {
            RoundLevel level = LinkBoard();
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.Commit(RoundAction.Tap(3));
            Assert.That(t.Legal(), Is.EqualTo(new[] { RoundAction.Tap(1), RoundAction.Tap(4) }));
        }

        [Test]
        public void OnlyAnAcceptedLinkCompactsThePlaces()
        {
            RoundLevel level = LinkBoard();
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.Commit(RoundAction.Tap(3));
            RoundState s = t.State;
            // Make a hole at place 0 (as a release would) and keep pod 3 in place 1; pod 0 then stands nowhere, which the
            // invariants would refuse, so they are off for this check of the place order only.
            t.CheckInvariants = false;
            s.Slots[0] = -1;
            Assert.That(s.Slots, Is.EqualTo(new[] { -1, 3, -1, -1, -1 }));
            RoundCommitResult r = t.Commit(RoundAction.Tap(1));
            Assert.That(r.Accepted);
            Assert.That(s.Slots, Is.EqualTo(new[] { 3, 1, 2, -1, -1 }), "compacted to the left, then the link in the first free places");
        }

        [Test]
        public void ASinglePodTakesTheFirstFreePlaceWithoutCompacting()
        {
            RoundLevel level = LinkBoard();
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            RoundState s = t.State;
            s.Slots[0] = -1;
            s.Slots[2] = 0;
            t.Commit(RoundAction.Tap(3));
            Assert.That(s.Slots, Is.EqualTo(new[] { 3, -1, 0, -1, -1 }));
        }

        [Test]
        public void ADepthPodRevealsOnReachingTheTopAndASealedOneOnCommit()
        {
            RoundLevel level = RoundTestLevels.Board(
                new[] { "#####", "PBAA#", "#####", "#...#", "#...#" },
                new[] { new RoundPod('P', 1), new RoundPod('B', 1, RoundMystery.Depth), new RoundPod('A', 1, RoundMystery.Sealed), new RoundPod('A', 1, RoundMystery.Depth) },
                new[] { new[] { 0, 1 }, new[] { 2, 3 } },
                entryX: 2);
            var t = new RoundTimeline(level);
            Assert.That(t.State.Revealed, Is.EqualTo(new[] { true, false, false, false }), "a sealed top pod stays hidden");
            RoundCommitResult a = t.Commit(RoundAction.Tap(0));
            Assert.That(a.Events.Select(e => e.ToString()), Is.EqualTo(new[] { "0 commit pod=0 slot=0", "0 reveal pod=1 mode=depth" }));
            RoundCommitResult b = t.Commit(RoundAction.Tap(2));
            Assert.That(b.Events.Select(e => e.ToString()),
                Is.EqualTo(new[] { "0 commit pod=2 slot=1", "0 reveal pod=2 mode=sealed", "0 reveal pod=3 mode=depth" }));
        }

        [Test]
        public void StatusIsWonPlayingWithABridgeJammedOrStuck()
        {
            // Won: no material cell.
            RoundLevel empty = RoundTestLevels.Board(new[] { "...", "...", "..." }, new[] { new RoundPod('G', 1) }, new[] { new[] { 0 }, new int[0] }, entryX: 1);
            Assert.That(RoundRules.Create(empty).Status, Is.EqualTo(RoundStatus.Won));

            // Playing while a bridge can be built, even with every place full.
            RoundLevel bridge = RoundTestLevels.Board(
                new[] { "GGG", "###", "...", "#.#" },
                new[] { new RoundPod('G', 3) },
                new[] { new int[0], new[] { 0 } },
                entryX: 1,
                bridge: new[] { new RoundBridgeChoice(7, 4, "middle") });
            RoundState b = RoundRules.Create(bridge);
            b.Slots[0] = b.Slots[1] = b.Slots[2] = b.Slots[3] = b.Slots[4] = 0;
            Assert.That(RoundRules.StatusOf(b), Is.EqualTo(RoundStatus.Playing));
            Assert.That(RoundRules.BridgeChoices(b), Is.EqualTo(new[] { 0 }));

            // 17 §8 as amended on 2026-10-28: a site in a wall along the board, its anchor beside it, crossing left.
            RoundLevel side = RoundTestLevels.Board(
                new[] { "G#..", "G#..", "G#.." },
                new[] { new RoundPod('G', 3) },
                new[] { new[] { 0 }, new int[0] },
                entryX: 3,
                bridge: new[] { new RoundBridgeChoice(5, 6, "left") });
            Assert.That(RoundLevelValidator.Orthogonal(5, 6, side.Width), Is.True);
            RoundState l = RoundRules.Create(side);
            Assert.That(RoundRules.BridgeChoices(l), Is.EqualTo(new[] { 0 }), "the anchor beside the site is reached");
            Assert.That(RoundRules.Legal(l).Last(), Is.EqualTo(RoundAction.BuildBridge(0)));
            RoundLevel farAnchor = RoundTestLevels.Board(
                new[] { "G#..", "G#..", "G#.." },
                new[] { new RoundPod('G', 3) },
                new[] { new[] { 0 }, new int[0] },
                entryX: 3,
                bridge: new[] { new RoundBridgeChoice(5, 4, "right") });
            Assert.That(RoundRules.BridgeChoices(RoundRules.Create(farAnchor)), Is.Empty, "an anchor on the far bank is never reached");

            RoundLevel walled = LinkBoard();
            RoundState s = RoundRules.Create(walled);
            s.Slots[0] = s.Slots[1] = s.Slots[2] = s.Slots[3] = s.Slots[4] = 0;
            Assert.That(RoundRules.StatusOf(s), Is.EqualTo(RoundStatus.Jammed));

            // Stuck: free places, but no top pod can be tapped: two links stand crossed, each partner under the other link.
            RoundLevel crossed = RoundTestLevels.Board(
                new[] { "#####", "PBAC#", "#####", "#...#", "#...#" },
                new[] { new RoundPod('P', 1, group: "A"), new RoundPod('B', 1, group: "B"), new RoundPod('A', 1, group: "B"), new RoundPod('C', 1, group: "A") },
                new[] { new[] { 0, 2 }, new[] { 1, 3 } },
                entryX: 2);
            RoundState x = RoundRules.Create(crossed);
            Assert.That(RoundRules.CanTap(x, 0), Is.EqualTo(RoundRefusal.PartnerNotExposed));
            Assert.That(RoundRules.CanTap(x, 1), Is.EqualTo(RoundRefusal.PartnerNotExposed));
            Assert.That(x.Status, Is.EqualTo(RoundStatus.Stuck));
        }
    }
}
