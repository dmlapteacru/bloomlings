using System;
using Bloomlings.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The gameplay layout (FR-068, T048): a top bar with Pause, "Level N" and the 2× speed toggle; the board in the
    /// center; the Garden Entry and the slots below the board; the tray at the bottom. There is no goals panel.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        private TextMeshProUGUI _level = null!;
        private TextMeshProUGUI _speedLabel = null!;
        private TextMeshProUGUI _toast = null!;
        private float _toastUntil;

        public RectTransform BoardArea { get; private set; } = null!;

        public RectTransform SlotArea { get; private set; } = null!;

        public RectTransform TrayArea { get; private set; } = null!;

        public bool DoubleSpeed { get; private set; }

        public static GameplayHud Create(RectTransform root, Action onPause, Action<bool> onSpeedChanged)
        {
            var hud = root.gameObject.AddComponent<GameplayHud>();
            Image background = UiFactory.CreateImage("Background", root, null, UiTheme.Background);
            UiFactory.Stretch(background.rectTransform);

            RectTransform top = UiFactory.Place(UiFactory.CreateRect("TopBar", root), 0.03f, 0.925f, 0.97f, 0.99f);
            Button pause = UiFactory.CreateButton("Pause", top, "II", UiTheme.Text, onPause);
            UiFactory.Place((RectTransform)pause.transform, 0f, 0.05f, 0.14f, 0.95f);
            hud._level = UiFactory.CreateText("Level", top, "Level 1", 64f, UiTheme.Text);
            UiFactory.Place(hud._level.rectTransform, 0.2f, 0f, 0.8f, 1f);
            Button speed = UiFactory.CreateButton("Speed", top, "1×", UiTheme.Accent, () =>
            {
                hud.DoubleSpeed = !hud.DoubleSpeed;
                hud._speedLabel.text = hud.DoubleSpeed ? "2×" : "1×";
                onSpeedChanged(hud.DoubleSpeed);
            });
            UiFactory.Place((RectTransform)speed.transform, 0.84f, 0.05f, 1f, 0.95f);
            hud._speedLabel = speed.GetComponentInChildren<TextMeshProUGUI>();

            hud.BoardArea = UiFactory.Place(UiFactory.CreateRect("BoardArea", root), 0.03f, 0.40f, 0.97f, 0.915f);
            hud.SlotArea = UiFactory.Place(UiFactory.CreateRect("SlotArea", root), 0.05f, 0.30f, 0.95f, 0.39f);
            hud.TrayArea = UiFactory.Place(UiFactory.CreateRect("TrayArea", root), 0.03f, 0.02f, 0.97f, 0.29f);

            hud._toast = UiFactory.CreateText("Toast", root, string.Empty, 44f, UiTheme.Warning);
            UiFactory.Place(hud._toast.rectTransform, 0.1f, 0.39f, 0.9f, 0.42f);
            return hud;
        }

        /// <summary>Sets the 2× toggle without raising its callback (the saved default, FR-069).</summary>
        public void SetDoubleSpeed(bool on)
        {
            DoubleSpeed = on;
            _speedLabel.text = on ? "2×" : "1×";
        }

        public void SetLevel(int levelNumber) => _level.text = "Level " + levelNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>A short message for a refused tap, shown at once (SC-008).</summary>
        public void Toast(string message)
        {
            _toast.text = message;
            _toastUntil = Time.unscaledTime + 1.2f;
        }

        private void Update()
        {
            if (_toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
            {
                _toast.text = string.Empty;
            }
        }
    }
}
