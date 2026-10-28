using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Rounds
{
    /// <summary>
    /// A committed round pod's tile flying from its column to its place (the playtest's <c>RoundPainter.Flights</c>), on a
    /// layer over the whole screen, in screen pixels: it eases from where its tile was when tapped to its place's tile,
    /// lifting in an arc and growing a little, over <see cref="RoundAnimator.FlightUs"/> of display time. Presentation only.
    /// </summary>
    public sealed class RoundFlightsView : MonoBehaviour
    {
        private readonly List<Image> _tiles = new List<Image>();
        private RectTransform _layer = null!;

        public static RoundFlightsView Create(RectTransform layer)
        {
            var view = layer.gameObject.AddComponent<RoundFlightsView>();
            view._layer = layer;
            return view;
        }

        /// <summary>
        /// Shows the flights now: each pod in a place that has a <paramref name="launch"/> box (screen pixels) and is still on
        /// its way to <paramref name="placeTiles"/>' tile of its place (screen pixels).
        /// </summary>
        public void Show(RoundLevelCore core, IReadOnlyList<Box> placeTiles, Box?[] launch)
        {
            RoundAnimator a = core.Animator;
            long now = core.Play.ClockUs;
            int used = 0;
            for (int k = 0; k < Math.Min(a.PlaceCount, placeTiles.Count); k++)
            {
                int pod = a.PodIn(k);
                if (pod < 0 || pod >= launch.Length || !launch[pod].HasValue)
                {
                    continue;
                }

                float q = (now - a.CommittedAt(pod)) / (float)RoundAnimator.FlightUs;
                if (q < 0f || q >= 1f)
                {
                    continue;
                }

                Box from = launch[pod]!.Value;
                Box to = placeTiles[k];
                float lift = Mathf.Sin(q * Mathf.PI);
                float e = 1f - ((1f - q) * (1f - q) * (1f - q));
                float x = from.CenterX + ((to.CenterX - from.CenterX) * e);
                float y = from.CenterY + ((to.CenterY - from.CenterY) * e) - (lift * to.Height * 0.5f);
                float size = (from.Width + ((to.Width - from.Width) * e)) * (1f + (0.12f * lift));
                if (used == _tiles.Count)
                {
                    Image created = UiFactory.CreateImage("Flight", _layer, null, Color.white);
                    created.raycastTarget = false;
                    created.preserveAspect = true;
                    _tiles.Add(created);
                }

                Image tile = _tiles[used++];
                UiKit.PlaceScreen(tile.rectTransform, Box.FromCenter(x, y, size, size));
                tile.sprite = RoundSprites.PodCube(a.MaterialOf(pod), RoundPodLook.Placed, core.Symbols, UiRaster.Quantize(Mathf.Max(8f, to.Width)));
                tile.gameObject.SetActive(true);
            }

            for (int i = used; i < _tiles.Count; i++)
            {
                _tiles[i].gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            foreach (Image tile in _tiles)
            {
                tile.gameObject.SetActive(false);
            }
        }
    }
}
