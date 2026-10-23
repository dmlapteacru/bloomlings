using System;
using System.Collections;
using Bloomlings.Client.UI.Design;
using UiQuote = Bloomlings.Client.UI.Design.PurchaseQuote;

namespace Bloomlings.Client.Services.Economy.Online
{
    public sealed record PendingEconomyPurchase(long Sequence, string Kind, string Target);

    /// <summary>In-game purchase adapter. Never invokes legacy/local spending or adds transaction effects to a wallet.</summary>
    public sealed class OnlinePurchaseOperation : IPurchaseOperation
    {
        private readonly OnlineEconomyClient _client;
        private readonly string _kind, _target, _boundSession;
        private readonly Action<IEnumerator> _run;
        private long? _sequence;
        private bool _busy;
        public OnlinePurchaseOperation(OnlineEconomyClient client, string kind, string target, Action<IEnumerator> run)
        {
            if (kind != "buy_cosmetic" && kind != "buy_booster" && kind != "consume_booster") throw new ArgumentException("Not a purchase kind.");
            _client = client; _boundSession = client.SessionKey; _kind = kind; _target = target; _run = run;
            if (Matches(client.PendingPurchase)) _sequence = client.PendingPurchase!.Sequence;
        }
        public string SessionKey => _client.SessionKey;
        public bool IsOnline => _client.IsOnline;
        public void Refresh(Action<PurchaseReply> done) => Start(Review(done), done);
        public void Check(Action<PurchaseReply> done) => Start(Review(done), done);
        public void Buy(UiQuote consent, Action<PurchaseReply> done) => Start(BuyRoutine(consent, done), done);
        private bool Matches(PendingEconomyPurchase? pending) => pending != null && pending.Kind == _kind && pending.Target == _target;
        private EconomyQuote? Quote => _kind == "buy_cosmetic" ? _client.Catalog.QuoteItem(_target) : _client.Catalog.QuoteBooster(_target, _kind == "consume_booster");
        private UiQuote? UiPrice => Quote is EconomyQuote q ? new UiQuote(q.CatalogVersion, q.Price, _client.Confirmed?.Petals ?? 0) : null;
        private void Start(IEnumerator routine, Action<PurchaseReply> done)
        {
            if (_boundSession != SessionKey) { done(new PurchaseReply("account-changed")); return; }
            if (_busy) { done(new PurchaseReply("pending")); return; }
            _busy = true;
            try { _run(Guard(routine, done)); }
            catch { _busy = false; done(new PurchaseReply("pending")); }
        }
        private IEnumerator Guard(IEnumerator routine, Action<PurchaseReply> done)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>(); stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    if (_boundSession != SessionKey) { done(new PurchaseReply("account-changed")); yield break; }
                    bool next; object? current = null;
                    try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                    catch { done(new PurchaseReply("pending")); yield break; }
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator child) stack.Push(child); else yield return current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); _busy = false; }
        }
        private IEnumerator Review(Action<PurchaseReply> done)
        {
            string session = SessionKey;
            yield return _client.Refresh();
            if (session != SessionKey) { done(new PurchaseReply("account-changed")); yield break; }
            if (_client.LastCode != "loaded") { done(new PurchaseReply(_client.IsOnline ? "unavailable" : "offline")); yield break; }
            PendingEconomyPurchase? pending = _client.PendingPurchase;
            if (_client.PendingCount > 0)
            {
                if (!Matches(pending)) { done(new PurchaseReply("pending-operations")); yield break; }
                if (_sequence.HasValue && pending!.Sequence != _sequence) { done(new PurchaseReply("result-unavailable")); yield break; }
                _sequence = pending!.Sequence;
                yield return _client.Flush(); // replay known dispatch; NEVER confirm an unsent/repriced purchase
                if (session != SessionKey) { done(new PurchaseReply("account-changed")); yield break; }
                if (_client.LastCode == "catalog-changed")
                {
                    yield return _client.Refresh();
                    if (session != SessionKey || _client.LastCode != "loaded") { done(new PurchaseReply("pending")); yield break; }
                    done(UiPrice is UiQuote price ? new PurchaseReply("price-changed", price) : new PurchaseReply("unavailable")); yield break;
                }
                if (_client.LastCode == "confirmation-required")
                { done(UiPrice is UiQuote price ? new PurchaseReply("confirmation-required", price) : new PurchaseReply("unavailable")); yield break; }
            }
            Complete(done);
        }
        private IEnumerator BuyRoutine(UiQuote consent, Action<PurchaseReply> done)
        {
            string session = SessionKey;
            EconomyQuote? quote = Quote;
            if (quote == null || quote.Price != consent.Price || quote.CatalogVersion != consent.Version)
            { done(UiPrice is UiQuote price ? new PurchaseReply("price-changed", price) : new PurchaseReply("unavailable")); yield break; }
            if (_client.PendingCount > 0)
            {
                if (!Matches(_client.PendingPurchase) || (_sequence.HasValue && _sequence != _client.PendingPurchase!.Sequence))
                { done(new PurchaseReply("pending-operations")); yield break; }
                _sequence = _client.PendingPurchase!.Sequence;
                yield return _client.ConfirmPendingPurchase(quote);
            }
            else
            {
                if (!_client.CanStartPurchase) { yield return Review(done); yield break; }
                _sequence = _client.NextOperationSequence;
                yield return _client.Purchase(quote);
            }
            if (session != SessionKey) { done(new PurchaseReply("account-changed")); yield break; }
            if (_client.LastCode == "catalog-changed" || _client.LastCode == "price-changed")
            { yield return Review(done); yield break; }
            Complete(done);
        }
        private void Complete(Action<PurchaseReply> done)
        {
            if (_sequence.HasValue && _client.LastAcknowledgedSequence == _sequence)
            {
                done(new PurchaseReply(_client.LastAcknowledgedOk == true ? "purchased" : _client.LastAcknowledgedCode ?? "unavailable")); return;
            }
            if (_client.PendingCount > 0) { done(new PurchaseReply("pending")); return; }
            if (_sequence.HasValue) { done(new PurchaseReply("result-unavailable")); return; }
            if (_kind == "buy_cosmetic" && _client.Owns(_target)) { done(new PurchaseReply("purchased")); return; }
            done(UiPrice is UiQuote price && _client.CanStartPurchase ? new PurchaseReply("quoted", price) : new PurchaseReply("unavailable"));
        }
    }
}
