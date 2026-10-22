using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Native Unity rendering of the shared account panel. The APK uses its own existing Kit renderer.</summary>
    public sealed class AccountScreen : MonoBehaviour
    {
        private AccountPanel _panel = null!;
        private RectTransform _parent = null!;
        private GameObject? _card;
        private string _signature = "";
        public static AccountScreen Create(RectTransform parent, AccountPanel panel)
        {
            var host = new GameObject("AccountPanel", typeof(RectTransform)); host.transform.SetParent(parent, false);
            var screen = host.AddComponent<AccountScreen>(); screen._parent = (RectTransform)host.transform; screen._panel = panel;
            UiFactory.Place(screen._parent, 0, 0, 1, 1);
            host.SetActive(false); return screen;
        }
        public void Show() { _panel.Open(); transform.SetAsLastSibling(); gameObject.SetActive(true); Draw(); }
        public void Hide() { _panel.Close(); gameObject.SetActive(false); }
        private void Update()
        {
            var s = _panel.Snapshot;
            string signature = s.ToString() + _panel.Busy + _panel.MessageKey + _panel.Preview?.Token;
            if (_signature != signature) { _signature = signature; Draw(); }
        }
        private void Draw()
        {
            if (_card != null) { _card.SetActive(false); Destroy(_card); }
            AccountPanelSnapshot s = _panel.Snapshot;
            AccountCloudPreview? preview = _panel.Preview;
            CardView card = UiKit.Card("Account", _parent, Loc.T(preview == null ? "account.panel.title" : "account.panel.restore_title"), 860, Hide);
            _card = card.Root;
            float y = 16, u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            Box body = card.Regions.Body;
            void Text(string text, float height, bool heading = false)
            {
                TextMeshProUGUI label = UiKit.Label("Text", card.Body, text, heading ? PopupDesign.Label : T.Body, UiTheme.Of(C.InkBrown), look:TextLook.Plain(C.InkBrown));
                label.textWrappingMode = TextWrappingModes.Normal;
                UiKit.PlaceBox(label.rectTransform, new Box(body.Left, body.Top+y*u, body.Right, body.Top+(y+height)*u), body); y += height + 14;
            }
            void Summary(string key, string name, int level, int petals) => Text(Loc.T(key) + "\n" + (string.IsNullOrWhiteSpace(name) ? Loc.T("account.panel.player") : name) + "\n" + Loc.F("account.panel.progress", level, NumberText.Group(petals)), 158);
            void Button(string key, Action? click, bool primary)
            {
                Box box = ScreenLayout.CardButton(body, body.Top + y*u, primary, u);
                var b = primary ? UiKit.PrimaryButton("Action", card.Body, Loc.T(key), click ?? (() => { })) : UiKit.SecondaryButton("Action", card.Body, Loc.T(key), click ?? (() => { }));
                b.interactable = click != null;
                UiKit.PlaceBox((RectTransform)b.transform, box, body); y += (primary ? DesignTokens.Size.CardPrimaryHeight : DesignTokens.Size.CardSecondaryHeight) + 18;
            }
            if (preview != null)
            {
                Text(Loc.T("account.panel.restore_detail"), 144);
                Summary("account.panel.on_device", s.Name, s.Level, s.Petals);
                Summary("account.panel.in_cloud", preview.Name, preview.Level, preview.Petals);
                Button("account.panel.restore", _panel.Can(AccountPanelAction.RestoreCloud) ? () => { _panel.Run(AccountPanelAction.RestoreCloud); Draw(); } : (Action?)null, true);
                Button("account.panel.keep_device", () => { _panel.CancelPreview(); Draw(); }, false);
            }
            else
            {
                Summary(s.IdentityKey, s.Name, s.Level, s.Petals);
                Text(Loc.T(_panel.StatusKey), 96, true);
                Text(Loc.T(_panel.MessageKey ?? _panel.DetailKey), 192);
                AccountPanelAction action = s.CanReviewCloud ? AccountPanelAction.ReviewCloud : s.CanSync ? AccountPanelAction.Sync : s.CanApple ? AccountPanelAction.Apple : AccountPanelAction.Google;
                string key = action switch { AccountPanelAction.ReviewCloud => "account.panel.review", AccountPanelAction.Sync => "account.panel.sync", AccountPanelAction.Apple => "settings.link_apple", _ => "settings.link_google" };
                Button(_panel.Busy ? "account.panel.working" : _panel.Can(action) ? key : "account.panel.coming_soon", _panel.Can(action) ? () => { _panel.Run(action); Draw(); } : (Action?)null, true);
                if (s.CanSync && (s.CanGoogle || s.CanApple) && !s.CanReviewCloud)
                {
                    AccountPanelAction link = s.CanApple ? AccountPanelAction.Apple : AccountPanelAction.Google;
                    Button(s.CanApple ? "settings.link_apple" : "settings.link_google", _panel.Can(link) ? () => { _panel.Run(link); Draw(); } : (Action?)null, false);
                }
                else Button("common.close", Hide, false);
            }
        }
    }
}
