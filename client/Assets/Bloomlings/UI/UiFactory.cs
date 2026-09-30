using System;
using Bloomlings.Client.Art;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Bloomlings.Client.Services.Feedback;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// Builds uGUI objects from code so screens need no hand-made prefabs. Rects are positioned with normalized
    /// anchors; the canvas scales from a 1080×1920 portrait reference.
    /// </summary>
    public static class UiFactory
    {
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTheme.ReferenceResolution;
            // Match the width: the canvas is always 1080 units wide, so design-token sizes map one to one (spec 002).
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Anchors a rect to a normalized region of its parent, with no offsets.</summary>
        public static RectTransform Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect) => Place(rect, 0f, 0f, 1f, 1f);

        /// <summary>A rect of fixed size anchored at the bottom-left corner of its parent, positioned by its center.</summary>
        public static RectTransform PlaceAbsolute(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = center;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Sprite? sprite, Color color, bool raycast = false)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        /// <summary>
        /// A raised pill button in the design board's style (spec 002 FR-005): the face in <paramref name="color"/> over
        /// a darker lower edge, with a bold label in the color's ink. The dark text color gives the cream secondary
        /// button, and the accent gives the green primary one.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, Color color, Action onClick, float fontSize = 48f)
        {
            bool secondary = color == UiTheme.Text || color == UiTheme.SlotLocked;
            Color face = secondary ? UiTheme.Secondary : color;
            Color edge = secondary ? UiTheme.SecondaryEdge : color == UiTheme.Accent ? UiTheme.AccentEdge : Color.Lerp(color, Color.black, 0.28f);
            Image edgeImage = UiKit.Pill(name, parent, edge, raycast: true);
            Image faceImage = UiKit.Pill("Face", edgeImage.transform, face);
            RectTransform faceRect = Stretch(faceImage.rectTransform);
            faceRect.offsetMin = new Vector2(0f, UiKit.Units(Design.DesignTokens.Elevation.RaisedEdge));
            if (color == UiTheme.Accent)
            {
                Image shine = UiKit.Pill("Shine", faceImage.transform, new Color(UiTheme.AccentTop.r, UiTheme.AccentTop.g, UiTheme.AccentTop.b, 0.75f));
                Place(shine.rectTransform, 0.03f, 0.5f, 0.97f, 0.94f);
            }

            var button = edgeImage.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            button.onClick.AddListener(() =>
            {
                GameFeedback.Current?.Play(SoundCue.Click);
                onClick();
            });
            edgeImage.gameObject.AddComponent<PressMotion>();
            Color ink = face.a < 0.5f ? UiTheme.Text : UiTheme.Of(UiTheme.ToRgba(face).Ink);
            TextMeshProUGUI text = CreateText("Label", faceImage.transform, label, fontSize, ink);
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * 0.6f;
            Place(text.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
            return button;
        }

        /// <summary>
        /// A full-screen shade with a centered cream card in the design board's popup style (spec 002 FR-007): rounded,
        /// with a soft shadow, popping in. Returns the card.
        /// </summary>
        public static RectTransform CreateModal(string name, Transform parent, float cardHeight01, out GameObject root)
        {
            Image shade = CreateImage(name, parent, null, UiTheme.PanelShade, raycast: true);
            Stretch(shade.rectTransform);
            root = shade.gameObject;
            Image card = UiKit.Rounded("Card", shade.transform, UiTheme.Panel, 56f, raycast: true);
            UiKit.CardShadow(card);
            card.gameObject.AddComponent<PopMotion>();
            float half = cardHeight01 / 2f;
            Place(card.rectTransform, 0.08f, 0.5f - half, 0.92f, 0.5f + half);
            return card.rectTransform;
        }
    }
}
