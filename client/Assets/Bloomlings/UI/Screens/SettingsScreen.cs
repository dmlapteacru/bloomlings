using System;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
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
        private readonly System.Collections.Generic.List<(ToggleView View, Func<bool> On)> _toggles = new System.Collections.Generic.List<(ToggleView, Func<bool>)>();
        private TextMeshProUGUI _music = null!;
        private TextMeshProUGUI _sfx = null!;
        private TextMeshProUGUI _haptics = null!;
        private TextMeshProUGUI _speed = null!;
        private TextMeshProUGUI? _account;
        private Func<string>? _accountStatus;
        private TextMeshProUGUI _restoreLabel = null!;
        private Button _privacy = null!;
        private IConsentService? _consent;

        public bool IsOpen => _root.activeSelf;

        /// <param name="onRestorePurchases">Restores purchases and reports whether the store answered.</param>
        /// <param name="consent">Shows the privacy options entry when the consent rules require one (FR-090).</param>
        public static SettingsScreen Create(
            Transform parent,
            SettingsData settings,
            Action persist,
            Action<Action<bool>>? onRestorePurchases = null,
            AccountActions? account = null,
            IConsentService? consent = null)
        {
            // A garden card with its header band; the rows keep their places on the card (spec 003 FR-015, FR-016).
            SettingsScreen screen = null!;
            CardView view = UiKit.Card("SettingsScreen", parent, Loc.T("settings.title"), 1240f, () => screen.Hide());
            RectTransform card = view.CardRect;
            GameObject root = view.Root;
            screen = root.AddComponent<SettingsScreen>();
            screen._root = root;
            screen._settings = settings;
            screen._persist = persist;
            screen._consent = consent;

            screen._music = Toggle(card, "Music", 0.77f, () => settings.Music, () => settings.Music = !settings.Music, screen);
            screen._sfx = Toggle(card, "Sound", 0.66f, () => settings.Sfx, () => settings.Sfx = !settings.Sfx, screen);
            screen._haptics = Toggle(card, "Haptics", 0.55f, () => settings.Haptics, () => settings.Haptics = !settings.Haptics, screen);
            screen._speed = Toggle(card, "Speed", 0.44f, () => settings.Speed2x, () => settings.Speed2x = !settings.Speed2x, screen);

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

            Button restore = null!;
            restore = UiFactory.CreateButton("RestorePurchases", card, Loc.T("settings.restore"), UiTheme.SlotLocked, () =>
            {
                restore.interactable = false;
                screen._restoreLabel.text = Loc.T("settings.restoring");
                onRestorePurchases?.Invoke(ok =>
                {
                    restore.interactable = true;
                    screen._restoreLabel.text = Loc.T(ok ? "settings.restore_done" : "settings.restore_failed");
                });
            }, 36f);
            UiFactory.Place((RectTransform)restore.transform, 0.08f, 0.15f, 0.48f, 0.24f);
            restore.interactable = onRestorePurchases != null;
            screen._restoreLabel = restore.GetComponentInChildren<TextMeshProUGUI>();

            screen._privacy = UiFactory.CreateButton("PrivacyOptions", card, Loc.T("settings.privacy"), UiTheme.SlotLocked, () =>
                screen._consent?.ShowPrivacyOptions(screen.Refresh), 36f);
            UiFactory.Place((RectTransform)screen._privacy.transform, 0.52f, 0.15f, 0.92f, 0.24f);

            root.SetActive(false);
            return screen;
        }

        public void Show()
        {
            _restoreLabel.text = Loc.T("settings.restore");
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
            foreach ((ToggleView view, Func<bool> on) in _toggles)
            {
                view.Show(on());
            }

            _privacy.gameObject.SetActive(_consent != null && _consent.PrivacyOptionsRequired);
            if (_account != null && _accountStatus != null)
            {
                _account.text = _accountStatus();
            }
        }

        /// <summary>A settings row: an outlined panel with its label and a garden switch (spec 003 FR-016).</summary>
        private static TextMeshProUGUI Toggle(RectTransform card, string name, float y, Func<bool> on, Action flip, SettingsScreen screen)
        {
            Image row = UiKit.Row(name, card, highlighted: false);
            UiFactory.Place(row.rectTransform, 0.08f, y, 0.92f, y + 0.095f);
            TextMeshProUGUI label = UiKit.Label("Label", row.transform, name, DesignTokens.Type.Body, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain), TextAlignmentOptions.Left);
            UiFactory.Place(label.rectTransform, 0.06f, 0.1f, 0.68f, 0.9f);
            ToggleView toggle = UiKit.Toggle("Switch", row.transform, () =>
            {
                flip();
                screen._persist();
                screen.Refresh();
            });
            UiFactory.Place((RectTransform)toggle.transform, 0.72f, 0.24f, 0.94f, 0.76f);
            screen._toggles.Add((toggle, on));
            return label;
        }

        private static string OnOff(bool value) => value ? Loc.T("common.on") : Loc.T("common.off");
    }
}
