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

    /// <summary>Compact item preview, balance, price on Buy and Cancel; the purchase runs once after confirmation.</summary>
    public sealed class PurchaseCardView : MonoBehaviour
    {
        private readonly PurchaseConfirmation _confirmation = new PurchaseConfirmation();
        private GameObject _root = null!;
        private RectTransform _picture = null!;
        private TextMeshProUGUI _question = null!;
        private TextMeshProUGUI _note = null!;
        private TextMeshProUGUI _buyLabel = null!;
        private Image _buyPetal = null!;
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

            _picture = UiFactory.CreateRect("Picture",at);
            UiKit.PlaceBox(_picture,r.Picture,body);
            _question=UiKit.Label("Item",at,string.Empty,PopupDesign.Label,UiTheme.Of(C.InkBrown),look:TextLook.Plain(C.InkBrown));
            _question.textWrappingMode=TextWrappingModes.Normal;UiKit.PlaceBox(_question.rectTransform,r.Question,body);
            _note = UiKit.Label("Note", at, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(_note.rectTransform, r.Note, body);

            Button buy = UiKit.PrimaryButton("Buy", at, Loc.T("purchase.buy"), Buy);
            UiKit.PlaceBox((RectTransform)buy.transform, r.Confirm, body);
            _buyLabel=buy.GetComponentInChildren<TextMeshProUGUI>();
            GardenButton buyView=buy.GetComponent<GardenButton>();
            _buyPetal=UiKit.PetalIcon("PricePetal",buyView.Content);
            BoxLayout.On(buyView.Content).Watch(_buyLabel).Then(b=>
            {
                Box f=PrimaryActionDesign.Content(b,false);TypeStyle type=PrimaryActionDesign.Type(T.Button);
                if(_buyPetal.gameObject.activeSelf) UiKit.PriceButtonContent(_buyPetal.rectTransform,_buyLabel,type,f);
                else KitText.Place(_buyLabel,type,f.CenterX,f.CenterY,UiKit.Units(type.Size),f.Width-f.Height*.3f);
            });
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            Button cancel = UiKit.SecondaryButton("Cancel", at, Loc.T("common.cancel"), Cancel);
            UiKit.PlaceBox((RectTransform)cancel.transform, r.Cancel.Inset(40f * u, 0f), body);
        }

        /// <summary>
        /// Asks to confirm <paramref name="offer"/> and opens the card over its siblings: <paramref name="picture"/> puts the
        /// item's picture into the preview (a booster's icon, the hero in the outfit, the style's live preview, the avatar);
        /// <paramref name="buy"/> runs only on Buy, <paramref name="cancelled"/> when Cancel closes it. <paramref name="note"/>
        /// is the caption above the purchase action (by default the store's sheet follows, for real money).
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
            _question.text = offer.Name;
            _buyLabel.text = Loc.T("purchase.buy") + "  " + offer.PriceText;
            _buyPetal.gameObject.SetActive(!offer.RealMoney);
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
