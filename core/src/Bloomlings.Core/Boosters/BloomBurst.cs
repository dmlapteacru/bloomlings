using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Boosters
{
    /// <summary>
    /// Bloom Burst (FR-050 default, T116): the player picks a visible exact variant; every remaining layer of it, visible
    /// and hidden, leaves the board, and every pod of it leaves the tray and the slots, so the accounting still
    /// reconciles. Where a cell's visible top layer goes, it counts as a clear: its key is collected and special
    /// counters around it advance. Hidden layers that go do not count, as the player never saw them.
    /// </summary>
    internal static class BloomBurst
    {
        public static bool CanApply(LevelState state, VariantId variant)
        {
            if (!state.Catalog.Contains(variant))
            {
                return false;
            }

            Board board = state.Board;
            for (int i = 0; i < board.CellCount; i++)
            {
                if (board.IsTarget(i) && !board.IsMysteryHidden(i) && board.TopLayer(i) == variant)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Apply(LevelState state, VariantId variant, IReadOnlyList<IRoundHook> hooks, List<GameEvent> events)
        {
            Board board = state.Board;
            var hookEvents = new List<GameEvent>();
            var context = new RoundContext(state, 0, Reachability.Compute(board), hookEvents);
            foreach (IRoundHook hook in hooks)
            {
                hook.BeforeAllocation(context);
            }

            var cells = new List<CellPos>();
            for (int i = 0; i < board.CellCount; i++)
            {
                if (!board.IsTarget(i))
                {
                    continue;
                }

                bool topRemoved = board.TopLayer(i) == variant;
                if (state.BurstCell(i, variant) == 0)
                {
                    continue;
                }

                cells.Add(board.PosOf(i));
                if (topRemoved)
                {
                    bool opened = !board.IsTarget(i);
                    var result = new LayerClearResult(variant, opened, opened ? default : board.TopLayer(i));
                    foreach (IRoundHook hook in hooks)
                    {
                        hook.OnLayerCleared(context, i, result, -1);
                    }
                }
            }

            var pods = new List<string>();
            for (int p = 0; p < state.Pods.Length; p++)
            {
                PodLocation location = state.Pods[p].Location;
                if (state.PodVariant(p) == variant && (location == PodLocation.Tray || location == PodLocation.Slot))
                {
                    state.RemovePod(p);
                    pods.Add(state.PodId(p));
                }
            }

            foreach (IRoundHook hook in hooks)
            {
                hook.AfterClears(context);
            }

            events.Add(new VariantBurst(0, variant, cells, pods));
            events.AddRange(hookEvents);
        }
    }
}
