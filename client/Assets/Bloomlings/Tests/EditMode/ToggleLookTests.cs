using System;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The volumetric switches and the pressed-in and glossy pieces beside them (spec 005 FR-048, contracts/look.md §6.21;
    /// the owner's Settings mockup of 2026-10-08): the tracks pressed into the row, their marks, the round cream knob, the
    /// Leaderboard's grooves and the badges' balls.
    /// </summary>
    public class ToggleLookTests
    {
        private const int W = 320;
        private const int H = 164;

        [Test]
        public void TheOnTrack_IsGreen_PressedIn_WithAFaintCheckInItsLeftEnd()
        {
            byte[] on = UiRaster.ToggleTrack(W, H, true);
            Assert.That(on, Is.EqualTo(UiRaster.ToggleTrack(W, H, true)), "deterministic");
            Assert.That(Alpha(on, W, 0, 0), Is.Zero, "a round end");
            Assert.That(Alpha(on, W, W / 2, H / 2), Is.EqualTo(255));
            (byte r, byte g, byte b) = Rgb(on, W, W / 2, H / 2);
            Assert.That(g > r && g > b, Is.True, "green");
            AssertPressedIn(on);

            // The mark's disc in the left end, where the knob is not, is lighter than the plain track beside it.
            int cx = (int)(H * 0.52f);
            int side = (int)(H * UiRaster.ToggleMarkShare);
            Assert.That(MeanLuma(on, W, cx, H / 2, side / 3), Is.GreaterThan(MeanLuma(on, W, W / 2, H / 2, side / 3) + 8f), "the faint check");
        }

        [Test]
        public void TheOffTrack_IsDeeperWood_PressedIn_WithAFaintLeafInItsRightEnd()
        {
            byte[] off = UiRaster.ToggleTrack(W, H, false);
            Assert.That(off, Is.EqualTo(UiRaster.ToggleTrack(W, H, false)), "deterministic");
            (byte r, byte g, byte b) = Rgb(off, W, W / 2, H / 2);
            Assert.That(r > g && g > b, Is.True, "warm wood");
            byte[] tab = UiRaster.ButtonFace(W, H, GardenLook.Tan, 0.5f, false);
            Assert.That(Luma(off, W, W / 2, H / 2), Is.LessThan(Luma(tab, W, W / 2, H / 2) - 15f), "deeper than the light wood tabs");
            AssertPressedIn(off);

            int cx = W - (int)(H * 0.52f);
            int side = (int)(H * UiRaster.ToggleMarkShare);
            Assert.That(MeanLuma(off, W, cx, H / 2, side / 4), Is.LessThan(MeanLuma(off, W, W / 2, H / 2, side / 4) - 4f), "the leaf pressed in");
            Assert.That(off, Is.Not.EqualTo(UiRaster.ToggleTrack(W, H, true)));
        }

        [Test]
        public void TheKnob_IsARoundCreamButton_FlatOnTop_LitFromTheUpperLeft()
        {
            const int s = 160;
            byte[] knob = UiRaster.ToggleKnob(s);
            Assert.That(knob, Is.EqualTo(UiRaster.ToggleKnob(s)), "deterministic");
            Assert.That(Alpha(knob, s, 2, 2), Is.Zero, "round");
            Assert.That(Alpha(knob, s, s / 2, s / 2), Is.EqualTo(255));
            (byte r, byte g, byte b) = Rgb(knob, s, s / 2, s / 2);
            Assert.That(r >= g && g > b && b > 180, Is.True, "cream");
            Assert.That(Math.Abs(Luma(knob, s, s / 2, s / 2) - Luma(knob, s, (int)(s * 0.4f), (int)(s * 0.4f))), Is.LessThan(4f), "a flat top");

            // Its rounded edge: lighter toward the light at the upper left than at the lower right.
            int near = (int)(s * 0.2f);
            int far = s - near;
            Assert.That(Luma(knob, s, near, near), Is.GreaterThan(Luma(knob, s, far, far) + 12f));
            Assert.That(UiRaster.KnobShare, Is.InRange(0.9f, 1f), "it fills the track's round end");
        }

        [Test]
        public void AGroove_IsTheLightWoodPressedIn_PalerThanTheOffTrack()
        {
            byte[] groove = UiRaster.Groove(W, H);
            Assert.That(groove, Is.EqualTo(UiRaster.Groove(W, H)), "deterministic");
            AssertPressedIn(groove);
            Assert.That(Luma(groove, W, W / 2, H / 2), Is.GreaterThan(Luma(UiRaster.ToggleTrack(W, H, false), W, W / 2, H / 2) + 15f));
        }

        [Test]
        public void ABadgesBall_IsGlossy_LitFromTheUpperLeft()
        {
            const int s = 120;
            ColorSet green = GardenLook.Green;
            byte[] ball = UiRaster.Ball(s, green.Face, green.Top, green.Lip, green.Line);
            Assert.That(ball, Is.EqualTo(UiRaster.Ball(s, green.Face, green.Top, green.Lip, green.Line)), "deterministic");
            Assert.That(Alpha(ball, s, 1, 1), Is.Zero);
            (byte r, byte g, byte b) = Rgb(ball, s, s / 2, s / 2);
            Assert.That(g > r && g > b, Is.True);
            Assert.That(Luma(ball, s, (int)(s * 0.32f), (int)(s * 0.32f)), Is.GreaterThan(Luma(ball, s, (int)(s * 0.7f), (int)(s * 0.7f)) + 20f));
        }

        [Test]
        public void TheSettingsSwitch_FitsItsRow_ClearOfItsLabel()
        {
            var row = new Box(100f, 500f, 980f, 626f);
            Box toggle = CardLook.SettingsToggleBox(row, 1f);
            Assert.That(toggle.Top > row.Top && toggle.Bottom < row.Bottom, Is.True, "inside the row");
            Assert.That(toggle.Right, Is.LessThan(row.Right), "clear of its right end");
            Assert.That(toggle.Height / row.Height, Is.InRange(0.6f, 0.7f), "about two thirds of the row, as in the mockup");
            Assert.That(CardLook.SettingsLabelRight(row, 1f), Is.LessThan(toggle.Left), "the label ends before it");
            Assert.That(CardLook.SettingsLabelRight(row, 1f), Is.GreaterThan(CardLook.SettingsLabelLeft(row, 1f) + 400f), "room for the label");
        }

        /// <summary>Pressed in: just under its upper edge deeper than just over its lower one, where it catches the light.</summary>
        private static void AssertPressedIn(byte[] rgba)
        {
            int inset = (int)(H * 0.1f);
            Assert.That(Luma(rgba, W, W / 2, inset), Is.LessThan(Luma(rgba, W, W / 2, H - 1 - inset) - 6f), "pressed in");
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

        private static float MeanLuma(byte[] rgba, int width, int cx, int cy, int half)
        {
            float sum = 0f;
            int n = 0;
            for (int y = cy - half; y <= cy + half; y++)
            {
                for (int x = cx - half; x <= cx + half; x++)
                {
                    sum += Luma(rgba, width, x, y);
                    n++;
                }
            }

            return sum / n;
        }
    }
}
