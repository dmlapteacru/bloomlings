using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Bloomlings.Generator;

namespace Bloomlings.Pipeline.Review
{
    /// <summary>
    /// The review sheet of a batch (T082, <c>review</c>): per level a board render (variant colors with icon letters,
    /// stones and entries) and a finished-picture render, and an <c>index.html</c> with the metrics and the manual QA
    /// tier (FR-084): Levels 1–100 need a playtest, 101–500 a manual review, 501+ sampling; milestone, Hard and Super
    /// Hard levels are flagged for stronger review in every tier.
    /// </summary>
    public static class ReviewRenderer
    {
        private const int Cell = 24;

        public static void Render(IReadOnlyList<(LevelDefinition Level, BasePicture Picture, ValidationRecord? Record)> batch, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);
            var html = new StringBuilder();
            html.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
                .Append("<title>Bloomlings review</title><style>body{font-family:system-ui,sans-serif;margin:16px;background:#f7f5ee;color:#333}")
                .Append("table{border-collapse:collapse}td,th{border:1px solid #ccc;padding:6px;vertical-align:top;font-size:13px}")
                .Append("img{image-rendering:pixelated;max-width:240px}.flag{color:#b33;font-weight:bold}</style></head><body><h1>Level review</h1>")
                .Append("<table><tr><th>Level</th><th>Board</th><th>Finished picture</th><th>Class / score</th><th>Metrics</th><th>QA</th></tr>");
            foreach ((LevelDefinition level, BasePicture original, ValidationRecord? record) in batch.OrderBy(b => b.Level.LevelNumber))
            {
                // Drafts are reviewed here before approval, so they are rendered through an in-memory preview copy.
                bool draft = original.Review.Status != ReviewStatus.Approved;
                BasePicture picture = draft ? PicturePicker.AsPreview(original) : original;
                string name = "level-" + level.LevelNumber.ToString("00000", CultureInfo.InvariantCulture);
                File.WriteAllBytes(Path.Combine(outputFolder, name + "-board.png"), RenderBoard(level, picture));
                File.WriteAllBytes(Path.Combine(outputFolder, name + "-finished.png"), RenderFinished(level, picture));
                html.Append("<tr><td><b>L").Append(level.LevelNumber).Append("</b><br>").Append(WebUtility.HtmlEncode(picture.Subject))
                    .Append("<br><small>").Append(WebUtility.HtmlEncode(picture.Id)).Append(level.Picture.Mirror == Mirror.Horizontal ? " (mirrored)" : string.Empty).Append("</small></td>")
                    .Append("<td><img src=\"").Append(name).Append("-board.png\" alt=\"board\"></td>")
                    .Append("<td><img src=\"").Append(name).Append("-finished.png\" alt=\"finished picture\"></td>")
                    .Append("<td>").Append(level.Difficulty.Class).Append(level.Difficulty.Overridden ? " (override)" : string.Empty)
                    .Append("<br>score ").Append(level.Difficulty.Score).Append("</td><td><small>");
                if (record != null)
                {
                    html.Append(record.Result.ToString().ToLowerInvariant()).Append(", ").Append(record.SolutionTrace.Count).Append(" taps<br>");
                    foreach (KeyValuePair<string, int> metric in record.Metrics)
                    {
                        html.Append(WebUtility.HtmlEncode(metric.Key)).Append(": ").Append(metric.Value).Append("<br>");
                    }
                }

                html.Append("</small></td><td>").Append(Tier(level.LevelNumber));
                IEnumerable<string> flags = Flags(level);
                if (draft)
                {
                    flags = flags.Prepend($"picture {original.Review.Status.ToString().ToLowerInvariant()}: approve before release (FR-084)");
                }

                foreach (string flag in flags)
                {
                    html.Append("<br><span class=\"flag\">").Append(flag).Append("</span>");
                }

                html.Append("</td></tr>");
            }

            html.Append("</table></body></html>\n");
            File.WriteAllText(Path.Combine(outputFolder, "index.html"), html.ToString());
        }

        public static string Tier(int level) => level <= 100 ? "manual playtest" : level <= 500 ? "manual review" : "sampling";

        public static IEnumerable<string> Flags(LevelDefinition level)
        {
            if (level.LevelNumber % 25 == 0)
            {
                yield return "milestone: stronger review";
            }

            if (level.Difficulty.Class != DifficultyClass.Normal)
            {
                yield return level.Difficulty.Class + ": stronger review";
            }
        }

        public static byte[] RenderBoard(LevelDefinition level, BasePicture picture)
        {
            Board board = BoardBuilder.Build(level, picture, VariantCatalog.Default);
            int w = board.Width;
            int h = board.Height + 1;
            var canvas = new Canvas(w * Cell, h * Cell, (0xF7, 0xF5, 0xEE));
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    int index = board.IndexOf(new CellPos(x, y));
                    int px = x * Cell;
                    int py = (board.Height - 1 - y) * Cell;
                    switch (board.KindAt(index))
                    {
                        case CellKind.Target:
                            (byte, byte, byte) color = Hex(VariantCatalog.Default.Get(board.TopLayer(index)).ColorHex);
                            canvas.Fill(px + 1, py + 1, Cell - 2, Cell - 2, color);
                            canvas.Text(px + 7, py + 5, Letter(board.TopLayer(index)), (255, 255, 255));
                            if (board.RemainingLayers(index) > 1)
                            {
                                // The layer peek: the next layer's color in the top-right corner.
                                canvas.Fill(px + Cell - 7, py + 2, 5, 5, Hex(VariantCatalog.Default.Get(board.NextLayer(index)!.Value).ColorHex));
                            }

                            if (board.IsMysteryHidden(index))
                            {
                                // A mystery tile: the reviewer sees the hidden variant with a ? frame.
                                canvas.Fill(px + 1, py + 1, Cell - 2, 3, (0x30, 0x30, 0x30));
                                canvas.Text(px + Cell - 7, py + Cell - 9, "?", (0x30, 0x30, 0x30));
                            }

                            if (board.KeyAt(index) != null)
                            {
                                canvas.Fill(px + 2, py + Cell - 8, 6, 6, (0xFA, 0xCC, 0x40));
                            }

                            break;
                        case CellKind.Stone:
                            canvas.Fill(px + 1, py + 1, Cell - 2, Cell - 2, (0x8C, 0x8C, 0x94));
                            break;
                        case CellKind.Special:
                            bool fountain = level.Specials.Any(sp => sp.Type == SpecialType.Fountain && sp.Cells.Contains(new CellPos(x, y)));
                            canvas.Fill(px + 1, py + 1, Cell - 2, Cell - 2, fountain ? ((byte)0x93, (byte)0xA8, (byte)0xBC) : ((byte)0x5C, (byte)0x80, (byte)0x4C));
                            canvas.Text(px + 7, py + 5, fountain ? "F" : "G", (255, 255, 255));
                            break;
                        default:
                            canvas.Fill(px + 1, py + 1, Cell - 2, Cell - 2, (0xDC, 0xD4, 0xBD));
                            break;
                    }
                }
            }

            foreach (EntryDef entry in board.Entries)
            {
                int px = entry.Cell.X * Cell;
                int py = board.Height * Cell;
                canvas.Fill(px + 6, py + 6, Cell - 12, Cell - 12, (0xFA, 0xCC, 0x40));
            }

            return canvas.Encode();
        }

        public static byte[] RenderFinished(LevelDefinition level, BasePicture picture)
        {
            int w = picture.Width;
            int h = picture.Height;
            var canvas = new Canvas(w * Cell, h * Cell, (0xDC, 0xD4, 0xBD));
            bool mirror = level.Picture.Mirror == Mirror.Horizontal;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int value = picture.CellAt(mirror ? w - 1 - x : x, y);
                    (byte R, byte G, byte B) color = value >= 0 && level.Mapping.TryGetValue(picture.Roles[value].RoleId, out VariantId variant)
                        ? Light(Hex(VariantCatalog.Default.Get(variant).ColorHex))
                        : value == BasePicture.Stone ? ((byte)0x8C, (byte)0x8C, (byte)0x94) : ((byte)0xDC, (byte)0xD4, (byte)0xBD);
                    canvas.Fill(x * Cell, (h - 1 - y) * Cell, Cell, Cell, color);
                }
            }

            return canvas.Encode();
        }

        private static string Letter(VariantId variant) => variant.Key switch
        {
            "violet_bud" => "V",
            "wood" => "O",
            _ => variant.Key.Substring(0, 1).ToUpperInvariant(),
        };

        private static (byte, byte, byte) Hex(string hex) =>
            (Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));

        private static (byte, byte, byte) Light((byte R, byte G, byte B) c) =>
            ((byte)(c.R + ((255 - c.R) * 55 / 100)), (byte)(c.G + ((255 - c.G) * 55 / 100)), (byte)(c.B + ((255 - c.B) * 55 / 100)));

        /// <summary>A small RGBA canvas with rectangles and a 5×7 bitmap font.</summary>
        private sealed class Canvas
        {
            private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
            {
                ['A'] = new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" },
                ['G'] = new[] { " ### ", "#   #", "#    ", "# ###", "#   #", "#   #", " ### " },
                ['?'] = new[] { " ### ", "#   #", "    #", "   # ", "  #  ", "     ", "  #  " },
                ['B'] = new[] { "#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### " },
                ['D'] = new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#   #", "#### " },
                ['F'] = new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#    " },
                ['L'] = new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####" },
                ['M'] = new[] { "#   #", "## ##", "# # #", "# # #", "#   #", "#   #", "#   #" },
                ['O'] = new[] { " ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " },
                ['V'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", " # # ", "  #  " },
                ['W'] = new[] { "#   #", "#   #", "#   #", "# # #", "# # #", "## ##", "#   #" },
            };

            private readonly int _width;
            private readonly int _height;
            private readonly byte[] _rgba;

            public Canvas(int width, int height, (byte R, byte G, byte B) background)
            {
                _width = width;
                _height = height;
                _rgba = new byte[width * height * 4];
                Fill(0, 0, width, height, background);
            }

            public void Fill(int x, int y, int w, int h, (byte R, byte G, byte B) color)
            {
                for (int py = Math.Max(0, y); py < Math.Min(_height, y + h); py++)
                {
                    for (int px = Math.Max(0, x); px < Math.Min(_width, x + w); px++)
                    {
                        int i = ((py * _width) + px) * 4;
                        _rgba[i] = color.R;
                        _rgba[i + 1] = color.G;
                        _rgba[i + 2] = color.B;
                        _rgba[i + 3] = 255;
                    }
                }
            }

            public void Text(int x, int y, string text, (byte R, byte G, byte B) color)
            {
                foreach (char c in text)
                {
                    if (Font.TryGetValue(c, out string[]? glyph))
                    {
                        for (int gy = 0; gy < glyph.Length; gy++)
                        {
                            for (int gx = 0; gx < glyph[gy].Length; gx++)
                            {
                                if (glyph[gy][gx] == '#')
                                {
                                    Fill(x + (gx * 2), y + (gy * 2), 2, 2, color);
                                }
                            }
                        }
                    }

                    x += 12;
                }
            }

            public byte[] Encode() => PngWriter.WriteRgba(_width, _height, _rgba);
        }
    }
}
