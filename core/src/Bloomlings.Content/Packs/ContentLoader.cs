using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>
    /// Builds a <see cref="ContentSet"/> from a manifest and its packs. Reading the bytes is left to the caller
    /// (StreamingAssets, UnityWebRequest on Android, a CDN download or a file in tools), so this stays engine-free.
    /// Every pack is verified before any of it is used (FR-078).
    /// </summary>
    public static class ContentLoader
    {
        /// <param name="readPack">Returns the raw bytes of a pack listed in the manifest.</param>
        public static ContentSet FromPacks(ContentManifest manifest, Func<PackEntry, byte[]> readPack)
        {
            var levels = new List<LevelDefinition>();
            var pictures = new List<BasePicture>();
            var daily = new List<DailyPoolEntry>();
            foreach (PackEntry entry in manifest.Packs)
            {
                switch (entry.Kind)
                {
                    case PackKind.Levels:
                        levels.AddRange(LevelPackReader.Read(entry, readPack(entry)));
                        break;
                    case PackKind.Pictures:
                        pictures.AddRange(PicturePackReader.Read(entry, readPack(entry)));
                        break;
                    case PackKind.Daily:
                        // The Daily Challenge pool (R19); it never changes the main level sequence.
                        daily.AddRange(LevelPackWriter.ReadDaily(entry, readPack(entry)));
                        break;
                }
            }

            var set = new ContentSet(manifest.ContentVersion, manifest.ShuffleNodeBudget, levels, pictures, daily);
            if (manifest.MaxLevel.HasValue && set.MaxLevel != manifest.MaxLevel.Value)
            {
                throw new ContentIntegrityException("manifest", $"maxLevel is {manifest.MaxLevel.Value}, the packs end at {set.MaxLevel}");
            }

            return set;
        }
    }
}
