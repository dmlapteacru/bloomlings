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
    /// the picture library, no imported art, audio or font files in the client, and every picture owned or licensed.
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

            // Every visual is procedural placeholder art (ProceduralSprites); imported assets need a licence record first.
            Assert.That(media, Is.Empty);
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
