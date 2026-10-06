using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Slots;

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
        /// <summary>
        /// Runs rounds from 1 and returns the index of the final, quiet round. With <paramref name="events"/> null (a
        /// search) the rounds change the state exactly as they would otherwise, but no event is built.
        /// </summary>
        public static int Settle(LevelState state, IReadOnlyList<IRoundHook> hooks, List<GameEvent>? events)
        {
            Board board = state.Board;
            int hookCount = hooks.Count;
            Candidates candidates = Candidates.For(state.Catalog.Count);
            List<int> claimPods = candidates.ClaimPods;
            List<int> claimCells = candidates.ClaimCells;
            int[] slots = candidates.Slots;
            CellPos[] positions = board.Positions;

            for (int round = 1; ; round++)
            {
                // Reachability depends only on the cell kinds; the board keeps it until a kind changes. Only events need
                // the routes.
                ReachabilityResult reach = events != null ? board.Reach : board.ReachTargets;
                var context = new RoundContext(state, round, reach, events);
                for (int h = 0; h < hookCount; h++)
                {
                    hooks[h].BeforeAllocation(context);
                }

                // Allocation: oldest slot first; each pod claims its nearest unclaimed tiles of its exact variant.
                claimPods.Clear();
                claimCells.Clear();
                int occupied = state.Slots.OccupiedByAge(slots);
                if (occupied > 0)
                {
                    candidates.Allocate(state, reach.Packed, slots, occupied);
                }

                if (claimPods.Count == 0 && !context.Changed)
                {
                    return round;
                }

                // Apply all claims at once.
                for (int i = 0; i < claimPods.Count; i++)
                {
                    int pod = claimPods[i];
                    int cell = claimCells[i];
                    events?.Add(new TileCleared(round, positions[cell], board.TopLayer(cell), state.PodId(pod), reach.RouteTo(cell)));
                    LayerClearResult result = state.ClearTopLayer(cell);
                    state.SetRemaining(pod, state.Pods[pod].Remaining - 1);
                    if (events != null)
                    {
                        if (result.Opened)
                        {
                            events.Add(new CellOpened(round, positions[cell]));
                        }
                        else
                        {
                            events.Add(new LayerRevealed(round, positions[cell], result.Revealed));
                        }
                    }

                    for (int h = 0; h < hookCount; h++)
                    {
                        hooks[h].OnLayerCleared(context, cell, result, pod);
                    }
                }

                for (int h = 0; h < hookCount; h++)
                {
                    hooks[h].AfterClears(context);
                }

                // Finished pods leave in slot-age order.
                occupied = state.Slots.OccupiedByAge(slots);
                for (int k = 0; k < occupied; k++)
                {
                    int slot = slots[k];
                    int pod = state.Slots.PodIn(slot);
                    if (state.Pods[pod].Remaining == 0)
                    {
                        state.CompletePod(pod);
                        if (events != null)
                        {
                            events.Add(new PodCompleted(round, state.PodId(pod), slot));
                            events.Add(new SlotFreed(round, slot));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The allocation of one round (FR-020, FR-021). Each pod, oldest slot first, walks the reachable targets in their
        /// fixed order and claims the unclaimed ones whose visible top layer is its exact variant, until its count is
        /// covered. The candidates are first grouped by variant, keeping the target order. A pod then claims the next
        /// ones of its variant: the earlier pods of that variant took the ones before, so these are exactly the first
        /// unclaimed ones, and the claims are the same, in the same order. A hidden mystery tile is never a candidate.
        /// A lone pod walks the targets directly. The work arrays are kept per thread (a settle runs to its end before
        /// another starts on the same thread).
        /// </summary>
        private sealed class Candidates
        {
            [System.ThreadStatic]
            private static Candidates? s_current;

            private readonly int[] _start;
            private readonly int[] _next;
            private int[] _byVariant = new int[64];

            /// <summary>The claims of the round: the pods and their target cells, in order.</summary>
            public List<int> ClaimPods { get; } = new List<int>();

            public List<int> ClaimCells { get; } = new List<int>();

            /// <summary>The occupied slots by age (<see cref="WaitingSlots.OccupiedByAge(int[])"/>).</summary>
            public int[] Slots { get; } = new int[WaitingSlots.Capacity];

            private Candidates(int variants)
            {
                _start = new int[variants + 1];
                _next = new int[variants];
            }

            public static Candidates For(int variants)
            {
                Candidates? current = s_current;
                if (current == null || current._next.Length != variants)
                {
                    current = new Candidates(variants);
                    s_current = current;
                }

                return current;
            }

            public void Allocate(LevelState state, int[] targets, int[] slots, int occupied)
            {
                Board board = state.Board;
                List<int> claimPods = ClaimPods;
                List<int> claimCells = ClaimCells;
                if (occupied == 1)
                {
                    int lone = state.Slots.PodIn(slots[0]);
                    int wanted = state.PodVariantIndex[lone];
                    int left = state.Pods[lone].Remaining;
                    for (int t = 0; t < targets.Length && left > 0; t++)
                    {
                        int cell = ReachabilityResult.CellOf(targets[t]);
                        if (!board.IsMysteryHidden(cell) && board.TopCode(cell) == wanted)
                        {
                            claimPods.Add(lone);
                            claimCells.Add(cell);
                            left--;
                        }
                    }

                    return;
                }

                int variants = _next.Length;
                System.Array.Clear(_start, 0, _start.Length);
                if (_byVariant.Length < targets.Length)
                {
                    _byVariant = new int[System.Math.Max(targets.Length, _byVariant.Length * 2)];
                }

                // Count the candidates of each variant (a pod's variant is in the catalog, so a layer the catalog lacks
                // is never one), then place them in target order.
                for (int t = 0; t < targets.Length; t++)
                {
                    int cell = ReachabilityResult.CellOf(targets[t]);
                    if (!board.IsMysteryHidden(cell))
                    {
                        int code = board.TopCode(cell);
                        if (code >= 0)
                        {
                            _start[code + 1]++;
                        }
                    }
                }

                for (int v = 1; v <= variants; v++)
                {
                    _start[v] += _start[v - 1];
                }

                int[] next = _next;
                System.Array.Copy(_start, next, variants);
                for (int t = 0; t < targets.Length; t++)
                {
                    int cell = ReachabilityResult.CellOf(targets[t]);
                    if (!board.IsMysteryHidden(cell))
                    {
                        int code = board.TopCode(cell);
                        if (code >= 0)
                        {
                            _byVariant[next[code]++] = t;
                        }
                    }
                }

                // Each variant's next unclaimed candidate.
                System.Array.Copy(_start, next, variants);
                for (int s = 0; s < occupied; s++)
                {
                    int pod = state.Slots.PodIn(slots[s]);
                    int variant = state.PodVariantIndex[pod];
                    int need = state.Pods[pod].Remaining;
                    int k = next[variant];
                    int end = _start[variant + 1];
                    while (need > 0 && k < end)
                    {
                        claimPods.Add(pod);
                        claimCells.Add(ReachabilityResult.CellOf(targets[_byVariant[k++]]));
                        need--;
                    }

                    next[variant] = k;
                }
            }
        }
    }
}
