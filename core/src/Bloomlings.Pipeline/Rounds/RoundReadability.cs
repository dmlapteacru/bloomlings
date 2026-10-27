using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Rounds;
using Bloomlings.Pipeline.Readability;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>One pair of round materials: CIEDE2000 under normal vision and the three simulated deficiencies.</summary>
    public sealed record RoundColourPair(RoundMaterial A, RoundMaterial B, IReadOnlyDictionary<Vision, double> DeltaE)
    {
        public double Normal => DeltaE[Vision.Normal];

        /// <summary>The smallest distance under a simulated colour vision deficiency.</summary>
        public double MinSimulated => Math.Min(DeltaE[Vision.Protanopia], Math.Min(DeltaE[Vision.Deuteranopia], DeltaE[Vision.Tritanopia]));
    }

    /// <summary>
    /// The readability gate of the round palette (spec 006 FR-047, research R6, T038), with the game's own colour math
    /// (<see cref="ColorScience"/>): under normal vision every pair of the 16 normal colours must have CIEDE2000 ≥ 10. The
    /// simulated protanopia, deuteranopia and tritanopia numbers are written to the report only: colour-blind players get
    /// the symbols mode, and levels need not avoid colour pairs.
    /// </summary>
    public static class RoundReadability
    {
        public const double MinDeltaE = ReadabilityCommand.MinDeltaE;

        private static readonly Vision[] Visions = { Vision.Normal, Vision.Protanopia, Vision.Deuteranopia, Vision.Tritanopia };

        public static IReadOnlyList<RoundColourPair> Analyze()
        {
            IReadOnlyList<RoundMaterial> all = RoundMaterial.All;
            var pairs = new List<RoundColourPair>();
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    var deltas = new SortedDictionary<Vision, double>();
                    foreach (Vision vision in Visions)
                    {
                        double d = ColorScience.DeltaE2000(
                            ColorScience.ToLab(ColorScience.Simulate(ColorScience.ParseHex(all[i].ColorHex), vision)),
                            ColorScience.ToLab(ColorScience.Simulate(ColorScience.ParseHex(all[j].ColorHex), vision)));
                        deltas[vision] = Math.Round(d, 2);
                    }

                    pairs.Add(new RoundColourPair(all[i], all[j], deltas));
                }
            }

            return pairs;
        }

        /// <summary>The gate's errors: one per pair below <see cref="MinDeltaE"/> under normal vision.</summary>
        public static IReadOnlyList<RoundIssue> Gate(IReadOnlyList<RoundColourPair> pairs) =>
            pairs.Where(p => p.Normal < MinDeltaE)
                .Select(p => new RoundIssue("palette", "readability", $"{p.A.Code} {p.A.Name} / {p.B.Code} {p.B.Name}: CIEDE2000 {p.Normal} under normal vision, below {MinDeltaE}"))
                .ToList();

        /// <summary>The report: thresholds, the smallest normal distance, and every pair with all four visions.</summary>
        public static string WriteReport(IReadOnlyList<RoundColourPair> pairs)
        {
            var array = new JArray();
            foreach (RoundColourPair pair in pairs)
            {
                var delta = new JObject();
                foreach (KeyValuePair<Vision, double> d in pair.DeltaE)
                {
                    delta[d.Key.ToString().ToLowerInvariant()] = new JValue((decimal)d.Value);
                }

                array.Add(new JObject
                {
                    ["a"] = pair.A.Code.ToString(),
                    ["b"] = pair.B.Code.ToString(),
                    ["names"] = pair.A.Name + " / " + pair.B.Name,
                    ["deltaE2000"] = delta,
                    ["passesNormal"] = pair.Normal >= MinDeltaE,
                    ["mergesSimulated"] = pair.MinSimulated < MinDeltaE,
                });
            }

            RoundColourPair smallest = pairs.OrderBy(p => p.Normal).First();
            return CanonicalJson.Write(new JObject
            {
                ["what"] = "Round palette readability (spec 006 FR-047): the gate is normal vision; simulated deficiencies are reported only (the symbols mode covers them).",
                ["thresholds"] = new JObject { ["minDeltaE2000Normal"] = new JValue((decimal)MinDeltaE) },
                ["smallestNormal"] = new JObject { ["a"] = smallest.A.Code.ToString(), ["b"] = smallest.B.Code.ToString(), ["deltaE2000"] = new JValue((decimal)smallest.Normal) },
                ["passes"] = pairs.All(p => p.Normal >= MinDeltaE),
                ["mergingSimulatedPairs"] = pairs.Count(p => p.MinSimulated < MinDeltaE),
                ["pairs"] = array,
            }, indented: true);
        }
    }
}
