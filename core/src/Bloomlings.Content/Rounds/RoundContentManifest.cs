using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Rounds
{
    /// <summary>The kind of a round pack.</summary>
    public enum RoundPackKind
    {
        Levels,
        Pictures,
    }

    /// <summary>One pack of a round content version: where it is, its SHA-256 and length, and its level range.</summary>
    public sealed class RoundPackEntry
    {
        public RoundPackEntry(string id, RoundPackKind kind, string path, string sha256, long bytes, int? firstLevel = null, int? lastLevel = null)
        {
            Id = id;
            Kind = kind;
            Path = path;
            Sha256 = sha256;
            Bytes = bytes;
            FirstLevel = firstLevel;
            LastLevel = lastLevel;
        }

        public string Id { get; }

        public RoundPackKind Kind { get; }

        /// <summary>Relative to the manifest, e.g. <c>levels/round_levels_0001_0250.jsonl.gz</c>.</summary>
        public string Path { get; }

        /// <summary>Lowercase hex SHA-256 of the pack file as stored (gzip bytes).</summary>
        public string Sha256 { get; }

        public long Bytes { get; }

        public int? FirstLevel { get; }

        public int? LastLevel { get; }

        /// <summary>Throws <see cref="ContentIntegrityException"/> unless the length and the hash both match.</summary>
        public void Verify(byte[] data)
        {
            if (data.LongLength != Bytes)
            {
                throw new ContentIntegrityException(Id, $"expected {Bytes} bytes, got {data.LongLength}");
            }

            string actual = PackIntegrity.ComputeSha256Hex(data);
            if (!string.Equals(actual, Sha256, StringComparison.Ordinal))
            {
                throw new ContentIntegrityException(Id, $"SHA-256 mismatch: expected {Sha256}, got {actual}");
            }
        }
    }

    /// <summary>A level as the manifest lists it: enough for a level list and its class badge without reading packs.</summary>
    public sealed class RoundManifestLevel
    {
        public RoundManifestLevel(int number, int definitionVersion, RoundClass roundClass, string pictureId, int pictureVersion)
        {
            Number = number;
            DefinitionVersion = definitionVersion;
            Class = roundClass;
            PictureId = pictureId;
            PictureVersion = pictureVersion;
        }

        public int Number { get; }

        public int DefinitionVersion { get; }

        public RoundClass Class { get; }

        public string PictureId { get; }

        public int PictureVersion { get; }
    }

    /// <summary>
    /// <c>round-content-manifest.v1</c> (<see cref="Schemas.SchemaResources.RoundContentManifest"/>): one published
    /// version of the round content, written by <c>rounds publish</c> as <c>manifest.json</c> next to its packs.
    /// </summary>
    public sealed class RoundContentManifest
    {
        public const string Format = "round-content-manifest.v1";

        public const string FileName = "manifest.json";

        private static readonly Regex PackId = new Regex("^[a-z0-9_]+$", RegexOptions.CultureInvariant);

        private static readonly Regex PackPath = new Regex(@"^(levels|pictures)/[a-z0-9_]+\.jsonl\.gz$", RegexOptions.CultureInvariant);

        private static readonly Regex PictureId = new Regex("^P[0-9]{4}$", RegexOptions.CultureInvariant);

        public RoundContentManifest(
            int contentVersion,
            RoundEngineVersion engine,
            IReadOnlyList<RoundManifestLevel> levels,
            IReadOnlyList<(string Id, int Version)> pictures,
            IReadOnlyList<RoundPackEntry> packs)
        {
            ContentVersion = contentVersion;
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
            Pictures = pictures ?? throw new ArgumentNullException(nameof(pictures));
            Packs = packs ?? throw new ArgumentNullException(nameof(packs));
        }

        public int ContentVersion { get; }

        /// <summary>The engine fingerprints every level of this version was proven on.</summary>
        public RoundEngineVersion Engine { get; }

        public IReadOnlyList<RoundManifestLevel> Levels { get; }

        public IReadOnlyList<(string Id, int Version)> Pictures { get; }

        public IReadOnlyList<RoundPackEntry> Packs { get; }

        public static RoundContentManifest Read(string json)
        {
            const string path = "";
            JObject root = JsonDoc.ParseObject(json, Format);
            JsonDoc.AllowOnly(root, path, "format", "contentVersion", "engine", "levels", "pictures", "packs");
            string format = JsonDoc.String(JsonDoc.Required(root, path, "format"), "format");
            if (!string.Equals(format, Format, StringComparison.Ordinal))
            {
                throw new ContentFormatException("format", $"must be '{Format}', got '{format}'");
            }

            int contentVersion = JsonDoc.Int(JsonDoc.Required(root, path, "contentVersion"), "contentVersion", min: 1);
            RoundEngineVersion engine = RoundJson.ReadEngine(JsonDoc.Required(root, path, "engine"), "engine");

            JArray levelsArray = JsonDoc.Array(JsonDoc.Required(root, path, "levels"), "levels", minItems: 1);
            var levels = new List<RoundManifestLevel>(levelsArray.Count);
            for (int i = 0; i < levelsArray.Count; i++)
            {
                string at = JsonDoc.Index("levels", i);
                JObject level = JsonDoc.Object(levelsArray[i], at);
                JsonDoc.AllowOnly(level, at, "number", "definitionVersion", "class", "picture");
                (string id, int version) = ReadPicture(JsonDoc.Required(level, at, "picture"), JsonDoc.Join(at, "picture"));
                string className = JsonDoc.String(JsonDoc.Required(level, at, "class"), JsonDoc.Join(at, "class"));
                RoundClass roundClass = className == "Normal" ? RoundClass.Normal
                    : className == "Hard" ? RoundClass.Hard
                    : className == "Super Hard" ? RoundClass.SuperHard
                    : throw new ContentFormatException(JsonDoc.Join(at, "class"), $"'{className}' is not one of: Normal, Hard, Super Hard");
                levels.Add(new RoundManifestLevel(
                    JsonDoc.Int(JsonDoc.Required(level, at, "number"), JsonDoc.Join(at, "number"), min: 1),
                    JsonDoc.Int(JsonDoc.Required(level, at, "definitionVersion"), JsonDoc.Join(at, "definitionVersion"), min: 1),
                    roundClass,
                    id,
                    version));
            }

            JArray picturesArray = JsonDoc.Array(JsonDoc.Required(root, path, "pictures"), "pictures", minItems: 1);
            var pictures = new List<(string, int)>(picturesArray.Count);
            for (int i = 0; i < picturesArray.Count; i++)
            {
                pictures.Add(ReadPicture(picturesArray[i], JsonDoc.Index("pictures", i)));
            }

            JArray packsArray = JsonDoc.Array(JsonDoc.Required(root, path, "packs"), "packs", minItems: 2);
            var packs = new List<RoundPackEntry>(packsArray.Count);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < packsArray.Count; i++)
            {
                string at = JsonDoc.Index("packs", i);
                JObject pack = JsonDoc.Object(packsArray[i], at);
                JsonDoc.AllowOnly(pack, at, "id", "kind", "path", "sha256", "bytes", "levelRange");
                string id = Matching(JsonDoc.Required(pack, at, "id"), JsonDoc.Join(at, "id"), PackId);
                if (!ids.Add(id))
                {
                    throw new ContentFormatException(JsonDoc.Join(at, "id"), $"pack '{id}' is listed twice");
                }

                string kind = JsonDoc.String(JsonDoc.Required(pack, at, "kind"), JsonDoc.Join(at, "kind"));
                if (kind != "levels" && kind != "pictures")
                {
                    throw new ContentFormatException(JsonDoc.Join(at, "kind"), $"'{kind}' is not one of: levels, pictures");
                }

                string sha = JsonDoc.String(JsonDoc.Required(pack, at, "sha256"), JsonDoc.Join(at, "sha256"));
                if (!PackIntegrity.IsSha256Hex(sha))
                {
                    throw new ContentFormatException(JsonDoc.Join(at, "sha256"), "expected 64 lowercase hex digits");
                }

                int? first = null;
                int? last = null;
                JToken? rangeToken = JsonDoc.Optional(pack, "levelRange");
                if (rangeToken != null)
                {
                    JArray range = JsonDoc.Array(rangeToken, JsonDoc.Join(at, "levelRange"), 2, 2);
                    first = JsonDoc.Int(range[0], JsonDoc.Index(JsonDoc.Join(at, "levelRange"), 0), min: 1);
                    last = JsonDoc.Int(range[1], JsonDoc.Index(JsonDoc.Join(at, "levelRange"), 1), min: first.Value);
                }

                packs.Add(new RoundPackEntry(
                    id,
                    kind == "levels" ? RoundPackKind.Levels : RoundPackKind.Pictures,
                    Matching(JsonDoc.Required(pack, at, "path"), JsonDoc.Join(at, "path"), PackPath),
                    sha,
                    JsonDoc.Int(JsonDoc.Required(pack, at, "bytes"), JsonDoc.Join(at, "bytes"), min: 1),
                    first,
                    last));
            }

            return new RoundContentManifest(contentVersion, engine, levels, pictures, packs);
        }

        public string Write()
        {
            var levels = new JArray();
            foreach (RoundManifestLevel level in Levels)
            {
                levels.Add(new JObject
                {
                    ["number"] = level.Number,
                    ["definitionVersion"] = level.DefinitionVersion,
                    ["class"] = RoundJson.ClassName(level.Class),
                    ["picture"] = new JObject { ["id"] = level.PictureId, ["version"] = level.PictureVersion },
                });
            }

            var pictures = new JArray();
            foreach ((string id, int version) in Pictures)
            {
                pictures.Add(new JObject { ["id"] = id, ["version"] = version });
            }

            var packs = new JArray();
            foreach (RoundPackEntry pack in Packs)
            {
                var item = new JObject
                {
                    ["id"] = pack.Id,
                    ["kind"] = pack.Kind == RoundPackKind.Levels ? "levels" : "pictures",
                    ["path"] = pack.Path,
                    ["sha256"] = pack.Sha256,
                    ["bytes"] = pack.Bytes,
                };
                if (pack.FirstLevel.HasValue && pack.LastLevel.HasValue)
                {
                    item["levelRange"] = new JArray(pack.FirstLevel.Value, pack.LastLevel.Value);
                }

                packs.Add(item);
            }

            return CanonicalJson.Write(new JObject
            {
                ["format"] = Format,
                ["contentVersion"] = ContentVersion,
                ["engine"] = RoundJson.WriteEngine(Engine),
                ["levels"] = levels,
                ["pictures"] = pictures,
                ["packs"] = packs,
            }, indented: true);
        }

        private static (string Id, int Version) ReadPicture(JToken token, string path)
        {
            JObject picture = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(picture, path, "id", "version");
            return (
                Matching(JsonDoc.Required(picture, path, "id"), JsonDoc.Join(path, "id"), PictureId),
                JsonDoc.Int(JsonDoc.Required(picture, path, "version"), JsonDoc.Join(path, "version"), min: 1));
        }

        private static string Matching(JToken token, string path, Regex pattern)
        {
            string value = JsonDoc.String(token, path);
            if (!pattern.IsMatch(value))
            {
                throw new ContentFormatException(path, $"'{value}' must match {pattern}");
            }

            return value;
        }
    }

    /// <summary>One round pack ready to be written: its manifest entry and its bytes.</summary>
    public sealed class RoundPackFile
    {
        public RoundPackFile(RoundPackEntry entry, byte[] bytes)
        {
            Entry = entry;
            Bytes = bytes;
        }

        public RoundPackEntry Entry { get; }

        public byte[] Bytes { get; }
    }

    /// <summary>
    /// The round packs (research R11, following the legacy packs of R5): gzip JSON Lines with a normalized header, one
    /// compact canonical document per line, so the bytes are the same on every run. Level packs hold 250 consecutive level
    /// numbers in ascending order (<c>levels/round_levels_0001_0250.jsonl.gz</c>, …); the picture pack holds the pictures
    /// the levels use, sorted by id and version (<c>pictures/round_pictures.jsonl.gz</c>).
    /// </summary>
    public static class RoundPacks
    {
        public const int LevelsPerPack = 250;

        public const string PicturePackId = "round_pictures";

        /// <summary>Builds the manifest and the pack files of one content version.</summary>
        public static RoundContentManifest Build(int contentVersion, IReadOnlyList<RoundLevel> levels, IReadOnlyList<RoundPicture> pictures, out IReadOnlyList<RoundPackFile> files)
        {
            if (levels.Count == 0)
            {
                throw new ArgumentException("A round content version needs at least one level.", nameof(levels));
            }

            RoundEngineVersion engine = levels[0].Engine ?? throw new ArgumentException($"L{levels[0].Number} has no engine fingerprints.", nameof(levels));
            var sortedLevels = new List<RoundLevel>(levels);
            sortedLevels.Sort((a, b) => a.Number.CompareTo(b.Number));
            var sortedPictures = new List<RoundPicture>(pictures);
            sortedPictures.Sort((a, b) =>
            {
                int byId = string.CompareOrdinal(a.Id, b.Id);
                return byId != 0 ? byId : a.Version.CompareTo(b.Version);
            });

            var packs = new List<RoundPackFile>(LevelPacks(sortedLevels)) { PicturePack(sortedPictures) };
            var entries = new List<RoundPackEntry>();
            foreach (RoundPackFile pack in packs)
            {
                entries.Add(pack.Entry);
            }

            var listed = new List<RoundManifestLevel>();
            foreach (RoundLevel level in sortedLevels)
            {
                listed.Add(new RoundManifestLevel(level.Number, level.DefinitionVersion, level.Class, level.PictureId!, level.PictureVersion));
            }

            var pictureRefs = new List<(string, int)>();
            foreach (RoundPicture picture in sortedPictures)
            {
                pictureRefs.Add((picture.Id, picture.Version));
            }

            files = packs;
            return new RoundContentManifest(contentVersion, engine, listed, pictureRefs, entries);
        }

        public static IReadOnlyList<RoundPackFile> LevelPacks(IReadOnlyList<RoundLevel> sortedLevels)
        {
            var packs = new List<RoundPackFile>();
            int i = 0;
            while (i < sortedLevels.Count)
            {
                int block = (sortedLevels[i].Number - 1) / LevelsPerPack;
                var lines = new List<string>();
                int first = sortedLevels[i].Number;
                int last = first;
                while (i < sortedLevels.Count && (sortedLevels[i].Number - 1) / LevelsPerPack == block)
                {
                    if (lines.Count > 0 && sortedLevels[i].Number != last + 1)
                    {
                        throw new ArgumentException($"Round levels must be contiguous; level {last + 1} is missing.", nameof(sortedLevels));
                    }

                    last = sortedLevels[i].Number;
                    lines.Add(RoundJson.WriteLevel(sortedLevels[i++], indented: false));
                }

                string id = $"round_levels_{first:0000}_{last:0000}";
                byte[] bytes = JsonLinesPack.Compress(lines);
                packs.Add(new RoundPackFile(new RoundPackEntry(id, RoundPackKind.Levels, $"levels/{id}.jsonl.gz", PackIntegrity.ComputeSha256Hex(bytes), bytes.LongLength, first, last), bytes));
            }

            return packs;
        }

        public static RoundPackFile PicturePack(IReadOnlyList<RoundPicture> sortedPictures)
        {
            var lines = new List<string>(sortedPictures.Count);
            foreach (RoundPicture picture in sortedPictures)
            {
                lines.Add(RoundJson.WritePicture(picture, indented: false));
            }

            byte[] bytes = JsonLinesPack.Compress(lines);
            return new RoundPackFile(new RoundPackEntry(PicturePackId, RoundPackKind.Pictures, $"pictures/{PicturePackId}.jsonl.gz", PackIntegrity.ComputeSha256Hex(bytes), bytes.LongLength), bytes);
        }

        /// <summary>Reads a level pack after checking its hash and length.</summary>
        public static IReadOnlyList<RoundLevel> ReadLevels(RoundPackEntry entry, byte[] bytes)
        {
            entry.Verify(bytes);
            var levels = new List<RoundLevel>();
            IReadOnlyList<string> lines = JsonLinesPack.ReadLines(bytes, entry.Id);
            for (int i = 0; i < lines.Count; i++)
            {
                string where = $"{entry.Id}:{i + 1}";
                levels.Add(RoundJson.ReadLevel(JsonDoc.ParseObject(lines[i], where), where));
            }

            return levels;
        }

        /// <summary>Reads the picture pack after checking its hash and length.</summary>
        public static IReadOnlyList<RoundPicture> ReadPictures(RoundPackEntry entry, byte[] bytes)
        {
            entry.Verify(bytes);
            var pictures = new List<RoundPicture>();
            IReadOnlyList<string> lines = JsonLinesPack.ReadLines(bytes, entry.Id);
            for (int i = 0; i < lines.Count; i++)
            {
                string where = $"{entry.Id}:{i + 1}";
                pictures.Add(RoundJson.ReadPicture(JsonDoc.ParseObject(lines[i], where), where));
            }

            return pictures;
        }
    }
}
