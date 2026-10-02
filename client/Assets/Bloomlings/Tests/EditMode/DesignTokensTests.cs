using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The design board's visual tokens (spec 002 FR-005, FR-026; contracts/design-tokens.md).</summary>
    public class DesignTokensTests
    {
        [Test]
        public void TokenNames_AreUnique_AndMatchTheContract()
        {
            // 63 of spec 002, 26 garden colors of spec 003 (contracts/garden-tokens.md) and 35 material and UI colors of
            // spec 005 (contracts/look.md §1.2).
            Assert.That(DesignTokens.Colors.All.Count, Is.EqualTo(124));
            Assert.That(DesignTokens.Colors.All.Keys, Has.Member("wood.light").And.Member("stone.face").And.Member("parchment.line").And.Member("ray.light"));
            Assert.That(DesignTokens.Type.All.Select(t => t.Name).Distinct().Count(), Is.EqualTo(DesignTokens.Type.All.Count));
            Assert.That(DesignTokens.Type.All.All(t => t.Min > 0f && t.Min <= t.Size), Is.True);
        }

        [Test]
        public void BodyText_ReachesFourAndAHalfToOne_OnPanels()
        {
            Assert.That(Rgba.Contrast(DesignTokens.Colors.TextPrimary, DesignTokens.Colors.SurfacePanel), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(Rgba.Contrast(DesignTokens.Colors.TextPrimary, DesignTokens.Colors.ButtonSecondary), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(Rgba.Contrast(DesignTokens.Colors.TextPrimary, DesignTokens.Colors.SurfaceRowHighlight), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(Rgba.Contrast(DesignTokens.Colors.TextSecondary, DesignTokens.Colors.SurfacePanel), Is.GreaterThanOrEqualTo(4.5));
        }

        [Test]
        public void LargeTextOnColor_ReachesThreeToOne_WithItsOutline()
        {
            // White bold text with the dark outline on the colored pills, badges and buttons (large text: 3:1). The
            // outline darkens the glyph edge, so the fill is checked against the outline-mixed background.
            var backgrounds = new Dictionary<string, Rgba>
            {
                ["badge.hard"] = DesignTokens.Colors.BadgeHard,
                ["badge.super_hard"] = DesignTokens.Colors.BadgeSuperHard,
                ["badge.count"] = DesignTokens.Colors.BadgeCount,
                ["button.dark"] = DesignTokens.Colors.ButtonDark,
                ["booster.shuffle"] = DesignTokens.Colors.BoosterShuffle,
            };
            foreach (KeyValuePair<string, Rgba> background in backgrounds)
            {
                Assert.That(Rgba.Contrast(DesignTokens.Colors.TextOnColor, background.Value), Is.GreaterThanOrEqualTo(3.0), background.Key);
            }

            // The light pills and the green button carry outlined text: their outline color must reach 3:1 on white.
            Assert.That(Rgba.Contrast(DesignTokens.Colors.TextOutline.Over(DesignTokens.Colors.ButtonPrimary), DesignTokens.Colors.TextOnColor), Is.GreaterThanOrEqualTo(3.0));
        }

        [Test]
        public void VariantSymbols_ReachThreeToOne_OnTilesAndPods()
        {
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                Rgba color = Rgba.FromHex(variant.ColorHex);
                Assert.That(Rgba.Contrast(color.Ink, color), Is.GreaterThanOrEqualTo(3.0), variant.Id.Key + " tile");
                Assert.That(Rgba.Contrast(color.Ink, DesignTokens.TileTop(color)), Is.GreaterThanOrEqualTo(3.0), variant.Id.Key + " tile top");
            }
        }

        [Test]
        public void Backdrop_StaysLight_ForEveryTheme()
        {
            foreach ((string background, string accent) in new[] { ("#F5F2E6", "#DDEBCF"), ("#E8F1F2", "#CFE3E8"), ("#F6EDE2", "#EAD8C3"), ("#E3E3F0", "#CACBE3") })
            {
                BackdropColors colors = DesignTokens.Backdrop(background, accent);
                Assert.That(colors.SkyTop.Luminance, Is.GreaterThan(0.5), background);
                Assert.That(colors.SkyBottom.Luminance, Is.GreaterThan(0.7), background);
                Assert.That(colors.HillNear.Luminance, Is.GreaterThan(0.4), background);
            }
        }

        [Test]
        public void Hex_RoundTrips()
        {
            Assert.That(Rgba.FromHex("#5DBB46").Hex, Is.EqualTo("#5DBB46"));
            Assert.That(Rgba.FromHex("#1E24308C").A, Is.EqualTo(0x8C));
        }
    }
}
