using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The Remove Ads card (spec 005 FR-033), opened by Home's No Ads scene at every level (the Store keeps its own Remove
    /// Ads row): a parchment card with the brown title and the cream round close, then the No Ads scene idling
    /// (<see cref="HomePromoView"/>, small and centered), the text in brown, the green buy button with the store's price
    /// ("Remove Ads · $2.99"; greyed "Unavailable" while the store is unavailable, with the offline line under the buttons)
    /// and the cream Restore Purchases below it. Once Remove Ads is owned (bought or restored) the card thanks the player,
    /// its button closes it, and it closes by itself a moment later. The purchase and the restore are the host's
    /// (<see cref="Create"/>), the Store's and Settings' own flows. Buy asks the purchase confirmation first (spec 005
    /// FR-040, <see cref="UiKit.PurchaseCard"/>, "Buy Remove Ads for $2.99?"); only its Buy starts the store's purchase,
    /// whose platform sheet then confirms the payment once more.
    /// </summary>
    public sealed class RemoveAdsCard : MonoBehaviour
    {
        private const float SceneUnits = 380f;
        private const float BodyUnits = 156f;
        private const float StatusUnits = 52f;

        /// <summary>How long the thanks stay before the card closes by itself, in seconds.</summary>
        private const float OwnedSeconds = 2.5f;

        private GameObject _root = null!;
        private HomePromoView _scene = null!;
        private TextMeshProUGUI _body = null!;
        private TextMeshProUGUI _status = null!;
        private Button _buy = null!;
        private TextMeshProUGUI _buyLabel = null!;
        private Button _restore = null!;
        private TextMeshProUGUI _restoreLabel = null!;
        private Action<Action> _purchase = _ => { };
        private Action<Action<bool>> _restorePurchases = done => done(false);
        private Func<bool> _owned = () => false;
        private PurchaseCardView _confirm = null!;
        private string? _price;
        private bool _available;
        private bool _thanked;
        private float _closeAt = -1f;

        public bool IsOpen => _root.activeSelf;

        /// <param name="purchase">Buys Remove Ads (the Store row's purchase), then calls back whatever the store answered.</param>
        /// <param name="restore">Restores purchases (Settings' flow) and reports whether the store answered.</param>
        /// <param name="owned">Whether Remove Ads is owned now (the purchase ledger).</param>
        public static RemoveAdsCard Create(Transform parent, Action<Action> purchase, Action<Action<bool>> restore, Func<bool> owned)
        {
            float content = 10f + (SceneUnits * HomePromo.HeightShare) + 16f + BodyUnits + 30f + DesignTokens.Size.CardPrimaryHeight + 24f + DesignTokens.Size.SecondaryHeight + 12f + StatusUnits + 20f;
            RemoveAdsCard screen = null!;
            CardView card = UiKit.Card("RemoveAds", parent, Loc.T("remove_ads.title"), content, () => screen.Hide(), sign: SignDecor.None);
            screen = card.Root.AddComponent<RemoveAdsCard>();
            screen._root = card.Root;
            screen._purchase = purchase;
            screen._restorePurchases = restore;
            screen._owned = owned;

            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top + (10f * u);

            // The No Ads scene at its idle pose (no attention sequence, no tap), centered.
            float side = Mathf.Min(SceneUnits * u, body.Width);
            screen._scene = HomePromoView.Create("Scene", card.Body, PromoScene.NoAds, calling: false);
            screen._scene.Place(HomePromo.SceneBox(body.CenterX - (side / 2f), y, side), body);
            y += (side * HomePromo.HeightShare) + (16f * u);

            // The text, wrapped over up to three lines and shrinking to fit.
            screen._body = UiKit.Label("Body", card.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            screen._body.textWrappingMode = TextWrappingModes.Normal;
            UiKit.PlaceBox(screen._body.rectTransform, new Box(body.Left + (body.Width * 0.03f), y, body.Right - (body.Width * 0.03f), y + (BodyUnits * u)), body);
            y += (BodyUnits + 30f) * u;

            // Buy: the green primary button with its leaves, breathing while it waits.
            Box buy = ScreenLayout.CardButton(body, y, true, u);
            screen._buy = UiKit.PrimaryButton("Buy", card.Body, Loc.T("remove_ads.buy"), screen.Buy, decorate: true, breathe: true);
            UiKit.PlaceBox((RectTransform)screen._buy.transform, buy, body);
            screen._buyLabel = screen._buy.GetComponentInChildren<TextMeshProUGUI>();

            // Restore Purchases: the cream secondary button, as in Settings.
            Box restoreBox = ScreenLayout.CardButton(body, buy.Bottom + (24f * u), false, u);
            screen._restore = UiKit.SecondaryButton("Restore", card.Body, Loc.T("remove_ads.restore"), screen.Restore);
            UiKit.PlaceBox((RectTransform)screen._restore.transform, restoreBox, body);
            screen._restoreLabel = screen._restore.GetComponentInChildren<TextMeshProUGUI>();

            // The offline line under the buttons, only while the store is unavailable.
            screen._status = UiKit.Label("Status", card.Body, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(screen._status.rectTransform, new Box(body.Left, restoreBox.Bottom + (12f * u), body.Right, restoreBox.Bottom + ((12f + StatusUnits) * u)), body);

            // The purchase confirmation (spec 005 FR-040), over this card when Buy asks.
            screen._confirm = UiKit.PurchaseCard(parent);
            card.Root.SetActive(false);
            return screen;
        }

        /// <summary>
        /// Opens the card with the store's localized <paramref name="price"/> (null while unknown: "Remove Ads" alone);
        /// while the store is not <paramref name="available"/>, the buy button is greyed "Unavailable".
        /// </summary>
        public void Show(string? price, bool available)
        {
            _available = available;
            _price = price;
            _thanked = false;
            _closeAt = -1f;
            _body.text = Loc.T("remove_ads.body");
            _buyLabel.text = !available ? Loc.T("store.unavailable") : price != null ? Loc.F("remove_ads.price", price) : Loc.T("remove_ads.buy");
            _buy.interactable = available;
            _restoreLabel.text = Loc.T("remove_ads.restore");
            _restore.interactable = true;
            _restore.gameObject.SetActive(true);
            _status.text = available ? string.Empty : Loc.T("store.offline");
            _root.SetActive(true);
            if (_owned())
            {
                Thank();
            }
        }

        public void Hide()
        {
            _closeAt = -1f;
            _confirm.Cancel();
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_closeAt >= 0f && Time.unscaledTime >= _closeAt)
            {
                Hide();
            }
        }

        private void Buy()
        {
            if (_thanked)
            {
                Hide();
                return;
            }

            if (!_available)
            {
                return;
            }

            // The purchase confirmation asks first (spec 005 FR-040); the platform's purchase sheet follows its Buy.
            var offer = PurchaseOffer.ForMoney(Loc.T("store.remove_ads"), _price ?? Loc.T("purchase.price_unknown"));
            _confirm.Show(offer, well =>
            {
                Image lotus = UiKit.PetalIcon("Lotus", well);
                UiFactory.Place(lotus.rectTransform, 0.14f, 0.14f, 0.86f, 0.86f);
            }, Purchase);
        }

        /// <summary>The confirmed purchase: one at a time, the button waits for the store's answer.</summary>
        private void Purchase()
        {
            if (!this || !_root.activeSelf)
            {
                return;
            }

            _buy.interactable = false;
            _purchase(() =>
            {
                if (!this || !_root.activeSelf)
                {
                    return;
                }

                if (_owned())
                {
                    Thank();
                }
                else
                {
                    _buy.interactable = _available;
                }
            });
        }

        private void Restore()
        {
            _restore.interactable = false;
            _restoreLabel.text = Loc.T("settings.restoring");
            _restorePurchases(ok =>
            {
                if (!this || !_root.activeSelf)
                {
                    return;
                }

                if (_owned())
                {
                    Thank();
                    return;
                }

                _restore.interactable = true;
                _restoreLabel.text = ok ? Loc.T("settings.restore_done") : Loc.T("settings.restore_failed");
            });
        }

        /// <summary>Remove Ads is owned: the thanks in the text's place, the button closes, and the card closes soon.</summary>
        private void Thank()
        {
            _thanked = true;
            _body.text = Loc.T("remove_ads.owned");
            _buyLabel.text = Loc.T("common.close");
            _buy.interactable = true;
            _restore.gameObject.SetActive(false);
            _status.text = string.Empty;
            _closeAt = Time.unscaledTime + OwnedSeconds;
        }
    }
}
