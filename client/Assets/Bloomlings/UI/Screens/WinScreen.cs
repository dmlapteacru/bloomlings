using System;
using System.Collections;
using Bloomlings.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The win sequence (FR-025, T050): the finished picture is revealed in full first, then a reward panel (a
    /// placeholder until the economy of US5), then Next.
    /// </summary>
    public sealed class WinScreen : MonoBehaviour
    {
        private const float RevealSeconds = 1.2f;

        private GameObject _root = null!;
        private TextMeshProUGUI _reward = null!;

        public static WinScreen Create(Transform parent, Action onNext)
        {
            Image panel = UiFactory.CreateImage("WinScreen", parent, Art.ProceduralSprites.RoundedSquare, UiTheme.Panel, raycast: true);
            UiFactory.Place(panel.rectTransform, 0.08f, 0.04f, 0.92f, 0.36f);
            var screen = panel.gameObject.AddComponent<WinScreen>();
            screen._root = panel.gameObject;
            TextMeshProUGUI title = UiFactory.CreateText("Title", panel.transform, "Restored!", 80f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.7f, 1f, 0.95f);
            screen._reward = UiFactory.CreateText("Reward", panel.transform, string.Empty, 52f, UiTheme.Text);
            UiFactory.Place(screen._reward.rectTransform, 0f, 0.42f, 1f, 0.66f);
            Button next = UiFactory.CreateButton("Next", panel.transform, "Next", UiTheme.Accent, onNext);
            UiFactory.Place((RectTransform)next.transform, 0.25f, 0.08f, 0.75f, 0.36f);
            panel.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>Waits for the picture reveal, then shows the panel.</summary>
        public void Show(MonoBehaviour host, string rewardText)
        {
            _reward.text = rewardText;
            host.StartCoroutine(ShowAfterReveal());
        }

        public void Hide() => _root.SetActive(false);

        private IEnumerator ShowAfterReveal()
        {
            yield return new WaitForSecondsRealtime(RevealSeconds);
            _root.SetActive(true);
        }
    }
}
