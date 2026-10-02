using System;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The pause card of frame 11 and the Settings card (spec 002 FR-018, FR-007) in the reference look of spec 005
    /// (contracts/look.md §3.5, §4.3): parchment cards with a brown title and the cream round close, the green Resume in
    /// its wooden rim, cream secondaries with brown glyphs, and cream rows with the garden toggles.
    /// </summary>
    public static class MenuCards
    {
        /// <summary>
        /// The pause card: Resume (primary), Restart, Settings and Home (secondary, each with its glyph), and the close
        /// button. Leaving or restarting costs nothing (spec 001 FR-030).
        /// </summary>
        public static void Pause(IPainter p, DesignApp app, float since)
        {
            float buttons = DesignTokens.Size.CardPrimaryHeight + (3f * DesignTokens.Size.SecondaryHeight) + (4f * 28f) + 30f;
            CardRegions r = Kit.Card(p, buttons, PlaytestText.T("pause.title"), app.CloseOverlay, Kit.Pop(since), T.Title);
            float gap = p.U(28f);
            float y = r.Body.Top + p.U(24f);
            Box Take(bool primary)
            {
                Box b = ScreenLayout.CardButton(r.Body, y, primary, p.Scale);
                y = b.Bottom + gap;
                return b;
            }

            // Resume, then the card's narrower cream buttons (spec 003 FR-011, FR-011a).
            Kit.PrimaryButton(p, Take(true), PlaytestText.T("pause.resume"), app.CloseOverlay, decorate: true);
            Kit.SecondaryButton(p, Take(false), PlaytestText.T("common.restart"), () =>
            {
                app.CloseOverlay();
                app.Level?.Restart();
            }, "ui.restart");
            Kit.SecondaryButton(p, Take(false), PlaytestText.T("pause.settings"), () => app.OpenOverlay(Overlay.Settings), "ui.settings");
            Kit.SecondaryButton(p, Take(false), PlaytestText.T("pause.home"), app.GoHome, GardenLook.BackGlyph.ShapeId);
            Kit.EndCard(p);
        }

        /// <summary>Settings: sound and haptics, the 2× speed (spec 001 FR-073), each a cream row with a garden toggle.</summary>
        public static void Settings(IPainter p, DesignApp app, float since)
        {
            float rows = (3f * 126f) + (2f * 20f) + 30f;
            CardRegions r = Kit.Card(p, rows, PlaytestText.T("settings.title"), app.CloseOverlay, Kit.Pop(since), T.Title);
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
            Box[] lines = ScreenLayout.Column(new Box(r.Body.Left, r.Body.Top + p.U(12f), r.Body.Right, r.Body.Bottom), items.Length, p.U(126f), p.U(20f));
            for (int i = 0; i < items.Length; i++)
            {
                (string key, bool on, Action toggle) = items[i];
                Box line = lines[i];
                Kit.Row(p, line, false);
                string value = key == "settings.speed" ? (on ? "2×" : "1×") : PlaytestText.T(on ? "common.on" : "common.off");
                p.TextLeft(PlaytestText.F(key, value), line.Left + p.U(36f), line.CenterY, T.ButtonSecondary, C.InkBrown, line.Width * 0.6f, 0.86f, TextLook.Plain(C.InkBrown));
                Kit.Toggle(p, Box.FromCenter(line.Right - p.U(104f), line.CenterY, p.U(136f), p.U(70f)), on, toggle);
            }

            Kit.EndCard(p);
        }
    }
}
