using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The bottom menu in uGUI (spec 005 FR-030, contracts/look.md §6.7), the twin of the playtest's <c>Kit.BottomNav</c>:
    /// Home, the Store page and the Wardrobe each show one (<see cref="BottomNavView"/>), laid out from the kit's
    /// <see cref="ScreenLayout.BottomNav"/>.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// A bottom menu over the whole screen (its parent covers the screen), calling <paramref name="onSelect"/> with the
        /// place a finger taps (with the click; the active place takes no tap). Show it with <see cref="BottomNavView.Show"/>.
        /// </summary>
        public static BottomNavView BottomNav(string name, Transform parent, Action<NavPlace> onSelect)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            UiFactory.Stretch(root);
            var view = root.gameObject.AddComponent<BottomNavView>();
            view.Build(onSelect);
            return view;
        }

        /// <summary>
        /// Shows a place's icon on an image: the owner's picture (<see cref="OwnerPictures.NavIcon"/>, pictures.md D9–D13)
        /// with its aspect kept, or while it is missing the place's glyph over its darker outline
        /// (<see cref="Design.BottomNav.Fallback"/>). Returns whether the owner's picture shows (the glyph takes a smaller box,
        /// <see cref="Design.BottomNav.GlyphBox"/>).
        /// </summary>
        public static bool SetNavIcon(Image image, NavPlace place)
        {
            Sprite? picture = OwnerArt.Icon(OwnerPictures.NavIcon(place));
            if (picture != null)
            {
                OwnerArt.Show(image, picture);
                return true;
            }

            (string shape, Rgba fill, Rgba line) = Design.BottomNav.Fallback(place);
            Sprite glyph = ProceduralSprites.Haloed(shape, fill, line, 0.07f);
            PictureFit.On(image, (w, h) => glyph, square: true);
            return false;
        }
    }

    /// <summary>
    /// The bottom menu (spec 005 FR-030; the playtest's <c>Kit.BottomNav</c>): the wooden bar's picture
    /// (<c>ui.nav.bar</c>, <see cref="UiRaster.NavBar"/>) across the screen's bottom, which takes the taps that fall on it;
    /// one clear touch target per place with its icon, squashing while pressed; and the raised medallion
    /// (<c>ui.nav.medallion</c>, <see cref="UiRaster.NavMedallion"/>) with the active place's icon, which takes no tap.
    /// </summary>
    public sealed class BottomNavView : MonoBehaviour
    {
        private readonly Dictionary<NavPlace, (Image Touch, Image Icon)> _places = new Dictionary<NavPlace, (Image, Image)>();
        private Image _bar = null!;
        private Image _medallion = null!;
        private Image _activeIcon = null!;
        private Action<NavPlace>? _onSelect;
        private NavPlace _active = NavPlace.Home;
        private bool _shown;

        /// <summary>The place in the medallion.</summary>
        public NavPlace Active => _active;

        internal void Build(Action<NavPlace> onSelect)
        {
            _onSelect = onSelect;

            // The bar takes the taps that fall on it, so none reach the stage under it.
            _bar = UiFactory.CreateImage("Bar", transform, null, Color.white, raycast: true);
            foreach (NavPlace place in Design.BottomNav.Order)
            {
                NavPlace p = place;
                Image touch = UiFactory.CreateImage(Design.BottomNav.Key(place), transform, null, Color.clear, raycast: true);
                UiKit.TapTarget(touch, () => _onSelect?.Invoke(p), press: true);
                Image icon = UiFactory.CreateImage("Icon", touch.transform, null, Color.white);
                icon.raycastTarget = false;
                _places[place] = (touch, icon);
                touch.gameObject.SetActive(false);
            }

            _medallion = UiFactory.CreateImage("Medallion", transform, null, Color.white, raycast: true);
            PictureFit.On(_medallion, (w, h) => ProceduralSprites.Picture("ui.nav.medallion", Mathf.Min(w, h), Mathf.Min(w, h), (pw, ph) => UiRaster.NavMedallion(Mathf.Min(pw, ph))), square: true);
            _activeIcon = UiFactory.CreateImage("ActiveIcon", transform, null, Color.white);
            _activeIcon.raycastTarget = false;
        }

        /// <summary>
        /// Lays the menu out for the screen's shape (<see cref="ScreenLayout.BottomNav"/>) with the shown
        /// <paramref name="places"/> (<see cref="Design.BottomNav.Places"/>) and <paramref name="active"/> in the medallion.
        /// </summary>
        public void Show(IReadOnlyList<NavPlace> places, NavPlace active)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            BottomNavRegions r = ScreenLayout.BottomNav(w, h, insets, places, active);
            _active = active;
            _shown = true;
            UiKit.PlaceScreen(_bar.rectTransform, r.Bar);
            NavBarShape shape = r.Shape;
            PictureFit.On(_bar, (pw, ph) => ProceduralSprites.Picture(shape.Key, pw, ph, (x, y) => UiRaster.NavBar(x, y, shape)));
            int activeIndex = r.ActiveIndex;
            foreach (NavPlace place in Design.BottomNav.Order)
            {
                (Image touch, Image icon) = _places[place];
                int index = r.IndexOf(place);
                bool shows = index >= 0 && index != activeIndex;
                touch.gameObject.SetActive(shows);
                if (!shows)
                {
                    continue;
                }

                Box target = r.Touch(index);
                UiKit.PlaceScreen(touch.rectTransform, target);
                Box box = r.Icon(index);
                UiKit.PlaceBox(icon.rectTransform, UiKit.SetNavIcon(icon, place) ? box : Design.BottomNav.GlyphBox(box), target);
            }

            UiKit.PlaceScreen(_medallion.rectTransform, r.Medallion);
            Box medallionIcon = r.Icon(activeIndex);
            UiKit.PlaceScreen(_activeIcon.rectTransform, UiKit.SetNavIcon(_activeIcon, active) ? medallionIcon : Design.BottomNav.GlyphBox(medallionIcon));
        }

        /// <summary>
        /// Where a place shows (a Home demo points at it, roadmap L10–L100): its touch target, the medallion for the active
        /// place, or null while it does not show.
        /// </summary>
        public RectTransform? PlaceRect(NavPlace place)
        {
            if (!_shown || !isActiveAndEnabled)
            {
                return null;
            }

            if (place == _active)
            {
                return _medallion.rectTransform;
            }

            Image touch = _places[place].Touch;
            return touch.gameObject.activeSelf ? touch.rectTransform : null;
        }
    }
}
