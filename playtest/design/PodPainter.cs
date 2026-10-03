using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>How a pod is drawn (frame 12): its waiting look is a separate flag (<see cref="Kit.Pod"/>).</summary>
    public enum PodLook
    {
        Exposed,
        Pressed,
        Locked,
    }

    /// <summary>
    /// The Source Tray as columns of pods (spec 005 FR-021, contracts/look.md §3.7, §6.1; frame 12), one column per stack in
    /// <see cref="ReferenceGameplayRegions.Columns"/>, its pods one after another and never on each other (the owner's
    /// gameplay rule of 2026-10-03), so the player reads what each choice uncovers:
    /// <list type="bullet">
    /// <item><description>the exposed pod in the top row (<see cref="ReferenceGameplayRegions.Pod"/> at depth 0): a dark
    /// wooden frame wider than tall, a panel tinted by the variant, the variant's sticker tile at its left and the plain
    /// count at its right; it squashes under the finger and is the only pod that takes a tap (spec 001 FR-011), through a
    /// touch box grown to the touch minimum;</description></item>
    /// <item><description>the next pods of the stack in the rows below it (<see cref="ReferenceGameplayRegions.PodRows"/>
    /// rows in all), the same parts muted but readable: variant symbol and count (spec 001 FR-013);</description></item>
    /// <item><description>a "+N" count badge on the last shown pod for the pods deeper than the tray shows;</description></item>
    /// <item><description>an emptied stack as a sunk well where its exposed pod stood;</description></item>
    /// <item><description>locked: the padlock on a grey panel; mystery: the lilac "?" tile and the count; connected: a link
    /// between the frames of members on one row, a ring of the group's color on each member otherwise.</description></item>
    /// </list>
    /// When the exposed pod leaves for its slot the pods under it slide up one row, and when Return puts a pod back on top
    /// the column slides down (<see cref="TrayMotion"/>); committed pods fly from their tile to their slot's tile, returned
    /// ones fly back.
    /// </summary>
    public static class PodPainter
    {
        /// <summary>
        /// The columns of the level's Source stacks in the tray's pod row: each stack's shown pods, the deepest first, at
        /// their places in <see cref="ReferenceGameplayRegions.Pod"/> (sliding there when the rules moved them,
        /// <see cref="LevelScreen.TrayMotion"/>), the exposed one taking the tap, or a well for an emptied stack; then the
        /// links of the connected groups and the "+N" badges. Records where each pod's tile was drawn in
        /// <see cref="LevelScreen.PodBoxes"/>, where flights start and end.
        /// </summary>
        public static void DrawColumns(IPainter p, ReferenceGameplayRegions r, LevelScreen s)
        {
            LevelView view = s.Session.View;
            s.PodBoxes.Clear();
            int stacks = Math.Min(view.StackCount, r.Columns.Count);
            var linked = new Dictionary<string, List<Box>>(StringComparer.Ordinal);
            float now = s.Animator.Now;
            TrayMotion motion = s.TrayMotion;
            motion.BeginFrame();

            // Shuffle: the pods swirl in place for a moment, and take their new places at once.
            float swirl = s.LastBooster.HasValue && s.LastBooster.Value.Kind == Client.Services.Save.BoosterKind.Shuffle ? (now - s.LastBooster.Value.At) / 0.45f : 1f;
            if (swirl < 1f)
            {
                p.Mark("fx.shuffle_swirl");
            }

            for (int st = 0; st < stacks; st++)
            {
                IReadOnlyList<string> stack = view.Stack(st);
                if (stack.Count == 0)
                {
                    EmptyColumn(p, r.Pod(st, 0));
                    continue;
                }

                p.Mark("pod.deck");

                // The shown rows and the one under them: a pod there grows in from the last row's bottom edge when the
                // exposed pod leaves, and shrinks into it when Return pushes the column down, fading, so it never leaves
                // the pod row.
                for (int depth = Math.Min(stack.Count, r.PodRows + 1) - 1; depth >= 0; depth--)
                {
                    string id = stack[depth];
                    bool shown = r.Shows(depth);
                    Box layout = shown ? r.Pod(st, depth) : r.Pod(st, r.PodRows - 1);
                    Box target = shown ? layout : new Box(layout.Left, layout.Bottom, layout.Right, layout.Bottom);
                    TrayMotion.Slide slide = motion.Track(id, st, depth, target, now, snap: swirl < 1f);
                    bool entering = slide.Moving && !r.Shows(slide.FromDepth);
                    if (!shown && !slide.Moving)
                    {
                        continue;
                    }

                    PodInfo pod = view.Pod(id);
                    bool exposed = depth == 0 && view.IsExposed(id);
                    bool locked = pod.Locked || s.Animator.HeldPodLocks.Contains(id);
                    bool live = exposed && !locked;
                    Box touch = Kit.Touch(p, layout);
                    PodChip chip = PodChip.In(layout);
                    Box drawn = slide.Box;
                    s.PodBoxes[id] = Map(chip.Tile, layout, drawn);
                    if (shown)
                    {
                        // The links join the frames, which stand narrower than their places in the middle of the column.
                        Link(linked, pod, PodChip.In(drawn).Frame);
                    }

                    // The exposed pod squashes under the finger and springs back (spec 003 FR-017).
                    float press = live ? Kit.Press(p, touch, true) : 0f;
                    PodLook look = locked ? PodLook.Locked : live && p.Pressed(touch) ? PodLook.Pressed : PodLook.Exposed;
                    if (exposed)
                    {
                        // Only the exposed pod takes the tap (a locked one answers with its refusal); its target is the
                        // pod's final place, grown to the touch minimum, which may reach over the waiting pod under it.
                        p.Hit(touch, () => s.Tap(id));
                    }

                    if (Returning(s, id))
                    {
                        // A pod Return put back is in the air until its flight lands on top of its column.
                        continue;
                    }

                    bool fading = entering || !shown;
                    if (fading)
                    {
                        p.PushAlpha(entering ? Kit.Ease(slide.Progress) : 1f - slide.Progress);
                    }

                    if (swirl < 1f)
                    {
                        float grow = drawn.Height * (0.05f + (0.25f * swirl));
                        p.StrokeRound(drawn.Inset(-grow), (drawn.Height * 0.2f) + grow, p.U(6f), C.BoosterShuffle.WithAlpha(1f - swirl));
                        p.PushTransform(0f, 0f, 0.75f + (0.25f * Kit.Ease(swirl)), drawn.CenterX, drawn.CenterY);
                    }

                    // The pod is drawn at its size in its row and moved (and stretched on its way between a waiting row
                    // and the top row, or into the last row's edge) by the canvas, so a slide reuses its pictures instead
                    // of rendering new sizes.
                    p.PushTransform(drawn.Left - layout.Left, drawn.Top - layout.Top, 1f, 0f, 0f);
                    p.PushSquash(drawn.Width / Math.Max(1f, layout.Width), drawn.Height / Math.Max(1f, layout.Height), layout.Left, layout.Top);
                    Kit.Squash(p, layout, press, tile: true);
                    Kit.Pod(p, chip, Shown(pod), pod.Remaining, look, waiting: depth > 0);
                    p.PopTransform();
                    p.PopTransform();
                    p.PopTransform();
                    if (swirl < 1f)
                    {
                        p.PopTransform();
                    }

                    if (fading)
                    {
                        p.PopAlpha();
                    }
                }
            }

            DrawLinks(p, view, linked);

            // The pods deeper than the tray shows wait under a "+N" badge on the last shown pod of their column.
            for (int st = 0; st < stacks; st++)
            {
                int more = view.Stack(st).Count - r.PodRows;
                if (more > 0)
                {
                    MoreBadge(p, r.Chip(st, r.PodRows - 1), more);
                }
            }
        }

        /// <summary>
        /// A column drawn from plain data (the component sheet, frame 12) at the places of stack <paramref name="stack"/> of
        /// <paramref name="r"/>, moved down by <paramref name="dy"/>: <paramref name="pods"/> top first (the exposed pod,
        /// then the waiting ones; a null variant is a hidden mystery pod), as many as the tray shows;
        /// <paramref name="total"/> pods in the stack (more than the rows shown adds the "+N" badge); the exposed pod in
        /// <paramref name="look"/>. No taps. Returns the pods' frames drawn (<see cref="PodChip.Frame"/>), top first.
        /// </summary>
        public static Box[] DrawColumn(IPainter p, ReferenceGameplayRegions r, int stack, float dy, IReadOnlyList<(VariantId? Variant, int Count, bool Locked)> pods, int total, PodLook look)
        {
            if (pods.Count == 0)
            {
                EmptyColumn(p, r.Pod(stack, 0).Offset(0f, dy));
                return Array.Empty<Box>();
            }

            p.Mark("pod.deck");
            int shown = Math.Min(pods.Count, r.PodRows);
            var frames = new Box[shown];
            PodChip? last = null;
            for (int depth = shown - 1; depth >= 0; depth--)
            {
                PodChip chip = PodChip.In(r.Pod(stack, depth).Offset(0f, dy));
                last ??= chip;
                frames[depth] = chip.Frame;
                PodLook podLook = pods[depth].Locked ? PodLook.Locked : depth == 0 ? look : PodLook.Exposed;
                Kit.Pod(p, chip, pods[depth].Variant, pods[depth].Count, podLook, waiting: depth > 0);
            }

            if (total > r.PodRows && last != null)
            {
                MoreBadge(p, last, total - r.PodRows);
            }

            return frames;
        }

        /// <summary>The "+N" disc of the pods a column does not show, over its last shown pod's top left corner (<see cref="PodChip.Badge"/>; the count keeps the bottom right).</summary>
        public static void MoreBadge(IPainter p, PodChip last, int more) =>
            Kit.CountBadge(p, last.Badge.CenterX, last.Badge.CenterY, last.Badge.Height, "+" + more.ToString(CultureInfo.InvariantCulture));

        /// <summary>A pod's variant as the player sees it: null while a mystery pod is hidden (FR-039).</summary>
        public static VariantId? Shown(PodInfo pod) => pod.Mystery && !pod.Variant.HasValue ? null : pod.Variant;

        /// <summary>Whether a pod Return put back is still flying from its slot to the tray.</summary>
        private static bool Returning(LevelScreen s, string podId)
        {
            foreach (Flight flight in s.Animator.Flights)
            {
                if (flight.ToTray && flight.PodId == podId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary><paramref name="part"/> of a pod laid out in <paramref name="from"/>, where the pod is drawn in <paramref name="to"/>.</summary>
        private static Box Map(Box part, Box from, Box to)
        {
            float sx = to.Width / Math.Max(1f, from.Width);
            float sy = to.Height / Math.Max(1f, from.Height);
            return new Box(
                to.Left + ((part.Left - from.Left) * sx),
                to.Top + ((part.Top - from.Top) * sy),
                to.Left + ((part.Right - from.Left) * sx),
                to.Top + ((part.Bottom - from.Top) * sy));
        }

        private static void Link(Dictionary<string, List<Box>> linked, PodInfo pod, Box box)
        {
            if (pod.ConnectedGroupId == null)
            {
                return;
            }

            if (!linked.TryGetValue(pod.ConnectedGroupId, out List<Box>? boxes))
            {
                linked[pod.ConnectedGroupId] = boxes = new List<Box>();
            }

            boxes.Add(box);
        }

        /// <summary>An emptied stack (§6.1, <c>pod.deck</c>): a sunk place of parchment where its exposed pod stood.</summary>
        public static void EmptyColumn(IPainter p, Box exposed)
        {
            p.Mark("pod.deck");
            Box well = exposed.Inset(exposed.Height * 0.06f);
            Kit.Well(p, well, well.Height * 0.24f, C.ParchmentWell.Mix(C.ParchmentEdge, 0.5f));
        }

        /// <summary>
        /// The links of the connected groups (spec 002 FR-012): a bar between consecutive members drawn on one row of the
        /// grid, and a small ring of the link's color on each member when a group's members lie on different rows. Each
        /// group has its own color (state.link, state.link_2, state.link_3), by the groups' order, as in Unity's TrayView.
        /// </summary>
        private static void DrawLinks(IPainter p, LevelView view, Dictionary<string, List<Box>> linked)
        {
            if (linked.Count == 0)
            {
                return;
            }

            List<string> groups = ConnectedGroups(view);
            foreach (KeyValuePair<string, List<Box>> group in linked)
            {
                Rgba color = LinkPalette[Math.Max(0, groups.IndexOf(group.Key)) % LinkPalette.Length];
                List<Box> boxes = group.Value;
                boxes.Sort((a, b) => a.Left.CompareTo(b.Left));
                bool oneRow = true;
                for (int i = 1; i < boxes.Count; i++)
                {
                    oneRow &= Math.Abs(boxes[i].CenterY - boxes[0].CenterY) < boxes[0].Height * 0.5f;
                }

                if (oneRow)
                {
                    for (int i = 1; i < boxes.Count; i++)
                    {
                        Link(p, boxes[i - 1], boxes[i], color);
                    }

                    continue;
                }

                foreach (Box box in boxes)
                {
                    LinkRing(p, box, color);
                }
            }
        }

        /// <summary>
        /// The ring mark of a connected pod whose group lies on several rows: a dot of the group's color on its frame's top
        /// right corner (the "+N" disc takes the top left, the count the bottom right).
        /// </summary>
        public static void LinkRing(IPainter p, Box box, Rgba color)
        {
            p.Mark("pod.link");
            float d = Math.Min(box.Width, box.Height) * 0.16f;
            p.FillCircle(box.Right - d, box.Top + d, d * 1.25f, Rgba.White);
            p.FillCircle(box.Right - d, box.Top + d, d, color);
        }

        /// <summary>The connected groups' link colors, in order (the Unity tray's palette).</summary>
        public static readonly Rgba[] LinkPalette = { C.StateLink, C.StateLink2, C.StateLink3 };

        /// <summary>The level's connected groups, sorted by id: a group's place picks its link color.</summary>
        private static List<string> ConnectedGroups(LevelView view)
        {
            var groups = new List<string>();
            foreach (string id in view.PodIds)
            {
                string? group = view.Pod(id).ConnectedGroupId;
                if (group != null && !groups.Contains(group))
                {
                    groups.Add(group);
                }
            }

            groups.Sort(StringComparer.Ordinal);
            return groups;
        }

        /// <summary>
        /// The link between two connected pods on one row (<c>pod.link</c>, spec 002 FR-012): a rounded bar with a white rim
        /// across the gap between their frames, a little above their middle, riveted to each frame's border, in its group's
        /// <paramref name="color"/> (teal <c>state.link</c> by default). Its sizes follow the pods' shorter side.
        /// </summary>
        public static void Link(IPainter p, Box a, Box b, Rgba? color = null)
        {
            Rgba link = color ?? C.StateLink;
            p.Mark("pod.link");
            float size = Math.Min(Math.Min(a.Width, a.Height), Math.Min(b.Width, b.Height));
            float y = a.Top + (a.Height * 0.42f);
            float x0 = a.Right - (size * 0.08f);
            float x1 = b.Left + (size * 0.08f);
            float bar = Math.Min(size * 0.12f, a.Height * 0.8f);
            p.Line(x0, y + (bar * 0.25f), x1, y + (bar * 0.25f), bar + (size * 0.05f), C.GardenShadow.WithAlpha(0.25f));
            p.Line(x0, y, x1, y, bar + (size * 0.04f), Rgba.White);
            p.Line(x0, y, x1, y, bar, link);
            p.Line(x0, y - (bar * 0.18f), x1, y - (bar * 0.18f), bar * 0.3f, link.Lighten(0.35f));
            foreach (float x in new[] { x0, x1 })
            {
                p.FillCircle(x, y, bar * 0.62f, Rgba.White);
                p.FillCircle(x, y, bar * 0.46f, link.Darken(0.15f));
            }
        }

        /// <summary>
        /// Committed pods flying from the tray to their slots, and returned pods flying back: the pod's variant tile (the
        /// lilac "?" while still hidden) leaves the pod's tile at its size there, arcs over and shrinks to the slot's tile,
        /// over a soft shadow (a returned one the other way, landing on the tile of its new place on top of its column).
        /// </summary>
        public static void DrawFlights(IPainter p, LevelScreen s)
        {
            foreach (Flight flight in s.Animator.Flights)
            {
                Box? slot = s.SlotBoxes[flight.Slot];
                if (slot == null)
                {
                    continue;
                }

                float k = Visuals.Clamp01((s.Animator.Now - flight.Start) / LevelAnimator.FlightSeconds);
                Box slotTile = SlotPainter.TileBox(slot.Value);
                float fromX = flight.FromX;
                float fromY = flight.FromY;
                float toX = slotTile.CenterX;
                float toY = slotTile.CenterY;
                float toSize = slotTile.Width;
                float fromSize = s.PodBoxes.TryGetValue(flight.PodId, out Box pod) ? pod.Width : s.LaunchBoxes.TryGetValue(flight.PodId, out Box launch) ? launch.Width : toSize;
                if (flight.ToTray)
                {
                    (fromX, fromY, toX, toY) = (toX, toY, flight.FromX, flight.FromY);
                    (fromSize, toSize) = (toSize, fromSize);
                    if (s.PodBoxes.TryGetValue(flight.PodId, out Box target))
                    {
                        toX = target.CenterX;
                        toY = target.CenterY;
                    }
                }

                if (float.IsNaN(fromX) || float.IsNaN(toX))
                {
                    continue;
                }

                float lift = (float)Math.Sin(k * Math.PI);
                float x = fromX + ((toX - fromX) * k);
                float y = fromY + ((toY - fromY) * k) - (lift * slot.Value.Height * 0.4f);
                float size = (fromSize + ((toSize - fromSize) * k)) * (1f + (0.12f * lift));
                Box box = Box.FromCenter(x, y, size, size);
                Kit.SoftShadow(p, box.Offset(0f, size * (0.06f + (0.12f * lift))), size * 0.2f, 0.25f * (1f - (0.5f * lift)));

                // The tile is drawn at the slot's tile size and scaled with the canvas, so the whole flight reuses the
                // slot's picture instead of rendering a new size every frame.
                float rest = slotTile.Width;
                p.PushTransform(0f, 0f, size / Math.Max(1f, rest), x, y);
                Kit.CandyTile(p, Box.FromCenter(x, y, rest, rest), flight.Variant, TileStyle.Sticker);
                p.PopTransform();
            }
        }
    }

    /// <summary>
    /// The tray's pods in motion (presentation only, spec 005 FR-002; the playtest's twin of the Unity tray's slides): where
    /// each pod of the tray's columns was drawn, so that when the rules move it to another row of its column (the exposed
    /// pod left for its Waiting Slot, Return put one back on top) it slides there in <see cref="SlideSeconds"/> of the
    /// animation's clock, easing out, instead of jumping. A pod that changes column (Shuffle, while its swirl plays) or
    /// first shows takes its place at once. The core's events alone move the pods; this only paces how the move is shown,
    /// so the rules' outcome never depends on it. Engine-free.
    /// </summary>
    public sealed class TrayMotion
    {
        /// <summary>How long a pod slides to its new row (animation seconds, so 2× speed halves it).</summary>
        public const float SlideSeconds = 0.18f;

        private readonly Dictionary<string, Place> _places = new Dictionary<string, Place>(StringComparer.Ordinal);
        private readonly List<string> _gone = new List<string>();
        private int _frame;

        /// <summary>Forgets every pod's place (a restart puts the whole tray back at once).</summary>
        public void Clear() => _places.Clear();

        /// <summary>Starts drawing the tray: forgets the pods the last drawing did not track (they left the tray).</summary>
        public void BeginFrame()
        {
            _frame++;
            _gone.Clear();
            foreach (KeyValuePair<string, Place> entry in _places)
            {
                if (entry.Value.Frame < _frame - 1)
                {
                    _gone.Add(entry.Key);
                }
            }

            foreach (string id in _gone)
            {
                _places.Remove(id);
            }
        }

        /// <summary>Whether a pod is still sliding at <paramref name="now"/> (the screen keeps redrawing).</summary>
        public bool Moving(float now)
        {
            foreach (Place place in _places.Values)
            {
                if (place.Progress(now) < 1f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tracks pod <paramref name="id"/> at <paramref name="depth"/> of <paramref name="stack"/>, whose place in the layout
        /// is <paramref name="target"/>: a new row of the same column starts a slide from where it is drawn now (mid-slide
        /// included); a new column, or <paramref name="snap"/>, puts it there at once. Returns where to draw it.
        /// </summary>
        public Slide Track(string id, int stack, int depth, Box target, float now, bool snap)
        {
            if (!_places.TryGetValue(id, out Place? place))
            {
                place = new Place(stack, depth, target);
                _places[id] = place;
            }
            else if (place.Depth != depth || place.Stack != stack)
            {
                if (place.Stack == stack && !snap)
                {
                    place.From = place.Box(now);
                    place.FromDepth = place.Depth;
                    place.At = now;
                }
                else
                {
                    place.FromDepth = depth;
                    place.At = float.NegativeInfinity;
                }

                place.Stack = stack;
                place.Depth = depth;
            }

            place.To = target;
            place.Frame = _frame;
            float k = place.Progress(now);
            return new Slide(place.Box(now), k, k < 1f ? place.FromDepth : depth);
        }

        /// <summary>Where a pod is drawn now (<see cref="Box"/>), how far its slide is (1 at rest) and the row it came from.</summary>
        public readonly struct Slide
        {
            public Slide(Box box, float progress, int fromDepth)
            {
                Box = box;
                Progress = progress;
                FromDepth = fromDepth;
            }

            public Box Box { get; }

            public float Progress { get; }

            public int FromDepth { get; }

            public bool Moving => Progress < 1f;
        }

        private sealed class Place
        {
            public Place(int stack, int depth, Box target)
            {
                Stack = stack;
                Depth = depth;
                FromDepth = depth;
                From = target;
                To = target;
                At = float.NegativeInfinity;
            }

            public int Stack { get; set; }

            public int Depth { get; set; }

            public int FromDepth { get; set; }

            public Box From { get; set; }

            public Box To { get; set; }

            public float At { get; set; }

            public int Frame { get; set; }

            public float Progress(float now) => float.IsNegativeInfinity(At) ? 1f : Visuals.Clamp01((now - At) / SlideSeconds);

            public Box Box(float now)
            {
                float k = Kit.Ease(Progress(now));
                return new Box(
                    From.Left + ((To.Left - From.Left) * k),
                    From.Top + ((To.Top - From.Top) * k),
                    From.Right + ((To.Right - From.Right) * k),
                    From.Bottom + ((To.Bottom - From.Bottom) * k));
            }
        }
    }
}
