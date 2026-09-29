using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>What the player can see of one cell. Hidden mystery variants are never exposed (FR-039).</summary>
    public readonly struct CellInfo
    {
        public CellInfo(CellKind kind, VariantId? visible, VariantId? next, int remainingLayers, bool mysteryHidden, string? keyId, string? specialId, bool isEntry)
        {
            Kind = kind;
            Visible = visible;
            Next = next;
            RemainingLayers = remainingLayers;
            MysteryHidden = mysteryHidden;
            KeyId = keyId;
            SpecialId = specialId;
            IsEntry = isEntry;
        }

        public CellKind Kind { get; }

        /// <summary>The visible top-layer variant; null for non-target cells and hidden mystery tiles.</summary>
        public VariantId? Visible { get; }

        /// <summary>The next layer's variant for the peek indicator (FR-036); null when there is none or it is hidden.</summary>
        public VariantId? Next { get; }

        public int RemainingLayers { get; }

        public bool MysteryHidden { get; }

        public string? KeyId { get; }

        public string? SpecialId { get; }

        public bool IsEntry { get; }
    }

    /// <summary>What the player can see of one pod (FR-012, FR-013).</summary>
    public readonly struct PodInfo
    {
        public PodInfo(string id, VariantId? variant, int remaining, PodLocation location, int slotIndex, bool locked, bool mystery, string? connectedGroupId)
        {
            Id = id;
            Variant = variant;
            Remaining = remaining;
            Location = location;
            SlotIndex = slotIndex;
            Locked = locked;
            Mystery = mystery;
            ConnectedGroupId = connectedGroupId;
        }

        public string Id { get; }

        /// <summary>The exact variant; null for a mystery pod that has not been committed (shown as "?").</summary>
        public VariantId? Variant { get; }

        public int Remaining { get; }

        public PodLocation Location { get; }

        public int SlotIndex { get; }

        public bool Locked { get; }

        public bool Mystery { get; }

        public string? ConnectedGroupId { get; }
    }

    /// <summary>What the player can see of a special object: its visible condition and counter (FR-037, FR-038).</summary>
    public readonly struct SpecialInfo
    {
        public SpecialInfo(SpecialDef definition, int progress, int total, bool triggered)
        {
            Definition = definition;
            Progress = progress;
            Total = total;
            Triggered = triggered;
        }

        public SpecialDef Definition { get; }

        public string Id => Definition.Id;

        public SpecialType Type => Definition.Type;

        public IReadOnlyList<CellPos> Cells => Definition.Cells;

        public SpecialCondition Condition => Definition.Condition;

        public int Progress { get; }

        public int Total { get; }

        public bool Triggered { get; }
    }

    /// <summary>
    /// Read-only access to the current settled state for rendering and queries. It reads the live session, so values
    /// change after each accepted command; the presentation keeps its own visual state from the event log (R4).
    /// </summary>
    public sealed class LevelView
    {
        private readonly LevelSession _session;

        internal LevelView(LevelSession session)
        {
            _session = session;
        }

        private LevelState State => _session.State;

        public int LevelNumber => State.Definition.LevelNumber;

        public int Width => State.Board.Width;

        public int Height => State.Board.Height;

        public IReadOnlyList<EntryDef> Entries => State.Board.Entries;

        public LevelStatus Status => State.Status;

        /// <summary>Remaining tile-layers on the whole board, visible and hidden.</summary>
        public int RemainingWork => State.Board.CountAllLayers();

        public CellInfo Cell(CellPos pos) => Cell(State.Board.IndexOf(pos));

        public CellInfo Cell(int index)
        {
            Board board = State.Board;
            CellKind kind = board.KindAt(index);
            bool hidden = board.IsMysteryHidden(index);
            VariantId? visible = kind == CellKind.Target && !hidden ? board.TopLayer(index) : (VariantId?)null;
            VariantId? next = kind == CellKind.Target && !hidden ? board.NextLayer(index) : null;
            return new CellInfo(kind, visible, next, board.RemainingLayers(index), hidden, board.KeyAt(index), board.SpecialAt(index), board.IsEntryCell(index));
        }

        public int SlotCapacity => WaitingSlots.Capacity;

        public SlotState SlotStateOf(int slot) => State.Slots.StateOf(slot);

        /// <summary>The pod id in a slot, or null.</summary>
        public string? PodInSlot(int slot)
        {
            int pod = State.Slots.PodIn(slot);
            return pod < 0 ? null : State.PodId(pod);
        }

        public int StackCount => State.Tray.StackCount;

        /// <summary>Pod ids of a stack, top (exposed) first.</summary>
        public IReadOnlyList<string> Stack(int stack)
        {
            IReadOnlyList<int> pods = State.Tray.StackTopFirst(stack);
            var ids = new string[pods.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = State.PodId(pods[i]);
            }

            return ids;
        }

        /// <summary>All pod ids in definition order.</summary>
        public IReadOnlyList<string> PodIds
        {
            get
            {
                var ids = new string[State.PodDefs.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = State.PodDefs[i].Id;
                }

                return ids;
            }
        }

        public PodInfo Pod(string podId)
        {
            int pod = State.PodIndex[podId];
            PodDef def = State.PodDefs[pod];
            PodRuntime runtime = State.Pods[pod];
            return new PodInfo(
                def.Id,
                runtime.VariantRevealed ? def.Variant : (VariantId?)null,
                runtime.Remaining,
                runtime.Location,
                runtime.SlotIndex,
                State.IsPodLocked(pod),
                def.Mystery,
                def.ConnectedGroupId);
        }

        /// <summary>The special objects in definition order, with their counters.</summary>
        public IReadOnlyList<SpecialInfo> Specials
        {
            get
            {
                var result = new SpecialInfo[State.Mechanics.Specials.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = new SpecialInfo(State.Mechanics.Specials[i].Def, State.SpecialProgress[i], State.Mechanics.Specials[i].Total, State.SpecialTriggered[i]);
                }

                return result;
            }
        }

        /// <summary>The key→lock pairs of the level (FR-033), for the key's flight to its lock.</summary>
        public IReadOnlyList<LockDef> Locks => State.Definition.Locks;

        public bool IsKeyCollected(string keyId) => State.IsKeyCollected(keyId);

        /// <summary>The ids of the pod's connected group in definition order (only the pod itself when not connected).</summary>
        public IReadOnlyList<string> ConnectedGroup(string podId)
        {
            int[] group = State.Mechanics.GroupOf(State.PodIndex[podId]);
            var ids = new string[group.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = State.PodId(group[i]);
            }

            return ids;
        }

        /// <summary>True when the pod is the exposed top of its stack.</summary>
        public bool IsExposed(string podId) => State.PodIndex.TryGetValue(podId, out int pod) && State.Tray.IsExposed(pod);
    }
}
