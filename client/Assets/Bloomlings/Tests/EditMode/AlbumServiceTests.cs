using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Album;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Content.Json;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Scene Album (spec 007-scene-album): the catalog, the picture → level binding of the published round levels, the
    /// retroactive opening, the shown scenes, the opening rule, the one-time reward, the "!" and the save's <c>album</c>
    /// section with its migration.
    /// </summary>
    public class AlbumServiceTests
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

        private static AlbumCatalog Catalog => AlbumCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings/Meta/Album/Resources/AlbumCatalog.json")));

        private static IRoundLevelSource Published => RoundContentSet.Read(path =>
        {
            string file = Path.Combine(DevContent.RepositoryContentFolder, "game", "rounds", "published", path);
            return File.Exists(file) ? File.ReadAllBytes(file) : null;
        });

        [Test]
        public void Catalog_HoldsTheOwnersTwoScenes_InLayerOrder_BoundByPicture()
        {
            AlbumCatalog catalog = Catalog;
            Assert.That(catalog.Scenes.Select(s => s.Id), Is.EqualTo(new[] { "forest", "airmail" }));
            Assert.That(catalog.Scenes[0].Objects.Select(o => o.Id), Is.EqualTo(new[] { "owl", "hive", "fox", "butterfly" }));
            Assert.That(catalog.Scenes[1].Objects.Select(o => o.Picture), Is.EqualTo(new[] { "P0394", "P1092", "P0013", "P0031" }));
            foreach (AlbumScene scene in catalog.Scenes)
            {
                Assert.That(scene.CanvasWidth, Is.EqualTo(853));
                Assert.That(scene.CanvasHeight, Is.EqualTo(1844));
                foreach (AlbumObject item in scene.Objects)
                {
                    Assert.That(item.X + item.Width, Is.LessThanOrEqualTo(scene.CanvasWidth), item.Id);
                    Assert.That(item.Y + item.Height, Is.LessThanOrEqualTo(scene.CanvasHeight), item.Id);
                }
            }
        }

        [Test]
        public void Catalog_RefusesAPictureThatOpensTwoObjects()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings/Meta/Album/Resources/AlbumCatalog.json"));
            Assert.Throws<ContentFormatException>(() => AlbumCatalog.Parse(json.Replace("\"P0394\"", "\"P0113\"")));
        }

        [Test]
        public void EverySceneAndObjectName_IsInTheEnglishTable()
        {
            Dictionary<string, string> english = Loc.ParseCsv(File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings", "UI", "Localization", "Resources", Loc.EnglishResource + ".csv")));
            var missing = new List<string>();
            foreach (AlbumScene scene in Catalog.Scenes)
            {
                missing.AddRange(new[] { scene.TitleKey }.Concat(scene.Objects.Select(o => o.TitleKey)).Where(k => !english.ContainsKey(k)));
            }

            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void ThePublishedRoundLevels_BindEachObjectToItsLevel_ByPicture()
        {
            Func<string, int?> levels = AlbumService.PictureLevels(Catalog, Published);
            // The owner's pack: forest L7 / L4 / L1 / L17, airmail L54 / L76 / L71 / L96 (scene.json), found by picture code.
            Assert.That(new[] { "P0113", "P0116", "P0001", "P0065" }.Select(levels), Is.EqualTo(new int?[] { 7, 4, 1, 17 }));
            Assert.That(new[] { "P0394", "P1092", "P0013", "P0031" }.Select(levels), Is.EqualTo(new int?[] { 54, 76, 71, 96 }));
            Assert.That(levels("P9999"), Is.Null, "a picture no published level holds opens nothing yet");
        }

        [Test]
        public void TheFirstLevelHoldingAPicture_Opens_AndItsTitleIsNoKey()
        {
            var source = new FakeRounds("P0002", "P0113", "P0113");
            Func<string, int?> levels = AlbumService.PictureLevels(Catalog, source);
            Assert.That(levels("P0113"), Is.EqualTo(2), "the lowest level holding the picture");
            Assert.That(levels("P0001"), Is.Null, "the forest's fox waits until a level holds its picture, whatever its title");
        }

        [Test]
        public void TheAlbum_OpensAtLevel27_AndEarlierWinsCountAtOnce()
        {
            Assert.That(UnlockRoadmap.Default.LevelOf(AlbumService.UnlockId), Is.EqualTo(27));
            Assert.That(BottomNav.UnlockLevel(NavPlace.Album, UnlockRoadmap.Default.LevelOf), Is.EqualTo(27));
            AlbumService album = Service(out PlayerSave save, out _, out ProgressionService progression);
            progression.FastForward(25);
            Assert.That(album.IsUnlocked, Is.False, "Level 26 is before the album");
            Assert.That(album.HasNews, Is.False, "no \"!\" before the album opens");
            progression.FastForward(26);
            Assert.That(album.IsUnlocked, Is.True, "Level 27 opens it");
            AlbumSceneView forest = album.View(album.Shown[0]);
            Assert.That(forest.OpenCount, Is.EqualTo(4), "the wins of Levels 1, 4, 7 and 17 count at once");
            Assert.That(forest.Complete && forest.RewardWaiting, Is.True);
            Assert.That(forest.Objects.All(o => o.New), Is.True, "the first open shows the colour's return for all four");
            Assert.That(album.HasNews, Is.True);
        }

        [Test]
        public void AnObject_OpensOnlyWithItsOwnLevel()
        {
            AlbumService album = Service(out PlayerSave save, out _, out ProgressionService progression);
            progression.FastForward(53);
            AlbumScene airmail = album.Catalog.Find("airmail")!;
            Assert.That(album.View(airmail).OpenCount, Is.Zero);
            progression.FastForward(54);
            AlbumSceneView view = album.View(airmail);
            Assert.That(view.OpenCount, Is.EqualTo(1));
            Assert.That(view.Find("mill")!.Open, Is.True);
            Assert.That(view.Find("airship")!.Open, Is.False);
            Assert.That(view.Find("airship")!.Level, Is.EqualTo(76), "\"Unlocks after Level 76\"");
            Assert.That(album.NextLevel, Is.EqualTo(55), "\"Next level\" is the next available level");
        }

        [Test]
        public void FutureScenes_ShowGradually_ByProgress()
        {
            var catalog = new AlbumCatalog(new[]
            {
                Scene("a", 5),
                Scene("b", 50),
                Scene("c", 200),
            });
            AlbumService album = Service(out PlayerSave save, out _, out _, catalog, AlbumService.PictureLevels(catalog, null));
            save.Progression.HighestCompletedLevel = 10;
            Assert.That(album.Shown.Select(s => s.Id), Is.EqualTo(new[] { "a" }), "Level 11: the scene of Level 50 lies past the window of 30");
            save.Progression.HighestCompletedLevel = 19;
            Assert.That(album.Shown.Select(s => s.Id), Is.EqualTo(new[] { "a", "b" }), "Level 20: within 30 levels");
            save.Progression.HighestCompletedLevel = 170;
            Assert.That(album.Shown.Select(s => s.Id), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void TheAlbumOpens_OnNews_ThenTheLastViewedIncomplete_ThenTheNearestIncomplete()
        {
            AlbumService album = Service(out PlayerSave save, out EconomyService economy, out ProgressionService progression);
            progression.FastForward(68);
            AlbumScene forest = album.Catalog.Find("forest")!;
            AlbumScene airmail = album.Catalog.Find("airmail")!;
            Assert.That(album.OpeningIndex(), Is.EqualTo(0), "the forest's reward waits and its reveals are new");
            album.Viewed(forest);
            Assert.That(album.OpeningIndex(), Is.EqualTo(0), "seen, but its reward still waits");
            album.Claim(forest);
            Assert.That(album.OpeningIndex(), Is.EqualTo(1), "the airmail's windmill is a new reveal");
            album.Viewed(airmail);
            Assert.That(album.HasNews, Is.False);
            album.Viewed(forest);
            Assert.That(album.OpeningIndex(), Is.EqualTo(1), "the last viewed (the forest) is complete: the nearest incomplete");
            progression.FastForward(76);
            Assert.That(album.OpeningIndex(), Is.EqualTo(1), "the airship's and the fox's reveals are new");
            Assert.That(album.NewReveals(airmail), Is.EqualTo(new[] { "airship", "fox" }));
            album.Viewed(airmail);
            Assert.That(album.NewReveals(airmail), Is.Empty);
            Assert.That(save.Album.LastViewed, Is.EqualTo("airmail"));
        }

        [Test]
        public void TheReward_IsClaimedOnce_ThroughTheEconomy_AndOnlyWhenComplete()
        {
            AlbumService album = Service(out PlayerSave save, out EconomyService economy, out ProgressionService progression);
            AlbumScene forest = album.Catalog.Find("forest")!;
            progression.FastForward(16);
            Assert.That(album.Claim(forest), Is.Zero, "the album is closed and the butterfly (L17) not won");
            progression.FastForward(27);
            int before = economy.Petals;
            Assert.That(album.RewardPetals, Is.EqualTo(50), "Remote Config economy.album.sceneReward, 50 by default");
            Assert.That(album.Claim(forest), Is.EqualTo(50));
            Assert.That(economy.Petals, Is.EqualTo(before + 50));
            Assert.That(album.Claim(forest), Is.Zero, "a second claim pays nothing");
            Assert.That(economy.Petals, Is.EqualTo(before + 50));
            Assert.That(album.View(forest).RewardClaimed, Is.True);
            Assert.That(album.Claim(album.Catalog.Find("airmail")!), Is.Zero, "an incomplete scene has no reward to claim");
            Assert.That(save.Album.Claimed, Is.EquivalentTo(new[] { "forest" }));
        }

        [Test]
        public void TheAlbumSection_RoundTrips_AndAnOlderSaveHasNone()
        {
            AlbumService album = Service(out PlayerSave save, out _, out ProgressionService progression);
            JObject before = SaveSerializer.ToJObject(save);
            Assert.That(before["album"], Is.Null, "a player who never opened the album writes no section");
            Assert.That(SaveSerializer.Read(before).Album.IsEmpty, Is.True, "a save without the section reads as an empty album");

            progression.FastForward(30);
            album.Viewed(album.Catalog.Find("forest")!);
            album.Claim(album.Catalog.Find("forest")!);
            JObject written = SaveSerializer.ToJObject(save);
            Assert.That((int)written["album"]!["version"]!, Is.EqualTo(AlbumData.CurrentVersion));
            PlayerSave read = SaveSerializer.Read(written);
            Assert.That(read.Album.Claimed, Is.EquivalentTo(new[] { "forest" }));
            Assert.That(read.Album.Seen["forest"], Is.EquivalentTo(new[] { "owl", "hive", "fox", "butterfly" }));
            Assert.That(read.Album.LastViewed, Is.EqualTo("forest"));
            Assert.That(SaveSerializer.Write(read), Is.EqualTo(SaveSerializer.Write(save)), "canonical round trip");
        }

        [Test]
        public void ANewerAlbumSection_IsRefused_AndAnUnknownFieldToo()
        {
            AlbumService album = Service(out PlayerSave save, out _, out ProgressionService progression);
            progression.FastForward(30);
            album.Viewed(album.Catalog.Find("forest")!);
            JObject written = SaveSerializer.ToJObject(save);
            written["album"]!["version"] = AlbumData.CurrentVersion + 1;
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(written));
            written = SaveSerializer.ToJObject(save);
            written["album"]!["extra"] = true;
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(written));
        }

        [Test]
        public void AMergedSave_KeepsEveryClaimAndReveal_SoNothingPaysTwice()
        {
            PlayerSave local = PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", Today);
            PlayerSave remote = PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", Today.AddMinutes(1));
            remote.Progression.HighestCompletedLevel = 40;
            local.Album.Claimed.Add("forest");
            local.Album.MarkSeen("forest", "owl");
            remote.Album.MarkSeen("forest", "fox");
            remote.Album.LastViewed = "airmail";
            PlayerSave merged = SaveMerge.Merge(local, remote);
            Assert.That(merged.Album.Claimed, Is.EquivalentTo(new[] { "forest" }));
            Assert.That(merged.Album.Seen["forest"], Is.EquivalentTo(new[] { "owl", "fox" }));
            Assert.That(merged.Album.LastViewed, Is.EqualTo("airmail"));
        }

        [Test]
        public void TheBottomMenu_ShowsTheAlbum_InTheCollectionsPlace()
        {
            Assert.That(BottomNav.Order, Is.EqualTo(new[] { NavPlace.Shop, NavPlace.Wardrobe, NavPlace.Home, NavPlace.Leaderboard, NavPlace.Album }));
            Assert.That(BottomNav.Order, Does.Not.Contain(NavPlace.Collection), "the Collection is switched off (spec 007)");
            Assert.That(BottomNav.IsOpen(NavPlace.Album, HomeLook.Early), Is.False);
            Assert.That(BottomNav.IsOpen(NavPlace.Album, HomeLook.Early with { Album = true }), Is.True);
            Assert.That(OwnerPictures.NavIcon(NavPlace.Album), Is.EqualTo("nav-album"));
            Assert.That(BottomNav.Slot(NavPlace.Album), Is.EqualTo("icon.nav.album"));
            Assert.That(AssetSlots.Has("icon.nav.album"), Is.True);
        }

        [Test]
        public void ATap_ChoosesTheTopmostOpaqueLayer_NeverATransparentArea()
        {
            // Two 2×2 layers on the same box: the back one opaque everywhere, the front one only in its top-left pixel.
            var boxes = new[] { new Box(0f, 0f, 20f, 20f), new Box(0f, 0f, 20f, 20f) };
            (byte[] Alpha, int Width, int Height)? Alpha(int i) => i == 0 ? (new byte[] { 255, 255, 255, 255 }, 2, 2) : (new byte[] { 255, 0, 0, 0 }, 2, 2);
            Assert.That(AlbumLook.HitObject(boxes, Alpha, 5f, 5f), Is.EqualTo(1), "the front layer where it is opaque");
            Assert.That(AlbumLook.HitObject(boxes, Alpha, 15f, 15f), Is.EqualTo(0), "the back layer through the front one's transparent area");
            Assert.That(AlbumLook.HitObject(new[] { boxes[1] }, i => Alpha(1), 15f, 15f), Is.EqualTo(-1), "a transparent area chooses nothing");
            Assert.That(AlbumLook.HitObject(boxes, Alpha, 25f, 5f), Is.EqualTo(-1), "outside every layer");
        }

        [Test]
        public void ALockedLayer_IsGreyAt82Percent_WithItsAlphaKept()
        {
            var rgba = new byte[] { 255, 0, 0, 200, 255, 255, 255, 255 };
            AlbumLook.LockedPixels(rgba);
            Assert.That(rgba[0], Is.EqualTo(rgba[1]).And.EqualTo(rgba[2]));
            Assert.That(rgba[0], Is.EqualTo((byte)Math.Round(76 * 0.82)));
            Assert.That(rgba[3], Is.EqualTo(200));
            Assert.That(rgba[4], Is.EqualTo((byte)Math.Round(255 * 0.82)));
        }

        [Test]
        public void TheSheet_LiftsTheCard_OnlyWhenItWouldCoverTheObject()
        {
            AlbumRegions r = AlbumLook.Page(1080f, 2340f, new Insets(110f, 63f), 853, 1844);
            AlbumSheetRegions s = AlbumLook.Sheet(1080f, 2340f, new Insets(110f, 63f));
            Assert.That((2340f - s.Top) / 2340f, Is.InRange(0.30f, 0.36f), "about a third of the screen (30–35 %)");
            Assert.That(AlbumLook.Lift(r.Card, r.Card.Top + 100f, s.Top, r.W), Is.EqualTo(1f), "an object high in the scene stays");
            float lift = AlbumLook.Lift(r.Card, r.Scene.Bottom, s.Top, r.W);
            Assert.That(lift, Is.LessThan(1f));
            Assert.That(r.Card.Top + ((r.Scene.Bottom - r.Card.Top) * lift), Is.LessThanOrEqualTo(s.Top), "the object's bottom clears the sheet");
            Assert.That(r.Scene.Width / r.Scene.Height, Is.EqualTo(853f / 1844f).Within(0.001f), "the scene is contain-fitted, never cropped");
        }

        private static AlbumScene Scene(string id, int level) => new AlbumScene(id, "album.scene." + id, 10, 20, id + "-background", new[]
        {
            new AlbumObject("a", "t", "P" + (level * 3).ToString("0000"), level, id + "-a", 0, 0, 1, 1),
            new AlbumObject("b", "t", "P" + ((level * 3) + 1).ToString("0000"), level + 1, id + "-b", 0, 0, 1, 1),
            new AlbumObject("c", "t", "P" + ((level * 3) + 2).ToString("0000"), level + 2, id + "-c", 0, 0, 1, 1),
        });

        private static AlbumService Service(out PlayerSave save, out EconomyService economy, out ProgressionService progression, AlbumCatalog? catalog = null, Func<string, int?>? levels = null)
        {
            save = PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", Today);
            economy = new EconomyService(save, EconomyConfig.Bundled, () => { });
            progression = new ProgressionService(save, UnlockRoadmap.Default, () => { });
            catalog ??= Catalog;
            return new AlbumService(save, catalog, levels ?? AlbumService.PictureLevels(catalog, Published), new BundledRemoteConfigService(), economy, () => { });
        }

        /// <summary>A round level source whose levels 1..N hold the given pictures.</summary>
        private sealed class FakeRounds : IRoundLevelSource
        {
            private readonly string[] _pictures;

            public FakeRounds(params string[] pictures)
            {
                _pictures = pictures;
            }

            public int ContentVersion => 1;

            public int LevelCount => _pictures.Length;

            public bool Has(int levelNumber) => levelNumber >= 1 && levelNumber <= LevelCount;

            public RoundManifestLevel Listed(int levelNumber) => new RoundManifestLevel(levelNumber, 1, RoundClass.Normal, _pictures[levelNumber - 1], 1);

            public RoundLevel GetLevel(int levelNumber) => throw new NotSupportedException();

            public RoundPicture GetPicture(RoundLevel level) => throw new NotSupportedException();
        }
    }
}
