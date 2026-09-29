using System;
using System.CommandLine;
using System.IO;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Commands
{
    /// <summary>Exit codes of contracts/pipeline-cli.md.</summary>
    public static class ExitCodes
    {
        public const int Success = 0;
        public const int ValidationFailed = 1;
        public const int UsageOrIo = 2;
    }

    /// <summary>Shared options and report output: a human summary, or the JSON report on stdout with <c>--json</c>.</summary>
    public static class Cli
    {
        public static readonly Option<bool> Json = new Option<bool>("--json") { Description = "Write a machine-readable JSON report to stdout.", Recursive = true };

        public static Option<string> Path(string name, string defaultValue, string description) =>
            new Option<string>(name) { Description = description, DefaultValueFactory = _ => defaultValue };

        public static Option<string> Required(string name, string description) =>
            new Option<string>(name) { Description = description, Required = true };

        public static Option<int> Int(string name, int defaultValue, string description) =>
            new Option<int>(name) { Description = description, DefaultValueFactory = _ => defaultValue };

        /// <summary>Runs a command body, mapping I/O and format errors to exit code 2.</summary>
        public static int Run(ParseResult parse, Func<JObject, int> body)
        {
            var report = new JObject();
            int code;
            try
            {
                code = body(report);
            }
            catch (Exception ex) when (ex is IOException || ex is ContentFormatException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidDataException || ex is FormatException)
            {
                report["error"] = ex.Message;
                Console.Error.WriteLine("error: " + ex.Message);
                code = ExitCodes.UsageOrIo;
            }

            report["exitCode"] = code;
            if (parse.GetValue(Json))
            {
                Console.Out.WriteLine(CanonicalJson.Write(report, indented: true).TrimEnd('\n'));
            }

            return code;
        }

        /// <summary>A human summary line (stderr when the JSON report owns stdout).</summary>
        public static void Say(ParseResult parse, string line)
        {
            if (parse.GetValue(Json))
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                Console.Out.WriteLine(line);
            }
        }

        /// <summary>Parses "11-25" or "7".</summary>
        public static (int First, int Last) Range(string text)
        {
            string[] parts = text.Split('-');
            int first = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            int last = parts.Length > 1 ? int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : first;
            if (first < 1 || last < first)
            {
                throw new ArgumentException($"'{text}' is not a level range like 11-25.");
            }

            return (first, last);
        }
    }
}
