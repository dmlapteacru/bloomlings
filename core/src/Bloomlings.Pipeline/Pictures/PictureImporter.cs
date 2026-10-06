using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Pictures
{
    /// <summary>The outcome of importing one source picture.</summary>
    public sealed record ImportedPicture(string Id, string? OutputPath, string? Error);

    /// <summary>
    /// <c>pictures import</c> (T073): reads <c>&lt;id&gt;.meta.json</c> (base-picture.v1 without <c>grid</c> and
    /// <c>structure</c>, plus an optional <c>legend</c>) with <c>&lt;id&gt;.png</c> or <c>&lt;id&gt;.grid.txt</c>, and
    /// writes <c>&lt;id&gt;.json</c> with the grid (rows bottom first) and the structure metrics. For a PNG, palette index
    /// i below the role count is role i, <c>roles.length</c> is EMPTY and <c>roles.length + 1</c> is STONE. A draft that
    /// passes the automated picture checks is approved by them (<see cref="PictureChecks"/>, FR-084 as amended).
    /// </summary>
    public static class PictureImporter
    {
        public const string MetaSuffix = ".meta.json";

        public static IReadOnlyList<ImportedPicture> ImportFolder(string sourceFolder, string libraryFolder)
        {
            Directory.CreateDirectory(libraryFolder);
            string[] metas = Directory.GetFiles(sourceFolder, "*" + MetaSuffix);
            Array.Sort(metas, StringComparer.Ordinal);
            var results = new List<ImportedPicture>();
            foreach (string meta in metas)
            {
                string id = Path.GetFileName(meta).Substring(0, Path.GetFileName(meta).Length - MetaSuffix.Length);
                try
                {
                    BasePicture picture = Import(meta);
                    string output = Path.Combine(libraryFolder, id + ".json");
                    File.WriteAllText(output, BasePictureJson.Write(picture));
                    results.Add(new ImportedPicture(id, output, null));
                }
                catch (Exception ex) when (ex is ContentFormatException || ex is InvalidDataException || ex is IOException)
                {
                    results.Add(new ImportedPicture(id, null, ex.Message));
                }
            }

            return results;
        }

        public static BasePicture Import(string metaPath)
        {
            string folder = Path.GetDirectoryName(metaPath)!;
            string id = Path.GetFileName(metaPath).Substring(0, Path.GetFileName(metaPath).Length - MetaSuffix.Length);
            JObject meta = JsonDoc.ParseObject(File.ReadAllText(metaPath), metaPath);
            if (meta["grid"] != null || meta["structure"] != null)
            {
                throw new ContentFormatException(metaPath, "the sidecar must not contain grid or structure (they are computed)");
            }

            JObject? legendObject = meta["legend"] as JObject;
            meta.Remove("legend");
            JArray roles = JsonDoc.Array(JsonDoc.Required(meta, string.Empty, "roles"), "roles", minItems: 2);
            var roleIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < roles.Count; i++)
            {
                roleIndex[JsonDoc.String(JsonDoc.Required(JsonDoc.Object(roles[i], "roles"), "roles", "roleId"), "roleId")] = i;
            }

            int[][] topFirst;
            string png = Path.Combine(folder, id + ".png");
            string text = Path.Combine(folder, id + ".grid.txt");
            if (File.Exists(png))
            {
                topFirst = FromPng(IndexedPngReader.Read(png), roles.Count, png);
            }
            else if (File.Exists(text))
            {
                if (legendObject == null)
                {
                    throw new ContentFormatException(metaPath, "a .grid.txt picture needs a legend");
                }

                var legend = new Dictionary<char, string>();
                foreach (JProperty entry in legendObject.Properties())
                {
                    if (entry.Name.Length != 1)
                    {
                        throw new ContentFormatException("legend." + entry.Name, "legend keys are single characters");
                    }

                    legend[entry.Name[0]] = JsonDoc.String(entry.Value, "legend." + entry.Name);
                }

                string?[][] cells = TextGridReader.Read(File.ReadAllText(text), legend, text);
                topFirst = new int[cells.Length][];
                for (int y = 0; y < cells.Length; y++)
                {
                    topFirst[y] = new int[cells[y].Length];
                    for (int x = 0; x < cells[y].Length; x++)
                    {
                        string? cell = cells[y][x];
                        topFirst[y][x] = cell == null ? BasePicture.Empty
                            : cell == "#" ? BasePicture.Stone
                            : roleIndex.TryGetValue(cell, out int r) ? r
                            : throw new ContentFormatException(metaPath, $"legend role '{cell}' is not in roles");
                    }
                }
            }
            else
            {
                throw new IOException($"{metaPath}: no {id}.png or {id}.grid.txt next to it.");
            }

            int height = topFirst.Length;
            int width = topFirst[0].Length;
            CheckDimension(meta, "width", width, metaPath);
            CheckDimension(meta, "height", height, metaPath);
            var grid = new JArray();
            for (int y = height - 1; y >= 0; y--)
            {
                grid.Add(new JArray(Array.ConvertAll(topFirst[y], v => (object)v)));
            }

            meta["grid"] = grid;
            BasePicture picture = BasePictureJson.Read(CanonicalJson.Write(meta, indented: false));
            return PictureChecks.ApproveIfPassing(picture with { Structure = StructureMetrics.Compute(picture) });
        }

        private static int[][] FromPng(IndexedImage image, int roleCount, string name)
        {
            var rows = new int[image.Height][];
            for (int y = 0; y < image.Height; y++)
            {
                rows[y] = new int[image.Width];
                for (int x = 0; x < image.Width; x++)
                {
                    int index = image.Indexes[y][x];
                    rows[y][x] = index < roleCount ? index
                        : index == roleCount ? BasePicture.Empty
                        : index == roleCount + 1 ? BasePicture.Stone
                        : throw new InvalidDataException($"{name}: palette index {index} at ({x},{y}) is not a role, EMPTY ({roleCount}) or STONE ({roleCount + 1}).");
                }
            }

            return rows;
        }

        private static void CheckDimension(JObject meta, string name, int actual, string path)
        {
            JToken? token = meta[name];
            if (token == null)
            {
                meta[name] = actual;
            }
            else if (token.Type != JTokenType.Integer || token.Value<int>() != actual)
            {
                throw new ContentFormatException(path, $"{name} is {token}, the grid has {actual}");
            }
        }
    }
}
