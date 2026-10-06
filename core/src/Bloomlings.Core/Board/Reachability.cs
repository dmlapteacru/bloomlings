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

            return Compute(board, routes: true);
        }

        /// <summary>
        /// The reachability of <paramref name="board"/>. Without <paramref name="routes"/> (a search, which builds no
        /// events) the walking routes are left out: the targets, their order and distances are the same.
        /// </summary>
        internal static ReachabilityResult Compute(Board board, bool routes)
        {
            int n = board.CellCount;
            CellKind[] kind = board.Kinds;
            int[] neighbours = board.Neighbours;
            var distance = new int[n];
            for (int i = 0; i < n; i++)
            {
                distance[i] = -1;
            }

            // A cell's parent is read only on a route, which starts at an entry cell (parent -1) and runs over cells
            // the search reached (parent set below).
            int[]? parent = routes ? new int[n] : null;

            // Multi-source BFS: every walkable entry cell starts at distance 1, in definition order.
            var queue = new int[n];
            int head = 0;
            int tail = 0;
            foreach (int index in board.EntryIndexes)
            {
                if (kind[index] == CellKind.Open && distance[index] < 0)
                {
                    distance[index] = 1;
                    if (parent != null)
                    {
                        parent[index] = -1;
                    }

                    queue[tail++] = index;
                }
            }

            // The neighbours come in the fixed order of CellPos.TryGetNeighbour (down, left, right, up).
            while (head < tail)
            {
                int current = queue[head++];
                int next = distance[current] + 1;
                int at = current * CellPos.NeighbourCount;
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    int nextIndex = neighbours[at + dir];
                    if (nextIndex < 0 || distance[nextIndex] >= 0 || kind[nextIndex] != CellKind.Open)
                    {
                        continue;
                    }

                    distance[nextIndex] = next;
                    if (parent != null)
                    {
                        parent[nextIndex] = current;
                    }

                    queue[tail++] = nextIndex;
                }
            }

            // Targets: an entry cell, or adjacent to a reached open cell. The scan runs row by row from the bottom and
            // column by column, so the targets of one distance come in the FR-021 order of row, then column. A
            // target's via (the cell its route enters from) is read only for a reachable target, which sets it here.
            int[]? via = routes ? new int[n] : null;
            int[] found = queue;
            var foundDistance = new int[n];
            bool[] entryCell = board.EntryCells;
            int count = 0;
            int maxDistance = 0;
            for (int i = 0; i < n; i++)
            {
                if (kind[i] != CellKind.Target)
                {
                    continue;
                }

                int best = entryCell[i] ? 1 : int.MaxValue;
                int bestVia = -1;
                int at = i * CellPos.NeighbourCount;
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    int nextIndex = neighbours[at + dir];
                    if (nextIndex < 0)
                    {
                        continue;
                    }

                    int d = distance[nextIndex];
                    if (d > 0 && d + 1 < best)
                    {
                        best = d + 1;
                        bestVia = nextIndex;
                    }
                }

                if (best != int.MaxValue)
                {
                    if (via != null)
                    {
                        via[i] = bestVia;
                    }

                    found[count] = i;
                    foundDistance[count] = best;
                    count++;
                    if (best > maxDistance)
                    {
                        maxDistance = best;
                    }
                }
            }

            // A stable counting sort by distance keeps the row and column order within each distance: the same total
            // order as CellPos.CompareCandidates (distance, row, column), which has no ties between distinct cells.
            var start = new int[maxDistance + 2];
            for (int k = 0; k < count; k++)
            {
                start[foundDistance[k] + 1]++;
            }

            for (int d = 1; d < start.Length; d++)
            {
                start[d] += start[d - 1];
            }

            CellPos[] positions = board.Positions;
            var sorted = new ReachableTarget[count];
            for (int k = 0; k < count; k++)
            {
                int i = found[k];
                sorted[start[foundDistance[k]]++] = new ReachableTarget(i, positions[i], foundDistance[k]);
            }

            return new ReachabilityResult(board, distance, parent, via, sorted);
        }
    }

    /// <summary>Outcome of <see cref="Reachability.Compute"/>. It never changes after it is computed.</summary>
    public sealed class ReachabilityResult
    {
        private readonly Board _board;
        private readonly int[] _distance;
        private readonly int[]? _parent;
        private readonly int[]? _via;
        private readonly ReachableTarget[] _targets;
        private bool[]? _reachable;

        internal ReachabilityResult(Board board, int[] distance, int[]? parent, int[]? via, ReachableTarget[] targets)
        {
            _board = board;
            _distance = distance;
            _parent = parent;
            _via = via;
            _targets = targets;
        }

        /// <summary>Reachable targets ordered by (distance ↑, row ↑ from the bottom, column ↑) (FR-021).</summary>
        public IReadOnlyList<ReachableTarget> Targets => _targets;

        /// <summary><see cref="Targets"/> as an array, for the rules' inner loops (callers never change it).</summary>
        internal ReachableTarget[] TargetArray => _targets;

        /// <summary>Whether the routes are known (<see cref="RouteTo"/>); a search's reachability leaves them out.</summary>
        internal bool HasRoutes => _via != null;

        public bool IsReachable(int index) => Reachable()[index];

        /// <summary>BFS distance of an open cell from the nearest entry; -1 when not reached.</summary>
        public int OpenDistance(int index) => _distance[index];

        /// <summary>
        /// The walking route for a reachable target: the entry cell, the open cells in between, then the target itself.
        /// Used for <c>TileCleared.routeFromEntry</c>.
        /// </summary>
        public IReadOnlyList<CellPos> RouteTo(int targetIndex)
        {
            if (!Reachable()[targetIndex])
            {
                throw new InvalidOperationException($"Cell {_board.PosOf(targetIndex)} is not reachable.");
            }

            if (_via == null || _parent == null)
            {
                throw new InvalidOperationException("This reachability was computed without routes.");
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

        private bool[] Reachable()
        {
            if (_reachable == null)
            {
                var reachable = new bool[_board.CellCount];
                foreach (ReachableTarget t in _targets)
                {
                    reachable[t.Index] = true;
                }

                _reachable = reachable;
            }

            return _reachable;
        }
    }
}
