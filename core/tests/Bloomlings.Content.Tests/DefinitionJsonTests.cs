using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Content.Tests
{
    public class DefinitionJsonTests
    {
        private static string LevelSample => ContractSchemas.Sample("level-sample.json");

        private static string PictureSample => ContractSchemas.Sample("picture-sample.json");

        [Test]
        public void Samples_AreValidAgainstTheContractSchemas()
        {
            Assert.That(ContractSchemas.IsValidLevel(LevelSample), Is.True);
            Assert.That(ContractSchemas.IsValidPicture(PictureSample), Is.True);
        }

        [Test]
        public void LevelSample_RoundTripsByteForByte()
        {
            string written = DefinitionJson.Write(DefinitionJson.Read(LevelSample));

            Assert.That(written, Is.EqualTo(LevelSample));
        }

        [Test]
        public void LevelSample_CompactFormRoundTripsToTheSameDocument()
        {
            string compact = DefinitionJson.Write(DefinitionJson.Read(LevelSample), indented: false);

            Assert.That(compact, Does.Not.Contain("\n"));
            Assert.That(DefinitionJson.Write(DefinitionJson.Read(compact), indented: false), Is.EqualTo(compact));
            Assert.That(DefinitionJson.Write(DefinitionJson.Read(compact)), Is.EqualTo(LevelSample));
        }

        [Test]
        public void LevelSample_ReadsEveryField()
        {
            LevelDefinition level = DefinitionJson.Read(LevelSample);

            Assert.Multiple(() =>
            {
                Assert.That(level.LevelNumber, Is.EqualTo(12));
                Assert.That(level.DefinitionVersion, Is.EqualTo(2));
                Assert.That(level.Seed, Is.EqualTo(ulong.MaxValue));
                Assert.That(level.Picture, Is.EqualTo(new PictureRef("sample_tulip", 1, Mirror.Horizontal, "soft_sky")));
                Assert.That(level.Mapping["leaf"], Is.EqualTo(VariantId.Moss));
                Assert.That(level.Mapping["stem"], Is.EqualTo(VariantId.Leaf));
                Assert.That(level.Entries[1], Is.EqualTo(new EntryDef(new CellPos(0, 4), EntrySide.Left)));
                Assert.That(level.Overlays[0].LayersBelow, Is.EqualTo(new[] { VariantId.Dew, VariantId.Water }));
                Assert.That(level.Overlays[1].KeyId, Is.EqualTo("k1"));
                Assert.That(level.Overlays[1].Mystery, Is.True);
                Assert.That(level.Overlays[2].Stone, Is.True);
                Assert.That(level.Overlays[3].Hole, Is.True);
                Assert.That(level.Specials[0].Condition.Kind, Is.EqualTo(SpecialConditionKind.ClearCountAdjacent));
                Assert.That(level.Specials[0].Condition.Variant, Is.EqualTo(VariantId.Water));
                Assert.That(level.Specials[0].Condition.Count, Is.EqualTo(3));
                Assert.That(level.Specials[0].Effect.Cells, Is.EqualTo(new[] { new CellPos(5, 6) }));
                Assert.That(level.Locks[1], Is.EqualTo(new LockDef("k2", LockTargetKind.Special, "gate1")));
                Assert.That(level.Slots.Count, Is.EqualTo(5));
                Assert.That(level.Slots.Locked, Is.EqualTo(new LockedSlotDef(4, "k2")));
                Assert.That(level.Tray.Stacks.Select(s => s.ToArray()), Is.EqualTo(new[] { new[] { "p1", "p2" }, new[] { "p3", "p4" } }));
                Assert.That(level.Pods[1].ConnectedGroupId, Is.EqualTo("g1"));
                Assert.That(level.Pods[3], Is.EqualTo(new PodDef("p4", VariantId.Flower, 4, true, "k1", null)));
                Assert.That(level.Difficulty, Is.EqualTo(new DifficultyDef(DifficultyClass.Hard, 1250, true)));
                Assert.That(level.Mechanics, Is.EqualTo(new[] { "keys", "layers", "gate" }));
                Assert.That(level.BoardLook, Is.EqualTo(BoardLook.Peek));
            });
        }

        [Test]
        public void ExplicitDefaults_AreNormalizedAway()
        {
            JObject doc = MinimalLevel();
            doc["picture"]!["mirror"] = "none";
            doc["picture"]!["backgroundTreatment"] = "default";
            doc["difficulty"]!["overridden"] = false;
            doc["mechanics"] = new JArray();
            doc["pods"]![0]!["mystery"] = false;

            string written = DefinitionJson.Write(DefinitionJson.Read(doc.ToString()));

            Assert.That(written, Is.EqualTo(DefinitionJson.Write(DefinitionJson.Read(MinimalLevel().ToString()))));
            Assert.That(written, Does.Not.Contain("mirror").And.Not.Contain("overridden").And.Not.Contain("mechanics"));
        }

        [Test]
        public void MinimalLevel_IsSchemaValidAndRoundTrips()
        {
            string canonical = DefinitionJson.Write(DefinitionJson.Read(MinimalLevel().ToString()));

            Assert.That(ContractSchemas.IsValidLevel(canonical), Is.True);
            Assert.That(DefinitionJson.Write(DefinitionJson.Read(canonical)), Is.EqualTo(canonical));
        }

        /// <summary>
        /// Invalid documents with the path the reader reports. All of them violate the contract schema too, except the
        /// ones listed in <see cref="ReaderOnly"/>, which break constraints a JSON Schema cannot express.
        /// </summary>
        private static readonly (string Name, Action<JObject> Break, string Path)[] InvalidLevels =
        {
            ("unknown property", d => d["colour"] = "red", "colour"),
            ("missing seed", d => d.Remove("seed"), "seed"),
            ("seed is a number", d => d["seed"] = 42, "seed"),
            ("seed above uint64", d => d["seed"] = "18446744073709551616", "seed"),
            ("seed with sign", d => d["seed"] = "-1", "seed"),
            ("level 0", d => d["levelNumber"] = 0, "levelNumber"),
            ("four slots", d => d["slots"]!["count"] = 4, "slots.count"),
            ("locked slot index 5", d => d["slots"]!["locked"] = new JObject { ["slotIndex"] = 5, ["keyId"] = "k" }, "slots.locked.slotIndex"),
            ("one stack", d => d["tray"]!["stacks"] = new JArray { new JArray("a", "b") }, "tray.stacks"),
            ("seven stacks", d => d["tray"]!["stacks"] = new JArray(Enumerable.Range(0, 7).Select(i => new JArray("a" + i))), "tray.stacks"),
            ("empty stack", d => d["tray"]!["stacks"]![1] = new JArray(), "tray.stacks[1]"),
            ("unknown variant", d => d["pods"]![0]!["variantId"] = "rose", "pods[0].variantId"),
            ("family used as variant", d => d["pods"]![0]!["variantId"] = "sprig", "pods[0].variantId"),
            ("pod count 0", d => d["pods"]![0]!["count"] = 0, "pods[0].count"),
            ("one pod", d => ((JArray)d["pods"]!).RemoveAt(1), "pods"),
            ("mapping with one role", d => d["mapping"] = new JObject { ["bg"] = "water" }, "mapping"),
            ("no entries", d => d["entries"] = new JArray(), "entries"),
            ("entry x out of range", d => d["entries"]![0]!["x"] = 22, "entries[0].x"),
            ("entry y out of range", d => d["entries"]![0]!["y"] = 28, "entries[0].y"),
            ("unknown board look", d => d["boardLook"] = "tiles", "boardLook"),
            ("entry side unknown", d => d["entries"]![0]!["side"] = "middle", "entries[0].side"),
            ("fractional score", d => d["difficulty"]!["score"] = 1.5m, "difficulty.score"),
            ("unknown difficulty class", d => d["difficulty"]!["class"] = "extreme", "difficulty.class"),
            ("four hidden layers", d => d["overlays"] = new JObject { ["cells"] = new JArray(new JObject { ["x"] = 1, ["y"] = 1, ["layersBelow"] = new JArray("dew", "dew", "dew", "dew") }) }, "overlays.cells[0].layersBelow"),
            ("duplicate mechanic", d => d["mechanics"] = new JArray("keys", "keys"), "mechanics[1]"),
            ("picture id with capitals", d => d["picture"]!["id"] = "Tulip", "picture.id"),
            ("special without cells", d => d["specials"] = new JArray(new JObject { ["id"] = "s", ["type"] = "gate", ["cells"] = new JArray(), ["condition"] = new JObject { ["kind"] = "key" }, ["effect"] = new JObject { ["kind"] = "open_cells" } }), "specials[0].cells"),
            ("lock target unknown kind", d => d["locks"] = new JArray(new JObject { ["keyId"] = "k", ["target"] = new JObject { ["kind"] = "tile", ["id"] = "x" } }), "locks[0].target.kind"),
        };

        private static readonly string[] ReaderOnly = { "seed above uint64" };

        private static string[] InvalidLevelNames => InvalidLevels.Select(c => c.Name).ToArray();

        [TestCaseSource(nameof(InvalidLevelNames))]
        public void InvalidLevel_IsRejectedByTheReaderAndTheSchema(string name)
        {
            (string _, Action<JObject> breakIt, string path) = InvalidLevels.Single(c => c.Name == name);
            JObject doc = MinimalLevel();
            breakIt(doc);
            string json = doc.ToString();

            var ex = Assert.Throws<ContentFormatException>(() => DefinitionJson.Read(json));
            Assert.That(ex!.Path, Is.EqualTo(path));
            if (!ReaderOnly.Contains(name))
            {
                Assert.That(ContractSchemas.IsValidLevel(json), Is.False, "The contract schema must reject it too.");
            }
        }

        /// <summary>
        /// FR-036 as amended on 2026-10-06: <c>boardLook</c> is optional (older content leaves it out and peeks), and a
        /// stated look is kept as written, <c>"peek"</c> included, because the generator always writes it.
        /// </summary>
        [TestCase(null)]
        [TestCase("peek")]
        [TestCase("icons")]
        public void BoardLook_IsOptional_AndKeptAsWritten(string? look)
        {
            JObject doc = MinimalLevel();
            if (look != null)
            {
                doc["boardLook"] = look;
            }

            LevelDefinition level = DefinitionJson.Read(doc.ToString());
            string canonical = DefinitionJson.Write(level);

            Assert.That(level.BoardLook, Is.EqualTo(look == null ? (BoardLook?)null : look == "icons" ? BoardLook.Icons : BoardLook.Peek));
            Assert.That(BoardLooks.Of(level), Is.EqualTo(look == "icons" ? BoardLook.Icons : BoardLook.Peek));
            Assert.That(canonical.Contains("\"boardLook\""), Is.EqualTo(look != null));
            Assert.That(DefinitionJson.Write(DefinitionJson.Read(canonical)), Is.EqualTo(canonical));
            Assert.That(DefinitionJson.Write(DefinitionJson.Read(canonical), indented: false), Does.Not.Contain("\n"));
            Assert.That(ContractSchemas.IsValidLevel(canonical), Is.True);
        }

        [Test]
        public void TheBiggestBoard_HasCoordinatesUpTo21By27()
        {
            JObject doc = MinimalLevel();
            doc["entries"]![0]!["x"] = 21;
            doc["entries"]![0]!["y"] = 27;
            doc["entries"]![0]!["side"] = "top";

            LevelDefinition level = DefinitionJson.Read(doc.ToString());

            Assert.That(level.Entries[0].Cell, Is.EqualTo(new CellPos(21, 27)));
            Assert.That(ContractSchemas.IsValidLevel(doc.ToString()), Is.True);
            Assert.That(CellPos.MaxWidth * CellPos.MaxHeight, Is.EqualTo(616), "22 × 28 (FR-008 as amended on 2026-10-06)");
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new CellPos(22, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new CellPos(0, 28));
        }

        [Test]
        public void ThePictureSchema_AllowsBoardsUpTo22By28()
        {
            string wide = PictureSample.Replace("\"width\": 7", "\"width\": 22");
            string tooWide = PictureSample.Replace("\"width\": 7", "\"width\": 23");
            string tall = PictureSample.Replace("\"height\": 8", "\"height\": 28");
            string tooTall = PictureSample.Replace("\"height\": 8", "\"height\": 29");

            // The grid no longer matches, which only the reader checks; the schema sees the size limits.
            Assert.That(ContractSchemas.IsValidPicture(wide), Is.True);
            Assert.That(ContractSchemas.IsValidPicture(tall), Is.True);
            Assert.That(ContractSchemas.IsValidPicture(tooWide), Is.False);
            Assert.That(ContractSchemas.IsValidPicture(tooTall), Is.False);
            Assert.That(BasePicture.MaxWidth, Is.EqualTo(22));
            Assert.That(BasePicture.MaxHeight, Is.EqualTo(28));
        }

        [TestCase("level-definition.schema.json")]
        [TestCase("base-picture.schema.json")]
        [TestCase("content-manifest.schema.json")]
        [TestCase("player-save.schema.json")]
        public void TheEmbeddedSchemas_AreTheContractsUnchanged(string file)
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
            {
                dir = dir.Parent;
            }

            string contract = File.ReadAllText(Path.Combine(dir!.FullName, "specs", "001-core-game-mvp", "contracts", file));

            Assert.That(Bloomlings.Content.Schemas.SchemaResources.Read(file), Is.EqualTo(contract));
        }

        [Test]
        public void DuplicateKeys_AreRejected()
        {
            string json = LevelSample.Replace("\"levelNumber\": 12,", "\"levelNumber\": 12,\n  \"levelNumber\": 13,");

            Assert.Throws<ContentFormatException>(() => DefinitionJson.Read(json));
        }

        [Test]
        public void TrailingContent_IsRejected()
        {
            Assert.Throws<ContentFormatException>(() => DefinitionJson.Read(LevelSample + "{}"));
        }

        [Test]
        public void PictureSample_RoundTripsByteForByte()
        {
            string written = BasePictureJson.Write(BasePictureJson.Read(PictureSample));

            Assert.That(written, Is.EqualTo(PictureSample));
        }

        [Test]
        public void PictureSample_ReadsGridBottomRowFirst()
        {
            BasePicture picture = BasePictureJson.Read(PictureSample);

            Assert.Multiple(() =>
            {
                Assert.That(picture.Width, Is.EqualTo(7));
                Assert.That(picture.Height, Is.EqualTo(8));
                Assert.That(picture.CellAt(3, 0), Is.EqualTo(3), "Row 0 is the bottom row (the stem).");
                Assert.That(picture.CellAt(0, 7), Is.EqualTo(BasePicture.Empty));
                Assert.That(picture.CellAt(6, 7), Is.EqualTo(BasePicture.Stone));
                Assert.That(picture.Roles[0].IsBackground, Is.True);
                Assert.That(picture.Roles[1].ColorGroup, Is.EqualTo(ColorGroup.PinkPurple));
                Assert.That(picture.Structure, Is.EqualTo(new PictureStructure(5, 2, 482)));
                Assert.That(picture.Review.Status, Is.EqualTo(ReviewStatus.Approved));
                Assert.That(picture.FinishedLook, Is.EqualTo(new FinishedLook(FinishedLookMode.Illustration, "illus_sample_tulip")));
                Assert.That(picture.Tags.Themes, Is.EqualTo(new[] { "garden", "flowers" }));
            });
        }

        [Test]
        public void BackgroundShare_IsWrittenWithoutTrailingZeros()
        {
            string json = PictureSample.Replace("\"backgroundShare\": 0.482", "\"backgroundShare\": 0.500");

            string written = BasePictureJson.Write(BasePictureJson.Read(json));

            Assert.That(written, Does.Contain("\"backgroundShare\": 0.5,\n"));
        }

        [TestCase("\"width\": 7", "\"width\": 8", "grid[0]")]
        [TestCase("\"height\": 8", "\"height\": 9", "grid")]
        [TestCase("[0, 0, 0, 3, 0, 0, 0],\n    [0, 0, 0, 3", "[0, 0, 0, 4, 0, 0, 0],\n    [0, 0, 0, 3", "grid[0][3]")]
        [TestCase("[0, 0, 0, 3, 0, 0, 0],\n    [0, 0, 0, 3", "[0, 0, 0, -3, 0, 0, 0],\n    [0, 0, 0, 3", "grid[0][3]")]
        [TestCase("\"backgroundShare\": 0.482", "\"backgroundShare\": 0.4825", "structure.backgroundShare")]
        [TestCase("\"backgroundShare\": 0.482", "\"backgroundShare\": 1.2", "structure.backgroundShare")]
        [TestCase("\"date\": \"2026-09-29\"", "\"date\": \"29.09.2026\"", "review.date")]
        [TestCase("\"colorGroup\": \"green\",\n      \"name\": \"Leaf\"", "\"colorGroup\": \"teal\",\n      \"name\": \"Leaf\"", "roles[2].colorGroup")]
        [TestCase("\"roleId\": \"stem\"", "\"roleId\": \"leaf\"", "roles[3].roleId")]
        [TestCase("\"subject\": \"Tulip\"", "\"subject\": \"T\"", "subject")]
        [TestCase("\"version\": 1,\n  \"width\"", "\"version\": 1,\n  \"palette\": 3,\n  \"width\"", "palette")]
        public void InvalidPicture_IsRejected(string find, string replace, string path)
        {
            Assert.That(PictureSample, Does.Contain(find), "The test edit must apply.");
            string json = PictureSample.Replace(find, replace);

            var ex = Assert.Throws<ContentFormatException>(() => BasePictureJson.Read(json));
            Assert.That(ex!.Path, Is.EqualTo(path));
        }

        internal static JObject MinimalLevel() => JObject.Parse(@"{
  ""levelNumber"": 1,
  ""definitionVersion"": 1,
  ""seed"": ""42"",
  ""generatorVersion"": ""curated"",
  ""picture"": { ""id"": ""sample_tulip"", ""version"": 1 },
  ""mapping"": { ""bg"": ""water"", ""leaf"": ""leaf"", ""petal"": ""flower"", ""stem"": ""leaf"" },
  ""entries"": [ { ""x"": 3, ""y"": 0, ""side"": ""bottom"" } ],
  ""slots"": { ""count"": 5 },
  ""tray"": { ""stacks"": [ [ ""p1"" ], [ ""p2"" ] ] },
  ""pods"": [
    { ""id"": ""p1"", ""variantId"": ""leaf"", ""count"": 2 },
    { ""id"": ""p2"", ""variantId"": ""water"", ""count"": 3 }
  ],
  ""difficulty"": { ""class"": ""normal"", ""score"": 0 },
  ""rewardProfile"": ""standard""
}");
    }
}
