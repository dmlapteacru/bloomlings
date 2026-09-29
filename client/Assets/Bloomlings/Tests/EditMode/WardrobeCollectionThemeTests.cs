using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>The Wardrobe (FR-063, T143), the Collection (FR-065, T145) and the theme rotation (FR-066, T146).</summary>
    public class WardrobeCollectionThemeTests
    {
        private static string CatalogPath => Path.Combine(Application.dataPath, "Bloomlings", "Meta", "Wardrobe", "Resources", "CosmeticCatalog.json");

        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(CatalogPath));

        [Test]
        public void Cosmetics_PassTheReadabilityCheck()
        {
            Assert.That(Catalog.ReadabilityProblems(), Is.Empty);
        }

        [Test]
        public void ReadabilityCheck_RejectsAWornVariantLikeColor()
        {
            var catalog = new CosmeticCatalog(new[]
            {
                new CosmeticItem("hat.red_cap", CosmeticKind.Hat, "Red Cap", "cap", "#D0302A", false),
                new CosmeticItem("badge.red", CosmeticKind.Badge, "Red Badge", "badge", "#D0302A", false),
            });

            Assert.That(catalog.ReadabilityProblems().Single(), Does.StartWith("hat.red_cap: worn tint"), "profile items are never drawn on the board");
        }

        [Test]
        public void EveryMilestoneItem_IsInTheCatalog()
        {
            CosmeticCatalog catalog = Catalog;
            foreach (string id in MilestoneTable.Default.Cadences.SelectMany(c => c.Items))
            {
                Assert.That(catalog.TryGet(id, out _), Is.True, id);
            }
        }

        [Test]
        public void Wardrobe_OneOwnedWornItemPerFamily_AndStarterItemsOnce()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            int saves = 0;
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => saves++);

            Assert.That(wardrobe.IsAvailable, Is.False, "locked before L40");
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            wardrobe.OnUnlock(WardrobeService.UnlockId);
            wardrobe.OnUnlock(WardrobeService.UnlockId);
            Assert.That(wardrobe.Owned.Select(i => i.Id), Is.EqualTo(new[] { "hat.sprout_cap" }), "starter items are given once");

            Assert.That(wardrobe.Equip(Family.Drop, "hat.leaf_cap"), Is.False, "not owned");
            save.Cosmetics.Owned.Add("hat.leaf_cap");
            save.Cosmetics.Owned.Add("frame.daisy");
            Assert.That(wardrobe.Equip(Family.Drop, "frame.daisy"), Is.False, "a frame is not worn by Bloomlings");
            Assert.That(wardrobe.Equip(Family.Drop, "hat.sprout_cap"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "hat.leaf_cap"), Is.True);
            Assert.That(wardrobe.EquippedFor(Family.Drop)!.Id, Is.EqualTo("hat.leaf_cap"), "one item per family");
            Assert.That(wardrobe.EquippedFor(Family.Sprig), Is.Null);
            Assert.That(save.Cosmetics.Equipped.Keys, Is.EqualTo(new[] { "drop" }));
            Assert.That(wardrobe.ProfileDecoration()!.Id, Is.EqualTo("frame.daisy"));
            Assert.That(SaveSerializer.Read(SaveSerializer.Write(save)).Cosmetics.Equipped["drop"], Is.EqualTo("hat.leaf_cap"), "the save schema accepts it");
            Assert.That(saves, Is.GreaterThan(0));
        }

        [Test]
        public void Collection_AddsEachWonLevelOnce()
        {
            ContentSet curated = DevContent.LoadCurated();
            LevelDefinition level = curated.GetLevel(curated.LevelNumbers[0]);
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            var collection = new CollectionService(save, () => { });

            Assert.That(collection.Add(level, 1), Is.True);
            Assert.That(collection.Add(level, 1), Is.False);
            CollectionEntry entry = collection.Entries.Single();
            Assert.That((entry.PictureId, entry.PictureVersion, entry.LevelNumber), Is.EqualTo((level.Picture.Id, level.Picture.Version, 1)));
            Assert.That(entry.MappingHash, Has.Length.EqualTo(16));
            Assert.That(CollectionService.MappingHash(level.Mapping.Reverse().ToDictionary(p => p.Key, p => p.Value)), Is.EqualTo(entry.MappingHash), "independent of key order");
        }

        [Test]
        public void Themes_RotateByBand_AndMirrorTheRoadmapFile()
        {
            ThemeRotation rotation = ThemeRotation.Default;
            Assert.That(new[] { 1, 99, 100, 149, 150, 200, 250, 300 }.Select(l => rotation.ThemeFor(l).Id), Is.EqualTo(new[]
            {
                "daylight_garden", "daylight_garden", "pond", "pond", "orchard", "moonlit_garden", "daylight_garden", "pond",
            }));

            ThemeRotation file = ThemeRotation.Parse(File.ReadAllText(Path.Combine(DevContent.RepositoryContentFolder, "roadmap", "themes.json")));
            Assert.That((file.StartLevel, file.BandLength), Is.EqualTo((rotation.StartLevel, rotation.BandLength)));
            Assert.That(file.Themes, Is.EqualTo(rotation.Themes));
            Assert.That(rotation.Themes.All(t => ThemeRotation.Luminance(t.Background) > 0.7), Is.True, "light backgrounds keep tiles readable");
        }
    }
}
