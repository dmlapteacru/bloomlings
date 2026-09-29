using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using UnityEngine;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// Editor-only access to the authoring data in the repository's <c>content/</c> folder, so a dev level can be
    /// played straight from the Gameplay scene without Boot (Tools/Bloomlings/Play Dev Level, T052).
    /// </summary>
    public static class DevContent
    {
        /// <summary>EditorPrefs key holding the level file chosen in the Play Dev Level menu.</summary>
        public const string SelectedLevelKey = "Bloomlings.DevLevelFile";

        public static string RepositoryContentFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content"));

        public static string DefaultLevelFile => Path.Combine(RepositoryContentFolder, "curated", "dev", "level-dev-001.json");

        /// <summary>Loads a level file and the picture it references from <c>content/pictures/lib</c>.</summary>
        public static (LevelDefinition Level, BasePicture Picture) Load(string levelFile)
        {
            LevelDefinition level = DefinitionJson.Read(File.ReadAllText(levelFile));
            string picturePath = Path.Combine(RepositoryContentFolder, "pictures", "lib", level.Picture.Id + ".json");
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(picturePath));
            return (level, picture);
        }

        /// <summary>
        /// The curated levels (<c>content/curated/level-*.json</c>) with the picture library, as loose development
        /// content. Boot uses it in the Editor until the pipeline publishes packs (US3).
        /// </summary>
        public static ContentSet LoadCurated()
        {
            string curated = Path.Combine(RepositoryContentFolder, "curated");
            string pictures = Path.Combine(RepositoryContentFolder, "pictures", "lib");
            return LooseContentFolder.FromDocuments(Read(pictures, "*.json"), Read(curated, "level-*.json"));
        }

        private static IEnumerable<KeyValuePair<string, string>> Read(string folder, string pattern)
        {
            if (!Directory.Exists(folder))
            {
                yield break;
            }

            string[] files = Directory.GetFiles(folder, pattern);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files)
            {
                yield return new KeyValuePair<string, string>(Path.GetFileName(file), File.ReadAllText(file));
            }
        }

        /// <summary>The level chosen in the editor menu, or dev level 1.</summary>
        public static (LevelDefinition Level, BasePicture Picture) LoadSelected()
        {
#if UNITY_EDITOR
            string file = UnityEditor.EditorPrefs.GetString(SelectedLevelKey, DefaultLevelFile);
            if (!File.Exists(file))
            {
                file = DefaultLevelFile;
            }

            return Load(file);
#else
            throw new System.InvalidOperationException("Dev content is only available in the Editor.");
#endif
        }
    }
}
