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
        private static readonly EntrySide[] Bottom = { EntrySide.Bottom };

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
                foreach (EntrySide[] sides in new[] { Bottom, new[] { EntrySide.Left }, Array.Empty<EntrySide>() })
                {
                    foreach (bool boosters in new[] { true, false })
                    {
                        foreach (bool badge in new[] { false, true })
                        {
                            foreach (int stacks in new[] { 2, 3, 4, 5 })
                            {
                                ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, insets, sides, stacks, 5, boosters, badge);
                                string at = w + "x" + h + " sides=" + sides.Length + " boosters=" + boosters + " badge=" + badge + " stacks=" + stacks;
                                AssertOrdered(r.Ordered, r.Safe, at);
                                Assert.That(r.K, Is.InRange(0.8f, 1f), at);
                                Assert.That(r.TopBar.Top, Is.GreaterThanOrEqualTo(r.Safe.Top), at + ": the top bar sits under the top inset");
                                Assert.That(r.Board.Width, Is.LessThanOrEqualTo((r.W * ReferenceGameplayRegions.MaxBoardShare) + 0.5f), at);
                                Assert.That(r.Board.Height, Is.GreaterThan(r.Safe.Height * 0.38f), at + ": the board keeps its room");
                                Assert.That(r.Tray.Bottom, Is.EqualTo(h).Within(0.5f), at + ": the tray runs to the screen's bottom");
                                Assert.That(r.Tray.Left, Is.EqualTo(0f).Within(0.5f), at);
                                Assert.That(r.Tray.Right, Is.EqualTo(w).Within(0.5f), at);
                                Assert.That(r.TrayContent.Bottom, Is.LessThanOrEqualTo(r.Safe.Bottom), at + ": the tray's content stays above the bottom inset");
                                Assert.That(r.SlotRow.Top, Is.GreaterThan(r.Tray.Top), at);
                                Assert.That(r.EntryStrip.Bottom, Is.EqualTo(r.Tray.Top).Within(0.5f), at);

                                Assert.That(r.Slots.Count, Is.EqualTo(5), at);
                                AssertRow(r.Slots, r.SlotRow, at + " slots");
                                AssertRow(r.Decks, r.PodRow, at + " decks");
                                Assert.That(r.Decks.Count, Is.EqualTo(stacks), at);
                                Assert.That(r.PodRows, Is.EqualTo(1), at + ": up to five stacks keep one row");
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

                                // Pause, the speed pill, the booster boxes and the decks are pressed: their touch boxes stay
                                // inside the safe area and never overlap.
                                float touch = Touch(w, h) * 0.95f;
                                var targets = new List<Box> { TouchBox(r.Pause, touch), TouchBox(r.Speed, touch) };
                                targets.AddRange(r.Boosters);
                                targets.AddRange(r.Decks);
                                AssertTargets(targets, r.Safe, touch, at);
                            }
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
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, Insets.None, Bottom, 4, 5);
            Assert.That(r.K, Is.EqualTo(1f).Within(0.001f));
            Assert.That(r.Pause.Left, Is.EqualTo(0.04f * w).Within(1f));
            Assert.That(r.Pause.Width, Is.EqualTo(0.13f * w).Within(1f));
            Assert.That(r.Sign.Width, Is.EqualTo(0.42f * w).Within(1f));
            Assert.That(r.Speed.Right, Is.EqualTo(0.96f * w).Within(1f));
            Assert.That(r.Board.Top, Is.EqualTo(0.162f * w).Within(1f));
            Assert.That(r.EntryStrip.Height, Is.EqualTo(0.17f * w).Within(1f));
            Assert.That(r.SlotRow.Height, Is.EqualTo(0.19f * w).Within(1f));
            Assert.That(r.BoosterRow.Height, Is.EqualTo(0.23f * w).Within(1f));
            Assert.That(r.PodRow.Height, Is.EqualTo(0.31f * w).Within(1f));
            Assert.That(r.Slots[0].Width, Is.EqualTo(0.165f * w).Within(1f));
            Assert.That(r.Slots[0].Left, Is.EqualTo(0.04f * w).Within(1f), "the slots span 0.92 W");
            Assert.That(r.Slots[4].Right, Is.EqualTo(0.96f * w).Within(1f));
            Assert.That(r.Boosters[0].Width, Is.EqualTo(0.195f * w).Within(1f));
            Assert.That(r.Boosters[0].Left, Is.EqualTo(0.05f * w).Within(1f), "the boosters span 0.9 W");
            Assert.That(r.Boosters[3].Right, Is.EqualTo(0.95f * w).Within(1f));
            Assert.That(r.Decks[0].Width, Is.EqualTo(0.23f * w).Within(1f));
            Assert.That(r.Decks[0].Left, Is.EqualTo(0.02f * w).Within(1f), "four decks span 0.96 W");
            Assert.That(r.Decks[3].Right, Is.EqualTo(0.98f * w).Within(1f));
            Assert.That(r.Tray.Top, Is.EqualTo(h - (0.855f * w)).Within(2f), "the tray holds 0.855 W of rows and padding");
        }

        [Test]
        public void Gameplay_ExtraSlotNarrowsThePlates_AndManyStacksWrap()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                string at = w + "x" + h;
                ReferenceGameplayRegions six = ScreenLayout.ReferenceGameplay(w, h, insets, Bottom, 4, 6);
                Assert.That(six.Slots.Count, Is.EqualTo(6), at);
                AssertRow(six.Slots, six.SlotRow, at + " six slots");
                ReferenceGameplayRegions eight = ScreenLayout.ReferenceGameplay(w, h, insets, Bottom, 8, 5);
                Assert.That(eight.PodRows, Is.EqualTo(2), at);
                Assert.That(eight.Decks.Count, Is.EqualTo(8), at);
                foreach (Box deck in eight.Decks)
                {
                    Assert.That(deck.Within(eight.PodRow), Is.True, at);
                    Assert.That(deck.Width, Is.GreaterThanOrEqualTo(0.17f * eight.W * eight.K - 0.5f), at);
                }

                for (int i = 0; i < eight.Decks.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        Assert.That(eight.Decks[i].Overlaps(eight.Decks[j]), Is.False, at + " decks " + j + " and " + i);
                    }
                }
            }
        }

        [Test]
        public void Gameplay_TheBoardFits_AndBottomArchesStandInTheEntryStrip()
        {
            var entries = new[] { new EntryDef(new CellPos(4, 0), EntrySide.Bottom) };
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach ((int bw, int bh) in new[] { (9, 12), (6, 6), (12, 16), (10, 8) })
                {
                    string at = w + "x" + h + " " + bw + "x" + bh;
                    ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(w, h, insets, Bottom, 4, 5);
                    BoardLayout layout = r.FitBoard(bw, bh, entries);
                    Assert.That(layout.Outer.Width, Is.LessThanOrEqualTo((r.W * ReferenceGameplayRegions.MaxBoardShare) + 1f), at);
                    Assert.That(layout.Outer.Top, Is.GreaterThanOrEqualTo(r.Board.Top - 1f), at);
                    Assert.That(layout.Arches[0].Picture.Bottom, Is.LessThanOrEqualTo(r.Tray.Top + 1f), at + ": the arch stays on the lawn");
                    Assert.That(layout.Outer.CenterX, Is.EqualTo(r.Safe.CenterX).Within(1f), at);
                }
            }
        }

        [Test]
        public void TheDeck_ShowsTheFrontPod_AndTwoBandsAboveIt()
        {
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, 2340f, Insets.None, Bottom, 4, 5);
            PodDeck deck = r.Deck(0);
            Assert.That(deck.Front.Height, Is.EqualTo(deck.Deck.Height * PodDeck.FrontShare).Within(0.5f));
            Assert.That(deck.Front.Bottom, Is.EqualTo(deck.Deck.Bottom).Within(0.5f));
            Assert.That(deck.Buried2.Top, Is.GreaterThanOrEqualTo(deck.Deck.Top - 0.5f), "both buried pods stay in the deck");
            Assert.That(deck.Band(1).Height, Is.EqualTo(deck.Deck.Height * PodDeck.Raise).Within(0.5f));
            Assert.That(deck.Band(2).Bottom, Is.EqualTo(deck.Buried1.Top).Within(0.5f));
            Assert.That(deck.Tile.Within(deck.Inner), Is.True);
            Assert.That(deck.Count.Top, Is.GreaterThanOrEqualTo(deck.Tile.Bottom - 0.5f));
            Assert.That(deck.Count.Height, Is.GreaterThan(deck.Front.Height * 0.12f), "the count keeps room");
            Assert.That(deck.Tile.Width, Is.EqualTo(deck.Deck.Width * PodDeck.TileShare).Within(1f));
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
                AssertTargets(new List<Box> { TouchBox(r.Double, touch), r.Next }, r.Safe, touch, at);
            }
        }

        [Test]
        public void Home_FollowsTheReference_AndKeepsEveryButtonReachable()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (float reserve in new[] { 0f, 170f * DesignTokens.ScaleFor(w, h) })
                {
                    ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets, reserve);
                    string at = w + "x" + h + " reserve=" + reserve;
                    AssertOrdered(r.Ordered, r.Safe, at);
                    Assert.That(r.Teaser.Bottom, Is.LessThanOrEqualTo(r.Safe.Bottom - reserve + 0.5f), at + ": the reserve stays free");
                    Assert.That(r.Diorama.Top, Is.GreaterThanOrEqualTo(r.Logo.Bottom), at);
                    Assert.That(r.Plaque.Top, Is.LessThan(r.Diorama.Bottom), at + ": the plaque stands at the diorama's foot");
                    Assert.That(r.Play.Width, Is.EqualTo(0.85f * r.W).Within(0.5f), at);
                    Assert.That(r.SideButton(false, 1), Is.EqualTo(r.Collection), at);
                    Assert.That(r.SideButton(true, 1), Is.EqualTo(r.Store), at);
                    foreach (Box side in new[] { r.Wardrobe, r.Collection, r.Daily, r.Store, r.Rank })
                    {
                        Assert.That(side.Overlaps(r.Logo), Is.False, at + ": " + side + " under the logo");
                        Assert.That(side.Bottom, Is.LessThan(r.Plaque.Top), at + ": " + side + " above the plaque");
                    }

                    float touch = Touch(w, h) * 0.95f;
                    var targets = new List<Box>();
                    foreach ((string _, Box box) in r.Buttons)
                    {
                        targets.Add(TouchBox(box, touch));
                    }

                    AssertTargets(targets, r.Safe, touch, at);
                }
            }
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

                    AssertTargets(targets, r.Safe, touch, at);
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
