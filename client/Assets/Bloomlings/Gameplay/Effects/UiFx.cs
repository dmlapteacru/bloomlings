using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Effects
{
    /// <summary>
    /// Short placeholder effects drawn on an overlay rect (doc 12: calm, light, decorative feedback on top of clear
    /// logical tiles): a card flying between two points, a puff of particles, and falling confetti. Each effect object
    /// runs its own coroutine on unscaled time and destroys itself; none of them changes what the player can read or
    /// do, and the rules never wait for them.
    /// </summary>
    public sealed class UiFx : MonoBehaviour
    {
        /// <summary>A card (body color and icon) flying from one world position to another, shrinking as it lands.</summary>
        public static void Fly(RectTransform overlay, Color body, Sprite? icon, Color ink, Vector3 from, Vector3 to, float size, float seconds, Action? onLanded = null)
        {
            Image card = UiFactory.CreateImage("FlyingCard", overlay, ProceduralSprites.RoundedSquare, body);
            card.raycastTarget = false;
            card.rectTransform.sizeDelta = Vector2.one * size;
            if (icon != null)
            {
                Image mark = UiFactory.CreateImage("Icon", card.transform, icon, ink);
                mark.preserveAspect = true;
                UiFactory.Place(mark.rectTransform, 0.18f, 0.18f, 0.82f, 0.82f);
            }

            var fx = card.gameObject.AddComponent<UiFx>();
            fx.StartCoroutine(fx.FlyRoutine(from, to, seconds, onLanded));
        }

        /// <summary>A small puff: <paramref name="count"/> particles thrown outward from a world position, fading as they go.</summary>
        public static void Puff(RectTransform overlay, Vector3 at, Color color, int count, float distance, float size, float seconds, Sprite? sprite = null)
        {
            RectTransform host = UiFactory.CreateRect("Puff", overlay);
            host.position = at;
            var fx = host.gameObject.AddComponent<UiFx>();
            var parts = new Image[count];
            var directions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Image part = UiFactory.CreateImage("Part", host, sprite ?? ProceduralSprites.Circle, color);
                part.raycastTarget = false;
                part.rectTransform.sizeDelta = Vector2.one * size * (0.6f + (0.4f * (((i * 37) % 10) / 10f)));
                float angle = ((i + 0.5f) / count * Mathf.PI * 2f) + (i % 2 == 0 ? 0.2f : -0.2f);
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                parts[i] = part;
            }

            fx.StartCoroutine(fx.PuffRoutine(parts, directions, color, distance, seconds));
        }

        /// <summary>Confetti falling over the whole overlay, in the given colors (the win and milestone celebrations).</summary>
        public static void Confetti(RectTransform overlay, Color[] colors, int count, float seconds)
        {
            RectTransform host = UiFactory.Stretch(UiFactory.CreateRect("Confetti", overlay));
            var fx = host.gameObject.AddComponent<UiFx>();
            Rect area = overlay.rect;
            var parts = new Image[count];
            var starts = new Vector2[count];
            var speeds = new float[count];
            for (int i = 0; i < count; i++)
            {
                // A fixed spread per index: deterministic and even, without a random source.
                float u = ((i * 0.618034f) % 1f);
                float v = ((i * 0.414214f) % 1f);
                Image part = UiFactory.CreateImage("Bit", host, i % 3 == 0 ? ProceduralSprites.Star : ProceduralSprites.RoundedSquare, colors[i % colors.Length]);
                part.raycastTarget = false;
                part.rectTransform.anchorMin = Vector2.zero;
                part.rectTransform.anchorMax = Vector2.zero;
                part.rectTransform.sizeDelta = new Vector2(18f + (14f * v), 26f + (10f * u));
                starts[i] = new Vector2(area.width * u, area.height * (1.02f + (0.25f * v)));
                speeds[i] = area.height * (0.45f + (0.35f * v));
                parts[i] = part;
            }

            fx.StartCoroutine(fx.ConfettiRoutine(parts, starts, speeds, seconds));
        }

        /// <summary>A scale pop on any transform: grows to <paramref name="peak"/> and settles back to 1.</summary>
        public static IEnumerator Pop(Transform target, float peak, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (target == null)
                {
                    yield break;
                }

                target.localScale = Vector3.one * (1f + ((peak - 1f) * Mathf.Sin(t / seconds * Mathf.PI)));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        private IEnumerator FlyRoutine(Vector3 from, Vector3 to, float seconds, Action? onLanded)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                transform.position = Vector3.Lerp(from, to, k);
                transform.localScale = Vector3.one * (1f - (0.25f * k));
                yield return null;
            }

            onLanded?.Invoke();
            Destroy(gameObject);
        }

        private IEnumerator PuffRoutine(Image[] parts, Vector2[] directions, Color color, float distance, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                float travel = distance * (1f - ((1f - k) * (1f - k)));
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i].rectTransform.anchoredPosition = directions[i] * travel;
                    parts[i].color = new Color(color.r, color.g, color.b, color.a * (1f - k));
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator ConfettiRoutine(Image[] parts, Vector2[] starts, float[] speeds, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float fade = Mathf.Clamp01((seconds - t) / 0.4f);
                for (int i = 0; i < parts.Length; i++)
                {
                    float sway = 30f * Mathf.Sin((t * 3f) + i);
                    parts[i].rectTransform.anchoredPosition = starts[i] + new Vector2(sway, -speeds[i] * t);
                    parts[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, (t * 180f) + (i * 40f));
                    Color c = parts[i].color;
                    parts[i].color = new Color(c.r, c.g, c.b, fade);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
