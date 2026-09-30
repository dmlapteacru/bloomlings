using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The screen regions across phone shapes (spec 002 FR-009, FR-017, SC-007; data-model rules 1–6).</summary>
    public class ScreenLayoutTests
    {
        /// <summary>16:9, 18:9, 19.5:9, 20:9 and 21:9 phones, with typical status and navigation bar insets.</summary>
        private static IEnumerable<(float W, float H, Insets Insets)> Phones()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 20f / 9f, 21f / 9f })
            {
                yield return (1080f, 1080f * ratio, new Insets(ratio > 2f ? 110f : 60f, ratio > 2f ? 60f : 0f));
                yield return (720f, 720f * ratio, Insets.None);
            }
        }

        [Test]
        public void Gameplay_KeepsFrameSevenOrder_WithoutOverlap_InsideTheSafeArea()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (bool badge in new[] { false, true })
                {
                    foreach (bool boosters in new[] { false, true })
                    {
                        GameplayRegions r = ScreenLayout.Gameplay(w, h, insets, badge, boosters);
                        string at = w + "x" + h + " badge=" + badge + " boosters=" + boosters;
                        AssertOrdered(r.Ordered, r.Safe, at);
                        Assert.That(r.Board.Height, Is.GreaterThanOrEqualTo(r.Safe.Height * ScreenLayout.BoardMinShare - 1f), at + " board share");
                        Assert.That(r.Board.Top, Is.GreaterThanOrEqualTo(r.TopBar.Bottom), at);
                        Assert.That(r.Slots.Top, Is.GreaterThanOrEqualTo(r.Board.Bottom), at);
                        Assert.That(r.Tray.Top, Is.GreaterThanOrEqualTo(r.Slots.Bottom), at);
                        if (boosters)
                        {
                            Assert.That(r.Boosters.Top, Is.GreaterThanOrEqualTo(r.Tray.Bottom), at);
                            Assert.That(r.Boosters.Height, Is.GreaterThan(0f), at);
                        }
                        else
                        {
                            Assert.That(r.Boosters.IsEmpty, Is.True, at + " the bar is hidden before L3");
                        }
                    }
                }
            }
        }

        [Test]
        public void Home_KeepsItsOrder_AndCollapsesLockedFeatures()
        {
            var full = new HomeLook(true, true, true, true, true, true, true, true);
            foreach ((float w, float h, Insets insets) in Phones())
            {
                foreach (HomeLook look in new[] { HomeLook.Early, full })
                {
                    HomeRegions r = ScreenLayout.Home(w, h, insets, look);
                    string at = w + "x" + h + (look == full ? " full" : " early");
                    AssertOrdered(r.Ordered, r.Safe, at);
                    Assert.That(r.Play.Height, Is.GreaterThan(0f), at);
                    Assert.That(r.Hero.Height, Is.GreaterThan(h * 0.2f), at + " the hero or scene keeps room");
                    Assert.That(r.Rank.IsEmpty, Is.EqualTo(!look.Rank), at);
                    Assert.That(r.Daily.IsEmpty, Is.EqualTo(!look.DailyChallenge), at);
                }
            }
        }

        [Test]
        public void Cards_AndSheets_StayOnScreen_AndKeepTheBoardVisible()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                CardRegions card = ScreenLayout.Card(w, h, insets, 900f);
                Box safe = ScreenLayout.SafeArea(w, h, insets);
                Assert.That(card.Card.Within(safe), Is.True);
                Assert.That(card.Close.Within(card.Card), Is.True);
                Assert.That(card.Title.Overlaps(card.Close), Is.False);

                SheetRegions sheet = ScreenLayout.Sheet(w, h, insets, 700f);
                Assert.That(sheet.Sheet.Top, Is.GreaterThan(h * 0.35f), "the board stays visible above the jam sheet");
                Assert.That(sheet.Body.Within(safe), Is.True);
            }
        }

        [Test]
        public void Rows_SplitIntoEqualCenteredCells()
        {
            Box[] cells = ScreenLayout.Row(new Box(0, 0, 1000, 200), 5, 20, 170, square: true);
            Assert.That(cells.Length, Is.EqualTo(5));
            Assert.That(cells[0].Width, Is.EqualTo(170f).Within(0.01f));
            Assert.That(cells[0].Left, Is.EqualTo(1000f - cells[4].Right).Within(0.01f));
            for (int i = 1; i < cells.Length; i++)
            {
                Assert.That(cells[i].Overlaps(cells[i - 1]), Is.False);
            }
        }

        private static void AssertOrdered(IReadOnlyList<(string Name, Box Box)> regions, Box safe, string at)
        {
            float bottom = float.MinValue;
            (string Name, Box Box)? previous = null;
            foreach ((string name, Box box) in regions)
            {
                if (box.IsEmpty)
                {
                    continue;
                }

                Assert.That(box.Within(safe), Is.True, at + ": " + name + " " + box + " leaves the safe area " + safe);
                if (previous.HasValue)
                {
                    Assert.That(box.Overlaps(previous.Value.Box), Is.False, at + ": " + name + " overlaps " + previous.Value.Name);
                    Assert.That(box.Top, Is.GreaterThanOrEqualTo(bottom - 12f), at + ": " + name + " is above " + previous.Value.Name);
                }

                bottom = box.Bottom;
                previous = (name, box);
            }
        }
    }
}
