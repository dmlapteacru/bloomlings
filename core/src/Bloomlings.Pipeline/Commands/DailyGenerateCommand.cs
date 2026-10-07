using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary>
    /// <c>daily generate</c> (R19): builds the Daily Challenge pool with the <c>daily</c> profile. Pool entry i is
    /// stored as level number i of the batch; the daily pack keeps the index. Every entry shows one of the Daily
    /// Challenge's own pictures (<see cref="PicturePicker.DailyTheme"/>), each once, as the <see cref="DailyPlan"/>
    /// spreads them, takes its class from the week (<see cref="DailyPlan.WeeklyClass"/>, or one class for all) and plays
    /// with the unlocks of the challenge's own unlock level (the owner, 2026-10-07).
    /// </summary>
    public static class DailyGenerateCommand
    {
        /// <summary>The roadmap row that unlocks the Daily Challenge, whose unlocks every pool entry plays with.</summary>
        public const string UnlockId = "system.daily_challenge";

        /// <summary>The rounds of spare pictures an entry without an accepted candidate gets.</summary>
        public const int SpareRounds = 3;

        public static Command Create()
        {
            var daily = new Command("daily", "Daily Challenge pool.");
            var generate = new Command("generate", "Build the Daily Challenge pool with the daily profile (R19).");
            Option<int> count = Cli.Int("--count", 365, "Pool size.");
            Option<long> seed = new Option<long>("--seed") { Description = "Generation seed.", Required = true };
            Option<string> profile = Cli.Path("--profile", "content/profiles/daily.json", "Daily profile.");
            Option<string> outDir = Cli.Path("--out", "content/work/daily", "Output folder.");
            Option<string> lib = Cli.Path("--lib", "content/pictures/lib", "Picture library.");
            Option<string> thresholds = Cli.Path("--thresholds", "content/profiles/difficulty-thresholds.json", "Difficulty thresholds.");
            Option<string> pairs = Cli.Path("--pairs", "content/readability/approved-pairs.json", "Approved readability pairs.");
            var allowDraft = new Option<bool>("--allow-draft") { Description = "Development preview only: use unapproved pictures." };
            var forcedClass = new Option<string>("--class") { Description = "The entries' difficulty classes: weekly (the default: four Normal days, Hard on Wednesday and Saturday, Super Hard on Sunday), or normal, hard or super_hard for every entry.", DefaultValueFactory = _ => "weekly" };
            var jobs = new Option<int>("--jobs") { Description = "Threads, as for generate.", DefaultValueFactory = _ => 1 };
            var segments = new Option<int>("--segments") { Description = "Contiguous parts of the pool generated in parallel, as for generate (default: --jobs). The pool depends on the segments, never on --jobs.", DefaultValueFactory = _ => 0 };
            foreach (Option option in new Option[] { count, seed, profile, outDir, lib, thresholds, pairs, allowDraft, forcedClass, jobs, segments })
            {
                generate.Options.Add(option);
            }

            generate.SetAction(parse => Cli.Run(parse, report =>
            {
                GenerationProfile band = ProfileLoader.ReadFile(parse.GetValue(profile)!);
                ApprovedPairs approved = ContentStore.LoadPairs(parse.GetValue(pairs)!) ?? throw new IOException("approved-pairs.json is missing.");
                var library = ContentStore.LoadLibrary(parse.GetValue(lib)!);
                var tuning = ProfileLoader.ReadThresholds(File.ReadAllText(parse.GetValue(thresholds)!), band.BandId);
                bool drafts = parse.GetValue(allowDraft);
                int size = parse.GetValue(count);
                ulong baseSeed = (ulong)parse.GetValue(seed);
                Func<int, DifficultyClass> classOf = parse.GetValue(forcedClass) switch
                {
                    "weekly" or null or "" => DailyPlan.WeeklyClass,
                    "normal" => _ => DifficultyClass.Normal,
                    "hard" => _ => DifficultyClass.Hard,
                    "super_hard" => _ => DifficultyClass.SuperHard,
                    var other => throw new ArgumentException($"--class '{other}': use weekly, normal, hard or super_hard."),
                };

                // The plan: the daily pictures the profile can show (its size, themes and structure targets), each entry its own.
                IReadOnlyList<BasePicture> pictures = new PicturePicker(library, drafts).Candidates(band, 1, new Dictionary<int, LevelDefinition>());
                DailyPlan plan = DailyPlan.Build(pictures, size, baseSeed);
                int rules = UnlockRoadmap.Default.LevelOf(UnlockId) ?? throw new InvalidOperationException($"the roadmap has no {UnlockId} row.");
                var progressLock = new object();
                LevelGenerator NewGenerator() => new LevelGenerator(
                    band,
                    new PicturePicker(library, drafts, plan.PictureOf),
                    tuning,
                    approved.IsApproved,
                    new DifficultySchedule(baseSeed))
                {
                    // Pool indexes are not Level N: the daily profile's own ranges apply, not the band guidelines, and the
                    // unlocks are those of the challenge's unlock level.
                    UseBandGuidelines = false,
                    ClassOf = classOf,
                    RulesLevel = rules,
                    Progress = (entry, accepted, candidates) =>
                    {
                        lock (progressLock)
                        {
                            Console.Error.WriteLine(accepted == null
                                ? $"  entry {entry} ({classOf(entry)}, {plan.PictureOf(entry)}): failed after {candidates} candidates"
                                : $"  entry {entry}: {accepted.Definition.Difficulty.Class} ({accepted.Definition.Difficulty.Score}), {accepted.Definition.Picture.Id}, {accepted.Definition.Pods.Count} pods, candidate {candidates}");
                        }
                    },
                };
                var history = new SortedDictionary<int, LevelDefinition>();
                int threads = Math.Max(1, parse.GetValue(jobs));
                int parts = parse.GetValue(segments) > 0 ? parse.GetValue(segments) : threads;
                (GenerationResult result, int repairs) = GenerateCommand.GenerateRange(NewGenerator, 1, size, baseSeed, history, new HashSet<int>(), parts, threads);

                // An entry whose picture made no level tries the spare pictures, in plan order, each with its own seed.
                var accepted = result.Accepted.ToDictionary(l => l.Definition.LevelNumber);
                var failed = new SortedSet<int>(result.Failed);
                var replaced = new SortedDictionary<int, string>();
                LevelGenerator spareGenerator = NewGenerator();
                for (int round = 0; round < SpareRounds && failed.Count > 0; round++)
                {
                    foreach (int entry in failed.ToList())
                    {
                        if (plan.Replace(entry) is not string spare)
                        {
                            continue;
                        }

                        replaced[entry] = spare;
                        GenerationResult again = spareGenerator.Regenerate(new[] { entry }, baseSeed ^ (0x5BA2EUL * (ulong)(round + 1)), history);
                        result.Rejections.AddRange(again.Rejections);
                        foreach (GeneratedLevel level in again.Accepted)
                        {
                            accepted[level.Definition.LevelNumber] = level;
                            failed.Remove(level.Definition.LevelNumber);
                        }
                    }
                }

                foreach (GeneratedLevel level in accepted.Values.OrderBy(l => l.Definition.LevelNumber))
                {
                    ContentStore.WriteLevel(parse.GetValue(outDir)!, level.Definition, level.Record);
                }

                // The rejected candidates, as generate writes them, for calibrating the profile and the thresholds.
                var rejections = new JArray(result.Rejections.Select(r => new JObject { ["level"] = r.Level, ["attempt"] = r.Attempt, ["reason"] = r.Reason }).ToArray());
                Directory.CreateDirectory(parse.GetValue(outDir)!);
                File.WriteAllText(Path.Combine(parse.GetValue(outDir)!, "rejections.json"), CanonicalJson.Write(new JObject { ["rejections"] = rejections }, indented: true));

                // The publish gate's daily checks (CatalogCommands.GateIssues), so a pool that would not publish fails here.
                var validator = new CatalogValidator(library, UnlockRoadmap.Default, approved, new SolveOptions(band.SolverNodeBudget)) { CheckBandGuidelines = false, CheckSequences = false, DailyPool = true, RulesLevel = rules };
                List<LevelIssue> errors = validator.Validate(accepted.Values.Select(l => l.Definition).ToList()).Issues.Where(i => i.IsError).ToList();
                report["validationErrors"] = new JArray(errors.Select(i => new JObject { ["level"] = i.Level, ["check"] = i.Check, ["message"] = i.Message }).ToArray());

                report["pictures"] = pictures.Count;
                report["segments"] = GenerateCommand.Segments(1, size, parts).Count;
                report["seamRepairs"] = repairs;
                report["spares"] = JObject.FromObject(replaced.ToDictionary(e => e.Key.ToString(CultureInfo.InvariantCulture), e => e.Value));
                report["sparesLeft"] = plan.Spares.Count;
                report["classes"] = JObject.FromObject(accepted.Values.GroupBy(l => l.Definition.Difficulty.Class.ToString()).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count()));
                report["accepted"] = accepted.Count;
                report["failed"] = new JArray(failed);
                Cli.Say(parse, $"daily generate: {accepted.Count} of {size} pool entries on {pictures.Count} daily pictures, {replaced.Count} on a spare, {failed.Count} failed, {errors.Count} validation errors.");
                return failed.Count == 0 && errors.Count == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            daily.Subcommands.Add(generate);
            return daily;
        }
    }
}
