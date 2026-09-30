using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// The render checks of one frame (spec 002 FR-027, SC-007; contracts/painter.md, "Recording"):
    /// <list type="bullet">
    /// <item><description>every drawn shape is in the shape library;</description></item>
    /// <item><description>every marked slot is registered;</description></item>
    /// <item><description>touch targets meet the minimum size and do not overlap;</description></item>
    /// <item><description>text and targets stay inside the safe area.</description></item>
    /// </list>
    /// Catch-all targets are left out of the target checks: the scrims, the card and sheet bodies, and a demo's
    /// tap-anywhere.
    /// </summary>
    public static class Checks
    {
        public static IEnumerable<string> Run(SkiaPainter p, Fixture frame, string shape)
        {
            string at = frame.Number.ToString("00") + "-" + frame.Slug + " " + shape + ": ";
            foreach (string id in p.UnknownShapes)
            {
                yield return at + "unknown shape " + id;
            }

            foreach (string id in p.Slots.Where(s => !AssetSlots.Has(s)))
            {
                yield return at + "unregistered slot " + id;
            }

            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            float screen = p.Width * p.Height;
            float min = DesignTokens.Size.TouchMin * p.Scale * 0.95f;
            // Only reachable targets count: those not covered by a later catch-all (a scrim, a card or sheet body).
            var reachable = new List<Box>();
            for (int i = 0; i < p.Targets.Count; i++)
            {
                Box t = p.Targets[i];
                bool covered = false;
                for (int j = i + 1; j < p.Targets.Count && !covered; j++)
                {
                    Box later = p.Targets[j];
                    covered = later.Width * later.Height >= screen * 0.12f && (t.Within(later) || t.Overlaps(later));
                }

                if (!covered)
                {
                    reachable.Add(t);
                }
            }

            List<Box> targets = reachable.Where(t => t.Width * t.Height < screen * 0.12f).Distinct().ToList();
            foreach (Box t in targets)
            {
                if (Math.Min(t.Width, t.Height) < min)
                {
                    yield return at + "touch target " + t + " is smaller than " + min.ToString("0") + " px";
                }

                if (!t.Within(safe))
                {
                    yield return at + "touch target " + t + " leaves the safe area " + safe;
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                for (int j = i + 1; j < targets.Count; j++)
                {
                    if (targets[i].Overlaps(targets[j]) && !Nested(targets[i], targets[j]))
                    {
                        yield return at + "touch targets overlap: " + targets[i] + " and " + targets[j];
                    }
                }
            }

            foreach ((Box box, string text) in p.Texts)
            {
                if (box.Left < safe.Left - 1f || box.Right > safe.Right + 1f || box.Top < safe.Top - 1f || box.Bottom > safe.Bottom + 1f)
                {
                    yield return at + "text \"" + text + "\" " + box + " leaves the safe area";
                }
            }
        }

        /// <summary>A target inside a larger one (a button on a row) is fine: the topmost gets the tap.</summary>
        private static bool Nested(Box a, Box b) => a.Within(b) || b.Within(a);
    }
}
