using System.CommandLine;
using System.IO;
using System.Linq;
using Bloomlings.Core.Variants;
using Bloomlings.Pipeline.Readability;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary><c>readability</c> (T075): writes the pair report and, on request, a provisional approved-pairs file.</summary>
    public static class ReadabilityCli
    {
        public static Command Create()
        {
            var command = new Command("readability", "Compute variant-pair color distances (normal and simulated color vision deficiencies) and grayscale contrast.");
            Option<string> outDir = Cli.Path("--out", "content/readability", "Folder for pairs-report.json.");
            var provisional = new Option<bool>("--write-provisional") { Description = "Also write approved-pairs.json with the candidate pairs as provisional (never overwrites a human-approved file)." };
            command.Options.Add(outDir);
            command.Options.Add(provisional);
            command.SetAction(parse => Cli.Run(parse, report =>
            {
                string folder = Directory.CreateDirectory(parse.GetValue(outDir)!).FullName;
                var pairs = ReadabilityCommand.Analyze(VariantCatalog.Default);
                File.WriteAllText(Path.Combine(folder, "pairs-report.json"), ReadabilityCommand.WriteReport(pairs));
                int candidates = pairs.Count(p => p.Candidate);
                report["pairs"] = pairs.Count;
                report["candidates"] = candidates;
                Cli.Say(parse, $"readability: {pairs.Count} pairs, {candidates} candidates → {Path.Combine(folder, "pairs-report.json")}");
                foreach (PairReport pair in pairs.Where(p => !p.Candidate))
                {
                    Cli.Say(parse, $"  not a candidate: {pair.A}+{pair.B} (min ΔE00 {pair.MinDeltaE}, grayscale ΔL {pair.GrayscaleDeltaL})");
                }

                if (parse.GetValue(provisional))
                {
                    string approvedPath = Path.Combine(folder, "approved-pairs.json");
                    ApprovedPairs? existing = File.Exists(approvedPath) ? ApprovedPairs.Read(approvedPath) : null;
                    if (existing != null && !existing.IsProvisional)
                    {
                        Cli.Say(parse, "approved-pairs.json is human-approved; left unchanged.");
                    }
                    else
                    {
                        File.WriteAllText(approvedPath, ApprovedPairs.Write(
                            "provisional",
                            null,
                            pairs.Where(p => p.Candidate),
                            "Automated candidates from pairs-report.json. A person must confirm them with the in-game readability tests (FR-005) and set status to approved."));
                        Cli.Say(parse, $"wrote {approvedPath} (provisional).");
                    }
                }

                return ExitCodes.Success;
            }));
            return command;
        }
    }
}
