using System.IO;
using System.Reflection;

namespace Bloomlings.Content.Schemas
{
    /// <summary>
    /// The four contract schemas embedded unchanged from <c>specs/001-core-game-mvp/contracts/</c>. The runtime readers
    /// validate by hand; tools use these with a JSON Schema validator.
    /// </summary>
    public static class SchemaResources
    {
        public const string LevelDefinition = "level-definition.schema.json";
        public const string BasePicture = "base-picture.schema.json";
        public const string ContentManifest = "content-manifest.schema.json";
        public const string PlayerSave = "player-save.schema.json";

        /// <summary>Returns the schema text, for example <c>Read(SchemaResources.LevelDefinition)</c>.</summary>
        public static string Read(string fileName)
        {
            Assembly assembly = typeof(SchemaResources).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream("Bloomlings.Content.Schemas." + fileName);
            if (stream == null)
            {
                throw new FileNotFoundException("Embedded schema not found.", fileName);
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
