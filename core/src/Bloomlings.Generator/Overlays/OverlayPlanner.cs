using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;

namespace Bloomlings.Generator.Overlays
{
    /// <summary>Mechanic names, as in a profile's <c>allowedMechanics</c> and a definition's <c>mechanics</c>.</summary>
    public static class MechanicNames
    {
        public const string Stone = "stone";
        public const string Key = "key";
        public const string LockedPod = "locked_pod";
        public const string ConnectedPair = "connected_pair";
        public const string LayeredTile = "layered_tile";
        public const string Gate = "gate";
        public const string Fountain = "fountain";
        public const string LockedSlot = "locked_slot";
        public const string MysteryTile = "mystery_tile";

        /// <summary>Roadmap L8 alternative: a pod shown as "? ×n" until committed (FR-039).</summary>
        public const string MysteryPod = "mystery_pod";

        /// <summary>Roadmap L150 (optional): a sealed garden object.</summary>
        public const string Chest = "chest";

        /// <summary>Roadmap L250 (optional): the second environmental object, a Statue or a Bridge.</summary>
        public const string Environment2 = "environment_2";

        /// <summary>Roadmap L400 (optional): a connected group of three, on Hard and Super Hard levels only.</summary>
        public const string ConnectedTriple = "connected_triple";

        /// <summary>The roadmap unlock id of a mechanic (<c>mechanic.&lt;name&gt;</c>).</summary>
        public static string UnlockId(string mechanic) => "mechanic." + mechanic;

        /// <summary>Every mechanic the generator can place, in planning order.</summary>
        public static IReadOnlyList<string> All { get; } = new[]
        {
            MechanicNames.Stone, MechanicNames.Key, MechanicNames.LockedPod, MechanicNames.ConnectedPair, MechanicNames.LayeredTile, MechanicNames.Gate,
            MechanicNames.Fountain, MechanicNames.LockedSlot, MechanicNames.MysteryTile, MechanicNames.MysteryPod, MechanicNames.Chest,
            MechanicNames.Environment2, MechanicNames.ConnectedTriple,
        };

        /// <summary>
        /// FR-031 showcase → practice → combination. The showcase is the unlock level; the practice level uses the
        /// mechanic again, alone. It is the roadmap's own practice row when there is one (the Key at L14), else the next
        /// level, and for the Connected Triple (Hard and Super Hard only) the next level of those classes.
        /// </summary>
        /// <param name="classOf">The difficulty class of a level (the schedule, or the catalog's own levels).</param>
        public static int? PracticeLevel(string mechanic, UnlockRoadmap roadmap, Func<int, DifficultyClass> classOf)
        {
            int? showcase = roadmap.LevelOf(UnlockId(mechanic));
            if (showcase == null)
            {
                return null;
            }

            int? row = roadmap.LevelOf("profile." + mechanic + "_practice");
            if (row != null && row.Value > showcase.Value)
            {
                return row.Value;
            }

            int level = showcase.Value + 1;
            if (mechanic == ConnectedTriple)
            {
                while (classOf(level) == DifficultyClass.Normal && level < showcase.Value + 100)
                {
                    level++;
                }
            }

            return level;
        }
    }

    /// <summary>The board overlays of one level (R9 step 4, before the solution plan).</summary>
    public sealed class BoardPlan
    {
        public List<CellOverlay> Overlays { get; } = new List<CellOverlay>();

        /// <summary>The hole made for a key door, gate or Fountain, by mechanic name (cell index).</summary>
        public Dictionary<string, int> HoleFor { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    /// <summary>Keys, locks and specials planned for one level (R9 step 4, after the solution plan).</summary>
    public sealed class LockPlan
    {
        public List<CellOverlay> KeyOverlays { get; } = new List<CellOverlay>();

        public List<LockDef> Locks { get; } = new List<LockDef>();

        public List<SpecialDef> Specials { get; } = new List<SpecialDef>();

        public LockedSlotDef? LockedSlot { get; set; }

        /// <summary>Planned-pod index → key id for locked pods.</summary>
        public Dictionary<int, string> LockedPods { get; } = new Dictionary<int, string>();

        /// <summary>Mechanics that could not be placed on this picture.</summary>
        public List<string> Dropped { get; } = new List<string>();
    }

    /// <summary>
    /// R9 step 4 (T105): adds the band's mechanics while the visible top layer keeps following the picture. Only
    /// mechanics the profile allows and the roadmap has unlocked at the level are used (FR-031). Board overlays
    /// (hidden layers, mystery tiles) come before the solution plan; keys are placed on tiles the plan clears early and
    /// open locks that the plan needs later, so the plan stays playable; gates and Fountains take empty or stone cells
    /// next to the tiles that fill their counters. The tray tuner then re-solves everything with the real rules.
    /// </summary>
    public static class OverlayPlanner
    {
        private static IReadOnlyList<string> Order => MechanicNames.All;

        /// <summary>
        /// Chooses up to <paramref name="max"/> mechanics for the level (stones in the picture itself come on top). The
        /// Connected Triple is only for Hard and Super Hard levels (doc 13 L400).
        /// </summary>
        public static List<string> Choose(GenerationProfile profile, int level, UnlockRoadmap roadmap, int max, ref Xoshiro256StarStar rng, DifficultyClass target = DifficultyClass.Normal)
        {
            var candidates = new List<string>();
            foreach (string mechanic in Order)
            {
                int? at = roadmap.LevelOf(MechanicNames.UnlockId(mechanic));
                bool classAllows = mechanic != MechanicNames.ConnectedTriple || target != DifficultyClass.Normal;
                if (profile.Allows(mechanic) && at != null && at.Value <= level && classAllows)
                {
                    candidates.Add(mechanic);
                }
            }

            // Up to two mechanics; a third from the advanced combinations profile (roadmap L175) when max allows it.
            int roll = rng.NextInt(100);
            int count = Math.Min(candidates.Count, Math.Min(max, roll < 25 ? 0 : roll < 75 ? 1 : max >= 3 && roll >= 90 ? 3 : 2));
            var chosen = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int pick = rng.NextInt(candidates.Count);
                chosen.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            chosen.Sort(StringComparer.Ordinal);
            return chosen;
        }

        /// <summary>
        /// Board overlays before the plan: stones and holes on background cells, hidden layers and mystery tiles. A key
        /// door, gate or Fountain gets a hole (open ground inside the picture) to stand on, and a Fountain two stones
        /// nearby to wash away. The layer variants come from the level's mapping, so no variant is added.
        /// </summary>
        /// <param name="background">Per cell: the picture's background role, where stones and holes may go.</param>
        /// <param name="look">
        /// The level's board look. On an <see cref="BoardLook.Icons"/> board no layer peek shows, so it gets fewer hidden
        /// layers (4–9% of its tiles instead of 8–17%), never more than <see cref="BandGuidelines.MaxHiddenLayersOnIcons"/>,
        /// and no mystery tile (generator slack for the hidden-layer fairness check, research R8b).
        /// </param>
        public static BoardPlan BoardOverlays(Board board, bool[] background, IReadOnlyCollection<string> mechanics, GenerationProfile profile, int level, IReadOnlyList<VariantId> active, ref Xoshiro256StarStar rng, BoardLook look = BoardLook.Peek)
        {
            bool icons = look == BoardLook.Icons;
            var plan = new BoardPlan();
            var overlays = new SortedDictionary<int, CellOverlay>();
            var targets = new List<int>();
            for (int i = 0; i < board.CellCount; i++)
            {
                if (board.IsTarget(i))
                {
                    targets.Add(i);
                }
            }

            // Stones and holes stay off the entry cells and their neighbours, so the start is never sealed.
            bool NearEntry(int cell)
            {
                foreach (EntryDef entry in board.Entries)
                {
                    CellPos e = entry.Cell;
                    CellPos c = board.PosOf(cell);
                    if (Math.Abs(e.X - c.X) + Math.Abs(e.Y - c.Y) <= 1)
                    {
                        return true;
                    }
                }

                return false;
            }

            List<int> ground = targets.Where(c => background[c] && !NearEntry(c) && board.PosOf(c).Y > 0).ToList();
            var taken = new HashSet<int>();

            foreach (string special in new[] { MechanicNames.Key, MechanicNames.Gate, MechanicNames.Fountain, MechanicNames.Chest, MechanicNames.Environment2 })
            {
                if (!mechanics.Contains(special))
                {
                    continue;
                }

                List<int> spots = ground.Where(c => !taken.Contains(c) && AdjacentTargets(board, c) >= 3 && !Touches(board, c, taken)).ToList();
                if (spots.Count == 0)
                {
                    continue;
                }

                // Near the middle of the board, where it stands in the Bloomlings' way: one of the three most central spots.
                CellPos middle = new CellPos(board.Width / 2, board.Height / 2);
                spots.Sort((a, b) =>
                {
                    int da = Math.Abs(board.PosOf(a).X - middle.X) + Math.Abs(board.PosOf(a).Y - middle.Y);
                    int db = Math.Abs(board.PosOf(b).X - middle.X) + Math.Abs(board.PosOf(b).Y - middle.Y);
                    return da != db ? da.CompareTo(db) : a.CompareTo(b);
                });
                int hole = spots[rng.NextInt(Math.Min(3, spots.Count))];
                taken.Add(hole);
                plan.HoleFor[special] = hole;
                overlays[hole] = new CellOverlay(board.PosOf(hole), Array.Empty<VariantId>(), false, false, true, null);
                if (special == MechanicNames.Fountain || special == MechanicNames.Chest)
                {
                    List<int> near = ground.Where(c => !taken.Contains(c) && Distance(board, c, hole) >= 2 && Distance(board, c, hole) <= 3).ToList();
                    foreach (int stone in Pick(near, 2, ref rng))
                    {
                        taken.Add(stone);
                        overlays[stone] = new CellOverlay(board.PosOf(stone), Array.Empty<VariantId>(), false, true, false, null);
                    }
                }
            }

            if (mechanics.Contains(MechanicNames.Stone))
            {
                List<int> free = ground.Where(c => !taken.Contains(c) && !Touches(board, c, taken)).ToList();
                foreach (int stone in Pick(free, 2 + rng.NextInt(3), ref rng))
                {
                    taken.Add(stone);
                    overlays[stone] = new CellOverlay(board.PosOf(stone), Array.Empty<VariantId>(), false, true, false, null);
                }
            }

            if (mechanics.Contains(MechanicNames.LayeredTile) && active.Count > 1)
            {
                // FR-036: depth 2 (1 below) first, depth 3 from L125, within the band's limit.
                int maxBelow = Math.Min(profile.MaxLayerDepth, BandGuidelines.MaxLayersBelow(level));
                List<int> free = targets.Where(c => !taken.Contains(c)).ToList();
                int count = Math.Max(2, free.Count * (icons ? 4 + rng.NextInt(6) : 8 + rng.NextInt(10)) / 100);
                int hiddenLeft = icons ? BandGuidelines.MaxHiddenLayersOnIcons : int.MaxValue;
                foreach (int cell in Pick(free, count, ref rng))
                {
                    int below = maxBelow >= 2 && rng.NextInt(100) < 30 ? 2 : 1;
                    below = Math.Min(below, hiddenLeft);
                    if (below <= 0)
                    {
                        break;
                    }

                    hiddenLeft -= below;
                    var layers = new VariantId[below];
                    VariantId above = board.TopLayer(cell);
                    for (int d = 0; d < below; d++)
                    {
                        VariantId next;
                        do
                        {
                            next = active[rng.NextInt(active.Count)];
                        }
                        while (next == above);
                        layers[d] = next;
                        above = next;
                    }

                    overlays[cell] = new CellOverlay(board.PosOf(cell), layers, false, false, false, null);
                }
            }

            if (mechanics.Contains(MechanicNames.MysteryTile) && !icons)
            {
                // Tiles that are not reachable at the start, so they reveal during play (FR-039); at most 3 (R8 cap).
                ReachabilityResult reach = Reachability.Compute(board);
                List<int> hidden = targets.Where(c => !taken.Contains(c) && !reach.IsReachable(c)).ToList();
                foreach (int cell in Pick(hidden, 1 + rng.NextInt(3), ref rng))
                {
                    overlays[cell] = overlays.TryGetValue(cell, out CellOverlay? o)
                        ? o with { Mystery = true }
                        : new CellOverlay(board.PosOf(cell), Array.Empty<VariantId>(), true, false, false, null);
                }
            }

            plan.Overlays.AddRange(overlays.Values);
            return plan;
        }

        private static int AdjacentTargets(Board board, int cell)
        {
            int count = 0;
            CellPos pos = board.PosOf(cell);
            for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
            {
                if (pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next) && board.IsTarget(board.IndexOf(next)))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool Touches(Board board, int cell, HashSet<int> cells)
        {
            foreach (int other in cells)
            {
                if (Distance(board, cell, other) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Keys and locks, key doors, gates and Fountains (after the plan and the pods).</summary>
        public static LockPlan Locks(
            Board board,
            IReadOnlyCollection<string> mechanics,
            IReadOnlyList<Wave> waves,
            IReadOnlyList<PlannedPod> pods,
            BoardPlan boardPlan,
            ref Xoshiro256StarStar rng)
        {
            var plan = new LockPlan();
            var mysteryCells = new HashSet<int>(boardPlan.Overlays.Where(o => o.Mystery).Select(o => board.IndexOf(o.Cell)));
            var usedKeyCells = new HashSet<int>();
            var usedSpecialCells = new HashSet<int>();
            int keyNumber = 0;

            // Cells the plan clears in its first third (keys lie on their top layer, which is still the original one).
            int early = Math.Max(1, waves.Count / 3);
            var earlyCells = new List<int>();
            for (int w = 0; w < early; w++)
            {
                foreach (int cell in waves[w].Cells)
                {
                    if (board.IsTarget(cell) && !mysteryCells.Contains(cell) && !earlyCells.Contains(cell))
                    {
                        earlyCells.Add(cell);
                    }
                }
            }

            if (mechanics.Contains(MechanicNames.LockedPod))
            {
                // Lock a pod of the last two thirds of the plan: its key is collected before it is needed.
                var later = Enumerable.Range(0, pods.Count).Where(i => pods[i].Wave >= early + 1).ToList();
                string? key = later.Count > 0 ? NextKey(board, earlyCells, usedKeyCells, plan, ref keyNumber, ref rng) : null;
                if (key == null)
                {
                    plan.Dropped.Add(MechanicNames.LockedPod);
                }
                else
                {
                    plan.LockedPods[later[rng.NextInt(later.Count)]] = key;
                }
            }

            if (mechanics.Contains(MechanicNames.LockedSlot))
            {
                string? key = NextKey(board, earlyCells, usedKeyCells, plan, ref keyNumber, ref rng);
                if (key == null)
                {
                    plan.Dropped.Add(MechanicNames.LockedSlot);
                }
                else
                {
                    plan.LockedSlot = new LockedSlotDef(SlotsDef.DefaultCount - 1, key);
                    plan.Locks.Add(new LockDef(key, LockTargetKind.Slot, (SlotsDef.DefaultCount - 1).ToString(CultureInfo.InvariantCulture)));
                }
            }

            List<int> openCells = Enumerable.Range(0, board.CellCount)
                .Where(c => (board.KindAt(c) == CellKind.Open && !board.IsEntryCell(c)) || board.KindAt(c) == CellKind.Stone)
                .ToList();

            if (mechanics.Contains(MechanicNames.Key))
            {
                // A key door: one empty cell next to tiles, locked until the key is collected (the L8/L14 Key).
                List<int> doors = boardPlan.HoleFor.TryGetValue(MechanicNames.Key, out int hole)
                    ? new List<int> { hole }
                    : openCells.Where(c => board.KindAt(c) == CellKind.Open && AdjacentLayers(board, c, null) > 0).ToList();
                string? key = doors.Count > 0 ? NextKey(board, earlyCells, usedKeyCells, plan, ref keyNumber, ref rng) : null;
                if (key == null)
                {
                    plan.Dropped.Add(MechanicNames.Key);
                }
                else
                {
                    int door = doors[rng.NextInt(doors.Count)];
                    usedSpecialCells.Add(door);
                    string id = "door" + keyNumber.ToString(CultureInfo.InvariantCulture);
                    plan.Specials.Add(new SpecialDef(
                        id,
                        SpecialType.Gate,
                        new[] { board.PosOf(door) },
                        new SpecialCondition(SpecialConditionKind.Key, key, null, null, Array.Empty<CellPos>()),
                        new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())));
                    plan.Locks.Add(new LockDef(key, LockTargetKind.Special, id));
                }
            }

            if (mechanics.Contains(MechanicNames.Gate))
            {
                // The Garden Gate / heavy blocker: its counter fills as the tiles around it are restored (FR-037).
                List<int> gates = boardPlan.HoleFor.TryGetValue(MechanicNames.Gate, out int hole)
                    ? new List<int> { hole }
                    : openCells.Where(c => !usedSpecialCells.Contains(c) && AdjacentLayers(board, c, null) >= 2).ToList();
                if (gates.Count == 0)
                {
                    plan.Dropped.Add(MechanicNames.Gate);
                }
                else
                {
                    int gate = gates[rng.NextInt(gates.Count)];
                    usedSpecialCells.Add(gate);
                    int count = Math.Min(AdjacentLayers(board, gate, null), 2 + rng.NextInt(3));
                    plan.Specials.Add(new SpecialDef(
                        "gate",
                        SpecialType.Gate,
                        new[] { board.PosOf(gate) },
                        new SpecialCondition(SpecialConditionKind.ClearCountAdjacent, null, null, count, Array.Empty<CellPos>()),
                        new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())));
                }
            }

            if (mechanics.Contains(MechanicNames.Chest))
            {
                // The sealed garden object (roadmap L150): restore every tile around it and it bursts open, clearing
                // the rubble (stones) beside it. Its own cell opens as a route, like a gate.
                List<int> spots = boardPlan.HoleFor.TryGetValue(MechanicNames.Chest, out int hole)
                    ? new List<int> { hole }
                    : openCells.Where(c => !usedSpecialCells.Contains(c) && AdjacentLayers(board, c, null) >= 3).ToList();
                if (spots.Count == 0)
                {
                    plan.Dropped.Add(MechanicNames.Chest);
                }
                else
                {
                    int chest = spots[rng.NextInt(spots.Count)];
                    usedSpecialCells.Add(chest);
                    int[] rubble = Enumerable.Range(0, board.CellCount)
                        .Where(s => s != chest && !usedSpecialCells.Contains(s) && board.KindAt(s) == CellKind.Stone && Distance(board, s, chest) <= 3)
                        .Take(2)
                        .ToArray();
                    plan.Specials.Add(new SpecialDef(
                        "chest",
                        SpecialType.Chest,
                        new[] { board.PosOf(chest) },
                        new SpecialCondition(SpecialConditionKind.ClearCountAdjacent, null, null, AdjacentLayers(board, chest, null), Array.Empty<CellPos>()),
                        rubble.Length > 0
                            ? new SpecialEffect(SpecialEffectKind.RemoveStones, rubble.Select(board.PosOf).ToArray())
                            : new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())));
                }
            }

            if (mechanics.Contains(MechanicNames.Environment2))
            {
                // The second environmental object (roadmap L250): a Statue, unveiled when the garden patch around it is
                // restored (a region counter), or a Bridge, built when enough of one exact variant around it is restored.
                // Both open their own cell as a route when they trigger.
                List<int> spots = boardPlan.HoleFor.TryGetValue(MechanicNames.Environment2, out int hole)
                    ? new List<int> { hole }
                    : openCells.Where(c => !usedSpecialCells.Contains(c) && AdjacentLayers(board, c, null) >= 2).ToList();
                if (spots.Count == 0)
                {
                    plan.Dropped.Add(MechanicNames.Environment2);
                }
                else
                {
                    int cell = spots[rng.NextInt(spots.Count)];
                    usedSpecialCells.Add(cell);
                    VariantId[] bridgeVariants = AdjacentVariants(board, cell).Where(v => AdjacentLayers(board, cell, v) >= 2).ToArray();
                    if (rng.NextInt(2) == 0 || bridgeVariants.Length == 0)
                    {
                        int[] region = Enumerable.Range(0, board.CellCount)
                            .Where(t => board.IsTarget(t) && !mysteryCells.Contains(t) && Distance(board, t, cell) <= 2)
                            .OrderBy(t => Distance(board, t, cell)).ThenBy(t => t)
                            .Take(4 + rng.NextInt(3))
                            .ToArray();
                        plan.Specials.Add(new SpecialDef(
                            "statue",
                            SpecialType.Statue,
                            new[] { board.PosOf(cell) },
                            new SpecialCondition(SpecialConditionKind.ClearRegion, null, null, null, region.Select(board.PosOf).ToArray()),
                            new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())));
                    }
                    else
                    {
                        VariantId variant = bridgeVariants[rng.NextInt(bridgeVariants.Length)];
                        plan.Specials.Add(new SpecialDef(
                            "bridge",
                            SpecialType.Bridge,
                            new[] { board.PosOf(cell) },
                            new SpecialCondition(SpecialConditionKind.ClearCountAdjacent, null, variant, Math.Min(AdjacentLayers(board, cell, variant), 2 + rng.NextInt(4)), Array.Empty<CellPos>()),
                            new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())));
                    }
                }
            }

            if (mechanics.Contains(MechanicNames.Fountain))
            {
                // "Restore N <variant> around it", then nearby stones wash away (FR-038). Needs stones to change.
                var options = new List<(int Cell, VariantId Variant, int Count, int[] Stones)>();
                IEnumerable<int> spots = boardPlan.HoleFor.TryGetValue(MechanicNames.Fountain, out int hole)
                    ? new[] { hole }
                    : openCells.Where(c => !usedSpecialCells.Contains(c));
                foreach (int cell in spots)
                {
                    int[] stones = Enumerable.Range(0, board.CellCount)
                        .Where(s => s != cell && !usedSpecialCells.Contains(s) && board.KindAt(s) == CellKind.Stone && Distance(board, s, cell) <= 3)
                        .Take(3)
                        .ToArray();
                    if (stones.Length == 0)
                    {
                        continue;
                    }

                    foreach (VariantId variant in AdjacentVariants(board, cell))
                    {
                        int layers = AdjacentLayers(board, cell, variant);
                        if (layers >= 2)
                        {
                            options.Add((cell, variant, layers, stones));
                        }
                    }
                }

                if (options.Count == 0)
                {
                    plan.Dropped.Add(MechanicNames.Fountain);
                }
                else
                {
                    (int cell, VariantId variant, int layers, int[] stones) = options[rng.NextInt(options.Count)];
                    usedSpecialCells.Add(cell);
                    plan.Specials.Add(new SpecialDef(
                        "fountain",
                        SpecialType.Fountain,
                        new[] { board.PosOf(cell) },
                        new SpecialCondition(SpecialConditionKind.ClearCountAdjacent, null, variant, Math.Min(layers, 2 + rng.NextInt(5)), Array.Empty<CellPos>()),
                        new SpecialEffect(SpecialEffectKind.RemoveStones, stones.Select(board.PosOf).ToArray())));
                }
            }

            return plan;
        }

        /// <summary>Puts a new key on an unused early cell; null when none is left.</summary>
        private static string? NextKey(Board board, List<int> earlyCells, HashSet<int> usedKeyCells, LockPlan plan, ref int keyNumber, ref Xoshiro256StarStar rng)
        {
            List<int> free = earlyCells.Where(c => !usedKeyCells.Contains(c)).ToList();
            if (free.Count == 0)
            {
                return null;
            }

            int cell = free[rng.NextInt(free.Count)];
            usedKeyCells.Add(cell);
            string id = "k" + (++keyNumber).ToString(CultureInfo.InvariantCulture);
            plan.KeyOverlays.Add(new CellOverlay(board.PosOf(cell), Array.Empty<VariantId>(), false, false, false, id));
            return id;
        }

        /// <summary>
        /// Tile-layers on the cells next to <paramref name="cell"/>, of <paramref name="variant"/> only when given. They
        /// can all be restored, so a counter up to this number can always be met.
        /// </summary>
        private static int AdjacentLayers(Board board, int cell, VariantId? variant)
        {
            int count = 0;
            CellPos pos = board.PosOf(cell);
            for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
            {
                if (!pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                {
                    continue;
                }

                int n = board.IndexOf(next);
                for (int d = 0; d < board.OriginalLayerCount(n) && board.IsTarget(n); d++)
                {
                    if (variant == null || board.LayerAt(n, d) == variant.Value)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static IEnumerable<VariantId> AdjacentVariants(Board board, int cell)
        {
            var variants = new SortedSet<VariantId>();
            CellPos pos = board.PosOf(cell);
            for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
            {
                if (pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next) && board.IsTarget(board.IndexOf(next)))
                {
                    int n = board.IndexOf(next);
                    for (int d = 0; d < board.OriginalLayerCount(n); d++)
                    {
                        variants.Add(board.LayerAt(n, d));
                    }
                }
            }

            return variants;
        }

        private static int Distance(Board board, int a, int b)
        {
            CellPos pa = board.PosOf(a);
            CellPos pb = board.PosOf(b);
            return Math.Abs(pa.X - pb.X) + Math.Abs(pa.Y - pb.Y);
        }

        private static List<int> Pick(List<int> from, int count, ref Xoshiro256StarStar rng)
        {
            var pool = new List<int>(from);
            var picked = new List<int>();
            while (picked.Count < count && pool.Count > 0)
            {
                int i = rng.NextInt(pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }

            picked.Sort();
            return picked;
        }
    }
}
