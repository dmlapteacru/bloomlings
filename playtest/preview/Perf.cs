using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;
using Bloomlings.Playtest.Design;
using Bloomlings.Solver;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// The frame-time harness (<c>dotnet run --project playtest/preview -- --perf</c>): it plays one journey through the
    /// real screens as a player would, frame by frame at 60 frames a second on one phone shape, with the painter's picture
    /// cache bounded and quantized as the APK's (<see cref="PainterBase.PictureSize"/>, the same budget and the same
    /// <see cref="PictureCache{T}"/>), and reports per screen: the milliseconds a frame took, how many pictures ran their
    /// C# raster and how long that took (<see cref="PaintStats"/>), the cache's misses, drops and bytes, and the managed
    /// bytes a frame allocated. The first frame of a screen (its opening) is reported apart from the frames after it,
    /// whose rasters are the ones that make an animation stutter. Then it lists the pictures that rendered most often.
    /// The raster numbers are the device's work (the same code); by default the draws are recorded on a canvas that draws
    /// nothing, as a phone's UI thread hands them to its GPU (<see cref="Draw"/>). Run it with
    /// <c>DOTNET_TieredCompilation=0</c>: the APK's Mono compiles each method optimized at once, while .NET's first,
    /// unoptimized tier would make every first picture look slower. The APK's Mono runs these rasters about 4 to 6 times
    /// slower than .NET on the same machine; the harness's sources also build for Mono (<c>UseMonoRuntime</c>, net8.0,
    /// linux-x64) for numbers nearer a phone's.
    /// </summary>
    public static class Perf
    {
        private const float Frame = 1f / 60f;

        /// <summary>
        /// Whether the frames are really drawn on this machine's CPU (<c>--perf-draw</c>); by default the painter records
        /// the draws on a canvas that draws nothing, as a phone's UI thread hands them to its GPU, so a frame's time is the
        /// screens' C# work, the pictures' rasters and the draws' set-up.
        /// </summary>
        public static bool Draw { get; set; }

        /// <summary>
        /// Where the made pictures are kept between runs (<c>--perf-store &lt;folder&gt;</c>, the APK's <see cref="PictureStore"/>):
        /// the first run fills it, a second run with the same folder shows a later launch's frames. Null: pictures are only made.
        /// </summary>
        public static string? StoreFolder { get; set; }

        /// <summary>Whether the managed allocations are sampled by type and listed (<c>--perf-allocs</c>, the runtime's allocation ticks).</summary>
        public static bool Allocations { get; set; }

        /// <summary>Samples the runtime's allocation events (one about every 100 KB) by the type allocated.</summary>
        private sealed class AllocationSampler : System.Diagnostics.Tracing.EventListener
        {
            public Dictionary<string, long> Bytes { get; } = new Dictionary<string, long>(StringComparer.Ordinal);

            public bool On { get; set; }

            protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source)
            {
                if (source.Name == "Microsoft-Windows-DotNETRuntime")
                {
                    // The GC keyword at verbose level carries GCAllocationTick.
                    EnableEvents(source, System.Diagnostics.Tracing.EventLevel.Verbose, (System.Diagnostics.Tracing.EventKeywords)0x1);
                }
            }

            protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs e)
            {
                if (!On || e.EventName == null || !e.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal) || e.Payload == null)
                {
                    return;
                }

                int typeAt = e.PayloadNames?.IndexOf("TypeName") ?? -1;
                int amountAt = e.PayloadNames?.IndexOf("AllocationAmount64") ?? -1;
                string type = typeAt >= 0 ? e.Payload[typeAt]?.ToString() ?? "?" : "?";
                long amount = amountAt >= 0 ? Convert.ToInt64(e.Payload[amountAt], System.Globalization.CultureInfo.InvariantCulture) : 100_000;
                lock (Bytes)
                {
                    Bytes[type] = (Bytes.TryGetValue(type, out long sum) ? sum : 0) + amount;
                }
            }
        }

        /// <summary>Whether every frame is printed as it is drawn (<c>--perf-verbose</c>).</summary>
        public static bool Verbose { get; set; }

        /// <summary>One frame's measures.</summary>
        private readonly struct Sample
        {
            public Sample(double ms, PaintStats.Counts kit, long misses, long evictions, long allocated, long looks)
            {
                Ms = ms;
                Kit = kit;
                Misses = misses;
                Evictions = evictions;
                Allocated = allocated;
                Looks = looks;
            }

            public double Ms { get; }

            public PaintStats.Counts Kit { get; }

            public long Misses { get; }

            public long Evictions { get; }

            public long Allocated { get; }

            /// <summary>Labels drawn with a look (on the APK each made a gradient and a blur filter until they were kept).</summary>
            public long Looks { get; }
        }

        private sealed class Segment
        {
            public Segment(string name) => Name = name;

            public string Name { get; }

            public List<Sample> Frames { get; } = new List<Sample>();

            public long CacheBytes { get; set; }

            public int CacheCount { get; set; }

            public int Gen0 { get; set; }

            public int Gen2 { get; set; }
        }

        private sealed class Journey
        {
            private readonly SkiaPainter _p;
            private readonly DesignApp _app;
            private Segment? _segment;

            public Journey(SkiaPainter p, DesignApp app)
            {
                _p = p;
                _app = app;
            }

            public List<Segment> Segments { get; } = new List<Segment>();

            public List<(string Segment, string Key, int W, int H, long Ticks)> Rasters { get; } = new List<(string, string, int, int, long)>();

            public string Current => _segment?.Name ?? string.Empty;

            /// <summary>Starts a new reported segment: its first frame is the screen's opening.</summary>
            public void Begin(string name)
            {
                End();
                _segment = new Segment(name);
                _gen0 = GC.CollectionCount(0);
                _gen2 = GC.CollectionCount(2);
            }

            private int _gen0;
            private int _gen2;

            public void End()
            {
                if (_segment == null)
                {
                    return;
                }

                var cache = SkiaPainter.PictureCacheStats;
                _segment.CacheBytes = cache.Bytes;
                _segment.CacheCount = cache.Count;
                _segment.Gen0 = GC.CollectionCount(0) - _gen0;
                _segment.Gen2 = GC.CollectionCount(2) - _gen2;
                Segments.Add(_segment);
                _segment = null;
            }

            /// <summary>Draws <paramref name="seconds"/> of frames at 60 a second, each measured.</summary>
            public void Run(float seconds)
            {
                int frames = Math.Max(1, (int)Math.Round(seconds / Frame));
                for (int i = 0; i < frames; i++)
                {
                    Step();
                }
            }

            /// <summary>One measured frame.</summary>
            public void Step()
            {
                var cacheBefore = SkiaPainter.PictureCacheStats;
                PaintStats.Counts before = PaintStats.Snapshot();
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                long looks = PaintStats.Looks;
                long start = Stopwatch.GetTimestamp();
                _p.Now += Frame;
                _p.BeginFrame();
                _app.Draw(_p, Frame);
                double ms = PaintStats.Ms(Stopwatch.GetTimestamp() - start);
                var cacheAfter = SkiaPainter.PictureCacheStats;
                if (Verbose)
                {
                    PaintStats.Counts d = PaintStats.Snapshot() - before;
                    Console.Error.WriteLine($"  frame {_segment?.Frames.Count}: {ms:0.0} ms, {d.Rasters} rasters {d.RasterMilliseconds:0.0} ms, {d.Draws} draws, {d.Masks} masks, {d.Backdrops} backdrops {PaintStats.Ms(d.BackdropTicks):0.0} ms");
                }
                _segment?.Frames.Add(new Sample(
                    ms,
                    PaintStats.Snapshot() - before,
                    cacheAfter.Misses - cacheBefore.Misses,
                    cacheAfter.Evictions - cacheBefore.Evictions,
                    GC.GetAllocatedBytesForCurrentThread() - allocated,
                    PaintStats.Looks - looks));
            }

            /// <summary>Draws frames until the level's animation has shown the rules state (at most 40 s).</summary>
            public void Settle()
            {
                for (int i = 0; i < 2400 && _app.Level != null && !_app.Level.Animator.Settled; i++)
                {
                    Step();
                }
            }

            /// <summary>A finger pressing <paramref name="box"/>'s middle for <paramref name="hold"/> s, lifting (a tap), then the spring-back.</summary>
            public void Press(Box box, float hold = 0.12f, float after = 0.3f)
            {
                _p.TouchDown(box.CenterX, box.CenterY);
                Run(hold);
                _p.TouchUp(box.CenterX, box.CenterY);
                Run(after);
            }

            /// <summary>Plays <paramref name="count"/> commands of a solution as a player would: a tap, then a moment of animation.</summary>
            public void Play(IReadOnlyList<Command> commands, int count, float gap = 0.35f)
            {
                for (int i = 0; i < count && i < commands.Count; i++)
                {
                    LevelScreen level = _app.Level!;
                    switch (commands[i])
                    {
                        case TapPod tap:
                            for (int wait = 0; wait < 600 && level.Animator.FreeOnScreen(level.Session.View) < level.Session.View.ConnectedGroup(tap.PodId).Count; wait++)
                            {
                                Run(0.1f);
                            }

                            level.Tap(tap.PodId);
                            break;
                        case UseExtraSlot:
                            level.UseBooster(BoosterKind.ExtraSlot, commands[i], free: true);
                            break;
                        case UseShuffle:
                            level.UseBooster(BoosterKind.Shuffle, commands[i], free: true);
                            break;
                        case UseReturn:
                            level.UseBooster(BoosterKind.Return, commands[i], free: true);
                            break;
                        case UseBloomBurst:
                            level.UseBooster(BoosterKind.BloomBurst, commands[i], free: true);
                            break;
                    }

                    Run(gap);
                }
            }
        }

        /// <summary>Runs the journey on one phone shape and prints the report; writes it as CSV too when <paramref name="csv"/> is set.</summary>
        public static int Run(ContentSet content, (string Name, float Width, float Height, Insets Insets) shape, string? csv)
        {
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-perf");
            if (Directory.Exists(data))
            {
                Directory.Delete(data, recursive: true);
            }

            Directory.CreateDirectory(data);
            using var p = new SkiaPainter((int)shape.Width, (int)shape.Height, shape.Insets, draw: Draw);
            var app = new DesignApp(data, content, new Silence(), showSplash: false);
            var journey = new Journey(p, app);
            PaintStats.OnRaster = (key, w, h, ticks) => journey.Rasters.Add((journey.Current, key, w, h, ticks));
            if (StoreFolder != null)
            {
                // As the APK keeps its pictures between launches: a second run with the same folder reads them back.
                SkiaPainter.Store = new PictureStore(StoreFolder, "perf", 1024L * 1024 * 1024) { Failed = Console.Error.WriteLine };
            }

            var total = Stopwatch.StartNew();

            // A player at Level 88 with Petals and pictures, on Home with no card open.
            app.Meta.SkipTo(87);
            app.Meta.Economy.Grant(5000 - app.Meta.Economy.Petals, null);
            foreach (int level in new[] { 11, 12, 14, 16, 17, 18 })
            {
                if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null)
                {
                    app.Meta.Collection.Add(definition, level);
                }
            }

            foreach (string id in GuideTour.DemoIds)
            {
                app.Meta.MarkDemoSeen(id);
            }

            app.GoHome();
            while (app.Overlays.Count > 0)
            {
                app.CloseOverlay();
            }

            ReferenceHomeRegions home = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p));
            journey.Begin("Home (open, idle 4 s)");
            journey.Run(4f);
            journey.Begin("Home (promo scenes calling, 9 s)");
            journey.Run(9f);
            journey.Begin("Home: press Settings, card pops");
            journey.Press(home.Settings, after: 0.6f);
            journey.Begin("Settings card (idle 1 s)");
            journey.Run(1f);
            journey.Begin("Settings: close, back on Home");
            app.CloseOverlay();
            journey.Run(1f);
            journey.Begin("Store page (Shop tab)");
            app.OpenStore();
            journey.Run(1f);
            ReferenceStoreRegions store = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets);
            journey.Begin("Store: Cosmetics tab");
            journey.Press(Box.FromCenter(store.Tabs.CenterX, store.Tabs.CenterY, 1f, 1f), after: 1f);
            journey.Begin("Store: Animations tab (live previews)");
            journey.Press(Box.FromCenter(store.Tabs.Right - (store.Tabs.Width / 6f), store.Tabs.CenterY, 1f, 1f), after: 2f);
            journey.Begin("Wardrobe");
            app.CloseStore();
            app.OpenWardrobe();
            journey.Run(1f);
            journey.Begin("Leaderboard");
            app.CloseWardrobe();
            app.OpenLeaderboard();
            journey.Run(1f);
            journey.Begin("Collection");
            app.CloseLeaderboard();
            app.OpenCollection();
            journey.Run(1f);
            journey.Begin("Profile page");
            app.CollectionBack();
            app.OpenProfile();
            journey.Run(1f);
            journey.Begin("Profile: edit card pops");
            app.OpenProfileEdit(Client.Meta.Profile.ProfileTab.Avatar);
            journey.Run(1f);
            app.CloseOverlay();
            app.CloseProfile();
            journey.Begin("Home again");
            journey.Run(1f);

            // A level of the playtest's content played from its solution, its clearing animated as on a device.
            journey.Begin("Home: press Play, level opens");
            home = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p));
            journey.Press(home.Play, after: 0.5f);
            if (app.Level == null)
            {
                app.LoadLevel(88);
            }

            CloseDemo(app);
            LevelScreen played = app.Level!;
            SolveResult win = new Solver.Solver().Solve(Session(content, played.Level), SolveOptions.Default);
            journey.Begin($"Level {played.Level}: taps and clearing");
            using AllocationSampler? sampler = Allocations ? new AllocationSampler() : null;
            if (sampler != null)
            {
                // Only while the level plays: the preview's own hero frames would hide the screens' allocations on Home.
                sampler.On = true;
            }

            journey.Play(win.Trace, Math.Max(1, win.Trace.Count - 1));
            if (sampler != null)
            {
                sampler.On = false;
            }
            journey.Begin($"Level {played.Level}: Pause card pops");
            app.OpenOverlay(Overlay.Pause);
            journey.Run(0.8f);
            app.CloseOverlay();
            journey.Begin($"Level {played.Level}: last tap, win card");
            journey.Play(win.Trace.Skip(win.Trace.Count - 1).ToList(), 1, gap: 0.1f);
            journey.Settle();
            journey.Run(3f);
            journey.Begin("Win: Next, lotus iris to the next level");
            app.Level!.Next();
            journey.Run(2.5f);

            // A big board (the icons look, over 288 cells) and a jam on a level of the content.
            int big = BigLevel(content, app);
            if (big > 0)
            {
                app.LoadLevel(big);
                CloseDemo(app);
                journey.Begin($"Level {big} (big board {Cells(content, app, big)} cells): open");
                journey.Run(0.5f);
                SolveResult bigWin = new Solver.Solver().Solve(Session(content, big), SolveOptions.Default);
                journey.Begin($"Level {big}: taps and clearing");
                journey.Play(bigWin.Trace, Math.Min(8, bigWin.Trace.Count - 1));
            }

            app.LoadLevel(28);
            CloseDemo(app);
            journey.Begin("Level 28: open");
            journey.Run(0.3f);
            SolveResult jam = new Solver.Solver().FindJam(Session(content, app.Level!.Level), SolveOptions.Default);
            journey.Begin("Level 28: play to a jam, the sheet rises");
            journey.Play(jam.Trace, jam.Trace.Count);
            journey.Settle();
            journey.Run(1.5f);
            journey.End();
            PaintStats.OnRaster = null;
            if (SkiaPainter.Store != null && !SkiaPainter.Store.Flush(TimeSpan.FromMinutes(2)))
            {
                Console.Error.WriteLine("perf: the picture store did not finish writing");
            }

            string report = Report(journey, shape.Name, total.Elapsed.TotalSeconds);
            Console.WriteLine(report);
            if (sampler != null)
            {
                Console.WriteLine($"managed allocations while Level {played.Level} played, by type (sampled every ~100 KB):");
                long all = Math.Max(1L, sampler.Bytes.Values.Sum());
                foreach (KeyValuePair<string, long> type in sampler.Bytes.OrderByDescending(t => t.Value).Take(20))
                {
                    Console.WriteLine(string.Format("  {0,-90} {1,8:0.0} MB {2,5:0}%", Trim(type.Key, 90), type.Value / (1024.0 * 1024.0), 100.0 * type.Value / all));
                }
            }
            if (csv != null)
            {
                File.WriteAllText(csv, Csv(journey));
                Console.WriteLine("wrote " + csv);
            }

            return 0;
        }

        private static string Report(Journey journey, string shape, double seconds)
        {
            var b = new StringBuilder();
            b.AppendLine($"perf: {shape}, 60 fps, picture cache {SkiaPainter.PictureCacheBudget / (1024 * 1024)} MiB, {(Draw ? "drawn on the CPU" : "draws recorded, not drawn")} ({seconds:0} s)");
            b.AppendLine("  worst = the segment's slowest frame (the hitch a player sees) and its picture time; pictures = made (rasters) and read");
            b.AppendLine("  back (--perf-store) in the whole segment; then the frames' mean and p95, how many frames after the first made a");
            b.AppendLine("  picture, draws a frame (a few JNI calls each on a device), labels with a look a frame, pictures dropped over budget,");
            b.AppendLine("  cache size, managed KB a frame");
            b.AppendLine();
            b.AppendLine(string.Format("{0,-46} {1,5} | {2,7} {3,7} | {4,5} {5,5} {6,8} | {7,6} {8,6} {9,5} {10,6} {11,6} | {12,5} {13,7} {14,7} {15,6}",
                "segment", "frms", "worst", "pic ms", "made", "read", "pic ms", "mean", "p95", "late", "draw/f", "look/f", "drop", "cacheMB", "allocKB", "gc0/2"));
            foreach (Segment s in journey.Segments)
            {
                Sample worst = s.Frames.OrderByDescending(f => f.Ms).First();
                double[] ms = s.Frames.Select(f => f.Ms).OrderBy(v => v).ToArray();
                b.AppendLine(string.Format(
                    "{0,-46} {1,5} | {2,7:0.0} {3,7:0.0} | {4,5} {5,5} {6,8:0.0} | {7,6:0.0} {8,6:0.0} {9,5} {10,6:0} {11,6:0} | {12,5} {13,7:0.0} {14,7:0} {15,6}",
                    Trim(s.Name, 46),
                    s.Frames.Count,
                    worst.Ms,
                    worst.Kit.Milliseconds,
                    s.Frames.Sum(f => f.Kit.Rasters),
                    s.Frames.Sum(f => f.Kit.Loads),
                    s.Frames.Sum(f => f.Kit.Milliseconds),
                    s.Frames.Average(f => f.Ms),
                    ms[Math.Min(ms.Length - 1, (int)(ms.Length * 0.95))],
                    s.Frames.Skip(1).Count(f => f.Kit.Rasters > 0),
                    s.Frames.Average(f => (double)f.Kit.Draws),
                    s.Frames.Average(f => (double)f.Looks),
                    s.Frames.Sum(f => f.Evictions),
                    s.CacheBytes / (1024.0 * 1024.0),
                    s.Frames.Average(f => f.Allocated / 1024.0),
                    s.Gen0 + "/" + s.Gen2));
            }

            var all = journey.Segments.SelectMany(s => s.Frames).ToList();
            b.AppendLine();
            b.AppendLine($"all frames: {all.Count}, {all.Average(f => f.Ms):0.0} ms mean, pictures made {all.Sum(f => f.Kit.Rasters)} ({all.Sum(f => f.Kit.RasterMilliseconds):0} ms), read back {all.Sum(f => f.Kit.Loads)} ({all.Sum(f => f.Kit.LoadMilliseconds):0} ms), masks {all.Sum(f => f.Kit.Masks)}, backdrops {all.Sum(f => f.Kit.Backdrops)}, on pictures {all.Sum(f => f.Kit.Milliseconds):0} ms, frames over 16.7 ms: {all.Count(f => f.Ms > 1000.0 / 60.0)}, over 50 ms: {all.Count(f => f.Ms > 50.0)}");

            // What a cache holding every picture of the journey would take (the budget that never renders one twice).
            var distinct = journey.Rasters.Select(r => (r.Key, r.W, r.H)).Distinct().ToList();
            b.AppendLine($"distinct pictures: {distinct.Count}, {distinct.Sum(r => (long)r.W * r.H * 4) / (1024.0 * 1024.0):0.0} MiB; rendered again after a drop: {journey.Rasters.Count - distinct.Count}");

            // The costliest segments' heaviest pictures.
            b.AppendLine();
            b.AppendLine("the heaviest pictures of the five segments that spent most on making them:");
            foreach (var segment in journey.Rasters.GroupBy(r => r.Segment).OrderByDescending(g => g.Sum(r => r.Ticks)).Take(5))
            {
                b.AppendLine($"  {segment.Key}: {segment.Count()} pictures, {PaintStats.Ms(segment.Sum(r => r.Ticks)):0} ms");
                foreach (var r in segment.OrderByDescending(r => r.Ticks).Take(6))
                {
                    b.AppendLine(string.Format("    {0,-60} {1,5}x{2,-5} {3,7:0.0} ms", Trim(r.Key, 60), r.W, r.H, PaintStats.Ms(r.Ticks)));
                }
            }

            // The pictures that rendered most: how often, at how many sizes, and their time.
            b.AppendLine();
            b.AppendLine("pictures rendered most often (renders, distinct sizes, same size again, total ms, largest):");
            foreach (var group in journey.Rasters.GroupBy(r => r.Key).OrderByDescending(g => g.Sum(r => r.Ticks)).Take(25))
            {
                int sizes = group.Select(r => (r.W, r.H)).Distinct().Count();
                int again = group.Count() - sizes;
                var largest = group.OrderByDescending(r => (long)r.W * r.H).First();
                b.AppendLine(string.Format("  {0,-62} {1,5} {2,5} {3,5} {4,9:0.0} ms  {5}x{6}", Trim(group.Key, 62), group.Count(), sizes, again, PaintStats.Ms(group.Sum(r => r.Ticks)), largest.W, largest.H));
            }

            return b.ToString();
        }

        private static string Csv(Journey journey)
        {
            var b = new StringBuilder("segment,frame,ms,rasters,raster_ms,masks,kit_ms,misses,evictions,alloc_bytes\n");
            foreach (Segment s in journey.Segments)
            {
                for (int i = 0; i < s.Frames.Count; i++)
                {
                    Sample f = s.Frames[i];
                    b.Append('"').Append(s.Name).Append("\",").Append(i).Append(',')
                        .Append(f.Ms.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                        .Append(f.Kit.Rasters).Append(',')
                        .Append(f.Kit.RasterMilliseconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                        .Append(f.Kit.Masks).Append(',')
                        .Append(f.Kit.Milliseconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                        .Append(f.Misses).Append(',').Append(f.Evictions).Append(',').Append(f.Allocated).Append('\n');
                }
            }

            return b.ToString();
        }

        private static string Trim(string text, int length) => text.Length <= length ? text : text.Substring(0, length - 1) + "…";

        /// <summary>The first level from L11 whose board is drawn in the icons look (over 288 cells), or 0.</summary>
        private static int BigLevel(ContentSet content, DesignApp app)
        {
            for (int level = 11; level < 400; level++)
            {
                if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null && definition.BoardLook == BoardLook.Icons)
                {
                    return level;
                }
            }

            return 0;
        }

        private static int Cells(ContentSet content, DesignApp app, int level)
        {
            LevelSession session = Session(content, app.Resolve(level));
            return session.View.Width * session.View.Height;
        }

        private static LevelSession Session(ContentSet content, int level)
        {
            LevelDefinition definition = content.GetLevel(level);
            return LevelSession.Load(definition, content.GetPicture(definition.Picture), new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget));
        }

        /// <summary>Closes a level's demo card and its guided spotlights.</summary>
        private static void CloseDemo(DesignApp app)
        {
            if (app.Level?.Demo != null)
            {
                app.Level.CloseDemo();
            }

            app.Level?.SkipGuide();
        }
    }
}
