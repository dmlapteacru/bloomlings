using System;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The reference look's material pictures (spec 005 research D4, FR-006; contracts/look.md §2, §3.1).</summary>
    public class UiRasterTests
    {
        [Test]
        public void Pictures_HaveTheirSize_InStraightRgba()
        {
            Assert.That(UiRaster.Plank(120, 40, 11f, 2f, WoodTone.Light, 1).Length, Is.EqualTo(120 * 40 * 4));
            Assert.That(UiRaster.Frame(64, 72, 12f, 7f, WoodTone.Dark, 1).Length, Is.EqualTo(64 * 72 * 4));
            Assert.That(UiRaster.Stone(48, 20, 6f, 1.5f, 1).Length, Is.EqualTo(48 * 20 * 4));
            Assert.That(UiRaster.Tile(56, Color("leaf"), "leaf", TileStyle.Board).Length, Is.EqualTo(56 * 56 * 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => UiRaster.Plank(0, 10, 2f, 1f, WoodTone.Light, 1));
        }

        [Test]
        public void TheSameArguments_GiveTheSameBytes()
        {
            Assert.That(UiRaster.Plank(160, 48, 13f, 2f, WoodTone.Light, 7), Is.EqualTo(UiRaster.Plank(160, 48, 13f, 2f, WoodTone.Light, 7)));
            Assert.That(UiRaster.Frame(80, 88, 14f, 9f, WoodTone.Dark, 3), Is.EqualTo(UiRaster.Frame(80, 88, 14f, 9f, WoodTone.Dark, 3)));
            Assert.That(UiRaster.Stone(64, 28, 8f, 2f, 5), Is.EqualTo(UiRaster.Stone(64, 28, 8f, 2f, 5)));
            Assert.That(UiRaster.Tile(72, Color("acorn"), "acorn", TileStyle.Sticker), Is.EqualTo(UiRaster.Tile(72, Color("acorn"), "acorn", TileStyle.Sticker)));
        }

        [Test]
        public void Corners_AreTransparent_AndCentersOpaque()
        {
            AssertCornersAndCenter(UiRaster.Plank(200, 56, 16f, 2f, WoodTone.Light, 1), 200, 56);
            AssertCornersAndCenter(UiRaster.Plank(96, 32, 9f, 1.5f, WoodTone.Dark, 2), 96, 32);
            AssertCornersAndCenter(UiRaster.Stone(80, 32, 10f, 2f, 4), 80, 32);
            AssertCornersAndCenter(UiRaster.Tile(64, Color("flower"), "flower", TileStyle.Board), 64, 64);
            AssertCornersAndCenter(UiRaster.Tile(64, Color("flower"), "flower", TileStyle.Sticker), 64, 64);
        }

        [Test]
        public void TheFrame_IsARing_WithATransparentHole()
        {
            const int w = 100;
            const int h = 110;
            byte[] frame = UiRaster.Frame(w, h, 18f, 11f, WoodTone.Dark, 1);
            Assert.That(Alpha(frame, w, 0, 0), Is.EqualTo(0), "outer corner");
            Assert.That(Alpha(frame, w, w / 2, h / 2), Is.EqualTo(0), "the hole");
            Assert.That(Alpha(frame, w, w / 2, 5), Is.EqualTo(255), "top member");
            Assert.That(Alpha(frame, w, 5, h / 2), Is.EqualTo(255), "side member");
            Assert.That(Alpha(frame, w, w / 2, 14), Is.InRange(1, 254), "the shadow along the hole's top");
        }

        [Test]
        public void EdgePixels_KeepTheirColor_StraightAlpha()
        {
            // The anti-aliased pixels of the rounded corners lie on the outline and keep its color: premultiplied bytes
            // would be darker by their alpha.
            const int w = 120;
            const int h = 40;
            byte[] plank = UiRaster.Plank(w, h, 12f, 3f, WoodTone.Light, 1);
            Rgba line = DesignTokens.Colors.WoodLine;
            int partial = 0;
            for (int i = 0; i < plank.Length; i += 4)
            {
                if (plank[i + 3] > 0 && plank[i + 3] < 255)
                {
                    partial++;
                    Assert.That(Math.Abs(plank[i] - line.R) + Math.Abs(plank[i + 1] - line.G) + Math.Abs(plank[i + 2] - line.B), Is.LessThan(8), "pixel " + (i / 4));
                }
            }

            Assert.That(partial, Is.GreaterThan(8));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentGrainAndStones()
        {
            Assert.That(UiRaster.Plank(160, 48, 13f, 2f, WoodTone.Light, 1), Is.Not.EqualTo(UiRaster.Plank(160, 48, 13f, 2f, WoodTone.Light, 2)));
            Assert.That(UiRaster.Stone(64, 28, 8f, 2f, 1), Is.Not.EqualTo(UiRaster.Stone(64, 28, 8f, 2f, 2)));
            Assert.That(UiRaster.Plank(160, 48, 13f, 2f, WoodTone.Light, 1), Is.Not.EqualTo(UiRaster.Plank(160, 48, 13f, 2f, WoodTone.Dark, 1)));
        }

        [Test]
        public void EveryVariant_HasItsTile_InBothStyles()
        {
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                Rgba color = Rgba.FromHex(variant.ColorHex);
                foreach (int size in new[] { 24, 64, 96 })
                {
                    byte[] board = UiRaster.Tile(size, color, variant.IconId, TileStyle.Board);
                    byte[] sticker = UiRaster.Tile(size, color, variant.IconId, TileStyle.Sticker);
                    AssertCornersAndCenter(board, size, size);
                    AssertCornersAndCenter(sticker, size, size);
                    Assert.That(board, Is.Not.EqualTo(sticker), variant.Id.Key + " " + size);
                }

                // The symbol shows: the same color with another symbol gives another picture.
                string other = variant.IconId == "mist" ? "bark" : "mist";
                Assert.That(UiRaster.Tile(64, color, variant.IconId, TileStyle.Board), Is.Not.EqualTo(UiRaster.Tile(64, color, other, TileStyle.Board)), variant.Id.Key);
            }
        }

        [Test]
        public void TheLaunchTiles_DifferFromEachOther()
        {
            VariantInfo[] launch = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Launch).ToArray();
            for (int a = 0; a < launch.Length; a++)
            {
                for (int b = a + 1; b < launch.Length; b++)
                {
                    // Same color, different symbol: the pictures still differ.
                    Rgba color = Rgba.FromHex(launch[a].ColorHex);
                    Assert.That(UiRaster.Tile(48, color, launch[a].IconId, TileStyle.Board), Is.Not.EqualTo(UiRaster.Tile(48, color, launch[b].IconId, TileStyle.Board)), launch[a].IconId + " vs " + launch[b].IconId);
                }
            }
        }

        [Test]
        public void TileStates_ChangeThePicture_AndGreyIsGrey()
        {
            Rgba color = Color("drop");
            byte[] normal = UiRaster.Tile(64, color, "drop", TileStyle.Sticker);
            byte[] dimmed = UiRaster.Tile(64, color, "drop", TileStyle.Sticker, TileState.Dimmed);
            byte[] grey = UiRaster.Tile(64, color, "drop", TileStyle.Sticker, TileState.Grey);
            byte[] mystery = UiRaster.Tile(64, color, "drop", TileStyle.Sticker, TileState.Mystery);
            Assert.That(dimmed, Is.Not.EqualTo(normal));
            Assert.That(grey, Is.Not.EqualTo(normal));
            Assert.That(mystery, Is.Not.EqualTo(normal));
            for (int i = 0; i < grey.Length; i += 4)
            {
                if (grey[i + 3] > 0)
                {
                    Assert.That(Math.Abs(grey[i] - grey[i + 1]) <= 2 && Math.Abs(grey[i + 1] - grey[i + 2]) <= 2, Is.True, "pixel " + (i / 4));
                }
            }

            // The mystery tile ignores the variant: the same picture for any color and icon.
            Assert.That(UiRaster.Tile(64, Color("leaf"), "leaf", TileStyle.Board, TileState.Mystery), Is.EqualTo(UiRaster.Tile(64, color, "drop", TileStyle.Board, TileState.Mystery)));
        }

        [Test]
        public void Vivid_PushesAColorAwayFromItsGrey_AndKeepsGreysAndAlpha()
        {
            Rgba grey = Rgba.FromHex("#808080");
            Assert.That(UiRaster.Vivid(grey, 1.6f), Is.EqualTo(grey));
            Assert.That(UiRaster.Vivid(Rgba.FromHex("#55C7FB"), 1f), Is.EqualTo(Rgba.FromHex("#55C7FB")));
            Rgba sky = UiRaster.Vivid(Rgba.FromHex("#55C7FB"), 1.6f);
            Assert.That(sky.B, Is.EqualTo(255), "blue clamps at full");
            Assert.That(sky.R, Is.LessThan(0x55), "red falls away from the grey");
            Assert.That(UiRaster.Vivid(new Rgba(200, 40, 40, 128), 1.4f).A, Is.EqualTo(128));
        }

        [Test]
        public void ThePedestal_IsAStoneDrum_LighterOnTopThanOnItsSide()
        {
            const int w = 240;
            const int h = 130;
            byte[] pedestal = UiRaster.Pedestal(w, h, 5);
            Assert.That(pedestal.Length, Is.EqualTo(w * h * 4));
            Assert.That(pedestal, Is.EqualTo(UiRaster.Pedestal(w, h, 5)), "deterministic");
            AssertCornersAndCenter(pedestal, w, h);

            // The paved top (its center, about 34 px down) is lighter than the side's middle course below it.
            Assert.That(Luminance(pedestal, w, w / 2, 34), Is.GreaterThan(Luminance(pedestal, w, w / 2, 100)));
        }

        [Test]
        public void Sizes_QuantizeToEightPixels_AndKeysCarryTheSize()
        {
            Assert.That(UiRaster.Quantize(1f), Is.EqualTo(8));
            Assert.That(UiRaster.Quantize(81f), Is.EqualTo(88));
            Assert.That(UiRaster.Quantize(88f), Is.EqualTo(88));
            Assert.That(UiRaster.CacheKey("mat.wood.light", 600, 144), Is.EqualTo("mat.wood.light@600x144"));
        }

        [Test]
        public void GrassCells_AreOpaqueLawn_DeterministicAndVaried()
        {
            // Spec 005 FR-020: the picture's background reads as garden, never as a lime Leaf tile.
            const int size = 64;
            byte[] grass = UiRaster.Grass(size, 1);
            Assert.That(grass.Length, Is.EqualTo(size * size * 4));
            Assert.That(grass, Is.EqualTo(UiRaster.Grass(size, 1)));
            Assert.That(grass, Is.Not.EqualTo(UiRaster.Grass(size, 2)));
            Assert.That(UiRaster.Grass(48, 40, 3).Length, Is.EqualTo(48 * 40 * 4));
            for (int i = 3; i < grass.Length; i += 4)
            {
                Assert.That(grass[i], Is.EqualTo(255), "a grass cell is opaque");
            }

            int c = (((size / 2) * size) + (size / 2)) * 4;
            var middle = new Rgba(grass[c], grass[c + 1], grass[c + 2]);
            Assert.That(middle.G, Is.GreaterThan(middle.R), "green");
            Assert.That(middle.G, Is.GreaterThan(middle.B), "green");
            Assert.That(Rgba.Contrast(middle, Color("leaf")), Is.GreaterThan(1.4), "darker and calmer than the Leaf tile");
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    Assert.That(UiRaster.GrassSeed(x, y), Is.InRange(0, UiRaster.GrassVariants - 1));
                }
            }

            Assert.That(UiRaster.GrassSeed(0, 0), Is.Not.EqualTo(UiRaster.GrassSeed(1, 0)), "neighbors differ");
        }

        [Test]
        public void TheWinGarden_IsTheLawnLightened_WithAGlowInTheMiddle()
        {
            BackdropColors colors = DesignTokens.Backdrop("#F5F2E6", "#DDEBCF");
            Assert.That(BackdropRaster.IsLawn(BackdropScene.Win), Is.True);
            Assert.That(BackdropRaster.IsLawn(BackdropScene.Home), Is.False);
            Assert.That(BackdropRaster.Downscale(BackdropScene.Win), Is.GreaterThan(BackdropRaster.Downscale(BackdropScene.Gameplay)), "rendered smaller: the garden blurs");
            const float aspect = 2340f / 1080f;
            Rgba lawn = BackdropRaster.Sample(0.5f, aspect * 0.5f, aspect, colors, BackdropScene.Gameplay);
            Rgba win = BackdropRaster.Sample(0.5f, aspect * 0.5f, aspect, colors, BackdropScene.Win);
            Rgba edge = BackdropRaster.Sample(0.5f, aspect * 0.06f, aspect, colors, BackdropScene.Win);
            Assert.That(win.Luminance, Is.GreaterThan(lawn.Luminance + 0.1), "lighter than the lawn");
            Assert.That(win.Luminance, Is.GreaterThan(edge.Luminance), "the glow is in the middle");
            Assert.That(BackdropRaster.Render(40, 86, colors, BackdropScene.Win), Is.EqualTo(BackdropRaster.Render(40, 86, colors, BackdropScene.Win)));
        }

        private static Rgba Color(string iconId) => Rgba.FromHex(VariantCatalog.Default.All.First(v => v.IconId == iconId).ColorHex);

        private static int Alpha(byte[] pixels, int width, int x, int y) => pixels[(((y * width) + x) * 4) + 3];

        private static double Luminance(byte[] pixels, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return new Rgba(pixels[i], pixels[i + 1], pixels[i + 2]).Luminance;
        }

        private static void AssertCornersAndCenter(byte[] pixels, int width, int height)
        {
            Assert.That(Alpha(pixels, width, 0, 0), Is.EqualTo(0), "top-left corner");
            Assert.That(Alpha(pixels, width, width - 1, 0), Is.EqualTo(0), "top-right corner");
            Assert.That(Alpha(pixels, width, 0, height - 1), Is.EqualTo(0), "bottom-left corner");
            Assert.That(Alpha(pixels, width, width - 1, height - 1), Is.EqualTo(0), "bottom-right corner");
            Assert.That(Alpha(pixels, width, width / 2, height / 2), Is.EqualTo(255), "center");
        }
    }
}
