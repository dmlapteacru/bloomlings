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

        /// <summary>A tile cleared, in no clearing style (the level tester); the levels play their style's collect (<see cref="ClearSound"/>).</summary>
        Clear,

        /// <summary>A pod finished its work and left its slot: a kalimba arpeggio that resolves its ladder.</summary>
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
            SoundCue.Clear => Clear(ClearSound.BlossomPluck, 4),
            SoundCue.PodDone => PodDoneChord(),
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

        // ---- The clearing sounds (spec 005 FR-042, contracts/look.md §6.16) ----

        /// <summary>
        /// A clearing sound's clip: a collect on ladder step <paramref name="index"/> (0 to <see cref="ClearSounds.Steps"/>
        /// - 1), a texture in variation <paramref name="index"/> (0 to <see cref="ClearSounds.Variations"/> - 1). Kind to a
        /// phone's speaker and to the ear in long runs: rounded attacks of 1–5 ms, decays of a tenth to a third of a
        /// second, a few soft partials or filtered noise, nothing harsh and nothing that rings on. Each clip is scaled to
        /// its sound's peak, so a style's collect sits over its textures and the cues over both.
        /// </summary>
        public static float[] Clear(ClearSound sound, int index)
        {
            int i = Math.Max(0, Math.Min(ClearSounds.ClipCount(sound) - 1, index));
            double f = ClearSounds.IsCollect(sound) ? ClearSounds.Ladder[i] : 0;
            float[] s;
            switch (sound)
            {
                case ClearSound.BlossomPluck:
                    // A kalimba: the note and a quick, soft tine overtone.
                    s = Buffer(0.42);
                    Voice(s, 0, f, f, 0, 0.0025, P(1, 1, 0.18), P(2, 0.05, 0.08), P(3, 0.1, 0.04));
                    return Finish(s, 0.24);
                case ClearSound.MunchGulp:
                    // A round "bloop" an octave down, rising into its note.
                    s = Buffer(0.26);
                    Voice(s, 0, f * 0.35, f * 0.5, 0.03, 0.004, P(1, 1, 0.1), P(2, 0.25, 0.05));
                    return Finish(s, 0.26);
                case ClearSound.FireflyChime:
                    // A small glass bell: inharmonic partials that fade faster the higher they are.
                    s = Buffer(0.6);
                    Voice(s, 0, f, f, 0, 0.0015, P(1, 1, 0.32), P(2.76, 0.22, 0.11), P(5.4, 0.07, 0.05), P(8.93, 0.025, 0.03));
                    return Finish(s, 0.22);
                case ClearSound.BubblePop:
                    // A water drop: a quick rise into the note, with the faint click of the pop.
                    s = Buffer(0.18);
                    Voice(s, 0, f * 0.55, f * 1.05, 0.012, 0.0015, P(1, 1, 0.055), P(2, 0.08, 0.03));
                    Noise(s, 0, 0.004, 3000, 3000, 1.4, 0.0005, 0.0015, 0.25, 11, false);
                    return Finish(s, 0.27);
                case ClearSound.PushPlonk:
                    // A marimba bar an octave down over a soft thump of the landing.
                    s = Buffer(0.36);
                    Voice(s, 0, f * 0.5, f * 0.5, 0, 0.002, P(1, 1, 0.16), P(3.93, 0.22, 0.03), P(9, 0.05, 0.008));
                    Voice(s, 0, 110, 80, 0.03, 0.002, P(1, 0.35, 0.04));
                    return Finish(s, 0.3);
                case ClearSound.FireworkSparkle:
                    // Two bell pings, the second an octave up.
                    s = Buffer(0.42);
                    Voice(s, 0, f, f, 0, 0.0015, P(1, 1, 0.12), P(2.76, 0.15, 0.05));
                    Voice(s, 0.045, f * 2, f * 2, 0, 0.0015, P(1, 0.7, 0.1), P(2.76, 0.1, 0.04));
                    return Finish(s, 0.24);
                case ClearSound.ParadeBoing:
                {
                    // A spring rising into the note with a wobble that settles, then a little confetti crackle.
                    s = Buffer(0.32);
                    Voice(s, 0, f * 0.5, f, 0.02, 0.003, 16, 0.06, 0.08, P(1, 1, 0.12), P(2, 0.12, 0.06));
                    double[] at = { 0.03, 0.055, 0.085, 0.12 };
                    for (int k = 0; k < at.Length; k++)
                    {
                        Noise(s, at[k], 0.006, 2500, 2500, 1.2, 0.0005, 0.002, 0.18 - (0.035 * k), 21 + k, false);
                    }

                    return Finish(s, 0.26);
                }

                case ClearSound.BlossomFwip:
                    // A breath that rises, and a soft glide under it.
                    s = Buffer(0.2);
                    Noise(s, 0, 0.17, 700 * (1 + (0.08 * i)), 2200 * (1 + (0.08 * i)), 1.0, 0.05, 0.06, 1, 31 + i, false);
                    Voice(s, 0, 600, 900, 0.05, 0.03, P(1, 0.35, 0.07));
                    return Finish(s, 0.12);
                case ClearSound.MunchCrunch:
                {
                    // Three tiny grains of crunch on a soft thump, a little higher for each bite.
                    s = Buffer(0.1);
                    double[] grains = { 0, 0.011, 0.024 };
                    for (int g = 0; g < grains.Length; g++)
                    {
                        Noise(s, grains[g], 0.02, 2000 + (250 * i), 2000 + (250 * i), 1.2, 0.0008, 0.006, 1 - (0.2 * g), 41 + (3 * i) + g, false);
                    }

                    double thump = 170 + (20 * i);
                    Voice(s, 0, thump * 1.3, thump, 0.01, 0.0015, P(1, 0.7, 0.03));
                    return Finish(s, 0.16);
                }

                case ClearSound.FireflyTwinkle:
                {
                    // Three tiny high pings of the scale (E6, G6, A6), in an order of the variation.
                    s = Buffer(0.2);
                    double[] notes = { 1318.51, 1567.98, 1760.00 };
                    for (int k = 0; k < notes.Length; k++)
                    {
                        double n = notes[(k + i) % notes.Length];
                        Voice(s, 0.035 * k, n, n, 0, 0.001, P(1, 1, 0.05), P(2.76, 0.1, 0.02));
                    }

                    return Finish(s, 0.11);
                }

                case ClearSound.BubbleBlow:
                    // Air filling the bubble: a soft rising "fwoop" with a little breath.
                    s = Buffer(0.24);
                    Voice(s, 0, 220, 420 + (25 * i), 0.06, 0.04, P(1, 1, 0.09));
                    Noise(s, 0, 0.2, 900, 900, 1.0, 0.04, 0.07, 0.25, 51 + i, true);
                    return Finish(s, 0.12);
                case ClearSound.PushKnock:
                {
                    // A small wood block: a dull knock, its two wooden partials and a tick of contact.
                    s = Buffer(0.08);
                    double k = 480 + (40 * i);
                    Voice(s, 0, k * 1.15, k, 0.005, 0.0008, P(1, 1, 0.018), P(1.58, 0.45, 0.009), P(2.5, 0.15, 0.005));
                    Noise(s, 0, 0.004, 2500, 2500, 1.5, 0.0004, 0.0015, 0.3, 61 + i, false);
                    return Finish(s, 0.14);
                }

                case ClearSound.FireworkFizz:
                    // A quiet fuse: noise rising in pitch and loudness, cut as the burst comes.
                    s = Buffer(0.3);
                    Noise(s, 0, 0.3, 800 + (100 * i), 2400 + (150 * i), 1.4, 0.27, 0, 1, 71 + i, false);
                    return Finish(s, 0.07);
                case ClearSound.FireworkBang:
                    // A soft, low "pomf": muffled noise over a falling thump, never a crack.
                    s = Buffer(0.16);
                    Noise(s, 0, 0.12, 1100 + (100 * i), 1100 + (100 * i), 1.0, 0.001, 0.025, 0.6, 81 + i, true);
                    Voice(s, 0, 140, 85, 0.04, 0.002, P(1, 1, 0.05));
                    return Finish(s, 0.2);
                default:
                {
                    // ParadeHop: a tiny "pip", the second hop higher.
                    s = Buffer(0.06);
                    double n = 880 + (170 * i);
                    Voice(s, 0, n * 0.92, n, 0.008, 0.001, P(1, 1, 0.02));
                    return Finish(s, 0.08);
                }
            }
        }

        /// <summary>A pod done: a quick kalimba arpeggio, C5 E5 G5 C6, that resolves its ladder on the tonic.</summary>
        private static float[] PodDoneChord()
        {
            float[] s = Buffer(0.62);
            double[] notes = { 523.25, 659.26, 783.99, 1046.50 };
            for (int k = 0; k < notes.Length; k++)
            {
                Voice(s, 0.055 * k, notes[k], notes[k], 0, 0.002, P(1, 0.8 + (0.066 * k), 0.25), P(3, 0.08, 0.04));
            }

            return Finish(s, 0.38);
        }

        // A partial of a voice: its frequency as a ratio of the voice's, its amplitude and its decay's time constant.
        private readonly struct Partial
        {
            public Partial(double ratio, double amplitude, double tau)
            {
                Ratio = ratio;
                Amplitude = amplitude;
                Tau = tau;
            }

            public double Ratio { get; }

            public double Amplitude { get; }

            public double Tau { get; }
        }

        private static Partial P(double ratio, double amplitude, double tau) => new Partial(ratio, amplitude, tau);

        private static float[] Buffer(double seconds) => new float[(int)(seconds * SampleRate)];

        private static void Voice(float[] into, double at, double from, double to, double glide, double attack, params Partial[] partials) =>
            Voice(into, at, from, to, glide, attack, 0, 0, 0, partials);

        /// <summary>
        /// Adds a voice from <paramref name="at"/> seconds: its frequency glides from <paramref name="from"/> towards
        /// <paramref name="to"/> with the time constant <paramref name="glide"/> (0 for none), with a vibrato of
        /// <paramref name="vibratoHz"/> and relative depth <paramref name="depth"/> that dies away over
        /// <paramref name="vibratoTau"/>; a linear attack, then each partial decaying on its own. Partials at or above
        /// 0.45 of the sample rate are left out, so nothing aliases.
        /// </summary>
        private static void Voice(float[] into, double at, double from, double to, double glide, double attack, double vibratoHz, double depth, double vibratoTau, params Partial[] partials)
        {
            int start = (int)(at * SampleRate);
            var phase = new double[partials.Length];
            var level = new double[partials.Length];
            var decay = new double[partials.Length];
            double longest = 0;
            for (int k = 0; k < partials.Length; k++)
            {
                level[k] = partials[k].Amplitude;
                decay[k] = Math.Exp(-1.0 / (partials[k].Tau * SampleRate));
                longest = Math.Max(longest, partials[k].Tau);
            }

            int end = Math.Min(into.Length, start + (int)((attack + (longest * 9.5)) * SampleRate));
            for (int n = start; n < end; n++)
            {
                double t = (double)(n - start) / SampleRate;
                double frequency = glide > 0 ? to + ((from - to) * Math.Exp(-t / glide)) : from;
                if (vibratoHz > 0)
                {
                    frequency *= 1 + (depth * Math.Exp(-t / vibratoTau) * Math.Sin(2 * Math.PI * vibratoHz * t));
                }

                double sum = 0;
                for (int k = 0; k < partials.Length; k++)
                {
                    double fk = frequency * partials[k].Ratio;
                    level[k] *= decay[k];
                    if (fk >= 0.45 * SampleRate)
                    {
                        continue;
                    }

                    phase[k] += 2 * Math.PI * fk / SampleRate;
                    sum += level[k] * Math.Sin(phase[k]);
                }

                double envelope = attack > 0 && t < attack ? t / attack : 1;
                into[n] += (float)(sum * envelope);
            }
        }

        /// <summary>
        /// Adds <paramref name="seconds"/> of filtered white noise from <paramref name="at"/>: band-passed (or low-passed)
        /// round a center gliding from <paramref name="from"/> to <paramref name="to"/> Hz (a state-variable filter of
        /// damping <paramref name="damping"/>, kept under a sixth of the sample rate so it stays stable), with a linear
        /// attack, a decay of time constant <paramref name="tau"/> (0 for none) and a 10 ms fade at its end. The noise
        /// is a seeded generator, so every clip is the same each time.
        /// </summary>
        private static void Noise(float[] into, double at, double seconds, double from, double to, double damping, double attack, double tau, double amplitude, int seed, bool lowpass)
        {
            int start = (int)(at * SampleRate);
            int length = (int)(seconds * SampleRate);
            uint state = ((uint)seed * 2654435761u) | 1u;
            double low = 0;
            double band = 0;
            double fade = 0.01;
            for (int j = 0; j < length && start + j < into.Length; j++)
            {
                double t = (double)j / SampleRate;
                double center = Math.Min(SampleRate / 6.5, from + ((to - from) * (j / (double)length)));
                double fq = 2 * Math.Sin(Math.PI * center / SampleRate);
                state = (state * 1664525u) + 1013904223u;
                double x = ((state >> 8) / 16777216.0 * 2) - 1;
                low += fq * band;
                double high = x - low - (damping * band);
                band += fq * high;
                double envelope = (attack > 0 && t < attack ? t / attack : 1) * (tau > 0 ? Math.Exp(-t / tau) : 1);
                double left = seconds - t;
                if (left < fade)
                {
                    envelope *= left / fade;
                }

                into[start + j] += (float)((lowpass ? low : band) * envelope * amplitude);
            }
        }

        /// <summary>Scales a clip to its peak and fades its first millisecond and last 6 ms, so it never clicks.</summary>
        private static float[] Finish(float[] samples, double peak)
        {
            float max = 0f;
            foreach (float v in samples)
            {
                max = Math.Max(max, Math.Abs(v));
            }

            double scale = max > 0f ? peak / max : 0;
            int fadeIn = Math.Min(samples.Length / 4, (int)(0.001 * SampleRate));
            int fadeOut = Math.Min(samples.Length / 4, (int)(0.006 * SampleRate));
            for (int n = 0; n < samples.Length; n++)
            {
                double gain = scale;
                if (n < fadeIn)
                {
                    gain *= (double)n / fadeIn;
                }

                int fromEnd = samples.Length - 1 - n;
                if (fromEnd < fadeOut)
                {
                    gain *= (double)fromEnd / fadeOut;
                }

                samples[n] = (float)(samples[n] * gain);
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
