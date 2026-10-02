using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Localization;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>Every player-facing string is table-driven (R18, T148).</summary>
    public class LocalizationTests
    {
        private static string Root => Path.Combine(Application.dataPath, "Bloomlings");

        private static Dictionary<string, string> English =>
            Loc.ParseCsv(File.ReadAllText(Path.Combine(Root, "UI", "Localization", "Resources", Loc.EnglishResource + ".csv")));

        private static IEnumerable<string> GameSources() =>
            new[] { "App", "Gameplay", "Meta", "Services", "UI" }
                .SelectMany(folder => Directory.GetFiles(Path.Combine(Root, folder), "*.cs", SearchOption.AllDirectories));

        /// <summary>The playtest's screens, which read the same table through <c>PlaytestText</c> (when the repository has them).</summary>
        private static IEnumerable<string> PlaytestSources() =>
            new[] { "design", "android" }
                .Select(folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "playtest", folder)))
                .Where(Directory.Exists)
                .SelectMany(folder => Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly));

        /// <summary>The keys a source asks for: <c>Loc.T("key")</c>, and both keys of <c>Loc.T(cond ? "a" : "b")</c>.</summary>
        private static IEnumerable<string> KeysUsedIn(string text)
        {
            var plain = new Regex(@"(?:Loc|PlaytestText)\.[TF]\(""([a-z0-9_.]+)""[,)]");
            var either = new Regex(@"(?:Loc|PlaytestText)\.[TF]\([^()""]*\?\s*""([a-z0-9_.]+)""\s*:\s*""([a-z0-9_.]+)""\s*[,)]");
            return plain.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value)
                .Concat(either.Matches(text).Cast<Match>().SelectMany(m => new[] { m.Groups[1].Value, m.Groups[2].Value }));
        }

        [Test]
        public void EveryKeyUsedInCode_IsInTheEnglishTable()
        {
            Dictionary<string, string> english = English;
            var missing = new List<string>();
            foreach (string file in GameSources().Concat(PlaytestSources()))
            {
                foreach (string key in KeysUsedIn(File.ReadAllText(file)))
                {
                    if (!english.ContainsKey(key))
                    {
                        missing.Add(Path.GetFileName(file) + ": " + key);
                    }
                }
            }

            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void KeysUsedThroughAChoice_AreFound()
        {
            List<string> keys = KeysUsedIn(@"Loc.T(stuck ? ""jam.stuck"" : ""jam.title""); PlaytestText.F(""common.level"", 3);").ToList();
            Assert.That(keys, Is.EquivalentTo(new[] { "jam.stuck", "jam.title", "common.level" }));
        }

        [Test]
        public void KeysBuiltFromAFamily_AreInTheEnglishTable()
        {
            // The Wardrobe and the Store's cosmetics build these keys from WardrobeService.FamilyKey, and the Wardrobe's
            // Profile tab from "profile".
            Dictionary<string, string> english = English;
            var missing = new List<string>();
            foreach (string key in WardrobeService.Families.Select(WardrobeService.FamilyKey))
            {
                missing.AddRange(new[] { "family." + key, "wardrobe.role." + key, "wardrobe.about." + key }.Where(k => !english.ContainsKey(k)));
            }

            missing.AddRange(new[] { "wardrobe.role.profile", "wardrobe.about.profile", "wardrobe.tab_profile" }.Where(k => !english.ContainsKey(k)));
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void NoPlayerFacingLiteral_IsLeftInTheUi()
        {
            var patterns = new[]
            {
                new Regex(@"Create(Text|Button)\(""[^""]*"", [^,]+, \$?""[^""]*[A-Za-z][^""]*"""),
                new Regex(@"UiKit\.(Label|PrimaryButton|SecondaryButton|DarkPill|Badge|Card|Sheet)\(""[^""]*"", [^,]+, \$?""[^""]*[A-Za-z][^""]*"""),
                new Regex(@"new DemoStep\(\$?"""),
                new Regex(@"(Toast|SetTitle)\(\$?""[^""]*[A-Za-z]"),
                new Regex(@"StartTargeting\([^,]+, \$?""[^""]*[A-Za-z]"),
                new Regex(@"\.text \+?= \$?""[^""]*[A-Za-z]"),
            };
            // In the UI and meta screens, also words glued onto values (" Petals") and interpolated text.
            var screenPatterns = new[]
            {
                new Regex(@"""\s+[A-Za-z]{2,}[^""]*"""),
                new Regex(@"\$""[^""]*[A-Za-z]{3,}"),
            };
            var found = new List<string>();
            foreach (string file in GameSources())
            {
                bool screen = file.Replace('\\', '/').Contains("/UI/") || file.Replace('\\', '/').Contains("/Meta/");
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    // Escapes such as \n are not words.
                    string line = Regex.Replace(lines[i], @"\\.", " ");
                    if (patterns.Any(p => p.IsMatch(line)) || (screen && !DeveloperText(line) && screenPatterns.Any(p => p.IsMatch(line))))
                    {
                        found.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }

            Assert.That(found, Is.Empty);
        }

        /// <summary>Comments, exceptions and validation messages are for developers, not players.</summary>
        private static bool DeveloperText(string line) =>
            line.Contains("///") || line.Contains("throw ") || line.Contains("Exception(") || line.Contains("problems.Add(");

        [Test]
        public void EnglishTable_IsWellFormed_AndCoversTheCosmetics()
        {
            Dictionary<string, string> english = English;
            Assert.That(english.Values.All(v => v.Length > 0), Is.True);
            Assert.That(english["daily.body"], Does.Contain("{0}").And.Contain(","), "quoted commas survive");

            CosmeticCatalog catalog = CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Root, "Meta", "Wardrobe", "Resources", "CosmeticCatalog.json")));
            foreach (CosmeticItem item in catalog.Items)
            {
                Assert.That(english.ContainsKey("cosmetic." + item.Id), Is.True, item.Id);
            }
        }

        [Test]
        public void Lookup_PrefersTheLocaleTable_ThenEnglish_ThenTheKey()
        {
            Loc.LoadEnglish("key,en\ncommon.close,Close\ncommon.level,Level {0}\n");
            try
            {
                Assert.That(Loc.T("common.close"), Is.EqualTo("Close"));
                Assert.That(Loc.F("common.level", 12), Is.EqualTo("Level 12"));
                Assert.That(Loc.T("missing.key"), Is.EqualTo("missing.key"));
                Assert.That(Loc.T("cosmetic.hat.x", "Hat X"), Is.EqualTo("Hat X"));

                Loc.Lookup = key => key == "common.close" ? "Fermer" : null;
                Assert.That(Loc.T("common.close"), Is.EqualTo("Fermer"));
                Assert.That(Loc.F("common.level", 3), Is.EqualTo("Level 3"), "a key missing from the locale falls back to English");
            }
            finally
            {
                Loc.Lookup = null;
                Loc.LoadEnglish(File.ReadAllText(Path.Combine(Root, "UI", "Localization", "Resources", Loc.EnglishResource + ".csv")));
            }
        }
    }
}
