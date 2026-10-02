using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Variants;
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

        [Test]
        public void EveryKeyUsedInCode_IsInTheEnglishTable()
        {
            Dictionary<string, string> english = English;
            var used = new Regex(@"Loc\.[TF]\(""([a-z0-9_.]+)""[,)]");
            var missing = new List<string>();
            foreach (string file in GameSources())
            {
                foreach (Match match in used.Matches(File.ReadAllText(file)))
                {
                    if (!english.ContainsKey(match.Groups[1].Value))
                    {
                        missing.Add(Path.GetFileName(file) + ": " + match.Groups[1].Value);
                    }
                }
            }

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

        /// <summary>
        /// The Wardrobe and the Store build these keys from <see cref="WardrobeService.FamilyKey"/>, so the key check above
        /// cannot see them: every family has its name, its role and its line, and the profile tab its role and line.
        /// </summary>
        [Test]
        public void EnglishTable_NamesEveryFamily_AndTheProfile()
        {
            Dictionary<string, string> english = English;
            var keys = new List<string> { "wardrobe.role.profile", "wardrobe.about.profile" };
            foreach (Family family in (Family[])System.Enum.GetValues(typeof(Family)))
            {
                string key = WardrobeService.FamilyKey(family);
                keys.Add("family." + key);
                keys.Add("wardrobe.role." + key);
                keys.Add("wardrobe.about." + key);
            }

            Assert.That(keys.Where(k => !english.ContainsKey(k)), Is.Empty);
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
