using System;
using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Hashing;
using Bloomlings.Core.Mechanics;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// The logical level state (data-model §2.1). All mutations go through the methods here so that the Zobrist hash
    /// stays in step with the state (T018): board and pod changes toggle their keys incrementally, and the part for
    /// the at most 6 slots (including their age order) is recomputed on demand.
    /// </summary>
    internal sealed class LevelState
    {
        private const int LocationTray = 1;
        private const int LocationSlot = 2;
        private const int LocationDone = 3;
        private const int LocationRemoved = 4;

        private readonly StateHasher _hasher;

        private LevelState(
            LevelDefinition definition,
            BasePicture picture,
            SessionOptions options,
            Board board,
            PodDef[] podDefs,
            Dictionary<string, int> podIndex,
            int[] podVariantIndex,
            PodRuntime[] pods,
            SourceTray tray,
            WaitingSlots slots,
            string[] keyIds,
            bool[] keyCollected,
            MechanicsData mechanics,
            int[] specialProgress,
            bool[] specialTriggered,
            StateHasher hasher)
        {
            Definition = definition;
            Picture = picture;
            Options = options;
            Board = board;
            PodDefs = podDefs;
            PodIndex = podIndex;
            PodVariantIndex = podVariantIndex;
            Pods = pods;
            Tray = tray;
            Slots = slots;
            KeyIds = keyIds;
            KeyCollected = keyCollected;
            Mechanics = mechanics;
            SpecialProgress = specialProgress;
            SpecialTriggered = specialTriggered;
            _hasher = hasher;
        }

        public LevelDefinition Definition { get; }

        public BasePicture Picture { get; }

        public SessionOptions Options { get; }

        public VariantCatalog Catalog => Options.Catalog;

        public Board Board { get; }

        /// <summary>Pods in definition order; a pod's index is its position here. Shared between clones.</summary>
        public PodDef[] PodDefs { get; }

        public Dictionary<string, int> PodIndex { get; }

        /// <summary>Catalog index of each pod's variant. Shared between clones.</summary>
        public int[] PodVariantIndex { get; }

        public PodRuntime[] Pods { get; }

        public SourceTray Tray { get; }

        public WaitingSlots Slots { get; }

        /// <summary>All key ids of the level, sorted ordinally. Shared between clones.</summary>
        public string[] KeyIds { get; }

        public bool[] KeyCollected { get; }

        /// <summary>Immutable per-level mechanic tables (locks, groups, specials). Shared between clones.</summary>
        public MechanicsData Mechanics { get; }

        /// <summary>Progress of each special's condition, in definition order (FR-037, FR-038).</summary>
        public int[] SpecialProgress { get; }

        /// <summary>Whether each special has triggered.</summary>
        public bool[] SpecialTriggered { get; }

        public bool ExtraSlotUsed { get; private set; }

        public int ShuffleUses { get; private set; }

        public int BoostersUsed { get; set; }

        public LevelStatus Status { get; set; }

        public ulong StateHash => _hasher.Value ^ SlotsHash();

        /// <summary>
        /// Builds the start state and checks exact accounting: "for every variant v, the sum of count over pods of v
        /// equals the number of top layers of v plus the number of hidden layers of v" (FR-023).
        /// </summary>
        public static LevelState Create(LevelDefinition definition, BasePicture picture, SessionOptions options, MysteryAssignment? assignment = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Board board = BoardBuilder.Build(definition, picture, options.Catalog);
            if (assignment != null)
            {
                definition = assignment.Apply(definition, board);
            }

            if (definition.Slots.Count != SlotsDef.DefaultCount)
            {
                throw new InvalidLevelException($"A level has exactly {SlotsDef.DefaultCount} slots, got {definition.Slots.Count} (FR-015).");
            }

            int podCount = definition.Pods.Count;
            var podDefs = new PodDef[podCount];
            var podIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            var podVariantIndex = new int[podCount];
            for (int i = 0; i < podCount; i++)
            {
                PodDef pod = definition.Pods[i];
                if (podIndex.ContainsKey(pod.Id))
                {
                    throw new InvalidLevelException($"Duplicate pod id '{pod.Id}'.");
                }

                if (pod.Count < 1)
                {
                    throw new InvalidLevelException($"Pod '{pod.Id}' has count {pod.Count}; count ≥ 1 is required.");
                }

                if (!options.Catalog.Contains(pod.Variant))
                {
                    throw new InvalidLevelException($"Pod '{pod.Id}' targets unknown variant '{pod.Variant}'.");
                }

                podDefs[i] = pod;
                podIndex.Add(pod.Id, i);
                podVariantIndex[i] = options.Catalog.IndexOf(pod.Variant);
            }

            var stacks = new List<IReadOnlyList<int>>();
            var inTray = new bool[podCount];
            foreach (IReadOnlyList<string> stack in definition.Tray.Stacks)
            {
                if (stack.Count == 0)
                {
                    throw new InvalidLevelException("A tray stack is empty.");
                }

                var indexes = new int[stack.Count];
                for (int d = 0; d < stack.Count; d++)
                {
                    if (!podIndex.TryGetValue(stack[d], out int index))
                    {
                        throw new InvalidLevelException($"The tray names unknown pod '{stack[d]}'.");
                    }

                    if (inTray[index])
                    {
                        throw new InvalidLevelException($"Pod '{stack[d]}' appears twice in the tray.");
                    }

                    inTray[index] = true;
                    indexes[d] = index;
                }

                stacks.Add(indexes);
            }

            for (int i = 0; i < podCount; i++)
            {
                if (!inTray[i])
                {
                    throw new InvalidLevelException($"Pod '{podDefs[i].Id}' is not in any tray stack.");
                }
            }

            if (stacks.Count < SourceTray.MinStacks || stacks.Count > SourceTray.MaxStacks)
            {
                throw new InvalidLevelException($"The tray has {stacks.Count} stacks; 2–6 stacks are allowed.");
            }

            var tray = new SourceTray(stacks, podCount);
            CheckAccounting(board, podDefs, options.Catalog);

            var pods = new PodRuntime[podCount];
            for (int i = 0; i < podCount; i++)
            {
                pods[i] = new PodRuntime
                {
                    Remaining = podDefs[i].Count,
                    Location = PodLocation.Tray,
                    HomeStack = tray.StackOf(i),
                    SlotIndex = -1,
                    VariantRevealed = !podDefs[i].Mystery,
                };
            }

            var slots = new WaitingSlots(definition.Slots.Locked?.SlotIndex ?? -1);
            string[] keyIds = CollectKeyIds(definition);
            MechanicsData mechanics = MechanicsData.Build(definition, board, podDefs, podIndex, tray, keyIds);
            var state = new LevelState(
                definition,
                picture,
                options,
                board,
                podDefs,
                podIndex,
                podVariantIndex,
                pods,
                tray,
                slots,
                keyIds,
                new bool[keyIds.Length],
                mechanics,
                new int[mechanics.Specials.Length],
                new bool[mechanics.Specials.Length],
                new StateHasher());
            state._hasher.Toggle(state.ComputeIncrementalPart());
            return state;
        }

        public LevelState Clone()
        {
            var clone = new LevelState(
                Definition,
                Picture,
                Options,
                Board.Clone(),
                PodDefs,
                PodIndex,
                PodVariantIndex,
                (PodRuntime[])Pods.Clone(),
                Tray.Clone(),
                Slots.Clone(),
                KeyIds,
                (bool[])KeyCollected.Clone(),
                Mechanics,
                (int[])SpecialProgress.Clone(),
                (bool[])SpecialTriggered.Clone(),
                _hasher.Clone());
            clone.ExtraSlotUsed = ExtraSlotUsed;
            clone.ShuffleUses = ShuffleUses;
            clone.BoostersUsed = BoostersUsed;
            clone.Status = Status;
            return clone;
        }

        /// <summary>The hash recomputed from scratch; equal to <see cref="StateHash"/> at all times (tests check this).</summary>
        public ulong ComputeFullHash() => ComputeIncrementalPart() ^ SlotsHash();

        public string PodId(int pod) => PodDefs[pod].Id;

        public VariantId PodVariant(int pod) => PodDefs[pod].Variant;

        public bool IsKeyCollected(string keyId)
        {
            int index = Array.BinarySearch(KeyIds, keyId, StringComparer.Ordinal);
            return index >= 0 && KeyCollected[index];
        }

        public bool IsPodLocked(int pod)
        {
            string? key = PodDefs[pod].LockKeyId;
            return key != null && !IsKeyCollected(key);
        }

        /// <summary>
        /// Whether an exposed pod may be committed now; null when it may. The level status is checked by the caller.
        /// A connected pod commits with its whole group (FR-035): every member must be exposed and unlocked, and there
        /// must be a free usable slot for each member.
        /// </summary>
        public RejectReason? CanCommit(int pod)
        {
            int[] group = Mechanics.GroupOf(pod);
            foreach (int member in group)
            {
                if (!Tray.IsExposed(member))
                {
                    return RejectReason.NotExposed;
                }
            }

            foreach (int member in group)
            {
                if (IsPodLocked(member))
                {
                    return RejectReason.Locked;
                }
            }

            int free = Slots.FreeCount;
            if (free == 0)
            {
                return RejectReason.NoFreeSlot;
            }

            if (free < group.Length)
            {
                return RejectReason.NotEnoughSlotsForGroup;
            }

            return null;
        }

        // ---- Mutations (each keeps the hash in step) ----

        /// <summary>Clears the top layer of a target cell: one work unit (FR-017).</summary>
        public LayerClearResult ClearTopLayer(int cell)
        {
            int depth = Board.TopDepth(cell);
            VariantId variant = Board.TopLayer(cell);
            bool wasHidden = Board.IsMysteryHidden(cell);
            _hasher.Toggle(ZobristFeature.CellLayer, cell, depth, Catalog.IndexOf(variant));
            LayerClearResult result = Board.ClearTopLayer(cell);
            if (result.Opened)
            {
                _hasher.Toggle(ZobristFeature.CellOpen, cell);
            }

            if (wasHidden && !Board.IsMysteryHidden(cell))
            {
                _hasher.Toggle(ZobristFeature.CellMysteryHidden, cell);
            }

            return result;
        }

        /// <summary>Reveals a mystery tile's variant (FR-039).</summary>
        public void RevealMysteryTile(int cell)
        {
            if (Board.IsMysteryHidden(cell))
            {
                Board.RevealMystery(cell);
                _hasher.Toggle(ZobristFeature.CellMysteryHidden, cell);
            }
        }

        /// <summary>Opens a special or stone cell (special effects, FR-037).</summary>
        public void OpenNonTargetCell(int cell)
        {
            Board.OpenCell(cell);
            _hasher.Toggle(ZobristFeature.CellOpen, cell);
        }

        public void SetRemaining(int pod, int remaining)
        {
            if (remaining < 0)
            {
                throw new InvalidOperationException($"Pod '{PodId(pod)}' would go negative.");
            }

            _hasher.Toggle(ZobristFeature.PodRemaining, pod, Pods[pod].Remaining);
            Pods[pod].Remaining = remaining;
            _hasher.Toggle(ZobristFeature.PodRemaining, pod, remaining);
        }

        /// <summary>Moves an exposed pod from its stack to the leftmost free slot and returns (stack, slot).</summary>
        public (int Stack, int Slot) CommitPod(int pod)
        {
            ToggleLocation(pod);
            int stack = Tray.Take(pod);
            int slot = Slots.Commit(pod);
            Pods[pod].Location = PodLocation.Slot;
            Pods[pod].SlotIndex = slot;
            ToggleLocation(pod);
            return (stack, slot);
        }

        /// <summary>A pod with count 0 leaves; its slot frees at once (FR-022). Returns the freed slot.</summary>
        public int CompletePod(int pod)
        {
            int slot = Pods[pod].SlotIndex;
            ToggleLocation(pod);
            Slots.Release(slot);
            Pods[pod].Location = PodLocation.Done;
            Pods[pod].SlotIndex = -1;
            ToggleLocation(pod);
            return slot;
        }

        public void RevealPod(int pod)
        {
            if (!Pods[pod].VariantRevealed)
            {
                Pods[pod].VariantRevealed = true;
                _hasher.Toggle(ZobristFeature.PodRevealed, pod);
            }
        }

        public void CollectKey(string keyId)
        {
            int index = Array.BinarySearch(KeyIds, keyId, StringComparer.Ordinal);
            if (index < 0)
            {
                throw new InvalidOperationException($"Unknown key '{keyId}'.");
            }

            if (!KeyCollected[index])
            {
                KeyCollected[index] = true;
                _hasher.Toggle(ZobristFeature.KeyCollected, index);
            }
        }

        public void UnlockSlot(int slot) => Slots.Unlock(slot);

        public void SetSpecialProgress(int special, int progress)
        {
            _hasher.Toggle(ZobristFeature.SpecialState, special, SpecialProgress[special], SpecialTriggered[special] ? 1 : 0);
            SpecialProgress[special] = progress;
            _hasher.Toggle(ZobristFeature.SpecialState, special, SpecialProgress[special], SpecialTriggered[special] ? 1 : 0);
        }

        public void MarkSpecialTriggered(int special)
        {
            _hasher.Toggle(ZobristFeature.SpecialState, special, SpecialProgress[special], SpecialTriggered[special] ? 1 : 0);
            SpecialTriggered[special] = true;
            _hasher.Toggle(ZobristFeature.SpecialState, special, SpecialProgress[special], 1);
        }

        public void MarkExtraSlotUsed()
        {
            if (!ExtraSlotUsed)
            {
                ExtraSlotUsed = true;
                _hasher.Toggle(ZobristFeature.ExtraSlotUsed, 0);
            }
        }

        public void IncrementShuffleUses()
        {
            if (ShuffleUses > 0)
            {
                _hasher.Toggle(ZobristFeature.ShuffleUses, ShuffleUses);
            }

            ShuffleUses++;
            _hasher.Toggle(ZobristFeature.ShuffleUses, ShuffleUses);
        }

        // ---- Hashing ----

        private void ToggleLocation(int pod)
        {
            _hasher.Toggle(LocationKey(pod));
            if (Pods[pod].Location == PodLocation.Tray)
            {
                _hasher.Toggle(ZobristFeature.TrayDepth, pod, Tray.DepthFromBottom(pod));
            }
        }

        private ulong LocationKey(int pod)
        {
            PodRuntime p = Pods[pod];
            switch (p.Location)
            {
                case PodLocation.Tray:
                    return ZobristKeys.Key(ZobristFeature.PodLocation, pod, LocationTray, Tray.StackOf(pod));
                case PodLocation.Slot:
                    return ZobristKeys.Key(ZobristFeature.PodLocation, pod, LocationSlot, p.SlotIndex);
                case PodLocation.Done:
                    return ZobristKeys.Key(ZobristFeature.PodLocation, pod, LocationDone);
                default:
                    return ZobristKeys.Key(ZobristFeature.PodLocation, pod, LocationRemoved);
            }
        }

        private ulong ComputeIncrementalPart()
        {
            var h = new StateHasher();
            for (int i = 0; i < Board.CellCount; i++)
            {
                switch (Board.KindAt(i))
                {
                    case CellKind.Open:
                        h.Toggle(ZobristFeature.CellOpen, i);
                        break;
                    case CellKind.Target:
                        for (int d = Board.TopDepth(i); d < Board.OriginalLayerCount(i); d++)
                        {
                            h.Toggle(ZobristFeature.CellLayer, i, d, Catalog.IndexOf(Board.LayerAt(i, d)));
                        }

                        if (Board.IsMysteryHidden(i))
                        {
                            h.Toggle(ZobristFeature.CellMysteryHidden, i);
                        }

                        break;
                }
            }

            for (int p = 0; p < Pods.Length; p++)
            {
                h.Toggle(ZobristFeature.PodRemaining, p, Pods[p].Remaining);
                h.Toggle(LocationKey(p));
                if (Pods[p].Location == PodLocation.Tray)
                {
                    h.Toggle(ZobristFeature.TrayDepth, p, Tray.DepthFromBottom(p));
                }

                if (PodDefs[p].Mystery && Pods[p].VariantRevealed)
                {
                    h.Toggle(ZobristFeature.PodRevealed, p);
                }
            }

            for (int k = 0; k < KeyCollected.Length; k++)
            {
                if (KeyCollected[k])
                {
                    h.Toggle(ZobristFeature.KeyCollected, k);
                }
            }

            for (int i = 0; i < SpecialProgress.Length; i++)
            {
                h.Toggle(ZobristFeature.SpecialState, i, SpecialProgress[i], SpecialTriggered[i] ? 1 : 0);
            }

            if (ExtraSlotUsed)
            {
                h.Toggle(ZobristFeature.ExtraSlotUsed, 0);
            }

            if (ShuffleUses > 0)
            {
                h.Toggle(ZobristFeature.ShuffleUses, ShuffleUses);
            }

            return h.Value;
        }

        private ulong SlotsHash()
        {
            ulong h = 0;
            for (int s = 0; s < WaitingSlots.Capacity; s++)
            {
                h ^= ZobristKeys.Key(ZobristFeature.SlotOrder, s, (int)Slots.StateOf(s), Slots.AgeRank(s) + 1);
            }

            return h;
        }

        private static void CheckAccounting(Board board, PodDef[] pods, VariantCatalog catalog)
        {
            foreach (VariantInfo info in catalog.All)
            {
                int demand = 0;
                foreach (PodDef pod in pods)
                {
                    if (pod.Variant == info.Id)
                    {
                        demand += pod.Count;
                    }
                }

                int layers = board.CountLayers(info.Id);
                if (demand != layers)
                {
                    throw new InvalidLevelException(
                        $"Exact accounting fails for '{info.Id}': pods need {demand}, the board has {layers} layers (FR-023).");
                }
            }
        }

        private static string[] CollectKeyIds(LevelDefinition definition)
        {
            var keys = new List<string>();
            foreach (CellOverlay overlay in definition.Overlays)
            {
                if (overlay.KeyId != null)
                {
                    if (keys.Contains(overlay.KeyId))
                    {
                        throw new InvalidLevelException($"Key '{overlay.KeyId}' lies on more than one cell (FR-033).");
                    }

                    keys.Add(overlay.KeyId);
                }
            }

            string[] sorted = keys.ToArray();
            Array.Sort(sorted, StringComparer.Ordinal);
            return sorted;
        }
    }
}
