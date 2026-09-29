using System;
using System.Collections;
using Bloomlings.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The win sequence (FR-025, T050, T122): the finished picture is revealed in full first, then the reward (the Petals
    /// earned and any booster drop), then Next. A milestone level adds a pulsing ribbon above the panel (FR-061, "a
    /// short celebration").
    /// </summary>
    public sealed class WinScreen : MonoBehaviour
    {
        private const float RevealSeconds = 1.2f;

        private GameObject _root = null!;
        private TextMeshProUGUI _reward = null!;
        private Button _double = null!;
        private RectTransform _ribbon = null!;
        private float _time;

        public static WinScreen Create(Transform parent, Action onNext)
        {
            Image panel = UiFactory.CreateImage("WinScreen", parent, Art.ProceduralSprites.RoundedSquare, UiTheme.Panel, raycast: true);
            UiFactory.Place(panel.rectTransform, 0.08f, 0.04f, 0.92f, 0.36f);
            var screen = panel.gameObject.AddComponent<WinScreen>();
            screen._root = panel.gameObject;
            TextMeshProUGUI title = UiFactory.CreateText("Title", panel.transform, Loc.T("win.title"), 80f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.7f, 1f, 0.95f);
            screen._reward = UiFactory.CreateText("Reward", panel.transform, string.Empty, 52f, UiTheme.Text);
            UiFactory.Place(screen._reward.rectTransform, 0f, 0.42f, 1f, 0.66f);
            Button next = UiFactory.CreateButton("Next", panel.transform, Loc.T("common.next"), UiTheme.Accent, onNext);
            UiFactory.Place((RectTransform)next.transform, 0.05f, 0.08f, 0.47f, 0.36f);
            screen._double = UiFactory.CreateButton("Double", panel.transform, Loc.T("win.double"), UiTheme.Warning, () => { }, 40f);
            UiFactory.Place((RectTransform)screen._double.transform, 0.53f, 0.08f, 0.95f, 0.36f);

            Image ribbon = UiFactory.CreateImage("Milestone", panel.transform, Art.ProceduralSprites.RoundedSquare, UiTheme.Warning);
            UiFactory.Place(ribbon.rectTransform, 0.12f, 1.02f, 0.88f, 1.2f);
            TextMeshProUGUI ribbonText = UiFactory.CreateText("Text", ribbon.transform, Loc.T("win.milestone_title"), 48f, UiTheme.TextOnColor);
            UiFactory.Place(ribbonText.rectTransform, 0.15f, 0f, 0.85f, 1f);
            foreach (float x in new[] { 0.02f, 0.86f })
            {
                Image star = UiFactory.CreateImage("Star", ribbon.transform, Art.ProceduralSprites.Star, Color.white);
                star.preserveAspect = true;
                UiFactory.Place(star.rectTransform, x, 0.1f, x + 0.12f, 0.9f);
            }

            screen._ribbon = ribbon.rectTransform;
            panel.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>Waits for the picture reveal, then shows the panel.</summary>
        /// <param name="doubleReward">The optional rewarded ad that doubles the Petals (FR-052); null hides it.</param>
        /// <param name="milestone">A milestone was granted: its ribbon shows.</param>
        public void Show(MonoBehaviour host, string rewardText, Action<Action<string>>? doubleReward = null, bool milestone = false)
        {
            _reward.text = rewardText;
            _ribbon.gameObject.SetActive(milestone);
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
            _time = 0f;
            yield return Bloomlings.Client.Gameplay.Effects.UiFx.Pop(_root.transform, 1.06f, 0.25f);
        }

        private void Update()
        {
            if (_ribbon.gameObject.activeSelf)
            {
                _time += Time.unscaledDeltaTime;
                _ribbon.localScale = Vector3.one * (1f + (0.05f * Mathf.Sin(_time * 5f)));
            }
        }
    }
}
