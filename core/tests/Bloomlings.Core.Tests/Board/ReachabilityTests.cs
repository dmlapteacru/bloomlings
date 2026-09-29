using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Board
{
    public class ReachabilityTests
    {
        private static Boards.Board Build(string[] rows, EntryDef[]? entries = null, CellOverlay[]? overlays = null)
        {
            BasePicture picture = TestContent.Picture("test", TestContent.GardenLegend, rows);
            LevelDefinition level = TestContent.Level(picture, TestContent.GardenMapping(), entries: entries, overlays: overlays);
            return BoardBuilder.Build(level, picture, VariantCatalog.Default);
        }

        [Test]
        public void FullBoard_OnlyTheEntryCellIsReachable()
        {
            var board = Build(new[]
            {
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
            });

            ReachabilityResult result = Reachability.Compute(board);

            Assert.That(result.Targets.Count, Is.EqualTo(1));
            Assert.That(result.Targets[0].Cell, Is.EqualTo(new CellPos(3, 0)));
            Assert.That(result.Targets[0].Distance, Is.EqualTo(1));
            Assert.That(result.RouteTo(result.Targets[0].Index), Is.EqualTo(new[] { new CellPos(3, 0) }));
        }

        [Test]
        public void OpenCorridor_GivesDistancesAndRoutes()
        {
            // Column 3 is open from the entry up to row 3.
            var board = Build(new[]
            {
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                "bbb.bbb", "bbb.bbb", "bbb.bbb", "bbb.bbb",
            });

            ReachabilityResult result = Reachability.Compute(board);

            Assert.That(result.OpenDistance(board.IndexOf(new CellPos(3, 0))), Is.EqualTo(1));
            Assert.That(result.OpenDistance(board.IndexOf(new CellPos(3, 3))), Is.EqualTo(4));
            ReachableTarget top = result.Targets.Single(t => t.Cell == new CellPos(3, 4));
            Assert.That(top.Distance, Is.EqualTo(5));
            Assert.That(
                result.RouteTo(top.Index),
                Is.EqualTo(new[] { new CellPos(3, 0), new CellPos(3, 1), new CellPos(3, 2), new CellPos(3, 3), new CellPos(3, 4) }));
        }

        [Test]
        public void DiagonalContactIsNotReachable()
        {
            // The open cell (3,0) touches (2,1) and (4,1) only diagonally.
            var board = Build(new[]
            {
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbb.bbb",
            });

            ReachabilityResult result = Reachability.Compute(board);

            Assert.That(result.IsReachable(board.IndexOf(new CellPos(2, 1))), Is.False);
            Assert.That(result.IsReachable(board.IndexOf(new CellPos(4, 1))), Is.False);
            Assert.That(result.IsReachable(board.IndexOf(new CellPos(3, 1))), Is.True);
            Assert.That(result.IsReachable(board.IndexOf(new CellPos(2, 0))), Is.True);
        }

        [Test]
        public void StonesBlockRoutes()
        {
            var board = Build(new[]
            {
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                "bbbbbbb", "bbb#bbb", "bbb.bbb", "bbb.bbb",
            });

            ReachabilityResult result = Reachability.Compute(board);

            Assert.That(result.IsReachable(board.IndexOf(new CellPos(3, 3))), Is.False, "The stone at (3,2) blocks the corridor.");
            Assert.That(result.Targets.Any(t => board.KindAt(t.Index) == CellKind.Stone), Is.False);
            Assert.That(result.IsReachable(board.IndexOf(new CellPos(2, 1))), Is.True);
        }

        [Test]
        public void TwoEntries_BothAreSources()
        {
            var entries = new[]
            {
                new EntryDef(new CellPos(0, 0), EntrySide.Bottom),
                new EntryDef(new CellPos(6, 5), EntrySide.Right),
            };
            var board = Build(
                new[]
                {
                    "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                    "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                },
                entries);

            ReachabilityResult result = Reachability.Compute(board);

            Assert.That(result.Targets.Select(t => t.Cell), Is.EquivalentTo(new[] { new CellPos(0, 0), new CellPos(6, 5) }));
        }

        [Test]
        public void Targets_AreOrderedByDistanceThenRowThenColumn()
        {
            // Open row 0 across the board: every row-1 target is at distance 2 except the entry neighbours.
            var board = Build(new[]
            {
                "bbbbbbb", "bbbbbbb", "bbbbbbb", "bbbbbbb",
                "bbbbbbb", "bbbbbbb", "bbbbbbb", ".......",
            });

            ReachabilityResult result = Reachability.Compute(board);

            var cells = result.Targets.Select(t => (t.Distance, t.Cell.Y, t.Cell.X)).ToList();
            var sorted = cells.OrderBy(c => c.Distance).ThenBy(c => c.Y).ThenBy(c => c.X).ToList();
            Assert.That(cells, Is.EqualTo(sorted));
            Assert.That(result.Targets[0].Cell, Is.EqualTo(new CellPos(3, 1)), "Nearest to the entry comes first.");
            Assert.That(result.Targets.Count, Is.EqualTo(7));
        }
    }
}
