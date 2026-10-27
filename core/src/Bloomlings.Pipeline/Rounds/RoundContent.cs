using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bloomlings.Content.Json;
using Bloomlings.Content.Rounds;
using Bloomlings.Content.Schemas;
using Bloomlings.Core.Rounds;
using Json.Schema;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>One finding of the round checks: where (<c>L0005</c>, <c>P0115</c>, <c>palette</c>, <c>set</c>), which check, and whether it blocks.</summary>
    public sealed record RoundIssue(string Where, string Check, string Message, bool IsError = true)
    {
        public override string ToString() => $"{(IsError ? "error" : "warning")} {Where} {Check}: {Message}";
    }

    /// <summary>A round file as read: its path, its text and its parsed value (null when it failed to read).</summary>
    public sealed record RoundFile<T>(string Path, string Text, T? Value)
        where T : class;

    /// <summary>
    /// A round content folder (<c>content/game/rounds/</c> or an export folder): <c>levels/L####.json</c>,
    /// <c>pictures/P####.json</c>, <c>proofs/L####.json</c>, <c>conformance/L####.json</c>. Every file is evaluated against
    /// its embedded schema (JsonSchema.Net) and read by <see cref="RoundJson"/>; failures become issues and the file stays
    /// out of the parsed sets.
    /// </summary>
    public sealed class RoundContent
    {
        public const string LevelsFolder = "levels";
        public const string PicturesFolder = "pictures";
        public const string ProofsFolder = "proofs";
        public const string ConformanceFolder = "conformance";

        private static readonly Regex LevelFile = new Regex(@"^L([0-9]{4,})\.json$", RegexOptions.CultureInvariant);
        private static readonly Regex PictureFile = new Regex(@"^(P[0-9]{4})\.json$", RegexOptions.CultureInvariant);

        private static readonly Lazy<JsonSchema> LevelSchema = new Lazy<JsonSchema>(() => JsonSchema.FromText(SchemaResources.Read(SchemaResources.RoundLevel)));
        private static readonly Lazy<JsonSchema> PictureSchema = new Lazy<JsonSchema>(() => JsonSchema.FromText(SchemaResources.Read(SchemaResources.RoundPicture)));
        private static readonly Lazy<JsonSchema> ProofSchema = new Lazy<JsonSchema>(() => JsonSchema.FromText(SchemaResources.Read(SchemaResources.RoundProof)));
        private static readonly Lazy<JsonSchema> ManifestSchema = new Lazy<JsonSchema>(() => JsonSchema.FromText(SchemaResources.Read(SchemaResources.RoundContentManifest)));

        public RoundContent(string folder)
        {
            Folder = folder;
        }

        public string Folder { get; }

        /// <summary>Levels by number (the file name's number; the document must agree).</summary>
        public SortedDictionary<int, RoundFile<RoundLevel>> Levels { get; } = new SortedDictionary<int, RoundFile<RoundLevel>>();

        /// <summary>Pictures by id (one version per id in a folder).</summary>
        public SortedDictionary<string, RoundFile<RoundPicture>> Pictures { get; } = new SortedDictionary<string, RoundFile<RoundPicture>>(StringComparer.Ordinal);

        public SortedDictionary<int, RoundFile<RoundProof>> Proofs { get; } = new SortedDictionary<int, RoundFile<RoundProof>>();

        public SortedDictionary<int, RoundFile<RoundConformanceFixture>> Fixtures { get; } = new SortedDictionary<int, RoundFile<RoundConformanceFixture>>();

        /// <summary>Reading problems: schema failures, unreadable files, misnamed files.</summary>
        public List<RoundIssue> Issues { get; } = new List<RoundIssue>();

        /// <summary>Loads a folder; a missing subfolder is simply empty.</summary>
        public static RoundContent Load(string folder)
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"round content folder not found: {folder}");
            }

            var content = new RoundContent(folder);
            foreach (string file in Files(folder, LevelsFolder, content))
            {
                int? number = Number(file, content);
                if (number == null)
                {
                    continue;
                }

                RoundLevel? level = ReadChecked(file, LevelSchema.Value, RoundJson.ReadLevel, content, out string text);
                if (level != null && level.Number != number)
                {
                    content.Issues.Add(new RoundIssue(Where(number.Value), "file-name", $"{Rel(folder, file)} holds level {level.Number}"));
                    level = null;
                }

                content.Levels[number.Value] = new RoundFile<RoundLevel>(file, text, level);
            }

            foreach (string file in Files(folder, PicturesFolder, content))
            {
                Match match = PictureFile.Match(Path.GetFileName(file));
                if (!match.Success)
                {
                    content.Issues.Add(new RoundIssue("set", "file-name", $"{Rel(folder, file)} is not named P####.json", IsError: false));
                    continue;
                }

                string id = match.Groups[1].Value;
                RoundPicture? picture = ReadChecked(file, PictureSchema.Value, RoundJson.ReadPicture, content, out string text);
                if (picture != null && picture.Id != id)
                {
                    content.Issues.Add(new RoundIssue(id, "file-name", $"{Rel(folder, file)} holds picture {picture.Id}"));
                    picture = null;
                }

                content.Pictures[id] = new RoundFile<RoundPicture>(file, text, picture);
            }

            foreach (string file in Files(folder, ProofsFolder, content))
            {
                int? number = Number(file, content);
                if (number == null)
                {
                    continue;
                }

                RoundProof? proof = ReadChecked(file, ProofSchema.Value, RoundJson.ReadProof, content, out string text);
                if (proof != null && proof.Number != number)
                {
                    content.Issues.Add(new RoundIssue(Where(number.Value), "file-name", $"{Rel(folder, file)} holds the proof of level {proof.Number}"));
                    proof = null;
                }

                content.Proofs[number.Value] = new RoundFile<RoundProof>(file, text, proof);
            }

            foreach (string file in Files(folder, ConformanceFolder, content))
            {
                int? number = Number(file, content);
                if (number == null)
                {
                    continue;
                }

                RoundConformanceFixture? fixture = ReadChecked(file, null, RoundJson.ReadFixture, content, out string text);
                if (fixture != null && fixture.Number != number)
                {
                    content.Issues.Add(new RoundIssue(Where(number.Value), "file-name", $"{Rel(folder, file)} holds the fixture of level {fixture.Number}"));
                    fixture = null;
                }

                content.Fixtures[number.Value] = new RoundFile<RoundConformanceFixture>(file, text, fixture);
            }

            return content;
        }

        /// <summary>The schema errors of a document (empty when valid), with the instance location of each.</summary>
        public static IReadOnlyList<string> SchemaErrors(JsonSchema schema, string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            EvaluationResults results = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (results.IsValid)
            {
                return Array.Empty<string>();
            }

            var errors = new List<string>();
            foreach (EvaluationResults detail in new[] { results }.Concat(results.Details ?? Enumerable.Empty<EvaluationResults>()))
            {
                if (detail.Errors == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> error in detail.Errors)
                {
                    string at = detail.InstanceLocation.ToString();
                    errors.Add($"{(at.Length == 0 ? "/" : at)}: {error.Value}");
                }
            }

            return errors.Count > 0 ? errors.Distinct(StringComparer.Ordinal).ToList() : new List<string> { "does not match the schema" };
        }

        /// <summary>The schema errors of a <c>round-content-manifest.v1</c> document.</summary>
        public static IReadOnlyList<string> ManifestSchemaErrors(string json) => SchemaErrors(ManifestSchema.Value, json);

        /// <summary><c>L0005</c> for level 5.</summary>
        public static string Where(int number) => $"L{number:0000}";

        public static string Rel(string folder, string file) => Path.GetRelativePath(folder, file).Replace('\\', '/');

        private static IEnumerable<string> Files(string folder, string sub, RoundContent content)
        {
            string dir = Path.Combine(folder, sub);
            if (!Directory.Exists(dir))
            {
                return Array.Empty<string>();
            }

            var files = new List<string>();
            foreach (string file in Directory.GetFiles(dir).OrderBy(f => f, StringComparer.Ordinal))
            {
                string name = Path.GetFileName(file);
                if (name.StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!name.EndsWith(".json", StringComparison.Ordinal))
                {
                    content.Issues.Add(new RoundIssue("set", "file-name", $"{Rel(folder, file)} is not a JSON file and is ignored", IsError: false));
                    continue;
                }

                files.Add(file);
            }

            return files;
        }

        private static int? Number(string file, RoundContent content)
        {
            Match match = LevelFile.Match(Path.GetFileName(file));
            if (!match.Success)
            {
                content.Issues.Add(new RoundIssue("set", "file-name", $"{Rel(content.Folder, file)} is not named L####.json", IsError: false));
                return null;
            }

            return int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static T? ReadChecked<T>(string file, JsonSchema? schema, Func<string, T> read, RoundContent content, out string text)
            where T : class
        {
            string rel = Rel(content.Folder, file);
            string where = Path.GetFileNameWithoutExtension(file);
            text = File.ReadAllText(file);
            bool failed = false;
            if (schema != null)
            {
                try
                {
                    foreach (string error in SchemaErrors(schema, text))
                    {
                        content.Issues.Add(new RoundIssue(where, "schema", $"{rel} {error}"));
                        failed = true;
                    }
                }
                catch (JsonException ex)
                {
                    content.Issues.Add(new RoundIssue(where, "schema", $"{rel}: invalid JSON: {ex.Message}"));
                    return null;
                }
            }

            try
            {
                T value = read(text);
                return failed ? null : value;
            }
            catch (Exception ex) when (ex is ContentFormatException || ex is ArgumentException)
            {
                content.Issues.Add(new RoundIssue(where, "schema", $"{rel} {ex.Message}"));
                return null;
            }
        }
    }
}
