using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The procedural sound, the clearing sounds (spec 005 FR-042) and the Settings toggles for music, sound and haptics (FR-073).</summary>
    public class FeedbackTests
    {
        [Test]
        public void EveryCue_IsAShortClickFreeClip()
        {
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>())
            {
                AssertClickFree(ToneSynth.Cue(cue), cue.ToString(), 0.05f);
                Assert.That(ToneSynth.Cue(cue), Is.EqualTo(ToneSynth.Cue(cue)), cue + " is deterministic");
            }
        }

        [Test]
        public void EveryClearingSound_IsAShortClickFreeClip_OnEachStepOrVariation()
        {
            foreach (ClearSound sound in ClearSounds.All)
            {
                for (int i = 0; i < ClearSounds.ClipCount(sound); i++)
                {
                    float[] samples = ToneSynth.Clear(sound, i);
                    string name = sound + " " + i;
                    AssertClickFree(samples, name, 0.05f);
                    Assert.That(samples.Length, Is.LessThanOrEqualTo(ToneSynth.SampleRate * 0.65), name + " never rings on");
                    Assert.That(ToneSynth.Clear(sound, i), Is.EqualTo(samples), name + " is deterministic");
                }
            }
        }

        [Test]
        public void TheCollects_SitOverTheTextures_AndUnderThePodDone()
        {
            float done = ToneSynth.Cue(SoundCue.PodDone).Max(Math.Abs);
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)).Cast<ClearStyle>())
            {
                float collect = ToneSynth.Clear(ClearSounds.CollectOf(style), 5).Max(Math.Abs);
                Assert.That(collect, Is.LessThan(done), style + "'s collect is quieter than a pod done");
            }

            foreach (ClearSound sound in ClearSounds.All.Where(s => !ClearSounds.IsCollect(s)))
            {
                Assert.That(ToneSynth.Clear(sound, 0).Max(Math.Abs), Is.LessThanOrEqualTo(0.2f), sound + " is a soft texture");
            }
        }

        [Test]
        public void TheLadder_IsPentatonic_AndAPodClimbsItToTheTop()
        {
            // C major pentatonic: C D E G A, so overlapping notes never clash with each other or the music.
            int[] pentatonic = { 0, 2, 4, 7, 9 };
            foreach (double f in ClearSounds.Ladder)
            {
                int semitone = (int)Math.Round(12 * Math.Log(f / 261.63, 2));
                Assert.That(pentatonic, Does.Contain(((semitone % 12) + 12) % 12), f + " Hz is on the scale");
            }

            Assert.That(ClearSounds.Ladder, Is.Ordered);
            Assert.That(ClearSounds.Step(1, 12), Is.EqualTo(0), "the first tile starts at the bottom");
            Assert.That(ClearSounds.Step(12, 12), Is.EqualTo(ClearSounds.Steps - 1), "the last tile is on top");
            Assert.That(ClearSounds.Step(1, 1), Is.EqualTo(ClearSounds.Steps - 1), "a pod of one plays the top note");
            int last = -1;
            for (int done = 1; done <= 40; done++)
            {
                int step = ClearSounds.Step(done, 40);
                Assert.That(step, Is.GreaterThanOrEqualTo(last), "the ladder never goes down");
                last = step;
            }

            var ladder = new ClearLadder();
            Assert.That(ladder.Next("a", 3), Is.EqualTo(0));
            Assert.That(ladder.Next("b", 3), Is.EqualTo(0), "each pod has its own ladder");
            Assert.That(ladder.Next("a", 3), Is.EqualTo(ClearSounds.Step(2, 3)));
            ladder.Reset();
            Assert.That(ladder.Next("a", 3), Is.EqualTo(0), "a new attempt starts again");
        }

        [Test]
        public void TheActsBeats_FollowTheLook_AndComeOncePerTrip()
        {
            const float start = 2f;
            const int cells = 4;
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)).Cast<ClearStyle>())
            {
                float arrival = ClearStyles.TripSeconds(cells);
                var all = new List<ClearBeat>();
                ClearSounds.Crossed(style, cells, start, arrival, float.NegativeInfinity, float.PositiveInfinity, 7, all);
                Assert.That(all, Is.Not.Empty, style + " has an act");
                Assert.That(all.All(b => b.Share > 0f && b.Share < 1f), style + "'s beats come between the start and the clear");
                Assert.That(all.All(b => !ClearSounds.IsCollect(b.Sound)), style + "'s collect comes with the clear, not here");
                Assert.That(all.Select(b => b.Share), Is.Ordered, style + "'s beats in time order");

                // Frame by frame, every beat comes exactly once, whatever the frames' lengths.
                var stepped = new List<ClearBeat>();
                float from = 0f;
                for (int frame = 1; from < start + arrival + 1f; frame++)
                {
                    float to = from + (0.011f * (1 + (frame % 5)));
                    ClearSounds.Crossed(style, cells, start, arrival, from, to, 7, stepped);
                    from = to;
                }

                Assert.That(stepped.Select(b => (b.Sound, b.Variation, b.Share)), Is.EqualTo(all.Select(b => (b.Sound, b.Variation, b.Share))), style.ToString());
            }

            // A Pusher's tile lands on every cell of its way home; a Muncher bites three times.
            var pushers = new List<ClearBeat>();
            ClearSounds.Crossed(ClearStyle.Pushers, cells, 0f, 5f, -1f, 6f, 0, pushers);
            Assert.That(pushers.Count(b => b.Sound == ClearSound.PushKnock), Is.EqualTo(cells));
            var munchers = new List<ClearBeat>();
            ClearSounds.Crossed(ClearStyle.Munchers, cells, 0f, 5f, -1f, 6f, 0, munchers);
            Assert.That(munchers.Select(b => b.Variation), Is.EqualTo(new[] { 0, 1, 2 }), "each bite a little higher");
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
            Assert.That(policy.ShouldPlayClear(ClearSound.BlossomPluck, 0), Is.True);
            Assert.That(policy.Haptic(SoundCue.Win, 0), Is.SameAs(HapticPattern.Strong));
            Assert.That(policy.MusicOn, Is.True);

            settings.Sfx = false;
            settings.Haptics = false;
            settings.Music = false;
            Assert.That(policy.ShouldPlaySound(SoundCue.Tap, 10), Is.False);
            Assert.That(policy.ShouldPlayClear(ClearSound.BlossomPluck, 10), Is.False);
            Assert.That(policy.Haptic(SoundCue.Win, 10), Is.Null);
            Assert.That(policy.ClearHaptic(ClearStyle.Bubbles, 10), Is.Null);
            Assert.That(policy.MusicOn, Is.False);
        }

        [Test]
        public void ClearingSounds_AreSpacedOut_AndOnlyAFewStartTogether()
        {
            var policy = new FeedbackPolicy(() => new SettingsData());

            Assert.That(policy.ShouldPlayClear(ClearSound.BubblePop, 10.0), Is.True);
            Assert.That(policy.ShouldPlayClear(ClearSound.BubblePop, 10.01), Is.False, "within the collect's spacing");
            Assert.That(policy.ShouldPlayClear(ClearSound.BubbleBlow, 10.01), Is.True, "another sound is not spaced by it");
            Assert.That(policy.ShouldPlayClear(ClearSound.BubblePop, 10.0 + FeedbackPolicy.ClearSpacingSeconds), Is.True);

            // A crowd of clears at once: the window keeps a few voices, the textures fewer than the collects.
            var crowd = new FeedbackPolicy(() => new SettingsData());
            int collects = ClearSounds.All.Where(ClearSounds.IsCollect).Count(s => crowd.ShouldPlayClear(s, 20.0));
            Assert.That(collects, Is.EqualTo(FeedbackPolicy.ClearVoices));
            var textures = new FeedbackPolicy(() => new SettingsData());
            int played = ClearSounds.All.Where(s => !ClearSounds.IsCollect(s)).Count(s => textures.ShouldPlayClear(s, 20.0));
            Assert.That(played, Is.EqualTo(FeedbackPolicy.TextureVoices));
            Assert.That(textures.ShouldPlayClear(ClearSound.BlossomPluck, 20.0), Is.True, "a collect still finds a voice");
            Assert.That(textures.ShouldPlayClear(ClearSound.MunchCrunch, 20.0 + FeedbackPolicy.ClearWindowSeconds), Is.True, "the window moves on");
        }

        [Test]
        public void TileHaptics_AreMicroTicks_SpacedOut_AndNeverOverAStrongerPattern()
        {
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)).Cast<ClearStyle>())
            {
                HapticPattern pattern = ClearSounds.CollectHaptic(style);
                Assert.That(pattern.IsMicro, style.ToString());
                Assert.That(pattern.Notes, Is.Not.Empty, style + " is composed of transients");
                // Felt in the hand (the owner, 2026-10-08: "barely felt"), yet a tick, never a buzz, and under the pod's.
                Assert.That(pattern.Notes.All(n => n.Scale >= 0.7f && n.Scale <= 1f), style + " is felt");
                Assert.That(pattern.Notes.All(n => n.Primitive != HapticPrimitive.LowTick), style + " is crisp: the low tick is too faint");
                Assert.That(pattern.Seconds, Is.LessThanOrEqualTo(0.06), style + " is short");
                Assert.That(pattern.Milliseconds, Is.InRange(10, 25), style + "'s pulse is a tick, never a buzz");
                Assert.That(pattern.Amplitude, Is.InRange(130, HapticPattern.PodDone.Amplitude - 1), style + "'s pulse is felt, and under the pod's");
                Assert.That(pattern.Notes.Max(n => n.Scale), Is.LessThanOrEqualTo(HapticPattern.PodDone.Notes.Max(n => n.Scale)));
            }

            var policy = new FeedbackPolicy(() => new SettingsData());
            Assert.That(policy.ClearHaptic(ClearStyle.Blossom, 5.0), Is.Not.Null);
            Assert.That(policy.ClearHaptic(ClearStyle.Blossom, 5.05), Is.Null, "within the gap");
            Assert.That(policy.ClearHaptic(ClearStyle.Blossom, 5.0 + 0.01 + FeedbackPolicy.MicroGapSeconds + 0.001), Is.Not.Null);

            // A pod done always plays and keeps the next tick off until it has ended.
            Assert.That(policy.Haptic(SoundCue.PodDone, 6.0), Is.SameAs(HapticPattern.PodDone));
            Assert.That(policy.ClearHaptic(ClearStyle.Blossom, 6.05), Is.Null, "never over the pod's pattern");
            Assert.That(policy.Haptic(SoundCue.Win, 6.06), Is.SameAs(HapticPattern.Strong), "a stronger pattern is never dropped");
            Assert.That(policy.Haptic(SoundCue.Clear, 7.0), Is.Null, "the tester's plain clear never vibrates");
        }

        private static void AssertClickFree(float[] samples, string name, float least)
        {
            Assert.That(samples.Length, Is.InRange(ToneSynth.SampleRate / 50, ToneSynth.SampleRate), name);
            Assert.That(samples.Max(Math.Abs), Is.GreaterThan(least).And.LessThanOrEqualTo(1f), name + " is audible and never clips");
            Assert.That(Math.Abs(samples[0]), Is.LessThan(0.01f), name + " starts at silence");
            Assert.That(Math.Abs(samples[samples.Length - 1]), Is.LessThan(0.01f), name + " ends at silence");
        }
    }
}
