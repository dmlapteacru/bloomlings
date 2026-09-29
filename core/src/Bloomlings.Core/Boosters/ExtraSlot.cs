using System.Collections.Generic;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Extra Slot (FR-043, T115): a sixth usable slot until the end of the level, at most once per level. It is
    /// disabled when it could have no effect (FR-046): when no pod is left in the tray, and on a Jammed or Stuck board
    /// when no exposed pod could use the new slot (only locked pods, or groups too big even with it), since the board
    /// would stay jammed.
    /// </summary>
    internal static class ExtraSlot
    {
        public static bool CanApply(LevelState state)
        {
            if (state.ExtraSlotUsed)
            {
                return false;
            }

            if (state.Status != LevelStatus.Playing)
            {
                return RecoveryCheck.ExposedPodFits(state, state.Slots.FreeCount + 1, -1);
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
