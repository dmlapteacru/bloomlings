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
    /// faces, under the pods (FR-043), and for a partner deeper than the rows show one straight tube to the bottom edge of its
    /// column's last shown row (<see cref="RoundAnimator.LinkMarks"/>, <see cref="LinkTube.HiddenTube"/>); and the pulse round
    /// the partner a refused tap waits for (along that tube and at its end while it is hidden). The pods slide up a row
    /// when the top one leaves. A closed latch (17 §8, FR-068; the owner's look of 2026-11-02) hides the pod it holds: in its
    /// place stands our Twig hero in the player's outfit holding a padlock in that pod's colour (<see cref="LatchHeroView"/>);
    /// when it opens the key flies into the padlock (<c>RoundFlightsView</c>), the shackle springs open and the padlock drops,
    /// Twig steps aside from the pod, which takes taps from the event on.
    /// </summary>
    public sealed partial class TrayView
    {
        private readonly Dictionary<int, (int Stack, int Depth, Box Box)> _roundPlaced = new Dictionary<int, (int, int, Box)>();
        private readonly HashSet<int> _roundShown = new HashSet<int>();
        private readonly List<Image> _roundTubes = new List<Image>();
        private readonly Dictionary<int, (Image Fill, Image Ring)> _roundGlows = new Dictionary<int, (Image, Image)>();
        private readonly Dictionary<int, Box> _roundFrames = new Dictionary<int, Box>();
        private readonly List<Image> _roundHints = new List<Image>();

        // A link partner deeper than the rows show (the owner's reviews of 2026-11-02): the pairs and their straight tubes under
        // the pods.
        private readonly List<RoundLinkMark> _roundMarks = new List<RoundLinkMark>();
        private readonly List<Image> _roundRoutes = new List<Image>();
        private readonly HashSet<Image> _roundHintRound = new HashSet<Image>();
        private RectTransform? _roundBack;
        private RectTransform? _roundFront;
        private ReferenceGameplayRegions? _roundGrid;
        private readonly Dictionary<int, (bool Empty, int Rows)> _roundColumns = new Dictionary<int, (bool, int)>();

        // Latch + key (17 §8): a column's queue items, each latch's hero, which of them show this frame and their padlocks.
        private readonly List<RoundQueueItem> _roundItems = new List<RoundQueueItem>();
        private readonly Dictionary<int, LatchHeroView> _roundLatches = new Dictionary<int, LatchHeroView>();
        private readonly HashSet<int> _roundLatchShown = new HashSet<int>();
        private readonly Dictionary<int, Box> _roundLatchLocks = new Dictionary<int, Box>();

        /// <summary>The player's equipped outfit of a family (the wardrobe's); the latch's Twig hero wears his (stock Twig while null).</summary>
        public System.Func<Bloomlings.Core.Variants.Family, Bloomlings.Client.Meta.Wardrobe.Outfit>? OutfitOf { get; set; }

        /// <summary>
        /// The padlock box of latch <paramref name="latch"/> in the tray area's top-down canvas units while its hero is shown (the
        /// opening key's flight ends in its keyhole), else null.
        /// </summary>
        public Box? RoundLatchLock(int latch) => _roundLatchLocks.TryGetValue(latch, out Box box) ? box : (Box?)null;

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
            _roundLatchLocks.Clear();
            Bloomlings.Client.Meta.Wardrobe.Outfit? outfit = OutfitOf?.Invoke(Bloomlings.Core.Variants.Family.Twig);
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
                        Box place = grid.Pod(s, depth);
                        Rgba body = item.Holds ? RoundLook.KeyMetal(a.KeyMaterial(item.Id)) : RoundLook.KeyBrass;
                        ShowLatch(item.Id).Show(place, outfit, body, a.LatchPips(item.Id), a.LatchOpenSince(item.Id), RoundLook.HeroSide(s, columns), depth > 0);
                        _roundLatchShown.Add(item.Id);
                        _roundLatchLocks[item.Id] = RoundLook.HeroLock(RoundLook.HeroCell(place));
                        continue;
                    }

                    int pod = item.Id;
                    bool held = a.HeldBehind(_roundItems, k);
                    bool locked = held && k > 0 && _roundItems[k - 1].IsLatch && _roundItems[k - 1].Holds && a.LatchClosed(_roundItems[k - 1].Id);
                    if (locked)
                    {
                        // the pod a closed latch holds is not drawn: its hero stands in its place (FR-068, 2026-11-02)
                        continue;
                    }

                    PodView view = Get(Key(pod));
                    bool wasShown = view.IsShown;
                    int material = a.Revealed(pod) ? a.MaterialOf(pod) : -1;
                    bool exposed = item.Depth == 0 && !held;
                    view.ShowRound(pod, material, a.Remaining(pod), interactive && exposed, !exposed, symbols, RoundSide(grid, s));
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

            foreach (KeyValuePair<int, LatchHeroView> latch in _roundLatches)
            {
                if (!_roundLatchShown.Contains(latch.Key))
                {
                    latch.Value.Hide();
                }
                else
                {
                    // over every pod (its head over the row above; while it opens, in front of the pod it held)
                    latch.Value.Root.SetSiblingIndex(_roundFront!.GetSiblingIndex());
                }
            }

            SyncRoundOverlays(core, grid);
        }

        /// <summary>Latch <paramref name="latch"/>'s hero view (made on first use), over the pods.</summary>
        private LatchHeroView ShowLatch(int latch)
        {
            if (!_roundLatches.TryGetValue(latch, out LatchHeroView? view))
            {
                view = LatchHeroView.Create(_area);
                _roundLatches[latch] = view;
            }

            return view;
        }

        /// <summary>Forgets where the round pods stood (a restart puts the whole tray back at once).</summary>
        public void ResetRound()
        {
            _roundPlaced.Clear();
            _roundColumns.Clear();
            foreach (LatchHeroView latch in _roundLatches.Values)
            {
                latch.Hide();
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

            // A partner deeper than the rows show (the owner's reviews of 2026-11-02, LinkTube.MarkSlot): one straight tube from the
            // shown pod to the bottom edge of the partner column's last shown row, under the pods.
            a.LinkMarks(grid.PodRows, _roundMarks);
            int pieces = 0;
            foreach (RoundLinkMark mark in _roundMarks)
            {
                if (!_roundFrames.TryGetValue(mark.Pod, out Box shown) || mark.Column >= grid.Columns.Count)
                {
                    continue;
                }

                if (pieces == _roundRoutes.Count)
                {
                    _roundRoutes.Add(RoundTubes.Create("LinkAway", _roundBack!));
                }

                RoundTubes.Show(_roundRoutes[pieces++], LinkTube.HiddenTube(grid, mark, shown), stepped: false);
            }

            for (int i = pieces; i < _roundRoutes.Count; i++)
            {
                _roundRoutes[i].gameObject.SetActive(false);
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

            // The pulse round the partner a refused tap waits for, or, while it waits deeper than the rows show, along the straight
            // tube towards it and at its open end (FR-008; the owner's reviews of 2026-11-02).
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
                    else if (RouteTo(member, out RoundLinkMark mark) && _roundFrames.TryGetValue(mark.Pod, out Box shown) && mark.Column < grid.Columns.Count)
                    {
                        // Hidden deeper than the rows show: its tube glows along its length and a ring pulses at its open end.
                        LinkTubeShape tube = LinkTube.HiddenTube(grid, mark, shown);
                        color = RoundLook.ColorOf(mark.Material);
                        float g = tube.Thickness * (0.1f + (0.3f * k));
                        ShowHint(ref hints, tube.Box.Inset(-g), color, k, round: true, degrees: tube.Degrees);
                        (float ex, float ey) = LinkTube.OpenEnd(tube);
                        float r = tube.Thickness * (0.7f + (0.6f * k));
                        ShowHint(ref hints, Box.FromCenter(ex, ey, 2f * r, 2f * r), color, k, round: true);
                        continue;
                    }
                    else if (a.ColumnOf(member) >= 0 && a.ColumnOf(member) < grid.Columns.Count)
                    {
                        ring = grid.Pod(a.ColumnOf(member), grid.PodRows - 1);
                    }
                    else
                    {
                        continue;
                    }

                    float grow = ring.Height * (0.06f + (0.1f * k));
                    ShowHint(ref hints, ring.Inset(-grow), color, k, round);
                }
            }

            for (int i = hints; i < _roundHints.Count; i++)
            {
                _roundHints[i].gameObject.SetActive(false);
            }
        }

        /// <summary>The hidden tube's pair (<see cref="RoundAnimator.LinkMarks"/>) that leads to hidden pod <paramref name="pod"/> this frame.</summary>
        private bool RouteTo(int pod, out RoundLinkMark found)
        {
            foreach (RoundLinkMark mark in _roundMarks)
            {
                if (mark.Partner == pod)
                {
                    found = mark;
                    return true;
                }
            }

            found = default;
            return false;
        }

        /// <summary>
        /// One pulse ring of the refused tap's hint at <paramref name="box"/> in <paramref name="color"/> at strength
        /// <paramref name="k"/>: a pod's rounded square, or a stadium round a tube turned by <paramref name="degrees"/> or a
        /// ring at its end (<paramref name="round"/>).
        /// </summary>
        private void ShowHint(ref int hints, Box box, Rgba color, float k, bool round, float degrees = 0f)
        {
            if (hints == _roundHints.Count)
            {
                Image created = null!;
                created = UiKit.RoundRing("LinkPulse", _roundFront!, Color.white, b => (_roundHintRound.Contains(created) ? Mathf.Min(b.Width, b.Height) * 0.5f : b.Height * 0.24f), _ => UiKit.Units(4f));
                created.raycastTarget = false;
                _roundHints.Add(created);
            }

            Image hint = _roundHints[hints++];
            if (round) _roundHintRound.Add(hint); else _roundHintRound.Remove(hint);
            BoxLayout.Place(hint.rectTransform, box);
            hint.rectTransform.localEulerAngles = new Vector3(0f, 0f, -degrees);
            hint.color = UiTheme.Of(color.WithAlpha(0.35f + (0.65f * k)));
            hint.gameObject.SetActive(true);
            hint.transform.SetAsLastSibling();
        }

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
