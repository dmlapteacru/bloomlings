using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;
using Bloomlings.Solver;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>One design board frame, rendered from a scripted state of the real playtest screens.</summary>
    public sealed class Fixture
    {
        public Fixture(int number, string slug, string title, Action<SkiaPainter, string> render)
        {
            Number = number;
            Slug = slug;
            Title = title;
            Render = render;
        }

        public int Number { get; }

        public string Slug { get; }

        public string Title { get; }

        /// <summary>Draws the frame into the painter, with a fresh save in the given folder.</summary>
        public Action<SkiaPainter, string> Render { get; }
    }

    /// <summary>
    /// The scripted states of frames 1–17. They use real levels from the playtest's content and the core's solver for
    /// winning and jamming move lists.
    /// </summary>
    public static class Fixtures
    {
        private const float Frame = 1f / 30f;

        public static IEnumerable<Fixture> All(ContentSet content, string root)
        {
            DesignApp App(string data, bool splash = false) => new DesignApp(data, content, new Silence(), splash);

            yield return new Fixture(1, "splash", "Splash", (p, data) =>
            {
                DesignApp app = App(data, splash: true);
                Run(app, p, 0.8f);
            });

            yield return new Fixture(2, "home-early", "Home (early levels)", (p, data) =>
            {
                // Level 5 with the pictures of the four won levels, as a player has them: the bottom menu shows its five
                // places (spec 005 FR-030), the Shop, the Wardrobe and the Leaderboard with their padlocks.
                DesignApp app = Early(App(data), content, 4);
                Run(app, p, 0.3f);
                Expect(Nav(p, app).Places.Count == 5, "every place shows on an early Home");
                Expect(!app.PlaceOpen(NavPlace.Shop) && !app.PlaceOpen(NavPlace.Wardrobe) && !app.PlaceOpen(NavPlace.Leaderboard), "the Shop, the Wardrobe and the Leaderboard are locked at Level 5");
                Expect(app.PlaceOpen(NavPlace.Home) && app.PlaceOpen(NavPlace.Collection), "Home and the Collection are open at Level 5");
            });

            yield return new Fixture(3, "home-progressed", "Home (progressed)", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                Run(app, p, 0.3f);
            });

            yield return new Fixture(4, "daily-reward", "Daily Reward (popup)", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 87);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(5, "leaderboard", "Leaderboard", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                app.OpenOverlay(Overlay.Leaderboard);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(6, "collection", "Collection", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                app.OpenOverlay(Overlay.Collection);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(7, "gameplay-normal", "Gameplay (normal)", (p, data) => Playing(App(data), p, content, 12, taps: 3));
            yield return new Fixture(8, "gameplay-hard", "Gameplay (hard)", (p, data) => Playing(App(data), p, content, 55, taps: 3));
            yield return new Fixture(9, "gameplay-super-hard", "Gameplay (super hard)", (p, data) => Playing(App(data), p, content, 82, taps: 3));

            yield return new Fixture(10, "jam", "Jam (centered card)", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(27);
                app.Meta.Economy.Grant(300, null);
                app.LoadLevel(28);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult jam = new Solver.Solver().FindJam(Session(content, app.Level!.Level), SolveOptions.Default);
                Play(app, p, jam.Trace, jam.Trace.Count);
                Settle(app, p);
                Run(app, p, 0.6f);
            });

            yield return new Fixture(11, "pause", "Pause menu", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                app.OpenOverlay(Overlay.Pause);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(12, "pod-states", "Pod states", (p, data) => PodStates(p));
            yield return new Fixture(13, "slot-states", "Waiting slot states", (p, data) => SlotStates(p));
            yield return new Fixture(14, "booster-bar", "Booster bar (progressive unlock)", (p, data) => BoosterBar(p));

            yield return new Fixture(15, "win", "Win screen", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(11);
                app.LoadLevel(12);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 12), SolveOptions.Default);
                Play(app, p, win.Trace, win.Trace.Count);
                Settle(app, p);
                // Long enough for the Petals to finish counting up (spec 003 FR-020), still within the confetti.
                Run(app, p, 1.1f);
            });

            yield return new Fixture(16, "milestone", "Milestone win", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(24);
                app.LoadLevel(25);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 25), SolveOptions.Default);
                Play(app, p, win.Trace, win.Trace.Count);
                Settle(app, p);
                Run(app, p, 1f);
                app.Level!.Next();
                Run(app, p, 2.5f);
            });

            yield return new Fixture(17, "store", "Store", (p, data) =>
            {
                // The Store page from Home's Petals "+"; its back and the system back return to Home.
                DesignApp app = Progressed(App(data), content, 49);
                CloseAll(app);
                Run(app, p, 0.1f);
                Box petals = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).Petals;
                Tap(p, Box.FromCenter(petals.Right - petals.Height, petals.CenterY, 1f, 1f));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Home, "Home's Petals \"+\" opens the Store page");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the Store page's back returns to Home");
                app.OpenStore();
                Expect(app.Back() && app.Screen == Design.Screen.Home, "the system back closes the Store page");

                // The bottom menu (FR-030): its Shop opens the page, a tap on the medallion (the active place) does nothing,
                // its Home returns to Home.
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Home, "the bottom menu's Shop opens the Store page");
                Run(app, p, 0.1f);
                Tap(p, Nav(p, app).Medallion);
                Expect(app.Screen == Design.Screen.Store, "a tap on the active place does nothing");
                Tap(p, NavTouch(p, app, NavPlace.Home));
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the bottom menu's Home returns to Home");
                app.OpenStore();
                Run(app, p, 0.5f);
            });
        }

        /// <summary>
        /// Extra review images beyond the board's 17 frames: themes, Settings, a Collection picture, a demo, boosters in use,
        /// the kit, the Store's cosmetics, the Wardrobe, Home's heroes in outfits, and the bottom menu's locked places (the
        /// Store page, the Wardrobe and the Leaderboard card before their unlock).
        /// </summary>
        public static IEnumerable<Fixture> Extras(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false);

            yield return new Fixture(18, "themes", "Extra: background themes", (p, data) =>
            {
                p.BeginFrame();
                (int Level, string Name)[] themes = { (1, "Daylight Garden"), (100, "Pond"), (150, "Orchard"), (200, "Moonlit Garden") };
                float w = p.Width / themes.Length;
                Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
                for (int i = 0; i < themes.Length; i++)
                {
                    var column = new Box(i * w, 0f, (i + 1) * w, p.Height);
                    Client.Gameplay.Themes.BackgroundTheme theme = Client.Gameplay.Themes.ThemeRotation.Default.ThemeFor(themes[i].Level);
                    p.Mark("bg.theme." + theme.Id);

                    // The theme's own gameplay picture (the owner's, pictures.md B2 to B5) as the level shows it, cover-fitted
                    // to the whole screen around the column's middle; the drawn lawn while the picture is missing.
                    var wide = new Box(column.CenterX - (p.Width / 2f), 0f, column.CenterX + (p.Width / 2f), p.Height);
                    Visuals.Background(
                        p,
                        column,
                        OwnerPictures.Gameplay(theme.Id),
                        () =>
                        {
                            p.PushClip(column);
                            p.Backdrop(wide, DesignTokens.Backdrop(theme.Background, theme.Accent), BackdropScene.Gameplay, theme.Id + "/wide");
                            p.PopClip();
                        },
                        (pw, ph) =>
                        {
                            float scale = Math.Max(wide.Width / pw, wide.Height / ph);
                            return Box.FromCenter(wide.CenterX, wide.CenterY, pw * scale, ph * scale);
                        });

                    // The theme's name on a small wooden plaque, and a corner of a board on its lawn: candy tiles in the
                    // stone border (spec 005 §4.1, §4.2).
                    Kit.WoodSign(p, Box.FromCenter(column.CenterX, safe.Top + p.U(90f), w * 0.9f, p.U(84f)), themes[i].Name, T.Caption);
                    float cell = Math.Min(w * 0.22f, p.U(64f));
                    var grid = Box.FromCenter(column.CenterX, p.Height * 0.5f, cell * 3f, cell * 3f);
                    Kit.StoneBorder(p, grid, cell);
                    for (int c = 0; c < 9; c++)
                    {
                        VariantId variant = ThemeTiles[(c + (i * 2)) % ThemeTiles.Length];
                        float x = grid.Left + ((c % 3) * cell);
                        float y = grid.Top + ((c / 3) * cell);
                        Kit.CandyTile(p, new Box(x, y, x + cell, y + cell).Inset(cell * 0.02f), variant, TileStyle.Board);
                    }
                }
            });

            yield return new Fixture(19, "settings", "Extra: Settings", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                app.OpenOverlay(Overlay.Pause);
                app.OpenOverlay(Overlay.Settings);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(20, "collection-detail", "Extra: Collection picture", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                app.OpenOverlay(Overlay.Collection);
                app.CollectionDetail = 2;
                Run(app, p, 0.5f);
            });

            yield return new Fixture(21, "demo", "Extra: booster demo", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(2);
                app.LoadLevel(3);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(22, "shuffle", "Extra: Shuffle in use", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 55), SolveOptions.Default);
                Play(app, p, win.Trace, 2);
                Run(app, p, 0.4f);
                app.Level!.UseBooster(BoosterKind.Shuffle, new UseShuffle(), free: true);
                Run(app, p, 0.15f);
            });

            yield return new Fixture(23, "bloom-burst", "Extra: Bloom Burst in use", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                VariantId variant = app.Level!.Session.Definition.Pods[0].Variant;
                app.Level.UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant), free: true);
                Run(app, p, 0.2f);
            });
            yield return new Fixture(24, "bloomlings", "Extra: Bloomlings", (p, data) => Bloomlings(p));
            yield return new Fixture(26, "store-cosmetics", "Extra: Store cosmetics (the Wardrobe look)", (p, data) =>
            {
                // The Store page from the Wardrobe's Petals "+"; its back returns to the Wardrobe, then the page again on
                // its Cosmetics tab.
                DesignApp app = Progressed(App(data), content, 49);
                CloseAll(app);
                app.OpenWardrobe();
                Run(app, p, 0.1f);
                Box petals = ScreenLayout.ReferenceWardrobe(p.Width, p.Height, p.Insets).Petals;
                Tap(p, Box.FromCenter(petals.Right - petals.Height, petals.CenterY, 1f, 1f));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Wardrobe, "the Wardrobe's Petals \"+\" opens the Store page");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Wardrobe, "the Store page's back returns to the Wardrobe");
                Run(app, p, 0.1f);
                Tap(p, Box.FromCenter(petals.Right - petals.Height, petals.CenterY, 1f, 1f));
                Expect(app.Screen == Design.Screen.Store, "the Wardrobe's Petals \"+\" opens the Store page again");
                Run(app, p, 0.1f);
                Box tabs = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Tabs;
                Tap(p, Box.FromCenter(tabs.Right - (tabs.Width / 4f), tabs.CenterY, 1f, 1f));
                Expect(app.StoreTab == 1, "a tap on the Cosmetics tab shows the cosmetics");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(25, "kit", "Extra: reference look kit", (p, data) => KitSheet(p));
            yield return new Fixture(27, "wardrobe", "Extra: Wardrobe (spec 005 FR-025)", (p, data) =>
            {
                // From the bottom menu's Wardrobe (FR-030); its Shop opens the Store page over it (whose back returns
                // here) and its Leaderboard the card over Home. Then a tap on the starter cap's card puts it on Sprig, so
                // the hero and the green worn card show an outfit.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                Run(app, p, 0.1f);
                Expect(Nav(p, app).Places.Count == 5, "every place shows on a progressed Home");
                Tap(p, NavTouch(p, app, NavPlace.Wardrobe));
                Expect(app.Screen == Design.Screen.Wardrobe, "the bottom menu's Wardrobe opens the Wardrobe");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Wardrobe, "the Wardrobe's Shop opens the Store page over it");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Wardrobe, "the Store page's back returns to the Wardrobe");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Home && app.IsOpen(Overlay.Leaderboard), "the Wardrobe's Leaderboard opens the card over Home");
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Expect(app.Screen == Design.Screen.Home && app.IsOpen(Overlay.Collection), "Home's Collection opens the card");
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Wardrobe));
                Expect(app.Screen == Design.Screen.Wardrobe, "the bottom menu's Wardrobe opens the Wardrobe again");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceWardrobe(p.Width, p.Height, p.Insets).Card(1));
                Expect(app.Meta.Wardrobe.EquippedFor(Family.Sprig, Client.Meta.Wardrobe.CosmeticKind.Hat)?.Id == "hat.sprout_cap", "a tap on an owned item's card wears it");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(28, "home-outfits", "Extra: Home's animated heroes in outfits (spec 005 FR-028)", (p, data) =>
            {
                // The progressed Home with an outfit on every hero (a hat each, Sprig's speckles, Bloom's wink, Twig's
                // sparkle trail), so the frames' outfit layers show: the hats turn with the heads and the skin keeps to
                // each frame. A tap on Play still plays; a tap on Twig's body makes it react at once, shown mid-reaction.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                foreach (string id in new[] { "hat.sprout_cap", "hat.straw_hat", "hat.flower_crown", "hat.acorn_cap", "skin.speckles", "trail.petal_sparkle", "expression.wink" })
                {
                    app.Meta.Save.Cosmetics.Owned.Add(id);
                }

                foreach ((Family family, string id) in new[]
                {
                    (Family.Sprig, "hat.sprout_cap"), (Family.Bloom, "hat.flower_crown"), (Family.Drop, "hat.straw_hat"), (Family.Twig, "hat.acorn_cap"),
                    (Family.Sprig, "skin.speckles"), (Family.Twig, "trail.petal_sparkle"), (Family.Bloom, "expression.wink"),
                })
                {
                    Expect(app.Meta.Wardrobe.Equip(family, id), "an owned " + id + " can be worn by " + family);
                }

                Run(app, p, 0.5f);
                Tap(p, ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).Play);
                Expect(app.Screen == Design.Screen.Level, "a tap on Play plays, whatever hero stands behind it");
                app.GoHome();
                CloseAll(app);
                Run(app, p, 0.5f);
                Box picture = HomeLayers.Cover(new Box(0f, 0f, p.Width, p.Height));
                Box twig = HeroMotion.PictureBox(HomeLayers.HeroCell(picture, Family.Twig), HeroMotion.Frame(Family.Twig, MotionClip.Idle, 0));
                Expect(!app.HomeMotion.Player(Family.Twig).Busy(app.Now), "Twig idles before the tap");
                p.Dispatch(twig.CenterX, twig.Top + (twig.Height * 0.7f));
                Expect(app.HomeMotion.Player(Family.Twig).Busy(app.Now), "a tap on Twig makes it react at once");
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "a tap on a hero opens nothing");
                Run(app, p, 0.6f);
            });
            yield return new Fixture(29, "store-locked", "Extra: locked Store page (spec 005 FR-030)", (p, data) =>
            {
                // Level 5: the bottom menu's Shop, locked, still opens the Store page, which says from which level it is
                // available (the roadmap's L12), the Shop raised in the medallion; its back returns to Home.
                DesignApp app = Early(App(data), content, 4);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Home, "the locked Shop opens the Store page");
                Expect(app.UnlockLevel(NavPlace.Shop) == 12, "the Store opens from the roadmap's level 12");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Available from level 12"), "the locked Store page says its level");
                Expect(Nav(p, app).Active == NavPlace.Shop, "the locked Store page raises the Shop");
                Tap(p, ScreenLayout.LockedPage(p.Width, p.Height, p.Insets).Header.Back);
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the locked Store page's back returns to Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store, "the locked Shop opens the Store page again");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(30, "wardrobe-locked", "Extra: locked Wardrobe (spec 005 FR-030)", (p, data) =>
            {
                // Level 15: the Store, the Leaderboard and the Collection are open, the Wardrobe not yet (L40). The menu's
                // Wardrobe opens the Wardrobe locked, saying its level; its Shop opens the open Store page over it, whose
                // back returns here, and so does its Petals "+".
                DesignApp app = Progressed(App(data), content, 14);
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Wardrobe));
                Expect(app.Screen == Design.Screen.Wardrobe && !app.PlaceOpen(NavPlace.Wardrobe), "the locked Wardrobe place opens the Wardrobe");
                Expect(app.UnlockLevel(NavPlace.Wardrobe) == 40, "the Wardrobe opens from the roadmap's level 40");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Available from level 40"), "the locked Wardrobe says its level");
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Wardrobe, "the locked Wardrobe's Shop opens the Store page over it");
                Run(app, p, 0.1f);
                Expect(!Shows(p, "Available from level 12"), "the Store page is open at Level 15");
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Wardrobe, "the Store page's back returns to the locked Wardrobe");
                Run(app, p, 0.1f);
                Box petals = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets).Header.Petals;
                Tap(p, Box.FromCenter(petals.Right - petals.Height, petals.CenterY, 1f, 1f));
                Expect(app.Screen == Design.Screen.Store, "the locked Wardrobe's Petals \"+\" opens the Store page");
                Expect(app.Back() && app.Screen == Design.Screen.Wardrobe, "the system back returns to the locked Wardrobe");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(31, "leaderboard-locked", "Extra: locked Leaderboard card (spec 005 FR-030)", (p, data) =>
            {
                // A new profile on Home (Level 1): the menu's Collection opens its card saying it is available from level 2
                // (its first picture comes with Level 1's win); the Leaderboard's card says level 10 (the roadmap's).
                DesignApp app = App(data);
                app.GoHome();
                CloseAll(app);
                Run(app, p, 0.1f);
                Expect(app.Meta.CurrentLevel == 1 && !app.PlaceOpen(NavPlace.Collection), "a new profile's Collection is locked");
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Expect(app.Screen == Design.Screen.Home && app.IsOpen(Overlay.Collection), "the locked Collection opens its card");
                Run(app, p, 0.5f);
                Expect(Shows(p, "Available from level 2"), "the locked Collection card says level 2");
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Home && app.IsOpen(Overlay.Leaderboard), "the locked Leaderboard opens its card");
                Expect(app.UnlockLevel(NavPlace.Leaderboard) == 10, "the Leaderboard opens from the roadmap's level 10");
                Run(app, p, 0.5f);
                Expect(Shows(p, "Available from level 10"), "the locked Leaderboard card says level 10");
            });
        }

        /// <summary>Draws frames for <paramref name="seconds"/>: animations advance as on a device; the last frame stays.</summary>
        private static void Run(DesignApp app, SkiaPainter p, float seconds)
        {
            int frames = Math.Max(1, (int)(seconds / Frame));
            for (int i = 0; i < frames; i++)
            {
                p.Now += Frame;
                p.BeginFrame();
                app.Draw(p, Frame);
            }
        }

        /// <summary>Plays the animation until it has shown the rules state (at most a minute of game time).</summary>
        private static void Settle(DesignApp app, SkiaPainter p)
        {
            for (int i = 0; i < 1800 && app.Level != null && !app.Level.Animator.Settled; i++)
            {
                p.Now += Frame;
                p.BeginFrame();
                app.Draw(p, Frame);
            }
        }

        /// <summary>The bottom menu as the app's screen draws it (its five places, the screen's place active).</summary>
        private static BottomNavRegions Nav(SkiaPainter p, DesignApp app) => HomeScreen.Nav(p, app.ActivePlace);

        /// <summary>Whether the last drawn frame shows <paramref name="text"/> as one of its texts.</summary>
        private static bool Shows(SkiaPainter p, string text) => p.Texts.Any(t => t.Text == text);

        /// <summary>The touch box of a place of the bottom menu as the app's screen draws it.</summary>
        private static Box NavTouch(SkiaPainter p, DesignApp app, NavPlace place)
        {
            BottomNavRegions nav = Nav(p, app);
            int index = nav.IndexOf(place);
            Expect(index >= 0, "the bottom menu shows " + place);
            return nav.Touch(index);
        }

        /// <summary>A tap in the middle of <paramref name="box"/> on the last drawn frame, as a finger would.</summary>
        private static void Tap(SkiaPainter p, Box box) => p.Dispatch(box.CenterX, box.CenterY);

        /// <summary>Fails the frame (the preview reports it) when a scripted interaction did not do what it should.</summary>
        private static void Expect(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException("expected: " + what);
            }
        }

        private static void CloseAll(DesignApp app)
        {
            while (app.Overlays.Count > 0)
            {
                app.CloseOverlay();
            }
        }

        private static void CloseDemo(DesignApp app)
        {
            if (app.Level?.Demo != null)
            {
                app.Level.CloseDemo();
            }
        }

        /// <summary>
        /// An early profile at Level <paramref name="highest"/>+1 with the pictures of the levels it won in its Collection, as
        /// a player has them (the skip collects none), on Home with no card open.
        /// </summary>
        private static DesignApp Early(DesignApp app, ContentSet content, int highest)
        {
            app.Meta.SkipTo(highest);
            for (int level = 1; level <= highest; level++)
            {
                if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null)
                {
                    app.Meta.Collection.Add(definition, level);
                }
            }

            app.GoHome();
            CloseAll(app);
            return app;
        }

        /// <summary>A profile at Level <paramref name="highest"/>+1 with some Petals and pictures in its Collection, on Home.</summary>
        private static DesignApp Progressed(DesignApp app, ContentSet content, int highest)
        {
            app.Meta.SkipTo(highest);
            app.Meta.Economy.Grant(1240 - app.Meta.Economy.Petals, null);
            foreach (int level in new[] { 11, 12, 14, 16, 17, 18 })
            {
                if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null)
                {
                    app.Meta.Collection.Add(definition, level);
                }
            }

            app.GoHome();
            return app;
        }

        private static LevelSession Session(ContentSet content, int level)
        {
            LevelDefinition definition = content.GetLevel(level);
            return LevelSession.Load(definition, content.GetPicture(definition.Picture), new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget));
        }

        /// <summary>A level in play: a few moves of its solution, caught while Bloomlings walk.</summary>
        private static void Playing(DesignApp app, SkiaPainter p, ContentSet content, int level, int taps)
        {
            app.Meta.SkipTo(level - 1);
            app.Meta.Economy.Grant(200, null);
            app.LoadLevel(level);
            CloseDemo(app);
            Run(app, p, 0.1f);
            SolveResult win = new Solver.Solver().Solve(Session(content, level), SolveOptions.Default);
            Play(app, p, win.Trace, taps);
            Run(app, p, 0.45f);
        }

        /// <summary>Plays commands as a player would: a tap, then a moment of animation.</summary>
        private static void Play(DesignApp app, SkiaPainter p, IReadOnlyList<Command> commands, int count)
        {
            for (int i = 0; i < count && i < commands.Count; i++)
            {
                LevelScreen level = app.Level!;
                switch (commands[i])
                {
                    case TapPod tap:
                        level.Tap(tap.PodId);
                        break;
                    case UseExtraSlot:
                        level.UseBooster(BoosterKind.ExtraSlot, commands[i], free: true);
                        break;
                    case UseShuffle:
                        level.UseBooster(BoosterKind.Shuffle, commands[i], free: true);
                        break;
                    case UseReturn:
                        level.UseBooster(BoosterKind.Return, commands[i], free: true);
                        break;
                    case UseBloomBurst:
                        level.UseBooster(BoosterKind.BloomBurst, commands[i], free: true);
                        break;
                }

                Run(app, p, i == count - 1 ? Frame : 0.35f);
            }
        }

        // ---- Component sheets (frames 12–14) ----

        private static void Sheet(SkiaPainter p, string title, out Box body)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            p.FillRound(safe.Inset(p.U(30f)), p.U(56f), C.SurfacePanel.WithAlpha(0.93f));
            p.Text(title, safe.CenterX, safe.Top + p.U(120f), T.TitleCaps, C.TextPrimary);
            body = new Box(safe.Left + p.U(70f), safe.Top + p.U(220f), safe.Right - p.U(70f), safe.Bottom - p.U(70f));
        }

        private static Box[] Grid(Box body, IPainter p, int count, int columns, float cellHeight)
        {
            var cells = new Box[count];
            float w = body.Width / columns;
            for (int i = 0; i < count; i++)
            {
                float x = body.Left + ((i % columns) * w);
                float y = body.Top + ((i / columns) * cellHeight);
                cells[i] = new Box(x, y, x + w, y + cellHeight);
            }

            return cells;
        }

        /// <summary>
        /// A component sheet in the reference look (frames 12–14, spec 005 §4.1, §6.1): the gameplay tray's parchment panel
        /// over the lawn from under the title to the bottom of the screen, and the title in <c>ink.brown</c>. The sheet's
        /// pieces take their sizes from the reference gameplay regions (<see cref="Reference"/>), so they look as in play.
        /// </summary>
        private static void TraySheet(SkiaPainter p, string title, out Box body)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            float w = safe.Width;
            Kit.WoodSign(p, Box.FromCenter(safe.CenterX, safe.Top + (0.08f * w), 0.56f * w, 0.115f * w), title, T.LevelPill, SignDecor.Ivy);
            var tray = new Box(0f, safe.Top + (0.17f * w), p.Width, p.Height);
            LevelScreen.TrayPanel(p, tray, 0.06f * w, w, Array.Empty<float>());
            body = new Box(safe.Left + (0.04f * w), tray.Top + (0.03f * w), safe.Right - (0.04f * w), safe.Bottom - (0.02f * w));
        }

        /// <summary>The reference gameplay regions of this frame's shape, for the sizes of decks, plates and booster boxes.</summary>
        private static ReferenceGameplayRegions Reference(IPainter p, int stacks = 4, int slots = 5) =>
            ScreenLayout.ReferenceGameplay(p.Width, p.Height, p.Insets, stacks, slots);

        /// <summary>A state's label on a component sheet: <c>ink.brown_soft</c> captions.</summary>
        private static void StateLabel(IPainter p, string label, float cx, float cy, float maxWidth) =>
            p.Text(label, cx, cy, T.Caption, C.InkBrownSoft, maxWidth);

        /// <summary>A separator line across a component sheet, as between the tray's rows.</summary>
        private static void SheetLine(IPainter p, Box body, float y) =>
            LevelScreen.Separator(p, Box.FromCenter(body.CenterX, y, body.Width, Math.Max(2f, body.Width * 0.0055f)));

        /// <summary>
        /// Frame 12: the pods of the tray's grid (spec 005 FR-021, the owner's rule of 2026-10-03) in every state at their
        /// size in play (exposed, pressed, locked, mystery; waiting under the exposed one, also as a hidden mystery and
        /// locked; "+N" more below; an emptied stack), and four columns as the tray shows them, one after another and never
        /// on each other, two exposed pods connected and a group across rows marked by rings.
        /// </summary>
        private static void PodStates(SkiaPainter p)
        {
            TraySheet(p, "Pod states", out Box body);
            ReferenceGameplayRegions g = Reference(p);
            Box exposedSize = g.Pod(0, 0);
            Box waitingSize = g.Pod(0, 1);
            (VariantId? Variant, int Count, bool Locked) Pod(VariantId? variant, int count, bool locked = false) => (variant, count, locked);
            (string Label, VariantId? Variant, int Count, PodLook Look, bool Waiting, int More, bool Empty)[] states =
            {
                ("Exposed", VariantId.Leaf, 18, PodLook.Exposed, false, 0, false),
                ("Pressed", VariantId.Leaf, 18, PodLook.Pressed, false, 0, false),
                ("Locked", VariantId.Acorn, 20, PodLook.Locked, false, 0, false),
                ("Mystery", null, 14, PodLook.Exposed, false, 0, false),
                ("Waiting", VariantId.Flower, 9, PodLook.Exposed, true, 0, false),
                ("Waiting mystery", null, 7, PodLook.Exposed, true, 0, false),
                ("Waiting, locked", VariantId.Water, 4, PodLook.Locked, true, 0, false),
                ("More below (+4)", VariantId.Dew, 10, PodLook.Exposed, true, 4, false),
                ("Emptied stack", null, 0, PodLook.Exposed, false, 0, true),
            };

            float rowHeight = exposedSize.Height + (0.085f * g.W);
            Box[] cells = Grid(body, p, states.Length, 3, rowHeight);
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                StateLabel(p, states[i].Label, cell.CenterX, cell.Top + (0.03f * g.W), cell.Width);
                Box size = states[i].Waiting ? waitingSize : exposedSize;
                Box box = Box.FromCenter(cell.CenterX, cell.Top + (0.065f * g.W) + (exposedSize.Height / 2f), size.Width, size.Height);
                if (states[i].Empty)
                {
                    PodPainter.EmptyColumn(p, box);
                    continue;
                }

                var chip = PodChip.In(box);
                Kit.Pod(p, chip, states[i].Variant, states[i].Count, states[i].Look, states[i].Waiting);
                if (states[i].More > 0)
                {
                    PodPainter.MoreBadge(p, chip, states[i].More);
                }
            }

            // Four columns as in play: the exposed pods on the top row, the next ones under them.
            float rowTop = cells[cells.Length - 1].Bottom + (0.01f * g.W);
            if (rowTop + (0.07f * g.W) + g.PodRow.Height > body.Bottom)
            {
                return;
            }

            SheetLine(p, body, rowTop);
            StateLabel(p, "The tray in play: each stack a column, two pods connected", body.CenterX, rowTop + (0.035f * g.W), body.Width);
            float dy = rowTop + (0.07f * g.W) - g.PodRow.Top;
            var columns = new[]
            {
                new[] { Pod(VariantId.Leaf, 12), Pod(VariantId.Moss, 5), Pod(VariantId.Wood, 9), Pod(VariantId.Flower, 6) },
                new[] { Pod(VariantId.Flower, 8), Pod(VariantId.Water, 3), Pod(VariantId.Leaf, 6), Pod(VariantId.Dew, 4) },
                new[] { Pod(VariantId.Water, 14), Pod(null, 4), Pod(VariantId.Flower, 7), Pod(VariantId.Acorn, 5, locked: true) },
                new[] { Pod(VariantId.Acorn, 6), Pod(VariantId.VioletBud, 8), Pod(VariantId.Dew, 2) },
            };
            int[] totals = { 4, 7, 4, 3 };
            var frames = new Box[Math.Min(columns.Length, g.Columns.Count)][];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = PodPainter.DrawColumn(p, g, i, dy, columns[i], totals[i], PodLook.Exposed);
            }

            if (frames.Length >= 4)
            {
                PodPainter.Link(p, frames[1][0], frames[2][0]);
                PodPainter.LinkRing(p, frames[0][1], PodPainter.LinkPalette[1]);
                PodPainter.LinkRing(p, frames[3][2], PodPainter.LinkPalette[1]);
            }
        }

        /// <summary>The variants shown on the themes frame's board corners.</summary>
        private static readonly VariantId[] ThemeTiles = { VariantId.Leaf, VariantId.Flower, VariantId.Water, VariantId.Acorn, VariantId.Moss, VariantId.VioletBud, VariantId.Dew, VariantId.Wood };

        /// <summary>
        /// Every variant (spec 005 §3.1, spec 004): its candy tile in the board and sticker styles next to its 2D character
        /// in each mood (happy, asleep, worried, blank), on parchment over the lawn, and the four 3D heroes of the meta
        /// screens below, each on its stone pedestal.
        /// </summary>
        private static void Bloomlings(SkiaPainter p)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            Box sheet = safe.Inset(p.U(24f));
            Kit.Paper(p, sheet, p.U(48f), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            Kit.WoodSign(p, Box.FromCenter(sheet.CenterX, sheet.Top + p.U(84f), p.U(470f), p.U(104f)), "Bloomlings", T.Title, SignDecor.Ivy);
            var body = new Box(sheet.Left + p.U(36f), sheet.Top + p.U(170f), sheet.Right - p.U(36f), sheet.Bottom - p.U(30f));

            VariantInfo[] variants = VariantCatalog.Default.All.ToArray();
            int columns = 2 + CharacterArt.Moods.Count;
            float w = body.Width / columns;
            float heroes = Math.Min(p.U(330f), body.Height * 0.22f);
            float row = Math.Min(p.U(126f), (body.Height - heroes - p.U(60f)) / variants.Length);
            string[] headers = { "board", "sticker", "happy", "asleep", "worried", "blank" };
            for (int c = 0; c < columns; c++)
            {
                p.Text(headers[c], body.Left + ((c + 0.5f) * w), body.Top + p.U(16f), T.Caption, C.InkBrownSoft, w);
            }

            for (int v = 0; v < variants.Length; v++)
            {
                float cy = body.Top + p.U(50f) + ((v + 0.5f) * row);
                float size = Math.Min(row * 0.86f, w * 0.8f);
                Kit.CandyTile(p, Box.FromCenter(body.Left + (0.5f * w), cy, size, size), variants[v].Id, TileStyle.Board);
                Kit.CandyTile(p, Box.FromCenter(body.Left + (1.5f * w), cy, size, size), variants[v].Id, TileStyle.Sticker);
                for (int m = 0; m < CharacterArt.Moods.Count; m++)
                {
                    Visuals.Character(p, Box.FromCenter(body.Left + ((m + 2.5f) * w), cy, size, size), variants[v].Id, CharacterArt.Moods[m]);
                }
            }

            // The four heroes, each on its stone pedestal, feet on the pedestal's top.
            float hw = body.Width / CharacterArt.Families.Count;
            for (int f = 0; f < CharacterArt.Families.Count; f++)
            {
                float cx = body.Left + ((f + 0.5f) * hw);
                var pedestal = Box.FromCenter(cx, body.Bottom - (heroes * 0.13f), hw * 0.78f, heroes * 0.26f);
                Kit.StonePedestal(p, pedestal);
                float top = HomeStage.PedestalTop(pedestal).CenterY;
                Visuals.Hero(p, HomeStage.Figure(cx, top, heroes * 0.86f), CharacterArt.Families[f], null);
            }
        }

        /// <summary>
        /// The reference look's kit on parchment (spec 005 T009), like the reference's "Target Variants" and "UI Elements"
        /// strips: the candy tiles in both styles, the stone border, the pedestal with rays and petals, the wooden
        /// signs, the buttons and their pressed state, the round buttons and pills, booster tiles, pods, slots and the jam
        /// choices.
        /// </summary>
        private static void KitSheet(SkiaPainter p)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            Box sheet = safe.Inset(p.U(20f));
            Kit.Paper(p, sheet, p.U(48f), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            Box body = sheet.Inset(p.U(44f), p.U(34f));
            float[] heights = { 70f, 150f, 330f, 150f, 170f, 140f, 220f, 230f, 250f };
            float gap = Math.Max(0f, (body.Height - p.U(heights.Sum())) / (heights.Length - 1));
            var rows = new Box[heights.Length];
            float y = body.Top;
            for (int i = 0; i < heights.Length; i++)
            {
                rows[i] = new Box(body.Left, y, body.Right, y + p.U(heights[i]));
                y = rows[i].Bottom + gap;
            }

            p.Text("Reference look kit", rows[0].CenterX, rows[0].CenterY, T.Title, C.InkBrown, rows[0].Width, look: TextLook.Plain(C.InkBrown));

            // The eight launch variants as sticker tiles (pods, slots, the jam row), with their names.
            VariantInfo[] launch = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Launch).ToArray();
            Box[] stickers = ScreenLayout.Row(new Box(rows[1].Left, rows[1].Top, rows[1].Right, rows[1].Top + p.U(100f)), launch.Length, p.U(16f), p.U(100f), square: true);
            for (int i = 0; i < launch.Length; i++)
            {
                Kit.CandyTile(p, stickers[i], launch[i].Id, TileStyle.Sticker);
                p.Text(launch[i].Id.Key.Replace('_', ' '), stickers[i].CenterX, stickers[i].Bottom + p.U(26f), T.Caption, C.InkBrownSoft, stickers[i].Width + p.U(14f));
            }

            // The board style in a 4 × 2 grid inside the stone border, centered in its row (a Garden Entry has no picture).
            float cell = p.U(78f);
            float rim = cell * 0.48f;
            var grid = new Box(rows[2].Left + rim, rows[2].CenterY - cell, rows[2].Left + rim + (cell * 4f), rows[2].CenterY + cell);
            Kit.StoneBorder(p, grid, cell);
            for (int i = 0; i < launch.Length; i++)
            {
                float cx = grid.Left + ((i % 4) * cell);
                float cy = grid.Top + ((i / 4) * cell);
                Kit.CandyTile(p, new Box(cx, cy, cx + cell, cy + cell), launch[i].Id, TileStyle.Board);
            }

            // The expansion variants, and a hero on the stone pedestal in the win's light.
            VariantInfo[] expansion = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Expansion).ToArray();
            float ex = grid.Right + rim + p.U(40f);
            for (int i = 0; i < expansion.Length; i++)
            {
                float sx = ex + ((i % 2) * p.U(100f));
                float sy = rows[2].Top + p.U(10f) + ((i / 2) * p.U(130f));
                var tile = new Box(sx, sy, sx + p.U(84f), sy + p.U(84f));
                Kit.CandyTile(p, tile, expansion[i].Id, TileStyle.Sticker);
                p.Text(expansion[i].Id.Key, tile.CenterX, tile.Bottom + p.U(22f), T.Caption, C.InkBrownSoft, tile.Width + p.U(14f));
            }

            var stage = new Box(ex + p.U(210f), rows[2].Top, rows[2].Right, rows[2].Bottom);
            var pedestal = Box.FromCenter(stage.CenterX, stage.Bottom - p.U(50f), Math.Min(stage.Width, p.U(230f)), p.U(100f));
            Kit.LightRays(p, pedestal.CenterX, pedestal.Top - p.U(70f), p.U(200f), p.Now);
            Box top = Kit.StonePedestal(p, pedestal);
            float hero = p.U(190f);
            Visuals.Hero(p, new Box(top.CenterX - (hero * 0.45f), top.CenterY - hero, top.CenterX + (hero * 0.45f), top.CenterY + (hero * 0.06f)), Family.Bloom, null);

            // The drawn Home stage's lotus fountain (ui.fountain), shown on Home only while the owner's picture is missing.
            Kit.LotusFountain(p, Box.FromCenter(pedestal.Left + (pedestal.Width * 0.08f), pedestal.Bottom - (pedestal.Height * 0.12f), pedestal.Width * 0.42f, pedestal.Width * 0.15f));
            Kit.FallingPetals(p, stage, p.Now);

            // Wooden signs: the gameplay level with ivy, the win title with flowers.
            Kit.WoodSign(p, new Box(rows[3].Left + p.U(50f), rows[3].CenterY - p.U(46f), rows[3].Left + p.U(320f), rows[3].CenterY + p.U(46f)), PlaytestText.F("common.level", 88), T.LevelPill, SignDecor.Ivy);
            Kit.WoodSign(p, new Box(rows[3].Right - p.U(530f), rows[3].CenterY - p.U(60f), rows[3].Right - p.U(30f), rows[3].CenterY + p.U(60f)), "Level complete!", T.Title, SignDecor.Flowers);

            // Buttons: Play in its wooden rim, the same pressed, the orange Next.
            Box[] buttons = Spread(rows[4], new[] { 360f, 290f, 250f }, new[] { 150f, 124f, 124f }, p);
            Kit.PrimaryButton(p, buttons[0], PlaytestText.T("common.play"), () => { }, T.ButtonLarge, decorate: true);
            p.Finger = (buttons[1].CenterX, buttons[1].CenterY);
            Kit.PrimaryButton(p, buttons[1], PlaytestText.T("common.play"), () => { });
            p.Finger = null;
            Kit.PrimaryButton(p, buttons[2], PlaytestText.T("common.next"), () => { }, set: GardenLook.Orange);

            // Round and squircle buttons and the speed pill.
            Box[] round = Spread(rows[5], new[] { 124f, 210f, 124f, 104f, 124f }, new[] { 124f, 112f, 124f, 104f, 124f }, p);
            Kit.RoundButton(p, round[0].CenterX, round[0].CenterY, round[0].Width, "ui.pause", () => { }, squircle: true);
            Kit.SpeedPill(p, round[1], "2×", () => { });
            Kit.RoundButton(p, round[2].CenterX, round[2].CenterY, round[2].Width, "ui.settings", () => { });
            Kit.RoundButton(p, round[3].CenterX, round[3].CenterY, round[3].Width, "ui.close", () => { });
            Kit.RoundButton(p, round[4].CenterX, round[4].CenterY, round[4].Width, GardenLook.BackGlyph.ShapeId, () => { });

            // Booster tiles (charges, price, selected, disabled), the Petals pill, a cost pill and a count badge.
            Box[] tray = Spread(new Box(rows[6].Left, rows[6].Top + p.U(20f), rows[6].Right, rows[6].Top + p.U(180f)), new[] { 140f, 140f, 140f, 140f, 280f }, new[] { 146f, 146f, 146f, 146f, 160f }, p);
            Kit.BoosterTile(p, tray[0], "extra_slot", new BoosterTileState(3, 30, false, true, true), () => { });
            Kit.BoosterTile(p, tray[1], "shuffle", new BoosterTileState(0, 30, false, true, true), () => { });
            Kit.BoosterTile(p, tray[2], "return", new BoosterTileState(1, 50, true, true, true), () => { });
            Kit.BoosterTile(p, tray[3], "bloom_burst", new BoosterTileState(0, 60, false, true, false), null);
            Kit.PetalsPill(p, new Box(tray[4].Left, tray[4].Top, tray[4].Right - p.U(20f), tray[4].Top + p.U(76f)), 2450, () => { }, align: 0f);
            Kit.CostPill(p, new Box(tray[4].Left, tray[4].Bottom - p.U(60f), tray[4].Left + p.U(170f), tray[4].Bottom), Cost.Charges(2));
            Kit.CountBadge(p, tray[4].Right - p.U(40f), tray[4].Bottom - p.U(30f), p.U(56f), "3");

            // Pods of the tray's grid (the exposed one, one waiting under it) and Waiting Slots (working, empty, stuck).
            Box[] pods = Spread(new Box(rows[7].Left, rows[7].Top + p.U(24f), rows[7].Right, rows[7].Bottom), new[] { 206f, 206f, 132f, 132f, 132f }, new[] { 124f, 96f, 150f, 150f, 150f }, p);
            Kit.Pod(p, PodChip.In(pods[0]), VariantId.Leaf, 12, PodLook.Exposed, waiting: false);
            Kit.Pod(p, PodChip.In(pods[1]), VariantId.Flower, 8, PodLook.Exposed, waiting: true);
            Kit.SlotPlate(p, pods[2], SlotPlateState.Working, VariantId.Water, 3);
            Kit.SlotPlate(p, pods[3], SlotPlateState.Empty);
            Kit.SlotPlate(p, pods[4], SlotPlateState.Stuck, VariantId.Acorn, 4);

            // The jam's choices with their cost pills, and Restart.
            Box[] choices = Spread(rows[8], new[] { 300f, 300f, 270f }, new[] { 240f, 240f, 110f }, p);
            Kit.ChoiceButton(p, choices[0], GardenLook.Green, GardenLook.BoosterIcon("extra_slot"), PlaytestText.T("booster.extra_slot"), Cost.Petals(30), () => { });
            Kit.ChoiceButton(p, choices[1], GardenLook.Blue, GardenLook.BoosterIcon("return"), PlaytestText.T("booster.return"), Cost.Free, () => { });
            Kit.SecondaryButton(p, choices[2], PlaytestText.T("common.restart"), () => { }, "ui.restart");
        }

        /// <summary>Boxes of the given sizes (reference units) spread evenly across a row and centered in its height.</summary>
        private static Box[] Spread(Box row, float[] widths, float[] heights, IPainter p)
        {
            float total = p.U(widths.Sum());
            float gap = (row.Width - total) / (widths.Length + 1);
            var boxes = new Box[widths.Length];
            float x = row.Left + gap;
            for (int i = 0; i < widths.Length; i++)
            {
                boxes[i] = new Box(x, row.CenterY - (p.U(heights[i]) / 2f), x + p.U(widths[i]), row.CenterY + (p.U(heights[i]) / 2f));
                x += p.U(widths[i]) + gap;
            }

            return boxes;
        }

        /// <summary>
        /// Frame 13: every Waiting Slot state (spec 002 FR-013, spec 005 §3.7) on plates of their size in play (§6.1), and
        /// the slot row as it shows in play.
        /// </summary>
        private static void SlotStates(SkiaPainter p)
        {
            TraySheet(p, "Waiting slot states", out Box body);
            ReferenceGameplayRegions g = Reference(p);
            Box plateSize = g.Slots[0];
            (string Label, SlotLook Look, bool Locked, bool Danger, bool Extra)[] states =
            {
                ("Empty", new SlotLook(), false, false, false),
                ("Working", new SlotLook { PodId = "a", Variant = VariantId.Leaf, Count = 12, InFlight = 2 }, false, false, false),
                ("Stuck", new SlotLook { PodId = "b", Variant = VariantId.Water, Count = 9 }, false, false, false),
                ("Locked", new SlotLook(), true, false, false),
                ("Danger (4/5)", new SlotLook(), false, true, false),
                ("Extra slot", new SlotLook(), false, false, true),
                ("Mystery", new SlotLook { PodId = "c", Count = 7, InFlight = 1 }, false, false, false),
                ("Extra slot, working", new SlotLook { PodId = "d", Variant = VariantId.Acorn, Count = 5, InFlight = 1 }, false, false, true),
                ("Return target", new SlotLook { PodId = "e", Variant = VariantId.Flower, Count = 4, InFlight = 1 }, false, false, false),
            };

            // Plates a little larger than in play, so each state reads on the sheet.
            float scale = 1.25f;
            float rowHeight = (plateSize.Height * scale) + (0.11f * g.W);
            Box[] cells = Grid(body, p, states.Length, 3, rowHeight);
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                var plate = Box.FromCenter(cell.CenterX, cell.Top + (0.08f * g.W) + (plateSize.Height * scale / 2f), plateSize.Width * scale, plateSize.Height * scale);
                if (states[i].Label == "Return target")
                {
                    SlotPainter.TargetGlow(p, plate);
                }

                SlotPainter.Slot(p, plate, states[i].Look, states[i].Locked, states[i].Danger, states[i].Extra, 10f);
                StateLabel(p, states[i].Label, cell.CenterX, cell.Top + (0.035f * g.W), cell.Width);
            }

            // The row as it shows in play: four working pods (one stuck) and the last free slot, in the regions' plates.
            float top = cells[cells.Length - 1].Bottom + (0.02f * g.W);
            if (top + (0.08f * g.W) + plateSize.Height > body.Bottom)
            {
                return;
            }

            SheetLine(p, body, top);
            StateLabel(p, "The row in play", body.CenterX, top + (0.04f * g.W), body.Width);
            float dy = top + (0.08f * g.W) - g.Slots[0].Top;
            SlotLook[] looks =
            {
                new SlotLook { PodId = "r1", Variant = VariantId.Leaf, Count = 3, InFlight = 1 },
                new SlotLook { PodId = "r2", Variant = VariantId.Flower, Count = 2, InFlight = 1 },
                new SlotLook { PodId = "r3", Variant = VariantId.Water, Count = 1 },
                new SlotLook { PodId = "r4", Variant = VariantId.Acorn, Count = 4, InFlight = 2 },
                new SlotLook(),
            };
            for (int i = 0; i < looks.Length && i < g.Slots.Count; i++)
            {
                SlotPainter.Slot(p, g.Slots[i].Offset(0f, dy), looks[i], false, i == looks.Length - 1, false, 10f);
            }

            // With the sixth slot of Extra Slot, the row narrows its plates.
            ReferenceGameplayRegions six = Reference(p, slots: 6);
            float sixTop = top + (0.1f * g.W) + plateSize.Height;
            if (sixTop + (0.08f * g.W) + six.Slots[0].Height > body.Bottom)
            {
                return;
            }

            SheetLine(p, body, sixTop);
            StateLabel(p, "With the extra slot", body.CenterX, sixTop + (0.04f * g.W), body.Width);
            float dy6 = sixTop + (0.08f * g.W) - six.Slots[0].Top;
            for (int i = 0; i < six.Slots.Count; i++)
            {
                SlotLook look = i < looks.Length - 1 ? looks[i] : new SlotLook();
                SlotPainter.Slot(p, six.Slots[i].Offset(0f, dy6), look, false, false, i == six.Slots.Count - 1, 10f);
            }
        }

        /// <summary>
        /// Frame 14: the booster row of the tray (spec 002 FR-014, spec 005 §3.7, §6.1) as it unlocks, step by step, in the
        /// four cream boxes of their size in play, and every tile state (charges, price, selected, disabled).
        /// </summary>
        private static void BoosterBar(SkiaPainter p)
        {
            TraySheet(p, "Booster bar", out Box body);
            ReferenceGameplayRegions g = Reference(p);
            (string Label, int Shown)[] steps = { ("Level 1–2 (hidden)", 0), ("Level 3", 1), ("Level 4", 2), ("Level 6", 3), ("Level 9+", 4) };
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] charges = { 2, 2, 1, 0 };

            // The boxes at their size in play, unless the rows do not fit the sheet: then all of them a little smaller.
            float w = g.W;
            float label = 0.065f * w;
            float gap = 0.07f * w;
            float pill = 0.05f * w;
            float side = g.Boosters[0].Height;
            float needed = label + ((steps.Length - 1) * (label + side + gap)) + (label + side + pill);
            float k = Math.Min(1f, body.Height / needed);
            label *= k;
            gap *= k;
            side *= k;
            Box Place(int i, float cy) => Box.FromCenter(g.Boosters[i].CenterX, cy, g.Boosters[i].Width * k, side);

            float top = body.Top;
            for (int s = 0; s < steps.Length; s++)
            {
                if (s > 0)
                {
                    SheetLine(p, body, top - (gap / 2f));
                }

                p.TextLeft(steps[s].Label, body.Left, top + (label * 0.45f), T.Caption, C.InkBrownSoft);
                if (steps[s].Shown == 0)
                {
                    top += label + (gap / 2f);
                    continue;
                }

                float cy = top + label + (side / 2f);
                for (int i = 0; i < steps[s].Shown; i++)
                {
                    BoosterBarPainter.Tile(p, Place(i, cy), ids[i], new BoosterTileState(charges[i], 60, false, true, true), null);
                }

                top += label + side + gap;
            }

            // Every tile state of spec 003 FR-031: charges, price, selected (Return), disabled.
            var states = new (string Label, string Id, BoosterTileState State)[]
            {
                ("Charges", "extra_slot", new BoosterTileState(3, 40, false, true, true)),
                ("Price", "shuffle", new BoosterTileState(0, 40, false, true, true)),
                ("Selected", "return", new BoosterTileState(1, 50, true, true, true)),
                ("Disabled", "bloom_burst", new BoosterTileState(0, 60, false, true, false)),
            };
            SheetLine(p, body, top - (gap / 2f));
            float stateCy = top + label + (side / 2f);
            for (int i = 0; i < states.Length; i++)
            {
                Box place = Place(i, stateCy);
                StateLabel(p, states[i].Label, place.CenterX, top + (label * 0.45f), place.Width * 1.3f);
                BoosterBarPainter.Tile(p, place, states[i].Id, states[i].State, null);
            }
        }
    }
}
