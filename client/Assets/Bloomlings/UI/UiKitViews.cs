using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// Lays out children from the element's own box, as the playtest's kit recipes do (spec 005 contracts/look.md §3):
    /// each child gets a box computed from the element's rect in top-down local coordinates (0, 0 at the top-left
    /// corner, y down, canvas units), anchored at the top-left corner with a center pivot. Callbacks run after the boxes
    /// (font sizes, geometry of faces). It re-runs when the element's size changes, when it is enabled, and when a
    /// watched label's text changes (measured layouts). Children of the element only; boxes may reach outside it
    /// (shadows, decorations).
    /// </summary>
    public sealed class BoxLayout : MonoBehaviour
    {
        private readonly List<(RectTransform Target, Func<Box, Box> Place)> _items = new List<(RectTransform, Func<Box, Box>)>();
        private readonly List<Action<Box>> _actions = new List<Action<Box>>();
        private readonly List<(TextMeshProUGUI Label, string Text)> _watched = new List<(TextMeshProUGUI, string)>();
        private bool _applying;

        /// <summary>The layout of a rect, added when it has none yet.</summary>
        public static BoxLayout On(RectTransform rect)
        {
            BoxLayout layout = rect.GetComponent<BoxLayout>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<BoxLayout>();
            }

            return layout;
        }

        /// <summary>The element's box in its own top-down coordinates, or empty before it has a size.</summary>
        public Box Bounds
        {
            get
            {
                Rect rect = ((RectTransform)transform).rect;
                return new Box(0f, 0f, rect.width, rect.height);
            }
        }

        /// <summary>Places <paramref name="target"/> (a child of this element) at the box <paramref name="place"/> gives.</summary>
        public BoxLayout Add(RectTransform target, Func<Box, Box> place)
        {
            _items.Add((target, place));
            Apply();
            return this;
        }

        /// <summary>Runs <paramref name="apply"/> with the element's box after the children are placed.</summary>
        public BoxLayout Then(Action<Box> apply)
        {
            _actions.Add(apply);
            Apply();
            return this;
        }

        /// <summary>
        /// Lays out again whenever <paramref name="label"/>'s text changes (layouts that measure text). Only layouts that
        /// watch a label get the small per-frame check (<see cref="LabelWatch"/>).
        /// </summary>
        public BoxLayout Watch(TextMeshProUGUI label)
        {
            _watched.Add((label, label.text));
            if (GetComponent<LabelWatch>() == null)
            {
                gameObject.AddComponent<LabelWatch>().Layout = this;
            }

            return this;
        }

        /// <summary>Whether a watched label's text changed since the last layout.</summary>
        internal bool TextChanged()
        {
            for (int i = 0; i < _watched.Count; i++)
            {
                string now = _watched[i].Label.text;
                if (!ReferenceEquals(now, _watched[i].Text) && now != _watched[i].Text)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Places a rect at a box of its parent's top-down coordinates (anchored top-left, centered pivot).</summary>
        public static void Place(RectTransform target, Box box)
        {
            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = new Vector2(Mathf.Max(0f, box.Width), Mathf.Max(0f, box.Height));
            target.anchoredPosition = new Vector2(box.CenterX, -box.CenterY);
        }

        /// <summary>Lays the children out now.</summary>
        public void Apply()
        {
            if (_applying)
            {
                return;
            }

            Box bounds = Bounds;
            if (bounds.IsEmpty)
            {
                return;
            }

            _applying = true;
            try
            {
                foreach ((RectTransform target, Func<Box, Box> place) in _items)
                {
                    if (target != null)
                    {
                        Place(target, place(bounds));
                    }
                }

                foreach (Action<Box> action in _actions)
                {
                    action(bounds);
                }

                for (int i = 0; i < _watched.Count; i++)
                {
                    _watched[i] = (_watched[i].Label, _watched[i].Label.text);
                }
            }
            finally
            {
                _applying = false;
            }
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();
    }

    /// <summary>Re-runs a <see cref="BoxLayout"/> after a watched label's text changed (the speed pill's "1×" → "2×").</summary>
    public sealed class LabelWatch : MonoBehaviour
    {
        internal BoxLayout? Layout { get; set; }

        private void LateUpdate()
        {
            if (Layout != null && Layout.TextChanged())
            {
                Layout.Apply();
            }
        }
    }

    /// <summary>How <see cref="PictureFit"/> sizes a picture to its rect.</summary>
    public enum PictureShape
    {
        /// <summary>The rect's own pixel size (planks, frames, stones, the arch, the pedestal).</summary>
        Rect,

        /// <summary>A square of the rect's shorter side, its aspect kept and centered (icons, clusters, rays).</summary>
        Square,

        /// <summary>A square of the rect's width, stretched over the rect (a candy tile sinking into its lip).</summary>
        SquareByWidth,
    }

    /// <summary>
    /// Keeps an image's picture at the rect's pixel size (spec 005 contracts/look.md §2.1, the playtest's
    /// <c>IPainter.Picture</c>): on every size change the rect's size in screen pixels, each side rounded up to a
    /// multiple of 8 (<see cref="UiRaster.Quantize"/>), asks the source for its sprite (cached by key and size). A sliced
    /// picture (planks, frames) gets the pixels-per-unit multiplier that maps its pixels one to one onto the rect's
    /// height, so its ends keep their shape. The fit counts the cached picture it shows
    /// (<see cref="ProceduralSprites.Fit"/>), so the pictures no fit shows any more can be released.
    /// </summary>
    public sealed class PictureFit : MonoBehaviour
    {
        /// <summary>The largest picture side in pixels; larger rects stretch the picture.</summary>
        public const int MaxSide = 1024;

        private Image _image = null!;
        private Func<int, int, Sprite>? _source;
        private bool _sliced;
        private PictureShape _shape;
        private int _width;
        private int _height;
        private string? _shown;

        /// <summary>Fits <paramref name="image"/>'s picture from <paramref name="source"/> (pixel width, height → sprite).</summary>
        public static PictureFit On(Image image, Func<int, int, Sprite> source, bool sliced = false, bool square = false) =>
            On(image, source, sliced, square ? PictureShape.Square : PictureShape.Rect);

        /// <summary>Fits <paramref name="image"/>'s picture from <paramref name="source"/> in the given shape.</summary>
        public static PictureFit On(Image image, Func<int, int, Sprite> source, bool sliced, PictureShape shape)
        {
            PictureFit fit = image.GetComponent<PictureFit>();
            if (fit == null)
            {
                fit = image.gameObject.AddComponent<PictureFit>();
            }

            fit._image = image;
            fit._sliced = sliced;
            fit._shape = shape;
            image.preserveAspect = shape == PictureShape.Square;
            fit.SetSource(source);
            return fit;
        }

        /// <summary>Changes what the picture shows (a tile's variant or state) and renders it at the current size.</summary>
        public void SetSource(Func<int, int, Sprite> source)
        {
            _source = source;
            _width = 0;
            _height = 0;
            Apply();
        }

        /// <summary>The picture size in pixels for a rect of <paramref name="width"/> × <paramref name="height"/> canvas units.</summary>
        public static (int Width, int Height) PixelSize(float width, float height, PictureShape shape, float pixelsPerUnit)
        {
            float w = width * pixelsPerUnit;
            float h = height * pixelsPerUnit;
            if (shape == PictureShape.Square)
            {
                w = h = Math.Min(w, h);
            }
            else if (shape == PictureShape.SquareByWidth)
            {
                h = w;
            }

            float shrink = Math.Min(1f, MaxSide / Math.Max(1f, Math.Max(w, h)));
            return (Math.Min(MaxSide, UiRaster.Quantize(w * shrink)), Math.Min(MaxSide, UiRaster.Quantize(h * shrink)));
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void OnDestroy()
        {
            ProceduralSprites.Unshow(_shown);
            _shown = null;
        }

        private void Apply()
        {
            if (_image == null || _source == null)
            {
                return;
            }

            Rect rect = _image.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            (int w, int h) = PixelSize(rect.width, rect.height, _shape, UiKit.PixelsPerUnit);
            if (w != _width || h != _height || _image.sprite == null)
            {
                _width = w;
                _height = h;
                _image.sprite = ProceduralSprites.Fit(_source, w, h, ref _shown);
                _image.type = _sliced ? Image.Type.Sliced : Image.Type.Simple;
            }

            if (_sliced)
            {
                _image.pixelsPerUnitMultiplier = h / Mathf.Max(0.01f, rect.height);
            }
        }
    }

    /// <summary>
    /// A rounded rectangle at any size (the Unity kit's <c>FillRound</c>, <c>StrokeRound</c> and soft edges): the image
    /// draws <see cref="ProceduralSprites.Round"/> sliced, with the pixels-per-unit multiplier that turns the sprite's
    /// border into the corner radius in canvas units. The radius comes from the rect's own top-down box (null: a pill,
    /// half the shorter side) and is never more than half the shorter side. A ring or a fading band takes its width from
    /// <see cref="Band"/>.
    /// </summary>
    public sealed class RoundShape : MonoBehaviour
    {
        private Image _image = null!;

        /// <summary>The corner radius in canvas units for the rect's box; null makes a pill.</summary>
        public Func<Box, float>? Radius { get; set; }

        /// <summary>Solid, a ring along the edge, or a band fading inward.</summary>
        public RoundFill Fill { get; private set; }

        /// <summary>The ring's or band's width in canvas units for the rect's box.</summary>
        public Func<Box, float>? Band { get; set; }

        public static RoundShape On(Image image, Func<Box, float>? radius, RoundFill fill = RoundFill.Solid, Func<Box, float>? band = null)
        {
            RoundShape shape = image.GetComponent<RoundShape>();
            if (shape == null)
            {
                shape = image.gameObject.AddComponent<RoundShape>();
            }

            shape._image = image;
            shape.Radius = radius;
            shape.Fill = fill;
            shape.Band = band;
            image.type = Image.Type.Sliced;
            shape.Apply();
            return shape;
        }

        /// <summary>
        /// The sliced sprite's multiplier and band share for a rect: the radius clamped to half the shorter side, and the
        /// larger of radius and band spanning the sprite's border.
        /// </summary>
        public static (float Multiplier, float BandShare) Fit(float width, float height, float radius, RoundFill fill, float band)
        {
            float half = Math.Max(0.01f, Math.Min(width, height) / 2f);
            float r = Math.Max(0.01f, Math.Min(radius, half));
            if (fill == RoundFill.Solid)
            {
                return (PicturePixels.RoundUnit / r, 0f);
            }

            float b = Math.Max(0.01f, Math.Min(band, half));
            return (PicturePixels.RoundUnit / Math.Max(r, b), b / r);
        }

        /// <summary>Fits the shape to the rect now (after its radius or band changed).</summary>
        public void Apply()
        {
            if (_image == null)
            {
                return;
            }

            Rect rect = _image.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var box = new Box(0f, 0f, rect.width, rect.height);
            float radius = Radius != null ? Radius(box) : float.MaxValue;
            float band = Band != null ? Band(box) : 1f;
            (float multiplier, float share) = Fit(rect.width, rect.height, radius, Fill, band);
            Sprite sprite = ProceduralSprites.Round(Fill, share);
            if (_image.sprite != sprite)
            {
                _image.sprite = sprite;
            }

            _image.type = Image.Type.Sliced;
            _image.pixelsPerUnitMultiplier = multiplier;
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();
    }

    /// <summary>
    /// Keeps a <see cref="Shadow"/> or <see cref="Outline"/> effect proportional to its graphic's height: a glyph's line
    /// under it on colored faces, its thin halo on cream (spec 005 §3.3: the shape grown by 0.06 shape units).
    /// </summary>
    public sealed class EffectFit : MonoBehaviour
    {
        private Shadow _effect = null!;
        private Vector2 _share;

        public static EffectFit On(Shadow effect, Vector2 share)
        {
            var fit = effect.gameObject.AddComponent<EffectFit>();
            fit._effect = effect;
            fit._share = share;
            fit.Apply();
            return fit;
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_effect == null)
            {
                return;
            }

            Rect rect = ((RectTransform)transform).rect;
            float size = Mathf.Min(rect.width, rect.height);
            if (size > 0f)
            {
                _effect.effectDistance = new Vector2(_share.x * size, _share.y * size);
            }
        }
    }

    /// <summary>Passes the presses of a bigger touch target to a garden face inside it (the Petals pill's green "+").</summary>
    public sealed class PressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public GardenButton? Target { get; set; }

        public void OnPointerDown(PointerEventData e) => Target?.OnPointerDown(e);

        public void OnPointerUp(PointerEventData e) => Target?.OnPointerUp(e);

        public void OnPointerExit(PointerEventData e) => Target?.OnPointerExit(e);
    }

    /// <summary>
    /// Lets taps through a raycasting image (and its children) where another rect lies: the win card's shade over the
    /// gameplay top bar, so Pause and the speed pill stay usable while the card shows (spec 005 FR-002).
    /// </summary>
    public sealed class RaycastHole : MonoBehaviour, ICanvasRaycastFilter
    {
        /// <summary>The rect the taps go through to; null or inactive: the image takes every tap.</summary>
        public RectTransform? Hole { get; set; }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
            Hole == null || !Hole.gameObject.activeInHierarchy || !RectTransformUtility.RectangleContainsScreenPoint(Hole, screenPoint, eventCamera);
    }

    /// <summary>Text measured by the kit's layouts (the playtest's <c>MeasureText</c> and its shrink to a width).</summary>
    public static class KitText
    {
        /// <summary>
        /// Centers a label on (<paramref name="cx"/>, <paramref name="cy"/>) of its parent's top-down box at
        /// <paramref name="fontSize"/> canvas units, at most <paramref name="maxWidth"/> wide (0: no limit): it shrinks to
        /// fit down to the style's minimum at that size, as the playtest's text does. Returns the width the text takes.
        /// </summary>
        public static float Place(TextMeshProUGUI label, TypeStyle style, float cx, float cy, float fontSize, float maxWidth = 0f)
        {
            float size = Mathf.Max(1f, fontSize);
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSize = size;
            label.fontSizeMin = Mathf.Min(size, size * style.Min / Mathf.Max(1f, style.Size));
            float natural = Measure(label, size);
            float width = maxWidth > 0f ? (natural > 0f ? Mathf.Min(natural, maxWidth) : maxWidth) : (natural > 0f ? natural : size * 4f);
            BoxLayout.Place(label.rectTransform, Box.FromCenter(cx, cy, width + (size * 0.1f), size * 1.6f));
            return width;
        }

        /// <summary>The width of the label's text at <paramref name="fontSize"/> (0 when the text engine cannot tell yet).</summary>
        public static float Measure(TextMeshProUGUI label, float fontSize) => Measure(label, fontSize, label.text);

        /// <summary>The width of <paramref name="text"/> in the label's font at <paramref name="fontSize"/> (0 when the text engine cannot tell yet).</summary>
        public static float Measure(TextMeshProUGUI label, float fontSize, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            bool auto = label.enableAutoSizing;
            float size = label.fontSize;
            label.enableAutoSizing = false;
            label.fontSize = fontSize;
            float width = label.GetPreferredValues(text).x;
            label.enableAutoSizing = auto;
            label.fontSize = size;
            return float.IsNaN(width) || width < 0f ? 0f : width;
        }
    }
}
