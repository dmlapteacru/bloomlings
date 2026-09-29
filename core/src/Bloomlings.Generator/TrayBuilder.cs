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
                pods.Add(new PodDef(id, order[i].Variant, order[i].Count, false, order[i].LockKeyId, null));
                trays[i % stacks].Add(id);
            }

            var stackList = new List<IReadOnlyList<string>>();
            foreach (List<string> stack in trays)
            {
                stackList.Add(stack);
            }

            // Locks name their pod by id, which depends on the order.
            var locks = new List<LockDef>();
            foreach (LockDef lockDef in skeleton.Locks)
            {
                if (lockDef.TargetKind != LockTargetKind.Pod)
                {
                    locks.Add(lockDef);
                }
            }

            foreach (PodDef pod in pods)
            {
                if (pod.LockKeyId != null)
                {
                    locks.Add(new LockDef(pod.LockKeyId, LockTargetKind.Pod, pod.Id));
                }
            }

            return skeleton with { Pods = pods, Tray = new TrayDef(stackList), Locks = locks };
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
            (LevelDefinition definition, LevelAnalysis? first, SolveStatus firstWin) = Evaluate(skeleton, picture, order, stacks, solver, options);
            if (first == null)
            {
                rejection = "tray:plan-not-winnable:" + firstWin.ToString().ToLowerInvariant();
                return null;
            }

            LevelAnalysis analysis = first;

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

                // Prefer a pod from the later part of the plan and put it into the earlier part.
                int late = order.Count / 3;
                int j = Math.Max(1, late) + rng.NextInt(order.Count - Math.Max(1, late));
                int i = rng.NextInt(Math.Max(1, Math.Min(j, (order.Count + 1) / 2)));
                var candidate = new List<PlannedPod>(order);
                PlannedPod moved = candidate[j];
                candidate.RemoveAt(j);
                candidate.Insert(i, moved);
                (LevelDefinition candidateDefinition, LevelAnalysis? candidateAnalysis, SolveStatus _) = Evaluate(skeleton, picture, candidate, stacks, solver, options);
                if (candidateAnalysis == null)
                {
                    continue;
                }

                // Keep a harder level, or one that becomes losable; while the level cannot be lost yet, an equally hard
                // step is kept too, so tempting pods can pile up until a jam becomes possible.
                int candidateScore = DifficultyScorer.Score(candidateAnalysis.Metrics, thresholds);
                bool candidateLosable = candidateAnalysis.Jam.Status == SolveStatus.Solvable;
                bool gainsLosability = requireLosable && !losable && candidateLosable;
                bool drift = requireLosable && !losable && candidateScore >= score;
                if (candidateScore > score || gainsLosability || drift)
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

        /// <summary>
        /// Connects two unlocked pods at the same depth in different stacks (FR-035) and keeps the first pair, in a seeded
        /// order, that leaves the level winnable, losable and in the target class. Null when no pair does.
        /// </summary>
        public static TrayOutcome? Connect(TrayOutcome tray, BasePicture picture, DifficultyClass target, DifficultyThresholds thresholds, SolveOptions options, ref Xoshiro256StarStar rng)
        {
            LevelDefinition level = tray.Definition;
            var locked = new HashSet<string>(StringComparer.Ordinal);
            foreach (PodDef pod in level.Pods)
            {
                if (pod.LockKeyId != null)
                {
                    locked.Add(pod.Id);
                }
            }

            var pairs = new List<(string, string)>();
            IReadOnlyList<IReadOnlyList<string>> stacks = level.Tray.Stacks;
            for (int a = 0; a < stacks.Count; a++)
            {
                for (int b = a + 1; b < stacks.Count; b++)
                {
                    for (int d = 0; d < Math.Min(stacks[a].Count, stacks[b].Count); d++)
                    {
                        if (!locked.Contains(stacks[a][d]) && !locked.Contains(stacks[b][d]))
                        {
                            pairs.Add((stacks[a][d], stacks[b][d]));
                        }
                    }
                }
            }

            var solver = new Solver.Solver();
            for (int attempt = 0; attempt < 6 && pairs.Count > 0; attempt++)
            {
                int pick = rng.NextInt(pairs.Count);
                (string first, string second) = pairs[pick];
                pairs.RemoveAt(pick);
                var pods = new List<PodDef>();
                foreach (PodDef pod in level.Pods)
                {
                    pods.Add(pod.Id == first || pod.Id == second ? pod with { ConnectedGroupId = "c1" } : pod);
                }

                LevelDefinition candidate = level with { Pods = pods };
                LevelSession session = LevelSession.Load(candidate, picture, new SessionOptions(0, 20000));
                SolveResult win = solver.Solve(session, options);
                if (win.Status != SolveStatus.Solvable)
                {
                    continue;
                }

                LevelAnalysis analysis = solver.Analyze(session, options, win);
                int score = DifficultyScorer.Score(analysis.Metrics, thresholds);
                DifficultyClass reached = DifficultyScorer.Classify(score, thresholds);
                if (analysis.Jam.Status == SolveStatus.Solvable && reached == target)
                {
                    return new TrayOutcome(candidate, analysis, score, reached, tray.Injections);
                }
            }

            return null;
        }

        /// <summary>Lays out and analyzes a pod order; the analysis is null when the level is not winnable (then it is skipped).</summary>
        private static (LevelDefinition, LevelAnalysis?, SolveStatus) Evaluate(LevelDefinition skeleton, BasePicture picture, IReadOnlyList<PlannedPod> order, int stacks, Solver.Solver solver, SolveOptions options)
        {
            LevelDefinition definition = Layout(skeleton, order, stacks);
            LevelSession session = LevelSession.Load(definition, picture, new SessionOptions(0, 20000));
            SolveResult win = solver.Solve(session, options);
            return win.Status == SolveStatus.Solvable
                ? (definition, solver.Analyze(session, options, win), win.Status)
                : (definition, null, win.Status);
        }

        private static int Rank(DifficultyClass c) => c == DifficultyClass.Normal ? 0 : c == DifficultyClass.Hard ? 1 : 2;
    }
}
