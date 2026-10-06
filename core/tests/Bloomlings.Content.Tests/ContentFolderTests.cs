using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
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

        /// <summary>
        /// The onboarding band (Levels 1–10): file names match level numbers, each level has its own picture (FR-083),
        /// L1 has 2 variants and L2 adds a third (FR-060), boards are 11–12×12 (the bigger boards of 2026-10-05) with 2–3
        /// variants, 3–8 pods and 95–140 work; L5 is the first Hard and L10 the first Super Hard level (roadmap).
        /// </summary>
        [Test]
        public void CuratedOnboardingLevels_FollowTheBandGuidelines()
        {
            var pictures = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (int n = 1; n <= 10; n++)
            {
                string file = Path.Combine(ContentRoot, "curated", $"level-{n:0000}.json");
                LevelDefinition level = DefinitionJson.Read(File.ReadAllText(file));
                BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(ContentRoot, "pictures", "lib", level.Picture.Id + ".json")));
                int variants = level.Pods.Select(p => p.Variant).Distinct().Count();
                int work = level.Pods.Sum(p => p.Count);

                Assert.Multiple(() =>
                {
                    Assert.That(level.LevelNumber, Is.EqualTo(n), file);
                    Assert.That(pictures.Add(level.Picture.Id), Is.True, $"L{n} reuses picture {level.Picture.Id}");
                    Assert.That(picture.Width, Is.InRange(11, 12), $"L{n} width");
                    Assert.That(picture.Height, Is.EqualTo(12), $"L{n} height");
                    Assert.That(variants, Is.EqualTo(n == 1 ? 2 : n == 2 ? 3 : variants).And.InRange(2, 3), $"L{n} variants");
                    Assert.That(level.Pods.Count, Is.InRange(3, 8), $"L{n} pods");
                    Assert.That(work, Is.InRange(95, 140), $"L{n} work");
                    DifficultyClass expected = n == 5 ? DifficultyClass.Hard : n == 10 ? DifficultyClass.SuperHard : DifficultyClass.Normal;
                    Assert.That(level.Difficulty.Class, Is.EqualTo(expected), $"L{n} class");
                });
            }
        }

        [Test]
        public void UnlockRoadmapFile_EqualsTheRuntimeRoadmap()
        {
            string text = File.ReadAllText(Path.Combine(ContentRoot, "roadmap", "unlock-roadmap.json"));

            Assert.That(RoadmapJson.Write(RoadmapJson.Read(text)), Is.EqualTo(text), "canonical");
            Assert.That(text, Is.EqualTo(RoadmapJson.Write(UnlockRoadmap.Default)), "content/roadmap mirrors UnlockRoadmap.Default");
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
