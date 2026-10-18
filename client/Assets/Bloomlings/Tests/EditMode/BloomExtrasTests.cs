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
    public class BloomExtrasTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));
        [Test]
        public void NineNewItemsAreFreeSeparateHeadAndBodyChoicesAndKeepTheirSelection()
        {
            var save=PlayerSave.CreateNew("bloom-extras",DateTime.UtcNow);
            var config=new BundledRemoteConfigService();var wardrobe=new WardrobeService(save,Catalog,config,()=>{});
            Assert.That(BloomExtrasArt.HeadIds.Length,Is.EqualTo(7));
            Assert.That(BloomExtrasArt.ClothesIds.Length,Is.EqualTo(2));
            Assert.That(BloomExtrasArt.ItemIds.Count(),Is.EqualTo(9));
            wardrobe.Equip(Family.Bloom,EyewearArt.BloomHeartId);wardrobe.Equip(Family.Bloom,BloomV2Art.KnitId);
            foreach(string id in BloomExtrasArt.ItemIds)
            {
                Assert.That(Catalog.TryGet(id,out var item),Is.True);
                Assert.That(item!.Free&&item.MetaOnly&&HeroGearArt.CanPreview(item),Is.True,id);
                Assert.That(item.Kind,Is.EqualTo(BloomExtrasArt.IsHead(id)?CosmeticKind.Hat:CosmeticKind.Clothes));
                Assert.That(WardrobeShelfCatalog.Build(wardrobe,item.Kind,Family.Bloom).Any(c=>c.Item?.Id==id),Is.True);
                Assert.That(wardrobe.Equip(Family.Sprig,id),Is.False);
                Assert.That(wardrobe.Equip(Family.Bloom,id),Is.True);
                var loaded=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,config,()=>{});
                var shown=HeroGearArt.DisplayOutfit(loaded.OutfitOf(Family.Bloom))!;
                Assert.That(shown.Get(item.Kind)?.Id,Is.EqualTo(id));
                Assert.That(shown.Glasses?.Id,Is.EqualTo(EyewearArt.BloomHeartId));
                Assert.That(shown.Scarf?.Id,Is.EqualTo(BloomV2Art.KnitId));
                Assert.That(HeroGearArt.Icon(id),Is.Not.Null);
                Assert.That(HeroGearArt.Layer(Family.Bloom,shown,3),Is.Not.Null);
                Assert.That(HomeHeroArt.Layer(Family.Bloom,shown,3),Is.Not.Null);
                Assert.That(shown.ForGameplay.IsEmpty,Is.True);
            }
            wardrobe.Unequip(Family.Bloom,CosmeticKind.Hat);
            Assert.That(HeroGearArt.Layer(Family.Bloom,wardrobe.OutfitOf(Family.Bloom),0)!.Value.Name,Is.EqualTo("bloomv2-base-hairpetals"));
            Assert.That(wardrobe.OutfitOf(Family.Bloom).Clothes,Is.Not.Null,"default Head preserves clothing");
        }
    }
}
