using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// The round levels in play (spec 006 T040, T050, contracts/presentation.md "engine-free render checks"), part of
    /// <c>--rounds</c>: the level screen on the round rules, driven as a player would, its frames written as
    /// <c>rounds-play-*.png</c> and checked from the asset slots each frame draws:
    /// <list type="bullet">
    /// <item><description>no icon on the cubes with the symbols mode off, the materials' symbols with it on;</description></item>
    /// <item><description>a link tube on a linked pair in the columns and in the places;</description></item>
    /// <item><description>no HUD under Auto (no Pause, speed pill, tray, places, boosters or columns), the board kept;</description></item>
    /// <item><description>the explanation cards on L6, L11, L16 and the reminders on L8, L13, L18;</description></item>
    /// <item><description>the reveal glow, the bridge sites and the built bridge, the walkers of the clearing style, the
    /// jam card on the round boosters and the win;</description></item>
    /// <item><description>a 48 × 48 board (the frame included) fitting the phone frame, with every place working, and its
    /// frame times (SC-009).</description></item>
    /// </list>
    /// </summary>
    public static class RoundPlayPreview
    {
        public static void Run(string root, RoundContentSet rounds, ContentSet empty, string outDir, Action<bool, string> expect)
        {
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-round-play-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                var proofs = new Dictionary<int, RoundProof>();
                foreach (string file in Directory.GetFiles(Path.Combine(root, "content/game/rounds/proofs"), "L*.json"))
                {
                    RoundProof proof = RoundJson.ReadProof(File.ReadAllText(file));
                    proofs[proof.Number] = proof;
                }

                void Frame(float dt)
                {
                    painter.Now += dt;
                    painter.BeginFrame();
                    app.Draw(painter, dt);
                }

                // Draws a frame with the asset slots of that frame alone, and writes it when named.
                HashSet<string> Shot(string? name, float dt = 1f / 30f)
                {
                    Frame(dt);
                    painter.Slots.Clear();
                    Frame(1f / 60f);
                    if (name != null)
                    {
                        File.WriteAllBytes(Path.Combine(outDir, "rounds-play-" + name + ".png"), painter.Png());
                    }

                    return new HashSet<string>(painter.Slots, StringComparer.Ordinal);
                }

                void Run(float seconds, Func<bool>? until = null)
                {
                    for (float t = 0f; t < seconds; t += 1f / 30f)
                    {
                        Frame(1f / 30f);
                        if (until != null && until())
                        {
                            return;
                        }
                    }
                }

                RoundLevelScreen Open(int n)
                {
                    app.LoadLevel(n);
                    return app.Round!;
                }

                bool Symbols(HashSet<string> slots) => slots.Any(s => s.StartsWith("symbol.material.", StringComparison.Ordinal));

                // ---- Level 1: the entry, the guided first tap (through the screen's own touch target), the walkers ----
                RoundLevelScreen s = Open(1);
                expect(s.Guide?.Kind == GuideKind.Entry, "Level 1 opens on the entry's spotlight");
                HashSet<string> slots = Shot("01-entry");
                expect(slots.Contains("board.entry.arch") && slots.Contains("ui.spotlight"), "the entry's spotlight lights the gate's arch");
                s.NextGuideStep();
                expect(s.Guide?.Kind == GuideKind.FirstTap && s.GuidePod >= 0, "then the forced first tap on a pod whose cubes are in reach");
                Shot("01-first-tap");
                Box pod = s.PodBoxes[s.GuidePod]!.Value;
                painter.Dispatch(pod.CenterX, pod.CenterY);
                expect(s.Play.Inputs.Count == 1 && s.Guide == null, "the guided pod takes the tap and the tour ends");
                Run(2.2f);
                slots = Shot("01-walkers");
                expect(s.Animator.Walkers.Count > 0 && slots.Contains("fx.clear.munchers"), "the walkers set off from the gate in the clearing style (Munchers)");
                expect(slots.Contains("tile.candy") && !Symbols(slots), "the cubes show no icon with the symbols mode off (FR-041)");
                app.Meta.Save.Settings.Symbols = true;
                slots = Shot("01-symbols");
                expect(Symbols(slots), "the symbols mode shows the materials' symbols (FR-047)");
                app.Meta.Save.Settings.Symbols = false;

                // The rest of the solution, the patient player's: Auto hides the HUD, then the win.
                bool autoShot = false;
                PlayUntil(s, proofs[1], Run, () => s.Auto);
                Run(0.6f);
                expect(s.Auto && s.Play.Timeline.Pending, "Auto starts once every column is empty and no bridge is left");
                slots = Shot("01-auto");
                autoShot = true;
                expect(s.HudAlpha <= 0f && !slots.Contains("ui.pause") && !slots.Contains("ui.pill.speed") && !slots.Contains("pod.deck") && !slots.Contains("slot.empty") && !slots.Contains("slot.state.working") && !slots.Contains("booster.tile"),
                    "under Auto the whole HUD is gone (FR-044)");
                expect(slots.Contains("tile.candy") && slots.Contains("board.border.stone"), "under Auto the board stays");
                expect(s.Speed == RoundProfile.TailDisplaySpeed, "Auto shows the rest at x2");
                Run(90f, () => s.Play.Settled && s.EndShownAt >= 0f);
                Run(1f);
                Shot("01-win");
                expect(autoShot && s.Won && s.Payout != null && app.Meta.CurrentLevel == 2, "the level is won and paid; the next level is current");

                // ---- The introduction cards (FR-045) ----
                foreach ((int n, RoundIntroKind kind, string mechanic) in new[]
                {
                    (6, RoundIntroKind.Explanation, RoundIntroCards.Hidden), (8, RoundIntroKind.Reminder, RoundIntroCards.Hidden),
                    (11, RoundIntroKind.Explanation, RoundIntroCards.Links), (13, RoundIntroKind.Reminder, RoundIntroCards.Links),
                    (16, RoundIntroKind.Explanation, RoundIntroCards.Bridge), (18, RoundIntroKind.Reminder, RoundIntroCards.Bridge),
                })
                {
                    s = Open(n);
                    s.SkipGuide();
                    expect(s.Intro != null && s.Intro.Kind == kind && s.Intro.Mechanic == mechanic, $"L{n} opens with the {mechanic} {kind.ToString().ToLowerInvariant()} card");
                    slots = Shot($"{n:00}-intro", 0.5f);
                    expect(slots.Contains("ui.card"), $"L{n}'s card is drawn");
                }

                // ---- Hidden pods: the reveal glow (L6) ----
                s = Open(6);
                s.SkipGuide();
                s.CloseIntro();
                bool glowed = PlayUntil(s, proofs[6], Run, () => Enumerable.Range(0, s.Round.Pods.Count).Any(i => s.RevealGlow(i) > 0.5f));
                slots = Shot("06-reveal", 1f / 60f);
                expect(glowed && slots.Contains("pod.reveal.glow"), "a hidden pod that reveals glows (FR-045)");

                // ---- Links: the tube in the columns, the partner's refusal, the tube in the places (L11) ----
                s = Open(11);
                s.SkipGuide();
                s.CloseIntro();
                slots = Shot("11-tube-columns");
                expect(s.Animator.Tubes(RoundScreenLook.PodRows).Any(t => t.Place == RoundTubePlace.Columns) && slots.Contains("pod.link.tube"), "a linked pair in the columns is joined by the tube (FR-043)");
                bool refused = false;
                bool placed = PlayUntil(s, proofs[11], Run, () =>
                {
                    foreach (int top in RoundRules.TopPods(s.Play.Timeline.State))
                    {
                        if (!refused && RoundRules.CanTap(s.Play.Timeline.State, top) == RoundRefusal.PartnerNotExposed)
                        {
                            s.Tap(top);
                            refused = s.ToastText == PlaytestText.T("refusal.partner_buried") && s.LinkHint.HasValue;
                        }
                    }

                    return s.Animator.Tubes(RoundScreenLook.PodRows).Any(t => t.Place == RoundTubePlace.Places);
                });
                Run(0.6f);
                slots = Shot("11-tube-places");
                expect(placed && slots.Contains("pod.link.tube"), "the linked pair in the places is joined by the tube");
                expect(refused, "a tap whose partner is buried says why and pulses the partner (FR-008)");

                // ---- The bridge (L16) ----
                s = Open(16);
                s.SkipGuide();
                s.CloseIntro();
                bool ready = PlayUntil(s, proofs[16], Run, () => s.BuildableBridges().Count > 0);
                slots = Shot("16-bridge-ready", 1f / 60f);
                expect(ready && slots.Contains("board.bridge"), "a bridge site whose approach is open glows (FR-009)");
                bool built = PlayUntil(s, proofs[16], Run, () => s.Animator.Bridge >= 0);
                slots = Shot("16-bridge-built", 0.3f);
                expect(built && slots.Contains("board.bridge"), "the built bridge's plank lies over the stream");

                // ---- Boosters and the jam card on the round rules (FR-012) ----
                app.Meta.JumpTo(12);
                foreach (BoosterKind kind in new[] { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst })
                {
                    app.Meta.MarkDemoSeen(GuideTour.BoosterId(kind));
                }

                // The greedy player (each frame, the first tap the rules allow) jams on the first level found so, at x2.
                int jamLevel = 0;
                for (int n = 2; n <= rounds.LevelCount && jamLevel == 0; n++)
                {
                    var probe = new RoundPlay(rounds.GetLevel(n));
                    Greedy(probe, null);
                    jamLevel = probe.Status == RoundStatus.Jammed || probe.Status == RoundStatus.Stuck ? n : 0;
                }

                expect(jamLevel > 0, "a greedy player jams on some round level");
                if (jamLevel > 0)
                {
                    s = Open(jamLevel);
                    s.SkipGuide();
                    s.CloseIntro();
                    slots = Shot($"{jamLevel:00}-boosters");
                    expect(slots.Contains("booster.tile"), "the booster bar shows the unlocked boosters");
                    Greedy(s.Play, s);
                    Run(2f);
                    slots = Shot($"{jamLevel:00}-jam", 0.5f);
                    expect(s.Blocked && slots.Contains("ui.jam.slots") && s.JamBoosters().Count > 0, $"L{jamLevel} jams; the jam card offers the boosters the round rules accept ({string.Join(", ", s.JamBoosters())})");
                    int before = s.Animator.PlaceCount;
                    s.PressBooster(BoosterKind.ExtraSlot);
                    Run(1f);
                    expect(s.Animator.PlaceCount == before + 1 && s.Play.Inputs.Any(i => i.Type == RoundInputType.ExtraSlot), "Extra Slot adds a place at once, recorded with its model time");
                    Shot($"{jamLevel:00}-extra-slot");
                }

                // ---- A 48 x 48 board with every place working (SC-009) ----
                RoundLevel big = BigLevel();
                var screen = new RoundLevelScreen(app, 21, big, null);
                screen.SkipGuide();
                RoundPainter.LayoutCache cache = RoundPainter.Layout(painter, screen, true, false);
                Box outer = cache.Board.Outer;
                expect(outer.Left >= 0f && outer.Right <= painter.Width && outer.Top >= painter.Insets.Top && outer.Bottom <= cache.Regions.Slots[0].Top,
                    "a 48 x 48 board fits the phone frame above the places");
                for (int k = 0; k < 6; k++)
                {
                    foreach (int top in RoundRules.TopPods(screen.Play.Timeline.State))
                    {
                        if (screen.Play.Check(RoundAction.Tap(top)) == RoundRefusal.None)
                        {
                            screen.Tap(top);
                        }
                    }
                }

                var watch = new Stopwatch();
                var times = new List<double>();
                for (int f = 0; f < 90; f++)
                {
                    painter.Now += 1f / 60f;
                    painter.BeginFrame();
                    watch.Restart();
                    screen.Advance(1f / 60f);
                    screen.Draw(painter);
                    times.Add(watch.Elapsed.TotalMilliseconds);
                }

                File.WriteAllBytes(Path.Combine(outDir, "rounds-play-48x48.png"), painter.Png());
                int working = Enumerable.Range(0, screen.Animator.PlaceCount).Count(k => screen.Animator.PodIn(k) >= 0);
                times.Sort();
                Console.WriteLine($"rounds 48x48: cell {cache.Board.Cell:0.0} px, {working} places working, {screen.Animator.Walkers.Count} walkers; frame median {times[times.Count / 2]:0.0} ms, p95 {times[(int)(times.Count * 0.95)]:0.0} ms (preview painter, first frames render the pictures)");
                expect(working >= 5 && screen.Animator.Walkers.Count > 0, "the 48 x 48 board plays with five places working");

                // ---- Deep columns (the owner's pods of 2026-10-28, FR-064): three cubes a column, one after another, no "+N" ----
                var deep = new RoundLevelScreen(app, 21, DeepLevel(), null);
                deep.SkipGuide();
                RoundPainter.LayoutCache deepCache = RoundPainter.Layout(painter, deep, true, false);
                HashSet<string> DeepShot(string name, float seconds)
                {
                    for (float t = 0f; t < seconds; t += 1f / 30f)
                    {
                        painter.Now += 1f / 30f;
                        painter.BeginFrame();
                        deep.Advance(1f / 30f);
                        deep.Draw(painter);
                    }

                    painter.Slots.Clear();
                    painter.Now += 1f / 60f;
                    painter.BeginFrame();
                    deep.Advance(1f / 60f);
                    deep.Draw(painter);
                    File.WriteAllBytes(Path.Combine(outDir, "rounds-play-" + name + ".png"), painter.Png());
                    return new HashSet<string>(painter.Slots, StringComparer.Ordinal);
                }

                void CheckColumns(string when)
                {
                    ReferenceGameplayRegions dr = deepCache.Regions;
                    var drawn = deepCache.Pods.Where(e => e.Fade >= 1f).ToList();
                    bool threeRows = dr.PodRows == RoundScreenLook.PodRows && Enumerable.Range(0, deep.Animator.ColumnCount).All(c => drawn.Count(e => e.Column == c) == Math.Min(RoundScreenLook.PodRows, deep.Animator.Waiting(c)));
                    bool apart = drawn.All(e => drawn.All(o => e.Pod == o.Pod || !(e.Layout.Left < o.Layout.Right - 0.5f && o.Layout.Left < e.Layout.Right - 0.5f && e.Layout.Top < o.Layout.Bottom - 0.5f && o.Layout.Top < e.Layout.Bottom - 0.5f)));
                    bool ordered = drawn.All(e => e.Depth == 0 || e.Layout.Top > dr.Pod(e.Column, e.Depth - 1).Bottom);
                    bool noMore = !painter.Texts.Any(t => t.Text.StartsWith("+", StringComparison.Ordinal) && t.Box.Top >= dr.PodRow.Top - 1f);
                    expect(threeRows && apart && ordered && noMore && deep.Animator.Waiting(0) > RoundScreenLook.PodRows,
                        $"deep columns {when}: each column shows its first three pods as cubes, one after another, apart, and no \"+N\" ({string.Join(" ", Enumerable.Range(0, deep.Animator.ColumnCount).Select(c => deep.Animator.Waiting(c)))} waiting)");
                }

                HashSet<string> deepSlots = DeepShot("deep-columns", 0.2f);
                expect(deepSlots.Contains(RoundLook.PodCubeSlot) && !deepSlots.Contains("pod.card") && deepSlots.Contains("pod.state.mystery"),
                    "the pods are cubes with no wooden frame (pod.cube), a hidden one the \"?\" cube");
                CheckColumns("at the start");
                int[] nextUp = Enumerable.Range(0, deep.Animator.ColumnCount).Select(c => deep.Animator.At(c, 1)).ToArray();
                for (int c = 0; c < deep.Animator.ColumnCount; c++)
                {
                    int top = deep.Animator.At(c, 0);
                    if (top >= 0 && deep.Play.Check(RoundAction.Tap(top)) == RoundRefusal.None)
                    {
                        deep.Tap(top);
                        break;
                    }
                }

                DeepShot("deep-columns-slid", 0.6f);
                expect(Enumerable.Range(0, deep.Animator.ColumnCount).Any(c => deep.Animator.At(c, 0) == nextUp[c]), "the top pod leaves and the one under it comes up to the top row");
                CheckColumns("after a tap");

                // ---- The owner's board of 2026-10-28: one fixed box, a solid neutral surface, the cube size from the matrix ----
                Box? fixedBox = null;
                float lastCell = float.MaxValue;
                foreach (int sideCells in new[] { 12, 32, 48 })
                {
                    var screenOf = new RoundLevelScreen(app, 21, BigLevel(sideCells), null);
                    screenOf.SkipGuide();
                    for (int k = 0; k < 3; k++)
                    {
                        foreach (int top in RoundRules.TopPods(screenOf.Play.Timeline.State))
                        {
                            if (screenOf.Play.Check(RoundAction.Tap(top)) == RoundRefusal.None)
                            {
                                screenOf.Tap(top);
                            }
                        }
                    }

                    // Mid-clear: some cubes eaten, the surface bare where they stood.
                    int cubes = Enumerable.Range(0, screenOf.Round.CellCount).Count(c => screenOf.Animator.OriginalMaterial(c) >= 0);
                    int Eaten() => Enumerable.Range(0, screenOf.Round.CellCount).Count(c => screenOf.Animator.OriginalMaterial(c) >= 0 && screenOf.Animator.MaterialAt(c) < 0);
                    for (int f = 0; f < 300 && (f < 30 || Eaten() < Math.Min(cubes / 3, 120)); f++)
                    {
                        painter.Now += 1f / 30f;
                        painter.BeginFrame();
                        screenOf.Advance(1f / 30f);
                        screenOf.Draw(painter);
                    }

                    painter.Slots.Clear();
                    painter.Now += 1f / 60f;
                    painter.BeginFrame();
                    screenOf.Advance(1f / 60f);
                    screenOf.Draw(painter);
                    File.WriteAllBytes(Path.Combine(outDir, $"rounds-play-board-{sideCells}x{sideCells}.png"), painter.Png());
                    var boardSlots = new HashSet<string>(painter.Slots, StringComparer.Ordinal);
                    RoundBoardLayout fitted = RoundPainter.Layout(painter, screenOf, true, false).Board;
                    int eaten = Enumerable.Range(0, screenOf.Round.CellCount).Count(c => fitted.KindOf(c) == RoundCellKind.Material && screenOf.Animator.MaterialAt(c) < 0);
                    expect(boardSlots.Contains(RoundBoardLayout.SurfaceSlot) && boardSlots.Contains("board.border.stone") && !boardSlots.Contains("tile.grass") && !boardSlots.Contains("tile.ground"),
                        $"the {sideCells} x {sideCells} board is one neutral surface in the stone frame: no grass cells, no imprint of {eaten} eaten cubes");
                    expect(eaten > 0, $"the {sideCells} x {sideCells} board is caught mid-clear");
                    fixedBox ??= fitted.Outer;
                    expect(fitted.Outer.Equals(fixedBox.Value) && fitted.Grid.Within(fitted.Surface) && fitted.Cell < lastCell,
                        $"the {sideCells} x {sideCells} board stands in the same box {fitted.Outer} with cubes of {fitted.Cell:0.0} px, smaller than a smaller matrix's");
                    lastCell = fitted.Cell;
                }

                SideBySide(Path.Combine(outDir, "rounds-play-board-12x12.png"), Path.Combine(outDir, "rounds-play-board-48x48.png"), Path.Combine(outDir, "rounds-play-board-small-big.png"));

                // ---- Bridges on two strips of a wall laid over the picture (17 §8 as amended on 2026-10-28) ----
                RoundLevel fenced = SideBridgeLevel();
                var side = new RoundLevelScreen(app, 21, fenced, null);
                side.SkipGuide();
                HashSet<string> SideShot(string name, float seconds)
                {
                    for (float t = 0f; t < seconds; t += 1f / 30f)
                    {
                        painter.Now += 1f / 30f;
                        painter.BeginFrame();
                        side.Advance(1f / 30f);
                        side.Draw(painter);
                    }

                    painter.Slots.Clear();
                    painter.Now += 1f / 60f;
                    painter.BeginFrame();
                    side.Advance(1f / 60f);
                    side.Draw(painter);
                    File.WriteAllBytes(Path.Combine(outDir, "rounds-play-" + name + ".png"), painter.Png());
                    return new HashSet<string>(painter.Slots, StringComparer.Ordinal);
                }

                HashSet<string> fence = SideShot("side-bridge-start", 0.2f);
                expect(fence.Contains(RoundBoardLayout.WallSlot) && fence.Contains(RoundBoardLayout.WaterSlot) == false && side.BuildableBridges().Count == 0,
                    "an overlay's wall is drawn as stone (board.wall), and no site is buildable before its approach is cleared");
                for (int k = 0; k < 4; k++)
                {
                    foreach (int top in RoundRules.TopPods(side.Play.Timeline.State))
                    {
                        if (fenced.Pods[top].Code != 'G' && side.Play.Check(RoundAction.Tap(top)) == RoundRefusal.None)
                        {
                            side.Tap(top);
                        }
                    }

                    SideShot("side-bridge-clearing", 3f);
                }

                fence = SideShot("side-bridge-sites", 0.5f);
                RoundBoardLayout fencedBoard = RoundPainter.Layout(painter, side, true, false).Board;
                expect(side.BuildableBridges().Count == 2 && fence.Contains(RoundBoardLayout.BridgeSlot)
                    && fencedBoard.Bridges[0].Crossing == RoundCrossing.Left && fencedBoard.Bridges[1].Crossing == RoundCrossing.Up,
                    "once both approaches are cleared both sites glow on their near banks: one crossing left, one up");
                side.BuildBridge(0);
                fence = SideShot("side-bridge-built", 0.5f);
                expect(side.Animator.Bridge == 0 && fence.Contains(RoundBoardLayout.BridgeSlot) && side.BuildableBridges().Count == 0,
                    "the bridge crossing left is built, its plank laid left to right, and the other site is gone");
                app.Dispose();
            }
            finally
            {
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }

        /// <summary>
        /// The greedy player at x2 and 30 frames a second: every frame it taps the first pod the rules allow, until the level
        /// ends (through the screen's own <see cref="RoundLevelScreen.Tap"/> when <paramref name="screen"/> is given, the same
        /// commands at the same model times as on the bare <paramref name="play"/>).
        /// </summary>
        private static void Greedy(RoundPlay play, RoundLevelScreen? screen)
        {
            for (int frame = 0; frame < 200_000; frame++)
            {
                if (play.Status != RoundStatus.Playing)
                {
                    return;
                }

                foreach (RoundAction action in play.Timeline.Legal())
                {
                    if (action.Type == RoundActionType.Tap)
                    {
                        if (screen != null) screen.Tap(action.Pod); else play.Commit(action);
                        break;
                    }
                }

                play.Advance(1.0 / 30.0, 2);
            }
        }

        /// <summary>
        /// Plays the level's saved solution through the screen as a patient player (each tap once the work settled), checking
        /// <paramref name="done"/> after every frame; true when it held. Continues from the screen's commands so far.
        /// </summary>
        private static bool PlayUntil(RoundLevelScreen s, RoundProof proof, Action<float, Func<bool>?> run, Func<bool> done)
        {
            bool hit = false;
            Func<bool> check = () => hit = hit || done();
            for (int guard = 0; guard < 500; guard++)
            {
                run(60f, () => check() || !s.Play.Timeline.Pending);
                if (hit)
                {
                    return true;
                }

                // The saved solution while the commands so far follow it, else the first legal action (patient either way).
                RoundAction? next = null;
                IReadOnlyList<RoundInput> inputs = s.Play.Inputs;
                IReadOnlyList<RoundCommand> commands = proof.Solution.Commands;
                bool onProof = inputs.Count < commands.Count;
                for (int i = 0; i < inputs.Count && onProof; i++)
                {
                    onProof = inputs[i].Value == commands[i].Index && (inputs[i].Type == RoundInputType.Tap) == (commands[i].Type == RoundCommandType.Tap);
                }

                if (onProof)
                {
                    RoundCommand c = commands[inputs.Count];
                    next = c.Type == RoundCommandType.Tap ? RoundAction.Tap(c.Index) : RoundAction.BuildBridge(c.Index);
                }
                else
                {
                    List<RoundAction> legal = s.Play.Timeline.Legal();
                    next = legal.Count > 0 ? legal[0] : (RoundAction?)null;
                }

                if (next == null)
                {
                    break;
                }

                int count = inputs.Count;
                if (next.Value.Type == RoundActionType.Tap) s.Tap(next.Value.Pod); else s.BuildBridge(next.Value.Choice);
                if (check())
                {
                    return true;
                }

                if (s.Play.Inputs.Count == count)
                {
                    break;
                }
            }

            run(60f, () => check() || (!s.Play.Timeline.Pending && !s.Animator.Busy));
            return hit;
        }

        /// <summary>Two frames side by side in one picture (the small and the big board for the owner's review).</summary>
        private static void SideBySide(string left, string right, string into)
        {
            using SkiaSharp.SKBitmap a = SkiaSharp.SKBitmap.Decode(left);
            using SkiaSharp.SKBitmap b = SkiaSharp.SKBitmap.Decode(right);
            int gap = 24;
            using var both = new SkiaSharp.SKBitmap(a.Width + gap + b.Width, Math.Max(a.Height, b.Height));
            using (var canvas = new SkiaSharp.SKCanvas(both))
            {
                canvas.Clear(SkiaSharp.SKColors.White);
                canvas.DrawBitmap(a, 0f, 0f);
                canvas.DrawBitmap(b, a.Width + gap, 0f);
            }

            using SkiaSharp.SKData png = both.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
            File.WriteAllBytes(into, png.ToArray());
        }

        /// <summary>
        /// A <paramref name="side"/> × <paramref name="side"/> board, the frame included (48: the largest round board): the
        /// wall, the open ring, a picture of the 16 materials in bands, the gate in the middle of the bottom, and pods of up
        /// to 60 cubes in three columns, so the exact accounting holds.
        /// </summary>
        public static RoundLevel BigLevel(int side = 48)
        {
            var rows = new string[side];
            var counts = new int[RoundMaterial.Codes.Length];
            for (int y = 0; y < side; y++)
            {
                var row = new char[side];
                for (int x = 0; x < side; x++)
                {
                    bool wall = x == 0 || y == 0 || x == side - 1 || y == side - 1;
                    bool ring = x == 1 || y == 1 || x == side - 2 || y == side - 2;
                    if (wall)
                    {
                        row[x] = '#';
                    }
                    else if (ring)
                    {
                        row[x] = '.';
                    }
                    else
                    {
                        int m = (((x - 2) / 6) + ((y - 2) / 5)) % counts.Length;
                        row[x] = RoundMaterial.Codes[m];
                        counts[m]++;
                    }
                }

                rows[y] = new string(row);
            }

            char[] bottom = rows[0].ToCharArray();
            bottom[side / 2] = '.';
            rows[0] = new string(bottom);
            var pods = new List<RoundPod>();
            for (int m = 0; m < counts.Length; m++)
            {
                for (int left = counts[m]; left > 0; left -= 60)
                {
                    pods.Add(new RoundPod(RoundMaterial.Codes[m], Math.Min(60, left)));
                }
            }

            var tray = new List<int>[3] { new List<int>(), new List<int>(), new List<int>() };
            for (int p = 0; p < pods.Count; p++)
            {
                tray[p % 3].Add(p);
            }

            return new RoundLevel(21, side, side, rows, pods, tray, new[] { side / 2 });
        }

        /// <summary>
        /// Deep columns (FR-064): a 20 × 20 board (the frame included) of the 16 materials in bands, its pods of at most 8
        /// cubes dealt over six columns, so each column holds five or six pods; the second pod of the first column is hidden.
        /// </summary>
        public static RoundLevel DeepLevel()
        {
            const int side = 20;
            var counts = new int[RoundMaterial.Codes.Length];
            var rows = new string[side];
            for (int y = 0; y < side; y++)
            {
                var row = new char[side];
                for (int x = 0; x < side; x++)
                {
                    bool wall = x == 0 || y == 0 || x == side - 1 || y == side - 1;
                    bool ring = x == 1 || y == 1 || x == side - 2 || y == side - 2;
                    if (wall)
                    {
                        row[x] = '#';
                    }
                    else if (ring)
                    {
                        row[x] = '.';
                    }
                    else
                    {
                        int m = (((x - 2) / 4) + (((y - 2) / 4) * 4)) % counts.Length;
                        row[x] = RoundMaterial.Codes[m];
                        counts[m]++;
                    }
                }

                rows[y] = new string(row);
            }

            rows[0] = rows[0].Substring(0, side / 2) + "." + rows[0].Substring((side / 2) + 1);
            var pods = new List<RoundPod>();
            for (int m = 0; m < counts.Length; m++)
            {
                for (int left = counts[m]; left > 0; left -= 8)
                {
                    pods.Add(new RoundPod(RoundMaterial.Codes[m], Math.Min(8, left), pods.Count == 6 ? RoundMystery.Depth : RoundMystery.None));
                }
            }

            var tray = new List<int>[6];
            for (int c = 0; c < tray.Length; c++)
            {
                tray[c] = new List<int>();
            }

            for (int p = 0; p < pods.Count; p++)
            {
                tray[p % tray.Length].Add(p);
            }

            return new RoundLevel(21, side, side, rows, pods, tray, new[] { side / 2 }, mechanics: new[] { "hidden" });
        }

        /// <summary>
        /// A board with a wall laid over its picture (17 §8 as amended on 2026-10-28): an L of stone fences the top left
        /// corner's G, its site at (3, 1) crossing left from the P, the one at (1, 3) crossing up from the B.
        /// </summary>
        public static RoundLevel SideBridgeLevel()
        {
            var picture = new RoundPicture("P9100", 1, "Fenced corner", 8, 6, new[] { "GGGGPPPP", "GGGGPPPP", "GGGGPPPP", "GGGGPPPP", "BBBBBBBB", "BBBBBBBB" }, "2026-10-28");
            var overlay = new RoundOverlay(new[] { (3, 0), (3, 1), (3, 2), (3, 3), (0, 3), (1, 3), (2, 3) }, new[] { (3, 1), (1, 3) });
            RoundBoardShape board = RoundPictureBoard.Of(picture, overlay);
            var pods = new[] { new RoundPod('B', 8), new RoundPod('B', 8), new RoundPod('P', 8), new RoundPod('P', 8), new RoundPod('G', 9) };
            var tray = new IReadOnlyList<int>[] { new[] { 0, 2 }, new[] { 1, 3 }, new[] { 4 } };
            return new RoundLevel(21, board.Width, board.Height, board.RowsBottomFirst, pods, tray, board.Entries, board.Bridge,
                mechanics: new[] { "bridge" }, overlay: overlay);
        }
    }
}
