using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Meta.Wardrobe
{
    /// <summary>
    /// What a cosmetic is. Skins, hats, trails and expressions are worn by Bloomlings (one of each kind per family);
    /// frames, badges and markers decorate the profile (one of each is shown).
    /// </summary>
    public enum CosmeticKind
    {
        Skin,
        Hat,
        Trail,
        Expression,
        Frame,
        Badge,
        Marker,
    }

    /// <summary>
    /// One cosmetic (FR-063). <see cref="Shape"/> names the placeholder accessory art; <see cref="Tint"/> is
    /// <c>#RRGGBB</c>. <see cref="Starter"/> items are given when the Wardrobe unlocks. An item with a
    /// <see cref="Price"/> is sold for Petals in the Store once the Wardrobe is open (FR-051, doc 10 §2); the others
    /// come from milestones only. <see cref="MilestoneLevel"/> is set on the generated level badges and markers.
    /// </summary>
    public sealed record CosmeticItem(string Id, CosmeticKind Kind, string Name, string Shape, string Tint, bool Starter, int Price = 0, int? MilestoneLevel = null)
    {
        /// <summary>Worn by Bloomlings on the board, so it must keep tiles, pods and workers readable.</summary>
        public bool IsWorn => CosmeticCatalog.IsWornKind(Kind);

        public bool ForSale => Price > 0;
    }

    /// <summary>
    /// The cosmetic catalog (T143), read from <c>Meta/Wardrobe/Resources/CosmeticCatalog.json</c>. Cosmetics have no
    /// gameplay effect. <see cref="ReadabilityProblems"/> is the FR-063 check: a worn cosmetic is a small accessory
    /// (a hat above the worker, an expression on its face, a trail behind it) or a skin pattern laid thinly over the
    /// body (at most <see cref="SkinOpacity"/>, so the variant color still shows through), always in a neutral tint (HSV
    /// saturation at most <see cref="MaxWornSaturation"/>), so it can never be read as a variant color; the variant tint
    /// and icon of tiles, pods and workers are never changed.
    /// <para>
    /// Milestone rewards never run out (FR-061): once a cadence's listed items are all owned, it grants a generated
    /// level badge (<c>badge.level_N</c>) or, for prestige cadences, a level marker (<c>marker.level_N</c>).
    /// <see cref="TryGet"/> resolves those ids without listing them.
    /// </para>
    /// </summary>
    public sealed class CosmeticCatalog
    {
        public const double MaxWornSaturation = 0.3;

        /// <summary>The opacity of a skin pattern over the variant-colored body.</summary>
        public const float SkinOpacity = 0.45f;

        private const string LevelBadgePrefix = "badge.level_";
        private const string LevelMarkerPrefix = "marker.level_";

        /// <summary>The accessory shapes the placeholder art can draw, by kind.</summary>
        public static readonly IReadOnlyDictionary<CosmeticKind, string[]> Shapes = new Dictionary<CosmeticKind, string[]>
        {
            [CosmeticKind.Skin] = new[] { "spots", "stripes", "petals", "speckles" },
            [CosmeticKind.Hat] = new[] { "sprout", "cap", "brim", "crown", "nightcap" },
            [CosmeticKind.Trail] = new[] { "sparkle", "swirl" },
            [CosmeticKind.Expression] = new[] { "wink", "smile", "stars", "sleepy" },
            [CosmeticKind.Frame] = new[] { "frame" },
            [CosmeticKind.Badge] = new[] { "badge" },
            [CosmeticKind.Marker] = new[] { "marker" },
        };

        private readonly Dictionary<string, CosmeticItem> _byId = new Dictionary<string, CosmeticItem>(StringComparer.Ordinal);

        public CosmeticCatalog(IEnumerable<CosmeticItem> items)
        {
            var list = new List<CosmeticItem>();
            foreach (CosmeticItem item in items)
            {
                if (_byId.ContainsKey(item.Id))
                {
                    throw new ArgumentException($"Duplicate cosmetic '{item.Id}'.", nameof(items));
                }

                _byId.Add(item.Id, item);
                list.Add(item);
            }

            Items = list;
        }

        /// <summary>The worn kinds, in Wardrobe order.</summary>
        public static IReadOnlyList<CosmeticKind> WornKinds { get; } = new[] { CosmeticKind.Skin, CosmeticKind.Hat, CosmeticKind.Trail, CosmeticKind.Expression };

        /// <summary>The profile kinds, in Wardrobe order.</summary>
        public static IReadOnlyList<CosmeticKind> ProfileKinds { get; } = new[] { CosmeticKind.Frame, CosmeticKind.Badge, CosmeticKind.Marker };

        /// <summary>The listed items (generated level badges and markers are not listed).</summary>
        public IReadOnlyList<CosmeticItem> Items { get; }

        public static bool IsWornKind(CosmeticKind kind) =>
            kind == CosmeticKind.Skin || kind == CosmeticKind.Hat || kind == CosmeticKind.Trail || kind == CosmeticKind.Expression;

        /// <summary>A listed item, or a generated level badge or marker.</summary>
        public bool TryGet(string id, out CosmeticItem? item)
        {
            if (_byId.TryGetValue(id, out item))
            {
                return true;
            }

            item = Generated(id);
            return item != null;
        }

        /// <summary>The generated badge of a milestone level (a profile reward that never runs out).</summary>
        public static string LevelBadgeId(int level) => LevelBadgePrefix + level.ToString(CultureInfo.InvariantCulture);

        /// <summary>The generated leaderboard marker of a prestige milestone level.</summary>
        public static string LevelMarkerId(int level) => LevelMarkerPrefix + level.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The generated item behind <c>badge.level_N</c> or <c>marker.level_N</c>, or null. Its tint grows with the
        /// level: neutral, silver from every 250th level, gold from every 1000th.
        /// </summary>
        public static CosmeticItem? Generated(string id)
        {
            bool badge = id.StartsWith(LevelBadgePrefix, StringComparison.Ordinal);
            if (!badge && !id.StartsWith(LevelMarkerPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            string number = id.Substring(badge ? LevelBadgePrefix.Length : LevelMarkerPrefix.Length);
            if (number.Length == 0 || number[0] == '0'
                || !int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int level) || level < 1)
            {
                return null;
            }

            string tint = level % 1000 == 0 ? "#D9C27A" : level % 250 == 0 ? "#C0C4C8" : "#B8C4A8";
            // The display name comes from the localization table (cosmetic.level_badge, cosmetic.level_marker).
            return new CosmeticItem(id, badge ? CosmeticKind.Badge : CosmeticKind.Marker, id, badge ? "badge" : "marker", tint, false, 0, level);
        }

        /// <summary>Reads <c>{"items": [{id, kind, name, shape, tint, starter?, price?}]}</c>.</summary>
        public static CosmeticCatalog Parse(string json)
        {
            JObject root = JsonDoc.ParseObject(json, "cosmetics");
            JsonDoc.AllowOnly(root, string.Empty, "note", "items");
            JArray array = JsonDoc.Array(JsonDoc.Required(root, string.Empty, "items"), "items");
            var items = new List<CosmeticItem>();
            for (int i = 0; i < array.Count; i++)
            {
                string path = JsonDoc.Index("items", i);
                JObject item = JsonDoc.Object(array[i], path);
                JsonDoc.AllowOnly(item, path, "id", "kind", "name", "shape", "tint", "starter", "price");
                JToken? starter = JsonDoc.Optional(item, "starter");
                JToken? price = JsonDoc.Optional(item, "price");
                items.Add(new CosmeticItem(
                    JsonDoc.String(JsonDoc.Required(item, path, "id"), JsonDoc.Join(path, "id")),
                    ParseKind(JsonDoc.String(JsonDoc.Required(item, path, "kind"), JsonDoc.Join(path, "kind")), JsonDoc.Join(path, "kind")),
                    JsonDoc.String(JsonDoc.Required(item, path, "name"), JsonDoc.Join(path, "name")),
                    JsonDoc.String(JsonDoc.Required(item, path, "shape"), JsonDoc.Join(path, "shape")),
                    JsonDoc.String(JsonDoc.Required(item, path, "tint"), JsonDoc.Join(path, "tint")),
                    starter != null && JsonDoc.Bool(starter, JsonDoc.Join(path, "starter")),
                    price == null ? 0 : JsonDoc.Int(price, JsonDoc.Join(path, "price"), min: 1)));
            }

            return new CosmeticCatalog(items);
        }

        /// <summary>The FR-063 readability check and catalog rules; empty when every item passes.</summary>
        public IReadOnlyList<string> ReadabilityProblems()
        {
            var problems = new List<string>();
            foreach (CosmeticItem item in Items)
            {
                string prefix = KindName(item.Kind) + ".";
                if (!item.Id.StartsWith(prefix, StringComparison.Ordinal))
                {
                    problems.Add($"{item.Id}: the id must start with '{prefix}'");
                }

                if (Generated(item.Id) != null)
                {
                    problems.Add($"{item.Id}: level badge and marker ids are generated, not listed");
                }

                if (item.Starter && item.ForSale)
                {
                    problems.Add($"{item.Id}: a starter item is given, not sold");
                }

                if (Array.IndexOf(Shapes[item.Kind], item.Shape) < 0)
                {
                    problems.Add($"{item.Id}: shape '{item.Shape}' is not a {KindName(item.Kind)} shape");
                }

                if (!TryParseHex(item.Tint, out double r, out double g, out double b))
                {
                    problems.Add($"{item.Id}: tint '{item.Tint}' is not #RRGGBB");
                    continue;
                }

                double saturation = Saturation(r, g, b);
                if (item.IsWorn && saturation > MaxWornSaturation)
                {
                    problems.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}: worn tint {1} has saturation {2:0.00} > {3:0.00}, so it could read as a variant color (FR-063)",
                        item.Id,
                        item.Tint,
                        saturation,
                        MaxWornSaturation));
                }
            }

            return problems;
        }

        public static string KindName(CosmeticKind kind) => kind switch
        {
            CosmeticKind.Skin => "skin",
            CosmeticKind.Hat => "hat",
            CosmeticKind.Trail => "trail",
            CosmeticKind.Expression => "expression",
            CosmeticKind.Frame => "frame",
            CosmeticKind.Badge => "badge",
            _ => "marker",
        };

        /// <summary>HSV saturation of an RGB color with channels in 0–1.</summary>
        public static double Saturation(double r, double g, double b)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            return max <= 0 ? 0 : (max - min) / max;
        }

        public static bool TryParseHex(string hex, out double r, out double g, out double b)
        {
            r = g = b = 0;
            if (hex.Length != 7 || hex[0] != '#'
                || !int.TryParse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            {
                return false;
            }

            r = ((rgb >> 16) & 0xFF) / 255.0;
            g = ((rgb >> 8) & 0xFF) / 255.0;
            b = (rgb & 0xFF) / 255.0;
            return true;
        }

        /// <summary>The kind named by an id's prefix (<c>hat.leaf_cap</c> → Hat), or null.</summary>
        public static CosmeticKind? KindOfId(string id)
        {
            int dot = id.IndexOf('.');
            return dot <= 0 ? null : KindOfName(id.Substring(0, dot));
        }

        public static CosmeticKind? KindOfName(string name) => name switch
        {
            "skin" => CosmeticKind.Skin,
            "hat" => CosmeticKind.Hat,
            "trail" => CosmeticKind.Trail,
            "expression" => CosmeticKind.Expression,
            "frame" => CosmeticKind.Frame,
            "badge" => CosmeticKind.Badge,
            "marker" => CosmeticKind.Marker,
            _ => null,
        };

        private static CosmeticKind ParseKind(string text, string path) => KindOfName(text)
            ?? throw new ContentFormatException(path, $"'{text}' is not a cosmetic kind");
    }
}
