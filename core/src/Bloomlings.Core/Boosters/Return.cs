using System.Collections.Generic;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Return (FR-045, T115): the unfinished pod in a slot goes back to the top of its original stack with its remaining
    /// count; tiles it cleared stay cleared. A returned member of a connected group comes back alone.
    /// </summary>
    internal static class Return
    {
        public static bool CanApply(LevelState state, int slot) =>
            slot >= 0 && slot < WaitingSlots.Capacity && state.Slots.StateOf(slot) == SlotState.Occupied;

        public static void Apply(LevelState state, int slot, List<GameEvent> events)
        {
            int pod = state.Slots.PodIn(slot);
            state.ReturnPod(pod);
            events.Add(new PodReturned(0, state.PodId(pod), state.Pods[pod].HomeStack));
        }
    }
}
