using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Boards;
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
    /// <summary>
    /// T069: the generator is deterministic, every accepted level passes <see cref="CatalogValidator"/>, every discarded
    /// candidate records a reason, and the visible top layer always follows the picture mapping. The fixture uses the
    /// approved onboarding pictures of the repository (11–12×12 since 2026-10-05) and a small test band. Those levels are
    /// not tutorial levels, so these tests turn the band guidelines off; <see cref="BandGuidelinesTests"/> covers them.
    /// </summary>
    public class GeneratorTests
    {
        private const ulong Seed = 20260929;

        private const string SmallBand = @"{
  ""bandId"": ""test-small"",
  ""levelRange"": [11, 40],
  ""boardSize"": { ""width"": [11, 12], ""height"": [12, 12] },
  ""picturePool"": { ""themes"": [] },
  ""structureTargets"": { ""nestingDepth"": [1, 6], ""backgroundSharePermille"": [0, 1000] },
  ""variantCount"": [3, 3],
  ""allowedVariants"": [""acorn"", ""dew"", ""flower"", ""leaf"", ""moss"", ""violet_bud"", ""water"", ""wood""],
  ""mappingConstraints"": { ""allowRoleMerge"": true },
  ""entryLayouts"": [""bottom_center""],
  ""maxLayerDepth"": 0,
  ""allowedMechanics"": [""stone""],
  ""stacks"": [2, 3],
  ""podCount"": [7, 9],
  ""podSize"": [3, 40],
  ""work"": [90, 140],
  ""bufferPressureTarget"": ""relaxed"",
  ""durationTarget"": [20, 60],
  ""hardMode"": { ""extraPods"": 1, ""maxInjections"": 8, ""hardPressure"": ""tense"", ""superHardPressure"": ""critical"" },
  ""solver"": { ""nodeBudget"": 20000, ""maxCandidatesPerLevel"": 16 }
}";

        private static readonly DifficultyThresholds Thresholds = DifficultyThresholds.Default with { HardMin = 100_000, SuperHardMin = 200_000 };

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

        private static (GenerationResult Result, SortedDictionary<int, LevelDefinition> History) Run(string profileJson, int first, int last)
        {
            GenerationProfile profile = ProfileLoader.Read(profileJson);
            var generator = new LevelGenerator(profile, new PicturePicker(Library), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed))
            {
                UseBandGuidelines = false,
            };
            var history = new SortedDictionary<int, LevelDefinition>();
            GenerationResult result = generator.Generate(first, last, Seed, history);
            return (result, history);
        }

        [Test]
        public void SameProfileAndSeed_GiveByteIdenticalDefinitions()
        {
            GenerationResult a = Run(SmallBand, 11, 12).Result;
            GenerationResult b = Run(SmallBand, 11, 12).Result;

            Assert.That(a.Accepted, Is.Not.Empty);
            Assert.That(b.Accepted.Select(l => DefinitionJson.Write(l.Definition)), Is.EqualTo(a.Accepted.Select(l => DefinitionJson.Write(l.Definition))));
            Assert.That(b.Accepted.Select(l => l.Record.Write()), Is.EqualTo(a.Accepted.Select(l => l.Record.Write())));
            Assert.That(b.Rejections, Is.EqualTo(a.Rejections));
        }

        [Test]
        public void AcceptedLevels_PassTheCatalogValidator()
        {
            (GenerationResult result, SortedDictionary<int, LevelDefinition> history) = Run(SmallBand, 11, 12);
            Assert.That(result.Failed, Is.Empty, "levels without an accepted candidate: " + string.Join(", ", result.Rejections.Select(r => r.Reason)));

            var validator = new CatalogValidator(Library, UnlockRoadmap.Default, Pairs, new SolveOptions(20000)) { CheckBandGuidelines = false };
            CatalogReport report = validator.Validate(history.Values.ToList());
            IEnumerable<string> errors = report.Issues.Where(i => i.IsError).Select(i => $"L{i.Level} {i.Check}: {i.Message}");

            Assert.That(errors, Is.Empty);
            foreach (GeneratedLevel level in result.Accepted)
            {
                // The generator always writes the board look from the cell count (FR-036 as amended on 2026-10-06).
                Assert.That(level.Definition.BoardLook, Is.EqualTo(BoardLook.Peek), "12×12 boards peek");
                Assert.That(DefinitionJson.Write(level.Definition), Does.Contain("\"boardLook\": \"peek\""));
                Assert.That(level.Record.Result, Is.EqualTo(Bloomlings.Content.Validation.ValidationResult.Solvable));
                Assert.That(level.Record.JamWitness, Is.Not.Null.And.Not.Empty, "every level from L11 on is losable (FR-081)");
                Assert.That(level.Record.DefinitionHash, Is.EqualTo(report.Records[level.Definition.LevelNumber].DefinitionHash));
            }
        }

        [Test]
        public void RejectedCandidates_RecordAReason()
        {
            // No approved picture has 500+ tile-layers, so every candidate is discarded at the work check.
            string impossible = SmallBand.Replace(@"""work"": [90, 140]", @"""work"": [500, 900]", StringComparison.Ordinal);

            GenerationResult result = Run(impossible, 11, 11).Result;

            Assert.That(result.Accepted, Is.Empty);
            Assert.That(result.Failed, Is.EqualTo(new[] { 11 }));
            Assert.That(result.Rejections, Has.Count.EqualTo(16));
            Assert.That(result.Rejections.Select(r => r.Reason), Has.All.Not.Empty);
            Assert.That(result.Rejections.Where(r => r.Reason.StartsWith("work:", StringComparison.Ordinal)), Is.Not.Empty);
        }

        [Test]
        public void TrayRejections_NameTheScoreTheTuningReached()
        {
            // Hard is out of reach (HardMin 100 000), so every candidate that gets to the tray tuner is rejected there.
            string band = SmallBand.Replace(@"""maxCandidatesPerLevel"": 16", @"""maxCandidatesPerLevel"": 3", StringComparison.Ordinal);
            var generator = new LevelGenerator(ProfileLoader.Read(band), new PicturePicker(Library), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed))
            {
                ForcedClass = DifficultyClass.Hard,
                UseBandGuidelines = false,
            };

            GenerationResult result = generator.Generate(11, 11, Seed, new SortedDictionary<int, LevelDefinition>());

            Assert.That(result.Failed, Is.EqualTo(new[] { 11 }));
            List<string> tray = result.Rejections.Select(r => r.Reason).Where(r => r.StartsWith("tray:class-", StringComparison.Ordinal)).ToList();
            Assert.That(tray, Is.Not.Empty, string.Join(", ", result.Rejections.Select(r => r.Reason)));
            foreach (string reason in tray)
            {
                Assert.That(reason, Does.Match(@"^tray:class-normal-not-hard:score-\d+$"));
                int score = int.Parse(reason.Substring(reason.LastIndexOf('-') + 1), System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(score, Is.GreaterThan(0).And.LessThan(Thresholds.HardMin));
            }
        }

        [Test]
        public void VisibleTopLayer_FollowsThePictureMapping()
        {
            GenerationResult result = Run(SmallBand, 11, 12).Result;
            Dictionary<string, BasePicture> pictures = Library.ToDictionary(p => p.Id);
            Assert.That(result.Accepted, Is.Not.Empty);

            foreach (GeneratedLevel generated in result.Accepted)
            {
                LevelDefinition level = generated.Definition;
                BasePicture picture = pictures[level.Picture.Id];
                Board board = BoardBuilder.Build(level, picture, VariantCatalog.Default);

                // Stones and holes are gameplay overlays on background cells; every other picture cell keeps its tile.
                var overlaid = new HashSet<CellPos>(level.Overlays.Where(o => o.Stone || o.Hole).Select(o => o.Cell));
                for (int y = 0; y < picture.Height; y++)
                {
                    for (int x = 0; x < picture.Width; x++)
                    {
                        int sourceX = level.Picture.Mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
                        int role = picture.CellAt(sourceX, y);
                        int index = board.IndexOf(new CellPos(x, y));
                        if (overlaid.Contains(new CellPos(x, y)))
                        {
                            Assert.That(picture.Roles[role].IsBackground, Is.True, $"L{level.LevelNumber} ({x},{y}): stones and holes go on the background");
                        }
                        else if (role >= 0)
                        {
                            Assert.That(board.TopLayer(index), Is.EqualTo(level.Mapping[picture.Roles[role].RoleId]), $"L{level.LevelNumber} ({x},{y})");
                        }
                        else
                        {
                            Assert.That(board.IsTarget(index), Is.False, $"L{level.LevelNumber} ({x},{y})");
                        }
                    }
                }
            }
        }

        [TestCase("stone", 11)]
        [TestCase("key", 14)]
        [TestCase("locked_pod", 16)]
        [TestCase("connected_pair", 18)]
        public void ForcedMechanic_IsPlacedAndTheLevelValidates(string mechanic, int level)
        {
            string band = SmallBand.Replace(@"""allowedMechanics"": [""stone""]", @"""allowedMechanics"": [""stone"", ""key"", ""locked_pod"", ""connected_pair""]", StringComparison.Ordinal);
            var generator = new LevelGenerator(ProfileLoader.Read(band), new PicturePicker(Library), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed))
            {
                ForcedMechanics = new[] { mechanic },
                ForcedClass = DifficultyClass.Normal,
                UseBandGuidelines = false,
            };
            var history = new SortedDictionary<int, LevelDefinition>();

            GenerationResult result = generator.Generate(level, level, Seed, history);

            Assert.That(result.Failed, Is.Empty, string.Join(", ", result.Rejections.Select(r => r.Reason).Distinct()));
            LevelDefinition generated = result.Accepted.Single().Definition;
            Assert.That(generated.Mechanics, Does.Contain(mechanic));
            BasePicture picture = Library.Single(p => p.Id == generated.Picture.Id);
            Assert.That(LevelMechanics.UnlocksUsed(generated, picture), Does.Contain("mechanic." + mechanic));

            var validator = new CatalogValidator(Library, UnlockRoadmap.Default, Pairs, new SolveOptions(20000)) { CheckBandGuidelines = false };
            IEnumerable<string> errors = validator.Validate(history.Values.ToList()).Issues.Where(i => i.IsError).Select(i => $"{i.Check}: {i.Message}");
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void MechanicBeforeItsUnlock_IsRefused()
        {
            string band = SmallBand.Replace(@"""allowedMechanics"": [""stone""]", @"""allowedMechanics"": [""stone"", ""locked_pod""]", StringComparison.Ordinal);
            var generator = new LevelGenerator(ProfileLoader.Read(band), new PicturePicker(Library), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed))
            {
                ForcedMechanics = new[] { "locked_pod" },
            };

            Assert.Throws<ArgumentException>(() => generator.Generate(15, 15, Seed, new SortedDictionary<int, LevelDefinition>()), "locked pods unlock at L16");
        }

        [Test]
        public void Schedule_KeepsReliefAfterSuperHardAndNoSuperHardMilestones()
        {
            var schedule = new DifficultySchedule(Seed);
            int hard = 0;
            int superHard = 0;
            for (int level = 11; level <= 1010; level++)
            {
                DifficultyClass c = schedule.ClassFor(level);
                if (c == DifficultyClass.SuperHard)
                {
                    superHard++;
                    Assert.That(level % 25, Is.Not.Zero, $"L{level} is a milestone");
                    Assert.That(schedule.ClassFor(level + 1), Is.EqualTo(DifficultyClass.Normal), $"relief after L{level}");
                }
                else if (c == DifficultyClass.Hard)
                {
                    hard++;
                }
            }

            // FR-059: per 100 levels about 15–25 Hard and 6–10 Super Hard.
            Assert.That(hard, Is.InRange(150, 250));
            Assert.That(superHard, Is.InRange(60, 100));
        }

        /// <summary>The catalog's schedule (the seed of <c>generate</c>) puts a Super Hard on L151, the chest's practice.</summary>
        [Test]
        public void Schedule_MovesASuperHardOffAPracticeLevel_ToTheNextFreeLevel()
        {
            var plain = new DifficultySchedule(0xB100B100UL);
            var schedule = new DifficultySchedule(0xB100B100UL, UnlockRoadmap.Default);
            Assert.That(plain.ClassFor(151), Is.EqualTo(DifficultyClass.SuperHard), "the seed's own schedule");
            Assert.That(schedule.PracticeLevels(), Does.Contain(151));

            Assert.That(schedule.ClassFor(151), Is.Not.EqualTo(DifficultyClass.SuperHard));
            int moved = Enumerable.Range(152, 10).First(l => schedule.ClassFor(l) == DifficultyClass.SuperHard);
            Assert.That(plain.ClassFor(moved), Is.Not.EqualTo(DifficultyClass.SuperHard));
            Assert.That(schedule.ClassFor(151), Is.EqualTo(plain.ClassFor(moved)), "the practice level takes the class of the level the Super Hard moves to");
            Assert.That(schedule.ClassFor(moved + 1), Is.EqualTo(DifficultyClass.Normal), "relief");
            for (int level = 11; level <= 2010; level++)
            {
                if (level != 151 && level != moved)
                {
                    Assert.That(schedule.ClassFor(level), Is.EqualTo(plain.ClassFor(level)), $"L{level}: the rest of the schedule is unchanged");
                }
            }
        }

        [TestCase(0xB100B100UL, "key")]
        [TestCase(0xB100B100UL, "mystery_pod")]
        [TestCase(1UL, "key")]
        [TestCase(7UL, "key")]
        [TestCase(Seed, "key")]
        [TestCase(Seed, "mystery_pod")]
        public void Schedule_NeverPutsSuperHardOnAPracticeLevel_AndKeepsItsRules(ulong seed, string level8)
        {
            UnlockRoadmap roadmap = UnlockRoadmap.ForLevel8(level8);
            var plain = new DifficultySchedule(seed);
            var schedule = new DifficultySchedule(seed, roadmap);
            IReadOnlyCollection<int> practices = schedule.PracticeLevels();
            Assert.That(practices, Has.Count.GreaterThanOrEqualTo(10));
            foreach (int practice in practices)
            {
                Assert.That(schedule.ClassFor(practice), Is.Not.EqualTo(DifficultyClass.SuperHard), $"L{practice} is a practice level");
            }

            // The triple's practice stays the first Hard (or Super Hard) level after L400, and it is Hard.
            int triple = MechanicNames.PracticeLevel(MechanicNames.ConnectedTriple, roadmap, schedule.ClassFor)!.Value;
            Assert.That(practices, Does.Contain(triple));
            Assert.That(schedule.ClassFor(triple), Is.EqualTo(DifficultyClass.Hard));

            for (int level = 11; level <= 5010; level++)
            {
                if (schedule.ClassFor(level) == DifficultyClass.SuperHard)
                {
                    Assert.That(level % 25, Is.Not.Zero, $"L{level} is a milestone (FR-059)");
                    Assert.That(schedule.ClassFor(level + 1), Is.EqualTo(DifficultyClass.Normal), $"relief after L{level}");
                }
            }

            // FR-059: every block of 100 from L11 keeps the counts it had before the moves, which the score check judges.
            for (int start = 11; start + 99 <= 5010; start += 100)
            {
                int Count(DifficultySchedule s, DifficultyClass c) => Enumerable.Range(start, 100).Count(l => s.ClassFor(l) == c);
                Assert.That(Count(schedule, DifficultyClass.Hard), Is.EqualTo(Count(plain, DifficultyClass.Hard)).Within(1), $"Hard in L{start}–{start + 99}");
                Assert.That(Count(schedule, DifficultyClass.SuperHard), Is.EqualTo(Count(plain, DifficultyClass.SuperHard)).Within(1), $"Super Hard in L{start}–{start + 99}");
            }
        }

        [Test]
        public void TheCatalogSchedule_Holds15To25HardAnd6To10SuperHard_InEveryBlockOf100()
        {
            var schedule = new DifficultySchedule(0xB100B100UL, UnlockRoadmap.Default);
            for (int start = 11; start + 99 <= 5010; start += 100)
            {
                int hard = Enumerable.Range(start, 100).Count(l => schedule.ClassFor(l) == DifficultyClass.Hard);
                int superHard = Enumerable.Range(start, 100).Count(l => schedule.ClassFor(l) == DifficultyClass.SuperHard);
                Assert.That(hard, Is.InRange(15, 25), $"Hard in L{start}–{start + 99}");
                Assert.That(superHard, Is.InRange(6, 10), $"Super Hard in L{start}–{start + 99}");
            }
        }

        [Test]
        public void Segments_FixTheLevels_WhateverTheThreads()
        {
            Assert.That(Bloomlings.Pipeline.Commands.GenerateCommand.Segments(11, 110, 5), Is.EqualTo(new[] { (11, 60), (61, 110) }), "at most one per 50 levels");
            Assert.That(Bloomlings.Pipeline.Commands.GenerateCommand.Segments(11, 40, 3), Is.EqualTo(new[] { (11, 40) }));
            Assert.That(Bloomlings.Pipeline.Commands.GenerateCommand.Segments(11, 16, 3, minLength: 2), Is.EqualTo(new[] { (11, 12), (13, 14), (15, 16) }));

            GenerationProfile profile = ProfileLoader.Read(SmallBand);
            LevelGenerator NewGenerator() => new LevelGenerator(profile, new PicturePicker(Library), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed))
            {
                ForcedClass = DifficultyClass.Normal,
                UseBandGuidelines = false,
            };

            (GenerationResult Result, int SeamRepairs) Run(int jobs) => Bloomlings.Pipeline.Commands.GenerateCommand.GenerateRange(
                NewGenerator, 11, 14, Seed, new SortedDictionary<int, LevelDefinition>(), new HashSet<int>(), segments: 2, jobs: jobs, minSegmentLength: 2);

            // Two segments (L11–12 and L13–14) on one thread, then on two: the same levels, rejections and seam repairs.
            (GenerationResult one, int oneRepairs) = Run(1);
            (GenerationResult two, int twoRepairs) = Run(2);
            Assert.That(one.Accepted, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(two.Accepted.Select(l => DefinitionJson.Write(l.Definition)), Is.EqualTo(one.Accepted.Select(l => DefinitionJson.Write(l.Definition))));
            Assert.That(two.Rejections, Is.EqualTo(one.Rejections));
            Assert.That(two.Failed, Is.EqualTo(one.Failed));
            Assert.That(twoRepairs, Is.EqualTo(oneRepairs));
        }

        [Test]
        public void SeamRepair_RedoesOnlyTheLaterLevel_AndKeepsTheEarlierSegment()
        {
            // Six levels in three segments of two. Blind to each other, the segments give levels without mechanics side by
            // side (the small band only has stones), and three such levels in a row break FR-083 across a seam.
            GenerationProfile profile = ProfileLoader.Read(SmallBand);
            List<BasePicture> pictures = Library;
            LevelGenerator NewGenerator() => new LevelGenerator(profile, new PicturePicker(pictures), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed))
            {
                ForcedClass = DifficultyClass.Normal,
                UseBandGuidelines = false,
            };

            (GenerationResult Result, int SeamRepairs) Run(int jobs) => Bloomlings.Pipeline.Commands.GenerateCommand.GenerateRange(
                NewGenerator, 11, 16, Seed, new SortedDictionary<int, LevelDefinition>(), new HashSet<int>(), segments: 3, jobs: jobs, minSegmentLength: 2);

            (GenerationResult result, int repairs) = Run(1);
            Assert.That(repairs, Is.GreaterThan(0), "the fixture must have a seam to repair");
            Assert.That(result.Accepted, Has.Count.GreaterThanOrEqualTo(4), "a level of this small band may fail on its own");
            Dictionary<int, LevelDefinition> final = result.Accepted.ToDictionary(l => l.Definition.LevelNumber, l => l.Definition);

            // The first segment, generated alone, is what the result keeps: its levels are never the later side of a seam.
            GenerationResult firstAlone = NewGenerator().Generate(11, 12, Seed, new SortedDictionary<int, LevelDefinition>());
            Assert.That(firstAlone.Accepted, Is.Not.Empty);
            foreach (GeneratedLevel level in firstAlone.Accepted)
            {
                Assert.That(DefinitionJson.Write(final[level.Definition.LevelNumber]), Is.EqualTo(DefinitionJson.Write(level.Definition)));
            }

            // Every level fits its neighbours on both sides.
            var history = new SortedDictionary<int, LevelDefinition>(final);
            foreach (int level in history.Keys)
            {
                Assert.That(LevelGenerator.Conflicts(level, history), Is.Empty, $"L{level}");
            }

            // The same levels and repairs on two threads.
            (GenerationResult two, int twoRepairs) = Run(2);
            Assert.That(two.Accepted.Select(l => DefinitionJson.Write(l.Definition)), Is.EqualTo(result.Accepted.Select(l => DefinitionJson.Write(l.Definition))));
            Assert.That(twoRepairs, Is.EqualTo(repairs));
        }
    }
}
