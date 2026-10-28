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
    /// The tray's columns on a round level (spec 006 T059, contracts/presentation.md; the playtest's
    /// <c>RoundPainter.Columns</c>): the game's wooden pods one after another in a column per stack, the top pod the only one
    /// that takes the tap, "+N" for the rest; a pod's frame tinted by its material with its plain tile and count, a hidden pod's
    /// "?" until it reveals and its glow then (FR-045); the link tubes on linked pods' front faces, under the pods (FR-043);
    /// and the pulse round the partner a refused tap waits for. A pod slides to its new row when its column moves.
    /// </summary>
    public sealed partial class TrayView
    {
        private readonly Dictionary<int, (int Stack, int Depth, Box Box)> _roundPlaced = new Dictionary<int, (int, int, Box)>();
        private readonly HashSet<int> _roundShown = new HashSet<int>();
        private readonly List<Image> _roundTubes = new List<Image>();
        private readonly Dictionary<int, (Image Fill, Image Ring)> _roundGlows = new Dictionary<int, (Image, Image)>();
        private readonly Dictionary<int, Box> _roundFrames = new Dictionary<int, Box>();
        private readonly List<Image> _roundHints = new List<Image>();
        private RectTransform? _roundBack;
        private RectTransform? _roundFront;
        private ReferenceGameplayRegions? _roundGrid;

        /// <summary>A shown round pod's tile box in the tray area's top-down canvas units (its flight to a place starts there).</summary>
        public Box? RoundTileBox(int pod) =>
            _pods.TryGetValue(Key(pod), out PodView? view) && view.IsShown && _roundPlaced.ContainsKey(pod)
                ? PodChip.In(view.Visual, _roundPlaced[pod].Depth > 0).Tile
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
            foreach (Image disc in _moreDiscs)
            {
                disc.gameObject.SetActive(false);
            }

            for (int s = 0; s < columns; s++)
            {
                int waiting = a.Waiting(s);
                Image? well = s < _wells.Count ? _wells[s] : null;
                if (waiting == 0)
                {
                    Box front = grid.Pod(s, 0);
                    Box empty = front.Inset(front.Height * 0.06f);
                    well = Well(s, empty.Height);
                    BoxLayout.Place(well.rectTransform, empty);
                    well.gameObject.SetActive(true);
                    continue;
                }

                if (well != null)
                {
                    well.gameObject.SetActive(false);
                }

                int rows = Mathf.Min(waiting, grid.PodRows);
                for (int depth = rows - 1; depth >= 0; depth--)
                {
                    int pod = a.At(s, depth);
                    PodView view = Get(Key(pod));
                    bool wasShown = view.IsShown;
                    int material = a.Revealed(pod) ? a.MaterialOf(pod) : -1;
                    bool exposed = depth == 0;
                    view.ShowRound(pod, material, a.Remaining(pod), interactive && exposed, !exposed, symbols);
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

                int more = waiting - rows;
                if (more > 0)
                {
                    TMPro.TextMeshProUGUI label = More(s);
                    label.text = "+" + more.ToString(CultureInfo.InvariantCulture);
                    Box badge = grid.Chip(s, rows - 1).Badge;
                    float size = badge.Height * 1.26f;
                    BoxLayout.Place(_moreDiscs[s].rectTransform, Box.FromCenter(badge.CenterX, badge.CenterY, size, size));
                    _moreDiscs[s].transform.SetAsLastSibling();
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

            SyncRoundOverlays(core, grid);
        }

        /// <summary>Forgets where the round pods stood (a restart puts the whole tray back at once).</summary>
        public void ResetRound()
        {
            _roundPlaced.Clear();
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
                if (_pods.TryGetValue(Key(pod), out PodView? view) && _roundPlaced.TryGetValue(pod, out (int Stack, int Depth, Box Box) placed))
                {
                    _roundFrames[pod] = PodChip.In(view.Visual, placed.Depth > 0).Frame;
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

                RoundTubes.Show(_roundTubes[used++], LinkTube.Between(first, RoundLook.ColorOf(a.MaterialOf(tube.First)), second, RoundLook.ColorOf(a.MaterialOf(tube.Second))));
            }

            for (int i = used; i < _roundTubes.Count; i++)
            {
                _roundTubes[i].gameObject.SetActive(false);
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

            // The pulse round the partner a refused tap waits for, or round the "+N" that hides it (FR-008).
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
                    if (_roundFrames.TryGetValue(member, out Box frame))
                    {
                        ring = frame;
                    }
                    else if (a.ColumnOf(member) >= 0 && a.ColumnOf(member) < grid.Columns.Count)
                    {
                        Box badge = grid.Chip(a.ColumnOf(member), grid.PodRows - 1).Badge;
                        ring = Box.FromCenter(badge.CenterX, badge.CenterY, badge.Height * 1.5f, badge.Height * 1.5f);
                    }
                    else
                    {
                        continue;
                    }

                    if (hints == _roundHints.Count)
                    {
                        Image created = UiKit.RoundRing("LinkPulse", _roundFront!, Color.white, b => b.Height * 0.24f, _ => UiKit.Units(4f));
                        created.raycastTarget = false;
                        _roundHints.Add(created);
                    }

                    Image hint = _roundHints[hints++];
                    float grow = ring.Height * (0.06f + (0.1f * k));
                    BoxLayout.Place(hint.rectTransform, ring.Inset(-grow));
                    Rgba color = RoundLook.ColorOf(a.MaterialOf(member));
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

        private static string Key(int pod) => pod.ToString(CultureInfo.InvariantCulture);

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
