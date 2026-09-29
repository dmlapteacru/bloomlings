using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Definitions
{
    /// <summary>
    /// Sparse overlay on one board cell. <see cref="LayersBelow"/> are the hidden variants under the visible top layer,
    /// next-to-top first (FR-036).
    /// </summary>
    public sealed record CellOverlay(
        CellPos Cell,
        IReadOnlyList<VariantId> LayersBelow,
        bool Mystery,
        bool Stone,
        bool Hole,
        string? KeyId);
}
