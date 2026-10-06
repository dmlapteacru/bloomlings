using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    /// <summary>
    /// The owner's boards of 2026-10-06 (spec 001 FR-008 and FR-036 as amended, research R8b): regular boards of 224–288
    /// cells from L11, rare big levels of 289–616 cells (every milestone level from L525), and the board look stored in the
    /// level data, the next layer hidden on a big level's icons board. The fixtures are a big level the generator made on a
    /// 22×28 sketch (made before the library had big pictures) and a Super Hard level whose hidden layers, once hidden,
    /// force a blind guess.
    /// </summary>
    public class BigLevelTests
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

        private static string Fixture(string name) => File.ReadAllText(Path.Combine(RepoRoot, "core", "tests", "Bloomlings.Generator.Tests", "Fixtures", name));

        private static List<BasePicture> Library => ContentStore.LoadLibrary(Path.Combine(RepoRoot, "content", "pictures", "lib"));

        private static ApprovedPairs Pairs => ContentStore.LoadPairs(Path.Combine(RepoRoot, "content", "readability", "approved-pairs.json"))
            ?? throw new FileNotFoundException("approved-pairs.json");

        private static LevelDefinition BigLevel => DefinitionJson.Read(Fixture("big-level.level.json"));

        private static BasePicture BigPicture => BasePictureJson.Read(Fixture("big-level.picture.json"));

        private static GenerationProfile LongRun => ProfileLoader.ReadFile(Path.Combine(RepoRoot, "content", "profiles", "band-0501-1000.json"));

        [Test]
        public void BigLevels_AreTheMilestonesFromL525_AndNormal()
        {
            Assert.That(BandGuidelines.IsBigLevel(500), Is.False, "L500 is a milestone before the big levels");
            Assert.That(BandGuidelines.IsBigLevel(524), Is.False);
            Assert.That(BandGuidelines.IsBigLevel(525), Is.True);
            Assert.That(BandGuidelines.IsBigLevel(526), Is.False);
            Assert.That(BandGuidelines.IsBigLevel(5000), Is.True);

            var schedule = new DifficultySchedule(0xB100B100UL);
            int big = 0;
            for (int level = 1; level <= 5000; level++)
            {
                if (BandGuidelines.IsBigLevel(level))
                {
                    big++;
                    Assert.That(schedule.ClassFor(level), Is.EqualTo(DifficultyClass.Normal), $"L{level}: the tuner cannot make a big board Hard");
                }
            }

            Assert.That(big, Is.EqualTo(180), "rare: one level in 25 from L525");

            // A Hard due on a big level moves on: every 100 levels from L11 still hold 15–25 Hard ones (FR-059).
            for (int start = 11; start + 99 <= 5000; start += 7)
            {
                int hard = Enumerable.Range(start, 100).Count(l => schedule.ClassFor(l) == DifficultyClass.Hard);
                Assert.That(hard, Is.InRange(15, 25), $"L{start}–{start + 99}");
            }
        }

        [Test]
        public void Boards_FollowTheLevel_RegularFromL11_BigOnBigLevels()
        {
            Assert.That(BandGuidelines.Board(10).Allows(12, 12), Is.True, "the curated onboarding stays 11–12×12");
            Assert.That(BandGuidelines.Board(11).Allows(12, 12), Is.False, "from L11 at least 224 cells");
            Assert.That(BandGuidelines.Board(11).Allows(14, 16), Is.True);
            Assert.That(BandGuidelines.Board(300).Allows(16, 18), Is.True, "288 cells is the largest regular board");
            Assert.That(BandGuidelines.Board(300).Allows(13, 18), Is.False, "regular boards are 14–16 wide");
            Assert.That(BandGuidelines.Board(551).Allows(17, 18), Is.False, "306 cells is a big board");
            Assert.That(BandGuidelines.Board(550).Allows(17, 18), Is.True);
            Assert.That(BandGuidelines.Board(550).Allows(22, 28), Is.True, "the largest board");
            Assert.That(BandGuidelines.Board(550).Allows(16, 18), Is.False, "a big level has a big board");

            Assert.That(BoardLooks.For(14, 16), Is.EqualTo(BoardLook.Peek));
            Assert.That(BoardLooks.For(16, 18), Is.EqualTo(BoardLook.Peek));
            Assert.That(BoardLooks.For(17, 17), Is.EqualTo(BoardLook.Icons));
            Assert.That(BoardLooks.For(22, 28), Is.EqualTo(BoardLook.Icons));
        }

        [Test]
        public void ThePicturePicker_GivesBigPicturesToBigLevelsOnly()
        {
            var picker = new PicturePicker(Library.Append(BigPicture));
            var history = new Dictionary<int, LevelDefinition>();

            IReadOnlyList<BasePicture> big = picker.Candidates(LongRun, 550, history, BandGuidelines.Board(550));
            IReadOnlyList<BasePicture> regular = picker.Candidates(LongRun, 551, history, BandGuidelines.Board(551));

            Assert.That(big.Select(p => p.Id), Does.Contain(BigPicture.Id));
            Assert.That(big.Select(p => p.Width * p.Height), Has.All.InRange(BandGuidelines.BigMinCells, BandGuidelines.BigMaxCells));
            Assert.That(regular, Is.Not.Empty);
            Assert.That(regular.Select(p => p.Width * p.Height), Has.All.InRange(BandGuidelines.RegularMinCells, BoardLooks.MaxPeekCells));
        }

        [Test]
        public void TheGenerator_RefusesABigLevelWithoutABigPicture()
        {
            // The library without its big pictures (it has them since the big band was drawn).
            List<BasePicture> regularOnly = Library.Where(p => p.Width * p.Height <= BoardLooks.MaxPeekCells).ToList();
            var generator = new LevelGenerator(LongRun, new PicturePicker(regularOnly), DifficultyThresholds.Default, Pairs.IsApproved, new DifficultySchedule(1));

            GenerationResult result = generator.Generate(550, 550, 1, new SortedDictionary<int, LevelDefinition>());

            Assert.That(result.Failed, Is.EqualTo(new[] { 550 }));
            Assert.That(result.Rejections.Select(r => r.Reason), Has.All.EqualTo("picture:no-big-picture-available"));
        }

        [Test]
        public void ABigLevel_StoresTheIconsLook_AndPassesEveryCheck()
        {
            LevelDefinition level = BigLevel;
            Assert.That(BigPicture.Width * BigPicture.Height, Is.EqualTo(616));
            Assert.That(level.BoardLook, Is.EqualTo(BoardLook.Icons));
            Assert.That(level.Overlays.Sum(o => o.LayersBelow.Count), Is.InRange(1, BandGuidelines.MaxHiddenLayersOnIcons));
            Assert.That(level.Overlays.Any(o => o.Mystery) || level.Pods.Any(p => p.Mystery), Is.False, "no mystery on an icons board");

            var validator = new CatalogValidator(Library.Append(BigPicture), UnlockRoadmap.Default, Pairs, new SolveOptions(200_000));
            CatalogReport report = validator.Validate(new[] { level });

            Assert.That(report.Issues.Where(i => i.IsError).Select(i => $"{i.Check}: {i.Message}"), Is.Empty);
            Assert.That(report.Records[600].Checks, Does.Contain("player-info-fair").And.Contain("band").And.Contain("board"));
            Assert.That(report.Records[600].PlayerInfoFair, Is.True);

            // On it the player sees no next layer: the view hides it.
            LevelSession session = LevelSession.Load(level, BigPicture, new SessionOptions(1, 20000));
            Assert.That(Enumerable.Range(0, 616).Select(i => session.View.Cell(i).Next), Has.All.Null);
        }

        [Test]
        public void ABoardLookThatDisagreesWithTheCellCount_FailsTheBoardCheck()
        {
            LevelDefinition curated = ContentStore.LoadLevels(Path.Combine(RepoRoot, "content", "curated")).Single(l => l.Level.LevelNumber == 3).Level;
            var validator = new CatalogValidator(Library.Append(BigPicture), UnlockRoadmap.Default, Pairs, new SolveOptions(20000));

            List<LevelIssue> icons = validator.Validate(new[] { curated with { BoardLook = BoardLook.Icons } }).Issues;
            List<LevelIssue> peek = validator.Validate(new[] { BigLevel with { BoardLook = BoardLook.Peek } }).Issues;
            List<LevelIssue> unstated = validator.Validate(new[] { BigLevel with { BoardLook = null } }).Issues;

            Assert.That(icons.Any(i => i.IsError && i.Check == "board" && i.Message.Contains("boardLook is icons")), Is.True, "a 144-cell board peeks");
            Assert.That(peek.Any(i => i.IsError && i.Check == "board" && i.Message.Contains("boardLook is peek")), Is.True, "a 616-cell board shows icons");
            Assert.That(unstated.Any(i => i.IsError && i.Check == "board" && i.Message.Contains("not stated")), Is.True, "absent means peek");
            Assert.That(validator.Validate(new[] { curated }).Issues.Where(i => i.IsError), Is.Empty, "the curated levels state no look and peek");
        }

        [Test]
        public void HiddenLayersThatForceABlindGuess_FailPlayerInfoFair()
        {
            // A Super Hard level (critical buffer pressure) shown with its next layers it is fine; hiding them, the player
            // can no longer avoid committing a pod whose tiles lie under the wrong cells.
            LevelDefinition level = DefinitionJson.Read(Fixture("icons-blind-guess.level.json"));
            BasePicture picture = BasePictureJson.Read(Fixture("icons-blind-guess.picture.json"));
            Assert.That(level.BoardLook, Is.EqualTo(BoardLook.Icons));

            FairnessResult hidden = FairnessChecker.Check(level, picture, new SessionOptions(1, 20000), 20000, BandGuidelines.MaxLayersBelow(level.LevelNumber), BandGuidelines.MaxHiddenLayersOnIcons);
            FairnessResult shown = FairnessChecker.Check(level with { BoardLook = BoardLook.Peek }, picture, new SessionOptions(1, 20000), 20000, BandGuidelines.MaxLayersBelow(level.LevelNumber), BandGuidelines.MaxHiddenLayersOnIcons);

            Assert.That(hidden.Status, Is.EqualTo(FairnessStatus.Unfair), hidden.Detail);
            Assert.That(hidden.PlayerInfoFair, Is.False);
            Assert.That(hidden.Detail, Does.Contain("loses"));
            Assert.That(shown.Status, Is.EqualTo(FairnessStatus.Fair));
            Assert.That(shown.PlayerInfoFair, Is.Null, "a peek board hides nothing the check judges");

            var validator = new CatalogValidator(new[] { picture }, UnlockRoadmap.Default, Pairs, new SolveOptions(200_000)) { CheckBandGuidelines = false };
            Assert.That(validator.Validate(new[] { level }).Issues.Any(i => i.IsError && i.Check == "player-info-fair" && i.Message.Contains("FR-036")), Is.True);
        }

        [Test]
        public void BandsWithBigLevels_HaveBigLevelThresholds()
        {
            string json = File.ReadAllText(Path.Combine(RepoRoot, "content", "profiles", "difficulty-thresholds.json"));
            foreach (string file in Directory.GetFiles(Path.Combine(RepoRoot, "content", "profiles"), "band-*.json"))
            {
                GenerationProfile profile = ProfileLoader.ReadFile(file);
                bool hasBig = Enumerable.Range(profile.LevelRange.Min, Math.Min(profile.LevelRange.Max, 5000) - profile.LevelRange.Min + 1).Any(BandGuidelines.IsBigLevel);
                DifficultyThresholds? big = ProfileLoader.ReadBigThresholds(json, profile.BandId);
                Assert.That(big != null, Is.EqualTo(hasBig), profile.BandId);
                if (big != null)
                {
                    DifficultyThresholds regular = ProfileLoader.ReadThresholds(json, profile.BandId);
                    Assert.That(big.HardMin, Is.GreaterThan(regular.HardMin), profile.BandId);
                    Assert.That(ProfileLoader.ReadThresholdsFor(json, profile.BandId, 550).HardMin, Is.EqualTo(big.HardMin).Or.EqualTo(regular.HardMin));
                    Assert.That(ProfileLoader.ReadThresholdsFor(json, profile.BandId, 551).HardMin, Is.EqualTo(regular.HardMin));
                }
            }
        }
    }
}
