using System;
using System.Collections;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The win card of the design board's frame 15 (spec 002 FR-020; FR-025, T050, T122).
    /// <list type="bullet">
    /// <item><description>The finished picture is revealed on the board first.</description></item>
    /// <item><description>Then this card rises below it: the Petals earned with the Petal symbol (and any booster
    /// drop), NEXT as the primary button, and the optional "×2 reward" rewarded ad.</description></item>
    /// <item><description>A milestone level shows its milestone card after NEXT (<see cref="MilestoneCard"/>).</description></item>
    /// </list>
    /// </summary>
    public sealed class WinScreen : MonoBehaviour
    {
        private const float RevealSeconds = 1.2f;

        private GameObject _root = null!;
        private TextMeshProUGUI _reward = null!;
        private Button _double = null!;
        private GameObject _milestoneMark = null!;

        public static WinScreen Create(Transform parent, Action onNext)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            Box safe = ScreenLayout.SafeArea(w, h, insets);
            var box = new Box(safe.CenterX - (Mathf.Min(safe.Width * 0.86f, 920f * u) / 2f), safe.Bottom - (40f * u) - (620f * u), safe.CenterX + (Mathf.Min(safe.Width * 0.86f, 920f * u) / 2f), safe.Bottom - (40f * u));
            Image panel = UiKit.Rounded("WinScreen", parent, UiTheme.Panel, 56f, raycast: true);
            UiKit.CardShadow(panel);
            UiKit.PlaceScreen(panel.rectTransform, box);
            var screen = panel.gameObject.AddComponent<WinScreen>();
            screen._root = panel.gameObject;
            TextMeshProUGUI title = UiKit.Label("Title", panel.transform, Loc.T("win.title"), DesignTokens.Type.Title, UiTheme.Text);
            UiFactory.Place(title.rectTransform, 0.06f, 0.78f, 0.94f, 0.96f);

            screen._reward = UiKit.Label("Reward", panel.transform, string.Empty, DesignTokens.Type.Reward, UiTheme.Text);
            screen._reward.outlineWidth = 0f;
            UiFactory.Place(screen._reward.rectTransform, 0.06f, 0.56f, 0.8f, 0.78f);
            Image petal = UiKit.PetalIcon("Petal", panel.transform);
            UiFactory.Place(petal.rectTransform, 0.8f, 0.58f, 0.92f, 0.76f);

            Button next = UiKit.PrimaryButton("Next", panel.transform, Loc.T("common.next"), onNext);
            UiFactory.Place((RectTransform)next.transform, 0.08f, 0.28f, 0.92f, 0.5f);
            screen._double = UiKit.SecondaryButton("Double", panel.transform, Loc.T("win.double"), () => { }, "ui.ad");
            UiFactory.Place((RectTransform)screen._double.transform, 0.2f, 0.06f, 0.8f, 0.22f);

            Image mark = UiKit.Pill("Milestone", panel.transform, UiTheme.Of(DesignTokens.Colors.MedalGold));
            UiFactory.Place(mark.rectTransform, 0.3f, 0.96f, 0.7f, 1.06f);
            TextMeshProUGUI markText = UiKit.Label("Text", mark.transform, Loc.T("milestone.reached"), DesignTokens.Type.Badge, UiTheme.Text);
            UiFactory.Place(markText.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
            screen._milestoneMark = mark.gameObject;
            panel.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>Waits for the picture reveal, then shows the card.</summary>
        /// <param name="doubleReward">The optional rewarded ad that doubles the Petals (FR-052); null hides it.</param>
        /// <param name="milestone">A milestone was granted: a small mark says so, and its card follows NEXT.</param>
        public void Show(MonoBehaviour host, string rewardText, Action<Action<string>>? doubleReward = null, bool milestone = false)
        {
            _reward.text = rewardText;
            _milestoneMark.SetActive(milestone);
            _double.gameObject.SetActive(doubleReward != null);
            _double.onClick.RemoveAllListeners();
            if (doubleReward != null)
            {
                _double.onClick.AddListener(() => doubleReward(text =>
                {
                    _reward.text = text;
                    _double.gameObject.SetActive(false);
                }));
            }

            host.StartCoroutine(ShowAfterReveal());
        }

        public void Hide() => _root.SetActive(false);

        private IEnumerator ShowAfterReveal()
        {
            yield return new WaitForSecondsRealtime(RevealSeconds);
            _root.SetActive(true);
            yield return Bloomlings.Client.Gameplay.Effects.UiFx.Pop(_root.transform, 1.06f, 0.25f);
        }
    }
}
