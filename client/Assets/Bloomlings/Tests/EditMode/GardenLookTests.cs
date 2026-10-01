using System.Linq;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The Garden look's engine-free rules (spec 003 FR-007 to FR-009, FR-017 to FR-020, FR-025, FR-031).</summary>
    public class GardenLookTests
    {
        [Test]
        public void ColoredSets_DeriveTheirShades_FromOneBase()
        {
            foreach (ColorSet set in GardenLook.ColoredSets)
            {
                Assert.That(set.Top, Is.EqualTo(set.Face.Lighten(0.18f)), set.Name);
                Assert.That(set.Lip, Is.EqualTo(set.Face.Darken(0.25f)), set.Name);
                Assert.That(set.Line, Is.EqualTo(set.Face.Darken(0.42f)), set.Name);
            }
        }

        [Test]
        public void NoOutline_IsPureBlack_AndCreamUsesTheGardenBrown()
        {
            foreach (ColorSet set in GardenLook.Sets)
            {
                Assert.That(set.Line, Is.Not.EqualTo(Rgba.Black), set.Name);
                Assert.That(set.Line.Luminance, Is.GreaterThan(0.01), set.Name);
            }

            Assert.That(GardenLook.Cream.Line, Is.EqualTo(DesignTokens.Colors.GardenOutline));
        }

        [Test]
        public void LabelsOnColoredFaces_ReachThreeToOne_AgainstTheirOutline()
        {
            // FR-025: the label's fill (both ends of its gradient) against its outline; button labels are large text.
            foreach (ColorSet set in GardenLook.ColoredSets)
            {
                TextLook look = GardenLook.LabelOn(set);
                Assert.That(look.Volumetric, Is.True, set.Name);
                Assert.That(Rgba.Contrast(look.FillTop, look.Outline), Is.GreaterThanOrEqualTo(3.0), set.Name + " top");
                Assert.That(Rgba.Contrast(look.FillBottom, look.Outline), Is.GreaterThanOrEqualTo(3.0), set.Name + " bottom");
            }
        }

        [Test]
        public void LabelsOnCreamAndWhite_ArePlainDarkBrown_AndReachFourAndAHalfToOne()
        {
            foreach (ColorSet set in new[] { GardenLook.Cream, GardenLook.White })
            {
                TextLook look = GardenLook.LabelOn(set);
                Assert.That(look.Volumetric, Is.False, set.Name);
                Assert.That(look.OutlineEm, Is.EqualTo(0f));
                Assert.That(look.ExtrudeEm, Is.EqualTo(0f));
                Assert.That(Rgba.Contrast(look.FillTop, set.Face), Is.GreaterThanOrEqualTo(4.5), set.Name);
            }

            Assert.That(Rgba.Contrast(DesignTokens.Colors.GardenLabelPlain, DesignTokens.Colors.GardenPaperBottom), Is.GreaterThanOrEqualTo(4.5));
        }

        [Test]
        public void TheLabelLook_KeepsItsBalancedVolume()
        {
            TextLook look = TextLook.OnColor(GardenLook.Green);
            Assert.That(look.OutlineEm, Is.EqualTo(0.036f));
            Assert.That(look.ExtrudeEm, Is.EqualTo(0.09f));
            Assert.That(look.ShadowAlpha, Is.EqualTo(0.3f));
            Assert.That(look.Outline, Is.EqualTo(GardenLook.Green.Line));
        }

        [Test]
        public void OnlyTheBadge_StaysUppercase()
        {
            // Spec 003 Clarifications (2A): sentence case everywhere, HARD and SUPER HARD stay uppercase.
            Assert.That(DesignTokens.Type.All.Where(t => t.Upper).Select(t => t.Name), Is.EqualTo(new[] { "type.badge" }));
        }

        [Test]
        public void ThePress_SinksAtOnce_OvershootsOnce_AndRests()
        {
            Assert.That(GardenLook.PressDepth(true, -1f), Is.EqualTo(1f));
            Assert.That(GardenLook.PressDepth(false, -1f), Is.EqualTo(0f));
            float seconds = DesignTokens.Motion.Press.Seconds;
            Assert.That(seconds, Is.LessThanOrEqualTo(0.4f));
            Assert.That(GardenLook.PressDepth(false, 0f), Is.EqualTo(1f).Within(1e-4));
            Assert.That(GardenLook.PressDepth(false, seconds), Is.EqualTo(0f));

            int signChanges = 0;
            float previous = 1f;
            float lowest = 1f;
            for (float t = 0f; t <= seconds; t += 0.005f)
            {
                float depth = GardenLook.PressDepth(false, t);
                if ((previous > 0f && depth < 0f) || (previous < 0f && depth > 0f))
                {
                    signChanges++;
                }

                lowest = System.Math.Min(lowest, depth);
                previous = depth;
            }

            Assert.That(signChanges, Is.EqualTo(1), "one overshoot past rest");
            Assert.That(lowest, Is.LessThan(0f).And.GreaterThan(-0.2f));
        }

        [Test]
        public void TheBreath_StaysWithinFourPercent_AndIsSlow()
        {
            Assert.That(DesignTokens.Motion.Breathe.Seconds, Is.GreaterThanOrEqualTo(1.2f));
            for (float t = 0f; t < 3f; t += 0.01f)
            {
                float scale = GardenLook.Breathe(t);
                Assert.That(scale, Is.InRange(1f, 1.04f));
            }

            Assert.That(GardenLook.Breathe(0f), Is.EqualTo(1f));
        }

        [Test]
        public void EarnedPetals_CountUp_FromZeroToTheAmount()
        {
            Assert.That(GardenLook.CountUp(20, 0f), Is.EqualTo(0));
            Assert.That(GardenLook.CountUp(20, DesignTokens.Motion.CountUp.Seconds), Is.EqualTo(20));
            Assert.That(GardenLook.CountUp(20, 10f), Is.EqualTo(20));
            long previous = 0;
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                long now = GardenLook.CountUp(1240, t);
                Assert.That(now, Is.GreaterThanOrEqualTo(previous));
                previous = now;
            }
        }

        [Test]
        public void TheGlow_PulsesBetween55And100Percent()
        {
            for (float t = 0f; t < 2.4f; t += 0.01f)
            {
                Assert.That(GardenLook.Glow(t), Is.InRange(0.55f - 1e-4f, 1f + 1e-4f));
            }
        }

        [Test]
        public void BoosterTiles_ShowChargesOrPrice_AndGreyOutPerFr046()
        {
            var charges = new BoosterTileState(charges: 2, price: 40, selected: false, usable: true, affordable: true);
            Assert.That(charges.ShowsCharges && !charges.ShowsPrice && !charges.Disabled, Is.True);

            var price = new BoosterTileState(0, 40, false, true, true);
            Assert.That(!price.ShowsCharges && price.ShowsPrice && !price.Disabled, Is.True);

            var poor = new BoosterTileState(0, 40, false, true, false);
            Assert.That(poor.Disabled, Is.True, "no charges and too few Petals");

            var useless = new BoosterTileState(3, 40, false, false, true);
            Assert.That(useless.Disabled, Is.True, "no effect now");

            var selected = new BoosterTileState(1, 40, true, false, true);
            Assert.That(selected.Disabled, Is.False, "a selected tile is never greyed");
        }

        [Test]
        public void TheDecoration_SitsOverTwoCorners_AndCanBeSwitchedOff()
        {
            var button = new Box(100f, 500f, 640f, 704f);
            (Box topLeft, Box bottomRight) = GardenLook.DecorationBoxes(button);
            Assert.That(topLeft.Contains(button.Left, button.Top), Is.True);
            Assert.That(bottomRight.Contains(button.Right, button.Bottom), Is.True);
            Assert.That(topLeft.Height, Is.EqualTo(button.Height * DesignTokens.Garden.DecorationSize).Within(0.01f));
            Assert.That(GardenLook.DecorationParts.Count, Is.EqualTo(5));
            Assert.That(DesignTokens.Garden.Decorations, Is.True);
        }

        [Test]
        public void TheRecipe_ScalesWithTheElement()
        {
            Assert.That(DesignTokens.Garden.Lip(204f), Is.EqualTo(DesignTokens.Garden.LipLarge));
            Assert.That(DesignTokens.Garden.Lip(110f), Is.EqualTo(DesignTokens.Garden.LipMedium));
            Assert.That(DesignTokens.Garden.Lip(50f), Is.EqualTo(DesignTokens.Garden.LipSmall));
            Assert.That(DesignTokens.Garden.Outline(50f), Is.LessThan(DesignTokens.Garden.Outline(110f)));
            Assert.That(DesignTokens.Garden.PlateInset(204f), Is.GreaterThan(DesignTokens.Garden.PlateInset(50f)));
        }
    }
}
