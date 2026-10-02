using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// A Garden Entry's stone arch on the board (spec 005 contracts/look.md §3.6): the middle of its open base
    /// (<see cref="BaseX"/>, <see cref="BaseY"/>), its outer radius in pixels, the side of the board it stands on (its
    /// crown toward the board), and the door point inside its opening where the walkers appear.
    /// </summary>
    public readonly struct EntryArch
    {
        public EntryArch(float baseX, float baseY, float radius, EntrySide side)
        {
            BaseX = baseX;
            BaseY = baseY;
            Radius = radius;
            Side = side;
        }

        public float BaseX { get; }

        public float BaseY { get; }

        public float Radius { get; }

        public EntrySide Side { get; }

        /// <summary>Where the Bloomlings appear: inside the opening, 42% of the radius from its base toward the crown.</summary>
        public (float X, float Y) Door
        {
            get
            {
                float d = Radius * 0.42f;
                return Side switch
                {
                    EntrySide.Top => (BaseX, BaseY + d),
                    EntrySide.Left => (BaseX + d, BaseY),
                    EntrySide.Right => (BaseX - d, BaseY),
                    _ => (BaseX, BaseY - d),
                };
            }
        }
    }

    /// <summary>
    /// Where the board's pieces go inside the board region of the gameplay layout (spec 005 contracts/look.md §3.6,
    /// §4.1): the grid of cells, the stone border around it (a dark gap of <see cref="Gap"/> cell and stones
    /// <see cref="Stone"/> cell thick) and one stone arch per Garden Entry beyond the border on its side, after a strip of
    /// lawn. The cells take the largest size that fits the grid, the border and arches of at least <see cref="ArchMin"/>
    /// cells; the room the region has left in that direction lets the arches grow up to <see cref="ArchMax"/> cells.
    /// The whole group is centered in the region. Arches stay within the border's span and shrink so neighbors on one side
    /// never overlap. Both builds draw from it, so the board looks the same in each. Engine-free.
    /// </summary>
    public sealed class BoardLayout
    {
        /// <summary>The dark gap between the tiles and the stones, in cells.</summary>
        public const float Gap = 0.04f;

        /// <summary>The stone border's thickness, in cells.</summary>
        public const float Stone = 0.42f;

        /// <summary>How far the border reaches beyond the grid, in cells.</summary>
        public const float Rim = Gap + Stone;

        /// <summary>The lawn kept on each side between the region's edges and the border (or a side arch), in cells.</summary>
        public const float Margin = 0.2f;

        /// <summary>The strip of lawn between the border and an arch's crown, in cells.</summary>
        public const float ArchGap = 0.14f;

        /// <summary>An arch's smallest outer radius, in cells.</summary>
        public const float ArchMin = 1.2f;

        /// <summary>An arch's largest outer radius, in cells (three cells wide, as in the reference).</summary>
        public const float ArchMax = 1.5f;

        private BoardLayout(Box grid, float cell, int width, int height, IReadOnlyList<EntryArch> arches)
        {
            Grid = grid;
            Cell = cell;
            Width = width;
            Height = height;
            Arches = arches;
        }

        /// <summary>The grid of cells, in pixels.</summary>
        public Box Grid { get; }

        /// <summary>One cell's side, in pixels.</summary>
        public float Cell { get; }

        public int Width { get; }

        public int Height { get; }

        /// <summary>The arches, in the order of the entries they were laid out for.</summary>
        public IReadOnlyList<EntryArch> Arches { get; }

        /// <summary>The stone border's outer edge.</summary>
        public Box Outer => Grid.Inset(-Rim * Cell);

        /// <summary>The box of the cell at (<paramref name="x"/>, <paramref name="y"/>); y counts up from the bottom row.</summary>
        public Box CellBox(int x, int y)
        {
            float left = Grid.Left + (x * Cell);
            float top = Grid.Top + ((Height - 1 - y) * Cell);
            return new Box(left, top, left + Cell, top + Cell);
        }

        /// <summary>Lays out a <paramref name="width"/> × <paramref name="height"/> board with its entries inside <paramref name="area"/>.</summary>
        public static BoardLayout Fit(Box area, int width, int height, IReadOnlyList<EntryDef> entries)
        {
            int w = Math.Max(1, width);
            int h = Math.Max(1, height);
            var sides = new bool[4];
            foreach (EntryDef entry in entries)
            {
                sides[(int)entry.Side] = true;
            }

            bool left = sides[(int)EntrySide.Left];
            bool right = sides[(int)EntrySide.Right];
            bool top = sides[(int)EntrySide.Top];
            bool bottom = sides[(int)EntrySide.Bottom];
            float room = ArchGap + ArchMin;
            float needX = w + (2f * (Rim + Margin)) + (left ? room : 0f) + (right ? room : 0f);
            float needY = h + (2f * Rim) + (top ? room : 0f) + (bottom ? room : 0f);
            float cell = Math.Max(1f, Math.Min(area.Width / needX, area.Height / needY));

            // The room left in each direction goes to that direction's arches, up to ArchMax.
            int countX = (left ? 1 : 0) + (right ? 1 : 0);
            int countY = (top ? 1 : 0) + (bottom ? 1 : 0);
            float growX = countX == 0 ? 0f : Math.Min(ArchMax - ArchMin, Math.Max(0f, area.Width - (cell * needX)) / (cell * countX));
            float growY = countY == 0 ? 0f : Math.Min(ArchMax - ArchMin, Math.Max(0f, area.Height - (cell * needY)) / (cell * countY));
            var radius = new float[4];
            radius[(int)EntrySide.Left] = left ? ArchMin + growX : 0f;
            radius[(int)EntrySide.Right] = right ? ArchMin + growX : 0f;
            radius[(int)EntrySide.Top] = top ? ArchMin + growY : 0f;
            radius[(int)EntrySide.Bottom] = bottom ? ArchMin + growY : 0f;
            float Room(EntrySide side) => sides[(int)side] ? ArchGap + radius[(int)side] : 0f;

            float usedX = cell * (w + (2f * Rim) + Room(EntrySide.Left) + Room(EntrySide.Right));
            float usedY = cell * (h + (2f * Rim) + Room(EntrySide.Top) + Room(EntrySide.Bottom));
            float gridLeft = area.Left + ((area.Width - usedX) / 2f) + (cell * (Room(EntrySide.Left) + Rim));
            float gridTop = area.Top + ((area.Height - usedY) / 2f) + (cell * (Room(EntrySide.Top) + Rim));
            var grid = new Box(gridLeft, gridTop, gridLeft + (w * cell), gridTop + (h * cell));
            Box outer = grid.Inset(-Rim * cell);
            return new BoardLayout(grid, cell, w, h, PlaceArches(entries, grid, outer, cell, radius));
        }

        /// <summary>One arch per entry, on its side, centered on its cell where the border's span allows.</summary>
        private static EntryArch[] PlaceArches(IReadOnlyList<EntryDef> entries, Box grid, Box outer, float cell, float[] radiusCells)
        {
            var arches = new EntryArch[entries.Count];
            var along = new float[entries.Count];
            var radius = new float[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                EntryDef entry = entries[i];
                bool across = entry.Side == EntrySide.Left || entry.Side == EntrySide.Right;
                float r = radiusCells[(int)entry.Side] * cell;
                float center = across
                    ? grid.Top + ((grid.Height / cell) - 1 - entry.Cell.Y + 0.5f) * cell
                    : grid.Left + ((entry.Cell.X + 0.5f) * cell);
                float from = across ? outer.Top : outer.Left;
                float to = across ? outer.Bottom : outer.Right;
                along[i] = to - from <= 2f * r ? (from + to) / 2f : Math.Max(from + r, Math.Min(to - r, center));
                radius[i] = r;
            }

            // Neighbors on one side share the room between them.
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = 0; j < entries.Count; j++)
                {
                    if (i != j && entries[i].Side == entries[j].Side)
                    {
                        float half = Math.Abs(along[i] - along[j]) / 2f;
                        radius[i] = Math.Max(cell * 0.6f, Math.Min(radius[i], half));
                    }
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                float r = radius[i];
                float reach = (ArchGap * cell) + r;
                arches[i] = entries[i].Side switch
                {
                    EntrySide.Top => new EntryArch(along[i], outer.Top - reach, r, EntrySide.Top),
                    EntrySide.Left => new EntryArch(outer.Left - reach, along[i], r, EntrySide.Left),
                    EntrySide.Right => new EntryArch(outer.Right + reach, along[i], r, EntrySide.Right),
                    _ => new EntryArch(along[i], outer.Bottom + reach, r, EntrySide.Bottom),
                };
            }

            return arches;
        }
    }
}
