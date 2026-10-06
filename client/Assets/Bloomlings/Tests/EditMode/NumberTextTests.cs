using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Numbers grouped as on the design board (spec 002 edge cases).</summary>
    public class NumberTextTests
    {
        [TestCase(0, "0")]
        [TestCase(240, "240")]
        [TestCase(1240, "1 240")]
        [TestCase(5000, "5 000")]
        [TestCase(12345, "12 345")]
        [TestCase(123456, "123 456")]
        [TestCase(1234567, "1 234 567")]
        [TestCase(-1240, "-1 240")]
        public void Group_InsertsANoBreakSpaceEveryThreeDigits(long value, string expected)
        {
            Assert.That(NumberText.Group(value), Is.EqualTo(expected));
        }

        [Test]
        public void Plus_PrefixesRewards()
        {
            Assert.That(NumberText.Plus(35), Is.EqualTo("+35"));
            Assert.That(NumberText.Plus(1200), Is.EqualTo("+1 200"));
        }
    }
}
