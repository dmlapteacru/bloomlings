using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Boards
{
    /// <summary>What a cell is at a given moment. Every cell is exactly one thing (FR-001).</summary>
    public enum CellKind
    {
        /// <summary>Open (restored) ground: walkable, shows the finished picture (FR-007).</summary>
        Open,

        /// <summary>A target tile with one or more layers; only its top layer is visible.</summary>
        Target,

        /// <summary>A permanent blocker: never cleared, never walkable (FR-032).</summary>
        Stone,

        /// <summary>Part of a special object (gate, Fountain, ...): not walkable until its effect opens it.</summary>
        Special,
    }

    /// <summary>Result of clearing the top layer of a target cell.</summary>
    public readonly struct LayerClearResult
    {
        public LayerClearResult(VariantId cleared, bool opened, VariantId revealed)
        {
            Cleared = cleared;
            Opened = opened;
            Revealed = revealed;
        }

        public VariantId Cleared { get; }

        /// <summary>True when the last layer was cleared and the cell became open ground.</summary>
        public bool Opened { get; }

        /// <summary>The new top layer when <see cref="Opened"/> is false; <c>default</c> otherwise.</summary>
        public VariantId Revealed { get; }
    }

    /// <summary>
    /// Mutable runtime board built by <see cref="BoardBuilder"/>. Row 0 is the bottom row. Layer stacks are shared
    /// between clones because play normally moves only the top pointer; a change to a stack itself copies first. The
    /// rarely changed per-cell tables (mystery flags, keys, specials) are shared the same way, so a clone copies only
    /// the cell kinds and the top pointers.
    /// </summary>
    public sealed class Board
    {
        /// <summary>The bytes of <see cref="CellKind.Open"/> and <see cref="CellKind.Target"/> in <see cref="Kinds"/>.</summary>
        internal const byte OpenKind = (byte)CellKind.Open;
        internal const byte TargetKind = (byte)CellKind.Target;

        private static readonly VariantId[] NoLayers = new VariantId[0];
        private static readonly int[] NoCodes = new int[0];

        // Kinds and top pointers as bytes (a cell has at most 4 layers, BoardBuilder.MaxLayersBelow + 1), so a clone
        // copies a quarter of the memory.
        private readonly byte[] _kind;
        private VariantId[][] _layers;

        // The catalog index of every layer, parallel to _layers (-1 for a variant the catalog lacks). It changes with
        // _layers and is shared with it.
        private int[][] _codes;
        private readonly VariantCatalog? _catalog;
        private bool _ownsLayers;
        private readonly byte[] _top;
        private bool[] _mysteryHidden;
        private bool _ownsMystery;
        private string?[] _keyId;
        private bool _ownsKeys;
        private string?[] _specialId;
        private bool _ownsSpecials;
        private readonly bool[] _isEntryCell;
        private readonly EntryDef[] _entries;

        // The 4-neighbourhood of every cell in the fixed order of CellPos (down, left, right, up), -1 off the board, the
        // position of every cell and the entry cells in definition order. Shared between clones: the geometry never changes.
        private readonly int[] _neighbours;
        private readonly CellPos[] _positions;
        private readonly int[] _entryIndexes;

        // Remaining layers on the whole board, or -1 while not counted since the board was built.
        private int _layerCount;

        // Reachability of the current cell kinds (it depends on nothing else), or null; dropped whenever a kind changes.
        private ReachabilityResult? _reach;

        // While _reach is null: the reachability before the cells in _opened (the first _openedCount) became open ground,
        // the only change of kind play makes, so a search can update it instead of computing it again. Null when unknown.
        private ReachabilityResult? _reachBefore;

        // Whether this board made _reach (or _reachBefore) itself and no clone shares it, so that it may update it in place.
        private bool _reachOwned;
        private bool _reachBeforeOwned;
        private int[] _opened = NoOpened;
        private int _openedCount;
        private static readonly int[] NoOpened = new int[0];

        /// <param name="catalog">The catalog the layers' indexes come from (<see cref="TopCode"/>); null for none.</param>
        internal Board(int width, int height, EntryDef[] entries, VariantCatalog? catalog = null)
        {
            Width = width;
            Height = height;
            int n = width * height;
            _kind = new byte[n];
            _layers = new VariantId[n][];
            _codes = new int[n][];
            _catalog = catalog;
            _ownsLayers = true;
            for (int i = 0; i < n; i++)
            {
                _layers[i] = NoLayers;
                _codes[i] = NoCodes;
            }

            _top = new byte[n];
            _mysteryHidden = new bool[n];
            _ownsMystery = true;
            _keyId = new string?[n];
            _ownsKeys = true;
            _specialId = new string?[n];
            _ownsSpecials = true;
            _isEntryCell = new bool[n];
            _entries = entries;
            foreach (EntryDef entry in entries)
            {
                _isEntryCell[entry.Cell.ToIndex(width)] = true;
            }

            _neighbours = new int[n * CellPos.NeighbourCount];
            _positions = new CellPos[n];
            for (int i = 0; i < n; i++)
            {
                int x = i % width;
                int y = i / width;
                int at = i * CellPos.NeighbourCount;
                _neighbours[at] = y > 0 ? i - width : -1;
                _neighbours[at + 1] = x > 0 ? i - 1 : -1;
                _neighbours[at + 2] = x < width - 1 ? i + 1 : -1;
                _neighbours[at + 3] = y < height - 1 ? i + width : -1;
                _positions[i] = CellPos.FromIndex(i, width);
            }

            _entryIndexes = new int[entries.Length];
            for (int e = 0; e < entries.Length; e++)
            {
                _entryIndexes[e] = entries[e].Cell.ToIndex(width);
            }

            _layerCount = -1;
        }

        private Board(Board source)
        {
            Width = source.Width;
            Height = source.Height;
            _kind = new byte[source._kind.Length];
            _top = new byte[source._top.Length];
            _catalog = source._catalog;
            _isEntryCell = source._isEntryCell;
            _entries = source._entries;
            _neighbours = source._neighbours;
            _positions = source._positions;
            _entryIndexes = source._entryIndexes;
            _layers = source._layers;
            _codes = source._codes;
            _mysteryHidden = source._mysteryHidden;
            _keyId = source._keyId;
            _specialId = source._specialId;
            CopyCellsFrom(source);
        }

        public int Width { get; }

        public int Height { get; }

        public int CellCount => _kind.Length;

        /// <summary>Garden Entries in definition order.</summary>
        public IReadOnlyList<EntryDef> Entries => _entries;

        public Board Clone() => new Board(this);

        /// <summary>
        /// Makes this board the same as <paramref name="source"/>, a board of the same level (a clone of the same
        /// build), as <see cref="Clone"/> would, but in this board's own arrays; false when it is not of the same build.
        /// </summary>
        internal bool TryCopyFrom(Board source)
        {
            if (!ReferenceEquals(source._neighbours, _neighbours) || ReferenceEquals(source, this))
            {
                return false;
            }

            CopyCellsFrom(source);
            return true;
        }

        /// <summary>The cell state of <paramref name="source"/>: the kinds and top pointers copied, the rest shared.</summary>
        private void CopyCellsFrom(Board source)
        {
            Array.Copy(source._kind, _kind, _kind.Length);
            Array.Copy(source._top, _top, _top.Length);

            // Shared until one side rewrites a cell's stack (Bloom Burst); then that side copies (see OwnLayers).
            _layers = source._layers;
            _codes = source._codes;
            source._ownsLayers = false;
            _ownsLayers = false;

            // Shared until one side changes them (see OwnMystery, OwnKeys and OwnSpecials).
            _mysteryHidden = source._mysteryHidden;
            source._ownsMystery = false;
            _ownsMystery = false;
            _keyId = source._keyId;
            source._ownsKeys = false;
            _ownsKeys = false;
            _specialId = source._specialId;
            source._ownsSpecials = false;
            _ownsSpecials = false;
            _layerCount = source._layerCount;
            _reach = source._reach;
            _reachOwned = false;
            source._reachOwned = false;
            _reachBefore = null;
            _openedCount = 0;
        }

        public int IndexOf(CellPos cell) => cell.ToIndex(Width);

        public CellPos PosOf(int index) => CellPos.FromIndex(index, Width);

        public CellKind KindAt(int index) => (CellKind)_kind[index];

        public bool IsWalkable(int index) => _kind[index] == OpenKind;

        public bool IsTarget(int index) => _kind[index] == TargetKind;

        public bool IsEntryCell(int index) => _isEntryCell[index];

        /// <summary>
        /// The neighbours of every cell, <see cref="CellPos.NeighbourCount"/> per cell in the order of
        /// <see cref="CellPos.TryGetNeighbour"/>; -1 where a neighbour lies off the board.
        /// </summary>
        internal int[] Neighbours => _neighbours;

        /// <summary>The kind of every cell as a byte (<see cref="OpenKind"/>, <see cref="TargetKind"/>; read only, the rules' inner loops).</summary>
        internal byte[] Kinds => _kind;

        /// <summary>Whether each cell is an entry cell (read only).</summary>
        internal bool[] EntryCells => _isEntryCell;

        /// <summary>The cell index of each Garden Entry, in definition order (read only).</summary>
        internal int[] EntryIndexes => _entryIndexes;

        /// <summary>The position of every cell, by index (read only).</summary>
        internal CellPos[] Positions => _positions;

        /// <summary>
        /// The reachability of the current cell kinds with its routes (<see cref="Reachability.Compute(Board)"/>),
        /// computed once and kept until a cell changes kind. The result is never changed, so clones share it.
        /// </summary>
        internal ReachabilityResult Reach
        {
            get
            {
                if (_reach == null || !_reach.HasRoutes)
                {
                    _reach = Reachability.Compute(this, routes: true);
                    _reachOwned = true;
                    _reachBefore = null;
                    _openedCount = 0;
                }

                return _reach;
            }
        }

        /// <summary>
        /// As <see cref="Reach"/>, but the routes may be left out: for a search, which builds no events. The targets,
        /// their order and distances are the same. After cells opened, it is updated from the reachability before
        /// (<see cref="Reachability.Update"/>).
        /// </summary>
        internal ReachabilityResult ReachTargets
        {
            get
            {
                if (_reach == null)
                {
                    _reach = _reachBefore != null
                        ? Reachability.Update(this, _reachBefore, _opened, _openedCount, inPlace: _reachBeforeOwned)
                        : Reachability.Compute(this, routes: false);
                    _reachOwned = true;
                    _reachBefore = null;
                    _openedCount = 0;
                }

                return _reach;
            }
        }

        /// <summary>The visible top layer of a target cell.</summary>
        public VariantId TopLayer(int index)
        {
            EnsureTarget(index);
            return _layers[index][_top[index]];
        }

        /// <summary>
        /// The catalog index of a target cell's top layer (the catalog given when the board was built), or -1 when the
        /// catalog does not hold it. The caller makes sure the cell is a target.
        /// </summary>
        internal int TopCode(int index) => _codes[index][_top[index]];

        /// <summary>The catalog index of the layer at <paramref name="depth"/> of the original stack, or -1 (see <see cref="TopCode"/>).</summary>
        internal int CodeAt(int index, int depth) => _codes[index][depth];

        /// <summary>Number of layers still on the cell, including the top one.</summary>
        public int RemainingLayers(int index) => _kind[index] == TargetKind ? _layers[index].Length - _top[index] : 0;

        /// <summary>Depth of the current top layer in the original stack (0 = original top); used for hashing.</summary>
        public int TopDepth(int index) => _top[index];

        /// <summary>The layer under the current top, for the layer peek indicator (FR-036); <c>null</c> if none.</summary>
        public VariantId? NextLayer(int index)
        {
            if (RemainingLayers(index) < 2)
            {
                return null;
            }

            return _layers[index][_top[index] + 1];
        }

        /// <summary>Number of layers the cell started with (0 for non-target cells at build time).</summary>
        public int OriginalLayerCount(int index) => _layers[index].Length;

        /// <summary>The layer at <paramref name="depth"/> of the original stack (0 = original top).</summary>
        public VariantId LayerAt(int index, int depth) => _layers[index][depth];

        public bool IsMysteryHidden(int index) => _mysteryHidden[index];

        public void RevealMystery(int index) => ClearMystery(index);

        /// <summary>Key lying on this cell, collected when its supporting top layer is cleared (FR-033).</summary>
        public string? KeyAt(int index) => _keyId[index];

        public void RemoveKey(int index)
        {
            if (_keyId[index] != null)
            {
                OwnKeys();
                _keyId[index] = null;
            }
        }

        public string? SpecialAt(int index) => _specialId[index];

        /// <summary>Clears the top layer of a target cell (one work unit, FR-017).</summary>
        public LayerClearResult ClearTopLayer(int index)
        {
            EnsureTarget(index);
            VariantId cleared = _layers[index][_top[index]];
            _top[index]++;
            if (_layerCount > 0)
            {
                _layerCount--;
            }

            if (_top[index] >= _layers[index].Length)
            {
                SetKind(index, CellKind.Open);
                ClearMystery(index);
                return new LayerClearResult(cleared, true, default);
            }

            return new LayerClearResult(cleared, false, _layers[index][_top[index]]);
        }

        /// <summary>Turns a special or stone cell into open ground (special effects, FR-037).</summary>
        public void OpenCell(int index)
        {
            if (_kind[index] == TargetKind)
            {
                throw new InvalidOperationException("A target cell opens only by clearing its layers.");
            }

            SetKind(index, CellKind.Open);
            if (_specialId[index] != null)
            {
                OwnSpecials();
                _specialId[index] = null;
            }
        }

        /// <summary>
        /// Remaining layers of <paramref name="variant"/> on the whole board, visible and hidden (FR-023 accounting).
        /// </summary>
        public int CountLayers(VariantId variant)
        {
            int count = 0;
            for (int i = 0; i < _kind.Length; i++)
            {
                if (_kind[i] != TargetKind)
                {
                    continue;
                }

                VariantId[] layers = _layers[i];
                for (int d = _top[i]; d < layers.Length; d++)
                {
                    if (layers[d] == variant)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Total remaining layers of all variants (counted once, then kept in step with every clear).</summary>
        public int CountAllLayers()
        {
            if (_layerCount < 0)
            {
                int count = 0;
                for (int i = 0; i < _kind.Length; i++)
                {
                    count += RemainingLayers(i);
                }

                _layerCount = count;
            }

            return _layerCount;
        }

        /// <summary>
        /// Removes every remaining layer of <paramref name="variant"/> from a target cell (Bloom Burst, FR-050) and returns
        /// how many were removed. The remaining layers keep their order; a cell with none left opens.
        /// </summary>
        internal int RemoveVariant(int index, VariantId variant)
        {
            EnsureTarget(index);
            OwnLayers();
            VariantId[] layers = _layers[index];
            var kept = new List<VariantId>(layers.Length);
            for (int d = _top[index]; d < layers.Length; d++)
            {
                if (layers[d] != variant)
                {
                    kept.Add(layers[d]);
                }
            }

            int removed = layers.Length - _top[index] - kept.Count;
            if (removed == 0)
            {
                return 0;
            }

            if (_layerCount >= 0)
            {
                _layerCount -= removed;
            }

            if (kept.Count == 0)
            {
                SetKind(index, CellKind.Open);
                ClearMystery(index);
                _layers[index] = NoLayers;
                _codes[index] = NoCodes;
            }
            else
            {
                _layers[index] = kept.ToArray();
                _codes[index] = Codes(_layers[index]);
            }

            _top[index] = 0;
            return removed;
        }

        /// <summary>Replaces the top layer of a target cell before play (a hypothetical mystery assignment).</summary>
        internal void ReplaceTop(int index, VariantId variant)
        {
            EnsureTarget(index);
            OwnLayers();
            var layers = (VariantId[])_layers[index].Clone();
            layers[_top[index]] = variant;
            _layers[index] = layers;
            _codes[index] = Codes(layers);
        }

        internal void SetOpen(int index)
        {
            SetKind(index, CellKind.Open);
            _layerCount = -1;
        }

        internal void SetStone(int index)
        {
            SetKind(index, CellKind.Stone);
            _layerCount = -1;
        }

        internal void SetTarget(int index, VariantId[] layersTopFirst, bool mysteryHidden, string? keyId)
        {
            // BoardBuilder allows at most 1 + MaxLayersBelow layers; the top pointer is a byte.
            if (layersTopFirst.Length > byte.MaxValue)
            {
                throw new InvalidLevelException($"Cell {PosOf(index)} has {layersTopFirst.Length} layers.");
            }

            OwnLayers();
            OwnMystery();
            OwnKeys();
            SetKind(index, CellKind.Target);
            _layers[index] = layersTopFirst;
            _codes[index] = Codes(layersTopFirst);
            _top[index] = 0;
            _mysteryHidden[index] = mysteryHidden;
            _keyId[index] = keyId;
            _layerCount = -1;
        }

        internal void SetSpecial(int index, string specialId)
        {
            OwnSpecials();
            SetKind(index, CellKind.Special);
            _specialId[index] = specialId;
            _layerCount = -1;
        }

        /// <summary>
        /// Changes a cell's kind; the kept reachability no longer holds. When the cell becomes open ground, the
        /// reachability before is kept with the opened cells, so that a search can update it.
        /// </summary>
        private void SetKind(int index, CellKind kind)
        {
            _kind[index] = (byte)kind;
            if (kind != CellKind.Open || (_reach == null && _reachBefore == null))
            {
                _reach = null;
                _reachBefore = null;
                _openedCount = 0;
                return;
            }

            if (_reach != null)
            {
                _reachBefore = _reach;
                _reachBeforeOwned = _reachOwned;
                _reach = null;
                _openedCount = 0;
            }

            if (_openedCount == _opened.Length)
            {
                var grown = new int[Math.Max(8, _opened.Length * 2)];
                Array.Copy(_opened, grown, _openedCount);
                _opened = grown;
            }

            _opened[_openedCount++] = index;
        }

        private void ClearMystery(int index)
        {
            if (_mysteryHidden[index])
            {
                OwnMystery();
                _mysteryHidden[index] = false;
            }
        }

        /// <summary>Copies the shared outer arrays of layer stacks (and their indexes) before this board changes one.</summary>
        private void OwnLayers()
        {
            if (!_ownsLayers)
            {
                _layers = (VariantId[][])_layers.Clone();
                _codes = (int[][])_codes.Clone();
                _ownsLayers = true;
            }
        }

        /// <summary>The catalog index of each layer, -1 where the catalog lacks the variant (or there is no catalog).</summary>
        private int[] Codes(VariantId[] layers)
        {
            if (layers.Length == 0)
            {
                return NoCodes;
            }

            var codes = new int[layers.Length];
            for (int d = 0; d < layers.Length; d++)
            {
                codes[d] = _catalog == null ? -1 : _catalog.IndexOrMinusOne(layers[d]);
            }

            return codes;
        }

        private void OwnMystery()
        {
            if (!_ownsMystery)
            {
                _mysteryHidden = (bool[])_mysteryHidden.Clone();
                _ownsMystery = true;
            }
        }

        private void OwnKeys()
        {
            if (!_ownsKeys)
            {
                _keyId = (string?[])_keyId.Clone();
                _ownsKeys = true;
            }
        }

        private void OwnSpecials()
        {
            if (!_ownsSpecials)
            {
                _specialId = (string?[])_specialId.Clone();
                _ownsSpecials = true;
            }
        }

        private void EnsureTarget(int index)
        {
            if (_kind[index] != TargetKind)
            {
                throw new InvalidOperationException($"Cell {PosOf(index)} is {(CellKind)_kind[index]}, not a target.");
            }
        }
    }
}
