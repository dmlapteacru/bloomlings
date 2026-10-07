using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>-- --sounds</c>: writes every synthesized clip (the cues and the clearing sounds, spec 005 FR-042) as a WAV file
    /// to <c>out/sounds/</c>, and <c>schedule.json</c>: for each clearing style, what a pod of eight tiles cleared from one
    /// arch plays and when, at 1× and at fast forward, after the same <see cref="FeedbackPolicy"/> as the game (the
    /// sounds and haptics that are spaced out are left out), for a listening page.
    /// </summary>
    public static class SoundExport
    {
        private static readonly int[] RouteCells = { 3, 4, 5, 3, 6, 4, 5, 3 };

        public static void Write(string outDir)
        {
            string folder = Path.Combine(outDir, "sounds");
            Directory.CreateDirectory(folder);
            foreach (SoundCue cue in (SoundCue[])Enum.GetValues(typeof(SoundCue)))
            {
                File.WriteAllBytes(Path.Combine(folder, "cue-" + cue + ".wav"), Wav(ToneSynth.Cue(cue)));
            }

            foreach (ClearSound sound in ClearSounds.All)
            {
                for (int i = 0; i < ClearSounds.ClipCount(sound); i++)
                {
                    File.WriteAllBytes(Path.Combine(folder, sound + "-" + i + ".wav"), Wav(ToneSynth.Clear(sound, i)));
                }
            }

            var json = new StringBuilder();
            json.Append("{\"styles\":[");
            bool firstStyle = true;
            foreach (ClearStyle style in (ClearStyle[])Enum.GetValues(typeof(ClearStyle)))
            {
                json.Append(firstStyle ? string.Empty : ",");
                firstStyle = false;
                json.Append("{\"style\":\"").Append(style).Append("\",\"speeds\":{");
                json.Append("\"1\":").Append(Schedule(style, 1f)).Append(",\"3\":").Append(Schedule(style, PlaySpeed.Fast));
                json.Append("}}");
            }

            json.Append("]}");
            File.WriteAllText(Path.Combine(folder, "schedule.json"), json.ToString());
            Console.WriteLine("Sounds: " + Directory.GetFiles(folder, "*.wav").Length + " clips and schedule.json in " + folder);
        }

        // One pod of eight tiles from one arch: a walker each line gap, its act's beats, its collect at the clear, the
        // pod's done chord after the last; real time is timeline time over the speed.
        private static string Schedule(ClearStyle style, float speed)
        {
            var events = new List<(double At, string Clip, HapticPattern? Haptic, ClearSound? Sound, SoundCue? Cue)>();
            int total = RouteCells.Length;
            float last = 0f;
            for (int i = 0; i < total; i++)
            {
                int cells = RouteCells[i];
                float start = i * ClearStyles.LineGap;
                float arrival = ClearStyles.TripSeconds(cells);
                var beats = new List<ClearBeat>();
                ClearSounds.Crossed(style, cells, start, arrival, float.NegativeInfinity, float.PositiveInfinity, i, beats);
                foreach (ClearBeat beat in beats)
                {
                    events.Add(((start + (beat.Share * arrival)) / speed, beat.Sound + "-" + beat.Variation, null, beat.Sound, null));
                }

                last = Math.Max(last, start + arrival);
            }

            // The collects climb the pod's ladder in the order they land, as ClearLadder counts them in the game.
            ClearSound collect = ClearSounds.CollectOf(style);
            float[] landings = RouteCells.Select((cells, i) => (i * ClearStyles.LineGap) + ClearStyles.TripSeconds(cells)).OrderBy(t => t).ToArray();
            for (int k = 0; k < landings.Length; k++)
            {
                events.Add((landings[k] / speed, collect + "-" + ClearSounds.Step(k + 1, total), ClearSounds.CollectHaptic(style), collect, null));
            }

            events.Add(((last + 0.15f) / speed, "cue-PodDone", HapticPattern.PodDone, null, SoundCue.PodDone));
            var policy = new FeedbackPolicy(() => new Bloomlings.Client.Services.Save.SettingsData());
            var json = new StringBuilder("[");
            bool first = true;
            foreach (var e in events.OrderBy(e => e.At))
            {
                bool sound = e.Cue.HasValue ? policy.ShouldPlaySound(e.Cue.Value, e.At) : policy.ShouldPlayClear(e.Sound!.Value, e.At);
                HapticPattern? haptic = e.Cue.HasValue ? policy.Haptic(e.Cue.Value, e.At) : e.Haptic != null ? policy.ClearHaptic(style, e.At) : null;
                if (!sound && haptic == null)
                {
                    continue;
                }

                json.Append(first ? string.Empty : ",");
                first = false;
                json.Append("{\"t\":").Append(e.At.ToString("0.###", CultureInfo.InvariantCulture));
                if (sound)
                {
                    json.Append(",\"clip\":\"").Append(e.Clip).Append('"');
                }

                if (haptic != null)
                {
                    json.Append(",\"vibrate\":[").Append(string.Join(",", Pulses(haptic))).Append(']');
                }

                json.Append('}');
            }

            return json.Append(']').ToString();
        }

        // A pattern as a browser's vibration pattern (on, off, on… in ms): each transient a short pulse of its strength.
        private static IEnumerable<int> Pulses(HapticPattern pattern)
        {
            if (pattern.Notes.Count == 0)
            {
                yield return pattern.Milliseconds;
                yield break;
            }

            for (int i = 0; i < pattern.Notes.Count; i++)
            {
                if (i > 0)
                {
                    yield return Math.Max(1, pattern.Notes[i].DelayMilliseconds);
                }

                yield return Math.Max(8, (int)Math.Round(8 + (12 * pattern.Notes[i].Scale)));
            }
        }

        private static byte[] Wav(float[] samples)
        {
            using var stream = new MemoryStream(44 + (samples.Length * 2));
            using var writer = new BinaryWriter(stream);
            int rate = ToneSynth.SampleRate;
            writer.Write("RIFF"u8.ToArray());
            writer.Write(36 + (samples.Length * 2));
            writer.Write("WAVEfmt "u8.ToArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data"u8.ToArray());
            writer.Write(samples.Length * 2);
            foreach (float sample in samples)
            {
                writer.Write((short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
