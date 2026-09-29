using System;
using System.Collections.Generic;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Packs
{
    /// <summary>One pack file ready to be written: its manifest entry and its bytes.</summary>
    public sealed record PackFile(PackEntry Entry, byte[] Bytes);

    /// <summary>A Daily Challenge pool entry (data-model §1.6; R19).</summary>
    public sealed record DailyPoolEntry(int Index, LevelDefinition Level);

    /// <summary>
    /// Builds the content packs (T071, research R5): level packs of 250 consecutive level numbers
    /// (<c>levels_0001_0250</c>, …) in ascending order, the picture pack sorted by id and version, and the daily pack.
    /// Every pack is gzip JSON Lines with a normalized header, so the bytes are the same on every run.
    /// </summary>
    public static class LevelPackWriter
    {
        public const int LevelsPerPack = 250;

        public static IReadOnlyList<PackFile> LevelPacks(IEnumerable<LevelDefinition> levels)
        {
            var sorted = new List<LevelDefinition>(levels);
            sorted.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            var packs = new List<PackFile>();
            int i = 0;
            while (i < sorted.Count)
            {
                int block = (sorted[i].LevelNumber - 1) / LevelsPerPack;
                var chunk = new List<LevelDefinition>();
                while (i < sorted.Count && (sorted[i].LevelNumber - 1) / LevelsPerPack == block)
                {
                    if (chunk.Count > 0 && sorted[i].LevelNumber != chunk[chunk.Count - 1].LevelNumber + 1)
                    {
                        throw new ArgumentException($"Levels must be contiguous; level {chunk[chunk.Count - 1].LevelNumber + 1} is missing.", nameof(levels));
                    }

                    chunk.Add(sorted[i++]);
                }

                int first = chunk[0].LevelNumber;
                int last = chunk[chunk.Count - 1].LevelNumber;
                string id = $"levels_{first:0000}_{last:0000}";
                byte[] bytes = LevelPackReader.Write(chunk);
                packs.Add(new PackFile(new PackEntry(id, PackKind.Levels, first, last, $"levels/{id}.jsonl.gz", null, PackIntegrity.ComputeSha256Hex(bytes), bytes.LongLength), bytes));
            }

            return packs;
        }

        public static PackFile PicturePack(IEnumerable<BasePicture> pictures, int pictureLibraryVersion)
        {
            var sorted = new List<BasePicture>(pictures);
            sorted.Sort((a, b) =>
            {
                int byId = string.CompareOrdinal(a.Id, b.Id);
                return byId != 0 ? byId : a.Version.CompareTo(b.Version);
            });
            byte[] bytes = PicturePackReader.Write(sorted);
            string id = $"pictures_v{pictureLibraryVersion}";
            return new PackFile(new PackEntry(id, PackKind.Pictures, null, null, $"pictures/{id}.jsonl.gz", null, PackIntegrity.ComputeSha256Hex(bytes), bytes.LongLength), bytes);
        }

        public static PackFile DailyPack(IEnumerable<DailyPoolEntry> pool, string id = "daily")
        {
            var sorted = new List<DailyPoolEntry>(pool);
            sorted.Sort((a, b) => a.Index.CompareTo(b.Index));
            var lines = new List<string>();
            foreach (DailyPoolEntry entry in sorted)
            {
                lines.Add(CanonicalJson.Write(new JObject { ["index"] = entry.Index, ["level"] = DefinitionJson.ToJObject(entry.Level) }, indented: false));
            }

            byte[] bytes = JsonLinesPack.Compress(lines);
            return new PackFile(new PackEntry(id, PackKind.Daily, null, null, $"daily/{id}.jsonl.gz", null, PackIntegrity.ComputeSha256Hex(bytes), bytes.LongLength), bytes);
        }

        /// <summary>Reads a daily pack written by <see cref="DailyPack"/>.</summary>
        public static IReadOnlyList<DailyPoolEntry> ReadDaily(PackEntry entry, byte[] bytes)
        {
            PackIntegrity.Verify(entry, bytes);
            var pool = new List<DailyPoolEntry>();
            IReadOnlyList<string> lines = JsonLinesPack.ReadLines(bytes, entry.Id);
            for (int i = 0; i < lines.Count; i++)
            {
                string where = $"{entry.Id}:{i + 1}";
                JObject line = JsonDoc.ParseObject(lines[i], where);
                JsonDoc.AllowOnly(line, where, "index", "level");
                pool.Add(new DailyPoolEntry(
                    JsonDoc.Int(JsonDoc.Required(line, where, "index"), JsonDoc.Join(where, "index"), min: 0),
                    DefinitionJson.Read(JsonDoc.Object(JsonDoc.Required(line, where, "level"), JsonDoc.Join(where, "level")), JsonDoc.Join(where, "level"))));
            }

            return pool;
        }
    }
}
