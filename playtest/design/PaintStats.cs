using System;
using System.Diagnostics;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// What the painters spent on making pictures, for the preview's frame-time harness (<c>playtest/preview -- --perf</c>)
    /// and the APK's slow-frame log: each <see cref="IPainter.Picture"/> that ran its C# raster (a cache miss) or was read
    /// back from the <see cref="PictureStore"/>, each shape mask and each garden backdrop rendered, with their time and
    /// bytes, and how many draws set up a paint. The counters only grow; a reader takes the difference between two
    /// <see cref="Snapshot"/>s. Engine-free; the UI thread updates them (the preview's painter under its cache lock).
    /// </summary>
    public static class PaintStats
    {
        /// <summary>A copy of the counters at one moment.</summary>
        public readonly struct Counts
        {
            public Counts(long rasters, long rasterTicks, long rasterBytes, long loads, long loadTicks, long masks, long maskTicks, long backdrops, long backdropTicks, long draws)
            {
                Rasters = rasters;
                RasterTicks = rasterTicks;
                RasterBytes = rasterBytes;
                Loads = loads;
                LoadTicks = loadTicks;
                Masks = masks;
                MaskTicks = maskTicks;
                Backdrops = backdrops;
                BackdropTicks = backdropTicks;
                Draws = draws;
            }

            /// <summary>How many pictures ran their raster (<see cref="IPainter.Picture"/> cache misses not in the store).</summary>
            public long Rasters { get; }

            /// <summary>The <see cref="Stopwatch"/> ticks spent in them, the host's bitmap upload included.</summary>
            public long RasterTicks { get; }

            /// <summary>The RGBA bytes they made.</summary>
            public long RasterBytes { get; }

            /// <summary>How many pictures were read back from the <see cref="PictureStore"/> instead of made.</summary>
            public long Loads { get; }

            /// <summary>The ticks spent reading them, the bitmap upload included.</summary>
            public long LoadTicks { get; }

            /// <summary>How many shape masks were rendered.</summary>
            public long Masks { get; }

            public long MaskTicks { get; }

            /// <summary>How many garden backdrops were rendered on the UI thread.</summary>
            public long Backdrops { get; }

            public long BackdropTicks { get; }

            /// <summary>How many draws set up a paint (fills, strokes, text layers, pictures, sprites): on a device each is a few JNI calls.</summary>
            public long Draws { get; }

            /// <summary>The milliseconds spent on getting pictures, masks and backdrops together.</summary>
            public double Milliseconds => Ms(RasterTicks + LoadTicks + MaskTicks + BackdropTicks);

            public double RasterMilliseconds => Ms(RasterTicks);

            public double LoadMilliseconds => Ms(LoadTicks);

            public static Counts operator -(Counts a, Counts b) => new Counts(
                a.Rasters - b.Rasters,
                a.RasterTicks - b.RasterTicks,
                a.RasterBytes - b.RasterBytes,
                a.Loads - b.Loads,
                a.LoadTicks - b.LoadTicks,
                a.Masks - b.Masks,
                a.MaskTicks - b.MaskTicks,
                a.Backdrops - b.Backdrops,
                a.BackdropTicks - b.BackdropTicks,
                a.Draws - b.Draws);
        }

        private static long s_rasters;
        private static long s_rasterTicks;
        private static long s_rasterBytes;
        private static long s_loads;
        private static long s_loadTicks;
        private static long s_masks;
        private static long s_maskTicks;
        private static long s_backdrops;
        private static long s_backdropTicks;
        private static long s_draws;

        /// <summary>
        /// Called for each picture raster with its key, size and ticks (the harness lists the pictures that render again and
        /// again); null on a device.
        /// </summary>
        public static Action<string, int, int, long>? OnRaster { get; set; }

        /// <summary>The counters now.</summary>
        public static Counts Snapshot() => new Counts(s_rasters, s_rasterTicks, s_rasterBytes, s_loads, s_loadTicks, s_masks, s_maskTicks, s_backdrops, s_backdropTicks, s_draws);

        /// <summary>A picture ran its raster: <paramref name="ticks"/> since <paramref name="start"/> (a <see cref="Stopwatch.GetTimestamp"/>).</summary>
        public static void Rastered(string key, int width, int height, long start)
        {
            long ticks = Stopwatch.GetTimestamp() - start;
            s_rasters++;
            s_rasterTicks += ticks;
            s_rasterBytes += (long)width * height * 4;
            OnRaster?.Invoke(key, width, height, ticks);
        }

        /// <summary>A picture was read back from the store since <paramref name="start"/>.</summary>
        public static void Loaded(long start)
        {
            s_loads++;
            s_loadTicks += Stopwatch.GetTimestamp() - start;
        }

        /// <summary>A draw set up its paint.</summary>
        public static void Drew() => s_draws++;

        /// <summary>A label was drawn with a look (<c>TextLook</c>: shadow, extrusion, outline, gradient fill).</summary>
        public static void Looked() => s_looks++;

        /// <summary>How many labels were drawn with a look so far (the APK made a gradient and a blur filter for each until it kept them).</summary>
        public static long Looks => s_looks;

        private static long s_looks;

        /// <summary>A shape mask was rendered since <paramref name="start"/>.</summary>
        public static void Masked(long start)
        {
            s_masks++;
            s_maskTicks += Stopwatch.GetTimestamp() - start;
        }

        /// <summary>A garden backdrop was rendered on the UI thread since <paramref name="start"/>.</summary>
        public static void Backdropped(long start)
        {
            s_backdrops++;
            s_backdropTicks += Stopwatch.GetTimestamp() - start;
        }

        /// <summary>Stopwatch ticks in milliseconds.</summary>
        public static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    }
}
