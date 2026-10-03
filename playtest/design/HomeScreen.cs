using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>How Home and the splash stand their heroes (<see cref="HomeScreen.StageOf"/>).</summary>
    public enum HomeStageKind
    {
        /// <summary>Over the owner's garden picture without its layers: no heroes.</summary>
        None,

        /// <summary>The owner's layered Home with the animated heroes (<see cref="HomeScreen.LayeredStage"/>).</summary>
        Layered,

        /// <summary>The drawn stand-in with the still heroes (<see cref="HomeScreen.Stage"/>).</summary>
        Diorama,
    }

    /// <summary>
    /// The splash of frame 1 (spec 002 FR-016, spec 005 §4.5) in Home's reference layout (§6.4): the wordmark in Home's
    /// logo box over the garden, so the splash turns into Home without anything jumping. Over the owner's layered Home
    /// (spec 005 FR-028, <see cref="HomeScreen.LayeredStage"/>) the fountain stands from the first frame and the four
    /// animated heroes rise in around the lotus already in Home's motion, which carries on into Home
    /// (<see cref="DesignApp.HomeMotion"/>); the drawn stand-in keeps the four still families around the lotus fountain on
    /// the stone in Home's diorama box; over the owner's garden picture alone there are none
    /// (<see cref="HomeStage.ShowsHeroes"/>). It shows while the game starts and never waits for a tap.
    /// </summary>
    public static class SplashScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            DesignApp.DrawBackdrop(p, BackdropScene.Splash, 1);
            ReferenceHomeRegions r = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets);
            float appear = Kit.Ease(app.Now / 0.5f);
            float rise = (1f - appear) * r.W * 0.04f;
            if (HomeScreen.StageOf(p, BackdropScene.Splash) == HomeStageKind.Layered)
            {
                // The heroes in the outfits Home shows, so nothing changes when Home takes over; the wordmark over them.
                HomeScreen.LayeredStage(p, app, HomeScreen.OutfitsOf(app), appear, rise);
                p.PushAlpha(appear);
                HomeScreen.Wordmark(p, r);
            }
            else
            {
                p.PushAlpha(appear);
                HomeScreen.Wordmark(p, r);
                p.PopAlpha();

                // On the drawn stand-in, the four families as 3D heroes around the fountain (spec 004 FR-017), rising in
                // with the wordmark; over the owner's garden picture alone, none.
                p.PushAlpha(appear);
                p.PushTransform(0f, rise, 1f, 0f, 0f);
                HomeScreen.Stage(p, r, BackdropScene.Splash);
                p.PopTransform();
            }

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
    /// the top, the diorama (the owner's layered Home with the four animated heroes around the lotus fountain, spec 005
    /// FR-028, <see cref="LayeredStage"/>; or the drawn stand-in's four still heroes around the lotus fountain on the
    /// stone; in their outfits once the Wardrobe is open), the level on a wooden plaque and the big Play button in its
    /// wooden rim. A tap on an animated hero makes it react (<see cref="HomeMotion.Tap"/>).</description></item>
    /// <item><description>Shown once unlocked, as cream round buttons along the sides: the Wardrobe, the Collection and the
    /// profile avatar at the left, the Daily Challenge and the Store at the right, with the rank as a parchment pill under
    /// the right column; "N levels to reward" with the gift as a parchment pill under Play.</description></item>
    /// </list>
    /// Play always continues Level N, and there is no level map. A small row at the very bottom keeps the playtest's
    /// skip and reset controls; it is not part of the product.
    /// </summary>
    public static class HomeScreen
    {
        /// <summary>
        /// The height the playtest's dev row keeps out of Home's layout (<see cref="ScreenLayout.ReferenceHome"/>'s
        /// <c>bottomReserve</c>): none. The row lies small and faded over the garden under the teaser, so the plaque, Play
        /// and the teaser stand where the reference has them (64%, 73.5% and 89.5% of the height).
        /// </summary>
        public static float DevReserve(IPainter p) => 0f;

        public static void Draw(IPainter p, DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            int level = meta.CurrentLevel;
            (int Level, Client.App.Progression.MilestoneCadence Cadence, int WinsToGo)? next = meta.Milestones.Next(meta.Progression.HighestCompletedLevel);
            HomeLook look = Look(app);
            ReferenceHomeRegions r = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, DevReserve(p));
            DesignApp.DrawBackdrop(p, BackdropScene.Home, level);

            // The diorama and the logo across the top. Over the owner's layered Home: the fountain, the four animated
            // heroes around the lotus (a tap makes one react; the buttons drawn after keep their taps) and the drifting
            // petals, then the logo over them. Else the logo, the drawn stand-in's four still heroes around the lotus
            // fountain (none over the owner's garden picture alone) and a few drawn petals drifting through the garden.
            // The heroes wear their outfits once the Wardrobe is open.
            bool heroes;
            app.HomeMoving = false;
            if (StageOf(p, BackdropScene.Home) == HomeStageKind.Layered)
            {
                app.HomeMoving = LayeredStage(p, app, OutfitsOf(app));
                HeroTaps(p, r, look, app);
                heroes = true;
                Wordmark(p, r);
            }
            else
            {
                Wordmark(p, r);
                heroes = Stage(p, r, BackdropScene.Home, OutfitsOf(app));

                // As on the reference's Home (Home redraws for Play's breath).
                Kit.FallingPetals(p, new Box(r.Safe.Left, r.Logo.Bottom, r.Safe.Right, r.Plaque.Top), app.Now);
            }

            if (heroes && look.Hero)
            {
                p.Mark("char.hero.home");
            }

            // The top: Settings at the left; the Petals at the right, with "+" to the Store once unlocked.
            Kit.RoundButton(p, r.Settings.CenterX, r.Settings.CenterY, r.Settings.Width, "ui.settings", () => app.OpenOverlay(Overlay.Settings));
            Kit.PetalsPill(p, r.Petals, app.ShownPetals, look.Store ? () => app.OpenOverlay(Overlay.Store) : (Action?)null);
            Kit.SparkleBurst(p, r.Petals.Left + (r.Petals.Height * 0.5f), r.Petals.CenterY, r.Petals.Height, app.SinceRewardBurst);

            SideButtons(p, r, look, meta, app);

            // The level on its wooden plaque, Play, the milestone teaser.
            Kit.WoodSign(p, r.Plaque, PlaytestText.F("common.level", NumberText.Group(level)), T.LevelHome);
            bool available = app.Content.LevelCount > 0;
            TypeStyle play = T.ButtonLarge with { Size = Math.Max(T.ButtonLarge.Size, r.Play.Height * ReferenceHomeRegions.PlayLabelShare / p.Scale) };
            Kit.PrimaryButton(p, r.Play, PlaytestText.T("common.play"), available ? app.StartLevel : (Action?)null, play, decorate: true, playArrow: false, breathe: app.Overlays.Count == 0);
            if (look.Teaser && next.HasValue)
            {
                string teaser = next.Value.WinsToGo == 1 ? PlaytestText.T("home.level_to_reward") : PlaytestText.F("home.levels_to_reward", next.Value.WinsToGo);
                Teaser(p, r.Teaser, teaser);
            }

            // The playtest's own controls, faded at the very bottom under the teaser (not part of the product).
            p.PushAlpha(0.7f);
            DevRow(p, app, DevRowBox(p, r));
            p.PopAlpha();

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Diorama.Top, r.Safe.Right, r.Plaque.Top), toast);
            }
        }

        /// <summary>
        /// The Bloomlings wordmark of Home and the splash: the owner's logo picture when it is embedded (spec 005
        /// pictures.md C1), sized by width (<see cref="ReferenceHomeRegions.LogoPicture"/>) so its letters span 0.8 W as
        /// the reference's; else the wooden letters with leaves and flowers in the logo box (<see cref="Kit.WoodLogo"/>).
        /// </summary>
        public static void Wordmark(IPainter p, ReferenceHomeRegions r)
        {
            string name = PlaytestText.T("home.logo");
            Visuals.Logo(p, LogoBox(p, r), () => Kit.WoodLogo(p, r.Logo, name));
        }

        /// <summary>Where the wordmark goes: the owner's logo picture's box when it is embedded, else the logo box.</summary>
        public static Box LogoBox(IPainter p, ReferenceHomeRegions r)
        {
            string logo = PainterBase.BrandPrefix + OwnerPictures.Logo;
            (int Width, int Height)? size = p.HasSprite(logo) ? p.SpriteSize(logo) : null;
            return size.HasValue && size.Value.Width > 0 && size.Value.Height > 0 ? r.LogoPicture(size.Value.Width, size.Value.Height) : r.Logo;
        }

        /// <summary>Home's look now: what is unlocked (spec 001 FR-058).</summary>
        public static HomeLook Look(DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            bool hasNext = meta.Milestones.Next(meta.Progression.HighestCompletedLevel).HasValue;
            return HomeLook.From(meta.Progression.IsUnlocked, meta.Collection.Count, hasNext, dailyChallengeAvailable: true, freeBoosterOffer: false);
        }

        /// <summary>The outfits Home's heroes wear: the player's, once the Wardrobe is open; else none.</summary>
        public static Func<Family, Outfit>? OutfitsOf(DesignApp app) => Look(app).Wardrobe ? app.Meta.Wardrobe.OutfitOf : (Func<Family, Outfit>?)null;

        /// <summary>
        /// How Home and the splash of <paramref name="scene"/> stand their heroes (<see cref="HomeStage.ShowsHeroes"/>):
        /// over the owner's layered Home when the scene shows the owner's Home garden (pictures.md B1; the splash takes it
        /// until its own, B6, exists) and the fountain's back, the lotus and the fountain's front are embedded
        /// (<see cref="HomeLayers"/>; the shadow and the petals are drawn when present); on the drawn stand-in without an
        /// owner picture; else none.
        /// </summary>
        public static HomeStageKind StageOf(IPainter p, BackdropScene scene)
        {
            string name = OwnerPictures.Resolve(scene, string.Empty, DesignApp.HasBackground(p));
            (int Width, int Height)? size = p.HasSprite(PainterBase.BackgroundPrefix + name) ? p.SpriteSize(PainterBase.BackgroundPrefix + name) : null;
            bool owner = size.HasValue && size.Value.Width > 0 && size.Value.Height > 0;
            bool layered = owner && name == HomeLayers.Back.Name;
            foreach (PictureBox layer in new[] { HomeLayers.FountainBack, HomeLayers.Lotus, HomeLayers.FountainFront })
            {
                layered = layered && p.HasSprite(PainterBase.BackgroundPrefix + layer.Name);
            }

            return !HomeStage.ShowsHeroes(owner, layered) ? HomeStageKind.None : layered ? HomeStageKind.Layered : HomeStageKind.Diorama;
        }

        /// <summary>
        /// The owner's layered Home over the backdrop's garden (spec 005 FR-028, <see cref="HomeLayers"/>), every layer in
        /// the backdrop's own cover box, back to front: the fountain's back; Drop and Bloom, each on its soft shadow; the
        /// lotus again (Bloom stands behind it); Sprig and Twig on their shadows; the fountain's front stones and flowers
        /// over the heroes' feet; the petals drifting down (<see cref="HomeLayers.PetalsAt"/>, twice, a picture height
        /// apart). Each hero shows its pose of <see cref="DesignApp.HomeMotion"/> (<see cref="Visuals.MotionHero"/>), or its
        /// still picture while its frames are missing, in <paramref name="outfitOf"/>'s outfit; the heroes and their shadows
        /// fade in with <paramref name="heroAlpha"/> and the heroes rise by <paramref name="heroRise"/> (the splash).
        /// Returns whether a hero moves.
        /// </summary>
        public static bool LayeredStage(IPainter p, DesignApp app, Func<Family, Outfit>? outfitOf, float heroAlpha = 1f, float heroRise = 0f)
        {
            Box picture = HomeLayers.Cover(new Box(0f, 0f, p.Width, p.Height));
            HomeMotion motion = app.HomeMotion;
            bool moving = false;
            bool lotus = false;
            Layer(p, picture, HomeLayers.FountainBack);
            foreach (Family family in HomeLayers.DrawOrder)
            {
                if (!lotus && !HomeLayers.BehindLotus(family))
                {
                    Layer(p, picture, HomeLayers.Lotus);
                    lotus = true;
                }

                p.PushAlpha(heroAlpha);
                LayerShadow(p, picture, family);
                p.PushTransform(0f, heroRise, 1f, 0f, 0f);
                Box cell = HomeLayers.HeroCell(picture, family);
                Outfit? outfit = outfitOf?.Invoke(family);
                if (Visuals.HasMotion(p, family))
                {
                    Visuals.MotionHero(p, cell, family, motion.Player(family).Pose(app.Now), outfit);
                    moving = true;
                }
                else
                {
                    Visuals.Hero(p, cell, family, outfit);
                }

                p.PopTransform();
                p.PopAlpha();
            }

            if (!lotus)
            {
                Layer(p, picture, HomeLayers.Lotus);
            }

            Layer(p, picture, HomeLayers.FountainFront);
            LayerPetals(p, picture, app.HomeSeconds);
            return moving;
        }

        /// <summary>A layer of the owner's layered Home in its box over the screen.</summary>
        private static void Layer(IPainter p, Box picture, PictureBox layer)
        {
            p.Mark(HomeLayers.SlotOf(layer));
            p.Sprite(PainterBase.BackgroundPrefix + layer.Name, HomeLayers.Place(picture, layer));
        }

        /// <summary>The soft shadow under a hero's feet (<see cref="HomeLayers.ShadowBox"/>), when the picture is embedded.</summary>
        private static void LayerShadow(IPainter p, Box picture, Family family)
        {
            string name = PainterBase.BackgroundPrefix + HomeLayers.Shadow.Name;
            if (!p.HasSprite(name))
            {
                return;
            }

            p.Mark(HomeLayers.SlotOf(HomeLayers.Shadow));
            p.PushAlpha(HomeLayers.ShadowAlpha);
            p.Sprite(name, HomeLayers.ShadowBox(picture, family));
            p.PopAlpha();
        }

        /// <summary>The petals drifting down <paramref name="seconds"/> after Home opened, wrapping round the picture's height.</summary>
        private static void LayerPetals(IPainter p, Box picture, float seconds)
        {
            string name = PainterBase.BackgroundPrefix + HomeLayers.Petals.Name;
            if (!p.HasSprite(name))
            {
                return;
            }

            p.Mark(HomeLayers.SlotOf(HomeLayers.Petals));
            Box petals = HomeLayers.PetalsAt(picture, seconds);
            p.PushAlpha(HomeLayers.PetalsAlpha);
            p.Sprite(name, petals);
            p.Sprite(name, petals.Offset(0f, -picture.Height));
            p.PopAlpha();
        }

        /// <summary>
        /// The touch targets of the animated heroes (each its seam pose's picture box, inside the safe area): a tap makes the
        /// hero react at once (<see cref="HomeMotion.Tap"/>). They are cut clear of every button, pill and plaque of Home
        /// (<see cref="UiBoxes"/>) and of the heroes in front of them, so a hero never takes a tap from them; a part smaller
        /// than the touch minimum takes none.
        /// </summary>
        private static void HeroTaps(IPainter p, ReferenceHomeRegions r, HomeLook look, DesignApp app)
        {
            Box picture = HomeLayers.Cover(new Box(0f, 0f, p.Width, p.Height));
            List<Box> taken = UiBoxes(p, r, look);
            float min = p.U(DesignTokens.Size.TouchMin);
            for (int i = HomeLayers.DrawOrder.Count - 1; i >= 0; i--)
            {
                Family family = HomeLayers.DrawOrder[i];
                if (!Visuals.HasMotion(p, family))
                {
                    continue;
                }

                Box seam = HeroMotion.PictureBox(HomeLayers.HeroCell(picture, family), HeroMotion.Frame(family, MotionClip.Idle, 0));
                Box target = Clear(new Box(Math.Max(seam.Left, r.Safe.Left), Math.Max(seam.Top, r.Safe.Top), Math.Min(seam.Right, r.Safe.Right), Math.Min(seam.Bottom, r.Safe.Bottom)), taken);
                if (target.Width < min || target.Height < min)
                {
                    continue;
                }

                taken.Add(target);
                p.Hit(target, () => app.HomeMotion.Tap(family, app.Now));
            }
        }

        /// <summary>The touch boxes of Home's buttons, pills, plaque and dev row (what the heroes' taps keep clear of).</summary>
        private static List<Box> UiBoxes(IPainter p, ReferenceHomeRegions r, HomeLook look)
        {
            var boxes = new List<Box>
            {
                Kit.Touch(p, r.Settings),
                Kit.Touch(p, r.Petals),
                Kit.Touch(p, r.Plaque),
                Kit.Touch(p, r.Play),
                Kit.Touch(p, r.Teaser),
                DevRowBox(p, r),
            };
            for (int i = 0; i < LeftButtons(look); i++)
            {
                boxes.Add(Kit.Touch(p, r.SideButton(false, i)));
            }

            int right = RightButtons(look);
            for (int i = 0; i < right; i++)
            {
                boxes.Add(Kit.Touch(p, r.SideButton(true, i)));
            }

            if (look.Rank)
            {
                boxes.Add(Kit.Touch(p, RankBox(r, right)));
            }

            return boxes;
        }

        /// <summary>
        /// The largest part of <paramref name="box"/> clear of every obstacle: for each one it overlaps, the larger of the
        /// parts left of, right of, above and below it.
        /// </summary>
        private static Box Clear(Box box, IReadOnlyList<Box> obstacles)
        {
            foreach (Box o in obstacles)
            {
                if (!box.Overlaps(o))
                {
                    continue;
                }

                Box[] parts =
                {
                    new Box(box.Left, box.Top, Math.Min(box.Right, o.Left), box.Bottom),
                    new Box(Math.Max(box.Left, o.Right), box.Top, box.Right, box.Bottom),
                    new Box(box.Left, box.Top, box.Right, Math.Min(box.Bottom, o.Top)),
                    new Box(box.Left, Math.Max(box.Top, o.Bottom), box.Right, box.Bottom),
                };
                Box best = parts[0];
                foreach (Box part in parts)
                {
                    if (part.Width * part.Height > best.Width * best.Height)
                    {
                        best = part;
                    }
                }

                box = best;
            }

            return box;
        }

        /// <summary>
        /// The drawn stand-in's heroes on Home and the splash (spec 005 FR-024, §4.5, §6.4), each in
        /// <paramref name="outfitOf"/>'s outfit, when <see cref="StageOf"/> is <see cref="HomeStageKind.Diorama"/> (no owner
        /// picture): the drawn stage (<see cref="HomeStage.ReferenceDiorama"/>) in the diorama box, the stone ring, Bloom,
        /// Drop and Sprig behind the lotus fountain, Twig in front. Over the owner's layered Home the heroes stand by
        /// <see cref="LayeredStage"/> instead, and over the owner's garden picture alone there are none. Returns whether
        /// heroes were drawn.
        /// </summary>
        public static bool Stage(IPainter p, ReferenceHomeRegions r, BackdropScene scene, Func<Family, Outfit>? outfitOf = null)
        {
            if (StageOf(p, scene) != HomeStageKind.Diorama)
            {
                return false;
            }

            HomeDiorama diorama = HomeStage.ReferenceDiorama(r.Diorama);
            Kit.StonePedestal(p, diorama.Pedestal);
            for (int i = 0; i < diorama.Heroes.Count; i++)
            {
                // The fountain stands between the back row (Bloom, Drop, Sprig) and Twig in front.
                if (i == 3)
                {
                    Kit.LotusFountain(p, diorama.Fountain);
                }

                (Family family, Box box) = diorama.Heroes[i];
                Visuals.Hero(p, box, family, outfitOf?.Invoke(family));
            }

            return true;
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
                Box rank = RankBox(r, right);
                RankRow(p, rank, PlaytestText.T("home.rank_unknown_offline"));
                p.Hit(Kit.Touch(p, rank), () => app.OpenOverlay(Overlay.Leaderboard));
            }
        }

        /// <summary>How many round buttons the left column shows: the Wardrobe, the Collection, the profile avatar.</summary>
        private static int LeftButtons(HomeLook look) => (look.Wardrobe ? 2 : 0) + (look.Collection ? 1 : 0);

        /// <summary>How many round buttons the right column shows: the Daily Challenge, the Store.</summary>
        private static int RightButtons(HomeLook look) => (look.DailyChallenge ? 1 : 0) + (look.Store ? 1 : 0);

        /// <summary>The rank pill under the right column's <paramref name="right"/> buttons.</summary>
        private static Box RankBox(ReferenceHomeRegions r, int right)
        {
            float top = right > 0 ? r.SideButton(true, right - 1).Bottom + (r.W * ReferenceHomeRegions.SideGapShare) : r.SideButton(true, 0).Top;
            return new Box(r.Rank.Left, top, r.Rank.Right, top + r.Rank.Height);
        }

        /// <summary>The playtest's dev row at the very bottom of the safe area.</summary>
        private static Box DevRowBox(IPainter p, ReferenceHomeRegions r) =>
            new Box(r.Safe.Left + (r.W * 0.04f), r.Safe.Bottom - p.U(DesignTokens.Size.TouchMin), r.Safe.Right - (r.W * 0.04f), r.Safe.Bottom);

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
