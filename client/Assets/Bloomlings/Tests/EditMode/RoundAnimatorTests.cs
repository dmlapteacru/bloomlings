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
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)))
            {
                Assert.That(ClearLook.Holds(style, walk, 1.5f), Is.False, style + ": walking out");
                Assert.That(ClearLook.Holds(style, walk, 1.75f), Is.True, style + ": acting until the clear");
                Assert.That(ClearLook.Holds(style, walk, 1.9f), Is.False, style + ": the cell is cleared");
                foreach (float now in new[] { 1.4f, 2.1f })
                {
                    var list = new FxList();
                    ClearLook.Walker(list, style, walk, now);
                    Assert.That(list.Items.Any(i => i.Kind == FxKind.Character && i.Material == material), Is.True, style + " at " + now + ": the material's walker");
                    Assert.That(list.Items.Where(i => i.Kind == FxKind.Tile).All(i => i.Material == material), Is.True, style + ": its cube");
                }

                var done = new FxList();
                ClearLook.Walker(done, style, walk, 2.5f);
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
