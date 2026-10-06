using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// Player-facing text from the Unity client's English table (<c>Strings_en.csv</c>, embedded), so demos and messages
    /// read the same in both clients. A missing key shows the key.
    /// </summary>
    public static class PlaytestText
    {
        private static Dictionary<string, string>? _table;

        public static string T(string key) => Table().TryGetValue(key, out string? value) ? value : key;

        public static bool Has(string key) => Table().ContainsKey(key);

        public static string F(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(key), args);

        private static Dictionary<string, string> Table()
        {
            if (_table != null)
            {
                return _table;
            }

            using Stream? stream = typeof(PlaytestText).Assembly.GetManifestResourceStream("strings/Strings_en.csv");
            using var reader = new StreamReader(stream ?? new MemoryStream());
            _table = Parse(reader.ReadToEnd());
            return _table;
        }

        /// <summary>Reads <c>key,text</c> rows; text may be quoted (with <c>""</c> for a quote) to hold commas.</summary>
        private static Dictionary<string, string> Parse(string csv)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in csv.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                int comma = line.IndexOf(',');
                if (comma <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, comma);
                string value = line.Substring(comma + 1);
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    var text = new StringBuilder();
                    for (int i = 1; i < value.Length - 1; i++)
                    {
                        text.Append(value[i]);
                        if (value[i] == '"' && i + 1 < value.Length - 1 && value[i + 1] == '"')
                        {
                            i++;
                        }
                    }

                    value = text.ToString();
                }

                if (key != "key")
                {
                    table[key] = value;
                }
            }

            return table;
        }
    }
}
