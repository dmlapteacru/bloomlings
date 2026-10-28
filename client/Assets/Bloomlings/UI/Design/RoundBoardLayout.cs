using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>What a cell of a round level's rows is on screen (<see cref="RoundBoardLayout.KindOf(int)"/>).</summary>
    public enum RoundCellKind
    {
        /// <summary>The rows' outer wall: drawn as the game's stone border, never as a cell.</summary>
        Frame,

        /// <summary>An entry in the wall: the game's entry arch, where the walkers set off.</summary>
        Gate,

        /// <summary>An open cell inside the frame (the walkway ring and the picture's background): grass.</summary>
        Path,

        /// <summary>A wall inside the frame that is the picture's own strip: the stream's water.</summary>
        Stream,

        /// <summary>
        /// A wall laid over the picture (the level's <see cref="RoundLevel.Overlay"/>, 17 §8 as amended on 2026-10-28): a stone
        /// wall (<see cref="RoundBoardLayout.WallSlot"/>).
        /// </summary>
        Wall,

        /// <summary>
        /// A bridge site of the Sprig bridge: its strip's ground (water, or stone on an overlay wall,
        /// <see cref="RoundBridgeSite.OnWall"/>) until its bridge is built, then a plank.
        /// </summary>
        BridgeSite,

        /// <summary>A material cell: the game's soft cube in the material's colour (<see cref="RoundLook"/>).</summary>
        Material,
    }

    /// <summary>A gate of a round level: its cell in the wall, the side it opens to, and where its arch and door are.</summary>
    public sealed class RoundGate
    {
        internal RoundGate(int cell, EntrySide side, Box innerCell)
        {
            Cell = cell;
            Side = side;
            InnerCell = innerCell;
        }

        /// <summary>The gate's cell index in the rows (the level's <c>entries</c>).</summary>
        public int Cell { get; }

        /// <summary>The wall the gate is in (a gate in the bottom row opens at the bottom).</summary>
        public EntrySide Side { get; }

        /// <summary>The box of the cell inside the frame next to the gate.</summary>
        public Box InnerCell { get; }

        /// <summary>Where the walkers set off: on the stone border beside <see cref="InnerCell"/> (<see cref="BoardLayout.DoorOf"/>).</summary>
        public (float X, float Y) Door => BoardLayout.DoorOf(InnerCell, Side);

        /// <summary>The entry arch's box and its clockwise turn (<see cref="BoardLayout.ArchOf"/>).</summary>
        public (Box Box, float Degrees) Arch => BoardLayout.ArchOf(InnerCell, Side);
    }

    /// <summary>Which way a Sprig bridge crosses its strip on screen, from its anchor on the near bank through its site.</summary>
    public enum RoundCrossing
    {
        /// <summary>The anchor is the cell below the site (a strip across the board).</summary>
        Up,

        /// <summary>The anchor is the cell above the site.</summary>
        Down,

        /// <summary>The anchor is the cell right of the site (a strip along the board, the far bank on the left).</summary>
        Left,

        /// <summary>The anchor is the cell left of the site (the far bank on the right).</summary>
        Right,
    }

    /// <summary>A site of the Sprig bridge: the strip cell it opens, its anchor on the near bank, and its plank.</summary>
    public sealed class RoundBridgeSite
    {
        internal RoundBridgeSite(int index, int cell, int anchor, string label, Box site, Box anchorCell, RoundCrossing crossing, bool onWall)
        {
            Index = index;
            Cell = cell;
            Anchor = anchor;
            Label = label;
            Site = site;
            AnchorCell = anchorCell;
            Crossing = crossing;
            OnWall = onWall;
        }

        /// <summary>The choice's index in the level's bridge (the rules' <c>choice</c>).</summary>
        public int Index { get; }

        public int Cell { get; }

        public int Anchor { get; }

        public string Label { get; }

        /// <summary>The site cell's box.</summary>
        public Box Site { get; }

        /// <summary>The anchor cell's box (the near bank the approach must reach).</summary>
        public Box AnchorCell { get; }

        /// <summary>
        /// Which way the bridge crosses (17 §8 as amended on 2026-10-28: up, or left or right over a strip along the board;
        /// down for an anchor above the site).
        /// </summary>
        public RoundCrossing Crossing { get; }

        /// <summary>Whether the plank runs up and down (the anchor is a row below or above the site), else left to right.</summary>
        public bool Across => Crossing == RoundCrossing.Up || Crossing == RoundCrossing.Down;

        /// <summary>Whether the site is in a wall laid over the picture: its ground is stone, not water.</summary>
        public bool OnWall { get; }

        /// <summary>
        /// Where a buildable site glows: the site with <see cref="RoundBoardLayout.GlowReach"/> of a cell onto its own near
        /// bank, toward its anchor, so the glow shows the side the bridge is built from.
        /// </summary>
        public Box Glow
        {
            get
            {
                float r = Site.Width * RoundBoardLayout.GlowReach;
                return Crossing switch
                {
                    RoundCrossing.Up => new Box(Site.Left, Site.Top, Site.Right, Site.Bottom + r),
                    RoundCrossing.Down => new Box(Site.Left, Site.Top - r, Site.Right, Site.Bottom),
                    RoundCrossing.Left => new Box(Site.Left, Site.Top, Site.Right + r, Site.Bottom),
                    _ => new Box(Site.Left - r, Site.Top, Site.Right, Site.Bottom),
                };
            }
        }

        /// <summary>
        /// The built bridge's plank before it is turned: <see cref="RoundBoardLayout.PlankReach"/> beyond the site onto each
        /// bank, <see cref="RoundBoardLayout.PlankWidth"/> of a cell wide, round the site's middle.
        /// </summary>
        public Box Plank => Box.FromCenter(Site.CenterX, Site.CenterY, Site.Width * (1f + (2f * RoundBoardLayout.PlankReach)), Site.Width * RoundBoardLayout.PlankWidth);

        /// <summary>
        /// The plank picture's clockwise turn to its crossing: 90° for a bridge up or down (over a strip across the board),
        /// 0° for one left or right (over a strip along it). The plank is the same at both ends, so a crossing and its
        /// opposite share a turn and the plank keeps the light from the upper left.
        /// </summary>
        public float PlankDegrees => Across ? 90f : 0f;

        /// <summary>The plank's screen bounds once turned.</summary>
        public Box PlankBounds => Across ? Box.FromCenter(Plank.CenterX, Plank.CenterY, Plank.Height, Plank.Width) : Plank;
    }

    /// <summary>
    /// Where a round level's board goes on screen (spec 006 FR-040, research R7, contracts/presentation.md), from its
    /// <c>rowsBottomFirst</c> exactly as the rules read them: the rows' outer wall (the frame) and its gate are drawn as the
    /// game's existing stone border and entry arch, so the cells inside the wall are the game's grid (<see cref="Board"/>, a
    /// <see cref="BoardLayout"/> of (w − 2) × (h − 2) cells). Inside the frame a <c>#</c> is the stream's water (a stone
    /// wall where the level's overlay laid it), a bridge site is its strip's ground until its bridge is built and then a
    /// plank turned to its crossing (up, down, left or right), an open cell is grass, and a material cell is the game's
    /// soft cube (<see cref="RoundLook"/>). Boards fit up to <see cref="MaxSide"/> × <see cref="MaxSide"/> with the frame.
    /// Cell indices are the rules' (<c>y × w + x</c>, row 0 at the bottom). Engine-free; presentation only.
    /// </summary>
    public sealed class RoundBoardLayout
    {
        /// <summary>The stream's asset slot (<see cref="UiRaster.Water"/>).</summary>
        public const string WaterSlot = "board.water";

        /// <summary>The built bridge's asset slot (its plank, <see cref="RoundBridgeSite.Plank"/>).</summary>
        public const string BridgeSlot = "board.bridge";

        /// <summary>A wall laid over the picture's asset slot (<see cref="UiRaster.WallStone"/>, <see cref="RoundCellKind.Wall"/>).</summary>
        public const string WallSlot = "board.wall";

        /// <summary>The largest board side with the frame (<see cref="RoundLevel.MaxSide"/>).</summary>
        public const int MaxSide = RoundLevel.MaxSide;

        /// <summary>How much a cube is inset into its cell (each side, in cells): the cubes nearly touch, as on the game's board.</summary>
        public const float CubeInset = 0.008f;

        /// <summary>How far a built bridge's plank reaches beyond its site onto each bank, in cells.</summary>
        public const float PlankReach = 0.22f;

        /// <summary>A built bridge's plank width, in cells.</summary>
        public const float PlankWidth = 0.78f;

        /// <summary>How far a buildable site's glow reaches onto its near bank, in cells (<see cref="RoundBridgeSite.Glow"/>).</summary>
        public const float GlowReach = 0.25f;

        private readonly RoundCellKind[] _kinds;
        private readonly sbyte[] _materials;
        private readonly int[] _entries;
        private readonly IReadOnlyList<RoundBridgeChoice> _bridge;

        private readonly bool[] _stone;

        private RoundBoardLayout(BoardLayout board, int width, int height, RoundCellKind[] kinds, sbyte[] materials, int[] entries, IReadOnlyList<RoundBridgeChoice> bridge, bool[] stone)
        {
            Board = board;
            Width = width;
            Height = height;
            _kinds = kinds;
            _materials = materials;
            _entries = entries;
            _bridge = bridge;
            _stone = stone;

            var gates = new List<RoundGate>();
            foreach (int cell in entries)
            {
                if (cell < 0 || cell >= width * height)
                {
                    continue;
                }

                int x = cell % width;
                int y = cell / width;
                if (IsInner(x, y))
                {
                    continue;
                }

                gates.Add(new RoundGate(cell, SideOf(x, y), InnerBox(x, y)));
            }

            Gates = gates;

            var sites = new List<RoundBridgeSite>();
            for (int i = 0; i < bridge.Count; i++)
            {
                RoundBridgeChoice choice = bridge[i];
                int d = choice.Cell - choice.Anchor;
                RoundCrossing crossing = d == width ? RoundCrossing.Up : d == -width ? RoundCrossing.Down : d == -1 ? RoundCrossing.Left : RoundCrossing.Right;
                bool onWall = choice.Cell >= 0 && choice.Cell < stone.Length && stone[choice.Cell];
                sites.Add(new RoundBridgeSite(i, choice.Cell, choice.Anchor, choice.Label, CellBox(choice.Cell), CellBox(choice.Anchor), crossing, onWall));
            }

            Bridges = sites;
        }

        /// <summary>The game's grid of the cells inside the frame, with its stone border (<see cref="BoardLayout.Outer"/>).</summary>
        public BoardLayout Board { get; }

        /// <summary>The rows' width, the frame included.</summary>
        public int Width { get; }

        /// <summary>The rows' height, the frame included.</summary>
        public int Height { get; }

        /// <summary>One cell's side, in pixels.</summary>
        public float Cell => Board.Cell;

        /// <summary>The cells inside the frame.</summary>
        public Box Grid => Board.Grid;

        /// <summary>The stone border's outer edge (the frame on screen).</summary>
        public Box Outer => Board.Outer;

        /// <summary>The gates in the wall, in the level's <c>entries</c> order.</summary>
        public IReadOnlyList<RoundGate> Gates { get; }

        /// <summary>The Sprig bridge's sites, in the level's choice order (empty without a bridge).</summary>
        public IReadOnlyList<RoundBridgeSite> Bridges { get; }

        /// <summary>
        /// Lays out <paramref name="level"/>'s board inside <paramref name="area"/> (<see cref="BoardLayout.Fit"/> of the cells
        /// inside the frame), the stone border at most <paramref name="maxOuterWidth"/> wide.
        /// </summary>
        public static RoundBoardLayout Fit(Box area, RoundLevel level, float maxOuterWidth = float.MaxValue) =>
            Fit(area, level.Width, level.Height, level.RowsBottomFirst, level.Entries, level.Bridge, maxOuterWidth, level.OverlayCells());

        /// <summary>
        /// Lays out a round board of <paramref name="width"/> × <paramref name="height"/> cells (the frame included) from its
        /// <paramref name="rowsBottomFirst"/>, its gates (<paramref name="entries"/>) and its bridge sites, inside
        /// <paramref name="area"/>. The walls among <paramref name="overlay"/>'s cells (the level's overlay,
        /// <see cref="RoundLevel.OverlayCells"/>) are a stone wall instead of water.
        /// </summary>
        public static RoundBoardLayout Fit(Box area, int width, int height, IReadOnlyList<string> rowsBottomFirst, IReadOnlyList<int> entries, IReadOnlyList<RoundBridgeChoice>? bridge, float maxOuterWidth = float.MaxValue, IReadOnlyList<int>? overlay = null)
        {
            if (width < 3 || height < 3 || width > MaxSide || height > MaxSide)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A round board is 3 to " + MaxSide + " cells a side, its frame included.");
            }

            if (rowsBottomFirst.Count != height)
            {
                throw new ArgumentException("A round board needs one row per cell of its height.", nameof(rowsBottomFirst));
            }

            var entrySet = new HashSet<int>(entries);
            var siteSet = new HashSet<int>();
            if (bridge != null)
            {
                foreach (RoundBridgeChoice choice in bridge)
                {
                    siteSet.Add(choice.Cell);
                }
            }

            var stone = new bool[width * height];
            if (overlay != null)
            {
                foreach (int cell in overlay)
                {
                    if (cell >= 0 && cell < stone.Length)
                    {
                        stone[cell] = true;
                    }
                }
            }

            var kinds = new RoundCellKind[width * height];
            var materials = new sbyte[width * height];
            for (int y = 0; y < height; y++)
            {
                string row = rowsBottomFirst[y];
                if (row.Length != width)
                {
                    throw new ArgumentException("Every row of a round board is as long as its width.", nameof(rowsBottomFirst));
                }

                for (int x = 0; x < width; x++)
                {
                    int i = (y * width) + x;
                    char c = row[x];
                    bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    int material = c == '.' || c == '#' ? -1 : RoundMaterial.IndexOf(c);
                    if (c != '.' && c != '#' && material < 0)
                    {
                        throw new ArgumentException("Unknown round board cell '" + c + "'.", nameof(rowsBottomFirst));
                    }

                    materials[i] = (sbyte)material;
                    kinds[i] = entrySet.Contains(i) && edge ? RoundCellKind.Gate
                        : edge ? RoundCellKind.Frame
                        : siteSet.Contains(i) ? RoundCellKind.BridgeSite
                        : c == '#' ? (stone[i] ? RoundCellKind.Wall : RoundCellKind.Stream)
                        : material >= 0 ? RoundCellKind.Material
                        : RoundCellKind.Path;
                }
            }

            int[] entryCells = new int[entries.Count];
            for (int e = 0; e < entryCells.Length; e++)
            {
                entryCells[e] = entries[e];
            }

            BoardLayout board = BoardLayout.Fit(area, width - 2, height - 2, maxOuterWidth);
            return new RoundBoardLayout(board, width, height, kinds, materials, entryCells, bridge ?? Array.Empty<RoundBridgeChoice>(), stone);
        }

        /// <summary>
        /// The board in the reference gameplay regions, as <see cref="ReferenceGameplayRegions.FitBoard"/> lays out the game's
        /// boards: over the board area less 0.02 W at each side, the stone border at most
        /// <see cref="ReferenceGameplayRegions.MaxBoardShare"/> × W wide, docked on the area's bottom.
        /// </summary>
        public static RoundBoardLayout In(ReferenceGameplayRegions regions, RoundLevel level) =>
            Fit(regions.BoardArea.Inset(regions.W * 0.02f, 0f), level, regions.W * ReferenceGameplayRegions.MaxBoardShare).DockBottom(regions.BoardArea.Bottom);

        /// <summary>The same board with its stone border's bottom on <paramref name="bottom"/> (<see cref="BoardLayout.DockBottom"/>).</summary>
        public RoundBoardLayout DockBottom(float bottom) => new RoundBoardLayout(Board.DockBottom(bottom), Width, Height, _kinds, _materials, _entries, _bridge, _stone);

        /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is inside the frame (row 0 at the bottom).</summary>
        public bool IsInner(int x, int y) => x > 0 && y > 0 && x < Width - 1 && y < Height - 1;

        /// <summary>What cell <paramref name="cell"/> is on screen.</summary>
        public RoundCellKind KindOf(int cell) => _kinds[cell];

        public RoundCellKind KindOf(int x, int y) => _kinds[(y * Width) + x];

        /// <summary>Whether a cell's ground is the stone wall laid over the picture (an overlay wall or site), not water.</summary>
        public bool IsStone(int cell) => _stone[cell];

        /// <summary>The material index of a cell in the rows (<see cref="RoundMaterial.Index"/>), or -1 for a cell without one.</summary>
        public int MaterialOf(int cell) => _materials[cell];

        /// <summary>
        /// The box of cell <paramref name="cell"/> on screen: a cell inside the frame is its grid cell; a frame cell's box lies
        /// a cell beyond the grid (under the stone border), as the grid would continue.
        /// </summary>
        public Box CellBox(int cell) => CellBox(cell % Width, cell / Width);

        /// <summary>The box of the cell at (<paramref name="x"/>, <paramref name="y"/>) of the rows (row 0 at the bottom).</summary>
        public Box CellBox(int x, int y) => Board.CellBox(x - 1, y - 1);

        /// <summary>A cube's box in its cell (<see cref="CubeInset"/>).</summary>
        public Box CubeBox(int cell) => CellBox(cell).Inset(Cell * CubeInset);

        /// <summary>
        /// Where a walker stands on cell <paramref name="cell"/> of its route: the middle of a cell inside the frame, or on
        /// the stone border beside the nearest inside cell for a frame cell or a gate (a gate's <see cref="RoundGate.Door"/>).
        /// </summary>
        public (float X, float Y) Point(int cell)
        {
            int x = cell % Width;
            int y = cell / Width;
            if (IsInner(x, y))
            {
                Box box = CellBox(x, y);
                return (box.CenterX, box.CenterY);
            }

            return BoardLayout.DoorOf(InnerBox(x, y), SideOf(x, y));
        }

        /// <summary>The wall a frame cell is in: the bottom or top row first, then the left or right column.</summary>
        private EntrySide SideOf(int x, int y) =>
            y == 0 ? EntrySide.Bottom : y == Height - 1 ? EntrySide.Top : x == 0 ? EntrySide.Left : EntrySide.Right;

        /// <summary>The box of the inside cell nearest to (<paramref name="x"/>, <paramref name="y"/>).</summary>
        private Box InnerBox(int x, int y) =>
            CellBox(Math.Max(1, Math.Min(Width - 2, x)), Math.Max(1, Math.Min(Height - 2, y)));
    }
}
