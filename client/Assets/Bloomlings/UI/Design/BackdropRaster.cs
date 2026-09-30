using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Which garden scene a backdrop shows (research R8).</summary>
    public enum BackdropScene
    {
        /// <summary>Gameplay: bushes frame the sides, arches far away, the board covers the middle (frames 7–9).</summary>
        Gameplay,

        /// <summary>Home: arches in the middle distance and a flat stone the Bloomlings sit on (frames 2 and 3).</summary>
        Home,

        /// <summary>Splash: the Home scene with more blossoms (frame 1).</summary>
        Splash,
    }

    /// <summary>
    /// The procedural garden backdrop, rendered as RGBA pixels in both clients (research R8, FR-008). It layers:
    /// <list type="bullet">
    /// <item><description>a sky gradient;</description></item>
    /// <item><description>distant stone arches;</description></item>
    /// <item><description>far and near rolling hills;</description></item>
    /// <item><description>round bushes at the sides;</description></item>
    /// <item><description>small blossom dots.</description></item>
    /// </list>
    /// Everything is tinted by the level band's theme (<see cref="DesignTokens.Backdrop"/>) and kept light so the board
    /// keeps its contrast. It is rendered at low resolution and upscaled smoothly by the client, then cached. It stands
    /// in for the <c>bg.*</c> asset slots. Engine-free.
    /// </summary>
    public static class BackdropRaster
    {
        /// <summary>RGBA bytes, row by row from the top, of a <paramref name="width"/> × <paramref name="height"/> backdrop.</summary>
        public static byte[] Render(int width, int height, BackdropColors colors, BackdropScene scene)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            var pixels = new byte[width * height * 4];
            float aspect = (float)height / width;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    // x in 0–1 across the width, y in width units from the top (circles stay round).
                    float x = (px + 0.5f) / width;
                    float y = (py + 0.5f) / width;
                    Rgba c = Sample(x, y, aspect, colors, scene);
                    int i = ((py * width) + px) * 4;
                    pixels[i] = c.R;
                    pixels[i + 1] = c.G;
                    pixels[i + 2] = c.B;
                    pixels[i + 3] = 255;
                }
            }

            return pixels;
        }

        /// <summary>The color of one point: x in 0–1, y in width units from the top, aspect = height / width.</summary>
        public static Rgba Sample(float x, float y, float aspect, BackdropColors colors, BackdropScene scene)
        {
            float v = y / aspect; // 0–1 down the screen
            float horizon = scene == BackdropScene.Gameplay ? 0.42f : 0.5f;

            // Sky.
            Rgba c = colors.SkyTop.Mix(colors.SkyBottom, Smooth(v / horizon));

            // Soft clouds.
            float cloud = Blob(x, y, 0.22f, 0.1f * aspect, 0.16f, 0.05f) + Blob(x, y, 0.72f, 0.16f * aspect, 0.2f, 0.06f);
            c = c.Mix(Rgba.White, Math.Min(0.55f, cloud * 0.55f));

            // Distant stone arches (the board's ruins), on the horizon.
            float archBase = (horizon + 0.04f) * aspect;
            float arch = Math.Max(Arch(x, y, scene == BackdropScene.Gameplay ? 0.3f : 0.36f, archBase, 0.2f, 0.26f), Arch(x, y, scene == BackdropScene.Gameplay ? 0.7f : 0.68f, archBase, 0.16f, 0.2f));
            c = c.Mix(colors.Ruin, arch * 0.85f);

            // Far and near hills.
            float far = ((horizon + 0.02f) * aspect) + (0.035f * (float)Math.Sin((x * 6.1f) + 1.2f));
            c = c.Mix(colors.HillFar, Edge(y - far, 0.004f));
            float near = ((horizon + 0.12f) * aspect) + (0.05f * (float)Math.Sin((x * 4.3f) + 2.4f));
            c = c.Mix(colors.HillNear, Edge(y - near, 0.004f));
            c = c.Mix(colors.HillNear.Darken(0.06f), Edge(y - (near + (0.35f * aspect)), 0.2f) * 0.6f);

            // Home and splash: the flat stone the Bloomlings sit on.
            if (scene != BackdropScene.Gameplay)
            {
                float stone = Ellipse(x, y, 0.5f, (horizon + 0.2f) * aspect, 0.3f, 0.07f);
                c = c.Mix(DesignTokens.Colors.TileStone.Lighten(0.35f), Edge(-stone, 0.01f));
                float top = Ellipse(x, y, 0.5f, ((horizon + 0.2f) * aspect) - 0.02f, 0.26f, 0.045f);
                c = c.Mix(DesignTokens.Colors.TileStone.Lighten(0.55f), Edge(-top, 0.01f));
            }

            // Hedges of round bushes framing the sides, shaded below and lit above, with blossom dots.
            float bushes = 0f;
            float shade = 0f;
            float blossom = 0f;
            foreach ((float bx, float by, float r) in Bushes(scene))
            {
                float cy = by * aspect;
                float d = Distance(x, y, bx, cy) - r;
                float inside = Edge(-d, 0.006f);
                if (inside > bushes)
                {
                    bushes = inside;
                    shade = Math.Max(0f, Math.Min(1f, ((y - cy) / r) + 0.3f));
                }

                if (d < 0f)
                {
                    blossom = Math.Max(blossom, Blossoms(x, y, bx, cy, r));
                }
            }

            c = c.Mix(colors.Bush.Lighten(0.1f).Mix(colors.Bush.Darken(0.14f), shade), bushes);
            if (blossom > 0f)
            {
                c = c.Mix(colors.Blossom, blossom);
            }

            // Ground flowers along the bottom of Home and the splash.
            if (scene != BackdropScene.Gameplay && v > 0.75f)
            {
                c = c.Mix(colors.Blossom, Blossoms(x, y, 0.5f, y, 2f) * 0.8f);
            }

            return c;
        }

        private static readonly (float X, float Y, float R)[] GameplayBushes = Hedges(0.08f, 1.05f, 0.075f, 0.11f);

        private static readonly (float X, float Y, float R)[] HomeBushes = Hedges(0.18f, 1.05f, 0.09f, 0.13f);

        private static (float X, float Y, float R)[] Bushes(BackdropScene scene) => scene == BackdropScene.Gameplay ? GameplayBushes : HomeBushes;

        /// <summary>Two hedges of overlapping round bushes down the screen's sides, from <paramref name="from"/> to <paramref name="to"/> (0–1 of the height).</summary>
        private static (float X, float Y, float R)[] Hedges(float from, float to, float step, float radius)
        {
            var list = new System.Collections.Generic.List<(float, float, float)>();
            int i = 0;
            for (float y = from; y <= to; y += step, i++)
            {
                float wobble = 0.02f * (float)Math.Sin(i * 1.7f);
                float r = radius * (0.85f + (0.3f * Hash(i, 3f)));
                list.Add((-0.03f + wobble + (i % 2 == 0 ? 0f : 0.05f), y, r));
                list.Add((1.03f - wobble - (i % 2 == 1 ? 0f : 0.05f), y + (step / 2f), r));
            }

            return list.ToArray();
        }

        /// <summary>Small round blossoms scattered in a cell grid, stable for a given place.</summary>
        private static float Blossoms(float x, float y, float cx, float cy, float r)
        {
            const float cell = 0.045f;
            float gx = (float)Math.Floor(x / cell);
            float gy = (float)Math.Floor(y / cell);
            float h = Hash(gx, gy);
            if (h > 0.35f)
            {
                return 0f;
            }

            float ox = (gx + 0.3f + (0.4f * Hash(gy, gx))) * cell;
            float oy = (gy + 0.3f + (0.4f * h * 2.5f)) * cell;
            float d = Distance(x, y, ox, oy) - (0.009f + (0.006f * h));
            return Edge(-d, 0.003f);
        }

        private static float Arch(float x, float y, float cx, float baseY, float width, float height)
        {
            float half = width / 2f;
            bool inBlock = Math.Abs(x - cx) < half && y > baseY - height && y < baseY;
            if (!inBlock)
            {
                return 0f;
            }

            // The opening: a round-topped doorway in the block's middle.
            float openHalf = half * 0.55f;
            float openTop = baseY - (height * 0.62f);
            bool inOpening = Math.Abs(x - cx) < openHalf && (y > openTop || Distance(x, y, cx, openTop) < openHalf);
            if (inOpening)
            {
                return 0f;
            }

            // The top of the block: a soft round crown.
            return y < baseY - height + 0.02f ? 0.5f : 1f;
        }

        private static float Ellipse(float x, float y, float cx, float cy, float rx, float ry) =>
            (float)Math.Sqrt((((x - cx) / rx) * ((x - cx) / rx)) + (((y - cy) / ry) * ((y - cy) / ry))) - 1f;

        private static float Blob(float x, float y, float cx, float cy, float rx, float ry)
        {
            float e = Ellipse(x, y, cx, cy, rx, ry);
            return e < 0f ? Math.Min(1f, -e * 2f) : 0f;
        }

        private static float Distance(float x, float y, float cx, float cy) => (float)Math.Sqrt(((x - cx) * (x - cx)) + ((y - cy) * (y - cy)));

        /// <summary>0 outside, 1 inside, with a soft edge of <paramref name="soft"/> width units.</summary>
        private static float Edge(float inside, float soft) => Math.Max(0f, Math.Min(1f, 0.5f + (inside / soft)));

        private static float Smooth(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * (3f - (2f * t));
        }

        private static float Hash(float a, float b)
        {
            double s = Math.Sin((a * 127.1) + (b * 311.7)) * 43758.5453;
            return (float)(s - Math.Floor(s));
        }
    }
}
