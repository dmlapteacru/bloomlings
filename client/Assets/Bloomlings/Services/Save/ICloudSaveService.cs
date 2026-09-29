using System;
using System.Collections;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>The answer of a cloud load: <see cref="Ok"/> with the stored document, or null when nothing is stored yet.</summary>
    public sealed record CloudLoadResult(bool Ok, string? Json)
    {
        public static CloudLoadResult Failed { get; } = new CloudLoadResult(false, null);
    }

    /// <summary>
    /// Cloud storage of the player save (contracts/backend-services.md, FR-087, FR-088): one key,
    /// <c>player_save_v1</c>, holding the <c>player-save.v1</c> document. The initial provider is UGS Cloud Save
    /// (<c>Integrations/Ugs</c>). Coroutines report through their callbacks and never throw.
    /// </summary>
    public interface ICloudSaveService
    {
        /// <summary>Signed in and able to reach the service now.</summary>
        bool IsAvailable { get; }

        IEnumerator Load(Action<CloudLoadResult> done);

        IEnumerator Store(string json, Action<bool> done);
    }

    /// <summary>The offline fallback: never available, so the local save is the only copy.</summary>
    public sealed class OfflineCloudSaveService : ICloudSaveService
    {
        public const string Key = "player_save_v1";

        public bool IsAvailable => false;

        public IEnumerator Load(Action<CloudLoadResult> done)
        {
            done(CloudLoadResult.Failed);
            yield break;
        }

        public IEnumerator Store(string json, Action<bool> done)
        {
            done(false);
            yield break;
        }
    }
}
