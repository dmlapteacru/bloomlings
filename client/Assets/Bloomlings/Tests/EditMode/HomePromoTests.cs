using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests.EditMode
{
    /// <summary>
    /// Home's promo scenes (spec 005 FR-032, <see cref="HomePromo"/>): the two attention sequences never play together,
    /// each starts and ends on its idle pose, the props stay on their stand, the labels sit on the plaques, the pictures
    /// exist with their slots and records, and the scenes stand under the logo, clear of Home's other buttons.
    /// </summary>
    public sealed class HomePromoTests
    {
        private static readonly Box Scene = HomePromo.SceneBox(43f, 600f, 0.27f * 1080f);

        [Test]
        public void TheTwoAttentionSequences_NeverPlayTogether_AndFollowTheirCycle()
        {
            for (float t = 0f; t < 120f; t += 0.05f)
            {
                bool noAds = HomePromo.AttentionAt(PromoScene.NoAds, t, true) >= 0f;
                bool daily = HomePromo.AttentionAt(PromoScene.Daily, t, true) >= 0f;
                Assert.That(noAds && daily, Is.False, "both at " + t);
            }

            Assert.That(HomePromo.AttentionAt(PromoScene.NoAds, 0.5f, true), Is.EqualTo(-1f), "nothing right as Home opens");
            Assert.That(HomePromo.AttentionAt(PromoScene.NoAds, HomePromo.NoAdsAt + 0.5f, true), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(HomePromo.AttentionAt(PromoScene.NoAds, HomePromo.NoAdsAt + HomePromo.CycleSeconds + 1f, true), Is.EqualTo(1f).Within(1e-3f));
            Assert.That(HomePromo.AttentionAt(PromoScene.Daily, HomePromo.DailyAt + 1f, true), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(HomePromo.AttentionAt(PromoScene.Daily, HomePromo.DailyAt + 1f, false), Is.EqualTo(-1f), "a claimed reward idles");
            Assert.That(HomePromo.AttentionAt(PromoScene.NoAds, HomePromo.NoAdsAt + HomePromo.NoAdsSeconds + 0.1f, true), Is.EqualTo(-1f));
        }

        [TestCase(PromoScene.NoAds)]
        [TestCase(PromoScene.Daily)]
        public void AnAttentionSequence_StartsAndEndsOnTheIdlePose(PromoScene scene)
        {
            float at = scene == PromoScene.NoAds ? HomePromo.NoAdsAt : HomePromo.DailyAt;
            float length = scene == PromoScene.NoAds ? HomePromo.NoAdsSeconds : HomePromo.DailySeconds;
            foreach (float t in new[] { at, at + length })
            {
                AssertSame(Visible(HomePromo.Layers(scene, Scene, t, true)), Visible(HomePromo.Layers(scene, Scene, t, false)), scene + " at " + t);
            }
        }

        [Test]
        public void TheSequences_DoWhatTheOwnerAsked()
        {
            // Sprig remains on stage while the sign visibly leaves it, then the scene resets.
            for (float t = 0; t < HomePromo.CycleSeconds * 2; t += .05f)
            {
                var layers = HomePromo.Layers(PromoScene.NoAds, Scene, t, true);
                Assert.That(Alpha(layers, HomePromo.NoAdsSprig) + Alpha(layers, HomePromo.NoAdsSprigPush), Is.EqualTo(1f).Within(.001f));
            }
            var launch = HomePromo.Layers(PromoScene.NoAds, Scene, HomePromo.NoAdsAt + 1.2f, true);
            PromoLayer sign = launch.Single(l => l.Picture == HomePromo.NoAdsSign);
            Assert.That(sign.X, Is.GreaterThan(Scene.Right), "the sign crosses the stand's edge");
            Assert.That(sign.Rotation, Is.GreaterThan(45f), "the sign tumbles instead of gently rocking");
            Assert.That(Alpha(HomePromo.Layers(PromoScene.NoAds, Scene, HomePromo.NoAdsAt + 1.5f, true), HomePromo.NoAdsSign), Is.Zero);
            Assert.That(Alpha(HomePromo.Layers(PromoScene.NoAds, Scene, HomePromo.NoAdsAt + .5f, true), HomePromo.NoAdsSprigPush), Is.EqualTo(1f));
            Assert.That(HomePromo.DailyAt - HomePromo.NoAdsAt - HomePromo.NoAdsSeconds, Is.GreaterThanOrEqualTo(2f));
            Assert.That(HomePromo.CycleSeconds + HomePromo.NoAdsAt - HomePromo.DailyAt - HomePromo.DailySeconds, Is.GreaterThanOrEqualTo(2f));
            var before = HomePromo.Layers(PromoScene.Daily, Scene, HomePromo.DailyAt + 1.2f, true);
            Assert.That(Alpha(before, HomePromo.DailyImprint), Is.Zero, "blank page before contact");
            Assert.That(Alpha(before, HomePromo.DailySparkles), Is.Zero, "no impact glint before contact");
            var pressed = HomePromo.Layers(PromoScene.Daily, Scene, HomePromo.DailyAt + 1.6f, true);
            Assert.That(Alpha(pressed, HomePromo.DailyImprint), Is.GreaterThan(.9f));
            Assert.That(pressed.Single(l => l.Picture == HomePromo.DailyStamp).Y - before.Single(l => l.Picture == HomePromo.DailyStamp).Y, Is.GreaterThan(Scene.Width * .20f), "large visible downstroke");
            var reveal = HomePromo.Layers(PromoScene.Daily, Scene, HomePromo.DailyAt + 2.5f, true);
            Assert.That(Alpha(reveal, HomePromo.DailyImprint), Is.EqualTo(1f), "the page keeps its mark after the tool leaves");
            Assert.That(Alpha(reveal, HomePromo.DailyStamp), Is.Zero);
            Assert.That(Alpha(reveal, HomePromo.DailyBookOpen), Is.EqualTo(1f));
            for (float t = 0; t < HomePromo.CycleSeconds; t += .1f)
            {
                var quiet = HomePromo.Layers(PromoScene.Daily, Scene, t, false);
                Assert.That(Alpha(quiet, HomePromo.DailyBook), Is.EqualTo(1f));
                Assert.That(Alpha(quiet, HomePromo.DailyBookOpen), Is.Zero);
                Assert.That(Alpha(quiet, HomePromo.DailyStamp), Is.Zero);
                Assert.That(Alpha(quiet, HomePromo.DailySparkles), Is.Zero);
                Assert.That(Alpha(quiet, HomePromo.DailyImprint), Is.Zero);
            }
        }

        [Test]
        public void WhileIdling_TheProps_StayOnTheirStand_AndTheSignStaysBesideSprig()
        {
            foreach (PromoScene scene in new[] { PromoScene.NoAds, PromoScene.Daily })
            {
                for (float t = 0f; t < 6f; t += 0.1f)
                {
                    foreach (PromoLayer layer in HomePromo.Layers(scene, Scene, t, false).Where(l => l.Alpha > 0f))
                    {
                        Assert.That(layer.X, Is.InRange(Scene.Left, Scene.Right), scene + " " + layer.Picture + " at " + t);
                        Assert.That(layer.Y, Is.InRange(Scene.Top, Scene.Bottom), scene + " " + layer.Picture + " at " + t);
                    }
                }
            }

            // The sign's pivot (its bottom-right corner) stays right of Sprig's feet, on the stand, a sign's width away.
            PromoLayer sprig = HomePromo.Layers(PromoScene.NoAds, Scene, 0f, false).Single(l => l.Picture == HomePromo.NoAdsSprig);
            PromoLayer sign = HomePromo.Layers(PromoScene.NoAds, Scene, 0f, false).Single(l => l.Picture == HomePromo.NoAdsSign);
            Assert.That(sign.X - sprig.X, Is.InRange(0.3f * Scene.Width, 0.55f * Scene.Width));
            Assert.That(sign.Rotation, Is.InRange(2f, 6f), "leaning away from Sprig");
        }

        [Test]
        public void TheLabels_SitOnTheirPlaques_InsideTheScene()
        {
            foreach (PromoScene scene in new[] { PromoScene.NoAds, PromoScene.Daily })
            {
                Box plaque = HomePromo.Plaque(Scene, scene);
                Assert.That(plaque.Within(Scene), Is.True, scene.ToString());
                Assert.That(plaque.Width, Is.GreaterThan(0.45f * Scene.Width), scene.ToString());
                Assert.That(plaque.Height * HomePromo.LabelShare, Is.GreaterThan(0.06f * Scene.Width), scene + ": letters at least 0.06 of the scene's width");
            }
        }

        [Test]
        public void ThePictures_ExistWithTheirSlotsAndRecords()
        {
            string folder = Path.Combine(Application.dataPath, "Bloomlings", "Art", "Decor", "Resources", OwnerPictures.DecorFolder);
            string notices = File.ReadAllText(Path.Combine(Application.dataPath, "..", "THIRD_PARTY_NOTICES.md"));
            foreach (string picture in HomePromo.Pictures)
            {
                Assert.That(File.Exists(Path.Combine(folder, picture + ".png")), Is.True, picture);
                Assert.That(notices, Does.Contain("| `client/Assets/Bloomlings/Art/Decor/Resources/Decor/" + picture + ".png` |"), picture);
                Assert.That(AssetSlots.Has(OwnerPictures.SlotOf(picture)), Is.True, picture);
            }

            Assert.That(OwnerPictures.SlotOf(HomePromo.NoAdsSprig), Is.EqualTo(HomePromo.Slot(PromoScene.NoAds)));
            Assert.That(OwnerPictures.SlotOf(HomePromo.DailyBook), Is.EqualTo(HomePromo.Slot(PromoScene.Daily)));
        }

        /// <summary>
        /// The promo scenes under the header, in the logo's place since Home lost its logo (the owner's tuning of
        /// 2026-10-05, spec 005 FR-036), 5% larger, their plates clear of Home's other buttons.
        /// </summary>
        [Test]
        public void TheScenes_StandUnderTheHeader_ClearOfHomesOtherButtons()
        {
            foreach ((float w, float h, Insets insets) in new[] { (1080f, 1920f, new Insets(63f, 0f)), (1080f, 2340f, new Insets(110f, 63f)), (1080f, 2520f, new Insets(120f, 66f)), (720f, 1600f, new Insets(48f, 30f)) })
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                string at = w + "x" + h;
                foreach (PromoScene scene in new[] { PromoScene.NoAds, PromoScene.Daily })
                {
                    Box box = r.Promo(scene);
                    Assert.That(box.Width, Is.EqualTo(HomePromo.WidthShare * ReferenceHomeRegions.PromoScale * r.W).Within(0.5f), at);
                    Box plate = HomePromo.PlateBox(box);
                    Assert.That(plate.Within(r.Safe), Is.True, at + " " + scene);
                    Assert.That(plate.Top, Is.GreaterThan(r.Header.Bottom), at + " " + scene + ": under the header");
                    Assert.That(plate.Bottom, Is.LessThan(r.Plaque.Top), at + " " + scene + ": above the plaque");
                    foreach (Box other in new[] { r.Settings, r.Avatar, r.Petals, r.Daily, r.Play, r.Plaque })
                    {
                        Assert.That(plate.Overlaps(other), Is.False, at + " " + scene + " overlaps " + other);
                    }
                }

                Assert.That(r.NoAds.CenterX - r.Safe.Left, Is.EqualTo(r.Safe.Right - r.DailyReward.CenterX).Within(0.5f), at + ": mirrored promo columns");
                Assert.That(r.DailyReward.CenterX, Is.EqualTo(r.Daily.CenterX).Within(0.5f), at + ": album centered above Challenge");
                Assert.That(r.Daily.Top, Is.GreaterThan(r.DailyReward.Bottom), at + ": the Daily Challenge under the Daily Reward's scene");
                Assert.That(r.Daily.Bottom, Is.LessThan(r.Safe.Top + .45f * r.Safe.Height), at + ": keep the challenge above the front hero's head");
            }
        }

        private static List<PromoLayer> Visible(IReadOnlyList<PromoLayer> layers) => layers.Where(l => l.Alpha > 0.001f).ToList();

        private static float Alpha(IReadOnlyList<PromoLayer> layers, string picture) => layers.Where(l => l.Picture == picture).Select(l => l.Alpha).DefaultIfEmpty(0f).Max();

        private static void AssertSame(List<PromoLayer> a, List<PromoLayer> b, string at)
        {
            Assert.That(a.Select(l => l.Picture), Is.EqualTo(b.Select(l => l.Picture)), at);
            for (int i = 0; i < a.Count; i++)
            {
                string name = at + " " + a[i].Picture;
                Assert.That(a[i].X, Is.EqualTo(b[i].X).Within(0.5f), name);
                Assert.That(a[i].Y, Is.EqualTo(b[i].Y).Within(0.5f), name);
                Assert.That(a[i].ScaleX, Is.EqualTo(b[i].ScaleX).Within(0.005f), name);
                Assert.That(a[i].ScaleY, Is.EqualTo(b[i].ScaleY).Within(0.005f), name);
                Assert.That(a[i].Rotation, Is.EqualTo(b[i].Rotation).Within(0.2f), name);
                Assert.That(a[i].Alpha, Is.EqualTo(b[i].Alpha).Within(0.01f), name);
            }
        }
    }
}
