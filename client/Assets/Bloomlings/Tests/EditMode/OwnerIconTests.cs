using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The owner's gameplay icons (spec 005 pictures.md G9–G32 and the currency lotus): their names and slots, the tile
    /// face they lie on, where they go on a tile and how the hosts grey or bake them.
    /// </summary>
    public class OwnerIconTests
    {
        [Test]
        public void TheVariantIcons_AreNamedAsThePictureListSays_AndHaveTheirSlots()
        {
            string[] all = VariantCatalog.Default.All.Select(v => v.IconId).ToArray();
            Assert.That(OwnerPictures.Variants, Is.EquivalentTo(all));
            foreach (string icon in OwnerPictures.Variants)
            {
                Assert.That(OwnerPictures.VariantIcon(icon), Is.EqualTo("variant-" + icon));
                Assert.That(OwnerPictures.FieldIcon(icon), Is.EqualTo("field-" + icon));
                Assert.That(OwnerPictures.SlotOf(OwnerPictures.VariantIcon(icon)), Is.EqualTo("tile.icon." + icon));
                Assert.That(OwnerPictures.SlotOf(OwnerPictures.FieldIcon(icon)), Is.EqualTo("tile.gem." + icon));
                Assert.That(AssetSlots.Has(OwnerPictures.SlotOf(OwnerPictures.VariantIcon(icon))), Is.True, icon);
                Assert.That(AssetSlots.Has(OwnerPictures.SlotOf(OwnerPictures.FieldIcon(icon))), Is.True, icon);
            }

            Assert.That(OwnerPictures.CurrencyLotus, Is.EqualTo("currency-lotus"));
            Assert.That(OwnerPictures.SlotOf(OwnerPictures.CurrencyLotus), Is.EqualTo("currency.petal"));
        }

        [Test]
        public void EveryIconPicture_IsInTheIconsFolder()
        {
            string folder = Path.Combine(Application.dataPath, "Bloomlings", "Art", "Icons", "Resources", OwnerPictures.IconFolder);
            foreach (string icon in OwnerPictures.Variants)
            {
                Assert.That(File.Exists(Path.Combine(folder, OwnerPictures.VariantIcon(icon) + ".png")), Is.True, icon);
                Assert.That(File.Exists(Path.Combine(folder, OwnerPictures.FieldIcon(icon) + ".png")), Is.True, icon);
            }

            Assert.That(File.Exists(Path.Combine(folder, OwnerPictures.CurrencyLotus + ".png")), Is.True);
        }

        [Test]
        public void ATile_ShowsTheFieldIconOnTheBoard_TheDetailedOneOnStickers_AndNoneWhenMystery()
        {
            Assert.That(OwnerPictures.TileIcon("leaf", TileStyle.Board, TileState.Normal), Is.EqualTo("field-leaf"));
            Assert.That(OwnerPictures.TileIcon("leaf", TileStyle.Flat, TileState.Normal), Is.EqualTo("field-leaf"));
            Assert.That(OwnerPictures.TileIcon("acorn", TileStyle.Sticker, TileState.Dimmed), Is.EqualTo("variant-acorn"));
            Assert.That(OwnerPictures.TileIcon("acorn", TileStyle.Sticker, TileState.Grey), Is.EqualTo("variant-acorn"));
            Assert.That(OwnerPictures.TileIcon("acorn", TileStyle.Sticker, TileState.Mystery), Is.Null);
            Assert.That(OwnerPictures.TileIcon("mystery", TileStyle.Board, TileState.Normal), Is.Null);
            Assert.That(OwnerPictures.TileIconAlpha(TileState.Normal), Is.EqualTo(1f));
            Assert.That(OwnerPictures.TileIconAlpha(TileState.Dimmed), Is.LessThan(1f));
            Assert.That(OwnerPictures.TileIconAlpha(TileState.Grey), Is.LessThan(1f));
        }

        [Test]
        public void TheIcon_LiesOnTheFacesMiddle_AtItsShareOfTheTile()
        {
            var square = new Box(10f, 20f, 110f, 120f);
            Box board = OwnerPictures.TileIconBox(square, TileStyle.Board);
            Box sticker = OwnerPictures.TileIconBox(square, TileStyle.Sticker);
            Box flat = OwnerPictures.TileIconBox(square, TileStyle.Flat);
            Assert.That(board.Width, Is.EqualTo(100f * OwnerPictures.FieldIconBox).Within(0.01f));
            Assert.That(sticker.Width, Is.EqualTo(100f * OwnerPictures.StickerIconBox).Within(0.01f));
            Assert.That(board.CenterX, Is.EqualTo(square.CenterX).Within(0.01f));

            // Above the lip: the face's middle is higher than the tile's; a flat tile has no lip.
            Assert.That(board.CenterY, Is.EqualTo(20f + (100f * (1f - UiRaster.TileLipShare(TileStyle.Board)) / 2f)).Within(0.01f));
            Assert.That(board.CenterY, Is.LessThan(square.CenterY));
            Assert.That(flat.CenterY, Is.EqualTo(square.CenterY).Within(0.01f));
        }

        [Test]
        public void TheFace_IsTheTileWithoutItsSymbol()
        {
            Rgba color = Color("flower");
            foreach (TileStyle style in new[] { TileStyle.Board, TileStyle.Sticker, TileStyle.Flat })
            {
                byte[] face = UiRaster.TileFace(64, color, style);
                byte[] tile = UiRaster.Tile(64, color, "flower", style);
                Assert.That(face.Length, Is.EqualTo(tile.Length), style.ToString());
                Assert.That(face, Is.Not.EqualTo(tile), style + ": the symbol is gone");

                // The corners (no symbol there) are the same; the middle is the plain face.
                Assert.That(Pixel(face, 64, 1, 32), Is.EqualTo(Pixel(tile, 64, 1, 32)), style + " edge");
                Assert.That(face[(((30 * 64) + 32) * 4) + 3], Is.EqualTo(255), style + " middle opaque");
            }

            Assert.That(UiRaster.TileFace(64, color, TileStyle.Sticker, TileState.Mystery), Is.EqualTo(UiRaster.Tile(64, color, "flower", TileStyle.Sticker, TileState.Mystery)));
            Assert.That(UiRaster.TileFace(64, color, TileStyle.Sticker, TileState.Dimmed), Is.Not.EqualTo(UiRaster.TileFace(64, color, TileStyle.Sticker)));
        }

        [Test]
        public void GreyPixels_TurnsColorsGrey_AndKeepsAlpha()
        {
            byte[] rgba = { 255, 0, 0, 255, 0, 255, 0, 128, 10, 20, 250, 0 };
            OwnerPictures.GreyPixels(rgba);
            for (int i = 0; i < rgba.Length; i += 4)
            {
                Assert.That(rgba[i], Is.EqualTo(rgba[i + 1]));
                Assert.That(rgba[i + 1], Is.EqualTo(rgba[i + 2]));
            }

            Assert.That(rgba[0], Is.EqualTo(Rgba.FromHex("#FF0000").Grey().R));
            Assert.That(rgba[7], Is.EqualTo(128));
            Assert.That(rgba[11], Is.EqualTo(0));
        }

        [Test]
        public void DrawOver_AveragesTheSourceIntoItsBox_AndLeavesTheRest()
        {
            // An 8 × 8 picture, its left half red and its right half transparent, drawn into the middle 4 × 4 of a white
            // 8 × 8 target: halved, each target pixel averages 2 × 2 source pixels.
            var source = new byte[8 * 8 * 4];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    int i = ((y * 8) + x) * 4;
                    source[i] = 255;
                    source[i + 3] = 255;
                }
            }

            byte[] target = Enumerable.Repeat((byte)255, 8 * 8 * 4).ToArray();
            UiRaster.DrawOver(target, 8, 8, source, 8, 8, new Box(2f, 2f, 6f, 6f));
            Assert.That(Pixel(target, 8, 0, 0), Is.EqualTo((255, 255, 255, 255)), "outside the box");
            Assert.That(Pixel(target, 8, 2, 3), Is.EqualTo((255, 0, 0, 255)), "the red half");
            Assert.That(Pixel(target, 8, 5, 3), Is.EqualTo((255, 255, 255, 255)), "the transparent half");

            // A box outside the target and an empty source change nothing.
            byte[] copy = (byte[])target.Clone();
            UiRaster.DrawOver(target, 8, 8, source, 8, 8, new Box(20f, 20f, 30f, 30f));
            UiRaster.DrawOver(target, 8, 8, Array.Empty<byte>(), 0, 0, new Box(0f, 0f, 8f, 8f));
            Assert.That(target, Is.EqualTo(copy));
        }

        private static Rgba Color(string iconId) => Rgba.FromHex(VariantCatalog.Default.All.First(v => v.IconId == iconId).ColorHex);

        private static (int R, int G, int B, int A) Pixel(byte[] pixels, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
        }
    }
}
