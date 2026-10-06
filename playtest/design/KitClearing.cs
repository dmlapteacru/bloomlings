using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The Store's clearing-style cards (spec 005 FR-038, contracts/look.md §6.12).</summary>
    public static partial class Kit
    {
        /// <summary>
        /// A clearing style's live preview filling <paramref name="well"/> (<c>ui.card.clearing</c>): the small board of
        /// <see cref="ClearPreview"/> in its stone border with the arch under its middle and the slot below, cleared in the
        /// style in a loop on the painter's clock. With <paramref name="pair"/> the free card shows Blossom and Munchers by
        /// turns, a loop each.
        /// </summary>
        public static void ClearingPreview(IPainter p, Box well, ClearStyle style, bool pair = false)
        {
            p.Mark("ui.card.clearing");
            (ClearPreview preview, float t) = ClearPreview.At(style, pair, (float)p.Now);
            Box bounds = ClearPreview.Bounds;
            float cell = Math.Min(well.Width * 0.84f / bounds.Width, well.Height * 0.9f / bounds.Height);
            float ox = well.CenterX - (bounds.Width * cell / 2f);
            float oy = well.CenterY - (bounds.Height * cell / 2f);
            var grid = new Box(ox, oy, ox + (ClearPreview.Columns * cell), oy + (ClearPreview.Rows * cell));
            StoneBorder(p, grid, cell);
            Rgba ground = Visuals.ColorOf(preview.Variant).Lighten(0.55f);
            for (int row = 0; row < ClearPreview.Rows; row++)
            {
                for (int column = 0; column < ClearPreview.Columns; column++)
                {
                    var full = new Box(ox + (column * cell), oy + (row * cell), ox + ((column + 1) * cell), oy + ((row + 1) * cell));
                    if (preview.Cleared(column, row, t) || preview.Held(column, row, t))
                    {
                        BoardPainter.Ground(p, full, cell, ground);
                        continue;
                    }

                    float sway = preview.Sway(column, row, t);
                    if (sway != 0f)
                    {
                        p.PushRotate(sway, full.CenterX, full.Bottom);
                    }

                    CandyTile(p, full.Inset(cell * 0.008f), preview.Variant, TileStyle.Board);
                    if (sway != 0f)
                    {
                        p.PopTransform();
                    }
                }
            }

            var entry = new Box(ox + (ClearPreview.EntryColumn * cell), oy + ((ClearPreview.Rows - 1) * cell), ox + ((ClearPreview.EntryColumn + 1) * cell), oy + (ClearPreview.Rows * cell));
            EntryArch(p, BoardLayout.ArchOf(entry, EntrySide.Bottom));
            Box slot = ClearPreview.Slot;
            var plate = new Box(ox + (slot.Left * cell), oy + (slot.Top * cell), ox + (slot.Right * cell), oy + (slot.Bottom * cell));
            int count = preview.Count(t);
            SlotPlate(p, plate, count > 0 ? SlotPlateState.Working : SlotPlateState.Empty, count > 0 ? preview.Variant : (Core.Variants.VariantId?)null, count);

            var list = new FxList();
            preview.Draw(list, t);
            ClearPainter.DrawItems(p, list.Items, FxLayer.Board, ox, oy, cell);
            ClearPainter.DrawItems(p, list.Items, FxLayer.Over, ox, oy, cell);
        }
    }
}
