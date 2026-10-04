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
            // No Ads: in the middle of its sequence Sprig and the sign are gone and the lotus is full.
            IReadOnlyList<PromoLayer> lotus = HomePromo.Layers(PromoScene.NoAds, Scene, HomePromo.NoAdsAt + 2f, true);
            Assert.That(Alpha(lotus, HomePromo.NoAdsLotus), Is.EqualTo(1f).Within(0.01f));
            Assert.That(Alpha(lotus, HomePromo.NoAdsSprig), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Alpha(lotus, HomePromo.NoAdsSign), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Alpha(HomePromo.Layers(PromoScene.NoAds, Scene, 0.5f, true), HomePromo.NoAdsLotus), Is.EqualTo(0f), "no lotus while idling");

            // Daily: no sparkles before the stamp is down, then the burst; the stamp takes its time.
            for (float a = 0f; a < 1.3f; a += 0.05f)
            {
                Assert.That(Alpha(HomePromo.Layers(PromoScene.Daily, Scene, HomePromo.DailyAt + a, true), HomePromo.DailySparkles), Is.EqualTo(0f), "sparkles at " + a);
            }

            Assert.That(Alpha(HomePromo.Layers(PromoScene.Daily, Scene, HomePromo.DailyAt + 1.6f, true), HomePromo.DailySparkles), Is.GreaterThan(0.5f));
            Assert.That(Alpha(HomePromo.Layers(PromoScene.Daily, Scene, HomePromo.DailyAt + 0.8f, true), HomePromo.DailyStamp), Is.GreaterThan(0.9f), "the stamp hovers before it presses");
            Assert.That(HomePromo.Layers(PromoScene.Daily, Scene, 3f, false).Select(l => l.Picture), Does.Not.Contain("promo-daily-badge"));
        }

        [Test]
        public void WhileIdling_TheProps_StandOnTheirStand_AndTheSignLeansOnSprigsPalms()
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
                Assert.That(plaque.Height * HomePromo.LabelShare, Is.GreaterThan(0.08f * Scene.Width), scene + ": letters about 0.09 of the scene's width");
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

        [Test]
        public void TheScenes_StandUnderTheLogo_ClearOfHomesOtherButtons()
        {
            foreach ((float w, float h, Insets insets) in new[] { (1080f, 1920f, new Insets(63f, 0f)), (1080f, 2340f, new Insets(110f, 63f)), (1080f, 2520f, new Insets(120f, 66f)), (720f, 1600f, new Insets(48f, 30f)) })
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                string at = w + "x" + h;
                foreach (PromoScene scene in new[] { PromoScene.NoAds, PromoScene.Daily })
                {
                    Box box = r.Promo(scene);
                    Assert.That(box.Width, Is.EqualTo(HomePromo.WidthShare * r.W).Within(0.5f), at);
                    Assert.That(box.Within(r.Safe), Is.True, at + " " + scene);
                    Assert.That(box.Top, Is.GreaterThanOrEqualTo(r.Logo.Bottom), at + " " + scene + ": under the logo");
                    Assert.That(box.Bottom, Is.LessThan(r.Plaque.Top), at + " " + scene + ": above the plaque");
                    foreach (Box other in new[] { r.Settings, r.Avatar, r.Petals, r.Daily, r.Play, r.Plaque })
                    {
                        Assert.That(box.Overlaps(other), Is.False, at + " " + scene + " overlaps " + other);
                    }
                }

                Assert.That(r.NoAds.Left, Is.EqualTo(r.Safe.Left + (0.04f * r.W)).Within(0.5f), at);
                Assert.That(r.DailyReward.Right, Is.EqualTo(r.Safe.Right - (0.04f * r.W)).Within(0.5f), at);
                Assert.That(r.Daily.Top, Is.GreaterThan(r.DailyReward.Bottom), at + ": the Daily Challenge under the Daily Reward's scene");
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
