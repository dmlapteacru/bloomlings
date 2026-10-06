using System;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Solver.Tests
{
    /// <summary>
    /// The hidden-layer fairness check of an icons board (spec 001 FR-036 as amended on 2026-10-06, research R8b): on such
    /// a board the player does not see where the hidden layers lie, so the visible player must win the real level and
    /// sampled worlds with the same hidden layers placed elsewhere. The board: four Flower tiles behind a Water tile, a Moss
    /// tile on top; the Water pod needs one Water layer more than shows.
    /// </summary>
    public class HiddenLayerFairnessTests
    {
        private static readonly string[] Board =
        {
            "#######",
            "###m###",
            "###f###",
            "###f###",
            "###f###",
            "###f###",
            "###w###",
            ".......",
        };

        private static readonly CellOverlay WaterUnderTheGate = new CellOverlay(new CellPos(3, 1), new[] { VariantId.Water }, false, false, false, null);

        private static LevelDefinition Level(BoardLook? look, PodDef x, params CellOverlay[] overlays) => Definition(
            Board,
            new[] { Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1), x, Pod("y", VariantId.Moss, 1) },
            stacks: new[] { new[] { "f1", "f3", x.Id }, new[] { "f2", "f4", "y" } },
            overlays: overlays) with
        {
            BoardLook = look,
        };

        private static FairnessResult Check(LevelDefinition level, int maxHidden = int.MaxValue) =>
            FairnessChecker.Check(level, Picture(Board), Options, 100_000, 2, maxHidden);

        [Test]
        public void IconsBoard_HiddenLayersAnyPlayerCanHandle_IsFair()
        {
            LevelDefinition level = Level(BoardLook.Icons, Pod("x", VariantId.Water, 2), WaterUnderTheGate);

            FairnessResult result = Check(level);

            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Fair), result.Detail);
            Assert.That(result.PlayerInfoFair, Is.True);
            Assert.That(result.Worlds, Is.EqualTo(1 + HiddenLayerFairness.SampledWorlds), "the real level and the sampled worlds");
            Assert.That(result.NodesUsed, Is.GreaterThan(0).And.LessThanOrEqualTo(HiddenLayerFairness.CheckBudget));
            Assert.That(Check(level), Is.EqualTo(result), "reproducible");
        }

        [Test]
        public void TheSameLevel_OnAPeekBoard_ShowsItsLayers_SoTheCheckHasNothingToJudge()
        {
            FairnessResult result = Check(Level(null, Pod("x", VariantId.Water, 2), WaterUnderTheGate));

            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Fair));
            Assert.That(result.PlayerInfoFair, Is.Null);
            Assert.That(result.NodesUsed, Is.Zero);
        }

        [Test]
        public void IconsBoard_WithoutHiddenLayers_HidesNothing()
        {
            FairnessResult result = Check(Level(BoardLook.Icons, Pod("x", VariantId.Water, 1)));

            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Fair));
            Assert.That(result.PlayerInfoFair, Is.Null);
            Assert.That(result.Worlds, Is.EqualTo(1));
        }

        [Test]
        public void IconsBoard_MoreHiddenLayersThanTheCap_IsOverCap()
        {
            FairnessResult result = Check(Level(BoardLook.Icons, Pod("x", VariantId.Water, 2), WaterUnderTheGate), maxHidden: 0);

            Assert.That(result.Status, Is.EqualTo(FairnessStatus.OverCap));
            Assert.That(result.PlayerInfoFair, Is.False);
        }

        [Test]
        public void IconsBoard_WithAMysteryTileOrPod_IsNotCovered()
        {
            var mysteryTile = new CellOverlay(new CellPos(3, 6), Array.Empty<VariantId>(), true, false, false, null);
            LevelDefinition tile = Level(BoardLook.Icons, Pod("x", VariantId.Water, 1), mysteryTile);
            LevelDefinition pod = Level(BoardLook.Icons, new PodDef("x", VariantId.Water, 1, true, null, null));

            Assert.That(Check(tile).Status, Is.EqualTo(FairnessStatus.Uncovered));
            Assert.That(Check(pod).Status, Is.EqualTo(FairnessStatus.Uncovered));
            Assert.That(Check(pod).PlayerInfoFair, Is.False);
        }

        [Test]
        public void TheViewHidesTheNextLayer_OnAnIconsBoardOnly()
        {
            LevelSession peek = LevelSession.Load(Level(null, Pod("x", VariantId.Water, 2), WaterUnderTheGate), Picture(Board), Options);
            LevelSession icons = LevelSession.Load(Level(BoardLook.Icons, Pod("x", VariantId.Water, 2), WaterUnderTheGate), Picture(Board), Options);

            Assert.That(peek.View.BoardLook, Is.EqualTo(BoardLook.Peek));
            Assert.That(peek.View.Cell(new CellPos(3, 1)).Next, Is.EqualTo(VariantId.Water));
            Assert.That(icons.View.BoardLook, Is.EqualTo(BoardLook.Icons));
            Assert.That(icons.View.Cell(new CellPos(3, 1)).Next, Is.Null, "the next layer is a surprise");
            Assert.That(icons.View.Cell(new CellPos(3, 1)).Visible, Is.EqualTo(VariantId.Water));
            Assert.That(icons.StateHash, Is.EqualTo(peek.StateHash), "the look is presentation: the rules and the state are the same");
        }
    }
}
