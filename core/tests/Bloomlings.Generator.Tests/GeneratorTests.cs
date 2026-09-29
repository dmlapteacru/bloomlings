using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Variants;
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
    /// small approved pictures of the repository (onboarding and dev) and a small test band.
    /// </summary>
    public class GeneratorTests
    {
        private const ulong Seed = 20260929;

        private const string SmallBand = @"{
  ""bandId"": ""test-small"",
  ""levelRange"": [11, 40],
  ""boardSize"": { ""width"": [7, 8], ""height"": [8, 9] },
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
  ""work"": [20, 80],
  ""bufferPressureTarget"": ""normal"",
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
            var generator = new LevelGenerator(profile, new PicturePicker(Library), Thresholds, Pairs.IsApproved, new DifficultySchedule(Seed));
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

            var validator = new CatalogValidator(Library, UnlockRoadmap.Default, Pairs, new SolveOptions(20000));
            CatalogReport report = validator.Validate(history.Values.ToList());
            IEnumerable<string> errors = report.Issues.Where(i => i.IsError).Select(i => $"L{i.Level} {i.Check}: {i.Message}");

            Assert.That(errors, Is.Empty);
            foreach (GeneratedLevel level in result.Accepted)
            {
                Assert.That(level.Record.Result, Is.EqualTo(Bloomlings.Content.Validation.ValidationResult.Solvable));
                Assert.That(level.Record.JamWitness, Is.Not.Null.And.Not.Empty, "every level from L11 on is losable (FR-081)");
                Assert.That(level.Record.DefinitionHash, Is.EqualTo(report.Records[level.Definition.LevelNumber].DefinitionHash));
            }
        }

        [Test]
        public void RejectedCandidates_RecordAReason()
        {
            // No approved picture has 500+ tile-layers, so every candidate is discarded at the work check.
            string impossible = SmallBand.Replace(@"""work"": [20, 80]", @"""work"": [500, 900]", StringComparison.Ordinal);

            GenerationResult result = Run(impossible, 11, 11).Result;

            Assert.That(result.Accepted, Is.Empty);
            Assert.That(result.Failed, Is.EqualTo(new[] { 11 }));
            Assert.That(result.Rejections, Has.Count.EqualTo(16));
            Assert.That(result.Rejections.Select(r => r.Reason), Has.All.Not.Empty);
            Assert.That(result.Rejections.Where(r => r.Reason.StartsWith("work:", StringComparison.Ordinal)), Is.Not.Empty);
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
                for (int y = 0; y < picture.Height; y++)
                {
                    for (int x = 0; x < picture.Width; x++)
                    {
                        int sourceX = level.Picture.Mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
                        int role = picture.CellAt(sourceX, y);
                        int index = board.IndexOf(new CellPos(x, y));
                        if (role >= 0)
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
    }
}
