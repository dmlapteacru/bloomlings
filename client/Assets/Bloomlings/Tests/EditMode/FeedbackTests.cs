using System;
using System.Linq;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The procedural sound and the Settings toggles for music, sound and haptics (FR-073).</summary>
    public class FeedbackTests
    {
        [Test]
        public void EveryCue_IsAShortClickFreeClip()
        {
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>())
            {
                float[] samples = ToneSynth.Cue(cue);
                Assert.That(samples.Length, Is.InRange(ToneSynth.SampleRate / 50, ToneSynth.SampleRate), cue.ToString());
                Assert.That(samples.Max(Math.Abs), Is.GreaterThan(0.05f).And.LessThanOrEqualTo(1f), cue + " is audible and never clips");
                Assert.That(Math.Abs(samples[0]), Is.LessThan(0.01f), cue + " starts at silence");
                Assert.That(Math.Abs(samples[samples.Length - 1]), Is.LessThan(0.01f), cue + " ends at silence");
                Assert.That(ToneSynth.Cue(cue), Is.EqualTo(samples), cue + " is deterministic");
            }
        }

        [Test]
        public void TheMusic_LoopsExactlyOverItsBars()
        {
            float[] music = ToneSynth.MusicLoop();
            double seconds = (double)music.Length / ToneSynth.SampleRate;

            Assert.That(seconds, Is.EqualTo(ToneSynth.MusicBars * 4 * 60.0 / ToneSynth.MusicBeatsPerMinute).Within(0.001));
            Assert.That(music.Max(Math.Abs), Is.GreaterThan(0.02f).And.LessThanOrEqualTo(1f));
            Assert.That(Math.Abs(music[0]), Is.LessThan(0.01f), "no click where the loop starts");
        }

        [Test]
        public void SoundAndHaptics_FollowTheirToggles()
        {
            var settings = new SettingsData();
            var policy = new FeedbackPolicy(() => settings);
            Assert.That(policy.ShouldPlaySound(SoundCue.Tap, 0), Is.True);
            Assert.That(policy.Haptic(SoundCue.Win), Is.EqualTo(HapticStrength.Strong));
            Assert.That(policy.MusicOn, Is.True);

            settings.Sfx = false;
            settings.Haptics = false;
            settings.Music = false;
            Assert.That(policy.ShouldPlaySound(SoundCue.Tap, 1), Is.False);
            Assert.That(policy.Haptic(SoundCue.Win), Is.Null);
            Assert.That(policy.MusicOn, Is.False);
        }

        [Test]
        public void TileClears_AreSpacedOut_AndNeverVibrate()
        {
            var policy = new FeedbackPolicy(() => new SettingsData());

            Assert.That(policy.ShouldPlaySound(SoundCue.Clear, 10.0), Is.True);
            Assert.That(policy.ShouldPlaySound(SoundCue.Clear, 10.01), Is.False, "within the spacing");
            Assert.That(policy.ShouldPlaySound(SoundCue.PodDone, 10.02), Is.True, "other cues are not spaced");
            Assert.That(policy.ShouldPlaySound(SoundCue.Clear, 10.0 + FeedbackPolicy.ClearSpacingSeconds), Is.True);
            Assert.That(policy.Haptic(SoundCue.Clear), Is.Null);
        }
    }
}
