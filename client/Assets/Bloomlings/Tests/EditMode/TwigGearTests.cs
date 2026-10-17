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
        public void StockTwig_AlwaysWearsAcornHat_WhenFreshRemovedOrReset()
        {
            var save = PlayerSave.CreateNew("stock-twig", DateTime.UtcNow);
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => {});
            Assert.That(wardrobe.OutfitOf(Family.Twig).IsEmpty, Is.True, "stock appearance needs no custom equipment entry");
            Assert.That(HeroGearArt.Layer(Family.Twig, wardrobe.OutfitOf(Family.Twig), 0)!.Value.Name, Is.EqualTo("twigv2-base-pumpkinhat"));
            Assert.That(SimpleHeroArt.Frame(Family.Twig).Name, Is.EqualTo("twigv2-base-pumpkinhat"));
            Assert.That(HeroMotion.FrameName(Family.Twig, MotionClip.Idle, 0), Is.EqualTo("twigv2-base-pumpkinhat"));
            wardrobe.Equip(Family.Twig, TwigV2Art.BucketId);
            wardrobe.Equip(Family.Twig, TwigV2Art.GlassesId);
            wardrobe.Equip(Family.Twig, TwigV2Art.CoatId);
            wardrobe.Unequip(Family.Twig, CosmeticKind.Hat);
            var shown = HeroGearArt.DisplayOutfit(wardrobe.OutfitOf(Family.Twig));
            Assert.That(HeroGearArt.Layer(Family.Twig, shown, 0)!.Value.Name, Is.EqualTo("twigv2-base-pumpkinhat"));
            Assert.That(HeroGearArt.Layer(Family.Twig, shown, 3)!.Value.Name, Is.EqualTo("twigv2-glasses-pumpkinhat"), "eyewear uses the stock hat's matching mask");
            Assert.That(shown!.Clothes?.Id, Is.EqualTo(TwigV2Art.CoatId));
            var reset = WardrobeShelfCatalog.Preview(shown, CosmeticKind.Hat, null);
            Assert.That(reset.IsEmpty, Is.True);
            Assert.That(HeroGearArt.Layer(Family.Twig, reset, 0)!.Value.Name, Is.EqualTo("twigv2-base-pumpkinhat"));
            wardrobe.UnequipAccessories(Family.Twig);
            var loaded = new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)), Catalog, new BundledRemoteConfigService(), () => {});
            Assert.That(HeroGearArt.Layer(Family.Twig, loaded.OutfitOf(Family.Twig), 0)!.Value.Name, Is.EqualTo("twigv2-base-pumpkinhat"));
        }

        [Test]
        public void LegacyFlowerCap_ResolvesToPanamaWithoutLosingProgressOrChargingPetals()
        {
            var save = PlayerSave.CreateNew("legacy", DateTime.UtcNow);
            save.Cosmetics.Equipped["twig.hat"] = "hat.twig_flower_cap";
            var config = new BundledRemoteConfigService();
            var economy = new EconomyService(save, EconomyConfig.From(config), () => {});
            long petals = economy.Petals;
            var wardrobe = new WardrobeService(save, Catalog, config, () => {}, economy);
            Assert.That(wardrobe.OutfitOf(Family.Twig).Hat?.Id, Is.EqualTo(TwigV2Art.BucketId));
            Assert.That(HeroGearArt.Layer(Family.Twig, wardrobe.OutfitOf(Family.Twig), 0)!.Value.Name, Is.EqualTo("twigv2-base-buckethat"));
            Assert.That(wardrobe.ShopItems.Any(i => i.Id == "hat.twig_flower_cap"), Is.False);
            Assert.That(wardrobe.Equip(Family.Twig, "hat.twig_flower_cap"), Is.True);
            Assert.That(save.Cosmetics.Equipped["twig.hat"], Is.EqualTo(TwigV2Art.BucketId));
            var loaded = new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)), Catalog, config, () => {});
            Assert.That(loaded.OutfitOf(Family.Twig).Hat?.Id, Is.EqualTo(TwigV2Art.BucketId));
            Assert.That(economy.Petals, Is.EqualTo(petals));
            loaded.UnequipAccessories(Family.Twig);
            Assert.That(loaded.OutfitOf(Family.Twig).IsEmpty, Is.True);
            Assert.That(HeroGearArt.Layer(Family.Twig, loaded.OutfitOf(Family.Twig), 0)!.Value.Name, Is.EqualTo("twigv2-base-pumpkinhat"));
        }

        [TestCase(TwigV2Art.AcornId, "twigv2-base-pumpkinhat")]
        [TestCase(TwigV2Art.BucketId, "twigv2-base-buckethat")]
        public void RefittedTwigHats_AreFreeSaveableExclusiveAndStayOnTheNewBase(string id, string frame)
        {
            var save = PlayerSave.CreateNew("fitted", DateTime.UtcNow);
            var config = new BundledRemoteConfigService();
            var economy = new EconomyService(save, EconomyConfig.From(config), () => {});
            var wardrobe = new WardrobeService(save, Catalog, config, () => {}, economy);
            long petals = economy.Petals;
            Assert.That(wardrobe.IsAvailable, Is.False);
            Assert.That(wardrobe.Equip(Family.Bloom, id), Is.False);
            Assert.That(wardrobe.Equip(Family.Twig, TwigV2Art.AcornId), Is.True);
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
            Assert.That(HeroGearArt.Layer(Family.Twig, loaded.OutfitOf(Family.Twig), 0)!.Value.Name, Is.EqualTo("twigv2-base-pumpkinhat"));
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
        public void V2_All128HeadClothesGlassesScarfCombinationsSurvivesReloadAndDrawsTheSavedLayers()
        {
            string[] heads = new[] { "" }.Concat(TwigV2Art.HeadIds).ToArray();
            foreach (string head in heads)
            foreach (string clothes in new[] { "" }.Concat(TwigV2Art.ClothesIds))
            foreach (bool glasses in new[] { false, true })
            foreach (bool scarf in new[] { false, true })
            {
                var save = PlayerSave.CreateNew("v2-combo", DateTime.UtcNow);
                var config = new BundledRemoteConfigService();
                var wardrobe = new WardrobeService(save, Catalog, config, () => {});
                if (head.Length > 0) Assert.That(wardrobe.Equip(Family.Twig, head), Is.True);
                if (clothes.Length > 0) Assert.That(wardrobe.Equip(Family.Twig, clothes), Is.True);
                if (glasses) Assert.That(wardrobe.Equip(Family.Twig, TwigV2Art.GlassesId), Is.True);
                if (scarf) Assert.That(wardrobe.Equip(Family.Twig, TwigV2Art.ScarfId), Is.True);
                var reloadedSave = SaveSerializer.Read(SaveSerializer.ToJObject(save));
                var loaded = new WardrobeService(reloadedSave, Catalog, config, () => {});
                Outfit worn = loaded.OutfitOf(Family.Twig);
                Outfit shown = HeroGearArt.DisplayOutfit(worn)!;
                Assert.That(shown.Hat?.Id, Is.EqualTo(head.Length == 0 ? null : head));
                Assert.That(shown.Glasses?.Id, Is.EqualTo(glasses ? TwigV2Art.GlassesId : null));
                Assert.That(shown.Clothes?.Id, Is.EqualTo(clothes.Length == 0 ? null : clothes));
                Assert.That(shown.Scarf?.Id, Is.EqualTo(scarf ? TwigV2Art.ScarfId : null));
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 0), Is.Not.Null);
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 1).HasValue, Is.EqualTo(clothes.Length > 0));
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 2).HasValue, Is.EqualTo(scarf));
                Assert.That(HeroGearArt.Layer(Family.Twig, shown, 3).HasValue, Is.EqualTo(glasses));
                string garment = clothes.Length == 0 ? "none" : clothes.Substring("clothes.twig_".Length);
                if (clothes.Length > 0) Assert.That(HeroGearArt.Layer(Family.Twig, shown, 1)!.Value.Name, Is.EqualTo("twigv2-clothes-" + garment));
                if (scarf) Assert.That(HeroGearArt.Layer(Family.Twig, shown, 2)!.Value.Name, Is.EqualTo("twigv2-scarf-" + garment), "scarf mask follows the selected garment");
                Assert.That(shown.ForGameplay.IsEmpty, Is.True);
                Assert.That(WardrobeShelfCatalog.Chosen(loaded, Family.Twig, CosmeticKind.Hat, null), Is.EqualTo(head.Length == 0 && clothes.Length == 0 && !glasses && !scarf));
                if (clothes.Length > 0)
                {
                    Assert.That(CosmeticCatalog.KindOfName(CosmeticCatalog.KindName(CosmeticKind.Clothes)), Is.EqualTo(CosmeticKind.Clothes));
                    loaded.Unequip(Family.Twig, CosmeticKind.Clothes);
                    Assert.That(loaded.OutfitOf(Family.Twig).Clothes, Is.Null);
                    Assert.That(loaded.OutfitOf(Family.Twig).Hat?.Id, Is.EqualTo(head.Length == 0 ? null : head));
                    Assert.That(loaded.OutfitOf(Family.Twig).Scarf?.Id, Is.EqualTo(scarf ? TwigV2Art.ScarfId : null));
                }
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
