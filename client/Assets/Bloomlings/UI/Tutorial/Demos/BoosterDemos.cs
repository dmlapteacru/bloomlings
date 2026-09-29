using System;
using UnityEngine;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Tutorial.Demos
{
    /// <summary>
    /// The booster demos at their unlock levels (FR-042, T122): L3 Extra Slot, L4 Shuffle, L6 Return, L9 Bloom Burst. Each
    /// comes with its free charge; the demo points at the button. The id is the roadmap unlock id.
    /// </summary>
    public static class BoosterDemos
    {
        public static DemoScript? For(string unlockId, Func<RectTransform?> button) => unlockId switch
        {
            "booster.extra_slot" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.extra_slot.1")) { PointAt = button },
                new DemoStep(Loc.T("demo.extra_slot.2"))),
            "booster.shuffle" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.shuffle.1")) { PointAt = button },
                new DemoStep(Loc.T("demo.shuffle.2"))),
            "booster.return" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.return.1")) { PointAt = button },
                new DemoStep(Loc.T("demo.return.2"))),
            "booster.bloom_burst" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.bloom_burst.1")) { PointAt = button },
                new DemoStep(Loc.T("demo.bloom_burst.2"))),
            _ => null,
        };
    }
}
