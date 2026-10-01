using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.Art
{
    /// <summary>
    /// Placeholder flat art generated at runtime: Unity sprites of the engine-free <see cref="ShapeLibrary"/> shapes
    /// (research R2). It covers tile and panel shapes, one distinct icon per variant (leaf, moss tuft, flower, bud,
    /// wave, dew droplet, log, acorn, …), one silhouette per family, and the UI glyphs of the design board. Every
    /// variant has its own shape, so hue never carries meaning alone (FR-005, FR-072). A shape's id is its asset slot
    /// (<see cref="AssetSlots"/>); final art replaces these through <see cref="Variants.VariantVisualCatalog"/> and the
    /// slot registry.
    /// </summary>
    public static class ProceduralSprites
    {
        private const int IconSize = 96;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>A white rounded square with a 9-slice border.</summary>
        public static Sprite RoundedSquare => Get("ui.panel", 64, border: 22f);

        public static Sprite Circle => Get("ui.circle", 64);

        /// <summary>A circle with a half-size 9-slice border: sliced, it draws a pill at any width (spec 002 pills and buttons).</summary>
        public static Sprite PillSprite => Get("ui.circle", 64, border: 31f);

        /// <summary>A soft ring used for the Garden Entry marker and highlights.</summary>
        public static Sprite Ring => Get("ui.ring", 64);

        public static Sprite Lock => Shape("ui.lock");

        public static Sprite Key => Shape("tile.key");

        public static Sprite Question => Shape("tile.mystery");

        /// <summary>A five-point star (Hard label).</summary>
        public static Sprite Star => Shape("ui.star");

        /// <summary>Two stars (Super Hard label).</summary>
        public static Sprite DoubleStar => Shape("ui.star2");

        /// <summary>A cross, used to show that a pod ignores a tile of another variant (FR-071).</summary>
        public static Sprite Cross => Shape("ui.cross");

        /// <summary>A gear for the Settings button (no font glyph needed).</summary>
        public static Sprite Gear => Shape("ui.settings");

        /// <summary>A garden gate / hedge seal: an arch with bars (FR-037).</summary>
        public static Sprite Gate => Shape("special.gate");

        /// <summary>A Fountain: a basin, a column and a spray (FR-038).</summary>
        public static Sprite Fountain => Shape("special.fountain");

        /// <summary>A sealed garden Chest: a box with a domed lid and a clasp (roadmap L150).</summary>
        public static Sprite Chest => Shape("special.chest");

        /// <summary>A garden Statue on a plinth (the L250 environmental object).</summary>
        public static Sprite Statue => Shape("special.statue");

        /// <summary>A Bridge with missing planks (the L250 environmental object, before it is repaired).</summary>
        public static Sprite BridgeBroken => Shape("special.bridge_broken");

        /// <summary>The repaired Bridge: whole planks across.</summary>
        public static Sprite Bridge => Shape("special.bridge");

        /// <summary>A dashed square: "restore this whole region" (a Statue's condition).</summary>
        public static Sprite Region => Shape("special.region");

        /// <summary>An hourglass: a waiting pod (no reachable tile of its variant yet, FR-019, FR-070).</summary>
        public static Sprite Hourglass => Shape("slot.state.waiting");

        /// <summary>An exclamation mark: jam risk (one usable slot left, FR-070).</summary>
        public static Sprite Exclamation => Shape("slot.state.jam_risk");

        /// <summary>Extra Slot: a slot with a plus.</summary>
        public static Sprite PlusSlot => Shape("booster.extra_slot");

        /// <summary>Shuffle: two crossing arrows.</summary>
        public static Sprite ShuffleArrows => Shape("booster.shuffle");

        /// <summary>Return: a curved arrow back.</summary>
        public static Sprite ReturnArrow => Shape("booster.return");

        /// <summary>Bloom Burst: a burst of petals.</summary>
        public static Sprite Burst => Shape("booster.bloom_burst");

        /// <summary>A pointing hand for tutorials (a palm and one raised finger).</summary>
        public static Sprite Pointer => Shape("ui.pointer");

        /// <summary>The Petal currency symbol (FR-006).</summary>
        public static Sprite Petal => Shape("currency.petal");

        /// <summary>Any <see cref="ShapeLibrary"/> shape by its asset slot id (icon size).</summary>
        public static Sprite Shape(string id) => Get(id, IconSize);

        /// <summary>A shape at a larger mask size, for big placeholders (the Home hero, the reward basket).</summary>
        public static Sprite Shape(string id, int size) => Get(id, size);

        /// <summary>The icon for a variant's <see cref="VariantInfo.IconId"/>.</summary>
        public static Sprite Icon(string iconId) => Shape(ShapeLibrary.SymbolId(iconId));

        /// <summary>
        /// Placeholder cosmetic art (T143) by <c>CosmeticCatalog</c> shape: hats sit above the worker, expressions on its
        /// face, trails behind it; frames, badges and markers decorate the profile.
        /// </summary>
        public static Sprite Accessory(string shape)
        {
            string id = ShapeLibrary.CosmeticId(shape);
            return ShapeLibrary.Has(id) ? Shape(id) : Circle;
        }

        /// <summary>The body silhouette of a Bloomling family (its outer edge, for masks and hit areas; doc 12 §2).</summary>
        public static Sprite Silhouette(Family family) => Shape(ShapeLibrary.SilhouetteId(family));

        /// <summary>
        /// A kawaii Bloomling in full color (spec 003 FR-032), baked once per look and size from the engine-free
        /// <see cref="BloomlingArt"/>, so it matches the playtest pixel for pixel. Draw it with a white image color; the
        /// variant symbol goes on top at <see cref="BloomlingArt.SymbolAnchors"/>.
        /// </summary>
        public static Sprite Bloomling(BloomlingLook look, int size = 128)
        {
            string cacheKey = look.Key + "@" + size;
            if (Cache.TryGetValue(cacheKey, out Sprite? cached))
            {
                return cached;
            }

            byte[] rgba = BloomlingArt.Render(look, size, topDown: false, premultiplied: false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
            }

            Sprite sprite = Bake(cacheKey, size, pixels);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// A skin pattern (spots, stripes, petals, speckles) cut to a family's body, laid over the variant-colored
        /// silhouette at <c>CosmeticCatalog.SkinOpacity</c> so the variant color still shows between the marks (FR-063).
        /// </summary>
        public static Sprite SkinPattern(Family family, string shape) =>
            Get("skin_" + family + "_" + shape, IconSize, ShapeLibrary.SkinOn(family, shape));

        /// <summary>A composite or grown shape under its own cache key (outlines, the decoration's parts).</summary>
        public static Sprite Composite(string key, Func<float, float, float> sdf, int size = IconSize) => Get(key, size, sdf);

        /// <summary>
        /// The leaves-and-flower decoration of the main buttons (spec 003 FR-011a), baked once into one colored sprite per
        /// corner: each part's outline, then its fill, back to front.
        /// </summary>
        public static Sprite Decoration(bool flipped)
        {
            string cacheKey = "ui.deco.garden" + (flipped ? "/flipped" : string.Empty);
            if (Cache.TryGetValue(cacheKey, out Sprite? cached))
            {
                return cached;
            }

            const int size = 128;
            var pixels = new Color32[size * size];
            foreach (DecorationPart part in GardenLook.DecorationParts)
            {
                (Rgba fill, Rgba line) = GardenLook.DecorationColors(part);
                bool leaf = part != DecorationPart.Petals && part != DecorationPart.Center;
                float stroke = leaf ? 0.032f : 0.02f;
                Over(pixels, ShapeRaster.Mask(ShapeLibrary.DecorationPartSdf(part, stroke, flipped), size, topDown: false), line);
                Over(pixels, ShapeRaster.Mask(ShapeLibrary.DecorationPartSdf(part, -stroke * 0.6f, flipped), size, topDown: false), fill);
            }

            Sprite sprite = Bake(cacheKey, size, pixels);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>A sprite of colored pixels (rows from the bottom, straight alpha).</summary>
        private static Sprite Bake(string name, int size, Color32[] pixels)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = name;
            return sprite;
        }

        /// <summary>Paints a color through a mask over the pixels (straight alpha).</summary>
        private static void Over(Color32[] pixels, byte[] mask, Rgba color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                float a = mask[i] / 255f;
                if (a <= 0f)
                {
                    continue;
                }

                Color32 under = pixels[i];
                float ua = under.a / 255f;
                float outA = a + (ua * (1f - a));
                byte Mix(byte top, byte bottom) => (byte)Mathf.Clamp(Mathf.RoundToInt(((top * a) + (bottom * ua * (1f - a))) / Mathf.Max(0.0001f, outA)), 0, 255);
                pixels[i] = new Color32(Mix(color.R, under.r), Mix(color.G, under.g), Mix(color.B, under.b), (byte)Mathf.RoundToInt(outA * 255f));
            }
        }

        private static Sprite Get(string id, int size, float border = 0f) => Get(id, size, ShapeLibrary.Get(id), border);

        private static Sprite Get(string key, int size, Func<float, float, float> sdf, float border = 0f)
        {
            string cacheKey = key + "@" + size + (border > 0f ? "/" + border : string.Empty);
            if (Cache.TryGetValue(cacheKey, out Sprite? cached))
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
            byte[] mask = ShapeRaster.Mask(sdf, size, topDown: false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, mask[i]);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = key;
            Cache[cacheKey] = sprite;
            return sprite;
        }
    }
}
