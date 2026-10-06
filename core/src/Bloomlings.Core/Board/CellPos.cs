using System;

namespace Bloomlings.Core.Boards
{
    /// <summary>
    /// A board cell position. Row 0 is the bottom row; the Garden Entry is usually on it (FR-009).
    /// Boards are at most 22 columns × 28 rows (FR-008 as amended on 2026-10-06, the owner: it was 14 × 16), the size of
    /// the rare big levels; regular boards stay at most 288 cells.
    /// </summary>
    public readonly struct CellPos : IEquatable<CellPos>
    {
        public const int MaxWidth = 22;
        public const int MaxHeight = 28;

        /// <summary>Number of orthogonal neighbours; diagonals never count (FR-010).</summary>
        public const int NeighbourCount = 4;

        // The 4-neighbourhood in a fixed order: down, left, right, up.
        // The order is part of the deterministic rules; do not change it.
        private static readonly int[] s_dx = { 0, -1, 1, 0 };
        private static readonly int[] s_dy = { -1, 0, 0, 1 };

        public CellPos(int x, int y)
        {
            if (x < 0 || x >= MaxWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, $"x must be in 0..{MaxWidth - 1}.");
            }

            if (y < 0 || y >= MaxHeight)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, $"y must be in 0..{MaxHeight - 1}.");
            }

            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        /// <summary>Row-major index on a board of the given width.</summary>
        public int ToIndex(int width) => (Y * width) + X;

        public static CellPos FromIndex(int index, int width) => new CellPos(index % width, index / width);

        /// <summary>
        /// Gets the neighbour in direction <paramref name="direction"/> (0 = down, 1 = left, 2 = right, 3 = up)
        /// when it lies on a board of the given size.
        /// </summary>
        public bool TryGetNeighbour(int direction, int width, int height, out CellPos neighbour)
        {
            if (direction < 0 || direction >= NeighbourCount)
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            int nx = X + s_dx[direction];
            int ny = Y + s_dy[direction];
            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            {
                neighbour = default;
                return false;
            }

            neighbour = new CellPos(nx, ny);
            return true;
        }

        /// <summary>
        /// The fixed, anticipatable tie-break of FR-021 used to order reachable candidates:
        /// route distance ascending, then row ascending (bottom row first), then column ascending.
        /// </summary>
        public static int CompareCandidates(int distanceA, CellPos a, int distanceB, CellPos b)
        {
            if (distanceA != distanceB)
            {
                return distanceA < distanceB ? -1 : 1;
            }

            if (a.Y != b.Y)
            {
                return a.Y < b.Y ? -1 : 1;
            }

            if (a.X != b.X)
            {
                return a.X < b.X ? -1 : 1;
            }

            return 0;
        }

        public bool Equals(CellPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is CellPos other && Equals(other);

        /// <summary>
        /// Unique per cell. No rule or search result depends on it: rules and solvers walk cells by index, and a
        /// dictionary or set keyed by cells is never enumerated in hash order (.NET enumerates in insertion order), so
        /// changing <see cref="MaxWidth"/> changes no outcome (the golden replays pin this).
        /// </summary>
        public override int GetHashCode() => (Y * MaxWidth) + X;

        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(CellPos left, CellPos right) => left.Equals(right);

        public static bool operator !=(CellPos left, CellPos right) => !left.Equals(right);
    }
}
