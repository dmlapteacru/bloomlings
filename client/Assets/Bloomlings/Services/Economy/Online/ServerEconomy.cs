using System;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>One authoritative read model for the existing meta services. Local grants are disabled by EconomyService.</summary>
    public sealed class ServerEconomy : IAuthoritativeEconomy, IDisposable
    {
        private readonly OnlineEconomyClient _client;
        private readonly Action<IEnumerator> _run;
        private PlayerSave? _save;
        private Action _persist = () => { };
        private bool _syncing, _disposed;
        private string? _projectionStamp;
        private readonly bool _activationApproved;
        public ServerEconomy(OnlineEconomyClient client, Action<IEnumerator> run, bool activationApproved = false) { _client = client; _run = run; _activationApproved = activationApproved; }
        public string OwnerId => _client.OwnerId;
        public bool IsOnline => !_disposed && _client.IsOnline;
        public int Petals => _client.Confirmed?.Petals ?? 0;
        private static string Kind(BoosterKind kind) { string text = kind.ToString(); return char.ToLowerInvariant(text[0]) + text.Substring(1); }
        public int Charges(BoosterKind kind) => _client.AvailableCharges(Kind(kind));
        public int Price(BoosterKind kind) => _client.Catalog.Booster(Kind(kind))?.Price ?? int.MaxValue;
        public int ItemPrice(string id) => _client.Catalog.Item(id)?.Price ?? 0;
        public string? UtcDay => _client.ServerUtcDate;
        public int DailyClaimed => UtcDay != null && _client.Confirmed?.DailyDate == UtcDay ? _client.Confirmed.DailyStep : 0;
        public bool CanClaimDaily => IsOnline && _client.CanStartPurchase && UtcDay != null && (DailyClaimed == 0 || DailyClaimed == 3);
        public void Bind(PlayerSave save, Action persist)
        {
            if (_save != null || save.CloudOwnerId != OwnerId || (save.ServerEconomyOwnerId != null && save.ServerEconomyOwnerId != OwnerId))
                throw new InvalidOperationException("Server economy requires an explicitly bound, matching account.");
            if (save.ServerEconomyOwnerId == null && (!_activationApproved || !_client.CanStartPurchase || _client.Confirmed == null
                || _client.Confirmed.HighestCompletedLevel != save.Progression.HighestCompletedLevel))
                throw new InvalidOperationException("First activation requires verified provisioning and an approved progress migration.");
            if (_client.Confirmed == null) throw new InvalidOperationException("A server profile requires its durable authoritative cache.");
            _save = save; _persist = persist;
            save.ServerEconomyOwnerId = OwnerId; _persist(); // sticky BEFORE projection: restart cannot use a local wallet
            _client.Changed += Project;
            Project();
        }
        private void Project()
        {
            if (_disposed || _save == null) return;
            if (_save.CloudOwnerId != OwnerId || _save.ServerEconomyOwnerId != OwnerId) throw new InvalidOperationException("Economy owner changed.");
            string stamp = (_client.Confirmed?.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none") + ":" + _client.Catalog.Version;
            foreach (BoosterKind kind in Enum.GetValues(typeof(BoosterKind))) stamp += ":" + Charges(kind).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_projectionStamp == stamp) return; // quote/status changes must not fsync the same save on the UI thread
            _save.Wallet.SetPetals(Petals);
            foreach (BoosterKind kind in Enum.GetValues(typeof(BoosterKind))) _save.Boosters.Set(kind, Charges(kind));
            _save.Cosmetics.Owned.Clear();
            EconomySnapshot? snapshot = _client.Confirmed;
            if (snapshot != null)
            {
                foreach (string id in snapshot.OwnedIds) _save.Cosmetics.Owned.Add(id);
                foreach (var item in _client.Catalog.Items) if (item.Free) _save.Cosmetics.Owned.Add(item.Id);
                var milestones = new MilestonesData();
                for (int level = 1; level <= snapshot.HighestCompletedLevel; level++) if (snapshot.HasMilestone(level)) milestones.TryClaim(level);
                _save.Milestones.Assign(milestones);
                _save.Daily.RewardLastClaimUtcDate = snapshot.DailyDate;
                _save.Daily.RewardClaimed = snapshot.DailyStep;
                _save.Daily.ChallengeLastCompletedUtcDate = snapshot.ChallengeDate;
                _save.Daily.FreeBoosterAdUtcDate = snapshot.BoosterAdDate;
            }
            _persist();
            _projectionStamp = stamp;
        }
        public bool TryTakeCharge(BoosterKind kind) => !_disposed && _client.QueueBoosterUse(Kind(kind));
        public bool RequestDaily(int step)
        {
            // Ad steps require a separate verified SSV ticket; local AdStub never authorizes them.
            if (!CanClaimDaily || (step != 1 && step != 4) || !_client.QueueDaily(step)) return false;
            Synchronize(); return true;
        }
        public bool RecordWin(string claimId, int level, string evidenceJson, bool challenge = false) => !_disposed && _client.RecordWin(claimId, level, evidenceJson, challenge);
        public void Synchronize()
        {
            if (_disposed || _syncing || !_client.IsOnline) return;
            _syncing = true;
            try { _run(Sync()); } catch { _syncing = false; throw; }
        }
        private IEnumerator Sync()
        {
            try
            {
                yield return _client.Refresh();
                if (_client.LastCode != "loaded") yield break;
                yield return _client.Flush();
                if (_client.PendingCount != 0 || _client.Confirmed == null) yield break;
                // Milestones follow VERIFIED progress, not the local display level or an unverified offline win.
                for (int level = 25; level <= _client.Confirmed.HighestCompletedLevel; level += 25)
                    if (MilestoneTable.Default.CadenceFor(level) != null) _client.QueueMilestone(level);
                if (_client.PendingCount > 0) yield return _client.Flush();
            }
            finally { _syncing = false; }
        }
        public void Dispose() { if (_disposed) return; _disposed = true; _client.Changed -= Project; _client.InvalidateSession(); }
    }
}
