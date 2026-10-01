using System;
using System.Collections;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The design board's components in uGUI (spec 002 FR-005, FR-007; tasks T015):
    /// <list type="bullet">
    /// <item><description>primary and secondary buttons with a darker lower edge;</description></item>
    /// <item><description>round icon buttons;</description></item>
    /// <item><description>the 2× pill, the level pill and the Petals pill;</description></item>
    /// <item><description>badges and count badges;</description></item>
    /// <item><description>popup cards and the bottom sheet;</description></item>
    /// <item><description>text in the board's type styles.</description></item>
    /// </list>
    /// Sizes come from <see cref="DesignTokens"/> in reference units. Positions come from <see cref="ScreenLayout"/>
    /// boxes in screen pixels, turned into normalized anchors, so the Unity client and the playtest share one layout rule.
    /// The canvas matches the screen width at 1080 units (<see cref="UiFactory.CreateCanvas"/>).
    /// </summary>
    public static class UiKit
    {
        /// <summary>The corner radius, in sprite pixels, of <see cref="ProceduralSprites.RoundedSquare"/>.</summary>
        private const float RoundedCornerPixels = 10.4f;

        /// <summary>The screen size and its safe insets, in pixels (Unity's safe area is bottom-left based).</summary>
        public static (float Width, float Height, Insets Insets) ScreenFrame()
        {
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            Rect safe = Screen.safeArea;
            return (w, h, new Insets(h - safe.yMax, safe.yMin, safe.xMin, w - safe.xMax));
        }

        /// <summary>The whole screen as a <see cref="Box"/>.</summary>
        public static Box ScreenBox()
        {
            (float w, float h, Insets _) = ScreenFrame();
            return new Box(0f, 0f, w, h);
        }

        /// <summary>Canvas units for a size in reference units (tablets are capped by <see cref="DesignTokens.ScaleFor"/>).</summary>
        public static float Units(float reference)
        {
            (float w, float h, Insets _) = ScreenFrame();
            return reference * DesignTokens.ScaleFor(w, h) * DesignTokens.ReferenceWidth / w;
        }

        /// <summary>Anchors a rect to a screen box, relative to its parent's screen box.</summary>
        public static RectTransform PlaceBox(RectTransform rect, Box box, Box parent)
        {
            float pw = Mathf.Max(1f, parent.Width);
            float ph = Mathf.Max(1f, parent.Height);
            return UiFactory.Place(
                rect,
                (box.Left - parent.Left) / pw,
                1f - ((box.Bottom - parent.Top) / ph),
                (box.Right - parent.Left) / pw,
                1f - ((box.Top - parent.Top) / ph));
        }

        /// <summary>Anchors a rect to a screen box; its parent covers the whole screen.</summary>
        public static RectTransform PlaceScreen(RectTransform rect, Box box) => PlaceBox(rect, box, ScreenBox());

        // ---- Text ----

        /// <summary>
        /// Text in a board type style: Nunito by weight, sentence case unless the style says uppercase, shrinking to fit
        /// (FR-026, edge cases). With a <paramref name="look"/> the label gets its volume (spec 003 FR-009).
        /// </summary>
        public static TextMeshProUGUI Label(string name, Transform parent, string text, TypeStyle style, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, TextLook? look = null)
        {
            TextMeshProUGUI label = UiFactory.CreateText(name, parent, text, Units(style.Size), color, alignment);
            Style(label, style, look);
            UiFactory.Stretch(label.rectTransform);
            return label;
        }

        public static void Style(TextMeshProUGUI label, TypeStyle style, TextLook? look = null)
        {
            TMP_FontAsset? font = UiFonts.For(style);
            if (font != null)
            {
                label.font = font;
            }

            // The bundled weights carry the boldness; the system fallback still needs the bold style.
            FontStyles fontStyle = style.Bold && font == null ? FontStyles.Bold : FontStyles.Normal;
            if (style.Upper)
            {
                fontStyle |= FontStyles.UpperCase;
            }

            label.fontStyle = fontStyle;
            label.fontSize = Units(style.Size);
            label.enableAutoSizing = true;
            label.fontSizeMax = Units(style.Size);
            label.fontSizeMin = Units(style.Min);
            if (look != null)
            {
                UiFonts.Apply(label, style, look);
            }
            else if (style.Outline > 0f)
            {
                label.outlineWidth = 0.18f;
                label.outlineColor = UiTheme.TextOutline;
            }
        }

        /// <summary>A grouped number ("1 240", research R9).</summary>
        public static string Number(long value) => NumberText.Group(value);

        // ---- Surfaces ----

        /// <summary>A rounded rectangle whose corner radius is <paramref name="radiusUnits"/> reference units.</summary>
        public static Image Rounded(string name, Transform parent, Color color, float radiusUnits, bool raycast = false)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.RoundedSquare, color, raycast);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = RoundedCornerPixels / Mathf.Max(1f, Units(radiusUnits));
            return image;
        }

        /// <summary>A pill (fully rounded ends) that keeps its shape at any height.</summary>
        public static Image Pill(string name, Transform parent, Color color, bool raycast = false)
        {
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.PillSprite, color, raycast);
            image.type = Image.Type.Sliced;
            image.gameObject.AddComponent<PillShape>();
            return image;
        }

        /// <summary>A pill, or a rounded rectangle when <paramref name="radiusUnits"/> is set.</summary>
        private static Image Surface(string name, Transform parent, Color color, float? radiusUnits) =>
            radiusUnits.HasValue ? Rounded(name, parent, color, radiusUnits.Value) : Pill(name, parent, color);

        /// <summary>Stretches a rect over its parent less the given insets, in canvas units.</summary>
        private static RectTransform Inset(RectTransform rect, float left, float bottom, float right, float top)
        {
            UiFactory.Stretch(rect);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>A vertical gradient on a graphic (the plate, the button faces, the paper).</summary>
        public static void Gradient(Graphic graphic, Color top, Color bottom)
        {
            graphic.color = Color.white;
            var gradient = graphic.gameObject.AddComponent<VerticalGradient>();
            gradient.Top = top;
            gradient.Bottom = bottom;
        }

        /// <summary>
        /// A raised surface (spec 002 elev.raised, kept for pods and tiles until they turn volumetric): the edge color
        /// fills the rect and the face sits on top of it, lifted by the edge height. Children go into the returned face.
        /// </summary>
        public static Image Raised(string name, Transform parent, Color face, Color edge, bool pill, float radiusUnits = 36f, bool raycast = false)
        {
            Image edgeImage = pill ? Pill(name, parent, edge, raycast) : Rounded(name, parent, edge, radiusUnits, raycast);
            Image faceImage = pill ? Pill("Face", edgeImage.transform, face) : Rounded("Face", edgeImage.transform, face, radiusUnits);
            RectTransform rect = UiFactory.Stretch(faceImage.rectTransform);
            rect.offsetMin = new Vector2(0f, Units(DesignTokens.Elevation.RaisedEdge));
            return faceImage;
        }

        /// <summary>A soft card shadow (elev.card) under a graphic.</summary>
        public static void CardShadow(Graphic graphic)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, DesignTokens.Elevation.CardShadowAlpha);
            shadow.effectDistance = new Vector2(0f, -Units(DesignTokens.Elevation.CardShadowOffset));
        }

        // ---- The Garden recipe (spec 003 FR-006 to FR-008) ----

        /// <summary>
        /// A garden element: the cream plate (shadow, thickness, brown outline, gradient) and the raised face on it (the
        /// set's line, lip, gradient top and highlight), with a <see cref="GardenButton"/> that presses, springs back,
        /// breathes and greys out. <paramref name="heightUnits"/> sizes the recipe; <paramref name="radiusUnits"/> null
        /// makes pills and discs. Children go into <see cref="GardenButton.Content"/>, above the lip.
        /// </summary>
        public static GardenButton Garden(string name, Transform parent, ColorSet set, float heightUnits, float? radiusUnits = null, bool raycast = true, bool plate = true)
        {
            Image root = UiFactory.CreateImage(name, parent, ProceduralSprites.PillSprite, Color.clear, raycast);
            float depth = plate ? Units(DesignTokens.Garden.Depth(heightUnits)) : 0f;
            float line = Units(DesignTokens.Garden.Outline(heightUnits));
            float inset = plate ? Units(DesignTokens.Garden.PlateInset(heightUnits)) : 0f;
            float lip = Units(DesignTokens.Garden.Lip(heightUnits));
            if (plate)
            {
                Image shadow = Surface("Shadow", root.transform, UiTheme.Of(DesignTokens.Colors.GardenShadow.WithAlpha(0.14f)), radiusUnits);
                Inset(shadow.rectTransform, -Units(2f), -depth, -Units(2f), 2f * depth);
                Image thickness = Surface("Depth", root.transform, UiTheme.Of(DesignTokens.Colors.GardenPlateDepth), radiusUnits);
                Inset(thickness.rectTransform, 0f, 0f, 0f, depth);
                Image plateLine = Surface("PlateLine", root.transform, UiTheme.Of(DesignTokens.Colors.GardenOutline), radiusUnits);
                Inset(plateLine.rectTransform, 0f, depth, 0f, 0f);
                Image plateFace = Surface("Plate", root.transform, Color.white, radiusUnits);
                Inset(plateFace.rectTransform, line, depth + line, line, line);
                Gradient(plateFace, UiTheme.Of(DesignTokens.Colors.GardenPlateTop), UiTheme.Of(DesignTokens.Colors.GardenPlateBottom));
            }

            float? faceRadius = radiusUnits.HasValue ? Mathf.Max(0f, radiusUnits.Value - DesignTokens.Garden.PlateInset(heightUnits)) : (float?)null;
            RectTransform face = Inset(UiFactory.CreateRect("Face", root.transform), inset, depth + inset, inset, inset);
            Image faceLine = Surface("Line", face, UiTheme.Of(set.Line), faceRadius);
            UiFactory.Stretch(faceLine.rectTransform);
            Image faceLip = Surface("Lip", face, UiTheme.Of(set.Lip), faceRadius);
            Inset(faceLip.rectTransform, line, line, line, line);
            Image faceTop = Surface("Top", face, Color.white, faceRadius);
            Inset(faceTop.rectTransform, line, line + lip, line, line);
            Gradient(faceTop, UiTheme.Of(set.Top), UiTheme.Of(set.Face));
            Image shine = Pill("Shine", faceTop.transform, Color.white);
            UiFactory.Place(shine.rectTransform, 0.07f, 1f - DesignTokens.Garden.HighlightHeight, 0.93f, 0.93f);
            Gradient(shine, new Color(1f, 1f, 1f, DesignTokens.Garden.HighlightAlpha), new Color(1f, 1f, 1f, 0f));
            RectTransform content = Inset(UiFactory.CreateRect("Content", face), line, line + lip, line, line);

            var view = root.gameObject.AddComponent<GardenButton>();
            view.Init(set, faceLine, faceLip, faceTop, shine, content, Mathf.Max(0f, lip - Units(3f)));
            return view;
        }

        /// <summary>A label on a garden face, in the set's look (FR-009): volumetric on colors, dark brown on cream.</summary>
        public static TextMeshProUGUI GardenLabel(GardenButton button, string text, TypeStyle style)
        {
            TextLook look = GardenLook.LabelOn(button.Set);
            TextMeshProUGUI label = Label("Label", button.Content, text, style, UiTheme.Of(look.FillTop), look: look);
            UiFactory.Place(label.rectTransform, 0.06f, 0.04f, 0.94f, 0.96f);
            button.Track(label, style);
            return label;
        }

        /// <summary>A glyph on a garden face (FR-010): light with a dark line under it on colors, dark on cream and white.</summary>
        public static Image GardenGlyph(GardenButton button, Transform parent, string shapeId)
        {
            Image glyph = UiFactory.CreateImage("Glyph", parent, ProceduralSprites.Shape(shapeId), UiTheme.Of(GardenLook.GlyphOn(button.Set)));
            glyph.preserveAspect = true;
            if (GardenLook.LabelOn(button.Set).Volumetric)
            {
                var line = glyph.gameObject.AddComponent<Shadow>();
                line.effectColor = UiTheme.Of(button.Set.Line);
                line.effectDistance = new Vector2(0f, -Units(5f));
            }

            return glyph;
        }

        /// <summary>
        /// The leaves and white flower over a main button's corners (FR-011a): never a touch target, laid out from the
        /// button's height (<see cref="GardenLook.DecorationBoxes"/>), and off when the decoration switch is off.
        /// </summary>
        public static void Decoration(GardenButton button)
        {
            if (!DesignTokens.Garden.Decorations)
            {
                return;
            }

            Image topLeft = UiFactory.CreateImage("DecoTopLeft", button.transform, ProceduralSprites.Decoration(false), Color.white);
            Image bottomRight = UiFactory.CreateImage("DecoBottomRight", button.transform, ProceduralSprites.Decoration(true), Color.white);
            var layout = button.gameObject.AddComponent<DecorationLayout>();
            layout.TopLeft = topLeft.rectTransform;
            layout.BottomRight = bottomRight.rectTransform;
        }

        // ---- Buttons ----

        /// <summary>
        /// The green primary button (PLAY, NEXT, CLAIM, RESUME, CONTINUE, Free rescue): a raised green pill on a cream
        /// plate with a volumetric label. <paramref name="decorate"/> adds the leaves and flower (FR-011a) and
        /// <paramref name="playArrow"/> the ▶ as tall as the letters (FR-010).
        /// </summary>
        public static Button PrimaryButton(string name, Transform parent, string label, Action onClick, TypeStyle? style = null, bool decorate = false, bool playArrow = false)
        {
            TypeStyle s = style ?? DesignTokens.Type.Button;
            GardenButton view = Garden(name, parent, GardenLook.Green, s == DesignTokens.Type.ButtonLarge ? DesignTokens.Size.PlayHeight : DesignTokens.Size.CardPrimaryHeight);
            TextMeshProUGUI text = GardenLabel(view, label, s);
            if (playArrow)
            {
                UiFactory.Place(text.rectTransform, 0.08f, 0.04f, 0.74f, 0.96f);
                text.alignment = TextAlignmentOptions.Right;
                PlayArrow(view, s);
            }

            if (decorate)
            {
                Decoration(view);
            }

            return Clickable(view, onClick);
        }

        /// <summary>The cream secondary button (RESTART, SETTINGS, HOME, Restart, ×2 reward, Get +N).</summary>
        public static Button SecondaryButton(string name, Transform parent, string label, Action onClick, string? iconId = null)
        {
            GardenButton view = Garden(name, parent, GardenLook.Cream, DesignTokens.Size.SecondaryHeight);
            TextMeshProUGUI text = GardenLabel(view, label, DesignTokens.Type.ButtonSecondary);
            UiFactory.Place(text.rectTransform, iconId != null ? 0.2f : 0.06f, 0.04f, 0.94f, 0.96f);
            if (iconId != null)
            {
                Image icon = GardenGlyph(view, view.Content, iconId);
                UiFactory.Place(icon.rectTransform, 0.06f, 0.2f, 0.18f, 0.8f);
            }

            return Clickable(view, onClick);
        }

        /// <summary>A round icon button on a round plate (Settings, Pause, Wardrobe, Collection; red for close).</summary>
        public static Button RoundIconButton(string name, Transform parent, string shapeId, Action onClick, ColorSet? set = null)
        {
            ColorSet colors = set ?? (shapeId == "ui.close" ? GardenLook.Red : GardenLook.White);
            GardenButton view = Garden(name, parent, colors, DesignTokens.Size.IconButton);
            Image glyph = GardenGlyph(view, view.Content, shapeId);
            UiFactory.Place(glyph.rectTransform, 0.2f, 0.16f, 0.8f, 0.84f);
            return Clickable(view, onClick);
        }

        /// <summary>The dark 2× pill of the gameplay top bar.</summary>
        public static Button DarkPill(string name, Transform parent, string label, Action onClick)
        {
            GardenButton view = Garden(name, parent, GardenLook.Dark, 100f);
            TextMeshProUGUI text = GardenLabel(view, label, DesignTokens.Type.LevelPill);
            UiFactory.Place(text.rectTransform, 0.08f, 0.04f, 0.92f, 0.96f);
            return Clickable(view, onClick);
        }

        /// <summary>Makes a garden element a button: the click sound and action, the press and the greyed state.</summary>
        private static Button Clickable(GardenButton view, Action onClick)
        {
            var button = view.gameObject.AddComponent<Button>();
            button.targetGraphic = view.Top;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            view.Button = button;
            return button;
        }

        /// <summary>The ▶ after PLAY's label, as tall as the letters and in the label's look (FR-010).</summary>
        private static void PlayArrow(GardenButton view, TypeStyle style)
        {
            TextLook look = TextLook.OnColor(view.Set);
            float em = Units(style.Size);
            float grow = look.OutlineEm * em / Mathf.Max(1f, em * 0.46f / ShapeRaster.Margin);
            Func<float, float, float> sdf = ShapeLibrary.Get("ui.play");
            RectTransform box = UiFactory.CreateRect("Play", view.Content);
            UiFactory.Place(box, 0.75f, 0.5f, 0.75f, 0.5f);
            box.sizeDelta = new Vector2(em * 0.92f, em * 0.92f);
            box.anchoredPosition = new Vector2(em * 0.5f, 0f);
            Image line = UiFactory.CreateImage("Line", box, ProceduralSprites.Composite("ui.play/line", (x, y) => sdf(x, y) - grow), UiTheme.Of(look.Outline));
            UiFactory.Stretch(line.rectTransform);
            var extrusion = line.gameObject.AddComponent<Shadow>();
            extrusion.effectColor = UiTheme.Of(look.Outline);
            extrusion.effectDistance = new Vector2(0f, -look.ExtrudeEm * em);
            Image fill = UiFactory.CreateImage("Fill", box, ProceduralSprites.Shape("ui.play"), UiTheme.Of(look.FillTop));
            UiFactory.Stretch(fill.rectTransform);
        }

        // ---- Pills and badges ----

        /// <summary>The sky-blue "Level N" pill of the gameplay top bar; returns its label.</summary>
        public static TextMeshProUGUI LevelPill(string name, Transform parent, out GardenButton pill)
        {
            pill = Garden(name, parent, GardenLook.Blue, 112f, raycast: false);
            TextMeshProUGUI label = GardenLabel(pill, string.Empty, DesignTokens.Type.LevelPill);
            UiFactory.Place(label.rectTransform, 0.08f, 0.04f, 0.92f, 0.96f);
            return label;
        }

        /// <summary>A small sticker badge on a plate: HARD in red or SUPER HARD in purple (frames 8 and 9, FR-014).</summary>
        public static TextMeshProUGUI Badge(string name, Transform parent, string text, ColorSet set, out GardenButton badge)
        {
            badge = Garden(name, parent, set, 50f, raycast: false);
            TextMeshProUGUI label = GardenLabel(badge, text, DesignTokens.Type.Badge);
            UiFactory.Place(label.rectTransform, 0.08f, 0.04f, 0.92f, 0.96f);
            return label;
        }

        /// <summary>A count badge (booster charges, "×N"): white on a dark brown disc with a cream ring and a brown outline.</summary>
        public static TextMeshProUGUI CountBadge(string name, Transform parent, out Image disc)
        {
            disc = Pill(name, parent, UiTheme.Of(DesignTokens.Colors.GardenOutline));
            Image ring = Pill("Ring", disc.transform, UiTheme.Of(DesignTokens.Colors.GardenBadgeRing));
            Inset(ring.rectTransform, Units(2f), Units(2f), Units(2f), Units(2f));
            Image fill = Pill("Fill", ring.transform, UiTheme.Of(DesignTokens.Colors.GardenBadge));
            Inset(fill.rectTransform, Units(4f), Units(4f), Units(4f), Units(4f));
            TextMeshProUGUI label = Label("Count", fill.transform, string.Empty, DesignTokens.Type.Badge, UiTheme.TextOnColor);
            UiFactory.Place(label.rectTransform, 0.1f, 0.1f, 0.9f, 0.9f);
            return label;
        }

        /// <summary>The Petal symbol with its outline: pink petals around a yellow center (FR-006, spec 003 FR-010).</summary>
        public static Image PetalIcon(string name, Transform parent)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get("currency.petal");
            Image line = UiFactory.CreateImage(name, parent, ProceduralSprites.Composite("currency.petal/line", (x, y) => sdf(x, y) - 0.07f), UiTheme.Of(DesignTokens.Colors.PetalEdge));
            line.preserveAspect = true;
            Image petal = UiFactory.CreateImage("Petal", line.transform, ProceduralSprites.Petal, UiTheme.Petal);
            petal.preserveAspect = true;
            UiFactory.Stretch(petal.rectTransform);
            Image center = UiFactory.CreateImage("Center", line.transform, ProceduralSprites.Circle, UiTheme.PetalCenter);
            center.preserveAspect = true;
            UiFactory.Place(center.rectTransform, 0.39f, 0.39f, 0.61f, 0.61f);
            return line;
        }

        /// <summary>
        /// The Petals balance pill: a white raised pill on a plate with the Petal symbol, the grouped balance and a round
        /// green "+" on its own plate (to the Store, once unlocked; frames 2, 3 and 17; FR-013).
        /// </summary>
        public static PetalsPill PetalsPill(string name, Transform parent, Action? onPlus)
        {
            GardenButton pill = Garden(name, parent, GardenLook.White, 100f, raycast: onPlus != null);
            var view = pill.gameObject.AddComponent<PetalsPill>();
            Image icon = PetalIcon("Petal", pill.transform);
            UiFactory.Place(icon.rectTransform, 0.02f, 0.05f, 0.26f, 0.95f);
            view.Balance = Label("Balance", pill.Content, "0", DesignTokens.Type.Count, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain), look: TextLook.Plain(DesignTokens.Colors.GardenLabelPlain));
            UiFactory.Place(view.Balance.rectTransform, 0.24f, 0.04f, 0.74f, 0.96f);
            GardenButton plus = Garden("Plus", pill.transform, GardenLook.Green, 72f, raycast: false);
            UiFactory.Place((RectTransform)plus.transform, 0.76f, 0.06f, 0.98f, 0.94f);
            plus.gameObject.AddComponent<SquareInParent>();
            Image glyph = GardenGlyph(plus, plus.Content, "ui.plus");
            UiFactory.Place(glyph.rectTransform, 0.2f, 0.16f, 0.8f, 0.84f);
            if (onPlus != null)
            {
                Clickable(pill, onPlus);
            }

            view.Plus = plus.gameObject;
            return view;
        }

        // ---- Cards and sheets ----

        /// <summary>
        /// Paper in a wooden frame (spec 003 FR-015): a soft shadow, the frame's thickness below, the brown frame line and
        /// the paper gradient. Cards, the sheet, the board and the slot row use it. Children drawn on the paper go into
        /// the returned root, after its layers.
        /// </summary>
        public static Image Paper(string name, Transform parent, float radiusUnits, float frameUnits, float depthUnits, bool raycast = true)
        {
            Image root = UiFactory.CreateImage(name, parent, ProceduralSprites.PillSprite, Color.clear, raycast);
            float depth = Units(depthUnits);
            float frame = Units(frameUnits);
            Image shadow = Rounded("Shadow", root.transform, UiTheme.Of(DesignTokens.Colors.GardenShadow.WithAlpha(0.2f)), radiusUnits);
            Inset(shadow.rectTransform, -Units(4f), -depth * 1.8f, -Units(4f), depth * 1.8f);
            Image thickness = Rounded("Depth", root.transform, UiTheme.Of(DesignTokens.Colors.GardenWoodDepth), radiusUnits);
            Inset(thickness.rectTransform, 0f, -depth, 0f, depth);
            Image wood = Rounded("Frame", root.transform, UiTheme.Of(DesignTokens.Colors.GardenWood), radiusUnits);
            UiFactory.Stretch(wood.rectTransform);
            Image paper = Rounded("Paper", root.transform, Color.white, Mathf.Max(0f, radiusUnits - frameUnits));
            Inset(paper.rectTransform, frame, frame, frame, frame);
            Gradient(paper, UiTheme.Of(DesignTokens.Colors.GardenPaperTop), UiTheme.Of(DesignTokens.Colors.GardenPaperBottom));
            return root;
        }

        /// <summary>
        /// A popup card (FR-007, spec 003 FR-015): a dimmed backdrop, paper in a wooden frame, a colored header band shaped
        /// like a button with the title in volumetric letters, and a red round close button when
        /// <paramref name="onClose"/> is given. <paramref name="contentHeight"/> is in reference units.
        /// </summary>
        public static CardView Card(string name, Transform parent, string title, float contentHeight, Action? onClose, TypeStyle? titleStyle = null, ColorSet? header = null)
        {
            (float w, float h, Insets insets) = ScreenFrame();
            CardRegions regions = ScreenLayout.Card(w, h, insets, contentHeight);
            Box screen = new Box(0f, 0f, w, h);
            float scale = DesignTokens.ScaleFor(w, h);

            Image shade = UiFactory.CreateImage(name, parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            Image card = Paper("Card", shade.transform, 56f, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            PlaceBox(card.rectTransform, regions.Card, screen);
            card.gameObject.AddComponent<PopMotion>();

            ColorSet set = header ?? GardenLook.Green;
            float margin = 18f * scale;
            var bandBox = new Box(regions.Card.Left + margin, regions.Card.Top + margin, regions.Card.Right - margin, regions.Title.Bottom - (4f * scale));
            GardenButton band = Garden("Header", card.transform, set, 108f, radiusUnits: 40f, raycast: false, plate: false);
            PlaceBox((RectTransform)band.transform, bandBox, regions.Card);
            TypeStyle style = titleStyle ?? DesignTokens.Type.Title;
            TextMeshProUGUI titleLabel = Label("Title", band.Content, title, style, UiTheme.TextOnColor, look: TextLook.OnColor(set));
            UiFactory.Place(titleLabel.rectTransform, 0.16f, 0f, 0.84f, 1f);
            if (onClose != null)
            {
                Button close = RoundIconButton("Close", card.transform, "ui.close", onClose, GardenLook.Red);
                PlaceBox((RectTransform)close.transform, regions.Close, regions.Card);
            }

            RectTransform body = PlaceBox(UiFactory.CreateRect("Body", card.transform), regions.Body, regions.Card);
            return new CardView(shade.gameObject, card.rectTransform, titleLabel, body, regions);
        }

        /// <summary>
        /// The jam bottom sheet (frame 10, FR-019): paper in a wooden frame that rises from the bottom and settles with a
        /// small bounce (spec 003 FR-018), with a grip, a title and a subtitle, over a light shade that keeps the board
        /// visible (spec 001 FR-027).
        /// </summary>
        public static SheetView Sheet(string name, Transform parent, string title, string subtitle, float contentHeight)
        {
            (float w, float h, Insets insets) = ScreenFrame();
            SheetRegions regions = ScreenLayout.Sheet(w, h, insets, contentHeight);
            Box screen = new Box(0f, 0f, w, h);

            // A light shade that takes no taps: the board stays visible and the top bar (Pause) stays usable.
            Image shade = UiFactory.CreateImage(name, parent, null, new Color(UiTheme.PanelShade.r, UiTheme.PanelShade.g, UiTheme.PanelShade.b, 0.3f), raycast: false);
            UiFactory.Stretch(shade.rectTransform);
            Image sheet = Paper("Sheet", shade.transform, 64f, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            PlaceBox(sheet.rectTransform, regions.Sheet, screen);
            sheet.gameObject.AddComponent<SheetMotion>();
            Image grip = Pill("Grip", sheet.transform, UiTheme.Of(DesignTokens.Colors.GardenWellEdge));
            PlaceBox(grip.rectTransform, regions.Grip, regions.Sheet);
            TextMeshProUGUI titleLabel = Label("Title", sheet.transform, title, DesignTokens.Type.TitleCaps, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain), look: TextLook.Plain(DesignTokens.Colors.GardenLabelPlain));
            PlaceBox(titleLabel.rectTransform, regions.Title, regions.Sheet);
            TextMeshProUGUI subtitleLabel = Label("Subtitle", sheet.transform, subtitle, DesignTokens.Type.Body, UiTheme.TextSecondary);
            PlaceBox(subtitleLabel.rectTransform, regions.Subtitle, regions.Sheet);
            RectTransform body = PlaceBox(UiFactory.CreateRect("Body", sheet.transform), regions.Body, regions.Sheet);
            return new SheetView(shade.gameObject, sheet.rectTransform, titleLabel, subtitleLabel, body, regions);
        }

        /// <summary>A list row (Store, Leaderboard): an outlined rounded panel; the player's own row raised and green.</summary>
        public static Image Row(string name, Transform parent, bool highlighted)
        {
            Image line = Rounded(name, parent, UiTheme.Of(DesignTokens.Colors.GardenOutline), 28f);
            Image panel = Rounded("Panel", line.transform, Color.white, 26f);
            Inset(panel.rectTransform, Units(3f), Units(3f), Units(3f), Units(3f));
            if (highlighted)
            {
                Gradient(panel, UiTheme.Of(DesignTokens.Colors.SurfaceRowHighlight.Lighten(0.4f)), UiTheme.Of(DesignTokens.Colors.SurfaceRowHighlight));
            }
            else
            {
                Gradient(panel, Color.white, UiTheme.Of(DesignTokens.Colors.GardenPaperTop));
            }

            return line;
        }

        /// <summary>A sunk well (unselected tabs, toggle tracks, empty slots): darker paper with a shadow along its top.</summary>
        public static Image Well(string name, Transform parent, Color fill, float? radiusUnits = null, bool raycast = false)
        {
            Image edge = radiusUnits.HasValue ? Rounded(name, parent, UiTheme.Of(DesignTokens.Colors.GardenWellEdge), radiusUnits.Value, raycast) : Pill(name, parent, UiTheme.Of(DesignTokens.Colors.GardenWellEdge), raycast);
            Image inner = Surface("Fill", edge.transform, Color.white, radiusUnits);
            Inset(inner.rectTransform, Units(3f), Units(3f), Units(3f), Units(3f));
            Color shadowTop = UiTheme.Of(DesignTokens.Colors.GardenShadow.WithAlpha(0.2f).Over(UiTheme.ToRgba(fill)));
            Gradient(inner, shadowTop, fill);
            return edge;
        }

        /// <summary>Tabs (Store, Wardrobe): the selected one a raised green button on a plate, the others sunk (FR-016).</summary>
        public static TabsView Tabs(string name, Transform parent, string[] labels, Action<int> onSelect)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = root.gameObject.AddComponent<TabsView>();
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                RectTransform cell = UiFactory.Place(UiFactory.CreateRect("Tab" + i, root), i / (float)labels.Length, 0f, (i + 1) / (float)labels.Length, 1f);
                cell.offsetMin = new Vector2(Units(8f), 0f);
                cell.offsetMax = new Vector2(-Units(8f), 0f);
                Button on = PrimaryButton("On", cell, labels[i], () => onSelect(index), DesignTokens.Type.ButtonSecondary);
                Image off = Well("Off", cell, UiTheme.Of(DesignTokens.Colors.GardenTabSunk), raycast: true);
                Inset(off.rectTransform, Units(6f), Units(8f), Units(6f), Units(8f));
                TextMeshProUGUI offLabel = Label("Label", off.transform, labels[i], DesignTokens.Type.ButtonSecondary, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain), look: TextLook.Plain(DesignTokens.Colors.GardenLabelPlain));
                UiFactory.Place(offLabel.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
                var offButton = off.gameObject.AddComponent<Button>();
                offButton.targetGraphic = off;
                offButton.onClick.AddListener(() =>
                {
                    GameFeedback.Current?.Play(SoundCue.Click);
                    onSelect(index);
                });
                view.Add(on.gameObject, off.gameObject);
            }

            return view;
        }

        /// <summary>A switch (Settings): a chunky outlined track with a raised knob (FR-016); on is green with the knob right.</summary>
        public static ToggleView Toggle(string name, Transform parent, Action onClick)
        {
            Image track = Well(name, parent, UiTheme.Of(DesignTokens.Colors.GardenTabSunk), raycast: true);
            var view = track.gameObject.AddComponent<ToggleView>();
            Image onFill = Pill("On", track.transform, UiTheme.Of(GardenLook.Green.Line));
            UiFactory.Stretch(onFill.rectTransform);
            Image onFace = Pill("Face", onFill.transform, UiTheme.Of(GardenLook.Green.Face));
            Inset(onFace.rectTransform, Units(3f), Units(3f), Units(3f), Units(3f));
            GardenButton knob = Garden("Knob", track.transform, GardenLook.White, 72f, raycast: false, plate: false);
            view.Init(onFill.gameObject, (RectTransform)knob.transform);
            var button = track.gameObject.AddComponent<Button>();
            button.targetGraphic = track;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            return view;
        }

        /// <summary>A reward item of the milestone card and the jam sheet: an icon over an amount ("+200", "×2").</summary>
        public static void IconWithAmount(Transform parent, string name, Sprite icon, Color iconColor, string amount, bool petal)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            Image image = UiFactory.CreateImage("Icon", root, icon, iconColor);
            image.preserveAspect = true;
            UiFactory.Place(image.rectTransform, 0.15f, 0.36f, 0.85f, 1f);
            TextMeshProUGUI text = Label("Amount", root, amount, DesignTokens.Type.Count, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain), look: TextLook.Plain(DesignTokens.Colors.GardenLabelPlain));
            UiFactory.Place(text.rectTransform, petal ? 0f : 0.05f, 0f, petal ? 0.78f : 0.95f, 0.34f);
            if (petal)
            {
                Image symbol = PetalIcon("Petal", root);
                UiFactory.Place(symbol.rectTransform, 0.78f, 0.04f, 0.98f, 0.3f);
            }
        }

        /// <summary>"+35" followed by the Petal symbol, for costs and rewards.</summary>
        public static string PetalAmount(long amount, bool plus) =>
            plus ? NumberText.Plus(amount) : NumberText.Group(amount);

        internal static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A popup card built by <see cref="UiKit.Card"/>.</summary>
    public sealed class CardView
    {
        public CardView(GameObject root, RectTransform card, TextMeshProUGUI title, RectTransform body, CardRegions regions)
        {
            Root = root;
            CardRect = card;
            Title = title;
            Body = body;
            Regions = regions;
        }

        /// <summary>The dimmed backdrop holding the card: show or hide this.</summary>
        public GameObject Root { get; }

        public RectTransform CardRect { get; }

        public TextMeshProUGUI Title { get; }

        public RectTransform Body { get; }

        public CardRegions Regions { get; }
    }

    /// <summary>A bottom sheet built by <see cref="UiKit.Sheet"/>.</summary>
    public sealed class SheetView
    {
        public SheetView(GameObject root, RectTransform sheet, TextMeshProUGUI title, TextMeshProUGUI subtitle, RectTransform body, SheetRegions regions)
        {
            Root = root;
            SheetRect = sheet;
            Title = title;
            Subtitle = subtitle;
            Body = body;
            Regions = regions;
        }

        public GameObject Root { get; }

        public RectTransform SheetRect { get; }

        public TextMeshProUGUI Title { get; }

        public TextMeshProUGUI Subtitle { get; }

        public RectTransform Body { get; }

        public SheetRegions Regions { get; }
    }

    /// <summary>The Petals balance pill (<see cref="UiKit.PetalsPill"/>).</summary>
    public sealed class PetalsPill : MonoBehaviour
    {
        public TextMeshProUGUI Balance { get; set; } = null!;

        public GameObject Plus { get; set; } = null!;

        private long _shown = -1;
        private CountUp? _count;

        /// <summary>Shows the balance; a rise counts up from the old balance (spec 003 FR-020).</summary>
        public void Show(long petals, bool storeUnlocked)
        {
            if (_shown >= 0 && petals > _shown)
            {
                _count ??= CountUp.On(Balance, NumberText.Group);
                _count.Run(_shown, petals);
            }
            else
            {
                Balance.text = NumberText.Group(petals);
            }

            _shown = petals;
            Plus.SetActive(storeUnlocked);
        }
    }

    /// <summary>Keeps a sliced circle sprite fully rounded at the rect's height (a pill).</summary>
    public sealed class PillShape : MonoBehaviour
    {
        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            var image = GetComponent<Image>();
            RectTransform rect = (RectTransform)transform;
            if (image != null && rect.rect.height > 0f)
            {
                image.pixelsPerUnitMultiplier = 32f / (rect.rect.height / 2f);
            }
        }
    }

    /// <summary>
    /// The press of elements that are not garden buttons yet (pods, booster tiles; spec 003 FR-017): a squash while the
    /// finger is down, then a spring back with one overshoot, on unscaled time so 2× never changes it.
    /// </summary>
    public sealed class PressMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private bool _down;
        private float _releasedAt = -10f;

        /// <summary>Tiles squash a little more (contracts/booster-tile.md).</summary>
        public bool Tile { get; set; }

        public void OnPointerDown(PointerEventData e) => _down = true;

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (_down)
            {
                _down = false;
                _releasedAt = Time.unscaledTime;
            }
        }

        private float _applied = float.NaN;

        private void Update()
        {
            // Only touch the transform when the press changes, so idle elements never dirty the canvas (FR-027).
            float depth = GardenLook.PressDepth(_down, Time.unscaledTime - _releasedAt);
            if (depth == _applied)
            {
                return;
            }

            _applied = depth;
            (float sx, float sy) = GardenLook.Squash(depth, Tile);
            transform.localScale = new Vector3(sx, sy, 1f);
        }
    }

    /// <summary>
    /// A garden element built by <see cref="UiKit.Garden"/> (spec 003 FR-006, FR-007, FR-012, FR-017, FR-019):
    /// <list type="bullet">
    /// <item><description>the press: the face sinks into its lip at once and the element squashes; on release it
    /// springs back with one overshoot; the click is never delayed;</description></item>
    /// <item><description>the breath of the one waiting button (<see cref="Breathe"/>);</description></item>
    /// <item><description>the greyed look of a button that is not interactable: the same shapes, no color, no
    /// highlight, no press.</description></item>
    /// </list>
    /// Everything runs on unscaled time, so the 2× setting never changes it (FR-021).
    /// </summary>
    public sealed class GardenButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private readonly System.Collections.Generic.List<(TextMeshProUGUI Label, TypeStyle Style)> _labels = new System.Collections.Generic.List<(TextMeshProUGUI, TypeStyle)>();
        private Image _line = null!;
        private Image _lip = null!;
        private Image _shine = null!;
        private RectTransform _content = null!;
        private float _travel;
        private Vector2 _topMin;
        private Vector2 _topMax;
        private Vector2 _contentMin;
        private Vector2 _contentMax;
        private Vector2 _lipMax;
        private Vector2 _lineMax;
        private bool _down;
        private float _releasedAt = -10f;
        private bool? _shownEnabled;
        private float _appliedDepth = float.NaN;
        private float _appliedBreath = float.NaN;

        /// <summary>The element's color set.</summary>
        public ColorSet Set { get; private set; } = GardenLook.Green;

        /// <summary>The face's top part (the button's target graphic).</summary>
        public Image Top { get; private set; } = null!;

        /// <summary>Where labels and glyphs go: the face above its lip.</summary>
        public RectTransform Content => _content;

        /// <summary>The button this element is, if any (greys out when not interactable).</summary>
        public Button? Button { get; set; }

        /// <summary>Whether this is the screen's one waiting button, breathing gently (FR-019).</summary>
        public bool Breathe { get; set; }

        /// <summary>Booster tiles squash a little more (contracts/booster-tile.md "States").</summary>
        public bool TileSquash { get; set; }

        public void Init(ColorSet set, Image line, Image lip, Image top, Image shine, RectTransform content, float travel)
        {
            _line = line;
            _lip = lip;
            Top = top;
            _shine = shine;
            _content = content;
            _travel = travel;
            _topMin = top.rectTransform.offsetMin;
            _topMax = top.rectTransform.offsetMax;
            _contentMin = content.offsetMin;
            _contentMax = content.offsetMax;
            _lipMax = lip.rectTransform.offsetMax;
            _lineMax = line.rectTransform.offsetMax;
            Recolor(set, true);
        }

        /// <summary>A label in the element's look, recolored with it.</summary>
        public void Track(TextMeshProUGUI label, TypeStyle style) => _labels.Add((label, style));

        /// <summary>Gives the element another color set (the level pill on Super Hard, the HARD badge).</summary>
        public void SetColors(ColorSet set)
        {
            Set = set;
            _shownEnabled = null;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (Interactable)
            {
                _down = true;
            }
        }

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e) => Release();

        private bool Interactable => Button == null || Button.interactable;

        private void Release()
        {
            if (_down)
            {
                _down = false;
                _releasedAt = Time.unscaledTime;
            }
        }

        private void Recolor(ColorSet set, bool enabled)
        {
            ColorSet shown = enabled ? set : set.Disabled();
            _line.color = UiTheme.Of(shown.Line);
            _lip.color = UiTheme.Of(shown.Lip);
            VerticalGradient? gradient = Top.GetComponent<VerticalGradient>();
            if (gradient != null)
            {
                gradient.Top = UiTheme.Of(shown.Top);
                gradient.Bottom = UiTheme.Of(shown.Face);
                Top.SetVerticesDirty();
            }

            _shine.enabled = enabled;
            foreach ((TextMeshProUGUI label, TypeStyle style) in _labels)
            {
                UiFonts.Apply(label, style, GardenLook.LabelOn(shown));
            }

            _shownEnabled = enabled;
        }

        private void Update()
        {
            bool enabled = Interactable;
            if (_shownEnabled != enabled)
            {
                Recolor(Set, enabled);
            }

            float depth = enabled ? GardenLook.PressDepth(_down, Time.unscaledTime - _releasedAt) : 0f;
            float breath = Breathe && enabled && depth == 0f ? GardenLook.Breathe(Time.unscaledTime) : 1f;

            // Only touch the rects when the press or the breath changes, so idle buttons never dirty the canvas (FR-027).
            if (depth == _appliedDepth && breath == _appliedBreath)
            {
                return;
            }

            _appliedDepth = depth;
            _appliedBreath = breath;
            float shift = _travel * Mathf.Clamp(depth, -0.25f, 1f);
            Top.rectTransform.offsetMin = _topMin - new Vector2(0f, shift);
            Top.rectTransform.offsetMax = _topMax - new Vector2(0f, shift);
            _content.offsetMin = _contentMin - new Vector2(0f, shift);
            _content.offsetMax = _contentMax - new Vector2(0f, shift);
            _lip.rectTransform.offsetMax = _lipMax - new Vector2(0f, Mathf.Max(0f, shift));
            _line.rectTransform.offsetMax = _lineMax - new Vector2(0f, Mathf.Max(0f, shift));
            (float sx, float sy) = GardenLook.Squash(depth, TileSquash);
            transform.localScale = new Vector3(sx * breath, sy * breath, 1f);
        }
    }

    /// <summary>A vertical two-color gradient over a graphic's mesh (plates, faces, paper; spec 003 FR-007).</summary>
    public sealed class VerticalGradient : BaseMeshEffect
    {
        public Color Top { get; set; } = Color.white;

        public Color Bottom { get; set; } = Color.white;

        public override void ModifyMesh(VertexHelper vh)
        {
            int count = vh.currentVertCount;
            if (count == 0)
            {
                return;
            }

            var vertex = new UIVertex();
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                min = Mathf.Min(min, vertex.position.y);
                max = Mathf.Max(max, vertex.position.y);
            }

            float height = Mathf.Max(0.0001f, max - min);
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.color = Color.Lerp(Bottom, Top, (vertex.position.y - min) / height);
                vh.SetUIVertex(vertex, i);
            }
        }
    }

    /// <summary>Lays the decoration over a main button's corners from the button's height (spec 003 FR-011a).</summary>
    public sealed class DecorationLayout : MonoBehaviour
    {
        public RectTransform TopLeft { get; set; } = null!;

        public RectTransform BottomRight { get; set; } = null!;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (TopLeft == null || BottomRight == null)
            {
                return;
            }

            Rect rect = ((RectTransform)transform).rect;
            (Box topLeft, Box bottomRight) = GardenLook.DecorationBoxes(new Box(0f, 0f, rect.width, rect.height));
            Place(TopLeft, topLeft, rect);
            Place(BottomRight, bottomRight, rect);
        }

        /// <summary>A box in the button's top-down coordinates as a rect anchored at its top-left corner.</summary>
        private static void Place(RectTransform target, Box box, Rect rect)
        {
            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = new Vector2(box.Width, box.Height);
            target.anchoredPosition = new Vector2(box.CenterX, -box.CenterY);
        }
    }

    /// <summary>Keeps a rect square at its height, centered where its anchors put it (the Petals "+").</summary>
    public sealed class SquareInParent : MonoBehaviour
    {
        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            var rect = (RectTransform)transform;
            float h = rect.rect.height;
            float w = rect.rect.width;
            if (h > 0f && w > h)
            {
                float trim = (w - h) / 2f;
                rect.offsetMin = new Vector2(trim, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-trim, rect.offsetMax.y);
            }
        }
    }

    /// <summary>Tabs built by <see cref="UiKit.Tabs"/>: shows the raised button for the selected tab, the well for the others.</summary>
    public sealed class TabsView : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<(GameObject On, GameObject Off)> _tabs = new System.Collections.Generic.List<(GameObject, GameObject)>();

        public void Add(GameObject on, GameObject off) => _tabs.Add((on, off));

        public void Select(int index)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].On.SetActive(i == index);
                _tabs[i].Off.SetActive(i != index);
            }
        }
    }

    /// <summary>A switch built by <see cref="UiKit.Toggle"/>: green with the knob right when on.</summary>
    public sealed class ToggleView : MonoBehaviour
    {
        private GameObject _on = null!;
        private RectTransform _knob = null!;

        public void Init(GameObject on, RectTransform knob)
        {
            _on = on;
            _knob = knob;
        }

        public void Show(bool on)
        {
            _on.SetActive(on);
            _knob.anchorMin = new Vector2(on ? 0.5f : -0.04f, -0.12f);
            _knob.anchorMax = new Vector2(on ? 1.04f : 0.5f, 1.12f);
            _knob.offsetMin = Vector2.zero;
            _knob.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// A number that counts up to its value (spec 003 FR-020), from 0 or from where it was, on unscaled time; it never
    /// blocks anything.
    /// </summary>
    public sealed class CountUp : MonoBehaviour
    {
        private TextMeshProUGUI _label = null!;

        /// <summary>How a value is written ("+20 Petals").</summary>
        public Func<long, string> Format { get; set; } = NumberText.Group;
        private long _from;
        private long _to;
        private float _startedAt = -10f;

        public static CountUp On(TextMeshProUGUI label, Func<long, string> format)
        {
            var count = label.gameObject.AddComponent<CountUp>();
            count._label = label;
            count.Format = format;
            return count;
        }

        /// <summary>Counts from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public void Run(long from, long to)
        {
            _from = from;
            _to = to;
            _startedAt = Time.unscaledTime;
            _label.text = Format(from);
        }

        private void Update()
        {
            float since = Time.unscaledTime - _startedAt;
            if (since < 0f || since > DesignTokens.Motion.CountUp.Seconds + 0.1f)
            {
                return;
            }

            _label.text = Format(_from + GardenLook.CountUp(_to - _from, since));
        }
    }

    /// <summary>The pop-in of popup cards (motion.pop): one small overshoot, settled within 0.35 s; unscaled by 2×.</summary>
    public sealed class PopMotion : MonoBehaviour
    {
        private void OnEnable()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            float seconds = DesignTokens.Motion.Pop.Seconds;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                float scale = k < 0.7f ? Mathf.Lerp(DesignTokens.Motion.Pop.Scale, 1.04f, k / 0.7f) : Mathf.Lerp(1.04f, 1f, (k - 0.7f) / 0.3f);
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }
    }

    /// <summary>The bottom sheet sliding up (motion.sheet).</summary>
    public sealed class SheetMotion : MonoBehaviour
    {
        private void OnEnable()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            RectTransform rect = (RectTransform)transform;
            Vector2 min = rect.anchorMin;
            Vector2 max = rect.anchorMax;
            float height = max.y - min.y;
            // Rises past its place by a little and settles back: one small bounce (spec 003 FR-018).
            float seconds = DesignTokens.Motion.Sheet.Seconds * 1.35f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = SheetRise(t / DesignTokens.Motion.Sheet.Seconds);
                float drop = height * (1f - k);
                rect.anchorMin = new Vector2(min.x, min.y - drop);
                rect.anchorMax = new Vector2(max.x, max.y - drop);
                yield return null;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private static float SheetRise(float t)
        {
            if (t < 1f)
            {
                return (1f - ((1f - t) * (1f - t) * (1f - t))) * 1.03f;
            }

            float k = Mathf.Min(1f, (t - 1f) / 0.35f);
            return 1.03f - (0.03f * (1f - ((1f - k) * (1f - k) * (1f - k))));
        }
    }
}
