using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Bloomlings.Core.Simulation;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// The board (T041) in the reference look (spec 005 contracts/look.md §3.6, §4.1; the playtest's <c>BoardPainter</c>):
    /// it lies on the lawn inside a border of sandy stone blocks, laid out by the shared <see cref="BoardLayout"/>, so it
    /// fits up to 14×16 cells into the board area without scrolling or zooming (FR-008) exactly as the playtest does.
    /// <list type="bullet">
    /// <item><description>Target tiles are candy tiles that nearly touch, parted by the dark board gap
    /// (<see cref="TileView"/>).</description></item>
    /// <item><description>Restored ground shows the finished picture as pale flat cells under the tiles (T042,
    /// <see cref="FinishedPictureRenderer"/>).</description></item>
    /// <item><description>Each Garden Entry is a stone arch on its side of the board; the Bloomlings come out at its door
    /// (<see cref="EntryArch.Door"/>).</description></item>
    /// </list>
    /// Its visual state follows the event timeline, not the logical state, so tiles change when their Bloomling arrives
    /// (R4): a restored tile shrinks away with a small sparkle, and on a win the finished picture shines. The cells that
    /// count toward a special's condition are ringed in that special's color.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private readonly List<TileView> _tiles = new List<TileView>();
        private readonly List<(RectTransform Root, Image Arch, EntryDef Entry)> _entries = new List<(RectTransform, Image, EntryDef)>();
        private readonly List<SpecialView> _specials = new List<SpecialView>();
        private readonly List<float> _archRadius = new List<float>();
        private RectTransform _area = null!;
        private RectTransform _grid = null!;
        private FinishedPictureRenderer _picture = null!;
        private StoneBorderView? _border;
        private VariantVisualCatalog? _visuals;
        private BoardLayout? _layout;
        private IReadOnlyList<EntryDef> _entryDefs = new EntryDef[0];
        private int _width;
        private int _height;

        /// <summary>A tile was tapped while targeting (Bloom Burst).</summary>
        public event System.Action<CellPos>? CellTapped;

        /// <summary>
        /// Lays the board out in its area's top-down canvas units (the area's box, the board's width and height, its
        /// entries): the HUD's <c>GameplayHud.FitBoard</c>, the stone border at most 0.86 of the safe width with a bottom
        /// entry's arch in the entry strip (spec 005 FR-020, contracts/look.md §6.1); null fits the whole area
        /// (<see cref="BoardLayout.Fit"/>).
        /// </summary>
        public System.Func<Box, int, int, IReadOnlyList<EntryDef>, BoardLayout>? Fit { get; set; }

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
            foreach ((RectTransform root, Image _, EntryDef _) in _entries)
            {
                Destroy(root.gameObject);
            }

            _entries.Clear();
            foreach (SpecialView special in _specials)
            {
                Destroy(special.gameObject);
            }

            _specials.Clear();
            _width = view.Width;
            _height = view.Height;
            _entryDefs = view.Entries;
            Layout();
            _picture.Build(definition, picture, _width, _height, GroundPixels(), _grid);

            // The stone border with its dark gap lies under the ground and the tiles (§3.6).
            if (_border == null)
            {
                _border = UiKit.StoneBorder(_grid, _width, _height);
            }
            else
            {
                _border.SetGrid(_width, _height);
            }

            _border.transform.SetAsFirstSibling();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var cell = new CellPos(x, y);
                    TileView tile = TileView.Create(_grid, cell, _visuals);
                    tile.Tapped += c => CellTapped?.Invoke(c);
                    _tiles.Add(tile);
                }
            }

            // The arches stand on the lawn behind the board (§3.6, board.arch), each over a soft ground shadow.
            foreach (EntryDef entry in view.Entries)
            {
                RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("Entry", _area));
                root.SetAsFirstSibling();
                int index = _entries.Count;
                BoxLayout layout = BoxLayout.On(root);
                UiKit.SoftShadow(layout, _ => ArchShadow(index), _ => ArchRadius(index), 0.18f, 0.02f);

                // A soft shadow on the lawn under the piers' feet (the playtest's Kit.StoneArch).
                Image foot = UiKit.Ellipse("Foot", root, UiTheme.Of(DesignTokens.Colors.GardenShadow.WithAlpha(0.18f)));
                layout.Add(foot.rectTransform, _ => ArchFoot(index));
                Image arch = UiKit.StoneArch("Arch", root, entry.Side);
                layout.Add(arch.rectTransform, _ => ArchBox(index));
                _entries.Add((root, arch, entry));
            }

            foreach (SpecialInfo special in view.Specials)
            {
                _specials.Add(SpecialView.Create(_grid, special, _visuals));
            }

            Layout();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Refresh(view, new CellPos(x, y));
                }
            }

            UpdateCounted(view);
        }

        /// <summary>
        /// Rings the cells that count toward each unresolved special (FR-037, FR-038): for "restore N &lt;variant&gt;
        /// around it", the target tiles next to it that hold that variant; for "restore this region", the region's
        /// remaining tiles.
        /// </summary>
        public void UpdateCounted(LevelView view)
        {
            foreach (TileView tile in _tiles)
            {
                tile.SetCounted(null);
            }

            foreach (SpecialInfo special in view.Specials)
            {
                if (special.Triggered || special.Condition.Kind == SpecialConditionKind.Key)
                {
                    continue;
                }

                Color color = SpecialView.ColorOf(special.Type);
                foreach (CellPos cell in CountedCells(view, special))
                {
                    if (Tile(cell).Shown.HasValue || view.Cell(cell).MysteryHidden)
                    {
                        Tile(cell).SetCounted(color);
                    }
                }
            }
        }

        private IEnumerable<CellPos> CountedCells(LevelView view, SpecialInfo special)
        {
            if (special.Condition.Kind == SpecialConditionKind.ClearRegion)
            {
                foreach (CellPos cell in special.Condition.RegionCells)
                {
                    if (view.Cell(cell).Kind == CellKind.Target)
                    {
                        yield return cell;
                    }
                }

                yield break;
            }

            var own = new HashSet<CellPos>(special.Cells);
            var seen = new HashSet<CellPos>();
            foreach (CellPos cell in special.Cells)
            {
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    if (!cell.TryGetNeighbour(dir, _width, _height, out CellPos next) || own.Contains(next) || !seen.Add(next))
                    {
                        continue;
                    }

                    CellInfo info = view.Cell(next);
                    if (info.Kind == CellKind.Target && (special.Condition.Variant == null || info.Visible == special.Condition.Variant))
                    {
                        yield return next;
                    }
                }
            }
        }

        /// <summary>Canvas position of a cell center inside <see cref="Grid"/>.</summary>
        public Vector2 CellCenter(CellPos cell) => new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);

        /// <summary>
        /// Where Bloomlings emerge: the door of the entry's stone arch (<see cref="EntryArch.Door"/>, 42% of its radius
        /// inside the opening), in <see cref="Grid"/>'s coordinates; just outside the entry cell before the first layout.
        /// </summary>
        public Vector2 EntryPoint(EntryDef entry)
        {
            if (_layout != null)
            {
                for (int i = 0; i < _entryDefs.Count && i < _layout.Arches.Count; i++)
                {
                    if (_entryDefs[i].Cell == entry.Cell && _entryDefs[i].Side == entry.Side)
                    {
                        (float x, float y) = _layout.Arches[i].Door;
                        return new Vector2(x - _layout.Grid.Left, _layout.Grid.Bottom - y);
                    }
                }
            }

            Vector2 c = CellCenter(entry.Cell);
            float d = CellSize * 0.78f;
            return entry.Side switch
            {
                EntrySide.Bottom => c + new Vector2(0f, -d),
                EntrySide.Top => c + new Vector2(0f, d),
                EntrySide.Left => c + new Vector2(-d, 0f),
                _ => c + new Vector2(d, 0f),
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
                    tile.ShowTarget(info.MysteryHidden ? null : info.Visible, info.RemainingLayers > 1 ? info.Next : null, info.KeyId);
                    break;
                case CellKind.Stone:
                    tile.ShowStone();
                    break;
                default:
                    // Open ground, or the ground under a special's own view (SpecialView).
                    tile.ShowOpen();
                    break;
            }
        }

        /// <summary>A layer was cleared and the next one is visible (applied when the worker arrives): the tile flips over.</summary>
        public void ShowLayer(CellPos cell, Core.Variants.VariantId newTop, LevelView view)
        {
            CellInfo info = view.Cell(cell);
            Sparkle(cell);
            Tile(cell).ShowTarget(newTop, info.Kind == CellKind.Target && info.Visible == newTop && info.RemainingLayers > 1 ? info.Next : null, null, animate: true);
        }

        /// <summary>Bloom Burst removed a variant (FR-050): each of its tiles bursts in a puff, then shows what is left.</summary>
        public void ShowBurst(IReadOnlyList<CellPos> cells, LevelView view)
        {
            foreach (CellPos cell in cells)
            {
                TileView tile = Tile(cell);
                if (tile.Shown.HasValue && isActiveAndEnabled)
                {
                    Effects.UiFx.Puff(_grid, tile.transform.position, UiTheme.Of(BoardPictures.ColorOf(tile.Shown.Value)), 7, CellSize * 0.9f, CellSize * 0.28f, 0.4f, ProceduralSprites.Star);
                }

                CellInfo info = view.Cell(cell);
                if (info.Kind == CellKind.Target)
                {
                    tile.ShowTarget(info.MysteryHidden ? null : info.Visible, info.RemainingLayers > 1 ? info.Next : null, info.KeyId, animate: true);
                }
                else if (info.Kind == CellKind.Stone)
                {
                    tile.ShowStone();
                }
                else
                {
                    tile.ShowOpen(animate: true);
                }
            }

            UpdateCounted(view);
        }

        /// <summary>A Bloomling restored the tile: it shrinks away with a small sparkle.</summary>
        public void ShowOpened(CellPos cell)
        {
            Sparkle(cell);
            Tile(cell).ShowOpen(animate: true);
        }

        /// <summary>A small puff in the light color of the tile's variant (decorative only).</summary>
        private void Sparkle(CellPos cell)
        {
            TileView tile = Tile(cell);
            if (!tile.Shown.HasValue || !isActiveAndEnabled)
            {
                return;
            }

            Effects.UiFx.Puff(_grid, tile.transform.position, UiTheme.Of(BoardPictures.ColorOf(tile.Shown.Value).Lighten(0.55f)), 5, CellSize * 0.6f, CellSize * 0.22f, 0.3f);
        }

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

        /// <summary>Lets the visible tiles take taps (Bloom Burst's target, each ringed) or stops it.</summary>
        public void SetTargeting(bool on)
        {
            foreach (TileView tile in _tiles)
            {
                tile.SetTappable(on && tile.Shown.HasValue);
            }
        }

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
            SpecialView? special = Special(specialId);
            special?.Trigger(_grid, CellSize);
            foreach (CellPos cell in cells)
            {
                // Opened ground and removed stones; revealed mystery tiles arrive as their own event.
                if (view.Cell(cell).Kind == CellKind.Open)
                {
                    Tile(cell).ShowOpen(animate: true);
                }
            }

            UpdateCounted(view);
        }

        private SpecialView? Special(string specialId) => _specials.Find(s => s.SpecialId == specialId);

        public void ShowMysteryRevealed(CellPos cell, Core.Variants.VariantId variant) => Tile(cell).ShowTarget(variant, null, null, animate: true);

        /// <summary>The win (FR-025): the last tiles shrink away and the finished picture shines.</summary>
        public void RevealAll()
        {
            foreach (TileView tile in _tiles)
            {
                if (tile.Shown.HasValue)
                {
                    tile.ShowOpen(animate: true);
                }
            }

            _picture.RevealAll();
        }

        private TileView Tile(CellPos cell) => _tiles[(cell.Y * _width) + cell.X];

        /// <summary>The ground's pixels per cell: the cell's size on screen, 64 before the board has a size.</summary>
        private int GroundPixels() => CellSize > 1f ? Mathf.CeilToInt(CellSize * UiKit.PixelsPerUnit) : 64;

        /// <summary>
        /// An entry's arch picture box in the area's top-down coordinates: the ring on the board's side of its base and the
        /// two straight piers it stands on beyond it (<see cref="EntryArch.Picture"/>, the playtest's <c>Kit.StoneArch</c>).
        /// </summary>
        private Box ArchBox(int index)
        {
            if (_layout == null || index >= _layout.Arches.Count)
            {
                return new Box(0f, 0f, 0f, 0f);
            }

            return _layout.Arches[index].Picture;
        }

        /// <summary>The soft ground shadow under an arch's piers, at its picture's far edge (the playtest's <c>Kit.StoneArch</c>).</summary>
        private Box ArchFoot(int index)
        {
            if (_layout == null || index >= _layout.Arches.Count)
            {
                return new Box(0f, 0f, 0f, 0f);
            }

            EntryArch arch = _layout.Arches[index];
            Box box = arch.Picture;
            float r = arch.Radius;
            float foot = r * 0.3f;
            return arch.Side switch
            {
                EntrySide.Left => Box.FromCenter(box.Left + (foot * 0.3f), box.CenterY, foot, r * 2.2f),
                EntrySide.Right => Box.FromCenter(box.Right - (foot * 0.3f), box.CenterY, foot, r * 2.2f),
                EntrySide.Top => Box.FromCenter(box.CenterX, box.Top + (foot * 0.3f), r * 2.2f, foot),
                _ => Box.FromCenter(box.CenterX, box.Bottom - (foot * 0.3f), r * 2.2f, foot),
            };
        }

        private float ArchRadius(int index) => _layout != null && index < _layout.Arches.Count ? _layout.Arches[index].Radius : 0f;

        /// <summary>The soft ground shadow under an arch's crown (the playtest's <c>BoardPainter.Arch</c>).</summary>
        private Box ArchShadow(int index)
        {
            if (_layout == null || index >= _layout.Arches.Count)
            {
                return new Box(0f, 0f, 0f, 0f);
            }

            EntryArch arch = _layout.Arches[index];
            float r = arch.Radius;
            (float sx, float sy) = arch.Side switch
            {
                EntrySide.Top => (0f, -r * 0.5f),
                EntrySide.Left => (-r * 0.5f, 0f),
                EntrySide.Right => (r * 0.5f, 0f),
                _ => (0f, r * 0.5f),
            };
            bool across = arch.Side == EntrySide.Left || arch.Side == EntrySide.Right;
            return Box.FromCenter(arch.BaseX - (sx * 0.86f) + (r * 0.04f), arch.BaseY - (sy * 0.86f) + (r * 0.08f), across ? r * 1.16f : r * 2.14f, across ? r * 2.14f : r * 1.16f);
        }

        /// <summary>
        /// Lays the board out in its area (<see cref="Fit"/>, else <see cref="BoardLayout.Fit"/>, in the area's top-down
        /// canvas units): the grid of cells, the stone border around it, one arch per entry; tiles take whole cells,
        /// specials their cells' box.
        /// </summary>
        private void Layout()
        {
            Rect area = _area.rect;
            var box = new Box(0f, 0f, Mathf.Max(1f, area.width), Mathf.Max(1f, area.height));
            _layout = Fit?.Invoke(box, _width, _height, _entryDefs) ?? BoardLayout.Fit(box, _width, _height, _entryDefs);
            CellSize = _layout.Cell;
            BoxLayout.Place(_grid, _layout.Grid);
            foreach (TileView tile in _tiles)
            {
                UiFactory.PlaceAbsolute((RectTransform)tile.transform, CellCenter(tile.Cell), Vector2.one * CellSize);
            }

            foreach ((RectTransform root, Image _, EntryDef _) in _entries)
            {
                BoxLayout.On(root).Apply();
            }

            // A special covers the box of its cells.
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
                UiFactory.PlaceAbsolute((RectTransform)special.transform, center, new Vector2((maxX - minX + 1) * CellSize, (maxY - minY + 1) * CellSize));
                special.transform.SetAsLastSibling();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_width > 0 && _grid != null)
            {
                Layout();

                // A new size: the ground is drawn again when its cells changed a lot (bilinear scaling keeps small changes).
                int pixels = GroundPixels();
                if (_picture.CellPixels > 0 && Mathf.Abs(pixels - _picture.CellPixels) > _picture.CellPixels / 4)
                {
                    _picture.Redraw(pixels);
                }
            }
        }
    }
}
