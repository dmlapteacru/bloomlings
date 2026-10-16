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
    public class SprigGearTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));

        [Test]
        public void FreeSprigAccessories_EquipIndependentlyAndSurviveSaveWithoutSpending()
        {
            var save=PlayerSave.CreateNew("gear",DateTime.UtcNow);
            var config=new BundledRemoteConfigService();
            var economy=new EconomyService(save,EconomyConfig.From(config),()=>{});
            var wardrobe=new WardrobeService(save,Catalog,config,()=>{},economy);
            long before=economy.Petals;
            Assert.That(wardrobe.IsAvailable,Is.False,"the existing wardrobe unlock is retained");
            foreach(string id in new[]{SprigGearArt.CapId,SprigGearArt.GlassesId,SprigGearArt.BackpackId})
            {
                Assert.That(wardrobe.ShopItems.Any(i=>i.Id==id),Is.True);
                Assert.That(wardrobe.Equip(Family.Bloom,id),Is.False,"an unfitted item cannot be worn by another hero");
                Assert.That(wardrobe.Equip(Family.Sprig,id),Is.True);
            }
            var restored=SaveSerializer.Read(SaveSerializer.ToJObject(save));
            var loaded=new WardrobeService(restored,Catalog,config,()=>{});
            Outfit look=loaded.OutfitOf(Family.Sprig);
            Assert.That(look.Hat?.Id,Is.EqualTo(SprigGearArt.CapId));
            Assert.That(look.Glasses?.Id,Is.EqualTo(SprigGearArt.GlassesId));
            Assert.That(look.Backpack?.Id,Is.EqualTo(SprigGearArt.BackpackId));
            Assert.That(look.ForGameplay.IsEmpty,Is.True,"3D meta accessories do not enter the gameplay figures");
            Assert.That(economy.Petals,Is.EqualTo(before));
            loaded.Unequip(Family.Sprig,CosmeticKind.Hat);
            Assert.That(loaded.OutfitOf(Family.Sprig).Glasses,Is.Not.Null);
            Assert.That(loaded.OutfitOf(Family.Sprig).Backpack,Is.Not.Null);
            loaded.UnequipAccessories(Family.Sprig);
            Assert.That(loaded.OutfitOf(Family.Sprig).IsEmpty,Is.True);
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void AllOutfits_SelectMatchingHeadAndDepthMaskedLayers(int mask)
        {
            var wardrobe=new WardrobeService(PlayerSave.CreateNew("gear",DateTime.UtcNow),Catalog,new BundledRemoteConfigService(),()=>{});
            string[] ids={SprigGearArt.CapId,SprigGearArt.GlassesId,SprigGearArt.BackpackId};
            for(int i=0;i<3;i++)if((mask&(1<<i))!=0)wardrobe.Equip(Family.Sprig,ids[i]);
            Outfit look=wardrobe.OutfitOf(Family.Sprig);
            Assert.That(SprigGearArt.Layer(look,0)!.Value.Name,Is.EqualTo((mask&1)!=0 ? "sprigmod-cap" : "sprigmod-base"));
            Assert.That(SprigGearArt.Layer(look,1).HasValue,Is.EqualTo((mask&4)!=0));
            Assert.That(SprigGearArt.Layer(look,2).HasValue,Is.EqualTo((mask&2)!=0));
            var shelf=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat,Family.Bloom);
            Assert.That(shelf.Any(c=>c.Item!=null&&SprigGearArt.IsTrial(c.Item.Id)),Is.False);
        }
    }
}
