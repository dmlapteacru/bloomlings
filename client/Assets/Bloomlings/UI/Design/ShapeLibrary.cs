using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Every placeholder shape of the game as a signed distance function (research R2): x and y in −1..1, y up, negative
    /// inside. A shape's id is the id of the asset slot it stands in for (<see cref="AssetSlots"/>), so final art can
    /// replace it one to one. The shapes are drawn without asset files (FR-002):
    /// <list type="bullet">
    /// <item><description>panels and tiles;</description></item>
    /// <item><description>one distinct symbol per variant (FR-005, FR-072);</description></item>
    /// <item><description>the family silhouettes;</description></item>
    /// <item><description>specials, boosters and cosmetics;</description></item>
    /// <item><description>the UI glyphs of the design board.</description></item>
    /// </list>
    /// <see cref="ShapeRaster"/> turns a shape into an alpha mask; the Unity client and the playtest tint and draw it.
    /// Engine-free.
    /// </summary>
    public static class ShapeLibrary
    {
        /// <summary>The id drawn for an unknown shape.</summary>
        public const string Fallback = "tile.mystery";

        private static readonly Dictionary<string, Func<float, float, float>> Shapes = Build();

        /// <summary>Every base shape id (composites such as a skin on a family are derived from these).</summary>
        public static IReadOnlyCollection<string> Ids => Shapes.Keys;

        public static bool Has(string id) => Shapes.ContainsKey(id);

        /// <summary>The distance function of a shape; an unknown id gives the "?" shape.</summary>
        public static Func<float, float, float> Get(string id) => Shapes.TryGetValue(id, out Func<float, float, float>? sdf) ? sdf : Shapes[Fallback];

        /// <summary>The symbol id of a variant's icon (<see cref="VariantInfo.IconId"/>): <c>symbol.leaf</c>, <c>symbol.bud</c>, …</summary>
        public static string SymbolId(string iconId) => "symbol." + iconId;

        /// <summary>The body silhouette id of a family: <c>char.sprig</c>, …</summary>
        public static string SilhouetteId(Family family) => "char." + family.ToString().ToLowerInvariant();

        /// <summary>The cosmetic shape id of a <c>CosmeticCatalog</c> shape: <c>cosmetic.cap</c>, …</summary>
        public static string CosmeticId(string shape) => shape switch
        {
            "frame" => "cosmetic.frame",
            "badge" => "cosmetic.badge",
            "marker" => "cosmetic.marker",
            _ => "cosmetic." + shape,
        };

        /// <summary>A skin pattern cut to a family's body (FR-063): a composite of a <c>char.*</c> and a <c>cosmetic.*</c> slot.</summary>
        public static Func<float, float, float> SkinOn(Family family, string shape) =>
            (x, y) => Max(SilhouetteSdf(family, x, y) + 0.04f, SkinPatternSdf(shape, x, y));

        /// <summary>A skin pattern over its whole box, for a picture to mask (spec 004: skins on the character pictures).</summary>
        public static Func<float, float, float> SkinPattern(string shape) => (x, y) => SkinPatternSdf(shape, x, y);

        /// <summary>
        /// One part of the <c>ui.deco.garden</c> cluster, grown by <paramref name="grow"/> (positive: its outline ring),
        /// turned half way when <paramref name="flipped"/> (the bottom-right corner).
        /// </summary>
        public static Func<float, float, float> DecorationPartSdf(DecorationPart part, float grow, bool flipped) =>
            flipped ? (x, y) => DecorationSdf(part, -x, -y) - grow : (Func<float, float, float>)((x, y) => DecorationSdf(part, x, y) - grow);

        /// <summary>How many clover leaves make the ivy cluster of a wooden sign (<c>ui.sign.ivy</c>, spec 005 §3.9).</summary>
        public const int IvyLeafCount = 6;

        /// <summary>
        /// One clover leaf of the ivy cluster (<c>ui.sign.ivy</c>), grown by <paramref name="grow"/> (positive: its
        /// outline ring), mirrored left to right when <paramref name="flipped"/> (the sign's right end). Leaves are listed
        /// back to front.
        /// </summary>
        public static Func<float, float, float> IvyLeafSdf(int leaf, float grow, bool flipped) =>
            flipped ? (x, y) => IvyLeaf(leaf, -x, y) - grow : (Func<float, float, float>)((x, y) => IvyLeaf(leaf, x, y) - grow);

        /// <summary>
        /// The midribs of one clover leaf's three leaflets (a thin line from its middle toward each tip), as a distance
        /// <paramref name="half"/> wide, mirrored like <see cref="IvyLeafSdf"/>.
        /// </summary>
        public static Func<float, float, float> IvyVeinSdf(int leaf, float half, bool flipped) =>
            flipped ? (x, y) => IvyVeins(leaf, -x, y) - half : (Func<float, float, float>)((x, y) => IvyVeins(leaf, x, y) - half);

        /// <summary>
        /// The ivy cluster's leaves: center, size and turn. Two groups gather at the top and bottom corners of a plank's end
        /// (around y 0.55 and −0.5) with one leaf bridging them.
        /// </summary>
        private static readonly (float X, float Y, float R, float Turn)[] IvyLeaves =
        {
            (-0.16f, 0.6f, 0.4f, 0.5f),
            (0.22f, 0.44f, 0.34f, 1.25f),
            (-0.04f, 0.02f, 0.32f, 2.3f),
            (-0.12f, -0.46f, 0.42f, 0.95f),
            (0.26f, -0.62f, 0.34f, 1.75f),
            (-0.3f, -0.74f, 0.32f, 0.2f),
        };

        /// <summary>A clover leaf: three pointed, heart-like leaflets from its middle, their tips outward.</summary>
        private static float IvyLeaf(int leaf, float x, float y)
        {
            (float cx, float cy, float r, float turn) = IvyLeaves[((leaf % IvyLeafCount) + IvyLeafCount) % IvyLeafCount];
            float d = Length(x - cx, y - cy) - (0.2f * r);
            for (int k = 0; k < 3; k++)
            {
                float a = turn + (k * 2f * MathF.PI / 3f);
                d = MathF.Min(d, LensSdf(x, y, cx, cy, cx + (0.95f * r * MathF.Cos(a)), cy + (0.95f * r * MathF.Sin(a)), 0.38f * r));
            }

            return d;
        }

        /// <summary>The distance to a clover leaf's three midribs (from its middle to 80% of each leaflet).</summary>
        private static float IvyVeins(int leaf, float x, float y)
        {
            (float cx, float cy, float r, float turn) = IvyLeaves[((leaf % IvyLeafCount) + IvyLeafCount) % IvyLeafCount];
            float d = float.MaxValue;
            for (int k = 0; k < 3; k++)
            {
                float a = turn + (k * 2f * MathF.PI / 3f);
                d = MathF.Min(d, Segment(x, y, cx + (0.12f * r * MathF.Cos(a)), cy + (0.12f * r * MathF.Sin(a)), cx + (0.76f * r * MathF.Cos(a)), cy + (0.76f * r * MathF.Sin(a))));
            }

            return d;
        }

        /// <summary>How many leaves make a flower cluster at a win sign's ends (spec 005 §3.9); two flowers lie over them.</summary>
        public const int FlowerClusterLeafCount = 5;

        /// <summary>How many flowers a flower cluster has.</summary>
        public const int FlowerClusterFlowerCount = 2;

        /// <summary>
        /// One leaf of a flower cluster (the win sign's ends, spec 005 §3.9): a broad almond fanned out from the cluster's
        /// base toward the upper left, left and lower left, grown by <paramref name="grow"/>, turned half way when
        /// <paramref name="flipped"/> (the bottom-right end). Leaves are listed back to front.
        /// </summary>
        public static Func<float, float, float> ClusterLeafSdf(int leaf, float grow, bool flipped) =>
            flipped ? (x, y) => ClusterLeaf(leaf, -x, -y) - grow : (Func<float, float, float>)((x, y) => ClusterLeaf(leaf, x, y) - grow);

        /// <summary>A flower cluster leaf's midrib, a line <paramref name="half"/> wide, turned like <see cref="ClusterLeafSdf"/>.</summary>
        public static Func<float, float, float> ClusterVeinSdf(int leaf, float half, bool flipped) =>
            flipped ? (x, y) => ClusterVein(leaf, -x, -y) - half : (Func<float, float, float>)((x, y) => ClusterVein(leaf, x, y) - half);

        /// <summary>
        /// One white flower of a flower cluster: its five round petals (<paramref name="center"/> false) or its round middle
        /// (true), grown by <paramref name="grow"/>, turned like <see cref="ClusterLeafSdf"/>.
        /// </summary>
        public static Func<float, float, float> ClusterFlowerSdf(int flower, bool center, float grow, bool flipped) =>
            flipped ? (x, y) => ClusterFlower(flower, center, -x, -y) - grow : (Func<float, float, float>)((x, y) => ClusterFlower(flower, center, x, y) - grow);

        /// <summary>The cluster's leaves: direction (degrees, y up), length and half-width, all from the base point.</summary>
        private static readonly (float Angle, float Length, float Half)[] ClusterLeaves =
        {
            (108f, 1.0f, 0.3f),
            (136f, 1.06f, 0.33f),
            (162f, 0.94f, 0.3f),
            (188f, 1.0f, 0.31f),
            (214f, 0.8f, 0.26f),
        };

        /// <summary>The cluster's flowers: center, radius and turn (the big one over the leaves, the small one at the corner).</summary>
        private static readonly (float X, float Y, float R, float Turn)[] ClusterFlowers =
        {
            (-0.34f, -0.04f, 0.37f, 0.2f),
            (0.3f, 0.5f, 0.28f, 0.75f),
        };

        private const float ClusterBaseX = 0.12f;
        private const float ClusterBaseY = -0.08f;

        private static (float X, float Y) ClusterLeafTip(int leaf)
        {
            (float Angle, float Length, float Half) shape = ClusterLeaves[((leaf % FlowerClusterLeafCount) + FlowerClusterLeafCount) % FlowerClusterLeafCount];
            float a = shape.Angle * MathF.PI / 180f;
            return (ClusterBaseX + (shape.Length * MathF.Cos(a)), ClusterBaseY + (shape.Length * MathF.Sin(a)));
        }

        private static float ClusterLeaf(int leaf, float x, float y)
        {
            (float tx, float ty) = ClusterLeafTip(leaf);
            float half = ClusterLeaves[((leaf % FlowerClusterLeafCount) + FlowerClusterLeafCount) % FlowerClusterLeafCount].Half;
            return LensSdf(x, y, ClusterBaseX, ClusterBaseY, tx, ty, half);
        }

        private static float ClusterVein(int leaf, float x, float y)
        {
            (float tx, float ty) = ClusterLeafTip(leaf);
            float dx = tx - ClusterBaseX;
            float dy = ty - ClusterBaseY;
            return Segment(x, y, ClusterBaseX + (0.12f * dx), ClusterBaseY + (0.12f * dy), ClusterBaseX + (0.84f * dx), ClusterBaseY + (0.84f * dy));
        }

        private static float ClusterFlower(int flower, bool center, float x, float y)
        {
            (float cx, float cy, float r, float turn) = ClusterFlowers[((flower % FlowerClusterFlowerCount) + FlowerClusterFlowerCount) % FlowerClusterFlowerCount];
            if (center)
            {
                return Length(x - cx, y - cy) - (0.3f * r);
            }

            float d = Length(x - cx, y - cy) - (0.45f * r);
            for (int i = 0; i < 5; i++)
            {
                float a = turn + (i * 2f * MathF.PI / 5f);
                d = MathF.Min(d, Length(x - cx - (0.55f * r * MathF.Cos(a)), y - cy - (0.55f * r * MathF.Sin(a))) - (0.42f * r));
            }

            return d;
        }

        /// <summary>The whole ivy cluster (the slot's one-color silhouette).</summary>
        private static float IvyCluster(float x, float y)
        {
            float d = float.MaxValue;
            for (int i = 0; i < IvyLeafCount; i++)
            {
                d = MathF.Min(d, IvyLeaf(i, x, y));
            }

            return d;
        }

        /// <summary>A part of the decoration in the cluster's square (the mockup's 120 × 100 SVG, centered, 60 per unit).</summary>
        private static float DecorationSdf(DecorationPart part, float x, float y)
        {
            // The leaves fan out from (0, −0.13) to the upper left, up, and right.
            const float bx = 0f;
            const float by = -0.13f;
            switch (part)
            {
                case DecorationPart.LeafA:
                    return LensSdf(x, y, bx, by, -0.95f, 0.25f, 0.23f);
                case DecorationPart.LeafB:
                    return LensSdf(x, y, bx, by, 0.45f, 0.8f, 0.23f);
                case DecorationPart.LeafC:
                    return LensSdf(x, y, bx, by, 0.97f, -0.2f, 0.21f);
                case DecorationPart.Petals:
                {
                    float d = float.MaxValue;
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (MathF.PI / 2f) + (i * 2f * MathF.PI / 5f);
                        float c = MathF.Cos(a);
                        float sn = MathF.Sin(a);
                        float u = ((x - bx) * c) + ((y - by + 0.03f) * sn) - 0.2f;
                        float v = (-(x - bx) * sn) + ((y - by + 0.03f) * c);
                        d = MathF.Min(d, (Length(u / 0.21f, v / 0.15f) - 1f) * 0.15f);
                    }

                    return d;
                }

                default:
                    return Length(x - bx, y - by + 0.03f) - 0.12f;
            }
        }

        /// <summary>A leaf: a lens from (ax, ay) to (bx, by) with half-width <paramref name="half"/>.</summary>
        private static float LensSdf(float x, float y, float ax, float ay, float bx, float by, float half)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float length = MathF.Sqrt((dx * dx) + (dy * dy));
            float along = ((((x - ax) * dx) + ((y - ay) * dy)) / length) - (length / 2f);
            float across = (((x - ax) * -dy) + ((y - ay) * dx)) / length;
            float l = length / 2f;
            float radius = ((l * l) + (half * half)) / (2f * half);
            float offset = radius - half;
            return Max(Length(along, across - offset) - radius, Length(along, across + offset) - radius);
        }

        /// <summary>The signed distance to a triangle (a, b, c).</summary>
        private static float TriangleSdf(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float e0x = bx - ax, e0y = by - ay, e1x = cx - bx, e1y = cy - by, e2x = ax - cx, e2y = ay - cy;
            float v0x = px - ax, v0y = py - ay, v1x = px - bx, v1y = py - by, v2x = px - cx, v2y = py - cy;
            float Clamp01(float t) => MathF.Max(0f, MathF.Min(1f, t));
            float t0 = Clamp01(((v0x * e0x) + (v0y * e0y)) / ((e0x * e0x) + (e0y * e0y)));
            float t1 = Clamp01(((v1x * e1x) + (v1y * e1y)) / ((e1x * e1x) + (e1y * e1y)));
            float t2 = Clamp01(((v2x * e2x) + (v2y * e2y)) / ((e2x * e2x) + (e2y * e2y)));
            float q0x = v0x - (e0x * t0), q0y = v0y - (e0y * t0);
            float q1x = v1x - (e1x * t1), q1y = v1y - (e1y * t1);
            float q2x = v2x - (e2x * t2), q2y = v2y - (e2y * t2);
            float sign = MathF.Sign((e0x * e2y) - (e0y * e2x));
            float d0 = (q0x * q0x) + (q0y * q0y), s0 = sign * ((v0x * e0y) - (v0y * e0x));
            float d1 = (q1x * q1x) + (q1y * q1y), s1 = sign * ((v1x * e1y) - (v1y * e1x));
            float d2 = (q2x * q2x) + (q2y * q2y), s2 = sign * ((v2x * e2y) - (v2y * e2x));
            float d = MathF.Min(d0, MathF.Min(d1, d2));
            float s = MathF.Min(s0, MathF.Min(s1, s2));
            return -MathF.Sqrt(d) * MathF.Sign(s);
        }

        private static Dictionary<string, Func<float, float, float>> Build()
        {
            var s = new Dictionary<string, Func<float, float, float>>(StringComparer.Ordinal)
            {
                // ---- UI kit: panels and basic forms ----
                ["ui.panel"] = (x, y) => RoundedBox(x, y, 0f, 0f, 1f, 1f, 0.35f),
                ["ui.circle"] = (x, y) => Length(x, y) - 1f,
                ["ui.ring"] = (x, y) => MathF.Abs(Length(x, y) - 0.8f) - 0.14f,
                ["ui.lock"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, -0.3f, 0.62f, 0.5f, 0.12f),
                    MathF.Abs(Length(x, y - 0.25f) - 0.36f) - 0.1f + Step(y < 0.2f)),
                ["ui.star"] = (x, y) => StarShape(x, y, 0.9f),
                ["ui.star2"] = (x, y) => Min(StarShape((x + 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f, StarShape((x - 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f),
                ["ui.cross"] = Cross,
                ["ui.close"] = (x, y) => Cross(x * 1.25f, y * 1.25f) / 1.25f,
                ["ui.settings"] = (x, y) =>
                {
                    float teeth = MathF.Cos(8f * MathF.Atan2(y, x)) > 0f ? 0.16f : 0f;
                    return Max(Length(x, y) - (0.62f + teeth), -(Length(x, y) - 0.26f));
                },
                ["ui.pointer"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, -0.35f, 0.42f, 0.42f, 0.2f),
                    RoundedBox(x, y, -0.12f, 0.3f, 0.13f, 0.5f, 0.12f)),

                // ---- UI kit: the design board's glyphs ----
                ["ui.pause"] = (x, y) => Min(RoundedBox(x, y, -0.26f, 0f, 0.14f, 0.52f, 0.1f), RoundedBox(x, y, 0.26f, 0f, 0.14f, 0.52f, 0.1f)),
                ["ui.restart"] = (x, y) => Min(
                    MathF.Abs(Length(x, y) - 0.55f) - 0.12f + Step(x > -0.05f && y > 0.2f),
                    Max(-(x - 0.02f), (0.6f * (x - 0.45f)) + (0.8f * (y - 0.55f)), (0.6f * (x - 0.45f)) - (0.8f * (y - 0.55f)))),
                ["ui.chevron"] = (x, y) => Min(Segment(x, y, -0.2f, 0.5f, 0.22f, 0f), Segment(x, y, 0.22f, 0f, -0.2f, -0.5f)) - 0.13f,
                ["ui.plus"] = (x, y) => Min(RoundedBox(x, y, 0f, 0f, 0.58f, 0.15f, 0.08f), RoundedBox(x, y, 0f, 0f, 0.15f, 0.58f, 0.08f)),
                // A pencil from the lower left (its tip) to the upper right, a gap before its eraser.
                ["ui.edit"] = (x, y) => Min(
                    Max(Segment(x, y, -0.24f, -0.24f, 0.44f, 0.44f) - 0.17f, -(MathF.Abs(((x + y) * 0.7071f) - 0.36f) - 0.035f)),
                    TriangleSdf(x, y, -0.62f, -0.62f, -0.42f, -0.17f, -0.17f, -0.42f)),
                ["ui.check"] = (x, y) => Min(Segment(x, y, -0.52f, 0.02f, -0.12f, -0.4f), Segment(x, y, -0.12f, -0.4f, 0.55f, 0.45f)) - 0.13f,
                ["ui.gift"] = (x, y) => Min(
                    Max(Min(RoundedBox(x, y, 0f, -0.32f, 0.68f, 0.45f, 0.08f), RoundedBox(x, y, 0f, 0.3f, 0.8f, 0.13f, 0.05f)), -(MathF.Abs(x) - 0.06f)),
                    MathF.Abs(Length(x + 0.22f, y - 0.62f) - 0.16f) - 0.07f + Step(y < 0.5f),
                    MathF.Abs(Length(x - 0.22f, y - 0.62f) - 0.16f) - 0.07f + Step(y < 0.5f)),
                ["ui.trophy"] = (x, y) => Min(
                    Max(Length(x, y - 0.32f) - 0.52f, y - 0.62f),
                    Max(MathF.Abs(Length(x + 0.5f, y - 0.38f) - 0.2f) - 0.07f, x + 0.35f),
                    Max(MathF.Abs(Length(x - 0.5f, y - 0.38f) - 0.2f) - 0.07f, -(x - 0.35f)),
                    RoundedBox(x, y, 0f, -0.36f, 0.1f, 0.2f, 0.03f),
                    RoundedBox(x, y, 0f, -0.68f, 0.42f, 0.12f, 0.05f)),
                ["ui.medal"] = (x, y) => Min(Length(x, y + 0.2f) - 0.55f, Segment(x, y, -0.32f, 0.88f, -0.05f, 0.3f) - 0.14f, Segment(x, y, 0.32f, 0.88f, 0.05f, 0.3f) - 0.14f),
                ["ui.ad"] = (x, y) => Min(
                    RoundedBox(x, y, -0.18f, 0f, 0.55f, 0.42f, 0.12f),
                    Max(MathF.Abs(y) - (0.1f + (0.7f * (x - 0.42f))), -(x - 0.42f), x - 0.86f)),
                ["ui.shirt"] = (x, y) => Max(
                    Min(RoundedBox(x, y, 0f, -0.18f, 0.45f, 0.62f, 0.08f), Segment(x, y, -0.35f, 0.34f, -0.78f, 0.02f) - 0.18f, Segment(x, y, 0.35f, 0.34f, 0.78f, 0.02f) - 0.18f),
                    -(Length(x, y - 0.5f) - 0.2f)),
                ["ui.grid"] = (x, y) => Min(
                    RoundedBox(x, y, -0.42f, 0.42f, 0.33f, 0.33f, 0.1f), RoundedBox(x, y, 0.42f, 0.42f, 0.33f, 0.33f, 0.1f),
                    RoundedBox(x, y, -0.42f, -0.42f, 0.33f, 0.33f, 0.1f), RoundedBox(x, y, 0.42f, -0.42f, 0.33f, 0.33f, 0.1f)),
                ["ui.sun"] = (x, y) => Min(Length(x, y) - 0.4f, Max(MathF.Abs(Length(x, y) - 0.74f) - 0.14f, (0.5f - MathF.Cos(8f * MathF.Atan2(y, x))) * 0.35f)),
                ["ui.person"] = (x, y) => Min(Length(x, y - 0.32f) - 0.3f, Max(Length(x, y + 0.9f) - 0.72f, -(y + 0.9f))),

                // ---- UI kit: the Garden look (spec 003) ----
                // PLAY's rounded triangle, as tall as the letters (FR-010).
                ["ui.play"] = (x, y) => TriangleSdf(x + 0.04f, y, -0.3f, 0.66f, 0.66f, 0f, -0.3f, -0.66f) - 0.1f,

                // ---- UI kit: the reference look (spec 005 contracts/look.md §3.8) ----
                // The speed pill's three chevrons (▶▶▶, the owner, 2026-10-06: no number on it) and the back button's
                // left arrow.
                ["ui.fast"] = (x, y) => Min(Min(FastChevron(x + 0.6f, y), FastChevron(x, y)), FastChevron(x - 0.6f, y)),
                ["ui.back"] = (x, y) => Min(
                    TriangleSdf(x, y, -0.78f, 0f, -0.12f, 0.56f, -0.12f, -0.56f) - 0.08f,
                    RoundedBox(x, y, 0.3f, 0f, 0.5f, 0.17f, 0.12f)),
                // The clover-like ivy over a wooden sign's ends (§3.2, §3.9); its leaves are drawn one by one (IvyLeafSdf).
                ["ui.sign.ivy"] = IvyCluster,
                // The leaves and white flower of the main buttons (FR-011a): the whole cluster; its parts are drawn
                // one by one in their own colors (DecorationPartSdf).
                ["ui.deco.garden"] = (x, y) => Min(
                    DecorationSdf(DecorationPart.LeafA, x, y),
                    DecorationSdf(DecorationPart.LeafB, x, y),
                    DecorationSdf(DecorationPart.LeafC, x, y),
                    DecorationSdf(DecorationPart.Petals, x, y)),

                // ---- Currency and rewards ----
                // The Petals currency is a lotus bud (spec 005 §3.4): a tall center petal, two side petals and two small
                // back petals. The front petals and the light petal middles are parts drawn over it (GardenLook.Lotus).
                ["currency.petal"] = (x, y) => Min(LotusFront(x, y), LotusBack(x, y)),
                ["currency.petal.front"] = LotusFront,
                ["currency.petal.tips"] = LotusLights,
                ["currency.reward_basket"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, -0.42f, 0.82f, 0.38f, 0.3f),
                    RoundedBox(x, y, 0f, -0.02f, 0.95f, 0.1f, 0.08f)),

                // ---- Characters ----
                ["char.sprig"] = (x, y) => SilhouetteSdf(Family.Sprig, x, y),
                ["char.bloom"] = (x, y) => SilhouetteSdf(Family.Bloom, x, y),
                ["char.drop"] = (x, y) => SilhouetteSdf(Family.Drop, x, y),
                ["char.twig"] = (x, y) => SilhouetteSdf(Family.Twig, x, y),

                // ---- Variant symbols (8 launch + 4 expansion) ----
                // Spec 005 (research D10, D13): drawn to read like the reference strip at board size, each family pair
                // keeping its own silhouette. The sticker tile's details (veins, centers, sparkle, rings, cap) follow the
                // same geometry (UiRaster).
                ["symbol.leaf"] = LeafSymbol,
                ["symbol.moss"] = MossSymbol,
                ["symbol.flower"] = Flower,
                ["symbol.bud"] = BudSymbol,
                ["symbol.drop"] = WaterSymbol,
                ["symbol.dew"] = (x, y) => Min(DewDrop(x, y), DewSparkle(x, y)),
                ["symbol.log"] = (x, y) => Max(StumpSolid(x, y), -StumpGrooves(x, y)),
                ["symbol.acorn"] = (x, y) => Max(AcornSolid(x, y), -AcornGroove(x, y)),
                ["symbol.vine"] = (x, y) => MathF.Abs(Length(x, y) - 0.55f) - 0.12f + Step(x > 0.2f && y > 0f),
                ["symbol.berry"] = (x, y) => Min(Length(x + 0.3f, y - 0.12f) - 0.33f, Length(x - 0.3f, y - 0.12f) - 0.33f, Length(x, y + 0.4f) - 0.33f, RoundedBox(x, y, 0f, 0.62f, 0.06f, 0.2f, 0.03f)),
                ["symbol.mist"] = (x, y) => Min(RoundedBox(x, y, 0f, 0.45f, 0.8f, 0.1f, 0.1f), RoundedBox(x, y, 0.1f, 0f, 0.7f, 0.1f, 0.1f), RoundedBox(x, y, -0.1f, -0.45f, 0.7f, 0.1f, 0.1f)),
                ["symbol.bark"] = (x, y) => Max(RoundedBox(x, y, 0f, 0f, 0.6f, 0.85f, 0.15f), -Min(RoundedBox(x, y, -0.25f, 0f, 0.05f, 0.6f, 0.03f), RoundedBox(x, y, 0.2f, 0.1f, 0.05f, 0.5f, 0.03f))),

                // ---- Board tiles and overlays ----
                ["tile.key"] = (x, y) => Min(
                    MathF.Abs(Length(x + 0.45f, y) - 0.32f) - 0.1f,
                    RoundedBox(x, y, 0.3f, 0f, 0.55f, 0.09f, 0.02f),
                    RoundedBox(x, y, 0.7f, -0.16f, 0.08f, 0.14f, 0.02f)),
                ["tile.mystery"] = (x, y) => Min(
                    MathF.Abs(Length(x, y - 0.3f) - 0.36f) - 0.1f + Step(y < 0.15f && x < 0.05f),
                    RoundedBox(x, y, 0f, -0.2f, 0.09f, 0.18f, 0.04f),
                    Length(x, y + 0.65f) - 0.12f),
                ["tile.stone"] = (x, y) => Min(RoundedBox(x, y, 0f, -0.2f, 0.82f, 0.52f, 0.42f), Length(x + 0.18f, y - 0.2f) - 0.5f, Length(x - 0.32f, y - 0.05f) - 0.42f),

                // ---- Specials ----
                ["special.gate"] = (x, y) => Max(
                    Min(RoundedBox(x, y, 0f, -0.2f, 0.75f, 0.6f, 0.05f), Length(x, y - 0.4f) - 0.75f),
                    -Min(
                        RoundedBox(x, y, -0.4f, -0.2f, 0.07f, 0.55f, 0.03f),
                        RoundedBox(x, y, 0f, -0.1f, 0.07f, 0.7f, 0.03f),
                        RoundedBox(x, y, 0.4f, -0.2f, 0.07f, 0.55f, 0.03f))),
                ["special.fountain"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, -0.6f, 0.85f, 0.2f, 0.15f),
                    RoundedBox(x, y, 0f, -0.15f, 0.12f, 0.35f, 0.05f),
                    MathF.Abs(Length(x, y - 0.1f) - 0.55f) - 0.07f + Step(y < 0.3f)),
                ["special.chest"] = (x, y) => Max(
                    Min(RoundedBox(x, y, 0f, -0.3f, 0.8f, 0.42f, 0.08f), Max(Length(x, y - 0.12f) - 0.8f, -(y - 0.12f), MathF.Abs(x) - 0.8f)),
                    -Min(RoundedBox(x, y, 0f, 0.1f, 0.85f, 0.04f, 0.01f), RoundedBox(x, y, 0f, -0.05f, 0.1f, 0.14f, 0.03f))),
                ["special.statue"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, -0.72f, 0.7f, 0.16f, 0.05f),
                    RoundedBox(x, y, 0f, -0.18f, 0.32f, 0.42f, 0.18f),
                    Length(x, y - 0.5f) - 0.26f),
                ["special.bridge_broken"] = (x, y) => Min(
                    RoundedBox(x, y, -0.55f, 0f, 0.28f, 0.7f, 0.06f),
                    RoundedBox(x, y, 0.55f, 0f, 0.28f, 0.7f, 0.06f),
                    RoundedBox(x, y, 0f, 0.62f, 0.9f, 0.06f, 0.03f),
                    RoundedBox(x, y, 0f, -0.62f, 0.9f, 0.06f, 0.03f)),
                ["special.bridge"] = (x, y) => Min(
                    Max(RoundedBox(x, y, 0f, 0f, 0.85f, 0.7f, 0.06f), -Min(RoundedBox(x, y, 0f, 0.24f, 0.9f, 0.025f, 0.01f), RoundedBox(x, y, 0f, -0.24f, 0.9f, 0.025f, 0.01f))),
                    RoundedBox(x, y, 0f, 0.8f, 0.95f, 0.06f, 0.03f),
                    RoundedBox(x, y, 0f, -0.8f, 0.95f, 0.06f, 0.03f)),
                ["special.region"] = (x, y) => Max(
                    MathF.Abs(RoundedBox(x, y, 0f, 0f, 0.72f, 0.72f, 0.15f)) - 0.08f,
                    -Min(RoundedBox(x, y, 0f, 0f, 0.12f, 0.95f, 0.01f), RoundedBox(x, y, 0f, 0f, 0.95f, 0.12f, 0.01f))),

                // ---- Slot and pod states ----
                ["slot.state.waiting"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, 0.75f, 0.55f, 0.08f, 0.04f),
                    RoundedBox(x, y, 0f, -0.75f, 0.55f, 0.08f, 0.04f),
                    Max(MathF.Abs(x) - (0.12f + (0.38f * MathF.Abs(y) / 0.68f)), MathF.Abs(y) - 0.68f)),
                ["slot.state.danger"] = (x, y) => Max(
                    MathF.Abs(RoundedBox(x, y, 0f, 0f, 0.9f, 0.9f, 0.26f)) - 0.05f,
                    (MathF.Sin(MathF.Atan2(y, x) * 14f) - 0.1f) * 0.2f),
                ["slot.state.jam_risk"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, 0.2f, 0.13f, 0.55f, 0.1f),
                    Length(x, y + 0.65f) - 0.15f),

                // ---- Boosters ----
                // Spec 005 §3.8: each id is the booster's one-color silhouette; the colored icon is drawn from its parts
                // (GardenLook.BoosterIcon).
                ["booster.extra_slot"] = (x, y) => Max(Length(x, y) - 0.86f, -ExtraSlotPlus(x, y)),
                ["booster.extra_slot.disc"] = (x, y) => Length(x, y) - 0.86f,
                ["booster.extra_slot.plus"] = ExtraSlotPlus,
                ["booster.shuffle"] = (x, y) => Min(ShuffleArrow(x, y), ShuffleArrow(-x, -y)),
                ["booster.shuffle.a"] = ShuffleArrow,
                ["booster.shuffle.b"] = (x, y) => ShuffleArrow(-x, -y),
                ["booster.return"] = ReturnArrow,
                ["booster.bloom_burst"] = (x, y) => Max(BurstPetals(x, y), -(Length(x, y) - 0.2f)),
                ["booster.bloom_burst.petals"] = BurstPetals,
                ["booster.bloom_burst.center"] = (x, y) => Length(x, y) - 0.25f,

                // ---- Cosmetics (T143 placeholder art) ----
                ["cosmetic.sprout"] = (x, y) => Min(RoundedBox(x, y, 0f, -0.45f, 0.06f, 0.4f, 0.04f), Leaf((x + 0.3f) * 2f, (y - 0.1f) * 2f) / 2f, Leaf((-x + 0.3f) * 2f, (y - 0.1f) * 2f) / 2f),
                ["cosmetic.cap"] = (x, y) => Min(Max(Length(x, y + 0.3f) - 0.7f, -(y + 0.3f)), RoundedBox(x, y, 0.15f, -0.38f, 0.85f, 0.08f, 0.06f)),
                ["cosmetic.brim"] = (x, y) => Min(Max(Length(x, y + 0.25f) - 0.48f, -(y + 0.25f)), RoundedBox(x, y, 0f, -0.3f, 0.95f, 0.08f, 0.07f)),
                ["cosmetic.crown"] = (x, y) => Min(RoundedBox(x, y, 0f, -0.35f, 0.75f, 0.18f, 0.06f), Length(x + 0.52f, y - 0.05f) - 0.17f, Length(x, y - 0.22f) - 0.2f, Length(x - 0.52f, y - 0.05f) - 0.17f),
                ["cosmetic.nightcap"] = (x, y) => Min(RoundedBox(x, y, 0f, -0.5f, 0.7f, 0.12f, 0.08f), Max(-(y + 0.4f), (MathF.Abs(x + (0.35f * (y + 0.4f))) * 1.3f) + (y * 0.55f) - 0.45f), Length(x - 0.45f, y - 0.62f) - 0.16f),
                ["cosmetic.sparkle"] = (x, y) => Min(StarShape(x * 1.4f, y * 1.4f, 0.9f) / 1.4f, StarShape((x - 0.55f) * 3f, (y + 0.5f) * 3f, 0.9f) / 3f),
                ["cosmetic.swirl"] = (x, y) => MathF.Abs(Length(x, y) - 0.55f) - 0.1f + Step(x < 0f && y < 0f),
                ["cosmetic.wink"] = (x, y) => Min(RoundedBox(x, y, -0.4f, 0.15f, 0.22f, 0.06f, 0.05f), Length(x - 0.4f, y - 0.15f) - 0.14f, Max(MathF.Abs(Length(x, y + 0.05f) - 0.4f) - 0.06f, y + 0.25f)),
                ["cosmetic.smile"] = (x, y) => Min(Length(x + 0.4f, y - 0.2f) - 0.12f, Length(x - 0.4f, y - 0.2f) - 0.12f, Max(MathF.Abs(Length(x, y + 0.05f) - 0.45f) - 0.07f, y + 0.2f)),
                ["cosmetic.stars"] = (x, y) => Min(StarShape((x + 0.42f) * 2.4f, (y - 0.1f) * 2.4f, 0.9f) / 2.4f, StarShape((x - 0.42f) * 2.4f, (y - 0.1f) * 2.4f, 0.9f) / 2.4f),
                ["cosmetic.sleepy"] = (x, y) => Min(
                    Max(MathF.Abs(Length(x + 0.4f, y - 0.3f) - 0.2f) - 0.05f, y - 0.3f),
                    Max(MathF.Abs(Length(x - 0.4f, y - 0.3f) - 0.2f) - 0.05f, y - 0.3f),
                    Length(x, y + 0.3f) - 0.1f),
                // The plain tinted frame: a band round the avatar's rounded square (the owner, 2026-10-06; it was a ring).
                ["cosmetic.frame"] = (x, y) => MathF.Abs(RoundedBox(x, y, 0f, 0f, 0.8f, 0.8f, 0.5f)) - 0.14f,
                ["cosmetic.badge"] = (x, y) => StarShape(x, y, 0.9f),
                ["cosmetic.marker"] = (x, y) => Min(StarShape((x + 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f, StarShape((x - 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f),

                // ---- Effects ----
                ["fx.sparkle"] = (x, y) => (Sq(MathF.Sqrt(MathF.Abs(x)) + MathF.Sqrt(MathF.Abs(y))) - 0.9f) * 0.5f,
                ["fx.petal_burst"] = (x, y) => PetalFlower(x * 1.2f, y * 1.2f) / 1.2f,
                // One falling petal of the win (spec 005 §3.9): a soft pointed oval with a notch at its round end.
                ["fx.petals"] = (x, y) => Max(LensSdf(x, y, -0.2f, -0.86f, 0.24f, 0.86f, 0.42f), -(Length(x + 0.02f, y + 0.95f) - 0.16f)),
                ["fx.droplet"] = (x, y) => Min(Length(x, y + 0.25f) - 0.45f, Triangle(x, y + 0.05f, 0.45f)),
            };

            foreach (string skin in new[] { "spots", "stripes", "petals", "speckles" })
            {
                string shape = skin;
                s["cosmetic." + shape] = (x, y) => Max(Length(x, y) - 0.9f, SkinPatternSdf(shape, x, y));
            }

            return s;
        }

        // ---- The reference look's symbols and icons (spec 005 research D10, D13; contracts/look.md §3.1.1, §3.4, §3.8) ----

        /// <summary>
        /// The symbol a sticker tile fills: the variant symbol without the cuts that let a one-color symbol read (the
        /// flower's middle, the stump's rings, the acorn's cap line), which the sticker draws as details instead.
        /// </summary>
        internal static Func<float, float, float> SolidSymbol(string iconId) => iconId switch
        {
            "flower" => FlowerPetals,
            "log" => StumpSolid,
            "acorn" => AcornSolid,
            _ => Get(SymbolId(iconId)),
        };

        /// <summary>
        /// The board's gem silhouette of a variant symbol (spec 005 FR-026, contracts/look.md §3.1.2): the symbol drawn
        /// simple, chunky and rounded so it reads as a bold gem with a thick outline at 40 px, keeping the distinct
        /// silhouettes of research D10 (leaf almond, scalloped moss cushion, five-petal flower, tulip bud, pointed water
        /// drop, round dew drop with its sparkle, stump, acorn). Spans about ±0.8 shape units. Not a registered shape: it
        /// is part of the <c>tile.candy</c> picture.
        /// </summary>
        public static Func<float, float, float> GemSymbol(string iconId) => iconId switch
        {
            "leaf" => GemLeaf,
            "moss" => GemMoss,
            "flower" => GemFlower,
            "bud" => GemBud,
            "drop" => (x, y) => Teardrop(x, y, 0f, -0.3f, 0.57f, 0.9f) - 0.03f,
            "dew" => GemDew,
            "log" => GemStump,
            "acorn" => GemAcorn,
            _ => SolidSymbol(iconId),
        };

        /// <summary>
        /// The inner line a gem draws over its fill so the one-color gem reads (negative on the line), or null: the
        /// leaf's midrib, the flower's center ring, the stump's top ellipse and the acorn's cap line.
        /// </summary>
        public static Func<float, float, float>? GemDetail(string iconId) => iconId switch
        {
            "leaf" => (x, y) => MathF.Max(Segment(x, y, -0.36f, -0.36f, 0.42f, 0.42f) - 0.045f, GemLeaf(x, y) + 0.18f),
            "flower" => (x, y) => MathF.Abs(Length(x, y) - 0.25f) - 0.05f,
            "log" => (x, y) => MathF.Max(MathF.Abs(GemEllipse(x, y - GemStumpTopY, 0.58f, 0.26f)) - 0.045f, y - GemStumpTopY),
            "acorn" => (x, y) => MathF.Max(MathF.Abs(y - (AcornCapY - 0.02f)) - 0.045f, AcornSolid(x, y) + 0.06f),
            _ => null,
        };

        /// <summary>The part of a gem drawn a shade lighter (the flower's middle, the stump's top, dew's sparkle), or null.</summary>
        public static Func<float, float, float>? GemLight(string iconId) => iconId switch
        {
            "flower" => (x, y) => Length(x, y) - 0.22f,
            "log" => (x, y) => GemEllipse(x, y - GemStumpTopY, 0.52f, 0.2f),
            "dew" => GemDewSparkle,
            _ => null,
        };

        /// <summary>The part of a gem drawn a shade darker (the acorn's cap), or null.</summary>
        public static Func<float, float, float>? GemDark(string iconId) => iconId switch
        {
            "acorn" => AcornCap,
            _ => null,
        };

        /// <summary>
        /// Dew: a round droplet leaning to the right (sliding on a leaf), its short soft tip up and to the left, so it
        /// differs from water's tall upright drop; its sparkle shines inside it (<see cref="GemDewSparkle"/>).
        /// </summary>
        private static float GemDew(float x, float y)
        {
            const float c = 0.866f;
            const float s = 0.5f;
            float u = (c * x) + (s * y);
            float v = (-s * x) + (c * y);
            return Teardrop(u + 0.04f, v, 0f, -0.18f, 0.6f, 0.62f) - 0.04f;
        }

        /// <summary>Dew's sparkle inside its droplet: a small four-pointed star on its right.</summary>
        private static float GemDewSparkle(float x, float y)
        {
            float u = MathF.Abs(x - 0.16f) / 0.3f;
            float v = MathF.Abs(y + 0.12f) / 0.3f;
            return (Sq(MathF.Sqrt(u) + MathF.Sqrt(v)) - 0.9f) * 0.15f;
        }

        /// <summary>The stump gem's top ellipse height.</summary>
        private const float GemStumpTopY = 0.32f;

        /// <summary>A chunky almond tilted toward the upper right, round at its base and softly pointed at its tip.</summary>
        private static float GemLeaf(float x, float y) =>
            SmoothMin(LensSdf(x, y, -0.6f, -0.6f, 0.66f, 0.66f, 0.44f), Length(x + 0.2f, y + 0.2f) - 0.5f, 0.16f) - 0.02f;

        /// <summary>A round cushion, a little wider than tall, with nine soft scallops around its edge and a flatter foot.</summary>
        private static float GemMoss(float x, float y)
        {
            float d = GemEllipse(x, y, 0.6f, 0.54f);
            for (int i = 0; i < 9; i++)
            {
                float a = 0.3f + (i * 2f * MathF.PI / 9f);
                d = SmoothMin(d, Length(x - (0.62f * MathF.Cos(a)), y - (0.54f * MathF.Sin(a))) - 0.19f, 0.04f);
            }

            return Max(d, -(y + 0.66f));
        }

        /// <summary>Five round petals around a smaller middle, so the notches between them stay deep.</summary>
        private static float GemFlower(float x, float y)
        {
            float d = Length(x, y) - 0.3f;
            for (int i = 0; i < 5; i++)
            {
                float a = (MathF.PI / 2f) + (i * 2f * MathF.PI / 5f);
                d = MathF.Min(d, Length(x - (0.5f * MathF.Cos(a)), y - (0.5f * MathF.Sin(a))) - 0.3f);
            }

            return d;
        }

        /// <summary>A tulip bud: a round body with three short rounded tips on top.</summary>
        private static float GemBud(float x, float y)
        {
            float tips = Min(
                LensSdf(x, y, 0f, -0.3f, 0f, 0.9f, 0.22f),
                LensSdf(x, y, -0.04f, -0.4f, -0.44f, 0.68f, 0.19f),
                LensSdf(x, y, 0.04f, -0.4f, 0.44f, 0.68f, 0.19f));
            return SmoothMin(tips, GemEllipse(x, y + 0.26f, 0.46f, 0.56f), 0.06f) - 0.02f;
        }

        /// <summary>A stump: a short cylinder with its top ellipse and two roots at its foot.</summary>
        private static float GemStump(float x, float y) => Min(
            RoundedBox(x, y, 0f, -0.18f, 0.58f, 0.5f, 0.1f),
            GemEllipse(x, y - GemStumpTopY, 0.58f, 0.26f),
            GemEllipse(x + 0.62f, y + 0.6f, 0.26f, 0.14f),
            GemEllipse(x - 0.62f, y + 0.6f, 0.26f, 0.14f));

        /// <summary>An acorn: a wide cap with a short stem over a round nut softly pointed below.</summary>
        private static float GemAcorn(float x, float y) => Min(
            SmoothMin(Max(GemEllipse(x, y - 0.18f, 0.8f, 0.44f), AcornCapY - y), RoundedBox(x, y, 0f, AcornCapY + 0.06f, 0.8f, 0.08f, 0.08f), 0.04f),
            RoundedBox(x, y, 0.04f, 0.7f, 0.08f, 0.14f, 0.07f),
            Teardrop(x, -y, 0f, 0.26f, 0.44f, 0.88f) - 0.04f);

        /// <summary>
        /// A closer ellipse distance than <see cref="Ellipse"/> (its implicit value over its gradient), so a gem's thick
        /// outline keeps an even width around a flat ellipse.
        /// </summary>
        private static float GemEllipse(float x, float y, float rx, float ry)
        {
            float f = (x * x / (rx * rx)) + (y * y / (ry * ry)) - 1f;
            float g = Length(2f * x / (rx * rx), 2f * y / (ry * ry));
            return g < 1e-4f ? -MathF.Min(rx, ry) : f / g;
        }

        /// <summary>A smooth union of two distances that blends them over <paramref name="k"/>.</summary>
        private static float SmoothMin(float a, float b, float k)
        {
            float h = MathF.Max(0f, MathF.Min(1f, 0.5f + (0.5f * (b - a) / k)));
            return (b * (1f - h)) + (a * h) - (k * h * (1f - h));
        }

        /// <summary>The leaf's axis, from the stem end of its blade to its tip (the sticker's midrib and veins follow it).</summary>
        internal const float LeafBaseX = -0.56f, LeafBaseY = -0.5f, LeafTipX = 0.8f, LeafTipY = 0.78f;

        /// <summary>The moss cushion's three dimples (x, y, radius) on the sticker.</summary>
        internal static readonly (float X, float Y, float R)[] MossDimples = { (-0.34f, 0.12f, 0.11f), (0.3f, 0.22f, 0.1f), (0.02f, -0.3f, 0.1f) };

        /// <summary>The water drop's round part (its highlight follows it on the sticker).</summary>
        internal const float WaterCenterY = -0.3f, WaterRadius = 0.56f;

        /// <summary>The stump's top: an ellipse at this height with these radii (its rings on the sticker).</summary>
        internal const float StumpTopY = 0.32f, StumpRx = 0.7f, StumpRy = 0.3f;

        /// <summary>The height of the acorn cap's lower edge.</summary>
        internal const float AcornCapY = 0.04f;

        /// <summary>A broad almond (about 1.5:1) with a round base, tilted toward the upper right, and a barely visible stem.</summary>
        private static float LeafSymbol(float x, float y)
        {
            float blade = Min(
                LensSdf(x, y, LeafBaseX, LeafBaseY, LeafTipX, LeafTipY, 0.62f),
                Length(x - (LeafBaseX + (0.32f * (LeafTipX - LeafBaseX))), y - (LeafBaseY + (0.32f * (LeafTipY - LeafBaseY)))) - 0.55f);
            return Min(blade, Segment(x, y, LeafBaseX + 0.06f, LeafBaseY + 0.06f, -0.74f, -0.74f) - 0.075f);
        }

        /// <summary>A round cushion (1:1) with about eleven soft, slightly uneven scallops around its edge.</summary>
        private static float MossSymbol(float x, float y)
        {
            float d = Ellipse(x, y, 0.66f, 0.62f);
            for (int i = 0; i < 11; i++)
            {
                float a = 0.2f + (i * 2f * MathF.PI / 11f);
                float r = 0.14f + (0.03f * MathF.Sin(i * 2.7f));
                d = MathF.Min(d, Length(x - (0.64f * MathF.Cos(a)), y - (0.64f * MathF.Sin(a))) - r);
            }

            return Max(d, -(y + 0.78f));
        }

        /// <summary>A chunky tulip bud: a round body with three short rounded tips and two sepals at its base.</summary>
        private static float BudSymbol(float x, float y) => Min(
            Min(LensSdf(x, y, 0f, -0.5f, 0f, 0.92f, 0.34f), LensSdf(x, y, 0.02f, -0.52f, -0.42f, 0.72f, 0.27f), LensSdf(x, y, -0.02f, -0.52f, 0.42f, 0.72f, 0.27f)),
            Ellipse(x, y + 0.22f, 0.56f, 0.48f),
            BudSepals(x, y));

        /// <summary>The bud's two sepals at its base (green on the sticker).</summary>
        internal static float BudSepals(float x, float y) => Min(LensSdf(x, y, 0f, -0.7f, -0.7f, -0.42f, 0.17f), LensSdf(x, y, 0f, -0.7f, 0.7f, -0.42f, 0.17f));

        /// <summary>Water: a tall teardrop with a sharp tip.</summary>
        private static float WaterSymbol(float x, float y) => Teardrop(x, y, 0f, WaterCenterY, WaterRadius, 0.9f);

        /// <summary>Dew's droplet: rounder and shorter than water's, with a soft tip, left of its sparkle.</summary>
        internal static float DewDrop(float x, float y) => Teardrop(x, y, -0.14f, -0.24f, 0.56f, 0.5f) - 0.04f;

        /// <summary>Dew's sparkle: a four-pointed star at the droplet's upper right (white on the sticker).</summary>
        internal static float DewSparkle(float x, float y)
        {
            float u = MathF.Abs(x - 0.62f) / 0.32f;
            float v = MathF.Abs(y - 0.6f) / 0.32f;
            return (Sq(MathF.Sqrt(u) + MathF.Sqrt(v)) - 0.9f) * 0.16f;
        }

        /// <summary>The stump's top ellipse.</summary>
        internal static float StumpTop(float x, float y) => Ellipse(x, y - StumpTopY, StumpRx, StumpRy);

        /// <summary>Wood: a short cylinder (a stump) with its top ellipse, a rounded foot and two small roots.</summary>
        internal static float StumpSolid(float x, float y) => Min(
            StumpTop(x, y),
            Min(RoundedBox(x, y, 0f, (StumpTopY - 0.5f) / 2f, StumpRx, (StumpTopY + 0.5f) / 2f, 0f), Ellipse(x, y + 0.5f, StumpRx, 0.2f)),
            Ellipse(x + 0.66f, y + 0.56f, 0.24f, 0.14f),
            Ellipse(x - 0.66f, y + 0.56f, 0.24f, 0.14f));

        /// <summary>The grooves that make the one-color stump read: the front rim of its top, one ring on it and two bark lines.</summary>
        private static float StumpGrooves(float x, float y) => Min(
            Max(MathF.Abs(StumpTop(x, y)) - 0.04f, y - StumpTopY),
            MathF.Abs(Ellipse(x, y - StumpTopY, 0.4f, 0.17f)) - 0.035f,
            Min(Segment(x, y, -0.34f, -0.08f, -0.34f, -0.42f), Segment(x, y, 0.3f, -0.02f, 0.3f, -0.36f)) - 0.035f);

        /// <summary>The acorn's cap: a dome above the cap line, its rounded rim and a short stem.</summary>
        internal static float AcornCap(float x, float y) => Min(
            Max(Ellipse(x, y - 0.2f, 0.7f, 0.42f), AcornCapY + 0.04f - y),
            RoundedBox(x, y, 0f, AcornCapY + 0.08f, 0.68f, 0.08f, 0.08f),
            RoundedBox(x, y, 0.06f, 0.74f, 0.07f, 0.15f, 0.06f));

        /// <summary>An acorn: the cap over a round nut with a soft point below.</summary>
        internal static float AcornSolid(float x, float y) => Min(AcornCap(x, y), Teardrop(x, -y, 0f, 0.26f, 0.5f, 0.86f) - 0.04f);

        /// <summary>The thin cut between the cap and the nut that makes the one-color acorn read.</summary>
        private static float AcornGroove(float x, float y) => Max(MathF.Abs(y - (AcornCapY - 0.03f)) - 0.03f, MathF.Abs(x) - 0.62f);

        /// <summary>The lotus bud's front: a tall almond center petal, two side petals curving out, and its base.</summary>
        private static float LotusFront(float x, float y) => Min(
            LensSdf(x, y, 0f, -0.7f, 0f, 0.92f, 0.36f),
            LensSdf(x, y, -0.06f, -0.7f, -0.56f, 0.5f, 0.3f),
            LensSdf(x, y, 0.06f, -0.7f, 0.56f, 0.5f, 0.3f),
            Ellipse(x, y + 0.6f, 0.46f, 0.16f));

        /// <summary>The lotus' two small back petals, low and to the sides.</summary>
        private static float LotusBack(float x, float y) => Min(
            LensSdf(x, y, -0.1f, -0.62f, -0.86f, 0.12f, 0.22f),
            LensSdf(x, y, 0.1f, -0.62f, 0.86f, 0.12f, 0.22f));

        /// <summary>
        /// The near-white middles of the lotus petals (each petal shrunk by 0.12, from 20% to 85% of its length), each only
        /// where the petals in front of it leave room: a side petal's middle stops short of the center petal and a back
        /// petal's short of the front ones, so a deep pink edge parts every petal from the next.
        /// </summary>
        private static float LotusLights(float x, float y)
        {
            float center = LensSdf(x, y, 0f, -0.7f, 0f, 0.92f, 0.36f);
            float sides = Max(
                Min(LensMiddle(x, y, -0.06f, -0.7f, -0.56f, 0.5f, 0.3f), LensMiddle(x, y, 0.06f, -0.7f, 0.56f, 0.5f, 0.3f)),
                -(center - 0.08f));
            float back = Max(
                Min(LensMiddle(x, y, -0.1f, -0.62f, -0.86f, 0.12f, 0.22f), LensMiddle(x, y, 0.1f, -0.62f, 0.86f, 0.12f, 0.22f)),
                -(LotusFront(x, y) - 0.08f));
            return Min(LensMiddle(x, y, 0f, -0.7f, 0f, 0.92f, 0.36f), sides, back);
        }

        /// <summary>A lens' middle: inside its outline by 0.12, between 20% and 85% of the way from a to b.</summary>
        private static float LensMiddle(float x, float y, float ax, float ay, float bx, float by, float half)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float length = MathF.Sqrt((dx * dx) + (dy * dy));
            float along = (((x - ax) * dx) + ((y - ay) * dy)) / length;
            return Max(LensSdf(x, y, ax, ay, bx, by, half) + 0.12f, (0.2f * length) - along, along - (0.85f * length));
        }

        /// <summary>Extra Slot's bold plus.</summary>
        private static float ExtraSlotPlus(float x, float y) => Min(RoundedBox(x, y, 0f, 0f, 0.52f, 0.17f, 0.1f), RoundedBox(x, y, 0f, 0f, 0.17f, 0.52f, 0.1f));

        /// <summary>One of Shuffle's arrows: the upper arc of a ring from 160° to 35°, its head turning down at the right end.</summary>
        private static float ShuffleArrow(float x, float y)
        {
            const float r = 0.56f;
            float ring = MathF.Abs(Length(x, y) - r) - 0.15f;
            // Inside the sector when counter-clockwise of 35° and clockwise of 160°.
            float c0 = MathF.Cos(35f * MathF.PI / 180f);
            float s0 = MathF.Sin(35f * MathF.PI / 180f);
            float c1 = MathF.Cos(160f * MathF.PI / 180f);
            float s1 = MathF.Sin(160f * MathF.PI / 180f);
            float arc = Max(ring, -((c0 * y) - (s0 * x)), (c1 * y) - (s1 * x));
            float ex = r * c0;
            float ey = r * s0;
            float head = TriangleSdf(x, y, ex + (0.33f * c0), ey + (0.33f * s0), ex - (0.33f * c0), ey - (0.33f * s0), ex + (0.4f * s0), ey - (0.4f * c0)) - 0.03f;
            return Min(arc, head);
        }

        /// <summary>
        /// Return's fat arrow: the head points left at the top, the shaft runs right and its tail bends gently down on the
        /// right (the upper right of a ring, overlapping the shaft so no seam shows).
        /// </summary>
        private static float ReturnArrow(float x, float y)
        {
            float yy = y + 0.28f;
            float head = TriangleSdf(x, yy, -0.88f, 0.3f, -0.3f, 0.8f, -0.3f, -0.2f) - 0.05f;
            float shaft = RoundedBox(x, yy, 0f, 0.3f, 0.34f, 0.2f, 0f);

            // The quarter ring's band meets the shaft exactly (0.1 to 0.5) where it starts, and its open inside stays
            // wider than any outline, so no notch closes into a hole.
            float hook = Max(MathF.Abs(Length(x - 0.2f, yy + 0.2f) - 0.5f) - 0.2f, -(x - 0.2f), -(yy + 0.2f));
            return Min(head, shaft, hook);
        }

        /// <summary>The speed glyph's second mark: a solid triangle with a notch cut from its back, so it reads as a chevron.</summary>
        /// <summary>One of the speed pill's chevrons, centered on the origin: a triangle with its back notched out.</summary>
        private static float FastChevron(float x, float y)
        {
            float triangle = TriangleSdf(x, y, -0.27f, 0.54f, 0.3f, 0f, -0.27f, -0.54f) - 0.04f;
            float notch = TriangleSdf(x + 0.24f, y, -0.27f, 0.54f, 0.3f, 0f, -0.27f, -0.54f) + 0.07f;
            return Max(triangle, -notch);
        }

        /// <summary>Bloom Burst's five oval petals around a filled middle.</summary>
        private static float BurstPetals(float x, float y)
        {
            float d = Length(x, y) - 0.34f;
            for (int i = 0; i < 5; i++)
            {
                float a = (MathF.PI / 2f) + (i * 2f * MathF.PI / 5f);
                float c = MathF.Cos(a);
                float sn = MathF.Sin(a);
                float u = (x * c) + (y * sn) - 0.5f;
                float v = (-x * sn) + (y * c);
                d = MathF.Min(d, (Length(u / 0.4f, v / 0.31f) - 1f) * 0.31f);
            }

            return d;
        }

        /// <summary>A circle (cx, cy, r) with a pointed tip straight above it at <paramref name="tipY"/>.</summary>
        private static float Teardrop(float x, float y, float cx, float cy, float r, float tipY)
        {
            float k = r / (tipY - cy);
            float tx = r * MathF.Sqrt(1f - (k * k));
            float ty = cy + (r * k);
            return Min(Length(x - cx, y - cy) - r, TriangleSdf(x, y, cx, tipY, cx - tx, ty, cx + tx, ty));
        }

        /// <summary>An approximate ellipse distance (exact on the axes), negative inside.</summary>
        private static float Ellipse(float x, float y, float rx, float ry) => (Length(x / rx, y / ry) - 1f) * MathF.Min(rx, ry);

        // ---- Composite pieces ----

        private static float SilhouetteSdf(Family family, float x, float y) => family switch
        {
            Family.Sprig => Min(Length(x, y + 0.25f) - 0.55f, Leaf((x - 0.15f) * 2.2f, (y - 0.55f) * 2.2f) / 2.2f),
            Family.Bloom => Min(Length(x, y + 0.25f) - 0.55f, Flower(x * 2.4f, (y - 0.5f) * 2.4f) / 2.4f),
            Family.Drop => Min(Length(x, y + 0.2f) - 0.58f, Triangle(x, y + 0.05f, 0.58f)),
            _ => Min(RoundedBox(x, y, 0f, -0.15f, 0.42f, 0.7f, 0.35f), RoundedBox(x, y, 0.3f, 0.55f, 0.25f, 0.06f, 0.03f)),
        };

        /// <summary>A repeating pattern over the whole square (negative inside a mark).</summary>
        private static float SkinPatternSdf(string shape, float x, float y)
        {
            switch (shape)
            {
                case "spots":
                    return Length(Repeat(x, 0.5f), Repeat(y + (0.25f * MathF.Floor((x / 0.5f) + 0.5f)), 0.5f)) - 0.12f;
                case "stripes":
                    return MathF.Abs(Repeat(y + (0.35f * x), 0.34f)) - 0.07f;
                case "petals":
                    float px = Repeat(x, 0.6f);
                    float py = Repeat(y, 0.6f);
                    return Flower(px * 4.5f, py * 4.5f) / 4.5f;
                default:
                    return Length(Repeat(x + (0.13f * MathF.Floor((y / 0.28f) + 0.5f)), 0.28f), Repeat(y, 0.28f)) - 0.05f;
            }
        }

        private static float Cross(float x, float y)
        {
            const float c = 0.70710678f;
            float u = (c * x) + (c * y);
            float v = (-c * x) + (c * y);
            return Min(RoundedBox(u, v, 0f, 0f, 0.85f, 0.14f, 0.1f), RoundedBox(u, v, 0f, 0f, 0.14f, 0.85f, 0.1f));
        }

        /// <summary>The Petal currency symbol: five long rounded petals (distinct from the Flower variant's round ones).</summary>
        private static float PetalFlower(float x, float y)
        {
            float d = float.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                float a = (MathF.PI / 2f) + (i * 2f * MathF.PI / 5f);
                float c = MathF.Cos(a);
                float s = MathF.Sin(a);
                float u = (x * c) + (y * s) - 0.5f;
                float v = (-x * s) + (y * c);
                d = MathF.Min(d, (Length(u / 0.46f, v / 0.3f) - 1f) * 0.3f);
            }

            return d;
        }

        /// <summary>The offset of a coordinate from its nearest multiple of a period (in −period/2..period/2).</summary>
        private static float Repeat(float v, float period) => v - (period * MathF.Floor((v / period) + 0.5f));

        private static float StarShape(float x, float y, float r)
        {
            float angle = MathF.Atan2(y, x) - (MathF.PI / 2f);
            float radius = r * (0.62f + (0.38f * MathF.Cos(5f * angle)));
            return Length(x, y) - (radius * 0.85f);
        }

        private static float Leaf(float x, float y)
        {
            // A lens (two intersecting circles), tilted 45°.
            const float c = 0.70710678f;
            float u = (c * x) + (c * y);
            float v = (-c * x) + (c * y);
            return Max(Length(u - 0.5f, v) - 0.9f, Length(u + 0.5f, v) - 0.9f);
        }

        private static float Flower(float x, float y) => Max(FlowerPetals(x, y), -(Length(x, y) - 0.18f));

        /// <summary>
        /// The flower's five round petals, clearly apart with deep notches between them, without the hole in the middle (the
        /// sticker paints its center there). The middle disk (0.36) just closes the small gaps the petals leave inside.
        /// </summary>
        private static float FlowerPetals(float x, float y)
        {
            float d = float.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                float a = (MathF.PI / 2f) + (i * 2f * MathF.PI / 5f);
                d = MathF.Min(d, Length(x - (0.5f * MathF.Cos(a)), y - (0.5f * MathF.Sin(a))) - 0.3f);
            }

            return MathF.Min(d, Length(x, y) - 0.36f);
        }

        /// <summary>An upward-pointing triangle on top of a circle of radius r at the origin (a droplet tip).</summary>
        private static float Triangle(float x, float y, float r) => Max(y - (r * 1.9f), (MathF.Abs(x) * 1.6f) + y - (r * 1.9f), -y);

        private static float RoundedBox(float x, float y, float cx, float cy, float hx, float hy, float radius)
        {
            float qx = MathF.Abs(x - cx) - hx + radius;
            float qy = MathF.Abs(y - cy) - hy + radius;
            return Length(MathF.Max(qx, 0f), MathF.Max(qy, 0f)) + MathF.Min(MathF.Max(qx, qy), 0f) - radius;
        }

        /// <summary>The distance to the segment a–b.</summary>
        private static float Segment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float t = MathF.Max(0f, MathF.Min(1f, (((px - ax) * dx) + ((py - ay) * dy)) / ((dx * dx) + (dy * dy))));
            return Length(px - (ax + (t * dx)), py - (ay + (t * dy)));
        }

        private static float Length(float x, float y) => MathF.Sqrt((x * x) + (y * y));

        private static float Sq(float v) => v * v;

        private static float Min(float a, float b) => MathF.Min(a, b);

        private static float Min(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c));

        private static float Min(float a, float b, float c, float d) => MathF.Min(MathF.Min(a, b), MathF.Min(c, d));

        private static float Min(float a, float b, float c, float d, float e) => MathF.Min(Min(a, b, c, d), e);

        private static float Max(float a, float b) => MathF.Max(a, b);

        private static float Max(float a, float b, float c) => MathF.Max(a, MathF.Max(b, c));

        /// <summary>Pushes a region outside a shape (used to cut shapes).</summary>
        private static float Step(bool outside) => outside ? 10f : 0f;
    }
}
