using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>The Store's clearing-style cards' buttons (spec 005 FR-038 as amended on 2026-10-06, contracts/look.md §6.12).</summary>
    public static partial class UiKit
    {
        /// <summary>
        /// A clearing style card's action button (<c>ui.button.clearing</c>, the playtest's <c>Kit.ClearingButton</c>), placed
        /// at <see cref="ClearingCard.Button"/> of the card: the glossy green "Buy" with the lotus and the price, the cream
        /// "Choose", or "Chosen" with a green check on a flat cream plate (<see cref="ClearingButtonView.Show"/>). It takes no
        /// tap of its own: the whole card is the button.
        /// </summary>
        public static ClearingButtonView ClearingButton(string name, Transform parent)
        {
            (RectTransform root, BoxLayout _) = Element(name, parent);
            var view = root.gameObject.AddComponent<ClearingButtonView>();
            view.Build(root);
            return view;
        }
    }

    /// <summary>A clearing style card's action button built by <see cref="UiKit.ClearingButton"/>.</summary>
    public sealed class ClearingButtonView : MonoBehaviour
    {
        private GardenButton _buy = null!;
        private TextMeshProUGUI _price = null!;
        private Image _lotus = null!;
        private GardenButton _choose = null!;
        private RectTransform _chosen = null!;

        internal void Build(RectTransform root)
        {
            // Buy: the lotus and the price, no "Buy" word, laid out together and shrunk together to fit (ClearingCard.PriceParts).
            _buy = UiKit.RaisedButton("Buy", root, GardenLook.Green, 0.5f, gloss: true, raycast: false);
            UiFactory.Stretch((RectTransform)_buy.transform);
            TextLook on = TextLook.OnColor(GardenLook.Green);
            _lotus = UiKit.PetalIcon("Lotus", root);
            _price = UiKit.KitLabel("Price", root, string.Empty, T.ButtonSecondary, on);
            BoxLayout.On(root).Watch(_price).Then(b =>
            {
                float size = ClearingCard.LabelSize(b);
                (Box lotus, Box price, float scale) = ClearingCard.PriceParts(b, KitText.Measure(_price, size));
                BoxLayout.Place(_lotus.rectTransform, lotus);
                KitText.Place(_price, T.ButtonSecondary, price.CenterX, price.CenterY, size * scale, price.Width + 1f);
            });

            // Choose: the cream face with the brown label.
            _choose = UiKit.RaisedButton("Choose", root, GardenLook.Cream, 0.5f, raycast: false);
            UiFactory.Stretch((RectTransform)_choose.transform);
            TextMeshProUGUI choose = UiKit.KitLabel("Label", _choose.Content, Loc.T("clearing.choose"), T.ButtonSecondary, GardenLook.LabelOn(GardenLook.Cream));
            BoxLayout.On(_choose.Content).Then(f => KitText.Place(choose, T.ButtonSecondary, f.CenterX, f.CenterY, root.rect.height * ClearingCard.LabelShare, f.Width * 0.86f));

            // Chosen: a flat cream plate in a green outline, the check and the label in green.
            _chosen = UiFactory.Stretch(UiFactory.CreateRect("Chosen", root));
            BoxLayout chosen = BoxLayout.On(_chosen);
            ColorSet green = GardenLook.Green;
            UiKit.SoftShadow(chosen, b => b, b => b.Height / 2f, 0.14f, 0.08f);
            Image plate = UiKit.RoundGradient("Plate", _chosen, C.CreamTop, C.CreamFace);
            Image line = UiKit.RoundRing("Line", _chosen, UiTheme.Of(green.Face), null, b => Mathf.Max(UiKit.Units(2f), b.Height * 0.05f));
            Image check = UiKit.ShapeImage("Check", _chosen, "ui.check", green.Face);
            TextMeshProUGUI label = UiKit.KitLabel("Label", _chosen, Loc.T("clearing.chosen"), T.ButtonSecondary, TextLook.Plain(green.Line));
            chosen.Add(plate.rectTransform, b => b).Add(line.rectTransform, b => b).Watch(label).Then(b =>
            {
                float h = b.Height;
                float size = ClearingCard.LabelSize(b);
                float text = Mathf.Min(KitText.Measure(label, size), b.Width - (h * 1.4f));
                float mark = h * 0.56f;
                float start = b.CenterX - ((mark + (h * 0.12f) + text) / 2f);
                BoxLayout.Place(check.rectTransform, Box.FromCenter(start + (mark / 2f), b.CenterY, mark, mark));
                KitText.Place(label, T.ButtonSecondary, start + mark + (h * 0.12f) + (text / 2f), b.CenterY, size, text + 1f);
            });
            Show(ClearingAction.Locked, 0);
        }

        /// <summary>Shows the button for <paramref name="action"/>; <see cref="ClearingAction.Locked"/> hides it (the card keeps its cost pill).</summary>
        public void Show(ClearingAction action, int price)
        {
            bool buy = action == ClearingAction.Buy;
            _buy.gameObject.SetActive(buy);
            _lotus.gameObject.SetActive(buy);
            _price.gameObject.SetActive(buy);
            _price.text = NumberText.Group(price);
            _choose.gameObject.SetActive(action == ClearingAction.Choose);
            _chosen.gameObject.SetActive(action == ClearingAction.Chosen);
            BoxLayout.On((RectTransform)transform).Apply();
        }
    }
}
