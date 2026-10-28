using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// Draws a round level (spec 006 T049, contracts/presentation.md) in the game's look, from <see cref="RoundAnimator"/>
    /// and <see cref="RoundBoardLayout"/>; it holds no rules.
    /// <list type="bullet">
    /// <item><description>The board: the game's stone border and entry arch, grass on the path, water on the stream, a
    /// stone wall where the level's overlay laid one, the built bridge's plank turned to its crossing (a buildable site
    /// glows toward its near bank and takes the tap), the cubes in the round palette without icons (the
    /// symbol only in the symbols mode), a cleared cube as the game's pale restored cell; the clearing style's walkers
    /// follow their couriers on the live timeline, the family hero tinted in the material's colour (FR-042, FR-048).</description></item>
    /// <item><description>The tray: the places' cream plates with the material tile and the count, the columns of wooden
    /// pods (three or four rows and "+N"), a hidden pod's "?" until it reveals and its glow then (FR-045), and the link
    /// tubes on the pods' front faces, under the pods, in the columns and in the places (FR-043).</description></item>
    /// <item><description>The HUD (top bar, tray, boosters) fades away under Auto, leaving the board (FR-044).</description></item>
    /// </list>
    /// </summary>
    public static class RoundPainter
    {
        /// <summary>The cached layout of one screen size (the board's layout allocates its cell tables).</summary>
        public sealed class LayoutCache
        {
            public (float W, float H, Insets Insets, int Places, bool Boosters, bool Badge) Key;
            public ReferenceGameplayRegions Regions = null!;
            public RoundBoardLayout Board = null!;
            public bool[] Held = Array.Empty<bool>();
            public readonly List<int> HeldCells = new List<int>();
            public readonly Dictionary<int, List<(float X, float Y)>> Points = new Dictionary<int, List<(float X, float Y)>>();
            public readonly List<int> Gone = new List<int>();
            public readonly List<(int Pod, int Column, int Depth, Box Layout, Box Drawn, float Fade)> Pods = new List<(int, int, int, Box, Box, float)>();
            public Box?[] FrameOf = Array.Empty<Box?>();
            public readonly bool[] Reachable = new bool[RoundMaterial.Codes.Length];
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RoundLevelScreen, LayoutCache> Caches =
            new System.Runtime.CompilerServices.ConditionalWeakTable<RoundLevelScreen, LayoutCache>();

        /// <summary>The variant a round material's family is drawn with by hosts that know only variants.</summary>
        public static VariantId FamilyVariant(int material) => RoundMaterial.All[material].Family switch
        {
            Family.Bloom => VariantId.Flower,
            Family.Drop => VariantId.Water,
            Family.Twig => VariantId.Wood,
            _ => VariantId.Leaf,
        };

        /// <summary>This frame's regions and board layout for <paramref name="s"/> (cached by screen size and tray).</summary>
        public static LayoutCache Layout(IPainter p, RoundLevelScreen s, bool hasBoosters, bool hasBadge)
        {
            LayoutCache cache = Caches.GetValue(s, _ => new LayoutCache());
            var key = (p.Width, p.Height, p.Insets, s.Animator.PlaceCount, hasBoosters, hasBadge);
            if (cache.Regions == null || !cache.Key.Equals(key))
            {
                ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(p.Width, p.Height, p.Insets, Math.Max(1, s.Round.Tray.Count), s.Animator.PlaceCount, hasBoosters, hasBadge);
                RoundBoardLayout board = RoundBoardLayout.In(r, s.Round);
                cache.Key = key;
                cache.Regions = r.AlignControls(board.Board);
                cache.Board = board;
                cache.Held = new bool[s.Round.CellCount];
                cache.HeldCells.Clear();
                cache.Points.Clear();
                cache.FrameOf = new Box?[s.Round.Pods.Count];
            }

            return cache;
        }

        public static bool HasBoosters(RoundLevelScreen s)
        {
            foreach ((BoosterKind kind, Recovery _, string _) in LevelScreen.Boosters)
            {
                if (s.Meta.Economy.IsUnlocked(kind))
                {
                    return true;
                }
            }

            return false;
        }

        public static (string Text, Rgba Color, string Slot)? Badge(RoundLevelScreen s)
        {
            (string Text, Rgba Color, string Slot)? badge = Visuals.DifficultyBadge(s.Difficulty);
            if (badge.HasValue && !s.Meta.Progression.IsUnlocked(s.Difficulty == DifficultyClass.Hard ? "profile.hard" : "profile.super_hard"))
            {
                return null;
            }

            return badge;
        }

        public static void Draw(IPainter p, RoundLevelScreen s)
        {
            if (s.Guide != null && (s.Won || s.Blocked))
            {
                s.SkipGuide();
            }

            if (DrawEndScreen(p, s))
            {
                return;
            }

            bool boosters = HasBoosters(s);
            (string Text, Rgba Color, string Slot)? badge = Badge(s);
            LayoutCache cache = Layout(p, s, boosters, badge.HasValue);
            ReferenceGameplayRegions r = cache.Regions;
            RoundBoardLayout board = cache.Board;
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, s.Level);

            float hud = s.HudAlpha;
            Action openPause = () => s.App.OpenOverlay(Overlay.Pause);
            if (hud > 0.001f)
            {
                // The top bar (spec 005 §6.1): Pause, the level's sign, the speed pill; gone under Auto (FR-044).
                p.PushAlpha(hud);
                bool live = hud >= 1f;
                Kit.RoundButton(p, r.Pause.CenterX, r.Pause.CenterY, r.Pause.Height, "ui.pause", live ? openPause : null);
                p.Mark("ui.pause");
                Kit.LevelPill(p, r.Sign, PlaytestText.F("common.level", NumberText.Group(s.Level)), s.Difficulty == DifficultyClass.SuperHard && badge.HasValue);
                Kit.SpeedPill(p, r.Speed, s.Meta.Save.Settings.Speed2x, live ? s.ToggleSpeed : (Action?)null);
                if (badge.HasValue)
                {
                    Kit.Badge(p, r.Badge, badge.Value.Text, badge.Value.Color, badge.Value.Slot);
                }

                p.PopAlpha();
            }

            Board(p, s, cache);

            if (hud > 0.001f)
            {
                p.PushAlpha(hud);
                LevelScreen.TrayPanel(p, r);
                Places(p, s, cache);
                if (boosters)
                {
                    BoosterBar(p, s, r.Boosters);
                }

                Columns(p, s, cache);
                Flights(p, s, cache);
                p.PopAlpha();
            }

            // The clearing style's items over the tray (fireflies, bubbles and sparkles flying to the places).
            ClearPainter.DrawItems(p, s.Fx.Items, FxLayer.Over, board.Grid.Left, board.Grid.Top, board.Cell, symbols: s.Symbols);

            string? toast = s.ToastText;
            if (toast != null)
            {
                Kit.Toast(p, board.Outer, toast);
            }

            if (s.Guide != null && s.Intro == null)
            {
                GuidePainter.Draw(p, s, board, r);
            }

            if (s.Play.Settled && (s.Won || (s.Blocked && !s.JamHidden)) && s.Intro == null)
            {
                if (s.EndShownAt < 0f)
                {
                    s.EndShownAt = s.UiNow;
                }

                float since = s.UiNow - s.EndShownAt;
                if (s.Won && s.ShowingMilestone)
                {
                    EndCards.Milestone(p, s, since);
                }
                else if (s.Won)
                {
                    EndCards.Win(p, s, since);
                }
                else if (s.App.Overlays.Count == 0)
                {
                    EndCards.Jam(p, s, since);
                }

                if (s.Won)
                {
                    p.Hit(Kit.Touch(p, r.Pause), openPause);
                }
            }

            if (s.Intro != null)
            {
                Intro(p, s, s.UiNow - s.IntroOpenedAt);
            }
        }

        /// <summary>The full-screen win and milestone, once the win has faded in over the level (as <see cref="LevelScreen"/>).</summary>
        private static bool DrawEndScreen(IPainter p, RoundLevelScreen s)
        {
            if (!s.Won || !s.Play.Settled || s.EndShownAt < 0f || (!s.ShowingMilestone && s.UiNow - s.EndShownAt < EndCards.WinFadeSeconds))
            {
                return false;
            }

            float since = s.UiNow - s.EndShownAt;
            if (s.ShowingMilestone)
            {
                EndCards.Milestone(p, s, since);
            }
            else
            {
                EndCards.Win(p, s, since);
            }

            return true;
        }

        // ---- The board ----

        /// <summary>
        /// The board (FR-040 to FR-042): the stone border, each cell inside the frame as grass, water, a cube or a cleared
        /// cell, the bridge and its sites, the entry arches, then the clearing style's walkers over the cubes.
        /// </summary>
        public static void Board(IPainter p, RoundLevelScreen s, LayoutCache cache)
        {
            RoundBoardLayout board = cache.Board;
            RoundAnimator a = s.Animator;
            bool symbols = s.Symbols;
            Kit.StoneBorder(p, board.Grid, board.Cell);
            BuildFx(s, cache);
            bool[] held = cache.Held;
            long now = s.Play.ClockUs;
            bool bursting = s.Targeting == Recovery.BloomBurst;
            int w = board.Width;
            for (int y = 1; y < board.Height - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int cell = (y * w) + x;
                    Box full = board.CellBox(cell);
                    switch (board.KindOf(cell))
                    {
                        case RoundCellKind.Stream:
                            Kit.Water(p, full, x);
                            break;
                        case RoundCellKind.Wall:
                            Kit.Wall(p, full, x, y);
                            break;
                        case RoundCellKind.BridgeSite:
                            if (board.IsStone(cell))
                            {
                                Kit.Wall(p, full, x, y);
                            }
                            else
                            {
                                Kit.Water(p, full, x);
                            }

                            break;
                        case RoundCellKind.Material:
                        {
                            int m = a.MaterialAt(cell);
                            if (m >= 0 && !held[cell])
                            {
                                Kit.MaterialTile(p, board.CubeBox(cell), RoundMaterial.All[m], RoundLook.CubeStyle, symbols);
                                if (bursting && RoundBoosters.CanBloomBurst(s.Play.Timeline, RoundMaterial.All[m].Code) == RoundRefusal.None)
                                {
                                    p.Mark("fx.burst");
                                    float pulse = 0.5f + (0.5f * (float)Math.Sin(p.Now * 6f));
                                    p.StrokeRound(full.Inset(board.Cell * 0.06f), board.Cell * 0.16f, Math.Max(1f, board.Cell * 0.08f), C.GardenGlow.WithAlpha(0.5f + (0.5f * pulse)));
                                }
                            }
                            else
                            {
                                Kit.ClearedCell(p, full, Math.Max(0, a.OriginalMaterial(cell)));
                                if (m < 0 && a.Burst(cell))
                                {
                                    BurstPuff(p, board, cell, a.OriginalMaterial(cell), now - a.ClearedAt(cell));
                                }
                            }

                            break;
                        }

                        default:
                            Kit.GrassCell(p, full, UiRaster.GrassSeed(x, y));
                            break;
                    }
                }
            }

            List<int> buildable = s.BuildableBridges();
            foreach (RoundBridgeSite site in board.Bridges)
            {
                if (a.Bridge == site.Index)
                {
                    Kit.BridgePlank(p, site);
                }
                else if (buildable.Contains(site.Index))
                {
                    // A site whose approach is open glows toward its near bank and takes the tap (FR-009).
                    p.Mark(RoundBoardLayout.BridgeSlot);
                    float glow = GardenLook.Glow(p.Now);
                    p.PushAlpha(0.45f + (0.4f * glow));
                    Kit.BridgePlank(p, site);
                    p.PopAlpha();
                    p.StrokeRound(site.Glow.Inset(-board.Cell * 0.08f), board.Cell * 0.2f, Math.Max(1.5f, board.Cell * 0.1f), C.GardenGlow.WithAlpha(0.6f + (0.4f * glow)));
                    int choice = site.Index;
                    if (s.HudAlpha >= 1f)
                    {
                        p.Hit(Kit.Touch(p, site.PlankBounds), () => s.BuildBridge(choice));
                    }
                }
            }

            foreach (RoundGate gate in board.Gates)
            {
                Kit.EntryArch(p, gate.Arch);
            }

            ClearPainter.DrawItems(p, s.Fx.Items, FxLayer.Board, board.Grid.Left, board.Grid.Top, board.Cell, symbols: symbols);

            if (bursting)
            {
                // Bloom Burst's target: the cube under the finger picks the material (cells are smaller than a touch target).
                p.Hit(board.Grid, () =>
                {
                    (float tx, float ty) = p.TapPoint;
                    int cx = 1 + (int)Math.Floor((tx - board.Grid.Left) / board.Cell);
                    int cy = board.Height - 2 - (int)Math.Floor((ty - board.Grid.Top) / board.Cell);
                    if (cx >= 1 && cy >= 1 && cx < board.Width - 1 && cy < board.Height - 1)
                    {
                        s.BurstCell((cy * board.Width) + cx);
                    }
                });
            }
        }

        /// <summary>A cube cleared by Bloom Burst: it swells and fades in a ring of sparkles (<c>fx.burst</c>).</summary>
        private static void BurstPuff(IPainter p, RoundBoardLayout board, int cell, int material, long sinceUs)
        {
            if (material < 0 || sinceUs < 0 || sinceUs >= RoundAnimator.BurstUs)
            {
                return;
            }

            float k = sinceUs / (float)RoundAnimator.BurstUs;
            p.Mark("fx.burst");
            Box cube = board.CubeBox(cell);
            p.PushAlpha(1f - k);
            p.PushTransform(0f, 0f, 1f + (0.35f * k), cube.CenterX, cube.CenterY);
            Kit.MaterialTile(p, cube, RoundMaterial.All[material], RoundLook.CubeStyle, false);
            p.PopTransform();
            p.StrokeCircle(cube.CenterX, cube.CenterY, cube.Width * (0.4f + (0.5f * k)), Math.Max(1f, cube.Width * 0.08f), Rgba.White);
            p.PopAlpha();
        }

        /// <summary>
        /// This frame's clearing-style items (spec 006 T044): each walker as a live walk of the timeline (its depart, arrive,
        /// clear and return), from the gate's door along its route, its tiles going to its pod's place; each cube cleared
        /// within <see cref="ClearStyles.RestoreSeconds"/> plays the style's restore.
        /// </summary>
        private static void BuildFx(RoundLevelScreen s, LayoutCache cache)
        {
            FxList list = s.Fx;
            list.Clear();
            foreach (int cell in cache.HeldCells)
            {
                cache.Held[cell] = false;
            }

            cache.HeldCells.Clear();
            RoundAnimator a = s.Animator;
            RoundBoardLayout board = cache.Board;
            ClearStyle style = s.Style;
            float now = s.ShowSeconds;
            foreach (RoundWalk walk in a.Walkers)
            {
                if (walk.Route.Count == 0)
                {
                    continue;
                }

                if (!cache.Points.TryGetValue(walk.Id, out List<(float X, float Y)>? points))
                {
                    points = new List<(float X, float Y)>(walk.Route.Count);
                    foreach (int cell in walk.Route)
                    {
                        (float px, float py) = board.Point(cell);
                        points.Add(((px - board.Grid.Left) / board.Cell, (py - board.Grid.Top) / board.Cell));
                    }

                    cache.Points[walk.Id] = points;
                }

                ClearWalk live = ClearWalk.Live(points, walk.Material, FamilyVariant(walk.Material), walk.Start / 1_000_000f, walk.Arrive / 1_000_000f, walk.Clear / 1_000_000f, walk.Done / 1_000_000f, Seat(s, cache, walk.Pod));
                if (ClearLook.Holds(style, live, now) && !cache.Held[walk.Cell])
                {
                    cache.Held[walk.Cell] = true;
                    cache.HeldCells.Add(walk.Cell);
                }

                ClearLook.Walker(list, style, live, now);
            }

            // Forget the routes of walkers that are home.
            if (cache.Points.Count > a.Walkers.Count + 64)
            {
                cache.Gone.Clear();
                foreach (int id in cache.Points.Keys)
                {
                    bool walking = false;
                    foreach (RoundWalk walk in a.Walkers)
                    {
                        walking |= walk.Id == id;
                    }

                    if (!walking)
                    {
                        cache.Gone.Add(id);
                    }
                }

                foreach (int id in cache.Gone)
                {
                    cache.Points.Remove(id);
                }
            }

            long restore = (long)(ClearStyles.RestoreSeconds * 1_000_000f);
            long clock = s.Play.ClockUs;
            if (clock - a.LastEventAt >= restore + 1)
            {
                return;
            }

            for (int cell = 0; cell < board.Width * board.Height; cell++)
            {
                long at = a.ClearedAt(cell);
                if (at < 0 || clock - at >= restore || a.Burst(cell))
                {
                    continue;
                }

                (float px, float py) = board.Point(cell);
                int material = a.OriginalMaterial(cell);
                ClearLook.Restore(list, style, new ClearFade(((px - board.Grid.Left) / board.Cell, (py - board.Grid.Top) / board.Cell), FamilyVariant(material), at / 1_000_000f, null, material), now);
            }
        }

        /// <summary>Where a pod's tiles go in its place (the plate's tile), in cell units of the grid; null when in none.</summary>
        private static Box? Seat(RoundLevelScreen s, LayoutCache cache, int pod)
        {
            int place = s.Animator.PlaceOf(pod);
            if (place < 0 || place >= cache.Regions.Slots.Count)
            {
                return null;
            }

            RoundBoardLayout board = cache.Board;
            Box tile = ReferenceGameplayRegions.SlotTile(cache.Regions.Slots[place]);
            float ox = board.Grid.Left;
            float oy = board.Grid.Top;
            float cell = board.Cell;
            return new Box((tile.Left - ox) / cell, (tile.Top - oy) / cell, (tile.Right - ox) / cell, (tile.Bottom - oy) / cell);
        }

        // ---- The places ----

        /// <summary>
        /// The places (spec 005 §3.7 plates): empty, the last free one in danger, a working pod's material tile and count, a
        /// waiting pod (work left, none in reach) greyed with the hourglass; a pod flying in fades in as it lands and a
        /// finished one puffs away; Return's targets glow; the link tubes between linked places first.
        /// </summary>
        private static void Places(IPainter p, RoundLevelScreen s, LayoutCache cache)
        {
            ReferenceGameplayRegions r = cache.Regions;
            RoundAnimator a = s.Animator;
            RoundTimeline t = s.Play.Timeline;
            s.PlaceBoxes.Clear();
            int count = Math.Min(a.PlaceCount, r.Slots.Count);
            for (int k = 0; k < count; k++)
            {
                s.PlaceBoxes.Add(r.Slots[k]);
            }

            foreach (RoundTube tube in a.Tubes(r.PodRows))
            {
                int pa = a.PlaceOf(tube.First);
                int pb = a.PlaceOf(tube.Second);
                if (tube.Place == RoundTubePlace.Places && pa < count && pb < count)
                {
                    Kit.LinkTubeStepped(p, LinkTube.Between(r.Slots[pa], RoundLook.ColorOf(a.MaterialOf(tube.First)), r.Slots[pb], RoundLook.ColorOf(a.MaterialOf(tube.Second))));
                }
            }

            ReachableMaterials(t, cache.Reachable);
            int free = 0;
            for (int k = 0; k < count; k++)
            {
                free += a.PodIn(k) < 0 ? 1 : 0;
            }

            long now = s.Play.ClockUs;
            for (int k = 0; k < count; k++)
            {
                Box plate = r.Slots[k];
                bool extra = k >= RoundRules.Capacity;
                int pod = a.PodIn(k);
                if (pod < 0)
                {
                    bool danger = free == 1 && s.Play.Status == RoundStatus.Playing && !s.Auto;
                    Kit.SlotPlate(p, plate, danger ? SlotPlateState.Danger : SlotPlateState.Empty, extra: extra);
                    (int left, long leftAt) = a.Leaving(k);
                    if (left >= 0 && now - leftAt < RoundAnimator.ExitUs && now >= leftAt)
                    {
                        float q = (now - leftAt) / (float)RoundAnimator.ExitUs;
                        p.Mark("fx.puff");
                        Box tile = ReferenceGameplayRegions.SlotTile(plate);
                        p.PushAlpha(1f - q);
                        p.PushTransform(0f, 0f, 1f + (0.3f * q), tile.CenterX, tile.CenterY);
                        Kit.MaterialTile(p, tile, RoundMaterial.All[a.MaterialOf(left)], RoundLook.PodStyle, s.Symbols);
                        p.PopTransform();
                        p.PopAlpha();
                    }

                    continue;
                }

                RoundMaterial material = RoundMaterial.All[a.MaterialOf(pod)];
                bool working = t.InFlight[pod] > 0 || (a.Remaining(pod) > 0 && cache.Reachable[material.Index]);
                float arrival = Visuals.Clamp01((now - a.CommittedAt(pod)) / (float)RoundAnimator.FlightUs);
                bool target = s.Targeting == Recovery.Return && RoundBoosters.CanReturn(t, pod) == RoundRefusal.None;
                if (target)
                {
                    SlotPainter.TargetGlow(p, plate);
                }

                if (arrival < 1f)
                {
                    Kit.SlotPlate(p, plate, SlotPlateState.Empty, extra: extra);
                    p.PushAlpha(0.25f + (0.75f * arrival * arrival));
                }

                bool symbols = s.Symbols;
                Kit.SlotPlate(p, plate, working ? SlotPlateState.Working : SlotPlateState.Stuck, null, a.Remaining(pod), extra, tile: (box, stuck) =>
                    Kit.MaterialTile(p, box, material, RoundLook.PodStyle, symbols, stuck ? TileState.Grey : TileState.Normal));
                if (arrival < 1f)
                {
                    p.PopAlpha();
                }

                if (target)
                {
                    int chosen = pod;
                    p.Hit(Kit.Touch(p, plate), () => s.ReturnPod(chosen));
                }
            }
        }

        /// <summary>The materials with a cube in reach now (a place whose material has none waits, greyed).</summary>
        private static void ReachableMaterials(RoundTimeline t, bool[] into)
        {
            Array.Clear(into, 0, into.Length);
            RoundState state = t.State;
            foreach (int cell in state.Reach().Targets)
            {
                if (state.Board[cell] >= 0)
                {
                    into[state.Board[cell]] = true;
                }
            }
        }

        /// <summary>A committed pod's tile flying from its column to its place (presentation only).</summary>
        private static void Flights(IPainter p, RoundLevelScreen s, LayoutCache cache)
        {
            RoundAnimator a = s.Animator;
            long now = s.Play.ClockUs;
            for (int k = 0; k < Math.Min(a.PlaceCount, cache.Regions.Slots.Count); k++)
            {
                int pod = a.PodIn(k);
                if (pod < 0 || !s.LaunchBoxes[pod].HasValue)
                {
                    continue;
                }

                float q = (now - a.CommittedAt(pod)) / (float)RoundAnimator.FlightUs;
                if (q < 0f || q >= 1f)
                {
                    continue;
                }

                Box from = s.LaunchBoxes[pod]!.Value;
                Box to = ReferenceGameplayRegions.SlotTile(cache.Regions.Slots[k]);
                float lift = (float)Math.Sin(q * Math.PI);
                float e = Kit.Ease(q);
                float x = from.CenterX + ((to.CenterX - from.CenterX) * e);
                float y = from.CenterY + ((to.CenterY - from.CenterY) * e) - (lift * to.Height * 0.5f);
                float size = (from.Width + ((to.Width - from.Width) * e)) * (1f + (0.12f * lift));
                Box box = Box.FromCenter(x, y, size, size);
                Kit.SoftShadow(p, box.Offset(0f, size * (0.06f + (0.12f * lift))), size * 0.2f, 0.25f * (1f - (0.5f * lift)));
                float rest = to.Width;
                p.PushTransform(0f, 0f, size / Math.Max(1f, rest), x, y);
                Kit.MaterialTile(p, Box.FromCenter(x, y, rest, rest), RoundMaterial.All[a.MaterialOf(pod)], RoundLook.PodStyle, s.Symbols);
                p.PopTransform();
            }
        }

        // ---- The columns ----

        /// <summary>
        /// The columns of pods (spec 005 FR-021: frames wider than tall, one column per stack, the exposed pod in front, the
        /// next ones under it, "+N" for the rest): every shown pod's box first (sliding between rows, <see cref="TrayMotion"/>),
        /// then the link tubes between linked pods' front faces, then the pods over them, a reveal glow behind a pod that has
        /// just revealed, and the pulse round a partner a refused tap waits for.
        /// </summary>
        private static void Columns(IPainter p, RoundLevelScreen s, LayoutCache cache)
        {
            ReferenceGameplayRegions r = cache.Regions;
            RoundAnimator a = s.Animator;
            float now = s.ShowSeconds;
            int columns = Math.Min(a.ColumnCount, r.Columns.Count);
            TrayMotion motion = s.TrayMotion;
            motion.BeginFrame();
            List<(int Pod, int Column, int Depth, Box Layout, Box Drawn, float Fade)> pods = cache.Pods;
            pods.Clear();
            for (int pod = 0; pod < cache.FrameOf.Length; pod++)
            {
                cache.FrameOf[pod] = null;
                s.PodBoxes[pod] = null;
            }

            bool swirl = s.LastBooster.HasValue && s.LastBooster.Value.Kind == BoosterKind.Shuffle && s.Play.ClockUs - s.LastBooster.Value.At < 450_000;
            for (int st = 0; st < columns; st++)
            {
                int waiting = a.Waiting(st);
                if (waiting == 0)
                {
                    continue;
                }

                for (int depth = Math.Min(waiting, r.PodRows + 1) - 1; depth >= 0; depth--)
                {
                    int pod = a.At(st, depth);
                    bool shown = r.Shows(depth);
                    Box layout = shown ? r.Pod(st, depth) : r.Pod(st, r.PodRows - 1);
                    Box target = shown ? layout : new Box(layout.Left, layout.Bottom, layout.Right, layout.Bottom);
                    TrayMotion.Slide slide = motion.Track(s.KeyOf(pod), st, depth, target, now, snap: swirl);
                    bool entering = slide.Moving && !r.Shows(slide.FromDepth);
                    if (!shown && !slide.Moving)
                    {
                        continue;
                    }

                    float fade = entering ? Kit.Ease(slide.Progress) : shown ? 1f : 1f - slide.Progress;
                    pods.Add((pod, st, depth, layout, slide.Box, fade));
                    if (shown)
                    {
                        PodChip chip = PodChip.In(slide.Box, depth > 0);
                        cache.FrameOf[pod] = chip.Frame;
                        s.PodBoxes[pod] = chip.Tile;
                    }
                }
            }

            // The tubes first, so they pass under the pods (FR-043).
            foreach (RoundTube tube in a.Tubes(r.PodRows))
            {
                if (tube.Place == RoundTubePlace.Columns && cache.FrameOf[tube.First].HasValue && cache.FrameOf[tube.Second].HasValue)
                {
                    Kit.LinkTubeStepped(p, LinkTube.Between(cache.FrameOf[tube.First]!.Value, RoundLook.ColorOf(a.MaterialOf(tube.First)), cache.FrameOf[tube.Second]!.Value, RoundLook.ColorOf(a.MaterialOf(tube.Second))));
                }
            }

            for (int st = 0; st < columns; st++)
            {
                if (a.Waiting(st) == 0)
                {
                    PodPainter.EmptyColumn(p, r.Pod(st, 0));
                }
                else
                {
                    p.Mark("pod.deck");
                }
            }

            bool interactive = s.HudAlpha >= 1f;
            foreach ((int pod, int st, int depth, Box layout, Box drawn, float fade) in pods)
            {
                bool exposed = depth == 0;
                Box touch = Kit.Touch(p, layout);
                if (exposed && interactive)
                {
                    int tapped = pod;
                    p.Hit(touch, () => s.Tap(tapped));
                }

                if (fade < 1f)
                {
                    p.PushAlpha(fade);
                }

                PodChip chip = PodChip.In(layout, depth > 0);
                float glow = s.RevealGlow(pod);
                if (glow > 0f)
                {
                    Kit.RevealGlow(p, PodChip.In(drawn, depth > 0).Frame, glow);
                }

                float press = exposed && interactive ? Kit.Press(p, touch, true) : 0f;
                PodLook look = exposed && interactive && p.Pressed(touch) ? PodLook.Pressed : PodLook.Exposed;
                p.PushTransform(drawn.Left - layout.Left, drawn.Top - layout.Top, 1f, 0f, 0f);
                p.PushSquash(drawn.Width / Math.Max(1f, layout.Width), drawn.Height / Math.Max(1f, layout.Height), layout.Left, layout.Top);
                Kit.Squash(p, layout, press, tile: true);
                RoundMaterial? material = a.Revealed(pod) ? RoundMaterial.All[a.MaterialOf(pod)] : null;
                Kit.RoundPod(p, chip, material, a.Remaining(pod), look, depth > 0, s.Symbols);
                p.PopTransform();
                p.PopTransform();
                p.PopTransform();
                if (fade < 1f)
                {
                    p.PopAlpha();
                }
            }

            for (int st = 0; st < columns; st++)
            {
                int more = a.Waiting(st) - r.PodRows;
                if (more > 0)
                {
                    PodPainter.MoreBadge(p, r.Chip(st, r.PodRows - 1), more);
                }
            }

            Hint(p, s, cache);
        }

        /// <summary>The pulse round the link partner a refused tap waits for (or round the "+N" that hides it).</summary>
        private static void Hint(IPainter p, RoundLevelScreen s, LayoutCache cache)
        {
            if (!s.LinkHint.HasValue)
            {
                return;
            }

            (int pod, float at) = s.LinkHint.Value;
            float k = PodLinks.Hint(s.UiNow - at);
            if (k <= 0f)
            {
                return;
            }

            RoundAnimator a = s.Animator;
            ReferenceGameplayRegions r = cache.Regions;
            foreach (int member in a.GroupOf(pod))
            {
                if (member == pod)
                {
                    continue;
                }

                Box ring;
                if (cache.FrameOf[member].HasValue)
                {
                    ring = cache.FrameOf[member]!.Value;
                }
                else if (a.ColumnOf(member) >= 0 && a.ColumnOf(member) < r.Columns.Count)
                {
                    Box badge = r.Chip(a.ColumnOf(member), r.PodRows - 1).Badge;
                    ring = Box.FromCenter(badge.CenterX, badge.CenterY, badge.Height * 1.5f, badge.Height * 1.5f);
                }
                else
                {
                    continue;
                }

                Rgba color = RoundLook.ColorOf(a.MaterialOf(member));
                float grow = ring.Height * (0.06f + (0.1f * k));
                p.StrokeRound(ring.Inset(-grow), (ring.Height * 0.22f) + grow, p.U(5f) * (0.6f + (0.4f * k)), color.WithAlpha(0.35f + (0.65f * k)));
            }
        }

        // ---- The boosters ----

        /// <summary>The booster bar (spec 005 §3.7) on the round rules: a booster the rules would not take now is greyed.</summary>
        private static void BoosterBar(IPainter p, RoundLevelScreen s, IReadOnlyList<Box> places)
        {
            s.BoosterBoxes.Clear();
            int i = 0;
            bool interactive = s.HudAlpha >= 1f;
            foreach ((BoosterKind kind, Recovery recovery, string id) in LevelScreen.Boosters)
            {
                if (!s.Meta.Economy.IsUnlocked(kind) || i >= places.Count)
                {
                    continue;
                }

                Box box = places[i++];
                s.BoosterBoxes[kind] = box;
                int charges = s.Meta.Economy.Charges(kind);
                var state = new BoosterTileState(
                    charges,
                    s.Meta.Economy.Price(kind),
                    selected: s.Targeting == recovery,
                    usable: (charges == 0 || s.Applicable(kind)) && !s.Won,
                    affordable: charges > 0 || s.Meta.Economy.CanAfford(kind));
                BoosterKind pressed = kind;
                Kit.BoosterTile(p, box, id, state, state.Disabled || !interactive ? (Action?)null : () =>
                {
                    if (s.Meta.Economy.Charges(pressed) == 0 && s.Guide == null) s.BuyBooster(pressed);
                    else s.PressBooster(pressed);
                });
            }
        }

        // ---- Cards ----

        /// <summary>The jam card's well: each place's material tile and count, an empty place as its plate.</summary>
        public static void JamWell(IPainter p, JamCardRegions r, RoundLevelScreen s)
        {
            p.Mark("ui.jam.slots");
            Kit.Well(p, r.Well, r.Well.Height * 0.2f);
            RoundAnimator a = s.Animator;
            for (int n = 0; n < a.PlaceCount; n++)
            {
                (Box tile, Box count) = r.WellCell(n, a.PlaceCount);
                int pod = a.PodIn(n);
                if (pod < 0)
                {
                    Kit.SlotPlate(p, tile, SlotPlateState.Empty);
                }
                else
                {
                    Kit.MaterialTile(p, tile, RoundMaterial.All[a.MaterialOf(pod)], RoundLook.PodStyle, s.Symbols);
                    Kit.CountBelow(p, count, a.Remaining(pod), false);
                }
            }
        }

        /// <summary>
        /// A round level's introduction card (spec 006 FR-045, <see cref="RoundIntroCards"/>): the mechanic's picture (a
        /// hidden pod beside the same pod revealed and glowing, two pods joined by the tube, or the stream with a plank), its
        /// title on the card's sign, its lines (three for the explanation, one for the reminder) and "Got it".
        /// </summary>
        public static void Intro(IPainter p, RoundLevelScreen s, float since)
        {
            RoundIntroCard card = s.Intro!;
            float width = ScreenLayout.Card(p.Width, p.Height, p.Insets, 0f).Body.Width * 0.94f;
            var lines = new List<(string Text, bool First)>();
            for (int i = 0; i < card.LineKeys.Count; i++)
            {
                foreach (string line in EndCards.Lines(p, PlaytestText.T(card.LineKeys[i]), i == 0 ? T.ButtonSecondary : T.Body, width, 3))
                {
                    lines.Add((line, i == 0));
                }
            }

            const float lineUnits = 58f;
            const float pictureUnits = 230f;
            const float buttonUnits = 150f;
            float content = pictureUnits + (lines.Count * lineUnits) + buttonUnits + 40f;
            CardRegions r = Kit.Card(p, content, PlaytestText.T(card.TitleKey), null, Kit.Pop(since));
            float y = r.Body.Top + p.U(10f);
            IntroPicture(p, card.Picture, new Box(r.Body.Left, y, r.Body.Right, y + p.U(pictureUnits - 20f)), since);
            y += p.U(pictureUnits);
            foreach ((string text, bool first) in lines)
            {
                Rgba ink = first ? C.InkBrown : C.InkBrownSoft;
                p.Text(text, r.Body.CenterX, y + p.U(lineUnits / 2f), first ? T.ButtonSecondary : T.Body, ink, r.Body.Width, look: TextLook.Plain(ink));
                y += p.U(lineUnits);
            }

            float bw = Math.Min(r.Body.Width * 0.7f, p.U(520f));
            var button = new Box(r.Body.CenterX - (bw / 2f), y + p.U(24f), r.Body.CenterX + (bw / 2f), y + p.U(24f + 110f));
            Kit.PrimaryButton(p, button, PlaytestText.T(card.ButtonKey), s.CloseIntro, T.Button);
            p.PopTransform();
        }

        /// <summary>The introduction card's picture, made of the level's own pieces (pods, tube, water, plank).</summary>
        private static void IntroPicture(IPainter p, RoundIntroPicture picture, Box area, float since)
        {
            float h = area.Height;
            float podW = h * 0.62f * PodChip.Aspect;
            float gap = h * 0.5f;
            switch (picture)
            {
                case RoundIntroPicture.HiddenPod:
                {
                    var left = Box.FromCenter(area.CenterX - ((podW + gap) / 2f), area.CenterY, podW, h * 0.62f);
                    var right = Box.FromCenter(area.CenterX + ((podW + gap) / 2f), area.CenterY, podW, h * 0.62f);
                    Kit.RoundPod(p, PodChip.In(left), null, 0, PodLook.Exposed, false, false);
                    p.Shape("ui.chevron", Box.FromCenter(area.CenterX, area.CenterY, h * 0.28f, h * 0.28f), C.InkBrownSoft);
                    PodChip revealed = PodChip.In(right);
                    Kit.RevealGlow(p, revealed.Frame, 0.4f + (0.6f * GardenLook.Glow(p.Now)));
                    Kit.RoundPod(p, revealed, RoundMaterial.All[1], 12, PodLook.Exposed, false, false);
                    break;
                }

                case RoundIntroPicture.LinkedPair:
                {
                    PodChip a = PodChip.In(Box.FromCenter(area.CenterX - ((podW + gap) / 2f), area.CenterY, podW, h * 0.62f));
                    PodChip b = PodChip.In(Box.FromCenter(area.CenterX + ((podW + gap) / 2f), area.CenterY, podW, h * 0.62f));
                    Kit.LinkTubeStepped(p, LinkTube.BetweenPods(a, RoundLook.ColorOf(0), b, RoundLook.ColorOf(8)));
                    Kit.RoundPod(p, a, RoundMaterial.All[0], 20, PodLook.Exposed, false, false);
                    Kit.RoundPod(p, b, RoundMaterial.All[8], 14, PodLook.Exposed, false, false);
                    break;
                }

                default:
                {
                    float cell = h * 0.34f;
                    int cells = 6;
                    float left = area.CenterX - (cell * cells / 2f);
                    for (int x = 0; x < cells; x++)
                    {
                        var above = new Box(left + (x * cell), area.CenterY - (cell * 1.5f), left + ((x + 1) * cell), area.CenterY - (cell * 0.5f));
                        var water = new Box(above.Left, above.Bottom, above.Right, above.Bottom + cell);
                        var below = new Box(above.Left, water.Bottom, above.Right, water.Bottom + cell);
                        Kit.GrassCell(p, above, x);
                        Kit.Water(p, water, x);
                        Kit.GrassCell(p, below, x + 3);
                    }

                    Box site = Box.FromCenter(area.CenterX + (cell * 0.5f), area.CenterY, cell, cell);
                    float plank = Kit.Ease(Visuals.Clamp01((since - 0.2f) / 0.4f));
                    p.PushAlpha(plank);
                    p.Mark(RoundBoardLayout.BridgeSlot);
                    p.PushRotate(90f, site.CenterX, site.CenterY);
                    Kit.WoodPlank(p, Box.FromCenter(site.CenterX, site.CenterY, cell * (1f + (2f * RoundBoardLayout.PlankReach)), cell * RoundBoardLayout.PlankWidth), 0.3f, 3);
                    p.PopTransform();
                    p.PopAlpha();
                    break;
                }
            }
        }

        /// <summary>
        /// The finished picture on the win (FR-046): the approved picture's cells as the board's cubes in a thin stone frame
        /// (empty cells as grass, the stream as water), as large as fits in <paramref name="box"/>.
        /// </summary>
        public static void FinishedPicture(IPainter p, Box box, RoundLevelScreen s)
        {
            p.Mark("tile.picture");
            (int w, int h) = s.PictureSize;
            w = Math.Max(1, w);
            h = Math.Max(1, h);
            const float thin = 0.3f;
            float rim = BoardLayout.Gap + thin;
            float cell = Math.Min(box.Width / (w + (2f * rim)), box.Height / (h + (2f * rim)));
            float ox = box.CenterX - (cell * w / 2f);
            float oy = box.CenterY - (cell * h / 2f);
            var grid = new Box(ox, oy, ox + (cell * w), oy + (cell * h));
            Kit.StoneBorder(p, grid, cell, thin);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    char c = CellOf(s, x, y);
                    var full = new Box(ox + (x * cell), oy + (y * cell), ox + ((x + 1) * cell), oy + ((y + 1) * cell));
                    int material = RoundMaterial.IndexOf(c);
                    if (material >= 0)
                    {
                        Kit.MaterialTile(p, full.Inset(cell * RoundBoardLayout.CubeInset), RoundMaterial.All[material], RoundLook.CubeStyle, s.Symbols);
                    }
                    else if (c == '#' || c == 'b')
                    {
                        Kit.Water(p, full, x);
                    }
                    else
                    {
                        Kit.GrassCell(p, full, UiRaster.GrassSeed(x, y));
                    }
                }
            }
        }

        // The picture's cell at (x, y), the top row first: the approved picture's, else the level's rows inside the ring.
        private static char CellOf(RoundLevelScreen s, int x, int y)
        {
            if (s.Picture != null)
            {
                return s.Picture.RowsTopFirst[y][x];
            }

            RoundLevel level = s.Round;
            return level.RowsBottomFirst[level.Height - 2 - y][x + 1];
        }
    }
}
