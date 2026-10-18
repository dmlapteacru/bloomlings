using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    public class WardrobeFinishTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));
        [TestCase(Family.Drop,12,5,"dropv2-base-nativedrop")]
        public void DeliveredItemsStayInTheirFamilyAndSlotAndPersistWithEyewearAndScarf(Family family,int total,int bodies,string stock)
        {
            var save=PlayerSave.CreateNew("wardrobe-finish",DateTime.UtcNow);var config=new BundledRemoteConfigService();
            var wardrobe=new WardrobeService(save,Catalog,config,()=>{});
            var items=Catalog.Items.Where(i=>WardrobeFinishArt.IsItem(i.Id)&&i.Fits(family)).ToArray();
            Assert.That(items.Length,Is.EqualTo(total));
            Assert.That(items.Count(i=>i.Kind==CosmeticKind.Hat),Is.EqualTo(7));
            Assert.That(items.Count(i=>i.Kind==CosmeticKind.Clothes),Is.EqualTo(bodies));
            string face=DropV2Art.GlassesId;
            string scarf=DropV2Art.KnitId;
            Assert.That(wardrobe.Equip(family,face)&&wardrobe.Equip(family,scarf),Is.True);
            foreach(var item in items)
            {
                Assert.That(item.Free&&item.MetaOnly&&HeroGearArt.CanPreview(item),Is.True,item.Id);
                Assert.That(WardrobeShelfCatalog.Build(wardrobe,item.Kind,family).Any(c=>c.Item?.Id==item.Id),Is.True);
                Assert.That(wardrobe.Equip(Family.Bloom,item.Id),Is.False);
                Assert.That(wardrobe.Equip(family,item.Id),Is.True);
                var loaded=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,config,()=>{});
                var shown=HeroGearArt.DisplayOutfit(loaded.OutfitOf(family))!;
                Assert.That(shown.Get(item.Kind)?.Id,Is.EqualTo(item.Id));
                Assert.That(shown.Glasses?.Id,Is.EqualTo(face));Assert.That(shown.Scarf?.Id,Is.EqualTo(scarf));
                Assert.That(HeroGearArt.Icon(item.Id),Is.Not.Null);
                for(int i=0;i<HeroGearArt.LayerCount;i++)Assert.That(HomeHeroArt.Layer(family,shown,i).HasValue,Is.EqualTo(HeroGearArt.Layer(family,shown,i).HasValue));
                Assert.That(shown.ForGameplay.IsEmpty,Is.True);
            }
            wardrobe.Unequip(family,CosmeticKind.Hat);
            Assert.That(HeroGearArt.Layer(family,wardrobe.OutfitOf(family),0)!.Value.Name,Is.EqualTo(stock));
            Assert.That(wardrobe.OutfitOf(family).Clothes,Is.Not.Null,"default Head retains the garment");
        }
    }
}
