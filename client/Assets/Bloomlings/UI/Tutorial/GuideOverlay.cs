using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Tutorial
{
    /// <summary>
    /// The guided spotlight over the gameplay (spec 005 FR-035, <see cref="Spotlight"/>, <see cref="GuideTour"/>; the
    /// playtest's <c>GuidePainter</c> is its twin): the screen dimmed by the scrim picture but for the step's lit holes, a
    /// glow ring breathing round the first one, the message on a parchment bubble with a tail pointing at it, and on a
    /// forced step the pointing hand. Taps: a step that is not forced goes on with a tap anywhere; a forced step lets the
    /// taps inside its holes through to the game (the pod, the booster, Return's plate, Bloom Burst's tiles, through this
    /// raycast filter) and takes every other tap.
    /// </summary>
    public sealed class GuideOverlay : MonoBehaviour, ICanvasRaycastFilter
    {
        private readonly List<Box> _holes = new List<Box>();
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _root = null!;
        private Image _scrim = null!;
        private Image _ring = null!;
        private Image _tail = null!;
        private RectTransform _bubble = null!;
        private RectTransform _iconSlot = null!;
        private TextMeshProUGUI _strong = null!;
        private TextMeshProUGUI _soft = null!;
        private TextMeshProUGUI _caption = null!;
        private TextMeshProUGUI _probe = null!;
        private Image _hand = null!;
        private Image _handLine = null!;
        private CanvasGroup _group = null!;
        private GuideStep? _step;
        private Func<List<Box>> _source = () => new List<Box>();
        private Action<RectTransform>? _icon;
        private Action _onTap = () => { };
        private SpotlightLayout _layout;
        private List<string> _strongLines = new List<string>();
        private readonly List<string> _softLines = new List<string>();
        private string? _scrimKey;
        private float _time;

        public bool IsShowing => _root.gameObject.activeSelf;

        public GuideStep? Step => IsShowing ? _step : null;

        public static GuideOverlay Create(Transform parent)
        {
            Image scrim = UiFactory.CreateImage("GuideOverlay", parent, null, Color.white, raycast: true);
            UiFactory.Stretch(scrim.rectTransform);
            var overlay = scrim.gameObject.AddComponent<GuideOverlay>();
            overlay._root = scrim.rectTransform;
            overlay._scrim = scrim;
            overlay._group = scrim.gameObject.AddComponent<CanvasGroup>();
            Button tap = scrim.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => overlay._onTap());

            overlay._ring = UiKit.RoundRing("Ring", scrim.transform, UiTheme.Of(C.GardenGlow), b => overlay._layout.Radius / Mathf.Max(0.0001f, UiKit.PixelsPerUnit), _ => UiKit.Units(6f));
            overlay._tail = UiKit.RoundRect("Tail", scrim.transform, UiTheme.Of(C.ParchmentBottom), _ => UiKit.Units(6f));
            Image bubble = UiKit.Paper("Bubble", scrim.transform, 34f, DesignTokens.Garden.OutlineWidth, 6f, raycast: false);
            overlay._bubble = bubble.rectTransform;
            overlay._iconSlot = UiFactory.CreateRect("Icon", bubble.transform);
            overlay._strong = UiKit.Label("Message", bubble.transform, string.Empty, T.ButtonSecondary, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            overlay._soft = UiKit.Label("Detail", bubble.transform, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            overlay._caption = UiKit.Label("Continue", bubble.transform, Loc.T("demo.tap_continue"), T.Caption, UiTheme.Of(C.InkBrownSoft));
            overlay._probe = UiKit.Label("Probe", bubble.transform, string.Empty, T.ButtonSecondary, Color.clear);
            overlay._handLine = UiFactory.CreateImage("HandLine", scrim.transform, ProceduralSprites.Pointer, UiTheme.Of(C.InkBrown));
            overlay._handLine.preserveAspect = true;
            overlay._hand = UiFactory.CreateImage("Hand", scrim.transform, ProceduralSprites.Pointer, Color.white);
            overlay._hand.preserveAspect = true;
            scrim.gameObject.SetActive(false);
            return overlay;
        }

        /// <summary>
        /// Shows <paramref name="step"/> lighting <paramref name="holes"/> (screen pixels, top-down, the first one ringed;
        /// asked again every frame, so the lit places follow the layout). <paramref name="icon"/> fills the bubble's icon
        /// (a booster, the tile that blocks the way); <paramref name="onTap"/> runs on a tap anywhere when the step is not
        /// forced.
        /// </summary>
        public void Show(GuideStep step, Func<List<Box>> holes, Action<RectTransform>? icon, Action onTap)
        {
            _step = step;
            _source = holes;
            _icon = icon;
            _onTap = step.Forced ? () => { } : onTap;
            _scrimKey = null;
            _time = 0f;
            _group.alpha = 0f;

            // The message, balanced on its lines once per step.
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float width = Spotlight.TextWidth(w, h, insets, icon != null) / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            _strongLines = UiKit.BalancedLines(_probe, Loc.T(step.MessageKeys[0]), UiKit.Units(T.ButtonSecondary.Size), width);
            _softLines.Clear();
            for (int i = 1; i < step.MessageKeys.Count; i++)
            {
                _softLines.AddRange(UiKit.BalancedLines(_probe, Loc.T(step.MessageKeys[i]), UiKit.Units(T.Body.Size), width));
            }

            _strong.text = string.Join("\n", _strongLines);
            _soft.text = string.Join("\n", _softLines);
            foreach (Transform child in _iconSlot)
            {
                Destroy(child.gameObject);
            }

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _icon?.Invoke(_iconSlot);
            _iconSlot.gameObject.SetActive(_icon != null);
            Lay();
        }

        public void Hide()
        {
            _step = null;
            _root.gameObject.SetActive(false);
        }

        /// <summary>A rect's box on the screen, in pixels, top-down (the canvas is screen space overlay).</summary>
        public Box ScreenOf(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            float h = Screen.height;
            float l = float.MaxValue, r = float.MinValue, b = float.MaxValue, t = float.MinValue;
            foreach (Vector3 c in _corners)
            {
                l = Mathf.Min(l, c.x);
                r = Mathf.Max(r, c.x);
                b = Mathf.Min(b, c.y);
                t = Mathf.Max(t, c.y);
            }

            // A turned rect (a side entry's arch) gives its turned bounds.
            return new Box(l, h - t, r, h - b);
        }

        /// <summary>Forced: taps inside a lit hole go through to the game; every other tap stops here.</summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_step == null || !_step.Forced)
            {
                return true;
            }

            float x = screenPoint.x;
            float y = Screen.height - screenPoint.y;
            foreach (Box hole in _holes)
            {
                if (hole.Contains(x, y))
                {
                    return false;
                }
            }

            return true;
        }

        private void Lay()
        {
            GuideStep step = _step!;
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            _holes.Clear();
            List<Box> lit = _source();
            for (int i = 0; i < lit.Count; i++)
            {
                _holes.Add(step.Kind == GuideKind.Blocked && i > 0 ? lit[i].Inset(-2f * u) : Spotlight.HoleAround(lit[i], u));
            }

            if (_holes.Count == 0)
            {
                _group.alpha = 0f;
                return;
            }

            // The scrim picture, rendered once per set of holes at a quarter of the screen.
            float radius = step.Kind == GuideKind.Blocked ? Spotlight.CellRadiusUnits : Spotlight.RadiusUnits;
            string key = Spotlight.ScrimKey(_holes, w, h);
            if (key != _scrimKey)
            {
                _scrimKey = key;
                var holes = new List<Box>(_holes);
                _scrim.sprite = ProceduralSprites.Picture(key, Spotlight.RasterSize(w), Spotlight.RasterSize(h), (pw, ph) => Spotlight.Scrim(holes, w, h, pw, ph, radius));
                _scrim.type = Image.Type.Simple;
            }

            // The bubble round the message.
            bool hasIcon = _icon != null;
            List<string> strong = _strongLines;
            List<string> soft = _softLines;
            _layout = Spotlight.Layout(w, h, insets, Spotlight.Union(_holes), strong.Count + soft.Count, hasIcon, !step.Forced, step.Forced);
            _soft.gameObject.SetActive(soft.Count > 0);
            _caption.gameObject.SetActive(!step.Forced);

            Box b = _layout.Bubble;
            UiKit.PlaceScreen(_bubble, b);
            float pad = Spotlight.BubblePadUnits * u;
            float iconSide = Spotlight.IconUnits * u;
            float textLeft = hasIcon ? b.Left + pad + iconSide + pad : b.Left + pad;
            float lineH = Spotlight.LineUnits * u;
            float captionH = step.Forced ? 0f : Spotlight.CaptionUnits * u;
            float top = b.CenterY - (((strong.Count + soft.Count) * lineH) + captionH) / 2f;
            UiKit.PlaceBox(_iconSlot, new Box(b.Left + pad, b.CenterY - (iconSide / 2f), b.Left + pad + iconSide, b.CenterY + (iconSide / 2f)), b);
            UiKit.PlaceBox((RectTransform)_strong.transform, new Box(textLeft, top, b.Right - pad, top + (strong.Count * lineH)), b);
            top += strong.Count * lineH;
            UiKit.PlaceBox((RectTransform)_soft.transform, new Box(textLeft, top, b.Right - pad, top + Mathf.Max(1f, soft.Count * lineH)), b);
            top += soft.Count * lineH;
            UiKit.PlaceBox((RectTransform)_caption.transform, new Box(textLeft, top, b.Right - pad, top + Mathf.Max(1f, captionH)), b);

            // The tail: a turned square half under the bubble, at the edge facing the hole.
            float tail = Spotlight.TailUnits * u * 1.3f;
            float edge = _layout.TailDown ? b.Bottom : b.Top;
            UiKit.PlaceScreen(_tail.rectTransform, Box.FromCenter(_layout.TailX, edge, tail, tail));
            _tail.color = UiTheme.Of(_layout.TailDown ? C.ParchmentBottom : C.ParchmentTop);
            _tail.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
            _tail.transform.SetSiblingIndex(_bubble.GetSiblingIndex());
            _hand.gameObject.SetActive(step.Forced);
            _handLine.gameObject.SetActive(step.Forced);
        }

        private void Update()
        {
            if (!IsShowing || _step == null)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            Lay();
            if (_holes.Count == 0)
            {
                return;
            }

            _group.alpha = Mathf.Clamp01(_time / 0.25f);
            (float w, float h, Insets _) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            float pulse = Spotlight.Pulse(_time);
            float grow = (3f + (7f * pulse)) * u;
            UiKit.PlaceScreen(_ring.rectTransform, _holes[0].Inset(-grow));
            _ring.color = UiTheme.Of(C.GardenGlow.WithAlpha(0.95f - (0.45f * pulse)));
            if (_step.Forced)
            {
                Box hand = Spotlight.HandAt(_layout, _time);
                UiKit.PlaceScreen(_handLine.rectTransform, hand.Inset(-5f * u));
                UiKit.PlaceScreen(_hand.rectTransform, hand);
            }
        }

        /// <summary>The bubble's icon for a booster's steps: its icon on a cream face.</summary>
        public static Action<RectTransform> BoosterIcon(string boosterId) => slot =>
        {
            GardenButton face = UiKit.IconFace("Face", slot, GardenLook.White, b => b.Height * 0.26f);
            UiFactory.Stretch((RectTransform)face.transform);
            Image icon = UiKit.BoosterIcon("Booster", face.Content, boosterId);
            UiFactory.Place(icon.rectTransform, 0.15f, 0.15f, 0.85f, 0.85f);
        };

        /// <summary>The bubble's icon for the blocked entry: the tile that blocks the way.</summary>
        public static Action<RectTransform> TileIcon(VariantId variant) => slot =>
        {
            CandyTileView tile = UiKit.CandyTile("Tile", slot, variant, TileStyle.Sticker);
            UiFactory.Stretch((RectTransform)tile.transform);
        };
    }
}
