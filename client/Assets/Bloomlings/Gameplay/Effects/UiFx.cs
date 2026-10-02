using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Effects
{
    /// <summary>
    /// Short placeholder effects drawn on an overlay rect (doc 12: calm, light, decorative feedback on top of clear
    /// logical tiles): a pod's tile flying between two points and a puff of particles (the win's confetti lives on its
    /// card, <c>UiKit.Confetti</c>). Each effect object runs its own coroutine on unscaled time and destroys itself; none
    /// of them changes what the player can read or do, and the rules never wait for them.
    /// </summary>
    public sealed class UiFx : MonoBehaviour
    {
        /// <summary>
        /// A pod's sticker tile flying from one world position to another (spec 005 §3.7, the playtest's
        /// <c>PodPainter.DrawFlights</c>): <paramref name="tile"/> (the variant's candy tile) over a soft shadow, rising on
        /// an arc <paramref name="lift"/> canvas units high at its middle, its size going from <paramref name="fromSize"/>
        /// to <paramref name="toSize"/> and 12% larger at the top of the arc, the shadow falling further and fading as it
        /// rises.
        /// </summary>
        public static void FlyTile(RectTransform overlay, Sprite tile, Vector3 from, Vector3 to, float fromSize, float toSize, float lift, float seconds, Action? onLanded = null)
        {
            RectTransform host = UiFactory.CreateRect("FlyingTile", overlay);
            float size = Mathf.Max(1f, toSize);
            host.sizeDelta = Vector2.one * size;
            Image shadow = UiKit.RoundRect("Shadow", host, UiTheme.Of(DesignTokens.Colors.GardenShadow.WithAlpha(0.25f)), b => b.Width * 0.2f);
            shadow.rectTransform.sizeDelta = Vector2.one * size;
            Image image = UiFactory.CreateImage("Tile", host, tile, Color.white);
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = Vector2.one * size;
            var fx = host.gameObject.AddComponent<UiFx>();
            fx.StartCoroutine(fx.FlyTileRoutine(shadow, from, to, fromSize / size, lift, seconds, onLanded));
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

        private IEnumerator FlyTileRoutine(Image shadow, Vector3 from, Vector3 to, float fromScale, float lift, float seconds, Action? onLanded)
        {
            RectTransform host = (RectTransform)transform;
            Vector3 up = host.parent != null ? host.parent.TransformVector(new Vector3(0f, lift, 0f)) : new Vector3(0f, lift, 0f);
            float size = host.sizeDelta.x;
            Color shade = shadow.color;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Place(host, shadow, shade, from, to, up, fromScale, size, Mathf.Clamp01(t / seconds));
                yield return null;
            }

            Place(host, shadow, shade, from, to, up, fromScale, size, 1f);
            onLanded?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>The flying tile at <paramref name="k"/> (0–1) of its flight.</summary>
        private static void Place(RectTransform host, Image shadow, Color shade, Vector3 from, Vector3 to, Vector3 up, float fromScale, float size, float k)
        {
            float rise = Mathf.Sin(k * Mathf.PI);
            host.position = Vector3.Lerp(from, to, k) + (up * rise);
            host.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, k) * (1f + (0.12f * rise));
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -size * (0.06f + (0.12f * rise)));
            shadow.color = new Color(shade.r, shade.g, shade.b, shade.a * (1f - (0.5f * rise)));
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
    }
}
