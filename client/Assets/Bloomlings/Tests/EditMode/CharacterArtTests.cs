using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>The character art kit of spec 004 (contracts/hosts.md "Shared kit", data-model.md).</summary>
    public class CharacterArtTests
    {
        private static IEnumerable<VariantInfo> Launch => VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Launch);

        [Test]
        public void TheOwnersBackgrounds_AreNamedAsThePictureListSays_AndHaveTheirSlots()
        {
            // Spec 005 pictures.md B: one gameplay picture per theme, Home and the splash.
            var names = Gameplay.Themes.ThemeRotation.Default.Themes.Select(t => OwnerPictures.Gameplay(t.Id)).ToList();
            Assert.That(names, Is.EquivalentTo(new[] { "gameplay-daylight", "gameplay-pond", "gameplay-orchard", "gameplay-moonlit" }));
            foreach (Gameplay.Themes.BackgroundTheme theme in Gameplay.Themes.ThemeRotation.Default.Themes)
            {
                Assert.That(AssetSlots.Has("bg.theme." + theme.Id), Is.True, theme.Id);
            }

            Assert.That(OwnerPictures.Background(BackdropScene.Home, "pond"), Is.EqualTo(OwnerPictures.Home));
            Assert.That(OwnerPictures.Background(BackdropScene.Splash, "pond"), Is.EqualTo(OwnerPictures.Splash));
            Assert.That(OwnerPictures.Background(BackdropScene.Gameplay, "moonlit_garden"), Is.EqualTo("gameplay-moonlit"));

            // Every picture of sections B and C fills a registered slot (FR-019): the themes', Home, the splash, the
            // Wardrobe, the logo and the tagline.
            foreach (Gameplay.Themes.BackgroundTheme theme in Gameplay.Themes.ThemeRotation.Default.Themes)
            {
                Assert.That(OwnerPictures.SlotOf(OwnerPictures.Gameplay(theme.Id)), Is.EqualTo("bg.theme." + theme.Id), theme.Id);
            }

            foreach (string picture in new[] { OwnerPictures.Home, OwnerPictures.Splash, OwnerPictures.Wardrobe, OwnerPictures.Logo, OwnerPictures.Tagline })
            {
                Assert.That(AssetSlots.Has(OwnerPictures.SlotOf(picture)), Is.True, picture);
            }

            Assert.That(OwnerPictures.SlotOf(OwnerPictures.Logo), Is.EqualTo("brand.wordmark"));
            Assert.That(OwnerPictures.Background(BackdropScene.Win, "pond"), Is.EqualTo(OwnerPictures.Win));
            Assert.That(OwnerPictures.SlotOf(OwnerPictures.Win), Is.EqualTo("bg.win"));
            Assert.That(AssetSlots.Has("bg.win"), Is.True);
        }

        [Test]
        public void TheOwnersIconsAndLeaves_AreNamedAsThePictureListSays_AndHaveTheirSlots()
        {
            // Spec 005 pictures.md D (FR-027): booster-{id} in the Icons folder, the leaves in the Decor folder.
            Assert.That(OwnerPictures.Boosters.Select(OwnerPictures.BoosterIcon), Is.EquivalentTo(new[] { "booster-extra_slot", "booster-shuffle", "booster-return", "booster-bloom_burst" }));
            Assert.That(OwnerPictures.Decor, Is.EquivalentTo(new[] { "ivy", "flowers", "button-leaves", "logo-leaves" }));
            foreach (string booster in OwnerPictures.Boosters)
            {
                Assert.That(OwnerPictures.SlotOf(OwnerPictures.BoosterIcon(booster)), Is.EqualTo("booster." + booster));
                Assert.That(AssetSlots.Has("booster." + booster), Is.True, booster);

                // Both kits recognize a booster's drawn icon, so they draw the owner's picture in its place.
                Assert.That(GardenLook.BoosterOf(GardenLook.BoosterIcon(booster)), Is.EqualTo(booster));
            }

            Assert.That(GardenLook.BoosterOf(GardenLook.Lotus), Is.Null);
            Assert.That(OwnerPictures.SlotOf(OwnerPictures.Ivy), Is.EqualTo("ui.sign.ivy"));
            Assert.That(OwnerPictures.SlotOf(OwnerPictures.Flowers), Is.EqualTo("ui.sign.flowers"));
            Assert.That(OwnerPictures.SlotOf(OwnerPictures.ButtonLeaves), Is.EqualTo("ui.deco.garden"));
            Assert.That(OwnerPictures.SlotOf(OwnerPictures.LogoLeaves), Is.EqualTo("ui.logo.wood"));
            foreach (string picture in OwnerPictures.Decor)
            {
                Assert.That(AssetSlots.Has(OwnerPictures.SlotOf(picture)), Is.True, picture);
            }
        }

        [Test]
        public void EveryVariantMoodAndFamily_HasOnePictureName()
        {
            IReadOnlyList<string> all = CharacterArt.AllPictures(VariantCatalog.Default.All);
            Assert.That(all.Count, Is.EqualTo((VariantCatalog.Default.Count * 4) + (4 * 2) + 1));
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Count));
            Assert.That(all, Does.Contain("2d/leaf-happy"));
            Assert.That(all, Does.Contain("2d/bud-asleep"));
            Assert.That(all, Does.Contain("3d/bloom-blank"));
            Assert.That(all, Does.Contain(CharacterArt.Group));
            foreach (string name in all)
            {
                Assert.That(AssetSlots.Find(CharacterArt.SlotOf(name))?.Kind, Is.EqualTo(PlaceholderKind.Generated), name);
            }
        }

        [Test]
        public void EveryPicture_IsInTheResourcesFolder()
        {
            string folder = Path.Combine(Application.dataPath, "Bloomlings", "Art", "Characters", "Resources", "Characters");
            foreach (string name in CharacterArt.AllPictures(VariantCatalog.Default.All))
            {
                Assert.That(File.Exists(Path.Combine(folder, name + ".png")), Is.True, name + " (run tools/artgen build)");
            }
        }

        [Test]
        public void Count_ReachesFourAndAHalfToOne_OnEveryCardFace()
        {
            // FR-008, SC-005: exposed and queued pods, working and stuck slots.
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                Rgba color = Rgba.FromHex(variant.ColorHex);
                Rgba[] faces =
                {
                    DesignTokens.PodCard(color),
                    DesignTokens.PodCard(DesignTokens.PodQueued(color)),
                    DesignTokens.PodCard(color).Grey(),
                };
                foreach (Rgba face in faces)
                {
                    Assert.That(Rgba.Contrast(CharacterArt.CountColor, face), Is.GreaterThanOrEqualTo(4.5), variant.IconId);
                }
            }

            Assert.That(CharacterArt.CountLook.Outline, Is.EqualTo(Rgba.White));
            Assert.That(CharacterArt.CountLook.OutlineEm, Is.EqualTo(CharacterArt.CountOutlineEm));
        }

        [Test]
        public void BoardTileTints_DifferPairwise()
        {
            // FR-013: the light tiles still tell the variants apart around the characters.
            Rgba[] tints = Launch.Select(v => DesignTokens.CharacterTile(Rgba.FromHex(v.ColorHex))).ToArray();
            for (int a = 0; a < tints.Length; a++)
            {
                for (int b = a + 1; b < tints.Length; b++)
                {
                    float dr = tints[a].R - tints[b].R;
                    float dg = tints[a].G - tints[b].G;
                    float db = tints[a].B - tints[b].B;
                    Assert.That(System.Math.Sqrt((dr * dr) + (dg * dg) + (db * db)), Is.GreaterThanOrEqualTo(20.0), a + " / " + b);
                }
            }
        }

        [Test]
        public void CharacterBoxes_StayInsideTheirFaces()
        {
            foreach ((float w, float h) in new[] { (100f, 100f), (100f, 90f), (100f, 120f), (240f, 200f) })
            {
                var face = new Box(10f, 20f, 10f + w, 20f + h);
                Assert.That(CharacterArt.OnCard(face).Within(face), Is.True, $"card {w}x{h}");
                Assert.That(CharacterArt.OnTile(face).Within(face), Is.True, $"tile {w}x{h}");
                Assert.That(CharacterArt.CountBox(face).Within(face), Is.True, $"count {w}x{h}");
            }
        }

        [Test]
        public void Faces_LieInsideThePictures()
        {
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                (float x, float y) = CharacterArt.FaceCenter2D(variant.IconId);
                Assert.That(x > 0.2f && x < 0.8f && y > 0.2f && y < 0.9f, Is.True, variant.IconId);
            }

            foreach (Family family in CharacterArt.Families)
            {
                (float x, float y) = CharacterArt.FaceCenterHero(family);
                Assert.That(x > 0.2f && x < 0.8f && y > 0.1f && y < 0.9f, Is.True, family.ToString());
            }
        }

        [Test]
        public void Slots_ReplaceTheCodeDrawnFaceAndAccent()
        {
            Assert.That(AssetSlots.Has("char.face"), Is.False);
            Assert.That(AssetSlots.Has("char.accent"), Is.False);
            foreach (VariantInfo variant in VariantCatalog.Default.All)
            {
                AssetSlot? slot = AssetSlots.Find(CharacterArt.Slot2D(variant.IconId));
                Assert.That(slot, Is.Not.Null, variant.IconId);
                Assert.That(slot!.Readability, Is.True, variant.IconId);
                Assert.That(slot.Priority, Is.EqualTo(variant.Status == VariantStatus.Launch ? AssetPriority.Launch : AssetPriority.Later), variant.IconId);
            }

            foreach (Family family in CharacterArt.Families)
            {
                Assert.That(AssetSlots.Find(CharacterArt.HeroSlot(family))?.Kind, Is.EqualTo(PlaceholderKind.Generated), family.ToString());

                // The family silhouettes stay: they are the fallback when a picture is missing (FR-021).
                Assert.That(ShapeLibrary.Has(ShapeLibrary.SilhouetteId(family)), Is.True, family.ToString());
            }

            Assert.That(AssetSlots.Find("char.hero.home")?.Kind, Is.EqualTo(PlaceholderKind.Generated));
            Assert.That(AssetSlots.Find("brand.splash_art")?.Kind, Is.EqualTo(PlaceholderKind.Generated));
        }

        [Test]
        public void GroupOnCard_StandsOnTheTopEdge_OrStaysAway()
        {
            var safe = new Box(0f, 100f, 1080f, 2300f);
            Box? group = CharacterArt.GroupOnCard(new Box(80f, 900f, 1000f, 1800f), safe, 1f, 0.9f);
            Assert.That(group.HasValue, Is.True);
            Assert.That(group!.Value.Bottom, Is.EqualTo(918f).Within(0.01f));
            Assert.That(group.Value.Top, Is.GreaterThanOrEqualTo(safe.Top));
            Assert.That(CharacterArt.GroupOnCard(new Box(80f, 200f, 1000f, 1800f), safe, 1f, 0.9f).HasValue, Is.False);
        }
    }
}
