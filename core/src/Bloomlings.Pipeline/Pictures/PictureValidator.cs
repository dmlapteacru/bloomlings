using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Bloomlings.Content.Json;
using Bloomlings.Content.Schemas;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Json.Schema;

namespace Bloomlings.Pipeline.Pictures
{
    /// <summary>The result of validating one library picture.</summary>
    public sealed record PictureReport(string File, string? Id, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings, bool Usable);

    /// <summary>
    /// <c>pictures validate</c> (T074): the embedded base-picture schema (JsonSchema.Net), "7 ≤ width ≤ 22, 8 ≤ height ≤ 28"
    /// (the format limits since 2026-10-06; the level band rules pick the sizes a level uses),
    /// "At least 2 roles; each role has exactly one color group", a licence, the automated picture checks
    /// (<see cref="PictureChecks"/>) and the review status: only <c>approved</c> pictures are usable (FR-084 as amended,
    /// FR-091).
    /// </summary>
    public static class PictureValidator
    {
        private static readonly Lazy<JsonSchema> Schema = new Lazy<JsonSchema>(() =>
            JsonSchema.FromText(SchemaResources.Read(SchemaResources.BasePicture)));

        public static IReadOnlyList<PictureReport> ValidateFolder(string libraryFolder)
        {
            string[] files = Directory.GetFiles(libraryFolder, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            var reports = new List<PictureReport>();
            foreach (string file in files)
            {
                reports.Add(Validate(file, File.ReadAllText(file)));
            }

            return reports;
        }

        public static PictureReport Validate(string file, string json)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                if (!Schema.Value.Evaluate(document.RootElement).IsValid)
                {
                    errors.Add("does not match base-picture.schema.json");
                }
            }

            BasePicture? picture = null;
            try
            {
                picture = BasePictureJson.Read(json);
            }
            catch (ContentFormatException ex)
            {
                errors.Add(ex.Message);
            }

            if (picture == null)
            {
                return new PictureReport(file, null, errors, warnings, false);
            }

            string expected = Path.GetFileNameWithoutExtension(file);
            if (!string.Equals(picture.Id, expected, StringComparison.Ordinal))
            {
                errors.Add($"id '{picture.Id}' does not match the file name '{expected}'");
            }

            if (picture.Source.Licence.Trim().Length == 0)
            {
                errors.Add("the licence is missing (FR-091)");
            }

            // The automated picture checks (FR-084 as amended): what a draft misses keeps it from being approved on import.
            IReadOnlyList<string> problems = PictureChecks.Problems(picture);
            foreach (string problem in problems)
            {
                warnings.Add(problem);
            }

            bool approved = picture.Review.Status == ReviewStatus.Approved;
            if (!approved)
            {
                warnings.Add(picture.Review.Status == ReviewStatus.Draft
                    ? "draft: it does not pass the automated picture checks yet, so it is unusable (FR-084)"
                    : $"review status is {picture.Review.Status}: unusable (FR-084)");
            }

            return new PictureReport(file, picture.Id, errors, warnings, errors.Count == 0 && approved);
        }
    }
}
