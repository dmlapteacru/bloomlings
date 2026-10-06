using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using UnityEngine;

namespace Bloomlings.Client.Services.Feedback
{
    /// <summary>
    /// Sound, music and haptics (FR-073). One object for the whole app, created by Boot: it plays the synthesized cues
    /// (<see cref="ToneSynth"/>) and the music loop, and follows the Settings toggles as they change, through
    /// <see cref="FeedbackPolicy"/>. <see cref="Current"/> is null until Boot creates it (tests, Editor tools), and every
    /// caller treats that as silence.
    /// </summary>
    public sealed class GameFeedback : MonoBehaviour
    {
        private const float MusicVolume = 0.6f;

        private readonly Dictionary<SoundCue, AudioClip> _clips = new Dictionary<SoundCue, AudioClip>();
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

        /// <summary>Plays a cue's sound and haptic pulse, each only if its Setting is on.</summary>
        /// <param name="pitch">1 for the clip as synthesized; tile clears vary it slightly.</param>
        public void Play(SoundCue cue, float pitch = 1f)
        {
            if (_policy.ShouldPlaySound(cue, Time.unscaledTime))
            {
                _sfx.pitch = pitch;
                _sfx.PlayOneShot(Clip(cue), 1f);
            }

            HapticStrength? haptic = _policy.Haptic(cue);
            if (haptic.HasValue)
            {
                Haptics.Pulse(haptic.Value);
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

        private static AudioClip ToClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, ToneSynth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>
    /// Short vibration pulses. Android uses the Vibrator service (one-shot effects with an amplitude from API 26);
    /// iOS has only the system vibration through Unity, kept for strong events. The Editor stays still.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject? _vibrator;
        private static int _sdk;
#endif

        public static void Pulse(HapticStrength strength)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            long milliseconds = strength switch { HapticStrength.Light => 12, HapticStrength.Medium => 25, _ => 60 };
            int amplitude = strength switch { HapticStrength.Light => 60, HapticStrength.Medium => 140, _ => 255 };
            try
            {
                if (_vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    _sdk = version.GetStatic<int>("SDK_INT");
                }

                if (_sdk >= 26)
                {
                    using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                    using AndroidJavaObject effect = effects.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (Exception)
            {
                // Handheld.Vibrate also makes Unity add the VIBRATE permission to the Android manifest.
                if (strength == HapticStrength.Strong)
                {
                    Handheld.Vibrate();
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (strength == HapticStrength.Strong)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
