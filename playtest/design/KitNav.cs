using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The bottom menu (spec 005 FR-030, contracts/look.md §6.7; Unity's <c>UiKit.BottomNav</c>): the wooden bar, its places'
    /// icons and the raised medallion of the active place, on Home, the Store page and the Wardrobe. It marks the slots it
    /// draws; screens only call it.
    /// </summary>
    public static partial class Kit
    {
        // The bar's render function for each picture key (its places' grooves), made once per key.
        private static readonly Dictionary<string, Func<int, int, byte[]>> NavBars = new Dictionary<string, Func<int, int, byte[]>>(StringComparer.Ordinal);

        private static readonly Func<int, int, byte[]> NavMedallionPicture = (w, h) => UiRaster.NavMedallion(Math.Min(w, h));

        /// <summary>
        /// The bottom menu in its regions (<see cref="ScreenLayout.BottomNav"/>): the wooden bar (<c>ui.nav.bar</c>,
        /// <see cref="UiRaster.NavBar"/>) across the screen's bottom; each shown place's icon on the plank, squashing while
        /// pressed, its touch box calling <paramref name="onSelect"/> with the place; then the medallion
        /// (<c>ui.nav.medallion</c>, <see cref="UiRaster.NavMedallion"/>) over the active place with its icon, which takes no
        /// tap.
        /// </summary>
        public static void BottomNav(IPainter p, BottomNavRegions r, Action<NavPlace> onSelect)
        {
            p.Mark("ui.nav.bar");
            NavBarShape shape = r.Shape;
            if (!NavBars.TryGetValue(shape.Key, out Func<int, int, byte[]>? render))
            {
                render = (w, h) => UiRaster.NavBar(w, h, shape);
                NavBars[shape.Key] = render;
            }

            p.Picture(shape.Key, r.Bar, render);
            int active = r.ActiveIndex;
            for (int i = 0; i < r.Places.Count; i++)
            {
                if (i == active)
                {
                    continue;
                }

                NavPlace place = r.Places[i];
                Box touch = r.Touch(i);
                Box icon = r.Icon(i);
                float depth = Press(p, touch, true);
                Squash(p, icon, depth, tile: true);
                NavIcon(p, place, icon);
                p.PopTransform();
                p.Hit(touch, () => onSelect(place));
            }

            p.Mark("ui.nav.medallion");
            p.Picture("ui.nav.medallion", r.Medallion, NavMedallionPicture);
            NavIcon(p, r.Active, r.Icon(active));
        }

        /// <summary>
        /// A place's icon (<see cref="Client.UI.Design.BottomNav.Slot"/>): the owner's picture (<see cref="OwnerPictures.NavIcon"/>) fitted
        /// into <paramref name="box"/>, or while it is missing the place's glyph in its color over a darker outline
        /// (<see cref="Client.UI.Design.BottomNav.Fallback"/>), 72% of the box.
        /// </summary>
        public static void NavIcon(IPainter p, NavPlace place, Box box)
        {
            p.Mark(Client.UI.Design.BottomNav.Slot(place));
            if (OwnerPicture(p, PainterBase.IconPrefix + OwnerPictures.NavIcon(place), box))
            {
                return;
            }

            (string shape, Rgba fill, Rgba line) = Client.UI.Design.BottomNav.Fallback(place);
            p.Mark(shape);
            Box glyph = Client.UI.Design.BottomNav.GlyphBox(box);
            Func<float, float, float> sdf = ShapeLibrary.Get(shape);
            p.ShapeOf(shape + "/line/0.07", (x, y) => sdf(x, y) - 0.07f, glyph, line);
            p.Shape(shape, glyph, fill);
        }
    }
}
