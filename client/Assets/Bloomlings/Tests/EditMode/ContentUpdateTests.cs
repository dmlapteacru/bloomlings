using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.Editor;
using Bloomlings.Client.Services.Content;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Client.Tests.EditMode
{
    /// <summary>
    /// T091–T092: content update decisions, the atomic cache, pack verification, the start-content choice and the
    /// import of published content. Content versions are built with the core's own pack and manifest writers.
    /// </summary>
    public class ContentUpdateTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "bloomlings-content-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private static ContentSet Curated => DevContent.LoadCurated();

        private static (ContentManifest Manifest, Dictionary<string, byte[]> Packs) Version(int contentVersion, string minApp = "0.1.0")
        {
            ContentSet curated = Curated;
            var levels = new List<LevelDefinition>();
            foreach (int number in curated.LevelNumbers)
            {
                levels.Add(curated.GetLevel(number));
            }

            var pictures = new List<BasePicture>();
            foreach (LevelDefinition level in levels)
            {
                BasePicture picture = curated.GetPicture(level.Picture);
                if (!pictures.Exists(p => p.Id == picture.Id))
                {
                    pictures.Add(picture);
                }
            }

            var request = new PublishRequest(contentVersion, minApp, 1, 20000, levels, pictures, Array.Empty<DailyPoolEntry>());
            ContentManifest manifest = ManifestWriter.Build(request, out IReadOnlyList<PackFile> files);
            var packs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (PackFile file in files)
            {
                packs[file.Entry.Id] = file.Bytes;
            }

            return (manifest, packs);
        }

        [Test]
        public void Decide_DownloadsOnlyNewerVersionsThisAppCanRead()
        {
            ContentManifest v5 = Version(5, "1.2.0").Manifest;

            Assert.That(ContentUpdater.Decide(v5, 4, "1.2.0"), Is.EqualTo(UpdateDecision.Download));
            Assert.That(ContentUpdater.Decide(v5, 5, "1.2.0"), Is.EqualTo(UpdateDecision.UpToDate));
            Assert.That(ContentUpdater.Decide(v5, 6, "9.0.0"), Is.EqualTo(UpdateDecision.UpToDate));
            Assert.That(ContentUpdater.Decide(v5, 4, "1.1.9"), Is.EqualTo(UpdateDecision.AppTooOld));
            Assert.That(ContentUpdater.Decide(v5, 4, "1.10.0-beta+7"), Is.EqualTo(UpdateDecision.Download));
        }

        [Test]
        public void PackUri_ResolvesPathsAgainstTheManifestAndRequiresHttps()
        {
            var manifest = new Uri("https://cdn.example.com/content/v5/manifest.json");
            var relative = new PackEntry("levels_0001_0250", PackKind.Levels, 1, 250, "levels/levels_0001_0250.jsonl.gz", null, new string('a', 64), 10);
            var insecure = relative with { Path = null, Url = "http://cdn.example.com/p.jsonl.gz" };

            Assert.That(ContentUpdater.PackUri(manifest, relative).AbsoluteUri, Is.EqualTo("https://cdn.example.com/content/v5/levels/levels_0001_0250.jsonl.gz"));
            Assert.Throws<InvalidOperationException>(() => ContentUpdater.PackUri(manifest, insecure));
        }

        [Test]
        public void Install_ActivatesAtomicallyAndLoadsBack()
        {
            var cache = new ContentCache(_root);
            (ContentManifest v2, Dictionary<string, byte[]> packs2) = Version(2);
            (ContentManifest v3, Dictionary<string, byte[]> packs3) = Version(3);

            cache.Install(v2, packs2);
            cache.Install(v3, packs3);

            Assert.That(cache.ActiveVersion, Is.EqualTo(3));
            Assert.That(cache.LoadActive().ContentVersion, Is.EqualTo(3));
            Assert.That(cache.LoadActive().LevelCount, Is.EqualTo(Curated.LevelCount));
            Assert.That(Directory.Exists(Path.Combine(_root, ContentCache.VersionFolder(2))), Is.False, "older versions are removed");
            Assert.That(Directory.Exists(Path.Combine(_root, ContentCache.StagingFolder)), Is.False);
        }

        [Test]
        public void Install_RejectsADamagedPackAndKeepsTheActiveVersion()
        {
            var cache = new ContentCache(_root);
            (ContentManifest v2, Dictionary<string, byte[]> packs2) = Version(2);
            cache.Install(v2, packs2);
            (ContentManifest v3, Dictionary<string, byte[]> packs3) = Version(3);
            string id = v3.Packs[0].Id;
            byte[] damaged = (byte[])packs3[id].Clone();
            damaged[damaged.Length / 2] ^= 0xFF;
            packs3[id] = damaged;

            Assert.Throws<ContentIntegrityException>(() => cache.Install(v3, packs3));
            Assert.Throws<ContentIntegrityException>(() => ContentUpdater.Parse(v3, packs3));
            Assert.That(cache.ActiveVersion, Is.EqualTo(2));
            Assert.That(cache.LoadActive().ContentVersion, Is.EqualTo(2));
        }

        [Test]
        public void ChooseStartContent_PrefersANewerReadableCacheAndFallsBackOnDamage()
        {
            var cache = new ContentCache(_root);
            ContentSet bundled = ContentUpdater.Parse(Version(1).Manifest, Version(1).Packs);
            var errors = new List<Exception>();

            Assert.That(ContentUpdater.ChooseStartContent(bundled, cache, "0.1.0", errors.Add), Is.SameAs(bundled), "empty cache");

            (ContentManifest v4, Dictionary<string, byte[]> packs4) = Version(4);
            cache.Install(v4, packs4);
            Assert.That(ContentUpdater.ChooseStartContent(bundled, cache, "0.1.0", errors.Add).ContentVersion, Is.EqualTo(4));

            string packFile = Path.Combine(_root, ContentCache.VersionFolder(4), ContentCache.LocalPath(v4.Packs[0]));
            File.WriteAllBytes(packFile, new byte[] { 1, 2, 3 });
            Assert.That(ContentUpdater.ChooseStartContent(bundled, cache, "0.1.0", errors.Add), Is.SameAs(bundled), "damaged cache");
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void ChooseStartContent_IgnoresACacheThatNeedsANewerApp()
        {
            var cache = new ContentCache(_root);
            ContentSet bundled = ContentUpdater.Parse(Version(1).Manifest, Version(1).Packs);
            (ContentManifest v4, Dictionary<string, byte[]> packs4) = Version(4, "2.0.0");
            cache.Install(v4, packs4);

            Assert.That(ContentUpdater.ChooseStartContent(bundled, cache, "1.9.9", e => Assert.Fail(e.Message)), Is.SameAs(bundled));
        }

        [Test]
        public void CatalogService_KeepsAnAttemptOnTheContentItStartedWith()
        {
            ContentSet v1 = ContentUpdater.Parse(Version(1).Manifest, Version(1).Packs);
            ContentSet v2 = ContentUpdater.Parse(Version(2).Manifest, Version(2).Packs);
            var catalog = new CatalogService(v1);

            LevelAttempt attempt = catalog.BeginAttempt(3)!;
            catalog.Activate(v2);

            Assert.That(attempt.Options.ContentVersion, Is.EqualTo(1));
            Assert.That(catalog.BeginAttempt(3)!.Options.ContentVersion, Is.EqualTo(2));
        }

        [Test]
        public void PastTheEndOfTheCatalog_OnlyDevelopmentBuildsRepeatIt()
        {
            ContentSet content = ContentUpdater.Parse(Version(1).Manifest, Version(1).Packs);
            int past = content.LevelNumbers[content.LevelCount - 1] + 1;

            var release = new CatalogService(content);
            Assert.That(release.HasLevel(past), Is.False);
            Assert.That(release.BeginAttempt(past), Is.Null, "a release build never replays old levels as new ones");

            var development = new CatalogService(content, repeatPastEnd: true);
            Assert.That(development.BeginAttempt(past)!.Definition, Is.SameAs(content.GetLevel(content.LevelNumbers[0])));
        }

        [Test]
        public void ImportFrom_CopiesVerifiedPublishedContentAndKeepsTheDevFolder()
        {
            string published = Path.Combine(_root, "build", "content");
            string streaming = Path.Combine(_root, "StreamingAssets", "content");
            (ContentManifest v7, Dictionary<string, byte[]> packs) = Version(7);
            foreach (PackEntry entry in v7.Packs)
            {
                string path = Path.Combine(published, entry.Path!);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, packs[entry.Id]);
            }

            File.WriteAllText(Path.Combine(published, "manifest.json"), v7.Write());
            Directory.CreateDirectory(Path.Combine(streaming, "dev"));
            Directory.CreateDirectory(Path.Combine(streaming, "levels"));
            File.WriteAllText(Path.Combine(streaming, "levels", "stale.jsonl.gz"), "old");

            ContentManifest imported = ImportContentMenu.ImportFrom(published, streaming);

            Assert.That(imported.ContentVersion, Is.EqualTo(7));
            Assert.That(File.Exists(Path.Combine(streaming, "manifest.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(streaming, "levels", "stale.jsonl.gz")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(streaming, "dev")), Is.True);
            foreach (PackEntry entry in v7.Packs)
            {
                Assert.That(File.ReadAllBytes(Path.Combine(streaming, entry.Path!)), Is.EqualTo(packs[entry.Id]));
            }
        }
    }
}
