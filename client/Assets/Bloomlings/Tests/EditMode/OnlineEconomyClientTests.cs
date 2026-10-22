using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Economy.Online;
using Bloomlings.Content.Json;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class OnlineEconomyClientTests
    {
        private string _root = null!;
        private JObject _fixture = null!;
        private EconomyCatalog _catalog = null!;
        private FakeTransport _server = null!;
        private OnlineEconomyClient? _client;
        private EconomyJournal? _journal;
        private static string Repo()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName,"backend","tests","fixtures","economy-client.json"))) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("Repository fixture not found.");
        }
        [SetUp] public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "bloom-economy-" + Guid.NewGuid().ToString("N"));
            _fixture = JsonDoc.ParseObject(File.ReadAllText(Path.Combine(Repo(),"backend","tests","fixtures","economy-client.json")),"fixture");
            _catalog = new EconomyCatalog((JObject)_fixture["catalog"]!);
            _server = new FakeTransport(_fixture);
            Open();
        }
        [TearDown] public void TearDown() { _client?.Dispose(); _journal?.Dispose(); if(Directory.Exists(_root)) Directory.Delete(_root,true); }
        private void Open(string owner = "alice")
        {
            _client?.Dispose(); _journal?.Dispose();
            _journal = new EconomyJournal(_root,owner,_catalog);
            _client = new OnlineEconomyClient(_journal,_server);
        }
        private void Ready() { Run(_client!.Refresh()); Assert.That(_client.LastCode, Is.EqualTo("loaded")); }
        private EconomyQuote Hat() => _client!.Catalog.QuoteItem("hat.twig_nightcap")!;
        private static void Run(IEnumerator routine)
        {
            try { while(routine.MoveNext()) if(routine.Current is IEnumerator nested) Run(nested); }
            finally { (routine as IDisposable)?.Dispose(); }
        }
        private sealed class FakeTransport : IEconomyTransport
        {
            public bool IsAvailable { get; set; } = true;
            public string? PlayerId { get; set; } = "alice";
            public long SessionVersion { get; set; } = 1;
            public JObject State;
            public JObject Catalog;
            public readonly List<string> Sent = new List<string>();
            public readonly HashSet<string> Tickets = new HashSet<string>();
            private readonly Dictionary<string,long> _cursors = new Dictionary<string,long>();
            private readonly Dictionary<string,(string Json, JObject Reply)> _receipts = new Dictionary<string,(string,JObject)>();
            public Action? DuringRead;
            public Action? DuringExecute;
            public bool DropResponse;
            public bool ThrowAfterCommit;
            public JObject? OverrideRead;
            public JObject? OverrideReply;
            public int Applied;
            public FakeTransport(JObject fixture) { State=(JObject)fixture["initialState"]!.DeepClone(); Catalog=(JObject)fixture["catalog"]!.DeepClone(); }
            public long Next(string install) => _cursors.TryGetValue(install,out long high) ? high+1 : 1;
            private JObject Failure(string code,bool retryable=false) => new JObject { ["ok"]=false,["code"]=code,["consumed"]=false,["retryable"]=retryable,["state"]=null,["catalogVersion"]=Catalog["version"]!.DeepClone() };
            public IEnumerator Read(string owner,long session,string install,Action<EconomyReply> done)
            {
                JObject read=OverrideRead ?? new JObject { ["ok"]=true,["code"]="loaded",["consumed"]=false,["retryable"]=false,["state"]=State.DeepClone(),["catalogVersion"]=Catalog["version"]!.DeepClone(),["catalog"]=new JObject { ["cosmetics"]=Catalog["cosmetics"]!.DeepClone(),["boosters"]=Catalog["boosters"]!.DeepClone() },["nextSequence"]=Next(install) };
                DuringRead?.Invoke(); done(new EconomyReply(read)); yield break;
            }
            public IEnumerator Execute(string owner,long session,string json,Action<EconomyReply> done)
            {
                Sent.Add(json); DuringExecute?.Invoke();
                JObject op=JsonDoc.ParseObject(json,"operation");
                string install=(string)op["installationId"]!; long seq=(long)op["sequence"]!; string key=install+":"+seq;
                JObject reply;
                if(_receipts.TryGetValue(key,out var old))
                {
                    reply=old.Json==json ? (JObject)old.Reply.DeepClone() : Failure("sequence-reused");
                    if(old.Json==json) { reply["state"]=State.DeepClone();reply["nextSequence"]=Next(install); }
                }
                else if(seq!=Next(install)) reply=Failure("sequence-gap");
                else if((string?)op["catalogVersion"]!=(string?)Catalog["version"]) reply=Failure("catalog-changed");
                else
                {
                    bool ok=true;string code="purchased";
                    if((string?)op["kind"]=="buy_cosmetic")
                    {
                        string item=(string)op["itemId"]!;int price=(int)Catalog["cosmetics"]![item]!["price"]!;
                        if(((JArray)State["owned"]!).Any(v=>(string?)v==item)){ok=false;code="already-owned";}
                        else if((int)State["petals"]!<price){ok=false;code="insufficient-petals";}
                        else {State["petals"]=(int)State["petals"]!-price;((JArray)State["owned"]!).Add(item);Applied++;}
                    }
                    else if((string?)op["kind"]=="claim_level" || (string?)op["kind"]=="claim_challenge")
                    {
                        if(!Tickets.Contains((string)op["ticketId"]!)){ok=false;code="reward-not-authorized";}
                        else {State["petals"]=(int)State["petals"]!+20;code="rewarded";Applied++;}
                    }
                    else {code="consumed";State["boosters"]![(string)op["boosterKind"]!]=(int)State["boosters"]![(string)op["boosterKind"]!]!-1;Applied++;}
                    _cursors[install]=seq;State["revision"]=(long)State["revision"]!+1;
                    reply=new JObject { ["ok"]=ok,["code"]=code,["consumed"]=true,["retryable"]=false,["state"]=State.DeepClone(),["nextSequence"]=seq+1,["catalogVersion"]=Catalog["version"]!.DeepClone(),["effects"]=new JObject { ["petals"]=999999 } };
                    _receipts.Add(key,(json,(JObject)reply.DeepClone()));
                }
                if(ThrowAfterCommit){ThrowAfterCommit=false;throw new IOException("response lost after server commit");}
                if(DropResponse)done(EconomyReply.Unavailable);
                else done(new EconomyReply(OverrideReply??reply));
                yield break;
            }
        }

        [Test] public void ParsesActualBackendContractAndBuiltInCatalog()
        {
            var read=new EconomyReply((JObject)_fixture["read"]!);
            var purchase=new EconomyReply((JObject)_fixture["purchase"]!);
            Assert.That(read.Catalog!.Version,Is.EqualTo(_catalog.Version));
            Assert.That(purchase.State!.Petals,Is.EqualTo(9350));
            Assert.That(purchase.Consumed,Is.True);
            Assert.That(purchase.State.Owns("hat.twig_nightcap"),Is.True);
            var bundle=EconomyCatalog.Parse(File.ReadAllText(Path.Combine(Repo(),"client/Assets/Bloomlings/Services/Economy/Resources/ServerEconomyCatalog.json")));
            Assert.That(bundle.Version,Is.EqualTo(read.Catalog.Version));
        }
        [Test] public void OfflineFirstLaunchCanBrowseBundledPricesButCannotQueueAPurchase()
        {
            _server.IsAvailable=false;
            Assert.That(Hat().Price,Is.EqualTo(650));
            Run(_client!.Purchase(Hat()));
            Assert.That(_client.LastCode,Is.EqualTo("offline"));Assert.That(_client.PendingCount,Is.Zero);Assert.That(_server.Sent,Is.Empty);
        }
        [Test] public void CacheSurvivesRestartButDoesNotGrantFreshOnlinePurchaseAuthority()
        {
            Ready();Run(_client!.Purchase(Hat()));Open();_server.IsAvailable=false;
            Assert.That(_client!.Owns("hat.twig_nightcap"),Is.True);Assert.That(_client.Confirmed!.Petals,Is.EqualTo(9350));
            _server.IsAvailable=true;Assert.That(_client.CanStartPurchase,Is.False);
            Run(_client.Purchase(_client.Catalog.QuoteItem("hat.twig_boater")!));Assert.That(_client.LastCode,Is.EqualTo("refresh-required"));
        }
        [Test] public void OwnedClothingCanBeEquippedOfflineAndSelectionSurvivesRestart()
        {
            Ready();Run(_client!.Purchase(Hat()));_server.IsAvailable=false;
            var item=new CosmeticItem("hat.twig_nightcap",CosmeticKind.Hat,"Nightcap","cap","#FFFFFF",false,650,ForFamily:Family.Twig);
            var definitions=new CosmeticCatalog(new[]{item});
            Assert.That(_client.Equip(Family.Twig,item.Id,definitions),Is.True);
            Assert.That(_client.Equip(Family.Bloom,item.Id,definitions),Is.False);
            Open();Assert.That(_client!.Equipped("twig.hat"),Is.EqualTo(item.Id));
        }
        [Test] public void LostResponseRetriesExactlyTheSameSerializedOperationWithoutAddingEffects()
        {
            Ready();_server.ThrowAfterCommit=true;Run(_client!.Purchase(Hat()));
            Assert.That(_client.PendingCount,Is.EqualTo(1));Assert.That(_client.Confirmed!.Petals,Is.EqualTo(10000));
            Open();Run(_client!.Flush());
            Assert.That(_server.Sent.Count,Is.EqualTo(2));Assert.That(_server.Sent[1],Is.EqualTo(_server.Sent[0]));Assert.That(_server.Applied,Is.EqualTo(1));
            Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.Confirmed!.Petals,Is.EqualTo(9350),"effects are never added to the snapshot");
        }
        [Test] public void CrashBeforeDispatchDoesNotAutoBuyAfterRestart()
        {
            Ready();int writes=0;_journal!.AfterTempWrite=()=>{if(++writes==2)throw new IOException("crash before dispatch marker commits");};
            Assert.Throws<IOException>(()=>Run(_client!.Purchase(Hat())));
            Assert.That(_server.Sent,Is.Empty);_journal.AfterTempWrite=null;Open();Ready();Run(_client!.Flush());
            Assert.That(_client.LastCode,Is.EqualTo("confirmation-required"));Assert.That(_server.Sent,Is.Empty);
            Run(_client.ConfirmPendingPurchase(Hat()));Assert.That(_server.Applied,Is.EqualTo(1));Assert.That(_client.PendingCount,Is.Zero);
        }
        [Test] public void CrashDuringAcknowledgementReplaysTheOldRequestRatherThanBuyingAgain()
        {
            Ready();_server.DuringExecute=()=>_journal!.AfterTempWrite=()=>throw new IOException("ack crash");
            Assert.Throws<IOException>(()=>Run(_client!.Purchase(Hat())));
            _journal!.AfterTempWrite=null;_server.DuringExecute=null;Open();Run(_client!.Flush());
            Assert.That(_server.Applied,Is.EqualTo(1));Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.Confirmed!.Petals,Is.EqualTo(9350));
        }
        [Test] public void PriceChangeBlocksOldOperationUntilARefreshAndExplicitConfirmation()
        {
            Ready();var old=Hat();_server.Catalog["version"]="catalog-new";_server.Catalog["cosmetics"]![old.Target]!["price"]=800;
            Run(_client!.Purchase(old));Assert.That(_client.LastCode,Is.EqualTo("catalog-changed"));Assert.That(_server.Applied,Is.Zero);
            Ready();Run(_client.Flush());Assert.That(_server.Applied,Is.Zero);
            Run(_client.ConfirmPendingPurchase(Hat()));Assert.That(_server.Applied,Is.EqualTo(1));Assert.That(_client.Confirmed!.Petals,Is.EqualTo(9200));
            Assert.That((long)JsonDoc.ParseObject(_server.Sent[0],"op")["sequence"]!,Is.EqualTo((long)JsonDoc.ParseObject(_server.Sent[1],"op")["sequence"]!));
        }
        [Test] public void RetryDoesNotReplaceAnUnknownPurchaseWithTheNewCatalog()
        {
            Ready();_server.DropResponse=true;Run(_client!.Purchase(Hat()));string sent=_server.Sent[0];
            _server.DropResponse=false;_server.Catalog["version"]="changed";_server.Catalog["cosmetics"]![Hat().Target]!["price"]=800;
            Ready();Run(_client.Flush());Assert.That(_server.Sent[1],Is.EqualTo(sent));Assert.That(_client.Confirmed!.Petals,Is.EqualTo(9350));
        }
        [Test] public void DefinitiveProtocolFailureDoesNotDropRequestOrAdvanceChannel()
        {
            Ready();_server.OverrideReply=new JObject{["ok"]=false,["code"]="needs-initialization",["consumed"]=false,["retryable"]=false,["state"]=null};
            Run(_client!.Purchase(Hat()));Open();Run(_client!.Flush());
            Assert.That(_client.PendingCount,Is.EqualTo(1));Assert.That(_client.LastCode,Is.EqualTo("needs-initialization"));Assert.That(_server.Sent.Count,Is.EqualTo(1));
        }
        [Test] public void BusinessRejectionIsAcknowledgedWithoutGrantingOwnership()
        {
            _server.State["petals"]=0;Ready();Run(_client!.Purchase(Hat()));
            Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.Owns(Hat().Target),Is.False);Assert.That(_client.LastAcknowledgedCode,Is.EqualTo("insufficient-petals"));
        }
        [Test] public void OfflineRewardEvidenceSurvivesRestartWithoutMintingConfirmedMoney()
        {
            Ready();_server.IsAvailable=false;
            Assert.That(_client!.RecordWin("level:101",101,"{\"level\":101,\"commands\":[\"tap a\"]}"),Is.True);
            Assert.That(_client.RecordWin("level:101",101,"{}"),Is.False);
            Open();Assert.That(_client!.Confirmed!.Petals,Is.EqualTo(10000));Assert.That(_client.UnsettledRewardIds,Does.Contain("level:101"));
            Assert.That(_client.RewardEvidence("level:101"),Does.Contain("tap a"));Assert.That(_client.PendingCount,Is.Zero);
            Assert.That(_client.QueueAuthorizedWin("level:101","verified-ticket"),Is.True);Assert.That(_client.QueueAuthorizedWin("level:101","other"),Is.False);
            _server.Tickets.Add("verified-ticket");_server.IsAvailable=true;Run(_client.Flush());
            Assert.That(_client.Confirmed!.Petals,Is.EqualTo(10020));Assert.That(_client.RewardSettled("level:101"),Is.True);
        }
        [Test] public void RejectedRewardAuthorizationRetainsEvidenceForALaterVerifiedTicket()
        {
            Ready();_client!.RecordWin("level:101",101,"{}");_client.QueueAuthorizedWin("level:101","not-ready");Run(_client.Flush());
            Assert.That(_client.RewardSettled("level:101"),Is.False);Assert.That(_client.RewardEvidence("level:101"),Is.EqualTo("{}"));
            _server.Tickets.Add("ready");_client.QueueAuthorizedWin("level:101","ready");Run(_client.Flush());Assert.That(_client.RewardSettled("level:101"),Is.True);
        }
        [Test] public void OfflineBoosterUseReservesCachedChargesWithoutAnAutomaticPurchase()
        {
            _server.State["boosters"]!["shuffle"]=1;Ready();_server.IsAvailable=false;
            Assert.That(_client!.QueueBoosterUse("shuffle"),Is.True);Assert.That(_client.QueueBoosterUse("shuffle"),Is.False);
            Assert.That(_client.AvailableCharges("shuffle"),Is.Zero);Assert.That(_client.Confirmed!.Charges("shuffle"),Is.EqualTo(1));
            _server.IsAvailable=true;Run(_client.Flush());Assert.That(_client.Confirmed!.Charges("shuffle"),Is.Zero);Assert.That(_client.Confirmed.Petals,Is.EqualTo(10000));
        }
        [TestCase(false)] [TestCase(true)] public void LateReadAndPurchaseResponseCannotCrossAtoBtoA(bool purchase)
        {
            Ready();Action change=()=>{_server.PlayerId="bob";_server.SessionVersion++;_server.PlayerId="alice";_server.SessionVersion++;};
            if(purchase){_server.DuringExecute=change;Run(_client!.Purchase(Hat()));Assert.That(_client.PendingCount,Is.EqualTo(1));Assert.That(_client.Confirmed!.Petals,Is.EqualTo(10000));}
            else{_server.DuringRead=change;Run(_client!.Refresh());Assert.That(_client.LastCode,Is.EqualTo("account-changed"));}
            Assert.That(_client!.CanStartPurchase,Is.False);
        }
        [Test] public void DifferentOwnerCannotApplyCachedWalletOrSendAnotherPlayersQueue()
        {
            Ready();_server.DropResponse=true;Run(_client!.Purchase(Hat()));_server.PlayerId="bob";Run(_client.Flush());Assert.That(_server.Sent.Count,Is.EqualTo(1));
            Open("bob");Assert.That(_client!.Confirmed,Is.Null);Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.Owns("hat.twig_nightcap"),Is.False);
        }
        [Test] public void StaleAndWrongOwnerSnapshotsNeverReplaceTheCache()
        {
            Ready();Run(_client!.Purchase(Hat()));_server.OverrideRead=(JObject)_fixture["read"]!.DeepClone();Run(_client.Refresh());
            Assert.That(_client.LastCode,Is.EqualTo("stale-response"));Assert.That(_client.Confirmed!.Petals,Is.EqualTo(9350));
            _server.OverrideRead["state"]!["ownerId"]="bob";Run(_client.Refresh());Assert.That(_client.LastCode,Is.EqualTo("invalid-response"));
        }
        [Test] public void SameRevisionWithDifferentBalanceIsRejected()
        {
            Ready();_server.State["petals"]=999999;Run(_client!.Refresh());Assert.That(_client.LastCode,Is.EqualTo("stale-response"));Assert.That(_client.Confirmed!.Petals,Is.EqualTo(10000));
        }
        [Test] public void CorruptJournalNeverCreatesANewInstallationAndSecondWriterIsRejected()
        {
            string main=_journal!.MainPath;Assert.Throws<IOException>(()=>new EconomyJournal(_root,"alice",_catalog));
            _client!.Dispose();File.WriteAllText(main,"broken");File.WriteAllText(main+".bak","broken");
            Assert.Throws<InvalidDataException>(()=>new EconomyJournal(_root,"alice",_catalog));Assert.That(File.ReadAllText(main),Is.EqualTo("broken"));
        }
        [Test] public void BackupRecoveryDoesNotTreatAnUncertainPurchaseAsNewlyConfirmed()
        {
            Ready();int writes=0;_journal!.AfterTempWrite=()=>{if(++writes==2)throw new IOException("dispatch marker");};
            Assert.Throws<IOException>(()=>Run(_client!.Purchase(Hat())));_journal.AfterTempWrite=null;
            _journal.Commit(_=>{}); // backup now contains the unattempted intent
            string main=_journal.MainPath;_client!.Dispose();File.WriteAllText(main,"broken");Open();Ready();Run(_client!.Flush());
            Assert.That(_server.Sent,Is.Empty);Assert.That(_client.LastCode,Is.EqualTo("confirmation-required"));
            Run(_client.ConfirmPendingPurchase(Hat()));Assert.That(_server.Applied,Is.EqualTo(1));
        }
        [Test] public void CorruptRewardEvidenceCannotBecomeAnAuthorizedClaim()
        {
            _client!.RecordWin("level:101",101,"{}");string file=Directory.GetFiles(Path.GetDirectoryName(_journal!.MainPath)!,"*.json",SearchOption.AllDirectories).Single(p=>p.Contains("evidence"));
            File.WriteAllText(file,"{\"changed\":true}");Assert.Throws<InvalidDataException>(()=>_client.QueueAuthorizedWin("level:101","ticket"));Assert.That(_client.PendingCount,Is.Zero);
        }
        [Test] public void SuccessfulReadRequiresExplicitServerCursorAndRejectsUnsafeNumbers()
        {
            var read=(JObject)_fixture["read"]!.DeepClone();read.Remove("nextSequence");Assert.Throws<ContentFormatException>(()=>new EconomyReply(read));
            read=(JObject)_fixture["read"]!.DeepClone();read["state"]!["revision"]=EconomyProtocol.MaxSafeInteger+1;Assert.Throws<ContentFormatException>(()=>new EconomyReply(read));
        }
        [Test] public void MissingJournalInAnInitializedOwnerFolderCannotResetItsIdentity()
        {
            string main=_journal!.MainPath;_client!.Dispose();File.Delete(main);File.Delete(main+".bak");File.Delete(main+".tmp");
            Assert.Throws<InvalidDataException>(()=>new EconomyJournal(_root,"alice",_catalog));
        }
        [Test] public void InterruptedBackupRecoveryNeverInstallsATrustedUncertainPurchase()
        {
            Ready();_server.DuringExecute=()=>throw new IOException("lost before server handles request");Run(_client!.Purchase(Hat()));
            _server.DuringExecute=null;_journal!.Commit(_=>{});string main=_journal.MainPath;_client.Dispose();File.WriteAllText(main,"corrupt");
            Assert.Throws<IOException>(()=>new EconomyJournal(_root,"alice",_catalog,()=>throw new IOException("recovery interrupted")));
            Open();Ready();Run(_client!.Flush());Assert.That(_server.Applied,Is.Zero);Assert.That(_client.LastCode,Is.EqualTo("confirmation-required"));
        }
        [Test] public void OfflineBoosterQueueCanReconcileCatalogChangeWithoutSkippingASequence()
        {
            _server.State["boosters"]!["shuffle"]=1;Ready();_server.IsAvailable=false;_client!.QueueBoosterUse("shuffle");
            _server.IsAvailable=true;_server.Catalog["version"]="new-prices";Ready();Run(_client.Flush());
            Assert.That(_client.LastCode,Is.EqualTo("catalog-changed"));Run(_client.ReconcileNonPurchase());
            Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.Confirmed!.Charges("shuffle"),Is.Zero);Assert.That(_client.Confirmed.Petals,Is.EqualTo(10000));
            Assert.That((long)JsonDoc.ParseObject(_server.Sent[0],"op")["sequence"]!,Is.EqualTo((long)JsonDoc.ParseObject(_server.Sent[1],"op")["sequence"]!));
        }
        [Test] public void OfflineRewardCatalogChangeRequiresAFreshTrustedTicket()
        {
            Ready();_client!.RecordWin("level:101",101,"{}");_client.QueueAuthorizedWin("level:101","old-ticket");
            _server.Catalog["version"]="new-rewards";Ready();Run(_client.Flush());Run(_client.ReconcileNonPurchase());
            Assert.That(_client.LastCode,Is.EqualTo("authorization-required"));Assert.That(_client.PendingCount,Is.EqualTo(1));
            _server.Tickets.Add("fresh-ticket");Run(_client.ReconcileNonPurchase("fresh-ticket"));
            Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.RewardSettled("level:101"),Is.True);
        }
        [Test] public void AutoBuyingABoosterCannotStartOffline()
        {
            Ready();_server.IsAvailable=false;Run(_client!.Purchase(_client.Catalog.QuoteBooster("shuffle",true)!));
            Assert.That(_client.PendingCount,Is.Zero);Assert.That(_server.Sent,Is.Empty);
        }
        [Test] public void RecoveredNonPurchaseReconciliationFirstReplaysOriginalUncertainBytes()
        {
            _server.State["boosters"]!["shuffle"]=1;Ready();_client!.QueueBoosterUse("shuffle");
            string original=(string)((JObject)((JArray)_journal!.Data["pending"]!)[0])["operation"]!;
            _journal.Commit(doc=>((JObject)((JArray)doc["pending"]!)[0])["uncertain"]=true);
            _server.Catalog["version"]="new-prices";Ready();Run(_client.ReconcileNonPurchase());
            Assert.That(_server.Sent[0],Is.EqualTo(original));Assert.That(_client.LastCode,Is.EqualTo("catalog-changed"));
            Run(_client.ReconcileNonPurchase());Assert.That(_client.PendingCount,Is.Zero);Assert.That(_client.Confirmed!.Charges("shuffle"),Is.Zero);
        }
    }
}
