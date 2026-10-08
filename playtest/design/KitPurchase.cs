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

            // The item's picture in a cream well (an outfit card's), clipped to it.
            Box well = r.Picture;
            float radius = well.Width * 0.12f;
            float line = Math.Max(1f, p.U(DesignTokens.Garden.OutlineWidth));
            SoftShadow(p, well, radius, 0.12f, 0.03f);
            p.FillRoundGradient(well, radius, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell);
            if (picture != null)
            {
                p.PushClipRound(well, radius);
                picture(p, well.Inset(well.Width * 0.06f));
                p.PopClip();
            }

            p.StrokeRound(well.Inset(line / 2f), radius - (line / 2f), line, C.ParchmentEdge.Darken(0.08f));

            // The question naming the item and the price, on up to two lines.
            string question = PlaytestText.F(offer.RealMoney ? "purchase.question_money" : "purchase.question_petals", offer.Name, offer.PriceText);
            List<string> lines = EndCards.Lines(p, question, T.Body, r.Question.Width, PurchaseConfirmRegions.QuestionLines);
            float lineHeight = r.Question.Height / PurchaseConfirmRegions.QuestionLines;
            float y = r.Question.CenterY - (lines.Count * lineHeight / 2f) + (lineHeight / 2f);
            foreach (string text in lines)
            {
                p.Text(text, r.Question.CenterX, y, T.Body, C.InkBrown, r.Question.Width, look: TextLook.Plain(C.InkBrown));
                y += lineHeight;
            }

            // The price: the cost pill with the lotus, or the store's price on the same cream pill.
            float h = r.Price.Height;
            float scale = h * 0.56f / p.U(T.Count.Size);
            float width = Math.Min(r.PriceMaxWidth, p.MeasureText(offer.PriceText, T.Count, scale) + (h * (offer.RealMoney ? 1f : 1.9f)));
            Box pill = Box.FromCenter(r.Price.CenterX, r.Price.CenterY, width, h);
            if (offer.RealMoney)
            {
                CostPill(p, pill, Cost.Charges(0), offer.PriceText);
            }
            else
            {
                CostPill(p, pill, Cost.Petals(offer.Petals));
            }

            p.Text(note, r.Note.CenterX, r.Note.CenterY, T.Caption, C.InkBrownSoft, r.Note.Width);
            PrimaryButton(p, r.Confirm, PlaytestText.T("purchase.buy"), onBuy);
            SecondaryButton(p, r.Cancel.Inset(p.U(40f), 0f), PlaytestText.T("common.cancel"), onCancel);
            EndCard(p);
        }
    }
}
