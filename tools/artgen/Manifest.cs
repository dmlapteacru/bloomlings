using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bloomlings.ArtGen
{
    /// <summary>One file of the art set (data-model.md "ArtManifest").</summary>
    public sealed class ManifestFile
    {
        /// <summary>The <see cref="Source"/> of a picture the owner made (spec 005 pictures.md A).</summary>
        public const string Owner = "owner";

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;

        [JsonPropertyName("slot")]
        public string Slot { get; set; } = string.Empty;

        /// <summary>
        /// Who made the file: absent for the pictures this tool generates, <see cref="Owner"/> for an owner picture that
        /// <c>adopt</c> recorded. <c>build</c> never overwrites an owner picture, and <c>check</c> verifies its size,
        /// margin and hash instead of comparing it with a fresh render.
        /// </summary>
        [JsonPropertyName("source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Source { get; set; }

        /// <summary>An owner picture's source record (repository path): the tool, the author and the licence.</summary>
        [JsonPropertyName("record")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Record { get; set; }

        [JsonIgnore]
        public bool IsOwner => Source == Owner;
    }

    /// <summary><c>manifest.json</c> of the character art folder: every file with its size, hash and asset slot.</summary>
    public sealed class Manifest
    {
        public const string FileName = "manifest.json";

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        [JsonPropertyName("tool")]
        public string Tool { get; set; } = "tools/artgen";

        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("files")]
        public List<ManifestFile> Files { get; set; } = new List<ManifestFile>();

        public static Manifest Read(string folder)
        {
            string path = System.IO.Path.Combine(folder, FileName);
            return File.Exists(path) ? JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path)) ?? new Manifest() : new Manifest();
        }

        /// <summary>The entry of a file (<c>3d/sprig.png</c>), or null when the manifest does not list it.</summary>
        public ManifestFile? Find(string path) => Files.FirstOrDefault(f => f.Path == path);

        /// <summary>
        /// Lists the set's PNG files under the folder, sorted by path, and writes the manifest:
        /// <list type="bullet">
        /// <item><description>a picture this run wrote (<paramref name="written"/>) gets a fresh entry, as does every
        /// picture of the set (<paramref name="set"/>) when there was no manifest yet;</description></item>
        /// <item><description>any other listed file keeps its entry: an owner picture as <c>adopt</c> recorded it (its hash
        /// is never refreshed here, so <c>check</c> sees a file changed since), a picture of the set as the last build
        /// wrote it;</description></item>
        /// <item><description>any other file is left out, so <c>check</c> reports it (adopt it or delete it).</description></item>
        /// </list>
        /// </summary>
        public static Manifest Write(string folder, IReadOnlyCollection<string> set, ISet<string> written, Func<string, string> slotOf)
        {
            Manifest previous = Read(folder);
            var manifest = new Manifest();
            foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                string relative = System.IO.Path.GetRelativePath(folder, file).Replace('\\', '/');
                string name = relative.Substring(0, relative.Length - 4);
                ManifestFile? entry = previous.Find(relative);
                if (written.Contains(name) || (previous.Files.Count == 0 && set.Contains(name)))
                {
                    manifest.Files.Add(Entry(folder, relative, slotOf(name)));
                }
                else if (entry != null && (entry.IsOwner || set.Contains(name)))
                {
                    manifest.Files.Add(entry);
                }
            }

            manifest.Save(folder);
            return manifest;
        }

        /// <summary>A fresh entry for a file: its size and hash, as the file is now.</summary>
        public static ManifestFile Entry(string folder, string relative, string slot)
        {
            string file = System.IO.Path.Combine(folder, relative);
            (int w, int h) = Png.Size(file);
            return new ManifestFile { Path = relative, Width = w, Height = h, Sha256 = Hash(file), Slot = slot };
        }

        /// <summary>Writes the manifest, its files sorted by path.</summary>
        public void Save(string folder)
        {
            Files = Files.OrderBy(f => f.Path, StringComparer.Ordinal).ToList();
            File.WriteAllText(System.IO.Path.Combine(folder, FileName), JsonSerializer.Serialize(this, Options) + "\n");
        }

        public static string Hash(string file)
        {
            using var sha = SHA256.Create();
            using FileStream stream = File.OpenRead(file);
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }
    }
}
