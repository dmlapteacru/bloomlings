#if BLOOMLINGS_UGS && BLOOMLINGS_UGS_LEADERBOARDS && BLOOMLINGS_UGS_CLOUDCODE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Backend;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using Entry = Bloomlings.Client.Services.Backend.LeaderboardEntry;

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>
    /// UGS Leaderboards (FR-062, T140): submissions go through the Cloud Code function <c>SubmitProgress</c>, which runs
    /// the sanity checks and writes the score of <c>global_highest_level</c> server-side; the board is read with the
    /// Leaderboards SDK. UGS ranks are 0-based; the game shows them 1-based.
    /// </summary>
    internal sealed class UgsLeaderboardService : ILeaderboardService
    {
        public const string LeaderboardId = "global_highest_level";

        public bool IsAvailable => UgsBootstrap.IsSignedIn && Application.internetReachability != NetworkReachability.NotReachable;

        public IEnumerator Submit(int level, int contentVersion, string commandLogHash, Action<SubmitResult> done)
        {
            var args = new Dictionary<string, object>
            {
                ["level"] = level,
                ["contentVersion"] = contentVersion,
                ["commandLogHash"] = commandLogHash,
            };
            Task<SubmitAnswer> call = CloudCodeService.Instance.CallEndpointAsync<SubmitAnswer>("SubmitProgress", args);
            Exception? error = null;
            yield return UgsBootstrap.Await(call, e => error = e);
            if (error != null)
            {
                Debug.LogWarning("[Leaderboard] Submit failed: " + error.Message);
                done(SubmitResult.Unreachable);
                yield break;
            }

            done(new SubmitResult(call.Result.accepted, call.Result.reason));
        }

        public IEnumerator Fetch(int neighbours, Action<LeaderboardPage?> done)
        {
            Task<LeaderboardPage?> fetch = FetchAsync(neighbours);
            Exception? error = null;
            yield return UgsBootstrap.Await(fetch, e => error = e);
            if (error != null)
            {
                Debug.LogWarning("[Leaderboard] Fetch failed: " + error.Message);
                done(null);
                yield break;
            }

            done(fetch.Result);
        }

        private static async Task<LeaderboardPage?> FetchAsync(int neighbours)
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            var entries = new List<Entry>();
            Entry? player = null;
            try
            {
                LeaderboardScoresPage range = await LeaderboardsService.Instance.GetPlayerRangeAsync(LeaderboardId, new GetPlayerRangeOptions { RangeLimit = neighbours });
                foreach (Unity.Services.Leaderboards.Models.LeaderboardEntry row in range.Results)
                {
                    var entry = new Entry(row.Rank + 1, row.PlayerName, LeaderboardScore.LevelOf(row.Score), row.PlayerId == playerId, row.Score);
                    entries.Add(entry);
                    if (entry.IsPlayer)
                    {
                        player = entry;
                    }
                }
            }
            catch (Exception ex) when (ex is Unity.Services.Leaderboards.Exceptions.LeaderboardsException)
            {
                // No score yet: show the top of the board instead.
                LeaderboardScoresPage top = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = (2 * neighbours) + 1 });
                foreach (Unity.Services.Leaderboards.Models.LeaderboardEntry row in top.Results)
                {
                    entries.Add(new Entry(row.Rank + 1, row.PlayerName, LeaderboardScore.LevelOf(row.Score), false, row.Score));
                }
            }

            return new LeaderboardPage(entries, player);
        }

        [Serializable]
        private sealed class SubmitAnswer
        {
            public bool accepted;
            public string? reason;
            public int? rank;
        }
    }
}
#endif
