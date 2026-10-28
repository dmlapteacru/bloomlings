using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// The same commands give the same outcome whatever the display speed or the frame steps, and across a snapshot and
    /// restore in the middle of work (spec 006 SC-007, FR-011; constitution III).
    /// </summary>
    public class RoundDeterminismTests
    {
        // A random game's commands with their model times (they land while work is in flight).
        private static List<RoundInput> RecordedCommands(RoundLevel level, uint seed)
        {
            var t = new RoundTimeline(level);
            var r = new RoundXorShift32(seed);
            var commands = new List<RoundInput>();
            while (t.Status == RoundStatus.Playing && commands.Count < 200)
            {
                List<RoundAction> moves = t.Legal();
                if (moves.Count > 0 && (r.Choice(3) > 0 || !t.Pending))
                {
                    RoundAction a = moves[r.Choice(moves.Count)];
                    commands.Add(RoundInput.Of(a, t.Now));
                    t.Commit(a);
                }
                else if (t.Pending)
                {
                    t.Advance(37_000 + r.Choice(500_000));
                }
                else
                {
                    break;
                }
            }

            return commands;
        }

        // The saved route, each action taken as soon as it is accepted (work overlaps the next commands).
        private static List<RoundInput> HastyRoute(RoundLevel level, int number)
        {
            var t = new RoundTimeline(level);
            var commands = new List<RoundInput>();
            foreach (var a in RoundTestLevels.ReferenceLevel(number).GetProperty("route").EnumerateArray())
            {
                RoundAction action = RoundTestLevels.Action(a);
                while (t.Check(action) != RoundRefusal.None && t.Pending)
                {
                    t.Advance(100_000);
                }

                commands.Add(RoundInput.Of(action, t.Now));
                Assert.That(t.Commit(action).Accepted);
            }

            return commands;
        }

        // Plays the commands, advancing by frames of real time × display speed until each command's model time.
        private static string Play(RoundLevel level, IReadOnlyList<RoundInput> commands, long frameUs, int speed, bool autoInTail)
        {
            var t = new RoundTimeline(level);
            foreach (RoundInput c in commands)
            {
                while (t.Pending && t.Now < c.AtUs)
                {
                    long step = frameUs * (autoInTail && t.Tail ? RoundProfile.TailDisplaySpeed : speed);
                    t.Advance(Math.Min(step, c.AtUs - t.Now));
                }

                Assert.That(t.Now, Is.EqualTo(c.AtUs), "the command lands at its model time");
                Assert.That(RoundReplay.Apply(t, c).Accepted, c.ToString());
            }

            while (t.Pending)
            {
                t.Advance(frameUs * (autoInTail && t.Tail ? RoundProfile.TailDisplaySpeed : speed));
            }

            return RoundEvents.Signature(t);
        }

        private static IEnumerable<int> Levels() => new[] { 5, 11, 16 };

        [TestCaseSource(nameof(Levels))]
        public void SpeedAndFrameStepsDoNotChangeTheOutcome(int number)
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(number));
            List<RoundInput> commands = RecordedCommands(level, 4242);
            Assert.That(commands.Count, Is.GreaterThan(3));
            string x1 = Play(level, commands, 16_667, 1, false);
            Assert.That(Play(level, commands, 16_667, 2, false), Is.EqualTo(x1), "×2");
            Assert.That(Play(level, commands, 16_667, 1, true), Is.EqualTo(x1), "Auto ×2 in the tail");
            Assert.That(Play(level, commands, 7_919, 2, true), Is.EqualTo(x1), "odd frames");
            Assert.That(Play(level, commands, 1_000_000_000, 1, false), Is.EqualTo(x1), "one jump per command");
        }

        [TestCaseSource(nameof(Levels))]
        public void ASnapshotInTheMiddleOfWorkResumesIdentically(int number)
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(number));
            List<RoundInput> commands = HastyRoute(level, number);
            int half = commands.Count / 2;

            var whole = new RoundTimeline(level);
            var log = new List<string>();
            foreach (RoundInput c in commands)
            {
                whole.Advance(c.AtUs - whole.Now);
                RoundReplay.Apply(whole, c);
                log.Add(RoundEvents.Signature(whole));
            }

            whole.AdvanceUntilIdle();

            // Play up to the first command from the middle on after which work is pending, then a little further.
            var first = new RoundTimeline(level);
            int k0 = 0;
            for (; k0 < commands.Count; k0++)
            {
                first.Advance(commands[k0].AtUs - first.Now);
                RoundReplay.Apply(first, commands[k0]);
                if (k0 >= half && first.Pending && k0 + 1 < commands.Count)
                {
                    break;
                }
            }

            half = k0 + 1;
            Assert.That(half, Is.LessThan(commands.Count));
            first.Advance(Math.Min(123_457, commands[half].AtUs - first.Now));
            Assert.That(first.Pending, "the snapshot is taken in the middle of work");
            RoundSnapshot snapshot = first.Snapshot();
            RoundTimeline resumed = RoundTimeline.FromSnapshot(level, snapshot);
            Assert.That(RoundEvents.Signature(resumed), Is.EqualTo(RoundEvents.Signature(first)));
            Assert.That(resumed.Couriers.Select(j => j.Id), Is.EqualTo(first.Couriers.Select(j => j.Id)));

            // The original keeps going too; neither shares state with the other.
            foreach (RoundTimeline t in new[] { resumed, first })
            {
                for (int k = half; k < commands.Count; k++)
                {
                    t.Advance(commands[k].AtUs - t.Now);
                    RoundReplay.Apply(t, commands[k]);
                    Assert.That(RoundEvents.Signature(t), Is.EqualTo(log[k]), $"after command {k}");
                }

                t.AdvanceUntilIdle();
                Assert.That(RoundEvents.Signature(t), Is.EqualTo(RoundEvents.Signature(whole)));
            }

            // Restore into an existing timeline.
            var other = new RoundTimeline(level);
            other.Restore(snapshot);
            Assert.That(RoundEvents.Signature(other), Is.EqualTo(RoundEvents.Signature(RoundTimeline.FromSnapshot(level, snapshot))));
        }

        [Test]
        public void TheSameSeedPlaysTheSameRandomGame()
        {
            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(9));
            RoundRandomGame a = RoundRandomPolicy.Play(level, 123456789);
            RoundRandomGame b = RoundRandomPolicy.Play(level, 123456789);
            Assert.That(RoundEvents.Signature(b.Timeline), Is.EqualTo(RoundEvents.Signature(a.Timeline)));
            Assert.That(b.Choices, Is.EqualTo(a.Choices));
        }

        [Test]
        public void TheXorShiftMatchesTheReference()
        {
            // rng(1) in level-stats/strategies.mjs: s = 1 → 270369, 67634689, 2647435461 …; seed 0 behaves as 1.
            var r = new RoundXorShift32(1);
            Assert.That(new[] { r.Next(), r.Next(), r.Next() }, Is.EqualTo(new uint[] { 270369, 67634689, 2647435461 }));
            Assert.That(new RoundXorShift32(0).Next(), Is.EqualTo(270369u));
            Assert.That(RoundXorShift32.FromSeed(4294967296L + 1).Next(), Is.EqualTo(270369u), "seed >>> 0");
            // floor(s / 2^32 × n) for s = 2647435461, n = 7 is 4.
            var c = new RoundXorShift32(67634689);
            Assert.That(c.Choice(7), Is.EqualTo(4));
            Assert.That(RoundRandomPolicy.SeedOf(1, 0), Is.EqualTo(7920u));
            Assert.That(RoundRandomPolicy.SeedOf(1, 399), Is.EqualTo((uint)(7919 + 399 * 104729 + 1)));
        }
    }
}
