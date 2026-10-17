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
        public void RestoredTwig_UsesOriginalAnimatedSet_AndKeepsSprigGear()
        {
            Assert.That(HeroGearArt.Supports(Family.Twig), Is.False);
            Assert.That(HeroGearArt.Layer(Family.Twig, Outfit.None, 0), Is.Null);
            Assert.That(HeroMotion.Frame(Family.Twig, MotionClip.Idle, 0).Name, Does.StartWith("twig-idle-"));
            Assert.That(HeroMotion.Frame(Family.Twig, MotionClip.Win, 0).Name, Does.StartWith("twig-win-"));
            Assert.That(HeroMotion.FrameCount(Family.Twig, MotionClip.Idle), Is.EqualTo(72));
            Assert.That(HeroGearArt.Supports(Family.Sprig), Is.True);
            Assert.That(HeroGearArt.Layer(Family.Sprig, Outfit.None, 0), Is.Not.Null);
        }

        [TestCase(TwigGearArt.AcornHatId)]
        [TestCase(TwigGearArt.FlowerCapId)]
        public void RetiredPilotHat_DoesNotOverrideRestoredTwig_OrEraseTheSave(string id)
        {
            var save = PlayerSave.CreateNew("twig", DateTime.UtcNow);
            save.Cosmetics.Equipped["twig.hat"] = id;
            var restored = SaveSerializer.Read(SaveSerializer.ToJObject(save));
            var wardrobe = new WardrobeService(restored, Catalog, new BundledRemoteConfigService(), () => {});
            Assert.That(wardrobe.ShopItems.Any(i => i.Id == id), Is.False);
            Assert.That(wardrobe.Equip(Family.Twig, id), Is.False);
            Assert.That(wardrobe.OutfitOf(Family.Twig).Hat, Is.Null);
            Assert.That(restored.Cosmetics.Equipped["twig.hat"], Is.EqualTo(id), "a study rollback must not rewrite player saves");
            Assert.That(HeroGearArt.Icon(id), Is.Null);
            Assert.That(wardrobe.ShopItems.Any(i => i.Id == SprigGearArt.CapId), Is.True);
        }
    }
}
