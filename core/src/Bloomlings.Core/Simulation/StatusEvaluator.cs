using System.Collections.Generic;

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

            foreach (int pod in state.Tray.Exposed())
            {
                if (state.CanCommit(pod) == null)
                {
                    return LevelStatus.Playing;
                }
            }

            return LevelStatus.Stuck;
        }

        private static bool AnyBlocksWin(LevelState state, IReadOnlyList<IRoundHook> hooks)
        {
            foreach (IRoundHook hook in hooks)
            {
                if (hook.BlocksWin(state))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
