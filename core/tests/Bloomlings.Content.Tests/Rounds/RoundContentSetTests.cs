using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Content.Tests.Rounds
{
    /// <summary>
    /// The published round content as the clients read it (spec 006 T025, T053): the exported L1–L20 packed with
    /// <see cref="RoundPacks.Build"/>, read back through <see cref="RoundContentSet"/> with each pack read on first use and
    /// checked against the manifest.
    /// </summary>
    [TestFixture]
    public class RoundContentSetTests
    {
        private const string Export = "content/game/rounds/export/L0001-L0020";

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

        /// <summary>The export's levels and pictures published as files by their paths (<c>manifest.json</c> included).</summary>
        private static Dictionary<string, byte[]> Published()
        {
            string folder = Path.Combine(RepositoryRoot, Export);
            List<RoundLevel> levels = Directory.GetFiles(Path.Combine(folder, "levels"), "*.json").Select(f => RoundJson.ReadLevel(File.ReadAllText(f))).ToList();
            List<RoundPicture> pictures = Directory.GetFiles(Path.Combine(folder, "pictures"), "*.json").Select(f => RoundJson.ReadPicture(File.ReadAllText(f))).ToList();
            RoundContentManifest manifest = RoundPacks.Build(1, levels, pictures, out IReadOnlyList<RoundPackFile> packs);
            var files = packs.ToDictionary(p => p.Entry.Path, p => p.Bytes, StringComparer.Ordinal);
            files[RoundContentManifest.FileName] = Encoding.UTF8.GetBytes(manifest.Write());
            return files;
        }

        [Test]
        public void TheExportedWave_ReadsBackLevelByLevel_OnFirstUse()
        {
            Dictionary<string, byte[]> files = Published();
            var reads = new List<string>();
            RoundContentSet set = RoundContentSet.Read(path =>
            {
                reads.Add(path);
                return files.TryGetValue(path, out byte[]? bytes) ? bytes : null;
            });

            Assert.Multiple(() =>
            {
                Assert.That(reads, Is.EqualTo(new[] { RoundContentManifest.FileName }), "only the manifest is read at start");
                Assert.That(set.LevelCount, Is.EqualTo(20));
                Assert.That(set.Has(1) && set.Has(20) && !set.Has(0) && !set.Has(21), Is.True);
                Assert.That(set.Listed(18).PictureId, Is.EqualTo(set.GetLevel(18).PictureId));
            });

            RoundLevel l6 = set.GetLevel(6);
            RoundPicture picture = set.GetPicture(l6);
            set.GetLevel(7);
            set.GetPicture(set.GetLevel(20));
            Assert.Multiple(() =>
            {
                Assert.That(l6.Number, Is.EqualTo(6));
                Assert.That(picture.Id, Is.EqualTo(l6.PictureId));
                Assert.That(picture.Version, Is.EqualTo(l6.PictureVersion));
                Assert.That(reads.Count, Is.EqualTo(3), "the level pack and the picture pack are each read once");
                Assert.That(set.ReadPacks, Is.EquivalentTo(new[] { "round_levels_0001_0020", RoundPacks.PicturePackId }));
                Assert.That(set.TryGetLevel(21, out RoundLevel? none), Is.False);
                Assert.That(none, Is.Null);
                Assert.Throws<ArgumentOutOfRangeException>(() => set.GetLevel(21));
            });
        }

        [Test]
        public void NoManifest_SaysNothingIsPublished()
        {
            ContentIntegrityException error = Assert.Throws<ContentIntegrityException>(() => RoundContentSet.Read(_ => null))!;
            Assert.That(error.Message, Does.Contain("no published round content"));
        }

        [Test]
        public void ATamperedPack_IsRefusedWhenItIsRead()
        {
            Dictionary<string, byte[]> files = Published();
            string pack = files.Keys.Single(k => k.StartsWith("levels/", StringComparison.Ordinal));
            byte[] bytes = (byte[])files[pack].Clone();
            bytes[bytes.Length - 1] ^= 1;
            files[pack] = bytes;
            RoundContentSet set = RoundContentSet.Read(path => files.TryGetValue(path, out byte[]? b) ? b : null);

            Assert.Multiple(() =>
            {
                Assert.That(set.LevelCount, Is.EqualTo(20));
                Assert.Throws<ContentIntegrityException>(() => set.GetLevel(1));
            });
        }

        [Test]
        public void AMissingPack_IsNamed()
        {
            Dictionary<string, byte[]> files = Published();
            files.Remove("pictures/" + RoundPacks.PicturePackId + ".jsonl.gz");
            RoundContentSet set = RoundContentSet.Read(path => files.TryGetValue(path, out byte[]? b) ? b : null);

            ContentIntegrityException error = Assert.Throws<ContentIntegrityException>(() => set.GetPicture(set.GetLevel(1)))!;
            Assert.That(error.Message, Does.Contain("pictures/round_pictures.jsonl.gz"));
        }
    }
}
