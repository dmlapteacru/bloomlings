using System;
using System.Collections;
using System.IO;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>
    /// Connects the in-game account panel to current authentication and conditional save sync.
    /// Restoration here is deliberately the current authenticated owner only (or an unbound guest). Occupied platform identities do not sign out or switch profiles.
    /// A separate explicit platform account-switch flow is needed before restoring another owner's profile.
    /// </summary>
    public sealed class CloudAccountPanelGateway : IAccountPanelGateway
    {
        private readonly PlayerSave _save;
        private readonly SaveService _saves;
        private readonly IAuthService _auth;
        private readonly ICloudSaveService _cloud;
        private readonly CloudSaveSync _sync;
        private readonly IClock _clock;
        private readonly Action<IEnumerator> _run;
        private readonly Action _restored;
        private Prepared? _prepared;
        private bool _busy;
        private sealed record Prepared(string Token, string Session, string LocalHash, string RemoteHash, DateTime ExpiresAt);

        public CloudAccountPanelGateway(PlayerSave save, SaveService saves, IAuthService auth, ICloudSaveService cloud,
            CloudSaveSync sync, IClock clock, Action<IEnumerator> run, Action restored)
        { _save = save; _saves = saves; _auth = auth; _cloud = cloud; _sync = sync; _clock = clock; _run = run; _restored = restored; }

        private string Session => _save.LocalPlayerId + ":" + _auth.PlayerId + ":" +
            (_cloud is IAccountCloudSaveService c ? c.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) : "local");
        private bool SameOwner => _auth.IsSignedIn && !string.IsNullOrWhiteSpace(_auth.PlayerId) && (_save.CloudOwnerId == null || _save.CloudOwnerId == _auth.PlayerId)
            && _cloud is IAccountCloudSaveService c && c.PlayerId == _auth.PlayerId;
        public AccountPanelSnapshot Read()
        {
            bool mismatch = _save.CloudOwnerId != null && _save.CloudOwnerId != _auth.PlayerId;
            AccountSaveState state = mismatch ? AccountSaveState.Unavailable : !_auth.IsSignedIn ? AccountSaveState.Device :
                _sync.LastResult == CloudSyncResult.Conflict ? AccountSaveState.Conflict :
                _sync.LastResult == CloudSyncResult.NeedsInitialization || _sync.LastResult == CloudSyncResult.UnsupportedProvider ? AccountSaveState.Unavailable :
                _sync.IsDirty || _sync.IsRunning ? AccountSaveState.Pending : AccountSaveState.Cloud;
            bool canUse = !_busy && !_sync.IsRunning && !mismatch;
            return new AccountPanelSnapshot(Session, _saves.WriteVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), _save.Profile.Name ?? "",
                _save.Progression.HighestCompletedLevel + 1, _save.Wallet.Petals,
                _save.LinkedIdentity == "apple" ? "account.linked_apple" : _save.LinkedIdentity == "google_play_games" ? "account.linked_google" : "account.panel.guest",
                state, canUse && _save.LinkedIdentity == null && _auth.CanLink(LinkProvider.GooglePlayGames),
                canUse && _save.LinkedIdentity == null && _auth.CanLink(LinkProvider.Apple),
                canUse && _auth.IsSignedIn && _cloud.IsAvailable,
                canUse && SameOwner && _cloud.IsAvailable && state == AccountSaveState.Conflict);
        }
        public void Execute(AccountPanelAction action, string? token, Action<AccountPanelReply> done)
        {
            if (_busy || _sync.IsRunning) { done(new AccountPanelReply("account.panel.retry")); return; }
            _busy = true;
            try { _run(Guard(Work(action, token, done), done)); }
            catch { _busy = false; throw; }
        }
        // Catch coroutine failures including nested provider iterators, not just their construction.
        private IEnumerator Guard(IEnumerator routine, Action<AccountPanelReply> done)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    bool more; object? current = null;
                    try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                    catch { done(new AccountPanelReply("account.panel.retry")); yield break; }
                    if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator child) stack.Push(child);
                    else yield return current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); _busy = false; }
        }
        private IEnumerator Work(AccountPanelAction action, string? token, Action<AccountPanelReply> done)
        {
            string session = Session;
            string localId = _save.LocalPlayerId;
            string? boundOwner = _save.CloudOwnerId;
            if (action == AccountPanelAction.Google || action == AccountPanelAction.Apple)
            {
                _prepared = null;
                if (_save.CloudOwnerId != null && _save.CloudOwnerId != _auth.PlayerId)
                { done(new AccountPanelReply("account.panel.changed")); yield break; }
                LinkProvider provider = action == AccountPanelAction.Apple ? LinkProvider.Apple : LinkProvider.GooglePlayGames;
                if (!_auth.CanLink(provider)) { done(new AccountPanelReply("account.panel.unavailable")); yield break; }
                if (!_auth.IsSignedIn)
                {
                    bool signed = false;
                    yield return _auth.SignIn(ok => signed = ok);
                    if (!signed) { done(new AccountPanelReply("account.panel.retry")); yield break; }
                    // Signing in may restore a different cached owner. Never hand it a bound local profile.
                    if (_save.LocalPlayerId != localId || _save.CloudOwnerId != boundOwner || (_save.CloudOwnerId != null && _save.CloudOwnerId != _auth.PlayerId))
                    { done(new AccountPanelReply("account.panel.changed")); yield break; }
                    session = Session;
                }
                bool linked = false;
                yield return _auth.Link(provider, ok => linked = ok);
                if (session != Session) { done(new AccountPanelReply("account.panel.changed")); yield break; }
                if (linked)
                {
                    _save.LinkedIdentity = LinkedIdentities.Name(provider); _saves.Save();
                    yield return _sync.Sync();
                    if (session != Session) { done(new AccountPanelReply("account.panel.changed")); yield break; }
                    done(new AccountPanelReply("account.panel.linked", CompletedLinkSession: Session));
                }
                else done(new AccountPanelReply(_auth is IAccountLinkStatus status && status.LastLinkResult == AccountLinkResult.ExistingAccount ? "account.panel.existing" : "account.panel.retry"));
                yield break;
            }
            if (action == AccountPanelAction.Sync)
            {
                _prepared = null;
                if (!_auth.IsSignedIn || !_cloud.IsAvailable || (_save.CloudOwnerId != null && _save.CloudOwnerId != _auth.PlayerId))
                { done(new AccountPanelReply("account.panel.unavailable")); yield break; }
                CloudSyncResult result = CloudSyncResult.Failed;
                yield return _sync.Sync(r => result = r);
                done(new AccountPanelReply(result == CloudSyncResult.UpToDate || result == CloudSyncResult.Uploaded || result == CloudSyncResult.Merged
                    ? "account.panel.saved_cloud" : result == CloudSyncResult.Conflict ? "account.panel.conflict_detail" : "account.panel.retry"));
                yield break;
            }
            // Never let a changed phone identity turn a review into an account switch.
            if (!SameOwner || !_cloud.IsAvailable || !(_cloud is IAccountCloudSaveService cloud))
            { _prepared = null; done(new AccountPanelReply("account.panel.changed")); yield break; }
            string owner = cloud.PlayerId!;
            long authSession = cloud.SessionVersion;
            string localHash = CloudSnapshot.Fingerprint(_save);
            Prepared? prepared = _prepared;
            if (action == AccountPanelAction.RestoreCloud)
            {
                _prepared = null; // single use, including failed/stale confirmations
                if (prepared == null || token != prepared.Token || prepared.Session != session || prepared.LocalHash != localHash || _clock.UtcNow > prepared.ExpiresAt)
                { done(new AccountPanelReply("account.panel.changed")); yield break; }
            }
            else if (action != AccountPanelAction.ReviewCloud)
            { done(new AccountPanelReply("account.panel.unavailable")); yield break; }
            CloudLoadResult? loaded = null;
            yield return cloud.LoadFor(owner, authSession, r => loaded = r);
            if (Session != session || !SameOwner || cloud.SessionVersion != authSession || localHash != CloudSnapshot.Fingerprint(_save))
            { _prepared = null; done(new AccountPanelReply("account.panel.changed")); yield break; }
            PlayerSave? remote = null;
            try { if (loaded?.Ok == true && loaded.Json != null) remote = SaveSerializer.Read(loaded.Json); }
            catch { /* An unsupported/corrupt cloud document is not a restore candidate. */ }
            if (remote == null || remote.CloudOwnerId != owner)
            { _prepared = null; done(new AccountPanelReply("account.panel.retry")); yield break; }
            string remoteHash = CloudSnapshot.Fingerprint(remote);
            if (action == AccountPanelAction.ReviewCloud)
            {
                _prepared = new Prepared(Guid.NewGuid().ToString("N"), session, localHash, remoteHash, _clock.UtcNow.AddMinutes(5));
                done(new AccountPanelReply("account.panel.restore_detail", new AccountCloudPreview(_prepared.Token, remote.Profile.Name ?? "", remote.Progression.HighestCompletedLevel + 1, remote.Wallet.Petals)));
            }
            else
            {
                if (prepared!.RemoteHash != remoteHash)
                { done(new AccountPanelReply("account.panel.changed")); yield break; }
                string archive = Path.Combine(Path.GetDirectoryName(_saves.MainPath)!, "conflicts", Guid.NewGuid().ToString("N"));
                var backup = new SaveService(archive, _clock);
                backup.Current.Assign(_save); backup.Save(); // durable before replacing the active snapshot
                _sync.InvalidateSession();
                CloudSnapshot.Restore(_save, remote); _saves.Save();
                _restored(); // all services keep the same PlayerSave object; refresh derived progression/views
                yield return _sync.Sync();
                done(new AccountPanelReply("account.panel.restored"));
            }
        }
    }
}
