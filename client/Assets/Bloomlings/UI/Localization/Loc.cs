using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Bloomlings.Client.UI.Localization
{
    /// <summary>
    /// Player-facing strings (research R18, T148). Every string shown to the player is looked up by key: first in the
    /// Unity Localization string table <see cref="Table"/> for the selected locale (<see cref="Lookup"/>, set by the
    /// Localization integration when the package is installed), then in the English table
    /// <c>UI/Localization/Resources/Strings_en.csv</c>, which is also the source the Unity table is built from
    /// (Tools/Bloomlings/Localization/Import English Strings). A missing key shows the key itself, so gaps are visible.
    /// </summary>
    public static class Loc
    {
        public const string Table = "UI";
        public const string EnglishResource = "Strings_en";

        private static Dictionary<string, string>? _english;

        /// <summary>The selected locale's text for a key, or null when the table has no entry.</summary>
        public static Func<string, string?>? Lookup { get; set; }

        /// <summary>The English text of every key.</summary>
        public static IReadOnlyDictionary<string, string> English => _english ??= LoadBundledEnglish();

        public static string T(string key) => Lookup?.Invoke(key) ?? (English.TryGetValue(key, out string? text) ? text : key);

        /// <summary>The text of <paramref name="key"/>, or <paramref name="fallback"/> when no table has it (catalog names).</summary>
        public static string T(string key, string fallback) =>
            Lookup?.Invoke(key) ?? (English.TryGetValue(key, out string? text) ? text : fallback);

        /// <summary>A text with <c>{0}</c>-style arguments; numbers use the invariant culture.</summary>
        public static string F(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(key), args);

        /// <summary>Replaces the English table (tests and tools, or a table loaded from elsewhere).</summary>
        public static void LoadEnglish(string csv) => _english = ParseCsv(csv);

        /// <summary>Reads <c>key,en</c> rows (RFC 4180 quoting; the first row is the header).</summary>
        public static Dictionary<string, string> ParseCsv(string csv)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            List<List<string>> rows = ReadRows(csv);
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                if (row.Count == 0 || (row.Count == 1 && row[0].Length == 0))
                {
                    continue;
                }

                if (row.Count < 2)
                {
                    throw new FormatException($"Strings row {i + 1} has no text.");
                }

                if (table.ContainsKey(row[0]))
                {
                    throw new FormatException($"Strings key '{row[0]}' is defined twice.");
                }

                table.Add(row[0], row[1]);
            }

            return table;
        }

        private static Dictionary<string, string> LoadBundledEnglish()
        {
            TextAsset? asset = Resources.Load<TextAsset>(EnglishResource);
            return asset != null ? ParseCsv(asset.text) : new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static List<List<string>> ReadRows(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    {
                        i++;
                    }

                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
