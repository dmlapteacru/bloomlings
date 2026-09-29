using System.Collections.Generic;
using System.IO;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>The inputs of one published content version.</summary>
    public sealed record PublishRequest(
        int ContentVersion,
        string MinAppVersion,
        int PictureLibraryVersion,
        int ShuffleNodeBudget,
        IReadOnlyList<LevelDefinition> Levels,
        IReadOnlyList<BasePicture> Pictures,
        IReadOnlyList<DailyPoolEntry> Daily);

    /// <summary>
    /// Writes the packs and <c>manifest.json</c> of a content version (T071, <c>publish</c>): paths relative to the
    /// output folder, SHA-256 and byte length per pack, and the fixed Shuffle node budget of the version (R10).
    /// </summary>
    public static class ManifestWriter
    {
        public const string ManifestFileName = "manifest.json";

        public static ContentManifest Build(PublishRequest request, out IReadOnlyList<PackFile> files)
        {
            var packs = new List<PackFile>();
            packs.Add(LevelPackWriter.PicturePack(request.Pictures, request.PictureLibraryVersion));
            packs.AddRange(LevelPackWriter.LevelPacks(request.Levels));
            if (request.Daily.Count > 0)
            {
                packs.Add(LevelPackWriter.DailyPack(request.Daily));
            }

            var entries = new List<PackEntry>();
            int maxLevel = 0;
            foreach (PackFile pack in packs)
            {
                entries.Add(pack.Entry);
                if (pack.Entry.LastLevel.HasValue && pack.Entry.LastLevel.Value > maxLevel)
                {
                    maxLevel = pack.Entry.LastLevel.Value;
                }
            }

            files = packs;
            return new ContentManifest(
                request.ContentVersion,
                request.MinAppVersion,
                request.PictureLibraryVersion,
                maxLevel == 0 ? (int?)null : maxLevel,
                request.ShuffleNodeBudget,
                entries);
        }

        /// <summary>Writes every pack and the manifest under <paramref name="outputFolder"/>.</summary>
        public static ContentManifest Publish(PublishRequest request, string outputFolder)
        {
            ContentManifest manifest = Build(request, out IReadOnlyList<PackFile> files);
            foreach (PackFile file in files)
            {
                string path = Path.Combine(outputFolder, file.Entry.Path!);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, file.Bytes);
            }

            File.WriteAllText(Path.Combine(outputFolder, ManifestFileName), manifest.Write());
            return manifest;
        }
    }
}
