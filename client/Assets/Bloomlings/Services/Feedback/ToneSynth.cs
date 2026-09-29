using System;
using System.Collections.Generic;

namespace Bloomlings.Client.Services.Feedback
{
    /// <summary>The game's sound effects; each has a short synthesized clip (<see cref="ToneSynth"/>).</summary>
    public enum SoundCue
    {
        /// <summary>A UI button.</summary>
        Click,

        /// <summary>A pod committed from the tray to a slot.</summary>
        Tap,

        /// <summary>A tap the rules refused (no free slot, locked, buried).</summary>
        Refused,

        /// <summary>A Bloomling restored a tile.</summary>
        Clear,

        /// <summary>A pod finished its work and left its slot.</summary>
        PodDone,

        /// <summary>A key was collected and its lock opened.</summary>
        Key,

        /// <summary>A gate, heavy blocker or Fountain triggered.</summary>
        Special,

        /// <summary>A booster was used.</summary>
        Booster,

        /// <summary>The slots jammed, or no pod can move.</summary>
        Jam,

        /// <summary>The level is won.</summary>
        Win,
    }

    /// <summary>
    /// Simple procedural audio, so the game has sound without audio assets (FR-073): short tone blips for the cues and
    /// a soft pentatonic loop for the music. Mono float samples in [-1, 1] at <see cref="SampleRate"/>, every clip fading
    /// in and out so it never clicks. Engine-free and deterministic; the client turns the samples into AudioClips.
    /// </summary>
    public static class ToneSynth
    {
        public const int SampleRate = 22050;

        /// <summary>The music loop's tempo; 8 bars of 4 beats.</summary>
        public const int MusicBeatsPerMinute = 96;

        public const int MusicBars = 8;

        private enum Wave
        {
            Sine,
            Triangle,
        }

        private readonly struct Note
        {
            public Note(double frequency, double seconds, double volume, Wave wave = Wave.Sine, double slideTo = 0)
            {
                Frequency = frequency;
                Seconds = seconds;
                Volume = volume;
                Shape = wave;
                SlideTo = slideTo;
            }

            public double Frequency { get; }

            public double Seconds { get; }

            public double Volume { get; }

            public Wave Shape { get; }

            /// <summary>A frequency to glide to over the note, or 0 for none.</summary>
            public double SlideTo { get; }
        }

        public static float[] Cue(SoundCue cue) => cue switch
        {
            SoundCue.Click => Sequence(new Note(880, 0.04, 0.25)),
            SoundCue.Tap => Sequence(new Note(660, 0.07, 0.35, Wave.Triangle, 880)),
            SoundCue.Refused => Sequence(new Note(220, 0.09, 0.35, Wave.Triangle), new Note(196, 0.12, 0.35, Wave.Triangle)),
            SoundCue.Clear => Sequence(new Note(1200, 0.035, 0.18)),
            SoundCue.PodDone => Sequence(new Note(1047, 0.08, 0.3), new Note(1319, 0.12, 0.3)),
            SoundCue.Key => Sequence(new Note(784, 0.06, 0.3), new Note(880, 0.06, 0.3), new Note(1175, 0.14, 0.3)),
            SoundCue.Special => Sequence(new Note(523, 0.25, 0.3, Wave.Triangle, 1047)),
            SoundCue.Booster => Sequence(new Note(300, 0.18, 0.3, Wave.Triangle, 900)),
            SoundCue.Jam => Sequence(new Note(523, 0.11, 0.35, Wave.Triangle), new Note(392, 0.11, 0.35, Wave.Triangle), new Note(330, 0.2, 0.35, Wave.Triangle)),
            _ => Sequence(new Note(523, 0.11, 0.4), new Note(659, 0.11, 0.4), new Note(784, 0.11, 0.4), new Note(1047, 0.3, 0.4)),
        };

        /// <summary>
        /// The music loop: a gentle C-major-pentatonic arpeggio in eighth notes over a root note per bar. Its length is
        /// exactly <see cref="MusicBars"/> bars, so it loops without a seam.
        /// </summary>
        public static float[] MusicLoop()
        {
            double beat = 60.0 / MusicBeatsPerMinute;
            int length = (int)Math.Round(MusicBars * 4 * beat * SampleRate);
            var samples = new float[length];
            double[] roots = { 130.81, 110.00, 146.83, 98.00, 130.81, 110.00, 146.83, 98.00 }; // C3 A2 D3 G2
            int[] steps = { 0, 2, 4, 7, 9, 7, 4, 2 }; // semitones over the root: a pentatonic rise and fall
            for (int bar = 0; bar < MusicBars; bar++)
            {
                double root = roots[bar];
                Mix(samples, Render(new Note(root, beat * 4, 0.10, Wave.Sine)), (int)(bar * 4 * beat * SampleRate));
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    double frequency = root * 4 * Math.Pow(2, steps[(eighth + bar) % steps.Length] / 12.0);
                    int start = (int)(((bar * 4) + (eighth * 0.5)) * beat * SampleRate);
                    Mix(samples, Render(new Note(frequency, beat * 0.45, 0.07, Wave.Triangle)), start);
                }
            }

            return samples;
        }

        private static float[] Sequence(params Note[] notes)
        {
            var parts = new List<float[]>();
            int length = 0;
            foreach (Note note in notes)
            {
                float[] part = Render(note);
                parts.Add(part);
                length += part.Length;
            }

            var samples = new float[length];
            int at = 0;
            foreach (float[] part in parts)
            {
                Array.Copy(part, 0, samples, at, part.Length);
                at += part.Length;
            }

            return samples;
        }

        /// <summary>One note with a 5 ms attack and an exponential decay that ends at silence.</summary>
        private static float[] Render(Note note)
        {
            int length = Math.Max(1, (int)(note.Seconds * SampleRate));
            int attack = Math.Min(length / 2, (int)(0.005 * SampleRate));
            var samples = new float[length];
            double phase = 0;
            for (int i = 0; i < length; i++)
            {
                double t = (double)i / length;
                double frequency = note.SlideTo > 0 ? note.Frequency + ((note.SlideTo - note.Frequency) * t) : note.Frequency;
                phase += 2 * Math.PI * frequency / SampleRate;
                double wave = note.Shape == Wave.Sine ? Math.Sin(phase) : (2 / Math.PI) * Math.Asin(Math.Sin(phase));
                double envelope = i < attack ? (double)i / attack : Math.Exp(-5 * t) * (1 - t);
                samples[i] = (float)(wave * envelope * note.Volume);
            }

            return samples;
        }

        private static void Mix(float[] into, float[] part, int start)
        {
            for (int i = 0; i < part.Length && start + i < into.Length; i++)
            {
                into[start + i] = Math.Max(-1f, Math.Min(1f, into[start + i] + part[i]));
            }
        }
    }
}
