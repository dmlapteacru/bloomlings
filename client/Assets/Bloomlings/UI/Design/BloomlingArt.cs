using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>A Bloomling's face (spec 003 FR-032; the spec 002 slot <c>char.face</c>).</summary>
    public enum BloomlingMood
    {
        /// <summary>Big sparkly eyes and a small smile: pods that can be taken, working slots, walkers, Home.</summary>
        Happy,

        /// <summary>Closed eyes: pods still waiting in their stack.</summary>
        Sleepy,

        /// <summary>Raised brows and a small frown: a stuck pod in a Waiting Slot.</summary>
        Worried,

        /// <summary>No eyes or mouth, for a cosmetic expression that draws the face.</summary>
        None,
    }

    /// <summary>
    /// One Bloomling picture (spec 003 FR-032, contracts/bloomling-look.md): its family, the variant color, the variant's
    /// icon id (its crest and belly badge), the face, and the white sticker halo of walkers on the board.
    /// </summary>
    public sealed record BloomlingLook(Family Family, Rgba Color, string? IconId = null, BloomlingMood Mood = BloomlingMood.Happy, bool Badge = true, bool Halo = false)
    {
        /// <summary>Whether the belly badge is drawn (it carries the variant symbol, which the host draws on it).</summary>
        public bool ShowsBadge => Badge && IconId != null;

        /// <summary>A cache key: the same key always renders the same picture.</summary>
        public string Key =>
            "bloomling/" + Family + "/" + (IconId ?? "-") + "/" + Color.Hex + "/" + Mood + (ShowsBadge ? "/badge" : string.Empty) + (Halo ? "/halo" : string.Empty);
    }

    /// <summary>
    /// The kawaii Bloomlings (spec 003 FR-032, style B of the character sheet the owner chose): one round body per family,
    /// head and body in one, with a soft pastel gradient, an outline in a darker shade of the body, a crest per variant
    /// (sprout leaves, petals, a bud, an acorn cap…), tiny feet, big sparkly eyes, blush, and a large white badge on the
    /// belly that carries the variant symbol (spec 001 FR-012: the symbol stays the first cue).
    /// <para>
    /// <see cref="Render"/> paints the whole figure into one RGBA picture, so the playtest and the Unity client draw the
    /// same pixels. The symbol is not part of the picture: hosts draw the <see cref="ShapeLibrary"/> symbol over
    /// <see cref="SymbolBox"/>, so it stays crisp and final icon art can replace it. Coordinates follow
    /// <see cref="ShapeRaster"/>: x and y in −1..1 of the unit square, y up; the design is drawn at <see cref="Fit"/> of
    /// it so the outline and the halo stay inside the margin. Engine-free.
    /// </para>
    /// </summary>
    public static class BloomlingArt
    {
        /// <summary>The share of the unit square the design is drawn at.</summary>
        public const float Fit = 0.94f;

        /// <summary>The belly badge in design units: its center height and radius.</summary>
        public const float BadgeY = -0.58f;

        public const float BadgeRadius = 0.3f;

        /// <summary>The symbol's radius as a share of the badge radius.</summary>
        public const float SymbolShare = 0.86f;

        /// <summary>How far below the picture's center the feet touch the ground, in design units (y up).</summary>
        public const float FeetY = -0.95f;

        /// <summary>The thinnest outline in design units; it never drops below 1.3 pixels.</summary>
        public const float OutlineWidth = 0.035f;

        private const float BodyY = -0.237f;
        private const float EyeX = 0.24f;
        private const float EyeY = -0.05f;
        private const float EyeRx = 0.13f;
        private const float EyeRy = 0.16f;
        private const float MouthY = -0.2f;
        private const float MouthRadius = 0.055f;
        private const float BlushX = 0.44f;
        private const float BlushY = -0.22f;
        private const float FootX = 0.2f;
        private const float FootY = -0.88f;

        /// <summary>The body color of a variant color: dark colors get a lighter, pastel body.</summary>
        public static Rgba BodyColor(Rgba color) => color.Luminance < 0.5 ? color.Lighten(0.18f) : color;

        /// <summary>The outline: a darker shade of the body, never black (spec 003 FR-008).</summary>
        public static Rgba LineColor(Rgba body) => (body.Luminance > 0.05 ? body.Darken(0.45f) : body.Darken(0.25f)).Mix(Rgba.White, 0.15f);

        /// <summary>The symbol color on the belly badge: the variant color, darkened until it reaches 3:1 on the badge.</summary>
        public static Rgba SymbolColor(Rgba color)
        {
            Rgba symbol = color.WithAlpha(1f);
            for (int i = 0; i < 20 && Rgba.Contrast(symbol, C.CharBadge) < 3.0; i++)
            {
                symbol = symbol.Darken(0.06f);
            }

            return symbol;
        }

        /// <summary>Where the host draws the variant symbol over a picture drawn into <paramref name="box"/> (y down).</summary>
        public static Box SymbolBox(Box box)
        {
            float cy = box.CenterY - (BadgeY * Fit / ShapeRaster.Margin * box.Height / 2f);
            float side = BadgeRadius * SymbolShare * Fit * box.Width;
            return Box.FromCenter(box.CenterX, cy, side, side);
        }

        /// <summary>The share of a pod or slot card face, from its bottom, that the count pill covers.</summary>
        public const float CardPillShare = 0.29f;

        /// <summary>
        /// The figure's picture box on a pod or slot card face (y down): as large as fits between the card's top and its
        /// count pill, so the pill never covers the badge (spec 001 FR-012: the symbol stays the first cue).
        /// </summary>
        public static Box OnCard(Box face)
        {
            // In a picture of side s the crest reaches this share of s above the center, and the badge this share below.
            float above = 0.95f * Fit / ShapeRaster.Margin / 2f;
            float below = (BadgeRadius - BadgeY + 0.02f) * Fit / ShapeRaster.Margin / 2f;
            float top = face.Top + (face.Height * 0.04f);
            float bottom = face.Bottom - (face.Height * (CardPillShare + 0.02f));
            float side = Math.Min(face.Width * 0.84f, (bottom - top) / (above + below));
            return Box.FromCenter(face.CenterX, top + (above * side), side, side);
        }

        /// <summary>The figure's place on a card face as anchors, 0..1 with y up (the Unity client's pods and slots).</summary>
        public static (float X0, float Y0, float X1, float Y1) OnCardAnchors
        {
            get
            {
                Box b = OnCard(new Box(0f, 0f, 1f, 1f));
                return (b.Left, 1f - b.Bottom, b.Right, 1f - b.Top);
            }
        }

        /// <summary>The symbol's place on a card face as anchors, 0..1 with y up: <see cref="SymbolAnchors"/> inside <see cref="OnCardAnchors"/>.</summary>
        public static (float X0, float Y0, float X1, float Y1) SymbolOnCardAnchors
        {
            get
            {
                (float x0, float y0, float x1, float y1) = OnCardAnchors;
                (float sx0, float sy0, float sx1, float sy1) = SymbolAnchors;
                return (x0 + (sx0 * (x1 - x0)), y0 + (sy0 * (y1 - y0)), x0 + (sx1 * (x1 - x0)), y0 + (sy1 * (y1 - y0)));
            }
        }

        /// <summary>The symbol's place as anchors of the picture's rectangle, 0..1 with y up (the Unity client).</summary>
        public static (float X0, float Y0, float X1, float Y1) SymbolAnchors
        {
            get
            {
                float cy = 0.5f + (BadgeY * Fit / ShapeRaster.Margin / 2f);
                float half = BadgeRadius * SymbolShare * Fit / 2f;
                return (0.5f - half, cy - half, 0.5f + half, cy + half);
            }
        }

        /// <summary>
        /// The figure's outer edge with its outline, in unit-square coordinates (the <c>char.*</c> shapes: skins, hit
        /// tests). Without an icon id it uses the family's own crest.
        /// </summary>
        public static Func<float, float, float> Silhouette(Family family, string? iconId = null)
        {
            List<Part> parts = Solids(family, iconId ?? DefaultIcon(family));
            return (x, y) =>
            {
                float dx = x / Fit;
                float dy = y / Fit;
                float d = float.MaxValue;
                foreach (Part part in parts)
                {
                    d = MathF.Min(d, part.Sdf(dx, dy));
                }

                return (d - OutlineWidth) * Fit;
            };
        }

        /// <summary>
        /// The picture as <paramref name="size"/> × <paramref name="size"/> RGBA bytes: rows from the top with
        /// <paramref name="topDown"/> (bitmaps), else from the bottom (Unity textures); premultiplied alpha for bitmaps,
        /// straight alpha for Unity.
        /// </summary>
        public static byte[] Render(BloomlingLook look, int size, bool topDown, bool premultiplied)
        {
            if (size < 8)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            float pixel = 2f * ShapeRaster.Margin / size / Fit;
            var buffer = new float[size * size * 4];
            foreach (Layer layer in Layers(look, pixel))
            {
                Paint(buffer, size, pixel, layer);
            }

            var bytes = new byte[size * size * 4];
            for (int row = 0; row < size; row++)
            {
                int from = topDown ? size - 1 - row : row;
                for (int col = 0; col < size; col++)
                {
                    int s = ((from * size) + col) * 4;
                    int t = ((row * size) + col) * 4;
                    float a = buffer[s + 3];
                    float k = premultiplied || a <= 0f ? 1f : 1f / a;
                    bytes[t] = ToByte(buffer[s] * k);
                    bytes[t + 1] = ToByte(buffer[s + 1] * k);
                    bytes[t + 2] = ToByte(buffer[s + 2] * k);
                    bytes[t + 3] = ToByte(a);
                }
            }

            return bytes;
        }

        /// <summary>The family's crest when no variant is given (Home, the Wardrobe).</summary>
        public static string DefaultIcon(Family family) => family switch
        {
            Family.Sprig => "leaf",
            Family.Bloom => "flower",
            Family.Drop => "drop",
            _ => "log",
        };

        // ---- The layers ----

        private static List<Layer> Layers(BloomlingLook look, float pixel)
        {
            Rgba body = BodyColor(look.Color);
            Rgba line = LineColor(body);
            float w = MathF.Max(OutlineWidth, 1.3f * pixel);
            float hairline = MathF.Max(0.02f, 0.6f * pixel);
            string icon = look.IconId ?? DefaultIcon(look.Family);
            var layers = new List<Layer>();

            if (look.Halo)
            {
                List<Part> solids = Solids(look.Family, icon);
                float halo = w + MathF.Max(0.06f, 2f * pixel);
                layers.Add(new Layer(Union(solids), halo, C.CharHalo));
            }

            // Behind the body: the crest, then the feet.
            foreach ((Part part, Rgba fill, Rgba edge, bool outlined) in Crest(look.Family, icon, body, line, w))
            {
                if (outlined)
                {
                    layers.Add(new Layer(part, w, edge));
                }

                layers.Add(new Layer(part, 0f, fill));
            }

            foreach (float side in new[] { -1f, 1f })
            {
                Part foot = EllipsePart(side * FootX, FootY, 0.14f, 0.085f, 0f);
                layers.Add(new Layer(foot, w, line));
                layers.Add(new Layer(foot, 0f, body.Darken(0.15f)));
            }

            // The body: outline, then the soft radial gradient (light upper left, darker lower right).
            Part shape = Body(look.Family);
            Rgba light = body.Lighten(0.45f);
            Rgba dark = body.Darken(0.12f);
            layers.Add(new Layer(shape, w, line));
            layers.Add(new Layer(shape, 0f, body, paint: (x, y) =>
            {
                float t = ShapeLibrary.Length(x + 0.17f, y - (BodyY + 0.27f)) / 1.0f;
                return t < 0.55f ? light.Mix(body, t / 0.55f) : body.Mix(dark, MathF.Min(1f, (t - 0.55f) / 0.45f));
            }));

            // In front: the Dew sparkle and the Mist cloud.
            foreach (Part accessory in Accessories(icon))
            {
                layers.Add(new Layer(accessory, w, line));
                layers.Add(new Layer(accessory, 0f, C.CharSparkle));
            }

            // The face.
            foreach (float side in new[] { -1f, 1f })
            {
                layers.Add(new Layer(EllipsePart(side * BlushX, BlushY, 0.12f, 0.075f, 0f), 0f, C.CharBlush, 0.55f));
            }

            layers.AddRange(Face(look.Mood, hairline));

            if (look.ShowsBadge)
            {
                Part badge = CirclePart(0f, BadgeY, BadgeRadius);
                layers.Add(new Layer(badge, w * 0.8f, line, 0.45f));
                layers.Add(new Layer(badge, 0f, C.CharBadge));
            }

            return layers;
        }

        private static IEnumerable<Layer> Face(BloomlingMood mood, float hairline)
        {
            if (mood == BloomlingMood.None)
            {
                yield break;
            }

            foreach (float side in new[] { -1f, 1f })
            {
                float ex = side * EyeX;
                if (mood == BloomlingMood.Sleepy)
                {
                    // Closed eyes: a gentle lower arc.
                    float cy = EyeY + 0.04f;
                    yield return new Layer(new Part((x, y) => MathF.Max(MathF.Abs(ShapeLibrary.Length(x - ex, y - cy) - 0.1f) - MathF.Max(0.026f, hairline), y - (cy - 0.045f)), ex - 0.14f, cy - 0.14f, ex + 0.14f, cy), 0f, C.CharInk);
                    continue;
                }

                yield return new Layer(EllipsePart(ex, EyeY, EyeRx, EyeRy, 0f), 0f, C.CharInk);
                float big = mood == BloomlingMood.Worried ? 0.04f : 0.055f;
                yield return new Layer(CirclePart(ex + 0.045f, EyeY + 0.065f, big), 0f, C.CharSparkle);
                yield return new Layer(CirclePart(ex - 0.045f, EyeY - 0.075f, 0.026f), 0f, C.CharSparkle);
                if (mood == BloomlingMood.Worried)
                {
                    // Brows raised at the inner ends.
                    float inner = side * 0.14f;
                    float outer = side * 0.33f;
                    yield return new Layer(SegmentPart(inner, EyeY + 0.27f, outer, EyeY + 0.21f, MathF.Max(0.024f, hairline)), 0f, C.CharInk);
                }
            }

            if (mood == BloomlingMood.Worried)
            {
                float cy = MouthY - 0.075f;
                yield return new Layer(new Part((x, y) => MathF.Max(MathF.Abs(ShapeLibrary.Length(x, y - cy) - MouthRadius) - MathF.Max(0.02f, hairline), (cy + 0.015f) - y), -0.09f, cy, 0.09f, cy + 0.09f), 0f, C.CharInk);
            }
            else
            {
                yield return new Layer(new Part((x, y) => MathF.Max(MathF.Abs(ShapeLibrary.Length(x, y - MouthY) - MouthRadius) - MathF.Max(0.02f, hairline), y - (MouthY - 0.01f)), -0.09f, MouthY - 0.09f, 0.09f, MouthY), 0f, C.CharInk);
            }
        }

        /// <summary>The crest behind the body, back to front: (shape, fill, outline color, outlined).</summary>
        private static List<(Part Part, Rgba Fill, Rgba Line, bool Outlined)> Crest(Family family, string icon, Rgba body, Rgba line, float w)
        {
            var crest = new List<(Part, Rgba, Rgba, bool)>();
            float stem = MathF.Max(0.03f, w * 0.85f);
            switch (family)
            {
                case Family.Sprig:
                    // A sprout: a short stem and two round leaves.
                    crest.Add((SegmentPart(0f, 0.36f, 0f, 0.64f, stem), line, line, false));
                    crest.Add((EllipsePart(-0.2f, 0.7f, 0.2f, 0.14f, -25f), body.Lighten(0.3f), line, true));
                    crest.Add((EllipsePart(0.22f, 0.74f, 0.22f, 0.15f, 25f), body.Lighten(0.3f), line, true));
                    break;
                case Family.Bloom when icon == "bud":
                    // A closed bud.
                    crest.Add((DropletPart(0f, 0.5f, 0.19f, 0.88f), body.Lighten(0.25f), line, true));
                    break;
                case Family.Bloom when icon == "berry":
                    // A berry's green calyx and stalk.
                    crest.Add((SegmentPart(0f, 0.43f, 0.04f, 0.68f, stem), C.GardenLeafLine, C.GardenLeafLine, false));
                    foreach (float angle in new[] { 150f, 30f, 110f, 70f })
                    {
                        float a = angle * MathF.PI / 180f;
                        crest.Add((EllipsePart(0.14f * MathF.Cos(a), 0.43f + (0.14f * MathF.Sin(a)), 0.15f, 0.065f, angle), C.GardenLeaf2, C.GardenLeafLine, true));
                    }

                    break;
                case Family.Bloom:
                    // A crown of petals: the top one first, the side ones in front.
                    var petals = new (float X, float Y)[] { (0f, 0.639f), (-0.247f, 0.561f), (0.247f, 0.561f), (-0.35f, 0.371f), (0.35f, 0.371f) };
                    foreach ((float x, float y) in petals)
                    {
                        crest.Add((CirclePart(x, y, 0.175f), body.Lighten(0.35f), line, true));
                    }

                    break;
                case Family.Twig when icon == "acorn":
                    // An acorn cap with a stalk.
                    crest.Add((SegmentPart(0f, 0.62f, 0.045f, 0.78f, MathF.Max(0.032f, stem)), C.CharCapLine, C.CharCapLine, false));
                    crest.Add((new Part((x, y) => MathF.Max(Ellipse(x, y, 0f, 0.2f, 0.56f, 0.47f), 0.2f - y), -0.56f, 0.2f, 0.56f, 0.67f), C.CharCap, C.CharCapLine, true));
                    break;
                case Family.Twig:
                    // A stump with a sprout growing from its top.
                    crest.Add((new Part((x, y) => MathF.Min(ShapeLibrary.Segment(x, y, 0.1f, 0.38f, 0.22f, 0.56f), ShapeLibrary.Segment(x, y, 0.22f, 0.56f, 0.34f, 0.68f)) - stem, 0.06f, 0.34f, 0.38f, 0.72f), line, line, false));
                    crest.Add((EllipsePart(0.44f, 0.75f, 0.14f, 0.085f, 30f), C.GardenLeaf2, C.GardenLeafLine, true));
                    break;
            }

            return crest;
        }

        /// <summary>Small accessories in front of the body.</summary>
        private static IEnumerable<Part> Accessories(string icon)
        {
            if (icon == "dew")
            {
                // A four-point sparkle.
                const float cx = 0.56f;
                const float cy = 0.6f;
                const float s = 0.13f;
                yield return new Part((x, y) => (Sq(MathF.Sqrt(MathF.Abs((x - cx) / s)) + MathF.Sqrt(MathF.Abs((y - cy) / s))) - 1f) * 0.5f * s, cx - s, cy - s, cx + s, cy + s);
            }
            else if (icon == "mist")
            {
                // A small cloud.
                yield return new Part((x, y) => MathF.Min(MathF.Min(ShapeLibrary.Length(x - 0.44f, y - 0.55f) - 0.09f, ShapeLibrary.Length(x - 0.56f, y - 0.6f) - 0.11f), ShapeLibrary.Length(x - 0.68f, y - 0.54f) - 0.085f), 0.35f, 0.45f, 0.77f, 0.71f);
            }
        }

        /// <summary>Every solid part (crest, feet, body, accessories), for the halo and the silhouette.</summary>
        private static List<Part> Solids(Family family, string icon)
        {
            var parts = new List<Part> { Body(family), EllipsePart(-FootX, FootY, 0.14f, 0.085f, 0f), EllipsePart(FootX, FootY, 0.14f, 0.085f, 0f) };
            foreach ((Part part, _, _, _) in Crest(family, icon, Rgba.White, Rgba.Black, OutlineWidth))
            {
                parts.Add(part);
            }

            parts.AddRange(Accessories(icon));
            return parts;
        }

        private static Part Body(Family family) => family switch
        {
            // An egg, a little taller than wide.
            Family.Sprig => EllipsePart(0f, BodyY + 0.01f, 0.65f, 0.68f, 0f),
            // A round bulb.
            Family.Bloom => EllipsePart(0f, BodyY, 0.7f, 0.67f, 0f),
            // A droplet.
            Family.Drop => DropletPart(0f, BodyY, 0.67f, 0.8f),
            // A stump.
            _ => new Part((x, y) => ShapeLibrary.RoundedBox(x, y, 0f, -0.247f, 0.6f, 0.66f, 0.24f), -0.6f, -0.907f, 0.6f, 0.413f),
        };

        // ---- Painting ----

        private static void Paint(float[] buffer, int size, float pixel, Layer layer)
        {
            Part part = layer.Part;
            float grow = layer.Grow + pixel;
            int c0 = Math.Max(0, ToIndex(part.X0 - grow, size));
            int c1 = Math.Min(size - 1, ToIndex(part.X1 + grow, size) + 1);
            int r0 = Math.Max(0, ToIndex(part.Y0 - grow, size));
            int r1 = Math.Min(size - 1, ToIndex(part.Y1 + grow, size) + 1);
            float step = 2f * ShapeRaster.Margin / size / Fit;
            for (int row = r0; row <= r1; row++)
            {
                float y = (((row + 0.5f) * step) - (ShapeRaster.Margin / Fit));
                for (int col = c0; col <= c1; col++)
                {
                    float x = (((col + 0.5f) * step) - (ShapeRaster.Margin / Fit));
                    float d = part.Sdf(x, y) - layer.Grow;
                    float coverage = MathF.Max(0f, MathF.Min(1f, 0.5f - (d / pixel)));
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    Rgba color = layer.Paint != null ? layer.Paint(x, y) : layer.Color;
                    float a = coverage * layer.Alpha * (color.A / 255f);
                    int i = ((row * size) + col) * 4;
                    float keep = 1f - a;
                    buffer[i] = (color.R / 255f * a) + (buffer[i] * keep);
                    buffer[i + 1] = (color.G / 255f * a) + (buffer[i + 1] * keep);
                    buffer[i + 2] = (color.B / 255f * a) + (buffer[i + 2] * keep);
                    buffer[i + 3] = a + (buffer[i + 3] * keep);
                }
            }
        }

        /// <summary>The pixel column (or row from the bottom) of a design coordinate.</summary>
        private static int ToIndex(float design, int size) => (int)MathF.Floor((((design * Fit / ShapeRaster.Margin) + 1f) * size / 2f) - 0.5f);

        private static byte ToByte(float v) => (byte)MathF.Round(MathF.Max(0f, MathF.Min(1f, v)) * 255f);

        // ---- Shapes (signed distances in design units) ----

        private static Part CirclePart(float cx, float cy, float r) =>
            new Part((x, y) => ShapeLibrary.Length(x - cx, y - cy) - r, cx - r, cy - r, cx + r, cy + r);

        private static Part SegmentPart(float ax, float ay, float bx, float by, float half) =>
            new Part((x, y) => ShapeLibrary.Segment(x, y, ax, ay, bx, by) - half, MathF.Min(ax, bx) - half, MathF.Min(ay, by) - half, MathF.Max(ax, bx) + half, MathF.Max(ay, by) + half);

        /// <summary>An ellipse whose long axis is turned <paramref name="degrees"/> counterclockwise.</summary>
        private static Part EllipsePart(float cx, float cy, float rx, float ry, float degrees)
        {
            float a = degrees * MathF.PI / 180f;
            float cos = MathF.Cos(a);
            float sin = MathF.Sin(a);
            float r = MathF.Max(rx, ry);
            return new Part(
                (x, y) =>
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    return Ellipse((dx * cos) + (dy * sin), (-dx * sin) + (dy * cos), 0f, 0f, rx, ry);
                },
                cx - r,
                cy - r,
                cx + r,
                cy + r);
        }

        /// <summary>A circle with a pointed top at <paramref name="tipY"/>; the two edges are tangent to the circle.</summary>
        private static Part DropletPart(float cx, float cy, float r, float tipY)
        {
            float sin = r / (tipY - cy);
            float cos = MathF.Sqrt(1f - (sin * sin));
            float tangentY = cy + (r * sin);
            return new Part(
                (x, y) => MathF.Min(ShapeLibrary.Length(x - cx, y - cy) - r, MathF.Max((MathF.Abs(x - cx) * cos) + ((y - tipY) * sin), tangentY - y)),
                cx - r,
                cy - r,
                cx + r,
                tipY);
        }

        /// <summary>The distance to an ellipse, first order: the implicit value over its gradient.</summary>
        private static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = x - cx;
            float dy = y - cy;
            float u = dx / rx;
            float v = dy / ry;
            float l = MathF.Sqrt((u * u) + (v * v));
            float gx = dx / (rx * rx);
            float gy = dy / (ry * ry);
            float g = MathF.Sqrt((gx * gx) + (gy * gy));
            return g < 1e-6f ? -MathF.Min(rx, ry) : (l - 1f) * l / g;
        }

        private static Part Union(List<Part> parts)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (Part p in parts)
            {
                x0 = MathF.Min(x0, p.X0);
                y0 = MathF.Min(y0, p.Y0);
                x1 = MathF.Max(x1, p.X1);
                y1 = MathF.Max(y1, p.Y1);
            }

            return new Part(
                (x, y) =>
                {
                    float d = float.MaxValue;
                    foreach (Part p in parts)
                    {
                        d = MathF.Min(d, p.Sdf(x, y));
                    }

                    return d;
                },
                x0,
                y0,
                x1,
                y1);
        }

        private static float Sq(float v) => v * v;

        /// <summary>A shape and its bounds in design units (the painter only visits pixels near the bounds).</summary>
        private readonly struct Part
        {
            public Part(Func<float, float, float> sdf, float x0, float y0, float x1, float y1)
            {
                Sdf = sdf;
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
            }

            public Func<float, float, float> Sdf { get; }

            public float X0 { get; }

            public float Y0 { get; }

            public float X1 { get; }

            public float Y1 { get; }
        }

        /// <summary>A part painted in a color (or a paint function), grown by <see cref="Grow"/> for outlines and the halo.</summary>
        private readonly struct Layer
        {
            public Layer(Part part, float grow, Rgba color, float alpha = 1f, Func<float, float, Rgba>? paint = null)
            {
                Part = part;
                Grow = grow;
                Color = color;
                Alpha = alpha;
                Paint = paint;
            }

            public Part Part { get; }

            public float Grow { get; }

            public Rgba Color { get; }

            public float Alpha { get; }

            public Func<float, float, Rgba>? Paint { get; }
        }
    }
}
