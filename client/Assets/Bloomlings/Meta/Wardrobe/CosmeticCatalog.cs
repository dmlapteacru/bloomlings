using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Meta.Wardrobe
{
    /// <summary>What a cosmetic is. Hats, trails and expressions are worn by Bloomlings; frames, badges and markers decorate the profile.</summary>
    public enum CosmeticKind
    {
        Hat,
        Trail,
        Expression,
        Frame,
        Badge,
        Marker,
    }

    /// <summary>
    /// One cosmetic (FR-063). <see cref="Shape"/> names the placeholder accessory art; <see cref="Tint"/> is
    /// <c>#RRGGBB</c>. <see cref="Starter"/> items are given when the Wardrobe unlocks.
    /// </summary>
    public sealed record CosmeticItem(string Id, CosmeticKind Kind, string Name, string Shape, string Tint, bool Starter)
    {
        /// <summary>Worn by Bloomlings on the board, so it must keep tiles, pods and workers readable.</summary>
        public bool IsWorn => Kind == CosmeticKind.Hat || Kind == CosmeticKind.Trail || Kind == CosmeticKind.Expression;
    }

    /// <summary>
    /// The cosmetic catalog (T143), read from <c>Meta/Wardrobe/Resources/CosmeticCatalog.json</c>. Cosmetics have no
    /// gameplay effect. <see cref="ReadabilityProblems"/> is the FR-063 check: a worn cosmetic is a small accessory
    /// (a hat above the worker, an expression on its face, a trail behind it) in a neutral tint (HSV saturation at most
    /// <see cref="MaxWornSaturation"/>), so it can never be read as a variant color; the variant tint and icon of tiles,
    /// pods and workers are never changed.
    /// </summary>
    public sealed class CosmeticCatalog
    {
        public const double MaxWornSaturation = 0.3;

        /// <summary>The accessory shapes the placeholder art can draw, by kind.</summary>
        public static readonly IReadOnlyDictionary<CosmeticKind, string[]> Shapes = new Dictionary<CosmeticKind, string[]>
        {
            [CosmeticKind.Hat] = new[] { "sprout", "cap", "brim", "crown", "nightcap" },
            [CosmeticKind.Trail] = new[] { "sparkle", "swirl" },
            [CosmeticKind.Expression] = new[] { "wink", "smile", "stars" },
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

        public IReadOnlyList<CosmeticItem> Items { get; }

        public bool TryGet(string id, out CosmeticItem? item) => _byId.TryGetValue(id, out item);

        /// <summary>Reads <c>{"items": [{id, kind, name, shape, tint, starter?}]}</c>.</summary>
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
                JsonDoc.AllowOnly(item, path, "id", "kind", "name", "shape", "tint", "starter");
                JToken? starter = JsonDoc.Optional(item, "starter");
                items.Add(new CosmeticItem(
                    JsonDoc.String(JsonDoc.Required(item, path, "id"), JsonDoc.Join(path, "id")),
                    ParseKind(JsonDoc.String(JsonDoc.Required(item, path, "kind"), JsonDoc.Join(path, "kind")), JsonDoc.Join(path, "kind")),
                    JsonDoc.String(JsonDoc.Required(item, path, "name"), JsonDoc.Join(path, "name")),
                    JsonDoc.String(JsonDoc.Required(item, path, "shape"), JsonDoc.Join(path, "shape")),
                    JsonDoc.String(JsonDoc.Required(item, path, "tint"), JsonDoc.Join(path, "tint")),
                    starter != null && JsonDoc.Bool(starter, JsonDoc.Join(path, "starter"))));
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

        private static CosmeticKind ParseKind(string text, string path) => text switch
        {
            "hat" => CosmeticKind.Hat,
            "trail" => CosmeticKind.Trail,
            "expression" => CosmeticKind.Expression,
            "frame" => CosmeticKind.Frame,
            "badge" => CosmeticKind.Badge,
            "marker" => CosmeticKind.Marker,
            _ => throw new ContentFormatException(path, $"'{text}' is not a cosmetic kind"),
        };
    }
}
