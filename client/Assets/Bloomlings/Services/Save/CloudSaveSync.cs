using System;
using System.Collections;
using Bloomlings.Content.Json;

namespace Bloomlings.Client.Services.Save
{
    public enum CloudSyncResult
    {
        Queued, Uploaded,
        // Kept for source compatibility: now means a whole remote snapshot was restored, not a union.
        Merged,
        Failed, Conflict, AccountChanged, NeedsInitialization, UnsupportedProvider, UpToDate,
    }

    /// <summary>
    /// Owner-scoped, conditional snapshot sync. A durable ancestor fingerprint distinguishes a local-only change
    /// from a remote-only change. Divergent snapshots stop for reconciliation; currency/inventory are NEVER unioned.
    /// No timestamps or highest-level comparisons determine which wallet wins.
    /// </summary>
    public sealed class CloudSaveSync
    {
        private readonly PlayerSave _save;
        private readonly Action _persist;
        private readonly ICloudSaveService _cloud;
        private bool _running;
        private long _epoch;

        public CloudSaveSync(PlayerSave save, Action persist, ICloudSaveService cloud)
        {
            _save = save;
            _persist = persist;
            _cloud = cloud;
        }

        public event Action? Merged;
        public event Action? StateChanged;
        public bool IsDirty { get; private set; } = true;
        public bool IsRunning => _running;
        public CloudSyncResult? LastResult { get; private set; }
        public void MarkDirty() => IsDirty = true;

        /// <summary>Call before any explicit profile switch, including A → B → A.</summary>
        public void InvalidateSession()
        {
            _epoch++;
            IsDirty = true;
            LastResult = CloudSyncResult.AccountChanged;
            StateChanged?.Invoke();
        }

        public IEnumerator Sync(Action<CloudSyncResult>? done = null)
        {
            if (_running) yield break;
            _running = true;
            try
            {
                if (!_cloud.IsAvailable) { Complete(CloudSyncResult.Queued, done); yield break; }
                if (!(_cloud is IAccountCloudSaveService cloud))
                { Complete(CloudSyncResult.UnsupportedProvider, done); yield break; }
                string? owner = cloud.PlayerId;
                long session = cloud.SessionVersion;
                long epoch = _epoch;
                string localId = _save.LocalPlayerId;
                bool Current() => epoch == _epoch && localId == _save.LocalPlayerId
                    && session == cloud.SessionVersion && owner == cloud.PlayerId
                    && (_save.CloudOwnerId == null || owner == _save.CloudOwnerId);
                if (string.IsNullOrWhiteSpace(owner) || !Current())
                { Complete(CloudSyncResult.AccountChanged, done); yield break; }

                CloudLoadResult? loaded = null;
                yield return cloud.LoadFor(owner!, session, r => loaded = r);
                if (!Current()) { Complete(CloudSyncResult.AccountChanged, done); yield break; }
                if (loaded == null || !loaded.Ok) { Complete(CloudSyncResult.Failed, done); yield break; }
                PlayerSave? remote = null;
                if (loaded.Json != null)
                {
                    try { remote = SaveSerializer.Read(loaded.Json); }
                    catch (ContentFormatException) { Complete(CloudSyncResult.Failed, done); yield break; }
                    // Legacy documents require deliberate migration; never adopt another profile by guesswork.
                    if (remote.CloudOwnerId != owner)
                    { Complete(CloudSyncResult.Conflict, done); yield break; }
                }
                if (_save.CloudOwnerId == null)
                {
                    if (remote != null) { Complete(CloudSyncResult.Conflict, done); yield break; }
                    _save.CloudOwnerId = owner;
                    _persist(); // bind durably BEFORE any first upload; a retry cannot give this guest to B
                }
                string localHash = CloudSnapshot.Fingerprint(_save);
                string? remoteHash = remote == null ? null : CloudSnapshot.Fingerprint(remote);
                if (remoteHash == localHash)
                {
                    _save.CloudBaseHash = localHash;
                    _persist();
                    IsDirty = CloudSnapshot.Fingerprint(_save) != localHash;
                    Complete(CloudSyncResult.UpToDate, done);
                    yield break;
                }
                if (remote != null && _save.CloudBaseHash == localHash)
                {
                    CloudSnapshot.Restore(_save, remote);
                    _persist();
                    IsDirty = CloudSnapshot.Fingerprint(_save) != remoteHash;
                    Merged?.Invoke();
                    Complete(CloudSyncResult.Merged, done);
                    yield break;
                }
                if ((remote != null && remoteHash != _save.CloudBaseHash)
                    || (remote == null && _save.CloudBaseHash != null))
                { Complete(CloudSyncResult.Conflict, done); yield break; }

                // Capture BEFORE yielding. Local changes during upload remain dirty and keep their own data.
                string snapshot = SaveSerializer.Write(_save, false);
                CloudStoreResult stored = CloudStoreResult.Failed;
                yield return cloud.StoreFor(owner!, session, snapshot, loaded.WriteLock, r => stored = r);
                if (!Current()) { Complete(CloudSyncResult.AccountChanged, done); yield break; }
                if (stored == CloudStoreResult.Stored)
                {
                    _save.CloudBaseHash = localHash;
                    _persist();
                    IsDirty = CloudSnapshot.Fingerprint(_save) != localHash;
                    Complete(CloudSyncResult.Uploaded, done);
                }
                else Complete(stored switch
                {
                    CloudStoreResult.Conflict => CloudSyncResult.Conflict,
                    CloudStoreResult.AccountChanged => CloudSyncResult.AccountChanged,
                    CloudStoreResult.NeedsInitialization => CloudSyncResult.NeedsInitialization,
                    _ => CloudSyncResult.Failed,
                }, done);
            }
            finally { _running = false; }
        }

        private void Complete(CloudSyncResult result, Action<CloudSyncResult>? done)
        {
            LastResult = result;
            if (result != CloudSyncResult.Uploaded && result != CloudSyncResult.Merged && result != CloudSyncResult.UpToDate)
                IsDirty = true;
            StateChanged?.Invoke();
            done?.Invoke(result);
        }
    }
}
