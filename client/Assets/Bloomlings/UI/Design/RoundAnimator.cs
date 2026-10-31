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
    /// moment for the cube's clear animation (FR-042); the built bridge;</description></item>
    /// <item><description>the walkers: every courier from its <c>depart</c> (which carries its whole plan) to its
    /// <c>return</c>, with its route and its four times;</description></item>
    /// <item><description>the places and the columns as the rules hold them, with when each pod took its place, when a
    /// finished pod left one (its puff) and when a hidden pod revealed (its glow, FR-045);</description></item>
    /// <item><description>the link tubes between linked pods in the columns and in the places (FR-043).</description></item>
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

        /// <summary>A cell's clear moment when it was cleared before the animator started (a restored attempt): no animation.</summary>
        public const long Before = long.MinValue / 4;

        private readonly RoundPrepared _prepared;
        private readonly sbyte[] _shown;
        private readonly long[] _clearedAt;
        private readonly bool[] _burst;
        private readonly List<RoundWalk> _walkers = new List<RoundWalk>();

        // Walkers the rules have home already whose look still walks back (ClearStyles.LiveLinger), and the list to draw.
        private readonly List<RoundWalk> _homing = new List<RoundWalk>();
        private readonly List<RoundWalk> _drawn = new List<RoundWalk>();
        private bool _drawnDirty = true;

        /// <summary>How long a walker is drawn after the rules' return (<see cref="ClearStyles.LiveLinger"/>), display µs.</summary>
        public static readonly long LingerUs = (long)Math.Round(ClearStyles.LiveLinger * 1_000_000.0);
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
        /// until their return plus <see cref="LingerUs"/> (the act lasts past the clear by <see cref="ClearStyles.LiveLinger"/>,
        /// the walk home at the rules' pace that much later). Presentation only.
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
                if (_homing[w].Done + LingerUs <= Now)
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
                        break;
                    case RoundEventType.Return:
                        for (int w = 0; w < _walkers.Count; w++)
                        {
                            if (_walkers[w].Id == e.Id)
                            {
                                if (_walkers[w].Done + LingerUs > Now)
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
