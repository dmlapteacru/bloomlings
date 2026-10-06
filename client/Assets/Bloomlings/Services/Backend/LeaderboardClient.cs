using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>
    /// The game side of the leaderboard (FR-062, T140, T141). It unlocks at L10 (<c>system.leaderboard</c>) behind the
    /// remote flag <c>feature.leaderboard</c>. Each win queues the highest completed level; the queue is the gap between
    /// that level and the stats counter <c>leaderboard.submittedLevel</c>, so it survives restarts and offline play and
    /// is submitted on reconnect. The last page read is kept for Home and the Leaderboard screen, marked stale when a
    /// refresh fails. Engine-free.
    /// </summary>
    public sealed class LeaderboardClient
    {
        public const string UnlockId = "system.leaderboard";
        public const string SubmittedCounter = "leaderboard.submittedLevel";
        /// <summary>
        /// The players read above and below the player (and the top read, twice as many plus one): five, so the Leaderboard
        /// page's rows fill its panel on every phone (eleven lines on 21:9; the owner's choice of 2026-10-04).
        /// </summary>
        public const int Neighbours = 5;

        private readonly PlayerSave _save;
        private readonly ILeaderboardService _service;
        private readonly IRemoteConfigService _config;
        private readonly IClock _clock;
        private readonly Action _persist;
        private string _commandLogHash = string.Empty;
        private int _contentVersion = 1;
        private bool _submitting;

        public LeaderboardClient(PlayerSave save, ILeaderboardService service, IRemoteConfigService config, IClock clock, Action persist)
        {
            _save = save;
            _service = service;
            _config = config;
            _clock = clock;
            _persist = persist;
        }

        public event Action? Updated;

        public bool IsUnlocked => _save.Unlocks.IsSet(UnlockId) && _config.Get(RemoteConfigKeys.LeaderboardEnabled);

        public LeaderboardPage? LastPage { get; private set; }

        public DateTime? LastFetchedUtc { get; private set; }

        /// <summary>The page on show was not refreshed by the last attempt (offline).</summary>
        public bool IsStale { get; private set; } = true;

        public int SubmittedLevel => (int)(_save.Stats.Counters.TryGetValue(SubmittedCounter, out long level) ? level : 0);

        public bool HasPendingSubmission => _save.Progression.HighestCompletedLevel > SubmittedLevel;

        /// <summary>The SHA-256 of a winning command log (one <see cref="CommandText"/> line per command).</summary>
        public static string CommandLogHash(IEnumerable<Command> commands)
        {
            var text = new StringBuilder();
            foreach (Command command in commands)
            {
                text.Append(CommandText.Format(command)).Append('\n');
            }

            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text.ToString()));
        }

        /// <summary>A win: remembers the proof for the submission of the new highest level.</summary>
        public void OnLevelWon(int contentVersion, string commandLogHash)
        {
            _contentVersion = contentVersion;
            _commandLogHash = commandLogHash;
        }

        /// <summary>Coroutine: submits the highest completed level if it was not submitted yet.</summary>
        public IEnumerator SubmitPending()
        {
            if (!IsUnlocked || !HasPendingSubmission || !_service.IsAvailable || _submitting)
            {
                yield break;
            }

            _submitting = true;
            int level = _save.Progression.HighestCompletedLevel;
            SubmitResult? result = null;
            yield return _service.Submit(level, _contentVersion, _commandLogHash, r => result = r);
            _submitting = false;
            if (result != null && result.Accepted)
            {
                _save.Stats.Counters[SubmittedCounter] = Math.Max(SubmittedLevel, level);
                _persist();
            }
        }

        /// <summary>Coroutine: submits what is pending, then reads the player's rank and neighbours.</summary>
        public IEnumerator Refresh()
        {
            if (!IsUnlocked)
            {
                yield break;
            }

            yield return SubmitPending();
            LeaderboardPage? page = null;
            if (_service.IsAvailable)
            {
                yield return _service.Fetch(Neighbours, p => page = p);
            }

            if (page != null)
            {
                LastPage = page;
                LastFetchedUtc = _clock.UtcNow;
                IsStale = false;
            }
            else
            {
                IsStale = true;
            }

            Updated?.Invoke();
        }
    }
}
