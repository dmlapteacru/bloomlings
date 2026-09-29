using System;
using UnityEngine;

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
                new DemoStep("Extra Slot: one more slot for this level") { PointAt = button },
                new DemoStep("Here is a free one")),
            "booster.shuffle" => new DemoScript(
                unlockId,
                new DemoStep("Shuffle rearranges the pods in the tray") { PointAt = button },
                new DemoStep("It always leaves a way to win")),
            "booster.return" => new DemoScript(
                unlockId,
                new DemoStep("Return sends a waiting pod back to the tray") { PointAt = button },
                new DemoStep("Then tap the slot to send back")),
            "booster.bloom_burst" => new DemoScript(
                unlockId,
                new DemoStep("Bloom Burst clears one symbol from the whole garden") { PointAt = button },
                new DemoStep("Then tap a tile with that symbol")),
            _ => null,
        };
    }
}
