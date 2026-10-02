using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The splash of frame 1 (spec 002 FR-016, spec 005 §4.5) in Home's reference layout (§6.4): the wooden wordmark in
    /// Home's logo box over the warm garden, and the four families around the lotus fountain on the stone in Home's
    /// diorama box, so the splash turns into Home without anything jumping. It shows while the game starts and never
    /// waits for a tap.
    /// </summary>
    public static class SplashScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            DesignApp.DrawBackdrop(p, BackdropScene.Splash, 1);
            ReferenceHomeRegions r = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p));
            float appear = Kit.Ease(app.Now / 0.5f);
            p.PushAlpha(appear);
            HomeScreen.Wordmark(p, r.Logo);
            p.PopAlpha();

            // The four families as 3D heroes on their stone (spec 004 FR-017), rising in with the wordmark.
            p.PushAlpha(appear);
            p.PushTransform(0f, (1f - appear) * r.W * 0.04f, 1f, 0f, 0f);
            HomeScreen.Stage(p, r.Diorama, BackdropScene.Splash, guest: false);
            p.PopTransform();

            // Three lotus buds pulsing in turn where Play will be, while the game loads.
            float bud = r.W * 0.075f;
            for (int i = 0; i < 3; i++)
            {
                float pulse = Math.Max(0f, (float)Math.Sin((app.Now * 5f) - (i * 1.1f)));
                float size = bud * (0.78f + (0.22f * pulse));
                Kit.Petal(p, Box.FromCenter(r.Play.CenterX + ((i - 1) * bud * 1.7f), r.Play.CenterY - (bud * 0.12f * pulse), size, size));
            }

            p.PopAlpha();
            p.Mark("brand.splash_art");
        }
    }

    /// <summary>
    /// Home of frames 2 and 3 (spec 002 FR-017, spec 001 FR-058) in the reference layout (spec 005 FR-024,
    /// contracts/look.md §6.4, <see cref="ScreenLayout.ReferenceHome"/>):
    /// <list type="bullet">
    /// <item><description>Always shown: Settings at the top left, the Petals pill at the top right, the wooden logo across
    /// the top, the diorama (the owner's Home picture, or the four heroes around the lotus fountain on the stone, in their
    /// outfits once the Wardrobe is open), the level on a wooden plaque and the big Play button in its wooden
    /// rim.</description></item>
    /// <item><description>Shown once unlocked, as cream round buttons along the sides: the Wardrobe, the Collection and the
    /// profile avatar at the left, the Daily Challenge and the Store at the right, with the rank as a parchment pill under
    /// the right column; "N levels to reward" with the gift as a parchment pill under Play.</description></item>
    /// </list>
    /// Play always continues Level N, and there is no level map. A small row at the very bottom keeps the playtest's
    /// skip and reset controls; it is not part of the product.
    /// </summary>
    public static class HomeScreen
    {
        /// <summary>The size of Play's label as a share of its button's height (the reference's big "PLAY").</summary>
        private const float PlayLabelShare = 0.5f;

        /// <summary>
        /// The height the playtest's dev row keeps at the bottom of the safe area: one touch target and a small gap. Home's
        /// layout leaves it out (<see cref="ScreenLayout.ReferenceHome"/>'s <c>bottomReserve</c>), and so does the splash.
        /// </summary>
        public static float DevReserve(IPainter p) => p.U(DesignTokens.Size.TouchMin) * 1.12f;

        public static void Draw(IPainter p, DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            int level = meta.CurrentLevel;
            (int Level, Client.App.Progression.MilestoneCadence Cadence, int WinsToGo)? next = meta.Milestones.Next(meta.Progression.HighestCompletedLevel);
            HomeLook look = HomeLook.From(meta.Progression.IsUnlocked, meta.Collection.Count, next.HasValue, dailyChallengeAvailable: true, freeBoosterOffer: false);
            ReferenceHomeRegions r = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, DevReserve(p));
            DesignApp.DrawBackdrop(p, BackdropScene.Home, level);

            // The logo across the top and the diorama: the four heroes around the lotus fountain, in their outfits once
            // the Wardrobe is open.
            Wordmark(p, r.Logo);
            if (look.Hero)
            {
                p.Mark("char.hero.home");
            }

            Stage(p, r.Diorama, BackdropScene.Home, guest: true, look.Wardrobe ? meta.Wardrobe.OutfitOf : (Func<Family, Outfit>?)null);

            // A few pink petals drifting through the garden, as on the reference's Home (Home redraws for Play's breath).
            Kit.FallingPetals(p, new Box(r.Safe.Left, r.Logo.Bottom, r.Safe.Right, r.Plaque.Top), app.Now);

            // The top: Settings at the left; the Petals at the right, with "+" to the Store once unlocked.
            Kit.RoundButton(p, r.Settings.CenterX, r.Settings.CenterY, r.Settings.Width, "ui.settings", () => app.OpenOverlay(Overlay.Settings));
            Kit.PetalsPill(p, r.Petals, app.ShownPetals, look.Store ? () => app.OpenOverlay(Overlay.Store) : (Action?)null);
            Kit.SparkleBurst(p, r.Petals.Left + (r.Petals.Height * 0.5f), r.Petals.CenterY, r.Petals.Height, app.SinceRewardBurst);

            SideButtons(p, r, look, meta, app);

            // The level on its wooden plaque, Play, the milestone teaser.
            Kit.WoodSign(p, r.Plaque, PlaytestText.F("common.level", NumberText.Group(level)), T.LevelHome);
            bool available = app.Content.LevelCount > 0;
            TypeStyle play = T.ButtonLarge with { Size = Math.Max(T.ButtonLarge.Size, r.Play.Height * PlayLabelShare / p.Scale) };
            Kit.PrimaryButton(p, r.Play, PlaytestText.T("common.play"), available ? app.StartLevel : (Action?)null, play, decorate: true, playArrow: true, breathe: app.Overlays.Count == 0);
            if (look.Teaser && next.HasValue)
            {
                string teaser = next.Value.WinsToGo == 1 ? PlaytestText.T("home.level_to_reward") : PlaytestText.F("home.levels_to_reward", next.Value.WinsToGo);
                Teaser(p, r.Teaser, teaser);
            }

            DevRow(p, app, new Box(r.Safe.Left + (r.W * 0.04f), r.Safe.Bottom - p.U(DesignTokens.Size.TouchMin), r.Safe.Right - (r.W * 0.04f), r.Safe.Bottom));

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Diorama.Top, r.Safe.Right, r.Plaque.Top), toast);
            }
        }

        /// <summary>
        /// The Bloomlings wordmark in <paramref name="box"/> (Home's logo box): the owner's logo picture when it is embedded
        /// (spec 005 pictures.md C1), else the wooden letters with leaves and flowers (<see cref="Kit.WoodLogo"/>).
        /// </summary>
        public static void Wordmark(IPainter p, Box box)
        {
            string name = PlaytestText.T("home.logo");
            Visuals.Logo(p, box, () => Kit.WoodLogo(p, box, name));
        }

        /// <summary>
        /// The four heroes on Home and the splash (spec 005 §4.5, §6.4). Over the owner's garden picture (pictures.md B1,
        /// B6, which has the well and the fountain), the group picture stands in front of it; until then the drawn stage
        /// (<see cref="HomeStage.Diorama"/>): the stone pedestal, Sprig, Bloom and Drop behind the lotus fountain, Twig and
        /// the guest (when <paramref name="guest"/>) in front, each hero in <paramref name="outfitOf"/>'s outfit.
        /// </summary>
        public static void Stage(IPainter p, Box stage, BackdropScene scene, bool guest, Func<Family, Outfit>? outfitOf = null)
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

            HomeDiorama diorama = HomeStage.ReferenceDiorama(stage, guest);
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
                Visuals.Hero(p, box, family, outfitOf?.Invoke(family));
            }
        }

        /// <summary>
        /// The cream round side buttons of the unlocked features (§6.4), each column stacked from its top: the Wardrobe,
        /// the Collection and the profile avatar at the left; the Daily Challenge and the Store at the right, with the rank
        /// pill under the right column.
        /// </summary>
        private static void SideButtons(IPainter p, ReferenceHomeRegions r, HomeLook look, PlaytestMeta meta, DesignApp app)
        {
            int left = 0;
            if (look.Wardrobe)
            {
                Box box = r.SideButton(false, left++);
                Kit.RoundButton(p, box.CenterX, box.CenterY, box.Width, "ui.shirt", app.OpenWardrobe);
            }

            if (look.Collection)
            {
                Box box = r.SideButton(false, left++);
                Kit.RoundButton(p, box.CenterX, box.CenterY, box.Width, "ui.grid", () => app.OpenOverlay(Overlay.Collection));
            }

            if (look.Wardrobe)
            {
                // The profile avatar opens the Wardrobe too, as the Unity client's opens its Profile tab.
                Avatar(p, r.SideButton(false, left++), meta.Wardrobe.Profile, app.OpenWardrobe);
            }

            int right = 0;
            if (look.DailyChallenge)
            {
                // The playtest has no Daily Challenge content.
                RoundSide(p, r.SideButton(true, right++), () => app.HomeToast("Daily Challenge: not in the playtest yet"), face =>
                {
                    p.Mark("ui.sun");
                    float g = face.Width * 0.72f;
                    OutlinedShape(p, "ui.sun", Box.FromCenter(face.CenterX, face.CenterY, g, g), C.GardenFlowerCenter, C.GardenFlowerCenterLine);
                });
            }

            if (look.Store)
            {
                RoundSide(p, r.SideButton(true, right++), () => app.OpenOverlay(Overlay.Store), face => StoreGlyph(p, face));
            }

            if (look.Rank)
            {
                // The rank under the right column (the server is deferred: the playtest shows the offline form).
                float top = right > 0 ? r.SideButton(true, right - 1).Bottom + (r.W * ReferenceHomeRegions.SideGapShare) : r.SideButton(true, 0).Top;
                var rank = new Box(r.Rank.Left, top, r.Rank.Right, top + r.Rank.Height);
                RankRow(p, rank, PlaytestText.T("home.rank_unknown_offline"));
                p.Hit(Kit.Touch(p, rank), () => app.OpenOverlay(Overlay.Leaderboard));
            }
        }

        /// <summary>
        /// A cream round side button with a colored glyph (§3.3's domed cushion, as <see cref="Kit.RoundButton"/>):
        /// <paramref name="glyph"/> draws into the face's content box, which moves with the press.
        /// </summary>
        private static void RoundSide(IPainter p, Box box, Action action, Action<Box> glyph)
        {
            p.Mark("ui.button.round");
            float depth = Kit.Press(p, box, true);
            Kit.Squash(p, box, depth);
            Box face = Kit.IconFace(p, box, GardenLook.White, box.Width / 2f, depth);
            glyph(face);
            p.PopTransform();
            p.Hit(Kit.Touch(p, box), action);
        }

        /// <summary>The Store's glyph: a pink lotus rising from the woven reward basket (the Store sells for Petals).</summary>
        private static void StoreGlyph(IPainter p, Box face)
        {
            p.Mark("currency.reward_basket");
            float s = face.Width;
            Kit.Petal(p, Box.FromCenter(face.CenterX, face.CenterY - (s * 0.14f), s * 0.62f, s * 0.62f));
            var basket = Box.FromCenter(face.CenterX, face.CenterY + (s * 0.17f), s * 0.78f, s * 0.5f);
            Func<float, float, float> sdf = ShapeLibrary.Get("currency.reward_basket");
            p.ShapeOf("currency.reward_basket/line", (x, y) => sdf(x, y) - 0.06f, basket, C.WoodDarkLine);
            p.Shape("currency.reward_basket", basket, C.RewardBasket);
            p.PushClip(new Box(basket.Left, basket.Top, basket.Right, basket.CenterY));
            p.ShapeOf("currency.reward_basket/light", (x, y) => sdf(x, y) + 0.04f, basket, C.RewardBasket.Lighten(0.25f));
            p.PopClip();
        }

        /// <summary>The milestone teaser: a parchment pill with "N levels to reward" and the pink gift.</summary>
        private static void Teaser(IPainter p, Box box, string text)
        {
            p.Mark("ui.gift");
            Kit.ParchmentPill(p, box);
            float gift = box.Height * 0.68f;
            float scale = Math.Min(1f, (box.Height * 0.5f) / p.U(T.Body.Size));
            float textWidth = Math.Min(p.MeasureText(text, T.Body, scale), box.Width - (gift * 2.6f));
            float gap = gift * 0.3f;
            float start = box.CenterX - ((textWidth + gap + gift) / 2f);
            p.Text(text, start + (textWidth / 2f), box.CenterY, T.Body, C.InkBrown, textWidth, scale, TextLook.Plain(C.InkBrown));
            OutlinedShape(p, "ui.gift", Box.FromCenter(start + textWidth + gap + (gift / 2f), box.CenterY - (gift * 0.03f), gift, gift), C.LotusFill, C.LotusLine);
        }

        /// <summary>The rank row: a parchment pill with the gold trophy, the rank and a brown chevron.</summary>
        private static void RankRow(IPainter p, Box box, string rank)
        {
            p.Mark("ui.trophy");
            Kit.ParchmentPill(p, box);
            float icon = box.Height * 0.66f;
            float chevron = icon * 0.5f;
            float gap = icon * 0.2f;
            float scale = Math.Min(1f, (box.Height * 0.48f) / p.U(T.Body.Size));
            float textWidth = Math.Min(p.MeasureText(rank, T.Body, scale), box.Width - (icon * 2.4f) - chevron);
            float start = box.CenterX - ((icon + gap + textWidth + gap + chevron) / 2f);
            OutlinedShape(p, "ui.trophy", Box.FromCenter(start + (icon / 2f), box.CenterY, icon, icon), C.MedalGold, C.MedalGold.Darken(0.42f));
            p.Text(rank, start + icon + gap + (textWidth / 2f), box.CenterY, T.Body, C.InkBrown, textWidth, scale, TextLook.Plain(C.InkBrown));
            p.Shape("ui.chevron", Box.FromCenter(start + icon + gap + textWidth + gap + (chevron / 2f), box.CenterY, chevron, chevron), C.InkBrownSoft);
        }

        /// <summary>A glyph in a saturated color over its darker outline (the gift, the trophy), as the reference's icons.</summary>
        private static void OutlinedShape(IPainter p, string shapeId, Box box, Rgba fill, Rgba line)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            p.ShapeOf(shapeId + "/line/0.07", (x, y) => sdf(x, y) - 0.07f, box, line);
            p.Shape(shapeId, box, fill);
        }

        /// <summary>
        /// The profile avatar (FR-061) as the left column's last round button: the hero's portrait on a domed cream disc in
        /// the chosen frame, with the badge. A tap opens the Wardrobe.
        /// </summary>
        private static void Avatar(IPainter p, Box box, ProfileLook look, Action action)
        {
            float size = box.Width;
            float depth = Kit.Press(p, box, true);
            Kit.Squash(p, box, depth);
            Box face = Kit.IconFace(p, box, GardenLook.White, size / 2f, depth);
            p.FillRoundGradient(face, face.Width / 2f, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));
            p.StrokeCircle(face.CenterX, face.CenterY, face.Width / 2f, Math.Max(1f, p.U(DesignTokens.Garden.OutlineWidth)), C.CreamLine);
            Visuals.Hero(p, Box.FromCenter(face.CenterX, face.CenterY + (size * 0.02f), size * 0.7f, size * 0.7f), Family.Sprig, null);
            if (look.Frame != null)
            {
                p.Shape("cosmetic.frame", Box.FromCenter(face.CenterX, face.CenterY, size * 1.08f, size * 1.08f), Visuals.Tint(look.Frame));
            }

            if (look.Badge != null)
            {
                p.Shape("cosmetic.badge", Box.FromCenter(face.CenterX + (size * 0.36f), face.CenterY + (size * 0.36f), size * 0.36f, size * 0.36f), Visuals.Tint(look.Badge));
            }

            p.PopTransform();
            p.Hit(Kit.Touch(p, box), action);
        }

        /// <summary>
        /// The playtest's own controls (not the product), small at the very bottom: step back, skip one or ten levels, start
        /// a new profile. Each pill is half the row tall; the whole cell takes the tap.
        /// </summary>
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
            float label = p.MeasureText("dev", T.Caption) + (row.Height * 0.2f);
            p.TextLeft("dev", row.Left, row.CenterY, T.Caption, C.InkBrownSoft);
            Box[] cells = ScreenLayout.Row(new Box(row.Left + label, row.Top, row.Right, row.Bottom), labels.Length, row.Height * 0.1f, float.MaxValue, square: false);
            float line = Math.Max(1f, p.U(DesignTokens.Garden.OutlineWidthSmall));
            for (int i = 0; i < cells.Length; i++)
            {
                Box pill = Box.FromCenter(cells[i].CenterX, cells[i].CenterY, cells[i].Width, cells[i].Height * 0.5f);
                p.FillRound(pill, pill.Height / 2f, C.CreamTop.WithAlpha(0.72f));
                p.StrokeRound(pill.Inset(line / 2f), (pill.Height / 2f) - (line / 2f), line, C.CreamLine.WithAlpha(0.6f));
                p.Text(labels[i], pill.CenterX, pill.CenterY, T.Caption, C.InkBrownSoft, pill.Width * 0.9f);
                p.Hit(cells[i], actions[i]);
            }
        }
    }
}
