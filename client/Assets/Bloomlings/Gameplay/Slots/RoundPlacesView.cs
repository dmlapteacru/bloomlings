using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Rounds;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Slots
{
    /// <summary>
    /// A round level's places (spec 006 T059, contracts/presentation.md; the playtest's <c>RoundPainter.Places</c>): the
    /// game's cream plates, each holding its pod's material tile with no variant icon (the symbol only in the symbols mode)
    /// and its count; the last free place in danger; a pod with work left and no cube of its material in reach greyed with
    /// the hourglass; a pod flying in fades in as it lands and a finished one puffs away; while Return waits, the places it
    /// can take back glow and take the tap; the link tubes between linked places, under the plates (FR-043). It reads the
    /// round level's state each frame (<see cref="Show"/>). Presentation only.
    /// </summary>
    public sealed class RoundPlacesView : MonoBehaviour
    {
        private readonly List<Place> _places = new List<Place>();
        private readonly List<Image> _tubes = new List<Image>();
        private RectTransform _area = null!;
        private RectTransform _back = null!;

        /// <summary>A place was tapped while Return waits for its target (the place's index).</summary>
        public event Action<int>? PlaceTapped;

        public static RoundPlacesView Create(RectTransform area)
        {
            var view = area.gameObject.AddComponent<RoundPlacesView>();
            view._area = area;
            view._back = UiFactory.Stretch(UiFactory.CreateRect("RoundTubes", area));
            return view;
        }

        /// <summary>The plate of place <paramref name="place"/> (the guided Return's hole), or null.</summary>
        public RectTransform? PlateRect(int place) => place >= 0 && place < _places.Count ? (RectTransform)_places[place].Base.transform : null;

        /// <summary>
        /// Shows the places of <paramref name="core"/>'s round level on <paramref name="cells"/> (the slot row's top-down
        /// canvas units, one box per place).
        /// </summary>
        public void Show(RoundLevelCore core, IReadOnlyList<Box> cells)
        {
            RoundAnimator a = core.Animator;
            int count = Math.Min(a.PlaceCount, cells.Count);
            while (_places.Count < count)
            {
                _places.Add(Place.Create(_area, _places.Count, i => PlaceTapped?.Invoke(i)));
            }

            core.RefreshReach();
            bool returning = core.Targeting == BoosterKind.Return;
            long now = core.Play.ClockUs;
            for (int k = 0; k < _places.Count; k++)
            {
                Place place = _places[k];
                if (k >= count)
                {
                    place.Hide();
                    continue;
                }

                place.Lay(cells[k]);
                bool extra = k >= RoundRules.Capacity;
                int pod = a.PodIn(k);
                RoundPlaceLook look = core.PlaceLookOf(k);
                if (pod < 0)
                {
                    place.ShowEmpty(look == RoundPlaceLook.Danger, extra);
                    (int left, long leftAt) = a.Leaving(k);
                    float q = left >= 0 && now >= leftAt ? (now - leftAt) / (float)RoundAnimator.ExitUs : 1f;
                    place.Puff(left >= 0 && q < 1f ? a.MaterialOf(left) : -1, q, core.Symbols);
                    place.SetTarget(false, false);
                    continue;
                }

                place.Puff(-1, 1f, core.Symbols);
                float arrival = core.Arrival(pod);
                // The side face by the place across the bar, which is centred on the screen and nearly as wide (constitution 1.2.0 VII).
                float middle = (cells[0].Left + cells[count - 1].Right) / 2f;
                float reach = (cells[count - 1].Right - cells[0].Left) / 2f / RoundScreenLook.BoardWidthShare;
                int side = RoundLook.SideStep(cells[k].CenterX - middle, reach);
                place.ShowPod(a.MaterialOf(pod), a.Remaining(pod), look == RoundPlaceLook.Stuck, extra, core.Symbols, 0.25f + (0.75f * arrival * arrival), side);
                bool target = returning && core.CanReturn(pod);
                place.SetTarget(target, target && core.HudLive);
            }

            // The tubes between linked places, before the plates (FR-043).
            int used = 0;
            foreach (RoundTube tube in a.Tubes(int.MaxValue))
            {
                int pa = a.PlaceOf(tube.First);
                int pb = a.PlaceOf(tube.Second);
                if (tube.Place != RoundTubePlace.Places || pa < 0 || pb < 0 || pa >= count || pb >= count)
                {
                    continue;
                }

                if (used == _tubes.Count)
                {
                    _tubes.Add(RoundTubes.Create("LinkTube", _back));
                }

                RoundTubes.Show(_tubes[used++], LinkTube.Between(cells[pa], RoundLook.ColorOf(a.MaterialOf(tube.First)), cells[pb], RoundLook.ColorOf(a.MaterialOf(tube.Second))));
            }

            for (int i = used; i < _tubes.Count; i++)
            {
                _tubes[i].gameObject.SetActive(false);
            }

            if (_back.GetSiblingIndex() != 0)
            {
                _back.SetAsFirstSibling();
            }
        }

        /// <summary>One place: its empty plate, the pod's plate over it (fading in), the puff of a finished pod and Return's glow.</summary>
        private sealed class Place
        {
            private readonly CanvasGroup _podFade;
            private readonly Image _glow;
            private readonly Image _puff;
            private readonly Button _touch;
            private Box _box;
            private (bool Danger, bool Extra)? _empty;
            private (int Material, int Count, bool Stuck, bool Extra, bool Symbols, int Side)? _pod;
            private int _puffKey = -1;

            private Place(SlotPlateView basePlate, SlotPlateView podPlate, CanvasGroup podFade, Image glow, Image puff, Button touch)
            {
                Base = basePlate;
                Pod = podPlate;
                _podFade = podFade;
                _glow = glow;
                _puff = puff;
                _touch = touch;
            }

            public SlotPlateView Base { get; }

            public SlotPlateView Pod { get; }

            public static Place Create(RectTransform area, int index, Action<int> tapped)
            {
                Image glow = UiKit.RoundRect("ReturnGlow", area, UiTheme.Of(C.GardenGlow.WithAlpha(0.55f)), b => Mathf.Min(b.Width, b.Height) * 0.26f);
                glow.raycastTarget = false;
                glow.gameObject.SetActive(false);
                SlotPlateView basePlate = UiKit.SlotPlate("Place", area);
                SlotPlateView podPlate = UiKit.SlotPlate("Pod", area);
                CanvasGroup fade = podPlate.gameObject.AddComponent<CanvasGroup>();
                fade.blocksRaycasts = false;
                Image puff = UiFactory.CreateImage("Puff", area, null, Color.white);
                puff.raycastTarget = false;
                puff.gameObject.SetActive(false);
                Image touchImage = UiFactory.CreateImage("ReturnTouch", area, null, Color.clear, raycast: true);
                Button touch = touchImage.gameObject.AddComponent<Button>();
                touch.transition = Selectable.Transition.None;
                touch.targetGraphic = touchImage;
                touch.onClick.AddListener(() => tapped(index));
                touchImage.gameObject.SetActive(false);
                return new Place(basePlate, podPlate, fade, glow, puff, touch);
            }

            public void Lay(Box box)
            {
                if (box.Equals(_box))
                {
                    return;
                }

                _box = box;
                BoxLayout.Place((RectTransform)Base.transform, box);
                BoxLayout.Place((RectTransform)Pod.transform, box);
                BoxLayout.Place(_glow.rectTransform, box.Inset(-Mathf.Min(box.Width, box.Height) * 0.08f));
                BoxLayout.Place(_puff.rectTransform, ReferenceGameplayRegions.SlotTile(box));
                float min = UiKit.Units(DesignTokens.Size.TouchMin);
                BoxLayout.Place((RectTransform)_touch.transform, Box.FromCenter(box.CenterX, box.CenterY, Mathf.Max(box.Width, min), Mathf.Max(box.Height, min)));
            }

            public void ShowEmpty(bool danger, bool extra)
            {
                Base.gameObject.SetActive(true);
                Pod.gameObject.SetActive(false);
                _pod = null;
                var empty = (danger, extra);
                if (!_empty.HasValue || !_empty.Value.Equals(empty))
                {
                    _empty = empty;
                    Base.Show(danger ? SlotPlateState.Danger : SlotPlateState.Empty, extra: extra);
                }
            }

            public void ShowPod(int material, int count, bool stuck, bool extra, bool symbols, float alpha, int side = 0)
            {
                Base.gameObject.SetActive(alpha < 1f);
                if (alpha < 1f && (!_empty.HasValue || _empty.Value.Danger || _empty.Value.Extra != extra))
                {
                    _empty = (false, extra);
                    Base.Show(SlotPlateState.Empty, extra: extra);
                }

                Pod.gameObject.SetActive(true);
                float fade = Mathf.Clamp01(alpha);
                if (Mathf.Abs(_podFade.alpha - fade) > 0.001f)
                {
                    _podFade.alpha = fade;
                }

                var pod = (material, count, stuck, extra, symbols, side);
                if (!_pod.HasValue || !_pod.Value.Equals(pod))
                {
                    _pod = pod;
                    Pod.ShowMaterial(stuck ? SlotPlateState.Stuck : SlotPlateState.Working, material, count, extra, symbols, side);
                }
            }

            /// <summary>A finished pod's tile puffing off the plate (<paramref name="material"/> -1: none) at <paramref name="q"/> of its puff.</summary>
            public void Puff(int material, float q, bool symbols)
            {
                bool on = material >= 0 && q < 1f;
                _puff.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                int key = (material * 2) + (symbols ? 1 : 0);
                if (key != _puffKey)
                {
                    _puffKey = key;
                    PictureFit.On(_puff, (w, h) => RoundSprites.PodCube(material, RoundPodLook.Placed, symbols, Math.Min(w, h)), square: true);
                }

                _puff.color = new Color(1f, 1f, 1f, 1f - q);
                _puff.rectTransform.localScale = Vector3.one * (1f + (0.3f * q));
            }

            /// <summary>Return's glow round a place it can take back, and its touch while the HUD takes taps.</summary>
            public void SetTarget(bool glow, bool tappable)
            {
                _glow.gameObject.SetActive(glow);
                if (tappable && !_touch.gameObject.activeSelf)
                {
                    _touch.transform.SetAsLastSibling();
                }

                _touch.gameObject.SetActive(tappable);
            }

            public void Hide()
            {
                Base.gameObject.SetActive(false);
                Pod.gameObject.SetActive(false);
                _glow.gameObject.SetActive(false);
                _puff.gameObject.SetActive(false);
                _touch.gameObject.SetActive(false);
                _empty = null;
                _pod = null;
            }
        }
    }
}
