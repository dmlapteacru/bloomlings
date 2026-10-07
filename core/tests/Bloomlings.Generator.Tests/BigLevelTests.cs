using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
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
    /// The owner's boards (spec 001 FR-008 and FR-036 as amended, research R8b): from L11 every size from 14×16 to 22×28
    /// about evenly (2026-10-07; regular boards of 224–288 cells and big milestone levels from L525 before), and the board
    /// look stored in the level data, the next layer hidden on a big board's icons board. The fixtures are a big level the generator made on a
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
        public void FromL11_EveryClassMayFallOnAnyBoard()
        {
            // The owner, 2026-10-07: every size from L11, and Hard and Super Hard on big boards too, with thresholds that
            // grow with the board. The schedule no longer keeps milestone levels Normal; they are still never Super Hard.
            Assert.That(BandGuidelines.IsBig(288), Is.False);
            Assert.That(BandGuidelines.IsBig(289), Is.True);
            var schedule = new DifficultySchedule(0xB100B100UL);
            var milestones = Enumerable.Range(1, 200).Select(k => k * 25).ToList();
            Assert.That(milestones.Any(l => schedule.ClassFor(l) == DifficultyClass.Hard), Is.True, "a milestone may be Hard");
            Assert.That(milestones.All(l => schedule.ClassFor(l) != DifficultyClass.SuperHard), Is.True, "never Super Hard (FR-059)");
            for (int start = 11; start + 99 <= 5000; start += 7)
            {
                int hard = Enumerable.Range(start, 100).Count(l => schedule.ClassFor(l) == DifficultyClass.Hard);
                Assert.That(hard, Is.InRange(15, 25), $"L{start}–{start + 99}");
            }
        }

        [Test]
        public void Boards_FromL11_AreEverySizeFrom14x16To22x28()
        {
            Assert.That(BandGuidelines.Board(10).Allows(12, 12), Is.True, "the curated onboarding stays 11–12×12");
            Assert.That(BandGuidelines.Board(10).Allows(14, 16), Is.False);
            Assert.That(BandGuidelines.Board(11).Allows(12, 12), Is.False, "from L11 at least 224 cells");
            Assert.That(BandGuidelines.Board(11).Allows(14, 16), Is.True);
            Assert.That(BandGuidelines.Board(11).Allows(22, 28), Is.True, "the biggest board from L11");
            Assert.That(BandGuidelines.Board(300).Allows(17, 18), Is.True);
            Assert.That(BandGuidelines.Board(5000).Allows(16, 18), Is.True);
            Assert.That(BandGuidelines.Board(5000).Allows(23, 28), Is.False, "22 wide at most");

            Assert.That(BoardLooks.For(14, 16), Is.EqualTo(BoardLook.Peek));
            Assert.That(BoardLooks.For(16, 18), Is.EqualTo(BoardLook.Peek));
            Assert.That(BoardLooks.For(17, 17), Is.EqualTo(BoardLook.Icons));
            Assert.That(BoardLooks.For(22, 28), Is.EqualTo(BoardLook.Icons));
        }

        [Test]
        public void ThePicturePicker_DrawsEverySizeAboutEvenly()
        {
            // The library holds more regular pictures than big ones; a size is drawn first, so each comes about as often.
            var picker = new PicturePicker(Library);
            var history = new Dictionary<int, LevelDefinition>();
            IReadOnlyList<BasePicture> candidates = picker.Candidates(LongRun, 551, history, BandGuidelines.Board(551));
            int sizes = candidates.Select(p => (p.Width, p.Height)).Distinct().Count();
            Assert.That(sizes, Is.EqualTo(17), "9 regular sizes and 8 big ones");

            var counts = new Dictionary<(int, int), int>();
            const int Draws = 3400;
            for (int k = 0; k < Draws; k++)
            {
                var rng = new Xoshiro256StarStar((ulong)k * 0x9E3779B97F4A7C15UL + 1);
                BasePicture picture = picker.Pick(LongRun, 551, history, ref rng, BandGuidelines.Board(551))!;
                counts[(picture.Width, picture.Height)] = counts.TryGetValue((picture.Width, picture.Height), out int n) ? n + 1 : 1;
            }

            Assert.That(counts.Count, Is.EqualTo(17));
            Assert.That(counts.Values, Has.All.InRange(Draws / 17 / 2, Draws / 17 * 3 / 2));
            int big = counts.Where(c => c.Key.Item1 * c.Key.Item2 > BoardLooks.MaxPeekCells).Sum(c => c.Value);
            Assert.That(big / (double)Draws, Is.InRange(0.38, 0.56), "8 sizes in 17 are big");
        }

        [Test]
        public void ClassThresholds_GrowWithTheBoard_FromTheBandsOwnToItsBigOnes()
        {
            var regular = DifficultyThresholds.Default with { HardMin = 2400, SuperHardMin = 3200 };
            var big = DifficultyThresholds.Default with { HardMin = 3900, SuperHardMin = 4700 };
            Assert.That(BandGuidelines.ThresholdsFor(regular, big, 224), Is.EqualTo(regular));
            Assert.That(BandGuidelines.ThresholdsFor(regular, big, 288), Is.EqualTo(regular));
            DifficultyThresholds middle = BandGuidelines.ThresholdsFor(regular, big, 452);
            Assert.That((middle.HardMin, middle.SuperHardMin), Is.EqualTo((3150, 3950)), "halfway from 288 to 616 cells");
            DifficultyThresholds biggest = BandGuidelines.ThresholdsFor(regular, big, 616);
            Assert.That((biggest.HardMin, biggest.SuperHardMin), Is.EqualTo((3900, 4700)));
            Assert.That(BandGuidelines.ThresholdsFor(regular, null, 616), Is.EqualTo(regular), "a band without big thresholds keeps its own");
        }

        /// <summary>
        /// The owner, 2026-10-07: the pictures with a lime role (Vine, from L45) or a red one (Berry, from L200) pass the
        /// automated approval, and the picker keeps each out of the levels before its variant joins the pool, so those
        /// levels are the same as without them; with no expansions (the Daily pool) they stay out.
        /// </summary>
        [Test]
        public void ExpansionPictures_AreApproved_AndKeptOutUntilTheirVariantJoins()
        {
            List<BasePicture> library = Library;
            List<BasePicture> lime = library.Where(p => p.Roles.Any(r => r.ColorGroup == Core.Variants.ColorGroup.Lime)).ToList();
            List<BasePicture> red = library.Where(p => p.Roles.Any(r => r.ColorGroup == Core.Variants.ColorGroup.Red)).ToList();
            Assert.That(lime, Is.Not.Empty, "the library has lime roles for Vine");
            Assert.That(red, Is.Not.Empty, "and red roles for Berry");
            Assert.That(lime.Concat(red).Select(p => p.Review.Status), Has.All.EqualTo(ReviewStatus.Approved));
            Assert.That(lime.Concat(red).SelectMany(Pipeline.Pictures.PictureChecks.Problems), Is.Empty, "the automated checks accept the expansion groups");

            VariantPool pool = VariantPool.Default;
            Assert.That(PicturePicker.Drawable(lime[0], pool.ExpansionsAt(44, UnlockRoadmap.Default)), Is.False, "no Vine before L45");
            Assert.That(PicturePicker.Drawable(lime[0], pool.ExpansionsAt(45, UnlockRoadmap.Default)), Is.True);
            BasePicture redOnly = red.First(p => p.Roles.All(r => r.ColorGroup != Core.Variants.ColorGroup.Lime));
            Assert.That(PicturePicker.Drawable(redOnly, pool.ExpansionsAt(199, UnlockRoadmap.Default)), Is.False, "no Berry before L200");
            Assert.That(PicturePicker.Drawable(redOnly, pool.ExpansionsAt(200, UnlockRoadmap.Default)), Is.True);
            Assert.That(PicturePicker.Drawable(lime[0], null), Is.False, "the Daily pool keeps them out");

            GenerationProfile early = ProfileLoader.ReadFile(Path.Combine(RepoRoot, "content", "profiles", "band-0026-0050.json"));
            var picker = new PicturePicker(library);
            var history = new Dictionary<int, LevelDefinition>();
            IReadOnlyList<BasePicture> at44 = picker.Candidates(early, 44, history, BandGuidelines.Board(44), pool.ExpansionsAt(44, UnlockRoadmap.Default));
            IReadOnlyList<BasePicture> at45 = picker.Candidates(early, 45, history, BandGuidelines.Board(45), pool.ExpansionsAt(45, UnlockRoadmap.Default));
            Assert.That(at44.Select(p => p.Id), Is.EqualTo(new PicturePicker(library.Except(lime).Except(red)).Candidates(early, 44, history, BandGuidelines.Board(44)).Select(p => p.Id)), "L44's candidates as without the new pictures");
            Assert.That(at45.Any(p => lime.Contains(p)), Is.True, "lime pictures from L45");
            Assert.That(at45.Any(p => red.Contains(p)), Is.False, "red ones still out");
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
        public void EveryBandFromL11_HasBigBoardThresholds()
        {
            string json = File.ReadAllText(Path.Combine(RepoRoot, "content", "profiles", "difficulty-thresholds.json"));
            foreach (string file in Directory.GetFiles(Path.Combine(RepoRoot, "content", "profiles"), "band-*.json"))
            {
                GenerationProfile profile = ProfileLoader.ReadFile(file);
                DifficultyThresholds regular = ProfileLoader.ReadThresholds(json, profile.BandId);
                DifficultyThresholds? big = ProfileLoader.ReadBigThresholds(json, profile.BandId);
                Assert.That(big, Is.Not.Null, profile.BandId + ": every band has big boards since 2026-10-07");
                Assert.That(big!.HardMin, Is.GreaterThan(regular.HardMin), profile.BandId);
                Assert.That(big.SuperHardMin, Is.GreaterThan(big.HardMin), profile.BandId);
                Assert.That(ProfileLoader.ReadThresholdsFor(json, profile.BandId, 288).HardMin, Is.EqualTo(regular.HardMin), profile.BandId);
                Assert.That(ProfileLoader.ReadThresholdsFor(json, profile.BandId, 616).HardMin, Is.EqualTo(big.HardMin), profile.BandId);
            }
        }
    }
}
