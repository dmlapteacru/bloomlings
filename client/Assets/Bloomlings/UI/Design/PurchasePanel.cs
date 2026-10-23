using System;

namespace Bloomlings.Client.UI.Design
{
    public enum PurchaseStage { Confirm, Loading, Offline, Pending, Reprice, Failed, Complete, Closed }
    public sealed record PurchaseQuote(string Version, int Price, long Balance);
    public sealed record PurchaseReply(string Code, PurchaseQuote? Quote = null);
    /// <summary>An operation belongs to one immutable profile/session. Check replays its durable pending request, never buys again.</summary>
    public interface IPurchaseOperation
    {
        string SessionKey { get; }
        bool IsOnline { get; }
        void Refresh(Action<PurchaseReply> done);
        void Buy(PurchaseQuote quote, Action<PurchaseReply> done);
        void Check(Action<PurchaseReply> done);
    }
    /// <summary>Actual in-game purchase state: asynchronous quote, explicit price consent and safe unknown-result recovery.</summary>
    public sealed class PurchasePanel
    {
        private IPurchaseOperation? _operation;
        private PurchaseQuote? _quote;
        private string _session = "";
        private long _generation;
        private bool _working;
        public PurchaseStage Stage { get; private set; } = PurchaseStage.Closed;
        public PurchaseOffer? Offer { get; private set; }
        public string Code { get; private set; } = "";
        public long? Balance => _quote?.Balance;
        public bool IsWorking => _working;
        public bool IsOnline => Current && _operation!.IsOnline;
        public bool CanBuy => !_working && (Stage == PurchaseStage.Confirm || Stage == PurchaseStage.Reprice) && _quote != null && Current && _operation!.IsOnline && _quote.Balance >= _quote.Price;
        public bool CanCheck => !_working && (Stage == PurchaseStage.Offline || Stage == PurchaseStage.Pending || Stage == PurchaseStage.Failed) && Current && _operation!.IsOnline;
        private bool Current => _operation != null && _operation.SessionKey == _session;
        public event Action? Changed;
        public void Open(PurchaseOffer offer, IPurchaseOperation operation)
        {
            _generation++; _working = false; _operation = operation; _session = operation.SessionKey; Offer = offer; _quote = null;
            Stage = PurchaseStage.Loading; Code = ""; Refresh();
        }
        private void Refresh() => Request(done => _operation!.Refresh(done), false);
        public void Confirm()
        {
            if (!Current) { FailSession(); return; }
            if (!CanBuy)
            {
                if (!_working && _operation != null && !_operation.IsOnline) { Stage = PurchaseStage.Offline; Code = "offline"; Changed?.Invoke(); }
                return;
            }
            PurchaseQuote quote = _quote!;
            Request(done => _operation!.Buy(quote, done), true);
        }
        public void Check()
        {
            if (!Current) { FailSession(); return; }
            if (CanCheck) Request(done => _operation!.Check(done), true);
        }
        /// <summary>Closing is presentation-only. An operation already sent remains in its owner's durable outbox.</summary>
        public void Close() { _generation++; _working = false; Stage = PurchaseStage.Closed; Offer = null; _operation = null; _quote = null; Changed?.Invoke(); }
        private void Request(Action<Action<PurchaseReply>> send, bool mutationOrReplay)
        {
            if (!Current) { FailSession(); return; }
            if (!_operation!.IsOnline) { Stage = PurchaseStage.Offline; Code = "offline"; Changed?.Invoke(); return; }
            long generation = _generation;
            _working = true; Stage = PurchaseStage.Loading; Changed?.Invoke();
            bool completed = false;
            void Complete(PurchaseReply reply)
            {
                if (completed || generation != _generation) return;
                completed = true; _working = false;
                if (!Current) { FailSession(); return; }
                Code = reply.Code;
                if (reply.Quote != null)
                {
                    bool changed = _quote != null && (_quote.Price != reply.Quote.Price || _quote.Version != reply.Quote.Version);
                    _quote = reply.Quote;
                    if (Offer != null && !Offer.RealMoney) Offer = Offer.WithPrice(reply.Quote.Price);
                    Stage = changed || reply.Code == "price-changed" || reply.Code == "confirmation-required" ? PurchaseStage.Reprice : PurchaseStage.Confirm;
                }
                else Stage = reply.Code switch
                {
                    "purchased" => PurchaseStage.Complete,
                    "offline" => PurchaseStage.Offline,
                    "pending" => PurchaseStage.Pending,
                    _ => PurchaseStage.Failed,
                };
                Changed?.Invoke();
            }
            try { send(Complete); }
            catch { Complete(new PurchaseReply(mutationOrReplay ? "pending" : "unavailable")); }
        }
        private void FailSession() { _generation++; _working = false; _quote = null; Code = "account-changed"; Stage = PurchaseStage.Failed; Changed?.Invoke(); }
    }
}
