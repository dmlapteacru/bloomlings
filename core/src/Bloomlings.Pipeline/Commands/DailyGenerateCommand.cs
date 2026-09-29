using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using Bloomlings.Core.Definitions;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Readability;
using Bloomlings.Solver;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary>
    /// <c>daily generate</c> (R19): builds the Daily Challenge pool with the <c>daily</c> profile. Pool entry i is
    /// stored as level number i + 1 of the batch; the daily pack keeps the index.
    /// </summary>
    public static class DailyGenerateCommand
    {
        public static Command Create()
        {
            var daily = new Command("daily", "Daily Challenge pool.");
            var generate = new Command("generate", "Build the Daily Challenge pool with the daily profile (R19).");
            Option<int> count = Cli.Int("--count", 30, "Pool size.");
            Option<long> seed = new Option<long>("--seed") { Description = "Generation seed.", Required = true };
            Option<string> profile = Cli.Path("--profile", "content/profiles/daily.json", "Daily profile.");
            Option<string> outDir = Cli.Path("--out", "content/work/daily", "Output folder.");
            Option<string> lib = Cli.Path("--lib", "content/pictures/lib", "Picture library.");
            Option<string> thresholds = Cli.Path("--thresholds", "content/profiles/difficulty-thresholds.json", "Difficulty thresholds.");
            Option<string> pairs = Cli.Path("--pairs", "content/readability/approved-pairs.json", "Approved readability pairs.");
            var allowDraft = new Option<bool>("--allow-draft") { Description = "Development preview only: use unapproved pictures." };
            foreach (Option option in new Option[] { count, seed, profile, outDir, lib, thresholds, pairs, allowDraft })
            {
                generate.Options.Add(option);
            }

            generate.SetAction(parse => Cli.Run(parse, report =>
            {
                GenerationProfile band = ProfileLoader.ReadFile(parse.GetValue(profile)!);
                ApprovedPairs approved = ContentStore.LoadPairs(parse.GetValue(pairs)!) ?? throw new IOException("approved-pairs.json is missing.");
                var generator = new LevelGenerator(
                    band,
                    new PicturePicker(ContentStore.LoadLibrary(parse.GetValue(lib)!), parse.GetValue(allowDraft)),
                    ProfileLoader.ReadThresholds(File.ReadAllText(parse.GetValue(thresholds)!), band.BandId),
                    approved.IsApproved,
                    new DifficultySchedule((ulong)parse.GetValue(seed)));
                var history = new SortedDictionary<int, LevelDefinition>();
                GenerationResult result = generator.Generate(1, parse.GetValue(count), (ulong)parse.GetValue(seed), history);
                foreach (GeneratedLevel level in result.Accepted)
                {
                    ContentStore.WriteLevel(parse.GetValue(outDir)!, level.Definition, level.Record);
                }

                report["accepted"] = result.Accepted.Count;
                report["failed"] = result.Failed.Count;
                Cli.Say(parse, $"daily generate: {result.Accepted.Count} of {parse.GetValue(count)} pool entries, {result.Failed.Count} failed.");
                return result.Failed.Count == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            daily.Subcommands.Add(generate);
            return daily;
        }
    }
}
