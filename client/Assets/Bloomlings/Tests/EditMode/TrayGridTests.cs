using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity tray's pod grid (spec 005 FR-021, the owner's gameplay rule of 2026-10-03): the grid the HUD hands
    /// <c>TrayView</c> (<see cref="GameplayHud.InPodRow(ReferenceGameplayRegions, float)"/>) puts every pod where the
    /// reference layout does, in the tray area's own units, one after another in its column and inside the pod row.
    /// </summary>
    public class TrayGridTests
    {
        /// <summary>The canvas units per screen pixel of a 1440 px wide phone (the canvas is 1080 units wide).</summary>
        private const float UnitsPerPixel = 0.75f;

        [Test]
        public void PodGrid_InTheTrayArea_IsTheReferenceGrid()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 21f / 9f })
            {
                for (int stacks = 1; stacks <= 6; stacks++)
                {
                    float h = 1080f * ratio;
                    string at = ratio.ToString("0.00") + ", " + stacks + " stacks";
                    ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(1080f, h, new Insets(60f, 40f), stacks, 5, true, false);
                    ReferenceGameplayRegions local = GameplayHud.InPodRow(r, UnitsPerPixel);
                    Assert.That(local.PodRows, Is.EqualTo(r.PodRows), at);
                    Assert.That(local.PodRows, Is.EqualTo(ratio >= 2.1f ? 4 : 3), at + ": four rows from 19.5:9, three on shorter phones");
                    Assert.That(local.PodRow.Left, Is.EqualTo(0f).Within(0.01f), at);
                    Assert.That(local.PodRow.Top, Is.EqualTo(0f).Within(0.01f), at);
                    for (int s = 0; s < stacks; s++)
                    {
                        for (int d = 0; d < local.PodRows; d++)
                        {
                            Box screen = r.Pod(s, d);
                            var expected = new Box((screen.Left - r.PodRow.Left) * UnitsPerPixel, (screen.Top - r.PodRow.Top) * UnitsPerPixel, (screen.Right - r.PodRow.Left) * UnitsPerPixel, (screen.Bottom - r.PodRow.Top) * UnitsPerPixel);
                            Box pod = local.Pod(s, d);
                            Assert.That(pod.Left, Is.EqualTo(expected.Left).Within(0.05f), at);
                            Assert.That(pod.Top, Is.EqualTo(expected.Top).Within(0.05f), at);
                            Assert.That(pod.Right, Is.EqualTo(expected.Right).Within(0.05f), at);
                            Assert.That(pod.Bottom, Is.EqualTo(expected.Bottom).Within(0.05f), at);
                            Assert.That(pod.Bottom, Is.LessThanOrEqualTo(local.PodRow.Bottom + 0.5f), at + ": inside the pod row");
                            if (d > 0)
                            {
                                Assert.That(pod.Top, Is.GreaterThan(local.Pod(s, d - 1).Bottom), at + ": never on the pod before it");
                            }
                        }
                    }
                }
            }
        }
    }
}
