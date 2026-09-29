using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Golden;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// Runs every <c>core/tests/golden/*.golden.json</c> through the Unity-compiled <c>com.bloomlings.core</c> and
    /// asserts the same digests, hashes and statuses as <c>dotnet test</c> (SC-005, T053).
    /// </summary>
    public class GoldenReplayEditModeTests
    {
        private static string GoldenFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "core", "tests", "golden"));

        private static string[] Cases() => Directory.Exists(GoldenFolder)
            ? Directory.GetFiles(GoldenFolder, "*.golden.json").Select(f => Path.GetFileName(f)!).OrderBy(n => n, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();

        [Test]
        public void GoldenCasesAreFound()
        {
            Assert.That(Cases(), Is.Not.Empty, "Expected golden cases in " + GoldenFolder);
        }

        [TestCaseSource(nameof(Cases))]
        public void Replay(string fileName)
        {
            GoldenCase golden = GoldenCase.Read(File.ReadAllText(Path.Combine(GoldenFolder, fileName)));

            GoldenOutcome outcome = GoldenRunner.Run(golden);

            Assert.That(outcome.Status, Is.EqualTo(golden.ExpectedStatus), "status");
            Assert.That(outcome.StateHash, Is.EqualTo(golden.ExpectedStateHash), "state hash");
            Assert.That(outcome.EventsDigest, Is.EqualTo(golden.ExpectedEventsDigest), "events digest");
        }
    }
}
