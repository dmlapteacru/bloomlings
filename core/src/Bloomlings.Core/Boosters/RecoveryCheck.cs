using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Whether a recovery can change a Jammed or Stuck outcome (FR-027, FR-046): after it, some tray pod must be able
    /// to take a slot. A pod (with its active connected group) fits when no member is locked and the group fits the
    /// free slots.
    /// </summary>
    internal static class RecoveryCheck
    {
        /// <summary>
        /// An exposed pod whose whole group is exposed and unlocked and fits <paramref name="free"/> slots, none of it
        /// in <paramref name="coveredStack"/> (the stack a returned pod will cover; -1 for none).
        /// </summary>
        public static bool ExposedPodFits(LevelState state, int free, int coveredStack)
        {
            foreach (int pod in state.Tray.Exposed())
            {
                int[] group = state.ActiveGroup(pod);
                if (group.Length > free)
                {
                    continue;
                }

                bool fits = true;
                foreach (int member in group)
                {
                    if (!state.Tray.IsExposed(member) || state.Tray.StackOf(member) == coveredStack || state.IsPodLocked(member))
                    {
                        fits = false;
                        break;
                    }
                }

                if (fits)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A tray pod that is not exposed yet, whose group is unlocked and fits the free slots.</summary>
        public static bool BuriedPodFits(LevelState state)
        {
            for (int pod = 0; pod < state.Pods.Length; pod++)
            {
                if (state.Pods[pod].Location != PodLocation.Tray || state.Tray.IsExposed(pod))
                {
                    continue;
                }

                int[] group = state.ActiveGroup(pod);
                if (group.Length > state.Slots.FreeCount)
                {
                    continue;
                }

                bool fits = true;
                foreach (int member in group)
                {
                    if (state.IsPodLocked(member))
                    {
                        fits = false;
                        break;
                    }
                }

                if (fits)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
