using System.Linq;
using Bloomlings.Core.Variants;
using Bloomlings.Pipeline.Readability;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests
{
    public class ReadabilityTests
    {
        // Test data of Sharma, Wu and Dalal (2005), pairs 1–3.
        [TestCase(50.0, 2.6772, -79.7751, 50.0, 0.0, -82.7485, 2.0425)]
        [TestCase(50.0, 3.1571, -77.2803, 50.0, 0.0, -82.7485, 2.8615)]
        [TestCase(50.0, 2.8361, -74.0200, 50.0, 0.0, -82.7485, 3.4412)]
        public void DeltaE2000_MatchesReferenceData(double l1, double a1, double b1, double l2, double a2, double b2, double expected)
        {
            Assert.That(ColorScience.DeltaE2000(new Lab(l1, a1, b1), new Lab(l2, a2, b2)), Is.EqualTo(expected).Within(0.0001));
        }

        [Test]
        public void White_IsL100()
        {
            Lab white = ColorScience.ToLab((1, 1, 1));
            Assert.That(white.L, Is.EqualTo(100).Within(0.01));
            Assert.That(white.A, Is.EqualTo(0).Within(0.01));
        }

        [Test]
        public void Report_CoversEveryPairWithDistinctIcons()
        {
            var pairs = ReadabilityCommand.Analyze(VariantCatalog.Default);

            Assert.That(pairs.Count, Is.EqualTo(66), "12 variants give 66 pairs");
            Assert.That(pairs.All(p => p.DistinctIcons), Is.True, "every variant has its own icon (FR-072)");
            Assert.That(pairs.Single(p => p.A == VariantId.Leaf && p.B == VariantId.Moss).SameFamily, Is.True);
        }
    }
}
