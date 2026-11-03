using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--board-cubes &lt;published dir&gt; &lt;level&gt; &lt;out dir&gt;</c>: the owner's look-dev of 2026-11-03 ("in the middle of a
    /// big picture the cubes look flat, squares in a drawn grid"): the same round level drawn by the full playtest's painter
    /// on a 1080 × 2340 phone in each cube look of <see cref="BoardCubeLook.Variants"/> (A is the game's look), written as
    /// the whole screen, the board box at phone scale and a 2.5× close-up of the picture's densest middle, plus one contact
    /// sheet of all of them with their numbers and the cell size each look leaves on the biggest boards. Review material
    /// only: the game's look stays <see cref="BoardCubeLook.Default"/>.
    /// </summary>
    public static class BoardCubeSheet
    {
        private const float Zoom = 2.5f;
        private const int WindowColumns = 16;
        private const int WindowRows = 11;

        public static int Write(string root, string folder, int level, string outDir)
        {
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            Directory.CreateDirectory(outDir);
            var insets = new Insets(110, 63);
            var boards = new List<SKBitmap>();
            var crops = new List<SKBitmap>();
            var notes = new StringBuilder();
            RoundLevel round = rounds.GetLevel(level);
            (int wx, int wy) = DensestWindow(round);
            notes.AppendLine("Board cube look-dev, level " + level + " (" + round.Width + " x " + round.Height + "), phone 1080 x 2340; close-up: columns "
                + wx + "-" + (wx + WindowColumns - 1) + ", rows " + wy + "-" + (wy - WindowRows + 1) + " (row 0 at the bottom), x" + Zoom.ToString(CultureInfo.InvariantCulture));
            BoardCubeLook before = BoardCubeLook.Current;
            try
            {
                foreach ((string id, string label, BoardCubeLook look) in BoardCubeLook.Variants)
                {
                    BoardCubeLook.Current = look;
                    (byte[] png, RoundBoardLayout board) = Render(rounds, empty, level, insets);
                    File.WriteAllBytes(Path.Combine(outDir, id + "-screen.png"), png);
                    using SKBitmap shot = SKBitmap.Decode(png);
                    SKBitmap boardBitmap = Crop(shot, board.Outer, 1f);
                    Save(boardBitmap, Path.Combine(outDir, id + "-board.png"));
                    boards.Add(boardBitmap);
                    SKBitmap crop = Crop(shot, Window(board, wx, wy), Zoom);
                    Save(crop, Path.Combine(outDir, id + "-closeup.png"));
                    crops.Add(crop);

                    notes.AppendLine(id + " " + Numbers(look) + " | " + CellSizes(rounds, look, insets));
                }
            }
            finally
            {
                BoardCubeLook.Current = before;
            }

            Sheet(root, Path.Combine(outDir, "board-cubes-sheet.png"), level, round, boards, crops);
            File.WriteAllText(Path.Combine(outDir, "variants.txt"), notes.ToString());
            Console.Write(notes.ToString());
            foreach (SKBitmap bitmap in boards)
            {
                bitmap.Dispose();
            }

            foreach (SKBitmap bitmap in crops)
            {
                bitmap.Dispose();
            }

            Console.WriteLine("board cubes: sheet and frames in " + outDir);
            return 0;
        }

        /// <summary>
        /// <c>--board-after &lt;published dir&gt; &lt;out dir&gt; &lt;level&gt;...</c>: the given levels in the game's cube look
        /// (<see cref="BoardCubeLook.Default"/>, the owner's "E" of 2026-11-03) as <c>after-L&lt;n&gt;-screen.png</c>,
        /// <c>-board.png</c> and, on a board big enough, <c>-closeup.png</c> (×2.5 of the densest middle).
        /// </summary>
        public static int After(string folder, string outDir, IReadOnlyList<int> levels)
        {
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            Directory.CreateDirectory(outDir);
            foreach (int level in levels)
            {
                string name = "after-L" + level.ToString("00", CultureInfo.InvariantCulture);
                (byte[] png, RoundBoardLayout board) = Render(rounds, empty, level, new Insets(110, 63));
                File.WriteAllBytes(Path.Combine(outDir, name + "-screen.png"), png);
                using SKBitmap shot = SKBitmap.Decode(png);
                using (SKBitmap boardBitmap = Crop(shot, board.Outer, 1f))
                {
                    Save(boardBitmap, Path.Combine(outDir, name + "-board.png"));
                }

                RoundLevel round = rounds.GetLevel(level);
                if (round.Width - 2 >= WindowColumns && round.Height - 2 >= WindowRows)
                {
                    (int wx, int wy) = DensestWindow(round);
                    using SKBitmap crop = Crop(shot, Window(board, wx, wy), Zoom);
                    Save(crop, Path.Combine(outDir, name + "-closeup.png"));
                }

                Console.WriteLine(name + ": " + round.Width + " x " + round.Height + ", cell " + board.Cell.ToString("0.0", CultureInfo.InvariantCulture) + " px");
            }

            return 0;
        }

        /// <summary>The close-up window from cell (<paramref name="wx"/>, <paramref name="wy"/>), the last row's overhang included.</summary>
        private static Box Window(RoundBoardLayout board, int wx, int wy)
        {
            Box a = board.CellBox(wx, wy);
            Box b = board.CellBox(wx + WindowColumns - 1, wy - WindowRows + 1);
            return new Box(a.Left, a.Top, b.Right, b.Bottom + (board.Cell * board.Look.Overhang));
        }

        /// <summary>Level <paramref name="level"/> as the full playtest draws it on a 1080 × 2340 phone (its intro closed), in <see cref="BoardCubeLook.Current"/>.</summary>
        private static (byte[] Png, RoundBoardLayout Board) Render(RoundContentSet rounds, ContentSet empty, int level, Insets insets)
        {
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-board-cubes-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var painter = new SkiaPainter(1080, 2340, insets);
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                void Frame(float dt)
                {
                    painter.Now += dt;
                    painter.BeginFrame();
                    app.Draw(painter, dt);
                }

                app.LoadLevel(level);
                RoundLevelScreen screen = app.Round!;
                screen.SkipGuide();
                for (int i = 0; i < 20; i++)
                {
                    Frame(1f / 30f);
                }

                if (screen.Intro != null)
                {
                    screen.CloseIntro();
                }

                for (int i = 0; i < 40; i++)
                {
                    Frame(1f / 30f);
                }

                byte[] png = painter.Png();
                RoundBoardLayout board = RoundPainter.Layout(painter, screen, RoundPainter.HasBoosters(screen), RoundPainter.Badge(screen).HasValue).Board;
                app.Dispose();
                return (png, board);
            }
            finally
            {
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }

        /// <summary>A look's numbers, as the sheet and the notes print them.</summary>
        private static string Numbers(BoardCubeLook k) => string.Format(CultureInfo.InvariantCulture,
            "fill {0:0.00}, row {1:0.00}, top {2:0.00}, front {3:0.00} (peek {4:0.00}), bevel {5:0.00}, gloss {6:0.00}, corner {7:0.00}, rim {8:0.0}, top lift {9:0.00}, bevel dark {10:0.00}, front {11:0.00}-{12:0.00}, seam {13:0.00}, gloss size {14:0.000}, shadow {15:0.00}@{16:0.00}",
            k.Fill, k.RowShare, k.TopShare, k.FrontShare, k.FrontPeek, k.BevelShare, k.Gloss, k.CornerShare, k.Rim, k.TopLift, k.BevelDark, k.FrontLight, k.FrontDeep, k.Seam, k.GlossSize, k.ShadowShare, k.ShadowAlpha);

        /// <summary>The cell size (px on a 1080-wide phone) the look leaves on the biggest published boards.</summary>
        private static string CellSizes(RoundContentSet rounds, BoardCubeLook look, Insets insets)
        {
            var parts = new List<string>();
            foreach (int n in new[] { 61, 80, 57, 55 })
            {
                if (n > rounds.LevelCount)
                {
                    continue;
                }

                RoundLevel r = rounds.GetLevel(n);
                ReferenceGameplayRegions regions = ScreenLayout.RoundGameplay(1080, 2340, insets, Math.Max(1, r.Tray.Count), 5, true, false);
                RoundBoardLayout b = RoundBoardLayout.In(regions, r, look);
                RoundBoardLayout a = RoundBoardLayout.In(regions, r, BoardCubeLook.Default);
                parts.Add(string.Format(CultureInfo.InvariantCulture, "L{0} {1}x{2}: cell {3:0.0} px ({4:+0.0;-0.0;0} %)", n, r.Width, r.Height, b.Cell, ((b.Cell / a.Cell) - 1f) * 100f));
            }

            return string.Join("; ", parts);
        }

        /// <summary>The window of cells (its upper-left cell, row 0 at the bottom) holding the most cubes and colour edges, nearest the middle.</summary>
        private static (int X, int Y) DensestWindow(RoundLevel level)
        {
            int w = level.Width;
            int h = level.Height;
            char At(int x, int y) => level.RowsBottomFirst[y][x];
            bool Cube(int x, int y) => At(x, y) != '.' && At(x, y) != '#';
            (int X, int Y) best = (1, h - 2);
            double bestScore = double.MinValue;
            for (int top = h - 2; top - WindowRows + 1 >= 1; top--)
            {
                for (int left = 1; left + WindowColumns - 1 <= w - 2; left++)
                {
                    int cubes = 0;
                    int edges = 0;
                    for (int y = top; y > top - WindowRows; y--)
                    {
                        for (int x = left; x < left + WindowColumns; x++)
                        {
                            if (!Cube(x, y))
                            {
                                continue;
                            }

                            cubes++;
                            edges += Cube(x + 1, y) && At(x + 1, y) != At(x, y) ? 1 : 0;
                            edges += Cube(x, y - 1) && At(x, y - 1) != At(x, y) ? 1 : 0;
                        }
                    }

                    double cx = left + (WindowColumns / 2.0) - (w / 2.0);
                    double cy = top - (WindowRows / 2.0) - (h / 2.0);
                    double score = (cubes * 4) + Math.Min(edges, cubes / 2) - (0.5 * Math.Sqrt((cx * cx) + (cy * cy)));
                    if (cubes == WindowColumns * WindowRows)
                    {
                        score += 40;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = (left, top);
                    }
                }
            }

            return best;
        }

        private static SKBitmap Crop(SKBitmap source, Box box, float zoom)
        {
            var src = SKRectI.Round(new SKRect(box.Left, box.Top, box.Right, box.Bottom));
            src.Intersect(new SKRectI(0, 0, source.Width, source.Height));
            var target = new SKBitmap((int)Math.Round(src.Width * zoom), (int)Math.Round(src.Height * zoom));
            using var canvas = new SKCanvas(target);
            using SKImage image = SKImage.FromBitmap(source);
            canvas.DrawImage(image, src, new SKRect(0, 0, target.Width, target.Height), new SKSamplingOptions(SKCubicResampler.Mitchell), null);
            return target;
        }

        private static void Save(SKBitmap bitmap, string path)
        {
            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, data.ToArray());
        }

        private static SKTypeface Font(string root, bool bold)
        {
            string nunito = Path.Combine(root, "client/Assets/Bloomlings/UI/Fonts/Resources/" + (bold ? "Nunito-ExtraBold.ttf" : "Nunito-SemiBold.ttf"));
            SKTypeface? face = File.Exists(nunito) ? SKTypeface.FromFile(nunito) : null;
            if (face != null && face.ContainsGlyph('ж'))
            {
                return face;
            }

            return SKTypeface.FromFamilyName("DejaVu Sans", bold ? SKFontStyle.Bold : SKFontStyle.Normal) ?? SKTypeface.Default;
        }

        private static void Sheet(string root, string path, int level, RoundLevel round, List<SKBitmap> boards, List<SKBitmap> crops)
        {
            const int pad = 40;
            const int head = 150;
            const int labelHeight = 110;
            const int numbersHeight = 200;
            int column = 0;
            int boardHeight = 0;
            int cropHeight = 0;
            for (int i = 0; i < boards.Count; i++)
            {
                column = Math.Max(column, Math.Max(boards[i].Width, crops[i].Width));
                boardHeight = Math.Max(boardHeight, boards[i].Height);
                cropHeight = Math.Max(cropHeight, crops[i].Height);
            }

            int width = pad + (boards.Count * (column + pad));
            int height = head + labelHeight + boardHeight + pad + cropHeight + numbersHeight + pad;
            using var sheet = new SKBitmap(width, height);
            using var canvas = new SKCanvas(sheet);
            canvas.Clear(new SKColor(246, 240, 228));
            using SKTypeface bold = Font(root, true);
            using SKTypeface plain = Font(root, false);
            using var titleFont = new SKFont(bold, 64);
            using var labelFont = new SKFont(bold, 72);
            using var smallFont = new SKFont(plain, 30);
            using var ink = new SKPaint { Color = new SKColor(62, 44, 30), IsAntialias = true };
            using var soft = new SKPaint { Color = new SKColor(110, 90, 70), IsAntialias = true };
            canvas.DrawText("Кубики борда: варианты вида · уровень " + level + " (" + round.Width + "×" + round.Height + ") · телефон 1080×2340, борд 1:1, ниже крупно ×2.5", pad, 95, titleFont, ink);
            for (int i = 0; i < boards.Count; i++)
            {
                (string id, string label, BoardCubeLook look) = BoardCubeLook.Variants[i];
                float x = pad + (i * (column + pad));
                labelFont.Size = 72;
                while (labelFont.Size > 30 && labelFont.MeasureText(label) > column)
                {
                    labelFont.Size -= 2;
                }

                canvas.DrawText(label, x, head + 75, labelFont, ink);
                float y = head + labelHeight;
                canvas.DrawBitmap(boards[i], x + ((column - boards[i].Width) / 2f), y);
                y += boardHeight + pad;
                canvas.DrawBitmap(crops[i], x + ((column - crops[i].Width) / 2f), y);
                y += cropHeight + 44;
                string[] lines =
                {
                    string.Format(CultureInfo.InvariantCulture, "зазор {0:0.00} · шаг ряда {1:0.00} · верх {2:0.00} · перед {3:0.00}", 1f - look.Fill, look.RowShare, look.TopShare, look.FrontShare),
                    string.Format(CultureInfo.InvariantCulture, "видно переда под соседом {0:0.00} · скос {1:0.00} · блик {2:0.00} · углы {3:0.00}", look.FrontPeek, look.BevelShare, look.Gloss, look.CornerShare),
                    string.Format(CultureInfo.InvariantCulture, "кант {0:0.0} · перед {1:0.00}–{2:0.00} · шов {3:0.00} · тень {4:0.00}", look.Rim, look.FrontLight, look.FrontDeep, look.Seam, look.ShadowAlpha),
                };
                foreach (string line in lines)
                {
                    smallFont.Size = 30;
                    while (smallFont.Size > 16 && smallFont.MeasureText(line) > column)
                    {
                        smallFont.Size -= 1;
                    }

                    canvas.DrawText(line, x, y, smallFont, soft);
                    y += 42;
                }
            }

            canvas.Flush();
            Save(sheet, path);
        }
    }
}
