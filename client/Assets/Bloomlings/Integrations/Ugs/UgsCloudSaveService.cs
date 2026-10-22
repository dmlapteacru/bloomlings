#if BLOOMLINGS_UGS && BLOOMLINGS_UGS_CLOUDSAVE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Save;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>SDK adapter. Never uses an unconditional write or writes under a newly selected identity.</summary>
    internal sealed class UgsCloudSaveService : IAccountCloudSaveService
    {
        private const string Key = OfflineCloudSaveService.Key;
        public bool IsAvailable => UgsBootstrap.IsSignedIn && Application.internetReachability != NetworkReachability.NotReachable;
        public string? PlayerId => UgsBootstrap.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        public long SessionVersion => UgsBootstrap.AuthSessionVersion;
        private bool Matches(string owner, long session) => IsAvailable && PlayerId == owner && SessionVersion == session;

        public IEnumerator Load(Action<CloudLoadResult> done)
        {
            done(CloudLoadResult.Failed); // legacy unscoped API deliberately fails closed
            yield break;
        }
        public IEnumerator Store(string json, Action<bool> done)
        {
            done(false);
            yield break;
        }

        public IEnumerator LoadFor(string owner, long session, Action<CloudLoadResult> done)
        {
            if (!Matches(owner, session)) { done(CloudLoadResult.Failed); yield break; }
            Task<Dictionary<string, Item>> load = CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Key });
            Exception? error = null;
            yield return UgsBootstrap.Await(load, e => error = e);
            if (error != null || !Matches(owner, session)) { done(CloudLoadResult.Failed); yield break; }
            CloudLoadResult result;
            try
            {
                result = load.Result.TryGetValue(Key, out Item? item)
                    ? new CloudLoadResult(true, item.Value.GetAs<JObject>().ToString(Newtonsoft.Json.Formatting.None)) { WriteLock = item.WriteLock }
                    : new CloudLoadResult(true, null);
            }
            catch (Exception) { result = CloudLoadResult.Failed; }
            done(result);
        }

        public IEnumerator StoreFor(string owner, long session, string json, string? writeLock, Action<CloudStoreResult> done)
        {
            if (!Matches(owner, session)) { done(CloudStoreResult.AccountChanged); yield break; }
            // A null Unity write lock means an unconditional write, NOT create-if-absent.
            // Provision new records through the server bootstrap before enabling live sync.
            if (string.IsNullOrEmpty(writeLock)) { done(CloudStoreResult.NeedsInitialization); yield break; }
            Task store;
            try
            {
                var document = JObject.Parse(json);
                if ((string?)document["cloudOwnerId"] != owner) { done(CloudStoreResult.AccountChanged); yield break; }
                store = CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, SaveItem> { [Key] = new SaveItem(document, writeLock) });
            }
            catch (Exception) { done(CloudStoreResult.Failed); yield break; }
            Exception? error = null;
            yield return UgsBootstrap.Await(store, e => error = e);
            // A rejected write (including stale write lock) stays queued; the next load distinguishes divergence.
            done(!Matches(owner, session) ? CloudStoreResult.AccountChanged : error == null ? CloudStoreResult.Stored : CloudStoreResult.Failed);
        }
    }
}
#endif
