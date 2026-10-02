using System;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The Source Tray (T043) on the tray's parchment in the reference look (spec 005 contracts/look.md §3.7, §4.1; the
    /// playtest's <c>PodPainter.DrawTray</c>): one column per stack; the exposed wooden pod sits on top with its handle,
    /// tappable, with the next two pods of the stack fully visible below it, dimmed toward the parchment, never
    /// overlapping, and a green "+N" count badge on the last shown pod's lower left corner for the deeper ones (spec 003
    /// FR-022a, <see cref="ScreenLayout.Tray"/>, which keeps room above the grid for the handles). An emptied stack leaves
    /// a sunk place on the parchment. Connected pods are joined by a riveted link bar in their group's color across the
    /// gap between their frames (FR-035). The tray mirrors the logical state directly, because commits are immediate
    /// feedback (R4), except that a pod whose key is still in flight keeps its lock until the key lands. Taps are
    /// forwarded to the controller.
    /// </summary>
    public sealed class TrayView : MonoBehaviour
    {
        /// <summary>The room kept above the tray grid for the exposed pods' handles, in reference units.</summary>
        public const float HandleRoom = 12f;

        private static readonly Rgba[] LinkPalette =
        {
            C.StateLink,
            Rgba.FromHex("#6FB6E8"),
            Rgba.FromHex("#E67FB0"),
        };

        private readonly Dictionary<string, PodView> _pods = new Dictionary<string, PodView>(StringComparer.Ordinal);
        private readonly List<LinkBarView> _links = new List<LinkBarView>();
        private readonly List<TMPro.TextMeshProUGUI> _more = new List<TMPro.TextMeshProUGUI>();
        private readonly List<Image> _moreDiscs = new List<Image>();
        private readonly List<Image> _wells = new List<Image>();
        private readonly HashSet<string> _heldLocks = new HashSet<string>(StringComparer.Ordinal);
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;
        private Action<string> _onTap = _ => { };
        private float _wellRadius = -10f;

        public static TrayView Create(RectTransform area, VariantVisualCatalog? visuals, Action<string> onTap)
        {
            var view = area.gameObject.AddComponent<TrayView>();
            view._area = area;
            view._visuals = visuals;
            view._onTap = onTap;
            return view;
        }

        public void Refresh(LevelView view)
        {
            foreach (PodView pod in _pods.Values)
            {
                pod.Hide();
            }

            foreach (LinkBarView link in _links)
            {
                link.gameObject.SetActive(false);
            }

            foreach (Image disc in _moreDiscs)
            {
                disc.gameObject.SetActive(false);
            }

            foreach (Image well in _wells)
            {
                well.gameObject.SetActive(false);
            }

            Rect area = _area.rect;
            int stacks = view.StackCount;
            var boxes = new Dictionary<string, Box>(StringComparer.Ordinal);

            // A grid like the reference game's source area (spec 003 FR-022a): one column per stack, the exposed pod on
            // top and the pods that follow it below, never overlapping, so the player sees what each choice uncovers. The
            // grid is laid out in the area's own top-down units and leaves room above it for the exposed pods' handles.
            TrayGrid grid = ScreenLayout.Tray(new Box(0f, UiKit.Units(HandleRoom), area.width, Mathf.Max(UiKit.Units(HandleRoom), area.height)), stacks, UiKit.Units(1f));
            for (int s = 0; s < stacks; s++)
            {
                IReadOnlyList<string> ids = view.Stack(s);
                if (ids.Count == 0)
                {
                    // An emptied stack: a sunk place on the parchment.
                    Box empty = grid.Cell(s, 0).Inset(grid.PodSize * 0.06f);
                    Image well = Well(s, empty.Width);
                    BoxLayout.Place(well.rectTransform, empty);
                    well.gameObject.SetActive(true);
                    well.transform.SetAsLastSibling();
                    continue;
                }

                // From the bottom row up, so the exposed pod (and its squash) lies over the pod below it.
                int shown = Mathf.Min(ids.Count, ScreenLayout.TrayRows);
                for (int d = shown - 1; d >= 0; d--)
                {
                    PodView pod = Get(ids[d]);
                    Box cell = grid.Cell(s, d);
                    BoxLayout.Place(pod.Rect, cell);
                    PodInfo info = view.Pod(ids[d]);
                    pod.Show(info, _visuals, interactive: d == 0, dimmed: d > 0, lockShown: info.Locked || _heldLocks.Contains(info.Id), linkColor: LinkColorOf(view, info));
                    pod.transform.SetAsLastSibling();
                    boxes[ids[d]] = cell;
                }

                // Deeper pods are not drawn; a "+N" count badge on the last shown pod's lower left corner says how many
                // more wait there.
                if (ids.Count > shown)
                {
                    TMPro.TextMeshProUGUI more = More(s);
                    more.text = "+" + (ids.Count - shown).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Box last = grid.Cell(s, shown - 1);
                    float h = last.Height * 0.26f;
                    Image disc = _moreDiscs[s];
                    BoxLayout.Place(disc.rectTransform, Box.FromCenter(last.Left + (h * 0.42f), last.Bottom - (h * 0.42f), h * 1.26f, h * 1.26f));
                }
            }

            DrawLinks(view, boxes);
            foreach (Image disc in _moreDiscs)
            {
                if (disc.gameObject.activeSelf)
                {
                    disc.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>Keeps a pod drawn locked until its key lands (the rules opened it already).</summary>
        public void HoldLock(string podId) => _heldLocks.Add(podId);

        /// <summary>The pods spin in place (Shuffle, US5).</summary>
        public void PlayShuffle()
        {
            foreach (PodView pod in _pods.Values)
            {
                if (pod.gameObject.activeSelf)
                {
                    pod.Spin();
                }
            }
        }

        /// <summary>A pod came back from a slot (Return, US5): it pops in on top of its stack.</summary>
        public void PlayReturned(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.gameObject.activeSelf)
            {
                pod.Pulse();
            }
        }

        /// <summary>
        /// Connected pods (FR-035): a link bar joins the visible members of each group across the gap between their frames,
        /// so it is clear that they commit together. Members sit at the same depth, so the bar is horizontal. Each group has
        /// its own color.
        /// </summary>
        private void DrawLinks(LevelView view, Dictionary<string, Box> boxes)
        {
            int used = 0;
            var drawn = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Box> entry in boxes)
            {
                PodInfo pod = view.Pod(entry.Key);
                if (pod.ConnectedGroupId == null || !drawn.Add(pod.ConnectedGroupId))
                {
                    continue;
                }

                IReadOnlyList<string> members = view.ConnectedGroup(entry.Key);
                Color color = LinkColorOf(view, pod) ?? UiTheme.LinkColor;
                for (int i = 1; i < members.Count; i++)
                {
                    if (!boxes.TryGetValue(members[i - 1], out Box a) || !boxes.TryGetValue(members[i], out Box b))
                    {
                        continue;
                    }

                    if (used == _links.Count)
                    {
                        _links.Add(UiKit.LinkBar("Link", _area));
                    }

                    LinkBarView link = _links[used++];
                    link.gameObject.SetActive(true);
                    link.Set(a.Left <= b.Left ? a : b, a.Left <= b.Left ? b : a, color);
                    link.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>The link color of a pod's connected group (<c>state.link</c>, then a small fixed palette by group order), or null.</summary>
        private static Color? LinkColorOf(LevelView view, PodInfo pod)
        {
            if (pod.ConnectedGroupId == null)
            {
                return null;
            }

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
            return UiTheme.Of(LinkPalette[Mathf.Max(0, groups.IndexOf(pod.ConnectedGroupId)) % LinkPalette.Length]);
        }

        /// <summary>The "+N" count badge of a stack (§3.4: white digits on a <c>badge.green</c> disc with a white ring).</summary>
        private TMPro.TextMeshProUGUI More(int stack)
        {
            while (_more.Count <= stack)
            {
                TMPro.TextMeshProUGUI label = UiKit.CountBadge("More", _area, out Image disc);
                _more.Add(label);
                _moreDiscs.Add(disc);
                disc.gameObject.SetActive(false);
            }

            _moreDiscs[stack].gameObject.SetActive(true);
            return _more[stack];
        }

        /// <summary>
        /// The sunk place of an emptied stack (a well of <c>parchment.well</c> mixed toward <c>parchment.edge</c>, radius 18%
        /// of its side <paramref name="side"/>, in canvas units); the wells are made again when the pods change size.
        /// </summary>
        private Image Well(int stack, float side)
        {
            float radiusUnits = side * 0.18f / Mathf.Max(0.0001f, UiKit.Units(1f));
            if (Mathf.Abs(radiusUnits - _wellRadius) > 1f)
            {
                foreach (Image old in _wells)
                {
                    Destroy(old.gameObject);
                }

                _wells.Clear();
                _wellRadius = radiusUnits;
            }

            while (_wells.Count <= stack)
            {
                Image well = UiKit.Well("Empty", _area, UiTheme.Of(C.ParchmentWell.Mix(C.ParchmentEdge, 0.5f)), _wellRadius);
                well.gameObject.SetActive(false);
                _wells.Add(well);
            }

            return _wells[stack];
        }

        public void ShowRefused(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod))
            {
                pod.Shake();
            }
        }

        /// <summary>The pod's key landed: its lock opens with a pulse.</summary>
        public void ShowAccepted(string podId)
        {
            _heldLocks.Remove(podId);
            if (_pods.TryGetValue(podId, out PodView? pod))
            {
                pod.Unlock();
            }
        }

        /// <summary>Forgets held locks (restart and boosters rebuild from the settled state).</summary>
        public void ReleaseLocks() => _heldLocks.Clear();

        /// <summary>The on-screen rect of a pod, for tutorial pointers.</summary>
        public RectTransform? RectOf(string podId) => _pods.TryGetValue(podId, out PodView? pod) && pod.gameObject.activeSelf ? pod.Rect : null;

        public void Clear()
        {
            foreach (PodView pod in _pods.Values)
            {
                Destroy(pod.gameObject);
            }

            _pods.Clear();
        }

        private PodView Get(string podId)
        {
            if (!_pods.TryGetValue(podId, out PodView? pod))
            {
                pod = PodView.Create(_area, _onTap);
                _pods.Add(podId, pod);
            }

            return pod;
        }
    }
}
