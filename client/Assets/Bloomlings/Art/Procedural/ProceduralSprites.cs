using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
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
        private static readonly Dictionary<string, Sprite> Pictures = new Dictionary<string, Sprite>(StringComparer.Ordinal);

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

        /// <summary>The body silhouette of a Bloomling family (placeholder worker art, doc 12 §2).</summary>
        public static Sprite Silhouette(Family family) => Shape(ShapeLibrary.SilhouetteId(family));

        /// <summary>
        /// A skin pattern (spots, stripes, petals, speckles) cut to a family's body, laid over the variant-colored
        /// silhouette at <c>CosmeticCatalog.SkinOpacity</c> so the variant color still shows between the marks (FR-063).
        /// </summary>
        public static Sprite SkinPattern(Family family, string shape) =>
            Get("skin_" + family + "_" + shape, IconSize, ShapeLibrary.SkinOn(family, shape));

        /// <summary>A skin pattern over its whole box, for a character picture to mask (spec 004).</summary>
        public static Sprite SkinPatternFull(string shape) => Get("skin_full_" + shape, 128, ShapeLibrary.SkinPattern(shape));

        /// <summary>A composite or grown shape under its own cache key (outlines, the decoration's parts).</summary>
        public static Sprite Composite(string key, Func<float, float, float> sdf, int size = IconSize) => Get(key, size, sdf);

        /// <summary>
        /// The leaves-and-flower decoration of the main buttons (spec 003 FR-011a), baked once into one colored sprite per
        /// corner: each part's outline, then its fill, back to front.
        /// </summary>
        public static Sprite Decoration(bool flipped)
        {
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>();
            foreach (DecorationPart part in GardenLook.DecorationParts)
            {
                (Rgba fill, Rgba line) = GardenLook.DecorationColors(part);
                bool leaf = part != DecorationPart.Petals && part != DecorationPart.Center;
                float stroke = leaf ? 0.032f : 0.02f;
                layers.Add((ShapeLibrary.DecorationPartSdf(part, stroke, flipped), line));
                layers.Add((ShapeLibrary.DecorationPartSdf(part, -stroke * 0.6f, flipped), fill));
            }

            return Baked("ui.deco.garden" + (flipped ? "/flipped" : string.Empty), 128, layers);
        }

        // ---- Pictures (spec 005 contracts/look.md §2.1) ----

        /// <summary>
        /// An engine-free RGBA picture as a sprite (spec 005 contracts/look.md §2.1). <paramref name="render"/> gets the
        /// pixel size and returns straight-alpha bytes, rows from the top (<see cref="UiRaster"/>); the rows are flipped
        /// for Unity (bottom-up) and the transparent pixels next to the picture take its color, so bilinear filtering
        /// shows no dark fringe. The texture is RGBA32, bilinear, clamped and <c>DontSave</c>; the sprite has 100 pixels
        /// per unit, a center pivot, a full-rect mesh and <paramref name="border"/> (left, bottom, right, top in pixels)
        /// for 9-slicing. Pictures are cached by <c>key@WxH</c> (and the border), so a key must always render the same
        /// picture at the same size.
        /// </summary>
        public static Sprite Picture(string key, int width, int height, Func<int, int, byte[]> render, Vector4 border = default)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A picture needs at least one pixel.");
            }

            string cacheKey = PicturePixels.CacheKey(key, width, height, border.x, border.y, border.z, border.w);
            if (Pictures.TryGetValue(cacheKey, out Sprite? cached) && cached != null)
            {
                return cached;
            }

            byte[] rgba = render(width, height);
            if (rgba.Length != width * height * 4)
            {
                throw new ArgumentException("Picture " + cacheKey + " rendered " + rgba.Length + " bytes, expected " + (width * height * 4) + ".");
            }

            byte[] rows = PicturePixels.ForTexture(rgba, width, height);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                int k = i * 4;
                pixels[i] = new Color32(rows[k], rows[k + 1], rows[k + 2], rows[k + 3]);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = cacheKey,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = cacheKey;
            sprite.hideFlags = HideFlags.DontSave;
            Pictures[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// A variant's candy tile (spec 005 contracts/look.md §3.1) of side <paramref name="size"/> pixels: the board style
        /// (a small raised bead of the symbol) or the sticker style (pods, slots, the jam row). A null
        /// <paramref name="variant"/> is the lilac mystery tile. The same key as the playtest's <c>Kit.CandyTile</c>.
        /// </summary>
        public static Sprite CandyTile(VariantId? variant, TileStyle style, TileState state = TileState.Normal, int size = 128)
        {
            if (!variant.HasValue || !VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info))
            {
                return CandyTile(DesignTokens.Colors.TileMystery, "mystery", style, TileState.Mystery, size);
            }

            return CandyTile(Rgba.FromHex(info.ColorHex), info.IconId, style, state, size);
        }

        /// <summary>A candy tile of any color and variant icon (the win picture draws its roles' colors this way).</summary>
        public static Sprite CandyTile(Rgba color, string iconId, TileStyle style, TileState state = TileState.Normal, int size = 128)
        {
            string key = "tile.candy/" + (style == TileStyle.Board ? "board" : "sticker") + "/" + state + "/" + iconId + "/" + color.Hex;
            return Picture(key, size, size, (w, h) => UiRaster.Tile(Math.Min(w, h), color, iconId, style, state));
        }

        /// <summary>
        /// A wooden plank (spec 005 §2, <c>mat.wood.light</c> or <c>mat.wood.dark</c>) of
        /// <paramref name="width"/> × <paramref name="height"/> pixels, 9-sliced: its ends (the rounded corners and the
        /// nails) are the side borders and its whole height the top and bottom borders, so drawn sliced at the height it
        /// was rendered for only the grain between the ends stretches. Its corner radius is <paramref name="radiusShare"/>
        /// of the height, its outline <paramref name="outlineShare"/> (at least 2 px), its deeper bottom band
        /// <paramref name="lipShare"/>. The same key as the playtest's <c>Kit.WoodPlank</c>.
        /// </summary>
        public static Sprite Plank(WoodTone tone, int width, int height, float radiusShare = 0.28f, float outlineShare = 0.025f, int seed = 7, float lipShare = UiRaster.PlankLip)
        {
            string key = "mat.wood." + (tone == WoodTone.Light ? "light" : "dark") + "/plank/" + Share(radiusShare) + "/" + Share(outlineShare) + "/" + Share(lipShare) + "/" + seed;
            return Picture(key, width, height, (w, h) => UiRaster.Plank(w, h, h * radiusShare, Math.Max(2f, h * outlineShare), tone, seed, lipShare), PlankBorder(width, height, radiusShare, outlineShare));
        }

        /// <summary>
        /// A pod's wooden frame (spec 005 §2, §3.7; <c>mat.wood.dark</c>): the plank material as a ring around a
        /// transparent hole, corner radius <paramref name="radiusShare"/> and border <paramref name="borderShare"/> of the
        /// width, 9-sliced along its members (the shadow inside the hole's top stays in the top border).
        /// </summary>
        public static Sprite Frame(WoodTone tone, int width, int height, float radiusShare = 0.18f, float borderShare = 0.11f, int seed = 4)
        {
            string shares = radiusShare == 0.18f && borderShare == 0.11f ? string.Empty : Share(radiusShare) + "/" + Share(borderShare) + "/";
            string key = "mat.wood." + (tone == WoodTone.Light ? "light" : "dark") + "/frame/" + shares + seed;
            float r = width * radiusShare;
            float b = width * borderShare;
            float side = (float)Math.Ceiling(Math.Max(r, b) + 2f);
            float top = (float)Math.Ceiling(Math.Max(r, b * 1.8f) + 2f);
            var border = new Vector4(
                Math.Max(0f, Math.Min(side, (width / 2) - 1)),
                Math.Max(0f, Math.Min(side, (height / 2) - 1)),
                Math.Max(0f, Math.Min(side, (width / 2) - 1)),
                Math.Max(0f, Math.Min(top, (height / 2) - 1)));
            return Picture(key, width, height, (w, h) => UiRaster.Frame(w, h, w * radiusShare, w * borderShare, tone, seed), border);
        }

        /// <summary>A stone block (spec 005 §2, <c>mat.stone</c>) with corners rounded by <paramref name="radiusShare"/> of its shorter side; the playtest's key.</summary>
        public static Sprite Stone(int width, int height, int seed, float radiusShare = 0.3f) =>
            Picture("mat.stone/block/" + Share(radiusShare) + "/" + seed, width, height, (w, h) =>
            {
                float side = Math.Min(w, h);
                return UiRaster.Stone(w, h, side * radiusShare, Math.Max(1f, side * 0.05f), seed);
            });

        /// <summary>The stone pedestal of the heroes (spec 005 §3.6, <c>ui.pedestal</c>).</summary>
        public static Sprite Pedestal(int width, int height, int seed = 5) =>
            Picture("ui.pedestal/" + seed, width, height, (w, h) => UiRaster.Pedestal(w, h, seed));

        /// <summary>
        /// A Garden Entry's stone arch (spec 005 §3.6, <c>board.arch</c>), its crown <paramref name="turns"/> quarter turns
        /// clockwise from up (<see cref="UiRaster.Arch"/>).
        /// </summary>
        public static Sprite Arch(int width, int height, int turns, int seed = 5) =>
            Picture("board.arch/" + (((turns % 4) + 4) % 4) + (seed == 5 ? string.Empty : "/" + seed), width, height, (w, h) => UiRaster.Arch(w, h, turns, seed));

        /// <summary>The stone arch of a Garden Entry on <paramref name="side"/>, its crown toward the board (spec 005 §3.6).</summary>
        public static Sprite Arch(int width, int height, EntrySide side) => Arch(width, height, side switch
        {
            EntrySide.Left => 1,
            EntrySide.Top => 2,
            EntrySide.Right => 3,
            _ => 0,
        });

        /// <summary>The win's light rays (spec 005 §3.9, <c>fx.rays</c>) as a square picture of side <paramref name="size"/>.</summary>
        public static Sprite LightRays(int size) =>
            Picture("fx.rays", size, size, (w, h) => PicturePixels.LightRays(Math.Min(w, h), DesignTokens.Colors.RayLight));

        /// <summary>
        /// A white dashed rounded outline (the empty Waiting Slot, spec 005 §3.7) of <paramref name="width"/> ×
        /// <paramref name="height"/> pixels: see <see cref="PicturePixels.DashedOutline"/>; every length is a share of the
        /// shorter side.
        /// </summary>
        public static Sprite DashedOutline(int width, int height, float insetShare, float radiusShare, float strokeShare, float onShare, float offShare)
        {
            string key = "ui.dashed/" + Share(insetShare) + "/" + Share(radiusShare) + "/" + Share(strokeShare) + "/" + Share(onShare) + "/" + Share(offShare);
            return Picture(key, width, height, (w, h) =>
            {
                float s = Math.Min(w, h);
                return PicturePixels.DashedOutline(w, h, s * insetShare, s * radiusShare, Math.Max(1.5f, s * strokeShare), s * onShare, s * offShare);
            });
        }

        /// <summary>
        /// The white rounded-rectangle sprite of the Unity kit's shapes (<c>UiKit.RoundRect</c>): a square of
        /// <see cref="PicturePixels.RoundSize"/> pixels whose 9-slice border is half its side, so drawn sliced with a
        /// pixels-per-unit multiplier of <c>RoundUnit / radius</c> it fills any rect with corners of that radius, up to a
        /// pill. <see cref="RoundFill.Ring"/> and <see cref="RoundFill.Fade"/> draw a band along the edge;
        /// <paramref name="bandShare"/> is the band over the corner radius (quantized to 1/64 of the border), and the larger
        /// of the two spans the border.
        /// </summary>
        public static Sprite Round(RoundFill fill = RoundFill.Solid, float bandShare = 0f)
        {
            const int size = PicturePixels.RoundSize;
            const int unit = PicturePixels.RoundUnit;
            int radius = unit;
            int band = 0;
            if (fill != RoundFill.Solid)
            {
                float q = Math.Max(1f / unit, bandShare);
                if (q <= 1f)
                {
                    band = Math.Max(1, (int)Math.Round(unit * q));
                }
                else
                {
                    band = unit;
                    radius = Math.Max(0, (int)Math.Round(unit / q));
                }
            }

            string key = "ui.round/" + fill + "/" + radius + "/" + band;
            var border = new Vector4(unit, unit, unit, unit);
            return Picture(key, size, size, (w, h) => PicturePixels.RoundRect(w, h, radius, fill, band), border);
        }

        /// <summary>
        /// A colored sprite baked from shapes (<see cref="ShapeLibrary"/> distance functions, the unit square fitted into the
        /// picture as <see cref="ShapeRaster"/> does), each layer painted over the ones before in its color with that color's
        /// alpha: outlines under fills, highlights and veins over them (the decoration, ivy, flower clusters, icons).
        /// </summary>
        public static Sprite Baked(string key, int size, IReadOnlyList<(Func<float, float, float> Sdf, Rgba Color)> layers) =>
            Picture(key, size, size, (w, h) =>
            {
                var rgba = new byte[w * h * 4];
                foreach ((Func<float, float, float> sdf, Rgba color) in layers)
                {
                    PaintOver(rgba, ShapeRaster.Mask(sdf, Math.Min(w, h), topDown: true), color);
                }

                return rgba;
            });

        /// <summary>
        /// A multi-part icon baked into one colored sprite (spec 005 contracts/look.md §3.4, §3.8: the lotus, the booster
        /// icons): each part's outline (the shape grown by its <see cref="IconPart.Grow"/>), then its fill, back to front;
        /// all in grey when <paramref name="grey"/> (a disabled booster).
        /// </summary>
        public static Sprite IconParts(IReadOnlyList<IconPart> parts, bool grey = false, int size = 128)
        {
            var key = new StringBuilder("icon");
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>();
            foreach (IconPart part in parts)
            {
                Func<float, float, float> sdf = ShapeLibrary.Get(part.ShapeId);
                key.Append('/').Append(part.ShapeId).Append(':').Append(part.Fill.Hex);
                if (part.Line.HasValue && part.Grow > 0f)
                {
                    float grow = part.Grow;
                    key.Append(':').Append(part.Line.Value.Hex).Append(':').Append(Share(grow));
                    layers.Add(((x, y) => sdf(x, y) - grow, grey ? part.Line.Value.Grey() : part.Line.Value));
                }

                layers.Add((sdf, grey ? part.Fill.Grey() : part.Fill));
            }

            if (grey)
            {
                key.Append("/grey");
            }

            return Baked(key.ToString(), size, layers);
        }

        /// <summary>
        /// A clover cluster for a wooden sign's end (spec 005 §3.2, §3.9, <c>ui.sign.ivy</c>), as the playtest's
        /// <c>Kit.IvyCluster</c> draws it: per leaf its dark <c>ivy.line</c> outline, the <c>ivy.leaf</c> shade, a light spot
        /// and the midribs; mirrored when <paramref name="flipped"/>. <paramref name="back"/> keeps only the leaves behind
        /// the sign (true) or in front of it (false); null keeps all.
        /// </summary>
        public static Sprite IvyCluster(bool flipped, bool? back = null, int size = 128)
        {
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>();
            for (int i = 0; i < ShapeLibrary.IvyLeafCount; i++)
            {
                bool behind = i % 2 == 1;
                if (back.HasValue && back.Value != behind)
                {
                    continue;
                }

                Func<float, float, float> leaf = ShapeLibrary.IvyLeafSdf(i, 0f, flipped);
                Func<float, float, float> veins = ShapeLibrary.IvyVeinSdf(i, 0.018f, flipped);
                layers.Add((ShapeLibrary.IvyLeafSdf(i, 0.055f, flipped), DesignTokens.Colors.IvyLine));
                layers.Add((leaf, GardenLook.IvyShade(i)));
                layers.Add(((x, y) => Math.Max(leaf(x + 0.05f, y - 0.06f) + 0.07f, leaf(x, y) + 0.02f), DesignTokens.Colors.IvyLeaf.Lighten(0.45f).WithAlpha(0.45f)));
                layers.Add(((x, y) => Math.Max(veins(x, y), leaf(x, y) + 0.03f), DesignTokens.Colors.IvyLine.WithAlpha(0.5f)));
            }

            string part = back.HasValue ? (back.Value ? "back" : "front") : "all";
            return Baked("ui.sign.ivy/" + (flipped ? "r" : "l") + "/" + part, size, layers);
        }

        /// <summary>
        /// The lush cluster at the win sign's ends (spec 005 §3.2, §3.9): five big leaves in three greens with veins and two
        /// white flowers with yellow middles, turned half way when <paramref name="flipped"/> (the playtest's
        /// <c>Kit.FlowerCluster</c>).
        /// </summary>
        public static Sprite FlowerCluster(bool flipped, int size = 160)
        {
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>();
            Rgba[] greens = { DesignTokens.Colors.GardenLeaf1, DesignTokens.Colors.GardenLeaf3, DesignTokens.Colors.GardenLeaf2 };
            for (int i = 0; i < ShapeLibrary.FlowerClusterLeafCount; i++)
            {
                Func<float, float, float> leaf = ShapeLibrary.ClusterLeafSdf(i, 0f, flipped);
                Func<float, float, float> vein = ShapeLibrary.ClusterVeinSdf(i, 0.022f, flipped);
                layers.Add((ShapeLibrary.ClusterLeafSdf(i, 0.04f, flipped), DesignTokens.Colors.GardenLeafLine));
                layers.Add((leaf, greens[i % greens.Length]));
                layers.Add(((x, y) => Math.Max(leaf(x + 0.05f, y - 0.06f) + 0.08f, leaf(x, y) + 0.03f), DesignTokens.Colors.GardenLeaf2.Lighten(0.35f).WithAlpha(0.4f)));
                layers.Add(((x, y) => Math.Max(vein(x, y), leaf(x, y) + 0.04f), DesignTokens.Colors.IvyLine.WithAlpha(0.55f)));
            }

            for (int i = 0; i < ShapeLibrary.FlowerClusterFlowerCount; i++)
            {
                layers.Add((ShapeLibrary.ClusterFlowerSdf(i, false, 0.035f, flipped), DesignTokens.Colors.GardenFlowerLine));
                layers.Add((ShapeLibrary.ClusterFlowerSdf(i, false, 0f, flipped), DesignTokens.Colors.GardenFlower));
                layers.Add((ShapeLibrary.ClusterFlowerSdf(i, true, 0.025f, flipped), DesignTokens.Colors.GardenFlowerCenterLine));
                layers.Add((ShapeLibrary.ClusterFlowerSdf(i, true, 0f, flipped), DesignTokens.Colors.GardenFlowerCenter));
            }

            return Baked("ui.deco.garden/cluster" + (flipped ? "/flipped" : string.Empty), size, layers);
        }

        /// <summary>The small pink flower over the wooden wordmark's end (spec 005 §4.5): an outlined five-petal bloom with a yellow middle.</summary>
        public static Sprite LogoFlower(int size = 96)
        {
            (Rgba petals, Rgba line, Rgba center) = GardenLook.PinkFlower;
            Func<float, float, float> sdf = ShapeLibrary.Get("fx.petal_burst");

            // The playtest draws the middle as a circle of 0.13 of the flower's box: 0.26 of the half box, in shape units.
            float middle = 0.26f * ShapeRaster.Margin;
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>
            {
                ((x, y) => sdf(x, y) - 0.07f, line),
                (sdf, petals),
                ((x, y) => (float)Math.Sqrt((x * x) + (y * y)) - middle, center),
            };
            return Baked("ui.logo.wood/flower", size, layers);
        }

        /// <summary>
        /// A shape with a thin halo all around it, as one colored sprite (a brown glyph on cream, spec 005 §3.3: the shape
        /// grown by <paramref name="grow"/> shape units in <paramref name="halo"/>, then the shape in <paramref name="fill"/>).
        /// </summary>
        public static Sprite Haloed(string shapeId, Rgba fill, Rgba halo, float grow = 0.06f, int size = IconSize)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)> { ((x, y) => sdf(x, y) - grow, halo), (sdf, fill) };
            return Baked(shapeId + "/haloed/" + fill.Hex + "/" + halo.Hex + "/" + Share(grow), size, layers);
        }

        /// <summary>The 9-slice border of a plank: its ends (corner radius, outline and nails) at the sides, its whole height top and bottom.</summary>
        private static Vector4 PlankBorder(int width, int height, float radiusShare, float outlineShare)
        {
            float r = Math.Min(height * radiusShare, Math.Min(width, height) / 2f);
            float outline = Math.Max(2f, height * outlineShare);
            float end = r + outline;
            if (height > 48)
            {
                // The nails sit max(0.7 r, 0.3 h) in from each end (UiRaster.Plank).
                float nail = Math.Max(1.4f, height * 0.035f);
                end = Math.Max(end, Math.Max(r * 0.7f, height * 0.3f) + nail + 2f);
            }

            float side = Math.Max(0f, Math.Min((float)Math.Ceiling(end + 1f), (width / 2) - 1));
            float vertical = Math.Max(0, (height / 2) - 1);
            return new Vector4(side, vertical, side, vertical);
        }

        /// <summary>Paints a color with its alpha through a mask over straight-alpha RGBA bytes.</summary>
        private static void PaintOver(byte[] rgba, byte[] mask, Rgba color)
        {
            float colorAlpha = color.A / 255f;
            for (int i = 0; i < mask.Length; i++)
            {
                float a = mask[i] / 255f * colorAlpha;
                if (a <= 0f)
                {
                    continue;
                }

                int k = i * 4;
                float ua = rgba[k + 3] / 255f;
                float outA = a + (ua * (1f - a));
                rgba[k] = Blend(color.R, rgba[k], a, ua, outA);
                rgba[k + 1] = Blend(color.G, rgba[k + 1], a, ua, outA);
                rgba[k + 2] = Blend(color.B, rgba[k + 2], a, ua, outA);
                rgba[k + 3] = (byte)Math.Min(255, (int)Math.Round(outA * 255f));
            }
        }

        private static byte Blend(byte top, byte bottom, float a, float ua, float outA) =>
            (byte)Math.Max(0, Math.Min(255, (int)Math.Round(((top * a) + (bottom * ua * (1f - a))) / Math.Max(0.0001f, outA))));

        /// <summary>A share as a short cache-key text (0.28 → "0.28").</summary>
        private static string Share(float share) => share.ToString("0.###", CultureInfo.InvariantCulture);

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
