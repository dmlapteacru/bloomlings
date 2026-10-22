using System;
using System.Collections;
using Bloomlings.Content.Json;

namespace Bloomlings.Client.Services.Save
{
    public enum AccountRestoreResult { Ready, Applied, Offline, Failed, Stale, UnsupportedSave }

    /// <summary>
    /// Two-step account restoration, after explicit platform sign-in. Preparing does not change the active profile;
    /// confirmation is invalidated by further local play or an authentication/profile change. A new identity gets
    /// an empty profile, never a copy of the previous owner's inventory. Hosts rebuild services after Applied.
    /// </summary>
    public sealed class AccountRestoreService
    {
        private readonly AccountProfiles _profiles;
        private readonly IAccountCloudSaveService _cloud;
        private Pending? _pending;
        private bool _loading;
        private long _request;
        public AccountRestoreService(AccountProfiles profiles, IAccountCloudSaveService cloud)
        { _profiles = profiles; _cloud = cloud; }

        public string? PendingOwner => _pending?.Owner;
        public int? PendingLevel => _pending?.Remote?.Progression.HighestCompletedLevel;
        public bool PendingIsNew => _pending != null && _pending.Remote == null;

        private sealed record Pending(string Owner, long AuthSession, long ProfileSession, string LocalId, string LocalHash, PlayerSave? Remote);

        public void Cancel() { _pending = null; _request++; }

        public IEnumerator Prepare(Action<AccountRestoreResult> done)
        {
            if (_loading) { done(AccountRestoreResult.Failed); yield break; }
            _loading = true;
            long attempt = ++_request;
            _pending = null;
            try
            {
                string? owner = _cloud.PlayerId;
                if (!_cloud.IsAvailable || string.IsNullOrWhiteSpace(owner))
                { done(AccountRestoreResult.Offline); yield break; }
                var request = new Pending(owner!, _cloud.SessionVersion, _profiles.SessionVersion,
                    _profiles.Current.Current.LocalPlayerId, CloudSnapshot.Fingerprint(_profiles.Current.Current), null);
                CloudLoadResult? loaded = null;
                yield return _cloud.LoadFor(owner!, request.AuthSession, result => loaded = result);
                if (attempt != _request || !Matches(request)) { done(AccountRestoreResult.Stale); yield break; }
                if (loaded == null || !loaded.Ok) { done(AccountRestoreResult.Failed); yield break; }
                PlayerSave? remote = null;
                if (loaded.Json != null)
                {
                    try { remote = SaveSerializer.Read(loaded.Json); }
                    catch (ContentFormatException) { done(AccountRestoreResult.UnsupportedSave); yield break; }
                    if (remote.CloudOwnerId != owner) { done(AccountRestoreResult.UnsupportedSave); yield break; }
                }
                _pending = request with { Remote = remote };
                done(AccountRestoreResult.Ready);
            }
            finally { _loading = false; }
        }

        public AccountRestoreResult Confirm()
        {
            Pending? pending = _pending;
            _pending = null; // a confirmation can be consumed only once
            if (pending == null || !Matches(pending)) return AccountRestoreResult.Stale;
            _profiles.SelectAuthenticated(pending.Owner);
            if (pending.Remote != null) _profiles.RestoreConfirmed(pending.Owner, pending.Remote);
            return AccountRestoreResult.Applied;
        }

        private bool Matches(Pending pending) => _cloud.PlayerId == pending.Owner
            && _cloud.SessionVersion == pending.AuthSession && _profiles.SessionVersion == pending.ProfileSession
            && _profiles.Current.Current.LocalPlayerId == pending.LocalId
            && CloudSnapshot.Fingerprint(_profiles.Current.Current) == pending.LocalHash;
    }
}
