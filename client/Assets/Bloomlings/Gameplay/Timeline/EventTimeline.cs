using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;
using UnityEngine;

namespace Bloomlings.Client.Gameplay.Timeline
{
    /// <summary>One work unit to animate: a TileCleared and the reveal that follows it (CellOpened or LayerRevealed).</summary>
    public sealed class WorkUnit
    {
        public WorkUnit(TileCleared clear, GameEvent? reveal, float travelSeconds)
        {
            Clear = clear;
            Reveal = reveal;
            TravelSeconds = travelSeconds;
        }

        public TileCleared Clear { get; }

        public GameEvent? Reveal { get; }

        /// <summary>Walking time along the route, in timeline seconds.</summary>
        public float TravelSeconds { get; }
    }

    /// <summary>Receives the timeline's visual cues.</summary>
    public interface ITimelineSink
    {
        /// <summary>A Bloomling sets off for one or more units (several when walkers are merged).</summary>
        void OnWorkStarted(IReadOnlyList<WorkUnit> batch, float travelSeconds);

        /// <summary>A Bloomling reached its tile: apply the clear and the reveal.</summary>
        void OnWorkArrived(WorkUnit unit);

        /// <summary>Any other event, at the start of its wave (mystery tile reveals) or at its end (all others).</summary>
        void OnEvent(GameEvent e);
    }

    /// <summary>
    /// Plays event logs as visual waves, one per settle round (research R4, T045). 2× speed only scales the timeline
    /// (FR-069). When the pending visual time exceeds the backlog threshold (Remote Config
    /// <c>fx.backlogThresholdMs</c>, default 1500) playback speeds up to 4× and walkers are merged. Input never waits
    /// for the timeline: taps apply to the logical state at once (FR-016, SC-008).
    /// </summary>
    public sealed class EventTimeline : MonoBehaviour
    {
        public const float StepSeconds = 0.07f;
        public const float RestoreSeconds = 0.22f;
        public const float MinWaveSeconds = 0.3f;
        public const float MaxWaveSeconds = 1.4f;
        public const float MaxRate = 4f;

        private readonly Queue<Wave> _waves = new Queue<Wave>();
        private Wave? _current;
        private float _time;
        private int _nextArrival;
        private ITimelineSink? _sink;

        /// <summary>1 or 2 (the 2× toggle).</summary>
        public float Speed { get; set; } = 1f;

        public float BacklogThresholdSeconds { get; set; } = 1.5f;

        /// <summary>Pauses playback (pause screen, app in background); resumes exactly where it stopped.</summary>
        public bool Paused { get; set; }

        /// <summary>Most walkers shown at once; a larger wave merges walkers (low-end cap, R4).</summary>
        public int WorkerCapacity { get; set; } = 60;

        /// <summary>Current playback rate, including backlog compression.</summary>
        public float Rate { get; private set; } = 1f;

        public bool IsIdle => _current == null && _waves.Count == 0;

        /// <summary>Pending visual time in timeline seconds.</summary>
        public float Backlog
        {
            get
            {
                float total = _current == null ? 0f : Mathf.Max(0f, _current.Duration - _time);
                foreach (Wave wave in _waves)
                {
                    total += wave.Duration;
                }

                return total;
            }
        }

        public void Bind(ITimelineSink sink) => _sink = sink;

        /// <summary>Queues the settle events of one command (rounds ≥ 1; round 0 is applied by the caller at once).</summary>
        public void Enqueue(IReadOnlyList<GameEvent> events)
        {
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
                    _waves.Enqueue(wave);
                }

                switch (e)
                {
                    case TileCleared clear:
                        GameEvent? reveal = null;
                        if (i + 1 < events.Count && IsRevealOf(events[i + 1], clear.Cell))
                        {
                            reveal = events[++i];
                        }

                        float travel = clear.RouteFromEntry.Count * StepSeconds;
                        wave.Work.Add(new WorkUnit(clear, reveal, travel));
                        wave.Duration = Mathf.Clamp(Mathf.Max(wave.Duration, travel + RestoreSeconds), MinWaveSeconds, MaxWaveSeconds);
                        break;
                    case MysteryTileRevealed _:
                        wave.Start.Add(e);
                        break;
                    default:
                        wave.End.Add(e);
                        break;
                }
            }
        }

        /// <summary>
        /// Plays everything pending at once, without walkers (before a booster changes the state, so the board, slots and
        /// tray start from the settled picture). The caller recalls the walkers.
        /// </summary>
        public void Flush()
        {
            if (_sink == null)
            {
                return;
            }

            if (_current != null)
            {
                DeliverArrivals(_current, all: true);
                foreach (GameEvent e in _current.End)
                {
                    _sink.OnEvent(e);
                }

                _current = null;
            }

            while (_waves.Count > 0)
            {
                Wave wave = _waves.Dequeue();
                foreach (GameEvent e in wave.Start)
                {
                    _sink.OnEvent(e);
                }

                foreach (WorkUnit unit in wave.Work)
                {
                    _sink.OnWorkArrived(unit);
                }

                foreach (GameEvent e in wave.End)
                {
                    _sink.OnEvent(e);
                }
            }

            _time = 0f;
        }

        /// <summary>Drops everything pending (restart, leaving the level).</summary>
        public void Clear()
        {
            _waves.Clear();
            _current = null;
            _time = 0f;
        }

        private void Update()
        {
            if (Paused || _sink == null)
            {
                return;
            }

            Rate = Mathf.Min(MaxRate, Speed * Mathf.Max(1f, Backlog / Mathf.Max(0.1f, BacklogThresholdSeconds)));
            float dt = Time.unscaledDeltaTime * Rate;
            while (dt > 0f)
            {
                if (_current == null)
                {
                    if (_waves.Count == 0)
                    {
                        return;
                    }

                    _current = _waves.Dequeue();
                    _time = 0f;
                    _nextArrival = 0;
                    StartWave(_current);
                }

                float step = Mathf.Min(dt, _current.Duration - _time);
                _time += step;
                dt -= step;
                DeliverArrivals(_current);
                if (_time >= _current.Duration)
                {
                    DeliverArrivals(_current, all: true);
                    foreach (GameEvent e in _current.End)
                    {
                        _sink.OnEvent(e);
                    }

                    _current = null;
                }
            }
        }

        private void StartWave(Wave wave)
        {
            foreach (GameEvent e in wave.Start)
            {
                _sink!.OnEvent(e);
            }

            // Arrivals in travel order; merge walkers when the wave is larger than the pool or playback is compressed.
            wave.Work.Sort((a, b) => a.TravelSeconds.CompareTo(b.TravelSeconds));
            int perWalker = Mathf.Max(1, Mathf.CeilToInt(wave.Work.Count / (float)Mathf.Max(1, WorkerCapacity)));
            if (Rate > 2.5f)
            {
                perWalker = Mathf.Max(perWalker, 3);
            }

            var batches = new Dictionary<string, List<WorkUnit>>();
            foreach (WorkUnit unit in wave.Work)
            {
                if (!batches.TryGetValue(unit.Clear.PodId, out List<WorkUnit>? batch) || batch.Count >= perWalker)
                {
                    batch = new List<WorkUnit>();
                    batches[unit.Clear.PodId] = batch;
                    wave.Batches.Add(batch);
                }

                batch.Add(unit);
            }

            foreach (List<WorkUnit> batch in wave.Batches)
            {
                float travel = batch[batch.Count - 1].TravelSeconds;
                foreach (WorkUnit unit in batch)
                {
                    wave.ArrivalTime[unit] = travel;
                }

                _sink!.OnWorkStarted(batch, travel);
            }

            wave.Work.Sort((a, b) => wave.ArrivalTime[a].CompareTo(wave.ArrivalTime[b]));
        }

        private void DeliverArrivals(Wave wave, bool all = false)
        {
            while (_nextArrival < wave.Work.Count && (all || wave.ArrivalTime[wave.Work[_nextArrival]] <= _time))
            {
                _sink!.OnWorkArrived(wave.Work[_nextArrival++]);
            }
        }

        private static bool IsRevealOf(GameEvent e, CellPos cell) =>
            (e is CellOpened opened && opened.Cell == cell) || (e is LayerRevealed layer && layer.Cell == cell);

        private sealed class Wave
        {
            public Wave(int round)
            {
                Round = round;
                Duration = MinWaveSeconds;
            }

            public int Round { get; }

            public float Duration { get; set; }

            public List<GameEvent> Start { get; } = new List<GameEvent>();

            public List<WorkUnit> Work { get; } = new List<WorkUnit>();

            public List<GameEvent> End { get; } = new List<GameEvent>();

            public List<List<WorkUnit>> Batches { get; } = new List<List<WorkUnit>>();

            public Dictionary<WorkUnit, float> ArrivalTime { get; } = new Dictionary<WorkUnit, float>();
        }
    }
}
