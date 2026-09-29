using System.Collections.Generic;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Solver
{
    public enum SolveStatus
    {
        /// <summary>A trace reaching the goal exists (a win for <see cref="ISolver.Solve"/>, a jam for FindJam).</summary>
        Solvable,

        /// <summary>The search space was exhausted: no such trace exists.</summary>
        Unsolvable,

        /// <summary>The node budget ran out; counts as a rejection (R8).</summary>
        Unknown,
    }

    /// <summary>Search settings. Budgets count nodes, never time, so results are reproducible (R8).</summary>
    public sealed record SolveOptions(int NodeBudget)
    {
        public static SolveOptions Default { get; } = new SolveOptions(200_000);
    }

    public sealed record SolveResult(SolveStatus Status, IReadOnlyList<Command> Trace, int NodesUsed);

    /// <summary>Everything the pipeline needs about one level: a winning trace, a jam witness and the metrics.</summary>
    public sealed record LevelAnalysis(SolveResult Win, SolveResult Jam, LevelMetrics Metrics)
    {
        public int NodesUsed => Win.NodesUsed + Jam.NodesUsed;
    }

    /// <summary>
    /// The solver interface of contracts/simulation-api.md. It clones sessions and applies commands through the same
    /// <see cref="LevelSession.Apply"/> the game uses; there is no separate rules implementation.
    /// </summary>
    public interface ISolver
    {
        /// <summary>A winning trace without boosters (FR-080).</summary>
        SolveResult Solve(LevelSession start, SolveOptions options);

        /// <summary>A trace that jams the level: proof that it is losable (FR-081).</summary>
        SolveResult FindJam(LevelSession start, SolveOptions options);

        /// <summary>Difficulty metrics from the search tree (FR-082, doc 06 §16).</summary>
        LevelMetrics Measure(LevelSession start, SolveOptions options);
    }
}
