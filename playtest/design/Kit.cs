using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The design board's components drawn through <see cref="IPainter"/> (spec 002 FR-005, FR-007; the playtest twin of
    /// the Unity client's <c>UiKit</c>), in the reference look of spec 005 (<c>specs/005-reference-look/contracts/look.md</c>
    /// §3). The kit covers:
    /// <list type="bullet">
    /// <item><description>glossy primary buttons in a wooden rim, cream secondary and icon buttons, jam choices;</description></item>
    /// <item><description>the speed and Petals pills, cost pills and badges;</description></item>
    /// <item><description>parchment cards, the bottom sheet, list rows, wells, tabs and toggles;</description></item>
    /// <item><description>wooden signs, candy tiles, pods, slots, booster tiles and the stone furniture (<c>KitGarden.cs</c>).</description></item>
    /// </list>
    /// Sizes are reference units scaled by <see cref="IPainter.Scale"/>, and colors are design tokens.
    /// </summary>
    public static partial class Kit
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
        /// The cream plate an element lies on (spec 003 FR-006, spec 005 §3.3): a soft shadow, its <c>parchment.edge</c>
        /// thickness below, a cream gradient and a thin <c>cream.line</c> outline. It fills <paramref name="box"/> less its
        /// thickness, and returns the plate's top face.
        /// </summary>
        public static Box Plate(IPainter p, Box box, float radius)
        {
            float h = box.Height / p.Scale;
            float depth = p.U(DesignTokens.Garden.Depth(h));
            float line = p.U(DesignTokens.Garden.Outline(h));
            var plate = new Box(box.Left, box.Top, box.Right, box.Bottom - depth);
            float r = Math.Min(radius, plate.Height / 2f);
            p.FillRound(plate.Offset(0f, depth * 2f).Inset(-p.U(2f), 0f), r, C.GardenShadow.WithAlpha(0.16f));
            p.FillRound(plate.Offset(0f, depth), r, C.ParchmentEdge.Darken(0.12f));
            p.FillRoundGradient(plate, r, C.CreamTop, C.ParchmentBottom);
            p.StrokeRound(plate.Inset(line / 2f), r - (line / 2f), line, C.CreamLine);
            return plate;
        }

        /// <summary>
        /// A raised button face in a color set (FR-007): the lip along its bottom edge, the face with its lighter top, a
        /// highlight band and an outline in the set's line. <paramref name="depth"/> is the press (1 sunk into the lip,
        /// below 0 the spring's overshoot); a pressed face also darkens by 8% (spec 005 §3.3). <paramref name="gloss"/> is
        /// the reference look's smooth gloss: a feathered band of the set's lightened top from 4% to 46% of the face that
        /// hugs its top edge, and a thin light line along the straight part of that edge. Returns the content box above
        /// the lip, moved with the press.
        /// </summary>
        public static Box Face(IPainter p, Box face, ColorSet set, float radius, float depth, bool highlight = true, float? lipUnits = null, bool gloss = false)
        {
            float h = face.Height / p.Scale;
            float lip = p.U(lipUnits ?? DesignTokens.Garden.Lip(h));
            float line = p.U(DesignTokens.Garden.Outline(h));
            float travel = Math.Max(0f, lip - p.U(3f));
            float shift = travel * Math.Max(-0.25f, Math.Min(1f, depth));
            float r = Math.Min(radius, face.Height / 2f);
            float dark = 0.08f * Math.Max(0f, Math.Min(1f, depth));

            var whole = new Box(face.Left, face.Top + Math.Max(0f, shift), face.Right, face.Bottom);
            p.FillRound(whole, r, set.Lip.Darken(dark));
            var top = new Box(face.Left, face.Top + shift, face.Right, face.Bottom - lip + shift);
            float topRadius = Math.Min(r, top.Height / 2f);
            p.FillRoundGradient(top, topRadius, set.Top.Darken(dark), set.Face.Darken(dark));
            if (highlight && gloss)
            {
                // The reference's smooth gloss: the lightened top color fading down from the top edge, feathered in three
                // steps so no edge shows, and a thin light line along the straight part of the top edge only.
                float fade = 1f - (dark * 4f);
                Rgba light = set.Top.Lighten(0.35f);
                var band = new Box(top.Left + (top.Width * 0.03f), top.Top + (top.Height * 0.04f), top.Right - (top.Width * 0.03f), top.Top + (top.Height * 0.46f));
                for (int k = 0; k < 3; k++)
                {
                    Box step = band.Inset(top.Width * 0.02f * k, top.Height * 0.02f * k);
                    p.FillRoundGradient(step, Math.Min(step.Height / 2f, topRadius), light.WithAlpha(0.35f / 3f * fade), light.WithAlpha(0f));
                }

                float shine = Math.Max(1f, line * 0.8f);
                p.PushClip(new Box(top.Left + (topRadius * 0.6f), top.Top, top.Right - (topRadius * 0.6f), top.Top + (top.Height * 0.18f)));
                p.StrokeRound(top.Inset(line * 1.6f), Math.Max(0f, topRadius - (line * 1.6f)), shine, Rgba.White.WithAlpha(0.55f * fade));
                p.PopClip();
            }
            else if (highlight)
            {
                float inset = top.Width * 0.07f;
                var band = new Box(top.Left + inset, top.Top + (top.Height * 0.07f), top.Right - inset, top.Top + (top.Height * DesignTokens.Garden.HighlightHeight));
                p.FillRoundGradient(band, Math.Min(band.Height / 2f, topRadius), Rgba.White.WithAlpha(DesignTokens.Garden.HighlightAlpha * (1f - dark * 4f)), Rgba.White.WithAlpha(0f));
            }

            var outline = new Box(face.Left, Math.Min(top.Top, whole.Top), face.Right, face.Bottom);
            p.StrokeRound(outline.Inset(line / 2f), Math.Min(r, outline.Height / 2f) - (line / 2f), line, set.Line);
            return top;
        }

        /// <summary>
        /// A garden button: the plate, then the raised face inset on it (FR-006, FR-007). Returns the face's content box.
        /// Pills pass <c>box.Height / 2</c> as the radius; round buttons a square box.
        /// </summary>
        public static Box GardenButton(IPainter p, Box box, ColorSet set, float radius, float depth, bool highlight = true, bool gloss = false)
        {
            Box plate = Plate(p, box, radius);
            float inset = p.U(DesignTokens.Garden.PlateInset(box.Height / p.Scale));
            Box face = plate.Inset(inset);
            return Face(p, face, set, Math.Max(0f, Math.Min(radius, plate.Height / 2f) - inset), depth, highlight, gloss: gloss);
        }

        /// <summary>
        /// A main button's body (spec 005 §3.3): a soft shadow, the uniform pale wood rim (<c>ui.button.rim</c>, a plank
        /// picture with only a thin deeper bottom band) filling <paramref name="box"/>, and the glossy face inset on every
        /// side. Returns the face's content box.
        /// </summary>
        public static Box RimmedButton(IPainter p, Box box, ColorSet set, float depth, bool highlight = true)
        {
            p.Mark("ui.button.rim");
            float h = box.Height;
            SoftShadow(p, box, h / 2f, 0.24f, 0.07f);
            WoodPlank(p, box, 0.5f, 3, outlineShare: 0.018f, lipShare: 0.03f);
            Box face = box.Inset(h * 0.085f);
            float lip = (face.Height / p.Scale) * 0.1f;
            return Face(p, face, set, face.Height / 2f, depth, highlight, lip, gloss: true);
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
            else
            {
                // A brown glyph on cream: a thin cream halo all around it, as on the reference's cream buttons.
                GlyphHalo(p, shapeId, box);
            }

            p.Shape(shapeId, box, glyph);
        }

        /// <summary>The thin <c>cream.top</c> halo around a brown glyph on cream (the shape grown by 0.06 shape units).</summary>
        private static void GlyphHalo(IPainter p, string shapeId, Box box)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            p.ShapeOf(shapeId + "/halo", (x, y) => sdf(x, y) - 0.06f, box, C.CreamTop);
        }

        /// <summary>
        /// A soft drop shadow under a raised element (spec 005): four <c>garden.shadow</c> layers, each grown by 1.5% of the
        /// box's shorter side and a quarter of <paramref name="alpha"/>, so its edge fades instead of reading as another
        /// lip; moved down by <paramref name="offsetShare"/> of the shorter side.
        /// </summary>
        public static void SoftShadow(IPainter p, Box box, float radius, float alpha, float offsetShare = 0.05f)
        {
            float s = Math.Min(box.Width, box.Height);
            Box shadow = box.Offset(0f, s * offsetShare);
            for (int k = 0; k < 4; k++)
            {
                float grow = s * 0.015f * k;
                p.FillRound(shadow.Inset(-grow), Math.Max(0f, radius) + grow, C.GardenShadow.WithAlpha(alpha / 4f));
            }
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
        public static void Decoration(IPainter p, Box button) => Decoration(p, GardenLook.DecorationBoxes(button));

        /// <summary>
        /// The same leaves and flower in the two boxes of <paramref name="boxes"/> (<see cref="GardenLook.DecorationBoxes"/>,
        /// or <see cref="GardenLook.PillDecorationBoxes"/> on Home's Petals pill): never a touch target.
        /// </summary>
        public static void Decoration(IPainter p, (Box TopLeft, Box BottomRight) boxes)
        {
            if (!DesignTokens.Garden.Decorations)
            {
                return;
            }

            p.Mark("ui.deco.garden");
            (Box topLeft, Box bottomRight) = boxes;
            string picture = PainterBase.DecorPrefix + OwnerPictures.ButtonLeaves;
            if (p.HasSprite(picture))
            {
                // The owner's sprig (pictures.md D7) on the top-left corner, turned half way for the bottom-right one.
                OwnerPicture(p, picture, topLeft);
                OwnerPicture(p, picture, bottomRight, mirror: true, turn: true);
                return;
            }

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

        /// <summary>
        /// A volumetric 2D block (board cells, pods, slot pods; spec 003 FR-022, FR-023): its darker lip along the bottom,
        /// the face with a lighter top, a soft highlight band and a thin translucent outline, drawn in the plane. Returns
        /// the face above the lip; <paramref name="pressed"/> sinks the face into its lip.
        /// </summary>
        public static Box Block(IPainter p, Box box, Rgba face, Rgba lip, float radius, float lipPx, float highlightAlpha, bool pressed = false, Rgba? top = null)
        {
            float line = Math.Max(1f, p.U(2f));
            float sink = pressed ? Math.Max(0f, lipPx - p.U(3f)) : 0f;
            Box whole = new Box(box.Left, box.Top + sink, box.Right, box.Bottom);
            float r = Math.Min(radius, whole.Height / 2f);
            p.FillRound(whole, r, lip);
            var faceBox = new Box(box.Left, box.Top + sink, box.Right, box.Bottom - lipPx + sink);
            p.FillRoundGradient(faceBox, Math.Min(r, faceBox.Height / 2f), top ?? face.Lighten(0.1f), face);
            if (highlightAlpha > 0f)
            {
                float inset = faceBox.Width * 0.1f;
                var band = new Box(faceBox.Left + inset, faceBox.Top + (faceBox.Height * 0.07f), faceBox.Right - inset, faceBox.Top + (faceBox.Height * 0.33f));
                p.FillRoundGradient(band, Math.Min(band.Height / 2f, r * 0.7f), Rgba.White.WithAlpha(highlightAlpha), Rgba.White.WithAlpha(0f));
            }

            p.StrokeRound(whole.Inset(line / 2f), r - (line / 2f), line, C.GardenShadow.WithAlpha(0.3f));
            return faceBox;
        }

        /// <summary>A board cell's lip: <c>garden.cell_lip</c>, but never more than 18% of a small cell.</summary>
        public static float CellLip(IPainter p, float cellHeight) => Math.Min(p.U(DesignTokens.Garden.CellLip), cellHeight * 0.18f);

        /// <summary>A pod's lip: <c>garden.pod_lip</c>, but never more than 12% of a small pod.</summary>
        public static float PodLip(IPainter p, float podHeight) => Math.Min(p.U(DesignTokens.Garden.PodLip), podHeight * 0.12f);

        // ---- Buttons ----

        /// <summary>
        /// The green primary button (Play, Next, Claim, Resume, Continue, Free rescue; spec 005 §3.3): a glossy green pill
        /// in a light wood rim with a volumetric white label outlined in dark green (FR-009, FR-012). Pressed, the face
        /// sinks into its lip and darkens. <paramref name="decorate"/> adds the leaves and flower,
        /// <paramref name="playArrow"/> the ▶ as tall as the letters, and <paramref name="breathe"/> the idle breath of
        /// the one waiting button (FR-019). <paramref name="set"/> picks another color (<see cref="GardenLook.Orange"/>).
        /// </summary>
        public static void PrimaryButton(IPainter p, Box box, string label, Action? action, TypeStyle? style = null, string? iconId = null, bool decorate = false, bool playArrow = false, bool breathe = false, ColorSet? set = null)
        {
            p.Mark("ui.button.primary");
            bool enabled = action != null;
            ColorSet colors = set ?? GardenLook.Green;
            if (!enabled)
            {
                colors = colors.Disabled();
            }

            float depth = Press(p, box, enabled);
            bool breathing = breathe && enabled && depth == 0f;
            if (breathing)
            {
                p.PushTransform(0f, 0f, GardenLook.Breathe(p.Now), box.CenterX, box.CenterY);
            }

            Squash(p, box, depth);
            Box f = RimmedButton(p, box, colors, depth, highlight: enabled);
            TypeStyle s = style ?? T.Button;
            TextLook look = TextLook.OnGloss(colors);
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
                Glyph(p, iconId, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), colors);
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

        /// <summary>
        /// The cream secondary button (Restart, Settings, Home, ×2 reward, Get +N; spec 005 §3.3): a cream face on a cream
        /// plate with a brown label and an optional brown glyph on the left (Restart's ⟳).
        /// </summary>
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
                // The glyph a little taller than the letters, as the reference's ⟳ on "Restart Level".
                float icon = f.Height * 0.7f;
                float textWidth = Math.Min(p.MeasureText(label, s), f.Width - (f.Height * 1.6f));
                float start = f.CenterX - ((icon + p.U(16f) + textWidth) / 2f);
                Glyph(p, iconId, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), GardenLook.Cream);
                p.Text(label, start + icon + p.U(16f) + (textWidth / 2f), f.CenterY, s, C.InkBrown, textWidth, look: look);
            }
            else
            {
                p.Text(label, f.CenterX, f.CenterY, s, C.InkBrown, f.Width - (f.Height * 0.7f), look: look);
            }

            p.PopTransform();
            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>
        /// A round or squircle icon button (Settings, Pause, close, back, Wardrobe, Collection; spec 005 §3.3): one domed
        /// cream cushion with a lip, an outline and a soft shadow, and a brown glyph at 46% of the size. Pause sits
        /// in a squircle (<paramref name="squircle"/>: radius 34% of the size; by default only Pause); the others are
        /// circles. Close is cream with a brown ✕ like every round button. <paramref name="set"/> paints another face (the
        /// green "+").
        /// </summary>
        public static void RoundButton(IPainter p, float cx, float cy, float size, string shapeId, Action? action, Rgba? glyph = null, ColorSet? set = null, bool? squircle = null)
        {
            p.Mark("ui.button.round");
            Box box = Box.FromCenter(cx, cy, size, size);
            ColorSet colors = set ?? GardenLook.White;
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            bool rounded = squircle ?? shapeId == "ui.pause";
            Box f = IconFace(p, box, colors, rounded ? size * 0.34f : size / 2f, depth);
            float g = size * GlyphShare(shapeId);
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

        /// <summary>
        /// The box share of a glyph on a round button, so each glyph looks about 46% of the button as in the reference:
        /// the shapes fill their boxes differently (the pause bars are slim, the gear is wide).
        /// </summary>
        private static float GlyphShare(string shapeId) => shapeId switch
        {
            "ui.pause" => 0.8f,
            "ui.close" => 0.74f,
            "ui.settings" => 0.66f,
            "ui.back" => 0.64f,
            _ => 0.6f,
        };

        /// <summary>
        /// The face of a round or squircle icon button, the speed pill and the booster tile (spec 005 §3.3): a soft shadow,
        /// the lip and the outline around a single domed cushion on cream sets (peach toward the edges, a lighter middle
        /// feathered in), or a light rim around a domed face in the set's color on colored sets. Returns the content box
        /// (9% of the shorter side inside the face), moved with the press.
        /// </summary>
        public static Box IconFace(IPainter p, Box box, ColorSet set, float radius, float depth)
        {
            float s = Math.Min(box.Width, box.Height);
            bool cream = !GardenLook.LabelOn(set).Volumetric;
            float lip = s * (cream ? 0.07f : 0.085f);
            float rim = s * 0.09f;
            float line = Math.Max(p.U(2f), s * (cream ? 0.02f : 0.024f));
            float shift = lip * 0.7f * Math.Max(-0.25f, Math.Min(1f, depth));
            float dark = 0.08f * Math.Max(0f, Math.Min(1f, depth));
            float r = Math.Min(radius, s / 2f);

            SoftShadow(p, box, r, 0.2f, 0.06f);
            var whole = new Box(box.Left, box.Top + Math.Max(0f, shift), box.Right, box.Bottom);
            p.FillRound(whole, r, set.Lip.Darken(dark));
            var top = new Box(box.Left, box.Top + shift, box.Right, box.Bottom - lip + shift);
            float topRadius = Math.Min(r, top.Height / 2f);
            Box inner = top.Inset(rim);
            float innerRadius = Math.Max(0f, topRadius - rim);
            if (cream)
            {
                // One domed cushion: the face's peach, a little deeper toward the bottom, and a lighter middle in three
                // feathered steps (no ring, no dish).
                p.FillRoundGradient(top, topRadius, set.Face.Darken(dark), set.Face.Darken(0.04f + dark));
                for (int k = 0; k < 3; k++)
                {
                    float inset = s * (0.08f + (0.04f * k));
                    Box dome = top.Inset(inset);
                    p.FillRoundGradient(dome, Math.Max(0f, topRadius - inset), set.Top.Darken(dark).WithAlpha(0.5f), set.Top.WithAlpha(0f));
                }
            }
            else
            {
                p.FillRoundGradient(top, topRadius, set.Top.Darken(dark), set.Face.Darken(dark));
                p.FillRoundGradient(inner, innerRadius, set.Top.Darken(dark), set.Face.Darken(dark));
                var band = new Box(inner.Left + (inner.Width * 0.12f), inner.Top + (inner.Height * 0.06f), inner.Right - (inner.Width * 0.12f), inner.Top + (inner.Height * 0.42f));
                p.FillRoundGradient(band, Math.Min(band.Height / 2f, innerRadius), Rgba.White.WithAlpha(0.4f), Rgba.White.WithAlpha(0f));
            }

            var outline = new Box(box.Left, Math.Min(top.Top, whole.Top), box.Right, box.Bottom);
            p.StrokeRound(outline.Inset(line / 2f), Math.Min(r, outline.Height / 2f) - (line / 2f), line, set.Line);
            return inner;
        }

        /// <summary>
        /// The speed pill of the gameplay top bar (spec 005 §3.3): the cream squircle style, wider, with the speed
        /// (<paramref name="label"/>, "1×" or "2×") in <c>ink.brown</c> and the <c>ui.fast</c> chevrons (▶▶) after it.
        /// </summary>
        public static void SpeedPill(IPainter p, Box box, string label, Action? action)
        {
            p.Mark("ui.pill.speed");
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            Box f = IconFace(p, box, GardenLook.White, box.Height * 0.34f, depth);
            TypeStyle s = T.LevelPill;
            float scale = box.Height * 0.5f / p.U(s.Size);

            // The ▶▶ box: its marks are 80% of it tall, so they stand as tall as the digits (about 42% of the pill).
            float glyph = box.Height * 0.52f;
            float gap = box.Height * 0.04f;
            float textWidth = Math.Min(p.MeasureText(label, s, scale), f.Width - glyph - gap - (box.Height * 0.2f));
            float start = f.CenterX - ((textWidth + gap + glyph) / 2f);
            p.Text(label, start + (textWidth / 2f), f.CenterY, s, C.InkBrown, textWidth, scale, TextLook.Plain(C.InkBrown));
            Box fast = Box.FromCenter(start + textWidth + gap + (glyph / 2f), f.CenterY, glyph, glyph);
            GlyphHalo(p, GardenLook.FastGlyph.ShapeId, fast);
            p.Shape(GardenLook.FastGlyph.ShapeId, fast, GardenLook.FastGlyph.Fill);
            p.PopTransform();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The 2× control of the gameplay top bar: since spec 005 the cream <see cref="SpeedPill"/>.</summary>
        public static void DarkPill(IPainter p, Box box, string label, Action? action) => SpeedPill(p, box, label, action);

        /// <summary>
        /// The gameplay level label: since spec 005 a wooden sign with ivy at both ends (§3.2, research D6), its letters in
        /// <c>badge.super_hard</c> darkened 0.35 on a Super Hard level, so they keep their contrast on the pale wood.
        /// </summary>
        public static void LevelPill(IPainter p, Box box, string text, bool superHard)
        {
            p.Mark("ui.pill.level");
            WoodSign(p, box, text, T.LevelPill, SignDecor.Ivy, superHard ? C.BadgeSuperHard.Darken(0.35f) : (Rgba?)null);
        }

        /// <summary>The HARD or SUPER HARD badge under the level label (frames 8 and 9): a sticker pill on a plate (FR-014).</summary>
        public static void Badge(IPainter p, Box box, string text, Rgba color, string slotId)
        {
            p.Mark(slotId);
            ColorSet set = ColorSet.From(slotId, color);
            Box f = GardenButton(p, box, set, box.Height / 2f, 0f);
            p.Text(text, f.CenterX, f.CenterY, T.Badge, C.TextOnColor, f.Width * 0.86f, look: TextLook.OnColor(set));
        }

        /// <summary>
        /// A count badge (booster charges, "+N" on a stack; spec 005 §3.4): white digits on a <c>badge.green</c> disc with a
        /// white ring (10% of its size) and a thin dark outline.
        /// </summary>
        public static void CountBadge(IPainter p, float cx, float cy, float size, string text)
        {
            p.Mark("ui.badge.count");
            float scale = size / p.U(48f);
            float width = Math.Max(size, p.MeasureText(text, T.Badge, scale) + (size * 0.55f));
            Box box = Box.FromCenter(cx, cy, width, size);
            float ring = size * 0.1f;
            float line = Math.Max(1f, size * 0.03f);
            Box outer = box.Inset(-(ring + line));
            float r = outer.Height / 2f;
            SoftShadow(p, outer, r, 0.25f, 0.07f);
            p.FillRound(outer, r, C.GardenShadow.WithAlpha(0.8f));
            p.FillRound(box.Inset(-ring), (size / 2f) + ring, Rgba.White);
            p.FillRoundGradient(box, size / 2f, C.BadgeGreen.Lighten(0.14f), C.BadgeGreen);
            p.Text(text, cx, cy + (size * 0.02f), T.Badge, C.TextOnColor, width * 0.9f, sizeScale: scale);
        }

        /// <summary>
        /// An icon badge (the shirt Home's Wardrobe avatar wore from the owner's note of 2026-10-04 until the bottom menu,
        /// spec 005 FR-030, took the avatar's place; no screen shows it now): a white glyph on the count
        /// badge's green disc with its white ring and thin dark outline (<see cref="CountBadge"/>), <paramref name="size"/>
        /// the disc's diameter.
        /// </summary>
        public static void IconBadge(IPainter p, float cx, float cy, float size, string shapeId)
        {
            p.Mark("ui.badge.count");
            Box box = Box.FromCenter(cx, cy, size, size);
            float ring = size * 0.1f;
            float line = Math.Max(1f, size * 0.03f);
            Box outer = box.Inset(-(ring + line));
            float r = outer.Height / 2f;
            SoftShadow(p, outer, r, 0.25f, 0.07f);
            p.FillRound(outer, r, C.GardenShadow.WithAlpha(0.8f));
            p.FillRound(box.Inset(-ring), (size / 2f) + ring, Rgba.White);
            p.FillRoundGradient(box, size / 2f, C.BadgeGreen.Lighten(0.14f), C.BadgeGreen);
            p.Shape(shapeId, box.Inset(size * 0.2f), Rgba.White);
        }

        /// <summary>A price tag (a booster without charges): since spec 005 a <see cref="CostPill"/> with the lotus.</summary>
        public static void PriceTag(IPainter p, Box box, int price) => CostPill(p, box, Cost.Petals(price));

        /// <summary>
        /// A cost pill (spec 005 §3.4; jam choices, booster tiles, the Store): a cream pill with a <c>cream.line</c> outline
        /// and a soft shadow holding the lotus and a brown price, a green ▶ square and "Free", or "×N" charges in big
        /// digits after <paramref name="chargeIcon"/> (the booster's icon, at 80% of the pill's height) when given.
        /// <paramref name="text"/> replaces the amount's text next to the same icon (the win's reward pill: the lotus and
        /// "+N" counting up).
        /// </summary>
        public static void CostPill(IPainter p, Box box, Cost cost, string? text = null, IReadOnlyList<IconPart>? chargeIcon = null)
        {
            p.Mark("ui.pill.cost");
            float h = box.Height;
            float r = h / 2f;
            float line = Math.Max(p.U(2f), h * 0.05f);
            SoftShadow(p, box, r, 0.2f, 0.12f);
            p.FillRound(box.Offset(0f, h * 0.07f), r, C.CreamLip);
            p.FillRoundGradient(box, r, C.CreamTop, C.ParchmentBottom);
            p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, C.CreamLine);

            // Charges ("×2") get bigger digits, as large as the reference's prices look next to their icons.
            bool charges = cost.Kind == CostKind.Charges && text == null;
            text ??= cost.Kind switch
            {
                CostKind.Petals => NumberText.Group(cost.Amount),
                CostKind.Free => PlaytestText.T("common.free"),
                _ => PlaytestText.F("common.charges", cost.Amount),
            };
            TypeStyle s = T.Count;
            float scale = h * (charges ? 0.66f : 0.56f) / p.U(s.Size);
            bool withCharge = charges && chargeIcon != null;
            float icon = cost.Kind == CostKind.Petals ? h * 0.86f : cost.Kind == CostKind.Free ? h * 0.6f : withCharge ? h * 0.8f : 0f;
            float gap = icon > 0f ? h * 0.16f : 0f;
            float textWidth = Math.Min(p.MeasureText(text, s, scale), box.Width - icon - gap - (h * 0.5f));
            float start = box.CenterX - ((icon + gap + textWidth) / 2f);
            Box iconBox = Box.FromCenter(start + (icon / 2f), box.CenterY, icon, icon);
            if (cost.Kind == CostKind.Petals)
            {
                Petal(p, iconBox);
            }
            else if (cost.Kind == CostKind.Free)
            {
                // The rewarded choice: a white ▶ on a small green square.
                p.Mark("ui.play");
                ColorSet green = GardenLook.Green;
                p.FillRound(iconBox.Offset(0f, icon * 0.08f), icon * 0.26f, green.Lip);
                p.FillRoundGradient(iconBox, icon * 0.26f, green.Top, green.Face);
                p.StrokeRound(iconBox.Inset(line / 4f), icon * 0.26f, Math.Max(1f, line / 2f), green.Line);
                p.Shape("ui.play", iconBox.Inset(icon * 0.2f).Offset(icon * 0.03f, 0f), Rgba.White);
            }
            else if (withCharge && chargeIcon != null)
            {
                IconParts(p, iconBox, chargeIcon);
            }

            p.Text(text, start + icon + gap + (textWidth / 2f), box.CenterY, s, C.InkBrown, textWidth, scale, TextLook.Plain(C.InkBrown));
        }

        /// <summary>
        /// The Petals symbol: the owner's lotus picture (<see cref="OwnerPictures.CurrencyLotus"/>) when it is embedded, else
        /// the drawn pink lotus with its outline and light tips (FR-006; spec 005 contracts/look.md §3.4).
        /// </summary>
        public static void Petal(IPainter p, Box box)
        {
            if (OwnerPicture(p, LotusPicture, box))
            {
                p.Mark("currency.petal");
                return;
            }

            IconParts(p, box, GardenLook.Lotus);
        }

        private const string LotusPicture = PainterBase.IconPrefix + OwnerPictures.CurrencyLotus;

        /// <summary>
        /// A multi-part icon (spec 005 contracts/look.md §3.4, §3.8: the lotus, the colored booster icons): each part's
        /// outline (the shape grown by its <see cref="IconPart.Grow"/>), then its fill, back to front. A
        /// <paramref name="grey"/> icon (a disabled booster) draws every part in grey. A booster's icon
        /// (<see cref="GardenLook.BoosterOf"/>) is the owner's picture instead when it is embedded (pictures.md D1–D4),
        /// faded when grey.
        /// </summary>
        public static void IconParts(IPainter p, Box box, IReadOnlyList<IconPart> parts, bool grey = false)
        {
            string? booster = GardenLook.BoosterOf(parts);
            string picture = booster == null ? string.Empty : PainterBase.IconPrefix + OwnerPictures.BoosterIcon(booster);
            if (booster != null && p.HasSprite(picture))
            {
                p.PushAlpha(grey ? GardenLook.PictureDisabledAlpha : 1f);
                p.Sprite(picture, box);
                p.PopAlpha();
                return;
            }

            foreach (IconPart part in parts)
            {
                if (part.Line.HasValue && part.Grow > 0f)
                {
                    Func<float, float, float> sdf = ShapeLibrary.Get(part.ShapeId);
                    float grow = part.Grow;
                    Rgba line = grey ? part.Line.Value.Grey() : part.Line.Value;
                    p.ShapeOf(part.ShapeId + "/line/" + grow.ToString("0.###", CultureInfo.InvariantCulture), (x, y) => sdf(x, y) - grow, box, line);
                }

                p.Shape(part.ShapeId, box, grey ? part.Fill.Grey() : part.Fill);
            }
        }

        /// <summary>
        /// The Petals balance pill (frames 2, 3 and 17; spec 005 §3.4; <see cref="PetalsPillParts"/>): a cream raised pill
        /// that fits its content inside <paramref name="box"/> (placed by <paramref name="align"/>: 1 keeps its right end on
        /// the box's, 0.5 centers it), the lotus inside its left end, the balance in <c>ink.brown</c> left-aligned right
        /// after the lotus, so a short amount never floats in the middle, and the round green "+" over its right end
        /// (FR-013). The pill takes the tap when <paramref name="onPlus"/> is set (Unity's <c>UiKit.PetalsPill</c>).
        /// <paramref name="decorate"/> adds the main buttons' leaves and flower over the corners of the pill and its "+"
        /// (<see cref="GardenLook.PillDecorationBoxes"/> of <see cref="PetalsPillParts.Span"/>), scaled to its height and
        /// never a touch target: Home's header pill (the owner's request of 2026-10-04). Returns the parts as drawn.
        /// </summary>
        public static PetalsPillParts PetalsPill(IPainter p, Box box, long petals, Action? onPlus, float align = 1f, bool decorate = false)
        {
            p.Mark("ui.pill.petals");
            TypeStyle s = T.Count;
            float scale = box.Height * PetalsPillParts.AmountShare / p.U(s.Size);
            string amount = NumberText.Group(petals);
            float measured = Math.Max(p.MeasureText(PetalsPillParts.WidthText(amount), s, scale), p.MeasureText(amount, s, scale));
            PetalsPillParts parts = PetalsPillParts.Fit(box, measured, onPlus != null, align);
            Box pill = parts.Pill;
            float h = pill.Height;
            float r = h / 2f;
            float line = Math.Max(p.U(2f), h * 0.04f);
            SoftShadow(p, pill, r, 0.2f, 0.08f);
            p.FillRound(pill, r, C.CreamLip);
            p.FillRoundGradient(parts.Face, Math.Min(r, parts.Face.Height / 2f), C.CreamTop, C.CreamFace);
            p.StrokeRound(pill.Inset(line / 2f), r - (line / 2f), line, C.CreamLine);
            Petal(p, parts.Lotus);
            p.TextLeft(amount, parts.Amount.Left, parts.Amount.CenterY, s, C.InkBrown, parts.Amount.Width + 1f, scale, TextLook.Plain(C.InkBrown));
            if (onPlus != null)
            {
                Box plus = parts.Plus;
                float depth = Press(p, Touch(p, pill), true);
                Squash(p, plus, depth);
                Box f = IconFace(p, plus, GardenLook.Green, plus.Width / 2f, depth);
                float g = plus.Width * 0.6f;
                Glyph(p, "ui.plus", Box.FromCenter(f.CenterX, f.CenterY, g, g), GardenLook.Green);
                p.PopTransform();
                p.Hit(Touch(p, pill), onPlus);
            }

            if (decorate)
            {
                Decoration(p, GardenLook.PillDecorationBoxes(parts.Span));
            }

            return parts;
        }

        /// <summary>The dimmed backdrop behind popups; a tap on it does nothing (the card decides).</summary>
        public static void Scrim(IPainter p, float alpha = 1f)
        {
            p.FillRect(new Box(0f, 0f, p.Width, p.Height), C.SurfaceScrim.WithAlpha(C.SurfaceScrim.A / 255f * alpha));
            p.Hit(new Box(0f, 0f, p.Width, p.Height), () => { });
        }

        /// <summary>
        /// A parchment surface (spec 005 §3.5; cards, the sheet, the tray panel, the slot band, toasts): a soft shadow
        /// <paramref name="depthUnits"/> below, the <c>parchment.top</c> to <c>parchment.bottom</c> gradient with a warm,
        /// darker aged band over its outer 6% (fading inward), a thin <c>parchment.edge</c> line inside it, and a
        /// thinner <c>parchment.line</c> outline (0.8 × <paramref name="frameUnits"/>). No wooden frame.
        /// </summary>
        public static void Paper(IPainter p, Box box, float radius, float frameUnits, float depthUnits)
        {
            p.Mark("mat.parchment");
            float depth = p.U(depthUnits);
            float line = p.U(frameUnits) * 0.8f;
            float r = Math.Min(radius, box.Height / 2f);
            p.FillRound(box.Offset(0f, depth * 1.6f).Inset(-p.U(3f), 0f), r, C.GardenShadow.WithAlpha(0.18f));
            p.FillRound(box.Offset(0f, depth * 0.7f), r, C.GardenShadow.WithAlpha(0.14f));
            p.FillRoundGradient(box, r, C.ParchmentTop, C.ParchmentBottom);

            // The aged edge over the outer 6% of the shorter side: twelve overlapping thin steps fading inward (about 0.4 at
            // the outline), fine enough that no ring shows.
            float step = Math.Max(1f, Math.Min(box.Width, box.Height) * 0.005f);
            for (int k = 0; k < 12; k++)
            {
                float inset = line + (step * k);
                p.StrokeRound(box.Inset(inset), Math.Max(0f, r - inset), step * 1.6f, C.ParchmentEdge.WithAlpha(0.26f * (1f - (k / 12f))));
            }

            float inner = line + Math.Max(p.U(3f), Math.Min(box.Width, box.Height) * 0.012f);
            p.StrokeRound(box.Inset(inner), Math.Max(0f, r - inner), Math.Max(1f, line * 0.4f), C.ParchmentEdge);
            p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, C.ParchmentLine);
        }

        /// <summary>
        /// A popup card (FR-007, spec 005 §3.5): the scrim, parchment, the title in <c>type.title</c> <c>ink.title</c> or,
        /// when <paramref name="sign"/> is set, a wooden sign across the card's top edge with that decoration (the win,
        /// the Store), and the cream round close button over the top-right corner when <paramref name="onClose"/> is set.
        /// <paramref name="contentHeight"/> is in reference units.
        /// </summary>
        public static CardRegions Card(IPainter p, float contentHeight, string title, Action? onClose, float pop = 1f, TypeStyle? titleStyle = null, SignDecor? sign = null)
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
                TypeStyle style = titleStyle ?? T.Title;
                if (sign.HasValue)
                {
                    float h = p.U(118f);
                    float width = Math.Min(r.Card.Width * 0.78f, p.MeasureText(title, style) + (h * 1.4f));
                    WoodSign(p, Box.FromCenter(r.Card.CenterX, r.Title.CenterY - p.U(30f), width, h), title, style, sign.Value);
                }
                else
                {
                    float closeRoom = onClose != null ? r.Close.Width + p.U(20f) : 0f;
                    float titleWidth = Math.Min(r.Title.Width, r.Card.Width - (2f * closeRoom) - p.U(60f));
                    p.Text(title, r.Title.CenterX, r.Title.CenterY, style, C.InkTitle, titleWidth, look: TextLook.Plain(C.InkTitle));
                }
            }

            if (onClose != null)
            {
                // Over the top-right corner, as in the reference.
                float shift = r.Close.Width * 0.3f;
                RoundButton(p, r.Close.CenterX + shift, r.Close.CenterY - shift, r.Close.Width, "ui.close", onClose);
            }

            return r;
        }

        /// <summary>Ends a <see cref="Card"/> (its pop transform).</summary>
        public static void EndCard(IPainter p) => p.PopTransform();

        /// <summary>
        /// The jam bottom sheet (frame 10, spec 005 §4.3): a light scrim that keeps the board visible, the parchment sheet
        /// rising by <paramref name="rise"/> (0–1) and settling with one small bounce, its grip, the title in
        /// <c>ink.title</c> and the subtitle in <c>ink.brown_soft</c>. <paramref name="contentHeight"/> is in reference units.
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
            p.FillRound(r.Grip, r.Grip.Height / 2f, C.ParchmentEdge.Darken(0.12f));
            p.Text(title, r.Title.CenterX, r.Title.CenterY, T.Title, C.InkTitle, r.Title.Width, look: TextLook.Plain(C.InkTitle));
            p.Text(subtitle, r.Subtitle.CenterX, r.Subtitle.CenterY, T.Body, C.InkBrownSoft, r.Subtitle.Width);
            return r;
        }

        public static void EndSheet(IPainter p) => p.PopTransform();

        /// <summary>
        /// A list row (Store, Leaderboard; spec 005 §3.5): a cream rounded panel with a <c>cream.line</c> outline and a
        /// lip; the player's own row is raised and green-tinted.
        /// </summary>
        public static void Row(IPainter p, Box box, bool highlighted)
        {
            p.Mark("ui.row");
            float radius = box.Height * DesignTokens.Radius.Row;
            float line = p.U(DesignTokens.Garden.OutlineWidth);
            if (highlighted)
            {
                p.FillRound(box.Offset(0f, p.U(6f)), radius, GardenLook.Green.Lip.Mix(C.CreamLip, 0.4f));
                p.FillRoundGradient(box, radius, C.SurfaceRowHighlight.Lighten(0.4f), C.SurfaceRowHighlight);
                p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, GardenLook.Green.Lip);
                return;
            }

            p.FillRound(box.Offset(0f, p.U(4f)), radius, C.CreamLip);
            p.FillRoundGradient(box, radius, C.CreamTop, C.CreamFace);
            p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine);
        }

        /// <summary>
        /// A sunk well (spec 005 §3.5: the jam's slot row, unselected tabs, toggle tracks): <c>parchment.well</c> (or
        /// <paramref name="fill"/>) with a shadow along its top and a <c>parchment.edge</c> outline.
        /// </summary>
        public static void Well(IPainter p, Box box, float radius, Rgba? fill = null)
        {
            float line = p.U(DesignTokens.Garden.OutlineWidth);
            float r = Math.Min(radius, box.Height / 2f);
            p.FillRound(box, r, fill ?? C.ParchmentWell);
            p.PushClip(box);
            p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + Math.Min(box.Height * 0.45f, p.U(40f))), r, C.GardenShadow.WithAlpha(0.16f), C.GardenShadow.WithAlpha(0f));
            p.PopClip();
            p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, C.ParchmentEdge.Darken(0.08f));
        }

        /// <summary>
        /// Tabs in a row (FR-016, spec 005): the selected one is a glossy green button on a cream plate with a white label,
        /// the others are parchment wells with brown labels.
        /// </summary>
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
                    Box face = GardenButton(p, cell, GardenLook.Green, cell.Height / 2f, 0f, gloss: true);
                    p.Text(labels[i], face.CenterX, face.CenterY, T.ButtonSecondary, C.TextOnColor, face.Width * 0.86f, look: TextLook.OnColor(GardenLook.Green));
                }
                else
                {
                    float depth = Press(p, cell, true);
                    Box well = cell.Inset(p.U(6f), p.U(8f)).Offset(0f, p.U(2f) + (p.U(2f) * depth));
                    Well(p, well, well.Height / 2f);
                    p.Text(labels[i], well.CenterX, well.CenterY + p.U(3f), T.ButtonSecondary, C.InkBrown, well.Width * 0.86f, look: TextLook.Plain(C.InkBrown));
                }

                p.Hit(Touch(p, cells[i]), () => onSelect(index));
            }
        }

        /// <summary>
        /// A switch (Settings; spec 005 §3.3, §3.5): a track pressed into the parchment and a domed cream knob like the round
        /// buttons. On, the track is the green set's glossy face with a white check where the knob was (so the state never
        /// rests on the hue alone) and the knob is right; off, it is a parchment well with the knob left.
        /// </summary>
        public static void Toggle(IPainter p, Box box, bool on, Action action)
        {
            p.Mark("ui.toggle");
            float r = box.Height / 2f;
            float line = Math.Max(p.U(2f), box.Height * 0.04f);
            if (on)
            {
                ColorSet green = GardenLook.Green;
                p.FillRoundGradient(box, r, green.Lip, green.Face);
                p.PushClip(box);
                p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + (box.Height * 0.5f)), r, C.GardenShadow.WithAlpha(0.22f), C.GardenShadow.WithAlpha(0f));
                p.PopClip();
                var shine = new Box(box.Left + (box.Height * 0.3f), box.Bottom - (box.Height * 0.34f), box.Right - (box.Height * 0.3f), box.Bottom - (box.Height * 0.12f));
                p.FillRoundGradient(shine, shine.Height / 2f, green.Top.WithAlpha(0f), green.Top.WithAlpha(0.55f));
                p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, green.Line);
                float check = box.Height * 0.5f;
                p.Shape("ui.check", Box.FromCenter(box.Left + r + (box.Height * 0.06f), box.CenterY, check, check), Rgba.White);
            }
            else
            {
                Well(p, box, r);
            }

            float knob = box.Height + p.U(10f);
            float cx = on ? box.Right - r : box.Left + r;
            Box knobBox = Box.FromCenter(cx, box.CenterY - p.U(2f), knob, knob);
            IconFace(p, knobBox, GardenLook.White, knob / 2f, Press(p, box, true));
            p.Hit(Touch(p, box), action);
        }

        /// <summary>A short message over the board (a refused tap, a hint): a parchment pill with brown text.</summary>
        public static void Toast(IPainter p, Box area, string message)
        {
            float h = p.U(96f);
            float w = Math.Min(area.Width, p.MeasureText(message, T.Body) + p.U(80f));
            Box box = Box.FromCenter(area.CenterX, area.Bottom - (h / 2f) - p.U(16f), w, h);
            Paper(p, box, h / 2f, DesignTokens.Garden.OutlineWidth, 5f);
            p.Text(message, box.CenterX, box.CenterY, T.Body, C.InkBrown, box.Width - p.U(40f));
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
