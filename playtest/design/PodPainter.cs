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
    /// <item><description>next in stack: in its variant color, muted, fully visible below the exposed one (spec 003
    /// FR-022a);</description></item>
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
            // top and the pods that follow it below, never overlapping, so the player sees what each choice uncovers.
            TrayGrid grid = ScreenLayout.Tray(area, stacks, p.Scale);
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
                    Box empty = grid.Cell(st, 0);
                    p.FillRound(empty, empty.Width * DesignTokens.Radius.Pod, C.SurfaceSunk.WithAlpha(0.6f));
                    continue;
                }

                int shown = Math.Min(stack.Count, ScreenLayout.TrayRows);
                for (int depth = 0; depth < shown; depth++)
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

                    Pod(p, box, pod, locked ? PodLook.Locked : pressed ? PodLook.Pressed : exposed ? PodLook.Exposed : PodLook.Next, s);
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

                // Deeper pods are not drawn; a "+N" badge on the last shown pod says how many more wait there. It sits in
                // the bottom-left corner, clear of the pod's "xN" (spec 004 FR-008).
                if (stack.Count > shown)
                {
                    Box last = grid.Cell(st, shown - 1);
                    float h = last.Height * 0.3f;
                    string more = "+" + (stack.Count - shown).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Kit.CountBadge(p, last.Left + (h * 0.4f), last.Bottom - (h * 0.4f), h, more);
                }
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
                Box face = Kit.Block(p, box, C.StateLockBg, C.StateLockBg.Darken(0.2f), radius, Kit.PodLip(p, box.Height), 0.35f);
                p.Shape("ui.lock", Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.42f), face.Width * 0.46f, face.Width * 0.46f), C.StateLock.Darken(0.2f));
                CountPill(p, face, pod.Remaining, dim: true);
                return;
            }

            if (hidden || !pod.Variant.HasValue)
            {
                p.Mark("pod.state.mystery");
                Rgba card = C.PodMystery;
                Box face = Kit.Block(p, box, look == PodLook.Next ? card.Grey().Lighten(0.2f) : card, card.Darken(0.18f), radius, Kit.PodLip(p, box.Height), look == PodLook.Next ? 0.25f : 0.55f, look == PodLook.Pressed);
                p.Shape("tile.mystery", Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.42f), face.Width * 0.5f, face.Width * 0.5f), look == PodLook.Next ? C.StateStuck : C.PodMysteryMark);
                CountPill(p, face, pod.Remaining, look == PodLook.Next);
                return;
            }

            VariantId variant = pod.Variant.Value;
            Rgba color = Visuals.ColorOf(variant);
            bool next = look == PodLook.Next;

            // A pod still in its stack keeps its variant color, muted, so what comes next reads at a glance (FR-022a).
            Rgba shown = next ? DesignTokens.PodQueued(color) : color;
            Rgba tint = DesignTokens.PodCard(shown);
            Rgba edge = DesignTokens.PodCardEdge(shown);
            // A volumetric 2D card (spec 003 FR-022): a thick lip, a bevel and a highlight; never 3D.
            Box f = Kit.Block(p, box, tint, edge, radius, Kit.PodLip(p, box.Height), next ? 0.25f : 0.55f, look == PodLook.Pressed);
            // The variant's character (spec 004 FR-008): awake on an exposed pod, asleep in the stack, and "xN" in the corner.
            Visuals.Character(p, CharacterArt.OnCard(f), variant, next ? CharacterMood.Asleep : CharacterMood.Happy);
            Count(p, f, pod.Remaining);
        }

        /// <summary>"xN" in the card face's bottom-right corner (spec 004 FR-008, data-model.md "PodCount").</summary>
        public static void Count(IPainter p, Box face, int count)
        {
            p.Mark("pod.count");
            Box box = CharacterArt.CountBox(face);
            string text = PlaytestText.F("pod.count", count);
            float scale = box.Height * 0.9f / p.U(T.Count.Size);
            float width = p.MeasureText(text, T.Count, scale);
            float right = box.Right;
            float shown = Math.Min(width, box.Width);
            p.Text(text, right - (shown / 2f), box.CenterY, T.Count, CharacterArt.CountColor, box.Width, scale, CharacterArt.CountLook);
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
                    Visuals.Character(p, CharacterArt.OnCard(box), flight.Variant.Value, CharacterMood.Happy);
                }
                else
                {
                    p.FillRound(box, size * DesignTokens.Radius.Pod, C.PodMystery);
                    p.Shape("tile.mystery", box.Inset(size * 0.22f), C.PodMysteryMark);
                }
            }
        }
    }
}
