using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The purchase confirmation (spec 005 FR-040), tap or scroll (FR-041) and the clearing cards' buttons (FR-038 as
    /// amended on 2026-10-06): the engine-free parts both builds use.
    /// </summary>
    public class PurchaseConfirmTests
    {
        private static readonly (float Width, float Height, Insets Insets)[] Phones =
        {
            (1080f, 1920f, new Insets(63, 0)),
            (1080f, 2340f, new Insets(110, 63)),
            (1080f, 2520f, new Insets(120, 66)),
            (1440f, 3200f, new Insets(140, 80)),
        };

        [Test]
        public void NothingIsBought_UntilThePlayerConfirms_AndACancelBuysNothing()
        {
            var confirmation = new PurchaseConfirmation();
            int bought = 0;
            Assert.That(confirmation.Confirm(), Is.False, "nothing asked");

            confirmation.Ask(PurchaseOffer.ForPetals("Fireflies", 5000), () => bought++);
            Assert.That(confirmation.Asking && bought == 0, Is.True, "asking spends nothing");
            confirmation.Cancel();
            Assert.That(confirmation.Asking || bought != 0, Is.False, "a cancel buys nothing");
            Assert.That(confirmation.Confirm(), Is.False, "a cancelled question cannot be confirmed later");

            confirmation.Ask(PurchaseOffer.ForPetals("Bubbles", 5000), () => bought += 10);
            confirmation.Ask(PurchaseOffer.ForPetals("Pushers", 5000), () => bought += 100);
            Assert.That(confirmation.Offer!.Name, Is.EqualTo("Pushers"), "a new question replaces the open one");
            Assert.That(confirmation.Confirm(), Is.True);
            Assert.That(bought, Is.EqualTo(100), "the confirm runs the purchase once");
            Assert.That(confirmation.Asking || confirmation.Confirm(), Is.False, "and only once");
        }

        [Test]
        public void TheQuestion_NamesTheItemAndThePrice_InPetalsOrRealMoney()
        {
            PurchaseOffer petals = PurchaseOffer.ForPetals("Fireflies", 5000);
            Assert.That(petals.RealMoney, Is.False);
            Assert.That(petals.PriceText, Is.EqualTo(NumberText.Group(5000)));
            Assert.That(petals.Affordable(4999), Is.False);
            Assert.That(petals.Affordable(5000), Is.True);

            PurchaseOffer money = PurchaseOffer.ForMoney("Remove Ads", "$2.99");
            Assert.That(money.RealMoney && money.Petals == 0, Is.True);
            Assert.That(money.PriceText, Is.EqualTo("$2.99"));
            Assert.That(money.Affordable(0), Is.True, "the store decides");
        }

        [Test]
        public void TheCard_LaysItsPartsInOrder_InsideItsBody_OnEveryPhone()
        {
            foreach ((float w, float h, Insets insets) in Phones)
            {
                PurchaseConfirmRegions r = ScreenLayout.PurchaseConfirm(w, h, insets);
                Box safe = ScreenLayout.SafeArea(w, h, insets);
                Box body = r.Card.Body;
                Assert.That(r.Card.Card.Within(safe), Is.True, "the card inside the safe area");
                var parts = new List<(string, Box)>
                {
                    ("picture", r.Picture), ("question", r.Question), ("price", r.Price), ("note", r.Note), ("confirm", r.Confirm), ("cancel", r.Cancel),
                };
                for (int i = 0; i < parts.Count; i++)
                {
                    (string name, Box box) = parts[i];
                    Assert.That(box.Within(body), Is.True, name + " inside the body on " + h);
                    if (i > 0)
                    {
                        Assert.That(box.Top, Is.GreaterThanOrEqualTo(parts[i - 1].Item2.Bottom - 0.01f), name + " below " + parts[i - 1].Item1);
                    }
                }

                Assert.That(r.Picture.Width, Is.EqualTo(r.Picture.Height).Within(0.01f), "a square well");
                Assert.That(r.PriceMaxWidth, Is.LessThanOrEqualTo(body.Width));
                float u = DesignTokens.ScaleFor(w, h);
                Assert.That(r.Confirm.Height, Is.EqualTo(DesignTokens.Size.CardPrimaryHeight * u).Within(0.01f));
            }
        }

        [Test]
        public void ATap_FiresOnlyWithinTheSlop_OnAPageThatScrolls()
        {
            var g = new TouchGesture(20f, 80f);
            g.Press(100f, 100f, scrolls: true);
            g.Move(110f, 112f);
            Assert.That(g.Dragging, Is.False, "within the slop");
            GestureEnd tap = g.Lift(112f, 110f);
            Assert.That(tap.Kind, Is.EqualTo(GestureKind.Tap));
            Assert.That((tap.X, tap.Y), Is.EqualTo((112f, 110f)), "where the finger lifted");

            g.Press(100f, 100f, scrolls: true);
            g.Move(100f, 125f);
            Assert.That(g.Dragging, Is.True, "past the slop the touch drags");
            g.Move(100f, 102f);
            Assert.That(g.Dragging, Is.True, "for good, even back near its start");
            GestureEnd back = g.Lift(100f, 102f);
            Assert.That(back.Kind == GestureKind.Drag && back.Page == 0, Is.True, "a drag never taps; a short one turns nothing");

            g.Press(100f, 300f, scrolls: true);
            GestureEnd up = g.Lift(104f, 200f);
            Assert.That(up.Kind == GestureKind.Drag && up.Page == 1, Is.True, "a swipe up brings the next page");
            g.Press(100f, 100f, scrolls: true);
            Assert.That(g.Lift(190f, 105f).Page, Is.EqualTo(-1), "a swipe to the right brings the previous one");

            g.Press(100f, 100f, scrolls: false);
            g.Move(300f, 300f);
            Assert.That(g.Dragging, Is.False, "off a page that scrolls nothing drags");
            Assert.That(g.Lift(300f, 300f).Kind, Is.EqualTo(GestureKind.Tap), "it taps where it lifts, as before");

            g.Press(100f, 100f, scrolls: true);
            g.Cancel();
            Assert.That(g.Lift(100f, 100f).Kind, Is.EqualTo(GestureKind.None), "a cancelled touch taps nothing");
        }

        [Test]
        public void ThePageStep_FollowsTheDragsMainDirection()
        {
            Assert.That(TouchGesture.PageStep(0f, -100f, 80f), Is.EqualTo(1), "up: next");
            Assert.That(TouchGesture.PageStep(0f, 100f, 80f), Is.EqualTo(-1), "down: previous");
            Assert.That(TouchGesture.PageStep(-100f, 30f, 80f), Is.EqualTo(1), "left: next");
            Assert.That(TouchGesture.PageStep(100f, -30f, 80f), Is.EqualTo(-1), "right: previous");
            Assert.That(TouchGesture.PageStep(60f, -60f, 80f), Is.EqualTo(0), "shorter than the swipe along either way");
        }

        [Test]
        public void TheSlop_IsTenDp_InEachHostsPixels()
        {
            Assert.That(DesignTokens.Touch.SlopDp, Is.InRange(8f, 10f), "the owner's 8–10 dp");
            Assert.That(TouchGesture.SlopPixels(420f, 1080f), Is.EqualTo(26.25f).Within(0.01f), "a 420 dpi phone");
            Assert.That(TouchGesture.SlopPixels(160f, 1080f), Is.EqualTo(10f).Within(0.01f), "one pixel a dp");
            Assert.That(TouchGesture.SlopPixels(0f, 1080f), Is.EqualTo(30f).Within(0.01f), "unknown density: a 360 dp wide phone");
            Assert.That(TouchGesture.SwipePixels(420f, 1080f), Is.GreaterThan(TouchGesture.SlopPixels(420f, 1080f)));
            TouchGesture g = TouchGesture.For(420f, 1080f);
            Assert.That(g.Slop, Is.EqualTo(26.25f).Within(0.01f));
        }

        [Test]
        public void TheClearingButton_HangsUnderItsCard_AndThePriceFitsIt()
        {
            var slot = new Box(100f, 500f, 390f, 920f);
            Box body = ClearingCard.Body(slot);
            Box button = ClearingCard.Button(slot);
            Assert.That(button.Within(slot), Is.True, "inside the card's touch box (the card is the button)");
            Assert.That(button.Top, Is.LessThan(body.Bottom), "over the body's bottom edge");
            Assert.That(button.Bottom, Is.EqualTo(slot.Bottom).Within(0.5f));

            Assert.That(button.Height, Is.EqualTo(body.Height * 0.3f).Within(0.01f), "1.5 times the cost pill's height (the owner, 2026-10-06)");

            (Box lotus, Box price, float scale) = ClearingCard.PriceParts(button, 100f);
            Assert.That(scale, Is.LessThanOrEqualTo(1f));
            Assert.That(lotus.Right, Is.LessThanOrEqualTo(price.Left + 0.01f), "the lotus, then the price, no Buy word");
            float pad = button.Height * ClearingCard.PadShare;
            Assert.That(lotus.Left, Is.GreaterThanOrEqualTo(button.Left + pad - 0.5f));
            Assert.That(price.Right, Is.LessThanOrEqualTo(button.Right - pad + 0.5f));
            Assert.That((lotus.Left + price.Right) / 2f, Is.EqualTo(button.CenterX).Within(0.5f), "centered");

            (Box _, Box _, float wide) = ClearingCard.PriceParts(button, 800f);
            Assert.That(wide, Is.LessThan(1f), "too long: everything shrinks together");
        }
    }
}
