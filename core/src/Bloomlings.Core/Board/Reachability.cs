using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Boards
{
    /// <summary>A target cell whose top layer Bloomlings can reach, with its route distance.</summary>
    public readonly struct ReachableTarget
    {
        public ReachableTarget(int index, CellPos cell, int distance)
        {
            Index = index;
            Cell = cell;
            Distance = distance;
        }

        public int Index { get; }

        public CellPos Cell { get; }

        /// <summary>Route distance from the nearest Garden Entry (the entry cell itself is at distance 1).</summary>
        public int Distance { get; }
    }

    /// <summary>
    /// Reachability (FR-010, research R3). A breadth-first search runs from every Garden Entry over open cells, using
    /// the 4-neighbourhood only. The cell at an entry is at distance 1. A target is reachable when it is an entry cell
    /// (distance 1) or touches a reached open cell at distance d (distance d + 1). Stones and special cells are never
    /// walkable. Targets are ordered by the fixed tie-break of FR-021.
    /// </summary>
    public static class Reachability
    {
        public static ReachabilityResult Compute(Board board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            int n = board.CellCount;
            var distance = new int[n];
            var parent = new int[n];
            for (int i = 0; i < n; i++)
            {
                distance[i] = -1;
                parent[i] = -1;
            }

            // Multi-source BFS: every walkable entry cell starts at distance 1, in definition order.
            var queue = new int[n];
            int head = 0;
            int tail = 0;
            foreach (var entry in board.Entries)
            {
                int index = board.IndexOf(entry.Cell);
                if (board.IsWalkable(index) && distance[index] < 0)
                {
                    distance[index] = 1;
                    queue[tail++] = index;
                }
            }

            while (head < tail)
            {
                int current = queue[head++];
                CellPos pos = board.PosOf(current);
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    if (!pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                    {
                        continue;
                    }

                    int nextIndex = board.IndexOf(next);
                    if (distance[nextIndex] >= 0 || !board.IsWalkable(nextIndex))
                    {
                        continue;
                    }

                    distance[nextIndex] = distance[current] + 1;
                    parent[nextIndex] = current;
                    queue[tail++] = nextIndex;
                }
            }

            // Targets: an entry cell, or adjacent to a reached open cell.
            var targets = new List<ReachableTarget>();
            var via = new int[n];
            for (int i = 0; i < n; i++)
            {
                via[i] = -1;
                if (!board.IsTarget(i))
                {
                    continue;
                }

                int best = int.MaxValue;
                int bestVia = -1;
                if (board.IsEntryCell(i))
                {
                    best = 1;
                }

                CellPos pos = board.PosOf(i);
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    if (!pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                    {
                        continue;
                    }

                    int nextIndex = board.IndexOf(next);
                    int d = distance[nextIndex];
                    if (d > 0 && d + 1 < best)
                    {
                        best = d + 1;
                        bestVia = nextIndex;
                    }
                }

                if (best != int.MaxValue)
                {
                    via[i] = bestVia;
                    targets.Add(new ReachableTarget(i, pos, best));
                }
            }

            targets.Sort((a, b) => CellPos.CompareCandidates(a.Distance, a.Cell, b.Distance, b.Cell));
            return new ReachabilityResult(board, distance, parent, via, targets);
        }
    }

    /// <summary>Outcome of <see cref="Reachability.Compute"/>.</summary>
    public sealed class ReachabilityResult
    {
        private readonly Board _board;
        private readonly int[] _distance;
        private readonly int[] _parent;
        private readonly int[] _via;
        private readonly bool[] _reachable;

        internal ReachabilityResult(Board board, int[] distance, int[] parent, int[] via, List<ReachableTarget> targets)
        {
            _board = board;
            _distance = distance;
            _parent = parent;
            _via = via;
            Targets = targets;
            _reachable = new bool[board.CellCount];
            foreach (ReachableTarget t in targets)
            {
                _reachable[t.Index] = true;
            }
        }

        /// <summary>Reachable targets ordered by (distance ↑, row ↑ from the bottom, column ↑) (FR-021).</summary>
        public IReadOnlyList<ReachableTarget> Targets { get; }

        public bool IsReachable(int index) => _reachable[index];

        /// <summary>BFS distance of an open cell from the nearest entry; -1 when not reached.</summary>
        public int OpenDistance(int index) => _distance[index];

        /// <summary>
        /// The walking route for a reachable target: the entry cell, the open cells in between, then the target itself.
        /// Used for <c>TileCleared.routeFromEntry</c>.
        /// </summary>
        public IReadOnlyList<CellPos> RouteTo(int targetIndex)
        {
            if (!_reachable[targetIndex])
            {
                throw new InvalidOperationException($"Cell {_board.PosOf(targetIndex)} is not reachable.");
            }

            var route = new List<CellPos>();
            int step = _via[targetIndex];
            while (step >= 0)
            {
                route.Add(_board.PosOf(step));
                step = _parent[step];
            }

            route.Reverse();
            route.Add(_board.PosOf(targetIndex));
            return route;
        }
    }
}
