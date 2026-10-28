using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The build's content (<see cref="PlaytestFiles"/>: the APK's assets, the preview's embedded resources):
    /// <list type="bullet">
    /// <item><description>the round levels (spec 006 FR-060, <see cref="LoadRounds"/>), the only levels the playtest APKs
    /// carry: the published round packs of <c>content/game/rounds/published</c> (<c>rounds publish</c>) under
    /// <c>rounds/</c>;</description></item>
    /// <item><description>the legacy levels and picture library (<see cref="Load"/>), which only the preview still embeds
    /// for its design board frames; the APKs no longer pack them, so there the set is empty.</description></item>
    /// </list>
    /// A level and its picture are read when a screen first asks for them, so nothing but the round manifest is parsed at
    /// start.
    /// </summary>
    public static class PlaytestContent
    {
        private const string LevelPrefix = "levels/level-";
        private const string PicturePrefix = "pictures/";
        private const string Json = ".json";

        /// <summary>Where a build carries the published round content: <c>rounds/manifest.json</c> and its packs.</summary>
        public const string RoundPrefix = "rounds/";

        /// <summary>Whether the build carries published round content (a <c>rounds/manifest.json</c>).</summary>
        public static bool HasRounds => PlaytestFiles.Has(RoundPrefix + RoundContentManifest.FileName);

        /// <summary>
        /// The build's round levels (spec 006 T025, T053): the manifest now, each pack on first use, checked against the
        /// manifest's SHA-256 (<see cref="RoundContentSet"/>).
        /// </summary>
        /// <exception cref="ContentIntegrityException">The build carries no round content, or it is inconsistent.</exception>
        public static RoundContentSet LoadRounds() => RoundContentSet.Read(path => PlaytestFiles.ReadBytes(RoundPrefix + path));

        /// <summary>
        /// The legacy levels (curated L1–10 plus <c>content/catalog/levels</c>) and picture library, as the preview embeds
        /// them for its design board frames. As with <c>publish --allow-draft</c>, pictures still waiting for approval
        /// (FR-084) are used as in-memory preview copies, so this content is for previews only. Empty in the APKs.
        /// </summary>
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
