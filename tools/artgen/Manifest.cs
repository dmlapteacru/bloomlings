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

        /// <summary>Lists every PNG under the folder, sorted by path, and writes the manifest.</summary>
        public static Manifest Write(string folder, Func<string, string> slotOf)
        {
            var manifest = new Manifest();
            foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                string relative = System.IO.Path.GetRelativePath(folder, file).Replace('\\', '/');
                (int w, int h) = Png.Size(file);
                manifest.Files.Add(new ManifestFile
                {
                    Path = relative,
                    Width = w,
                    Height = h,
                    Sha256 = Hash(file),
                    Slot = slotOf(relative.Substring(0, relative.Length - 4)),
                });
            }

            File.WriteAllText(System.IO.Path.Combine(folder, FileName), JsonSerializer.Serialize(manifest, Options) + "\n");
            return manifest;
        }

        public static string Hash(string file)
        {
            using var sha = SHA256.Create();
            using FileStream stream = File.OpenRead(file);
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }
    }
}
