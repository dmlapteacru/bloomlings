using System;
using System.Collections.Generic;
using System.Text;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Search
{
    /// <summary>Which pods a search tries first.</summary>
    public enum MoveOrder
    {
        /// <summary>Pods that clear tiles at once first: finds wins quickly (R8).</summary>
        ProgressFirst,

        /// <summary>Pods that would sit idle first: finds jams quickly (jam witnesses, FR-081).</summary>
        IdleFirst,
    }

    public enum SearchOutcome
    {
        /// <summary>A goal state was reached; <see cref="SearchResult.Path"/> leads to it.</summary>
        Found,

        /// <summary>The whole space was explored: no goal state exists.</summary>
        NotFound,

        /// <summary>The node budget ran out first. Budgets count nodes, never time, so the outcome is reproducible.</summary>
        BudgetExceeded,
    }

    public sealed class SearchResult
    {
        public SearchResult(SearchOutcome outcome, IReadOnlyList<Command> path, int nodesUsed)
        {
            Outcome = outcome;
            Path = path;
            NodesUsed = nodesUsed;
        }

        public SearchOutcome Outcome { get; }

        public IReadOnlyList<Command> Path { get; }

        public int NodesUsed { get; }
    }

    /// <summary>
    /// Depth-first search over settled states (research R8, T076). It lives in Core so that the solver, the generator
    /// and the runtime Shuffle (R10) share it. Each node is one applied command on a cloned <see cref="LevelSession"/>,
    /// so the search uses exactly the game rules. It uses a transposition table keyed by the state hash, orders moves
    /// (progressable pods first, or idle pods first), prunes symmetric exposed pods, and stops at a node budget.
    /// </summary>
    public static class StateSearch
    {
        /// <summary>Searches for a state that satisfies <paramref name="goal"/>.</summary>
        public static SearchResult Find(
            LevelSession start,
            Func<LevelSession, bool> goal,
            MoveOrder order,
            int nodeBudget,
            TranspositionTable? table = null)
        {
            var search = new Dfs(goal, order, nodeBudget, table ?? new TranspositionTable());
            var path = new List<Command>();
            bool found = search.Run(start, path);
            SearchOutcome outcome = found ? SearchOutcome.Found : search.BudgetExceeded ? SearchOutcome.BudgetExceeded : SearchOutcome.NotFound;
            return new SearchResult(outcome, found ? path : (IReadOnlyList<Command>)Array.Empty<Command>(), search.Nodes);
        }

        public static bool IsWon(LevelSession session) => session.Status == LevelStatus.Won;

        public static bool IsLost(LevelSession session) => session.Status == LevelStatus.Jammed || session.Status == LevelStatus.Stuck;

        /// <summary>
        /// The legal taps of a state after symmetry pruning, in the given order. Exposed pods with the same variant,
        /// count and modifiers and the same pods below them lead to equivalent states, so only the first is kept.
        /// </summary>
        public static IReadOnlyList<Command> Moves(LevelSession session, MoveOrder order)
        {
            LevelState state = session.State;
            if (state.Status != LevelStatus.Playing)
            {
                return Array.Empty<Command>();
            }

            bool[] progressable = ProgressableVariants(state);
            int[]? looks = state.PodLookClass;
            HashSet<string>? seen = looks == null ? new HashSet<string>(StringComparer.Ordinal) : null;
            Span<int> kept = stackalloc int[SourceTray.MaxStacks];
            int keptCount = 0;
            var first = new List<Command>();
            var second = new List<Command>();
            SourceTray tray = state.Tray;
            for (int s = 0; s < tray.StackCount; s++)
            {
                // The exposed pods in stack order (SourceTray.Exposed). The state is Playing, so a tap is allowed exactly
                // when the pod may be committed (LevelSession.Check).
                int pod = tray.TopOf(s);
                if (pod < 0 || state.CanCommit(pod) != null)
                {
                    continue;
                }

                // Only the first of the allowed pods with the same signature is kept.
                if (looks == null)
                {
                    if (!seen!.Add(Signature(state, pod)))
                    {
                        continue;
                    }
                }
                else
                {
                    bool same = false;
                    for (int k = 0; k < keptCount && !same; k++)
                    {
                        same = SameSignature(state, looks, kept[k], s);
                    }

                    if (same)
                    {
                        continue;
                    }

                    kept[keptCount++] = s;
                }

                bool progresses = progressable[state.PodVariantIndex[pod]];
                bool early = order == MoveOrder.ProgressFirst ? progresses : !progresses;
                (early ? first : second).Add(new TapPod(state.PodId(pod)));
            }

            first.AddRange(second);
            return first;
        }

        /// <summary>Catalog indexes of variants that have a reachable, visible top layer now.</summary>
        internal static bool[] ProgressableVariants(LevelState state)
        {
            var result = new bool[state.Catalog.Count];
            ReachabilityResult reach = state.Board.ReachTargets;
            foreach (int target in reach.Packed)
            {
                int cell = ReachabilityResult.CellOf(target);
                if (!state.Board.IsMysteryHidden(cell))
                {
                    int code = state.Board.TopCode(cell);
                    result[code >= 0 ? code : state.Catalog.IndexOf(state.Board.TopLayer(cell))] = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Whether the stacks <paramref name="a"/> and <paramref name="b"/> have equal <see cref="Signature"/> texts,
        /// without building them: each pod prints as "variant:remaining", then "?" while a hidden mystery, "L" and its
        /// lock key, "C" and its group, then "|". With <see cref="LevelState.PodLookClass"/> set no id holds those
        /// separators, so two texts are equal exactly when the stacks are equally long and every pair of pods at the same
        /// depth has the same look class, remaining count and mystery flag.
        /// </summary>
        private static bool SameSignature(LevelState state, int[] looks, int a, int b)
        {
            SourceTray tray = state.Tray;
            int count = tray.CountIn(a);
            if (tray.CountIn(b) != count)
            {
                return false;
            }

            for (int depth = 0; depth < count; depth++)
            {
                int p = tray.PodFromTop(a, depth);
                int q = tray.PodFromTop(b, depth);
                if (looks[p] != looks[q]
                    || state.Pods[p].Remaining != state.Pods[q].Remaining
                    || (state.PodDefs[p].Mystery && !state.Pods[p].VariantRevealed) != (state.PodDefs[q].Mystery && !state.Pods[q].VariantRevealed))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Signature(LevelState state, int exposedPod)
        {
            var sb = new StringBuilder();
            foreach (int pod in state.Tray.StackTopFirst(state.Tray.StackOf(exposedPod)))
            {
                PodDef def = state.PodDefs[pod];
                PodRuntime runtime = state.Pods[pod];
                sb.Append(def.Variant.Key).Append(':').Append(runtime.Remaining)
                    .Append(def.Mystery && !runtime.VariantRevealed ? "?" : string.Empty)
                    .Append(def.LockKeyId != null ? "L" + def.LockKeyId : string.Empty)
                    .Append(def.ConnectedGroupId != null ? "C" + def.ConnectedGroupId : string.Empty)
                    .Append('|');
            }

            return sb.ToString();
        }

        private sealed class Dfs
        {
            private readonly Func<LevelSession, bool> _goal;
            private readonly MoveOrder _order;
            private readonly int _budget;
            private readonly TranspositionTable _table;

            private readonly bool _reuse;
            private readonly List<LevelSession?> _spares = new List<LevelSession?>();

            public Dfs(Func<LevelSession, bool> goal, MoveOrder order, int budget, TranspositionTable table)
            {
                _goal = goal;
                _order = order;
                _budget = budget;
                _table = table;
                _reuse = goal.Equals((Func<LevelSession, bool>)IsWon) || goal.Equals((Func<LevelSession, bool>)IsLost);
            }

            public int Nodes { get; private set; }

            public bool BudgetExceeded { get; private set; }

            public bool Run(LevelSession session, List<Command> path)
            {
                if (_goal(session))
                {
                    return true;
                }

                ulong hash = session.StateHash;
                if (_table.TryGet(hash, out bool known))
                {
                    if (!known)
                    {
                        return false;
                    }

                    // Known to reach the goal but the path is not stored: search on to rebuild it.
                }

                // The children of this depth are explored one after another, and none is kept once its subtree is done,
                // so one session per depth is reused for them (only when the goal is a built-in one, which keeps no
                // session).
                int depth = path.Count;
                foreach (Command move in Moves(session, _order))
                {
                    if (Nodes >= _budget)
                    {
                        BudgetExceeded = true;
                        return false;
                    }

                    LevelSession? child = session.SearchChild(move, _reuse && depth < _spares.Count ? _spares[depth] : null);
                    Nodes++;
                    if (child == null)
                    {
                        continue;
                    }

                    if (_reuse)
                    {
                        if (depth < _spares.Count)
                        {
                            _spares[depth] = child;
                        }
                        else
                        {
                            _spares.Add(child);
                        }
                    }

                    path.Add(move);
                    if (Run(child, path))
                    {
                        _table.Set(hash, true);
                        return true;
                    }

                    path.RemoveAt(path.Count - 1);
                    if (BudgetExceeded)
                    {
                        return false;
                    }
                }

                _table.Set(hash, false);
                return false;
            }
        }
    }
}
