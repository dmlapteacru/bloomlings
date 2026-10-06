using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
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
        public void Wardrobe_OneItemOfEachKindPerFamily_AndStarterItemsOnce()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            int saves = 0;
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => saves++);

            Assert.That(wardrobe.IsAvailable, Is.False, "locked before L40");
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            wardrobe.OnUnlock(WardrobeService.UnlockId);
            wardrobe.OnUnlock(WardrobeService.UnlockId);
            Assert.That(wardrobe.Owned.Select(i => i.Id), Is.EqualTo(new[] { "hat.sprout_cap", "skin.speckles" }), "starter items are given once");

            Assert.That(wardrobe.Equip(Family.Drop, "hat.leaf_cap"), Is.False, "not owned");
            save.Cosmetics.Owned.Add("hat.leaf_cap");
            save.Cosmetics.Owned.Add("frame.daisy");
            save.Cosmetics.Owned.Add("expression.wink");
            Assert.That(wardrobe.Equip(Family.Drop, "frame.daisy"), Is.False, "a frame is not worn by Bloomlings");
            Assert.That(wardrobe.Equip(Family.Drop, "hat.sprout_cap"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "hat.leaf_cap"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "skin.speckles"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "expression.wink"), Is.True);
            Outfit drop = wardrobe.OutfitOf(Family.Drop);
            Assert.That((drop.Skin?.Id, drop.Hat?.Id, drop.Trail, drop.Expression?.Id), Is.EqualTo(("skin.speckles", "hat.leaf_cap", (CosmeticItem?)null, "expression.wink")), "one item of each kind");
            Assert.That(wardrobe.OutfitOf(Family.Sprig).IsEmpty, Is.True);
            Assert.That(save.Cosmetics.Equipped.Keys, Is.EqualTo(new[] { "drop.expression", "drop.hat", "drop.skin" }));

            wardrobe.Unequip(Family.Drop, CosmeticKind.Hat);
            Assert.That(wardrobe.EquippedFor(Family.Drop, CosmeticKind.Hat), Is.Null);
            Assert.That(wardrobe.EquippedFor(Family.Drop, CosmeticKind.Skin)!.Id, Is.EqualTo("skin.speckles"));

            PlayerSave read = SaveSerializer.Read(SaveSerializer.Write(save));
            Assert.That(read.Cosmetics.Equipped, Is.EqualTo(save.Cosmetics.Equipped), "the save schema accepts the slots");
            Assert.That(saves, Is.GreaterThan(0));

            save.Unlocks.Flags.Remove(WardrobeService.UnlockId);
            Assert.That(wardrobe.OutfitOf(Family.Drop).IsEmpty, Is.True, "nothing is worn while the Wardrobe is closed");
        }

        [Test]
        public void EarlySaves_WithOneItemPerFamily_StillRead()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            JObject json = JObject.Parse(SaveSerializer.Write(save));
            json["cosmetics"]!["equipped"] = new JObject { ["drop"] = "hat.leaf_cap", ["sprig"] = "trail.petal_sparkle" };

            PlayerSave read = SaveSerializer.Read(json.ToString());

            Assert.That(read.Cosmetics.Equipped, Is.EqualTo(new SortedDictionary<string, string>
            {
                ["drop.hat"] = "hat.leaf_cap",
                ["sprig.trail"] = "trail.petal_sparkle",
            }));
            json["cosmetics"]!["equipped"] = new JObject { ["drop"] = "frame.daisy" };
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(json.ToString()), "a frame was never worn");
            json["cosmetics"]!["equipped"] = new JObject { ["profile"] = new JObject { ["hat"] = "hat.leaf_cap" } };
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(json.ToString()), "the profile shows frames, badges and markers only");
        }

        [Test]
        public void Profile_ShowsTheChosenDecoration_ElseTheNewestOwned()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => { });
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            Assert.That(wardrobe.Profile, Is.EqualTo(ProfileLook.None));

            save.Cosmetics.Owned.Add("frame.daisy");
            save.Cosmetics.Owned.Add("frame.ivy");
            save.Cosmetics.Owned.Add(CosmeticCatalog.LevelMarkerId(750));
            Assert.That(wardrobe.Profile.Frame!.Id, Is.EqualTo("frame.ivy"), "the newest owned frame by default");
            Assert.That(wardrobe.Profile.Marker!.MilestoneLevel, Is.EqualTo(750));

            Assert.That(wardrobe.Show("frame.daisy"), Is.True);
            Assert.That(wardrobe.Show("hat.sprout_cap"), Is.False, "not owned, and not a profile item");
            Assert.That(wardrobe.Profile.Frame!.Id, Is.EqualTo("frame.daisy"));
            Assert.That(save.Cosmetics.Equipped["profile.frame"], Is.EqualTo("frame.daisy"));
            Assert.That(SaveSerializer.Read(SaveSerializer.Write(save)).Cosmetics.Equipped["profile.frame"], Is.EqualTo("frame.daisy"));
        }

        [Test]
        public void StoreCosmetics_AreBoughtWithPetals_OnceTheWardrobeIsOpen()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            var economy = new EconomyService(save, EconomyConfig.From(new BundledRemoteConfigService()), () => { });
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => { }, economy);
            CosmeticItem spots = wardrobe.ForSale.Single(i => i.Id == "skin.spots");
            save.Wallet.AddPetals(spots.Price + 10);

            Assert.That(wardrobe.TryBuy(spots.Id), Is.False, "the Wardrobe is not open yet");
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            Assert.That(wardrobe.TryBuy("hat.leaf_cap"), Is.False, "milestone items are not sold");
            Assert.That(wardrobe.TryBuy(spots.Id), Is.True);
            Assert.That(economy.Petals, Is.EqualTo(10));
            Assert.That(wardrobe.OwnedOf(CosmeticKind.Skin).Select(i => i.Id), Does.Contain("skin.spots"));
            Assert.That(wardrobe.ForSale.Select(i => i.Id), Does.Not.Contain("skin.spots"));
            Assert.That(wardrobe.TryBuy(spots.Id), Is.False, "owned");

            CosmeticItem stripes = wardrobe.ForSale.Single(i => i.Id == "skin.stripes");
            Assert.That(wardrobe.TryBuy(stripes.Id), Is.False, "Petals are short");
            Assert.That((economy.Petals, save.Cosmetics.Owned.Contains(stripes.Id)), Is.EqualTo((10, false)));
        }

        [Test]
        public void CatalogRules_SoldItemsAreNotMilestoneItems_AndEveryKindHasItems()
        {
            CosmeticCatalog catalog = Catalog;
            var milestoneItems = MilestoneTable.Default.Cadences.SelectMany(c => c.Items).ToHashSet();
            Assert.That(catalog.Items.Where(i => i.ForSale).Select(i => i.Id).Where(milestoneItems.Contains), Is.Empty, "milestone and prestige items are never sold");
            Assert.That(catalog.Items.Where(i => i.Starter).All(i => !i.ForSale), Is.True);
            foreach (CosmeticKind kind in Enum.GetValues(typeof(CosmeticKind)))
            {
                Assert.That(catalog.Items.Any(i => i.Kind == kind), Is.True, kind.ToString());
            }

            Assert.That(catalog.Items.Any(i => i.Kind == CosmeticKind.Skin && i.ForSale), Is.True, "skins in the Store");
            Assert.That(MilestoneTable.Default.Cadences.Where(c => c.Tier == MilestoneTier.Prestige).SelectMany(c => c.Items).Any(id => id.StartsWith("skin.", StringComparison.Ordinal)), Is.True, "a prestige skin");

            Assert.That(catalog.TryGet("badge.level_350", out CosmeticItem? badge), Is.True);
            Assert.That((badge!.Kind, badge.MilestoneLevel, badge.IsWorn), Is.EqualTo((CosmeticKind.Badge, (int?)350, false)));
            Assert.That(catalog.TryGet("marker.level_1250", out CosmeticItem? marker), Is.True);
            Assert.That(marker!.Kind, Is.EqualTo(CosmeticKind.Marker));
            foreach (string bad in new[] { "badge.level_0", "badge.level_07", "badge.level_", "badge.level_x", "hat.level_50" })
            {
                Assert.That(catalog.TryGet(bad, out _), Is.False, bad);
            }
        }

        [Test]
        public void InkOnVariantColors_IsDarkOnLightVariants_AndAlwaysReadable()
        {
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                Assert.That(CosmeticCatalog.TryParseHex(info.ColorHex, out double r, out double g, out double b), Is.True);
                Assert.That(InkContrast.BestRatio(r, g, b), Is.GreaterThanOrEqualTo(3.0), info.Id.Key + ": icons and counts need 3:1");
                bool dark = InkContrast.UseDarkInk(r, g, b);
                // Spec 005 research D1: the saturated palette makes Moss light enough for the dark ink too.
                if (info.Id == VariantId.Leaf || info.Id == VariantId.Moss || info.Id == VariantId.Flower || info.Id == VariantId.Dew)
                {
                    Assert.That(dark, Is.True, info.Id.Key);
                }
                else if (info.Id == VariantId.VioletBud || info.Id == VariantId.Wood)
                {
                    Assert.That(dark, Is.False, info.Id.Key);
                }
            }
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
