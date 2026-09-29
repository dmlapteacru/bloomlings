using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// An ordered event of the settle log (contracts/simulation-api.md; research R4). <see cref="Round"/> is 0 for the
    /// command's own immediate effects; settle rounds count from 1. The presentation replays events as one wave per round.
    /// </summary>
    public abstract record GameEvent(int Round);

    /// <summary>A pod left its stack for a slot. <see cref="FromDepth"/> is its depth in the stack (0 = exposed top).</summary>
    public sealed record PodCommitted(int Round, string PodId, int SlotIndex, int Stack, int FromDepth) : GameEvent(Round);

    public sealed record MysteryPodRevealed(int Round, string PodId, VariantId Variant) : GameEvent(Round);

    /// <summary>
    /// One work unit (FR-017): <see cref="PodId"/> cleared the top layer <see cref="Variant"/> of <see cref="Cell"/>.
    /// <see cref="RouteFromEntry"/> runs from the entry cell over open cells to the target (the last element).
    /// </summary>
    public sealed record TileCleared(int Round, CellPos Cell, VariantId Variant, string PodId, IReadOnlyList<CellPos> RouteFromEntry)
        : GameEvent(Round);

    public sealed record LayerRevealed(int Round, CellPos Cell, VariantId NewTopVariant) : GameEvent(Round);

    /// <summary>The cell became open ground and shows the finished picture (FR-007).</summary>
    public sealed record CellOpened(int Round, CellPos Cell) : GameEvent(Round);

    public sealed record MysteryTileRevealed(int Round, CellPos Cell, VariantId Variant) : GameEvent(Round);

    public sealed record KeyCollected(int Round, string KeyId, CellPos Cell) : GameEvent(Round);

    public sealed record LockOpened(int Round, LockTargetKind TargetKind, string TargetId) : GameEvent(Round);

    public sealed record SpecialProgressed(int Round, string SpecialId, int Progress, int Total) : GameEvent(Round);

    public sealed record SpecialTriggered(int Round, string SpecialId, IReadOnlyList<CellPos> EffectCells) : GameEvent(Round);

    public sealed record PodCompleted(int Round, string PodId, int SlotIndex) : GameEvent(Round);

    public sealed record SlotFreed(int Round, int SlotIndex) : GameEvent(Round);

    public sealed record ExtraSlotAdded(int Round, int SlotIndex) : GameEvent(Round);

    /// <summary>The new tray, pod ids top-first per stack.</summary>
    public sealed record TrayShuffled(int Round, IReadOnlyList<IReadOnlyList<string>> Stacks) : GameEvent(Round);

    public sealed record PodReturned(int Round, string PodId, int Stack) : GameEvent(Round);

    public sealed record VariantBurst(int Round, VariantId Variant, IReadOnlyList<CellPos> Cells, IReadOnlyList<string> PodIds)
        : GameEvent(Round);

    public sealed record LevelWon(int Round, int BoostersUsed) : GameEvent(Round);

    public sealed record LevelJammed(int Round, IReadOnlyList<Recovery> EligibleRecoveries) : GameEvent(Round);

    public sealed record LevelStuck(int Round, IReadOnlyList<Recovery> EligibleRecoveries) : GameEvent(Round);
}
