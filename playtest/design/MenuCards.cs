using System;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The pause card of frame 11 and the Settings card, in the board's card style (spec 002 FR-018, FR-007).</summary>
    public static class MenuCards
    {
        /// <summary>
        /// The pause card: RESUME (primary), RESTART, SETTINGS and HOME (secondary), and the close button. Leaving or
        /// restarting costs nothing (spec 001 FR-030).
        /// </summary>
        public static void Pause(IPainter p, DesignApp app, float since)
        {
            float buttons = DesignTokens.Size.CardPrimaryHeight + (3f * DesignTokens.Size.SecondaryHeight) + (4f * 26f) + 24f;
            CardRegions r = Kit.Card(p, buttons, PlaytestText.T("pause.title"), app.CloseOverlay, Kit.Pop(since), T.TitleCaps, GardenLook.Blue);
            float gap = p.U(26f);
            float y = r.Body.Top + p.U(28f);
            Box Take(bool primary)
            {
                Box b = ScreenLayout.CardButton(r.Body, y, primary, p.Scale);
                y = b.Bottom + gap;
                return b;
            }

            // RESUME, then the card's narrower secondary buttons (spec 003 FR-011, FR-011a).
            Kit.PrimaryButton(p, Take(true), PlaytestText.T("pause.resume"), app.CloseOverlay, decorate: true);
            Kit.SecondaryButton(p, Take(false), PlaytestText.T("common.restart"), () =>
            {
                app.CloseOverlay();
                app.Level?.Restart();
            });
            Kit.SecondaryButton(p, Take(false), PlaytestText.T("pause.settings"), () => app.OpenOverlay(Overlay.Settings));
            Kit.SecondaryButton(p, Take(false), PlaytestText.T("pause.home"), app.GoHome);
            Kit.EndCard(p);
        }

        /// <summary>Settings: sound and haptics, the 2× speed (spec 001 FR-073).</summary>
        public static void Settings(IPainter p, DesignApp app, float since)
        {
            float rows = (3f * 136f) + 16f;
            CardRegions r = Kit.Card(p, rows, PlaytestText.T("settings.title"), app.CloseOverlay, Kit.Pop(since));
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
                ("settings.speed", save.Speed2x, () =>
                {
                    save.Speed2x = !save.Speed2x;
                    app.Meta.Persist();
                    if (app.Level != null)
                    {
                        app.Level.Animator.Speed = save.Speed2x ? 2f : 1f;
                    }
                }),
            };
            Box[] lines = ScreenLayout.Column(new Box(r.Body.Left, r.Body.Top + p.U(16f), r.Body.Right, r.Body.Bottom), items.Length, p.U(118f), p.U(18f));
            for (int i = 0; i < items.Length; i++)
            {
                (string key, bool on, Action toggle) = items[i];
                Box line = lines[i];
                Kit.Row(p, line, false);
                string value = key == "settings.speed" ? (on ? "2×" : "1×") : PlaytestText.T(on ? "common.on" : "common.off");
                p.TextLeft(PlaytestText.F(key, value), line.Left + p.U(30f), line.CenterY, T.Body, C.GardenLabelPlain, line.Width * 0.6f);
                Kit.Toggle(p, Box.FromCenter(line.Right - p.U(90f), line.CenterY, p.U(120f), p.U(64f)), on, toggle);
            }

            Kit.EndCard(p);
        }
    }
}
