using System;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The Source Tray (T043) as the reference's grid of pods at the bottom of the tray (spec 005 FR-021, contracts/look.md
    /// §3.7, §6.1; the playtest's <c>PodPainter</c>): one column per stack, its pods one after another and never on each
    /// other (the owner's gameplay rule, 2026-10-03), in the boxes <see cref="Grid"/> gives
    /// (<see cref="ReferenceGameplayRegions.Pod"/>):
    /// <list type="bullet">
    /// <item><description>the exposed pod in the top row (<see cref="PodView"/>): bright, the only pod that takes a tap
    /// (spec 001 FR-011), through a touch box grown to <c>size.touch_min</c>;</description></item>
    /// <item><description>the next pods of the stack in the rows below it, as many as the tray shows
    /// (<see cref="ReferenceGameplayRegions.PodRows"/>: three, four from 19.5:9), each fully visible, muted but showing
    /// its variant's tile and its count (identity never by hue alone, spec 001 FR-072), so the player reads what each
    /// choice uncovers;</description></item>
    /// <item><description>a green "+N" count badge on the last shown pod's top-right corner for the pods beyond
    /// it;</description></item>
    /// <item><description>an emptied stack as a sunk well where its exposed pod stood;</description></item>
    /// <item><description>locked: the padlock on a grey panel; mystery: the lilac "?" tile; connected: a riveted link bar in
    /// the group's color between the frames of members side by side in one row, or a ring of that color on each member
    /// otherwise (FR-035).</description></item>
    /// </list>
    /// When the exposed pod leaves, the pods under it slide up one row and the next hidden one fades in at the bottom;
    /// Return puts a pod back on top and the column slides down; Shuffle re-lays the columns and turns the pods over
    /// (presentation only, spec 005 FR-002: the core's events and state drive it). The tray mirrors the logical state
    /// directly, because commits are immediate feedback (R4), except that a pod whose key is still in flight keeps its
    /// lock until the key lands. Taps are forwarded to the controller.
    /// </summary>
    public sealed class TrayView : MonoBehaviour
    {
        /// <summary>The radius of a connected member's ring mark, as a share of its pod's height (its white rim 30% more).</summary>
        public const float MarkShare = 0.15f;

        private static readonly Rgba[] LinkPalette =
        {
            C.StateLink,
            C.StateLink2,
            C.StateLink3,
        };

        private readonly Dictionary<string, PodView> _pods = new Dictionary<string, PodView>(StringComparer.Ordinal);
        private readonly HashSet<string> _shown = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<LinkBarView> _links = new List<LinkBarView>();
        private readonly List<(Image Rim, Image Dot)> _marks = new List<(Image, Image)>();
        private readonly List<TMPro.TextMeshProUGUI> _more = new List<TMPro.TextMeshProUGUI>();
        private readonly List<Image> _moreDiscs = new List<Image>();
        private readonly List<Image> _wells = new List<Image>();
        private readonly HashSet<string> _heldLocks = new HashSet<string>(StringComparer.Ordinal);
        private RectTransform _area = null!;
        private Action<string> _onTap = _ => { };
        private float _wellRadius = -10f;

        /// <summary>
        /// The pod grid for a number of stacks, its columns and pod sizes in the tray area's top-down canvas units (the
        /// HUD's <c>GameplayHud.PodGrid</c>, spec 005 §6.1); null lays the reference grid out for the screen.
        /// </summary>
        public Func<int, ReferenceGameplayRegions>? Grid { get; set; }

        /// <param name="visuals">Kept for the callers; the pods draw the variant catalog's candy tiles (spec 005).</param>
        public static TrayView Create(RectTransform area, VariantVisualCatalog? visuals, Action<string> onTap)
        {
            var view = area.gameObject.AddComponent<TrayView>();
            view._area = area;
            view._onTap = onTap;
            return view;
        }

        /// <summary>
        /// Shows the stacks of <paramref name="view"/>. With <paramref name="slide"/> the pods whose column moved slide to
        /// their new rows (a tap, Return, a key); a new level or a restart places them at once.
        /// </summary>
        public void Refresh(LevelView view, bool slide = true)
        {
            foreach (LinkBarView link in _links)
            {
                link.gameObject.SetActive(false);
            }

            foreach ((Image rim, Image dot) in _marks)
            {
                rim.gameObject.SetActive(false);
                dot.gameObject.SetActive(false);
            }

            foreach (Image disc in _moreDiscs)
            {
                disc.gameObject.SetActive(false);
            }

            foreach (Image well in _wells)
            {
                well.gameObject.SetActive(false);
            }

            int stacks = view.StackCount;
            ReferenceGameplayRegions grid = Grid?.Invoke(stacks) ?? ScreenGrid(stacks);
            float touchMin = UiKit.Units(DesignTokens.Size.TouchMin);
            var boxes = new Dictionary<string, Box>(StringComparer.Ordinal);
            _shown.Clear();
            for (int s = 0; s < stacks && s < grid.Columns.Count; s++)
            {
                IReadOnlyList<string> ids = view.Stack(s);
                if (ids.Count == 0)
                {
                    // An emptied stack: a sunk place on the parchment where its exposed pod stood.
                    Box front = grid.Pod(s, 0);
                    Box empty = front.Inset(front.Height * 0.06f);
                    Image well = Well(s, empty.Height);
                    BoxLayout.Place(well.rectTransform, empty);
                    well.gameObject.SetActive(true);
                    well.transform.SetAsLastSibling();
                    continue;
                }

                // The column top down: the exposed pod, then the next ones, each in its own row.
                int rows = Mathf.Min(ids.Count, grid.PodRows);
                for (int depth = 0; depth < rows; depth++)
                {
                    PodInfo info = view.Pod(ids[depth]);
                    bool exposed = depth == 0 && view.IsExposed(info.Id);
                    PodView pod = Get(info.Id);
                    Box box = grid.Pod(s, depth);

                    // A pod still in its column slides from where it is drawn; one coming into the last row rises from
                    // under it, fading in; one put back on top (Return) fades in at its place, under the pods sliding
                    // down. A pod that changed columns (Shuffle) is placed at once.
                    Box? from = null;
                    bool fadeIn = false;
                    if (slide && pod.IsShown && pod.Stack == s)
                    {
                        from = pod.Visual;
                    }
                    else if (slide && !pod.IsShown && (depth == 0 || depth == rows - 1))
                    {
                        from = depth == 0 ? box : grid.Pod(s, depth + 1);
                        fadeIn = true;
                    }

                    pod.Show(info, interactive: exposed, waiting: !exposed, lockShown: info.Locked || _heldLocks.Contains(info.Id));
                    pod.Place(s, box, exposed ? Touch(box, touchMin) : box, from, fadeIn);
                    pod.transform.SetAsLastSibling();
                    _shown.Add(info.Id);
                    boxes[info.Id] = box;
                }

                // The pods beyond the rows shown wait under a "+N" badge on the last shown pod's top-left corner (the count keeps its bottom right).
                int more = ids.Count - rows;
                if (more > 0)
                {
                    TMPro.TextMeshProUGUI label = More(s);
                    label.text = "+" + more.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Box badge = grid.Chip(s, rows - 1).Badge;
                    float size = badge.Height * 1.26f;
                    BoxLayout.Place(_moreDiscs[s].rectTransform, Box.FromCenter(badge.CenterX, badge.CenterY, size, size));
                }
            }

            foreach (PodView pod in _pods.Values)
            {
                if (pod.IsShown && !_shown.Contains(pod.PodId))
                {
                    pod.Hide();
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

        /// <summary>
        /// The reference grid for <paramref name="stacks"/> stacks when no layout is given: the screen's reference gameplay
        /// layout with boosters and no entry under the board, in the tray area's units.
        /// </summary>
        private static ReferenceGameplayRegions ScreenGrid(int stacks)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            return GameplayHud.InPodRow(ScreenLayout.ReferenceGameplay(w, h, insets, stacks, WaitingSlots.DefaultCount, true, false));
        }

        /// <summary>The exposed pod's touch box: its box grown about its center to <c>size.touch_min</c> each way.</summary>
        private static Box Touch(Box box, float min) => Box.FromCenter(box.CenterX, box.CenterY, Mathf.Max(box.Width, min), Mathf.Max(box.Height, min));

        /// <summary>Keeps a pod drawn locked until its key lands (the rules opened it already).</summary>
        public void HoldLock(string podId) => _heldLocks.Add(podId);

        /// <summary>The columns were re-laid (Shuffle, US5): every shown pod turns over in its new place.</summary>
        public void PlayShuffle()
        {
            foreach (PodView pod in _pods.Values)
            {
                if (pod.IsShown)
                {
                    pod.Spin();
                }
            }
        }

        /// <summary>A pod came back from a slot (Return, US5): it pops in on top of its column.</summary>
        public void PlayReturned(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.IsShown)
            {
                pod.Pulse();
            }
        }

        /// <summary>
        /// Connected pods (FR-035): a link bar joins the shown members of each group that sit side by side in one row,
        /// across the gap between their frames, so it is clear that they commit together; a group whose members lie in
        /// different rows or columns apart, or that shows a single member, marks each shown member with a ring of its color
        /// instead. Each group has its own color.
        /// </summary>
        private void DrawLinks(LevelView view, Dictionary<string, Box> boxes)
        {
            var groups = new Dictionary<string, List<Box>>(StringComparer.Ordinal);
            var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Box> entry in boxes)
            {
                PodInfo pod = view.Pod(entry.Key);
                if (pod.ConnectedGroupId == null)
                {
                    continue;
                }

                if (!groups.TryGetValue(pod.ConnectedGroupId, out List<Box>? members))
                {
                    groups[pod.ConnectedGroupId] = members = new List<Box>();
                    colors[pod.ConnectedGroupId] = LinkColorOf(view, pod) ?? UiTheme.LinkColor;
                }

                members.Add(entry.Value);
            }

            int used = 0;
            int marked = 0;
            foreach (KeyValuePair<string, List<Box>> group in groups)
            {
                List<Box> members = group.Value;
                members.Sort((a, b) => a.Left.CompareTo(b.Left));
                Color color = colors[group.Key];

                // One row of neighbors: the same row, and each member in the column next to the one before it.
                bool oneRow = members.Count > 1;
                for (int i = 1; i < members.Count; i++)
                {
                    oneRow &= Mathf.Abs(members[i].CenterY - members[0].CenterY) < members[0].Height * 0.5f
                        && members[i].Left - members[i - 1].Right < members[i].Height;
                }

                if (oneRow)
                {
                    for (int i = 1; i < members.Count; i++)
                    {
                        if (used == _links.Count)
                        {
                            _links.Add(UiKit.LinkBar("Link", _area));
                        }

                        LinkBarView link = _links[used++];
                        link.gameObject.SetActive(true);
                        link.Set(PodChip.In(members[i - 1]).Frame, PodChip.In(members[i]).Frame, color);
                        link.transform.SetAsLastSibling();
                    }

                    continue;
                }

                // Members in different rows or columns apart, or the only member shown: a small ring of the group's color on
                // each one's top-right corner (the "+N" disc takes the top left, the count the bottom right).
                foreach (Box place in members)
                {
                    Box box = PodChip.In(place).Frame;
                    if (marked == _marks.Count)
                    {
                        _marks.Add((UiKit.RoundRect("LinkMarkRim", _area, Color.white), UiKit.RoundRect("LinkMark", _area, Color.white)));
                    }

                    (Image rim, Image dot) = _marks[marked++];
                    float d = box.Height * MarkShare;
                    float x = box.Right - (box.Height * 0.12f);
                    float y = box.Top + (box.Height * 0.12f);
                    BoxLayout.Place(rim.rectTransform, Box.FromCenter(x, y, d * 2.6f, d * 2.6f));
                    BoxLayout.Place(dot.rectTransform, Box.FromCenter(x, y, d * 2f, d * 2f));
                    dot.color = color;
                    rim.gameObject.SetActive(true);
                    dot.gameObject.SetActive(true);
                    rim.transform.SetAsLastSibling();
                    dot.transform.SetAsLastSibling();
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
        /// of its height <paramref name="side"/>, in canvas units); the wells are made again when the pods change size.
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
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.IsShown)
            {
                pod.Shake();
            }
        }

        /// <summary>The pod's key landed: its lock opens with a pulse, in whichever row it waits.</summary>
        public void ShowAccepted(string podId)
        {
            _heldLocks.Remove(podId);
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.IsShown)
            {
                pod.Unlock();
            }
        }

        /// <summary>Forgets held locks (restart and boosters rebuild from the settled state).</summary>
        public void ReleaseLocks() => _heldLocks.Clear();

        /// <summary>The on-screen rect of a pod's place, for tutorial pointers and keys; null when the pod is not shown.</summary>
        public RectTransform? RectOf(string podId) =>
            _pods.TryGetValue(podId, out PodView? pod) && pod.IsShown ? pod.Rect : null;

        /// <summary>
        /// Where a pod's tile is (world position), where its flight to a slot starts and a returned pod lands; null when
        /// the pod is not shown.
        /// </summary>
        public Vector3? TilePosition(string podId) =>
            _pods.TryGetValue(podId, out PodView? pod) && pod.IsShown ? pod.TileRect.position : (Vector3?)null;

        /// <summary>The side of a pod's tile in canvas units (see <see cref="TilePosition"/>), or 0 when the pod is not shown.</summary>
        public float TileSize(string podId) =>
            _pods.TryGetValue(podId, out PodView? pod) && pod.IsShown ? pod.TileSize : 0f;

        public void Clear()
        {
            foreach (PodView pod in _pods.Values)
            {
                Destroy(pod.gameObject);
            }

            _pods.Clear();
            _shown.Clear();
        }

        private PodView Get(string podId)
        {
            if (!_pods.TryGetValue(podId, out PodView? pod))
            {
                pod = PodView.Create(_area, _onTap);
                pod.gameObject.SetActive(false);
                _pods.Add(podId, pod);
            }

            return pod;
        }
    }
}
