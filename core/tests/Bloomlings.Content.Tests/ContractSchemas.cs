using System;
using System.IO;
using System.Text.Json;
using Bloomlings.Content.Schemas;
using Json.Schema;

namespace Bloomlings.Content.Tests
{
    /// <summary>The embedded contract schemas, evaluated with JsonSchema.Net to cross-check the hand-written readers.</summary>
    internal static class ContractSchemas
    {
        private static readonly Lazy<JsonSchema> LevelSchema = new Lazy<JsonSchema>(() => Load(SchemaResources.LevelDefinition));
        private static readonly Lazy<JsonSchema> PictureSchema = new Lazy<JsonSchema>(() => Load(SchemaResources.BasePicture));
        private static readonly Lazy<JsonSchema> ManifestSchema = new Lazy<JsonSchema>(() => Load(SchemaResources.ContentManifest));

        public static bool IsValidLevel(string json) => IsValid(LevelSchema.Value, json);

        public static bool IsValidPicture(string json) => IsValid(PictureSchema.Value, json);

        public static bool IsValidManifest(string json) => IsValid(ManifestSchema.Value, json);

        public static string Sample(string fileName) =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", fileName));

        private static JsonSchema Load(string fileName) => JsonSchema.FromText(SchemaResources.Read(fileName));

        private static bool IsValid(JsonSchema schema, string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return schema.Evaluate(document.RootElement).IsValid;
        }
    }
}
