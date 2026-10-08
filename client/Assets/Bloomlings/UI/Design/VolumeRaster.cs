using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- Volume: soft cubes and raised buttons (the owner's request of 2026-10-08) ----

        /// <summary>
        /// Whether the board tiles are soft cubes (<see cref="Cube"/>) and the icon buttons raised faces on wooden plates
        /// (<see cref="RaisedPlate"/>, <see cref="RaisedFace"/>): the owner's volume proposal of 2026-10-08, rendered for
        /// review (the preview's <c>--volume</c>) and off in both builds until the owner approves it.
        /// </summary>
        public static bool VolumeLook { get; set; }

        /// <summary>The light that shapes the volumes: from the upper left, a little toward the viewer.</summary>
        private static readonly (float X, float Y, float Z) VolumeLight = Normalized(-0.45f, -0.75f, 1.1f);

        /// <summary>The highlight's half vector (the light and the viewer straight above).</summary>
        private static readonly (float X, float Y, float Z) VolumeHalf = Normalized(VolumeLight.X, VolumeLight.Y, VolumeLight.Z + 1f);

        /// <summary>A board tile's clear margin round its body, as a share of its side: the ground shows between the cubes.</summary>
        public const float CubeMargin = 0.035f;

        /// <summary>A board tile's front face under its top, as a share of its side (the top's middle is where the icon sits).</summary>
        public const float CubeSide = 0.1f;

        /// <summary>
        /// The soft cube of a board tile (the owner, 2026-10-08: "volumetric like cubes, but not cubes: the top must feel like
        /// the top of a cube"): a rounded square top (radius 17% of the side) whose edges round down over a bevel of 13% of
        /// the side, lit from the upper left (lighter where the bevel faces the light, deeper where it turns away, a soft
        /// highlight along the top left edge and a short white gloss dash in its corner), over a front face of
        /// <see cref="CubeSide"/> in the color darkened, all inside a clear margin of <see cref="CubeMargin"/>, with a
        /// soft deep edge where it meets the ground. Returns the color at (x, y) and its coverage, the top's middle for the
        /// symbol being (s / 2, (s − side) / 2).
        /// </summary>
        private static (Color Color, float Cover) Cube(float x, float y, float s, Rgba col)
        {
            float m = s * CubeMargin;
            float side = s * CubeSide;
            float r = s * 0.17f;
            float bevel = Math.Max(1.5f, s * 0.12f);
            float body = RoundRect(x, y, m, m, s - m, s - m, r);
            float cover = Coverage(body);
            if (cover <= 0f)
            {
                return (default, 0f);
            }

            float topBottom = s - m - side;
            float top = RoundRect(x, y, m, m, s - m, topBottom, r);
            Color c;
            if (top < 0.5f)
            {
                // The top: lighter at its top, a little deeper at its bottom, then the bevel's light and shade.
                float t = Clamp01((y - m) / Math.Max(1f, topBottom - m));
                c = new Color(col.Lighten(0.12f).Mix(col.Darken(0.04f), t));
                (float gx, float gy) = Gradient(x, y, (px, py) => RoundRect(px, py, m, m, s - m, topBottom, r));
                float e = Clamp01(-top / bevel);
                float tilt = 1f - e;
                (float nx, float ny, float nz) = BevelNormal(gx, gy, tilt);
                Shade(ref c, col, nx, ny, nz, 1.25f, 0.7f, 0.75f);

                // A cushion: the top's middle a little lighter.
                float dome = EllipseDistance(x - (s / 2f), y - ((m + topBottom) * 0.46f), s * 0.3f, s * 0.26f);
                c.Mix(col.Lighten(0.35f), 0.18f * (1f - Smooth(Clamp01((dome + (s * 0.04f)) / (s * 0.22f)))));

                // The gloss: a short white dash in the top left corner, along the edge.
                float gx0 = m + (s * 0.24f);
                float gy0 = m + (s * 0.13f);
                float dash = Segment(x, y, gx0 - (s * 0.08f), gy0 + (s * 0.02f), gx0 + (s * 0.05f), gy0 - (s * 0.012f)) - (s * 0.028f);
                c.Mix(Rgba.White, 0.55f * Clamp01(0.5f - (dash / Math.Max(1f, s * 0.02f))));

                // Where the top meets the front face, the bevel's lowest pixels blend into it.
                c.Mix(col.Darken(0.24f), Clamp01(top + 0.5f) * 0.5f);
            }
            else
            {
                // The front face: deeper downward, rounder at its ends, its top edge lit by the bevel above it.
                float t = Clamp01((y - (topBottom - r)) / Math.Max(1f, (s - m) - (topBottom - r)));
                c = new Color(col.Darken(0.2f).Mix(col.Darken(0.36f), t));
                float sideX = Clamp01((x - m) / Math.Max(1f, s - (2f * m)));
                c.Mix(col.Lighten(0.1f), 0.18f * Clamp01(1f - (sideX * 3f)));
                c.Mix(col.Darken(0.55f), 0.25f * Clamp01((sideX - 0.7f) * 3.3f));
            }

            // A soft, deep edge where the cube meets the ground.
            c.Mix(col.Darken(0.55f), 0.55f * Clamp01(1f + (body / Math.Max(1f, s * 0.02f))));
            return (c, cover);
        }

        /// <summary>How much more saturated the plate's wood is drawn (<see cref="Vivid"/>; the owner, 2026-10-08: "the color is still pale").</summary>
        public const float PlateVivid = 1.25f;

        /// <summary>How much more saturated a raised face's cream is drawn (<see cref="Vivid"/>).</summary>
        public const float FaceVivid = 1.15f;

        /// <summary>The thickness of an icon button's wooden plate under its top, as a share of the button's shorter side.</summary>
        public const float PlateSide = 0.035f;

        /// <summary>The thickness of an icon button's raised face under its top, as a share of the button's shorter side.</summary>
        public const float FaceSide = 0.022f;

        /// <summary>
        /// The wooden plate of an icon button or the speed pill (the owner, 2026-10-08: "the rim is volumetric, the button seems
        /// to come a little out of it, as if laid on a volumetric plane"), filling <paramref name="width"/> ×
        /// <paramref name="height"/> with corners of <paramref name="radius"/> px: a slab of light honey wood
        /// (<c>wood.light</c> grained) with its front side, <see cref="PlateSide"/> of the shorter side, in the wood's lip
        /// color under its top; the top's border, <see cref="GardenLook.IconRimShare"/> of the shorter side, rounds over at
        /// both edges (lit at the upper left, deeper at the lower right) inside a <c>wood.line</c> outline, and in its
        /// opening a dark groove and the soft shadow the raised face casts, downward (<see cref="RaisedFace"/> goes over it,
        /// in <see cref="RaisedFaceBox"/>).
        /// </summary>
        public static byte[] RaisedPlate(int width, int height, float radius)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float s = Math.Min(w, h);
            float rim = s * GardenLook.IconRimShare;
            float side = s * PlateSide;
            float topBottom = h - side;
            float line = Math.Max(1f, s * 0.016f);
            Box face = RaisedFaceBox(new Box(0f, 0f, w, h));
            float faceRadius = Math.Max(0f, radius - rim);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float body = RoundRect(x, y, 0f, 0f, w, h, radius);
                    float cover = Coverage(body);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float top = RoundRect(x, y, 0f, 0f, w, topBottom, radius);
                    Color c = Laminate(x, y, body, s, radius, w, h);
                    if (top < 0.5f)
                    {
                        // The border rounds over at the outside and into the opening; flat on its middle.
                        float q = Clamp01(-top / Math.Max(1f, rim * 1.15f));
                        (float gx, float gy) = Gradient(x, y, (ax, ay) => RoundRect(ax, ay, 0f, 0f, w, topBottom, radius));
                        float f = q < 0.4f ? (float)Math.Cos(q / 0.4f * Math.PI / 2f) : q > 0.75f ? -(float)Math.Cos((1f - Math.Min(1f, q)) / 0.25f * Math.PI / 2f) : 0f;
                        (float nx, float ny, float nz) = (gx * f, gy * f, (float)Math.Sqrt(Math.Max(0f, 1f - (f * f))));
                        Shade(ref c, C.WoodGrain, nx, ny, nz, 0.45f, 0.45f, 0.18f);

                        // The top of the frame catches the light (the reference's (249, 215, 168) above, deeper below).
                        c.Mix(C.WoodMid.Lighten(0.12f), 0.6f * Clamp01(1f - (y / (h * 0.3f))));
                        c.Mix(C.WoodEdge.Darken(0.12f), Clamp01(top + 0.5f) * 0.5f);
                    }
                    else
                    {
                        // The plate's front side: the turned edge's wood, a little deeper downward.
                        float t = Clamp01((y - (topBottom - radius)) / Math.Max(1f, h - (topBottom - radius)));
                        c.Mix(C.WoodEdge.Darken(0.06f), 0.45f + (0.25f * t));
                    }

                    // The opening: a dark groove round the face and the face's soft shadow below it.
                    float hole = RoundRect(x, y, face.Left, face.Top, face.Right, face.Bottom, faceRadius);
                    c.Mix(C.WoodLine, 0.22f * Clamp01(1f - (Math.Abs(hole - (s * 0.006f)) / Math.Max(1f, s * 0.012f))));
                    float shadow = RoundRect(x, y - (s * 0.03f), face.Left + (s * 0.01f), face.Top, face.Right - (s * 0.01f), face.Bottom, faceRadius);
                    c.Mix(C.WoodLine.Darken(0.25f), 0.16f * (1f - Smooth(Clamp01((shadow + (s * 0.005f)) / (s * 0.04f)))));

                    c.Mix(C.WoodLine, 0.55f * Clamp01(0.5f - (-body - line)));
                    c = new Color(Vivid(c.ToRgba(), PlateVivid));
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The box of the raised face on a plate filling <paramref name="box"/>: inside the border at the top and sides, and a
        /// little over the border at the bottom, standing on the plate's top.
        /// </summary>
        public static Box RaisedFaceBox(Box box)
        {
            float s = Math.Min(box.Width, box.Height);
            float rim = s * GardenLook.IconRimShare;
            return new Box(box.Left + (rim * 1.15f), box.Top + (rim * 1.1f), box.Right - (rim * 1.15f), box.Bottom - (s * PlateSide) - (rim * 0.85f));
        }

        /// <summary>The content box of a raised face in <paramref name="face"/> (its top less 9% all round).</summary>
        public static Box RaisedFaceContent(Box face, float buttonSide)
        {
            var top = new Box(face.Left, face.Top, face.Right, face.Bottom - (buttonSide * FaceSide));
            return top.Inset(Math.Min(top.Width, top.Height) * 0.09f);
        }

        /// <summary>
        /// The raised face of an icon button or the speed pill (see <see cref="RaisedPlate"/>), filling
        /// <paramref name="width"/> × <paramref name="height"/> with corners of <paramref name="radius"/> px: a cream slab
        /// (<paramref name="set"/>'s face, its top lighter at the upper left) whose top's edge rounds down over 15% of its
        /// shorter side, lit from the upper left, over its front side of <paramref name="sidePixels"/> in the set's lip.
        /// </summary>
        public static byte[] RaisedFace(int width, int height, float radius, float sidePixels, ColorSet set)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float w = width;
            float h = height;
            float s = Math.Min(w, h);
            float side = Math.Min(h * 0.3f, sidePixels);
            float topBottom = h - side;
            float bevel = Math.Max(1.5f, s * 0.07f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float body = RoundRect(x, y, 0f, 0f, w, h, radius);
                    float cover = Coverage(body);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float top = RoundRect(x, y, 0f, 0f, w, topBottom, radius);
                    Color c;
                    if (top < 0.5f)
                    {
                        float lit = Clamp01(1f - ((x / w * 0.5f) + (y / topBottom * 0.8f)));
                        c = new Color(set.Face.Mix(set.Lip, 0.12f).Mix(set.Top, 0.25f * lit));
                        c.Mix(set.Lip, 0.35f * Clamp01((y / topBottom) - 0.35f));
                        c.Mix(set.Lip.Darken(0.1f), FaceGrain(x, y, s));
                        (float gx, float gy) = Gradient(x, y, (ax, ay) => RoundRect(ax, ay, 0f, 0f, w, topBottom, radius));
                        float tilt = 1f - Clamp01(-top / bevel);
                        (float nx, float ny, float nz) = BevelNormal(gx, gy, tilt);
                        Shade(ref c, set.Face, nx, ny, nz, 0.35f, 0.2f, 0.15f);
                        c.Mix(set.Lip, Clamp01(top + 0.5f) * 0.2f);
                    }
                    else
                    {
                        float t = Clamp01((y - (topBottom - radius)) / Math.Max(1f, h - (topBottom - radius)));
                        c = new Color(set.Face.Mix(set.Lip, 0.45f + (0.3f * t)));
                    }

                    c.Mix(set.Lip.Darken(0.15f), 0.3f * Clamp01(1f + (body / Math.Max(1f, s * 0.02f))));
                    c = new Color(Vivid(c.ToRgba(), FaceVivid));
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The plate's laminate (the owner, 2026-10-08: "more wooden, like laminate flooring, and creamier"; matched to the
        /// reference's caramel cream, its border about (225, 176, 120) at the shaded side and (249, 215, 168) at the top): a
        /// board of warm wood (<c>wood.edge</c> toward <c>wood.mid</c>) cut into the frame, its grain running across the whole
        /// button: long streaks a little lighter or deeper and fine darker grain lines (<c>wood.grain</c>), wavering gently.
        /// </summary>
        private static Color Laminate(float x, float y, float outer, float s, float radius, float w, float h)
        {
            var c = new Color(C.WoodEdge.Mix(C.WoodMid, 0.25f).Mix(C.WoodGrain, 0.08f));

            // Long streaks along the board: slow across the button, quick down it.
            float streak = Fbm(x / (s * 0.7f), y / (s * 0.045f), 23, 3);
            c.Mix(C.WoodMid.Lighten(0.12f), 0.7f * Smooth(Clamp01((streak - 0.5f) / 0.3f)));
            c.Mix(C.WoodGrain, 0.45f * Smooth(Clamp01((0.42f - streak) / 0.3f)));

            // Fine grain lines across the button, wavering a little.
            float wave = Fbm(x / (s * 0.9f), y / (s * 0.1f), 29, 2);
            float lines = (float)Math.Abs(Math.Sin(((y / (s * 0.016f)) + (wave * 3.5f)) * Math.PI));
            float fine = (float)Math.Pow(1f - lines, 5);
            c.Mix(C.WoodGrain.Darken(0.1f), 0.5f * fine * (0.4f + (0.6f * Fbm(x / (s * 0.25f), y / (s * 0.05f), 31, 2))));
            return c;
        }

        /// <summary>The faint grain of a raised face's cream, across the button like the plate's (alpha 0–1 of <c>cream.lip</c>).</summary>
        private static float FaceGrain(float x, float y, float s)
        {
            float streak = Fbm(x / (s * 0.8f), y / (s * 0.05f), 37, 3);
            float wave = Fbm(x / (s * 0.9f), y / (s * 0.12f), 41, 2);
            float lines = (float)Math.Pow(1f - Math.Abs(Math.Sin(((y / (s * 0.02f)) + (wave * 3f)) * Math.PI)), 6);
            return (0.12f * Smooth(Clamp01((0.45f - streak) / 0.3f))) + (0.1f * lines);
        }

        /// <summary>The share of a raised glyph's picture its glyph box takes (the rest holds its shadow).</summary>
        public const float GlyphPictureShare = 0.86f;

        /// <summary>The picture box of a raised glyph whose shape fills <paramref name="glyph"/>, a little lower for its shadow.</summary>
        public static Box RaisedGlyphBox(Box glyph)
        {
            float side = Math.Max(glyph.Width, glyph.Height) / GlyphPictureShare;
            return Box.FromCenter(glyph.CenterX, glyph.CenterY + (side * 0.02f), side, side);
        }

        /// <summary>
        /// A button's glyph, a little raised (the owner, 2026-10-08: "even the icons on the buttons look a little
        /// volumetric"): the shape <paramref name="shapeId"/> in <paramref name="color"/>, filling
        /// <see cref="GlyphPictureShare"/> of a square picture of side <paramref name="size"/> (as the painters fit a
        /// shape, <see cref="ShapeRaster.Margin"/>), its edge rounding over a bevel of 4.5% of the glyph, lit from the upper
        /// left (lighter there, deeper at the lower right, a soft gloss), over a soft shadow below it and a thin light
        /// emboss line under its lower edge, as if pressed into the cream.
        /// </summary>
        public static byte[] RaisedGlyph(int size, string shapeId, Rgba color)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            float s = size;
            float box = s * GlyphPictureShare;
            float unit = box / 2f / ShapeRaster.Margin;
            float cx = s / 2f;
            float cy = (s / 2f) - (s * 0.02f);
            float bevel = Math.Max(1f, box * 0.045f);
            float shadowDrop = box * 0.035f;
            Func<float, float, float> d = (px, py) => sdf((px - cx) / unit, -(py - cy) / unit) * unit;
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    var paint = default(NavPaint);
                    float ds = d(x, y);

                    // The soft shadow below and the light emboss line under the lower edge.
                    float shadow = d(x, y - shadowDrop);
                    paint.Over(0.28f * (1f - Smooth(Clamp01((shadow + (box * 0.01f)) / (box * 0.06f)))), color.Darken(0.35f));
                    float emboss = d(x, y - (box * 0.022f));
                    paint.Over(0.5f * Coverage(emboss) * Clamp01(ds / Math.Max(0.5f, box * 0.01f)), C.CreamTop);

                    float cover = Coverage(ds);
                    if (cover > 0f)
                    {
                        float t = Clamp01((y - (cy - (box / 2f))) / box);
                        var c = new Color(color.Lighten(0.12f).Mix(color.Darken(0.08f), t));
                        (float gx, float gy) = Gradient(x, y, d);
                        (float nx, float ny, float nz) = BevelNormal(gx, gy, 1f - Clamp01(-ds / bevel));
                        Shade(ref c, color, nx, ny, nz, 0.6f, 0.45f, 0.22f);
                        paint.Over(cover, c.ToRgba());
                    }

                    paint.Write(pixels, ((py * size) + px) * 4);
                }
            }

            return pixels;
        }

        /// <summary>The normal of a bevel tilted <paramref name="tilt"/> (0 flat, 1 upright) toward the outward gradient (<paramref name="gx"/>, <paramref name="gy"/>).</summary>
        private static (float X, float Y, float Z) BevelNormal(float gx, float gy, float tilt)
        {
            // A quarter round: on a circle the normal's sine is the distance across the bevel.
            float sin = Math.Min(0.97f, Clamp01(tilt));
            float cos = (float)Math.Sqrt(Math.Max(0f, 1f - (sin * sin)));
            return (gx * sin, gy * sin, cos);
        }

        /// <summary>
        /// Lights <paramref name="c"/> by the normal: lighter toward <paramref name="col"/> lightened where it faces the light
        /// more than a flat top, deeper where less, and a highlight where it mirrors it.
        /// </summary>
        private static void Shade(ref Color c, Rgba col, float nx, float ny, float nz, float lighten, float darken, float gloss)
        {
            float flat = VolumeLight.Z;
            float lit = (nx * VolumeLight.X) + (ny * VolumeLight.Y) + (nz * VolumeLight.Z) - flat;
            if (lit > 0f)
            {
                c.Mix(col.Lighten(0.6f), Clamp01(lit * lighten));
            }
            else
            {
                c.Mix(col.Darken(0.5f), Clamp01(-lit * darken));
            }

            float half = (nx * VolumeHalf.X) + (ny * VolumeHalf.Y) + (nz * VolumeHalf.Z);
            float baseline = (float)Math.Pow(VolumeHalf.Z, 24);
            float spec = Clamp01(((float)Math.Pow(Math.Max(0f, half), 24) - baseline) / Math.Max(0.01f, 1f - baseline));
            c.Mix(Rgba.White, spec * gloss);
        }

        /// <summary>The outward unit gradient of a signed distance at (x, y), by central differences.</summary>
        private static (float X, float Y) Gradient(float x, float y, Func<float, float, float> distance)
        {
            float gx = distance(x + 0.75f, y) - distance(x - 0.75f, y);
            float gy = distance(x, y + 0.75f) - distance(x, y - 0.75f);
            float g = Length(gx, gy);
            return g < 1e-5f ? (0f, 0f) : (gx / g, gy / g);
        }

        private static (float X, float Y, float Z) Normalized(float x, float y, float z)
        {
            float n = (float)Math.Sqrt((x * x) + (y * y) + (z * z));
            return (x / n, y / n, z / n);
        }
    }
}
