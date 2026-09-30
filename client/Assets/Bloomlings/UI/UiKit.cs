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

        /// <summary>Text in a board type style: bold, uppercase and outline as the style says, shrinking to fit (FR-026, edge cases).</summary>
        public static TextMeshProUGUI Label(string name, Transform parent, string text, TypeStyle style, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI label = UiFactory.CreateText(name, parent, text, Units(style.Size), color, alignment);
            Style(label, style);
            UiFactory.Stretch(label.rectTransform);
            return label;
        }

        public static void Style(TextMeshProUGUI label, TypeStyle style)
        {
            FontStyles font = style.Bold ? FontStyles.Bold : FontStyles.Normal;
            if (style.Upper)
            {
                font |= FontStyles.UpperCase;
            }

            label.fontStyle = font;
            label.fontSize = Units(style.Size);
            label.enableAutoSizing = true;
            label.fontSizeMax = Units(style.Size);
            label.fontSizeMin = Units(style.Min);
            if (style.Outline > 0f)
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

        /// <summary>
        /// A raised surface (elev.raised): the edge color fills the rect and the face sits on top of it, lifted by the edge
        /// height. Children go into the returned face.
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

        // ---- Buttons ----

        /// <summary>The green primary button: PLAY, NEXT, CLAIM, RESUME, CONTINUE, Free rescue.</summary>
        public static Button PrimaryButton(string name, Transform parent, string label, Action onClick, TypeStyle? style = null)
        {
            Button button = RaisedButton(name, parent, UiTheme.Accent, UiTheme.AccentEdge, onClick, out Image face);
            Image shine = Pill("Shine", face.transform, new Color(UiTheme.AccentTop.r, UiTheme.AccentTop.g, UiTheme.AccentTop.b, 0.75f));
            UiFactory.Place(shine.rectTransform, 0.03f, 0.5f, 0.97f, 0.94f);
            TextMeshProUGUI text = Label("Label", face.transform, label, style ?? DesignTokens.Type.Button, UiTheme.TextOnColor);
            UiFactory.Place(text.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
            return button;
        }

        /// <summary>The cream secondary button: RESTART, SETTINGS, HOME, Restart, ×2 reward, Get +N.</summary>
        public static Button SecondaryButton(string name, Transform parent, string label, Action onClick, string? iconId = null)
        {
            Button button = RaisedButton(name, parent, UiTheme.Secondary, UiTheme.SecondaryEdge, onClick, out Image face);
            TextMeshProUGUI text = Label("Label", face.transform, label, DesignTokens.Type.ButtonSecondary, UiTheme.Text);
            UiFactory.Place(text.rectTransform, iconId != null ? 0.2f : 0.06f, 0.08f, 0.94f, 0.92f);
            if (iconId != null)
            {
                Image icon = UiFactory.CreateImage("Icon", face.transform, ProceduralSprites.Shape(iconId), UiTheme.Text);
                icon.preserveAspect = true;
                UiFactory.Place(icon.rectTransform, 0.06f, 0.22f, 0.18f, 0.78f);
            }

            return button;
        }

        /// <summary>A white round icon button (Settings, Pause, close, Wardrobe, Collection).</summary>
        public static Button RoundIconButton(string name, Transform parent, string shapeId, Action onClick)
        {
            Button button = RaisedButton(name, parent, UiTheme.IconButton, UiTheme.IconButtonEdge, onClick, out Image face, circle: true);
            Image glyph = UiFactory.CreateImage("Glyph", face.transform, ProceduralSprites.Shape(shapeId), UiTheme.IconGlyph);
            glyph.preserveAspect = true;
            UiFactory.Place(glyph.rectTransform, 0.24f, 0.24f, 0.76f, 0.76f);
            return button;
        }

        /// <summary>The dark 2× pill of the gameplay top bar.</summary>
        public static Button DarkPill(string name, Transform parent, string label, Action onClick)
        {
            Button button = RaisedButton(name, parent, UiTheme.DarkButton, UiTheme.Dark(UiTheme.DarkButton), onClick, out Image face);
            TextMeshProUGUI text = Label("Label", face.transform, label, DesignTokens.Type.LevelPill, UiTheme.TextOnColor);
            UiFactory.Place(text.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
            return button;
        }

        private static Button RaisedButton(string name, Transform parent, Color face, Color edge, Action onClick, out Image faceImage, bool circle = false)
        {
            Image edgeImage = circle ? UiFactory.CreateImage(name, parent, ProceduralSprites.Circle, edge, raycast: true) : Pill(name, parent, edge, raycast: true);
            edgeImage.preserveAspect = circle;
            faceImage = circle ? UiFactory.CreateImage("Face", edgeImage.transform, ProceduralSprites.Circle, face) : Pill("Face", edgeImage.transform, face);
            faceImage.preserveAspect = circle;
            RectTransform rect = UiFactory.Stretch(faceImage.rectTransform);
            rect.offsetMin = new Vector2(0f, Units(circle ? 6f : DesignTokens.Elevation.RaisedEdge));
            var button = edgeImage.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            edgeImage.gameObject.AddComponent<PressMotion>();
            return button;
        }

        // ---- Pills and badges ----

        /// <summary>The sky-blue "LEVEL N" pill of the gameplay top bar; returns its label.</summary>
        public static TextMeshProUGUI LevelPill(string name, Transform parent, out Image face)
        {
            face = Raised(name, parent, UiTheme.Of(DesignTokens.Colors.PillLevel), UiTheme.Of(DesignTokens.Colors.PillLevelEdge), pill: true);
            TextMeshProUGUI label = Label("Label", face.transform, string.Empty, DesignTokens.Type.LevelPill, UiTheme.TextOnColor);
            UiFactory.Place(label.rectTransform, 0.08f, 0.06f, 0.92f, 0.94f);
            return label;
        }

        /// <summary>A small badge: HARD in red or SUPER HARD in purple (frames 8 and 9).</summary>
        public static TextMeshProUGUI Badge(string name, Transform parent, string text, Color color, out Image pill)
        {
            pill = Pill(name, parent, color);
            TextMeshProUGUI label = Label("Label", pill.transform, text, DesignTokens.Type.Badge, UiTheme.TextOnColor);
            UiFactory.Place(label.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
            return label;
        }

        /// <summary>A dark count badge (booster charges, frame 14).</summary>
        public static TextMeshProUGUI CountBadge(string name, Transform parent, out Image disc)
        {
            disc = Pill(name, parent, UiTheme.Of(DesignTokens.Colors.BadgeCount));
            TextMeshProUGUI label = Label("Count", disc.transform, string.Empty, DesignTokens.Type.Badge, UiTheme.TextOnColor);
            UiFactory.Place(label.rectTransform, 0.1f, 0.1f, 0.9f, 0.9f);
            return label;
        }

        /// <summary>The Petal symbol: pink petals around a yellow center (FR-006).</summary>
        public static Image PetalIcon(string name, Transform parent)
        {
            Image petal = UiFactory.CreateImage(name, parent, ProceduralSprites.Petal, UiTheme.Petal);
            petal.preserveAspect = true;
            Image center = UiFactory.CreateImage("Center", petal.transform, ProceduralSprites.Circle, UiTheme.PetalCenter);
            center.preserveAspect = true;
            UiFactory.Place(center.rectTransform, 0.39f, 0.39f, 0.61f, 0.61f);
            return petal;
        }

        /// <summary>
        /// The Petals balance pill: the Petal symbol, the grouped balance and the green "+" (to the Store, once unlocked;
        /// frames 2, 3 and 17).
        /// </summary>
        public static PetalsPill PetalsPill(string name, Transform parent, Action? onPlus)
        {
            Image pill = Pill(name, parent, UiTheme.Of(DesignTokens.Colors.PillPetals), raycast: onPlus != null);
            var view = pill.gameObject.AddComponent<PetalsPill>();
            Image icon = PetalIcon("Petal", pill.transform);
            UiFactory.Place(icon.rectTransform, 0.02f, 0.05f, 0.26f, 0.95f);
            view.Balance = Label("Balance", pill.transform, "0", DesignTokens.Type.Count, UiTheme.Text);
            view.Balance.outlineWidth = 0f;
            UiFactory.Place(view.Balance.rectTransform, 0.27f, 0.08f, 0.76f, 0.92f);
            Image plus = UiFactory.CreateImage("Plus", pill.transform, ProceduralSprites.Circle, UiTheme.Accent, raycast: true);
            plus.preserveAspect = true;
            UiFactory.Place(plus.rectTransform, 0.76f, 0.1f, 0.98f, 0.9f);
            Image glyph = UiFactory.CreateImage("Glyph", plus.transform, ProceduralSprites.Shape("ui.plus"), Color.white);
            glyph.preserveAspect = true;
            UiFactory.Place(glyph.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);
            if (onPlus != null)
            {
                var button = pill.gameObject.AddComponent<Button>();
                button.targetGraphic = pill;
                button.onClick.AddListener(() =>
                {
                    GameFeedback.Current?.Play(SoundCue.Click);
                    onPlus();
                });
            }

            view.Plus = plus.gameObject;
            return view;
        }

        // ---- Cards and sheets ----

        /// <summary>
        /// A popup card (FR-007): a dimmed backdrop, a cream card with a soft shadow, a bold title and a round close
        /// button when <paramref name="onClose"/> is given. <paramref name="contentHeight"/> is in reference units.
        /// </summary>
        public static CardView Card(string name, Transform parent, string title, float contentHeight, Action? onClose, TypeStyle? titleStyle = null)
        {
            (float w, float h, Insets insets) = ScreenFrame();
            CardRegions regions = ScreenLayout.Card(w, h, insets, contentHeight);
            Box screen = new Box(0f, 0f, w, h);

            Image shade = UiFactory.CreateImage(name, parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            Image card = Rounded("Card", shade.transform, UiTheme.Panel, 56f, raycast: true);
            CardShadow(card);
            PlaceBox(card.rectTransform, regions.Card, screen);
            card.gameObject.AddComponent<PopMotion>();

            TextMeshProUGUI titleLabel = Label("Title", card.transform, title, titleStyle ?? DesignTokens.Type.Title, UiTheme.Text);
            PlaceBox(titleLabel.rectTransform, regions.Title, regions.Card);
            if (onClose != null)
            {
                Button close = RoundIconButton("Close", card.transform, "ui.close", onClose);
                PlaceBox((RectTransform)close.transform, regions.Close, regions.Card);
            }

            RectTransform body = PlaceBox(UiFactory.CreateRect("Body", card.transform), regions.Body, regions.Card);
            return new CardView(shade.gameObject, card.rectTransform, titleLabel, body, regions);
        }

        /// <summary>
        /// The jam bottom sheet (frame 10, FR-019): rises from the bottom with a grip, a title and a subtitle, over a light
        /// shade that keeps the board visible (spec 001 FR-027).
        /// </summary>
        public static SheetView Sheet(string name, Transform parent, string title, string subtitle, float contentHeight)
        {
            (float w, float h, Insets insets) = ScreenFrame();
            SheetRegions regions = ScreenLayout.Sheet(w, h, insets, contentHeight);
            Box screen = new Box(0f, 0f, w, h);

            // A light shade that takes no taps: the board stays visible and the top bar (Pause) stays usable.
            Image shade = UiFactory.CreateImage(name, parent, null, new Color(UiTheme.PanelShade.r, UiTheme.PanelShade.g, UiTheme.PanelShade.b, 0.3f), raycast: false);
            UiFactory.Stretch(shade.rectTransform);
            Image sheet = Rounded("Sheet", shade.transform, UiTheme.Panel, 64f, raycast: true);
            CardShadow(sheet);
            PlaceBox(sheet.rectTransform, regions.Sheet, screen);
            sheet.gameObject.AddComponent<SheetMotion>();
            Image grip = Pill("Grip", sheet.transform, UiTheme.PanelEdge);
            PlaceBox(grip.rectTransform, regions.Grip, regions.Sheet);
            TextMeshProUGUI titleLabel = Label("Title", sheet.transform, title, DesignTokens.Type.TitleCaps, UiTheme.Text);
            PlaceBox(titleLabel.rectTransform, regions.Title, regions.Sheet);
            TextMeshProUGUI subtitleLabel = Label("Subtitle", sheet.transform, subtitle, DesignTokens.Type.Body, UiTheme.TextSecondary);
            PlaceBox(subtitleLabel.rectTransform, regions.Subtitle, regions.Sheet);
            RectTransform body = PlaceBox(UiFactory.CreateRect("Body", sheet.transform), regions.Body, regions.Sheet);
            return new SheetView(shade.gameObject, sheet.rectTransform, titleLabel, subtitleLabel, body, regions);
        }

        /// <summary>A reward item of the milestone card and the jam sheet: an icon over an amount ("+200", "×2").</summary>
        public static void IconWithAmount(Transform parent, string name, Sprite icon, Color iconColor, string amount, bool petal)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            Image image = UiFactory.CreateImage("Icon", root, icon, iconColor);
            image.preserveAspect = true;
            UiFactory.Place(image.rectTransform, 0.15f, 0.36f, 0.85f, 1f);
            TextMeshProUGUI text = Label("Amount", root, amount, DesignTokens.Type.Count, UiTheme.Text);
            text.outlineWidth = 0f;
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

        public void Show(long petals, bool storeUnlocked)
        {
            Balance.text = NumberText.Group(petals);
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

    /// <summary>The press motion of buttons (motion.press): the element shrinks while pressed.</summary>
    public sealed class PressMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData e) => transform.localScale = Vector3.one * DesignTokens.Motion.Press.Scale;

        public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;

        public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;
    }

    /// <summary>The pop-in of popup cards (motion.pop), unscaled by 2× (menus are not gameplay).</summary>
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
            float seconds = DesignTokens.Motion.Sheet.Seconds;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - ((1f - (t / seconds)) * (1f - (t / seconds)));
                float drop = height * (1f - k);
                rect.anchorMin = new Vector2(min.x, min.y - drop);
                rect.anchorMax = new Vector2(max.x, max.y - drop);
                yield return null;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
        }
    }
}
