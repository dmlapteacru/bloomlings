using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

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
    /// The Source Tray and its pods, in the states of frame 12 (spec 002 FR-012):
    /// <list type="bullet">
    /// <item><description>exposed: bright and raised;</description></item>
    /// <item><description>next in stack: grey and smaller, peeking below;</description></item>
    /// <item><description>pressed;</description></item>
    /// <item><description>locked: a padlock;</description></item>
    /// <item><description>mystery: "?" with its count;</description></item>
    /// <item><description>connected: a teal link.</description></item>
    /// </list>
    /// Each pod keeps spec 001 FR-012's prominence: the variant symbol in ink on the Bloomling, the variant color, the
    /// count pill, then the family silhouette.
    /// </summary>
    public static class PodPainter
    {
        /// <summary>How many buried pods peek below the exposed one.</summary>
        private const int Peeks = 2;

        public static void DrawTray(IPainter p, Box area, LevelScreen s)
        {
            LevelView view = s.Session.View;
            s.PodBoxes.Clear();
            int stacks = view.StackCount;
            if (stacks == 0)
            {
                return;
            }

            float gap = p.U(20f);
            float columnWidth = (area.Width - (gap * (stacks - 1))) / stacks;
            float peek = p.U(34f);
            float size = Math.Min(Math.Min(columnWidth, p.U(250f)), area.Height - (peek * Peeks) - p.U(10f));
            float totalWidth = (size * stacks) + (gap * (stacks - 1));
            float x0 = area.CenterX - (totalWidth / 2f);
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
                float left = x0 + (st * (size + gap));
                float top = area.Top;
                // Buried pods first (behind), from the deepest shown.
                int shown = Math.Min(stack.Count - 1, Peeks);
                for (int i = shown; i >= 1; i--)
                {
                    float shrink = 1f - (0.08f * i);
                    Box back = new Box(left + (size * (1f - shrink) / 2f), top + (peek * i), left + (size * (1f + shrink) / 2f), top + (peek * i) + (size * shrink));
                    Pod(p, back, view.Pod(stack[i]), PodLook.Next, s);
                }

                if (stack.Count > Peeks + 1)
                {
                    Box more = Box.FromCenter(left + (size / 2f), top + size + (peek * Peeks) + p.U(2f), p.U(80f), p.U(40f));
                    p.FillRound(more, more.Height / 2f, C.BadgeCount.WithAlpha(0.7f));
                    p.Text("+" + (stack.Count - Peeks - 1), more.CenterX, more.CenterY, T.Badge, C.TextOnColor, more.Width * 0.9f);
                }

                if (stack.Count == 0)
                {
                    p.FillRound(new Box(left, top, left + size, top + size), size * DesignTokens.Radius.Pod, C.SurfaceSunk.WithAlpha(0.6f));
                    continue;
                }

                string id = stack[0];
                PodInfo pod = view.Pod(id);
                var box = new Box(left, top, left + size, top + size);
                s.PodBoxes[id] = box;
                bool locked = pod.Locked || s.Animator.HeldPodLocks.Contains(id);
                bool pressed = !locked && p.Pressed(box) && view.IsExposed(id);
                if (swirl < 1f)
                {
                    p.StrokeCircle(box.CenterX, box.CenterY, box.Width * (0.4f + (0.3f * swirl)), p.U(8f), C.BoosterShuffle.WithAlpha(1f - swirl));
                    p.PushTransform(0f, 0f, 0.75f + (0.25f * Kit.Ease(swirl)), box.CenterX, box.CenterY);
                }

                Pod(p, box, pod, locked ? PodLook.Locked : pressed ? PodLook.Pressed : view.IsExposed(id) ? PodLook.Exposed : PodLook.Next, s);
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

                p.Hit(box, () => s.Tap(id));
            }

            // Connected pods: a teal link between the members' cards.
            foreach (List<Box> group in linked.Values)
            {
                p.Mark("pod.link");
                for (int i = 1; i < group.Count; i++)
                {
                    Box a = group[i - 1];
                    Box b = group[i];
                    float y = a.Top + (a.Height * 0.42f);
                    p.Line(a.Right - p.U(12f), y, b.Left + p.U(12f), y, p.U(16f), Rgba.White);
                    p.Line(a.Right - p.U(12f), y, b.Left + p.U(12f), y, p.U(10f), C.StateLink);
                    p.FillCircle(a.Right - p.U(6f), y, p.U(12f), C.StateLink);
                    p.FillCircle(b.Left + p.U(6f), y, p.U(12f), C.StateLink);
                }
            }
        }

        /// <summary>One pod card in a state of frame 12.</summary>
        public static void Pod(IPainter p, Box box, PodInfo pod, PodLook look, LevelScreen? s)
        {
            p.Mark("pod.card");
            float radius = box.Width * DesignTokens.Radius.Pod;
            bool hidden = pod.Mystery && !pod.Variant.HasValue;
            if (look == PodLook.Locked)
            {
                p.Mark("pod.state.locked");
                Box face = Kit.Raised(p, box, C.StateLockBg, C.StateLockBg.Darken(0.2f), radius);
                p.Shape("ui.lock", Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.42f), face.Width * 0.46f, face.Width * 0.46f), C.StateLock.Darken(0.2f));
                CountPill(p, face, pod.Remaining, dim: true);
                return;
            }

            if (hidden || !pod.Variant.HasValue)
            {
                p.Mark("pod.state.mystery");
                Rgba card = Rgba.FromHex("#F7D6E6");
                Box face = Kit.Raised(p, box, look == PodLook.Next ? card.Grey().Lighten(0.2f) : card, card.Darken(0.18f), radius, look == PodLook.Pressed);
                p.Shape("tile.mystery", Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.42f), face.Width * 0.5f, face.Width * 0.5f), look == PodLook.Next ? C.StateStuck : Rgba.FromHex("#C0508A"));
                CountPill(p, face, pod.Remaining, look == PodLook.Next);
                return;
            }

            VariantId variant = pod.Variant.Value;
            Rgba color = Visuals.ColorOf(variant);
            bool next = look == PodLook.Next;
            Rgba tint = next ? DesignTokens.PodCard(color).Grey() : DesignTokens.PodCard(color);
            Rgba edge = next ? DesignTokens.PodCardEdge(color).Grey() : DesignTokens.PodCardEdge(color);
            Box f = Kit.Raised(p, box, tint, edge, radius, look == PodLook.Pressed);
            Rgba body = next ? color.Grey().Mix(C.StateStuck, 0.4f) : color;
            Box figure = Box.FromCenter(f.CenterX, f.Top + (f.Height * 0.42f), f.Width * 0.78f, f.Width * 0.78f);
            Visuals.Bloomling(p, figure, Visuals.FamilyOf(variant), body, Visuals.SymbolOf(variant), face: !next, symbolScale: 0.52f);
            CountPill(p, f, pod.Remaining, next);
        }

        /// <summary>The count in a dark pill on the card's lower edge.</summary>
        public static void CountPill(IPainter p, Box face, int count, bool dim)
        {
            p.Mark("pod.count");
            float h = face.Height * 0.26f;
            string text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            float w = Math.Max(h * 1.5f, p.MeasureText(text, T.Count, h / p.U(56f)) + (h * 0.8f));
            Box pill = Box.FromCenter(face.CenterX, face.Bottom - (h * 0.62f), Math.Min(w, face.Width * 0.9f), h);
            p.FillRound(pill, h / 2f, dim ? C.StateStuck : C.BadgeCount);
            p.Text(text, pill.CenterX, pill.CenterY, T.Count, C.TextOnColor, pill.Width * 0.9f, sizeScale: h / p.U(56f));
        }

        /// <summary>Committed pods flying from the tray to their slots, and returned pods flying back.</summary>
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
                float toY = slot.Value.CenterY;
                if (flight.ToTray)
                {
                    (fromX, fromY, toX, toY) = (slot.Value.CenterX, slot.Value.CenterY, flight.FromX, flight.FromY);
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

                float x = fromX + ((toX - fromX) * k);
                float y = fromY + ((toY - fromY) * k) - ((float)Math.Sin(k * Math.PI) * slot.Value.Height * 0.4f);
                float size = slot.Value.Width * (1.05f - (0.1f * k));
                Box box = Box.FromCenter(x, y, size, size);
                if (flight.Variant.HasValue)
                {
                    Rgba color = Visuals.ColorOf(flight.Variant.Value);
                    p.FillRound(box, size * DesignTokens.Radius.Pod, DesignTokens.PodCard(color));
                    Visuals.Bloomling(p, box.Inset(size * 0.12f), Visuals.FamilyOf(flight.Variant.Value), color, Visuals.SymbolOf(flight.Variant.Value));
                }
                else
                {
                    p.FillRound(box, size * DesignTokens.Radius.Pod, Rgba.FromHex("#F7D6E6"));
                    p.Shape("tile.mystery", box.Inset(size * 0.22f), Rgba.FromHex("#C0508A"));
                }
            }
        }
    }
}
