using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Wardrobe's regions across phone shapes (spec 005 §4.6, <see cref="WardrobeLayout"/>), and the family a win
    /// celebrates with (pictures.md A7).
    /// </summary>
    public class WardrobeLayoutTests
    {
        private static IEnumerable<(float W, float H, Insets Insets)> Phones()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 20f / 9f, 21f / 9f })
            {
                yield return (1080f, 1080f * ratio, new Insets(ratio > 2f ? 110f : 60f, ratio > 2f ? 60f : 0f));
                yield return (720f, 720f * ratio, Insets.None);
            }
        }

        [Test]
        public void TheBands_KeepTheirOrder_InsideTheSafeArea()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                WardrobeRegions r = WardrobeLayout.Wardrobe(w, h, insets);
                string at = w + "x" + h;
                Box? previous = null;
                foreach ((string name, Box box) in r.Ordered)
                {
                    Assert.That(box.IsEmpty, Is.False, at + ": " + name);
                    Assert.That(box.Within(r.Safe), Is.True, at + ": " + name + " " + box + " leaves the safe area");
                    if (previous.HasValue)
                    {
                        Assert.That(box.Overlaps(previous.Value), Is.False, at + ": " + name + " overlaps the band above it");
                        Assert.That(box.Top, Is.GreaterThanOrEqualTo(previous.Value.Bottom - 1f), at + ": " + name);
                    }

                    previous = box;
                }

                Assert.That(r.Rows, Is.InRange(1, 2), at);
                Assert.That(r.Hero.Bottom, Is.GreaterThan(r.Pedestal.Top), at + ": the hero stands on the pedestal");
                Assert.That(r.Hero.Top, Is.GreaterThanOrEqualTo(r.Stage.Top - 1f), at + ": the hero stays in its stage");
                Assert.That(r.Stage.Height, Is.GreaterThan(h * 0.17f), at + ": the hero keeps its room");
                Assert.That(r.Banner.Overlaps(r.Back), Is.False, at);
                Assert.That(r.NameTab.Within(r.Safe), Is.True, at);
            }
        }

        [Test]
        public void EveryButton_IsBigEnough_AndTheCardsShareTheGrid()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                WardrobeRegions r = WardrobeLayout.Wardrobe(w, h, insets);
                float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(w, h);
                string at = w + "x" + h;
                foreach ((string name, Box box) in r.Buttons)
                {
                    Assert.That(box.Width, Is.GreaterThanOrEqualTo(touch - 0.5f), at + ": " + name);
                    Assert.That(box.Height, Is.GreaterThanOrEqualTo(touch - 0.5f), at + ": " + name);
                    Assert.That(box.Within(r.Safe), Is.True, at + ": " + name);
                }

                for (int i = 0; i < r.PerPage; i++)
                {
                    Box card = r.Card(i);
                    Assert.That(card.Within(r.Grid), Is.True, at + ": card " + i);
                    Assert.That(card.Height, Is.GreaterThanOrEqualTo(card.Width), at + ": card " + i + " holds its picture and name");
                    for (int j = 0; j < i; j++)
                    {
                        Assert.That(card.Overlaps(r.Card(j)), Is.False, at + ": cards " + j + " and " + i);
                    }
                }

                Assert.That(r.Grid.Bottom, Is.LessThanOrEqualTo(r.Footer.Top + 0.5f), at + ": the footer stays below the cards");
            }
        }

        [Test]
        public void TheWins_CelebrateWithTwigAndSprig_ByTurns()
        {
            // The owner's choices of 2026-10-03: Twig celebrates, then Sprig takes turns with it, alternating its celebrate
            // and its clap.
            Assert.That(CharacterArt.Celebrants, Is.EqualTo(new[] { Family.Twig, Family.Sprig }));
            Assert.That(CharacterArt.CelebrantOf(1), Is.EqualTo(Family.Twig));
            Assert.That(CharacterArt.CelebrantOf(2), Is.EqualTo(Family.Sprig));
            Assert.That(CharacterArt.CelebrantOf(3), Is.EqualTo(Family.Twig));
            Assert.That(CharacterArt.CelebrantOf(4), Is.EqualTo(Family.Sprig));
            Assert.That(HeroMotion.WinClip(CharacterArt.CelebrantOf(2), CharacterArt.CelebrationTurn(2)), Is.EqualTo(MotionClip.Win), "L2: Sprig's celebrate");
            Assert.That(HeroMotion.WinClip(CharacterArt.CelebrantOf(4), CharacterArt.CelebrationTurn(4)), Is.EqualTo(MotionClip.Win2), "L4: its clap");
            Assert.That(HeroMotion.WinClip(CharacterArt.CelebrantOf(6), CharacterArt.CelebrationTurn(6)), Is.EqualTo(MotionClip.Win), "L6: its celebrate again");
            Assert.That(HeroMotion.WinClip(CharacterArt.CelebrantOf(3), CharacterArt.CelebrationTurn(3)), Is.EqualTo(MotionClip.Win), "L3: Twig's cheer");
        }
    }
}
