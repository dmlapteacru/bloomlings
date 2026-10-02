using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>The two woods of the reference look (spec 005 contracts/look.md §2).</summary>
    public enum WoodTone
    {
        /// <summary>Pale honey wood: signs, button rims (<c>wood.light</c> to <c>wood.mid</c>).</summary>
        Light,

        /// <summary>Darker wood: the pod frames (<c>wood.dark_top</c> to <c>wood.dark</c>).</summary>
        Dark,
    }

    /// <summary>How a candy tile draws its symbol (spec 005 contracts/look.md §3.1).</summary>
    public enum TileStyle
    {
        /// <summary>Board tiles: nearly square, with the symbol as a bold gem with a thick dark outline (spec 005 FR-026).</summary>
        Board,

        /// <summary>Pods, slots, the jam row, the Collection and the strip: a bigger, detailed symbol with a dark outline.</summary>
        Sticker,

        /// <summary>
        /// The finished picture (the win card, the Collection; research D14): the board style without the lip, a flat
        /// full-color tile with its small gloss and the board's gem icon.
        /// </summary>
        Flat,
    }

    /// <summary>
    /// The pictured states of a candy tile (spec 005 contracts/look.md §3.1). Pressed is not a picture: the kit sinks the
    /// face into its lip when it draws the tile.
    /// </summary>
    public enum TileState
    {
        Normal,

        /// <summary>A queued pod: face and symbol mixed 45% toward <c>parchment.bottom</c>.</summary>
        Dimmed,

        /// <summary>A stuck slot: the tile in grey.</summary>
        Grey,

        /// <summary>A mystery: the lilac mystery tile with a white "?" and no symbol.</summary>
        Mystery,
    }

    /// <summary>
    /// The reference look's material pictures (spec 005 research D4, contracts/look.md §2): wooden planks and frames,
    /// stone blocks and candy tiles, rendered as RGBA pixels that both builds cache and draw (<c>IPainter.Picture</c>,
    /// <c>ProceduralSprites.Picture</c>), like <see cref="BackdropRaster"/>.
    /// <list type="bullet">
    /// <item><description>Every function returns straight-alpha RGBA bytes, rows from the top, <c>width * height * 4</c> long.</description></item>
    /// <item><description>Edges are anti-aliased over one pixel.</description></item>
    /// <item><description>The same arguments give the same bytes: noise comes from an integer hash, never from a random generator.</description></item>
    /// </list>
    /// Sizes are pixels; colors are the <c>wood.*</c>, <c>stone.*</c>, <c>parchment.*</c> tokens and the variant colors.
    /// Engine-free.
    /// </summary>
    public static class UiRaster
    {
        /// <summary>The cache key of a picture: <c>key@WxH</c> (contracts/look.md §2).</summary>
        public static string CacheKey(string key, int width, int height) => key + "@" + width + "x" + height;

        /// <summary>A picture size in pixels rounded up to a multiple of 8 (fewer cache entries across screens), at least 8.</summary>
        public static int Quantize(float pixels) => Math.Max(8, ((int)Math.Ceiling(pixels / 8f)) * 8);

        // ---- Wood ----

        /// <summary>The share of a plank's height its slightly deeper bottom band takes by default (signs; contracts/look.md §2).</summary>
        public const float PlankLip = 0.06f;

        /// <summary>
        /// A wooden plank (a sign, a button rim): a pale top-to-bottom gradient, horizontal grain (many fine faint streaks,
        /// a few thin wavy lines and sometimes a knot), a lighter bevel inside the top edge, a slightly deeper band along
        /// the bottom <paramref name="lipShare"/> of the height (the button rim passes less), the outline, and two nail
        /// dots near the top-left and bottom-right ends when taller than 48 px. <paramref name="seed"/> varies the grain.
        /// </summary>
        public static byte[] Plank(int width, int height, float radius, float outline, WoodTone tone, int seed, float lipShare = PlankLip)
        {
            Check(width, height);
            Wood wood = Wood.Of(tone, width, height, seed);
            var pixels = new byte[width * height * 4];
            float r = Math.Min(radius, Math.Min(width, height) / 2f);
            float line = Math.Max(0f, outline);
            float bevel = Clamp(height * 0.035f, 1f, 4f);
            float lip = height * Math.Max(0f, lipShare);
            float nail = Math.Max(1.4f, height * 0.035f);
            float nailInset = Math.Max(r * 0.7f, height * 0.3f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float d = RoundRect(x, y, 0f, 0f, width, height, r);
                    float cover = Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float inner = d + line;
                    var c = new Color(wood.Face(px, py, horizontal: true));

                    // A slightly darker rim just inside the outline, then the deeper bottom band and the bevel (only near the edge).
                    c.Mix(wood.Lip, 0.1f * (1f - Smooth(Clamp01(-inner / (height * 0.12f)))));
                    if (inner > -(lip + 2f))
                    {
                        c.Mix(wood.Lip, 0.6f * Coverage(inner) * Outside(RoundRect(x, y + lip, 0f, 0f, width, height, r) + line, 1.5f));
                        c.Mix(wood.Light, 0.75f * Coverage(inner) * Outside(RoundRect(x, y - bevel, 0f, 0f, width, height, r) + line, 1f));
                    }

                    // The nails: near the top at the left end, near the bottom at the right end.
                    bool leftNail = x < width / 2f;
                    float nx = leftNail ? nailInset : width - nailInset;
                    float ny = height * (leftNail ? 0.3f : 0.7f);
                    if (height > 48 && Math.Abs(x - nx) < nail + 2f && Math.Abs(y - ny) < nail + 2f)
                    {
                        c.Mix(wood.Line.Lighten(0.1f), Coverage(Length(x - nx, y - ny) - nail));
                        c.Mix(wood.Light, 0.6f * Coverage(Length(x - nx + (nail * 0.35f), y - ny + (nail * 0.35f)) - (nail * 0.35f)));
                    }

                    c.Mix(wood.Line, Clamp01(0.5f + inner));
                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A wooden frame (the pod frame): the plank material as a ring <paramref name="border"/> wide around a transparent
        /// hole, its side members grained vertically, with an outline along both edges, a translucent shadow inside the
        /// hole along its top and a light edge on the wood along the hole's bottom. Draw the inner panel first, then the
        /// frame over it, so the shadow falls on the panel.
        /// </summary>
        public static byte[] Frame(int width, int height, float radius, float border, WoodTone tone, int seed)
        {
            Check(width, height);
            Wood wood = Wood.Of(tone, width, height, seed);
            var pixels = new byte[width * height * 4];
            float r = Math.Min(radius, Math.Min(width, height) / 2f);
            float b = Clamp(border, 1f, (Math.Min(width, height) / 2f) - 1f);
            float holeR = Math.Max(2f, r - b);
            float line = Math.Max(1f, b * 0.12f);
            float bevel = Clamp(b * 0.18f, 1f, 4f);
            float lip = b * 0.4f;
            float shadow = Math.Max(2f, b * 0.7f);
            Rgba shade = C.GardenShadow;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float outer = RoundRect(x, y, 0f, 0f, width, height, r);
                    float hole = RoundRect(x, y, b, b, width - b, height - b, holeR);
                    float d = Math.Max(outer, -hole);
                    float cover = Coverage(d);
                    if (cover > 0f)
                    {
                        // Side members run vertically: the nearest edge decides (mitred corners).
                        bool side = Math.Min(x, width - x) < Math.Min(y, height - y);
                        var c = new Color(wood.Face(px, py, horizontal: !side));
                        if (outer + line > -(lip + 2f))
                        {
                            c.Mix(wood.Lip, Coverage(outer + line) * Outside(RoundRect(x, y + lip, 0f, 0f, width, height, r) + line, 1.5f));
                            c.Mix(wood.Light, 0.7f * Coverage(outer + line) * Outside(RoundRect(x, y - bevel, 0f, 0f, width, height, r) + line, 1f));
                        }

                        // The light edge where the wood meets the hole's bottom.
                        float lower = Smooth(Clamp01((y - (height * 0.5f)) / (height * 0.2f)));
                        c.Mix(wood.Light, 0.55f * lower * Clamp01(1f - (Math.Abs(hole - (line + 1.2f)) / 1.2f)));
                        c.Mix(wood.Line, Math.Max(Clamp01(0.5f - (-outer - line)), Clamp01(0.5f - (hole - line))));
                        Put(pixels, width, px, py, c, cover);
                    }
                    else if (hole < 0f)
                    {
                        // Inside the hole: only the shadow along its top.
                        float depth = y - b;
                        float alpha = 0.32f * (1f - Smooth(Clamp01(depth / shadow))) * Coverage(hole);
                        if (alpha > 0f)
                        {
                            Put(pixels, width, px, py, new Color(shade), alpha);
                        }
                    }
                }
            }

            return pixels;
        }

        // ---- Stone ----

        /// <summary>
        /// A stone block (the board border, the arch, the pedestal): a warm sandy <c>stone.top</c> to <c>stone.face</c>
        /// gradient with a smooth, lightly mottled surface and rare faint speckles, a light bevel at the top, a
        /// <c>stone.lip</c> band along the bottom 14%, the dark <c>stone.line</c> outline, and on a few seeds a soft tuft of
        /// moss over one corner.
        /// </summary>
        public static byte[] Stone(int width, int height, float radius, float outline, int seed)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float r = Math.Min(radius, Math.Min(width, height) / 2f);
            float line = Math.Max(0f, outline);
            float side = Math.Min(width, height);
            float bevel = Clamp(height * 0.06f, 1f, 4f);
            float lip = height * 0.14f;
            float grain = side * 0.32f;
            float speck = Math.Max(2.5f, side * 0.07f);
            bool moss = Hash(seed, 11, 7) < 0.18f;
            int corner = (int)(Hash(seed, 3, 5) * 4f) % 4;
            float mossX = corner % 2 == 0 ? 0f : width;
            float mossY = corner < 2 ? 0f : height;
            float mossR = side * 0.42f;
            Rgba top = C.StoneTop;
            Rgba face = C.StoneFace;
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float d = RoundRect(x, y, 0f, 0f, width, height, r);
                    float cover = Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float inner = d + line;
                    var c = new Color(top.Mix(face, Smooth(Clamp01((y / height) * 1.4f))));
                    c.Scale(1f + ((Fbm(x / grain, y / grain, seed, 3) - 0.5f) * 0.08f));

                    // Speckles: rare small darker dots, one per hashed cell at most.
                    float cx = (float)Math.Floor(x / speck);
                    float cy = (float)Math.Floor(y / speck);
                    if (Hash((int)cx, (int)cy, seed + 17) < 0.03f)
                    {
                        float ox = (cx + 0.3f + (0.4f * Hash((int)cx, (int)cy, seed + 29))) * speck;
                        float oy = (cy + 0.3f + (0.4f * Hash((int)cy, (int)cx, seed + 31))) * speck;
                        c.Mix(C.StoneLine, 0.15f * Coverage(Length(x - ox, y - oy) - Math.Max(0.7f, speck * 0.16f)));
                    }

                    if (inner > -(lip + 2f))
                    {
                        c.Mix(C.StoneLip, Coverage(inner) * Outside(RoundRect(x, y + lip, 0f, 0f, width, height, r) + line, 1.5f));
                        c.Mix(top.Lighten(0.5f), 0.8f * Coverage(inner) * Outside(RoundRect(x, y - bevel, 0f, 0f, width, height, r) + line, 1f));
                    }

                    c.Mix(C.StoneLine, Clamp01(0.5f + inner));
                    if (moss)
                    {
                        float n = Fbm(x / (side * 0.18f), y / (side * 0.18f), seed + 5, 3);
                        float m = (mossR * (0.55f + (0.7f * n))) - Length(x - mossX, y - mossY);
                        float k = Clamp01(m / (side * 0.08f));
                        if (k > 0f)
                        {
                            Rgba tuft = C.StoneMoss.Lighten(0.22f).Mix(C.StoneMoss.Darken(0.22f), Fbm(x / (side * 0.06f), y / (side * 0.06f), seed + 9, 2));
                            c.Mix(tuft, 0.7f * k);
                        }
                    }

                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// A stone pedestal (the win, Home and Wardrobe heroes stand on it; spec 005 contracts/look.md §3.6): a warm
        /// grey-beige stone drum seen a little from above, as wide as the picture. Its top is an ellipse 28% as tall as
        /// wide, paved in <c>stone.top</c> mixed 0.4 toward <c>stone.face</c>, with a ring joint at 0.72 of its radius,
        /// radial joints outside it and one hairline crack; its side is two courses of blocks shaded from
        /// <c>stone.face</c> darkened 0.06 to <c>stone.lip</c> darkened 0.12, darker toward both sides like a cylinder,
        /// with staggered <c>stone.line</c> joints (alpha 0.55, 1.5% of the height wide), a crack, and <c>stone.moss</c>
        /// tufts on about a third of the base rim; a <c>stone.line</c> outline 2.5% of the height goes around it and along
        /// the top's front edge. (The kit draws the soft ground shadow under it.)
        /// </summary>
        public static byte[] Pedestal(int width, int height, int seed)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float cx = width / 2f;
            float rx = Math.Max(2f, (width / 2f) - 1f);
            float ry = Math.Max(1f, Math.Min(rx * 0.28f, (height - 4f) * 0.3f));
            float ty = ry + 1f;
            float by = Math.Max(ty + 1f, height - ry - 1f);
            float side = by - ty;
            float line = Math.Max(1f, height * 0.025f);
            float joint = Math.Max(0.8f, height * 0.0075f);
            float grain = Math.Max(4f, width * 0.08f);
            Rgba paving = C.StoneTop.Mix(C.StoneFace, 0.4f);
            Rgba sideTop = C.StoneFace.Darken(0.06f);
            Rgba sideBottom = C.StoneLip.Darken(0.12f);
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float dx = x - cx;
                    float dTop = EllipseDistance(dx, y - ty, rx, ry);
                    float dBottom = EllipseDistance(dx, y - by, rx, ry);
                    float dSide = Math.Max(Math.Abs(dx) - rx, Math.Max(ty - y, y - by));
                    float d = Math.Min(dTop, Math.Min(dSide, dBottom));
                    float cover = Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float k = Clamp(dx / rx, -1f, 1f);
                    float front = ry * (float)Math.Sqrt(Math.Max(0f, 1f - (k * k)));
                    float mottle = (Fbm(x / grain, y / grain, seed, 3) - 0.5f) * 0.14f;
                    Color c;
                    if (dTop < 0.5f)
                    {
                        // The paved top: lighter toward the front, a ring joint, radial joints between it and the rim, and
                        // one hairline crack running from the ring toward the back left.
                        float e = Length(dx / rx, (y - ty) / ry);
                        c = new Color(paving.Mix(C.StoneTop, 0.3f * Smooth(Clamp01((y - ty + ry) / (2f * ry)))));
                        c.Scale(1f + mottle);
                        float ring = Math.Abs(e - 0.72f) * ry;
                        c.Mix(C.StoneLine, 0.55f * Clamp01(1f - (ring / joint)));
                        float a = (float)Math.Atan2((y - ty) / ry, dx / rx);
                        if (e > 0.72f)
                        {
                            float step = (float)(Math.PI / 4.0);
                            float nearest = (step * (float)Math.Round((a - 0.2f) / step)) + 0.2f;
                            float off = Math.Abs(a - nearest) * e * ry;
                            c.Mix(C.StoneLine, 0.5f * Clamp01(1f - (off / joint)));
                        }
                        else
                        {
                            float crackAngle = -2.3f + (0.18f * (float)Math.Sin(e * 23f));
                            float crack = Math.Abs(a - crackAngle) * e * ry;
                            c.Mix(C.StoneLine, 0.45f * Clamp01(1f - (crack / (joint * 0.7f))) * Clamp01((e - 0.25f) * 4f));
                        }

                        c.Mix(C.StoneTop.Lighten(0.45f), 0.5f * Clamp01(1f - (Math.Abs(dTop + (line * 1.6f)) / line)));
                        c.Mix(C.StoneLine, Clamp01(0.5f + dTop + line) * (y > ty ? 1f : 0.85f));
                        Mossy(ref c, x, y, seed + 3, width, Clamp01((e - 0.86f) * 5f) * (y > ty ? 0.7f : 0.3f) * MossPatch(dx / rx, seed + 5));
                    }
                    else
                    {
                        // The side: two courses of blocks, shaded like a cylinder lit from the upper left.
                        float t = Clamp01((y - ty - front) / Math.Max(1f, side));
                        c = new Color(sideTop.Mix(sideBottom, Smooth(t)));
                        c.Scale(1f - (0.22f * k * k) - (0.07f * k) + mottle);
                        int course = t < 0.5f ? 0 : 1;
                        float courseLine = Math.Abs(y - (ty + front + (0.5f * side)));
                        c.Mix(C.StoneLine, 0.55f * Clamp01(1f - (courseLine / joint)));
                        float theta = (float)Math.Asin(k);
                        float stepAngle = (float)(Math.PI / 4.0);
                        float offset = course == 0 ? 0.5f : 0f;
                        float nearest = stepAngle * ((float)Math.Round((theta / stepAngle) - offset) + offset);
                        float jointX = Math.Abs(dx - (rx * (float)Math.Sin(nearest)));
                        c.Mix(C.StoneLine, 0.55f * Clamp01(1f - (jointX / joint)) * Clamp01(courseLine / (joint * 2f)));

                        // A hairline crack down the upper course, right of the middle.
                        float crackX = (rx * 0.28f) + (side * 0.06f * (float)Math.Sin(t * 19f));
                        c.Mix(C.StoneLine, 0.4f * Clamp01(1f - (Math.Abs(dx - crackX) / (joint * 0.7f))) * Clamp01((0.48f - t) * 8f));

                        // A light edge just under the top's rim, the lip band along the bottom and moss tufts on part of it.
                        c.Mix(C.StoneTop, 0.35f * Clamp01(1f - (Math.Abs(y - (ty + front + (line * 1.6f))) / line)));
                        c.Mix(C.StoneLip.Darken(0.2f), Smooth(Clamp01((t - 0.86f) / 0.14f)) * 0.5f);
                        Mossy(ref c, x, y, seed, width, Clamp01((t - 0.6f) * 2.6f) * MossPatch(dx / rx, seed));
                        c.Mix(C.StoneLine, Clamp01(0.5f + d + line));
                    }

                    Put(pixels, width, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>Where moss grows along a pedestal's rim (0–1), by the position across it (−1…1): about a third of it.</summary>
        private static float MossPatch(float across, int seed) => Clamp01((Fbm((across * 3f) + 10f, 0.5f, seed + 13, 2) - 0.5f) * 6f);

        /// <summary>
        /// A Garden Entry's stone arch (spec 005 contracts/look.md §3.6): a half ring of nine sandy stone blocks, 28% of its
        /// outer radius thick, around an opening that shows the lawn and a sandy flagstone path fanning out from the base (where
        /// the Bloomlings come out, as in the reference) with a soft shadow under the crown, seen from above,
        /// its crown <paramref name="turns"/> quarter turns clockwise from up (0: the crown up toward a board above it, the
        /// opening facing down). The ring fills the picture's width: for crown up or down a picture twice as wide as tall
        /// holds just the ring; a taller one stands the ring on two straight stone piers, as long as the extra height, so
        /// the arch reads as a doorway standing on the lawn (left or right: the same, turned). The opening is a little
        /// see-through and the path fainter, so the backdrop's lawn shows in it.
        /// </summary>
        public static byte[] Arch(int width, int height, int turns, int seed)
        {
            Check(width, height);
            turns = ((turns % 4) + 4) % 4;
            bool across = turns % 2 == 1;
            float w = across ? height : width;
            float h = across ? width : height;
            var pixels = new byte[width * height * 4];
            const int blocks = 9;
            float outer = Math.Max(2f, Math.Min(w / 2f, h) - 1f);
            float pier = Math.Max(0f, h - (w / 2f));
            float inner = outer * 0.72f;
            float path = inner * 0.86f;
            float jointHalf = Math.Max(0.7f, outer * 0.012f);
            float line = Math.Max(1f, outer * 0.018f);
            float sector = (float)(Math.PI / blocks);
            float grain = Math.Max(4f, outer * 0.15f);
            Rgba gap = C.StoneLine.Darken(0.25f);
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    float y = py + 0.5f;
                    float u;
                    float v;
                    switch (turns)
                    {
                        case 1:
                            u = y;
                            v = h - x;
                            break;
                        case 2:
                            u = w - x;
                            v = h - y;
                            break;
                        case 3:
                            u = w - y;
                            v = x;
                            break;
                        default:
                            u = x;
                            v = y;
                            break;
                    }

                    // uy: up from the ground (the picture's base); ry: up from the ring's base, on top of the piers. Below
                    // the ring the distance runs straight across, so the ring's ends continue down as the piers.
                    float ux = u - (w / 2f);
                    float uy = h - v;
                    float ry = uy - pier;
                    float dist = ry >= 0f ? Length(ux, ry) : Math.Abs(ux);
                    float floor = Clamp01(uy + 0.5f);
                    if (floor <= 0f || dist > outer + 1f)
                    {
                        continue;
                    }

                    // Back to front: the lawn in the opening, the sandy flagstone path the Bloomlings come out on, a soft
                    // shadow under the crown, the dark joints, the stones.
                    var c = new Color(GardenLook.ArchOpening);
                    float alpha = 0.8f * Clamp01(inner + 0.5f - dist);
                    float pathD = Length(ux / (path * 0.82f), uy / path) - 1f;
                    float onPath = Clamp01(0.5f - (pathD * path / Math.Max(1f, outer * 0.025f)));
                    if (onPath > 0f)
                    {
                        var flag = new Color(C.StoneTop.Mix(C.StoneFace, Clamp01(uy / path)));
                        flag.Scale(1f + ((Fbm(x / grain, y / grain, seed + 211, 3) - 0.5f) * 0.1f));

                        // Flagstones: two rings around the base, split into staggered pieces.
                        float rho = Length(ux, uy) / path;
                        float ringJoint = Math.Min(Math.Abs(rho - 0.36f), Math.Abs(rho - 0.7f)) * path;
                        float angle = (float)Math.Atan2(Math.Max(0f, uy), ux);
                        int ringIndex = rho < 0.36f ? 0 : rho < 0.7f ? 1 : 2;
                        float pieces = ringIndex == 0 ? 2f : ringIndex == 1 ? 3f : 4f;
                        float slot = (angle / (float)Math.PI * pieces) + (ringIndex % 2 == 0 ? 0f : 0.5f);
                        float radialJoint = ringIndex == 0 ? float.MaxValue : Math.Abs(slot - (float)Math.Round(slot)) / pieces * (float)Math.PI * rho * path;
                        float joint = Math.Min(ringJoint, radialJoint);
                        flag.Mix(C.StoneLine, 0.45f * Clamp01(1f - (joint / Math.Max(0.8f, outer * 0.012f))));
                        Over(ref c, ref alpha, flag.ToRgba(), 0.6f * onPath * Clamp01(inner + 0.5f - dist));
                    }

                    float under = 1f - Smooth(Clamp01((inner - dist) / (inner * 0.45f)));
                    Over(ref c, ref alpha, C.GardenShadow, 0.42f * under * Smooth(Clamp01(Math.Max(0f, ry) / inner)) * Clamp01(inner + 0.5f - dist));
                    float ring = Clamp01(dist - inner + 0.5f) * Clamp01(outer + 0.5f - dist);
                    Over(ref c, ref alpha, gap, 0.85f * ring);

                    // The ring's blocks, then each pier as one more block under a joint.
                    float d;
                    int block;
                    if (ry >= 0f || pier <= 0f)
                    {
                        float phi = (float)Math.Atan2(Math.Max(0f, ry), ux);
                        block = Math.Min(blocks - 1, Math.Max(0, (int)(phi / sector)));
                        float edge = Math.Min(phi - (block * sector), ((block + 1) * sector) - phi) * dist;
                        d = -Math.Min(Math.Min(dist - inner, outer - dist), Math.Min(edge - jointHalf, pier > 0f ? ry - jointHalf : uy));
                    }
                    else
                    {
                        block = ux < 0f ? blocks + 1 : blocks;
                        d = -Math.Min(Math.Min(dist - inner, outer - dist), Math.Min(-ry - jointHalf, uy));
                    }

                    float cover = Coverage(d);
                    if (cover > 0f)
                    {
                        float radial = Clamp01((dist - inner) / (outer - inner));
                        var stone = new Color(C.StoneFace.Mix(C.StoneTop, 0.6f * radial));
                        stone.Scale(1f + ((Fbm(x / grain, y / grain, seed + (block * 13), 3) - 0.5f) * 0.08f));
                        stone.Mix(C.StoneLip, 0.85f * Clamp01(1f - ((dist - inner) / (line * 2.2f))));
                        stone.Mix(C.StoneTop.Lighten(0.5f), 0.6f * Clamp01(1f - ((outer - dist - line) / line)));
                        stone.Mix(C.StoneLine, Clamp01(0.5f + d + line));
                        Over(ref c, ref alpha, stone.ToRgba(), cover);
                    }

                    if (alpha > 0f)
                    {
                        Put(pixels, width, px, py, c, alpha * floor);
                    }
                }
            }

            return pixels;
        }

        /// <summary>Straight-alpha "over": <paramref name="color"/> at <paramref name="layer"/> over the color and alpha so far.</summary>
        private static void Over(ref Color c, ref float alpha, Rgba color, float layer)
        {
            layer = Clamp01(layer);
            if (layer <= 0f)
            {
                return;
            }

            float result = layer + (alpha * (1f - layer));
            float under = alpha * (1f - layer) / result;
            c.R = (color.R * (layer / result)) + (c.R * under);
            c.G = (color.G * (layer / result)) + (c.G * under);
            c.B = (color.B * (layer / result)) + (c.B * under);
            alpha = result;
        }

        /// <summary>Moss over a stone where <paramref name="amount"/> is high and the noise agrees (pedestal base and rim).</summary>
        private static void Mossy(ref Color c, float x, float y, int seed, int width, float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            float scale = Math.Max(3f, width * 0.05f);
            float n = Fbm(x / scale, y / scale, seed + 77, 3);
            float k = Clamp01((n - (0.62f - (0.3f * amount))) * 6f) * amount;
            if (k > 0f)
            {
                Rgba tuft = C.StoneMoss.Lighten(0.2f).Mix(C.StoneMoss.Darken(0.25f), Fbm(x / (scale * 0.3f), y / (scale * 0.3f), seed + 91, 2));
                c.Mix(tuft, 0.9f * k);
            }
        }

        /// <summary>An approximate signed distance (pixels) to an ellipse centered at the origin: its implicit value over its gradient.</summary>
        private static float EllipseDistance(float x, float y, float rx, float ry)
        {
            float f = ((x * x) / (rx * rx)) + ((y * y) / (ry * ry)) - 1f;
            float gx = 2f * x / (rx * rx);
            float gy = 2f * y / (ry * ry);
            float g = Length(gx, gy);
            return g < 1e-4f ? -Math.Min(rx, ry) : f / g;
        }

        // ---- Candy tiles ----

        /// <summary>
        /// The share of a candy tile's side its lip takes (§3.1), so the kit's press can sink the face into it.
        /// </summary>
        public static float TileLipShare(TileStyle style) => style == TileStyle.Flat ? 0f : 0.07f;

        /// <summary>
        /// A candy tile of side <paramref name="size"/> (spec 005 contracts/look.md §3.1): a satin rounded square in
        /// <paramref name="color"/> with a lighter top, a faint gloss band, a thin lighter bevel inside its top edge, a thin
        /// darker lip, a crisp dark outline, and the variant symbol of <paramref name="iconId"/>: a bold gem with a thick
        /// dark outline, a fill in a shade of the tile and a white highlight (<see cref="TileStyle.Board"/> and
        /// <see cref="TileStyle.Flat"/>, <see cref="ShapeLibrary.GemSymbol"/>, §3.1.2; nearly square, so the board reads as
        /// one mosaic), or a bigger, detailed sticker with a dark outline in its own tone (<see cref="TileStyle.Sticker"/>,
        /// <see cref="ShapeLibrary.SymbolId"/>, §3.1.1). Below 28 px the gem is a flat dark silhouette.
        /// </summary>
        public static byte[] Tile(int size, Rgba color, string iconId, TileStyle style, TileState state = TileState.Normal) =>
            Tile(size, color, iconId, style, state, true);

        /// <summary>
        /// A candy tile's face without its symbol (spec 005 pictures.md G9–G24): the same satin square, bevel, gloss band,
        /// lip and outline as <see cref="Tile"/> in <paramref name="color"/> and <paramref name="state"/> (grey, or dimmed
        /// 45% toward <c>parchment.bottom</c>), for the owner's icon picture drawn over it
        /// (<see cref="OwnerPictures.TileIconBox"/>). A mystery tile keeps its "?".
        /// </summary>
        public static byte[] TileFace(int size, Rgba color, TileStyle style, TileState state = TileState.Normal) =>
            Tile(size, color, "mystery", style, state, state == TileState.Mystery);

        private static byte[] Tile(int size, Rgba color, string iconId, TileStyle style, TileState state, bool withSymbol)
        {
            Check(size, size);
            bool board = style != TileStyle.Sticker;
            bool mystery = state == TileState.Mystery;
            bool grey = state == TileState.Grey;
            bool drawn = withSymbol && !mystery;
            Rgba col = mystery ? C.TileMystery : grey ? color.Grey() : color;
            Sticker sticker = drawn ? Sticker.Of(iconId, color, grey) : default;
            Func<float, float, float>? symbol = mystery ? ShapeLibrary.Get("tile.mystery") : drawn ? ShapeLibrary.Get(ShapeLibrary.SymbolId(iconId)) : null;
            Func<float, float, float>? solid = mystery ? symbol : drawn ? ShapeLibrary.SolidSymbol(iconId) : null;
            Gem gem = board && drawn ? Gem.Of(iconId, col, size) : default;
            float s = size;

            // At least 3 px of corner, so small tiles stay rounded (and their corner pixels clear).
            float r = Math.Max(3f, s * (board ? 0.07f : 0.2f));
            float line = Math.Max(1f, s * (board ? 0.02f : 0.026f));
            float lip = s * TileLipShare(style);
            float faceH = s - lip;
            float bevel = Math.Max(0.75f, s * 0.025f);
            bool small = size < 28;

            // The symbol's box and its scale from shape units to pixels (as ShapeRaster fits a shape into a box). The
            // board's symbol shape covers about 70% of its box, so the bead spans about 42% of the tile.
            float box = s * (board ? 0.60f : 0.66f);
            float sx = s / 2f;
            float sy = (s / 2f) - (s * (board ? 0.02f : 0.01f));
            float unit = box / 2f / ShapeRaster.Margin;
            float iconLine = Math.Max(0.8f, box * (board ? 0.045f : StickerLine));

            Rgba top = col.Lighten(0.36f);
            Rgba bottom = col.Darken(0.14f);
            Rgba lipColor = col.Darken(0.28f);
            Rgba outline = col.Darken(board ? 0.45f : 0.5f);
            Rgba bevelColor = col.Lighten(0.45f);
            Rgba beadFlat = col.Darken(0.32f);
            float bandAlpha = board ? 0.18f : 0.16f;
            Rgba dim = C.ParchmentBottom;
            var pixels = new byte[size * size * 4];
            for (int py = 0; py < size; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f;
                    float d = RoundRect(x, y, 0f, 0f, s, s, r);
                    float cover = Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    // The face over its lip: the tile shape moved up by the lip.
                    float t = Clamp01(y / faceH);
                    var c = new Color(t < 0.6f ? top.Mix(col, t / 0.6f) : col.Mix(bottom, (t - 0.6f) / 0.4f));
                    if (lip > 0f)
                    {
                        c.Mix(lipColor, Outside(RoundRect(x, y + lip, 0f, 0f, s, s, r), 1f));
                    }

                    // Pillowy satin: a faint band fading downward and a lighter bevel just inside the top and left edges,
                    // fading out by 60% of the side.
                    float bandTop = faceH * 0.06f;
                    float bandBottom = faceH * 0.24f;
                    float band = Coverage(RoundRect(x, y, s * 0.1f, bandTop, s * 0.9f, bandBottom, (bandBottom - bandTop) / 2f));
                    c.Mix(Rgba.White, band * bandAlpha * (1f - Clamp01((y - bandTop) / (bandBottom - bandTop))));
                    float topLit = Clamp01(((s * 0.6f) - y) / (s * 0.3f));
                    float leftLit = Clamp01(((s * 0.6f) - x) / (s * 0.3f)) * Clamp01((faceH - y) / (s * 0.25f));
                    float lit = Math.Max(topLit, leftLit);
                    if (lit > 0f)
                    {
                        float edge = Clamp01(1f - (Math.Abs(d + line + bevel) / bevel));
                        c.Mix(bevelColor, 0.8f * edge * lit);
                    }

                    // The symbol, in shape units (y up).
                    float u = (x - sx) / unit;
                    float v = -(y - sy) / unit;
                    if (symbol != null && Math.Abs(u) < 1.3f && Math.Abs(v) < 1.3f)
                    {
                        float ds = symbol(u, v) * unit;
                        if (board)
                        {
                            if (mystery)
                            {
                                c.Mix(Rgba.White, Coverage(ds));
                            }
                        }
                        else if (mystery)
                        {
                            c.Mix(col.Darken(0.35f), Coverage(ds - iconLine));
                            c.Mix(Rgba.White, Coverage(ds));
                        }
                        else if (solid != null)
                        {
                            sticker.Draw(ref c, u, v, ds, solid(u, v) * unit, unit, iconLine, sy, box, y);
                        }
                    }

                    if (board && drawn)
                    {
                        gem.Draw(ref c, x, y, s / 2f, faceH / 2f, small, beadFlat);
                    }

                    c.Mix(outline, Clamp01(0.5f - (-d - line)));
                    if (state == TileState.Dimmed)
                    {
                        c.Mix(dim, 0.45f);
                    }

                    Put(pixels, size, px, py, c, cover);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The board's gem icon (spec 005 FR-026, contracts/look.md §3.1.2): the variant's gem silhouette
        /// (<see cref="ShapeLibrary.GemSymbol"/>) in a box of <see cref="GemBox"/> of the tile, centered on the face, with
        /// a thick dark outline (<see cref="GemLine"/> of the tile, in a deep saturated shade of the tile color:
        /// <see cref="Vivid"/> of the color darkened 0.42, by 1.5) over a faint drop shadow, a vivid fill in a shade of the
        /// tile color (on light colors the color darkened <see cref="GemShade"/>, on darker ones lightened 0.25, then
        /// saturated by <see cref="GemSaturate"/> or 1.4) lighter at the top, its inner line and lighter or darker part
        /// (<see cref="ShapeLibrary.GemDetail"/>), a strong white highlight on its upper left (alpha 0.6) and a specular dot.
        /// Small tiles draw only the silhouette in the flat shade.
        /// </summary>
        private readonly struct Gem
        {
            private readonly Func<float, float, float> _shape;
            private readonly Func<float, float, float>? _detail;
            private readonly Func<float, float, float>? _light;
            private readonly Func<float, float, float>? _dark;
            private readonly Rgba _line;
            private readonly Rgba _top;
            private readonly Rgba _bottom;
            private readonly float _unit;
            private readonly float _width;

            private Gem(Func<float, float, float> shape, Func<float, float, float>? detail, Func<float, float, float>? light, Func<float, float, float>? dark, Rgba color, float size)
            {
                _shape = shape;
                _detail = detail;
                _light = light;
                _dark = dark;
                _line = Vivid(color.Darken(0.42f), 1.5f);
                Rgba fill = Lightness(color) > 0.55f ? Vivid(color.Darken(GemShade), GemSaturate) : Vivid(color.Lighten(0.25f), 1.4f);
                _top = fill.Lighten(0.18f);
                _bottom = fill.Darken(0.06f);
                _unit = size * GemBox / 2f / ShapeRaster.Margin;
                _width = Math.Max(1f, size * GemLine);
            }

            public static Gem Of(string iconId, Rgba color, int size) =>
                new Gem(ShapeLibrary.GemSymbol(iconId), ShapeLibrary.GemDetail(iconId), ShapeLibrary.GemLight(iconId), ShapeLibrary.GemDark(iconId), color, size);

            /// <summary>Draws the gem at pixel (<paramref name="x"/>, <paramref name="y"/>) of a face centered on (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
            public void Draw(ref Color c, float x, float y, float cx, float cy, bool small, Rgba flat)
            {
                float u = (x - cx) / _unit;
                float v = -(y - cy) / _unit;
                float reach = 1f + (_width * 2f / _unit);
                if (Math.Abs(u) > reach || Math.Abs(v) > reach + 0.1f)
                {
                    return;
                }

                float d = _shape(u, v) * _unit;
                if (small)
                {
                    c.Mix(_line.Mix(flat, 0.4f), Coverage(d - (_width * 0.5f)));
                    return;
                }

                // A faint drop shadow under the outline, then the outline, then the fill.
                float shadow = _shape(u, v + (0.06f * 0.8f)) * _unit;
                c.Mix(_line, 0.22f * Coverage(shadow - _width));
                c.Mix(_line, Coverage(d - _width));
                float inside = Coverage(d);
                if (inside <= 0f)
                {
                    return;
                }

                var fill = new Color(_top.Mix(_bottom, Clamp01((0.8f - v) / 1.6f)));
                if (_light != null)
                {
                    fill.Mix(_top.Lighten(0.22f), Coverage(_light(u, v) * _unit));
                }

                if (_dark != null)
                {
                    fill.Mix(_bottom.Darken(0.2f), Coverage(_dark(u, v) * _unit));
                }

                if (_detail != null)
                {
                    fill.Mix(_line, 0.55f * Coverage(_detail(u, v) * _unit));
                }

                // The soft white highlight on the upper left, kept inside the outline, and a tiny specular dot.
                fill.Mix(Rgba.White, 0.6f * Specular(u, v) * Coverage(d + (_width * 0.8f)));
                fill.Mix(Rgba.White, 0.95f * Coverage((Length(u + 0.32f, v - 0.42f) - 0.1f) * _unit) * Coverage(d + (_width * 0.5f)));
                c.Mix(fill.ToRgba(), inside);
            }

            /// <summary>The HSL lightness of a color (0–1).</summary>
            private static float Lightness(Rgba color)
            {
                int max = Math.Max(color.R, Math.Max(color.G, color.B));
                int min = Math.Min(color.R, Math.Min(color.G, color.B));
                return (max + min) / 510f;
            }
        }

        /// <summary>The gem icon's shape box as a share of the board tile's side (the gem with its outline spans about 56% of it).</summary>
        public const float GemBox = 0.64f;

        /// <summary>The gem icon's dark outline as a share of the board tile's side.</summary>
        public const float GemLine = 0.06f;

        /// <summary>A sticker icon's outline as a share of its symbol box (§3.1.1).</summary>
        public const float StickerLine = 0.04f;

        /// <summary>How much darker than its tile a sticker icon's outline is (§3.1.1; Dew's is <c>#1F8D95</c>).</summary>
        public const float StickerLineDarken = 0.45f;

        /// <summary>How much darker than its tile a gem is on a light tile (HSL lightness above 0.55), before <see cref="GemSaturate"/>.</summary>
        public const float GemShade = 0.12f;

        /// <summary>How much a gem's fill on a light tile is saturated away from its grey (<see cref="Vivid"/>).</summary>
        public const float GemSaturate = 1.6f;

        /// <summary>
        /// A more saturated <paramref name="color"/>: each channel pushed away from the color's grey
        /// (<c>0.3 R + 0.59 G + 0.11 B</c>) by <paramref name="k"/> (1 keeps it), clamped, as <c>GardenLook.SpecialFace</c>.
        /// </summary>
        public static Rgba Vivid(Rgba color, float k)
        {
            float g = (0.3f * color.R) + (0.59f * color.G) + (0.11f * color.B);
            byte Push(byte c) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(g + (k * (c - g)))));
            return new Rgba(Push(color.R), Push(color.G), Push(color.B), color.A);
        }

        // ---- Grass cells ----

        /// <summary>How many different grass cell pictures there are (<see cref="GrassSeed"/>).</summary>
        public const int GrassVariants = 4;

        /// <summary>How much a grass cell is inset into its cell (each side, in cells), as the restored ground.</summary>
        public const float GrassInset = 0.025f;

        /// <summary>The grass picture of a board cell (<see cref="GrassVariants"/> of them, so neighbors differ).</summary>
        public static int GrassSeed(int x, int y) => (((x * 7) + (y * 13)) % GrassVariants + GrassVariants) % GrassVariants;

        /// <summary>
        /// A board cell of the picture's background (spec 005 FR-020; <c>tile.grass</c>): a square of lawn of side
        /// <paramref name="size"/>, its face inset by <see cref="GrassInset"/> with a small radius (10%) over a deeper green,
        /// so a soft line parts it from its neighbors, in a muted mid green between <c>lawn.light</c> and <c>lawn.dark</c> with a
        /// soft mottle, short dark and light blades, a faint shadow along its top (it lies a little below the tiles) and a
        /// slightly deeper rim. <paramref name="seed"/> (<see cref="GrassSeed"/>) varies the mottle and the blades. It
        /// stays flat and calm (no bevel, no symbol), so it reads as garden ground, never as a lime Leaf tile.
        /// </summary>
        public static byte[] Grass(int size, int seed) => Grass(size, size, seed);

        /// <summary>A grass cell stretched to <paramref name="width"/> × <paramref name="height"/> (a picture host's quantized box).</summary>
        public static byte[] Grass(int width, int height, int seed)
        {
            Check(width, height);
            float s = Math.Min(width, height);
            float inset = s * GrassInset;
            float r = Math.Max(1.5f, (s - (2f * inset)) * 0.1f);
            Rgba top = C.LawnLight.Mix(C.LawnDark, 0.5f).Darken(0.04f);
            Rgba bottom = C.LawnDark.Darken(0.06f);
            Rgba gap = bottom.Darken(0.3f);
            Rgba blade = C.LawnDark.Darken(0.3f);
            Rgba bladeLight = C.LawnLight.Lighten(0.18f);
            float scale = Math.Max(8f, s);
            var pixels = new byte[width * height * 4];
            for (int py = 0; py < height; py++)
            {
                float y = py + 0.5f;
                for (int px = 0; px < width; px++)
                {
                    float x = px + 0.5f;
                    var c = new Color(gap);
                    float d = RoundRect(x, y, inset, inset, width - inset, height - inset, r);
                    float face = Coverage(d);
                    if (face > 0f)
                    {
                        float u = x / scale;
                        float v = y / scale;
                        var g = new Color(top.Mix(bottom, Clamp01(((y / height) - 0.1f) / 0.9f)));
                        g.Scale(1f + ((Fbm((u * 3f) + seed, (v * 3f) - seed, 301 + seed, 3) - 0.5f) * 0.22f));

                        // Short blades in a fine grid: a dark or light stroke leaning a little in each small cell.
                        const float grid = 0.125f;
                        int gx = (int)Math.Floor(u / grid);
                        int gy = (int)Math.Floor(v / grid);
                        float bx = (gx + 0.2f + (0.6f * Hash(gx, gy, 311 + seed))) * grid;
                        float by = (gy + 0.75f) * grid;
                        float lean = (Hash(gx, gy, 312 + seed) - 0.5f) * 0.12f;
                        float t = Clamp01((by - v) / (grid * 0.55f));
                        float dist = Math.Abs(u - (bx + (lean * t))) - (grid * 0.07f * (1f - (0.7f * t)));
                        float stroke = Coverage(dist * scale) * (v <= by && v >= by - (grid * 0.55f) ? 1f : 0f);
                        g.Mix(Hash(gx, gy, 313 + seed) < 0.6f ? blade : bladeLight, stroke * 0.42f);

                        // It lies a little below the tiles: a faint shadow along its top and a slightly deeper rim.
                        g.Mix(C.GardenShadow, 0.14f * Clamp01(1f - ((y - inset) / (height * 0.22f))));
                        g.Mix(bottom.Darken(0.18f), 0.5f * Clamp01(1f + (d / Math.Max(1f, s * 0.05f))));
                        c.Mix(g.ToRgba(), face);
                    }

                    Put(pixels, width, px, py, c, 1f);
                }
            }

            return pixels;
        }

        /// <summary>The soft white specular on a symbol's upper left (0–1), in shape units: board beads and stickers.</summary>
        private static float Specular(float u, float v)
        {
            float hu = (u + 0.28f) / 0.42f;
            float hv = (v - 0.34f) / 0.24f;
            return Clamp01((1f - Length(hu, hv)) * 3f);
        }

        /// <summary>The sticker look of one variant's symbol (§3.1.1): its fill colors and its detail.</summary>
        private readonly struct Sticker
        {
            private readonly string _icon;
            private readonly Rgba _top;
            private readonly Rgba _bottom;
            private readonly Rgba _line;
            private readonly Rgba _detail;
            private readonly Rgba _second;
            private readonly bool _grey;

            private Sticker(string icon, Rgba top, Rgba bottom, Rgba detail, Rgba second, Rgba line, bool grey)
            {
                _icon = icon;
                _grey = grey;
                _top = Tone(top, grey);
                _bottom = Tone(bottom, grey);
                _line = Tone(line, grey);
                _detail = Tone(detail, grey);
                _second = Tone(second, grey);
            }

            /// <summary>
            /// The sticker of <paramref name="iconId"/> on a tile of <paramref name="color"/>: its own fill colors and detail,
            /// outlined in the tile's own color darkened <see cref="StickerLineDarken"/> (Dew, whose icon is nearly white,
            /// in <c>#1F8D95</c>), so every icon contrasts with its face.
            /// </summary>
            public static Sticker Of(string iconId, Rgba color, bool grey)
            {
                Rgba line = color.Darken(StickerLineDarken);
                return iconId switch
                {
                    "leaf" => new Sticker(iconId, Rgba.FromHex("#5FB84A"), Rgba.FromHex("#2F8C32"), Rgba.FromHex("#8ED86A"), Rgba.White, line, grey),
                    "moss" => new Sticker(iconId, Rgba.FromHex("#22B79C"), Rgba.FromHex("#0D7C68"), Rgba.FromHex("#5FD6BF"), Rgba.White, line, grey),
                    "flower" => new Sticker(iconId, Rgba.FromHex("#FFD2E2"), Rgba.FromHex("#F79AC0"), Rgba.FromHex("#FFD35C"), Rgba.FromHex("#F08A24"), line, grey),
                    "bud" => new Sticker(iconId, Rgba.FromHex("#C58BF5"), Rgba.FromHex("#8E4BD8"), Rgba.FromHex("#E2C4FF"), Rgba.FromHex("#5BAA3A"), line, grey),
                    "drop" => new Sticker(iconId, Rgba.FromHex("#2F8EF5"), Rgba.FromHex("#0B4FB0"), Rgba.White, Rgba.White, line, grey),
                    "dew" => new Sticker(iconId, Rgba.FromHex("#E6FFFF"), Rgba.FromHex("#9EEFF3"), Rgba.White, Rgba.FromHex("#3FAFB8"), Rgba.FromHex("#1F8D95"), grey),
                    "log" => new Sticker(iconId, Rgba.FromHex("#C47A3C"), Rgba.FromHex("#7A3A12"), Rgba.FromHex("#E3A566"), Rgba.FromHex("#6A3210"), line, grey),
                    "acorn" => new Sticker(iconId, Rgba.FromHex("#E39A4A"), Rgba.FromHex("#A35A18"), Rgba.FromHex("#7A4A22"), Rgba.FromHex("#4F2C10"), line, grey),
                    _ => new Sticker(iconId, color.Lighten(0.2f), color.Darken(0.15f), color, color, line, grey),
                };
            }

            /// <summary>Draws the outline (in the icon's own dark tone), the fill, the detail and the highlight at one point of the symbol.</summary>
            public void Draw(ref Color c, float u, float v, float d, float solid, float unit, float line, float sy, float box, float y)
            {
                c.Mix(_line, Coverage(d - line));
                float inside = Coverage(solid);
                if (inside <= 0f)
                {
                    return;
                }

                float g = Clamp01((y - (sy - (box / 2f))) / box);
                var fill = new Color(_top.Mix(_bottom, g));
                Detail(ref fill, u, v, unit, line);

                // A soft white highlight on the symbol's upper left.
                float highlight = Specular(u, v) * Coverage(solid + (line * 2.5f));
                fill.Mix(Rgba.White, 0.4f * highlight);
                c.Mix(fill.ToRgba(), inside);
            }

            private void Detail(ref Color fill, float u, float v, float unit, float line)
            {
                float px = 1f / unit; // one pixel in shape units
                switch (_icon)
                {
                    case "leaf":
                    {
                        float bx = ShapeLibrary.LeafBaseX;
                        float by = ShapeLibrary.LeafBaseY;
                        float dx = ShapeLibrary.LeafTipX - bx;
                        float dy = ShapeLibrary.LeafTipY - by;
                        float vein = Segment(u, v, bx + (0.08f * dx), by + (0.08f * dy), bx + (0.84f * dx), by + (0.84f * dy)) - 0.04f;
                        float nx = -dy * 0.3f;
                        float ny = dx * 0.3f;
                        float ax = bx + (0.38f * dx);
                        float ay = by + (0.38f * dy);
                        float cx = bx + (0.58f * dx);
                        float cy = by + (0.58f * dy);
                        vein = Math.Min(vein, Segment(u, v, ax, ay, ax + nx + (0.12f * dx), ay + ny + (0.12f * dy)) - 0.03f);
                        vein = Math.Min(vein, Segment(u, v, cx, cy, cx - nx + (0.12f * dx), cy - ny + (0.12f * dy)) - 0.03f);
                        vein = Math.Max(vein, ShapeLibrary.SolidSymbol("leaf")(u, v) + 0.12f);
                        fill.Mix(_detail, Coverage(vein / px));
                        break;
                    }

                    case "moss":
                        foreach ((float x, float y, float r) in ShapeLibrary.MossDimples)
                        {
                            // Faint: the reference's moss is a nearly uniform cushion.
                            fill.Mix(_detail, 0.2f * Coverage((Length(u - x, v - y) - r) / px));
                            fill.Mix(_bottom, 0.15f * Coverage((Length(u - x, v - y + (r * 0.55f)) - (r * 0.75f)) / px) * Outside((Length(u - x, v - y) - r) / px, 1f));
                        }

                        break;
                    case "flower":
                    {
                        float center = Length(u, v);
                        fill.Mix(_detail.Darken(0.2f), Coverage((center - 0.27f) / px));
                        fill.Mix(_detail, Coverage((center - 0.22f) / px));
                        fill.Mix(_second, Coverage((Length(u + 0.03f, v - 0.03f) - 0.08f) / px));
                        break;
                    }

                    case "bud":
                    {
                        float mid = Math.Max(Segment(u, v, 0f, -0.42f, 0f, 0.66f) - 0.035f, ShapeLibrary.SolidSymbol("bud")(u, v) + 0.1f);
                        fill.Mix(_detail, 0.85f * Coverage(mid / px));
                        float sepal = ShapeLibrary.BudSepals(u, v);
                        fill.Mix(_second.Darken(0.3f), Coverage(sepal / px));
                        fill.Mix(_second, Coverage((sepal + (line / unit)) / px));
                        break;
                    }

                    case "drop":
                    {
                        float arc = Math.Abs(Length(u, v - ShapeLibrary.WaterCenterY) - (ShapeLibrary.WaterRadius * 0.68f)) - 0.055f;
                        arc = Math.Max(arc, Math.Max(u + 0.06f, -(v - ShapeLibrary.WaterCenterY + 0.08f)));
                        fill.Mix(_detail, 0.9f * Coverage(arc / px));
                        break;
                    }

                    case "dew":
                    {
                        // The white sparkle with a thin outline in the icon's own line color.
                        float sparkle = ShapeLibrary.DewSparkle(u, v);
                        fill.Mix(_line, Coverage((sparkle - (line * 0.6f / unit)) / px));
                        fill.Mix(_detail, Coverage(sparkle / px));
                        float arc = Math.Abs(Length(u + 0.14f, v + 0.24f) - 0.36f) - 0.05f;
                        arc = Math.Max(arc, Math.Max(u + 0.18f, -(v + 0.2f)));
                        fill.Mix(_detail, 0.8f * Coverage(arc / px));
                        break;
                    }

                    case "log":
                    {
                        float topFace = ShapeLibrary.StumpTop(u, v);
                        fill.Mix(_detail, Coverage((topFace + (line / unit)) / px));
                        float ry = ShapeLibrary.StumpRy;
                        float rx = ShapeLibrary.StumpRx;
                        float ring1 = Math.Abs(Ellipse(u, v - ShapeLibrary.StumpTopY, rx * 0.64f, ry * 0.64f)) - 0.03f;
                        float ring2 = Math.Abs(Ellipse(u, v - ShapeLibrary.StumpTopY, rx * 0.3f, ry * 0.3f)) - 0.03f;
                        fill.Mix(_second, 0.85f * Coverage(Math.Min(ring1, ring2) / px));
                        fill.Mix(_line, Coverage((Math.Abs(topFace) - (line / unit * 0.6f)) / px) * Clamp01((ShapeLibrary.StumpTopY - v) / 0.05f));
                        break;
                    }

                    case "acorn":
                    {
                        float cap = ShapeLibrary.AcornCap(u, v);
                        if (cap < 2f * px)
                        {
                            var capColor = new Color(_detail.Lighten(0.12f).Mix(_detail.Darken(0.1f), Clamp01((0.7f - v) / 0.7f)));
                            fill.Mix(capColor.ToRgba(), Coverage(cap / px));
                            float edge = Math.Abs(v - (ShapeLibrary.AcornCapY + 0.02f)) - 0.035f;
                            fill.Mix(_second, Coverage(Math.Max(edge, cap) / px));
                        }

                        break;
                    }
                }
            }
        }

        // ---- Wood material ----

        /// <summary>The plank material of one tone: its colors and its grain, built once per picture.</summary>
        private sealed class Wood
        {
            private readonly Rgba _top;
            private readonly Rgba _bottom;
            private readonly Rgba _grain;
            private readonly int _height;
            private readonly int _width;
            private readonly int _seed;
            private readonly float _scale;
            private readonly float[] _lineY;
            private readonly float[] _lineFrom;
            private readonly float[] _lineTo;

            // The wavy lines' offsets (fractions of the thickness) along x for planks and along y for frame sides.
            private readonly float[][] _waveX;
            private readonly float[][] _waveY;
            private readonly bool _knot;
            private readonly float _knotX;
            private readonly float _knotY;

            private Wood(Rgba top, Rgba bottom, Rgba grain, Rgba lip, Rgba line, int width, int height, int seed)
            {
                _top = top;
                _bottom = bottom;
                _grain = grain;
                Lip = lip;
                Line = line;
                Light = top.Lighten(0.4f);
                _width = width;
                _height = height;
                _seed = seed;
                _scale = Math.Max(6f, Math.Min(height, width) * 0.9f);
                int count = 6 + (int)(Hash(seed, 1, 2) * 4f);
                _lineY = new float[count];
                _lineFrom = new float[count];
                _lineTo = new float[count];
                _waveX = new float[count][];
                _waveY = new float[count][];
                for (int i = 0; i < count; i++)
                {
                    _lineY[i] = 0.16f + (0.66f * (i + Hash(seed, i, 3)) / count);
                    _lineFrom[i] = 0.04f + (0.4f * Hash(seed, i, 7));
                    _lineTo[i] = _lineFrom[i] + 0.3f + (0.5f * Hash(seed, i, 8));
                    float amp = 0.004f + (0.008f * Hash(seed, i, 4));
                    float freq = (1.5f + (2.5f * Hash(seed, i, 5))) / Math.Max(1f, Math.Min(width, height) * 2.2f);
                    float phase = 6.2832f * Hash(seed, i, 6);
                    _waveX[i] = Waves(width, amp, freq, phase);
                    _waveY[i] = Waves(height, amp, freq, phase);
                }

                _knot = Hash(seed, 9, 9) < 0.6f;
                _knotX = 0.2f + (0.6f * Hash(seed, 9, 10));
                _knotY = 0.35f + (0.3f * Hash(seed, 9, 11));
            }

            public Rgba Lip { get; }

            public Rgba Line { get; }

            public Rgba Light { get; }

            public static Wood Of(WoodTone tone, int width, int height, int seed) => tone == WoodTone.Light
                ? new Wood(C.WoodLight, C.WoodMid, C.WoodGrain, C.WoodEdge, C.WoodLine, width, height, seed)
                : new Wood(C.WoodDarkTop, C.WoodDark, C.WoodDark.Darken(0.32f), C.WoodDark.Darken(0.25f), C.WoodDarkLine, width, height, seed);

            /// <summary>The grained face at pixel (px, py); a frame's side members pass <paramref name="horizontal"/> false.</summary>
            public Rgba Face(int px, int py, bool horizontal)
            {
                float x = px + 0.5f;
                float y = py + 0.5f;
                float along = horizontal ? x : y;
                float across = horizontal ? y : x;
                float length = horizontal ? _width : _height;
                float thickness = horizontal ? _height : _width;
                var c = new Color(_top.Mix(_bottom, Smooth(y / _height)));

                // Many fine, faint streaks: noise stretched along the grain, and a gentle unevenness.
                float streak = Fbm(along / (_scale * 1.6f), across / (_scale * 0.07f), _seed, 3);
                c.Mix(_grain, 0.4f * Clamp01((streak - 0.42f) * 2.4f));
                c.Scale(1f + ((Noise(along / (_scale * 0.8f), across / (_scale * 0.4f), _seed + 41) - 0.5f) * 0.08f));

                // Thin, faint, barely wavy lines across part of the length.
                float width = 0.6f + (thickness * 0.004f);
                float k = along / length;
                for (int i = 0; i < _lineY.Length; i++)
                {
                    if (k < _lineFrom[i] || k > _lineTo[i])
                    {
                        continue;
                    }

                    float wave = horizontal ? _waveX[i][px] : _waveY[i][py];
                    float dist = Math.Abs(across - ((_lineY[i] + wave) * thickness));
                    if (dist < width)
                    {
                        float fade = Clamp01((k - _lineFrom[i]) / 0.08f) * Clamp01((_lineTo[i] - k) / 0.08f);
                        c.Mix(_grain, 0.22f * fade * (1f - (dist / width)));
                    }
                }

                if (_knot && horizontal)
                {
                    float kx = (x - (_knotX * _width)) / (_height * 0.13f);
                    float ky = (y - (_knotY * _height)) / (_height * 0.06f);
                    float e = Length(kx, ky);
                    if (e < 2f)
                    {
                        c.Mix(_grain, 0.4f * Clamp01((1f - e) * 3f));
                        c.Mix(_grain, 0.35f * Clamp01(1f - (Math.Abs(e - 1.6f) * 3f)));
                    }
                }

                return c.ToRgba();
            }

            private static float[] Waves(int count, float amp, float freq, float phase)
            {
                var waves = new float[count];
                for (int i = 0; i < count; i++)
                {
                    waves[i] = amp * (float)Math.Sin((((i + 0.5f) * freq) * 6.2832f) + phase);
                }

                return waves;
            }
        }

        // ---- Pixels ----

        /// <summary>An opaque working color in 0–255 floats.</summary>
        private struct Color
        {
            public float R;
            public float G;
            public float B;

            public Color(Rgba c)
            {
                R = c.R;
                G = c.G;
                B = c.B;
            }

            /// <summary>Blends toward <paramref name="c"/> by <paramref name="t"/> times its alpha.</summary>
            public void Mix(Rgba c, float t)
            {
                t = Clamp01(t) * (c.A / 255f);
                if (t <= 0f)
                {
                    return;
                }

                R += (c.R - R) * t;
                G += (c.G - G) * t;
                B += (c.B - B) * t;
            }

            /// <summary>Multiplies the brightness (mottling).</summary>
            public void Scale(float k)
            {
                R = Math.Min(255f, R * k);
                G = Math.Min(255f, G * k);
                B = Math.Min(255f, B * k);
            }

            public Rgba ToRgba() => new Rgba(Byte(R), Byte(G), Byte(B));
        }

        private static void Put(byte[] pixels, int width, int px, int py, Color c, float alpha)
        {
            int i = ((py * width) + px) * 4;
            pixels[i] = Byte(c.R);
            pixels[i + 1] = Byte(c.G);
            pixels[i + 2] = Byte(c.B);
            pixels[i + 3] = Byte(Clamp01(alpha) * 255f);
        }

        private static byte Byte(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));

        /// <summary>
        /// Draws a straight-alpha RGBA picture (<paramref name="source"/>, <paramref name="sourceWidth"/> ×
        /// <paramref name="sourceHeight"/>, rows from the top) over another (<paramref name="target"/>) into
        /// <paramref name="box"/> (target pixels, top-down), its aspect kept and centered: each target pixel averages the
        /// source pixels it covers (premultiplied), so a 256 px icon stays smooth on a 30 px tile, and the edges of the box
        /// blend by their coverage. The finished picture bakes the owner's field icons into its flat tiles this way
        /// (spec 005 pictures.md G17–G24).
        /// </summary>
        public static void DrawOver(byte[] target, int width, int height, byte[] source, int sourceWidth, int sourceHeight, Box box)
        {
            if (sourceWidth < 1 || sourceHeight < 1 || box.Width <= 0f || box.Height <= 0f || source.Length < sourceWidth * sourceHeight * 4)
            {
                return;
            }

            float scale = Math.Min(box.Width / sourceWidth, box.Height / sourceHeight);
            float w = sourceWidth * scale;
            float h = sourceHeight * scale;
            float left = box.CenterX - (w / 2f);
            float top = box.CenterY - (h / 2f);
            int x0 = Math.Max(0, (int)Math.Floor(left));
            int x1 = Math.Min(width, (int)Math.Ceiling(left + w));
            int y0 = Math.Max(0, (int)Math.Floor(top));
            int y1 = Math.Min(height, (int)Math.Ceiling(top + h));
            for (int py = y0; py < y1; py++)
            {
                float sy0 = Math.Max(0f, (py - top) / scale);
                float sy1 = Math.Min(sourceHeight, (py + 1 - top) / scale);
                float coverY = Math.Min(py + 1f, top + h) - Math.Max(py, top);
                for (int px = x0; px < x1; px++)
                {
                    float sx0 = Math.Max(0f, (px - left) / scale);
                    float sx1 = Math.Min(sourceWidth, (px + 1 - left) / scale);
                    float area = (sx1 - sx0) * (sy1 - sy0);
                    if (area <= 0f)
                    {
                        continue;
                    }

                    // The premultiplied average of the source pixels under this target pixel.
                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    float a = 0f;
                    for (int iy = (int)sy0; iy < sy1; iy++)
                    {
                        float wy = Math.Min(iy + 1f, sy1) - Math.Max(iy, sy0);
                        for (int ix = (int)sx0; ix < sx1; ix++)
                        {
                            float weight = wy * (Math.Min(ix + 1f, sx1) - Math.Max(ix, sx0));
                            int s = ((iy * sourceWidth) + ix) * 4;
                            float sa = source[s + 3] * weight;
                            r += source[s] * sa;
                            g += source[s + 1] * sa;
                            b += source[s + 2] * sa;
                            a += sa;
                        }
                    }

                    float cover = coverY * (Math.Min(px + 1f, left + w) - Math.Max(px, left));
                    float alpha = Clamp01(a / area / 255f * cover);
                    if (alpha <= 0f)
                    {
                        continue;
                    }

                    // Source over target, in straight alpha.
                    int t = ((py * width) + px) * 4;
                    float ta = target[t + 3] / 255f;
                    float outA = alpha + (ta * (1f - alpha));
                    float keep = ta * (1f - alpha);
                    target[t] = Byte(((r / a * alpha) + (target[t] * keep)) / outA);
                    target[t + 1] = Byte(((g / a * alpha) + (target[t + 1] * keep)) / outA);
                    target[t + 2] = Byte(((b / a * alpha) + (target[t + 2] * keep)) / outA);
                    target[t + 3] = Byte(outA * 255f);
                }
            }
        }

        private static void Check(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A picture needs at least one pixel.");
            }
        }

        private static Rgba Tone(Rgba c, bool grey) => grey ? c.Grey() : c;

        // ---- Distances and noise ----

        /// <summary>The signed distance (pixels, negative inside) to a rounded rectangle from (l, t) to (r, b).</summary>
        private static float RoundRect(float x, float y, float l, float t, float r, float b, float radius)
        {
            float hx = (r - l) / 2f;
            float hy = (b - t) / 2f;
            float rad = Math.Min(radius, Math.Min(hx, hy));
            float qx = Math.Abs(x - ((l + r) / 2f)) - hx + rad;
            float qy = Math.Abs(y - ((t + b) / 2f)) - hy + rad;
            return Length(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - rad;
        }

        /// <summary>The coverage of a pixel at signed distance <paramref name="d"/> (pixels): a one-pixel anti-aliased edge.</summary>
        private static float Coverage(float d) => Clamp01(0.5f - d);

        /// <summary>How far outside a shape a point is, 0 to 1 over <paramref name="soft"/> pixels.</summary>
        private static float Outside(float d, float soft) => Clamp01(0.5f + (d / soft));

        private static float Ellipse(float x, float y, float rx, float ry) => (Length(x / rx, y / ry) - 1f) * Math.Min(rx, ry);

        private static float Segment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float t = Clamp01((((px - ax) * dx) + ((py - ay) * dy)) / ((dx * dx) + (dy * dy)));
            return Length(px - (ax + (t * dx)), py - (ay + (t * dy)));
        }

        private static float Length(float x, float y) => (float)Math.Sqrt((x * x) + (y * y));

        private static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Smooth(float t) => t * t * (3f - (2f * t));

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

        private static int Floor(float v)
        {
            int i = (int)v;
            return v < i ? i - 1 : i;
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
