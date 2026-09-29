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
            foreach (Option option in new Option[] { profile, levels, seed, outDir, lib, catalog, curated, extraHistory, keep, forced, forcedClass, thresholds, pairs, allowDraft, level8 })
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

                DifficultyThresholds difficulty = ProfileLoader.ReadThresholds(File.ReadAllText(parse.GetValue(thresholds)!), band.BandId);
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

                var generator = new LevelGenerator(
                    band,
                    new PicturePicker(library, parse.GetValue(allowDraft)),
                    difficulty,
                    approved.IsApproved,
                    new DifficultySchedule(0xB100B100UL),
                    UnlockRoadmap.ForLevel8(parse.GetValue(level8)!));
                if (!string.IsNullOrEmpty(parse.GetValue(forced)))
                {
                    generator.ForcedMechanics = parse.GetValue(forced)!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                if (!string.IsNullOrEmpty(parse.GetValue(forcedClass)))
                {
                    generator.ForcedClass = parse.GetValue(forcedClass) switch
                    {
                        "normal" => DifficultyClass.Normal,
                        "hard" => DifficultyClass.Hard,
                        "super_hard" => DifficultyClass.SuperHard,
                        var other => throw new ArgumentException($"--class '{other}': use normal, hard or super_hard."),
                    };
                }

                generator.Progress = (level, accepted, candidates) => Console.Error.WriteLine(accepted == null
                    ? $"  L{level}: failed after {candidates} candidates"
                    : $"  L{level}: {accepted.Definition.Difficulty.Class} ({accepted.Definition.Difficulty.Score}), {accepted.Definition.Picture.Id}, {accepted.Definition.Pods.Count} pods, candidate {candidates}");
                GenerationResult result = generator.Generate(first, last, (ulong)parse.GetValue(seed), history, kept);

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
    }
}
