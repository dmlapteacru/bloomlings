using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// What a purchase confirmation names (spec 005 FR-040): the item and its price, in Petals or in real money (the
    /// store's localized price text). Engine-free.
    /// </summary>
    public sealed class PurchaseOffer
    {
        private PurchaseOffer(string name, int petals, string? moneyPrice)
        {
            Name = name;
            Petals = petals;
            MoneyPrice = moneyPrice;
        }

        /// <summary>The item's player-facing name ("Fireflies", "Shuffle", "Bucket Hat").</summary>
        public string Name { get; }

        /// <summary>The price in Petals (0 for a real-money purchase).</summary>
        public int Petals { get; }

        /// <summary>The store's localized price ("$2.99") of a real-money purchase, or null for Petals.</summary>
        public string? MoneyPrice { get; }

        /// <summary>Whether real money pays: the platform's own purchase sheet follows the confirmation.</summary>
        public bool RealMoney => MoneyPrice != null;

        /// <summary>The price as the card shows it: the grouped Petals ("5 000") or the store's text.</summary>
        public string PriceText => MoneyPrice ?? NumberText.Group(Petals);

        /// <summary>A purchase for <paramref name="price"/> Petals.</summary>
        public static PurchaseOffer ForPetals(string name, int price) => new PurchaseOffer(name, Math.Max(0, price), null);

        /// <summary>A real-money purchase at the store's localized <paramref name="price"/>.</summary>
        public static PurchaseOffer ForMoney(string name, string price) => new PurchaseOffer(name, 0, price);

        /// <summary>Whether a balance of <paramref name="petals"/> pays for it (always for real money: the store decides).</summary>
        public bool Affordable(long petals) => RealMoney || petals >= Petals;
    }

    /// <summary>
    /// The purchase confirmation's state (spec 005 FR-040, the owner's request of 2026-10-06: "Every purchase needs a
    /// confirmation popup"), both builds: a purchase asks (<see cref="Ask"/>) and nothing is spent until the player
    /// confirms; the confirm runs the purchase once, a cancel drops it and buys nothing. A new question replaces one still
    /// open. Engine-free.
    /// </summary>
    public sealed class PurchaseConfirmation
    {
        private Action? _buy;

        /// <summary>The purchase waiting for the player's answer, or null.</summary>
        public PurchaseOffer? Offer { get; private set; }

        /// <summary>Whether a purchase waits for the player's answer.</summary>
        public bool Asking => Offer != null;

        /// <summary>Asks to confirm <paramref name="offer"/>; <paramref name="buy"/> runs only on <see cref="Confirm"/>.</summary>
        public void Ask(PurchaseOffer offer, Action buy)
        {
            Offer = offer;
            _buy = buy;
        }

        /// <summary>The player confirms: the question closes, then the purchase runs once; false when nothing was asked.</summary>
        public bool Confirm()
        {
            Action? buy = _buy;
            if (Offer == null || buy == null)
            {
                return false;
            }

            Cancel();
            buy();
            return true;
        }

        /// <summary>The player cancels (the cream button, the system back): the question closes and nothing is bought.</summary>
        public void Cancel()
        {
            Offer = null;
            _buy = null;
        }
    }

    /// <summary>
    /// The purchase confirmation card (spec 005 FR-040, contracts/look.md §6.14; slot <c>ui.card.purchase</c>), both
    /// builds: a popup card (<see cref="ScreenLayout.Card"/>, "Confirm purchase" in its title, no close: Cancel closes it)
    /// holding, top to bottom, the item's picture in a cream well (<see cref="Picture"/>), the question naming the item and
    /// its price on up to two lines ("Buy Fireflies for 5 000 Petals?", <see cref="Question"/>), the cost pill with the
    /// lotus and the price, or the store's price for real money (<see cref="Price"/>), a caption line (<see cref="Note"/>:
    /// the Petals balance, or that the store confirms the payment next), the green "Buy" (<see cref="Confirm"/>) and the
    /// cream "Cancel" (<see cref="Cancel"/>). Engine-free.
    /// </summary>
    public sealed record PurchaseConfirmRegions(CardRegions Card, Box Picture, Box Question, Box Price, Box Note, Box Confirm, Box Cancel)
    {
        /// <summary>The picture well's side, in reference units (at most half the body's width).</summary>
        public const float PictureUnits = 320f;

        /// <summary>A line of the question, in reference units; the question takes up to <see cref="QuestionLines"/>.</summary>
        public const float LineUnits = 60f;

        public const int QuestionLines = 2;

        /// <summary>The cost pill's height, in reference units (the Daily Reward's reward pill's).</summary>
        public const float PriceUnits = 92f;

        /// <summary>The caption line's height, in reference units.</summary>
        public const float NoteUnits = 50f;

        /// <summary>The card's content height in reference units (<see cref="ScreenLayout.Card"/>).</summary>
        public const float ContentUnits = 10f + PictureUnits + 26f + (QuestionLines * LineUnits) + 14f + PriceUnits + 10f + NoteUnits + 30f
            + DesignTokens.Size.CardPrimaryHeight + 24f + DesignTokens.Size.SecondaryHeight + 16f;

        /// <summary>The widest the cost pill may grow (the hosts fit it to its price).</summary>
        public float PriceMaxWidth => Card.Body.Width * 0.7f;
    }

    public static partial class ScreenLayout
    {
        /// <summary>The purchase confirmation card's regions on a screen (<see cref="PurchaseConfirmRegions"/>).</summary>
        public static PurchaseConfirmRegions PurchaseConfirm(float width, float height, Insets insets)
        {
            CardRegions card = Card(width, height, insets, PurchaseConfirmRegions.ContentUnits);
            float u = DesignTokens.ScaleFor(width, height);
            Box body = card.Body;
            float y = body.Top + (10f * u);
            float side = Math.Min(PurchaseConfirmRegions.PictureUnits * u, body.Width * 0.5f);
            Box picture = Box.FromCenter(body.CenterX, y + (side / 2f), side, side);
            y = picture.Bottom + (26f * u);
            var question = new Box(body.Left, y, body.Right, y + (PurchaseConfirmRegions.QuestionLines * PurchaseConfirmRegions.LineUnits * u));
            y = question.Bottom + (14f * u);
            float pill = PurchaseConfirmRegions.PriceUnits * u;
            var price = new Box(body.CenterX - (body.Width * 0.35f), y, body.CenterX + (body.Width * 0.35f), y + pill);
            y = price.Bottom + (10f * u);
            var note = new Box(body.Left, y, body.Right, y + (PurchaseConfirmRegions.NoteUnits * u));
            y = note.Bottom + (30f * u);
            Box confirm = CardButton(body, y, true, u);
            Box cancel = CardButton(body, confirm.Bottom + (24f * u), false, u);
            return new PurchaseConfirmRegions(card, picture, question, price, note, confirm, cancel);
        }
    }
}
