using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Content.Rounds
{
    /// <summary>
    /// An approved round picture (spec 006 FR-021, data model RoundPicture; wire format <c>round-picture.v1</c>). Rows are
    /// top first: material codes, '.' empty, '#' stream, 'b' bridge site. Only owner-approved pictures of the project's
    /// own (<c>rights: own</c>) exist in this format: the schema has no other status.
    /// </summary>
    public sealed class RoundPicture
    {
        /// <summary>The largest picture side (44); the board adds a ring and a wall on each side (48).</summary>
        public const int MaxSide = 44;

        public RoundPicture(
            string id,
            int version,
            string title,
            int width,
            int height,
            IReadOnlyList<string> rowsTopFirst,
            string decidedAt,
            string? family = null,
            string? direction = null,
            string? subject = null,
            string? source = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Version = version;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Width = width;
            Height = height;
            RowsTopFirst = rowsTopFirst ?? throw new ArgumentNullException(nameof(rowsTopFirst));
            DecidedAt = decidedAt ?? throw new ArgumentNullException(nameof(decidedAt));
            Family = family;
            Direction = direction;
            Subject = subject;
            Source = source;
        }

        /// <summary><c>P####</c>, never reused.</summary>
        public string Id { get; }

        /// <summary>A changed matrix is a new version.</summary>
        public int Version { get; }

        public string Title { get; }

        public string? Family { get; }

        public string? Direction { get; }

        public string? Subject { get; }

        public string? Source { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<string> RowsTopFirst { get; }

        /// <summary>When the owner approved it (the approval record's <c>decidedAt</c>).</summary>
        public string DecidedAt { get; }

        /// <summary>The key of a picture reference, <c>P0001@1</c>.</summary>
        public string Key => KeyOf(Id, Version);

        public static string KeyOf(string id, int version) => id + "@" + version;
    }

    /// <summary>
    /// The board around a picture, as the reference's <c>boardOf</c> (<c>tools/rounds/lib.mjs</c>; 17 §8 as amended on
    /// 2026-10-28): a wall frame and an open ring on each side, the Garden Entry in the middle of the bottom wall. An
    /// optional overlay lays a strip over the picture: each of its walls becomes '#', each of its sites 'b'. Every strip
    /// cell ('#' or 'b') on the picture's outer edge closes the ring cell directly outward (a corner cell both), so a
    /// strip that reaches the edge cuts the far bank off. An overlay's site must stand on a wall (listed among its walls,
    /// or a strip of the picture). Each bridge site 'b' becomes a wall whose anchor is its one orthogonal neighbour
    /// reachable from the entry while every site is still a wall; the sites come in reading order (top row first, left to
    /// right), each labelled with its crossing and where it stands ("Мостик вверх слева").
    /// </summary>
    public static class RoundPictureBoard
    {
        /// <summary>The board of a picture with an optional overlay: size, rows bottom first, the entry and the bridge sites.</summary>
        /// <exception cref="ArgumentException">An overlay cell lies outside the picture, or a site has no reachable
        /// neighbour or more than one.</exception>
        public static RoundBoardShape Of(RoundPicture picture, RoundOverlay? overlay = null)
        {
            int pw = picture.Width;
            int ph = picture.Height;
            char[][] cells = new char[ph][];
            for (int y = 0; y < ph; y++)
            {
                cells[y] = picture.RowsTopFirst[y].ToCharArray();
            }

            if (overlay != null)
            {
                Lay(overlay.Walls, '#', "wall");
                Lay(overlay.Sites, 'b', "site");
            }

            void Lay(IReadOnlyList<(int X, int Y)> at, char c, string what)
            {
                foreach ((int x, int y) in at)
                {
                    if (x < 0 || x >= pw || y < 0 || y >= ph)
                    {
                        throw new ArgumentException($"picture {picture.Key}: overlay {what} ({x},{y}) lies outside the {pw}×{ph} picture.");
                    }

                    if (c == 'b' && cells[y][x] != '#' && cells[y][x] != 'b')
                    {
                        throw new ArgumentException($"picture {picture.Key}: overlay site ({x},{y}) is not on a wall (list it among the walls too).");
                    }

                    cells[y][x] = c;
                }
            }

            // The board top first: the frame wall at row 0 and column 0, the ring at row 1 and column 1.
            int w = pw + 4;
            int h = ph + 4;
            char[][] top = new char[h][];
            for (int r = 0; r < h; r++)
            {
                top[r] = new char[w];
                for (int c = 0; c < w; c++)
                {
                    bool frame = r == 0 || r == h - 1 || c == 0 || c == w - 1;
                    top[r][c] = frame ? '#' : r >= 2 && r < h - 2 && c >= 2 && c < w - 2 ? cells[r - 2][c - 2] : '.';
                }
            }

            static bool Strip(char c) => c == '#' || c == 'b';
            for (int y = 0; y < ph; y++)
            {
                for (int x = 0; x < pw; x++)
                {
                    if (!Strip(cells[y][x]))
                    {
                        continue;
                    }

                    if (x == 0)
                    {
                        top[y + 2][1] = '#';
                    }

                    if (x == pw - 1)
                    {
                        top[y + 2][w - 2] = '#';
                    }

                    if (y == 0)
                    {
                        top[1][x + 2] = '#';
                    }

                    if (y == ph - 1)
                    {
                        top[h - 2][x + 2] = '#';
                    }
                }
            }

            int mid = w / 2;
            top[h - 1][mid] = '.';

            // Reading order of the sites, then every 'b' a wall.
            var sites = new List<(int X, int Y)>();
            for (int y = 0; y < ph; y++)
            {
                for (int x = 0; x < pw; x++)
                {
                    if (cells[y][x] == 'b')
                    {
                        sites.Add((x, y));
                        top[y + 2][x + 2] = '#';
                    }
                }
            }

            var rows = new List<string>(h);
            for (int r = h - 1; r >= 0; r--)
            {
                rows.Add(new string(top[r]));
            }

            List<RoundBridgeChoice>? choices = null;
            if (sites.Count > 0)
            {
                // What the entry reaches through every cell that is not a wall (materials are cleared on the way).
                bool[] reached = new bool[w * h];
                var queue = new Queue<int>();
                int entry = mid;
                reached[entry] = true;
                queue.Enqueue(entry);
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    int x = i % w;
                    int y = i / w;
                    foreach (int j in new[] { y > 0 ? i - w : -1, x > 0 ? i - 1 : -1, x + 1 < w ? i + 1 : -1, y + 1 < h ? i + w : -1 })
                    {
                        if (j >= 0 && !reached[j] && rows[j / w][j % w] != '#')
                        {
                            reached[j] = true;
                            queue.Enqueue(j);
                        }
                    }
                }

                choices = new List<RoundBridgeChoice>(sites.Count);
                var crossings = new List<(int X, int Y, string Dir)>(sites.Count);
                foreach ((int x, int y) in sites)
                {
                    int bx = x + 2;
                    int by = h - 1 - (y + 2);
                    int cell = (by * w) + bx;
                    var anchors = new List<int>(1);
                    foreach (int j in new[] { cell - w, cell - 1, cell + 1, cell + w })
                    {
                        if (reached[j])
                        {
                            anchors.Add(j);
                        }
                    }

                    if (anchors.Count != 1)
                    {
                        string why = anchors.Count == 0 ? "no neighbour the entry reaches" : $"{anchors.Count} neighbours the entry reaches";
                        throw new ArgumentException($"picture {picture.Key}: bridge site ({x},{y}) has {why}; exactly one is its anchor.");
                    }

                    int d = cell - anchors[0];
                    crossings.Add((x, y, d == w ? Up : d == -w ? Down : d == -1 ? Left : Right));
                    choices.Add(new RoundBridgeChoice(cell, anchors[0], string.Empty));
                }

                string[] labels = Labels(crossings, pw, ph);
                for (int i = 0; i < choices.Count; i++)
                {
                    choices[i] = new RoundBridgeChoice(choices[i].Cell, choices[i].Anchor, labels[i]);
                }
            }

            return new RoundBoardShape(w, h, rows, new[] { mid }, choices);
        }

        private const string Up = "вверх";

        private const string Down = "вниз";

        private const string Left = "влево";

        private const string Right = "вправо";

        /// <summary>
        /// The sites' labels, as the reference's: the crossing and where it stands, up or down by its column (left, centre,
        /// right third), left or right by its row (top, centre, bottom third); two equal labels told apart by their order.
        /// </summary>
        private static string[] Labels(List<(int X, int Y, string Dir)> sites, int pw, int ph)
        {
            static bool Vertical(string dir) => dir == Up || dir == Down;
            string Where((int X, int Y, string Dir) c) => Vertical(c.Dir)
                ? (c.X + 0.5 < pw / 3.0 ? "слева" : c.X + 0.5 > 2.0 * pw / 3.0 ? "справа" : "в центре")
                : (c.Y + 0.5 < ph / 3.0 ? "сверху" : c.Y + 0.5 > 2.0 * ph / 3.0 ? "снизу" : "в центре");
            string[] labels = sites.Select(c => "Мостик " + c.Dir + " " + Where(c)).ToArray();
            if (labels.Length == 2 && labels[0] == labels[1])
            {
                bool vertical = Vertical(sites[0].Dir);
                int a = vertical ? sites[0].X : sites[0].Y;
                int b = vertical ? sites[1].X : sites[1].Y;
                if (a != b)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        bool first = (i == 0) == (a < b);
                        labels[i] = "Мостик " + sites[i].Dir + " " + (vertical ? (first ? "слева" : "справа") : (first ? "сверху" : "снизу"));
                    }
                }
            }

            return labels;
        }
    }

    /// <summary>A board's geometry: size, rows bottom first, entry cells and the bridge sites (null without a bridge).</summary>
    public sealed class RoundBoardShape
    {
        public RoundBoardShape(int width, int height, IReadOnlyList<string> rowsBottomFirst, IReadOnlyList<int> entries, IReadOnlyList<RoundBridgeChoice>? bridge)
        {
            Width = width;
            Height = height;
            RowsBottomFirst = rowsBottomFirst;
            Entries = entries;
            Bridge = bridge;
        }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<string> RowsBottomFirst { get; }

        public IReadOnlyList<int> Entries { get; }

        public IReadOnlyList<RoundBridgeChoice>? Bridge { get; }
    }
}
