using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.Meta.DailyReward
{
    /// <summary>
    /// The Daily Reward popup of the design board's frame 4 (spec 002 FR-022; FR-055, T134). It shows:
    /// <list type="bullet">
    /// <item><description>"Daily Rewards" and "Day N";</description></item>
    /// <item><description>the reward basket heaped with Petals, "+N" with the Petal symbol;</description></item>
    /// <item><description>CLAIM, and the optional rewarded-ad bonus the player may start (FR-052).</description></item>
    /// </list>
    /// The bonus claims the reward together with its extra Petals, so it is earned at most once a day. The popup opens by
    /// itself once a day while a claim is due.
    /// </summary>
    public sealed class DailyRewardPopup : MonoBehaviour
    {
        private CardView _card = null!;
        private TextMeshProUGUI _day = null!;
        private TextMeshProUGUI _amount = null!;
        private Button _claim = null!;
        private Button _bonus = null!;

        public static DailyRewardPopup Create(Transform parent)
        {
            CardView card = UiKit.Card("DailyReward", parent, Loc.T("daily_reward.title"), 60f + 330f + 110f + DesignTokens.Size.PrimaryHeightSmall + DesignTokens.Size.SecondaryHeight + 50f, null);
            var popup = card.Root.AddComponent<DailyRewardPopup>();
            popup._card = card;
            Button close = UiKit.RoundIconButton("Close", card.CardRect, "ui.close", popup.Hide);
            UiKit.PlaceBox((RectTransform)close.transform, card.Regions.Close, card.Regions.Card);

            RectTransform body = card.Body;
            popup._day = UiKit.Label("Day", body, string.Empty, DesignTokens.Type.Body, UiTheme.TextSecondary);
            UiFactory.Place(popup._day.rectTransform, 0f, 0.9f, 1f, 1f);

            // The reward basket (placeholder art: a basket heaped with Petal symbols).
            RectTransform art = UiFactory.Place(UiFactory.CreateRect("Basket", body), 0.25f, 0.5f, 0.75f, 0.9f);
            foreach ((float x0, float y0, float s) in new[] { (0.05f, 0.42f, 0.34f), (0.6f, 0.42f, 0.36f), (0.32f, 0.5f, 0.38f), (0.2f, 0.3f, 0.3f), (0.48f, 0.3f, 0.3f) })
            {
                Image petal = UiKit.PetalIcon("Petal", art);
                UiFactory.Place(petal.rectTransform, x0, y0, x0 + s, y0 + s);
            }

            Image basket = UiFactory.CreateImage("BasketShape", art, ProceduralSprites.Shape("currency.reward_basket", 192), UiTheme.Of(DesignTokens.Colors.RewardBasket));
            basket.preserveAspect = true;
            UiFactory.Place(basket.rectTransform, 0.05f, -0.05f, 0.95f, 0.5f);

            popup._amount = UiKit.Label("Amount", body, string.Empty, DesignTokens.Type.Reward, UiTheme.Text);
            popup._amount.outlineWidth = 0f;
            UiFactory.Place(popup._amount.rectTransform, 0.1f, 0.37f, 0.78f, 0.5f);
            Image symbol = UiKit.PetalIcon("Petal", body);
            UiFactory.Place(symbol.rectTransform, 0.72f, 0.38f, 0.84f, 0.49f);

            popup._claim = UiKit.PrimaryButton("Claim", body, Loc.T("daily_reward.claim"), () => { });
            UiFactory.Place((RectTransform)popup._claim.transform, 0.08f, 0.18f, 0.92f, 0.34f);
            popup._bonus = UiKit.SecondaryButton("Bonus", body, Loc.T("daily_reward.watch"), () => { }, "ui.ad");
            UiFactory.Place((RectTransform)popup._bonus.transform, 0.18f, 0.02f, 0.82f, 0.15f);
            card.Root.SetActive(false);
            return popup;
        }

        /// <param name="bonusAvailable">A rewarded ad is ready and today's bonus is unused.</param>
        /// <param name="watchBonus">Shows the ad; reports the Petals paid (claim and bonus), 0 when nothing was earned.</param>
        /// <param name="bonusPetals">The bonus amount for "Get +N"; 0 shows "Watch for more".</param>
        public void Show(int petals, int streak, bool bonusAvailable, Func<int> claim, Action<Action<int>> watchBonus, int bonusPetals = 0)
        {
            _day.text = Loc.F("daily_reward.day", streak);
            _amount.text = NumberText.Plus(petals);
            _claim.onClick.RemoveAllListeners();
            _claim.onClick.AddListener(() =>
            {
                claim();
                Hide();
            });
            _bonus.gameObject.SetActive(bonusAvailable);
            _bonus.GetComponentInChildren<TextMeshProUGUI>().text = bonusPetals > 0 ? Loc.F("daily_reward.bonus", bonusPetals) : Loc.T("daily_reward.watch");
            _bonus.onClick.RemoveAllListeners();
            _bonus.onClick.AddListener(() => watchBonus(total =>
            {
                if (total > 0)
                {
                    _bonus.gameObject.SetActive(false);
                    _amount.text = NumberText.Plus(total);
                    _claim.GetComponentInChildren<TextMeshProUGUI>().text = Loc.T("common.close");
                }
            }));
            _claim.GetComponentInChildren<TextMeshProUGUI>().text = Loc.T("daily_reward.claim");
            _card.Root.SetActive(true);
        }

        public void Hide() => _card.Root.SetActive(false);
    }
}
