using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// The Drop water channel and the Twig wooden shutters (rules 17 §8, the owner, 2026-11-05; spec 006 FR-069, FR-070;
    /// <c>tools/rounds/README.md</c> «Channel», «Shutters»): the C# port against the reference engine on the fixture's
    /// <c>channels</c> and <c>shutters</c> cases (written by <c>Fixtures/export-reference.cjs</c>: the small levels of
    /// <c>tools/rounds/test/channel-shutters.test.mjs</c> and placed levels given a channel or a shutter by a fixed rule),
    /// plus what the fixture cannot show: readiness, the choice and the new entry, the ride, the unchosen exit, the shutters'
    /// blocking and opening time, a restored snapshot, the validator's refusals, the legal actions, the replay commands and
    /// Bloom Burst.
    /// </summary>
    public class RoundChannelShutterTests
    {
        private static IEnumerable<string> ChannelCases() => RoundTestLevels.ReferenceChannelCases.Select(c => c.GetProperty("name").GetString()!).ToArray();

        private static IEnumerable<string> ShutterCases() => RoundTestLevels.ReferenceShutterCases.Select(c => c.GetProperty("name").GetString()!).ToArray();

        private static JsonElement Case(string kind, string name) =>
            kind == "channel" ? RoundTestLevels.ReferenceChannelCase(name) : RoundTestLevels.ReferenceShutterCase(name);

        private static RoundLevel LevelOf(JsonElement c) => RoundTestLevels.FromDefinition(1, c.GetProperty("definition"));

        private static RoundLevel Channel(string name = "small") => LevelOf(RoundTestLevels.ReferenceChannelCase(name));

        private static RoundLevel Shutters(string name = "small") => LevelOf(RoundTestLevels.ReferenceShutterCase(name));

        private static IEnumerable<TestCaseData> AllCases() =>
            ChannelCases().Select(n => new TestCaseData("channel", n).SetName("Channel_" + n))
                .Concat(ShutterCases().Select(n => new TestCaseData("shutter", n).SetName("Shutter_" + n)));

        [Test]
        public void TheFixtureHoldsTheSmallLevelsAndThreePlacedLevelsEach()
        {
            Assert.That(ChannelCases(), Is.EqualTo(new[] { "small", "small-exit1", "need", "both", "L0014+channel", "L0017+channel", "L0029+channel" }));
            Assert.That(ShutterCases(), Is.EqualTo(new[] { "small", "small-early", "L0005+shutter", "L0013+shutter", "L0028+shutter" }));
        }

        [TestCaseSource(nameof(AllCases))]
        public void TheDefinitionIsValid(string kind, string name)
        {
            RoundLevel level = LevelOf(Case(kind, name));
            Assert.That(RoundLevelValidator.ValidateDefinition(level), Is.Empty);
            RoundState s = RoundRules.Create(level);
            if (level.Channel != null)
            {
                Assert.That(s.Channel, Is.EqualTo(-1));
                Assert.That(level.Mechanics, Does.Contain("canal"));
            }

            if (level.Shutters.Count > 0)
            {
                Assert.That(s.Shut, Is.All.True, "every shutter starts closed");
                Assert.That(level.Mechanics, Does.Contain("shutters"));
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void TheSolutionMatchesTheReferenceLog(string kind, string name)
        {
            JsonElement c = Case(kind, name);
            List<string> lines = RoundReferenceTests.CheckSolution(c, LevelOf(c), name);
            var marks = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].EndsWith(" channelReady", StringComparison.Ordinal) || lines[i].Contains(" channelOpen ", StringComparison.Ordinal)
                    || lines[i].Contains(" shutterOpen ", StringComparison.Ordinal))
                {
                    marks.Add(i > 0 ? lines[i - 1] : string.Empty);
                    marks.Add(lines[i]);
                }
            }

            Assert.That(marks, Is.EqualTo(c.GetProperty("marks").EnumerateArray().Select(x => x.GetString()!)));
            if (c.TryGetProperty("lines", out JsonElement all))
            {
                Assert.That(lines, Is.EqualTo(all.EnumerateArray().Select(x => x.GetString()!)), "the whole canonical log");
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void TheRouteOnARunningClockEndsTheSame(string kind, string name)
        {
            JsonElement c = Case(kind, name);
            RoundReferenceTests.CheckTimed(c, LevelOf(c), name);
        }

        [TestCaseSource(nameof(AllCases))]
        public void RandomGamesEndTheSame(string kind, string name)
        {
            JsonElement c = Case(kind, name);
            RoundReferenceTests.CheckRandom(c, LevelOf(c), name);
        }

        [TestCaseSource(nameof(AllCases))]
        public void ThePatientSolverAgrees(string kind, string name)
        {
            JsonElement c = Case(kind, name);
            RoundReferenceTests.CheckSolver(c, LevelOf(c));
        }

        [Test]
        public void TheChannelIsNotChoosableBeforeTheSpringIsReached()
        {
            RoundLevel level = Channel();
            RoundState s = RoundRules.Create(level);
            Assert.That(RoundRules.ChannelChoices(s), Is.Empty);
            Assert.That(RoundRules.Legal(s).Any(a => a.Type == RoundActionType.Channel), Is.False);
            Assert.That(RoundRules.Check(s, RoundAction.ChooseExit(0)), Is.EqualTo(RoundRefusal.ChannelNotReady));
            Assert.That(RoundRules.ReasonText(RoundRefusal.ChannelNotReady), Is.EqualTo("channel-not-ready"));
            Assert.That(s.SpringReached, Is.False);

            // the clear that reaches the spring readies the channel at its time, after its cell reveals and latches
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            int seen = 0;
            while (t.Pending)
            {
                t.Advance(50_000);
                IReadOnlyList<RoundEvent> ev = t.LastEvents;
                for (int i = 0; i < ev.Count; i++)
                {
                    if (ev[i].Type != RoundEventType.ChannelReady)
                    {
                        continue;
                    }

                    seen++;
                    int k = i - 1;
                    while (ev[k].Type == RoundEventType.CellReveal || ev[k].Type == RoundEventType.LatchOpen)
                    {
                        k--;
                    }

                    Assert.That(ev[k].Type, Is.EqualTo(RoundEventType.Clear));
                    Assert.That(ev[i].At, Is.EqualTo(ev[k].At));
                    Assert.That(RoundLevelValidator.Orthogonal(ev[k].Cell, level.Channel!.Spring, level.Width), Is.True, "the cleared cell is beside the spring");
                    Assert.That(RoundEvents.Line(ev[i]), Is.EqualTo(ev[i].At + " channelReady"));
                }
            }

            Assert.That(seen, Is.EqualTo(1));
            Assert.That(RoundRules.Legal(t.State).Where(a => a.Type == RoundActionType.Channel), Is.EqualTo(new[] { RoundAction.ChooseExit(0), RoundAction.ChooseExit(1) }));

            // nothing to tap but the choice: the attempt stays Playing (17 §1: a choice can still change the state)
            RoundTimeline jam = new RoundTimeline(level);
            jam.Commit(RoundAction.Tap(0));
            jam.AdvanceUntilIdle();
            Assert.That(jam.Status, Is.EqualTo(RoundStatus.Playing));
            Assert.That(jam.Tail, Is.False, "the tail waits for the choice");
        }

        [Test]
        public void TheChosenExitBecomesAnEntryAndTheOtherNever()
        {
            RoundLevel level = Channel();
            RoundChannel ch = level.Channel!;
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            int free = t.FreeSlots;
            RoundCommitResult r = t.Commit(RoundAction.ChooseExit(0));
            Assert.That(r.Accepted, Is.True);
            Assert.That(r.Events[0].Type, Is.EqualTo(RoundEventType.ChannelOpen));
            Assert.That((r.Events[0].Exit, r.Events[0].Cell, r.Events[0].At), Is.EqualTo((0, ch.Exits[0].Cell, t.Now)));
            Assert.That(RoundEvents.Line(r.Events[0]), Is.EqualTo(t.Now + " channelOpen exit=0"));
            Assert.That(t.FreeSlots, Is.EqualTo(free), "no place taken");
            Assert.That(t.State.Channel, Is.EqualTo(0));
            Assert.That(t.Check(RoundAction.ChooseExit(1)), Is.EqualTo(RoundRefusal.ChannelNotReady), "the choice is final");
            Assert.That(t.State.Reach().Distance[ch.Exits[0].Cell], Is.EqualTo(1), "the chosen exit is a source");
            Assert.That(t.State.Reach().Distance[ch.Exits[1].Cell], Is.LessThan(0), "the unchosen pocket stays closed");
            Assert.That(t.State.Signature().Split('|')[^2], Is.EqualTo("0"), "the signature carries the choice");

            t.Commit(RoundAction.Tap(1));
            t.Commit(RoundAction.Tap(2));
            var starts = new HashSet<int>();
            while (t.Pending)
            {
                t.Advance(100_000);
                foreach (RoundEvent e in t.LastEvents)
                {
                    if (e.Type == RoundEventType.Depart)
                    {
                        starts.Add(e.Route![0]);
                    }
                }
            }

            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(starts, Does.Contain(ch.Exits[0].Cell));
            Assert.That(starts, Does.Not.Contain(ch.Exits[1].Cell));
            Assert.That(starts.All(c => c == ch.Exits[0].Cell || level.Entries.Contains(c)), Is.True);
        }

        [Test]
        public void AWalkerFromTheExitRidesOutAndBack()
        {
            RoundLevel level = Channel();
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            t.Commit(RoundAction.ChooseExit(0));
            RoundCommitResult r = t.Commit(RoundAction.Tap(1));
            RoundEvent depart = r.Events.First(e => e.Type == RoundEventType.Depart);
            RoundCourier j = t.Couriers.First(c => c.Id == depart.Id);
            Assert.That(j.Route[0], Is.EqualTo(level.Channel!.Exits[0].Cell));
            Assert.That(j.RideUs, Is.EqualTo(600_000));
            Assert.That(depart.RideUs, Is.EqualTo(600_000));
            long distance = j.Route.Count - 1;
            Assert.That(j.Arrive, Is.EqualTo(j.Start + 600_000 + Math.Max(650_000, distance * 100_000)));
            Assert.That(j.Done, Is.EqualTo(j.Clear + Math.Max(400_000, distance * 75_000) + 600_000));
            RoundCourierPose pose = t.PoseOf(j, t.Now);
            Assert.That(pose.Riding, Is.True);
            Assert.That(((int)pose.X, (int)pose.Y), Is.EqualTo((j.Route[0] % level.Width, j.Route[0] / level.Width)));
            Assert.That(t.PoseOf(j, j.Start + 650_000).Riding, Is.False, "out of the water after the ride");
            Assert.That(t.PoseOf(j, j.Done - 100_000).Riding, Is.True, "in the water before it is home");

            var plain = new RoundTimeline(level);
            plain.Commit(RoundAction.Tap(0));
            Assert.That(plain.Couriers[0].RideUs, Is.EqualTo(0));
            Assert.That(plain.CourierPoses()[0].Riding, Is.False);
        }

        [Test]
        public void ARestoredSnapshotKeepsTheChoiceAndRecomputesTheRest()
        {
            RoundLevel level = LevelOf(RoundTestLevels.ReferenceChannelCase("both"));
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            t.Commit(RoundAction.ChooseExit(1));
            t.Commit(RoundAction.Tap(1));
            t.Commit(RoundAction.Tap(2));
            while (t.Pending)
            {
                t.Advance(300_000);
                RoundTimeline copy = t.Copy();
                Assert.That(copy.State.Channel, Is.EqualTo(1));
                Assert.That(copy.State.SpringReached, Is.True);
                Assert.That(copy.State.Shut, Is.EqualTo(t.State.Shut));
                Assert.That(RoundEvents.Signature(copy), Is.EqualTo(RoundEvents.Signature(t)));
                Assert.That(copy.State.Reach().Targets, Is.EqualTo(t.State.Reach().Targets));
            }

            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));

            // a level without a channel: no choice in its signature
            RoundState plain = RoundRules.Create(RoundTestLevels.Content(5));
            Assert.That(plain.Signature().Split('|').Length, Is.EqualTo(8));
            Assert.That(plain.Shut, Is.Null);
        }

        [Test]
        public void ShuttersBlockTargetsAndOpenAtTheExactTimeOfTheClear()
        {
            RoundLevel level = Shutters();
            RoundShutter shutter = level.Shutters[0];
            var t = new RoundTimeline(level);
            Assert.That(t.State.ShutterLeft(0), Is.EqualTo(5));
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            foreach (int cell in shutter.Cells)
            {
                Assert.That(t.State.Reach().Targets, Does.Not.Contain(cell), "under the closed shutter: no target");
                Assert.That(t.State.ShutAt(cell), Is.True);
            }

            RoundCommitResult g = t.Commit(RoundAction.Tap(2));
            Assert.That(g.Events.Any(e => e.Type == RoundEventType.Depart), Is.False, "its work waits");
            t.Commit(RoundAction.Tap(1));
            var events = new List<RoundEvent>();
            var left = new List<int>();
            while (t.Pending)
            {
                t.Advance(50_000);
                foreach (RoundEvent e in t.LastEvents)
                {
                    events.Add(e);
                    if (e.Type == RoundEventType.Clear && t.State.Shut![0])
                    {
                        left.Add(t.State.ShutterLeft(0));
                    }
                }
            }

            int k = events.FindIndex(e => e.Type == RoundEventType.ShutterOpen);
            Assert.That(events.Take(k).Count(e => e.Type == RoundEventType.Clear), Is.EqualTo(5), "the fifth B clear opens it");
            Assert.That(events[k - 1].Type, Is.EqualTo(RoundEventType.Clear));
            Assert.That(events[k].At, Is.EqualTo(events[k - 1].At));
            Assert.That(RoundEvents.Line(events[k]), Is.EqualTo(events[k].At + " shutterOpen shutter=0"));
            RoundEvent firstG = events.First(e => e.Type == RoundEventType.Depart && e.Pod == 2);
            Assert.That(firstG.At, Is.EqualTo(events[k].At), "the waiting pod leaves at that moment");
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(t.State.Shut, Is.EqualTo(new[] { false }));
            Assert.That(t.State.ShutterLeft(0), Is.EqualTo(0));
        }

        [Test]
        public void BloomBurstLeavesTheCellsUnderAClosedShutterButItsWorkCounts()
        {
            RoundLevel level = Shutters();
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(0));
            t.AdvanceUntilIdle();
            t.Commit(RoundAction.Tap(2));
            Assert.That(RoundBoosters.CanBloomBurst(t, 'G'), Is.EqualTo(RoundRefusal.NoFreeCells), "every G cell is under the closed shutter");
            t.Commit(RoundAction.Tap(1));
            RoundCommitResult burst = RoundBoosters.BloomBurst(t, 'B');
            Assert.That(burst.Accepted, Is.True);
            int k = burst.Events.ToList().FindIndex(e => e.Type == RoundEventType.ShutterOpen);
            Assert.That(k, Is.GreaterThan(0), "the burst's B cells are work done: the shutter opens right after it");
            Assert.That(burst.Events[k - 1].Type, Is.EqualTo(RoundEventType.BurstClear));
            Assert.That(RoundBoosters.CanBloomBurst(t, 'G'), Is.EqualTo(RoundRefusal.None), "now its cells are work");
            t.AdvanceUntilIdle();
            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));
        }

        [Test]
        public void TheRandomPolicyAndTheSolverSeeTheChoice()
        {
            RoundLevel need = Channel("need");
            RoundSolveResult r = new RoundSolver(100000).Route(need);
            Assert.That(r.Win, Is.True);
            Assert.That(r.Route!, Does.Contain(RoundAction.ChooseExit(0)));

            RoundState s = RoundSolver.Step(RoundRules.Create(need), RoundAction.Tap(0))!;
            Assert.That(RoundRules.Legal(s).Where(a => a.Type == RoundActionType.Channel), Is.EqualTo(new[] { RoundAction.ChooseExit(0), RoundAction.ChooseExit(1) }));
            RoundState wrong = RoundSolver.Step(RoundSolver.Step(s, RoundAction.ChooseExit(1))!, RoundAction.Tap(1))!;
            Assert.That(wrong.Status, Is.EqualTo(RoundStatus.Stuck), "with exit 1 the walled area is never reached");

            int choices = 0;
            for (int g = 0; g < 20; g++)
            {
                RoundRandomGame game = RoundRandomPolicy.Play(need, RoundRandomPolicy.SeedOf(1, g), checkInvariants: true);
                choices += game.Timeline.State.Channel >= 0 ? 1 : 0;
            }

            Assert.That(choices, Is.EqualTo(20), "every game chooses an exit (the only way on once the B pod is done)");
        }

        [Test]
        public void ADecidingChannelsClassTest_ChoosesOnlyItsWinningExit()
        {
            // rules 17 §5, the owner, 2026-11-05: a Super Hard where exactly one exit wins is measured with that exit only
            // (reference measure.mjs randomTest {exit}, perf/games.mjs keepExit); "need" is won with exit 0 and stuck with 1
            RoundLevel need = Channel("need");
            Assert.Multiple(() =>
            {
                Assert.That(RoundRandomPolicy.KeepExit(null), Is.Null);
                Assert.That(RoundRandomPolicy.Keep(null, null), Is.Null, "no narrowing: the uniform policy of Normal and Hard");
                Func<RoundAction, bool> keep = RoundRandomPolicy.KeepExit(0)!;
                Assert.That(keep(RoundAction.ChooseExit(0)), Is.True);
                Assert.That(keep(RoundAction.ChooseExit(1)), Is.False);
                Assert.That(keep(RoundAction.Tap(1)), Is.True, "every pod action stays");
                Assert.That(keep(RoundAction.BuildBridge(1)), Is.True, "every bridge action stays");
                Func<RoundAction, bool> both = RoundRandomPolicy.Keep(1, 0)!;
                Assert.That(both(RoundAction.BuildBridge(0)), Is.False);
                Assert.That(both(RoundAction.BuildBridge(1)), Is.True);
                Assert.That(both(RoundAction.ChooseExit(1)), Is.False);
                Assert.That(both(RoundAction.ChooseExit(0)), Is.True);
            });

            for (int g = 0; g < 20; g++)
            {
                uint seed = RoundRandomPolicy.SeedOf(1, g);
                RoundRandomGame kept = RoundRandomPolicy.Play(need, seed, RoundRandomPolicy.KeepExit(1), checkInvariants: true);
                Assert.That(kept.Timeline.State.Channel, Is.EqualTo(-1).Or.EqualTo(1), $"game {g}: only exit 1 may be chosen");
                Assert.That(kept.Won, Is.False, $"game {g}: exit 1 never wins");
            }

            RoundRandomRun all = RoundRandomPolicy.Test(need);
            RoundRandomRun winning = RoundRandomPolicy.Test(need, exit: 0);
            RoundRandomRun losing = RoundRandomPolicy.Test(need, exit: 1);
            Assert.Multiple(() =>
            {
                Assert.That(winning.Runs, Is.EqualTo(100));
                Assert.That(winning.Wins, Is.EqualTo(100), "with the winning exit every game wins (the reference's 100 %)");
                Assert.That(winning.Games.All(x => x.Timeline.State.Channel == 0), Is.True);
                Assert.That(losing.Wins, Is.Zero);
                Assert.That(all.Runs, Is.EqualTo(400), "uniform over both exits the share crosses 50 %: widened (the reference's 52.3 % of 400)");
                Assert.That(all.Wins, Is.EqualTo(209));
            });
        }

        [Test]
        public void TheChannelCommandReplaysAndParses()
        {
            RoundInput input = RoundInput.Parse("channel exit=1 at=1500");
            Assert.That((input.Type, input.Value, input.AtUs), Is.EqualTo((RoundInputType.Channel, 1, 1500L)));
            Assert.That(input.ToString(), Is.EqualTo("channel exit=1 at=1500"));
            Assert.That(RoundInput.Of(RoundAction.ChooseExit(0), 7).ToString(), Is.EqualTo("channel exit=0 at=7"));
            Assert.That(RoundAction.ChooseExit(1).ToString(), Is.EqualTo("channel exit=1"));

            RoundLevel level = Channel();
            var t = new RoundTimeline(level);
            var replay = new RoundReplay(t);
            replay.Execute(RoundInput.Tap(0, 0));
            Assert.That(replay.Execute(RoundInput.Channel(1, 0), waitUntilAccepted: true).Accepted, Is.True, "waits until the spring is reached");
            Assert.That(t.State.Channel, Is.EqualTo(1));
        }

        [Test]
        public void TheValidatorRefusesBadChannels()
        {
            RoundLevel ok = Channel();
            RoundChannel ch = ok.Channel!;
            RoundLevel With(RoundChannel c) => new RoundLevel(ok.Number, ok.Width, ok.Height, ok.RowsBottomFirst, ok.Pods, ok.Tray, ok.Entries,
                mechanics: ok.Mechanics, channel: c);
            RoundChannelExit Exit(RoundChannelExit e, int? cell = null, IReadOnlyList<int>? route = null, long? ride = null) =>
                new RoundChannelExit(cell ?? e.Cell, route ?? e.Route, ride ?? e.RideUs);

            Assert.That(RoundLevelValidator.ValidateDefinition(ok), Is.Empty);
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundChannel(ok.Entries[0], ch.Exits))), Does.Contain(RoundLevelValidator.Channel));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundChannel(ch.Spring, new[] { ch.Exits[0] }))), Does.Contain(RoundLevelValidator.Channel));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundChannel(ch.Spring, new[] { ch.Exits[0], ch.Exits[0] }))), Does.Contain(RoundLevelValidator.ChannelExit));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundChannel(ch.Spring, new[] { Exit(ch.Exits[0], route: new[] { ch.Spring, ch.Exits[0].Cell }), ch.Exits[1] }))),
                Does.Contain(RoundLevelValidator.ChannelRoute));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundChannel(ch.Spring, new[] { Exit(ch.Exits[0], ride: 1500), ch.Exits[1] }))),
                Does.Contain(RoundLevelValidator.ChannelRide));
            RoundLevel noMechanic = new RoundLevel(ok.Number, ok.Width, ok.Height, ok.RowsBottomFirst, ok.Pods, ok.Tray, ok.Entries, channel: ch);
            Assert.That(RoundLevelValidator.ValidateDefinition(noMechanic), Does.Contain(RoundLevelValidator.Mechanics));

            // a spring walled in can never be reached
            string[] rows = ok.RowsBottomFirst.ToArray();
            int w = ok.Width, y = ch.Spring / w, x = ch.Spring % w;
            string Wall(string r, int i) => r.Substring(0, i) + "#" + r.Substring(i + 1);
            rows[y - 1] = Wall(rows[y - 1], x);
            rows[y + 1] = Wall(rows[y + 1], x);
            rows[y] = Wall(Wall(rows[y], x - 1), x + 1);
            var pods = new[] { new RoundPod('B', 22), new RoundPod('G', 14), new RoundPod('R', 11) };
            var walled = new RoundLevel(ok.Number, ok.Width, ok.Height, rows, pods, ok.Tray, ok.Entries, mechanics: ok.Mechanics, channel: ch);
            Assert.That(RoundLevelValidator.ValidateDefinition(walled), Is.EqualTo(new[] { RoundLevelValidator.ChannelSpringUnreachable }));
        }

        [Test]
        public void TheValidatorRefusesBadShutters()
        {
            RoundLevel ok = Shutters();
            RoundShutter sh = ok.Shutters[0];
            RoundLevel With(params RoundShutter[] s) => new RoundLevel(ok.Number, ok.Width, ok.Height, ok.RowsBottomFirst, ok.Pods, ok.Tray, ok.Entries,
                mechanics: ok.Mechanics, shutters: s);
            Assert.That(RoundLevelValidator.ValidateDefinition(ok), Is.Empty);
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(sh.Cells, 'B', 8))), Is.EqualTo(new[] { RoundLevelValidator.ShutterCountUnreachable }), "more than the B outside");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(sh.Cells, 'G', 1))), Is.EqualTo(new[] { RoundLevelValidator.ShutterCountUnreachable }), "never its own cells only");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(sh.Cells.Skip(1).ToArray(), 'B', 1))), Does.Contain(RoundLevelValidator.Shutter), "a full rectangle");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(sh.Cells.Reverse().ToArray(), 'B', 1))), Does.Contain(RoundLevelValidator.Shutter), "sorted");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(sh.Cells, 'B', 0))), Does.Contain(RoundLevelValidator.Shutter));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(sh.Cells, 'q', 1))), Does.Contain(RoundLevelValidator.Shutter));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(sh, new RoundShutter(new[] { sh.Cells[0] }, 'B', 1))), Does.Contain(RoundLevelValidator.Shutter), "never over another shutter");
            int open = ok.RowsBottomFirst[0].IndexOf('.');
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new RoundShutter(new[] { open }, 'B', 1))), Does.Contain(RoundLevelValidator.Shutter), "material cells only");
            RoundLevel noMechanic = new RoundLevel(ok.Number, ok.Width, ok.Height, ok.RowsBottomFirst, ok.Pods, ok.Tray, ok.Entries, shutters: ok.Shutters);
            Assert.That(RoundLevelValidator.ValidateDefinition(noMechanic), Does.Contain(RoundLevelValidator.Mechanics));
        }

        [Test]
        public void TheIntroductionsOfTheNewMechanicsFollowTheCalendar()
        {
            Assert.That(RoundLevelValidator.IntroductionTable, Does.Contain(("canal", 101, 103)));
            Assert.That(RoundLevelValidator.IntroductionTable, Does.Contain(("shutters", 121, 123)));
        }
    }
}
