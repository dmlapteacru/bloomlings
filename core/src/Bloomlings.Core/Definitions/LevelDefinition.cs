using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Definitions
{
    /// <summary>
    /// Stable, versioned definition of one sequential level (FR-075–FR-077; contracts/level-definition.schema.json;
    /// data-model §1.3). The runtime expands it deterministically: picture grid → mirror → role→variant mapping
    /// (visible top layer) → overlays (see <c>BoardBuilder</c>).
    /// </summary>
    /// <param name="LevelNumber">Unique within a content version; ≥ 1.</param>
    /// <param name="DefinitionVersion">"Bumped only for a deliberate fix (FR-076)."</param>
    /// <param name="Seed">Generator seed; also salts the Shuffle PRNG (research R3, R10).</param>
    /// <param name="Mapping">
    /// roleId → variantId. "Each role maps to a variant of the same colorGroup that is allowed in the band.
    /// Several roles may share one variant (merge)."
    /// </param>
    /// <param name="Entries">"At least 1 entry; default is bottom-center (FR-009)".</param>
    /// <param name="Overlays">
    /// Sparse per-cell overlays. <c>layersBelow</c>: "depth ≤ 2 before L125 and ≤ 3 after (FR-036)".
    /// "Keys sit on target cells only (FR-033)".
    /// </param>
    /// <param name="Locks">"Exactly one lock per key and one key per lock (FR-033)".</param>
    /// <param name="Slots">"At most 1 locked slot, and only from L80 (FR-039)".</param>
    /// <param name="Tray">Stacks of pod ids, top (exposed) first: "2–6 stacks (tuned per band)".</param>
    /// <param name="Pods">
    /// "`count ≥ 1`. Connected groups have 2 members (3 only if that mechanic is unlocked). Members of a connected group
    /// sit at the same depth in different stacks".
    /// </param>
    public sealed record LevelDefinition(
        int LevelNumber,
        int DefinitionVersion,
        ulong Seed,
        string GeneratorVersion,
        PictureRef Picture,
        IReadOnlyDictionary<string, VariantId> Mapping,
        IReadOnlyList<EntryDef> Entries,
        IReadOnlyList<CellOverlay> Overlays,
        IReadOnlyList<SpecialDef> Specials,
        IReadOnlyList<LockDef> Locks,
        SlotsDef Slots,
        TrayDef Tray,
        IReadOnlyList<PodDef> Pods,
        DifficultyDef Difficulty,
        string RewardProfile,
        IReadOnlyList<string> Mechanics);
}
