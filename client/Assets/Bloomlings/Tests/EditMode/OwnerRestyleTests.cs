using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The owner's requests of 2026-10-08 (spec 005 FR-049, contracts/look.md §6.22): the Wardrobe's pedestal in the soft
    /// volume of the owner's fountain, the avatar picker's bigger cells.
    /// </summary>
    public class OwnerRestyleTests
    {
        [Test]
        public void ThePedestal_IsSoftCreamStone_LitOnTop_WithoutMoss()
        {
            const int w = 420;
            const int h = 170;
            byte[] pedestal = UiRaster.Pedestal(w, h, 3);
            Assert.That(pedestal, Is.EqualTo(UiRaster.Pedestal(w, h, 3)), "deterministic");
            Assert.That(pedestal[3], Is.Zero, "the top-left corner is outside the drum");
            Assert.That(pedestal[((w * h) - 1) * 4 + 3], Is.Zero, "the bottom-right corner too");

            // The top's slab is lighter than the side's front.
            float ry = System.Math.Min((w / 2f - 1f) * 0.28f, (h - 4f) * 0.3f);
            float top = Luma(pedestal, w, w / 2, (int)(ry + 1f));
            float front = Luma(pedestal, w, w / 2, (int)((h - ry) * 0.5f + ry));
            Assert.That(top, Is.GreaterThan(front), "lit from above");

            // Stone colors only: no green moss anywhere (it had moss patches before FR-049).
            for (int i = 0; i < pedestal.Length; i += 4)
            {
                if (pedestal[i + 3] < 128)
                {
                    continue;
                }

                Assert.That(pedestal[i + 1], Is.LessThanOrEqualTo(pedestal[i] + 4), "no green at " + (i / 4));
            }
        }

        [Test]
        public void TheSeedTurnsTheBlocks_NotTheStone()
        {
            byte[] a = UiRaster.Pedestal(300, 120, 1);
            byte[] b = UiRaster.Pedestal(300, 120, 4);
            Assert.That(a, Is.Not.EqualTo(b), "the joints turn with the seed");
            int opaqueA = 0;
            int opaqueB = 0;
            for (int i = 3; i < a.Length; i += 4)
            {
                opaqueA += a[i] > 128 ? 1 : 0;
                opaqueB += b[i] > 128 ? 1 : 0;
            }

            Assert.That(opaqueA, Is.EqualTo(opaqueB), "the same drum");
        }

        [Test]
        public void TheAvatarPicker_TakesThreeBigColumnsOnATallPhone()
        {
            ProfileEditRegions tall = ScreenLayout.ProfileEdit(1080f, 2340f, new Insets(110f, 60f));
            Assert.That(tall.Columns, Is.EqualTo(3), "a 19.5:9 phone");
            Assert.That(tall.CellSize, Is.GreaterThan(ScreenLayout.ProfileEdit(1080f, 2340f, new Insets(110f, 60f), 4).CellSize * 1.2f), "visibly bigger than four a row");
            Assert.That(tall.NameButton.Height, Is.EqualTo(ProfileEditRegions.CellUnits * tall.Unit * 0.7f).Within(1e-3), "the Name tab keeps its size");
        }

        private static float Luma(byte[] rgba, int width, int x, int y)
        {
            int i = ((y * width) + x) * 4;
            return (0.299f * rgba[i]) + (0.587f * rgba[i + 1]) + (0.114f * rgba[i + 2]);
        }
    }
}
