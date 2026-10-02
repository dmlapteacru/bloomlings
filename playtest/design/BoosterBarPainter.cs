using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The booster bar of frame 14 (spec 002 FR-014) as the reference's booster tiles on the tray's parchment (spec 005
    /// contracts/look.md §3.7, §3.8, research D9; <see cref="Kit.BoosterTile"/>; spec 003 FR-031, contracts/booster-tile.md).
    /// <list type="bullet">
    /// <item><description>It is hidden before the first booster unlocks.</description></item>
    /// <item><description>Each booster appears at its unlock level as a cream tile in a silver rim with its colored icon:
    /// Extra Slot a white "+" on a blue disc, Shuffle two chasing arrows, Return a yellow arrow, Bloom Burst a pink
    /// flower.</description></item>
    /// <item><description>A dark green count badge shows the charges as plain digits. With none left, a cream cost pill with
    /// the lotus and a green "+" show instead, so buying stays clear.</description></item>
    /// <item><description>A booster the level cannot use now is greyed (spec 001 FR-046).</description></item>
    /// <item><description>The booster whose target is being chosen is raised, with a pulsing golden glow.</description></item>
    /// </list>
    /// </summary>
    public static class BoosterBarPainter
    {
        /// <summary>
        /// The booster row of the tray (spec 005 FR-020, contracts/look.md §6.1): <paramref name="places"/> are the four
        /// cream boxes of <see cref="ReferenceGameplayRegions.Boosters"/>; unlocked boosters take theirs in order
        /// (frame 14).
        /// </summary>
        public static void Draw(IPainter p, IReadOnlyList<Box> places, LevelScreen s)
        {
            var shown = new List<(BoosterKind Kind, Recovery Recovery, string Id)>();
            foreach ((BoosterKind kind, Recovery recovery, string id) in LevelScreen.Boosters)
            {
                if (s.Meta.Economy.IsUnlocked(kind))
                {
                    shown.Add((kind, recovery, id));
                }
            }

            IReadOnlyList<Recovery> eligible = s.Session.EligibleRecoveries();
            for (int i = 0; i < shown.Count && i < places.Count; i++)
            {
                (BoosterKind kind, Recovery recovery, string id) = shown[i];
                int charges = s.Meta.Economy.Charges(kind);
                var state = new BoosterTileState(
                    charges,
                    s.Meta.Economy.Price(kind),
                    selected: s.Targeting == recovery,
                    usable: Applicable(s, recovery, eligible) && s.Session.Status != LevelStatus.Won,
                    affordable: charges > 0 || s.Meta.Economy.CanAfford(kind));
                Tile(p, places[i], id, state, state.Disabled ? (Action?)null : () => s.PressBooster(kind, recovery));
            }
        }

        /// <summary>One booster tile in its state (contracts/booster-tile.md "Layers", spec 005 §3.7).</summary>
        public static void Tile(IPainter p, Box box, string id, BoosterTileState state, Action? action) =>
            Kit.BoosterTile(p, box, id, state, action);

        private static bool Applicable(LevelScreen s, Recovery recovery, IReadOnlyList<Recovery> eligible)
        {
            if (s.Session.Status == LevelStatus.Jammed || s.Session.Status == LevelStatus.Stuck)
            {
                return eligible.Contains(recovery);
            }

            return recovery switch
            {
                Recovery.ExtraSlot => s.Session.Check(new UseExtraSlot()).IsAllowed,
                Recovery.Shuffle => s.Session.Check(new UseShuffle()).IsAllowed,
                _ => true,
            };
        }
    }
}
