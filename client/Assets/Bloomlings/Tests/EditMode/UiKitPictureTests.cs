using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Definitions;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The engine-free parts of the Unity kit of spec 005 (contracts/look.md §2.1, §3): the picture primitive's keys, row
    /// flip and edge bleed, the sliced UI pictures, the fit of rounded shapes and pictures to rects, and the recipe
    /// geometry the kit lays out (stone border, pedestal, petals, cost pill text).
    /// </summary>
    public class UiKitPictureTests
    {
        [Test]
        public void CacheKeys_AreKeyAtSize_WithTheBorderWhenSliced()
        {
            Assert.That(PicturePixels.CacheKey("tile.candy/board", 64, 64), Is.EqualTo(UiRaster.CacheKey("tile.candy/board", 64, 64)));
            Assert.That(PicturePixels.CacheKey("mat.wood.light/plank", 512, 128), Is.EqualTo("mat.wood.light/plank@512x128"));
            Assert.That(PicturePixels.CacheKey("mat.wood.light/plank", 512, 128, 40f, 63f, 40f, 63f), Is.EqualTo("mat.wood.light/plank@512x128/40,63,40,63"));
            Assert.That(PicturePixels.CacheKey("a", 8, 8, 1f), Is.Not.EqualTo(PicturePixels.CacheKey("a", 8, 8, 2f)));
        }

        [Test]
        public void FlipRows_PutsTheTopRowAtTheBottom()
        {
            const int w = 2;
            const int h = 3;
            byte[] rgba = new byte[w * h * 4];
            for (int row = 0; row < h; row++)
            {
                for (int i = 0; i < w * 4; i++)
                {
                    rgba[(row * w * 4) + i] = (byte)(10 * (row + 1));
                }
            }

            byte[] flipped = PicturePixels.FlipRows(rgba, w, h);
            Assert.That(flipped.Take(w * 4), Is.All.EqualTo(30), "the last top-down row comes first");
            Assert.That(flipped.Skip(2 * w * 4), Is.All.EqualTo(10), "the first top-down row comes last");
            Assert.That(rgba[0], Is.EqualTo(10), "the source is not changed");
            Assert.Throws<ArgumentException>(() => PicturePixels.FlipRows(new byte[5], w, h));
        }

        [Test]
        public void BleedEdges_GivesClearNeighborsTheEdgeColor_KeepingThemClear()
        {
            const int w = 4;
            const int h = 1;
            byte[] rgba = new byte[w * h * 4];
            Put(rgba, 0, 200, 40, 10, 255);
            PicturePixels.BleedEdges(rgba, w, h);
            Assert.That((rgba[4], rgba[5], rgba[6], rgba[7]), Is.EqualTo(((byte)200, (byte)40, (byte)10, (byte)0)), "the neighbor takes the color, not the alpha");
            Assert.That((rgba[8], rgba[11]), Is.EqualTo(((byte)0, (byte)0)), "pixels two away stay untouched");
            Assert.That(rgba[3], Is.EqualTo(255));
        }

        [Test]
        public void ForTexture_FlipsAndBleeds()
        {
            const int w = 2;
            const int h = 2;
            byte[] rgba = new byte[w * h * 4];
            Put(rgba, 0, 255, 0, 0, 255);
            byte[] texture = PicturePixels.ForTexture(rgba, w, h);
            Assert.That(texture[(2 * 4) + 3], Is.EqualTo(255), "the top-left pixel is now in the bottom-up texture's top row");
            Assert.That(texture[(3 * 4) + 0], Is.EqualTo(255), "its clear neighbor took its red");
            Assert.That(texture[(3 * 4) + 3], Is.EqualTo(0));
        }

        [Test]
        public void RoundRects_AreSolidRingsOrFades()
        {
            const int s = PicturePixels.RoundSize;
            byte[] solid = PicturePixels.RoundRect(s, s, PicturePixels.RoundUnit);
            Assert.That(Alpha(solid, s, 0, 0), Is.EqualTo(0), "the corner of a disc is clear");
            Assert.That(Alpha(solid, s, s / 2, s / 2), Is.EqualTo(255));
            Assert.That(Alpha(solid, s, s / 2, 0), Is.GreaterThan(100), "the straight edge reaches the border");

            byte[] ring = PicturePixels.RoundRect(s, s, PicturePixels.RoundUnit, RoundFill.Ring, 8f);
            Assert.That(Alpha(ring, s, s / 2, 3), Is.EqualTo(255), "inside the band");
            Assert.That(Alpha(ring, s, s / 2, 20), Is.EqualTo(0), "past the band");
            Assert.That(Alpha(ring, s, s / 2, s / 2), Is.EqualTo(0), "the middle, which the 9-slice stretches, is clear");

            byte[] fade = PicturePixels.RoundRect(s, s, PicturePixels.RoundUnit, RoundFill.Fade, 32f);
            Assert.That(Alpha(fade, s, s / 2, 2), Is.GreaterThan(Alpha(fade, s, s / 2, 16)));
            Assert.That(Alpha(fade, s, s / 2, 16), Is.GreaterThan(Alpha(fade, s, s / 2, 28)));
            Assert.That(Alpha(fade, s, s / 2, 40), Is.EqualTo(0));
            Assert.That(solid.Where((b, i) => i % 4 != 3), Is.All.EqualTo(255), "white, tinted by the image color");
        }

        [Test]
        public void TheDashedOutline_AlternatesDashesAndGaps_AroundAClearMiddle()
        {
            const int s = 100;
            byte[] dashed = PicturePixels.DashedOutline(s, s, 9f, 14f, 3f, 9f, 6f);
            int[] alongTop = Enumerable.Range(25, 50).Select(x => (int)Alpha(dashed, s, x, 9)).ToArray();
            Assert.That(alongTop.Count(a => a > 200), Is.GreaterThan(10), "dashes");
            Assert.That(alongTop.Count(a => a == 0), Is.GreaterThan(5), "gaps");
            Assert.That(Alpha(dashed, s, s / 2, s / 2), Is.EqualTo(0));
            Assert.That(Alpha(dashed, s, 2, 2), Is.EqualTo(0), "outside the inset outline");
            Assert.That(dashed, Is.EqualTo(PicturePixels.DashedOutline(s, s, 9f, 14f, 3f, 9f, 6f)));
        }

        [Test]
        public void LightRays_GlowAtTheCenter_AndFadeOut()
        {
            const int s = 160;
            byte[] rays = PicturePixels.LightRays(s, DesignTokens.Colors.RayLight);
            Assert.That(Alpha(rays, s, s / 2, s / 2), Is.GreaterThan(Alpha(rays, s, s - 4, s / 2 + 30)));
            Assert.That(Alpha(rays, s, 0, 0), Is.EqualTo(0), "the corner is past every ray");
            Assert.That(rays[0], Is.EqualTo(DesignTokens.Colors.RayLight.R));
            Assert.That(rays, Is.EqualTo(PicturePixels.LightRays(s, DesignTokens.Colors.RayLight)));
        }

        [Test]
        public void CoverUv_FillsTheArea_CroppingTheLongerSide()
        {
            Assert.That(PicturePixels.CoverUv(100, 100, 50, 50), Is.EqualTo((0f, 0f, 1f, 1f)));
            (float x, float y, float w, float h) = PicturePixels.CoverUv(200, 100, 100, 100);
            Assert.That((x, y, w, h), Is.EqualTo((0.25f, 0f, 0.5f, 1f)), "a wide picture loses its sides");
            (x, y, w, h) = PicturePixels.CoverUv(1080, 2340, 1080, 1920);
            Assert.That(w, Is.EqualTo(1f));
            Assert.That(h, Is.EqualTo(1920f / 2340f).Within(1e-4f), "a taller picture loses its top and bottom");
            Assert.That(y, Is.EqualTo((1f - h) / 2f).Within(1e-4f));
        }

        [Test]
        public void RoundShapes_MapTheSpriteBorderToTheRadius()
        {
            (float m, float share) = RoundShape.Fit(200f, 100f, 20f, RoundFill.Solid, 0f);
            Assert.That(m, Is.EqualTo(PicturePixels.RoundUnit / 20f), "border pixels / multiplier = the radius in canvas units");
            Assert.That(share, Is.EqualTo(0f));
            (m, _) = RoundShape.Fit(200f, 100f, float.MaxValue, RoundFill.Solid, 0f);
            Assert.That(PicturePixels.RoundUnit / m, Is.EqualTo(50f).Within(1e-4f), "a pill: half the shorter side");
            (m, share) = RoundShape.Fit(200f, 100f, 20f, RoundFill.Ring, 4f);
            Assert.That(PicturePixels.RoundUnit / m, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(share, Is.EqualTo(0.2f).Within(1e-4f));
            (m, share) = RoundShape.Fit(200f, 100f, 10f, RoundFill.Fade, 30f);
            Assert.That(PicturePixels.RoundUnit / m, Is.EqualTo(30f).Within(1e-4f), "a band wider than the radius spans the border");
            Assert.That(share, Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void PictureSizes_AreScreenPixels_RoundedUpToEight()
        {
            Assert.That(PictureFit.PixelSize(100f, 41f, PictureShape.Rect, 1f), Is.EqualTo((104, 48)));
            Assert.That(PictureFit.PixelSize(100f, 41f, PictureShape.Rect, 2f), Is.EqualTo((200, 88)));
            Assert.That(PictureFit.PixelSize(100f, 41f, PictureShape.Square, 1f), Is.EqualTo((48, 48)));
            Assert.That(PictureFit.PixelSize(100f, 41f, PictureShape.SquareByWidth, 1f), Is.EqualTo((104, 104)));
            (int w, int h) = PictureFit.PixelSize(4000f, 1000f, PictureShape.Rect, 1f);
            Assert.That(w, Is.EqualTo(PictureFit.MaxSide));
            Assert.That(h, Is.EqualTo(256), "a capped picture keeps its aspect");
        }

        [Test]
        public void TheStoneBorder_RingsTheGrid_WithCornersAndRuns()
        {
            const float cell = 50f;
            var grid = new Box(0f, 0f, 4 * cell, 2 * cell);
            var blocks = StoneBorderView.Blocks(grid, cell, 0.42f);
            Assert.That(blocks.Take(4).Select(b => b.Seed), Is.EqualTo(new[] { 1, 2, 3, 4 }), "the four corners first");
            float gap = cell * 0.04f;
            int across = (int)Math.Round(((4 * cell) + (2 * gap)) / (cell * 0.9f));
            int down = (int)Math.Round(((2 * cell) + (2 * gap)) / (cell * 0.9f));
            Assert.That(blocks.Count, Is.EqualTo(4 + (2 * across) + (2 * down)), "four corners, then runs of about 0.9 cell per side");
            float outer = gap + (cell * 0.42f);
            foreach ((Box box, int _, float _) in blocks)
            {
                Assert.That(box.Within(grid.Inset(-outer)), Is.True);
                Assert.That(box.Overlaps(grid.Inset(-gap)), Is.False, "the blocks lie outside the dark gap");
            }

            Assert.That(blocks, Is.EqualTo(StoneBorderView.Blocks(grid, cell, 0.42f)), "the same place, the same stones");
        }

        [Test]
        public void ThePedestalTop_IsTheDrumsTopEllipse()
        {
            Box top = UiKit.PedestalTop(new Box(0f, 0f, 230f, 100f));
            Assert.That(top.Width, Is.EqualTo(230f));
            Assert.That(top.Height, Is.EqualTo(2f * Math.Min((115f - 1f) * 0.28f, 96f * 0.3f)).Within(1e-3f));
        }

        [Test]
        public void FallingPetals_AreTheSameAtTheSameTime_InsideTheArea()
        {
            var area = new Box(0f, 0f, 400f, 600f);
            for (int i = 0; i < 10; i++)
            {
                (Box box, float sx, float sy) = FallingPetalsView.Petal(i, area, 3.5f, 1f);
                Assert.That(FallingPetalsView.Petal(i, area, 3.5f, 1f), Is.EqualTo((box, sx, sy)));
                Assert.That(box.CenterX, Is.InRange(-area.Width * 0.05f, area.Width * 1.05f));
                Assert.That(box.CenterY, Is.InRange(area.Top - box.Height, area.Bottom + box.Height));
                Assert.That(sx, Is.InRange(0.25f, 1f));
            }
        }

        [Test]
        public void CostPills_ShowAPrice_Free_OrCharges()
        {
            Loc.LoadEnglish(File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings", "UI", "Localization", "Resources", Loc.EnglishResource + ".csv")));
            Assert.That(CostPillView.Text(Cost.Petals(1240)), Is.EqualTo(NumberText.Group(1240)));
            Assert.That(CostPillView.Text(Cost.Free), Is.EqualTo(Loc.T("common.free")));
            Assert.That(CostPillView.Text(Cost.Charges(3)), Is.EqualTo(Loc.F("common.charges", 3)));
            Assert.That(CostPillView.Text(Cost.Charges(3)), Does.Contain("3"));
        }

        private static void Put(byte[] rgba, int pixel, byte r, byte g, byte b, byte a)
        {
            rgba[pixel * 4] = r;
            rgba[(pixel * 4) + 1] = g;
            rgba[(pixel * 4) + 2] = b;
            rgba[(pixel * 4) + 3] = a;
        }

        private static byte Alpha(byte[] rgba, int width, int x, int y) => rgba[(((y * width) + x) * 4) + 3];
    }
}
