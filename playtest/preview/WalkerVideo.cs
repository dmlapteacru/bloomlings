using System;
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

        public static int Write(string root, int level, float seconds, string outDir, string lookName, float from)
        {
            WalkerLook? look = LookOf(lookName);
            if (look == null)
            {
                Console.Error.WriteLine("walker video: the look is default, contrast or creature");
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
            WalkerLook.Current = look;
            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                app.LoadLevel(level);
                RoundLevelScreen s = app.Round!;
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
                WalkerLook.Current = before;
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }
    }
}
