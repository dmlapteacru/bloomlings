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
            List<int> roles = Enumerable.Range(0, Variants.Length).ToList();
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

            return AddMechanics(seed, rows, pods, stacks, overlays);
        }

        /// <summary>
        /// Adds US4 mechanics on a separate random stream, so the base level stays as it was: mystery tiles, a key that
        /// locks a pod, a key for a locked slot, a connected pair at the same depth and a gate with a counter.
        /// </summary>
        private static LevelSession AddMechanics(ulong seed, string[] rows, List<PodDef> pods, List<string>[] stacks, List<CellOverlay> overlays)
        {
            var rng = new Xoshiro256StarStar(seed ^ 0x5EC1A1B0C0FFEEUL);
            int height = rows.Length;
            var byCell = overlays.ToDictionary(o => o.Cell);
            CellOverlay At(CellPos cell) => byCell.TryGetValue(cell, out CellOverlay? o) ? o : TestContent.Overlay(cell.X, cell.Y);
            var targets = new List<CellPos>();
            var empties = new List<CellPos>();
            for (int r = 0; r < height; r++)
            {
                for (int x = 0; x < rows[r].Length; x++)
                {
                    var cell = new CellPos(x, height - 1 - r);
                    char c = rows[r][x];
                    if (c == '.')
                    {
                        if (!(cell.Y == 0 && cell.X == rows[r].Length / 2))
                        {
                            empties.Add(cell);
                        }
                    }
                    else if (c != '#')
                    {
                        targets.Add(cell);
                    }
                }
            }

            foreach (CellPos cell in targets)
            {
                if (rng.NextInt(100) < 8)
                {
                    byCell[cell] = At(cell) with { Mystery = true };
                }
            }

            var locks = new List<LockDef>();
            LockedSlotDef? lockedSlot = null;
            var keyCells = targets.OrderBy(_ => rng.NextInt(1000)).ToList();
            if (rng.NextInt(100) < 40)
            {
                int p = rng.NextInt(pods.Count);
                byCell[keyCells[0]] = At(keyCells[0]) with { KeyId = "k0" };
                pods[p] = pods[p] with { LockKeyId = "k0" };
                locks.Add(new LockDef("k0", LockTargetKind.Pod, pods[p].Id));
            }

            if (rng.NextInt(100) < 25 && keyCells.Count > 1)
            {
                int slot = rng.NextInt(5);
                byCell[keyCells[1]] = At(keyCells[1]) with { KeyId = "k1" };
                lockedSlot = new LockedSlotDef(slot, "k1");
                locks.Add(new LockDef("k1", LockTargetKind.Slot, slot.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            if (rng.NextInt(100) < 30 && stacks.Length >= 2)
            {
                int depth = rng.NextInt(2);
                List<string>[] deep = stacks.Where(st => st.Count > depth).ToArray();
                if (deep.Length >= 2)
                {
                    foreach (string id in new[] { deep[0][depth], deep[1][depth] })
                    {
                        int p = pods.FindIndex(pod => pod.Id == id);
                        pods[p] = pods[p] with { ConnectedGroupId = "g0" };
                    }
                }
            }

            var specials = new List<SpecialDef>();
            if (rng.NextInt(100) < 25 && empties.Count > 0)
            {
                CellPos gate = empties[rng.NextInt(empties.Count)];
                specials.Add(new SpecialDef(
                    "gate",
                    SpecialType.Gate,
                    new[] { gate },
                    new SpecialCondition(SpecialConditionKind.ClearCountAdjacent, null, null, 1 + rng.NextInt(3), System.Array.Empty<CellPos>()),
                    new SpecialEffect(SpecialEffectKind.OpenCells, System.Array.Empty<CellPos>())));
            }

            LevelDefinition definition = RuleLevels.Definition(
                rows,
                pods.ToArray(),
                stacks.Select(st => st.ToArray()).ToArray(),
                byCell.Values.OrderBy(o => o.Cell.Y).ThenBy(o => o.Cell.X).ToArray()) with
            {
                Locks = locks,
                Slots = new SlotsDef(SlotsDef.DefaultCount, lockedSlot),
                Specials = specials,
            };
            return LevelSession.Load(definition, RuleLevels.Picture(rows), RuleLevels.Options);
        }

        /// <summary>A command sequence: mostly taps on exposed pods, sometimes on buried or finished ones.</summary>
        public static List<Command> Commands(ulong seed, LevelSession session, int maxSteps = 60)
        {
            var rng = new Xoshiro256StarStar(seed ^ 0xC0FFEEUL);
            var boosterRng = new Xoshiro256StarStar(seed ^ 0xB0057E25UL);
            LevelSession probe = session.Clone();
            var commands = new List<Command>();
            IReadOnlyList<string> ids = probe.View.PodIds;
            for (int step = 0; step < maxSteps; step++)
            {
                // Now and then a booster, drawn from its own stream so the tap sequence stays as it was (US5).
                int booster = boosterRng.NextInt(100);
                if (booster < 12)
                {
                    Command boost = (booster % 4) switch
                    {
                        0 => new UseExtraSlot(),
                        1 => new UseShuffle(),
                        2 => new UseReturn(boosterRng.NextInt(6)),
                        _ => new UseBloomBurst(Variants[boosterRng.NextInt(Variants.Length)]),
                    };
                    commands.Add(boost);
                    probe.Apply(boost);
                }

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
