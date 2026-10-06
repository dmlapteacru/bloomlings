using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Content.Tests
{
    /// <summary>
    /// The automated part of the originality review (FR-091, T154): no reference game's name in the shipped client or
    /// the picture library, no imported art, audio or font files in the client without a licence record, and every
    /// picture owned or licensed.
    /// The visual comparison of the pictures with the reference game's levels stays a human review.
    /// </summary>
    public class OriginalityTests
    {
        private static readonly string[] ForbiddenNames = { "colony flow", "colonyflow", "abi games", "abigames" };

        private static readonly string[] ImportedMedia = { ".png", ".jpg", ".jpeg", ".psd", ".wav", ".mp3", ".ogg", ".ttf", ".otf", ".fbx" };

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

        [Test]
        public void ReferenceGameName_IsNotInTheClientOrThePictures()
        {
            string[] folders = { Path.Combine("client", "Assets"), Path.Combine("content", "pictures"), Path.Combine("content", "curated") };
            string[] hits = folders
                .Select(f => Path.Combine(RepositoryRoot, f))
                .Where(Directory.Exists)
                .SelectMany(f => Directory.GetFiles(f, "*", SearchOption.AllDirectories))
                .Where(f => !ImportedMedia.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => ForbiddenNames.Any(n => File.ReadAllText(f).IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(f => Path.GetRelativePath(RepositoryRoot, f))
                .ToArray();

            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void Client_HasNoImportedArtAudioOrFonts()
        {
            string assets = Path.Combine(RepositoryRoot, "client", "Assets");
            string[] media = Directory.Exists(assets)
                ? Directory.GetFiles(assets, "*", SearchOption.AllDirectories)
                    .Where(f => ImportedMedia.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(f => Path.GetRelativePath(RepositoryRoot, f))
                    .ToArray()
                : Array.Empty<string>();

            // Every visual is procedural placeholder art (ProceduralSprites) or the project's own generated art; any other
            // file needs a licence record first: a row of client/THIRD_PARTY_NOTICES.md naming the file and its licence
            // file, which must exist. A row for a folder covers the files its manifest.json lists (spec 004 research R15).
            string notices = Path.Combine(RepositoryRoot, "client", "THIRD_PARTY_NOTICES.md");
            string[] records = File.Exists(notices)
                ? File.ReadAllLines(notices).Where(l => l.StartsWith("| `", StringComparison.Ordinal)).ToArray()
                : Array.Empty<string>();
            string[] unrecorded = media
                .Select(m => m.Replace('\\', '/'))
                .Where(m => !records.Any(r => HasLicenceFile(r) && (r.StartsWith("| `" + m + "`", StringComparison.Ordinal) || ListedByFolder(r, m))))
                .ToArray();
            Assert.That(unrecorded, Is.Empty);
        }

        /// <summary>
        /// Whether a folder record (<c>| `path/` |</c>) covers a file: its <c>manifest.json</c> lists it, and an owner
        /// picture there (spec 005 pictures.md A) also names an existing source record of its own.
        /// </summary>
        private static bool ListedByFolder(string record, string file)
        {
            string folder = record.Split('`')[1];
            if (!folder.EndsWith("/", StringComparison.Ordinal) || !file.StartsWith(folder, StringComparison.Ordinal))
            {
                return false;
            }

            return CharacterArtTests.RecordedFiles(Path.Combine(RepositoryRoot, folder), RepositoryRoot).Contains(file.Substring(folder.Length));
        }

        /// <summary>Whether a licence record's last column names an existing licence file.</summary>
        private static bool HasLicenceFile(string record)
        {
            string[] cells = record.Split('|', StringSplitOptions.RemoveEmptyEntries);
            string licence = cells[cells.Length - 1].Trim().Trim('`');
            return licence.Length > 0 && File.Exists(Path.Combine(RepositoryRoot, licence));
        }

        [Test]
        public void EveryPicture_IsOwnedOrLicensed()
        {
            string[] files = Directory.GetFiles(Path.Combine(RepositoryRoot, "content", "pictures", "lib"), "*.json");
            Assert.That(files, Is.Not.Empty);
            foreach (string file in files)
            {
                BasePicture picture = BasePictureJson.Read(File.ReadAllText(file));
                Assert.That(picture.Source.Licence, Is.EqualTo("owned").Or.EqualTo("licensed"), Path.GetFileName(file));
            }
        }
    }
}
