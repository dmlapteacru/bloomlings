using System;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The pause card of frame 11 and the Settings card (spec 002 FR-018, FR-007) in the reference look of spec 005
    /// (contracts/look.md §3.5, §4.3, §6.19): the popups' wooden frame with the title on a wooden sign and the close, the
    /// green Resume and the cream secondaries raised on their plates, and the Settings rows raised with their icons and the
    /// garden toggles.
    /// </summary>
    public static class MenuCards
    {
        /// <summary>
        /// The pause card: Resume (primary), Restart, Settings and Home (secondary, each with its glyph), and the close
        /// button. Leaving or restarting costs nothing (spec 001 FR-030).
        /// </summary>
        public static void Pause(IPainter p, DesignApp app, float since)
        {
            CardRegions r=Kit.Card(p,PopupDesign.PauseContent,PlaytestText.T("pause.title"),app.CardClose,Kit.Pop(since));
            Kit.PrimaryButton(p,PopupDesign.PauseResume(r.Body,p.Scale),PlaytestText.T("pause.resume"),app.CloseOverlay);
            Box tray=PopupDesign.PauseTray(r.Body,p.Scale);Kit.NavigationBar(p,tray,3);
            Box[] cells=ScreenLayout.Row(tray,3,0,float.MaxValue,false);
            string[] labels={PlaytestText.T("common.restart"),PlaytestText.T("pause.settings"),PlaytestText.T("pause.home")};
            string[] icons={"ui.restart","ui.settings","ui.star"};
            Action[] actions={()=>{app.CloseOverlay();app.Level?.Restart();},()=>app.OpenOverlay(Overlay.Settings),app.GoHome};
            for(int i=0;i<cells.Length;i++)
            {
                Box cell=cells[i];Box icon=PopupDesign.CellIcon(cell);
                if(i==2) Kit.OwnerPicture(p,PainterBase.IconPrefix+"nav-home",icon);else Kit.RaisedGlyph(p,icons[i],icon,C.InkBrown);
                Box label=PopupDesign.CellLabel(cell);p.Text(labels[i],label.CenterX,label.CenterY,PopupDesign.Label,C.InkBrown,label.Width,look:TextLook.Plain(C.InkBrown));p.Hit(cell,actions[i]);
            }
            Kit.EndCard(p);
        }

        /// <summary>
        /// Settings: sound and haptics, fast forward (spec 001 FR-069, shown On or Off with no number), each a cream row with a garden toggle; the
        /// playtest's dev row under them wherever the card opens (not the product; it lay at Home's bottom before the bottom
        /// menu, spec 005 FR-030; the owner, 2026-10-04: "put the dev level up in Settings").
        /// </summary>
        public static void Settings(IPainter p, DesignApp app, float since)
        {
            float rows = PopupDesign.SettingsContent;
            CardRegions r = Kit.Card(p, rows, PlaytestText.T("settings.title"), app.CardClose, Kit.Pop(since), T.Title);
            var save = app.Meta.Save.Settings;
            (string Key, bool On, Action Toggle)[] items =
            {
                ("settings.sound", app.Sound.Enabled, () =>
                {
                    app.Sound.Enabled = !app.Sound.Enabled;
                    save.Sfx = app.Sound.Enabled;
                    app.Meta.Persist();
                }),
                ("settings.haptics", save.Haptics, () =>
                {
                    save.Haptics = !save.Haptics;
                    app.Meta.Persist();
                }),
            };
            Box[] lines = ScreenLayout.Column(new Box(r.Body.Left, r.Body.Top + p.U(12f), r.Body.Right, r.Body.Bottom), items.Length, p.U(126f), p.U(12f));
            for (int i = 0; i < items.Length; i++)
            {
                (string key, bool on, Action toggle) = items[i];
                Box line = lines[i];
                if(i>0)p.Line(line.Left+line.Width*.04f,line.Top-p.U(6),line.Right-line.Width*.04f,line.Top-p.U(6),p.U(2),C.ParchmentEdge.WithAlpha(.35f));
                DrawSettingsIcon(p, CardLook.SettingsIconOf(key), CardLook.SettingsIconBox(line, p.U(1f)));
                string value = PlaytestText.T(on ? "common.on" : "common.off");
                float left = CardLook.SettingsLabelLeft(line, p.U(1f));
                p.TextLeft(PlaytestText.T(key + "_label"), left, line.CenterY, PopupDesign.Label, C.InkBrown, CardLook.SettingsLabelRight(line, p.U(1f)) - left, CardLook.SettingsLabelScale, TextLook.Plain(C.InkBrown));
                Kit.Toggle(p, CardLook.SettingsToggleBox(line, p.U(1f)), on, toggle);
            }

            float top = lines[lines.Length - 1].Bottom + p.U(20f);
            DevRow(p, app, new Box(r.Body.Left, top, r.Body.Right, top + p.U(DesignTokens.Size.TouchMin)));
            float petalTop=top+p.U(DesignTokens.Size.TouchMin+12);
            PetalRow(p,app,new Box(r.Body.Left,petalTop,r.Body.Right,petalTop+p.U(DesignTokens.Size.TouchMin)));
            Box policy=ScreenLayout.CardButton(r.Body,petalTop+p.U(DesignTokens.Size.TouchMin+24),false,p.Scale);
            Kit.SecondaryButton(p,policy,PlaytestText.T("settings.privacy_policy"),()=>app.OpenOverlay(Overlay.PrivacyPolicy),style:PopupDesign.Label);
            Kit.EndCard(p);
        }

        /// <summary>A Settings row's icon (the owner's mockup of 2026-10-08): the owner's picture, its drawn stand-in, or the raised glyph.</summary>
        private static void DrawSettingsIcon(IPainter p, SettingsIcon icon, Box box)
        {
            p.Mark("ui.settings.icon");
            if(icon.Picture!=null && PopupDesign.SettingsIcon(icon.Picture)){p.Sprite(icon.Picture,box);return;}
            if (icon.Glyph != null)
            {
                Kit.RaisedGlyph(p, icon.Glyph, box.Inset(box.Width * 0.06f), C.MedalGold);
            }
            else if (icon.Picture == null || !Kit.OwnerPicture(p, PainterBase.IconPrefix + icon.Picture, box))
            {
                p.Shape(icon.StandIn, box, C.InkBrown);
            }
        }

        /// <summary>
        /// The playtest's own controls (not the product), in the Settings card: step back, skip one or ten levels, start a
        /// new profile, after "dev L<i>n</i>" (the current level, so a skip shows at once). Used from a level's pause, a
        /// control also returns Home, where the new level waits. Each pill is half the row tall; the whole cell takes the tap.
        /// </summary>
        public static void PrivacyPolicy(IPainter p,DesignApp app,float since)
        {
            CardRegions r=Kit.Card(p,180,PlaytestText.T("settings.privacy_policy"),app.CardClose,Kit.Pop(since),sign:SignDecor.None);
            var lines=EndCards.Lines(p,PlaytestText.T("settings.privacy_mock"),T.Body,r.Body.Width,2);
            float line=p.U(60),y=r.Body.CenterY-(lines.Count-1)*line/2;
            foreach(string text in lines){p.Text(text,r.Body.CenterX,y,T.Body,C.InkBrown,r.Body.Width);y+=line;}Kit.EndCard(p);
        }
        private static void PetalRow(IPainter p, DesignApp app, Box row)
        {
            string[] labels={"−100","+100","+1000","Reset"};
            int[] deltas={-100,100,1000,0};
            string caption=PlaytestText.T("settings.petals_label")+" "+NumberText.Group(app.Meta.Economy.Petals);
            float label=row.Width*.30f;
            p.TextLeft(caption,row.Left,row.CenterY,T.Caption,C.InkBrownSoft,label);
            Box[] cells=ScreenLayout.Row(new Box(row.Left+label,row.Top,row.Right,row.Bottom),4,row.Height*.1f,float.MaxValue,square:false);
            for(int i=0;i<cells.Length;i++)
            {
                Box pill=Box.FromCenter(cells[i].CenterX,cells[i].CenterY,cells[i].Width,cells[i].Height*.78f);
                Kit.RaisedRow(p,pill,pill.Height/2f);
                p.Text(labels[i],pill.CenterX,pill.CenterY-pill.Height*.03f,T.ButtonSecondary,C.InkBrown,pill.Width*.86f,.8f,TextLook.Plain(C.InkBrown));
                int delta=deltas[i];
                p.Hit(cells[i],()=>app.Meta.Economy.AdjustPetalsForTesting(delta==0?-app.Meta.Economy.Petals:delta));
            }
        }
        private static void DevRow(IPainter p, DesignApp app, Box row)
        {
            string[] labels = { "−1", "+1", "+10", "Reset" };
            Action[] actions =
            {
                () => app.Meta.StepBack(),
                () => app.Meta.SkipTo(app.Meta.Progression.HighestCompletedLevel + 1),
                () => app.Meta.SkipTo(app.Meta.Progression.HighestCompletedLevel + 10),
                app.ResetProfile,
            };
            string caption = "Level " + app.Meta.CurrentLevel.ToString(System.Globalization.CultureInfo.InvariantCulture);
            float label = p.MeasureText(caption, T.Caption) + (row.Height * 0.2f);
            p.TextLeft(caption, row.Left, row.CenterY, T.Caption, C.InkBrownSoft);
            Box[] cells = ScreenLayout.Row(new Box(row.Left + label, row.Top, row.Right, row.Bottom), labels.Length, row.Height * 0.1f, float.MaxValue, square: false);
            for (int i = 0; i < cells.Length; i++)
            {
                // Raised cream pills, as the popups' rows (spec 005 FR-045).
                Box pill = Box.FromCenter(cells[i].CenterX, cells[i].CenterY, cells[i].Width, cells[i].Height * 0.78f);
                Kit.RaisedRow(p, pill, pill.Height / 2f);
                p.Text(labels[i], pill.CenterX, pill.CenterY - (pill.Height * 0.03f), T.ButtonSecondary, C.InkBrown, pill.Width * 0.86f, 0.8f, TextLook.Plain(C.InkBrown));
                Action action = actions[i];
                p.Hit(cells[i], () =>
                {
                    action();
                    if (app.Screen == Screen.Level)
                    {
                        app.GoHome();
                    }
                });
            }
        }
    }
}
