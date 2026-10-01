using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using SkiaSharp;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// The 2D variant characters of spec 004 (contracts/character-look.md "2D characters"): each variant's whole shape is
    /// its symbol, drawn on a 100 × 100 design square with a soft radial gradient, an outline in a darker shade of the
    /// body (never black), a highlight and a simple face, in four moods. Ported from the approved concept
    /// (<c>concept-gameplay-2d.jpg</c>).
    /// </summary>
    public static class Characters2D
    {
        private const float Outline = 3.4f;

        /// <summary>Renders one character picture (<see cref="CharacterArt.Size2D"/> square, transparent background).</summary>
        public static SKBitmap Render(VariantInfo variant, CharacterMood mood)
        {
            int size = CharacterArt.Size2D;
            var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            float margin = size * CharacterArt.Margin2D;
            canvas.Translate(margin, margin);
            canvas.Scale((size - (2f * margin)) / 100f);
            var look = new Look(variant, mood);
            Draw(canvas, variant.IconId, look);
            (float fx, float fy) = CharacterArt.FaceDesign2D(variant.IconId);
            Face(canvas, fx, fy, mood);
            canvas.Flush();
            return bitmap;
        }

        private static void Draw(SKCanvas c, string icon, Look k)
        {
            switch (icon)
            {
                case "leaf":
                {
                    // A leaf blade whose tip leans up and to the right (a drop is upright and symmetric), with a stalk.
                    Stroke(c, Path("M30,86 Q22,94 13,93"), k.Line, 4.6f);
                    Stroke(c, Path("M30,86 Q22,94 13,93"), k.Base.Darken(0.12f), 2f);
                    Body(c, k, Path("M78,7 C82,30 86,50 81,67 C76,85 63,94 47,94 C29,94 17,82 18,64 C19,40 42,20 78,7 Z"));
                    Stroke(c, Path("M60,40 Q68,24 76,12"), k.Base.Lighten(0.3f).WithAlpha(0.8f), 2.2f);
                    Stroke(c, Path("M66,28 L58,24 M70,20 L64,15"), k.Base.Lighten(0.3f).WithAlpha(0.6f), 1.6f);
                    Shine(c, 33, 50, 6, 11, 25);
                    break;
                }

                case "moss":
                {
                    (float X, float Y, float R)[] circles = { (50, 60, 31), (27, 54, 16), (73, 54, 16), (36, 36, 15), (64, 36, 15), (50, 29, 15) };
                    using SKPath cushion = Union(circles);
                    Body(c, k, cushion);
                    foreach ((float x, float y, float r) in new[] { (36f, 36f, 9f), (64f, 36f, 9f), (50f, 29f, 9f) })
                    {
                        Oval(c, x - 2, y - 3, r * 0.55f, r * 0.55f, 0, Rgba.White.WithAlpha(0.18f));
                    }

                    Stroke(c, Path("M50,15 L50,9"), k.Base.Darken(0.3f), 2.4f);
                    OvalOutlined(c, 45, 8, 5, 3, 25, k.Base.Lighten(0.35f), k.Line, 1.8f);
                    OvalOutlined(c, 55, 8, 5, 3, -25, k.Base.Lighten(0.35f), k.Line, 1.8f);
                    break;
                }

                case "flower":
                {
                    var parts = new (float X, float Y, float R)[6];
                    for (int i = 0; i < 5; i++)
                    {
                        double a = (-Math.PI / 2) + (i * 2 * Math.PI / 5);
                        parts[i] = (50f + (25f * (float)Math.Cos(a)), 52f + (25f * (float)Math.Sin(a)), 19.5f);
                    }

                    parts[5] = (50, 52, 22);
                    using SKPath flower = Union(parts);
                    Body(c, k, flower);
                    for (int i = 0; i < 5; i++)
                    {
                        double a = (-Math.PI / 2) + (Math.PI / 5) + (i * 2 * Math.PI / 5);
                        var gap = new SKPath();
                        gap.MoveTo(50f + (16f * (float)Math.Cos(a)), 52f + (16f * (float)Math.Sin(a)));
                        gap.LineTo(50f + (31f * (float)Math.Cos(a)), 52f + (31f * (float)Math.Sin(a)));
                        Stroke(c, gap, k.Base.Darken(0.25f).WithAlpha(0.45f), 2f);
                    }

                    Oval(c, 50, 54, 17, 17, 0, k.Base.Lighten(0.45f).WithAlpha(0.75f));
                    Shine(c, 36, 30, 5, 8);
                    break;
                }

                case "bud":
                {
                    Rgba sepal = k.Tone(Palette.SepalGreen.Mix(k.Raw, 0.15f));
                    Fill(c, Path("M30,78 C22,74 18,64 22,58 C30,62 36,70 38,80 Z"), sepal, sepal.Darken(0.45f), 2.6f);
                    Fill(c, Path("M70,78 C78,74 82,64 78,58 C70,62 64,70 62,80 Z"), sepal, sepal.Darken(0.45f), 2.6f);
                    Body(c, k, Path("M24,54 C24,30 30,22 31,18 C38,24 43,30 45,36 C47,26 49,16 50,10 C51,16 53,26 55,36 C57,30 62,24 69,18 C70,22 76,30 76,54 C76,76 64,90 50,90 C36,90 24,76 24,54 Z"));
                    Stroke(c, Path("M45,36 C44,46 44,52 46,58"), k.Base.Darken(0.2f).WithAlpha(0.5f), 2f);
                    Stroke(c, Path("M55,36 C56,46 56,52 54,58"), k.Base.Darken(0.2f).WithAlpha(0.5f), 2f);
                    Shine(c, 35, 44, 5, 9);
                    break;
                }

                case "drop":
                    Body(c, k, Path("M50,7 C56,22 81,42 81,64 C81,82 67,94 50,94 C33,94 19,82 19,64 C19,42 44,22 50,7 Z"));
                    Stroke(c, Path("M50,15 C54,26 74,44 75,62"), Rgba.White.WithAlpha(0.25f), 3f);
                    Shine(c, 35, 50, 7, 13, -20, 0.6f);
                    Oval(c, 34, 70, 2.6f, 2.6f, 0, Rgba.White.WithAlpha(0.6f));
                    break;

                case "dew":
                {
                    using SKPath round = new SKPath();
                    round.AddCircle(50, 57, 33);
                    Body(c, k, round);
                    using SKPath ring = new SKPath();
                    ring.AddCircle(50, 57, 27);
                    Stroke(c, ring, Rgba.White.WithAlpha(0.28f), 2.5f);
                    Shine(c, 36, 42, 8, 11, -35, 0.65f);
                    Sparkle(c, 82, 22, 9, k.Tone(Rgba.White), k.Line);
                    Sparkle(c, 70, 10, 4.5f, k.Tone(Rgba.White), k.Line);
                    break;
                }

                case "log":
                {
                    Rgba top = k.Base.Mix(Palette.WoodTop, 0.6f);
                    RoundRect(c, 12, 48, 12, 16, 6, k.Base.Darken(0.05f), k.Line, 3f);
                    RoundRect(c, 76, 48, 12, 16, 6, k.Base.Darken(0.05f), k.Line, 3f);
                    Body(c, k, Path("M22,30 L22,82 C22,90 36,94 50,94 C64,94 78,90 78,82 L78,30 Z"));
                    foreach (float x in new[] { 31f, 69f })
                    {
                        Stroke(c, Path($"M{x},40 C{x - 2},56 {x + 2},70 {x},86"), k.Base.Darken(0.3f).WithAlpha(0.45f), 2.2f);
                    }

                    OvalOutlined(c, 50, 30, 28, 10, 0, top, k.Line, Outline);
                    OvalStroke(c, 50, 30, 18, 6, top.Darken(0.25f), 1.8f);
                    OvalStroke(c, 50, 30, 8, 2.8f, top.Darken(0.25f), 1.8f);
                    Rgba sprout = k.Tone(Palette.Sprout);
                    Stroke(c, Path("M64,26 Q66,16 70,12"), sprout.Darken(0.4f), 2.4f);
                    OvalOutlined(c, 75, 11, 7, 4, -25, sprout, sprout.Darken(0.45f), 2f);
                    break;
                }

                case "acorn":
                {
                    Rgba cap = k.Tone(k.Raw.Mix(Palette.CapBrown, 0.55f));
                    Body(c, k, Path("M28,50 C28,76 38,93 50,93 C62,93 72,76 72,50 Z"));
                    Shine(c, 38, 64, 4.5f, 9, -15);
                    Stroke(c, Path("M50,24 C52,16 55,11 58,8"), cap.Darken(0.3f), 4f);
                    using SKPath capPath = Path("M20,52 C20,32 34,22 50,22 C66,22 80,32 80,52 C66,58 34,58 20,52 Z");
                    Fill(c, capPath, cap, cap.Darken(0.4f), Outline);
                    Hatch(c, Path("M22,50 C22,33 35,25 50,25 C65,25 78,33 78,50 C64,55 36,55 22,50 Z"), cap.Darken(0.3f).WithAlpha(0.55f));
                    Shine(c, 38, 32, 6, 3.5f, -10, 0.35f);
                    break;
                }

                case "vine":
                {
                    Rgba leaf = k.Tone(Palette.SproutGreen.Mix(k.Raw, 0.3f));
                    Body(c, k, Path("M50,34 C70,34 80,48 80,64 C80,82 67,92 50,92 C33,92 20,82 20,64 C20,48 30,34 50,34 Z"));
                    Stroke(c, Path("M52,35 C52,24 58,16 67,15 C75,14 80,21 76,27 C73,32 66,30 67,25"), k.Line, 3.2f);
                    OvalOutlined(c, 41, 27, 8, 4.5f, 35, leaf, leaf.Darken(0.45f), 2f);
                    OvalOutlined(c, 82, 34, 6, 3.5f, -30, leaf, leaf.Darken(0.45f), 2f);
                    Shine(c, 36, 50, 6, 10);
                    break;
                }

                case "berry":
                {
                    Rgba calyx = k.Tone(Palette.SepalGreen);
                    (float X, float Y, float R)[] berries = { (36, 64, 21), (64, 64, 21), (50, 46, 22) };
                    using SKPath cluster = Union(berries);
                    Body(c, k, cluster);
                    foreach ((float x, float y) in new[] { (30f, 72f), (40f, 78f), (66f, 74f), (60f, 80f), (44f, 38f), (58f, 40f) })
                    {
                        Oval(c, x, y, 1.6f, 2.4f, 0, k.Base.Lighten(0.55f).WithAlpha(0.7f));
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        float a = -90f + ((i - 2) * 32f);
                        double r = a * Math.PI / 180;
                        OvalOutlined(c, 50f + (9f * (float)Math.Cos(r)), 24f + (9f * (float)Math.Sin(r)), 8, 3.5f, a, calyx, calyx.Darken(0.45f), 1.8f);
                    }

                    Shine(c, 40, 40, 5, 8);
                    break;
                }

                case "mist":
                {
                    (float X, float Y, float R)[] puffs = { (30, 64, 17), (50, 50, 23), (70, 62, 18), (50, 70, 20), (38, 52, 15), (64, 50, 14) };
                    using SKPath cloud = Union(puffs);
                    Body(c, k, cloud);
                    Stroke(c, Path("M30,90 Q40,86 50,90 T70,90"), k.Line.WithAlpha(0.6f), 2.4f);
                    Shine(c, 40, 40, 6, 9, -40);
                    break;
                }

                default:
                {
                    // bark: a chunky slab with a jagged top, grooves and a lichen spot
                    Body(c, k, Path("M20,36 L30,26 L40,33 L50,24 L60,33 L70,26 L80,36 L80,84 C80,90 74,93 66,93 L34,93 C26,93 20,90 20,84 Z"));
                    foreach (float x in new[] { 30f, 50f, 70f })
                    {
                        Stroke(c, Path($"M{x},40 C{x - 2},58 {x + 2},72 {x},88"), k.Base.Darken(0.3f).WithAlpha(0.45f), 2.2f);
                    }

                    Rgba lichen = k.Tone(Palette.Lichen);
                    Oval(c, 70, 78, 6, 4, 0, lichen);
                    Oval(c, 75, 74, 3.5f, 2.5f, 0, lichen);
                    Shine(c, 34, 46, 5, 9);
                    break;
                }
            }
        }

        // ---- The face ----

        private static void Face(SKCanvas c, float cx, float cy, CharacterMood mood)
        {
            Oval(c, cx - 15, cy + 6, 5.2f, 3.2f, 0, Palette.Blush.WithAlpha(0.42f));
            Oval(c, cx + 15, cy + 6, 5.2f, 3.2f, 0, Palette.Blush.WithAlpha(0.42f));
            if (mood == CharacterMood.Blank)
            {
                return;
            }

            foreach (float x in new[] { cx - 9, cx + 9 })
            {
                if (mood == CharacterMood.Asleep)
                {
                    var arc = new SKPath();
                    arc.MoveTo(x - 4, cy - 0.5f);
                    arc.QuadTo(x, cy + 3.5f, x + 4, cy - 0.5f);
                    Stroke(c, arc, Palette.Ink, 2.2f);
                }
                else
                {
                    Oval(c, x, cy, 3.3f, 4.3f, 0, Palette.Ink);
                    Oval(c, x + 1.1f, cy - 1.6f, 1.25f, 1.25f, 0, Rgba.White);
                }
            }

            var mouth = new SKPath();
            if (mood == CharacterMood.Worried)
            {
                mouth.MoveTo(cx - 4, cy + 9);
                mouth.QuadTo(cx, cy + 6, cx + 4, cy + 9);
            }
            else
            {
                mouth.MoveTo(cx - 4.5f, cy + 6);
                mouth.QuadTo(cx, cy + 10.5f, cx + 4.5f, cy + 6);
            }

            Stroke(c, mouth, Palette.Ink, 2.2f);
        }

        // ---- Drawing helpers (design units) ----

        /// <summary>The body: its outline, then the radial gradient of the look (center 38%/30% of its bounds, radius 78%).</summary>
        private static void Body(SKCanvas c, Look k, SKPath path)
        {
            using (var line = Paint(k.Line))
            {
                line.Style = SKPaintStyle.Stroke;
                line.StrokeWidth = Outline;
                line.StrokeJoin = SKStrokeJoin.Round;
                c.DrawPath(path, line);
            }

            SKRect b = path.Bounds;
            var colors = new[] { Sk(k.Base.Lighten(0.42f)), Sk(k.Base), Sk(k.Base.Darken(0.14f)) };
            SKMatrix local = SKMatrix.CreateScale(b.Width, b.Height).PostConcat(SKMatrix.CreateTranslation(b.Left, b.Top));
            using var shader = SKShader.CreateRadialGradient(new SKPoint(0.38f, 0.3f), 0.78f, colors, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp, local);
            using var fill = new SKPaint { IsAntialias = true, Shader = shader, Style = SKPaintStyle.Fill };
            c.DrawPath(path, fill);
        }

        private static SKPath Union((float X, float Y, float R)[] circles)
        {
            SKPath result = new SKPath();
            foreach ((float x, float y, float r) in circles)
            {
                using var circle = new SKPath();
                circle.AddCircle(x, y, r);
                using SKPath merged = result.Op(circle, SKPathOp.Union) ?? result;
                result.Dispose();
                result = new SKPath(merged);
            }

            return result;
        }

        private static void Fill(SKCanvas c, SKPath path, Rgba fill, Rgba line, float width)
        {
            using (var p = Paint(line))
            {
                p.Style = SKPaintStyle.Stroke;
                p.StrokeWidth = width;
                p.StrokeJoin = SKStrokeJoin.Round;
                c.DrawPath(path, p);
            }

            using var f = Paint(fill);
            c.DrawPath(path, f);
        }

        private static void Stroke(SKCanvas c, SKPath path, Rgba color, float width)
        {
            using var p = Paint(color);
            p.Style = SKPaintStyle.Stroke;
            p.StrokeWidth = width;
            p.StrokeCap = SKStrokeCap.Round;
            p.StrokeJoin = SKStrokeJoin.Round;
            c.DrawPath(path, p);
        }

        private static void Oval(SKCanvas c, float cx, float cy, float rx, float ry, float degrees, Rgba color)
        {
            using var p = Paint(color);
            c.Save();
            c.RotateDegrees(degrees, cx, cy);
            c.DrawOval(cx, cy, rx, ry, p);
            c.Restore();
        }

        private static void OvalOutlined(SKCanvas c, float cx, float cy, float rx, float ry, float degrees, Rgba fill, Rgba line, float width)
        {
            c.Save();
            c.RotateDegrees(degrees, cx, cy);
            using (var f = Paint(fill))
            {
                c.DrawOval(cx, cy, rx, ry, f);
            }

            using (var s = Paint(line))
            {
                s.Style = SKPaintStyle.Stroke;
                s.StrokeWidth = width;
                c.DrawOval(cx, cy, rx, ry, s);
            }

            c.Restore();
        }

        private static void OvalStroke(SKCanvas c, float cx, float cy, float rx, float ry, Rgba line, float width)
        {
            using var s = Paint(line);
            s.Style = SKPaintStyle.Stroke;
            s.StrokeWidth = width;
            c.DrawOval(cx, cy, rx, ry, s);
        }

        private static void RoundRect(SKCanvas c, float x, float y, float w, float h, float r, Rgba fill, Rgba line, float width)
        {
            var rect = new SKRect(x, y, x + w, y + h);
            using (var f = Paint(fill))
            {
                c.DrawRoundRect(rect, r, r, f);
            }

            using var s = Paint(line);
            s.Style = SKPaintStyle.Stroke;
            s.StrokeWidth = width;
            c.DrawRoundRect(rect, r, r, s);
        }

        private static void Shine(SKCanvas c, float x, float y, float rx, float ry, float degrees = -30f, float opacity = 0.55f) =>
            Oval(c, x, y, rx, ry, degrees, Rgba.White.WithAlpha(opacity));

        /// <summary>A four-point sparkle with curved sides.</summary>
        private static void Sparkle(SKCanvas c, float x, float y, float r, Rgba fill, Rgba line)
        {
            float k = r * 0.18f;
            var star = new SKPath();
            star.MoveTo(x, y - r);
            star.QuadTo(x + k, y - k, x + r, y);
            star.QuadTo(x + k, y + k, x, y + r);
            star.QuadTo(x - k, y + k, x - r, y);
            star.QuadTo(x - k, y - k, x, y - r);
            star.Close();
            Fill(c, star, fill, line, 1.6f);
        }

        /// <summary>Cross-hatching clipped to a shape (the acorn cap).</summary>
        private static void Hatch(SKCanvas c, SKPath clip, Rgba color)
        {
            c.Save();
            c.ClipPath(clip, SKClipOperation.Intersect, true);
            using var p = Paint(color);
            p.Style = SKPaintStyle.Stroke;
            p.StrokeWidth = 1.6f;
            for (float t = -100; t <= 200; t += 7f * 1.41421356f)
            {
                c.DrawLine(t, 0, t + 100, 100, p);
                c.DrawLine(t, 100, t + 100, 0, p);
            }

            c.Restore();
            clip.Dispose();
        }

        private static SKPath Path(string svg) => SKPath.ParseSvgPathData(svg) ?? throw new ArgumentException("Bad path: " + svg);

        private static SKPaint Paint(Rgba color) => new SKPaint { IsAntialias = true, Color = Sk(color), Style = SKPaintStyle.Fill };

        internal static SKColor Sk(Rgba c) => new SKColor(c.R, c.G, c.B, c.A);

        /// <summary>The colors of one character in one mood.</summary>
        private sealed class Look
        {
            public Look(VariantInfo variant, CharacterMood mood)
            {
                Mood = mood;
                Raw = Palette.BodyColor(variant.IconId, Rgba.FromHex(variant.ColorHex));
                Base = Tone(Raw);
                Line = Base.Darken(0.38f);
            }

            public CharacterMood Mood { get; }

            /// <summary>The happy body color.</summary>
            public Rgba Raw { get; }

            /// <summary>The body color in this mood.</summary>
            public Rgba Base { get; }

            public Rgba Line { get; }

            /// <summary>A color in this mood: muted while asleep, greyed while worried (contracts/character-look.md).</summary>
            public Rgba Tone(Rgba color) => Mood switch
            {
                CharacterMood.Asleep => color.Mix(Palette.Muted, 0.45f),
                CharacterMood.Worried => color.Grey().Mix(DesignTokens.Colors.StateStuck, 0.35f),
                _ => color,
            };
        }
    }
}
