using System;
using System.Collections;
using System.IO;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class AccountPanelTests
    {
        private sealed class Gateway : IAccountPanelGateway
        {
            public AccountPanelSnapshot State = new AccountPanelSnapshot("A:1", "one", "Rosie", 44, 1200, "account.panel.guest", AccountSaveState.Conflict, CanGoogle:true, CanSync:true, CanReviewCloud:true);
            public Action<AccountPanelReply>? Done;
            public int Calls;
            public string? Token;
            public AccountPanelSnapshot Read() => State;
            public void Execute(AccountPanelAction action, string? token, Action<AccountPanelReply> done) { Calls++; Done = done; Token = token; }
        }
        private static AccountCloudPreview Preview => new AccountCloudPreview("token", "Rosie cloud", 50, 700);
        [Test] public void DoubleTapAndDuplicateCallbackCannotDispatchTwoRestores()
        {
            var g = new Gateway(); var p = new AccountPanel(g); p.Open();
            p.Run(AccountPanelAction.ReviewCloud); p.Run(AccountPanelAction.ReviewCloud);
            Assert.That(g.Calls, Is.EqualTo(1)); g.Done!(new AccountPanelReply("review", Preview));
            var oldCallback = g.Done;
            p.Run(AccountPanelAction.RestoreCloud); p.Run(AccountPanelAction.RestoreCloud);
            Assert.That(g.Calls, Is.EqualTo(2)); Assert.That(g.Token, Is.EqualTo("token"));
            oldCallback!(new AccountPanelReply("old", Preview)); Assert.That(p.Busy, Is.True);
            g.Done!(new AccountPanelReply("done")); Assert.That(p.Preview, Is.Null);
        }
        [Test] public void LocalPlayAfterReviewInvalidatesConfirmation()
        {
            var g = new Gateway(); var p = new AccountPanel(g); p.Run(AccountPanelAction.ReviewCloud);
            g.Done!(new AccountPanelReply("review", Preview));
            g.State = g.State with { ProgressStamp = "two" };
            p.Run(AccountPanelAction.RestoreCloud); Assert.That(g.Calls, Is.EqualTo(1)); Assert.That(p.Preview, Is.Null);
        }
        [Test] public void LateReviewAfterAccountSwitchCannotOfferForeignRestore()
        {
            var g = new Gateway(); var p = new AccountPanel(g); p.Run(AccountPanelAction.ReviewCloud);
            g.State = g.State with { SessionKey = "A:3" }; // A → B → A
            g.Done!(new AccountPanelReply("review", Preview));
            Assert.That(p.Preview, Is.Null); Assert.That(p.MessageKey, Is.EqualTo("account.panel.changed"));
        }
        [Test] public void ClosingAndReopeningDoesNotResurrectPendingReview()
        {
            var g = new Gateway(); var p = new AccountPanel(g); p.Open(); p.Run(AccountPanelAction.ReviewCloud);
            p.Close(); p.Open(); g.Done!(new AccountPanelReply("review", Preview));
            Assert.That(p.Preview, Is.Null); Assert.That(p.Busy, Is.False);
        }
        [Test] public void DeviceOnlyBuildCannotPretendToConnectOrRestore()
        {
            var g = new Gateway { State = new AccountPanelSnapshot("guest", "one", "", 1, 0, "account.panel.guest", AccountSaveState.Device) };
            var p = new AccountPanel(g);
            foreach (AccountPanelAction action in Enum.GetValues(typeof(AccountPanelAction))) p.Run(action);
            Assert.That(g.Calls, Is.Zero); Assert.That(p.StatusKey, Is.EqualTo("account.panel.saved_device"));
        }
        [Test] public void ProviderExceptionLeavesPanelRetryable()
        {
            var p = new AccountPanel(new DelegateAccountPanelGateway(() => new Gateway().State, (_, __, ___) => throw new IOException()));
            p.Run(AccountPanelAction.Sync); Assert.That(p.Busy, Is.False); Assert.That(p.MessageKey, Is.EqualTo("account.panel.retry"));
        }

        private static readonly DateTime Now = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        private sealed class Clock : IClock { public DateTime UtcNow { get; set; } = Now; public DateTime UtcToday => UtcNow.Date; }
        private sealed class Auth : IAuthService
        {
            public bool SignedIn = true;
            public bool AllowLink;
            public bool IsSignedIn => SignedIn;
            public string? PlayerId { get; set; } = "A";
            public string? LinkedIdentity => "google_play_games";
            public bool CanLink(LinkProvider provider) => AllowLink;
            public IEnumerator SignIn(Action<bool> done) { SignedIn = true; PlayerId = "A"; done(true); yield break; }
            public IEnumerator Link(LinkProvider provider, Action<bool> done) { done(AllowLink); yield break; }
        }
        private sealed class Cloud : IAccountCloudSaveService
        {
            public bool Available = true;
            public bool IsAvailable => Available;
            public string? PlayerId { get; set; } = "A";
            public long SessionVersion { get; set; }
            public string? Json;
            public Action? DuringLoad;
            public IEnumerator Load(Action<CloudLoadResult> done) => throw new AssertionException("Unscoped load");
            public IEnumerator Store(string json, Action<bool> done) => throw new AssertionException("Unscoped store");
            public IEnumerator LoadFor(string owner, long session, Action<CloudLoadResult> done)
            { var result = new CloudLoadResult(true, Json) { WriteLock = "rev" }; DuringLoad?.Invoke(); done(result); yield break; }
            public IEnumerator StoreFor(string owner, long session, string json, string? writeLock, Action<CloudStoreResult> done)
            { throw new AssertionException("Restore must not write a cloud snapshot"); }
        }
        private static void Run(IEnumerator r) { try { while (r.MoveNext()) if (r.Current is IEnumerator child) Run(child); } finally { (r as IDisposable)?.Dispose(); } }

        private sealed class Setup : IDisposable
        {
            public string Root = Path.Combine(Path.GetTempPath(), "account-panel-" + Guid.NewGuid().ToString("N"));
            public Clock Clock = new Clock();
            public SaveService Saves;
            public PlayerSave Remote;
            public Cloud Cloud = new Cloud();
            public Auth Auth = new Auth();
            public CloudAccountPanelGateway Gateway;
            public AccountPanel Panel;
            public int Refreshes;
            public Setup()
            {
                var clock = Clock; Saves = new SaveService(Root, clock); Saves.Load();
                Saves.Current.CloudOwnerId = "A"; Saves.Current.Wallet.SetPetals(1200); Saves.Current.Cosmetics.Owned.Add("device.item"); Saves.Save();
                Remote = SaveSerializer.Read(SaveSerializer.Write(Saves.Current)); Remote.Wallet.SetPetals(700);
                Remote.Cosmetics.Owned.Clear(); Remote.Cosmetics.Owned.Add("cloud.item"); Remote.Progression.HighestCompletedLevel = 49;
                Cloud.Json = SaveSerializer.Write(Remote);
                var sync = new CloudSaveSync(Saves.Current, Saves.Save, Cloud); Run(sync.Sync());
                Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Conflict));
                Gateway = new CloudAccountPanelGateway(Saves.Current, Saves, Auth, Cloud, sync, clock, Run, () => Refreshes++);
                Panel = new AccountPanel(Gateway); Panel.Open();
            }
            public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
        }
        [Test] public void RestoreUsesWholeSnapshotAndPreservesDeviceBranchOnDisk()
        {
            using var s = new Setup(); string localId = s.Saves.Current.LocalPlayerId;
            s.Panel.Run(AccountPanelAction.ReviewCloud);
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(1200));
            Assert.That(s.Panel.Preview!.Petals, Is.EqualTo(700));
            s.Panel.Run(AccountPanelAction.RestoreCloud);
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(700));
            Assert.That(s.Saves.Current.Cosmetics.Owned, Is.EquivalentTo(new[] { "cloud.item" }));
            Assert.That(s.Saves.Current.LocalPlayerId, Is.EqualTo(localId)); Assert.That(s.Refreshes, Is.EqualTo(1));
            string[] archives = Directory.GetDirectories(Path.Combine(s.Root, "conflicts")); Assert.That(archives.Length, Is.EqualTo(1));
            var archive = new SaveService(archives[0], new Clock()); archive.Load();
            Assert.That(archive.Current.Wallet.Petals, Is.EqualTo(1200));
            Assert.That(archive.Current.Cosmetics.Owned, Does.Contain("device.item"));
        }
        [Test] public void ChangedRemoteSinceReviewRequiresAnotherConfirmation()
        {
            using var s = new Setup(); s.Panel.Run(AccountPanelAction.ReviewCloud);
            s.Remote.Wallet.SetPetals(600); s.Cloud.Json = SaveSerializer.Write(s.Remote);
            s.Panel.Run(AccountPanelAction.RestoreCloud);
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(1200)); Assert.That(s.Refreshes, Is.Zero);
            Assert.That(s.Panel.MessageKey, Is.EqualTo("account.panel.changed"));
        }
        [Test] public void ChangingLocalProgressDuringRestoreReadDoesNotOverwriteIt()
        {
            using var s = new Setup(); s.Panel.Run(AccountPanelAction.ReviewCloud);
            s.Cloud.DuringLoad = () => s.Saves.Current.Wallet.SetPetals(1100);
            s.Panel.Run(AccountPanelAction.RestoreCloud);
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(1100)); Assert.That(s.Refreshes, Is.Zero);
        }
        [Test] public void ProviderSessionChangeDuringRestoreDoesNotOverwriteLocalProfile()
        {
            using var s = new Setup(); s.Panel.Run(AccountPanelAction.ReviewCloud);
            s.Cloud.DuringLoad = () => s.Cloud.SessionVersion++;
            s.Panel.Run(AccountPanelAction.RestoreCloud);
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(1200)); Assert.That(s.Refreshes, Is.Zero);
        }
        [Test] public void InitialGuestConnectionAcceptsOnlyTheHostValidatedNewSession()
        {
            string root = Path.Combine(Path.GetTempPath(), "account-first-link-" + Guid.NewGuid().ToString("N"));
            try
            {
                var clock = new Clock(); var saves = new SaveService(root, clock); saves.Load(); saves.Save();
                var auth = new Auth { SignedIn = false, PlayerId = null, AllowLink = true };
                var cloud = new Cloud { Available = false };
                var sync = new CloudSaveSync(saves.Current, saves.Save, cloud);
                var panel = new AccountPanel(new CloudAccountPanelGateway(saves.Current, saves, auth, cloud, sync, clock, Run, () => { }));
                panel.Open(); panel.Run(AccountPanelAction.Google);
                Assert.That(panel.MessageKey, Is.EqualTo("account.panel.linked"));
                Assert.That(saves.Current.LinkedIdentity, Is.EqualTo("google_play_games"));
                Assert.That(saves.Current.CloudOwnerId, Is.Null, "offline linking must not claim a saved cloud wallet");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        [Test] public void UnboundDeviceCanExplicitlyRestoreItsAuthenticatedCloudWithoutCopyingGuestWallet()
        {
            using var s = new Setup(); s.Saves.Current.CloudOwnerId = null; s.Saves.Save();
            s.Panel.Run(AccountPanelAction.ReviewCloud);
            Assert.That(s.Panel.Preview, Is.Not.Null); Assert.That(s.Saves.Current.CloudOwnerId, Is.Null);
            s.Panel.Run(AccountPanelAction.RestoreCloud);
            Assert.That(s.Saves.Current.CloudOwnerId, Is.EqualTo("A"));
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(700));
            Assert.That(s.Saves.Current.Cosmetics.Owned, Does.Not.Contain("device.item"));
        }
        [Test] public void OldRestoreConfirmationExpiresWithoutTouchingDeviceSave()
        {
            using var s = new Setup(); s.Panel.Run(AccountPanelAction.ReviewCloud);
            s.Clock.UtcNow = s.Clock.UtcNow.AddMinutes(6);
            s.Panel.Run(AccountPanelAction.RestoreCloud);
            Assert.That(s.Saves.Current.Wallet.Petals, Is.EqualTo(1200)); Assert.That(s.Refreshes, Is.Zero);
            Assert.That(s.Panel.MessageKey, Is.EqualTo("account.panel.changed"));
        }
        [Test] public void PhoneIdentityMismatchCannotRestoreAnotherProfile()
        {
            using var s = new Setup(); s.Auth.PlayerId = "B"; s.Cloud.PlayerId = "B";
            s.Panel.Run(AccountPanelAction.ReviewCloud);
            Assert.That(s.Panel.Preview, Is.Null); Assert.That(s.Saves.Current.CloudOwnerId, Is.EqualTo("A"));
        }
    }
}
