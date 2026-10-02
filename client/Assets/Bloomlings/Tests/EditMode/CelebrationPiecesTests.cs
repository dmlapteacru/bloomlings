using System.Collections.Generic;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity twins of the playtest's art-direction recipes (spec 005 T021): the win's confetti above the card, the
    /// group standing on its pedestal, and the shared outfit card's proportions.
    /// </summary>
    public class CelebrationPiecesTests
    {
        [Test]
        public void TheConfetti_FallsAboveTheCard_AndFadesBy2Point2Seconds()
        {
            const float width = 1080f;
            const float cardTop = 700f;
            for (int i = 0; i < ConfettiView.Count; i++)
            {
                foreach (float seconds in new[] { 0f, 0.5f, 1.4f, 2.1f })
                {
                    (Box box, float alpha) = ConfettiView.Bit(i, width, cardTop, seconds, 1f);
                    // The sway may carry a bit just past the left edge, as in the playtest.
                    Assert.That(box.CenterX, Is.InRange(-width * 0.05f, width), "bit " + i + " stays across the screen");
                    Assert.That(box.CenterY, Is.InRange(-40f, cardTop + 40f), "bit " + i + " falls through the heroes' room");
                    Assert.That(box.Height, Is.EqualTo(box.Width * 0.6f).Within(0.001f));
                    Assert.That(alpha, Is.GreaterThan(0f));
                    Assert.That(ConfettiView.Bit(i, width, cardTop, seconds, 1f), Is.EqualTo((box, alpha)), "the same time, the same frame");
                }

                Assert.That(ConfettiView.Bit(i, width, cardTop, ConfettiView.Seconds, 1f).Alpha, Is.EqualTo(0f));
            }
        }

        [Test]
        public void TheConfettiColors_AreEachVariantOnce_ThenTheLotusPinks()
        {
            var pods = new[]
            {
                new PodDef("a", VariantId.Water, 5, false, null, null),
                new PodDef("b", VariantId.Leaf, 6, false, null, null),
                new PodDef("c", VariantId.Water, 4, false, null, null),
            };
            List<Rgba> colors = ConfettiView.ColorsOf(pods);
            Assert.That(colors, Is.EqualTo(new[]
            {
                Rgba.FromHex(VariantCatalog.Default.Get(VariantId.Water).ColorHex),
                Rgba.FromHex(VariantCatalog.Default.Get(VariantId.Leaf).ColorHex),
                DesignTokens.Colors.LotusFill,
                DesignTokens.Colors.PetalCenter,
            }));
        }

        [Test]
        public void TheGroup_StandsOnThePedestal_WithoutABaseOfItsOwn()
        {
            var stage = new Box(80f, 120f, 1000f, 760f);
            (Box pedestal, Box group, float _, float raysY) = HomeStage.Celebration(stage);
            float feet = group.Top + (group.Height * CharacterArt.GroupFeetShare);
            (float topY, float ry) = HomeStage.PedestalTop(pedestal);
            Assert.That(feet, Is.InRange(topY - ry, topY + ry), "the heroes' feet stand on the pedestal's top ellipse");
            Assert.That(pedestal.Bottom, Is.EqualTo(stage.Bottom).Within(0.5f));
            Assert.That(group.Top + (group.Height * CharacterArt.GroupHeadShare), Is.GreaterThanOrEqualTo(stage.Top - 0.5f), "the heads stay in the stage");
            Assert.That(raysY, Is.LessThan(feet), "the rays turn behind the heroes' bodies");
        }

        [Test]
        public void TheOutfitCard_KeepsThePillsRoom_AndItsWellAbove()
        {
            var box = new Box(0f, 0f, 300f, 400f);
            Box card = OutfitCardView.CardBox(box, pillRoom: true);
            Assert.That(card.Height, Is.EqualTo(400f / (1f + (0.6f * OutfitCardView.PillShare))).Within(0.01f));
            Assert.That(OutfitCardView.CardBox(box, pillRoom: false), Is.EqualTo(box));
            Box well = OutfitCardView.WellBox(box, pillRoom: false);
            Assert.That(well.Left, Is.EqualTo(300f * 0.075f).Within(0.01f));
            Assert.That(well.Width, Is.EqualTo(300f * 0.85f).Within(0.01f));
            float face = 400f - (300f * 0.035f);
            Assert.That(well.Height, Is.EqualTo(face * 0.64f).Within(0.01f), "the well is 64% of the face");
            Assert.That(well.Bottom, Is.LessThan(face), "the name has room below the well");
        }
    }
}
