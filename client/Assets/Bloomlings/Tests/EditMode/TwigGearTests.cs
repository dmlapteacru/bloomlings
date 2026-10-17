using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    public class TwigGearTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));

        [Test]
        public void FreeTwigHats_SwapRemoveAndSurviveSaveWithoutSpendingOrUnlockingWardrobe()
        {
            var save = PlayerSave.CreateNew("twig", DateTime.UtcNow);
            var config = new BundledRemoteConfigService();
            var economy = new EconomyService(save, EconomyConfig.From(config), () => {});
            var wardrobe = new WardrobeService(save, Catalog, config, () => {}, economy);
            long before = economy.Petals;
            Assert.That(wardrobe.IsAvailable, Is.False);
            foreach (string id in new[] { TwigGearArt.AcornHatId, TwigGearArt.FlowerCapId })
            {
                Assert.That(wardrobe.ShopItems.Any(i => i.Id == id), Is.True);
                Assert.That(wardrobe.Equip(Family.Sprig, id), Is.False, "headwear is fitted only to Twig");
                Assert.That(wardrobe.Equip(Family.Twig, id), Is.True);
                var restored = SaveSerializer.Read(SaveSerializer.ToJObject(save));
                var loaded = new WardrobeService(restored, Catalog, config, () => {});
                Outfit look = loaded.OutfitOf(Family.Twig);
                Assert.That(look.Hat?.Id, Is.EqualTo(id));
                Assert.That(look.ForGameplay.IsEmpty, Is.True, "meta headwear stays out of puzzle figures");
                Assert.That(TwigGearArt.IsHat(look.Hat?.Id), Is.True);
                Assert.That(TwigGearArt.Frame(look).Name,
                    Is.EqualTo(id == TwigGearArt.AcornHatId ? "twigmod-acorn" : "twigmod-cap"));
                Assert.That(HeroGearArt.Icon(id), Is.Not.Null);
            }
            Assert.That(economy.Petals, Is.EqualTo(before));
            wardrobe.Unequip(Family.Twig, CosmeticKind.Hat);
            var removed = new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)), Catalog, config, () => {});
            Assert.That(removed.OutfitOf(Family.Twig).Hat, Is.Null);
            Assert.That(TwigGearArt.Frame(removed.OutfitOf(Family.Twig)).Name, Is.EqualTo("twigmod-base"));
        }

        [Test]
        public void SimplifiedPreview_KeepsLegacyEquipmentSavedButDoesNotMisapplyItsParts()
        {
            Assert.That(HeroGearArt.Supports(Family.Twig), Is.True);
            Assert.That(HeroGearArt.Layer(Family.Twig, Outfit.None, 0)!.Value.Name, Is.EqualTo("twigv2-base-none"));
            Assert.That(HeroGearArt.Layer(Family.Twig, Outfit.None, 1), Is.Null);
            Assert.That(HeroGearArt.Layer(Family.Twig, Outfit.None, 2), Is.Null);
            Assert.That(HeroGearArt.HasBakedHat(Family.Twig, Outfit.None), Is.False);
            Assert.That(HeroGearArt.Supports(Family.Sprig), Is.True);
            Assert.That(HeroGearArt.Layer(Family.Sprig, Outfit.None, 0), Is.Not.Null);
            Assert.That(HeroGearArt.Supports(Family.Bloom), Is.True);
            Assert.That(HeroGearArt.Supports(Family.Drop), Is.True);
            var save = PlayerSave.CreateNew("preview", DateTime.UtcNow);
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => {});
            wardrobe.Equip(Family.Twig, TwigGearArt.FlowerCapId);
            wardrobe.Equip(Family.Sprig, SprigGearArt.GlassesId);
            Assert.That(HeroGearArt.DisplayOutfit(wardrobe.OutfitOf(Family.Twig))!.Hat, Is.Null);
            Assert.That(HeroGearArt.DisplayOutfit(wardrobe.OutfitOf(Family.Sprig))!.Glasses, Is.Null);
            Assert.That(wardrobe.OutfitOf(Family.Twig).Hat?.Id, Is.EqualTo(TwigGearArt.FlowerCapId));
            Assert.That(WardrobeShelfCatalog.Build(wardrobe, CosmeticKind.Hat, Family.Twig).Any(c => c.Item?.MetaOnly == true && !TwigV2Art.IsItem(c.Item.Id)), Is.False);
            Assert.That(WardrobeShelfCatalog.Chosen(wardrobe, Family.Twig, CosmeticKind.Hat, null), Is.True);
            var reloaded = new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)), Catalog, new BundledRemoteConfigService(), () => {});
            Assert.That(reloaded.OutfitOf(Family.Sprig).Glasses?.Id, Is.EqualTo(SprigGearArt.GlassesId));

        }
        [TestCase(TwigModularArt.AcornHatId, "twigv2-base-pumpkinhat")]
        [TestCase(TwigModularArt.PanamaHatId, "twigv2-base-buckethat")]
        public void RefittedTwigHats_AreFreeSaveableExclusiveAndStayOnTheNewBase(string id, string frame)
        {
            var save = PlayerSave.CreateNew("fitted", DateTime.UtcNow);
            var config = new BundledRemoteConfigService();
            var economy = new EconomyService(save, EconomyConfig.From(config), () => {});
            var wardrobe = new WardrobeService(save, Catalog, config, () => {}, economy);
            long petals = economy.Petals;
            Assert.That(wardrobe.IsAvailable, Is.False);
            Assert.That(wardrobe.Equip(Family.Bloom, id), Is.False);
            Assert.That(wardrobe.Equip(Family.Twig, TwigModularArt.AcornHatId), Is.True);
            Assert.That(wardrobe.Equip(Family.Twig, id), Is.True);
            var loaded = new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)), Catalog, config, () => {});
            Outfit shown = HeroGearArt.DisplayOutfit(loaded.OutfitOf(Family.Twig))!;
            Assert.That(shown.Hat?.Id, Is.EqualTo(id));
            Assert.That(HeroGearArt.Layer(Family.Twig, shown, 0)!.Value.Name, Is.EqualTo(frame));
            Assert.That(HeroGearArt.HasBakedHat(Family.Twig, shown), Is.True);
            Assert.That(HeroGearArt.CanPreview(shown.Hat!), Is.True);
            Assert.That(HeroGearArt.Icon(id)!.Value.Name, Does.StartWith("twigv2-icon-"));
            Assert.That(shown.ForGameplay.IsEmpty, Is.True);
            Assert.That(economy.Petals, Is.EqualTo(petals));
            loaded.Unequip(Family.Twig, CosmeticKind.Hat);
            Assert.That(HeroGearArt.Layer(Family.Twig, loaded.OutfitOf(Family.Twig), 0)!.Value.Name, Is.EqualTo("twigv2-base-none"));
        }

        [Test]
        public void V2_AllDeliveredItemsAreFreeVisibleAndRestrictedToTwig()
        {
            var save = PlayerSave.CreateNew("v2-items", DateTime.UtcNow);
            var config = new BundledRemoteConfigService();
            var economy = new EconomyService(save, EconomyConfig.From(config), () => {});
            var wardrobe = new WardrobeService(save, Catalog, config, () => {}, economy);
            long petals = economy.Petals;
            var shelf = WardrobeShelfCatalog.Build(wardrobe, CosmeticKind.Hat, Family.Twig);
            foreach (string id in TwigV2Art.ItemIds)
            {
                Assert.That(Catalog.TryGet(id, out var item), Is.True, id);
                Assert.That(item!.Free && item.MetaOnly && HeroGearArt.CanPreview(item), Is.True, id);
                Assert.That(shelf.Any(cell => cell.Item?.Id == id), Is.True, id);
                Assert.That(TwigV2Art.Icon(id), Is.Not.Null, id);
                Assert.That(wardrobe.Equip(Family.Sprig, id), Is.False, id);
                Assert.That(wardrobe.Equip(Family.Twig, id), Is.True, id);
            }
            Assert.That(economy.Petals, Is.EqualTo(petals));
            Assert.That(wardrobe.IsAvailable, Is.False, "all free delivered parts work before L40");
        }

        [Test]
        public void V2_EveryHeadGlassesScarfCombinationSurvivesReloadAndDrawsTheSavedLayers()
        {
            string[] heads = new[] { "" }.Concat(TwigV2Art.HeadIds).ToArray();
            foreach (string head in heads)
            foreach (bool glasses in new[] { false, true })
            foreach (bool scarf in new[] { false, true })
            {
                var save = PlayerSave.CreateNew("v2-combo", DateTime.UtcNow);
                var config = new BundledRemoteConfigService();
                var wardrobe = new WardrobeService(save, Catalog, config, () => {});
                if (head.Length > 0) Assert.That(wardrobe.Equip(Family.Twig, head), Is.True);
                if (glasses) Assert.That(wardrobe.Equip(Family.Twig, TwigV2Art.GlassesId), Is.True);
                if (scarf) Assert.That(wardrobe.Equip(Family.Twig, TwigV2Art.ScarfId), Is.True);
                var reloadedSave = SaveSerializer.Read(SaveSerializer.ToJObject(save));
                var loaded = new WardrobeService(reloadedSave, Catalog, config, () => {});
                Outfit worn = loaded.OutfitOf(Family.Twig);
                Outfit shown = HeroGearArt.DisplayOutfit(worn)!;
                Assert.That(shown.Hat?.Id, Is.EqualTo(head.Length == 0 ? null : head));
                Assert.That(shown.Glasses?.Id, Is.EqualTo(glasses ? TwigV2Art.GlassesId : null));
                Assert.That(shown.Scarf?.Id, Is.EqualTo(scarf ? TwigV2Art.ScarfId : null));
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 0), Is.Not.Null);
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 1).HasValue, Is.EqualTo(scarf));
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 2).HasValue, Is.EqualTo(glasses));
                Assert.That(shown.ForGameplay.IsEmpty, Is.True);
                Assert.That(WardrobeShelfCatalog.Chosen(loaded, Family.Twig, CosmeticKind.Hat, null), Is.EqualTo(head.Length == 0 && !glasses && !scarf));
                if (scarf)
                {
                    Assert.That(CosmeticCatalog.KindOfName(CosmeticCatalog.KindName(CosmeticKind.Scarf)), Is.EqualTo(CosmeticKind.Scarf));
                    Assert.That(WardrobeShelfCatalog.Preview(shown, CosmeticKind.Hat, null).IsEmpty, Is.True);
                    loaded.Unequip(Family.Twig, CosmeticKind.Scarf);
                    Assert.That(loaded.OutfitOf(Family.Twig).Scarf, Is.Null);
                    Assert.That(loaded.OutfitOf(Family.Twig).Glasses?.Id, Is.EqualTo(glasses ? TwigV2Art.GlassesId : null));
                }
                loaded.UnequipAccessories(Family.Twig);
                Assert.That(loaded.OutfitOf(Family.Twig).IsEmpty, Is.True);
                Assert.That(new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(reloadedSave)), Catalog, config, () => {}).OutfitOf(Family.Twig).IsEmpty, Is.True);
            }
        }
    }
}
