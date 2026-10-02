using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Which garden scene a backdrop shows (research R8).</summary>
    public enum BackdropScene
    {
        /// <summary>
        /// Gameplay: a lawn seen from above (spec 005 contracts/look.md §4.2), the board and its stone border lie on it
        /// (frames 7–9).
        /// </summary>
        Gameplay,

        /// <summary>Home: arches in the middle distance (frames 2 and 3). The 3D heroes bring their own pedestal and shadow (spec 004).</summary>
        Home,

        /// <summary>Splash: the Home scene with more blossoms (frame 1).</summary>
        Splash,

        /// <summary>
        /// The full-screen win (spec 005 FR-023, contracts/look.md §6.3): the gameplay garden softly blurred and lightened,
        /// with a warm glow in the middle behind the picture and the hero, until the owner's <c>win</c> picture exists.
        /// </summary>
        Win,
    }

    /// <summary>
    /// The procedural garden backdrop, rendered as RGBA pixels in both clients (research R8, FR-008). It stands in for
    /// the <c>bg.*</c> asset slots until the owner's pictures exist (spec 005 <c>pictures.md</c> B). Engine-free.
    /// <list type="bullet">
    /// <item><description>Gameplay is a lawn (spec 005 contracts/look.md §4.2): <c>lawn.light</c> to <c>lawn.dark</c>
    /// grass in soft patches with fine blades, small five-petal flowers, darker leafy clumps along the screen's edges and a
    /// soft vignette, no sky. The level band's theme tilts its hue (<see cref="DesignTokens.Backdrop"/>): a fresh green
    /// Daylight Garden, a teal Pond, a warm Orchard and a dusky Moonlit Garden.</description></item>
    /// <item><description>Home and the splash layer a sky gradient, distant stone arches, far and near rolling hills,
    /// round bushes at the sides and small blossom dots, tinted by the theme and kept light.</description></item>
    /// </list>
    /// The host renders it at a fraction of the screen (<see cref="Downscale"/>), upscales it smoothly and caches it.
    /// </summary>
    public static class BackdropRaster
    {
        /// <summary>
        /// How many screen pixels one backdrop pixel spans: the Home and splash skies are smooth and take a fifth of the
        /// screen's resolution; the gameplay lawn's blades and flowers need a third (spec 005 §4.2; about 0.6 s for a
        /// 1080 × 2340 screen on a desktop, once per theme and size); the win's blurred garden an eighth.
        /// </summary>
        public static int Downscale(BackdropScene scene) => scene switch
        {
            BackdropScene.Gameplay => 3,
            // The win's garden is the lawn blurred: rendered at an eighth and upscaled smoothly, its details melt.
            BackdropScene.Win => 8,
            _ => 5,
        };

        /// <summary>Whether a scene is the gameplay lawn seen from above (the gameplay and, blurred, the win).</summary>
        public static bool IsLawn(BackdropScene scene) => scene == BackdropScene.Gameplay || scene == BackdropScene.Win;

        /// <summary>RGBA bytes, row by row from the top, of a <paramref name="width"/> × <paramref name="height"/> backdrop.</summary>
        public static byte[] Render(int width, int height, BackdropColors colors, BackdropScene scene)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            var pixels = new byte[width * height * 4];
            float aspect = (float)height / width;
            Lawn? lawn = IsLawn(scene) ? Lawn.Of(colors) : null;
            lawn?.Prepare(aspect);
            bool win = scene == BackdropScene.Win;
            float texel = 1f / width;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    // x in 0–1 across the width, y in width units from the top (circles stay round).
                    float x = (px + 0.5f) / width;
                    float y = (py + 0.5f) / width;
                    int i = ((py * width) + px) * 4;
                    if (lawn != null)
                    {
                        Vec c = lawn.Sample(x, y, aspect, texel);
                        if (win)
                        {
                            c = WinLight(c, x, y, aspect);
                        }

                        pixels[i] = Byte(c.R);
                        pixels[i + 1] = Byte(c.G);
                        pixels[i + 2] = Byte(c.B);
                    }
                    else
                    {
                        Rgba c = Garden(x, y, aspect, colors, texel);
                        pixels[i] = c.R;
                        pixels[i + 1] = c.G;
                        pixels[i + 2] = c.B;
                    }

                    pixels[i + 3] = 255;
                }
            }

            return pixels;
        }

        /// <summary>The color of one point: x in 0–1, y in width units from the top, aspect = height / width.</summary>
        public static Rgba Sample(float x, float y, float aspect, BackdropColors colors, BackdropScene scene)
        {
            if (!IsLawn(scene))
            {
                // One sample at the resolution a phone renders the garden at (1080 px wide, a fifth).
                return Garden(x, y, aspect, colors, 1f / 216f);
            }

            // One sample at the resolution a phone renders the lawn at (1080 px wide, a third; the win an eighth).
            Vec c = Lawn.Of(colors).Sample(x, y, aspect, Downscale(scene) / 1080f);
            if (scene == BackdropScene.Win)
            {
                c = WinLight(c, x, y, aspect);
            }

            return new Rgba(Byte(c.R), Byte(c.G), Byte(c.B));
        }

        /// <summary>
        /// The win's garden (spec 005 §6.3): the lawn lightened toward a warm cream and a warm glow of <c>ray.light</c>
        /// around the middle of the screen (behind the picture and the hero), strongest at its center.
        /// </summary>
        private static Vec WinLight(Vec c, float x, float y, float aspect)
        {
            c = Vec.Mix(c, V(C.ParchmentTop), 0.24f);
            float d = Length(x - 0.5f, (y - (aspect * 0.5f)) * 0.8f) / 0.75f;
            float glow = Clamp01(1f - d);
            return Vec.Mix(c, V(C.RayLight), glow * glow * 0.62f);
        }

        // ---- Home and splash: sky, arches, hills, bushes ----

        /// <summary>
        /// One point of the Home and splash garden; <paramref name="texel"/> is one backdrop pixel in width units, the
        /// width of the arches' anti-aliased edges.
        /// </summary>
        private static Rgba Garden(float x, float y, float aspect, BackdropColors colors, float texel)
        {
            float v = y / aspect; // 0–1 down the screen
            const float horizon = 0.5f;

            // Sky.
            Rgba c = colors.SkyTop.Mix(colors.SkyBottom, Smooth(v / horizon));

            // Soft clouds.
            float cloud = Blob(x, y, 0.22f, 0.1f * aspect, 0.16f, 0.05f) + Blob(x, y, 0.72f, 0.16f * aspect, 0.2f, 0.06f);
            c = c.Mix(Rgba.White, Math.Min(0.55f, cloud * 0.55f));

            // Distant stone arches (the board's ruins), on the horizon.
            float archBase = (horizon + 0.04f) * aspect;
            float arch = Math.Max(Arch(x, y, 0.36f, archBase, 0.2f, 0.26f, texel), Arch(x, y, 0.68f, archBase, 0.16f, 0.2f, texel));
            c = c.Mix(colors.Ruin, arch * 0.85f);

            // Far and near hills.
            float far = ((horizon + 0.02f) * aspect) + (0.035f * (float)Math.Sin((x * 6.1f) + 1.2f));
            c = c.Mix(colors.HillFar, Edge(y - far, 0.004f));
            float near = ((horizon + 0.12f) * aspect) + (0.05f * (float)Math.Sin((x * 4.3f) + 2.4f));
            c = c.Mix(colors.HillNear, Edge(y - near, 0.004f));
            c = c.Mix(colors.HillNear.Darken(0.06f), Edge(y - (near + (0.35f * aspect)), 0.2f) * 0.6f);

            // Hedges of round bushes framing the sides, shaded below and lit above, with a leafy texture and a few
            // blossoms.
            float bushes = 0f;
            float shade = 0f;
            bool inBush = false;
            foreach ((float bx, float by, float r) in HomeBushes)
            {
                float cy = by * aspect;
                float d = Distance(x, y, bx, cy) - r;
                float inside = Edge(-d, 0.006f);
                if (inside > bushes)
                {
                    bushes = inside;
                    shade = Math.Max(0f, Math.Min(1f, ((y - cy) / r) + 0.3f));
                }

                inBush |= d < 0f;
            }

            if (bushes > 0f)
            {
                float leafy = Fbm(x * 30f, y * 30f, 71, 2) - 0.5f;
                Rgba bush = colors.Bush.Lighten(0.1f).Mix(colors.Bush.Darken(0.14f), shade);
                bush = leafy > 0f ? bush.Lighten(leafy * 0.16f) : bush.Darken(-leafy * 0.16f);
                c = c.Mix(bush, bushes);
            }

            if (inBush)
            {
                c = Blossoms(c, x, y, colors, 1f);
            }

            // Ground flowers along the bottom of Home and the splash, gathered toward the hedges at the sides.
            if (v > 0.75f)
            {
                float side = Math.Min(x, 1f - x);
                c = Blossoms(c, x, y, colors, 0.25f + (0.75f * Smooth((0.32f - side) / 0.22f)));
            }

            return c;
        }

        private static readonly (float X, float Y, float R)[] HomeBushes = Hedges(0.18f, 1.05f, 0.09f, 0.13f);

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

        /// <summary>
        /// Small round blossoms scattered in a cell grid, stable for a given place, of varied sizes: mostly the theme's
        /// pink, some white with a yellow middle. <paramref name="density"/> (0–1) thins them out.
        /// </summary>
        private static Rgba Blossoms(Rgba c, float x, float y, BackdropColors colors, float density)
        {
            const float cell = 0.045f;
            float gx = (float)Math.Floor(x / cell);
            float gy = (float)Math.Floor(y / cell);
            float h = Hash(gx, gy);
            if (h > 0.2f * density)
            {
                return c;
            }

            float k = Hash(gx + 17f, gy - 5f);
            float ox = (gx + 0.3f + (0.4f * Hash(gy, gx))) * cell;
            float oy = (gy + 0.3f + (0.4f * k)) * cell;
            float r = 0.006f + (0.01f * Hash(gy + 3f, gx + 11f));
            float rho = Distance(x, y, ox, oy);
            float petal = Edge(r - rho, 0.003f);
            if (petal <= 0f)
            {
                return c;
            }

            bool white = k > 0.62f;
            c = c.Mix(white ? C.GardenFlower : colors.Blossom, petal);
            return c.Mix(C.GardenFlowerCenter, Edge((r * 0.38f) - rho, 0.002f) * (white ? 1f : 0.7f));
        }

        /// <summary>
        /// A distant stone arch: a block with a round-topped doorway, as a signed distance blended over one texel, so its
        /// curves stay smooth at the backdrop's low resolution; its top band is half as strong (a soft crown).
        /// </summary>
        private static float Arch(float x, float y, float cx, float baseY, float width, float height, float texel)
        {
            float half = width / 2f;
            float dx = Math.Abs(x - cx);
            float top = baseY - height;
            float block = Math.Max(dx - half, Math.Max(top - y, y - baseY));

            // The opening: a round-topped doorway in the block's middle.
            float openHalf = half * 0.55f;
            float openTop = baseY - (height * 0.62f);
            float opening = Math.Min(Math.Max(dx - openHalf, openTop - y), Distance(x, y, cx, openTop) - openHalf);
            float d = Math.Max(block, -opening);
            float soft = 1.5f * texel;
            float inside = Edge(-d, soft);
            if (inside <= 0f)
            {
                return 0f;
            }

            return inside * (0.5f + (0.5f * Edge(y - (top + 0.02f), soft)));
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

        // ---- Gameplay: the lawn (spec 005 §4.2) ----

        /// <summary>
        /// The gameplay lawn of one theme: its colors, worked out once per picture, and the sampling of its layers (grass
        /// patches, blades, leafy clumps at the edges, flowers, vignette). Coordinates are those of <see cref="Sample"/>;
        /// <c>texel</c> is one backdrop pixel in width units, the width of every anti-aliased edge. Deterministic: noise
        /// comes from an integer hash.
        /// </summary>
        private sealed class Lawn
        {
            // Grid cells (width units) of the blades, the edge clumps' leaves and the flowers.
            private const float BladeCell = 0.011f;
            private const float LeafCell = 0.058f;
            private const float FlowerCell = 0.062f;
            private const float BushCell = 0.1f;
            private const float BigFlowerCell = 0.1f;

            // The blades of the whole picture, worked out once (Prepare): five numbers per cell (base x, y, tip x, y, light).
            private float[]? _blades;
            private int _bladeCols;
            private int _bladeRows;

            private readonly Vec _light;
            private readonly Vec _dark;
            private readonly Vec _sunny;
            private readonly Vec _blade;
            private readonly Vec _bladeLight;
            private readonly Vec _leaf;
            private readonly Vec _leafDeep;
            private readonly Vec _leafLine;
            private readonly Vec _leafVein;
            private readonly Vec _leafSpring;
            private readonly Vec _bush;
            private readonly Vec _bushLight;
            private readonly Vec _bushDeep;
            private readonly Vec _shade;
            private readonly Vec _path;
            private readonly Vec[] _petals;
            private readonly Vec _center;
            private readonly Vec _centerLine;

            private Lawn(Vec tilt, float dusk, Vec accentFlower, Vec blossom)
            {
                // Dusk dims red most and blue least, so the evening lawn turns a cooler blue-green.
                float day = 1f - dusk;
                var dim = new Vec(1f - dusk, 1f - (dusk * 0.85f), 1f - (dusk * 0.5f));
                _light = Vec.Scale(V(C.LawnLight) + tilt, dim);
                _dark = Vec.Scale(V(C.LawnDark) + (tilt * 0.8f), dim);
                _sunny = Vec.Mix(_light, V(Rgba.White), 0.2f);
                _blade = _dark * 0.72f;
                _bladeLight = Vec.Mix(_light, V(Rgba.White), 0.28f);
                Vec foliageDeep = Vec.Scale(V(C.FoliageDeep) + (tilt * 0.5f), dim);
                Vec foliage = Vec.Scale(V(C.Foliage) + (tilt * 0.6f), dim);
                Vec foliageLight = Vec.Scale(V(C.FoliageLight) + (tilt * 0.7f), dim);
                _leaf = foliage;
                _leafDeep = Vec.Mix(foliageDeep, foliage, 0.35f);
                _leafLine = foliageDeep * 0.7f;
                _leafVein = Vec.Mix(_light, V(Rgba.White), 0.1f);
                _leafSpring = Vec.Mix(foliageLight, V(C.GardenLeaf2) * day, 0.4f);
                _bush = foliage;
                _bushLight = foliageLight;
                _bushDeep = foliageDeep;
                _shade = _dark * 0.68f;
                _path = Vec.Mix(_light, V(C.GardenFlowerCenter), 0.16f);

                // Flowers keep more of their light at dusk than the grass.
                float bloom = 1f - (dusk * 0.5f);
                _petals = new[] { V(C.GardenFlower) * bloom, blossom * bloom, V(C.GardenFlowerCenter) * bloom, accentFlower * bloom, V(C.ButtonOrange) * bloom };
                _center = V(C.GardenFlowerCenter) * bloom;
                _centerLine = V(C.GardenFlowerCenterLine) * bloom;
            }

            /// <summary>
            /// The lawn of a theme. The theme's accent and background are recovered from the backdrop colors
            /// (<see cref="DesignTokens.Backdrop"/> mixes them 35% into <c>backdrop.hill_far</c> and
            /// <c>backdrop.sky_bottom</c>): the accent's hue, away from its grey, tilts the grass (a clear tilt only, so
            /// the Daylight Garden keeps the reference's <c>lawn.light</c> and <c>lawn.dark</c>) and colors a fourth
            /// flower; a dimmer background darkens the lawn toward dusk.
            /// </summary>
            public static Lawn Of(BackdropColors colors)
            {
                Vec accent = (V(colors.HillFar) - (V(C.BackdropHillFar) * 0.65f)) * (1f / 0.35f);
                Vec background = (V(colors.SkyBottom) - (V(C.BackdropSkyBottom) * 0.65f)) * (1f / 0.35f);
                Vec delta = accent - V(C.BackdropHillFar);
                Vec tilt = delta - Vec.Grey(delta.Mean);
                float strength = Smooth((tilt.Length - 8f) / 14f);
                tilt = tilt * (1.25f * strength);
                float dusk = Clamp01((Luma(V(C.BackdropSkyBottom)) - Luma(background) - 0.02f) * 8f) * 0.3f;

                // The fourth flower: the accent made vivid, or the lotus pink where the theme is close to grey.
                Vec vivid = accent + ((accent - Vec.Grey(accent.Mean)) * 5f);
                Vec accentFlower = Vec.Mix(V(C.LotusFill), vivid.Clamped(), strength);
                return new Lawn(tilt, dusk, accentFlower, V(colors.Blossom));
            }

            public Vec Sample(float x, float y, float aspect, float texel)
            {
                // Grass: big soft patches of shade and sun, a finer mottle.
                float n = Fbm(x * 4.6f, y * 4.6f, 11, 3);
                Vec c = Vec.Mix(_light, _dark, Smooth((n - 0.36f) / 0.34f) * 0.72f);
                c = Vec.Mix(c, _sunny, Smooth((0.4f - n) / 0.12f) * 0.45f);

                // Lighter, sunny paths winding through the grass (the ridges of a slow noise).
                float ridge = Math.Abs(Fbm(x * 1.9f, y * 1.9f, 17, 3) - 0.5f);
                c = Vec.Mix(c, _path, Smooth(1f - (ridge / 0.045f)) * 0.5f);
                c = c * (1f + ((Fbm(x * 24f, y * 24f, 23, 2) - 0.5f) * 0.16f));

                c = Blades(c, x, y, texel);

                // How close the point is to the screen's edges: the hedge, bushes, leaves and flowers gather there.
                float edge = Math.Min(Math.Min(x, 1f - x), Math.Min(y, aspect - y));
                if (edge < 0.26f)
                {
                    c = Hedge(c, x, y, aspect, texel);
                    c = Bushes(c, x, y, aspect, texel);
                    c = Clumps(c, x, y, aspect, texel);
                    c = Flowers(c, x, y, aspect, texel, BigFlowerCell, 100, edgeOnly: true);
                }

                c = Flowers(c, x, y, aspect, texel, FlowerCell, 0, edgeOnly: false);

                // A soft vignette toward the sides and the ends.
                float vx = Smooth(1f - (Math.Min(x, 1f - x) / 0.24f));
                float vy = Smooth(1f - (Math.Min(y, aspect - y) / 0.34f));
                float vignette = Math.Max(vx, vy) * (0.75f + (0.25f * Math.Min(vx, vy)));
                return Vec.Mix(c, _shade, vignette * 0.12f);
            }

            /// <summary>Works out every blade of a picture <paramref name="aspect"/> times as tall as wide once, before sampling it.</summary>
            public void Prepare(float aspect)
            {
                _bladeCols = (int)Math.Ceiling(1f / BladeCell) + 3;
                _bladeRows = (int)Math.Ceiling(aspect / BladeCell) + 3;
                _blades = new float[_bladeCols * _bladeRows * 5];
                for (int row = 0; row < _bladeRows; row++)
                {
                    for (int col = 0; col < _bladeCols; col++)
                    {
                        Blade(col - 1, row - 1, _blades, ((row * _bladeCols) + col) * 5);
                    }
                }
            }

            /// <summary>The blade of one cell: its base and tip (width units) and whether it is a light one.</summary>
            private static void Blade(int cx, int cy, float[] into, int at)
            {
                float bx = (cx + Hash(cx, cy, 1)) * BladeCell;
                float by = (cy + 0.4f + (0.6f * Hash(cx, cy, 2))) * BladeCell;
                float lean = (Hash(cx, cy, 3) - 0.5f) * 0.9f;
                float length = BladeCell * (0.62f + (0.34f * Hash(cx, cy, 4)));
                into[at] = bx;
                into[at + 1] = by;
                into[at + 2] = bx + ((float)Math.Sin(lean) * length);
                into[at + 3] = by - ((float)Math.Cos(lean) * length);
                into[at + 4] = Hash(cx, cy, 5) < 0.58f ? 0f : 1f;
            }

            /// <summary>Fine grass blades: one short tapered stroke per small cell, leaning a little, dark or light.</summary>
            private Vec Blades(Vec c, float x, float y, float texel)
            {
                int gx = Floor(x / BladeCell);
                int gy = Floor(y / BladeCell);
                float dark = 0f;
                float light = 0f;
                float[]? one = null;
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int cx = gx + i;
                        int cy = gy + j;
                        float[] blades;
                        int at;
                        int col = cx + 1;
                        int row = cy + 1;
                        if (_blades != null && col >= 0 && row >= 0 && col < _bladeCols && row < _bladeRows)
                        {
                            blades = _blades;
                            at = ((row * _bladeCols) + col) * 5;
                        }
                        else
                        {
                            one ??= new float[5];
                            Blade(cx, cy, one, 0);
                            blades = one;
                            at = 0;
                        }

                        (float d, float t) = Segment(x, y, blades[at], blades[at + 1], blades[at + 2], blades[at + 3]);
                        float cover = Clamp01(0.5f - ((d - (BladeCell * 0.1f * (1f - (0.75f * t)))) / texel));
                        if (cover <= 0f)
                        {
                            continue;
                        }

                        if (blades[at + 4] == 0f)
                        {
                            dark = Math.Max(dark, cover);
                        }
                        else
                        {
                            light = Math.Max(light, cover);
                        }
                    }
                }

                c = Vec.Mix(c, _blade, dark * 0.42f);
                return Vec.Mix(c, _bladeLight, light * 0.32f);
            }

            /// <summary>
            /// Soft bushes right at the screen's edges: bumpy round clumps in darker greens, lit from the upper left, with a
            /// leafy speckle inside, a deeper rim and a soft shadow on the grass to the lower right.
            /// </summary>
            private Vec Bushes(Vec c, float x, float y, float aspect, float texel)
            {
                int gx = Floor(x / BushCell);
                int gy = Floor(y / BushCell);
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int cx = gx + i;
                        int cy = gy + j;
                        float bx = (cx + 0.2f + (0.6f * Hash(cx, cy, 51))) * BushCell;
                        float by = (cy + 0.2f + (0.6f * Hash(cx, cy, 52))) * BushCell;
                        float near = Math.Min(Math.Min(bx, 1f - bx), Math.Min(by, aspect - by));
                        if (Hash(cx, cy, 53) >= Smooth((0.11f - near) / 0.08f))
                        {
                            continue;
                        }

                        float r = BushCell * (0.6f + (0.4f * Hash(cx, cy, 54)));
                        float dx = x - bx;
                        float dy = y - by;
                        float dist = Length(dx, dy);
                        if (dist > r * 1.6f)
                        {
                            continue;
                        }

                        float bump = (Fbm(x * 70f, y * 70f, 57 + cx, 2) - 0.5f) * r * 0.45f;
                        float shadow = Length(dx - (r * 0.12f), dy - (r * 0.16f)) - r + bump;
                        c = Vec.Mix(c, _shade, 0.4f * Clamp01(1f - (shadow / (r * 0.3f))));
                        float d = dist - r + bump;
                        float cover = Clamp01(0.5f - (d / texel));
                        if (cover <= 0f)
                        {
                            continue;
                        }

                        // Lit from the upper left, deeper toward the lower right and at the rim; leaves as a speckle.
                        float lit = Clamp01(0.5f - ((dx * 0.6f) + (dy * 0.8f)) / (r * 1.6f));
                        Vec face = Vec.Mix(_bushDeep, _bush, lit);
                        float speckle = Fbm(x * 150f, y * 150f, 61, 2);
                        face = Vec.Mix(face, _bushLight, Smooth((speckle - 0.55f) / 0.12f) * 0.55f * (0.3f + lit));
                        face = Vec.Mix(face, _bushDeep, Smooth((0.4f - speckle) / 0.12f) * 0.35f);
                        face = Vec.Mix(face, _bushDeep * 0.85f, Clamp01(1f + (d / (r * 0.12f))));
                        c = Vec.Mix(c, face, cover);
                    }
                }

                return c;
            }

            /// <summary>
            /// Darker leafy clumps along the screen's edges: almond leaves, thicker the nearer the edge, each with a soft
            /// shadow, a dark outline, a lighter side and a midrib.
            /// </summary>
            private Vec Clumps(Vec c, float x, float y, float aspect, float texel)
            {
                int gx = Floor(x / LeafCell);
                int gy = Floor(y / LeafCell);
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int cx = gx + i;
                        int cy = gy + j;
                        float lx = (cx + 0.15f + (0.7f * Hash(cx, cy, 31))) * LeafCell;
                        float ly = (cy + 0.15f + (0.7f * Hash(cx, cy, 32))) * LeafCell;
                        float near = Math.Min(Math.Min(lx, 1f - lx), Math.Min(ly, aspect - ly));
                        float chance = Smooth((0.19f - near) / 0.13f) * 0.8f;
                        if (Hash(cx, cy, 33) >= chance)
                        {
                            continue;
                        }

                        // Leaves point inward from the nearest edge, fanned at random.
                        float inward = Math.Min(lx, 1f - lx) < Math.Min(ly, aspect - ly)
                            ? (lx < 0.5f ? 0f : (float)Math.PI)
                            : (ly < aspect / 2f ? (float)(Math.PI / 2.0) : (float)(-Math.PI / 2.0));
                        float angle = inward + ((Hash(cx, cy, 34) - 0.5f) * 2.4f);
                        float half = LeafCell * (0.46f + (0.34f * Hash(cx, cy, 35)));
                        float ca = (float)Math.Cos(angle);
                        float sa = (float)Math.Sin(angle);

                        // The shadow: the leaf moved down a little, soft.
                        (float shadow, _, _) = Leaf(x - lx, y - ly - (half * 0.18f), ca, sa, half);
                        c = Vec.Mix(c, _shade, 0.35f * Clamp01(1f - (shadow / (half * 0.25f))));

                        (float d, float u, float v) = Leaf(x - lx, y - ly, ca, sa, half);
                        float cover = Clamp01(0.5f - (d / texel));
                        if (cover <= 0f)
                        {
                            continue;
                        }

                        // Lighter on one side of the midrib, darker toward the tip; a dark rim; a light midrib.
                        float k = Hash(cx, cy, 36);
                        Vec face = Vec.Mix(_leafDeep, _leaf, (v > 0f ? 0.85f : 0.45f) + (0.15f * k));
                        if (k > 0.84f)
                        {
                            // A few young leaves in spring green.
                            face = Vec.Mix(_leafSpring * 0.86f, _leafSpring, v > 0f ? 1f : 0.55f);
                        }
                        face = Vec.Mix(face, _leafDeep, Clamp01((u / half) - 0.3f) * 0.4f);
                        float rib = Clamp01(1f - ((Math.Abs(v) - (texel * 0.4f)) / texel)) * Clamp01(((half * 0.82f) - Math.Abs(u)) / texel);
                        face = Vec.Mix(face, _leafVein, rib * 0.5f);
                        face = Vec.Mix(face, _leafLine, Clamp01(1f + ((d + (texel * 0.4f)) / (texel * 1.2f))));
                        c = Vec.Mix(c, face, cover);
                    }
                }

                return c;
            }

            /// <summary>
            /// The signed distance (width units) to an almond leaf at the origin along (ca, sa) with half length
            /// <paramref name="half"/>, and the point in the leaf's frame (u along it, v across).
            /// </summary>
            private static (float D, float U, float V) Leaf(float x, float y, float ca, float sa, float half)
            {
                float u = (x * ca) + (y * sa);
                float v = (-x * sa) + (y * ca);
                float width = half * 0.4f;
                float r = ((half * half) + (width * width)) / (2f * width);
                float o = r - width;
                float d = Math.Max(Length(u, v + o) - r, Length(u, v - o) - r);
                return (d, u, v);
            }

            /// <summary>
            /// The dense hedge along the screen's sides (the reference's deep foliage): deep greens with a leafy speckle and
            /// lighter leaf tips, its inner edge wavy, so no grass shows right at the edges.
            /// </summary>
            private Vec Hedge(Vec c, float x, float y, float aspect, float texel)
            {
                float side = Math.Min(x, 1f - x);
                float reach = 0.045f + (0.05f * Fbm(y * 7f, x < 0.5f ? 1f : 9f, 81, 3));
                float d = side - reach;
                float cover = Clamp01(0.5f - (d / (texel * 1.5f)));
                if (cover <= 0f)
                {
                    // Its soft shadow on the grass.
                    return Vec.Mix(c, _shade, 0.35f * Clamp01(1f - (d / 0.02f)));
                }

                float speckle = Fbm(x * 120f, y * 120f, 83, 2);
                Vec face = Vec.Mix(_bushDeep, _bush, Smooth((speckle - 0.35f) / 0.3f));
                face = Vec.Mix(face, _bushLight, Smooth((speckle - 0.62f) / 0.1f) * 0.6f);
                face = Vec.Mix(face, _bushDeep * 0.8f, Clamp01(1f + (d / 0.012f)) * 0.5f);
                return Vec.Mix(c, face, cover);
            }

            /// <summary>
            /// Five-petal flowers (pink, white, orange, yellow and the theme's own) in a grid of <paramref name="cell"/>,
            /// scattered, more of them toward the screen's edges (only there when <paramref name="edgeOnly"/>: the big ones):
            /// round petals with a darker rim, a lighter heart and a yellow center, over a soft shadow.
            /// </summary>
            private Vec Flowers(Vec c, float x, float y, float aspect, float texel, float cell, int seed, bool edgeOnly)
            {
                int gx = Floor(x / cell);
                int gy = Floor(y / cell);
                float r = cell * (0.16f + (0.08f * Hash(gx, gy, 41 + seed)));
                float fx = (gx + 0.5f + ((Hash(gx, gy, 42 + seed) - 0.5f) * (1f - (4f * r / cell)))) * cell;
                float fy = (gy + 0.5f + ((Hash(gx, gy, 43 + seed) - 0.5f) * (1f - (4f * r / cell)))) * cell;
                float near = Math.Min(Math.Min(fx, 1f - fx), Math.Min(fy, aspect - fy));
                float chance = edgeOnly ? 0.85f * Smooth((0.16f - near) / 0.1f) : 0.14f + (0.76f * Smooth((0.3f - near) / 0.2f));
                if (Hash(gx, gy, 44 + seed) >= chance)
                {
                    return c;
                }

                float dx = x - fx;
                float dy = y - fy;
                float rho = Length(dx, dy);
                if (rho > r * 1.35f)
                {
                    return c;
                }

                // Its shadow, then the petals: each point measured in its nearest petal's frame.
                float shadowRho = Length(dx - (r * 0.12f), dy - (r * 0.22f));
                c = Vec.Mix(c, _shade, 0.3f * Clamp01(1f - ((shadowRho - (r * 0.9f)) / (r * 0.35f))));
                const float step = (float)(2.0 * Math.PI / 5.0);
                float turn = Hash(gx, gy, 45 + seed) * step;
                float phi = (float)Math.Atan2(dy, dx) - turn;
                float local = phi - (step * (float)Math.Round(phi / step));
                float px = rho * (float)Math.Cos(local);
                float py = rho * (float)Math.Sin(local);
                float petal = Length(px - (r * 0.55f), py) - (r * 0.45f);
                float cover = Clamp01(0.5f - (petal / texel));
                if (cover > 0f)
                {
                    // Pink most often, then white, orange, the theme's own and yellow.
                    float pick = Hash(gx, gy, 46 + seed);
                    Vec color = pick < 0.36f ? _petals[1] : pick < 0.6f ? _petals[0] : pick < 0.74f ? _petals[4] : pick < 0.87f ? _petals[3] : _petals[2];
                    Vec face = Vec.Mix(color * 0.86f, Vec.Mix(color, V(Rgba.White), 0.25f), Clamp01(1f - (rho / r)));
                    face = Vec.Mix(face, color * 0.62f, Clamp01(1f + ((petal + (texel * 0.3f)) / texel)));
                    c = Vec.Mix(c, face, cover);
                }

                float center = rho - (r * 0.27f);
                c = Vec.Mix(c, _centerLine, Clamp01(0.5f - (center / texel)));
                return Vec.Mix(c, _center, Clamp01(0.5f - ((center + (texel * 0.9f)) / texel)));
            }
        }

        /// <summary>A working color in 0–255 floats (the lawn's colors are mixed and scaled without rounding).</summary>
        private readonly struct Vec
        {
            public Vec(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }

            public float R { get; }

            public float G { get; }

            public float B { get; }

            public float Mean => (R + G + B) / 3f;

            public float Length => (float)Math.Sqrt((R * R) + (G * G) + (B * B));

            public static Vec Grey(float v) => new Vec(v, v, v);

            /// <summary>Multiplies channel by channel.</summary>
            public static Vec Scale(Vec a, Vec k) => new Vec(a.R * k.R, a.G * k.G, a.B * k.B);

            public static Vec Mix(Vec a, Vec b, float t)
            {
                t = Clamp01(t);
                return new Vec(a.R + ((b.R - a.R) * t), a.G + ((b.G - a.G) * t), a.B + ((b.B - a.B) * t));
            }

            public Vec Clamped() => new Vec(Math.Max(0f, Math.Min(255f, R)), Math.Max(0f, Math.Min(255f, G)), Math.Max(0f, Math.Min(255f, B)));

            public static Vec operator +(Vec a, Vec b) => new Vec(a.R + b.R, a.G + b.G, a.B + b.B);

            public static Vec operator -(Vec a, Vec b) => new Vec(a.R - b.R, a.G - b.G, a.B - b.B);

            public static Vec operator *(Vec a, float k) => new Vec(a.R * k, a.G * k, a.B * k);
        }

        private static Vec V(Rgba c) => new Vec(c.R, c.G, c.B);

        /// <summary>The brightness of a color (Rec. 601 luma of its 0–255 channels), 0–1.</summary>
        private static float Luma(Vec c) => ((0.299f * c.R) + (0.587f * c.G) + (0.114f * c.B)) / 255f;

        private static byte Byte(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Length(float x, float y) => (float)Math.Sqrt((x * x) + (y * y));

        private static int Floor(float v)
        {
            int i = (int)v;
            return v < i ? i - 1 : i;
        }

        /// <summary>The distance from a point to a segment, and how far along the segment (0–1) the nearest point lies.</summary>
        private static (float D, float T) Segment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float t = Clamp01((((px - ax) * dx) + ((py - ay) * dy)) / Math.Max(1e-9f, (dx * dx) + (dy * dy)));
            return (Length(px - (ax + (t * dx)), py - (ay + (t * dy))), t);
        }

        /// <summary>A deterministic hash of two integers and a seed, in 0–1 (no floating-point trigonometry).</summary>
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)((x * 374761393) + (y * 668265263) + (seed * 1442695041));
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Value noise in 0–1: hashed lattice values, smoothly interpolated.</summary>
        private static float Noise(float x, float y, int seed)
        {
            int ix = Floor(x);
            int iy = Floor(y);
            float fx = Smooth(x - ix);
            float fy = Smooth(y - iy);
            float a = Hash(ix, iy, seed);
            float b = Hash(ix + 1, iy, seed);
            float c = Hash(ix, iy + 1, seed);
            float d = Hash(ix + 1, iy + 1, seed);
            return a + ((b - a) * fx) + ((c - a) * fy) + ((a - b - c + d) * fx * fy);
        }

        /// <summary>Fractal noise in 0–1: <paramref name="octaves"/> layers of value noise.</summary>
        private static float Fbm(float x, float y, int seed, int octaves)
        {
            float sum = 0f;
            float amp = 0.5f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Noise(x, y, seed + (i * 101));
                total += amp;
                x *= 2.03f;
                y *= 2.03f;
                amp *= 0.5f;
            }

            return sum / total;
        }
    }
}
