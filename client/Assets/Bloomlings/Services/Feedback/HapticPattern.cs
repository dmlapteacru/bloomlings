using System.Collections.Generic;

namespace Bloomlings.Client.Services.Feedback
{
    /// <summary>
    /// The transients a haptic pattern is made of, as phones render them crisply: Android's composition primitives
    /// (API 30; the low tick from API 31) and iOS's impact styles. A tick is the lightest, a low tick rounder and
    /// heavier, a click the crispest.
    /// </summary>
    public enum HapticPrimitive
    {
        Tick,
        LowTick,
        Click,
    }

    /// <summary>One transient of a pattern: its primitive, its strength (0–1) and the pause before it.</summary>
    public readonly struct HapticNote
    {
        public HapticNote(HapticPrimitive primitive, float scale, int delayMilliseconds = 0)
        {
            Primitive = primitive;
            Scale = scale;
            DelayMilliseconds = delayMilliseconds;
        }

        public HapticPrimitive Primitive { get; }

        public float Scale { get; }

        public int DelayMilliseconds { get; }
    }

    /// <summary>
    /// A haptic pattern (spec 001 FR-073, spec 005 FR-042) as the platforms play it: <see cref="Notes"/> where the
    /// phone renders composed transients, else one short pulse of <see cref="Milliseconds"/> at
    /// <see cref="Amplitude"/> (1–255) where it has amplitude control. A <see cref="IsMicro"/> pattern (a tile's clear)
    /// is never played as a plain on-off buzz: a phone without amplitude control stays still for it, and the policy
    /// spaces micro patterns out (<see cref="FeedbackPolicy.MicroGapSeconds"/>). Engine-free.
    /// </summary>
    public sealed class HapticPattern
    {
        // The cues' pulses (FR-073 since the audit of 2026-09-29), kept as they were: one-shots of a strength.
        public static readonly HapticPattern Light = new HapticPattern(12, 60, false);

        public static readonly HapticPattern Medium = new HapticPattern(25, 140, false);

        public static readonly HapticPattern Strong = new HapticPattern(60, 255, false);

        /// <summary>
        /// A pod done: two clicks ("ta-dum"), the second the strongest, after its last tile's collect; above every tile's
        /// micro haptic (the owner's stronger tile clicks of 2026-10-08).
        /// </summary>
        public static readonly HapticPattern PodDone = new HapticPattern(
            32, 230, false, new HapticNote(HapticPrimitive.Click, 0.8f), new HapticNote(HapticPrimitive.Click, 1f, 55));

        private HapticPattern(int milliseconds, int amplitude, bool micro, params HapticNote[] notes)
        {
            Milliseconds = milliseconds;
            Amplitude = amplitude;
            IsMicro = micro;
            Notes = notes;
        }

        /// <summary>The composed transients; empty for a plain pulse.</summary>
        public IReadOnlyList<HapticNote> Notes { get; }

        /// <summary>The single pulse's length where transients cannot be composed.</summary>
        public int Milliseconds { get; }

        /// <summary>The single pulse's amplitude, 1–255.</summary>
        public int Amplitude { get; }

        /// <summary>A tile's micro haptic: spaced out, and skipped on a phone that can only buzz.</summary>
        public bool IsMicro { get; }

        /// <summary>About how long the pattern lasts, in seconds (each transient counted as 10 ms).</summary>
        public double Seconds
        {
            get
            {
                if (Notes.Count == 0)
                {
                    return Milliseconds / 1000.0;
                }

                int ms = 0;
                foreach (HapticNote note in Notes)
                {
                    ms += note.DelayMilliseconds + 10;
                }

                return ms / 1000.0;
            }
        }

        /// <summary>A tile's micro haptic: its transients and the short, soft pulse that stands in for them.</summary>
        public static HapticPattern Micro(int milliseconds, int amplitude, params HapticNote[] notes) =>
            new HapticPattern(milliseconds, amplitude, true, notes);
    }
}
