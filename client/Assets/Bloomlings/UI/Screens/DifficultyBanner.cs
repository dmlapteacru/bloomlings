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
    /// treatment (FR-059, T065). Tap to dismiss; it also fades out by itself.
    /// </summary>
    public sealed class DifficultyBanner : MonoBehaviour
    {
        private const float ShowSeconds = 1.6f;

        private static readonly Color HardColor = new Color(0.93f, 0.55f, 0.18f);
        private static readonly Color SuperHardColor = new Color(0.78f, 0.22f, 0.36f);

        private GameObject _root = null!;
        private Image _panel = null!;
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
            banner._panel = UiFactory.CreateImage("Panel", shade.transform, ProceduralSprites.RoundedSquare, HardColor);
            UiFactory.Place(banner._panel.rectTransform, 0.08f, 0.55f, 0.92f, 0.7f);
            banner._icon = UiFactory.CreateImage("Icon", banner._panel.transform, ProceduralSprites.Star, Color.white);
            banner._icon.preserveAspect = true;
            UiFactory.Place(banner._icon.rectTransform, 0.04f, 0.15f, 0.24f, 0.85f);
            banner._label = UiFactory.CreateText("Label", banner._panel.transform, string.Empty, 88f, Color.white);
            banner._label.fontStyle = FontStyles.Bold;
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
            _panel.color = super ? SuperHardColor : HardColor;
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
