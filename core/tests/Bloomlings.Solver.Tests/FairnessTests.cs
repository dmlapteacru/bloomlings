using System;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Solver.Tests
{
    /// <summary>
    /// T097: the player-information fairness check (FR-039, R8). Four Flower pods must wait behind a Water tile; two pods
    /// lie under them. With the fifth slot the player must commit the Water pod: committing the Moss pod jams.
    /// </summary>
    public class FairnessTests
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

        private static LevelDefinition Level(PodDef x, PodDef y) => Definition(
            Board,
            new[] { Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1), x, y },
            stacks: new[] { new[] { "f1", "f3", x.Id }, new[] { "f2", "f4", y.Id } });

        private static PodDef Mystery(string id, VariantId variant) => new PodDef(id, variant, 1, true, null, null);

        private static FairnessResult Check(LevelDefinition level) => FairnessChecker.Check(level, Picture(Board), Options, 100_000);

        [Test]
        public void BlindGuess_IsUnfair()
        {
            // Both hidden pods have count 1: Water/Moss and Moss/Water both keep the accounting, so the player cannot tell.
            LevelDefinition level = Level(Mystery("x", VariantId.Water), Mystery("y", VariantId.Moss));

            Assert.That(new Solver().Solve(LevelSession.Load(level, Picture(Board), Options), new SolveOptions(100_000)).Status, Is.EqualTo(SolveStatus.Solvable), "winnable with hidden knowledge");
            FairnessResult result = Check(level);

            Assert.That(result.Worlds, Is.EqualTo(2));
            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Unfair));
            Assert.That(result.PlayerInfoFair, Is.False);
        }

        [Test]
        public void DeducibleFromTheAccounting_IsFair()
        {
            // The Moss pod is visible, so the per-variant accounting leaves only Water for the hidden pod.
            LevelDefinition level = Level(Mystery("x", VariantId.Water), Pod("y", VariantId.Moss, 1));

            FairnessResult result = Check(level);

            Assert.That(result.Worlds, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Fair));
            Assert.That(result.PlayerInfoFair, Is.True);
        }

        [Test]
        public void MysteryTile_RevealedBeforeTheChoice_IsFair()
        {
            // The hidden top of (3,1) reveals when it becomes reachable, which it is from the start.
            LevelDefinition level = Level(Pod("x", VariantId.Water, 1), Pod("y", VariantId.Moss, 1)) with
            {
                Overlays = new[] { new CellOverlay(new Bloomlings.Core.Boards.CellPos(3, 1), Array.Empty<VariantId>(), true, false, false, null) },
            };

            FairnessResult result = Check(level);

            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Fair));
        }

        [Test]
        public void MysteryLoadAboveTheCap_IsRejected()
        {
            LevelDefinition level = Level(Mystery("x", VariantId.Water), Mystery("y", VariantId.Moss)) with
            {
                Pods = new[]
                {
                    Mystery("f1", VariantId.Flower), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1),
                    Mystery("x", VariantId.Water), Mystery("y", VariantId.Moss),
                },
            };

            Assert.That(Check(level).Status, Is.EqualTo(FairnessStatus.OverCap));
        }

        [Test]
        public void LevelWithoutMystery_IsFairWithoutSearching()
        {
            FairnessResult result = Check(Level(Pod("x", VariantId.Water, 1), Pod("y", VariantId.Moss, 1)));

            Assert.That(result.Status, Is.EqualTo(FairnessStatus.Fair));
            Assert.That(result.PlayerInfoFair, Is.Null);
            Assert.That(result.NodesUsed, Is.Zero);
        }
    }
}
