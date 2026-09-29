using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Json
{
    /// <summary>
    /// Reads and writes <c>base-picture.v1</c> documents (contracts/base-picture.schema.json). Besides the schema, the
    /// reader checks the grid shape (<c>height</c> rows of <c>width</c> cells, role indexes inside <c>roles</c>),
    /// because the rest of the core relies on it. <c>structure</c> is written by the pipeline with all three metrics,
    /// and <c>backgroundShare</c> must have at most three decimals (it is stored in per mille).
    /// </summary>
    public static class BasePictureJson
    {
        public static BasePicture Read(string json) => Read(JsonDoc.ParseObject(json, "picture"), string.Empty);

        public static string Write(BasePicture picture, bool indented = true) =>
            CanonicalJson.Write(ToJObject(picture), indented);

        internal static BasePicture Read(JObject root, string path)
        {
            JsonDoc.AllowOnly(
                root,
                path,
                "id",
                "version",
                "subject",
                "width",
                "height",
                "roles",
                "grid",
                "finishedLook",
                "tags",
                "structure",
                "review",
                "source");

            string id = JsonDoc.Id(JsonDoc.Required(root, path, "id"), JsonDoc.Join(path, "id"));
            int version = JsonDoc.Int(JsonDoc.Required(root, path, "version"), JsonDoc.Join(path, "version"), min: 1);
            string subject = JsonDoc.String(JsonDoc.Required(root, path, "subject"), JsonDoc.Join(path, "subject"));
            if (subject.Length < 2)
            {
                throw new ContentFormatException(JsonDoc.Join(path, "subject"), "must have at least 2 characters");
            }

            int width = JsonDoc.Int(
                JsonDoc.Required(root, path, "width"),
                JsonDoc.Join(path, "width"),
                BasePicture.MinWidth,
                BasePicture.MaxWidth);
            int height = JsonDoc.Int(
                JsonDoc.Required(root, path, "height"),
                JsonDoc.Join(path, "height"),
                BasePicture.MinHeight,
                BasePicture.MaxHeight);
            IReadOnlyList<PictureRole> roles = ReadRoles(JsonDoc.Required(root, path, "roles"), JsonDoc.Join(path, "roles"));
            IReadOnlyList<IReadOnlyList<int>> grid = ReadGrid(
                JsonDoc.Required(root, path, "grid"),
                JsonDoc.Join(path, "grid"),
                width,
                height,
                roles.Count);
            FinishedLook finishedLook = ReadFinishedLook(
                JsonDoc.Required(root, path, "finishedLook"),
                JsonDoc.Join(path, "finishedLook"));
            PictureTags tags = ReadTags(JsonDoc.Required(root, path, "tags"), JsonDoc.Join(path, "tags"));
            JToken? structureToken = JsonDoc.Optional(root, "structure");
            PictureStructure? structure = structureToken == null
                ? null
                : ReadStructure(structureToken, JsonDoc.Join(path, "structure"));
            PictureReview review = ReadReview(JsonDoc.Required(root, path, "review"), JsonDoc.Join(path, "review"));
            PictureSource source = ReadSource(JsonDoc.Required(root, path, "source"), JsonDoc.Join(path, "source"));

            return new BasePicture(id, version, subject, width, height, roles, grid, finishedLook, tags, structure, review, source);
        }

        internal static JObject ToJObject(BasePicture picture)
        {
            var roles = new JArray();
            foreach (PictureRole role in picture.Roles)
            {
                var obj = new JObject
                {
                    ["roleId"] = role.RoleId,
                    ["name"] = role.Name,
                    ["colorGroup"] = WireNames.ColorGroups.ToWire(role.ColorGroup),
                };
                if (role.IsBackground)
                {
                    obj["isBackground"] = true;
                }

                roles.Add(obj);
            }

            var grid = new JArray();
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                var cells = new JArray();
                foreach (int cell in row)
                {
                    cells.Add(cell);
                }

                grid.Add(cells);
            }

            var finishedLook = new JObject { ["mode"] = WireNames.FinishedLookModes.ToWire(picture.FinishedLook.Mode) };
            AddString(finishedLook, "illustrationKey", picture.FinishedLook.IllustrationKey);

            var tags = new JObject();
            AddStrings(tags, "themes", picture.Tags.Themes);
            AddStrings(tags, "seasons", picture.Tags.Seasons);
            AddStrings(tags, "bands", picture.Tags.Bands);

            var review = new JObject { ["status"] = WireNames.ReviewStatuses.ToWire(picture.Review.Status) };
            AddString(review, "reviewer", picture.Review.Reviewer);
            AddString(review, "date", picture.Review.Date);
            AddString(review, "notes", picture.Review.Notes);

            var source = new JObject
            {
                ["kind"] = WireNames.SourceKinds.ToWire(picture.Source.Kind),
                ["licence"] = picture.Source.Licence,
            };
            AddString(source, "origin", picture.Source.Origin);

            var root = new JObject
            {
                ["id"] = picture.Id,
                ["version"] = picture.Version,
                ["subject"] = picture.Subject,
                ["width"] = picture.Width,
                ["height"] = picture.Height,
                ["roles"] = roles,
                ["grid"] = grid,
                ["finishedLook"] = finishedLook,
                ["tags"] = tags,
                ["review"] = review,
                ["source"] = source,
            };

            if (picture.Structure != null)
            {
                root["structure"] = new JObject
                {
                    ["regionCount"] = picture.Structure.RegionCount,
                    ["nestingDepth"] = picture.Structure.NestingDepth,
                    ["backgroundShare"] = new JValue(picture.Structure.BackgroundSharePermille / 1000m),
                };
            }

            return root;
        }

        private static IReadOnlyList<PictureRole> ReadRoles(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path, minItems: 2);
            var roles = new PictureRole[array.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                JObject obj = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(obj, itemPath, "roleId", "name", "colorGroup", "isBackground");
                string roleId = JsonDoc.Id(JsonDoc.Required(obj, itemPath, "roleId"), JsonDoc.Join(itemPath, "roleId"));
                if (!seen.Add(roleId))
                {
                    throw new ContentFormatException(JsonDoc.Join(itemPath, "roleId"), $"duplicate role '{roleId}'");
                }

                string name = JsonDoc.String(JsonDoc.Required(obj, itemPath, "name"), JsonDoc.Join(itemPath, "name"));
                ColorGroup colorGroup = JsonDoc.Enum(
                    JsonDoc.Required(obj, itemPath, "colorGroup"),
                    JsonDoc.Join(itemPath, "colorGroup"),
                    WireNames.ColorGroups);
                JToken? backgroundToken = JsonDoc.Optional(obj, "isBackground");
                bool isBackground = backgroundToken != null && JsonDoc.Bool(backgroundToken, JsonDoc.Join(itemPath, "isBackground"));
                roles[i] = new PictureRole(roleId, name, colorGroup, isBackground);
            }

            return roles;
        }

        private static IReadOnlyList<IReadOnlyList<int>> ReadGrid(JToken token, string path, int width, int height, int roleCount)
        {
            JArray rows = JsonDoc.Array(token, path, minItems: height, maxItems: height);
            var grid = new IReadOnlyList<int>[height];
            for (int y = 0; y < height; y++)
            {
                string rowPath = JsonDoc.Index(path, y);
                JArray row = JsonDoc.Array(rows[y], rowPath, minItems: width, maxItems: width);
                var cells = new int[width];
                for (int x = 0; x < width; x++)
                {
                    cells[x] = JsonDoc.Int(row[x], JsonDoc.Index(rowPath, x), min: BasePicture.Stone, max: roleCount - 1);
                }

                grid[y] = cells;
            }

            return grid;
        }

        private static FinishedLook ReadFinishedLook(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "mode", "illustrationKey");
            FinishedLookMode mode = JsonDoc.Enum(
                JsonDoc.Required(obj, path, "mode"),
                JsonDoc.Join(path, "mode"),
                WireNames.FinishedLookModes);
            return new FinishedLook(mode, OptionalString(obj, path, "illustrationKey"));
        }

        private static PictureTags ReadTags(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "themes", "seasons", "bands");
            return new PictureTags(
                OptionalStrings(obj, path, "themes"),
                OptionalStrings(obj, path, "seasons"),
                OptionalStrings(obj, path, "bands"));
        }

        private static PictureStructure ReadStructure(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "regionCount", "nestingDepth", "backgroundShare");
            int regionCount = JsonDoc.Int(JsonDoc.Required(obj, path, "regionCount"), JsonDoc.Join(path, "regionCount"), min: 1);
            int nestingDepth = JsonDoc.Int(JsonDoc.Required(obj, path, "nestingDepth"), JsonDoc.Join(path, "nestingDepth"), min: 1);
            string sharePath = JsonDoc.Join(path, "backgroundShare");
            decimal share = JsonDoc.Number(JsonDoc.Required(obj, path, "backgroundShare"), sharePath, 0m, 1m);
            decimal permille = share * 1000m;
            if (permille != decimal.Truncate(permille))
            {
                throw new ContentFormatException(sharePath, $"{share} has more than 3 decimals");
            }

            return new PictureStructure(regionCount, nestingDepth, (int)permille);
        }

        private static PictureReview ReadReview(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "status", "reviewer", "date", "notes");
            ReviewStatus status = JsonDoc.Enum(
                JsonDoc.Required(obj, path, "status"),
                JsonDoc.Join(path, "status"),
                WireNames.ReviewStatuses);
            string? date = OptionalString(obj, path, "date");
            if (date != null && !IsIsoDate(date))
            {
                throw new ContentFormatException(JsonDoc.Join(path, "date"), $"'{date}' is not a YYYY-MM-DD date");
            }

            return new PictureReview(status, OptionalString(obj, path, "reviewer"), date, OptionalString(obj, path, "notes"));
        }

        private static PictureSource ReadSource(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "kind", "origin", "licence");
            PictureSourceKind kind = JsonDoc.Enum(
                JsonDoc.Required(obj, path, "kind"),
                JsonDoc.Join(path, "kind"),
                WireNames.SourceKinds);
            string licence = JsonDoc.String(JsonDoc.Required(obj, path, "licence"), JsonDoc.Join(path, "licence"));
            return new PictureSource(kind, OptionalString(obj, path, "origin"), licence);
        }

        private static bool IsIsoDate(string text)
        {
            if (text.Length != 10 || text[4] != '-' || text[7] != '-')
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (i != 4 && i != 7 && (text[i] < '0' || text[i] > '9'))
                {
                    return false;
                }
            }

            int month = ((text[5] - '0') * 10) + (text[6] - '0');
            int day = ((text[8] - '0') * 10) + (text[9] - '0');
            return month >= 1 && month <= 12 && day >= 1 && day <= 31;
        }

        private static string? OptionalString(JObject obj, string path, string name)
        {
            JToken? token = JsonDoc.Optional(obj, name);
            return token == null ? null : JsonDoc.String(token, JsonDoc.Join(path, name));
        }

        private static IReadOnlyList<string> OptionalStrings(JObject obj, string path, string name)
        {
            JToken? token = JsonDoc.Optional(obj, name);
            if (token == null)
            {
                return Array.Empty<string>();
            }

            string listPath = JsonDoc.Join(path, name);
            JArray array = JsonDoc.Array(token, listPath);
            var values = new string[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                values[i] = JsonDoc.String(array[i], JsonDoc.Index(listPath, i));
            }

            return values;
        }

        private static void AddString(JObject obj, string name, string? value)
        {
            if (value != null)
            {
                obj[name] = value;
            }
        }

        private static void AddStrings(JObject obj, string name, IReadOnlyList<string> values)
        {
            if (values.Count == 0)
            {
                return;
            }

            var array = new JArray();
            foreach (string value in values)
            {
                array.Add(value);
            }

            obj[name] = array;
        }
    }
}
