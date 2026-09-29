using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>What the Daily Challenge card shows.</summary>
    public sealed record DailyChallengeModel(string UtcDate, bool CompletedToday, int RewardPetals);

    /// <summary>
    /// The Daily Challenge card (FR-064, T144): today's puzzle, the same for every player, with its own reward. Playing
    /// it never changes Level N; Home's Play button always continues the main sequence.
    /// </summary>
    public sealed class DailyChallengeScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private TextMeshProUGUI _date = null!;
        private TextMeshProUGUI _body = null!;
        private Button _play = null!;

        public bool IsOpen => _root.activeSelf;

        public static DailyChallengeScreen Create(Transform parent, Action onPlay)
        {
            RectTransform card = UiFactory.CreateModal("DailyChallenge", parent, 0.42f, out GameObject root);
            var screen = root.AddComponent<DailyChallengeScreen>();
            screen._root = root;
            TextMeshProUGUI title = UiFactory.CreateText("Title", card, "Daily Challenge", 68f, UiTheme.Accent);
            UiFactory.Place(title.rectTransform, 0f, 0.8f, 1f, 0.96f);
            screen._date = UiFactory.CreateText("Date", card, string.Empty, 40f, UiTheme.Text);
            UiFactory.Place(screen._date.rectTransform, 0f, 0.68f, 1f, 0.8f);
            screen._body = UiFactory.CreateText("Body", card, string.Empty, 40f, UiTheme.Text);
            UiFactory.Place(screen._body.rectTransform, 0.05f, 0.42f, 0.95f, 0.66f);
            screen._play = UiFactory.CreateButton("Play", card, "Play", UiTheme.Accent, () =>
            {
                screen.Hide();
                onPlay();
            }, 56f);
            UiFactory.Place((RectTransform)screen._play.transform, 0.2f, 0.2f, 0.8f, 0.38f);
            Button close = UiFactory.CreateButton("Close", card, "Close", UiTheme.Text, screen.Hide, 40f);
            UiFactory.Place((RectTransform)close.transform, 0.3f, 0.03f, 0.7f, 0.17f);
            root.SetActive(false);
            return screen;
        }

        public void Show(DailyChallengeModel model)
        {
            _date.text = model.UtcDate + " (UTC)";
            _body.text = model.CompletedToday
                ? "Done for today! A new puzzle arrives at midnight UTC."
                : "One puzzle, the same for everyone today. Reward: " + model.RewardPetals.ToString(System.Globalization.CultureInfo.InvariantCulture) + " Petals.";
            _play.GetComponentInChildren<TextMeshProUGUI>().text = model.CompletedToday ? "Play again" : "Play";
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
