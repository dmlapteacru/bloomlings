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
