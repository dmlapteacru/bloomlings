using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Tests.EditMode
{
    /// <summary>
    /// The owner's Home tuning of 2026-10-05 (spec 005 FR-036, research D29): the stage's box and the heroes' size, the
    /// promo scenes' plates and the soft shadows' pictures.
    /// </summary>
    public sealed class HomeTuningTests
    {
        [Test]
        public void TheStage_IsTheCoverBoxShrunkTowardTheScreensMiddleAt60Percent()
        {
            foreach ((float w, float h) in new[] { (1080f, 2340f), (1080f, 1920f), (720f, 1600f) })
            {
                var screen = new Box(0f, 0f, w, h);
                Box cover = HomeLayers.Cover(screen);
                Box stage = HomeLayers.Stage(screen);
                Assert.That(stage.Width, Is.EqualTo(cover.Width * HomeLayers.StageScale).Within(0.01f));
                Assert.That(stage.Height, Is.EqualTo(cover.Height * HomeLayers.StageScale).Within(0.01f));
                float ay = h * HomeLayers.StageAnchorShare;
                Assert.That(stage.CenterX, Is.EqualTo(screen.CenterX).Within(0.01f));
                Assert.That((ay - stage.Top) / (ay - cover.Top), Is.EqualTo(HomeLayers.StageScale).Within(1e-4f), "the anchor stays put");
            }
        }

        [Test]
        public void EachHero_StandsAtItsScaleAboutItsFeet()
        {
            Box picture = HomeLayers.Stage(new Box(0f, 0f, 1080f, 2340f));
            foreach (Family family in HomeLayers.DrawOrder)
            {
                (float x, float feet, float height) = HomeLayers.Placement(family);
                Box cell = HomeLayers.HeroCell(picture, family);
                float fill = HeroMotion.Has(family) ? HeroMotion.Fill(family) : 0.8f;
                Assert.That(cell.Height, Is.EqualTo(height * HomeLayers.HeroScale * picture.Width / fill).Within(0.01f), family.ToString());
                Assert.That(cell.CenterX, Is.EqualTo(picture.Left + (x * picture.Width)).Within(0.01f), family.ToString());
                Assert.That(cell.Top + (cell.Height * HeroMotion.FootLine), Is.EqualTo(picture.Top + (feet * picture.Height)).Within(0.01f), family + ": the feet stay on the fountain");
            }

            // Twig stands further right than Drop, so more of Drop shows beside it (the owner, 2026-10-05).
            Assert.That(HomeLayers.Placement(Family.Twig).X - HomeLayers.Placement(Family.Drop).X, Is.GreaterThanOrEqualTo(0.1f));
        }

        [Test]
        public void APromoPicturesShadow_KeepsItsPivotPoint_AndDropsALittle()
        {
            var layer = new PromoLayer(HomePromo.NoAdsSign, 400f, 600f, 200f, 200f, 0.3f, 0.8f, 1.1f, 0.9f, 12f, 0.7f);
            (PromoLayer shadow, float pad, float blur) = HomePromo.ShadowOf(layer, 1080f);
            Assert.That(blur, Is.EqualTo(16f).Within(1e-3f), "the owner's 16 px on a 1080 px screen");
            Assert.That(pad, Is.EqualTo(HomePromo.ShadowPad * blur).Within(1e-3f));
            Assert.That(shadow.X, Is.EqualTo(layer.X));
            Assert.That(shadow.Y, Is.EqualTo(layer.Y + 8f).Within(1e-3f), "8 px down");
            Assert.That(shadow.Box.Left + (shadow.Width * shadow.PivotX), Is.EqualTo(layer.X).Within(1e-3f));
            Assert.That(shadow.Box.Top + (shadow.Height * shadow.PivotY), Is.EqualTo(shadow.Y).Within(1e-3f));
            Assert.That(shadow.Width, Is.EqualTo(layer.Width + (2f * pad)).Within(1e-3f));
            Assert.That((shadow.ScaleX, shadow.ScaleY, shadow.Rotation, shadow.Alpha), Is.EqualTo((layer.ScaleX, layer.ScaleY, layer.Rotation, layer.Alpha)));
        }

        [Test]
        public void ASilhouettesShadow_IsGardenShadow_SoftAtItsEdge_AndClearFarOut()
        {
            // A solid 20 × 20 square in a 60 × 60 picture with 20 px of room: dark at the middle, half at the edge, clear far out.
            var mask = new byte[20 * 20];
            Array.Fill(mask, (byte)255);
            byte[] shadow = UiRaster.SilhouetteShadow(mask, 20, 20, 60, 60, 20f, 20f, 3f, HomePromo.ShadowAlpha);
            int At(int x, int y) => shadow[(((y * 60) + x) * 4) + 3];
            Assert.That(At(30, 30), Is.EqualTo((int)Math.Round(HomePromo.ShadowAlpha * 255f)).Within(3), "the middle");
            Assert.That(At(20, 30), Is.InRange(50, 100), "about half at the edge");
            Assert.That(At(2, 2), Is.EqualTo(0), "clear far out");
            Assert.That((shadow[(((30 * 60) + 30) * 4) + 0], shadow[(((30 * 60) + 30) * 4) + 1], shadow[(((30 * 60) + 30) * 4) + 2]), Is.EqualTo((C.GardenShadow.R, C.GardenShadow.G, C.GardenShadow.B)));
            Assert.That(UiRaster.SilhouetteShadow(mask, 20, 20, 60, 60, 20f, 20f, 3f, HomePromo.ShadowAlpha), Is.EqualTo(shadow), "deterministic");
        }

        [Test]
        public void ThePlatesShadow_IsARoundedBoxBlurred()
        {
            byte[] shadow = UiRaster.RoundShadow(80, 60, 20f, 20f, 6f, 4f, HomePromo.PlateShadowAlpha);
            int At(int x, int y) => shadow[(((y * 80) + x) * 4) + 3];
            Assert.That(At(40, 30), Is.EqualTo((int)Math.Round(HomePromo.PlateShadowAlpha * 255f)).Within(2), "the middle");
            Assert.That((At(19, 30) + At(20, 30)) / 2f, Is.EqualTo(HomePromo.PlateShadowAlpha * 255f / 2f).Within(3f), "half at the edge, between the two pixels round it");
            Assert.That(At(0, 0), Is.EqualTo(0), "clear in the corner");
        }

        [Test]
        public void APlate_GrowsTheScenesBox()
        {
            var scene = new Box(100f, 200f, 400f, 400f);
            Box plate = HomePromo.PlateBox(scene);
            Assert.That(plate.Width, Is.EqualTo(scene.Width * (1f + (2f * HomePromo.PlateGrowShare))).Within(1e-3f));
            Assert.That(plate.CenterX, Is.EqualTo(scene.CenterX).Within(1e-3f));
            Assert.That(HomePromo.PlateRadius(plate), Is.EqualTo(HomePromo.PlateRadiusShare * plate.Width).Within(1e-3f));
            (Box box, float pad, float blur) = HomePromo.PlateShadowOf(plate, 1080f);
            Assert.That(blur, Is.EqualTo(12.8f).Within(1e-3f));
            Assert.That(box.CenterY, Is.EqualTo(plate.CenterY + 6.4f).Within(1e-3f));
            Assert.That(box.Width, Is.EqualTo(plate.Width + (2f * pad)).Within(1e-3f));
        }
    }
}
