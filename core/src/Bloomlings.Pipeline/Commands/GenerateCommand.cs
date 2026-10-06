using System;
using System.Collections.Generic;
using System.CommandLine;
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
    /// <c>generate</c> (R9, T089): generates a level range from a profile into a batch folder, validates the accepted
    /// levels with <see cref="CatalogValidator"/> against the existing catalog, and records every rejection.
    /// </summary>
    public static class GenerateCommand
    {
        public static Command Create()
        {
            var command = new Command("generate", "Generate candidates for a level range from a profile (R9).");
            Option<string> profile = Cli.Required("--profile", "Generation profile, e.g. content/profiles/band-0011-0025.json.");
            Option<string> levels = Cli.Required("--levels", "Level range, e.g. 11-25.");
            Option<long> seed = new Option<long>("--seed") { Description = "Generation seed.", Required = true };
            Option<string> outDir = Cli.Required("--out", "Batch folder, e.g. content/work/band-0011.");
            Option<string> lib = Cli.Path("--lib", "content/pictures/lib", "Picture library.");
            Option<string> catalog = Cli.Path("--catalog", "content/catalog", "Existing catalog (history for the FR-083 windows).");
            Option<string> curated = Cli.Path("--curated", "content/curated", "Curated levels (history).");
            Option<string> thresholds = Cli.Path("--thresholds", "content/profiles/difficulty-thresholds.json", "Difficulty weights and thresholds.");
            Option<string> pairs = Cli.Path("--pairs", "content/readability/approved-pairs.json", "Approved readability pairs.");
            var extraHistory = new Option<string[]>("--history") { Description = "More batch folders to treat as earlier levels (e.g. an unpublished preview of the previous band).", AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => Array.Empty<string>() };
            var keep = new Option<string[]>("--keep") { Description = "Folders of fixed levels (showcase, curated): kept in the history and not generated.", AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => new[] { "content/showcase" } };
            var forced = new Option<string>("--mechanics") { Description = "Showcase mode: exactly these mechanics, comma-separated (e.g. locked_pod)." };
            var forcedClass = new Option<string>("--class") { Description = "A fixed difficulty class: normal, hard or super_hard (showcases are normal)." };
            Option<string> level8 = Cli.Level8();
            var allowDraft = new Option<bool>("--allow-draft") { Description = "Development preview only: use pictures that are not approved yet. Such levels fail validate." };
            var jobs = new Option<int>("--jobs") { Description = "Generate this many contiguous segments of the range in parallel, then generate again the levels at their seams that break a repetition rule with their neighbours.", DefaultValueFactory = _ => 1 };
            foreach (Option option in new Option[] { profile, levels, seed, outDir, lib, catalog, curated, extraHistory, keep, forced, forcedClass, thresholds, pairs, allowDraft, level8, jobs })
            {
                command.Options.Add(option);
            }

            command.SetAction(parse => Cli.Run(parse, report =>
            {
                GenerationProfile band = ProfileLoader.ReadFile(parse.GetValue(profile)!);
                List<BasePicture> library = ContentStore.LoadLibrary(parse.GetValue(lib)!);
                ApprovedPairs? approved = ContentStore.LoadPairs(parse.GetValue(pairs)!);
                if (approved == null)
                {
                    throw new IOException($"{parse.GetValue(pairs)} is missing; run `readability --write-provisional` or record approved pairs.");
                }

                string thresholdsJson = File.ReadAllText(parse.GetValue(thresholds)!);
                DifficultyThresholds difficulty = ProfileLoader.ReadThresholds(thresholdsJson, band.BandId);
                DifficultyThresholds? bigDifficulty = ProfileLoader.ReadBigThresholds(thresholdsJson, band.BandId);
                var history = new SortedDictionary<int, LevelDefinition>();
                IEnumerable<(string, LevelDefinition)> sources = ContentStore.LoadLevels(parse.GetValue(curated)!).Concat(ContentStore.LoadLevels(parse.GetValue(catalog)!));
                foreach (string folder in parse.GetValue(extraHistory) ?? Array.Empty<string>())
                {
                    sources = sources.Concat(ContentStore.LoadLevels(folder));
                }

                (int first, int last) = Cli.Range(parse.GetValue(levels)!);
                var kept = new HashSet<int>();
                foreach (string folder in parse.GetValue(keep) ?? Array.Empty<string>())
                {
                    // When generating into the keep folder itself (showcase authoring), the requested levels are redone.
                    bool intoKeep = Path.GetFullPath(folder) == Path.GetFullPath(parse.GetValue(outDir)!);
                    foreach ((string file, LevelDefinition level) in ContentStore.LoadLevels(folder))
                    {
                        if (intoKeep && level.LevelNumber >= first && level.LevelNumber <= last)
                        {
                            continue;
                        }

                        kept.Add(level.LevelNumber);
                        sources = sources.Append((file, level));
                    }
                }

                foreach ((string _, LevelDefinition level) in sources)
                {
                    history[level.LevelNumber] = level;
                }

                var progressLock = new object();
                LevelGenerator NewGenerator()
                {
                    var made = new LevelGenerator(
                        band,
                        new PicturePicker(library, parse.GetValue(allowDraft)),
                        difficulty,
                        approved.IsApproved,
                        new DifficultySchedule(0xB100B100UL),
                        UnlockRoadmap.ForLevel8(parse.GetValue(level8)!));
                    made.BigLevelThresholds = bigDifficulty;
                    if (!string.IsNullOrEmpty(parse.GetValue(forced)))
                    {
                        made.ForcedMechanics = parse.GetValue(forced)!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    }

                    if (!string.IsNullOrEmpty(parse.GetValue(forcedClass)))
                    {
                        made.ForcedClass = parse.GetValue(forcedClass) switch
                        {
                            "normal" => DifficultyClass.Normal,
                            "hard" => DifficultyClass.Hard,
                            "super_hard" => DifficultyClass.SuperHard,
                            var other => throw new ArgumentException($"--class '{other}': use normal, hard or super_hard."),
                        };
                    }

                    made.Progress = (level, accepted, candidates) =>
                    {
                        lock (progressLock)
                        {
                            Console.Error.WriteLine(accepted == null
                                ? $"  L{level}: failed after {candidates} candidates"
                                : $"  L{level}: {accepted.Definition.Difficulty.Class} ({accepted.Definition.Difficulty.Score}), {accepted.Definition.Picture.Id}, {accepted.Definition.Pods.Count} pods, candidate {candidates}");
                        }
                    };
                    return made;
                }

                (GenerationResult result, int seamRepairs) = GenerateRange(NewGenerator, first, last, (ulong)parse.GetValue(seed), history, kept, Math.Max(1, parse.GetValue(jobs)));
                report["jobs"] = Math.Max(1, parse.GetValue(jobs));
                report["seamRepairs"] = seamRepairs;

                string batch = parse.GetValue(outDir)!;
                foreach (GeneratedLevel level in result.Accepted)
                {
                    ContentStore.WriteLevel(batch, level.Definition, level.Record);
                }

                var rejections = new JArray(result.Rejections.Select(r => new JObject { ["level"] = r.Level, ["attempt"] = r.Attempt, ["reason"] = r.Reason }).ToArray());
                Directory.CreateDirectory(batch);
                File.WriteAllText(Path.Combine(batch, "rejections.json"), CanonicalJson.Write(new JObject { ["rejections"] = rejections }, indented: true));

                // Validate the accepted levels in the context of the catalog (FR-080, FR-081, FR-083).
                var validator = new CatalogValidator(library, UnlockRoadmap.ForLevel8(parse.GetValue(level8)!), approved, new SolveOptions(band.SolverNodeBudget));
                var accepted = new HashSet<int>(result.Accepted.Select(a => a.Definition.LevelNumber));
                CatalogReport validation = validator.Validate(history.Values.ToList(), accepted);
                var issues = validation.Issues.Where(i => accepted.Contains(i.Level)).ToList();

                report["accepted"] = result.Accepted.Count;
                report["rejected"] = result.Rejections.Count;
                report["failedLevels"] = new JArray(result.Failed.ToArray());
                report["validationErrors"] = issues.Count(i => i.IsError);
                report["classes"] = new JObject(result.Accepted.GroupBy(a => a.Definition.Difficulty.Class.ToString()).OrderBy(g => g.Key).Select(g => new JProperty(g.Key, g.Count())));
                Cli.Say(parse, $"generate {band.BandId} L{first}–{last}: {result.Accepted.Count} accepted, {result.Rejections.Count} candidates rejected, {result.Failed.Count} levels failed.");
                foreach (IGrouping<string, Rejection> reasons in result.Rejections.GroupBy(r => r.Reason.Split(':')[0] + ":" + (r.Reason.Split(':').Length > 1 ? r.Reason.Split(':')[1] : string.Empty)).OrderByDescending(g => g.Count()))
                {
                    Cli.Say(parse, $"  rejected {reasons.Count(),4}× {reasons.Key}");
                }

                foreach (LevelIssue issue in issues)
                {
                    Cli.Say(parse, $"  {(issue.IsError ? "error" : "warning")} L{issue.Level} {issue.Check}: {issue.Message}");
                }

                return result.Failed.Count == 0 && issues.All(i => !i.IsError) ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            return command;
        }

        /// <summary>
        /// Generates <paramref name="first"/>–<paramref name="last"/> into <paramref name="history"/>. With more than one
        /// job the range is cut into contiguous segments generated in parallel, each knowing only the fixed history; the
        /// levels near a segment's start that then break a repetition rule with their neighbours
        /// (<see cref="LevelGenerator.Conflicts"/>) are generated again between them, until none does. Returns the
        /// accepted levels (one per level number) and how many were generated again.
        /// </summary>
        public static (GenerationResult Result, int SeamRepairs) GenerateRange(Func<LevelGenerator> newGenerator, int first, int last, ulong seed, SortedDictionary<int, LevelDefinition> history, ISet<int> kept, int jobs)
        {
            int count = last - first + 1;
            int segments = Math.Max(1, Math.Min(jobs, count / PicturePicker.RepeatWindow));
            if (segments == 1)
            {
                return (newGenerator().Generate(first, last, seed, history, kept), 0);
            }

            var bounds = new List<(int First, int Last)>();
            for (int k = 0; k < segments; k++)
            {
                int a = first + (int)((long)count * k / segments);
                int b = first + (int)((long)count * (k + 1) / segments) - 1;
                bounds.Add((a, b));
            }

            var parts = new GenerationResult[segments];
            var fixedHistory = new SortedDictionary<int, LevelDefinition>(history);
            System.Threading.Tasks.Parallel.For(0, segments, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = segments }, k =>
            {
                var own = new SortedDictionary<int, LevelDefinition>(fixedHistory);
                parts[k] = newGenerator().Generate(bounds[k].First, bounds[k].Last, seed, own, kept);
            });

            var accepted = new SortedDictionary<int, GeneratedLevel>();
            var merged = new GenerationResult();
            foreach (GenerationResult part in parts)
            {
                foreach (GeneratedLevel level in part.Accepted)
                {
                    accepted[level.Definition.LevelNumber] = level;
                    history[level.Definition.LevelNumber] = level.Definition;
                }

                merged.Rejections.AddRange(part.Rejections);
                merged.Failed.AddRange(part.Failed);
            }

            // The seams: a segment's first levels never saw the previous segment's last ones.
            LevelGenerator repairer = newGenerator();
            int repairs = 0;
            var seamLevels = new List<int>();
            for (int k = 1; k < segments; k++)
            {
                for (int l = bounds[k].First - PicturePicker.RepeatWindow; l < bounds[k].First + PicturePicker.RepeatWindow; l++)
                {
                    if (l >= first && l <= last && !kept.Contains(l))
                    {
                        seamLevels.Add(l);
                    }
                }
            }

            for (int round = 0; round < 8; round++)
            {
                var readOnly = new SortedDictionary<int, LevelDefinition>(history);
                var broken = seamLevels.Where(l => readOnly.ContainsKey(l) && LevelGenerator.Conflicts(l, readOnly).Count > 0).ToList();
                if (broken.Count == 0)
                {
                    break;
                }

                GenerationResult again = repairer.Regenerate(broken, seed ^ (0x5EA3UL * (ulong)(round + 1)), history);
                repairs += broken.Count;
                foreach (int level in broken)
                {
                    accepted.Remove(level);
                }

                foreach (GeneratedLevel level in again.Accepted)
                {
                    accepted[level.Definition.LevelNumber] = level;
                }

                merged.Rejections.AddRange(again.Rejections);
                merged.Failed.RemoveAll(again.Accepted.Select(a => a.Definition.LevelNumber).Contains);
                merged.Failed.AddRange(again.Failed.Where(l => !merged.Failed.Contains(l)));
            }

            merged.Accepted.AddRange(accepted.Values);
            return (merged, repairs);
        }
    }
}
