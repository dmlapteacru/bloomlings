using System;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Simulation;
using UnityEngine;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The Source Tray (T043): one column per stack; the exposed pod sits on top, highlighted and tappable, with up to
    /// two buried pods drawn smaller below it. The tray mirrors the logical state directly, because commits are
    /// immediate feedback (R4). Taps are forwarded to the controller.
    /// </summary>
    public sealed class TrayView : MonoBehaviour
    {
        private const int BuriedShown = 2;

        private readonly Dictionary<string, PodView> _pods = new Dictionary<string, PodView>(StringComparer.Ordinal);
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

            Rect area = _area.rect;
            int stacks = view.StackCount;
            float columnWidth = area.width / stacks;
            float size = Mathf.Min(columnWidth * 0.82f, area.height * 0.42f);
            for (int s = 0; s < stacks; s++)
            {
                IReadOnlyList<string> ids = view.Stack(s);
                float x = (s + 0.5f) * columnWidth;

                // Draw buried pods first so the exposed one is on top.
                int shown = Mathf.Min(ids.Count, 1 + BuriedShown);
                for (int d = shown - 1; d >= 0; d--)
                {
                    PodView pod = Get(ids[d]);
                    float scale = d == 0 ? 1f : 0.78f - (0.08f * (d - 1));
                    float y = area.height - (size * 0.55f) - (d * size * 0.62f);
                    UiFactory.PlaceAbsolute(pod.Rect, new Vector2(x, y), Vector2.one * size * scale);
                    pod.Show(view.Pod(ids[d]), _visuals, interactive: d == 0, dimmed: d > 0);
                    pod.transform.SetAsLastSibling();
                }
            }
        }

        public void ShowRefused(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod))
            {
                pod.Shake();
            }
        }

        public void ShowAccepted(string podId)
        {
            if (_pods.TryGetValue(podId, out PodView? pod))
            {
                pod.Pulse();
            }
        }

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
