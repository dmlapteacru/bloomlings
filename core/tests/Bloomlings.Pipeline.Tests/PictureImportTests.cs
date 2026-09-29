using System;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Pipeline.Pictures;
using Bloomlings.Pipeline.Review;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests
{
    /// <summary>T072–T074: PNG and text grids, import with structure metrics, and picture validation.</summary>
    public class PictureImportTests
    {
        private string _folder = string.Empty;

        [SetUp]
        public void SetUp() => _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "bloomlings-pic-" + Guid.NewGuid().ToString("N"))).FullName;

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, recursive: true);

        [TestCase((byte)1)]
        [TestCase((byte)2)]
        [TestCase((byte)4)]
        [TestCase((byte)8)]
        public void IndexedPng_RoundTripsAtEveryBitDepth(byte bitDepth)
        {
            int max = (1 << bitDepth) - 1;
            int[][] rows = Enumerable.Range(0, 9).Select(y => Enumerable.Range(0, 11).Select(x => (x + y) % (max + 1)).ToArray()).ToArray();
            var palette = Enumerable.Range(0, max + 1).Select(i => ((byte)i, (byte)(255 - i), (byte)0)).ToArray();
            var image = new IndexedImage(11, 9, rows, palette);

            IndexedImage read = IndexedPngReader.Read(PngWriter.WriteIndexed(image, bitDepth));

            Assert.That(read.Width, Is.EqualTo(11));
            Assert.That(read.Height, Is.EqualTo(9));
            Assert.That(read.Indexes, Is.EqualTo(rows));
            Assert.That(read.Palette.Count, Is.EqualTo(max + 1));
        }

        [Test]
        public void CorruptPng_IsRejected()
        {
            byte[] png = PngWriter.WriteIndexed(new IndexedImage(2, 2, new[] { new[] { 0, 1 }, new[] { 1, 0 } }, new[] { ((byte)0, (byte)0, (byte)0), ((byte)9, (byte)9, (byte)9) }));
            png[png.Length - 20] ^= 0xFF;

            Assert.Throws<InvalidDataException>(() => IndexedPngReader.Read(png));
        }

        [Test]
        public void TextGrid_MapsTheLegend()
        {
            string?[][] cells = TextGridReader.Read("; a comment\nab.\n#ba\n", new System.Collections.Generic.Dictionary<char, string> { ['a'] = "sky", ['b'] = "leaf" });

            Assert.That(cells[0], Is.EqualTo(new[] { "sky", "leaf", null }));
            Assert.That(cells[1], Is.EqualTo(new[] { "#", "leaf", "sky" }));
            Assert.Throws<InvalidDataException>(() => TextGridReader.Read("ab\nabc", new System.Collections.Generic.Dictionary<char, string> { ['a'] = "x", ['b'] = "y", ['c'] = "z" }));
        }

        [Test]
        public void Import_TextGrid_WritesGridAndStructure()
        {
            WriteMeta("pond", "\"legend\": {\"s\": \"sky\", \"l\": \"lily\", \"w\": \"water\"},");
            File.WriteAllText(Path.Combine(_folder, "pond.grid.txt"), string.Join("\n",
                ".ssssss.",
                "ssssssss",
                "sswwwwss",
                "swwllwws",
                "swwllwws",
                "sswwwwss",
                "ssssssss",
                ".ssssss."));

            var results = PictureImporter.ImportFolder(_folder, Path.Combine(_folder, "lib"));

            Assert.That(results.Single().Error, Is.Null);
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(results.Single().OutputPath!));
            Assert.That(picture.Width, Is.EqualTo(8));
            Assert.That(picture.Height, Is.EqualTo(8));
            Assert.That(picture.CellAt(0, 0), Is.EqualTo(BasePicture.Empty), "The bottom row comes first in the grid.");
            Assert.That(picture.CellAt(3, 3), Is.EqualTo(1), "lily");
            Assert.That(picture.Structure, Is.EqualTo(new PictureStructure(3, 3, 625)), "sky ring, water ring, lily core; 40 of 64 cells are sky");
        }

        [Test]
        public void Import_Png_MapsEmptyAndStonePaletteIndexes()
        {
            WriteMeta("stones", string.Empty);
            int[][] rows = Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x => x == 0 ? 3 : x == 7 ? 4 : (y < 4 ? 0 : 2)).ToArray()).ToArray();
            var palette = Enumerable.Range(0, 5).Select(i => ((byte)(i * 40), (byte)0, (byte)0)).ToArray();
            File.WriteAllBytes(Path.Combine(_folder, "stones.png"), PngWriter.WriteIndexed(new IndexedImage(8, 8, rows, palette)));

            BasePicture picture = PictureImporter.Import(Path.Combine(_folder, "stones.meta.json"));

            Assert.That(picture.CellAt(0, 0), Is.EqualTo(BasePicture.Empty), "index = role count (3) is EMPTY");
            Assert.That(picture.CellAt(7, 0), Is.EqualTo(BasePicture.Stone), "index = role count + 1 is STONE");
            Assert.That(picture.CellAt(3, 7), Is.EqualTo(0), "the top row of the PNG is the top row of the board");
        }

        [Test]
        public void Validator_FlagsDraftsAndSchemaErrors()
        {
            WriteMeta("pond", "\"legend\": {\"s\": \"sky\", \"l\": \"lily\", \"w\": \"water\"},", status: "draft");
            File.WriteAllText(Path.Combine(_folder, "pond.grid.txt"), string.Join("\n", Enumerable.Repeat("sswwllss", 8)));
            string lib = Path.Combine(_folder, "lib");
            PictureImporter.ImportFolder(_folder, lib);
            File.WriteAllText(Path.Combine(lib, "broken.json"), "{\"id\": \"broken\"}");

            var reports = PictureValidator.ValidateFolder(lib).ToDictionary(r => Path.GetFileNameWithoutExtension(r.File));

            Assert.That(reports["pond"].Errors, Is.Empty);
            Assert.That(reports["pond"].Usable, Is.False, "draft pictures are unusable until approved");
            Assert.That(reports["broken"].Errors, Is.Not.Empty);
        }

        private void WriteMeta(string id, string extra, string status = "approved")
        {
            File.WriteAllText(Path.Combine(_folder, id + ".meta.json"), "{" + extra + $@"
  ""id"": ""{id}"", ""version"": 1, ""subject"": ""Test {id}"",
  ""roles"": [
    {{ ""roleId"": ""sky"", ""name"": ""Sky"", ""colorGroup"": ""blue_cyan"", ""isBackground"": true }},
    {{ ""roleId"": ""lily"", ""name"": ""Lily"", ""colorGroup"": ""pink_purple"" }},
    {{ ""roleId"": ""water"", ""name"": ""Water"", ""colorGroup"": ""blue_cyan"" }}
  ],
  ""finishedLook"": {{ ""mode"": ""auto"" }},
  ""tags"": {{ ""themes"": [""pond""] }},
  ""review"": {{ ""status"": ""{status}"" }},
  ""source"": {{ ""kind"": ""hand"", ""licence"": ""owned"" }}
}}");
        }
    }
}
