using Bloomlings.Core.Boards;

namespace Bloomlings.Core.Definitions
{
    public enum EntrySide
    {
        Bottom,
        Left,
        Right,
        Top,
    }

    /// <summary>A Garden Entry: a border cell where Bloomlings enter the board from the given side (FR-009).</summary>
    public sealed record EntryDef(CellPos Cell, EntrySide Side);
}
