using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Editor;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity side of the owner's animated heroes and layered Home (spec 005 FR-028): the frame cache
    /// (<see cref="HeroFrames"/>) mirrors the kit's frames, and every frame and layer picture lies where the loaders look
    /// and the importer (<see cref="HeroMotionImporter"/>) gives it its sprite settings.
    /// </summary>
    public class HeroFramesTests
    {
        private static string ClientRoot => Path.Combine(Application.dataPath, "Bloomlings");

        [Test]
        public void TheFrameCache_MirrorsTheKitsFrames_AndWraps()
        {
            foreach (Family family in CharacterArt.Families)
            {
                HeroFrameSet set = HeroFrames.Of(family);
                Assert.That(HeroFrames.Of(family), Is.SameAs(set), family + ": one set a family");
                Assert.That(set.Baked, Is.EqualTo(HeroMotion.Has(family)), family.ToString());
                foreach (MotionClip clip in new[] { MotionClip.Idle, MotionClip.React })
                {
                    int count = HeroMotion.FrameCount(family, clip);
                    Assert.That(set.Count(clip), Is.EqualTo(count), family + " " + clip);
                    for (int i = 0; i < count; i++)
                    {
                        HeroFrame kit = HeroMotion.Frame(family, clip, i);
                        HeroFrame cached = set.Frame(clip, i);
                        Assert.That(cached.Name, Is.EqualTo(kit.Name));
                        Assert.That((cached.X, cached.Y, cached.Width, cached.Height), Is.EqualTo((kit.X, kit.Y, kit.Width, kit.Height)), kit.Name);
                        Assert.That(cached.Head, Is.EqualTo(kit.Head), kit.Name);
                        Assert.That(cached.Top, Is.EqualTo(kit.Top), kit.Name);
                    }

                    if (count > 0)
                    {
                        Assert.That(set.Frame(clip, count).Name, Is.EqualTo(set.Frame(clip, 0).Name), family + " " + clip + ": the index wraps");
                        Assert.That(set.Frame(clip, -1).Name, Is.EqualTo(set.Frame(clip, count - 1).Name), family + " " + clip + ": and backwards");
                    }
                }
            }
        }

        [Test]
        public void EveryFrame_LiesWhereTheCacheLoadsIt_AndTheImporterTakesIt()
        {
            string folder = Path.Combine(ClientRoot, "Art", "Heroes", "Resources", HeroMotion.Folder);
            Assert.That(HeroMotionImporter.FramesFolder, Is.EqualTo("Assets/Bloomlings/Art/Heroes/Resources/" + HeroMotion.Folder + "/"));
            var expected = new HashSet<string>(HeroMotion.AllFrames());
            Assert.That(expected, Has.Count.EqualTo(576), "four families, a 96-frame idle and a 48-frame reaction each (24 fps)");
            foreach (string name in expected)
            {
                Assert.That(File.Exists(Path.Combine(folder, name + ".png")), Is.True, name + ".png");
                Assert.That(HeroMotionImporter.Handles(HeroMotionImporter.FramesFolder + name + ".png"), Is.True, name);
            }

            List<string> pictures = Directory.GetFiles(folder, "*.png").Select(f => Path.GetFileNameWithoutExtension(f) ?? string.Empty).ToList();
            Assert.That(pictures.Where(p => !expected.Contains(p)), Is.Empty, "no stray frame pictures");
        }

        [Test]
        public void EveryHomeLayer_LiesWhereOwnerArtLoadsIt_AndTheImporterTakesItsCutOuts()
        {
            string folder = Path.Combine(ClientRoot, "Art", "Backgrounds", "Resources", OwnerPictures.BackgroundFolder);
            foreach (PictureBox layer in HomeLayers.All)
            {
                bool garden = layer.Name == HomeLayers.Back.Name;
                string file = layer.Name + (garden ? ".jpg" : ".png");
                Assert.That(File.Exists(Path.Combine(folder, file)), Is.True, file);
                string asset = "Assets/Bloomlings/Art/Backgrounds/Resources/" + OwnerPictures.BackgroundFolder + "/" + file;
                Assert.That(HeroMotionImporter.Handles(asset), Is.EqualTo(!garden), file + (garden ? " keeps the backgrounds' import" : " is a sprite layer"));
            }

            Assert.That(HeroMotionImporter.Handles("Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/gameplay-pond.jpg"), Is.False);
            Assert.That(HeroMotionImporter.Handles("Assets/Bloomlings/Art/Icons/Resources/Icons/booster-shuffle.png"), Is.False);
        }
    }
}
