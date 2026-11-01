using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// One walker of a round level (a courier of the live timeline, spec 006 FR-042, FR-048): it leaves the gate at
    /// <see cref="Start"/>, reaches <see cref="Cell"/> at <see cref="Arrive"/>, acts until the rules clear the cell at
    /// <see cref="Clear"/> and is back home at <see cref="Done"/>, walking <see cref="Route"/> (the rules' cell indices, the
    /// gate first, the target last) out and back. Times are the host's display clock in µs.
    /// </summary>
    public readonly struct RoundWalk
    {
        public RoundWalk(int id, int pod, int material, int cell, long start, long arrive, long clear, long done, IReadOnlyList<int> route)
        {
            Id = id;
            Pod = pod;
            Material = material;
            Cell = cell;
            Start = start;
            Arrive = arrive;
            Clear = clear;
            Done = done;
            Route = route;
        }

        /// <summary>The courier's id (the timeline's).</summary>
        public int Id { get; }

        /// <summary>The pod it works for.</summary>
        public int Pod { get; }

        /// <summary>Its pod's material (<see cref="RoundMaterial.Index"/>): the cube it eats, and its tint.</summary>
        public int Material { get; }

        public int Cell { get; }

        public long Start { get; }

        public long Arrive { get; }

        public long Clear { get; }

        public long Done { get; }

        public IReadOnlyList<int> Route { get; }
    }

    /// <summary>Where a link tube is drawn: between two pods in the columns, or between their places.</summary>
    public enum RoundTubePlace
    {
        Columns,
        Places,
    }

    /// <summary>
    /// One thing a column shows in its queue (spec 006 FR-068, rules 17 §8 «Засов и жёлудь-ключ»): a pod, or a latch standing
    /// at its place among the pods (<see cref="IsLatch"/>), at display row <see cref="Row"/> (0 = the top).
    /// </summary>
    public readonly struct RoundQueueItem
    {
        public RoundQueueItem(bool isLatch, int id, int depth, int row)
        {
            IsLatch = isLatch;
            Id = id;
            Depth = depth;
            Row = row;
        }

        /// <summary>A latch (its index in <see cref="RoundLevel.Latches"/> is <see cref="Id"/>), else a pod.</summary>
        public bool IsLatch { get; }

        /// <summary>The pod, or the latch's index.</summary>
        public int Id { get; }

        /// <summary>A pod's depth in its column (0 = the top pod, the one that takes the tap); a latch's: the pods before it.</summary>
        public int Depth { get; }

        /// <summary>The row the item is drawn in: its depth plus the latches shown before it.</summary>
        public int Row { get; }
    }

    /// <summary>A link tube to draw (spec 006 FR-043): the linked pods <see cref="First"/> and <see cref="Second"/> and where they stand.</summary>
    public readonly struct RoundTube
    {
        public RoundTube(int first, int second, RoundTubePlace place)
        {
            First = first;
            Second = second;
            Place = place;
        }

        public int First { get; }

        public int Second { get; }

        public RoundTubePlace Place { get; }
    }

    /// <summary>
    /// What a round level shows each frame (spec 006 T043, research R8, contracts/presentation.md), shared by both builds:
    /// it reads the live timeline's events and state, never changes them, and keeps for the painters
    /// <list type="bullet">
    /// <item><description>the board as shown: each cell's material until its <c>clear</c> (or Bloom Burst) moment, and that
    /// moment for the cube's clear animation (FR-042); the built bridge; each hidden cell veiled until its
    /// <c>cellReveal</c> moment, and that moment for its pop (17 §8 «Скрытые клетки», FR-066);</description></item>
    /// <item><description>the walkers: every courier from its <c>depart</c> (which carries its whole plan) to its
    /// <c>return</c>, with its route and its four times;</description></item>
    /// <item><description>the places and the columns as the rules hold them, with when each pod took its place, when a
    /// finished pod left one (its puff) and when a hidden pod revealed (its glow, FR-045);</description></item>
    /// <item><description>the link tubes between linked pods in the columns and in the places (FR-043);</description></item>
    /// <item><description>the latches (17 §8 «Засов и жёлудь-ключ», FR-068): each closed until its <c>latchOpen</c> moment, and
    /// that moment for its opening; the columns' queues with the latches at their places (<see cref="ColumnItems"/>).</description></item>
    /// </list>
    /// Its clock is the host's display clock in µs (<see cref="Now"/>): the model time plus the time that passed while
    /// nothing was pending (the timeline stands still then, <see cref="RoundReplay"/>). The host passes that offset with
    /// each batch of events, so an event's display time is its model time plus the offset. Engine-free; presentation
    /// only: the same events always give the same picture, whatever the display speed.
    /// </summary>
    public sealed class RoundAnimator
    {
        /// <summary>How long a committed pod flies from its column to its place (display µs).</summary>
        public const long FlightUs = 280_000;

        /// <summary>How long a finished pod puffs off its place (display µs).</summary>
        public const long ExitUs = 300_000;

        /// <summary>How long a cube cleared by Bloom Burst puffs away (display µs).</summary>
        public const long BurstUs = 400_000;

        /// <summary>How long the board keeps moving after the last event: the cleared cubes' restore effects (display µs).</summary>
        public const long SettleUs = 1_000_000;

        /// <summary>How long a hidden cell's reveal pops (display µs; <c>RoundLook.CellRevealSeconds</c> in seconds).</summary>
        public const long CellRevealUs = 360_000;

        /// <summary>
        /// How long a latch's opening plays (display µs; <c>RoundLook.LatchOpenSeconds</c>): the acorn key pops off its cell and
        /// the latch block swings open and leaves its column; then the pods behind it slide up.
        /// </summary>
        public const long LatchOpenUs = 520_000;

        /// <summary>A cell's clear moment when it was cleared before the animator started (a restored attempt): no animation.</summary>
        public const long Before = long.MinValue / 4;

        private readonly RoundPrepared _prepared;
        private readonly sbyte[] _shown;
        private readonly long[] _clearedAt;
        private readonly bool[] _burst;

        // Hidden cells (17 §8 «Скрытые клетки»): which cells are veiled as the events hold them, and when each revealed.
        private readonly bool[] _veiled;
        private readonly long[] _cellRevealedAt;
        private readonly bool[] _burstVeiled;

        // Latch + key (17 §8 «Засов и жёлудь-ключ»): which latches are closed as the events hold them, and when each opened.
        private readonly bool[] _latched;
        private readonly long[] _latchOpenedAt;
        private readonly List<RoundWalk> _walkers = new List<RoundWalk>();

        // Walkers the rules have home already whose look still walks back (ClearStyles.LiveLinger), and the list to draw.
        private readonly List<RoundWalk> _homing = new List<RoundWalk>();
        private readonly List<RoundWalk> _drawn = new List<RoundWalk>();
        private bool _drawnDirty = true;

        /// <summary>How long a walker's act is drawn after the rules' clear (<see cref="ClearStyles.LiveLinger"/>), display µs.</summary>
        public static readonly long LingerUs = (long)Math.Round(ClearStyles.LiveLinger * 1_000_000.0);

        /// <summary>
        /// When a walker's look is home again, display µs (<see cref="ClearStyles.LiveHomeAt"/>): its act to the clear plus the
        /// linger, then home at its way out's pace.
        /// </summary>
        public static long HomeUs(RoundWalk walk) => walk.Clear + LingerUs + Math.Max(walk.Done - walk.Clear, walk.Arrive - walk.Start);
        private readonly long[] _revealedAt;
        private readonly long[] _committedAt;
        private readonly long[] _recalledAt;
        private readonly int[] _remaining;
        private readonly bool[] _revealed;
        private readonly int[] _placeOf;
        private readonly int[] _columnOf;
        private readonly int[] _depthOf;
        private readonly List<RoundTube> _tubes = new List<RoundTube>();
        private int[] _slots = Array.Empty<int>();
        private (int Pod, long At)[] _leaving = Array.Empty<(int, long)>();
        private int[][] _columns = Array.Empty<int[]>();
        private int[] _heads = Array.Empty<int>();

        public RoundAnimator(RoundLevel level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            _prepared = level.Prepare();
            int cells = level.CellCount;
            int pods = level.Pods.Count;
            _shown = new sbyte[cells];
            _clearedAt = new long[cells];
            _burst = new bool[cells];
            _veiled = new bool[cells];
            _cellRevealedAt = new long[cells];
            _burstVeiled = new bool[cells];
            _latched = new bool[level.Latches.Count];
            _latchOpenedAt = new long[level.Latches.Count];
            _revealedAt = new long[pods];
            _committedAt = new long[pods];
            _recalledAt = new long[pods];
            _remaining = new int[pods];
            _revealed = new bool[pods];
            _placeOf = new int[pods];
            _columnOf = new int[pods];
            _depthOf = new int[pods];
        }

        public RoundLevel Level { get; }

        public int Width => Level.Width;

        public int Height => Level.Height;

        /// <summary>The display clock in µs (<see cref="SetTime"/>).</summary>
        public long Now { get; private set; }

        /// <summary>The display time of the last event seen.</summary>
        public long LastEventAt { get; private set; } = Before;

        /// <summary>The built bridge's choice, or -1.</summary>
        public int Bridge { get; private set; } = -1;

        /// <summary>When the bridge was built (display µs).</summary>
        public long BridgeAt { get; private set; } = Before;

        /// <summary>The couriers on the way, in departure order (the rules' couriers: <see cref="MatchesRules"/> counts them).</summary>
        public IReadOnlyList<RoundWalk> Walkers => _walkers;

        /// <summary>
        /// The walkers to draw: the couriers on the way, then those the rules have home already whose look still walks back,
        /// until <see cref="HomeUs"/> (the act lasts past the clear by <see cref="ClearStyles.LiveLinger"/>, the walk home at the
        /// way out's pace). Presentation only.
        /// </summary>
        public IReadOnlyList<RoundWalk> ShownWalkers
        {
            get
            {
                if (_drawnDirty)
                {
                    _drawn.Clear();
                    _drawn.AddRange(_walkers);
                    _drawn.AddRange(_homing);
                    _drawnDirty = false;
                }

                return _drawn;
            }
        }

        /// <summary>The number of places (6 after Extra Slot).</summary>
        public int PlaceCount => _slots.Length;

        public int ColumnCount => _columns.Length;

        /// <summary>Whether the board still moves: a walker on the way, or a clear's effects within <see cref="SettleUs"/>.</summary>
        public bool Busy => _walkers.Count > 0 || _homing.Count > 0 || Now - LastEventAt < SettleUs;

        /// <summary>Starts over from <paramref name="timeline"/> (a new or restored attempt) at display time <paramref name="nowUs"/>.</summary>
        public void Reset(RoundTimeline timeline, long nowUs)
        {
            if (timeline == null)
            {
                throw new ArgumentNullException(nameof(timeline));
            }

            Now = nowUs;
            LastEventAt = Before;
            long offset = nowUs - timeline.Now;
            sbyte[] board = timeline.State.Board;
            for (int c = 0; c < _shown.Length; c++)
            {
                _shown[c] = board[c];
                _burst[c] = false;
                _clearedAt[c] = _prepared.Board[c] >= 0 && board[c] < 0 ? Before : -1;
                _veiled[c] = !timeline.State.Known(c);
                _cellRevealedAt[c] = Before;
                _burstVeiled[c] = false;
            }

            for (int i = 0; i < _latched.Length; i++)
            {
                _latched[i] = timeline.State.Latched != null && timeline.State.Latched[i];
                _latchOpenedAt[i] = Before;
            }

            for (int p = 0; p < _revealedAt.Length; p++)
            {
                _revealedAt[p] = Before;
                _committedAt[p] = Before;
                _recalledAt[p] = Before;
            }

            Bridge = timeline.State.Bridge;
            BridgeAt = Before;
            _walkers.Clear();
            _homing.Clear();
            _drawnDirty = true;
            foreach (RoundCourier j in timeline.Couriers)
            {
                _walkers.Add(new RoundWalk(j.Id, j.Pod, _prepared.Colours[j.Pod], j.Cell, j.Start + offset, j.Arrive + offset, j.Clear + offset, j.Done + offset, j.Route));
            }

            _leaving = Array.Empty<(int, long)>();
            Sync(timeline);
        }

        /// <summary>Moves the display clock to <paramref name="nowUs"/> (never back).</summary>
        public void SetTime(long nowUs)
        {
            if (nowUs > Now)
            {
                Now = nowUs;
            }

            // A walker home in the rules is drawn until its look is home too.
            for (int w = _homing.Count - 1; w >= 0; w--)
            {
                if (HomeUs(_homing[w]) <= Now)
                {
                    _homing.RemoveAt(w);
                    _drawnDirty = true;
                }
            }
        }

        /// <summary>
        /// Takes a batch of the timeline's events (a command's, a booster's or an advance's <see cref="RoundTimeline.LastEvents"/>),
        /// each shown at its model time plus <paramref name="offsetUs"/>.
        /// </summary>
        public void Observe(IReadOnlyList<RoundEvent> events, long offsetUs)
        {
            for (int i = 0; i < events.Count; i++)
            {
                RoundEvent e = events[i];
                long at = e.At + offsetUs;
                LastEventAt = Math.Max(LastEventAt, at);
                switch (e.Type)
                {
                    case RoundEventType.Commit:
                        _committedAt[e.Pod] = at;
                        break;
                    case RoundEventType.Reveal:
                        _revealedAt[e.Pod] = at;
                        break;
                    case RoundEventType.Bridge:
                        _shown[e.Cell] = RoundLevel.Open;
                        BridgeAt = at;
                        break;
                    case RoundEventType.Depart:
                        _walkers.Add(new RoundWalk(e.Id, e.Pod, _prepared.Colours[e.Pod], e.Cell, at, e.ArriveAt + offsetUs, e.ClearAt + offsetUs, e.DoneAt + offsetUs, e.Route ?? Array.Empty<int>()));
                        _drawnDirty = true;
                        break;
                    case RoundEventType.Clear:
                    case RoundEventType.BurstClear:
                        _shown[e.Cell] = RoundLevel.Open;
                        _clearedAt[e.Cell] = at;
                        _burst[e.Cell] = e.Type == RoundEventType.BurstClear;

                        // A veiled cell Bloom Burst took clears unseen: it puffs away as the "?" cube (BurstVeiled).
                        _burstVeiled[e.Cell] = _veiled[e.Cell];
                        _veiled[e.Cell] = false;
                        break;
                    case RoundEventType.CellReveal:
                        _veiled[e.Cell] = false;
                        _cellRevealedAt[e.Cell] = at;
                        break;
                    case RoundEventType.LatchOpen:
                        _latched[e.Latch] = false;
                        _latchOpenedAt[e.Latch] = at;
                        break;
                    case RoundEventType.Return:
                        for (int w = 0; w < _walkers.Count; w++)
                        {
                            if (_walkers[w].Id == e.Id)
                            {
                                if (HomeUs(_walkers[w]) > Now)
                                {
                                    _homing.Add(_walkers[w]);
                                }

                                _walkers.RemoveAt(w);
                                _drawnDirty = true;
                                break;
                            }
                        }

                        break;
                    case RoundEventType.Complete:
                        if (e.Slot >= 0)
                        {
                            EnsureLeaving(e.Slot + 1);
                            _leaving[e.Slot] = (e.Pod, at);
                        }

                        break;
                    case RoundEventType.Recall:
                        _recalledAt[e.Pod] = at;
                        break;
                }
            }
        }

        /// <summary>Copies the rules' places, counts, reveals and columns (call after every command and advance).</summary>
        public void Sync(RoundTimeline timeline)
        {
            RoundState s = timeline.State;
            Bridge = s.Bridge;
            if (_slots.Length != s.Slots.Length)
            {
                _slots = new int[s.Slots.Length];
            }

            EnsureLeaving(_slots.Length);
            Array.Copy(s.Slots, _slots, _slots.Length);
            Array.Copy(s.Remaining, _remaining, _remaining.Length);
            Array.Copy(s.Revealed, _revealed, _revealed.Length);
            for (int p = 0; p < _placeOf.Length; p++)
            {
                _placeOf[p] = -1;
                _columnOf[p] = -1;
                _depthOf[p] = -1;
            }

            for (int k = 0; k < _slots.Length; k++)
            {
                if (_slots[k] >= 0)
                {
                    _placeOf[_slots[k]] = k;
                }
            }

            if (_columns.Length != s.Columns.Length)
            {
                _columns = new int[s.Columns.Length][];
                _heads = new int[s.Columns.Length];
            }

            for (int c = 0; c < _columns.Length; c++)
            {
                List<int> stack = s.Columns[c];
                if (_columns[c] == null || _columns[c].Length != stack.Count)
                {
                    _columns[c] = new int[stack.Count];
                }

                stack.CopyTo(_columns[c]);
                _heads[c] = s.Heads[c];
                for (int k = _heads[c]; k < stack.Count; k++)
                {
                    _columnOf[stack[k]] = c;
                    _depthOf[stack[k]] = k - _heads[c];
                }
            }
        }

        // ---- The board ----

        /// <summary>The material a cell shows now (<see cref="RoundMaterial.Index"/>), <see cref="RoundLevel.Open"/> or <see cref="RoundLevel.Wall"/>.</summary>
        public int MaterialAt(int cell) => _shown[cell];

        /// <summary>The material a cell had in the level (its cleared look), or a negative value for a cell without one.</summary>
        public int OriginalMaterial(int cell) => _prepared.Board[cell];

        /// <summary>When the cell's cube was cleared (display µs), <see cref="Before"/> for one cleared before, or -1.</summary>
        public long ClearedAt(int cell) => _clearedAt[cell];

        /// <summary>Whether the cell's cube was cleared by Bloom Burst (it puffs away instead of being eaten).</summary>
        public bool Burst(int cell) => _burst[cell];

        /// <summary>
        /// Whether the cell is drawn as the hidden cell's neutral "?" cube now (17 §8 «Скрытые клетки»): a hidden cell whose
        /// <c>cellReveal</c> has not been seen, or whose reveal's display time is still ahead of <see cref="Now"/>. Its material
        /// stays unknown to the player until then (Bloom Burst cannot pick it).
        /// </summary>
        public bool Veiled(int cell) => _veiled[cell] || _cellRevealedAt[cell] > Now;

        /// <summary>Whether Bloom Burst took the cell while it was still veiled: its puff is the "?" cube.</summary>
        public bool BurstVeiled(int cell) => _burstVeiled[cell];

        /// <summary>When the hidden cell revealed during play (display µs), or <see cref="Before"/>.</summary>
        public long CellRevealedAt(int cell) => _cellRevealedAt[cell];

        /// <summary>
        /// Seconds since the cell's reveal at <see cref="Now"/> while its pop plays (<see cref="CellRevealUs"/>), else -1: the
        /// painters scale its cube by <see cref="RoundLook.CellRevealScale"/> and fade the "?" over it by
        /// <see cref="RoundLook.CellRevealVeil"/>.
        /// </summary>
        public float CellRevealSince(int cell)
        {
            long at = _cellRevealedAt[cell];
            if (at == Before || at > Now || Now - at >= CellRevealUs)
            {
                return -1f;
            }

            return (Now - at) / 1_000_000f;
        }

        // ---- Latches and their acorn keys (17 §8 «Засов и жёлудь-ключ», FR-068) ----

        /// <summary>The level's latches (<see cref="RoundLevel.Latches"/>).</summary>
        public int LatchCount => _latched.Length;

        /// <summary>Whether latch <paramref name="latch"/> is drawn closed now: its <c>latchOpen</c> not seen, or still ahead of <see cref="Now"/>.</summary>
        public bool LatchClosed(int latch) => _latched[latch] || _latchOpenedAt[latch] > Now;

        /// <summary>When the latch opened during play (display µs), or <see cref="Before"/>.</summary>
        public long LatchOpenedAt(int latch) => _latchOpenedAt[latch];

        /// <summary>
        /// Seconds since the latch opened at <see cref="Now"/> while its opening plays (<see cref="LatchOpenUs"/>), else -1: the
        /// painters pop the acorn key off its cell and swing the latch block open and out of its column.
        /// </summary>
        public float LatchOpenSince(int latch)
        {
            long at = _latchOpenedAt[latch];
            if (_latched[latch] || at == Before || at > Now || Now - at >= LatchOpenUs)
            {
                return -1f;
            }

            return (Now - at) / 1_000_000f;
        }

        /// <summary>Whether the latch (and its acorn key) is drawn now: closed, or opening.</summary>
        public bool LatchShown(int latch) => LatchClosed(latch) || LatchOpenSince(latch) >= 0f;

        /// <summary>The latch whose acorn key lies on <paramref name="cell"/>, or -1.</summary>
        public int LatchOfKey(int cell)
        {
            IReadOnlyList<RoundLatch> latches = Level.Latches;
            for (int i = 0; i < latches.Count; i++)
            {
                if (latches[i].Key == cell)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Column <paramref name="column"/>'s queue as drawn (FR-068), top first, into <paramref name="into"/> (cleared): its
        /// waiting pods with every shown latch (<see cref="LatchShown"/>) of the column at its place among them, a latch
        /// before the pod at its depth, each item at its row; at most <paramref name="maxRows"/> rows (0: all).
        /// </summary>
        public void ColumnItems(int column, int maxRows, List<RoundQueueItem> into)
        {
            into.Clear();
            int head = _heads[column];
            int waiting = _columns[column].Length - head;
            int limit = maxRows > 0 ? maxRows : int.MaxValue;
            IReadOnlyList<RoundLatch> latches = Level.Latches;
            int row = 0;
            for (int k = 0; k <= waiting && row < limit; k++)
            {
                // The latches standing before the pod at depth k (an opening one the head has passed stays on top).
                for (int depth = 0; depth < 64 && latches.Count > 0; depth++)
                {
                    int found = -1;
                    for (int i = 0; i < latches.Count; i++)
                    {
                        if (latches[i].Column == column && LatchShown(i) && Math.Max(0, latches[i].Depth - head) == k
                            && (found < 0 || latches[i].Depth < latches[found].Depth) && !Listed(into, i))
                        {
                            found = i;
                        }
                    }

                    if (found < 0 || row >= limit)
                    {
                        break;
                    }

                    into.Add(new RoundQueueItem(true, found, k, row++));
                }

                if (k < waiting && row < limit)
                {
                    into.Add(new RoundQueueItem(false, _columns[column][head + k], k, row++));
                }
            }
        }

        /// <summary>
        /// Whether the queue item at <paramref name="index"/> of <paramref name="items"/> (<see cref="ColumnItems"/>) stands
        /// behind a latch still drawn closed: such a pod is no top pod, drawn as a waiting one, and takes no tap.
        /// </summary>
        public bool HeldBehind(IReadOnlyList<RoundQueueItem> items, int index)
        {
            for (int k = 0; k < index && k < items.Count; k++)
            {
                if (items[k].IsLatch && LatchClosed(items[k].Id))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Listed(List<RoundQueueItem> items, int latch)
        {
            foreach (RoundQueueItem item in items)
            {
                if (item.IsLatch && item.Id == latch)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- Pods, places and columns ----

        /// <summary>The pod's unsent count, as the rules hold it (a courier takes one at its departure).</summary>
        public int Remaining(int pod) => _remaining[pod];

        public bool Revealed(int pod) => _revealed[pod];

        /// <summary>When the pod revealed during play (display µs), or <see cref="Before"/>.</summary>
        public long RevealedAt(int pod) => _revealedAt[pod];

        /// <summary>When the pod took its place (display µs), or <see cref="Before"/>.</summary>
        public long CommittedAt(int pod) => _committedAt[pod];

        /// <summary>When Return sent the pod back to its column (display µs), or <see cref="Before"/>.</summary>
        public long RecalledAt(int pod) => _recalledAt[pod];

        /// <summary>The pod in place <paramref name="place"/>, or -1.</summary>
        public int PodIn(int place) => _slots[place];

        /// <summary>The place the pod is in, or -1.</summary>
        public int PlaceOf(int pod) => _placeOf[pod];

        /// <summary>The pod that last left place <paramref name="place"/> and when (its puff), or (-1, <see cref="Before"/>).</summary>
        public (int Pod, long At) Leaving(int place) => place < _leaving.Length ? _leaving[place] : (-1, Before);

        /// <summary>Column <paramref name="column"/>'s pods top first, the taken ones before <see cref="Head"/>.</summary>
        public IReadOnlyList<int> Column(int column) => _columns[column];

        /// <summary>The index of column <paramref name="column"/>'s top pod in <see cref="Column"/>.</summary>
        public int Head(int column) => _heads[column];

        /// <summary>How many pods still wait in column <paramref name="column"/>.</summary>
        public int Waiting(int column) => _columns[column].Length - _heads[column];

        /// <summary>The pod at <paramref name="depth"/> (0 = the top) of column <paramref name="column"/>, or -1.</summary>
        public int At(int column, int depth)
        {
            int k = _heads[column] + depth;
            return depth >= 0 && k < _columns[column].Length ? _columns[column][k] : -1;
        }

        /// <summary>The column a waiting pod is in, or -1.</summary>
        public int ColumnOf(int pod) => _columnOf[pod];

        /// <summary>A waiting pod's depth in its column (0 = the top), or -1.</summary>
        public int DepthOf(int pod) => _depthOf[pod];

        /// <summary>The pods linked with <paramref name="pod"/> (itself included), in pod order.</summary>
        public IReadOnlyList<int> GroupOf(int pod) => _prepared.Groups[pod];

        /// <summary>The material of a pod (<see cref="RoundMaterial.Index"/>).</summary>
        public int MaterialOf(int pod) => _prepared.Colours[pod];

        /// <summary>
        /// The link tubes to draw now (FR-043): each linked pair whose members both stand in the places, or both wait in the
        /// columns within the <paramref name="shownRows"/> rows the tray shows. The list is reused between calls.
        /// </summary>
        public IReadOnlyList<RoundTube> Tubes(int shownRows)
        {
            _tubes.Clear();
            int[][] groups = _prepared.Groups;
            for (int p = 0; p < groups.Length; p++)
            {
                int[] group = groups[p];
                if (group.Length < 2 || group[0] != p)
                {
                    continue;
                }

                for (int m = 1; m < group.Length; m++)
                {
                    int a = group[m - 1];
                    int b = group[m];
                    if (_placeOf[a] >= 0 && _placeOf[b] >= 0)
                    {
                        _tubes.Add(new RoundTube(a, b, RoundTubePlace.Places));
                    }
                    else if (_depthOf[a] >= 0 && _depthOf[b] >= 0 && _depthOf[a] < shownRows && _depthOf[b] < shownRows)
                    {
                        _tubes.Add(new RoundTube(a, b, RoundTubePlace.Columns));
                    }
                }
            }

            return _tubes;
        }

        /// <summary>
        /// Whether what the animator shows equals the rules' state of <paramref name="timeline"/> (the playtest check, T031):
        /// the board, the places, the counts, the reveals and the columns, and no walker once nothing is pending.
        /// </summary>
        public bool ShowsRulesState(RoundTimeline timeline, out string difference)
        {
            RoundState s = timeline.State;
            for (int c = 0; c < _shown.Length; c++)
            {
                if (_shown[c] != s.Board[c])
                {
                    difference = Diagnostic("cell {0} shows {1}, the rules hold {2}", c, _shown[c], s.Board[c]);
                    return false;
                }

                if (_veiled[c] != (s.Board[c] >= 0 && !s.Known(c)))
                {
                    difference = Diagnostic("cell {0}: veiled {1}, the rules hold {2}", c, _veiled[c], !s.Known(c));
                    return false;
                }
            }

            if (_slots.Length != s.Slots.Length)
            {
                difference = Diagnostic("{0} places shown, the rules hold {1}", _slots.Length, s.Slots.Length);
                return false;
            }

            for (int k = 0; k < _slots.Length; k++)
            {
                if (_slots[k] != s.Slots[k])
                {
                    difference = Diagnostic("place {0} shows pod {1}, the rules hold {2}", k, _slots[k], s.Slots[k]);
                    return false;
                }
            }

            for (int p = 0; p < _remaining.Length; p++)
            {
                if (_remaining[p] != s.Remaining[p] || _revealed[p] != s.Revealed[p])
                {
                    difference = Diagnostic("pod {0} differs", p);
                    return false;
                }
            }

            for (int c = 0; c < _heads.Length; c++)
            {
                if (_heads[c] != s.Heads[c])
                {
                    difference = Diagnostic("column {0}: its head differs", c);
                    return false;
                }
            }

            for (int i = 0; i < _latched.Length; i++)
            {
                if (_latched[i] != (s.Latched != null && s.Latched[i]))
                {
                    difference = Diagnostic("latch {0}: closed {1}, the rules differ", i, _latched[i]);
                    return false;
                }
            }

            if (_walkers.Count != timeline.Couriers.Count)
            {
                difference = Diagnostic("{0} walkers shown, {1} couriers on the way", _walkers.Count, timeline.Couriers.Count);
                return false;
            }

            difference = string.Empty;
            return true;
        }

        // A check's diagnostic for developers (never shown to players).
        private static string Diagnostic(string format, params object[] values) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, format, values);

        private void EnsureLeaving(int count)
        {
            if (_leaving.Length >= count)
            {
                return;
            }

            var grown = new (int Pod, long At)[count];
            for (int k = 0; k < grown.Length; k++)
            {
                grown[k] = k < _leaving.Length ? _leaving[k] : (-1, Before);
            }

            _leaving = grown;
        }
    }
}
