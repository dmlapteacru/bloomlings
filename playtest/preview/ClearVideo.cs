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
    /// <c>--clear-video &lt;level&gt; &lt;seconds&gt; &lt;out dir&gt; [style]</c>: a round level played at ×1 by a quick player
    /// (the saved solution's commands, one every <see cref="TapEvery"/> seconds while the rules take them), every frame
    /// at 30 fps written as <c>frame-0000.png</c> with <c>board.txt</c> (the board's box in pixels: left top width
    /// height), for a video of the clearing style's walkers (ffmpeg crops and encodes). Review material only.
    /// </summary>
    public static class ClearVideo
    {
        private const float Fps = 30f;

        private const float TapEvery = 0.7f;

        public static int Write(string root, int level, float seconds, string outDir, string? styleId)
        {
            string folder = Path.Combine(root, "content/game/rounds/published");
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            RoundProof proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(root, "content/game/rounds/proofs", "L" + level.ToString("0000", CultureInfo.InvariantCulture) + ".json")));
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-clear-video-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);
            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                ClearStyle? style = ClearStyles.Parse(styleId);
                app.LoadLevel(level);
                RoundLevelScreen s = app.Round!;
                if (style.HasValue)
                {
                    s.Style = style.Value;
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

                Frame(1f / Fps);
                RoundBoardLayout board = RoundPainter.Layout(painter, s, true, false).Board;
                Box outer = board.Outer;
                File.WriteAllText(Path.Combine(outDir, "board.txt"), string.Format(CultureInfo.InvariantCulture, "{0:0} {1:0} {2:0} {3:0}\n", outer.Left, outer.Top, outer.Width, outer.Height));
                float nextTap = 0f;
                int frames = (int)(seconds * Fps);
                for (int f = 0; f < frames; f++)
                {
                    float t = f / Fps;
                    if (t >= nextTap && s.Play.Inputs.Count < proof.Solution.Commands.Count)
                    {
                        RoundCommand c = proof.Solution.Commands[s.Play.Inputs.Count];
                        int before = s.Play.Inputs.Count;
                        if (c.Type == RoundCommandType.Tap)
                        {
                            s.Tap(c.Index);
                        }
                        else
                        {
                            s.BuildBridge(c.Index);
                        }

                        nextTap = s.Play.Inputs.Count > before ? t + TapEvery : t + (1f / Fps);
                    }

                    Frame(1f / Fps);
                    File.WriteAllBytes(Path.Combine(outDir, "frame-" + f.ToString("0000", CultureInfo.InvariantCulture) + ".png"), painter.Png());
                }

                Console.WriteLine($"clear video: L{level}, {frames} frames at {Fps} fps, {s.Play.Inputs.Count} taps, board {outer.Width:0}×{outer.Height:0} px, cell {board.Cell:0.0} px → {outDir}");
                app.Dispose();
                return 0;
            }
            finally
            {
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }
    }
}
