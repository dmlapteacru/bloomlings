using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Resolves automatic work to a fixpoint (research R3). Each round:
    /// 1. compute reachability;
    /// 2. build each active pod's candidates of its exact variant (FR-003), in the fixed order of FR-021;
    /// 3. allocate in slot-age order, each pod claiming min(remaining, unclaimed candidates) (FR-020);
    /// 4. apply all claims at once, emitting TileCleared and then CellOpened or LayerRevealed;
    /// 5. pods at count 0 leave and free their slots (PodCompleted, SlotFreed; FR-022).
    /// The loop ends with the first round that claims nothing and changes nothing.
    /// </summary>
    internal static class Settler
    {
        /// <summary>Runs rounds from 1 and returns the index of the final, quiet round.</summary>
        public static int Settle(LevelState state, IReadOnlyList<IRoundHook> hooks, List<GameEvent> events)
        {
            Board board = state.Board;
            var claimed = new bool[board.CellCount];
            var claimPods = new List<int>();
            var claimTargets = new List<ReachableTarget>();

            for (int round = 1; ; round++)
            {
                ReachabilityResult reach = Reachability.Compute(board);
                var context = new RoundContext(state, round, reach, events);
                foreach (IRoundHook hook in hooks)
                {
                    hook.BeforeAllocation(context);
                }

                // Allocation: oldest slot first; each pod claims its nearest unclaimed tiles of its exact variant.
                claimPods.Clear();
                claimTargets.Clear();
                System.Array.Clear(claimed, 0, claimed.Length);
                foreach (int slot in state.Slots.OccupiedByAge())
                {
                    int pod = state.Slots.PodIn(slot);
                    VariantId variant = state.PodVariant(pod);
                    int need = state.Pods[pod].Remaining;
                    foreach (ReachableTarget target in reach.Targets)
                    {
                        if (need == 0)
                        {
                            break;
                        }

                        int cell = target.Index;
                        if (claimed[cell] || board.IsMysteryHidden(cell) || board.TopLayer(cell) != variant)
                        {
                            continue;
                        }

                        claimed[cell] = true;
                        claimPods.Add(pod);
                        claimTargets.Add(target);
                        need--;
                    }
                }

                if (claimPods.Count == 0 && !context.Changed)
                {
                    return round;
                }

                // Apply all claims at once.
                for (int i = 0; i < claimPods.Count; i++)
                {
                    int pod = claimPods[i];
                    ReachableTarget target = claimTargets[i];
                    VariantId variant = board.TopLayer(target.Index);
                    events.Add(new TileCleared(round, target.Cell, variant, state.PodId(pod), reach.RouteTo(target.Index)));
                    LayerClearResult result = state.ClearTopLayer(target.Index);
                    state.SetRemaining(pod, state.Pods[pod].Remaining - 1);
                    if (result.Opened)
                    {
                        events.Add(new CellOpened(round, target.Cell));
                    }
                    else
                    {
                        events.Add(new LayerRevealed(round, target.Cell, result.Revealed));
                    }

                    foreach (IRoundHook hook in hooks)
                    {
                        hook.OnLayerCleared(context, target.Index, result, pod);
                    }
                }

                foreach (IRoundHook hook in hooks)
                {
                    hook.AfterClears(context);
                }

                // Finished pods leave in slot-age order.
                foreach (int slot in state.Slots.OccupiedByAge())
                {
                    int pod = state.Slots.PodIn(slot);
                    if (state.Pods[pod].Remaining == 0)
                    {
                        state.CompletePod(pod);
                        events.Add(new PodCompleted(round, state.PodId(pod), slot));
                        events.Add(new SlotFreed(round, slot));
                    }
                }
            }
        }
    }
}
