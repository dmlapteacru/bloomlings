using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>The optional account section of Settings (FR-087, T138).</summary>
    /// <param name="Status">"Local profile", "Signed in" or the linked identity.</param>
    /// <param name="Link">Links a platform identity; <paramref name="Link"/>'s callback reports success.</param>
    public sealed record AccountActions(Func<string> Status, bool CanLinkApple, bool CanLinkGooglePlayGames, Action<LinkProvider, Action<bool>> Link);

    /// <summary>
    /// Settings (T066): music, sound effects, haptics and the 2× default, stored in the save and saved on every change
    /// (R15). Restore Purchases is wired with the store in US6. The account section offers optional Sign in with Apple
    /// or Google Play Games linking for cloud sync (FR-087); the game never requires it. In the reference look of spec 005
    /// (contracts/look.md §4.3; the playtest's <c>MenuCards.Settings</c>): a parchment card with the brown title and the
    /// cream round close, cream rows with brown labels and the garden switches, and cream buttons below them.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private const float RowUnits = 126f;
        private const float RowGapUnits = 20f;
        private const float StatusUnits = 56f;
        private const float ButtonGapUnits = 24f;

        private GameObject _root = null!;
        private SettingsData _settings = null!;
        private Action _persist = () => { };
        private readonly List<(ToggleView View, Func<bool> On)> _toggles = new List<(ToggleView, Func<bool>)>();
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
            bool links = account != null && (account.CanLinkApple || account.CanLinkGooglePlayGames);
            float content = 12f + (4f * RowUnits) + (3f * RowGapUnits)
                + (account != null ? ButtonGapUnits + StatusUnits : 0f)
                + (links ? 16f + DesignTokens.Size.SecondaryHeight : 0f)
                + ButtonGapUnits + DesignTokens.Size.SecondaryHeight + 30f;

            SettingsScreen screen = null!;
            CardView view = UiKit.Card("SettingsScreen", parent, Loc.T("settings.title"), content, () => screen.Hide(), T.Title);
            GameObject root = view.Root;
            screen = root.AddComponent<SettingsScreen>();
            screen._root = root;
            screen._settings = settings;
            screen._persist = persist;
            screen._consent = consent;

            Box body = view.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            Box[] lines = ScreenLayout.Column(new Box(body.Left, body.Top + (12f * u), body.Right, body.Bottom), 4, RowUnits * u, RowGapUnits * u);
            screen._music = Toggle(view.Body, body, lines[0], "Music", "settings.music", () => settings.Music, () => settings.Music = !settings.Music, screen, u);
            screen._sfx = Toggle(view.Body, body, lines[1], "Sound", "settings.sound", () => settings.Sfx, () => settings.Sfx = !settings.Sfx, screen, u);
            screen._haptics = Toggle(view.Body, body, lines[2], "Haptics", "settings.haptics", () => settings.Haptics, () => settings.Haptics = !settings.Haptics, screen, u);
            screen._speed = Toggle(view.Body, body, lines[3], "Speed", "settings.speed", () => settings.Speed2x, () => settings.Speed2x = !settings.Speed2x, screen, u);
            float y = lines[3].Bottom;

            if (account != null)
            {
                y += ButtonGapUnits * u;
                screen._accountStatus = account.Status;
                screen._account = UiKit.Label("Account", view.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft));
                UiKit.PlaceBox(screen._account.rectTransform, new Box(body.Left, y, body.Right, y + (StatusUnits * u)), body);
                y += StatusUnits * u;
                if (links)
                {
                    y += 16f * u;
                    var row = new Box(body.Left, y, body.Right, y + (DesignTokens.Size.SecondaryHeight * u));
                    Box[] cells = Pair(row, account.CanLinkApple && account.CanLinkGooglePlayGames ? 2 : 1, u);
                    int cell = 0;
                    if (account.CanLinkApple)
                    {
                        Button apple = UiKit.SecondaryButton("LinkApple", view.Body, Loc.T("settings.link_apple"), () => account.Link(LinkProvider.Apple, _ => screen.Refresh()));
                        UiKit.PlaceBox((RectTransform)apple.transform, cells[cell++], body);
                    }

                    if (account.CanLinkGooglePlayGames)
                    {
                        Button google = UiKit.SecondaryButton("LinkGoogle", view.Body, Loc.T("settings.link_google"), () => account.Link(LinkProvider.GooglePlayGames, _ => screen.Refresh()));
                        UiKit.PlaceBox((RectTransform)google.transform, cells[cell], body);
                    }

                    y = row.Bottom;
                }
            }

            // Restore Purchases and the privacy options side by side, as cream buttons.
            y += ButtonGapUnits * u;
            Box[] buttons = Pair(new Box(body.Left, y, body.Right, y + (DesignTokens.Size.SecondaryHeight * u)), 2, u);
            Button restore = null!;
            restore = UiKit.SecondaryButton("RestorePurchases", view.Body, Loc.T("settings.restore"), () =>
            {
                restore.interactable = false;
                screen._restoreLabel.text = Loc.T("settings.restoring");
                onRestorePurchases?.Invoke(ok =>
                {
                    restore.interactable = true;
                    screen._restoreLabel.text = ok ? Loc.T("settings.restore_done") : Loc.T("settings.restore_failed");
                });
            });
            UiKit.PlaceBox((RectTransform)restore.transform, buttons[0], body);
            restore.interactable = onRestorePurchases != null;
            screen._restoreLabel = restore.GetComponentInChildren<TextMeshProUGUI>();

            screen._privacy = UiKit.SecondaryButton("PrivacyOptions", view.Body, Loc.T("settings.privacy"), () =>
                screen._consent?.ShowPrivacyOptions(screen.Refresh));
            UiKit.PlaceBox((RectTransform)screen._privacy.transform, buttons[1], body);

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
            _speed.text = Loc.F("settings.speed", Loc.T(_settings.Speed2x ? "common.on" : "common.off"));
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

        /// <summary>
        /// A settings row (the playtest's <c>MenuCards.Settings</c>; spec 005 FR-045, the owner's mockup of 2026-10-08): a
        /// raised cream row, its icon (<see cref="CardLook.SettingsIconOf"/>), its brown label after it
        /// (<see cref="CardLook.SettingsLabelScale"/> of <c>type.button_secondary</c>), and the garden switch (136 × 70
        /// units) near its right end.
        /// </summary>
        private static TextMeshProUGUI Toggle(RectTransform cardBody, Box body, Box line, string name, string key, Func<bool> on, Action flip, SettingsScreen screen, float u)
        {
            Image row = UiKit.Row(name, cardBody, highlighted: false);
            UiKit.PlaceBox(row.rectTransform, line, body);
            Icon(row.transform, CardLook.SettingsIconOf(key), CardLook.SettingsIconBox(line, u), line);
            TextMeshProUGUI label = UiKit.Label("Label", row.transform, string.Empty, T.ButtonSecondary, UiTheme.Of(C.InkBrown), TextAlignmentOptions.Left, TextLook.Plain(C.InkBrown));
            label.fontSizeMax = UiKit.Units(T.ButtonSecondary.Size * CardLook.SettingsLabelScale);
            label.fontSize = label.fontSizeMax;
            float left = CardLook.SettingsLabelLeft(line, u);
            UiKit.PlaceBox(label.rectTransform, new Box(left, line.Top, line.Right - (190f * u), line.Bottom), line);
            ToggleView toggle = UiKit.Toggle("Switch", row.transform, () =>
            {
                flip();
                screen._persist();
                screen.Refresh();
            });
            UiKit.PlaceBox((RectTransform)toggle.transform, Box.FromCenter(line.Right - (104f * u), line.CenterY, 136f * u, 70f * u), line);
            screen._toggles.Add((toggle, on));
            return label;
        }

        /// <summary>A settings row's icon: the owner's picture, the raised glyph, or the drawn stand-in (the playtest's <c>MenuCards</c>).</summary>
        private static void Icon(Transform row, SettingsIcon icon, Box box, Box line)
        {
            Sprite? picture = icon.Picture != null ? OwnerArt.Icon(icon.Picture) : null;
            Image image;
            if (icon.Glyph != null)
            {
                image = UiKit.RaisedGlyph(row, ProceduralSprites.RaisedGlyph(icon.Glyph, C.MedalGold));
                box = box.Inset(box.Width * 0.06f);
            }
            else if (picture != null)
            {
                image = UiFactory.CreateImage("Icon", row, null, Color.white);
                OwnerArt.Show(image, picture);
            }
            else
            {
                image = UiFactory.CreateImage("Icon", row, ProceduralSprites.Shape(icon.StandIn), UiTheme.Of(C.InkBrown));
                image.preserveAspect = true;
            }

            image.raycastTarget = false;
            UiKit.PlaceBox(image.rectTransform, box, line);
        }

        /// <summary>One or two cream buttons in a row: two halves with a gap, or one at the card's secondary width.</summary>
        private static Box[] Pair(Box row, int count, float u) =>
            ScreenLayout.Row(row, count, ButtonGapUnits * u, count == 1 ? DesignTokens.Size.CardSecondaryWidth * u : float.MaxValue, square: false);

        private static string OnOff(bool value) => value ? Loc.T("common.on") : Loc.T("common.off");
    }
}
