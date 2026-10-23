using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>
    /// Cached read model and durable owner-scoped outbox. Authoritative currency is only replaced by validated
    /// snapshots; offline reward evidence/booster reservations never mint money or permanent inventory.
    /// </summary>
    public sealed class OnlineEconomyClient : IDisposable
    {
        private readonly EconomyJournal _journal;
        private readonly IEconomyTransport _transport;
        private EconomyCatalog _catalog;
        private EconomySnapshot? _snapshot;
        private long? _verifiedSession;
        private long? _serverNextSequence;
        private bool _busy;
        private bool _disposed;
        private long _epoch;
        public event Action? Changed;
        public string OwnerId => _journal.OwnerId;
        public EconomyCatalog Catalog => _catalog;
        public EconomySnapshot? Confirmed => _snapshot;
        public int PendingCount => _journal.PendingCount;
        public string? LastCode { get; private set; }
        public bool IsOnline => !_disposed && Current;
        public string SessionKey => OwnerId + ":" + _transport.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + _epoch.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public long? NextOperationSequence => _journal.NextSequence;
        public long? LastAcknowledgedSequence => (long?)_journal.Data["lastResult"]?["sequence"];
        public bool? LastAcknowledgedOk => (bool?)_journal.Data["lastResult"]?["ok"];
        public PendingEconomyPurchase? PendingPurchase
        {
            get
            {
                if (PendingCount == 0) return null;
                JObject op = Operation((JObject)((JArray)_journal.Data["pending"]!)[0]);
                return IsPurchase(op) ? new PendingEconomyPurchase((long)op["sequence"]!, (string)op["kind"]!, (string)(op["itemId"] ?? op["boosterKind"])!) : null;
            }
        }
        public string? LastAcknowledgedCode => _journal.Data["lastResult"] is JObject result ? (string?)result["code"] : null;
        public IReadOnlyList<string> UnsettledRewardIds => ((JObject)_journal.Data["rewards"]!).Properties().Where(p => (bool?)p.Value["settled"] != true).Select(p => p.Name).ToArray();
        public string RewardEvidence(string claimId) => _journal.ReadRewardEvidence(claimId);
        public bool IsBusy => _busy;
        public bool CanStartPurchase => !_disposed && !_busy && Current && _verifiedSession == _transport.SessionVersion && _snapshot != null && PendingCount == 0 && _journal.NextSequence.HasValue && _serverNextSequence == _journal.NextSequence;
        private bool Current => _transport.IsAvailable && _transport.PlayerId == OwnerId;
        public OnlineEconomyClient(EconomyJournal journal, IEconomyTransport transport)
        { _journal = journal; _transport = transport; _catalog = journal.Catalog; _snapshot = journal.Snapshot; }
        public bool Owns(string itemId) => Catalog.Item(itemId)?.Free == true || _snapshot?.Owns(itemId) == true;
        public string? Equipped(string slot) => (string?)((JObject)_journal.Data["outfit"]!)[slot];

        public bool Equip(Family family, string itemId, CosmeticCatalog definitions)
        {
            if (_disposed || !Owns(itemId) || !definitions.TryGet(itemId, out CosmeticItem? item) || item == null || item.Retired || !item.IsWorn || !item.Fits(family)) return false;
            string slot = family.ToString().ToLowerInvariant() + "." + CosmeticCatalog.KindName(item.Kind);
            _journal.Commit(doc => ((JObject)doc["outfit"]!)[slot] = itemId);
            Changed?.Invoke(); return true;
        }
        public int AvailableCharges(string kind)
        {
            int available = _snapshot?.Charges(kind) ?? 0;
            foreach (JObject entry in (JArray)_journal.Data["pending"]!)
            {
                JObject op = Operation(entry);
                if ((string?)op["kind"] == "consume_booster" && (string?)op["boosterKind"] == kind) available--;
            }
            return Math.Max(0, available);
        }
        public bool QueueBoosterUse(string kind)
        {
            if (_disposed || !EconomyProtocol.Boosters.Contains(kind) || AvailableCharges(kind) <= 0) return false;
            Enqueue("consume_booster", new JObject { ["boosterKind"] = kind }, null); return true;
        }

        public bool RecordWin(string claimId, int level, string evidenceJson, bool challenge = false)
        {
            if (_disposed) return false;
            EconomyProtocol.Require(EconomyProtocol.IsId(claimId) && level >= 1 && level <= 2500, "invalid reward intent");
            JObject rewards = (JObject)_journal.Data["rewards"]!;
            if (rewards[claimId] != null) return false;
            string hash = _journal.StoreEvidence(evidenceJson); // durable proof before the index; crash can leave only an orphan
            _journal.Commit(doc => ((JObject)doc["rewards"]!)[claimId] = new JObject { ["kind"] = challenge ? "claim_challenge" : "claim_level", ["level"] = level, ["evidenceHash"] = hash, ["settled"] = false });
            Changed?.Invoke(); return true;
        }
        public bool QueueAuthorizedWin(string claimId, string ticketId)
        {
            if (_disposed) return false;
            EconomyProtocol.Require(EconomyProtocol.IsId(ticketId), "invalid ticket");
            JObject? reward = ((JObject)_journal.Data["rewards"]!)[claimId] as JObject;
            if (reward == null || (bool)reward["settled"]! || ((JArray)_journal.Data["pending"]!).OfType<JObject>().Any(e => (string?)e["rewardId"] == claimId)) return false;
            _journal.ReadRewardEvidence(claimId); // do not queue a claim whose evidence was lost/corrupted
            string kind = (string)reward["kind"]!;
            var fields = new JObject { ["ticketId"] = ticketId };
            if (kind == "claim_level") fields["level"] = reward["level"]!.DeepClone();
            Enqueue(kind, fields, claimId); return true;
        }
        public bool RewardSettled(string claimId) => (bool?)((JObject)_journal.Data["rewards"]!)[claimId]?["settled"] == true;

        public IEnumerator Refresh(Action<string>? done = null)
        {
            if (_busy) { Finish("busy", done); yield break; }
            if (!Current || _disposed) { _verifiedSession = null; Finish("offline", done); yield break; }
            _busy = true;
            long session = _transport.SessionVersion, epoch = _epoch;
            try
            {
                EconomyReply? reply = null;
                yield return Request(() => _transport.Read(OwnerId, session, _journal.InstallationId, r => reply = r), () => reply = EconomyReply.Unavailable);
                if (!Matches(session, epoch)) { _verifiedSession = null; Finish("account-changed", done); yield break; }
                if (reply == null || !reply.Ok || reply.Consumed || reply.State == null || reply.Catalog == null || reply.State.OwnerId != OwnerId)
                { _verifiedSession = null; Finish(reply != null && !reply.Ok ? reply.Code : "invalid-response", done); yield break; }
                if (!Acceptable(reply.State)) { _verifiedSession = null; Finish("stale-response", done); yield break; }
                _journal.Commit(doc =>
                {
                    doc["snapshot"] = reply.State.ToJson(); doc["catalog"] = reply.Catalog.ToJson();
                    // A read never drops an existing request. If no queue remains, recover a rolled-back local cursor.
                    if (((JArray)doc["pending"]!).Count == 0)
                    {
                        long? local = doc["nextSequence"]?.Type == JTokenType.Null ? null : (long?)doc["nextSequence"];
                        if (reply.NextSequence == null || (local.HasValue && reply.NextSequence > local))
                            doc["nextSequence"] = reply.NextSequence.HasValue ? new JValue(reply.NextSequence.Value) : JValue.CreateNull();
                    }
                });
                Reload(); _serverNextSequence = reply.NextSequence; _verifiedSession = session;
                Finish("loaded", done);
            }
            finally { _busy = false; }
        }

        public IEnumerator Purchase(EconomyQuote quote, Action<string>? done = null)
        {
            if (!CanStartPurchase) { Finish(!Current ? "offline" : PendingCount > 0 ? "pending-operations" : "refresh-required", done); yield break; }
            if (!ValidQuote(quote)) { Finish("price-changed", done); yield break; }
            if (_serverNextSequence != _journal.NextSequence) { Finish("sequence-conflict", done); yield break; }
            JObject fields = quote.Kind == "buy_cosmetic" ? new JObject { ["itemId"] = quote.Target }
                : new JObject { ["boosterKind"] = quote.Target };
            if (quote.Kind == "consume_booster") fields["autoBuy"] = true;
            long sequence = Enqueue(quote.Kind, fields, null);
            yield return Drain(sequence, done); // explicit consent exists only in this invocation, never in a persisted flag
        }

        public IEnumerator ConfirmPendingPurchase(EconomyQuote quote, Action<string>? done = null)
        {
            if (_busy || _disposed || !Current || _verifiedSession != _transport.SessionVersion || PendingCount == 0)
            { Finish("refresh-required", done); yield break; }
            JObject entry = (JObject)((JArray)_journal.Data["pending"]!)[0];
            JObject op = Operation(entry);
            long sequence = (long)op["sequence"]!;
            if (!IsPurchase(op) || !ValidQuote(quote) || (string?)op["kind"] != quote.Kind || (string?)(op["itemId"] ?? op["boosterKind"]) != quote.Target)
            { Finish("price-changed", done); yield break; }
            if ((bool)entry["attempted"]! && !(bool)entry["uncertain"]! && (string?)entry["blockCode"] != "catalog-changed")
            { yield return Flush(done); yield break; } // unknown outcomes can only replay the ORIGINAL request
            if (_serverNextSequence != sequence) { yield return Flush(done); yield break; }
            string prior = (string)entry["operation"]!;
            op["catalogVersion"] = Catalog.Version;
            string replacement = CanonicalJson.Write(op, false);
            if (replacement != prior && ((JArray)entry["alternatives"]!).Count >= 4) { Finish("manual-reconciliation", done); yield break; }
            _journal.Commit(doc =>
            {
                JObject head = (JObject)((JArray)doc["pending"]!)[0];
                if (replacement != prior) ((JArray)head["alternatives"]!).Add(prior);
                head["operation"] = replacement; head["attempted"] = false; head["uncertain"] = false; head["blockCode"] = null;
            });
            yield return Drain(sequence, done);
        }
        /// <summary>Rebind only a definitely rejected/unattempted NON-purchase after an online read.
        /// Reward callers obtain a fresh trusted ticket; the original request stays as a replay candidate.</summary>
        public IEnumerator ReconcileNonPurchase(string? freshTicketId = null, Action<string>? done = null)
        {
            if (_busy || _disposed || !Current || _verifiedSession != _transport.SessionVersion || PendingCount == 0)
            { Finish("refresh-required", done); yield break; }
            JObject head = (JObject)((JArray)_journal.Data["pending"]!)[0];
            JObject op = Operation(head);
            if (IsPurchase(op)) { Finish("confirmation-required", done); yield break; }
            long sequence = (long)op["sequence"]!;
            if (_serverNextSequence != sequence || (((bool)head["attempted"]! || (bool)head["uncertain"]!) && (string?)head["blockCode"] != "catalog-changed"))
            { yield return Flush(done); yield break; }
            if ((string?)op["kind"] == "claim_level" || (string?)op["kind"] == "claim_challenge")
            {
                if (!EconomyProtocol.IsId(freshTicketId)) { Finish("authorization-required", done); yield break; }
                op["ticketId"] = freshTicketId;
            }
            op["catalogVersion"] = Catalog.Version;
            string replacement = CanonicalJson.Write(op, false), prior = (string)head["operation"]!;
            if (replacement != prior && ((JArray)head["alternatives"]!).Count >= 4) { Finish("manual-reconciliation", done); yield break; }
            _journal.Commit(doc =>
            {
                JObject entry = (JObject)((JArray)doc["pending"]!)[0];
                if (replacement != prior) ((JArray)entry["alternatives"]!).Add(prior);
                entry["operation"] = replacement; entry["attempted"] = false; entry["uncertain"] = false; entry["blockCode"] = null;
            });
            yield return Flush(done);
        }

        public IEnumerator Flush(Action<string>? done = null) => Drain(null, done);

        private IEnumerator Drain(long? permittedNewPurchase, Action<string>? done)
        {
            if (_busy) { Finish("busy", done); yield break; }
            if (!Current || _disposed) { Finish("offline", done); yield break; }
            _busy = true;
            long session = _transport.SessionVersion, epoch = _epoch;
            try
            {
                int budget = 32; // bounded work; no tight retry loop on a failed network request
                while (PendingCount > 0 && budget-- > 0)
                {
                    if (!Matches(session, epoch)) { Finish("account-changed", done); yield break; }
                    JObject head = (JObject)((JArray)_journal.Data["pending"]!)[0];
                    JObject operation = Operation(head);
                    long sequence = (long)operation["sequence"]!;
                    bool alreadyProcessed = _verifiedSession == session && (_serverNextSequence == null || _serverNextSequence > sequence);
                    if (head["blockCode"]?.Type == JTokenType.String && !alreadyProcessed) { Finish((string)head["blockCode"]!, done); yield break; }
                    if (IsPurchase(operation) && (!(bool)head["attempted"]! || (bool)head["uncertain"]!) && !alreadyProcessed && permittedNewPurchase != sequence)
                    { Finish("confirmation-required", done); yield break; }
                    _journal.Commit(doc => ((JObject)((JArray)doc["pending"]!)[0])["attempted"] = true);
                    var candidates = new List<string> { (string)head["operation"]! };
                    foreach (JToken old in (JArray)head["alternatives"]!) candidates.Add(old.Value<string>()!);
                    EconomyReply? reply = null;
                    foreach (string json in candidates)
                    {
                        reply = null;
                        yield return Request(() => _transport.Execute(OwnerId, session, json, r => reply = r), () => reply = EconomyReply.Unavailable);
                        if (!Matches(session, epoch)) { Finish("account-changed", done); yield break; }
                        if (reply?.Code != "sequence-reused" || reply.Consumed) break;
                    }
                    if (reply == null) reply = EconomyReply.Unavailable;
                    if (reply.Consumed)
                    {
                        if (reply.State == null || reply.State.OwnerId != OwnerId || !Acceptable(reply.State)
                            || (reply.NextSequence.HasValue && reply.NextSequence.Value <= sequence))
                        { Finish("invalid-acknowledgement", done); yield break; }
                        _journal.Commit(doc =>
                        {
                            doc["snapshot"] = reply.State.ToJson();
                            JArray pending = (JArray)doc["pending"]!;
                            string? rewardId = (string?)((JObject)pending[0])["rewardId"];
                            if (rewardId != null && (reply.Ok || reply.Code == "already-claimed" || reply.Code == "older-date-already-superseded"))
                                ((JObject)((JObject)doc["rewards"]!)[rewardId]!)["settled"] = true;
                            pending.RemoveAt(0);
                            if (pending.Count == 0) doc["nextSequence"] = reply.NextSequence.HasValue ? new JValue(reply.NextSequence.Value) : JValue.CreateNull();
                            doc["lastResult"] = new JObject { ["sequence"] = sequence, ["code"] = reply.Code, ["ok"] = reply.Ok };
                        });
                        Reload(); _serverNextSequence = reply.NextSequence;
                        if (reply.CatalogVersion != null && reply.CatalogVersion != Catalog.Version) _verifiedSession = null;
                        // Do not add effects! The acknowledged snapshot already includes the transaction.
                        LastCode = reply.Code; Changed?.Invoke();
                    }
                    else
                    {
                        if (!reply.Retryable) _journal.Commit(doc => ((JObject)((JArray)doc["pending"]!)[0])["blockCode"] = reply.Code);
                        if (reply.CatalogVersion != null && reply.CatalogVersion != Catalog.Version) _verifiedSession = null;
                        Finish(reply.Code, done); yield break;
                    }
                }
                Finish(PendingCount == 0 ? "synchronized" : "queued", done);
            }
            finally { _busy = false; }
        }

        private bool ValidQuote(EconomyQuote quote)
        {
            if (quote.CatalogVersion != Catalog.Version) return false;
            EconomyQuote? actual = quote.Kind == "buy_cosmetic" ? Catalog.QuoteItem(quote.Target)
                : quote.Kind == "buy_booster" || quote.Kind == "consume_booster" ? Catalog.QuoteBooster(quote.Target, quote.Kind == "consume_booster") : null;
            return actual == quote;
        }
        private long Enqueue(string kind, JObject fields, string? rewardId)
        {
            long sequence = _journal.NextSequence ?? throw new InvalidOperationException("Installation sequence exhausted.");
            var operation = new JObject { ["kind"] = kind, ["installationId"] = _journal.InstallationId, ["sequence"] = sequence, ["catalogVersion"] = Catalog.Version };
            foreach (JProperty field in fields.Properties()) operation[field.Name] = field.Value.DeepClone();
            _journal.Commit(doc =>
            {
                EconomyProtocol.Require(((JArray)doc["pending"]!).Count < 128, "queue full");
                ((JArray)doc["pending"]!).Add(new JObject { ["operation"] = CanonicalJson.Write(operation, false), ["attempted"] = false, ["uncertain"] = false, ["blockCode"] = null, ["rewardId"] = rewardId == null ? JValue.CreateNull() : new JValue(rewardId), ["alternatives"] = new JArray() });
                doc["nextSequence"] = sequence == EconomyProtocol.MaxSafeInteger ? JValue.CreateNull() : new JValue(sequence + 1);
            });
            Changed?.Invoke(); return sequence;
        }
        private bool Matches(long session, long epoch) => !_disposed && epoch == _epoch && Current && _transport.SessionVersion == session;
        private bool Acceptable(EconomySnapshot snapshot) => snapshot.OwnerId == OwnerId && (_snapshot == null || snapshot.Revision > _snapshot.Revision
            || (snapshot.Revision == _snapshot.Revision && CanonicalJson.Write(snapshot.ToJson(), false) == CanonicalJson.Write(_snapshot.ToJson(), false)));
        private static JObject Operation(JObject entry) => JsonDoc.ParseObject((string)entry["operation"]!, "pending operation");
        private static bool IsPurchase(JObject op) => (string?)op["kind"] == "buy_cosmetic" || (string?)op["kind"] == "buy_booster" || ((string?)op["kind"] == "consume_booster" && (bool?)op["autoBuy"] == true);
        private void Reload() { _catalog = _journal.Catalog; _snapshot = _journal.Snapshot; }
        private void Finish(string code, Action<string>? done) { LastCode = code; Changed?.Invoke(); done?.Invoke(code); }
        public void InvalidateSession() { _epoch++; _verifiedSession = null; }
        public void Dispose() { if (_disposed) return; _disposed = true; InvalidateSession(); _journal.Dispose(); }

        private static IEnumerator Request(Func<IEnumerator> make, Action failed)
        {
            var stack = new Stack<IEnumerator>();
            try
            {
                IEnumerator? first = null;
                try { first = make(); } catch (Exception) { failed(); }
                if (first == null) yield break;
                stack.Push(first);
                while (stack.Count > 0)
                {
                    IEnumerator top = stack.Peek(); bool next = false; bool error = false;
                    try { next = top.MoveNext(); } catch (Exception) { error = true; }
                    if (error) { failed(); yield break; }
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (top.Current is IEnumerator nested) stack.Push(nested);
                    else yield return top.Current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }
    }
}
