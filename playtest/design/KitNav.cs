using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The bottom menu (spec 005 FR-030, contracts/look.md §6.7; Unity's <c>UiKit.BottomNav</c>): the wooden bar, its places'
    /// icons (a locked one with its padlock badge) and the raised medallion of the active place, on Home and the four pages
    /// (the Store, the Wardrobe, the Leaderboard and the Collection); and the notice a locked place's page shows
    /// (<see cref="LockedNotice"/>). It marks the slots it draws; screens only call it.
    /// </summary>
    public static partial class Kit
    {
        // The bar's render function for each picture key (its places' grooves), made once per key.
        private static readonly Dictionary<string, Func<int, int, byte[]>> NavBars = new Dictionary<string, Func<int, int, byte[]>>(StringComparer.Ordinal);

        private static readonly Func<int, int, byte[]> NavMedallionPicture = (w, h) => UiRaster.NavMedallion(Math.Min(w, h));

        /// <summary>
        /// The bottom menu in its regions (<see cref="ScreenLayout.BottomNav"/>): the wooden bar (<c>ui.nav.bar</c>,
        /// <see cref="UiRaster.NavBar"/>) across the screen's bottom; each place's icon on the plank, squashing while
        /// pressed, its touch box calling <paramref name="onSelect"/> with the place, a place <paramref name="look"/> keeps
        /// locked (<see cref="Client.UI.Design.BottomNav.IsOpen"/>) with the padlock badge on its icon's lower right
        /// (<see cref="NavLock"/>), still tappable; then the medallion (<c>ui.nav.medallion</c>,
        /// <see cref="UiRaster.NavMedallion"/>) over the active place with its icon and no badge, which takes no tap.
        /// </summary>
        public static void BottomNav(IPainter p, BottomNavRegions r, HomeLook look, Action<NavPlace> onSelect)
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
                if (!Client.UI.Design.BottomNav.IsOpen(place, look))
                {
                    NavLock(p, icon);
                }

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

        /// <summary>
        /// A locked place's padlock badge (<c>ui.nav.lock</c>) on its icon <paramref name="icon"/>: the outfit cards'
        /// <see cref="LockBadge"/> in <see cref="Client.UI.Design.BottomNav.LockBox"/>, 0.34 of the icon's side at its lower
        /// right, inside the plank's band.
        /// </summary>
        public static void NavLock(IPainter p, Box icon)
        {
            p.Mark("ui.nav.lock");
            Box badge = Client.UI.Design.BottomNav.LockBox(icon);
            LockBadge(p, badge.CenterX, badge.CenterY, Client.UI.Design.BottomNav.LockDisc(badge));
        }

        /// <summary>
        /// The notice of a locked <paramref name="place"/> in <paramref name="area"/> (<c>ui.locked.notice</c>,
        /// <see cref="ScreenLayout.LockedNotice"/>; Unity's <c>UiKit.LockedNotice</c>): the place's icon
        /// (<see cref="NavIcon"/>) with the padlock badge on its lower right (<see cref="NavLock"/>), then "Available from
        /// level <paramref name="level"/>" in <c>type.title</c> <c>ink.brown</c> and "Keep playing to unlock it!" in
        /// <c>type.body</c> <c>ink.brown_soft</c>, each sized by its line and shrunk to its width. Never a touch target.
        /// </summary>
        public static void LockedNotice(IPainter p, Box area, NavPlace place, int level)
        {
            p.Mark("ui.locked.notice");
            LockedNoticeRegions r = ScreenLayout.LockedNotice(area);
            NavIcon(p, place, r.Icon);
            NavLock(p, r.Icon);
            NoticeLine(p, PlaytestText.F("locked.message", NumberText.Group(level)), r.Message, LockedNoticeRegions.MessageTextShare, T.Title, C.InkBrown);
            NoticeLine(p, PlaytestText.T("locked.hint"), r.Hint, LockedNoticeRegions.HintTextShare, T.Body, C.InkBrownSoft);
        }

        /// <summary>A line of the locked notice: <paramref name="share"/> of its box tall, shrunk to the box's width, centered.</summary>
        private static void NoticeLine(IPainter p, string text, Box line, float share, TypeStyle style, Rgba ink)
        {
            float scale = (line.Height * share) / p.U(style.Size);
            float width = p.MeasureText(text, style, scale);
            if (width > line.Width && width > 0f)
            {
                scale *= line.Width / width;
            }

            p.Text(text, line.CenterX, line.CenterY, style, ink, line.Width, scale, TextLook.Plain(ink));
        }
    }
}
