using System.Collections;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// Shown before a Hard (label from L5) or Super Hard (label from L10) level starts, with its own color and icon
    /// treatment (FR-059, T065), in the badge colors of the design board (spec 002 FR-010). The level sign keeps the
    /// badge during play. Tap to dismiss; it also fades out by itself. In the reference look of spec 005 (research D6,
    /// contracts/look.md §3.2): a wooden sign whose letters take the badge color (red for Hard, purple for Super Hard, as
    /// the gameplay sign's letters on a Super Hard level), with the star (Hard) or the double star (Super Hard) in that
    /// color over a darker outline at both ends.
    /// </summary>
    public sealed class DifficultyBanner : MonoBehaviour
    {
        private const float ShowSeconds = 1.6f;
        private const float SignUnits = 150f;

        private GameObject _root = null!;
        private WoodSignView _sign = null!;
        private GameObject _hard = null!;
        private GameObject _super = null!;

        public static DifficultyBanner Create(Transform parent)
        {
            Image shade = UiFactory.CreateImage("DifficultyBanner", parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var banner = shade.gameObject.AddComponent<DifficultyBanner>();
            banner._root = shade.gameObject;
            Button dismiss = shade.gameObject.AddComponent<Button>();
            dismiss.onClick.AddListener(banner.Hide);

            // The stage keeps the banner's place on the screen; the sign is as wide as its letters and stars need.
            RectTransform stage = UiFactory.Place(UiFactory.CreateRect("Stage", shade.transform), 0.08f, 0.55f, 0.92f, 0.7f);
            banner._sign = UiKit.WoodSign("Sign", stage, string.Empty, T.Title, SignDecor.None, C.BadgeHard);
            (RectTransform hardLeft, RectTransform hardRight) = Stars(stage, "ui.star", C.BadgeHard, out banner._hard);
            (RectTransform superLeft, RectTransform superRight) = Stars(stage, "ui.star2", C.BadgeSuperHard, out banner._super);
            TextMeshProUGUI label = banner._sign.Label;
            BoxLayout.On(stage).Watch(label).Then(b =>
            {
                float h = Mathf.Min(b.Height, UiKit.Units(SignUnits));
                float em = Mathf.Min(UiKit.Units(T.Title.Size), h * 0.62f);
                float measured = KitText.Measure(label, em);
                float star = h * 0.56f;
                float width = Mathf.Min(b.Width, (measured > 0f ? measured : b.Width * 0.4f) + (2f * star) + (h * 1.2f));
                Box sign = Box.FromCenter(b.CenterX, b.CenterY, width, h);
                BoxLayout.Place((RectTransform)banner._sign.transform, sign);
                Box left = Box.FromCenter(sign.Left + (h * 0.25f) + (star / 2f), sign.CenterY - (h * 0.03f), star, star);
                Box right = Box.FromCenter(sign.Right - (h * 0.25f) - (star / 2f), sign.CenterY - (h * 0.03f), star, star);
                BoxLayout.Place(hardLeft, left);
                BoxLayout.Place(hardRight, right);
                BoxLayout.Place(superLeft, left);
                BoxLayout.Place(superRight, right);
            });
            shade.gameObject.SetActive(false);
            return banner;
        }

        /// <summary>Shows the banner when the class has a label and its unlock has been reached; returns whether it did.</summary>
        public bool TryShow(DifficultyClass difficulty, bool hardUnlocked, bool superHardUnlocked)
        {
            bool show = (difficulty == DifficultyClass.Hard && hardUnlocked) || (difficulty == DifficultyClass.SuperHard && superHardUnlocked);
            if (!show)
            {
                return false;
            }

            bool super = difficulty == DifficultyClass.SuperHard;
            _sign.SetLetters(super ? C.BadgeSuperHard : C.BadgeHard);
            _sign.Text = super ? Loc.T("difficulty.super_hard") : Loc.T("difficulty.hard");
            _hard.SetActive(!super);
            _super.SetActive(super);
            _root.SetActive(true);
            StartCoroutine(AutoHide());
            return true;
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>The two stars of a class, in its color over a darker outline, under one group to show or hide.</summary>
        private static (RectTransform Left, RectTransform Right) Stars(RectTransform stage, string shapeId, Rgba color, out GameObject group)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(shapeId, stage));
            group = root.gameObject;
            Image left = UiKit.OutlinedIcon("Left", root, shapeId, color, color.Darken(0.42f));
            Image right = UiKit.OutlinedIcon("Right", root, shapeId, color, color.Darken(0.42f));
            return (left.rectTransform, right.rectTransform);
        }

        private IEnumerator AutoHide()
        {
            yield return new WaitForSecondsRealtime(ShowSeconds);
            Hide();
        }
    }
}
