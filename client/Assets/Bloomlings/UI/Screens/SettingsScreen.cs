using System;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>The optional account section of Settings (FR-087, T138).</summary>
    /// <param name="Status">"Local profile", "Signed in" or the linked identity.</param>
    /// <param name="Link">Links a platform identity; <paramref name="Link"/>'s callback reports success.</param>
    public sealed record AccountActions(Func<string> Status, bool CanLinkApple, bool CanLinkGooglePlayGames, Action<LinkProvider, Action<bool>> Link);

    /// <summary>
    /// Settings (T066): music, sound effects, haptics and the 2× default, stored in the save and saved on every change
    /// (R15). Restore Purchases is wired with the store in US6. The account section offers optional Sign in with Apple
    /// or Google Play Games linking for cloud sync (FR-087); the game never requires it.
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
        private TextMeshProUGUI? _account;
        private Func<string>? _accountStatus;

        public bool IsOpen => _root.activeSelf;

        public static SettingsScreen Create(Transform parent, SettingsData settings, Action persist, Action? onRestorePurchases = null, AccountActions? account = null)
        {
            RectTransform card = UiFactory.CreateModal("SettingsScreen", parent, 0.62f, out GameObject root);
            var screen = root.AddComponent<SettingsScreen>();
            screen._root = root;
            screen._settings = settings;
            screen._persist = persist;

            TextMeshProUGUI title = UiFactory.CreateText("Title", card, Loc.T("settings.title"), 72f, UiTheme.Text);
            UiFactory.Place(title.rectTransform, 0f, 0.88f, 1f, 0.97f);
            screen._music = Toggle(card, "Music", 0.77f, () => settings.Music = !settings.Music, screen);
            screen._sfx = Toggle(card, "Sound", 0.66f, () => settings.Sfx = !settings.Sfx, screen);
            screen._haptics = Toggle(card, "Haptics", 0.55f, () => settings.Haptics = !settings.Haptics, screen);
            screen._speed = Toggle(card, "Speed", 0.44f, () => settings.Speed2x = !settings.Speed2x, screen);

            if (account != null)
            {
                screen._accountStatus = account.Status;
                screen._account = UiFactory.CreateText("Account", card, string.Empty, 36f, UiTheme.Text);
                UiFactory.Place(screen._account.rectTransform, 0.05f, 0.365f, 0.95f, 0.425f);
                if (account.CanLinkApple)
                {
                    Button apple = UiFactory.CreateButton("LinkApple", card, Loc.T("settings.link_apple"), UiTheme.Text, () => account.Link(LinkProvider.Apple, _ => screen.Refresh()), 36f);
                    UiFactory.Place((RectTransform)apple.transform, 0.08f, 0.265f, account.CanLinkGooglePlayGames ? 0.48f : 0.92f, 0.355f);
                }

                if (account.CanLinkGooglePlayGames)
                {
                    Button google = UiFactory.CreateButton("LinkGoogle", card, Loc.T("settings.link_google"), UiTheme.Text, () => account.Link(LinkProvider.GooglePlayGames, _ => screen.Refresh()), 36f);
                    UiFactory.Place((RectTransform)google.transform, account.CanLinkApple ? 0.52f : 0.08f, 0.265f, 0.92f, 0.355f);
                }
            }

            Button restore = UiFactory.CreateButton("RestorePurchases", card, Loc.T("settings.restore"), UiTheme.SlotLocked, () => onRestorePurchases?.Invoke(), 40f);
            UiFactory.Place((RectTransform)restore.transform, 0.15f, 0.15f, 0.85f, 0.24f);
            restore.interactable = onRestorePurchases != null;

            Button close = UiFactory.CreateButton("Close", card, Loc.T("common.close"), UiTheme.Accent, screen.Hide);
            UiFactory.Place((RectTransform)close.transform, 0.25f, 0.03f, 0.75f, 0.12f);
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
            _music.text = Loc.F("settings.music", OnOff(_settings.Music));
            _sfx.text = Loc.F("settings.sound", OnOff(_settings.Sfx));
            _haptics.text = Loc.F("settings.haptics", OnOff(_settings.Haptics));
            _speed.text = Loc.F("settings.speed", _settings.Speed2x ? "2×" : "1×");
            if (_account != null && _accountStatus != null)
            {
                _account.text = _accountStatus();
            }
        }

        private static TextMeshProUGUI Toggle(RectTransform card, string name, float y, Action flip, SettingsScreen screen)
        {
            Button button = UiFactory.CreateButton(name, card, name, UiTheme.Text, () =>
            {
                flip();
                screen._persist();
                screen.Refresh();
            }, 48f);
            UiFactory.Place((RectTransform)button.transform, 0.15f, y, 0.85f, y + 0.095f);
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static string OnOff(bool value) => value ? Loc.T("common.on") : Loc.T("common.off");
    }
}
