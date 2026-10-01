using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Localization;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The bundled Nunito files (spec 003 FR-009, SC-006, contracts/fonts.md): present with their license, and covering
    /// every character the game shows, read from each file's <c>cmap</c> table.
    /// </summary>
    public class GardenFontTests
    {
        private static string Folder => Path.Combine(Application.dataPath, "Bloomlings", "UI", "Fonts", "Resources");

        private static readonly string[] Files = { "Nunito-ExtraBold.ttf", "Nunito-SemiBold.ttf" };

        [Test]
        public void BothWeights_AndTheLicense_AreBundled()
        {
            foreach (string file in Files)
            {
                Assert.That(File.Exists(Path.Combine(Folder, file)), Is.True, file);
            }

            string license = File.ReadAllText(Path.Combine(Folder, "OFL.txt"));
            Assert.That(license, Does.Contain("SIL OPEN FONT LICENSE Version 1.1"));
            Assert.That(license, Does.Contain("The Nunito Project Authors"));
        }

        [Test]
        public void TheFonts_CoverLatinCyrillicAndTheGameSymbols()
        {
            var needed = new List<int>();
            for (int c = 0x20; c <= 0x7E; c++)
            {
                needed.Add(c);
            }

            for (int c = 0xA0; c <= 0xFF; c++)
            {
                needed.Add(c);
            }

            for (int c = 0x410; c <= 0x44F; c++)
            {
                needed.Add(c);
            }

            needed.AddRange(new[] { 0x401, 0x451, 0xD7, 0x2212 });
            foreach (string file in Files)
            {
                HashSet<int> covered = Cmap(File.ReadAllBytes(Path.Combine(Folder, file)));
                int[] missing = needed.Where(c => !covered.Contains(c)).ToArray();
                Assert.That(missing, Is.Empty, file + " misses " + string.Join(", ", missing.Select(c => "U+" + c.ToString("X4"))));
            }
        }

        [Test]
        public void EveryEnglishString_RendersInTheFont()
        {
            // SC-006: no missing glyphs. The ▶ of PLAY is a shape (ui.play), never a character.
            string csv = File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings", "UI", "Localization", "Resources", Loc.EnglishResource + ".csv"));
            var shown = new HashSet<int>();
            foreach (string value in Loc.ParseCsv(csv).Values)
            {
                foreach (char c in value)
                {
                    if (!char.IsControl(c))
                    {
                        shown.Add(c);
                    }
                }
            }

            foreach (string file in Files)
            {
                HashSet<int> covered = Cmap(File.ReadAllBytes(Path.Combine(Folder, file)));
                int[] missing = shown.Where(c => !covered.Contains(c)).ToArray();
                Assert.That(missing, Is.Empty, file + " misses " + string.Join(", ", missing.Select(c => "U+" + c.ToString("X4"))));
            }
        }

        /// <summary>The code points of a TrueType font's Unicode <c>cmap</c> subtables (formats 4 and 12).</summary>
        private static HashSet<int> Cmap(byte[] font)
        {
            int U16(int at) => (font[at] << 8) | font[at + 1];
            long U32(int at) => ((long)font[at] << 24) | ((long)font[at + 1] << 16) | ((long)font[at + 2] << 8) | font[at + 3];
            int tables = U16(4);
            int cmap = -1;
            for (int i = 0; i < tables; i++)
            {
                int record = 12 + (i * 16);
                if (font[record] == 'c' && font[record + 1] == 'm' && font[record + 2] == 'a' && font[record + 3] == 'p')
                {
                    cmap = (int)U32(record + 8);
                }
            }

            Assert.That(cmap, Is.GreaterThan(0), "no cmap table");
            var covered = new HashSet<int>();
            int subtables = U16(cmap + 2);
            for (int i = 0; i < subtables; i++)
            {
                int platform = U16(cmap + 4 + (i * 8));
                int at = cmap + (int)U32(cmap + 4 + (i * 8) + 4);
                if (platform != 0 && platform != 3)
                {
                    continue;
                }

                int format = U16(at);
                if (format == 4)
                {
                    int segments = U16(at + 6) / 2;
                    int ends = at + 14;
                    int starts = ends + (segments * 2) + 2;
                    int deltas = starts + (segments * 2);
                    int offsets = deltas + (segments * 2);
                    for (int s = 0; s < segments; s++)
                    {
                        int end = U16(ends + (s * 2));
                        int start = U16(starts + (s * 2));
                        int delta = U16(deltas + (s * 2));
                        int offset = U16(offsets + (s * 2));
                        for (int c = start; c <= end && c != 0xFFFF; c++)
                        {
                            int glyph = offset == 0 ? (c + delta) & 0xFFFF : U16(offsets + (s * 2) + offset + ((c - start) * 2));
                            if (glyph != 0)
                            {
                                covered.Add(c);
                            }
                        }
                    }
                }
                else if (format == 12)
                {
                    long groups = U32(at + 12);
                    for (int g = 0; g < groups; g++)
                    {
                        int group = at + 16 + (g * 12);
                        for (long c = U32(group); c <= U32(group + 4); c++)
                        {
                            covered.Add((int)c);
                        }
                    }
                }
            }

            return covered;
        }
    }
}
