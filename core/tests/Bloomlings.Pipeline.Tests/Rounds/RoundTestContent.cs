using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Pipeline.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Tests.Rounds
{
    /// <summary>
    /// A tiny hand-made round content folder for the pipeline tests: levels L0001…L000n on the 3 × 4 picture P9001 (two
    /// pods, two columns), their proofs (100 random runs, all won) and conformance fixtures consistent with the proofs.
    /// It does not depend on the real export of L1–L20.
    /// </summary>
    internal sealed class RoundTestContent : IDisposable
    {
        public static readonly RoundValidationOptions Wired = new RoundValidationOptions
        {
            DefinitionRules = new NoDefinitionErrors(),
            Conformance = new Conforms(),
        };

        public RoundTestContent(int levels = 2)
        {
            Root = Path.Combine(Path.GetTempPath(), "bloomlings-rounds-" + Guid.NewGuid().ToString("N"));
            Folder = Path.Combine(Root, "rounds");
            Write(Folder, levels);
        }

        public string Root { get; }

        /// <summary>The rounds folder (levels/, pictures/, proofs/, conformance/).</summary>
        public string Folder { get; }

        public static string Sample(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", name));

        /// <summary>Writes a complete, consistent set of <paramref name="levels"/> levels into <paramref name="folder"/>.</summary>
        public static void Write(string folder, int levels, int firstLevel = 1)
        {
            foreach (string sub in new[] { "levels", "pictures", "proofs", "conformance" })
            {
                Directory.CreateDirectory(Path.Combine(folder, sub));
            }

            File.WriteAllText(Path.Combine(folder, "pictures", "P9001.json"), Sample("round-picture.json"));
            for (int n = firstLevel; n < firstLevel + levels; n++)
            {
                JObject level = JObject.Parse(Sample("round-level.json"));
                level["number"] = n;
                File.WriteAllText(Path.Combine(folder, "levels", $"L{n:0000}.json"), level.ToString());
                JObject proof = JObject.Parse(Sample("round-proof.json"));
                proof["number"] = n;
                File.WriteAllText(Path.Combine(folder, "proofs", $"L{n:0000}.json"), proof.ToString());
                File.WriteAllText(Path.Combine(folder, "conformance", $"L{n:0000}.json"), Fixture(n, level, proof).ToString());
            }
        }

        /// <summary>A fixture consistent with the proof: one window per command, the proof's games, all won.</summary>
        public static JObject Fixture(int number, JObject level, JObject proof)
        {
            var windows = new JArray();
            int i = 0;
            foreach (JToken command in (JArray)proof["solution"]!["commands"]!)
            {
                windows.Add(new JObject { ["command"] = command.DeepClone(), ["events"] = "sha256:e" + i, ["signature"] = "sha256:s" + i });
                i++;
            }

            int runs = proof["randomTest"]!["runs"]!.Value<int>();
            int wins = proof["randomTest"]!["wins"]!.Value<int>();
            var games = new JArray();
            for (int g = 0; g < runs; g++)
            {
                games.Add(new JObject { ["seed"] = 7920 + (g * 104729), ["outcome"] = g < wins ? "Won" : "Jammed", ["endUs"] = 60_000_000 + g, ["finalSignature"] = "sha256:g" + g });
            }

            return new JObject
            {
                ["format"] = "round-conformance.v1",
                ["number"] = number,
                ["definitionVersion"] = level["definitionVersion"]!.DeepClone(),
                ["engine"] = level["engine"]!.DeepClone(),
                ["solution"] = new JObject { ["windows"] = windows, ["outcome"] = "Won", ["endUs"] = proof["solution"]!["endUs"]!.DeepClone(), ["finalSignature"] = "sha256:f" },
                ["random"] = new JObject { ["policy"] = "17§5 extended", ["seedBase"] = proof["randomTest"]!["seedBase"]!.DeepClone(), ["runs"] = runs, ["games"] = games, ["wins"] = wins },
                ["owner"] = new JArray(),
            };
        }

        public string PathOf(string relative) => Path.Combine(Folder, relative);

        public JObject Read(string relative) => JObject.Parse(File.ReadAllText(PathOf(relative)));

        public void Save(string relative, JObject json) => File.WriteAllText(PathOf(relative), json.ToString());

        public void Edit(string relative, Action<JObject> edit)
        {
            JObject json = Read(relative);
            edit(json);
            Save(relative, json);
        }

        public RoundValidation Validate(RoundValidationOptions? options = null) =>
            RoundSetValidator.Validate(RoundContent.Load(Folder), options ?? Wired);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        /// <summary>Stands in for T008 in tests: no definition errors.</summary>
        public sealed class NoDefinitionErrors : IRoundDefinitionRules
        {
            public IReadOnlyList<string> Check(RoundLevel level) => Array.Empty<string>();
        }

        /// <summary>Stands in for the replay in tests: every level conforms.</summary>
        public sealed class Conforms : IRoundConformance
        {
            public IReadOnlyList<string> Check(RoundLevel level, RoundProof proof, RoundConformanceFixture fixture) => Array.Empty<string>();
        }
    }
}
