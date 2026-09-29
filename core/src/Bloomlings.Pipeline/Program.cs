using System.CommandLine;
using Bloomlings.Pipeline.Commands;

namespace Bloomlings.Pipeline
{
    /// <summary>
    /// The content pipeline CLI (<c>bloomlings-pipeline</c>, contracts/pipeline-cli.md, T081): picture library →
    /// generation → validation → scoring → review → publish. Exit codes: 0 success, 1 validation failures, 2 usage or
    /// I/O error; <c>--json</c> writes a machine-readable report to stdout.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var root = new RootCommand("Bloomlings content pipeline (contracts/pipeline-cli.md).");
            root.Options.Add(Cli.Json);
            root.Subcommands.Add(PictureCommands.Create());
            root.Subcommands.Add(ReadabilityCli.Create());
            root.Subcommands.Add(GenerateCommand.Create());
            root.Subcommands.Add(DailyGenerateCommand.Create());
            root.Subcommands.Add(CatalogCommands.Solve());
            root.Subcommands.Add(CatalogCommands.Validate());
            root.Subcommands.Add(CatalogCommands.Score());
            root.Subcommands.Add(CatalogCommands.ReviewSheet());
            root.Subcommands.Add(CatalogCommands.Publish());
            root.Subcommands.Add(CatalogCommands.Replay());
            root.Subcommands.Add(CatalogCommands.Diff());

            ParseResult parse = root.Parse(args);
            if (parse.Errors.Count > 0)
            {
                foreach (System.CommandLine.Parsing.ParseError error in parse.Errors)
                {
                    System.Console.Error.WriteLine(error.Message);
                }

                return ExitCodes.UsageOrIo;
            }

            return parse.Invoke();
        }
    }
}
