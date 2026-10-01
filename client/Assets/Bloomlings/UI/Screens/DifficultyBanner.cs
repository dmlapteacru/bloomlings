using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Core.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// Shown before a Hard (label from L5) or Super Hard (label from L10) level starts, with its own color and icon
    /// treatment (FR-059, T065), in the badge colors of the design board (spec 002 FR-010). The level pill keeps the
    /// badge during play. Tap to dismiss; it also fades out by itself.
    /// </summary>
    public sealed class DifficultyBanner : MonoBehaviour
    {
        private const float ShowSeconds = 1.6f;

        private GameObject _root = null!;
        private GardenButton _panel = null!;
        private Image _icon = null!;
        private TextMeshProUGUI _label = null!;

        public static DifficultyBanner Create(Transform parent)
        {
            Image shade = UiFactory.CreateImage("DifficultyBanner", parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var banner = shade.gameObject.AddComponent<DifficultyBanner>();
            banner._root = shade.gameObject;
            Button dismiss = shade.gameObject.AddComponent<Button>();
            dismiss.onClick.AddListener(banner.Hide);
            // An outlined sticker on a plate, red for Hard and purple for Super Hard (spec 003 FR-014).
            banner._panel = UiKit.Garden("Panel", shade.transform, Design.GardenLook.Red, 200f, raycast: false);
            UiFactory.Place((RectTransform)banner._panel.transform, 0.08f, 0.55f, 0.92f, 0.7f);
            banner._icon = UiKit.GardenGlyph(banner._panel, banner._panel.Content, "ui.star");
            UiFactory.Place(banner._icon.rectTransform, 0.04f, 0.15f, 0.24f, 0.85f);
            banner._label = UiKit.GardenLabel(banner._panel, string.Empty, Design.DesignTokens.Type.Title);
            UiFactory.Place(banner._label.rectTransform, 0.26f, 0f, 0.98f, 1f);
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
            _panel.SetColors(super ? Design.GardenLook.Purple : Design.GardenLook.Red);
            _icon.sprite = super ? ProceduralSprites.DoubleStar : ProceduralSprites.Star;
            _label.text = super ? Loc.T("difficulty.super_hard") : Loc.T("difficulty.hard");
            _root.SetActive(true);
            StartCoroutine(AutoHide());
            return true;
        }

        public void Hide() => _root.SetActive(false);

        private IEnumerator AutoHide()
        {
            yield return new WaitForSecondsRealtime(ShowSeconds);
            Hide();
        }
    }
}
