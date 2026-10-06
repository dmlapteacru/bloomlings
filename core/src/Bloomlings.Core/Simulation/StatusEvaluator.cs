using System.Collections.Generic;
using Bloomlings.Core.Tray;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// The end-of-fixpoint check, in this order (FR-025, FR-026, research R3):
    /// 1. Won: every target layer is cleared and no mandatory special is unresolved;
    /// 2. Jammed: every usable slot is occupied. At a fixpoint no active pod has a candidate and nothing is pending,
    ///    so a full buffer is a jam; a full buffer where a pod still progresses never reaches this check;
    /// 3. Stuck: a slot is free but no exposed tray pod can be committed, and work remains.
    /// </summary>
    internal static class StatusEvaluator
    {
        public static LevelStatus Evaluate(LevelState state, IReadOnlyList<IRoundHook> hooks)
        {
            if (state.Board.CountAllLayers() == 0 && !AnyBlocksWin(state, hooks))
            {
                return LevelStatus.Won;
            }

            if (state.Slots.FreeCount == 0)
            {
                return LevelStatus.Jammed;
            }

            // The exposed pods in stack order (SourceTray.Exposed).
            SourceTray tray = state.Tray;
            for (int s = 0; s < tray.StackCount; s++)
            {
                int pod = tray.TopOf(s);
                if (pod >= 0 && state.CanCommit(pod) == null)
                {
                    return LevelStatus.Playing;
                }
            }

            return LevelStatus.Stuck;
        }

        private static bool AnyBlocksWin(LevelState state, IReadOnlyList<IRoundHook> hooks)
        {
            for (int h = 0; h < hooks.Count; h++)
            {
                if (hooks[h].BlocksWin(state))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
