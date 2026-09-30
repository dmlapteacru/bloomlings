using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>The asset slot registry behind the asset inventory (spec 002 FR-029, FR-030, SC-003; contracts/asset-slots.md).</summary>
    public class AssetSlotTests
    {
        private static string Root => Path.Combine(Application.dataPath, "Bloomlings");

        [Test]
        public void Ids_AreUnique_AndMatchTheirCategoryPrefix()
        {
            Assert.That(AssetSlots.All.Select(s => s.Id).Distinct().Count(), Is.EqualTo(AssetSlots.All.Count));
            foreach (AssetSlot slot in AssetSlots.All)
            {
                Assert.That(AssetSlots.CategoryOf(slot.Id), Is.EqualTo(slot.Category), slot.Id);
                Assert.That(Regex.IsMatch(slot.Id, "^[a-z]+(\\.[a-z0-9_]+)+$"), Is.True, slot.Id);
                Assert.That(slot.Title, Is.Not.Empty, slot.Id);
                Assert.That(slot.UsedIn, Is.Not.Empty, slot.Id);
                Assert.That(slot.Frames.All(f => f >= 1 && f <= 17), Is.True, slot.Id);
            }
        }

        [Test]
        public void EveryCategoryOfFr029_HasEntries()
        {
            foreach (AssetCategory category in System.Enum.GetValues(typeof(AssetCategory)).Cast<AssetCategory>())
            {
                Assert.That(AssetSlots.All.Any(s => s.Category == category), Is.True, category.ToString());
            }
        }

        [Test]
        public void EveryShape_IsARegisteredShapeSlot()
        {
            foreach (string id in ShapeLibrary.Ids)
            {
                AssetSlot? slot = AssetSlots.Find(id);
                Assert.That(slot, Is.Not.Null, id);
                Assert.That(slot!.Kind, Is.EqualTo(PlaceholderKind.Shape), id);
            }

            foreach (AssetSlot slot in AssetSlots.All.Where(s => s.Kind == PlaceholderKind.Shape))
            {
                Assert.That(ShapeLibrary.Has(slot.Id), Is.True, slot.Id + " claims a shape placeholder but has none");
            }
        }

        [Test]
        public void SymbolsPodsSlotsAndTiles_CarryTheReadabilityDuty()
        {
            foreach (AssetSlot slot in AssetSlots.All.Where(s => s.Category == AssetCategory.VariantSymbol || s.Category == AssetCategory.PodSlot || s.Category == AssetCategory.BoardTile))
            {
                Assert.That(slot.Readability, Is.True, slot.Id);
            }
        }

        [Test]
        public void EverySoundCue_HasAnAudioSlot()
        {
            foreach (SoundCue cue in System.Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>())
            {
                Assert.That(AssetSlots.Has("audio.cue." + cue.ToString().ToLowerInvariant()), Is.True, cue.ToString());
            }
        }

        [Test]
        public void EveryShapeIdReferencedByTheClient_IsRegistered()
        {
            // Shape ids named in client code: ProceduralSprites.Shape("…") and the painters' ids.
            var used = new Regex(@"Shape\(""([a-z_.0-9]+)""[,)]");
            var unknown = new List<string>();
            foreach (string file in Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains("Tests")))
            {
                foreach (Match match in used.Matches(File.ReadAllText(file)))
                {
                    if (!ShapeLibrary.Has(match.Groups[1].Value))
                    {
                        unknown.Add(Path.GetFileName(file) + ": " + match.Groups[1].Value);
                    }
                }
            }

            Assert.That(unknown, Is.Empty);
        }
    }
}
