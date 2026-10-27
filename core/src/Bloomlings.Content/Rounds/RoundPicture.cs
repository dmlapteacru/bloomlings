using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    /// The board around a picture, as the reference's <c>boardOf</c> (<c>tools/rounds/lib.mjs</c>): a wall frame and an
    /// open ring on each side, the Garden Entry in the middle of the bottom wall, a stream row that spans the whole picture
    /// closing the ring too, and a bridge site 'b' becoming a wall whose anchor is the cell one row below.
    /// </summary>
    public static class RoundPictureBoard
    {
        private static readonly Regex StreamRow = new Regex("^[#b]+$", RegexOptions.CultureInvariant);

        /// <summary>The board of a picture: size, rows bottom first, the entry and the bridge sites.</summary>
        public static RoundBoardShape Of(RoundPicture picture)
        {
            int w = picture.Width + 4;
            int h = picture.Height + 4;
            string wall = new string('#', w);
            string ring = "#" + new string('.', w - 2) + "#";
            var top = new List<string> { wall, ring };
            foreach (string row in picture.RowsTopFirst)
            {
                string edge = StreamRow.IsMatch(row) ? "#" : ".";
                top.Add("#" + edge + row.Replace('b', '#') + edge + "#");
            }

            int mid = w / 2;
            top.Add(ring);
            top.Add(wall.Substring(0, mid) + "." + wall.Substring(mid + 1));
            var rows = new List<string>(top);
            rows.Reverse();

            var choices = new List<RoundBridgeChoice>();
            for (int y = 0; y < picture.RowsTopFirst.Count; y++)
            {
                string row = picture.RowsTopFirst[y];
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] != 'b')
                    {
                        continue;
                    }

                    int by = h - 1 - (y + 2);
                    int bx = x + 2;
                    int cell = (by * w) + bx;
                    choices.Add(new RoundBridgeChoice(cell, cell - w, bx * 2 < w ? "Левый мостик" : "Правый мостик"));
                }
            }

            return new RoundBoardShape(w, h, rows, new[] { mid }, choices.Count > 0 ? choices : null);
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
