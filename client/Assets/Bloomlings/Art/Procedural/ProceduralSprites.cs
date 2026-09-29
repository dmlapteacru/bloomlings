using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.Art
{
    /// <summary>
    /// Placeholder flat art generated at runtime from signed distance functions: tile and panel shapes, one distinct
    /// icon per variant (leaf, moss tuft, flower, bud, wave, dew droplet, log, acorn, …) and one silhouette per family.
    /// Every variant has its own shape, so hue never carries meaning alone (FR-005, FR-072). Final art replaces these
    /// through <see cref="Variants.VariantVisualCatalog"/>.
    /// </summary>
    public static class ProceduralSprites
    {
        private const int IconSize = 96;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>A white rounded square with a 9-slice border.</summary>
        public static Sprite RoundedSquare => Get("rounded", 64, (x, y) => RoundedBox(x, y, 0f, 0f, 1f, 1f, 0.35f), border: 22f);

        public static Sprite Circle => Get("circle", 64, (x, y) => Length(x, y) - 1f);

        /// <summary>A soft ring used for the Garden Entry marker and highlights.</summary>
        public static Sprite Ring => Get("ring", 64, (x, y) => Mathf.Abs(Length(x, y) - 0.8f) - 0.14f);

        public static Sprite Lock => Get("lock", IconSize, (x, y) => Min(
            RoundedBox(x, y, 0f, -0.3f, 0.62f, 0.5f, 0.12f),
            Mathf.Abs(Length(x, y - 0.25f) - 0.36f) - 0.1f + Step(y < 0.2f)));

        public static Sprite Key => Get("key", IconSize, (x, y) => Min(
            Mathf.Abs(Length(x + 0.45f, y) - 0.32f) - 0.1f,
            RoundedBox(x, y, 0.3f, 0f, 0.55f, 0.09f, 0.02f),
            RoundedBox(x, y, 0.7f, -0.16f, 0.08f, 0.14f, 0.02f)));

        public static Sprite Question => Get("question", IconSize, (x, y) => Min(
            Mathf.Abs(Length(x, y - 0.3f) - 0.36f) - 0.1f + Step(y < 0.15f && x < 0.05f),
            RoundedBox(x, y, 0f, -0.2f, 0.09f, 0.18f, 0.04f),
            Length(x, y + 0.65f) - 0.12f));

        /// <summary>A five-point star (Hard label).</summary>
        public static Sprite Star => Get("star", IconSize, (x, y) => StarShape(x, y, 0.9f));

        /// <summary>Two stars (Super Hard label).</summary>
        public static Sprite DoubleStar => Get("star2", IconSize, (x, y) => Min(StarShape((x + 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f, StarShape((x - 0.42f) * 1.7f, y * 1.7f, 0.9f) / 1.7f));

        /// <summary>A cross, used to show that a pod ignores a tile of another variant (FR-071).</summary>
        public static Sprite Cross => Get("cross", IconSize, (x, y) =>
        {
            const float c = 0.70710678f;
            float u = (c * x) + (c * y);
            float v = (-c * x) + (c * y);
            return Min(RoundedBox(u, v, 0f, 0f, 0.85f, 0.14f, 0.1f), RoundedBox(u, v, 0f, 0f, 0.14f, 0.85f, 0.1f));
        });

        /// <summary>A gear for the Settings button (no font glyph needed).</summary>
        public static Sprite Gear => Get("gear", IconSize, (x, y) =>
        {
            float teeth = Mathf.Cos(8f * Mathf.Atan2(y, x)) > 0f ? 0.16f : 0f;
            return Max(Length(x, y) - (0.62f + teeth), -(Length(x, y) - 0.26f));
        });

        /// <summary>A garden gate / hedge seal: an arch with bars (FR-037).</summary>
        public static Sprite Gate => Get("gate", IconSize, (x, y) => Max(
            Min(RoundedBox(x, y, 0f, -0.2f, 0.75f, 0.6f, 0.05f), Length(x, y - 0.4f) - 0.75f),
            -Min(
                RoundedBox(x, y, -0.4f, -0.2f, 0.07f, 0.55f, 0.03f),
                RoundedBox(x, y, 0f, -0.1f, 0.07f, 0.7f, 0.03f),
                RoundedBox(x, y, 0.4f, -0.2f, 0.07f, 0.55f, 0.03f))));

        /// <summary>A Fountain: a basin, a column and a spray (FR-038).</summary>
        public static Sprite Fountain => Get("fountain", IconSize, (x, y) => Min(
            RoundedBox(x, y, 0f, -0.6f, 0.85f, 0.2f, 0.15f),
            RoundedBox(x, y, 0f, -0.15f, 0.12f, 0.35f, 0.05f),
            Mathf.Abs(Length(x, y - 0.1f) - 0.55f) - 0.07f + Step(y < 0.3f)));

        /// <summary>Extra Slot: a slot with a plus.</summary>
        public static Sprite PlusSlot => Get("b_extra", IconSize, (x, y) => Max(
            RoundedBox(x, y, 0f, 0f, 0.8f, 0.8f, 0.2f),
            -Min(RoundedBox(x, y, 0f, 0f, 0.5f, 0.12f, 0.05f), RoundedBox(x, y, 0f, 0f, 0.12f, 0.5f, 0.05f))));

        /// <summary>Shuffle: two crossing arrows.</summary>
        public static Sprite ShuffleArrows => Get("b_shuffle", IconSize, (x, y) => Min(
            RoundedBox(x - y, y, 0f, 0f, 0.1f, 0.6f, 0.05f),
            RoundedBox(x + y, y, 0f, 0f, 0.1f, 0.6f, 0.05f),
            Max(Length(x - 0.6f, y - 0.6f) - 0.25f, -(x - 0.45f) - (y - 0.45f)),
            Max(Length(x - 0.6f, y + 0.6f) - 0.25f, -(x - 0.45f) + (y + 0.45f))));

        /// <summary>Return: a curved arrow back.</summary>
        public static Sprite ReturnArrow => Get("b_return", IconSize, (x, y) => Min(
            Mathf.Abs(Length(x, y) - 0.55f) - 0.12f + Step(x < 0f && y < 0f),
            Max(Max(-(x + 0.3f), y + 0.2f), (x - 0.1f) - (y + 0.75f))));

        /// <summary>Bloom Burst: a burst of petals.</summary>
        public static Sprite Burst => Get("b_burst", IconSize, (x, y) => Min(StarShape(x, y, 0.95f), Length(x, y) - 0.3f));

        /// <summary>A pointing hand for tutorials (a palm and one raised finger).</summary>
        public static Sprite Pointer => Get("pointer", IconSize, (x, y) => Min(
            RoundedBox(x, y, 0f, -0.35f, 0.42f, 0.42f, 0.2f),
            RoundedBox(x, y, -0.12f, 0.3f, 0.13f, 0.5f, 0.12f)));

        /// <summary>The icon for a variant's <see cref="VariantInfo.IconId"/>.</summary>
        public static Sprite Icon(string iconId) => iconId switch
        {
            "leaf" => Get("icon_leaf", IconSize, Leaf),
            "moss" => Get("icon_moss", IconSize, (x, y) => Min(Length(x + 0.42f, y + 0.25f) - 0.36f, Length(x - 0.42f, y + 0.25f) - 0.36f, Length(x, y + 0.2f) - 0.42f, Length(x, y - 0.35f) - 0.3f)),
            "flower" => Get("icon_flower", IconSize, Flower),
            "bud" => Get("icon_bud", IconSize, (x, y) => Min(Length(x, y + 0.2f) - 0.5f, Triangle(x, y, 0.5f))),
            "drop" => Get("icon_wave", IconSize, (x, y) => Min(Wave(x, y - 0.3f), Wave(x, y + 0.3f))),
            "dew" => Get("icon_dew", IconSize, (x, y) => Max(Min(Length(x, y + 0.3f) - 0.45f, Triangle(x, y + 0.1f, 0.45f)), -(Length(x + 0.15f, y + 0.35f) - 0.12f))),
            "log" => Get("icon_log", IconSize, (x, y) => Max(RoundedBox(x, y, 0f, 0f, 0.85f, 0.42f, 0.4f), -(Mathf.Abs(Length(x - 0.55f, y) - 0.2f) - 0.05f))),
            "acorn" => Get("icon_acorn", IconSize, (x, y) => Min(Length(x, y + 0.2f) - 0.5f, RoundedBox(x, y, 0f, 0.35f, 0.6f, 0.2f, 0.18f), RoundedBox(x, y, 0f, 0.62f, 0.06f, 0.14f, 0.03f))),
            "vine" => Get("icon_vine", IconSize, (x, y) => Mathf.Abs(Length(x, y) - 0.55f) - 0.12f + Step(x > 0.2f && y > 0f)),
            "berry" => Get("icon_berry", IconSize, (x, y) => Min(Length(x + 0.3f, y + 0.2f) - 0.35f, Length(x - 0.3f, y + 0.2f) - 0.35f, Length(x, y - 0.3f) - 0.35f)),
            "mist" => Get("icon_mist", IconSize, (x, y) => Min(RoundedBox(x, y, 0f, 0.45f, 0.8f, 0.1f, 0.1f), RoundedBox(x, y, 0.1f, 0f, 0.7f, 0.1f, 0.1f), RoundedBox(x, y, -0.1f, -0.45f, 0.7f, 0.1f, 0.1f))),
            "bark" => Get("icon_bark", IconSize, (x, y) => Max(RoundedBox(x, y, 0f, 0f, 0.6f, 0.85f, 0.15f), -Min(RoundedBox(x, y, -0.25f, 0f, 0.05f, 0.6f, 0.03f), RoundedBox(x, y, 0.2f, 0.1f, 0.05f, 0.5f, 0.03f)))),
            _ => Question,
        };

        /// <summary>
        /// Placeholder cosmetic art (T143) by <c>CosmeticCatalog</c> shape: hats sit above the worker, expressions on its
        /// face, trails behind it; frames, badges and markers decorate the profile.
        /// </summary>
        public static Sprite Accessory(string shape) => shape switch
        {
            "sprout" => Get("acc_sprout", IconSize, (x, y) => Min(RoundedBox(x, y, 0f, -0.45f, 0.06f, 0.4f, 0.04f), Leaf((x + 0.3f) * 2f, (y - 0.1f) * 2f) / 2f, Leaf((-x + 0.3f) * 2f, (y - 0.1f) * 2f) / 2f)),
            "cap" => Get("acc_cap", IconSize, (x, y) => Min(Max(Length(x, y + 0.3f) - 0.7f, -(y + 0.3f)), RoundedBox(x, y, 0.15f, -0.38f, 0.85f, 0.08f, 0.06f))),
            "brim" => Get("acc_brim", IconSize, (x, y) => Min(Max(Length(x, y + 0.25f) - 0.48f, -(y + 0.25f)), RoundedBox(x, y, 0f, -0.3f, 0.95f, 0.08f, 0.07f))),
            "crown" => Get("acc_crown", IconSize, (x, y) => Min(RoundedBox(x, y, 0f, -0.35f, 0.75f, 0.18f, 0.06f), Length(x + 0.52f, y - 0.05f) - 0.17f, Length(x, y - 0.22f) - 0.2f, Length(x - 0.52f, y - 0.05f) - 0.17f)),
            "nightcap" => Get("acc_nightcap", IconSize, (x, y) => Min(RoundedBox(x, y, 0f, -0.5f, 0.7f, 0.12f, 0.08f), Max(-(y + 0.4f), (Mathf.Abs(x + (0.35f * (y + 0.4f))) * 1.3f) + (y * 0.55f) - 0.45f), Length(x - 0.45f, y - 0.62f) - 0.16f)),
            "sparkle" => Get("acc_sparkle", IconSize, (x, y) => Min(StarShape(x * 1.4f, y * 1.4f, 0.9f) / 1.4f, StarShape((x - 0.55f) * 3f, (y + 0.5f) * 3f, 0.9f) / 3f)),
            "swirl" => Get("acc_swirl", IconSize, (x, y) => Mathf.Abs(Length(x, y) - 0.55f) - 0.1f + Step(x < 0f && y < 0f)),
            "wink" => Get("acc_wink", IconSize, (x, y) => Min(RoundedBox(x, y, -0.4f, 0.15f, 0.22f, 0.06f, 0.05f), Length(x - 0.4f, y - 0.15f) - 0.14f, Max(Mathf.Abs(Length(x, y + 0.05f) - 0.4f) - 0.06f, y + 0.25f))),
            "smile" => Get("acc_smile", IconSize, (x, y) => Min(Length(x + 0.4f, y - 0.2f) - 0.12f, Length(x - 0.4f, y - 0.2f) - 0.12f, Max(Mathf.Abs(Length(x, y + 0.05f) - 0.45f) - 0.07f, y + 0.2f))),
            "stars" => Get("acc_stars", IconSize, (x, y) => Min(StarShape((x + 0.42f) * 2.4f, (y - 0.1f) * 2.4f, 0.9f) / 2.4f, StarShape((x - 0.42f) * 2.4f, (y - 0.1f) * 2.4f, 0.9f) / 2.4f)),
            "sleepy" => Get("acc_sleepy", IconSize, (x, y) => Min(
                Max(Mathf.Abs(Length(x + 0.4f, y - 0.3f) - 0.2f) - 0.05f, y - 0.3f),
                Max(Mathf.Abs(Length(x - 0.4f, y - 0.3f) - 0.2f) - 0.05f, y - 0.3f),
                Length(x, y + 0.3f) - 0.1f)),
            "spots" or "stripes" or "petals" or "speckles" => Get("acc_" + shape, IconSize, (x, y) => Max(Length(x, y) - 0.9f, SkinPatternSdf(shape, x, y))),
            "frame" => Ring,
            "badge" => Star,
            "marker" => DoubleStar,
            _ => Circle,
        };

        /// <summary>The body silhouette of a Bloomling family (placeholder worker art, doc 12 §2).</summary>
        public static Sprite Silhouette(Family family) => Get("fam_" + family, IconSize, (x, y) => SilhouetteSdf(family, x, y));

        /// <summary>
        /// A skin pattern (spots, stripes, petals, speckles) cut to a family's body, laid over the variant-colored
        /// silhouette at <c>CosmeticCatalog.SkinOpacity</c> so the variant color still shows between the marks (FR-063).
        /// </summary>
        public static Sprite SkinPattern(Family family, string shape) =>
            Get("skin_" + family + "_" + shape, IconSize, (x, y) => Max(SilhouetteSdf(family, x, y) + 0.04f, SkinPatternSdf(shape, x, y)));

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
                    return Length(Repeat(x, 0.5f), Repeat(y + (0.25f * Mathf.Floor((x / 0.5f) + 0.5f)), 0.5f)) - 0.12f;
                case "stripes":
                    return Mathf.Abs(Repeat(y + (0.35f * x), 0.34f)) - 0.07f;
                case "petals":
                    float px = Repeat(x, 0.6f);
                    float py = Repeat(y, 0.6f);
                    return Flower(px * 4.5f, py * 4.5f) / 4.5f;
                default:
                    return Length(Repeat(x + (0.13f * Mathf.Floor((y / 0.28f) + 0.5f)), 0.28f), Repeat(y, 0.28f)) - 0.05f;
            }
        }

        /// <summary>The offset of a coordinate from its nearest multiple of a period (in −period/2..period/2).</summary>
        private static float Repeat(float v, float period) => v - (period * Mathf.Floor((v / period) + 0.5f));

        private static float StarShape(float x, float y, float r)
        {
            float angle = Mathf.Atan2(y, x) - (Mathf.PI / 2f);
            float radius = r * (0.62f + (0.38f * Mathf.Cos(5f * angle)));
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
                float a = (Mathf.PI / 2f) + (i * 2f * Mathf.PI / 5f);
                d = Mathf.Min(d, Length(x - (0.45f * Mathf.Cos(a)), y - (0.45f * Mathf.Sin(a))) - 0.35f);
            }

            return Max(d, -(Length(x, y) - 0.18f));
        }

        private static float Wave(float x, float y) => Mathf.Abs(y - (0.12f * Mathf.Sin(x * 5f))) - 0.12f + Step(Mathf.Abs(x) > 0.85f);

        /// <summary>An upward-pointing triangle on top of a circle of radius r at the origin (a droplet tip).</summary>
        private static float Triangle(float x, float y, float r) => Max(y - (r * 1.9f), Mathf.Abs(x) * 1.6f + y - (r * 1.9f), -y);

        private static float RoundedBox(float x, float y, float cx, float cy, float hx, float hy, float radius)
        {
            float qx = Mathf.Abs(x - cx) - hx + radius;
            float qy = Mathf.Abs(y - cy) - hy + radius;
            return Length(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static float Length(float x, float y) => Mathf.Sqrt((x * x) + (y * y));

        private static float Min(params float[] values)
        {
            float m = float.MaxValue;
            foreach (float v in values)
            {
                m = Mathf.Min(m, v);
            }

            return m;
        }

        private static float Max(params float[] values)
        {
            float m = float.MinValue;
            foreach (float v in values)
            {
                m = Mathf.Max(m, v);
            }

            return m;
        }

        /// <summary>Pushes a region outside a shape (used to cut shapes).</summary>
        private static float Step(bool outside) => outside ? 10f : 0f;

        private static Sprite Get(string key, int size, Func<float, float, float> sdf, float border = 0f)
        {
            if (Cache.TryGetValue(key, out Sprite? cached))
            {
                return cached;
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            float pixel = 2f / size;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) * pixel) - 1f;
                    float y = ((py + 0.5f) * pixel) - 1f;
                    float d = sdf(x * 1.08f, y * 1.08f);
                    float alpha = Mathf.Clamp01(0.5f - (d / pixel));
                    pixels[(py * size) + px] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
