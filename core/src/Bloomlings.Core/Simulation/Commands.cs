using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// A player command (contracts/simulation-api.md). The outcome of a level depends only on the sequence of accepted
    /// commands (FR-024). <see cref="CommandText"/> gives each command a stable text form for logs and golden files.
    /// </summary>
    public abstract record Command;

    /// <summary>Commit an exposed pod to the leftmost free usable slot (FR-014).</summary>
    public sealed record TapPod(string PodId) : Command;

    /// <summary>Booster: add the sixth slot, at most once per level (FR-043).</summary>
    public sealed record UseExtraSlot() : Command;

    /// <summary>Booster: rearrange the tray constructively (FR-044, research R10).</summary>
    public sealed record UseShuffle() : Command;

    /// <summary>Booster: send the pod in a slot back to the top of its original stack (FR-045).</summary>
    public sealed record UseReturn(int SlotIndex) : Command;

    /// <summary>Booster: remove every layer and every pod of one variant (FR-050).</summary>
    public sealed record UseBloomBurst(VariantId Variant) : Command;

    /// <summary>Rebuild the level from the same definition (FR-028).</summary>
    public sealed record Restart() : Command;
}
