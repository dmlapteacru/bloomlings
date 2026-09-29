using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// The board (T041): fits up to 14×16 cells into the board area without scrolling or zooming (FR-008), draws the
    /// finished picture under the tiles (T042), and marks the Garden Entries on the board edge. Its visual state
    /// follows the event timeline, not the logical state, so tiles change when their Bloomling arrives (R4).
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private readonly List<TileView> _tiles = new List<TileView>();
        private readonly List<(Image Marker, EntryDef Entry)> _entries = new List<(Image, EntryDef)>();
        private readonly List<SpecialView> _specials = new List<SpecialView>();
        private RectTransform _area = null!;
        private RectTransform _grid = null!;
        private FinishedPictureRenderer _picture = null!;
        private VariantVisualCatalog? _visuals;
        private int _width;
        private int _height;

        /// <summary>Side length of one cell in canvas units.</summary>
        public float CellSize { get; private set; }

        /// <summary>The rect holding tiles, picture and workers; its origin is the bottom-left cell corner.</summary>
        public RectTransform Grid => _grid;

        public static BoardView Create(RectTransform area, VariantVisualCatalog? visuals)
        {
            var view = area.gameObject.AddComponent<BoardView>();
            view._area = area;
            view._visuals = visuals;
            view._grid = UiFactory.CreateRect("Grid", area);
            view._picture = view._grid.gameObject.AddComponent<FinishedPictureRenderer>();
            return view;
        }

        public void Build(LevelView view, LevelDefinition definition, BasePicture picture)
        {
            foreach (TileView tile in _tiles)
            {
                Destroy(tile.gameObject);
            }

            _tiles.Clear();
            foreach ((Image marker, EntryDef _) in _entries)
            {
                Destroy(marker.gameObject);
            }

            _entries.Clear();
            foreach (SpecialView special in _specials)
            {
                Destroy(special.gameObject);
            }

            _specials.Clear();
            _width = view.Width;
            _height = view.Height;
            Layout();
            _picture.Build(definition, picture, _visuals, _grid);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var cell = new CellPos(x, y);
                    TileView tile = TileView.Create(_grid, cell, _visuals);
                    _tiles.Add(tile);
                    Refresh(view, cell);
                }
            }

            foreach (EntryDef entry in view.Entries)
            {
                Image marker = UiFactory.CreateImage("Entry", _grid, ProceduralSprites.Ring, UiTheme.EntryMarker);
                _entries.Add((marker, entry));
            }

            foreach (SpecialInfo special in view.Specials)
            {
                _specials.Add(SpecialView.Create(_grid, special, _visuals));
            }

            Layout();
        }

        /// <summary>Canvas position of a cell center inside <see cref="Grid"/>.</summary>
        public Vector2 CellCenter(CellPos cell) => new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);

        /// <summary>Where Bloomlings emerge: just outside the board edge next to the entry cell.</summary>
        public Vector2 EntryPoint(EntryDef entry)
        {
            Vector2 c = CellCenter(entry.Cell);
            return entry.Side switch
            {
                EntrySide.Bottom => c + new Vector2(0f, -CellSize * 0.85f),
                EntrySide.Top => c + new Vector2(0f, CellSize * 0.85f),
                EntrySide.Left => c + new Vector2(-CellSize * 0.85f, 0f),
                _ => c + new Vector2(CellSize * 0.85f, 0f),
            };
        }

        /// <summary>Redraws one cell from the logical state (used on build and restart).</summary>
        public void Refresh(LevelView view, CellPos cell)
        {
            TileView tile = Tile(cell);
            CellInfo info = view.Cell(cell);
            switch (info.Kind)
            {
                case CellKind.Target:
                    tile.ShowTarget(info.Visible, info.Next, info.KeyId);
                    break;
                case CellKind.Stone:
                    tile.ShowStone();
                    break;
                case CellKind.Special:
                    // Ground under the special's own view (SpecialView).
                    tile.ShowOpen();
                    break;
                default:
                    tile.ShowOpen();
                    break;
            }
        }

        /// <summary>A layer was cleared and the next one is visible (applied when the worker arrives).</summary>
        public void ShowLayer(CellPos cell, Core.Variants.VariantId newTop, LevelView view)
        {
            CellInfo info = view.Cell(cell);
            Tile(cell).ShowTarget(newTop, info.Kind == CellKind.Target && info.Visible == newTop ? info.Next : null, null);
        }

        public void ShowOpened(CellPos cell) => Tile(cell).ShowOpen();

        /// <summary>World position of a key still lying on the board, or null.</summary>
        public Vector3? KeyPosition(CellPos cell)
        {
            TileView tile = Tile(cell);
            return tile.HasKey ? tile.KeyPosition : (Vector3?)null;
        }

        public void HideKey(CellPos cell) => Tile(cell).HideKey();

        /// <summary>Highlights the tile carrying this key (a locked pod or slot was tapped).</summary>
        public void FlashKey(LevelView view, string keyId)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var cell = new CellPos(x, y);
                    if (view.Cell(cell).KeyId == keyId)
                    {
                        Tile(cell).FlashKey();
                    }
                }
            }
        }

        public Vector3 CellWorldPosition(CellPos cell) => Tile(cell).transform.position;

        public RectTransform CellRect(CellPos cell) => (RectTransform)Tile(cell).transform;

        /// <summary>The first cell, bottom row first, whose state matches; null when none does (demo pointers).</summary>
        public RectTransform? FindCell(LevelView view, System.Func<CellInfo, bool> match)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var cell = new CellPos(x, y);
                    if (match(view.Cell(cell)))
                    {
                        return CellRect(cell);
                    }
                }
            }

            return null;
        }

        public RectTransform? SpecialRect(string specialId) => Special(specialId) is SpecialView special ? (RectTransform)special.transform : null;

        public RectTransform? SpecialRectOfType(SpecialType type) => _specials.Find(s => s.Type == type) is SpecialView special ? (RectTransform)special.transform : null;

        public Vector3? SpecialPosition(string specialId)
        {
            SpecialView? special = Special(specialId);
            return special != null ? special.transform.position : (Vector3?)null;
        }

        public void ShowSpecialProgress(string specialId, int progress, int total, SpecialConditionKind kind) => Special(specialId)?.SetProgress(progress, total, kind);

        /// <summary>A special triggered: its animation, then the changed cells (opened ground, removed stones).</summary>
        public void TriggerSpecial(string specialId, IReadOnlyList<CellPos> cells, LevelView view)
        {
            Special(specialId)?.Trigger();
            foreach (CellPos cell in cells)
            {
                // Opened ground and removed stones; revealed mystery tiles arrive as their own event.
                if (view.Cell(cell).Kind == CellKind.Open)
                {
                    Tile(cell).ShowOpen();
                }
            }
        }

        private SpecialView? Special(string specialId) => _specials.Find(s => s.SpecialId == specialId);

        public void ShowMysteryRevealed(CellPos cell, Core.Variants.VariantId variant) => Tile(cell).ShowTarget(variant, null, null);

        public void RevealAll()
        {
            foreach (TileView tile in _tiles)
            {
                if (tile.Shown.HasValue)
                {
                    tile.ShowOpen();
                }
            }

            _picture.RevealAll();
        }

        private TileView Tile(CellPos cell) => _tiles[(cell.Y * _width) + cell.X];

        private void Layout()
        {
            Rect area = _area.rect;

            // One cell of margin below for the entry markers; the rest fits the board by its aspect ratio.
            CellSize = Mathf.Floor(Mathf.Min(area.width / _width, area.height / (_height + 1f)));
            _grid.anchorMin = new Vector2(0.5f, 0.5f);
            _grid.anchorMax = new Vector2(0.5f, 0.5f);
            _grid.pivot = new Vector2(0.5f, 0.5f);
            _grid.sizeDelta = new Vector2(_width * CellSize, _height * CellSize);
            _grid.anchoredPosition = new Vector2(0f, CellSize * 0.5f);
            foreach (TileView tile in _tiles)
            {
                UiFactory.PlaceAbsolute((RectTransform)tile.transform, CellCenter(tile.Cell), Vector2.one * CellSize * 0.96f);
            }

            foreach ((Image marker, EntryDef entry) in _entries)
            {
                UiFactory.PlaceAbsolute(marker.rectTransform, EntryPoint(entry), Vector2.one * CellSize * 0.8f);
            }

            // A special covers the bounding box of its cells.
            foreach (SpecialView special in _specials)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (CellPos cell in special.Cells)
                {
                    minX = Mathf.Min(minX, cell.X);
                    minY = Mathf.Min(minY, cell.Y);
                    maxX = Mathf.Max(maxX, cell.X);
                    maxY = Mathf.Max(maxY, cell.Y);
                }

                var center = new Vector2((minX + maxX + 1) * 0.5f * CellSize, (minY + maxY + 1) * 0.5f * CellSize);
                UiFactory.PlaceAbsolute((RectTransform)special.transform, center, new Vector2((maxX - minX + 1) * CellSize, (maxY - minY + 1) * CellSize) * 0.96f);
                special.transform.SetAsLastSibling();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_width > 0 && _grid != null)
            {
                Layout();
            }
        }
    }
}
