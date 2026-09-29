using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Readability
{
    /// <summary>One variant pair of the readability report.</summary>
    public sealed record PairReport(
        VariantId A,
        VariantId B,
        bool SameFamily,
        IReadOnlyDictionary<Vision, double> DeltaE,
        double MinDeltaE,
        double GrayscaleDeltaL,
        bool DistinctIcons,
        bool Candidate);

    /// <summary>
    /// The readability check (FR-005, T075). For every variant pair it computes CIEDE2000 under normal vision and the
    /// three simulated color vision deficiencies, and the lightness difference in grayscale (flagged for the human
    /// grayscale/icon test when small). Pairs with distinct icons and enough color distance become
    /// candidates in <c>content/readability/pairs-report.json</c>; a person then records the approved pairs in
    /// <c>approved-pairs.json</c> after the in-game readability tests (grayscale/icon, small size, pod, slot, moving
    /// character). Distinct icons are required for every pair, since hue alone never carries meaning (FR-072).
    /// </summary>
    public static class ReadabilityCommand
    {
        /// <summary>Minimum CIEDE2000 under every simulated vision to become a candidate.</summary>
        public const double MinDeltaE = 10.0;

        /// <summary>
        /// Lightness difference (L*) in grayscale below which a pair is flagged for the human grayscale/icon test. It is
        /// advisory: in grayscale the icon carries the identity (FR-005).
        /// </summary>
        public const double MinGrayscaleDeltaL = 8.0;

        public static IReadOnlyList<PairReport> Analyze(VariantCatalog catalog)
        {
            var reports = new List<PairReport>();
            IReadOnlyList<VariantInfo> all = catalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    reports.Add(Pair(all[i], all[j]));
                }
            }

            return reports;
        }

        public static PairReport Pair(VariantInfo a, VariantInfo b)
        {
            var deltas = new SortedDictionary<Vision, double>();
            double min = double.MaxValue;
            foreach (Vision vision in new[] { Vision.Normal, Vision.Protanopia, Vision.Deuteranopia, Vision.Tritanopia })
            {
                double d = ColorScience.DeltaE2000(
                    ColorScience.ToLab(ColorScience.Simulate(ColorScience.ParseHex(a.ColorHex), vision)),
                    ColorScience.ToLab(ColorScience.Simulate(ColorScience.ParseHex(b.ColorHex), vision)));
                deltas[vision] = Math.Round(d, 2);
                min = Math.Min(min, d);
            }

            double grayA = ColorScience.ToLab(Gray(ColorScience.ParseHex(a.ColorHex))).L;
            double grayB = ColorScience.ToLab(Gray(ColorScience.ParseHex(b.ColorHex))).L;
            double grayDelta = Math.Round(Math.Abs(grayA - grayB), 2);
            bool icons = !string.Equals(a.IconId, b.IconId, StringComparison.Ordinal);
            return new PairReport(
                a.Id,
                b.Id,
                a.Family == b.Family,
                deltas,
                Math.Round(min, 2),
                grayDelta,
                icons,
                icons && min >= MinDeltaE);
        }

        public static string WriteReport(IReadOnlyList<PairReport> pairs)
        {
            var array = new JArray();
            foreach (PairReport pair in pairs)
            {
                var delta = new JObject();
                foreach (KeyValuePair<Vision, double> d in pair.DeltaE)
                {
                    delta[d.Key.ToString().ToLowerInvariant()] = new JValue((decimal)d.Value);
                }

                array.Add(new JObject
                {
                    ["a"] = pair.A.Key,
                    ["b"] = pair.B.Key,
                    ["sameFamily"] = pair.SameFamily,
                    ["deltaE2000"] = delta,
                    ["minDeltaE"] = new JValue((decimal)pair.MinDeltaE),
                    ["grayscaleDeltaL"] = new JValue((decimal)pair.GrayscaleDeltaL),
                    ["distinctIcons"] = pair.DistinctIcons,
                    ["candidate"] = pair.Candidate,
                    ["grayscaleFlag"] = pair.GrayscaleDeltaL < MinGrayscaleDeltaL,
                });
            }

            return CanonicalJson.Write(new JObject
            {
                ["thresholds"] = new JObject
                {
                    ["minDeltaE2000"] = new JValue((decimal)MinDeltaE),
                    ["minGrayscaleDeltaL"] = new JValue((decimal)MinGrayscaleDeltaL),
                },
                ["pairs"] = array,
            }, indented: true);
        }

        private static (double R, double G, double B) Gray((double R, double G, double B) srgb)
        {
            double y = ColorScience.Luminance(srgb);
            double v = y <= 0.0031308 ? y * 12.92 : (1.055 * Math.Pow(y, 1 / 2.4)) - 0.055;
            return (v, v, v);
        }
    }

    /// <summary>
    /// <c>content/readability/approved-pairs.json</c>: the variant pairs that may appear together (FR-005).
    /// <c>status</c> is <c>provisional</c> until a person signs off (then <c>approved</c>, with <c>reviewer</c>); the
    /// catalog validator reports provisional pairs as warnings and unlisted pairs as errors.
    /// </summary>
    public sealed record ApprovedPairs(string Status, string? Reviewer, IReadOnlyCollection<string> Pairs)
    {
        public static string Key(VariantId a, VariantId b) =>
            string.CompareOrdinal(a.Key, b.Key) <= 0 ? a.Key + "+" + b.Key : b.Key + "+" + a.Key;

        public bool IsApproved(VariantId a, VariantId b) => a == b || Contains(Key(a, b));

        public bool IsProvisional => !string.Equals(Status, "approved", StringComparison.Ordinal);

        public static ApprovedPairs Read(string path)
        {
            JObject root = JsonDoc.ParseObject(File.ReadAllText(path), path);
            JsonDoc.AllowOnly(root, string.Empty, "status", "reviewer", "pairs", "note");
            string status = JsonDoc.String(JsonDoc.Required(root, string.Empty, "status"), "status");
            JToken? reviewer = JsonDoc.Optional(root, "reviewer");
            JArray array = JsonDoc.Array(JsonDoc.Required(root, string.Empty, "pairs"), "pairs");
            var pairs = new SortedSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                JArray pair = JsonDoc.Array(array[i], JsonDoc.Index("pairs", i), 2, 2);
                pairs.Add(Key(new VariantId(JsonDoc.String(pair[0], "pairs")), new VariantId(JsonDoc.String(pair[1], "pairs"))));
            }

            return new ApprovedPairs(status, reviewer == null ? null : JsonDoc.String(reviewer, "reviewer"), pairs);
        }

        public static string Write(string status, string? reviewer, IEnumerable<PairReport> approved, string note)
        {
            var pairs = new JArray();
            foreach (PairReport pair in approved)
            {
                pairs.Add(new JArray(pair.A.Key, pair.B.Key));
            }

            var root = new JObject { ["status"] = status, ["pairs"] = pairs, ["note"] = note };
            if (reviewer != null)
            {
                root["reviewer"] = reviewer;
            }

            return CanonicalJson.Write(root, indented: true);
        }

        private bool Contains(string key)
        {
            foreach (string pair in Pairs)
            {
                if (string.Equals(pair, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
