using System;
using System.Collections.Generic;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Gameplay.Themes
{
    /// <summary>A background theme: colors as <c>#RRGGBB</c>.</summary>
    public sealed record BackgroundTheme(string Id, string Name, string Background, string Accent);

    /// <summary>
    /// Background themes by level band (FR-066, T146): daylight garden, pond, orchard and moonlit garden. Before
    /// <see cref="StartLevel"/> (the roadmap's <c>system.theme_rotation</c>, L100) every level uses the first theme; from
    /// there the next theme starts every <see cref="BandLength"/> levels (L100 pond, L150 orchard, L200 moonlit garden,
    /// L250 daylight garden again, ...). Visual only: there are no areas to navigate, and
    /// every theme keeps a light background so tiles and pods stay readable. <see cref="Default"/> mirrors
    /// <c>content/roadmap/themes.json</c>. Engine-free.
    /// </summary>
    public sealed class ThemeRotation
    {
        public ThemeRotation(int startLevel, int bandLength, IReadOnlyList<BackgroundTheme> themes)
        {
            if (startLevel < 1 || bandLength < 1 || themes.Count == 0)
            {
                throw new ArgumentException("Theme rotation needs a start level, a band length and at least one theme.");
            }

            StartLevel = startLevel;
            BandLength = bandLength;
            Themes = themes;
        }

        public int StartLevel { get; }

        public int BandLength { get; }

        public IReadOnlyList<BackgroundTheme> Themes { get; }

        public static ThemeRotation Default { get; } = new ThemeRotation(100, 50, new[]
        {
            new BackgroundTheme("daylight_garden", "Daylight Garden", "#F5F2E6", "#DDEBCF"),
            new BackgroundTheme("pond", "Pond", "#E8F1F2", "#CFE3E8"),
            new BackgroundTheme("orchard", "Orchard", "#F6EDE2", "#EAD8C3"),
            new BackgroundTheme("moonlit_garden", "Moonlit Garden", "#E3E3F0", "#CACBE3"),
        });

        public BackgroundTheme ThemeFor(int level)
        {
            if (level < StartLevel)
            {
                return Themes[0];
            }

            return Themes[(((level - StartLevel) / BandLength) + 1) % Themes.Count];
        }

        public static ThemeRotation Parse(string json)
        {
            JObject root = JsonDoc.ParseObject(json, "themes");
            JsonDoc.AllowOnly(root, string.Empty, "note", "startLevel", "bandLength", "themes");
            JArray array = JsonDoc.Array(JsonDoc.Required(root, string.Empty, "themes"), "themes", minItems: 1);
            var themes = new List<BackgroundTheme>();
            for (int i = 0; i < array.Count; i++)
            {
                string path = JsonDoc.Index("themes", i);
                JObject item = JsonDoc.Object(array[i], path);
                JsonDoc.AllowOnly(item, path, "id", "name", "background", "accent");
                themes.Add(new BackgroundTheme(
                    JsonDoc.Id(JsonDoc.Required(item, path, "id"), JsonDoc.Join(path, "id")),
                    JsonDoc.String(JsonDoc.Required(item, path, "name"), JsonDoc.Join(path, "name")),
                    Hex(item, path, "background"),
                    Hex(item, path, "accent")));
            }

            return new ThemeRotation(
                JsonDoc.Int(JsonDoc.Required(root, string.Empty, "startLevel"), "startLevel", min: 1),
                JsonDoc.Int(JsonDoc.Required(root, string.Empty, "bandLength"), "bandLength", min: 1),
                themes);
        }

        /// <summary>Relative luminance (0–1) of a <c>#RRGGBB</c> color, for the readability check.</summary>
        public static double Luminance(string hex)
        {
            double Channel(int offset)
            {
                double c = Convert.ToInt32(hex.Substring(offset, 2), 16) / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Channel(1)) + (0.7152 * Channel(3)) + (0.0722 * Channel(5));
        }

        private static string Hex(JObject item, string path, string name)
        {
            string value = JsonDoc.String(JsonDoc.Required(item, path, name), JsonDoc.Join(path, name));
            if (value.Length != 7 || value[0] != '#' || !IsHex(value.Substring(1)))
            {
                throw new ContentFormatException(JsonDoc.Join(path, name), $"'{value}' is not a #RRGGBB color");
            }

            return value.ToUpperInvariant();
        }

        private static bool IsHex(string text)
        {
            foreach (char c in text)
            {
                if (!Uri.IsHexDigit(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
