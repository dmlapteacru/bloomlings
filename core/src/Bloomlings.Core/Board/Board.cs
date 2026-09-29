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
    /// between clones because play normally moves only the top pointer; a change to a stack itself copies first.
    /// </summary>
    public sealed class Board
    {
        private static readonly VariantId[] NoLayers = new VariantId[0];

        private readonly CellKind[] _kind;
        private VariantId[][] _layers;
        private bool _ownsLayers;
        private readonly int[] _top;
        private readonly bool[] _mysteryHidden;
        private readonly string?[] _keyId;
        private readonly string?[] _specialId;
        private readonly bool[] _isEntryCell;
        private readonly EntryDef[] _entries;

        internal Board(int width, int height, EntryDef[] entries)
        {
            Width = width;
            Height = height;
            int n = width * height;
            _kind = new CellKind[n];
            _layers = new VariantId[n][];
            _ownsLayers = true;
            for (int i = 0; i < n; i++)
            {
                _layers[i] = NoLayers;
            }

            _top = new int[n];
            _mysteryHidden = new bool[n];
            _keyId = new string?[n];
            _specialId = new string?[n];
            _isEntryCell = new bool[n];
            _entries = entries;
            foreach (EntryDef entry in entries)
            {
                _isEntryCell[entry.Cell.ToIndex(width)] = true;
            }
        }

        private Board(Board source)
        {
            Width = source.Width;
            Height = source.Height;
            _kind = (CellKind[])source._kind.Clone();
            // Shared until one side rewrites a cell's stack (Bloom Burst); then that side copies (see OwnLayers).
            _layers = source._layers;
            source._ownsLayers = false;
            _ownsLayers = false;
            _top = (int[])source._top.Clone();
            _mysteryHidden = (bool[])source._mysteryHidden.Clone();
            _keyId = (string?[])source._keyId.Clone();
            _specialId = (string?[])source._specialId.Clone();
            _isEntryCell = source._isEntryCell;
            _entries = source._entries;
        }

        public int Width { get; }

        public int Height { get; }

        public int CellCount => _kind.Length;

        /// <summary>Garden Entries in definition order.</summary>
        public IReadOnlyList<EntryDef> Entries => _entries;

        public Board Clone() => new Board(this);

        public int IndexOf(CellPos cell) => cell.ToIndex(Width);

        public CellPos PosOf(int index) => CellPos.FromIndex(index, Width);

        public CellKind KindAt(int index) => _kind[index];

        public bool IsWalkable(int index) => _kind[index] == CellKind.Open;

        public bool IsTarget(int index) => _kind[index] == CellKind.Target;

        public bool IsEntryCell(int index) => _isEntryCell[index];

        /// <summary>The visible top layer of a target cell.</summary>
        public VariantId TopLayer(int index)
        {
            EnsureTarget(index);
            return _layers[index][_top[index]];
        }

        /// <summary>Number of layers still on the cell, including the top one.</summary>
        public int RemainingLayers(int index) => _kind[index] == CellKind.Target ? _layers[index].Length - _top[index] : 0;

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

        public void RevealMystery(int index) => _mysteryHidden[index] = false;

        /// <summary>Key lying on this cell, collected when its supporting top layer is cleared (FR-033).</summary>
        public string? KeyAt(int index) => _keyId[index];

        public void RemoveKey(int index) => _keyId[index] = null;

        public string? SpecialAt(int index) => _specialId[index];

        /// <summary>Clears the top layer of a target cell (one work unit, FR-017).</summary>
        public LayerClearResult ClearTopLayer(int index)
        {
            EnsureTarget(index);
            VariantId cleared = _layers[index][_top[index]];
            _top[index]++;
            if (_top[index] >= _layers[index].Length)
            {
                _kind[index] = CellKind.Open;
                _mysteryHidden[index] = false;
                return new LayerClearResult(cleared, true, default);
            }

            return new LayerClearResult(cleared, false, _layers[index][_top[index]]);
        }

        /// <summary>Turns a special or stone cell into open ground (special effects, FR-037).</summary>
        public void OpenCell(int index)
        {
            if (_kind[index] == CellKind.Target)
            {
                throw new InvalidOperationException("A target cell opens only by clearing its layers.");
            }

            _kind[index] = CellKind.Open;
            _specialId[index] = null;
        }

        /// <summary>
        /// Remaining layers of <paramref name="variant"/> on the whole board, visible and hidden (FR-023 accounting).
        /// </summary>
        public int CountLayers(VariantId variant)
        {
            int count = 0;
            for (int i = 0; i < _kind.Length; i++)
            {
                if (_kind[i] != CellKind.Target)
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

        /// <summary>Total remaining layers of all variants.</summary>
        public int CountAllLayers()
        {
            int count = 0;
            for (int i = 0; i < _kind.Length; i++)
            {
                count += RemainingLayers(i);
            }

            return count;
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

            if (kept.Count == 0)
            {
                _kind[index] = CellKind.Open;
                _mysteryHidden[index] = false;
                _layers[index] = NoLayers;
            }
            else
            {
                _layers[index] = kept.ToArray();
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
        }

        internal void SetOpen(int index) => _kind[index] = CellKind.Open;

        internal void SetStone(int index) => _kind[index] = CellKind.Stone;

        internal void SetTarget(int index, VariantId[] layersTopFirst, bool mysteryHidden, string? keyId)
        {
            OwnLayers();
            _kind[index] = CellKind.Target;
            _layers[index] = layersTopFirst;
            _top[index] = 0;
            _mysteryHidden[index] = mysteryHidden;
            _keyId[index] = keyId;
        }

        internal void SetSpecial(int index, string specialId)
        {
            _kind[index] = CellKind.Special;
            _specialId[index] = specialId;
        }

        /// <summary>Copies the shared outer array of layer stacks before this board changes one.</summary>
        private void OwnLayers()
        {
            if (!_ownsLayers)
            {
                _layers = (VariantId[][])_layers.Clone();
                _ownsLayers = true;
            }
        }

        private void EnsureTarget(int index)
        {
            if (_kind[index] != CellKind.Target)
            {
                throw new InvalidOperationException($"Cell {PosOf(index)} is {_kind[index]}, not a target.");
            }
        }
    }
}
