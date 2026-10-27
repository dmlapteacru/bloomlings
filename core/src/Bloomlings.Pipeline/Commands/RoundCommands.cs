using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Pipeline.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary>
    /// <c>rounds import | validate | diff | publish</c> (spec 006, contracts/pipeline-rounds-cli.md; T037, T038, T051,
    /// T052): the game side of a round wave's transfer. Exit codes as the rest of the CLI: 0 success, 1 validation errors,
    /// 2 usage or I/O. The definition rules and the conformance replay are plugged in through <see cref="RoundSeams"/>.
    /// </summary>
    public static class RoundCommands
    {
        public const string DefaultRounds = "content/game/rounds";

        public static Command Create()
        {
            var rounds = new Command("rounds", "Round levels in the game (spec 006): import an export, validate, diff and publish.");
            rounds.Subcommands.Add(Import());
            rounds.Subcommands.Add(Validate());
            rounds.Subcommands.Add(Diff());
            rounds.Subcommands.Add(Publish());
            return rounds;
        }

        public static Command Import()
        {
            var command = new Command("import", "Check an export of tools/rounds/export.mjs with the rounds already in the game (schemas, definition rules, picture ↔ board, proofs, fixtures, fingerprints), then copy its levels, pictures, proofs and fixtures into the rounds folder.");
            var exportDir = new Argument<string>("export-dir") { Description = "The exporter's output, e.g. content/game/rounds/export/L0001-L0020." };
            Option<string> roundsDir = RoundsOption();
            command.Arguments.Add(exportDir);
            command.Options.Add(roundsDir);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                RoundImportResult result = RoundImporter.Import(parse.GetValue(exportDir)!, parse.GetValue(roundsDir)!, new RoundValidationOptions());
                Report(parse, report, result.Issues);
                report["copied"] = new JArray(result.Copied.ToArray());
                if (!result.Imported)
                {
                    Cli.Say(parse, $"rounds import refused: {result.Issues.Count(i => i.IsError)} errors; nothing copied.");
                    return ExitCodes.ValidationFailed;
                }

                Cli.Say(parse, $"rounds import: {result.Copied.Count} files → {parse.GetValue(roundsDir)}. Run rounds validate next.");
                return ExitCodes.Success;
            }));
            return command;
        }

        public static Command Validate()
        {
            var command = new Command("validate", "Validate the game's round content: schemas, definition rules, picture ↔ board, proof completeness, fixtures and their replay, equal engine fingerprints, and the palette's normal-vision readability.");
            Option<string> roundsDir = RoundsOption();
            Option<string> readability = Cli.Path("--readability-report", string.Empty, "Where to write the palette report (all four visions); default <rounds>/readability-report.json.");
            command.Options.Add(roundsDir);
            command.Options.Add(readability);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                string folder = parse.GetValue(roundsDir)!;
                RoundContent content = RoundContent.Load(folder);
                RoundValidation validation = RoundSetValidator.Validate(content, new RoundValidationOptions());
                string reportPath = parse.GetValue(readability)!.Length > 0 ? parse.GetValue(readability)! : Path.Combine(folder, "readability-report.json");
                if (validation.Pairs != null)
                {
                    File.WriteAllText(reportPath, RoundReadability.WriteReport(validation.Pairs));
                    report["readabilityReport"] = reportPath;
                }

                Report(parse, report, validation.Issues);
                report["levels"] = content.Levels.Count;
                report["pictures"] = content.Pictures.Count;
                int errors = validation.Errors.Count();
                Cli.Say(parse, $"rounds validate: {content.Levels.Count} levels, {content.Pictures.Count} pictures, {errors} errors, {validation.Issues.Count - errors} warnings.");
                return errors == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            return command;
        }

        public static Command Diff()
        {
            var command = new Command("diff", "Compare round content: a changed level needs a higher definitionVersion, a changed picture a higher version (FR-052).");
            Option<string> from = Cli.Required("--from", "Earlier content: a git ref (e.g. main), a published folder (manifest.json) or a rounds folder.");
            Option<string> to = Cli.Path("--to", DefaultRounds, "Later content: a rounds folder or a published folder.");
            Option<string> gitFolder = Cli.Path("--git-folder", string.Empty, "The rounds folder read at a git ref (default: --to when it is a rounds folder, else content/game/rounds).");
            command.Options.Add(from);
            command.Options.Add(to);
            command.Options.Add(gitFolder);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                string toSource = parse.GetValue(to)!;
                string folder = parse.GetValue(gitFolder)!.Length > 0 ? parse.GetValue(gitFolder)!
                    : Directory.Exists(Path.Combine(toSource, RoundContent.LevelsFolder)) ? toSource : DefaultRounds;
                RoundSource before = RoundSource.Load(parse.GetValue(from)!, folder);
                RoundSource after = RoundSource.Load(toSource, folder);
                RoundDiffResult diff = RoundDiff.Compare(before, after);
                report["fromContentVersion"] = before.ContentVersion;
                report["toContentVersion"] = after.ContentVersion;
                report["changed"] = new JArray(diff.Changed.ToArray());
                report["added"] = new JArray(diff.Added.ToArray());
                report["changedPictures"] = new JArray(diff.ChangedPictures.ToArray());
                report["addedPictures"] = new JArray(diff.AddedPictures.ToArray());
                report["errors"] = new JArray(diff.Errors.ToArray());
                foreach (int number in diff.Changed)
                {
                    Cli.Say(parse, $"  changed {RoundContent.Where(number)}: definitionVersion {before.Levels[number].DefinitionVersion} → {after.Levels[number].DefinitionVersion}");
                }

                foreach (string error in diff.Errors)
                {
                    Cli.Say(parse, "  error: " + error);
                }

                Cli.Say(parse, $"rounds diff: {diff.Changed.Count} changed, {diff.Added.Count} added, {diff.ChangedPictures.Count} pictures changed, {diff.Errors.Count} errors.");
                return diff.Errors.Count == 0 ? ExitCodes.Success : ExitCodes.ValidationFailed;
            }));
            return command;
        }

        public static Command Publish()
        {
            var command = new Command("publish", "Validate the rounds folder (any error refuses), then write the packs and round-content-manifest.v1 for the clients.");
            Option<string> roundsDir = RoundsOption();
            Option<string> outDir = Cli.Required("--out", "Output folder, e.g. content/game/rounds/published.");
            var version = new Option<int?>("--content-version") { Description = "Content version (default: kept when the packs are unchanged, else the published one + 1, or 1)." };
            command.Options.Add(roundsDir);
            command.Options.Add(outDir);
            command.Options.Add(version);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                RoundContent content = RoundContent.Load(parse.GetValue(roundsDir)!);
                RoundValidation validation = RoundSetValidator.Validate(content, new RoundValidationOptions());
                Report(parse, report, validation.Issues);
                if (!validation.Passed)
                {
                    Cli.Say(parse, $"rounds publish refused: {validation.Errors.Count()} validation errors.");
                    return ExitCodes.ValidationFailed;
                }

                RoundPublishResult result = RoundPublisher.Publish(content, parse.GetValue(outDir)!, parse.GetValue(version));
                report["contentVersion"] = result.Manifest.ContentVersion;
                report["levels"] = result.Manifest.Levels.Count;
                report["pictures"] = result.Pictures;
                report["packs"] = new JArray(result.Manifest.Packs.Select(p => p.Path).ToArray());
                report["removed"] = new JArray(result.Removed.ToArray());
                Cli.Say(parse, $"rounds publish v{result.Manifest.ContentVersion}: {result.Manifest.Levels.Count} levels, {result.Pictures} pictures, {result.Manifest.Packs.Count} packs → {parse.GetValue(outDir)}");
                return ExitCodes.Success;
            }));
            return command;
        }

        private static Option<string> RoundsOption() => Cli.Path("--rounds", DefaultRounds, "The game's rounds folder.");

        private static void Report(ParseResult parse, JObject report, IReadOnlyList<RoundIssue> issues)
        {
            report["errors"] = new JArray(issues.Where(i => i.IsError).Select(Json).ToArray());
            report["warnings"] = new JArray(issues.Where(i => !i.IsError).Select(Json).ToArray());
            foreach (RoundIssue issue in issues)
            {
                Cli.Say(parse, "  " + issue);
            }
        }

        private static JObject Json(RoundIssue issue) => new JObject { ["where"] = issue.Where, ["check"] = issue.Check, ["message"] = issue.Message };
    }
}
