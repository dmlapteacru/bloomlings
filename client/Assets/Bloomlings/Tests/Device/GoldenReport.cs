using System;
using System.Collections.Generic;
using System.Text;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Tests.Device
{
    /// <summary>The outcome of one golden replay on a device.</summary>
    public sealed record GoldenResult(string Name, bool Passed, string StateHash, string EventsDigest, string Status, string? Error);

    /// <summary>
    /// The device determinism report (SC-005, SC-011, T151): one line per case and a corpus digest, the SHA-256 of every
    /// case's name, state hash, events digest and status. Two devices agree exactly when their corpus digests are equal,
    /// and a device agrees with <c>dotnet test</c> when every case passes. Engine-free.
    /// </summary>
    public static class GoldenReport
    {
        public const string ResultMarker = "[GoldenReplay] RESULT";

        public static string CorpusDigest(IReadOnlyList<GoldenResult> results)
        {
            var sorted = new List<GoldenResult>(results);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            var text = new StringBuilder();
            foreach (GoldenResult result in sorted)
            {
                text.Append(result.Name).Append(' ').Append(result.StateHash).Append(' ').Append(result.EventsDigest).Append(' ').Append(result.Status).Append('\n');
            }

            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text.ToString()));
        }

        /// <summary>The single log line CI looks for: pass count, total, corpus digest, platform and scripting backend.</summary>
        public static string SummaryLine(IReadOnlyList<GoldenResult> results, string platform, string backend)
        {
            int passed = 0;
            foreach (GoldenResult result in results)
            {
                passed += result.Passed ? 1 : 0;
            }

            string verdict = passed == results.Count && results.Count > 0 ? "PASS" : "FAIL";
            return $"{ResultMarker} {verdict} {passed}/{results.Count} corpus={CorpusDigest(results)} platform={platform} backend={backend}";
        }

        public static string ToJson(IReadOnlyList<GoldenResult> results, string platform, string backend)
        {
            var cases = new JArray();
            foreach (GoldenResult result in results)
            {
                cases.Add(new JObject
                {
                    ["name"] = result.Name,
                    ["passed"] = result.Passed,
                    ["stateHash"] = result.StateHash,
                    ["eventsDigest"] = result.EventsDigest,
                    ["status"] = result.Status,
                    ["error"] = result.Error,
                });
            }

            return CanonicalJson.Write(new JObject
            {
                ["platform"] = platform,
                ["backend"] = backend,
                ["corpusDigest"] = CorpusDigest(results),
                ["cases"] = cases,
            }, indented: true);
        }
    }
}
