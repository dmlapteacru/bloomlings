using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Economy.Online;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public sealed class GuestWinArchiveTests
    {
        private string _folder = null!;
        [SetUp] public void SetUp() => _folder = Path.Combine(Path.GetTempPath(), "guest-win-archive-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); }

        private static WinEvidence Evidence(int level = 42)
        {
            var grid = new IReadOnlyList<int>[8];
            for (int y = 0; y < grid.Length; y++)
            {
                int[] row = Enumerable.Repeat(BasePicture.Empty, 7).ToArray();
                if (y < 2) row[3] = y;
                grid[y] = row;
            }
            var picture = new BasePicture("archive_test", 1, "Archive test", 7, 8,
                new[] { new PictureRole("leaf", "Leaf", ColorGroup.Green, false), new PictureRole("flower", "Flower", ColorGroup.PinkPurple, false) },
                grid, new FinishedLook(FinishedLookMode.Auto, null),
                new PictureTags(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()), null,
                new PictureReview(ReviewStatus.Approved, "test", null, null), new PictureSource(PictureSourceKind.Hand, null, "owned"));
            var definition = new LevelDefinition(level, 1, 123, "test", new PictureRef(picture.Id, picture.Version, Mirror.None, "default"),
                new Dictionary<string, VariantId> { ["leaf"] = VariantId.Leaf, ["flower"] = VariantId.Flower },
                new[] { new EntryDef(new CellPos(3, 0), EntrySide.Bottom) }, Array.Empty<CellOverlay>(), Array.Empty<SpecialDef>(), Array.Empty<LockDef>(),
                new SlotsDef(5, null), new TrayDef(new IReadOnlyList<string>[] { new[] { "leaf_pod" }, new[] { "flower_pod" } }),
                new[] { new PodDef("leaf_pod", VariantId.Leaf, 1, false, null, null), new PodDef("flower_pod", VariantId.Flower, 1, false, null, null) },
                new DifficultyDef(DifficultyClass.Normal, 1000, false), "default", Array.Empty<string>());
            LevelSession session = LevelSession.Load(definition, picture, new SessionOptions(7, 20000));
            session.Apply(new TapPod("leaf_pod")); session.Apply(new TapPod("flower_pod"));
            return WinEvidence.Create(session);
        }

        private string PathFor(int level = 42) => Path.Combine(_folder, "level-" + level.ToString("D5", System.Globalization.CultureInfo.InvariantCulture) + ".json");

        [Test]
        public void FirstWinSurvivesANewArchiveInstance()
        {
            var archive = new GuestWinArchive(_folder); WinEvidence evidence = Evidence();
            Assert.That(archive.Read(42), Is.Null);
            Assert.That(archive.Store(evidence), Is.True);
            WinEvidence read = new GuestWinArchive(_folder).Read(42)!;
            Assert.That(read.Write(), Is.EqualTo(evidence.Write()));
            Assert.That(read.Hash, Is.EqualTo(evidence.Hash));
            Assert.That(Directory.GetFiles(_folder).Select(Path.GetFileName), Is.EquivalentTo(new[] { "level-00042.json" }));
        }

        [Test]
        public void DuplicateLevelKeepsTheFirstAttemptAndBytes()
        {
            var archive = new GuestWinArchive(_folder); WinEvidence first = Evidence(), later = Evidence();
            Assert.That(later.AttemptId, Is.Not.EqualTo(first.AttemptId));
            archive.Store(first); byte[] original = File.ReadAllBytes(PathFor());
            Assert.That(archive.Store(first), Is.False);
            Assert.That(new GuestWinArchive(_folder).Store(later), Is.False);
            Assert.That(File.ReadAllBytes(PathFor()), Is.EqualTo(original));
            Assert.That(archive.Read(42)!.AttemptId, Is.EqualTo(first.AttemptId));
        }

        [Test]
        public void ConcurrentFirstWritersCommitExactlyOneWholeRecord()
        {
            var a = new GuestWinArchive(_folder); var b = new GuestWinArchive(_folder);
            WinEvidence first = Evidence(), second = Evidence(); bool storedA = false, storedB = false;
            Parallel.Invoke(() => storedA = a.Store(first), () => storedB = b.Store(second));
            Assert.That(storedA ^ storedB, Is.True);
            Assert.That(a.Read(42)!.Hash, Is.EqualTo(storedA ? first.Hash : second.Hash));
            Assert.That(Directory.GetFiles(_folder).Length, Is.EqualTo(1));
        }

        [Test]
        public void AllReturnsOnlyCommittedRecordsInLevelOrder()
        {
            var archive = new GuestWinArchive(_folder);
            archive.Store(Evidence(2500)); archive.Store(Evidence(1)); archive.Store(Evidence(42));
            File.WriteAllText(Path.Combine(_folder, ".win-interrupted.tmp"), "partial");
            IReadOnlyList<WinEvidence> all = archive.All();
            Assert.That(all.Select(e => e.Level), Is.EqualTo(new[] { 1, 42, 2500 }));
            Assert.Throws<NotSupportedException>(() => ((IList<WinEvidence>)all).Clear());
        }

        [TestCase("")]
        [TestCase("{\"hash\":")]
        [TestCase("{}")]
        public void TruncatedOrMalformedCommittedEntriesAreNeverOverwritten(string damaged)
        {
            var archive = new GuestWinArchive(_folder); File.WriteAllText(PathFor(), damaged);
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
            Assert.Throws<InvalidDataException>(() => archive.Store(Evidence()));
            Assert.Throws<InvalidDataException>(() => archive.All());
            Assert.That(File.ReadAllText(PathFor()), Is.EqualTo(damaged));
        }

        [Test]
        public void ChecksumDetectsAWellFormedButChangedAttempt()
        {
            var archive = new GuestWinArchive(_folder); archive.Store(Evidence());
            JObject wrapper = JObject.Parse(File.ReadAllText(PathFor()));
            JObject evidence = JObject.Parse((string)wrapper["evidence"]!);
            evidence["attemptId"] = Guid.NewGuid().ToString("N");
            wrapper["evidence"] = WinEvidence.Read(evidence.ToString()).Write();
            File.WriteAllText(PathFor(), wrapper.ToString());
            byte[] damaged = File.ReadAllBytes(PathFor());
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
            Assert.Throws<InvalidDataException>(() => archive.Store(Evidence()));
            Assert.That(File.ReadAllBytes(PathFor()), Is.EqualTo(damaged));
        }

        [Test]
        public void AValidRecordForAnotherLevelCannotOccupyTheLevelFilename()
        {
            var archive = new GuestWinArchive(_folder); archive.Store(Evidence(1));
            File.Copy(PathFor(1), PathFor(42));
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
            Assert.Throws<InvalidDataException>(() => archive.Store(Evidence(42)));
            Assert.That(archive.Read(1)!.Level, Is.EqualTo(1));
        }

        [Test]
        public void UnknownMetadataAndNonCanonicalEvidenceAreRejected()
        {
            var archive = new GuestWinArchive(_folder); archive.Store(Evidence());
            string original = File.ReadAllText(PathFor()); JObject wrapper = JObject.Parse(original);
            wrapper["grantPetals"] = 999; File.WriteAllText(PathFor(), wrapper.ToString());
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
            wrapper = JObject.Parse(original); wrapper["evidence"] = " " + (string)wrapper["evidence"]!;
            File.WriteAllText(PathFor(), wrapper.ToString());
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
        }

        [Test]
        public void OversizedAndInvalidUtf8FilesAreRejectedWithoutReplacement()
        {
            var archive = new GuestWinArchive(_folder);
            using (var stream = File.Create(PathFor())) stream.SetLength(3L * WinEvidence.MaxBytes);
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
            Assert.Throws<InvalidDataException>(() => archive.Store(Evidence()));
            Assert.That(new FileInfo(PathFor()).Length, Is.EqualTo(3L * WinEvidence.MaxBytes));
            File.WriteAllBytes(PathFor(), new byte[] { 0xff, 0xfe });
            Assert.Throws<InvalidDataException>(() => archive.Read(42));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(2501)]
        public void InvalidReadLevelsCannotBecomePaths(int level)
        {
            var archive = new GuestWinArchive(_folder);
            Assert.Throws<ArgumentOutOfRangeException>(() => archive.Read(level));
            Assert.That(Directory.GetFiles(_folder), Is.Empty);
        }

        [Test]
        public void OutOfRangeStoreAndWriteFailuresNeverReportACommittedWin()
        {
            var archive = new GuestWinArchive(_folder);
            Assert.Throws<ArgumentOutOfRangeException>(() => archive.Store(Evidence(2501)));
            Directory.Delete(_folder);
            Assert.Throws<DirectoryNotFoundException>(() => archive.Store(Evidence()));
            Assert.That(archive.Read(42), Is.Null);
        }
    }
}
