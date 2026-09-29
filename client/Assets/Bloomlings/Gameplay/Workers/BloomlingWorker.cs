using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Workers
{
    /// <summary>
    /// One Bloomling on the board (T046): it emerges at the Garden Entry, walks its route over open cells, plays a
    /// short restore at the target, then despawns. The states idle, emerge, move, restore and despawn are driven in
    /// code by the timeline's clock, so 2× speed and backlog compression apply. Workers are small and drawn above the
    /// tiles without covering them (doc 12 §8).
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

        private readonly List<Vector2> _path = new List<Vector2>();
        private Image _image = null!;
        private Image _accessory = null!;
        private EventTimeline _timeline = null!;
        private WorkerPool _pool = null!;
        private float _travel;
        private float _time;

        public State Current { get; private set; } = State.Idle;

        public static BloomlingWorker Create(Transform parent, WorkerPool pool, EventTimeline timeline)
        {
            Image image = UiFactory.CreateImage("Bloomling", parent, null, Color.white);
            image.preserveAspect = true;
            var worker = image.gameObject.AddComponent<BloomlingWorker>();
            worker._image = image;
            worker._accessory = UiFactory.CreateImage("Accessory", image.transform, null, Color.white);
            worker._accessory.preserveAspect = true;
            worker._accessory.gameObject.SetActive(false);
            worker._pool = pool;
            worker._timeline = timeline;
            image.gameObject.SetActive(false);
            return worker;
        }

        /// <param name="path">Canvas positions: the entry point, then the route cells ending at the target.</param>
        /// <param name="cosmetic">
        /// The family's equipped cosmetic (FR-063), drawn as a small neutral accessory: a hat above the worker, an
        /// expression on its face, a trail behind it. The variant tint of the body is never changed.
        /// </param>
        public void Launch(Sprite silhouette, Color tint, IReadOnlyList<Vector2> path, float size, float travelSeconds, CosmeticItem? cosmetic = null)
        {
            ShowCosmetic(cosmetic);
            _path.Clear();
            _path.AddRange(path);
            _image.sprite = silhouette;
            _image.color = tint;
            var rect = (RectTransform)transform;
            UiFactory.PlaceAbsolute(rect, _path[0], Vector2.one * size);
            transform.localScale = Vector3.zero;
            transform.SetAsLastSibling();
            _travel = Mathf.Max(0.05f, travelSeconds);
            _time = 0f;
            Current = State.Emerge;
            gameObject.SetActive(true);
        }

        private void ShowCosmetic(CosmeticItem? cosmetic)
        {
            if (cosmetic == null || !cosmetic.IsWorn || !ColorUtility.TryParseHtmlString(cosmetic.Tint, out Color tint))
            {
                _accessory.gameObject.SetActive(false);
                return;
            }

            _accessory.sprite = ProceduralSprites.Accessory(cosmetic.Shape);
            _accessory.color = tint;
            RectTransform rect = _accessory.rectTransform;
            switch (cosmetic.Kind)
            {
                case CosmeticKind.Hat:
                    UiFactory.Place(rect, 0.2f, 0.72f, 0.8f, 1.22f);
                    break;
                case CosmeticKind.Expression:
                    UiFactory.Place(rect, 0.33f, 0.3f, 0.67f, 0.55f);
                    break;
                default:
                    UiFactory.Place(rect, -0.3f, -0.05f, 0.05f, 0.3f);
                    break;
            }

            _accessory.gameObject.SetActive(true);
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

            _time += Time.unscaledDeltaTime * _timeline.Rate;
            var rect = (RectTransform)transform;
            switch (Current)
            {
                case State.Emerge:
                    transform.localScale = Vector3.one * Mathf.Clamp01(_time / EmergeSeconds);
                    Move(rect);
                    if (_time >= EmergeSeconds)
                    {
                        Current = State.Move;
                    }

                    break;
                case State.Move:
                    transform.localScale = Vector3.one;
                    Move(rect);
                    if (_time >= _travel)
                    {
                        Current = State.Restore;
                        _time = 0f;
                    }

                    break;
                case State.Restore:
                    transform.localScale = Vector3.one * (1f + (0.25f * Mathf.Sin(Mathf.Clamp01(_time / EventTimeline.RestoreSeconds) * Mathf.PI)));
                    if (_time >= EventTimeline.RestoreSeconds)
                    {
                        Current = State.Despawn;
                        _time = 0f;
                    }

                    break;
                case State.Despawn:
                    transform.localScale = Vector3.one * (1f - Mathf.Clamp01(_time / DespawnSeconds));
                    if (_time >= DespawnSeconds)
                    {
                        Current = State.Idle;
                        gameObject.SetActive(false);
                        _pool.Release(this);
                    }

                    break;
            }
        }

        private void Move(RectTransform rect)
        {
            float t = Mathf.Clamp01(_time / _travel) * (_path.Count - 1);
            int i = Mathf.Min(Mathf.FloorToInt(t), _path.Count - 2);
            rect.anchoredPosition = _path.Count == 1 ? _path[0] : Vector2.Lerp(_path[i], _path[i + 1], t - i);
        }
    }
}
