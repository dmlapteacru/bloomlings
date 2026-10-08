using System;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The kit's big pictures render their rows on several threads (<see cref="UiRaster.ParallelRows"/>, the volume look's
    /// lag): the bytes are the same as on one thread, for every picture that does, at sizes above
    /// <see cref="UiRaster.ParallelPixels"/> and at odd ones.
    /// </summary>
    public class RasterRowsTests
    {
        private static readonly (string Name, Func<byte[]> Render)[] Pictures =
        {
            ("a page's card frame", () => UiRaster.CardFrame(1040, 1304, 83f)),
            ("a card frame of odd size", () => UiRaster.CardFrame(961, 603, 77.3f)),
            ("a card frame with big corners", () => UiRaster.CardFrame(592, 736, 300f)),
            ("a pill's plate", () => UiRaster.ButtonPlate(912, 224, 0.5f)),
            ("a tile's plate", () => UiRaster.ButtonPlate(320, 376, 0.11f, GardenLook.TileRimShare)),
            ("a glossy green face", () => UiRaster.ButtonFace(864, 168, GardenLook.Green, 0.5f, true)),
            ("a cream face", () => UiRaster.ButtonFace(728, 120, GardenLook.Cream, 0.5f, false)),
            ("a raised row", () => UiRaster.RaisedRow(952, 260, 65f, GardenLook.RowCream)),
            ("the player's row", () => UiRaster.RaisedRow(952, 128, 32f, GardenLook.RowYou)),
            ("a sign", () => UiRaster.LaminateSign(432, 152, 42f)),
            ("a plank", () => UiRaster.Plank(720, 288, 57.6f, 7.2f, WoodTone.Light, 7)),
            ("a pod frame", () => UiRaster.Frame(400, 240, 48f, 36f, WoodTone.Dark, 4)),
            ("the pedestal", () => UiRaster.Pedestal(648, 168, 5)),
            ("the medallion", () => UiRaster.NavMedallion(216)),
            ("the Wooden Frame", () => UiRaster.ProfileFrame(256, ProfileFrameStyle.WoodRing)),
            ("the Golden Ribbon", () => UiRaster.ProfileFrame(200, ProfileFrameStyle.GoldenRibbon)),
        };

        [Test]
        public void BigPictures_AreTheSameBytes_OnOneThreadOrMany()
        {
            bool parallel = UiRaster.ParallelRows;
            try
            {
                foreach ((string name, Func<byte[]> render) in Pictures)
                {
                    UiRaster.ParallelRows = true;
                    byte[] many = render();
                    UiRaster.ParallelRows = false;
                    byte[] one = render();
                    Assert.That(many, Is.EqualTo(one), name);
                }
            }
            finally
            {
                UiRaster.ParallelRows = parallel;
            }
        }

        [Test]
        public void TheCardFramesOpenMiddle_IsItsPanelsCream_FullyCovered()
        {
            // The middle the frame's shadow no longer reaches is the panel's own cream, opaque, lighter at the top.
            const int w = 600;
            const int h = 900;
            byte[] card = UiRaster.CardFrame(w, h, 48f);
            int Index(int x, int y) => ((y * w) + x) * 4;
            Assert.That(card[Index(w / 2, h / 2) + 3], Is.EqualTo(255));
            Assert.That(card[Index(w / 2, 120)], Is.GreaterThanOrEqualTo(card[Index(w / 2, h - 120)]), "lighter at the top");
            Assert.That(card[Index(w / 3, h / 2)], Is.EqualTo(card[Index(w / 2, h / 2)]), "one color across a row");
            Assert.That(card[Index(w / 3, h / 2) + 1], Is.EqualTo(card[Index((w * 2) / 3, h / 2) + 1]));
        }
    }
}
