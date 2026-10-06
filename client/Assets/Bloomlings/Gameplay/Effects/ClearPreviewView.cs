using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Effects
{
    /// <summary>
    /// A clearing style's live preview in a Store card's well (spec 005 FR-038, contracts/look.md §6.12;
    /// <c>ui.card.clearing</c>, the playtest's <c>Kit.ClearingPreview</c>): the small board of <see cref="ClearPreview"/>
    /// in its stone border on its plain ground, the arch under its middle and the slot below, cleared in the style in a
    /// loop on unscaled time, the style's items drawn by two <see cref="ClearFxView"/> layers. With the pair the free
    /// card shows Blossom and Munchers by turns (<see cref="ClearPreview.At"/>). Presentation only.
    /// </summary>
    public sealed class ClearPreviewView : MonoBehaviour
    {
        private readonly List<(RectTransform Holder, CandyTileView Tile)> _tiles = new List<(RectTransform, CandyTileView)>();
        private readonly FxList _fx = new FxList();
        private RectTransform _root = null!;
        private RectTransform _grid = null!;
        private Image _ground = null!;
        private Image _arch = null!;
        private SlotPlateView _slot = null!;
        private ClearFxView _fxBoard = null!;
        private ClearFxView _fxOver = null!;
        private ClearStyle _style;
        private bool _pair;
        private VariantId? _shown;
        private int _count = -1;
        private Vector2 _size;
        private float _cell;

        /// <summary>A preview filling <paramref name="well"/> (a card's picture rect, which clips it).</summary>
        public static ClearPreviewView Create(RectTransform well, ClearStyle style, bool pair, System.Func<Family, Outfit>? outfits = null)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("ClearingPreview", well));
            var view = root.gameObject.AddComponent<ClearPreviewView>();
            view._root = root;
            view._style = style;
            view._pair = pair;
            view.Build(outfits);
            return view;
        }

        private void Build(System.Func<Family, Outfit>? outfits)
        {
            _grid = UiFactory.CreateRect("Grid", _root);
            _ground = UiFactory.CreateImage("Ground", _grid, null, Color.white);
            UiFactory.Stretch(_ground.rectTransform);
            UiKit.StoneBorder(_grid, ClearPreview.Columns, ClearPreview.Rows);
            for (int row = 0; row < ClearPreview.Rows; row++)
            {
                for (int column = 0; column < ClearPreview.Columns; column++)
                {
                    // Each tile in a holder pivoted on its bottom middle, so Blossom's sway turns it about its foot.
                    RectTransform holder = UiFactory.CreateRect("Cell", _grid);
                    CandyTileView tile = UiKit.CandyTile("Tile", holder, ClearPreview.VariantOf(_style), TileStyle.Board);
                    _tiles.Add((holder, tile));
                }
            }

            _arch = UiFactory.CreateImage("EntryArch", _grid, null, Color.white);
            _slot = UiKit.SlotPlate("Slot", _grid);
            _fxBoard = ClearFxView.Create(_grid, "ClearFx");
            _fxOver = ClearFxView.Create(_grid, "ClearFxOver");
            _fxBoard.Outfits = outfits;
            _fxOver.Outfits = outfits;
        }

        private void Update()
        {
            Rect rect = _root.rect;
            if (rect.width < 1f || rect.height < 1f)
            {
                return;
            }

            if (Mathf.Abs(rect.width - _size.x) > 0.5f || Mathf.Abs(rect.height - _size.y) > 0.5f)
            {
                _size = new Vector2(rect.width, rect.height);
                Layout();
            }

            (ClearPreview scene, float t) = ClearPreview.At(_style, _pair, Time.unscaledTime);
            if (!_shown.HasValue || !_shown.Value.Equals(scene.Variant))
            {
                Show(scene.Variant);
            }

            for (int row = 0; row < ClearPreview.Rows; row++)
            {
                for (int column = 0; column < ClearPreview.Columns; column++)
                {
                    RectTransform holder = _tiles[(row * ClearPreview.Columns) + column].Holder;
                    bool gone = scene.Cleared(column, row, t) || scene.Held(column, row, t);
                    if (holder.gameObject.activeSelf == gone)
                    {
                        holder.gameObject.SetActive(!gone);
                    }

                    if (!gone)
                    {
                        holder.localEulerAngles = new Vector3(0f, 0f, -scene.Sway(column, row, t));
                    }
                }
            }

            int count = scene.Count(t);
            if (count != _count)
            {
                _count = count;
                _slot.Show(count > 0 ? SlotPlateState.Working : SlotPlateState.Empty, count > 0 ? scene.Variant : (VariantId?)null, count);
            }

            _fx.Clear();
            scene.Draw(_fx, t);
            _fxBoard.Render(_fx.Items, FxLayer.Board);
            _fxOver.Render(_fx.Items, FxLayer.Over);
        }

        /// <summary>The scene's tiles and ground in <paramref name="variant"/> (the free card changes it every loop).</summary>
        private void Show(VariantId variant)
        {
            _shown = variant;
            foreach ((RectTransform _, CandyTileView tile) in _tiles)
            {
                tile.Show(variant);
            }

            _count = -1;
            Ground();
        }

        /// <summary>
        /// Places the scene in the well as the playtest does: the whole picture (<see cref="ClearPreview.Bounds"/>) centered,
        /// at most 84% of the well wide and 90% tall; the tiles, the arch and the slot on the grid in cell units.
        /// </summary>
        private void Layout()
        {
            Box bounds = ClearPreview.Bounds;
            float c = Mathf.Min(_size.x * 0.84f / bounds.Width, _size.y * 0.9f / bounds.Height);
            _cell = c;
            int columns = ClearPreview.Columns;
            int rows = ClearPreview.Rows;

            // The grid's top is the picture's top (y up from the well's bottom left).
            float top = (_size.y / 2f) + (bounds.Height * c / 2f);
            UiFactory.PlaceAbsolute(_grid, new Vector2(_size.x / 2f, top - (rows * c / 2f)), new Vector2(columns * c, rows * c));
            Vector2 At(float x, float y) => new Vector2(x * c, (rows - y) * c);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    (RectTransform holder, CandyTileView tile) = _tiles[(row * columns) + column];
                    holder.anchorMin = Vector2.zero;
                    holder.anchorMax = Vector2.zero;
                    holder.pivot = new Vector2(0.5f, 0f);
                    holder.sizeDelta = new Vector2(c, c);
                    holder.anchoredPosition = At(column + 0.5f, row + 1f);
                    var tileRect = (RectTransform)tile.transform;
                    UiFactory.Stretch(tileRect);
                    float inset = c * BoardPictures.TileInset;
                    tileRect.offsetMin = new Vector2(inset, inset);
                    tileRect.offsetMax = new Vector2(-inset, -inset);
                }
            }

            int entry = ClearPreview.EntryColumn;
            (Box arch, float degrees) = BoardLayout.ArchOf(new Box(entry, rows - 1, entry + 1, rows), EntrySide.Bottom);
            UiFactory.PlaceAbsolute(_arch.rectTransform, At(arch.CenterX, arch.CenterY), new Vector2(arch.Width * c, arch.Height * c));
            _arch.transform.localEulerAngles = new Vector3(0f, 0f, -degrees);
            int pw = UiRaster.Quantize(arch.Width * c * UiKit.PixelsPerUnit);
            int ph = UiRaster.Quantize(arch.Height * c * UiKit.PixelsPerUnit);
            _arch.sprite = ProceduralSprites.Picture("board.entry.arch", pw, ph, UiRaster.EntryArch);

            Box slot = ClearPreview.Slot;
            UiFactory.PlaceAbsolute((RectTransform)_slot.transform, At(slot.CenterX, slot.CenterY), new Vector2(slot.Width * c, slot.Height * c));
            foreach (ClearFxView fx in new[] { _fxBoard, _fxOver })
            {
                fx.Cell = c;
                fx.Map = At;
            }

            Ground();
        }

        /// <summary>The plain ground under the tiles in the shown variant's pale color, at the cell's pixel size.</summary>
        private void Ground()
        {
            if (!_shown.HasValue || _cell <= 0f)
            {
                return;
            }

            VariantId variant = _shown.Value;
            int cellPixels = UiRaster.Quantize(_cell * UiKit.PixelsPerUnit);
            Rgba color = BoardPictures.ColorOf(variant).Lighten(0.55f);
            _ground.sprite = ProceduralSprites.Picture("tile.ground.plain." + variant, ClearPreview.Columns * cellPixels, ClearPreview.Rows * cellPixels, (pw, ph) => BoardPictures.PlainGround(ClearPreview.Columns, ClearPreview.Rows, cellPixels, color));
        }
    }
}
