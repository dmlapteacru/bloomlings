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
    public class EyewearArtTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));
        [TestCase(Family.Sprig)]
        [TestCase(Family.Bloom)]
        [TestCase(Family.Twig)]
        public void BothDeliveredFramesAreSeparateFaceChoicesAndPersistWithoutChangingOtherSlots(Family family)
        {
            var save=WardrobeArtInventory.Create("eyewear",DateTime.UtcNow);
            var wardrobe=new WardrobeService(save,Catalog,new BundledRemoteConfigService(),()=>{});
            foreach(var kind in new[]{CosmeticKind.Hat,CosmeticKind.Clothes,CosmeticKind.Scarf})
            {
                var item=WardrobeShelfCatalog.Build(wardrobe,kind,family).First(c=>c.Item!=null).Item!;
                Assert.That(wardrobe.Equip(family,item.Id),Is.True);
            }
            var original=wardrobe.OutfitOf(family);
            var faces=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Glasses,family).Where(c=>c.Item!=null).Select(c=>c.Item!).ToArray();
            Assert.That(faces.Length,Is.EqualTo(2));
            var images=faces.Select(f=>HeroGearArt.Icon(f.Id)!.Value.Name).ToArray();Assert.That(images.Distinct().Count(),Is.EqualTo(2));
            foreach(var face in faces)
            {
                Assert.That((face.Free||face.ForSale)&&face.MetaOnly&&face.Fits(family),Is.True);
                Assert.That(wardrobe.Equip(family,face.Id),Is.True);
                var loaded=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,new BundledRemoteConfigService(),()=>{});
                var outfit=loaded.OutfitOf(family);Assert.That(outfit.Glasses?.Id,Is.EqualTo(face.Id));
                Assert.That(HeroGearArt.DisplayOutfit(outfit)!.Glasses?.Id,Is.EqualTo(face.Id));
                Assert.That(new[]{outfit.Hat?.Id,outfit.Clothes?.Id,outfit.Scarf?.Id},
                    Is.EqualTo(new[]{original.Hat?.Id,original.Clothes?.Id,original.Scarf?.Id}),"Face selection preserves the fitted outfit");
                var front=HeroGearArt.Layer(family,outfit,3);var home=HomeHeroArt.Layer(family,outfit,3);
                Assert.That(front.HasValue&&home.HasValue,Is.True);
                Assert.That(front!.Value.Name,Does.StartWith("wardrobefinish-"+family.ToString().ToLowerInvariant()+"-"));
                Assert.That(home!.Value.Name,Does.StartWith("homehero-"+family.ToString().ToLowerInvariant()+"-finish-"));
                Assert.That(HomeHeroArt.AllFrames.Any(f=>f.Name==home.Value.Name),Is.True);
                loaded.Unequip(family,CosmeticKind.Glasses);
                Assert.That(HeroGearArt.Layer(family,loaded.OutfitOf(family),3),Is.Null);
                Assert.That(HeroGearArt.Layer(family,loaded.OutfitOf(family),0).HasValue,Is.True,"removing glasses never removes the stock Head");
            }
        }
        [Test]
        public void EveryGeneratedEyewearLayerAndIconIsPresentAtItsDeclaredSize()
        {
            string root=Path.Combine(Application.dataPath,"Bloomlings/Art/Heroes/Resources/HeroMotion");
            Assert.That(EyewearArt.ItemIds.Count(),Is.EqualTo(6));
            foreach(var frame in EyewearArt.AllFrames)
            {
                byte[] png=File.ReadAllBytes(Path.Combine(root,frame.Name+".png"));
                int Int32(int i)=>(png[i]<<24)|(png[i+1]<<16)|(png[i+2]<<8)|png[i+3];
                Assert.That(Int32(16),Is.EqualTo(frame.Width),frame.Name);
                Assert.That(Int32(20),Is.EqualTo(frame.Height),frame.Name);
                Assert.That(png[24],Is.EqualTo(8));Assert.That(png[25],Is.EqualTo(6));
            }
        }
    }
}
