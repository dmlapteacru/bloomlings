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
            yield return new Fixture(25, "kit", "Extra: reference look kit", (p, data) => KitSheet(p));
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

        /// <summary>
        /// A component sheet in the reference look (frames 12–14, spec 005 §4.1): the tray's parchment over the lawn and the
        /// title in <c>ink.brown</c>.
        /// </summary>
        private static void TraySheet(SkiaPainter p, string title, out Box body, bool bodyBand = true)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            Box sheet = safe.Inset(p.U(30f));
            LevelScreen.TrayFrame(p, sheet, p.U(48f));
            Box inner = sheet.Inset(p.U(7f));
            float split = safe.Top + p.U(190f);
            LevelScreen.TrayBand(p, new Box(inner.Left, inner.Top, inner.Right, split - p.U(4f)), p.U(42f));
            if (bodyBand)
            {
                LevelScreen.TrayBand(p, new Box(inner.Left, split + p.U(4f), inner.Right, inner.Bottom), p.U(42f));
            }

            p.Text(title, safe.CenterX, safe.Top + p.U(110f), T.Title, C.InkBrown, look: TextLook.Plain(C.InkBrown));
            body = new Box(safe.Left + p.U(70f), safe.Top + p.U(220f), safe.Right - p.U(70f), safe.Bottom - p.U(70f));
        }

        /// <summary>The left and right edges of a component sheet's bands (see <see cref="TraySheet"/>).</summary>
        private static (float Left, float Right) SheetBand(IPainter p)
        {
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            return (safe.Left + p.U(37f), safe.Right - p.U(37f));
        }

        /// <summary>A state's label on a component sheet: <c>ink.brown_soft</c> captions.</summary>
        private static void StateLabel(IPainter p, string label, float cx, float cy, float maxWidth) =>
            p.Text(label, cx, cy, T.Caption, C.InkBrownSoft, maxWidth);

        private static void PodStates(SkiaPainter p)
        {
            TraySheet(p, "Pod states", out Box body);
            (string Label, PodInfo Pod, PodLook Look)[] states =
            {
                ("Exposed", new PodInfo("a", VariantId.Leaf, 18, PodLocation.Tray, -1, false, false, null), PodLook.Exposed),
                ("Next in stack", new PodInfo("b", VariantId.Water, 12, PodLocation.Tray, -1, false, false, null), PodLook.Next),
                ("Pressed", new PodInfo("c", VariantId.Leaf, 18, PodLocation.Tray, -1, false, false, null), PodLook.Pressed),
                ("Locked", new PodInfo("d", VariantId.Acorn, 20, PodLocation.Tray, -1, true, false, null), PodLook.Locked),
                ("Mystery", new PodInfo("e", null, 14, PodLocation.Tray, -1, false, true, null), PodLook.Exposed),
                ("Connected", new PodInfo("f", VariantId.Flower, 12, PodLocation.Tray, -1, false, false, "g"), PodLook.Exposed),
            };
            Box[] cells = Grid(body, p, states.Length, 3, p.U(400f));
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                float size = Math.Min(cell.Width * 0.7f, p.U(230f));
                Box pod = Box.FromCenter(cell.CenterX, cell.Top + p.U(100f) + (size / 2f), size, size);
                if (states[i].Label == "Connected")
                {
                    // Two linked pods side by side, as two columns of the tray.
                    float small = size * 0.62f;
                    Box a = Box.FromCenter(cell.CenterX - (small * 0.62f), pod.CenterY, small, small);
                    Box b = Box.FromCenter(cell.CenterX + (small * 0.62f), pod.CenterY, small, small);
                    PodPainter.Pod(p, a, states[i].Pod, PodLook.Exposed, null);
                    PodPainter.Pod(p, b, new PodInfo("g", VariantId.Water, 8, PodLocation.Tray, -1, false, false, "g"), PodLook.Exposed, null);
                    PodPainter.Link(p, a, b);
                }
                else
                {
                    PodPainter.Pod(p, pod, states[i].Pod, states[i].Look, null, handle: states[i].Look != PodLook.Next);
                }

                StateLabel(p, states[i].Label, cell.CenterX, cell.Top + p.U(36f), cell.Width);
            }

            // A column of the tray: the exposed pod, the two that follow it, and "+N" for the ones further down.
            var column = new Box(body.Left, cells[cells.Length - 1].Bottom + p.U(10f), body.Right, body.Bottom);
            StateLabel(p, "A tray column with more below", column.CenterX, column.Top + p.U(36f), column.Width);
            float pod2 = Math.Min(p.U(150f), (column.Height - p.U(110f)) / 3.2f);
            if (pod2 > p.U(40f))
            {
                (VariantId Variant, int Count)[] stack = { (VariantId.Flower, 9), (VariantId.Moss, 6), (VariantId.Wood, 12) };
                for (int d = stack.Length - 1; d >= 0; d--)
                {
                    Box box = Box.FromCenter(column.CenterX, column.Top + p.U(100f) + (d * pod2 * 1.09f) + (pod2 / 2f), pod2, pod2);
                    PodPainter.Pod(p, box, new PodInfo("s" + d, stack[d].Variant, stack[d].Count, PodLocation.Tray, -1, false, false, null), d == 0 ? PodLook.Exposed : PodLook.Next, null, handle: d == 0);
                    if (d == stack.Length - 1)
                    {
                        float h = box.Height * 0.26f;
                        Kit.CountBadge(p, box.Left + (h * 0.42f), box.Bottom - (h * 0.42f), h, "+4");
                    }
                }
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

        /// <summary>
        /// The reference look's kit on parchment (spec 005 T009), like the reference's "Target Variants" and "UI Elements"
        /// strips: the candy tiles in both styles, the stone border and arch, the pedestal with rays and petals, the wooden
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

            // The board style in a 4 × 2 grid inside the stone border, a Garden Entry arch below it (its crown at the border).
            float cell = p.U(78f);
            float rim = cell * 0.48f;
            var grid = new Box(rows[2].Left + rim, rows[2].Top + rim, rows[2].Left + rim + (cell * 4f), rows[2].Top + rim + (cell * 2f));
            Kit.StoneBorder(p, grid, cell);
            for (int i = 0; i < launch.Length; i++)
            {
                float cx = grid.Left + ((i % 4) * cell);
                float cy = grid.Top + ((i / 4) * cell);
                Kit.CandyTile(p, new Box(cx, cy, cx + cell, cy + cell), launch[i].Id, TileStyle.Board);
            }

            Kit.StoneArch(p, grid.Left + (cell * 2f), grid.Bottom + (cell * 0.46f) + (cell * 1.5f), cell, EntrySide.Bottom);

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
            Kit.PetalsPill(p, new Box(tray[4].Left, tray[4].Top, tray[4].Right - p.U(20f), tray[4].Top + p.U(76f)), 2450, () => { });
            Kit.CostPill(p, new Box(tray[4].Left, tray[4].Bottom - p.U(60f), tray[4].Left + p.U(170f), tray[4].Bottom), Cost.Charges(2));
            Kit.CountBadge(p, tray[4].Right - p.U(40f), tray[4].Bottom - p.U(30f), p.U(56f), "3");

            // Pods (exposed with its handle, queued) and Waiting Slots (working, empty, stuck).
            Box[] pods = Spread(new Box(rows[7].Left, rows[7].Top + p.U(24f), rows[7].Right, rows[7].Bottom), new[] { 180f, 180f, 156f, 156f, 156f }, new[] { 180f, 180f, 156f, 156f, 156f }, p);
            Kit.Pod(p, pods[0], VariantId.Leaf, 12, PodLook.Exposed);
            Kit.Pod(p, pods[1], VariantId.Flower, 8, PodLook.Next);
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

        private static void SlotStates(SkiaPainter p)
        {
            TraySheet(p, "Waiting slot states", out Box body);
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
            Box[] cells = Grid(body, p, states.Length, 3, p.U(390f));
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                float height = Math.Min(cell.Width * 0.7f, p.U(230f));
                float width = height * SlotPainter.PlateAspect;
                var plate = Box.FromCenter(cell.CenterX, cell.Top + p.U(100f) + (height / 2f), width, height);
                if (states[i].Label == "Return target")
                {
                    SlotPainter.TargetGlow(p, plate);
                }

                SlotPainter.Slot(p, plate, states[i].Look, states[i].Locked, states[i].Danger, states[i].Extra, 10f);
                StateLabel(p, states[i].Label, cell.CenterX, cell.Top + p.U(36f), cell.Width);
            }

            // The row as it shows in play: two working pods, a stuck one, an empty slot and the last free one in danger.
            var row = new Box(body.Left, cells[cells.Length - 1].Bottom + p.U(20f), body.Right, body.Bottom);
            if (row.Height > p.U(160f))
            {
                StateLabel(p, "The row in play", row.CenterX, row.Top + p.U(16f), row.Width);
                var band = new Box(row.Left, row.Top + p.U(46f), row.Right, Math.Min(row.Bottom, row.Top + p.U(46f + 170f)));
                Box[] plates = SlotPainter.Cells(p, band, 5);
                SlotLook[] looks =
                {
                    new SlotLook { PodId = "r1", Variant = VariantId.Leaf, Count = 3, InFlight = 1 },
                    new SlotLook { PodId = "r2", Variant = VariantId.Flower, Count = 2, InFlight = 1 },
                    new SlotLook { PodId = "r3", Variant = VariantId.Water, Count = 1 },
                    new SlotLook { PodId = "r4", Variant = VariantId.Acorn, Count = 4, InFlight = 2 },
                    new SlotLook(),
                };
                for (int i = 0; i < plates.Length; i++)
                {
                    SlotPainter.Slot(p, plates[i], looks[i], false, i == plates.Length - 1, false, 10f);
                }
            }
        }

        private static void BoosterBar(SkiaPainter p)
        {
            TraySheet(p, "Booster bar", out Box body, bodyBand: false);
            (float bandLeft, float bandRight) = SheetBand(p);
            (string Label, int Shown)[] steps = { ("Level 1–2 (hidden)", 0), ("Level 3", 1), ("Level 4", 2), ("Level 6", 3), ("Level 9+", 4) };
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] charges = { 2, 2, 1, 0 };
            float rowHeight = Math.Min(p.U(250f), body.Height / (steps.Length + 1.3f));
            float barHeight = Math.Min(p.U(DesignTokens.Size.BoosterButton), rowHeight - p.U(80f));
            float groove = p.U(4f);
            for (int s = 0; s < steps.Length; s++)
            {
                // Each unlock step on its own band of the tray's parchment.
                var row = new Box(body.Left, body.Top + (s * rowHeight), body.Right, body.Top + (s * rowHeight) + rowHeight);
                LevelScreen.TrayBand(p, new Box(bandLeft, row.Top - p.U(26f) + groove, bandRight, row.Bottom - p.U(26f) - groove), p.U(22f));
                p.TextLeft(steps[s].Label, row.Left, row.Top + p.U(16f), T.Caption, C.InkBrownSoft);
                var bar = new Box(row.Left, row.Top + p.U(50f), row.Right, row.Top + p.U(50f) + barHeight);
                Box[] places = BoosterBarPainter.Places(p, bar);
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
            float top = body.Top + (steps.Length * rowHeight);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            LevelScreen.TrayBand(p, new Box(bandLeft, top - p.U(26f) + groove, bandRight, safe.Bottom - p.U(37f)), p.U(42f));
            var stateBar = new Box(body.Left, top + p.U(70f), body.Right, top + p.U(70f) + barHeight);
            Box[] statePlaces = BoosterBarPainter.Places(p, stateBar);
            for (int i = 0; i < states.Length; i++)
            {
                StateLabel(p, states[i].Label, statePlaces[i].CenterX, top + p.U(26f), statePlaces[i].Width * 1.4f);
                BoosterBarPainter.Tile(p, BoosterBarPainter.Fit(p, statePlaces[i], stateBar), states[i].Id, states[i].State, null);
            }
        }
    }
}
