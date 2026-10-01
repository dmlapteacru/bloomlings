using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The design board's components drawn through <see cref="IPainter"/> (spec 002 FR-005, FR-007; the playtest twin of
    /// the Unity client's <c>UiKit</c>). The kit covers:
    /// <list type="bullet">
    /// <item><description>primary and secondary buttons with a darker lower edge;</description></item>
    /// <item><description>round icon buttons;</description></item>
    /// <item><description>the level, 2× and Petals pills;</description></item>
    /// <item><description>badges;</description></item>
    /// <item><description>popup cards and the bottom sheet;</description></item>
    /// <item><description>list rows, tabs and toggles.</description></item>
    /// </list>
    /// Sizes are reference units scaled by <see cref="IPainter.Scale"/>, and colors are design tokens.
    /// </summary>
    public static class Kit
    {
        public static float U(this IPainter p, float reference) => reference * p.Scale;

        /// <summary>A raised pill or rounded rectangle: the edge color below, the face on top (elev.raised). Returns the face.</summary>
        public static Box Raised(IPainter p, Box box, Rgba face, Rgba edge, float radius, bool pressed = false, Rgba? top = null)
        {
            float lift = p.U(DesignTokens.Elevation.RaisedEdge);
            if (pressed)
            {
                Box sunk = new Box(box.Left, box.Top + lift, box.Right, box.Bottom);
                p.FillRound(sunk, radius, face);
                return sunk;
            }

            p.FillRound(box, radius, edge);
            Box faceBox = new Box(box.Left, box.Top, box.Right, box.Bottom - lift);
            if (top.HasValue)
            {
                p.FillRoundGradient(faceBox, radius, top.Value, face);
            }
            else
            {
                p.FillRound(faceBox, radius, face);
            }

            return faceBox;
        }

        // ---- The Garden recipe (spec 003 FR-006 to FR-008) ----

        /// <summary>
        /// The flat cream plate an element lies on (FR-006): a soft shadow, its thickness below, a cream gradient and a
        /// thin brown outline. It fills <paramref name="box"/> less its thickness, and returns the plate's top face.
        /// </summary>
        public static Box Plate(IPainter p, Box box, float radius)
        {
            float h = box.Height / p.Scale;
            float depth = p.U(DesignTokens.Garden.Depth(h));
            float line = p.U(DesignTokens.Garden.Outline(h));
            var plate = new Box(box.Left, box.Top, box.Right, box.Bottom - depth);
            float r = Math.Min(radius, plate.Height / 2f);
            p.FillRound(plate.Offset(0f, depth * 2f).Inset(-p.U(2f), 0f), r, C.GardenShadow.WithAlpha(0.14f));
            p.FillRound(plate.Offset(0f, depth), r, C.GardenPlateDepth);
            p.FillRoundGradient(plate, r, C.GardenPlateTop, C.GardenPlateBottom);
            p.StrokeRound(plate.Inset(line / 2f), r - (line / 2f), line, C.GardenOutline);
            return plate;
        }

        /// <summary>
        /// A raised button face in a color set (FR-007): the lip along its bottom edge, the face with its lighter top, a
        /// highlight band and an outline in the set's line. <paramref name="depth"/> is the press (1 sunk into the lip,
        /// below 0 the spring's overshoot). Returns the content box above the lip, moved with the press.
        /// </summary>
        public static Box Face(IPainter p, Box face, ColorSet set, float radius, float depth, bool highlight = true, float? lipUnits = null)
        {
            float h = face.Height / p.Scale;
            float lip = p.U(lipUnits ?? DesignTokens.Garden.Lip(h));
            float line = p.U(DesignTokens.Garden.Outline(h));
            float travel = Math.Max(0f, lip - p.U(3f));
            float shift = travel * Math.Max(-0.25f, Math.Min(1f, depth));
            float r = Math.Min(radius, face.Height / 2f);

            var whole = new Box(face.Left, face.Top + Math.Max(0f, shift), face.Right, face.Bottom);
            p.FillRound(whole, r, set.Lip);
            var top = new Box(face.Left, face.Top + shift, face.Right, face.Bottom - lip + shift);
            p.FillRoundGradient(top, Math.Min(r, top.Height / 2f), set.Top, set.Face);
            if (highlight)
            {
                float inset = top.Width * 0.07f;
                var band = new Box(top.Left + inset, top.Top + (top.Height * 0.07f), top.Right - inset, top.Top + (top.Height * DesignTokens.Garden.HighlightHeight));
                p.FillRoundGradient(band, band.Height / 2f, Rgba.White.WithAlpha(DesignTokens.Garden.HighlightAlpha), Rgba.White.WithAlpha(0f));
            }

            var outline = new Box(face.Left, Math.Min(top.Top, whole.Top), face.Right, face.Bottom);
            p.StrokeRound(outline.Inset(line / 2f), Math.Min(r, outline.Height / 2f) - (line / 2f), line, set.Line);
            return top;
        }

        /// <summary>
        /// A garden button: the plate, then the raised face inset on it (FR-006, FR-007). Returns the face's content box.
        /// Pills pass <c>box.Height / 2</c> as the radius; round buttons a square box.
        /// </summary>
        public static Box GardenButton(IPainter p, Box box, ColorSet set, float radius, float depth, bool highlight = true)
        {
            Box plate = Plate(p, box, radius);
            float inset = p.U(DesignTokens.Garden.PlateInset(box.Height / p.Scale));
            Box face = plate.Inset(inset);
            return Face(p, face, set, Math.Max(0f, Math.Min(radius, plate.Height / 2f) - inset), depth, highlight);
        }

        /// <summary>The press of an element (FR-017): its depth now, from the finger and the spring-back.</summary>
        public static float Press(IPainter p, Box box, bool enabled) => enabled ? GardenLook.PressDepth(p.Pressed(box), p.Released(box)) : 0f;

        /// <summary>Starts the squash of a pressed element about its bottom center; end it with <see cref="IPainter.PopTransform"/>.</summary>
        public static void Squash(IPainter p, Box box, float depth, bool tile = false)
        {
            (float sx, float sy) = GardenLook.Squash(depth, tile);
            p.PushSquash(sx, sy, box.CenterX, box.Bottom);
        }

        /// <summary>
        /// A glyph on a garden face (FR-010): light with a dark line under it on colored faces, dark on cream and white.
        /// </summary>
        public static void Glyph(IPainter p, string shapeId, Box box, ColorSet set)
        {
            Rgba glyph = GardenLook.GlyphOn(set);
            if (GardenLook.LabelOn(set).Volumetric)
            {
                p.Shape(shapeId, box.Offset(0f, box.Height * 0.06f), set.Line);
            }

            p.Shape(shapeId, box, glyph);
        }

        /// <summary>
        /// A shape with the label look (the ▶ of PLAY, FR-010): its extrusion and outline in the look's line, then the
        /// fill, so it matches the letters next to it. <paramref name="em"/> is the letters' size in pixels.
        /// </summary>
        public static void VolumetricShape(IPainter p, string shapeId, Box box, TextLook look, float em)
        {
            float unit = box.Width / 2f / ShapeRaster.Margin;
            float grow = look.OutlineEm * em / Math.Max(1f, unit);
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            string key = shapeId + "/line/" + Math.Round(grow, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
            Func<float, float, float> lined = (x, y) => sdf(x, y) - grow;
            float step = Math.Max(1f, look.ExtrudeEm * em / 4f);
            p.ShapeOf(key, lined, box.Offset(0f, (look.ExtrudeEm + 0.05f) * em), C.GardenShadow.WithAlpha(look.ShadowAlpha));
            for (int k = 4; k >= 0; k--)
            {
                p.ShapeOf(key, lined, box.Offset(0f, k * step), look.Outline);
            }

            p.Shape(shapeId, box, look.FillTop);
        }

        /// <summary>
        /// The leaves and white flower over a main button's corners (FR-011a): never a touch target, drawn after the button
        /// so they may overlap its plate, and switched off as one setting (<see cref="DesignTokens.Garden.Decorations"/>).
        /// </summary>
        public static void Decoration(IPainter p, Box button)
        {
            if (!DesignTokens.Garden.Decorations)
            {
                return;
            }

            p.Mark("ui.deco.garden");
            (Box topLeft, Box bottomRight) = GardenLook.DecorationBoxes(button);
            DecorationCluster(p, topLeft, flipped: false);
            DecorationCluster(p, bottomRight, flipped: true);
        }

        private static void DecorationCluster(IPainter p, Box box, bool flipped)
        {
            foreach (DecorationPart part in GardenLook.DecorationParts)
            {
                (Rgba fill, Rgba line) = GardenLook.DecorationColors(part);
                bool leaf = part != DecorationPart.Petals && part != DecorationPart.Center;
                float stroke = leaf ? 0.032f : 0.02f;
                string key = "ui.deco.garden/" + part + (flipped ? "/flipped" : string.Empty);
                p.ShapeOf(key + "/line", ShapeLibrary.DecorationPartSdf(part, stroke, flipped), box, line);
                p.ShapeOf(key, ShapeLibrary.DecorationPartSdf(part, -stroke * 0.6f, flipped), box, fill);
            }
        }

        /// <summary>
        /// A short sparkle burst around a point (spec 003 FR-020): eight sparkles fly out and fade in 0.8 s.
        /// <paramref name="since"/> is the seconds since the reward; nothing draws outside 0–0.8 s.
        /// </summary>
        public static void SparkleBurst(IPainter p, float cx, float cy, float size, float since)
        {
            const float seconds = 0.8f;
            if (since < 0f || since >= seconds)
            {
                return;
            }

            p.Mark("fx.sparkle");
            float k = since / seconds;
            p.PushAlpha(1f - (k * k));
            for (int i = 0; i < 8; i++)
            {
                double a = (i * Math.PI / 4) + 0.3;
                float d = size * (0.4f + (1.1f * Ease(k)));
                float s = size * (0.36f + (0.2f * (i % 2))) * (1f - (0.4f * k));
                Rgba color = i % 2 == 0 ? C.PetalCenter : Rgba.White;
                p.Shape("fx.sparkle", Box.FromCenter(cx + (float)(Math.Cos(a) * d), cy + (float)(Math.Sin(a) * d), s, s), color);
            }

            p.PopAlpha();
        }

        // ---- Buttons ----

        /// <summary>
        /// The green primary button (PLAY, NEXT, CLAIM, RESUME, CONTINUE, Free rescue): a raised green pill on a cream
        /// plate with a volumetric label (FR-009, FR-012). <paramref name="decorate"/> adds the leaves and flower,
        /// <paramref name="playArrow"/> the ▶ as tall as the letters, and <paramref name="breathe"/> the idle breath of
        /// the one waiting button (FR-019).
        /// </summary>
        public static void PrimaryButton(IPainter p, Box box, string label, Action? action, TypeStyle? style = null, string? iconId = null, bool decorate = false, bool playArrow = false, bool breathe = false)
        {
            p.Mark("ui.button.primary");
            bool enabled = action != null;
            ColorSet set = enabled ? GardenLook.Green : GardenLook.Green.Disabled();
            float depth = Press(p, box, enabled);
            bool breathing = breathe && enabled && depth == 0f;
            if (breathing)
            {
                p.PushTransform(0f, 0f, GardenLook.Breathe(p.Now), box.CenterX, box.CenterY);
            }

            Squash(p, box, depth);
            Box f = GardenButton(p, box, set, box.Height / 2f, depth, highlight: enabled);
            TypeStyle s = style ?? T.Button;
            TextLook look = TextLook.OnColor(set);
            float side = f.Height * 0.45f;
            if (playArrow)
            {
                p.Mark("ui.play");
                float em = s.Size * p.Scale;
                float textWidth = p.MeasureText(label, s);
                float arrow = em * 0.92f;
                float gap = em * 0.1f;
                float k = Math.Min(1f, (f.Width - (side * 2f)) / Math.Max(1f, textWidth + gap + arrow));
                float start = f.CenterX - (((textWidth + gap + arrow) * k) / 2f);
                p.Text(label, start + (textWidth * k / 2f), f.CenterY, s, C.TextOnColor, sizeScale: k, look: look);
                VolumetricShape(p, "ui.play", Box.FromCenter(start + ((textWidth + gap) * k) + (arrow * k / 2f), f.CenterY + (em * k * 0.02f), arrow * k, arrow * k), look, em * k);
            }
            else if (iconId != null)
            {
                float icon = f.Height * 0.5f;
                float textWidth = Math.Min(p.MeasureText(label, s), f.Width - (f.Height * 1.4f));
                float start = f.CenterX - ((icon + p.U(14f) + textWidth) / 2f);
                Glyph(p, iconId, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), set);
                p.Text(label, start + icon + p.U(14f) + (textWidth / 2f), f.CenterY, s, C.TextOnColor, textWidth, look: look);
            }
            else
            {
                p.Text(label, f.CenterX, f.CenterY, s, C.TextOnColor, f.Width - (side * 2f), look: look);
            }

            if (decorate)
            {
                Decoration(p, box);
            }

            p.PopTransform();
            if (breathing)
            {
                p.PopTransform();
            }

            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The cream secondary button (RESTART, SETTINGS, HOME, Restart, ×2 reward, Get +N): dark brown label.</summary>
        public static void SecondaryButton(IPainter p, Box box, string label, Action? action, string? iconId = null, TypeStyle? style = null)
        {
            p.Mark("ui.button.secondary");
            bool enabled = action != null;
            float depth = Press(p, box, enabled);
            p.PushAlpha(enabled ? 1f : 0.55f);
            Squash(p, box, depth);
            Box f = GardenButton(p, box, GardenLook.Cream, box.Height / 2f, depth, highlight: enabled);
            TypeStyle s = style ?? T.ButtonSecondary;
            TextLook look = GardenLook.LabelOn(GardenLook.Cream);
            if (iconId != null)
            {
                float icon = f.Height * 0.46f;
                float textWidth = Math.Min(p.MeasureText(label, s), f.Width - (f.Height * 1.4f));
                float start = f.CenterX - ((icon + p.U(14f) + textWidth) / 2f);
                Glyph(p, iconId, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), GardenLook.Cream);
                p.Text(label, start + icon + p.U(14f) + (textWidth / 2f), f.CenterY, s, C.GardenLabelPlain, textWidth, look: look);
            }
            else
            {
                p.Text(label, f.CenterX, f.CenterY, s, C.GardenLabelPlain, f.Width - (f.Height * 0.7f), look: look);
            }

            p.PopTransform();
            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>
        /// A round icon button on a round plate (Settings, Pause, close, Wardrobe, Collection): white by default, red for
        /// close (FR-012, FR-015).
        /// </summary>
        public static void RoundButton(IPainter p, float cx, float cy, float size, string shapeId, Action? action, Rgba? glyph = null, ColorSet? set = null)
        {
            p.Mark("ui.button.round");
            Box box = Box.FromCenter(cx, cy, size, size);
            ColorSet colors = set ?? GardenLook.White;
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            Box f = GardenButton(p, box, colors, size / 2f, depth);
            float g = Math.Min(f.Width, f.Height) * 0.6f;
            Box glyphBox = Box.FromCenter(f.CenterX, f.CenterY, g, g);
            if (glyph.HasValue)
            {
                p.Shape(shapeId, glyphBox, glyph.Value);
            }
            else
            {
                Glyph(p, shapeId, glyphBox, colors);
            }

            p.PopTransform();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The dark 2× pill of the gameplay top bar.</summary>
        public static void DarkPill(IPainter p, Box box, string label, Action? action)
        {
            p.Mark("ui.pill.speed");
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            Box f = GardenButton(p, box, GardenLook.Dark, box.Height / 2f, depth);
            p.Text(label, f.CenterX, f.CenterY, T.LevelPill, C.TextOnColor, f.Width * 0.8f, look: TextLook.OnColor(GardenLook.Dark));
            p.PopTransform();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The "Level N" pill: sky blue, lilac on Super Hard.</summary>
        public static void LevelPill(IPainter p, Box box, string text, bool superHard)
        {
            p.Mark("ui.pill.level");
            ColorSet set = superHard ? GardenLook.Lilac : GardenLook.Blue;
            Box f = GardenButton(p, box, set, box.Height / 2f, 0f);
            p.Text(text, f.CenterX, f.CenterY, T.LevelPill, C.TextOnColor, f.Width * 0.86f, look: TextLook.OnColor(set));
        }

        /// <summary>The HARD or SUPER HARD badge under the level pill (frames 8 and 9): a sticker pill on a plate (FR-014).</summary>
        public static void Badge(IPainter p, Box box, string text, Rgba color, string slotId)
        {
            p.Mark(slotId);
            ColorSet set = ColorSet.From(slotId, color);
            Box f = GardenButton(p, box, set, box.Height / 2f, 0f);
            p.Text(text, f.CenterX, f.CenterY, T.Badge, C.TextOnColor, f.Width * 0.86f, look: TextLook.OnColor(set));
        }

        /// <summary>A count badge (booster charges, "×N"): white on a dark brown disc with a cream ring and a brown outline.</summary>
        public static void CountBadge(IPainter p, float cx, float cy, float size, string text)
        {
            p.Mark("ui.badge.count");
            float width = Math.Max(size, p.MeasureText(text, T.Badge, size / p.U(48f)) + (size * 0.55f));
            Box box = Box.FromCenter(cx, cy, width, size);
            float ring = Math.Max(p.U(3f), size * 0.08f);
            float line = Math.Max(p.U(2f), size * 0.04f);
            p.FillRound(box.Inset(-(ring + line)).Offset(0f, size * 0.08f), (size / 2f) + ring + line, C.GardenShadow.WithAlpha(0.25f));
            p.FillRound(box.Inset(-(ring + line)), (size / 2f) + ring + line, C.GardenOutline);
            p.FillRound(box.Inset(-ring), (size / 2f) + ring, C.GardenBadgeRing);
            p.FillRound(box, size / 2f, C.GardenBadge);
            p.Text(text, cx, cy + (size * 0.03f), T.Badge, C.TextOnColor, width * 0.9f, sizeScale: size / p.U(48f));
        }

        /// <summary>
        /// A price tag (a booster without charges, FR-014): the Petal symbol and the price in dark brown on a cream tag
        /// with a brown outline.
        /// </summary>
        public static void PriceTag(IPainter p, Box box, int price)
        {
            p.Mark("ui.badge.count");
            float line = p.U(DesignTokens.Garden.OutlineWidthSmall + 1f);
            p.FillRound(box.Offset(0f, p.U(4f)), box.Height / 2f, C.GardenPlateDepth);
            p.FillRound(box, box.Height / 2f, GardenLook.Cream.Face);
            p.StrokeRound(box.Inset(line / 2f), (box.Height / 2f) - (line / 2f), line, C.GardenOutline);
            string text = NumberText.Group(price);
            float icon = box.Height * 0.78f;
            float scale = box.Height / p.U(56f);
            float textWidth = Math.Min(p.MeasureText(text, T.Badge, scale), box.Width - icon - (box.Height * 0.5f));
            float start = box.CenterX - ((icon + p.U(4f) + textWidth) / 2f);
            Petal(p, Box.FromCenter(start + (icon / 2f), box.CenterY, icon, icon));
            p.Text(text, start + icon + p.U(4f) + (textWidth / 2f), box.CenterY, T.Badge, C.GardenLabelPlain, textWidth, scale);
        }

        /// <summary>The Petal symbol: pink petals around a yellow center, with an outline (FR-006, spec 003 FR-010).</summary>
        public static void Petal(IPainter p, Box box)
        {
            float grow = 0.07f;
            Func<float, float, float> sdf = ShapeLibrary.Get("currency.petal");
            p.ShapeOf("currency.petal/line", (x, y) => sdf(x, y) - grow, box, C.PetalEdge);
            p.Shape("currency.petal", box, C.PetalFill);
            p.FillCircle(box.CenterX, box.CenterY, box.Width * 0.2f, C.PetalCenter);
        }

        /// <summary>
        /// The Petals balance pill (frames 2, 3 and 17): a white raised pill on a plate with the Petal symbol and the
        /// balance, and its round green "+" on its own plate (FR-013).
        /// </summary>
        public static void PetalsPill(IPainter p, Box box, long petals, Action? onPlus)
        {
            p.Mark("ui.pill.petals");
            Box f = GardenButton(p, box, GardenLook.White, box.Height / 2f, 0f);
            float icon = box.Height * 0.86f;
            Petal(p, Box.FromCenter(box.Left + (box.Height * 0.5f), f.CenterY, icon, icon));
            float right = onPlus != null ? box.Right - box.Height : box.Right - (box.Height * 0.3f);
            p.Text(NumberText.Group(petals), (box.Left + box.Height + right) / 2f, f.CenterY, T.Count, C.GardenLabelPlain, right - box.Left - box.Height, look: TextLook.Plain(C.GardenLabelPlain));
            if (onPlus != null)
            {
                float size = box.Height * 0.84f;
                Box plus = Box.FromCenter(box.Right - (box.Height * 0.5f), box.CenterY, size, size);
                float depth = Press(p, Touch(p, box), true);
                Squash(p, plus, depth);
                Box face = GardenButton(p, plus, GardenLook.Green, size / 2f, depth);
                float g = face.Height * 0.62f;
                Glyph(p, "ui.plus", Box.FromCenter(face.CenterX, face.CenterY, g, g), GardenLook.Green);
                p.PopTransform();
                p.Hit(Touch(p, box), onPlus);
            }
        }

        /// <summary>The dimmed backdrop behind popups; a tap on it does nothing (the card decides).</summary>
        public static void Scrim(IPainter p, float alpha = 1f)
        {
            p.FillRect(new Box(0f, 0f, p.Width, p.Height), C.SurfaceScrim.WithAlpha(C.SurfaceScrim.A / 255f * alpha));
            p.Hit(new Box(0f, 0f, p.Width, p.Height), () => { });
        }

        /// <summary>
        /// A paper surface in a wooden frame (spec 003 FR-015): a soft shadow, the frame's thickness below, the paper
        /// gradient and the brown frame line. Cards, the sheet, the board and the slot row use it.
        /// </summary>
        public static void Paper(IPainter p, Box box, float radius, float frameUnits, float depthUnits)
        {
            float depth = p.U(depthUnits);
            float line = p.U(frameUnits);
            p.FillRound(box.Offset(0f, depth * 1.8f).Inset(-p.U(4f), 0f), radius, C.GardenShadow.WithAlpha(0.2f));
            p.FillRound(box.Offset(0f, depth), radius, C.GardenWoodDepth);
            p.FillRoundGradient(box, radius, C.GardenPaperTop, C.GardenPaperBottom);
            p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.GardenWood);
        }

        /// <summary>
        /// A popup card (FR-007, spec 003 FR-015): the scrim, paper in a wooden frame, a colored header band shaped like a
        /// button with the title in volumetric letters, and a red round close button when <paramref name="onClose"/> is
        /// set. <paramref name="contentHeight"/> is in reference units. A card without a title has no band.
        /// </summary>
        public static CardRegions Card(IPainter p, float contentHeight, string title, Action? onClose, float pop = 1f, TypeStyle? titleStyle = null, ColorSet? header = null)
        {
            p.Mark("ui.card");
            Scrim(p);
            CardRegions r = ScreenLayout.Card(p.Width, p.Height, p.Insets, contentHeight);
            float radius = Math.Max(p.U(DesignTokens.Radius.CardMin), r.Card.Width * DesignTokens.Radius.Card);
            p.PushTransform(0f, 0f, pop, r.Card.CenterX, r.Card.CenterY);
            Paper(p, r.Card, radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            p.Hit(r.Card, () => { });
            if (title.Length > 0)
            {
                ColorSet set = header ?? GardenLook.Green;
                float margin = p.U(18f);
                var band = new Box(r.Card.Left + margin, r.Card.Top + margin, r.Card.Right - margin, r.Title.Bottom - p.U(4f));
                Box face = Face(p, band, set, radius - margin, 0f, lipUnits: 12f);
                float closeRoom = onClose != null ? r.Close.Width + p.U(20f) : 0f;
                float titleWidth = Math.Min(r.Title.Width, face.Width - (2f * closeRoom) - p.U(40f));
                p.Text(title, face.CenterX, face.CenterY, titleStyle ?? T.Title, C.TextOnColor, titleWidth, look: TextLook.OnColor(set));
            }

            if (onClose != null)
            {
                RoundButton(p, r.Close.CenterX, r.Close.CenterY, r.Close.Width, "ui.close", onClose, set: GardenLook.Red);
            }

            return r;
        }

        /// <summary>Ends a <see cref="Card"/> (its pop transform).</summary>
        public static void EndCard(IPainter p) => p.PopTransform();

        /// <summary>
        /// The jam bottom sheet (frame 10): a light scrim that keeps the board visible, the paper sheet in its wooden
        /// frame rising by <paramref name="rise"/> (0–1) and settling with one small bounce, its grip, title and subtitle.
        /// <paramref name="contentHeight"/> is in reference units.
        /// </summary>
        public static SheetRegions Sheet(IPainter p, float contentHeight, string title, string subtitle, float rise = 1f)
        {
            p.Mark("ui.sheet");
            // A light scrim that keeps the board visible; the top bar (Pause) stays usable above the sheet.
            p.FillRect(new Box(0f, 0f, p.Width, p.Height), C.SurfaceScrim.WithAlpha(0.3f * Math.Min(1f, rise)));
            SheetRegions r = ScreenLayout.Sheet(p.Width, p.Height, p.Insets, contentHeight);
            float drop = (1f - rise) * r.Sheet.Height;
            p.PushTransform(0f, drop, 1f, 0f, 0f);
            float radius = p.U(64f);
            Paper(p, r.Sheet, radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            p.Hit(r.Sheet, () => { });
            p.FillRound(r.Grip, r.Grip.Height / 2f, C.GardenWellEdge);
            p.Text(title, r.Title.CenterX, r.Title.CenterY, T.TitleCaps, C.GardenLabelPlain, r.Title.Width, look: TextLook.Plain(C.GardenLabelPlain));
            p.Text(subtitle, r.Subtitle.CenterX, r.Subtitle.CenterY, T.Body, C.TextSecondary, r.Subtitle.Width);
            return r;
        }

        public static void EndSheet(IPainter p) => p.PopTransform();

        /// <summary>A list row (Store, Leaderboard): an outlined rounded panel; the player's own row is raised and green.</summary>
        public static void Row(IPainter p, Box box, bool highlighted)
        {
            p.Mark("ui.row");
            float radius = box.Height * DesignTokens.Radius.Row;
            float line = p.U(DesignTokens.Garden.OutlineWidth);
            if (highlighted)
            {
                p.FillRound(box.Offset(0f, p.U(6f)), radius, C.GardenPlateDepth);
                p.FillRoundGradient(box, radius, C.SurfaceRowHighlight.Lighten(0.4f), C.SurfaceRowHighlight);
            }
            else
            {
                p.FillRound(box.Offset(0f, p.U(4f)), radius, C.GardenWell);
                p.FillRoundGradient(box, radius, Rgba.White, C.GardenPaperTop);
            }

            p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.GardenOutline);
        }

        /// <summary>A sunk well (unselected tabs, toggle tracks, empty slots): darker paper with a shadow along its top.</summary>
        public static void Well(IPainter p, Box box, float radius, Rgba fill)
        {
            float line = p.U(DesignTokens.Garden.OutlineWidth);
            float r = Math.Min(radius, box.Height / 2f);
            p.FillRound(box, r, fill);
            p.PushClip(box);
            p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + (box.Height * 0.45f)), r, C.GardenShadow.WithAlpha(0.2f), C.GardenShadow.WithAlpha(0f));
            p.PopClip();
            p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, C.GardenWellEdge);
        }

        /// <summary>Tabs in a row: the selected one is a raised green button on a plate, the others are sunk (FR-016).</summary>
        public static void Tabs(IPainter p, Box box, IReadOnlyList<string> labels, int selected, Action<int> onSelect)
        {
            p.Mark("ui.tab");
            Box[] cells = ScreenLayout.Row(box, labels.Count, p.U(16f), float.MaxValue, square: false);
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                Box cell = cells[i];
                if (i == selected)
                {
                    Box face = GardenButton(p, cell, GardenLook.Green, cell.Height / 2f, 0f);
                    p.Text(labels[i], face.CenterX, face.CenterY, T.ButtonSecondary, C.TextOnColor, face.Width * 0.86f, look: TextLook.OnColor(GardenLook.Green));
                }
                else
                {
                    float depth = Press(p, cell, true);
                    Box well = cell.Inset(p.U(6f), p.U(8f)).Offset(0f, p.U(2f) + (p.U(2f) * depth));
                    Well(p, well, well.Height / 2f, C.GardenTabSunk);
                    p.Text(labels[i], well.CenterX, well.CenterY + p.U(3f), T.ButtonSecondary, C.GardenLabelPlain, well.Width * 0.86f, look: TextLook.Plain(C.GardenLabelPlain));
                }

                p.Hit(Touch(p, cells[i]), () => onSelect(index));
            }
        }

        /// <summary>A switch (Settings): a chunky outlined track with a raised knob (FR-016); on is green with the knob right.</summary>
        public static void Toggle(IPainter p, Box box, bool on, Action action)
        {
            p.Mark("ui.toggle");
            float line = p.U(DesignTokens.Garden.OutlineWidth);
            if (on)
            {
                p.FillRound(box, box.Height / 2f, GardenLook.Green.Face);
                p.PushClip(box);
                p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + (box.Height * 0.45f)), box.Height / 2f, C.GardenShadow.WithAlpha(0.18f), C.GardenShadow.WithAlpha(0f));
                p.PopClip();
                p.StrokeRound(box.Inset(line / 2f), (box.Height / 2f) - (line / 2f), line, GardenLook.Green.Line);
            }
            else
            {
                Well(p, box, box.Height / 2f, C.GardenTabSunk);
            }

            float knob = box.Height + p.U(8f);
            float cx = on ? box.Right - (box.Height / 2f) : box.Left + (box.Height / 2f);
            Box knobBox = Box.FromCenter(cx, box.CenterY - p.U(2f), knob, knob);
            Face(p, knobBox, GardenLook.White, knob / 2f, Press(p, box, true), lipUnits: 8f);
            p.Hit(Touch(p, box), action);
        }

        /// <summary>A short message over the board (a refused tap, a hint): a paper pill with a brown outline.</summary>
        public static void Toast(IPainter p, Box area, string message)
        {
            float h = p.U(96f);
            float w = Math.Min(area.Width, p.MeasureText(message, T.Body) + p.U(80f));
            Box box = Box.FromCenter(area.CenterX, area.Bottom - (h / 2f) - p.U(16f), w, h);
            Paper(p, box, h / 2f, DesignTokens.Garden.OutlineWidth, 5f);
            p.Text(message, box.CenterX, box.CenterY, T.Body, C.GardenLabelPlain, box.Width - p.U(40f));
        }

        /// <summary>A box grown to the minimum touch size around its center (FR-027).</summary>
        public static Box Touch(IPainter p, Box box)
        {
            float min = p.U(DesignTokens.Size.TouchMin);
            return Box.FromCenter(box.CenterX, box.CenterY, Math.Max(box.Width, min), Math.Max(box.Height, min));
        }

        /// <summary>Ease-out for rising sheets and popping cards.</summary>
        public static float Ease(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return 1f - ((1f - t) * (1f - t) * (1f - t));
        }

        /// <summary>
        /// How far a sheet has risen <paramref name="seconds"/> after opening (motion.sheet, spec 003 FR-018): it slides
        /// up past its place by a little and settles back, one small bounce.
        /// </summary>
        public static float SheetRise(float seconds)
        {
            float t = Math.Max(0f, seconds / DesignTokens.Motion.Sheet.Seconds);
            if (t >= 1.35f)
            {
                return 1f;
            }

            if (t < 1f)
            {
                return Ease(t) * 1.03f;
            }

            return 1.03f - (0.03f * Ease((t - 1f) / 0.35f));
        }

        /// <summary>The pop scale of a card opened <paramref name="seconds"/> ago (motion.pop).</summary>
        public static float Pop(float seconds)
        {
            float k = Math.Max(0f, Math.Min(1f, seconds / DesignTokens.Motion.Pop.Seconds));
            float start = DesignTokens.Motion.Pop.Scale;
            return k < 0.7f ? start + ((1.04f - start) * (k / 0.7f)) : 1.04f - (0.04f * ((k - 0.7f) / 0.3f));
        }
    }
}
