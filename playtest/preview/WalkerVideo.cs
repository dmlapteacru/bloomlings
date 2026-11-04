using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--walker-video &lt;level&gt; &lt;seconds&gt; &lt;out dir&gt; &lt;look&gt; [from seconds]</c>: the walker mock of 2026-11-03 (the owner:
    /// the walkers cannot be seen breaking the cubes on small cubes). A round level played at ×1 by a quick player (the saved
    /// solution's commands, each as soon as the rules take it and at least <see cref="TapEvery"/> apart, so no refusal toast
    /// shows) with the walkers in a <see cref="WalkerLook"/>: <c>default</c> (the game's), <c>contrast</c> (a fixed size on
    /// screen, outline, contact shadow, shade, carrying the cube home) or <c>creature</c> (the same with the Sprig and Drop
    /// families a drawn running creature). Frames at <see cref="Fps"/> from <c>from</c> seconds on as <c>frame-0000.png</c>, with
    /// <c>board.txt</c> (the board surface's box in pixels: left top width height, and the cell). Review material only; the
    /// game's look stays <see cref="WalkerLook.Default"/>.
    /// </summary>
    public static class WalkerVideo
    {
        private const float Fps = 25f;

        private const float TapEvery = 0.7f;

        public static WalkerLook? LookOf(string name) => name switch
        {
            "default" => WalkerLook.Default,
            "contrast" => WalkerLook.Contrast,
            "creature" => WalkerLook.Creature,
            "hands-creature" => WalkerLook.HandsCreature,
            "hands-heroes" => WalkerLook.HandsHeroes,
            _ => null,
        };

        /// <summary>
        /// <c>--walker-sheet &lt;out.png&gt;</c>: the mock's walkers side by side on the board surface over cubes of their colour,
        /// one row a material: the game's walker, the contrast walker, then the drawn creature's run cycle and standing pose
        /// (Sprig and Drop materials). Review material only.
        /// </summary>
        public static int Sheet(string outPng)
        {
            int[] materials = { 0, 10, 7, 9, 2, 4, 1, 3, 5 };
            const int Cell = 150;
            int columns = 2 + WalkerLook.RunFrames + 1;
            using var painter = new SkiaPainter(columns * Cell, materials.Length * Cell, new Insets(0, 0));
            painter.BeginFrame();
            painter.FillRect(new Box(0, 0, columns * Cell, materials.Length * Cell), DesignTokens.Colors.BoardSurface);
            WalkerLook before = WalkerLook.Current;
            try
            {
                for (int row = 0; row < materials.Length; row++)
                {
                    RoundMaterial material = RoundMaterial.All[materials[row]];
                    for (int col = 0; col < columns; col++)
                    {
                        float left = col * Cell;
                        float top = row * Cell;

                        // Cubes of the walker's own colour under its lower half, the worst case for camouflage.
                        float cube = Cell / 6f;
                        for (int cy = 3; cy < 6; cy++)
                        {
                            for (int cx = 0; cx < 6; cx++)
                            {
                                Kit.MaterialTile(painter, new Box(left + (cx * cube), top + (cy * cube), left + ((cx + 1) * cube), top + ((cy + 1) * cube)), material, RoundLook.CubeStyle, false);
                            }
                        }

                        Box box = Box.FromCenter(left + (Cell / 2f), top + (Cell * 0.5f), Cell * 0.8f, Cell * 0.8f);
                        if (col == 0)
                        {
                            WalkerLook.Current = WalkerLook.Default;
                            Visuals.Walker(painter, box, material, -1, 1);
                        }
                        else if (col == 1)
                        {
                            WalkerLook.Current = WalkerLook.Contrast;
                            Visuals.Walker(painter, box, material, -1, 1);
                        }
                        else if (WalkerLook.Creature.DrawsCreature(material.Family))
                        {
                            WalkerLook.Current = WalkerLook.Creature;
                            Visuals.Walker(painter, box, material, col - 2, 1);
                        }
                    }
                }
            }
            finally
            {
                WalkerLook.Current = before;
            }

            File.WriteAllBytes(outPng, painter.Png());
            Console.WriteLine("walker sheet → " + outPng);
            return 0;
        }

        /// <summary>
        /// <c>--walker-strips &lt;out dir&gt;</c>: the "in hands" acts frame by frame, one row a walker, on a patch of the board's cubes
        /// (<c>gags-strip.png</c>: Munchers' four gags with the creature and the heroes; <c>styles-strip.png</c>: the other six
        /// styles; <c>creatures-sheet.png</c>: the four creatures' poses), with <c>*.rows.txt</c> naming each row for the labels.
        /// Review material only.
        /// </summary>
        public static int Strips(string outDir)
        {
            Directory.CreateDirectory(outDir);
            WalkerLook before = WalkerLook.Current;
            try
            {
                int[] materials = { 0, 1, 2, 3 };
                var gags = new List<(string, WalkerLook, ClearStyle, int, int)>();
                string[] gagNames = { "chomp", "squish", "toss", "shake" };
                for (int gag = 0; gag < 4; gag++)
                {
                    gags.Add((gagNames[gag] + " creature", WalkerLook.HandsCreature, ClearStyle.Munchers, gag, materials[gag]));
                    gags.Add((gagNames[gag] + " heroes", WalkerLook.HandsHeroes, ClearStyle.Munchers, gag, materials[gag]));
                }

                StripSheet(Path.Combine(outDir, "gags-strip.png"), gags);
                var styles = new List<(string, WalkerLook, ClearStyle, int, int)>();
                int m = 0;
                foreach (ClearStyle style in ClearStyles.Bought)
                {
                    styles.Add((ClearStyles.Id(style) + " creature", WalkerLook.HandsCreature, style, -1, materials[m % 4]));
                    styles.Add((ClearStyles.Id(style) + " heroes", WalkerLook.HandsHeroes, style, -1, materials[m % 4]));
                    m++;
                }

                StripSheet(Path.Combine(outDir, "styles-strip.png"), styles);
                CreatureSheet(Path.Combine(outDir, "creatures-sheet.png"));

                // The game's walkers of 2026-11-04: Munchers stepping aside to eat, and the Head Bonk with every walker kind.
                var game = new List<(string, WalkerLook, ClearStyle, int, int)>
                {
                    ("aside creatures", WalkerLook.GameCreatures, ClearStyle.Munchers, -1, 0),
                    ("aside heroes", WalkerLook.GameHeroes, ClearStyle.Munchers, -1, 1),
                    ("bonk classic", WalkerLook.Default, ClearStyle.Bonk, -1, 2),
                    ("bonk creatures", WalkerLook.GameCreatures, ClearStyle.Bonk, -1, 3),
                    ("bonk heroes", WalkerLook.GameHeroes, ClearStyle.Bonk, -1, 1),
                };
                StripSheet(Path.Combine(outDir, "game-strip.png"), game);
                StorePreviewStrip(Path.Combine(outDir, "store-bonk.png"));
            }
            finally
            {
                WalkerLook.Use(WalkerKind.Classic, ClearStyles.Default);
                WalkerLook.Current = before;
            }

            return 0;
        }

        // One row a walker: frames of its act from just before its arrival to its way home.
        private static void StripSheet(string outPng, List<(string Label, WalkerLook Look, ClearStyle Style, int Gag, int Material)> rows)
        {
            const int Frames = 12;
            const int FrameW = 190;
            const int RowH = 380;
            const float Cell = 36f;
            const float Walker = 100f;
            float cubesAcross = Walker / Cell / WalkerLook.MockBoardShare;
            using var painter = new SkiaPainter(Frames * FrameW, rows.Count * RowH, new Insets(0, 0));
            painter.BeginFrame();
            painter.FillRect(new Box(0, 0, Frames * FrameW, rows.Count * RowH), DesignTokens.Colors.BoardSurface);
            var labels = new List<string>();
            var list = new FxList();
            for (int r = 0; r < rows.Count; r++)
            {
                (string label, WalkerLook look, ClearStyle style, int gag, int material) = rows[r];
                labels.Add(label);
                WalkerKind? kind = ReferenceEquals(look, WalkerLook.GameCreatures) ? WalkerKind.Creatures
                    : ReferenceEquals(look, WalkerLook.GameHeroes) ? WalkerKind.Heroes
                    : look.IsDefault ? WalkerKind.Classic : (WalkerKind?)null;
                if (kind.HasValue)
                {
                    WalkerLook.Use(kind.Value, style);
                }
                else
                {
                    WalkerLook.Current = look;
                }
                RoundMaterial mat = RoundMaterial.All[material];

                // A target whose cell picks the gag (ClearLook's seed: 7 a column, 13 a row).
                const int Ty = 3;
                int tx = 8;
                while (gag >= 0 && ((tx * 7) + (Ty * 13)) % 4 != gag)
                {
                    tx++;
                }

                var points = new List<(float X, float Y)>();
                for (int i = 6; i >= 0; i--)
                {
                    points.Add((tx - i + 0.5f, Ty + 0.5f));
                }

                ClearWalk walk = ClearWalk.Live(points, material, RoundLook.WalkerVariant(mat.Family), 0f, 0.6f, 0.7f, 1.25f, null, cubesAcross);
                if (style == ClearStyle.Munchers && WalkerLook.Current.AsideMunch)
                {
                    // Its aside spot: a step back and aside off its way (the hosts choose one with AsideSpots).
                    (float sx, float sy, float _, float _) = ClearLook.StopOf(points, Walker / Cell);
                    walk = walk.WithAside((sx - (0.85f * Walker / Cell), sy + (0.2f * Walker / Cell)));
                }
                for (int f = 0; f < Frames; f++)
                {
                    float t = 0.56f + (f * 0.11f);
                    float left = f * FrameW;
                    float top = r * RowH;
                    float share = label.StartsWith("aside", StringComparison.Ordinal) ? 0.97f : style == ClearStyle.Pushers ? 0.75f : 0.45f;
                    float ox = left + (FrameW * share) - ((tx + 0.5f) * Cell);
                    float oy = top + (RowH * 0.72f) - ((Ty + 0.5f) * Cell);
                    painter.PushClip(new Box(left, top, left + FrameW - 2, top + RowH - 2));

                    // A patch of cubes of its colour round the target, the far rows first; the target until the walker takes it.
                    for (int cy = Ty - 2; cy <= Ty + 1; cy++)
                    {
                        for (int cx = tx; cx <= tx + 3; cx++)
                        {
                            if ((cx == tx && cy == Ty && t >= 0.6f) || (cx == tx && cy > Ty))
                            {
                                continue;
                            }

                            BoardCubeLook cubeLook = BoardCubeLook.Default;
                            float x0 = ox + (cx * Cell);
                            float y0 = oy + (cy * Cell);
                            Kit.BoardCube(painter, new Box(x0 - (Cell * cubeLook.Overlap), y0, x0 + Cell + (Cell * cubeLook.Overlap), y0 + (Cell * cubeLook.PictureShare)), mat, false, cubeLook);
                        }
                    }

                    list.Clear();
                    ClearLook.Walker(list, style, walk, t);
                    ClearPainter.DrawItems(painter, list.Items, FxLayer.Board, ox, oy, Cell);
                    ClearPainter.DrawItems(painter, list.Items, FxLayer.Over, ox, oy, Cell);
                    painter.PopClip();
                }
            }

            File.WriteAllBytes(outPng, painter.Png());
            File.WriteAllLines(Path.ChangeExtension(outPng, ".rows.txt"), labels);
            Console.WriteLine("walker strips → " + outPng);
        }

        // The Head Bonk's live Store preview (Kit.ClearingPreview, the catalog's and the confirmation's) over a loop, a frame every 0.2 s.
        private static void StorePreviewStrip(string outPng)
        {
            const int W = 360;
            const int H = 300;
            const int Frames = 10;
            using var painter = new SkiaPainter(W * Frames, H * 2, new Insets(0, 0));
            painter.BeginFrame();
            painter.FillRect(new Box(0, 0, W * Frames, H * 2), DesignTokens.Colors.ParchmentTop);
            for (int row = 0; row < 2; row++)
            {
                for (int f = 0; f < Frames; f++)
                {
                    painter.Now = 1f + (f * 0.2f);
                    Kit.ClearingPreview(painter, new Box((f * W) + 8, (row * H) + 8, ((f + 1) * W) - 8, ((row + 1) * H) - 8), ClearStyle.Bonk, catalog: row == 0);
                }
            }

            File.WriteAllBytes(outPng, painter.Png());
            Console.WriteLine("store preview strip → " + outPng);
        }

        // The four creatures (rows) in their poses: the run's eight frames, standing from the side, facing, holding, the hands.
        private static void CreatureSheet(string outPng)
        {
            const int Cell = 150;
            int columns = WalkerLook.PoseHands + 1;
            int[] materials = { 0, 1, 2, 3, 10, 12, 4, 15 };
            using var painter = new SkiaPainter(columns * Cell, materials.Length * Cell, new Insets(0, 0));
            painter.BeginFrame();
            painter.FillRect(new Box(0, 0, columns * Cell, materials.Length * Cell), DesignTokens.Colors.BoardSurface);
            WalkerLook.Current = WalkerLook.HandsCreature;
            for (int row = 0; row < materials.Length; row++)
            {
                RoundMaterial material = RoundMaterial.All[materials[row]];
                for (int pose = 0; pose <= WalkerLook.PoseHands; pose++)
                {
                    Box box = Box.FromCenter((pose * Cell) + (Cell / 2f), (row * Cell) + (Cell * 0.56f), Cell * 0.62f, Cell * 0.62f);
                    if (pose == WalkerLook.PoseHands)
                    {
                        // The holding pose with the cube and its hands over it, as the act draws it.
                        Visuals.Walker(painter, box, material, WalkerLook.PoseHold, 1);
                        float g = WalkerLook.HeldShare * box.Width;
                        float cy = box.Bottom - (WalkerLook.HeldUp * box.Width);
                        BoardCubeLook look = BoardCubeLook.Default;
                        float top = cy - (g * look.RowShare / 2f);
                        Kit.BoardCube(painter, new Box(box.CenterX - (g / 2f) - (g * look.Overlap), top, box.CenterX + (g / 2f) + (g * look.Overlap), top + (g * look.PictureShare)), material, false, look);
                    }

                    Visuals.Walker(painter, box, material, pose, 1);
                }
            }

            File.WriteAllBytes(outPng, painter.Png());
            Console.WriteLine("creatures sheet → " + outPng);
        }

        public static int Write(string root, int level, float seconds, string outDir, string lookName, float from, string? styleId = null)
        {
            // The game's walkers by their Settings choice (classic, creatures, heroes: the real path), else a mock look.
            WalkerKind? kind = lookName == "classic" || lookName == "creatures" || lookName == "heroes" ? WalkerKinds.Parse(lookName) : (WalkerKind?)null;
            WalkerLook? look = kind.HasValue ? WalkerLook.For(kind.Value) : LookOf(lookName);
            if (look == null)
            {
                Console.Error.WriteLine("walker video: the look is classic, creatures, heroes (the game's), or the mocks default, contrast, creature, hands-creature, hands-heroes");
                return 2;
            }

            string roundsDir = Path.Combine(root, "content/game/rounds");
            string folder = Path.Combine(roundsDir, "published");
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            RoundProof proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(roundsDir, "proofs", "L" + level.ToString("0000", CultureInfo.InvariantCulture) + ".json")));
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-walker-video-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);
            WalkerLook before = WalkerLook.Current;
            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                if (kind.HasValue)
                {
                    app.Meta.Save.Settings.Walkers = WalkerKinds.Id(kind.Value);
                }

                app.LoadLevel(level);
                RoundLevelScreen s = app.Round!;
                if (ClearStyles.Parse(styleId) is ClearStyle style)
                {
                    s.Style = style;
                }

                if (!kind.HasValue)
                {
                    // A mock look over whatever the level chose.
                    WalkerLook.Current = look;
                }

                void Frame(float dt)
                {
                    painter.Now += dt;
                    painter.BeginFrame();
                    app.Draw(painter, dt);
                }

                // The guides: the entry and the forced first tap through the screen's own touch target.
                for (int guard = 0; guard < 10 && s.Guide != null; guard++)
                {
                    Frame(1f / Fps);
                    if (s.Guide.Kind == GuideKind.FirstTap && s.GuidePod >= 0 && s.PodBoxes[s.GuidePod].HasValue)
                    {
                        Box pod = s.PodBoxes[s.GuidePod]!.Value;
                        painter.Dispatch(pod.CenterX, pod.CenterY);
                    }
                    else
                    {
                        s.NextGuideStep();
                    }
                }

                if (s.Intro != null)
                {
                    s.CloseIntro();
                }

                Frame(1f / Fps);
                RoundBoardLayout board = RoundPainter.Layout(painter, s, true, false).Board;
                Box surface = board.Surface;
                File.WriteAllText(Path.Combine(outDir, "board.txt"), string.Format(CultureInfo.InvariantCulture, "{0:0} {1:0} {2:0} {3:0} {4:0.00}\n", surface.Left, surface.Top, surface.Width, surface.Height, board.Cell));
                float nextTap = 0f;
                int frames = (int)(seconds * Fps);
                int first = (int)(from * Fps);
                for (int f = 0; f < frames; f++)
                {
                    float t = f / Fps;
                    if (t >= nextTap && s.Play.Inputs.Count < proof.Solution.Commands.Count)
                    {
                        RoundCommand c = proof.Solution.Commands[s.Play.Inputs.Count];
                        int count = s.Play.Inputs.Count;
                        if (c.Type == RoundCommandType.Tap)
                        {
                            if (s.Play.Check(RoundAction.Tap(c.Index)) == RoundRefusal.None)
                            {
                                s.Tap(c.Index);
                            }
                        }
                        else
                        {
                            s.BuildBridge(c.Index);
                        }

                        nextTap = s.Play.Inputs.Count > count ? t + TapEvery : t + (1f / Fps);
                    }

                    Frame(1f / Fps);
                    if (f >= first)
                    {
                        File.WriteAllBytes(Path.Combine(outDir, "frame-" + (f - first).ToString("0000", CultureInfo.InvariantCulture) + ".png"), painter.Png());
                    }
                }

                Console.WriteLine($"walker video: L{level} in the {look.Name} look, {frames - first} frames at {Fps} fps, {s.Play.Inputs.Count} taps, surface {surface.Width:0}×{surface.Height:0} px, cell {board.Cell:0.0} px → {outDir}");
                app.Dispose();
                return 0;
            }
            finally
            {
                WalkerLook.Use(WalkerKind.Classic, ClearStyles.Default);
                WalkerLook.Current = before;
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }
    }
}
