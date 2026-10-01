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
                DesignApp app = App(data);
                app.Meta.SkipTo(4);
                app.GoHome();
                CloseAll(app);
                Run(app, p, 0.3f);
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

            yield return new Fixture(10, "jam", "Jam (bottom sheet)", (p, data) =>
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
                DesignApp app = Progressed(App(data), content, 49);
                CloseAll(app);
                app.OpenOverlay(Overlay.Store);
                Run(app, p, 0.5f);
            });
        }

        /// <summary>Extra review images beyond the board's 17 frames: themes, Settings, a Collection picture, a demo, boosters in use.</summary>
        public static IEnumerable<Fixture> Extras(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false);

            yield return new Fixture(18, "themes", "Extra: background themes", (p, data) =>
            {
                p.BeginFrame();
                (int Level, string Name)[] themes = { (1, "Daylight Garden"), (100, "Pond"), (150, "Orchard"), (200, "Moonlit Garden") };
                float w = p.Width / themes.Length;
                for (int i = 0; i < themes.Length; i++)
                {
                    var column = new Box(i * w, 0f, (i + 1) * w, p.Height);
                    Client.Gameplay.Themes.BackgroundTheme theme = Client.Gameplay.Themes.ThemeRotation.Default.ThemeFor(themes[i].Level);
                    p.Mark("bg.theme." + theme.Id);
                    p.PushClip(column);
                    p.Backdrop(new Box(column.CenterX - (p.Width / 2f), 0f, column.CenterX + (p.Width / 2f), p.Height), DesignTokens.Backdrop(theme.Background, theme.Accent), BackdropScene.Gameplay, theme.Id + "/wide");
                    p.PopClip();
                    Box label = Box.FromCenter(column.CenterX, p.Height * 0.5f, w * 0.9f, p.U(90f));
                    p.FillRound(label, label.Height / 2f, C.SurfacePanel.WithAlpha(0.9f));
                    p.Text(themes[i].Name, label.CenterX, label.CenterY, T.Caption, C.TextPrimary, label.Width * 0.9f);
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

        private static void PodStates(SkiaPainter p)
        {
            Sheet(p, "Pod states", out Box body);
            (string Label, PodInfo Pod, PodLook Look)[] states =
            {
                ("Exposed", new PodInfo("a", VariantId.Leaf, 18, PodLocation.Tray, -1, false, false, null), PodLook.Exposed),
                ("Next in stack", new PodInfo("b", VariantId.Water, 12, PodLocation.Tray, -1, false, false, null), PodLook.Next),
                ("Pressed", new PodInfo("c", VariantId.Leaf, 18, PodLocation.Tray, -1, false, false, null), PodLook.Pressed),
                ("Locked", new PodInfo("d", VariantId.Acorn, 20, PodLocation.Tray, -1, true, false, null), PodLook.Locked),
                ("Mystery", new PodInfo("e", null, 14, PodLocation.Tray, -1, false, true, null), PodLook.Exposed),
                ("Connected", new PodInfo("f", VariantId.Flower, 12, PodLocation.Tray, -1, false, false, "g"), PodLook.Exposed),
            };
            Box[] cells = Grid(body, p, states.Length, 3, p.U(440f));
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                float size = Math.Min(cell.Width * 0.7f, p.U(240f));
                Box pod = Box.FromCenter(cell.CenterX, cell.Top + p.U(80f) + (size / 2f), size, size);
                if (states[i].Label == "Connected")
                {
                    // Two linked pods side by side.
                    Box a = Box.FromCenter(cell.CenterX - (size * 0.36f), pod.CenterY, size * 0.66f, size * 0.66f);
                    Box b = Box.FromCenter(cell.CenterX + (size * 0.36f), pod.CenterY, size * 0.66f, size * 0.66f);
                    PodPainter.Pod(p, a, states[i].Pod, PodLook.Exposed, null);
                    PodPainter.Pod(p, b, new PodInfo("g", VariantId.Water, 8, PodLocation.Tray, -1, false, false, "g"), PodLook.Exposed, null);
                    p.Mark("pod.link");
                    float y = a.Top + (a.Height * 0.42f);
                    p.Line(a.Right - p.U(8f), y, b.Left + p.U(8f), y, p.U(10f), C.StateLink);
                }
                else
                {
                    PodPainter.Pod(p, pod, states[i].Pod, states[i].Look, null);
                }

                p.Text(states[i].Label, cell.CenterX, cell.Top + p.U(40f), T.Caption, C.TextSecondary);
            }
        }

        /// <summary>
        /// Every variant character (spec 004): its board tile, then each mood (happy, asleep, worried, blank), and the four
        /// 3D heroes of the meta screens below.
        /// </summary>
        private static void Bloomlings(SkiaPainter p)
        {
            Sheet(p, "Bloomlings", out Box body);
            VariantInfo[] variants = VariantCatalog.Default.All.ToArray();
            int columns = 1 + CharacterArt.Moods.Count;
            float w = body.Width / columns;
            float heroes = Math.Min(p.U(300f), body.Height * 0.2f);
            float row = Math.Min(p.U(110f), (body.Height - heroes - p.U(70f)) / variants.Length);
            string[] headers = { "tile", "happy", "asleep", "worried", "blank" };
            for (int c = 0; c < columns; c++)
            {
                p.Text(headers[c], body.Left + ((c + 0.5f) * w), body.Top + p.U(20f), T.Caption, C.TextSecondary);
            }

            for (int v = 0; v < variants.Length; v++)
            {
                float cy = body.Top + p.U(60f) + ((v + 0.5f) * row);
                float size = Math.Min(row * 0.92f, w * 0.8f);
                BoardPainter.CharacterBlock(p, Box.FromCenter(body.Left + (0.5f * w), cy, size, size), variants[v].Id);
                for (int m = 0; m < CharacterArt.Moods.Count; m++)
                {
                    Visuals.Character(p, Box.FromCenter(body.Left + ((m + 1.5f) * w), cy, size, size), variants[v].Id, CharacterArt.Moods[m]);
                }
            }

            float hw = body.Width / CharacterArt.Families.Count;
            for (int f = 0; f < CharacterArt.Families.Count; f++)
            {
                Visuals.Hero(p, Box.FromCenter(body.Left + ((f + 0.5f) * hw), body.Bottom - (heroes / 2f), hw * 0.92f, heroes), CharacterArt.Families[f], null);
            }
        }

        private static void SlotStates(SkiaPainter p)
        {
            Sheet(p, "Waiting slot states", out Box body);
            (string Label, SlotLook Look, bool Locked, bool Danger, bool Extra)[] states =
            {
                ("Empty", new SlotLook(), false, false, false),
                ("Working", new SlotLook { PodId = "a", Variant = VariantId.Leaf, Count = 12, InFlight = 2 }, false, false, false),
                ("Stuck", new SlotLook { PodId = "b", Variant = VariantId.Water, Count = 9 }, false, false, false),
                ("Locked", new SlotLook(), true, false, false),
                ("Danger (4/5)", new SlotLook(), false, true, false),
                ("Extra slot", new SlotLook(), false, false, true),
            };
            Box[] cells = Grid(body, p, states.Length, 3, p.U(400f));
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                float size = Math.Min(cell.Width * 0.6f, p.U(200f));
                SlotPainter.Slot(p, Box.FromCenter(cell.CenterX, cell.Top + p.U(80f) + (size / 2f), size, size), states[i].Look, states[i].Locked, states[i].Danger, states[i].Extra, 10f);
                p.Text(states[i].Label, cell.CenterX, cell.Top + p.U(40f), T.Caption, C.TextSecondary);
            }
        }

        private static void BoosterBar(SkiaPainter p)
        {
            Sheet(p, "Booster bar", out Box body);
            (string Label, int Shown)[] steps = { ("Level 1–2 (hidden)", 0), ("Level 3", 1), ("Level 4", 2), ("Level 6", 3), ("Level 9+", 4) };
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] charges = { 2, 2, 1, 0 };
            float rowHeight = p.U(240f);
            for (int s = 0; s < steps.Length; s++)
            {
                var row = new Box(body.Left, body.Top + (s * rowHeight), body.Right, body.Top + (s * rowHeight) + rowHeight);
                p.TextLeft(steps[s].Label, row.Left, row.Top + p.U(30f), T.Caption, C.TextSecondary);
                var bar = new Box(row.Left, row.Top + p.U(56f), row.Right, row.Top + p.U(56f) + p.U(DesignTokens.Size.BoosterButton));
                Box[] places = ScreenLayout.Row(bar, 4, p.U(24f), p.U(220f), square: false);
                for (int i = 0; i < steps[s].Shown; i++)
                {
                    BoosterBarPainter.Tile(p, BoosterBarPainter.Fit(p, places[i], bar), ids[i], new BoosterTileState(charges[i], 60, false, true, true), null);
                }
            }

            // Every tile state of spec 003 FR-031: charges, price, selected (Return), disabled.
            var states = new (string Label, string Id, BoosterTileState State)[]
            {
                ("Charges", "extra_slot", new BoosterTileState(3, 40, false, true, true)),
                ("Price", "shuffle", new BoosterTileState(0, 40, false, true, true)),
                ("Selected", "return", new BoosterTileState(1, 50, true, true, true)),
                ("Disabled", "bloom_burst", new BoosterTileState(0, 60, false, true, false)),
            };
            var stateRow = new Box(body.Left, body.Top + (steps.Length * rowHeight), body.Right, body.Top + ((steps.Length + 1) * rowHeight));
            var stateBar = new Box(stateRow.Left, stateRow.Top + p.U(56f), stateRow.Right, stateRow.Top + p.U(56f) + p.U(DesignTokens.Size.BoosterButton));
            Box[] statePlaces = ScreenLayout.Row(stateBar, 4, p.U(24f), p.U(220f), square: false);
            for (int i = 0; i < states.Length; i++)
            {
                p.Text(states[i].Label, statePlaces[i].CenterX, stateRow.Top + p.U(24f), T.Caption, C.TextSecondary);
                BoosterBarPainter.Tile(p, BoosterBarPainter.Fit(p, statePlaces[i], stateBar), states[i].Id, states[i].State, null);
            }
        }
    }
}
