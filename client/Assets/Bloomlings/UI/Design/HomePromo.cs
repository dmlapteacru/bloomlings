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

    /// <summary>Garden objects on short sandstone stands. Shared poses keep Unity and Android previews identical.
    /// Attention alternates: Sprig shoves the AD sign off its stand, and a large physical stamp marks the open album.
    /// No frame sequences: the hosts transform a handful of transparent sprites.</summary>
    public static class HomePromo
    {
        /// <summary>A scene's width, as a share of the safe width W.</summary>
        public const float WidthShare = 0.32f;

        /// <summary>Home artwork is slightly smaller inside its unchanged touch rectangle.</summary>
        public const float HomeVisualScale = .92f;

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
        public const float CycleSeconds = 14f;

        public const float NoAdsAt = 1f;

        public const float DailyAt = 6.5f;

        /// <summary>The attention sequences' lengths.</summary>
        public const float NoAdsSeconds = 3.2f;

        public const float DailySeconds = 3.6f;

        /// <summary>The idle loops' lengths: No Ads' breathing and sway, the album's float.</summary>
        public const float NoAdsIdleSeconds = 2.4f;

        public const float DailyIdleSeconds = 2.6f;

        // The Decor pictures (pictures.md D14–D22).
        public const string DailyStand = "promo-daily-platform";
        public const string DailyBook = "promo-daily-book";
        public const string DailyBookOpen = "promo-daily-book-open";
        public const string DailyImprint = "promo-daily-imprint";
        public const string DailyStamp = "promo-daily-stamp";
        public const string DailySparkles = "promo-daily-sparkles";
        public const string NoAdsStand = "promo-noads-platform";
        public const string NoAdsSprig = "promo-noads-sprig";
        public const string NoAdsSprigPush = "promo-noads-sprig-push";
        public const string NoAdsSign = "promo-noads-sign";
        public const string NoAdsLotus = "promo-noads-lotus";

        /// <summary>Every picture of the two scenes, in the Decor folder.</summary>
        public static readonly string[] Pictures =
        {
            DailyStand, DailyBook, DailyBookOpen, DailyStamp, DailyImprint, DailySparkles, NoAdsStand, NoAdsSprig, NoAdsSprigPush, NoAdsSign, NoAdsLotus,
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
            return new Box(scene.Left + scene.Width * .22f, scene.Top + scene.Width * .466f,
                scene.Left + scene.Width * .78f, scene.Top + scene.Width * .544f);
        }

        /// <summary>The label's letters' height, as a share of the plaque's face (<see cref="Plaque"/>).</summary>
        public const float LabelShare = 0.9f;

        /// <summary>
        /// The cream plate a scene stands on on Home (spec 005 FR-036, the owner's tuning of 2026-10-05: "a light plate, as
        /// the buttons have"): the round buttons' cushion (<c>ui.button.round</c>) over the scene's box grown by this share
        /// of its width on every side, its corners <see cref="PlateRadiusShare"/> of the plate's width.
        /// </summary>
        public const float PlateGrowShare = 0.02f;

        public const float PlateRadiusShare = 0.22f;

        /// <summary>
        /// The soft shadows on Home (the owner's 16 px, 8 px down, on a 1080 px wide screen): under every picture of a scene,
        /// <c>garden.shadow</c> at <see cref="ShadowAlpha"/>, blurred by <see cref="ShadowBlurShare"/> of W and dropped
        /// <see cref="ShadowDropShare"/> of W; under the plate, at <see cref="PlateShadowAlpha"/>, blurred by
        /// <see cref="PlateShadowBlurShare"/> and dropped <see cref="PlateShadowDropShare"/>. The Remove Ads card's scene has
        /// neither plate nor shadows.
        /// </summary>
        public const float ShadowAlpha = 0.22f;

        public const float ShadowBlurShare = 8f / 1080f;

        public const float ShadowDropShare = 4f / 1080f;

        public const float PlateShadowAlpha = 0.4f;

        public const float PlateShadowBlurShare = 12.8f / 1080f;

        public const float PlateShadowDropShare = 6.4f / 1080f;

        /// <summary>The room a shadow picture keeps round its silhouette, in blurs (<see cref="ShadowOf"/>).</summary>
        public const float ShadowPad = 2.5f;

        /// <summary>The plate under a scene in <paramref name="scene"/> (<see cref="PlateGrowShare"/>).</summary>
        public static Box PlateBox(Box scene) => scene.Inset(-PlateGrowShare * scene.Width);

        /// <summary>The plate's corner radius.</summary>
        public static float PlateRadius(Box plate) => PlateRadiusShare * plate.Width;

        /// <summary>
        /// The plate's shadow picture's box (the plate grown by <see cref="ShadowPad"/> blurs and dropped) and the room it
        /// keeps round the plate, in screen px, for a safe width of <paramref name="w"/>.
        /// </summary>
        public static (Box Box, float Pad, float Blur) PlateShadowOf(Box plate, float w)
        {
            float blur = PlateShadowBlurShare * w;
            float pad = ShadowPad * blur;
            return (plate.Inset(-pad).Offset(0f, PlateShadowDropShare * w), pad, blur);
        }

        /// <summary>
        /// The shadow of a scene's picture as drawn now (<paramref name="layer"/>), for a safe width of <paramref name="w"/>:
        /// the same picture, pivot point, scale, turn and alpha, its canvas grown by <see cref="ShadowPad"/> blurs on every
        /// side (the room returned, in screen px of the unscaled canvas, with the blur) and dropped
        /// <see cref="ShadowDropShare"/> of W. Hosts draw every picture's shadow first, then the pictures.
        /// </summary>
        public static (PromoLayer Shadow, float Pad, float Blur) ShadowOf(PromoLayer layer, float w)
        {
            float blur = ShadowBlurShare * w;
            float pad = ShadowPad * blur;
            float width = layer.Width + (2f * pad);
            float height = layer.Height + (2f * pad);
            var shadow = new PromoLayer(
                layer.Picture,
                layer.X,
                layer.Y + (ShadowDropShare * w),
                width,
                height,
                ((layer.PivotX * layer.Width) + pad) / width,
                ((layer.PivotY * layer.Height) + pad) / height,
                layer.ScaleX,
                layer.ScaleY,
                layer.Rotation,
                layer.Alpha);
            return (shadow, pad, blur);
        }

        /// <summary>
        /// The shadow picture of <paramref name="picture"/>'s silhouette (<paramref name="mask"/>, its alpha) at
        /// <paramref name="width"/> × <paramref name="height"/> pixels for a shadow canvas of <paramref name="canvasWidth"/>
        /// screen px with <paramref name="pad"/> and <paramref name="blur"/> screen px (<see cref="ShadowOf"/>).
        /// </summary>
        public static byte[] Shadow(byte[] mask, int maskWidth, int maskHeight, int width, int height, float canvasWidth, float canvasHeight, float pad, float blur)
        {
            float kx = width / Math.Max(1f, canvasWidth);
            float ky = height / Math.Max(1f, canvasHeight);
            return UiRaster.SilhouetteShadow(mask, maskWidth, maskHeight, width, height, pad * kx, pad * ky, blur * (kx + ky) / 2f, ShadowAlpha);
        }

        /// <summary>The plate's shadow picture at <paramref name="width"/> × <paramref name="height"/> pixels (<see cref="PlateShadowOf"/>).</summary>
        public static byte[] PlateShadow(int width, int height, Box shadowBox, Box plate, float pad, float blur)
        {
            float kx = width / Math.Max(1f, shadowBox.Width);
            float ky = height / Math.Max(1f, shadowBox.Height);
            return UiRaster.RoundShadow(width, height, pad * kx, pad * ky, PlateRadius(plate) * kx, blur * (kx + ky) / 2f, PlateShadowAlpha);
        }

        /// <summary>The cache key of a scene picture's shadow (the painters add the size).</summary>
        public static string ShadowKey(string picture) => "ui.promo.shadow/" + picture;

        /// <summary>The cache key of the plate's shadow.</summary>
        public const string PlateShadowKey = "ui.promo.plate_shadow";

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

        /// <summary>A short, shared tap spring. The host transforms art, never the touch rectangle.</summary>
        public static float PressScale(bool down, float sinceRelease) => down ? .94f :
            sinceRelease < 0f || sinceRelease >= .22f ? 1f : Key(sinceRelease, (0f, .94f), (.10f, 1.02f), (.22f, 1f));

        public static readonly TypeStyle LabelStyle = new TypeStyle("type.shop.home_promo", 42f, false, false, 0f, 20f);
        public const string ChallengePicture = "promo-challenge-board";
        public static Box ChallengeLabel(Box box) => new Box(box.Left + box.Width * .21f, box.Top + box.Height * .827f,
            box.Right - box.Width * .21f, box.Top + box.Height * .912f);
        public static Box SealBox(Box box) => Box.FromCenter(box.Left + box.Width * .79f,
            box.Top + box.Width * .07f, box.Width * .18f, box.Width * .18f);
        public static bool CastsShadow(string picture) => picture == DailyStand || picture == NoAdsStand;

        /// <summary>Wind up, open, hover, press, lift to reveal the mark, then close. All phases remain readable at Home size.</summary>
        private static IReadOnlyList<PromoLayer> Daily(Box box, float t, float a)
        {
            float open = a < 0 ? 0 : Key(a, (0f, 0f), (.15f, 0f), (.55f, 1f), (3.05f, 1f), (DailySeconds, 0f));
            float dip = a < 0 ? 0 : Key(a, (1.42f, 0f), (1.55f, .025f), (1.9f, 0f));
            float closedScale = a < 0 ? 1f : Key(a, (0f, 1f), (.15f, .92f), (.55f, 1f));
            float pop = a < 0 ? 1f : Key(a, (.15f, .85f), (.55f, 1.06f), (.75f, 1f));
            float stampAlpha = a < 0 ? 0 : Key(a, (.65f, 0f), (.9f, 1f), (2f, 1f), (2.35f, 0f));
            float stampX = a < 0 ? .64f : Key(a, (.65f, .80f), (.95f, .64f), (1.9f, .64f), (2.35f, .80f));
            float stampY = a < 0 ? .04f : Key(a, (.65f, -.02f), (.95f, .04f), (1.25f, .04f), (1.55f, .27f), (1.8f, .17f), (2.35f, -.03f));
            float stampSy = a < 0 ? 1f : Key(a, (1.4f, 1f), (1.55f, .9f), (1.75f, 1f));
            float imprint = a < 0 ? 0 : Key(a, (1.5f, 0f), (1.57f, 1f));
            float glint = a < 0 ? 0 : Key(a, (1.55f, 0f), (1.7f, 1f), (2.35f, 0f));
            return new[]
            {
                Stand(DailyStand, box),
                Prop(DailyBook, box, .5f, .43f, .54f, .9f, closedScale, closedScale, 0f, 1f - open),
                Prop(DailyBookOpen, box, .5f, .46f + dip, .78f, .9f, pop, pop, 0f, open),
                Prop(DailyImprint, box, .64f, .27f + dip, .18f, .5f, 1f, .72f, -5f, imprint * open),
                Prop(DailyStamp, box, stampX, stampY + dip, .27f, .96f, 1f, stampSy, 0f, stampAlpha * open),
                Prop(DailySparkles, box, .82f, .17f, .18f, .5f, 1f, 1f, 0f, glint),
            };
        }

        /// <summary>Sprig braces and shoves the sign off the stand, celebrates the bloom, and resets for the next loop.</summary>
        private static IReadOnlyList<PromoLayer> NoAds(Box box, float t, float a)
        {
            float idle = Bump((t % NoAdsIdleSeconds) / NoAdsIdleSeconds);
            float push = a < 0 ? 0 : Key(a, (0f, 0f), (.12f, 0f), (.3f, 1f), (2.45f, 1f), (2.9f, 0f));
            float x = a < 0 ? 0 : Key(a, (0f, 0f), (.2f, -.025f), (.62f, .09f), (.95f, .05f), (2.4f, .02f), (NoAdsSeconds, 0f));
            float hop = a < 0 ? 0 : Key(a, (.7f, 0f), (.9f, -.09f), (1.12f, 0f), (1.4f, -.06f), (1.6f, 0f));
            float lean = a < 0 ? 0 : Key(a, (0f, 0f), (.2f, -8f), (.6f, 12f), (.85f, -5f), (1.3f, 0f));
            float squash = a < 0 ? 1f : Key(a, (0f, 1f), (.2f, .9f), (.5f, 1.04f), (.65f, 1f));
            float signX = .72f, signY = .43f, signRot = 3f, signAlpha = 1f;
            if (a >= 0 && a < 2.45f)
            {
                signX += Key(a, (.2f, 0f), (.6f, .06f), (.95f, .18f), (1.32f, .38f));
                signY += Key(a, (.6f, 0f), (.86f, -.07f), (1.32f, .18f));
                signRot = Key(a, (.2f, 3f), (.6f, 18f), (.95f, 60f), (1.32f, 110f));
                signAlpha = Key(a, (1.05f, 1f), (1.32f, 0f));
            }
            else if (a >= 2.45f)
            {
                signX += Key(a, (2.45f, .28f), (NoAdsSeconds, 0f));
                signY += Key(a, (2.45f, .04f), (NoAdsSeconds, 0f));
                signRot = Key(a, (2.45f, -22f), (2.85f, 5f), (NoAdsSeconds, 3f));
                signAlpha = Key(a, (2.45f, 0f), (2.8f, 1f));
            }
            float bloom = a < 0 ? 0 : Key(a, (.95f, 0f), (1.25f, 1f), (2.25f, 1f), (2.75f, 0f));
            float bloomScale = a < 0 ? 1f : Key(a, (.95f, .2f), (1.4f, 1.12f), (1.65f, 1f), (2.25f, 1f), (2.75f, .6f));
            float sx = 1f - .008f * idle, sy = (1f + .015f * idle) * squash;
            return new[]
            {
                Stand(NoAdsStand, box),
                Prop(NoAdsSprig, box, .32f + x, .43f + hop, .50f, .96f, sx, sy, -idle + lean, 1f - push),
                Prop(NoAdsSprigPush, box, .32f + x, .43f + hop, .50f, .96f, sx, sy, -idle + lean, push),
                Prop(NoAdsLotus, box, .72f, .43f, .34f, .9f, bloomScale, bloomScale, 0f, bloom),
                Prop(NoAdsSign, box, signX, signY, .40f, .86f, 1f, 1f, signRot, signAlpha),
            };
        }

        private static PromoLayer Stand(string picture, Box box) =>
            new PromoLayer(picture, box.Left, box.Top + box.Width * .04f, box.Width, box.Width * .60f, 0f, 0f, 1f, 1f, 0f, 1f);

        private static PromoLayer Prop(string picture, Box box, float x, float y, float size, float pivotY,
            float sx, float sy, float rotation, float alpha) =>
            new PromoLayer(picture, box.Left + box.Width * x, box.Top + box.Width * y, box.Width * size,
                box.Width * size, .5f, pivotY, sx, sy, rotation, Math.Max(0f, Math.Min(1f, alpha)));

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
