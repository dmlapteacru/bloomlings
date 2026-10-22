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

        private sealed class Auth : IAuthService
        {
            public bool IsSignedIn => true;
            public string? PlayerId { get; set; } = "A";
            public string? LinkedIdentity => "google_play_games";
            public bool CanLink(LinkProvider provider) => false;
            public IEnumerator SignIn(Action<bool> done) { done(true); yield break; }
            public IEnumerator Link(LinkProvider provider, Action<bool> done) { done(false); yield break; }
        }

        [Test]
        public void DifferentAuthenticatedOwnerCannotSubmitTheLocalPlayersProgress()
        {
            var save = PlayerSave.CreateNew("local", T0);
            save.CloudOwnerId = "A";
            save.Progression.HighestCompletedLevel = 100;
            save.Unlocks.Flags[LeaderboardClient.UnlockId] = true;
            var board = new FakeBoard { IsAvailable = true };
            var auth = new Auth { PlayerId = "B" };
            var client = new LeaderboardClient(save, board, new BundledRemoteConfigService(), new FixedClock(), () => { }, auth);
            Run(client.Refresh());
            Assert.That(board.Submitted, Is.Zero);
            Assert.That(client.LastPage, Is.Null);
            auth.PlayerId = "A";
            Run(client.Refresh());
            Assert.That(board.Submitted, Is.EqualTo(100));
            auth.PlayerId = "B";
            Run(client.Refresh());
            Assert.That(client.LastPage, Is.Null, "do not show A's cached position as B's");
        }

        [Test]
        public void Leaderboard_UnlocksAtL10_AndSubmitsThePendingLevelOnReconnect()
        {
            PlayerSave save = PlayerSave.CreateNew("p1", T0);
            var board = new FakeBoard();
            var client = new LeaderboardClient(save, board, new BundledRemoteConfigService(), new FixedClock(), () => { });
            save.Progression.HighestCompletedLevel = 9;
            Assert.That(client.IsUnlocked, Is.False, "hidden before L10");

            save.Unlocks.Flags[LeaderboardClient.UnlockId] = true;
            save.Progression.HighestCompletedLevel = 14;
            client.OnLevelWon(1, "abc");
            Run(client.Refresh());
            Assert.That(client.HasPendingSubmission, Is.True, "queued while offline");
            Assert.That((client.IsStale, client.LastPage), Is.EqualTo((true, (LeaderboardPage?)null)));

            board.IsAvailable = true;
            Run(client.Refresh());
            Assert.That(board.Submitted, Is.EqualTo(14));
            Assert.That(client.HasPendingSubmission, Is.False);
            Assert.That((client.IsStale, client.LastPage!.Player!.Rank), Is.EqualTo((false, 1234)));

            board.IsAvailable = false;
            Run(client.Refresh());
            Assert.That((client.IsStale, client.LastPage!.Player!.Rank), Is.EqualTo((true, 1234)), "the last rank stays, marked stale");
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
