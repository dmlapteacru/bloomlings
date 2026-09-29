using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using NUnit.Framework;

namespace Bloomlings.Content.Tests
{
    /// <summary>
    /// Authoring data in <c>content/</c> is validated like code: every picture and curated level is canonical, and
    /// every level loads with its picture (board build and exact accounting).
    /// </summary>
    public class ContentFolderTests
    {
        private static string ContentRoot
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
                {
                    dir = dir.Parent;
                }

                return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."), "content");
            }
        }

        private static string[] PictureFiles() => Files(Path.Combine("pictures", "lib"));

        private static string[] LevelFiles() => Files("curated");

        [TestCaseSource(nameof(PictureFiles))]
        public void Picture_IsCanonical(string relativePath)
        {
            string text = File.ReadAllText(Path.Combine(ContentRoot, relativePath));

            Assert.That(BasePictureJson.Write(BasePictureJson.Read(text)), Is.EqualTo(text));
        }

        [TestCaseSource(nameof(LevelFiles))]
        public void Level_IsCanonicalAndLoads(string relativePath)
        {
            string text = File.ReadAllText(Path.Combine(ContentRoot, relativePath));
            LevelDefinition level = DefinitionJson.Read(text);
            string picturePath = Path.Combine(ContentRoot, "pictures", "lib", level.Picture.Id + ".json");
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(picturePath));

            Assert.That(DefinitionJson.Write(level), Is.EqualTo(text));
            Assert.That(picture.Version, Is.EqualTo(level.Picture.Version));
            Assert.DoesNotThrow(() => LevelSession.Load(level, picture, new SessionOptions(0, 20000)));
        }

        private static string[] Files(string folder)
        {
            string path = Path.Combine(ContentRoot, folder);
            if (!Directory.Exists(path))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(ContentRoot, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
