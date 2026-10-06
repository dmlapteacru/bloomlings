using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// Whether Home and the splash stand heroes (spec 005 FR-024, FR-028, <see cref="HomeStage.ShowsHeroes"/>): the
    /// animated heroes over the owner's layered Home (the owner, 2026-10-02), the still heroes on the drawn stand-in, and
    /// none over an owner picture without the fountain's layers (no fountain to stand them in).
    /// </summary>
    public class HomeStageHeroesTests
    {
        [Test]
        public void HeroesStand_OverTheLayeredHome_AndOnTheDrawnStandIn_ButNotOverABareOwnerPicture()
        {
            Assert.That(HomeStage.ShowsHeroes(ownerPicture: true, layered: true), Is.True, "the owner's layered Home stands the animated heroes");
            Assert.That(HomeStage.ShowsHeroes(ownerPicture: true, layered: false), Is.False, "an owner picture without the fountain's layers shows none");
            Assert.That(HomeStage.ShowsHeroes(ownerPicture: true), Is.False, "the layers are opt-in: a host that does not draw them shows none");
            Assert.That(HomeStage.ShowsHeroes(ownerPicture: false), Is.True, "the drawn stand-in keeps its still heroes");
            Assert.That(HomeStage.ShowsHeroes(ownerPicture: false, layered: true), Is.True, "the drawn stand-in keeps its still heroes");
        }
    }
}
