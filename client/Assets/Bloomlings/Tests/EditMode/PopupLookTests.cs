using System;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The popups and buttons after the owner's mockup of 2026-10-08 (spec 005 FR-045, contracts/look.md §6.19): the card's
    /// wooden frame round its cream panel, the laminate signs, every button raised on its plate, the raised rows.
    /// </summary>
    public class PopupLookTests
    {
        [Test]
        public void TheCardFrame_IsWoodRoundACreamPanel_ShadedUnderItsTopEdge()
        {
            const int w = 460;
            const int h = 520;
            byte[] card = UiRaster.CardFrame(w, h, w * 0.08f);
            Assert.That(card, Is.EqualTo(UiRaster.CardFrame(w, h, w * 0.08f)), "deterministic");
            Assert.That(Alpha(card, w, 0, 0), Is.Zero, "the rounded corner");

            float border = UiRaster.CardFrameBorder(w);
            Assert.That(border, Is.EqualTo(w * UiRaster.CardFrameUnits / 920f).Within(1e-3));
            (byte r, byte g, byte b) = Rgb(card, w, w / 2, (int)(border * 0.5f));
            Assert.That(r > g && g > b, Is.True, "the frame's top member is warm wood");
            Assert.That((r - b) / (float)r, Is.InRange(0.3f, 0.8f), "caramel, as the buttons' plates");

            // The panel is cream, lighter than the wood, and a little deeper just under the frame's top edge.
            Box panel = CardLook.Panel(new Box(0f, 0f, w, h));
            float middle = Luma(card, w, w / 2, h / 2);
            Assert.That(middle, Is.GreaterThan(Luma(card, w, w / 2, (int)(border * 0.5f)) + 25f));
            Assert.That(Alpha(card, w, w / 2, h / 2), Is.EqualTo(255), "an opaque panel");
            Assert.That(Luma(card, w, w / 2, (int)panel.Top + 2), Is.LessThan(Luma(card, w, w / 2, (int)panel.Top + (int)(border * 4f))), "the frame's soft shadow");
        }

        [Test]
        public void TheLaminateSign_IsLitOnTopOverItsFrontSide_WithTwoNails()
        {
            const int w = 360;
            const int h = 110;
            byte[] sign = UiRaster.LaminateSign(w, h, h * 0.28f);
            Assert.That(sign, Is.EqualTo(UiRaster.LaminateSign(w, h, h * 0.28f)), "deterministic");
            Assert.That(Alpha(sign, w, 0, 0), Is.Zero);
            Assert.That(Alpha(sign, w, w / 2, h / 2), Is.EqualTo(255));
            Assert.That(Luma(sign, w, w / 2, (int)(h * 0.25f)), Is.GreaterThan(Luma(sign, w, w / 2, h - 3) + 15f), "its top over its front side");

            // A nail near the top at the left end: darker than the wood beside it.
            float r = h * 0.28f;
            int nx = (int)Math.Max(r * 0.7f, h * 0.3f);
            int ny = (int)(h * (1f - UiRaster.SignSide) * 0.3f);
            Assert.That(Luma(sign, w, nx, ny), Is.LessThan(Luma(sign, w, nx + (int)(h * 0.12f), ny) - 10f), "the nail");
        }

        [Test]
        public void APillPlate_HasRoundEnds_AndAGlossyFaceIsLighterOnTop()
        {
            const int w = 400;
            const int h = 120;
            byte[] plate = UiRaster.ButtonPlate(w, h, 0.5f);
            Assert.That(Alpha(plate, w, 2, 4), Is.Zero, "a round end");
            Assert.That(Alpha(plate, w, 2, h / 2), Is.GreaterThan(0), "its middle reaches the end");

            Box faceBox = UiRaster.RaisedFaceBox(new Box(0f, 0f, w, h));
            int fw = (int)faceBox.Width;
            int fh = (int)faceBox.Height;
            byte[] matte = UiRaster.ButtonFace(fw, fh, GardenLook.Green, 0.5f, false);
            byte[] gloss = UiRaster.ButtonFace(fw, fh, GardenLook.Green, 0.5f, true);
            Assert.That(Luma(gloss, fw, fw / 2, (int)(fh * 0.2f)), Is.GreaterThan(Luma(matte, fw, fw / 2, (int)(fh * 0.2f)) + 5f), "the gloss band");
            Assert.That(Luma(gloss, fw, fw / 2, (int)(fh * 0.75f)), Is.EqualTo(Luma(matte, fw, fw / 2, (int)(fh * 0.75f))).Within(2f), "below it the same face");
            (byte r, byte g, byte b) = Rgb(gloss, fw, fw / 2, fh / 2);
            Assert.That(g > r && g > b, Is.True, "a green face keeps its green");
        }

        [Test]
        public void ARaisedRow_IsACreamSlab_LighterOnTopThanItsFrontSide()
        {
            const int w = 300;
            const int h = 90;
            byte[] row = UiRaster.RaisedFace(w, h, h * 0.25f, h * 0.06f, GardenLook.Cream);
            Assert.That(Alpha(row, w, 0, 0), Is.Zero);
            Assert.That(Luma(row, w, w / 3, h / 3), Is.GreaterThan(Luma(row, w, w / 2, h - 2) + 10f));
            (byte r, byte g, byte b) = Rgb(row, w, w / 2, h / 2);
            Assert.That(r >= g && g > b, Is.True, "cream");
        }

        [Test]
        public void ARow_IsTheLightEvenCream_OfTheMockup()
        {
            const int w = 600;
            const int h = 126;
            byte[] row = UiRaster.RaisedRow(w, h, h * 0.25f);
            byte[] face = UiRaster.RaisedFace(w, h, h * 0.25f, h * 0.06f, GardenLook.Cream);
            (byte r, byte g, byte b) = Rgb(row, w, w / 2, (int)(h * 0.6f));
            Assert.That(b, Is.GreaterThan(Rgb(face, w, w / 2, (int)(h * 0.6f)).B + 15), "lighter cream than a button's face over a long row");
            Assert.That(r, Is.GreaterThan(240));
            Assert.That(b, Is.InRange(195, 225), "the mockup's (254, 236, 207)");
            Assert.That(Math.Abs(Luma(row, w, w / 2, (int)(h * 0.3f)) - Luma(row, w, w / 2, (int)(h * 0.75f))), Is.LessThan(12f), "even along its height");
        }

        [Test]
        public void ACardsBody_KeepsClearOfItsFrame()
        {
            foreach ((float w, float h) in new[] { (1080f, 1920f), (1080f, 2340f), (1440f, 3200f) })
            {
                CardRegions r = ScreenLayout.Card(w, h, default, 600f);
                float frame = UiRaster.CardFrameBorder(r.Card.Width);
                Box panel = CardLook.Panel(r.Card);
                Assert.That(r.Body.Left - panel.Left, Is.GreaterThanOrEqualTo(DesignTokens.Space.M * DesignTokens.ScaleFor(w, h) - 0.01f), "a gap at the left");
                Assert.That(panel.Right - r.Body.Right, Is.GreaterThanOrEqualTo(DesignTokens.Space.M * DesignTokens.ScaleFor(w, h) - 0.01f), "and at the right");
                Assert.That(r.Body.Bottom, Is.LessThan(panel.Bottom), "above the frame's bottom member");
                Assert.That(frame, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void TheSign_StraddlesTheCardsTopEdge_WithTheLotusRisingBehindIt()
        {
            var card = new Box(80f, 400f, 1000f, 1300f);
            Box title = new Box(card.Left + 170f, card.Top + 20f, card.Right - 170f, card.Top + 130f);
            Box sign = CardLook.Sign(card, title, 260f, 1f);
            Assert.That(sign.Top, Is.LessThan(card.Top), "over the frame's top edge");
            Assert.That(sign.Bottom, Is.GreaterThan(card.Top));
            Assert.That(sign.CenterX, Is.EqualTo(card.CenterX).Within(1e-3));
            Assert.That(sign.Width, Is.LessThanOrEqualTo(card.Width * 0.78f + 1e-3));
            Box lotus = CardLook.Lotus(sign);
            Assert.That(lotus.Top, Is.LessThan(sign.Top - sign.Height), "its petals rise above the sign");
            Assert.That(lotus.CenterX, Is.EqualTo(sign.CenterX).Within(1e-3));
            (Box topLeft, Box bottomRight) = CardLook.Decoration(card, 1f);
            Assert.That(topLeft.Contains(card.Left, card.Top), Is.True, "over the top-left corner");
            Assert.That(bottomRight.Contains(card.Right, card.Bottom), Is.True, "over the bottom-right corner");
        }

        [Test]
        public void TheSettingsRows_TakeTheMockupsIcons()
        {
            Assert.That(CardLook.SettingsIconOf("settings.sound").Picture, Is.EqualTo(OwnerPictures.VariantIcon("flower")));
            Assert.That(CardLook.SettingsIconOf("settings.haptics").Picture, Is.EqualTo(OwnerPictures.VariantIcon("leaf")));
            Assert.That(CardLook.SettingsIconOf("settings.music").Picture, Is.EqualTo(OwnerPictures.VariantIcon("bud")));
            SettingsIcon speed = CardLook.SettingsIconOf("settings.speed");
            Assert.That(speed.Picture, Is.Null);
            Assert.That(speed.Glyph, Is.EqualTo("ui.fast"));
            Assert.That(ShapeLibrary.Has(speed.Glyph!), Is.True);
            foreach (string key in new[] { "settings.sound", "settings.haptics", "settings.music" })
            {
                Assert.That(ShapeLibrary.Has(CardLook.SettingsIconOf(key).StandIn), Is.True, key + " has a drawn stand-in");
            }

            var row = new Box(100f, 500f, 900f, 626f);
            Box icon = CardLook.SettingsIconBox(row, 1f);
            Assert.That(icon.Left, Is.GreaterThan(row.Left));
            Assert.That(icon.Right, Is.LessThan(CardLook.SettingsLabelLeft(row, 1f)), "the label starts after the icon");
            Assert.That(icon.Top >= row.Top && icon.Bottom <= row.Bottom, Is.True);
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
