using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Every rewarded ad's mark is the clapperboard (spec 005 FR-051, the owner, 2026-10-10).</summary>
    public class AdMarkTests
    {
        [Test]
        public void TheAdMark_IsTheClapperboard()
        {
            Assert.That(GardenLook.AdMark, Is.EqualTo("ui.ad"));
            System.Func<float, float, float> mark = ShapeLibrary.Get(GardenLook.AdMark);
            Assert.That(mark(0f, -0.4f), Is.LessThan(0f), "the board");
            Assert.That(mark(0.95f, 0.05f), Is.GreaterThan(0f), "beside the board");

            // The arm is lifted 22° about its hinge at the board's top-left: along its middle line, turned, some points are
            // inside (between the stripes' gaps), and where a closed arm would lie at its far end there is an open gap.
            const float angle = 22f * (float)System.Math.PI / 180f;
            int arm = 0;
            for (int i = 0; i < 40; i++)
            {
                float ax = -0.75f + (i * 0.0375f) + 0.82f;
                const float ay = 0.36f - 0.17f;
                float x = (ax * (float)System.Math.Cos(angle)) - (ay * (float)System.Math.Sin(angle)) - 0.82f;
                float y = (ax * (float)System.Math.Sin(angle)) + (ay * (float)System.Math.Cos(angle)) + 0.17f;
                arm += mark(x, y) < 0f ? 1 : 0;
            }

            Assert.That(arm, Is.GreaterThan(8), "the lifted arm");
            for (float x = 0.3f; x <= 0.7f; x += 0.1f)
            {
                Assert.That(mark(x, 0.36f), Is.GreaterThan(0f), "the open gap under the arm's far end at x " + x);
            }

            // The band and the arm are striped: along the band some points are inside, some in its gaps.
            int inside = 0;
            int gaps = 0;
            for (int i = 0; i < 40; i++)
            {
                float d = mark(-0.75f + (i * 0.0375f), 0f);
                inside += d < 0f ? 1 : 0;
                gaps += d > 0f ? 1 : 0;
            }

            Assert.That(inside, Is.GreaterThan(8), "the band's stripes");
            Assert.That(gaps, Is.GreaterThan(8), "the gaps between them");
        }

        [Test]
        public void TheAdMark_IsABlueSticker_TheSameOnEveryFace()
        {
            var layers = GardenLook.AdMarkLayers;
            Assert.That(layers.Count, Is.EqualTo(3));
            Assert.That(layers[0], Is.EqualTo(("ui.ad.outline", GardenLook.Blue.Line)), "the dark outline at the back");
            Assert.That(layers[1], Is.EqualTo(("ui.ad.base", Rgba.White)), "the white under the stripes");
            Assert.That(layers[2], Is.EqualTo((GardenLook.AdMark, GardenLook.Blue.Face)), "the striped blue clapperboard on top");

            // The white base fills the stripes' gaps and the outline grows round the base.
            System.Func<float, float, float> mark = ShapeLibrary.Get(GardenLook.AdMark);
            System.Func<float, float, float> white = ShapeLibrary.Get("ui.ad.base");
            System.Func<float, float, float> outline = ShapeLibrary.Get("ui.ad.outline");
            for (int i = 0; i < 40; i++)
            {
                float x = -0.75f + (i * 0.0375f);
                Assert.That(white(x, 0f), Is.LessThan(0f), "the band's whole length at x " + x);
                Assert.That(outline(x, 0f), Is.LessThanOrEqualTo(white(x, 0f)), "the outline round it at x " + x);
                Assert.That(white(x, 0f), Is.LessThanOrEqualTo(mark(x, 0f) + 1e-4f), "the stripes inside the base at x " + x);
            }
        }
    }
}
