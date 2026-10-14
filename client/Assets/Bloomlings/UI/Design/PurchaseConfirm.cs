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

    /// <summary>Compact confirmation: item preview and name, balance/store note, price on Buy, and Cancel. No close icon.</summary>
    public sealed record PurchaseConfirmRegions(CardRegions Card, Box Picture, Box Question, Box Price, Box Note, Box Confirm, Box Cancel)
    {
        public const float PictureUnits = 380f;
        public const float LineUnits = 56f;
        public const int QuestionLines = 2;
        public const float PriceUnits = 0f;
        public const float NoteUnits = 52f;
        public const float ContentUnits = 12f + PictureUnits + 12f + (QuestionLines * LineUnits) + NoteUnits + 28f + DesignTokens.Size.CardPrimaryHeight + 18f + DesignTokens.Size.CardSecondaryHeight + 16f;
        public float PriceMaxWidth => Card.Body.Width * .7f;
    }
    public static partial class ScreenLayout
    {
        public static PurchaseConfirmRegions PurchaseConfirm(float width,float height,Insets insets)
        {
            CardRegions card=Card(width,height,insets,PurchaseConfirmRegions.ContentUnits);
            float u=DesignTokens.ScaleFor(width,height); Box body=card.Body;
            float side=Math.Min(PurchaseConfirmRegions.PictureUnits*u,body.Width*.64f);
            Box picture=Box.FromCenter(body.CenterX,body.Top+12*u+side/2,side,side);
            Box question=new Box(body.Left,picture.Bottom+12*u,body.Right,picture.Bottom+124*u);
            Box note=new Box(body.Left,question.Bottom,body.Right,question.Bottom+52*u);
            Box confirm=CardButton(body,note.Bottom+28*u,true,u);
            Box cancel=CardButton(body,confirm.Bottom+18*u,false,u);
            return new PurchaseConfirmRegions(card,picture,question,new Box(body.CenterX,note.Top,body.CenterX,note.Top),note,confirm,cancel);
        }
    }
}
