using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests
{
    [NonParallelizable]
    public class ScoreBatchTests
    {
        private static string Repo
        {
            get
            {
                for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                    if (File.Exists(Path.Combine(dir.FullName, "content/launch.props"))) return dir.FullName;
                throw new DirectoryNotFoundException("Repository root");
            }
        }

        [Test]
        public void SparseSolvedPilotScoresEachLevelWithoutInventingFullHundreds()
        {
            JObject report = Score(Path.Combine(Repo, "content/pictures/pilot/samples"),
                Path.Combine(Repo, "content/pictures/pilot/lib"));
            Assert.That(((JArray)report["scores"]!).Count, Is.EqualTo(3));
            Assert.That((JArray)report["blocks"]!, Is.Empty);
            foreach (JObject score in (JArray)report["scores"]!)
                Assert.That(score.Value<bool>("agrees"), Is.True);
        }

        [Test]
        public void CompleteRealHundredStillChecksTheDifficultyMix()
        {
            string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "bloomlings-score-" + Guid.NewGuid().ToString("N"))).FullName;
            try
            {
                foreach (string kind in new[] { "levels", "validation" })
                {
                    Directory.CreateDirectory(Path.Combine(folder, kind));
                    for (int n = 11; n <= 110; n++)
                    {
                        string name = $"level-{n:00000}.json";
                        File.Copy(Path.Combine(Repo, "content/catalog", kind, name), Path.Combine(folder, kind, name));
                    }
                }
                JObject report = Score(folder, Path.Combine(Repo, "content/pictures/lib"));
                JArray blocks = (JArray)report["blocks"]!;
                Assert.That(blocks.Count, Is.EqualTo(1));
                Assert.That(blocks[0].Value<int>("from"), Is.EqualTo(11));
                Assert.That(blocks[0].Value<int>("to"), Is.EqualTo(110));
                Assert.That(blocks[0].Value<bool>("ok"), Is.True);
            }
            finally { Directory.Delete(folder, recursive: true); }
        }

        private static JObject Score(string definitions, string library)
        {
            using var output = new StringWriter();
            TextWriter previous = Console.Out;
            int result;
            try
            {
                Console.SetOut(output);
                result = Program.Main(new[] { "score", "--defs", definitions, "--lib", library,
                    "--curated", Path.Combine(Repo, "content/work/no-score-history-" + Guid.NewGuid().ToString("N")),
                    "--profiles", Path.Combine(Repo, "content/profiles"),
                    "--thresholds", Path.Combine(Repo, "content/profiles/difficulty-thresholds.json"), "--json" });
            }
            finally { Console.SetOut(previous); }
            Assert.That(result, Is.Zero, output.ToString());
            return JObject.Parse(output.ToString());
        }
    }
}
