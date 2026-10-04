using System;
using System.Collections.Generic;
using System.Globalization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>The places of the bottom menu (spec 005 FR-030, contracts/look.md §6.7), in their order from the left.</summary>
    public enum NavPlace
    {
        /// <summary>The Store page (from L12).</summary>
        Shop,

        /// <summary>The Wardrobe page (from L40).</summary>
        Wardrobe,

        /// <summary>Home, always.</summary>
        Home,

        /// <summary>The Leaderboard card over Home (from L10).</summary>
        Leaderboard,

        /// <summary>The Collection card over Home (once a picture is won).</summary>
        Collection,
    }

    /// <summary>
    /// The bottom menu's rules (spec 005 FR-030, contracts/look.md §6.7; the owner's request of 2026-10-04, the wooden
    /// variant): which places show, in which order, their names, slots and stand-in glyphs, both builds. A place shows only
    /// once its feature is unlocked (a player never sees the button of a locked feature): the Shop with
    /// <see cref="HomeLook.Store"/>, the Wardrobe with <see cref="HomeLook.Wardrobe"/>, the Leaderboard with
    /// <see cref="HomeLook.Rank"/>, the Collection with <see cref="HomeLook.Collection"/>; Home always. Engine-free.
    /// </summary>
    public static class BottomNav
    {
        /// <summary>The plank's height, as a share of W.</summary>
        public const float PlankShare = 0.12f;

        /// <summary>How far the vines at the plank's ends reach above its top, as a share of W (the bar picture's top).</summary>
        public const float DecorShare = 0.045f;

        /// <summary>The raised medallion's box, as a share of W, and how far it rises above the plank's top.</summary>
        public const float MedallionShare = 0.19f;

        public const float RiseShare = 0.05f;

        /// <summary>The wooden disc's diameter, as a share of the medallion's box (its leaves and flowers take the rest).</summary>
        public const float DiscShare = 0.88f;

        /// <summary>The plank's ends, as shares of W from the safe area's sides (the vines curl around them).</summary>
        public const float EndShare = 0.03f;

        /// <summary>Where the places' span starts and ends, as shares of W (the places share it evenly).</summary>
        public const float SpanStart = 0.12f;

        public const float SpanEnd = 0.88f;

        /// <summary>A place's icon on the plank, as a share of the plank's height (the owner's icons keep a small margin).</summary>
        public const float IconShare = 0.86f;

        /// <summary>The active place's icon in the medallion, as a share of the disc's diameter.</summary>
        public const float MedallionIconShare = 0.7f;

        /// <summary>The grooves between the places: their length as a share of the plank's height.</summary>
        public const float GrooveShare = 0.5f;

        /// <summary>The room a screen keeps between its lowest content and the menu's top, as a share of W.</summary>
        public const float GapShare = 0.015f;

        /// <summary>The places in their order from the left.</summary>
        public static IReadOnlyList<NavPlace> Order { get; } = new[] { NavPlace.Shop, NavPlace.Wardrobe, NavPlace.Home, NavPlace.Leaderboard, NavPlace.Collection };

        /// <summary>Whether <paramref name="place"/> shows for a player whose Home looks like <paramref name="look"/>.</summary>
        public static bool Shows(NavPlace place, HomeLook look) => place switch
        {
            NavPlace.Shop => look.Store,
            NavPlace.Wardrobe => look.Wardrobe,
            NavPlace.Leaderboard => look.Rank,
            NavPlace.Collection => look.Collection,
            _ => true,
        };

        /// <summary>The places shown for <paramref name="look"/>, in their order (Home alone early on).</summary>
        public static IReadOnlyList<NavPlace> Places(HomeLook look)
        {
            var places = new List<NavPlace>();
            foreach (NavPlace place in Order)
            {
                if (Shows(place, look))
                {
                    places.Add(place);
                }
            }

            return places;
        }

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
    /// of the height; the vines reach above it), the bottom of its band in the safe area (<see cref="BandBottom"/>; the wood
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
    /// <item><description><see cref="Bar"/>: the box of the bar's wooden picture, the whole screen's width from the vines'
    /// reach over the plank to the screen's bottom (the wood runs on behind the bottom inset).</description></item>
    /// <item><description><see cref="Plank"/>: the plank's band, <see cref="BottomNav.PlankShare"/> of W tall, its bottom on
    /// the safe area's bottom.</description></item>
    /// <item><description><see cref="PlaceBoxes"/>: the shown places' columns on the band, sharing the span from
    /// <see cref="BottomNav.SpanStart"/> to <see cref="BottomNav.SpanEnd"/> of W evenly, in order.</description></item>
    /// <item><description><see cref="Medallion"/>: the raised round medallion over the active place, rising
    /// <see cref="BottomNav.RiseShare"/> of W above the plank (a little smaller on a screen without a bottom inset, so it
    /// stays on the screen).</description></item>
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
        /// column's middle, a little above the band's middle; the active place's in the medallion's disc
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
            float side = Plank.Height * BottomNav.IconShare;
            return Box.FromCenter(column.CenterX, Plank.CenterY - (Plank.Height * 0.03f), side, side);
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
        /// The bottom menu (contracts/look.md §6.7) for the shown <paramref name="places"/> (<see cref="BottomNav.Places"/>;
        /// the <paramref name="active"/> one is added in its order when missing), in fractions of the safe width W: the
        /// plank 0.12 W tall from 0.03 W to 0.97 W, its bottom on the safe bottom, the wood running on to the screen's
        /// bottom; the bar's picture from 0.045 W above the plank (its vines) to the screen's bottom, across the screen; the
        /// places sharing 0.12 W to 0.88 W evenly; the medallion 0.19 W square on the active place, its top 0.05 W above the
        /// plank's (smaller when its bottom would leave the screen: on a phone without a bottom inset it ends on the
        /// screen's bottom, 0.17 W).
        /// </summary>
        public static BottomNavRegions BottomNav(float width, float height, Insets insets, IReadOnlyList<NavPlace> places, NavPlace active)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float plankTop = safe.Bottom - (Design.BottomNav.PlankShare * w);
            var plank = new Box(safe.Left + (Design.BottomNav.EndShare * w), plankTop, safe.Right - (Design.BottomNav.EndShare * w), safe.Bottom);
            var bar = new Box(0f, plankTop - (Design.BottomNav.DecorShare * w), width, height);

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
            float size = Math.Max(1f, Math.Min(Design.BottomNav.MedallionShare * w, height - top));
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
            return Math.Min(plankTop - (Design.BottomNav.DecorShare * w), MedallionTop(plankTop, w));
        }

        private static float MedallionTop(float plankTop, float w) => plankTop - (Design.BottomNav.RiseShare * w);
    }
}
