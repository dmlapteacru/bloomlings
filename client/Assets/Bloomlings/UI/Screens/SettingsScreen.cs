using System;
using Bloomlings.Client.Services.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// Settings (T066): music, sound effects, haptics and the 2× default, stored in the save and saved on every change
    /// (R15). Restore Purchases is wired with the store in US6.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private GameObject _root = null!;
        private SettingsData _settings = null!;
        private Action _persist = () => { };
        private TextMeshProUGUI _music = null!;
        private TextMeshProUGUI _sfx = null!;
        private TextMeshProUGUI _haptics = null!;
        private TextMeshProUGUI _speed = null!;

        public bool IsOpen => _root.activeSelf;

        public static SettingsScreen Create(Transform parent, SettingsData settings, Action persist, Action? onRestorePurchases = null)
        {
            RectTransform card = UiFactory.CreateModal("SettingsScreen", parent, 0.5f, out GameObject root);
            var screen = root.AddComponent<SettingsScreen>();
            screen._root = root;
            screen._settings = settings;
            screen._persist = persist;

            TextMeshProUGUI title = UiFactory.CreateText("Title", card, "Settings", 72f, UiTheme.Text);
            UiFactory.Place(title.rectTransform, 0f, 0.86f, 1f, 0.97f);
            screen._music = Toggle(card, "Music", 0.72f, () => settings.Music = !settings.Music, screen);
            screen._sfx = Toggle(card, "Sound", 0.58f, () => settings.Sfx = !settings.Sfx, screen);
            screen._haptics = Toggle(card, "Haptics", 0.44f, () => settings.Haptics = !settings.Haptics, screen);
            screen._speed = Toggle(card, "Speed", 0.30f, () => settings.Speed2x = !settings.Speed2x, screen);

            Button restore = UiFactory.CreateButton("RestorePurchases", card, "Restore Purchases", UiTheme.SlotLocked, () => onRestorePurchases?.Invoke(), 40f);
            UiFactory.Place((RectTransform)restore.transform, 0.15f, 0.15f, 0.85f, 0.25f);
            restore.interactable = onRestorePurchases != null;

            Button close = UiFactory.CreateButton("Close", card, "Close", UiTheme.Accent, screen.Hide);
            UiFactory.Place((RectTransform)close.transform, 0.25f, 0.03f, 0.75f, 0.13f);
            root.SetActive(false);
            return screen;
        }

        public void Show()
        {
            Refresh();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void Refresh()
        {
            _music.text = "Music: " + OnOff(_settings.Music);
            _sfx.text = "Sound: " + OnOff(_settings.Sfx);
            _haptics.text = "Haptics: " + OnOff(_settings.Haptics);
            _speed.text = "Speed: " + (_settings.Speed2x ? "2×" : "1×");
        }

        private static TextMeshProUGUI Toggle(RectTransform card, string name, float y, Action flip, SettingsScreen screen)
        {
            Button button = UiFactory.CreateButton(name, card, name, UiTheme.Text, () =>
            {
                flip();
                screen._persist();
                screen.Refresh();
            }, 48f);
            UiFactory.Place((RectTransform)button.transform, 0.15f, y, 0.85f, y + 0.11f);
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static string OnOff(bool value) => value ? "On" : "Off";
    }
}
