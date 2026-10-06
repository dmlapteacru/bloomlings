using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    /// <summary>FR-083 windows across the curated/catalog boundary, empty mechanic sets, and published content (FR-076).</summary>
    public class CatalogRulesTests
    {
        private static string RepoRoot
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
                {
                    dir = dir.Parent;
                }

                return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
            }
        }

        private static List<BasePicture> Library => ContentStore.LoadLibrary(Path.Combine(RepoRoot, "content", "pictures", "lib"));

        private static List<LevelDefinition> Curated =>
            ContentStore.LoadLevels(Path.Combine(RepoRoot, "content", "curated")).Select(l => l.Level).ToList();

        private static CatalogValidator Validator() => new CatalogValidator(
            Library,
            UnlockRoadmap.Default,
            ContentStore.LoadPairs(Path.Combine(RepoRoot, "content", "readability", "approved-pairs.json")),
            new SolveOptions(20000));

        [Test]
        public void FromL11_ThreeLevelsWithoutMechanics_AreRepetitive_ButTheTutorialIsNot()
        {
            // Curated L1 has no mechanics; three copies with different pictures would still repeat "none".
            List<LevelDefinition> curated = Curated;
            var plain = curated.Where(l => l.LevelNumber <= 7).Take(3).ToList();

            List<LevelIssue> tutorial = Validator().Validate(plain.Select((l, i) => l with { LevelNumber = i + 1 }).ToList()).Issues;
            Assert.That(tutorial.Any(i => i.Message.Contains("same mechanics")), Is.False);

            List<LevelIssue> later = Validator().Validate(plain.Select((l, i) => l with { LevelNumber = i + 11 }).ToList()).Issues;
            Assert.That(later.Any(i => i.Level == 13 && i.Message.Contains("same mechanics (none)")), Is.True);
        }

        [Test]
        public void Conflicts_AreJudgedOnBothSides_ForASeamOfAParallelBuild()
        {
            // Copies of curated levels placed at L200 and around it: a level knows its later neighbours too.
            List<LevelDefinition> curated = Curated;
            LevelDefinition a = curated.Single(l => l.LevelNumber == 3);
            LevelDefinition b = curated.Single(l => l.LevelNumber == 5);
            var history = new SortedDictionary<int, LevelDefinition>
            {
                [200] = a with { LevelNumber = 200 },
                [230] = a with { LevelNumber = 230 },
            };

            // The same picture 30 levels later is caught at the earlier level as well.
            Assert.That(LevelGenerator.Conflicts(200, history), Has.Some.StartsWith("picture:"));
            Assert.That(LevelGenerator.Conflicts(230, history), Has.Some.StartsWith("picture:"));

            // Fifty levels apart is allowed; the source layout repeats only within 50 levels.
            history.Remove(230);
            history[250] = a with { LevelNumber = 250 };
            Assert.That(LevelGenerator.Conflicts(200, history).Any(c => c.StartsWith("picture:") || c.StartsWith("source:")), Is.False);

            // Three in a row with the same variant set and mechanics, the level in the middle.
            var row = new SortedDictionary<int, LevelDefinition>
            {
                [300] = b with { LevelNumber = 300 },
                [301] = a with { LevelNumber = 301, Mapping = b.Mapping, Pods = b.Pods, Tray = b.Tray, Mechanics = b.Mechanics },
                [302] = b with { LevelNumber = 302 },
            };
            IReadOnlyList<string> middle = LevelGenerator.Conflicts(301, row);
            Assert.That(middle, Has.Member("similarity:variant-set-3-in-a-row"));
            Assert.That(middle, Has.Member("similarity:mechanics-3-in-a-row"));
            Assert.That(LevelGenerator.Conflicts(299, row), Is.Empty, "a level not in the history has nothing to break");
        }

        [Test]
        public void TheCuratedLevels_AreTheContextOfTheCatalogWindows()
        {
            // A catalog L11 with the variant set of curated L9 and L10 repeats it 3 times in a row across the boundary.
            List<LevelDefinition> curated = Curated;
            LevelDefinition ten = curated.Single(l => l.LevelNumber == 10);
            var context = new List<LevelDefinition> { ten with { LevelNumber = 9 }, ten };
            var catalog = new List<LevelDefinition> { ten with { LevelNumber = 11 } };

            Assert.That(Validator().Validate(catalog).Issues.Any(i => i.Message.Contains("variant set 3 levels")), Is.False, "without context");
            List<LevelIssue> issues = Validator().Validate(catalog, context: context).Issues;
            Assert.That(issues.Any(i => i.Level == 11 && i.Message.Contains("variant set 3 levels")), Is.True);
            Assert.That(issues.Any(i => i.Level != 11), Is.False, "context levels are not reported");
        }

        [Test]
        public void PublishedContent_IsReadBackWithItsVersionAndPictures()
        {
            string folder = Path.Combine(Path.GetTempPath(), "bloomlings-published-" + Guid.NewGuid().ToString("N"));
            try
            {
                List<LevelDefinition> levels = Curated.OrderBy(l => l.LevelNumber).ToList();
                Dictionary<string, BasePicture> library = Library.ToDictionary(p => p.Id + "@" + p.Version);
                var used = levels.Select(l => library[l.Picture.Id + "@" + l.Picture.Version]).Distinct().ToList();
                ManifestWriter.Publish(new PublishRequest(7, "1.0.0", 7, 50_000, levels, used, new List<DailyPoolEntry>()), folder);

                (List<LevelDefinition> read, int? version) = ContentStore.LoadAny(folder, folder);

                Assert.That(version, Is.EqualTo(7));
                Assert.That(read.Select(l => l.LevelNumber), Is.EqualTo(levels.Select(l => l.LevelNumber)));
                Assert.That(ContentStore.PicturesOf(folder, "unused").Select(p => p.Id), Is.EquivalentTo(used.Select(p => p.Id)));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
    }
}
