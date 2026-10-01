using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The kawaii Bloomlings (spec 003 FR-032, contracts/bloomling-look.md).</summary>
    public class BloomlingArtTests
    {
        private const int Size = 96;

        [Test]
        public void EveryVariant_RendersAVisibleFigure_InsideItsSquare()
        {
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                var look = new BloomlingLook(variant.Family, Rgba.FromHex(variant.ColorHex), variant.IconId, Halo: true);
                byte[] rgba = BloomlingArt.Render(look, Size, topDown: true, premultiplied: true);
                float coverage = Coverage(rgba);
                Assert.That(coverage, Is.InRange(0.25f, 0.8f), variant.Id.Key);

                // Even with the halo, nothing reaches the picture's edge, so nothing is clipped.
                for (int i = 0; i < Size; i++)
                {
                    Assert.That(Alpha(rgba, i, 0) + Alpha(rgba, i, Size - 1) + Alpha(rgba, 0, i) + Alpha(rgba, Size - 1, i), Is.EqualTo(0), variant.Id.Key + " edge");
                }
            }
        }

        [Test]
        public void TheBadge_IsLight_AndItsSymbol_ReachesThreeToOne()
        {
            // Spec 001 FR-012: the symbol stays the first cue, on a white badge it reaches 3:1 (spec 003 FR-024).
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                Rgba color = Rgba.FromHex(variant.ColorHex);
                foreach (Rgba shown in new[] { color, DesignTokens.PodQueued(color), color.Grey().Mix(DesignTokens.Colors.StateStuck, 0.35f) })
                {
                    Assert.That(Rgba.Contrast(BloomlingArt.SymbolColor(shown), DesignTokens.Colors.CharBadge), Is.GreaterThanOrEqualTo(3.0), variant.Id.Key);
                }

                byte[] rgba = BloomlingArt.Render(new BloomlingLook(variant.Family, color, variant.IconId), Size, topDown: true, premultiplied: false);
                (int x, int y) = Pixel(0f, BloomlingArt.BadgeY + (BloomlingArt.BadgeRadius * 0.9f));
                Assert.That(Color(rgba, x, y), Is.EqualTo(DesignTokens.Colors.CharBadge), variant.Id.Key + " badge");
            }
        }

        [Test]
        public void WithoutAnIcon_ThereIsNoBadge()
        {
            byte[] rgba = BloomlingArt.Render(new BloomlingLook(Family.Bloom, Rgba.FromHex("#EF8DA5")), Size, topDown: true, premultiplied: false);
            (int x, int y) = Pixel(0f, BloomlingArt.BadgeY);
            Assert.That(Color(rgba, x, y), Is.Not.EqualTo(DesignTokens.Colors.CharBadge));
        }

        [Test]
        public void TheEyes_AreOpen_WhenHappyOrWorried_AndClosed_WhenSleepy()
        {
            // The left eye's lower half, clear of its sparkles and of the closed eye's arc: ink only when open.
            (int x, int y) = Pixel(-0.24f, -0.17f);
            foreach (BloomlingMood mood in new[] { BloomlingMood.Happy, BloomlingMood.Worried, BloomlingMood.Sleepy, BloomlingMood.None })
            {
                byte[] rgba = BloomlingArt.Render(new BloomlingLook(Family.Sprig, Rgba.FromHex("#ADCF42"), "leaf", mood), Size, topDown: true, premultiplied: false);
                bool ink = Color(rgba, x, y).Equals(DesignTokens.Colors.CharInk);
                Assert.That(ink, Is.EqualTo(mood == BloomlingMood.Happy || mood == BloomlingMood.Worried), mood.ToString());
            }
        }

        [Test]
        public void TheFamilies_HaveDistinctSilhouettes_AndTheCrestTellsVariantsApart()
        {
            Family[] families = { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig };
            byte[][] masks = families.Select(f => ShapeRaster.Mask(BloomlingArt.Silhouette(f), 64, topDown: true)).ToArray();
            for (int a = 0; a < masks.Length; a++)
            {
                for (int b = a + 1; b < masks.Length; b++)
                {
                    Assert.That(ShapeRaster.Difference(masks[a], masks[b]), Is.GreaterThan(0.04f), families[a] + " vs " + families[b]);
                }
            }

            Assert.That(ShapeRaster.Difference(ShapeRaster.Mask(BloomlingArt.Silhouette(Family.Bloom, "flower"), 64, true), ShapeRaster.Mask(BloomlingArt.Silhouette(Family.Bloom, "bud"), 64, true)), Is.GreaterThan(0.02f));
            Assert.That(ShapeRaster.Difference(ShapeRaster.Mask(BloomlingArt.Silhouette(Family.Twig, "log"), 64, true), ShapeRaster.Mask(BloomlingArt.Silhouette(Family.Twig, "acorn"), 64, true)), Is.GreaterThan(0.02f));
        }

        [Test]
        public void TheCharacterShapes_FollowTheDrawnFigure()
        {
            // The char.* slots (skins, hit areas) are the figure's outer edge.
            foreach (Family family in new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig })
            {
                byte[] mask = ShapeRaster.Mask(ShapeLibrary.SilhouetteId(family), Size, topDown: true);
                byte[] rgba = BloomlingArt.Render(new BloomlingLook(family, Rgba.FromHex("#3D82E0")), Size, topDown: true, premultiplied: true);
                var drawn = new byte[Size * Size];
                for (int i = 0; i < drawn.Length; i++)
                {
                    drawn[i] = rgba[(i * 4) + 3];
                }

                Assert.That(ShapeRaster.Difference(mask, drawn), Is.LessThan(0.02f), family.ToString());
            }
        }

        [Test]
        public void Rendering_IsDeterministic_AndKeyedByEverythingItShows()
        {
            var look = new BloomlingLook(Family.Drop, Rgba.FromHex("#74F9F9"), "dew", BloomlingMood.Worried, Halo: true);
            Assert.That(BloomlingArt.Render(look, 48, true, true), Is.EqualTo(BloomlingArt.Render(look, 48, true, true)));
            string[] keys =
            {
                look.Key,
                (look with { Mood = BloomlingMood.Happy }).Key,
                (look with { Halo = false }).Key,
                (look with { Badge = false }).Key,
                (look with { Color = Rgba.FromHex("#3D82E0") }).Key,
                (look with { IconId = "drop" }).Key,
            };
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
        }

        [Test]
        public void ThePremultipliedPicture_NeverExceedsItsAlpha()
        {
            byte[] rgba = BloomlingArt.Render(new BloomlingLook(Family.Twig, Rgba.FromHex("#B55A11"), "acorn"), 64, topDown: false, premultiplied: true);
            for (int i = 0; i < rgba.Length; i += 4)
            {
                Assert.That(rgba[i] <= rgba[i + 3] && rgba[i + 1] <= rgba[i + 3] && rgba[i + 2] <= rgba[i + 3], Is.True);
            }
        }

        [Test]
        public void OnACard_TheBadgeClearsTheCountPill()
        {
            var face = new Box(0f, 0f, 200f, 190f);
            Box figure = BloomlingArt.OnCard(face);
            Box symbol = BloomlingArt.SymbolBox(figure);
            float badgeBottom = symbol.CenterY + (symbol.Width / BloomlingArt.SymbolShare / 2f);
            Assert.That(badgeBottom, Is.LessThan(face.Bottom - (face.Height * BloomlingArt.CardPillShare)));
            Assert.That(figure.Top, Is.GreaterThanOrEqualTo(face.Top - (figure.Height * 0.05f)));
            Assert.That(figure.Width, Is.GreaterThan(face.Width * 0.7f));
        }

        private static (int X, int Y) Pixel(float designX, float designY)
        {
            float pixel = 2f * ShapeRaster.Margin / Size;
            int x = (int)(((designX * BloomlingArt.Fit) + ShapeRaster.Margin) / pixel);
            int y = (int)((ShapeRaster.Margin - (designY * BloomlingArt.Fit)) / pixel);
            return (x, y);
        }

        private static int Alpha(byte[] rgba, int x, int y) => rgba[(((y * Size) + x) * 4) + 3];

        private static Rgba Color(byte[] rgba, int x, int y)
        {
            int i = ((y * Size) + x) * 4;
            return new Rgba(rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]);
        }

        private static float Coverage(byte[] rgba)
        {
            int inside = 0;
            for (int i = 3; i < rgba.Length; i += 4)
            {
                if (rgba[i] >= 128)
                {
                    inside++;
                }
            }

            return (float)inside / (rgba.Length / 4);
        }
    }
}
