using System;
using System.Globalization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The Drop water channel's pictures (rules 17 §8 «Drop Водный канал», spec 006 FR-069; the owner's look A of 2026-11-05,
    /// <see cref="ChannelLook"/>): the spring's and the exits' well, the open stream between cream stone banks, the dots of a
    /// route not chosen yet, the foam, the splash drops, the glow and dashed rings and a rider's bubble. Straight-alpha RGBA,
    /// deterministic, cached by key and size in both builds. Engine-free.
    /// </summary>
    public static partial class UiRaster
    {
        /// <summary>A well's height over its width as the board's oblique cubes are seen (<see cref="RoundScreenLook.BoardRowShare"/>).</summary>
        public const float WellRowShare = RoundScreenLook.BoardRowShare;

        /// <summary>A well's front face under its top ring, as a share of its half width.</summary>
        public const float WellFront = 0.214f;

        /// <summary>A well picture's half width over the well's half width (its outline and shadow).</summary>
        public const float WellSide = 1.16f;

        /// <summary>A well picture's top over the well's middle, in shares of its half height.</summary>
        public const float WellTop = 1.12f;

        /// <summary>A well picture's bottom under its front face, in shares of its half height (the front's oval and the shadow).</summary>
        public const float WellBottom = 1.62f;

        /// <summary>
        /// The channel's well (<c>board.channel</c>; the spring, and each exit smaller) in <paramref name="width"/> ×
        /// <paramref name="height"/> (<see cref="ChannelLook.WellBox"/>): a round well of the frame's cream stone seen from the
        /// board's viewpoint, its top ring of eight blocks over its front face, a soft shadow; inside it still dark water while
        /// it sleeps, or awake lighter water with two rings spreading (frame <paramref name="frame"/> of
        /// <see cref="ChannelLook.RingFrames"/>).
        /// </summary>
        public static byte[] ChannelSpring(int width, int height, bool awake, int frame)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float rx = width / 2f / WellSide;
            float ry = rx * WellRowShare;
            float cx = width / 2f;
            float cy = ry * WellTop;
            float front = rx * WellFront;
            float line = Math.Max(1f, rx * 0.045f);
            float ix = rx * 0.64f;
            float iy = ry * 0.64f;
            float joint = Math.Max(1f, rx * 0.054f);
            float blur = Math.Max(1f, rx * 0.14f);
            Rgba deep = awake ? C.WaterDeep : C.WaterDeep.Darken(0.35f);
            Rgba light = awake ? C.WaterTop : deep;
            float ringPhase = frame / (float)ChannelLook.RingFrames;
            Rows(width, height, py =>
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    var c = new Color(Rgba.White);
                    float a = 0f;

                    // its soft shadow below right
                    float sh = Ellipse(x - cx - (rx * 0.054f), y - cy - front - (rx * 0.107f), rx, ry);
                    Over(ref c, ref a, C.WoodDarkLine, 0.28f * (1f - Smooth(Clamp01((sh + blur) / (2f * blur)))));

                    // the front face: the lower oval and the band between the two ovals
                    float body = Math.Min(Ellipse(x - cx, y - cy - front, rx, ry), RectDistance(x, y, cx - rx, cy, cx + rx, cy + front));
                    Over(ref c, ref a, C.StoneLine, Coverage(body - line));
                    Over(ref c, ref a, C.StoneLip, Coverage(body));

                    // the top ring of blocks, lit from the upper left
                    float top = Ellipse(x - cx, y - cy, rx, ry);
                    Over(ref c, ref a, C.StoneLine, Coverage(top - line));
                    float cover = Coverage(top);
                    if (cover > 0f)
                    {
                        float tone = Clamp01(((x - (cx - rx)) / (2f * rx) * 0.5f) + ((y - (cy - ry)) / (2f * ry) * 0.5f));
                        Over(ref c, ref a, C.StoneTop.Mix(C.StoneFace, tone), cover);
                        float inner = Ellipse(x - cx, y - cy, ix, iy);
                        if (inner > 0f)
                        {
                            // the joints between the eight blocks
                            double angle = Math.Atan2((y - cy) / ry, (x - cx) / rx);
                            double k = Math.Round((angle / (Math.PI / 4.0)) - 0.5);
                            double delta = angle - ((k + 0.5) * Math.PI / 4.0);
                            float r = Length(x - cx, y - cy);
                            float d = Math.Abs((float)Math.Sin(delta)) * r;
                            Over(ref c, ref a, C.StoneLip, Coverage(d - (joint / 2f)) * cover);
                        }
                    }

                    // the water inside its dark rim
                    float well = Ellipse(x - cx, y - cy, ix, iy);
                    Over(ref c, ref a, C.StoneLine, Coverage(well - line));
                    float water = Coverage(well);
                    if (water > 0f)
                    {
                        float g = Clamp01(Length((x - (cx - (ix * 0.2f))) / ix, (y - (cy - (iy * 0.25f))) / iy));
                        Over(ref c, ref a, light.Mix(deep, g), water);
                        if (awake)
                        {
                            for (int ring = 0; ring < 2; ring++)
                            {
                                float t = (ringPhase + (ring * 0.5f)) % 1f;
                                float rr = Math.Max(0.5f, ix * t * 1.1f);
                                float rd = Math.Abs(Ellipse(x - cx, y - cy, rr, Math.Max(0.5f, iy * t * 1.1f)));
                                Over(ref c, ref a, C.WaterFoam, 0.8f * (1f - t) * Coverage(rd - (joint / 2f)) * water);
                            }
                        }
                    }

                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            });

            return pixels;
        }

        /// <summary>
        /// A ring in the garden's glow colour (<c>garden.glow</c>) inside <paramref name="width"/> × <paramref name="height"/>,
        /// its stroke <paramref name="strokeShare"/> of the shorter side: an oval in an oblong box, a circle in a square one. The
        /// exits that can be chosen and the calling spring wear it, breathing (hosts set its alpha).
        /// </summary>
        public static byte[] ChannelGlowRing(int width, int height, float strokeShare)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float stroke = Math.Max(1.5f, Math.Min(width, height) * strokeShare);
            float rx = (width / 2f) - (stroke / 2f) - 1f;
            float ry = (height / 2f) - (stroke / 2f) - 1f;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = Math.Abs(Ellipse(px + 0.5f - (width / 2f), py + 0.5f - (height / 2f), Math.Max(1f, rx), Math.Max(1f, ry)));
                    float k = Coverage(d - (stroke / 2f));
                    if (k > 0f)
                    {
                        Put(pixels, width, px, py, new Color(C.GardenGlow), k);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a glow ring (<see cref="ChannelGlowRing"/>).</summary>
        public static string ChannelGlowKey(float strokeShare) => ChannelLook.Slot + "/glow/" + Math.Round(strokeShare, 3).ToString("0.000", CultureInfo.InvariantCulture);

        /// <summary>A faint exit's dashed white ring (twenty dashes) inside a <paramref name="size"/> square.</summary>
        public static byte[] ChannelDashRing(int size)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float stroke = Math.Max(2f, size * 0.03f);
            float r = (size / 2f) - (stroke / 2f) - 1f;
            float c0 = size / 2f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f - c0;
                    float y = py + 0.5f - c0;
                    float d = Math.Abs(Length(x, y) - r);
                    float k = Coverage(d - (stroke / 2f));
                    if (k <= 0f)
                    {
                        continue;
                    }

                    double turn = (Math.Atan2(y, x) / (2.0 * Math.PI)) + 1.0;
                    double dash = (turn * 20.0) % 1.0;
                    float edge = (float)Math.Min(Math.Abs(dash - 0.55), Math.Min(dash, 1.0 - dash)) * (float)(2.0 * Math.PI * r / 20.0);
                    float on = dash < 0.55 ? Clamp01(edge + 0.5f) : 0f;
                    if (on > 0f)
                    {
                        Put(pixels, size, px, py, new Color(Rgba.White), 0.95f * k * on);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of the dashed ring (<see cref="ChannelDashRing"/>).</summary>
        public const string ChannelDashKey = ChannelLook.Slot + "/dash";

        /// <summary>A dot of a route not chosen yet in a <paramref name="size"/> square: a deep-water dot under a foam one.</summary>
        public static byte[] ChannelDot(int size) => Discs(size, (C.WaterDeep, 0.55f, 1f), (C.WaterFoam, 0.95f, 0.58f));

        /// <summary>The cache key of a route's dot (<see cref="ChannelDot"/>).</summary>
        public const string ChannelDotKey = ChannelLook.Slot + "/dot";

        /// <summary>A splash drop and the water's running head in a <paramref name="size"/> square: foam round light water.</summary>
        public static byte[] ChannelDrop(int size) => Discs(size, (C.WaterFoam, 0.95f, 1f), (C.WaterTop, 1f, 0.6f));

        /// <summary>The cache key of a splash drop (<see cref="ChannelDrop"/>).</summary>
        public const string ChannelDropKey = ChannelLook.Slot + "/drop";

        /// <summary>A foam dash on the stream in <paramref name="width"/> × <paramref name="height"/>: a capsule along its longer side.</summary>
        public static byte[] ChannelFoam(int width, int height)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float r = Math.Min(width, height) / 2f;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = RoundRect(px + 0.5f, py + 0.5f, 0f, 0f, width, height, r);
                    float k = Coverage(d);
                    if (k > 0f)
                    {
                        Put(pixels, width, px, py, new Color(C.WaterFoam), 0.85f * k);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a foam dash (<see cref="ChannelFoam"/>), across or upright.</summary>
        public static string ChannelFoamKey(bool across) => ChannelLook.Slot + "/foam/" + (across ? "h" : "v");

        /// <summary>
        /// A rider's bubble in a <paramref name="size"/> square (a walker riding the stream, 17 §8): a glossy bubble of water
        /// tinted with the walker's material <paramref name="color"/>, a light rim, a white shine at its upper left and a small
        /// one at its lower right.
        /// </summary>
        public static byte[] ChannelBubble(int size, Rgba color)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float r = (size / 2f) - 1f;
            float c0 = size / 2f;
            float line = Math.Max(1f, size * 0.035f);
            Rgba body = color.Mix(C.WaterTop, 0.2f);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f - c0;
                    float y = py + 0.5f - c0;
                    float d = Length(x, y) - r;
                    if (d > 1f)
                    {
                        continue;
                    }

                    var c = new Color(Rgba.White);
                    float a = 0f;
                    Over(ref c, ref a, color.Darken(0.35f).Mix(C.WaterDeep, 0.5f), Coverage(d));
                    float inner = Coverage(d + line);
                    float g = Clamp01(Length(x + (r * 0.3f), y + (r * 0.35f)) / (r * 1.4f));
                    Over(ref c, ref a, body.Lighten(0.35f).Mix(body.Darken(0.12f), g), inner * 0.92f);
                    float rim = Clamp01(1f - (Math.Abs(d + (line * 2.2f)) / (line * 1.4f)));
                    Over(ref c, ref a, Rgba.White, 0.35f * rim * inner);
                    float shine = Ellipse(x + (r * 0.36f), y + (r * 0.4f), r * 0.3f, r * 0.18f);
                    Over(ref c, ref a, Rgba.White, 0.9f * Coverage(shine));
                    float spot = Length(x - (r * 0.42f), y - (r * 0.38f)) - (r * 0.09f);
                    Over(ref c, ref a, Rgba.White, 0.7f * Coverage(spot));
                    if (a > 0f)
                    {
                        Put(pixels, size, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        /// <summary>The cache key of a rider's bubble (<see cref="ChannelBubble"/>) for a material code.</summary>
        public static string ChannelBubbleKey(char code) => ChannelLook.Slot + "/bubble/" + code;

        /// <summary>
        /// A piece of the channel's open stream (<see cref="ChannelLook.Stream"/>) in <paramref name="width"/> ×
        /// <paramref name="height"/>: along the route's line, a band <see cref="ChannelLook.StreamCells"/> wide of the frame's
        /// cream stone (its blocks' joints across it every 0.9 cell) raised over the cubes on its darker front face with a soft
        /// shadow, the deep water in its middle with a lighter strip, all with dark outlines; only the pixels whose nearest
        /// point of the line lies in the shape's bands, so the pieces of a growing stream join without a seam.
        /// </summary>
        public static byte[] ChannelStream(int width, int height, ChannelStreamShape shape)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            ChannelPath path = shape.Path;
            float sx = shape.WidthCells / width;
            float sy = shape.HeightCells / height;
            float half = ChannelLook.StreamCells / 2f;
            float water = ChannelLook.StreamCells * ChannelLook.WaterShare / 2f;
            float line = ChannelLook.LineCells;
            float lift = shape.Front;
            float px1 = 1f / Math.Max(1f, width / shape.WidthCells);
            float from = path.LengthTo(shape.FirstBand) - (shape.FirstBand > 0 ? px1 : 1000f);
            float to = path.LengthTo(shape.LastBand + 1) + (shape.LastBand + 1 < path.Points.Count - 1 ? px1 : 1000f);
            Rows(width, height, py =>
            {
                float y = (py + 0.5f) * sy;
                for (int px = 0; px < width; px++)
                {
                    float x = (px + 0.5f) * sx;
                    float d0 = path.Nearest(x, y, out float along);
                    if (along < from || along > to)
                    {
                        continue;
                    }

                    float d2 = path.Nearest(x - 0.08f, y - lift - 0.18f, out _);
                    float d1 = path.Nearest(x, y - (lift * 0.75f), out _);
                    if (d0 > half + line + 0.05f && d1 > half + line + 0.05f && d2 > 0.75f)
                    {
                        continue;
                    }

                    var c = new Color(Rgba.White);
                    float a = 0f;

                    // the soft shadow on the cubes
                    Over(ref c, ref a, C.WoodDarkLine, 0.3f * (1f - Smooth(Clamp01((d2 - 0.33f) / 0.28f))));

                    // the raised front face, then the top of the banks
                    Over(ref c, ref a, C.StoneLine, Edge(d1 - half - line, sx));
                    Over(ref c, ref a, C.StoneLip, Edge(d1 - half, sx));
                    Over(ref c, ref a, C.StoneLine, Edge(d0 - half - line, sx));
                    float top = Edge(d0 - half, sx);
                    Over(ref c, ref a, C.StoneTop, top);
                    if (top > 0f)
                    {
                        float m = (along - 0.45f) / 0.9f;
                        float j = Math.Abs(m - (float)Math.Round(m)) * 0.9f;
                        Over(ref c, ref a, C.StoneLip, 0.9f * top * Edge(j - 0.03f, sx));
                    }

                    // the water
                    Over(ref c, ref a, C.StoneLine, 0.9f * Edge(d0 - water - line, sx));
                    Over(ref c, ref a, C.WaterDeep, Edge(d0 - water, sx));
                    Over(ref c, ref a, C.WaterTop, 0.7f * Edge(d0 - (water * 0.5f), sx));
                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            });

            return pixels;
        }

        // A one-pixel anti-aliased edge for a distance in cells (pixel size s cells).
        private static float Edge(float distanceCells, float s) => Coverage(distanceCells / Math.Max(1e-4f, s));

        private static float RectDistance(float x, float y, float l, float t, float r, float b)
        {
            float dx = Math.Max(l - x, x - r);
            float dy = Math.Max(t - y, y - b);
            return Length(Math.Max(dx, 0f), Math.Max(dy, 0f)) + Math.Min(Math.Max(dx, dy), 0f);
        }

        // Concentric discs in a square: each (colour, alpha, radius share of the half side), the first lowest.
        private static byte[] Discs(int size, params (Rgba Color, float Alpha, float Share)[] discs)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float c0 = size / 2f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float r = Length(px + 0.5f - c0, py + 0.5f - c0);
                    var c = new Color(Rgba.White);
                    float a = 0f;
                    foreach ((Rgba color, float alpha, float share) in discs)
                    {
                        Over(ref c, ref a, color, alpha * Coverage(r - (c0 * share) + 0.5f));
                    }

                    if (a > 0f)
                    {
                        Put(pixels, size, px, py, c, a);
                    }
                }
            }

            return pixels;
        }
    }
}
