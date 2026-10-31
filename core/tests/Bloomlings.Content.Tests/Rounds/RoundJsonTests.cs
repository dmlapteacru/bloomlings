using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Content.Schemas;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Content.Tests.Rounds
{
    /// <summary>
    /// The round formats of spec 006 (T010): each sample round-trips byte for byte, the hand-written readers and the
    /// embedded schemas (evaluated with JsonSchema.Net) refuse the same broken documents, and the published packs read back.
    /// </summary>
    [TestFixture]
    public class RoundJsonTests
    {
        private static readonly string LevelSample = ContractSchemas.Sample("round-level.json");
        private static readonly string PictureSample = ContractSchemas.Sample("round-picture.json");
        private static readonly string ProofSample = ContractSchemas.Sample("round-proof.json");

        [Test]
        public void Samples_AreValidAgainstTheEmbeddedSchemas()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundLevel(LevelSample), Is.True);
                Assert.That(ContractSchemas.IsValidRoundPicture(PictureSample), Is.True);
                Assert.That(ContractSchemas.IsValidRoundProof(ProofSample), Is.True);
            });
        }

        [Test]
        public void Level_RoundTripsByteForByte() =>
            Assert.That(RoundJson.WriteLevel(RoundJson.ReadLevel(LevelSample)), Is.EqualTo(LevelSample));

        [Test]
        public void Picture_RoundTripsByteForByte() =>
            Assert.That(RoundJson.WritePicture(RoundJson.ReadPicture(PictureSample)), Is.EqualTo(PictureSample));

        [Test]
        public void Proof_RoundTripsByteForByte() =>
            Assert.That(RoundJson.WriteProof(RoundJson.ReadProof(ProofSample)), Is.EqualTo(ProofSample));

        [Test]
        public void Level_MapsToTheCoreModel()
        {
            RoundLevel level = RoundJson.ReadLevel(LevelSample);
            RoundPrepared prepared = level.Prepare();

            Assert.Multiple(() =>
            {
                Assert.That(level.Number, Is.EqualTo(1));
                Assert.That(level.Class, Is.EqualTo(RoundClass.Normal));
                Assert.That((level.Width, level.Height), Is.EqualTo((7, 8)));
                Assert.That(level.Pods.Select(p => (p.Code, p.Count)), Is.EqualTo(new[] { ('G', 4), ('P', 4) }));
                Assert.That(level.Tray.Select(c => c.ToArray()), Is.EqualTo(new[] { new[] { 0 }, new[] { 1 } }));
                Assert.That(level.Entries, Is.EqualTo(new[] { 3 }));
                Assert.That(level.Bridge, Is.Null);
                Assert.That(level.PictureId, Is.EqualTo("P9001"));
                Assert.That(level.Engine!.Rules, Is.EqualTo(new string('1', 64)));
                Assert.That(prepared.Board[3], Is.EqualTo(RoundLevel.Open), "the gate");
                Assert.That(prepared.Board[(3 * 7) + 2], Is.EqualTo((sbyte)RoundMaterial.IndexOf('P')));
            });
        }

        [Test]
        public void PictureBoard_IsTheLevelsBoard()
        {
            RoundBoardShape board = RoundPictureBoard.Of(RoundJson.ReadPicture(PictureSample));
            RoundLevel level = RoundJson.ReadLevel(LevelSample);

            Assert.Multiple(() =>
            {
                Assert.That((board.Width, board.Height), Is.EqualTo((level.Width, level.Height)));
                Assert.That(board.RowsBottomFirst, Is.EqualTo(level.RowsBottomFirst));
                Assert.That(board.Entries, Is.EqualTo(level.Entries));
                Assert.That(board.Bridge, Is.Null);
            });
        }

        [Test]
        public void PictureBoard_TurnsBridgeSitesIntoWallsWithAnchorsBelow()
        {
            var picture = new RoundPicture("P9002", 1, "River", 5, 4, new[] { "GGGGG", "#b#b#", "PPPPP", "....." }, "2026-10-27");
            RoundBoardShape board = RoundPictureBoard.Of(picture);

            int w = board.Width;
            Assert.Multiple(() =>
            {
                Assert.That(board.Bridge, Has.Count.EqualTo(2));
                Assert.That(board.RowsBottomFirst[4], Is.EqualTo(new string('#', 9)), "the stream row closes the ring");
                foreach (RoundBridgeChoice choice in board.Bridge!)
                {
                    Assert.That(board.RowsBottomFirst[choice.Cell / w][choice.Cell % w], Is.EqualTo('#'));
                    Assert.That(choice.Anchor, Is.EqualTo(choice.Cell - w));
                }

                Assert.That(board.Bridge![0].Label, Is.EqualTo("Мостик вверх слева"));
                Assert.That(board.Bridge![1].Label, Is.EqualTo("Мостик вверх справа"));
            });
        }

        [Test]
        public void Level_WithEveryOptionalPart_RoundTrips()
        {
            RoundLevel sample = RoundJson.ReadLevel(LevelSample);
            var level = new RoundLevel(
                18,
                sample.Width,
                sample.Height,
                sample.RowsBottomFirst,
                new[] { new RoundPod('G', 2, RoundMystery.Depth), new RoundPod('G', 2, group: "A"), new RoundPod('P', 4, group: "A") },
                new IReadOnlyList<int>[] { new[] { 0, 1 }, new[] { 2 } },
                new[] { 3 },
                new[] { new RoundBridgeChoice(40, 33, "Левый мостик"), new RoundBridgeChoice(44, 37, "Правый мостик") },
                5,
                RoundClass.SuperHard,
                2,
                new RoundIntroduction("bridge", true),
                "P9001",
                1,
                new[] { "hidden", "links", "bridge" },
                new RoundException("one deciding bridge from the second introduction", "owner kept both winning bridges", "owner", "2026-10-27"),
                sample.Engine);

            string json = RoundJson.WriteLevel(level);
            RoundLevel back = RoundJson.ReadLevel(json);

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundLevel(json), Is.True);
                Assert.That(RoundJson.WriteLevel(back), Is.EqualTo(json));
                Assert.That(back.Class, Is.EqualTo(RoundClass.SuperHard));
                Assert.That(back.Pods[0].Mystery, Is.EqualTo(RoundMystery.Depth));
                Assert.That(back.Pods[2].Group, Is.EqualTo("A"));
                Assert.That(back.Bridge!.Select(c => (c.Cell, c.Anchor)), Is.EqualTo(new[] { (40, 33), (44, 37) }));
                Assert.That(back.Introduction!.Second, Is.True);
                Assert.That(back.Exception!.Rule, Is.EqualTo("one deciding bridge from the second introduction"));
                Assert.That(json, Does.Contain("\"Super Hard\""));
            });
        }

        [Test]
        public void Level_WithAnOverlay_RoundTrips()
        {
            // 17 §8 as amended on 2026-10-28: board.overlay lays walls and sites over the picture, in picture coordinates.
            JObject root = JObject.Parse(LevelSample);
            root["board"]!["overlay"] = new JObject
            {
                ["walls"] = new JArray(new JArray(0, 1), new JArray(1, 1), new JArray(2, 1)),
                ["sites"] = new JArray(new JArray(0, 1), new JArray(2, 1)),
            };
            string json = root.ToString();

            RoundLevel level = RoundJson.ReadLevel(json);
            string written = RoundJson.WriteLevel(level);

            Assert.Multiple(() =>
            {
                Assert.That(level.Overlay!.Walls, Is.EqualTo(new[] { (0, 1), (1, 1), (2, 1) }));
                Assert.That(level.Overlay.Sites, Is.EqualTo(new[] { (0, 1), (2, 1) }));
                Assert.That(level.PictureCell(1, 1), Is.EqualTo(((level.Height - 4) * level.Width) + 3), "picture (1, 1) is board column 3, row 3 from the top");
                Assert.That(RoundJson.WriteLevel(RoundJson.ReadLevel(written)), Is.EqualTo(written));
                Assert.That(JObject.Parse(written)["board"]!["overlay"]!.ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo("{\"sites\":[[0,1],[2,1]],\"walls\":[[0,1],[1,1],[2,1]]}"));
                if (SchemaResources.Read(SchemaResources.RoundLevel).Contains("\"overlay\""))
                {
                    // The schema gains board.overlay with the JS side's change of the same day.
                    Assert.That(ContractSchemas.IsValidRoundLevel(written), Is.True);
                }
            });
        }

        [TestCase("[[3,0]]", "board.overlay.walls[0][0]", TestName = "OverlayWallRightOfThePicture_IsRejected")]
        [TestCase("[[0,4]]", "board.overlay.walls[0][1]", TestName = "OverlayWallBelowThePicture_IsRejected")]
        [TestCase("[[-1,0]]", "board.overlay.walls[0][0]", TestName = "OverlayWallLeftOfThePicture_IsRejected")]
        [TestCase("[[0,1,2]]", "board.overlay.walls[0]", TestName = "OverlayCellOfThreeNumbers_IsRejected")]
        public void Level_OverlayCellOutsideThePicture_IsRejected(string walls, string path)
        {
            // The sample's board is 7 × 8: its picture is 3 × 4.
            JObject root = JObject.Parse(LevelSample);
            root["board"]!["overlay"] = new JObject { ["walls"] = JArray.Parse(walls), ["sites"] = new JArray(new JArray(0, 0), new JArray(1, 0)) };

            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(root.ToString()));

            Assert.That(ex!.Path, Is.EqualTo(path));
        }

        [TestCase("[[0,1]]", TestName = "OverlayWithOneSite_IsRejected")]
        [TestCase("[[0,1],[1,1],[2,1]]", TestName = "OverlayWithThreeSites_IsRejected")]
        public void Level_OverlayWithoutTwoSites_IsRejected(string sites)
        {
            JObject root = JObject.Parse(LevelSample);
            root["board"]!["overlay"] = new JObject { ["walls"] = new JArray(), ["sites"] = JArray.Parse(sites) };

            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(root.ToString()));

            Assert.That(ex!.Path, Is.EqualTo("board.overlay.sites"));
        }

        [Test]
        public void Level_OverlayWithoutSites_IsRejected()
        {
            JObject root = JObject.Parse(LevelSample);
            root["board"]!["overlay"] = new JObject { ["walls"] = new JArray() };

            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(root.ToString()));

            Assert.That(ex!.Path, Is.EqualTo("board.overlay.sites"));
        }

        [Test]
        public void Level_WithSealedMystery_IsNotWritable()
        {
            RoundLevel sample = RoundJson.ReadLevel(LevelSample);
            var level = new RoundLevel(1, sample.Width, sample.Height, sample.RowsBottomFirst, new[] { new RoundPod('G', 4, RoundMystery.Sealed), new RoundPod('P', 4) }, sample.Tray, pictureId: "P9001", engine: sample.Engine);

            Assert.Throws<ArgumentException>(() => RoundJson.WriteLevel(level));
        }

        [Test]
        public void Proof_ReadsBridgeCommandsAndMicroseconds()
        {
            JObject proof = JObject.Parse(ProofSample);
            ((JArray)proof["solution"]!["commands"]!).Add(new JObject { ["type"] = "bridge", ["choice"] = 1, ["atUs"] = 5_000_000_000L });
            string json = proof.ToString();

            RoundProof read = RoundJson.ReadProof(json);

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundProof(json), Is.True);
                Assert.That(read.Solution.Commands[2], Is.EqualTo(new RoundCommand(RoundCommandType.Bridge, 1, 5_000_000_000L)));
                Assert.That(read.RandomTest.Runs, Is.EqualTo(100));
                Assert.That(read.RandomTest.WilsonLow, Is.EqualTo(0.963m));
                Assert.That(read.HasCheck("pressure"), Is.True);
                Assert.That(read.Audit.Notes, Is.EqualTo(new[] { "layer B: none" }));
            });
        }

        // ------------------------------------------------------------ schema rejections (the reader and the schema agree)

        [TestCase("pods[0].code", "X", TestName = "BadCode_InAPod_IsRejected")]
        [TestCase("board.rowsBottomFirst[3]", "#.XP..#", TestName = "BadCode_InARow_IsRejected")]
        [TestCase("board.w", 49, TestName = "FortyNineWideBoard_IsRejected")]
        [TestCase("board.h", 7, TestName = "SevenHighBoard_IsRejected")]
        [TestCase("capacity", 4, TestName = "CapacityFour_IsRejected")]
        [TestCase("pods[1].count", 61, TestName = "CountSixtyOne_IsRejected")]
        [TestCase("pods[1].count", 0, TestName = "CountZero_IsRejected")]
        [TestCase("picture.id", "P12", TestName = "BadPictureCode_IsRejected")]
        [TestCase("class", "Easy", TestName = "UnknownClass_IsRejected")]
        [TestCase("format", "round-level.v2", TestName = "OtherFormat_IsRejected")]
        [TestCase("engine.rules", "abc", TestName = "ShortFingerprint_IsRejected")]
        public void Level_SchemaViolation_IsRejectedByBoth(string path, object value)
        {
            JObject level = JObject.Parse(LevelSample);
            level.SelectToken(path)!.Replace(JToken.FromObject(value));
            string json = level.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundLevel(json), Is.False, "schema");
                Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(json), "reader");
            });
        }

        [Test]
        public void Level_FortyNineWideBoard_NamesThePath()
        {
            JObject level = JObject.Parse(LevelSample);
            level["board"]!["w"] = 49;

            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(level.ToString()));

            Assert.That(ex!.Path, Is.EqualTo("board.w"));
        }

        [TestCase("hero")]
        [TestCase("unknown")]
        public void Level_UnknownProperty_IsRejectedByBoth(string name)
        {
            JObject level = JObject.Parse(LevelSample);
            level[name] = 1;
            string json = level.ToString();

            Assert.That(ContractSchemas.IsValidRoundLevel(json), Is.False);
            Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(json));
        }

        [TestCase("format")]
        [TestCase("number")]
        [TestCase("definitionVersion")]
        [TestCase("class")]
        [TestCase("picture")]
        [TestCase("board")]
        [TestCase("capacity")]
        [TestCase("pods")]
        [TestCase("tray")]
        [TestCase("mechanics")]
        [TestCase("engine")]
        public void Level_MissingRequiredField_IsRejectedByBoth(string name)
        {
            JObject level = JObject.Parse(LevelSample);
            level.Remove(name);
            string json = level.ToString();

            Assert.That(ContractSchemas.IsValidRoundLevel(json), Is.False);
            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(json));
            Assert.That(ex!.Path, Is.EqualTo(name));
        }

        [Test]
        public void Level_RowsNotOfTheDeclaredSize_AreRejectedByTheReader()
        {
            JObject level = JObject.Parse(LevelSample);
            level["board"]!["rowsBottomFirst"]![2] = "#....#";

            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(level.ToString()));

            Assert.That(ex!.Path, Is.EqualTo("board.rowsBottomFirst[2]"));
        }

        [Test]
        public void Level_TrayWithOneColumn_IsRejectedByBoth()
        {
            JObject level = JObject.Parse(LevelSample);
            level["tray"] = new JArray(new JArray(0, 1));
            string json = level.ToString();

            Assert.That(ContractSchemas.IsValidRoundLevel(json), Is.False);
            Assert.Throws<ContentFormatException>(() => RoundJson.ReadLevel(json));
        }

        [Test]
        public void Proof_ABridgeLevelsRandomTestNamesItsBridge_AndRoundTrips()
        {
            JObject proof = JObject.Parse(ProofSample);
            proof["randomTest"]!["bridge"] = 1;
            string json = proof.ToString();

            RoundProof read = RoundJson.ReadProof(json);

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundProof(json), Is.True);
                Assert.That(read.RandomTest.Bridge, Is.EqualTo(1));
                Assert.That(RoundJson.ReadProof(RoundJson.WriteProof(read)).RandomTest.Bridge, Is.EqualTo(1));
                Assert.That(RoundJson.ReadProof(ProofSample).RandomTest.Bridge, Is.Null, "absent: every bridge may be built");
            });
        }

        [TestCase(-1)]
        [TestCase("left")]
        public void Proof_ABadRandomTestBridge_IsRejectedByBoth(object value)
        {
            JObject proof = JObject.Parse(ProofSample);
            proof["randomTest"]!["bridge"] = JToken.FromObject(value);
            string json = proof.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundProof(json), Is.False, "schema");
                Assert.Throws<ContentFormatException>(() => RoundJson.ReadProof(json), "reader");
            });
        }

        [TestCase("format")]
        [TestCase("number")]
        [TestCase("definitionVersion")]
        [TestCase("solution")]
        [TestCase("randomTest")]
        [TestCase("checks")]
        [TestCase("audit")]
        [TestCase("engine")]
        public void Proof_MissingRequiredField_IsRejectedByBoth(string name)
        {
            JObject proof = JObject.Parse(ProofSample);
            proof.Remove(name);
            string json = proof.ToString();

            Assert.That(ContractSchemas.IsValidRoundProof(json), Is.False);
            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadProof(json));
            Assert.That(ex!.Path, Is.EqualTo(name));
        }

        [TestCase("solution", "endUs")]
        [TestCase("randomTest", "wilson95")]
        [TestCase("randomTest", "policy")]
        [TestCase("audit", "report")]
        [TestCase("engine", "clock")]
        public void Proof_MissingNestedField_IsRejectedByBoth(string parent, string name)
        {
            JObject proof = JObject.Parse(ProofSample);
            ((JObject)proof[parent]!).Remove(name);
            string json = proof.ToString();

            Assert.That(ContractSchemas.IsValidRoundProof(json), Is.False);
            var ex = Assert.Throws<ContentFormatException>(() => RoundJson.ReadProof(json));
            Assert.That(ex!.Path, Is.EqualTo(parent + "." + name));
        }

        [TestCase("randomTest.runs", 200)]
        [TestCase("randomTest.share", 1.5)]
        [TestCase("solution.outcome", "Jammed")]
        [TestCase("solution.commands[0].type", "shuffle")]
        [TestCase("audit.result", "NO")]
        public void Proof_SchemaViolation_IsRejectedByBoth(string path, object value)
        {
            JObject proof = JObject.Parse(ProofSample);
            proof.SelectToken(path)!.Replace(JToken.FromObject(value));
            string json = proof.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundProof(json), Is.False, "schema");
                Assert.Throws<ContentFormatException>(() => RoundJson.ReadProof(json), "reader");
            });
        }

        [Test]
        public void Proof_TapWithAChoice_IsRejectedByBoth()
        {
            JObject proof = JObject.Parse(ProofSample);
            proof["solution"]!["commands"]![0]!["choice"] = 1;
            string json = proof.ToString();

            Assert.That(ContractSchemas.IsValidRoundProof(json), Is.False);
            Assert.Throws<ContentFormatException>(() => RoundJson.ReadProof(json));
        }

        [TestCase("id", "P9")]
        [TestCase("w", 45)]
        [TestCase("rights", "franchise")]
        [TestCase("approval.status", "draft")]
        [TestCase("rows[0]", "GXP")]
        public void Picture_SchemaViolation_IsRejectedByBoth(string path, object value)
        {
            JObject picture = JObject.Parse(PictureSample);
            picture.SelectToken(path)!.Replace(JToken.FromObject(value));
            string json = picture.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundPicture(json), Is.False, "schema");
                Assert.Throws<ContentFormatException>(() => RoundJson.ReadPicture(json), "reader");
            });
        }

        [Test]
        public void Picture_NotRectangular_IsRejectedByTheReader()
        {
            JObject picture = JObject.Parse(PictureSample);
            picture["rows"]![1] = "GG";

            Assert.Throws<ContentFormatException>(() => RoundJson.ReadPicture(picture.ToString()));
        }

        [Test]
        public void DefinitionRules_AreTheValidatorsJob()
        {
            // The schema and the reader accept a level whose accounting is wrong (5 G cells, pods count 4 + 4 …): the
            // definition rules are RoundLevelValidator's (T008).
            JObject level = JObject.Parse(LevelSample);
            level["pods"]![0]!["count"] = 9;
            string json = level.ToString();

            Assert.That(ContractSchemas.IsValidRoundLevel(json), Is.True);
            Assert.DoesNotThrow(() => RoundJson.ReadLevel(json));
        }

        // ------------------------------------------------------------ T008 seam
        // TODO(T008, after the merge): assert RoundLevelValidator's errors on levels read by RoundJson here:
        //   "board 7–48 wide, 8–48 high"; row length and characters; "tray is a partition of the pods", 2–6 columns,
        //   capacity 5; pod count 1–60; "exact accounting: cells of each code equal the sum of counts" (the level of
        //   DefinitionRules_AreTheValidatorsJob); "groups are pairs in different columns at equal start depth"; "hidden pods
        //   are at most 6, never in a group"; "bridge sites are walls with an orthogonal neighbour as anchor"; mechanics match pods
        //   and hero; an exception accepted only for the rule it names.
        [Test]
        [Ignore("T008: RoundLevelValidator is written in parallel; assert its errors here after the merge.")]
        public void Validator_Errors_OnLevelsReadFromJson()
        {
        }

        // ------------------------------------------------------------ fixtures, manifest and packs

        [Test]
        public void Fixture_ReadsTheContractShape()
        {
            string json = @"{
  ""format"": ""round-conformance.v1"", ""number"": 1, ""definitionVersion"": 1,
  ""engine"": " + JObject.Parse(LevelSample)["engine"] + @",
  ""solution"": {
    ""windows"": [{""command"": {""type"": ""tap"", ""pod"": 0, ""atUs"": 0}, ""events"": ""sha256:aa"", ""signature"": ""sha256:bb""}],
    ""outcome"": ""Won"", ""endUs"": 6600000, ""finalSignature"": ""sha256:cc""
  },
  ""random"": {""policy"": ""17§5 extended"", ""seedBase"": 1, ""runs"": 1,
    ""games"": [{""seed"": 7920, ""outcome"": ""Won"", ""endUs"": 61250000, ""finalSignature"": ""sha256:dd""}], ""wins"": 1},
  ""owner"": [{""source"": ""results.json"", ""attempt"": ""a1"", ""commands"": [{""type"": ""tap"", ""pod"": 1, ""atUs"": 787200}],
    ""outcome"": ""won"", ""endUs"": 90514000, ""finalSignature"": ""sha256:ee""}],
  ""extra"": true
}";

            RoundConformanceFixture fixture = RoundJson.ReadFixture(json);

            Assert.Multiple(() =>
            {
                Assert.That(fixture.Number, Is.EqualTo(1));
                Assert.That(fixture.Windows.Single().Command, Is.EqualTo(new RoundCommand(RoundCommandType.Tap, 0, 0)));
                Assert.That(fixture.SolutionEndUs, Is.EqualTo(6_600_000));
                Assert.That(fixture.Games.Single().Seed, Is.EqualTo(7920));
                Assert.That(fixture.RandomWins, Is.EqualTo(1));
                Assert.That(fixture.Owner.Single().Commands.Single().AtUs, Is.EqualTo(787_200));
                Assert.That(fixture.Document["extra"]!.Value<bool>(), Is.True);
            });
        }

        [Test]
        public void Packs_ReadBackWhatWasPublished()
        {
            RoundLevel level = RoundJson.ReadLevel(LevelSample);
            RoundPicture picture = RoundJson.ReadPicture(PictureSample);

            RoundContentManifest manifest = RoundPacks.Build(3, new[] { level }, new[] { picture }, out IReadOnlyList<RoundPackFile> files);
            string manifestJson = manifest.Write();
            RoundContentManifest read = RoundContentManifest.Read(manifestJson);
            RoundPackFile levelPack = files.Single(f => f.Entry.Kind == RoundPackKind.Levels);
            RoundPackFile picturePack = files.Single(f => f.Entry.Kind == RoundPackKind.Pictures);

            Assert.Multiple(() =>
            {
                Assert.That(ContractSchemas.IsValidRoundManifest(manifestJson), Is.True);
                Assert.That(read.Write(), Is.EqualTo(manifestJson));
                Assert.That(read.ContentVersion, Is.EqualTo(3));
                Assert.That(read.Levels.Single().PictureId, Is.EqualTo("P9001"));
                Assert.That(levelPack.Entry.Path, Is.EqualTo("levels/round_levels_0001_0001.jsonl.gz"));
                Assert.That(picturePack.Entry.Path, Is.EqualTo("pictures/round_pictures.jsonl.gz"));
                Assert.That(RoundJson.WriteLevel(RoundPacks.ReadLevels(read.Packs[0], levelPack.Bytes).Single()), Is.EqualTo(LevelSample));
                Assert.That(RoundJson.WritePicture(RoundPacks.ReadPictures(read.Packs[1], picturePack.Bytes).Single()), Is.EqualTo(PictureSample));
            });
        }

        [Test]
        public void Packs_AreTheSameBytesOnEveryRun()
        {
            RoundLevel level = RoundJson.ReadLevel(LevelSample);
            RoundPicture picture = RoundJson.ReadPicture(PictureSample);

            RoundPacks.Build(1, new[] { level }, new[] { picture }, out IReadOnlyList<RoundPackFile> first);
            RoundPacks.Build(1, new[] { level }, new[] { picture }, out IReadOnlyList<RoundPackFile> second);

            Assert.That(first.Select(f => f.Entry.Sha256), Is.EqualTo(second.Select(f => f.Entry.Sha256)));
        }

        [Test]
        public void Packs_ATamperedPack_IsRefused()
        {
            RoundPacks.Build(1, new[] { RoundJson.ReadLevel(LevelSample) }, new[] { RoundJson.ReadPicture(PictureSample) }, out IReadOnlyList<RoundPackFile> files);
            byte[] bytes = (byte[])files[0].Bytes.Clone();
            bytes[bytes.Length - 1] ^= 1;

            Assert.Throws<ContentIntegrityException>(() => RoundPacks.ReadLevels(files[0].Entry, bytes));
        }

        [TestCase("round-level.schema.json")]
        [TestCase("round-picture.schema.json")]
        [TestCase("round-proof.schema.json")]
        [TestCase("round-content-manifest.schema.json")]
        public void TheEmbeddedRoundSchemas_AreTheContractsUnchanged(string file)
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
            {
                dir = dir.Parent;
            }

            string contract = File.ReadAllText(Path.Combine(dir!.FullName, "specs", "006-round-levels-in-game", "contracts", file));

            Assert.That(SchemaResources.Read(file), Is.EqualTo(contract));
        }
    }
}
