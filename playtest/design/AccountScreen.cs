using System;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>Account and cloud progress, in the same wood/cream/green kit as Settings and purchase cards.</summary>
    public static class AccountScreen
    {
        public const float Content = 860;
        public static void Draw(IPainter p, DesignApp app, float since)
        {
            AccountPanel panel = app.Account;
            AccountPanelSnapshot s = panel.Snapshot;
            AccountCloudPreview? preview = panel.Preview;
            p.Mark(preview == null ? "ui.account" : "ui.account.restore");
            CardRegions r = Kit.Card(p, Content, PlaytestText.T(preview == null ? "account.panel.title" : "account.panel.restore_title"), app.CardClose, Kit.Pop(since));
            float y = r.Body.Top + p.U(16);
            if (preview != null)
            {
                Paragraph(p, r.Body, ref y, "account.panel.restore_detail", 3);
                Summary(p, new Box(r.Body.Left, y, r.Body.Right, y + p.U(158)), "account.panel.on_device", s.Name, s.Level, s.Petals);
                y += p.U(178);
                Summary(p, new Box(r.Body.Left, y, r.Body.Right, y + p.U(158)), "account.panel.in_cloud", preview.Name, preview.Level, preview.Petals);
                y += p.U(184);
                Kit.PrimaryButton(p, ScreenLayout.CardButton(r.Body, y, true, p.Scale), PlaytestText.T("account.panel.restore"),
                    panel.Can(AccountPanelAction.RestoreCloud) ? () => panel.Run(AccountPanelAction.RestoreCloud) : null);
                y += p.U(DesignTokens.Size.CardPrimaryHeight + 18);
                Kit.SecondaryButton(p, ScreenLayout.CardButton(r.Body, y, false, p.Scale), PlaytestText.T("account.panel.keep_device"), panel.CancelPreview);
            }
            else
            {
                Summary(p, new Box(r.Body.Left, y, r.Body.Right, y + p.U(158)), s.IdentityKey, s.Name, s.Level, s.Petals);
                y += p.U(184);
                Paragraph(p, r.Body, ref y, panel.StatusKey, 2, true);
                Paragraph(p, r.Body, ref y, panel.MessageKey ?? (app.ServerClient != null ? app.ServerClient.PendingCount > 0 || app.ServerClient.UnsettledRewardIds.Count > 0 ? "account.panel.server_pending" : "account.panel.server_detail" : panel.DetailKey), 3);
                AccountPanelAction action = s.CanReviewCloud ? AccountPanelAction.ReviewCloud :
                    s.CanSync ? AccountPanelAction.Sync : s.CanApple ? AccountPanelAction.Apple : AccountPanelAction.Google;
                string key = action switch { AccountPanelAction.ReviewCloud => "account.panel.review", AccountPanelAction.Sync => app.ServerClient != null ? "account.panel.check_wallet" : "account.panel.sync", AccountPanelAction.Apple => "settings.link_apple", _ => "settings.link_google" };
                bool enabled = panel.Can(action);
                Kit.PrimaryButton(p, ScreenLayout.CardButton(r.Body, y, true, p.Scale), PlaytestText.T(panel.Busy ? "account.panel.working" : enabled ? key : app.ServerClient != null ? "account.panel.connect_to_check" : "account.panel.coming_soon"),
                    enabled ? () => panel.Run(action) : null, set: enabled ? GardenLook.Green : GardenLook.Green.Disabled());
                y += p.U(DesignTokens.Size.CardPrimaryHeight + 18);
                if (s.CanSync && (s.CanGoogle || s.CanApple) && !s.CanReviewCloud)
                {
                    AccountPanelAction link = s.CanApple ? AccountPanelAction.Apple : AccountPanelAction.Google;
                    Kit.SecondaryButton(p, ScreenLayout.CardButton(r.Body, y, false, p.Scale), PlaytestText.T(s.CanApple ? "settings.link_apple" : "settings.link_google"),
                        panel.Can(link) ? () => panel.Run(link) : null);
                }
                else if (app.ServerClient?.PendingPurchase != null)
                    Kit.SecondaryButton(p, ScreenLayout.CardButton(r.Body, y, false, p.Scale), PlaytestText.T("account.panel.pending_purchase"), app.OpenPendingPurchase);
                else Kit.SecondaryButton(p, ScreenLayout.CardButton(r.Body, y, false, p.Scale), PlaytestText.T("common.close"), app.CardClose);
            }
            Kit.EndCard(p);
        }

        private static void Summary(IPainter p, Box box, string labelKey, string name, int level, int petals)
        {
            Kit.RaisedRow(p, box, p.U(24));
            p.Text(PlaytestText.T(labelKey), box.CenterX, box.Top + p.U(30), T.Caption, C.InkBrownSoft, box.Width * .9f);
            p.Text(string.IsNullOrWhiteSpace(name) ? PlaytestText.T("account.panel.player") : name, box.CenterX, box.Top + p.U(77), PopupDesign.Label, C.InkBrown, box.Width * .9f, look:TextLook.Plain(C.InkBrown));
            p.Text(PlaytestText.F("account.panel.progress", level, NumberText.Group(petals)), box.CenterX, box.Top + p.U(124), T.Caption, C.InkBrownSoft, box.Width * .9f);
        }
        internal static void Paragraph(IPainter p, Box body, ref float y, string key, int count, bool heading = false)
        {
            var style = heading ? PopupDesign.Label : T.Body;
            var lines = EndCards.Lines(p, PlaytestText.T(key), style, body.Width * .94f, count);
            foreach (string line in lines) { p.Text(line, body.CenterX, y + p.U(24), style, C.InkBrown, body.Width * .94f, look:TextLook.Plain(C.InkBrown)); y += p.U(48); }
            y += p.U(14);
        }
    }
}
