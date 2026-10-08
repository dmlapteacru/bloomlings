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
            // regular 224–288 cells from L11, and on a big board (the owner, 2026-10-07: every size from L11) the band's
            // pods and work grow with the cells, in proportion to the largest regular board's 288.
            Assert.That(BandGuidelines.Work(5, DifficultyClass.Normal, 144), Is.EqualTo(new IntRange(95, 140)));
            Assert.That(BandGuidelines.Work(20, DifficultyClass.Hard, 256), Is.EqualTo(new IntRange(150, 275)));
            Assert.That(BandGuidelines.Work(60, DifficultyClass.Normal, 288), Is.EqualTo(new IntRange(150, 330)));
            Assert.That(BandGuidelines.Work(60, DifficultyClass.Hard, 288), Is.EqualTo(new IntRange(150, 330)), "one board range for every class");
            Assert.That(BandGuidelines.Work(200, DifficultyClass.Normal, 224), Is.EqualTo(new IntRange(150, 360)));
            Assert.That(BandGuidelines.Work(20, DifficultyClass.Normal, 616), Is.EqualTo(new IntRange(321, 588)), "an early 22×28 board");
            Assert.That(BandGuidelines.Work(550, DifficultyClass.Normal, 616), Is.EqualTo(new IntRange(321, 770)));
            Assert.That(BandGuidelines.For(550, 616).Pods, Is.EqualTo(new IntRange(26, 86)));
            Assert.That(BandGuidelines.For(550, 288).Pods, Is.EqualTo(new IntRange(12, 40)));
            Assert.That(BandGuidelines.For(15, 440).Pods, Is.EqualTo(new IntRange(15, 34)), "a 20×22 board at L15");

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
                        foreach (int cells in new[] { 224, 288, 340, 440, 616 })
                        {
                            Assert.That(BandGuidelines.Intersect(profile.Work, BandGuidelines.Work(level, difficulty, cells)), Is.Not.Null, $"{where} work on {cells} cells");
                            Assert.That(BandGuidelines.Intersect(profile.PodCount, BandGuidelines.For(level, cells).Pods), Is.Not.Null, $"{where} pods on {cells} cells");
                        }
                    }

                    // The profile's board sizes reach the level's board rule: from L11 every size from 14×16 to 22×28.
                    BoardRule board = BandGuidelines.Board(level);
                    Assert.That(profile.BoardWidth.Contains(14) && profile.BoardHeight.Contains(16) && profile.BoardWidth.Contains(22) && profile.BoardHeight.Contains(28), Is.True, $"{profile.BandId}: 14×16 to 22×28");
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

            // As L26 or L550 its board is too small (every size from 14×16 to 22×28 from L11), and as L51 its 3 variants are
            // too few (5).
            Assert.That(Validate(three with { LevelNumber = 26 }).Any(i => i.IsError && i.Check == "band" && i.Message.Contains("board 12×12")), Is.True);
            Assert.That(Validate(three with { LevelNumber = 550 }).Any(i => i.IsError && i.Check == "band" && i.Message.Contains("board 12×12")), Is.True);
            Assert.That(Validate(three with { LevelNumber = 51 }).Any(i => i.IsError && i.Check == "variant-count"), Is.True);
        }

        [Test]
        public void HardAndSuperHardLevels_HaveAtMostThreeStacks()
        {
            // The owner, 2026-10-08: "4 are passed quite fast, even Super Hard". Fewer exposed pods to choose from.
            Assert.That(BandGuidelines.Stacks(new IntRange(2, 6), DifficultyClass.Normal), Is.EqualTo(new IntRange(2, 6)));
            Assert.That(BandGuidelines.Stacks(new IntRange(2, 6), DifficultyClass.Hard), Is.EqualTo(new IntRange(2, 3)));
            Assert.That(BandGuidelines.Stacks(new IntRange(3, 6), DifficultyClass.SuperHard), Is.EqualTo(new IntRange(3, 3)));
            Assert.That(BandGuidelines.Stacks(new IntRange(4, 5), DifficultyClass.Hard), Is.EqualTo(new IntRange(4, 4)), "never under the profile's minimum");

            // Curated L5 is Hard with 3 stacks; with its last stack split in two it has 4, which only a Normal level may have.
            LevelDefinition five = Curated(5);
            Assert.That(five.Difficulty.Class, Is.EqualTo(DifficultyClass.Hard));
            Assert.That(five.Tray.Stacks.Count, Is.EqualTo(3));
            IReadOnlyList<string> last = five.Tray.Stacks[^1];
            Assert.That(last.Count, Is.GreaterThan(1));
            var four = new TrayDef(five.Tray.Stacks.Take(2).Append(last.Take(1).ToList()).Append(last.Skip(1).ToList()).ToList());
            static bool TooMany(IEnumerable<LevelIssue> issues) => issues.Any(i => i.IsError && i.Check == "data-model" && i.Message.Contains("at most 3"));
            Assert.That(TooMany(Validate(five)), Is.False);
            Assert.That(TooMany(Validate(five with { Tray = four })), Is.True);
            Assert.That(TooMany(Validate(five with { Tray = four, Difficulty = five.Difficulty with { Class = DifficultyClass.Normal } })), Is.False);
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
            // as entries 1–3 would repeat its picture and have 3 variants at "L1" in the catalog. It has rules of its own
            // (the owner, 2026-10-07): only the Daily Challenge's pictures, each once, a subject only every 60 entries.
            List<LevelDefinition> pool = Enumerable.Range(1, 3).Select(i => three with { LevelNumber = i }).ToList();
            Assert.That(Validate(pool.ToArray()).Any(i => i.IsError && (i.Check == "similarity" || i.Check == "variant-count")), Is.True, "as catalog levels");
            List<LevelIssue> daily = CatalogCommands.GateIssues(lib, pairs, 20000, curated, pool).Where(i => i.IsError).ToList();
            Assert.That(daily.Select(i => i.Check).Distinct(), Is.EquivalentTo(new[] { "daily-picture-pool", "daily-picture-once", "daily-subject-window" }), string.Join("; ", daily.Select(i => $"L{i.Level} {i.Check}: {i.Message}")));
            Assert.That(daily.Where(i => i.Check == "daily-picture-once").Select(i => i.Level), Is.EqualTo(new[] { 2, 3 }));
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
