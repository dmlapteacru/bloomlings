using System;
using UnityEngine;
using Bloomlings.Client.UI.Localization;

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

        public Func<RectTransform?> Chest { get; set; } = () => null;

        /// <summary>The Statue or Bridge (the L250 environmental object).</summary>
        public Func<RectTransform?> Environment2 { get; set; } = () => null;

        public Func<RectTransform?> TriplePod { get; set; } = () => null;
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
                new DemoStep(Loc.T("demo.stone")) { PointAt = at.Stone }),
            "mechanic.key" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.key.1")) { PointAt = at.KeyTile },
                new DemoStep(Loc.T("demo.key.2")) { PointAt = at.KeyLock }),
            "mechanic.locked_pod" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.locked_pod.1")) { PointAt = at.LockedPod },
                new DemoStep(Loc.T("demo.locked_pod.2")) { PointAt = at.KeyTile }),
            "mechanic.connected_pair" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.connected_pair.1")) { PointAt = at.ConnectedPod },
                new DemoStep(Loc.T("demo.connected_pair.2"))),
            "mechanic.layered_tile" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.layered_tile.1")) { PointAt = at.LayeredTile },
                new DemoStep(Loc.T("demo.layered_tile.2")) { PointAt = at.LayeredTile }),
            "mechanic.gate" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.gate")) { PointAt = at.Gate }),
            "mechanic.fountain" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.fountain.1")) { PointAt = at.Fountain },
                new DemoStep(Loc.T("demo.fountain.2"))),
            "mechanic.locked_slot" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.locked_slot")) { PointAt = at.LockedSlot }),
            "mechanic.mystery_tile" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.mystery_tile")) { PointAt = at.MysteryTile }),
            "mechanic.mystery_pod" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.mystery_pod")) { PointAt = at.MysteryPod }),
            "mechanic.chest" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.chest.1")) { PointAt = at.Chest },
                new DemoStep(Loc.T("demo.chest.2")) { PointAt = at.Chest }),
            "mechanic.environment_2" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.environment_2")) { PointAt = at.Environment2 }),
            "mechanic.connected_triple" => new DemoScript(
                unlockId,
                new DemoStep(Loc.T("demo.connected_triple.1")) { PointAt = at.TriplePod },
                new DemoStep(Loc.T("demo.connected_triple.2"))),
            _ => null,
        };
    }
}
