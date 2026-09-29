using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using NUnit.Framework;

namespace Bloomlings.Content.Tests
{
    public class PackReaderTests
    {
        private static LevelDefinition SampleLevel(int levelNumber) =>
            DefinitionJson.Read(DefinitionJsonTests.MinimalLevel().ToString()) with { LevelNumber = levelNumber };

        private static BasePicture SamplePicture => BasePictureJson.Read(ContractSchemas.Sample("picture-sample.json"));

        private static PackEntry EntryFor(string id, PackKind kind, byte[] bytes, int? first = null, int? last = null) =>
            new PackEntry(id, kind, first, last, $"packs/{id}.jsonl.gz", null, PackIntegrity.ComputeSha256Hex(bytes), bytes.LongLength);

        [Test]
        public void LevelPack_RoundTripsThroughGzipJsonLines()
        {
            LevelDefinition[] levels = Enumerable.Range(1, 3).Select(SampleLevel).ToArray();
            byte[] bytes = LevelPackReader.Write(levels);

            IReadOnlyList<LevelDefinition> read = LevelPackReader.Read(EntryFor("levels_0001", PackKind.Levels, bytes, 1, 3), bytes);

            Assert.That(read.Select(l => DefinitionJson.Write(l)), Is.EqualTo(levels.Select(l => DefinitionJson.Write(l))));
        }

        [Test]
        public void PackWithWrongHash_IsRejected()
        {
            byte[] bytes = LevelPackReader.Write(new[] { SampleLevel(1), SampleLevel(2) });
            PackEntry entry = EntryFor("levels_0001", PackKind.Levels, bytes, 1, 2) with { Sha256 = new string('0', 64) };

            var ex = Assert.Throws<ContentIntegrityException>(() => LevelPackReader.Read(entry, bytes));
            Assert.That(ex!.Message, Does.Contain("SHA-256 mismatch"));
        }

        [Test]
        public void TamperedPack_IsRejected()
        {
            byte[] bytes = LevelPackReader.Write(new[] { SampleLevel(1), SampleLevel(2) });
            PackEntry entry = EntryFor("levels_0001", PackKind.Levels, bytes, 1, 2);
            byte[] tampered = (byte[])bytes.Clone();
            tampered[tampered.Length / 2] ^= 0x01;

            Assert.Throws<ContentIntegrityException>(() => LevelPackReader.Read(entry, tampered));
        }

        [Test]
        public void PackWithWrongLength_IsRejected()
        {
            byte[] bytes = LevelPackReader.Write(new[] { SampleLevel(1), SampleLevel(2) });
            PackEntry entry = EntryFor("levels_0001", PackKind.Levels, bytes, 1, 2) with { Bytes = bytes.Length + 1 };

            var ex = Assert.Throws<ContentIntegrityException>(() => LevelPackReader.Read(entry, bytes));
            Assert.That(ex!.Message, Does.Contain("bytes"));
        }

        [Test]
        public void LevelPack_MustMatchItsLevelRange()
        {
            byte[] bytes = LevelPackReader.Write(new[] { SampleLevel(1), SampleLevel(3) });

            Assert.Throws<ContentIntegrityException>(() => LevelPackReader.Read(EntryFor("l", PackKind.Levels, bytes, 1, 2), bytes));
            Assert.Throws<ContentIntegrityException>(() => LevelPackReader.Read(EntryFor("l", PackKind.Levels, bytes, 1, 3), bytes));
        }

        [Test]
        public void InvalidLineInPack_NamesPackAndLine()
        {
            byte[] bytes = JsonLinesPack.Compress(new[]
            {
                DefinitionJson.Write(SampleLevel(1), indented: false),
                DefinitionJson.Write(SampleLevel(2), indented: false).Replace("\"count\":5", "\"count\":6"),
            });

            var ex = Assert.Throws<ContentFormatException>(() => LevelPackReader.ReadUnverified(bytes, "levels_0001"));
            Assert.That(ex!.Path, Is.EqualTo("levels_0001:2.slots.count"));
        }

        [Test]
        public void NotGzip_IsRejected()
        {
            byte[] bytes = { 1, 2, 3, 4, 5 };

            Assert.Throws<ContentIntegrityException>(() => LevelPackReader.ReadUnverified(bytes, "junk"));
        }

        [Test]
        public void Manifest_ParsesAndRoundTrips()
        {
            const string json = @"{
  ""contentVersion"": 3,
  ""maxLevel"": 250,
  ""minAppVersion"": ""1.0.0"",
  ""packs"": [
    {
      ""bytes"": 1234,
      ""id"": ""levels_0001"",
      ""kind"": ""levels"",
      ""levelRange"": [1, 250],
      ""path"": ""content/levels_0001.jsonl.gz"",
      ""sha256"": ""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa""
    },
    {
      ""bytes"": 99,
      ""id"": ""pictures"",
      ""kind"": ""pictures"",
      ""sha256"": ""bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"",
      ""url"": ""https://cdn.example.com/pictures.jsonl.gz""
    }
  ],
  ""pictureLibraryVersion"": 2,
  ""shuffleNodeBudget"": 50000
}
";
            string canonical = json.Replace("\r\n", "\n");

            ContentManifest manifest = ContentManifest.Read(canonical);

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidManifest(canonical), Is.True);
                Assert.That(manifest.ContentVersion, Is.EqualTo(3));
                Assert.That(manifest.ShuffleNodeBudget, Is.EqualTo(50000));
                Assert.That(manifest.Packs[0].FirstLevel, Is.EqualTo(1));
                Assert.That(manifest.Packs[0].LastLevel, Is.EqualTo(250));
                Assert.That(manifest.Packs[1].Url, Is.EqualTo("https://cdn.example.com/pictures.jsonl.gz"));
                Assert.That(manifest.Write(), Is.EqualTo(canonical));
            });
        }

        [TestCase("\"shuffleNodeBudget\": 50000", "\"shuffleNodeBudget\": 999", "shuffleNodeBudget")]
        [TestCase("\"shuffleNodeBudget\": 50000,", "", "shuffleNodeBudget")]
        [TestCase("\"path\": \"p.gz\",", "\"path\": \"p.gz\", \"url\": \"https://x/p.gz\",", "packs[0]")]
        [TestCase("\"path\": \"p.gz\",", "", "packs[0]")]
        [TestCase("\"sha256\": \"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"", "\"sha256\": \"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"", "packs[0].sha256")]
        [TestCase("\"minAppVersion\": \"1.0.0\"", "\"minAppVersion\": \"1.0\"", "minAppVersion")]
        [TestCase("\"levelRange\": [1, 250],", "", "packs[0].levelRange")]
        [TestCase("\"kind\": \"levels\"", "\"kind\": \"music\"", "packs[0].kind")]
        public void InvalidManifest_IsRejected(string find, string replace, string path)
        {
            const string valid = @"{ ""contentVersion"": 1, ""minAppVersion"": ""1.0.0"", ""pictureLibraryVersion"": 1,
  ""shuffleNodeBudget"": 50000,
  ""packs"": [ { ""id"": ""l1"", ""kind"": ""levels"", ""levelRange"": [1, 250],
    ""path"": ""p.gz"",
    ""sha256"": ""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"", ""bytes"": 10 } ] }";
            Assert.That(ContentManifest.Read(valid).Packs, Has.Count.EqualTo(1));
            Assert.That(valid, Does.Contain(find), "The test edit must apply.");
            string json = valid.Replace(find, replace);

            var ex = Assert.Throws<ContentFormatException>(() => ContentManifest.Read(json));
            Assert.That(ex!.Path, Is.EqualTo(path));
        }

        [Test]
        public void ContentLoader_VerifiesAndIndexesPacks()
        {
            byte[] levelBytes = LevelPackReader.Write(Enumerable.Range(1, 3).Select(SampleLevel));
            byte[] pictureBytes = PicturePackReader.Write(new[] { SamplePicture });
            PackEntry levelsEntry = EntryFor("levels_0001", PackKind.Levels, levelBytes, 1, 3);
            PackEntry picturesEntry = EntryFor("pictures", PackKind.Pictures, pictureBytes);
            var manifest = new ContentManifest(7, "1.0.0", 1, 3, 40000, new[] { picturesEntry, levelsEntry });
            var files = new Dictionary<string, byte[]> { [levelsEntry.Id] = levelBytes, [picturesEntry.Id] = pictureBytes };

            ContentSet set = ContentLoader.FromPacks(manifest, entry => files[entry.Id]);

            Assert.Multiple(() =>
            {
                Assert.That(set.ContentVersion, Is.EqualTo(7));
                Assert.That(set.ShuffleNodeBudget, Is.EqualTo(40000));
                Assert.That(set.LevelNumbers, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(set.MaxLevel, Is.EqualTo(3));
                Assert.That(set.GetLevel(2).LevelNumber, Is.EqualTo(2));
                Assert.That(set.GetPicture(set.GetLevel(2).Picture).Subject, Is.EqualTo("Tulip"));
                Assert.That(set.TryGetLevel(4, out _), Is.False);
            });
        }

        [Test]
        public void ContentLoader_RejectsALevelWhosePictureIsMissing()
        {
            byte[] levelBytes = LevelPackReader.Write(new[] { SampleLevel(1), SampleLevel(2) });
            PackEntry levelsEntry = EntryFor("levels_0001", PackKind.Levels, levelBytes, 1, 2);
            var manifest = new ContentManifest(1, "1.0.0", 1, null, 40000, new[] { levelsEntry });

            var ex = Assert.Throws<ContentIntegrityException>(() => ContentLoader.FromPacks(manifest, _ => levelBytes));
            Assert.That(ex!.Message, Does.Contain("missing picture sample_tulip v1"));
        }

        [Test]
        public void LooseFolder_LoadsOneFilePerLevelAndPicture()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloomlings-loose-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(root, LooseContentFolder.PicturesFolder));
                Directory.CreateDirectory(Path.Combine(root, LooseContentFolder.LevelsFolder));
                File.WriteAllText(Path.Combine(root, "pictures", "sample_tulip.json"), ContractSchemas.Sample("picture-sample.json"));
                File.WriteAllText(Path.Combine(root, "levels", "level-dev-002.json"), DefinitionJson.Write(SampleLevel(2)));
                File.WriteAllText(Path.Combine(root, "levels", "level-dev-001.json"), DefinitionJson.Write(SampleLevel(1)));

                ContentSet set = LooseContentFolder.Load(root);

                Assert.That(set.ContentVersion, Is.EqualTo(LooseContentFolder.DevContentVersion));
                Assert.That(set.ShuffleNodeBudget, Is.EqualTo(LooseContentFolder.DevShuffleNodeBudget));
                Assert.That(set.LevelNumbers, Is.EqualTo(new[] { 1, 2 }));
                Assert.That(set.PictureCount, Is.EqualTo(1));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void LooseFolder_ErrorsNameTheFile()
        {
            var ex = Assert.Throws<ContentFormatException>(() => LooseContentFolder.FromDocuments(
                Array.Empty<KeyValuePair<string, string>>(),
                new[] { new KeyValuePair<string, string>("level-dev-009.json", "{\"levelNumber\": 0}") }));

            Assert.That(ex!.Path, Is.EqualTo("level-dev-009.json.levelNumber"));
        }
    }
}
