using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;

namespace Bloomlings.Generator
{
    /// <summary>One wave of the planned solution: a variant and the tile-layers of it cleared in one go (cell indexes in order).</summary>
    public sealed record Wave(VariantId Variant, int Count, IReadOnlyList<int> Cells);

    /// <summary>
    /// R9 step 5 (T086): simulates a "good play" policy on the board. At each step it takes the variant with the most
    /// reachable tiles (catalog order breaks ties) and clears that variant until none of it is reachable, which is one
    /// wave. The waves are the order in which each variant's work becomes reachable. Returns null when some tile can
    /// never be reached (an enclosed region).
    /// </summary>
    public static class SolutionPlanner
    {
        public static IReadOnlyList<Wave>? Plan(Board start, VariantCatalog catalog)
        {
            Board board = start.Clone();
            var waves = new List<Wave>();
            while (board.CountAllLayers() > 0)
            {
                ReachabilityResult reach = Reachability.Compute(board);
                var counts = new int[catalog.Count];
                foreach (ReachableTarget target in reach.Targets)
                {
                    counts[catalog.IndexOf(board.TopLayer(target.Index))]++;
                }

                int best = -1;
                for (int i = 0; i < counts.Length; i++)
                {
                    if (counts[i] > 0 && (best < 0 || counts[i] > counts[best]))
                    {
                        best = i;
                    }
                }

                if (best < 0)
                {
                    return null;
                }

                VariantId variant = catalog.All[best].Id;
                int cleared = 0;
                var cells = new List<int>();
                while (true)
                {
                    bool any = false;
                    foreach (ReachableTarget target in Reachability.Compute(board).Targets)
                    {
                        if (board.TopLayer(target.Index) == variant)
                        {
                            board.ClearTopLayer(target.Index);
                            cells.Add(target.Index);
                            cleared++;
                            any = true;
                        }
                    }

                    if (!any)
                    {
                        break;
                    }
                }

                waves.Add(new Wave(variant, cleared, cells));
            }

            return waves;
        }
    }
}
