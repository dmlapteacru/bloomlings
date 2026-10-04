using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The reference layouts (spec 005 FR-020 to FR-025, contracts/look.md §6) across phone shapes: every region in its
    /// screen order inside the safe area, nothing a finger presses overlapping, and the measured fractions kept on a
    /// 19.5:9 phone.
    /// </summary>
    public class ReferenceLayoutTests
    {
        /// <summary>16:9 to 21:9 phones, with typical status and navigation bar insets, and the reference's own shape.</summary>
        private static IEnumerable<(float W, float H, Insets Insets)> Phones()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 20f / 9f, 21f / 9f })
            {
                yield return (1080f, 1080f * ratio, new Insets(ratio > 2f ? 110f : 60f, ratio > 2f ? 60f : 0f));
                yield return (720f, 720f * ratio, Insets.None);
            }

            yield return (1080f, 1080f * ScreenLayout.ReferenceAspect, Insets.None);
        }

        private static float Touch(float w, float h) => DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(w, h);

        /// <summary>A button's touch box as the kit grows it (<c>Kit.Touch</c>, <c>UiKit</c>): at least the touch minimum.</summary>
        private static Box TouchBox(Box box, float min) => Box.FromCenter(box.CenterX, box.CenterY, Math.Max(box.Width, min), Math.Max(box.Height, min));

        [Test]
        public void Gameplay_KeepsTheReferenceOrder_InsideTheSafeArea()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (bool boosters in new[] { true, false })
                {
                    foreach (bool badge in new[] { false, true })
                    {
                        foreach (int stacks in new[] { 2, 3, 4, 5 })
                        {
                            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, insets, stacks, 5, boosters, badge);
                            string at = w + "x" + h + " boosters=" + boosters + " badge=" + badge + " stacks=" + stacks;
                            AssertOrdered(r.Ordered, r.Safe, at);
                            Assert.That(r.K, Is.InRange(0.8f, 1f), at);
                            Assert.That(r.TopBar.Top, Is.GreaterThanOrEqualTo(r.Safe.Top), at + ": the top bar sits under the top inset");
                            Assert.That(r.Board.Width, Is.LessThanOrEqualTo((r.W * ReferenceGameplayRegions.MaxBoardShare) + 0.5f), at);
                            // The tray keeps the reference's size on 19.5:9 and shrinks by k on shorter phones (k = 0.86 at
                            // 16:9), so the board keeps at least a third of the height even with four rows of pods and a badge.
                            Assert.That(r.Board.Height, Is.GreaterThan(r.Safe.Height * 0.33f), at + ": the board keeps its room");
                            Assert.That(r.Tray.Bottom, Is.EqualTo(h).Within(0.5f), at + ": the tray runs to the screen's bottom");
                            Assert.That(r.Tray.Left, Is.EqualTo(0f).Within(0.5f), at);
                            Assert.That(r.Tray.Right, Is.EqualTo(w).Within(0.5f), at);
                            Assert.That(r.TrayContent.Bottom, Is.LessThanOrEqualTo(r.Safe.Bottom), at + ": the tray's content stays above the bottom inset");
                            Assert.That(r.SlotRow.Top, Is.GreaterThan(r.Tray.Top), at);
                            Assert.That(r.EntryStrip.Bottom, Is.EqualTo(r.Tray.Top).Within(0.5f), at);
                            Assert.That(r.EntryStrip.Height, Is.EqualTo(ReferenceGameplayRegions.EntryStripShare * r.W * r.K).Within(0.5f), at + ": a thin strip of lawn, no arch's room");

                            Assert.That(r.Slots.Count, Is.EqualTo(5), at);
                            AssertRow(r.Slots, r.SlotRow, at + " slots");
                            AssertRow(r.Columns, r.PodRow, at + " columns");
                            Assert.That(r.Columns.Count, Is.EqualTo(stacks), at);
                            bool tall = r.Safe.Height / r.W >= ReferenceGameplayRegions.FourRowsAspect;
                            Assert.That(r.PodRows, Is.EqualTo(tall ? 4 : 3), at + ": four rows of pods on tall phones, three on short ones");
                            AssertGrid(r, at);
                            if (boosters)
                            {
                                Assert.That(r.Boosters.Count, Is.EqualTo(4), at);
                                AssertRow(r.Boosters, r.BoosterRow, at + " boosters");
                                Assert.That(r.SeparatorTop.Bottom, Is.LessThanOrEqualTo(r.BoosterRow.Top), at);
                                Assert.That(r.SeparatorTop.Top, Is.GreaterThanOrEqualTo(r.SlotRow.Bottom), at);
                                Assert.That(r.SeparatorBottom.Bottom, Is.LessThanOrEqualTo(r.PodRow.Top), at);
                                Assert.That(r.SeparatorBottom.Top, Is.GreaterThanOrEqualTo(r.BoosterRow.Bottom), at);
                                for (int i = 0; i < 4; i++)
                                {
                                    Assert.That(r.BoosterBadge(i).Overlaps(r.Boosters[i]), Is.True, at + ": the badge sits on its box");
                                }
                            }
                            else
                            {
                                Assert.That(r.Boosters.Count, Is.EqualTo(0), at);
                                Assert.That(r.BoosterRow.IsEmpty, Is.True, at);
                                Assert.That(r.SeparatorBottom.IsEmpty, Is.True, at);
                            }

                            Assert.That(r.Badge.IsEmpty, Is.EqualTo(!badge), at);

                            // Pause, the speed pill, the booster boxes and the exposed pods are pressed: their touch boxes
                            // stay inside the safe area and never overlap (an exposed pod's grows to the touch minimum,
                            // over the waiting pod under it, which takes no taps).
                            float touch = Touch(w, h) * 0.95f;
                            var targets = new List<Box> { TouchBox(r.Pause, touch), TouchBox(r.Speed, touch) };
                            targets.AddRange(r.Boosters);
                            for (int s = 0; s < stacks; s++)
                            {
                                targets.Add(TouchBox(r.Pod(s, 0), touch));
                            }

                            AssertTargets(targets, r.Safe, touch, at);
                        }
                    }
                }
            }
        }

        [Test]
        public void Gameplay_MatchesTheMeasuredFractions_OnTheReferenceShape()
        {
            float w = 1080f;
            float h = w * ScreenLayout.ReferenceAspect;
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, Insets.None, 4, 5);
            Assert.That(r.K, Is.EqualTo(1f).Within(0.001f));
            Assert.That(r.Pause.Left, Is.EqualTo(0.04f * w).Within(1f));
            Assert.That(r.Pause.Width, Is.EqualTo(0.13f * w).Within(1f));
            Assert.That(r.Sign.Width, Is.EqualTo(0.42f * w).Within(1f));
            Assert.That(r.Speed.Right, Is.EqualTo(0.96f * w).Within(1f));
            Assert.That(r.Board.Top, Is.EqualTo(0.162f * w).Within(1f));
            Assert.That(r.EntryStrip.Height, Is.EqualTo(0.04f * w).Within(1f), "the plain strip: the Garden Entries have no arch");
            Assert.That(r.Board.Bottom, Is.EqualTo(r.Tray.Top - (0.04f * w)).Within(1f), "the board's region reaches down to it");
            Assert.That(r.SlotRow.Height, Is.EqualTo(0.16f * w).Within(1f));
            Assert.That(r.BoosterRow.Height, Is.EqualTo(0.18f * w).Within(1f));
            Assert.That(r.PodRows, Is.EqualTo(4));
            Assert.That(r.PodRow.Height, Is.EqualTo(0.46f * w).Within(1f), "the exposed pod 0.13 W and three more 0.1 W, 0.01 W apart");
            Assert.That(r.Slots[0].Width, Is.EqualTo(0.14f * w).Within(1f));
            Assert.That(r.Slots[0].Left, Is.EqualTo(0.04f * w).Within(1f), "the slots span 0.92 W");
            Assert.That(r.Slots[4].Right, Is.EqualTo(0.96f * w).Within(1f));
            Assert.That(r.Boosters[0].Width, Is.EqualTo(0.16f * w).Within(1f));
            Assert.That(r.Boosters[0].Left, Is.EqualTo(0.05f * w).Within(1f), "the boosters span 0.9 W");
            Assert.That(r.Boosters[3].Right, Is.EqualTo(0.95f * w).Within(1f));
            Assert.That(r.Columns[0].Width, Is.EqualTo(0.228f * w).Within(1f));
            Assert.That(r.Columns[0].Left, Is.EqualTo(0.02f * w).Within(1f), "four columns span 0.96 W");
            Assert.That(r.Columns[3].Right, Is.EqualTo(0.98f * w).Within(1f));
            Assert.That(r.Pod(0, 0).Height, Is.EqualTo(0.13f * w).Within(1f));
            Assert.That(r.Pod(0, 1).Height, Is.EqualTo(0.1f * w).Within(1f));
            Assert.That(r.Tray.Top, Is.EqualTo(h - (0.905f * w)).Within(2f), "the tray holds 0.905 W of rows and padding");
        }

        [Test]
        public void Gameplay_ExtraSlotNarrowsThePlates_AndSixStacksFit()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                string at = w + "x" + h;
                ReferenceGameplayRegions six = ScreenLayout.ReferenceGameplay(w, h, insets, 4, 6);
                Assert.That(six.Slots.Count, Is.EqualTo(6), at);
                AssertRow(six.Slots, six.SlotRow, at + " six slots");
                // The most stacks a level has (SourceTray.MaxStacks) still make one row of columns.
                ReferenceGameplayRegions many = ScreenLayout.ReferenceGameplay(w, h, insets, 6, 5);
                Assert.That(many.Columns.Count, Is.EqualTo(6), at);
                AssertRow(many.Columns, many.PodRow, at + " six columns");
                AssertGrid(many, at + " six stacks");
            }
        }

        [Test]
        public void Gameplay_TheBoardTakesItsWholeRoom_AndTheEntriesNone()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach ((int bw, int bh) in new[] { (9, 12), (6, 6), (12, 16), (10, 8) })
                {
                    string at = w + "x" + h + " " + bw + "x" + bh;
                    ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, insets, 4, 5);
                    BoardLayout layout = r.FitBoard(bw, bh);
                    float max = r.W * ReferenceGameplayRegions.MaxBoardShare;
                    Assert.That(layout.Outer.Width, Is.LessThanOrEqualTo(max + 1f), at);
                    Assert.That(layout.Outer.Top, Is.GreaterThanOrEqualTo(r.Board.Top - 1f), at);
                    Assert.That(layout.Outer.Bottom, Is.LessThanOrEqualTo(r.Tray.Top + 1f), at + ": the border stays on the lawn");
                    Assert.That(layout.Outer.CenterX, Is.EqualTo(r.Safe.CenterX).Within(1f), at);

                    // No arch takes room any more: the border reaches the width cap or the whole height of the board's lawn.
                    bool full = Math.Abs(layout.Outer.Width - max) <= 1f || Math.Abs(layout.Outer.Height - r.BoardArea.Height) <= 1f;
                    Assert.That(full, Is.True, at + ": the board fills its room");
                }
            }
        }

        [Test]
        public void TheEntries_SetOffFromTheStoneBorder_BesideTheirCells()
        {
            BoardLayout layout = BoardLayout.Fit(new Box(0f, 0f, 1000f, 1400f), 9, 12);
            var entries = new[]
            {
                new EntryDef(new CellPos(4, 0), EntrySide.Bottom),
                new EntryDef(new CellPos(0, 6), EntrySide.Left),
                new EntryDef(new CellPos(8, 3), EntrySide.Right),
                new EntryDef(new CellPos(2, 11), EntrySide.Top),
            };
            foreach (EntryDef entry in entries)
            {
                Box cell = layout.CellBox(entry.Cell.X, entry.Cell.Y);
                (float x, float y) = layout.Door(entry);
                string at = entry.Side.ToString();
                Assert.That(Inside(layout.Outer, x, y), Is.True, at + ": on the stone border");
                Assert.That(Inside(layout.Grid, x, y), Is.False, at + ": outside the cells");
                bool across = entry.Side == EntrySide.Left || entry.Side == EntrySide.Right;
                Assert.That(across ? y : x, Is.EqualTo(across ? cell.CenterY : cell.CenterX).Within(0.01f), at + ": beside its cell");
            }

            (float bx, float by) = layout.Door(entries[0]);
            Assert.That(by - layout.Grid.Bottom, Is.EqualTo(layout.Cell * (BoardLayout.Gap + (BoardLayout.Stone / 2f))).Within(0.01f), "the middle of the border");
            Assert.That(bx, Is.EqualTo(layout.CellBox(4, 0).CenterX).Within(0.01f));
        }

        private static bool Inside(Box box, float x, float y) => x > box.Left && x < box.Right && y > box.Top && y < box.Bottom;

        [Test]
        public void ThePods_GoOneAfterAnother_NeverOnEachOther()
        {
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, 2340f, Insets.None, 4, 5);
            for (int depth = 1; depth < r.PodRows; depth++)
            {
                Assert.That(r.Pod(0, depth).Top, Is.EqualTo(r.Pod(0, depth - 1).Bottom + r.PodGap).Within(0.5f), "depth " + depth + " under the one before");
                Assert.That(r.Pod(0, depth).Left, Is.EqualTo(r.Pod(0, 0).Left).Within(0.01f), "one column");
            }

            Assert.That(r.Pod(0, r.PodRows - 1).Bottom, Is.EqualTo(r.PodRow.Bottom).Within(0.5f), "the last row ends the pod row");
            Assert.That(r.Shows(r.PodRows - 1), Is.True);
            Assert.That(r.Shows(r.PodRows), Is.False, "deeper pods are counted on the badge, not drawn");

            PodChip chip = r.Chip(0, 1);
            Box place = r.Pod(0, 1);
            Assert.That(chip.Frame.Within(place.Inset(-0.01f)), Is.True, "the frame stays in its place");
            Assert.That(chip.Frame.Width, Is.EqualTo(chip.Frame.Height * PodChip.Aspect).Within(0.01f), "a frame a little wider than tall");
            Assert.That(chip.Frame.CenterX, Is.EqualTo(place.CenterX).Within(0.01f), "centered in its column");
            Assert.That(chip.Tile.Width, Is.EqualTo(chip.Tile.Height).Within(0.01f), "a square tile");
            Assert.That(chip.Tile.CenterX, Is.EqualTo(chip.Inner.CenterX).Within(0.01f), "the icon first: over the panel's middle");
            Assert.That(chip.Tile.CenterY, Is.EqualTo(chip.Inner.CenterY).Within(0.01f));
            Assert.That(chip.Tile.Within(chip.Frame), Is.True, "the tile stays inside the frame");
            Assert.That(chip.Tile.Within(chip.Icon), Is.True, "the owner's icon reaches a little beyond the tile");
            Assert.That(chip.Count.Height, Is.EqualTo(chip.Frame.Height * PodChip.CountShare).Within(0.01f), "small digits");
            Assert.That(chip.Count.CenterX, Is.GreaterThan(chip.Inner.CenterX), "the count at the right");
            Assert.That(chip.Count.CenterY, Is.GreaterThan(chip.Inner.CenterY), "the count at the bottom");
            Assert.That(chip.Inner.Contains(chip.Count.CenterX, chip.Count.CenterY), Is.True, "the count's middle on the panel");
            Assert.That(chip.Badge.Overlaps(chip.Frame), Is.True, "the +N disc sits on the frame's corner");
            Assert.That(chip.Badge.CenterX, Is.LessThan(chip.Inner.CenterX), "the +N disc at the top left, away from the count");
            Assert.That(chip.Badge.CenterY, Is.LessThan(chip.Inner.CenterY));
        }

        [Test]
        public void AFramePlace_NarrowerThanItsAspect_KeepsItsWidth()
        {
            var place = new Box(10f, 20f, 110f, 100f);
            PodChip chip = PodChip.In(place);
            Assert.That(chip.Frame.Left, Is.EqualTo(place.Left).Within(0.01f));
            Assert.That(chip.Frame.Right, Is.EqualTo(place.Right).Within(0.01f));
            Assert.That(chip.Frame.Height, Is.EqualTo(place.Height).Within(0.01f));
        }

        [Test]
        public void ThePodCount_IsDarkInAWhiteOutline_SofterWhenDimmed()
        {
            TextLook bright = PodChip.CountLook(dim: false);
            TextLook dim = PodChip.CountLook(dim: true);
            Assert.That(bright.FillTop, Is.EqualTo(DesignTokens.Colors.InkBrown));
            Assert.That(bright.Outline, Is.EqualTo(Rgba.White));
            Assert.That(bright.OutlineEm, Is.EqualTo(PodChip.CountOutlineEm));
            Assert.That(dim.FillTop, Is.Not.EqualTo(bright.FillTop));
            Assert.That(dim.Outline, Is.EqualTo(Rgba.White));
        }

        [Test]
        public void TheJamCard_IsCentered_AndHoldsItsChoices()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (int choices in new[] { 1, 2, 3, 4, 5 })
                {
                    foreach (bool close in new[] { true, false })
                    {
                        JamCardRegions r = ScreenLayout.JamCard(w, h, insets, choices, close);
                        string at = w + "x" + h + " choices=" + choices + " close=" + close;
                        Assert.That(r.Card.Within(r.Safe), Is.True, at + ": the card " + r.Card + " stays on screen");
                        Assert.That(r.Card.Width, Is.EqualTo(0.92f * r.W).Within(0.5f), at);
                        Assert.That(r.Card.CenterX, Is.EqualTo(r.Safe.CenterX).Within(0.5f), at);
                        Assert.That(r.Scale, Is.InRange(0.5f, 1f), at);
                        if (r.Scale >= 1f)
                        {
                            Assert.That(r.Card.CenterY, Is.EqualTo(r.Safe.Top + (r.Safe.Height * 0.51f)).Within(1f), at + ": centered at 51% of the height");
                        }

                        AssertOrdered(r.Ordered, r.Card, at);
                        Assert.That(r.Choices.Count, Is.EqualTo(choices), at);
                        for (int i = 0; i < choices; i++)
                        {
                            Assert.That(r.Choices[i].Within(r.Card), Is.True, at + " choice " + i);
                            Assert.That(r.CostPills[i].Within(r.Card), Is.True, at + " pill " + i);
                            Assert.That(r.CostPills[i].Top, Is.LessThan(r.Choices[i].Bottom), at + ": the pill overlaps its button's bottom edge");
                            Assert.That(r.CostPills[i].Bottom, Is.GreaterThan(r.Choices[i].Bottom), at + ": the pill hangs under its button");
                            Assert.That(r.CostPills[i].CenterX, Is.EqualTo(r.Choices[i].CenterX).Within(0.5f), at);
                            for (int j = 0; j < i; j++)
                            {
                                Assert.That(r.Choices[i].Overlaps(r.Choices[j]), Is.False, at + " choices " + j + " and " + i);
                                Assert.That(r.Choices[i].Overlaps(r.CostPills[j]), Is.False, at + " choice " + i + " and pill " + j);
                            }
                        }

                        if (choices % 2 == 1)
                        {
                            Assert.That(r.Choices[choices - 1].CenterX, Is.EqualTo(r.Safe.CenterX).Within(0.5f), at + ": an odd last choice is centered");
                        }

                        Assert.That(r.Close.IsEmpty, Is.EqualTo(!close), at);
                        if (close)
                        {
                            Assert.That(r.Close.Within(r.Safe), Is.True, at);
                            Assert.That(r.Close.Overlaps(r.Card), Is.True, at + ": the close button sits over the card's corner");
                            Assert.That(r.Close.Overlaps(r.Title), Is.False, at);
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            (Box tile, Box count) = r.WellCell(i, 5);
                            Assert.That(tile.Within(r.Well), Is.True, at + " well tile " + i);
                            Assert.That(count.Within(r.Well), Is.True, at + " well count " + i);
                        }
                    }
                }
            }
        }

        [Test]
        public void TheWinScreen_StacksSignPictureHeroRewardAndNext()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                WinRegions r = ScreenLayout.WinScreen(w, h, insets);
                string at = w + "x" + h;
                foreach (Box box in new[] { r.Sign, r.Picture, r.Hero, r.Pedestal, r.Reward, r.Double, r.Drop, r.Next })
                {
                    Assert.That(box.Within(r.Safe), Is.True, at + ": " + box + " leaves the safe area");
                    Assert.That(box.IsEmpty, Is.False, at);
                }

                Assert.That(r.Sign.Bottom, Is.LessThanOrEqualTo(r.Picture.Top), at);
                Assert.That(r.Hero.Top, Is.LessThan(r.Picture.Bottom), at + ": the hero overlaps the picture's foot");
                Assert.That(r.Hero.Bottom, Is.GreaterThan(r.Pedestal.Top), at + ": the hero stands on the pedestal");
                Assert.That(r.Reward.Overlaps(r.Pedestal), Is.True, at + ": the reward pill sits on the pedestal's front");
                Assert.That(r.Next.Top, Is.GreaterThanOrEqualTo(r.Reward.Bottom), at);
                Assert.That(r.Double.Overlaps(r.Reward), Is.False, at);
                Assert.That(r.Drop.Overlaps(r.Reward), Is.False, at);
                Assert.That(r.Picture.Width, Is.LessThanOrEqualTo(0.8f * r.W + 0.5f), at);
                Assert.That(r.RaysX, Is.EqualTo(r.Hero.CenterX).Within(0.5f), at);
                float touch = Touch(w, h) * 0.95f;
                AssertTargets(new List<Box> { TouchBox(r.Pause, touch), TouchBox(r.Double, touch), r.Next }, r.Safe, touch, at);
                Assert.That(r.Pause.Overlaps(r.Sign), Is.False, at + ": Pause stays clear of the sign");

                // The hero's feet stand on the middle of the pedestal's top (not its back rim).
                (float topY, float ry) = HomeStage.PedestalTop(r.Pedestal);
                float feet = r.Hero.Top + (r.Hero.Height * HomeStage.FeetShare);
                Assert.That(feet, Is.InRange(topY - (ry * 0.5f), topY + (ry * 0.5f)), at + ": the feet on the pedestal's middle");

                // The owner's win picture, drawn from the top, puts its own stone disc under the pedestal's top.
                var screen = new Box(0f, 0f, w, h);
                Box picture = OwnerPictures.TopAnchored(screen, 852, 1846, topY, OwnerPictures.WinStageShare);
                Assert.That(picture.Top, Is.EqualTo(0f).Within(0.01f), at);
                Assert.That(picture.Width, Is.GreaterThanOrEqualTo(w - 0.5f), at + ": it covers the screen's width");
                Assert.That(picture.Bottom, Is.GreaterThanOrEqualTo(h - 0.5f), at + ": and its height");
                Assert.That(picture.Top + (picture.Height * OwnerPictures.WinStageShare), Is.EqualTo(topY).Within(1f), at + ": its disc under the pedestal's top");
                Assert.That(picture.CenterX, Is.EqualTo(screen.CenterX).Within(0.5f), at);
            }
        }

        [Test]
        public void TheSlotTile_FillsTheReferenceShareOfItsPlate_AndLeavesRoomForTheCount()
        {
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, 2160f, Insets.None, 4, 5);
            Box plate = r.Slots[0];
            Box tile = ReferenceGameplayRegions.SlotTile(plate);
            float lip = Math.Min(plate.Width, plate.Height) * ReferenceGameplayRegions.SlotLipShare;
            float face = plate.Height - lip;
            Assert.That(tile.Width, Is.EqualTo(tile.Height).Within(0.01f));
            Assert.That(tile.Width, Is.EqualTo(Math.Min(plate.Width * 0.74f, face * 0.66f)).Within(0.5f));
            Assert.That(tile.Top, Is.EqualTo(plate.Top + (face * 0.08f)).Within(0.5f));
            Assert.That(tile.CenterX, Is.EqualTo(plate.CenterX).Within(0.5f));
            Assert.That(plate.Bottom - lip - tile.Bottom, Is.GreaterThan(face * 0.2f), "the count keeps a quarter of the face");
            Assert.That(tile.Width, Is.GreaterThan(0.09f * r.W), "about 0.1 W (the slots shrank so the tray shows its pods' rows)");
        }

        [Test]
        public void TwoOrThreeColumns_StayTogether_AtTheirWidest()
        {
            foreach (int stacks in new[] { 2, 3 })
            {
                ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, 1080f * ScreenLayout.ReferenceAspect, Insets.None, stacks, 5);
                for (int i = 0; i < stacks; i++)
                {
                    Assert.That(r.Columns[i].Width, Is.EqualTo(ReferenceGameplayRegions.PodMaxShare * r.W).Within(1f), stacks + " columns");
                }

                for (int i = 1; i < stacks; i++)
                {
                    Assert.That(r.Columns[i].Left - r.Columns[i - 1].Right, Is.LessThanOrEqualTo((0.04f * r.W) + 0.5f), stacks + " columns sit together");
                }

                Assert.That((r.Columns[0].Left + r.Columns[stacks - 1].Right) / 2f, Is.EqualTo(r.Safe.CenterX).Within(0.5f), stacks + " columns centered");
            }
        }

        [Test]
        public void ThePetalsPill_FitsItsAmount_TheLotusInside_TheAmountRightAfterIt()
        {
            // Home's pill box (0.38 W × 0.095 W at 1080) and amounts from "0" to "12 345" (about 0.3 to 1.8 of its height).
            var box = new Box(650f, 40f, 1060f, 142.6f);
            float h = box.Height;
            foreach (bool plus in new[] { false, true })
            {
                float reach = plus ? h * PetalsPillParts.PlusOut : 0f;
                PetalsPillParts zero = PetalsPillParts.Fit(box, h * 0.3f, plus);
                PetalsPillParts large = PetalsPillParts.Fit(box, h * 1.8f, plus);
                foreach (PetalsPillParts parts in new[] { zero, large })
                {
                    string at = "plus=" + plus + " width=" + parts.Pill.Width;
                    Assert.That(parts.Pill.Height, Is.EqualTo(h).Within(0.01f), at);
                    Assert.That(parts.Pill.Right + reach, Is.EqualTo(box.Right).Within(0.01f), at + ": the pill (and the \"+\") ends at the box's right end");

                    // The lotus inside the pill's left end: the picture's visible part (94% × 73% of its box) within the pill.
                    Box lotus = parts.Lotus;
                    Assert.That(lotus.Left + (lotus.Width * 0.03f), Is.GreaterThan(parts.Pill.Left + (h * 0.05f)), at + ": not over the left edge");
                    Assert.That(lotus.CenterY - (lotus.Height * 0.365f), Is.GreaterThan(parts.Pill.Top), at);
                    Assert.That(lotus.CenterY + (lotus.Height * 0.365f), Is.LessThan(parts.Face.Bottom), at);

                    // The amount starts right after the lotus whatever its length, never in the pill's middle.
                    Assert.That(parts.Amount.Left, Is.EqualTo(lotus.Right + (h * PetalsPillParts.AmountGap)).Within(0.01f), at);
                    Assert.That(parts.AmountSize, Is.EqualTo(h * PetalsPillParts.AmountShare).Within(0.01f), at);
                    if (plus)
                    {
                        Assert.That(parts.Plus.Left, Is.GreaterThan(parts.Amount.Right), at + ": the \"+\" after the amount");
                        Assert.That(parts.Plus.Right, Is.EqualTo(box.Right).Within(0.01f), at);
                    }
                    else
                    {
                        Assert.That(parts.Plus.IsEmpty, Is.True, at);
                    }
                }

                Assert.That(large.Amount.Left - large.Pill.Left, Is.EqualTo(zero.Amount.Left - zero.Pill.Left).Within(0.01f), "the same place after the lotus");
                Assert.That(zero.Amount.Width, Is.EqualTo(h * 0.3f).Within(0.01f), "a short amount gets a short pill");
                Assert.That(large.Amount.Width, Is.EqualTo(h * 1.8f).Within(0.01f));
                Assert.That(zero.Pill.Width, Is.LessThan(large.Pill.Width));
            }

            // A text engine that cannot measure yet: the whole box, the amount still after the lotus.
            PetalsPillParts unknown = PetalsPillParts.Fit(box, 0f, false);
            Assert.That(unknown.Pill.Left, Is.EqualTo(box.Left).Within(0.01f));
            Assert.That(unknown.Pill.Right, Is.EqualTo(box.Right).Within(0.01f));
            Assert.That(unknown.Amount.Left, Is.EqualTo(unknown.Lotus.Right + (h * PetalsPillParts.AmountGap)).Within(0.01f));

            // Longer than the box: the pill takes the whole box (the "+" reaching beyond it, as the reference's) before
            // its digits shrink. Centered (align 0.5): the pill and its "+" around the box's middle.
            PetalsPillParts huge = PetalsPillParts.Fit(box, h * 9f, true);
            Assert.That(huge.Pill.Left, Is.EqualTo(box.Left).Within(0.01f));
            Assert.That(huge.Pill.Right, Is.EqualTo(box.Right).Within(0.01f));
            Assert.That(huge.Amount.Right, Is.LessThan(huge.Plus.Left), "the amount still ends before the \"+\"");
            PetalsPillParts centered = PetalsPillParts.Fit(box, h * 0.6f, true, 0.5f);
            Assert.That((centered.Pill.Left + centered.Plus.Right) / 2f, Is.EqualTo(box.CenterX).Within(0.01f));

            Assert.That(PetalsPillParts.WidthText(NumberText.Group(1240)), Is.EqualTo("0" + NumberText.Separator + "000"), "measured on zeros, so a count-up keeps its pill");
        }

        /// <summary>
        /// Home (spec 005 FR-024, FR-030): its regions in order inside the safe area; the plaque, Play and the teaser row
        /// above the bottom menu (Play's bottom and the free booster's touch box end a gap over its top, less the reserve);
        /// Play 0.15 H tall unless the plaque would rise above 60% of H, then shorter (never under 0.11 H nor the touch
        /// minimum); the Daily Challenge the right column's first side button; and every button, the bottom menu's places
        /// included, reachable and clear of the others.
        /// </summary>
        [Test]
        public void Home_FollowsTheReference_AndKeepsEveryButtonReachable()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (float reserve in new[] { 0f, 132f * DesignTokens.ScaleFor(w, h) })
                {
                    ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets, reserve);
                    string at = w + "x" + h + " reserve=" + reserve;
                    float touch = Touch(w, h);
                    float limit = r.NavTop - (BottomNav.GapShare * r.W) - reserve;
                    AssertOrdered(r.Ordered, r.Safe, at);
                    Assert.That(r.NavTop, Is.EqualTo(ScreenLayout.BottomNavTop(w, h, insets)).Within(0.01f), at);
                    Assert.That(r.Teaser.Bottom, Is.LessThanOrEqualTo(limit + 0.5f), at + ": the teaser above the bottom menu and the reserve");
                    Assert.That(TouchBox(r.FreeBooster, touch).Bottom, Is.LessThanOrEqualTo(limit + 0.5f), at + ": the free booster's touch box above them");
                    Assert.That(r.Play.Bottom, Is.LessThan(limit - (touch / 2f)), at + ": Play ends above the medallion's top with a gap");
                    Assert.That(r.Play.Bottom, Is.LessThanOrEqualTo(TouchBox(r.FreeBooster, touch).Top + 0.5f), at + ": Play clear of the free booster's touch box");
                    Assert.That(r.Diorama.Top, Is.GreaterThanOrEqualTo(r.Logo.Bottom), at);
                    Assert.That(r.Plaque.Top, Is.LessThan(r.Diorama.Bottom), at + ": the plaque stands at the diorama's foot");
                    Assert.That(r.Play.Width, Is.EqualTo(0.85f * r.W).Within(0.5f), at);
                    float sh = r.Safe.Height;
                    Assert.That(r.Play.Height, Is.InRange(Math.Max(ReferenceHomeRegions.PlayMinShare * sh, touch) - 0.5f, (ReferenceHomeRegions.PlayShare * sh) + 0.5f), at);
                    if (r.Play.Height < (ReferenceHomeRegions.PlayShare * sh) - 0.5f && r.Play.Height > Math.Max(ReferenceHomeRegions.PlayMinShare * sh, touch) + 0.5f)
                    {
                        Assert.That(r.Plaque.Top, Is.EqualTo(r.Safe.Top + (ReferenceHomeRegions.PlaqueFloorShare * sh)).Within(0.5f), at + ": Play shrinks only to keep the plaque at 60% of H");
                    }

                    Assert.That(r.SideButton(true, 0), Is.EqualTo(r.Daily), at);
                    Assert.That(r.Daily.Overlaps(r.Logo), Is.False, at + ": the Daily Challenge under the logo");
                    Assert.That(r.Daily.Bottom, Is.LessThan(r.Plaque.Top), at + ": the Daily Challenge above the plaque");

                    var targets = new List<Box>();
                    foreach ((string _, Box box) in r.Buttons)
                    {
                        targets.Add(TouchBox(box, touch * 0.95f));
                    }

                    BottomNavRegions nav = ScreenLayout.BottomNav(w, h, insets, BottomNav.Order, NavPlace.Home);
                    foreach ((string _, Box box) in nav.Buttons)
                    {
                        targets.Add(box);
                    }

                    AssertTargets(targets, r.Safe, touch * 0.95f, at);
                }
            }

            // On the reference's 19.5:9 phone (the preview's insets) Play keeps its full height.
            ReferenceHomeRegions reference = ScreenLayout.ReferenceHome(1080f, 2340f, new Insets(110f, 63f));
            Assert.That(reference.Play.Height, Is.EqualTo(ReferenceHomeRegions.PlayShare * reference.Safe.Height).Within(0.5f));
        }

        /// <summary>
        /// The bottom menu (spec 005 FR-030, the owner's wooden variant): on every phone and for one to five shown places,
        /// the bar across the screen to its bottom; the plank 0.12 W tall on the safe bottom; the places in their order,
        /// sharing the span evenly and centered; the medallion 0.19 W over the active place, rising at least 0.05 W above
        /// the plank and staying on the screen; the icons in their places (the active one in the disc); every place but the
        /// active one a touch target of at least the touch minimum inside the safe area, clear of the others; and the
        /// menu's top the same whatever its places.
        /// </summary>
        [Test]
        public void TheBottomMenu_SpreadsItsPlacesInOrder_AndRaisesTheActiveOne()
        {
            NavPlace[][] sets =
            {
                new[] { NavPlace.Home },
                new[] { NavPlace.Home, NavPlace.Leaderboard },
                new[] { NavPlace.Shop, NavPlace.Home, NavPlace.Leaderboard },
                new[] { NavPlace.Shop, NavPlace.Home, NavPlace.Leaderboard, NavPlace.Collection },
                new[] { NavPlace.Shop, NavPlace.Wardrobe, NavPlace.Home, NavPlace.Leaderboard },
                new[] { NavPlace.Shop, NavPlace.Wardrobe, NavPlace.Home, NavPlace.Leaderboard, NavPlace.Collection },
            };
            foreach ((float w, float h, Insets insets) in Phones())
            {
                float touch = Touch(w, h);
                foreach (NavPlace[] places in sets)
                {
                    foreach (NavPlace active in places)
                    {
                        if (active == NavPlace.Leaderboard || active == NavPlace.Collection)
                        {
                            continue; // cards over Home, never a page of their own
                        }

                        BottomNavRegions r = ScreenLayout.BottomNav(w, h, insets, places, active);
                        string at = w + "x" + h + " " + string.Join(",", places) + " active=" + active;
                        float sw = r.Safe.Width;
                        Assert.That(r.Places, Is.EqualTo(places), at + ": the places in their order");
                        Assert.That(r.Bar.Left, Is.EqualTo(0f).Within(0.01f), at);
                        Assert.That(r.Bar.Right, Is.EqualTo(w).Within(0.01f), at);
                        Assert.That(r.Bar.Bottom, Is.EqualTo(h).Within(0.01f), at + ": the wood runs to the screen's bottom");
                        Assert.That(r.Plank.Height, Is.EqualTo(BottomNav.PlankShare * sw).Within(0.5f), at);
                        Assert.That(r.Plank.Bottom, Is.EqualTo(r.Safe.Bottom).Within(0.01f), at + ": the plank sits on the safe bottom");
                        Assert.That(r.Plank.Within(r.Safe), Is.True, at);
                        Assert.That(r.Bar.Top, Is.LessThan(r.Plank.Top), at + ": the vines reach over the plank");
                        Assert.That(r.Top, Is.EqualTo(ScreenLayout.BottomNavTop(w, h, insets)).Within(0.01f), at + ": the top whatever the places");
                        Assert.That(r.Top, Is.LessThanOrEqualTo(Math.Min(r.Bar.Top, r.Medallion.Top) + 0.01f), at);

                        // The places: one column each across the span, equal, touching, centered.
                        Assert.That(r.PlaceBoxes.Count, Is.EqualTo(places.Length), at);
                        Assert.That(r.PlaceBoxes[0].Left, Is.EqualTo(r.Safe.Left + (BottomNav.SpanStart * sw)).Within(0.5f), at);
                        Assert.That(r.PlaceBoxes[places.Length - 1].Right, Is.EqualTo(r.Safe.Left + (BottomNav.SpanEnd * sw)).Within(0.5f), at);
                        Assert.That(r.PlaceBoxes[0].Left - r.Safe.Left, Is.EqualTo(r.Safe.Right - r.PlaceBoxes[places.Length - 1].Right).Within(0.5f), at + ": centered");
                        for (int i = 0; i < places.Length; i++)
                        {
                            Box place = r.PlaceBoxes[i];
                            Assert.That(place.Within(r.Plank), Is.True, at + ": place " + i + " on the plank");
                            Assert.That(place.Width, Is.EqualTo(r.PlaceBoxes[0].Width).Within(0.01f), at + ": evenly spread");
                            if (i > 0)
                            {
                                Assert.That(place.Left, Is.EqualTo(r.PlaceBoxes[i - 1].Right).Within(0.01f), at);
                            }

                            Box icon = r.Icon(i);
                            if (i == r.ActiveIndex)
                            {
                                Assert.That(icon.Within(r.Disc), Is.True, at + ": the active icon in the disc");
                            }
                            else
                            {
                                Assert.That(icon.Within(r.Plank), Is.True, at + ": icon " + i + " on the plank");
                                Assert.That(icon.Left, Is.GreaterThanOrEqualTo(place.Left - 0.5f), at);
                                Assert.That(icon.Right, Is.LessThanOrEqualTo(place.Right + 0.5f), at);
                                Assert.That(icon.Overlaps(r.Disc), Is.False, at + ": icon " + i + " clear of the medallion");
                            }
                        }

                        // The medallion over the active place, raised, on screen.
                        Box active0 = r.PlaceBoxes[r.ActiveIndex];
                        Assert.That(r.Places[r.ActiveIndex], Is.EqualTo(active), at);
                        Assert.That(r.Medallion.Width, Is.EqualTo(Math.Min(BottomNav.MedallionShare * sw, h - r.Medallion.Top)).Within(0.5f), at + ": 0.19 W, less when it would leave the screen");
                        Assert.That(r.Medallion.Width, Is.GreaterThan(0.16f * sw), at);
                        Assert.That(r.Medallion.Height, Is.EqualTo(r.Medallion.Width).Within(0.01f), at);
                        Assert.That(r.Medallion.CenterX, Is.EqualTo(active0.CenterX).Within(0.01f), at + ": the medallion over the active place");
                        Assert.That(r.Medallion.Top, Is.EqualTo(r.Plank.Top - (BottomNav.RiseShare * sw)).Within(0.5f), at + ": it rises 0.05 W above the plank");
                        Assert.That(r.Medallion.Bottom, Is.LessThanOrEqualTo(h + 0.5f), at + ": it stays on the screen");
                        Assert.That(r.Medallion.Left, Is.GreaterThanOrEqualTo(-0.5f), at);
                        Assert.That(r.Medallion.Right, Is.LessThanOrEqualTo(w + 0.5f), at);

                        // The taps: every place but the active one, each in its column.
                        var targets = new List<Box>();
                        foreach ((string name, Box box) in r.Buttons)
                        {
                            Assert.That(name, Is.Not.EqualTo(BottomNav.Key(active)), at + ": the active place takes no tap");
                            targets.Add(box);
                        }

                        Assert.That(targets.Count, Is.EqualTo(places.Length - 1), at);
                        for (int i = 0; i < places.Length; i++)
                        {
                            Box t = r.Touch(i);
                            Assert.That(t.Left, Is.GreaterThanOrEqualTo(r.PlaceBoxes[i].Left - 0.5f), at);
                            Assert.That(t.Right, Is.LessThanOrEqualTo(r.PlaceBoxes[i].Right + 0.5f), at);
                            Assert.That(t.Bottom, Is.EqualTo(r.Safe.Bottom).Within(0.01f), at);
                        }

                        AssertTargets(targets, r.Safe, touch * 0.95f, at);

                        // The bar's picture: its plank and band inside the box, a groove between two places.
                        NavBarShape shape = r.Shape;
                        Assert.That(shape.Grooves.Count, Is.EqualTo(places.Length - 1), at);
                        Assert.That(shape.PlankTop, Is.InRange(0f, 1f), at);
                        Assert.That(shape.BandBottom, Is.InRange(shape.PlankTop, 1f), at);
                        for (int i = 0; i < shape.Grooves.Count; i++)
                        {
                            Assert.That(shape.Grooves[i], Is.InRange(shape.PlankLeft, shape.PlankRight), at);
                            Assert.That(shape.Grooves[i] * r.Bar.Width, Is.EqualTo(r.PlaceBoxes[i + 1].Left).Within(0.5f), at + ": groove " + i + " between two places");
                        }
                    }
                }
            }
        }

        /// <summary>The places a player sees (spec 005 FR-030): Home always, each other place once its feature unlocks.</summary>
        [Test]
        public void TheBottomMenu_ShowsAPlaceOnlyOnceItsFeatureIsUnlocked()
        {
            Assert.That(BottomNav.Places(HomeLook.Early), Is.EqualTo(new[] { NavPlace.Home }));
            Assert.That(BottomNav.Places(HomeLook.Early with { Rank = true }), Is.EqualTo(new[] { NavPlace.Home, NavPlace.Leaderboard }));
            Assert.That(BottomNav.Places(HomeLook.Early with { Rank = true, Store = true }), Is.EqualTo(new[] { NavPlace.Shop, NavPlace.Home, NavPlace.Leaderboard }));
            Assert.That(BottomNav.Places(HomeLook.Early with { Collection = true }), Is.EqualTo(new[] { NavPlace.Home, NavPlace.Collection }));
            Assert.That(BottomNav.Places(new HomeLook(true, true, true, true, true, true, true, true)), Is.EqualTo(BottomNav.Order));
            Assert.That(BottomNav.Order, Is.EqualTo(new[] { NavPlace.Shop, NavPlace.Wardrobe, NavPlace.Home, NavPlace.Leaderboard, NavPlace.Collection }));

            // The active place shows even when the list leaves it out.
            BottomNavRegions r = ScreenLayout.BottomNav(1080f, 2340f, new Insets(110f, 63f), new[] { NavPlace.Home }, NavPlace.Shop);
            Assert.That(r.Places, Is.EqualTo(new[] { NavPlace.Shop, NavPlace.Home }));
            Assert.That(r.Active, Is.EqualTo(NavPlace.Shop));

            // Each place's icon is the owner's picture with its slot.
            foreach (NavPlace place in BottomNav.Order)
            {
                Assert.That(OwnerPictures.NavIcon(place), Is.EqualTo("nav-" + BottomNav.Key(place)));
                Assert.That(OwnerPictures.SlotOf(OwnerPictures.NavIcon(place)), Is.EqualTo(BottomNav.Slot(place)));
                Assert.That(AssetSlots.Has(BottomNav.Slot(place)), Is.True, place.ToString());
                Assert.That(ShapeLibrary.Has(BottomNav.Fallback(place).ShapeId), Is.True, place + "'s stand-in glyph");
            }

            Assert.That(AssetSlots.Has("ui.nav.bar") && AssetSlots.Has("ui.nav.medallion"), Is.True);
        }

        [Test]
        public void TheWardrobe_FollowsTheReference_AndKeepsEveryButtonReachable()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (bool chips in new[] { false, true })
                {
                    ReferenceWardrobeRegions r = ScreenLayout.ReferenceWardrobe(w, h, insets, chips);
                    string at = w + "x" + h + " chips=" + chips;
                    AssertOrdered(r.Ordered, r.Safe, at);
                    Assert.That(r.Chips.IsEmpty, Is.EqualTo(!chips), at);
                    Assert.That(r.Hero.Bottom, Is.GreaterThan(r.Pedestal.Top), at + ": the hero stands on the pedestal");
                    Assert.That(r.NameTab.Within(r.NameCard), Is.True, at);
                    Assert.That(r.Grid.Within(r.Panel), Is.True, at);
                    Assert.That(r.Panel.Bottom, Is.EqualTo(h).Within(0.5f), at + ": the panel runs to the screen's bottom");
                    Assert.That(r.Banner.Overlaps(r.Back), Is.False, at);

                    // Everything above the bottom menu (FR-030): the cards, the footer and the page arrows' touch boxes.
                    Assert.That(r.NavTop, Is.EqualTo(ScreenLayout.BottomNavTop(w, h, insets)).Within(0.01f), at);
                    Assert.That(r.Grid.Bottom, Is.LessThanOrEqualTo(r.NavTop), at + ": the cards above the bottom menu");
                    Assert.That(r.Footer.Bottom, Is.LessThanOrEqualTo(r.NavTop), at + ": the footer above the bottom menu");
                    Assert.That(TouchBox(r.PageNext, Touch(w, h)).Bottom, Is.LessThanOrEqualTo(r.NavTop + 0.5f), at + ": the page arrows' touch boxes above it");
                    foreach (int count in new[] { 4, 5 })
                    {
                        for (int i = 0; i < count; i++)
                        {
                            Assert.That(r.Tab(i, count).Within(r.Tabs), Is.True, at + " tab " + i + " of " + count);
                            if (i > 0)
                            {
                                Assert.That(r.Tab(i, count).Overlaps(r.Tab(i - 1, count)), Is.False, at);
                            }
                        }
                    }

                    for (int i = 0; i < ReferenceWardrobeRegions.Columns; i++)
                    {
                        Assert.That(r.Card(i).Within(r.Grid), Is.True, at + " card " + i);
                        if (i > 0)
                        {
                            Assert.That(r.Card(i).Overlaps(r.Card(i - 1)), Is.False, at);
                        }
                    }

                    float touch = Touch(w, h) * 0.95f;
                    var targets = new List<Box>();
                    foreach ((string _, Box box) in r.Buttons)
                    {
                        targets.Add(TouchBox(box, touch));
                    }

                    foreach ((string _, Box box) in ScreenLayout.BottomNav(w, h, insets, BottomNav.Order, NavPlace.Wardrobe).Buttons)
                    {
                        targets.Add(box);
                    }

                    AssertTargets(targets, r.Safe, touch, at);
                }
            }
        }

        /// <summary>
        /// The page header (the owner's note of 2026-10-04: the Wardrobe's header "not on one line"): the back button, the
        /// banner and the Petals pill share one middle line, the back button's, on every phone; the banner with its ivy
        /// clusters (<see cref="PageHeader.BannerExtent"/>) touches neither the back button nor the Petals pill's box, nor
        /// their touch boxes; the banner's letters keep room for "Wardrobe"; and the Wardrobe and the Store page share it.
        /// </summary>
        [Test]
        public void ThePageHeader_PutsBackBannerAndPetalsOnOneLine_AndTheBannersLeavesTouchNeither()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                string at = w + "x" + h;
                Box safe = ScreenLayout.SafeArea(w, h, insets);
                PageHeader header = ScreenLayout.PageHeader(safe);
                float sw = safe.Width;
                Assert.That(header.Back.CenterY, Is.EqualTo(safe.Top + (safe.Height * PageHeader.TopShare) + (PageHeader.BackShare * sw / 2f)).Within(0.5f), at + ": the back button keeps its place");
                Assert.That(header.Banner.CenterY, Is.EqualTo(header.CenterY).Within(0.5f), at + ": the banner on the back button's line");
                Assert.That(header.Petals.CenterY, Is.EqualTo(header.CenterY).Within(0.5f), at + ": the Petals pill on the back button's line");
                Assert.That(header.Back.Width, Is.EqualTo(PageHeader.BackShare * sw).Within(0.5f), at);
                Assert.That(header.Banner.Height, Is.EqualTo(PageHeader.BannerShare * sw).Within(0.5f), at);
                Assert.That(header.BannerExtent.Height, Is.EqualTo(header.Banner.Height * GardenLook.IvyShare).Within(0.5f), at + ": the banner and its leaves about the back button's height");
                Assert.That(header.Petals.Width, Is.EqualTo(PageHeader.PetalsWidthShare * sw).Within(0.5f), at);
                Assert.That(header.Petals.Height, Is.EqualTo(PageHeader.PetalsHeightShare * sw).Within(0.5f), at);
                Assert.That(header.Petals.Right, Is.EqualTo(safe.Right - (0.02f * sw)).Within(0.5f), at);
                Assert.That(header.Row.Within(safe), Is.True, at + ": the header row inside the safe area");

                // The leaves (the plank's ivy clusters) clear the back button and the Petals pill's box, with their touch
                // boxes, by the gap.
                Box extent = header.BannerExtent;
                float touch = Touch(w, h);
                foreach ((string name, Box box) in new[] { ("Back", header.Back), ("Petals", header.Petals), ("Back touch", TouchBox(header.Back, touch)), ("Petals touch", TouchBox(header.Petals, touch)) })
                {
                    Assert.That(extent.Overlaps(box), Is.False, at + ": the banner's leaves " + extent + " overlap " + name + " " + box);
                }

                Assert.That(extent.Left - header.Back.Right, Is.EqualTo(PageHeader.GapShare * sw).Within(0.5f), at);
                Assert.That(header.Petals.Left - extent.Right, Is.EqualTo(PageHeader.GapShare * sw).Within(0.5f), at);

                // "Wardrobe" needs about 0.285 W at type.title; the room between the owner's ivy clusters (the plank less
                // 1.25 × its height) keeps it at 95% or more.
                Assert.That(header.Banner.Width - (GardenLook.IvyShare * header.Banner.Height), Is.GreaterThanOrEqualTo(0.27f * sw), at + ": the title keeps its room");

                ReferenceWardrobeRegions wardrobe = ScreenLayout.ReferenceWardrobe(w, h, insets);
                ReferenceStoreRegions store = ScreenLayout.ReferenceStore(w, h, insets);
                Assert.That(wardrobe.Header, Is.EqualTo(header), at + ": the Wardrobe's header");
                Assert.That(store.Header, Is.EqualTo(header), at + ": the Store page's header");
                Assert.That(wardrobe.Hero.Top, Is.GreaterThanOrEqualTo(header.Row.Bottom - 0.5f), at + ": the hero under the header");
            }
        }

        /// <summary>
        /// The Store page (the owner's note of 2026-10-04: "a separate page, not a popup"): inside the safe area, the header
        /// row, the tabs, the offline line, the list and the footer in order; the panel from under the header to the
        /// screen's bottom; the Shop's rows inside the list, above the footer when they take more than a page, never
        /// overlapping, at least the touch minimum tall; the outfit cards inside the lighter panel, at least two rows of
        /// three; and every button reachable.
        /// </summary>
        [Test]
        public void TheStorePage_KeepsItsRegionsInOrder_AndEveryTargetReachable()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (bool cosmetics in new[] { false, true })
                {
                    foreach (bool status in new[] { false, true })
                    {
                        ReferenceStoreRegions r = ScreenLayout.ReferenceStore(w, h, insets, cosmetics, status);
                        string at = w + "x" + h + " cosmetics=" + cosmetics + " status=" + status;
                        AssertOrdered(r.Ordered, r.Safe, at);
                        Assert.That(r.Tabs.IsEmpty, Is.EqualTo(!cosmetics), at);
                        Assert.That(r.Status.IsEmpty, Is.EqualTo(!status), at);
                        Assert.That(r.Panel.Top, Is.GreaterThan(r.Header.Row.Bottom), at + ": the panel under the header");
                        Assert.That(r.Panel.Bottom, Is.EqualTo(h).Within(0.5f), at + ": the panel runs to the screen's bottom");
                        Assert.That(r.List.Within(r.Panel), Is.True, at);
                        Assert.That(r.NavTop, Is.EqualTo(ScreenLayout.BottomNavTop(w, h, insets)).Within(0.01f), at);
                        Assert.That(r.List.Bottom, Is.EqualTo(r.NavTop - (0.02f * r.W)).Within(0.5f), at + ": the list ends 0.02 W over the bottom menu");
                        Assert.That(r.Footer.Within(r.List), Is.True, at);
                        if (cosmetics)
                        {
                            Assert.That(r.Tabs.Within(r.Panel), Is.True, at);
                        }

                        float touch = Touch(w, h) * 0.95f;
                        foreach (int count in new[] { 1, 4, 7, 9, 12, 20 })
                        {
                            int perPage = r.RowsPerPage(count);
                            bool paged = count > perPage;
                            Assert.That(perPage, Is.InRange(1, count), at + " rows " + count);
                            Assert.That(r.RowHeight(count), Is.InRange((ReferenceStoreRegions.RowShare * r.W) - 0.5f, (ReferenceStoreRegions.RowMaxShare * r.W) + 0.5f), at + " rows " + count);
                            for (int i = 0; i < perPage; i++)
                            {
                                Box row = r.Row(i, count);
                                Assert.That(row.Within(r.List), Is.True, at + " rows " + count + ": row " + i + " " + row + " inside the list");
                                Assert.That(row.Height, Is.GreaterThanOrEqualTo(touch), at + ": a row is a full touch target");
                                if (paged)
                                {
                                    Assert.That(row.Bottom, Is.LessThanOrEqualTo(r.Footer.Top + 0.5f), at + " rows " + count + ": row " + i + " above the footer");
                                }

                                if (i > 0)
                                {
                                    Assert.That(row.Overlaps(r.Row(i - 1, count)), Is.False, at);
                                }
                            }
                        }

                        // The seven rows of the Shop (four boosters, three real-money rows) fit one page on every phone.
                        Assert.That(r.RowsPerPage(7), Is.EqualTo(7), at + ": the Shop's seven rows on one page");

                        Assert.That(r.FamilyTabs.Within(r.List), Is.True, at);
                        Assert.That(r.OutfitPanel.Top, Is.EqualTo(r.FamilyTabs.Bottom).Within(0.5f), at);
                        Assert.That(r.OutfitRows, Is.GreaterThanOrEqualTo(2), at + ": at least two rows of outfit cards");
                        Assert.That(r.OutfitsPerPage, Is.EqualTo(r.OutfitRows * ReferenceStoreRegions.OutfitColumns), at);
                        for (int i = 0; i < r.OutfitsPerPage; i++)
                        {
                            Box card = r.OutfitCard(i);
                            Assert.That(card.Within(r.OutfitGrid), Is.True, at + ": card " + i + " " + card + " inside the grid " + r.OutfitGrid);
                            Assert.That(card.Height, Is.LessThanOrEqualTo((card.Width * ReferenceStoreRegions.OutfitMaxAspect) + 0.5f), at);
                            for (int j = 0; j < i; j++)
                            {
                                Assert.That(card.Overlaps(r.OutfitCard(j)), Is.False, at + ": cards " + j + " and " + i);
                            }
                        }

                        Assert.That(r.OutfitGrid.Bottom, Is.LessThanOrEqualTo(r.Footer.Top + 0.5f), at + ": the cards above the footer");

                        var targets = new List<Box>();
                        foreach ((string _, Box box) in r.Buttons)
                        {
                            targets.Add(TouchBox(box, touch));
                        }

                        BottomNavRegions nav = ScreenLayout.BottomNav(w, h, insets, BottomNav.Order, NavPlace.Shop);
                        foreach ((string _, Box box) in nav.Buttons)
                        {
                            targets.Add(box);
                            Assert.That(box.Overlaps(r.Row(r.RowsPerPage(7) - 1, 7)), Is.False, at + ": the last row clear of the menu's " + box);
                            Assert.That(box.Overlaps(r.OutfitCard(r.OutfitsPerPage - 1)), Is.False, at + ": the last outfit card clear of the menu's " + box);
                        }

                        AssertTargets(targets, r.Safe, touch, at);
                    }
                }
            }
        }

        /// <summary>
        /// The tray's pods: each column's shown pods inside the pod row, one under the other without overlapping, the
        /// exposed one at least as tall as the others, every pod's place at least <see cref="PodChip.MinAspect"/> times as
        /// wide as tall (its frame then has its whole width).
        /// </summary>
        private static void AssertGrid(ReferenceGameplayRegions r, string at)
        {
            for (int s = 0; s < r.Columns.Count; s++)
            {
                for (int d = 0; d < r.PodRows; d++)
                {
                    Box pod = r.Pod(s, d);
                    Assert.That(pod.Within(r.PodRow.Inset(-0.5f)), Is.True, at + ": pod " + s + "/" + d + " inside the pod row");
                    Assert.That(pod.Width, Is.GreaterThanOrEqualTo((pod.Height * PodChip.MinAspect) - 0.5f), at + ": pod " + s + "/" + d + " wide enough for its frame");
                    if (d > 0)
                    {
                        Assert.That(pod.Top, Is.GreaterThanOrEqualTo(r.Pod(s, d - 1).Bottom), at + ": pods " + s + "/" + (d - 1) + " and " + d + " overlap");
                        Assert.That(r.Pod(s, 0).Height, Is.GreaterThanOrEqualTo(pod.Height - 0.01f), at);
                    }
                }
            }
        }

        private static void AssertRow(IReadOnlyList<Box> cells, Box row, string at)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Assert.That(cells[i].Within(row), Is.True, at + ": cell " + i + " " + cells[i] + " leaves its row " + row);
                if (i > 0)
                {
                    Assert.That(cells[i].Left, Is.GreaterThanOrEqualTo(cells[i - 1].Right), at + ": cells " + (i - 1) + " and " + i + " overlap");
                    Assert.That(cells[i].Width, Is.EqualTo(cells[0].Width).Within(0.01f), at);
                }
            }

            if (cells.Count > 0)
            {
                Assert.That(cells[0].Left - row.Left, Is.EqualTo(row.Right - cells[cells.Count - 1].Right).Within(0.5f), at + ": centered");
            }
        }

        private static void AssertTargets(IReadOnlyList<Box> targets, Box safe, float min, string at)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                Assert.That(Math.Min(targets[i].Width, targets[i].Height), Is.GreaterThanOrEqualTo(min - 0.5f), at + ": target " + targets[i] + " is too small");
                Assert.That(targets[i].Within(safe), Is.True, at + ": target " + targets[i] + " leaves the safe area " + safe);
                for (int j = 0; j < i; j++)
                {
                    Assert.That(targets[i].Overlaps(targets[j]), Is.False, at + ": targets " + targets[j] + " and " + targets[i] + " overlap");
                }
            }
        }

        private static void AssertOrdered(IReadOnlyList<(string Name, Box Box)> regions, Box within, string at)
        {
            (string Name, Box Box)? previous = null;
            foreach ((string name, Box box) in regions)
            {
                if (box.IsEmpty)
                {
                    continue;
                }

                Assert.That(box.Within(within), Is.True, at + ": " + name + " " + box + " leaves " + within);
                if (previous.HasValue)
                {
                    Assert.That(box.Overlaps(previous.Value.Box), Is.False, at + ": " + name + " " + box + " overlaps " + previous.Value.Name + " " + previous.Value.Box);
                    Assert.That(box.Top, Is.GreaterThanOrEqualTo(previous.Value.Box.Bottom - 0.5f), at + ": " + name + " is above " + previous.Value.Name);
                }

                previous = (name, box);
            }
        }
    }
}
