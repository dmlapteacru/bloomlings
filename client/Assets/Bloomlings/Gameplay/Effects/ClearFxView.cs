using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Effects
{
    /// <summary>
    /// Draws one layer of the clearing style's items (spec 005 FR-038; the kit's <see cref="ClearLook"/>, the same list
    /// the playtest draws) with pooled Bloomling figures (the walkers, in their family's outfit) and images: candy tiles,
    /// circles, rings, rounded boxes and the shape library's shapes. Each item is placed by its center, turned and
    /// squashed about it; the items keep the list's order. <see cref="Map"/> turns the kit's cell units into this
    /// layer's coordinates (from its bottom left), <see cref="Cell"/> is a cell's size in them.
    /// </summary>
    public sealed class ClearFxView : MonoBehaviour
    {
        private readonly List<Image> _images = new List<Image>();
        private readonly List<(BloomlingFigure Figure, CanvasGroup Group, VariantId? Shown)> _figures = new List<(BloomlingFigure, CanvasGroup, VariantId?)>();
        private RectTransform _layer = null!;

        /// <summary>A point in cell units (y down) to this layer's coordinates from its bottom left.</summary>
        public Func<float, float, Vector2> Map { get; set; } = (x, y) => new Vector2(x, -y);

        /// <summary>A cell's size in this layer's units.</summary>
        public float Cell { get; set; } = 1f;

        /// <summary>What each family wears (the Wardrobe, FR-063); presentation only.</summary>
        public Func<Family, Outfit>? Outfits { get; set; }

        public static ClearFxView Create(Transform parent, string name)
        {
            RectTransform rect = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = rect.gameObject.AddComponent<ClearFxView>();
            view._layer = rect;
            return view;
        }

        /// <summary>Draws the items of <paramref name="layer"/>, hiding the pooled pieces it does not need.</summary>
        public void Render(IReadOnlyList<FxItem> items, FxLayer layer)
        {
            int images = 0;
            int figures = 0;
            int order = 0;
            foreach (FxItem item in items)
            {
                if (item.Layer != layer || Mathf.Abs(item.Sx) < 0.001f || Mathf.Abs(item.Sy) < 0.001f)
                {
                    continue;
                }

                Vector2 center = Map(item.X, item.Y);
                if (item.Kind == FxKind.Character)
                {
                    RectTransform figure = Figure(figures++, item.Variant, item.Alpha);
                    Place(figure, center, item.W * Cell, item.H * Cell, item);
                    figure.SetSiblingIndex(order++);
                    continue;
                }

                Image image = ImageAt(images++);
                float margin = item.Kind == FxKind.Tile || item.Kind == FxKind.Shape ? 1f : ShapeRaster.Margin;
                float w = item.W * Cell;
                float h = item.H * Cell;
                (Sprite sprite, float side) = SpriteOf(item);
                image.sprite = sprite;
                Color color = UiTheme.Of(item.Kind == FxKind.Tile ? Rgba.White : item.Color);
                image.color = new Color(color.r, color.g, color.b, color.a * item.Alpha);
                if (side > 0f)
                {
                    // A rounded box drawn in a square picture of its longer side, so its corners keep their round.
                    w = h = side * Cell;
                }

                Place(image.rectTransform, center, w * margin, h * margin, item);
                image.rectTransform.SetSiblingIndex(order++);
            }

            for (int i = images; i < _images.Count; i++)
            {
                _images[i].gameObject.SetActive(false);
            }

            for (int i = figures; i < _figures.Count; i++)
            {
                _figures[i].Figure.Rect.gameObject.SetActive(false);
            }
        }

        /// <summary>Hides everything (a restart, leaving the level).</summary>
        public void Clear() => Render(Array.Empty<FxItem>(), FxLayer.Board);

        private static void Place(RectTransform rect, Vector2 center, float w, float h, FxItem item)
        {
            UiFactory.PlaceAbsolute(rect, center, new Vector2(w, h));
            rect.localScale = new Vector3(item.Sx, item.Sy, 1f);
            rect.localEulerAngles = new Vector3(0f, 0f, -item.Turn);
        }

        // The sprite for an item and, for a rounded box, the side of the square picture it is drawn in (cell units).
        private static (Sprite Sprite, float Side) SpriteOf(FxItem item)
        {
            switch (item.Kind)
            {
                case FxKind.Tile:
                    return (ProceduralSprites.CandyTile(item.Variant, TileStyle.Board), 0f);
                case FxKind.Ring:
                    return (Ring(item.Line / Math.Max(0.001f, item.W / 2f)), 0f);
                case FxKind.Round:
                case FxKind.RoundRing:
                {
                    float side = Math.Max(item.W, item.H);
                    return (RoundBox(item.W / side, item.H / side, item.Radius / (side / 2f), item.Kind == FxKind.RoundRing ? item.Line / (side / 2f) : 0f), side);
                }

                case FxKind.Shape:
                    return (ProceduralSprites.Shape(item.Shape ?? ShapeLibrary.Fallback), 0f);
                default:
                    return (ProceduralSprites.Circle, 0f);
            }
        }

        // A ring of radius 1 whose line is <paramref name="line"/> of the radius (cached by a 2% step).
        private static Sprite Ring(float line)
        {
            float t = Mathf.Clamp((float)Math.Round(line * 50f) / 50f, 0.02f, 1f);
            return ProceduralSprites.Composite("fx.ring." + t.ToString("0.00", CultureInfo.InvariantCulture), (x, y) => Math.Abs((float)Math.Sqrt((x * x) + (y * y)) - (1f - (t / 2f))) - (t / 2f), 64);
        }

        // A rounded box of half sizes (w, h) at most 1, corner r and, for an outline, line l (cached by a 5% step).
        private static Sprite RoundBox(float w, float h, float r, float l)
        {
            float qw = (float)Math.Round(w * 20f) / 20f;
            float qh = (float)Math.Round(h * 20f) / 20f;
            float qr = (float)Math.Round(Mathf.Min(r, Mathf.Min(qw, qh)) * 20f) / 20f;
            float ql = (float)Math.Round(l * 50f) / 50f;
            string key = string.Format(CultureInfo.InvariantCulture, "fx.round.{0:0.00}.{1:0.00}.{2:0.00}.{3:0.00}", qw, qh, qr, ql);
            return ProceduralSprites.Composite(key, (x, y) =>
            {
                float qx = Math.Abs(x) - qw + qr;
                float qy = Math.Abs(y) - qh + qr;
                float outside = (float)Math.Sqrt((Math.Max(qx, 0f) * Math.Max(qx, 0f)) + (Math.Max(qy, 0f) * Math.Max(qy, 0f)));
                float d = outside + Math.Min(Math.Max(qx, qy), 0f) - qr;
                return ql > 0f ? Math.Abs(d + (ql / 2f)) - (ql / 2f) : d;
            }, 64);
        }

        private Image ImageAt(int index)
        {
            if (index >= _images.Count)
            {
                Image image = UiFactory.CreateImage("Fx", _layer, ProceduralSprites.Circle, Color.white);
                image.raycastTarget = false;
                _images.Add(image);
            }

            Image found = _images[index];
            found.gameObject.SetActive(true);
            return found;
        }

        private RectTransform Figure(int index, VariantId variant, float alpha)
        {
            if (index >= _figures.Count)
            {
                BloomlingFigure created = BloomlingFigure.Create("Walker", _layer);
                created.Body.raycastTarget = false;
                CanvasGroup group = created.Rect.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                _figures.Add((created, group, null));
            }

            (BloomlingFigure figure, CanvasGroup canvas, VariantId? shown) = _figures[index];
            if (!shown.HasValue || !shown.Value.Equals(variant))
            {
                Family family = VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? info.Family : Family.Sprig;
                figure.ShowCharacter(variant, Outfits?.Invoke(family));
                _figures[index] = (figure, canvas, variant);
            }

            canvas.alpha = alpha;
            figure.Rect.gameObject.SetActive(true);
            return figure.Rect;
        }
    }
}
