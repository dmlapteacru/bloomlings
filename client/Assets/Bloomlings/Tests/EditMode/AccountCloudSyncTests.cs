using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class AccountCloudSyncTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        private sealed class Clock : IClock { public DateTime UtcNow => Now; public DateTime UtcToday => Now.Date; }

        // Models a server's atomic revisioned store. Independent devices share the Documents, never a local save.
        private sealed class Cloud : IAccountCloudSaveService
        {
            public Dictionary<string, (string Json, int Revision)> Documents = new Dictionary<string, (string, int)>();
            public bool IsAvailable { get; set; } = true;
            public string? PlayerId { get; set; } = "A";
            public long SessionVersion { get; set; }
            public Action? DuringLoad;
            public Action? DuringStore;
            public bool LoseAcknowledgement;
            public bool FailWrite;
            public int Stores;
            public bool Match(string owner, long session) => IsAvailable && owner == PlayerId && session == SessionVersion;
            public IEnumerator Load(Action<CloudLoadResult> done) => throw new AssertionException("Unscoped load");
            public IEnumerator Store(string json, Action<bool> done) => throw new AssertionException("Unscoped store");
            public IEnumerator LoadFor(string owner, long session, Action<CloudLoadResult> done)
            {
                var result = !Match(owner, session) ? CloudLoadResult.Failed : Documents.TryGetValue(owner, out var entry)
                    ? new CloudLoadResult(true, entry.Json) { WriteLock = entry.Revision.ToString() }
                    : new CloudLoadResult(true, null);
                DuringLoad?.Invoke();
                done(result);
                yield break;
            }
            public IEnumerator StoreFor(string owner, long session, string json, string? writeLock, Action<CloudStoreResult> done)
            {
                DuringStore?.Invoke();
                if (!Match(owner, session)) { done(CloudStoreResult.AccountChanged); yield break; }
                if (FailWrite) { done(CloudStoreResult.Failed); yield break; }
                bool exists = Documents.TryGetValue(owner, out var entry);
                if ((exists ? entry.Revision.ToString() : null) != writeLock)
                { done(CloudStoreResult.Conflict); yield break; }
                Documents[owner] = (json, exists ? entry.Revision + 1 : 1);
                Stores++;
                done(LoseAcknowledgement ? CloudStoreResult.Failed : CloudStoreResult.Stored);
            }
        }

        private static PlayerSave Save(string? owner = "A", int petals = 1000)
        {
            var save = PlayerSave.CreateNew(Guid.NewGuid().ToString("N"), Now);
            save.CloudOwnerId = owner;
            save.Wallet.SetPetals(petals);
            return save;
        }
        private static void Run(IEnumerator routine)
        {
            try { while (routine.MoveNext()) if (routine.Current is IEnumerator nested) Run(nested); }
            finally { (routine as IDisposable)?.Dispose(); }
        }
        private static CloudSaveSync Sync(PlayerSave save, Cloud cloud, Action? persist = null) => new CloudSaveSync(save, persist ?? (() => { }), cloud);
        private static void Upload(PlayerSave save, Cloud cloud) { var sync = Sync(save, cloud); Run(sync.Sync()); Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Uploaded)); }

        [Test]
        public void FirstGuestClaimIsDurableAndCannotBeCopiedToAnotherOwner()
        {
            var save = Save(null);
            var cloud = new Cloud { FailWrite = true };
            string? disk = null;
            var sync = Sync(save, cloud, () => disk = SaveSerializer.Write(save));
            Run(sync.Sync());
            Assert.That(SaveSerializer.Read(disk!).CloudOwnerId, Is.EqualTo("A"));
            cloud.PlayerId = "B";
            cloud.FailWrite = false;
            Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.AccountChanged));
            Assert.That(cloud.Documents.ContainsKey("B"), Is.False);
        }

        [Test]
        public void GuestCannotOverwriteAnExistingCloudAccount()
        {
            var cloud = new Cloud(); Upload(Save(), cloud);
            var guest = Save(null, 9000); var sync = Sync(guest, cloud);
            Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Conflict));
            Assert.That(guest.CloudOwnerId, Is.Null);
            Assert.That(cloud.Stores, Is.EqualTo(1));
        }

        [Test]
        public void OfflineSaveSurvivesRestartAndUploadsOnReconnect()
        {
            var cloud = new Cloud(); var save = Save(); Upload(save, cloud);
            save.Wallet.TrySpendPetals(300);
            var restarted = SaveSerializer.Read(SaveSerializer.Write(save));
            var sync = Sync(restarted, cloud); cloud.IsAvailable = false;
            Run(sync.Sync()); Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Queued));
            cloud.IsAvailable = true; Run(sync.Sync());
            Assert.That(SaveSerializer.Read(cloud.Documents["A"].Json).Wallet.Petals, Is.EqualTo(700));
            Assert.That(sync.IsDirty, Is.False);
        }

        [Test]
        public void TwoDevicesCannotCombineABoughtItemWithTheOldBalance()
        {
            var cloud = new Cloud(); var a = Save(); Upload(a, cloud);
            var b = SaveMerge.Clone(a);
            a.Wallet.TrySpendPetals(500); a.Cosmetics.Owned.Add("hat.test"); Upload(a, cloud);
            b.Progression.HighestCompletedLevel = 50;
            var sync = Sync(b, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Conflict));
            var remote = SaveSerializer.Read(cloud.Documents["A"].Json);
            Assert.That(remote.Wallet.Petals, Is.EqualTo(500));
            Assert.That(remote.Cosmetics.Owned, Does.Contain("hat.test"));
            Assert.That(b.Cosmetics.Owned, Is.Empty);
            Assert.That(b.Progression.HighestCompletedLevel, Is.EqualTo(50), "local branch is retained");
        }

        [Test]
        public void UnchangedDeviceRestoresWholeRemoteSnapshotIncludingClaimsAndWallet()
        {
            var cloud = new Cloud(); var a = Save(); Upload(a, cloud);
            var b = SaveMerge.Clone(a); string id = b.LocalPlayerId = "device-b";
            a.Wallet.TrySpendPetals(500); a.Cosmetics.Owned.Add("hat.test"); a.Daily.RewardClaimed = 2; Upload(a, cloud);
            var sync = Sync(b, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Merged));
            Assert.That((b.Wallet.Petals, b.Daily.RewardClaimed, b.LocalPlayerId), Is.EqualTo((500, 2, id)));
            Assert.That(b.Cosmetics.Owned, Does.Contain("hat.test"));
            Assert.That(sync.IsDirty, Is.False);
        }

        [Test]
        public void PurchaseWhileUploadIsInFlightRemainsQueued()
        {
            var cloud = new Cloud(); var save = Save(); Upload(save, cloud);
            save.Progression.HighestCompletedLevel = 5;
            cloud.DuringStore = () => { save.Wallet.TrySpendPetals(300); save.Cosmetics.Owned.Add("hat.test"); };
            var sync = Sync(save, cloud); Run(sync.Sync());
            Assert.That(sync.IsDirty, Is.True);
            Assert.That(save.Wallet.Petals, Is.EqualTo(700));
            cloud.DuringStore = null; Run(sync.Sync());
            var remote = SaveSerializer.Read(cloud.Documents["A"].Json);
            Assert.That(remote.Wallet.Petals, Is.EqualTo(700));
            Assert.That(remote.Cosmetics.Owned, Does.Contain("hat.test"));
            Assert.That(sync.IsDirty, Is.False);
        }

        [Test]
        public void TwoWritersCannotOverwriteTheSameRevision()
        {
            var cloud = new Cloud(); var a = Save(); Upload(a, cloud);
            var b = SaveMerge.Clone(a); a.Wallet.TrySpendPetals(500); b.Wallet.TrySpendPetals(700);
            cloud.DuringStore = () => { cloud.DuringStore = null; Upload(b, cloud); };
            var sync = Sync(a, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Conflict));
            Assert.That(SaveSerializer.Read(cloud.Documents["A"].Json).Wallet.Petals, Is.EqualTo(300));
        }

        [Test]
        public void LostAcknowledgementDoesNotUploadOrGrantTwice()
        {
            var cloud = new Cloud { LoseAcknowledgement = true }; var save = Save();
            var sync = Sync(save, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Failed));
            cloud.LoseAcknowledgement = false; Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.UpToDate));
            Assert.That(cloud.Stores, Is.EqualTo(1));
            Assert.That(save.Wallet.Petals, Is.EqualTo(1000));
        }

        [TestCase(false)] [TestCase(true)]
        public void LateLoadOrUploadCannotCrossAtoBtoA(bool upload)
        {
            var cloud = new Cloud(); var save = Save(); Upload(save, cloud);
            save.Wallet.TrySpendPetals(100);
            Action change = () => { cloud.PlayerId = "B"; cloud.SessionVersion++; cloud.PlayerId = "A"; cloud.SessionVersion++; };
            if (upload) cloud.DuringStore = change; else cloud.DuringLoad = change;
            var sync = Sync(save, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.AccountChanged));
            Assert.That(sync.IsDirty, Is.True);
            Assert.That(cloud.Stores, Is.EqualTo(1));
        }

        [Test]
        public void LocalChangeDuringLoadCannotBeOverwrittenByRemote()
        {
            var cloud = new Cloud(); var a = Save(); Upload(a, cloud); var b = SaveMerge.Clone(a);
            a.Wallet.TrySpendPetals(100); Upload(a, cloud);
            cloud.DuringLoad = () => b.Wallet.TrySpendPetals(200);
            var sync = Sync(b, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Conflict));
            Assert.That(b.Wallet.Petals, Is.EqualTo(800));
        }

        [Test]
        public void UnreadableOrForeignCloudIsNotOverwritten()
        {
            foreach (string json in new[] { "{invalid", SaveSerializer.Write(Save("B")), SaveSerializer.Write(Save()).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99") })
            {
                var cloud = new Cloud(); cloud.Documents["A"] = (json, 1);
                var sync = Sync(Save(), cloud); Run(sync.Sync());
                Assert.That(cloud.Stores, Is.Zero);
                Assert.That(sync.IsDirty, Is.True);
            }
        }

        [Test]
        public void MissingPreviouslySyncedCloudDoesNotResurrectOldCurrency()
        {
            var cloud = new Cloud(); var save = Save(); Upload(save, cloud); cloud.Documents.Clear();
            var sync = Sync(save, cloud); Run(sync.Sync());
            Assert.That(sync.LastResult, Is.EqualTo(CloudSyncResult.Conflict));
            Assert.That(cloud.Documents, Is.Empty);
        }

        [Test]
        public void LegacyUnionCannotBeUsedForAnAccountWallet()
        {
            Assert.Throws<InvalidOperationException>(() => SaveMerge.Merge(Save(), Save()));
        }

        [TestCase(false)] [TestCase(true)]
        public void RestoringBRequiresConfirmationAndNeverCopiesA(bool existing)
        {
            string root = Path.Combine(Path.GetTempPath(), "bloom-confirm-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profiles = new AccountProfiles(root, new Clock()); profiles.ClaimGuest("A");
                profiles.Current.Current.Wallet.SetPetals(9000);
                var cloud = new Cloud { PlayerId = "B" };
                if (existing) Upload(Save("B", 100), cloud);
                var restore = new AccountRestoreService(profiles, cloud);
                AccountRestoreResult result = AccountRestoreResult.Failed;
                Run(restore.Prepare(r => result = r));
                Assert.That(result, Is.EqualTo(AccountRestoreResult.Ready));
                Assert.That(profiles.Current.Current.CloudOwnerId, Is.EqualTo("A"));
                Assert.That(restore.Confirm(), Is.EqualTo(AccountRestoreResult.Applied));
                Assert.That(profiles.Current.Current.CloudOwnerId, Is.EqualTo("B"));
                Assert.That(profiles.Current.Current.Wallet.Petals, Is.EqualTo(existing ? 100 : 0));
                Assert.That(restore.Confirm(), Is.EqualTo(AccountRestoreResult.Stale));
                profiles.SelectAuthenticated("A");
                Assert.That(profiles.Current.Current.Wallet.Petals, Is.EqualTo(9000));
            }
            finally { Directory.Delete(root, true); }
        }

        [TestCase(false)] [TestCase(true)]
        public void ConfirmationExpiresIfPlayerContinuesPlayingOrChangesPlatformAccount(bool identity)
        {
            string root = Path.Combine(Path.GetTempPath(), "bloom-stale-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profiles = new AccountProfiles(root, new Clock()); profiles.ClaimGuest("A");
                var cloud = new Cloud { PlayerId = "B" };
                var restore = new AccountRestoreService(profiles, cloud); Run(restore.Prepare(_ => { }));
                if (identity) cloud.SessionVersion++;
                else profiles.Current.Current.Wallet.AddPetals(20);
                Assert.That(restore.Confirm(), Is.EqualTo(AccountRestoreResult.Stale));
                Assert.That(profiles.Current.Current.CloudOwnerId, Is.EqualTo("A"));
            }
            finally { Directory.Delete(root, true); }
        }

        [Test]
        public void CancelledRestoreCannotBeReopenedByALateResponse()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloom-cancel-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profiles = new AccountProfiles(root, new Clock()); profiles.ClaimGuest("A");
                var cloud = new Cloud { PlayerId = "B" };
                var restore = new AccountRestoreService(profiles, cloud);
                cloud.DuringLoad = restore.Cancel;
                Run(restore.Prepare(_ => { }));
                Assert.That(restore.PendingOwner, Is.Null);
                Assert.That(restore.Confirm(), Is.EqualTo(AccountRestoreResult.Stale));
            }
            finally { Directory.Delete(root, true); }
        }

        [Test]
        public void CorruptAccountFilesAreNotReplacedWithAnEmptyProfile()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloom-corrupt-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profiles = new AccountProfiles(root, new Clock()); profiles.ClaimGuest("A");
                File.WriteAllText(profiles.Current.MainPath, "broken");
                File.WriteAllText(profiles.Current.BackupPath, "broken backup");
                Assert.Throws<InvalidDataException>(() => new AccountProfiles(root, new Clock()));
                Assert.That(File.ReadAllText(profiles.Current.MainPath), Is.EqualTo("broken"));
            }
            finally { Directory.Delete(root, true); }
        }

        [Test]
        public void ProfileFoldersRetainOfflineChangesAndDelayedWritesStayWithTheirOwner()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloom-accounts-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profiles = new AccountProfiles(root, new Clock());
                profiles.Current.Current.Wallet.SetPetals(1000);
                profiles.ClaimGuest("A"); var old = profiles.Current;
                old.Current.Wallet.TrySpendPetals(300); old.Save();
                profiles.SelectAuthenticated("B");
                Assert.That(profiles.Current.Current.Wallet.Petals, Is.Zero);
                old.Current.Wallet.AddPetals(10); old.Save(); // a late A callback cannot touch B's file
                var restarted = new AccountProfiles(root, new Clock());
                Assert.That(restarted.Current.Current.CloudOwnerId, Is.EqualTo("B"));
                Assert.That(restarted.Current.Current.Wallet.Petals, Is.Zero);
                restarted.SelectAuthenticated("A");
                Assert.That(restarted.Current.Current.Wallet.Petals, Is.EqualTo(710));
                Assert.Throws<InvalidOperationException>(() => restarted.ClaimGuest("B"));
            }
            finally { Directory.Delete(root, true); }
        }

        [Test]
        public void ExplicitRestoreArchivesLocalBranchAndDoesNotUnionInventories()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloom-restore-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profiles = new AccountProfiles(root, new Clock()); profiles.ClaimGuest("A");
                profiles.Current.Current.Cosmetics.Owned.Add("hat.local");
                var remote = Save(); remote.Cosmetics.Owned.Add("hat.remote");
                profiles.RestoreConfirmed("A", remote);
                Assert.That(profiles.Current.Current.Cosmetics.Owned, Is.EquivalentTo(new[] { "hat.remote" }));
                Assert.That(Directory.GetDirectories(root, "conflicts", SearchOption.AllDirectories).Length, Is.EqualTo(1));
                Assert.Throws<InvalidOperationException>(() => profiles.RestoreConfirmed("B", Save("B")));
            }
            finally { Directory.Delete(root, true); }
        }
    }
}
