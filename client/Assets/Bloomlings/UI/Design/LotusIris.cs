using System;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// One frame of the lotus loader or the lotus iris (<see cref="LotusIris.Splash"/>, <see cref="LotusIris.Transition"/>):
    /// what the hosts draw over the screen, back to front: the parchment cover with its round hole, the glow, the ring of
    /// petals, the lotus, the logo (the splash) and the text.
    /// </summary>
    public readonly struct LotusPose
    {
        public LotusPose(float open, float glowAlpha, float ringAlpha, float ringLit, float ringSpin, float lotusScale, float lotusTurn, float lotusAlpha, float textAlpha, float textRise, float logoScale, float logoAlpha)
        {
            Open = open;
            GlowAlpha = glowAlpha;
            RingAlpha = ringAlpha;
            RingLit = ringLit;
            RingSpin = ringSpin;
            LotusScale = lotusScale;
            LotusTurn = lotusTurn;
            LotusAlpha = lotusAlpha;
            TextAlpha = textAlpha;
            TextRise = textRise;
            LogoScale = logoScale;
            LogoAlpha = logoAlpha;
        }

        /// <summary>How far the iris is open: 0 covers the whole screen, 1 shows it all (the hole's radius over <see cref="LotusIrisLayout.MaxRadius"/>).</summary>
        public float Open { get; }

        public float GlowAlpha { get; }

        public float RingAlpha { get; }

        /// <summary>The share of the ring's petals lit, 0–1: the splash's progress; the transition's ring is all lit.</summary>
        public float RingLit { get; }

        /// <summary>The ring's turn in degrees clockwise (the transition's ring spins while it waits).</summary>
        public float RingSpin { get; }

        public float LotusScale { get; }

        /// <summary>The lotus' turn in degrees clockwise.</summary>
        public float LotusTurn { get; }

        public float LotusAlpha { get; }

        public float TextAlpha { get; }

        /// <summary>How far below its place the text still is, in reference units (it rises in).</summary>
        public float TextRise { get; }

        public float LogoScale { get; }

        /// <summary>The splash's logo; none in the transition.</summary>
        public float LogoAlpha { get; }

        /// <summary>Whether anything of the cover shows (the hosts hide the overlay otherwise).</summary>
        public bool Covers => Open < 1f;
    }

    /// <summary>Where the lotus loader's pieces go on a screen (<see cref="LotusIris.Layout"/>), in screen pixels, top-down.</summary>
    public readonly struct LotusIrisLayout
    {
        public LotusIrisLayout(float width, float height, float unit, float centerX, float centerY, Box lotus, float ringRadius, float petal, float textY, Box glow, float maxRadius)
        {
            Width = width;
            Height = height;
            Unit = unit;
            CenterX = centerX;
            CenterY = centerY;
            Lotus = lotus;
            RingRadius = ringRadius;
            Petal = petal;
            TextY = textY;
            Glow = glow;
            MaxRadius = maxRadius;
        }

        public float Width { get; }

        public float Height { get; }

        /// <summary>Pixels per reference unit (<see cref="DesignTokens.ScaleFor"/>).</summary>
        public float Unit { get; }

        /// <summary>The iris' middle and the lotus' middle.</summary>
        public float CenterX { get; }

        public float CenterY { get; }

        public Box Lotus { get; }

        /// <summary>The radius the ring's petals stand on.</summary>
        public float RingRadius { get; }

        /// <summary>A ring petal's box side (the <c>fx.petals</c> shape fitted into it, pointing outward).</summary>
        public float Petal { get; }

        /// <summary>The middle line of "Level N" (or "Loading...") under the lotus.</summary>
        public float TextY { get; }

        /// <summary>The soft light behind the lotus.</summary>
        public Box Glow { get; }

        /// <summary>The hole's radius that shows the whole screen, the rim included.</summary>
        public float MaxRadius { get; }

        /// <summary>The middle of ring petal <paramref name="index"/> turned by <paramref name="spin"/> degrees, and its turn (clockwise, 0 pointing up).</summary>
        public (float X, float Y, float Turn) PetalAt(int index, float spin)
        {
            float turn = (index * 360f / LotusIris.PetalCount) + spin;
            double a = (turn - 90f) * Math.PI / 180.0;
            return (CenterX + (float)(Math.Cos(a) * RingRadius), CenterY + (float)(Math.Sin(a) * RingRadius), turn);
        }

        /// <summary>The iris' hole for a pose: its radius in pixels (0 when closed).</summary>
        public float HoleRadius(LotusPose pose) => Math.Max(0f, Math.Min(1f, pose.Open)) * MaxRadius;
    }

    /// <summary>
    /// The lotus loader and the lotus iris (spec 005 FR-039, contracts/look.md §6.13; the owner's choice of 2026-10-06,
    /// "2 · Lotus" of the loading-screen GIFs, <c>tools/loading-gifs</c>). Both builds draw it the same way:
    /// <list type="bullet">
    /// <item><description>At launch (<see cref="Splash"/>) the logo and the lotus stand on the parchment while the ring of
    /// <see cref="PetalCount"/> petals round the lotus fills with the loading; then the iris opens from the lotus on the
    /// first screen.</description></item>
    /// <item><description>Between levels (<see cref="Transition"/>, the win's Next) the iris closes on the middle of the
    /// screen, the lotus pops in with "Level N" under it and the ring spinning, the next level comes up under the cover at
    /// <see cref="SwitchAt"/>, and the iris opens on it.</description></item>
    /// </list>
    /// The cover is <c>parchment.bottom</c> everywhere but a round hole: one picture of a hole (<see cref="UiRaster.IrisHole"/>)
    /// scaled to the hole's size plus up to four plain boxes round it (<see cref="CoverPanels"/>), so nothing is rendered
    /// per frame; a <c>lotus.fill</c> rim with a <c>lotus.line</c> edge runs round the hole. Engine-free.
    /// </summary>
    public static class LotusIris
    {
        /// <summary>The petals of the ring round the lotus.</summary>
        public const int PetalCount = 12;

        /// <summary>The transition: the iris closes over this long (ease in).</summary>
        public const float CloseSeconds = 0.5f;

        /// <summary>The transition: the cover holds this long once closed (the lotus, "Level N").</summary>
        public const float HoldSeconds = 0.9f;

        /// <summary>The transition: the iris opens over this long (ease in), the lotus growing and fading.</summary>
        public const float OpenSeconds = 0.5f;

        /// <summary>The transition: when the next level replaces the win, under the closed cover.</summary>
        public const float SwitchAt = CloseSeconds;

        /// <summary>The transition: when the iris starts to open.</summary>
        public const float OpenAt = CloseSeconds + HoldSeconds;

        public const float TransitionSeconds = OpenAt + OpenSeconds;

        /// <summary>The splash: when the ring starts to fill.</summary>
        public const float SplashFillFrom = 0.45f;

        /// <summary>The splash: the ring fills over at least this long, however fast the game loads.</summary>
        public const float SplashFillSeconds = 1.15f;

        /// <summary>The splash: the iris opens on the first screen over this long.</summary>
        public const float SplashOpenSeconds = 0.55f;

        /// <summary>The hole picture's side in pixels; the hosts scale it to the hole.</summary>
        public const int HoleRaster = 512;

        /// <summary>The hole's diameter as a share of the hole picture's side (the rest is cover).</summary>
        public const float HoleShare = 0.8f;

        /// <summary>The glow picture's side in pixels.</summary>
        public const int GlowRaster = 128;

        /// <summary>The picture keys (both hosts cache by them).</summary>
        public const string HoleKey = "ui.lotus_iris/hole";

        public const string GlowKey = "ui.lotus_iris/glow";

        /// <summary>The shape of the ring's petals (the win's falling petals).</summary>
        public const string PetalShape = "fx.petals";

        /// <summary>The middle of the lotus as a share of the safe height.</summary>
        public const float CenterShare = 0.48f;

        public const float LotusUnits = 340f;

        public const float RingUnits = 225f;

        public const float PetalUnits = 58f;

        /// <summary>A ring petal's outline: the petal drawn this much larger in <c>lotus.line</c> (or <c>parchment.line</c> unlit) behind it.</summary>
        public const float PetalLineScale = 1.16f;

        /// <summary>From the lotus' middle to the text's middle line.</summary>
        public const float TextUnits = 375f;

        /// <summary>"Level N" in <c>type.level_home</c> at this size; "Loading..." in <c>type.button_secondary</c>.</summary>
        public const float LevelTextScale = 1.25f;

        public const float GlowUnits = 1150f;

        public const float RimUnits = 10f;

        public const float RimLineUnits = 3f;

        /// <summary>The text rises this far as it fades in.</summary>
        public const float RiseUnits = 30f;

        /// <summary>The cover's color.</summary>
        public static Rgba Cover => C.ParchmentBottom;

        /// <summary>The glow behind the lotus.</summary>
        public static Rgba GlowColor => C.ParchmentTop.Lighten(0.5f);

        public static Rgba Rim => C.LotusFill;

        public static Rgba RimLine => C.LotusLine;

        /// <summary>A ring petal's fill at <paramref name="lit"/> (0 waiting, 1 lit).</summary>
        public static Rgba PetalFill(float lit) => C.LotusTip.Mix(C.LotusFill, Clamp01(lit));

        /// <summary>A ring petal's outline at <paramref name="lit"/>.</summary>
        public static Rgba PetalLine(float lit) => C.ParchmentLine.Mix(C.LotusLine, Clamp01(lit));

        /// <summary>"Level N" under the lotus; "Loading..." on the splash.</summary>
        public static Rgba TextColor(bool splash) => splash ? C.InkBrownSoft : C.InkTitle;

        public static LotusIrisLayout Layout(float width, float height, Insets insets)
        {
            float u = DesignTokens.ScaleFor(width, height);
            Box safe = ScreenLayout.SafeArea(width, height, insets);
            float cx = width / 2f;
            float cy = safe.Top + (safe.Height * CenterShare);
            float lotus = LotusUnits * u;
            float far = Math.Max(Distance(cx, cy, 0f, 0f), Math.Max(Distance(cx, cy, width, 0f), Math.Max(Distance(cx, cy, 0f, height), Distance(cx, cy, width, height))));
            return new LotusIrisLayout(width, height, u, cx, cy, Box.FromCenter(cx, cy, lotus, lotus), RingUnits * u, PetalUnits * u, cy + (TextUnits * u),
                Box.FromCenter(cx, cy, GlowUnits * u, GlowUnits * u), far + ((RimUnits + RimLineUnits + 4f) * u));
        }

        /// <summary>
        /// The transition at <paramref name="t"/> seconds after the win's Next: the iris closes (0–0.5 s), the lotus pops in
        /// once it is closed with the glow, the spinning ring and "Level N" rising under it, the next level comes up at
        /// <see cref="SwitchAt"/>, and the iris opens from <see cref="OpenAt"/> (the lotus growing and fading, the ring and
        /// the text gone in 0.2 s).
        /// </summary>
        public static LotusPose Transition(float t)
        {
            float closing = EaseIn(Seg(t, 0f, CloseSeconds));
            float opening = EaseIn(Seg(t, OpenAt, TransitionSeconds));
            float open = t < OpenAt ? 1f - closing : opening;
            float pop = Seg(t, CloseSeconds, CloseSeconds + 0.45f);
            float gone = 1f - Seg(t, OpenAt, OpenAt + 0.2f);
            float text = EaseOut(Seg(t, CloseSeconds + 0.1f, CloseSeconds + 0.45f));
            return new LotusPose(
                open,
                Seg(t, CloseSeconds, CloseSeconds + 0.3f) * (1f - opening),
                0.9f * Seg(t, CloseSeconds + 0.05f, CloseSeconds + 0.35f) * gone,
                1f,
                t * 126f,
                Back(pop) * Breath(t, 4f) * (1f + (1.4f * opening)),
                -30f * (1f - EaseOut(pop)),
                Clamp01(pop * 2f) * (1f - opening),
                text * gone,
                (1f - text) * RiseUnits,
                1f,
                0f);
        }

        /// <summary>
        /// The splash at <paramref name="t"/> seconds after launch: the logo and the lotus pop in, the ring fills with
        /// <paramref name="progress"/> (<see cref="SplashProgress"/>), "Loading..." fades in under it; from
        /// <paramref name="openSince"/> seconds (negative while it has not started) the iris opens on the first screen,
        /// the lotus growing and everything on the cover fading.
        /// </summary>
        public static LotusPose Splash(float t, float progress, float openSince = -1f)
        {
            float opening = openSince < 0f ? 0f : Seg(t, openSince, openSince + SplashOpenSeconds);
            float fade = 1f - opening;
            float logo = Seg(t, 0.1f, 0.6f);
            float lotus = Seg(t, 0.3f, 0.8f);
            return new LotusPose(
                EaseIn(opening),
                fade,
                EaseOut(Seg(t, 0.5f, 0.9f)) * fade,
                Clamp01(progress),
                0f,
                Back(lotus) * Breath(t, 4.2f) * (1f + (1.4f * opening)),
                4f * (float)Math.Sin(t * 2.1f),
                Clamp01(lotus * 2f) * fade,
                EaseOut(Seg(t, 0.7f, 1f)) * fade,
                0f,
                0.75f + (0.25f * Back(logo)),
                Clamp01(logo * 2f) * fade);
        }

        /// <summary>
        /// The ring's progress on the splash: the share loaded (<paramref name="loaded"/>, 0–1, 1 when the first screen is
        /// up), but never faster than a fill over <see cref="SplashFillSeconds"/> from <see cref="SplashFillFrom"/>, so the
        /// ring always fills petal by petal.
        /// </summary>
        public static float SplashProgress(float t, float loaded) =>
            Math.Min(Clamp01(loaded), EaseInOut(Seg(t, SplashFillFrom, SplashFillFrom + SplashFillSeconds)));

        /// <summary>Whether the splash may open: everything is loaded and the ring is full.</summary>
        public static bool SplashFull(float t, float loaded) => loaded >= 1f && SplashProgress(t, loaded) >= 1f;

        /// <summary>When the splash that opened at <paramref name="openSince"/> is gone.</summary>
        public static bool SplashDone(float t, float openSince) => openSince >= 0f && t >= openSince + SplashOpenSeconds;

        /// <summary>
        /// How lit ring petal <paramref name="index"/> is at <paramref name="lit"/> (0–1), and its pop: the newest petal swells
        /// as it lights (×1.35 at its middle).
        /// </summary>
        public static (float Lit, float Scale) PetalState(int index, float lit)
        {
            float on = Clamp01((lit * PetalCount) - index);
            float pop = on > 0f && on < 1f ? 1f + (0.35f * (float)Math.Sin(Math.PI * on)) : 1f;
            return (on, pop);
        }

        /// <summary>The hole picture's box for a hole of <paramref name="radius"/> round (cx, cy).</summary>
        public static Box HoleBox(float cx, float cy, float radius)
        {
            float side = 2f * radius / HoleShare;
            return Box.FromCenter(cx, cy, side, side);
        }

        /// <summary>
        /// The plain cover round the hole picture's box: up to four boxes (above, below, left and right of it) on a
        /// <paramref name="width"/> × <paramref name="height"/> screen. Their edges lie on whole pixels at least 1 px inside
        /// the picture, and the side boxes reach 1 px into the ones above and below, so no anti-aliased seam lets the
        /// screen show through.
        /// </summary>
        public static Box[] CoverPanels(float width, float height, Box hole)
        {
            var panels = new System.Collections.Generic.List<Box>(4);
            float top = Clamp((float)Math.Ceiling(hole.Top) + 1f, 0f, height);
            float bottom = Clamp((float)Math.Floor(hole.Bottom) - 1f, 0f, height);
            if (top > 0f)
            {
                panels.Add(new Box(0f, 0f, width, top));
            }

            if (bottom < height)
            {
                panels.Add(new Box(0f, bottom, width, height));
            }

            if (bottom > top)
            {
                float left = Clamp((float)Math.Ceiling(hole.Left) + 1f, 0f, width);
                float right = Clamp((float)Math.Floor(hole.Right) - 1f, 0f, width);
                float sideTop = Math.Max(0f, top - 1f);
                float sideBottom = Math.Min(height, bottom + 1f);
                if (left > 0f)
                {
                    panels.Add(new Box(0f, sideTop, left, sideBottom));
                }

                if (right < width)
                {
                    panels.Add(new Box(right, sideTop, width, sideBottom));
                }
            }

            return panels.ToArray();
        }

        private static float Distance(float x0, float y0, float x1, float y1) => (float)Math.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));

        private static float Breath(float t, float speed) => 1f + (0.04f * (float)Math.Sin(t * speed));

        private static float Seg(float t, float from, float to) => to > from ? Clamp01((t - from) / (to - from)) : (t >= to ? 1f : 0f);

        private static float EaseIn(float x) => x * x * x;

        private static float EaseOut(float x) => 1f - ((1f - x) * (1f - x) * (1f - x));

        private static float EaseInOut(float x) => x < 0.5f ? 4f * x * x * x : 1f - ((float)Math.Pow((-2f * x) + 2f, 3) / 2f);

        /// <summary>0 → 1 overshooting a little (an ease-out-back).</summary>
        private static float Back(float x)
        {
            const float s = 1.70158f;
            float k = x - 1f;
            return (k * k * (((s + 1f) * k) + s)) + 1f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));
    }

    public static partial class UiRaster
    {
        // ---- The lotus iris (spec 005 FR-039) ----

        /// <summary>
        /// The lotus iris' hole (<see cref="LotusIris"/>): <see cref="LotusIris.Cover"/> everywhere but a round hole in the
        /// middle, <see cref="LotusIris.HoleShare"/> of the side across, with a one-pixel soft edge. Deterministic, straight
        /// alpha.
        /// </summary>
        public static byte[] IrisHole(int width, int height)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            var color = new Color(LotusIris.Cover);
            float cx = width / 2f;
            float cy = height / 2f;
            float r = Math.Min(width, height) * LotusIris.HoleShare / 2f;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float d = Length(px + 0.5f - cx, py + 0.5f - cy) - r;
                    Put(pixels, width, px, py, color, Clamp01(0.5f + d));
                }
            }

            return pixels;
        }

        /// <summary>The soft light behind the lotus: <see cref="LotusIris.GlowColor"/>, full in the middle, fading smoothly to nothing at the edge.</summary>
        public static byte[] IrisGlow(int width, int height)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            var color = new Color(LotusIris.GlowColor);
            float cx = width / 2f;
            float cy = height / 2f;
            float r = Math.Min(width, height) / 2f;
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float k = 1f - Clamp01(Length(px + 0.5f - cx, py + 0.5f - cy) / r);
                    Put(pixels, width, px, py, color, Smooth(k) * 0.9f);
                }
            }

            return pixels;
        }
    }
}
