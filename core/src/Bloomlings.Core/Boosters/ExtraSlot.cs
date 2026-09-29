using System.Collections.Generic;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Extra Slot (FR-043, T115): a sixth usable slot until the end of the level, at most once per level. It is
    /// disabled when no pod is left in the tray, because it could have no effect (FR-046).
    /// </summary>
    internal static class ExtraSlot
    {
        public static bool CanApply(LevelState state)
        {
            if (state.ExtraSlotUsed)
            {
                return false;
            }

            foreach (PodRuntime pod in state.Pods)
            {
                if (pod.Location == PodLocation.Tray)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Apply(LevelState state, List<GameEvent> events)
        {
            state.AddExtraSlot();
            state.MarkExtraSlotUsed();
            events.Add(new ExtraSlotAdded(0, WaitingSlots.ExtraSlotIndex));
        }
    }
}
