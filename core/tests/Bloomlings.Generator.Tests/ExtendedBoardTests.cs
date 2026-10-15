using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    public class ExtendedBoardTests
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

        [TestCase(24, 24)]
        [TestCase(30, 32)]
        [TestCase(36, 36)]
        public void ExtendedBoardsPassTheReleaseChecksAndPlayAfterPacking(int width, int height)
        {
            string size = $"{width}x{height}";
            LevelDefinition level = DefinitionJson.Read(File.ReadAllText(Path.Combine(Repo, "content/curated/dev", $"level-dev-mosaic-{size}.json")));
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(Repo, "content/pictures/dev/lib", level.Picture.Id + ".json")));
            Assert.That((picture.Width, picture.Height), Is.EqualTo((width, height)));
            Assert.That(level.GeneratorVersion, Is.EqualTo(LevelGenerator.ExtendedBoardVersion));
            Assert.That(DefinitionJson.Read(DefinitionJson.Write(level)), Is.Not.Null);
            var pairs = ContentStore.LoadPairs(Path.Combine(Repo, "content/readability/approved-pairs.json"))!;
            var validator = new CatalogValidator(new[] { picture }, UnlockRoadmap.Default, pairs, new SolveOptions(200000));
            CatalogReport report = validator.Validate(new[] { level });
            Assert.That(report.Issues.Where(i => i.IsError).Select(i => i.Message), Is.Empty);
            Assert.That(report.Records[level.LevelNumber].Checks, Does.Contain("band").And.Contain("board").And.Contain("solvable").And.Contain("losable"));

            var request = new PublishRequest(1, "1.0.0", 1, 20000, new[] { level }, new[] { picture }, Array.Empty<DailyPoolEntry>());
            ContentManifest manifest = ManifestWriter.Build(request, out IReadOnlyList<PackFile> files);
            var bytes = files.ToDictionary(f => f.Entry.Id, f => f.Bytes);
            ContentSet packed = ContentLoader.FromPacks(ContentManifest.Read(manifest.Write()), entry => bytes[entry.Id]);
            LevelDefinition restored = packed.GetLevel(level.LevelNumber);
            Assert.That(DefinitionJson.Write(restored), Is.EqualTo(DefinitionJson.Write(level)));
            var session = LevelSession.Load(restored, packed.GetPicture(restored.Picture), new SessionOptions(1, 20000));
            Assert.That(session.View.Cell(width * height - 1).Kind, Is.EqualTo(session.View.Cell(new CellPos(width - 1, height - 1)).Kind));
            SolveResult win = new Solver.Solver().Solve(session, new SolveOptions(200000));
            Assert.That(win.Status, Is.EqualTo(SolveStatus.Solvable));
            foreach (Command command in win.Trace) Assert.That(session.Apply(command).Accepted, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.StateHash, Is.EqualTo(session.Clone().StateHash));
        }

        [Test]
        public void LargestBoardReproducesThroughTheNormalGenerator()
        {
            GenerationProfile profile = ProfileLoader.ReadFile(Path.Combine(Repo, "content/profiles/dev/band-mosaic-36x36.json"));
            var library = ContentStore.LoadLibrary(Path.Combine(Repo, "content/pictures/dev/lib"));
            var pairs = ContentStore.LoadPairs(Path.Combine(Repo, "content/readability/approved-pairs.json"))!;
            string thresholds = File.ReadAllText(Path.Combine(Repo, "content/profiles/difficulty-thresholds.json"));
            var generator = new LevelGenerator(profile, new PicturePicker(library),
                ProfileLoader.ReadThresholds(thresholds, profile.BandId), pairs.IsApproved,
                new DifficultySchedule(0xB100B100UL))
            {
                ForcedClass = DifficultyClass.Normal,
                BigBoardThresholds = ProfileLoader.ReadBigThresholds(thresholds, profile.BandId),
            };
            GenerationResult result = generator.Generate(5003, 5003, 2, new Dictionary<int, LevelDefinition>());
            Assert.That(result.Failed, Is.Empty, string.Join("; ", result.Rejections.Select(r => r.Reason)));
            Assert.That(result.Accepted.Count, Is.EqualTo(1));
            string expected = File.ReadAllText(Path.Combine(Repo, "content/curated/dev/level-dev-mosaic-36x36.json"));
            Assert.That(DefinitionJson.Write(result.Accepted.Single().Definition), Is.EqualTo(expected));
        }

        [TestCase("hard", DifficultyClass.Hard)]
        [TestCase("super-hard", DifficultyClass.SuperHard)]
        public void LargestBoardKeepsTheHardClassAndPressureUnderReleaseValidation(string name, DifficultyClass expected)
        {
            LevelDefinition level = DefinitionJson.Read(File.ReadAllText(Path.Combine(Repo,
                "core/tests/Bloomlings.Generator.Tests/Fixtures", $"extended-36-{name}.level.json")));
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(Repo, "content/pictures/dev/lib", level.Picture.Id + ".json")));
            var pairs = ContentStore.LoadPairs(Path.Combine(Repo, "content/readability/approved-pairs.json"))!;
            CatalogReport report = new CatalogValidator(new[] { picture }, UnlockRoadmap.Default, pairs, new SolveOptions(200000)).Validate(new[] { level });
            Assert.That(report.Issues.Where(i => i.IsError).Select(i => i.Message), Is.Empty);
            var record = report.Records[level.LevelNumber];
            string json = File.ReadAllText(Path.Combine(Repo, "content/profiles/difficulty-thresholds.json"));
            DifficultyThresholds thresholds = BandGuidelines.ThresholdsFor(
                ProfileLoader.ReadThresholds(json, "band-2001-5000"),
                ProfileLoader.ReadBigThresholds(json, "band-2001-5000"), picture.Width * picture.Height);
            int score = DifficultyScorer.Score(record.Metrics, thresholds);
            Assert.That(level.Difficulty.Overridden, Is.False);
            Assert.That(score, Is.EqualTo(level.Difficulty.Score));
            Assert.That(DifficultyScorer.Classify(score, thresholds), Is.EqualTo(expected));
            Assert.That(BandGuidelines.BigBoardPeakSlots(expected).Contains(record.Metrics["peakBuffer"]), Is.True);
            Assert.That(record.SolutionTrace, Is.Not.Empty);
            Assert.That(record.JamWitness, Is.Not.Empty);
        }

        [Test]
        public void ExtendedTrayDoesNotReturnAClassMatchOutsideTheRequestedPressure()
        {
            LevelDefinition level = DefinitionJson.Read(File.ReadAllText(Path.Combine(Repo, "content/curated/dev/level-dev-mosaic-36x36.json")));
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(Repo, "content/pictures/dev/lib", level.Picture.Id + ".json")));
            string json = File.ReadAllText(Path.Combine(Repo, "content/profiles/difficulty-thresholds.json"));
            DifficultyThresholds thresholds = BandGuidelines.ThresholdsFor(
                ProfileLoader.ReadThresholds(json, "band-2001-5000"),
                ProfileLoader.ReadBigThresholds(json, "band-2001-5000"), picture.Width * picture.Height);
            PlannedPod[] plan = level.Pods.Select(p => new PlannedPod(p.Variant, p.Count, 0, p.LockKeyId)).ToArray();
            var rng = new Xoshiro256StarStar(1);
            TrayOutcome? valid = TrayBuilder.Tune(level, picture, plan, level.Tray.Stacks.Count, DifficultyClass.Normal,
                true, 0, thresholds, new SolveOptions(200000), ref rng, out string? reason,
                peakSlots: BandGuidelines.BigBoardPeakSlots(DifficultyClass.Normal), preventClassOvershoot: true);
            Assert.That(valid, Is.Not.Null, reason);
            TrayOutcome? rejected = TrayBuilder.Tune(level, picture, plan, level.Tray.Stacks.Count, DifficultyClass.Normal,
                true, 0, thresholds, new SolveOptions(200000), ref rng, out reason,
                peakSlots: new IntRange(0, 0), preventClassOvershoot: true);
            Assert.That(rejected, Is.Null, "a class match must still meet the requested pressure");
            Assert.That(reason, Does.StartWith("tray:pressure-"));
        }

        [Test]
        public void LargeProfileCountsGrowWhileLegacyBalanceStaysPinned()
        {
            GenerationProfile profile = ProfileLoader.ReadFile(Path.Combine(Repo, "content/profiles/band-0501-1000.json"));
            Assert.That(profile.ForBoard(616), Is.SameAs(profile));
            Assert.That(profile.ForBoard(1296).Work.Max, Is.GreaterThan(1200));
            Assert.That(profile.ForBoard(1296).PodCount.Max, Is.GreaterThan(profile.PodCount.Max));
            Assert.That(profile.ForBoard(1296).PodSize, Is.EqualTo(profile.PodSize));
            var regular = DifficultyThresholds.Default with { HardMin = 2400, SuperHardMin = 3200 };
            var big = regular with { HardMin = 3900, SuperHardMin = 4700 };
            Assert.That(BandGuidelines.ThresholdsFor(regular, big, 452).HardMin, Is.EqualTo(3150));
            Assert.That(BandGuidelines.ThresholdsFor(regular, big, 616).HardMin, Is.EqualTo(3900));
            DifficultyThresholds expanded = BandGuidelines.ThresholdsFor(regular, big, 1296);
            Assert.That(expanded.HardMin, Is.EqualTo(7010));
            Assert.That(expanded.SuperHardMin - expanded.HardMin, Is.EqualTo(big.SuperHardMin - big.HardMin),
                "board work grows while the five-slot danger gap stays calibrated");
            var library = ContentStore.LoadLibrary(Path.Combine(Repo, "content/pictures/dev/lib"));
            var picker = new PicturePicker(library);
            Assert.That(picker.Candidates(profile, 601, new Dictionary<int, LevelDefinition>(), BandGuidelines.Board(601)).Select(p => (p.Width, p.Height)),
                Is.EquivalentTo(new[] { (24, 24), (30, 32), (36, 36) }));
        }
    }
}
