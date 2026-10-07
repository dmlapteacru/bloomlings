using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    /// <summary>
    /// The Daily Challenge pool (R19, FR-064; the owner, 2026-10-07): its own pictures, each once and a subject only every
    /// 60 days, on the biggest board, Normal, Hard or Super Hard by the week.
    /// </summary>
    public class DailyPoolTests
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

        private static List<string> Ids(int subjects, int each) =>
            Enumerable.Range(1, subjects).SelectMany(s => Enumerable.Range(1, each).Select(k => $"subject{s}_{k:00}")).ToList();

        [Test]
        public void TheWeek_HasFourNormalDays_HardOnWednesdayAndSaturday_AndSuperHardOnSunday()
        {
            // Entry 1 is 2026-01-01, a Thursday.
            DifficultyClass[] firstWeek = Enumerable.Range(1, 7).Select(DailyPlan.WeeklyClass).ToArray();
            Assert.That(firstWeek, Is.EqualTo(new[]
            {
                DifficultyClass.Normal, DifficultyClass.Normal, DifficultyClass.Hard, DifficultyClass.SuperHard,
                DifficultyClass.Normal, DifficultyClass.Normal, DifficultyClass.Hard,
            }));
            Assert.That(new DateTime(2026, 1, 1).AddDays(3).DayOfWeek, Is.EqualTo(DayOfWeek.Sunday), "entry 4 is a Sunday");

            var year = Enumerable.Range(1, 365).Select(DailyPlan.WeeklyClass).ToList();
            Assert.That(year.Count(c => c == DifficultyClass.Normal), Is.EqualTo(209));
            Assert.That(year.Count(c => c == DifficultyClass.Hard), Is.EqualTo(104));
            Assert.That(year.Count(c => c == DifficultyClass.SuperHard), Is.EqualTo(52));
            for (int e = 1; e < 365; e++)
            {
                if (year[e - 1] == DifficultyClass.SuperHard)
                {
                    Assert.That(year[e], Is.EqualTo(DifficultyClass.Normal), $"entry {e + 1}: a Normal day after a Super Hard one");
                }
            }
        }

        [Test]
        public void ASubject_IsThePictureIdWithoutItsNumber()
        {
            Assert.That(DailyPlan.SubjectOf("dragon_02"), Is.EqualTo("dragon"));
            Assert.That(DailyPlan.SubjectOf("moon_and_stars_10"), Is.EqualTo("moon_and_stars"));
            Assert.That(DailyPlan.SubjectOf("ice_cream"), Is.EqualTo("ice_cream"));
            Assert.That(DailyPlan.SubjectOf("plain"), Is.EqualTo("plain"));
        }

        [Test]
        public void ThePlan_ShowsEveryPictureOnce_AndASubjectOnlyEverySixtyEntries()
        {
            List<string> ids = Ids(128, 3);
            DailyPlan plan = DailyPlan.Build(ids, 365, 7);
            var entries = Enumerable.Range(1, 365).Select(e => plan.PictureOf(e)!).ToList();

            Assert.That(entries.Distinct().Count(), Is.EqualTo(365), "each picture once");
            Assert.That(plan.Spares.Count, Is.EqualTo(384 - 365));
            Assert.That(entries.Concat(plan.Spares), Is.EquivalentTo(ids));
            Assert.That(MinimumGap(entries), Is.GreaterThanOrEqualTo(DailyPlan.SubjectWindow));
            Assert.That(plan.PictureOf(0), Is.Null);
            Assert.That(plan.PictureOf(366), Is.Null);

            // The same pictures and seed give the same plan, in any order of the pictures; another seed another one.
            DailyPlan again = DailyPlan.Build(Enumerable.Reverse(ids), 365, 7);
            Assert.That(Enumerable.Range(1, 365).Select(again.PictureOf), Is.EqualTo(entries));
            DailyPlan other = DailyPlan.Build(ids, 365, 8);
            Assert.That(Enumerable.Range(1, 365).Select(other.PictureOf), Is.Not.EqualTo(entries));
        }

        [Test]
        public void ASpare_TakesAFailedEntry_OnlyWhereItsSubjectKeepsTheWindow()
        {
            DailyPlan plan = DailyPlan.Build(Ids(128, 3), 365, 3);
            int spares = plan.Spares.Count;
            string old = plan.PictureOf(200)!;
            string? spare = plan.Replace(200);

            Assert.That(spare, Is.Not.Null);
            Assert.That(plan.PictureOf(200), Is.EqualTo(spare));
            Assert.That(plan.Spares, Has.Count.EqualTo(spares - 1).And.No.Member(spare));
            Assert.That(Enumerable.Range(1, 365).Select(plan.PictureOf), Has.No.Member(old), "the failed picture is used no more");
            Assert.That(MinimumGap(Enumerable.Range(1, 365).Select(e => plan.PictureOf(e)!).ToList()), Is.GreaterThanOrEqualTo(DailyPlan.SubjectWindow));

            // Too few subjects for the window: a pool of 61 days needs 61 subjects, or two pictures 60 days apart.
            Assert.Throws<ArgumentException>(() => DailyPlan.Build(Ids(30, 3), 61, 1));
            Assert.That(DailyPlan.Build(Ids(60, 2), 61, 1).PictureOf(61), Is.Not.Null);
        }

        [Test]
        public void DailyPictures_AreTheChallengesOwn_AndTheLevelsNeverShowThem()
        {
            List<BasePicture> library = ContentStore.LoadLibrary(Path.Combine(RepoRoot, "content", "pictures", "lib"));
            BasePicture big = library.First(p => p.Width == 22 && p.Height == 28 && !PicturePicker.IsDaily(p));
            BasePicture daily = big with { Id = "trial_daily_01", Tags = big.Tags with { Themes = big.Tags.Themes.Concat(new[] { PicturePicker.DailyTheme }).ToList() } };
            var picker = new PicturePicker(library.Concat(new[] { daily }));
            var history = new Dictionary<int, LevelDefinition>();

            GenerationProfile dailyProfile = ProfileLoader.ReadFile(Path.Combine(RepoRoot, "content", "profiles", "daily.json"));
            GenerationProfile band = ProfileLoader.ReadFile(Path.Combine(RepoRoot, "content", "profiles", "band-0501-1000.json"));
            Assert.That(dailyProfile.PictureThemes, Is.EqualTo(new[] { PicturePicker.DailyTheme }));

            IReadOnlyList<BasePicture> forDaily = picker.Candidates(dailyProfile, 1, history);
            Assert.That(forDaily.Select(p => p.Id), Has.Member("trial_daily_01"));
            Assert.That(forDaily.All(PicturePicker.IsDaily), Is.True, "the daily profile takes the daily pictures only");
            Assert.That(picker.Candidates(band, 550, history, BandGuidelines.Board(550)).Select(p => p.Id), Has.No.Member("trial_daily_01").And.Member(big.Id), "a big level never takes one");

            // With a plan, an entry takes its planned picture alone.
            var planned = new PicturePicker(library.Concat(new[] { daily }), plan: e => e == 1 ? "trial_daily_01" : null);
            Assert.That(planned.Candidates(dailyProfile, 1, history).Select(p => p.Id), Is.EqualTo(new[] { "trial_daily_01" }));
            Assert.That(planned.Candidates(dailyProfile, 2, history), Is.Empty);
        }

        [Test]
        public void TheValidator_RefusesADailyPictureInTheCatalog()
        {
            List<BasePicture> library = ContentStore.LoadLibrary(Path.Combine(RepoRoot, "content", "pictures", "lib"));
            LevelDefinition three = ContentStore.LoadLevels(Path.Combine(RepoRoot, "content", "curated")).Select(l => l.Level).Single(l => l.LevelNumber == 3);
            BasePicture picture = library.Single(p => p.Id == three.Picture.Id && p.Version == three.Picture.Version);
            BasePicture daily = picture with { Tags = picture.Tags with { Themes = picture.Tags.Themes.Concat(new[] { PicturePicker.DailyTheme }).ToList() } };
            var pairs = ContentStore.LoadPairs(Path.Combine(RepoRoot, "content", "readability", "approved-pairs.json"));
            var validator = new CatalogValidator(library.Where(p => p != picture).Concat(new[] { daily }), UnlockRoadmap.Default, pairs, new SolveOptions(20000));

            Assert.That(validator.Validate(new[] { three }).Issues.Any(i => i.IsError && i.Check == "picture-pool"), Is.True);
            validator.DailyPool = true;
            validator.CheckBandGuidelines = false;
            validator.CheckSequences = false;
            Assert.That(validator.Validate(new[] { three with { LevelNumber = 1 } }).Issues.Where(i => i.IsError), Is.Empty);
        }

        [Test]
        public void TheDailyPool_IsAYearOfNewPicturesOnTheBiggestBoard()
        {
            // The owner, 2026-10-07: every day a new picture of a subject the levels never show, on the biggest board, with
            // small cells, Normal, Hard or Super Hard by the week; it plays with the unlocks of its own unlock, L50.
            GenerationProfile profile = ProfileLoader.ReadFile(Path.Combine(RepoRoot, "content", "profiles", "daily.json"));
            Assert.That((profile.BoardWidth.Min, profile.BoardWidth.Max, profile.BoardHeight.Min, profile.BoardHeight.Max), Is.EqualTo((22, 22, 28, 28)));
            Assert.That(BandGuidelines.BigBoard.Allows(22, 28), Is.True);

            Dictionary<string, BasePicture> pictures = ContentStore.LoadLibrary(Path.Combine(RepoRoot, "content", "pictures", "lib")).ToDictionary(p => p.Id + "@" + p.Version);
            if (!Directory.Exists(Path.Combine(RepoRoot, "content", "daily", "levels")))
            {
                Assert.Ignore("The Daily Challenge pool is being built on its new pictures (T185).");
            }

            List<LevelDefinition> pool = ContentStore.LoadLevels(Path.Combine(RepoRoot, "content", "daily")).Select(l => l.Level).OrderBy(l => l.LevelNumber).ToList();
            Assert.That(pool.Select(l => l.LevelNumber), Is.EqualTo(Enumerable.Range(1, 365)), "one puzzle for each day of a year");
            Assert.That(pool.Select(l => l.Picture.Id).Distinct().Count(), Is.EqualTo(365), "every day a new picture");
            Assert.That(MinimumGap(pool.Select(l => l.Picture.Id).ToList()), Is.GreaterThanOrEqualTo(DailyPlan.SubjectWindow));

            int unlock = UnlockRoadmap.Default.LevelOf("system.daily_challenge")!.Value;
            foreach (LevelDefinition level in pool)
            {
                BasePicture picture = pictures[level.Picture.Id + "@" + level.Picture.Version];
                Assert.That(PicturePicker.IsDaily(picture), Is.True, $"entry {level.LevelNumber}: {picture.Id} is a daily picture");
                Assert.That((picture.Width, picture.Height), Is.EqualTo((22, 28)), $"entry {level.LevelNumber}: {picture.Id}");
                Assert.That(level.BoardLook, Is.EqualTo(BoardLook.Icons), $"entry {level.LevelNumber}");
                Assert.That(level.Difficulty.Class, Is.EqualTo(DailyPlan.WeeklyClass(level.LevelNumber)), $"entry {level.LevelNumber}");
                foreach (string mechanic in level.Mechanics)
                {
                    Assert.That(profile.Allows(mechanic), Is.True, $"entry {level.LevelNumber}: {mechanic}");
                    Assert.That(UnlockRoadmap.Default.LevelOf("mechanic." + mechanic), Is.LessThanOrEqualTo(unlock), $"entry {level.LevelNumber}: {mechanic} is unlocked by L{unlock}");
                }
            }

            // And no level of the catalog shows one of them.
            string catalog = Path.Combine(RepoRoot, "content", "catalog");
            if (Directory.Exists(catalog))
            {
                foreach (LevelDefinition level in ContentStore.LoadLevels(catalog).Select(l => l.Level))
                {
                    Assert.That(PicturePicker.IsDaily(pictures[level.Picture.Id + "@" + level.Picture.Version]), Is.False, $"L{level.LevelNumber}: {level.Picture.Id}");
                }
            }
        }

        private static int MinimumGap(IReadOnlyList<string> entries)
        {
            int gap = int.MaxValue;
            var last = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int e = 0; e < entries.Count; e++)
            {
                string subject = DailyPlan.SubjectOf(entries[e]);
                if (last.TryGetValue(subject, out int at))
                {
                    gap = Math.Min(gap, e - at);
                }

                last[subject] = e;
            }

            return gap;
        }
    }
}
