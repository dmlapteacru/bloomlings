using System;
using Bloomlings.Client.UI;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Pause with Resume, Restart and Leave, all free of penalty (FR-030, FR-040; T049).</summary>
    public sealed class PauseScreen : MonoBehaviour
    {
        private GameObject _root = null!;

        public bool IsOpen => _root.activeSelf;

        public static PauseScreen Create(Transform parent, Action onResume, Action onRestart, Action onLeave)
        {
            RectTransform card = UiFactory.CreateModal("PauseScreen", parent, 0.34f, out GameObject root);
            var screen = root.AddComponent<PauseScreen>();
            screen._root = root;
            TMPro.TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("pause.title"), 72f, UiTheme.Text);
            UiFactory.Place(title.rectTransform, 0f, 0.78f, 1f, 0.95f);
            Button resume = UiFactory.CreateButton("Resume", card, Loc.T("pause.resume"), UiTheme.Accent, onResume);
            UiFactory.Place((RectTransform)resume.transform, 0.15f, 0.54f, 0.85f, 0.72f);
            Button restart = UiFactory.CreateButton("Restart", card, Loc.T("common.restart"), UiTheme.Text, onRestart);
            UiFactory.Place((RectTransform)restart.transform, 0.15f, 0.30f, 0.85f, 0.48f);
            Button leave = UiFactory.CreateButton("Leave", card, Loc.T("pause.leave"), UiTheme.SlotLocked, onLeave);
            UiFactory.Place((RectTransform)leave.transform, 0.15f, 0.06f, 0.85f, 0.24f);
            root.SetActive(false);
            return screen;
        }

        public void Show() => _root.SetActive(true);

        public void Hide() => _root.SetActive(false);
    }
}
