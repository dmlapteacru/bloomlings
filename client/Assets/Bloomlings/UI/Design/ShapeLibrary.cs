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
        public static Func<float, float, float> SkinOn(Family family, string shape)
        {
            Func<float, float, float> body = BloomlingArt.Silhouette(family);
            return (x, y) => Max(body(x, y) + 0.04f, SkinPatternSdf(shape, x, y));
        }

        /// <summary>
        /// One part of the <c>ui.deco.garden</c> cluster, grown by <paramref name="grow"/> (positive: its outline ring),
        /// turned half way when <paramref name="flipped"/> (the bottom-right corner).
        /// </summary>
        public static Func<float, float, float> DecorationPartSdf(DecorationPart part, float grow, bool flipped) =>
            flipped ? (x, y) => DecorationSdf(part, -x, -y) - grow : (Func<float, float, float>)((x, y) => DecorationSdf(part, x, y) - grow);

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
                // The leaves and white flower of the main buttons (FR-011a): the whole cluster; its parts are drawn
                // one by one in their own colors (DecorationPartSdf).
                ["ui.deco.garden"] = (x, y) => Min(
                    DecorationSdf(DecorationPart.LeafA, x, y),
                    DecorationSdf(DecorationPart.LeafB, x, y),
                    DecorationSdf(DecorationPart.LeafC, x, y),
                    DecorationSdf(DecorationPart.Petals, x, y)),

                // ---- Currency and rewards ----
                ["currency.petal"] = (x, y) => Max(PetalFlower(x, y), -(Length(x, y) - 0.22f)),
                ["currency.reward_basket"] = (x, y) => Min(
                    RoundedBox(x, y, 0f, -0.42f, 0.82f, 0.38f, 0.3f),
                    RoundedBox(x, y, 0f, -0.02f, 0.95f, 0.1f, 0.08f)),

                // ---- Characters ----
                // The outer edge of the kawaii figures (BloomlingArt), which draws them in full.
                ["char.sprig"] = BloomlingArt.Silhouette(Family.Sprig),
                ["char.bloom"] = BloomlingArt.Silhouette(Family.Bloom),
                ["char.drop"] = BloomlingArt.Silhouette(Family.Drop),
                ["char.twig"] = BloomlingArt.Silhouette(Family.Twig),
                ["char.face"] = (x, y) => Min(
                    Length(x + 0.3f, y - 0.14f) - 0.12f,
                    Length(x - 0.3f, y - 0.14f) - 0.12f,
                    Max(MathF.Abs(Length(x, y + 0.02f) - 0.3f) - 0.06f, y + 0.12f)),

                // ---- Variant symbols (8 launch + 4 expansion) ----
                ["symbol.leaf"] = Leaf,
                ["symbol.moss"] = (x, y) => Min(Length(x + 0.42f, y + 0.25f) - 0.36f, Length(x - 0.42f, y + 0.25f) - 0.36f, Length(x, y + 0.2f) - 0.42f, Length(x, y - 0.35f) - 0.3f),
                ["symbol.flower"] = Flower,
                ["symbol.bud"] = (x, y) => Min(
                    Max((Length(x / 0.4f, (y - 0.08f) / 0.52f) - 1f) * 0.4f, -Min(Length(x, y - 0.66f) - 0.14f, Max(MathF.Abs(x) - (0.3f * (y - 0.35f)), 0.35f - y))),
                    Leaf((x - 0.34f) * 2.6f, (y + 0.5f) * 2.6f) / 2.6f,
                    Leaf((-x - 0.34f) * 2.6f, (y + 0.5f) * 2.6f) / 2.6f,
                    RoundedBox(x, y, 0f, -0.72f, 0.06f, 0.22f, 0.03f)),
                ["symbol.drop"] = (x, y) => Min(Wave(x, y - 0.3f), Wave(x, y + 0.3f)),
                ["symbol.dew"] = (x, y) => Max(Min(Length(x, y + 0.3f) - 0.45f, Triangle(x, y + 0.1f, 0.45f)), -(Length(x + 0.15f, y + 0.35f) - 0.12f)),
                ["symbol.log"] = (x, y) => Max(RoundedBox(x, y, 0f, 0f, 0.85f, 0.42f, 0.4f), -(MathF.Abs(Length(x - 0.55f, y) - 0.2f) - 0.05f)),
                ["symbol.acorn"] = (x, y) => Min(Length(x, y + 0.2f) - 0.5f, RoundedBox(x, y, 0f, 0.35f, 0.6f, 0.2f, 0.18f), RoundedBox(x, y, 0f, 0.62f, 0.06f, 0.14f, 0.03f)),
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
                ["booster.extra_slot"] = (x, y) => Max(
                    RoundedBox(x, y, 0f, 0f, 0.8f, 0.8f, 0.2f),
                    -Min(RoundedBox(x, y, 0f, 0f, 0.5f, 0.12f, 0.05f), RoundedBox(x, y, 0f, 0f, 0.12f, 0.5f, 0.05f))),
                ["booster.shuffle"] = (x, y) => Min(
                    RoundedBox(x - y, y, 0f, 0f, 0.1f, 0.6f, 0.05f),
                    RoundedBox(x + y, y, 0f, 0f, 0.1f, 0.6f, 0.05f),
                    Max(Length(x - 0.6f, y - 0.6f) - 0.25f, -(x - 0.45f) - (y - 0.45f)),
                    Max(Length(x - 0.6f, y + 0.6f) - 0.25f, -(x - 0.45f) + (y + 0.45f))),
                ["booster.return"] = (x, y) => Min(
                    MathF.Abs(Length(x, y) - 0.55f) - 0.12f + Step(x < 0.05f && y > 0.2f),
                    Max(x + 0.02f, (0.6f * (-x - 0.45f)) + (0.8f * (y - 0.55f)), (0.6f * (-x - 0.45f)) - (0.8f * (y - 0.55f)))),
                ["booster.bloom_burst"] = (x, y) => Min(StarShape(x, y, 0.95f), Length(x, y) - 0.3f),

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
                ["cosmetic.frame"] = (x, y) => MathF.Abs(Length(x, y) - 0.8f) - 0.14f,
                ["cosmetic.badge"] = (x, y) => StarShape(x, y, 0.9f),
                ["cosmetic.marker"] = (x, y) => Min(StarShape((x + 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f, StarShape((x - 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f),

                // ---- Effects ----
                ["fx.sparkle"] = (x, y) => (Sq(MathF.Sqrt(MathF.Abs(x)) + MathF.Sqrt(MathF.Abs(y))) - 0.9f) * 0.5f,
                ["fx.petal_burst"] = (x, y) => PetalFlower(x * 1.2f, y * 1.2f) / 1.2f,
                ["fx.droplet"] = (x, y) => Min(Length(x, y + 0.25f) - 0.45f, Triangle(x, y + 0.05f, 0.45f)),
            };

            foreach (string skin in new[] { "spots", "stripes", "petals", "speckles" })
            {
                string shape = skin;
                s["cosmetic." + shape] = (x, y) => Max(Length(x, y) - 0.9f, SkinPatternSdf(shape, x, y));
            }

            return s;
        }

        // ---- Composite pieces ----

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

        private static float Flower(float x, float y)
        {
            float d = float.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                float a = (MathF.PI / 2f) + (i * 2f * MathF.PI / 5f);
                d = MathF.Min(d, Length(x - (0.45f * MathF.Cos(a)), y - (0.45f * MathF.Sin(a))) - 0.35f);
            }

            return Max(d, -(Length(x, y) - 0.18f));
        }

        private static float Wave(float x, float y) => MathF.Abs(y - (0.12f * MathF.Sin(x * 5f))) - 0.12f + Step(MathF.Abs(x) > 0.85f);

        /// <summary>An upward-pointing triangle on top of a circle of radius r at the origin (a droplet tip).</summary>
        private static float Triangle(float x, float y, float r) => Max(y - (r * 1.9f), (MathF.Abs(x) * 1.6f) + y - (r * 1.9f), -y);

        internal static float RoundedBox(float x, float y, float cx, float cy, float hx, float hy, float radius)
        {
            float qx = MathF.Abs(x - cx) - hx + radius;
            float qy = MathF.Abs(y - cy) - hy + radius;
            return Length(MathF.Max(qx, 0f), MathF.Max(qy, 0f)) + MathF.Min(MathF.Max(qx, qy), 0f) - radius;
        }

        /// <summary>The distance to the segment a–b.</summary>
        internal static float Segment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float t = MathF.Max(0f, MathF.Min(1f, (((px - ax) * dx) + ((py - ay) * dy)) / ((dx * dx) + (dy * dy))));
            return Length(px - (ax + (t * dx)), py - (ay + (t * dy)));
        }

        internal static float Length(float x, float y) => MathF.Sqrt((x * x) + (y * y));

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
