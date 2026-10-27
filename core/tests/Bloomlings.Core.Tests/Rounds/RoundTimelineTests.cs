using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>The live timeline in integer µs (spec 006 research R2, R3; data model RoundTimeline).</summary>
    public class RoundTimelineTests
    {
        // 5 wide; the entry is the bottom row's middle. A G straight up the open corridor at distance 3 … 9 (one per row).
        private static RoundLevel Corridor(int gs, params RoundPod[] pods)
        {
            var rows = new List<string>();
            for (int i = 0; i < gs; i++)
            {
                rows.Add("##G##");
            }

            rows.Add("##.##");
            rows.Add("##.##");
            return RoundTestLevels.Board(rows.ToArray(), pods.Length > 0 ? pods : new[] { new RoundPod('G', gs) }, new[] { new[] { 0 }, new int[0] }, entryX: 2);
        }

        [Test]
        public void TheProfileIsTheReferenceInMicroseconds()
        {
            RoundProfile p = RoundProfile.Default;
            Assert.That(new[] { p.OutPerCell, p.MinOut, p.ReturnPerCell, p.MinReturn, p.Act, p.DepartureGap, p.ReleaseLag, p.TailDepartureGap },
                Is.EqualTo(new long[] { 100_000, 650_000, 75_000, 400_000, 100_000, 250_000, 300_000, 250_000 }));
        }

        [Test]
        public void ACourierWalksOutActsAndWalksBackByTheProfile()
        {
            // Three G in a column above two open cells: the first target is at distance 3, a route of 3 cells (2 steps).
            RoundLevel level = Corridor(3);
            var t = new RoundTimeline(level);
            RoundCommitResult r = t.Commit(RoundAction.Tap(0));
            RoundEvent depart = r.Events.Single(e => e.Type == RoundEventType.Depart);
            Assert.That(depart.Route, Is.EqualTo(new[] { 2, 7, 12 }));
            // 2 steps: out max(650 000, 200 000), act 100 000, back max(400 000, 150 000).
            Assert.That((depart.At, depart.ArriveAt, depart.ClearAt, depart.DoneAt), Is.EqualTo((0L, 650_000L, 750_000L, 1_150_000L)));
            Assert.That(t.Steps.Select(s => s.ToString()), Is.EqualTo(new[] { "650000:arrive:1", "750000:clear:1", "1150000:return:1" }),
                "the next G is not reachable until the first clears; no check step without free cells");
            t.Advance(750_000);
            // The clear opened the cell: the next courier leaves at once (its gap has passed), now at distance 4.
            Assert.That(t.LastEvents.Select(e => e.ToString()), Is.EqualTo(new[]
            {
                "650000 arrive id=1",
                "750000 clear id=1 cell=12",
                "750000 depart id=2 pod=0 cell=17 arrive=1400000 clear=1500000 done=1900000 route=4",
            }));
        }

        [Test]
        public void LongRoutesTakeTheirPerCellTimes()
        {
            // A G at distance 9: a route of 9 cells, 8 steps: out 800 000 (over the 650 000 minimum), back 600 000.
            var rows = new List<string> { "##G##" };
            for (int i = 0; i < 8; i++)
            {
                rows.Add("##.##");
            }

            RoundLevel level = RoundTestLevels.Board(rows.ToArray(), new[] { new RoundPod('G', 1) }, new[] { new[] { 0 }, new int[0] }, entryX: 2);
            var t = new RoundTimeline(level);
            RoundEvent d = t.Commit(RoundAction.Tap(0)).Events.Single(e => e.Type == RoundEventType.Depart);
            Assert.That(d.Route!.Count, Is.EqualTo(9));
            Assert.That((d.ArriveAt, d.ClearAt, d.DoneAt), Is.EqualTo((800_000L, 900_000L, 1_500_000L)));
        }

        [Test]
        public void APodSendsOneCourierPerGapAndWaitsWithOneCheckStep()
        {
            // Four G cells side by side, all on the frontier at once.
            RoundLevel level = RoundTestLevels.Board(new[] { "#GGGG", "#....", "##.##" }, new[] { new RoundPod('G', 4) }, new[] { new[] { 0 }, new int[0] }, entryX: 2);
            var t = new RoundTimeline(level);
            RoundCommitResult r = t.Commit(RoundAction.Tap(0));
            Assert.That(r.Events.Count(e => e.Type == RoundEventType.Depart), Is.EqualTo(1));
            Assert.That(t.Steps.Count(s => s.Type == RoundStepType.Check), Is.EqualTo(1));
            Assert.That(t.Steps.First(s => s.Type == RoundStepType.Check).At, Is.EqualTo(250_000));
            t.Advance(250_000);
            t.Advance(250_000);
            t.Advance(250_000);
            var departs = new List<long>();
            var tl = new RoundTimeline(level);
            var log = new List<RoundEvent>(tl.Commit(RoundAction.Tap(0)).Events);
            while (tl.Pending)
            {
                tl.Advance(50_000);
                log.AddRange(tl.LastEvents);
            }

            Assert.That(log.Where(e => e.Type == RoundEventType.Depart).Select(e => e.At), Is.EqualTo(new long[] { 0, 250_000, 500_000, 750_000 }));
            // Frontier order: by distance, then cell index.
            Assert.That(log.Where(e => e.Type == RoundEventType.Depart).Select(e => e.Cell).First(), Is.EqualTo(tl.Level.Width * 1 + 2));
        }

        [Test]
        public void TheOldestPodDispatchesFirst()
        {
            // Two G pods; the older one takes the nearest cells, the younger the rest.
            RoundLevel level = RoundTestLevels.Board(new[] { "GGGGG", "#...#", "##.##" }, new[] { new RoundPod('G', 2), new RoundPod('G', 3) },
                new[] { new[] { 0 }, new[] { 1 } }, entryX: 2);
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(1));
            t.Commit(RoundAction.Tap(0));
            var departs = new List<RoundEvent>();
            var tl = new RoundTimeline(level);
            departs.AddRange(tl.Commit(RoundAction.Tap(1)).Events.Where(e => e.Type == RoundEventType.Depart));
            departs.AddRange(tl.Commit(RoundAction.Tap(0)).Events.Where(e => e.Type == RoundEventType.Depart));
            // Pod 1 (committed first) took the nearest target; pod 0 the next one in frontier order.
            int[] frontier = RoundRules.Create(level).Reach().Targets.ToArray();
            Assert.That(departs.Select(e => (e.Pod, e.Cell)), Is.EqualTo(new[] { (1, frontier[0]), (0, frontier[1]) }));
        }

        [Test]
        public void StepsRunByTimeThenTypeThenKey()
        {
            var steps = new List<RoundStep>
            {
                new RoundStep(RoundStepType.Check, 100, 0),
                new RoundStep(RoundStepType.Release, 100, 5),
                new RoundStep(RoundStepType.Return, 100, 3),
                new RoundStep(RoundStepType.Arrive, 100, 9),
                new RoundStep(RoundStepType.Clear, 100, 4),
                new RoundStep(RoundStepType.Clear, 100, 2),
                new RoundStep(RoundStepType.Arrive, 50, 99),
            };
            steps.Sort();
            Assert.That(steps.Select(s => s.ToString()), Is.EqualTo(new[]
            {
                "50:arrive:99", "100:clear:2", "100:clear:4", "100:arrive:9", "100:return:3", "100:release:5", "100:check:0",
            }));
        }

        [Test]
        public void ALinkReleasesItsPlacesTogetherAfterTheLagOnceAllWorkIsSent()
        {
            RoundLevel level = RoundTestLevels.Board(new[] { "##P##", "##.##", "##B##", "#...#", "##.##" },
                new[] { new RoundPod('B', 1, group: "A"), new RoundPod('P', 1, group: "A") },
                new[] { new[] { 0 }, new[] { 1 } }, entryX: 2);
            var t = new RoundTimeline(level);
            var log = new List<RoundEvent>(t.Commit(RoundAction.Tap(0)).Events);
            Assert.That(t.State.Slots.Take(2), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(t.Steps.Any(s => s.Type == RoundStepType.Release), Is.False, "pod 1's cell is behind pod 0's");
            while (t.Pending)
            {
                t.Advance(10_000);
                log.AddRange(t.LastEvents);
            }

            RoundEvent second = log.Last(e => e.Type == RoundEventType.Depart);
            RoundEvent[] completes = log.Where(e => e.Type == RoundEventType.Complete).ToArray();
            Assert.That(completes.Select(e => e.Pod), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(completes.Select(e => e.At), Is.All.EqualTo(second.At + 300_000), "the places free 300 ms after the last departure");
            Assert.That(completes[0].At, Is.LessThan(second.DoneAt), "couriers outlive their places");
        }

        [Test]
        public void TheTailStartsWhenTheColumnsAreEmptyAndNoBridgeIsLeft()
        {
            RoundLevel level = Corridor(2, new RoundPod('G', 1), new RoundPod('G', 1));
            var t = new RoundTimeline(level);
            Assert.That(t.Tail, Is.False);
            t.Commit(RoundAction.Tap(0));
            Assert.That(t.Tail, Is.True, "column 0 held both pods? no: one column each below");
        }

        [Test]
        public void InvariantsHoldThroughAPlayAndCatchALostReservation()
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(3));
            var t = new RoundTimeline(level);
            foreach (RoundAction a in new RoundSolver().Route(level).Route!)
            {
                while (t.Check(a) != RoundRefusal.None && t.Pending)
                {
                    t.Advance(100_000);
                }

                Assert.That(t.Commit(a).Accepted);
                t.Advance(1_000_000);
            }

            t.AdvanceUntilIdle();
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));

            var u = new RoundTimeline(level);
            u.Commit(u.Legal()[0]);
            int cell = u.Couriers[0].Cell;
            u.ReservedArray[cell] = -1;
            Assert.Throws<InvalidOperationException>(() => u.Assert());
        }

        [Test]
        public void StatusIsPlayingWhileWorkIsPending()
        {
            RoundLevel level = Corridor(1);
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            Assert.That(t.Pending);
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Playing));
            t.AdvanceUntilIdle();
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(t.Now, Is.EqualTo(1_150_000), "time stays at the last step once nothing is pending");
            Assert.That(t.Advance(5_000_000), Is.False);
            Assert.That(t.Now, Is.EqualTo(1_150_000), "model time stands still while nothing is pending");
        }
    }
}
