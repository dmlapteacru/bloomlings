using System;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>The purchase confirmation (spec 005 FR-040, contracts/look.md §6.14), the playtest's <c>Kit.PurchaseCard</c>.</summary>
    public static partial class UiKit
    {
        /// <summary>
        /// The purchase confirmation card (<c>ui.card.purchase</c>) under <paramref name="parent"/>, hidden: a popup card
        /// titled "Confirm purchase" with no close (Cancel closes it), laid by <see cref="ScreenLayout.PurchaseConfirm"/>.
        /// <see cref="PurchaseCardView.Show"/> fills it with the item and opens it over its siblings.
        /// </summary>
        public static PurchaseCardView PurchaseCard(Transform parent)
        {
            CardView card = Card("PurchaseConfirm", parent, Loc.T("purchase.title"), PurchaseConfirmRegions.ContentUnits, null);
            PurchaseCardView view = card.Root.AddComponent<PurchaseCardView>();
            (float w, float h, Insets insets) = ScreenFrame();
            PurchaseConfirmRegions r = ScreenLayout.PurchaseConfirm(w, h, insets);
            view.Build(card, r);
            card.Root.SetActive(false);
            return view;
        }
    }

    /// <summary>
    /// The purchase confirmation card built by <see cref="UiKit.PurchaseCard"/> (spec 005 FR-040): the item's picture in a
    /// cream well, "Buy {item} for {price} Petals?" (or the store's price), the cost pill, a caption (the Petals balance, or
    /// that the store's own purchase sheet follows), the green "Buy" and the cream "Cancel". Nothing is bought until Buy:
    /// the purchase waits in its <see cref="PurchaseConfirmation"/>; Cancel buys nothing.
    /// </summary>
    public sealed class PurchaseCardView : MonoBehaviour
    {
        private readonly PurchaseConfirmation _confirmation = new PurchaseConfirmation();
        private GameObject _root = null!;
        private RectTransform _picture = null!;
        private TextMeshProUGUI _question = null!;
        private TextMeshProUGUI _note = null!;
        private CostPillView _petals = null!;
        private CostPillView _money = null!;
        private Action? _cancelled;

        /// <summary>Whether the card is open (a purchase waits for the player's answer).</summary>
        public bool IsOpen => _root.activeSelf;

        /// <summary>The purchase waiting for the answer, or null.</summary>
        public PurchaseOffer? Offer => _confirmation.Offer;

        internal void Build(CardView card, PurchaseConfirmRegions r)
        {
            _root = card.Root;
            Box body = r.Card.Body;
            Transform at = card.Body;

            // The picture's cream well (an outfit card's), its picture clipped to it.
            Func<Box, float> radius = b => b.Width * 0.12f;
            Image well = UiKit.RoundGradient("Well", at, C.ParchmentWell.Mix(C.CreamTop, 0.35f), C.ParchmentWell, radius);
            well.gameObject.AddComponent<Mask>();
            UiKit.PlaceBox(well.rectTransform, r.Picture, body);
            _picture = UiFactory.CreateRect("Picture", well.transform);
            BoxLayout.On(well.rectTransform).Add(_picture, b => b.Inset(b.Width * 0.06f));
            Image line = UiKit.RoundRing("WellLine", at, UiTheme.Of(C.ParchmentEdge.Darken(0.08f)), radius, _ => Mathf.Max(1f, UiKit.Units(DesignTokens.Garden.OutlineWidth)));
            UiKit.PlaceBox(line.rectTransform, r.Picture, body);

            // The question on up to two lines.
            _question = UiKit.Label("Question", at, string.Empty, T.Body, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            _question.textWrappingMode = TextWrappingModes.Normal;
            UiKit.PlaceBox(_question.rectTransform, r.Question, body);

            // The price: the cost pill with the lotus, or the store's price on the same cream pill, as wide as its text.
            RectTransform price = UiFactory.CreateRect("Price", at);
            UiKit.PlaceBox(price, r.Price, body);
            _petals = UiKit.CostPill("Petals", price, Cost.Petals(0));
            _money = UiKit.TextPill("Money", price, CostKind.Charges, string.Empty);
            float max = r.PriceMaxWidth;
            foreach ((CostPillView pill, bool lotus) in new[] { (_petals, true), (_money, false) })
            {
                TextMeshProUGUI amount = UiKit.PillText(pill);
                BoxLayout.On(price).Watch(amount).Add((RectTransform)pill.transform, b =>
                {
                    float hh = b.Height;
                    float measured = KitText.Measure(amount, hh * 0.56f);
                    float text = measured > 0f ? measured : hh * 0.56f * 0.6f * Mathf.Max(1, amount.text.Length);
                    return Box.FromCenter(b.CenterX, b.CenterY, Mathf.Min(max, text + (hh * (lotus ? 1.9f : 1f))), hh);
                });
            }

            _note = UiKit.Label("Note", at, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(_note.rectTransform, r.Note, body);

            Button buy = UiKit.PrimaryButton("Buy", at, Loc.T("purchase.buy"), Buy);
            UiKit.PlaceBox((RectTransform)buy.transform, r.Confirm, body);
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            Button cancel = UiKit.SecondaryButton("Cancel", at, Loc.T("common.cancel"), Cancel);
            UiKit.PlaceBox((RectTransform)cancel.transform, r.Cancel.Inset(40f * u, 0f), body);
        }

        /// <summary>
        /// Asks to confirm <paramref name="offer"/> and opens the card over its siblings: <paramref name="picture"/> puts the
        /// item's picture into the well (a booster's icon, the hero in the outfit, the style's live preview, the avatar);
        /// <paramref name="buy"/> runs only on Buy, <paramref name="cancelled"/> when Cancel closes it. <paramref name="note"/>
        /// is the caption under the price (by default the store's sheet follows, for real money).
        /// </summary>
        public void Show(PurchaseOffer offer, Action<RectTransform>? picture, Action buy, Action? cancelled = null, string? note = null)
        {
            _confirmation.Ask(offer, buy);
            _cancelled = cancelled;
            for (int i = _picture.childCount - 1; i >= 0; i--)
            {
                Destroy(_picture.GetChild(i).gameObject);
            }

            picture?.Invoke(_picture);
            _question.text = Loc.F(offer.RealMoney ? "purchase.question_money" : "purchase.question_petals", offer.Name, offer.PriceText);
            _petals.gameObject.SetActive(!offer.RealMoney);
            _money.gameObject.SetActive(offer.RealMoney);
            if (offer.RealMoney)
            {
                UiKit.PillText(_money).text = offer.PriceText;
            }
            else
            {
                _petals.SetCost(Cost.Petals(offer.Petals));
            }

            _note.text = note ?? (offer.RealMoney ? Loc.T("purchase.store_next") : string.Empty);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Asks to confirm a Petals purchase, showing the balance under the price; short Petals say so through
        /// <paramref name="refused"/> without asking.
        /// </summary>
        public void ShowPetals(PurchaseOffer offer, long balance, Action<RectTransform>? picture, Action buy, Action refused, Action? cancelled = null)
        {
            if (!offer.Affordable(balance))
            {
                GameFeedback.Current?.Play(SoundCue.Refused);
                refused();
                return;
            }

            Show(offer, picture, buy, cancelled, Loc.F("purchase.balance", NumberText.Group(balance)));
        }

        /// <summary>Closes the card without buying (Cancel, the system back): nothing is bought.</summary>
        public void Cancel()
        {
            if (!_root.activeSelf && !_confirmation.Asking)
            {
                return;
            }

            _root.SetActive(false);
            _confirmation.Cancel();
            Action? cancelled = _cancelled;
            _cancelled = null;
            cancelled?.Invoke();
        }

        /// <summary>Buy: the card closes, then the purchase runs once.</summary>
        private void Buy()
        {
            _root.SetActive(false);
            _cancelled = null;
            _confirmation.Confirm();
        }
    }
}
