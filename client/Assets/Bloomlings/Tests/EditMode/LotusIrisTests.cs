using System.Linq;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The lotus loader and the lotus iris between levels (spec 005 FR-039, contracts/look.md §6.13).</summary>
    public class LotusIrisTests
    {
        private static readonly (float W, float H, Insets Insets)[] Shapes =
        {
            (1080f, 1920f, new Insets(63f, 0f)),
            (1080f, 2340f, new Insets(110f, 63f)),
            (1080f, 2520f, new Insets(120f, 66f)),
            (1536f, 2048f, new Insets(0f, 0f)),
        };

        [Test]
        public void TheTransition_ClosesHoldsAndOpens_AndTheNextLevelComesUpUnderTheClosedCover()
        {
            Assert.That(LotusIris.Transition(0f).Open, Is.EqualTo(1f));
            Assert.That(LotusIris.Transition(LotusIris.TransitionSeconds).Open, Is.EqualTo(1f));
            Assert.That(LotusIris.Transition(LotusIris.TransitionSeconds).Covers, Is.False);
            for (float t = LotusIris.CloseSeconds; t <= LotusIris.OpenAt; t += 0.05f)
            {
                Assert.That(LotusIris.Transition(t).Open, Is.EqualTo(0f), "closed at " + t);
            }

            Assert.That(LotusIris.SwitchAt, Is.InRange(LotusIris.CloseSeconds, LotusIris.OpenAt));
            float last = 1f;
            for (float t = 0f; t <= LotusIris.CloseSeconds; t += 0.02f)
            {
                float open = LotusIris.Transition(t).Open;
                Assert.That(open, Is.LessThanOrEqualTo(last + 1e-6f), "closing at " + t);
                last = open;
            }

            last = 0f;
            for (float t = LotusIris.OpenAt; t <= LotusIris.TransitionSeconds; t += 0.02f)
            {
                float open = LotusIris.Transition(t).Open;
                Assert.That(open, Is.GreaterThanOrEqualTo(last - 1e-6f), "opening at " + t);
                last = open;
            }

            Assert.That(LotusIris.TransitionSeconds, Is.LessThanOrEqualTo(2f), "short enough to play between every two levels");
        }

        [Test]
        public void NothingOfTheCoverShowsOverTheScreen_ThroughTheHole()
        {
            // The lotus, the glow, the ring and the text are drawn over the cover without a mask: they only show once the
            // iris is closed, and they are gone before the opening hole reaches them.
            for (float t = 0f; t < LotusIris.CloseSeconds; t += 0.01f)
            {
                LotusPose pose = LotusIris.Transition(t);
                Assert.That(pose.LotusAlpha + pose.GlowAlpha + pose.RingAlpha + pose.TextAlpha, Is.EqualTo(0f), "closing at " + t);
            }

            foreach ((float w, float h, Insets insets) in Shapes)
            {
                LotusIrisLayout l = LotusIris.Layout(w, h, insets);
                for (float t = LotusIris.OpenAt; t <= LotusIris.TransitionSeconds; t += 0.01f)
                {
                    LotusPose pose = LotusIris.Transition(t);
                    float hole = l.HoleRadius(pose);
                    if (hole > l.RingRadius - (l.Petal / 2f))
                    {
                        Assert.That(pose.RingAlpha, Is.EqualTo(0f), "the ring at " + t);
                    }

                    if (hole > l.TextY - l.CenterY - (l.Unit * 60f))
                    {
                        Assert.That(pose.TextAlpha, Is.EqualTo(0f), "the text at " + t);
                    }
                }
            }
        }

        [Test]
        public void TheSplash_FillsItsRingWithTheLoading_ThenOpensOnTheFirstScreen()
        {
            Assert.That(LotusIris.SplashProgress(0f, 1f), Is.EqualTo(0f));
            Assert.That(LotusIris.SplashProgress(LotusIris.SplashFillFrom + LotusIris.SplashFillSeconds, 1f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(LotusIris.SplashProgress(10f, 0.6f), Is.EqualTo(0.6f).Within(1e-5f), "never ahead of the loading");
            Assert.That(LotusIris.SplashFull(10f, 0.99f), Is.False);
            Assert.That(LotusIris.SplashFull(LotusIris.SplashFillFrom + 0.3f, 1f), Is.False, "the ring always fills petal by petal");
            Assert.That(LotusIris.SplashFull(LotusIris.SplashFillFrom + LotusIris.SplashFillSeconds, 1f), Is.True);

            for (float t = 0f; t < 3f; t += 0.05f)
            {
                Assert.That(LotusIris.Splash(t, 0.5f).Open, Is.EqualTo(0f), "covered while it loads");
            }

            const float open = 1.7f;
            Assert.That(LotusIris.Splash(open, 1f, open).Open, Is.EqualTo(0f));
            Assert.That(LotusIris.Splash(open + LotusIris.SplashOpenSeconds, 1f, open).Open, Is.EqualTo(1f));
            Assert.That(LotusIris.Splash(open + LotusIris.SplashOpenSeconds, 1f, open).LogoAlpha, Is.EqualTo(0f));
            Assert.That(LotusIris.SplashDone(open + LotusIris.SplashOpenSeconds, open), Is.True);
            Assert.That(LotusIris.SplashDone(open + 0.1f, open), Is.False);
            Assert.That(LotusIris.Splash(1.2f, 1f).LogoAlpha, Is.EqualTo(1f), "the logo shows while it loads");
        }

        [Test]
        public void TheRing_LightsItsPetalsOneByOne()
        {
            Assert.That(Enumerable.Range(0, LotusIris.PetalCount).Count(i => LotusIris.PetalState(i, 0f).Lit > 0f), Is.EqualTo(0));
            Assert.That(Enumerable.Range(0, LotusIris.PetalCount).Count(i => LotusIris.PetalState(i, 0.5f).Lit >= 1f), Is.EqualTo(LotusIris.PetalCount / 2));
            Assert.That(Enumerable.Range(0, LotusIris.PetalCount).All(i => LotusIris.PetalState(i, 1f).Lit >= 1f), Is.True);
            (float lit, float scale) = LotusIris.PetalState(3, 3.5f / LotusIris.PetalCount);
            Assert.That(lit, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(scale, Is.GreaterThan(1.3f), "the newest petal swells as it lights");
        }

        [Test]
        public void TheLayout_KeepsTheLotusRingAndTextOnTheSafeScreen_AndTheOpenIrisShowsEverything()
        {
            foreach ((float w, float h, Insets insets) in Shapes)
            {
                LotusIrisLayout l = LotusIris.Layout(w, h, insets);
                Box safe = ScreenLayout.SafeArea(w, h, insets);
                Assert.That(l.RingRadius - (l.Petal / 2f), Is.GreaterThan(l.Lotus.Width / 2f), "the ring stands round the lotus");
                Assert.That(l.TextY, Is.GreaterThan(l.CenterY + l.RingRadius + (l.Petal / 2f)), "the text under the ring");
                Assert.That(l.TextY + (l.Unit * 60f), Is.LessThan(safe.Bottom));
                Assert.That(l.CenterY - l.RingRadius, Is.GreaterThan(safe.Top + (safe.Height * 0.25f)), "room for the logo above");
                foreach ((float x, float y) in new[] { (0f, 0f), (w, 0f), (0f, h), (w, h) })
                {
                    float d = (float)System.Math.Sqrt(((x - l.CenterX) * (x - l.CenterX)) + ((y - l.CenterY) * (y - l.CenterY)));
                    Assert.That(l.MaxRadius, Is.GreaterThan(d + (l.Unit * LotusIris.RimUnits)), "the rim leaves the screen");
                }

                (float px, float py, float turn) = l.PetalAt(0, 0f);
                Assert.That(px, Is.EqualTo(l.CenterX).Within(0.01f));
                Assert.That(py, Is.EqualTo(l.CenterY - l.RingRadius).Within(0.01f), "the first petal at the top");
                Assert.That(turn, Is.EqualTo(0f));
                Assert.That(l.PetalAt(3, 0f).X, Is.EqualTo(l.CenterX + l.RingRadius).Within(0.01f), "clockwise");
            }
        }

        [Test]
        public void TheCover_IsTheHolePictureAndThePanelsRoundIt()
        {
            const float w = 1080f;
            const float h = 2340f;
            foreach (float radius in new[] { 3f, 120f, 600f, 1500f })
            {
                Box hole = LotusIris.HoleBox(540f, 1100f, radius);
                Assert.That(hole.Width * LotusIris.HoleShare / 2f, Is.EqualTo(radius).Within(0.01f));
                Box[] panels = LotusIris.CoverPanels(w, h, hole);
                for (float y = 1f; y < h; y += 37f)
                {
                    for (float x = 1f; x < w; x += 23f)
                    {
                        bool inPicture = x > hole.Left && x < hole.Right && y > hole.Top && y < hole.Bottom;
                        bool inPanel = panels.Any(b => x >= b.Left && x <= b.Right && y >= b.Top && y <= b.Bottom);
                        Assert.That(inPicture || inPanel, Is.True, "covered at " + x + "," + y + " for " + radius);
                        float dx = x - 540f;
                        float dy = y - 1100f;
                        if ((dx * dx) + (dy * dy) < (radius - 2f) * (radius - 2f))
                        {
                            Assert.That(inPanel, Is.False, "the hole stays open at " + x + "," + y);
                        }
                    }
                }
            }

            Assert.That(LotusIris.CoverPanels(w, h, LotusIris.HoleBox(540f, 1100f, 4000f)), Is.Empty);
        }

        [Test]
        public void TheIrisPictures_AreACoverWithARoundHole_AndASoftLight()
        {
            const int size = 64;
            byte[] hole = UiRaster.IrisHole(size, size);
            Assert.That(hole.Length, Is.EqualTo(size * size * 4));
            Assert.That(hole[3], Is.EqualTo(255), "the corner is cover");
            Assert.That(hole[0], Is.EqualTo(LotusIris.Cover.R));
            int middle = (((size / 2) * size) + (size / 2)) * 4;
            Assert.That(hole[middle + 3], Is.EqualTo(0), "the middle is the hole");
            Assert.That(UiRaster.IrisHole(size, size), Is.EqualTo(hole), "deterministic");

            byte[] glow = UiRaster.IrisGlow(size, size);
            Assert.That(glow[middle + 3], Is.GreaterThan(200));
            Assert.That(glow[3], Is.EqualTo(0), "fades to nothing at the edge");
        }
    }
}
