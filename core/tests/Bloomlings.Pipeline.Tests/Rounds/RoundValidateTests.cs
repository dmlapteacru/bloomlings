using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Pipeline.Commands;
using Bloomlings.Pipeline.Rounds;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests.Rounds
{
    /// <summary><c>rounds validate</c> and <c>rounds import</c> (T037, T038) on a tiny hand-made round set.</summary>
    [TestFixture]
    public class RoundValidateTests
    {
        private static string[] Errors(RoundValidation validation, string? check = null) =>
            validation.Errors.Where(i => check == null || i.Check == check).Select(i => i.ToString()).ToArray();

        [Test]
        public void AConsistentSet_Passes()
        {
            using var content = new RoundTestContent();

            RoundValidation validation = content.Validate();

            Assert.That(Errors(validation), Is.Empty);
            Assert.That(validation.Issues.Where(i => !i.IsError), Is.Empty);
        }

        [Test]
        public void Unwired_DefinitionRulesAndConformance_FailEveryLevel()
        {
            using var content = new RoundTestContent();

            RoundValidation validation = content.Validate(new RoundValidationOptions
            {
                DefinitionRules = new UnwiredRoundDefinitionRules(),
                Conformance = new UnwiredRoundConformance(),
            });

            Assert.That(Errors(validation, "conformance"), Has.Length.EqualTo(2).And.All.Contains("conformance not wired"));
            Assert.That(Errors(validation, "definition"), Has.Length.EqualTo(2).And.All.Contains("RoundLevelValidator (T008)"));
        }

        [Test]
        public void TheSeamsErrors_AreReportedPerLevel()
        {
            using var content = new RoundTestContent();
            var options = new RoundValidationOptions
            {
                DefinitionRules = new RoundTestContent.NoDefinitionErrors(),
                Conformance = new Differs(),
            };

            string[] errors = Errors(content.Validate(options), "conformance");

            Assert.That(errors, Is.EqualTo(new[]
            {
                "error L0001 conformance: L0001 solution window 1 (tap pod 1 at 3300000 µs): events differ",
                "error L0002 conformance: L0002 solution window 1 (tap pod 1 at 3300000 µs): events differ",
            }));
        }

        [Test]
        public void ASchemaError_IsNamedWithTheFile()
        {
            using var content = new RoundTestContent();
            content.Edit("levels/L0002.json", j => j["pods"]![1]!["count"] = 61);

            string[] errors = Errors(content.Validate(), "schema");

            Assert.That(errors, Is.Not.Empty);
            Assert.That(errors, Has.All.StartWith("error L0002 schema: levels/L0002.json"));
        }

        [Test]
        public void AGapInTheLevels_IsAnError()
        {
            using var content = new RoundTestContent(3);
            foreach (string sub in new[] { "levels", "proofs", "conformance" })
            {
                File.Delete(content.PathOf($"{sub}/L0002.json"));
            }

            Assert.That(Errors(content.Validate(), "sequence"), Is.EqualTo(new[] { "error set sequence: levels must be 1..3 without gaps; missing L0002" }));
        }

        [Test]
        public void ABoardThatIsNotItsPicture_IsAnError()
        {
            using var content = new RoundTestContent();
            content.Edit("levels/L0001.json", j =>
            {
                j["board"]!["rowsBottomFirst"]![3] = "#.PG..#";
                j["board"]!["rowsBottomFirst"]![4] = "#.GPP.#";
            });

            string[] errors = Errors(content.Validate(), "picture-board");

            Assert.That(errors, Is.EqualTo(new[] { "error L0001 picture-board: row 3 (bottom first) is '#.PG..#', picture P9001 v1 makes '#.PP..#'" }));
        }

        [Test]
        public void AMissingOrOtherPictureVersion_IsAnError()
        {
            using var content = new RoundTestContent();
            content.Edit("levels/L0001.json", j => j["picture"]!["version"] = 2);
            content.Edit("levels/L0002.json", j => j["picture"]!["id"] = "P9002");

            string[] errors = Errors(content.Validate(), "picture");

            Assert.That(errors, Is.EqualTo(new[]
            {
                "error L0001 picture: uses P9001 v2, but pictures/P9001.json is v1",
                "error L0002 picture: picture P9002 v1 is missing",
            }));
        }

        [Test]
        public void AMissingProofOrFixture_IsAnError()
        {
            using var content = new RoundTestContent();
            File.Delete(content.PathOf("proofs/L0001.json"));
            File.Delete(content.PathOf("conformance/L0002.json"));

            RoundValidation validation = content.Validate();

            Assert.That(Errors(validation, "proof"), Is.EqualTo(new[] { "error L0001 proof: the proof is missing" }));
            Assert.That(Errors(validation, "fixture"), Is.EqualTo(new[] { "error L0002 fixture: the conformance fixture is missing" }));
        }

        [Test]
        public void ProofCompleteness_ChecksTheClassChecks()
        {
            using var content = new RoundTestContent();
            content.Edit("proofs/L0001.json", j => ((JObject)j["checks"]!).Remove("forced"));
            content.Edit("proofs/L0002.json", j => ((JObject)j["checks"]!).Remove("pressure"));

            RoundValidation validation = content.Validate();

            Assert.That(Errors(validation, "proof"), Is.EqualTo(new[] { "error L0001 proof: the class check 'forced' is missing" }));
            Assert.That(validation.Issues.Where(i => !i.IsError).Select(i => i.ToString()), Is.EqualTo(new[] { "warning L0002 proof: the class check 'pressure' is missing" }));
        }

        [Test]
        public void ProofCompleteness_ChecksTheCommandsAgainstTheLevel()
        {
            using var content = new RoundTestContent();
            content.Edit("proofs/L0001.json", j =>
            {
                ((JArray)j["solution"]!["commands"]!).Add(new JObject { ["type"] = "tap", ["pod"] = 2, ["atUs"] = 1000 });
                ((JArray)j["solution"]!["commands"]!).Add(new JObject { ["type"] = "bridge", ["choice"] = 0, ["atUs"] = 7_000_000 });
            });

            string[] errors = Errors(content.Validate(), "proof");

            Assert.That(errors, Is.EqualTo(new[]
            {
                "error L0001 proof: solution command 2: tap pod 2 at 1000 µs names pod 2 of 2",
                "error L0001 proof: solution command 2: tap pod 2 at 1000 µs is earlier than the command before it (3300000 µs)",
                "error L0001 proof: solution command 3: bridge 0 at 7000000 µs names a bridge site the level does not have",
                "error L0001 proof: the solution ends at 6600000 µs, before its last command (7000000 µs)",
            }));
        }

        [Test]
        public void ProofCompleteness_ChecksTheRandomTest()
        {
            using var content = new RoundTestContent();
            content.Edit("proofs/L0001.json", j =>
            {
                j["randomTest"]!["wins"] = 65;
                j["randomTest"]!["share"] = 0.6;
            });

            string[] errors = Errors(content.Validate(), "proof");

            Assert.That(errors, Has.Some.EqualTo("error L0001 proof: random test: share 0.6 is not 65/100"));
            Assert.That(errors, Has.Some.StartWith("error L0001 proof: random test: wilson95 [0.963, 1] is not the interval of 65/100 ([0.553, 0.736])"));
        }

        [Test]
        public void ARandomTestOfTheRealLevelFour_IsConsistent()
        {
            // L4 of the first wave: 231 of 400, share 0.578, Wilson [0.529, 0.625], Normal (content/rounds/levels/L0004.json).
            using var content = new RoundTestContent(1);
            content.Edit("proofs/L0001.json", j =>
            {
                j["randomTest"]!["runs"] = 400;
                j["randomTest"]!["wins"] = 231;
                j["randomTest"]!["share"] = 0.578;
                j["randomTest"]!["wilson95"] = new JArray(0.529, 0.625);
            });
            content.Save("conformance/L0001.json", RoundTestContent.Fixture(1, content.Read("levels/L0001.json"), content.Read("proofs/L0001.json")));

            Assert.That(Errors(content.Validate()), Is.Empty);
        }

        [Test]
        public void AClassThatIsNotTheRandomTests_NeedsAnExceptionForThatRule()
        {
            using var content = new RoundTestContent();
            content.Edit("levels/L0001.json", j => j["class"] = "Hard");
            content.Edit("levels/L0002.json", j =>
            {
                j["class"] = "Hard";
                j["exception"] = new JObject { ["rule"] = RoundSetValidator.ClassRule, ["reason"] = "test", ["decidedBy"] = "owner", ["decidedAt"] = "2026-10-27" };
            });
            content.Edit("proofs/L0002.json", j => j["audit"]!["result"] = "OK with exception");

            string[] errors = Errors(content.Validate(), "proof");

            Assert.That(errors, Is.EqualTo(new[] { "error L0001 proof: class matches the random test: the level is Hard, its random test Normal" }));
        }

        [Test]
        public void AnExceptionForAnotherRule_DoesNotExcuseTheClass()
        {
            using var content = new RoundTestContent(1);
            content.Edit("levels/L0001.json", j =>
            {
                j["class"] = "Hard";
                j["exception"] = new JObject { ["rule"] = "one deciding bridge from the second introduction", ["reason"] = "owner kept both winning bridges", ["decidedBy"] = "owner", ["decidedAt"] = "2026-10-27" };
            });
            content.Edit("proofs/L0001.json", j => j["audit"]!["result"] = "OK with exception");

            Assert.That(Errors(content.Validate(), "proof"), Has.Length.EqualTo(1).And.All.Contains(RoundSetValidator.ClassRule));
        }

        [Test]
        public void AFixtureThatIsNotItsProof_IsAnError()
        {
            using var content = new RoundTestContent();
            content.Edit("conformance/L0001.json", j => j["solution"]!["windows"]![1]!["command"]!["atUs"] = 3_300_001);
            content.Edit("conformance/L0002.json", j =>
            {
                j["random"]!["wins"] = 99;
                ((JArray)j["random"]!["games"]!).RemoveAt(0);
            });

            string[] errors = Errors(content.Validate(), "fixture");

            Assert.That(errors, Is.EqualTo(new[]
            {
                "error L0001 fixture: solution window 1 is tap pod 1 at 3300001 µs, the proof's command is tap pod 1 at 3300000 µs",
                "error L0002 fixture: 99 random games recorded for 100 runs",
                "error L0002 fixture: random wins: fixture says 99, its games 99, the proof 100",
            }));
        }

        [Test]
        public void EngineFingerprints_MustBeTheFixtureSets()
        {
            using var content = new RoundTestContent();
            string other = new string('5', 64);
            content.Edit("levels/L0002.json", j => j["engine"]!["timeline"] = other);
            content.Edit("proofs/L0002.json", j => j["engine"]!["timeline"] = other);

            RoundValidation validation = content.Validate();

            Assert.That(Errors(validation, "engine"), Is.EqualTo(new[]
            {
                "error L0002 engine: the level's engine fingerprints differ from the fixture set's (rules 11111111 vs 11111111, timeline 55555555 vs 22222222)",
                "error L0002 engine: the proof's engine fingerprints differ from the fixture set's (rules 11111111 vs 11111111, timeline 55555555 vs 22222222)",
            }));
        }

        [Test]
        public void TheRoundPalette_PassesUnderNormalVision()
        {
            IReadOnlyList<RoundColourPair> pairs = RoundReadability.Analyze();
            JObject report = JObject.Parse(RoundReadability.WriteReport(pairs));

            Assert.Multiple(() =>
            {
                Assert.That(pairs, Has.Count.EqualTo(120), "16 materials give 120 pairs");
                Assert.That(RoundReadability.Gate(pairs), Is.Empty);
                Assert.That(pairs.Min(p => p.Normal), Is.GreaterThanOrEqualTo(10));
                Assert.That(report["passes"]!.Value<bool>(), Is.True);
                Assert.That(report["mergingSimulatedPairs"]!.Value<int>(), Is.EqualTo(11), "research R6: 11 pairs merge for colour-blind players");
                Assert.That(pairs.Single(p => p.A.Code == 'V' && p.B.Code == 'N').DeltaE[Readability.Vision.Protanopia], Is.LessThan(1), "Слива/Глубина 0.7 under protanopia");
            });
        }

        [Test]
        public void Wilson_IsTheReferences()
        {
            (double low, double high) = RoundSetValidator.Wilson(100, 100);
            Assert.That(Math.Round(low, 3), Is.EqualTo(0.963));
            Assert.That(high, Is.EqualTo(1).Within(1e-12));
            Assert.That(RoundSetValidator.ClassOfShare(0.34), Is.EqualTo(RoundClass.Hard));
            Assert.That(RoundSetValidator.ClassOfShare(0.12), Is.EqualTo(RoundClass.SuperHard));
            Assert.That(RoundSetValidator.ClassOfShare(0.04), Is.Null);
        }

        // ------------------------------------------------------------ import

        [Test]
        public void Import_CopiesAConsistentExport()
        {
            using var content = new RoundTestContent(0);
            string export = Path.Combine(content.Root, "export");
            RoundTestContent.Write(export, 2);
            File.WriteAllText(Path.Combine(export, "export.json"), new JObject { ["wave"] = new JArray(1, 2), ["engine"] = JObject.Parse(RoundTestContent.Sample("round-level.json"))["engine"] }.ToString());

            RoundImportResult result = RoundImporter.Import(export, content.Folder, RoundTestContent.Wired);

            Assert.That(result.Issues.Where(i => i.IsError), Is.Empty);
            Assert.That(result.Copied, Has.Count.EqualTo(7));
            Assert.That(File.ReadAllText(content.PathOf("conformance/L0002.json")), Is.EqualTo(File.ReadAllText(Path.Combine(export, "conformance", "L0002.json"))));
            Assert.That(Errors(content.Validate()), Is.Empty);
        }

        [Test]
        public void Import_ANextWave_IsCheckedWithTheLevelsAlreadyIn()
        {
            using var content = new RoundTestContent(2);
            string export = Path.Combine(content.Root, "export");
            RoundTestContent.Write(export, 2, firstLevel: 3);
            File.Delete(Path.Combine(export, "pictures", "P9001.json"));

            RoundImportResult result = RoundImporter.Import(export, content.Folder, RoundTestContent.Wired);

            Assert.That(result.Imported, Is.True, string.Join("\n", result.Issues));
            Assert.That(RoundContent.Load(content.Folder).Levels.Keys, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Import_RefusesAnInconsistentExportAndCopiesNothing()
        {
            using var content = new RoundTestContent(0);
            string export = Path.Combine(content.Root, "export");
            RoundTestContent.Write(export, 2);
            File.Delete(Path.Combine(export, "pictures", "P9001.json"));
            File.Delete(content.PathOf("pictures/P9001.json"));
            File.WriteAllText(Path.Combine(export, "export.json"), new JObject { ["engine"] = new JObject { ["rules"] = new string('9', 64), ["timeline"] = new string('9', 64), ["palette"] = new string('9', 64), ["clock"] = new string('9', 64) } }.ToString());

            RoundImportResult result = RoundImporter.Import(export, content.Folder, RoundTestContent.Wired);

            Assert.That(result.Imported, Is.False);
            Assert.That(result.Copied, Is.Empty);
            Assert.That(result.Issues.Where(i => i.IsError).Select(i => i.Check).Distinct(), Is.EquivalentTo(new[] { "picture", "engine" }));
            Assert.That(Directory.GetFiles(content.PathOf("levels")), Is.Empty);
        }

        [Test]
        public void Cli_Validate_FailsUntilTheReplayIsWired()
        {
            using var content = new RoundTestContent();
            string report = Path.Combine(content.Root, "readability.json");

            int code = Program.Main(new[] { "rounds", "validate", "--rounds", content.Folder, "--readability-report", report });

            Assert.That(code, Is.EqualTo(ExitCodes.ValidationFailed));
            Assert.That(File.Exists(report), Is.True);
        }

        [Test]
        public void Cli_Validate_PassesWithTheSeamsWired()
        {
            using var content = new RoundTestContent();
            IRoundConformance conformance = RoundSeams.Conformance;
            IRoundDefinitionRules rules = RoundSeams.DefinitionRules;
            RoundSeams.Conformance = new RoundTestContent.Conforms();
            RoundSeams.DefinitionRules = new RoundTestContent.NoDefinitionErrors();
            try
            {
                int code = Program.Main(new[] { "rounds", "validate", "--rounds", content.Folder });

                Assert.That(code, Is.EqualTo(ExitCodes.Success));
                Assert.That(File.Exists(content.PathOf("readability-report.json")), Is.True);
            }
            finally
            {
                RoundSeams.Conformance = conformance;
                RoundSeams.DefinitionRules = rules;
            }
        }

        private sealed class Differs : IRoundConformance
        {
            public IReadOnlyList<string> Check(RoundLevel level, RoundProof proof, RoundConformanceFixture fixture) =>
                new[] { $"{RoundContent.Where(level.Number)} solution window 1 ({proof.Solution.Commands[1]}): events differ" };
        }
    }
}
