using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The splash of frame 1 (spec 002 FR-016, spec 005 §4.5): the wooden wordmark over the warm garden, with the four
    /// families around the lotus fountain on the stone. It shows while the game starts and never waits for a tap.
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
            float width = Math.Min(safe.Width, p.U(1000f));
            var stage = Box.FromCenter(safe.CenterX, safe.Top + (safe.Height * 0.62f), width, width * 0.7f);
            p.PushAlpha(appear);
            p.PushTransform(0f, (1f - appear) * p.U(40f), 1f, 0f, 0f);
            HomeScreen.Stage(p, stage, BackdropScene.Splash, guest: false);
            p.PopTransform();
            p.PopAlpha();
            p.Mark("brand.splash_art");
        }
    }

    /// <summary>
    /// Home of frames 2 and 3 (spec 002 FR-017, spec 001 FR-058) in the reference look (spec 005 §4.5).
    /// <list type="bullet">
    /// <item><description>Always shown: the Petals pill, Settings, the level on a wooden plaque and the big Play button in
    /// its wooden rim.</description></item>
    /// <item><description>Shown once unlocked: the hero on its stone pedestal with the Wardrobe and Collection buttons,
    /// "N levels to reward" with a gift, the rank and the Daily Challenge card, on parchment.</description></item>
    /// </list>
    /// Play always continues Level N, and there is no level map. A small labelled dev row at the bottom keeps the
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

            // The level on its wooden plaque, the milestone teaser, Play.
            LevelPlaque(p, r.Level, PlaytestText.F("common.level", NumberText.Group(level)));
            if (look.Teaser && next.HasValue)
            {
                string teaser = next.Value.WinsToGo == 1 ? PlaytestText.T("home.level_to_reward") : PlaytestText.F("home.levels_to_reward", next.Value.WinsToGo);
                Teaser(p, r.Teaser, teaser);
            }

            bool available = app.Content.LevelCount > 0;
            Kit.PrimaryButton(p, r.Play, PlaytestText.T("common.play"), available ? app.StartLevel : (Action?)null, T.ButtonLarge, decorate: true, playArrow: true, breathe: app.Overlays.Count == 0);

            if (look.Rank)
            {
                // Rank row (the server is deferred: the playtest shows the offline form).
                RankRow(p, r.Rank, PlaytestText.T("home.rank_unknown_offline"));
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

        /// <summary>
        /// The Bloomlings wordmark: the owner's logo picture when it is embedded (spec 005 pictures.md C1), else the wooden
        /// letters with leaves and flowers (<see cref="Kit.WoodLogo"/>), at most <paramref name="maxWidth"/> wide.
        /// </summary>
        public static void Wordmark(IPainter p, float cx, float cy, float maxWidth)
        {
            string name = PlaytestText.T("home.logo");
            Box box = Box.FromCenter(cx, cy, maxWidth, Math.Min(maxWidth * 0.37f, p.U(T.Wordmark.Size) * 1.5f));
            Visuals.Logo(p, box, () => Kit.WoodLogo(p, box, name));
        }

        /// <summary>
        /// The four heroes on Home early and the splash (spec 005 §4.5). Over the owner's garden picture (pictures.md B1,
        /// B6, which has the well and the fountain), the group picture stands in front of it; until then the drawn stage
        /// (<see cref="HomeStage.Diorama"/>): the stone pedestal, Sprig, Bloom and Drop behind the lotus fountain, Twig and
        /// the guest (when <paramref name="guest"/>) in front.
        /// </summary>
        public static void Stage(IPainter p, Box stage, BackdropScene scene, bool guest)
        {
            if (p.HasSprite(PainterBase.BackgroundPrefix + OwnerPictures.Background(scene, string.Empty)))
            {
                (Box group, Box beside) = CharacterArt.GroupWithGuest(stage);
                Visuals.Group(p, group);
                if (guest)
                {
                    Visuals.Guest(p, beside);
                }

                return;
            }

            HomeDiorama diorama = HomeStage.Diorama(stage, guest);
            Kit.StonePedestal(p, diorama.Pedestal);
            for (int i = 0; i < diorama.Heroes.Count; i++)
            {
                // The fountain and the guest stand between the back row (Bloom, Sprig, Drop) and Twig in front.
                if (i == 3)
                {
                    Kit.LotusFountain(p, diorama.Fountain);
                    if (guest)
                    {
                        Visuals.Guest(p, diorama.Guest);
                    }
                }

                (Family family, Box box) = diorama.Heroes[i];
                Visuals.Hero(p, box, family, null);
            }
        }

        /// <summary>
        /// The hero area: the wordmark and the four 3D heroes on their stone early on (frame 2), the player's hero on a
        /// stone pedestal later (frame 3), with the Wardrobe and Collection buttons.
        /// </summary>
        private static void Hero(IPainter p, HomeRegions r, HomeLook look, PlaytestMeta meta, DesignApp app)
        {
            Box hero = r.Hero;
            if (!look.Hero)
            {
                Wordmark(p, hero.CenterX, hero.Top + (hero.Height * 0.22f), Math.Min(hero.Width * 0.8f, p.U(760f)));
                Stage(p, new Box(hero.Left, hero.Top + (hero.Height * 0.36f), hero.Right, hero.Bottom), BackdropScene.Home, guest: true);
                return;
            }

            // Frame 3: the player's family as a large 3D hero in its outfit on the stone pedestal (until the owner's garden
            // picture brings its own ground), the guest beside it, the Wardrobe and Collection buttons.
            p.Mark("char.hero.home");
            (Box pedestal, Box body, Box guest) = HomeStage.HeroOnPedestal(new Box(hero.Left, hero.Top, hero.Right, hero.Bottom - p.U(8f)));
            if (!p.HasSprite(PainterBase.BackgroundPrefix + OwnerPictures.Home))
            {
                Kit.StonePedestal(p, pedestal);
            }

            Visuals.Hero(p, body, Family.Sprig, meta.Wardrobe.OutfitOf(Family.Sprig));
            Visuals.Guest(p, guest);

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

        /// <summary>"Level N" on a plain wooden plaque (spec 005 §4.5), as tall as its row and as wide as its letters need.</summary>
        private static void LevelPlaque(IPainter p, Box row, string text)
        {
            float h = row.Height;
            float scale = Math.Min(1f, (h * 0.62f) / p.U(T.LevelHome.Size));
            float width = Math.Min(row.Width * 0.8f, p.MeasureText(text, T.LevelHome, scale) + (h * 1.5f));
            Kit.WoodSign(p, Box.FromCenter(row.CenterX, row.CenterY, width, h), text, T.LevelHome);
        }

        /// <summary>The milestone teaser: a parchment pill with "N levels to reward" and the pink gift.</summary>
        private static void Teaser(IPainter p, Box box, string text)
        {
            p.Mark("ui.gift");
            Kit.ParchmentPill(p, box);
            float gift = box.Height * 0.68f;
            float textWidth = Math.Min(p.MeasureText(text, T.Body), box.Width - (gift * 2.6f));
            float start = box.CenterX - ((textWidth + p.U(18f) + gift) / 2f);
            p.Text(text, start + (textWidth / 2f), box.CenterY, T.Body, C.InkBrown, textWidth, look: TextLook.Plain(C.InkBrown));
            OutlinedShape(p, "ui.gift", Box.FromCenter(start + textWidth + p.U(18f) + (gift / 2f), box.CenterY - (gift * 0.03f), gift, gift), C.LotusFill, C.LotusLine);
        }

        /// <summary>The rank row: a parchment pill with the gold trophy, the rank and a brown chevron.</summary>
        private static void RankRow(IPainter p, Box box, string rank)
        {
            p.Mark("ui.trophy");
            Kit.ParchmentPill(p, box);
            float icon = box.Height * 0.66f;
            float chevron = icon * 0.5f;
            float gap = p.U(16f);
            float textWidth = Math.Min(p.MeasureText(rank, T.Body), box.Width - (icon * 3f));
            float start = box.CenterX - ((icon + gap + textWidth + gap + chevron) / 2f);
            OutlinedShape(p, "ui.trophy", Box.FromCenter(start + (icon / 2f), box.CenterY, icon, icon), C.MedalGold, C.MedalGold.Darken(0.42f));
            p.Text(rank, start + icon + gap + (textWidth / 2f), box.CenterY, T.Body, C.InkBrown, textWidth, look: TextLook.Plain(C.InkBrown));
            p.Shape("ui.chevron", Box.FromCenter(start + icon + gap + textWidth + gap + (chevron / 2f), box.CenterY, chevron, chevron), C.InkBrownSoft);
        }

        /// <summary>A glyph in a saturated color over its darker outline (the gift, the trophy), as the reference's icons.</summary>
        private static void OutlinedShape(IPainter p, string shapeId, Box box, Rgba fill, Rgba line)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            p.ShapeOf(shapeId + "/line/0.07", (x, y) => sdf(x, y) - 0.07f, box, line);
            p.Shape(shapeId, box, fill);
        }

        /// <summary>The Daily Challenge card on parchment: the sun on a cream disc, the title, "New today", a chevron.</summary>
        private static void DailyCard(IPainter p, Box box, DesignApp app)
        {
            Kit.Paper(p, box, box.Height * 0.3f, DesignTokens.Garden.OutlineWidth, 6f);
            float disc = box.Height * 0.66f;
            float cx = box.Left + (box.Height * 0.56f);
            Box face = Kit.IconFace(p, Box.FromCenter(cx, box.CenterY, disc, disc), GardenLook.White, disc / 2f, 0f);
            OutlinedShape(p, "ui.sun", face.Inset(-face.Width * 0.06f), C.GardenFlowerCenter, C.GardenFlowerCenterLine);
            float left = box.Left + (box.Height * 1.05f);
            float width = box.Width - (box.Height * 1.7f);
            p.TextLeft(PlaytestText.T("daily.title"), left, box.Top + (box.Height * 0.37f), T.ButtonSecondary, C.InkBrown, width, 0.9f, TextLook.Plain(C.InkBrown));
            // The playtest has no Daily Challenge content, so its reward is not shown.
            p.TextLeft(PlaytestText.T("home.daily_new_plain"), left, box.Top + (box.Height * 0.69f), T.Caption, C.InkBrownSoft, width);
            float chevron = box.Height * 0.3f;
            p.Shape("ui.chevron", Box.FromCenter(box.Right - (box.Height * 0.42f), box.CenterY, chevron, chevron), C.InkBrownSoft);
            p.Hit(box, () => app.HomeToast("Daily Challenge: not in the playtest yet"));
        }

        /// <summary>
        /// The profile avatar (FR-061): the hero's portrait on a domed cream disc like the round buttons, in the chosen
        /// frame, with the badge.
        /// </summary>
        private static void Avatar(IPainter p, float cx, float cy, float size, ProfileLook look)
        {
            Box face = Kit.IconFace(p, Box.FromCenter(cx, cy, size, size), GardenLook.White, size / 2f, 0f);
            p.FillRoundGradient(face, face.Width / 2f, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));
            p.StrokeCircle(face.CenterX, face.CenterY, face.Width / 2f, Math.Max(1f, p.U(3f)), C.CreamLine);
            Visuals.Hero(p, Box.FromCenter(face.CenterX, face.CenterY + (size * 0.02f), size * 0.7f, size * 0.7f), Family.Sprig, null);
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
            p.TextLeft("dev", row.Left, row.Top - p.U(18f), T.Caption, C.InkBrownSoft);
            string[] labels = { "−1", "+1", "+10", "Reset" };
            Action[] actions =
            {
                () => app.Meta.StepBack(),
                () => app.Meta.SkipTo(app.Meta.Progression.HighestCompletedLevel + 1),
                () => app.Meta.SkipTo(app.Meta.Progression.HighestCompletedLevel + 10),
                app.ResetProfile,
            };
            Box[] cells = ScreenLayout.Row(row, labels.Length, p.U(16f), float.MaxValue, square: false);
            float line = Math.Max(1f, p.U(2f));
            for (int i = 0; i < cells.Length; i++)
            {
                p.FillRound(cells[i], cells[i].Height / 2f, C.CreamTop.WithAlpha(0.72f));
                p.StrokeRound(cells[i].Inset(line / 2f), (cells[i].Height / 2f) - (line / 2f), line, C.CreamLine.WithAlpha(0.6f));
                p.Text(labels[i], cells[i].CenterX, cells[i].CenterY, T.ButtonSecondary, C.InkBrownSoft, cells[i].Width * 0.9f);
                Action action = actions[i];
                p.Hit(cells[i], action);
            }
        }
    }
}
