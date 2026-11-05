using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>One slat of a shutter's crate lid as drawn (<see cref="ShutterLook.Slat"/>): its box, its turn and its alpha.</summary>
    public readonly struct ShutterSlat
    {
        public ShutterSlat(Box box, float degrees, float alpha, int seed)
        {
            Box = box;
            Degrees = degrees;
            Alpha = alpha;
            Seed = seed;
        }

        public Box Box { get; }

        /// <summary>Its turn round its middle, degrees clockwise on screen.</summary>
        public float Degrees { get; }

        public float Alpha { get; }

        /// <summary>Its plank's grain (<see cref="UiRaster.Plank"/>'s seed, 0 to 4).</summary>
        public int Seed { get; }
    }

    /// <summary>A puff of dust or a leaf as a shutter opens (<see cref="ShutterLook.Dust"/>, <see cref="ShutterLook.Leaves"/>).</summary>
    public readonly struct ShutterBit
    {
        public ShutterBit(Box box, float degrees, float alpha)
        {
            Box = box;
            Degrees = degrees;
            Alpha = alpha;
        }

        public Box Box { get; }

        public float Degrees { get; }

        public float Alpha { get; }
    }

    /// <summary>
    /// A Twig wooden shutter's look on a round board (rules 17 §8 «Twig Деревянные створки», spec 006 FR-070; the owner's look B
    /// of 2026-11-05, after <c>playtest/preview/ChannelShutterMock.cs</c>): a crate lid over the shutter's rectangle, level honey
    /// slats (the signs' plank) between two dark side battens, raised over the cubes on its darker front edge with a soft
    /// shadow; hosts draw it after the cubes of its bottom row (<see cref="BottomRow"/>), so the nearer rows' cubes stand in
    /// front of its edge as the board's fixed viewpoint orders them. On it a tag hangs from a nail on two strings: the pods'
    /// own cube in the counted material with the count still to go (<see cref="RoundAnimator.ShutterLeft"/>), swaying a little
    /// and popping at each counted cube, while a spark of the material flies from each counted cube to it. When the count is
    /// reached (<c>shutterOpen</c>) the slats pop off one after another from the top with dust and leaves
    /// (<see cref="OpenSeconds"/>); the cubes under it are then ordinary work. Screen pixels; engine-free; presentation only.
    /// </summary>
    public sealed class ShutterLook
    {
        /// <summary>The asset slot of the shutters' pictures.</summary>
        public const string Slot = "board.shutter";

        /// <summary>How long the opening plays (seconds; <see cref="RoundAnimator.ShutterOpenUs"/>).</summary>
        public const float OpenSeconds = RoundAnimator.ShutterOpenUs / 1_000_000f;

        /// <summary>How far the lid stands over the cubes, in cells (its top drawn that much higher, its edge that much taller).</summary>
        public const float LidCells = 0.14f;

        /// <summary>How long a counted cube's spark flies to the tag (seconds).</summary>
        public const float SparkSeconds = 0.45f;

        /// <summary>How long the tag pops at a counted cube (seconds).</summary>
        public const float PopSeconds = 0.25f;

        /// <summary>The tag at the least, in pixels of a screen 1080 wide (it reads on the smallest cubes, ~22 dp).</summary>
        public const float TagMinPixels = 58f;

        private readonly RoundBoardLayout _board;
        private readonly float _tagMin;

        private ShutterLook(RoundBoardLayout board, RoundShutter shutter, int index, float tagMin)
        {
            _board = board;
            Shutter = shutter;
            Index = index;
            _tagMin = tagMin;
            int w = board.Width;
            int x0 = int.MaxValue;
            int x1 = int.MinValue;
            int y0 = int.MaxValue;
            int y1 = int.MinValue;
            foreach (int cell in shutter.Cells)
            {
                x0 = Math.Min(x0, cell % w);
                x1 = Math.Max(x1, cell % w);
                y0 = Math.Min(y0, cell / w);
                y1 = Math.Max(y1, cell / w);
            }

            BottomRow = y0;
            TopRow = y1;
            Columns = (x0, x1);
            Material = RoundMaterial.Of(shutter.Code);
            float lid = board.Cell * LidCells;
            Box far = board.CellBox(x0, y1);
            Box near = board.CellBox(x1, y0);
            Top = new Box(far.Left, far.Top - lid, near.Right, near.Bottom - lid);
            FrontPixels = board.Front + lid;
        }

        /// <summary>The look of each of <paramref name="level"/>'s shutters on <paramref name="board"/> (the tag at least <paramref name="tagMin"/> pixels).</summary>
        public static List<ShutterLook> For(RoundBoardLayout board, RoundLevel level, float tagMin)
        {
            var list = new List<ShutterLook>();
            for (int i = 0; i < level.Shutters.Count; i++)
            {
                list.Add(new ShutterLook(board, level.Shutters[i], i, tagMin));
            }

            return list;
        }

        /// <summary>The look of one shutter (<paramref name="index"/> of its level's) on <paramref name="board"/>.</summary>
        public static ShutterLook Of(RoundBoardLayout board, RoundShutter shutter, int index, float tagMin) => new ShutterLook(board, shutter, index, tagMin);

        /// <summary>The tag's least size on a screen <paramref name="screenWidth"/> wide (<see cref="TagMinPixels"/> scaled).</summary>
        public static float TagMin(float screenWidth) => TagMinPixels * screenWidth / RoundScreenLook.ReferenceWidth;

        public RoundShutter Shutter { get; }

        /// <summary>Its index in the level's shutters (the events').</summary>
        public int Index { get; }

        /// <summary>The material it counts.</summary>
        public RoundMaterial Material { get; }

        /// <summary>Its rows (row 0 at the bottom): its nearest one, after whose cubes hosts draw the lid.</summary>
        public int BottomRow { get; }

        public int TopRow { get; }

        public (int First, int Last) Columns { get; }

        /// <summary>The lid's top face on screen, raised <see cref="LidCells"/> over the cubes.</summary>
        public Box Top { get; }

        /// <summary>The lid's front edge under <see cref="Top"/>, in pixels.</summary>
        public float FrontPixels { get; }

        /// <summary>Whether the cell lies under this shutter.</summary>
        public bool Covers(int cell)
        {
            int x = cell % _board.Width;
            int y = cell / _board.Width;
            return x >= Columns.First && x <= Columns.Last && y >= BottomRow && y <= TopRow;
        }

        /// <summary>
        /// The lid's base picture (its soft shadow and its dark front edge, <see cref="UiRaster.ShutterBase"/>): its screen box,
        /// cache key and renderer.
        /// </summary>
        public (Box Box, string Key, Func<int, int, byte[]> Render) Base()
        {
            float cell = _board.Cell;
            float wc = Top.Width / cell;
            float hc = Top.Height / cell;
            float fc = FrontPixels / cell;
            var box = new Box(Top.Left - (0.3f * cell), Top.Top - (0.3f * cell), Top.Right + (0.6f * cell), Top.Bottom + FrontPixels + (0.7f * cell));
            string key = Slot + "/base/" + wc.ToString("0.00", CultureInfo.InvariantCulture) + "x" + hc.ToString("0.00", CultureInfo.InvariantCulture) + "/" + fc.ToString("0.00", CultureInfo.InvariantCulture);
            return (box, key, (w, h) => UiRaster.ShutterBase(w, h, wc, hc, fc));
        }

        /// <summary>How many slats the lid has (about one a row).</summary>
        public int SlatCount => Math.Max(2, (int)Math.Round(Top.Height / (_board.Cell * 0.95f)));

        /// <summary>
        /// Slat <paramref name="k"/> (0 at the top) at <paramref name="open"/> seconds' share of the opening (0: closed): one
        /// after another from the top each lifts off, slides aside and turns away, fading.
        /// </summary>
        public ShutterSlat Slat(int k, float open)
        {
            float cell = _board.Cell;
            int n = SlatCount;
            float sh = Top.Height / n;
            float t = Clamp01((open - (k * 0.09f)) / 0.4f);
            var slat = new Box(Top.Left - (cell * 0.05f), Top.Top + (k * sh), Top.Right + (cell * 0.05f), Top.Top + ((k + 1) * sh));
            float lift = Smooth(t) * cell * 2.4f;
            float dx = (k % 2 == 0 ? -1f : 1f) * Smooth(t) * cell * 1.6f;
            float turn = (k % 2 == 0 ? -1f : 1f) * t * 18f;
            int seed = ((k + TopRow) % 5 + 5) % 5;
            return new ShutterSlat(slat.Offset(dx, -lift), turn, 1f - t, seed);
        }

        /// <summary>The slats' cache key (<see cref="UiRaster.Plank"/> of the signs' honey wood).</summary>
        public static string SlatKey(int seed) => Slot + "/slat/" + seed.ToString(CultureInfo.InvariantCulture);

        /// <summary>A slat's picture.</summary>
        public static byte[] SlatPicture(int width, int height, int seed) =>
            UiRaster.Plank(width, height, Math.Min(width, height) * 0.22f, Math.Max(1f, Math.Min(width, height) * 0.06f), WoodTone.Light, seed);

        /// <summary>The side battens' alpha at <paramref name="open"/> (they go after most slats).</summary>
        public static float SidesAlpha(float open) => 1f - Clamp01((open - 0.5f) / 0.3f);

        /// <summary>The base's (shadow and front edge's) alpha at <paramref name="open"/>.</summary>
        public static float BaseAlpha(float open) => 1f - Clamp01(open / 0.35f);

        /// <summary>The two side battens (upright boxes, <see cref="UiRaster.ShutterBatten"/>).</summary>
        public (Box Left, Box Right) Battens()
        {
            float cell = _board.Cell;
            float bw = Math.Max(cell * 0.6f, Top.Width * 0.09f);
            float l = Top.Left + (bw * 0.75f);
            float r = Top.Right - (bw * 0.75f);
            return (new Box(l - (bw / 2f), Top.Top - (cell * 0.12f), l + (bw / 2f), Top.Bottom + (cell * 0.1f)),
                new Box(r - (bw / 2f), Top.Top - (cell * 0.12f), r + (bw / 2f), Top.Bottom + (cell * 0.1f)));
        }

        /// <summary>The tag's cube box at rest (before its sway and pop).</summary>
        public Box Tag
        {
            get
            {
                float m = Math.Min(Top.Width, Top.Height);
                float d = Math.Max(Math.Min(Math.Max(m * 0.5f, _tagMin), m * 0.92f), _tagMin) * 0.95f;
                return Box.FromCenter(Top.CenterX, Top.Top + (Top.Height * 0.56f), d, d);
            }
        }

        /// <summary>The nail the tag hangs from (screen point): over the tag's middle.</summary>
        public (float X, float Y) Nail
        {
            get
            {
                Box tag = Tag;
                return (tag.CenterX, tag.Top - (tag.Height * 0.62f));
            }
        }

        /// <summary>The nail's size as a share of the tag's width.</summary>
        public const float NailShare = 0.16f;

        /// <summary>The nail's size in pixels.</summary>
        public float NailSize => Tag.Width * NailShare;

        /// <summary>The strings' picture box (<see cref="UiRaster.ShutterHanger"/>): from the nail down to the tag's top.</summary>
        public Box Hanger
        {
            get
            {
                Box tag = Tag;
                (float nx, float ny) = Nail;
                return new Box(tag.Left, ny - (NailSize / 2f), tag.Right, tag.Top + (tag.Height * 0.14f));
            }
        }

        /// <summary>The tag's sway at <paramref name="seconds"/>, degrees round the nail.</summary>
        public static float Sway(float seconds) => (float)Math.Sin(seconds * 1.7) * 4f;

        /// <summary>The tag's scale: its pop <paramref name="sinceCount"/> seconds after a counted cube, and its swell as it opens.</summary>
        public static float TagScale(float sinceCount, float open)
        {
            float pop = sinceCount >= 0f && sinceCount < PopSeconds ? 1f - (sinceCount / PopSeconds) : 0f;
            return 1f + (0.2f * (float)Math.Sin(pop * Math.PI)) + (open > 0f ? 0.25f * open : 0f);
        }

        /// <summary>The tag's alpha at <paramref name="open"/> (it fades early in the opening).</summary>
        public static float TagAlpha(float open) => 1f - Clamp01(open / 0.35f);

        /// <summary>
        /// A counted cube's spark <paramref name="since"/> seconds after its clear (null outside <see cref="SparkSeconds"/>):
        /// from the cube's cell <paramref name="from"/> on an arc to the tag, shrinking a little.
        /// </summary>
        public Box? Spark(Box from, float since)
        {
            float k = since / SparkSeconds;
            if (k < 0f || k > 1f)
            {
                return null;
            }

            Box tag = Tag;
            float e = Smooth(k);
            float x = from.CenterX + ((tag.CenterX - from.CenterX) * e);
            float y = from.CenterY + ((tag.CenterY - from.CenterY) * e) - ((float)Math.Sin(k * Math.PI) * _board.Cell * 2.2f);
            float r = Math.Max(5f, _board.Cell * 0.34f) * (1f - (0.35f * k)) * 1.4f;
            return Box.FromCenter(x, y, r * 2f, r * 2f);
        }

        /// <summary>The cache key of a spark of material <paramref name="code"/> (<see cref="UiRaster.ShutterSpark"/>).</summary>
        public static string SparkKey(char code) => Slot + "/spark/" + code;

        /// <summary>The puffs of dust at <paramref name="open"/> round the lid.</summary>
        public void Dust(float open, List<ShutterBit> into)
        {
            into.Clear();
            if (open <= 0f)
            {
                return;
            }

            float cell = _board.Cell;
            Box top = Top;
            float front = FrontPixels;
            for (int i = 0; i < 16; i++)
            {
                float u = Hash01(i);
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
                into.Add(new ShutterBit(Box.FromCenter(x + dx, y + dy - (cell * 0.6f * k), r * 2f, r * 2f), 0f, 0.4f * (1f - k) / 0.4f));
            }
        }

        /// <summary>The leaves flying off at <paramref name="open"/> (shape <c>symbol.leaf</c> in <c>stone.moss</c>).</summary>
        public void Leaves(float open, List<ShutterBit> into)
        {
            into.Clear();
            if (open <= 0f)
            {
                return;
            }

            float cell = _board.Cell;
            Box top = Top;
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
                into.Add(new ShutterBit(Box.FromCenter(x, y, s, s), (i * 60f) + (k * 260f), 1f - (k * k)));
            }
        }

        /// <summary>The opening's share at <paramref name="since"/> seconds after the event (0 closed, 1 done).</summary>
        public static float OpenShare(float since) => since < 0f ? 0f : Clamp01(since / OpenSeconds);

        private static float Hash01(int i)
        {
            unchecked
            {
                uint h = (uint)((i * 374761393) + 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Smooth(float t) => t * t * (3f - (2f * t));
    }
}
