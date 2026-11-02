using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The round level screen's engine-free clock and animator (spec 006 T043, T044, T047; research R8): the display clock
    /// drives the live timeline, the animator shows what the events say (walkers on their routes, the cubes' clear moments,
    /// places, reveals, tubes), the speed never changes an outcome, and the clearing styles take their legs from the
    /// timeline.
    /// </summary>
    public class RoundAnimatorTests
    {
        // 7 × 5 with the frame: the gate in the bottom wall's middle, an open ring, three G cubes in the middle row.
        private static RoundLevel Plain() => new RoundLevel(1, 7, 5,
            new[] { "###.###", "#.....#", "#.GGG.#", "#.....#", "#######" },
            new[] { new RoundPod('G', 3) },
            new IReadOnlyList<int>[] { new[] { 0 } });

        // Two linked pods, each on top of its column: G and P.
        private static RoundLevel Linked() => new RoundLevel(2, 7, 5,
            new[] { "###.###", "#.....#", "#.GGPP#", "#.....#", "#######" },
            new[] { new RoundPod('G', 2, group: "A"), new RoundPod('P', 2, group: "A") },
            new IReadOnlyList<int>[] { new[] { 0 }, new[] { 1 } });

        // A hidden P pod under the G pod.
        private static RoundLevel Hidden() => new RoundLevel(3, 7, 5,
            new[] { "###.###", "#.....#", "#.GGPP#", "#.....#", "#######" },
            new[] { new RoundPod('G', 2), new RoundPod('P', 2, RoundMystery.Depth) },
            new IReadOnlyList<int>[] { new[] { 0, 1 } });

        private static void RunOut(RoundPlay play, Func<int, double> frame, int speed)
        {
            for (int k = 0; k < 100_000 && (play.Timeline.Pending || play.Animator.Busy); k++)
            {
                play.Advance(frame(k), speed);
            }
        }

        [Test]
        public void ATap_SendsWalkersFromTheGate_AndEveryCubeShowsItsClearAtItsEventTime()
        {
            var play = new RoundPlay(Plain());
            RoundCommitResult tap = play.Commit(RoundAction.Tap(0));
            Assert.That(tap.Accepted, Is.True);
            Assert.That(play.Inputs.Single().ToString(), Is.EqualTo("tap pod=0 at=0"));
            Assert.That(play.Animator.PodIn(0), Is.EqualTo(0));
            Assert.That(play.Animator.Walkers.Count, Is.EqualTo(1), "one courier leaves at once, the next after the gap");
            RoundWalk first = play.Animator.Walkers[0];
            Assert.That(first.Route[0], Is.EqualTo(3), "the route starts at the gate");
            Assert.That(first.Route[first.Route.Count - 1], Is.EqualTo(first.Cell));
            Assert.That(first.Start < first.Arrive && first.Arrive < first.Clear && first.Clear < first.Done, Is.True);
            Assert.That(first.Material, Is.EqualTo(RoundMaterial.IndexOf('G')));

            var clears = new Dictionary<int, long>();
            for (int k = 0; k < 10_000 && (play.Timeline.Pending || play.Animator.Busy); k++)
            {
                foreach (RoundEvent e in play.Advance(1.0 / 60.0, 1))
                {
                    if (e.Type == RoundEventType.Clear)
                    {
                        clears[e.Cell] = e.At + play.IdleUs;
                    }
                }
            }

            Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(clears.Count, Is.EqualTo(3));
            foreach (KeyValuePair<int, long> clear in clears)
            {
                Assert.That(play.Animator.ClearedAt(clear.Key), Is.EqualTo(clear.Value), "the cube's clear animation keys on its clear event");
                Assert.That(play.Animator.MaterialAt(clear.Key), Is.EqualTo((int)RoundLevel.Open));
            }

            Assert.That(play.Animator.Walkers, Is.Empty);
            Assert.That(play.Animator.Leaving(0).Pod, Is.EqualTo(0), "the finished pod puffs off its place");
            Assert.That(play.Animator.ShowsRulesState(play.Timeline, out string difference), Is.True, difference);
        }

        [Test]
        public void TheWalkerBody_Is1Point4CubesOnBigCubes_And1Point6OnSmallOnes()
        {
            // the owner, 2026-10-31: "1.6 when the cubes get small, 1.4 on big ones"
            Assert.That(ClearStyles.WalkerBodyCubes(10f), Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(ClearStyles.WalkerBodyCubes(ClearStyles.BigCubesAcross), Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(ClearStyles.WalkerBodyCubes((ClearStyles.BigCubesAcross + ClearStyles.SmallCubesAcross) / 2f), Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(ClearStyles.WalkerBodyCubes(ClearStyles.SmallCubesAcross), Is.EqualTo(1.6f).Within(1e-5f));
            Assert.That(ClearStyles.WalkerBodyCubes(60f), Is.EqualTo(1.6f).Within(1e-5f));
        }

        [Test]
        public void OnSmallCubes_TheWalkersAreOneAndAHalfTimesBigger_AndBigCubesKeepTheirSize()
        {
            // the owner's phone report of 2026-11-02 (L54): "the heroes on such small cubes should be bigger, about one and a half times"
            Assert.That(ClearStyles.SmallCubeBoost(ClearStyles.BigCubesAcross), Is.EqualTo(1f).Within(1e-5f), "L1's big cubes");
            Assert.That(ClearStyles.SmallCubeBoost(ClearStyles.BoostFromCubesAcross), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(ClearStyles.SmallCubeBoost(29f), Is.EqualTo(1.25f).Within(1e-5f), "linear in between");
            Assert.That(ClearStyles.SmallCubeBoost(ClearStyles.BoostFullCubesAcross), Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(ClearStyles.SmallCubeBoost(37f), Is.EqualTo(1.5f).Within(1e-5f), "L54's 37 cubes across");
            Assert.That(ClearStyles.SmallCubeBoost(60f), Is.EqualTo(1.5f).Within(1e-5f));

            Assert.That(ClearStyles.WalkerCubes(ClearStyles.BigCubesAcross), Is.EqualTo(1.4f).Within(1e-5f), "big cubes unchanged");
            Assert.That(ClearStyles.WalkerCubes(37f), Is.EqualTo(ClearStyles.WalkerBodyCubes(37f) * 1.5f).Within(1e-5f));
            Assert.That(ClearStyles.WalkerCubes(ClearStyles.SmallCubesAcross), Is.EqualTo(2.4f).Within(1e-5f));
            for (float across = 10f; across < 60f; across += 0.5f)
            {
                Assert.That(ClearStyles.WalkerCubes(across + 0.5f), Is.GreaterThanOrEqualTo(ClearStyles.WalkerCubes(across)), "never smaller on smaller cubes");
            }

            // one rule for every style and both builds: the live walk carries it
            var points = new List<(float X, float Y)> { (3f, 4f), (3f, 3f) };
            ClearWalk walk = ClearWalk.Live(points, 0, VariantId.Leaf, 0f, 1f, 1.1f, 1.5f, null, ClearStyles.SmallCubesAcross);
            Assert.That(walk.LiveScale * ClearLook.WalkerSize, Is.EqualTo(2.4f).Within(1e-5f));
            ClearWalk big = ClearWalk.Live(points, 0, VariantId.Leaf, 0f, 1f, 1.1f, 1.5f, null, ClearStyles.BigCubesAcross);
            Assert.That(big.LiveScale * ClearLook.WalkerSize, Is.EqualTo(1.4f).Within(1e-5f));
        }

        [Test]
        public void AWalkerHomeInTheRules_IsDrawnUntilItsLookIsHome()
        {
            // the owner, 2026-10-31: the act lasts past the clear by ClearStyles.LiveLinger, so the walk home ends that much later
            var play = new RoundPlay(Plain());
            play.Commit(RoundAction.Tap(0));
            RoundWalk first = play.Animator.Walkers[0];
            bool lingered = false;
            for (int k = 0; k < 10_000 && (play.Timeline.Pending || play.Animator.Busy); k++)
            {
                play.Advance(1.0 / 60.0, 1);
                long now = play.Animator.Now;
                bool inRules = play.Animator.Walkers.Any(w => w.Id == first.Id);
                bool shown = play.Animator.ShownWalkers.Any(w => w.Id == first.Id);
                Assert.That(inRules, Is.EqualTo(now < first.Done), "the rules' courier is home at its return");
                Assert.That(shown, Is.EqualTo(now < RoundAnimator.HomeUs(first)), "drawn until its look is home");
                if (shown && !inRules)
                {
                    lingered = true;
                    Assert.That(play.Animator.Busy, Is.True, "the board still moves while a walker walks home");
                    Assert.That(play.Animator.ShowsRulesState(play.Timeline, out string difference), Is.True, difference);
                }
            }

            Assert.That(lingered, Is.True);
            Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(play.Animator.ShownWalkers, Is.Empty);
        }

        [Test]
        public void TheDisplaySpeed_AndTheFrames_NeverChangeTheOutcome()
        {
            string Final(Func<int, double> frame, int speed)
            {
                var play = new RoundPlay(Linked());
                play.Commit(RoundAction.Tap(0));
                RunOut(play, frame, speed);
                Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
                return RoundEvents.Signature(play.Timeline) + "|" + string.Join(",", Enumerable.Range(0, 35).Select(c => play.Animator.ClearedAt(c)));
            }

            string normal = Final(_ => 1.0 / 60.0, 1);
            Assert.That(Final(_ => 1.0 / 60.0, RoundPlay.SpeedUp), Is.EqualTo(normal), "the speed-up");
            Assert.That(Final(k => k % 5 == 0 ? 0.1 : 0.007, RoundProfile.TailDisplaySpeed), Is.EqualTo(normal), "Auto at uneven frames");
        }

        [Test]
        public void WhileNothingIsPending_TheModelClockStands_AndTheIdleTimeIsKeptApart()
        {
            var play = new RoundPlay(Plain());
            play.Advance(1.5, 1);
            Assert.That(play.Timeline.Now, Is.EqualTo(0L));
            Assert.That(play.IdleUs, Is.EqualTo(1_500_000L));
            Assert.That(play.ClockUs, Is.EqualTo(1_500_000L));
            play.Commit(RoundAction.Tap(0));
            Assert.That(play.Inputs[0].AtUs, Is.EqualTo(0L), "a command carries the model time, not the display clock");
            Assert.That(play.Animator.Walkers[0].Start, Is.EqualTo(1_500_000L), "events show at model time plus the idle offset");
            Assert.That(play.Animator.CommittedAt(0), Is.EqualTo(1_500_000L));
        }

        [Test]
        public void Auto_StartsOnTheTail_AtTwiceTheSpeed_WhateverTheSpeedUp()
        {
            var play = new RoundPlay(Hidden());
            Assert.That(play.Auto, Is.False);
            Assert.That(play.Speed(false), Is.EqualTo(1));
            Assert.That(play.Speed(true), Is.EqualTo(RoundPlay.SpeedUp));
            play.Commit(RoundAction.Tap(0));
            Assert.That(play.Auto, Is.False, "a pod still waits in the column");
            play.Commit(RoundAction.Tap(1));
            Assert.That(play.Auto, Is.True, "every column is empty and no bridge is left to choose");
            Assert.That(play.Speed(false), Is.EqualTo(RoundProfile.TailDisplaySpeed));
            Assert.That(play.Speed(true), Is.EqualTo(RoundProfile.TailDisplaySpeed));
        }

        [Test]
        public void AHiddenPod_RevealsOnTop_AndItsRevealIsTimed()
        {
            var play = new RoundPlay(Hidden());
            Assert.That(play.Animator.Revealed(1), Is.False);
            play.Advance(0.25, 1);
            play.Commit(RoundAction.Tap(0));
            Assert.That(play.Animator.Revealed(1), Is.True, "it shows its colour and count as soon as it is on top (FR-007)");
            Assert.That(play.Animator.RevealedAt(1), Is.EqualTo(250_000L));
            Assert.That(RoundLook.RevealGlow(0.5f), Is.GreaterThan(0f));
            Assert.That(RoundLook.RevealGlow(RoundLook.RevealGlowSeconds), Is.EqualTo(0f));
        }

        // 9 × 7 with the frame: a 5 × 3 block of G cubes, its two middle cubes of the middle row hidden (17 §8 «Скрытые клетки»).
        private static RoundLevel HiddenCells() => new RoundLevel(4, 9, 7,
            new[] { "####.####", "#.......#", "#.GGGGG.#", "#.GGGGG.#", "#.GGGGG.#", "#.......#", "#########" },
            new[] { new RoundPod('G', 15) },
            new IReadOnlyList<int>[] { new[] { 0 } },
            mechanics: new[] { "cells" },
            hiddenCells: new[] { 30, 31 });

        [Test]
        public void AHiddenCell_IsVeiledUntilItsRevealPlays_AndNoWalkerTargetsItBefore()
        {
            var play = new RoundPlay(HiddenCells());
            Assert.That(play.Animator.Veiled(30) && play.Animator.Veiled(31), Is.True);
            Assert.That(play.Animator.Veiled(29), Is.False, "a cube on the start front is no hidden cell");
            play.Advance(0.1, 1);
            play.Commit(RoundAction.Tap(0));
            var revealedAt = new Dictionary<int, long>();
            for (int k = 0; k < 100_000 && (play.Timeline.Pending || play.Animator.Busy); k++)
            {
                IReadOnlyList<RoundEvent> fresh = play.Advance(1.0 / 60.0, 1);
                foreach (RoundEvent e in fresh.Where(e => e.Type == RoundEventType.CellReveal))
                {
                    revealedAt[e.Cell] = play.Animator.CellRevealedAt(e.Cell);
                    Assert.That(play.Animator.CellRevealSince(e.Cell), Is.InRange(0f, RoundLook.CellRevealSeconds), "its pop starts with its reveal");
                }

                foreach (RoundWalk walk in play.Animator.Walkers)
                {
                    Assert.That(play.Animator.Veiled(walk.Cell), Is.False, "no walker goes to a cube still showing its \"?\"");
                }
            }

            Assert.That(revealedAt.Keys.OrderBy(c => c), Is.EqualTo(new[] { 30, 31 }));
            Assert.That(play.Animator.Veiled(30) || play.Animator.Veiled(31), Is.False);
            Assert.That(play.Animator.CellRevealSince(30), Is.EqualTo(-1f), "the pop has ended");
            Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(play.Animator.ShowsRulesState(play.Timeline, out string difference), Is.True, difference);
        }

        [Test]
        public void ARevealAheadOfTheDisplayClock_StaysVeiledUntilItsTime()
        {
            RoundLevel level = HiddenCells();
            var timeline = new RoundTimeline(level);
            var animator = new RoundAnimator(level);
            animator.Reset(timeline, 0);
            timeline.Commit(RoundAction.Tap(0));
            var reveals = new List<RoundEvent>();
            while (timeline.Pending)
            {
                timeline.Advance(50_000);
                reveals.AddRange(timeline.LastEvents.Where(e => e.Type == RoundEventType.CellReveal));
            }

            RoundEvent first = reveals[0];
            Assert.That(first.At, Is.GreaterThan(0L));
            animator.Observe(new[] { first }, 0);
            Assert.That(animator.Veiled(first.Cell), Is.True, "before its moment");
            animator.SetTime(first.At);
            Assert.That(animator.Veiled(first.Cell), Is.False);
            Assert.That(animator.CellRevealSince(first.Cell), Is.EqualTo(0f));
            animator.SetTime(first.At + RoundAnimator.CellRevealUs);
            Assert.That(animator.CellRevealSince(first.Cell), Is.EqualTo(-1f));
        }

        // 9 × 8 with the frame: A under G with a hole at cell 40 in the G block, its key; the latch in column 1 after one pod,
        // holding pod 2 (17 §8).
        private static RoundLevel Latched() => new RoundLevel(4, 9, 8,
            new[] { "####.####", "#.......#", "#.AAAAA.#", "#.GGGGG.#", "#.GG.GG.#", "#.GGGGG.#", "#.......#", "#########" },
            new[] { new RoundPod('A', 5), new RoundPod('G', 7), new RoundPod('G', 7) },
            new IReadOnlyList<int>[] { new[] { 0 }, new[] { 1, 2 } },
            mechanics: new[] { "latch" },
            latches: new[] { new RoundLatch(1, 1, 40) });

        [Test]
        public void ALatch_IsDrawnOnThePodItHoldsUntilItsOpeningPlays_ThenLeavesIt()
        {
            RoundLevel level = Latched();
            Assert.That(RoundLevelValidator.ValidateDefinition(level), Is.Empty);
            var play = new RoundPlay(level);
            var items = new List<RoundQueueItem>();
            play.Animator.ColumnItems(1, 3, items);
            Assert.That(items.Select(i => (i.IsLatch, i.Id, i.Row, i.Holds)), Is.EqualTo(new[] { (false, 1, 0, false), (true, 0, 1, true), (false, 2, 1, false) }), "the latch shares the row of the pod it holds");
            Assert.That(play.Animator.LatchOfKey(40), Is.EqualTo(0));
            Assert.That(play.Animator.HeldPod(0), Is.EqualTo(2));
            Assert.That(play.Animator.KeyMaterial(0), Is.EqualTo(RoundMaterial.IndexOf('G')), "the key in the held pod's colour");
            Assert.That(play.Animator.LatchPips(0), Is.EqualTo(0), "a single latch needs no pips");
            play.Advance(0.1, 1);
            play.Commit(RoundAction.Tap(0));
            play.Commit(RoundAction.Tap(1));
            play.Animator.ColumnItems(1, 3, items);
            Assert.That(items.Select(i => (i.IsLatch, i.Id, i.Row)), Is.EqualTo(new[] { (true, 0, 0), (false, 2, 0) }), "on top, on its pod: one item in the column");
            Assert.That(play.Animator.HeldBehind(items, 1), Is.True, "the pod behind a closed latch takes no tap");
            bool opened = false;
            for (int k = 0; k < 100_000 && (play.Timeline.Pending || play.Animator.Busy); k++)
            {
                IReadOnlyList<RoundEvent> fresh = play.Advance(1.0 / 60.0, 1);
                if (fresh.Any(e => e.Type == RoundEventType.LatchOpen))
                {
                    opened = true;
                    Assert.That(play.Animator.LatchClosed(0), Is.False);
                    Assert.That(play.Animator.LatchOpenSince(0), Is.InRange(0f, RoundLook.LatchOpenSeconds), "its opening starts with its event");
                    play.Animator.ColumnItems(1, 3, items);
                    Assert.That(items[0].IsLatch && !play.Animator.HeldBehind(items, 1), Is.True, "still drawn while it opens; the pod behind takes taps");
                    play.Commit(RoundAction.Tap(2));
                }
            }

            Assert.That(opened, Is.True);
            Assert.That(play.Animator.LatchShown(0), Is.False, "the opening has ended");
            Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(play.Animator.ShowsRulesState(play.Timeline, out string difference), Is.True, difference);
            Assert.That(RoundLook.KeyOpen(RoundLook.LatchOpenSeconds).Alpha, Is.EqualTo(0f));
            Assert.That(RoundLook.KeyOpen(-1f), Is.EqualTo((0f, 1f, 1f)));
            Assert.That(RoundLook.PadlockOpen(-1f), Is.EqualTo((false, 0f, 1f, 1f)));
            Assert.That(RoundLook.PadlockOpen(RoundLook.LatchOpenSeconds).Alpha, Is.EqualTo(0f));
            Assert.That(RoundLook.AcornHop(RoundLook.LatchOpenSeconds).Alpha, Is.EqualTo(0f));
        }

        [Test]
        public void LatchesHoldingPodsOfOneColour_CarryPips_OthersNone()
        {
            // Three latches: two hold G pods (pips 1 and 2), one holds an A pod (none).
            var level = new RoundLevel(4, 9, 8,
                new[] { "####.####", "#.......#", "#.AAAAA.#", "#.GGGGG.#", "#.GG.GG.#", "#.GGGGG.#", "#.......#", "#########" },
                new[] { new RoundPod('A', 2), new RoundPod('G', 7), new RoundPod('A', 3), new RoundPod('G', 7), new RoundPod('G', 7) },
                new IReadOnlyList<int>[] { new[] { 0, 1 }, new[] { 2, 3 }, new[] { 4 } },
                mechanics: new[] { "latch" },
                latches: new[] { new RoundLatch(0, 1, 40), new RoundLatch(1, 1, 10), new RoundLatch(1, 0, 12) });
            var animator = new RoundAnimator(level);
            Assert.That((animator.LatchPips(0), animator.LatchPips(1), animator.LatchPips(2)), Is.EqualTo((1, 2, 0)));
            Assert.That(RoundAnimator.LatchPipsOf(level, 1), Is.EqualTo(2));
            Assert.That(animator.KeyMaterial(2), Is.EqualTo(RoundMaterial.IndexOf('A')));
        }

        [Test]
        public void ALinkedPair_HasItsTubeInTheColumns_ThenInThePlaces()
        {
            var play = new RoundPlay(Linked());
            RoundTube columns = play.Animator.Tubes(3).Single();
            Assert.That((columns.First, columns.Second, columns.Place), Is.EqualTo((0, 1, RoundTubePlace.Columns)));
            play.Commit(RoundAction.Tap(1));
            RoundTube places = play.Animator.Tubes(3).Single();
            Assert.That(places.Place, Is.EqualTo(RoundTubePlace.Places));
            Assert.That(play.Animator.PlaceOf(0) >= 0 && play.Animator.PlaceOf(1) >= 0, Is.True, "one tap launches the pair");
        }

        // G pod 0 on top of column 0 linked to P pod 1 fourth in column 1 (deeper than three rows), and, in the two-group
        // version, P pod 6 on top of column 2 linked to P pod 5 fifth in column 1.
        private static RoundLevel DeepPartner(bool twoGroups) => twoGroups
            ? new RoundLevel(5, 11, 5,
                new[] { "#####.#####", "#.........#", "#.GGGGPPP.#", "#.........#", "###########" },
                new[] { new RoundPod('G', 1, group: "A"), new RoundPod('P', 1, group: "A"), new RoundPod('G', 1), new RoundPod('G', 1), new RoundPod('G', 1), new RoundPod('P', 1, group: "B"), new RoundPod('P', 1, group: "B") },
                new IReadOnlyList<int>[] { new[] { 0 }, new[] { 2, 3, 4, 1, 5 }, new[] { 6 } })
            : new RoundLevel(5, 9, 5,
                new[] { "####.####", "#.......#", "#.GGGGP.#", "#.......#", "#########" },
                new[] { new RoundPod('G', 1, group: "A"), new RoundPod('P', 1, group: "A"), new RoundPod('G', 1), new RoundPod('G', 1), new RoundPod('G', 1) },
                new IReadOnlyList<int>[] { new[] { 0 }, new[] { 2, 3, 4, 1 } });

        [Test]
        public void APartnerDeeperThanTheRows_GetsARoutedTube_ThenTheNormalTubeReplacesIt()
        {
            // the owner's phone report of 2026-11-02 (L54): "it says there is a link, but I can't see it"; the review: the tube
            // must link them really, or go down
            var play = new RoundPlay(DeepPartner(false));
            RoundAnimator a = play.Animator;
            var marks = new List<RoundLinkMark>();
            Assert.That(a.RowOf(1), Is.EqualTo(3));
            Assert.That(a.Tubes(3), Is.Empty, "no pod-to-pod tube to a pod the tray does not show");
            a.LinkMarks(3, marks);
            RoundLinkMark mark = marks.Single();
            Assert.That((mark.Pod, mark.ShownColumn, mark.Partner, mark.Column, mark.Row, mark.ToRight, mark.Gap), Is.EqualTo((0, 0, 1, 1, 3, true, 0)));
            Assert.That(mark.Material, Is.EqualTo(RoundMaterial.IndexOf('G')), "in the shown pod's colour, never the hidden one's");
            Assert.That((mark.Lane, mark.Lanes), Is.EqualTo((0, 1)));

            // the tap waits for the hidden partner
            Assert.That(play.Check(RoundAction.Tap(0)), Is.EqualTo(RoundRefusal.PartnerNotExposed));

            // four rows shown: the normal tube and no route
            Assert.That(a.Tubes(4).Single().Place, Is.EqualTo(RoundTubePlace.Columns));
            a.LinkMarks(4, marks);
            Assert.That(marks, Is.Empty);

            // the top pod of column 1 leaves: the partner comes into the third row, the normal tube replaces the route
            play.Commit(RoundAction.Tap(2));
            Assert.That(a.RowOf(1), Is.EqualTo(2));
            RoundTube tube = a.Tubes(3).Single();
            Assert.That((tube.First, tube.Second, tube.Place), Is.EqualTo((0, 1, RoundTubePlace.Columns)));
            a.LinkMarks(3, marks);
            Assert.That(marks, Is.Empty);
        }

        [Test]
        public void TwoHiddenPartnersInOneColumn_EachGetsItsRoute_InItsGroupsColour()
        {
            var play = new RoundPlay(DeepPartner(true));
            var marks = new List<RoundLinkMark>();
            play.Animator.LinkMarks(3, marks);
            Assert.That(marks.Select(m => (m.Pod, m.Partner, m.Column, m.Row, m.ToRight, m.Gap, m.Material, m.Lanes)), Is.EqualTo(new[]
            {
                (0, 1, 1, 3, true, 0, RoundMaterial.IndexOf('G'), 1),
                (6, 5, 1, 4, false, 1, RoundMaterial.IndexOf('P'), 1),
            }));
        }

        [Test]
        public void TheRoutedTube_RunsDownTheGap_IntoThePartnersPlace_OrOutOfTheScreen()
        {
            var shown = new Box(100f, 200f, 200f, 300f);
            float gapX = 235f;
            Rgba red = RoundLook.ColorOf(RoundMaterial.IndexOf('G'));
            float thickness = 100f * LinkTube.CubeThicknessShare;

            // the place in the layout on screen: three pieces, plugged into both cubes
            var place = new Box(270f, 560f, 370f, 660f);
            LinkRoute route = LinkTube.HiddenRoute(shown, place, gapX, gapX, 420f, 800f, red);
            Assert.That(route.Connected, Is.True);
            Assert.That(route.Pieces.Count, Is.EqualTo(3));
            LinkTubeShape out0 = route.Pieces[0];
            LinkTubeShape down = route.Pieces[1];
            LinkTubeShape into = route.Pieces[2];
            Assert.That(out0.Start.X, Is.LessThan(shown.Right), "plugged in under the shown cube's edge");
            Assert.That(out0.Start.Y, Is.EqualTo(LinkTube.FrontY(shown, LinkTube.CubeFrontShare)).Within(1e-3), "at a tube's height");
            Assert.That(out0.StartCollar && !out0.EndCollar && !out0.Coupling, Is.True);
            Assert.That(Math.Abs(down.Degrees), Is.EqualTo(90f).Within(0.01f), "upright down the gap");
            Assert.That(down.Start.X, Is.EqualTo(gapX).Within(1e-3));
            Assert.That(down.AxisBox.Left, Is.GreaterThan(shown.Right), "the gap: clear of the shown column");
            Assert.That(down.AxisBox.Right, Is.LessThan(place.Left), "and of the partner's");
            Assert.That(!down.StartCollar && !down.EndCollar && !down.Coupling, Is.True);
            Assert.That(into.End.X, Is.GreaterThan(place.Left), "plugged into the place");
            Assert.That(into.End.Y, Is.EqualTo(LinkTube.FrontY(place, LinkTube.CubeFrontShare)).Within(1e-3));
            Assert.That(into.EndCollar && !into.StartCollar, Is.True);
            Assert.That(route.End, Is.EqualTo(into.End));
            Assert.That(route.Pieces.All(piece => piece.StartColor.Equals(red) && piece.EndColor.Equals(red) && Math.Abs(piece.Thickness - thickness) < 1e-3), Is.True);

            // the corners: each piece's round end centred on the corner
            float round = thickness * 0.36f;
            Assert.That(out0.End.X - round, Is.EqualTo(gapX).Within(1e-3));
            Assert.That(down.Start.Y + round, Is.EqualTo(out0.Start.Y).Within(1e-3));
            Assert.That(down.End.Y - round, Is.EqualTo(into.Start.Y).Within(1e-3));
            Assert.That(into.Start.X + round, Is.EqualTo(gapX).Within(1e-3));

            // the place below the screen, the partner's column next door: the tube runs down the gap out of the screen
            var deep = new Box(270f, 960f, 370f, 1060f);
            LinkRoute off = LinkTube.HiddenRoute(shown, deep, gapX, gapX, 420f, 800f, red);
            Assert.That(off.Connected, Is.False);
            Assert.That(off.Pieces.Count, Is.EqualTo(2));
            Assert.That(off.Pieces[1].End.Y, Is.GreaterThan(800f), "past the screen's bottom edge");
            Assert.That(off.Pieces[1].EndCollar, Is.False);

            // a column between: down the gap beside the shown pod, level under the rows to the gap beside the partner's column,
            // then down out of the screen
            var far = new Box(440f, 960f, 540f, 1060f);
            LinkRoute across = LinkTube.HiddenRoute(shown, far, gapX, 405f, 420f, 800f, red);
            Assert.That(across.Connected, Is.False);
            Assert.That(across.Pieces.Count, Is.EqualTo(4));
            Assert.That(across.Pieces[2].Start.Y, Is.EqualTo(420f).Within(1e-3), "level under the rows");
            Assert.That(across.Pieces[3].Start.X, Is.EqualTo(405f).Within(1e-3), "down the gap beside the partner's column");
            Assert.That(across.Pieces[3].End.Y, Is.GreaterThan(800f));
            Assert.That(across.End.X, Is.EqualTo(405f).Within(1e-3));

            // to the left: mirrored
            LinkRoute left = LinkTube.HiddenRoute(new Box(270f, 200f, 370f, 300f), new Box(100f, 560f, 200f, 660f), gapX, gapX, 420f, 800f, red);
            Assert.That(left.Connected, Is.True);
            Assert.That(left.End.X, Is.LessThan(200f));
            Assert.That(left.Pieces[1].Start.X, Is.EqualTo(gapX).Within(1e-3));

            // two routes in one gap lie side by side
            float x0 = LinkTube.HiddenRoute(shown, deep, gapX, gapX, 420f, 800f, red, 0, 2).Pieces[1].Start.X;
            float x1 = LinkTube.HiddenRoute(shown, deep, gapX, gapX, 420f, 800f, red, 1, 2).Pieces[1].Start.X;
            Assert.That(x1 - x0, Is.GreaterThanOrEqualTo(thickness * 0.9f));
            Assert.That((x0 + x1) / 2f, Is.EqualTo(gapX).Within(1e-3));

            // the pieces' pictures: no coupling ring, collars only at the plugs
            Assert.That(into.PictureKey, Is.Not.EqualTo(LinkTube.BetweenCubes(shown, red, place, red).PictureKey));
            Assert.That(UiRaster.LinkTube(80, 24, red, red, false, false, false), Is.Not.EqualTo(UiRaster.LinkTube(80, 24, red, red)));
            Assert.That(AssetSlots.Has(LinkTube.MarkSlot), Is.True);
        }

        [Test]
        public void OnTheRoundScreen_ThePartnersRoute_LeavesTheTrayDownTheGap()
        {
            var play = new RoundPlay(DeepPartner(false));
            ReferenceGameplayRegions r = ScreenLayout.RoundGameplay(1080f, 2424f, new Insets(110f, 63f), 2, 5);
            var marks = new List<RoundLinkMark>();
            play.Animator.LinkMarks(r.PodRows, marks);
            RoundLinkMark mark = marks.Single();
            Box shown = LinkTube.PlaceOf(r, 0, 0);
            LinkRoute route = LinkTube.HiddenRoute(r, mark, shown);
            float x = route.Pieces[1].Start.X;
            Assert.That(x, Is.GreaterThan(r.Columns[0].Right).And.LessThan(r.Columns[1].Left), "down the gap between the columns");
            Assert.That(route.Pieces[1].AxisBox.Bottom, Is.GreaterThan(r.Pod(1, r.PodRows - 1).Bottom), "below the shown rows");
        }

        [Test]
        public void ARestoredAttempt_ShowsItsWalkersWhereTheyAre()
        {
            var play = new RoundPlay(Plain());
            play.Commit(RoundAction.Tap(0));
            play.Advance(0.6, 1);
            RoundTimeline restored = RoundTimeline.FromSnapshot(play.Level, play.Timeline.Snapshot());
            var animator = new RoundAnimator(play.Level);
            animator.Reset(restored, restored.Now);
            Assert.That(animator.Walkers.Select(w => (w.Id, w.Cell, w.Start, w.Done)), Is.EqualTo(play.Animator.Walkers.Select(w => (w.Id, w.Cell, w.Start, w.Done))));
            Assert.That(animator.ShowsRulesState(restored, out string difference), Is.True, difference);
        }

        [Test]
        public void ALiveWalk_TakesItsLegsFromTheTimeline_AndIsTheTintedHero()
        {
            var points = new List<(float X, float Y)> { (3f, 4f), (3f, 3f), (3f, 2f) };
            int material = RoundMaterial.IndexOf('P');
            ClearWalk walk = ClearWalk.Live(points, material, VariantId.Flower, 1f, 1.7f, 1.8f, 2.4f, null);
            Assert.That(walk.IsLive, Is.True);
            // the act lasts past the rules' clear (1.8 s) by the linger, and the walk home (0.6 s) starts that much later
            float linger = ClearStyles.LiveLinger;
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)))
            {
                Assert.That(ClearLook.Holds(style, walk, 1.5f), Is.False, style + ": walking out");
                Assert.That(ClearLook.Holds(style, walk, 1.75f), Is.True, style + ": acting until the clear");
                Assert.That(ClearLook.Holds(style, walk, 1.8f + linger - 0.05f), Is.True, style + ": still acting the linger after the clear");
                Assert.That(ClearLook.Holds(style, walk, 1.8f + linger + 0.05f), Is.False, style + ": the act is over, the cell is cleared");
                float home = ClearStyles.LiveHomeAt(1f, 1.7f, 1.8f, 2.4f);
                Assert.That(home, Is.EqualTo(1.8f + linger + 0.7f).Within(1e-5f), "home at the way out's pace (0.7 s), not the rules' 0.6 s");
                foreach (float now in new[] { 1.4f, home - 0.3f })
                {
                    var list = new FxList();
                    ClearLook.Walker(list, style, walk, now);
                    Assert.That(list.Items.Any(i => i.Kind == FxKind.Character && i.Material == material), Is.True, style + " at " + now + ": the material's walker");
                    Assert.That(list.Items.Where(i => i.Kind == FxKind.Tile).All(i => i.Material == material), Is.True, style + ": its cube");
                }

                var done = new FxList();
                ClearLook.Walker(done, style, walk, home + 0.05f);
                Assert.That(done.Items, Is.Empty, style + ": home");
            }

            var legacy = new ClearWalk(points, VariantId.Flower, 1f, 2f, null);
            Assert.That(legacy.IsLive, Is.False);
            var items = new FxList();
            ClearLook.Walker(items, ClearStyle.Munchers, legacy, 1.2f);
            Assert.That(items.Items.All(i => i.Material == -1), Is.True, "a legacy walk carries no round material");
        }
    }
}
