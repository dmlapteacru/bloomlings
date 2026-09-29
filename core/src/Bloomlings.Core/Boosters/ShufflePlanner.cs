using System;
using System.Collections.Generic;
using Bloomlings.Core.Random;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Shuffle with its winnability guarantee (FR-044, research R10, T117). Only the pods still in the tray move; pods in
    /// slots are untouched, locks stay on their pods, and a connected group is laid out at one depth in different
    /// stacks. The plan:
    /// <list type="number">
    /// <item>solve the relaxed problem, in which any tray pod (or group) may be committed at any time;</item>
    /// <item>lay its commit order out round-robin across the existing stacks, so the exposure order is that order;</item>
    /// <item>verify the arrangement with the normal search;</item>
    /// <item>otherwise try seeded candidates, seeded from (level seed, content version, shuffle uses, state hash);</item>
    /// <item>as a last resort, put the pods that can progress now on top.</item>
    /// </list>
    /// Everything shares one node budget, <see cref="SessionOptions.ShuffleNodeBudget"/>, which comes from the content
    /// manifest, so every device gets the same result. If the relaxed problem has a solution, the result is winnable.
    /// </summary>
    internal static class ShufflePlanner
    {
        private const int SeededCandidates = 8;

        /// <summary>
        /// Two or more tray pods are needed. On a Jammed board no order of the tray can free a slot, so Shuffle is
        /// disabled; on a Stuck board it needs a buried pod that could be committed once exposed (FR-046).
        /// </summary>
        public static bool CanApply(LevelState state)
        {
            int inTray = 0;
            foreach (PodRuntime pod in state.Pods)
            {
                if (pod.Location == PodLocation.Tray)
                {
                    inTray++;
                }
            }

            if (inTray < 2)
            {
                return false;
            }

            return state.Status switch
            {
                LevelStatus.Jammed => false,
                LevelStatus.Stuck => RecoveryCheck.BuriedPodFits(state),
                _ => true,
            };
        }

        /// <summary>The new arrangement: pod indexes per stack, top first.</summary>
        public static IReadOnlyList<IReadOnlyList<int>> Plan(LevelSession session)
        {
            LevelState state = session.State;
            int stacks = state.Tray.StackCount;
            var budget = new int[] { Math.Max(1, state.Options.ShuffleNodeBudget) };
            List<int[]> units = Units(state);

            List<int[]>? order = RelaxedSolve(session, budget);
            if (order != null)
            {
                IReadOnlyList<IReadOnlyList<int>> layout = Layout(order, stacks);
                if (Verify(session, layout, budget))
                {
                    return layout;
                }
            }

            ulong seed = SplitMix64.Mix(state.Definition.Seed
                ^ SplitMix64.Mix((ulong)(uint)state.Options.ContentVersion)
                ^ SplitMix64.Mix(0x5B0FF1E0UL + (ulong)(uint)state.ShuffleUses)
                ^ state.StateHash);
            var rng = new Xoshiro256StarStar(seed);
            for (int candidate = 0; candidate < SeededCandidates && budget[0] > 0; candidate++)
            {
                var shuffled = new List<int[]>(units);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = rng.NextInt(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                IReadOnlyList<IReadOnlyList<int>> layout = Layout(shuffled, stacks);
                if (Verify(session, layout, budget))
                {
                    return layout;
                }
            }

            return Layout(ProgressFirst(state, units), stacks);
        }

        /// <summary>The tray pods as commit units: a connected group still whole in the tray, or a single pod.</summary>
        private static List<int[]> Units(LevelState state)
        {
            var units = new List<int[]>();
            var seen = new HashSet<int>();
            for (int p = 0; p < state.Pods.Length; p++)
            {
                if (state.Pods[p].Location != PodLocation.Tray || seen.Contains(p))
                {
                    continue;
                }

                int[] group = state.ActiveGroup(p);
                foreach (int member in group)
                {
                    seen.Add(member);
                }

                units.Add(group);
            }

            return units;
        }

        /// <summary>
        /// Depth-first search of the relaxed problem; returns the commit order of the winning line, or null when the
        /// relaxed problem has no solution within the budget (then no arrangement can be won either).
        /// </summary>
        private static List<int[]>? RelaxedSolve(LevelSession start, int[] budget)
        {
            var visited = new HashSet<ulong>();
            var path = new List<int[]>();
            return Dfs(start, path, visited, budget) ? path : null;
        }

        private static bool Dfs(LevelSession session, List<int[]> path, HashSet<ulong> visited, int[] budget)
        {
            LevelState state = session.State;
            if (state.Status == LevelStatus.Won)
            {
                return true;
            }

            if (state.Status != LevelStatus.Playing && state.Status != LevelStatus.Stuck)
            {
                return false;
            }

            if (!visited.Add(state.RelaxedHash()))
            {
                return false;
            }

            bool[] progressable = StateSearch.ProgressableVariants(state);
            var first = new List<int[]>();
            var second = new List<int[]>();
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (int[] unit in Units(state))
            {
                if (!state.CanCommitRelaxed(unit) || !signatures.Add(Signature(state, unit)))
                {
                    continue;
                }

                bool progresses = false;
                foreach (int pod in unit)
                {
                    progresses |= progressable[state.PodVariantIndex[pod]];
                }

                (progresses ? first : second).Add(unit);
            }

            first.AddRange(second);
            foreach (int[] unit in first)
            {
                if (budget[0] <= 0)
                {
                    return false;
                }

                budget[0]--;
                LevelSession child = session.RelaxedChild(unit);
                path.Add(unit);
                if (Dfs(child, path, visited, budget))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        /// <summary>Units that are interchangeable in the relaxed problem: same variants, counts, locks and group size.</summary>
        private static string Signature(LevelState state, int[] unit)
        {
            var parts = new List<string>();
            foreach (int pod in unit)
            {
                parts.Add(state.PodVariant(pod).Key + ":" + state.Pods[pod].Remaining + (state.PodDefs[pod].LockKeyId ?? string.Empty));
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join("|", parts);
        }

        private static bool Verify(LevelSession session, IReadOnlyList<IReadOnlyList<int>> layout, int[] budget)
        {
            if (budget[0] <= 0)
            {
                return false;
            }

            LevelSession arranged = session.WithTray(layout);
            SearchResult result = StateSearch.Find(arranged, StateSearch.IsWon, MoveOrder.ProgressFirst, budget[0]);
            budget[0] -= result.NodesUsed;
            return result.Outcome == SearchOutcome.Found;
        }

        /// <summary>The last resort: units that can progress now come first, then the rest in their order.</summary>
        private static List<int[]> ProgressFirst(LevelState state, List<int[]> units)
        {
            bool[] progressable = StateSearch.ProgressableVariants(state);
            var first = new List<int[]>();
            var rest = new List<int[]>();
            foreach (int[] unit in units)
            {
                bool progresses = false;
                foreach (int pod in unit)
                {
                    progresses |= progressable[state.PodVariantIndex[pod]] && !state.IsPodLocked(pod);
                }

                (progresses ? first : rest).Add(unit);
            }

            first.AddRange(rest);
            return first;
        }

        /// <summary>
        /// Round-robin layout: each pod goes on the lowest stack (leftmost on ties), so the exposure order follows the
        /// unit order. A group needs as many stacks of equal height as it has members; until such stacks exist, the
        /// following single pods go first.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<int>> Layout(IReadOnlyList<int[]> order, int stackCount)
        {
            var stacks = new List<int>[stackCount];
            for (int s = 0; s < stackCount; s++)
            {
                stacks[s] = new List<int>();
            }

            var pending = new List<int[]>();
            foreach (int[] unit in order)
            {
                if (unit.Length == 1)
                {
                    stacks[Lowest(stacks)].Add(unit[0]);
                    PlacePending(stacks, pending, force: false);
                }
                else if (!TryPlaceGroup(stacks, unit))
                {
                    pending.Add(unit);
                }
            }

            PlacePending(stacks, pending, force: true);
            var result = new IReadOnlyList<int>[stackCount];
            for (int s = 0; s < stackCount; s++)
            {
                result[s] = stacks[s];
            }

            return result;
        }

        private static void PlacePending(List<int>[] stacks, List<int[]> pending, bool force)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                if (TryPlaceGroup(stacks, pending[i]))
                {
                    pending.RemoveAt(i--);
                }
                else if (force)
                {
                    // No equal heights left: the lowest stacks take it (the verification decides whether it works).
                    var used = new HashSet<int>();
                    foreach (int pod in pending[i])
                    {
                        int s = Lowest(stacks, used);
                        stacks[s].Add(pod);
                        used.Add(s);
                    }

                    pending.RemoveAt(i--);
                }
            }
        }

        private static bool TryPlaceGroup(List<int>[] stacks, int[] group)
        {
            if (group.Length > stacks.Length)
            {
                return false;
            }

            int minHeight = int.MaxValue;
            foreach (List<int> stack in stacks)
            {
                minHeight = Math.Min(minHeight, stack.Count);
            }

            var chosen = new List<int>();
            for (int s = 0; s < stacks.Length && chosen.Count < group.Length; s++)
            {
                if (stacks[s].Count == minHeight)
                {
                    chosen.Add(s);
                }
            }

            if (chosen.Count < group.Length)
            {
                return false;
            }

            for (int i = 0; i < group.Length; i++)
            {
                stacks[chosen[i]].Add(group[i]);
            }

            return true;
        }

        private static int Lowest(List<int>[] stacks, HashSet<int>? exclude = null)
        {
            int best = -1;
            for (int s = 0; s < stacks.Length; s++)
            {
                if ((exclude == null || !exclude.Contains(s)) && (best < 0 || stacks[s].Count < stacks[best].Count))
                {
                    best = s;
                }
            }

            return best;
        }
    }
}
