#if BLOOMLINGS_UGS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>
    /// Unity Gaming Services (research R11; T126). Compiled only when the Remote Config package is installed; registers
    /// the Remote Config fetch before the first scene. Sign-in is anonymous and never blocks play (FR-087).
    /// </summary>
    internal static class UgsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ServiceProviders.RemoteConfigFetch = FetchRemoteConfig;
        }

        /// <summary>Initializes UGS and signs in anonymously if needed.</summary>
        internal static async Task EnsureSignedInAsync()
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
    }
}
#endif
