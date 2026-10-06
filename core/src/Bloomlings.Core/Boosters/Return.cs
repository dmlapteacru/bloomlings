using System.Collections.Generic;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Return (FR-045, T115): the unfinished pod in a slot goes back to the top of its original stack with its remaining
    /// count; tiles it cleared stay cleared. A returned member of a connected group comes back alone.
    /// On a Jammed or Stuck board a Return only helps when another pod can take the freed slot: the returned pod covers
    /// its home stack, and committing it again would give the same jam. Otherwise it is disabled (FR-046).
    /// </summary>
    internal static class Return
    {
        public static bool CanApply(LevelState state, int slot)
        {
            if (slot < 0 || slot >= WaitingSlots.Capacity || state.Slots.StateOf(slot) != SlotState.Occupied)
            {
                return false;
            }

            if (state.Status == LevelStatus.Playing)
            {
                return true;
            }

            int home = state.Pods[state.Slots.PodIn(slot)].HomeStack;
            return RecoveryCheck.ExposedPodFits(state, state.Slots.FreeCount + 1, home);
        }

        public static void Apply(LevelState state, int slot, List<GameEvent> events)
        {
            int pod = state.Slots.PodIn(slot);
            state.ReturnPod(pod);
            events.Add(new PodReturned(0, state.PodId(pod), state.Pods[pod].HomeStack));
        }
    }
}
