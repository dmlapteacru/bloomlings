using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Solver
{
    /// <summary>
    /// Scoring weights and class thresholds (<c>content/profiles/difficulty-thresholds.json</c>, FR-082). A weight is
    /// applied to a metric by its wire name as <c>value × weight / 1000</c>; everything is integer.
    /// </summary>
    public sealed record DifficultyThresholds(IReadOnlyDictionary<string, int> Weights, int HardMin, int SuperHardMin)
    {
        /// <summary>Starting weights; calibrate them against playtests in the thresholds file.</summary>
        public static DifficultyThresholds Default { get; } = new DifficultyThresholds(
            new SortedDictionary<string, int>(StringComparer.Ordinal)
            {
                ["unsafeChoicePermille"] = 2000,
                ["peakBuffer"] = 150_000,
                ["meanBufferX1000"] = 100,
                ["dependencyDepth"] = 60_000,
                ["variantCount"] = 50_000,
                ["siblingPairs"] = 60_000,
                ["crossVariantLayers"] = 15_000,
                ["specialLoad"] = 40_000,
                ["podCount"] = 20_000,
                ["totalWork"] = 1_000,
                ["branchingX1000"] = 20,
            },
            3_000,
            4_500);
    }

    /// <summary>
    /// The difficulty score and class (T078). The score is an integer fixed-point value (× 1000) with integer
    /// thresholds, so generation is reproducible. Curation may override the class (<c>difficulty.overridden</c>).
    /// </summary>
    public static class DifficultyScorer
    {
        public static int Score(LevelMetrics metrics, DifficultyThresholds thresholds) => Score(metrics.ToDictionary(), thresholds);

        /// <summary>The score from metrics by wire name, as stored in validation records.</summary>
        public static int Score(IReadOnlyDictionary<string, int> values, DifficultyThresholds thresholds)
        {
            long score = 0;
            foreach (KeyValuePair<string, int> weight in thresholds.Weights)
            {
                if (!values.TryGetValue(weight.Key, out int value))
                {
                    throw new ArgumentException($"Unknown metric '{weight.Key}' in the difficulty weights.", nameof(thresholds));
                }

                score += (long)value * weight.Value / 1000;
            }

            return (int)Math.Max(0, Math.Min(int.MaxValue, score));
        }

        public static DifficultyClass Classify(int score, DifficultyThresholds thresholds) =>
            score >= thresholds.SuperHardMin ? DifficultyClass.SuperHard
            : score >= thresholds.HardMin ? DifficultyClass.Hard
            : DifficultyClass.Normal;
    }
}
