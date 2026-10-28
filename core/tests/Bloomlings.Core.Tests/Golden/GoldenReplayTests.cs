using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Golden;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Golden
{
    /// <summary>
    /// Replays every <c>core/tests/golden/*.golden.json</c> and fails when the event digest, the state hash or the
    /// status differs (SC-005). With <c>BLOOMLINGS_GOLDEN_REGEN=1</c> it rewrites the expected values and the review
    /// logs instead; use that only for an intended, reviewed rules change.
    /// </summary>
    public class GoldenReplayTests
    {
        private static bool Regenerate => Environment.GetEnvironmentVariable("BLOOMLINGS_GOLDEN_REGEN") == "1";

        public static string GoldenFolder
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
                {
                    dir = dir.Parent;
                }

                if (dir == null)
                {
                    throw new DirectoryNotFoundException("Repository root (core/Bloomlings.sln) not found above the test binaries.");
                }

                return Path.Combine(dir.FullName, "core", "tests", "golden");
            }
        }

        private static string[] Cases() => Directory.GetFiles(GoldenFolder, "*.golden.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray()!;

        [Test]
        public void GoldenCasesExist()
        {
            Assert.That(Cases(), Is.Not.Empty);
        }

        // Round level cases (spec 006) live in golden/rounds/, so the legacy readers of golden/*.golden.json (the Unity
        // EditMode tests, the playtest check) never meet a format they do not know.
        private static string[] RoundCases() => Directory.Exists(Rounds.RoundGolden.Folder)
            ? Directory.GetFiles(Rounds.RoundGolden.Folder, "rounds-*.golden.json").Select(f => Path.GetFileName(f)!).OrderBy(name => name, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();

        [Test]
        public void RoundCasesExist()
        {
            Assert.That(RoundCases(), Is.Not.Empty);
        }

        [TestCaseSource(nameof(RoundCases))]
        public void ReplayRound(string fileName)
        {
            string path = Path.Combine(Rounds.RoundGolden.Folder, fileName);
            string text = File.ReadAllText(path);
            Rounds.RoundGolden golden = Rounds.RoundGolden.Read(text);
            Rounds.RoundGolden.Outcome outcome = golden.Run();

            if (Regenerate)
            {
                File.WriteAllText(path, golden.WithOutcome(outcome));
                File.WriteAllText(path.Replace(".golden.json", ".events.txt"), outcome.Log);
                Assert.Pass("Regenerated " + fileName);
            }

            Assert.Multiple(() =>
            {
                Assert.That(golden.Write(), Is.EqualTo(text), "Golden files are stored in canonical form.");
                Assert.That(outcome.Status, Is.EqualTo(golden.ExpectedStatus), "status");
                Assert.That(outcome.EndUs, Is.EqualTo(golden.ExpectedEndUs), "end time");
                Assert.That(outcome.State, Is.EqualTo(golden.ExpectedState), "state signature");
                Assert.That(outcome.Events, Is.EqualTo(golden.ExpectedEvents), "events digest");
            });
        }

        [TestCaseSource(nameof(Cases))]
        public void Replay(string fileName)
        {
            string path = Path.Combine(GoldenFolder, fileName);
            string text = File.ReadAllText(path);
            GoldenCase golden = GoldenCase.Read(text);
            GoldenOutcome outcome = GoldenRunner.Run(golden);

            if (Regenerate)
            {
                File.WriteAllText(path, GoldenRunner.WithOutcome(golden, outcome).Write());
                File.WriteAllText(Path.ChangeExtension(path, null).Replace(".golden", string.Empty) + ".events.txt", string.Join("\n", outcome.Log) + "\n");
                Assert.Pass("Regenerated " + fileName);
            }

            Assert.Multiple(() =>
            {
                Assert.That(golden.Write(), Is.EqualTo(text), "Golden files are stored in canonical form.");
                Assert.That(outcome.Status, Is.EqualTo(golden.ExpectedStatus), "status");
                Assert.That(outcome.StateHash, Is.EqualTo(golden.ExpectedStateHash), "state hash");
                Assert.That(outcome.EventsDigest, Is.EqualTo(golden.ExpectedEventsDigest), "events digest");
            });
        }
    }
}
