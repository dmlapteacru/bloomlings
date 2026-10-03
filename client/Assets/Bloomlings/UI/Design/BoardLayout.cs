using System;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Where the board's pieces go inside the board region of the gameplay layout (spec 005 contracts/look.md §3.6,
    /// §4.1): the grid of cells and the stone border around it (a dark gap of <see cref="Gap"/> cell and stones
    /// <see cref="Stone"/> cell thick), with <see cref="Margin"/> cell of lawn kept at its left and right. The cells take
    /// the largest size that fits the region (and the widest border the caller allows), and the board is centered in the
    /// region. A Garden Entry has no picture of its own (the owner, 2026-10-03: no stone arch, and the board keeps the
    /// arch's room): its Bloomlings set off from the stone border beside the entry cell (<see cref="Door"/>). Both builds
    /// draw from it, so the board looks the same in each. Engine-free.
    /// </summary>
    public sealed class BoardLayout
    {
        /// <summary>The dark gap between the tiles and the stones, in cells.</summary>
        public const float Gap = 0.04f;

        /// <summary>The stone border's thickness, in cells.</summary>
        public const float Stone = 0.42f;

        /// <summary>How far the border reaches beyond the grid, in cells.</summary>
        public const float Rim = Gap + Stone;

        /// <summary>The lawn kept at the left and the right between the region's edges and the border, in cells.</summary>
        public const float Margin = 0.2f;

        /// <summary>
        /// From an entry cell's center to where its Bloomlings set off, in cells: the middle of the stone border beside the
        /// cell, on the entry's side.
        /// </summary>
        public const float DoorReach = 0.5f + Gap + (Stone / 2f);

        private BoardLayout(Box grid, float cell, int width, int height)
        {
            Grid = grid;
            Cell = cell;
            Width = width;
            Height = height;
        }

        /// <summary>The grid of cells, in pixels.</summary>
        public Box Grid { get; }

        /// <summary>One cell's side, in pixels.</summary>
        public float Cell { get; }

        public int Width { get; }

        public int Height { get; }

        /// <summary>The stone border's outer edge.</summary>
        public Box Outer => Grid.Inset(-Rim * Cell);

        /// <summary>The box of the cell at (<paramref name="x"/>, <paramref name="y"/>); y counts up from the bottom row.</summary>
        public Box CellBox(int x, int y)
        {
            float left = Grid.Left + (x * Cell);
            float top = Grid.Top + ((Height - 1 - y) * Cell);
            return new Box(left, top, left + Cell, top + Cell);
        }

        /// <summary>Where the Bloomlings of <paramref name="entry"/> set off (<see cref="DoorOf"/> its cell's box).</summary>
        public (float X, float Y) Door(EntryDef entry) => DoorOf(CellBox(entry.Cell.X, entry.Cell.Y), entry.Side);

        /// <summary>
        /// Where the Bloomlings of an entry on <paramref name="side"/> set off, for the entry cell's <paramref name="cell"/>
        /// box (top-down): <see cref="DoorReach"/> cells from the cell's center toward that side, on the stone border.
        /// </summary>
        public static (float X, float Y) DoorOf(Box cell, EntrySide side)
        {
            float d = cell.Width * DoorReach;
            return side switch
            {
                EntrySide.Top => (cell.CenterX, cell.CenterY - d),
                EntrySide.Left => (cell.CenterX - d, cell.CenterY),
                EntrySide.Right => (cell.CenterX + d, cell.CenterY),
                _ => (cell.CenterX, cell.CenterY + d),
            };
        }

        /// <summary>
        /// Lays out a <paramref name="width"/> × <paramref name="height"/> board inside <paramref name="area"/>, centered:
        /// the cells as large as the grid, its border and the lawn margins allow, with the border's outer box at most
        /// <paramref name="maxOuterWidth"/> wide.
        /// </summary>
        public static BoardLayout Fit(Box area, int width, int height, float maxOuterWidth = float.MaxValue)
        {
            int w = Math.Max(1, width);
            int h = Math.Max(1, height);
            float needX = w + (2f * (Rim + Margin));
            float needY = h + (2f * Rim);
            float cell = Math.Min(Math.Min(area.Width / needX, area.Height / needY), maxOuterWidth / (w + (2f * Rim)));
            cell = Math.Max(1f, cell);
            float gridLeft = area.CenterX - (w * cell / 2f);
            float gridTop = area.CenterY - (h * cell / 2f);
            var grid = new Box(gridLeft, gridTop, gridLeft + (w * cell), gridTop + (h * cell));
            return new BoardLayout(grid, cell, w, h);
        }
    }
}
