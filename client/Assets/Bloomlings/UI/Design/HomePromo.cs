using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Home's two promo scenes (spec 005 FR-032): No Ads at the left, the Daily Reward at the right.</summary>
    public enum PromoScene
    {
        NoAds,
        Daily,
    }

    /// <summary>
    /// One picture of a promo scene as drawn at a moment (<see cref="HomePromo.Layers"/>): the picture's whole canvas
    /// <see cref="Width"/> × <see cref="Height"/> screen px with its pivot (<see cref="PivotX"/>, <see cref="PivotY"/>, shares
    /// of the canvas) on (<see cref="X"/>, <see cref="Y"/>), then scaled by <see cref="ScaleX"/> × <see cref="ScaleY"/> and
    /// turned <see cref="Rotation"/> degrees clockwise around the pivot, at <see cref="Alpha"/>. Screen px, y down.
    /// </summary>
    public readonly struct PromoLayer
    {
        public PromoLayer(string picture, float x, float y, float width, float height, float pivotX, float pivotY, float scaleX, float scaleY, float rotation, float alpha)
        {
            Picture = picture;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            PivotX = pivotX;
            PivotY = pivotY;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Rotation = rotation;
            Alpha = alpha;
        }

        /// <summary>The picture's name in the Decor folder (<see cref="HomePromo.Pictures"/>).</summary>
        public string Picture { get; }

        public float X { get; }

        public float Y { get; }

        public float Width { get; }

        public float Height { get; }

        public float PivotX { get; }

        public float PivotY { get; }

        public float ScaleX { get; }

        public float ScaleY { get; }

        public float Rotation { get; }

        public float Alpha { get; }

        /// <summary>The unscaled, unturned box of the canvas (the pivot on <see cref="X"/>, <see cref="Y"/>).</summary>
        public Box Box => new Box(X - (Width * PivotX), Y - (Height * PivotY), X + (Width * (1f - PivotX)), Y + (Height * (1f - PivotY)));
    }

    /// <summary>
    /// Home's two promo scenes (spec 005 FR-032, contracts/look.md §6.4.1; the owner's pack
    /// <c>bloomlings_daily_noads_anim_assets</c> of 2026-10-04 and the preview the owner approved on the same day): the owner's
    /// layers on a flowered stone stand with a wooden plaque carrying the scene's label. No Ads, at the left: Sprig pushes
    /// the crossed AD sign, which leans on its palms; Daily, at the right: a closed album. Each scene idles all the time
    /// and, while it calls for attention, plays its attention sequence once every <see cref="CycleSeconds"/>, the two never
    /// together (<see cref="AttentionAt"/>):
    /// <list type="bullet">
    /// <item><description>No Ads (<see cref="NoAdsSeconds"/>): Sprig crouches, lunges and pushes the sign off the stand, tipping
    /// it over its bottom-right corner; Sprig hops and is gone; a lotus blooms in their place and breathes; then it folds
    /// away and the two pop back.</description></item>
    /// <item><description>Daily (<see cref="DailySeconds"/>): the album squashes and pops open; the flower stamp appears over its
    /// right page, hovers, lifts and presses down (the album dips); only then the petals and sparkles burst out and fade;
    /// the album closes again.</description></item>
    /// </list>
    /// The pictures keep their whole canvases (the owner's 1448 × 1086 stands and 1254 × 1254 layers, fitted into the Decor
    /// folder at <see cref="StandCanvas"/> and <see cref="LayerCanvas"/>), so every pivot below is in the owner's pixels.
    /// Engine-free.
    /// </summary>
    public static class HomePromo
    {
        /// <summary>A scene's width, as a share of the safe width W.</summary>
        public const float WidthShare = 0.27f;

        /// <summary>A scene box's height, as a share of its width: the stand's canvas from 70 px above it to its 900th row.</summary>
        public const float HeightShare = 970f / 1448f;

        /// <summary>From the scene box's top to the stand canvas's top, in the stand's pixels (room for the props above it).</summary>
        public const float StandTop = 70f;

        /// <summary>The owner's canvases: the stands (1448 × 1086) and the other layers (1254 × 1254).</summary>
        public const float StandWidth = 1448f;

        public const float StandHeight = 1086f;

        public const float LayerSide = 1254f;

        /// <summary>The fitted pictures' sizes in the Decor folder.</summary>
        public static readonly (int Width, int Height) StandCanvas = (724, 543);

        public static readonly (int Width, int Height) LayerCanvas = (512, 512);

        /// <summary>How often each scene's attention sequence plays, and when in that cycle (seconds since Home opened).</summary>
        public const float CycleSeconds = 12f;

        public const float NoAdsAt = 1f;

        public const float DailyAt = 6.5f;

        /// <summary>The attention sequences' lengths.</summary>
        public const float NoAdsSeconds = 3.2f;

        public const float DailySeconds = 2.6f;

        /// <summary>The idle loops' lengths: No Ads' breathing and sway, the album's float.</summary>
        public const float NoAdsIdleSeconds = 2.4f;

        public const float DailyIdleSeconds = 2.6f;

        // The Decor pictures (pictures.md D14–D22).
        public const string DailyStand = "promo-daily-platform";
        public const string DailyBook = "promo-daily-book";
        public const string DailyBookOpen = "promo-daily-book-open";
        public const string DailyStamp = "promo-daily-stamp";
        public const string DailySparkles = "promo-daily-sparkles";
        public const string NoAdsStand = "promo-noads-platform";
        public const string NoAdsSprig = "promo-noads-sprig";
        public const string NoAdsSign = "promo-noads-sign";
        public const string NoAdsLotus = "promo-noads-lotus";

        /// <summary>Every picture of the two scenes, in the Decor folder.</summary>
        public static readonly string[] Pictures =
        {
            DailyStand, DailyBook, DailyBookOpen, DailyStamp, DailySparkles, NoAdsStand, NoAdsSprig, NoAdsSign, NoAdsLotus,
        };

        /// <summary>The asset slot of a scene (both its stand and its props).</summary>
        public static string Slot(PromoScene scene) => scene == PromoScene.NoAds ? "ui.promo.no_ads" : "ui.promo.daily";

        /// <summary>The label's string key (on the stand's plaque).</summary>
        public static string LabelKey(PromoScene scene) => scene == PromoScene.NoAds ? "home.promo_no_ads" : "home.promo_daily";

        /// <summary>The scene's box for a scene <paramref name="width"/> wide with its top-left corner on (<paramref name="left"/>, <paramref name="top"/>).</summary>
        public static Box SceneBox(float left, float top, float width) => new Box(left, top, left + width, top + (width * HeightShare));

        /// <summary>
        /// The face of the stand's wooden plaque, where the label goes (the Daily plaque's face from 370 to 1090 across and
        /// 635 to 810 down the stand; No Ads' from 385 to 1065 and 640 to 820).
        /// </summary>
        public static Box Plaque(Box scene, PromoScene kind)
        {
            float u = scene.Width / StandWidth;
            float oy = scene.Top + (StandTop * u);
            return kind == PromoScene.NoAds
                ? new Box(scene.Left + (385 * u), oy + (640 * u), scene.Left + (1065 * u), oy + (820 * u))
                : new Box(scene.Left + (370 * u), oy + (635 * u), scene.Left + (1090 * u), oy + (810 * u));
        }

        /// <summary>The label's letters' height, as a share of the plaque's face (<see cref="Plaque"/>).</summary>
        public const float LabelShare = 0.74f;

        /// <summary>
        /// The time into the scene's attention sequence at <paramref name="seconds"/> since Home opened, or −1 when it idles:
        /// No Ads from <see cref="NoAdsAt"/>, the Daily Reward from <see cref="DailyAt"/>, every <see cref="CycleSeconds"/>,
        /// so the two never play together. A scene that does not call for attention (<paramref name="calling"/> false: the
        /// Daily Reward already claimed today) always idles.
        /// </summary>
        public static float AttentionAt(PromoScene scene, float seconds, bool calling)
        {
            if (!calling)
            {
                return -1f;
            }

            float a = seconds - (scene == PromoScene.NoAds ? NoAdsAt : DailyAt);
            if (a < 0f)
            {
                return -1f;
            }

            a %= CycleSeconds;
            return a <= (scene == PromoScene.NoAds ? NoAdsSeconds : DailySeconds) ? a : -1f;
        }

        /// <summary>
        /// The scene's pictures back to front at <paramref name="seconds"/> since Home opened (<see cref="AttentionAt"/>),
        /// in the scene <paramref name="box"/> (<see cref="SceneBox"/>). The plaque's label is the host's, over the stand
        /// (<see cref="Plaque"/>): after the first layer.
        /// </summary>
        public static IReadOnlyList<PromoLayer> Layers(PromoScene scene, Box box, float seconds, bool calling)
        {
            float a = AttentionAt(scene, seconds, calling);
            return scene == PromoScene.NoAds ? NoAds(box, seconds, a) : Daily(box, seconds, a);
        }

        // ---- Daily ----

        private static IReadOnlyList<PromoLayer> Daily(Box box, float t, float a)
        {
            float b = box.Width, u = b / StandWidth, k = b / 240f;
            float ox = box.Left, oy = box.Top + (StandTop * u);
            var layers = new List<PromoLayer> { Stand(DailyStand, ox, oy, b) };

            float idle = Bump((t % DailyIdleSeconds) / DailyIdleSeconds);
            bool on = a >= 0f;
            float bx = ox + (725 * u), by = oy + (590 * u) - (4f * k * idle);
            float idleRot = -2f * idle, idleScale = 1f + (0.02f * idle);
            float sb = 0.37f * b / 915f;

            // The closed album squashes, the open one pops in and dips under the stamp, then the album closes again.
            float closedAlpha = 1f, openAlpha = 0f, cScale = 1f, cRot = 0f, oScale = 1f, oRot = 0f, oY = 0f;
            if (on)
            {
                if (a < 0.15f)
                {
                    cScale = Key(a, (0f, 1f), (0.15f, 0.94f));
                    cRot = Key(a, (0f, 0f), (0.15f, -4f));
                }
                else
                {
                    openAlpha = a < 2.2f ? 1f : Key(a, (2.2f, 1f), (2.55f, 0f));
                    closedAlpha = 1f - openAlpha;
                    oScale = Key(a, (0.15f, 0.85f), (0.30f, 1.05f), (0.45f, 1f)) * Key(a, (1.22f, 1f), (1.32f, 0.97f), (1.45f, 1f));
                    oRot = Key(a, (0.15f, -4f), (0.30f, 1f), (0.45f, 0f));
                    oY = Key(a, (0.15f, 0f), (0.27f, -10f * k), (0.45f, 0f));
                }
            }

            layers.Add(Layer(DailyBook, bx, by, sb, 639, 1057, idleScale * cScale, idleScale * cScale, idleRot + cRot, closedAlpha));
            layers.Add(Layer(DailyBookOpen, bx, by + oY, sb, 637, 1025, idleScale * oScale, idleScale * oScale, idleRot + oRot, openAlpha));

            // The stamp over the right page: it fades in hovering, lifts, presses down and settles.
            if (on && a >= 0.5f)
            {
                float y = Key(a, (0.50f, -50f * k), (0.85f, -42f * k), (1.05f, -52f * k), (1.27f, 0f));
                float sc = Key(a, (0.50f, 0.8f), (0.70f, 1.1f), (1.05f, 1.12f), (1.30f, 0.92f), (1.42f, 1.04f), (1.55f, 1f));
                float rot = Key(a, (0.50f, -14f), (1.05f, -14f), (1.30f, -8f));
                float alpha = Key(a, (0.50f, 0f), (0.70f, 1f)) * openAlpha;
                layers.Add(Layer(DailyStamp, bx + (323 * sb), by + oY - (385 * sb) + y, 0.14f * b / 1131f, 628, 612, sc, sc, rot, alpha));
            }

            // The petals and sparkles: only once the stamp is down, bursting out of the album and fading.
            if (on && a >= 1.3f)
            {
                float alpha = Key(a, (1.30f, 0f), (1.45f, 0.85f), (1.85f, 0.75f), (2.40f, 0f));
                float sc = Key(a, (1.30f, 0.5f), (1.70f, 1f), (2.40f, 1.15f));
                float rise = Key(a, (1.30f, 0f), (2.40f, -20f * k));
                layers.Add(Layer(DailySparkles, bx, by - (560 * sb) + rise, 0.6f * b / 1027f, 641, 627, sc, sc, Key(a, (1.3f, -4f), (2.4f, 4f)), alpha));
            }

            return layers;
        }

        // ---- No Ads ----

        private readonly struct Pose
        {
            public Pose(float x, float y, float rot, float sx, float sy, float scale, float alpha)
            {
                X = x;
                Y = y;
                Rot = rot;
                Sx = sx;
                Sy = sy;
                Scale = scale;
                Alpha = alpha;
            }

            public float X { get; }

            public float Y { get; }

            public float Rot { get; }

            public float Sx { get; }

            public float Sy { get; }

            public float Scale { get; }

            public float Alpha { get; }
        }

        /// <summary>Sprig's pose: breathing and swaying; in the sequence a crouch, a lunge, a hop, gone, and back.</summary>
        private static Pose SprigPose(float t, float a, float k)
        {
            float idle = Bump((t % NoAdsIdleSeconds) / NoAdsIdleSeconds);
            float x = 6f * k * idle, y = 0f, rot = -3f * idle, sy = 1f + (0.03f * idle), sx = 1f - (0.01f * idle), scale = 1f, alpha = 1f;
            if (a >= 0f && a < 2.6f)
            {
                x += Key(a, (0f, 0f), (0.14f, -8f * k), (0.42f, 18f * k));
                sy *= Key(a, (0f, 1f), (0.14f, 0.92f), (0.30f, 1.05f), (0.42f, 1f));
                rot += Key(a, (0f, 0f), (0.14f, 6f), (0.42f, 0f));
                y = Key(a, (0.48f, 0f), (0.58f, -10f * k), (0.68f, 0f));
                scale = Key(a, (0.66f, 1f), (0.84f, 0.5f));
                alpha = Key(a, (0.66f, 1f), (0.84f, 0f));
            }
            else if (a >= 2.6f)
            {
                scale = Key(a, (2.75f, 0.6f), (3.0f, 1.06f), (3.2f, 1f));
                alpha = Key(a, (2.75f, 0f), (2.95f, 1f));
            }

            return new Pose(x, y, rot, sx, sy, scale, alpha);
        }

        private static IReadOnlyList<PromoLayer> NoAds(Box box, float t, float a)
        {
            float b = box.Width, u = b / StandWidth, k = b / 240f;
            float ox = box.Left, oy = box.Top + (StandTop * u);
            var layers = new List<PromoLayer> { Stand(NoAdsStand, ox, oy, b) };

            float sSprig = 0.38f * b / 1072f, sSign = 0.31f * b / 941f;
            float baseX = ox + (0.34f * b), baseY = oy + (612 * u), signY = oy + (620 * u);

            // The front palm's tip (the sprig picture's 1150, 720) on the screen for a pose.
            (float X, float Y) Palm(Pose q)
            {
                float r = q.Rot * (float)Math.PI / 180f;
                float dx = sSprig * q.Scale * q.Sx * (1150 - 634), dy = sSprig * q.Scale * q.Sy * (720 - 1187);
                return (baseX + q.X + (dx * (float)Math.Cos(r)) - (dy * (float)Math.Sin(r)), baseY + q.Y + (dx * (float)Math.Sin(r)) + (dy * (float)Math.Cos(r)));
            }

            // Where the sign's pivot (its bottom-right corner, 1098, 1072) stands so that its left edge, tilted by tilt
            // degrees, covers the palm's tip by a little: the hands press on its side.
            float Attach((float X, float Y) palm, float tilt)
            {
                float r = tilt * (float)Math.PI / 180f;
                float dx = 158 - 1098;
                float dy = (((palm.Y - signY) / sSign) - (dx * (float)Math.Sin(r))) / (float)Math.Cos(r);
                float edge = sSign * ((dx * (float)Math.Cos(r)) - (dy * (float)Math.Sin(r)));
                return palm.X - (75 * sSprig) - edge;
            }

            float idleTilt = 4f + (2f * (float)Math.Sin(2.0 * Math.PI * t / NoAdsIdleSeconds));
            Pose pose = SprigPose(t, a, k);
            float tilt = idleTilt, signScale = 1f, signAlpha = 1f, signDy = 0f;
            float px = Attach(Palm(pose), idleTilt);
            float lotusAlpha = 0f, lotusScale = 1f;
            if (a >= 0f && a < 2.6f)
            {
                if (a < 0.42f)
                {
                    // The crouch leaves the sign where it stood; the lunge pushes it and tips it over its corner.
                    tilt = Key(a, (0.14f, idleTilt), (0.42f, 16f));
                    px = Math.Max(Attach(Palm(SprigPose(t, -1f, k)), idleTilt), Attach(Palm(pose), tilt));
                }
                else
                {
                    float pushed = Attach(Palm(SprigPose(t - a + 0.42f, 0.42f, k)), 16f);
                    px = pushed + Key(a, (0.42f, 0f), (0.85f, 0.38f * b));
                    tilt = Key(a, (0.42f, 16f), (0.85f, 50f));
                    signDy = Key(a, (0.42f, 0f), (0.58f, -14f * k), (0.85f, 12f * k));
                    signScale = Key(a, (0.42f, 1f), (0.85f, 0.8f));
                    signAlpha = Key(a, (0.55f, 1f), (0.85f, 0f));
                }

                lotusAlpha = Key(a, (0.62f, 0f), (0.85f, 1f));
                lotusScale = Key(a, (0.62f, 0.4f), (0.95f, 1.12f), (1.15f, 1f)) * (a > 1.15f ? 1f + (0.03f * Bump((a - 1.15f) / 1.45f)) : 1f);
            }
            else if (a >= 2.6f)
            {
                // The lotus folds away and Sprig and the sign pop back.
                lotusAlpha = Key(a, (2.6f, 1f), (2.9f, 0f));
                lotusScale = Key(a, (2.6f, 1f), (2.9f, 0.8f));
                signScale = Key(a, (2.75f, 0.6f), (3.0f, 1.06f), (3.2f, 1f));
                signAlpha = Key(a, (2.75f, 0f), (2.95f, 1f));
            }

            layers.Add(Layer(NoAdsLotus, ox + (700 * u), oy + (600 * u), 0.6f * b / 1128f, 614, 1096, lotusScale, lotusScale, 0f, lotusAlpha));
            layers.Add(Layer(NoAdsSprig, baseX + pose.X, baseY + pose.Y, sSprig, 634, 1187, pose.Sx * pose.Scale, pose.Sy * pose.Scale, pose.Rot, pose.Alpha));
            layers.Add(Layer(NoAdsSign, px, signY + signDy, sSign, 1098, 1072, signScale, signScale, tilt, signAlpha));
            return layers;
        }

        // ---- Helpers ----

        private static PromoLayer Stand(string picture, float ox, float oy, float width) =>
            new PromoLayer(picture, ox, oy, width, width * StandHeight / StandWidth, 0f, 0f, 1f, 1f, 0f, 1f);

        /// <summary>A 1254-square layer drawn at <paramref name="scale"/> screen px per owner pixel, its pivot in owner pixels.</summary>
        private static PromoLayer Layer(string picture, float x, float y, float scale, float pivotX, float pivotY, float sx, float sy, float rotation, float alpha)
        {
            float side = LayerSide * scale;
            return new PromoLayer(picture, x, y, side, side, pivotX / LayerSide, pivotY / LayerSide, sx, sy, rotation, Math.Max(0f, Math.Min(1f, alpha)));
        }

        /// <summary>0 → 1 → 0 over a phase of 0 to 1 (a cosine bump: eases in and out).</summary>
        private static float Bump(float phase) => (1f - (float)Math.Cos(2.0 * Math.PI * phase)) / 2f;

        private static float Smooth(float x) => x <= 0f ? 0f : x >= 1f ? 1f : x * x * (3f - (2f * x));

        /// <summary>The value at <paramref name="t"/> of keyframes eased in and out between them, held before and after.</summary>
        private static float Key(float t, params (float T, float V)[] keys)
        {
            if (t <= keys[0].T)
            {
                return keys[0].V;
            }

            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (t <= keys[i + 1].T)
                {
                    float f = Smooth((t - keys[i].T) / Math.Max(1e-4f, keys[i + 1].T - keys[i].T));
                    return keys[i].V + ((keys[i + 1].V - keys[i].V) * f);
                }
            }

            return keys[keys.Length - 1].V;
        }
    }
}
