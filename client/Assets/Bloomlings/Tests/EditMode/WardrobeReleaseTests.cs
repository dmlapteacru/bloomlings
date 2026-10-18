using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    public class WardrobeReleaseTests
    {
        private static CosmeticCatalog Catalog => CosmeticCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json")));
        private PlayerSave _save=null!;
        private WardrobeService _wardrobe=null!;
        private EconomyService _economy=null!;
        private int _writes;
        [SetUp]
        public void FreshInventory()
        {
            _save=PlayerSave.CreateNew("release",DateTime.UtcNow);
            var config=new BundledRemoteConfigService();
            _writes=0;
            _economy=new EconomyService(_save,EconomyConfig.From(config),()=>_writes++);
            _wardrobe=new WardrobeService(_save,Catalog,config,()=>_writes++,_economy);
        }
        private void Reach(int level)
        {
            _save.Progression.HighestCompletedLevel=level-1;
            _save.Unlocks.Flags[WardrobeService.UnlockId]=level>=40;
        }
        private CosmeticItem Item(string id){Assert.That(Catalog.TryGet(id,out var item),Is.True,id);return item!;}
        private WardrobeShelfItem[] Shelf()=>WardrobeService.Families.SelectMany(f=>CosmeticCatalog.WornKinds.SelectMany(k=>WardrobeShelfCatalog.Build(_wardrobe,k,f))).Where(c=>c.Item!=null).ToArray();
        [TestCase(40,55)][TestCase(59,55)][TestCase(60,69)][TestCase(79,69)][TestCase(80,84)][TestCase(99,84)][TestCase(100,89)]
        public void FourUnlockStages_HavePlentyOfChoicesFromTheStart(int level,int available)
        {
            Reach(level);
            var shelf=Shelf();
            Assert.That(shelf.Length,Is.EqualTo(89));
            Assert.That(shelf.Count(c=>c.State!=WardrobeItemState.LevelLocked),Is.EqualTo(available));
            Assert.That(shelf.Select(c=>c.Item!.UnlockLevel).Distinct().OrderBy(x=>x),Is.EqualTo(new[]{40,60,80,100}));
            Assert.That(shelf.Any(c=>c.Item!.Retired||c.Item.DefaultLook||!HeroGearArt.CanPreview(c.Item)),Is.False);
            Assert.That(shelf.Count(c=>c.Item!.Free),Is.EqualTo(12));
            foreach(var family in WardrobeService.Families)
            {
                var starters=shelf.Where(c=>c.Item!.ForFamily==family&&c.Item.Free).ToArray();
                Assert.That(starters.Select(c=>c.Item!.Kind).OrderBy(x=>x),Is.EqualTo(new[]{CosmeticKind.Hat,CosmeticKind.Glasses,CosmeticKind.Clothes}.OrderBy(x=>x)));
                Assert.That(shelf.Count(c=>c.Item!.ForFamily==family&&c.Item.UnlockLevel==40),Is.GreaterThanOrEqualTo(12));
            }
            Assert.That(shelf.Where(c=>!c.Item!.Free).All(c=>c.Item!.Price>=250&&c.Item.Price<=3500),Is.True);
        }
        [Test]
        public void ReleasePrices_LeaveLongTermGoalsBeyondTheFirstHundredLevels()
        {
            Reach(100);
            var shelf=Shelf();int total=shelf.Sum(c=>c.Item!.Price);
            Assert.That(total,Is.EqualTo(73000));
            int maxLevelIncome=Enumerable.Range(1,99).Sum(_=>_economy.WinPetals(Bloomlings.Core.Definitions.DifficultyClass.SuperHard,0))
                +Enumerable.Range(1,99).Sum(l=>MilestoneTable.Default.CadenceFor(l)?.Petals??0);
            Assert.That(maxLevelIncome,Is.LessThan(total/10),"even all Super Hard clean wins cannot buy the catalog by current L100; daily rewards are extra");
            Assert.That(shelf.Count(c=>c.Item!.Price>=2000),Is.EqualTo(10),"special and holiday Heads remain long-term goals");
            Assert.That(shelf.Count(c=>!c.Item!.Free&&c.Item.Price<=650),Is.GreaterThanOrEqualTo(35),"many everyday choices remain reachable");
        }
        [Test]
        public void BeforeForty_StarterGearDoesNotBypassWardrobeGate()
        {
            Reach(39);
            Assert.That(_wardrobe.IsAvailable||_wardrobe.HasFreeHeroItems,Is.False);
            Assert.That(_wardrobe.ShopItems,Is.Empty);
            Assert.That(_wardrobe.Equip(Family.Twig,TwigV2Art.HairId),Is.False);
            _save.Cosmetics.Equipped["twig.hat"]=TwigV2Art.HairId;
            Assert.That(_wardrobe.OutfitOf(Family.Twig).IsEmpty,Is.True);
            Assert.That(HeroGearArt.Layer(Family.Twig,_wardrobe.OutfitOf(Family.Twig),0)!.Value.Name,Is.EqualTo("twigv2-base-pumpkinhat"));
        }
        [Test]
        public void LockedItem_CanBeFittedButNotBought_ThenOpensAtExactBoundary()
        {
            Reach(59);_economy.AdjustPetalsForTesting(2000);
            var coat=Item(SprigV2Art.CoatId);var browse=new WardrobeBrowseState();browse.Select(CosmeticKind.Clothes);
            string before=SaveSerializer.Write(_save);
            browse.TryOn(_wardrobe,coat);
            Assert.That(browse.ShownOutfit(_wardrobe).Clothes,Is.EqualTo(coat));
            Assert.That(_wardrobe.IsBuyable(coat),Is.False);
            Assert.That(_wardrobe.TryBuy(coat.Id),Is.False);
            Assert.That(SaveSerializer.Write(_save),Is.EqualTo(before));
            browse.Select(CosmeticKind.Glasses);Assert.That(browse.PreviewItem,Is.Null);
            Reach(60);
            Assert.That(_wardrobe.IsBuyable(coat),Is.True);
            Assert.That(_wardrobe.TryBuy(coat.Id),Is.True);
            Assert.That(_economy.Petals,Is.EqualTo(2000-coat.Price));
            Assert.That(_wardrobe.Equip(Family.Sprig,coat.Id),Is.True);
            Reach(40);
            Assert.That(_wardrobe.IsLevelLocked(coat),Is.False,"owned items stay usable when testing earlier levels");
            Assert.That(_wardrobe.OutfitOf(Family.Sprig).Clothes?.Id,Is.EqualTo(coat.Id));
        }
        [Test]
        public void AffordablePurchase_IsAtomic_ChargesOnce_AndSurvivesReload()
        {
            Reach(40);var cap=Item(SprigV2Art.CapId);
            _economy.AdjustPetalsForTesting(cap.Price-1);
            Assert.That(_wardrobe.TryBuy(cap.Id),Is.False);
            Assert.That(_save.Cosmetics.Owned,Does.Not.Contain(cap.Id));
            _economy.AdjustPetalsForTesting(1);
            string? persisted=null;
            var config=new BundledRemoteConfigService();
            var economy=new EconomyService(_save,EconomyConfig.From(config),()=>persisted=SaveSerializer.Write(_save));
            var wardrobe=new WardrobeService(_save,Catalog,config,()=>{},economy);
            Assert.That(wardrobe.TryBuy(cap.Id),Is.True);
            Assert.That(economy.Petals,Is.Zero);
            var disk=SaveSerializer.Read(persisted!);
            Assert.That(disk.Cosmetics.Owned,Does.Contain(cap.Id));
            Assert.That(disk.Wallet.Petals,Is.Zero);
            int events=0;wardrobe.Bought+=_=>events++;
            Assert.That(wardrobe.TryBuy(cap.Id),Is.False);Assert.That(events,Is.Zero);
            var browse=new WardrobeBrowseState();browse.TryOn(wardrobe,cap);
            Assert.That(browse.PreviewItem,Is.Null,"owned tap equips directly");
            Assert.That(wardrobe.OutfitOf(Family.Sprig).Hat?.Id,Is.EqualTo(cap.Id));
        }
        [Test]
        public void FormerTrialSave_KeepsOnlyActuallyEquippedPaidItems_WithoutCharging()
        {
            Reach(40);
            _save.Cosmetics.Equipped["bloom.hat"]="hat.bloom_crown";
            _save.Cosmetics.Equipped["twig.hat"]="hat.twig_flower_cap";
            _save.Cosmetics.Owned.Add("skin.spots");
            _save.Cosmetics.Owned.Add("hat.twig_flower_cap");
            string[] previous=_save.Cosmetics.Equipped.Values.ToArray();
            var config=new BundledRemoteConfigService();
            var restored=SaveSerializer.Read(SaveSerializer.Write(_save));
            int writes=0;
            var wardrobe=new WardrobeService(restored,Catalog,config,()=>writes++);
            Assert.That(wardrobe.OutfitOf(Family.Bloom).Hat?.Id,Is.EqualTo("hat.bloom_crown"));
            Assert.That(wardrobe.OutfitOf(Family.Twig).Hat?.Id,Is.EqualTo(TwigV2Art.BucketId));
            Assert.That(restored.Cosmetics.Owned,Is.EquivalentTo(new[]{"skin.spots","hat.twig_flower_cap","hat.bloom_crown",TwigV2Art.BucketId}));
            Assert.That(restored.Wallet.Petals,Is.Zero);
            Assert.That(restored.Cosmetics.Equipped.Values,Is.EqualTo(previous));
            new WardrobeService(restored,Catalog,config,()=>writes++);
            Assert.That(writes,Is.EqualTo(1),"migration persists once and is idempotent");
        }
        [Test]
        public void RetiredAndDuplicateGoods_AreHiddenAndCannotBePurchasedOrGranted()
        {
            Reach(100);_economy.AdjustPetalsForTesting(5000);
            _wardrobe.OnUnlock(WardrobeService.UnlockId);
            foreach(var item in Catalog.Items.Where(i=>i.Retired))
            {
                Assert.That(_wardrobe.TryBuy(item.Id),Is.False,item.Id);
                Assert.That(_wardrobe.ShopItems.Any(i=>i.Id==item.Id),Is.False,item.Id);
                Assert.That(_save.Cosmetics.Owned,Does.Not.Contain(item.Id),item.Id);
            }
            foreach(var id in MilestoneTable.Default.Cadences.SelectMany(c=>c.Items))Assert.That(Item(id).Retired,Is.False,id);
            var browse=new WardrobeBrowseState();
            foreach(var stock in Catalog.Items.Where(i=>i.DefaultLook))
            {
                browse.Select(stock.ForFamily!.Value);
                Assert.That(_wardrobe.Equip(browse.Family,stock.Id),Is.True);
                Assert.That(browse.Selected(_wardrobe,null),Is.True,"old explicit stock Head selects the single Default card");
                browse.TryOn(_wardrobe,null);
                Assert.That(_wardrobe.OutfitOf(browse.Family).Hat,Is.Null);
                Assert.That(browse.Selected(_wardrobe,null),Is.True);
            }
            Assert.That(_economy.Petals,Is.EqualTo(5000));
        }
        [Test]
        public void TesterPetals_ClampPersistNotifyAndLeaveProgressAndInventoryAlone()
        {
            Reach(40);_wardrobe.Equip(Family.Twig,TwigV2Art.HairId);
            string gear=string.Join("|",_save.Cosmetics.Equipped.Values);
            int writesBefore=_writes,notifications=0;_economy.Changed+=()=>notifications++;
            _economy.AdjustPetalsForTesting(-100);
            Assert.That(_writes,Is.EqualTo(writesBefore));
            _economy.AdjustPetalsForTesting(100);
            _economy.AdjustPetalsForTesting(1000);
            Assert.That(_economy.Petals,Is.EqualTo(1100));
            _economy.AdjustPetalsForTesting(-100);
            Assert.That(_economy.Petals,Is.EqualTo(1000));
            _economy.AdjustPetalsForTesting(int.MaxValue);
            Assert.That(_economy.Petals,Is.EqualTo(int.MaxValue));
            _economy.AdjustPetalsForTesting(1000);
            _economy.AdjustPetalsForTesting(int.MinValue);
            Assert.That(_economy.Petals,Is.Zero);
            Assert.That((_writes-writesBefore,notifications),Is.EqualTo((5,5)));
            Assert.That(_save.Progression.CurrentLevel,Is.EqualTo(40));
            Assert.That(string.Join("|",_save.Cosmetics.Equipped.Values),Is.EqualTo(gear));
            Assert.That(SaveSerializer.Read(SaveSerializer.Write(_save)).Wallet.Petals,Is.Zero);
        }
    }
}
