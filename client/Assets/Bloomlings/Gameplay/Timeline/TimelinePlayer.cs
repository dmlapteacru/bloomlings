using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Gameplay.Timeline
{
    /// <summary>
    /// The engine-free player behind <see cref="EventTimeline"/> (research R4, T045; the playtest's <c>LevelAnimator</c>):
    /// it turns the settle events of each command into waves, one per settle round, schedules them on its own clock
    /// (<see cref="Now"/>, in timeline seconds) and tells an <see cref="ITimelineSink"/> what to show and when.
    /// <para>
    /// The waves of one command play one after another, in round order; the waves of different commands play side by
    /// side, so pods committed one after another work at the same time (spec 001 FR-018, the owner's report of
    /// 2026-10-03). The rules resolve each tap at once (FR-014, FR-022), so a wave only waits for what it depends on: it
    /// starts no earlier than the end of its command's previous wave, and late enough that each of its Bloomlings reaches
    /// every cell of its route, and its targets, only after that cell's earlier queued change has shown (a tile an
    /// earlier tap clears, a layer or a mystery tile revealed under it), <see cref="StepMargin"/> later. Its end events
    /// keep the rules' order: a special's progress and trigger, and a pod's completion, end after the last earlier wave
    /// that changed the same special or that pod's work, and the level's outcome (won, jammed, stuck) after every earlier
    /// wave, so its card waits for the last wave. Wave starts, arrivals and wave ends happen in time order, each at its
    /// own moment of the clock.
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
        // The clearing pace, halved on the owner's request of 2026-10-03 (it was 0.07 s a step and waves of 0.3–1.4 s).
        public const float StepSeconds = 0.14f;
        public const float MinWaveSeconds = 0.6f;
        public const float MaxWaveSeconds = 2.8f;

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
        private ITimelineSink? _sink;

        public TimelinePlayer()
        {
            ForgetReady();
        }

        /// <summary>1 or 2 (the 2× toggle).</summary>
        public float Speed { get; set; } = 1f;

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

                        float travel = Math.Max(1, clear.RouteFromEntry.Count) * StepSeconds;
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

            float earliest = Now;
            foreach (Wave w in waves)
            {
                Plan(w);
                w.StartAt = StartOf(w, earliest);
                Record(w);
                _waves.Add(w);
                earliest = w.StartAt + w.Duration;
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
        }

        /// <summary>When the last queued wave ends (now, when none is queued).</summary>
        private float LastEnd()
        {
            float end = Now;
            foreach (Wave wave in _waves)
            {
                end = Math.Max(end, wave.StartAt + wave.Duration);
            }

            return end;
        }

        /// <summary>
        /// The wave's walkers and arrival times: in travel order, merged when the wave is larger than the pool or playback
        /// is compressed (a walker follows its farthest unit's route, and all its units arrive with it). A walk is scaled
        /// into the wave, so the walker reaches its target as its tiles change.
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

            var arrival = new Dictionary<WorkUnit, float>();
            foreach (List<WorkUnit> batch in batches)
            {
                float travel = Math.Min(batch[batch.Count - 1].TravelSeconds, wave.Duration - RestoreSeconds);
                wave.Walks.Add(new Walk(batch, travel));
                foreach (WorkUnit unit in batch)
                {
                    arrival[unit] = travel;
                }
            }

            StableSort(byTravel, unit => arrival[unit]);
            wave.Work.Clear();
            wave.Work.AddRange(byTravel);
            foreach (WorkUnit unit in byTravel)
            {
                wave.Arrivals.Add(arrival[unit]);
            }
        }

        /// <summary>
        /// The wave's start: no earlier than <paramref name="earliest"/> (the end of its command's previous wave, or now),
        /// and late enough for every change it depends on.
        /// </summary>
        private float StartOf(Wave wave, float earliest)
        {
            float start = earliest;
            foreach (Walk walk in wave.Walks)
            {
                // The walker follows its farthest unit's route and reaches route cell j at (j + 1) / count of its walk:
                // its path is the entry point, then the route cells (BloomlingWorker.Move).
                IReadOnlyList<CellPos> route = walk.Batch[walk.Batch.Count - 1].Clear.RouteFromEntry;
                for (int j = 0; j < route.Count; j++)
                {
                    float reach = walk.Travel * (j + 1) / route.Count;
                    start = Math.Max(start, _ready[Index(route[j])] + StepMargin - reach);
                }

                foreach (WorkUnit unit in walk.Batch)
                {
                    start = Math.Max(start, _ready[Index(unit.Clear.Cell)] + StepMargin - walk.Travel);
                }
            }

            foreach (CellPos cell in CellsAt(wave.Start))
            {
                start = Math.Max(start, _ready[Index(cell)]);
            }

            foreach (CellPos cell in CellsAt(wave.End))
            {
                start = Math.Max(start, _ready[Index(cell)] - wave.Duration);
            }

            foreach (string key in KeysAtEnd(wave))
            {
                if (_keyReady.TryGetValue(key, out float ready))
                {
                    start = Math.Max(start, ready + KeyGap - wave.Duration);
                }
            }

            if (EndsLevel(wave))
            {
                start = Math.Max(start, LastEnd() + KeyGap - wave.Duration);
            }

            return start;
        }

        /// <summary>Notes when the wave's changes show, for the waves queued after it.</summary>
        private void Record(Wave wave)
        {
            float end = wave.StartAt + wave.Duration;
            foreach (string key in KeysAtEnd(wave))
            {
                KeyReady(key, end);
            }

            for (int i = 0; i < wave.Work.Count; i++)
            {
                KeyReady("pod:" + wave.Work[i].Clear.PodId, end);
                int cell = Index(wave.Work[i].Clear.Cell);
                _ready[cell] = Math.Max(_ready[cell], wave.StartAt + wave.Arrivals[i]);
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
                        t = wave.StartAt + wave.Arrivals[wave.NextArrival];
                        k = Arriving;
                    }
                    else
                    {
                        t = wave.StartAt + wave.Duration;
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
                    _sink!.OnWorkStarted(walk.Batch, wave.StartAt, walk.Travel);
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

        /// <summary>One walker: the units it stands for (its farthest last) and its walking time, in timeline seconds.</summary>
        private sealed class Walk
        {
            public Walk(IReadOnlyList<WorkUnit> batch, float travel)
            {
                Batch = batch;
                Travel = travel;
            }

            public IReadOnlyList<WorkUnit> Batch { get; }

            public float Travel { get; }
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

            /// <summary>When it starts on the clock (after its command's earlier waves and every change it depends on).</summary>
            public float StartAt { get; set; }

            public bool Started { get; set; }

            /// <summary>The next of <see cref="Work"/> to arrive.</summary>
            public int NextArrival { get; set; }

            public List<GameEvent> Start { get; } = new List<GameEvent>();

            /// <summary>Its work units in arrival order, each arriving <see cref="Arrivals"/> seconds after the wave starts.</summary>
            public List<WorkUnit> Work { get; } = new List<WorkUnit>();

            public List<float> Arrivals { get; } = new List<float>();

            public List<Walk> Walks { get; } = new List<Walk>();

            public List<GameEvent> End { get; } = new List<GameEvent>();
        }
    }
}
