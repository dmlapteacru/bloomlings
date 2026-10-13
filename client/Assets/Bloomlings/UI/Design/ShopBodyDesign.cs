using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.UI.Design
{
    public enum ShopVisualKind { Card, Ribbon, Button, Sprite, Text, Well, Divider }

    /// <summary>One layer of the store body. Unity and the device preview consume the same layout.</summary>
    public sealed record ShopVisual(ShopVisualKind Kind, Box Box, string Value = "", string Id = "", TypeStyle? Type = null, bool Dark = false);

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
        public static readonly ColorSet Well = new ColorSet("shop.well", Rgba.FromHex("#FFDB98"), Rgba.FromHex("#FFEBC2"), Rgba.FromHex("#E9B86E"), Rgba.FromHex("#E9C38D"));
        public static readonly TypeStyle Title = new TypeStyle("type.shop.title", 56, true, false, 0, 20);
        public static readonly TypeStyle Amount = new TypeStyle("type.shop.amount", 68, true, false, 0, 24);
        public static readonly TypeStyle Price = new TypeStyle("type.shop.price", 58, true, false, 0, 24);
        public static readonly TypeStyle Count = new TypeStyle("type.shop.count", 46, true, false, 0, 24);
        public static readonly TypeStyle Small = new TypeStyle("type.shop.small", 36, true, false, 0, 16);
        public static TextLook Letters => new TextLook(Rgba.White, Cream, Ink, 0.055f, 0.035f, 0.12f);
        public static TextLook DarkLetters => TextLook.Plain(Ink);

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
        public static Box ButtonLabel(Box b, bool petals) => b.Inset(b.Width * 0.05f, b.Height * (petals ? 0.12f : 0.20f));
        public static (int Column, int Row)? BoosterCell(string name) => name switch
        {
            "boost-extra_slot" => (0, 0),
            "boost-shuffle" => (1, 0),
            "boost-return" => (0, 1),
            "boost-bloom_burst" => (1, 1),
            _ => null,
        };
        public static string ResourceName(string name) => name.StartsWith("boost-", StringComparison.Ordinal) ? "booster-atlas-v2" : name == "background" || name == "price-button" || name == "hero-card-v2" || name == "noads-basket-v2" ? name : "offer-atlas";
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
            Section(nodes, Next(0.06f), text("store.shop_petals"));
            float cw = (w - gap * 2) / 3;
            for (int row = 0; row < 2; row++)
            {
                Box coins = Next(0.36f);
                for (int i = 0; i < 3; i++) Currency(nodes, new Box(coins.Left + i * (cw + gap), coins.Top, coins.Left + i * (cw + gap) + cw, coins.Bottom), Offers[6 + row * 3 + i], visible);
            }
            Section(nodes, Next(0.06f), text("store.shop_boosters"));
            Boosters(nodes, Next(0.32f), boosterPrice, unlocked);
            Section(nodes, Next(0.06f), text("store.shop_bundles"));
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
            if (o.BestSale) Art(n, b, "hero-card-v2");
            else
            {
                n.Add(new ShopVisual(ShopVisualKind.Card, b));
                Art(n, In(b, 0.01f, -0.02f, 0.55f, 0.80f), o.Art);
                n.Add(new ShopVisual(ShopVisualKind.Ribbon, In(b, 0, 0.76f, 1, 1)));
            }
            Label(n, In(b, 0.17f, 0.57f, 0.51f, 0.77f), NumberText.Group(o.Petals), Amount);
            RewardGrid(n, In(b, 0.56f, 0.065f, 0.94f, 0.72f), o.Boosters!);
            Label(n, In(b, 0.045f, 0.805f, 0.63f, 0.94f), text(o.NameKey), Title);
            Buy(n, In(b, 0.67f, 0.785f, 0.94f, 0.965f), o.Price, o.Id);
            if (o.BestSale)
            {
                Art(n, In(b, 0.00f, 0.00f, 0.22f, 0.3f), "badge");
                Label(n, In(b, 0.015f, 0.035f, 0.18f, 0.145f), "BEST", Small, true);
                Label(n, In(b, 0.015f, 0.125f, 0.18f, 0.235f), "SALE", Small, true);
            }
        }
        private static void SmallOffer(List<ShopVisual> n, Box b, ShopOffer o, Func<string, string> text, Func<string, bool> visible)
        {
            if (!visible(o.Id)) return;
            n.Add(new ShopVisual(ShopVisualKind.Card, b));
            if (o.Boosters == null)
            {
                Art(n, In(b, -0.02f, -0.015f, 1.02f, 0.74f), o.Art);
            }
            else
            {
                Art(n, In(b, -0.01f, 0.00f, 0.60f, 0.72f), o.Art);
                Label(n, In(b, 0.025f, 0.535f, 0.59f, 0.69f), NumberText.Group(o.Petals), Amount);
                RewardGrid(n, In(b, 0.57f, 0.045f, 0.985f, 0.70f), o.Boosters);
            }
            Label(n, In(b, 0.02f, 0.70f, 0.98f, 0.80f), text(o.NameKey), Count, true);
            Buy(n, In(b, 0.20f, 0.805f, 0.80f, 0.975f), o.Price, o.Id);
        }
        private static void Currency(List<ShopVisual> n, Box b, ShopOffer o, Func<string, bool> visible)
        {
            if (!visible(o.Id)) return;
            n.Add(new ShopVisual(ShopVisualKind.Card, b));
            Art(n, In(b, -0.02f, -0.01f, 1.02f, 0.67f), o.Art);
            if (o.ValuePercent > 0) Label(n, In(b, 0.62f, 0.015f, 0.96f, 0.125f), "+" + o.ValuePercent + "%", Small);
            Label(n, In(b, 0.025f, 0.565f, 0.975f, 0.735f), NumberText.Group(o.Petals), Amount);
            Buy(n, In(b, 0.14f, 0.74f, 0.86f, 0.965f), o.Price, o.Id);
        }
        private static void Section(List<ShopVisual> n, Box b, string title)
        {
            Label(n, In(b, 0.005f, 0, 0.36f, 1), title, Title);
            n.Add(new ShopVisual(ShopVisualKind.Divider, In(b, 0.4f, 0.46f, 0.995f, 0.62f)));
        }
        private static void RewardGrid(List<ShopVisual> n, Box b, BoosterGrant g)
        {
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] counts = { g.ExtraSlot, g.Shuffle, g.Return, g.BloomBurst };
            for (int i = 0; i < 4; i++)
            {
                Box cell = In(b, (i % 2) * 0.5f, (i / 2) * 0.5f, (i % 2) * 0.5f + 0.47f, (i / 2) * 0.5f + 0.47f);
                n.Add(new ShopVisual(ShopVisualKind.Sprite, In(cell, 0.00f, -0.01f, 1.00f, 0.80f), "shop/boost-" + ids[i]));
                Label(n, In(cell, 0.05f, 0.70f, 0.95f, 0.99f), "×" + counts[i], Count);
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
                n.Add(new ShopVisual(ShopVisualKind.Card, cell));
                n.Add(new ShopVisual(ShopVisualKind.Sprite, In(cell, -0.02f, -0.005f, 1.02f, 0.64f), "shop/boost-" + ids[i]));
                Label(n, In(cell, 0.10f, 0.575f, 0.90f, 0.735f), "×" + (i == 3 ? 1 : 3), Count);
                n.Add(new ShopVisual(ShopVisualKind.Button, In(cell, 0.05f, 0.755f, 0.95f, 0.975f), NumberText.Group(price(kind)), "booster_" + kind, Count, Dark: true));
            }
        }

        public static byte[] Surface(ShopVisualKind kind, int w, int h) => kind switch
        {
            ShopVisualKind.Button => UiRaster.ButtonFace(w, h, Green, 0.38f, true),
            ShopVisualKind.Ribbon => UiRaster.RaisedRow(w, h, Math.Min(w, h) * 0.42f, Coral),
            ShopVisualKind.Well => UiRaster.RaisedRow(w, h, Math.Min(w, h) * 0.24f, Well),
            _ => UiRaster.RaisedRow(w, h, Math.Min(w, h) * 0.16f, Card),
        };
    }
}
