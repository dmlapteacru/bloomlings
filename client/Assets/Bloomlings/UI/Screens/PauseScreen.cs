using System;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The pause card of the design board's frame 11 (spec 002 FR-018; FR-030, FR-040; T049) in the reference look of
    /// spec 005 (contracts/look.md §4.3; the playtest's <c>MenuCards.Pause</c>): parchment with the brown "Paused" and
    /// the cream round close, the green Resume in its wooden rim, then Restart (⟳), Settings (gear) and Home (back arrow)
    /// as cream buttons with their brown glyphs. Leaving or restarting costs nothing.
    /// </summary>
    public sealed class PauseScreen : MonoBehaviour
    {
        private GameObject _root = null!;

        public bool IsOpen => _root.activeSelf;

        /// <param name="onSettings">Opens Settings over the pause card; null hides the button.</param>
        public static PauseScreen Create(Transform parent, Action onResume, Action onRestart, Action onLeave, Action? onSettings = null)
        {
            float content = DesignTokens.Size.CardPrimaryHeight + (3f * DesignTokens.Size.SecondaryHeight) + (4f * 28f) + 30f;
            CardView card = UiKit.Card("PauseScreen", parent, Loc.T("pause.title"), content, onResume, DesignTokens.Type.Title);
            var screen = card.Root.AddComponent<PauseScreen>();
            screen._root = card.Root;
            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top + (24f * u);
            Box Take(bool primary)
            {
                // The card's buttons are narrower and centered (spec 003 FR-011).
                Box b = ScreenLayout.CardButton(body, y, primary, u);
                y = b.Bottom + (28f * u);
                return b;
            }

            Button resume = UiKit.PrimaryButton("Resume", card.Body, Loc.T("pause.resume"), onResume, decorate: true);
            UiKit.PlaceBox((RectTransform)resume.transform, Take(true), body);
            Button restart = UiKit.SecondaryButton("Restart", card.Body, Loc.T("common.restart"), onRestart, "ui.restart");
            UiKit.PlaceBox((RectTransform)restart.transform, Take(false), body);
            Box settingsBox = Take(false);
            if (onSettings != null)
            {
                Button settings = UiKit.SecondaryButton("Settings", card.Body, Loc.T("pause.settings"), onSettings, "ui.settings");
                UiKit.PlaceBox((RectTransform)settings.transform, settingsBox, body);
            }

            Button leave = UiKit.SecondaryButton("Home", card.Body, Loc.T("pause.home"), onLeave, GardenLook.BackGlyph.ShapeId);
            UiKit.PlaceBox((RectTransform)leave.transform, Take(false), body);
            card.Root.SetActive(false);
            return screen;
        }

        public void Show() => _root.SetActive(true);

        public void Hide() => _root.SetActive(false);
    }
}
