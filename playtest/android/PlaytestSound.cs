using System;
using System.Collections.Generic;
using Android.Content;
using Android.Media;
using Android.OS;
using Bloomlings.Client.Services.Feedback;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// Sound and vibration for the playtest client: the Unity client's synthesized cues (<see cref="ToneSynth"/>, linked
    /// from <c>client/</c>) played as static AudioTracks, and short vibration pulses. Presentation only; a tester can
    /// mute both with the top-bar toggle.
    /// </summary>
    public sealed class PlaytestSound
    {
        private readonly Dictionary<SoundCue, AudioTrack> _tracks = new Dictionary<SoundCue, AudioTrack>();
        private readonly Vibrator? _vibrator;

        public PlaytestSound(Context context)
        {
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
        }

        public bool Enabled { get; set; } = true;

        /// <summary>Short vibration pulses with the cues (the Settings haptics toggle).</summary>
        public bool Haptics { get; set; } = true;

        public void Play(SoundCue cue)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                AudioTrack track = Track(cue);
                if (track.PlayState == PlayState.Playing)
                {
                    track.Stop();
                }

                track.ReloadStaticData();
                track.Play();
                Vibrate(cue);
            }
            catch (Java.Lang.Exception)
            {
                // No audio device or vibrator: stay silent.
            }
        }

        private void Vibrate(SoundCue cue)
        {
            (long Milliseconds, int Amplitude)? pulse = cue switch
            {
                SoundCue.Tap or SoundCue.Key => (12L, 60),
                SoundCue.Refused or SoundCue.Special or SoundCue.Booster => (25L, 140),
                SoundCue.Jam or SoundCue.Win => (60L, 255),
                _ => null,
            };
            if (Haptics && pulse.HasValue && _vibrator != null && _vibrator.HasVibrator)
            {
                _vibrator.Vibrate(VibrationEffect.CreateOneShot(pulse.Value.Milliseconds, pulse.Value.Amplitude));
            }
        }

        private AudioTrack Track(SoundCue cue)
        {
            if (_tracks.TryGetValue(cue, out AudioTrack? track))
            {
                return track;
            }

            float[] samples = ToneSynth.Cue(cue);
            var pcm = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                pcm[i] = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            }

            track = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Sonification)!.Build()!)!
                .SetAudioFormat(new AudioFormat.Builder().SetEncoding(Encoding.Pcm16bit)!.SetSampleRate(ToneSynth.SampleRate)!.SetChannelMask(ChannelOut.Mono)!.Build()!)!
                .SetBufferSizeInBytes(pcm.Length * 2)!
                .SetTransferMode(AudioTrackMode.Static)!
                .Build()!;
            track.Write(pcm, 0, pcm.Length);
            _tracks[cue] = track;
            return track;
        }
    }
}
