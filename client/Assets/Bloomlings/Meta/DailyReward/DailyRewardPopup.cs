using System;
using System.Globalization;
using Bloomlings.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.Meta.DailyReward
{
    /// <summary>
    /// The Daily Reward popup on Home (FR-055, T134): today's Petals and streak, one Claim, and an optional rewarded-ad
    /// bonus the player may start (FR-052). It opens by itself once a day while a claim is due.
    /// </summary>
    public sealed class DailyRewardPopup : MonoBehaviour
    {
        private GameObject _root = null!;
        private TextMeshProUGUI _text = null!;
        private Button _claim = null!;
        private Button _bonus = null!;

        public static DailyRewardPopup Create(Transform parent)
        {
            RectTransform card = UiFactory.CreateModal("DailyReward", parent, 0.4f, out GameObject root);
            var popup = root.AddComponent<DailyRewardPopup>();
            popup._root = root;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("daily_reward.title"), 64f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.78f, 1f, 0.96f);
            popup._text = UiFactory.CreateText("Reward", card, string.Empty, 48f, UiTheme.Text);
            UiFactory.Place(popup._text.rectTransform, 0f, 0.5f, 1f, 0.76f);
            popup._claim = UiFactory.CreateButton("Claim", card, Loc.T("daily_reward.claim"), UiTheme.Accent, () => { });
            UiFactory.Place((RectTransform)popup._claim.transform, 0.08f, 0.1f, 0.48f, 0.42f);
            popup._bonus = UiFactory.CreateButton("Bonus", card, Loc.T("daily_reward.watch"), UiTheme.Warning, () => { }, 36f);
            UiFactory.Place((RectTransform)popup._bonus.transform, 0.52f, 0.1f, 0.92f, 0.42f);
            root.SetActive(false);
            return popup;
        }

        /// <param name="bonusAvailable">A rewarded ad is ready and today's bonus is unused.</param>
        public void Show(int petals, int streak, bool bonusAvailable, Func<int> claim, Action<Action<int>> watchBonus)
        {
            _text.text = Loc.F("common.petals_plus", petals) + "\n" + Loc.F("daily_reward.day", streak);
            _claim.onClick.RemoveAllListeners();
            _claim.onClick.AddListener(() =>
            {
                claim();
                Hide();
            });
            _bonus.gameObject.SetActive(bonusAvailable);
            _bonus.onClick.RemoveAllListeners();
            _bonus.onClick.AddListener(() => watchBonus(extra =>
            {
                _bonus.gameObject.SetActive(false);
                if (extra > 0)
                {
                    _text.text += "\n" + Loc.F("daily_reward.bonus", extra);
                }
            }));
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
