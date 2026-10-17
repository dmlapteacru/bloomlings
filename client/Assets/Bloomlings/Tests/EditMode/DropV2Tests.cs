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
    public class DropV2Tests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));

        [Test]
        public void AllDeliveredItemsAreFreeFittedAndListedOnlyInTheirOwnCategory()
        {
            var wardrobe = new WardrobeService(PlayerSave.CreateNew("drop-v2",DateTime.UtcNow),Catalog,new BundledRemoteConfigService(),()=>{});
            Assert.That(CosmeticCatalog.WornKinds, Is.EqualTo(new[]{CosmeticKind.Hat,CosmeticKind.Glasses,CosmeticKind.Clothes,CosmeticKind.Scarf}));
            foreach (string id in DropV2Art.ItemIds)
            {
                Assert.That(Catalog.TryGet(id,out var item),Is.True,id);
                Assert.That(item!.Free && item.MetaOnly && HeroGearArt.CanPreview(item),Is.True,id);
                foreach(var kind in CosmeticCatalog.WornKinds)
                    Assert.That(WardrobeShelfCatalog.Build(wardrobe,kind,Family.Drop).Any(c=>c.Item?.Id==id),Is.EqualTo(item.Kind==kind),id+" "+kind);
                Assert.That(wardrobe.Equip(Family.Twig,id),Is.False);
                Assert.That(DropV2Art.Icon(id),Is.Not.Null,id);
            }
        }

        [Test]
        public void All240SavedOutfitsReloadWithTheMatchingMasksAndDefaultAffectsOnlyOneSlot()
        {
            var looks=new System.Collections.Generic.HashSet<string>();
            foreach(string head in new[]{""}.Concat(DropV2Art.HeadIds))
            foreach(string body in new[]{""}.Concat(DropV2Art.ClothesIds))
            foreach(string scarf in new[]{""}.Concat(DropV2Art.ScarfIds))
            foreach(bool glasses in new[]{false,true})
            {
                var save=PlayerSave.CreateNew("drop-combo",DateTime.UtcNow);
                var config=new BundledRemoteConfigService();
                var wardrobe=new WardrobeService(save,Catalog,config,()=>{});
                foreach(string id in new[]{head,body,scarf,glasses?DropV2Art.GlassesId:""})
                    if(id.Length>0)Assert.That(wardrobe.Equip(Family.Drop,id),Is.True,id);
                var loaded=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,config,()=>{});
                Outfit outfit=loaded.OutfitOf(Family.Drop),shown=HeroGearArt.DisplayOutfit(outfit)!;
                Assert.That(shown.Hat?.Id,Is.EqualTo(head.Length>0?head:null));
                Assert.That(shown.Clothes?.Id,Is.EqualTo(body.Length>0?body:null));
                Assert.That(shown.Scarf?.Id,Is.EqualTo(scarf.Length>0?scarf:null));
                Assert.That(shown.ForGameplay.IsEmpty,Is.True);
                Assert.That(HeroGearArt.Layer(Family.Drop,shown,0)!.Value.Name,Does.StartWith("dropv2-base-"));
                Assert.That(HeroGearArt.Layer(Family.Drop,shown,1).HasValue,Is.EqualTo(body.Length>0));
                Assert.That(HeroGearArt.Layer(Family.Drop,shown,2).HasValue,Is.EqualTo(scarf.Length>0));
                Assert.That(HeroGearArt.Layer(Family.Drop,shown,3).HasValue,Is.EqualTo(glasses));
                looks.Add(string.Join("|",Enumerable.Range(0,HeroGearArt.LayerCount).Select(i=>HeroGearArt.Layer(Family.Drop,shown,i)?.Name??"")));
                foreach(var kind in CosmeticCatalog.WornKinds)
                {
                    Outfit preview=WardrobeShelfCatalog.Preview(shown,kind,null);
                    Assert.That(preview.Get(kind),Is.Null);
                    foreach(var other in CosmeticCatalog.WornKinds.Where(k=>k!=kind))Assert.That(preview.Get(other),Is.EqualTo(shown.Get(other)));
                }
                loaded.Unequip(Family.Drop,CosmeticKind.Hat);
                Assert.That(HeroGearArt.Layer(Family.Drop,loaded.OutfitOf(Family.Drop),0)!.Value.Name,Is.EqualTo("dropv2-base-nativedrop"));
                Assert.That(loaded.OutfitOf(Family.Drop).Clothes?.Id,Is.EqualTo(shown.Clothes?.Id));
                Assert.That(loaded.OutfitOf(Family.Drop).Scarf?.Id,Is.EqualTo(shown.Scarf?.Id));
            }
            Assert.That(looks.Count,Is.EqualTo(216),"default and explicit native droplet share the same look; every other combination remains distinct");
            Assert.That(SimpleHeroArt.Frame(Family.Drop).Name,Is.EqualTo("dropv2-base-nativedrop"));
            Assert.That(HeroMotion.FrameName(Family.Drop,MotionClip.Win,0),Is.EqualTo("dropv2-base-nativedrop"));
        }
    }
}
