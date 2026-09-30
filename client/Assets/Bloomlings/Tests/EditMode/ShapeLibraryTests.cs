using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The placeholder shapes (spec 002 FR-002, research R2; spec 001 FR-005, FR-072).</summary>
    public class ShapeLibraryTests
    {
        [Test]
        public void EveryShape_RasterizesToAVisibleMask()
        {
            foreach (string id in ShapeLibrary.Ids)
            {
                float coverage = ShapeRaster.Coverage(ShapeRaster.Mask(id, 48, topDown: true));
                Assert.That(coverage, Is.GreaterThan(0.03f).And.LessThan(0.98f), id);
            }
        }

        [Test]
        public void EveryVariant_HasItsOwnSymbol_DistinctFromEveryOther()
        {
            string[] ids = VariantCatalog.Default.All.Select(v => ShapeLibrary.SymbolId(v.IconId)).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            foreach (string id in ids)
            {
                Assert.That(ShapeLibrary.Has(id), Is.True, id);
            }

            byte[][] masks = ids.Select(id => ShapeRaster.Mask(id, 48, topDown: true)).ToArray();
            for (int a = 0; a < masks.Length; a++)
            {
                for (int b = a + 1; b < masks.Length; b++)
                {
                    Assert.That(ShapeRaster.Difference(masks[a], masks[b]), Is.GreaterThan(0.08f), ids[a] + " vs " + ids[b]);
                }
            }
        }

        [Test]
        public void ThePetalCurrency_DiffersFromTheFlowerVariant()
        {
            Assert.That(ShapeRaster.Difference(ShapeRaster.Mask("currency.petal", 48, true), ShapeRaster.Mask("symbol.flower", 48, true)), Is.GreaterThan(0.08f));
        }

        [Test]
        public void UnknownIds_FallBackToTheMysteryShape()
        {
            Assert.That(ShapeLibrary.Has("ui.nope"), Is.False);
            Assert.That(ShapeRaster.Mask("ui.nope", 32, true), Is.EqualTo(ShapeRaster.Mask(ShapeLibrary.Fallback, 32, true)));
        }

        [Test]
        public void Masks_FlipBetweenTextureAndBitmapRows()
        {
            byte[] up = ShapeRaster.Mask("ui.play", 32, topDown: false);
            byte[] down = ShapeRaster.Mask("ui.play", 32, topDown: true);
            for (int row = 0; row < 32; row++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Assert.That(down[(row * 32) + x], Is.EqualTo(up[((31 - row) * 32) + x]));
                }
            }
        }
    }
}
