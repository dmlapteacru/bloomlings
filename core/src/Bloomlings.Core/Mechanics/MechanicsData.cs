using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Tray;

namespace Bloomlings.Core.Mechanics
{
    /// <summary>The fixed rules of one special object, resolved to cell indexes.</summary>
    internal sealed class SpecialRule
    {
        public SpecialRule(SpecialDef def, int total, int[] ownCells, int[] regionCells, int[] effectCells)
        {
            Def = def;
            Total = total;
            OwnCells = ownCells;
            RegionCells = regionCells;
            EffectCells = effectCells;
        }

        public SpecialDef Def { get; }

        /// <summary>The condition's target: 1 for a key, the count, or the region size.</summary>
        public int Total { get; }

        public int[] OwnCells { get; }

        public int[] RegionCells { get; }

        /// <summary>The cells the effect changes (open, remove stones or reveal), in definition order.</summary>
        public int[] EffectCells { get; }

        /// <summary>A Gate (and the late specials) opens its own cells when it triggers; a Fountain stays (FR-037, FR-038).</summary>
        public bool OpensOwnCells => Def.Type != SpecialType.Fountain;
    }

    /// <summary>
    /// Immutable per-level tables for the US4 mechanics, built and validated once when a level loads and shared by all
    /// clones of its state: key→lock pairs (FR-033), connected groups (FR-035) and special objects (FR-037, FR-038).
    /// Any inconsistency is an <see cref="InvalidLevelException"/>.
    /// </summary>
    internal sealed class MechanicsData
    {
        private readonly int[][] _groupOf;
        private readonly int[][] _specialsNextTo;

        private MechanicsData(LockDef?[] lockByKey, int[][] groupOf, SpecialRule[] specials, int[][] specialsNextTo, bool hasMystery)
        {
            LockByKey = lockByKey;
            _groupOf = groupOf;
            Specials = specials;
            _specialsNextTo = specialsNextTo;
            HasMystery = hasMystery;
        }

        /// <summary>The lock each key opens, aligned with the sorted key ids of the level.</summary>
        public LockDef?[] LockByKey { get; }

        public SpecialRule[] Specials { get; }

        public bool HasMystery { get; }

        public bool HasKeys => LockByKey.Length > 0;

        /// <summary>The members of a pod's connected group in definition order; a single pod when it is not connected.</summary>
        public int[] GroupOf(int pod) => _groupOf[pod];

        /// <summary>The order in which a tap commits a group: the tapped pod first, then the others in definition order.</summary>
        public int[] CommitOrder(int tappedPod)
        {
            int[] group = _groupOf[tappedPod];
            if (group.Length == 1)
            {
                return group;
            }

            var order = new int[group.Length];
            order[0] = tappedPod;
            int n = 1;
            foreach (int member in group)
            {
                if (member != tappedPod)
                {
                    order[n++] = member;
                }
            }

            return order;
        }

        /// <summary>Specials whose <c>clear_count_adjacent</c> condition counts clears on this cell.</summary>
        public int[] SpecialsNextTo(int cell) => _specialsNextTo[cell];

        public static MechanicsData Build(
            LevelDefinition definition,
            Board board,
            PodDef[] pods,
            Dictionary<string, int> podIndex,
            SourceTray tray,
            string[] keyIds)
        {
            int[][] groupOf = BuildGroups(pods, tray);
            LockDef?[] locks = BuildLocks(definition, pods, podIndex, keyIds);
            SpecialRule[] specials = BuildSpecials(definition, board, out int[][] nextTo);
            bool hasMystery = false;
            foreach (CellOverlay overlay in definition.Overlays)
            {
                hasMystery |= overlay.Mystery;
            }

            return new MechanicsData(locks, groupOf, specials, nextTo, hasMystery);
        }

        private static int[][] BuildGroups(PodDef[] pods, SourceTray tray)
        {
            var members = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (int i = 0; i < pods.Length; i++)
            {
                string? group = pods[i].ConnectedGroupId;
                if (group == null)
                {
                    continue;
                }

                if (!members.TryGetValue(group, out List<int>? list))
                {
                    list = new List<int>();
                    members.Add(group, list);
                }

                list.Add(i);
            }

            var groupOf = new int[pods.Length][];
            for (int i = 0; i < pods.Length; i++)
            {
                groupOf[i] = new[] { i };
            }

            foreach (KeyValuePair<string, List<int>> group in members)
            {
                List<int> list = group.Value;
                if (list.Count < 2 || list.Count > 3)
                {
                    throw new InvalidLevelException($"Connected group '{group.Key}' has {list.Count} members; groups have 2 (or 3) (FR-035).");
                }

                var stacks = new HashSet<int>();
                int depth = tray.DepthFromTop(list[0]);
                foreach (int pod in list)
                {
                    if (!stacks.Add(tray.StackOf(pod)))
                    {
                        throw new InvalidLevelException($"Connected group '{group.Key}' has two members in one stack.");
                    }

                    if (tray.DepthFromTop(pod) != depth)
                    {
                        throw new InvalidLevelException($"Connected group '{group.Key}': members must sit at the same depth.");
                    }
                }

                int[] array = list.ToArray();
                foreach (int pod in array)
                {
                    groupOf[pod] = array;
                }
            }

            return groupOf;
        }

        private static LockDef?[] BuildLocks(LevelDefinition definition, PodDef[] pods, Dictionary<string, int> podIndex, string[] keyIds)
        {
            var byKey = new LockDef?[keyIds.Length];
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (LockDef lockDef in definition.Locks)
            {
                int key = Array.BinarySearch(keyIds, lockDef.KeyId, StringComparer.Ordinal);
                if (key < 0)
                {
                    throw new InvalidLevelException($"Lock for key '{lockDef.KeyId}': no such key lies on the board (FR-033).");
                }

                if (byKey[key] != null)
                {
                    throw new InvalidLevelException($"Key '{lockDef.KeyId}' opens more than one lock (FR-033).");
                }

                if (!targets.Add(lockDef.TargetKind + ":" + lockDef.TargetId))
                {
                    throw new InvalidLevelException($"{lockDef.TargetKind} '{lockDef.TargetId}' has more than one key (FR-033).");
                }

                switch (lockDef.TargetKind)
                {
                    case LockTargetKind.Pod:
                        if (!podIndex.TryGetValue(lockDef.TargetId, out int pod) || !string.Equals(pods[pod].LockKeyId, lockDef.KeyId, StringComparison.Ordinal))
                        {
                            throw new InvalidLevelException($"Lock '{lockDef.KeyId}' names pod '{lockDef.TargetId}', which is not locked by that key.");
                        }

                        break;
                    case LockTargetKind.Slot:
                        LockedSlotDef? locked = definition.Slots.Locked;
                        if (locked == null || !string.Equals(locked.KeyId, lockDef.KeyId, StringComparison.Ordinal)
                            || lockDef.TargetId != locked.SlotIndex.ToString(CultureInfo.InvariantCulture))
                        {
                            throw new InvalidLevelException($"Lock '{lockDef.KeyId}' names slot '{lockDef.TargetId}', which is not locked by that key (FR-039).");
                        }

                        break;
                    case LockTargetKind.Special:
                        SpecialDef? special = null;
                        foreach (SpecialDef candidate in definition.Specials)
                        {
                            if (string.Equals(candidate.Id, lockDef.TargetId, StringComparison.Ordinal))
                            {
                                special = candidate;
                            }
                        }

                        if (special == null || special.Condition.Kind != SpecialConditionKind.Key
                            || !string.Equals(special.Condition.KeyId, lockDef.KeyId, StringComparison.Ordinal))
                        {
                            throw new InvalidLevelException($"Lock '{lockDef.KeyId}' names special '{lockDef.TargetId}', which does not wait for that key.");
                        }

                        break;
                }

                byKey[key] = lockDef;
            }

            for (int k = 0; k < keyIds.Length; k++)
            {
                if (byKey[k] == null)
                {
                    throw new InvalidLevelException($"Key '{keyIds[k]}' opens no lock; every key has exactly one lock (FR-033).");
                }
            }

            foreach (PodDef pod in pods)
            {
                if (pod.LockKeyId != null && !targets.Contains(LockTargetKind.Pod + ":" + pod.Id))
                {
                    throw new InvalidLevelException($"Pod '{pod.Id}' is locked by key '{pod.LockKeyId}', but no lock pairs them.");
                }
            }

            if (definition.Slots.Locked != null
                && !targets.Contains(LockTargetKind.Slot + ":" + definition.Slots.Locked.SlotIndex.ToString(CultureInfo.InvariantCulture)))
            {
                throw new InvalidLevelException("The locked slot has no key (FR-039).");
            }

            foreach (SpecialDef special in definition.Specials)
            {
                if (special.Condition.Kind == SpecialConditionKind.Key && !targets.Contains(LockTargetKind.Special + ":" + special.Id))
                {
                    throw new InvalidLevelException($"Special '{special.Id}' waits for a key, but no lock pairs them.");
                }
            }

            return byKey;
        }

        private static SpecialRule[] BuildSpecials(LevelDefinition definition, Board board, out int[][] nextTo)
        {
            var adjacency = new List<int>?[board.CellCount];
            var rules = new SpecialRule[definition.Specials.Count];
            for (int s = 0; s < rules.Length; s++)
            {
                SpecialDef def = definition.Specials[s];
                int[] own = Indexes(board, def.Cells);
                var ownSet = new HashSet<int>(own);
                SpecialCondition condition = def.Condition;
                int total;
                int[] region = Array.Empty<int>();
                switch (condition.Kind)
                {
                    case SpecialConditionKind.Key:
                        if (condition.KeyId == null)
                        {
                            throw new InvalidLevelException($"Special '{def.Id}': a key condition needs a keyId.");
                        }

                        total = 1;
                        break;
                    case SpecialConditionKind.ClearCountAdjacent:
                        if (condition.Count == null || condition.Count.Value < 1)
                        {
                            throw new InvalidLevelException($"Special '{def.Id}': clear_count_adjacent needs a count ≥ 1.");
                        }

                        if (def.Type == SpecialType.Fountain && condition.Variant == null)
                        {
                            throw new InvalidLevelException($"Fountain '{def.Id}' needs an exact variant in its condition (FR-038).");
                        }

                        total = condition.Count.Value;
                        var around = new SortedSet<int>();
                        foreach (int cell in own)
                        {
                            CellPos pos = board.PosOf(cell);
                            for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                            {
                                if (pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next) && !ownSet.Contains(board.IndexOf(next)))
                                {
                                    around.Add(board.IndexOf(next));
                                }
                            }
                        }

                        foreach (int cell in around)
                        {
                            (adjacency[cell] ??= new List<int>()).Add(s);
                        }

                        break;
                    case SpecialConditionKind.ClearRegion:
                        region = Indexes(board, condition.RegionCells);
                        if (region.Length == 0)
                        {
                            throw new InvalidLevelException($"Special '{def.Id}': clear_region needs region cells.");
                        }

                        foreach (int cell in region)
                        {
                            if (!board.IsTarget(cell))
                            {
                                throw new InvalidLevelException($"Special '{def.Id}': region cell {board.PosOf(cell)} is not a target tile.");
                            }
                        }

                        total = region.Length;
                        break;
                    default:
                        throw new InvalidLevelException($"Special '{def.Id}' has an unknown condition.");
                }

                int[] effect = Indexes(board, def.Effect.Cells);
                foreach (int cell in effect)
                {
                    bool ok = def.Effect.Kind switch
                    {
                        SpecialEffectKind.OpenCells => !board.IsTarget(cell),
                        SpecialEffectKind.RemoveStones => board.KindAt(cell) == CellKind.Stone,
                        _ => board.IsTarget(cell) && board.IsMysteryHidden(cell),
                    };
                    if (!ok)
                    {
                        string need = def.Effect.Kind switch
                        {
                            SpecialEffectKind.OpenCells => "a non-target cell",
                            SpecialEffectKind.RemoveStones => "a stone",
                            _ => "a hidden mystery tile",
                        };
                        throw new InvalidLevelException($"Special '{def.Id}': effect cell {board.PosOf(cell)} must be {need}.");
                    }
                }

                if (def.Type == SpecialType.Fountain && effect.Length == 0)
                {
                    throw new InvalidLevelException($"Fountain '{def.Id}' must change the board visibly (FR-038): give effect cells.");
                }

                rules[s] = new SpecialRule(def, total, own, region, effect);
            }

            nextTo = new int[board.CellCount][];
            for (int i = 0; i < nextTo.Length; i++)
            {
                List<int>? list = adjacency[i];
                nextTo[i] = list == null ? Array.Empty<int>() : list.ToArray();
            }

            return rules;
        }

        private static int[] Indexes(Board board, IReadOnlyList<CellPos> cells)
        {
            var result = new int[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                CellPos c = cells[i];
                if (c.X >= board.Width || c.Y >= board.Height)
                {
                    throw new InvalidLevelException($"Cell {c} lies outside the board.");
                }

                result[i] = board.IndexOf(c);
            }

            return result;
        }
    }
}
