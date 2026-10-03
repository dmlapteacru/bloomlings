using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Workers
{
    /// <summary>
    /// One Bloomling on the board (T046): it emerges at the Garden Entry, hops along its route over open cells facing
    /// the way it walks, plays a short restore at the target, then despawns. The states idle, emerge, move, restore
    /// and despawn are driven in code by the timeline's clock (the time since it set off, <see cref="EventTimeline.Now"/>),
    /// so 2× speed, backlog compression and pauses apply, and it reaches each route cell exactly when the timeline, which
    /// schedules later waves by it, expects. Workers are
    /// drawn above the tiles (doc 12 §8). Each is its variant's 2D character, whose shape is the variant's symbol, so the
    /// target reads without color (doc 12 §6, "moving-character test"; spec 004 FR-010), with a flat shadow under it, and
    /// it wears its family's outfit (FR-063).
    /// </summary>
    public sealed class BloomlingWorker : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Emerge,
            Move,
            Restore,
            Despawn,
        }

        private const float EmergeSeconds = 0.12f;
        private const float DespawnSeconds = 0.12f;

        /// <summary>Hops per route cell, and the hop height as a share of the worker size.</summary>
        private const float HopsPerCell = 1f;
        private const float HopHeight = 0.14f;

        private readonly List<Vector2> _path = new List<Vector2>();
        private BloomlingFigure _figure = null!;
        private EventTimeline _timeline = null!;
        private WorkerPool _pool = null!;
        private float _start;
        private float _travel;
        private float _time;
        private float _size;
        private float _facing = 1f;

        public State Current { get; private set; } = State.Idle;

        public static BloomlingWorker Create(Transform parent, WorkerPool pool, EventTimeline timeline)
        {
            BloomlingFigure figure = BloomlingFigure.Create("Bloomling", parent);
            GameObject host = figure.Rect.gameObject;
            var worker = host.AddComponent<BloomlingWorker>();
            worker._figure = figure;

            // A flat ground shadow just below the feet (the picture's transparent margin), so it never darkens them.
            Image shadow = UiFactory.CreateImage("Shadow", figure.Body.transform, ProceduralSprites.Circle, new Color(0f, 0f, 0f, 0.13f));
            shadow.raycastTarget = false;
            shadow.transform.SetAsFirstSibling();
            UiFactory.Place(shadow.rectTransform, 0.2f, -0.06f, 0.8f, 0.08f);
            worker._pool = pool;
            worker._timeline = timeline;
            host.SetActive(false);
            return worker;
        }

        /// <param name="visual">The variant it clears: its character and family.</param>
        /// <param name="path">Canvas positions: the entry point, then the route cells ending at the target.</param>
        /// <param name="start">When it sets off, on the timeline clock.</param>
        /// <param name="travelSeconds">Its walk to the target, in timeline seconds: it reaches route cell j at (j + 1) / count of it.</param>
        /// <param name="outfit">
        /// What its family wears (FR-063): a thin skin pattern, a hat, an expression, a trail. The character's colors
        /// and shape are never changed.
        /// </param>
        public void Launch(VariantVisual visual, IReadOnlyList<Vector2> path, float size, float start, float travelSeconds, Outfit? outfit = null)
        {
            _figure.ShowCharacter(visual.Id, outfit);
            _path.Clear();
            _path.AddRange(path);
            _size = size;
            _facing = 1f;
            var rect = (RectTransform)transform;
            UiFactory.PlaceAbsolute(rect, _path[0], Vector2.one * size);
            transform.localScale = Vector3.zero;
            transform.SetAsLastSibling();
            _start = start;
            _travel = Mathf.Max(0.05f, travelSeconds);
            _time = 0f;
            Current = State.Emerge;
            gameObject.SetActive(true);
        }

        /// <summary>Immediately returns to the pool (restart).</summary>
        public void Recall()
        {
            Current = State.Idle;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_timeline.Paused || Current == State.Idle)
            {
                return;
            }

            _time = Mathf.Max(0f, _timeline.Now - _start);
            var rect = (RectTransform)transform;
            switch (Current)
            {
                case State.Emerge:
                    Scale(Mathf.Clamp01(_time / EmergeSeconds));
                    Move(rect);
                    if (_time >= EmergeSeconds)
                    {
                        Current = State.Move;
                    }

                    break;
                case State.Move:
                    Scale(1f);
                    Move(rect);
                    if (_time >= _travel)
                    {
                        Current = State.Restore;
                    }

                    break;
                case State.Restore:
                    float restored = (_time - _travel) / EventTimeline.RestoreSeconds;
                    Scale(1f + (0.25f * Mathf.Sin(Mathf.Clamp01(restored) * Mathf.PI)));
                    if (restored >= 1f)
                    {
                        Current = State.Despawn;
                    }

                    break;
                case State.Despawn:
                    float gone = (_time - _travel - EventTimeline.RestoreSeconds) / DespawnSeconds;
                    Scale(1f - Mathf.Clamp01(gone));
                    if (gone >= 1f)
                    {
                        Current = State.Idle;
                        gameObject.SetActive(false);
                        _pool.Release(this);
                    }

                    break;
            }
        }

        /// <summary>Walks the route: a small hop per cell, turned toward the next cell (the trail sways behind).</summary>
        private void Move(RectTransform rect)
        {
            if (_path.Count == 1)
            {
                rect.anchoredPosition = _path[0];
                return;
            }

            float t = Mathf.Clamp01(_time / _travel) * (_path.Count - 1);
            int i = Mathf.Min(Mathf.FloorToInt(t), _path.Count - 2);
            Vector2 step = _path[i + 1] - _path[i];
            if (Mathf.Abs(step.x) > 0.01f)
            {
                _facing = step.x < 0f ? -1f : 1f;
            }

            float hop = Mathf.Abs(Mathf.Sin((t - i) * Mathf.PI * HopsPerCell)) * HopHeight * _size;
            rect.anchoredPosition = Vector2.Lerp(_path[i], _path[i + 1], t - i) + new Vector2(0f, hop);
            _figure.Trail.localEulerAngles = new Vector3(0f, 0f, 12f * Mathf.Sin(_time * 18f));
        }

        private void Scale(float scale) => transform.localScale = new Vector3(scale * _facing, scale, 1f);
    }
}
