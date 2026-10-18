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
        public void CurrentSprigAccessories_KeepIndependentSlotsAndRetiredBackpackStaysHidden()
        {
            var save=WardrobeArtInventory.Create("gear",DateTime.UtcNow);
            var config=new BundledRemoteConfigService();
            var economy=new EconomyService(save,EconomyConfig.From(config),()=>{});
            var wardrobe=new WardrobeService(save,Catalog,config,()=>{},economy);
            long before=economy.Petals;
            foreach(string id in new[]{SprigV2Art.CapId,SprigV2Art.GlassesId,SprigV2Art.ScarfId})
            {
                Assert.That(wardrobe.Equip(Family.Bloom,id),Is.False);
                Assert.That(wardrobe.Equip(Family.Sprig,id),Is.True);
            }
            Assert.That(wardrobe.Equip(Family.Sprig,"backpack.sprig_leaf"),Is.False);
            var restored=SaveSerializer.Read(SaveSerializer.ToJObject(save));
            var loaded=new WardrobeService(restored,Catalog,config,()=>{});
            Outfit look=loaded.OutfitOf(Family.Sprig);
            Assert.That(look.Hat?.Id,Is.EqualTo(SprigV2Art.CapId));
            Assert.That(look.Glasses?.Id,Is.EqualTo(SprigV2Art.GlassesId));
            Assert.That(look.Scarf?.Id,Is.EqualTo(SprigV2Art.ScarfId));
            Assert.That(look.Backpack,Is.Null);
            Assert.That(look.ForGameplay.IsEmpty,Is.True);
            Assert.That(economy.Petals,Is.EqualTo(before));
            loaded.Unequip(Family.Sprig,CosmeticKind.Hat);
            Assert.That(loaded.OutfitOf(Family.Sprig).Glasses,Is.Not.Null);
            Assert.That(loaded.OutfitOf(Family.Sprig).Scarf,Is.Not.Null);
            loaded.UnequipAccessories(Family.Sprig);
            Assert.That(loaded.OutfitOf(Family.Sprig).IsEmpty,Is.True);
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void LegacySelectionsUseOnlyCurrentHeroParts(int mask)
        {
            var wardrobe=new WardrobeService(WardrobeArtInventory.Create("gear",DateTime.UtcNow),Catalog,new BundledRemoteConfigService(),()=>{});
            string[] ids={SprigV2Art.CapId,SprigV2Art.GlassesId,"backpack.sprig_leaf"};
            for(int i=0;i<3;i++)if((mask&(1<<i))!=0)wardrobe.Equip(Family.Sprig,ids[i]);
            Outfit look=wardrobe.OutfitOf(Family.Sprig);
            Assert.That(HeroGearArt.Layer(Family.Sprig,look,0)!.Value.Name,Is.EqualTo((mask&1)!=0 ? "sprigv2-base-cap" : "sprigv2-base-hairleafy"));
            Assert.That(HeroGearArt.Layer(Family.Sprig,look,3).HasValue,Is.EqualTo((mask&2)!=0));
            Assert.That(HeroGearArt.DisplayOutfit(look)!.Backpack,Is.Null,"retired unfitted backpacks remain saved but cannot resurrect the old mesh");
            var shelf=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat,Family.Bloom);
            Assert.That(shelf.Any(c=>c.Item?.Id==SprigV2Art.CapId),Is.False);
        }
    }
}
