using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Core.Progression
{
    /// <summary>
    /// Which roadmap unlocks a level needs. Content validation checks them against the unlock roadmap, and the client
    /// uses them to pick the demo of a mechanic the player meets for the first time (FR-031).
    /// </summary>
    public static class LevelMechanics
    {
        /// <summary>
        /// The roadmap unlock ids of the mechanics a level uses (FR-031), sorted: stones from the picture or overlays, keys,
        /// layers (and depth 3), mystery tiles and pods, locked and connected pods, a locked slot and specials. A single
        /// locked cell that opens with a key is the Key mechanic (L8/L14, before keys unlock Source content at L16); a
        /// Garden Gate with a counter is the L35 mechanic.
        /// </summary>
        public static IReadOnlyList<string> UnlocksUsed(LevelDefinition level, BasePicture picture)
        {
            var used = new SortedSet<string>(StringComparer.Ordinal);
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int cell in row)
                {
                    if (cell == BasePicture.Stone)
                    {
                        used.Add("mechanic.stone");
                    }
                }
            }

            foreach (CellOverlay overlay in level.Overlays)
            {
                if (overlay.Stone)
                {
                    used.Add("mechanic.stone");
                }

                if (overlay.KeyId != null)
                {
                    used.Add("mechanic.key");
                }

                if (overlay.LayersBelow.Count > 0)
                {
                    used.Add("mechanic.layered_tile");
                }

                if (overlay.LayersBelow.Count > 1)
                {
                    used.Add("profile.layers_depth_3");
                }

                if (overlay.Mystery)
                {
                    used.Add("mechanic.mystery_tile");
                }
            }

            foreach (PodDef pod in level.Pods)
            {
                if (pod.LockKeyId != null)
                {
                    used.Add("mechanic.locked_pod");
                }

                if (pod.ConnectedGroupId != null)
                {
                    used.Add("mechanic.connected_pair");
                }

                if (pod.Mystery)
                {
                    used.Add("mechanic.mystery_pod");
                }
            }

            if (level.Slots.Locked != null)
            {
                used.Add("mechanic.locked_slot");
            }

            foreach (SpecialDef special in level.Specials)
            {
                bool keyDoor = special.Type == SpecialType.Gate && special.Condition.Kind == SpecialConditionKind.Key && special.Cells.Count == 1;
                used.Add(keyDoor ? "mechanic.key" : special.Type switch
                {
                    SpecialType.Gate => "mechanic.gate",
                    SpecialType.Fountain => "mechanic.fountain",
                    SpecialType.Chest => "mechanic.chest",
                    _ => "mechanic.environment_2",
                });
            }

            return used.ToList();
        }
    }
}
