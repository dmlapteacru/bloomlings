using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
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
    /// The Source Tray as the reference's row of decks (spec 005 FR-021, contracts/look.md §3.7, §6.1; frame 12), one deck
    /// per stack in <see cref="ReferenceGameplayRegions.Decks"/>:
    /// <list type="bullet">
    /// <item><description>the exposed pod in front: a dark wooden frame, a panel tinted by the variant, the variant's
    /// sticker tile and the plain count below it; it squashes under the finger and is the only pod that takes a tap
    /// (spec 001 FR-011);</description></item>
    /// <item><description>up to two buried pods as wooden frames peeking above it, each showing the frame's top edge and a
    /// band of its variant color with its small symbol (identity never by hue alone, spec 001 FR-072);</description></item>
    /// <item><description>a "+N" count badge on the deck's top right for the pods beyond those two;</description></item>
    /// <item><description>an emptied stack as a sunk well;</description></item>
    /// <item><description>locked: the padlock on a grey panel (a grey band when buried); mystery: the lilac "?" tile (a
    /// lilac band with "?"); connected: a link between the frames, in its group's color.</description></item>
    /// </list>
    /// Committed pods fly from the front pod's tile to their slot's tile.
    /// </summary>
    public static class PodPainter
    {
        /// <summary>The count's type size as a share of its room under the tile (the reference's big dark digits).</summary>
        public const float CountFill = 1.2f;

        /// <summary>The buried pods' frame top edge, as a share of its band; the variant's band fills the rest.</summary>
        public const float BandRail = 0.3f;

        /// <summary>The small symbol on a buried pod's band, as a share of the band's height (at most 70%, §6.1).</summary>
        public const float BandSymbol = 0.7f;

        /// <summary>
        /// How far the variant's color is lightened for the top of a deck pod's panel: clearly tinted, keeping the color's
        /// hue (the reference's lime, pink, sky blue and orange panels; spec 005 FR-020).
        /// </summary>
        public const float PanelTop = 0.5f;

        /// <summary>How far the variant's color is lightened for the bottom of the panel, under the count.</summary>
        public const float PanelBottom = 0.8f;

        /// <summary>How many buried pods a deck shows behind its front pod.</summary>
        public const int BuriedShown = 2;

        /// <summary>
        /// The decks of the level's Source stacks in the tray's pod row (<see cref="ReferenceGameplayRegions.Decks"/>):
        /// each stack's buried pods, its front pod (squashing under the finger, taking the tap when exposed) and its "+N"
        /// badge, or a well for an emptied stack; then the links of the connected groups. Records where each pod's tile
        /// was drawn in <see cref="LevelScreen.PodBoxes"/>, where flights start.
        /// </summary>
        public static void DrawDecks(IPainter p, ReferenceGameplayRegions r, LevelScreen s)
        {
            LevelView view = s.Session.View;
            s.PodBoxes.Clear();
            int stacks = Math.Min(view.StackCount, r.Decks.Count);
            var linked = new Dictionary<string, List<Box>>(StringComparer.Ordinal);

            // Shuffle: the decks swirl in place for a moment.
            float swirl = s.LastBooster.HasValue && s.LastBooster.Value.Kind == Client.Services.Save.BoosterKind.Shuffle ? (s.Animator.Now - s.LastBooster.Value.At) / 0.45f : 1f;
            if (swirl < 1f)
            {
                p.Mark("fx.shuffle_swirl");
            }

            for (int st = 0; st < stacks; st++)
            {
                PodDeck deck = r.Deck(st);
                IReadOnlyList<string> stack = view.Stack(st);
                if (stack.Count == 0)
                {
                    EmptyDeck(p, deck);
                    continue;
                }

                if (swirl < 1f)
                {
                    p.StrokeCircle(deck.Deck.CenterX, deck.Deck.CenterY, deck.Deck.Width * (0.4f + (0.3f * swirl)), p.U(6f), C.BoosterShuffle.WithAlpha(1f - swirl));
                    p.PushTransform(0f, 0f, 0.75f + (0.25f * Kit.Ease(swirl)), deck.Deck.CenterX, deck.Deck.CenterY);
                }

                // The buried pods, the deepest first, so each lies under the one in front of it.
                for (int depth = Math.Min(stack.Count - 1, BuriedShown); depth >= 1; depth--)
                {
                    string buriedId = stack[depth];
                    PodInfo buried = view.Pod(buriedId);
                    bool buriedLocked = buried.Locked || s.Animator.HeldPodLocks.Contains(buriedId);
                    s.PodBoxes[buriedId] = Buried(p, deck, depth, Shown(buried), buriedLocked);
                    Link(linked, buried, deck.Band(depth));
                }

                // The front pod squashes under the finger and springs back (spec 003 FR-017).
                string id = stack[0];
                PodInfo pod = view.Pod(id);
                bool exposed = view.IsExposed(id);
                bool locked = pod.Locked || s.Animator.HeldPodLocks.Contains(id);
                bool live = exposed && !locked;
                Box touch = Kit.Touch(p, deck.Front);
                float press = live ? Kit.Press(p, touch, true) : 0f;
                Kit.Squash(p, deck.Front, press, tile: true);
                PodLook look = locked ? PodLook.Locked : live && p.Pressed(deck.Front) ? PodLook.Pressed : exposed ? PodLook.Exposed : PodLook.Next;
                s.PodBoxes[id] = Front(p, deck, Shown(pod), pod.Remaining, look);
                p.PopTransform();
                Link(linked, pod, deck.Front);
                if (swirl < 1f)
                {
                    p.PopTransform();
                }

                // The pods beyond the two shown wait under a "+N" badge on the deck's top right.
                int more = stack.Count - 1 - BuriedShown;
                if (more > 0)
                {
                    Kit.CountBadge(p, deck.Badge.CenterX, deck.Badge.CenterY, deck.Badge.Height, "+" + more.ToString(CultureInfo.InvariantCulture));
                }

                if (exposed)
                {
                    // Only the exposed pod takes the tap; a locked one answers with its refusal.
                    p.Hit(touch, () => s.Tap(id));
                }
            }

            DrawLinks(p, view, linked);
        }

        /// <summary>
        /// A deck drawn from plain data (the component sheet, frame 12): <paramref name="pods"/> top first (the front pod,
        /// then the buried ones; a null variant is a hidden mystery pod), <paramref name="total"/> pods in the stack (more
        /// than three shows the "+N" badge), the front pod in <paramref name="look"/>. No taps.
        /// </summary>
        public static void DrawDeck(IPainter p, PodDeck deck, IReadOnlyList<(VariantId? Variant, int Count, bool Locked)> pods, int total, PodLook look)
        {
            if (pods.Count == 0)
            {
                EmptyDeck(p, deck);
                return;
            }

            for (int depth = Math.Min(pods.Count - 1, BuriedShown); depth >= 1; depth--)
            {
                Buried(p, deck, depth, pods[depth].Variant, pods[depth].Locked);
            }

            Front(p, deck, pods[0].Variant, pods[0].Count, pods[0].Locked ? PodLook.Locked : look);
            int more = total - 1 - BuriedShown;
            if (more > 0)
            {
                Kit.CountBadge(p, deck.Badge.CenterX, deck.Badge.CenterY, deck.Badge.Height, "+" + more.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>A pod's variant as the player sees it: null while a mystery pod is hidden (FR-039).</summary>
        private static VariantId? Shown(PodInfo pod) => pod.Mystery && !pod.Variant.HasValue ? null : pod.Variant;

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

        /// <summary>
        /// The front pod of a deck (§3.7, §6.1) in <paramref name="deck"/>: the wooden frame filling
        /// <see cref="PodDeck.Front"/> with its panel tinted by the variant, the sticker tile in <see cref="PodDeck.Tile"/>
        /// and the count below it in big <c>ink.brown</c> digits. A null <paramref name="variant"/> is a mystery pod (the
        /// lilac "?" tile); a locked pod shows the padlock; a queued one is dimmed; a pressed one sinks. Returns where the
        /// tile was drawn.
        /// </summary>
        public static Box Front(IPainter p, PodDeck deck, VariantId? variant, int count, PodLook look)
        {
            p.Mark("pod.card");
            bool queued = look == PodLook.Next;
            Rgba? tint = variant.HasValue && !queued && look != PodLook.Locked ? Visuals.ColorOf(variant.Value) : (Rgba?)null;
            Box panel = Frame(p, deck.Front, look, tint);

            // A pressed frame sinks: its tile and count go with it.
            float sink = panel.Top - deck.Inner.Top;
            Box tile = deck.Tile.Offset(0f, sink);
            Box countBox = deck.Count.Offset(0f, sink);
            if (look == PodLook.Locked)
            {
                p.Mark("pod.state.locked");
                float g = tile.Width * 0.62f;
                p.Shape("ui.lock", Box.FromCenter(tile.CenterX, tile.CenterY, g, g), C.StateLock.Darken(0.2f));
            }
            else
            {
                if (!variant.HasValue)
                {
                    p.Mark("pod.state.mystery");
                }

                Kit.CandyTile(p, tile, variant, TileStyle.Sticker, queued ? TileState.Dimmed : TileState.Normal, pressed: look == PodLook.Pressed);
                if (queued && !variant.HasValue)
                {
                    // The mystery tile has no dimmed picture: a veil of the parchment dims it like the others.
                    p.FillRound(tile, tile.Width * 0.2f, C.ParchmentBottom.WithAlpha(0.45f));
                }
            }

            Kit.CountBelow(p, countBox, count, queued || look == PodLook.Locked, CountFill);
            return tile;
        }

        /// <summary>
        /// A deck pod's wooden frame (<see cref="Kit.PodFrame"/>, no handle: the buried frames peek above it) with its panel
        /// in the variant's <paramref name="color"/> lightened (<see cref="PanelTop"/> to <see cref="PanelBottom"/>), or
        /// plain cream without a color. Returns the panel.
        /// </summary>
        private static Box Frame(IPainter p, Box box, PodLook look, Rgba? color) =>
            Kit.PodFrame(p, box, look, handle: false, color?.Lighten(PanelTop), 1f, color?.Lighten(PanelBottom));

        /// <summary>
        /// A buried pod of a deck (<c>pod.deck</c>, FR-021): its wooden frame (<see cref="PodDeck.Buried"/>) behind the pod
        /// in front, of which only the band <see cref="PodDeck.Band"/> shows: the frame's top edge and, under it, a strip of
        /// the pod's variant color with its small white symbol outlined in the variant's dark shade, so the next pods read
        /// by symbol and color. A hidden mystery pod shows a lilac strip with "?", a locked one a grey strip with the
        /// padlock. Returns the symbol's box.
        /// </summary>
        public static Box Buried(IPainter p, PodDeck deck, int depth, VariantId? variant, bool locked)
        {
            p.Mark("pod.deck");
            Box frame = deck.Buried(depth);
            Box band = deck.Band(depth);
            Frame(p, frame, PodLook.Exposed, variant.HasValue && !locked ? Visuals.ColorOf(variant.Value) : (Rgba?)null);

            // The strip runs from under the frame's top edge into the pod in front, which covers its lower part.
            float w = frame.Width;
            float inset = w * PodDeck.Border * 0.8f;
            float top = band.Top + (band.Height * BandRail);
            var strip = new Box(frame.Left + inset, top, frame.Right - inset, band.Bottom + (band.Height * 0.6f));
            Rgba color = locked ? C.StateLockBg : variant.HasValue ? Visuals.ColorOf(variant.Value) : C.TileMystery;
            float radius = Math.Min(strip.Height / 2f, w * 0.06f);
            float line = Math.Max(1f, w * 0.012f);
            p.FillRound(strip.Inset(-line), radius + line, color.Darken(0.45f));
            p.FillRoundGradient(strip, radius, color.Lighten(0.4f), color.Lighten(0.12f));

            // The symbol's shape spans about 70% of its box, so the box is as tall as the band's visible strip and the
            // symbol itself about 70% of the band; small, it reads best as a dark silhouette (as the board's small gems).
            float size = (band.Bottom - top) * BandSymbol / 0.7f;
            Box symbol = Box.FromCenter(band.CenterX, (top + band.Bottom) / 2f, size, size);
            if (locked)
            {
                p.Mark("pod.state.locked");
                p.Shape("ui.lock", symbol, C.StateLock.Darken(0.25f));
            }
            else if (variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info))
            {
                string shape = ShapeLibrary.SymbolId(info.IconId);
                Func<float, float, float> sdf = ShapeLibrary.Get(shape);
                p.ShapeOf(shape + "/band", (x, y) => sdf(x, y) - 0.14f, symbol, color.Lighten(0.55f));
                p.Shape(shape, symbol, color.Darken(0.52f));
            }
            else
            {
                p.Mark("pod.state.mystery");
                Func<float, float, float> sdf = ShapeLibrary.Get("tile.mystery");
                p.ShapeOf("tile.mystery/band", (x, y) => sdf(x, y) - 0.16f, symbol, color.Darken(0.45f));
                p.Shape("tile.mystery", symbol, Rgba.White);
            }

            // The buried pods sit a little in the shade of the one in front.
            p.FillRound(new Box(frame.Left, band.Top, frame.Right, band.Bottom), w * 0.18f, C.GardenShadow.WithAlpha(0.06f * depth));
            return symbol;
        }

        /// <summary>An emptied stack (§6.1): a sunk place of parchment where its front pod stood.</summary>
        public static void EmptyDeck(IPainter p, PodDeck deck)
        {
            p.Mark("pod.deck");
            Box well = deck.Front.Inset(deck.Front.Width * 0.04f);
            Kit.Well(p, well, well.Width * 0.18f, C.ParchmentWell.Mix(C.ParchmentEdge, 0.5f));
        }

        /// <summary>
        /// The links of the connected groups (spec 002 FR-012): a bar between consecutive members drawn on one row (their
        /// fronts or, when buried, their bands), and a small ring of the link's color on each member when a group spans
        /// two rows of decks. Each group has its own color (state.link, state.link_2, state.link_3), by the groups' order,
        /// as in Unity's TrayView.
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

                p.Mark("pod.link");
                foreach (Box box in boxes)
                {
                    float d = Math.Min(box.Width, box.Height) * 0.16f;
                    p.FillCircle(box.Left + d, box.Top + d, d * 1.25f, Rgba.White);
                    p.FillCircle(box.Left + d, box.Top + d, d, color);
                }
            }
        }

        /// <summary>The connected groups' link colors, in order (the Unity tray's palette).</summary>
        private static readonly Rgba[] LinkPalette = { C.StateLink, C.StateLink2, C.StateLink3 };

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
        /// One pod in a state of frame 12 outside a deck (<see cref="Kit.Pod"/>): a mystery pod still hidden shows the "?"
        /// tile. <paramref name="handle"/> puts the wooden handle on top.
        /// </summary>
        public static void Pod(IPainter p, Box box, PodInfo pod, PodLook look, LevelScreen? s, bool handle = true) =>
            Kit.Pod(p, box, Shown(pod), pod.Remaining, look, handle);

        /// <summary>
        /// The link between two connected pods (<c>pod.link</c>, spec 002 FR-012): a rounded bar with a white rim across the
        /// gap between their frames, a little above their middle, riveted to each frame, in its group's
        /// <paramref name="color"/> (teal <c>state.link</c> by default).
        /// </summary>
        public static void Link(IPainter p, Box a, Box b, Rgba? color = null)
        {
            Rgba link = color ?? C.StateLink;
            p.Mark("pod.link");
            float size = Math.Min(a.Width, b.Width);
            float y = a.Top + (a.Height * 0.42f);
            float x0 = a.Right - (size * 0.08f);
            float x1 = b.Left + (size * 0.08f);
            float bar = Math.Min(size * 0.1f, a.Height * 0.8f);
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
        /// lilac "?" while still hidden) leaves the deck's front pod at its size there, arcs over and shrinks to the slot's
        /// tile, over a soft shadow.
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
}
