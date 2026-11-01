using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// Hidden cells (rules 17 §8 «Скрытые клетки», the owner, 2026-11-01; <c>tools/rounds/README.md</c> "Hidden cells"):
    /// the C# port against the reference engine on the fixture's <c>cells</c> cases (written by
    /// <c>Fixtures/export-reference.cjs</c>: the small level of <c>tools/rounds/test/cells.test.mjs</c> and placed levels
    /// L5, L13 and L16 given hidden cells), plus the rules the fixture cannot show: the start reveal, a restored snapshot,
    /// identity of the targeting, the validator's refusals and Bloom Burst.
    /// </summary>
    public class RoundHiddenCellsTests
    {
        private static IEnumerable<string> Cases() => RoundTestLevels.ReferenceCellCases.Select(c => c.GetProperty("name").GetString()!).ToArray();

        private static RoundLevel LevelOf(JsonElement c) => RoundTestLevels.FromDefinition(1, c.GetProperty("definition"));

        [Test]
        public void TheFixtureHoldsTheSmallLevelAndThreePlacedLevels()
        {
            Assert.That(Cases(), Is.EqualTo(new[] { "small", "L0005+cells", "L0013+cells", "L0016+cells" }));
        }

        [TestCaseSource(nameof(Cases))]
        public void TheDefinitionIsValidAndTheStartRevealMatches(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase(name);
            RoundLevel level = LevelOf(c);
            Assert.That(RoundLevelValidator.ValidateDefinition(level), Is.Empty);
            Assert.That(level.Mechanics, Does.Contain("cells"));
            RoundState s = RoundRules.Create(level);
            int[] veiled = Enumerable.Range(0, s.Board.Length).Where(i => !s.Known(i)).ToArray();
            Assert.That(veiled, Is.EqualTo(c.GetProperty("veiledAtStart").EnumerateArray().Select(x => x.GetInt32()).ToArray()));
            Assert.That(s.VeiledLeft, Is.EqualTo(veiled.Length));
            AssertNoVeiledTarget(s);
        }

        [TestCaseSource(nameof(Cases))]
        public void TheSavedSolutionRevealsInTheReferenceOrder(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase(name);
            List<string> lines = RoundReferenceTests.CheckSolution(c, LevelOf(c), name);
            Assert.That(lines.Where(l => l.Contains(" cellReveal ", StringComparison.Ordinal)),
                Is.EqualTo(c.GetProperty("reveals").EnumerateArray().Select(x => x.GetString()!)));
            if (c.TryGetProperty("lines", out JsonElement all))
            {
                Assert.That(lines, Is.EqualTo(all.EnumerateArray().Select(x => x.GetString()!)), "the whole canonical log");
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void TheSavedRouteOnARunningClockEndsTheSame(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase(name);
            RoundReferenceTests.CheckTimed(c, LevelOf(c), name);
        }

        [TestCaseSource(nameof(Cases))]
        public void RandomGamesEndTheSame(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase(name);
            RoundReferenceTests.CheckRandom(c, LevelOf(c), name);
        }

        [TestCaseSource(nameof(Cases))]
        public void ThePatientSolverAgrees(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase(name);
            RoundReferenceTests.CheckSolver(c, LevelOf(c));
        }

        [Test]
        public void EachRevealFollowsItsClearOrTheBridgeAtTheSameTimeInIncreasingCellOrder()
        {
            RoundLevel level = LevelOf(RoundTestLevels.ReferenceCellCase("small"));
            var t = new RoundTimeline(level);
            int seen = 0;
            void Check(IReadOnlyList<RoundEvent> events)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].Type != RoundEventType.CellReveal)
                    {
                        continue;
                    }

                    seen++;
                    int k = i - 1;
                    while (events[k].Type == RoundEventType.CellReveal)
                    {
                        Assert.That(events[k].Cell, Is.LessThan(events[i].Cell));
                        k--;
                    }

                    Assert.That(events[k].Type, Is.AnyOf(RoundEventType.Clear, RoundEventType.Bridge));
                    Assert.That(events[i].At, Is.EqualTo(events[k].At));
                    Assert.That(t.State.Known(events[i].Cell), Is.True);
                }

                AssertNoVeiledTarget(t.State);
            }

            Check(t.Commit(RoundAction.Tap(0)).Events);
            while (t.Pending)
            {
                t.Advance(100_000);
                Check(t.LastEvents);
                if (t.Pending)
                {
                    // a restored snapshot recomputes the veiled cells from the board
                    RoundTimeline copy = t.Copy();
                    Assert.That(copy.State.Veiled, Is.EqualTo(t.State.Veiled));
                    Assert.That(copy.State.VeiledLeft, Is.EqualTo(t.State.VeiledLeft));
                }
            }

            RoundCommitResult bridge = t.Commit(RoundAction.BuildBridge(0));
            Assert.That(bridge.Accepted, Is.True);
            Check(bridge.Events);
            Assert.That(bridge.Events.Count(e => e.Type == RoundEventType.CellReveal), Is.EqualTo(1), "the bridge reveals the far cell beside its site");
            t.Commit(RoundAction.Tap(1));
            while (t.Pending)
            {
                t.Advance(100_000);
                Check(t.LastEvents);
            }

            Assert.That(t.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(seen, Is.EqualTo(4));
            Assert.That(t.State.VeiledLeft, Is.EqualTo(0));
            Assert.That(RoundEvents.Line(RoundEvent.CellRevealed(2500, 7)), Is.EqualTo("2500 cellReveal cell=7"));
        }

        [Test]
        public void HiddenCellsChangeNoEventButTheirRevealsAndNoSignature()
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase("L0005+cells");
            RoundLevel with = LevelOf(c);
            RoundLevel without = RoundTestLevels.Content(5);
            Assert.That(RoundRules.Create(without).Veiled, Is.Null);
            RoundAction[] route = c.GetProperty("route").EnumerateArray().Select(RoundTestLevels.Action).ToArray();
            List<string> Play(RoundLevel level)
            {
                var t = new RoundTimeline(level);
                var log = new List<string>();
                foreach (RoundAction a in route)
                {
                    while (t.Pending)
                    {
                        t.AdvanceUntilIdle();
                        log.AddRange(t.LastEvents.Select(RoundEvents.Line));
                    }

                    log.AddRange(t.Commit(a).Events.Select(RoundEvents.Line));
                    log.Add(RoundEvents.Signature(t));
                }

                t.AdvanceUntilIdle();
                log.AddRange(t.LastEvents.Select(RoundEvents.Line));
                log.Add(RoundEvents.Signature(t));
                return log;
            }

            List<string> a = Play(without);
            List<string> b = Play(with);
            Assert.That(b.Count(l => l.Contains(" cellReveal ", StringComparison.Ordinal)), Is.GreaterThan(0));
            Assert.That(b.Where(l => !l.Contains(" cellReveal ", StringComparison.Ordinal)), Is.EqualTo(a));
        }

        [Test]
        public void BloomBurstClearsVeiledCellsUnseenAndRevealsWhatItOpens()
        {
            RoundLevel level = LevelOf(RoundTestLevels.ReferenceCellCase("small"));
            var t = new RoundTimeline(level);
            Assert.That(t.State.VeiledLeft, Is.EqualTo(4));
            RoundCommitResult burst = RoundBoosters.BloomBurst(t, 'G');
            Assert.That(burst.Accepted, Is.True, burst.Reason);
            // the deep G cell cleared unseen; the far A cells stay behind the wall until the bridge
            Assert.That(t.State.VeiledLeft, Is.EqualTo(3));
            Assert.That(burst.Events.Count(e => e.Type == RoundEventType.CellReveal), Is.EqualTo(0));
            t.AdvanceUntilIdle();
            RoundCommitResult bridge = t.Commit(RoundAction.BuildBridge(0));
            Assert.That(bridge.Events.Count(e => e.Type == RoundEventType.CellReveal), Is.EqualTo(1));
            RoundCommitResult second = RoundBoosters.BloomBurst(t, 'A');
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(t.State.VeiledLeft, Is.EqualTo(0));
            AssertNoVeiledTarget(t.State);
        }

        [Test]
        public void TheValidatorRefusesWhatTheReferenceRefuses()
        {
            JsonElement c = RoundTestLevels.ReferenceCellCase("small");
            RoundLevel ok = LevelOf(c);
            int[] cells = ok.HiddenCells.ToArray();
            RoundLevel With(int[] hidden, IReadOnlyList<RoundPod>? pods = null, IReadOnlyList<string>? mechanics = null) =>
                new RoundLevel(1, ok.Width, ok.Height, ok.RowsBottomFirst, pods ?? ok.Pods, ok.Tray, ok.Entries, ok.Bridge,
                    mechanics: mechanics ?? ok.Mechanics, hiddenCells: hidden);

            Assert.That(RoundLevelValidator.ValidateDefinition(ok), Is.Empty);
            Assert.That(RoundLevelValidator.ValidateDefinition(With(cells.Reverse().ToArray())), Does.Contain(RoundLevelValidator.HiddenCells), "unsorted");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new[] { cells[0], cells[0] })), Does.Contain(RoundLevelValidator.HiddenCells), "duplicate");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new[] { ok.CellCount })), Does.Contain(RoundLevelValidator.HiddenCells), "off the board");
            Assert.That(RoundLevelValidator.ValidateDefinition(With(new[] { 0 })), Does.Contain(RoundLevelValidator.HiddenCellMaterial), "a wall");
            var hiddenPods = new[] { ok.Pods[0], new RoundPod(ok.Pods[1].Code, ok.Pods[1].Count, RoundMystery.Depth) };
            Assert.That(RoundLevelValidator.ValidateDefinition(With(cells, hiddenPods, new[] { "bridge", "cells", "hidden" })),
                Does.Contain(RoundLevelValidator.HiddenCellsWithPods));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(cells, mechanics: new[] { "bridge" })), Does.Contain(RoundLevelValidator.Mechanics));
            Assert.That(RoundLevelValidator.ValidateDefinition(With(Array.Empty<int>())), Does.Contain(RoundLevelValidator.Mechanics), "cells listed without hidden cells");
        }

        [Test]
        public void HiddenCellsAreIntroducedOnLevels51And53()
        {
            Assert.That(RoundLevelValidator.IntroductionTable, Does.Contain(("cells", 51, 53)));
            Assert.That(RoundLevelValidator.IntroductionTable, Does.Contain(("latch", 61, 63)), "latch + key follows on 61 and 63 (17 §9 as amended on 2026-11-01)");
        }

        private static void AssertNoVeiledTarget(RoundState s)
        {
            var targets = new HashSet<int>(s.Reach().Targets);
            for (int i = 0; i < s.Board.Length; i++)
            {
                if (!s.Known(i))
                {
                    Assert.That(s.Board[i], Is.GreaterThanOrEqualTo(0), $"veiled {i} is a material cell");
                    Assert.That(targets.Contains(i), Is.False, $"veiled {i} is no target");
                }
            }
        }
    }
}
