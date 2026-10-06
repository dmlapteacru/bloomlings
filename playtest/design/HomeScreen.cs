using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Profile;
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
    /// The splash of frame 1 (spec 002 FR-016 as amended by spec 005 FR-039, contracts/look.md §6.13): the lotus loader.
    /// The logo and the lotus stand on the parchment while the ring of petals round the lotus fills, "Loading..." under
    /// it; once it is full the first screen comes up under it and the lotus iris opens on it (<see cref="DesignApp"/>). It
    /// shows while the game starts and never waits for a tap.
    /// </summary>
    public static class SplashScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            float progress = LotusIris.SplashProgress(app.Now, 1f);
            LotusPainter.Draw(p, LotusIris.Splash(app.Now, progress), PlaytestText.T("splash.loading"), splash: true);
        }
    }

    /// <summary>
    /// Home of frames 2 and 3 (spec 002 FR-017, spec 001 FR-058) in the reference layout (spec 005 FR-024,
    /// contracts/look.md §6.4, <see cref="ScreenLayout.ReferenceHome"/>):
    /// <list type="bullet">
    /// <item><description>Always shown: the header row (the owner's request of 2026-10-04): Settings at the top left,
    /// the large Petals pill in the middle with the Play button's leaves and flower on its top-left and bottom-right
    /// corners, and the profile avatar at the top right (a tap says "Profile coming soon" until the profile page
    /// comes); the wooden logo across the top, the diorama (the owner's layered Home with the four animated heroes
    /// around the lotus fountain, spec 005 FR-028, <see cref="LayeredStage"/>; or the drawn stand-in's four still
    /// heroes around the lotus fountain on the stone; in their outfits once the Wardrobe is open), the level on a wooden plaque and the big Play button in its
    /// wooden rim. A tap on an animated hero makes it react (<see cref="HomeMotion.Tap"/>).</description></item>
    /// <item><description>The promo scenes under the logo (spec 005 FR-032, <see cref="Promo"/>): No Ads at the left from
    /// level 1 while Remove Ads is not owned (a tap opens the Remove Ads card), the Daily Reward at the right from its
    /// unlock (a tap opens its card), each idling and in turn playing its attention sequence while it calls.</description></item>
    /// <item><description>Shown once unlocked: the Daily Challenge as a cream round button at the right, under the Daily
    /// Reward's scene; "N levels to reward" with the gift as a parchment pill under Play.</description></item>
    /// <item><description>The bottom menu (spec 005 FR-030, <see cref="Kit.BottomNav"/>): the wooden bar with its five
    /// places always shown (Shop, Wardrobe, Home, Leaderboard, Collection, each a page; a locked one with a padlock badge,
    /// its page saying from which level it is available), Home in the raised medallion; it replaced Home's Store, Wardrobe
    /// and Collection side buttons and the rank pill (<see cref="DesignApp.Navigate"/>).</description></item>
    /// </list>
    /// Play always continues Level N, and there is no level map. The playtest's skip and reset controls, not part of the
    /// product, moved from Home's bottom into the Settings card opened from Home (<see cref="MenuCards.Settings"/>) when the
    /// bottom menu took that band, so Home stands as the Unity client's.
    /// </summary>
    public static class HomeScreen
    {
        /// <summary>
        /// The height the playtest's dev row keeps out of Home's layout (<see cref="ScreenLayout.ReferenceHome"/>'s
        /// <c>bottomReserve</c>): none. Since the bottom menu (FR-030) took the band under the teaser, the row lives in the
        /// Settings card opened from Home (<see cref="MenuCards.Settings"/>), so the plaque, Play and the teaser stand where
        /// the Unity client has them.
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

            // The diorama (no logo since the owner's tuning of 2026-10-05, spec 005 FR-036; the splash keeps its wordmark).
            // Over the owner's layered Home: the fountain and the four animated heroes around the lotus (a tap makes one
            // react; the buttons drawn after keep their taps), and the drifting petals when Settings switched them on.
            // Else the drawn stand-in's four still heroes around the lotus fountain (none over the owner's garden picture
            // alone) and a few drawn petals drifting through the garden. The heroes wear their outfits once the Wardrobe
            // is open.
            bool heroes;
            app.HomeMoving = false;
            if (StageOf(p, BackdropScene.Home) == HomeStageKind.Layered)
            {
                app.HomeMoving = LayeredStage(p, app, OutfitsOf(app));
                HeroTaps(p, r, look, app);
                heroes = true;
            }
            else
            {
                heroes = Stage(p, r, BackdropScene.Home, OutfitsOf(app));
            }

            if (heroes && look.Hero)
            {
                p.Mark("char.hero.home");
            }

            // The header row (the owner's request of 2026-10-04): Settings at the left; the large Petals pill in the middle
            // with its flowered corners, its "+" to the Store once unlocked; the profile avatar at the right.
            Kit.RoundButton(p, r.Settings.CenterX, r.Settings.CenterY, r.Settings.Width, "ui.settings", () => app.OpenOverlay(Overlay.Settings));
            PetalsPillParts petals = Kit.PetalsPill(p, r.Petals, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null, align: 0.5f);
            Kit.SparkleBurst(p, petals.Lotus.CenterX, petals.Lotus.CenterY, petals.Pill.Height, app.SinceRewardBurst);
            ProfileLook profile = meta.Wardrobe.Profile;
            AvatarItem avatar = meta.Profile.Avatar;
            Kit.Avatar(p, r.Avatar, avatar, profile.Frame, profile.Badge, app.OpenProfile, OutfitsOf(app)?.Invoke(avatar.Family));

            // The promo scenes under the header (spec 005 FR-032, FR-036): No Ads at the left, the Daily Reward at the right,
            // each on its cream plate. They idle all the time, so Home keeps redrawing under a card too.
            if (Promos(p, r, app))
            {
                app.HomeMoving = true;
            }

            SideButtons(p, r, look, app);

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

            // The bottom menu, Home in its medallion, the locked places with their padlocks (FR-030).
            Kit.BottomNav(p, Nav(p, NavPlace.Home), look, app.Navigate);

            // The Remove Ads card shows its own toasts over the scrim (MetaCards.RemoveAds).
            string? toast = app.HomeToastText;
            if (toast != null && !app.IsOpen(Overlay.RemoveAds))
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Diorama.Top, r.Safe.Right, r.Plaque.Top), toast);
            }
        }

        /// <summary>
        /// Whether Home shows a promo scene (spec 005 FR-032): No Ads from level 1 while Remove Ads is not owned (never
        /// bought in the playtest); the Daily Reward from its unlock (L7,
        /// <see cref="Client.Meta.DailyReward.DailyRewardService.IsUnlocked"/>), idling once today's reward is claimed.
        /// </summary>
        public static bool ShowsPromo(DesignApp app, PromoScene scene) =>
            scene == PromoScene.NoAds ? !app.Meta.Save.Purchases.RemoveAds : app.Meta.DailyReward.IsUnlocked;

        /// <summary>
        /// Home's promo scenes in their boxes (<see cref="ReferenceHomeRegions.Promo"/>), each a touch target that presses
        /// as a whole: No Ads opens the Remove Ads card at every level (<see cref="Overlay.RemoveAds"/>; the Store keeps its
        /// own row), the Daily Reward its card (<see cref="Overlay.DailyReward"/>). No Ads always calls for attention, the
        /// Daily Reward while it can be claimed. Returns whether a scene moves (its pictures are embedded).
        /// </summary>
        private static bool Promos(IPainter p, ReferenceHomeRegions r, DesignApp app)
        {
            bool moving = false;
            foreach (PromoScene scene in new[] { PromoScene.NoAds, PromoScene.Daily })
            {
                if (!ShowsPromo(app, scene))
                {
                    continue;
                }

                Box box = r.Promo(scene);
                bool calling = scene == PromoScene.NoAds || app.Meta.DailyReward.CanClaim;
                Action open = scene == PromoScene.NoAds ? () => app.OpenOverlay(Overlay.RemoveAds) : () => app.OpenOverlay(Overlay.DailyReward);
                float depth = Kit.Press(p, box, true);
                Kit.Squash(p, box, depth);
                Plate(p, box, r.W, depth);
                moving |= Promo(p, scene, box, app.PromoSeconds, calling, r.W);
                p.PopTransform();
                p.Hit(Kit.Touch(p, box), open);
            }

            return moving;
        }

        /// <summary>
        /// A promo scene in <paramref name="box"/> (<see cref="HomePromo.SceneBox"/>) at <paramref name="seconds"/> since Home
        /// opened (spec 005 FR-032, <see cref="HomePromo.Layers"/>): the owner's layers back to front, each its whole canvas
        /// in its box, scaled and turned about its pivot, at its alpha, with the label on the stand's plaque right after the
        /// stand. While the scene's pictures are missing, the label on a wooden sign in the box instead. Also the Remove Ads
        /// card's scene (<paramref name="calling"/> false: idling). On Home (<paramref name="shadowW"/>, the safe width W)
        /// every picture's soft shadow lies under all of them (<see cref="HomePromo.ShadowOf"/>). Returns whether it moves
        /// (the pictures are drawn).
        /// </summary>
        public static bool Promo(IPainter p, PromoScene scene, Box box, float seconds, bool calling, float shadowW = 0f)
        {
            p.Mark(HomePromo.Slot(scene));
            string label = PlaytestText.T(HomePromo.LabelKey(scene));
            if (!HasPromoPictures(p, scene))
            {
                Kit.WoodSign(p, Box.FromCenter(box.CenterX, box.CenterY, box.Width * 0.9f, box.Height * 0.42f), label, T.LevelPill);
                return false;
            }

            IReadOnlyList<PromoLayer> layers = HomePromo.Layers(scene, box, seconds, calling);
            if (shadowW > 0f)
            {
                foreach (PromoLayer layer in layers)
                {
                    PromoShadow(p, layer, shadowW);
                }
            }

            for (int i = 0; i < layers.Count; i++)
            {
                PromoLayer layer = layers[i];
                if (layer.Alpha > 0f)
                {
                    p.PushAlpha(layer.Alpha);
                    p.PushRotate(layer.Rotation, layer.X, layer.Y);
                    p.PushSquash(layer.ScaleX, layer.ScaleY, layer.X, layer.Y);
                    p.Sprite(PainterBase.DecorPrefix + layer.Picture, layer.Box);
                    p.PopTransform();
                    p.PopTransform();
                    p.PopAlpha();
                }

                if (i == 0)
                {
                    // The label on the stand's wooden plaque, in the wooden sign's embossed brown letters.
                    Box plaque = HomePromo.Plaque(box, scene);
                    float scale = plaque.Height * HomePromo.LabelShare / p.U(T.LevelPill.Size);
                    p.Text(label, plaque.CenterX, plaque.CenterY, T.LevelPill, C.InkBrown, plaque.Width * 0.92f, scale, GardenLook.SignLetters(C.InkBrown));
                }
            }

            return true;
        }

        /// <summary>
        /// A promo picture's soft shadow (spec 005 FR-036): its silhouette blurred (<see cref="HomePromo.Shadow"/>), posed as
        /// the picture and dropped a little.
        /// </summary>
        private static void PromoShadow(IPainter p, PromoLayer layer, float w)
        {
            string name = PainterBase.DecorPrefix + layer.Picture;
            (byte[] Alpha, int Width, int Height)? mask = layer.Alpha > 0f ? p.SpriteAlpha(name) : null;
            if (mask == null)
            {
                return;
            }

            (PromoLayer shadow, float pad, float blur) = HomePromo.ShadowOf(layer, w);
            (byte[] alpha, int mw, int mh) = mask.Value;
            p.PushAlpha(shadow.Alpha);
            p.PushRotate(shadow.Rotation, shadow.X, shadow.Y);
            p.PushSquash(shadow.ScaleX, shadow.ScaleY, shadow.X, shadow.Y);
            p.Picture(HomePromo.ShadowKey(layer.Picture), shadow.Box, (pw, ph) => HomePromo.Shadow(alpha, mw, mh, pw, ph, shadow.Width, shadow.Height, pad, blur));
            p.PopTransform();
            p.PopTransform();
            p.PopAlpha();
        }

        /// <summary>
        /// The cream plate a promo scene stands on (spec 005 FR-036, <see cref="HomePromo.PlateBox"/>): its soft shadow, then
        /// the round buttons' cushion, pressing with the scene.
        /// </summary>
        private static void Plate(IPainter p, Box scene, float w, float depth)
        {
            p.Mark("ui.button.round");
            Box plate = HomePromo.PlateBox(scene);
            (Box shadow, float pad, float blur) = HomePromo.PlateShadowOf(plate, w);
            p.Picture(HomePromo.PlateShadowKey, shadow, (pw, ph) => HomePromo.PlateShadow(pw, ph, shadow, plate, pad, blur));
            Kit.IconFace(p, plate, GardenLook.White, HomePromo.PlateRadius(plate), depth);
        }

        /// <summary>Whether every picture of a promo scene is embedded (<see cref="HomePromo.Pictures"/>).</summary>
        private static bool HasPromoPictures(IPainter p, PromoScene scene)
        {
            string slot = HomePromo.Slot(scene);
            foreach (string picture in HomePromo.Pictures)
            {
                if (OwnerPictures.SlotOf(picture) == slot && !p.HasSprite(PainterBase.DecorPrefix + picture))
                {
                    return false;
                }
            }

            return true;
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

        /// <summary>
        /// Home's look now: what is unlocked (spec 001 FR-058). The Collection also counts as open from its level
        /// (<see cref="BottomNav.CollectionLevel"/>) while it is still empty: a player reaches it with level 1's picture, but
        /// the dev row's skips collect none, and its page would otherwise say "Available from level 2" at any level.
        /// </summary>
        public static HomeLook Look(DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            bool hasNext = meta.Milestones.Next(meta.Progression.HighestCompletedLevel).HasValue;
            HomeLook look = HomeLook.From(meta.Progression.IsUnlocked, meta.Collection.Count, hasNext, dailyChallengeAvailable: true, freeBoosterOffer: false);
            return look with { Collection = look.Collection || meta.CurrentLevel >= BottomNav.CollectionLevel };
        }

        /// <summary>The bottom menu's regions with its five places (<see cref="BottomNav.Order"/>) and <paramref name="active"/> in the medallion.</summary>
        public static BottomNavRegions Nav(IPainter p, NavPlace active) =>
            ScreenLayout.BottomNav(p.Width, p.Height, p.Insets, BottomNav.Order, active);

        /// <summary>The outfits Home's heroes wear: the player's, once the Wardrobe is open; else none.</summary>
        public static Func<Family, Outfit>? OutfitsOf(DesignApp app) => Look(app).Wardrobe ? app.Meta.Wardrobe.OutfitOf : (Func<Family, Outfit>?)null;

        /// <summary>
        /// How Home and the splash of <paramref name="scene"/> stand their heroes (<see cref="HomeStage.ShowsHeroes"/>):
        /// over the owner's layered Home when the scene shows the owner's Home garden (pictures.md B1; the splash takes it
        /// until its own, B6, exists) and the fountain's back, the lotus and the fountain's front are embedded
        /// (<see cref="HomeLayers.IsLayered"/>; the shadow and the petals are drawn when present); on the drawn stand-in without an
        /// owner picture; else none.
        /// </summary>
        public static HomeStageKind StageOf(IPainter p, BackdropScene scene)
        {
            string name = OwnerPictures.Resolve(scene, string.Empty, DesignApp.HasBackground(p));
            (int Width, int Height)? size = p.HasSprite(PainterBase.BackgroundPrefix + name) ? p.SpriteSize(PainterBase.BackgroundPrefix + name) : null;
            bool owner = size.HasValue && size.Value.Width > 0 && size.Value.Height > 0;
            bool layered = owner && HomeLayers.IsLayered(name, picture => p.HasSprite(PainterBase.BackgroundPrefix + picture));

            return !HomeStage.ShowsHeroes(owner, layered) ? HomeStageKind.None : layered ? HomeStageKind.Layered : HomeStageKind.Diorama;
        }

        /// <summary>
        /// The owner's layered Home over the backdrop's garden (spec 005 FR-028, <see cref="HomeLayers"/>), every layer in
        /// the backdrop's own cover box, back to front: the fountain's back; Drop and Bloom, each on its soft shadow; the
        /// lotus again (Bloom stands behind it); Sprig and Twig on their shadows; the fountain's front stones and flowers
        /// over the heroes' feet. Each hero shows its pose of <see cref="DesignApp.HomeMotion"/> (<see cref="Visuals.MotionHero"/>), or its
        /// still picture while its frames are missing, in <paramref name="outfitOf"/>'s outfit; the heroes and their shadows
        /// fade in with <paramref name="heroAlpha"/> and the heroes rise by <paramref name="heroRise"/> (the splash).
        /// Returns whether a hero moves.
        /// </summary>
        public static bool LayeredStage(IPainter p, DesignApp app, Func<Family, Outfit>? outfitOf, float heroAlpha = 1f, float heroRise = 0f)
        {
            Box picture = HomeLayers.Stage(new Box(0f, 0f, p.Width, p.Height));
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

        /// <summary>
        /// The touch targets of the animated heroes (each its seam pose's picture box, inside the safe area): a tap makes the
        /// hero react at once (<see cref="HomeMotion.Tap"/>). They are cut clear of every button, pill and plaque of Home
        /// (<see cref="UiBoxes"/>) and of the heroes in front of them, so a hero never takes a tap from them; a part smaller
        /// than the touch minimum takes none.
        /// </summary>
        private static void HeroTaps(IPainter p, ReferenceHomeRegions r, HomeLook look, DesignApp app)
        {
            Box picture = HomeLayers.Stage(new Box(0f, 0f, p.Width, p.Height));
            List<Box> taken = UiBoxes(p, r, look, app);
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

        /// <summary>
        /// The touch boxes of Home's buttons, pills, plaque and promo scenes, and the bottom menu from its top down (what the
        /// heroes' taps keep clear of).
        /// </summary>
        private static List<Box> UiBoxes(IPainter p, ReferenceHomeRegions r, HomeLook look, DesignApp app)
        {
            var boxes = new List<Box>
            {
                Kit.Touch(p, r.Settings),
                Kit.Touch(p, r.Petals),
                Kit.Touch(p, r.Avatar),
                Kit.Touch(p, r.Plaque),
                Kit.Touch(p, r.Play),
                Kit.Touch(p, r.Teaser),
                new Box(r.Safe.Left, r.NavTop, r.Safe.Right, r.Safe.Bottom),
            };
            if (look.DailyChallenge)
            {
                boxes.Add(Kit.Touch(p, r.Daily));
            }

            foreach (PromoScene scene in new[] { PromoScene.NoAds, PromoScene.Daily })
            {
                if (ShowsPromo(app, scene))
                {
                    boxes.Add(Kit.Touch(p, r.Promo(scene)));
                }
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
        /// The cream round side button of the unlocked Daily Challenge (§6.4), the right column's first: the sun. The Store,
        /// the Wardrobe, the Collection and the rank are the bottom menu's places since the owner's request of 2026-10-04
        /// (FR-030).
        /// </summary>
        private static void SideButtons(IPainter p, ReferenceHomeRegions r, HomeLook look, DesignApp app)
        {
            if (look.DailyChallenge)
            {
                // The playtest has no Daily Challenge content.
                RoundSide(p, r.Daily, () => app.HomeToast("Daily Challenge: not in the playtest yet"), face =>
                {
                    p.Mark("ui.sun");
                    float g = face.Width * 0.72f;
                    OutlinedShape(p, "ui.sun", Box.FromCenter(face.CenterX, face.CenterY, g, g), C.GardenFlowerCenter, C.GardenFlowerCenterLine);
                });
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

        /// <summary>A glyph in a saturated color over its darker outline (the gift, the trophy), as the reference's icons.</summary>
        private static void OutlinedShape(IPainter p, string shapeId, Box box, Rgba fill, Rgba line)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shapeId);
            p.ShapeOf(shapeId + "/line/0.07", (x, y) => sdf(x, y) - 0.07f, box, line);
            p.Shape(shapeId, box, fill);
        }
    }
}
