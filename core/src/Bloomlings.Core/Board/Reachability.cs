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
            byte[] kind = board.Kinds;
            int[] neighbours = board.Neighbours;

            // Open distances fit in a short: a board has at most 22 × 28 cells.
            var distance = new short[n];
            for (int i = 0; i < n; i++)
            {
                distance[i] = -1;
            }

            // A cell's parent is read only on a route, which starts at an entry cell (parent -1) and runs over cells
            // the search reached (parent set below).
            int[]? parent = routes ? new int[n] : null;

            // Multi-source BFS: every walkable entry cell starts at distance 1, in definition order.
            Scratch scratch = Scratch.Begin(n);
            int[] queue = scratch.Queue;
            int head = 0;
            int tail = 0;
            foreach (int index in board.EntryIndexes)
            {
                if (kind[index] == Board.OpenKind && distance[index] < 0)
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
                short next = (short)(distance[current] + 1);
                int at = current * CellPos.NeighbourCount;
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    int nextIndex = neighbours[at + dir];
                    if (nextIndex < 0 || distance[nextIndex] >= 0 || kind[nextIndex] != Board.OpenKind)
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
            int[] foundDistance = scratch.Values;
            bool[] entryCell = board.EntryCells;
            int count = 0;
            int maxDistance = 0;
            for (int i = 0; i < n; i++)
            {
                if (kind[i] != Board.TargetKind)
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

            var sorted = new int[count];
            for (int k = 0; k < count; k++)
            {
                sorted[start[foundDistance[k]]++] = ReachabilityResult.Pack(found[k], foundDistance[k]);
            }

            return new ReachabilityResult(board, distance, parent, via, sorted);
        }

        /// <summary>
        /// The reachability of <paramref name="board"/> without routes, from <paramref name="before"/>, the reachability
        /// of the same board before the first <paramref name="count"/> cells of <paramref name="opened"/> became open
        /// ground (the only change of kind play makes). It equals <see cref="Compute(Board, bool)"/> without routes: the
        /// same open distances, targets and order.
        /// <para>
        /// Opening cells only adds walkable cells, so distances only shrink. Each opened cell takes its distance from its
        /// reached neighbours (1 at an entry), and every cell whose distance shrinks hands it on to its open neighbours
        /// until no distance shrinks: every distance is then the length of a shortest route. Only targets next to a cell
        /// whose distance changed can change; the others keep their place, and the changed ones are merged in by the
        /// same order (distance, then row and column, which is the cell index).
        /// </para>
        /// <para>
        /// With <paramref name="inPlace"/> (the board's own result, which nothing else holds) <paramref name="before"/>
        /// itself is updated and returned; otherwise it stays as it is and a new result is returned.
        /// </para>
        /// </summary>
        internal static ReachabilityResult Update(Board board, ReachabilityResult before, int[] opened, int count, bool inPlace)
        {
            byte[] kind = board.Kinds;
            int[] neighbours = board.Neighbours;
            bool[] entryCell = board.EntryCells;
            short[] distance = inPlace ? before.Distances : (short[])before.Distances.Clone();
            Scratch scratch = Scratch.Begin(board.CellCount);

            // Each opened cell takes its distance from its reached neighbours, or 1 at an entry.
            for (int k = 0; k < count; k++)
            {
                int cell = opened[k];
                if (kind[cell] == Board.OpenKind)
                {
                    int best = entryCell[cell] ? 1 : MeasureFrom(cell, neighbours, distance, int.MaxValue);
                    if (best != int.MaxValue && (distance[cell] < 0 || best < distance[cell]))
                    {
                        distance[cell] = (short)best;
                        scratch.Changed(cell);
                    }
                }
            }

            // Every cell whose distance shrank hands it on to its open neighbours, until none shrinks.
            while (scratch.TryDequeue(out int current))
            {
                short reach = (short)(distance[current] + 1);
                int at = current * CellPos.NeighbourCount;
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    int next = neighbours[at + dir];
                    if (next >= 0 && kind[next] == Board.OpenKind && (distance[next] < 0 || distance[next] > reach))
                    {
                        distance[next] = reach;
                        scratch.Changed(next);
                    }
                }
            }

            // The targets next to a changed cell are measured again (as Compute does) and sorted by (distance, index),
            // which is the order of their packed values.
            int[] changed = scratch.ChangedCells;
            int changedCount = scratch.ChangedCount;
            int[] addedTargets = scratch.Queue;
            int added = 0;
            for (int k = 0; k < changedCount; k++)
            {
                int at = changed[k] * CellPos.NeighbourCount;
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    int target = neighbours[at + dir];
                    if (target >= 0 && kind[target] == Board.TargetKind && scratch.MarkAffected(target))
                    {
                        int best = MeasureFrom(target, neighbours, distance, entryCell[target] ? 1 : int.MaxValue);
                        if (best == int.MaxValue)
                        {
                            continue;
                        }

                        // Insertion sort: few targets change at a time.
                        int packed = ReachabilityResult.Pack(target, best);
                        int j = added++;
                        while (j > 0 && addedTargets[j - 1] > packed)
                        {
                            addedTargets[j] = addedTargets[j - 1];
                            j--;
                        }

                        addedTargets[j] = packed;
                    }
                }
            }

            // The other targets keep their distance and order; the measured ones are merged in. The opened cells were
            // marked too: an old target that is no longer one opened, as nothing else changes a kind.
            for (int k = 0; k < count; k++)
            {
                scratch.MarkAffected(opened[k]);
            }

            int[] old = before.Packed;
            int[] buffer = scratch.Values;
            int m = 0;
            int a = 0;
            for (int k = 0; k < old.Length; k++)
            {
                int keep = old[k];
                if (scratch.IsAffected(ReachabilityResult.CellOf(keep)))
                {
                    continue;
                }

                while (a < added && addedTargets[a] < keep)
                {
                    buffer[m++] = addedTargets[a++];
                }

                buffer[m++] = keep;
            }

            while (a < added)
            {
                buffer[m++] = addedTargets[a++];
            }

            var merged = new int[m];
            Array.Copy(buffer, merged, m);

            if (inPlace)
            {
                before.Replace(distance, merged);
                return before;
            }

            return new ReachabilityResult(board, distance, null, null, merged);
        }

        /// <summary>The smallest of <paramref name="best"/> and a reached neighbour's distance + 1 (the target rule of Compute).</summary>
        private static int MeasureFrom(int cell, int[] neighbours, short[] distance, int best)
        {
            int at = cell * CellPos.NeighbourCount;
            for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
            {
                int next = neighbours[at + dir];
                if (next >= 0)
                {
                    int d = distance[next];
                    if (d > 0 && d + 1 < best)
                    {
                        best = d + 1;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Per-thread work arrays, kept between calls (<see cref="Compute(Board, bool)"/> and <see cref="Update"/> run
        /// to the end before another starts on the same thread): a queue, a list of values, the cells whose distance
        /// changed with the queue of those to hand it on (a ring in which a cell is at most once), and stamps that mark
        /// cells without clearing.
        /// </summary>
        private sealed class Scratch
        {
            [ThreadStatic]
            private static Scratch? s_current;

            private int[] _ring = new int[1];
            private int[] _queuedStamp = new int[0];
            private int[] _changedStamp = new int[0];
            private int[] _affectedStamp = new int[0];
            private int[] _changed = new int[0];
            private int _stamp;
            private int _head;
            private int _tail;
            private int _changedCount;

            /// <summary>A work array of one entry per cell.</summary>
            public int[] Queue { get; private set; } = new int[0];

            /// <summary>Another work array of one entry per cell.</summary>
            public int[] Values { get; private set; } = new int[0];

            public int[] ChangedCells => _changed;

            public int ChangedCount => _changedCount;

            /// <summary>The thread's work arrays, cleared for a board of <paramref name="cells"/> cells.</summary>
            public static Scratch Begin(int cells)
            {
                Scratch scratch = s_current ??= new Scratch();
                if (scratch._changed.Length < cells)
                {
                    scratch.Queue = new int[cells];
                    scratch.Values = new int[cells];
                    scratch._ring = new int[cells + 1];
                    scratch._queuedStamp = new int[cells];
                    scratch._changedStamp = new int[cells];
                    scratch._affectedStamp = new int[cells];
                    scratch._changed = new int[cells];
                    scratch._stamp = 0;
                }

                if (scratch._stamp == int.MaxValue)
                {
                    Array.Clear(scratch._queuedStamp, 0, scratch._queuedStamp.Length);
                    Array.Clear(scratch._changedStamp, 0, scratch._changedStamp.Length);
                    Array.Clear(scratch._affectedStamp, 0, scratch._affectedStamp.Length);
                    scratch._stamp = 0;
                }

                scratch._stamp++;
                scratch._head = 0;
                scratch._tail = 0;
                scratch._changedCount = 0;
                return scratch;
            }

            /// <summary>Records that a cell's distance changed and queues it to hand its distance on.</summary>
            public void Changed(int cell)
            {
                if (_changedStamp[cell] != _stamp)
                {
                    _changedStamp[cell] = _stamp;
                    _changed[_changedCount++] = cell;
                }

                if (_queuedStamp[cell] != _stamp)
                {
                    _queuedStamp[cell] = _stamp;
                    _ring[_tail] = cell;
                    if (++_tail == _ring.Length)
                    {
                        _tail = 0;
                    }
                }
            }

            public bool TryDequeue(out int cell)
            {
                if (_head == _tail)
                {
                    cell = -1;
                    return false;
                }

                cell = _ring[_head];
                if (++_head == _ring.Length)
                {
                    _head = 0;
                }

                // Out of the queue: a later change queues it again (stamp 0 is never current).
                _queuedStamp[cell] = 0;
                return true;
            }

            /// <summary>Marks a target to measure again; false when it already was.</summary>
            public bool MarkAffected(int cell)
            {
                if (_affectedStamp[cell] == _stamp)
                {
                    return false;
                }

                _affectedStamp[cell] = _stamp;
                return true;
            }

            public bool IsAffected(int cell) => _affectedStamp[cell] == _stamp;
        }
    }

    /// <summary>
    /// Outcome of <see cref="Reachability.Compute(Board)"/>. It never changes after it is computed (a board updates the
    /// one it keeps for a search in place only while nothing else holds it).
    /// </summary>
    public sealed class ReachabilityResult
    {
        private readonly Board _board;
        private short[] _distance;
        private int[]? _parent;
        private int[]? _via;
        private int[] _packed;
        private ReachableTarget[]? _targets;
        private bool[]? _reachable;

        internal ReachabilityResult(Board board, short[] distance, int[]? parent, int[]? via, int[] packed)
        {
            _board = board;
            _distance = distance;
            _parent = parent;
            _via = via;
            _packed = packed;
        }

        /// <summary>Reachable targets ordered by (distance ↑, row ↑ from the bottom, column ↑) (FR-021).</summary>
        public IReadOnlyList<ReachableTarget> Targets => _targets ??= Unpack();

        /// <summary>
        /// The targets in the same order, each as one int (<see cref="Pack"/>), for the rules' inner loops. A board has
        /// fewer than 2^16 cells and distances below 2^15, so the packed values sort exactly as (distance, index), which is
        /// (distance, row, column).
        /// </summary>
        internal int[] Packed => _packed;

        /// <summary>Whether the routes are known (<see cref="RouteTo"/>); a search's reachability leaves them out.</summary>
        internal bool HasRoutes => _via != null;

        /// <summary>The open distances by cell (<see cref="OpenDistance"/>).</summary>
        internal short[] Distances => _distance;

        internal static int Pack(int cell, int distance) => (distance << 16) | cell;

        internal static int CellOf(int packed) => packed & 0xFFFF;

        internal static int DistanceOf(int packed) => packed >> 16;

        /// <summary>An in-place update (<see cref="Reachability.Update"/>): new distances and targets, no routes.</summary>
        internal void Replace(short[] distance, int[] packed)
        {
            _distance = distance;
            _packed = packed;
            _parent = null;
            _via = null;
            _targets = null;
            _reachable = null;
        }

        private ReachableTarget[] Unpack()
        {
            var targets = new ReachableTarget[_packed.Length];
            CellPos[] positions = _board.Positions;
            for (int k = 0; k < targets.Length; k++)
            {
                int cell = CellOf(_packed[k]);
                targets[k] = new ReachableTarget(cell, positions[cell], DistanceOf(_packed[k]));
            }

            return targets;
        }

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
                foreach (int packed in _packed)
                {
                    reachable[CellOf(packed)] = true;
                }

                _reachable = reachable;
            }

            return _reachable;
        }
    }
}
