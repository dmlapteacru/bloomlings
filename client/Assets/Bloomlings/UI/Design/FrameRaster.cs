using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <content>
    /// The five drawn profile frames (spec 005 FR-037 as amended 2026-10-06, contracts/look.md §6.11): square pictures
    /// laid in place of the avatar's wood rim (<see cref="AvatarLook.Frame"/>), each a band over the framed disc's
    /// rounded-square edge (<see cref="AvatarLook.FrameEdge"/> from the middle, its corners
    /// <see cref="AvatarLook.FrameCorner"/>, the owner's rounded square of 2026-10-06; they were rings) that casts a soft shadow a little lower, lit from the upper left like the rest of the
    /// reference look. A band's place across it is <see cref="FrameDist"/> (a ring's distance from the middle) and its
    /// place along it <see cref="FrameAlong"/> (a ring's angle), so each frame keeps the recipe it had as a ring. Like the other material pictures they are straight-alpha RGBA rows from the
    /// top, anti-aliased over one pixel and the same for the same arguments.
    /// </content>
    public static partial class UiRaster
    {
        // The light of the frames' bands (a unit vector toward the upper left and the viewer; y grows down).
        private const float FrameLightX = -0.42f;
        private const float FrameLightY = -0.62f;
        private const float FrameLightZ = 0.66f;

        private const int StoneRingBlocks = 12;
        private const int StoneRingSeed = 17;

        // The framed disc's rounded square in a frame's picture (the rim's middle line): half its side, its corner radius
        // and its straight half-sides.
        private const float FrameHalf = AvatarLook.FrameEdge;
        private const float FrameCorner = AvatarLook.FrameCorner;
        private const float FrameStraight = FrameHalf - FrameCorner;

        // A quarter of the rounded square's outline and the whole of it, in picture units.
        private const float FrameQuarter = (2f * FrameStraight) + (FrameCorner * (float)Math.PI / 2f);
        private const float FramePerimeter = 4f * FrameQuarter;

        private static readonly NavLeaf[] LeafRingLeaves = LeafWreath(20, 0f, 0.118f, 0.041f, 0.092f, 0.033f, 3);

        private static readonly NavLeaf[] WreathLeaves = LeafWreath(16, 1f / 32f, 0.074f, 0.026f, 0.066f, 0.023f, 7);

        /// <summary>
        /// A drawn profile frame (<see cref="ProfileFrameStyle"/>) in a square picture of side <paramref name="size"/>, its
        /// band centered on the framed disc's edge (<see cref="AvatarLook.FrameEdge"/> of the side from the middle):
        /// <list type="bullet">
        /// <item><description><see cref="ProfileFrameStyle.WoodRing"/> (Wooden Frame): a rounded band in the icon buttons'
        /// plate laminate (spec 005 FR-047), lit from the upper left, in the plate's <c>wood.line</c> outline, and four brass
        /// nails (<c>medal.gold</c>) at its corners;</description></item>
        /// <item><description><see cref="ProfileFrameStyle.LeafRing"/> (Leaf Frame): a green vine (<c>garden.leaf_3</c>)
        /// round the disc with twenty almond leaves in the three garden greens, all turning one way, out and in by
        /// turns;</description></item>
        /// <item><description><see cref="ProfileFrameStyle.FlowerWreath"/>: two twisted twigs (<c>wood.dark</c>) with
        /// sixteen small leaves and eight blossoms, pink (<c>lotus.fill</c>) at the sides' middles and white
        /// (<c>garden.flower</c>) at the corners, with golden middles;</description></item>
        /// <item><description><see cref="ProfileFrameStyle.StoneRing"/> (Stone Frame): twelve blocks of sandy stone, one
        /// bent round each corner (<c>stone.top</c> to <c>stone.face</c>, mottled, a <c>stone.lip</c> side away from the
        /// light, the <c>stone.line</c> outline) on dark mortar, two with a tuft of moss (<c>stone.moss</c>);</description></item>
        /// <item><description><see cref="ProfileFrameStyle.GoldenRibbon"/>: a gold satin band (<c>medal.gold</c>, lit to
        /// <c>ray.light</c>) wound round in eighteen folds, a gold star on top and a pink bow (<c>lotus.*</c>) at the
        /// bottom.</description></item>
        /// </list>
        /// </summary>
        public static byte[] ProfileFrame(int size, ProfileFrameStyle style)
        {
            Check(size, size);
            var pixels = new byte[size * size * 4];
            float pixel = 1f / size;
            for (int py = 0; py < size; py++)
            {
                float v = ((py + 0.5f) / size) - 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float u = ((px + 0.5f) / size) - 0.5f;
                    var paint = default(NavPaint);
                    switch (style)
                    {
                        case ProfileFrameStyle.WoodRing:
                            PaintWoodRing(ref paint, u, v, pixel);
                            break;
                        case ProfileFrameStyle.LeafRing:
                            PaintLeafRing(ref paint, u, v, pixel);
                            break;
                        case ProfileFrameStyle.FlowerWreath:
                            PaintFlowerWreath(ref paint, u, v, pixel);
                            break;
                        case ProfileFrameStyle.StoneRing:
                            PaintStoneRing(ref paint, u, v, pixel);
                            break;
                        default:
                            PaintGoldenRibbon(ref paint, u, v, pixel);
                            break;
                    }

                    paint.Write(pixels, ((py * size) + px) * 4);
                }
            }

            return pixels;
        }

        // ---- The wooden ring ----

        /// <summary>
        /// The Wooden Frame (spec 005 FR-047; the owner, 2026-10-08: "the base frame must become the same wood"): a band in
        /// the icon buttons' plate laminate (<see cref="RaisedPlate"/>, its grain across the picture as on a button), rounding
        /// over at both edges and flat on its middle, lit from the upper left, its top catching the light, deeper lower down,
        /// in the plate's outline and saturation (<see cref="Finish"/>), with four brass nails at its corners.
        /// </summary>
        private static void PaintWoodRing(ref NavPaint paint, float u, float v, float pixel)
        {
            float edge = AvatarLook.FrameEdge;
            float inner = edge - 0.056f;
            float outer = edge + 0.05f;
            FrameShadow(ref paint, u, v, inner, outer, 0.3f);
            float dist = FrameDist(u, v);
            float mid = (inner + outer) / 2f;
            float half = (outer - inner) / 2f;
            float d = Math.Abs(dist - mid) - half;
            float cover = Cover(d, pixel);
            if (cover > 0f)
            {
                // The plate's wood at the plate's scale: a band as wide as a button's rim of IconRimShare.
                float band = (outer - inner) / pixel;
                Color c = Laminate(u / pixel, v / pixel, band / GardenLook.IconRimShare);

                // Across the band: rounding over at the outside, flat on its middle, rounding down at the inside.
                float q = Clamp01((outer - dist) / (outer - inner));
                float f = q < 0.4f ? (float)Math.Cos(q / 0.4f * Math.PI / 2f) : q > 0.75f ? -(float)Math.Cos((1f - q) / 0.25f * Math.PI / 2f) : 0f;
                (float gx, float gy) = FrameNormal(u, v);
                Shade(ref c, C.WoodGrain, gx * f, gy * f, (float)Math.Sqrt(Math.Max(0f, 1f - (f * f))), 0.45f, 0.45f, 0.18f);
                c.Mix(C.WoodMid.Lighten(0.12f), 0.6f * Clamp01(1f - ((v + 0.5f) / 0.3f)));
                Finish(ref c, v + 0.5f, d / pixel, Math.Max(1f, 0.012f / pixel));
                paint.Over(cover, c.ToRgba());
            }

            // Four brass nails at the corners.
            for (int k = 0; k < 4; k++)
            {
                (float nx, float ny, _) = FramePoint((k + 0.5f) * FrameQuarter, mid - FrameHalf);
                PaintNail(ref paint, u, v, nx, ny, Math.Max(pixel * 1.6f, 0.012f), pixel);
            }
        }

        /// <summary>A round brass nail head: a dark rim, the gold head lit from the upper left, a light glint.</summary>
        private static void PaintNail(ref NavPaint paint, float u, float v, float cx, float cy, float r, float pixel)
        {
            float dx = u - cx;
            float dy = v - cy;
            if (Math.Abs(dx) > r * 2f || Math.Abs(dy) > r * 2f)
            {
                return;
            }

            float d = Length(dx, dy) - r;
            paint.Over(Cover(d - (r * 0.35f), pixel), C.WoodLine);
            Rgba head = C.MedalGold.Darken(0.12f).Mix(C.MedalGold.Lighten(0.35f), Clamp01(0.5f - ((dx + dy) / (r * 2.4f))));
            paint.Over(Cover(d, pixel), head);
            paint.Over(0.8f * Cover(Length(dx + (r * 0.35f), dy + (r * 0.35f)) - (r * 0.3f), pixel), C.RayLight);
        }

        // ---- The leaf ring ----

        private static void PaintLeafRing(ref NavPaint paint, float u, float v, float pixel)
        {
            float edge = AvatarLook.FrameEdge;
            FrameShadow(ref paint, u, v, edge - 0.05f, edge + 0.05f, 0.24f);
            float dist = FrameDist(u, v);
            float half = 0.015f;
            float line = FrameLine(pixel);
            float d = Math.Abs(dist - edge) - half;
            if (d - line < pixel)
            {
                // The vine: a green stem outlined in garden.leaf_line, a lighter streak toward the light.
                paint.Over(Cover(d - line, pixel), C.GardenLeafLine);
                Rgba stem = C.GardenLeaf3.Mix(C.GardenLeaf2, Clamp01(BandLight(u, v, dist, edge, half) - 0.4f));
                paint.Over(Cover(d, pixel), stem);
                paint.Over(0.5f * Cover(Math.Abs(dist - edge + (half * 0.35f)) - (half * 0.25f), pixel), C.GardenLeaf2);
            }

            foreach (NavLeaf leaf in LeafRingLeaves)
            {
                leaf.Paint(ref paint, u, v, pixel);
            }
        }

        /// <summary>
        /// <paramref name="count"/> leaves round the disc's edge, every second one leaning out and the others in, all turning
        /// clockwise; the first <paramref name="from"/> of the way round from the right side's middle, each a little off its
        /// place, spread evenly along the rounded square.
        /// </summary>
        private static NavLeaf[] LeafWreath(int count, float from, float outLength, float outHalf, float inLength, float inHalf, int seed)
        {
            var leaves = new NavLeaf[count];
            for (int i = 0; i < count; i++)
            {
                float along = (from + ((float)i / count) + ((Hash(i, 3, seed) - 0.5f) / 60f)) * FramePerimeter;
                (float x, float y, float tangent) = FramePoint(along, 0f);
                bool outward = i % 2 == 0;
                leaves[i] = NavLeaf.At(
                    x,
                    y,
                    tangent + (outward ? -42f : 42f),
                    outward ? outLength : inLength,
                    outward ? outHalf : inHalf,
                    i % 3);
            }

            return leaves;
        }

        // ---- The flower wreath ----

        private static void PaintFlowerWreath(ref NavPaint paint, float u, float v, float pixel)
        {
            float edge = AvatarLook.FrameEdge;
            FrameShadow(ref paint, u, v, edge - 0.045f, edge + 0.045f, 0.24f);
            float dist = FrameDist(u, v);
            float line = FrameLine(pixel);
            if (Math.Abs(dist - edge) < 0.06f)
            {
                // Two twigs twisted round each other, the one behind first.
                float angle = FrameAngle(u, v);
                float wave = (float)Math.Sin(angle * 5f);
                bool firstBehind = Math.Cos(angle * 5f) < 0f;
                for (int pass = 0; pass < 2; pass++)
                {
                    int k = (pass == 0) == firstBehind ? 0 : 1;
                    float r = edge + (0.011f * (k == 0 ? wave : -wave));
                    float d = Math.Abs(dist - r) - 0.0095f;
                    paint.Over(Cover(d - line, pixel), C.WoodDarkLine);
                    Rgba twig = C.WoodDark.Mix(C.WoodDarkTop, Clamp01(BandLight(u, v, dist, r, 0.0095f)));
                    paint.Over(Cover(d, pixel), twig);
                }
            }

            foreach (NavLeaf leaf in WreathLeaves)
            {
                leaf.Paint(ref paint, u, v, pixel);
            }

            // Eight blossoms: pink at the sides' middles, white at the corners.
            for (int j = 0; j < 8; j++)
            {
                (float cx, float cy, _) = FramePoint(j * FrameQuarter / 2f, 0f);
                float turn = Hash(j, 11, 7) * 1.2f;
                if (j % 2 == 0)
                {
                    PaintBlossom(ref paint, cx, cy, 0.07f, turn, C.LotusFill, C.LotusTip, C.LotusLine, u, v, pixel);
                }
                else
                {
                    PaintFlower(ref paint, cx, cy, 0.058f, turn, u, v, pixel);
                }
            }
        }

        /// <summary>
        /// A five-petal blossom in <paramref name="petal"/> with a lighter <paramref name="tip"/> toward its middle, its
        /// <paramref name="line"/> outline and a golden middle (<c>garden.flower_center</c>), as <see cref="PaintFlower"/>.
        /// </summary>
        private static void PaintBlossom(ref NavPaint paint, float cx, float cy, float r, float turn, Rgba petal, Rgba tip, Rgba line, float x, float y, float pixel)
        {
            float dx = x - cx;
            float dy = y - cy;
            float grow = Math.Max(pixel, r * 0.09f);
            if (Math.Abs(dx) > r + grow + pixel || Math.Abs(dy) > r + grow + pixel)
            {
                return;
            }

            float centre = Length(dx, dy);
            float d = centre - (0.45f * r);
            for (int i = 0; i < 5; i++)
            {
                float a = turn + (i * 2f * (float)Math.PI / 5f);
                d = Math.Min(d, Length(dx - (0.55f * r * (float)Math.Cos(a)), dy - (0.55f * r * (float)Math.Sin(a))) - (0.42f * r));
            }

            paint.Over(Cover(d - grow, pixel), line);
            paint.Over(Cover(d, pixel), petal);
            paint.Over(0.7f * Cover(d, pixel) * (1f - Smooth(Clamp01((centre - (0.3f * r)) / (0.5f * r)))), tip);
            float m = centre - (0.26f * r);
            paint.Over(Cover(m - (r * 0.07f), pixel), C.GardenFlowerCenterLine);
            paint.Over(Cover(m, pixel), C.GardenFlowerCenter);
        }

        // ---- The stone ring ----

        private static void PaintStoneRing(ref NavPaint paint, float u, float v, float pixel)
        {
            float edge = AvatarLook.FrameEdge;
            float inner = edge - 0.062f;
            float outer = edge + 0.056f;
            FrameShadow(ref paint, u, v, inner, outer, 0.32f);
            float dist = FrameDist(u, v);
            if (dist < inner - 0.02f || dist > outer + 0.02f)
            {
                return;
            }

            float line = FrameLine(pixel);
            float mid = (inner + outer) / 2f;
            float half = (outer - inner) / 2f;

            // The mortar between the blocks, a little inside their edges.
            paint.Over(Cover(Math.Abs(dist - mid) - (half - 0.014f), pixel), C.StoneLip.Darken(0.32f));

            // The block under this pixel along the band: the boundaries a little uneven, each block its own size, one
            // block bent round each corner.
            float step = FramePerimeter / StoneRingBlocks;
            float at = FrameAlong(u, v);
            int k = Math.Min(StoneRingBlocks - 1, (int)(at / step));
            if (at < StoneBoundary(k))
            {
                k = (k + StoneRingBlocks - 1) % StoneRingBlocks;
            }
            else if (at >= StoneBoundary(k + 1))
            {
                k = (k + 1) % StoneRingBlocks;
            }

            float from = StoneBoundary(k);
            float to = StoneBoundary(k + 1);
            float rel = at - ((from + to) / 2f);
            if (rel > FramePerimeter / 2f)
            {
                rel -= FramePerimeter;
            }
            else if (rel < -FramePerimeter / 2f)
            {
                rel += FramePerimeter;
            }

            float blockInner = inner + (0.005f * (Hash(k, 6, StoneRingSeed) - 0.5f));
            float blockOuter = outer + (0.008f * (Hash(k, 7, StoneRingSeed) - 0.5f));
            // Measured along the band, so the joints run straight across it.
            float halfLength = ((to - from) / 2f) - 0.004f;
            float halfWidth = (blockOuter - blockInner) / 2f;
            float along = rel;
            float across = dist - ((blockInner + blockOuter) / 2f);
            float d = RoundRect(along, across, -halfLength, -halfWidth, halfLength, halfWidth, 0.014f);
            float cover = Cover(d, pixel);
            if (cover <= 0f)
            {
                return;
            }

            float light = BandLight(u, v, dist, mid, half);
            var c = new Color(C.StoneFace.Mix(C.StoneLip, 0.25f).Mix(C.StoneTop, Clamp01((light - 0.45f) * 1.8f)));
            c.Scale(0.9f + (0.1f * Hash(k, 8, StoneRingSeed)));
            c.Scale(1f + ((Fbm((u * 24f) + k, v * 24f, 23 + k, 3) - 0.5f) * 0.22f));
            if (Noise(u * 95f, v * 95f, 41) > 0.84f)
            {
                c.Mix(C.StoneLip, 0.45f);
            }

            // Each block a little deeper toward its edges, its side away from the light in stone.lip, a light bevel
            // inside its lit edge.
            c.Mix(C.StoneLip, 0.4f * Clamp01((d + 0.016f) / 0.016f));
            c.Mix(C.StoneLip, 0.85f * Clamp01((0.56f - light) * 2.4f));
            c.Mix(C.StoneTop.Lighten(0.4f), 0.55f * Clamp01((light - 0.7f) * 3f) * Clamp01((d + 0.014f) / 0.01f));

            // A tuft of moss over the outer edge of two blocks (the lower right corner's and the upper left corner's).
            if (k == 1 || k == 7)
            {
                float moss = Length(along - (halfLength * 0.2f), (across - (halfWidth * 0.75f)) * 1.25f) - 0.04f - (0.018f * (Fbm(u * 34f, v * 34f, 29, 2) - 0.5f));
                float tuft = Clamp01(-moss / 0.007f);
                c.Mix(C.StoneMoss, 0.92f * tuft);
                c.Mix(C.StoneMoss.Lighten(0.25f), 0.5f * tuft * Clamp01((light - 0.55f) * 3f));
                c.Mix(C.StoneMoss.Darken(0.3f), 0.6f * tuft * Clamp01((moss + 0.012f) / 0.012f));
            }

            c.Mix(C.StoneLine, Clamp01(0.5f + ((d + (line * 1.3f)) / pixel)));
            paint.Over(cover, c.ToRgba());
        }

        /// <summary>
        /// Where the stone frame's block <paramref name="k"/> starts along the band (<see cref="FrameAlong"/>; the last wraps
        /// to the first): three blocks a quarter, the corners' blocks centered on the corners.
        /// </summary>
        private static float StoneBoundary(int k)
        {
            float step = FramePerimeter / StoneRingBlocks;
            int wraps = k >= StoneRingBlocks ? 1 : 0;
            int index = k % StoneRingBlocks;
            return ((index + (0.3f * (Hash(index, 5, StoneRingSeed) - 0.5f))) * step) + (wraps * FramePerimeter);
        }

        // ---- The golden ribbon ----

        private static void PaintGoldenRibbon(ref NavPaint paint, float u, float v, float pixel)
        {
            float edge = AvatarLook.FrameEdge;
            float inner = edge - 0.044f;
            float outer = edge + 0.044f;
            FrameShadow(ref paint, u, v, inner, outer, 0.28f);
            float dist = FrameDist(u, v);
            float mid = (inner + outer) / 2f;
            float half = (outer - inner) / 2f;
            float line = FrameLine(pixel);
            Rgba gold = C.MedalGold;
            Rgba deep = gold.Darken(0.3f);
            Rgba rim = gold.Darken(0.5f);
            float d = Math.Abs(dist - mid) - half;
            float cover = Cover(d, pixel);
            if (cover > 0f)
            {
                float light = BandLight(u, v, dist, mid, half);
                var c = new Color(gold);
                c.Mix(C.RayLight, 0.78f * Clamp01((light - 0.66f) * 3.2f));
                c.Mix(deep, 0.65f * Clamp01((0.6f - light) * 2f));

                // The satin wound round in eighteen folds: a deep line where one fold tucks under the next, a sheen after it.
                float across = (dist - inner) / (outer - inner);
                float phase = (FrameAlong(u, v) / FramePerimeter * 18f) + (across * 0.8f);
                float f = phase - (float)Math.Floor(phase);
                float fold = FramePerimeter / 18f;
                c.Mix(deep, 0.6f * Cover((Math.Min(f, 1f - f) * fold) - (line * 0.3f), pixel));
                c.Mix(C.RayLight, 0.4f * Cover((Math.Abs(f - 0.12f) * fold) - (line * 0.35f), pixel));
                c.Mix(rim, Clamp01(0.5f + ((d + line) / pixel)));
                paint.Over(cover, c.ToRgba());
            }

            PaintRibbonStar(ref paint, u, v, edge, gold, rim, line, pixel);
            PaintRibbonBow(ref paint, u, v, edge, line, pixel);
        }

        /// <summary>The gold five-point star on the ribbon's top, lit from the upper left, with a glint.</summary>
        private static void PaintRibbonStar(ref NavPaint paint, float u, float v, float edge, Rgba gold, Rgba rim, float line, float pixel)
        {
            float cx = 0f;
            float cy = -edge - 0.004f;
            float r = 0.082f;
            float x = u - cx;
            float y = v - cy;
            if (Math.Abs(x) > r + 0.02f || Math.Abs(y) > r + 0.02f)
            {
                return;
            }

            float d = Star5(x, -y, r, 0.46f);
            paint.Over(Cover(d - line, pixel), rim);
            Rgba fill = gold.Lighten(0.1f).Mix(C.RayLight, Clamp01(0.45f - ((x + y) / (r * 2.2f))));
            paint.Over(Cover(d, pixel), fill);
            paint.Over(0.5f * Cover(d, pixel) * Clamp01((x + y) / r), gold.Darken(0.18f));
            paint.Over(0.9f * Cover(Length(x + (r * 0.26f), y + (r * 0.2f)) - (r * 0.11f), pixel), C.RayLight);
        }

        /// <summary>The pink bow at the ribbon's bottom: two tails, two loops with their shaded insides, and the knot.</summary>
        private static void PaintRibbonBow(ref NavPaint paint, float u, float v, float edge, float line, float pixel)
        {
            float cy = edge + 0.006f;
            float x = u;
            float y = v - cy;
            if (Math.Abs(x) > 0.15f || y < -0.07f || y > 0.1f)
            {
                return;
            }

            float ax = Math.Abs(x);

            // The tails: from under the knot down and out, a bite out of each end.
            float tail = Segment(ax, y, 0.012f, 0.008f, 0.046f, 0.068f) - 0.016f;
            tail = Math.Max(tail, -(Length(ax - 0.05f, y - 0.088f) - 0.014f));
            PaintBowPart(ref paint, tail, y > 0.04f ? 0.2f : 0f, line, pixel);

            // The loops, tilted up toward the outside, and the shaded hole of each.
            float lx = ax - 0.064f;
            float ly = y + 0.008f;
            const float Cos = 0.951f;
            const float Sin = 0.309f;
            float rx = (lx * Cos) + (ly * Sin);
            float ry = (-lx * Sin) + (ly * Cos);
            float loop = Ellipse(rx, ry, 0.064f, 0.04f);
            PaintBowPart(ref paint, loop, Clamp01(ry / 0.04f) * 0.35f, line, pixel);
            float hole = Ellipse(rx + 0.012f, ry + 0.002f, 0.03f, 0.014f);
            paint.Over(Cover(hole, pixel), C.LotusLine.Mix(C.LotusFill, 0.4f));

            // The knot.
            float knot = RoundRect(x, y, -0.022f, -0.024f, 0.022f, 0.022f, 0.012f);
            PaintBowPart(ref paint, knot, 0f, line, pixel);
            paint.Over(0.6f * Cover(knot, pixel) * Clamp01(-y / 0.02f), C.LotusTip);
        }

        /// <summary>A part of the bow: the <c>lotus.line</c> outline, the <c>lotus.fill</c> satin, a little deeper by <paramref name="shade"/>.</summary>
        private static void PaintBowPart(ref NavPaint paint, float d, float shade, float line, float pixel)
        {
            if (d - line > pixel)
            {
                return;
            }

            paint.Over(Cover(d - line, pixel), C.LotusLine);
            paint.Over(Cover(d, pixel), C.LotusFill.Mix(C.LotusLine, shade));
        }

        /// <summary>
        /// The signed distance to a five-point star of outer radius <paramref name="r"/>, one point up (y up here), its inner
        /// corners at <paramref name="inner"/> of the way in.
        /// </summary>
        private static float Star5(float x, float y, float r, float inner)
        {
            const float K1x = 0.809016994f;
            const float K1y = -0.587785252f;
            const float K2x = -K1x;
            const float K2y = K1y;
            x = Math.Abs(x);
            float t = 2f * Math.Max((K1x * x) + (K1y * y), 0f);
            x -= t * K1x;
            y -= t * K1y;
            t = 2f * Math.Max((K2x * x) + (K2y * y), 0f);
            x -= t * K2x;
            y -= t * K2y;
            x = Math.Abs(x);
            y -= r;
            float bax = (inner * -K1y) - 0f;
            float bay = (inner * K1x) - 1f;
            float h = Clamp(((x * bax) + (y * bay)) / ((bax * bax) + (bay * bay)), 0f, r);
            float side = (y * bax) - (x * bay);
            return Length(x - (bax * h), y - (bay * h)) * (side < 0f ? -1f : 1f);
        }

        // ---- Shared ----

        /// <summary>A frame's outline width in picture units: about a pixel at small sizes, 0.0065 of the side at large ones.</summary>
        private static float FrameLine(float pixel) => Math.Max(pixel * 1.1f, 0.0065f);

        /// <summary>
        /// A ring's distance from the middle for the frames' rounded square: <see cref="AvatarLook.FrameEdge"/> plus the
        /// signed distance from (u, v) to the disc's rounded square, so a band at <c>FrameEdge ± d</c> is that square grown or
        /// shrunk by d. It is |u| or |v| along the axes and 0 in the middle.
        /// </summary>
        private static float FrameDist(float u, float v)
        {
            float qx = Math.Abs(u) - FrameStraight;
            float qy = Math.Abs(v) - FrameStraight;
            return FrameStraight + Length(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f);
        }

        /// <summary>
        /// Where (u, v) lies along the band (a ring's angle): the length along the disc's rounded square, from the right
        /// side's middle round through the bottom (clockwise on screen, as a ring's angle grew), in [0, <see cref="FramePerimeter"/>).
        /// </summary>
        private static float FrameAlong(float u, float v)
        {
            float q = AlongQuarter(Math.Abs(u), Math.Abs(v));
            if (u >= 0f)
            {
                return v >= 0f ? q : FramePerimeter - q;
            }

            return v >= 0f ? (2f * FrameQuarter) - q : (2f * FrameQuarter) + q;
        }

        /// <summary><see cref="FrameAlong"/> as an angle in radians, for the patterns that went round a ring.</summary>
        private static float FrameAngle(float u, float v)
        {
            float a = FrameAlong(u, v) / FramePerimeter * 2f * (float)Math.PI;
            return a > (float)Math.PI ? a - (2f * (float)Math.PI) : a;
        }

        /// <summary>The length along the lower right quarter of the rounded square (x, y ≥ 0) from the right side's middle.</summary>
        private static float AlongQuarter(float x, float y)
        {
            float qx = x - FrameStraight;
            float qy = y - FrameStraight;
            if (qx > 0f && qy > 0f)
            {
                return FrameStraight + (FrameCorner * (float)Math.Atan2(qy, qx));
            }

            return qx >= qy ? Math.Min(y, FrameStraight) : FrameStraight + (FrameCorner * (float)Math.PI / 2f) + (FrameStraight - Math.Min(x, FrameStraight));
        }

        /// <summary>
        /// The point <paramref name="along"/> the band (<see cref="FrameAlong"/>), <paramref name="offset"/> out from the
        /// disc's edge, and the band's direction there in degrees (y down; a ring's angle + 90°).
        /// </summary>
        private static (float X, float Y, float Degrees) FramePoint(float along, float offset)
        {
            float s = along % FramePerimeter;
            if (s < 0f)
            {
                s += FramePerimeter;
            }

            int quarter = Math.Min(3, (int)(s / FrameQuarter));
            float t = s - (quarter * FrameQuarter);
            bool mirrored = quarter == 1 || quarter == 3;
            float q = mirrored ? FrameQuarter - t : t;

            // The point and its direction in the lower right quarter, q from the right side's middle.
            float x;
            float y;
            float tx;
            float ty;
            float bend = FrameCorner * (float)Math.PI / 2f;
            if (q <= FrameStraight)
            {
                x = FrameHalf + offset;
                y = q;
                tx = 0f;
                ty = 1f;
            }
            else if (q < FrameStraight + bend)
            {
                float a = (q - FrameStraight) / FrameCorner;
                x = FrameStraight + ((FrameCorner + offset) * (float)Math.Cos(a));
                y = FrameStraight + ((FrameCorner + offset) * (float)Math.Sin(a));
                tx = -(float)Math.Sin(a);
                ty = (float)Math.Cos(a);
            }
            else
            {
                x = FrameStraight - (q - FrameStraight - bend);
                y = FrameHalf + offset;
                tx = -1f;
                ty = 0f;
            }

            // Into the point's quarter: mirrored across, and the direction turned where the length runs the other way.
            switch (quarter)
            {
                case 1:
                    x = -x;
                    ty = -ty;
                    break;
                case 2:
                    x = -x;
                    y = -y;
                    tx = -tx;
                    ty = -ty;
                    break;
                case 3:
                    y = -y;
                    tx = -tx;
                    break;
            }

            return (x, y, (float)(Math.Atan2(ty, tx) * 180.0 / Math.PI));
        }

        /// <summary>
        /// The light on a band round the rounded square (its cross-section a half circle) at (u, v): about 0.66 on its
        /// crest, toward 1 on the side facing the upper left, lower on the side away from it.
        /// </summary>
        private static float BandLight(float u, float v, float dist, float mid, float half)
        {
            float s = Clamp((dist - mid) / Math.Max(1e-5f, half), -1f, 1f);
            (float nx, float ny) = FrameNormal(u, v);
            float nz = (float)Math.Sqrt(Math.Max(0f, 1f - (s * s)));
            return (nx * s * FrameLightX) + (ny * s * FrameLightY) + (nz * FrameLightZ);
        }

        /// <summary>The outward direction of the rounded square's bands at (u, v): out of a side, or out of a corner's middle.</summary>
        private static (float X, float Y) FrameNormal(float u, float v)
        {
            float qx = Math.Abs(u) - FrameStraight;
            float qy = Math.Abs(v) - FrameStraight;
            float sx = u < 0f ? -1f : 1f;
            float sy = v < 0f ? -1f : 1f;
            if (qx > 0f && qy > 0f)
            {
                float length = Math.Max(1e-5f, Length(qx, qy));
                return (sx * qx / length, sy * qy / length);
            }

            return qx >= qy ? (sx, 0f) : (0f, sy);
        }

        /// <summary>The soft shadow a frame casts on the avatar and round it: its band a little lower, feathered.</summary>
        private static void FrameShadow(ref NavPaint paint, float u, float v, float inner, float outer, float alpha)
        {
            float mid = (inner + outer) / 2f;
            float half = (outer - inner) / 2f;
            float drop = Math.Abs(FrameDist(u, v - 0.014f) - mid) - half;
            if (drop < 0.03f)
            {
                paint.Over(alpha * (1f - Smooth(Clamp01((drop + 0.006f) / 0.03f))), C.GardenShadow);
            }
        }
    }
}
