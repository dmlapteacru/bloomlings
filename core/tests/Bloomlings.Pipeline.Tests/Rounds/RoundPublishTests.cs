using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Pipeline.Commands;
using Bloomlings.Pipeline.Rounds;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests.Rounds
{
    /// <summary><c>rounds publish</c> (T052): manifest and packs, the content version, and the refusal on errors.</summary>
    [TestFixture]
    public class RoundPublishTests
    {
        [Test]
        public void Publish_WritesAManifestAndPacksThatReadBack()
        {
            using var content = new RoundTestContent();
            string outDir = Path.Combine(content.Root, "published");

            RoundPublishResult result = RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null);
            string manifestJson = File.ReadAllText(Path.Combine(outDir, "manifest.json"));
            RoundSource published = RoundSource.LoadPublished(outDir);

            Assert.Multiple(() =>
            {
                Assert.That(RoundContent.ManifestSchemaErrors(manifestJson), Is.Empty);
                Assert.That(result.Manifest.ContentVersion, Is.EqualTo(1));
                Assert.That(result.Manifest.Packs.Select(p => p.Path), Is.EqualTo(new[] { "levels/round_levels_0001_0002.jsonl.gz", "pictures/round_pictures.jsonl.gz" }));
                Assert.That(result.Manifest.Levels.Select(l => l.Number), Is.EqualTo(new[] { 1, 2 }));
                Assert.That(result.Manifest.Pictures, Is.EqualTo(new[] { ("P9001", 1) }));
                Assert.That(published.Levels.Keys, Is.EqualTo(new[] { 1, 2 }));
                Assert.That(RoundJson.WriteLevel(published.Levels[2]), Is.EqualTo(RoundJson.WriteLevel(RoundContent.Load(content.Folder).Levels[2].Value!)));
                Assert.That(published.Pictures.Keys, Is.EqualTo(new[] { "P9001@1" }));
            });
        }

        [Test]
        public void ContentVersion_IsKeptForTheSamePacksAndRaisedForChangedOnes()
        {
            using var content = new RoundTestContent();
            string outDir = Path.Combine(content.Root, "published");

            int first = RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null).Manifest.ContentVersion;
            int again = RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null).Manifest.ContentVersion;
            content.Edit("levels/L0002.json", j => j["definitionVersion"] = 2);
            int changed = RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null).Manifest.ContentVersion;

            Assert.That((first, again, changed), Is.EqualTo((1, 1, 2)));
        }

        [Test]
        public void AnExplicitVersion_AlreadyPublishedWithOtherContent_IsRefused()
        {
            using var content = new RoundTestContent();
            string outDir = Path.Combine(content.Root, "published");
            RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, 4);
            content.Edit("levels/L0002.json", j => j["definitionVersion"] = 2);

            Assert.Throws<System.ArgumentException>(() => RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, 4));
            Assert.That(RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, 5).Manifest.ContentVersion, Is.EqualTo(5));
        }

        [Test]
        public void StalePacks_AreRemoved()
        {
            using var content = new RoundTestContent(2);
            string outDir = Path.Combine(content.Root, "published");
            RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null);
            RoundTestContent.Write(content.Folder, 1, firstLevel: 3);

            RoundPublishResult result = RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null);

            Assert.That(result.Removed, Is.EqualTo(new[] { "levels/round_levels_0001_0002.jsonl.gz" }));
            Assert.That(Directory.GetFiles(Path.Combine(outDir, "levels")).Select(Path.GetFileName), Is.EqualTo(new[] { "round_levels_0001_0003.jsonl.gz" }));
        }

        [Test]
        public void Cli_Publish_RefusesOnAnyValidationError()
        {
            using var content = new RoundTestContent();
            string outDir = Path.Combine(content.Root, "published");

            // The default seams are not wired yet, so every level fails conformance.
            int code = Program.Main(new[] { "rounds", "publish", "--rounds", content.Folder, "--out", outDir });

            Assert.That(code, Is.EqualTo(ExitCodes.ValidationFailed));
            Assert.That(Directory.Exists(outDir), Is.False);
        }

        [Test]
        public void Cli_Publish_WritesTheVersionWhenValid()
        {
            using var content = new RoundTestContent();
            string outDir = Path.Combine(content.Root, "published");
            IRoundConformance conformance = RoundSeams.Conformance;
            IRoundDefinitionRules rules = RoundSeams.DefinitionRules;
            RoundSeams.Conformance = new RoundTestContent.Conforms();
            RoundSeams.DefinitionRules = new RoundTestContent.NoDefinitionErrors();
            try
            {
                int code = Program.Main(new[] { "rounds", "publish", "--rounds", content.Folder, "--out", outDir, "--content-version", "3" });

                Assert.That(code, Is.EqualTo(ExitCodes.Success));
                Assert.That(RoundContentManifest.Read(File.ReadAllText(Path.Combine(outDir, "manifest.json"))).ContentVersion, Is.EqualTo(3));
            }
            finally
            {
                RoundSeams.Conformance = conformance;
                RoundSeams.DefinitionRules = rules;
            }
        }
    }
}
