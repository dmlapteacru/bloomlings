using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>The four boosters in live time (spec 006 FR-012, data model "Booster commands", SC-008).</summary>
    public class RoundBoostersTests
    {
        // Six pods whose materials are walled off (they park in their places) and one G pod with five reachable cells.
        private static RoundLevel Parking(bool link = false) => RoundTestLevels.Board(
            new[]
            {
                "#######",
                "PBACDV#",
                "#######",
                "#GGGGG#",
                "#.....#",
                "###.###",
            },
            new[]
            {
                new RoundPod('P', 1, group: link ? "A" : null), new RoundPod('B', 1), new RoundPod('A', 1),
                new RoundPod('C', 1, group: link ? "A" : null), new RoundPod('D', 1), new RoundPod('V', 1), new RoundPod('G', 5),
            },
            new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6 } },
            entryX: 3);

        private static void Reconciles(RoundTimeline t)
        {
            t.Assert();
            var cells = new int[16];
            foreach (sbyte c in t.State.Board)
            {
                if (c >= 0)
                {
                    cells[c]++;
                }
            }

            var work = new int[16];
            for (int p = 0; p < t.State.Remaining.Length; p++)
            {
                work[t.State.Prepared.Colours[p]] += t.State.Remaining[p];
            }

            foreach (RoundCourier j in t.Couriers.Where(j => j.Clear > t.Now))
            {
                work[t.State.Prepared.Colours[j.Pod]]++;
            }

            Assert.That(work, Is.EqualTo(cells), "cells = unsent + outbound, per material");
        }

        [Test]
        public void ExtraSlotAddsALastPlaceAtOnceOncePerLevel()
        {
            var t = new RoundTimeline(Parking());
            foreach (int pod in new[] { 0, 3, 1, 4, 2 })
            {
                Assert.That(t.Commit(RoundAction.Tap(pod)).Accepted);
            }

            Assert.That(t.Status, Is.EqualTo(RoundStatus.Jammed));
            RoundCommitResult r = RoundBoosters.ExtraSlot(t);
            Assert.That(r.Accepted);
            Assert.That(r.Events.Select(e => e.ToString()), Is.EqualTo(new[] { "0 extra-slot slot=5" }));
            Assert.That(t.State.Slots, Is.EqualTo(new[] { 0, 3, 1, 4, 2, -1 }));
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Playing), "the jam is rescued");
            Assert.That(RoundBoosters.ExtraSlot(t).Refusal, Is.EqualTo(RoundRefusal.AlreadyUsed));
            Assert.That(t.Commit(RoundAction.Tap(6)).Events[0].ToString(), Is.EqualTo("0 commit pod=6 slot=5"));
            Reconciles(t);
        }

        [Test]
        public void ReturnSendsAWaitingPodBackToTheTopOfItsColumn()
        {
            var t = new RoundTimeline(Parking());
            t.Commit(RoundAction.Tap(0));
            t.Commit(RoundAction.Tap(1));
            t.Commit(RoundAction.Tap(6));
            Assert.That(RoundBoosters.CanReturn(t, 6), Is.EqualTo(RoundRefusal.CourierInFlight), "a G courier is on the way");
            Assert.That(RoundBoosters.CanReturn(t, 2), Is.EqualTo(RoundRefusal.NotReturnable), "not in a place");

            RoundCommitResult r = RoundBoosters.Return(t, 0);
            Assert.That(r.Accepted);
            Assert.That(r.Events.Select(e => e.ToString()), Is.EqualTo(new[] { "0 recall pod=0 slot=0 column=0" }));
            Assert.That(t.State.Slots.Take(3), Is.EqualTo(new[] { -1, 1, 6 }), "its place frees, the others keep theirs");
            Assert.That(t.State.Columns[0], Is.EqualTo(new[] { 1, 0, 2 }), "taken pods stay before the head");
            Assert.That(t.State.Heads[0], Is.EqualTo(1));
            Assert.That(t.State.TopOf(0), Is.EqualTo(0));
            Assert.That(t.State.Remaining[0], Is.EqualTo(1), "with its unsent count");
            Assert.That(RoundEvents.Signature(t), Does.EndWith("|1,0,2;3,4,5;6"), "changed columns show in the signature");
            Reconciles(t);

            // The G pod can be returned once its couriers are home; its cleared cells stay cleared.
            while (t.InFlight[6] > 0)
            {
                t.Advance(50_000);
            }

            int remaining = t.State.Remaining[6];
            int cleared = t.ProcessedCells;
            if (remaining > 0)
            {
                Assert.That(RoundBoosters.Return(t, 6).Accepted);
                Assert.That(t.State.TopOf(2), Is.EqualTo(6));
                Assert.That(t.ProcessedCells, Is.EqualTo(cleared));
                Assert.That(t.Steps.Any(s => s.Type == RoundStepType.Check && s.Key == 6), Is.False, "its check step is dropped");
                Reconciles(t);
            }
        }

        [Test]
        public void ALinkReturnsTogetherOnlyWhenBothMembersMay()
        {
            var t = new RoundTimeline(Parking(link: true));
            Assert.That(t.Commit(RoundAction.Tap(0)).Accepted, "the link launches as one");
            Assert.That(t.State.Slots.Take(2), Is.EqualTo(new[] { 0, 3 }));
            RoundCommitResult r = RoundBoosters.Return(t, 3);
            Assert.That(r.Events.Select(e => e.ToString()), Is.EqualTo(new[] { "0 recall pod=0 slot=0 column=0", "0 recall pod=3 slot=1 column=1" }));
            Assert.That((t.State.TopOf(0), t.State.TopOf(1)), Is.EqualTo((0, 3)), "both on top again, at equal depth");
            Assert.That(RoundRules.CanTap(t.State, 0), Is.EqualTo(RoundRefusal.None));
            Reconciles(t);
        }

        [Test]
        public void AFinishedPodWaitingForItsPartnerCannotBeReturned()
        {
            RoundLevel level = RoundTestLevels.Board(new[] { "#####", "P####", "#####", "##G##", "##.##" },
                new[] { new RoundPod('G', 1, group: "A"), new RoundPod('P', 1, group: "A") }, new[] { new[] { 0 }, new[] { 1 } }, entryX: 2);
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            Assert.That(t.State.Remaining[0], Is.EqualTo(0));
            Assert.That(RoundBoosters.CanReturn(t, 0), Is.EqualTo(RoundRefusal.NotReturnable));
            Assert.That(RoundBoosters.CanReturn(t, 1), Is.EqualTo(RoundRefusal.NotReturnable), "its partner neither: the link returns whole");
        }

        [Test]
        public void ShuffleMovesOnlyRevealedSinglePodsAndKeepsAWin()
        {
            // L19 has links and hidden pods.
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(19));
            var t = new RoundTimeline(level);
            RoundState before = t.State.Clone();
            RoundCommitResult r = RoundBoosters.Shuffle(t);
            Assert.That(r.Accepted);
            RoundState after = t.State;
            Assert.That(r.Events.Single().ToString(), Is.EqualTo("0 shuffle changed=1"));
            for (int c = 0; c < after.Columns.Length; c++)
            {
                for (int k = 0; k < after.Columns[c].Count; k++)
                {
                    int was = before.Columns[c][k];
                    if (!before.Revealed[was] || level.Pods[was].Group != null)
                    {
                        Assert.That(after.Columns[c][k], Is.EqualTo(was), "hidden pods and links keep their positions");
                    }

                    Assert.That(after.ColumnOf[after.Columns[c][k]], Is.EqualTo(c));
                }
            }

            Assert.That(after.Columns.SelectMany(c => c).OrderBy(p => p), Is.EqualTo(Enumerable.Range(0, level.Pods.Count)));
            Assert.That(after.Revealed, Is.EqualTo(before.Revealed), "hidden stay hidden");
            Assert.That(new RoundSolver(20000).Winnable(t), Is.True);
            Reconciles(t);

            // The same commands give the same arrangement.
            var again = new RoundTimeline(level);
            RoundBoosters.Shuffle(again);
            Assert.That(RoundEvents.Signature(again), Is.EqualTo(RoundEvents.Signature(t)));
            Assert.That(t.ShufflesUsed, Is.EqualTo(1));
        }

        [Test]
        public void ShuffleKeepsTheOrderWhenNoWinIsFound()
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(4));
            var t = new RoundTimeline(level);
            string before = RoundEvents.Signature(t);
            RoundCommitResult r = RoundBoosters.Shuffle(t, seed: 7, budget: 1);
            Assert.That(r.Accepted);
            Assert.That(r.Events.Single().ToString(), Is.EqualTo("0 shuffle changed=0"));
            Assert.That(RoundEvents.Signature(t), Is.EqualTo(before), "the solver ran out of budget: unknown is not a win");

            RoundLevel parking = Parking();
            var p = new RoundTimeline(parking);
            p.Commit(RoundAction.Tap(6));
            Assert.That(RoundBoosters.CanShuffle(p), Is.EqualTo(RoundRefusal.None));
        }

        [Test]
        public void BloomBurstClearsTheFreeCellsAndSettlesTheMaterialsPods()
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(1));
            var t = new RoundTimeline(level);
            RoundAction first = t.Legal()[0];
            t.Commit(first);
            t.Advance(2_000_000);
            Assert.That(t.Couriers.Count(j => j.Clear > t.Now), Is.GreaterThan(0), "couriers are on the way");
            char code = level.Pods[first.Pod].Code;
            int material = RoundMaterial.IndexOf(code);
            int[] reserved = Enumerable.Range(0, t.State.Board.Length).Where(c => t.Reserved[c] >= 0).ToArray();
            int free = Enumerable.Range(0, t.State.Board.Length).Count(c => t.State.Board[c] == material && t.Reserved[c] < 0);
            int cleared = t.ProcessedCells;

            RoundCommitResult r = RoundBoosters.BloomBurst(t, code);
            Assert.That(r.Accepted);
            Assert.That(r.Events[0].ToString(), Is.EqualTo($"{t.Now} burst code={code} cells={free}"));
            Assert.That(r.Events.Count(e => e.Type == RoundEventType.BurstClear), Is.EqualTo(free));
            Assert.That(t.ProcessedCells, Is.EqualTo(cleared + free));
            foreach (int c in reserved)
            {
                Assert.That(t.State.Board[c], Is.EqualTo((sbyte)material), "a reserved cell stays with its courier");
            }

            Assert.That(Enumerable.Range(0, level.Pods.Count).Where(p => level.Pods[p].Code == code).Select(p => t.State.Remaining[p]), Is.All.EqualTo(0));
            Assert.That(t.Steps.Any(s => s.Type == RoundStepType.Release && s.Key == first.Pod), "the pod releases by the normal rule");
            Reconciles(t);

            var log = new List<RoundEvent>();
            while (t.Pending)
            {
                t.Advance(100_000);
                log.AddRange(t.LastEvents);
            }

            Assert.That(log.Count(e => e.Type == RoundEventType.Clear), Is.EqualTo(reserved.Length), "the couriers finish their cells");
            Assert.That(log.Any(e => e.Type == RoundEventType.Complete && e.Pod == first.Pod));
            Assert.That(RoundBoosters.CanBloomBurst(t, code), Is.EqualTo(RoundRefusal.MaterialNotRevealed), "nothing of it is left to do");
            Assert.That(RoundBoosters.CanBloomBurst(t, 'x'), Is.EqualTo(RoundRefusal.MaterialNotRevealed));
            Reconciles(t);
        }

        [Test]
        public void BloomBurstNeedsARevealedPodOfTheMaterial()
        {
            RoundLevel level = RoundTestLevels.Board(new[] { "#####", "#PBA#", "#####", "#...#", "##.##" },
                new[] { new RoundPod('P', 1), new RoundPod('B', 1, RoundMystery.Depth), new RoundPod('A', 1) }, new[] { new[] { 0, 1 }, new[] { 2 } }, entryX: 2);
            var t = new RoundTimeline(level);
            Assert.That(RoundBoosters.CanBloomBurst(t, 'B'), Is.EqualTo(RoundRefusal.MaterialNotRevealed));
            Assert.That(RoundBoosters.CanBloomBurst(t, 'P'), Is.EqualTo(RoundRefusal.None), "unreachable cells clear too");
            Assert.That(RoundBoosters.BloomBurst(t, 'A').Accepted);
            Assert.That(t.State.Remaining[2], Is.EqualTo(0), "a pod still in its column is settled too");
            Reconciles(t);
            t.Commit(RoundAction.Tap(2));
            t.AdvanceUntilIdle();
            Assert.That(t.State.Slots.All(s => s < 0), "a settled pod tapped later releases at once after the lag");
        }

        [Test]
        public void BoostersAreRefusedOnceTheLevelIsWon()
        {
            RoundLevel level = RoundTestLevels.Board(new[] { "##G##", "##.##" }, new[] { new RoundPod('G', 1) }, new[] { new[] { 0 }, new int[0] }, entryX: 2);
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(RoundBoosters.CanExtraSlot(t), Is.EqualTo(RoundRefusal.LevelWon));
            Assert.That(RoundBoosters.CanShuffle(t), Is.EqualTo(RoundRefusal.LevelWon));
            Assert.That(RoundBoosters.CanReturn(t, 0), Is.EqualTo(RoundRefusal.LevelWon));
            Assert.That(RoundBoosters.CanBloomBurst(t, 'G'), Is.EqualTo(RoundRefusal.LevelWon));
        }

        private static IEnumerable<int> FuzzLevels() => new[] { 2, 11, 15, 16, 20 };

        [TestCaseSource(nameof(FuzzLevels))]
        public void RandomPlayWithBoostersKeepsEveryInvariant(int number)
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(number));
            for (uint seed = 1; seed <= 6; seed++)
            {
                var t = new RoundTimeline(level);
                var r = new RoundXorShift32(seed * 7919);
                for (int turn = 0; turn < 400 && t.Status != RoundStatus.Won; turn++)
                {
                    int roll = r.Choice(20);
                    if (roll == 0)
                    {
                        RoundBoosters.ExtraSlot(t);
                    }
                    else if (roll == 1)
                    {
                        int[] waiting = t.State.Slots.Where(p => p >= 0).ToArray();
                        if (waiting.Length > 0)
                        {
                            RoundBoosters.Return(t, waiting[r.Choice(waiting.Length)]);
                        }
                    }
                    else if (roll == 2 && t.ShufflesUsed < 2)
                    {
                        RoundBoosters.Shuffle(t, seed, budget: 500);
                    }
                    else if (roll == 3)
                    {
                        RoundBoosters.BloomBurst(t, RoundMaterial.Codes[r.Choice(16)]);
                    }
                    else
                    {
                        List<RoundAction> moves = t.Legal();
                        if (moves.Count > 0 && roll % 2 == 0)
                        {
                            t.Commit(moves[r.Choice(moves.Count)]);
                        }
                        else if (t.Pending)
                        {
                            t.Advance(r.Choice(900_000));
                        }
                        else if (moves.Count == 0)
                        {
                            // Jammed or stuck: only a booster can go on.
                            continue;
                        }
                    }

                    Reconciles(t);
                }
            }
        }
    }
}
