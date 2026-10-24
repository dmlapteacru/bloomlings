using System;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>Existing dev/local economy behind the same purchase UX. It is never a fallback from an online operation.</summary>
    public sealed class LocalPurchaseOperation : IPurchaseOperation
    {
        private readonly Func<string> _session;
        private readonly Func<bool> _online;
        private readonly Func<long> _balance;
        private readonly Func<PurchaseOffer> _offer;
        private readonly Func<bool> _buy;
        private readonly string _owner;
        private bool _attempted;
        private string _result = "pending";
        public LocalPurchaseOperation(Func<string> session, Func<bool> online, Func<long> balance, Func<PurchaseOffer> offer, Func<bool> buy)
        { _session = session; _owner = session(); _online = online; _balance = balance; _offer = offer; _buy = buy; }
        public string SessionKey => _session();
        public bool IsOnline => _online();
        private PurchaseQuote Quote { get { var offer = _offer(); return new PurchaseQuote("local:" + offer.PriceText, offer.Petals, _balance()); } }
        public void Refresh(Action<PurchaseReply> done)
        {
            if (SessionKey != _owner) { done(new PurchaseReply("account-changed")); return; }
            done(IsOnline ? new PurchaseReply("quoted", Quote) : new PurchaseReply("offline"));
        }
        public void Buy(PurchaseQuote quote, Action<PurchaseReply> done)
        {
            if (SessionKey != _owner) { done(new PurchaseReply("account-changed")); return; }
            if (_attempted) { Check(done); return; }
            if (!IsOnline) { done(new PurchaseReply("offline")); return; }
            PurchaseQuote current = Quote;
            if (quote.Version != current.Version || quote.Price != current.Price) { done(new PurchaseReply("price-changed", current)); return; }
            if (current.Balance < current.Price) { done(new PurchaseReply("insufficient-funds", current)); return; }
            _attempted = true;
            _result = _buy() ? "purchased" : "rejected";
            done(new PurchaseReply(_result));
        }
        public void Check(Action<PurchaseReply> done)
        {
            if (SessionKey != _owner) { done(new PurchaseReply("account-changed")); return; }
            if (_attempted) done(new PurchaseReply(_result)); else Refresh(done);
        }
    }
}
