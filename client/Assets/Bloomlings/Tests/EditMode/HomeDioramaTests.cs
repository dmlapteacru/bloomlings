using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The reference Home's diorama (spec 005 FR-024, contracts/look.md §6.4, <see cref="HomeStage.ReferenceDiorama"/>):
    /// the four heroes as large as the reference's around the lotus fountain on the well's stone ring, inside Home's
    /// diorama region on every phone shape.
    /// </summary>
    public class HomeDioramaTests
    {
        private static IEnumerable<(float W, float H, Insets Insets, float Reserve)> Shapes()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 20f / 9f, 21f / 9f })
            {
                foreach (float reserve in new[] { 0f, 148f })
                {
                    yield return (1080f, 1080f * ratio, new Insets(ratio > 2f ? 110f : 60f, ratio > 2f ? 60f : 0f), reserve);
                    yield return (720f, 720f * ratio, Insets.None, reserve * 720f / 1080f);
                }
            }
        }

        [Test]
        public void TheDiorama_StaysInsideItsRegion_WithTheFourHeroesAroundTheFountain()
        {
            foreach ((float w, float h, Insets insets, float reserve) in Shapes())
            {
                Box stage = ScreenLayout.ReferenceHome(w, h, insets, reserve).Diorama;
                HomeDiorama d = HomeStage.ReferenceDiorama(stage);
                string at = w + "x" + h + " reserve=" + reserve;

                Assert.That(d.Pedestal.Within(stage), Is.True, at + ": the stone ring inside the stage");
                Assert.That(d.Fountain.Left >= d.Pedestal.Left && d.Fountain.Right <= d.Pedestal.Right, Is.True, at + ": the fountain within the ring's width");
                Assert.That(d.Fountain.Bottom, Is.InRange(d.Pedestal.Top, d.Pedestal.Bottom), at + ": the fountain stands on the ring");
                var families = new HashSet<Family>();
                foreach ((Family family, Box box) in d.Heroes)
                {
                    families.Add(family);
                    Assert.That(box.Width, Is.EqualTo(box.Height * CharacterArt.HeroWidth / CharacterArt.HeroHeight).Within(0.5f), at + ": " + family + " is a solo picture");
                    Assert.That(box.Top, Is.GreaterThanOrEqualTo(stage.Top - 0.5f), at + ": " + family + " under the logo");
                    Box body = box.Inset(box.Width * 0.04f, 0f);
                    Assert.That(body.Left, Is.GreaterThanOrEqualTo(stage.Left - 0.5f), at + ": " + family + " on the screen");
                    Assert.That(body.Right, Is.LessThanOrEqualTo(stage.Right + 0.5f), at + ": " + family + " on the screen");
                    float feet = box.Top + (box.Height * HomeStage.FeetShare);
                    Assert.That(feet, Is.LessThan(d.Pedestal.Bottom), at + ": " + family + " stands on or behind the ring");
                }

                Assert.That(families.Count, Is.EqualTo(4), at + ": the four families");
                Assert.That(d.Heroes[0].Family, Is.EqualTo(Family.Bloom), at + ": Bloom behind the fountain");
                Assert.That(d.Heroes[2].Box.Height, Is.GreaterThan(d.Heroes[3].Box.Height), at + ": Sprig larger than Twig, as the reference's");
                Assert.That(d.Pedestal.Width, Is.GreaterThan(stage.Width * 0.5f), at + ": the ring spans most of the width");
            }
        }

        [Test]
        public void AroundTheOwnersFountain_TheFourHeroesStandUnderTheLogo_AndAboveThePlaque()
        {
            foreach ((float w, float h, Insets insets, float _) in Shapes())
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                Box logo = r.LogoPicture();
                float letters = logo.Bottom - (logo.Height * 0.1f);
                var screen = new Box(0f, 0f, w, h);
                IReadOnlyList<(Family Family, Box Box)> heroes = HomeStage.AroundFountain(screen, letters);
                string at = w + "x" + h;

                Assert.That(heroes.Count, Is.EqualTo(4), at);
                Assert.That(heroes[0].Family, Is.EqualTo(Family.Bloom), at + ": Bloom behind the lotus, drawn first");
                Assert.That(heroes[2].Family, Is.EqualTo(Family.Sprig), at + ": Sprig at the left front");
                var families = new HashSet<Family>();
                foreach ((Family family, Box box) in heroes)
                {
                    families.Add(family);
                    Assert.That(box.Width, Is.EqualTo(box.Height * CharacterArt.HeroWidth / CharacterArt.HeroHeight).Within(0.5f), at + ": " + family + " is a solo picture");
                    Assert.That(box.Top + (box.Height * HomeStage.HeadShare), Is.GreaterThanOrEqualTo(letters - 0.5f), at + ": " + family + " under the logo's letters");
                    Box body = box.Inset(box.Width * 0.06f, 0f);
                    Assert.That(body.Left, Is.GreaterThanOrEqualTo(screen.Left - 0.5f), at + ": " + family + " on the screen");
                    Assert.That(body.Right, Is.LessThanOrEqualTo(screen.Right + 0.5f), at + ": " + family + " on the screen");
                }

                Assert.That(families.Count, Is.EqualTo(4), at + ": the four families");
                Box sprig = heroes[2].Box;
                Assert.That(sprig.CenterX, Is.LessThan(screen.CenterX), at + ": Sprig at the left");
                Assert.That(heroes[3].Box.CenterX, Is.GreaterThan(screen.CenterX), at + ": Twig at the right");
                Assert.That(sprig.Height, Is.GreaterThan(heroes[3].Box.Height), at + ": Sprig larger than Twig, as the reference's");
            }

            // The player's hero takes Sprig's place at the left front.
            IReadOnlyList<(Family Family, Box Box)> swapped = HomeStage.AroundFountain(new Box(0f, 0f, 1080f, 2340f), 0f, Family.Bloom);
            Assert.That(swapped[2].Family, Is.EqualTo(Family.Bloom));
            Assert.That(swapped[0].Family, Is.EqualTo(Family.Sprig));
        }

        [Test]
        public void TheLogoPicture_IsSizedByWidth_AndStaysUnderSettings()
        {
            foreach ((float w, float h, Insets insets, float _) in Shapes())
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                Box logo = r.LogoPicture();
                string at = w + "x" + h;
                Assert.That(logo.Width, Is.EqualTo(ReferenceHomeRegions.LogoPictureShare * r.W).Within(0.5f), at);
                Assert.That(logo.Height, Is.EqualTo(logo.Width * 440f / 1200f).Within(0.5f), at);
                Assert.That(logo.CenterX, Is.EqualTo(r.Logo.CenterX).Within(0.5f), at);
                Assert.That(logo.Top + (logo.Height * 0.1f), Is.GreaterThanOrEqualTo(r.Settings.Bottom - 0.5f), at + ": the letters under Settings");
            }
        }
    }
}
