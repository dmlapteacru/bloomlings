using System;
using System.Collections.Generic;
using System.Globalization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>The places of the bottom menu (spec 005 FR-030, contracts/look.md §6.7), in their order from the left.</summary>
    public enum NavPlace
    {
        /// <summary>The Store page (open from L12; before it, the page says so).</summary>
        Shop,

        /// <summary>The Wardrobe page (open from L40; before it, the page says so).</summary>
        Wardrobe,

        /// <summary>Home, always open.</summary>
        Home,

        /// <summary>The Leaderboard card over Home (open from L10; before it, the card says so).</summary>
        Leaderboard,

        /// <summary>The Collection card over Home (open once a picture is won, from L2; before it, the card says so).</summary>
        Collection,
    }

    /// <summary>
    /// The bottom menu's rules (spec 005 FR-030, contracts/look.md §6.7; the owner's request of 2026-10-04, the wooden
    /// variant): the places in their order, which are open, from which level, their names, slots and stand-in glyphs, both
    /// builds. Every place always shows (the owner's request of 2026-10-04: "the menu's places must always be visible"). A
    /// place is open once its feature is unlocked (<see cref="IsOpen"/>): the Shop with <see cref="HomeLook.Store"/>, the
    /// Wardrobe with <see cref="HomeLook.Wardrobe"/>, the Leaderboard with <see cref="HomeLook.Rank"/>, the Collection
    /// with <see cref="HomeLook.Collection"/>; Home always. A locked place keeps its icon, still tappable, with a padlock
    /// badge (<see cref="LockBox"/>), and its page or card says from which level it is available
    /// (<see cref="UnlockLevel"/>, <see cref="ScreenLayout.LockedNotice"/>). Engine-free.
    /// </summary>
    public static class BottomNav
    {
        /// <summary>
        /// The plank's height, as a share of W (the owner's icons fill it, 2026-10-04). With <see cref="RiseShare"/> it keeps
        /// the menu's top 0.17 W above the safe bottom, so the screens above it keep their room.
        /// </summary>
        public const float PlankShare = 0.14f;

        /// <summary>The raised medallion's box, as a share of W, and how far it rises above the plank's top.</summary>
        public const float MedallionShare = 0.2f;

        public const float RiseShare = 0.03f;

        /// <summary>The wooden disc's diameter, as a share of the medallion's box (its leaves and flowers take the rest).</summary>
        public const float DiscShare = 0.88f;

        /// <summary>The plank's ends, as shares of W from the safe area's sides.</summary>
        public const float EndShare = 0.03f;

        /// <summary>Where the places' span starts and ends, as shares of W (the places share it evenly, the plank's whole length).</summary>
        public const float SpanStart = 0.04f;

        public const float SpanEnd = 0.96f;

        /// <summary>
        /// A place's icon on the plank, as a share of the plank's height: the whole band (the owner's pictures keep their own
        /// thin margin), so the icons leave little of the plank free.
        /// </summary>
        public const float IconShare = 1f;

        /// <summary>The active place's icon in the medallion, as a share of the disc's diameter (a little larger than the plank's).</summary>
        public const float MedallionIconShare = 0.86f;

        /// <summary>The grooves between the places: their length as a share of the plank's height.</summary>
        public const float GrooveShare = 0.5f;

        /// <summary>The room a screen keeps between its lowest content and the menu's top, as a share of W.</summary>
        public const float GapShare = 0.015f;

        /// <summary>
        /// A locked place's padlock badge, as a share of its icon's side (the whole badge, its ring included; the outfit
        /// cards' <c>Kit.LockBadge</c> recipe).
        /// </summary>
        public const float LockShare = 0.34f;

        /// <summary>The room the padlock badge keeps from its icon's right and bottom edges, as a share of the icon's side.</summary>
        public const float LockInsetShare = 0.03f;

        /// <summary>
        /// The level the Collection opens from: its first picture comes with Level 1's win, so a player on Level 2 has one
        /// (the Collection is open once it holds a picture, <see cref="HomeLook.Collection"/>; the roadmap has no unlock
        /// for it).
        /// </summary>
        public const int CollectionLevel = 2;

        /// <summary>The places in their order from the left: all five always show.</summary>
        public static IReadOnlyList<NavPlace> Order { get; } = new[] { NavPlace.Shop, NavPlace.Wardrobe, NavPlace.Home, NavPlace.Leaderboard, NavPlace.Collection };

        /// <summary>
        /// Whether <paramref name="place"/> is open for a player whose Home looks like <paramref name="look"/>: Home always,
        /// the others once their feature is unlocked. A place that is not open still shows, with a padlock badge, and its
        /// page or card says from which level it is available (<see cref="UnlockLevel"/>).
        /// </summary>
        public static bool IsOpen(NavPlace place, HomeLook look) => place switch
        {
            NavPlace.Shop => look.Store,
            NavPlace.Wardrobe => look.Wardrobe,
            NavPlace.Leaderboard => look.Rank,
            NavPlace.Collection => look.Collection,
            _ => true,
        };

        /// <summary>
        /// The level from which <paramref name="place"/> is available, as its locked page or card says: the roadmap's level
        /// of the feature's unlock (<paramref name="levelOf"/>, the build's own roadmap, <c>UnlockRoadmap.LevelOf</c>): the
        /// Shop's <see cref="HomeLook.StoreUnlock"/> (L12), the Wardrobe's <see cref="HomeLook.WardrobeUnlock"/> (L40),
        /// the Leaderboard's <see cref="HomeLook.LeaderboardUnlock"/> (L10); the Collection from
        /// <see cref="CollectionLevel"/>; Home from Level 1. An unlock the roadmap does not list counts from Level 1.
        /// </summary>
        public static int UnlockLevel(NavPlace place, Func<string, int?> levelOf)
        {
            string? unlock = place switch
            {
                NavPlace.Shop => HomeLook.StoreUnlock,
                NavPlace.Wardrobe => HomeLook.WardrobeUnlock,
                NavPlace.Leaderboard => HomeLook.LeaderboardUnlock,
                _ => null,
            };

            if (unlock != null)
            {
                return Math.Max(1, levelOf(unlock) ?? 1);
            }

            return place == NavPlace.Collection ? CollectionLevel : 1;
        }

        /// <summary>
        /// A locked place's padlock badge in its icon box (<c>ui.nav.lock</c>; the locked notice's too): a square
        /// <see cref="LockShare"/> of the icon's side at its lower right corner, <see cref="LockInsetShare"/> inside its
        /// right and bottom edges, so on the plank it stays inside the band.
        /// </summary>
        public static Box LockBox(Box icon)
        {
            float side = Math.Min(icon.Width, icon.Height);
            float badge = side * LockShare;
            float inset = side * LockInsetShare;
            return new Box(icon.Right - inset - badge, icon.Bottom - inset - badge, icon.Right - inset, icon.Bottom - inset);
        }

        /// <summary>
        /// The padlock badge's cream disc in its box (<see cref="LockBox"/>): the box less its <c>cream.line</c> ring, 8% of
        /// the disc on each side (<c>Kit.LockBadge</c>, <c>UiKit.LockBadge</c>).
        /// </summary>
        public static float LockDisc(Box badge) => Math.Min(badge.Width, badge.Height) / 1.16f;

        /// <summary>A place's name in file names and keys: <c>shop</c>, <c>wardrobe</c>, <c>home</c>, <c>leaderboard</c>, <c>collection</c>.</summary>
        public static string Key(NavPlace place) => place switch
        {
            NavPlace.Shop => "shop",
            NavPlace.Wardrobe => "wardrobe",
            NavPlace.Leaderboard => "leaderboard",
            NavPlace.Collection => "collection",
            _ => "home",
        };

        /// <summary>The asset slot of a place's icon (the owner's picture, <see cref="OwnerPictures.NavIcon"/>).</summary>
        public static string Slot(NavPlace place) => place switch
        {
            NavPlace.Shop => "icon.nav.shop",
            NavPlace.Wardrobe => "icon.nav.wardrobe",
            NavPlace.Leaderboard => "icon.nav.leaderboard",
            NavPlace.Collection => "icon.nav.collection",
            _ => "icon.nav.home",
        };

        /// <summary>
        /// The glyph a place shows while the owner's icon is missing, as the booster icons fall back to their drawn icon:
        /// the reward basket (Shop, <c>currency.reward_basket</c>), the shirt (Wardrobe, <c>ui.shirt</c>), the fountain
        /// (Home, <c>special.fountain</c>), the trophy (Leaderboard, <c>ui.trophy</c>) and the grid (Collection,
        /// <c>ui.grid</c>), each in a saturated color over its darker outline (the shape grown by 0.07 shape units).
        /// </summary>
        public static (string ShapeId, Rgba Fill, Rgba Line) Fallback(NavPlace place) => place switch
        {
            NavPlace.Shop => ("currency.reward_basket", C.RewardBasket, C.WoodDarkLine),
            NavPlace.Wardrobe => ("ui.shirt", C.LotusFill, C.LotusLine),
            NavPlace.Leaderboard => ("ui.trophy", C.MedalGold, C.MedalGold.Darken(0.42f)),
            NavPlace.Collection => ("ui.grid", C.BadgeSuperHard, C.BadgeSuperHard.Darken(0.42f)),
            _ => ("special.fountain", C.ButtonBlue, C.ButtonBlue.Darken(0.42f)),
        };

        /// <summary>The stand-in glyph's box in a place's icon box: 14% smaller at each side (the owner's pictures keep their own margin).</summary>
        public static Box GlyphBox(Box icon) => icon.Inset(icon.Width * 0.14f);
    }

    /// <summary>
    /// What the bar's wooden picture shows (<see cref="UiRaster.NavBar"/>), as shares of its box (<see cref="BottomNavRegions.Bar"/>):
    /// the plank's ends (<see cref="PlankLeft"/>, <see cref="PlankRight"/> of the width), its top (<see cref="PlankTop"/>
    /// of the height), the bottom of its band in the safe area (<see cref="BandBottom"/>; the wood
    /// runs on to the picture's bottom, the screen's), and the grooves between the places (<see cref="Grooves"/>, of the
    /// width). <see cref="Key"/> names the picture in both builds' caches.
    /// </summary>
    public sealed class NavBarShape
    {
        public NavBarShape(float plankLeft, float plankRight, float plankTop, float bandBottom, IReadOnlyList<float> grooves)
        {
            PlankLeft = plankLeft;
            PlankRight = plankRight;
            PlankTop = plankTop;
            BandBottom = bandBottom;
            Grooves = grooves;
            var key = new System.Text.StringBuilder("ui.nav.bar/");
            key.Append(Share(plankLeft)).Append('/').Append(Share(plankRight)).Append('/').Append(Share(plankTop)).Append('/').Append(Share(bandBottom));
            foreach (float groove in grooves)
            {
                key.Append('/').Append(Share(groove));
            }

            Key = key.ToString();
        }

        public float PlankLeft { get; }

        public float PlankRight { get; }

        public float PlankTop { get; }

        public float BandBottom { get; }

        public IReadOnlyList<float> Grooves { get; }

        /// <summary>The picture's cache key: <c>ui.nav.bar/</c> and its shares.</summary>
        public string Key { get; }

        private static string Share(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The bottom menu (spec 005 FR-030, contracts/look.md §6.7), both builds (<c>Kit.BottomNav</c>, <c>UiKit.BottomNav</c>):
    /// <list type="bullet">
    /// <item><description><see cref="Bar"/>: the box of the bar's wooden picture, the whole screen's width from the plank's
    /// top to the screen's bottom (the wood runs on behind the bottom inset).</description></item>
    /// <item><description><see cref="Plank"/>: the plank's band, <see cref="BottomNav.PlankShare"/> of W tall, its bottom on
    /// the safe area's bottom.</description></item>
    /// <item><description><see cref="PlaceBoxes"/>: the shown places' columns on the band, sharing the span from
    /// <see cref="BottomNav.SpanStart"/> to <see cref="BottomNav.SpanEnd"/> of W evenly, in order.</description></item>
    /// <item><description><see cref="Medallion"/>: the raised round medallion over the active place, rising
    /// <see cref="BottomNav.RiseShare"/> of W above the plank (a little smaller on a screen without a bottom inset, so its
    /// disc stays on the screen).</description></item>
    /// </list>
    /// Screen pixels, y down. Engine-free.
    /// </summary>
    public sealed record BottomNavRegions(
        Box Safe,
        float W,
        Box Bar,
        Box Plank,
        IReadOnlyList<NavPlace> Places,
        IReadOnlyList<Box> PlaceBoxes,
        NavPlace Active,
        Box Medallion,
        float TouchMin)
    {
        /// <summary>The highest point of the menu (the medallion's top): screens keep their content above it.</summary>
        public float Top => Math.Min(Bar.Top, Medallion.Top);

        /// <summary>The active place's index in <see cref="Places"/>.</summary>
        public int ActiveIndex => IndexOf(Active);

        /// <summary>A place's index in <see cref="Places"/>, or −1 when it does not show.</summary>
        public int IndexOf(NavPlace place)
        {
            for (int i = 0; i < Places.Count; i++)
            {
                if (Places[i] == place)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The medallion's wooden disc (<see cref="BottomNav.DiscShare"/> of its box, centered).</summary>
        public Box Disc => Box.FromCenter(Medallion.CenterX, Medallion.CenterY, Medallion.Width * BottomNav.DiscShare, Medallion.Height * BottomNav.DiscShare);

        /// <summary>
        /// Place <paramref name="index"/>'s icon: a square <see cref="BottomNav.IconShare"/> of the plank's height on its
        /// column's middle and the band's middle (no wider than its column); the active place's in the medallion's disc
        /// (<see cref="BottomNav.MedallionIconShare"/> of it).
        /// </summary>
        public Box Icon(int index)
        {
            if (index == ActiveIndex)
            {
                Box disc = Disc;
                float m = disc.Width * BottomNav.MedallionIconShare;
                return Box.FromCenter(disc.CenterX, disc.CenterY - (disc.Height * 0.02f), m, m);
            }

            Box column = PlaceBoxes[index];
            float side = Math.Min(Plank.Height * BottomNav.IconShare, column.Width);
            return Box.FromCenter(column.CenterX, Plank.CenterY, side, side);
        }

        /// <summary>
        /// Place <paramref name="index"/>'s touch box: its column from the safe bottom up the plank's height (at least the
        /// touch minimum), a neighbor of the active place cut clear of the medallion. The active place takes no tap.
        /// </summary>
        public Box Touch(int index)
        {
            Box column = PlaceBoxes[index];
            float top = Math.Max(Safe.Top, Safe.Bottom - Math.Max(Plank.Height, TouchMin));
            float left = column.Left;
            float right = column.Right;
            if (index != ActiveIndex)
            {
                if (column.CenterX > Medallion.CenterX)
                {
                    left = Math.Max(left, Math.Min(Medallion.Right, column.CenterX - (TouchMin / 2f)));
                }
                else
                {
                    right = Math.Min(right, Math.Max(Medallion.Left, column.CenterX + (TouchMin / 2f)));
                }
            }

            return new Box(left, top, right, Safe.Bottom);
        }

        /// <summary>Everything a finger can press: the places but the active one.</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons
        {
            get
            {
                var buttons = new List<(string, Box)>();
                for (int i = 0; i < Places.Count; i++)
                {
                    if (i != ActiveIndex)
                    {
                        buttons.Add((BottomNav.Key(Places[i]), Touch(i)));
                    }
                }

                return buttons;
            }
        }

        /// <summary>The bar's wooden picture: its plank, its band and the grooves between the places (<see cref="NavBarShape"/>).</summary>
        public NavBarShape Shape
        {
            get
            {
                float w = Math.Max(1f, Bar.Width);
                float h = Math.Max(1f, Bar.Height);
                var grooves = new List<float>();
                for (int i = 1; i < PlaceBoxes.Count; i++)
                {
                    grooves.Add((PlaceBoxes[i].Left - Bar.Left) / w);
                }

                return new NavBarShape((Plank.Left - Bar.Left) / w, (Plank.Right - Bar.Left) / w, (Plank.Top - Bar.Top) / h, (Plank.Bottom - Bar.Top) / h, grooves);
            }
        }
    }

    /// <content>The bottom menu's layout (contracts/look.md §6.7).</content>
    public static partial class ScreenLayout
    {
        /// <summary>
        /// The bottom menu (contracts/look.md §6.7) for the shown <paramref name="places"/> (the screens show all five,
        /// <see cref="Design.BottomNav.Order"/>; the <paramref name="active"/> one is added in its order when missing), in
        /// fractions of the safe width W: the plank 0.14 W tall from 0.03 W to 0.97 W, its bottom on the safe bottom, the
        /// wood running on to the screen's bottom; the bar's picture from the plank's top to the screen's bottom, across
        /// the screen; the places sharing 0.04 W to 0.96 W evenly; the medallion 0.2 W square on the active place, its top
        /// 0.03 W above the plank's (smaller when its disc would leave the screen: on a phone without a bottom inset its
        /// disc ends on the screen's bottom, about 0.18 W).
        /// </summary>
        public static BottomNavRegions BottomNav(float width, float height, Insets insets, IReadOnlyList<NavPlace> places, NavPlace active)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float plankTop = safe.Bottom - (Design.BottomNav.PlankShare * w);
            var plank = new Box(safe.Left + (Design.BottomNav.EndShare * w), plankTop, safe.Right - (Design.BottomNav.EndShare * w), safe.Bottom);
            var bar = new Box(0f, plankTop, width, height);

            var shown = new List<NavPlace>();
            foreach (NavPlace place in Design.BottomNav.Order)
            {
                bool listed = false;
                foreach (NavPlace p in places)
                {
                    listed |= p == place;
                }

                if (listed || place == active)
                {
                    shown.Add(place);
                }
            }

            float start = safe.Left + (Design.BottomNav.SpanStart * w);
            float span = (Design.BottomNav.SpanEnd - Design.BottomNav.SpanStart) * w;
            float cell = span / shown.Count;
            var boxes = new Box[shown.Count];
            int activeIndex = 0;
            for (int i = 0; i < shown.Count; i++)
            {
                boxes[i] = new Box(start + (i * cell), plank.Top, start + ((i + 1) * cell), plank.Bottom);
                if (shown[i] == active)
                {
                    activeIndex = i;
                }
            }

            float top = MedallionTop(plankTop, w);
            float size = Math.Max(1f, Math.Min(Design.BottomNav.MedallionShare * w, (height - top) / (0.5f + (Design.BottomNav.DiscShare / 2f))));
            var medallion = new Box(boxes[activeIndex].CenterX - (size / 2f), top, boxes[activeIndex].CenterX + (size / 2f), top + size);
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            return new BottomNavRegions(safe, w, bar, plank, shown, boxes, active, medallion, touch);
        }

        /// <summary>
        /// The bottom menu's top (<see cref="BottomNavRegions.Top"/>, the same whatever its places): Home, the Store page
        /// and the Wardrobe keep their content above it.
        /// </summary>
        public static float BottomNavTop(float width, float height, Insets insets)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float plankTop = safe.Bottom - (Design.BottomNav.PlankShare * w);
            return Math.Min(plankTop, MedallionTop(plankTop, w));
        }

        private static float MedallionTop(float plankTop, float w) => plankTop - (Design.BottomNav.RiseShare * w);
    }
}
