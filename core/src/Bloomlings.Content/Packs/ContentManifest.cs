using System;
using System.Collections.Generic;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Packs
{
    public enum PackKind
    {
        Levels,
        Pictures,
        Daily,
    }

    /// <summary>One pack listed in the manifest. Exactly one of <see cref="Path"/> and <see cref="Url"/> is set.</summary>
    /// <param name="FirstLevel">First level of a <c>levels</c> pack (inclusive), from <c>levelRange</c>.</param>
    /// <param name="LastLevel">Last level of a <c>levels</c> pack (inclusive), from <c>levelRange</c>.</param>
    /// <param name="Sha256">Lowercase hex SHA-256 of the pack file as stored (gzip bytes).</param>
    public sealed record PackEntry(
        string Id,
        PackKind Kind,
        int? FirstLevel,
        int? LastLevel,
        string? Path,
        string? Url,
        string Sha256,
        long Bytes);

    /// <summary>
    /// <c>content-manifest.v1</c> (contracts/content-manifest.schema.json; research R5, R6). Describes one content
    /// version. <see cref="ShuffleNodeBudget"/> is fixed per content version so Shuffle gives the same result on every
    /// device (FR-024).
    /// </summary>
    public sealed record ContentManifest(
        int ContentVersion,
        string MinAppVersion,
        int PictureLibraryVersion,
        int? MaxLevel,
        int ShuffleNodeBudget,
        IReadOnlyList<PackEntry> Packs)
    {
        public const int MinShuffleNodeBudget = 1000;

        public const string PackFormat = "jsonl+gzip";

        public static ContentManifest Read(string json)
        {
            const string path = "";
            JObject root = JsonDoc.ParseObject(json, "manifest");
            JsonDoc.AllowOnly(
                root,
                path,
                "contentVersion",
                "minAppVersion",
                "pictureLibraryVersion",
                "maxLevel",
                "shuffleNodeBudget",
                "packs");

            int contentVersion = JsonDoc.Int(JsonDoc.Required(root, path, "contentVersion"), "contentVersion", min: 1);
            string minAppVersion = JsonDoc.String(JsonDoc.Required(root, path, "minAppVersion"), "minAppVersion");
            if (!IsSemVer(minAppVersion))
            {
                throw new ContentFormatException("minAppVersion", $"'{minAppVersion}' is not MAJOR.MINOR.PATCH");
            }

            int pictureLibraryVersion = JsonDoc.Int(
                JsonDoc.Required(root, path, "pictureLibraryVersion"),
                "pictureLibraryVersion",
                min: 1);
            JToken? maxLevelToken = JsonDoc.Optional(root, "maxLevel");
            int? maxLevel = maxLevelToken == null ? (int?)null : JsonDoc.Int(maxLevelToken, "maxLevel", min: 1);
            int budget = JsonDoc.Int(
                JsonDoc.Required(root, path, "shuffleNodeBudget"),
                "shuffleNodeBudget",
                min: MinShuffleNodeBudget);

            JArray packsArray = JsonDoc.Array(JsonDoc.Required(root, path, "packs"), "packs", minItems: 1);
            var packs = new PackEntry[packsArray.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < packsArray.Count; i++)
            {
                packs[i] = ReadPack(packsArray[i], JsonDoc.Index("packs", i));
                if (!ids.Add(packs[i].Id))
                {
                    throw new ContentFormatException(JsonDoc.Index("packs", i), $"duplicate pack id '{packs[i].Id}'");
                }
            }

            return new ContentManifest(contentVersion, minAppVersion, pictureLibraryVersion, maxLevel, budget, packs);
        }

        public string Write(bool indented = true)
        {
            var packs = new JArray();
            foreach (PackEntry pack in Packs)
            {
                var obj = new JObject
                {
                    ["id"] = pack.Id,
                    ["kind"] = KindNames.ToWire(pack.Kind),
                    ["sha256"] = pack.Sha256,
                    ["bytes"] = pack.Bytes,
                };
                if (pack.FirstLevel.HasValue && pack.LastLevel.HasValue)
                {
                    obj["levelRange"] = new JArray(pack.FirstLevel.Value, pack.LastLevel.Value);
                }

                if (pack.Path != null)
                {
                    obj["path"] = pack.Path;
                }

                if (pack.Url != null)
                {
                    obj["url"] = pack.Url;
                }

                packs.Add(obj);
            }

            var root = new JObject
            {
                ["contentVersion"] = ContentVersion,
                ["minAppVersion"] = MinAppVersion,
                ["pictureLibraryVersion"] = PictureLibraryVersion,
                ["shuffleNodeBudget"] = ShuffleNodeBudget,
                ["packs"] = packs,
            };
            if (MaxLevel.HasValue)
            {
                root["maxLevel"] = MaxLevel.Value;
            }

            return CanonicalJson.Write(root, indented);
        }

        private static readonly EnumNames<PackKind> KindNames = new EnumNames<PackKind>(
            (PackKind.Levels, "levels"),
            (PackKind.Pictures, "pictures"),
            (PackKind.Daily, "daily"));

        private static PackEntry ReadPack(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "id", "kind", "levelRange", "path", "url", "sha256", "bytes", "format");
            string id = JsonDoc.String(JsonDoc.Required(obj, path, "id"), JsonDoc.Join(path, "id"));
            PackKind kind = JsonDoc.Enum(JsonDoc.Required(obj, path, "kind"), JsonDoc.Join(path, "kind"), KindNames);

            int? first = null;
            int? last = null;
            JToken? rangeToken = JsonDoc.Optional(obj, "levelRange");
            if (rangeToken != null)
            {
                string rangePath = JsonDoc.Join(path, "levelRange");
                JArray range = JsonDoc.Array(rangeToken, rangePath, minItems: 2, maxItems: 2);
                first = JsonDoc.Int(range[0], JsonDoc.Index(rangePath, 0), min: 1);
                last = JsonDoc.Int(range[1], JsonDoc.Index(rangePath, 1), min: 1);
                if (last < first)
                {
                    throw new ContentFormatException(rangePath, $"[{first}, {last}] is not ascending");
                }
            }

            if (kind == PackKind.Levels && rangeToken == null)
            {
                throw new ContentFormatException(JsonDoc.Join(path, "levelRange"), "required for a levels pack");
            }

            JToken? pathToken = JsonDoc.Optional(obj, "path");
            JToken? urlToken = JsonDoc.Optional(obj, "url");
            if ((pathToken == null) == (urlToken == null))
            {
                throw new ContentFormatException(path, "exactly one of 'path' and 'url' is required");
            }

            string? packPath = pathToken == null ? null : JsonDoc.String(pathToken, JsonDoc.Join(path, "path"));
            string? url = urlToken == null ? null : JsonDoc.String(urlToken, JsonDoc.Join(path, "url"));
            if (url != null && !url.StartsWith("https://", StringComparison.Ordinal))
            {
                throw new ContentFormatException(JsonDoc.Join(path, "url"), "must be an https URL");
            }

            string shaPath = JsonDoc.Join(path, "sha256");
            string sha = JsonDoc.String(JsonDoc.Required(obj, path, "sha256"), shaPath);
            if (!PackIntegrity.IsSha256Hex(sha))
            {
                throw new ContentFormatException(shaPath, "expected 64 lowercase hex characters");
            }

            JToken bytesToken = JsonDoc.Required(obj, path, "bytes");
            if (bytesToken.Type != JTokenType.Integer || bytesToken.Value<long>() < 1)
            {
                throw new ContentFormatException(JsonDoc.Join(path, "bytes"), "expected an integer ≥ 1");
            }

            JToken? formatToken = JsonDoc.Optional(obj, "format");
            if (formatToken != null)
            {
                string format = JsonDoc.String(formatToken, JsonDoc.Join(path, "format"));
                if (!string.Equals(format, PackFormat, StringComparison.Ordinal))
                {
                    throw new ContentFormatException(JsonDoc.Join(path, "format"), $"'{format}' is not '{PackFormat}'");
                }
            }

            return new PackEntry(id, kind, first, last, packPath, url, sha, bytesToken.Value<long>());
        }

        private static bool IsSemVer(string text)
        {
            string[] parts = text.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Length == 0)
                {
                    return false;
                }

                foreach (char c in part)
                {
                    if (c < '0' || c > '9')
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
