using System;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class PhoneUsabilityTests
    {
        private static ScrollMotion Flick()
        {
            var motion = new ScrollMotion();
            motion.Press(800, 0); motion.Record(680, .04f); motion.Release(600, .07f, 2400);
            return motion;
        }

        [Test]
        public void QuickSwipe_CoastsInTheSameDirection_AndComesToRest()
        {
            var motion = Flick();
            Assert.That(motion.Moving, Is.True);
            float first = motion.Advance(1f / 60, 12), distance = first;
            Assert.That(first, Is.LessThan(0));
            float second = motion.Advance(1f / 60, 12); distance += second;
            Assert.That(Math.Abs(second), Is.LessThan(Math.Abs(first)));
            for (int i = 0; i < 180; i++) distance += motion.Advance(1f / 60, 12);
            Assert.That(motion.Moving, Is.False);
            Assert.That(-distance, Is.InRange(250f, 600f), "a moderate glide, under one viewport");
        }

        [Test]
        public void GlideDistance_DoesNotDependOnFrameRate()
        {
            var sixty = Flick(); var thirty = Flick(); float a = 0, b = 0;
            for (int i = 0; i < 30; i++) a += sixty.Advance(1f / 60, 12);
            for (int i = 0; i < 15; i++) b += thirty.Advance(1f / 30, 12);
            Assert.That(a, Is.EqualTo(b).Within(.1f));
        }

        [Test]
        public void HoldingStillBeforeRelease_DoesNotFling_AndNewTouchStopsMotion()
        {
            var motion = new ScrollMotion();
            motion.Press(800, 0); motion.Record(600, .04f); motion.Record(600, .30f); motion.Release(600, .31f, 2400);
            Assert.That(motion.Moving, Is.False);
            motion = Flick(); motion.Press(400, 1);
            Assert.That(motion.Advance(.02f, 12), Is.Zero);
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2340)]
        [TestCase(720, 1680)]
        public void PurchaseControls_StayReadableAndTouchableAcrossLayouts(int width, int height)
        {
            var store = ScreenLayout.ReferenceStore(width, height, Insets.None, true);
            Box area = ShopBodyDesign.Bounds(store);
            var nodes = ShopBodyDesign.Build(area, key => key, _ => true, _ => 250, _ => true);
            foreach (var node in nodes)
                if (node.Kind == ShopVisualKind.Button)
                    Assert.That(node.Box.Height, Is.GreaterThanOrEqualTo(width * .115f), node.Id);
            Box cosmetic = ShopCatalogDesign.CosmeticCard(area, 0);
            Box animation = ShopCatalogDesign.AnimationCard(area, 0);
            Assert.That(ShopCatalogDesign.Parts(cosmetic, true).Action.Height, Is.GreaterThan(width * .09f));
            Assert.That(ShopCatalogDesign.Parts(animation, false).Action.Height, Is.GreaterThan(width * .09f));
            var edit = ProfileGameDesign.Edit(width, height, Insets.None);
            Assert.That(ProfileGameDesign.PurchaseFace(ProfileGameDesign.CellPrice(edit.Cell(0))).Height, Is.GreaterThan(width * .09f));
            Assert.That(edit.Preview.Width, Is.GreaterThan(width * .23f));
            Assert.That(edit.Cell(2).Top, Is.EqualTo(edit.Cell(0).Top), "three avatars per row");
            Assert.That(edit.Cell(3).Top, Is.GreaterThan(edit.Cell(0).Bottom), "the fourth avatar starts the next row");
            var gameplay = ScreenLayout.ReferenceGameplay(width, height, Insets.None, 4, 5);
            foreach (Box booster in gameplay.Boosters)
            {
                Box plus = GameplaySkin.BoosterBuyPlus(booster);
                Assert.That(plus.CenterX, Is.InRange(booster.Left, booster.Right));
                Assert.That(plus.CenterY, Is.InRange(booster.Top, booster.Bottom));
                Assert.That(plus.Bottom, Is.LessThan(gameplay.PodRow.Top), "the plus never covers the pod below");
            }
            var wardrobe = WardrobeShelfDesign.Page(width, height, Insets.None);
            Assert.That(ProfileGameDesign.PurchaseFace(WardrobeShelfDesign.Action(wardrobe.Cell(0))).Height, Is.GreaterThan(width * .06f),"three-column Wardrobe keeps its price readable");
            Assert.That(WardrobeShelfDesign.PurchaseTouch(wardrobe.Cell(0)).Height, Is.GreaterThanOrEqualTo(DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width,height)));
        }

        [Test]
        public void NonShopPrices_UseTheApprovedTwentyPercentReduction()
        {
            Box original = new Box(10, 20, 210, 120);
            Box face = ProfileGameDesign.PurchaseFace(original);
            Assert.That(face.Width, Is.EqualTo(160).Within(.001f));
            Assert.That(face.Height, Is.EqualTo(80).Within(.001f));
            Assert.That(face.CenterX, Is.EqualTo(original.CenterX));
            Assert.That(face.CenterY, Is.EqualTo(original.CenterY));
        }

        [Test]
        public void Collection_ThousandsOfPicturesKeepABoundedVisibleWindow_AndLastRowReachable()
        {
            foreach (int height in new[] { 1920, 2340, 2520 })
            {
                var r = CollectionGalleryDesign.Page(1080, height, new Insets(90, 60));
                const int count = 5001;
                float offset = r.Maximum(count);
                var rows = r.VisibleRows(count, offset);
                Assert.That(rows.Last, Is.EqualTo(count / 2));
                Assert.That(rows.Last - rows.First, Is.LessThan(8));
                Assert.That(r.Cell(count - 1).Bottom - offset, Is.LessThan(r.Area.Bottom));
                Assert.That(r.Area.Bottom, Is.LessThan(ScreenLayout.BottomNavTop(1080, height, new Insets(90, 60))));
                Assert.That(r.Picture.Top, Is.GreaterThan(r.Area.Top));
                Assert.That(r.Level.Bottom, Is.LessThan(r.Area.Bottom));
            }
        }
    }
}
