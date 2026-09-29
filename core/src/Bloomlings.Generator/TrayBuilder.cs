using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Solver;

namespace Bloomlings.Generator
{
    /// <summary>A built tray with its level and analysis.</summary>
    public sealed record TrayOutcome(LevelDefinition Definition, LevelAnalysis Analysis, int Score, DifficultyClass Class, int Injections);

    /// <summary>
    /// R9 step 7 (T088). The planned pods are laid out round-robin: pod i goes to stack i mod k at depth ⌊i/k⌋, so the
    /// exposure order equals the plan and the plan is playable. Difficulty is then injected by moving a later pod
    /// earlier (a tempting premature pod that would sit in a slot, which also buries the needed pods behind it). Each
    /// step is re-solved and kept only if the level stays winnable and gets harder, until the score reaches the target
    /// class and the level can be lost (FR-081).
    /// </summary>
    public static class TrayBuilder
    {
        public static LevelDefinition Layout(LevelDefinition skeleton, IReadOnlyList<PlannedPod> order, int stacks)
        {
            var pods = new List<PodDef>();
            var trays = new List<List<string>>();
            for (int s = 0; s < stacks; s++)
            {
                trays.Add(new List<string>());
            }

            for (int i = 0; i < order.Count; i++)
            {
                string id = "p" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                pods.Add(new PodDef(id, order[i].Variant, order[i].Count, false, null, null));
                trays[i % stacks].Add(id);
            }

            var stackList = new List<IReadOnlyList<string>>();
            foreach (List<string> stack in trays)
            {
                stackList.Add(stack);
            }

            return skeleton with { Pods = pods, Tray = new TrayDef(stackList) };
        }

        public static TrayOutcome? Tune(
            LevelDefinition skeleton,
            BasePicture picture,
            IReadOnlyList<PlannedPod> plan,
            int stacks,
            DifficultyClass target,
            bool requireLosable,
            int maxInjections,
            DifficultyThresholds thresholds,
            SolveOptions options,
            ref Xoshiro256StarStar rng,
            out string? rejection)
        {
            var solver = new Solver.Solver();
            var order = new List<PlannedPod>(plan);
            (LevelDefinition definition, LevelAnalysis analysis) = Evaluate(skeleton, picture, order, stacks, solver, options);
            if (analysis.Win.Status != SolveStatus.Solvable)
            {
                rejection = "tray:plan-not-winnable:" + analysis.Win.Status.ToString().ToLowerInvariant();
                return null;
            }

            int score = DifficultyScorer.Score(analysis.Metrics, thresholds);
            int injections = 0;
            for (int attempt = 0; attempt <= maxInjections * 3; attempt++)
            {
                DifficultyClass current = DifficultyScorer.Classify(score, thresholds);
                bool losable = analysis.Jam.Status == SolveStatus.Solvable;
                if (current == target && (losable || !requireLosable))
                {
                    rejection = null;
                    return new TrayOutcome(definition, analysis, score, current, injections);
                }

                if (Rank(current) > Rank(target) || injections >= maxInjections)
                {
                    break;
                }

                // Move a later pod forward: it tempts the player before its tiles are reachable.
                if (order.Count < 2)
                {
                    break;
                }

                int j = 1 + rng.NextInt(order.Count - 1);
                int i = rng.NextInt(j);
                var candidate = new List<PlannedPod>(order);
                PlannedPod moved = candidate[j];
                candidate.RemoveAt(j);
                candidate.Insert(i, moved);
                (LevelDefinition candidateDefinition, LevelAnalysis candidateAnalysis) = Evaluate(skeleton, picture, candidate, stacks, solver, options);
                if (candidateAnalysis.Win.Status != SolveStatus.Solvable)
                {
                    continue;
                }

                int candidateScore = DifficultyScorer.Score(candidateAnalysis.Metrics, thresholds);
                bool gainsLosability = requireLosable && !losable && candidateAnalysis.Jam.Status == SolveStatus.Solvable;
                if (candidateScore > score || gainsLosability)
                {
                    order = candidate;
                    definition = candidateDefinition;
                    analysis = candidateAnalysis;
                    score = candidateScore;
                    injections++;
                }
            }

            DifficultyClass reached = DifficultyScorer.Classify(score, thresholds);
            rejection = reached == target ? "tray:not-losable" : $"tray:class-{reached.ToString().ToLowerInvariant()}-not-{target.ToString().ToLowerInvariant()}";
            return null;
        }

        private static (LevelDefinition, LevelAnalysis) Evaluate(LevelDefinition skeleton, BasePicture picture, IReadOnlyList<PlannedPod> order, int stacks, Solver.Solver solver, SolveOptions options)
        {
            LevelDefinition definition = Layout(skeleton, order, stacks);
            LevelSession session = LevelSession.Load(definition, picture, new SessionOptions(0, 20000));
            return (definition, solver.Analyze(session, options));
        }

        private static int Rank(DifficultyClass c) => c == DifficultyClass.Normal ? 0 : c == DifficultyClass.Hard ? 1 : 2;
    }
}
