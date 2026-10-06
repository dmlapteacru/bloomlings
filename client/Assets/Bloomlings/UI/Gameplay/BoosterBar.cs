using System;
using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using UnityEngine;

namespace Bloomlings.Client.UI.Gameplay
{
    /// <summary>The state of one booster button.</summary>
    public readonly struct BoosterButtonState
    {
        public BoosterButtonState(bool unlocked, bool applicable, int charges, int price, bool affordable)
        {
            Unlocked = unlocked;
            Applicable = applicable;
            Charges = charges;
            Price = price;
            Affordable = affordable;
        }

        public bool Unlocked { get; }

        /// <summary>The level accepts it now (<c>LevelSession.Check</c>); otherwise the button is disabled (FR-046).</summary>
        public bool Applicable { get; }

        public int Charges { get; }

        public int Price { get; }

        public bool Affordable { get; }
    }

    /// <summary>
    /// The booster bar of the design board's frame 14 (spec 002 FR-014, T120) as the reference's row of four big cream
    /// booster boxes on the tray's parchment, between the Waiting Slots and the pod columns (spec 005 FR-020, contracts/look.md
    /// §3.7, §3.8, §6.1; the playtest's <c>BoosterBarPainter</c>, <see cref="UiKit.BoosterTile"/>; spec 003 FR-031,
    /// contracts/booster-tile.md).
    /// <list type="bullet">
    /// <item><description>Each booster appears at its unlock level as a cream tile in a silver rim with its colored icon:
    /// Extra Slot a white "+" on a blue disc, Shuffle two chasing arrows, Return a yellow arrow, Bloom Burst a pink flower
    /// (FR-042); a locked booster is not shown, and the unlocked ones take the bar's four places in order.</description></item>
    /// <item><description>A dark green count badge shows the owned charges as plain digits. With none left, a cream cost
    /// pill with the lotus and a green "+" show instead, so buying stays clear.</description></item>
    /// <item><description>A booster the level cannot use now is greyed and not pressable (FR-046).</description></item>
    /// <item><description>The booster whose target is being chosen is raised with a pulsing golden glow. Return and
    /// Bloom Burst then ask for a target (a slot, a tile), and the controller runs that step.</description></item>
    /// </list>
    /// The HUD hides the whole bar before the first unlock.
    /// </summary>
    public sealed class BoosterBar : MonoBehaviour
    {
        /// <summary>The space between two tiles, in tile sides: the reference's tiles stand close together.</summary>
        public const float Gap = 0.45f;

        private static readonly BoosterKind[] Order = { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst };
        private static readonly string[] Ids = { "extra_slot", "shuffle", "return", "bloom_burst" };

        private readonly BoosterTileView[] _tiles = new BoosterTileView[4];
        private readonly BoosterTileState[] _states = new BoosterTileState[4];
        private readonly bool[] _unlocked = new bool[4];
        private RectTransform _area = null!;
        private BoosterKind? _targeting;

        public static BoosterBar Create(RectTransform area, Action<BoosterKind> onPress)
        {
            var bar = area.gameObject.AddComponent<BoosterBar>();
            bar._area = area;
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterKind kind = Order[i];
                bar._tiles[i] = UiKit.BoosterTile(kind.ToString(), area, Ids[i], () => onPress(kind));
                bar._tiles[i].gameObject.SetActive(false);
            }

            return bar;
        }

        /// <summary>The tile of a booster, for demo pointers.</summary>
        public RectTransform RectOf(BoosterKind kind) => (RectTransform)_tiles[Array.IndexOf(Order, kind)].transform;

        public void Refresh(Func<BoosterKind, BoosterButtonState> state)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterButtonState s = state(Order[i]);
                _unlocked[i] = s.Unlocked;
                _states[i] = new BoosterTileState(s.Charges, s.Price, _targeting == Order[i], s.Applicable, s.Charges > 0 || s.Affordable);
            }

            Layout();
            for (int i = 0; i < Order.Length; i++)
            {
                Show(i);
            }
        }

        /// <summary>Raises and rings the tile whose target the player is choosing (Return, Bloom Burst).</summary>
        public void SetTargeting(BoosterKind? kind)
        {
            _targeting = kind;
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterTileState s = _states[i];
                _states[i] = new BoosterTileState(s.Charges, s.Price, kind == Order[i], s.Usable, s.Affordable);
                Show(i);
            }
        }

        /// <summary>Draws a tile in its state (contracts/booster-tile.md "States").</summary>
        private void Show(int i)
        {
            _tiles[i].gameObject.SetActive(_unlocked[i]);
            if (_unlocked[i])
            {
                _tiles[i].Show(_states[i]);
            }
        }

        /// <summary>
        /// The four booster boxes in the bar's top-down canvas units (the HUD's <c>GameplayHud.BoosterCells</c>: cream
        /// squircles 0.195 W spread across 0.9 W, spec 005 FR-020, §6.1); null lays them out from the bar's own box.
        /// </summary>
        public Func<IReadOnlyList<Box>>? Places { get; set; }

        /// <summary>
        /// The bar's four places (frame 14; spec 005 §6.1): the boxes <see cref="Places"/> gives, or else square tiles of
        /// <c>size.booster_tile</c> (at most the bar's height less 6 units, and four fitting across), <see cref="Gap"/>
        /// apart and centered, each 4 units above the bar's middle so its cost pill fits below; the unlocked boosters take
        /// them in order.
        /// </summary>
        private void Layout()
        {
            Rect rect = _area.rect;
            var area = new Box(0f, 0f, rect.width, rect.height);
            IReadOnlyList<Box>? given = Places?.Invoke();
            float side = Mathf.Min(UiKit.Units(DesignTokens.Size.BoosterTileWidth), Mathf.Min(area.Height - UiKit.Units(6f), area.Width / (4f + (3f * Gap))));
            if (given == null && side <= 0f)
            {
                return;
            }

            IReadOnlyList<Box> places = given ?? ScreenLayout.Row(area, 4, side * Gap, side, square: false);
            int place = 0;
            for (int i = 0; i < Order.Length && place < places.Count; i++)
            {
                if (!_unlocked[i])
                {
                    continue;
                }

                Box at = places[place++];
                if (given != null)
                {
                    BoxLayout.Place((RectTransform)_tiles[i].transform, at);
                    continue;
                }

                float s = Mathf.Min(side, at.Width);
                BoxLayout.Place((RectTransform)_tiles[i].transform, Box.FromCenter(at.CenterX, area.CenterY - UiKit.Units(4f), s, s));
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_area != null)
            {
                Layout();
            }
        }

        /// <summary>A short pulse when a booster is used.</summary>
        public void Pulse(BoosterKind kind)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(PulseRoutine(_tiles[Array.IndexOf(Order, kind)].transform));
            }
        }

        private static IEnumerator PulseRoutine(Transform target)
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * (1f + (0.1f * Mathf.Sin(t / 0.2f * Mathf.PI)));
                yield return null;
            }

            target.localScale = Vector3.one;
        }
    }
}
