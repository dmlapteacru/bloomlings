using System;
using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The Source Tray (T043) as the reference's row of decks at the bottom of the tray (spec 005 FR-021, contracts/look.md
    /// §3.7, §6.1; the playtest's <c>PodPainter.DrawDecks</c>), one deck per stack in the boxes <see cref="Decks"/> gives:
    /// <list type="bullet">
    /// <item><description>the exposed pod in front (<see cref="PodView"/>): a dark wooden frame, its panel tinted by the
    /// variant, the sticker tile and the count; it squashes under the finger and is the only pod that takes a tap (spec 001
    /// FR-011);</description></item>
    /// <item><description>up to two buried pods as wooden frames peeking above it (<see cref="UiKit.BuriedPod"/>), each
    /// showing a band of its variant color with its small symbol (identity never by hue alone, spec 001
    /// FR-072);</description></item>
    /// <item><description>a green "+N" count badge on the deck's top right for the pods beyond those two;</description></item>
    /// <item><description>an emptied stack as a sunk well;</description></item>
    /// <item><description>locked: the padlock on a grey panel (a grey band when buried); mystery: the lilac "?" tile (a lilac
    /// band with "?"); connected: a riveted link bar in the group's color between the frames (or the bands of buried
    /// members), or a ring of that color on each member when the group spans two rows of decks (FR-035).</description></item>
    /// </list>
    /// The tray mirrors the logical state directly, because commits are immediate feedback (R4), except that a pod whose
    /// key is still in flight keeps its lock until the key lands. Taps are forwarded to the controller.
    /// </summary>
    public sealed class TrayView : MonoBehaviour
    {
        /// <summary>How many buried pods a deck shows behind its front pod.</summary>
        public const int BuriedShown = 2;

        /// <summary>The radius of a connected member's ring mark, as a share of its deck's width (its white rim 30% more).</summary>
        public const float MarkShare = 0.07f;

        private static readonly Rgba[] LinkPalette =
        {
            C.StateLink,
            C.StateLink2,
            C.StateLink3,
        };

        private readonly Dictionary<string, PodView> _pods = new Dictionary<string, PodView>(StringComparer.Ordinal);
        private readonly List<BuriedPodView[]> _buried = new List<BuriedPodView[]>();
        private readonly Dictionary<string, BuriedPodView> _buriedShown = new Dictionary<string, BuriedPodView>(StringComparer.Ordinal);
        private readonly List<LinkBarView> _links = new List<LinkBarView>();
        private readonly List<(Image Rim, Image Dot)> _marks = new List<(Image, Image)>();
        private readonly List<TMPro.TextMeshProUGUI> _more = new List<TMPro.TextMeshProUGUI>();
        private readonly List<Image> _moreDiscs = new List<Image>();
        private readonly List<Image> _wells = new List<Image>();
        private readonly HashSet<string> _heldLocks = new HashSet<string>(StringComparer.Ordinal);
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;
        private Action<string> _onTap = _ => { };
        private float _wellRadius = -10f;

        /// <summary>
        /// The deck boxes for a number of stacks, in the tray area's top-down canvas units (the HUD's
        /// <c>GameplayHud.DeckCells</c>, spec 005 §6.1); null spreads the decks across the area.
        /// </summary>
        public Func<int, IReadOnlyList<Box>>? Decks { get; set; }

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

            foreach (BuriedPodView[] deck in _buried)
            {
                foreach (BuriedPodView buried in deck)
                {
                    buried.gameObject.SetActive(false);
                    buried.PodId = null;
                }
            }

            _buriedShown.Clear();
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
            IReadOnlyList<Box> decks = Decks?.Invoke(stacks) ?? Spread(stacks);
            var boxes = new Dictionary<string, Box>(StringComparer.Ordinal);
            for (int s = 0; s < stacks && s < decks.Count; s++)
            {
                PodDeck deck = PodDeck.In(decks[s]);
                IReadOnlyList<string> ids = view.Stack(s);
                if (ids.Count == 0)
                {
                    // An emptied stack: a sunk place on the parchment where its front pod stood.
                    Box empty = deck.Front.Inset(deck.Front.Width * 0.04f);
                    Image well = Well(s, empty.Width);
                    BoxLayout.Place(well.rectTransform, empty);
                    well.gameObject.SetActive(true);
                    well.transform.SetAsLastSibling();
                    continue;
                }

                // The buried pods, the deepest first, so each lies under the one in front of it.
                for (int depth = Mathf.Min(ids.Count - 1, BuriedShown); depth >= 1; depth--)
                {
                    PodInfo info = view.Pod(ids[depth]);
                    BuriedPodView buried = Buried(s, depth);
                    BoxLayout.Place((RectTransform)buried.transform, deck.Buried(depth));
                    buried.gameObject.SetActive(true);
                    buried.PodId = info.Id;
                    buried.transform.localScale = Vector3.one;
                    buried.Show(info.Variant, info.Locked || _heldLocks.Contains(info.Id), depth);
                    buried.transform.SetAsLastSibling();
                    _buriedShown[info.Id] = buried;
                    boxes[info.Id] = deck.Band(depth);
                }

                // The front pod: the only one that takes a tap (a locked one answers with its refusal).
                PodInfo front = view.Pod(ids[0]);
                bool exposed = view.IsExposed(front.Id);
                PodView pod = Get(front.Id);
                BoxLayout.Place(pod.Rect, deck.Front);
                pod.Show(front, _visuals, interactive: exposed, dimmed: !exposed, lockShown: front.Locked || _heldLocks.Contains(front.Id), linkColor: LinkColorOf(view, front));
                pod.transform.SetAsLastSibling();
                boxes[front.Id] = deck.Front;

                // The pods beyond the two shown wait under a "+N" badge on the deck's top right.
                int more = ids.Count - 1 - BuriedShown;
                if (more > 0)
                {
                    TMPro.TextMeshProUGUI label = More(s);
                    label.text = "+" + more.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    float size = deck.Badge.Height * 1.26f;
                    BoxLayout.Place(_moreDiscs[s].rectTransform, Box.FromCenter(deck.Badge.CenterX, deck.Badge.CenterY, size, size));
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
        /// Decks spread across the area when no layout is given: as tall as the area, at most 0.74 of that wide (the
        /// reference's 0.23 W by 0.31 W) and a twentieth of a deck apart, centered.
        /// </summary>
        private IReadOnlyList<Box> Spread(int stacks)
        {
            Rect rect = _area.rect;
            var area = new Box(0f, 0f, Mathf.Max(1f, rect.width), Mathf.Max(1f, rect.height));
            float width = Mathf.Min(area.Height * 0.23f / 0.31f, area.Width / Mathf.Max(1, stacks));
            return ScreenLayout.Row(area, stacks, width * 0.06f, width, square: false);
        }

        /// <summary>Keeps a pod drawn locked until its key lands (the rules opened it already).</summary>
        public void HoldLock(string podId) => _heldLocks.Add(podId);

        /// <summary>The decks turn over in place (Shuffle, US5): the front pods and the buried bands.</summary>
        public void PlayShuffle()
        {
            foreach (PodView pod in _pods.Values)
            {
                if (pod.gameObject.activeSelf)
                {
                    pod.Spin();
                }
            }

            if (isActiveAndEnabled && _buriedShown.Count > 0)
            {
                StartCoroutine(SpinBuried(new List<BuriedPodView>(_buriedShown.Values)));
            }
        }

        private static IEnumerator SpinBuried(List<BuriedPodView> views)
        {
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                var scale = new Vector3(Mathf.Cos(t / 0.35f * Mathf.PI * 2f), 1f, 1f);
                foreach (BuriedPodView view in views)
                {
                    if (view != null)
                    {
                        view.transform.localScale = scale;
                    }
                }

                yield return null;
            }

            foreach (BuriedPodView view in views)
            {
                if (view != null)
                {
                    view.transform.localScale = Vector3.one;
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
        /// Connected pods (FR-035): a link bar joins the shown members of each group across the gap between their frames (or
        /// their bands, when they are buried at the same depth), so it is clear that they commit together; a group whose
        /// members lie at different depths or on two rows of decks, or that shows a single member, marks each shown member
        /// with a ring of its color instead. Each group has its own color.
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
                bool oneRow = members.Count > 1;
                for (int i = 1; i < members.Count; i++)
                {
                    oneRow &= Mathf.Abs(members[i].CenterY - members[0].CenterY) < members[0].Height * 0.5f;
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
                        link.Set(members[i - 1], members[i], color);
                        link.transform.SetAsLastSibling();
                    }

                    continue;
                }

                // Members at different depths or on two rows of decks, or the only member shown: a small ring of the group's
                // color on each one's left, on the front pod's top-left corner or the middle of a buried pod's band.
                foreach (Box box in members)
                {
                    if (marked == _marks.Count)
                    {
                        _marks.Add((UiKit.RoundRect("LinkMarkRim", _area, Color.white), UiKit.RoundRect("LinkMark", _area, Color.white)));
                    }

                    (Image rim, Image dot) = _marks[marked++];
                    float d = box.Width * MarkShare;
                    float x = box.Left + (box.Width * 0.11f);
                    float y = Mathf.Min(box.Top + (box.Width * 0.11f), box.CenterY);
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

        /// <summary>The buried pod view of a stack at a depth (1 or 2), made when first needed.</summary>
        private BuriedPodView Buried(int stack, int depth)
        {
            while (_buried.Count <= stack)
            {
                var views = new BuriedPodView[BuriedShown];
                for (int d = 0; d < BuriedShown; d++)
                {
                    views[d] = UiKit.BuriedPod("Buried" + (d + 1), _area);
                    views[d].gameObject.SetActive(false);
                }

                _buried.Add(views);
            }

            return _buried[stack][Mathf.Clamp(depth, 1, BuriedShown) - 1];
        }

        /// <summary>
        /// The sunk place of an emptied stack (a well of <c>parchment.well</c> mixed toward <c>parchment.edge</c>, radius 18%
        /// of its side <paramref name="side"/>, in canvas units); the wells are made again when the decks change size.
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

        /// <summary>The pod's key landed: its lock opens with a pulse (on its band when it is still buried).</summary>
        public void ShowAccepted(string podId)
        {
            _heldLocks.Remove(podId);
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.gameObject.activeSelf)
            {
                pod.Unlock();
            }
            else if (_buriedShown.TryGetValue(podId, out BuriedPodView? buried) && buried.Locked)
            {
                buried.Show(buried.Variant, false, buried.Depth);
                if (isActiveAndEnabled)
                {
                    StartCoroutine(UiFx.Pop(buried.Symbol, 1.3f, 0.2f));
                }
            }
        }

        /// <summary>Forgets held locks (restart and boosters rebuild from the settled state).</summary>
        public void ReleaseLocks() => _heldLocks.Clear();

        /// <summary>
        /// The on-screen rect of a pod, for tutorial pointers and keys: the front pod, or the small symbol on a buried pod's
        /// band; null when the pod is not shown.
        /// </summary>
        public RectTransform? RectOf(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.gameObject.activeSelf)
            {
                return pod.Rect;
            }

            return _buriedShown.TryGetValue(podId, out BuriedPodView? buried) && buried.gameObject.activeSelf ? buried.Symbol : null;
        }

        /// <summary>
        /// Where a pod's tile is (world position), where its flight to a slot starts and a returned pod lands: the front
        /// pod's sticker tile, or the symbol on a buried pod's band; null when the pod is not shown.
        /// </summary>
        public Vector3? TilePosition(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.gameObject.activeSelf)
            {
                return pod.TileRect.position;
            }

            return _buriedShown.TryGetValue(podId, out BuriedPodView? buried) && buried.gameObject.activeSelf ? buried.Symbol.position : (Vector3?)null;
        }

        /// <summary>The side of a pod's tile in canvas units (see <see cref="TilePosition"/>), or 0 when the pod is not shown.</summary>
        public float TileSize(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod) && pod.gameObject.activeSelf)
            {
                return pod.TileRect.rect.width;
            }

            return _buriedShown.TryGetValue(podId, out BuriedPodView? buried) && buried.gameObject.activeSelf ? buried.Symbol.rect.width : 0f;
        }

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
