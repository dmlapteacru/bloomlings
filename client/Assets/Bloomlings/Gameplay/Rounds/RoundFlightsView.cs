using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

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
        private readonly List<(Image Key, Image Ring, string? Shown)> _keys = new List<(Image, Image, string?)>();
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

        /// <summary>
        /// The opening latches' keys (17 §8, FR-068; the owner's look of 2026-11-02; the playtest's <c>Kit.FlyingKey</c>), in
        /// screen pixels: each flies from its rest box over its board cell (<paramref name="keyRest"/> of the key's cell) to the
        /// keyhole of its padlock in the Twig hero's hands (<paramref name="padlock"/> of the latch; the column's top place when
        /// the hero is not shown), turning, shrinking to the keyhole's scale (<see cref="RoundLook.KeyFlight"/>), then turns in the
        /// hole with the click's ring (<see cref="RoundLook.KeyClick"/>) and fades. The picture keeps its rest size, moved and
        /// scaled.
        /// </summary>
        public void ShowKeys(RoundLevelCore core, Func<int, Box?> keyRest, Func<int, Box?> padlock)
        {
            RoundAnimator a = core.Animator;
            int used = 0;
            for (int latch = 0; latch < a.LatchCount; latch++)
            {
                float since = a.LatchOpenSince(latch);
                Box? rest = since >= 0f ? keyRest(a.Level.Latches[latch].Key) : null;
                Box? held = since >= 0f ? padlock(latch) : null;
                if (!rest.HasValue || !held.HasValue)
                {
                    continue;
                }

                if (used == _keys.Count)
                {
                    Image ring = UiFactory.CreateImage("KeyClick", _layer, ProceduralSprites.Ring, Color.white);
                    ring.raycastTarget = false;
                    Image created = UiFactory.CreateImage("FlyingKey", _layer, null, Color.white);
                    created.raycastTarget = false;
                    _keys.Add((created, ring, null));
                }

                (Image key, Image click, string? shown) = _keys[used];
                Box from = rest.Value;
                Box lockBox = held.Value;
                int pips = a.LatchPips(latch);
                float turns = RoundLook.KeyTurns(Board.RoundBoardView.KeySeconds((a.Now / 1_000_000f) - since, latch));
                (Box box, float sx, bool back, float alpha) = RoundLook.KeyFlight(since, from, RoundLook.KeyInHole(lockBox, pips), turns);
                Rgba metal = RoundLook.KeyMetal(a.KeyMaterial(latch));
                Rgba m = back ? RoundLook.KeyBack(metal) : metal;
                string look = UiRaster.UprightKeyKey(m, pips);
                if (shown != look)
                {
                    shown = look;
                    PictureFit.On(key, (w, h) => RoundSprites.UprightKey(m, pips, w, h));
                }

                UiKit.PlaceScreen(key.rectTransform, Box.FromCenter(box.CenterX, box.CenterY, from.Width, from.Height));
                float k = box.Height / Mathf.Max(1f, from.Height);
                key.rectTransform.localScale = new Vector3(sx * k, k, 1f);
                key.color = new Color(1f, 1f, 1f, alpha);
                key.gameObject.SetActive(alpha > 0f);
                (float radius, float ringAlpha) = RoundLook.KeyClick(since);
                click.gameObject.SetActive(ringAlpha > 0f);
                if (ringAlpha > 0f)
                {
                    (float kx, float ky) = RoundLook.Keyhole(lockBox, pips);
                    float r = radius * lockBox.Width;
                    UiKit.PlaceScreen(click.rectTransform, Box.FromCenter(kx, ky, 2f * r, 2f * r));
                    click.color = UiTheme.Of(C.GardenGlow.WithAlpha(ringAlpha));
                }

                _keys[used++] = (key, click, shown);
            }

            for (int i = used; i < _keys.Count; i++)
            {
                _keys[i].Key.gameObject.SetActive(false);
                _keys[i].Ring.gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            foreach (Image tile in _tiles)
            {
                tile.gameObject.SetActive(false);
            }

            foreach ((Image key, Image ring, string? _) in _keys)
            {
                key.gameObject.SetActive(false);
                ring.gameObject.SetActive(false);
            }
        }
    }
}
