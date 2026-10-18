using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>The Wardrobe (FR-063, T143), the Collection (FR-065, T145) and the theme rotation (FR-066, T146).</summary>
    public class WardrobeCollectionThemeTests
    {
        private static string CatalogPath => Path.Combine(Application.dataPath, "Bloomlings", "Meta", "Wardrobe", "Resources", "CosmeticCatalog.json");

        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(CatalogPath));

        [Test]
        public void Wardrobe_GroupsEveryHerosHairBeforeHats_ThenWinterAndNewYear()
        {
            var save=PlayerSave.CreateNew("sorting",DateTime.UtcNow);save.Unlocks.Flags[WardrobeService.UnlockId]=true;
            var config=new BundledRemoteConfigService();var economy=new EconomyService(save,EconomyConfig.From(config),()=>{});
            save.Wallet.AddPetals(20000);
            var wardrobe=new WardrobeService(save,Catalog,config,()=>{},economy);
            var expected=new Dictionary<Family,string[]>
            {
                [Family.Sprig]=new[]{SprigV2Art.RosetteId,SprigV2Art.SwirlId,"hat.sprig_clover","hat.sprig_sprout",SprigV2Art.BucketId,SprigV2Art.CapId,SprigV2Art.BeretId,"hat.sprig_bowler","hat.sprig_boater","hat.sprig_explorer","hat.sprig_headscarf","hat.sprig_nightcap",SprigV2Art.EarmuffsId,SprigV2Art.XmasId},
                [Family.Twig]=new[]{TwigV2Art.HairId,"hat.twig_pinecone",TwigV2Art.BucketId,TwigV2Art.BeretId,"hat.twig_bowler","hat.twig_boater","hat.twig_explorer",TwigV2Art.HatHairId,"hat.twig_mushroom","hat.twig_headscarf","hat.twig_nightcap",TwigV2Art.EarmuffsId,TwigV2Art.XmasId},
                [Family.Bloom]=new[]{BloomV2Art.BobId,BloomV2Art.PeachId,BloomV2Art.RoseId,BloomV2Art.BucketId,"hat.bloom_bucket_green","hat.bloom_summer_panama",BloomV2Art.CapId,BloomV2Art.BeretId,"hat.bloom_boater","hat.bloom_flower_hat","hat.bloom_headscarf","hat.bloom_crown","hat.bloom_nightcap",BloomV2Art.EarmuffsId,BloomV2Art.XmasId},
                [Family.Drop]=new[]{DropV2Art.WaveId,DropV2Art.RipplesId,DropV2Art.WhirlpoolId,"hat.drop_shell",DropV2Art.BucketId,"hat.drop_rain_hat",DropV2Art.CapId,"hat.drop_sailor_hat",DropV2Art.BeretId,"hat.drop_boater","hat.drop_explorer","hat.drop_headscarf","hat.drop_nightcap",DropV2Art.EarmuffsId,DropV2Art.XmasId},
            };
            foreach(var pair in expected)
            {
                var rows=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat,pair.Key);
                Assert.That(rows[0].Item,Is.Null,"Default stays first");
                Assert.That(rows.Where(c=>c.Item?.ForFamily==pair.Key).Select(c=>c.Item!.Id),Is.EqualTo(pair.Value),pair.Key.ToString());
                Assert.That(rows.Where(c=>c.Item!=null).All(c=>!string.IsNullOrEmpty(c.Item!.WardrobeClass)),Is.True,"every visible item has a class");
                var before=rows.Select(c=>c.Item?.Id).ToArray();
                var paid=rows.FirstOrDefault(c=>c.State==WardrobeItemState.ForSale)?.Item;
                if(paid!=null){Assert.That(wardrobe.TryBuy(paid.Id),Is.True);wardrobe.Equip(pair.Key,paid.Id);}
                Assert.That(WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat,pair.Key).Select(c=>c.Item?.Id),Is.EqualTo(before),"buying and equipping do not move items");
                var body=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Clothes,pair.Key).Where(c=>c.Item!=null).Select(c=>c.Item!.Shape);
                Assert.That(body,Is.EqualTo(pair.Key switch { Family.Bloom=>new[]{"overalls","top","dress","coat","jacket"},Family.Sprig=>new[]{"overalls","vest","coat","jacket"},Family.Twig=>new[]{"overalls","top","vest","coat","jacket"},_=>new[]{"overalls","top","top","coat","jacket"}}),"everyday classes, autumn, winter");
            }
        }
        [Test]
        public void WardrobeClassification_IsOptional_AndFutureEventsStayGroupedAndVisible()
        {
            var parsed=CosmeticCatalog.Parse("{\"items\":[{\"id\":\"hat.hair\",\"kind\":\"hat\",\"name\":\"Hair\",\"shape\":\"cap\",\"tint\":\"#999999\",\"wardrobeClass\":\"hair\",\"wardrobeEvent\":\"future_event\"}]}");
            Assert.That(parsed.Items[0].WardrobeClass,Is.EqualTo("hair"));
            Assert.That(parsed.Items[0].WardrobeEvent,Is.EqualTo("future_event"));
            CosmeticItem Item(string id,string? type,string? season=null)=>new CosmeticItem("hat."+id,CosmeticKind.Hat,id,"cap","#999999",false){WardrobeClass=type,WardrobeEvent=season};
            var items=new[]{Item("zcap","cap","z_event"),Item("bcap","cap","b_event"),Item("zhair","hair","z_event"),Item("winter","earmuffs","winter"),Item("cap","cap"),Item("hair","hair"),Item("unknown",null),Item("newyear","holiday_hat","new_year")};
            var wardrobe=new WardrobeService(PlayerSave.CreateNew("sorting",DateTime.UtcNow),new CosmeticCatalog(items),new BundledRemoteConfigService(),()=>{});
            Assert.That(WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat).Skip(1).Select(c=>c.Item!.Id),Is.EqualTo(new[]{"hat.hair","hat.cap","hat.unknown","hat.winter","hat.newyear","hat.bcap","hat.zhair","hat.zcap"}));
        }

        [Test]
        public void Cosmetics_PassTheReadabilityCheck()
        {
            Assert.That(Catalog.ReadabilityProblems(), Is.Empty);
        }

        [Test]
        public void ReadabilityCheck_RejectsAWornVariantLikeColor()
        {
            var catalog = new CosmeticCatalog(new[]
            {
                new CosmeticItem("hat.red_cap", CosmeticKind.Hat, "Red Cap", "cap", "#D0302A", false),
                new CosmeticItem("badge.red", CosmeticKind.Badge, "Red Badge", "badge", "#D0302A", false),
            });

            Assert.That(catalog.ReadabilityProblems().Single(), Does.StartWith("hat.red_cap: worn tint"), "profile items are never drawn on the board");
        }

        [Test]
        public void CatalogRules_FreeItemsAreProfileItems_NeitherGivenNorSold()
        {
            var catalog = new CosmeticCatalog(new[]
            {
                new CosmeticItem("frame.wood_ring", CosmeticKind.Frame, "Wooden Frame", "wood_ring", "#F1CD98", false, Free: true),
                new CosmeticItem("frame.sold", CosmeticKind.Frame, "Sold", "frame", "#F1CD98", false, 100, Free: true),
                new CosmeticItem("frame.given", CosmeticKind.Frame, "Given", "frame", "#F1CD98", true, Free: true),
                new CosmeticItem("hat.free", CosmeticKind.Hat, "Free Hat", "cap", "#A8B59A", false, Free: true),
                new CosmeticItem("frame.unknown", CosmeticKind.Frame, "Unknown", "lace", "#F1CD98", false),
            });

            Assert.That(catalog.ReadabilityProblems(), Is.EqualTo(new[]
            {
                "frame.sold: a free item is everyone's from Level 1, neither given nor sold",
                "frame.given: a free item is everyone's from Level 1, neither given nor sold",
                "hat.free: only profile items are free (the profile opens at Level 1, the Wardrobe later)",
                "frame.unknown: shape 'lace' is not a frame shape",
            }));
            Assert.That(catalog.FreeOf(CosmeticKind.Frame).Select(i => i.Id), Is.EqualTo(new[] { "frame.wood_ring", "frame.sold", "frame.given" }));
            Assert.That(CosmeticCatalog.Parse("{\"items\": [{\"id\": \"frame.wood_ring\", \"kind\": \"frame\", \"name\": \"W\", \"shape\": \"wood_ring\", \"tint\": \"#F1CD98\", \"free\": true}]}").Items.Single().Free, Is.True);
        }

        [Test]
        public void EveryMilestoneItem_IsInTheCatalog()
        {
            CosmeticCatalog catalog = Catalog;
            foreach (string id in MilestoneTable.Default.Cadences.SelectMany(c => c.Items))
            {
                Assert.That(catalog.TryGet(id, out _), Is.True, id);
            }
        }

        [Test]
        public void Wardrobe_OneItemOfEachKindPerFamily_AndStarterItemsOnce()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            int saves = 0;
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => saves++);

            Assert.That(wardrobe.IsAvailable, Is.False, "locked before L40");
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            wardrobe.OnUnlock(WardrobeService.UnlockId);
            wardrobe.OnUnlock(WardrobeService.UnlockId);
            Assert.That(wardrobe.Owned.Where(i => !i.Free), Is.Empty, "retired trial starters are never granted");
            Assert.That(wardrobe.Owned.Count(i=>i.IsWorn&&!i.DefaultLook), Is.EqualTo(12));
            Assert.That(wardrobe.Equip(Family.Drop, "hat.drop_side_wave"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "clothes.drop_overalls"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "glasses.drop_round"), Is.True);
            Assert.That(wardrobe.Equip(Family.Drop, "hat.leaf_cap"), Is.False, "retired and not owned");
            save.Cosmetics.Owned.Add("hat.leaf_cap");
            Assert.That(wardrobe.Equip(Family.Drop, "hat.leaf_cap"), Is.False, "old ownership is retained but cannot restore placeholder art");
            Outfit drop=wardrobe.OutfitOf(Family.Drop);
            Assert.That((drop.Hat?.Id,drop.Clothes?.Id,drop.Glasses?.Id),Is.EqualTo(("hat.drop_side_wave","clothes.drop_overalls","glasses.drop_round")));
            Assert.That(wardrobe.OutfitOf(Family.Sprig).IsEmpty,Is.True);
            wardrobe.Unequip(Family.Drop,CosmeticKind.Hat);
            Assert.That(wardrobe.EquippedFor(Family.Drop,CosmeticKind.Hat),Is.Null);
            Assert.That(wardrobe.EquippedFor(Family.Drop,CosmeticKind.Clothes)!.Id,Is.EqualTo("clothes.drop_overalls"));

            PlayerSave read = SaveSerializer.Read(SaveSerializer.Write(save));
            Assert.That(read.Cosmetics.Equipped, Is.EqualTo(save.Cosmetics.Equipped), "the save schema accepts the slots");
            Assert.That(saves, Is.GreaterThan(0));

            save.Unlocks.Flags.Remove(WardrobeService.UnlockId);
            Assert.That(wardrobe.OutfitOf(Family.Drop).IsEmpty, Is.True, "nothing is worn while the Wardrobe is closed");
        }

        [Test]
        public void EarlySaves_WithOneItemPerFamily_StillRead()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            JObject json = JObject.Parse(SaveSerializer.Write(save));
            json["cosmetics"]!["equipped"] = new JObject { ["drop"] = "hat.leaf_cap", ["sprig"] = "trail.petal_sparkle" };

            PlayerSave read = SaveSerializer.Read(json.ToString());

            Assert.That(read.Cosmetics.Equipped, Is.EqualTo(new SortedDictionary<string, string>
            {
                ["drop.hat"] = "hat.leaf_cap",
                ["sprig.trail"] = "trail.petal_sparkle",
            }));
            json["cosmetics"]!["equipped"] = new JObject { ["drop"] = "frame.daisy" };
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(json.ToString()), "a frame was never worn");
            json["cosmetics"]!["equipped"] = new JObject { ["profile"] = new JObject { ["hat"] = "hat.leaf_cap" } };
            Assert.Throws<ContentFormatException>(() => SaveSerializer.Read(json.ToString()), "the profile shows frames, badges and markers only");
        }

        [Test]
        public void Profile_ShowsTheChosenDecoration_ElseTheNewestOwned()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => { });
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            Assert.That(wardrobe.Profile, Is.EqualTo(ProfileLook.None));

            save.Cosmetics.Owned.Add("frame.daisy");
            save.Cosmetics.Owned.Add("frame.ivy");
            save.Cosmetics.Owned.Add(CosmeticCatalog.LevelMarkerId(750));
            Assert.That(wardrobe.Profile.Frame!.Id, Is.EqualTo("frame.ivy"), "the newest owned frame by default");
            Assert.That(wardrobe.Profile.Marker!.MilestoneLevel, Is.EqualTo(750));

            Assert.That(wardrobe.Show("frame.daisy"), Is.True);
            Assert.That(wardrobe.Show("hat.sprout_cap"), Is.False, "not owned, and not a profile item");
            Assert.That(wardrobe.Profile.Frame!.Id, Is.EqualTo("frame.daisy"));
            Assert.That(save.Cosmetics.Equipped["profile.frame"], Is.EqualTo("frame.daisy"));
            Assert.That(SaveSerializer.Read(SaveSerializer.Write(save)).Cosmetics.Equipped["profile.frame"], Is.EqualTo("frame.daisy"));
        }

        [Test]
        public void StoreCosmetics_AreBoughtWithPetals_OnceTheWardrobeIsOpen()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            var economy = new EconomyService(save, EconomyConfig.From(new BundledRemoteConfigService()), () => { });
            var wardrobe = new WardrobeService(save, Catalog, new BundledRemoteConfigService(), () => { }, economy);
            Catalog.TryGet("hat.sprig_daisy", out var cap);
            CosmeticItem spots=cap!;
            save.Wallet.AddPetals(spots.Price + 10);

            Assert.That(wardrobe.TryBuy(spots.Id), Is.False, "the Wardrobe is not open yet");
            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            save.Progression.HighestCompletedLevel=39;
            Assert.That(wardrobe.TryBuy("hat.leaf_cap"), Is.False, "milestone items are not sold");
            Assert.That(wardrobe.TryBuy(spots.Id), Is.True);
            Assert.That(economy.Petals, Is.EqualTo(10));
            Assert.That(wardrobe.OwnedOf(CosmeticKind.Hat).Select(i => i.Id), Does.Contain(spots.Id));
            Assert.That(wardrobe.ForSale.Select(i => i.Id), Does.Not.Contain(spots.Id));
            Assert.That(wardrobe.TryBuy(spots.Id), Is.False, "owned");

            CosmeticItem stripes = wardrobe.ForSale.Single(i => i.Id == "hat.sprig_panama");
            Assert.That(wardrobe.TryBuy(stripes.Id), Is.False, "Petals are short");
            Assert.That((economy.Petals, save.Cosmetics.Owned.Contains(stripes.Id)), Is.EqualTo((10, false)));
        }

        [Test]
        public void CatalogRules_SoldItemsAreNotMilestoneItems_AndEveryKindHasItems()
        {
            CosmeticCatalog catalog = Catalog;
            var milestoneItems = MilestoneTable.Default.Cadences.SelectMany(c => c.Items).ToHashSet();
            Assert.That(catalog.Items.Where(i => i.ForSale).Select(i => i.Id).Where(milestoneItems.Contains), Is.Empty, "milestone and prestige items are never sold");
            Assert.That(catalog.Items.Where(i => i.Starter).All(i => !i.ForSale), Is.True);
            foreach (CosmeticKind kind in Enum.GetValues(typeof(CosmeticKind)))
            {
                Assert.That(catalog.Items.Any(i => i.Kind == kind), Is.True, kind.ToString());
            }

            Assert.That(catalog.Items.Any(i => i.Kind == CosmeticKind.Skin && i.ForSale), Is.False, "unfitted skins are retired");
            Assert.That(MilestoneTable.Default.Cadences.Where(c => c.Tier == MilestoneTier.Prestige).SelectMany(c => c.Items).Any(id => id.StartsWith("skin.", StringComparison.Ordinal)), Is.False, "no placeholder prestige skins");

            Assert.That(catalog.TryGet("badge.level_350", out CosmeticItem? badge), Is.True);
            Assert.That((badge!.Kind, badge.MilestoneLevel, badge.IsWorn), Is.EqualTo((CosmeticKind.Badge, (int?)350, false)));
            Assert.That(catalog.TryGet("marker.level_1250", out CosmeticItem? marker), Is.True);
            Assert.That(marker!.Kind, Is.EqualTo(CosmeticKind.Marker));
            foreach (string bad in new[] { "badge.level_0", "badge.level_07", "badge.level_", "badge.level_x", "hat.level_50" })
            {
                Assert.That(catalog.TryGet(bad, out _), Is.False, bad);
            }
        }

        [Test]
        public void InkOnVariantColors_IsDarkOnLightVariants_AndAlwaysReadable()
        {
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                Assert.That(CosmeticCatalog.TryParseHex(info.ColorHex, out double r, out double g, out double b), Is.True);
                Assert.That(InkContrast.BestRatio(r, g, b), Is.GreaterThanOrEqualTo(3.0), info.Id.Key + ": icons and counts need 3:1");
                bool dark = InkContrast.UseDarkInk(r, g, b);
                // Spec 005 research D1: the saturated palette makes Moss light enough for the dark ink too.
                if (info.Id == VariantId.Leaf || info.Id == VariantId.Moss || info.Id == VariantId.Flower || info.Id == VariantId.Dew)
                {
                    Assert.That(dark, Is.True, info.Id.Key);
                }
                else if (info.Id == VariantId.VioletBud || info.Id == VariantId.Wood)
                {
                    Assert.That(dark, Is.False, info.Id.Key);
                }
            }
        }

        [Test]
        public void Collection_AddsEachWonLevelOnce()
        {
            ContentSet curated = DevContent.LoadCurated();
            LevelDefinition level = curated.GetLevel(curated.LevelNumbers[0]);
            PlayerSave save = PlayerSave.CreateNew("p1", DateTime.UtcNow);
            var collection = new CollectionService(save, () => { });

            Assert.That(collection.Add(level, 1), Is.True);
            Assert.That(collection.Add(level, 1), Is.False);
            CollectionEntry entry = collection.Entries.Single();
            Assert.That((entry.PictureId, entry.PictureVersion, entry.LevelNumber), Is.EqualTo((level.Picture.Id, level.Picture.Version, 1)));
            Assert.That(entry.MappingHash, Has.Length.EqualTo(16));
            Assert.That(CollectionService.MappingHash(level.Mapping.Reverse().ToDictionary(p => p.Key, p => p.Value)), Is.EqualTo(entry.MappingHash), "independent of key order");
            LevelDefinition updated = level with { Picture = level.Picture with { Id = "hero_sprig" } };
            Assert.That(collection.Add(updated, 1), Is.True, "new launch art can replace previously completed art");
            Assert.That(collection.Add(updated, 1), Is.False, "replaying the new art still counts only once");
            Assert.That(collection.Count, Is.EqualTo(2), "the old collected picture is retained");
        }

        [Test]
        public void Themes_RotateByBand_AndMirrorTheRoadmapFile()
        {
            ThemeRotation rotation = ThemeRotation.Default;
            Assert.That(new[] { 1, 99, 100, 149, 150, 200, 250, 300, 350, 399, 400, 449, 450, 499, 500, 550, 949, 950 }.Select(l => rotation.ThemeFor(l).Id), Is.EqualTo(new[]
            {
                "daylight_garden", "daylight_garden", "pond", "pond", "orchard", "moonlit_garden", "fern", "willow",
                "pear", "pear", "citrus", "citrus", "plum", "plum", "daylight_garden", "pond", "plum", "daylight_garden",
            }));

            ThemeRotation file = ThemeRotation.Parse(File.ReadAllText(Path.Combine(DevContent.RepositoryContentFolder, "roadmap", "themes.json")));
            Assert.That((file.StartLevel, file.BandLength), Is.EqualTo((rotation.StartLevel, rotation.BandLength)));
            Assert.That(file.Themes, Is.EqualTo(rotation.Themes));
            Assert.That(rotation.Themes.All(t => ThemeRotation.Luminance(t.Background) > 0.7), Is.True, "light backgrounds keep tiles readable");
        }
    }
}
