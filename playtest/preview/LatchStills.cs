using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;
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

                Parts(Path.Combine(outDir, "latch-parts.png"), app);
                if (Environment.GetEnvironmentVariable("BLOOMLINGS_LATCH_PARTS_ONLY") == "1")
                {
                    return 0;
                }

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

                // the key's turn: a frame every 0.15 s (about a twelfth of a turn)
                var turning = new List<byte[]> { closed };
                for (int k = 0; k < 5; k++)
                {
                    for (int f = 0; f < 4; f++)
                    {
                        Frame(1f / 30f);
                    }

                    turning.Add(painter.Png());
                }

                Strip(turning.ToArray(), new[] { keyArea, columnArea }, Path.Combine(outDir, "latch-closed-closeup.png"), 1.4f);

                // ---- Its opening: the saved solution to the latchOpen event, a frame every 0.13 s of the opening ----
                var proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(roundsDir, "proofs", "L" + one.ToString("0000", CultureInfo.InvariantCulture) + ".json")));
                var before = new List<byte[]>();
                int tick = 0;
                var items = new List<RoundQueueItem>();
                bool InRows()
                {
                    // the closed latch (its hero) shown in its column's rows
                    s.Animator.ColumnItems(latch.Column, RoundScreenLook.PodRows, items);
                    return s.Animator.LatchClosed(0) && items.Exists(item => item.IsLatch && item.Id == 0);
                }

                bool opened = Play(s, proof, Frame, () =>
                {
                    if (s.Animator.LatchOpenSince(0) >= 0f)
                    {
                        return true;
                    }

                    // the closed latch in its column's rows, a frame every 0.2 s (the last three before the opening are kept)
                    if (InRows() && (tick++ % 6) == 0)
                    {
                        before.Add(painter.Png());
                        if (before.Count > 3)
                        {
                            before.RemoveAt(0);
                        }
                    }

                    return false;
                });
                if (!opened)
                {
                    Console.WriteLine("The latch did not open on L" + one);
                    return 1;
                }

                var frames = new List<byte[]>();
                frames.AddRange(before);
                if (before.Count > 0)
                {
                    Save("latch-closed-on-top", before[before.Count - 1]);
                }

                frames.Add(painter.Png());
                for (int k = 0; k < 8; k++)
                {
                    for (int f = 0; f < 3; f++)
                    {
                        Frame(1f / 30f);
                    }

                    frames.Add(painter.Png());
                    if (k == 2)
                    {
                        Save("latch-opening-flight", frames[frames.Count - 1]);
                    }
                    else if (k == 5)
                    {
                        Save("latch-opening-open", frames[frames.Count - 1]);
                    }
                }

                for (int f = 0; f < 15; f++)
                {
                    Frame(1f / 30f);
                }

                frames.Add(painter.Png());
                Save("latch-after", frames[frames.Count - 1]);
                Box top = cache.Regions.Pod(latch.Column, 0);
                var openingArea = new Box(column.Left - (column.Width * 0.4f), top.Top - (column.Width * 0.6f), column.Right + (column.Width * 0.6f), cache.Regions.Pod(latch.Column, 1).Bottom + (column.Width * 0.1f));
                Strip(frames.ToArray(), new[] { openingArea }, Path.Combine(outDir, "latch-opening-pod-strip.png"), 1.1f);
                var whole = new Box(cache.Board.Surface.Left, Math.Min(keyArea.Top, cache.Board.Surface.Top), cache.Board.Surface.Right, cache.Regions.Pod(0, 2).Bottom);
                Strip(frames.ToArray(), new[] { whole }, Path.Combine(outDir, "latch-opening-flight-strip.png"), 0.32f);

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

        // The pieces large on the board's surface: the hero holding padlocks of three colours, a hidden pod's brass one, one
        // with pips, one in a waiting row and one in an outfit; the opening's frames with the key flying in from the right; and
        // the upright key turning (a frame every eighth of a turn) over a few cubes, in a few colours.
        private static void Parts(string into, DesignApp app)
        {
            using var painter = new SkiaPainter(1500, 1700, new Insets(0, 0));
            painter.BeginFrame();
            Kit.BoardSurface(painter, new Box(0f, 0f, 1500f, 1700f), 0f);
            float pod = 170f;
            Outfit? stock = null;
            CosmeticCatalog catalog = app.Meta.Wardrobe.Catalog;
            catalog.TryGet(TwigV2Art.BucketId, out CosmeticItem? hat);
            catalog.TryGet(TwigV2Art.JacketId, out CosmeticItem? jacket);
            catalog.TryGet(TwigV2Art.ScarfId, out CosmeticItem? scarf);
            var dressed = new Outfit(null, hat, null, null, Scarf: scarf, Clothes: jacket);

            (int Material, int Pips, bool Waiting, bool Dressed)[] heroes = { (2, 0, false, false), (5, 0, false, false), (0, 2, false, false), (-1, 0, false, false), (21, 0, true, false), (8, 0, false, true) };
            for (int i = 0; i < heroes.Length; i++)
            {
                var place = new Box(40f + (i * 240f), 110f, 40f + (i * 240f) + pod, 110f + pod);
                painter.StrokeRound(place, 12f, 2f, Rgba.White.WithAlpha(0.5f));
                (int m, int pips, bool waiting, bool dressedUp) = heroes[i];
                Kit.LatchHero(painter, place, dressedUp ? dressed : stock, RoundLook.KeyMetal(m), pips, -1f, 1, waiting);
            }

            // the opening at 0, 0.16, 0.32, 0.48, 0.56, 0.62, 0.72, 0.85 s; the pod behind the hero from the event on
            float[] times = { 0f, 0.16f, 0.32f, 0.48f, 0.56f, 0.62f, 0.72f, 0.85f };
            for (int i = 0; i < times.Length; i++)
            {
                var place = new Box(30f + (i * 180f), 560f, 30f + (i * 180f) + 130f, 560f + 130f);
                Kit.RoundPod(painter, PodChip.Cube(place), RoundMaterial.All[2], 28, PodLook.Exposed, false, false);
                Kit.LatchHero(painter, place, stock, RoundLook.KeyMetal(2), 0, times[i], 1, false);
                var keyCell = new Box(place.Left + 60f, 900f, place.Left + 100f, 935f);
                Kit.FlyingKey(painter, RoundLook.KeyBox(keyCell), RoundLook.HeroLock(RoundLook.HeroCell(place)), RoundLook.KeyMetal(2), 0, times[i], 0f);
            }

            float cell = 52f;
            int[] keys = { 2, 5, -1 };
            for (int row = 0; row < keys.Length; row++)
            {
                float y = 1180f + (row * 175f);
                for (int i = 0; i < 8; i++)
                {
                    float x = 30f + (i * 182f);
                    for (int c = 0; c < 3; c++)
                    {
                        if (c == 1)
                        {
                            continue;
                        }

                        var top = new Box(x + (c * cell), y, x + ((c + 1) * cell), y + (cell * RoundBoardLayout.RowShare));
                        Kit.BoardCube(painter, new Box(top.Left, top.Top, top.Right, top.Bottom + (cell * RoundBoardLayout.FrontShare)), RoundMaterial.All[((c + row) * 7) % 24], false);
                    }

                    var keyCell = new Box(x + cell, y, x + (2 * cell), y + (cell * RoundBoardLayout.RowShare));
                    (float sx, bool back) = RoundLook.KeySpin(i / 8f);
                    painter.Picture(UiRaster.SoftOvalKey, RoundLook.KeyShadow(keyCell), (w, h) => UiRaster.SoftOval(w, h));
                    Kit.UprightKey(painter, RoundLook.KeyBox(keyCell), RoundLook.KeyMetal(keys[row]), row == 1 ? 1 : 0, sx, back);
                }
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
