using System;
using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Generator
{
    /// <summary>
    /// R9 step 3 (T086): places the Garden Entries for a layout name and picks the mirroring. Layouts:
    /// <c>bottom_center</c> (default, FR-009), <c>bottom_left</c>, <c>bottom_right</c>, <c>two_bottom</c> and
    /// <c>side_left</c>/<c>side_right</c>. An entry never sits on a stone.
    /// </summary>
    public static class EntryPlanner
    {
        public static IReadOnlyList<EntryDef>? Entries(string layout, BasePicture picture, Mirror mirror)
        {
            int w = picture.Width;
            int h = picture.Height;
            var cells = new List<EntryDef>();
            switch (layout)
            {
                case "bottom_center":
                    cells.Add(new EntryDef(new CellPos(w / 2, 0), EntrySide.Bottom));
                    break;
                case "bottom_left":
                    cells.Add(new EntryDef(new CellPos(1, 0), EntrySide.Bottom));
                    break;
                case "bottom_right":
                    cells.Add(new EntryDef(new CellPos(w - 2, 0), EntrySide.Bottom));
                    break;
                case "two_bottom":
                    cells.Add(new EntryDef(new CellPos(w / 4, 0), EntrySide.Bottom));
                    cells.Add(new EntryDef(new CellPos(w - 1 - (w / 4), 0), EntrySide.Bottom));
                    break;
                case "side_left":
                    cells.Add(new EntryDef(new CellPos(0, h / 3), EntrySide.Left));
                    break;
                case "side_right":
                    cells.Add(new EntryDef(new CellPos(w - 1, h / 3), EntrySide.Right));
                    break;
                default:
                    throw new ArgumentException($"Unknown entry layout '{layout}'.", nameof(layout));
            }

            foreach (EntryDef entry in cells)
            {
                int x = mirror == Mirror.Horizontal ? w - 1 - entry.Cell.X : entry.Cell.X;
                if (picture.CellAt(x, entry.Cell.Y) == BasePicture.Stone)
                {
                    return null;
                }
            }

            return cells;
        }
    }
}
