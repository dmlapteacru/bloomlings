using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    public class AcceptedHeroTests
    {
        [Test]
        public void RuntimeHeroResourcesContainOnlyTheAcceptedQuartetHomeViewsAndRegisteredEyewear()
        {
            string dir=Path.Combine(Application.dataPath,"Bloomlings/Art/Heroes/Resources/HeroMotion");
            string[] allowed={"sprigv2-","bloomv2-","dropv2-","twigv2-","homehero-"};
            Assert.That(Directory.GetFiles(dir,"*.png").Select(Path.GetFileName).Where(n=>!allowed.Any(p=>n!.StartsWith(p,System.StringComparison.Ordinal))&&!EyewearArt.AllFrames.Any(f=>f.Name+".png"==n)&&!BloomExtrasArt.AllFrames.Any(f=>f.Name+".png"==n)&&!WardrobeAdditionsArt.AllFrames.Any(f=>f.Name+".png"==n)),Is.Empty);
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..",".."));
            foreach(string file in new[]{"heroes.glb","sprig-lab.glb","sprig-plant-study.glb","sprig-outfit-study.glb"})
                Assert.That(File.Exists(Path.Combine(root,"tools/heroanim/models",file)),Is.False,"retired model "+file);
            Assert.That(HeroGearArt.Icon("backpack.sprig_leaf"),Is.Null,"no legacy mesh icon can resurrect a retired hero");
        }

        [Test]
        public void AllFourDefaultAndBlankIdentitiesAreCurrentWhileProfileChoicesKeepTheirIds()
        {
            foreach(var family in CharacterArt.Families)
            {
                Assert.That(SimpleHeroArt.Frame(family).Name,Does.StartWith(family.ToString().ToLowerInvariant()+"v2-"));
                Assert.That(CharacterArt.OwnerBlanks.Contains(family),Is.True,"the fallback blank must match the accepted identity");
            }
            Assert.That(AvatarCatalog.All.Select(a=>a.Id),Is.EquivalentTo(new[]{
                "avatar.sprig_default","avatar.bloom_default","avatar.drop_default","avatar.twig_default",
                "avatar.sprig_ladybug_watercolor","avatar.bloom_ribbon_plush","avatar.drop_sailor_sticker","avatar.twig_glasses_scarf_clay",
                "avatar.sprig_dewdrop_papercut","avatar.sprig_flower_crown_3d","avatar.drop_bubble_shell_3d","avatar.twig_autumn_wreath_3d",
                "avatar.bloom_pearl_tiara_3d","avatar.bloom_jeweled_crown_magic"}),"art replacement must not reset purchased avatar choices");
        }
    }
}
