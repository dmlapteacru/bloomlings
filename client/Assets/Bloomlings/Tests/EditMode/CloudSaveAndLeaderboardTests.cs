using System;
using System.Collections;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Simulation;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Offline queueing and merge-on-reconnect of the cloud save (R15, T139); the leaderboard queue (T140, T141).</summary>
    public class CloudSaveAndLeaderboardTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

        private sealed class FakeCloud : ICloudSaveService
        {
            public bool IsAvailable { get; set; }

            public string? Stored { get; set; }

            public bool FailStore { get; set; }

            public int Stores { get; private set; }

            public IEnumerator Load(Action<CloudLoadResult> done)
            {
                done(IsAvailable ? new CloudLoadResult(true, Stored) : CloudLoadResult.Failed);
                yield break;
            }

            public IEnumerator Store(string json, Action<bool> done)
            {
                if (!FailStore)
                {
                    Stored = json;
                    Stores++;
                }

                done(!FailStore);
                yield break;
            }
        }

        private sealed class FakeBoard : ILeaderboardService
        {
            public bool IsAvailable { get; set; }

            public bool Reject { get; set; }

            public int Submitted { get; private set; }

            public IEnumerator Submit(int level, int contentVersion, string commandLogHash, Action<SubmitResult> done)
            {
                if (!Reject)
                {
                    Submitted = level;
                }

                done(new SubmitResult(!Reject, Reject ? "implausible-jump" : null));
                yield break;
            }

            public IEnumerator Fetch(int neighbours, Action<LeaderboardPage?> done)
            {
                var player = new LeaderboardEntry(1234, "You", Submitted, true);
                done(IsAvailable ? new LeaderboardPage(new[] { player }, player) : null);
                yield break;
            }
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => T0;

            public DateTime UtcToday => T0.Date;
        }

        /// <summary>Runs a coroutine to the end, entering nested coroutines as Unity does.</summary>
        private static void Run(IEnumerator routine)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                }
                else if (top.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                }
            }
        }

        [Test]
        public void Offline_QueuesTheChange_AndReconnectMergesAndUploads()
        {
            PlayerSave local = PlayerSave.CreateNew("local", T0);
            local.Progression.HighestCompletedLevel = 12;
            local.Wallet.AddPetals(40);
            PlayerSave other = PlayerSave.CreateNew("other", T0.AddMinutes(-30));
            other.Progression.HighestCompletedLevel = 30;
            other.Wallet.AddPetals(90);
            other.Purchases.RemoveAds = true;

            var cloud = new FakeCloud { Stored = SaveSerializer.Write(other, indented: false) };
            int persisted = 0;
            var sync = new CloudSaveSync(local, () => persisted++, cloud);

            CloudSyncResult? result = null;
            Run(sync.Sync(r => result = r));
            Assert.That(result, Is.EqualTo(CloudSyncResult.Queued));
            Assert.That(sync.IsDirty, Is.True);
            Assert.That(local.Progression.HighestCompletedLevel, Is.EqualTo(12), "nothing changes offline");

            cloud.IsAvailable = true;
            bool merged = false;
            sync.Merged += () => merged = true;
            Run(sync.Sync(r => result = r));
            Assert.That(result, Is.EqualTo(CloudSyncResult.Merged));
            Assert.That(merged, Is.True);
            Assert.That(sync.IsDirty, Is.False);
            Assert.That(persisted, Is.EqualTo(1), "the merged save is written locally");
            Assert.That((local.Progression.HighestCompletedLevel, local.Wallet.Petals, local.Purchases.RemoveAds), Is.EqualTo((30, 90, true)), "merged in place");
            Assert.That(local.LocalPlayerId, Is.EqualTo("local"));
            Assert.That(SaveSerializer.Read(cloud.Stored!).Progression.HighestCompletedLevel, Is.EqualTo(30), "the merge is uploaded");
        }

        [Test]
        public void EmptyCloud_GetsTheLocalSave_AndAFailedUploadStaysQueued()
        {
            PlayerSave local = PlayerSave.CreateNew("local", T0);
            var cloud = new FakeCloud { IsAvailable = true, FailStore = true };
            var sync = new CloudSaveSync(local, () => { }, cloud);

            CloudSyncResult? result = null;
            Run(sync.Sync(r => result = r));
            Assert.That(result, Is.EqualTo(CloudSyncResult.Failed));
            Assert.That(sync.IsDirty, Is.True);

            cloud.FailStore = false;
            Run(sync.Sync(r => result = r));
            Assert.That(result, Is.EqualTo(CloudSyncResult.Uploaded));
            Assert.That(cloud.Stores, Is.EqualTo(1));
        }

        [Test]
        public void UnreadableCloudDocument_IsNeverOverwritten()
        {
            PlayerSave local = PlayerSave.CreateNew("local", T0);
            string newer = SaveSerializer.Write(local, indented: false).Replace("\"schemaVersion\":1", "\"schemaVersion\":2");
            var cloud = new FakeCloud { IsAvailable = true, Stored = newer };
            var sync = new CloudSaveSync(local, () => { }, cloud);

            CloudSyncResult? result = null;
            Run(sync.Sync(r => result = r));
            Assert.That(result, Is.EqualTo(CloudSyncResult.Failed));
            Assert.That(cloud.Stored, Is.EqualTo(newer));
        }

        [Test]
        public void Leaderboard_UnlocksAtL10_AndSubmitsThePendingLevelOnReconnect()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", T0);
            var board = new FakeBoard();
            var client = new LeaderboardClient(save, board, new BundledRemoteConfigService(), new FixedClock(), () => { });
            save.Progression.HighestCompletedLevel = 9;
            Assert.That(client.RankText, Is.Null, "hidden before L10");

            save.Unlocks.Flags[LeaderboardClient.UnlockId] = true;
            save.Progression.HighestCompletedLevel = 14;
            client.OnLevelWon(1, "abc");
            Run(client.Refresh());
            Assert.That(client.HasPendingSubmission, Is.True, "queued while offline");
            Assert.That(client.RankText, Is.EqualTo("Rank: offline"));

            board.IsAvailable = true;
            Run(client.Refresh());
            Assert.That(board.Submitted, Is.EqualTo(14));
            Assert.That(client.HasPendingSubmission, Is.False);
            Assert.That(client.RankText, Is.EqualTo("Rank #1,234"));

            board.IsAvailable = false;
            Run(client.Refresh());
            Assert.That(client.RankText, Is.EqualTo("Rank #1,234 (offline)"), "the last rank stays with a stale label");
        }

        [Test]
        public void CommandLogHash_IsStableAndOrderSensitive()
        {
            Command[] log = { new TapPod("p1"), new TapPod("p2") };
            string hash = LeaderboardClient.CommandLogHash(log);

            Assert.That(hash, Has.Length.EqualTo(64));
            Assert.That(LeaderboardClient.CommandLogHash(new Command[] { new TapPod("p1"), new TapPod("p2") }), Is.EqualTo(hash));
            Assert.That(LeaderboardClient.CommandLogHash(new Command[] { new TapPod("p2"), new TapPod("p1") }), Is.Not.EqualTo(hash));
        }

        [Test]
        public void RejectedSubmission_StaysPending()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", T0);
            save.Unlocks.Flags[LeaderboardClient.UnlockId] = true;
            save.Progression.HighestCompletedLevel = 300;
            var board = new FakeBoard { IsAvailable = true, Reject = true };
            var client = new LeaderboardClient(save, board, new BundledRemoteConfigService(), new FixedClock(), () => { });

            Run(client.SubmitPending());
            Assert.That(client.HasPendingSubmission, Is.True);
            Assert.That(client.SubmittedLevel, Is.Zero);
        }
    }
}
