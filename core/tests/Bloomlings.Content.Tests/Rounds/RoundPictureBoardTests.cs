using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Content.Tests.Rounds
{
    /// <summary>
    /// The board of a picture (<see cref="RoundPictureBoard.Of"/>, the reference's <c>boardOf</c>) after the amendment of
    /// 2026-10-28 (17 §8 "Где стоит мост"): strips on any side, crossings up, down, left and right, an overlay laid over an
    /// approved picture, the ring closed outward of every strip cell on the picture's edge, the anchor the one neighbour the
    /// entry reaches. The expected boards are worked out by hand; the published levels are the backward identity cases.
    /// </summary>
    [TestFixture]
    public class RoundPictureBoardTests
    {
        private static RoundPicture Picture(params string[] rowsTopFirst) =>
            new RoundPicture("P9100", 1, "Test", rowsTopFirst[0].Length, rowsTopFirst.Length, rowsTopFirst, "2026-10-28");

        private static string[] TopFirst(RoundBoardShape board) => board.RowsBottomFirst.Reverse().ToArray();

        private static (int Cell, int Anchor)[] Sites(RoundBoardShape board) =>
            board.Bridge?.Select(c => (c.Cell, c.Anchor)).ToArray() ?? Array.Empty<(int, int)>();

        [Test]
        public void AWallDownThePicture_CutsTheLeftBankOff_AndItsSiteCrossesLeft()
        {
            // A wall column at x = 1 from the top edge to the bottom edge closes the ring above and below it.
            RoundBoardShape board = RoundPictureBoard.Of(Picture("G#PPP", "Gb.PP", "G#PPP", "G#PPP"));

            Assert.Multiple(() =>
            {
                Assert.That(TopFirst(board), Is.EqualTo(new[]
                {
                    "#########",
                    "#..#....#",
                    "#.G#PPP.#",
                    "#.G#.PP.#",
                    "#.G#PPP.#",
                    "#.G#PPP.#",
                    "#..#....#",
                    "####.####",
                }));
                Assert.That(board.Entries, Is.EqualTo(new[] { 4 }));

                // The site (1, 1) is cell 4 × 9 + 3; its anchor is the open cell to its right.
                Assert.That(Sites(board), Is.EqualTo(new[] { (39, 40) }));
                Assert.That(board.Bridge!.Single().Label, Is.EqualTo("Мостик влево в центре"));
            });
        }

        [Test]
        public void AnLShapedOverlay_FencesACorner_WithASiteUpAndASiteLeft_InReadingOrder()
        {
            RoundPicture picture = Picture("PPPPPP", "PPPPPP", "GGGGGG", "GGGGGG", "GGGGGG");
            var overlay = new RoundOverlay(new[] { (0, 2), (1, 2), (2, 2), (3, 2), (3, 0), (3, 1) }, new[] { (1, 2), (3, 1) });

            RoundBoardShape board = RoundPictureBoard.Of(picture, overlay);

            Assert.Multiple(() =>
            {
                Assert.That(TopFirst(board), Is.EqualTo(new[]
                {
                    "##########",
                    "#....#...#",
                    "#.PPP#PP.#",
                    "#.PPP#PP.#",
                    "######GG.#",
                    "#.GGGGGG.#",
                    "#.GGGGGG.#",
                    "#........#",
                    "#####.####",
                }));

                // Reading order: (3, 1) in the upper row first, crossing left from the P on its right; then (1, 2), crossing
                // up from the G below it.
                Assert.That(Sites(board), Is.EqualTo(new[] { (55, 56), (43, 33) }));
                Assert.That(board.Bridge!.Select(c => c.Label), Is.EqualTo(new[] { "Мостик влево сверху", "Мостик вверх слева" }));
                Assert.That(RoundLevelValidator.Orthogonal(55, 56, board.Width) && RoundLevelValidator.Orthogonal(43, 33, board.Width), Is.True);
            });
        }

        [Test]
        public void ASiteWhoseNearBankIsAbove_CrossesDown()
        {
            // A pocket in the bottom right corner, fenced by an overlay; its site is reached from above.
            RoundPicture picture = Picture("GGGGGGG", "GGGGGGG", "GGGGGGG", "GGGGGGG");
            var overlay = new RoundOverlay(new[] { (4, 2), (5, 2), (6, 2), (4, 3) }, new[] { (5, 2) });

            RoundBoardShape board = RoundPictureBoard.Of(picture, overlay);

            Assert.Multiple(() =>
            {
                Assert.That(TopFirst(board), Is.EqualTo(new[]
                {
                    "###########",
                    "#.........#",
                    "#.GGGGGGG.#",
                    "#.GGGGGGG.#",
                    "#.GGGG#####",
                    "#.GGGG#GG.#",
                    "#.....#...#",
                    "#####.#####",
                }));
                Assert.That(Sites(board), Is.EqualTo(new[] { (40, 51) }));
                Assert.That(board.Bridge!.Single().Label, Is.EqualTo("Мостик вниз справа"));
            });
        }

        [Test]
        public void TheOverlay_IsThePictureWithItsCellsChanged()
        {
            RoundPicture picture = Picture("PPPPPP", "PPPPPP", "GGGGGG", "GGGGGG", "GGGGGG");
            var overlay = new RoundOverlay(new[] { (0, 2), (1, 2), (2, 2), (3, 2), (3, 0), (3, 1) }, new[] { (1, 2), (3, 1) });
            RoundPicture drawn = Picture("PPP#PP", "PPPbPP", "#b##GG", "GGGGGG", "GGGGGG");

            RoundBoardShape laid = RoundPictureBoard.Of(picture, overlay);
            RoundBoardShape plain = RoundPictureBoard.Of(drawn);

            Assert.Multiple(() =>
            {
                Assert.That(laid.RowsBottomFirst, Is.EqualTo(plain.RowsBottomFirst));
                Assert.That(Sites(laid), Is.EqualTo(Sites(plain)));
            });
        }

        [Test]
        public void AStripCellOnTheEdge_ClosesOnlyTheRingCellOutward_ACornerBoth()
        {
            // A lone '#' in the top left corner closes the ring above and left of it, not the ring's corner.
            RoundBoardShape corner = RoundPictureBoard.Of(Picture("#GG", "GGG", "GGG"));
            // A lone '#' in the middle of the right edge closes the ring cell right of it only.
            RoundBoardShape side = RoundPictureBoard.Of(Picture("GGG", "GG#", "GGG"));

            Assert.Multiple(() =>
            {
                Assert.That(TopFirst(corner), Is.EqualTo(new[] { "#######", "#.#...#", "###GG.#", "#.GGG.#", "#.GGG.#", "#.....#", "###.###" }));
                Assert.That(TopFirst(side), Is.EqualTo(new[] { "#######", "#.....#", "#.GGG.#", "#.GG###", "#.GGG.#", "#.....#", "###.###" }));
                Assert.That(corner.Bridge, Is.Null);
            });
        }

        [Test]
        public void ASiteWithoutAReachableNeighbour_IsAnError()
        {
            // The corner site's ring cells are closed and its other neighbours are walls.
            var e = Assert.Throws<ArgumentException>(() => RoundPictureBoard.Of(Picture("b#GGG", "#GGGG", "GGGGG")));
            Assert.That(e!.Message, Does.Contain("bridge site (0,0)").And.Contain("no neighbour the entry reaches"));
        }

        [Test]
        public void ASiteWithMoreThanOneReachableNeighbour_IsAnError()
        {
            var e = Assert.Throws<ArgumentException>(() => RoundPictureBoard.Of(Picture("GGbGG", "GGGGG", "GGGGG")));
            Assert.That(e!.Message, Does.Contain("bridge site (2,0)").And.Contain("3 neighbours the entry reaches"));
        }

        [Test]
        public void AnOverlaySiteOffAWall_IsAnError()
        {
            RoundPicture picture = Picture("GGG", "GGG", "GGG");
            var e = Assert.Throws<ArgumentException>(() => RoundPictureBoard.Of(picture, new RoundOverlay(Array.Empty<(int, int)>(), new[] { (1, 1) })));
            Assert.That(e!.Message, Does.Contain("overlay site (1,1) is not on a wall"));
        }

        [Test]
        public void TwoEqualLabels_AreToldApartByTheirOrder()
        {
            // Two sites crossing up in the middle third: "в центре" twice becomes left and right.
            RoundBoardShape board = RoundPictureBoard.Of(Picture("GGGGGGGGG", "###b#b###", "PPPPPPPPP"));
            Assert.That(board.Bridge!.Select(c => c.Label), Is.EqualTo(new[] { "Мостик вверх слева", "Мостик вверх справа" }));
        }

        [Test]
        public void AnOverlayCellOutsideThePicture_IsAnError()
        {
            RoundPicture picture = Picture("GGG", "GGG", "GGG");
            var e = Assert.Throws<ArgumentException>(() => RoundPictureBoard.Of(picture, new RoundOverlay(new[] { (3, 0) }, Array.Empty<(int, int)>())));
            Assert.That(e!.Message, Does.Contain("overlay wall (3,0) lies outside the 3×3 picture"));
        }

        [Test]
        public void AFullWidthStream_IsUnchanged_ItsSitesCrossUp()
        {
            RoundBoardShape board = RoundPictureBoard.Of(Picture("GGGGG", "#b#b#", "PPPPP", "....."));

            Assert.Multiple(() =>
            {
                Assert.That(TopFirst(board), Is.EqualTo(new[]
                {
                    "#########",
                    "#.......#",
                    "#.GGGGG.#",
                    "#########",
                    "#.PPPPP.#",
                    "#.......#",
                    "#.......#",
                    "####.####",
                }));
                Assert.That(Sites(board), Is.EqualTo(new[] { (39, 30), (41, 32) }));
                Assert.That(board.Bridge!.Select(c => c.Label), Is.EqualTo(new[] { "Мостик вверх слева", "Мостик вверх справа" }));
            });
        }

        private static IEnumerable<int> Published() => Enumerable.Range(1, 20);

        /// <summary>Backward identity: every published level's board is still its picture's board (no overlay).</summary>
        [TestCaseSource(nameof(Published))]
        public void APublishedLevel_IsStillItsPicturesBoard(int number)
        {
            string root = RepositoryRoot;
            RoundLevel level = RoundJson.ReadLevel(File.ReadAllText(Path.Combine(root, "content", "game", "rounds", "levels", $"L{number:D4}.json")));
            RoundPicture picture = RoundJson.ReadPicture(File.ReadAllText(Path.Combine(root, "content", "game", "rounds", "pictures", level.PictureId + ".json")));

            RoundBoardShape board = RoundPictureBoard.Of(picture, level.Overlay);

            Assert.Multiple(() =>
            {
                Assert.That((board.Width, board.Height), Is.EqualTo((level.Width, level.Height)));
                Assert.That(board.RowsBottomFirst, Is.EqualTo(level.RowsBottomFirst));
                Assert.That(board.Entries, Is.EqualTo(level.Entries));
                // The labels are the reference's names, not part of the board (the picture-board check leaves them out too).
                Assert.That(board.Bridge?.Select(c => (c.Cell, c.Anchor)), Is.EqualTo(level.Bridge?.Select(c => (c.Cell, c.Anchor))));
            });
        }

        private static string RepositoryRoot
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
                {
                    dir = dir.Parent;
                }

                return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
            }
        }
    }
}
