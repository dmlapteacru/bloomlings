using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// Loads the build's levels (the whole catalog, <c>content/catalog/levels</c>) and the picture library
    /// (<see cref="PlaytestFiles"/>: the APK's assets, the preview's embedded resources) into a content set. A level and its
    /// picture are read when a screen first asks for them, so the 5000 levels and the 2300 pictures cost nothing at start
    /// (only their names are listed). As with <c>publish --allow-draft</c>, pictures still waiting for approval (FR-084) are
    /// used as in-memory preview copies, so this content is for playtests only.
    /// </summary>
    public static class PlaytestContent
    {
        private const string LevelPrefix = "levels/level-";
        private const string PicturePrefix = "pictures/";
        private const string Json = ".json";

        public static ContentSet Load()
        {
            var levels = new Dictionary<int, string>();
            foreach (string name in PlaytestFiles.Names("levels/"))
            {
                if (name.StartsWith(LevelPrefix, StringComparison.Ordinal) && name.EndsWith(Json, StringComparison.Ordinal))
                {
                    int number = int.Parse(name.Substring(LevelPrefix.Length, name.Length - LevelPrefix.Length - Json.Length), NumberStyles.None, CultureInfo.InvariantCulture);
                    levels.Add(number, name);
                }
            }

            // A library picture's file is named by its id (content/pictures/lib/<id>.json).
            var pictures = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string name in PlaytestFiles.Names(PicturePrefix))
            {
                if (name.EndsWith(Json, StringComparison.Ordinal))
                {
                    pictures.Add(name.Substring(PicturePrefix.Length, name.Length - PicturePrefix.Length - Json.Length), name);
                }
            }

            return new ContentSet(
                LooseContentFolder.DevContentVersion,
                LooseContentFolder.DevShuffleNodeBudget,
                levels.Keys,
                number => DefinitionJson.Read(ReadText(levels[number])),
                pictures.Keys,
                id =>
                {
                    BasePicture picture = BasePictureJson.Read(ReadText(pictures[id]));
                    return picture.Review.Status == ReviewStatus.Approved ? picture : picture with { Review = picture.Review with { Status = ReviewStatus.Approved } };
                });
        }

        private static string ReadText(string name) =>
            PlaytestFiles.ReadText(name) ?? throw new FileNotFoundException("The build does not carry " + name + ".", name);
    }
}
