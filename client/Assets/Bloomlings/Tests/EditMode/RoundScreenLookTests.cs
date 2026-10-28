using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests.EditMode
{
    /// <summary>
    /// The owner's round level screen of 2026-10-28 (spec 006 FR-064, contracts/presentation.md): a bigger fixed board box,
    /// pods as cubes in exactly three rows one after another with no "+N", and waiting pods whose colours stay readable.
    /// </summary>
    public class RoundScreenLookTests
    {
        private static readonly (float W, float H)[] Screens = { (1080f, 1920f), (1080f, 2340f), (1080f, 2400f), (720f, 1600f) };

        private static Insets InsetsOf(float h) => new Insets(h * 0.03f, h * 0.02f);

        // ---- The pods' queue ----

        [Test]
        public void EveryColumn_ShowsThreeCubes_OneAfterAnother_InOrder_NeverOverlapping()
        {
            Assert.That(RoundScreenLook.PodRows, Is.EqualTo(3));
            foreach ((float w, float h) in Screens)
            {
                foreach (int stacks in new[] { 1, 2, 3, 4, 5, 6 })
                {
                    ReferenceGameplayRegions r = ScreenLayout.RoundGameplay(w, h, InsetsOf(h), stacks, 5, true, true);
                    string at = $"{w}x{h}, {stacks} columns";
                    Assert.That(r.PodRows, Is.EqualTo(3), at);
                    Assert.That(r.Shows(2), Is.True, at);
                    Assert.That(r.Shows(3), Is.False, at + ": the fourth pod is never shown");
                    Assert.That(r.PodGap, Is.GreaterThan(0f), at + ": a gap, not an overlap");
                    var boxes = new List<Box>();
                    for (int st = 0; st < stacks; st++)
                    {
                        for (int d = 0; d < r.PodRows; d++)
                        {
                            Box pod = r.Pod(st, d);
                            Assert.That(pod.Width, Is.EqualTo(pod.Height).Within(0.01f), at + ": a square cube");
                            Assert.That(pod.Width, Is.LessThanOrEqualTo((RoundScreenLook.PodCubeShare * r.W) + 0.01f), at);
                            Assert.That(pod.Within(r.PodRow), Is.True, at + ": " + pod + " in the pod row " + r.PodRow);
                            Assert.That(pod.Within(r.Safe), Is.True, at);
                            if (d > 0)
                            {
                                Box above = r.Pod(st, d - 1);
                                Assert.That(pod.Top, Is.GreaterThan(above.Bottom), at + ": one after another, below the one before");
                                Assert.That(pod.CenterX, Is.EqualTo(above.CenterX).Within(0.01f), at + ": in its column");
                            }

                            PodChip cube = PodChip.Cube(pod);
                            Assert.That(cube.Frame.Left, Is.EqualTo(pod.Left).Within(0.01f), at + ": no frame round the cube");
                            Assert.That(cube.Frame.Top, Is.EqualTo(pod.Top).Within(0.01f), at);
                            Assert.That(cube.Frame.Width, Is.EqualTo(pod.Width).Within(0.01f), at);
                            Assert.That(cube.Tile, Is.EqualTo(cube.Frame), at + ": the pod is its cube");
                            Assert.That(cube.Badge.IsEmpty, Is.True, at + ": no \"+N\" disc");
                            Assert.That(cube.Count.Within(cube.Frame), Is.True, at + ": the count on the cube");
                            boxes.Add(pod);
                        }
                    }

                    for (int i = 0; i < boxes.Count; i++)
                    {
                        for (int j = i + 1; j < boxes.Count; j++)
                        {
                            Box a = boxes[i];
                            Box b = boxes[j];
                            bool overlap = a.Left < b.Right - 0.01f && b.Left < a.Right - 0.01f && a.Top < b.Bottom - 0.01f && b.Top < a.Bottom - 0.01f;
                            Assert.That(overlap, Is.False, at + $": pods {a} and {b} overlap");
                        }
                    }

                    // Under the booster row, the places above it, the board above them.
                    Assert.That(r.PodRow.Top, Is.GreaterThan(r.BoosterRow.Bottom), at);
                    Assert.That(r.BoosterRow.Top, Is.GreaterThan(r.SlotRow.Bottom), at);
                    Assert.That(r.RoundBoard.Bottom, Is.LessThanOrEqualTo(r.Slots[0].Top), at);
                }
            }
        }

        [Test]
        public void ADeepColumn_ShowsItsFirstThreePods_AndTheNextSlidesUpWhenTheTopLeaves()
        {
            // One column of six pods over a strip of six G cubes: the tray shows three, nothing for the other three.
            var rows = new[] { "########", "#......#", "#GGGGGG#", "#......#", "###.####" }.Reverse().ToArray();
            var pods = Enumerable.Range(0, 6).Select(_ => new RoundPod('G', 1)).ToArray();
            var level = new RoundLevel(1, 8, 5, rows, pods, new[] { new[] { 0, 1, 2, 3, 4, 5 } }, new[] { 3 });
            var play = new RoundPlay(level);
            ReferenceGameplayRegions r = ScreenLayout.RoundGameplay(1080f, 2340f, Insets.None, 1, 5);
            RoundAnimator a = play.Animator;
            Assert.That(a.Waiting(0), Is.EqualTo(6));
            int shown = Enumerable.Range(0, a.Waiting(0)).Count(r.Shows);
            Assert.That(shown, Is.EqualTo(3), "three of six pods are shown");
            Assert.That(a.Tubes(r.PodRows), Is.Empty);

            int second = a.At(0, 1);
            Assert.That(play.Commit(RoundAction.Tap(a.At(0, 0))).Refusal, Is.EqualTo(RoundRefusal.None));
            play.Advance(0.05, 1);
            Assert.That(a.At(0, 0), Is.EqualTo(second), "the pod under the top one comes up to the top row");
            Assert.That(a.DepthOf(second), Is.EqualTo(0));
            Assert.That(a.Waiting(0), Is.EqualTo(5));
        }

        [Test]
        public void NoBuildDrawsAPlusN_ForARoundColumn()
        {
            // The kit's round pod has no "+N" part; the hosts' round columns never draw one (the playtest's RoundPainter, Unity's TrayView.Round).
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (string file in new[]
            {
                Path.Combine(root, "..", "playtest", "design", "RoundPainter.cs"),
                Path.Combine(root, "Assets", "Bloomlings", "Gameplay", "Tray", "TrayView.Round.cs"),
            })
            {
                string source = File.ReadAllText(file);
                Assert.That(source, Does.Not.Contain("MoreBadge"), file);
                Assert.That(source, Does.Not.Contain("More(s)"), file);
                Assert.That(source, Does.Not.Contain("\"+\" +"), file);
            }
        }

        // ---- The pods' look ----

        private static (byte R, byte G, byte B, byte A) Pixel(byte[] pixels, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
        }

        private static float Luma((byte R, byte G, byte B, byte A) c) => (0.299f * c.R) + (0.587f * c.G) + (0.114f * c.B);

        private static int Chroma((byte R, byte G, byte B, byte A) c) => Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));

        [Test]
        public void APod_IsAnObliqueCube_NoBox_TheTopRowInAWhiteOutline_ItsNumberInTheMiddleOfItsTop()
        {
            Assert.That(AssetSlots.Has(RoundLook.PodCubeSlot), Is.True);
            const int size = 96;
            float m = size * RoundScreenLook.PodRingShare;
            int topBottom = (int)((size - m) - ((size - (2f * m)) * RoundScreenLook.CubeDepthShare));
            foreach (RoundMaterial material in RoundMaterial.All)
            {
                string at = material.Code.ToString();
                Rgba color = RoundLook.ColorOf(material);
                byte[] top = UiRaster.PodCube(size, color, null, false, RoundPodLook.Top);
                byte[] placed = UiRaster.PodCube(size, color, null, false, RoundPodLook.Placed);
                byte[] waiting = UiRaster.PodCube(size, color, null, false, RoundPodLook.Waiting);
                Assert.That(Pixel(top, size, 0, 0).A, Is.EqualTo(0), at + ": a clear corner, no box");
                Assert.That(Pixel(placed, size, 0, 0).A, Is.EqualTo(0), at);

                // The top row's white outline; none on a pod in a place.
                (byte R, byte G, byte B, byte A) ring = Pixel(top, size, 2, size / 2);
                Assert.That(ring.A, Is.GreaterThan(200), at + ": the top row's outline");
                Assert.That(Math.Min(ring.R, Math.Min(ring.G, ring.B)), Is.GreaterThan(225), at + ": a white outline");
                Assert.That(Pixel(placed, size, 2, size / 2).A, Is.EqualTo(0), at + ": no outline in a place");
                Assert.That(waiting, Is.Not.EqualTo(placed), at + ": a waiting pod is barely lighter, on a flatter base");

                // A top of nearly flat colour, a front of a deeper shade of the same hue.
                (byte R, byte G, byte B, byte A) face = Pixel(placed, size, size / 2, topBottom / 2);
                Rgba light = color.Lighten(0.06f);
                Assert.That(Math.Abs(face.R - light.R) + Math.Abs(face.G - light.G) + Math.Abs(face.B - light.B), Is.LessThan(40), at + ": the top is the material's colour");
                (byte R, byte G, byte B, byte A) front = Pixel(placed, size, size / 2, topBottom + 4);
                Assert.That(Luma(front), Is.LessThan(Luma(face)), at + ": the front is deeper than the top");
                Assert.That(Chroma(front), Is.GreaterThanOrEqualTo(Chroma(face) * 0.6f), at + ": never grey");
            }

            byte[] hidden = UiRaster.PodCube(size, RoundLook.ColorOf(0), null, true, RoundPodLook.Top);
            Assert.That(hidden, Is.EqualTo(UiRaster.PodCube(size, RoundLook.ColorOf(5), null, true, RoundPodLook.Top)), "a hidden pod tells nothing of its colour");
            Assert.That(UiRaster.PodCubeKey('G', true, RoundPodLook.Top, false), Is.EqualTo(UiRaster.PodCubeKey('B', true, RoundPodLook.Top, false)));
            Assert.That(UiRaster.PodCubeKey('G', false, RoundPodLook.Waiting, false), Is.Not.EqualTo(UiRaster.PodCubeKey('G', false, RoundPodLook.Top, false)));

            // The number big in the middle of the top face, on the top row and below it.
            var place = new Box(100f, 200f, 196f, 296f);
            foreach (bool deeper in new[] { false, true })
            {
                PodChip chip = PodChip.Cube(place, deeper);
                float front = deeper ? RoundScreenLook.WaitingDepthShare : RoundScreenLook.CubeDepthShare;
                float faceBottom = place.Top + (96f - m) - ((96f - (2f * m)) * front);
                Assert.That(chip.Count.CenterX, Is.EqualTo(place.CenterX).Within(0.01f));
                Assert.That(chip.Count.CenterY, Is.EqualTo((place.Top + m + faceBottom) / 2f).Within(0.01f), "centred on the top face");
                Assert.That(chip.Count.Height, Is.EqualTo(96f * RoundScreenLook.CountShare).Within(0.01f), "big");
            }

            Assert.That(PodChip.CubeCountLook(true).FillTop.A, Is.LessThan(PodChip.CubeCountLook(false).FillTop.A), "a waiting pod's number is half clear");
            Assert.That(PodChip.CubeCountLook(false), Is.EqualTo(PodChip.CountLook(false)), "the pods' count look of before: only its place and size are new");
            Assert.That(PodChip.CubeCountLook(true).OutlineEm, Is.EqualTo(PodChip.CountOutlineEm), "the same outline");
        }

        [Test]
        public void ABoardCube_IsTheObliqueCube_TouchingItsNeighbours_ItsFrontADeeperShade()
        {
            const int w = 34;
            int h = (int)Math.Round(w * (RoundScreenLook.BoardRowShare + RoundScreenLook.BoardFrontShare));
            foreach (RoundMaterial material in RoundMaterial.All)
            {
                string at = material.Code.ToString();
                byte[] cube = UiRaster.BoardCube(w, h, RoundLook.ColorOf(material), null);
                Assert.That(cube, Is.EqualTo(UiRaster.BoardCube(w, h, RoundLook.ColorOf(material), null)), "deterministic");
                Assert.That(Pixel(cube, w, 0, h / 3).A, Is.GreaterThan(127), at + ": no gap at the side, the cubes touch");
                (byte R, byte G, byte B, byte A) top = Pixel(cube, w, w / 2, h / 3);
                (byte R, byte G, byte B, byte A) front = Pixel(cube, w, w / 2, h - 4);
                Assert.That(Luma(front), Is.LessThan(Luma(top)), at + ": a visible front, deeper than the top");
                Assert.That(Chroma(front), Is.GreaterThanOrEqualTo(Chroma(top) * 0.6f), at + ": never grey");
            }

            // The specular dot stands at the top's lower right.
            byte[] green = UiRaster.BoardCube(w, h, RoundLook.ColorOf('G'), null);
            int bestX = 0;
            int bestY = 0;
            float best = 0f;
            int faceBottom = (int)(w * RoundScreenLook.BoardRowShare);
            for (int y = 4; y < faceBottom; y++)
            {
                for (int x = 4; x < w - 4; x++)
                {
                    float l = Luma(Pixel(green, w, x, y));
                    if (l > best)
                    {
                        best = l;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            Assert.That(bestX, Is.GreaterThan(w / 2), "the gloss is on the right");
            Assert.That(bestY, Is.GreaterThan(faceBottom / 2), "low on the top");
        }

        [Test]
        public void SideFaces_GrowFromTheMiddleOutward_OnTheSideAwayFromIt_InFewWidths()
        {
            Assert.That(AssetSlots.Has(RoundLook.SideSlot), Is.True);
            Assert.That(RoundLook.SideStep(0f, 100f), Is.EqualTo(0), "none in the middle");
            Assert.That(RoundLook.SideStep(-100f, 100f), Is.EqualTo(-RoundScreenLook.SideSteps), "the left side at the left edge");
            Assert.That(RoundLook.SideStep(100f, 100f), Is.EqualTo(RoundScreenLook.SideSteps));
            Assert.That(RoundLook.SideStep(250f, 100f), Is.EqualTo(RoundScreenLook.SideSteps), "never beyond the outermost");
            Assert.That(RoundLook.SideWidth(34f, RoundScreenLook.SideSteps), Is.EqualTo(34f * RoundScreenLook.SideShare).Within(1e-4), "0.10 of a cube at the edge");
            Assert.That(RoundLook.SideWidth(34f, 2), Is.LessThan(RoundLook.SideWidth(34f, 4)), "linear outward");

            RoundLevel level = Square(14);
            RoundBoardLayout board = RoundBoardLayout.Fit(new Box(0f, 0f, 1000f, 1000f), level);
            int row = 6 * level.Width;
            int leftmost = row + 1;
            int rightmost = row + level.Width - 2;
            Assert.That(board.SideStepOf(leftmost), Is.EqualTo(-RoundScreenLook.SideSteps));
            Assert.That(board.SideStepOf(rightmost), Is.EqualTo(RoundScreenLook.SideSteps));
            Assert.That(board.SideNeighbour(leftmost), Is.EqualTo(leftmost - 1), "a left cube's side shows where its left neighbour is empty");
            Box side = board.SideBox(leftmost);
            Box body = board.CubeBody(leftmost);
            Assert.That(side.Right, Is.GreaterThan(body.Left).And.LessThan(body.CenterX), "beside the cube, tucked under it");
            Assert.That(side.Left, Is.EqualTo(body.Left - (body.Width * RoundScreenLook.SideShare)).Within(1e-3));
            Assert.That(board.SideBox(row + (level.Width / 2)).Width, Is.LessThan(board.SideBox(leftmost).Width), "narrower toward the middle");

            // The side's shade lies between the top's and the front's; the left side lighter than the right.
            Rgba orange = RoundLook.ColorOf('O');
            byte[] leftFace = UiRaster.SideFace(8, 40, orange, true, 0.7f);
            byte[] rightFace = UiRaster.SideFace(8, 40, orange, false, 0.7f);
            Assert.That(Luma(Pixel(leftFace, 8, 3, 10)), Is.GreaterThan(Luma(Pixel(rightFace, 8, 4, 10))));
            byte[] cube = UiRaster.BoardCube(34, 43, orange, null);
            float topLuma = Luma(Pixel(cube, 34, 17, 12));
            float frontLuma = Luma(Pixel(cube, 34, 17, 40));
            Assert.That(Luma(Pixel(leftFace, 8, 3, 10)), Is.LessThan(topLuma).And.GreaterThan(frontLuma), "between the top and the front");
        }

        [Test]
        public void TheBoardsCells_AreObliqueRows_WiderThanTall_TheNearestFrontUnderTheLastRow()
        {
            RoundLevel level = Square(14);
            RoundBoardLayout board = RoundBoardLayout.Fit(new Box(0f, 0f, 1000f, 1000f), level);
            Assert.That(board.RowPitch, Is.EqualTo(board.Cell * RoundScreenLook.BoardRowShare).Within(1e-3));
            Assert.That(board.Front, Is.EqualTo(board.Cell * RoundScreenLook.BoardFrontShare).Within(1e-3));
            Assert.That(board.Grid.Height, Is.EqualTo((board.Rows * board.RowPitch) + board.Front).Within(1e-2), "the rows and the nearest front");
            Assert.That(board.Grid.Width, Is.EqualTo(board.Columns * board.Cell).Within(1e-2));
            Assert.That(board.Grid.Within(board.Surface), Is.True);
            for (int y = 1; y < level.Height - 1; y++)
            {
                for (int x = 1; x < level.Width - 1; x++)
                {
                    int cell = (y * level.Width) + x;
                    Box top = board.CellBox(cell);
                    Assert.That(top.Height, Is.EqualTo(board.RowPitch).Within(1e-3), "a cell is a cube's top");
                    Assert.That(board.CellAt(top.CenterX, top.CenterY), Is.EqualTo(cell), "the hit test maps back");
                    (float px, float py) = board.Point(cell);
                    Assert.That(px, Is.EqualTo(top.CenterX).Within(1e-3));
                    Assert.That(py, Is.EqualTo(top.CenterY).Within(1e-3), "the walkers walk on the tops");
                    Box picture = board.CubeBox(cell);
                    Assert.That(picture.Top, Is.EqualTo(top.Top).Within(1e-3));
                    Assert.That(picture.Height, Is.EqualTo(board.RowPitch + board.Front).Within(1e-3), "the picture is the top and its front");
                }
            }
        }

        [Test]
        public void WaitingColours_UpToTheReadableLighten_StayApartFromEveryOther()
        {
            // The palette's readability bound under normal vision (FR-047): CIEDE2000 at least 10 between two colours. Up to
            // RoundScreenLook.ReadableInactiveLighten every waiting colour keeps it from every other material's colour, waiting
            // or on top; the owner's InactiveLighten (the constructor's value) is reported against it, not refused.
            const double bound = 10.0;
            Assert.That(RoundScreenLook.InactiveLighten, Is.InRange(0f, 1f));
            Assert.That(RoundLook.WaitingColor(RoundLook.ColorOf(0)), Is.EqualTo(RoundLook.ColorOf(0).Lighten(RoundScreenLook.InactiveLighten)));
            IReadOnlyList<RoundMaterial> all = RoundMaterial.All;
            float k = RoundScreenLook.ReadableInactiveLighten;
            for (int i = 0; i < all.Count; i++)
            {
                Rgba top = RoundLook.ColorOf(all[i]);
                Rgba waiting = RoundLook.WaitingColor(top, k);
                Assert.That(waiting, Is.Not.EqualTo(top), all[i].Code + ": dimmer than the top row");
                for (int j = 0; j < all.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Rgba other = RoundLook.ColorOf(all[j]);
                    Assert.That(DeltaE(waiting, RoundLook.WaitingColor(other, k)), Is.GreaterThanOrEqualTo(bound), $"{all[i].Code}/{all[j].Code}: two waiting pods tell apart");
                    Assert.That(DeltaE(waiting, other), Is.GreaterThanOrEqualTo(bound), $"{all[i].Code} waiting / {all[j].Code} on top: never taken for another colour");
                }
            }

            // Just above it a pair merges: the bound is the largest that holds.
            bool merges = false;
            for (int i = 0; i < all.Count && !merges; i++)
            {
                for (int j = 0; j < all.Count && !merges; j++)
                {
                    merges = i != j && DeltaE(RoundLook.WaitingColor(RoundLook.ColorOf(all[i]), k + 0.04f), RoundLook.ColorOf(all[j])) < bound;
                }
            }

            Assert.That(merges, Is.True, "ReadableInactiveLighten is the largest readable lighten (within 0.04)");
        }

        [Test]
        public void ACubeTube_PlugsIntoTheCubesFrontFaces_UnderTheirTops()
        {
            ReferenceGameplayRegions r = ScreenLayout.RoundGameplay(1080f, 2340f, Insets.None, 4, 5);
            Box left = r.Pod(0, 0);
            Box right = r.Pod(2, 1);
            LinkTubeShape tube = LinkTube.BetweenCubes(left, RoundLook.ColorOf(0), right, RoundLook.ColorOf(8));
            Assert.That(tube.Start.X, Is.EqualTo(left.Right).Within(left.Height * 0.05f));
            Assert.That(tube.End.X, Is.EqualTo(right.Left).Within(right.Height * 0.05f));
            foreach ((Box cube, float y) in new[] { (left, tube.Start.Y), (right, tube.End.Y) })
            {
                float frontTop = cube.Bottom - (cube.Height * (UiRaster.CubeSide + UiRaster.CubeMargin));
                Assert.That(y, Is.InRange(frontTop, cube.Bottom), "on the cube's front face, under its top");
            }

            Assert.That(tube.Thickness, Is.LessThan(left.Height * 0.2f));
        }

        // ---- The board box ----

        [Test]
        public void TheBoardBox_IsBiggerThanBefore_SquareWhereItFits_AndTheSameOnEveryLevel()
        {
            foreach ((float w, float h) in Screens)
            {
                ReferenceGameplayRegions first = ScreenLayout.RoundGameplay(w, h, InsetsOf(h), 3, 5);
                Box box = first.RoundBoard;
                string at = w + "x" + h;
                (float top, float scale) = ScreenLayout.RoundFit(first.Safe, ScreenLayout.RoundGameplay(w, h, InsetsOf(h), 3, 5, true, true).Badge.Bottom);
                Assert.That(box.Top, Is.EqualTo(top).Within(0.01f), at);
                Assert.That(box.Width, Is.EqualTo(first.W * RoundScreenLook.BoardWidthShare * scale).Within(0.01f), at + ": the owner's width");
                Assert.That(box.Height, Is.EqualTo(ScreenLayout.RoundReferenceHeight(first.W) * RoundScreenLook.BoardHeightShare * scale).Within(0.01f), at + ": the owner's height");
                Assert.That(box.Top, Is.GreaterThanOrEqualTo(first.TopBar.Bottom), at + ": under the top bar");
                Assert.That(first.PodRow.Bottom, Is.LessThanOrEqualTo(first.Safe.Bottom + 0.01f), at + ": the pods fit above the bottom inset");
                if (h / w >= 2.1f)
                {
                    Assert.That(box.Width, Is.GreaterThan(first.W * 0.86f), at + ": wider than the 0.86 W box before 2026-10-28");
                }

                // The places' bar is as wide as the box.
                Box panel = GameplaySkin.SlotPanel(ScreenLayout.AlignRound(first, box));
                Assert.That(panel.Left, Is.EqualTo(box.Left).Within(0.5f), at);
                Assert.That(panel.Right, Is.EqualTo(box.Right).Within(0.5f), at);

                foreach (int stacks in new[] { 1, 2, 4, 6 })
                {
                    foreach (bool boosters in new[] { false, true })
                    {
                        foreach (bool badge in new[] { false, true })
                        {
                            ReferenceGameplayRegions r = ScreenLayout.RoundGameplay(w, h, InsetsOf(h), stacks, 6, boosters, badge);
                            Assert.That(r.RoundBoard, Is.EqualTo(box), $"{at}, {stacks} columns, boosters {boosters}, badge {badge}");
                            Assert.That(r.RoundBoard.Top, Is.GreaterThanOrEqualTo((badge ? r.Badge.Bottom : r.TopBar.Bottom) - 0.01f), at);
                        }
                    }
                }

                // Pictures from 12 × 12 to 48 × 48 cells stand in that box, the frame one thickness.
                RoundBoardLayout small = RoundBoardLayout.In(first, Square(12));
                RoundBoardLayout big = RoundBoardLayout.In(first, Square(48));
                Assert.That(small.Outer, Is.EqualTo(box), at);
                Assert.That(big.Outer, Is.EqualTo(box), at);
                Assert.That(big.FrameThickness, Is.EqualTo(small.FrameThickness), at);
                Assert.That(small.FrameThickness, Is.EqualTo(Math.Min(box.Width, box.Height) * RoundScreenLook.FrameShare).Within(1e-2), at + ": the owner's frame");
            }

            // On the owner's own phone (1080 x 2340, no insets) the box and the pods are exactly the constructor's.
            ReferenceGameplayRegions owner = ScreenLayout.RoundGameplay(1080f, 2340f, Insets.None, 3, 5);
            Assert.That(owner.RoundBoard.Width, Is.EqualTo(1080f * RoundScreenLook.BoardWidthShare).Within(0.5f));
            Assert.That(owner.RoundBoard.Height, Is.EqualTo(2340f * RoundScreenLook.BoardHeightShare).Within(0.5f));
            Assert.That(owner.RoundBoard.Top, Is.EqualTo(2340f * RoundScreenLook.BoardTopShare).Within(0.5f));
            Assert.That(owner.Pod(0, 0).Width, Is.EqualTo(1080f * RoundScreenLook.PodCubeShare).Within(0.5f));
        }

        private static RoundLevel Square(int side)
        {
            var rows = new string[side];
            for (int y = 0; y < side; y++)
            {
                bool edge = y == 0 || y == side - 1;
                rows[y] = edge ? new string('#', side) : "#" + (y == 1 || y == side - 2 ? new string('.', side - 2) : "." + new string('G', side - 4) + ".") + "#";
            }

            rows[0] = rows[0].Substring(0, side / 2) + "." + rows[0].Substring((side / 2) + 1);
            int cubes = (side - 4) * (side - 4);
            return new RoundLevel(1, side, side, rows, new[] { new RoundPod('G', cubes) }, new[] { new[] { 0 } }, new[] { side / 2 });
        }

        // ---- CIEDE2000 (sRGB, D65), as the readability gate measures the palette ----

        private static double DeltaE(Rgba a, Rgba b)
        {
            (double l1, double a1, double b1) = Lab(a);
            (double l2, double a2, double b2) = Lab(b);
            double c1 = Math.Sqrt((a1 * a1) + (b1 * b1));
            double c2 = Math.Sqrt((a2 * a2) + (b2 * b2));
            double cb = (c1 + c2) / 2;
            double g = 0.5 * (1 - Math.Sqrt(Math.Pow(cb, 7) / (Math.Pow(cb, 7) + Math.Pow(25, 7))));
            double a1p = (1 + g) * a1;
            double a2p = (1 + g) * a2;
            double c1p = Math.Sqrt((a1p * a1p) + (b1 * b1));
            double c2p = Math.Sqrt((a2p * a2p) + (b2 * b2));
            double h1 = Hue(b1, a1p);
            double h2 = Hue(b2, a2p);
            double dl = l2 - l1;
            double dc = c2p - c1p;
            double dh = 0;
            if (c1p * c2p != 0)
            {
                dh = h2 - h1;
                dh = dh > 180 ? dh - 360 : dh < -180 ? dh + 360 : dh;
            }

            double dhh = 2 * Math.Sqrt(c1p * c2p) * Math.Sin(Rad(dh / 2));
            double lb = (l1 + l2) / 2;
            double cbp = (c1p + c2p) / 2;
            double hb = c1p * c2p == 0 ? h1 + h2 : Math.Abs(h1 - h2) <= 180 ? (h1 + h2) / 2 : h1 + h2 < 360 ? (h1 + h2 + 360) / 2 : (h1 + h2 - 360) / 2;
            double t = 1 - (0.17 * Math.Cos(Rad(hb - 30))) + (0.24 * Math.Cos(Rad(2 * hb))) + (0.32 * Math.Cos(Rad((3 * hb) + 6))) - (0.2 * Math.Cos(Rad((4 * hb) - 63)));
            double dth = 30 * Math.Exp(-Math.Pow((hb - 275) / 25, 2));
            double rc = 2 * Math.Sqrt(Math.Pow(cbp, 7) / (Math.Pow(cbp, 7) + Math.Pow(25, 7)));
            double sl = 1 + (0.015 * Math.Pow(lb - 50, 2) / Math.Sqrt(20 + Math.Pow(lb - 50, 2)));
            double sc = 1 + (0.045 * cbp);
            double sh = 1 + (0.015 * cbp * t);
            double rt = -Math.Sin(Rad(2 * dth)) * rc;
            return Math.Sqrt(Math.Pow(dl / sl, 2) + Math.Pow(dc / sc, 2) + Math.Pow(dhh / sh, 2) + (rt * (dc / sc) * (dhh / sh)));
        }

        private static double Rad(double degrees) => degrees * Math.PI / 180;

        private static double Hue(double b, double a)
        {
            double h = Math.Atan2(b, a) * 180 / Math.PI;
            return h < 0 ? h + 360 : h;
        }

        private static (double L, double A, double B) Lab(Rgba c)
        {
            static double Linear(byte v)
            {
                double x = v / 255.0;
                return x <= 0.04045 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
            }

            double r = Linear(c.R);
            double g = Linear(c.G);
            double b = Linear(c.B);
            double x = ((0.4124 * r) + (0.3576 * g) + (0.1805 * b)) / 0.95047;
            double y = (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
            double z = ((0.0193 * r) + (0.1192 * g) + (0.9505 * b)) / 1.08883;
            static double F(double v) => v > 0.008856 ? Math.Pow(v, 1.0 / 3.0) : (7.787 * v) + (16.0 / 116.0);
            return ((116 * F(y)) - 16, 500 * (F(x) - F(y)), 200 * (F(y) - F(z)));
        }
    }
}
