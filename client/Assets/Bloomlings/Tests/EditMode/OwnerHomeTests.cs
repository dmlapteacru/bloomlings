using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// Home, the splash and the win over the owner's pictures (spec 005 FR-024, FR-028, contracts/look.md §6.3, §6.4): the
    /// animated heroes on the owner's layered Home, the logo picture sized by width, the splash taking the Home garden, the
    /// win's painted stage under the hero, and the blank-faced twins used only when they match the hero.
    /// </summary>
    public class OwnerHomeTests
    {
        private static readonly (int W, int H, Insets Insets)[] Phones =
        {
            (1080, 2340, new Insets(63f, 110f, 0f, 0f)),
            (1080, 2520, new Insets(63f, 110f, 0f, 0f)),
            (1080, 1920, new Insets(48f, 0f, 0f, 0f)),
        };

        [Test]
        public void OverTheOwnersLayeredHome_HomeAndTheSplashStandTheAnimatedHeroes()
        {
            // The owner's Home layer pack (2026-10-02): over the garden with its fountain layers the animated heroes stand
            // on the fountain; over an owner picture without them (a splash picture of its own) none; without the owner's
            // picture the drawn stand-in keeps its still heroes.
            Assert.That(HeroPictures.StageOf(ownerPicture: true, layered: true), Is.EqualTo(HomeHeroes.Layered));
            Assert.That(HeroPictures.StageOf(ownerPicture: true, layered: false), Is.EqualTo(HomeHeroes.None));
            Assert.That(HeroPictures.StageOf(ownerPicture: false, layered: false), Is.EqualTo(HomeHeroes.Drawn));
            Assert.That(HeroPictures.StageOf(ownerPicture: false, layered: true), Is.EqualTo(HomeHeroes.Drawn));
        }

        [Test]
        public void TheLogoPicture_IsSizedByWidth_UnderSettings()
        {
            foreach ((int w, int h, Insets insets) in Phones)
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
                Box logo = r.LogoPicture(1200, 440);
                Assert.That(logo.Width, Is.EqualTo(ReferenceHomeRegions.LogoPictureShare * r.W).Within(0.5f));
                Assert.That(logo.Height, Is.EqualTo(logo.Width * 440f / 1200f).Within(0.5f));
                Assert.That(logo.CenterX, Is.EqualTo(r.Logo.CenterX).Within(0.5f));
                Assert.That(logo.Top, Is.GreaterThanOrEqualTo(r.Settings.Bottom - (0.1f * logo.Height) - 0.5f));
            }
        }

        [Test]
        public void TheSplash_TakesTheHomeGarden_WhileItsOwnPictureIsMissing()
        {
            Assert.That(OwnerPictures.Resolve(BackdropScene.Splash, string.Empty, name => name == OwnerPictures.Home), Is.EqualTo(OwnerPictures.Home));
            Assert.That(OwnerPictures.Resolve(BackdropScene.Splash, string.Empty, _ => true), Is.EqualTo(OwnerPictures.Splash));
            Assert.That(OwnerPictures.Resolve(BackdropScene.Home, string.Empty, _ => false), Is.EqualTo(OwnerPictures.Home));
            Assert.That(OwnerPictures.Resolve(BackdropScene.Gameplay, "pond", _ => false), Is.EqualTo("gameplay-pond"));
        }

        [Test]
        public void TheWinPicture_ZoomsFromTheTop_SoItsDiscLiesUnderTheHerosFeet()
        {
            foreach ((int w, int h, Insets insets) in Phones)
            {
                var screen = new Box(0f, 0f, w, h);
                WinRegions r = ScreenLayout.WinScreen(w, h, insets);
                float feet = r.Hero.Top + (r.Hero.Height * HomeStage.FeetShare);
                float zoom = OwnerPictures.WinZoom(screen, feet);
                float cover = System.Math.Max(w / 852f, h / 1846f);
                Assert.That(zoom, Is.GreaterThanOrEqualTo(1f));
                float disc = screen.Top + (OwnerPictures.WinDiscShare * 1846f * cover * zoom);
                if (zoom > 1f)
                {
                    Assert.That(disc, Is.EqualTo(feet).Within(1f), w + "x" + h);
                }
                else
                {
                    Assert.That(disc, Is.GreaterThanOrEqualTo(feet - 1f), w + "x" + h);
                }
            }
        }

        [Test]
        public void TheExpressionBadge_StaysOffTheOwnersFaces()
        {
            var picture = new Box(0f, 0f, 512f, 576f);
            Box badge = CharacterArt.ExpressionBadge(picture);
            Assert.That(badge.Width, Is.EqualTo(0.22f * picture.Width).Within(0.01f));
            Assert.That(badge.Right, Is.LessThanOrEqualTo(picture.Right));
            foreach (Family family in CharacterArt.Families)
            {
                (float x, float y) = CharacterArt.FaceCenterHero(family);
                Box face = CharacterArt.ExpressionBox(picture, (x, y));
                bool overlaps = badge.Left < face.Right && face.Left < badge.Right && badge.Top < face.Bottom && face.Top < badge.Bottom;
                Assert.That(overlaps, Is.False, family + ": the badge stays off the face");
            }
        }

        [Test]
        public void TheManifest_TellsWhichBlankTwinsMatchTheirHeroes()
        {
            string manifest = File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings", "Art", "Characters", "Resources", "Characters", "manifest.json"));
            IReadOnlyDictionary<string, string?> sources = CharacterSprites.ManifestSources(manifest);
            Assert.That(sources.ContainsKey(CharacterArt.Hero(Family.Sprig)), Is.True);
            Assert.That(sources.ContainsKey(CharacterArt.Hero(Family.Sprig, blank: true)), Is.True);
            Assert.That(sources.ContainsKey("2d/leaf-happy"), Is.True);
            Assert.That(sources["2d/leaf-happy"], Is.Null, "a generated picture has no source");

            // A worn expression may use the blank twin only when it comes from the same source as the solo hero.
            foreach (Family family in CharacterArt.Families)
            {
                sources.TryGetValue(CharacterArt.Hero(family), out string? hero);
                sources.TryGetValue(CharacterArt.Hero(family, blank: true), out string? blank);
                if (hero == "owner" && blank != "owner")
                {
                    Assert.That(hero, Is.Not.EqualTo(blank), family + ": the generated blank is another design");
                }
            }

            const string sample = "{ \"files\": [ { \"path\": \"3d/sprig.png\", \"source\": \"owner\" }, { \"path\": \"3d/sprig-blank.png\", \"width\": 512 } ] }";
            IReadOnlyDictionary<string, string?> parsed = CharacterSprites.ManifestSources(sample);
            Assert.That(parsed["3d/sprig"], Is.EqualTo("owner"));
            Assert.That(parsed["3d/sprig-blank"], Is.Null);
        }
    }
}
