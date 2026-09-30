using System;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The pause card of the design board's frame 11 (spec 002 FR-018; FR-030, FR-040; T049): "PAUSED", RESUME
    /// (primary), RESTART, SETTINGS and HOME (secondary), and the round close button. Leaving or restarting costs nothing.
    /// </summary>
    public sealed class PauseScreen : MonoBehaviour
    {
        private GameObject _root = null!;

        public bool IsOpen => _root.activeSelf;

        /// <param name="onSettings">Opens Settings over the pause card; null hides the button.</param>
        public static PauseScreen Create(Transform parent, Action onResume, Action onRestart, Action onLeave, Action? onSettings = null)
        {
            float content = DesignTokens.Size.PrimaryHeightSmall + (3f * DesignTokens.Size.SecondaryHeight) + (4f * 26f) + 8f;
            CardView card = UiKit.Card("PauseScreen", parent, Loc.T("pause.title"), content, onResume, DesignTokens.Type.TitleCaps);
            var screen = card.Root.AddComponent<PauseScreen>();
            screen._root = card.Root;
            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top + (16f * u);
            Box Take(float height)
            {
                var b = new Box(body.Left + (20f * u), y, body.Right - (20f * u), y + (height * u));
                y = b.Bottom + (26f * u);
                return b;
            }

            Button resume = UiKit.PrimaryButton("Resume", card.Body, Loc.T("pause.resume"), onResume);
            UiKit.PlaceBox((RectTransform)resume.transform, Take(DesignTokens.Size.PrimaryHeightSmall), body);
            Button restart = UiKit.SecondaryButton("Restart", card.Body, Loc.T("common.restart"), onRestart);
            UiKit.PlaceBox((RectTransform)restart.transform, Take(DesignTokens.Size.SecondaryHeight), body);
            Box settingsBox = Take(DesignTokens.Size.SecondaryHeight);
            if (onSettings != null)
            {
                Button settings = UiKit.SecondaryButton("Settings", card.Body, Loc.T("pause.settings"), onSettings);
                UiKit.PlaceBox((RectTransform)settings.transform, settingsBox, body);
            }

            Button leave = UiKit.SecondaryButton("Home", card.Body, Loc.T("pause.home"), onLeave);
            UiKit.PlaceBox((RectTransform)leave.transform, Take(DesignTokens.Size.SecondaryHeight), body);
            card.Root.SetActive(false);
            return screen;
        }

        public void Show() => _root.SetActive(true);

        public void Hide() => _root.SetActive(false);
    }
}
