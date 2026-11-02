using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--latch-stills &lt;rounds dir&gt; &lt;out dir&gt;</c>: the latch's look for the owner's review (spec 006 FR-068, the
    /// owner's look of 2026-11-01) on a rounds folder with <c>published/</c> and <c>proofs/</c> (a temporary set, so the game's
    /// <c>content/game/rounds</c> is never touched): the first level with one latch and an introduction card writes the card,
    /// the closed lock with its key and a close-up of both, then plays its saved solution to the latch's opening and writes
    /// a strip of it (the column and the key); the first level with two latches writes its card and the two locks. Review
    /// material only.
    /// </summary>
    public static class LatchStills
    {
        public static int Write(string roundsDir, string outDir)
        {
            string folder = Path.Combine(roundsDir, "published");
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-latch-stills-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);
            Parts(Path.Combine(outDir, "latch-parts.png"));
            if (Environment.GetEnvironmentVariable("BLOOMLINGS_LATCH_PARTS_ONLY") == "1")
            {
                return 0;
            }

            int one = 0;
            int two = 0;
            for (int n = 1; n <= rounds.LevelCount; n++)
            {
                int latches = rounds.GetLevel(n).Latches.Count;
                one = one == 0 && latches == 1 ? n : one;
                two = two == 0 && latches >= 2 ? n : two;
            }

            if (one == 0)
            {
                Console.WriteLine("No level with a latch in " + folder);
                return 1;
            }

            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                void Frame(float dt)
                {
                    painter.Now += dt;
                    painter.BeginFrame();
                    app.Draw(painter, dt);
                }

                void Save(string name, byte[] png) => File.WriteAllBytes(Path.Combine(outDir, name + ".png"), png);

                RoundLevelScreen Open(int n)
                {
                    app.LoadLevel(n);
                    RoundLevelScreen screen = app.Round!;
                    screen.SkipGuide();
                    return screen;
                }

                // ---- One latch: the card, the closed lock and its key ----
                RoundLevelScreen s = Open(one);
                for (int i = 0; i < 30; i++)
                {
                    Frame(1f / 30f);
                }

                if (s.Intro != null)
                {
                    Save("latch-intro-card", painter.Png());
                    s.CloseIntro();
                }

                for (int i = 0; i < 30; i++)
                {
                    Frame(1f / 30f);
                }

                byte[] closed = painter.Png();
                Save("latch-closed-with-key", closed);
                RoundPainter.LayoutCache cache = RoundPainter.Layout(painter, s, true, false);
                RoundLatch latch = s.Round.Latches[0];
                Box keyCell = cache.Board.CellBox(latch.Key);
                Box keyArea = RoundLook.KeyBox(keyCell, cache.Board.Surface).Inset(-keyCell.Width * 1.2f);
                Box column = cache.Regions.Columns[latch.Column];
                Box columnArea = new Box(column.Left - (column.Width * 0.3f), cache.Regions.Pod(latch.Column, 0).Top - (column.Width * 0.45f), column.Right + (column.Width * 0.3f), cache.Regions.Pod(latch.Column, 2).Bottom + (column.Width * 0.1f));
                Strip(new[] { closed }, new[] { keyArea, columnArea }, Path.Combine(outDir, "latch-closed-closeup.png"), 1.6f);

                // ---- Its opening: the saved solution to the latchOpen event, a frame every 0.13 s of the opening ----
                var proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(roundsDir, "proofs", "L" + one.ToString("0000", CultureInfo.InvariantCulture) + ".json")));
                byte[]? before = null;
                int tick = 0;
                bool Top() => s.Animator.LatchClosed(0) && s.Play.Timeline.State.Heads[latch.Column] == latch.Depth;
                bool opened = Play(s, proof, Frame, () =>
                {
                    if (s.Animator.LatchOpenSince(0) >= 0f)
                    {
                        return true;
                    }

                    // the closed lock on top of its column, a frame every 0.2 s (the last one before the opening is kept)
                    if (Top() && (tick++ % 6) == 0)
                    {
                        using SKImage image = painter.Snapshot();
                        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 80);
                        before = encoded.ToArray();
                    }

                    return false;
                });
                if (!opened)
                {
                    Console.WriteLine("The latch did not open on L" + one);
                    return 1;
                }

                var frames = new List<byte[]>();
                if (before != null)
                {
                    frames.Add(before);
                }

                frames.Add(painter.Png());
                for (int k = 0; k < 4; k++)
                {
                    for (int f = 0; f < 4; f++)
                    {
                        Frame(1f / 30f);
                    }

                    frames.Add(painter.Png());
                }

                Save("latch-opening", frames[Math.Min(2, frames.Count - 1)]);
                Box top = cache.Regions.Pod(latch.Column, 0);
                var openingArea = new Box(column.Left - (column.Width * 0.3f), top.Top - (column.Width * 0.5f), column.Right + (column.Width * 0.45f), cache.Regions.Pod(latch.Column, 1).Bottom + (column.Width * 0.1f));
                Strip(frames.ToArray(), new[] { openingArea }, Path.Combine(outDir, "latch-opening-pod-strip.png"), 1.4f);
                Strip(frames.ToArray(), new[] { keyArea }, Path.Combine(outDir, "latch-opening-key-strip.png"), 1.4f);

                // ---- Two latches on one level ----
                if (two > 0)
                {
                    s = Open(two);
                    for (int i = 0; i < 30; i++)
                    {
                        Frame(1f / 30f);
                    }

                    if (s.Intro != null)
                    {
                        Save("latch-reminder-card", painter.Png());
                        s.CloseIntro();
                    }

                    for (int i = 0; i < 30; i++)
                    {
                        Frame(1f / 30f);
                    }

                    Save("two-latches", painter.Png());

                    // later, both locks in view (each within the three rows its column shows), if the solution gets there
                    var proof2 = RoundJson.ReadProof(File.ReadAllText(Path.Combine(roundsDir, "proofs", "L" + two.ToString("0000", CultureInfo.InvariantCulture) + ".json")));
                    RoundLevelScreen screen = s;
                    bool InView()
                    {
                        for (int i = 0; i < screen.Round.Latches.Count; i++)
                        {
                            RoundLatch l = screen.Round.Latches[i];
                            if (!screen.Animator.LatchClosed(i) || l.Depth - screen.Play.Timeline.State.Heads[l.Column] >= RoundScreenLook.PodRows)
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    if (Play(s, proof2, Frame, InView))
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Frame(1f / 30f);
                        }

                        Save("two-latches-in-view", painter.Png());
                    }
                }

                Console.WriteLine("Latch stills (L" + one + (two > 0 ? ", L" + two : string.Empty) + ") in " + outDir);
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

        // The pieces large on the board's surface: locked pods (three colours, a hidden one, pips), the opening's frames and the
        // keys in several colours (one with pips, a hidden pod's brass one) over a few cubes.
        private static void Parts(string into)
        {
            using var painter = new SkiaPainter(1400, 1100, new Insets(0, 0));
            painter.BeginFrame();
            Kit.BoardSurface(painter, new Box(0f, 0f, 1400f, 1100f), 0f);
            float pod = 190f;
            int[] materials = { 2, 5, 0, -1 };
            for (int i = 0; i < materials.Length; i++)
            {
                var place = new Box(60f + (i * 330f), 90f, 60f + (i * 330f) + pod, 90f + pod);
                PodChip chip = PodChip.Cube(place);
                RoundMaterial? m = materials[i] >= 0 ? RoundMaterial.All[materials[i]] : null;
                Kit.LockedPod(painter, chip, m, 28, false);
                Kit.LatchOverlay(painter, chip.Frame, i == 2 ? 2 : 0, -1f);
            }

            // the opening at 0, 0.1, 0.2, 0.3, 0.42 s
            float[] times = { 0.0f, 0.1f, 0.2f, 0.3f, 0.42f };
            for (int i = 0; i < times.Length; i++)
            {
                var place = new Box(40f + (i * 270f), 430f, 40f + (i * 270f) + 150f, 430f + 150f);
                PodChip chip = PodChip.Cube(place);
                Kit.RoundPod(painter, chip, RoundMaterial.All[2], 28, PodLook.Exposed, false, false);
                Kit.LatchOverlay(painter, chip.Frame, 0, times[i]);
            }

            float cell = 56f;
            int[] keys = { 2, 5, 0, 15, 21, -1 };
            for (int i = 0; i < keys.Length; i++)
            {
                float x = 40f + ((i % 3) * 460f);
                float y = 700f + ((i / 3) * 200f);
                for (int c = 0; c < 6; c++)
                {
                    if (c == 2)
                    {
                        continue;
                    }

                    var top = new Box(x + (c * cell), y, x + ((c + 1) * cell), y + (cell * RoundBoardLayout.RowShare));
                    Kit.BoardCube(painter, new Box(top.Left, top.Top, top.Right, top.Bottom + (cell * RoundBoardLayout.FrontShare)), RoundMaterial.All[(c * 7) % 24], false);
                }

                var keyCell = new Box(x + (2 * cell), y, x + (3 * cell), y + (cell * RoundBoardLayout.RowShare));
                Kit.RoundKey(painter, keyCell, RoundLook.KeyMetal(keys[i]), i == 1 ? 1 : 0);
            }

            File.WriteAllBytes(into, painter.Png());
        }

        // The saved solution as a patient player (each command once the work settled), until done() holds after a frame.
        internal static bool Play(RoundLevelScreen s, RoundProof proof, Action<float> frame, Func<bool> done)
        {
            IReadOnlyList<RoundCommand> commands = proof.Solution.Commands;
            for (int c = 0; c <= commands.Count; c++)
            {
                for (float t = 0f; t < 60f; t += 1f / 30f)
                {
                    frame(1f / 30f);
                    if (done())
                    {
                        return true;
                    }

                    if (!s.Play.Timeline.Pending)
                    {
                        break;
                    }
                }

                if (c == commands.Count)
                {
                    break;
                }

                RoundCommand command = commands[c];
                if (command.Type == RoundCommandType.Tap) s.Tap(command.Index); else s.BuildBridge(command.Index);
            }

            return false;
        }

        // Crops each area out of each frame and lays them out: a row per area, a column per frame, scaled by zoom.
        internal static void Strip(byte[][] frames, Box[] areas, string into, float zoom)
        {
            var bitmaps = new List<SKBitmap>();
            try
            {
                foreach (byte[] png in frames)
                {
                    bitmaps.Add(SKBitmap.Decode(png));
                }

                int gap = 16;
                int width = 0;
                int height = 0;
                foreach (Box area in areas)
                {
                    width = Math.Max(width, (int)((area.Width * zoom * frames.Length) + (gap * (frames.Length + 1))));
                    height += (int)(area.Height * zoom) + gap;
                }

                using var strip = new SKBitmap(width, height + gap);
                using (var canvas = new SKCanvas(strip))
                {
                    canvas.Clear(SKColors.White);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    float y = gap;
                    foreach (Box area in areas)
                    {
                        float x = gap;
                        foreach (SKBitmap bitmap in bitmaps)
                        {
                            var source = new SKRect(area.Left, area.Top, area.Right, area.Bottom);
                            var target = new SKRect(x, y, x + (area.Width * zoom), y + (area.Height * zoom));
                            canvas.DrawBitmap(bitmap, source, target, paint);
                            x += (area.Width * zoom) + gap;
                        }

                        y += (area.Height * zoom) + gap;
                    }
                }

                using SKData data = strip.Encode(SKEncodedImageFormat.Png, 90);
                File.WriteAllBytes(into, data.ToArray());
            }
            finally
            {
                foreach (SKBitmap bitmap in bitmaps)
                {
                    bitmap.Dispose();
                }
            }
        }
    }
}
