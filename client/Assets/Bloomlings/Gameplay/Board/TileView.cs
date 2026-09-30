using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// One board cell. A target shows a raised rounded tile in its variant color (the design board's frames 7–9: a
    /// lighter top and a darker lower edge) with the variant icon in a contrasting ink (never a character face, spec 002
    /// FR-011); open cells hide the tile so the finished picture shows through; stones show a gray
    /// block. A corner badge with the next variant's color and icon previews the next hidden layer (FR-036). A key sits
    /// in the opposite corner without hiding the tile's icon or color (FR-033). A cell that counts toward a special's
    /// condition carries a thin outline in the special's color (FR-037, FR-038). Changes animate: a cleared tile shrinks
    /// away, a revealed layer or mystery tile flips in, and a tile that Bloom Burst can target breathes.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        private Image _frame = null!;
        private Image _fill = null!;
        private Image _shine = null!;
        private Image _icon = null!;
        private Image _peek = null!;
        private Image _peekIcon = null!;
        private Image _keyMark = null!;
        private Image _counted = null!;
        private VariantVisualCatalog? _visuals;
        private Coroutine? _animation;
        private bool _targetable;
        private float _time;

        /// <summary>Raised when the tile is tapped while it is tappable (Bloom Burst picks its variant, T120).</summary>
        public event System.Action<CellPos>? Tapped;

        public CellPos Cell { get; private set; }

        /// <summary>The variant currently shown, or null when the tile is hidden.</summary>
        public VariantId? Shown { get; private set; }

        public static TileView Create(Transform parent, CellPos cell, VariantVisualCatalog? visuals)
        {
            Image frame = UiFactory.CreateImage($"Tile {cell.X},{cell.Y}", parent, ProceduralSprites.RoundedSquare, UiTheme.TileFrame);
            var view = frame.gameObject.AddComponent<TileView>();
            view._frame = frame;
            view._visuals = visuals;
            view.Cell = cell;
            // The raised look: the frame shows as the darker lower edge, the fill sits on it with a lighter top half.
            view._fill = UiFactory.CreateImage("Fill", frame.transform, ProceduralSprites.RoundedSquare, Color.white);
            UiFactory.Place(view._fill.rectTransform, 0f, 0.09f, 1f, 1f);
            view._shine = UiFactory.CreateImage("Shine", view._fill.transform, ProceduralSprites.RoundedSquare, new Color(1f, 1f, 1f, 0f));
            UiFactory.Place(view._shine.rectTransform, 0.03f, 0.5f, 0.97f, 0.97f);
            view._icon = UiFactory.CreateImage("Icon", frame.transform, null, new Color(1f, 1f, 1f, 0.92f));
            UiFactory.Place(view._icon.rectTransform, 0.2f, 0.25f, 0.8f, 0.85f);
            view._icon.preserveAspect = true;
            view._peek = UiFactory.CreateImage("NextLayer", frame.transform, ProceduralSprites.Circle, Color.white);
            UiFactory.Place(view._peek.rectTransform, 0.62f, 0.62f, 1f, 1f);
            view._peekIcon = UiFactory.CreateImage("NextIcon", view._peek.transform, null, new Color(1f, 1f, 1f, 0.95f));
            view._peekIcon.preserveAspect = true;
            UiFactory.Place(view._peekIcon.rectTransform, 0.18f, 0.18f, 0.82f, 0.82f);
            view._keyMark = UiFactory.CreateImage("Key", frame.transform, ProceduralSprites.Key, UiTheme.EntryMarker);
            UiFactory.Place(view._keyMark.rectTransform, 0.02f, 0.62f, 0.4f, 0.98f);
            view._counted = UiFactory.CreateImage("Counted", frame.transform, ProceduralSprites.RoundedSquare, Color.white);
            view._counted.fillCenter = false;
            UiFactory.Place(view._counted.rectTransform, -0.04f, -0.04f, 1.04f, 1.04f);
            view._counted.enabled = false;
            Button button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => view.Tapped?.Invoke(view.Cell));
            frame.raycastTarget = false;
            return view;
        }

        /// <param name="animate">Flip in (a revealed layer or mystery tile); false on build and restart.</param>
        public void ShowTarget(VariantId? visible, VariantId? next, string? keyId, bool animate = false)
        {
            StopAnimation();
            gameObject.SetActive(true);
            _frame.enabled = true;
            if (visible.HasValue)
            {
                VariantVisual visual = Visual(visible.Value);
                _fill.sprite = visual.Tile ?? ProceduralSprites.RoundedSquare;
                _fill.color = visual.Color;
                Rgba color = UiTheme.ToRgba(visual.Color);
                _shine.color = WithAlpha(UiTheme.Of(DesignTokens.TileTop(color)), visual.Tile == null ? 0.7f : 0f);
                _icon.sprite = visual.Icon;
                _icon.color = WithAlpha(visual.Ink, 0.92f);
                _icon.enabled = true;
                _frame.color = UiTheme.Of(DesignTokens.TileEdge(color));
            }
            else
            {
                // A hidden mystery tile (FR-039): neutral tile with a question mark.
                Color mystery = UiTheme.Of(DesignTokens.Colors.TileMystery);
                _fill.sprite = ProceduralSprites.RoundedSquare;
                _fill.color = mystery;
                _shine.color = new Color(1f, 1f, 1f, 0.18f);
                _icon.sprite = ProceduralSprites.Question;
                _icon.color = new Color(1f, 1f, 1f, 0.92f);
                _icon.enabled = true;
                _frame.color = UiTheme.Dark(mystery);
            }

            // The layer peek (FR-036): a corner badge with the next layer's color and icon.
            _peek.enabled = next.HasValue;
            _peekIcon.enabled = next.HasValue;
            if (next.HasValue)
            {
                VariantVisual peek = Visual(next.Value);
                _peek.color = peek.Color;
                _peekIcon.sprite = peek.Icon;
                _peekIcon.color = WithAlpha(peek.Ink, 0.95f);
            }

            _keyMark.enabled = keyId != null;
            Shown = visible;
            if (animate && isActiveAndEnabled)
            {
                _animation = StartCoroutine(FlipIn());
            }
        }

        public void ShowStone()
        {
            StopAnimation();
            gameObject.SetActive(true);
            // A garden stone (frame 9): the stone shape, lit on top, sitting on its own shadow.
            _frame.color = Color.clear;
            _fill.sprite = ProceduralSprites.Shape("tile.stone");
            _fill.color = UiTheme.StoneColor;
            _shine.color = new Color(1f, 1f, 1f, 0f);
            _icon.enabled = false;
            _peek.enabled = false;
            _peekIcon.enabled = false;
            _keyMark.enabled = false;
            Shown = null;
        }

        /// <summary>The key mark's position, where a collected key starts its flight (T107).</summary>
        public Vector3 KeyPosition => _keyMark.transform.position;

        public bool HasKey => _keyMark.enabled;

        /// <summary>Tiles take taps only while a booster waits for a target; such tiles breathe gently.</summary>
        public void SetTappable(bool tappable)
        {
            _frame.raycastTarget = tappable;
            _targetable = tappable;
            _time = 0f;
            if (!tappable && _animation == null)
            {
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>Outlines the cell in a special's color while it counts toward that special; null removes it.</summary>
        public void SetCounted(Color? color)
        {
            _counted.enabled = color.HasValue;
            if (color.HasValue)
            {
                _counted.color = color.Value;
            }
        }

        public void HideKey() => _keyMark.enabled = false;

        /// <summary>Draws attention to this tile's key (a locked pod was tapped, T109).</summary>
        public void FlashKey()
        {
            if (_keyMark.enabled && isActiveAndEnabled)
            {
                StartCoroutine(Flash());
            }
        }

        private System.Collections.IEnumerator Flash()
        {
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                _keyMark.transform.localScale = Vector3.one * (1f + (0.35f * Mathf.Abs(Mathf.Sin(t * 10f))));
                yield return null;
            }

            _keyMark.transform.localScale = Vector3.one;
        }

        /// <summary>Open ground: the tile disappears and the finished picture shows through.</summary>
        /// <param name="animate">Shrink away (a Bloomling restored it); false on build and restart.</param>
        public void ShowOpen(bool animate = false)
        {
            StopAnimation();
            Shown = null;
            _counted.enabled = false;
            if (animate && isActiveAndEnabled)
            {
                _animation = StartCoroutine(ShrinkAway());
                return;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_targetable && _animation == null)
            {
                _time += Time.unscaledDeltaTime;
                transform.localScale = Vector3.one * (1f + (0.06f * Mathf.Sin(_time * 7f)));
            }
        }

        private System.Collections.IEnumerator ShrinkAway()
        {
            _targetable = false;
            _frame.raycastTarget = false;
            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.18f;
                transform.localScale = Vector3.one * (1f - (0.8f * k * k));
                transform.localEulerAngles = new Vector3(0f, 0f, 25f * k);
                yield return null;
            }

            transform.localScale = Vector3.one;
            transform.localEulerAngles = Vector3.zero;
            _animation = null;
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FlipIn()
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.2f;
                transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(k * Mathf.PI)), 1f, 1f);
                yield return null;
            }

            transform.localScale = Vector3.one;
            _animation = null;
        }

        private void StopAnimation()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            transform.localScale = Vector3.one;
            transform.localEulerAngles = Vector3.zero;
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private VariantVisual Visual(VariantId id) => _visuals != null ? _visuals.Get(id) : VariantVisualCatalog.Default(id);
    }
}
