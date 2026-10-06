using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.Gameplay.Workers
{
    /// <summary>
    /// The Bloomlings off the timeline: the win celebration's hopping Bloomlings, and the walkers' cap. The walkers
    /// themselves are the level's clearing style's (spec 005 FR-038, <see cref="Effects.ClearFxView"/>); at most
    /// <see cref="Capacity"/> walkers show at once (60 on low-end devices), the timeline merging a larger wave's walkers.
    /// The look comes from the variant's family, tinted with the variant color (doc 12 §2).
    /// </summary>
    public sealed class WorkerPool : MonoBehaviour
    {
        private BoardView _board = null!;
        private VariantVisualCatalog? _visuals;

        public int Capacity { get; private set; } = 60;

        /// <summary>What each family wears (the Wardrobe, FR-063); presentation only.</summary>
        public System.Func<Family, Outfit>? Outfits { get; set; }

        public static WorkerPool Create(GameObject host, BoardView board, EventTimeline timeline, VariantVisualCatalog? visuals, int capacity)
        {
            var pool = host.AddComponent<WorkerPool>();
            pool._board = board;
            pool._visuals = visuals;
            pool.Capacity = capacity;
            timeline.WorkerCapacity = capacity;
            return pool;
        }

        /// <summary>
        /// The win celebration: one Bloomling per variant of the level, in its colors and outfit, hops below the board
        /// for a moment (decorative; the level is already won).
        /// </summary>
        public void Celebrate(IReadOnlyList<VariantId> variants)
        {
            float size = _board.CellSize * 0.9f;
            int count = Mathf.Min(variants.Count, 6);
            float center = _board.Grid.rect.width / 2f;
            for (int i = 0; i < count; i++)
            {
                VariantVisual visual = _visuals != null ? _visuals.Get(variants[i]) : VariantVisualCatalog.Default(variants[i]);
                BloomlingFigure figure = BloomlingFigure.Create("Cheer", _board.Grid);
                figure.Body.raycastTarget = false;
                var at = new Vector2(center + ((i - ((count - 1) / 2f)) * size * 1.15f), -size * 0.55f);
                UiFactory.PlaceAbsolute(figure.Rect, at, Vector2.one * size);
                figure.ShowCharacter(visual.Id, Outfits?.Invoke(visual.Family));
                figure.Rect.gameObject.AddComponent<Cheer>().Begin(at, size, i * 0.7f);
            }
        }

        /// <summary>A celebrating Bloomling: it hops in place, then leaves.</summary>
        private sealed class Cheer : MonoBehaviour
        {
            private const float Seconds = 1.8f;
            private Vector2 _at;
            private float _size;
            private float _phase;
            private float _time;

            public void Begin(Vector2 at, float size, float phase)
            {
                _at = at;
                _size = size;
                _phase = phase;
            }

            private void Update()
            {
                _time += Time.unscaledDeltaTime;
                var rect = (RectTransform)transform;
                rect.anchoredPosition = _at + new Vector2(0f, Mathf.Abs(Mathf.Sin((_time * 7f) + _phase)) * _size * 0.35f);
                float scale = Mathf.Clamp01(_time / 0.15f) * Mathf.Clamp01((Seconds - _time) / 0.2f);
                transform.localScale = Vector3.one * scale;
                if (_time >= Seconds)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
