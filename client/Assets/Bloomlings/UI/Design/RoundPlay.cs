using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// A round level being played on the live timeline (spec 006 T026, FR-003, FR-011, research R2, R8), shared by both
    /// builds' level screens and the playtest check:
    /// <list type="bullet">
    /// <item><description>The display clock advances by real elapsed time × the display speed (×1, the speed-up ×2, Auto
    /// ×2), accumulated as whole µs with the fraction carried, so no frame rate or speed changes an outcome.</description></item>
    /// <item><description>While work is pending that time moves the model clock (<see cref="RoundTimeline.Advance"/>);
    /// while nothing is pending the model clock stands still, as in the reference, and the time is kept apart
    /// (<see cref="IdleUs"/>), so the walkers' and the cubes' effects still play out on the display clock.</description></item>
    /// <item><description>Taps, bridge choices and boosters are committed at the current model time and recorded with it
    /// (<see cref="Inputs"/>); a <see cref="RoundReplay"/> of them gives the same outcome.</description></item>
    /// <item><description>Every event goes to the <see cref="Animator"/>, at its model time plus the idle offset.</description></item>
    /// </list>
    /// Rules come only from <see cref="Bloomlings.Core.Rounds"/>; this class holds none. Engine-free.
    /// </summary>
    public sealed class RoundPlay
    {
        /// <summary>The normal speed-up's display speed (spec 006 assumptions: "the normal speed-up keeps its present ×2").</summary>
        public const int SpeedUp = 2;

        private readonly List<RoundEvent> _fresh = new List<RoundEvent>();
        private readonly List<RoundInput> _inputs = new List<RoundInput>();
        private double _carry;

        public RoundPlay(RoundLevel level, RoundProfile? profile = null)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Profile = profile;
            Timeline = new RoundTimeline(level, profile);
            Animator = new RoundAnimator(level);
            Animator.Reset(Timeline, 0);
        }

        public RoundLevel Level { get; }

        public RoundProfile? Profile { get; }

        /// <summary>The rules (read them; change them only through this class).</summary>
        public RoundTimeline Timeline { get; private set; }

        public RoundAnimator Animator { get; }

        /// <summary>The display time that passed while nothing was pending, in µs.</summary>
        public long IdleUs { get; private set; }

        /// <summary>The display clock in µs: the model time plus <see cref="IdleUs"/>.</summary>
        public long ClockUs => Timeline.Now + IdleUs;

        /// <summary>The commands of this attempt with their model times (a <see cref="RoundReplay"/> plays them again).</summary>
        public IReadOnlyList<RoundInput> Inputs => _inputs;

        /// <summary>The events of the last <see cref="Advance"/>, <see cref="Commit"/> or <see cref="Apply"/> (reused).</summary>
        public IReadOnlyList<RoundEvent> Fresh => _fresh;

        /// <summary>How many booster commands this attempt used.</summary>
        public int BoostersUsed { get; private set; }

        /// <summary>Auto (FR-011, FR-044): every column is empty and no bridge is left to choose; the rest shows at ×2.</summary>
        public bool Auto => Timeline.Tail;

        public RoundStatus Status => Timeline.Status;

        /// <summary>Whether the board has settled: nothing pending and the last effects shown (the end cards may show).</summary>
        public bool Settled => !Timeline.Pending && !Animator.Busy;

        /// <summary>The display speed now: ×2 under Auto, else ×2 with the speed-up on, else ×1. Presentation only.</summary>
        public int Speed(bool speedUp) => Auto ? RoundProfile.TailDisplaySpeed : speedUp ? SpeedUp : 1;

        /// <summary>
        /// Moves the display clock on by <paramref name="seconds"/> of real time at <paramref name="speed"/>: the model time
        /// while work is pending, the idle time otherwise. Returns this step's events (<see cref="Fresh"/>).
        /// </summary>
        public IReadOnlyList<RoundEvent> Advance(double seconds, int speed)
        {
            _fresh.Clear();
            double us = (Math.Max(0.0, seconds) * Math.Max(0, speed) * 1_000_000.0) + _carry;
            long delta = (long)Math.Floor(us);
            _carry = us - delta;
            AdvanceUs(delta);
            return _fresh;
        }

        /// <summary>Moves the display clock on by exactly <paramref name="deltaUs"/> µs (tests and checks).</summary>
        public IReadOnlyList<RoundEvent> AdvanceUs(long deltaUs)
        {
            if (deltaUs <= 0)
            {
                return _fresh;
            }

            long before = Timeline.Now;
            if (Timeline.Pending)
            {
                Timeline.Advance(deltaUs);
                _fresh.AddRange(Timeline.LastEvents);
                Animator.Observe(Timeline.LastEvents, IdleUs);
                Animator.Sync(Timeline);
            }

            IdleUs += deltaUs - (Timeline.Now - before);
            Animator.SetTime(ClockUs);
            return _fresh;
        }

        /// <summary>Why <paramref name="action"/> would be refused now, or <see cref="RoundRefusal.None"/>.</summary>
        public RoundRefusal Check(RoundAction action) => Timeline.Check(action);

        /// <summary>Commits a tap or a bridge at the current model time; an accepted one is recorded and animated.</summary>
        public RoundCommitResult Commit(RoundAction action) =>
            Apply(action.Type == RoundActionType.Tap ? RoundInput.Tap(action.Pod, 0) : RoundInput.Bridge(action.Choice, 0));

        /// <summary>Applies a recorded kind of command (a player action or a booster) at the current model time.</summary>
        public RoundCommitResult Apply(RoundInput command)
        {
            _fresh.Clear();
            RoundInput at = command.At(Timeline.Now);
            RoundCommitResult result = RoundReplay.Apply(Timeline, at);
            if (result.Accepted)
            {
                _inputs.Add(at);
                if (at.Type != RoundInputType.Tap && at.Type != RoundInputType.Bridge)
                {
                    BoostersUsed++;
                }

                _fresh.AddRange(result.Events);
                Animator.Observe(result.Events, IdleUs);
                Animator.Sync(Timeline);
                Animator.SetTime(ClockUs);
            }

            return result;
        }

        /// <summary>A fresh attempt of the same level (the display clock keeps running, so effects never jump back).</summary>
        public void Restart()
        {
            long clock = ClockUs;
            Timeline = new RoundTimeline(Level, Profile);
            IdleUs = clock;
            _inputs.Clear();
            _fresh.Clear();
            BoostersUsed = 0;
            Animator.Reset(Timeline, clock);
        }
    }
}
