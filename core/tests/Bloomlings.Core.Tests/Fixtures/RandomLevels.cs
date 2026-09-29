using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Tests.Fixtures
{
    /// <summary>
    /// Seeded generator of small random levels for property tests: a 7×8 picture with 2–4 roles, holes and stones,
    /// some hidden layers, exact-accounting pods split per variant and 2–5 tray stacks.
    /// </summary>
    public static class RandomLevels
    {
        private static readonly VariantId[] Variants =
        {
            VariantId.Leaf, VariantId.Moss, VariantId.Water, VariantId.Dew, VariantId.Flower, VariantId.Wood,
        };

        public static LevelSession Create(ulong seed)
        {
            var rng = new Xoshiro256StarStar(seed);
            int roleCount = 2 + rng.NextInt(3);
            List<int> roles = Enumerable.Range(0, RuleLevels.Legend.Length).ToList();
            var chosen = new List<int>();
            for (int i = 0; i < roleCount; i++)
            {
                int pick = rng.NextInt(roles.Count);
                chosen.Add(roles[pick]);
                roles.RemoveAt(pick);
            }

            const int width = 7;
            const int height = 8;
            var rows = new string[height];
            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder();
                for (int x = 0; x < width; x++)
                {
                    int roll = rng.NextInt(100);
                    bool entry = x == width / 2 && y == height - 1;
                    if (roll < 12)
                    {
                        row.Append('.');
                    }
                    else if (roll < 17 && !entry)
                    {
                        row.Append('#');
                    }
                    else
                    {
                        row.Append(RuleLevels.Legend[chosen[rng.NextInt(chosen.Count)]].Symbol);
                    }
                }

                rows[y] = row.ToString();
            }

            // Hidden layers on some target cells (rows are top-first here; board y = height - 1 - row).
            var overlays = new List<CellOverlay>();
            var demand = new Dictionary<VariantId, int>();
            for (int r = 0; r < height; r++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = rows[r][x];
                    if (c == '.' || c == '#')
                    {
                        continue;
                    }

                    VariantId top = Variants[System.Array.FindIndex(RuleLevels.Legend, l => l.Symbol == c)];
                    Add(demand, top, 1);
                    if (rng.NextInt(100) < 15)
                    {
                        int depth = 1 + rng.NextInt(2);
                        var below = new VariantId[depth];
                        for (int d = 0; d < depth; d++)
                        {
                            below[d] = Variants[chosen[rng.NextInt(chosen.Count)]];
                            Add(demand, below[d], 1);
                        }

                        overlays.Add(TestContent.Overlay(x, height - 1 - r, below));
                    }
                }
            }

            // Pods: split each variant's demand into 1–3 pods.
            var pods = new List<PodDef>();
            foreach (VariantId variant in Variants)
            {
                if (!demand.TryGetValue(variant, out int total))
                {
                    continue;
                }

                int parts = 1 + rng.NextInt(System.Math.Min(3, total));
                for (int p = 0; p < parts; p++)
                {
                    int count = p == parts - 1 ? total : 1 + rng.NextInt(total - (parts - p - 1));
                    total -= count;
                    pods.Add(RuleLevels.Pod($"{variant.Key}_{p}", variant, count));
                }
            }

            if (pods.Count < 2)
            {
                // A single-variant board with one tile: add a second tile of another variant is not possible here,
                // so split differently by retrying with the next seed.
                return Create(unchecked(seed + 0x9E3779B97F4A7C15UL));
            }

            // Tray: shuffle the pods and deal them into 2–5 stacks.
            for (int i = pods.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (pods[i], pods[j]) = (pods[j], pods[i]);
            }

            int stackCount = System.Math.Min(pods.Count, 2 + rng.NextInt(4));
            var stacks = Enumerable.Range(0, stackCount).Select(_ => new List<string>()).ToArray();
            for (int i = 0; i < pods.Count; i++)
            {
                int stack = i < stackCount ? i : rng.NextInt(stackCount);
                stacks[stack].Add(pods[i].Id);
            }

            return RuleLevels.Session(rows, pods.ToArray(), stacks.Select(s => s.ToArray()).ToArray(), overlays.ToArray());
        }

        /// <summary>A command sequence: mostly taps on exposed pods, sometimes on buried or finished ones.</summary>
        public static List<Command> Commands(ulong seed, LevelSession session, int maxSteps = 60)
        {
            var rng = new Xoshiro256StarStar(seed ^ 0xC0FFEEUL);
            LevelSession probe = session.Clone();
            var commands = new List<Command>();
            IReadOnlyList<string> ids = probe.View.PodIds;
            for (int step = 0; step < maxSteps; step++)
            {
                string pick;
                if (rng.NextInt(100) < 80)
                {
                    string[] exposed = ids.Where(probe.View.IsExposed).ToArray();
                    if (exposed.Length == 0)
                    {
                        break;
                    }

                    pick = exposed[rng.NextInt(exposed.Length)];
                }
                else
                {
                    pick = ids[rng.NextInt(ids.Count)];
                }

                var command = new TapPod(pick);
                commands.Add(command);
                probe.Apply(command);
                if (probe.Status != LevelStatus.Playing && rng.NextInt(100) < 70)
                {
                    break;
                }
            }

            return commands;
        }

        private static void Add(Dictionary<VariantId, int> demand, VariantId variant, int n) =>
            demand[variant] = (demand.TryGetValue(variant, out int v) ? v : 0) + n;
    }
}
