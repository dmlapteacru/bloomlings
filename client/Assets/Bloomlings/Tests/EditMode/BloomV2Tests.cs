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
    public class BloomV2Tests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));

        [Test]
        public void AllDeliveredItemsAreFittedAndListedOnlyInTheirOwnCategory()
        {
            var wardrobe = new WardrobeService(WardrobeArtInventory.Create("bloom-v2",DateTime.UtcNow),Catalog,new BundledRemoteConfigService(),()=>{});
            Assert.That(CosmeticCatalog.WornKinds, Is.EqualTo(new[]{CosmeticKind.Hat,CosmeticKind.Glasses,CosmeticKind.Clothes,CosmeticKind.Scarf}));
            foreach (string id in BloomV2Art.ItemIds)
            {
                Assert.That(Catalog.TryGet(id,out var item),Is.True,id);
                Assert.That((item!.Free || item.ForSale) && item.MetaOnly && HeroGearArt.CanPreview(item),Is.True,id);
                foreach(var kind in CosmeticCatalog.WornKinds)
                    Assert.That(WardrobeShelfCatalog.Build(wardrobe,kind,Family.Bloom).Any(c=>c.Item?.Id==id),Is.EqualTo(item.Kind==kind&&!item.DefaultLook),id+" "+kind);
                Assert.That(wardrobe.Equip(Family.Twig,id),Is.False);
                Assert.That(BloomV2Art.Icon(id),Is.Not.Null,id);
            }
        }

        [Test]
        public void All240SavedOutfitsReloadWithTheMatchingMasksAndDefaultAffectsOnlyOneSlot()
        {
            var looks=new System.Collections.Generic.HashSet<string>();
            foreach(string head in new[]{""}.Concat(BloomV2Art.HeadIds))
            foreach(string body in new[]{""}.Concat(BloomV2Art.ClothesIds))
            foreach(string scarf in new[]{""}.Concat(BloomV2Art.ScarfIds))
            foreach(bool glasses in new[]{false,true})
            {
                var save=WardrobeArtInventory.Create("bloom-combo",DateTime.UtcNow);
                var config=new BundledRemoteConfigService();
                var wardrobe=new WardrobeService(save,Catalog,config,()=>{});
                foreach(string id in new[]{head,body,scarf,glasses?BloomV2Art.GlassesId:""})
                    if(id.Length>0)Assert.That(wardrobe.Equip(Family.Bloom,id),Is.True,id);
                var loaded=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,config,()=>{});
                Outfit outfit=loaded.OutfitOf(Family.Bloom),shown=HeroGearArt.DisplayOutfit(outfit)!;
                Assert.That(shown.Hat?.Id,Is.EqualTo(head.Length>0?head:null));
                Assert.That(shown.Clothes?.Id,Is.EqualTo(body.Length>0?body:null));
                Assert.That(shown.Scarf?.Id,Is.EqualTo(scarf.Length>0?scarf:null));
                Assert.That(shown.ForGameplay.IsEmpty,Is.True);
                Assert.That(HeroGearArt.Layer(Family.Bloom,shown,0)!.Value.Name,Does.StartWith("bloomv2-base-"));
                Assert.That(HeroGearArt.Layer(Family.Bloom,shown,1).HasValue,Is.EqualTo(body.Length>0));
                Assert.That(HeroGearArt.Layer(Family.Bloom,shown,2).HasValue,Is.EqualTo(scarf.Length>0));
                Assert.That(HeroGearArt.Layer(Family.Bloom,shown,3).HasValue,Is.EqualTo(glasses));
                looks.Add(string.Join("|",Enumerable.Range(0,HeroGearArt.LayerCount).Select(i=>HeroGearArt.Layer(Family.Bloom,shown,i)?.Name??"")));
                foreach(var kind in CosmeticCatalog.WornKinds)
                {
                    Outfit preview=WardrobeShelfCatalog.Preview(shown,kind,null);
                    Assert.That(preview.Get(kind),Is.Null);
                    foreach(var other in CosmeticCatalog.WornKinds.Where(k=>k!=kind))Assert.That(preview.Get(other),Is.EqualTo(shown.Get(other)));
                }
                loaded.Unequip(Family.Bloom,CosmeticKind.Hat);
                Assert.That(HeroGearArt.Layer(Family.Bloom,loaded.OutfitOf(Family.Bloom),0)!.Value.Name,Is.EqualTo("bloomv2-base-hairpetals"));
                Assert.That(loaded.OutfitOf(Family.Bloom).Clothes?.Id,Is.EqualTo(shown.Clothes?.Id));
                Assert.That(loaded.OutfitOf(Family.Bloom).Scarf?.Id,Is.EqualTo(shown.Scarf?.Id));
            }
            Assert.That(looks.Count,Is.EqualTo(216),"default and explicit petals share the same look; every other combination remains distinct");
            Assert.That(SimpleHeroArt.Frame(Family.Bloom).Name,Is.EqualTo("bloomv2-base-hairpetals"));
            Assert.That(HeroMotion.FrameName(Family.Bloom,MotionClip.Win,0),Is.EqualTo("bloomv2-base-hairpetals"));
        }
    }
}
