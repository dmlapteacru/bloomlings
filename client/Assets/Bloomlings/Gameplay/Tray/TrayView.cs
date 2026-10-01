using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The Source Tray (T043): one column per stack; the exposed pod sits on top, highlighted and tappable, with the next
    /// two pods of the stack fully visible below it in their muted variant colors, never overlapping, and a "+N" badge for
    /// the deeper ones (spec 003 FR-022a, <see cref="ScreenLayout.Tray"/>). Connected pods are joined by a link bar in
    /// their group's color, drawn over the gap between the cards. The tray mirrors the logical state directly, because
    /// commits are immediate feedback (R4), except that a pod whose key is still in flight keeps its lock until the key
    /// lands. Taps are forwarded to the controller.
    /// </summary>
    public sealed class TrayView : MonoBehaviour
    {
        private readonly Dictionary<string, PodView> _pods = new Dictionary<string, PodView>(StringComparer.Ordinal);
        private readonly List<Image> _links = new List<Image>();
        private readonly List<TMPro.TextMeshProUGUI> _more = new List<TMPro.TextMeshProUGUI>();
        private readonly List<Image> _moreDiscs = new List<Image>();
        private readonly HashSet<string> _heldLocks = new HashSet<string>(StringComparer.Ordinal);
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;
        private Action<string> _onTap = _ => { };

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

            foreach (Image link in _links)
            {
                link.enabled = false;
            }

            foreach (Image disc in _moreDiscs)
            {
                disc.gameObject.SetActive(false);
            }

            Rect area = _area.rect;
            int stacks = view.StackCount;
            var positions = new Dictionary<string, (Vector2 Center, float Size)>(StringComparer.Ordinal);

            // A grid like the reference game's source area (spec 003 FR-022a): one column per stack, the exposed pod on
            // top and the pods that follow it below, never overlapping, so the player sees what each choice uncovers.
            // The grid is laid out top-down in the area's own units, then turned into bottom-up anchored positions.
            TrayGrid grid = ScreenLayout.Tray(new Box(0f, 0f, area.width, area.height), stacks, UiKit.Units(1f));
            for (int s = 0; s < stacks; s++)
            {
                IReadOnlyList<string> ids = view.Stack(s);
                int shown = Mathf.Min(ids.Count, ScreenLayout.TrayRows);
                for (int d = 0; d < shown; d++)
                {
                    PodView pod = Get(ids[d]);
                    Box cell = grid.Cell(s, d);
                    var center = new Vector2(cell.CenterX, area.height - cell.CenterY);
                    UiFactory.PlaceAbsolute(pod.Rect, center, Vector2.one * cell.Width);
                    PodInfo info = view.Pod(ids[d]);
                    pod.Show(info, _visuals, interactive: d == 0, dimmed: d > 0, lockShown: info.Locked || _heldLocks.Contains(info.Id), linkColor: LinkColorOf(view, info));
                    pod.transform.SetAsLastSibling();
                    positions[ids[d]] = (center, cell.Width);
                }

                // Deeper pods are not drawn; a "+N" badge on the last shown pod says how many more wait there.
                if (ids.Count > shown)
                {
                    TMPro.TextMeshProUGUI more = More(s);
                    more.text = "+" + (ids.Count - shown).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Box last = grid.Cell(s, shown - 1);
                    float badge = last.Height * 0.3f;
                    Image disc = _moreDiscs[s];
                    UiFactory.PlaceAbsolute(disc.rectTransform, new Vector2(last.Right - (badge * 0.4f), area.height - last.Bottom + (badge * 0.4f)), new Vector2(badge * 1.6f, badge));
                    disc.transform.SetAsLastSibling();
                }
            }

            DrawLinks(view, positions);
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
        /// Connected pods (FR-035): a link bar joins the visible members of each group, over the gap between their cards,
        /// so it is clear that they commit together. Members sit at the same depth, so the bar is horizontal. Each group
        /// has its own color, matching the link ring on its cards.
        /// </summary>
        private void DrawLinks(LevelView view, Dictionary<string, (Vector2 Center, float Size)> positions)
        {
            int used = 0;
            var drawn = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, (Vector2 Center, float Size)> entry in positions)
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
                    if (!positions.TryGetValue(members[i - 1], out (Vector2 Center, float Size) a) || !positions.TryGetValue(members[i], out (Vector2 Center, float Size) b))
                    {
                        continue;
                    }

                    if (used == _links.Count)
                    {
                        _links.Add(UiFactory.CreateImage("Link", _area, ProceduralSprites.RoundedSquare, UiTheme.LinkColor));
                    }

                    Image link = _links[used++];
                    link.enabled = true;
                    link.color = color;
                    float left = Mathf.Min(a.Center.x, b.Center.x) + (a.Size * 0.42f);
                    float right = Mathf.Max(a.Center.x, b.Center.x) - (b.Size * 0.42f);
                    float width = Mathf.Max(right - left, a.Size * 0.2f);
                    UiFactory.PlaceAbsolute(link.rectTransform, new Vector2((left + right) / 2f, a.Center.y), new Vector2(width, Mathf.Max(10f, a.Size * 0.14f)));
                    link.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>The link color of a pod's connected group (a small fixed palette by group order), or null.</summary>
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
            return LinkPalette[Mathf.Max(0, groups.IndexOf(pod.ConnectedGroupId)) % LinkPalette.Length];
        }

        private static readonly Color[] LinkPalette =
        {
            UiTheme.LinkColor,
            new Color(0.62f, 0.83f, 0.94f, 0.95f),
            new Color(0.96f, 0.66f, 0.80f, 0.95f),
        };

        /// <summary>The "+N" count badge of a stack (a dark brown disc with a cream ring, spec 003 FR-014).</summary>
        private TMPro.TextMeshProUGUI More(int stack)
        {
            while (_more.Count <= stack)
            {
                TMPro.TextMeshProUGUI label = UiKit.CountBadge("More", _area, out Image disc);
                _more.Add(label);
                _moreDiscs.Add(disc);
            }

            _moreDiscs[stack].gameObject.SetActive(true);
            return _more[stack];
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

        /// <summary>The on-screen rect of a pod card, for tutorial pointers.</summary>
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
