using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Core.Variants;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Commands;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    /// <summary>The spec's Level Band Guidelines, in the generator and in the validator (FR-004, FR-060, FR-079).</summary>
    public class BandGuidelinesTests
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

        private static ApprovedPairs Pairs => ContentStore.LoadPairs(Path.Combine(RepoRoot, "content", "readability", "approved-pairs.json"))
            ?? throw new FileNotFoundException("approved-pairs.json");

        private static LevelDefinition Curated(int number) =>
            ContentStore.LoadLevels(Path.Combine(RepoRoot, "content", "curated")).Single(l => l.Level.LevelNumber == number).Level;

        private static List<LevelIssue> Validate(params LevelDefinition[] levels) =>
            new CatalogValidator(Library, UnlockRoadmap.Default, Pairs, new SolveOptions(20000)).Validate(levels).Issues;

        [TestCase(1, DifficultyClass.Normal, 2, 2)]
        [TestCase(10, DifficultyClass.SuperHard, 2, 3)]
        [TestCase(25, DifficultyClass.Normal, 3, 4)]
        [TestCase(28, DifficultyClass.Normal, 4, 4)]
        [TestCase(32, DifficultyClass.Normal, 4, 5)]
        [TestCase(60, DifficultyClass.Normal, 5, 5)]
        [TestCase(60, DifficultyClass.Hard, 5, 5)]
        [TestCase(70, DifficultyClass.Hard, 5, 6)]
        [TestCase(70, DifficultyClass.Normal, 5, 5)]
        [TestCase(150, DifficultyClass.Normal, 5, 5)]
        [TestCase(150, DifficultyClass.SuperHard, 5, 6)]
        [TestCase(300, DifficultyClass.Normal, 5, 6)]
        [TestCase(600, DifficultyClass.Normal, 4, 6)]
        public void VariantCounts_FollowTheBandsAndTheRoadmap(int level, DifficultyClass difficulty, int min, int max)
        {
            Assert.That(BandGuidelines.Variants(level, difficulty), Is.EqualTo(new IntRange(min, max)));
        }

        [Test]
        public void Work_IsByClassWhereTheBandSaysSo_AndLayersDeepenOnTheRoadmap()
        {
            // Work follows the boards (75–95% occupancy plus the layers from L28): the curated 11–12×12 of 2026-10-05, the
            // regular 224–288 cells from L11 and the big 289–616 cells of 2026-10-06.
            Assert.That(BandGuidelines.Work(5, DifficultyClass.Normal), Is.EqualTo(new IntRange(95, 140)));
            Assert.That(BandGuidelines.Work(20, DifficultyClass.Hard), Is.EqualTo(new IntRange(150, 275)));
            Assert.That(BandGuidelines.Work(60, DifficultyClass.Normal), Is.EqualTo(new IntRange(150, 330)));
            Assert.That(BandGuidelines.Work(60, DifficultyClass.Hard), Is.EqualTo(new IntRange(150, 330)), "one board range for every class");
            Assert.That(BandGuidelines.Work(200, DifficultyClass.Normal), Is.EqualTo(new IntRange(150, 360)));
            Assert.That(BandGuidelines.Work(550, DifficultyClass.Normal), Is.EqualTo(new IntRange(200, 650)), "a big level");
            Assert.That(BandGuidelines.Work(551, DifficultyClass.Normal), Is.EqualTo(new IntRange(150, 360)));
            Assert.That(BandGuidelines.For(550).Pods, Is.EqualTo(new IntRange(24, 56)));

            Assert.That(BandGuidelines.MaxLayersBelow(27), Is.Zero);
            Assert.That(BandGuidelines.MaxLayersBelow(28), Is.EqualTo(1));
            Assert.That(BandGuidelines.MaxLayersBelow(124), Is.EqualTo(1));
            Assert.That(BandGuidelines.MaxLayersBelow(125), Is.EqualTo(2));
        }

        [Test]
        public void EveryBandProfile_CanMeetTheGuidelinesOfEachOfItsLevels()
        {
            foreach (string file in Directory.GetFiles(Path.Combine(RepoRoot, "content", "profiles"), "band-*.json"))
            {
                GenerationProfile profile = ProfileLoader.ReadFile(file);
                for (int level = profile.LevelRange.Min; level <= Math.Min(profile.LevelRange.Max, 5000); level++)
                {
                    foreach (DifficultyClass difficulty in new[] { DifficultyClass.Normal, DifficultyClass.Hard, DifficultyClass.SuperHard })
                    {
                        string where = $"{profile.BandId} L{level} {difficulty}";
                        Assert.That(BandGuidelines.Intersect(profile.VariantCount, BandGuidelines.Variants(level, difficulty)), Is.Not.Null, where + " variants");
                        Assert.That(BandGuidelines.Intersect(profile.Work, BandGuidelines.Work(level, difficulty)), Is.Not.Null, where + " work");
                        Assert.That(BandGuidelines.Intersect(profile.PodCount, BandGuidelines.For(level).Pods), Is.Not.Null, where + " pods");
                    }

                    // The profile's board sizes reach the level's board rule (regular, or big on a big level).
                    BoardRule board = BandGuidelines.Board(level);
                    bool reachable = false;
                    for (int w = profile.BoardWidth.Min; w <= profile.BoardWidth.Max && !reachable; w++)
                    {
                        for (int h = profile.BoardHeight.Min; h <= profile.BoardHeight.Max && !reachable; h++)
                        {
                            reachable = board.Allows(w, h);
                        }
                    }

                    Assert.That(reachable, Is.True, $"{profile.BandId} L{level} board {board}");
                }

                // The profile must be able to reach the top of each rule: 6-variant Hard from L70, depth 3 from L125.
                if (profile.LevelRange.Contains(70))
                {
                    Assert.That(profile.VariantCount.Max, Is.GreaterThanOrEqualTo(6), profile.BandId);
                }

                if (profile.LevelRange.Max >= 125)
                {
                    Assert.That(profile.MaxLayerDepth, Is.GreaterThanOrEqualTo(2), profile.BandId);
                }
            }
        }

        [Test]
        public void Pods_NeverFallUnderTheSmallestClass_AndSmallWavesJoinTheirVariant()
        {
            VariantId leaf = VariantId.Leaf;
            VariantId moss = VariantId.Moss;
            var waves = new List<Wave>
            {
                new Wave(leaf, 12, Array.Empty<int>()),
                new Wave(moss, 3, Array.Empty<int>()),
                new Wave(leaf, 2, Array.Empty<int>()),
                new Wave(moss, 2, Array.Empty<int>()),
                new Wave(moss, 9, Array.Empty<int>()),
                new Wave(VariantId.Water, 20, Array.Empty<int>()),
            };
            var rng = new Xoshiro256StarStar(7);

            IReadOnlyList<PlannedPod> pods = PodPartitioner.Partition(waves, new IntRange(4, 9), new IntRange(1, 40), 0, ref rng)
                ?? throw new AssertionException("no partition");

            Assert.That(pods.Select(p => p.Count), Has.All.GreaterThanOrEqualTo(BandGuidelines.MinPodSize));
            Assert.That(pods.Where(p => p.Variant == leaf).Sum(p => p.Count), Is.EqualTo(14), "demand is kept");
            Assert.That(pods.Where(p => p.Variant == moss).Sum(p => p.Count), Is.EqualTo(14));
            Assert.That(pods.Where(p => p.Variant == moss).Min(p => p.Wave), Is.EqualTo(1), "a merged pod belongs to its earliest wave");

            var tiny = new List<Wave> { new Wave(leaf, 20, Array.Empty<int>()), new Wave(VariantId.Dew, 4, Array.Empty<int>()) };
            Assert.That(PodPartitioner.Partition(tiny, new IntRange(2, 6), new IntRange(1, 40), 0, ref rng), Is.Null, "4 Dew tiles cannot make a pod");
        }

        [Test]
        public void TheGenerator_RefusesAProfileThatCannotMeetTheGuidelines()
        {
            // A profile for L51 with 4 variants only: the guidelines ask for 5 there.
            GenerationProfile profile = ProfileLoader.ReadFile(Path.Combine(RepoRoot, "content", "profiles", "band-0051-0100.json")) with
            {
                VariantCount = new IntRange(4, 4),
            };
            var generator = new LevelGenerator(profile, new PicturePicker(Library), DifficultyThresholds.Default, Pairs.IsApproved, new DifficultySchedule(1));

            GenerationResult result = generator.Generate(51, 51, 1, new SortedDictionary<int, LevelDefinition>());

            Assert.That(result.Failed, Is.EqualTo(new[] { 51 }));
            Assert.That(result.Rejections.Select(r => r.Reason), Has.All.StartWith("profile:band-0051-0100-outside-guidelines"));
        }

        [Test]
        public void TheValidator_ChecksBoardPodsAndPodSizeByBand()
        {
            // Curated L3: a 12×12 board (the onboarding size since 2026-10-05) passes as L3.
            LevelDefinition three = Curated(3);
            Assert.That(Validate(three).Where(i => i.IsError), Is.Empty);

            // Its 8-tile trunk pod split into two 4-tile pods: as L3 the small pods only warn (hand-curated tutorial level).
            PodDef trunk = three.Pods.Single(p => p.Id == "t1");
            LevelDefinition small = three with
            {
                Pods = three.Pods.Select(p => p == trunk ? p with { Count = 4 } : p).Append(trunk with { Id = "t2", Count = 4 }).ToList(),
                Tray = new TrayDef(three.Tray.Stacks.Select(s => (IReadOnlyList<string>)s.SelectMany(id => id == "t1" ? new[] { "t1", "t2" } : new[] { id }).ToList()).ToList()),
            };
            List<LevelIssue> asTutorial = Validate(small);
            Assert.That(asTutorial.Where(i => i.IsError), Is.Empty, string.Join("; ", asTutorial.Where(i => i.IsError).Select(i => i.Message)));
            Assert.That(asTutorial.Any(i => !i.IsError && i.Check == "band" && i.Message.Contains("4 tiles")), Is.True);

            // The same level as L11 breaks the early band: pods of 5+, 10–22 pods, and from L11 a board of 224–288 cells
            // (FR-008 as amended on 2026-10-06; 12×12 was an early board before).
            var band = Validate(small with { LevelNumber = 11 }).Where(i => i.IsError && i.Check == "band").Select(i => i.Message).ToList();
            Assert.That(band.Any(m => m.Contains("4 tiles")), Is.True, string.Join("; ", band));
            Assert.That(band.Any(m => m.Contains("6 Source Pods")), Is.True, string.Join("; ", band));
            Assert.That(band.Any(m => m.Contains("board 12×12 (144 cells)")), Is.True, string.Join("; ", band));

            // As L26 its board is too small, as a big level (L550) far too small, and as L51 its 3 variants are too few (5).
            Assert.That(Validate(three with { LevelNumber = 26 }).Any(i => i.IsError && i.Check == "band" && i.Message.Contains("board 12×12")), Is.True);
            Assert.That(Validate(three with { LevelNumber = 550 }).Any(i => i.IsError && i.Check == "band" && i.Message.Contains("a big level")), Is.True);
            Assert.That(Validate(three with { LevelNumber = 51 }).Any(i => i.IsError && i.Check == "variant-count"), Is.True);
        }

        [Test]
        public void ThePublishGate_RefusesAnInvalidCatalog_AndChecksTheDailyPoolByItself()
        {
            string lib = Path.Combine(RepoRoot, "content", "pictures", "lib");
            string pairs = Path.Combine(RepoRoot, "content", "readability", "approved-pairs.json");
            List<LevelDefinition> curated = ContentStore.LoadLevels(Path.Combine(RepoRoot, "content", "curated")).Select(l => l.Level).ToList();

            Assert.That(CatalogCommands.GateIssues(lib, pairs, 20000, curated, Array.Empty<LevelDefinition>()).Where(i => i.IsError), Is.Empty);

            // One Water tile too many in a pod: exact accounting fails, so the gate stops the publish.
            LevelDefinition three = curated[2];
            PodDef first = three.Pods[0];
            var broken = curated.Select(l => l.LevelNumber == 3 ? three with { Pods = three.Pods.Select(p => p == first ? p with { Count = p.Count + 1 } : p).ToList() } : l).ToList();
            Assert.That(CatalogCommands.GateIssues(lib, pairs, 20000, broken, Array.Empty<LevelDefinition>()).Any(i => i.IsError && i.Level == 3 && i.Check == "accounting"), Is.True);

            // A daily pool numbered by pool index is not held to Level N's band rules or FR-083 windows: L3 three times
            // as entries 1–3 would repeat its picture and have 3 variants at "L1" in the catalog.
            List<LevelDefinition> pool = Enumerable.Range(1, 3).Select(i => three with { LevelNumber = i }).ToList();
            Assert.That(Validate(pool.ToArray()).Any(i => i.IsError && (i.Check == "similarity" || i.Check == "variant-count")), Is.True, "as catalog levels");
            List<LevelIssue> daily = CatalogCommands.GateIssues(lib, pairs, 20000, curated, pool);
            Assert.That(daily.Where(i => i.IsError), Is.Empty, string.Join("; ", daily.Where(i => i.IsError).Select(i => $"L{i.Level} {i.Check}: {i.Message}")));
        }

        [Test]
        public void TheValidator_WantsAllFourFamiliesInEveryWindowFromL20()
        {
            // Curated L7 uses two families; five such levels in a row from L20 miss the other two.
            LevelDefinition seven = Curated(7);
            LevelDefinition[] window = Enumerable.Range(20, BandGuidelines.FamilyWindow).Select(n => seven with { LevelNumber = n }).ToArray();

            List<LevelIssue> issues = Validate(window);

            Assert.That(issues.Any(i => i.IsError && i.Check == "families" && i.Level == 24), Is.True);
        }
    }
}
