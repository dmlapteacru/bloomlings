using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The difficulty class of a round level (rules 17 §5).</summary>
    public enum RoundClass
    {
        Normal,
        Hard,
        SuperHard,
    }

    /// <summary>How a hidden pod reveals (reference <c>rules.cjs</c>): round levels use <see cref="Depth"/>.</summary>
    public enum RoundMystery
    {
        None,

        /// <summary>Reveals as soon as it is the top pod of its column, before any choice.</summary>
        Depth,

        /// <summary>Reveals only when committed (kept for the reference's older mode; not used by round levels).</summary>
        Sealed,
    }

    /// <summary>A pod of a round level: its material, its count, whether it is hidden, its link group.</summary>
    public sealed class RoundPod
    {
        public RoundPod(char code, int count, RoundMystery mystery = RoundMystery.None, string? group = null)
        {
            Code = code;
            Count = count;
            Mystery = mystery;
            Group = group;
        }

        public char Code { get; }

        public int Count { get; }

        public RoundMystery Mystery { get; }

        /// <summary>The link group name ("A", "B" …), or null for a single pod.</summary>
        public string? Group { get; }
    }

    /// <summary>
    /// One site of the Sprig bridge: the wall cell it opens and its anchor, the orthogonal neighbour on the near bank (below,
    /// above or beside it; 17 §8 as amended on 2026-10-28). The crossing goes from the anchor through the cell.
    /// </summary>
    public sealed class RoundBridgeChoice
    {
        public RoundBridgeChoice(int cell, int anchor, string label)
        {
            Cell = cell;
            Anchor = anchor;
            Label = label;
        }

        public int Cell { get; }

        public int Anchor { get; }

        public string Label { get; }
    }

    /// <summary>
    /// A strip laid over an approved picture (17 §8 "Где стоит мост", 2026-10-28; <c>board.overlay</c> of
    /// <c>round-level.v1</c>): cells in picture coordinates (x from the left, y from the top) that become walls, and the
    /// bridge sites among or beside them. The cells under it are no work: the board's rows already hold the wall.
    /// </summary>
    public sealed class RoundOverlay
    {
        public RoundOverlay(IReadOnlyList<(int X, int Y)> walls, IReadOnlyList<(int X, int Y)> sites)
        {
            Walls = walls ?? throw new ArgumentNullException(nameof(walls));
            Sites = sites ?? throw new ArgumentNullException(nameof(sites));
        }

        /// <summary>Picture cells that become a wall ('#').</summary>
        public IReadOnlyList<(int X, int Y)> Walls { get; }

        /// <summary>Picture cells that become a bridge site ('b': a wall the bridge may open).</summary>
        public IReadOnlyList<(int X, int Y)> Sites { get; }
    }

    /// <summary>The mechanic a level introduces, and whether it is its second meeting (spec 006 FR-045).</summary>
    public sealed class RoundIntroduction
    {
        public RoundIntroduction(string mechanic, bool second)
        {
            Mechanic = mechanic;
            Second = second;
        }

        public string Mechanic { get; }

        public bool Second { get; }
    }

    /// <summary>A documented exception to one named rule of the level generation rules (e.g. L18's bridge).</summary>
    public sealed class RoundException
    {
        public RoundException(string rule, string reason, string decidedBy, string decidedAt)
        {
            Rule = rule;
            Reason = reason;
            DecidedBy = decidedBy;
            DecidedAt = decidedAt;
        }

        public string Rule { get; }

        public string Reason { get; }

        public string DecidedBy { get; }

        public string DecidedAt { get; }
    }

    /// <summary>The fingerprints (SHA-256, hex) of the reference round engine a level was measured on (research R12).</summary>
    public sealed class RoundEngineVersion
    {
        public RoundEngineVersion(string rules, string timeline, string palette, string clock)
        {
            Rules = rules;
            Timeline = timeline;
            Palette = palette;
            Clock = clock;
        }

        public string Rules { get; }

        public string Timeline { get; }

        public string Palette { get; }

        public string Clock { get; }
    }

    /// <summary>
    /// A round level definition (spec 006 FR-020, data model RoundLevel; wire format <c>round-level.v1</c>). The rows are
    /// the reference engine's input: bottom row first, '.' open, '#' wall, a material code otherwise; the frame ring and
    /// wall are part of the rows. <see cref="Prepare"/> gives the indexed form the rules work on, exactly as the
    /// reference's <c>prepare</c>.
    /// </summary>
    public sealed class RoundLevel
    {
        /// <summary>The largest board side with the frame (48 × 48).</summary>
        public const int MaxSide = 48;

        public const sbyte Open = -1;

        public const sbyte Wall = -2;

        private RoundPrepared? _prepared;

        public RoundLevel(
            int number,
            int width,
            int height,
            IReadOnlyList<string> rowsBottomFirst,
            IReadOnlyList<RoundPod> pods,
            IReadOnlyList<IReadOnlyList<int>> tray,
            IReadOnlyList<int>? entries = null,
            IReadOnlyList<RoundBridgeChoice>? bridge = null,
            int capacity = 5,
            RoundClass roundClass = RoundClass.Normal,
            int definitionVersion = 1,
            RoundIntroduction? introduction = null,
            string? pictureId = null,
            int pictureVersion = 1,
            IReadOnlyList<string>? mechanics = null,
            RoundException? exception = null,
            RoundEngineVersion? engine = null,
            RoundOverlay? overlay = null)
        {
            Number = number;
            Width = width;
            Height = height;
            RowsBottomFirst = rowsBottomFirst ?? throw new ArgumentNullException(nameof(rowsBottomFirst));
            Pods = pods ?? throw new ArgumentNullException(nameof(pods));
            Tray = tray ?? throw new ArgumentNullException(nameof(tray));
            Entries = entries ?? new[] { width / 2 };
            Bridge = bridge;
            Capacity = capacity;
            Class = roundClass;
            DefinitionVersion = definitionVersion;
            Introduction = introduction;
            PictureId = pictureId;
            PictureVersion = pictureVersion;
            Mechanics = mechanics ?? Array.Empty<string>();
            Exception = exception;
            Engine = engine;
            Overlay = overlay;
        }

        public int Number { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<string> RowsBottomFirst { get; }

        public IReadOnlyList<RoundPod> Pods { get; }

        /// <summary>Columns of pod indices, top (exposed) pod first.</summary>
        public IReadOnlyList<IReadOnlyList<int>> Tray { get; }

        /// <summary>Entry (gate) cell indices; one gate in the middle of the bottom row by default.</summary>
        public IReadOnlyList<int> Entries { get; }

        /// <summary>The Sprig bridge's sites, or null when the level has no bridge.</summary>
        public IReadOnlyList<RoundBridgeChoice>? Bridge { get; }

        public int Capacity { get; }

        public RoundClass Class { get; }

        public int DefinitionVersion { get; }

        public RoundIntroduction? Introduction { get; }

        public string? PictureId { get; }

        public int PictureVersion { get; }

        public IReadOnlyList<string> Mechanics { get; }

        public RoundException? Exception { get; }

        public RoundEngineVersion? Engine { get; }

        /// <summary>The strip laid over the picture (walls and sites in picture coordinates), or null.</summary>
        public RoundOverlay? Overlay { get; }

        /// <summary>
        /// The board cell of a picture cell (x from the left, y from the top): the picture sits inside a wall frame and an
        /// open ring, so (x, y) is column x + 2 of row y + 2 from the top.
        /// </summary>
        public int PictureCell(int x, int y) => ((Height - 1 - (y + 2)) * Width) + x + 2;

        /// <summary>
        /// The board cells the overlay lays (presentation: they are drawn as a stone wall): each wall and site, and where it
        /// stands on the picture's edge the ring cell it closes outward (a corner both), as the reference's <c>boardOf</c>
        /// closes them. Empty without an overlay.
        /// </summary>
        public IReadOnlyList<int> OverlayCells()
        {
            if (Overlay == null)
            {
                return Array.Empty<int>();
            }

            int pw = Width - 4;
            int ph = Height - 4;
            var cells = new List<int>(Overlay.Walls.Count + Overlay.Sites.Count);
            void Add(int x, int y)
            {
                cells.Add(PictureCell(x, y));
                if (x == 0)
                {
                    cells.Add(PictureCell(-1, y));
                }

                if (x == pw - 1)
                {
                    cells.Add(PictureCell(pw, y));
                }

                if (y == 0)
                {
                    cells.Add(PictureCell(x, -1));
                }

                if (y == ph - 1)
                {
                    cells.Add(PictureCell(x, ph));
                }
            }

            foreach ((int x, int y) in Overlay.Walls)
            {
                Add(x, y);
            }

            foreach ((int x, int y) in Overlay.Sites)
            {
                Add(x, y);
            }

            return cells;
        }

        public int CellCount => Width * Height;

        /// <summary>The indexed form (neighbours, board, pod colours, entries, groups), built once.</summary>
        public RoundPrepared Prepare() => _prepared ??= new RoundPrepared(this);
    }

    /// <summary>
    /// The indexed form of a round level, as the reference's <c>prepare</c>: neighbours in the fixed order
    /// <c>i - w</c> (the row below; rows are bottom first), left, right, <c>i + w</c> (the row above), -1 off the board;
    /// the board as material index, <see cref="RoundLevel.Open"/> or <see cref="RoundLevel.Wall"/>; each pod's material
    /// index; the entry set; and each pod's group (the indices of the pods sharing its group name, in pod order, or the
    /// pod alone).
    /// </summary>
    public sealed class RoundPrepared
    {
        internal RoundPrepared(RoundLevel level)
        {
            int w = level.Width;
            int n = level.CellCount;
            Neighbours = new int[n * 4];
            for (int i = 0; i < n; i++)
            {
                int x = i % w;
                int y = i / w;
                int at = i * 4;
                Neighbours[at] = y > 0 ? i - w : -1;
                Neighbours[at + 1] = x > 0 ? i - 1 : -1;
                Neighbours[at + 2] = x + 1 < w ? i + 1 : -1;
                Neighbours[at + 3] = y + 1 < level.Height ? i + w : -1;
            }

            Board = new sbyte[n];
            int k = 0;
            foreach (string row in level.RowsBottomFirst)
            {
                foreach (char c in row)
                {
                    if (k >= n)
                    {
                        throw new ArgumentException("The rows hold more cells than width × height.");
                    }

                    Board[k++] = c == '.' ? RoundLevel.Open : c == '#' ? RoundLevel.Wall : (sbyte)RoundMaterial.IndexOf(c);
                }
            }

            if (k != n)
            {
                throw new ArgumentException("The rows hold fewer cells than width × height.");
            }

            Colours = new sbyte[level.Pods.Count];
            for (int p = 0; p < level.Pods.Count; p++)
            {
                Colours[p] = (sbyte)RoundMaterial.IndexOf(level.Pods[p].Code);
            }

            Entries = new int[level.Entries.Count];
            IsEntry = new bool[n];
            for (int e = 0; e < level.Entries.Count; e++)
            {
                Entries[e] = level.Entries[e];
                if (Entries[e] >= 0 && Entries[e] < n)
                {
                    IsEntry[Entries[e]] = true;
                }
            }

            Groups = new int[level.Pods.Count][];
            for (int p = 0; p < level.Pods.Count; p++)
            {
                string? group = level.Pods[p].Group;
                if (group == null)
                {
                    Groups[p] = new[] { p };
                    continue;
                }

                var members = new List<int>();
                for (int q = 0; q < level.Pods.Count; q++)
                {
                    if (string.Equals(level.Pods[q].Group, group, StringComparison.Ordinal))
                    {
                        members.Add(q);
                    }
                }

                Groups[p] = members.ToArray();
            }
        }

        public int[] Neighbours { get; }

        public sbyte[] Board { get; }

        public sbyte[] Colours { get; }

        public int[] Entries { get; }

        public bool[] IsEntry { get; }

        public int[][] Groups { get; }
    }
}
