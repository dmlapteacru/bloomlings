using System;
using UnityEngine;

namespace Bloomlings.Client.UI.Tutorial.Demos
{
    /// <summary>Where a mechanic demo points: resolved from the level on screen when the step starts.</summary>
    public sealed class DemoTargets
    {
        public Func<RectTransform?> Stone { get; set; } = () => null;

        public Func<RectTransform?> KeyTile { get; set; } = () => null;

        public Func<RectTransform?> KeyLock { get; set; } = () => null;

        public Func<RectTransform?> LockedPod { get; set; } = () => null;

        public Func<RectTransform?> ConnectedPod { get; set; } = () => null;

        public Func<RectTransform?> LayeredTile { get; set; } = () => null;

        public Func<RectTransform?> Gate { get; set; } = () => null;

        public Func<RectTransform?> Fountain { get; set; } = () => null;

        public Func<RectTransform?> LockedSlot { get; set; } = () => null;

        public Func<RectTransform?> MysteryTile { get; set; } = () => null;

        public Func<RectTransform?> MysteryPod { get; set; } = () => null;
    }

    /// <summary>
    /// One short demo per mechanic, shown once at its roadmap level (FR-031: showcase → practice → combination; T111).
    /// The demo id is the roadmap unlock id, so the save's <c>demosSeen</c> and the roadmap stay in step.
    /// </summary>
    public static class MechanicDemos
    {
        /// <summary>The demo for a roadmap unlock id, or null when the unlock has none.</summary>
        public static DemoScript? For(string unlockId, DemoTargets at) => unlockId switch
        {
            "mechanic.stone" => new DemoScript(
                unlockId,
                new DemoStep("Stones never move. Bloomlings walk around them") { PointAt = at.Stone }),
            "mechanic.key" => new DemoScript(
                unlockId,
                new DemoStep("Clear the tile under the key to collect it") { PointAt = at.KeyTile },
                new DemoStep("The key opens its lock") { PointAt = at.KeyLock }),
            "mechanic.locked_pod" => new DemoScript(
                unlockId,
                new DemoStep("This pod is locked") { PointAt = at.LockedPod },
                new DemoStep("Collect its key on the board to open it") { PointAt = at.KeyTile }),
            "mechanic.connected_pair" => new DemoScript(
                unlockId,
                new DemoStep("Linked pods go to the slots together") { PointAt = at.ConnectedPod },
                new DemoStep("They need a free slot each")),
            "mechanic.layered_tile" => new DemoScript(
                unlockId,
                new DemoStep("Some tiles hide another tile underneath") { PointAt = at.LayeredTile },
                new DemoStep("The corner shows the symbol that comes next") { PointAt = at.LayeredTile }),
            "mechanic.gate" => new DemoScript(
                unlockId,
                new DemoStep("The gate opens when its counter is full") { PointAt = at.Gate }),
            "mechanic.fountain" => new DemoScript(
                unlockId,
                new DemoStep("Restore the shown tiles around the Fountain") { PointAt = at.Fountain },
                new DemoStep("Then it changes the garden")),
            "mechanic.locked_slot" => new DemoScript(
                unlockId,
                new DemoStep("One slot is locked until you collect its key") { PointAt = at.LockedSlot }),
            "mechanic.mystery_tile" => new DemoScript(
                unlockId,
                new DemoStep("A ? tile shows its symbol when Bloomlings can reach it") { PointAt = at.MysteryTile }),
            "mechanic.mystery_pod" => new DemoScript(
                unlockId,
                new DemoStep("A ? pod shows its symbol when it goes to a slot") { PointAt = at.MysteryPod }),
            _ => null,
        };
    }
}
