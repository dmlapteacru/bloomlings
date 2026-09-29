using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Solver.Tests
{
    /// <summary>T068: solvable and unsolvable fixtures, jam witnesses, budgets and determinism.</summary>
    public class SolverTests
    {
        private static readonly Solver Solver = new Solver();

        /// <summary>
        /// Row 0 is a Leaf wall around a Water entry tile; behind it Flower, Wood, Moss and Dew wait. Committing four
        /// waiting pods plus the Leaf pod before Water jams; Water first wins.
        /// </summary>
        private static LevelSession JamProne() => Session(
            new[] { "#######", "#######", "#######", "#######", "#######", "#######", "fomdlll", "#llwll#" },
            new[]
            {
                Pod("f", VariantId.Flower, 1), Pod("o", VariantId.Wood, 1), Pod("m", VariantId.Moss, 1),
                Pod("d", VariantId.Dew, 1), Pod("l2", VariantId.Leaf, 7), Pod("w", VariantId.Water, 1),
            },
            new[] { new[] { "f", "w" }, new[] { "o" }, new[] { "m" }, new[] { "d" }, new[] { "l2" } });

        [Test]
        public void SolvableFixture_GivesAReplayableWinningTrace()
        {
            LevelSession level = JamProne();

            SolveResult result = Solver.Solve(level, SolveOptions.Default);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solvable));
            LevelSession replay = level.Clone();
            foreach (Command command in result.Trace)
            {
                Assert.That(replay.Apply(command).Accepted, Is.True);
            }

            Assert.That(replay.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void EnclosedTile_IsUnsolvable()
        {
            // The Flower tile in the middle is walled in by stones: no route can ever reach it.
            LevelSession level = Session(
                new[] { "#######", "#######", "#######", "##fff##", "##f#f##", "##fff##", "#######", "wwwwwww" },
                new[] { Pod("w", VariantId.Water, 7), Pod("f", VariantId.Flower, 8) });

            SolveResult result = Solver.Solve(level, SolveOptions.Default);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(result.Trace, Is.Empty);
        }

        [Test]
        public void JamWitness_ExistsForALosableLevel()
        {
            LevelSession level = JamProne();

            SolveResult jam = Solver.FindJam(level, SolveOptions.Default);

            Assert.That(jam.Status, Is.EqualTo(SolveStatus.Solvable));
            LevelSession replay = level.Clone();
            foreach (Command command in jam.Trace)
            {
                replay.Apply(command);
            }

            Assert.That(replay.Status, Is.AnyOf(LevelStatus.Jammed, LevelStatus.Stuck));
        }

        [Test]
        public void ForgivingLevel_HasNoJamWitness()
        {
            LevelSession level = Session(
                new[] { "#######", "#######", "#######", "#######", "#######", "#######", "lllmmmm", "......." },
                new[] { Pod("leaf", VariantId.Leaf, 3), Pod("moss", VariantId.Moss, 4) });

            Assert.That(Solver.FindJam(level, SolveOptions.Default).Status, Is.EqualTo(SolveStatus.Unsolvable));
        }

        [Test]
        public void ExhaustedBudget_ReturnsUnknown()
        {
            LevelSession level = JamProne();

            SolveResult result = Solver.FindJam(level, new SolveOptions(2));

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Unknown));
            Assert.That(result.NodesUsed, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void Results_AreIdenticalAcrossRuns()
        {
            SolveResult a = Solver.Solve(JamProne(), SolveOptions.Default);
            SolveResult b = Solver.Solve(JamProne(), SolveOptions.Default);
            LevelMetrics ma = Solver.Measure(JamProne(), SolveOptions.Default);
            LevelMetrics mb = Solver.Measure(JamProne(), SolveOptions.Default);

            Assert.That(b.Trace.Select(CommandText.Format), Is.EqualTo(a.Trace.Select(CommandText.Format)));
            Assert.That(b.NodesUsed, Is.EqualTo(a.NodesUsed));
            Assert.That(mb, Is.EqualTo(ma));
        }

        [Test]
        public void Metrics_ReflectUnsafeChoicesAndBufferPressure()
        {
            LevelMetrics metrics = Solver.Measure(JamProne(), SolveOptions.Default);

            Assert.That(metrics.Complete, Is.True);
            Assert.That(metrics.UnsafeChoicePermille, Is.GreaterThan(0), "Some taps lose the level.");
            Assert.That(metrics.DeadEndDepth, Is.EqualTo(5), "A jam needs five commits.");
            Assert.That(metrics.VariantCount, Is.EqualTo(6));
            Assert.That(metrics.SiblingPairs, Is.EqualTo(2), "Leaf+Moss and Water+Dew.");
            Assert.That(metrics.TotalWork, Is.EqualTo(12));
            Assert.That(metrics.PeakBuffer, Is.InRange(1, 5));
        }

        [Test]
        public void DependencyDepth_CountsRegionShells()
        {
            // Entry region Water, then a Leaf shell, then a Flower core.
            LevelSession level = Session(
                new[] { "#######", "#######", "#######", "##lll##", "##lfl##", "##lll##", "wwwwwww", "wwwwwww" },
                new[] { Pod("w", VariantId.Water, 14), Pod("l", VariantId.Leaf, 8), Pod("f", VariantId.Flower, 1) });

            Assert.That(Solver.Measure(level, SolveOptions.Default).DependencyDepth, Is.EqualTo(3));
        }

        [Test]
        public void Scorer_ClassifiesByIntegerThresholds()
        {
            var thresholds = new DifficultyThresholds(new System.Collections.Generic.Dictionary<string, int> { ["unsafeChoicePermille"] = 1000 }, 300, 600);
            LevelMetrics metrics = Solver.Measure(JamProne(), SolveOptions.Default);
            int score = DifficultyScorer.Score(metrics, thresholds);

            Assert.That(score, Is.EqualTo(metrics.UnsafeChoicePermille));
            Assert.That(DifficultyScorer.Classify(299, thresholds), Is.EqualTo(DifficultyClass.Normal));
            Assert.That(DifficultyScorer.Classify(300, thresholds), Is.EqualTo(DifficultyClass.Hard));
            Assert.That(DifficultyScorer.Classify(600, thresholds), Is.EqualTo(DifficultyClass.SuperHard));
        }
    }
}
