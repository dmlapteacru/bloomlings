using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The reference look's meta pieces (spec 005 <c>specs/005-reference-look/contracts/look.md</c> §4.5, §4.6): the
    /// Wardrobe's family tabs and outfit cards (the playtest's Store cosmetics), the green check badge of a worn item,
    /// a picture frame (the Collection), a parchment pill (Home's rows) and the lotus fountain of the drawn Home stage.
    /// Each piece marks the asset slots it draws; screens only call them.
    /// </summary>
    public static partial class Kit
    {
        /// <summary>
        /// Family tabs joined to the panel below them (§4.6, <c>ui.tab.family</c>; the reference Wardrobe): one cream tab
        /// per family with rounded top corners, the family's 3D hero (in <paramref name="outfitOf"/>'s outfit) and its
        /// name; the selected tab is lighter, a little taller and flows into the lighter panel, the others sit behind its
        /// edge. A tap on a tab calls <paramref name="onSelect"/>. <paramref name="cells"/> places the tabs (the Wardrobe's
        /// <see cref="ReferenceWardrobeRegions.Tab"/>); without it they share <paramref name="tabs"/> evenly. Returns the
        /// panel's content box.
        /// </summary>
        public static Box FamilyTabs(IPainter p, Box tabs, Box panel, IReadOnlyList<Family> families, IReadOnlyList<string> names, int selected, Action<int> onSelect, Func<Family, Outfit?>? outfitOf = null, IReadOnlyList<Box>? cells = null)
        {
            p.Mark("ui.tab.family");
            cells ??= ScreenLayout.Row(tabs, families.Count, p.U(10f), float.MaxValue, square: false);
            float line = Math.Max(1f, p.U(DesignTokens.Garden.OutlineWidth) * 0.8f);
            float radius = Math.Min(cells.Count > 0 ? cells[0].Width * 0.18f : 0f, p.U(34f));
            float panelRadius = p.U(26f);
            float sunk = tabs.Height * 0.07f;

            // The other tabs first: a little lower, their bottoms hidden under the panel's edge.
            for (int i = 0; i < cells.Count; i++)
            {
                if (i != selected)
                {
                    Box tab = new Box(cells[i].Left, cells[i].Top + sunk, cells[i].Right, panel.Top + panelRadius);
                    TabFace(p, tab, radius, C.CreamFace, C.CreamFace.Darken(0.05f), line, panel.Top + line);
                }
            }

            // The panel: lighter than the card's parchment, with a thin tan outline.
            p.FillRoundGradient(panel, panelRadius, C.ParchmentTop, C.CreamTop);
            p.StrokeRound(panel.Inset(line / 2f), panelRadius - (line / 2f), line, C.CreamLine);

            if (selected >= 0 && selected < cells.Count)
            {
                // The selected tab flows into the panel: its face covers the panel's outline under it.
                Box tab = new Box(cells[selected].Left, cells[selected].Top, cells[selected].Right, panel.Top + panelRadius);
                TabFace(p, tab, radius, C.ParchmentTop.Lighten(0.3f), C.ParchmentTop, line, panel.Top + (line * 1.5f), outlineBottom: panel.Top);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                bool on = i == selected;
                Box cell = cells[i];
                float top = cell.Top + (on ? 0f : sunk);
                float h = cell.Bottom - top;
                float figure = Math.Min(cell.Width * 0.66f, h * 0.6f);
                Box hero = Box.FromCenter(cell.CenterX, top + (h * 0.08f) + (figure * 0.52f), figure * CharacterArt.HeroWidth / CharacterArt.HeroHeight, figure);
                Visuals.Hero(p, hero, families[i], outfitOf?.Invoke(families[i]));
                Rgba ink = on ? C.InkBrown : C.InkBrownSoft;
                float scale = Math.Min(1f, (h * 0.2f) / p.U(T.ButtonSecondary.Size));
                p.Text(names[i], cell.CenterX, top + (h * 0.83f), T.ButtonSecondary, ink, cell.Width * 0.9f, scale, TextLook.Plain(ink));
                int index = i;
                p.Hit(Touch(p, cell), () => onSelect(index));
            }

            return panel.Inset(p.U(22f));
        }

        /// <summary>
        /// A tab's face: <paramref name="tab"/> with its top corners rounded by <paramref name="radius"/> and its bottom
        /// cut at <paramref name="clipBottom"/>; the outline stops at <paramref name="outlineBottom"/> (the panel's edge).
        /// </summary>
        private static void TabFace(IPainter p, Box tab, float radius, Rgba top, Rgba bottom, float line, float clipBottom, float? outlineBottom = null)
        {
            var tall = new Box(tab.Left, tab.Top, tab.Right, tab.Bottom + radius);
            p.PushClip(new Box(tab.Left - line, tab.Top - line, tab.Right + line, clipBottom));
            p.FillRoundGradient(tall, radius, top, bottom);
            p.PopClip();
            p.PushClip(new Box(tab.Left - line, tab.Top - line, tab.Right + line, outlineBottom ?? clipBottom));
            p.StrokeRound(tall.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine);
            p.PopClip();
        }

        /// <summary>
        /// An outfit card (§4.6, <c>ui.card.outfit</c>; the reference Wardrobe): a cream card with a beige picture well,
        /// which <paramref name="picture"/> fills (the hero wearing the item), and the item's name below it. The worn one
        /// (<paramref name="worn"/>) has a green-tinted well with a green border and the check badge. A
        /// <paramref name="cost"/> adds the cost pill on the card's bottom edge (the Store); <paramref name="box"/> then
        /// holds the card and the pill below it (<paramref name="pillRoom"/> keeps that room without a pill, so cards in a
        /// row line up), and the whole box is the touch target. A <paramref name="locked"/> item (earned later, the
        /// Wardrobe) fades its picture and carries the padlock badge instead.
        /// </summary>
        public static void OutfitCard(IPainter p, Box box, string name, bool worn, Action<Box> picture, Cost? cost = null, Action? action = null, bool pillRoom = false, bool locked = false)
        {
            p.Mark("ui.card.outfit");
            const float pillShare = 0.2f;
            float bodyHeight = cost.HasValue || pillRoom ? box.Height / (1f + (0.6f * pillShare)) : box.Height;
            var card = new Box(box.Left, box.Top, box.Right, box.Top + bodyHeight);
            float depth = Press(p, box, action != null);
            Squash(p, card, depth, tile: true);
            float w = card.Width;
            float radius = w * 0.11f;
            float lip = w * 0.035f;
            float line = Math.Max(1f, w * 0.011f);
            SoftShadow(p, card, radius, 0.18f, 0.035f);
            p.FillRound(card, radius, C.CreamLip);
            var face = new Box(card.Left, card.Top, card.Right, card.Bottom - lip);
            p.FillRoundGradient(face, radius, C.CreamTop, C.CreamFace);
            p.StrokeRound(card.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine);

            float pad = w * 0.075f;
            var well = new Box(face.Left + pad, face.Top + pad, face.Right - pad, face.Top + pad + (face.Height * 0.64f));
            float wellRadius = radius * 0.7f;
            if (worn)
            {
                p.FillRoundGradient(well, wellRadius, GardenLook.Green.Top.Mix(C.CreamTop, 0.55f), GardenLook.Green.Face.Mix(C.CreamTop, 0.5f));
            }
            else
            {
                p.FillRoundGradient(well, wellRadius, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell);
            }

            p.PushClip(well);
            p.FillRoundGradient(new Box(well.Left, well.Top, well.Right, well.Top + (well.Height * 0.2f)), wellRadius, C.GardenShadow.WithAlpha(0.1f), C.GardenShadow.WithAlpha(0f));
            p.PushAlpha(locked ? GardenLook.PictureDisabledAlpha : 1f);
            picture(well);
            p.PopAlpha();
            p.PopClip();
            if (worn)
            {
                float border = Math.Max(p.U(4f), w * 0.022f);
                p.StrokeRound(well.Inset(border / 2f), wellRadius - (border / 2f), border, GardenLook.Green.Face);
                float badge = w * 0.22f;
                CheckBadge(p, well.Right - (badge * 0.42f), well.Bottom - (badge * 0.42f), badge);
            }
            else
            {
                p.StrokeRound(well.Inset(line / 2f), wellRadius - (line / 2f), line, C.ParchmentEdge.Darken(0.08f));
            }

            if (locked)
            {
                float badge = w * 0.22f;
                LockBadge(p, well.Right - (badge * 0.42f), well.Bottom - (badge * 0.42f), badge);
            }

            float nameTop = well.Bottom;
            float nameBottom = cost.HasValue || pillRoom ? face.Bottom - ((box.Height - bodyHeight) * 0.5f) : face.Bottom;
            float scale = Math.Min(1f, ((nameBottom - nameTop) * 0.62f) / p.U(T.ButtonSecondary.Size));
            Rgba ink = locked ? C.InkBrownSoft : C.InkBrown;
            p.Text(name, card.CenterX, (nameTop + nameBottom) / 2f, T.ButtonSecondary, ink, w * 0.88f, scale, TextLook.Plain(ink));
            p.PopTransform();

            if (cost.HasValue)
            {
                float pillHeight = bodyHeight * pillShare;
                CostPill(p, Box.FromCenter(card.CenterX, card.Bottom + (pillHeight * 0.1f), w * 0.78f, pillHeight), cost.Value);
            }

            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>
        /// A cream round ‹ or › button (§4.6: the Wardrobe's arrows; the Store's pages): the round button's domed cushion
        /// with a brown chevron in its cream halo, pointing right when <paramref name="next"/>. Without an action it is
        /// greyed and takes no tap.
        /// </summary>
        public static void ArrowButton(IPainter p, float cx, float cy, float size, bool next, Action? action)
        {
            p.Mark("ui.button.round");
            p.Mark("ui.chevron");
            Box box = Box.FromCenter(cx, cy, size, size);
            p.PushAlpha(action != null ? 1f : 0.45f);
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            Box face = IconFace(p, box, GardenLook.White, size / 2f, depth);
            Func<float, float, float> chevron = ShapeLibrary.Get("ui.chevron");
            float m = next ? 1f : -1f;
            string key = "ui.chevron" + (next ? "/next" : "/previous");
            Box glyph = Box.FromCenter(face.CenterX + (size * 0.03f * m), face.CenterY, size * 0.5f, size * 0.5f);
            p.ShapeOf(key + "/halo", (x, y) => chevron(m * x, y) - 0.06f, glyph, C.CreamTop);
            p.ShapeOf(key, (x, y) => chevron(m * x, y), glyph, C.InkBrown);
            p.PopTransform();
            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The worn item's badge (§4.6): a green disc with a white ring and a white check, over a soft shadow.</summary>
        public static void CheckBadge(IPainter p, float cx, float cy, float size)
        {
            float ring = size * 0.1f;
            Box outer = Box.FromCenter(cx, cy, size + (2f * ring), size + (2f * ring));
            SoftShadow(p, outer, outer.Width / 2f, 0.25f, 0.06f);
            p.FillCircle(cx, cy, outer.Width / 2f, Rgba.White);
            p.FillRoundGradient(Box.FromCenter(cx, cy, size, size), size / 2f, GardenLook.Green.Top, GardenLook.Green.Face);
            p.StrokeCircle(cx, cy, (size / 2f) - Math.Max(0.5f, size * 0.02f), Math.Max(1f, size * 0.04f), GardenLook.Green.Line);
            p.Shape("ui.check", Box.FromCenter(cx, cy, size * 0.58f, size * 0.58f), Rgba.White);
        }

        /// <summary>
        /// A locked item's badge (the Wardrobe's outfit cards): a domed cream disc in a <c>cream.line</c> ring with the
        /// brown padlock, over a soft shadow, where the worn item's check would be.
        /// </summary>
        public static void LockBadge(IPainter p, float cx, float cy, float size)
        {
            p.Mark("ui.lock");
            float ring = size * 0.08f;
            Box outer = Box.FromCenter(cx, cy, size + (2f * ring), size + (2f * ring));
            SoftShadow(p, outer, outer.Width / 2f, 0.25f, 0.06f);
            p.FillCircle(cx, cy, outer.Width / 2f, C.CreamLine);
            p.FillRoundGradient(Box.FromCenter(cx, cy, size, size), size / 2f, C.CreamTop, C.CreamFace);
            p.Shape("ui.lock", Box.FromCenter(cx, cy, size * 0.56f, size * 0.56f), C.InkBrown);
        }

        /// <summary>
        /// The Wardrobe's name card (§6.5, the reference Wardrobe; <c>mat.parchment</c>): a wide parchment card whose middle
        /// rises into a tab (<paramref name="tab"/>: rounded top corners, flowing into the card) carrying
        /// <paramref name="name"/> in <c>type.title</c> <c>ink.title</c>. The card's body starts at the tab's middle and
        /// runs to <paramref name="card"/>'s bottom, where the family tabs cover it. Never a touch target.
        /// </summary>
        public static void NameCard(IPainter p, Box card, Box tab, string name)
        {
            float line = p.U(DesignTokens.Garden.FrameWidth) * 0.8f;
            var body = new Box(card.Left, tab.Top + (tab.Height * 0.42f), card.Right, card.Bottom);
            float radius = Math.Min(body.Width, body.Height) * 0.16f;
            Paper(p, body, radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard * 0.5f);

            // The tab: the card's parchment rising above its edge, its face covering the card's outline under it and its own
            // outline stopping where it meets the card.
            float r = tab.Height * 0.34f;
            Rgba join = C.ParchmentTop.Mix(C.ParchmentBottom, 0.08f);
            var tall = new Box(tab.Left, tab.Top, tab.Right, body.Top + (tab.Height * 0.6f));
            SoftShadow(p, new Box(tab.Left, tab.Top, tab.Right, body.Top), r, 0.12f, 0.04f);
            p.PushClip(new Box(tab.Left - line, tab.Top - line, tab.Right + line, body.Top + (line * 2.5f)));
            p.FillRoundGradient(tall, r, C.ParchmentTop.Lighten(0.2f), join);
            p.PopClip();
            p.PushClip(new Box(tab.Left - line, tab.Top - line, tab.Right + line, body.Top + (line * 0.5f)));
            p.StrokeRound(tall.Inset(line / 2f), r - (line / 2f), line, C.ParchmentLine);
            p.PopClip();

            float scale = Math.Min(tab.Height * 0.62f, tab.Width * 0.13f) / p.U(T.Title.Size);
            p.Text(name, tab.CenterX, tab.CenterY + (tab.Height * 0.04f), T.Title, C.InkTitle, tab.Width * 0.86f, scale, TextLook.Plain(C.InkTitle));
        }

        /// <summary>
        /// A picture frame (the Collection, <c>collection.frame</c>): a raised cream frame with a lip and a soft shadow
        /// around a beige well. Returns the well, where the picture goes.
        /// </summary>
        public static Box PictureFrame(IPainter p, Box box)
        {
            p.Mark("collection.frame");
            float w = Math.Min(box.Width, box.Height);
            float radius = w * 0.1f;
            float lip = w * 0.035f;
            float line = Math.Max(1f, w * 0.01f);
            SoftShadow(p, box, radius, 0.2f, 0.035f);
            p.FillRound(box, radius, C.CreamLip);
            var face = new Box(box.Left, box.Top, box.Right, box.Bottom - lip);
            p.FillRoundGradient(face, radius, C.CreamTop, C.CreamFace);
            p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine);
            Box well = face.Inset(w * 0.07f);
            float wellRadius = radius * 0.6f;
            p.FillRoundGradient(well, wellRadius, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell);
            p.PushClip(well);
            p.FillRoundGradient(new Box(well.Left, well.Top, well.Right, well.Top + (well.Height * 0.16f)), wellRadius, C.GardenShadow.WithAlpha(0.12f), C.GardenShadow.WithAlpha(0f));
            p.PopClip();
            p.StrokeRound(well.Inset(line / 2f), wellRadius - (line / 2f), line, C.ParchmentEdge.Darken(0.08f));
            return well;
        }

        /// <summary>
        /// A parchment pill (Home's milestone teaser and rank rows; spec 005 §3.5): the parchment surface with a soft
        /// shadow, rounded to a pill.
        /// </summary>
        public static void ParchmentPill(IPainter p, Box box) => Paper(p, box, box.Height / 2f, DesignTokens.Garden.OutlineWidth, 5f);

        /// <summary>
        /// The lotus fountain on the drawn Home stage (<c>ui.fountain</c>; the reference's Home diorama, until the owner's
        /// picture): a small stone basin, water with a light rim, two lily pads and the pink lotus rising from it.
        /// </summary>
        public static void LotusFountain(IPainter p, Box box)
        {
            p.Mark("ui.fountain");
            Box top = StonePedestal(p, box);
            float rx = top.Width * 0.4f;
            float ry = top.Height * 0.36f;
            float cx = top.CenterX;
            float cy = top.CenterY + (top.Height * 0.04f);
            Rgba water = C.ButtonBlue.Lighten(0.42f);
            p.PushSquash(1f, ry / rx, cx, cy);
            p.FillCircle(cx, cy, rx + Math.Max(1f, rx * 0.04f), C.StoneLine.WithAlpha(0.5f));
            p.FillCircle(cx, cy, rx, water);
            p.FillCircle(cx - (rx * 0.18f), cy - (rx * 0.12f), rx * 0.62f, water.Lighten(0.35f).WithAlpha(0.7f));
            p.StrokeCircle(cx, cy, rx * 0.8f, Math.Max(1f, rx * 0.03f), Rgba.White.WithAlpha(0.6f));
            p.PopTransform();

            // Two lily pads beside the lotus.
            float pad = rx * 0.42f;
            foreach (float side in new[] { -1f, 1f })
            {
                float px = cx + (side * rx * 0.5f);
                p.PushSquash(1f, 0.45f, px, cy);
                p.FillCircle(px, cy, pad + Math.Max(1f, pad * 0.08f), C.IvyLine);
                p.FillCircle(px, cy, pad, C.LawnLight);
                p.FillCircle(px - (pad * 0.2f), cy - (pad * 0.2f), pad * 0.55f, C.IvyLeaf.Lighten(0.2f));
                p.PopTransform();
            }

            float lotus = box.Width * 0.5f;
            Petal(p, Box.FromCenter(cx, cy - (lotus * 0.3f), lotus, lotus));
        }
    }
}
