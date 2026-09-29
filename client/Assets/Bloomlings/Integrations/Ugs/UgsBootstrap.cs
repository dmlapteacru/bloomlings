#if BLOOMLINGS_UGS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
#if BLOOMLINGS_UGS_REMOTECONFIG
using Newtonsoft.Json.Linq;
using Unity.Services.RemoteConfig;
#endif

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>
    /// Unity Gaming Services (research R11; T126, T138–T140). Compiled when the Authentication package is installed;
    /// each further package (Remote Config, Cloud Save, Leaderboards with Cloud Code) adds its service. Everything is
    /// registered in <see cref="ServiceProviders"/> before the first scene. Sign-in is anonymous and never blocks play
    /// (FR-087).
    /// </summary>
    internal static class UgsBootstrap
    {
        private static Task? _signIn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ServiceProviders.Auth = () => new UgsAuthService();
#if BLOOMLINGS_UGS_REMOTECONFIG
            ServiceProviders.RemoteConfigFetch = FetchRemoteConfig;
#endif
#if BLOOMLINGS_UGS_CLOUDSAVE
            ServiceProviders.CloudSave = () => new UgsCloudSaveService();
#endif
#if BLOOMLINGS_UGS_LEADERBOARDS && BLOOMLINGS_UGS_CLOUDCODE
            ServiceProviders.Leaderboard = () => new UgsLeaderboardService();
#endif
        }

        /// <summary>True when UGS is initialized and a player is signed in.</summary>
        internal static bool IsSignedIn =>
            UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;

        /// <summary>Initializes UGS and signs in anonymously (or restores the cached player); concurrent callers share one attempt.</summary>
        internal static Task EnsureSignedInAsync()
        {
            if (IsSignedIn)
            {
                return Task.CompletedTask;
            }

            if (_signIn == null || _signIn.IsCompleted)
            {
                _signIn = SignInAsync();
            }

            return _signIn;
        }

        /// <summary>Runs a task as a coroutine; <paramref name="done"/> gets the faulted exception or null.</summary>
        internal static IEnumerator Await(Task task, Action<Exception?> done)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            done(task.IsFaulted || task.IsCanceled ? (Exception?)task.Exception?.GetBaseException() ?? new OperationCanceledException() : null);
        }

        private static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

#if BLOOMLINGS_UGS_REMOTECONFIG
        private static IEnumerator FetchRemoteConfig(Action<IReadOnlyDictionary<string, string>> onValues)
        {
            Task<RuntimeConfig> fetch = FetchAsync();
            while (!fetch.IsCompleted)
            {
                yield return null;
            }

            if (fetch.IsFaulted || fetch.IsCanceled)
            {
                Debug.LogWarning("[RemoteConfig] Fetch failed; the bundled defaults stay. " + fetch.Exception?.GetBaseException().Message);
                yield break;
            }

            // Raw strings by key; the client parses and clamps them (UgsRemoteConfigService).
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (JProperty property in fetch.Result.config.Properties())
            {
                values[property.Name] = property.Value.Type == JTokenType.Boolean
                    ? ((bool)property.Value ? "true" : "false")
                    : property.Value.ToString();
            }

            onValues(values);
        }

        private static async Task<RuntimeConfig> FetchAsync()
        {
            await EnsureSignedInAsync();
            return await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
        }

        private struct UserAttributes
        {
        }

        private struct AppAttributes
        {
        }
#endif
    }
}
#endif
