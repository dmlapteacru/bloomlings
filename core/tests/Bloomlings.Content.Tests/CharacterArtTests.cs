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
    /// is complete (57 files), every file is listed with its size and SHA-256, and nothing else is in the folder. The
    /// owner's 3D pictures (spec 005 pictures.md A) may take a generated picture's place or add one in <c>3d/</c>; they are
    /// marked <c>"source": "owner"</c> and name their source record. It reads no pixels;
    /// <c>dotnet run --project tools/artgen -- check</c> compares the generated pictures with a fresh render.
    /// </summary>
    public class CharacterArtTests
    {
        private const string Folder = "client/Assets/Bloomlings/Art/Characters/Resources/Characters/";

        /// <summary>The families of the 3D heroes, as the generated <c>3d/</c> file names spell them.</summary>
        private static readonly string[] Families = { "sprig", "bloom", "drop", "twig" };

        /// <summary>The generated 3D set: each hero with and without its face, and the group.</summary>
        private static IEnumerable<string> Set3D => Families.SelectMany(f => new[] { "3d/" + f + ".png", "3d/" + f + "-blank.png" }).Append("3d/group.png");

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

        /// <summary>
        /// The paths a folder's <c>manifest.json</c> covers for the originality record (relative, with <c>/</c>), or none
        /// without a manifest: every generated file, and an owner picture only when its source record exists under
        /// <paramref name="root"/> (spec 005 pictures.md A).
        /// </summary>
        public static ISet<string> RecordedFiles(string folder, string root) =>
            new HashSet<string>(Entries(folder).Where(e => !e.Owner || (e.Record.Length > 0 && File.Exists(Path.Combine(root, e.Record)))).Select(e => e.Path), StringComparer.Ordinal);

        private static IEnumerable<(string Path, int Width, int Height, string Sha256, string Slot, bool Owner, string Record)> Entries(string folder)
        {
            string manifest = Path.Combine(folder, "manifest.json");
            if (!File.Exists(manifest))
            {
                yield break;
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest));
            foreach (JsonElement file in document.RootElement.GetProperty("files").EnumerateArray())
            {
                bool owner = file.TryGetProperty("source", out JsonElement source) && source.GetString() == "owner";
                string record = file.TryGetProperty("record", out JsonElement r) ? r.GetString() ?? string.Empty : string.Empty;
                yield return (file.GetProperty("path").GetString()!, file.GetProperty("width").GetInt32(), file.GetProperty("height").GetInt32(), file.GetProperty("sha256").GetString()!, file.GetProperty("slot").GetString()!, owner, record);
            }
        }

        [Test]
        public void Manifest_ListsTheWholeSet()
        {
            var entries = Entries(ArtFolder).ToList();
            var paths = new HashSet<string>(entries.Select(e => e.Path), StringComparer.Ordinal);
            Assert.That(paths.Count, Is.EqualTo(entries.Count), "each file listed once");

            // The 48 2D characters are always generated; the 9 generated 3D pictures are there, each the tool's or the owner's.
            Assert.That(entries.Count(e => e.Path.StartsWith("2d/", StringComparison.Ordinal) && !e.Owner), Is.EqualTo(48));
            Assert.That(entries.Where(e => e.Owner).Select(e => e.Path).Where(p => !p.StartsWith("3d/", StringComparison.Ordinal)), Is.Empty, "owner pictures are 3D heroes in 3d/");
            Assert.That(Set3D.Where(p => !paths.Contains(p)), Is.Empty);
            Assert.That(entries.Where(e => e.Path.StartsWith("3d/", StringComparison.Ordinal) && !e.Owner).Select(e => e.Path).Where(p => !Set3D.Contains(p)), Is.Empty, "the tool generates only the set");
            Assert.That(entries.Count(e => !e.Owner || Set3D.Contains(e.Path)), Is.EqualTo(57));
            Assert.That(entries.All(e => e.Slot.StartsWith("char.v.", StringComparison.Ordinal) || e.Slot.StartsWith("char.hero3d.", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void OwnerPictures_NameAnExistingSourceRecord()
        {
            var missing = Entries(ArtFolder).Where(e => e.Owner && (e.Record.Length == 0 || !File.Exists(Path.Combine(RepositoryRoot, e.Record))))
                .Select(e => e.Path + " (record: " + (e.Record.Length == 0 ? "none" : e.Record) + ")");
            Assert.That(missing, Is.Empty, "run tools/artgen adopt with --record (tools/artgen/README.md)");
        }

        [Test]
        public void Manifest_MatchesTheFiles()
        {
            var problems = new List<string>();
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string path, int width, int height, string sha256, string _, bool _, string _) in Entries(ArtFolder))
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
