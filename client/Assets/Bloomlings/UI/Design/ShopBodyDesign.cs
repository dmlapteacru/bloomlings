using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.UI.Design
{
    public enum ShopVisualKind { Card, Ribbon, Button, Sprite, Text, Well, RewardBox }
    public enum ShopGroup { Bundles, Petals, Boosters, NoAds }

    /// <summary>One layer of the store body. Unity and the device preview consume the same layout.</summary>
    public sealed record ShopVisual(ShopVisualKind Kind, Box Box, string Value = "", string Id = "", TypeStyle? Type = null, bool Dark = false, ShopGroup Group = ShopGroup.Bundles, TextLook? Look = null, float Angle = 0, Rgba? Tint = null);

    public sealed record ShopOffer(string Id, string NameKey, int Petals, string Price, string Art, BoosterGrant? Boosters = null, bool BestSale = false, int ValuePercent = 0);

    /// <summary>Presentation of the approved offers; prices remain previews until platform products are connected.</summary>
    public static class ShopBodyDesign
    {
        public const float ReferenceWidth = 1080f;
        public static readonly Rgba Ink = Rgba.FromHex("#542713");
        public static readonly Rgba Cream = Rgba.FromHex("#FFF4D7");
        public static readonly ColorSet Card = new ColorSet("shop.card", Rgba.FromHex("#FFE6AF"), Rgba.FromHex("#FFF6DD"), Rgba.FromHex("#C78B48"), Rgba.FromHex("#DEAF72"));
        public static readonly ColorSet Green = new ColorSet("shop.price", Rgba.FromHex("#70D517"), Rgba.FromHex("#B2FA3A"), Rgba.FromHex("#347917"), Rgba.FromHex("#4A851E"));
        public static readonly ColorSet Coral = new ColorSet("shop.ribbon", Rgba.FromHex("#F67957"), Rgba.FromHex("#FFAC7F"), Rgba.FromHex("#B9472D"), Rgba.FromHex("#D66345"));
        public static readonly ColorSet Well = new ColorSet("shop.well", Rgba.FromHex("#FFE2A3"), Rgba.FromHex("#FFF9DD"), Rgba.FromHex("#CC9349"), Rgba.FromHex("#E9C38D"));
        public static readonly TypeStyle Title = new TypeStyle("type.shop.title", 56, true, false, 0, 20);
        public static readonly TypeStyle Amount = new TypeStyle("type.shop.amount", 68, true, false, 0, 24);
        public static readonly TypeStyle Price = new TypeStyle("type.shop.price", 58, true, false, 0, 24);
        public static readonly TypeStyle Count = new TypeStyle("type.shop.count", 46, true, false, 0, 24);
        public static readonly TypeStyle Small = new TypeStyle("type.shop.small", 36, true, false, 0, 16);
        public static readonly TypeStyle Badge = new TypeStyle("type.shop.badge", 32, true, false, 0, 20);
        public static TextLook Letters => new TextLook(Rgba.White, Cream, Ink, 0.075f, 0.035f, 0.12f);
        public static TextLook DarkLetters => TextLook.Plain(Ink);
        public static readonly Rgba MoneyInk = Rgba.FromHex("#205B0D");
        public static readonly Rgba PetalPriceInk = Rgba.FromHex("#77401E");
        public static readonly Rgba BadgeLight = Rgba.FromHex("#FFFAB6");
        public static TextLook MoneyLetters => new TextLook(Rgba.White, Cream, MoneyInk, 0.085f, 0.02f, 0.1f);
        public static TextLook PetalLetters => new TextLook(PetalPriceInk, PetalPriceInk, PetalPriceInk, 0, 0, 0);
        public static TextLook BadgeLetters => new TextLook(Ink, Ink, Ink, 0.027f, 0, 0);
        public static TextLook LookFor(ShopVisual n) => n.Look ?? (n.Kind == ShopVisualKind.Button ? (n.Dark ? PetalLetters : MoneyLetters) : n.Dark ? DarkLetters : Letters);
        public static readonly ColorSet PetalsBorder = new ColorSet("shop.border.petals", Rgba.FromHex("#FFD250"), Rgba.FromHex("#FFF8CC"), Rgba.FromHex("#EAA32D"), Rgba.FromHex("#D89225"));
        public static readonly ColorSet BoostersBorder = new ColorSet("shop.border.boosters", Rgba.FromHex("#71C5F1"), Rgba.FromHex("#DBF7FF"), Rgba.FromHex("#308ECA"), Rgba.FromHex("#257BA9"));
        public static readonly ColorSet BundlesBorder = new ColorSet("shop.border.bundles", Rgba.FromHex("#E8A0D8"), Rgba.FromHex("#FFE4F4"), Rgba.FromHex("#B76AB0"), Rgba.FromHex("#955093"));
        public static readonly ColorSet NoAdsBorder = new ColorSet("shop.border.noads", Rgba.FromHex("#8AD9B4"), Rgba.FromHex("#E4FFDF"), Rgba.FromHex("#42A579"), Rgba.FromHex("#338861"));
        public static readonly ColorSet InsetBorder = new ColorSet("shop.border.inset", Rgba.FromHex("#EDC588"), Rgba.FromHex("#FFF5DA"), Rgba.FromHex("#D7A363"), Rgba.FromHex("#C28C4C"));
        public static ColorSet Border(ShopGroup group) => group switch { ShopGroup.Petals => PetalsBorder, ShopGroup.Boosters => BoostersBorder, ShopGroup.NoAds => NoAdsBorder, _ => BundlesBorder };
        public static Box FeaturedArt(Box b)
        {
            // Actual PNG dimensions: all overlays use its aspect-fitted rectangle, never the viewport's height.
            float scale = Math.Min(b.Width / 1704f, b.Height / 923f);
            return Box.FromCenter(b.CenterX, b.CenterY, 1704f * scale, 923f * scale);
        }
        public static Box FeaturedRewards(Box art) => In(art, 0.552f, 0.135f, 0.913f, 0.672f);
        public static Box FeaturedInset(Box art) => In(art, 0.526f, 0.104f, 0.929f, 0.708f);
        public static Box FeaturedFooter(Box art) => In(art, 0.03f, 0.73f, 0.98f, 0.958f);

        public static readonly IReadOnlyList<ShopOffer> Offers = new[]
        {
            new ShopOffer("garden_small", "store.garden_bundle", 5000, "$9.99", "hero", new BoosterGrant(3,3,3,1), true),
            new ShopOffer("remove_ads", "store.remove_ads", 0, "$5.99", "noads"),
            new ShopOffer("remove_ads_bundle", "store.shop_no_ads_pack", 1200, "$9.99", "noads-basket-v2", new BoosterGrant(1,1,1,1)),
            new ShopOffer("starter_garden", "store.starter_pack", 2000, "$4.99", "pouch", new BoosterGrant(1,1,1,1)),
            new ShopOffer("garden_medium", "store.garden_bundle", 11000, "$19.99", "basket", new BoosterGrant(6,6,6,2)),
            new ShopOffer("garden_large", "store.garden_bundle", 25000, "$39.99", "chest", new BoosterGrant(12,12,12,3)),
            new ShopOffer("petals_1200", "store.preview_petals_1200", 1200, "$1.99", "pouch"),
            new ShopOffer("petals_5500", "store.preview_petals_5500", 5500, "$7.99", "basket",ValuePercent:14),
            new ShopOffer("petals_12000", "store.preview_petals_12000", 12000, "$14.99", "chest",ValuePercent:16),
            new ShopOffer("petals_27500", "store.preview_petals_27500", 27500, "$29.99", "pouch",ValuePercent:14),
            new ShopOffer("petals_58000", "store.preview_petals_58000", 58000, "$54.99", "basket",ValuePercent:15),
            new ShopOffer("petals_120000", "store.preview_petals_120000", 120000, "$99.99", "chest",ValuePercent:13),
        };

        public static bool IsShopFont(TypeStyle style) => style.Name.StartsWith("type.shop.", StringComparison.Ordinal);
        public static Box Bounds(ReferenceStoreRegions r) => new Box(r.Safe.Left + r.W * 0.025f, r.Tabs.Bottom + r.W * 0.015f, r.Safe.Right - r.W * 0.025f, r.NavTop - r.W * 0.015f);
        public static float ContentHeight(IReadOnlyList<ShopVisual> nodes, Box viewport)
        {
            float bottom = viewport.Top;
            foreach (ShopVisual n in nodes) bottom = Math.Max(bottom, n.Box.Bottom);
            return Math.Max(viewport.Height, bottom - viewport.Top + viewport.Width * 0.035f);
        }
        public static Box ButtonLabel(Box b, bool petals) => In(b, petals ? 0.34f : 0.05f, petals ? 0.12f : 0.20f, 0.95f, petals ? 0.88f : 0.80f);
        public static Box ButtonPetal(Box b)
        {
            float side = Math.Min(b.Width * 0.26f, b.Height * 0.90f);
            // Center the currency in the gap between the inner left rim and the price-label region.
            return Box.FromCenter(b.Left + b.Width * (0.055f + 0.34f) * 0.5f, b.CenterY, side, side);
        }
        public static (int Column, int Row)? BoosterCell(string name) => name switch
        {
            "boost-extra_slot" => (0, 0),
            "boost-shuffle" => (1, 0),
            "boost-return" => (0, 1),
            "boost-bloom_burst" => (1, 1),
            _ => null,
        };
        public static string ResourceName(string name) => name.StartsWith("accessory-", StringComparison.Ordinal) ? "catalog-accessories-v1" : name.StartsWith("catalog-", StringComparison.Ordinal) ? "catalog-frames-v1" : name.StartsWith("boost-", StringComparison.Ordinal) ? "booster-atlas-v2" : name == "background" || name == "price-button" || name == "hero-card-v2" || name == "noads-basket-v2" ? name : "offer-atlas";
        public static (int Left, int Top, int Width, int Height) BoosterCrop(string name, int width, int height)
        {
            // Opaque subject bounds (alpha >= 160) plus 3 pixels; all icons center their artwork, not the atlas cell.
            Box crop = name switch
            {
                "boost-extra_slot" => new Box(46, 51, 592, 593),
                "boost-shuffle" => new Box(33, 52, 583, 597),
                "boost-return" => new Box(47, 29, 583, 560),
                "boost-bloom_burst" => new Box(12, 25, 585, 550),
                _ => new Box(0, 0, 627, 627),
            };
            return ((int)(crop.Left * width / 627), (int)(crop.Top * height / 627), (int)(crop.Width * width / 627), (int)(crop.Height * height / 627));
        }
        public static (int Column, int Row)? AtlasCell(string name) => name switch
        {
            "hero" => (0, 0),
            "noads" => (1, 0),
            "pouch" => (2, 0),
            "basket" => (0, 1),
            "chest" => (1, 1),
            "badge" => (2, 1),
            _ => null,
        };
        public static int AtlasLeftPadding(string name) => name == "badge" ? 40 : name == "pouch" ? 24 : 0;
        public static (int Left, int Top, int Width, int Height) AtlasCrop(string name, int width, int height)
        {
            // Measured opaque artwork bounds plus a tiny gutter: align subjects, not their different transparent margins.
            Box crop = name switch
            {
                "pouch" => new Box(31, 106, 497, 508),
                "basket" => new Box(14, 42, 512, 452),
                "chest" => new Box(24, 32, 512, 452),
                _ => new Box(AtlasLeftPadding(name), 0, 512, 512),
            };
            return ((int)(crop.Left * width / 512), (int)(crop.Top * height / 512), (int)(crop.Width * width / 512), (int)(crop.Height * height / 512));
        }

        public static List<ShopVisual> Build(Box area, Func<string, string> text, Func<string, bool> visible, Func<BoosterKind, int> boosterPrice, Func<BoosterKind, bool> unlocked)
        {
            var nodes = new List<ShopVisual>();
            float w = area.Width;
            float gap = w * 0.02f;
            float y = area.Top + w * 0.02f;
            Box Next(float share) { var b = new Box(area.Left, y, area.Right, y + w * share); y = b.Bottom + gap; return b; }
            Hero(nodes, Next(0.55f), Offers[0], text, visible);
            if (visible("remove_ads") || visible("remove_ads_bundle"))
            {
                Box pair = Next(0.47f);
                float split = pair.Left + w * 0.42f;
                SmallOffer(nodes, new Box(pair.Left, pair.Top, split - gap * 0.5f, pair.Bottom), Offers[1], text, visible);
                SmallOffer(nodes, new Box(split + gap * 0.5f, pair.Top, pair.Right, pair.Bottom), Offers[2], text, visible);
            }
            float cw = (w - gap * 2) / 3;
            for (int row = 0; row < 2; row++)
            {
                Box coins = Next(0.36f);
                for (int i = 0; i < 3; i++) Currency(nodes, new Box(coins.Left + i * (cw + gap), coins.Top, coins.Left + i * (cw + gap) + cw, coins.Bottom), Offers[6 + row * 3 + i], visible);
            }
            Boosters(nodes, Next(0.255f), boosterPrice, unlocked);
            Box packs = Next(0.47f);
            SmallOffer(nodes, new Box(packs.Left, packs.Top, packs.CenterX - gap * 0.5f, packs.Bottom), Offers[3], text, visible);
            SmallOffer(nodes, new Box(packs.CenterX + gap * 0.5f, packs.Top, packs.Right, packs.Bottom), Offers[4], text, visible);
            Hero(nodes, Next(0.52f), Offers[5], text, visible);
            return nodes;
        }

        private static Box In(Box b, float l, float t, float r, float bottom) => new Box(b.Left + b.Width * l, b.Top + b.Height * t, b.Left + b.Width * r, b.Top + b.Height * bottom);
        private static void Label(List<ShopVisual> n, Box b, string text, TypeStyle type, bool dark = false) => n.Add(new ShopVisual(ShopVisualKind.Text, b, text, Type: type, Dark: dark));
        private static void Art(List<ShopVisual> n, Box b, string art) => n.Add(new ShopVisual(ShopVisualKind.Sprite, b, "shop/" + art));
        private static void Buy(List<ShopVisual> n, Box b, string price, string id) => n.Add(new ShopVisual(ShopVisualKind.Button, b, price, id, Price));
        private static void Hero(List<ShopVisual> n, Box b, ShopOffer o, Func<string, string> text, Func<string, bool> visible)
        {
            if (!visible(o.Id)) return;
            if (o.BestSale)
            {
                b = FeaturedArt(b);
                Art(n, b, "hero-card-v2");
            }
            else
            {
                n.Add(new ShopVisual(ShopVisualKind.Card, b));
                Art(n, In(b, 0.06f, 0.04f, 0.51f, 0.735f), o.Art);
                n.Add(new ShopVisual(ShopVisualKind.Ribbon, In(b, 0.015f, 0.76f, 0.985f, 0.97f)));
            }
            Label(n, In(b, 0.17f, 0.565f, 0.51f, 0.72f), NumberText.Group(o.Petals), Amount);
            if (o.BestSale) RewardGrid(n, FeaturedRewards(b), o.Boosters!);
            else RewardPanel(n, In(b, 0.55f, 0.075f, 0.91f, 0.69f), o.Boosters!);
            Label(n, In(b, 0.065f, 0.76f, 0.63f, 0.907f), text(o.NameKey), Title);
            Buy(n, In(b, 0.665f, 0.755f, 0.932f, 0.918f), o.Price, o.Id);
            if (o.BestSale)
            {
                Box badge = In(b, 0.005f, 0.005f, 0.23f, 0.335f);
                n.Add(new ShopVisual(ShopVisualKind.Sprite, badge.Scale(1.14f, 1.14f), "shop/badge", Tint: BadgeLight.WithAlpha(0.16f)));
                n.Add(new ShopVisual(ShopVisualKind.Sprite, badge.Scale(1.075f, 1.075f), "shop/badge", Tint: BadgeLight.WithAlpha(0.45f)));
                n.Add(new ShopVisual(ShopVisualKind.Sprite, badge.Scale(1.025f, 1.025f), "shop/badge", Tint: BadgeLight));
                Art(n, badge, "badge");
                // Both lines share one tilted center axis, rather than rotating around two unshifted centers.
                float angle = -18f, radians = angle * (float)Math.PI / 180f;
                for (int row = 0; row < 2; row++)
                {
                    float offset = (row - 0.5f) * b.Height * 0.065f;
                    Box line = Box.FromCenter(b.Left + b.Width * 0.105f - offset * (float)Math.Sin(radians), b.Top + b.Height * 0.1525f + offset * (float)Math.Cos(radians), b.Width * 0.13f, b.Height * 0.07f);
                    n.Add(new ShopVisual(ShopVisualKind.Text, line, row == 0 ? "BEST" : "SALE", Type: Badge, Look: BadgeLetters, Angle: angle));
                }
            }
        }
        private static void SmallOffer(List<ShopVisual> n, Box b, ShopOffer o, Func<string, string> text, Func<string, bool> visible)
        {
            if (!visible(o.Id)) return;
            n.Add(new ShopVisual(ShopVisualKind.Card, b, Group: o.Id.StartsWith("remove_ads", StringComparison.Ordinal) ? ShopGroup.NoAds : ShopGroup.Bundles));
            if (o.Boosters == null)
            {
                Art(n, In(b, 0.055f, 0.02f, 0.945f, 0.705f), o.Art);
            }
            else
            {
                Art(n, In(b, 0.06f, 0.035f, 0.53f, 0.69f), o.Art);
                Label(n, In(b, 0.06f, 0.535f, 0.53f, 0.69f), NumberText.Group(o.Petals), Amount);
                float right = o.Id == "remove_ads_bundle" ? 0.945f : 0.91f;
                RewardPanel(n, In(b, 0.57f, 0.075f, right, 0.66f), o.Boosters);
            }
            Label(n, In(b, 0.02f, 0.675f, 0.98f, 0.76f), text(o.NameKey), Count, true);
            Buy(n, In(b, 0.20f, 0.775f, 0.80f, 0.945f), o.Price, o.Id);
        }
        private static void Currency(List<ShopVisual> n, Box b, ShopOffer o, Func<string, bool> visible)
        {
            if (!visible(o.Id)) return;
            n.Add(new ShopVisual(ShopVisualKind.Card, b, Group: ShopGroup.Petals));
            Art(n, In(b, 0.04f, 0.045f, 0.96f, 0.66f), o.Art);
            if (o.ValuePercent > 0) Label(n, In(b, 0.62f, 0.015f, 0.96f, 0.125f), "+" + o.ValuePercent + "%", Small);
            Label(n, In(b, 0.025f, 0.565f, 0.975f, 0.735f), NumberText.Group(o.Petals), Amount);
            Buy(n, In(b, 0.14f, 0.74f, 0.86f, 0.965f), o.Price, o.Id);
        }
        private static void RewardPanel(List<ShopVisual> n, Box b, BoosterGrant g)
        {
            // Fit the panel to its square icons and overlapping counts, with equal padding in physical pixels.
            float padding = b.Width * 0.065f, gap = b.Width * 0.07f;
            float side = (b.Width - padding * 2 - gap) * 0.5f, countHeight = side * 0.44f;
            float rowHeight = side + countHeight * 0.58f;
            b = Box.FromCenter(b.CenterX, b.CenterY, b.Width, rowHeight * 2 + gap + padding * 2);
            n.Add(new ShopVisual(ShopVisualKind.RewardBox, b));
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] counts = { g.ExtraSlot, g.Shuffle, g.Return, g.BloomBurst };
            for (int i = 0; i < 4; i++)
            {
                float left = b.Left + padding + (i % 2) * (side + gap);
                float top = b.Top + padding + (i / 2) * (rowHeight + gap);
                Box icon = new Box(left, top, left + side, top + side);
                n.Add(new ShopVisual(ShopVisualKind.Sprite, icon, "shop/boost-" + ids[i]));
                Box count = Box.FromCenter(icon.CenterX, icon.Bottom + countHeight * 0.08f, side * 0.9f, countHeight);
                Label(n, count, "×" + counts[i], Count);
            }
        }
        private static void RewardGrid(List<ShopVisual> n, Box b, BoosterGrant g)
        {
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] counts = { g.ExtraSlot, g.Shuffle, g.Return, g.BloomBurst };
            for (int i = 0; i < 4; i++)
            {
                Box cell = In(b, (i % 2) * 0.5f, (i / 2) * 0.5f, (i % 2) * 0.5f + 0.47f, (i / 2) * 0.5f + 0.47f);
                Box icon = In(cell, 0, -0.01f, 1, 0.80f);
                n.Add(new ShopVisual(ShopVisualKind.Sprite, icon, "shop/boost-" + ids[i]));
                float side = Math.Min(icon.Width, icon.Height), countHeight = cell.Height * 0.28f;
                float iconBottom = icon.CenterY + side * 0.5f;
                // Anchor to the fitted square icon, rather than the taller reward-grid cell.
                Box count = Box.FromCenter(cell.CenterX, iconBottom - side * 0.10f + countHeight * 0.32f, cell.Width * 0.9f, countHeight);
                Label(n, count, "×" + counts[i], Count);
            }
        }
        private static void Boosters(List<ShopVisual> n, Box b, Func<BoosterKind, int> price, Func<BoosterKind, bool> unlocked)
        {
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            for (int i = 0; i < 4; i++)
            {
                BoosterKind kind = (BoosterKind)i;
                if (!unlocked(kind)) continue;
                Box cell = In(b, i * 0.25f, 0, i * 0.25f + 0.235f, 1);
                n.Add(new ShopVisual(ShopVisualKind.Card, cell, Group: ShopGroup.Boosters));
                n.Add(new ShopVisual(ShopVisualKind.Sprite, In(cell, 0.20f, 0.03f, 0.80f, 0.57f), "shop/boost-" + ids[i]));
                Label(n, In(cell, 0.20f, 0.47f, 0.80f, 0.67f), "×" + (i == 3 ? 1 : 3), Count);
                n.Add(new ShopVisual(ShopVisualKind.Button, In(cell, 0.07f, 0.675f, 0.93f, 0.925f), NumberText.Group(price(kind)), "booster_" + kind, Count, Dark: true));
            }
        }

        public static byte[] Surface(ShopVisualKind kind, int w, int h, ShopGroup group = ShopGroup.Bundles) => kind switch
        {
            ShopVisualKind.Button => UiRaster.ButtonFace(w, h, Green, 0.38f, true),
            ShopVisualKind.Ribbon => UiRaster.RaisedRow(w, h, Math.Min(w, h) * 0.42f, Coral),
            ShopVisualKind.Well => ShopBodyRaster.Framed(w, h, 0.48f, InsetBorder, Well, 0.08f, true, 0.18f),
            ShopVisualKind.RewardBox => ShopBodyRaster.Framed(w, h, 0.18f, InsetBorder, Card, 0.03f, true),
            _ => ShopBodyRaster.Framed(w, h, group == ShopGroup.Boosters ? 0.20f : 0.16f, Border(group), Card, 0.038f),
        };
    }
}
