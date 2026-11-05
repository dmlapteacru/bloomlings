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
    /// - <see cref="Revealed"/>, <see cref="Bridge"/> (the built site or -1) and <see cref="Status"/>;
    /// - <see cref="Veiled"/>: per cell, whether it is a hidden cell not revealed yet (null without hidden cells);
    /// - <see cref="Latched"/>: per latch, whether it is still closed (null without latches);
    /// - <see cref="Channel"/>: the chosen exit of the water channel or -1, and <see cref="SpringReached"/>;
    /// - <see cref="Shut"/>: per shutter, whether it is still closed (null without shutters).
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
            Channel = -1;
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
            Veiled = other.Veiled == null ? null : (bool[])other.Veiled.Clone();
            VeiledLeft = other.VeiledLeft;
            Latched = other.Latched == null ? null : (bool[])other.Latched.Clone();
            LatchedLeft = other.LatchedLeft;
            Channel = other.Channel;
            SpringReached = other.SpringReached;
            Shut = other.Shut == null ? null : (bool[])other.Shut.Clone();
            ShutLeft = other.ShutLeft;
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

        /// <summary>
        /// Per cell, true while it is a hidden cell not revealed yet (reference <c>state.veiled</c>, rules 17 §8 «Скрытые
        /// клетки»); null on a level without hidden cells. A function of the board (a hidden material cell that is not a
        /// target), so the signature leaves it out; <see cref="RoundRules.RevealCells"/> keeps it up to date.
        /// </summary>
        public bool[]? Veiled { get; internal set; }

        /// <summary>How many cells are still veiled (reference <c>state.veiledLeft</c>).</summary>
        public int VeiledLeft { get; internal set; }

        /// <summary>
        /// What a player may know about cell <paramref name="cell"/> (reference <c>known</c>): false only for a hidden
        /// cell not revealed yet, whose material is shown as a neutral «?».
        /// </summary>
        public bool Known(int cell) => Veiled == null || !Veiled[cell];

        /// <summary>
        /// Per latch, true while it is closed (reference <c>state.latched</c>, rules 17 §8 «Засов и жёлудь-ключ»); null on a
        /// level without latches. Without boosters a function of the heads and the board (a latch is open exactly when its
        /// column's head has passed it, or stands at it with its key reached), so the signature leaves it out;
        /// <see cref="RoundRules.OpenLatches"/> keeps it up to date and a restored snapshot recomputes it.
        /// </summary>
        public bool[]? Latched { get; internal set; }

        /// <summary>How many latches are still closed (reference <c>state.latchedLeft</c>).</summary>
        public int LatchedLeft { get; internal set; }

        /// <summary>
        /// The chosen exit of the water channel (rules 17 §8 «Drop Водный канал», 2026-11-05; reference
        /// <c>state.channel</c>), or -1 while none is chosen (always -1 without a channel). The chosen exit is an active
        /// entry; the extended signature lists it on a channel level.
        /// </summary>
        public int Channel { get; internal set; }

        /// <summary>
        /// Whether the channel's spring has been reached by an open path from an active entry (reference
        /// <c>state.springReached</c>): a function of the board and the choice, kept only to time
        /// <see cref="RoundEventType.ChannelReady"/>.
        /// </summary>
        public bool SpringReached { get; internal set; }

        /// <summary>
        /// Per shutter, true while it is closed (rules 17 §8 «Twig Деревянные створки», 2026-11-05; reference
        /// <c>state.shut</c>); null on a level without shutters. A function of the board (a shutter is open exactly when the
        /// work done of its material, its start cells minus its cells now, is at least its count), so the signature leaves
        /// it out; <see cref="RoundRules.OpenShutters"/> keeps it up to date and a restored snapshot recomputes it.
        /// </summary>
        public bool[]? Shut { get; internal set; }

        /// <summary>How many shutters are still closed (reference <c>state.shutLeft</c>).</summary>
        public int ShutLeft { get; internal set; }

        /// <summary>Whether cell <paramref name="cell"/> lies under a closed shutter (reference <c>shutAt</c>).</summary>
        public bool ShutAt(int cell) => Shut != null && Prepared.ShutCell![cell] >= 0 && Shut[Prepared.ShutCell[cell]];

        /// <summary>
        /// The count still shown on shutter <paramref name="shutter"/> (reference <c>shutterLeft</c>): its count minus the
        /// work of its material done so far, 0 once it is open.
        /// </summary>
        public int ShutterLeft(int shutter)
        {
            if (Shut == null || !Shut[shutter])
            {
                return 0;
            }

            int material = Prepared.ShutMaterial![shutter];
            int left = 0;
            foreach (sbyte c in Board)
            {
                if (c == material)
                {
                    left++;
                }
            }

            return Math.Max(0, Level.Shutters[shutter].Count - (Prepared.ShutStart![material] - left));
        }

        /// <summary>
        /// The depth of column <paramref name="column"/>'s first closed latch (reference <c>blockAt</c>): the pods at that
        /// index and behind it are held; <see cref="int.MaxValue"/> when no closed latch stands in the column.
        /// </summary>
        public int BlockAt(int column)
        {
            if (Latched == null || LatchedLeft == 0)
            {
                return int.MaxValue;
            }

            foreach (int i in Prepared.LatchColumns![column])
            {
                if (Latched[i])
                {
                    return Level.Latches[i].Depth;
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        /// The closed latch on top of column <paramref name="column"/> (its index in <see cref="RoundLevel.Latches"/>), or
        /// -1: the column's head stands at its first closed latch, so the column has no top pod.
        /// </summary>
        public int TopLatchOf(int column)
        {
            if (Latched == null || LatchedLeft == 0)
            {
                return -1;
            }

            foreach (int i in Prepared.LatchColumns![column])
            {
                if (Latched[i])
                {
                    return Level.Latches[i].Depth == Heads[column] ? i : -1;
                }
            }

            return -1;
        }

        /// <summary>The columns whose top is a closed latch (reference <c>blockedColumns</c>; 17 §8: 3 columns at most 1, 4 at most 2).</summary>
        public List<int> BlockedColumns()
        {
            var result = new List<int>();
            for (int c = 0; c < Columns.Length; c++)
            {
                if (TopLatchOf(c) >= 0)
                {
                    result.Add(c);
                }
            }

            return result;
        }

        /// <summary>A copy that shares nothing mutable (the cached reach is immutable and shared).</summary>
        public RoundState Clone() => new RoundState(this);

        /// <summary>The breadth-first reach from the entries, cached until the board changes (reference <c>reach</c>).</summary>
        public RoundReach Reach() => _reach ??= RoundReach.Of(this);

        /// <summary>Drops the cached reach; call after any change of <see cref="Board"/>.</summary>
        internal void BoardChanged() => _reach = null;

        /// <summary>
        /// The pod on top of column <paramref name="column"/>, or -1 when the column is empty or its head stands at a closed
        /// latch (reference <c>topPods</c>: a pod behind a closed latch is no top pod).
        /// </summary>
        public int TopOf(int column)
        {
            List<int> stack = Columns[column];
            int head = Heads[column];
            return head < stack.Count && head < BlockAt(column) ? stack[head] : -1;
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
        /// active pods by age, revealed as 0/1 digits, the bridge, the chosen channel exit (only on a channel level) and the
        /// status, joined by '|'.
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
            if (Level.Channel != null)
            {
                // The chosen channel exit, only on a channel level (17 §8, 2026-11-05).
                sb.Append('|').Append(Channel.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

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
    /// The reach of a board (reference <c>reach</c>): breadth-first distances from the entries, then the chosen channel
    /// exit, over open cells (a source is 1), each open cell's parent towards the entry, and the targets: material cells touching a reached cell
    /// (or an entry), each at the nearest such neighbour's distance + 1 (an entry cell at 1), sorted by distance, then by
    /// cell index; a cell under a closed shutter is no target. Immutable once built.
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
            Array.Fill(distance, -1);
            Array.Fill(parent, -1);
            int[] queue = Scratch(n);

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

            // The chosen channel exit: an active entry after the level's entries (17 §8 «Drop Водный канал»).
            RoundChannel? channel = state.Level.Channel;
            if (channel != null && state.Channel >= 0)
            {
                int exit = channel.Exits[state.Channel].Cell;
                if (board[exit] == RoundLevel.Open && distance[exit] < 0)
                {
                    distance[exit] = 1;
                    queue[tail++] = exit;
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

            // Each target is keyed by distance × n + index: sorting the keys gives distance order, ties by index.
            int[] keys = queue;
            int count = 0;
            var targetDistance = new int[n];
            bool[]? shut = state.Shut;
            int[]? shutCell = meta.ShutCell;
            for (int i = 0; i < n; i++)
            {
                if (board[i] < 0)
                {
                    continue;
                }

                // A cell under a closed shutter is no target (17 §8 «Twig Деревянные створки»).
                if (shut != null && shutCell![i] >= 0 && shut[shutCell[i]])
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
                    keys[count++] = best * n + i;
                    targetDistance[i] = best;
                }
            }

            Array.Sort(keys, 0, count);
            var sorted = new int[count];
            for (int k = 0; k < count; k++)
            {
                sorted[k] = keys[k] % n;
            }

            return new RoundReach(sorted, distance, targetDistance, parent);
        }

        // A reusable work array per thread (the breadth-first queue, then the target keys); never kept in a result.
        [ThreadStatic]
        private static int[]? _scratch;

        private static int[] Scratch(int n)
        {
            if (_scratch == null || _scratch.Length < n)
            {
                _scratch = new int[n];
            }

            return _scratch;
        }
    }
}
