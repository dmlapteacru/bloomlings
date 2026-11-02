using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Gameplay.Rounds;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The tray's columns on a round level (spec 006 FR-064, contracts/presentation.md; the playtest's
    /// <c>RoundPainter.Columns</c>; the owner's pods of 2026-10-28, as in the reference game): a column per stack showing its first
    /// three pods as soft cubes one after another, never under each other, the top pod the only one that takes the tap and
    /// nothing for the rest (no "+N"); a pod's cube in its material's colour with its count, the waiting ones only a little
    /// lighter, a hidden pod's "?" cube until it reveals and its glow then (FR-045); the link tubes on linked cubes' front
    /// faces, under the pods (FR-043), and for a partner deeper than the rows show a stub towards its column with a link badge
    /// and a badge under that column (<see cref="RoundAnimator.LinkMarks"/>); and the pulse round the partner a refused tap
    /// waits for (round that badge while it is hidden). The pods slide up a row
    /// when the top one leaves. A latch (17 §8, FR-068; the owner's look of 2026-11-01) is drawn on the pod it holds: that pod
    /// a little darker with a padlock and our acorn hero hugging it; when it opens the padlock springs open and drops, the
    /// acorn hops off and the pod takes taps.
    /// </summary>
    public sealed partial class TrayView
    {
        private readonly Dictionary<int, (int Stack, int Depth, Box Box)> _roundPlaced = new Dictionary<int, (int, int, Box)>();
        private readonly HashSet<int> _roundShown = new HashSet<int>();
        private readonly List<Image> _roundTubes = new List<Image>();
        private readonly Dictionary<int, (Image Fill, Image Ring)> _roundGlows = new Dictionary<int, (Image, Image)>();
        private readonly Dictionary<int, Box> _roundFrames = new Dictionary<int, Box>();
        private readonly List<Image> _roundHints = new List<Image>();

        // A link partner deeper than the rows show (the owner's report of 2026-11-02): the marks, the stubs under the pods and the
        // badges over them, with the colour each badge shows.
        private readonly List<RoundLinkMark> _roundMarks = new List<RoundLinkMark>();
        private readonly List<Image> _roundStubs = new List<Image>();
        private readonly HashSet<Image> _roundHintRound = new HashSet<Image>();
        private readonly List<(Image Badge, string Color)> _roundMarkBadges = new List<(Image, string)>();
        private RectTransform? _roundBack;
        private RectTransform? _roundFront;
        private ReferenceGameplayRegions? _roundGrid;
        private readonly Dictionary<int, (bool Empty, int Rows)> _roundColumns = new Dictionary<int, (bool, int)>();

        // Latch + key (17 §8): a column's queue items, each latch's padlock, acorn and plate, and which of them show this frame.
        private readonly List<RoundQueueItem> _roundItems = new List<RoundQueueItem>();
        private readonly Dictionary<int, LatchViews> _roundLatches = new Dictionary<int, LatchViews>();
        private readonly HashSet<int> _roundLatchShown = new HashSet<int>();

        /// <summary>A shown round pod's tile box in the tray area's top-down canvas units (its flight to a place starts there).</summary>
        public Box? RoundTileBox(int pod) =>
            _pods.TryGetValue(Key(pod), out PodView? view) && view.IsShown && _roundPlaced.ContainsKey(pod)
                ? PodChip.Cube(view.Visual).Tile
                : (Box?)null;

        /// <summary>The rect of a shown round pod (the guided first tap's hole), or null.</summary>
        public RectTransform? RoundRectOf(int pod) => RectOf(Key(pod));

        /// <summary>
        /// Shows the columns of <paramref name="core"/>'s round level in <paramref name="grid"/> (the tray area's top-down
        /// canvas units, the HUD's <c>PodGrid</c>); <paramref name="interactive"/> lets the top pods take taps. Call every
        /// frame: pods are placed again only when their row changes.
        /// </summary>
        public void ShowRound(RoundLevelCore core, ReferenceGameplayRegions grid, bool interactive)
        {
            EnsureRoundLayers();
            bool force = !ReferenceEquals(grid, _roundGrid);
            _roundGrid = grid;
            RoundAnimator a = core.Animator;
            bool symbols = core.Symbols;
            float touchMin = UiKit.Units(DesignTokens.Size.TouchMin);
            int columns = Mathf.Min(a.ColumnCount, grid.Columns.Count);
            _roundShown.Clear();
            _roundLatchShown.Clear();
            if (force)
            {
                _roundColumns.Clear();
            }

            for (int s = 0; s < columns; s++)
            {
                int waiting = a.Waiting(s);
                int shownRows = Mathf.Min(waiting, grid.PodRows);
                var column = (waiting == 0, shownRows);
                bool changed = !_roundColumns.TryGetValue(s, out (bool Empty, int Rows) before) || !before.Equals(column);
                _roundColumns[s] = column;
                Image? well = s < _wells.Count ? _wells[s] : null;
                if (waiting == 0)
                {
                    if (changed)
                    {
                        Box front = grid.Pod(s, 0);
                        Box empty = front.Inset(front.Height * 0.06f);
                        well = Well(s, empty.Height);
                        BoxLayout.Place(well.rectTransform, empty);
                        well.gameObject.SetActive(true);
                        if (s < _moreDiscs.Count)
                        {
                            _moreDiscs[s].gameObject.SetActive(false);
                        }
                    }

                    continue;
                }

                if (well != null)
                {
                    well.gameObject.SetActive(false);
                }

                // The column's queue with its latches at their places (17 §8 «Засов и жёлудь-ключ», FR-068): a pod's row is its
                // depth plus the latches before it; a pod behind a closed latch is drawn waiting and takes no tap.
                a.ColumnItems(s, grid.PodRows, _roundItems);
                int rows = _roundItems.Count;
                for (int k = rows - 1; k >= 0; k--)
                {
                    RoundQueueItem item = _roundItems[k];
                    int depth = item.Row;
                    if (item.IsLatch)
                    {
                        ShowLatch(item.Id, grid.Pod(s, depth), a.LatchOpenSince(item.Id), a.LatchPips(item.Id), a.LatchClosed(item.Id) && !item.Holds);
                        _roundLatchShown.Add(item.Id);
                        continue;
                    }

                    int pod = item.Id;
                    PodView view = Get(Key(pod));
                    bool wasShown = view.IsShown;
                    int material = a.Revealed(pod) ? a.MaterialOf(pod) : -1;
                    bool held = a.HeldBehind(_roundItems, k);
                    bool exposed = item.Depth == 0 && !held;
                    bool locked = held && k > 0 && _roundItems[k - 1].IsLatch && _roundItems[k - 1].Holds && a.LatchClosed(_roundItems[k - 1].Id);
                    view.ShowRound(pod, material, a.Remaining(pod), interactive && exposed, !exposed && !locked, symbols, RoundSide(grid, s), locked);
                    Box box = grid.Pod(s, depth);
                    bool moved = !_roundPlaced.TryGetValue(pod, out (int Stack, int Depth, Box Box) placed) || placed.Stack != s || placed.Depth != depth;
                    if (force || moved || !wasShown)
                    {
                        // A pod still in its column slides from where it is drawn; one coming into the last row rises from
                        // under it, fading in; one put back on top (Return) fades in at its place; a pod that changed columns
                        // (Shuffle) is placed at once.
                        Box? from = null;
                        bool fadeIn = false;
                        if (!force && wasShown && view.Stack == s)
                        {
                            from = view.Visual;
                        }
                        else if (!force && !wasShown && (depth == 0 || depth == rows - 1))
                        {
                            from = depth == 0 ? box : grid.Pod(s, depth + 1);
                            fadeIn = true;
                        }

                        view.Place(s, box, exposed ? Touch(box, touchMin) : box, from, fadeIn);
                        _roundPlaced[pod] = (s, depth, box);
                        view.transform.SetSiblingIndex(_roundFront!.GetSiblingIndex());
                    }

                    _roundShown.Add(pod);
                }

                // No "+N" for the pods past the third row (the owner, 2026-10-28).
                if (s < _moreDiscs.Count)
                {
                    _moreDiscs[s].gameObject.SetActive(false);
                }
            }

            foreach (PodView view in _pods.Values)
            {
                if (view.IsShown && view.RoundPod >= 0 && !_roundShown.Contains(view.RoundPod))
                {
                    _roundPlaced.Remove(view.RoundPod);
                    view.Hide();
                }
            }

            foreach (KeyValuePair<int, LatchViews> latch in _roundLatches)
            {
                latch.Value.Root.gameObject.SetActive(_roundLatchShown.Contains(latch.Key));
            }

            SyncRoundOverlays(core, grid);
        }

        /// <summary>A latch's pictures over its row: the plate (no pod of its own behind it), the acorn hero and the padlock.</summary>
        private sealed class LatchViews
        {
            public RectTransform Root = null!;
            public Image Plate = null!;
            public Image Acorn = null!;
            public Image Padlock = null!;
            public bool? Open;
        }

        /// <summary>
        /// A latch (17 §8, FR-068; the playtest's <c>Kit.LatchOverlay</c>) on its row's pod box <paramref name="row"/>: our acorn
        /// hero sitting on the held pod's top right hugging the padlock (<see cref="RoundLook.LatchParts"/>,
        /// <see cref="RoundSprites.Padlock"/> with <paramref name="pips"/>), over a dim empty plate when no pod of its own stands
        /// behind it (<paramref name="plate"/>); while it opens (<paramref name="since"/> ≥ 0) the padlock springs open and drops
        /// (<see cref="RoundLook.PadlockOpen"/>) and the acorn hops off (<see cref="RoundLook.AcornHop"/>).
        /// </summary>
        private void ShowLatch(int latch, Box row, float since, int pips, bool plate)
        {
            Box frame = PodChip.Cube(row).Frame;
            float s = Mathf.Min(frame.Width, frame.Height);
            if (!_roundLatches.TryGetValue(latch, out LatchViews? views))
            {
                RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("Latch", _area));
                Image plateImage = UiKit.RoundRect("Plate", root, UiTheme.Of(C.InkBrown.WithAlpha(0.25f)), b => Mathf.Min(b.Width, b.Height) * 0.22f);
                plateImage.raycastTarget = false;
                Image acorn = UiFactory.CreateImage("Acorn", root, null, Color.white);
                acorn.raycastTarget = false;
                Sprite? hero = Bloomlings.Client.Art.CharacterSprites.Get(CharacterArt.Picture2D("acorn", CharacterMood.Happy));
                if (hero != null)
                {
                    acorn.sprite = hero;
                    acorn.preserveAspect = true;
                }
                else
                {
                    PictureFit.On(acorn, (w, h) => Bloomlings.Client.Art.RoundSprites.AcornFigure(Mathf.Min(w, h)));
                }

                Image padlock = UiFactory.CreateImage("Padlock", root, null, Color.white);
                padlock.raycastTarget = false;
                views = new LatchViews { Root = root, Plate = plateImage, Acorn = acorn, Padlock = padlock };
                _roundLatches[latch] = views;
            }

            views.Plate.gameObject.SetActive(plate);
            if (plate)
            {
                BoxLayout.Place(views.Plate.rectTransform, frame.Inset(s * 0.06f));
            }

            (Box acornBox, Box lockBox) = RoundLook.LatchParts(frame);
            (float hx, float hy, float ha) = RoundLook.AcornHop(since);
            BoxLayout.Place(views.Acorn.rectTransform, acornBox.Offset(hx * s, -hy * s));
            views.Acorn.color = new Color(1f, 1f, 1f, ha);
            (bool open, float drop, float scale, float alpha) = RoundLook.PadlockOpen(since);
            if (views.Open != open)
            {
                views.Open = open;
                int marks = pips;
                PictureFit.On(views.Padlock, (w, h) => Bloomlings.Client.Art.RoundSprites.Padlock(marks, open, Mathf.Min(w, h)));
            }

            BoxLayout.Place(views.Padlock.rectTransform, lockBox.Offset(0f, drop * s));
            views.Padlock.rectTransform.localScale = new Vector3(scale, scale, 1f);
            views.Padlock.color = new Color(1f, 1f, 1f, alpha);
            views.Root.gameObject.SetActive(true);
            views.Root.SetSiblingIndex(_roundFront!.GetSiblingIndex());
        }

        /// <summary>Forgets where the round pods stood (a restart puts the whole tray back at once).</summary>
        public void ResetRound()
        {
            _roundPlaced.Clear();
            _roundColumns.Clear();
            foreach (LatchViews latch in _roundLatches.Values)
            {
                latch.Root.gameObject.SetActive(false);
            }

            foreach (PodView view in _pods.Values)
            {
                if (view.IsShown)
                {
                    view.Hide();
                }
            }
        }

        /// <summary>The tubes, the reveal glows and the partner's pulse, from where the pods are drawn this frame.</summary>
        private void SyncRoundOverlays(RoundLevelCore core, ReferenceGameplayRegions grid)
        {
            RoundAnimator a = core.Animator;
            _roundFrames.Clear();
            foreach (int pod in _roundShown)
            {
                if (_pods.TryGetValue(Key(pod), out PodView? view) && _roundPlaced.ContainsKey(pod))
                {
                    _roundFrames[pod] = PodChip.Cube(view.Visual).Frame;
                }
            }

            // The tubes first (the back layer), so they pass under the pods (FR-043).
            int used = 0;
            foreach (RoundTube tube in a.Tubes(grid.PodRows))
            {
                if (tube.Place != RoundTubePlace.Columns || !_roundFrames.TryGetValue(tube.First, out Box first) || !_roundFrames.TryGetValue(tube.Second, out Box second))
                {
                    continue;
                }

                if (used == _roundTubes.Count)
                {
                    _roundTubes.Add(RoundTubes.Create("LinkTube", _roundBack!));
                }

                RoundTubes.Show(_roundTubes[used++], LinkTube.BetweenCubes(first, RoundLook.ColorOf(a.MaterialOf(tube.First)), second, RoundLook.ColorOf(a.MaterialOf(tube.Second))));
            }

            for (int i = used; i < _roundTubes.Count; i++)
            {
                _roundTubes[i].gameObject.SetActive(false);
            }

            // A partner deeper than the rows show (the owner's report of 2026-11-02, LinkTube.MarkSlot): a stub out of the shown
            // pod towards the partner's column under the pods, a link badge on its end and one under the partner column's last
            // shown pod over them.
            a.LinkMarks(grid.PodRows, _roundMarks);
            int stubs = 0;
            int badges = 0;
            foreach (RoundLinkMark mark in _roundMarks)
            {
                Rgba color = RoundLook.ColorOf(mark.Material);
                Box badgeBox;
                if (mark.Kind == RoundLinkMarkKind.Stub && _roundFrames.TryGetValue(mark.Pod, out Box shown))
                {
                    if (stubs == _roundStubs.Count)
                    {
                        _roundStubs.Add(RoundTubes.Create("LinkStub", _roundBack!));
                    }

                    RoundTubes.Show(_roundStubs[stubs++], LinkTube.Stub(shown, color, mark.ToRight));
                    badgeBox = LinkTube.StubBadge(shown, mark.ToRight);
                }
                else if (mark.Kind == RoundLinkMarkKind.Waiting && mark.Column < grid.Columns.Count)
                {
                    badgeBox = LinkTube.WaitingBadge(LastShown(grid, mark.Column), mark.Order, mark.Of);
                }
                else
                {
                    continue;
                }

                if (badges == _roundMarkBadges.Count)
                {
                    Image created = UiKit.LinkBadge("LinkAway", _area, color);
                    created.raycastTarget = false;
                    _roundMarkBadges.Add((created, color.Hex));
                }

                (Image badge, string shownColor) = _roundMarkBadges[badges];
                if (shownColor != color.Hex)
                {
                    UiKit.SetLinkBadge(badge, color);
                    _roundMarkBadges[badges] = (badge, color.Hex);
                }

                badges++;
                BoxLayout.Place(badge.rectTransform, badgeBox);
                badge.rectTransform.localScale = Vector3.one;
                badge.gameObject.SetActive(true);
                badge.transform.SetSiblingIndex(_roundFront!.GetSiblingIndex());
            }

            for (int i = stubs; i < _roundStubs.Count; i++)
            {
                _roundStubs[i].gameObject.SetActive(false);
            }

            for (int i = badges; i < _roundMarkBadges.Count; i++)
            {
                _roundMarkBadges[i].Badge.gameObject.SetActive(false);
            }

            // The reveal glow behind a pod that has just revealed (FR-045).
            foreach (KeyValuePair<int, (Image Fill, Image Ring)> glow in _roundGlows)
            {
                bool on = _roundFrames.ContainsKey(glow.Key) && core.RevealGlow(glow.Key) > 0f;
                glow.Value.Fill.gameObject.SetActive(on);
                glow.Value.Ring.gameObject.SetActive(on);
            }

            foreach (KeyValuePair<int, Box> frame in _roundFrames)
            {
                float strength = core.RevealGlow(frame.Key);
                if (strength <= 0f)
                {
                    continue;
                }

                if (!_roundGlows.TryGetValue(frame.Key, out (Image Fill, Image Ring) parts))
                {
                    Image fill = UiKit.RoundRect("RevealGlow", _roundBack!, Color.white, b => Mathf.Min(b.Width, b.Height) * 0.3f);
                    Image ring = UiKit.RoundRing("RevealRing", _roundBack!, Color.white, b => Mathf.Min(b.Width, b.Height) * 0.25f, b => Mathf.Min(b.Width, b.Height) * 0.035f);
                    fill.raycastTarget = false;
                    ring.raycastTarget = false;
                    parts = (fill, ring);
                    _roundGlows[frame.Key] = parts;
                }

                Box f = frame.Value;
                float s = Mathf.Min(f.Width, f.Height);
                BoxLayout.Place(parts.Fill.rectTransform, f.Inset(-s * 0.14f));
                BoxLayout.Place(parts.Ring.rectTransform, f.Inset(-s * 0.03f));
                Color glowColor = UiTheme.Of(C.GardenGlow);
                parts.Fill.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.45f * strength);
                parts.Ring.color = new Color(glowColor.r, glowColor.g, glowColor.b, strength);
                parts.Fill.gameObject.SetActive(true);
                parts.Ring.gameObject.SetActive(true);
            }

            // The pulse round the partner a refused tap waits for, or, while it waits deeper than the rows show, round its link
            // badge under its column's last shown pod (FR-008; the owner's report of 2026-11-02).
            int hints = 0;
            float k = core.LinkPulse;
            if (k > 0f && core.LinkHint.HasValue)
            {
                int tapped = core.LinkHint.Value.Pod;
                foreach (int member in a.GroupOf(tapped))
                {
                    if (member == tapped)
                    {
                        continue;
                    }

                    Box ring;
                    bool round = false;
                    Rgba color = RoundLook.ColorOf(a.MaterialOf(member));
                    if (_roundFrames.TryGetValue(member, out Box frame))
                    {
                        ring = frame;
                    }
                    else if (WaitingMark(member, out RoundLinkMark mark, out int index) && mark.Column < grid.Columns.Count)
                    {
                        // Hidden deeper than the rows show: its badge under the column swells, a ring pulsing round it.
                        Box badge = LinkTube.WaitingBadge(LastShown(grid, mark.Column), mark.Order, mark.Of);
                        float swell = 1f + (0.36f * k);
                        _roundMarkBadges[index].Badge.rectTransform.localScale = new Vector3(swell, swell, 1f);
                        color = RoundLook.ColorOf(mark.Material);
                        float g = badge.Height * (0.12f + (0.22f * k));
                        ring = badge.Inset(-g);
                        round = true;
                    }
                    else if (a.ColumnOf(member) >= 0 && a.ColumnOf(member) < grid.Columns.Count)
                    {
                        ring = grid.Pod(a.ColumnOf(member), grid.PodRows - 1);
                    }
                    else
                    {
                        continue;
                    }

                    if (hints == _roundHints.Count)
                    {
                        Image created = null!;
                        created = UiKit.RoundRing("LinkPulse", _roundFront!, Color.white, b => b.Height * (_roundHintRound.Contains(created) ? 0.5f : 0.24f), _ => UiKit.Units(4f));
                        created.raycastTarget = false;
                        _roundHints.Add(created);
                    }

                    Image hint = _roundHints[hints++];
                    if (round) _roundHintRound.Add(hint); else _roundHintRound.Remove(hint);
                    float grow = round ? 0f : ring.Height * (0.06f + (0.1f * k));
                    BoxLayout.Place(hint.rectTransform, ring.Inset(-grow));
                    hint.color = UiTheme.Of(color.WithAlpha(0.35f + (0.65f * k)));
                    hint.gameObject.SetActive(true);
                    hint.transform.SetAsLastSibling();
                }
            }

            for (int i = hints; i < _roundHints.Count; i++)
            {
                _roundHints[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// The waiting badge (<see cref="RoundLinkMarkKind.Waiting"/>) standing for hidden pod <paramref name="pod"/> this frame and
        /// its index among the shown mark badges.
        /// </summary>
        private bool WaitingMark(int pod, out RoundLinkMark found, out int index)
        {
            index = 0;
            foreach (RoundLinkMark mark in _roundMarks)
            {
                bool drawn = mark.Kind == RoundLinkMarkKind.Waiting || _roundFrames.ContainsKey(mark.Pod);
                if (mark.Kind == RoundLinkMarkKind.Waiting && mark.Pod == pod && index < _roundMarkBadges.Count)
                {
                    found = mark;
                    return true;
                }

                index += drawn ? 1 : 0;
            }

            found = default;
            return false;
        }

        /// <summary>The cube of column <paramref name="stack"/>'s last shown row, where a hidden link partner's badge stands under.</summary>
        private static Box LastShown(ReferenceGameplayRegions grid, int stack) => PodChip.Cube(grid.Pod(stack, grid.PodRows - 1)).Frame;

        private static string Key(int pod) => pod.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The side face's step of column <paramref name="stack"/> (constitution 1.2.0 VII): by its distance from the screen's
        /// middle, which the columns are centred on, half the screen's width being the full step.
        /// </summary>
        private static int RoundSide(ReferenceGameplayRegions grid, int stack)
        {
            Box first = grid.Columns[0];
            Box last = grid.Columns[grid.Columns.Count - 1];
            float middle = (first.Left + last.Right) / 2f;
            return RoundLook.SideStep(grid.Columns[stack].CenterX - middle, grid.W / 2f);
        }

        /// <summary>The layer under the pods (tubes, glows) and the marker the pods are kept before (the pulse goes over them).</summary>
        private void EnsureRoundLayers()
        {
            if (_roundBack != null)
            {
                return;
            }

            _roundBack = UiFactory.Stretch(UiFactory.CreateRect("RoundBack", _area));
            _roundBack.SetAsFirstSibling();
            _roundFront = UiFactory.Stretch(UiFactory.CreateRect("RoundFront", _area));
            _roundFront.SetAsLastSibling();
        }
    }
}
