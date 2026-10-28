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

        /// <summary>
        /// An open cell inside the frame (the walkway ring and the picture's background): the board's neutral surface shows
        /// through, nothing is drawn on it (the owner's decision of 2026-10-28).
        /// </summary>
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

        /// <summary>
        /// A material cell: the game's soft cube in the material's colour (<see cref="RoundLook"/>); once eaten, the neutral
        /// surface shows, no imprint of the cube (the owner's decision of 2026-10-28).
        /// </summary>
        Material,
    }

    /// <summary>A gate of a round level: its cell in the wall, the side it opens to, and where its arch and door are.</summary>
    public sealed class RoundGate
    {
        internal RoundGate(int cell, EntrySide side, Box innerCell, (float X, float Y) door, (Box Box, float Degrees) arch)
        {
            Cell = cell;
            Side = side;
            InnerCell = innerCell;
            Door = door;
            Arch = arch;
        }

        /// <summary>The gate's cell index in the rows (the level's <c>entries</c>).</summary>
        public int Cell { get; }

        /// <summary>The wall the gate is in (a gate in the bottom row opens at the bottom).</summary>
        public EntrySide Side { get; }

        /// <summary>The box of the cell inside the frame next to the gate.</summary>
        public Box InnerCell { get; }

        /// <summary>
        /// Where the walkers set off: the middle of the fixed stone frame, in line with <see cref="InnerCell"/>
        /// (<see cref="RoundBoardLayout.Point"/>).
        /// </summary>
        public (float X, float Y) Door { get; }

        /// <summary>The entry arch's box and its clockwise turn, sized by the frame's unit (<see cref="BoardLayout.ArchAt"/>).</summary>
        public (Box Box, float Degrees) Arch { get; }
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
    /// A piece of a strip carried on beyond the grid to the frame (<see cref="RoundBoardLayout.StripPieces"/>): the strip's ring
    /// cell, the virtual column and row the piece stands at (for the picture's seed) and its box, one cell in size.
    /// </summary>
    public readonly struct RoundStripPiece
    {
        public RoundStripPiece(int cell, int x, int y, Box box)
        {
            Cell = cell;
            X = x;
            Y = y;
            Box = box;
        }

        /// <summary>The ring cell of the strip the piece carries on (a <see cref="RoundCellKind.Stream"/> or <see cref="RoundCellKind.Wall"/>).</summary>
        public int Cell { get; }

        /// <summary>The column the piece stands at, as the rows would continue (negative left of the rows).</summary>
        public int X { get; }

        /// <summary>The row the piece stands at, as the rows would continue (row 0 at the bottom).</summary>
        public int Y { get; }

        /// <summary>The piece's box: one cell, the last one of a run laid against the frame.</summary>
        public Box Box { get; }
    }

    /// <summary>
    /// Where a round level's board goes on screen (spec 006 FR-040, research R7, contracts/presentation.md; the owner's board
    /// of 2026-10-28), from its <c>rowsBottomFirst</c> exactly as the rules read them:
    /// <list type="bullet">
    /// <item><description>One fixed board box for every level (<see cref="Outer"/>, <see cref="ReferenceGameplayRegions.RoundBoard"/>):
    /// its stone frame always <see cref="FrameThickness"/> thick (<see cref="Unit"/> = the box's shorter side over
    /// <see cref="FrameUnits"/>), whatever the picture's size. The rows' outer wall is that frame, never drawn per cell; a gate
    /// is the entry arch in the frame in line with its cell.</description></item>
    /// <item><description>Inside the frame one solid neutral surface (<see cref="Surface"/>, <see cref="SurfaceSlot"/>): no
    /// grid of cells, no grass, and no imprint where a cube was eaten.</description></item>
    /// <item><description>The cells inside the wall (the walkway ring and the picture) take the largest cube that fits the
    /// surface (<see cref="Cell"/>), keeping the picture's proportions, centred (<see cref="Grid"/>): a big matrix gets smaller
    /// cubes, a small one bigger cubes. Everything on the board (cubes, water, walls, planks, walkers) derives from that
    /// size.</description></item>
    /// </list>
    /// Inside the wall a <c>#</c> is the stream's water (a stone wall where the level's overlay laid it), carried on to the
    /// frame where it closes the ring (<see cref="StripPieces"/>); a bridge site is its strip's ground until its bridge is built and
    /// then a plank turned to its crossing (up, down, left or right); a material cell is the game's soft cube
    /// (<see cref="RoundLook"/>). Boards fit up to <see cref="MaxSide"/> × <see cref="MaxSide"/> with the wall. Cell indices
    /// are the rules' (<c>y × w + x</c>, row 0 at the bottom). Engine-free; presentation only.
    /// </summary>
    public sealed class RoundBoardLayout
    {
        /// <summary>The stream's asset slot (<see cref="UiRaster.Water"/>).</summary>
        public const string WaterSlot = "board.water";

        /// <summary>The built bridge's asset slot (its plank, <see cref="RoundBridgeSite.Plank"/>).</summary>
        public const string BridgeSlot = "board.bridge";

        /// <summary>A wall laid over the picture's asset slot (<see cref="UiRaster.WallStone"/>, <see cref="RoundCellKind.Wall"/>).</summary>
        public const string WallSlot = "board.wall";

        /// <summary>The board's solid neutral surface inside the frame (<see cref="UiRaster.BoardSurface"/>, <see cref="Surface"/>).</summary>
        public const string SurfaceSlot = "board.surface";

        /// <summary>The largest board side with the frame (<see cref="RoundLevel.MaxSide"/>).</summary>
        public const int MaxSide = RoundLevel.MaxSide;

        /// <summary>
        /// The fixed box's shorter side in frame units (<see cref="Unit"/>): the stone frame is <see cref="BoardLayout.Rim"/>
        /// units thick and its blocks about a unit long, on every level.
        /// </summary>
        public const float FrameUnits = 22f;

        /// <summary>The surface's corner radius in frame units (the frame's dark gap round it is <see cref="BoardLayout.Gap"/>).</summary>
        public const float SurfaceRadius = 0.12f;

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

        private RoundBoardLayout(Box outer, int width, int height, RoundCellKind[] kinds, sbyte[] materials, int[] entries, IReadOnlyList<RoundBridgeChoice> bridge, bool[] stone)
        {
            Outer = outer;
            Width = width;
            Height = height;
            _kinds = kinds;
            _materials = materials;
            _entries = entries;
            _bridge = bridge;
            _stone = stone;

            Unit = Math.Max(1f, Math.Min(outer.Width, outer.Height) / FrameUnits);
            Surface = outer.Inset(BoardLayout.Rim * Unit);
            int columns = width - 2;
            int rows = height - 2;
            Cell = Math.Max(1f, Math.Min(Surface.Width / columns, Surface.Height / rows));
            Grid = Box.FromCenter(Surface.CenterX, Surface.CenterY, Cell * columns, Cell * rows);

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

                EntrySide side = SideOf(x, y);
                (float X, float Y) door = DoorAt(x, y);
                gates.Add(new RoundGate(cell, side, InnerBox(x, y), door, BoardLayout.ArchAt(door, side, Unit)));
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
            StripPieces = Pieces();
        }

        /// <summary>The rows' width, the wall included.</summary>
        public int Width { get; }

        /// <summary>The rows' height, the wall included.</summary>
        public int Height { get; }

        /// <summary>The columns inside the wall (the walkway ring and the picture): the cells the cube size is fitted to.</summary>
        public int Columns => Width - 2;

        /// <summary>The rows inside the wall.</summary>
        public int Rows => Height - 2;

        /// <summary>The fixed board box: the stone frame's outer edge, the same for every level of a screen.</summary>
        public Box Outer { get; }

        /// <summary>The frame's unit in pixels: <see cref="Outer"/>'s shorter side over <see cref="FrameUnits"/>.</summary>
        public float Unit { get; }

        /// <summary>The stone frame's thickness in pixels (its dark gap included): the same on every level.</summary>
        public float FrameThickness => BoardLayout.Rim * Unit;

        /// <summary>The solid neutral surface inside the frame: the board's background on every level.</summary>
        public Box Surface { get; }

        /// <summary>One cell's side (the cube size), in pixels: the largest that fits the cells inside the wall on <see cref="Surface"/>.</summary>
        public float Cell { get; }

        /// <summary>The cells inside the wall, centred on <see cref="Surface"/>.</summary>
        public Box Grid { get; }

        /// <summary>The gates in the wall, in the level's <c>entries</c> order.</summary>
        public IReadOnlyList<RoundGate> Gates { get; }

        /// <summary>The Sprig bridge's sites, in the level's choice order (empty without a bridge).</summary>
        public IReadOnlyList<RoundBridgeSite> Bridges { get; }

        /// <summary>
        /// The strips carried on to the frame: a stream or wall cell of the walkway ring (a strip that closes the ring ends in the
        /// wall) continues in cell-sized pieces over the surface between the grid and the frame, so it never stops short of
        /// the frame on a picture narrower than the board. Drawn with the ring cell's own picture.
        /// </summary>
        public IReadOnlyList<RoundStripPiece> StripPieces { get; }

        /// <summary>
        /// Lays out <paramref name="level"/>'s board in the fixed board box <paramref name="box"/>: the stone frame on its edge,
        /// the surface inside it, and the cells inside the wall at the largest cube that fits, centred.
        /// </summary>
        public static RoundBoardLayout Fit(Box box, RoundLevel level) =>
            Fit(box, level.Width, level.Height, level.RowsBottomFirst, level.Entries, level.Bridge, level.OverlayCells());

        /// <summary>
        /// Lays out a round board of <paramref name="width"/> × <paramref name="height"/> cells (the wall included) from its
        /// <paramref name="rowsBottomFirst"/>, its gates (<paramref name="entries"/>) and its bridge sites, in the fixed board box
        /// <paramref name="box"/>. The walls among <paramref name="overlay"/>'s cells (the level's overlay,
        /// <see cref="RoundLevel.OverlayCells"/>) are a stone wall instead of water.
        /// </summary>
        public static RoundBoardLayout Fit(Box box, int width, int height, IReadOnlyList<string> rowsBottomFirst, IReadOnlyList<int> entries, IReadOnlyList<RoundBridgeChoice>? bridge, IReadOnlyList<int>? overlay = null)
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

            return new RoundBoardLayout(box, width, height, kinds, materials, entryCells, bridge ?? Array.Empty<RoundBridgeChoice>(), stone);
        }

        /// <summary>
        /// The board in the reference gameplay regions: in the fixed board box (<see cref="ReferenceGameplayRegions.RoundBoard"/>),
        /// the same on every level of the screen whatever its picture, badge or boosters.
        /// </summary>
        public static RoundBoardLayout In(ReferenceGameplayRegions regions, RoundLevel level) => Fit(regions.RoundBoard, level);

        /// <summary>The same board moved so its frame's bottom is on <paramref name="bottom"/> (the cube size kept).</summary>
        public RoundBoardLayout DockBottom(float bottom) =>
            new RoundBoardLayout(Outer.Offset(0f, bottom - Outer.Bottom), Width, Height, _kinds, _materials, _entries, _bridge, _stone);

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
        /// The box of cell <paramref name="cell"/> on screen: a cell inside the wall is its grid cell; a wall cell's box lies a
        /// cell beyond the grid, as the grid would continue (the wall itself is the fixed frame).
        /// </summary>
        public Box CellBox(int cell) => CellBox(cell % Width, cell / Width);

        /// <summary>The box of the cell at (<paramref name="x"/>, <paramref name="y"/>) of the rows (row 0 at the bottom).</summary>
        public Box CellBox(int x, int y)
        {
            float left = Grid.Left + ((x - 1) * Cell);
            float top = Grid.Top + ((Height - 2 - y) * Cell);
            return new Box(left, top, left + Cell, top + Cell);
        }

        /// <summary>A cube's box in its cell (<see cref="CubeInset"/>).</summary>
        public Box CubeBox(int cell) => CellBox(cell).Inset(Cell * CubeInset);

        /// <summary>
        /// Where a walker stands on cell <paramref name="cell"/> of its route: the middle of a cell inside the wall, or in the
        /// middle of the stone frame in line with the nearest inside cell for a wall cell or a gate (a gate's
        /// <see cref="RoundGate.Door"/>).
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

            return DoorAt(x, y);
        }

        /// <summary>The cell of the rows under the screen point (<paramref name="px"/>, <paramref name="py"/>) inside the wall, or -1.</summary>
        public int CellAt(float px, float py)
        {
            int x = 1 + (int)Math.Floor((px - Grid.Left) / Cell);
            int y = Height - 2 - (int)Math.Floor((py - Grid.Top) / Cell);
            return IsInner(x, y) ? (y * Width) + x : -1;
        }

        /// <summary>The wall a frame cell is in: the bottom or top row first, then the left or right column.</summary>
        private EntrySide SideOf(int x, int y) =>
            y == 0 ? EntrySide.Bottom : y == Height - 1 ? EntrySide.Top : x == 0 ? EntrySide.Left : EntrySide.Right;

        /// <summary>The box of the inside cell nearest to (<paramref name="x"/>, <paramref name="y"/>).</summary>
        private Box InnerBox(int x, int y) =>
            CellBox(Math.Max(1, Math.Min(Width - 2, x)), Math.Max(1, Math.Min(Height - 2, y)));

        /// <summary>The middle of the stone frame on a wall cell's side, in line with its nearest inside cell.</summary>
        private (float X, float Y) DoorAt(int x, int y)
        {
            Box inner = InnerBox(x, y);
            float mid = (BoardLayout.Gap + (BoardLayout.Stone / 2f)) * Unit;
            return SideOf(x, y) switch
            {
                EntrySide.Top => (inner.CenterX, Surface.Top - mid),
                EntrySide.Left => (Surface.Left - mid, inner.CenterY),
                EntrySide.Right => (Surface.Right + mid, inner.CenterY),
                _ => (inner.CenterX, Surface.Bottom + mid),
            };
        }

        /// <summary>The pieces carrying the ring's strip cells on to the frame (<see cref="StripPieces"/>).</summary>
        private List<RoundStripPiece> Pieces()
        {
            var pieces = new List<RoundStripPiece>();
            var across = new List<(int Step, float Start)>();
            var down = new List<(int Step, float Start)>();
            for (int y = 1; y < Height - 1; y++)
            {
                for (int x = 1; x < Width - 1; x++)
                {
                    bool ring = x == 1 || y == 1 || x == Width - 2 || y == Height - 2;
                    RoundCellKind kind = KindOf(x, y);
                    if (!ring || (kind != RoundCellKind.Stream && kind != RoundCellKind.Wall))
                    {
                        continue;
                    }

                    Box box = CellBox(x, y);
                    Steps(across, box.Left, x == 1 ? Surface.Left : x == Width - 2 ? Surface.Right : box.Left, x == 1 ? -1 : 1);
                    Steps(down, box.Top, y == 1 ? Surface.Bottom : y == Height - 2 ? Surface.Top : box.Top, y == 1 ? 1 : -1);
                    int cell = (y * Width) + x;
                    foreach ((int sx, float left) in across)
                    {
                        foreach ((int sy, float top) in down)
                        {
                            if (sx != 0 || sy != 0)
                            {
                                pieces.Add(new RoundStripPiece(cell, x + sx, y - sy, new Box(left, top, left + Cell, top + Cell)));
                            }
                        }
                    }
                }
            }

            return pieces;
        }

        /// <summary>
        /// The starts of a run of cell-sized pieces from a cell starting at <paramref name="start"/> toward the frame's
        /// <paramref name="edge"/> (<paramref name="direction"/> -1 toward smaller coordinates): the cell itself (step 0),
        /// then whole cells, the last laid against the edge.
        /// </summary>
        private void Steps(List<(int Step, float Start)> into, float start, float edge, int direction)
        {
            into.Clear();
            into.Add((0, start));
            float room = direction < 0 ? start - edge : edge - (start + Cell);
            if (room < 0.5f)
            {
                return;
            }

            int count = (int)Math.Ceiling((room / Cell) - 0.01f);
            for (int k = 1; k <= count; k++)
            {
                float at = start + (direction * k * Cell);
                at = direction < 0 ? Math.Max(at, edge) : Math.Min(at, edge - Cell);
                into.Add((direction * k, at));
            }
        }
    }
}
