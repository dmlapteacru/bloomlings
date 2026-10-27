using System;
using System.Collections.Generic;
using System.Text;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The status of a round attempt, as the reference's <c>status</c> (research R3).</summary>
    public enum RoundStatus
    {
        Playing,
        Won,
        Jammed,
        Stuck,
    }

    /// <summary>
    /// The rules state of a round attempt (spec 006 data model RoundState), the port of the reference's state object in
    /// <c>rules.cjs</c>:
    /// - <see cref="Board"/>: per cell, a material index, <see cref="RoundLevel.Open"/> or <see cref="RoundLevel.Wall"/>;
    /// - <see cref="Remaining"/>: per pod, its unsent count (a courier takes one at its departure);
    /// - <see cref="Columns"/> and <see cref="Heads"/>: per column, its pods (top first) and the index of its top pod;
    /// - <see cref="Slots"/>: per place, a pod or -1;
    /// - <see cref="Ages"/>: per pod, its commit order (<see cref="Serial"/> counts commits);
    /// - <see cref="Revealed"/>, <see cref="Bridge"/> (the built site or -1) and <see cref="Status"/>.
    /// The columns start as a copy of the level's tray. Only Return and Shuffle change them; without boosters they equal
    /// the tray, as the reference's fixed tray (research R10).
    /// </summary>
    public sealed class RoundState
    {
        private RoundReach? _reach;

        internal RoundState(RoundLevel level, int capacity)
        {
            Level = level;
            Prepared = level.Prepare();
            Board = (sbyte[])Prepared.Board.Clone();
            int pods = level.Pods.Count;
            Remaining = new int[pods];
            Revealed = new bool[pods];
            for (int p = 0; p < pods; p++)
            {
                Remaining[p] = level.Pods[p].Count;
                Revealed[p] = level.Pods[p].Mystery == RoundMystery.None;
            }

            Columns = new List<int>[level.Tray.Count];
            ColumnOf = new int[pods];
            for (int c = 0; c < Columns.Length; c++)
            {
                Columns[c] = new List<int>(level.Tray[c]);
                foreach (int p in level.Tray[c])
                {
                    if (p >= 0 && p < pods)
                    {
                        ColumnOf[p] = c;
                    }
                }
            }

            Heads = new int[Columns.Length];
            Slots = new int[capacity];
            for (int s = 0; s < capacity; s++)
            {
                Slots[s] = -1;
            }

            Ages = new int[pods];
            Bridge = -1;
            Status = RoundStatus.Playing;
        }

        private RoundState(RoundState other)
        {
            Level = other.Level;
            Prepared = other.Prepared;
            Board = (sbyte[])other.Board.Clone();
            Remaining = (int[])other.Remaining.Clone();
            Columns = new List<int>[other.Columns.Length];
            for (int c = 0; c < Columns.Length; c++)
            {
                Columns[c] = new List<int>(other.Columns[c]);
            }

            ColumnOf = (int[])other.ColumnOf.Clone();
            ColumnsChanged = other.ColumnsChanged;
            Heads = (int[])other.Heads.Clone();
            Slots = (int[])other.Slots.Clone();
            Ages = (int[])other.Ages.Clone();
            Revealed = (bool[])other.Revealed.Clone();
            Serial = other.Serial;
            Bridge = other.Bridge;
            Status = other.Status;
            _reach = other._reach;
        }

        public RoundLevel Level { get; }

        public RoundPrepared Prepared { get; }

        public sbyte[] Board { get; }

        public int[] Remaining { get; }

        /// <summary>Per column, its pods top first (a mutable copy of the tray; consumed pods stay before the head).</summary>
        public List<int>[] Columns { get; }

        /// <summary>Per pod, the column it belongs to (Return sends a pod back there).</summary>
        public int[] ColumnOf { get; }

        /// <summary>True once a booster changed the columns; the extended signature then lists them.</summary>
        public bool ColumnsChanged { get; internal set; }

        public int[] Heads { get; }

        /// <summary>Per place, the pod in it or -1. Extra Slot replaces the array with a longer one.</summary>
        public int[] Slots { get; internal set; }

        public int[] Ages { get; }

        public bool[] Revealed { get; }

        public int Serial { get; internal set; }

        public int Bridge { get; internal set; }

        public RoundStatus Status { get; internal set; }

        /// <summary>A copy that shares nothing mutable (the cached reach is immutable and shared).</summary>
        public RoundState Clone() => new RoundState(this);

        /// <summary>The breadth-first reach from the entries, cached until the board changes (reference <c>reach</c>).</summary>
        public RoundReach Reach() => _reach ??= RoundReach.Of(this);

        /// <summary>Drops the cached reach; call after any change of <see cref="Board"/>.</summary>
        internal void BoardChanged() => _reach = null;

        /// <summary>The pod on top of column <paramref name="column"/>, or -1 when the column is empty.</summary>
        public int TopOf(int column)
        {
            List<int> stack = Columns[column];
            int head = Heads[column];
            return head < stack.Count ? stack[head] : -1;
        }

        /// <summary>True when every column is empty (the reference's <c>queueEmpty</c>).</summary>
        public bool QueueEmpty
        {
            get
            {
                for (int c = 0; c < Columns.Length; c++)
                {
                    if (Heads[c] < Columns[c].Count)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public int FreeSlots
        {
            get
            {
                int free = 0;
                foreach (int p in Slots)
                {
                    if (p < 0)
                    {
                        free++;
                    }
                }

                return free;
            }
        }

        internal int SlotOf(int pod) => Array.IndexOf(Slots, pod);

        /// <summary>The occupied places' pods, oldest commit first.</summary>
        public int[] ActiveByAge()
        {
            var active = new List<int>(Slots.Length);
            foreach (int p in Slots)
            {
                if (p >= 0)
                {
                    active.Add(p);
                }
            }

            int[] result = active.ToArray();
            int[] ages = Ages;
            Array.Sort(result, (a, b) => ages[a].CompareTo(ages[b]));
            return result;
        }

        /// <summary>
        /// The reference's <c>signature(state)</c>, exactly: board letters with '.' and '#', remaining, heads, slots, the
        /// active pods by age, revealed as 0/1 digits, the bridge and the status, joined by '|'.
        /// </summary>
        public string Signature()
        {
            var sb = new StringBuilder(Board.Length + 256);
            AppendSignature(sb);
            return sb.ToString();
        }

        internal void AppendSignature(StringBuilder sb)
        {
            foreach (sbyte c in Board)
            {
                sb.Append(c == RoundLevel.Open ? '.' : c == RoundLevel.Wall ? '#' : RoundMaterial.Codes[c]);
            }

            sb.Append('|');
            AppendJoined(sb, Remaining);
            sb.Append('|');
            AppendJoined(sb, Heads);
            sb.Append('|');
            AppendJoined(sb, Slots);
            sb.Append('|');
            AppendJoined(sb, ActiveByAge());
            sb.Append('|');
            foreach (bool r in Revealed)
            {
                sb.Append(r ? '1' : '0');
            }

            sb.Append('|').Append(Bridge.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append('|').Append(StatusText(Status));
        }

        /// <summary>The reference's status words: Playing, Won, Jammed, Stuck.</summary>
        public static string StatusText(RoundStatus status) => status switch
        {
            RoundStatus.Won => "Won",
            RoundStatus.Jammed => "Jammed",
            RoundStatus.Stuck => "Stuck",
            _ => "Playing",
        };

        internal static void AppendJoined(StringBuilder sb, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(values[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// The reach of a board (reference <c>reach</c>): breadth-first distances from the entries over open cells (an
    /// entry is 1), each open cell's parent towards the entry, and the targets: material cells touching a reached cell
    /// (or an entry), each at the nearest such neighbour's distance + 1 (an entry cell at 1), sorted by distance, then by
    /// cell index. Immutable once built.
    /// </summary>
    public sealed class RoundReach
    {
        private RoundReach(int[] targets, int[] distance, int[] targetDistance, int[] parent)
        {
            Targets = targets;
            Distance = distance;
            TargetDistance = targetDistance;
            Parent = parent;
        }

        /// <summary>The reachable material cells in work order (the dispatch frontier).</summary>
        public IReadOnlyList<int> Targets { get; }

        /// <summary>Per cell, its breadth-first distance (entries 1), or -1 when not reached.</summary>
        public IReadOnlyList<int> Distance { get; }

        public IReadOnlyList<int> TargetDistance { get; }

        public IReadOnlyList<int> Parent { get; }

        internal int[] TargetArray => (int[])Targets;

        internal static RoundReach Of(RoundState state)
        {
            sbyte[] board = state.Board;
            RoundPrepared meta = state.Prepared;
            int[] neighbours = meta.Neighbours;
            int n = board.Length;
            var distance = new int[n];
            var parent = new int[n];
            var queue = new int[n];
            for (int i = 0; i < n; i++)
            {
                distance[i] = -1;
                parent[i] = -1;
            }

            int head = 0;
            int tail = 0;
            foreach (int entry in meta.Entries)
            {
                if (board[entry] == RoundLevel.Open && distance[entry] < 0)
                {
                    distance[entry] = 1;
                    queue[tail++] = entry;
                }
            }

            while (head < tail)
            {
                int i = queue[head++];
                int next = distance[i] + 1;
                for (int d = 0; d < 4; d++)
                {
                    int j = neighbours[i * 4 + d];
                    if (j >= 0 && board[j] == RoundLevel.Open && distance[j] < 0)
                    {
                        distance[j] = next;
                        parent[j] = i;
                        queue[tail++] = j;
                    }
                }
            }

            var targets = new List<int>();
            var targetDistance = new int[n];
            for (int i = 0; i < n; i++)
            {
                if (board[i] < 0)
                {
                    continue;
                }

                int best = meta.IsEntry[i] ? 1 : int.MaxValue;
                for (int d = 0; d < 4; d++)
                {
                    int j = neighbours[i * 4 + d];
                    if (j >= 0 && distance[j] > 0)
                    {
                        best = Math.Min(best, distance[j] + 1);
                    }
                }

                if (best < int.MaxValue)
                {
                    targets.Add(i);
                    targetDistance[i] = best;
                }
            }

            // Targets were collected in index order; the comparison breaks distance ties by index, so the order is total.
            int[] sorted = targets.ToArray();
            Array.Sort(sorted, (a, b) => targetDistance[a] != targetDistance[b] ? targetDistance[a].CompareTo(targetDistance[b]) : a.CompareTo(b));
            return new RoundReach(sorted, distance, targetDistance, parent);
        }
    }
}
