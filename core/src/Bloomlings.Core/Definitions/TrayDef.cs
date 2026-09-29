using System.Collections.Generic;

namespace Bloomlings.Core.Definitions
{
    /// <summary>Source Tray stacks of pod ids, top (exposed) first (FR-011).</summary>
    public sealed record TrayDef(IReadOnlyList<IReadOnlyList<string>> Stacks);
}
