using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Which side of each segment of a walker's way has room for its lane (the owner's Trails of 2026-11-04,
    /// <see cref="WalkerLook.Trails"/>): a walker going out keeps <see cref="WalkerLook.TrailLane"/> walker boxes to the right of the
    /// way's centre line and one coming home to the left, but only where that side is open ground (a path cell, an eaten cell or
    /// the board's empty surface round the rows), so a lane never runs over cubes; where it is not, the walker keeps to the
    /// centre line. Measured once a walker, when the host first sees it (each host caches it with the walker's way). Engine-free,
    /// presentation only; points are in the walkers' cell units (<see cref="ClearWalk.Points"/>).
    /// </summary>
    public static class TrailLanes
    {
        /// <summary>The right of the way (toward its cube) is free: the outward lane's side.</summary>
        public const byte Right = 1;

        /// <summary>The left of the way is free: the homeward lane's side.</summary>
        public const byte Left = 2;

        // Where across a lane its walker stands (shares of the walker box from the centre line) and along a segment we look.
        private static readonly float[] Across = { 0.15f, 0.45f, 0.75f };
        private static readonly float[] Along = { 0.15f, 0.5f, 0.85f };

        /// <summary>
        /// The free sides of each segment of <paramref name="points"/> for a walker of <paramref name="walkerCells"/> cells:
        /// <see cref="Right"/> and/or <see cref="Left"/>. <paramref name="cellAt"/> gives the rules cell under a point
        /// (<see cref="AsideSpots.Outside"/> for the board's empty surface round the rows, -1 off the board), <paramref name="open"/>
        /// whether a cell is open ground now.
        /// </summary>
        public static byte[] Flags(IReadOnlyList<(float X, float Y)> points, float walkerCells, Func<float, float, int> cellAt, Func<int, bool> open)
        {
            int segments = Math.Max(0, points.Count - 1);
            var flags = new byte[Math.Max(1, segments)];
            for (int i = 0; i < segments; i++)
            {
                (float ax, float ay) = points[i];
                (float bx, float by) = points[i + 1];
                float dx = bx - ax;
                float dy = by - ay;
                float len = (float)Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 1e-4f)
                {
                    continue;
                }

                float nx = -dy / len;
                float ny = dx / len;
                byte f = 0;
                if (Free(ax, ay, dx, dy, nx, ny, 1f, walkerCells, cellAt, open))
                {
                    f |= Right;
                }

                if (Free(ax, ay, dx, dy, nx, ny, -1f, walkerCells, cellAt, open))
                {
                    f |= Left;
                }

                flags[i] = f;
            }

            return flags;
        }

        /// <summary><see cref="Flags(IReadOnlyList{ValueTuple{float, float}}, float, Func{float, float, int}, Func{int, bool})"/> on a round board now.</summary>
        public static byte[] Flags(IReadOnlyList<(float X, float Y)> points, float walkerCells, RoundBoardLayout layout, RoundAnimator a) =>
            Flags(points, walkerCells, (x, y) =>
            {
                float px = layout.Grid.Left + (x * layout.Cell);
                float py = layout.Grid.Top + (y * layout.Cell);
                int cell = layout.CellAt(px, py);
                return cell >= 0 ? cell : layout.Surface.Inset(layout.Cell * 0.5f).Contains(px, py) ? AsideSpots.Outside : -1;
            }, cell => layout.KindOf(cell) == RoundCellKind.Path || (layout.KindOf(cell) == RoundCellKind.Material && a.MaterialAt(cell) < 0));

        private static bool Free(float ax, float ay, float dx, float dy, float nx, float ny, float side, float w, Func<float, float, int> cellAt, Func<int, bool> open)
        {
            foreach (float t in Along)
            {
                foreach (float k in Across)
                {
                    int cell = cellAt(ax + (dx * t) + (nx * side * k * w), ay + (dy * t) + (ny * side * k * w));
                    if (cell == -1 || (cell >= 0 && !open(cell)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
