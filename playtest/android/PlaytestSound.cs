using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Android.OS;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// Sound and haptics for the playtest client: the Unity client's synthesized cues and clearing sounds
    /// (<see cref="ToneSynth"/>, spec 005 FR-042, linked from <c>client/</c>) written once as WAV files to the cache and
    /// played through a SoundPool, so several overlap, and the haptic patterns (<see cref="HapticPattern"/>) on the
    /// vibrator, as the Unity client's <c>Haptics</c> plays them: composed transients where the phone renders them
    /// (API 30), the predefined tick or click for a tile (API 29), a short pulse where it has amplitude control, and no
    /// tile haptic on a phone that can only buzz. The Unity client's <see cref="FeedbackPolicy"/> decides what plays.
    /// Presentation only.
    /// </summary>
    public sealed class PlaytestSound
    {
        // The pool's voices; a new sound takes the lowest-priority one when all play (textures 1, collects 2, cues 3).
        private const int Voices = 10;

        private readonly SettingsData _settings = new SettingsData { Music = false };
        private readonly FeedbackPolicy _policy;
        private readonly SoundPool? _pool;
        private readonly Dictionary<string, int> _ids = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<int> _loaded = new HashSet<int>();
        private readonly Vibrator? _vibrator;
        private readonly bool _amplitude;
        private readonly bool _primitives;
        private readonly bool _lowTick;

        public PlaytestSound(Context context)
        {
            _policy = new FeedbackPolicy(() => _settings);
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                _vibrator = (context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator;
            }
            else
            {
#pragma warning disable CA1422, CS0618 // The pre-Android 12 vibrator service.
                _vibrator = context.GetSystemService(Context.VibratorService) as Vibrator;
#pragma warning restore CA1422, CS0618
            }

            try
            {
                if (_vibrator != null && _vibrator.HasVibrator)
                {
                    _amplitude = _vibrator.HasAmplitudeControl;
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        _primitives = _vibrator.AreAllPrimitivesSupported((int)VibrationEffectCompositionPrimitive.Tick, (int)VibrationEffectCompositionPrimitive.Click);
                    }

                    if (_primitives && OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        _lowTick = _vibrator.AreAllPrimitivesSupported((int)VibrationEffectCompositionPrimitive.LowTick);
                    }
                }

                _pool = new SoundPool.Builder()
                    .SetMaxStreams(Voices)!
                    .SetAudioAttributes(new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Sonification)!.Build())!
                    .Build()!;
                _pool.LoadComplete += (_, e) =>
                {
                    if (e.Status == 0)
                    {
                        lock (_loaded)
                        {
                            _loaded.Add(e.SampleId);
                        }
                    }
                };
                string folder = Path.Combine(context.CacheDir!.AbsolutePath, "sounds");
                Task.Run(() => LoadAll(folder));
            }
            catch (Java.Lang.Exception)
            {
                // No audio: stay silent.
                _pool = null;
            }
        }

        /// <summary>Sound effects (the Settings sound toggle).</summary>
        public bool Enabled
        {
            get => _settings.Sfx;
            set => _settings.Sfx = value;
        }

        /// <summary>Haptic patterns with the cues and clears (the Settings haptics toggle).</summary>
        public bool Haptics
        {
            get => _settings.Haptics;
            set => _settings.Haptics = value;
        }

        public void Play(SoundCue cue)
        {
            double now = Now;
            if (_policy.ShouldPlaySound(cue, now))
            {
                Start(Key(cue), 3);
            }

            HapticPattern? haptic = _policy.Haptic(cue, now);
            if (haptic != null)
            {
                Vibrate(haptic);
            }
        }

        /// <summary>A clearing sound (a texture of a walker's act, or a collect on ladder step <paramref name="index"/>) if the policy lets it start.</summary>
        public void PlayClear(ClearSound sound, int index)
        {
            if (_policy.ShouldPlayClear(sound, Now))
            {
                Start(Key(sound, index), ClearSounds.IsCollect(sound) ? 2 : 1);
            }
        }

        /// <summary>A tile's clear in the level's style: its collect on the pod's ladder step and its micro haptic.</summary>
        public void Collect(ClearStyle style, int step)
        {
            PlayClear(ClearSounds.CollectOf(style), step);
            HapticPattern? haptic = _policy.ClearHaptic(style, Now);
            if (haptic != null)
            {
                Vibrate(haptic);
            }
        }

        private static double Now => SystemClock.UptimeMillis() / 1000.0;

        private static string Key(SoundCue cue) => "cue-" + cue;

        private static string Key(ClearSound sound, int index) => "clear-" + sound + "-" + index;

        private void Start(string key, int priority)
        {
            if (_pool == null)
            {
                return;
            }

            int id;
            lock (_loaded)
            {
                if (!_ids.TryGetValue(key, out id) || !_loaded.Contains(id))
                {
                    // Not loaded yet (the first second after the app opens): this one stays silent.
                    return;
                }
            }

            try
            {
                _pool.Play(id, 1f, 1f, priority, 0, 1f);
            }
            catch (Java.Lang.Exception)
            {
                // No audio device: stay silent.
            }
        }

        /// <summary>Writes every clip as a WAV file and loads it into the pool (on a worker thread, at start).</summary>
        private void LoadAll(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                foreach (SoundCue cue in (SoundCue[])Enum.GetValues(typeof(SoundCue)))
                {
                    Load(folder, Key(cue), ToneSynth.Cue(cue));
                }

                foreach (ClearSound sound in ClearSounds.All)
                {
                    for (int i = 0; i < ClearSounds.ClipCount(sound); i++)
                    {
                        Load(folder, Key(sound, i), ToneSynth.Clear(sound, i));
                    }
                }
            }
            catch (Exception)
            {
                // A full cache or no audio: the sounds that loaded play, the rest stay silent.
            }
        }

        private void Load(string folder, string key, float[] samples)
        {
            string path = Path.Combine(folder, key + ".wav");
            File.WriteAllBytes(path, Wav(samples));
            int id = _pool!.Load(path, 1);
            lock (_loaded)
            {
                _ids[key] = id;
            }
        }

        /// <summary>A mono 16-bit PCM WAV file of the samples at <see cref="ToneSynth.SampleRate"/>.</summary>
        public static byte[] Wav(float[] samples)
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

        private void Vibrate(HapticPattern pattern)
        {
            if (_vibrator == null || !_vibrator.HasVibrator)
            {
                return;
            }

            try
            {
                if (pattern.Notes.Count > 0 && _primitives && OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    VibrationEffect.Composition composition = VibrationEffect.StartComposition();
                    foreach (HapticNote note in pattern.Notes)
                    {
                        composition.AddPrimitive(PrimitiveOf(note.Primitive), note.Scale, note.DelayMilliseconds);
                    }

                    _vibrator.Vibrate(composition.Compose());
                }
                else if (pattern.IsMicro && pattern.Notes.Count > 0 && OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    // The predefined click: its tick is too faint to feel on most phones (the owner, 2026-10-08).
                    _vibrator.Vibrate(VibrationEffect.CreatePredefined(VibrationEffect.EffectClick));
                }
                else if (_amplitude || !pattern.IsMicro)
                {
                    _vibrator.Vibrate(VibrationEffect.CreateOneShot(pattern.Milliseconds, pattern.Amplitude));
                }
            }
            catch (Java.Lang.Exception)
            {
                // No vibrator after all: stay still.
            }
        }

        // A low tick where the phone has it (API 31), else a tick.
        private int PrimitiveOf(HapticPrimitive primitive)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                return 0;
            }

            return primitive switch
            {
                HapticPrimitive.Click => (int)VibrationEffectCompositionPrimitive.Click,
                HapticPrimitive.LowTick when _lowTick && OperatingSystem.IsAndroidVersionAtLeast(31) => (int)VibrationEffectCompositionPrimitive.LowTick,
                _ => (int)VibrationEffectCompositionPrimitive.Tick,
            };
        }
    }
}
