using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;
using SkiaSharp;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--channel-shutters &lt;out dir&gt;</c>: the look mock of 2026-11-05 for the owner (wave L101–L150, nothing in the rules yet):
    /// the Drop water channel (17 §8 «Водный канал») and the Twig wooden shutters (17 §8 «Деревянные створки»), each in two
    /// variants, drawn over real published levels' boards in the game's own drawing (the cubes, the entry arch, the
    /// stream's water and cream stone banks, the planks and the pods' cubes with their numbers) and played by the game's
    /// screen with the Classic walkers. The levels are preview copies (a carved pocket, an extra entry for the chosen exit,
    /// a plain tray), never the game's content. Writes phone frames (1080 wide), ×2 crops (<c>*-x2.png</c>, rendered at
    /// 2160 wide) and MP4 clips. Review material only: the hooks it sets (<see cref="RoundPainter.MockUnderCubes"/>,
    /// <see cref="RoundPainter.MockOverCubes"/>) stay null in the game.
    /// </summary>
    public static class ChannelShutterMock
    {
        private const float Fps = 25f;

        private static string _out = string.Empty;

        public static int Write(string root, string outDir)
        {
            _out = outDir;
            Directory.CreateDirectory(outDir);
            string folder = Path.Combine(root, "content/game/rounds/published");
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-channel-mock-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                using var big = new SkiaPainter(2160, 4680, new Insets(220, 126));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                app.Meta.Save.Progression.HighestCompletedLevel = 100;
                var rig = new Rig(app, painter, big);
                string only = Environment.GetEnvironmentVariable("BLOOMLINGS_MOCK_ONLY") ?? string.Empty;
                if (only.Length == 0 || only.Contains("channel", StringComparison.Ordinal))
                {
                    foreach (string variant in new[] { "A", "B" })
                    {
                        Channel(rig, rounds.GetLevel(76), variant);
                    }

                    ChannelExtras(rig, rounds.GetLevel(76));
                }

                if (only.Length == 0 || only.Contains("shutters", StringComparison.Ordinal))
                {
                    foreach (string variant in new[] { "A", "B" })
                    {
                        Shutters(rig, rounds.GetLevel(91), variant);
                        ShutterSizes(rig, rounds, variant);
                    }
                }

                app.Dispose();
                return 0;
            }
            finally
            {
                RoundPainter.MockUnderCubes = null;
                RoundPainter.MockOverCubes = null;
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }

        // ---- The rig: the game's screen on a preview copy of a level, frames, stills and clips ----

        private sealed class Rig
        {
            public Rig(DesignApp app, SkiaPainter painter, SkiaPainter big)
            {
                App = app;
                Painter = painter;
                Big = big;
            }

            public DesignApp App { get; }

            public SkiaPainter Painter { get; }

            public SkiaPainter Big { get; }

            public RoundLevelScreen S { get; private set; } = null!;

            public Process? Clip { get; private set; }

            public void Open(RoundLevel level)
            {
                var s = new RoundLevelScreen(App, level.Number, level, null);
                App.ShowRound(s);
                s.SkipGuide();
                if (s.Intro != null)
                {
                    s.CloseIntro();
                }

                S = s;
                Frame(1f / Fps);
            }

            public void Frame(float dt)
            {
                Painter.Now += dt;
                Painter.BeginFrame();
                App.Draw(Painter, dt);
                if (Clip != null)
                {
                    byte[] png = Painter.Png();
                    Clip.StandardInput.BaseStream.Write(png, 0, png.Length);
                }
            }

            public void Run(float seconds, Action<float>? each = null)
            {
                int n = (int)Math.Round(seconds * Fps);
                for (int i = 0; i < n; i++)
                {
                    each?.Invoke(i / Fps);
                    Frame(1f / Fps);
                }
            }

            /// <summary>The current frame (1080 wide) and, drawn again at 2160 wide, its crop round <paramref name="crop"/> (1080 px units).</summary>
            public void Still(string name, Box crop)
            {
                File.WriteAllBytes(Path.Combine(_out, name + ".png"), Painter.Png());
                Big.Now = Painter.Now;
                Big.BeginFrame();
                App.Draw(Big, 0f);
                using SKImage image = Big.Snapshot();
                var r = SKRectI.Create((int)(crop.Left * 2f), (int)(crop.Top * 2f), (int)(crop.Width * 2f), (int)(crop.Height * 2f));
                r.Intersect(SKRectI.Create(0, 0, image.Width, image.Height));
                using SKImage part = image.Subset(r);
                using SKData png = part.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(Path.Combine(_out, name + "-x2.png"), png.ToArray());

                // Draw the 1080 frame again so its layout cache is the one in use.
                Painter.BeginFrame();
                App.Draw(Painter, 0f);
            }

            public void StartClip(string name)
            {
                var info = new ProcessStartInfo("ffmpeg", "-y -loglevel error -f image2pipe -framerate " + Fps.ToString(CultureInfo.InvariantCulture)
                    + " -i - -c:v libx264 -pix_fmt yuv420p -crf 26 -preset medium -movflags +faststart \"" + Path.Combine(_out, name + ".mp4") + "\"")
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                };
                Clip = Process.Start(info);
            }

            public void EndClip()
            {
                if (Clip != null)
                {
                    Clip.StandardInput.Close();
                    Clip.WaitForExit();
                    Clip = null;
                }
            }

            public RoundBoardLayout Board => RoundPainter.Layout(Painter, S, RoundPainter.HasBoosters(S), RoundPainter.Badge(S).HasValue).Board;

            public void TapTop(int column)
            {
                IReadOnlyList<int> col = S.Round.Tray[column];
                foreach (int pod in col)
                {
                    if (S.Play.Check(RoundAction.Tap(pod)) == RoundRefusal.None)
                    {
                        S.Tap(pod);
                        return;
                    }
                }
            }
        }

        /// <summary>A preview copy of <paramref name="src"/>: its rows (top first), entries, a plain tray and the shown number.</summary>
        private static RoundLevel Copy(RoundLevel src, int number, char[][] rowsTop, IReadOnlyList<int> entries, (char Code, int Count)[][] columns)
        {
            var bottom = new List<string>();
            for (int y = rowsTop.Length - 1; y >= 0; y--)
            {
                bottom.Add(new string(rowsTop[y]));
            }

            // The rules keep the exact balance (each material's pods = its cubes): the given pods lead their columns (cut to what
            // the board holds), then the rest of every material follows in pods of at most 50, dealt round the columns.
            var left = new Dictionary<char, int>();
            foreach (char[] row in rowsTop)
            {
                foreach (char ch in row)
                {
                    if (ch != '#' && ch != '.')
                    {
                        left[ch] = left.TryGetValue(ch, out int n) ? n + 1 : 1;
                    }
                }
            }

            var pods = new List<RoundPod>();
            var lists = new List<List<int>>();
            foreach ((char Code, int Count)[] column in columns)
            {
                var ids = new List<int>();
                foreach ((char code, int count) in column)
                {
                    int have = left.TryGetValue(code, out int n) ? n : 0;
                    int take = Math.Min(have, count);
                    if (take > 0)
                    {
                        ids.Add(pods.Count);
                        pods.Add(new RoundPod(code, take));
                        left[code] = have - take;
                    }
                }

                lists.Add(ids);
            }

            int next = 0;
            var codes = new List<char>(left.Keys);
            codes.Sort();
            foreach (char code in codes)
            {
                for (int n = left[code]; n > 0; n -= 50)
                {
                    lists[next % lists.Count].Add(pods.Count);
                    pods.Add(new RoundPod(code, Math.Min(50, n)));
                    next++;
                }
            }

            var tray = new List<IReadOnlyList<int>>(lists);

            return new RoundLevel(number, src.Width, src.Height, bottom, pods, tray, entries, pictureId: src.PictureId, pictureVersion: src.PictureVersion);
        }

        private static char[][] RowsTop(RoundLevel src)
        {
            var rows = new char[src.Height][];
            for (int y = 0; y < src.Height; y++)
            {
                rows[y] = src.RowsBottomFirst[src.Height - 1 - y].ToCharArray();
            }

            return rows;
        }

        private static int CellTop(RoundLevel level, int x, int yTop) => ((level.Height - 1 - yTop) * level.Width) + x;

        // Screen point of a board point in cell units (x from the frame's left column, y from the top row; edges at integers).
        private static (float X, float Y) At(RoundBoardLayout b, float fx, float fy) =>
            (b.Grid.Left + ((fx - 1f) * b.Cell), b.Grid.Top + ((fy - 1f) * b.RowPitch));

        private static SKColor K(Rgba c, float alpha = 1f) => new SKColor(c.R, c.G, c.B, (byte)Math.Max(0, Math.Min(255, Math.Round(c.A * alpha))));

        private static SKPaint Stroke(SKColor c, float width) => new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = width,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = c,
        };

        private static SKPaint Fill(SKColor c) => new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = c };

        private static float Smooth(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * (3f - (2f * t));
        }

        private static float Clamp01(float t) => Math.Max(0f, Math.Min(1f, t));

        // =====================================================================================================================
        // ---- The Drop water channel ----
        // =====================================================================================================================

        private enum Phase
        {
            /// <summary>The level's start: the approach's cubes stand, the spring sleeps, the two exits faint.</summary>
            Start,

            /// <summary>The approach is cleared: the spring wakes and both exits ask for the choice.</summary>
            Choice,

            /// <summary>An exit is chosen: the water runs to it, it opens as an entry, the other fades.</summary>
            Chosen,
        }

        private sealed class ChannelMock
        {
            public string Variant = "A";
            public Phase Phase;
            public float AwakeAt = -100f;
            public float ChosenAt = -100f;
            public int Chosen;
            public bool Hand;
            public bool FaintRoutes;
            public bool ApproachMark = true;
            public int InnerCell;
            public int OuterCell;
            public float RunSeconds = 0.9f;
        }

        // L76 (the airship) as the mock's Level 101: the spring in the top of the envelope behind three cubes of the top edge,
        // the inner exit in a pocket in the cream middle, the outer one in the right frame by the nose.
        private static readonly (int X0, int Y0, int X1, int Y1) SpringCells = (22, 3, 24, 4);
        private static readonly (int X0, int Y0, int X1, int Y1) ApproachCells = (22, 2, 24, 2);
        private static readonly (int X0, int Y0, int X1, int Y1) PocketCells = (22, 12, 24, 14);
        private const int OuterRow = 13;

        private static (float X, float Y)[] InnerRoute => new[] { (23.5f, 4.2f), (23.5f, 13.0f) };

        private static (float X, float Y)[] OuterRoute(string variant) => variant == "A"
            ? new[] { (24.6f, 4.0f), (36.5f, 4.0f), (36.5f, 13.5f), (37.2f, 13.5f) }
            : new[] { (24.6f, 4.0f), (36.5f, 4.0f), (36.5f, 13.5f), (38.0f, 13.5f) };

        private static RoundLevel ChannelLevel(RoundLevel src, bool approach, int extraEntry)
        {
            char[][] rows = RowsTop(src);
            void Carve((int X0, int Y0, int X1, int Y1) r)
            {
                for (int y = r.Y0; y <= r.Y1; y++)
                {
                    for (int x = r.X0; x <= r.X1; x++)
                    {
                        rows[y][x] = '.';
                    }
                }
            }

            Carve(SpringCells);
            Carve(PocketCells);
            if (!approach)
            {
                Carve(ApproachCells);
            }

            var entries = new List<int>(src.Entries);
            if (extraEntry >= 0)
            {
                int x = extraEntry % src.Width;
                int yb = extraEntry / src.Width;
                rows[src.Height - 1 - yb][x] = '.';
                entries.Add(extraEntry);
            }

            (char, int)[][] tray =
            {
                new[] { ('C', 50), ('B', 48), ('R', 32), ('A', 22), ('Y', 11), ('N', 29) },
                new[] { ('C', 50), ('N', 30), ('B', 48), ('D', 15), ('C', 49), ('R', 31) },
                new[] { ('B', 48), ('R', 31), ('C', 50), ('Y', 11), ('A', 22), ('B', 48) },
            };
            return Copy(src, 101, rows, entries, tray);
        }

        private static void Channel(Rig rig, RoundLevel src, string variant)
        {
            var mock = new ChannelMock { Variant = variant };
            int inner = CellTop(src, 23, 13);
            int outer = variant == "A" ? CellTop(src, 37, OuterRow) : CellTop(src, 38, OuterRow);
            mock.InnerCell = inner;
            mock.OuterCell = outer;
            RoundPainter.MockUnderCubes = null;
            RoundPainter.MockOverCubes = (p, s, b) => DrawChannel(p, s, b, mock);
            string tag = "channel-" + variant;

            // 1. The start: the approach's cubes, the sleeping spring, the faint exits.
            rig.Open(ChannelLevel(src, true, -1));
            rig.Run(1.2f);
            Box crop = BoardCrop(rig.Board);
            rig.Still(tag + "-1-start", crop);

            // 2–4. The approach cleared (the level with the inner exit as an entry: nothing is tapped before the choice).
            rig.Open(ChannelLevel(src, false, inner));
            mock.Phase = Phase.Choice;
            mock.AwakeAt = rig.Painter.Now;
            rig.StartClip(tag);
            rig.Run(0.8f);
            mock.Hand = true;
            rig.Run(1.6f);
            rig.Still(tag + "-2-choice", crop);
            rig.Run(0.6f);
            mock.Hand = false;
            mock.Phase = Phase.Chosen;
            mock.Chosen = 0;
            mock.ChosenAt = rig.Painter.Now;
            rig.Run(0.45f);
            rig.Still(tag + "-3-water", crop);
            rig.Run(0.85f);
            rig.TapTop(0);
            rig.Run(0.7f);
            rig.TapTop(1);
            rig.Run(2.2f);
            rig.Still(tag + "-4-open", crop);
            rig.TapTop(2);
            rig.Run(4.5f);
            rig.EndClip();

            // 5. The other choice: the outer exit by the nose (its own entry).
            rig.Open(ChannelLevel(src, false, outer));
            mock.Phase = Phase.Chosen;
            mock.Chosen = 1;
            mock.ChosenAt = rig.Painter.Now - 3f;
            rig.Run(0.2f);
            rig.TapTop(2);
            rig.Run(0.5f);
            rig.TapTop(1);
            rig.Run(2.6f);
            rig.Still(tag + "-5-outer", crop);
        }

        /// <summary>The option frames: the routes shown faint before the choice, and the symbols mode is not needed (the channel carries no material).</summary>
        private static void ChannelExtras(Rig rig, RoundLevel src)
        {
            var mock = new ChannelMock { Variant = "A", FaintRoutes = true, InnerCell = CellTop(src, 23, 13), OuterCell = CellTop(src, 37, OuterRow) };
            RoundPainter.MockOverCubes = (p, s, b) => DrawChannel(p, s, b, mock);
            rig.Open(ChannelLevel(src, true, -1));
            rig.Run(1.2f);
            rig.Still("channel-A-option-faint-routes", BoardCrop(rig.Board));
            var mockB = new ChannelMock { Variant = "B", FaintRoutes = true, InnerCell = CellTop(src, 23, 13), OuterCell = CellTop(src, 38, OuterRow) };
            RoundPainter.MockOverCubes = (p, s, b) => DrawChannel(p, s, b, mockB);
            rig.Run(0.2f);
            rig.Still("channel-B-option-faint-routes", BoardCrop(rig.Board));
        }

        private static Box BoardCrop(RoundBoardLayout b) => b.Outer.Inset(-8f);

        private static void DrawChannel(IPainter p, RoundLevelScreen s, RoundBoardLayout b, ChannelMock m)
        {
            var sk = (SkiaPainter)p;
            SKCanvas cv = sk.Canvas;
            float now = p.Now;
            float cell = b.Cell;

            // The routes: faint dotted ones before the choice (the option), the chosen one filling with water.
            (float X, float Y)[] innerRoute = InnerRoute;
            (float X, float Y)[] outerRoute = OuterRoute(m.Variant);
            if (m.FaintRoutes && m.Phase != Phase.Chosen)
            {
                FaintRoute(cv, b, innerRoute);
                FaintRoute(cv, b, outerRoute);
            }

            if (m.Phase == Phase.Chosen)
            {
                float k = Clamp01((now - m.ChosenAt) / m.RunSeconds);
                Route(cv, b, m.Chosen == 0 ? innerRoute : outerRoute, m.Variant, now, Smooth(k));
            }

            // The spring at the channel's start.
            (float sx, float sy) = At(b, SpringCells.X0 + 1.5f, SpringCells.Y0 + 1.0f);
            float awake = m.Phase == Phase.Start ? 0f : Clamp01((now - m.AwakeAt) / 0.6f);
            Spring(p, cv, b, sx, sy + (cell * 0.1f), cell * 1.12f, awake, now, m.Phase == Phase.Choice);

            // The approach (17 §8: its condition visible): a dashed water-blue outline round the cubes to clear, while they stand.
            if (m.Phase == Phase.Start && m.ApproachMark)
            {
                (float ax, float ay) = At(b, ApproachCells.X0, ApproachCells.Y0);
                (float bx, float by) = At(b, ApproachCells.X1 + 1, ApproachCells.Y1 + 1);
                using SKPaint dash = Stroke(K(Rgba.White, 0.95f), Math.Max(2f, cell * 0.09f));
                dash.PathEffect = SKPathEffect.CreateDash(new[] { cell * 0.28f, cell * 0.18f }, now * cell * 0.6f % (cell * 0.46f));
                using SKPaint under = Stroke(K(C.WaterDeep, 0.9f), Math.Max(3f, cell * 0.17f));
                var rect = new SKRect(ax - (cell * 0.08f), ay - (cell * 0.06f), bx + (cell * 0.08f), by + (b.Front * 0.9f));
                cv.DrawRoundRect(rect, cell * 0.25f, cell * 0.25f, under);
                cv.DrawRoundRect(rect, cell * 0.25f, cell * 0.25f, dash);
            }

            // The exits.
            for (int e = 0; e < 2; e++)
            {
                bool isInner = e == 0;
                float alpha;
                float glow = 0f;
                float open = 0f;
                if (m.Phase == Phase.Start)
                {
                    alpha = 0.62f;
                }
                else if (m.Phase == Phase.Choice)
                {
                    alpha = 1f;
                    glow = RoundBoardLayout.BridgePulse(now);
                }
                else if (m.Chosen == e)
                {
                    alpha = 1f;
                    open = Clamp01((now - m.ChosenAt - (m.RunSeconds * 0.85f)) / 0.45f);
                }
                else
                {
                    alpha = 0.62f * (1f - Clamp01((now - m.ChosenAt) / 0.6f));
                }

                if (alpha <= 0.01f)
                {
                    continue;
                }

                if (isInner)
                {
                    (float cx, float cy) = At(b, 23.5f, 13.5f);
                    Exit(p, cv, b, m.Variant, cx, cy, EntrySide.Bottom, alpha, glow, open, now, m.Phase);
                }
                else if (m.Variant == "A")
                {
                    (float cx, float cy) = At(b, 37.5f, OuterRow + 0.5f);
                    Exit(p, cv, b, m.Variant, cx, cy, EntrySide.Right, alpha, glow, open, now, m.Phase);
                }
                else
                {
                    // The outer arch in the right frame, where the game puts an entry on that side.
                    (float _, float cy) = At(b, 37.5f, OuterRow + 0.5f);
                    float mid = (BoardLayout.Gap + (BoardLayout.Stone / 2f)) * b.Unit;
                    Exit(p, cv, b, m.Variant, b.Surface.Right + mid, cy, EntrySide.Right, alpha, glow, open, now, m.Phase);
                }
            }

            // The hand of the guides over the inner exit (the first introduction's hint).
            if (m.Hand)
            {
                (float cx, float cy) = At(b, 23.5f, 13.5f);
                float side = p.U(Spotlight.HandUnits);
                float bob = side * 0.08f * (float)Math.Sin(now * 6.0);
                Box hand = new Box(cx - (side * 0.18f), cy + (cell * 0.3f) + bob, cx - (side * 0.18f) + side, cy + (cell * 0.3f) + side + bob);
                p.Shape("ui.pointer", hand.Inset(-p.U(5f)), C.InkBrown);
                p.Shape("ui.pointer", hand, Rgba.White);
            }
        }

        private static SKPath PathOf(RoundBoardLayout b, (float X, float Y)[] route)
        {
            var path = new SKPath();
            for (int i = 0; i < route.Length; i++)
            {
                (float x, float y) = At(b, route[i].X, route[i].Y);
                if (i == 0)
                {
                    path.MoveTo(x, y);
                }
                else
                {
                    path.LineTo(x, y);
                }
            }

            return path;
        }

        private static void FaintRoute(SKCanvas cv, RoundBoardLayout b, (float X, float Y)[] route)
        {
            using SKPath path = PathOf(b, route);
            float cell = b.Cell;
            using SKPaint under = Stroke(K(C.WaterDeep, 0.55f), cell * 0.26f);
            under.PathEffect = SKPathEffect.CreateDash(new[] { 0.01f, cell * 0.62f }, 0f);
            using SKPaint dot = Stroke(K(C.WaterFoam, 0.95f), cell * 0.15f);
            dot.PathEffect = SKPathEffect.CreateDash(new[] { 0.01f, cell * 0.62f }, 0f);
            cv.DrawPath(path, under);
            cv.DrawPath(path, dot);
        }

        /// <summary>The chosen route, grown to <paramref name="k"/> of its length: A an open stream between cream stone banks, B a wooden trough on posts.</summary>
        private static void Route(SKCanvas cv, RoundBoardLayout b, (float X, float Y)[] route, string variant, float now, float k)
        {
            if (k <= 0f)
            {
                return;
            }

            float cell = b.Cell;
            using SKPath full = PathOf(b, route);
            using var measure = new SKPathMeasure(full, false);
            float length = measure.Length;
            using var path = new SKPath();
            measure.GetSegment(0f, length * k, path, true);
            float lift = b.Front;
            float line = Math.Max(1f, cell * 0.05f);

            // Its soft shadow on the cubes.
            using (SKPaint shadow = Stroke(K(C.WoodDarkLine, 0.3f), cell * 0.95f))
            {
                shadow.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, cell * 0.14f);
                cv.Save();
                cv.Translate(cell * 0.08f, lift + (cell * 0.18f));
                cv.DrawPath(path, shadow);
                cv.Restore();
            }

            float w = cell * (variant == "A" ? 1.2f : 1.08f);
            float water = w * (variant == "A" ? 0.6f : 0.52f);
            if (variant == "B")
            {
                // The trough's posts, down on to the cubes every two and a half cells.
                using var posts = new SKPathMeasure(path, false);
                for (float d = cell * 1.2f; d < posts.Length - (cell * 0.6f); d += cell * 2.5f)
                {
                    posts.GetPosition(d, out SKPoint pos);
                    var post = new SKRect(pos.X - (cell * 0.12f), pos.Y, pos.X + (cell * 0.12f), pos.Y + lift + (cell * 0.42f));
                    cv.DrawRoundRect(post, cell * 0.05f, cell * 0.05f, Fill(K(C.WoodDarkLine)));
                    post.Inflate(-line, -line);
                    cv.DrawRoundRect(post, cell * 0.04f, cell * 0.04f, Fill(K(C.WoodDark)));
                }
            }

            // Its front face under it (the board's fixed viewpoint, as the cubes'), then its top.
            Rgba side = variant == "A" ? C.StoneLip : C.WoodDark;
            Rgba top = variant == "A" ? C.StoneTop : C.WoodDarkTop;
            Rgba edge = variant == "A" ? C.StoneLine : C.WoodDarkLine;
            cv.Save();
            cv.Translate(0f, lift * 0.75f);
            using (SKPaint o = Stroke(K(edge), w + (2f * line)))
            {
                cv.DrawPath(path, o);
            }

            using (SKPaint f = Stroke(K(side), w))
            {
                cv.DrawPath(path, f);
            }

            cv.Restore();
            using (SKPaint o = Stroke(K(edge), w + (2f * line)))
            {
                cv.DrawPath(path, o);
            }

            using (SKPaint t = Stroke(K(top), w))
            {
                cv.DrawPath(path, t);
            }

            if (variant == "A")
            {
                // The banks' blocks: joints across them every cell.
                DrawAcross(cv, path, cell * 0.9f, w, Stroke(K(C.StoneLip, 0.9f), Math.Max(1f, cell * 0.06f)));
            }
            else
            {
                // The planks' grain along it and the hoops across it.
                using SKPaint grain = Stroke(K(C.WoodGrain, 0.9f), w * 0.8f);
                cv.DrawPath(path, grain);
                DrawAcross(cv, path, cell * 1.25f, w * 1.04f, Stroke(K(C.WoodDarkLine), Math.Max(1.5f, cell * 0.11f)));
            }

            // The water in it.
            using (SKPaint o = Stroke(K(edge, 0.9f), water + (2f * line)))
            {
                cv.DrawPath(path, o);
            }

            using (SKPaint d = Stroke(K(C.WaterDeep), water))
            {
                cv.DrawPath(path, d);
            }

            using (SKPaint l = Stroke(K(C.WaterTop, 0.7f), water * 0.5f))
            {
                cv.DrawPath(path, l);
            }

            // The flow: foam dashes running from the spring to the exit.
            float period = cell * 1.05f;
            float phase = period - (now * cell * 2.4f % period);
            using (SKPaint foam = Stroke(K(C.WaterFoam, 0.85f), Math.Max(1.5f, cell * 0.07f)))
            {
                foam.PathEffect = SKPathEffect.CreateDash(new[] { cell * 0.3f, period - (cell * 0.3f) }, phase);
                cv.DrawPath(path, foam);
            }

            if (k < 1f)
            {
                // The water's head running on.
                using var head = new SKPathMeasure(path, false);
                head.GetPosition(head.Length, out SKPoint tip);
                cv.DrawCircle(tip.X, tip.Y, water * 0.6f, Fill(K(C.WaterFoam, 0.9f)));
                cv.DrawCircle(tip.X, tip.Y, water * 0.35f, Fill(K(C.WaterTop)));
            }
        }

        private static void DrawAcross(SKCanvas cv, SKPath path, float every, float width, SKPaint paint)
        {
            using (paint)
            {
                using var measure = new SKPathMeasure(path, false);
                for (float d = every * 0.5f; d < measure.Length; d += every)
                {
                    measure.GetPositionAndTangent(d, out SKPoint pos, out SKPoint tan);
                    float nx = -tan.Y * width / 2f;
                    float ny = tan.X * width / 2f;
                    cv.DrawLine(pos.X - nx, pos.Y - ny, pos.X + nx, pos.Y + ny, paint);
                }
            }
        }

        /// <summary>
        /// The spring at the channel's start: a round well of the frame's cream stone seen from the board's viewpoint (its top
        /// ring of blocks over its front), still dark water while it sleeps; awake, the water rises and rings, and a white drop
        /// bobs over it.
        /// </summary>
        private static void Spring(IPainter p, SKCanvas cv, RoundBoardLayout b, float cx, float cy, float rx, float awake, float now, bool calling)
        {
            float ry = rx * b.Look.RowShare;
            float front = b.Front * 0.8f;
            float line = Math.Max(1f, b.Cell * 0.05f);
            using (SKPaint sh = Fill(K(C.WoodDarkLine, 0.28f)))
            {
                sh.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, b.Cell * 0.15f);
                cv.DrawOval(cx + (b.Cell * 0.06f), cy + front + (b.Cell * 0.12f), rx, ry, sh);
            }

            cv.DrawOval(cx, cy + front, rx + line, ry + line, Fill(K(C.StoneLine)));
            cv.DrawOval(cx, cy + front, rx, ry, Fill(K(C.StoneLip)));
            cv.DrawRect(cx - rx, cy, rx * 2f, front, Fill(K(C.StoneLip)));
            cv.DrawLine(cx - rx - (line / 2f), cy, cx - rx - (line / 2f), cy + front, Stroke(K(C.StoneLine), line));
            cv.DrawLine(cx + rx + (line / 2f), cy, cx + rx + (line / 2f), cy + front, Stroke(K(C.StoneLine), line));
            cv.DrawOval(cx, cy, rx + line, ry + line, Fill(K(C.StoneLine)));
            using (SKPaint top = Fill(K(C.StoneTop)))
            {
                top.Shader = SKShader.CreateLinearGradient(new SKPoint(cx - rx, cy - ry), new SKPoint(cx + rx, cy + ry), new[] { K(C.StoneTop), K(C.StoneFace) }, SKShaderTileMode.Clamp);
                cv.DrawOval(cx, cy, rx, ry, top);
            }

            float ix = rx * 0.64f;
            float iy = ry * 0.64f;
            using (SKPaint joint = Stroke(K(C.StoneLip), Math.Max(1f, b.Cell * 0.06f)))
            {
                for (int i = 0; i < 8; i++)
                {
                    double a = (i + 0.5) * Math.PI / 4.0;
                    cv.DrawLine(cx + (float)(Math.Cos(a) * ix), cy + (float)(Math.Sin(a) * iy), cx + (float)(Math.Cos(a) * rx), cy + (float)(Math.Sin(a) * ry), joint);
                }
            }

            cv.DrawOval(cx, cy, ix + line, iy + line, Fill(K(C.StoneLine)));
            Rgba deep = C.WaterDeep.Darken(0.35f * (1f - awake));
            using (SKPaint w = Fill(K(deep)))
            {
                w.Shader = SKShader.CreateRadialGradient(new SKPoint(cx - (ix * 0.2f), cy - (iy * 0.25f)), ix, new[] { K(C.WaterTop.Mix(deep, 1f - awake)), K(deep) }, SKShaderTileMode.Clamp);
                cv.DrawOval(cx, cy, ix, iy, w);
            }

            if (awake > 0f)
            {
                // Rings spreading on the water.
                cv.Save();
                using var clip = new SKPath();
                clip.AddOval(new SKRect(cx - ix, cy - iy, cx + ix, cy + iy));
                cv.ClipPath(clip, SKClipOperation.Intersect, true);
                for (int r = 0; r < 2; r++)
                {
                    float t = ((now * 0.8f) + (r * 0.5f)) % 1f;
                    cv.DrawOval(cx, cy, ix * t * 1.1f, iy * t * 1.1f, Stroke(K(C.WaterFoam, 0.8f * (1f - t) * awake), Math.Max(1f, b.Cell * 0.06f)));
                }

                cv.Restore();

                // The Drop's sign: a white drop bobbing over the spring.
                float bob = (float)Math.Sin(now * 3.0) * b.Cell * 0.12f;
                float d = b.Cell * 0.95f;
                Box drop = Box.FromCenter(cx, cy - ry - (d * 0.55f) + bob, d * 0.8f, d);
                p.PushAlpha(awake);
                p.Shape("fx.droplet", drop.Inset(-p.U(3f)), C.WaterDeep);
                p.Shape("fx.droplet", drop, Rgba.White);
                p.PopAlpha();
            }

            if (calling)
            {
                float pulse = RoundBoardLayout.BridgePulse(now);
                cv.DrawOval(cx, cy + (front * 0.5f), rx * (1.12f + (0.06f * pulse)), (ry + (front * 0.5f)) * (1.12f + (0.06f * pulse)),
                    Stroke(K(C.GardenGlow, 0.35f + (0.3f * pulse)), Math.Max(2f, b.Cell * 0.1f)));
            }
        }

        /// <summary>
        /// An exit: A a small spring well (the start's look, smaller) with its drop; B the game's entry arch with a pool at its
        /// foot (in the frame for the outer exit, turned to its side as the game turns an entry there). Faint before the choice,
        /// breathing in the glow ring while it can be chosen, opening with a splash once chosen.
        /// </summary>
        private static void Exit(IPainter p, SKCanvas cv, RoundBoardLayout b, string variant, float cx, float cy, EntrySide side, float alpha, float glow, float open, float now, Phase phase)
        {
            float cell = b.Cell;
            float breath = 1f + (RoundBoardLayout.BridgeBreath * 1.5f * glow);
            if (glow > 0f || phase == Phase.Choice)
            {
                // The glow ring of a choice (the buildable bridge's).
                float r = cell * (variant == "A" ? 1.6f : 1.8f) * (1f + (0.05f * glow));
                cv.DrawCircle(cx, cy, r, Stroke(K(C.GardenGlow, RoundBoardLayout.BridgeRingAlpha(glow)), Math.Max(2f, cell * RoundBoardLayout.BridgeRingStroke * 1.4f)));
            }
            else if (phase == Phase.Start)
            {
                // The faint exit's dashed ring.
                using SKPaint dash = Stroke(K(Rgba.White, 0.9f * alpha * 1.6f), Math.Max(2f, cell * 0.08f));
                dash.PathEffect = SKPathEffect.CreateDash(new[] { cell * 0.25f, cell * 0.2f }, 0f);
                cv.DrawCircle(cx, cy, cell * (variant == "A" ? 1.5f : 1.7f), dash);
            }

            // The splash of the opening.
            if (open > 0f && open < 1f)
            {
                for (int i = 0; i < 10; i++)
                {
                    double a = (i * Math.PI * 2.0 / 10.0) + 0.3;
                    float d = cell * (0.4f + (1.6f * open));
                    float y = -cell * 1.2f * open * (1f - open) * 2f;
                    cv.DrawCircle(cx + (float)(Math.Cos(a) * d), cy + ((float)Math.Sin(a) * d * 0.6f) + y, cell * 0.16f * (1f - open), Fill(K(C.WaterFoam, 0.95f)));
                    cv.DrawCircle(cx + (float)(Math.Cos(a) * d), cy + ((float)Math.Sin(a) * d * 0.6f) + y, cell * 0.1f * (1f - open), Fill(K(C.WaterTop)));
                }
            }

            float pop = open > 0f && open < 1f ? 1f + (0.15f * (float)Math.Sin(open * Math.PI)) : 1f;
            if (variant == "A")
            {
                p.PushAlpha(alpha);
                using (var layer = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(alpha * 255)) })
                {
                    cv.SaveLayer(layer);
                    cv.Translate(cx, cy);
                    cv.Scale(breath * pop);
                    cv.Translate(-cx, -cy);
                    Spring(p, cv, b, cx, cy - (cell * 0.15f), cell * 1.15f, phase == Phase.Start ? 0.35f : 1f, now, false);
                    cv.Restore();
                }

                p.PopAlpha();
            }
            else
            {
                float wArch = cell * 3.0f * breath * pop;
                float hArch = wArch * BoardLayout.ArchHeight / BoardLayout.ArchWidth;
                p.PushAlpha(alpha);
                if (side == EntrySide.Bottom)
                {
                    // The pool at its foot, then the arch upright with its opening over the exit's cell.
                    cv.DrawOval(cx, cy + (cell * 0.45f), cell * 1.45f, cell * 0.6f, Fill(K(C.StoneLine, alpha)));
                    cv.DrawOval(cx, cy + (cell * 0.45f), cell * 1.33f, cell * 0.5f, Fill(K(C.WaterDeep, alpha)));
                    cv.DrawOval(cx, cy + (cell * 0.4f), cell * 0.75f, cell * 0.24f, Fill(K(C.WaterTop, 0.8f * alpha)));
                    Box arch = new Box(cx - (wArch / 2f), cy + (cell * 0.65f) - hArch, cx + (wArch / 2f), cy + (cell * 0.65f));
                    p.Picture("board.entry.arch", arch, UiRaster.EntryArch);
                }
                else
                {
                    (Box box, float degrees) = BoardLayout.ArchAt((cx, cy), side, b.Unit);
                    Box scaled = Box.FromCenter(box.CenterX, box.CenterY, box.Width * breath * pop, box.Height * breath * pop);
                    Kit.EntryArch(p, (scaled, degrees));
                }

                p.PopAlpha();
            }
        }

        // =====================================================================================================================
        // ---- The Twig wooden shutters ----
        // =====================================================================================================================

        private sealed class ShutterMock
        {
            public string Variant = "A";
            public (int X0, int Y0, int X1, int Y1) Cells;
            public char Material;
            public int Start = 30;

            /// <summary>Count the walkers' cleared cubes of the material (else the counter stays at <see cref="Start"/>).</summary>
            public bool Live;

            public float OpenAt = -1f;
            public float OpenSeconds = 0.95f;

            /// <summary>A still's fixed opening progress (0–1), or negative.</summary>
            public float FixedOpen = -1f;
        }

        private static (int X0, int Y0, int X1, int Y1) ShutterRect(int level) => level switch
        {
            91 => (13, 5, 20, 9),
            61 => (8, 17, 13, 21),
            _ => (4, 19, 9, 23),
        };

        private static RoundLevel ShutterLevel(RoundLevel src, int number, (char, int)[][] tray) => Copy(src, number, RowsTop(src), src.Entries, tray);

        private static void Shutters(Rig rig, RoundLevel src, string variant)
        {
            var mock = new ShutterMock { Variant = variant, Cells = ShutterRect(91), Material = 'G', Start = 30, Live = true };
            RoundPainter.MockOverCubes = (p, s, b) => DrawShutter(p, s, b, mock);
            string tag = "shutters-" + variant;
            (char, int)[][] tray =
            {
                new[] { ('G', 36), ('M', 23), ('Y', 51), ('C', 52), ('P', 36), ('Y', 50) },
                new[] { ('M', 23), ('G', 36), ('C', 52), ('Y', 51), ('O', 27), ('D', 25) },
                new[] { ('D', 25), ('O', 27), ('G', 36), ('Y', 50), ('C', 51), ('P', 36) },
            };
            rig.Open(ShutterLevel(src, 121, tray));
            rig.Run(0.6f);
            Box crop = BoardCrop(rig.Board);
            rig.Still(tag + "-1-closed", crop);
            rig.StartClip(tag);
            rig.Run(0.4f);
            rig.TapTop(0);
            rig.Run(0.8f);
            rig.TapTop(1);
            rig.Run(0.8f);
            rig.TapTop(2);
            bool ticked = false;
            bool opening = false;
            for (int f = 0; f < Fps * 26f && (mock.OpenAt < 0f || rig.Painter.Now < mock.OpenAt + 3.2f); f++)
            {
                rig.Frame(1f / Fps);
                int left = Remaining(rig.S, rig.Board, mock);
                if (!ticked && left <= 22 && left >= 18 && SinceLastClear(rig.S, rig.Board, mock) < 0.12f)
                {
                    rig.Still(tag + "-2-counting", crop);
                    ticked = true;
                }

                if (!opening && mock.OpenAt >= 0f && rig.Painter.Now >= mock.OpenAt + (mock.OpenSeconds * 0.42f))
                {
                    rig.Still(tag + "-3-opening", crop);
                    opening = true;
                }
            }

            rig.Still(tag + "-4-open", crop);
            rig.EndClip();

            // The symbols mode (Settings, off by default): the material's symbol on the counter too.
            var closed = new ShutterMock { Variant = variant, Cells = mock.Cells, Material = 'G', Start = 30 };
            RoundPainter.MockOverCubes = (p, s, b) => DrawShutter(p, s, b, closed);
            rig.App.Meta.Save.Settings.Symbols = true;
            rig.Open(ShutterLevel(src, 121, tray));
            rig.Run(0.4f);
            rig.Still(tag + "-5-symbols", crop);
            rig.App.Meta.Save.Settings.Symbols = false;
        }

        private static void ShutterSizes(Rig rig, RoundContentSet rounds, string variant)
        {
            foreach ((int level, char material, int start, string name) in new[] { (61, 'M', 60, "small"), (72, 'G', 30, "big") })
            {
                RoundLevel src = rounds.GetLevel(level);
                var mock = new ShutterMock { Variant = variant, Cells = ShutterRect(level), Material = material, Start = start };
                RoundPainter.MockOverCubes = (p, s, b) => DrawShutter(p, s, b, mock);
                (char, int)[][] tray = level == 61
                    ? new[] { new[] { ('M', 60), ('P', 58), ('C', 54) }, new[] { ('P', 58), ('M', 60), ('G', 39) }, new[] { ('C', 54), ('L', 30), ('M', 59) }, new[] { ('G', 38), ('D', 23), ('P', 57) } }
                    : new[] { new[] { ('G', 39), ('V', 39), ('C', 15) }, new[] { ('V', 39), ('G', 39), ('D', 10) }, new[] { ('M', 13), ('V', 38), ('G', 39) } };
                rig.Open(ShutterLevel(src, level == 61 ? 123 : 127, tray));
                rig.Run(0.4f);
                rig.Still("shutters-" + variant + "-6-" + name + "-cubes", BoardCrop(rig.Board));
            }
        }

        private static bool Under(RoundBoardLayout b, ShutterMock m, int cell)
        {
            int x = cell % b.Width;
            int yTop = b.Height - 1 - (cell / b.Width);
            return x >= m.Cells.X0 && x <= m.Cells.X1 && yTop >= m.Cells.Y0 && yTop <= m.Cells.Y1;
        }

        private static int Done(RoundLevelScreen s, RoundBoardLayout b, ShutterMock m, out long last)
        {
            last = long.MinValue;
            if (!m.Live)
            {
                return 0;
            }

            RoundAnimator a = s.Animator;
            int material = RoundMaterial.IndexOf(m.Material);
            int done = 0;
            for (int cell = 0; cell < b.Width * b.Height; cell++)
            {
                if (a.OriginalMaterial(cell) == material && a.MaterialAt(cell) < 0 && !Under(b, m, cell))
                {
                    long at = a.ClearedAt(cell);
                    if (at <= a.Now)
                    {
                        done++;
                        last = Math.Max(last, at);
                    }
                }
            }

            return done;
        }

        private static int Remaining(RoundLevelScreen s, RoundBoardLayout b, ShutterMock m) => Math.Max(0, m.Start - Done(s, b, m, out _));

        private static float SinceLastClear(RoundLevelScreen s, RoundBoardLayout b, ShutterMock m)
        {
            Done(s, b, m, out long last);
            return last == long.MinValue ? 99f : (s.Animator.Now - last) / 1_000_000f;
        }

        private static void DrawShutter(IPainter p, RoundLevelScreen s, RoundBoardLayout b, ShutterMock m)
        {
            var sk = (SkiaPainter)p;
            SKCanvas cv = sk.Canvas;
            float now = p.Now;
            float cell = b.Cell;
            int done = Done(s, b, m, out long last);
            int left = Math.Max(0, m.Start - done);
            if (left == 0 && m.OpenAt < 0f && m.Live)
            {
                m.OpenAt = now;
            }

            float open = m.FixedOpen >= 0f ? m.FixedOpen : m.OpenAt < 0f ? 0f : Clamp01((now - m.OpenAt) / m.OpenSeconds);
            if (open >= 1f)
            {
                return;
            }

            (float l, float t) = At(b, m.Cells.X0, m.Cells.Y0);
            (float r, float bt) = At(b, m.Cells.X1 + 1, m.Cells.Y1 + 1);
            float lid = cell * 0.14f;
            var top = new Box(l, t - lid, r, bt - lid);
            float front = b.Front + lid;
            RoundMaterial material = RoundMaterial.Of(m.Material);
            Rgba color = RoundLook.ColorOf(material);
            float pop = last == long.MinValue ? 0f : Clamp01(1f - ((s.Animator.Now - last) / 250_000f));

            // The counted cubes flying to the counter as small sparks of their colour (the work of that material anywhere).
            Box counter = CounterBox(b, top, m.Variant);
            if (m.Live && open <= 0f)
            {
                RoundAnimator a = s.Animator;
                int mi = RoundMaterial.IndexOf(m.Material);
                for (int cell2 = 0; cell2 < b.Width * b.Height; cell2++)
                {
                    if (a.OriginalMaterial(cell2) != mi || a.MaterialAt(cell2) >= 0 || Under(b, m, cell2))
                    {
                        continue;
                    }

                    float k = (a.Now - a.ClearedAt(cell2)) / 450_000f;
                    if (k < 0f || k > 1f)
                    {
                        continue;
                    }

                    Box from = b.CellBox(cell2);
                    float e = Smooth(k);
                    float x = from.CenterX + ((counter.CenterX - from.CenterX) * e);
                    float y = from.CenterY + ((counter.CenterY - from.CenterY) * e) - ((float)Math.Sin(k * Math.PI) * cell * 2.2f);
                    float rr = Math.Max(5f, cell * 0.34f) * (1f - (0.35f * k));
                    cv.DrawCircle(x, y, rr * 1.4f, Fill(K(Rgba.White, 0.85f)));
                    cv.DrawCircle(x, y, rr, Fill(K(color)));
                }
            }

            if (m.Variant == "A")
            {
                Doors(p, cv, b, top, front, open, m.Cells);
            }
            else
            {
                Slats(p, cv, b, top, front, open, m.Cells);
            }

            // The nearer row's cubes in front of the lid (the board's oblique order: a nearer cube hides what stands behind it).
            int yb = b.Height - 1 - (m.Cells.Y1 + 1);
            for (int x = m.Cells.X0; x <= m.Cells.X1 && open <= 0f; x++)
            {
                int c = (yb * b.Width) + x;
                int mat = s.Animator.MaterialAt(c);
                if (b.KindOf(c) == RoundCellKind.Material && mat >= 0)
                {
                    Kit.BoardCube(p, b.CubeBox(c), RoundMaterial.All[mat], s.Symbols, b.Look);
                }
            }

            // Dust and leaves as it opens.
            if (open > 0f)
            {
                Dust(p, cv, top, front, cell, open);
            }

            // The counter: A a round plaque on the doors, B a tag hanging from a nail; it pops at each counted cube.
            float fade = 1f - Clamp01(open / 0.35f);
            if (fade > 0f)
            {
                float scale = 1f + (0.2f * (float)Math.Sin(pop * Math.PI)) + (open > 0f ? 0.25f * open : 0f);
                p.PushAlpha(fade);
                Counter(p, cv, b, counter, m.Variant, material, color, left, scale, s.Symbols, now);
                p.PopAlpha();
            }
        }

        private static Box CounterBox(RoundBoardLayout b, Box top, string variant)
        {
            float d = Math.Max(Math.Min(top.Width, top.Height) * 0.5f, 58f);
            d = Math.Min(d, Math.Min(top.Width, top.Height) * 0.92f);
            d = Math.Max(d, 58f);
            return variant == "A"
                ? Box.FromCenter(top.CenterX, top.CenterY, d, d)
                : Box.FromCenter(top.CenterX, top.Top + (top.Height * 0.56f), d * 0.95f, d * 0.95f);
        }

        private static void Counter(IPainter p, SKCanvas cv, RoundBoardLayout b, Box box, string variant, RoundMaterial material, Rgba color, int left, float scale, bool symbols, float now)
        {
            if (variant == "A")
            {
                // A round wooden plaque (the icon buttons' plate) with the material's ball and its number in the pods' look.
                Box plate = Box.FromCenter(box.CenterX, box.CenterY, box.Width * scale, box.Height * scale);
                int ps = Math.Max(8, (int)plate.Width);
                p.Picture("mock.shutter.plate", plate, (w, h) => UiRaster.ButtonPlate(w, h, 0.5f));
                Box ball = plate.Inset(plate.Width * 0.13f);
                p.Picture("mock.shutter.ball/" + material.Code, ball, (w, h) => UiRaster.Ball(Math.Min(w, h), color, color.Darken(0.18f), color.Darken(0.3f), color.Darken(0.55f)));
                Box count = Box.FromCenter(ball.CenterX, ball.CenterY + (ball.Height * 0.02f), ball.Width * 0.86f, ball.Height * 0.5f);
                Kit.CubeCount(p, count, left, false);
                if (symbols)
                {
                    Box sym = Box.FromCenter(plate.Right - (plate.Width * 0.12f), plate.Top + (plate.Height * 0.12f), plate.Width * 0.36f, plate.Width * 0.36f);
                    p.Picture("mock.shutter.symbol.disc", sym, (w, h) => UiRaster.ToggleKnob(Math.Min(w, h)));
                    p.Shape(RoundLook.SymbolOf(material), sym.Inset(sym.Width * 0.2f), color.Darken(0.25f));
                }

                _ = ps;
            }
            else
            {
                // A tag hanging on a string from a nail: the pod's own cube with its number, swinging a little.
                float sway = (float)Math.Sin(now * 1.7) * 4f;
                float nailY = box.Top - (box.Height * 0.62f);
                float nailX = box.CenterX;
                Box cube = Box.FromCenter(box.CenterX, box.CenterY, box.Width * scale, box.Height * scale);
                p.PushRotate(sway, nailX, nailY);
                using (SKPaint str = Stroke(K(C.WoodDarkLine), Math.Max(1.5f, b.Cell * 0.07f)))
                {
                    cv.DrawLine(nailX, nailY, cube.Left + (cube.Width * 0.3f), cube.Top + (cube.Height * 0.12f), str);
                    cv.DrawLine(nailX, nailY, cube.Right - (cube.Width * 0.3f), cube.Top + (cube.Height * 0.12f), str);
                }

                Kit.PlacedCube(p, cube, material, left, false, symbols);
                p.PopTransform();
                float nail = Math.Max(3f, b.Cell * 0.16f);
                cv.DrawCircle(nailX, nailY, nail * 1.2f, Fill(K(C.WoodDarkLine)));
                cv.DrawCircle(nailX, nailY, nail * 0.8f, Fill(K(C.StoneTop.Mix(C.StoneLine, 0.4f))));
            }
        }

        /// <summary>
        /// Variant A: two doors of upright honey planks (the signs' and the bridge's wood) on dark cross battens, iron hinges at
        /// the outer edges, a seam in the middle, over the lid's darker front; opening, each door swings out on its hinge.
        /// </summary>
        private static void Doors(IPainter p, SKCanvas cv, RoundBoardLayout b, Box top, float front, float open, (int X0, int Y0, int X1, int Y1) cells)
        {
            float cell = b.Cell;
            float line = Math.Max(1f, cell * 0.06f);
            float swing = Smooth(open / 0.8f);
            if (open <= 0f)
            {
                using SKPaint sh = Fill(K(C.WoodDarkLine, 0.3f));
                sh.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, cell * 0.25f);
                cv.DrawRoundRect(new SKRect(top.Left + (cell * 0.1f), top.Top + (cell * 0.3f), top.Right + (cell * 0.2f), top.Bottom + front + (cell * 0.3f)), cell * 0.3f, cell * 0.3f, sh);
            }

            float half = top.Width / 2f;
            for (int d = 0; d < 2; d++)
            {
                bool leftDoor = d == 0;
                float hinge = leftDoor ? top.Left : top.Right;
                var door = new Box(leftDoor ? top.Left : top.CenterX, top.Top, leftDoor ? top.CenterX : top.Right, top.Bottom);

                // The swing: the door narrows toward its hinge (it turns out of the board), then past the side it shows its back.
                float angle = swing * 115f;
                float sx = (float)Math.Cos(angle * Math.PI / 180.0);
                float alpha = 1f - Clamp01((open - 0.6f) / 0.4f);
                if (alpha <= 0f)
                {
                    continue;
                }

                p.PushAlpha(alpha);
                p.PushSquash(sx, 1f, hinge, door.CenterY);

                // the door's front edge (the lid's thickness)
                var edge = new SKRect(door.Left, door.Bottom - (cell * 0.05f), door.Right, door.Bottom + front);
                cv.DrawRoundRect(edge, cell * 0.12f, cell * 0.12f, Fill(K(C.WoodDarkLine, alpha)));
                edge.Inflate(-line, -line);
                cv.DrawRoundRect(edge, cell * 0.1f, cell * 0.1f, Fill(K(sx >= 0f ? C.WoodDark : C.WoodDarkLine.Mix(C.WoodDark, 0.5f), alpha)));

                int planks = Math.Max(2, (int)Math.Round(half / (cell * 0.95f)));
                float pw = door.Width / planks;
                for (int k = 0; k < planks; k++)
                {
                    var plank = new Box(door.Left + (k * pw), door.Top, door.Left + ((k + 1) * pw), door.Bottom);
                    int seed = (d * 7) + k + cells.X0;
                    p.PushRotate(90f, plank.CenterX, plank.CenterY);
                    Box turned = Box.FromCenter(plank.CenterX, plank.CenterY, plank.Height, plank.Width);
                    p.Picture("mock.shutter.plank/" + (seed % 5), turned, (w, h) => UiRaster.Plank(w, h, Math.Min(w, h) * 0.18f, Math.Max(1f, Math.Min(w, h) * 0.06f), WoodTone.Light, seed % 5));
                    p.PopTransform();
                }

                // the cross battens and the iron hinges on them
                foreach (float at in new[] { 0.2f, 0.8f })
                {
                    float y = door.Top + (door.Height * at);
                    float bh = Math.Max(cell * 0.55f, door.Height * 0.12f);
                    var batten = new Box(door.Left + (cell * 0.1f), y - (bh / 2f), door.Right - (cell * 0.1f), y + (bh / 2f));
                    p.Picture("mock.shutter.batten", batten, (w, h) => UiRaster.Plank(w, h, Math.Min(w, h) * 0.25f, Math.Max(1f, Math.Min(w, h) * 0.08f), WoodTone.Dark, 3));
                    float hx0 = leftDoor ? door.Left - (cell * 0.12f) : door.Right - (half * 0.42f);
                    float hx1 = leftDoor ? door.Left + (half * 0.42f) : door.Right + (cell * 0.12f);
                    var strap = new SKRect(hx0, y - (bh * 0.24f), hx1, y + (bh * 0.24f));
                    cv.DrawRoundRect(strap, bh * 0.2f, bh * 0.2f, Fill(K(new Rgba(70, 64, 60), alpha)));
                    strap.Inflate(-line * 0.8f, -line * 0.8f);
                    cv.DrawRoundRect(strap, bh * 0.16f, bh * 0.16f, Fill(K(new Rgba(118, 110, 104), alpha)));
                    for (int n = 0; n < 2; n++)
                    {
                        float nx = strap.Left + (strap.Width * (n == 0 ? 0.25f : 0.75f));
                        cv.DrawCircle(nx, y, Math.Max(1.5f, bh * 0.08f), Fill(K(new Rgba(205, 200, 190), alpha)));
                    }
                }

                if (sx < 0.35f)
                {
                    // turned away from the light as it opens
                    cv.DrawRect(new SKRect(door.Left, door.Top, door.Right, door.Bottom + front), Fill(K(C.WoodDarkLine, alpha * 0.45f * (1f - Math.Max(0f, sx)))));
                }

                p.PopTransform();
                p.PopAlpha();
            }

            if (open <= 0f)
            {
                // the seam between the doors
                cv.DrawLine(top.CenterX, top.Top + line, top.CenterX, top.Bottom + front - line, Stroke(K(C.WoodDarkLine), Math.Max(2f, cell * 0.1f)));
            }
        }

        /// <summary>
        /// Variant B: a crate lid of level honey slats (each the signs' plank) between two dark side battens over the lid's
        /// front; opening, the slats pop off one after another from the top, lifting and turning away.
        /// </summary>
        private static void Slats(IPainter p, SKCanvas cv, RoundBoardLayout b, Box top, float front, float open, (int X0, int Y0, int X1, int Y1) cells)
        {
            float cell = b.Cell;
            float line = Math.Max(1f, cell * 0.06f);
            if (open <= 0f)
            {
                using SKPaint sh = Fill(K(C.WoodDarkLine, 0.3f));
                sh.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, cell * 0.25f);
                cv.DrawRoundRect(new SKRect(top.Left + (cell * 0.1f), top.Top + (cell * 0.3f), top.Right + (cell * 0.2f), top.Bottom + front + (cell * 0.3f)), cell * 0.3f, cell * 0.3f, sh);

                var edge = new SKRect(top.Left, top.Bottom - (cell * 0.05f), top.Right, top.Bottom + front);
                cv.DrawRoundRect(edge, cell * 0.14f, cell * 0.14f, Fill(K(C.WoodDarkLine)));
                edge.Inflate(-line, -line);
                cv.DrawRoundRect(edge, cell * 0.12f, cell * 0.12f, Fill(K(C.WoodDark)));
            }

            int slats = Math.Max(2, (int)Math.Round(top.Height / (cell * 0.95f)));
            float sh2 = top.Height / slats;
            float bw = Math.Max(cell * 0.6f, top.Width * 0.09f);
            for (int k = 0; k < slats; k++)
            {
                float start = k * 0.09f;
                float t = Clamp01((open - start) / 0.4f);
                float alpha = 1f - t;
                if (alpha <= 0f)
                {
                    continue;
                }

                var slat = new Box(top.Left - (cell * 0.05f), top.Top + (k * sh2), top.Right + (cell * 0.05f), top.Top + ((k + 1) * sh2));
                float lift = Smooth(t) * cell * 2.4f;
                float dx = (k % 2 == 0 ? -1f : 1f) * Smooth(t) * cell * 1.6f;
                float turn = (k % 2 == 0 ? -1f : 1f) * t * 18f;
                p.PushAlpha(alpha);
                p.PushRotate(turn, slat.CenterX + dx, slat.CenterY - lift);
                Box at = slat.Offset(dx, -lift);
                int seed = (k + cells.Y0) % 5;
                p.Picture("mock.shutter.slat/" + seed, at, (w, h) => UiRaster.Plank(w, h, Math.Min(w, h) * 0.22f, Math.Max(1f, Math.Min(w, h) * 0.06f), WoodTone.Light, seed));
                p.PopTransform();
                p.PopAlpha();
            }

            float sides = 1f - Clamp01((open - 0.5f) / 0.3f);
            if (sides > 0f)
            {
                p.PushAlpha(sides);
                foreach (float x in new[] { top.Left + (bw * 0.75f), top.Right - (bw * 0.75f) })
                {
                    var batten = new Box(x - (bw / 2f), top.Top - (cell * 0.12f), x + (bw / 2f), top.Bottom + (cell * 0.1f));
                    p.PushRotate(90f, batten.CenterX, batten.CenterY);
                    Box turned = Box.FromCenter(batten.CenterX, batten.CenterY, batten.Height, batten.Width);
                    p.Picture("mock.shutter.side", turned, (w, h) => UiRaster.Plank(w, h, Math.Min(w, h) * 0.3f, Math.Max(1f, Math.Min(w, h) * 0.09f), WoodTone.Dark, 2));
                    p.PopTransform();
                    for (int n = 0; n < 2; n++)
                    {
                        float ny = n == 0 ? top.Top + (sh2 * 0.5f) : top.Bottom - (sh2 * 0.5f);
                        cv.DrawCircle(x, ny, Math.Max(1.5f, cell * 0.1f), Fill(K(new Rgba(205, 200, 190), sides)));
                    }
                }

                p.PopAlpha();
            }
        }

        private static void Dust(IPainter p, SKCanvas cv, Box top, float front, float cell, float open)
        {
            var rng = new Random(7);
            for (int i = 0; i < 16; i++)
            {
                float u = (float)rng.NextDouble();
                bool horizontal = i % 2 == 0;
                float x = horizontal ? top.Left + (u * top.Width) : (i % 4 == 1 ? top.Left : top.Right);
                float y = horizontal ? (i % 4 == 0 ? top.Top : top.Bottom + front) : top.Top + (u * (top.Height + front));
                float k = Clamp01((open - (0.04f * (i % 5))) / 0.75f);
                if (k <= 0f || k >= 1f)
                {
                    continue;
                }

                float r = cell * (0.25f + (0.5f * k));
                float dx = (x - top.CenterX) / Math.Max(1f, top.Width) * cell * 2.5f * k;
                float dy = (y - top.CenterY) / Math.Max(1f, top.Height) * cell * 1.8f * k;
                cv.DrawCircle(x + dx, y + dy - (cell * 0.6f * k), r, Fill(K(C.StoneTop.Mix(C.StoneFace, 0.5f), 0.4f * (1f - k))));
            }

            for (int i = 0; i < 6; i++)
            {
                float k = Clamp01((open - (0.05f * i)) / 0.9f);
                if (k <= 0f || k >= 1f)
                {
                    continue;
                }

                double a = (i * Math.PI * 2.0 / 6.0) + 0.5;
                float d = (top.Width * 0.35f) + (cell * 2.5f * k);
                float x = top.CenterX + (float)(Math.Cos(a) * d);
                float y = top.CenterY + ((float)Math.Sin(a) * d * 0.6f) - (cell * 2f * k) + (cell * 3f * k * k);
                float s = cell * 0.9f;
                p.PushAlpha(1f - (k * k));
                p.PushRotate((i * 60f) + (k * 260f), x, y);
                p.Shape("symbol.leaf", Box.FromCenter(x, y, s, s), C.StoneMoss);
                p.PopTransform();
                p.PopAlpha();
            }
        }
    }
}
