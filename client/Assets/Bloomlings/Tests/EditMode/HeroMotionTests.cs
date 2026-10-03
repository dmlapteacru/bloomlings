using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The owner's animated heroes as flat frames (spec 005 FR-028, <see cref="HeroMotion"/>), their player
    /// (<see cref="HeroMotionPlayer"/>, <see cref="HomeMotion"/>) and the layered Home they stand in (<see cref="HomeLayers"/>).
    /// </summary>
    public class HeroMotionTests
    {
        private const float Eps = 1e-3f;

        [Test]
        public void EveryFamily_HasItsIdleAndReaction_TwigItsWinsCheer()
        {
            // The owner's table (2026-10-02): the Meshy heroes' idles last 4 s, their reactions 2 s. Twig's Blender model
            // (2026-10-03) keeps its clips' own lengths: a 3 s breathing, a 1.5 s small bounce and the win's 3 s cheer.
            foreach (Family family in CharacterArt.Families)
            {
                bool twig = family == Family.Twig;
                Assert.That(HeroMotion.Has(family), Is.True, family.ToString());
                Assert.That(HeroMotion.Seconds(family, MotionClip.Idle), Is.EqualTo(twig ? 3f : 4f).Within(Eps), family + " idle");
                Assert.That(HeroMotion.Seconds(family, MotionClip.React), Is.EqualTo(twig ? 1.5f : 2f).Within(Eps), family + " reaction");
                Assert.That(HeroMotion.HasWin(family), Is.EqualTo(twig), family + " has its own cheer");
                Assert.That(HeroMotion.Seconds(family, MotionClip.Win), Is.EqualTo(twig ? 3f : 0f).Within(Eps), family + " cheer");
            }
        }

        [Test]
        public void ACelebration_PlaysTheHerosCheer_ElseItsReaction_ThenIdles()
        {
            var twig = new HeroMotionPlayer(Family.Twig, 0f);
            twig.Celebrate(0f);
            Assert.That(twig.Pose(0f).Clip, Is.EqualTo(MotionClip.Win), "Twig cheers from the start");
            Assert.That(twig.Pose(0f).Index, Is.EqualTo(0));
            float cheer = HeroMotion.Seconds(Family.Twig, MotionClip.Win);
            Assert.That(twig.Pose(cheer - 0.01f).Clip, Is.EqualTo(MotionClip.Win));
            Assert.That(twig.Pose(cheer + 0.01f).Clip, Is.EqualTo(MotionClip.Idle), "then idles");
            Assert.That(twig.Pose(cheer + 0.01f).Index, Is.EqualTo(0), "from the seam the cheer ends on");

            var sprig = new HeroMotionPlayer(Family.Sprig, 0f);
            sprig.Celebrate(0f);
            Assert.That(sprig.Pose(0.5f).Clip, Is.EqualTo(MotionClip.React), "a hero without a cheer reacts");

            twig.React(cheer + 1f, waitForSeam: true);
            Assert.That(twig.Pose(cheer + HeroMotion.Seconds(Family.Twig, MotionClip.Idle) + 0.01f).Clip, Is.EqualTo(MotionClip.React), "a reaction after the cheer is a reaction");
        }

        [Test]
        public void EveryFrame_LiesInsideTheCell_WithItsHeadPoints()
        {
            var names = new HashSet<string>();
            foreach (Family family in CharacterArt.Families)
            {
                foreach (MotionClip clip in HeroMotion.Clips)
                {
                    for (int i = 0; i < HeroMotion.FrameCount(family, clip); i++)
                    {
                        HeroFrame f = HeroMotion.Frame(family, clip, i);
                        string at = f.Name;
                        Assert.That(f.Name, Is.EqualTo(HeroMotion.FrameName(family, clip, i)));
                        Assert.That(names.Add(f.Name), Is.True, at + " named once");
                        Assert.That(f.X >= 0 && f.Y >= 0 && f.Width > 0 && f.Height > 0, Is.True, at);
                        Assert.That(f.X + f.Width <= HeroMotion.CellWidth && f.Y + f.Height <= HeroMotion.CellHeight, Is.True, at + " inside the cell");
                        Assert.That(f.Top.Y, Is.LessThan(f.Head.Y), at + ": the brow above the chin");
                        Assert.That(f.Roll, Is.InRange(-60f, 60f), at + ": the head roughly upright");
                    }
                }
            }

            Assert.That(new List<string>(HeroMotion.AllFrames()), Is.EquivalentTo(names));
        }

        [Test]
        public void TheSeamPose_StandsOnTheFootLine()
        {
            foreach (Family family in CharacterArt.Families)
            {
                HeroFrame seam = HeroMotion.Frame(family, MotionClip.Idle, 0);
                float feet = (seam.Y + seam.Height) / (float)HeroMotion.CellHeight;
                Assert.That(feet, Is.EqualTo(HeroMotion.FootLine).Within(0.02f), family.ToString());
                Assert.That(HeroMotion.Fill(family), Is.InRange(0.6f, 0.9f), family + " fills its cell");
            }
        }

        [Test]
        public void TheIdle_Loops()
        {
            var player = new HeroMotionPlayer(Family.Sprig, 10f);
            int frames = HeroMotion.FrameCount(Family.Sprig, MotionClip.Idle);
            Assert.That(player.Pose(10f).Index, Is.EqualTo(0));
            Assert.That(player.Pose(10f + (1.5f / HeroMotion.Fps)).Index, Is.EqualTo(1));
            Assert.That(player.Pose(14f - (0.5f / HeroMotion.Fps)).Index, Is.EqualTo(frames - 1));
            Assert.That(player.Pose(14f + (0.25f / HeroMotion.Fps)).Index, Is.EqualTo(0), "the loop starts over");
            Assert.That(player.Pose(14f).Clip, Is.EqualTo(MotionClip.Idle));
            Assert.That(player.NextSeam(11f), Is.EqualTo(14f).Within(Eps));
        }

        [Test]
        public void AWaitedReaction_StartsAndEndsOnTheSeam()
        {
            var player = new HeroMotionPlayer(Family.Bloom, 0f);
            player.React(1f, waitForSeam: true);
            Assert.That(player.Busy(1f), Is.True);
            Assert.That(player.Pose(3.9f).Clip, Is.EqualTo(MotionClip.Idle), "the idle plays on until the seam");
            HeroPose start = player.Pose(4.01f);
            Assert.That(start.Clip, Is.EqualTo(MotionClip.React));
            Assert.That(start.Index, Is.EqualTo(0));
            Assert.That(start.FromIdle, Is.EqualTo(-1), "no cross-fade at the seam");
            Assert.That(player.Pose(5.99f).Clip, Is.EqualTo(MotionClip.React));
            HeroPose after = player.Pose(6.01f);
            Assert.That(after.Clip, Is.EqualTo(MotionClip.Idle));
            Assert.That(after.Index, Is.EqualTo(0), "the idle starts over from the seam the reaction ends on");
            Assert.That(player.Busy(6.01f), Is.False);
        }

        [Test]
        public void ATapBetweenSeams_ReactsAtOnce_CrossFadingFromTheIdle()
        {
            var player = new HeroMotionPlayer(Family.Drop, 0f);
            player.React(1f);
            HeroPose pose = player.Pose(1.05f);
            Assert.That(pose.Clip, Is.EqualTo(MotionClip.React));
            Assert.That(pose.FromIdle, Is.EqualTo(HeroMotion.Fps), "from the idle frame it interrupted (the one at 1 s)");
            Assert.That(pose.FromAlpha, Is.InRange(0.01f, 0.99f));
            Assert.That(player.Pose(1f + HeroMotion.DissolveSeconds + 0.01f).FromIdle, Is.EqualTo(-1));
            player.React(1.5f);
            Assert.That(player.Pose(2.5f).Index, Is.EqualTo((int)(1.5f * HeroMotion.Fps)), "a second tap does not restart it");

            var near = new HeroMotionPlayer(Family.Drop, 0f);
            near.React(3.8f);
            Assert.That(near.Pose(3.9f).Clip, Is.EqualTo(MotionClip.Idle), "a tap just before the seam waits for it");
            Assert.That(near.Pose(4.01f).Clip, Is.EqualTo(MotionClip.React));
        }

        [Test]
        public void HomesReactions_TakeTurns()
        {
            var home = new HomeMotion(100f);
            home.Update(100f + HomeLayers.FirstReaction + 0.01f);
            Family first = HomeLayers.ReactionOrder[0];
            Assert.That(home.Player(first).Busy(101.6f), Is.True, "Bloom reacts first, at its next seam");
            foreach (Family other in HomeLayers.DrawOrder)
            {
                if (other != first)
                {
                    Assert.That(home.Player(other).Busy(101.6f), Is.False, other.ToString());
                }
            }

            home.Update(100f + HomeLayers.FirstReaction + HomeLayers.ReactionEvery + 0.01f);
            Assert.That(home.Player(HomeLayers.ReactionOrder[1]).Busy(107.6f), Is.True, "then Sprig");
        }

        private static IEnumerable<(float W, float H, Insets Insets)> Shapes()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 20f / 9f, 21f / 9f })
            {
                yield return (1080f, 1080f * ratio, new Insets(ratio > 2f ? 110f : 60f, ratio > 2f ? 60f : 0f));
                yield return (720f, 720f * ratio, Insets.None);
            }
        }

        [Test]
        public void TheLayers_LieInsideThePicture()
        {
            foreach (PictureBox layer in HomeLayers.All)
            {
                Assert.That(layer.X >= 0 && layer.Y >= 0, Is.True, layer.Name);
                Assert.That(layer.X + layer.Width <= HomeLayers.PictureWidth && layer.Y + layer.Height <= HomeLayers.PictureHeight, Is.True, layer.Name);
                Assert.That(HomeLayers.SlotOf(layer), Does.StartWith("bg.home"), layer.Name);
            }

            Assert.That(HomeLayers.Back.Name, Is.EqualTo(OwnerPictures.Home), "the garden is the Home picture the backdrop draws");
            Assert.That(HomeLayers.SlotOf(HomeLayers.FountainBack), Is.EqualTo("bg.home.fountain_back"));
        }

        [Test]
        public void TheHeroes_StandOnTheFountain_BetweenTheLogoAndThePlaque_OnEveryShape()
        {
            foreach ((float w, float h, Insets insets) in Shapes())
            {
                var screen = new Box(0f, 0f, w, h);
                Box picture = HomeLayers.Cover(screen);
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                string at = w + "x" + h;
                Assert.That(picture.Width / picture.Height, Is.EqualTo(HomeLayers.PictureWidth / (float)HomeLayers.PictureHeight).Within(1e-4f), at);
                Assert.That(picture.Left <= 0.01f && picture.Right >= w - 0.01f && picture.Top <= 0.01f && picture.Bottom >= h - 0.01f, Is.True, at + ": the picture covers the screen");
                foreach (Family family in HomeLayers.DrawOrder)
                {
                    Box cell = HomeLayers.HeroCell(picture, family);
                    Box seam = HeroMotion.PictureBox(cell, HeroMotion.Frame(family, MotionClip.Idle, 0));
                    (float x, float feet, float height) = HomeLayers.Placement(family);
                    Assert.That(seam.Height, Is.EqualTo(height * picture.Width).Within(picture.Width * 0.03f), at + " " + family + ": its height");
                    Assert.That(seam.Left >= 0f && seam.Right <= w, Is.True, at + " " + family + ": on screen");
                    Assert.That(seam.Top, Is.GreaterThan(r.Logo.Bottom), at + " " + family + ": under the logo");
                    Assert.That(seam.Bottom, Is.LessThan(r.Plaque.Top), at + " " + family + ": above the plaque");
                    Box shadow = HomeLayers.ShadowBox(picture, family);
                    Assert.That(shadow.CenterY, Is.EqualTo(picture.Top + (feet * picture.Height)).Within(shadow.Height * 0.2f), at + " " + family + ": the shadow under the feet");
                }
            }
        }

        [Test]
        public void ThePetals_DriftDown_AndWrap()
        {
            var picture = new Box(0f, 0f, HomeLayers.PictureWidth, HomeLayers.PictureHeight);
            Box start = HomeLayers.PetalsAt(picture, 0f);
            Assert.That(start.Top, Is.EqualTo(HomeLayers.Petals.Y).Within(Eps));
            Assert.That(HomeLayers.PetalsAt(picture, 10f).Top, Is.GreaterThan(start.Top));
            float lap = HomeLayers.PictureHeight / HomeLayers.PetalsSpeed;
            Assert.That(HomeLayers.PetalsAt(picture, lap * HomeLayers.PetalsSwaySeconds).Top, Is.EqualTo(start.Top).Within(0.5f), "a whole number of laps comes back");
        }
    }
}
