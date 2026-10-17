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
    public class HomeHeroArtTests
    {
        [TestCase(Family.Sprig)]
        [TestCase(Family.Bloom)]
        [TestCase(Family.Drop)]
        [TestCase(Family.Twig)]
        public void EveryFittedOutfitHasAllItsHomeLayersWithoutChangingTheSavedLook(Family family)
        {
            var catalog = CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));
            var save = PlayerSave.CreateNew("home-angle",DateTime.UtcNow);
            var wardrobe = new WardrobeService(save,catalog,new BundledRemoteConfigService(),()=>{});
            string[] Choices(CosmeticKind kind) => new[]{""}.Concat(catalog.Items.Where(i=>i.Fits(family)&&i.Kind==kind&&i.MetaOnly&&HeroGearArt.CanPreview(i)).Select(i=>i.Id)).ToArray();
            foreach(string head in Choices(CosmeticKind.Hat))
            foreach(string body in Choices(CosmeticKind.Clothes))
            foreach(string scarf in Choices(CosmeticKind.Scarf))
            foreach(string glasses in Choices(CosmeticKind.Glasses))
            {
                foreach(var choice in new[]{(CosmeticKind.Hat,head),(CosmeticKind.Clothes,body),(CosmeticKind.Scarf,scarf),(CosmeticKind.Glasses,glasses)})
                {
                    if(choice.Item2.Length==0) wardrobe.Unequip(family,choice.Item1);
                    else Assert.That(wardrobe.Equip(family,choice.Item2),Is.True,choice.Item2);
                }
                Outfit outfit = wardrobe.OutfitOf(family);
                string before = SaveSerializer.ToJObject(save).ToString();
                for(int i=0;i<HeroGearArt.LayerCount;i++)
                {
                    HeroFrame? front = HeroGearArt.Layer(family,outfit,i), home = HomeHeroArt.Layer(family,outfit,i);
                    Assert.That(home.HasValue,Is.EqualTo(front.HasValue),family+" slot "+i);
                    if(!home.HasValue) continue;
                    Assert.That(home.Value.Name,Does.StartWith("homehero-"+family.ToString().ToLowerInvariant()+"-"));
                    Assert.That(HomeHeroArt.AllFrames.Any(f=>f.Name==home.Value.Name),Is.True);
                    Assert.That(HeroGearArt.Layer(family,outfit,i)!.Value.Name,Is.EqualTo(front!.Value.Name),"other hosts retain the frontal view");
                }
                Assert.That(SaveSerializer.ToJObject(save).ToString(),Is.EqualTo(before),"view selection cannot change equipment");
            }
        }
    }
}
