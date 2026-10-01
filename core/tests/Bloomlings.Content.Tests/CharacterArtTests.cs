using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace Bloomlings.Content.Tests
{
    /// <summary>
    /// The committed character art of spec 004 matches its manifest (data-model.md "ArtManifest", research R12): the set
    /// is complete (57 files), every file is listed with its size and SHA-256, and nothing else is in the folder. It reads
    /// no pixels; <c>dotnet run --project tools/artgen -- check</c> compares the pictures with a fresh render.
    /// </summary>
    public class CharacterArtTests
    {
        private const string Folder = "client/Assets/Bloomlings/Art/Characters/Resources/Characters/";

        private static string RepositoryRoot
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
                {
                    dir = dir.Parent;
                }

                return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
            }
        }

        private static string ArtFolder => Path.Combine(RepositoryRoot, Folder);

        /// <summary>The paths a folder's <c>manifest.json</c> lists (relative, with <c>/</c>), or none without a manifest.</summary>
        public static ISet<string> ListedFiles(string folder) =>
            new HashSet<string>(Entries(folder).Select(e => e.Path), StringComparer.Ordinal);

        private static IEnumerable<(string Path, int Width, int Height, string Sha256, string Slot)> Entries(string folder)
        {
            string manifest = Path.Combine(folder, "manifest.json");
            if (!File.Exists(manifest))
            {
                yield break;
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest));
            foreach (JsonElement file in document.RootElement.GetProperty("files").EnumerateArray())
            {
                yield return (file.GetProperty("path").GetString()!, file.GetProperty("width").GetInt32(), file.GetProperty("height").GetInt32(), file.GetProperty("sha256").GetString()!, file.GetProperty("slot").GetString()!);
            }
        }

        [Test]
        public void Manifest_ListsTheWholeSet()
        {
            var entries = Entries(ArtFolder).ToList();
            Assert.That(entries.Count, Is.EqualTo(57));
            Assert.That(entries.Count(e => e.Path.StartsWith("2d/", StringComparison.Ordinal)), Is.EqualTo(48));
            Assert.That(entries.Count(e => e.Path.StartsWith("3d/", StringComparison.Ordinal)), Is.EqualTo(9));
            Assert.That(entries.Select(e => e.Path).Distinct().Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.All(e => e.Slot.StartsWith("char.v.", StringComparison.Ordinal) || e.Slot.StartsWith("char.hero3d.", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void Manifest_MatchesTheFiles()
        {
            var problems = new List<string>();
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string path, int width, int height, string sha256, string _) in Entries(ArtFolder))
            {
                listed.Add(path);
                string file = Path.Combine(ArtFolder, path);
                if (!File.Exists(file))
                {
                    problems.Add(path + " is listed but missing");
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(file);
                using (var sha = SHA256.Create())
                {
                    if (Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant() != sha256)
                    {
                        problems.Add(path + ": the SHA-256 differs from the manifest (run tools/artgen build)");
                    }
                }

                // The PNG header's IHDR chunk holds the size, big-endian, at bytes 16 to 23.
                int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                if (w != width || h != height)
                {
                    problems.Add($"{path}: {w} × {h}, the manifest says {width} × {height}");
                }
            }

            foreach (string file in Directory.GetFiles(ArtFolder, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(ArtFolder, file).Replace('\\', '/');
                if (relative != "manifest.json" && !relative.EndsWith(".meta", StringComparison.Ordinal) && !listed.Contains(relative))
                {
                    problems.Add(relative + " is in the folder but not in the manifest");
                }
            }

            Assert.That(problems, Is.Empty);
        }
    }
}
