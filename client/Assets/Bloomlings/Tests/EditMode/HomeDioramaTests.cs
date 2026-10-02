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
    }
}
