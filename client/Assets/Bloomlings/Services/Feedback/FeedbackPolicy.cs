using System;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Services.Feedback
{
    public enum HapticStrength
    {
        Light,
        Medium,
        Strong,
    }

    /// <summary>
    /// What a cue plays under the player's Settings (FR-073): sound effects only with Sound on, a haptic pulse only with
    /// Haptics on, music only with Music on. Tile clears are frequent, so their sound plays at most once per
    /// <see cref="ClearSpacingSeconds"/>; they never vibrate. Engine-free, so the rules are tested without Unity.
    /// </summary>
    public sealed class FeedbackPolicy
    {
        public const double ClearSpacingSeconds = 0.045;

        private readonly Func<SettingsData> _settings;
        private double _lastClear = double.NegativeInfinity;

        public FeedbackPolicy(Func<SettingsData> settings)
        {
            _settings = settings;
        }

        public bool MusicOn => _settings().Music;

        /// <summary>Whether the cue's sound plays now (<paramref name="now"/> in seconds, any clock).</summary>
        public bool ShouldPlaySound(SoundCue cue, double now)
        {
            if (!_settings().Sfx)
            {
                return false;
            }

            if (cue != SoundCue.Clear)
            {
                return true;
            }

            if (now - _lastClear < ClearSpacingSeconds - 1e-9)
            {
                return false;
            }

            _lastClear = now;
            return true;
        }

        /// <summary>The haptic pulse of a cue, or null for none (or with Haptics off).</summary>
        public HapticStrength? Haptic(SoundCue cue)
        {
            if (!_settings().Haptics)
            {
                return null;
            }

            return cue switch
            {
                SoundCue.Tap => HapticStrength.Light,
                SoundCue.Key => HapticStrength.Light,
                SoundCue.Refused => HapticStrength.Medium,
                SoundCue.Special => HapticStrength.Medium,
                SoundCue.Booster => HapticStrength.Medium,
                SoundCue.Jam => HapticStrength.Strong,
                SoundCue.Win => HapticStrength.Strong,
                _ => null,
            };
        }
    }
}
