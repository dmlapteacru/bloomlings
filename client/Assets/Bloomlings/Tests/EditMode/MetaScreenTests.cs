using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class MetaScreenTests
    {
        [Test]
        public void EachFamilyAndCategory_RemembersItsOwnClampedScroll()
        {
            var state=new WardrobeBrowseState();
            state.SetOffset(180,500);state.Select(CosmeticKind.Glasses);state.SetOffset(90,500);
            state.Select(Family.Drop);Assert.That(state.Offset,Is.Zero);
            state.SetOffset(900,300);Assert.That(state.Offset,Is.EqualTo(300));
            state.Select(Family.Sprig);Assert.That(state.Offset,Is.EqualTo(90));
            state.Select(CosmeticKind.Hat);Assert.That(state.Offset,Is.EqualTo(180));
            state.Drag(500,500);Assert.That(state.Offset,Is.Zero);
        }
        [Test]
        public void ThousandItemCatalog_OnlyKeepsVisibleRows_AndLastRowCanClearMenu()
        {
            foreach(float ratio in new[]{16f/9,19.5f/9,21f/9})
            {
                var r=WardrobeShelfDesign.Page(1080,1080*ratio,new Insets(80,40));
                Assert.That(r.Area.Height,Is.GreaterThan(r.RowHeight*1.25f));
                Assert.That(r.Categories.Bottom,Is.LessThan(r.Area.Top));
                var window=r.Visible(1000,r.Maximum(1000));
                Assert.That(window.Last,Is.EqualTo(999));
                Assert.That(window.Last-window.First,Is.LessThan(24),"allocation stays bounded by screen size");
                Assert.That(r.Cell(999).Bottom-r.Maximum(1000),Is.LessThan(r.NavTop));
                Assert.That(r.Thumb(1000,r.Maximum(1000)).Within(r.Track),Is.True);
            }
        }
        [Test]
        public void BuyingAndEquipping_DoNotReorderItems_AndDefaultChangesOnlyThisCategory()
        {
            var save=PlayerSave.CreateNew("wardrobe",DateTime.UtcNow);
            var hat=new CosmeticItem("hat.test",CosmeticKind.Hat,"Hat","cap","#999999",false,200);
            var skin=new CosmeticItem("skin.test",CosmeticKind.Skin,"Skin","spots","#999999",false);
            var catalog=new CosmeticCatalog(new[]{hat,skin});
            var wardrobe=new WardrobeService(save,catalog,new BundledRemoteConfigService(),()=>{});
            var before=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat);
            save.Cosmetics.Owned.Add(hat.Id);
            var after=WardrobeShelfCatalog.Build(wardrobe,CosmeticKind.Hat);
            Assert.That(after.Select(i=>i.Item?.Id),Is.EqualTo(before.Select(i=>i.Item?.Id)));
            Assert.That(after[1].State,Is.EqualTo(WardrobeItemState.Owned));
            var preview=WardrobeShelfCatalog.Preview(new Outfit(skin,hat,null,null),CosmeticKind.Hat,null);
            Assert.That(preview.Hat,Is.Null);Assert.That(preview.Skin,Is.SameAs(skin));
        }
        [Test]
        public void LongNames_KeepCompleteGraphemes_AndTheModelRetainsFullName()
        {
            string full="Garden 👩‍🌾👩‍🌾👩‍🌾 Gardener with a very long name";
            float Measure(string s)=>StringInfo.ParseCombiningCharacters(s).Length;
            string fitted=LeaderboardHonorDesign.FitName(full,12,Measure);
            Assert.That(fitted,Does.EndWith("…"));Assert.That(Measure(fitted),Is.LessThanOrEqualTo(12));
            Assert.That(fitted,Does.Not.Contain("\uFFFD"));
            var row=new LeaderboardEntry(4,full,87,false);
            Assert.That(row.Name,Is.EqualTo(full));
            Assert.That(LeaderboardHonorDesign.FitName("Rosie",20,Measure),Is.EqualTo("Rosie"));
        }
        [Test]
        public void Appearance_RoundTripsAllIndependentSlots_AndOldRowsHaveNoForcedLook()
        {
            var appearance=new LeaderboardAppearance("avatar.drop_default","frame.golden_ribbon","badge.sprout","marker.level_250","A gardener with a very long name 👩‍🌾");
            Assert.That(LeaderboardAppearance.FromMetadata(new JObject{["appearance"]=JObject.Parse(appearance.ToJson())}),Is.EqualTo(appearance));
            Assert.That(LeaderboardAppearance.FromMetadata(null),Is.Null);
            Assert.That(LeaderboardAppearance.FromMetadata("{bad json"),Is.Null);
            Assert.That(appearance with{Badge=null},Is.Not.EqualTo(appearance));
            Assert.That((appearance with{Badge=null}).Fingerprint,Is.Not.EqualTo(appearance.Fingerprint));
            Assert.That((appearance with{Name="Renamed gardener"}).Fingerprint,Is.Not.EqualTo(appearance.Fingerprint));
        }
        private sealed class Clock:IClock
        {
            public DateTime UtcNow=>new DateTime(2026,10,14,0,0,0,DateTimeKind.Utc);
            public DateTime UtcToday=>UtcNow.Date;
        }
        private sealed class Board:ILeaderboardService,ILeaderboardAppearanceService
        {
            public bool IsAvailable{get;set;}=true;
            public readonly List<LeaderboardAppearance> Sent=new List<LeaderboardAppearance>();
            public Action? DuringFirst;
            public bool Reject;
            public IEnumerator Submit(int level,int version,string hash,Action<SubmitResult> done){done(SubmitResult.Unreachable);yield break;}
            public IEnumerator Fetch(int count,Action<LeaderboardPage?> done){done(null);yield break;}
            public IEnumerator SubmitAppearance(int level,int version,string hash,LeaderboardAppearance look,Action<SubmitResult> done)
            {
                Sent.Add(look);if(Sent.Count==1)DuringFirst?.Invoke();done(new SubmitResult(!Reject,Reject?"rejected":null));yield break;
            }
        }
        private static void Run(IEnumerator routine)
        {
            while(routine.MoveNext())if(routine.Current is IEnumerator child)Run(child);
        }
        [Test]
        public void OfflineProfileChanges_StayQueued_AndAnEditDuringSubmitSendsLatestSnapshot()
        {
            var save=PlayerSave.CreateNew("profile",DateTime.UtcNow);save.Progression.HighestCompletedLevel=10;
            save.Unlocks.Flags[LeaderboardClient.UnlockId]=true;
            save.Stats.Counters[LeaderboardClient.SubmittedCounter]=10;
            var board=new Board{IsAvailable=false};
            var client=new LeaderboardClient(save,board,new BundledRemoteConfigService(),new Clock(),()=>{});
            Run(client.SubmitPending());Assert.That(board.Sent,Is.Empty);Assert.That(client.HasPendingSubmission,Is.True);
            board.IsAvailable=true;
            board.DuringFirst=()=>save.Cosmetics.Equipped["profile.avatar"]="avatar.drop_default";
            Run(client.SubmitPending());
            Assert.That(board.Sent.Count,Is.EqualTo(2));
            Assert.That(board.Sent.Last().Avatar,Is.EqualTo("avatar.drop_default"));
            Assert.That(client.SubmittedLevel,Is.EqualTo(10));Assert.That(client.HasPendingSubmission,Is.False);
            save.Profile.Name="Garden friend";
            Run(client.SubmitPending());Assert.That(board.Sent.Last().Name,Is.EqualTo(save.Profile.Name));
            Assert.That(client.HasPendingSubmission,Is.False);
            save.Cosmetics.Equipped["profile.badge"]="badge.sprout";board.Reject=true;
            Run(client.SubmitPending());Assert.That(client.HasPendingSubmission,Is.True);
        }
    }
}
