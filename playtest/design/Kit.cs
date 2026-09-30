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

        /// <summary>The green primary button (PLAY, NEXT, CLAIM, RESUME, CONTINUE, Free rescue).</summary>
        public static void PrimaryButton(IPainter p, Box box, string label, Action? action, TypeStyle? style = null, string? iconId = null)
        {
            p.Mark("ui.button.primary");
            bool enabled = action != null;
            bool pressed = enabled && p.Pressed(box);
            Rgba face = enabled ? C.ButtonPrimary : C.ButtonPrimary.Grey().Lighten(0.2f);
            Rgba top = enabled ? C.ButtonPrimaryTop : face.Lighten(0.15f);
            Box f = Raised(p, box, face, enabled ? C.ButtonPrimaryEdge : face.Darken(0.2f), box.Height / 2f, pressed, top);
            TypeStyle s = style ?? T.Button;
            float textLeft = f.Left + (f.Height * 0.4f);
            if (iconId != null)
            {
                float icon = f.Height * 0.46f;
                float textWidth = Math.Min(p.MeasureText(label, s), f.Width - (f.Height * 1.4f));
                float start = f.CenterX - ((icon + p.U(14f) + textWidth) / 2f);
                p.Shape(iconId, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), C.TextOnColor);
                p.Text(label, start + icon + p.U(14f) + (textWidth / 2f), f.CenterY, s, C.TextOnColor, textWidth);
            }
            else
            {
                p.Text(label, f.CenterX, f.CenterY, s, C.TextOnColor, f.Right - textLeft - (f.Height * 0.4f));
            }

            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The cream secondary button (RESTART, SETTINGS, HOME, Restart, ×2 reward, Get +N).</summary>
        public static void SecondaryButton(IPainter p, Box box, string label, Action? action, string? iconId = null, TypeStyle? style = null)
        {
            p.Mark("ui.button.secondary");
            bool enabled = action != null;
            bool pressed = enabled && p.Pressed(box);
            p.PushAlpha(enabled ? 1f : 0.55f);
            Box f = Raised(p, box, C.ButtonSecondary, C.ButtonSecondaryEdge, box.Height / 2f, pressed);
            TypeStyle s = style ?? T.ButtonSecondary;
            if (iconId != null)
            {
                float icon = f.Height * 0.42f;
                float textWidth = Math.Min(p.MeasureText(label, s), f.Width - (f.Height * 1.4f));
                float start = f.CenterX - ((icon + p.U(14f) + textWidth) / 2f);
                p.Shape(iconId, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), C.TextPrimary);
                p.Text(label, start + icon + p.U(14f) + (textWidth / 2f), f.CenterY, s, C.TextPrimary, textWidth);
            }
            else
            {
                p.Text(label, f.CenterX, f.CenterY, s, C.TextPrimary, f.Width - (f.Height * 0.6f));
            }

            p.PopAlpha();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>A white round icon button (Settings, Pause, close, Wardrobe, Collection).</summary>
        public static void RoundButton(IPainter p, float cx, float cy, float size, string shapeId, Action? action, Rgba? glyph = null)
        {
            p.Mark("ui.button.round");
            Box box = Box.FromCenter(cx, cy, size, size);
            bool pressed = action != null && p.Pressed(box);
            float lift = p.U(6f);
            if (!pressed)
            {
                p.FillCircle(cx, cy + (lift / 2f), size / 2f, C.ButtonIconEdge);
            }

            float faceY = pressed ? cy + (lift / 2f) : cy - (lift / 2f);
            p.FillCircle(cx, faceY, (size / 2f) - p.U(2f), C.ButtonIcon);
            float g = size * 0.5f;
            p.Shape(shapeId, Box.FromCenter(cx, faceY, g, g), glyph ?? C.ButtonIconGlyph);
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The dark 2× pill of the gameplay top bar.</summary>
        public static void DarkPill(IPainter p, Box box, string label, Action? action)
        {
            p.Mark("ui.pill.speed");
            bool pressed = action != null && p.Pressed(box);
            Box f = Raised(p, box, C.ButtonDark, C.ButtonDark.Darken(0.35f), box.Height / 2f, pressed);
            p.Text(label, f.CenterX, f.CenterY, T.LevelPill, C.TextOnColor, f.Width * 0.8f);
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>The "LEVEL N" pill (sky blue, lilac on Super Hard).</summary>
        public static void LevelPill(IPainter p, Box box, string text, bool superHard)
        {
            p.Mark("ui.pill.level");
            Rgba face = superHard ? C.PillLevelSuperHard : C.PillLevel;
            Rgba edge = superHard ? C.PillLevelSuperHard.Darken(0.25f) : C.PillLevelEdge;
            Box f = Raised(p, box, face, edge, box.Height / 2f, top: face.Lighten(0.2f));
            p.Text(text, f.CenterX, f.CenterY, T.LevelPill, C.TextOnColor, f.Width * 0.86f);
        }

        /// <summary>The HARD or SUPER HARD badge under the level pill (frames 8 and 9).</summary>
        public static void Badge(IPainter p, Box box, string text, Rgba color, string slotId)
        {
            p.Mark(slotId);
            p.FillRound(box.Offset(0f, p.U(4f)), box.Height / 2f, color.Darken(0.3f));
            p.FillRound(box, box.Height / 2f, color);
            p.Text(text, box.CenterX, box.CenterY, T.Badge, C.TextOnColor, box.Width * 0.86f);
        }

        /// <summary>A dark count badge (booster charges).</summary>
        public static void CountBadge(IPainter p, float cx, float cy, float size, string text)
        {
            p.Mark("ui.badge.count");
            float width = Math.Max(size, p.MeasureText(text, T.Badge) + (size * 0.5f));
            Box box = Box.FromCenter(cx, cy, width, size);
            p.FillRound(box.Inset(-p.U(3f)), (size / 2f) + p.U(3f), C.TextOnColor);
            p.FillRound(box, size / 2f, C.BadgeCount);
            p.Text(text, cx, cy, T.Badge, C.TextOnColor, width * 0.9f, sizeScale: size / p.U(48f));
        }

        /// <summary>The Petal symbol: pink petals around a yellow center (FR-006).</summary>
        public static void Petal(IPainter p, Box box)
        {
            p.Shape("currency.petal", box, C.PetalFill);
            p.FillCircle(box.CenterX, box.CenterY, box.Width * 0.2f, C.PetalCenter);
        }

        /// <summary>The Petals balance pill with its green "+" (frames 2, 3 and 17).</summary>
        public static void PetalsPill(IPainter p, Box box, long petals, Action? onPlus)
        {
            p.Mark("ui.pill.petals");
            p.FillRound(box.Offset(0f, p.U(4f)), box.Height / 2f, C.PillPetalsEdge);
            p.FillRound(box, box.Height / 2f, C.PillPetals.Over(C.SurfacePanel));
            float icon = box.Height * 0.9f;
            Petal(p, Box.FromCenter(box.Left + (box.Height * 0.5f), box.CenterY, icon, icon));
            float right = onPlus != null ? box.Right - box.Height : box.Right - (box.Height * 0.3f);
            p.Text(NumberText.Group(petals), (box.Left + box.Height + right) / 2f, box.CenterY, T.Count, C.TextPrimary, right - box.Left - box.Height);
            if (onPlus != null)
            {
                float r = box.Height * 0.38f;
                float cx = box.Right - (box.Height * 0.5f);
                p.FillCircle(cx, box.CenterY, r, C.AccentPlus);
                p.Shape("ui.plus", Box.FromCenter(cx, box.CenterY, r * 1.2f, r * 1.2f), C.TextOnColor);
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
        /// A popup card (FR-007): the scrim, the cream card with a soft shadow, its title and the round close button when
        /// <paramref name="onClose"/> is set. <paramref name="contentHeight"/> is in reference units.
        /// </summary>
        public static CardRegions Card(IPainter p, float contentHeight, string title, Action? onClose, float pop = 1f, TypeStyle? titleStyle = null)
        {
            p.Mark("ui.card");
            Scrim(p);
            CardRegions r = ScreenLayout.Card(p.Width, p.Height, p.Insets, contentHeight);
            float radius = Math.Max(p.U(DesignTokens.Radius.CardMin), r.Card.Width * DesignTokens.Radius.Card);
            p.PushTransform(0f, 0f, pop, r.Card.CenterX, r.Card.CenterY);
            p.FillRound(r.Card.Offset(0f, p.U(DesignTokens.Elevation.CardShadowOffset)), radius, Rgba.Black.WithAlpha(DesignTokens.Elevation.CardShadowAlpha));
            p.FillRound(r.Card, radius, C.SurfacePanel);
            p.StrokeRound(r.Card, radius, p.U(3f), C.SurfacePanelEdge);
            p.Hit(r.Card, () => { });
            p.Text(title, r.Title.CenterX, r.Title.CenterY, titleStyle ?? T.Title, C.TextPrimary, r.Title.Width);
            if (onClose != null)
            {
                RoundButton(p, r.Close.CenterX, r.Close.CenterY, r.Close.Width, "ui.close", onClose);
            }

            return r;
        }

        /// <summary>Ends a <see cref="Card"/> (its pop transform).</summary>
        public static void EndCard(IPainter p) => p.PopTransform();

        /// <summary>
        /// The jam bottom sheet (frame 10): a light scrim that keeps the board visible, the cream sheet rising by
        /// <paramref name="rise"/> (0–1), its grip, title and subtitle. <paramref name="contentHeight"/> is in reference units.
        /// </summary>
        public static SheetRegions Sheet(IPainter p, float contentHeight, string title, string subtitle, float rise = 1f)
        {
            p.Mark("ui.sheet");
            // A light scrim that keeps the board visible; the top bar (Pause) stays usable above the sheet.
            p.FillRect(new Box(0f, 0f, p.Width, p.Height), C.SurfaceScrim.WithAlpha(0.3f * rise));
            SheetRegions r = ScreenLayout.Sheet(p.Width, p.Height, p.Insets, contentHeight);
            float drop = (1f - Ease(rise)) * r.Sheet.Height;
            p.PushTransform(0f, drop, 1f, 0f, 0f);
            float radius = p.U(64f);
            p.FillRound(r.Sheet.Offset(0f, -p.U(6f)), radius, Rgba.Black.WithAlpha(0.12f));
            p.FillRound(r.Sheet, radius, C.SurfacePanel);
            p.Hit(r.Sheet, () => { });
            p.FillRound(r.Grip, r.Grip.Height / 2f, C.SurfacePanelEdge);
            p.Text(title, r.Title.CenterX, r.Title.CenterY, T.TitleCaps, C.TextPrimary, r.Title.Width);
            p.Text(subtitle, r.Subtitle.CenterX, r.Subtitle.CenterY, T.Body, C.TextSecondary, r.Subtitle.Width);
            return r;
        }

        public static void EndSheet(IPainter p) => p.PopTransform();

        /// <summary>A list row (Store, Leaderboard): cream, or light green for the player's own row.</summary>
        public static void Row(IPainter p, Box box, bool highlighted)
        {
            p.Mark("ui.row");
            float radius = box.Height * DesignTokens.Radius.Row;
            p.FillRound(box.Offset(0f, p.U(4f)), radius, C.SurfacePanelEdge);
            p.FillRound(box, radius, highlighted ? C.SurfaceRowHighlight : Rgba.White);
        }

        /// <summary>Tabs in a row; the selected one is green.</summary>
        public static void Tabs(IPainter p, Box box, IReadOnlyList<string> labels, int selected, Action<int> onSelect)
        {
            p.Mark("ui.tab");
            Box[] cells = ScreenLayout.Row(box, labels.Count, p.U(16f), float.MaxValue, square: false);
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                bool on = i == selected;
                p.FillRound(cells[i], cells[i].Height / 2f, on ? C.ButtonPrimary : C.SurfaceSunk);
                p.Text(labels[i], cells[i].CenterX, cells[i].CenterY, T.ButtonSecondary, on ? C.TextOnColor : C.TextPrimary, cells[i].Width * 0.86f);
                p.Hit(Touch(p, cells[i]), () => onSelect(index));
            }
        }

        /// <summary>A pill switch (Settings).</summary>
        public static void Toggle(IPainter p, Box box, bool on, Action action)
        {
            p.Mark("ui.toggle");
            p.FillRound(box, box.Height / 2f, on ? C.ButtonPrimary : C.StateLockBg);
            float r = (box.Height / 2f) - p.U(6f);
            float cx = on ? box.Right - (box.Height / 2f) : box.Left + (box.Height / 2f);
            p.FillCircle(cx, box.CenterY, r, Rgba.White);
            p.Hit(Touch(p, box), action);
        }

        /// <summary>A short message over the board (a refused tap, a hint).</summary>
        public static void Toast(IPainter p, Box area, string message)
        {
            float h = p.U(96f);
            float w = Math.Min(area.Width, p.MeasureText(message, T.Body) + p.U(80f));
            Box box = Box.FromCenter(area.CenterX, area.Bottom - (h / 2f) - p.U(16f), w, h);
            p.FillRound(box.Offset(0f, p.U(5f)), h / 2f, Rgba.Black.WithAlpha(0.12f));
            p.FillRound(box, h / 2f, C.SurfacePanel);
            p.Text(message, box.CenterX, box.CenterY, T.Body, C.TextPrimary, box.Width - p.U(40f));
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

        /// <summary>The pop scale of a card opened <paramref name="seconds"/> ago (motion.pop).</summary>
        public static float Pop(float seconds)
        {
            float k = Math.Max(0f, Math.Min(1f, seconds / DesignTokens.Motion.Pop.Seconds));
            float start = DesignTokens.Motion.Pop.Scale;
            return k < 0.7f ? start + ((1.04f - start) * (k / 0.7f)) : 1.04f - (0.04f * ((k - 0.7f) / 0.3f));
        }
    }
}
