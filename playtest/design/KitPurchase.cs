using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The purchase confirmation (spec 005 FR-040, contracts/look.md §6.14).</summary>
    public static partial class Kit
    {
        /// <summary>Compact purchase artwork and lettering, retaining the original comfortable touch area.</summary>
        public static void PurchaseButton(IPainter p, Box box, string label, Action? action, TypeStyle? style = null, string? iconId = null)
        {
            PrimaryButton(p, ProfileGameDesign.PurchaseFace(box), label, action,
                ProfileGameDesign.PurchaseType(style ?? T.Button), iconId);
            if (action != null) p.Hit(box, action);
        }

        /// <summary>
        /// The purchase confirmation card (<c>ui.card.purchase</c>; Unity's <c>UiKit.PurchaseCard</c>), laid by
        /// <see cref="ScreenLayout.PurchaseConfirm"/>: the popup card titled "Confirm purchase" with no close (Cancel
        /// closes it), the item's <paramref name="picture"/> in a cream well, "Buy {item} for {price} Petals?" (or the
        /// store's price for real money) on up to two lines, the cost pill with the lotus and the price, the caption
        /// <paramref name="note"/> (the balance, or that the store confirms the payment next), the green "Buy" and the cream
        /// "Cancel". <paramref name="pop"/> is the card's pop scale (<see cref="Pop"/>).
        /// </summary>
        public static void PurchaseCard(IPainter p, PurchaseOffer offer, Action<IPainter, Box>? picture, string note, Action onBuy, Action onCancel, float pop = 1f)
        {
            p.Mark("ui.card.purchase");
            PurchaseConfirmRegions r = ScreenLayout.PurchaseConfirm(p.Width, p.Height, p.Insets);
            Card(p, PurchaseConfirmRegions.ContentUnits, PlaytestText.T("purchase.title"), null, pop);

            if(picture != null) { p.PushClipRound(r.Picture,r.Picture.Width*.08f); picture(p,r.Picture); p.PopClip(); }
            List<string> lines=EndCards.Lines(p,offer.Name,PopupDesign.Label,r.Question.Width,2);
            float lh=r.Question.Height/2;float y=r.Question.CenterY-(lines.Count-1)*lh/2;
            foreach(string text in lines) { p.Text(text,r.Question.CenterX,y,PopupDesign.Label,C.InkBrown,r.Question.Width,look:TextLook.Plain(C.InkBrown)); y+=lh; }
            p.Text(note,r.Note.CenterX,r.Note.CenterY,T.Caption,C.InkBrownSoft,r.Note.Width);
            PurchaseButton(p,r.Confirm,PlaytestText.T("purchase.buy")+"  "+offer.PriceText,onBuy,iconId:offer.RealMoney?null:"currency.petal");
            SecondaryButton(p,r.Cancel.Inset(p.U(40f),0),PlaytestText.T("common.cancel"),onCancel,style:PopupDesign.Label);
            EndCard(p);
        }
    }
}
