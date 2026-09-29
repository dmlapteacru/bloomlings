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
    /// <c>pictures validate</c> (T074): the embedded base-picture schema (JsonSchema.Net), "7 ≤ width ≤ 14, 8 ≤ height ≤ 16",
    /// "At least 2 roles; each role has exactly one color group", a licence, and the review status: only
    /// <c>approved</c> pictures are usable (FR-084, FR-091).
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

            var used = new bool[picture.Roles.Count];
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int cell in row)
                {
                    if (cell >= 0)
                    {
                        used[cell] = true;
                    }
                }
            }

            for (int i = 0; i < picture.Roles.Count; i++)
            {
                PictureRole role = picture.Roles[i];
                if (!used[i])
                {
                    warnings.Add($"role '{role.RoleId}' is not used by the grid");
                }

                if (!HasLaunchVariant(role.ColorGroup))
                {
                    warnings.Add($"role '{role.RoleId}' ({role.ColorGroup}) has no launch variant; usable only after a pool expansion");
                }
            }

            if (picture.Structure == null)
            {
                warnings.Add("structure metrics are missing (run pictures import)");
            }

            bool approved = picture.Review.Status == ReviewStatus.Approved;
            if (!approved)
            {
                warnings.Add($"review status is {picture.Review.Status}: unusable until approved (FR-084)");
            }

            return new PictureReport(file, picture.Id, errors, warnings, errors.Count == 0 && approved);
        }

        private static bool HasLaunchVariant(ColorGroup group)
        {
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                if (info.ColorGroup == group && info.Status == VariantStatus.Launch)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
