using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Definitions
{
    public enum SpecialType
    {
        Gate,
        Fountain,
        Chest,
        Statue,
        Bridge,
    }

    public enum SpecialConditionKind
    {
        Key,
        ClearCountAdjacent,
        ClearRegion,
    }

    /// <summary>The visible condition of a special object (FR-037, FR-038).</summary>
    public sealed record SpecialCondition(
        SpecialConditionKind Kind,
        string? KeyId,
        VariantId? Variant,
        int? Count,
        IReadOnlyList<CellPos> RegionCells);

    public enum SpecialEffectKind
    {
        OpenCells,
        RevealLayers,
        RemoveStones,
    }

    public sealed record SpecialEffect(SpecialEffectKind Kind, IReadOnlyList<CellPos> Cells);

    /// <summary>A special board object such as a Garden Gate or a Fountain.</summary>
    public sealed record SpecialDef(
        string Id,
        SpecialType Type,
        IReadOnlyList<CellPos> Cells,
        SpecialCondition Condition,
        SpecialEffect Effect);
}
