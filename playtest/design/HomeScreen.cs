using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The splash of frame 1 (spec 002 FR-016): the Bloomlings wordmark over the garden, with the four families on the
    /// stone. It shows while the game starts and never waits for a tap.
    /// </summary>
    public static class SplashScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            DesignApp.DrawBackdrop(p, BackdropScene.Splash, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            float appear = Kit.Ease(app.Now / 0.5f);
            p.PushAlpha(appear);
            HomeScreen.Wordmark(p, safe.CenterX, safe.Top + (safe.Height * 0.3f), Math.Min(safe.Width * 0.84f, p.U(900f)));
            p.PopAlpha();

            // The four families as 3D heroes on their stone (spec 004 FR-017), rising in with the wordmark.
            float width = Math.Min(safe.Width * 0.96f, p.U(1000f));
            float height = width * CharacterArt.GroupHeight / CharacterArt.GroupWidth;
            p.PushAlpha(appear);
            p.PushTransform(0f, (1f - appear) * p.U(40f), 1f, 0f, 0f);
            Visuals.Group(p, Box.FromCenter(safe.CenterX, safe.Top + (safe.Height * 0.64f), width, height));
            p.PopTransform();
            p.PopAlpha();
            p.Mark("brand.splash_art");
        }
    }

    /// <summary>
    /// Home of frames 2 and 3 (spec 002 FR-017, spec 001 FR-058).
    /// <list type="bullet">
    /// <item><description>Always shown: the Petals pill, Settings, "LEVEL N" and the big PLAY button.</description></item>
    /// <item><description>Shown once unlocked: the hero with the Wardrobe and Collection buttons, "N levels to reward"
    /// with a gift, "Rank #N", and the Daily Challenge card.</description></item>
    /// </list>
    /// PLAY always continues Level N, and there is no level map. A small labelled dev row at the bottom keeps the
    /// playtest's skip and reset controls; it is not part of the product.
    /// </summary>
    public static class HomeScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            int level = meta.CurrentLevel;
            (int Level, Client.App.Progression.MilestoneCadence Cadence, int WinsToGo)? next = meta.Milestones.Next(meta.Progression.HighestCompletedLevel);
            HomeLook look = HomeLook.From(meta.Progression.IsUnlocked, meta.Collection.Count, next.HasValue, dailyChallengeAvailable: true, freeBoosterOffer: false);
            float devRow = p.U(170f);
            HomeRegions r = ScreenLayout.Home(p.Width, p.Height, p.Insets, look, devRow);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, level);

            // Top bar: Petals with "+" to the Store once unlocked, then Settings (frames 2 and 3).
            float bar = r.TopBar.Height;
            Kit.RoundButton(p, r.TopBar.Right - (bar / 2f), r.TopBar.CenterY, bar, "ui.settings", () => app.OpenOverlay(Overlay.Settings));
            Box petals = new Box(r.TopBar.Right - bar - p.U(24f) - p.U(330f), r.TopBar.CenterY - (bar * 0.36f), r.TopBar.Right - bar - p.U(24f), r.TopBar.CenterY + (bar * 0.36f));
            Kit.PetalsPill(p, petals, app.ShownPetals, look.Store ? () => app.OpenOverlay(Overlay.Store) : (Action?)null);
            Kit.SparkleBurst(p, petals.Left + (petals.Height * 0.5f), petals.CenterY, petals.Height, app.SinceRewardBurst);
            if (look.Wardrobe)
            {
                Avatar(p, r.TopBar.Left + (bar / 2f), r.TopBar.CenterY, bar, meta.Wardrobe.Profile);
            }

            Hero(p, r, look, meta, app);

            // LEVEL N, the milestone teaser, PLAY.
            p.Text(PlaytestText.F("common.level", NumberText.Group(level)), r.Level.CenterX, r.Level.CenterY, T.LevelHome, C.TextPrimary, r.Level.Width, look: TextLook.Headline);
            if (look.Teaser && next.HasValue)
            {
                p.Mark("ui.gift");
                p.FillRound(r.Teaser, r.Teaser.Height / 2f, C.SurfacePanel.WithAlpha(0.9f));
                string teaser = next.Value.WinsToGo == 1 ? PlaytestText.T("home.level_to_reward") : PlaytestText.F("home.levels_to_reward", next.Value.WinsToGo);
                float gift = r.Teaser.Height * 0.72f;
                p.Text(teaser, r.Teaser.CenterX - (gift * 0.4f), r.Teaser.CenterY, T.Body, C.TextPrimary, r.Teaser.Width - (gift * 2.2f));
                p.Shape("ui.gift", Box.FromCenter(r.Teaser.Right - (gift * 0.9f), r.Teaser.CenterY, gift, gift), C.BoosterBloomBurst);
            }

            bool available = app.Content.LevelCount > 0;
            Kit.PrimaryButton(p, r.Play, PlaytestText.T("common.play"), available ? app.StartLevel : (Action?)null, T.ButtonLarge, decorate: true, playArrow: true, breathe: app.Overlays.Count == 0);

            if (look.Rank)
            {
                // Rank row (the server is deferred: the playtest shows the offline form).
                p.Mark("ui.trophy");
                float icon = r.Rank.Height * 0.7f;
                string rank = PlaytestText.T("home.rank_unknown_offline");
                float textWidth = Math.Min(p.MeasureText(rank, T.Body), r.Rank.Width - (icon * 3f));
                float start = r.Rank.CenterX - ((icon + p.U(16f) + textWidth + icon) / 2f);
                p.Shape("ui.trophy", Box.FromCenter(start + (icon / 2f), r.Rank.CenterY, icon, icon), C.MedalGold.Darken(0.1f));
                p.Text(rank, start + icon + p.U(16f) + (textWidth / 2f), r.Rank.CenterY, T.Body, C.TextPrimary, textWidth);
                p.Shape("ui.chevron", Box.FromCenter(start + icon + p.U(32f) + textWidth + (icon * 0.3f), r.Rank.CenterY, icon * 0.6f, icon * 0.6f), C.TextSecondary);
                p.Hit(Kit.Touch(p, r.Rank), () => app.OpenOverlay(Overlay.Leaderboard));
            }

            if (look.DailyChallenge)
            {
                DailyCard(p, r.Daily, app);
            }

            DevRow(p, app, new Box(r.Safe.Left + p.U(44f), r.Safe.Bottom - devRow, r.Safe.Right - p.U(44f), r.Safe.Bottom - p.U(16f)));

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, r.Hero, toast);
            }
        }

        /// <summary>The Bloomlings wordmark placeholder: bold outlined green text with a Petal above the i.</summary>
        public static void Wordmark(IPainter p, float cx, float cy, float maxWidth)
        {
            p.Mark("brand.wordmark");
            string name = PlaytestText.T("home.logo");
            TypeStyle style = T.Wordmark;
            float width = Math.Min(maxWidth, p.MeasureText(name, style));
            float scale = width / Math.Max(1f, p.MeasureText(name, style));
            p.Text(name, cx, cy + p.U(8f), style, C.WordmarkOutline, sizeScale: scale);
            p.Text(name, cx, cy, style, C.WordmarkFill, sizeScale: scale);
            float petal = style.Size * p.Scale * scale * 0.42f;
            Kit.Petal(p, Box.FromCenter(cx + (width * 0.08f), cy - (style.Size * p.Scale * scale * 0.62f), petal, petal));
        }

        /// <summary>The hero area: the four 3D heroes on their stone early on (frame 2), the player's hero later (frame 3).</summary>
        private static void Hero(IPainter p, HomeRegions r, HomeLook look, PlaytestMeta meta, DesignApp app)
        {
            Box hero = r.Hero;
            if (!look.Hero)
            {
                Wordmark(p, hero.CenterX, hero.Top + (hero.Height * 0.22f), Math.Min(hero.Width * 0.8f, p.U(760f)));
                var stage = new Box(hero.Left, hero.Top + (hero.Height * 0.36f), hero.Right, hero.Bottom);
                (Box group, Box guest) = CharacterArt.GroupWithGuest(stage);
                Visuals.Group(p, group);
                Visuals.Guest(p, guest);
                return;
            }

            // Frame 3: the player's family as a large 3D hero in its outfit, with the Wardrobe and Collection buttons.
            p.Mark("char.hero.home");
            float heroHeight = Math.Min(hero.Height * 0.9f, hero.Width * 0.62f * CharacterArt.HeroHeight / CharacterArt.HeroWidth);
            Box body = Box.FromCenter(hero.CenterX, hero.CenterY, heroHeight * CharacterArt.HeroWidth / CharacterArt.HeroHeight, heroHeight);
            Visuals.Hero(p, body, Family.Sprig, meta.Wardrobe.OutfitOf(Family.Sprig));
            Visuals.Guest(p, CharacterArt.GuestBesideHero(PainterBase.Fit(body, CharacterArt.HeroWidth, CharacterArt.HeroHeight), hero));

            float button = p.U(DesignTokens.Size.IconButton);
            float y = r.Features.Top + (button * 0.6f);
            if (look.Wardrobe)
            {
                Kit.RoundButton(p, r.Features.CenterX, y, button, "ui.shirt", () => app.HomeToast("Wardrobe: not in the playtest yet"));
                y += button * 1.25f;
            }

            if (look.Collection)
            {
                Kit.RoundButton(p, r.Features.CenterX, y, button, "ui.grid", () => app.OpenOverlay(Overlay.Collection));
            }
        }

        /// <summary>The Daily Challenge card: sun, title, "New today · +N" with the Petal symbol, chevron.</summary>
        private static void DailyCard(IPainter p, Box box, DesignApp app)
        {
            p.FillRound(box.Offset(0f, p.U(6f)), box.Height * 0.3f, C.SurfacePanelEdge);
            p.FillRound(box, box.Height * 0.3f, C.SurfacePanel);
            float icon = box.Height * 0.62f;
            p.Shape("ui.sun", Box.FromCenter(box.Left + (box.Height * 0.55f), box.CenterY, icon, icon), C.PetalCenter.Darken(0.1f));
            float left = box.Left + box.Height;
            p.TextLeft(PlaytestText.T("daily.title"), left, box.Top + (box.Height * 0.36f), T.Body, C.TextPrimary, box.Width - (box.Height * 2f));
            // The playtest has no Daily Challenge content, so its reward is not shown.
            p.TextLeft(PlaytestText.T("home.daily_new_plain"), left, box.Top + (box.Height * 0.7f), T.Caption, C.TextSecondary, box.Width - (box.Height * 2.4f));
            p.Shape("ui.chevron", Box.FromCenter(box.Right - (box.Height * 0.4f), box.CenterY, icon * 0.5f, icon * 0.5f), C.TextSecondary);
            p.Hit(box, () => app.HomeToast("Daily Challenge: not in the playtest yet"));
        }

        /// <summary>The profile avatar: a round portrait in the chosen frame, with the badge (FR-061).</summary>
        private static void Avatar(IPainter p, float cx, float cy, float size, ProfileLook look)
        {
            p.FillCircle(cx, cy, size / 2f, Rgba.White);
            p.FillCircle(cx, cy, (size / 2f) - p.U(8f), C.PillLevel.Lighten(0.4f));
            Visuals.Hero(p, Box.FromCenter(cx, cy + (size * 0.02f), size * 0.74f, size * 0.74f), Family.Sprig, null);
            if (look.Frame != null)
            {
                p.Shape("cosmetic.frame", Box.FromCenter(cx, cy, size * 1.08f, size * 1.08f), Tint(look.Frame));
            }

            if (look.Badge != null)
            {
                p.Shape("cosmetic.badge", Box.FromCenter(cx + (size * 0.36f), cy + (size * 0.36f), size * 0.36f, size * 0.36f), Tint(look.Badge));
            }
        }

        private static Rgba Tint(CosmeticItem item) => Visuals.Tint(item);

        /// <summary>The playtest's own controls (not the product): step back, skip one or ten levels, start a new profile.</summary>
        private static void DevRow(IPainter p, DesignApp app, Box row)
        {
            p.TextLeft("dev", row.Left, row.Top - p.U(18f), T.Caption, C.TextSecondary);
            string[] labels = { "−1", "+1", "+10", "Reset" };
            Action[] actions =
            {
                () => app.Meta.StepBack(),
                () => app.Meta.SkipTo(app.Meta.Progression.HighestCompletedLevel + 1),
                () => app.Meta.SkipTo(app.Meta.Progression.HighestCompletedLevel + 10),
                app.ResetProfile,
            };
            Box[] cells = ScreenLayout.Row(row, labels.Length, p.U(16f), float.MaxValue, square: false);
            for (int i = 0; i < cells.Length; i++)
            {
                p.FillRound(cells[i], cells[i].Height / 2f, C.SurfaceSunk.WithAlpha(0.85f));
                p.Text(labels[i], cells[i].CenterX, cells[i].CenterY, T.ButtonSecondary, C.TextSecondary, cells[i].Width * 0.9f);
                Action action = actions[i];
                p.Hit(cells[i], action);
            }
        }
    }
}
