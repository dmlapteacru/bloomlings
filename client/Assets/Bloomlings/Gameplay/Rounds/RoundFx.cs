using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.Gameplay.Rounds
{
    /// <summary>
    /// A round level's clearing-style items each frame (spec 006 T044, T059; the playtest's <c>RoundPainter.BuildFx</c>):
    /// each walker as a live walk of the timeline (its depart, arrive, clear and return), from the gate's door along its
    /// route, its tiles going to its pod's place, in the material's plain cube and as the family hero tinted in the
    /// material's colour (<see cref="FxItem.Material"/>, FR-048); each cube cleared within
    /// <see cref="ClearStyles.RestoreSeconds"/> plays the style's restore. The cubes the style holds this frame
    /// (<see cref="Holds"/>) are drawn by the style, so the board shows their ground. Items are in cell units of the grid
    /// inside the frame (its top left at 0, 0; y down). Engine-free; presentation only.
    /// </summary>
    public sealed class RoundFx
    {
        private readonly Dictionary<int, List<(float X, float Y)>> _points = new Dictionary<int, List<(float X, float Y)>>();
        private readonly List<int> _gone = new List<int>();
        private readonly List<int> _heldCells = new List<int>();
        private bool[] _held = Array.Empty<bool>();
        private RoundBoardLayout? _layout;

        /// <summary>This frame's items.</summary>
        public FxList List { get; } = new FxList();

        /// <summary>The cells the style holds this frame.</summary>
        public IReadOnlyList<int> HeldCells => _heldCells;

        /// <summary>Whether the style holds cell <paramref name="cell"/>'s cube this frame (the board shows its ground).</summary>
        public bool Holds(int cell) => cell >= 0 && cell < _held.Length && _held[cell];

        /// <summary>The variant of a round material's family, for the kit's pieces that still take a variant.</summary>
        public static VariantId FamilyVariant(int material) => material < 0 ? VariantId.Leaf : RoundLook.WalkerVariant(RoundMaterial.All[material].Family);

        /// <summary>Forgets every walker's route (a new layout, a restart).</summary>
        public void Reset()
        {
            _points.Clear();
            List.Clear();
            ClearHeld();
            _layout = null;
        }

        /// <summary>
        /// Builds this frame's items from the animator at display time <paramref name="clockUs"/> in the board's
        /// <paramref name="layout"/>; <paramref name="seat"/> gives a pod's place tile in the same cell units (null while in none).
        /// </summary>
        public void Build(RoundAnimator a, RoundBoardLayout layout, ClearStyle style, long clockUs, Func<int, Box?> seat)
        {
            if (!ReferenceEquals(layout, _layout))
            {
                _points.Clear();
                _layout = layout;
            }

            if (_held.Length != layout.Width * layout.Height)
            {
                _held = new bool[layout.Width * layout.Height];
                _heldCells.Clear();
            }

            FxList list = List;
            list.Clear();
            ClearHeld();
            float now = clockUs / 1_000_000f;
            foreach (RoundWalk walk in a.ShownWalkers)
            {
                if (walk.Route.Count == 0)
                {
                    continue;
                }

                if (!_points.TryGetValue(walk.Id, out List<(float X, float Y)>? points))
                {
                    points = new List<(float X, float Y)>(walk.Route.Count);
                    foreach (int cell in walk.Route)
                    {
                        points.Add(InCells(layout, cell));
                    }

                    _points[walk.Id] = points;
                }

                ClearWalk live = ClearWalk.Live(points, walk.Material, FamilyVariant(walk.Material), walk.Start / 1_000_000f, walk.Arrive / 1_000_000f, walk.Clear / 1_000_000f, walk.Done / 1_000_000f, seat(walk.Pod));
                if (ClearLook.Holds(style, live, now) && walk.Cell >= 0 && walk.Cell < _held.Length && !_held[walk.Cell])
                {
                    _held[walk.Cell] = true;
                    _heldCells.Add(walk.Cell);
                }

                ClearLook.Walker(list, style, live, now);
            }

            ForgetHome(a);

            long restore = (long)(ClearStyles.RestoreSeconds * 1_000_000f);
            if (clockUs - a.LastEventAt >= restore + 1)
            {
                return;
            }

            int cells = layout.Width * layout.Height;
            for (int cell = 0; cell < cells; cell++)
            {
                long at = a.ClearedAt(cell);
                if (at < 0 || clockUs - at >= restore || a.Burst(cell))
                {
                    continue;
                }

                int material = a.OriginalMaterial(cell);
                ClearLook.Restore(list, style, new ClearFade(InCells(layout, cell), FamilyVariant(material), at / 1_000_000f, null, material), now);
            }
        }

        /// <summary>A rules cell's walker point (<see cref="RoundBoardLayout.Point"/>) in cell units of the grid.</summary>
        public static (float X, float Y) InCells(RoundBoardLayout layout, int cell)
        {
            (float px, float py) = layout.Point(cell);
            return ((px - layout.Grid.Left) / layout.Cell, (py - layout.Grid.Top) / layout.Cell);
        }

        /// <summary>A screen box (the layout's units) in cell units of the grid.</summary>
        public static Box InCells(RoundBoardLayout layout, Box box)
        {
            float ox = layout.Grid.Left;
            float oy = layout.Grid.Top;
            float cell = layout.Cell;
            return new Box((box.Left - ox) / cell, (box.Top - oy) / cell, (box.Right - ox) / cell, (box.Bottom - oy) / cell);
        }

        private void ClearHeld()
        {
            foreach (int cell in _heldCells)
            {
                if (cell < _held.Length)
                {
                    _held[cell] = false;
                }
            }

            _heldCells.Clear();
        }

        // Forgets the routes of walkers that are home.
        private void ForgetHome(RoundAnimator a)
        {
            if (_points.Count <= a.ShownWalkers.Count + 64)
            {
                return;
            }

            _gone.Clear();
            foreach (int id in _points.Keys)
            {
                bool walking = false;
                foreach (RoundWalk walk in a.ShownWalkers)
                {
                    walking |= walk.Id == id;
                }

                if (!walking)
                {
                    _gone.Add(id);
                }
            }

            foreach (int id in _gone)
            {
                _points.Remove(id);
            }
        }
    }
}
