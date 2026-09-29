using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Board
{
    public class BoardBuilderTests
    {
        // 7×8 flower in a pot on a water background (top row first).
        private static readonly string[] FlowerRows =
        {
            "bbbpbbb",
            "bbpppbb",
            "bbbpbbb",
            "bblllbb",
            "bbblbbb",
            "bbwwwbb",
            "bbwwwbb",
            "bbbbbbb",
        };

        private static BasePicture Flower() => TestContent.Picture("flower_pot", TestContent.GardenLegend, FlowerRows);

        [Test]
        public void Build_MapsPictureRolesToTopLayerVariants()
        {
            BasePicture picture = Flower();
            var board = BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping()), picture, VariantCatalog.Default);

            Assert.That(board.Width, Is.EqualTo(7));
            Assert.That(board.Height, Is.EqualTo(8));
            Assert.That(board.TopLayer(board.IndexOf(new CellPos(0, 0))), Is.EqualTo(VariantId.Water));
            Assert.That(board.TopLayer(board.IndexOf(new CellPos(3, 1))), Is.EqualTo(VariantId.Wood));
            Assert.That(board.TopLayer(board.IndexOf(new CellPos(3, 3))), Is.EqualTo(VariantId.Leaf));
            Assert.That(board.TopLayer(board.IndexOf(new CellPos(3, 7))), Is.EqualTo(VariantId.Flower));
            Assert.That(board.CountLayers(VariantId.Flower), Is.EqualTo(5));
        }

        [Test]
        public void Build_MirrorFlipsColumns()
        {
            BasePicture picture = TestContent.Picture(
                "corner",
                TestContent.GardenLegend,
                "lbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "pbbbbbb");
            var level = TestContent.Level(picture, TestContent.GardenMapping(), mirror: Mirror.Horizontal);

            var board = BoardBuilder.Build(level, picture, VariantCatalog.Default);

            Assert.That(board.TopLayer(board.IndexOf(new CellPos(6, 0))), Is.EqualTo(VariantId.Flower));
            Assert.That(board.TopLayer(board.IndexOf(new CellPos(6, 7))), Is.EqualTo(VariantId.Leaf));
            Assert.That(board.TopLayer(board.IndexOf(new CellPos(0, 0))), Is.EqualTo(VariantId.Water));
        }

        [Test]
        public void Build_AppliesLayersStoneHoleMysteryAndKeyOverlays()
        {
            BasePicture picture = Flower();
            var overlays = new[]
            {
                TestContent.Overlay(3, 3, layersBelow: new[] { VariantId.VioletBud, VariantId.Dew }, keyId: "k1"),
                TestContent.Overlay(0, 7, stone: true),
                TestContent.Overlay(6, 7, hole: true),
                TestContent.Overlay(3, 7, mystery: true),
            };
            var board = BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping(), overlays: overlays), picture, VariantCatalog.Default);

            int layered = board.IndexOf(new CellPos(3, 3));
            Assert.That(board.RemainingLayers(layered), Is.EqualTo(3));
            Assert.That(board.TopLayer(layered), Is.EqualTo(VariantId.Leaf));
            Assert.That(board.NextLayer(layered), Is.EqualTo((VariantId?)VariantId.VioletBud));
            Assert.That(board.KeyAt(layered), Is.EqualTo("k1"));
            Assert.That(board.KindAt(board.IndexOf(new CellPos(0, 7))), Is.EqualTo(CellKind.Stone));
            Assert.That(board.KindAt(board.IndexOf(new CellPos(6, 7))), Is.EqualTo(CellKind.Open));
            Assert.That(board.IsMysteryHidden(board.IndexOf(new CellPos(3, 7))), Is.True);
            Assert.That(board.CountLayers(VariantId.Dew), Is.EqualTo(1));
        }

        [Test]
        public void ClearTopLayer_RevealsNextLayerThenOpensCell()
        {
            BasePicture picture = Flower();
            var overlays = new[] { TestContent.Overlay(3, 3, layersBelow: new[] { VariantId.VioletBud }) };
            var board = BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping(), overlays: overlays), picture, VariantCatalog.Default);
            int index = board.IndexOf(new CellPos(3, 3));

            LayerClearResult first = board.ClearTopLayer(index);
            Assert.That(first.Cleared, Is.EqualTo(VariantId.Leaf));
            Assert.That(first.Opened, Is.False);
            Assert.That(first.Revealed, Is.EqualTo(VariantId.VioletBud));

            LayerClearResult second = board.ClearTopLayer(index);
            Assert.That(second.Opened, Is.True);
            Assert.That(board.KindAt(index), Is.EqualTo(CellKind.Open));
        }

        [Test]
        public void Build_RejectsColorGroupMismatch()
        {
            BasePicture picture = Flower();
            Dictionary<string, VariantId> mapping = TestContent.GardenMapping();
            mapping["petal"] = VariantId.Leaf; // pink/purple role cannot become a green variant

            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, mapping), picture, VariantCatalog.Default));
        }

        [Test]
        public void Build_RejectsUnapprovedPicture()
        {
            BasePicture picture = Flower() with { Review = new PictureReview(ReviewStatus.Draft, null, null, null) };

            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping()), picture, VariantCatalog.Default));
        }

        [Test]
        public void Build_RejectsUnmappedUsedRole()
        {
            BasePicture picture = Flower();
            Dictionary<string, VariantId> mapping = TestContent.GardenMapping();
            mapping.Remove("pot");

            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, mapping), picture, VariantCatalog.Default));
        }

        [Test]
        public void Build_RejectsEntryOffTheEdgeOrBlocked()
        {
            BasePicture picture = Flower();
            var offEdge = new[] { new EntryDef(new CellPos(3, 1), EntrySide.Bottom) };
            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping(), entries: offEdge), picture, VariantCatalog.Default));

            var blocked = new[] { new EntryDef(new CellPos(3, 0), EntrySide.Bottom) };
            var stone = new[] { TestContent.Overlay(3, 0, stone: true) };
            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping(), entries: blocked, overlays: stone), picture, VariantCatalog.Default));
        }

        [Test]
        public void Build_RejectsKeyOrLayersOnNonTargetCell()
        {
            BasePicture picture = TestContent.Picture(
                "holes",
                TestContent.GardenLegend,
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbb.bbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb",
                "bbbbbbb");
            var key = new[] { TestContent.Overlay(3, 4, keyId: "k1") };

            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping(), overlays: key), picture, VariantCatalog.Default));
        }

        [Test]
        public void Build_RejectsSpecialCoveringATarget()
        {
            BasePicture picture = Flower();
            var special = new SpecialDef(
                "gate1",
                SpecialType.Gate,
                new[] { new CellPos(3, 3) },
                new SpecialCondition(SpecialConditionKind.Key, "k1", null, null, new CellPos[0]),
                new SpecialEffect(SpecialEffectKind.OpenCells, new[] { new CellPos(3, 3) }));

            Assert.Throws<InvalidLevelException>(() => BoardBuilder.Build(TestContent.Level(picture, TestContent.GardenMapping(), specials: new[] { special }), picture, VariantCatalog.Default));
        }
    }
}
