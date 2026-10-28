using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The engine-free presentation of round levels (spec 006 FR-040–FR-047, contracts/presentation.md): the board layout
    /// from the rows (frame, gate, stream, bridge; up to 48 × 48), the link tube on the pods' fronts, cubes without a
    /// variant icon and the symbols mode, and the introduction cards.
    /// </summary>
    public class RoundPresentationTests
    {
        // ---- A board as tools/rounds/lib.mjs boardOf builds it from a picture (wall, walkway ring, picture, gate) ----

        private static RoundLevel BoardOf(string[] pictureTopFirst, int number = 1)
        {
            int w = pictureTopFirst[0].Length + 4;
            int h = pictureTopFirst.Length + 4;
            string wall = new string('#', w);
            string ring = "#" + new string('.', w - 2) + "#";
            var top = new List<string> { wall, ring };
            var choices = new List<RoundBridgeChoice>();
            foreach (string r in pictureTopFirst)
            {
                bool stream = r.All(c => c == '#' || c == 'b');
                string e = stream ? "#" : ".";
                top.Add("#" + e + r.Replace('b', '#') + e + "#");
            }

            int mid = w / 2;
            top.Add(ring);
            top.Add(wall.Substring(0, mid) + "." + wall.Substring(mid + 1));
            for (int y = 0; y < pictureTopFirst.Length; y++)
            {
                for (int x = 0; x < pictureTopFirst[y].Length; x++)
                {
                    if (pictureTopFirst[y][x] == 'b')
                    {
                        int by = h - 1 - (y + 2);
                        int cell = (by * w) + x + 2;
                        choices.Add(new RoundBridgeChoice(cell, cell - w, x + 2 < w / 2 ? "left" : "right"));
                    }
                }
            }

            List<string> rows = Enumerable.Reverse(top).ToList();
            var pods = new[] { new RoundPod('G', 1) };
            return new RoundLevel(number, w, h, rows, pods, new[] { new[] { 0 } }, new[] { mid }, choices.Count > 0 ? choices : null);
        }

        private static readonly string[] BridgePicture =
        {
            "GGGG",
            "#b#b",
            "PPGG",
        };

        [Test]
        public void TheRows_BecomeFrameGatePathStreamBridgeAndCubes()
        {
            RoundLevel level = BoardOf(BridgePicture);
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level);
            int w = level.Width;
            Assert.That(layout.Width, Is.EqualTo(8));
            Assert.That(layout.Height, Is.EqualTo(7));
            Assert.That(layout.Board.Width, Is.EqualTo(6), "the game's grid is the cells inside the wall");
            Assert.That(layout.Board.Height, Is.EqualTo(5));
            Assert.That(layout.KindOf(0, 0), Is.EqualTo(RoundCellKind.Frame));
            Assert.That(layout.KindOf(w / 2, 0), Is.EqualTo(RoundCellKind.Gate));
            Assert.That(layout.KindOf(1, 1), Is.EqualTo(RoundCellKind.Path), "the walkway ring");
            Assert.That(layout.KindOf(2, 2), Is.EqualTo(RoundCellKind.Material));
            Assert.That(layout.MaterialOf(2 + (2 * w)), Is.EqualTo(RoundMaterial.IndexOf('P')));
            Assert.That(layout.KindOf(1, 3), Is.EqualTo(RoundCellKind.Stream), "a stream row closes the ring");
            Assert.That(layout.KindOf(2, 3), Is.EqualTo(RoundCellKind.Stream));
            Assert.That(layout.KindOf(3, 3), Is.EqualTo(RoundCellKind.BridgeSite));
            Assert.That(layout.KindOf(0, 3), Is.EqualTo(RoundCellKind.Frame), "the stream ends in the frame");
            Assert.That(layout.MaterialOf(0), Is.EqualTo(-1));
        }

        [Test]
        public void TheGate_IsTheEntryArchBesideItsInsideCell_AndTheWalkersSetOffFromIt()
        {
            RoundLevel level = BoardOf(BridgePicture);
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level);
            Assert.That(layout.Gates.Count, Is.EqualTo(1));
            RoundGate gate = layout.Gates[0];
            Assert.That(gate.Cell, Is.EqualTo(level.Width / 2));
            Assert.That(gate.Side, Is.EqualTo(EntrySide.Bottom));
            Box inner = layout.CellBox(level.Width / 2, 1);
            Assert.That(gate.InnerCell.Left, Is.EqualTo(inner.Left).Within(1e-3));
            Assert.That(inner.Bottom, Is.EqualTo(layout.Grid.Bottom).Within(1e-3), "the inside cell is in the grid's bottom row");
            (float x, float y) = layout.Point(gate.Cell);
            Assert.That(x, Is.EqualTo(gate.Door.X).Within(1e-3));
            Assert.That(y, Is.EqualTo(gate.Door.Y).Within(1e-3));
            Assert.That(y, Is.GreaterThan(layout.Grid.Bottom).And.LessThan(layout.Outer.Bottom), "on the stone border");
            Assert.That(gate.Arch.Degrees, Is.EqualTo(0f), "a bottom arch opens upward");

            (float cx, float cy) = layout.Point(2 + (2 * level.Width));
            Box cell = layout.CellBox(2, 2);
            Assert.That(cx, Is.EqualTo(cell.CenterX).Within(1e-3));
            Assert.That(cy, Is.EqualTo(cell.CenterY).Within(1e-3));
        }

        [Test]
        public void ABridgeSite_TakesAPlankAcrossTheStream_ReachingBothBanks()
        {
            RoundLevel level = BoardOf(BridgePicture);
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level);
            Assert.That(layout.Bridges.Count, Is.EqualTo(2));
            RoundBridgeSite site = layout.Bridges[0];
            Assert.That(site.Index, Is.EqualTo(0));
            Assert.That(site.Cell, Is.EqualTo(3 + (3 * level.Width)));
            Assert.That(site.Anchor, Is.EqualTo(site.Cell - level.Width));
            Assert.That(site.Across, Is.True, "the anchor is a row below: the plank runs up and down");
            Assert.That(site.PlankDegrees, Is.EqualTo(90f));
            Assert.That(site.AnchorCell.Top, Is.EqualTo(site.Site.Bottom).Within(1e-3), "the anchor is the cell under the site");
            Box bounds = site.PlankBounds;
            Assert.That(bounds.Top, Is.LessThan(site.Site.Top), "onto the far bank");
            Assert.That(bounds.Bottom, Is.GreaterThan(site.Site.Bottom), "onto the near bank");
            Assert.That(bounds.Width, Is.LessThan(site.Site.Width));
        }

        // ---- Bridges on any strip and walls laid over a picture (17 §8 as amended on 2026-10-28) ----

        private static RoundLevel Laid(string[] pictureTopFirst, RoundOverlay? overlay)
        {
            var picture = new Bloomlings.Content.Rounds.RoundPicture("P9100", 1, "Test", pictureTopFirst[0].Length, pictureTopFirst.Length, pictureTopFirst, "2026-10-28");
            Bloomlings.Content.Rounds.RoundBoardShape board = Bloomlings.Content.Rounds.RoundPictureBoard.Of(picture, overlay);
            return new RoundLevel(1, board.Width, board.Height, board.RowsBottomFirst, new[] { new RoundPod('G', 1) }, new[] { new[] { 0 } }, board.Entries, board.Bridge, overlay: overlay);
        }

        // An L-shaped wall fences the top left corner: its site at (3, 1) crosses left, the one at (1, 2) crosses up.
        private static RoundLevel Fenced() => Laid(
            new[] { "PPPPPP", "PPPPPP", "GGGGGG", "GGGGGG", "GGGGGG" },
            new RoundOverlay(new[] { (0, 2), (1, 2), (2, 2), (3, 2), (3, 0), (3, 1) }, new[] { (1, 2), (3, 1) }));

        [Test]
        public void AnOverlaysWall_IsStone_AndThePicturesStream_StaysWater()
        {
            RoundLevel level = Fenced();
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level);
            int wall = level.PictureCell(0, 2);
            int site = level.PictureCell(1, 2);
            Assert.That(layout.KindOf(wall), Is.EqualTo(RoundCellKind.Wall));
            Assert.That(layout.IsStone(wall), Is.True);
            Assert.That(layout.KindOf(site), Is.EqualTo(RoundCellKind.BridgeSite));
            Assert.That(layout.IsStone(site), Is.True);
            Assert.That(layout.Bridges.All(b => b.OnWall), Is.True);
            Assert.That(layout.KindOf(1, level.Height - 1 - 4), Is.EqualTo(RoundCellKind.Wall), "the ring cell the wall closes runs on in stone");
            Assert.That(layout.KindOf(5, level.Height - 2), Is.EqualTo(RoundCellKind.Wall), "above the wall's top end too");

            RoundBoardLayout stream = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), BoardOf(BridgePicture));
            Assert.That(stream.KindOf(2, 3), Is.EqualTo(RoundCellKind.Stream));
            Assert.That(stream.Bridges.Any(b => b.OnWall), Is.False);
            Assert.That(stream.Bridges.All(b => b.Crossing == RoundCrossing.Up), Is.True);
        }

        [Test]
        public void ABridgeLeft_LaysItsPlankLeftToRight_AndGlowsOnItsNearBank()
        {
            RoundLevel level = Fenced();
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level);
            Assert.That(layout.Bridges.Count, Is.EqualTo(2));
            RoundBridgeSite left = layout.Bridges[0];
            Assert.That(left.Cell, Is.EqualTo(level.PictureCell(3, 1)), "reading order: the upper site first");
            Assert.That(left.Anchor, Is.EqualTo(left.Cell + 1));
            Assert.That(left.Crossing, Is.EqualTo(RoundCrossing.Left));
            Assert.That(left.Across, Is.False);
            Assert.That(left.PlankDegrees, Is.EqualTo(0f));
            Assert.That(left.AnchorCell.Left, Is.EqualTo(left.Site.Right).Within(1e-3), "the anchor is the cell right of the site");
            Box bounds = left.PlankBounds;
            Assert.That(bounds.Left, Is.LessThan(left.Site.Left), "onto the far bank");
            Assert.That(bounds.Right, Is.GreaterThan(left.Site.Right), "onto the near bank");
            Assert.That(bounds.Height, Is.LessThan(left.Site.Height));
            Assert.That(left.Glow.Left, Is.EqualTo(left.Site.Left).Within(1e-3));
            Assert.That(left.Glow.Right, Is.EqualTo(left.Site.Right + (left.Site.Width * RoundBoardLayout.GlowReach)).Within(1e-3), "the glow leans toward the anchor");
            Assert.That(left.Glow.Top, Is.EqualTo(left.Site.Top).Within(1e-3));

            RoundBridgeSite up = layout.Bridges[1];
            Assert.That(up.Crossing, Is.EqualTo(RoundCrossing.Up));
            Assert.That(up.PlankDegrees, Is.EqualTo(90f));
            Assert.That(up.Glow.Bottom, Is.GreaterThan(up.Site.Bottom), "the near bank is below");
            Assert.That(up.Glow.Top, Is.EqualTo(up.Site.Top).Within(1e-3));
        }

        [Test]
        public void ABridgeRight_AndABridgeDown_TurnTheirPlanksAndGlowsToo()
        {
            // A wall down column 3, its far bank on the right.
            RoundLevel rightward = Laid(new[] { "PPPPG", "PPPPG", "PPPPG", "PPPPG" }, new RoundOverlay(new[] { (3, 0), (3, 1), (3, 2), (3, 3) }, new[] { (3, 1) }));
            RoundBridgeSite right = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), rightward).Bridges.Single();
            Assert.That(right.Crossing, Is.EqualTo(RoundCrossing.Right));
            Assert.That(right.Anchor, Is.EqualTo(right.Cell - 1));
            Assert.That(right.PlankDegrees, Is.EqualTo(0f));
            Assert.That(right.Glow.Left, Is.LessThan(right.Site.Left));
            Assert.That(right.Glow.Right, Is.EqualTo(right.Site.Right).Within(1e-3));

            // A pocket in the bottom right corner, reached from above.
            RoundLevel downward = Laid(new[] { "GGGGGGG", "GGGGGGG", "GGGGGGG", "GGGGGGG" }, new RoundOverlay(new[] { (4, 2), (5, 2), (6, 2), (4, 3) }, new[] { (5, 2) }));
            RoundBridgeSite down = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), downward).Bridges.Single();
            Assert.That(down.Crossing, Is.EqualTo(RoundCrossing.Down));
            Assert.That(down.Anchor, Is.EqualTo(down.Cell + downward.Width));
            Assert.That(down.PlankDegrees, Is.EqualTo(90f));
            Assert.That(down.AnchorCell.Bottom, Is.EqualTo(down.Site.Top).Within(1e-3), "the anchor is the cell above the site");
            Assert.That(down.Glow.Top, Is.LessThan(down.Site.Top));
            Assert.That(down.Glow.Bottom, Is.EqualTo(down.Site.Bottom).Within(1e-3));
        }

        [Test]
        public void TheWall_IsRegistered_AndItsPictureFillsTheCell()
        {
            Assert.That(AssetSlots.Has(RoundBoardLayout.WallSlot), Is.True);
            byte[] wall = UiRaster.WallStone(32, 1);
            Assert.That(wall.Length, Is.EqualTo(32 * 32 * 4));
            for (int i = 3; i < wall.Length; i += 4)
            {
                Assert.That(wall[i], Is.EqualTo(255), "a wall fills its cell, so cells join into one wall");
            }

            Assert.That(UiRaster.WallStone(32, 1), Is.EqualTo(wall), "deterministic");
            Assert.That(UiRaster.WallStone(32, 2), Is.Not.EqualTo(wall), "the variants differ");
            Assert.That(Enumerable.Range(0, 20).Select(x => UiRaster.WallSeed(x, 3)).Distinct().Count(), Is.EqualTo(UiRaster.WallVariants));
        }

        [Test]
        public void ABoardOf48By48_FitsThePhoneFrame_WithItsStoneBorder()
        {
            string row = new string('G', 44);
            RoundLevel level = BoardOf(Enumerable.Repeat(row, 44).ToArray());
            Assert.That(level.Width, Is.EqualTo(RoundBoardLayout.MaxSide));
            Assert.That(level.Height, Is.EqualTo(RoundBoardLayout.MaxSide));
            foreach ((float w, float h) in new[] { (1080f, 1920f), (1080f, 2340f), (1080f, 2400f), (720f, 1600f) })
            {
                foreach (int stacks in new[] { 2, 3, 6 })
                {
                    ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, new Insets(h * 0.03f, h * 0.02f), stacks, 5);
                    RoundBoardLayout layout = RoundBoardLayout.In(r, level);
                    string at = w + "x" + h + ", " + stacks + " stacks";
                    Assert.That(layout.Outer.Within(r.BoardArea), Is.True, at + ": " + layout.Outer + " in " + r.BoardArea);
                    Assert.That(layout.Outer.Width, Is.LessThanOrEqualTo((r.W * ReferenceGameplayRegions.MaxBoardShare) + 0.5f), at);
                    Assert.That(layout.Outer.Bottom, Is.EqualTo(r.BoardArea.Bottom).Within(0.5f), at + ": docked on the board area");
                    Assert.That(layout.Cell, Is.GreaterThanOrEqualTo(r.W * 0.012f), at + ": " + layout.Cell + " px a cell");
                    Box corner = layout.CellBox(level.Width - 2, level.Height - 2);
                    Assert.That(corner.Within(layout.Grid), Is.True, at);
                }
            }
        }

        [Test]
        public void ABoardBeyond48_OrWithWrongRows_IsRefused()
        {
            string row = new string('G', 45);
            RoundLevel level = BoardOf(Enumerable.Repeat(row, 4).ToArray());
            Assert.Throws<ArgumentOutOfRangeException>(() => RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level));
            Assert.Throws<ArgumentException>(() => RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), 5, 5, new[] { "#####", "#.G.#", "#####" }, new[] { 2 }, null));
            Assert.Throws<ArgumentException>(() => RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), 5, 3, new[] { "##.##", "#.Z.#", "#####" }, new[] { 2 }, null));
        }

        [Test]
        public void DockBottom_KeepsTheCells_AndMovesTheBorder()
        {
            RoundLevel level = BoardOf(BridgePicture);
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 800f, 900f), level);
            RoundBoardLayout docked = layout.DockBottom(1000f);
            Assert.That(docked.Cell, Is.EqualTo(layout.Cell));
            Assert.That(docked.Outer.Bottom, Is.EqualTo(1000f).Within(1e-3));
            Assert.That(docked.Gates[0].Door.Y - layout.Gates[0].Door.Y, Is.EqualTo(1000f - layout.Outer.Bottom).Within(1e-3));
            Assert.That(docked.Bridges[1].Site.Top - layout.Bridges[1].Site.Top, Is.EqualTo(1000f - layout.Outer.Bottom).Within(1e-3));
        }

        // ---- Link tubes ----

        private static readonly Rgba Red = RoundLook.ColorOf('R');
        private static readonly Rgba Blue = RoundLook.ColorOf('B');

        [Test]
        public void ATube_JoinsTheFacingSidesOfTwoPods_LowOnTheirFront_InEachPodsColour()
        {
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, 2340f, Insets.None, 3, 5);
            PodChip left = r.Chip(0, 0);
            PodChip right = r.Chip(2, 0);
            LinkTubeShape tube = LinkTube.BetweenPods(right, Blue, left, Red);
            Assert.That(tube.Start.X, Is.LessThan(tube.End.X), "it starts at the left pod");
            Assert.That(tube.StartColor, Is.EqualTo(Red), "the half nearer each pod is that pod's colour");
            Assert.That(tube.EndColor, Is.EqualTo(Blue));
            Assert.That(tube.ColorAt(0.2f), Is.EqualTo(Red));
            Assert.That(tube.ColorAt(0.8f), Is.EqualTo(Blue));
            Assert.That(tube.Start.X, Is.EqualTo(left.Frame.Right).Within(left.Frame.Height * 0.05f), "on the left pod's right side");
            Assert.That(tube.End.X, Is.EqualTo(right.Frame.Left).Within(right.Frame.Height * 0.05f), "on the right pod's left side");
            Assert.That(tube.Degrees, Is.EqualTo(0f).Within(1e-3), "pods at one depth: level");
            Assert.That(tube.Middle.X, Is.EqualTo((left.Frame.Right + right.Frame.Left) / 2f).Within(left.Frame.Height * 0.05f));
            foreach (PodChip pod in new[] { left, right })
            {
                // Low on the front, never above or over the pod's top.
                Assert.That(tube.Box.Top, Is.GreaterThan(pod.Frame.CenterY));
                Assert.That(tube.Box.Bottom, Is.LessThan(pod.Frame.Bottom));
            }
        }

        [Test]
        public void PodsAtDifferentDepths_AreJoinedBySlantedTube_AndPlacesLikewise()
        {
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, 2340f, Insets.None, 4, 5);
            PodChip top = r.Chip(1, 0);
            PodChip deeper = r.Chip(3, 1);
            LinkTubeShape tube = LinkTube.BetweenPods(top, Red, deeper, Blue);
            Assert.That(tube.Degrees, Is.GreaterThan(0f).And.LessThan(90f), "down to the right, turned clockwise");
            Assert.That(tube.Start.Y, Is.EqualTo(LinkTube.FrontY(top.Frame)).Within(1e-3));
            Assert.That(tube.End.Y, Is.EqualTo(LinkTube.FrontY(deeper.Frame)).Within(1e-3));
            Assert.That(tube.Length, Is.GreaterThan(deeper.Frame.Left - top.Frame.Right));

            Box a = r.Slots[0];
            Box b = r.Slots[3];
            LinkTubeShape places = LinkTube.Between(a, Red, b, Blue);
            Assert.That(places.Start.X, Is.EqualTo(a.Right).Within(a.Height * 0.05f));
            Assert.That(places.End.X, Is.EqualTo(b.Left).Within(b.Height * 0.05f));
            Assert.That(places.Start.Y, Is.GreaterThan(a.CenterY), "low on the place's front");
            Assert.That(places.PictureKey, Does.StartWith(LinkTube.Slot + "/"));
            Assert.That(AssetSlots.Has(LinkTube.Slot), Is.True);
        }

        [Test]
        public void TheTubePicture_HasEachHalfInItsPodsColour_AndClearCorners()
        {
            byte[] pixels = UiRaster.LinkTube(160, 32, Red, Blue);
            Assert.That(pixels.Length, Is.EqualTo(160 * 32 * 4));
            Assert.That(pixels, Is.EqualTo(UiRaster.LinkTube(160, 32, Red, Blue)), "deterministic");
            Assert.That(pixels[3], Is.EqualTo(0), "the top left corner is clear");
            (int r1, int g1, int b1) = At(pixels, 160, 45, 17);
            (int r2, int g2, int b2) = At(pixels, 160, 115, 17);
            Assert.That(r1, Is.GreaterThan(b1), "the left half is the left pod's red");
            Assert.That(b2, Is.GreaterThan(r2), "the right half is the right pod's blue");
            Assert.That(pixels[((16 * 160) + 80) * 4 + 3], Is.EqualTo(255), "the coupling in the middle is solid");
        }

        private static (int R, int G, int B) At(byte[] pixels, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return (pixels[i], pixels[i + 1], pixels[i + 2]);
        }

        // ---- Cubes, symbols and the reveal glow ----

        [Test]
        public void ACube_HasNoVariantIcon_UnlessTheSymbolsModeIsOn()
        {
            foreach (RoundMaterial m in RoundMaterial.All)
            {
                Rgba color = RoundLook.ColorOf(m);
                Assert.That(color, Is.EqualTo(Rgba.FromHex(m.ColorHex)));
                byte[] plain = UiRaster.MaterialTile(48, color, RoundLook.Mark(m, false), RoundLook.CubeStyle);
                Assert.That(plain, Is.EqualTo(UiRaster.Tile(48, color, "leaf", TileStyle.BoardColor)), m.Code + ": the plain soft cube, no icon");
                byte[] symbol = UiRaster.MaterialTile(48, color, RoundLook.Mark(m, true), RoundLook.CubeStyle);
                Assert.That(symbol, Is.Not.EqualTo(plain), m.Code + ": its symbol in the symbols mode");
                Assert.That(Rgba.Contrast(RoundLook.SymbolInk(color), color), Is.GreaterThanOrEqualTo(3.0), m.Code + ": the symbol reads");
                byte[] pod = UiRaster.MaterialTile(40, color, RoundLook.Mark(m, true), RoundLook.PodStyle);
                Assert.That(pod, Is.Not.EqualTo(UiRaster.MaterialTile(40, color, null, RoundLook.PodStyle)), m.Code + ": on pods and places too");
            }

            Rgba leaf = RoundLook.ColorOf('G');
            Assert.That(UiRaster.MaterialTile(48, leaf, RoundLook.SymbolIds[0], RoundLook.CubeStyle, TileState.Mystery),
                Is.EqualTo(UiRaster.MaterialTile(48, leaf, null, RoundLook.CubeStyle, TileState.Mystery)), "a mystery shows no symbol");
        }

        [Test]
        public void TheSixteenSymbols_AreRegisteredShapes_AllDifferent()
        {
            Assert.That(RoundLook.SymbolIds.Count, Is.EqualTo(RoundMaterial.Codes.Length));
            var masks = new List<byte[]>();
            foreach (string id in RoundLook.SymbolIds)
            {
                Assert.That(ShapeLibrary.Has(id), Is.True, id);
                Assert.That(AssetSlots.Find(id)?.Kind, Is.EqualTo(PlaceholderKind.Shape), id);
                byte[] mask = ShapeRaster.Mask(id, 48, true);
                Assert.That(ShapeRaster.Coverage(mask), Is.GreaterThan(0.08f), id + " has some ink");
                masks.Add(mask);
            }

            for (int a = 0; a < masks.Count; a++)
            {
                for (int b = a + 1; b < masks.Count; b++)
                {
                    Assert.That(ShapeRaster.Difference(masks[a], masks[b]), Is.GreaterThan(0.05f), RoundLook.SymbolIds[a] + " vs " + RoundLook.SymbolIds[b]);
                }
            }

            Assert.That(ShapeLibrary.Has("ui.symbols") && AssetSlots.Has("ui.symbols"), Is.True);
            Assert.That(CardLook.SettingsIconOf("settings.symbols").Glyph, Is.EqualTo("ui.symbols"));
        }

        [Test]
        public void TheStreamAndBridge_AreRegistered_AndTheWaterJoinsAlongARun()
        {
            Assert.That(AssetSlots.Has(RoundBoardLayout.WaterSlot) && AssetSlots.Has(RoundBoardLayout.BridgeSlot), Is.True);
            byte[] water = UiRaster.Water(32, 1);
            Assert.That(water.Length, Is.EqualTo(32 * 32 * 4));
            for (int i = 3; i < water.Length; i += 4)
            {
                Assert.That(water[i], Is.EqualTo(255), "water fills its whole cell, so a run joins");
            }

            Assert.That(UiRaster.Water(32, 1), Is.Not.EqualTo(UiRaster.Water(32, 2)), "the run does not repeat one tile");
            Assert.That(UiRaster.WaterSeed(-1), Is.InRange(0, UiRaster.WaterVariants - 1));
        }

        [Test]
        public void AJustRevealedPod_GlowsForAboutOnePointSixSeconds()
        {
            Assert.That(RoundLook.RevealGlow(-0.1f), Is.EqualTo(0f));
            Assert.That(RoundLook.RevealGlow(RoundLook.RevealGlowRise), Is.EqualTo(1f).Within(1e-4));
            Assert.That(RoundLook.RevealGlow(0.8f), Is.GreaterThan(0.2f).And.LessThan(1f));
            Assert.That(RoundLook.RevealGlow(1.2f), Is.LessThan(RoundLook.RevealGlow(0.8f)));
            Assert.That(RoundLook.RevealGlow(RoundLook.RevealGlowSeconds), Is.EqualTo(0f));
            Assert.That(RoundLook.RevealGlowSeconds, Is.EqualTo(1.6f));
        }

        // ---- Introduction cards ----

        [Test]
        public void TheFirstMeeting_Explains_TheSecondReminds()
        {
            RoundIntroCard? hidden = RoundIntroCards.For(new RoundIntroduction("hidden", false));
            Assert.That(hidden, Is.Not.Null);
            Assert.That(hidden!.Kind, Is.EqualTo(RoundIntroKind.Explanation));
            Assert.That(hidden.Picture, Is.EqualTo(RoundIntroPicture.HiddenPod));
            Assert.That(hidden.LineKeys.Count, Is.EqualTo(3));
            Assert.That(hidden.Id, Is.EqualTo("round.intro.hidden"));

            RoundIntroCard? links = RoundIntroCards.For(new RoundIntroduction("links", true));
            Assert.That(links!.Kind, Is.EqualTo(RoundIntroKind.Reminder));
            Assert.That(links.Picture, Is.EqualTo(RoundIntroPicture.LinkedPair));
            Assert.That(links.LineKeys.Count, Is.EqualTo(1));
            Assert.That(links.Id, Is.EqualTo("round.intro.links.reminder"));

            Assert.That(RoundIntroCards.For("bridge", false)!.Picture, Is.EqualTo(RoundIntroPicture.Bridge));
            Assert.That(RoundIntroCards.For((RoundIntroduction?)null), Is.Null);
            Assert.That(RoundIntroCards.For("latch", false), Is.Null);

            var seen = new HashSet<string> { "round.intro.hidden" };
            Assert.That(RoundIntroCards.ToShow(new RoundIntroduction("hidden", false), seen.Contains), Is.Null, "shown once");
            Assert.That(RoundIntroCards.ToShow(new RoundIntroduction("hidden", true), seen.Contains)!.Kind, Is.EqualTo(RoundIntroKind.Reminder));
        }

        [Test]
        public void EveryIntroductionAndSymbolsString_IsInTheEnglishTable()
        {
            string csv = File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings", "UI", "Localization", "Resources", Loc.EnglishResource + ".csv"));
            Dictionary<string, string> english = Loc.ParseCsv(csv);
            var keys = RoundIntroCards.Keys().Concat(new[] { "settings.symbols", "settings.symbols_label" }).ToList();
            Assert.That(keys.Where(k => !english.ContainsKey(k)), Is.Empty);
            foreach (string mechanic in RoundIntroCards.Mechanics)
            {
                foreach (bool second in new[] { false, true })
                {
                    RoundIntroCard card = RoundIntroCards.For(mechanic, second)!;
                    Assert.That(english.ContainsKey(card.TitleKey) && card.LineKeys.All(english.ContainsKey) && english.ContainsKey(card.ButtonKey), Is.True, card.Id);
                }
            }
        }

        // ---- The symbols mode in the save ----

        [Test]
        public void TheSymbolsMode_IsOffByDefault_OptionalInTheSave_AndPersists()
        {
            PlayerSave save = PlayerSave.CreateNew("p", new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc));
            Assert.That(save.Settings.Symbols, Is.False);
            string off = SaveSerializer.Write(save);
            Assert.That(off, Does.Not.Contain("\"symbols\""), "an old save's shape while off");
            Assert.That(SaveSerializer.Read(off).Settings.Symbols, Is.False);

            save.Settings.Symbols = true;
            string on = SaveSerializer.Write(save);
            Assert.That(on, Does.Contain("\"symbols\""));
            Assert.That(SaveSerializer.Read(on).Settings.Symbols, Is.True);
        }
    }
}
