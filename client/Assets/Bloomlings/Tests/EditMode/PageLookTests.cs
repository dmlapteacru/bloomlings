using System;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// Every row, rimmed button, page and frame in the new look (spec 005 FR-047, contracts/look.md §6.20; the owner's audit
    /// request and references of 2026-10-08): the own row's green slab, the light wood tabs, the tiles' thin rim, the Wooden
    /// Frame's laminate, the page flowers and the wooden frames.
    /// </summary>
    public class PageLookTests
    {
        [Test]
        public void TheOwnRow_IsTheRaisedSlabInGreen()
        {
            const int w = 600;
            const int h = 120;
            byte[] own = UiRaster.RaisedRow(w, h, h * 0.25f, GardenLook.RowYou);
            Assert.That(own, Is.EqualTo(UiRaster.RaisedRow(w, h, h * 0.25f, GardenLook.RowYou)), "deterministic");
            Assert.That(Alpha(own, w, 0, 0), Is.Zero, "rounded");
            (byte r, byte g, byte b) = Rgb(own, w, w / 2, h / 2);
            Assert.That(g > r && g > b, Is.True, "green-tinted");
            Assert.That(Luma(own, w, w / 2, h / 3), Is.GreaterThan(Luma(own, w, w / 2, h - 2) + 10f), "lighter on top than its front side");
            Assert.That(UiRaster.RaisedRow(w, h, h * 0.25f), Is.EqualTo(UiRaster.RaisedRow(w, h, h * 0.25f, GardenLook.RowCream)), "the row cream by default");
        }

        [Test]
        public void AnUnselectedTab_IsLightWood_DeeperThanACreamFace()
        {
            const int w = 300;
            const int h = 90;
            byte[] tan = UiRaster.ButtonFace(w, h, GardenLook.Tan, 0.5f, false);
            byte[] cream = UiRaster.ButtonFace(w, h, GardenLook.Cream, 0.5f, false);
            (byte r, byte g, byte b) = Rgb(tan, w, w / 2, h / 2);
            Assert.That(r > g && g > b, Is.True, "warm wood");
            Assert.That(Luma(tan, w, w / 2, h / 2), Is.LessThan(Luma(cream, w, w / 2, h / 2)), "deeper than the cream face");
            Assert.That((r - b) / (float)r, Is.InRange(0.2f, 0.5f), "light wood, not caramel");
        }

        [Test]
        public void AFramedTile_HasAThinRim_ItsFaceInsideIt()
        {
            var tile = new Box(0f, 0f, 260f, 200f);
            Box face = UiRaster.RaisedFaceBox(tile, GardenLook.TileRimShare);
            Box button = UiRaster.RaisedFaceBox(tile);
            Assert.That(face.Left - tile.Left, Is.LessThan(button.Left - tile.Left), "thinner than a button's rim");
            Assert.That(face.Left - tile.Left, Is.GreaterThan(200f * GardenLook.TileRimShare), "inside the rim");
            Box top = CardLook.TileTop(tile);
            Assert.That(top.Top, Is.EqualTo(face.Top).Within(1e-3));
            Assert.That(top.Bottom, Is.LessThan(face.Bottom), "above the face's front side");

            const int w = 260;
            const int h = 200;
            byte[] plate = UiRaster.ButtonPlate(w, h, 0.22f, GardenLook.TileRimShare);
            Assert.That(plate, Is.EqualTo(UiRaster.ButtonPlate(w, h, 0.22f, GardenLook.TileRimShare)), "deterministic");
            Assert.That(Alpha(plate, w, 0, 0), Is.Zero, "rounded");
            (byte r, byte g, byte b) = Rgb(plate, w, (int)(h * GardenLook.TileRimShare * 0.5f), h / 2);
            Assert.That(r > g && g > b, Is.True, "the rim is the plate's wood");
        }

        [Test]
        public void TheWoodenFrame_IsThePlatesLaminate()
        {
            const int s = 200;
            byte[] frame = UiRaster.ProfileFrame(s, ProfileFrameStyle.WoodRing);
            Assert.That(frame, Is.EqualTo(UiRaster.ProfileFrame(s, ProfileFrameStyle.WoodRing)), "deterministic");
            Assert.That(Alpha(frame, s, s / 2, s / 2), Is.Zero, "the avatar shows through its middle");

            // The band's middle on the left side: warm, saturated caramel as the buttons' plates.
            int x = (int)(s * (0.5f - AvatarLook.FrameEdge));
            (byte r, byte g, byte b) = Rgb(frame, s, x, s / 2);
            Assert.That(Alpha(frame, s, x, s / 2), Is.EqualTo(255));
            Assert.That(r > g && g > b, Is.True);
            Assert.That((r - b) / (float)r, Is.InRange(0.35f, 0.85f), "caramel, as the buttons' plates");
        }

        [Test]
        public void ThePageFlowers_SitOverTheFourCorners_FlippedToEach()
        {
            var panel = new Box(40f, 300f, 1040f, 2200f);
            var flowers = CardLook.PageFlowers(panel, 2000f, 1f);
            Assert.That(flowers.Count, Is.EqualTo(4));
            foreach ((Corner corner, Box box) in flowers)
            {
                bool right = corner == Corner.TopRight || corner == Corner.BottomRight;
                bool bottom = corner == Corner.BottomLeft || corner == Corner.BottomRight;
                Assert.That(box.Contains(right ? panel.Right : panel.Left, box.CenterY), Is.True, corner + " over its side");
                Assert.That(bottom ? box.Contains(box.CenterX, 2000f - (box.Height * 0.2f)) && box.Bottom > 2000f - box.Height : box.Contains(box.CenterX, panel.Top), Is.True, corner + " at its edge");
                Assert.That(CardLook.Mirrored(corner), Is.EqualTo(right));
                Assert.That(CardLook.Turned(corner), Is.EqualTo(bottom));
                Assert.That(box.Width, Is.EqualTo(bottom ? CardLook.PageFootFlowerUnits : CardLook.PageFlowerUnits).Within(1e-3));
                if (!bottom)
                {
                    Assert.That(Math.Abs(box.CenterX - (right ? panel.Right : panel.Left)), Is.LessThan(box.Width * 0.15f), corner + " near its corner, clear of the content");
                }
            }
        }

        [Test]
        public void TheTitleSign_FitsItsTextAndEnds_CenteredInTheTitle()
        {
            var title = new Box(100f, 1000f, 980f, 1090f);
            Box sign = CardLook.TitleSign(title, 300f);
            Assert.That(sign.CenterX, Is.EqualTo(title.CenterX).Within(1e-3));
            Assert.That(sign.CenterY, Is.EqualTo(title.CenterY).Within(1e-3));
            Assert.That(sign.Width, Is.EqualTo(300f + (2f * sign.Height)).Within(1e-3), "the text and its ends");
            Assert.That(CardLook.TitleSign(title, 5000f).Width, Is.EqualTo(title.Width * 0.8f).Within(1e-3), "at most 80% of the title");
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
