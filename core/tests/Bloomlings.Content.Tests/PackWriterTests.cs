using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using NUnit.Framework;

namespace Bloomlings.Content.Tests
{
    /// <summary>T070: pack writing, byte stability and manifest hashes.</summary>
    public class PackWriterTests
    {
        private static LevelDefinition Level(int n) =>
            DefinitionJson.Read(DefinitionJsonTests.MinimalLevel().ToString()) with { LevelNumber = n, Seed = (ulong)n };

        private static BasePicture Picture => BasePictureJson.Read(ContractSchemas.Sample("picture-sample.json"));

        private static PublishRequest Request(int levels) => new PublishRequest(
            3,
            "1.0.0",
            1,
            20000,
            Enumerable.Range(1, levels).Select(Level).ToArray(),
            new[] { Picture },
            new[] { new DailyPoolEntry(0, Level(1)), new DailyPoolEntry(1, Level(2)) });

        [Test]
        public void SixHundredLevels_FillThreePacks()
        {
            IReadOnlyList<PackFile> packs = LevelPackWriter.LevelPacks(Enumerable.Range(1, 600).Select(Level));

            Assert.That(packs.Select(p => p.Entry.Id), Is.EqualTo(new[] { "levels_0001_0250", "levels_0251_0500", "levels_0501_0600" }));
            Assert.That(packs.Select(p => LevelPackReader.Read(p.Entry, p.Bytes).Count), Is.EqualTo(new[] { 250, 250, 100 }));
        }

        [Test]
        public void PackBytes_AreStableAcrossRuns()
        {
            ContentManifest a = ManifestWriter.Build(Request(600), out IReadOnlyList<PackFile> filesA);
            ContentManifest b = ManifestWriter.Build(Request(600), out IReadOnlyList<PackFile> filesB);

            Assert.That(filesB.Select(f => f.Bytes), Is.EqualTo(filesA.Select(f => f.Bytes)));
            Assert.That(b.Write(), Is.EqualTo(a.Write()));
            Assert.That(filesA.All(f => f.Bytes[4] == 0 && f.Bytes[5] == 0 && f.Bytes[6] == 0 && f.Bytes[7] == 0), Is.True, "gzip mtime is 0");
        }

        [Test]
        public void PublishedFolder_LoadsWithVerifiedHashes()
        {
            string folder = Path.Combine(Path.GetTempPath(), "bloomlings-publish-" + Guid.NewGuid().ToString("N"));
            try
            {
                ContentManifest written = ManifestWriter.Publish(Request(600), folder);
                ContentManifest manifest = ContentManifest.Read(File.ReadAllText(Path.Combine(folder, ManifestWriter.ManifestFileName)));

                Assert.That(ContractSchemas.IsValidManifest(manifest.Write()), Is.True);
                Assert.That(manifest.MaxLevel, Is.EqualTo(600));
                Assert.That(manifest.ShuffleNodeBudget, Is.EqualTo(20000));
                foreach (PackEntry entry in manifest.Packs)
                {
                    byte[] bytes = File.ReadAllBytes(Path.Combine(folder, entry.Path!));
                    Assert.DoesNotThrow(() => PackIntegrity.Verify(entry, bytes), entry.Id);
                }

                ContentSet set = ContentLoader.FromPacks(manifest, e => File.ReadAllBytes(Path.Combine(folder, e.Path!)));
                Assert.That(set.LevelCount, Is.EqualTo(600));
                Assert.That(written.Packs.Count, Is.EqualTo(5), "3 level packs, the picture pack and the daily pack.");

                PackEntry daily = manifest.Packs.Single(p => p.Kind == PackKind.Daily);
                Assert.That(LevelPackWriter.ReadDaily(daily, File.ReadAllBytes(Path.Combine(folder, daily.Path!))).Select(d => d.Index), Is.EqualTo(new[] { 0, 1 }));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }
        }

        [Test]
        public void GapsInTheLevelSequence_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => LevelPackWriter.LevelPacks(new[] { Level(1), Level(3) }));
        }

        [Test]
        public void ValidationRecord_RoundTrips()
        {
            LevelDefinition level = Level(7);
            var record = new ValidationRecord(
                7,
                1,
                ValidationRecord.HashOf(level),
                "solver-1.0.0",
                200000,
                42,
                ValidationResult.Solvable,
                new Command[] { new TapPod("p1"), new TapPod("p2") },
                null,
                null,
                new SortedDictionary<string, int>(StringComparer.Ordinal) { ["peakBuffer"] = 2, ["totalWork"] = 5 },
                new[] { "solvable", "accounting" });

            string text = record.Write();

            Assert.That(ValidationRecord.Read(text).Write(), Is.EqualTo(text));
            Assert.That(record.DefinitionHash, Has.Length.EqualTo(64));
            Assert.That(ValidationRecord.HashOf(level with { Seed = 8 }), Is.Not.EqualTo(record.DefinitionHash));
        }
    }
}
