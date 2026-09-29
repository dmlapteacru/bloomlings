using System;
using System.Collections.Generic;
using Bloomlings.Core.Random;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;

namespace Bloomlings.Generator
{
    /// <summary>A planned pod: its variant, count and the wave it belongs to, and the key that locks it (FR-034).</summary>
    public sealed record PlannedPod(VariantId Variant, int Count, int Wave, string? LockKeyId = null);

    /// <summary>
    /// R9 step 6 (T087): splits each variant's demand along the planned waves into pods. No pod exceeds the band's
    /// maximum size or falls under its minimum (at least <see cref="BandGuidelines.MinPodSize"/>, the "small" class),
    /// and the total pod count lands in the band's range (plus the hard mode's extra pods). Large pods are split in turn
    /// until the count is reached, never below the minimum. A wave too small for a pod of its own joins a pod of the same
    /// variant in the nearest wave (the earlier one when both are as near), so that pod waits for the later tiles; a
    /// variant whose whole demand is under the minimum rejects the candidate. Sizes follow the classes of doc 05 §12:
    /// small 5–15, medium 16–40, large 41–100, exceptional 100+.
    /// </summary>
    public static class PodPartitioner
    {
        public static IReadOnlyList<PlannedPod>? Partition(IReadOnlyList<Wave> waves, IntRange podCount, IntRange podSize, int extraPods, ref Xoshiro256StarStar rng)
        {
            int minSize = Math.Max(podSize.Min, BandGuidelines.MinPodSize);
            int[]? demand = MergeSmallWaves(waves, minSize);
            if (demand == null)
            {
                return null;
            }

            var pods = new List<PlannedPod>();
            for (int w = 0; w < waves.Count; w++)
            {
                int count = demand[w];
                if (count == 0)
                {
                    continue;
                }

                int parts = Math.Max(1, (count + podSize.Max - 1) / podSize.Max);
                for (int p = 0; p < parts; p++)
                {
                    int size = (count / parts) + (p < count % parts ? 1 : 0);
                    pods.Add(new PlannedPod(waves[w].Variant, size, w));
                }
            }

            int target = Math.Min(podCount.Max, podCount.Min + rng.NextInt(podCount.Max - podCount.Min + 1) + extraPods);
            if (pods.Count > podCount.Max)
            {
                return null;
            }

            while (pods.Count < target)
            {
                int largest = -1;
                for (int i = 0; i < pods.Count; i++)
                {
                    if (largest < 0 || pods[i].Count > pods[largest].Count)
                    {
                        largest = i;
                    }
                }

                // Both halves must keep the minimum size.
                if (largest < 0 || pods[largest].Count < minSize * 2)
                {
                    break;
                }

                PlannedPod pod = pods[largest];
                int first = (pod.Count / 2) + (pod.Count >= (minSize * 2) + 2 ? rng.NextInt(3) - 1 : 0);
                first = Math.Max(minSize, Math.Min(pod.Count - minSize, first));
                pods[largest] = pod with { Count = first };
                pods.Insert(largest + 1, pod with { Count = pod.Count - first });
            }

            return podCount.Contains(pods.Count) ? pods : null;
        }

        /// <summary>
        /// Each wave's demand after moving waves under <paramref name="minSize"/> into the nearest wave of the same
        /// variant; null when a variant's whole demand is under the minimum.
        /// </summary>
        private static int[]? MergeSmallWaves(IReadOnlyList<Wave> waves, int minSize)
        {
            var demand = new int[waves.Count];
            for (int w = 0; w < waves.Count; w++)
            {
                demand[w] = waves[w].Count;
            }

            for (int w = 0; w < waves.Count; w++)
            {
                if (demand[w] == 0 || demand[w] >= minSize)
                {
                    continue;
                }

                int into = -1;
                for (int distance = 1; distance < waves.Count && into < 0; distance++)
                {
                    foreach (int other in new[] { w - distance, w + distance })
                    {
                        if (other >= 0 && other < waves.Count && demand[other] > 0 && waves[other].Variant == waves[w].Variant)
                        {
                            into = other;
                            break;
                        }
                    }
                }

                if (into < 0)
                {
                    return null;
                }

                // The merged pod belongs to the earlier wave: it is committed when its first tiles open.
                int keep = Math.Min(w, into);
                int drop = Math.Max(w, into);
                demand[keep] += demand[drop];
                demand[drop] = 0;
                w = keep - 1; // Look at the grown wave again: two small waves may still be under the minimum.
            }

            return demand;
        }
    }
}
