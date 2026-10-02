using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// A finished picture in full color (spec 005 research D14, contracts/look.md §4.4; the playtest's
    /// <c>BoardPainter.Picture</c>): each cell a flat candy tile of its role's variant (<see cref="TileStyle.Flat"/>: no
    /// lip, a small gloss, the board-style symbol), stones as stone blocks and ground as cream cells, inside a thin stone
    /// border (0.3 cell) when the cells are at least 16 units; a light band sweeps it once when it is shown. The board's
    /// own restored ground stays pale (<c>FinishedPictureRenderer</c>); this is the win card's picture.
    /// </summary>
    public sealed class WinPictureView : MonoBehaviour
    {
        private const float Thin = 0.3f;
        private const float TileInset = 0.008f;
        private const float GroundInset = 0.025f;
        private const float SweepSeconds = 1.2f;

        private RectTransform _root = null!;
        private RectTransform? _grid;
        private RectTransform _sweepClip = null!;
        private Image _sweep = null!;
        private Box _gridBox;
        private float _unit;
        private float _shownAt = -10f;

        /// <summary>The picture's rect; place it with <see cref="Show"/>.</summary>
        public RectTransform Rect => _root;

        public static WinPictureView Create(string name, Transform parent)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            var view = root.gameObject.AddComponent<WinPictureView>();
            view._root = root;
            view._sweepClip = UiFactory.CreateRect("Sweep", root);
            view._sweepClip.gameObject.AddComponent<RectMask2D>();
            view._sweep = UiFactory.CreateImage("Band", view._sweepClip, null, UiTheme.Of(Rgba.White.WithAlpha(0.3f)));
            view._sweep.raycastTarget = false;
            return view;
        }

        /// <summary>
        /// Builds the picture of <paramref name="definition"/> (its mapping and mirror) over <paramref name="picture"/>,
        /// fitted and centered in <paramref name="box"/> (screen pixels) of a parent whose screen box is
        /// <paramref name="parent"/>; <paramref name="scale"/> is the screen's reference scale.
        /// </summary>
        public void Show(LevelDefinition definition, BasePicture picture, Box box, Box parent, float scale)
        {
            UiKit.PlaceBox(_root, box, parent);
            if (_grid != null)
            {
                Destroy(_grid.gameObject);
            }

            int w = Mathf.Max(1, picture.Width);
            int h = Mathf.Max(1, picture.Height);
            float rim = BoardLayout.Gap + Thin;
            float cell = Mathf.Min(box.Width / (w + (2f * rim)), box.Height / (h + (2f * rim)));
            bool framed = cell >= 16f * scale;
            if (!framed)
            {
                cell = Mathf.Min(box.Width / w, box.Height / h);
            }

            float ox = box.CenterX - (cell * w / 2f);
            float oy = box.CenterY - (cell * h / 2f);
            _gridBox = new Box(ox, oy, ox + (cell * w), oy + (cell * h));
            _grid = UiFactory.CreateRect("Grid", _root);
            _grid.SetAsFirstSibling();
            UiKit.PlaceBox(_grid, _gridBox, box);
            if (framed)
            {
                UiKit.StoneBorder(_grid, w, h, Thin);
            }
            else
            {
                Image gap = UiKit.RoundRect("Gap", _grid, UiTheme.Of(GardenLook.BoardGap), b => b.Width / w * 0.2f);
                UiFactory.Stretch(gap.rectTransform);
            }

            bool mirror = definition.Picture.Mirror == Mirror.Horizontal;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float left = ox + (x * cell);
                    float top = oy + ((h - 1 - y) * cell);
                    var full = new Box(left, top, left + cell, top + cell);
                    int px = mirror ? picture.Width - 1 - x : x;
                    int value = px >= 0 && px < picture.Width && y < picture.Height ? picture.CellAt(px, y) : BasePicture.Empty;
                    if (value >= 0 && definition.Mapping.TryGetValue(picture.Roles[value].RoleId, out VariantId variant))
                    {
                        CandyTileView tile = UiKit.CandyTile("Tile", _grid, variant, TileStyle.Flat);
                        UiKit.PlaceBox((RectTransform)tile.transform, full.Inset(cell * TileInset), _gridBox);
                    }
                    else if (value == BasePicture.Stone)
                    {
                        Image stone = UiKit.StoneBlock("Stone", _grid, 21 + (((x * 5) + (y * 3)) % 6), 0.26f);
                        UiKit.PlaceBox(stone.rectTransform, full.Inset(cell * 0.04f), _gridBox);
                    }
                    else
                    {
                        Image ground = UiKit.RoundRect("Ground", _grid, UiTheme.Of(C.TileGround), b => b.Width * 0.1f);
                        UiKit.PlaceBox(ground.rectTransform, full.Inset(cell * GroundInset), _gridBox);
                    }
                }
            }

            UiKit.PlaceBox(_sweepClip, _gridBox, box);
            _sweepClip.SetAsLastSibling();
            _unit = scale;
            if (isActiveAndEnabled)
            {
                _shownAt = Time.unscaledTime;
            }

            Sweep(Time.unscaledTime - _shownAt);
        }

        // The band sweeps when the picture comes into view (the win card shows after the board's reveal).
        private void OnEnable() => _shownAt = Time.unscaledTime;

        private void Update()
        {
            float since = Time.unscaledTime - _shownAt;
            if (since <= SweepSeconds + 0.1f)
            {
                Sweep(since);
            }
        }

        /// <summary>
        /// The light band at <paramref name="since"/> seconds (fx.win_shine): a bar 70 units wide, leaning 120 units over
        /// the picture's height, moving from left to right over 1.2 s, then gone.
        /// </summary>
        private void Sweep(float since)
        {
            bool on = since < SweepSeconds;
            _sweep.gameObject.SetActive(on);
            if (!on || _gridBox.IsEmpty)
            {
                return;
            }

            float u = _unit;
            float band = _gridBox.Left + ((_gridBox.Width + (200f * u)) * (since / SweepSeconds)) - (100f * u);
            float lean = Mathf.Atan2(120f * u, Mathf.Max(1f, _gridBox.Height)) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt((_gridBox.Height * _gridBox.Height) + (120f * u * 120f * u)) + (70f * u);
            UiKit.PlaceBox(_sweep.rectTransform, Box.FromCenter(band, _gridBox.CenterY, 70f * u, length), _gridBox);
            _sweep.rectTransform.localEulerAngles = new Vector3(0f, 0f, -lean);
        }
    }
}
