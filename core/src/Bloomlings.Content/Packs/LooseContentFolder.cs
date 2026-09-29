using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>
    /// Development mode: one JSON file per level and per picture, used until the <c>publish</c> command exists (US3).
    /// Layout: <c>&lt;root&gt;/pictures/*.json</c> and <c>&lt;root&gt;/levels/*.json</c>, read in ordinal file-name order.
    /// Loose content has content version 0 and the development Shuffle budget.
    /// </summary>
    public static class LooseContentFolder
    {
        public const int DevContentVersion = 0;

        /// <summary>Shuffle node budget for loose content; published content takes it from the manifest.</summary>
        public const int DevShuffleNodeBudget = 20000;

        public const string PicturesFolder = "pictures";

        public const string LevelsFolder = "levels";

        public static ContentSet Load(string root)
        {
            return FromDocuments(
                ReadFolder(Path.Combine(root, PicturesFolder)),
                ReadFolder(Path.Combine(root, LevelsFolder)));
        }

        /// <param name="pictures">(file name, JSON text) per picture.</param>
        /// <param name="levels">(file name, JSON text) per level.</param>
        public static ContentSet FromDocuments(
            IEnumerable<KeyValuePair<string, string>> pictures,
            IEnumerable<KeyValuePair<string, string>> levels)
        {
            var parsedPictures = new List<BasePicture>();
            foreach (KeyValuePair<string, string> document in pictures)
            {
                parsedPictures.Add(BasePictureJson.Read(JsonDoc.ParseObject(document.Value, document.Key), document.Key));
            }

            var parsedLevels = new List<LevelDefinition>();
            foreach (KeyValuePair<string, string> document in levels)
            {
                parsedLevels.Add(DefinitionJson.Read(JsonDoc.ParseObject(document.Value, document.Key), document.Key));
            }

            return new ContentSet(DevContentVersion, DevShuffleNodeBudget, parsedLevels, parsedPictures);
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return Array.Empty<KeyValuePair<string, string>>();
            }

            string[] files = Directory.GetFiles(folder, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            var documents = new List<KeyValuePair<string, string>>(files.Length);
            foreach (string file in files)
            {
                documents.Add(new KeyValuePair<string, string>(Path.GetFileName(file), File.ReadAllText(file)));
            }

            return documents;
        }
    }
}
