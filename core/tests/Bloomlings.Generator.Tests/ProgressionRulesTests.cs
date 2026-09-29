using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Overlays;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    /// <summary>The roadmap's Level 8 choice, the variant pool, practice levels and the late mechanics (FR-031, FR-035, FR-039, FR-060).</summary>
    public class ProgressionRulesTests
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

        private const string SmallBand = @"{
  ""bandId"": ""test-small"",
  ""levelRange"": [8, 500],
  ""boardSize"": { ""width"": [7, 8], ""height"": [8, 9] },
  ""picturePool"": { ""themes"": [] },
  ""structureTargets"": { ""nestingDepth"": [1, 6], ""backgroundSharePermille"": [0, 1000] },
  ""variantCount"": [3, 3],
  ""allowedVariants"": [""acorn"", ""dew"", ""flower"", ""leaf"", ""moss"", ""violet_bud"", ""water"", ""wood""],
  ""mappingConstraints"": { ""allowRoleMerge"": true },
  ""entryLayouts"": [""bottom_center""],
  ""maxLayerDepth"": 0,
  ""allowedMechanics"": [""stone"", ""mystery_pod"", ""chest"", ""environment_2"", ""connected_triple""],
  ""stacks"": [3, 3],
  ""podCount"": [7, 9],
  ""podSize"": [3, 40],
  ""work"": [20, 80],
  ""bufferPressureTarget"": ""relaxed"",
  ""durationTarget"": [20, 60],
  ""hardMode"": { ""extraPods"": 1, ""maxInjections"": 8, ""hardPressure"": ""tense"", ""superHardPressure"": ""critical"" },
  ""solver"": { ""nodeBudget"": 20000, ""maxCandidatesPerLevel"": 24 }
}";

        [Test]
        public void Level8_CanGoToTheMysteryPod_ThenTheKeyUnlocksAtL14()
        {
            Assert.That(UnlockRoadmap.Default.LevelOf("mechanic.key"), Is.EqualTo(8));
            Assert.That(UnlockRoadmap.Default.LevelOf("mechanic.mystery_pod"), Is.Null);

            UnlockRoadmap mystery = UnlockRoadmap.ForLevel8("mystery_pod");
            Assert.That(mystery.LevelOf("mechanic.mystery_pod"), Is.EqualTo(8));
            Assert.That(mystery.LevelOf("mechanic.key"), Is.EqualTo(14));
            Assert.That(mystery.LevelOf("profile.key_practice"), Is.Null);
            Assert.Throws<ArgumentException>(() => UnlockRoadmap.ForLevel8("stone"));
        }

        [Test]
        public void ExpansionVariants_JoinThePoolAtTheirMilestones()
        {
            VariantPool pool = VariantPool.Default;
            UnlockRoadmap roadmap = UnlockRoadmap.Default;

            Assert.That(pool.IntroducedAt(VariantId.Leaf, roadmap), Is.EqualTo(1));
            Assert.That(pool.IntroducedAt(VariantId.Vine, roadmap), Is.EqualTo(45));
            Assert.That(pool.IntroducedAt(VariantId.Berry, roadmap), Is.EqualTo(200));
            Assert.That(pool.IntroducedAt(VariantId.Mist, roadmap), Is.Null, "not scheduled");
            Assert.That(pool.IsAvailable(VariantId.Vine, 44, roadmap), Is.False);
            Assert.That(pool.JoiningAt(45, roadmap), Is.EqualTo(new[] { VariantId.Vine }));
            Assert.That(pool.ExpansionsAt(250, roadmap), Is.EquivalentTo(new[] { VariantId.Vine, VariantId.Berry }));
        }

        [TestCase("stone", 12)]
        [TestCase("key", 14)]
        [TestCase("locked_pod", 17)]
        [TestCase("connected_pair", 19)]
        [TestCase("layered_tile", 29)]
        [TestCase("gate", 36)]
        [TestCase("fountain", 61)]
        [TestCase("locked_slot", 81)]
        [TestCase("chest", 151)]
        public void EachMechanic_HasItsPracticeLevel(string mechanic, int practice)
        {
            Assert.That(MechanicNames.PracticeLevel(mechanic, UnlockRoadmap.Default, _ => DifficultyClass.Normal), Is.EqualTo(practice));
        }

        [Test]
        public void TheTriplesPractice_IsTheFirstHardLevelAfterItsShowcase()
        {
            DifficultyClass ClassOf(int level) => level == 404 ? DifficultyClass.Hard : DifficultyClass.Normal;

            Assert.That(MechanicNames.PracticeLevel(MechanicNames.ConnectedTriple, UnlockRoadmap.Default, ClassOf), Is.EqualTo(404));
        }

        [TestCase("chest", 150, "chest", false)]
        [TestCase("environment_2", 250, null, false)]
        [TestCase("connected_triple", 400, null, true)]
        public void LateMechanics_ArePlacedAndValidate(string mechanic, int level, string? specialType, bool hard)
        {
            // A triple commits three pods at once, so it only fits Hard pressure (doc 13: Hard and Super Hard levels).
            var generator = new LevelGenerator(
                ProfileLoader.Read(SmallBand),
                new PicturePicker(Library),
                DifficultyThresholds.Default with { HardMin = hard ? 0 : 100_000, SuperHardMin = 200_000 },
                Pairs.IsApproved,
                new DifficultySchedule(7))
            {
                ForcedMechanics = new[] { mechanic },
                ForcedClass = hard ? DifficultyClass.Hard : DifficultyClass.Normal,
                UseBandGuidelines = false,
            };
            var history = new SortedDictionary<int, LevelDefinition>();

            GenerationResult result = generator.Generate(level, level, 7, history);

            Assert.That(result.Failed, Is.Empty, string.Join(", ", result.Rejections.Select(r => r.Reason).Distinct()));
            LevelDefinition generated = result.Accepted.Single().Definition;
            BasePicture picture = Library.Single(p => p.Id == generated.Picture.Id);
            Assert.That(LevelMechanics.UnlocksUsed(generated, picture), Does.Contain("mechanic." + mechanic));
            if (specialType != null)
            {
                Assert.That(generated.Specials.Select(s => s.Type.ToString().ToLowerInvariant()), Does.Contain(specialType));
            }

            var validator = new CatalogValidator(Library, UnlockRoadmap.Default, Pairs, new SolveOptions(20000)) { CheckBandGuidelines = false };
            IEnumerable<string> errors = validator.Validate(history.Values.ToList()).Issues.Where(i => i.IsError).Select(i => $"{i.Check}: {i.Message}");
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void AMysteryPod_IsPlacedFairly_WhenLevel8GoesToIt()
        {
            UnlockRoadmap roadmap = UnlockRoadmap.MysteryPodAtLevel8;
            var generator = new LevelGenerator(
                ProfileLoader.Read(SmallBand),
                new PicturePicker(Library),
                DifficultyThresholds.Default with { HardMin = 100_000, SuperHardMin = 200_000 },
                Pairs.IsApproved,
                new DifficultySchedule(7),
                roadmap)
            {
                ForcedMechanics = new[] { MechanicNames.MysteryPod },
                ForcedClass = DifficultyClass.Normal,
                UseBandGuidelines = false,
            };
            var history = new SortedDictionary<int, LevelDefinition>();

            // L13: past the Stone showcase (L11) and practice (L12), before the Key's new showcase at L14.
            GenerationResult result = generator.Generate(13, 13, 7, history);

            Assert.That(result.Failed, Is.Empty, string.Join(", ", result.Rejections.Select(r => r.Reason).Distinct()));
            GeneratedLevel generated = result.Accepted.Single();
            Assert.That(generated.Definition.Pods.Count(p => p.Mystery), Is.EqualTo(1));
            Assert.That(generated.Record.PlayerInfoFair, Is.True);

            var validator = new CatalogValidator(Library, roadmap, Pairs, new SolveOptions(20000)) { CheckBandGuidelines = false };
            Assert.That(validator.Validate(history.Values.ToList()).Issues.Where(i => i.IsError).Select(i => i.Message), Is.Empty);
            var byDefault = new CatalogValidator(Library, UnlockRoadmap.Default, Pairs, new SolveOptions(20000)) { CheckBandGuidelines = false };
            Assert.That(byDefault.Validate(history.Values.ToList()).Issues.Any(i => i.IsError && i.Check == "unlock"), Is.True, "not on the default roadmap");
        }
    }
}
