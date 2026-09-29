#if BLOOMLINGS_UGS && BLOOMLINGS_UGS_CLOUDSAVE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Save;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Bloomlings.Integrations.Ugs
{
    /// <summary>
    /// UGS Cloud Save (FR-087, FR-088, T139): the player data key <c>player_save_v1</c> holds the
    /// <c>player-save.v1</c> document as an object. <see cref="CloudSaveSync"/> queues and merges; this class only moves
    /// the document.
    /// </summary>
    internal sealed class UgsCloudSaveService : ICloudSaveService
    {
        private const string Key = OfflineCloudSaveService.Key;

        public bool IsAvailable => UgsBootstrap.IsSignedIn && Application.internetReachability != NetworkReachability.NotReachable;

        public IEnumerator Load(Action<CloudLoadResult> done)
        {
            Task<Dictionary<string, Item>> load = CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Key });
            Exception? error = null;
            yield return UgsBootstrap.Await(load, e => error = e);
            if (error != null)
            {
                Debug.LogWarning("[CloudSave] Load failed: " + error.Message);
                done(CloudLoadResult.Failed);
                yield break;
            }

            done(new CloudLoadResult(true, load.Result.TryGetValue(Key, out Item? item) ? item.Value.GetAs<JObject>().ToString(Newtonsoft.Json.Formatting.None) : null));
        }

        public IEnumerator Store(string json, Action<bool> done)
        {
            Task store = CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { [Key] = JObject.Parse(json) });
            Exception? error = null;
            yield return UgsBootstrap.Await(store, e => error = e);
            if (error != null)
            {
                Debug.LogWarning("[CloudSave] Store failed: " + error.Message);
            }

            done(error == null);
        }
    }
}
#endif
