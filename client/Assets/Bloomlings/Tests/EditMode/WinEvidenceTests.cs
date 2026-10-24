using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bloomlings.Client.Services.Economy.Online;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public sealed class WinEvidenceTests
    {
        private static LevelSession NewSession()
        {
            var grid = new IReadOnlyList<int>[8];
            for (int y = 0; y < grid.Length; y++)
            {
                int[] row = Enumerable.Repeat(BasePicture.Empty, 7).ToArray();
                if (y < 2) row[3] = y;
                grid[y] = row;
            }
            var picture = new BasePicture("evidence_test", 1, "Evidence test", 7, 8,
                new[] { new PictureRole("leaf", "Leaf", ColorGroup.Green, false), new PictureRole("flower", "Flower", ColorGroup.PinkPurple, false) },
                grid, new FinishedLook(FinishedLookMode.Auto, null),
                new PictureTags(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()), null,
                new PictureReview(ReviewStatus.Approved, "test", null, null), new PictureSource(PictureSourceKind.Hand, null, "owned"));
            var definition = new LevelDefinition(42, 1, 123, "test", new PictureRef(picture.Id, picture.Version, Mirror.None, "default"),
                new Dictionary<string, VariantId> { ["leaf"] = VariantId.Leaf, ["flower"] = VariantId.Flower },
                new[] { new EntryDef(new CellPos(3, 0), EntrySide.Bottom) }, Array.Empty<CellOverlay>(), Array.Empty<SpecialDef>(), Array.Empty<LockDef>(),
                new SlotsDef(5, null), new TrayDef(new IReadOnlyList<string>[] { new[] { "leaf_pod" }, new[] { "flower_pod" } }),
                new[] { new PodDef("leaf_pod", VariantId.Leaf, 1, false, null, null), new PodDef("flower_pod", VariantId.Flower, 1, false, null, null) },
                new DifficultyDef(DifficultyClass.Hard, 5000, false), "default", Array.Empty<string>());
            return LevelSession.Load(definition, picture, new SessionOptions(7, 20000));
        }

        private static LevelSession Win(bool booster = false)
        {
            LevelSession session = NewSession();
            if (booster) Assert.That(session.Apply(new UseExtraSlot()).Accepted, Is.True);
            Assert.That(session.Apply(new TapPod("leaf_pod")).Accepted, Is.True);
            Assert.That(session.Apply(new TapPod("flower_pod")).Accepted, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            return session;
        }

        private static ValidatedWinEvidence Replay(WinEvidence evidence, LevelSession trusted) =>
            evidence.ValidateAndReplay(trusted.Definition, trusted.Picture, trusted.Options);
        private static string Edit(WinEvidence evidence, Action<JObject> edit)
        {
            JObject json = JObject.Parse(evidence.Write()); edit(json); return json.ToString();
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void RoundTripReplaysTrustedLevelAndCountsActualBoosters(bool booster, int expected)
        {
            LevelSession session = Win(booster);
            WinEvidence evidence = WinEvidence.Create(session);
            WinEvidence read = WinEvidence.Read(evidence.Write());
            Assert.That(Replay(read, session), Is.EqualTo(new ValidatedWinEvidence(42, DifficultyClass.Hard, expected)));
            Assert.That(read.Write(), Is.EqualTo(evidence.Write()));
            Assert.That(read.Hash, Is.EqualTo(PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(read.Write()))));
            Assert.That(read.DefinitionHash, Is.EqualTo(PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(DefinitionJson.Write(session.Definition, false)))));
            Assert.That(read.PictureHash, Is.EqualTo(PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(BasePictureJson.Write(session.Picture, false)))));
            Assert.That(read.AttemptId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EvidenceIsAnImmutableSnapshotAndDoesNotAuthorizeOrDeduplicateClaims()
        {
            LevelSession session = Win();
            WinEvidence evidence = WinEvidence.Create(session);
            string json = evidence.Write();
            session.Apply(new Restart());
            Assert.That(evidence.Write(), Is.EqualTo(json));
            Assert.That(evidence.Commands.Count, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)evidence.Commands)[0] = "restart");
            // Replay consistency alone deliberately grants no one-time reward or attempt authenticity.
            Assert.That(Replay(evidence, session), Is.EqualTo(Replay(evidence, session)));
        }

        [Test]
        public void CannotCreateEvidenceForAnUnfinishedSession()
        {
            Assert.Throws<ContentFormatException>(() => WinEvidence.Create(NewSession()));
        }

        [Test]
        public void RejectedLiveInputsAreAbsentButInjectedRejectedCommandsFailReplay()
        {
            LevelSession session = NewSession();
            Assert.That(session.Apply(new TapPod("missing")).Accepted, Is.False);
            session.Apply(new TapPod("leaf_pod")); session.Apply(new TapPod("flower_pod"));
            WinEvidence evidence = WinEvidence.Create(session);
            Assert.That(evidence.Commands.Count, Is.EqualTo(2));
            string json = Edit(evidence, root => ((JArray)root["commands"]!).Insert(0, "tap:missing"));
            Assert.Throws<ContentFormatException>(() => Replay(WinEvidence.Read(json), session));
            json = Edit(evidence, root => ((JArray)root["commands"]!).Add("tap:leaf_pod"));
            Assert.Throws<ContentFormatException>(() => Replay(WinEvidence.Read(json), session));
        }

        [Test]
        public void AnUnfinishedReplayCannotPassEvenWithItsCorrectStateHash()
        {
            LevelSession session = Win();
            LevelSession unfinished = NewSession(); unfinished.Apply(new TapPod("leaf_pod"));
            string json = Edit(WinEvidence.Create(session), root =>
            {
                ((JArray)root["commands"]!).RemoveAt(1);
                root["finalStateHash"] = unfinished.StateHash.ToString("x16", CultureInfo.InvariantCulture);
            });
            Assert.Throws<ContentFormatException>(() => Replay(WinEvidence.Read(json), session));
        }

        [TestCase("level", "43")]
        [TestCase("contentVersion", "8")]
        [TestCase("definitionHash", "0000000000000000000000000000000000000000000000000000000000000000")]
        [TestCase("pictureHash", "0000000000000000000000000000000000000000000000000000000000000000")]
        [TestCase("finalStateHash", "0000000000000000")]
        public void TamperedIdentifiersAndHashesFailReplay(string key, string value)
        {
            LevelSession session = Win();
            string json = Edit(WinEvidence.Create(session), root => root[key] = key == "level" || key == "contentVersion"
                ? new JValue(int.Parse(value, CultureInfo.InvariantCulture)) : new JValue(value));
            Assert.Throws<ContentFormatException>(() => Replay(WinEvidence.Read(json), session));
        }

        [Test]
        public void TrustedCatalogMustMatchEvenWhenLevelAndPictureIdsAreUnchanged()
        {
            LevelSession session = Win(); WinEvidence evidence = WinEvidence.Create(session);
            LevelDefinition differentDifficulty = session.Definition with { Difficulty = new DifficultyDef(DifficultyClass.SuperHard, 9000, false) };
            BasePicture differentPicture = session.Picture with { Subject = "Different content with the same ID" };
            Assert.Throws<ContentFormatException>(() => evidence.ValidateAndReplay(differentDifficulty, session.Picture, session.Options));
            Assert.Throws<ContentFormatException>(() => evidence.ValidateAndReplay(session.Definition, differentPicture, session.Options));
            Assert.Throws<ContentFormatException>(() => evidence.ValidateAndReplay(session.Definition, session.Picture, session.Options with { ContentVersion = 8 }));
        }

        [TestCase("restart")]
        [TestCase("return:00")]
        [TestCase("unknown")]
        [TestCase("tap:")]
        [TestCase("burst:BAD")]
        public void MalformedOrNonLogCommandsAreRejected(string command)
        {
            string json = Edit(WinEvidence.Create(Win()), root => ((JArray)root["commands"]!).Insert(0, command));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(json));
        }

        [TestCase("bonus")]
        [TestCase("elapsedSeconds")]
        [TestCase("freeDemoCount")]
        [TestCase("boostersUsed")]
        [TestCase("difficulty")]
        public void ClientSuppliedRewardFactsAreNotAccepted(string key)
        {
            string json = Edit(WinEvidence.Create(Win()), root => root[key] = 0);
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(json));
        }

        [Test]
        public void SchemaGuidTypesDuplicatesAndTrailingDocumentsAreValidated()
        {
            WinEvidence evidence = WinEvidence.Create(Win());
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(Edit(evidence, root => root["schemaVersion"] = 2)));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(Edit(evidence, root => root["attemptId"] = "not-a-guid")));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(Edit(evidence, root => root["attemptId"] = Guid.Empty.ToString("N"))));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(Edit(evidence, root => root["level"] = "42")));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(Edit(evidence, root => root["commands"] = new JArray(123))));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(evidence.Write().Insert(1, "\"level\":42,")));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(evidence.Write() + "{}"));
        }

        [Test]
        public void PayloadLimitsApplyToUtf8BytesAndCommandCountBeforeReplay()
        {
            WinEvidence evidence = WinEvidence.Create(Win());
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(new string(' ', WinEvidence.MaxBytes + 1)));
            string unicode = Edit(evidence, root => root["commands"] = new JArray("tap:" + new string('é', WinEvidence.MaxBytes / 2)));
            Assert.That(unicode.Length, Is.LessThan(WinEvidence.MaxBytes));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(unicode));
            string excessive = Edit(evidence, root => root["commands"] = new JArray(Enumerable.Repeat("extra_slot", WinEvidence.MaxCommands + 1)));
            Assert.That(Encoding.UTF8.GetByteCount(excessive), Is.LessThan(WinEvidence.MaxBytes));
            Assert.Throws<ContentFormatException>(() => WinEvidence.Read(excessive));
        }
    }
}
