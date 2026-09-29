using System;
using System.Collections;
using System.Collections.Generic;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>One row of the leaderboard: the rank is 1-based; the level is decoded from the score.</summary>
    /// <summary>
    /// One leaderboard row (data-model §3.2). <paramref name="Score"/> is the time-encoded score
    /// (<see cref="LeaderboardScore"/>), 0 when unknown; <see cref="ReachedAtUtc"/> is decoded from it.
    /// </summary>
    public sealed record LeaderboardEntry(int Rank, string Name, int Level, bool IsPlayer, double Score = 0)
    {
        public System.DateTime? ReachedAtUtc => Score > 0 ? LeaderboardScore.CompletedAt(Score) : (System.DateTime?)null;
    }

    /// <summary>The player's row and a small window of neighbours around it.</summary>
    public sealed record LeaderboardPage(IReadOnlyList<LeaderboardEntry> Entries, LeaderboardEntry? Player);

    /// <summary>The answer of a submission: accepted, or rejected by a sanity check (logged server-side, never banned).</summary>
    public sealed record SubmitResult(bool Accepted, string? Reason)
    {
        public static SubmitResult Unreachable { get; } = new SubmitResult(false, "unreachable");
    }

    /// <summary>
    /// The global leaderboard <c>global_highest_level</c> (contracts/backend-services.md, FR-062, T140). Progress is
    /// submitted through the Cloud Code function <c>SubmitProgress(level, contentVersion, commandLogHash)</c>, which runs
    /// the sanity checks and writes the score (<see cref="LeaderboardScore"/>); the board is read directly. The initial
    /// provider is UGS Leaderboards (<c>Integrations/Ugs</c>). Coroutines report through their callbacks and never throw.
    /// </summary>
    public interface ILeaderboardService
    {
        bool IsAvailable { get; }

        IEnumerator Submit(int level, int contentVersion, string commandLogHash, Action<SubmitResult> done);

        /// <summary>The player's rank with up to <paramref name="neighbours"/> players above and below; null on failure.</summary>
        IEnumerator Fetch(int neighbours, Action<LeaderboardPage?> done);
    }

    /// <summary>The offline fallback: the leaderboard is hidden or shows its stale label.</summary>
    public sealed class OfflineLeaderboardService : ILeaderboardService
    {
        public bool IsAvailable => false;

        public IEnumerator Submit(int level, int contentVersion, string commandLogHash, Action<SubmitResult> done)
        {
            done(SubmitResult.Unreachable);
            yield break;
        }

        public IEnumerator Fetch(int neighbours, Action<LeaderboardPage?> done)
        {
            done(null);
            yield break;
        }
    }
}
