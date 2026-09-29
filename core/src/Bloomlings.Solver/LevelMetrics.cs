using System.Collections.Generic;

namespace Bloomlings.Solver
{
    /// <summary>
    /// Difficulty metrics (FR-082, R8, T078). All values are integers; ratios are in per mille and averages × 1000, so
    /// scores are identical on every machine.
    /// </summary>
    /// <param name="DependencyDepth">Region shells to pass from the entry to the deepest tile (outer regions shield inner ones).</param>
    /// <param name="BranchingX1000">Mean number of distinct legal taps along the winning line × 1000.</param>
    /// <param name="UnsafeChoicePermille">Share of legal taps in winnable states after which the level can no longer be won (walk capped at <c>Solver.MetricsNodeCap</c> nodes).</param>
    /// <param name="DeadEndDepth">Commits in the jam witness (how deep a losing line runs); 0 when no jam exists.</param>
    /// <param name="PeakBuffer">Most occupied slots right after a commit on the winning line.</param>
    /// <param name="MeanBufferX1000">Mean occupied slots right after each commit on the winning line × 1000.</param>
    /// <param name="ConnectedCommitments">Connected groups in the level.</param>
    /// <param name="VariantCount">Active variants.</param>
    /// <param name="SiblingPairs">Families with two active variants.</param>
    /// <param name="CrossVariantLayers">Hidden layers whose variant differs from the layer above.</param>
    /// <param name="SpecialLoad">Specials plus keys plus locked pods and slots.</param>
    /// <param name="TotalWork">Tile-layers to clear (= total pod count).</param>
    /// <param name="PodCount">Source Pods.</param>
    /// <param name="EstimatedDurationMs">A rough play time from commits and work.</param>
    /// <param name="Complete">False when a budget ran out and some metrics are partial.</param>
    public sealed record LevelMetrics(
        int DependencyDepth,
        int BranchingX1000,
        int UnsafeChoicePermille,
        int DeadEndDepth,
        int PeakBuffer,
        int MeanBufferX1000,
        int ConnectedCommitments,
        int VariantCount,
        int SiblingPairs,
        int CrossVariantLayers,
        int SpecialLoad,
        int TotalWork,
        int PodCount,
        int EstimatedDurationMs,
        bool Complete)
    {
        /// <summary>The metrics by their wire name, for scoring weights and validation records.</summary>
        public IReadOnlyDictionary<string, int> ToDictionary() => new SortedDictionary<string, int>(System.StringComparer.Ordinal)
        {
            ["dependencyDepth"] = DependencyDepth,
            ["branchingX1000"] = BranchingX1000,
            ["unsafeChoicePermille"] = UnsafeChoicePermille,
            ["deadEndDepth"] = DeadEndDepth,
            ["peakBuffer"] = PeakBuffer,
            ["meanBufferX1000"] = MeanBufferX1000,
            ["connectedCommitments"] = ConnectedCommitments,
            ["variantCount"] = VariantCount,
            ["siblingPairs"] = SiblingPairs,
            ["crossVariantLayers"] = CrossVariantLayers,
            ["specialLoad"] = SpecialLoad,
            ["totalWork"] = TotalWork,
            ["podCount"] = PodCount,
            ["estimatedDurationMs"] = EstimatedDurationMs,
        };
    }
}
