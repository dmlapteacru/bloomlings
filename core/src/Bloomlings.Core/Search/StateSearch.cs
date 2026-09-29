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
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var first = new List<Command>();
            var second = new List<Command>();
            foreach (int pod in state.Tray.Exposed())
            {
                if (!session.Check(new TapPod(state.PodId(pod))).IsAllowed || !seen.Add(Signature(state, pod)))
                {
                    continue;
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
            ReachabilityResult reach = Reachability.Compute(state.Board);
            foreach (ReachableTarget target in reach.Targets)
            {
                if (!state.Board.IsMysteryHidden(target.Index))
                {
                    result[state.Catalog.IndexOf(state.Board.TopLayer(target.Index))] = true;
                }
            }

            return result;
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

            public Dfs(Func<LevelSession, bool> goal, MoveOrder order, int budget, TranspositionTable table)
            {
                _goal = goal;
                _order = order;
                _budget = budget;
                _table = table;
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

                foreach (Command move in Moves(session, _order))
                {
                    if (Nodes >= _budget)
                    {
                        BudgetExceeded = true;
                        return false;
                    }

                    LevelSession child = session.Clone();
                    Nodes++;
                    if (!child.Apply(move).Accepted)
                    {
                        continue;
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
