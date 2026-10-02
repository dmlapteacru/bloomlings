using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>How a pod card is drawn (frame 12).</summary>
    public enum PodLook
    {
        Exposed,
        Next,
        Pressed,
        Locked,
    }

    /// <summary>
    /// The Source Tray and its pods, in the states of frame 12 (spec 002 FR-012), as the reference's wooden pods on the
    /// tray's parchment (spec 005 contracts/look.md §3.7, research D3; <see cref="Kit.Pod"/>):
    /// <list type="bullet">
    /// <item><description>exposed: a dark wooden frame with its handle on top, a panel tinted by the variant, the variant
    /// tile and the plain count below it;</description></item>
    /// <item><description>next in stack: the same pod dimmed toward the parchment, fully visible below the exposed one
    /// (spec 003 FR-022a);</description></item>
    /// <item><description>pressed: the frame sinks and squashes, and springs back;</description></item>
    /// <item><description>locked: the padlock on a grey panel;</description></item>
    /// <item><description>mystery: the lilac "?" tile with its count;</description></item>
    /// <item><description>connected: a teal link between the frames.</description></item>
    /// </list>
    /// The variant reads first from its tile (color and symbol), then from the panel's tint (spec 001 FR-012).
    /// </summary>
    public static class PodPainter
    {
        public static void DrawTray(IPainter p, Box area, LevelScreen s)
        {
            LevelView view = s.Session.View;
            s.PodBoxes.Clear();
            int stacks = view.StackCount;
            if (stacks == 0)
            {
                return;
            }

            // A grid like the reference game's source area (spec 003 FR-022a): one column per stack, the exposed pod on
            // top and the pods that follow it below, never overlapping, so the player sees what each choice uncovers. The
            // grid leaves room above it for the exposed pods' handles.
            TrayGrid grid = ScreenLayout.Tray(new Box(area.Left, area.Top + p.U(HandleRoom), area.Right, area.Bottom), stacks, p.Scale);
            var linked = new Dictionary<string, List<Box>>(StringComparer.Ordinal);

            // Shuffle: the pods swirl in place for a moment.
            float swirl = s.LastBooster.HasValue && s.LastBooster.Value.Kind == Client.Services.Save.BoosterKind.Shuffle ? (s.Animator.Now - s.LastBooster.Value.At) / 0.45f : 1f;
            if (swirl < 1f)
            {
                p.Mark("fx.shuffle_swirl");
            }

            for (int st = 0; st < stacks; st++)
            {
                IReadOnlyList<string> stack = view.Stack(st);
                if (stack.Count == 0)
                {
                    // An emptied stack: a sunk place on the parchment.
                    Box empty = grid.Cell(st, 0).Inset(grid.PodSize * 0.06f);
                    Kit.Well(p, empty, empty.Width * 0.18f, C.ParchmentWell.Mix(C.ParchmentEdge, 0.5f));
                    continue;
                }

                int shown = Math.Min(stack.Count, ScreenLayout.TrayRows);
                // From the bottom row up, so the exposed pod's squash lies over the pod below it.
                for (int depth = shown - 1; depth >= 0; depth--)
                {
                    string id = stack[depth];
                    PodInfo pod = view.Pod(id);
                    Box box = grid.Cell(st, depth);
                    s.PodBoxes[id] = box;
                    bool exposed = depth == 0 && view.IsExposed(id);
                    bool locked = pod.Locked || s.Animator.HeldPodLocks.Contains(id);
                    bool pressed = exposed && !locked && p.Pressed(box);
                    if (swirl < 1f)
                    {
                        p.StrokeCircle(box.CenterX, box.CenterY, box.Width * (0.4f + (0.3f * swirl)), p.U(6f), C.BoosterShuffle.WithAlpha(1f - swirl));
                        p.PushTransform(0f, 0f, 0.75f + (0.25f * Kit.Ease(swirl)), box.CenterX, box.CenterY);
                    }

                    // The exposed pod squashes under the finger and springs back (spec 003 FR-017).
                    float press = exposed && !locked ? Kit.Press(p, Kit.Touch(p, box), true) : 0f;
                    Kit.Squash(p, box, press, tile: true);
                    Pod(p, box, pod, locked ? PodLook.Locked : pressed ? PodLook.Pressed : exposed ? PodLook.Exposed : PodLook.Next, s, handle: depth == 0);
                    p.PopTransform();
                    if (swirl < 1f)
                    {
                        p.PopTransform();
                    }

                    if (pod.ConnectedGroupId != null)
                    {
                        if (!linked.TryGetValue(pod.ConnectedGroupId, out List<Box>? boxes))
                        {
                            linked[pod.ConnectedGroupId] = boxes = new List<Box>();
                        }

                        boxes.Add(box);
                    }

                    if (exposed)
                    {
                        // The exposed pod takes the tap; its target grows to the touch minimum over the row below,
                        // which takes no taps (FR-027).
                        p.Hit(Kit.Touch(p, box), () => s.Tap(id));
                    }
                }

                // Deeper pods are not drawn; a "+N" count badge on the last shown pod's lower left corner says how many more
                // wait there.
                if (stack.Count > shown)
                {
                    Box last = grid.Cell(st, shown - 1);
                    float h = last.Height * 0.26f;
                    string more = "+" + (stack.Count - shown).ToString(CultureInfo.InvariantCulture);
                    Kit.CountBadge(p, last.Left + (h * 0.42f), last.Bottom - (h * 0.42f), h, more);
                }
            }

            foreach (List<Box> group in linked.Values)
            {
                for (int i = 1; i < group.Count; i++)
                {
                    Link(p, group[i - 1], group[i]);
                }
            }
        }

        /// <summary>The room kept above the tray grid for the exposed pods' handles, in reference units.</summary>
        public const float HandleRoom = 12f;

        /// <summary>
        /// One pod in a state of frame 12 (<see cref="Kit.Pod"/>): a mystery pod still hidden shows the "?" tile.
        /// <paramref name="handle"/> puts the wooden handle on an exposed, pressed or locked pod at the top of its column.
        /// </summary>
        public static void Pod(IPainter p, Box box, PodInfo pod, PodLook look, LevelScreen? s, bool handle = true)
        {
            bool hidden = pod.Mystery && !pod.Variant.HasValue;
            Kit.Pod(p, box, hidden ? null : pod.Variant, pod.Remaining, look, handle);
        }

        /// <summary>
        /// The teal link between two connected pods (<c>pod.link</c>, spec 002 FR-012): a rounded bar with a white rim
        /// across the gap between their frames, a little above their middle, riveted to each frame.
        /// </summary>
        public static void Link(IPainter p, Box a, Box b)
        {
            p.Mark("pod.link");
            float size = Math.Min(a.Width, b.Width);
            float y = a.Top + (a.Height * 0.42f);
            float x0 = a.Right - (size * 0.08f);
            float x1 = b.Left + (size * 0.08f);
            float bar = size * 0.1f;
            p.Line(x0, y + (bar * 0.25f), x1, y + (bar * 0.25f), bar + (size * 0.05f), C.GardenShadow.WithAlpha(0.25f));
            p.Line(x0, y, x1, y, bar + (size * 0.04f), Rgba.White);
            p.Line(x0, y, x1, y, bar, C.StateLink);
            p.Line(x0, y - (bar * 0.18f), x1, y - (bar * 0.18f), bar * 0.3f, C.StateLink.Lighten(0.35f));
            foreach (float x in new[] { x0, x1 })
            {
                p.FillCircle(x, y, bar * 0.62f, Rgba.White);
                p.FillCircle(x, y, bar * 0.46f, C.StateLink.Darken(0.15f));
            }
        }

        /// <summary>The count in a card face's lower part (kept for callers of the spec 004 cards): the plain count below the tile.</summary>
        public static void Count(IPainter p, Box face, int count) =>
            Kit.CountBelow(p, new Box(face.Left, face.Bottom - (face.Height * 0.3f), face.Right, face.Bottom), count, false);

        /// <summary>The count on a card's lower edge (kept for callers of the spec 002 cards): the plain count, softer when dimmed.</summary>
        public static void CountPill(IPainter p, Box face, int count, bool dim) =>
            Kit.CountBelow(p, new Box(face.Left, face.Bottom - (face.Height * 0.3f), face.Right, face.Bottom), count, dim);

        /// <summary>
        /// Committed pods flying from the tray to their slots, and returned pods flying back: the pod's variant tile (the
        /// lilac "?" while still hidden) leaves its frame, arcs over and shrinks to the slot's tile, over a soft shadow.
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
                float fromX = flight.FromX;
                float fromY = flight.FromY;
                float toX = slot.Value.CenterX;
                float toY = slot.Value.Top + (slot.Value.Width * 0.39f);
                float fromSize = s.PodBoxes.TryGetValue(flight.PodId, out Box pod) ? pod.Width * 0.56f : slot.Value.Width * 0.64f;
                float toSize = slot.Value.Width * 0.64f;
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
                Kit.CandyTile(p, box, flight.Variant, TileStyle.Sticker);
            }
        }
    }
}
