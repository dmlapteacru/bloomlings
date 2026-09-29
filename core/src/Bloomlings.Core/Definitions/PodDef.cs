using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Definitions
{
    /// <summary>A Spirit Pod: one exact target variant and a count of work units (FR-017).</summary>
    public sealed record PodDef(
        string Id,
        VariantId Variant,
        int Count,
        bool Mystery,
        string? LockKeyId,
        string? ConnectedGroupId);
}
