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
    /// R9 step 6 (T087): splits each variant's demand along the planned waves into pods. Every wave gives at least one
    /// pod, no pod exceeds the band's maximum size, and the total pod count lands in the band's range (plus the hard
    /// mode's extra pods). Large pods are split in turn until the count is reached. Sizes follow the classes of doc 05
    /// §12: small 5–15, medium 16–40, large 41–100, exceptional 100+.
    /// </summary>
    public static class PodPartitioner
    {
        public static IReadOnlyList<PlannedPod>? Partition(IReadOnlyList<Wave> waves, IntRange podCount, IntRange podSize, int extraPods, ref Xoshiro256StarStar rng)
        {
            var pods = new List<PlannedPod>();
            for (int w = 0; w < waves.Count; w++)
            {
                int count = waves[w].Count;
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
                    if (pods[i].Count >= 2 && (largest < 0 || pods[i].Count > pods[largest].Count))
                    {
                        largest = i;
                    }
                }

                if (largest < 0 || pods[largest].Count < Math.Max(2, podSize.Min * 2) && pods.Count >= podCount.Min)
                {
                    break;
                }

                PlannedPod pod = pods[largest];
                int first = (pod.Count / 2) + (pod.Count >= 6 ? rng.NextInt(3) - 1 : 0);
                first = Math.Max(1, Math.Min(pod.Count - 1, first));
                pods[largest] = pod with { Count = first };
                pods.Insert(largest + 1, pod with { Count = pod.Count - first });
            }

            return podCount.Contains(pods.Count) ? pods : null;
        }
    }
}
