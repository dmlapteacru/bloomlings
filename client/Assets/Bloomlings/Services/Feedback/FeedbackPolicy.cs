using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Client.Services.Feedback
{
    /// <summary>
    /// What a cue plays under the player's Settings (FR-073): sound effects only with Sound on, a haptic pattern only with
    /// Haptics on, music only with Music on. The clearing sounds (spec 005 FR-042) come often, so each clear sound plays
    /// at most once per its spacing, and only a few start together (<see cref="ClearVoices"/> within
    /// <see cref="ClearWindowSeconds"/>; the textures yield to the collects); a tile's micro haptic plays at most once per
    /// <see cref="MicroGapSeconds"/> and never over a stronger pattern. Engine-free, so the rules are tested without
    /// Unity, and both builds use it. Times in seconds of real time (any clock), never the timeline's, so fast forward
    /// thins the clears out instead of crowding them.
    /// </summary>
    public sealed class FeedbackPolicy
    {
        /// <summary>The least time between two collects of the same sound.</summary>
        public const double ClearSpacingSeconds = 0.045;

        /// <summary>The least time between two textures of the same sound.</summary>
        public const double TextureSpacingSeconds = 0.06;

        /// <summary>The window in which at most <see cref="ClearVoices"/> clear sounds start.</summary>
        public const double ClearWindowSeconds = 0.2;

        /// <summary>How many clear sounds may start within the window (collects).</summary>
        public const int ClearVoices = 5;

        /// <summary>How many of them a texture may join (the collects keep the rest).</summary>
        public const int TextureVoices = 3;

        /// <summary>
        /// The least time between the end of a haptic pattern and a tile's micro haptic: about eleven ticks a second at
        /// most, each felt on its own, never merging into a buzz.
        /// </summary>
        public const double MicroGapSeconds = 0.09;

        private readonly Func<SettingsData> _settings;
        private readonly Dictionary<ClearSound, double> _lastClear = new Dictionary<ClearSound, double>();
        private readonly Queue<double> _recentClears = new Queue<double>();
        private double _hapticFree = double.NegativeInfinity;

        public FeedbackPolicy(Func<SettingsData> settings)
        {
            _settings = settings;
        }

        public bool MusicOn => _settings().Music;

        /// <summary>Whether the cue's sound plays now (<paramref name="now"/> in seconds, any clock).</summary>
        public bool ShouldPlaySound(SoundCue cue, double now) => _settings().Sfx;

        /// <summary>Whether a clearing sound plays now (spec 005 FR-042): its spacing and the window's voices allow it.</summary>
        public bool ShouldPlayClear(ClearSound sound, double now)
        {
            if (!_settings().Sfx)
            {
                return false;
            }

            bool collect = ClearSounds.IsCollect(sound);
            double spacing = collect ? ClearSpacingSeconds : TextureSpacingSeconds;
            if (_lastClear.TryGetValue(sound, out double last) && now - last < spacing - 1e-9)
            {
                return false;
            }

            while (_recentClears.Count > 0 && now - _recentClears.Peek() >= ClearWindowSeconds - 1e-9)
            {
                _recentClears.Dequeue();
            }

            if (_recentClears.Count >= (collect ? ClearVoices : TextureVoices))
            {
                return false;
            }

            _lastClear[sound] = now;
            _recentClears.Enqueue(now);
            return true;
        }

        /// <summary>The haptic pattern of a cue now, or null for none (or with Haptics off).</summary>
        public HapticPattern? Haptic(SoundCue cue, double now)
        {
            HapticPattern? pattern = cue switch
            {
                SoundCue.Tap => HapticPattern.Light,
                SoundCue.Key => HapticPattern.Light,
                SoundCue.PodDone => HapticPattern.PodDone,
                SoundCue.Refused => HapticPattern.Medium,
                SoundCue.Special => HapticPattern.Medium,
                SoundCue.Booster => HapticPattern.Medium,
                SoundCue.Jam => HapticPattern.Strong,
                SoundCue.Win => HapticPattern.Strong,
                _ => null,
            };
            return Allow(pattern, now);
        }

        /// <summary>The micro haptic of a tile's clear in the style now (spec 005 FR-042), or null when spaced out or off.</summary>
        public HapticPattern? ClearHaptic(ClearStyle style, double now) => Allow(ClearSounds.CollectHaptic(style), now);

        private HapticPattern? Allow(HapticPattern? pattern, double now)
        {
            if (pattern == null || !_settings().Haptics)
            {
                return null;
            }

            if (pattern.IsMicro && now < _hapticFree - 1e-9)
            {
                return null;
            }

            _hapticFree = Math.Max(_hapticFree, now + pattern.Seconds + MicroGapSeconds);
            return pattern;
        }
    }
}
