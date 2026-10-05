using System.Collections.Generic;
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

        /// <summary>Walking time along the route at the timeline's pace, in timeline seconds.</summary>
        public float TravelSeconds { get; }
    }

    /// <summary>Receives the timeline's visual cues.</summary>
    public interface ITimelineSink
    {
        /// <summary>
        /// A Bloomling sets off for one or more units (several when walkers are merged) at <paramref name="start"/> on the
        /// timeline clock (<see cref="EventTimeline.Now"/>) and reaches its target <paramref name="travelSeconds"/> later,
        /// when its units arrive. Its path is the entry point, then the route of the batch's last unit.
        /// </summary>
        void OnWorkStarted(IReadOnlyList<WorkUnit> batch, float start, float travelSeconds);

        /// <summary>A Bloomling reached its tile: apply the clear and the reveal.</summary>
        void OnWorkArrived(WorkUnit unit);

        /// <summary>Any other event, at the start of its wave (mystery tile reveals) or at its end (all others).</summary>
        void OnEvent(GameEvent e);
    }

    /// <summary>
    /// Plays event logs as visual waves, one per settle round (research R4, T045), on the Unity frame clock; the
    /// scheduling lives in the engine-free <see cref="TimelinePlayer"/>. The waves of one command play one after another,
    /// the waves of different commands side by side, each as soon as what it depends on has shown (the owner's report of
    /// 2026-10-03). 2× speed only scales the timeline (FR-069). When the time until every queued wave has played exceeds
    /// the backlog threshold (Remote Config <c>fx.backlogThresholdMs</c>) playback speeds up to 4× and walkers are merged.
    /// Input never waits for the timeline: taps apply to the logical state at once (FR-016, SC-008). It updates before
    /// the walkers, so each frame draws them at the clock the timeline delivers that frame's arrivals by.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class EventTimeline : MonoBehaviour
    {
        // The clearing pace, halved on the owner's requests of 2026-10-03 and 2026-10-05 (TimelinePlayer).
        public const float StepSeconds = TimelinePlayer.StepSeconds;
        public const float MinWaveSeconds = TimelinePlayer.MinWaveSeconds;
        public const float MaxWaveSeconds = TimelinePlayer.MaxWaveSeconds;

        public const float RestoreSeconds = TimelinePlayer.RestoreSeconds;
        public const float MaxRate = TimelinePlayer.MaxRate;

        private readonly TimelinePlayer _player = new TimelinePlayer();

        /// <summary>1 or 2 (the 2× toggle).</summary>
        public float Speed
        {
            get => _player.Speed;
            set => _player.Speed = value;
        }

        /// <summary>The backlog beyond which playback speeds up; defaults to the bundled <c>fx.backlogThresholdMs</c>.</summary>
        public float BacklogThresholdSeconds
        {
            get => _player.BacklogThresholdSeconds;
            set => _player.BacklogThresholdSeconds = value;
        }

        /// <summary>Pauses playback (pause screen, app in background); resumes exactly where it stopped.</summary>
        public bool Paused { get; set; }

        /// <summary>Most walkers one wave shows; a larger wave merges walkers (low-end cap, R4).</summary>
        public int WorkerCapacity
        {
            get => _player.WorkerCapacity;
            set => _player.WorkerCapacity = value;
        }

        /// <summary>Current playback rate, including backlog compression.</summary>
        public float Rate => _player.Rate;

        /// <summary>The timeline clock in timeline seconds: a walker's progress is the time since it set off on it.</summary>
        public float Now => _player.Now;

        public bool IsIdle => _player.IsIdle;

        /// <summary>Time until every queued wave has played, in timeline seconds.</summary>
        public float Backlog => _player.Backlog;

        public void Bind(ITimelineSink sink) => _player.Bind(sink);

        /// <summary>Queues the settle events of one command (rounds ≥ 1; round 0 is applied by the caller at once).</summary>
        public void Enqueue(IReadOnlyList<GameEvent> events) => _player.Enqueue(events);

        /// <summary>
        /// Plays everything pending at once, without walkers (before a booster changes the state, so the board, slots and
        /// tray start from the settled picture). The caller recalls the walkers.
        /// </summary>
        public void Flush() => _player.Flush();

        /// <summary>Drops everything pending (restart, leaving the level).</summary>
        public void Clear() => _player.Clear();

        private void Update()
        {
            if (!Paused)
            {
                _player.Advance(Time.unscaledDeltaTime);
            }
        }
    }
}
