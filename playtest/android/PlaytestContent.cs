using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// Loads the embedded levels (the whole catalog, <c>content/catalog/levels</c>) and the picture library into a content
    /// set. A level is read when a screen first asks for it, so the 5000 levels cost nothing at start. As with
    /// <c>publish --allow-draft</c>, pictures still waiting for approval (FR-084) are used as in-memory preview copies,
    /// so this content is for playtests only.
    /// </summary>
    public static class PlaytestContent
    {
        private const string LevelPrefix = "levels/level-";

        public static ContentSet Load()
        {
            var pictures = new List<BasePicture>();
            var levels = new Dictionary<int, string>();
            Assembly assembly = typeof(PlaytestContent).Assembly;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.StartsWith(LevelPrefix, StringComparison.Ordinal))
                {
                    int number = int.Parse(name.Substring(LevelPrefix.Length, name.Length - LevelPrefix.Length - ".json".Length), NumberStyles.None, CultureInfo.InvariantCulture);
                    levels.Add(number, name);
                    continue;
                }

                if (!name.StartsWith("pictures/", StringComparison.Ordinal))
                {
                    continue;
                }

                BasePicture picture = BasePictureJson.Read(ReadText(assembly, name));
                pictures.Add(picture.Review.Status == ReviewStatus.Approved ? picture : picture with { Review = picture.Review with { Status = ReviewStatus.Approved } });
            }

            return new ContentSet(
                LooseContentFolder.DevContentVersion,
                LooseContentFolder.DevShuffleNodeBudget,
                levels.Keys,
                number => DefinitionJson.Read(ReadText(assembly, levels[number])),
                pictures);
        }

        private static string ReadText(Assembly assembly, string name)
        {
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
