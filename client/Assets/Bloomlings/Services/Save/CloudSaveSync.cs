using System;
using System.Collections;
using Bloomlings.Content.Json;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>The outcome of one sync.</summary>
    public enum CloudSyncResult
    {
        /// <summary>Offline or signed out: the change stays queued.</summary>
        Queued,

        /// <summary>The cloud had no save; the local one was uploaded.</summary>
        Uploaded,

        /// <summary>Both saves were merged (R15), stored locally and uploaded.</summary>
        Merged,

        /// <summary>The load or the upload failed; the change stays queued for the next attempt.</summary>
        Failed,
    }

    /// <summary>
    /// Keeps the local save and the cloud save in step (research R15, FR-087, T139). While offline or signed out,
    /// changes are only queued (<see cref="IsDirty"/>). A sync loads the cloud document, merges it into the local save
    /// in place (<see cref="SaveMerge"/>: the furthest progression, all entitlements, re-applied purchases), writes the
    /// result locally and uploads it. Syncs run at sign-in, when Home opens and after a win, never in the middle of a
    /// level. A cloud document this app cannot read is left untouched and never overwritten. Engine-free.
    /// </summary>
    public sealed class CloudSaveSync
    {
        private readonly PlayerSave _save;
        private readonly Action _persist;
        private readonly ICloudSaveService _cloud;
        private bool _running;

        public CloudSaveSync(PlayerSave save, Action persist, ICloudSaveService cloud)
        {
            _save = save;
            _persist = persist;
            _cloud = cloud;
        }

        /// <summary>The merge changed the local save (progression, wallet, unlocks); listeners refresh.</summary>
        public event Action? Merged;

        /// <summary>A local change has not reached the cloud yet. Starts true so the first sync of a session runs.</summary>
        public bool IsDirty { get; private set; } = true;

        public CloudSyncResult? LastResult { get; private set; }

        /// <summary>Records a local change to upload with the next sync.</summary>
        public void MarkDirty() => IsDirty = true;

        /// <summary>Coroutine: one load → merge → store round. Overlapping calls return at once.</summary>
        public IEnumerator Sync(Action<CloudSyncResult>? done = null)
        {
            if (_running)
            {
                yield break;
            }

            _running = true;
            try
            {
                CloudSyncResult result = CloudSyncResult.Queued;
                if (_cloud.IsAvailable)
                {
                    CloudLoadResult? loaded = null;
                    yield return _cloud.Load(r => loaded = r);
                    result = CloudSyncResult.Failed;
                    if (loaded != null && loaded.Ok)
                    {
                        PlayerSave? remote = null;
                        bool readable = true;
                        if (loaded.Json != null)
                        {
                            try
                            {
                                remote = SaveSerializer.Read(loaded.Json);
                            }
                            catch (ContentFormatException)
                            {
                                // A newer schema or a damaged document: keep it for a newer app, never overwrite it.
                                readable = false;
                            }
                        }

                        if (readable)
                        {
                            if (remote != null)
                            {
                                _save.Assign(SaveMerge.Merge(_save, remote));
                                _persist();
                                Merged?.Invoke();
                            }

                            bool stored = false;
                            yield return _cloud.Store(SaveSerializer.Write(_save, indented: false), ok => stored = ok);
                            if (stored)
                            {
                                IsDirty = false;
                                result = remote != null ? CloudSyncResult.Merged : CloudSyncResult.Uploaded;
                            }
                        }
                    }
                }

                LastResult = result;
                done?.Invoke(result);
            }
            finally
            {
                _running = false;
            }
        }
    }
}
