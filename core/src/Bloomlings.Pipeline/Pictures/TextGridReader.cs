using System;
using System.Collections.Generic;
using System.IO;

namespace Bloomlings.Pipeline.Pictures
{
    /// <summary>
    /// Reads <c>.grid.txt</c> pictures (T072): one character per cell, top row first. Characters map to role ids through
    /// the sidecar's <c>legend</c>; <c>.</c> is EMPTY and <c>#</c> is STONE. Blank lines and lines starting with
    /// <c>;</c> are ignored.
    /// </summary>
    public static class TextGridReader
    {
        /// <summary>Returns role ids per cell (null = EMPTY, "#" = STONE), rows top first.</summary>
        public static string?[][] Read(string text, IReadOnlyDictionary<char, string> legend, string name = "grid")
        {
            var rows = new List<string?[]>();
            int width = -1;
            foreach (string rawLine in text.Replace("\r", string.Empty).Split('\n'))
            {
                string line = rawLine.TrimEnd();
                if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal))
                {
                    continue;
                }

                if (width >= 0 && line.Length != width)
                {
                    throw new InvalidDataException($"{name}: row {rows.Count + 1} has {line.Length} cells, expected {width}.");
                }

                width = line.Length;
                var cells = new string?[width];
                for (int x = 0; x < width; x++)
                {
                    char c = line[x];
                    if (c == '.')
                    {
                        cells[x] = null;
                    }
                    else if (c == '#')
                    {
                        cells[x] = "#";
                    }
                    else if (legend.TryGetValue(c, out string? role))
                    {
                        cells[x] = role;
                    }
                    else
                    {
                        throw new InvalidDataException($"{name}: character '{c}' in row {rows.Count + 1} is not in the legend.");
                    }
                }

                rows.Add(cells);
            }

            if (rows.Count == 0)
            {
                throw new InvalidDataException($"{name}: the grid is empty.");
            }

            return rows.ToArray();
        }
    }
}
