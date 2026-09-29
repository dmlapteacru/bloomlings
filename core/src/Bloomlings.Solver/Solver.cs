using System;
using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

namespace Bloomlings.Solver
{
    /// <summary>
    /// The deterministic solver (R8, T077): depth-first search over settled states with a transposition table, move
    /// ordering and symmetry pruning (<see cref="StateSearch"/>). <see cref="Measure"/> also derives the difficulty
    /// metrics.
    /// </summary>
    public sealed class Solver : ISolver
    {
        /// <summary>Recorded in validation records; bump it when solver behavior changes.</summary>
        public const string Version = "solver-1.0.0";

        public SolveResult Solve(LevelSession start, SolveOptions options) =>
            ToResult(StateSearch.Find(start, StateSearch.IsWon, MoveOrder.ProgressFirst, options.NodeBudget));

        public SolveResult FindJam(LevelSession start, SolveOptions options) =>
            ToResult(StateSearch.Find(start, StateSearch.IsLost, MoveOrder.IdleFirst, options.NodeBudget));

        public LevelMetrics Measure(LevelSession start, SolveOptions options) => Analyze(start, options).Metrics;

        /// <summary>The winning trace, the jam witness and the metrics in one pass.</summary>
        public LevelAnalysis Analyze(LevelSession start, SolveOptions options)
        {
            SolveResult win = Solve(start, options);
            SolveResult jam = FindJam(start, options);
            return new LevelAnalysis(win, jam, Metrics(start, options, win, jam));
        }

        private static LevelMetrics Metrics(LevelSession start, SolveOptions options, SolveResult win, SolveResult jam)
        {
            bool complete = true;
            complete &= win.Status != SolveStatus.Unknown && jam.Status != SolveStatus.Unknown;

            // Branching and unsafe choices over every winnable state the budget can reach.
            var analysis = new TreeAnalysis(options.NodeBudget);
            analysis.Winnable(start);
            complete &= !analysis.BudgetExceeded;

            // Buffer use along the winning line.
            int peak = 0;
            int bufferSum = 0;
            int commits = 0;
            LevelSession session = start.Clone();
            foreach (Command step in win.Trace)
            {
                int occupiedBefore = OccupiedSlots(session);
                CommandResult applied = session.Apply(step);
                int committed = 0;
                foreach (GameEvent e in applied.Events)
                {
                    if (e is PodCommitted)
                    {
                        committed++;
                    }
                }

                int occupied = occupiedBefore + committed;
                peak = Math.Max(peak, occupied);
                bufferSum += occupied;
                commits++;
            }

            LevelDefinition definition = start.Definition;
            Board board = BoardBuilder.Build(definition, start.Picture, start.Options.Catalog);
            (int variants, int siblings) = VariantLoad(definition, start.Options.Catalog);
            int work = 0;
            int connected = 0;
            var groups = new HashSet<string>(StringComparer.Ordinal);
            int locked = definition.Slots.Locked != null ? 1 : 0;
            foreach (PodDef pod in definition.Pods)
            {
                work += pod.Count;
                if (pod.ConnectedGroupId != null && groups.Add(pod.ConnectedGroupId))
                {
                    connected++;
                }

                if (pod.LockKeyId != null)
                {
                    locked++;
                }
            }

            int keys = 0;
            foreach (CellOverlay overlay in definition.Overlays)
            {
                if (overlay.KeyId != null)
                {
                    keys++;
                }
            }

            return new LevelMetrics(
                DependencyDepth(board),
                analysis.WinnableStates == 0 ? 0 : (int)((long)analysis.MovesInWinnableStates * 1000 / analysis.WinnableStates),
                analysis.MovesInWinnableStates == 0 ? 0 : (int)((long)analysis.LosingMovesInWinnableStates * 1000 / analysis.MovesInWinnableStates),
                jam.Status == SolveStatus.Solvable ? CountCommits(jam.Trace) : 0,
                peak,
                commits == 0 ? 0 : (int)((long)bufferSum * 1000 / commits),
                connected,
                variants,
                siblings,
                CrossVariantLayers(board),
                definition.Specials.Count + keys + locked,
                work,
                definition.Pods.Count,
                (definition.Pods.Count * 1500) + (work * 120),
                complete);
        }

        /// <summary>
        /// Region shells from the entry: a 0-1 search where entering a target of a different same-variant region costs
        /// 1 and open cells cost nothing. The depth is the number of regions passed to reach the deepest target.
        /// </summary>
        public static int DependencyDepth(Board board)
        {
            int n = board.CellCount;
            var component = new int[n];
            for (int i = 0; i < n; i++)
            {
                component[i] = -1;
            }

            int components = 0;
            var stack = new Stack<int>();
            for (int i = 0; i < n; i++)
            {
                if (!board.IsTarget(i) || component[i] >= 0)
                {
                    continue;
                }

                VariantId variant = board.TopLayer(i);
                component[i] = components;
                stack.Push(i);
                while (stack.Count > 0)
                {
                    int cell = stack.Pop();
                    CellPos pos = board.PosOf(cell);
                    for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                    {
                        if (pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                        {
                            int j = board.IndexOf(next);
                            if (board.IsTarget(j) && component[j] < 0 && board.TopLayer(j) == variant)
                            {
                                component[j] = components;
                                stack.Push(j);
                            }
                        }
                    }
                }

                components++;
            }

            var cost = new int[n];
            for (int i = 0; i < n; i++)
            {
                cost[i] = int.MaxValue;
            }

            var deque = new LinkedList<int>();
            foreach (EntryDef entry in board.Entries)
            {
                int cell = board.IndexOf(entry.Cell);
                if (board.KindAt(cell) == CellKind.Stone || board.KindAt(cell) == CellKind.Special)
                {
                    continue;
                }

                cost[cell] = board.IsTarget(cell) ? 1 : 0;
                deque.AddLast(cell);
            }

            while (deque.Count > 0)
            {
                int cell = deque.First!.Value;
                deque.RemoveFirst();
                CellPos pos = board.PosOf(cell);
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    if (!pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                    {
                        continue;
                    }

                    int j = board.IndexOf(next);
                    CellKind kind = board.KindAt(j);
                    if (kind == CellKind.Stone || kind == CellKind.Special)
                    {
                        continue;
                    }

                    bool step = board.IsTarget(j) && (!board.IsTarget(cell) || component[j] != component[cell]);
                    int c = cost[cell] + (step ? 1 : 0);
                    if (c < cost[j])
                    {
                        cost[j] = c;
                        if (step)
                        {
                            deque.AddLast(j);
                        }
                        else
                        {
                            deque.AddFirst(j);
                        }
                    }
                }
            }

            int depth = 0;
            for (int i = 0; i < n; i++)
            {
                if (board.IsTarget(i) && cost[i] != int.MaxValue)
                {
                    depth = Math.Max(depth, cost[i]);
                }
            }

            return depth;
        }

        private static (int Variants, int Siblings) VariantLoad(LevelDefinition definition, VariantCatalog catalog)
        {
            var variants = new SortedSet<VariantId>();
            foreach (PodDef pod in definition.Pods)
            {
                variants.Add(pod.Variant);
            }

            var perFamily = new int[4];
            foreach (VariantId variant in variants)
            {
                perFamily[(int)catalog.Get(variant).Family]++;
            }

            int siblings = 0;
            foreach (int count in perFamily)
            {
                if (count >= 2)
                {
                    siblings++;
                }
            }

            return (variants.Count, siblings);
        }

        private static int CrossVariantLayers(Board board)
        {
            int count = 0;
            for (int i = 0; i < board.CellCount; i++)
            {
                for (int d = 1; d < board.OriginalLayerCount(i); d++)
                {
                    if (board.LayerAt(i, d) != board.LayerAt(i, d - 1))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int OccupiedSlots(LevelSession session)
        {
            int occupied = 0;
            for (int s = 0; s < session.View.SlotCapacity; s++)
            {
                if (session.View.PodInSlot(s) != null)
                {
                    occupied++;
                }
            }

            return occupied;
        }

        private static int CountCommits(IReadOnlyList<Command> trace)
        {
            int count = 0;
            foreach (Command command in trace)
            {
                if (command is TapPod)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Decides winnability for every state reachable within a node budget (memoized by state hash) and counts, in
        /// winnable states, the legal taps and the taps after which the level can no longer be won.
        /// </summary>
        private sealed class TreeAnalysis
        {
            private readonly Dictionary<ulong, bool> _memo = new Dictionary<ulong, bool>();
            private readonly int _budget;
            private int _nodes;

            public TreeAnalysis(int budget)
            {
                _budget = budget;
            }

            public bool BudgetExceeded { get; private set; }

            public int WinnableStates { get; private set; }

            public int MovesInWinnableStates { get; private set; }

            public int LosingMovesInWinnableStates { get; private set; }

            /// <summary>True when a win is reachable; unknown parts (budget) count as winnable.</summary>
            public bool Winnable(LevelSession session)
            {
                if (session.Status == LevelStatus.Won)
                {
                    return true;
                }

                if (session.Status != LevelStatus.Playing)
                {
                    return false;
                }

                ulong hash = session.StateHash;
                if (_memo.TryGetValue(hash, out bool known))
                {
                    return known;
                }

                int moves = 0;
                int losing = 0;
                bool winnable = false;
                foreach (Command move in StateSearch.Moves(session, MoveOrder.ProgressFirst))
                {
                    if (_nodes >= _budget)
                    {
                        BudgetExceeded = true;
                        return true;
                    }

                    LevelSession child = session.Clone();
                    _nodes++;
                    child.Apply(move);
                    moves++;
                    if (Winnable(child))
                    {
                        winnable = true;
                    }
                    else
                    {
                        losing++;
                    }

                    if (BudgetExceeded)
                    {
                        return true;
                    }
                }

                _memo[hash] = winnable;
                if (winnable)
                {
                    WinnableStates++;
                    MovesInWinnableStates += moves;
                    LosingMovesInWinnableStates += losing;
                }

                return winnable;
            }
        }

        private static SolveResult ToResult(SearchResult result) => new SolveResult(
            result.Outcome switch
            {
                SearchOutcome.Found => SolveStatus.Solvable,
                SearchOutcome.NotFound => SolveStatus.Unsolvable,
                _ => SolveStatus.Unknown,
            },
            result.Path,
            result.NodesUsed);
    }
}
