using System;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The volume look (spec 005 FR-044, contracts/look.md §6.18; the owner, 2026-10-08): the board's soft cube tiles, the
    /// icon buttons' wooden plate with the cream face raised on it, and the raised glyphs.
    /// </summary>
    public class VolumeRasterTests
    {
        [Test]
        public void ABoardTile_IsASoftCube_ItsTopLighterThanItsFrontFace()
        {
            const int s = 96;
            Rgba leaf = Rgba.FromHex(VariantCatalog.Default.All.First(v => v.IconId == "leaf").ColorHex);
            byte[] tile = UiRaster.TileFace(s, leaf, TileStyle.Board);
            Assert.That(UiRaster.TileLipShare(TileStyle.Board), Is.EqualTo(UiRaster.CubeSide), "the front face under the top");
            Assert.That(tile, Is.EqualTo(UiRaster.TileFace(s, leaf, TileStyle.Board)), "deterministic");

            // The ground shows round the cube, its body is opaque.
            Assert.That(Alpha(tile, s, 1, s / 2), Is.Zero, "the clear margin at the left");
            Assert.That(Alpha(tile, s, s / 2, s - 1), Is.Zero, "and below");
            Assert.That(Alpha(tile, s, s / 2, s / 2), Is.EqualTo(255));

            // The top's middle is lighter than the front face under it, and the top's upper left lighter than its lower right.
            float top = Luma(tile, s, s / 2, (int)(s * 0.3f));
            float front = Luma(tile, s, s / 2, (int)(s * (1f - UiRaster.CubeMargin - (UiRaster.CubeSide / 2f))));
            Assert.That(top, Is.GreaterThan(front + 30f), "the top over its front face");
            Assert.That(Luma(tile, s, (int)(s * 0.12f), (int)(s * 0.3f)), Is.GreaterThan(Luma(tile, s, (int)(s * 0.88f), (int)(s * 0.6f))), "lit from the upper left");

            // The symbol sits on the top's middle, above the tile's.
            Box icon = OwnerPictures.TileIconBox(new Box(0f, 0f, s, s), TileStyle.Board);
            Assert.That(icon.CenterY, Is.EqualTo(s * (1f - UiRaster.CubeSide) / 2f).Within(0.01f));
        }

        [Test]
        public void TheButtonPlate_IsWood_ItsBorderLitAtTheTop_AndOpaqueUnderTheFace()
        {
            const int s = 160;
            byte[] plate = UiRaster.ButtonPlate(s, s);
            Assert.That(plate, Is.EqualTo(UiRaster.ButtonPlate(s, s)), "deterministic");
            Assert.That(Alpha(plate, s, 0, 0), Is.Zero, "the rounded corner");
            Assert.That(Alpha(plate, s, s / 2, 4), Is.EqualTo(255), "the border");
            Assert.That(Alpha(plate, s, s / 2, s / 2), Is.EqualTo(255), "the opening, under the face, is the plate's");

            // The border's top catches the light; its front side at the bottom is deeper.
            float rim = s * GardenLook.IconRimShare;
            float upper = Luma(plate, s, s / 2, (int)(rim * 0.5f));
            float lower = Luma(plate, s, s / 2, s - 3);
            Assert.That(upper, Is.GreaterThan(lower + 20f), "lit at the top, deeper at its lower side");

            // Caramel wood, the owner's pick (between 5a and 6a): warm, red over green over blue, neither pale nor orange.
            (byte r, byte g, byte b) = Rgb(plate, s, (int)(rim * 0.5f), s / 2);
            Assert.That(r, Is.GreaterThan(g));
            Assert.That(g, Is.GreaterThan(b));
            float saturation = (r - b) / (float)r;
            Assert.That(saturation, Is.InRange(0.4f, 0.8f), "the side of the border");
        }

        [Test]
        public void TheRaisedFace_StandsInsideThePlate_LighterOnTopThanItsFrontSide()
        {
            var button = new Box(10f, 20f, 170f, 180f);
            Box face = UiRaster.RaisedFaceBox(button);
            float rim = button.Height * GardenLook.IconRimShare;
            Assert.That(face.Left - button.Left, Is.GreaterThan(rim), "inside the border at the sides");
            Assert.That(face.Top - button.Top, Is.GreaterThan(rim), "and at the top");
            Assert.That(button.Bottom - face.Bottom, Is.LessThan(rim + (button.Height * UiRaster.PlateSide)), "a little over the border at the bottom");

            Box content = UiRaster.RaisedFaceContent(face, button.Height);
            Assert.That(face.Contains(content.Left, content.Top) && face.Contains(content.Right, content.Bottom), Is.True);
            Assert.That(content.Bottom, Is.LessThan(face.Bottom - (button.Height * UiRaster.FaceSide)), "above the face's front side");

            int w = (int)face.Width;
            int h = (int)face.Height;
            byte[] picture = UiRaster.ButtonFace(w, h, GardenLook.White);
            Assert.That(picture.Length, Is.EqualTo(w * h * 4));
            Assert.That(Alpha(picture, w, 0, 0), Is.Zero, "rounded");
            Assert.That(Alpha(picture, w, w / 2, h / 2), Is.EqualTo(255));
            Assert.That(Luma(picture, w, w / 3, h / 3), Is.GreaterThan(Luma(picture, w, w / 2, h - 2) + 15f), "the cream top over its front side");
            Assert.That(UiRaster.ButtonFace(w, h, GardenLook.White.Disabled()), Is.Not.EqualTo(picture), "a disabled face is greyed");
        }

        [Test]
        public void TheFaceBox_GivesBackItsButtonsSide_SquareWideOrTall()
        {
            foreach (Box button in new[] { new Box(0f, 0f, 120f, 120f), new Box(0f, 0f, 260f, 100f), new Box(0f, 0f, 90f, 150f) })
            {
                Box face = UiRaster.RaisedFaceBox(button);
                Assert.That(UiRaster.ButtonSideOfFace(face.Width, face.Height), Is.EqualTo(Math.Min(button.Width, button.Height)).Within(0.01f), button.ToString());
            }
        }

        [Test]
        public void APress_SinksTheFace_ByUpTo3PercentOfTheButton()
        {
            Assert.That(UiRaster.RaisedFaceSink(100f, 0f), Is.Zero);
            Assert.That(UiRaster.RaisedFaceSink(100f, 1f), Is.EqualTo(3f).Within(1e-4));
            Assert.That(UiRaster.RaisedFaceSink(100f, 2f), Is.EqualTo(3f).Within(1e-4), "never deeper");
            Assert.That(UiRaster.RaisedFaceSink(100f, -1f), Is.EqualTo(-0.75f).Within(1e-4), "a quarter up on the spring-back");
        }

        [Test]
        public void ARaisedGlyph_FillsItsBoxAboutTheMiddle_WithItsShadowBelow()
        {
            const int s = 160;
            byte[] glyph = UiRaster.RaisedGlyph(s, "ui.pause", DesignTokens.Colors.InkBrown);
            Assert.That(glyph, Is.EqualTo(UiRaster.RaisedGlyph(s, "ui.pause", DesignTokens.Colors.InkBrown)), "deterministic");
            Assert.That(Alpha(glyph, s, 0, 0), Is.Zero);
            Assert.That(Alpha(glyph, s, s - 1, s - 1), Is.Zero, "the margin holds the shadow");

            // Opaque rows of the bars: as far from the top as from the bottom (the shape about the middle), the shadow under them.
            int first = -1;
            int last = -1;
            for (int y = 0; y < s; y++)
            {
                bool solid = false;
                for (int x = 0; x < s && !solid; x++)
                {
                    solid = Alpha(glyph, s, x, y) == 255;
                }

                if (solid)
                {
                    first = first < 0 ? y : first;
                    last = y;
                }
            }

            Assert.That(first, Is.GreaterThan(0));
            Assert.That(Math.Abs(first - (s - 1 - last)), Is.LessThanOrEqualTo(2), "the shape about the picture's middle");
            Box box = UiRaster.RaisedGlyphBox(new Box(0f, 0f, 40f, 40f));
            Assert.That(box.Width, Is.EqualTo(40f * UiRaster.GlyphPictureScale).Within(1e-3));
            Assert.That(box.CenterX, Is.EqualTo(20f).Within(1e-3));
            Assert.That(box.CenterY, Is.EqualTo(20f).Within(1e-3));
        }

        [Test]
        public void TheBackChevron_IsTheNextOneMirrored_ItsLightStillFromTheUpperLeft()
        {
            const int s = 96;
            byte[] next = UiRaster.RaisedChevron(s, true, DesignTokens.Colors.InkBrown);
            byte[] back = UiRaster.RaisedChevron(s, false, DesignTokens.Colors.InkBrown);
            bool lightMirrored = true;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Assert.That(Math.Abs(Alpha(back, s, x, y) - Alpha(next, s, s - 1 - x, y)), Is.LessThanOrEqualTo(1), "the same shape, mirrored");
                    if (Alpha(next, s, x, y) == 255 && Math.Abs(Luma(back, s, s - 1 - x, y) - Luma(next, s, x, y)) > 4f)
                    {
                        lightMirrored = false;
                    }
                }
            }

            Assert.That(lightMirrored, Is.False, "its light is not mirrored");
        }

        private static int Alpha(byte[] rgba, int width, int x, int y) => rgba[(((y * width) + x) * 4) + 3];

        private static (byte R, byte G, byte B) Rgb(byte[] rgba, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return (rgba[i], rgba[i + 1], rgba[i + 2]);
        }

        private static float Luma(byte[] rgba, int width, int x, int y)
        {
            (byte r, byte g, byte b) = Rgb(rgba, width, x, y);
            return (0.299f * r) + (0.587f * g) + (0.114f * b);
        }
    }
}
