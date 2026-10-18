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
    public class SprigV2Tests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));

        [Test]
        public void AllDeliveredItemsAreFittedAndListedOnlyInTheirOwnCategory()
        {
            var wardrobe = new WardrobeService(WardrobeArtInventory.Create("sprig-v2",DateTime.UtcNow),Catalog,new BundledRemoteConfigService(),()=>{});
            Assert.That(CosmeticCatalog.WornKinds, Is.EqualTo(new[]{CosmeticKind.Hat,CosmeticKind.Glasses,CosmeticKind.Clothes,CosmeticKind.Scarf}));
            foreach (string id in SprigV2Art.ItemIds)
            {
                Assert.That(Catalog.TryGet(id,out var item),Is.True,id);
                Assert.That((item!.Free || item.ForSale) && item.MetaOnly && HeroGearArt.CanPreview(item),Is.True,id);
                foreach(var kind in CosmeticCatalog.WornKinds)
                    Assert.That(WardrobeShelfCatalog.Build(wardrobe,kind,Family.Sprig).Any(c=>c.Item?.Id==id),Is.EqualTo(item.Kind==kind&&!item.DefaultLook),id+" "+kind);
                Assert.That(wardrobe.Equip(Family.Twig,id),Is.False);
                Assert.That(SprigV2Art.Icon(id),Is.Not.Null,id);
            }
        }

        [Test]
        public void All144SavedOutfitsReloadWithTheMatchingMasksAndDefaultAffectsOnlyOneSlot()
        {
            foreach(string head in new[]{""}.Concat(SprigV2Art.HeadIds))
            foreach(string body in new[]{""}.Concat(SprigV2Art.ClothesIds))
            foreach(bool scarf in new[]{false,true})
            foreach(bool glasses in new[]{false,true})
            {
                var save=WardrobeArtInventory.Create("sprig-combo",DateTime.UtcNow);
                var config=new BundledRemoteConfigService();
                var wardrobe=new WardrobeService(save,Catalog,config,()=>{});
                foreach(string id in new[]{head,body,scarf?SprigV2Art.ScarfId:"",glasses?SprigV2Art.GlassesId:""})
                    if(id.Length>0)Assert.That(wardrobe.Equip(Family.Sprig,id),Is.True,id);
                var loaded=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,config,()=>{});
                Outfit outfit=loaded.OutfitOf(Family.Sprig),shown=HeroGearArt.DisplayOutfit(outfit)!;
                Assert.That(shown.Hat?.Id,Is.EqualTo(head.Length>0?head:null));
                Assert.That(shown.Clothes?.Id,Is.EqualTo(body.Length>0?body:null));
                Assert.That(shown.ForGameplay.IsEmpty,Is.True);
                Assert.That(HeroGearArt.Layer(Family.Sprig,shown,0)!.Value.Name,Does.StartWith("sprigv2-base-"));
                Assert.That(HeroGearArt.Layer(Family.Sprig,shown,1).HasValue,Is.EqualTo(body.Length>0));
                Assert.That(HeroGearArt.Layer(Family.Sprig,shown,2).HasValue,Is.EqualTo(scarf));
                Assert.That(HeroGearArt.Layer(Family.Sprig,shown,3).HasValue,Is.EqualTo(glasses));
                foreach(var kind in CosmeticCatalog.WornKinds)
                {
                    Outfit preview=WardrobeShelfCatalog.Preview(shown,kind,null);
                    Assert.That(preview.Get(kind),Is.Null);
                    foreach(var other in CosmeticCatalog.WornKinds.Where(k=>k!=kind))Assert.That(preview.Get(other),Is.EqualTo(shown.Get(other)));
                }
                loaded.Unequip(Family.Sprig,CosmeticKind.Hat);
                Assert.That(HeroGearArt.Layer(Family.Sprig,loaded.OutfitOf(Family.Sprig),0)!.Value.Name,Is.EqualTo("sprigv2-base-hairleafy"));
                Assert.That(loaded.OutfitOf(Family.Sprig).Clothes?.Id,Is.EqualTo(shown.Clothes?.Id));
            }
            Assert.That(SimpleHeroArt.Frame(Family.Sprig).Name,Is.EqualTo("sprigv2-base-hairleafy"));
            Assert.That(HeroMotion.FrameName(Family.Sprig,MotionClip.Win,0),Is.EqualTo("sprigv2-base-hairleafy"));
        }
    }
}
