using System;
using System.Collections;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class PurchasePanelTests
    {
        private sealed class Operation : IPurchaseOperation
        {
            public string SessionKey { get; set; } = "A:1";
            public bool IsOnline { get; set; } = true;
            public Action<PurchaseReply>? Done;
            public int Buys, Checks;
            public void Refresh(Action<PurchaseReply> done) => Done = done;
            public void Buy(PurchaseQuote quote, Action<PurchaseReply> done) { Buys++; Done = done; }
            public void Check(Action<PurchaseReply> done) { Checks++; Done = done; }
        }
        private static PurchaseOffer Offer => PurchaseOffer.ForItem("Hat", 650, "hat.twig_nightcap");
        [Test] public void RequiresQuoteAndExplicitConsentAndRejectsDoubleTap()
        {
            var op = new Operation(); var p = new PurchasePanel(); p.Open(Offer, op); p.Confirm(); Assert.That(op.Buys, Is.Zero);
            op.Done!(new PurchaseReply("quoted", new PurchaseQuote("v1", 650, 1000))); Assert.That(p.CanBuy, Is.True);
            p.Confirm(); p.Confirm(); Assert.That(op.Buys, Is.EqualTo(1)); Assert.That(p.IsWorking, Is.True);
        }
        [Test] public void LostResponseOffersOnlyStatusCheckNotAnotherBuy()
        {
            var op = new Operation(); var p = new PurchasePanel(); p.Open(Offer, op);
            op.Done!(new PurchaseReply("quoted", new PurchaseQuote("v1", 650, 1000))); p.Confirm();
            op.Done!(new PurchaseReply("pending")); Assert.That(p.CanBuy, Is.False); Assert.That(p.CanCheck, Is.True);
            p.Confirm(); Assert.That(op.Buys, Is.EqualTo(1)); p.Check(); Assert.That(op.Checks, Is.EqualTo(1));
        }
        [Test] public void NewPriceIsShownWithoutAutomaticConsent()
        {
            var op = new Operation(); var p = new PurchasePanel(); p.Open(Offer, op);
            op.Done!(new PurchaseReply("quoted", new PurchaseQuote("v1", 650, 1000))); p.Confirm();
            op.Done!(new PurchaseReply("price-changed", new PurchaseQuote("v2", 800, 1000)));
            Assert.That(p.Stage, Is.EqualTo(PurchaseStage.Reprice)); Assert.That(p.Offer!.Petals, Is.EqualTo(800));
            Assert.That(op.Buys, Is.EqualTo(1)); p.Confirm(); Assert.That(op.Buys, Is.EqualTo(2));
        }
        [Test] public void OfflineDoesNotRequestOrBuyAndReconnectOnlyLoadsQuote()
        {
            var op = new Operation { IsOnline = false }; var p = new PurchasePanel(); p.Open(Offer, op); p.Confirm();
            Assert.That(p.Stage, Is.EqualTo(PurchaseStage.Offline)); Assert.That(op.Done, Is.Null); Assert.That(op.Buys, Is.Zero);
            op.IsOnline = true; p.Check(); Assert.That(op.Checks, Is.EqualTo(1)); Assert.That(op.Buys, Is.Zero);
        }
        [Test] public void LateResponseFromPriorPanelCannotCloseOrRepriceNewItem()
        {
            var a = new Operation(); var b = new Operation(); var p = new PurchasePanel(); p.Open(Offer, a);
            var old = a.Done; p.Close(); p.Open(PurchaseOffer.ForItem("Other", 1000, "hat.twig_boater"), b);
            old!(new PurchaseReply("purchased")); Assert.That(p.Stage, Is.EqualTo(PurchaseStage.Loading)); Assert.That(p.Offer!.Name, Is.EqualTo("Other"));
        }
        [Test] public void AToBToAResponseDoesNotConfirmWrongSession()
        {
            var op = new Operation(); var p = new PurchasePanel(); p.Open(Offer, op); op.SessionKey = "A:3";
            op.Done!(new PurchaseReply("purchased")); Assert.That(p.Stage, Is.EqualTo(PurchaseStage.Failed)); Assert.That(p.Code, Is.EqualTo("account-changed"));
        }
        [Test] public void InsufficientBalanceNeverDispatchesBuy()
        {
            var op = new Operation(); var p = new PurchasePanel(); p.Open(Offer, op);
            op.Done!(new PurchaseReply("quoted", new PurchaseQuote("v1", 650, 10))); p.Confirm(); Assert.That(op.Buys, Is.Zero);
        }
        [Test] public void NativeCoroutineRunnerBoundsImmediateWorkAndDisposesOnShutdown()
        {
            bool disposed = false; int steps = 0;
            IEnumerator Nested() { try { for (int i = 0; i < 200; i++) { steps++; yield return Empty(); } } finally { disposed = true; } }
            IEnumerator Empty() { yield break; }
            using var queue = new UiRoutineQueue(); queue.Run(Nested()); queue.Tick();
            Assert.That(steps, Is.LessThan(200)); Assert.That(queue.HasWork, Is.True);
            queue.Dispose(); Assert.That(disposed, Is.True); Assert.That(queue.HasWork, Is.False);
        }
    }
}
