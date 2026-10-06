using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Gameplay.Timeline
{
    /// <summary>
    /// The engine-free player behind <see cref="EventTimeline"/> (research R4, T045; the playtest's <c>LevelAnimator</c>):
    /// it turns the settle events of each command into waves, one per settle round, schedules them on its own clock
    /// (<see cref="Now"/>, in timeline seconds) and tells an <see cref="ITimelineSink"/> what to show and when.
    /// <para>
    /// The waves of every command play side by side, so pods committed one after another work at the same time (spec
    /// 001 FR-018, the owner's report of 2026-10-03), and a command's rounds no longer wait for each other (the owner's
    /// calm pace of 2026-10-06). The rules resolve each tap at once (FR-014, FR-022), so a walker only waits for its way:
    /// it sets off as soon as it reaches every cell of its route, and its targets, only after that cell's earlier queued
    /// change has shown (a tile an earlier tap or round takes, a layer or a mystery tile revealed under it),
    /// <see cref="StepMargin"/> later, whatever the wave's other walkers wait for (the owner, 2026-10-04), and at least
    /// <see cref="ClearStyles.LineGap"/> after its pod's last walker out of its arch, nearer tiles first. Every trip takes
    /// <see cref="ClearStyles.TripSeconds"/> in every clearing style (<see cref="Style"/>, spec 005 FR-038); a later
    /// walker may cross a cell once its tile is gone from it (the style's out and act legs) unless a layer comes up under
    /// it. A wave ends after its last arrival, and its end events keep the rules' order: after its command's earlier
    /// rounds, a special's progress and trigger and a pod's completion after the last earlier wave that changed the same
    /// special or that pod's work, and the level's outcome (won, jammed, stuck) after every earlier wave, so its card
    /// waits for the last wave. Wave starts, arrivals and wave ends happen in time order, each at its own moment.
    /// </para>
    /// <para>
    /// 2× speed only scales the clock (FR-069). When the time until every queued wave has played (<see cref="Backlog"/>)
    /// exceeds the backlog threshold (Remote Config <c>fx.backlogThresholdMs</c>) the clock runs faster, up to
    /// <see cref="MaxRate"/>, and walkers are merged. Nothing here changes an outcome: every change comes from the core's
    /// events, already applied to the logical state.
    /// </para>
    /// </summary>
    public sealed class TimelinePlayer
    {
        // The clearing pace is the clearing styles' (ClearStyles.TripSeconds, the owner's calm pace of 2026-10-06; it was
        // 0.28 s a step and waves of 1.2–5.6 s): every style takes the same time for a tile, and no wave squeezes a trip.
        public const float MinWaveSeconds = 1.2f;
        public const float MaxWaveSeconds = 40f;

        public const float RestoreSeconds = 0.22f;
        public const float MaxRate = 4f;

        /// <summary>How long after a cell's earlier change has shown a later Bloomling may step on it.</summary>
        public const float StepMargin = 0.05f;

        /// <summary>How much later than the last wave changing the same special or pod (or any wave, for the outcome) a wave ends.</summary>
        public const float KeyGap = 0.001f;

        /// <summary>From this playback rate on, a wave's Bloomlings are merged three to a walker.</summary>
        private const float MergeRate = 2.5f;

        // On a tie in time: a wave ends before another's Bloomling arrives, which comes before a wave starts (a command's
        // next round starts as its previous one ends).
        private const int Ending = 0;
        private const int Arriving = 1;
        private const int Starting = 2;

        private readonly List<Wave> _waves = new List<Wave>();

        // Per cell: when its last change queued so far shows (a later Bloomling steps on it or changes it after that).
        private readonly float[] _ready = new float[CellPos.MaxWidth * CellPos.MaxHeight];

        // Per special and per pod: when the last wave queued so far that changes it ends.
        private readonly Dictionary<string, float> _keyReady = new Dictionary<string, float>(StringComparer.Ordinal);

        // Per pod and arch (its entry cell): when its last walker queued so far leaves, so the next keeps the line (each
        // pod's walkers leave in a line of their own; pods still work side by side, FR-018).
        private readonly Dictionary<(string Pod, int Door), float> _lastGo = new Dictionary<(string, int), float>();
        private ITimelineSink? _sink;

        public TimelinePlayer()
        {
            ForgetReady();
        }

        /// <summary>1 or 2 (the 2× toggle).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>The level's clearing style (spec 005 FR-038): its legs decide when a walker's tile leaves its cell.</summary>
        public ClearStyle Style { get; set; } = ClearStyle.Blossom;

        /// <summary>The backlog beyond which playback speeds up (Remote Config <c>fx.backlogThresholdMs</c>).</summary>
        public float BacklogThresholdSeconds { get; set; } = RemoteConfigKeys.FxBacklogThresholdMs.Default / 1000f;

        /// <summary>Most walkers one wave shows; a larger wave merges walkers (low-end cap, R4).</summary>
        public int WorkerCapacity { get; set; } = 60;

        /// <summary>Current playback rate, including backlog compression.</summary>
        public float Rate { get; private set; } = 1f;

        /// <summary>The timeline clock, in timeline seconds; walkers measure their walk on it.</summary>
        public float Now { get; private set; }

        /// <summary>Every queued wave has played.</summary>
        public bool IsIdle => _waves.Count == 0;

        /// <summary>How many waves are playing now (more than one while the waves of several commands play side by side).</summary>
        public int Playing
        {
            get
            {
                int playing = 0;
                foreach (Wave wave in _waves)
                {
                    playing += wave.Started ? 1 : 0;
                }

                return playing;
            }
        }

        /// <summary>How long, in timeline seconds, until every queued wave has played.</summary>
        public float Backlog => LastEnd() - Now;

        public void Bind(ITimelineSink sink) => _sink = sink;

        /// <summary>
        /// Queues the settle events of one command (rounds ≥ 1; round 0 is applied by the caller at once) and schedules its
        /// waves: one after another from now, each also late enough for what it depends on (see the class summary).
        /// </summary>
        public void Enqueue(IReadOnlyList<GameEvent> events)
        {
            var waves = new List<Wave>();
            Wave? wave = null;
            for (int i = 0; i < events.Count; i++)
            {
                GameEvent e = events[i];
                if (e.Round == 0)
                {
                    continue;
                }

                if (wave == null || wave.Round != e.Round)
                {
                    wave = new Wave(e.Round);
                    waves.Add(wave);
                }

                switch (e)
                {
                    case TileCleared clear:
                        GameEvent? reveal = null;
                        if (i + 1 < events.Count && IsRevealOf(events[i + 1], clear.Cell))
                        {
                            reveal = events[++i];
                        }

                        float travel = ClearStyles.TripSeconds(clear.RouteFromEntry.Count);
                        wave.Work.Add(new WorkUnit(clear, reveal, travel));
                        wave.Duration = Math.Min(MaxWaveSeconds, Math.Max(MinWaveSeconds, Math.Max(wave.Duration, travel + RestoreSeconds)));
                        break;
                    case MysteryTileRevealed _:
                        wave.Start.Add(e);
                        break;
                    default:
                        wave.End.Add(e);
                        break;
                }
            }

            float lastEnd = Now;
            foreach (Wave w in waves)
            {
                Plan(w);
                Schedule(w, lastEnd);
                Record(w);
                _waves.Add(w);
                lastEnd = w.EndAt;
            }
        }

        /// <summary>
        /// Moves the clock on by <paramref name="realSeconds"/> of wall time (times the playback rate): every wave start,
        /// arrival and wave end due by then happens in time order.
        /// </summary>
        public void Advance(float realSeconds)
        {
            if (_sink == null)
            {
                return;
            }

            Rate = Math.Min(MaxRate, Speed * Math.Max(1f, Backlog / Math.Max(0.1f, BacklogThresholdSeconds)));
            float until = Now + (realSeconds * Rate);
            Play(until, flushing: false);
            Now = Math.Max(Now, until);
        }

        /// <summary>
        /// Plays everything pending at once, without walkers (before a booster changes the state, so the board, slots and
        /// tray start from the settled picture): every start, arrival and end in the order the clock would have played
        /// them, so each cell, special and pod ends on its last change. The clock stays; the caller recalls the walkers.
        /// </summary>
        public void Flush()
        {
            if (_sink == null)
            {
                return;
            }

            Play(float.PositiveInfinity, flushing: true);
            ForgetReady();
        }

        /// <summary>Drops everything pending (restart, leaving the level).</summary>
        public void Clear()
        {
            _waves.Clear();
            ForgetReady();
        }

        private static bool IsRevealOf(GameEvent e, CellPos cell) =>
            (e is CellOpened opened && opened.Cell == cell) || (e is LayerRevealed layer && layer.Cell == cell);

        private static int Index(CellPos cell) => cell.X + (cell.Y * CellPos.MaxWidth);

        /// <summary>A wave that shows the level's outcome: its card waits for every earlier wave.</summary>
        private static bool EndsLevel(Wave wave)
        {
            foreach (GameEvent e in wave.End)
            {
                if (e is LevelWon || e is LevelJammed || e is LevelStuck)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The specials and pods a wave's end events change, which must show in the rules' order.</summary>
        private static IEnumerable<string> KeysAtEnd(Wave wave)
        {
            foreach (GameEvent e in wave.End)
            {
                switch (e)
                {
                    case SpecialProgressed progressed:
                        yield return "special:" + progressed.SpecialId;
                        break;
                    case SpecialTriggered triggered:
                        yield return "special:" + triggered.SpecialId;
                        break;
                    case PodCompleted done:
                        yield return "pod:" + done.PodId;
                        break;
                }
            }
        }

        /// <summary>The cells a wave's start or end events change on screen (a revealed mystery tile, a key, a special's cells).</summary>
        private static IEnumerable<CellPos> CellsAt(List<GameEvent> events)
        {
            foreach (GameEvent e in events)
            {
                switch (e)
                {
                    case MysteryTileRevealed revealed:
                        yield return revealed.Cell;
                        break;
                    case KeyCollected key:
                        yield return key.Cell;
                        break;
                    case SpecialTriggered triggered:
                        foreach (CellPos cell in triggered.EffectCells)
                        {
                            yield return cell;
                        }

                        break;
                }
            }
        }

        private void ForgetReady()
        {
            for (int i = 0; i < _ready.Length; i++)
            {
                _ready[i] = float.NegativeInfinity;
            }

            _keyReady.Clear();
            _lastGo.Clear();
        }

        /// <summary>When the last queued wave ends (now, when none is queued).</summary>
        private float LastEnd()
        {
            float end = Now;
            foreach (Wave wave in _waves)
            {
                end = Math.Max(end, wave.EndAt);
            }

            return end;
        }

        /// <summary>
        /// The wave's walkers: in travel order, merged when the wave is larger than the pool or playback is compressed (a
        /// walker follows its farthest unit's route, and all its units arrive with it). A walk is scaled into the wave, so
        /// the walker reaches its target as its tiles change.
        /// </summary>
        private void Plan(Wave wave)
        {
            var byTravel = new List<WorkUnit>(wave.Work);
            StableSort(byTravel, unit => unit.TravelSeconds);
            int perWalker = Math.Max(1, (int)Math.Ceiling(byTravel.Count / (double)Math.Max(1, WorkerCapacity)));
            if (Rate > MergeRate)
            {
                perWalker = Math.Max(perWalker, 3);
            }

            var open = new Dictionary<string, List<WorkUnit>>(StringComparer.Ordinal);
            var batches = new List<List<WorkUnit>>();
            foreach (WorkUnit unit in byTravel)
            {
                if (!open.TryGetValue(unit.Clear.PodId, out List<WorkUnit>? batch) || batch.Count >= perWalker)
                {
                    batch = new List<WorkUnit>();
                    open[unit.Clear.PodId] = batch;
                    batches.Add(batch);
                }

                batch.Add(unit);
            }

            foreach (List<WorkUnit> batch in batches)
            {
                wave.Walks.Add(new Walk(batch, Math.Min(batch[batch.Count - 1].TravelSeconds, wave.Duration - RestoreSeconds)));
            }
        }

        /// <summary>
        /// The wave's times: it starts now, or when its start events' cells have shown; each walker sets off as soon as
        /// every cell of its route and its targets has shown its earlier change, and a line gap after its pod's last
        /// walker out of its arch, its units arriving with it (<see cref="Wave.Work"/> in arrival order); the wave ends after its last
        /// arrival, after <paramref name="previousEnd"/> (its command's previous round) and late enough for its end events'
        /// order.
        /// </summary>
        private void Schedule(Wave wave, float previousEnd)
        {
            float start = Now;
            foreach (CellPos cell in CellsAt(wave.Start))
            {
                start = Math.Max(start, _ready[Index(cell)]);
            }

            float end = start + wave.Duration;
            var arrival = new Dictionary<WorkUnit, float>();
            var gone = new Dictionary<WorkUnit, float>();
            var units = new List<WorkUnit>();
            foreach (Walk walk in wave.Walks)
            {
                // The walker follows its farthest unit's route and reaches route cell j at (j + 1) / count of its walk out
                // (ClearLook): its path is the entry point, then the route cells.
                WorkUnit lead = walk.Batch[walk.Batch.Count - 1];
                IReadOnlyList<CellPos> route = lead.Clear.RouteFromEntry;
                ClearLegs legs = ClearStyles.LegsOf(Style, route.Count);
                float scale = walk.Travel / Math.Max(0.01f, lead.TravelSeconds);
                float outward = legs.Out * scale;
                float go = start;
                for (int j = 0; j < route.Count; j++)
                {
                    float reach = outward * (j + 1) / route.Count;
                    go = Math.Max(go, _ready[Index(route[j])] + StepMargin - reach);
                }

                foreach (WorkUnit unit in walk.Batch)
                {
                    go = Math.Max(go, _ready[Index(unit.Clear.Cell)] + StepMargin - outward);
                }

                if (route.Count > 0)
                {
                    var line = (lead.Clear.PodId, Index(route[0]));
                    if (_lastGo.TryGetValue(line, out float last))
                    {
                        go = Math.Max(go, last + ClearStyles.LineGap);
                    }

                    _lastGo[line] = go;
                }

                walk.Go = go;
                foreach (WorkUnit unit in walk.Batch)
                {
                    arrival[unit] = go + walk.Travel;

                    // The lead's tile leaves its cell with the style's out and act legs; merged units' tiles at the clear.
                    gone[unit] = unit == lead ? go + ((legs.Out + legs.Act) * scale) : go + walk.Travel;
                    units.Add(unit);
                }

                end = Math.Max(end, go + walk.Travel + RestoreSeconds);
            }

            StableSort(units, unit => arrival[unit]);
            wave.Work.Clear();
            wave.Work.AddRange(units);
            wave.Arrivals.Clear();
            wave.Gone.Clear();
            foreach (WorkUnit unit in units)
            {
                wave.Arrivals.Add(arrival[unit]);
                wave.Gone.Add(gone[unit]);
            }

            foreach (CellPos cell in CellsAt(wave.End))
            {
                end = Math.Max(end, _ready[Index(cell)]);
            }

            // A command's rounds still end in the rules' order.
            end = Math.Max(end, previousEnd + KeyGap);

            foreach (string key in KeysAtEnd(wave))
            {
                if (_keyReady.TryGetValue(key, out float ready))
                {
                    end = Math.Max(end, ready + KeyGap);
                }
            }

            if (EndsLevel(wave))
            {
                end = Math.Max(end, LastEnd() + KeyGap);
            }

            wave.StartAt = start;
            wave.EndAt = end;
        }

        /// <summary>Notes when the wave's changes show, for the waves queued after it.</summary>
        private void Record(Wave wave)
        {
            float end = wave.EndAt;
            foreach (string key in KeysAtEnd(wave))
            {
                KeyReady(key, end);
            }

            for (int i = 0; i < wave.Work.Count; i++)
            {
                KeyReady("pod:" + wave.Work[i].Clear.PodId, end);
                int cell = Index(wave.Work[i].Clear.Cell);

                // A later walker may cross the cell once its tile is gone, unless a layer comes up under it at the clear.
                _ready[cell] = Math.Max(_ready[cell], wave.Work[i].Reveal is LayerRevealed ? wave.Arrivals[i] : wave.Gone[i]);
            }

            foreach (CellPos cell in CellsAt(wave.Start))
            {
                _ready[Index(cell)] = Math.Max(_ready[Index(cell)], wave.StartAt);
            }

            foreach (CellPos cell in CellsAt(wave.End))
            {
                _ready[Index(cell)] = Math.Max(_ready[Index(cell)], end);
            }
        }

        private void KeyReady(string key, float end) =>
            _keyReady[key] = _keyReady.TryGetValue(key, out float ready) ? Math.Max(ready, end) : end;

        /// <summary>
        /// Plays every wave start, arrival and end due by <paramref name="until"/>, in time order. While flushing nothing
        /// sets off walkers and the clock stays where it is.
        /// </summary>
        private void Play(float until, bool flushing)
        {
            while (true)
            {
                Wave? next = null;
                int kind = Ending;
                float at = float.PositiveInfinity;
                foreach (Wave wave in _waves)
                {
                    float t;
                    int k;
                    if (!wave.Started)
                    {
                        t = wave.StartAt;
                        k = Starting;
                    }
                    else if (wave.NextArrival < wave.Work.Count)
                    {
                        t = wave.Arrivals[wave.NextArrival];
                        k = Arriving;
                    }
                    else
                    {
                        t = wave.EndAt;
                        k = Ending;
                    }

                    if (next == null || t < at || (t == at && k < kind))
                    {
                        next = wave;
                        kind = k;
                        at = t;
                    }
                }

                if (next == null || at > until)
                {
                    return;
                }

                if (!flushing)
                {
                    Now = Math.Max(Now, at);
                }

                switch (kind)
                {
                    case Starting:
                        Start(next, withWalkers: !flushing);
                        break;
                    case Arriving:
                        _sink!.OnWorkArrived(next.Work[next.NextArrival++]);
                        break;
                    default:
                        _waves.Remove(next);
                        foreach (GameEvent e in next.End)
                        {
                            _sink!.OnEvent(e);
                        }

                        break;
                }
            }
        }

        private void Start(Wave wave, bool withWalkers)
        {
            wave.Started = true;
            foreach (GameEvent e in wave.Start)
            {
                _sink!.OnEvent(e);
            }

            if (withWalkers)
            {
                foreach (Walk walk in wave.Walks)
                {
                    _sink!.OnWorkStarted(walk.Batch, walk.Go, walk.Travel);
                }
            }
        }

        /// <summary>Sorts by a key, keeping the order of equal keys (the rules' order).</summary>
        private static void StableSort(List<WorkUnit> units, Func<WorkUnit, float> key)
        {
            var indexed = new List<(WorkUnit Unit, int Index)>(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                indexed.Add((units[i], i));
            }

            indexed.Sort((a, b) =>
            {
                int byKey = key(a.Unit).CompareTo(key(b.Unit));
                return byKey != 0 ? byKey : a.Index.CompareTo(b.Index);
            });
            for (int i = 0; i < units.Count; i++)
            {
                units[i] = indexed[i].Unit;
            }
        }

        /// <summary>One walker: the units it stands for (its farthest last), its walking time and when it sets off, in timeline seconds.</summary>
        private sealed class Walk
        {
            public Walk(IReadOnlyList<WorkUnit> batch, float travel)
            {
                Batch = batch;
                Travel = travel;
            }

            public IReadOnlyList<WorkUnit> Batch { get; }

            public float Travel { get; }

            /// <summary>When it sets off on the clock (its route clear), at the wave's start or later.</summary>
            public float Go { get; set; }
        }

        private sealed class Wave
        {
            public Wave(int round)
            {
                Round = round;
                Duration = MinWaveSeconds;
            }

            public int Round { get; }

            public float Duration { get; set; }

            /// <summary>When it starts on the clock (after its command's earlier waves and its start events' cells).</summary>
            public float StartAt { get; set; }

            /// <summary>When it ends (its end events show): after its last arrival and in the rules' order.</summary>
            public float EndAt { get; set; }

            public bool Started { get; set; }

            /// <summary>The next of <see cref="Work"/> to arrive.</summary>
            public int NextArrival { get; set; }

            public List<GameEvent> Start { get; } = new List<GameEvent>();

            /// <summary>Its work units in arrival order, each arriving at <see cref="Arrivals"/> on the clock.</summary>
            public List<WorkUnit> Work { get; } = new List<WorkUnit>();

            public List<float> Arrivals { get; } = new List<float>();

            /// <summary>When each of <see cref="Work"/>'s tiles is gone from its cell (crossable), on the clock.</summary>
            public List<float> Gone { get; } = new List<float>();

            public List<Walk> Walks { get; } = new List<Walk>();

            public List<GameEvent> End { get; } = new List<GameEvent>();
        }
    }
}
