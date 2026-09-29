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
            Assert.That(BandGuidelines.Work(20, DifficultyClass.Hard), Is.EqualTo(new IntRange(50, 100)));
            Assert.That(BandGuidelines.Work(60, DifficultyClass.Normal), Is.EqualTo(new IntRange(90, 180)));
            Assert.That(BandGuidelines.Work(60, DifficultyClass.Hard), Is.EqualTo(new IntRange(150, 300)));
            Assert.That(BandGuidelines.Work(200, DifficultyClass.Normal).Min, Is.EqualTo(150));

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
                        Assert.That(BandGuidelines.Intersect(profile.PodCount, BandGuidelines.BandOf(level).Pods), Is.Not.Null, where + " pods");
                    }
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
            // Curated L3: an 8×8 board with a 4-tile pod. As L3 the small pod only warns (hand-curated tutorial level).
            LevelDefinition three = Curated(3);
            List<LevelIssue> asTutorial = Validate(three);
            Assert.That(asTutorial.Where(i => i.IsError), Is.Empty);
            Assert.That(asTutorial.Any(i => !i.IsError && i.Check == "band" && i.Message.Contains("4 tiles")), Is.True);

            // The same level as L11 breaks the early band: board 9–10×10 and pods of 5+.
            List<LevelIssue> asEarly = Validate(three with { LevelNumber = 11 });
            var band = asEarly.Where(i => i.IsError && i.Check == "band").Select(i => i.Message).ToList();
            Assert.That(band.Any(m => m.Contains("board 8×8")), Is.True, string.Join("; ", band));
            Assert.That(band.Any(m => m.Contains("4 tiles")), Is.True, string.Join("; ", band));

            // As L51 its 3 variants are too few (5 in the core completion band).
            Assert.That(Validate(three with { LevelNumber = 51 }).Any(i => i.IsError && i.Check == "variant-count"), Is.True);
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
