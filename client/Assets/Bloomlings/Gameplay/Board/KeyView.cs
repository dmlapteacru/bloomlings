using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// A collected key flying from its tile to its lock (T107, FR-033): a pod in the tray, a slot or a special on the
    /// board. It moves in world space above everything, so it can cross from the board to the tray, and calls back when
    /// it lands so the lock can play its opening.
    /// </summary>
    public sealed class KeyView : MonoBehaviour
    {
        public const float FlightSeconds = 0.55f;

        public static KeyView Fly(RectTransform overlay, Vector3 from, Vector3 to, float size, Action onLanded)
        {
            Image image = UiFactory.CreateImage("Flying key", overlay, ProceduralSprites.Key, UiTheme.EntryMarker);
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = Vector2.one * size;
            var view = image.gameObject.AddComponent<KeyView>();
            view.StartCoroutine(view.Flight(from, to, onLanded));
            return view;
        }

        private IEnumerator Flight(Vector3 from, Vector3 to, Action onLanded)
        {
            for (float t = 0f; t < FlightSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / FlightSeconds);
                transform.position = Vector3.Lerp(from, to, k) + (Vector3.up * (Mathf.Sin(k * Mathf.PI) * 120f));
                transform.localScale = Vector3.one * (1f + (0.4f * Mathf.Sin(k * Mathf.PI)));
                yield return null;
            }

            onLanded();
            Destroy(gameObject);
        }
    }
}
