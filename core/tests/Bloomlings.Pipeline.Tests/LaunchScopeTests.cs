using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Packs;
using Bloomlings.Pipeline.Catalog;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests
{
    [NonParallelizable]
    public class LaunchScopeTests
    {
        private string _folder = string.Empty;
        private static string Repo
        {
            get
            {
                for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                    if (File.Exists(Path.Combine(dir.FullName, "content/launch.props"))) return dir.FullName;
                throw new DirectoryNotFoundException("Repository root");
            }
        }

        [SetUp] public void Setup() => _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "bloomlings-launch-" + Guid.NewGuid().ToString("N"))).FullName;
        [TearDown] public void Cleanup() => Directory.Delete(_folder, true);

        [Test]
        public void LaunchBoundaryIs2500AndCannotBeZeroOrAmbiguous()
        {
            Assert.That(LaunchScope.ReadLimit(Path.Combine(Repo, "content/launch.props")), Is.EqualTo(2500));
            string config = Path.Combine(_folder, "scope.props");
            foreach (string value in new[] { "0", "-1", "abc", "2500</LaunchLevelCount><LaunchLevelCount>5000" })
            {
                File.WriteAllText(config, "<Project><PropertyGroup><LaunchLevelCount>" + value + "</LaunchLevelCount></PropertyGroup></Project>");
                Assert.Throws<InvalidDataException>(() => LaunchScope.ReadLimit(config));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublishIncludesOnlyRequestedLevelsAndReferencedPictures(bool explicitLimit)
        {
            string levels = Directory.CreateDirectory(Path.Combine(_folder, "levels")).FullName;
            string library = Directory.CreateDirectory(Path.Combine(_folder, "pictures")).FullName;
            File.Copy(Path.Combine(Repo, "content/curated/level-0001.json"), Path.Combine(levels, "level-00001.json"));
            // A reserved future definition is deliberately malformed: it must not enter parsing or the release gate.
            File.WriteAllText(Path.Combine(levels, "level-00002.json"), "future draft, not JSON");
            foreach (string name in new[] { "sprout", "tulip" })
                File.Copy(Path.Combine(Repo, "content/pictures/lib/" + name + ".json"), Path.Combine(library, name + ".json"));
            string config = Path.Combine(_folder, "scope.props"), output = Path.Combine(_folder, "published");
            File.WriteAllText(config, "<Project><PropertyGroup><LaunchLevelCount>1</LaunchLevelCount></PropertyGroup></Project>");
            var args = new System.Collections.Generic.List<string> { "publish", "--catalog", _folder, "--lib", library,
                "--pairs", Path.Combine(Repo, "content/readability/approved-pairs.json"), "--content-version", "1", "--out", output };
            args.AddRange(explicitLimit ? new[] { "--max-level", "1" } : new[] { "--launch-config", config });
            Assert.That(Program.Main(args.ToArray()), Is.Zero);
            ContentManifest manifest = ContentManifest.Read(File.ReadAllText(Path.Combine(output, "manifest.json")));
            ContentSet loaded = ContentLoader.FromPacks(manifest, entry => File.ReadAllBytes(Path.Combine(output, entry.Path!)));
            Assert.That(manifest.MaxLevel, Is.EqualTo(1));
            Assert.That(loaded.LevelNumbers, Is.EqualTo(new[] { 1 }));
            Assert.That(loaded.Pictures.Select(p => p.Id), Is.EqualTo(new[] { "sprout" }));
            Assert.That(File.ReadAllText(Path.Combine(levels, "level-00002.json")), Is.EqualTo("future draft, not JSON"));
        }

        [Test]
        public void MissingLastLevelRefusesPublicationInsteadOfSilentlyShippingLess()
        {
            string levels = Directory.CreateDirectory(Path.Combine(_folder, "levels")).FullName;
            File.Copy(Path.Combine(Repo, "content/curated/level-0001.json"), Path.Combine(levels, "level-00001.json"));
            string output = Path.Combine(_folder, "published");
            Assert.That(Program.Main(new[] { "publish", "--catalog", _folder, "--max-level", "2", "--content-version", "1", "--out", output }), Is.EqualTo(2));
            Assert.That(File.Exists(Path.Combine(output, "manifest.json")), Is.False);
        }
    }
}
