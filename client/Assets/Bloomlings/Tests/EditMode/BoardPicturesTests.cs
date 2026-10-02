using System;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The board's pictures in the reference look (spec 005 contracts/look.md §3.6, §4.1, §4.4, research D14): the restored
    /// ground under the tiles, the full-color finished picture and the stone obstacle, as engine-free RGBA bytes.
    /// </summary>
    public class BoardPicturesTests
    {
        [Test]
        public void TheGround_IsOpaque_AndEachCellShowsItsPaleColor()
        {
            (LevelDefinition level, BasePicture picture) = FirstLevel();
            const int cell = 32;
            int w = picture.Width;
            int h = picture.Height;
            byte[] ground = BoardPictures.Ground(level, picture, w, h, cell);
            Assert.That(ground.Length, Is.EqualTo(w * cell * h * cell * 4));
            for (int i = 3; i < ground.Length; i += 4)
            {
                Assert.That(ground[i], Is.EqualTo(255), "the ground covers the whole grid");
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Rgba color = BoardPictures.GroundColor(level, picture, x, y);

                    // Below the faint top shade, the face is the cell's pale color; the cell's corner is darkened 0.12.
                    Rgba face = Pixel(ground, w * cell, (x * cell) + (cell / 2), ((h - 1 - y) * cell) + (cell * 3 / 4));
                    Rgba corner = Pixel(ground, w * cell, x * cell, (h - 1 - y) * cell);
                    Assert.That(Distance(face, color), Is.LessThan(6), $"cell {x},{y}");
                    Assert.That(Distance(corner, color.Darken(0.12f)), Is.LessThan(6), $"cell {x},{y} corner");
                }
            }
        }

        [Test]
        public void TheGround_OfARoleCell_IsItsVariantLightened()
        {
            (LevelDefinition level, BasePicture picture) = FirstLevel();
            for (int y = 0; y < picture.Height; y++)
            {
                for (int x = 0; x < picture.Width; x++)
                {
                    (VariantId? variant, int value) = BoardPictures.PictureCell(level, picture, x, y);
                    Rgba expected = variant.HasValue ? BoardPictures.ColorOf(variant.Value).Lighten(0.55f) : value == BasePicture.Stone ? C.StoneFace.Lighten(0.35f) : C.TileGround;
                    Assert.That(BoardPictures.GroundColor(level, picture, x, y), Is.EqualTo(expected));
                }
            }
        }

        [Test]
        public void ThePictureCell_FollowsTheMirror()
        {
            (LevelDefinition level, BasePicture picture) = FirstLevel();
            Mirror other = level.Picture.Mirror == Mirror.Horizontal ? Mirror.None : Mirror.Horizontal;
            LevelDefinition mirrored = level with { Picture = level.Picture with { Mirror = other } };
            int w = picture.Width;
            for (int y = 0; y < picture.Height; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Assert.That(BoardPictures.PictureCell(mirrored, picture, x, y).Value, Is.EqualTo(BoardPictures.PictureCell(level, picture, w - 1 - x, y).Value));
                }
            }
        }

        [Test]
        public void TheFinishedPicture_HasItsSize_AndIsDeterministic()
        {
            (LevelDefinition level, BasePicture picture) = FirstLevel();
            byte[] framed = BoardPictures.Finished(level, picture, 24, true, out int fw, out int fh);
            float rim = BoardLayout.Gap + BoardPictures.PictureBorder + BoardPictures.PictureMargin;
            Assert.That(fw, Is.EqualTo((int)Math.Ceiling((picture.Width + (2f * rim)) * 24f)));
            Assert.That(fh, Is.EqualTo((int)Math.Ceiling((picture.Height + (2f * rim)) * 24f)));
            Assert.That(framed.Length, Is.EqualTo(fw * fh * 4));
            Assert.That(BoardPictures.Finished(level, picture, 24, true, out _, out _), Is.EqualTo(framed));
            Assert.That(framed[3], Is.EqualTo(0), "the corner of the room around the border is clear");

            byte[] flat = BoardPictures.Finished(level, picture, 16, false, out int w, out int h);
            Assert.That(w, Is.EqualTo(picture.Width * 16));
            Assert.That(h, Is.EqualTo(picture.Height * 16));
            Assert.That(flat.Length, Is.EqualTo(w * h * 4));
        }

        [Test]
        public void TheFinishedPicture_ShowsEachRoleInItsFullColor()
        {
            (LevelDefinition level, BasePicture picture) = FirstLevel();
            const int cell = 40;
            byte[] pixels = BoardPictures.Finished(level, picture, cell, false, out int w, out int _);
            int checkedCells = 0;
            for (int y = 0; y < picture.Height; y++)
            {
                for (int x = 0; x < picture.Width; x++)
                {
                    (VariantId? variant, int value) = BoardPictures.PictureCell(level, picture, x, y);
                    Rgba sample = Pixel(pixels, w, (x * cell) + (cell / 5), ((picture.Height - 1 - y) * cell) + (cell * 3 / 5));
                    if (variant.HasValue)
                    {
                        // A flat candy tile: its face is the variant's full color, not the pale ground's.
                        Rgba color = BoardPictures.ColorOf(variant.Value);
                        Assert.That(Distance(sample, color), Is.LessThan(Distance(sample, color.Lighten(0.55f))), $"cell {x},{y}");
                        checkedCells++;
                    }
                    else if (value != BasePicture.Stone)
                    {
                        Assert.That(Distance(sample, C.TileGround), Is.LessThan(6), $"ground cell {x},{y}");
                    }
                }
            }

            Assert.That(checkedCells, Is.GreaterThan(0));
        }

        [Test]
        public void AStoneObstacle_IsABlockWithAShadow_AndItsCrackFlips()
        {
            const int size = 124;
            byte[] stone = BoardPictures.StoneObstacle(size, 22, false);
            Assert.That(stone.Length, Is.EqualTo(size * size * 4));
            Assert.That(BoardPictures.StoneObstacle(size, 22, false), Is.EqualTo(stone));
            Assert.That(stone[3], Is.EqualTo(0), "the top-left corner is clear");
            Assert.That(Pixel(stone, size, size / 2, size * 3 / 4).A, Is.EqualTo(255), "the block");
            Assert.That(BoardPictures.StoneObstacle(size, 22, true), Is.Not.EqualTo(stone), "the crack runs the other way");
        }

        private static (LevelDefinition Level, BasePicture Picture) FirstLevel()
        {
            ContentSet content = DevContent.LoadCurated();
            LevelDefinition level = content.GetLevel(content.LevelNumbers[0]);
            return (level, content.GetPicture(level.Picture));
        }

        private static Rgba Pixel(byte[] rgba, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return new Rgba(rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]);
        }

        private static int Distance(Rgba a, Rgba b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
    }
}
