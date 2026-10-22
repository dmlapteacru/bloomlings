using System;

namespace Bloomlings.Client.UI.Design
{
    public enum AccountSaveState { Device, Pending, Cloud, Conflict, Unavailable }
    public enum AccountPanelAction { Google, Apple, Sync, ReviewCloud, RestoreCloud }

    /// <summary>Host-provided facts. SessionKey changes on every identity/profile switch, including A → B → A.</summary>
    public sealed record AccountPanelSnapshot(string SessionKey, string ProgressStamp, string Name, int Level,
        int Petals, string IdentityKey, AccountSaveState SaveState, bool CanGoogle = false,
        bool CanApple = false, bool CanSync = false, bool CanReviewCloud = false);

    /// <summary>An opaque, expiring restore token. The host must also validate it at the point of mutation.</summary>
    public sealed record AccountCloudPreview(string Token, string Name, int Level, int Petals);
    public sealed record AccountPanelReply(string MessageKey, AccountCloudPreview? Preview = null, string? CompletedLinkSession = null);

    public interface IAccountPanelGateway
    {
        AccountPanelSnapshot Read();
        /// <summary>Never copies accounts on link. Restore requires the exact prepared token and revalidates owner/session/local progress.</summary>
        void Execute(AccountPanelAction action, string? token, Action<AccountPanelReply> done);
    }

    public sealed class DelegateAccountPanelGateway : IAccountPanelGateway
    {
        private readonly Func<AccountPanelSnapshot> _read;
        private readonly Action<AccountPanelAction, string?, Action<AccountPanelReply>> _execute;
        public DelegateAccountPanelGateway(Func<AccountPanelSnapshot> read,
            Action<AccountPanelAction, string?, Action<AccountPanelReply>> execute) { _read = read; _execute = execute; }
        public AccountPanelSnapshot Read() => _read();
        public void Execute(AccountPanelAction action, string? token, Action<AccountPanelReply> done) => _execute(action, token, done);
    }

    /// <summary>Shared in-game account UX, independent of the renderer and SDK. No simulated login or wallet mutation.</summary>
    public sealed class AccountPanel
    {
        private readonly IAccountPanelGateway _gateway;
        private long _request, _view;
        private string? _previewSession, _previewStamp;
        private AccountCloudPreview? _preview;
        public AccountPanel(IAccountPanelGateway gateway) => _gateway = gateway;
        public bool Busy { get; private set; }
        public string? MessageKey { get; private set; }
        public AccountPanelSnapshot Snapshot => _gateway.Read();
        public AccountCloudPreview? Preview
        {
            get
            {
                AccountPanelSnapshot s = Snapshot;
                if (s.SessionKey != _previewSession || s.ProgressStamp != _previewStamp) _preview = null;
                return _preview;
            }
        }
        public event Action? Changed;
        public string StatusKey => Busy ? "account.panel.syncing" : Snapshot.SaveState switch
        {
            AccountSaveState.Cloud => "account.panel.saved_cloud",
            AccountSaveState.Pending => "account.panel.pending",
            AccountSaveState.Conflict => "account.panel.conflict",
            AccountSaveState.Unavailable => "account.panel.unavailable",
            _ => "account.panel.saved_device",
        };
        public string DetailKey => Snapshot.SaveState switch
        {
            AccountSaveState.Cloud => "account.panel.cloud_detail",
            AccountSaveState.Pending => "account.panel.pending_detail",
            AccountSaveState.Conflict => "account.panel.conflict_detail",
            _ => "account.panel.device_detail",
        };
        public bool Can(AccountPanelAction action)
        {
            if (Busy) return false;
            AccountPanelSnapshot s = Snapshot;
            return action switch
            {
                AccountPanelAction.Google => s.CanGoogle,
                AccountPanelAction.Apple => s.CanApple,
                AccountPanelAction.Sync => s.CanSync,
                AccountPanelAction.ReviewCloud => s.CanReviewCloud,
                AccountPanelAction.RestoreCloud => s.CanReviewCloud && Preview != null,
                _ => false,
            };
        }
        public void Open() { _view++; CancelPreview(); MessageKey = null; }
        public void Close() { _view++; CancelPreview(); }
        public void CancelPreview() { _preview = null; Changed?.Invoke(); }
        public void Run(AccountPanelAction action)
        {
            if (!Can(action)) return;
            AccountPanelSnapshot before = Snapshot;
            string? token = action == AccountPanelAction.RestoreCloud ? Preview?.Token : null;
            if (action == AccountPanelAction.RestoreCloud && token == null) return;
            _preview = null;
            MessageKey = null;
            Busy = true;
            long attempt = ++_request;
            long view = _view;
            Changed?.Invoke();
            bool completed = false;
            void Complete(AccountPanelReply reply)
            {
                if (completed || attempt != _request) return;
                completed = true;
                Busy = false;
                if (view != _view) { Changed?.Invoke(); return; }
                AccountPanelSnapshot now = Snapshot;
                // A late callback never presents A's result on B's panel, even after returning to A.
                bool intentionalLink = (action == AccountPanelAction.Google || action == AccountPanelAction.Apple)
                    && reply.Preview == null && reply.CompletedLinkSession == now.SessionKey;
                if (now.SessionKey != before.SessionKey && !intentionalLink) { MessageKey = "account.panel.changed"; Changed?.Invoke(); return; }
                MessageKey = reply.MessageKey;
                if (reply.Preview != null && now.ProgressStamp == before.ProgressStamp)
                {
                    _preview = reply.Preview; _previewSession = now.SessionKey; _previewStamp = now.ProgressStamp;
                }
                else if (reply.Preview != null) MessageKey = "account.panel.changed";
                Changed?.Invoke();
            }
            try { _gateway.Execute(action, token, Complete); }
            catch { Complete(new AccountPanelReply("account.panel.retry")); }
        }
    }
}
