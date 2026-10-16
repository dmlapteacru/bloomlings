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
    public class TwigGearTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));

        [Test]
        public void FreeHeadwear_SwitchesOneSlot_PersistsAndRemovesToBareBase()
        {
            var save=PlayerSave.CreateNew("twig",DateTime.UtcNow);
            var wardrobe=new WardrobeService(save,Catalog,new BundledRemoteConfigService(),()=>{});
            Assert.That(wardrobe.IsAvailable,Is.False);
            foreach(string id in new[]{TwigGearArt.AcornHatId,TwigGearArt.FlowerCapId})
            {
                Assert.That(wardrobe.ShopItems.Any(i=>i.Id==id),Is.True);
                Assert.That(wardrobe.Equip(Family.Sprig,id),Is.False,"headwear is fitted only to Twig");
                Assert.That(wardrobe.Equip(Family.Twig,id),Is.True);
                Assert.That(wardrobe.OutfitOf(Family.Twig).Hat?.Id,Is.EqualTo(id));
                Assert.That(wardrobe.OutfitOf(Family.Twig).ForGameplay.IsEmpty,Is.True);
            }
            var restored=new WardrobeService(SaveSerializer.Read(SaveSerializer.ToJObject(save)),Catalog,new BundledRemoteConfigService(),()=>{});
            Assert.That(restored.OutfitOf(Family.Twig).Hat?.Id,Is.EqualTo(TwigGearArt.FlowerCapId));
            restored.Unequip(Family.Twig,CosmeticKind.Hat);
            Assert.That(TwigGearArt.Frame(restored.OutfitOf(Family.Twig)).Name,Is.EqualTo("twigmod-base"));
            Assert.That(WardrobeShelfCatalog.Build(restored,CosmeticKind.Hat,Family.Bloom).Any(i=>TwigGearArt.IsHat(i.Item?.Id)),Is.False);
        }

        [TestCase(TwigGearArt.AcornHatId,"twigmod-acorn")]
        [TestCase(TwigGearArt.FlowerCapId,"twigmod-cap")]
        public void SharedRenderer_UsesOneFittedStateAndItsActualShopIcon(string id,string frame)
        {
            var wardrobe=new WardrobeService(PlayerSave.CreateNew("twig",DateTime.UtcNow),Catalog,new BundledRemoteConfigService(),()=>{});
            wardrobe.Equip(Family.Twig,id);
            Outfit look=wardrobe.OutfitOf(Family.Twig);
            Assert.That(HeroGearArt.Layer(Family.Twig,look,0)!.Value.Name,Is.EqualTo(frame));
            Assert.That(HeroGearArt.Layer(Family.Twig,look,1),Is.Null);
            Assert.That(HeroGearArt.Layer(Family.Twig,look,2),Is.Null);
            Assert.That(HeroGearArt.HasBakedHat(Family.Twig,look),Is.True,"a legacy procedural hat must not be drawn over the fitted mesh");
            Assert.That(HeroGearArt.Icon(id),Is.Not.Null);
            Assert.That(HeroGearArt.Supports(Family.Sprig),Is.True,"the existing Sprig path is preserved");
        }
    }
}
