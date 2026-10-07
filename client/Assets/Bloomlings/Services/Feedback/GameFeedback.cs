using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using UnityEngine;

namespace Bloomlings.Client.Services.Feedback
{
    /// <summary>
    /// Sound, music and haptics (FR-073). One object for the whole app, created by Boot: it plays the synthesized cues
    /// and clearing sounds (<see cref="ToneSynth"/>, spec 005 FR-042) and the music loop, pulses the haptic patterns
    /// (<see cref="Haptics"/>), and follows the Settings toggles as they change, through <see cref="FeedbackPolicy"/>.
    /// <see cref="Current"/> is null until Boot creates it (tests, Editor tools), and every caller treats that as
    /// silence.
    /// </summary>
    public sealed class GameFeedback : MonoBehaviour
    {
        private const float MusicVolume = 0.6f;

        private readonly Dictionary<SoundCue, AudioClip> _clips = new Dictionary<SoundCue, AudioClip>();
        private readonly Dictionary<(ClearSound Sound, int Index), AudioClip> _clearClips = new Dictionary<(ClearSound, int), AudioClip>();
        private FeedbackPolicy _policy = null!;
        private AudioSource _sfx = null!;
        private AudioSource _music = null!;

        public static GameFeedback? Current { get; private set; }

        public static GameFeedback Create(SettingsData settings)
        {
            var host = new GameObject("GameFeedback");
            DontDestroyOnLoad(host);
            var feedback = host.AddComponent<GameFeedback>();
            feedback._policy = new FeedbackPolicy(() => settings);
            feedback._sfx = host.AddComponent<AudioSource>();
            feedback._sfx.playOnAwake = false;
            feedback._music = host.AddComponent<AudioSource>();
            feedback._music.playOnAwake = false;
            feedback._music.loop = true;
            feedback._music.volume = MusicVolume;
            Current = feedback;
            return feedback;
        }

        /// <summary>Plays a cue's sound and haptic pattern, each only if its Setting is on.</summary>
        public void Play(SoundCue cue)
        {
            double now = Time.unscaledTime;
            if (_policy.ShouldPlaySound(cue, now))
            {
                _sfx.PlayOneShot(Clip(cue), 1f);
            }

            HapticPattern? haptic = _policy.Haptic(cue, now);
            if (haptic != null)
            {
                Haptics.Play(haptic);
            }
        }

        /// <summary>A clearing sound (a texture of a walker's act, or a collect on ladder step <paramref name="index"/>) if the policy lets it start.</summary>
        public void PlayClear(ClearSound sound, int index)
        {
            if (_policy.ShouldPlayClear(sound, Time.unscaledTime))
            {
                _sfx.PlayOneShot(ClearClip(sound, index), 1f);
            }
        }

        /// <summary>A tile's clear in the level's style: its collect on the pod's ladder step and its micro haptic.</summary>
        public void Collect(ClearStyle style, int step)
        {
            PlayClear(ClearSounds.CollectOf(style), step);
            HapticPattern? haptic = _policy.ClearHaptic(style, Time.unscaledTime);
            if (haptic != null)
            {
                Haptics.Play(haptic);
            }
        }

        /// <summary>Makes a style's clips before its level plays, so its first clears never wait for the synthesizer.</summary>
        public void Prewarm(ClearStyle style)
        {
            ClearSound collect = ClearSounds.CollectOf(style);
            for (int i = 0; i < ClearSounds.Steps; i++)
            {
                ClearClip(collect, i);
            }

            var beats = new List<ClearBeat>();
            for (int seed = 0; seed < ClearSounds.Variations; seed++)
            {
                ClearSounds.Crossed(style, 2, 0f, 1f, -1f, 2f, seed, beats);
            }

            foreach (ClearBeat beat in beats)
            {
                ClearClip(beat.Sound, beat.Variation);
            }
        }

        /// <summary>The music follows the Music toggle while the app runs.</summary>
        private void Update()
        {
            bool on = _policy.MusicOn;
            if (on && !_music.isPlaying)
            {
                if (_music.clip == null)
                {
                    _music.clip = ToClip("music", ToneSynth.MusicLoop());
                }

                _music.Play();
            }
            else if (!on && _music.isPlaying)
            {
                _music.Stop();
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        private AudioClip Clip(SoundCue cue)
        {
            if (!_clips.TryGetValue(cue, out AudioClip? clip))
            {
                clip = ToClip(cue.ToString(), ToneSynth.Cue(cue));
                _clips[cue] = clip;
            }

            return clip;
        }

        private AudioClip ClearClip(ClearSound sound, int index)
        {
            if (!_clearClips.TryGetValue((sound, index), out AudioClip? clip))
            {
                clip = ToClip(sound + "-" + index, ToneSynth.Clear(sound, index));
                _clearClips[(sound, index)] = clip;
            }

            return clip;
        }

        private static AudioClip ToClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, ToneSynth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>
    /// The haptic patterns (<see cref="HapticPattern"/>; spec 005 FR-042). Android composes a pattern's transients where
    /// the phone renders them (API 30, the low tick from API 31; the crispest and softest feel), else plays the
    /// predefined tick or click for a tile's micro haptic (API 29), else a short one-shot pulse where it has amplitude
    /// control (API 26); a phone that can only buzz plays the cues' pulses and never a micro haptic. iOS plays impact
    /// feedback of a matching style and intensity through the plugin in <c>Assets/Plugins/iOS</c>. The Editor stays still.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // android.os.VibrationEffect's constants.
        private const int PrimitiveClick = 1;
        private const int PrimitiveTick = 7;
        private const int PrimitiveLowTick = 8;
        private const int EffectClick = 0;
        private const int EffectTick = 2;

        private static AndroidJavaObject? _vibrator;
        private static bool _ready;
        private static int _sdk;
        private static bool _amplitude;
        private static bool _primitives;
        private static bool _lowTick;
#elif UNITY_IOS && !UNITY_EDITOR
        // The plugin's impact styles past the primitives' (0 light for a tick, 1 soft for a low tick, 2 rigid for a click).
        private const int ImpactMedium = 3;
        private const int ImpactHeavy = 4;

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void BloomlingsHapticImpact(int primitive, float intensity, int delayMilliseconds);
#endif

        public static void Play(HapticPattern pattern)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Prepare();
                if (_vibrator == null)
                {
                    return;
                }

                using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                if (pattern.Notes.Count > 0 && _primitives)
                {
                    using AndroidJavaObject composition = effects.CallStatic<AndroidJavaObject>("startComposition");
                    foreach (HapticNote note in pattern.Notes)
                    {
                        using AndroidJavaObject chained = composition.Call<AndroidJavaObject>("addPrimitive", PrimitiveOf(note.Primitive), note.Scale, note.DelayMilliseconds);
                    }

                    using AndroidJavaObject composed = composition.Call<AndroidJavaObject>("compose");
                    _vibrator.Call("vibrate", composed);
                }
                else if (pattern.IsMicro && pattern.Notes.Count > 0 && _sdk >= 29)
                {
                    int effect = pattern.Notes[0].Primitive == HapticPrimitive.Click ? EffectClick : EffectTick;
                    using AndroidJavaObject predefined = effects.CallStatic<AndroidJavaObject>("createPredefined", effect);
                    _vibrator.Call("vibrate", predefined);
                }
                else if (_sdk >= 26 && (_amplitude || !pattern.IsMicro))
                {
                    using AndroidJavaObject shot = effects.CallStatic<AndroidJavaObject>("createOneShot", (long)pattern.Milliseconds, pattern.Amplitude);
                    _vibrator.Call("vibrate", shot);
                }
                else if (!pattern.IsMicro)
                {
                    _vibrator.Call("vibrate", (long)pattern.Milliseconds);
                }
            }
            catch (Exception)
            {
                // Handheld.Vibrate also makes Unity add the VIBRATE permission to the Android manifest.
                if (pattern == HapticPattern.Strong)
                {
                    Handheld.Vibrate();
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (pattern.Notes.Count == 0)
            {
                // The cues' pulses: a light, medium or heavy impact for their strength.
                int style = pattern == HapticPattern.Strong ? ImpactHeavy : pattern == HapticPattern.Medium ? ImpactMedium : (int)HapticPrimitive.Tick;
                BloomlingsHapticImpact(style, pattern == HapticPattern.Light ? 0.7f : 1f, 0);
                return;
            }

            int at = 0;
            foreach (HapticNote note in pattern.Notes)
            {
                at += note.DelayMilliseconds;
                BloomlingsHapticImpact((int)note.Primitive, note.Scale, at);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void Prepare()
        {
            if (_ready)
            {
                return;
            }

            _ready = true;
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            _sdk = version.GetStatic<int>("SDK_INT");
            if (_vibrator == null || !_vibrator.Call<bool>("hasVibrator"))
            {
                _vibrator = null;
                return;
            }

            _amplitude = _sdk >= 26 && _vibrator.Call<bool>("hasAmplitudeControl");
            _primitives = _sdk >= 30 && _vibrator.Call<bool>("areAllPrimitivesSupported", new[] { PrimitiveTick, PrimitiveClick });
            _lowTick = _primitives && _sdk >= 31 && _vibrator.Call<bool>("areAllPrimitivesSupported", new[] { PrimitiveLowTick });
        }

        // A low tick where the phone has it (API 31), else a tick.
        private static int PrimitiveOf(HapticPrimitive primitive) => primitive switch
        {
            HapticPrimitive.Click => PrimitiveClick,
            HapticPrimitive.LowTick => _lowTick ? PrimitiveLowTick : PrimitiveTick,
            _ => PrimitiveTick,
        };
#endif
    }
}
