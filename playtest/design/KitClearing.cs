using System;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

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

        /// <summary>
        /// A clearing style card's action button (<c>ui.button.clearing</c>, <see cref="ClearingCard.Button"/>; spec 005 FR-038
        /// as amended on 2026-10-06; Unity's <c>UiKit.ClearingButton</c>): <see cref="ClearingAction.Buy"/> the glossy green
        /// face on its plate with the lotus and <paramref name="price"/> in white, no "Buy" word (<see cref="ClearingCard.PriceParts"/>);
        /// <see cref="ClearingAction.Choose"/> the cream face with "Choose" in brown; <see cref="ClearingAction.Chosen"/> a
        /// flat cream plate in a green outline with a green check and "Chosen". It lies inside its card's touch box and takes
        /// the card's tap (the whole card is the button); a <paramref name="pressable"/> face sinks under the finger as every
        /// garden button's. <see cref="ClearingAction.Locked"/> draws nothing (the card keeps its cost pill).
        /// </summary>
        public static void ClearingButton(IPainter p, Box box, ClearingAction action, int price, bool pressable)
        {
            if (action == ClearingAction.Locked)
            {
                return;
            }

            p.Mark("ui.button.clearing");
            float h = box.Height;
            if (action == ClearingAction.Chosen)
            {
                ColorSet green = GardenLook.Green;
                float line = Math.Max(p.U(2f), h * 0.05f);
                SoftShadow(p, box, h / 2f, 0.14f, 0.08f);
                p.FillRoundGradient(box, h / 2f, C.CreamTop, C.CreamFace);
                p.StrokeRound(box.Inset(line / 2f), (h / 2f) - (line / 2f), line, green.Face);
                float size = ClearingCard.LabelSize(box);
                string chosen = PlaytestText.T("clearing.chosen");
                float text = Math.Min(p.MeasureText(chosen, T.ButtonSecondary, size / p.U(T.ButtonSecondary.Size)), box.Width - (h * 1.4f));
                float check = h * 0.56f;
                float start = box.CenterX - ((check + (h * 0.12f) + text) / 2f);
                p.Shape("ui.check", Box.FromCenter(start + (check / 2f), box.CenterY, check, check), green.Face);
                p.Text(chosen, start + check + (h * 0.12f) + (text / 2f), box.CenterY, T.ButtonSecondary, green.Line, text, size / p.U(T.ButtonSecondary.Size), TextLook.Plain(green.Line));
                return;
            }

            bool buy = action == ClearingAction.Buy;
            ColorSet set = buy ? GardenLook.Green : GardenLook.Cream;
            p.Mark(buy ? "ui.button.primary" : "ui.button.secondary");
            float depth = Press(p, box, pressable);
            Squash(p, box, depth);
            Box face = GardenButton(p, box, set, h / 2f, depth, gloss: buy);
            float scale = ClearingCard.LabelSize(box) / p.U(T.ButtonSecondary.Size);
            TextLook look = buy ? TextLook.OnColor(set) : GardenLook.LabelOn(set);
            if (buy)
            {
                string amount = NumberText.Group(price);
                (Box Lotus, Box Price, float Scale) parts = ClearingCard.PriceParts(box, p.MeasureText(amount, T.ButtonSecondary, scale));
                Petal(p, parts.Lotus);
                p.Text(amount, parts.Price.CenterX, parts.Price.CenterY, T.ButtonSecondary, C.TextOnColor, parts.Price.Width + 1f, scale * parts.Scale, look);
            }
            else
            {
                p.Text(PlaytestText.T("clearing.choose"), face.CenterX, face.CenterY, T.ButtonSecondary, C.InkBrown, face.Width * 0.86f, scale, look);
            }

            p.PopTransform();
        }
    }
}
