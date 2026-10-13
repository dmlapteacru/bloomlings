using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The same real layers as the Unity store; header and navigation are drawn by the existing screen.</summary>
    public static class ShopBodyPainter
    {
        public static void Background(IPainter p)
        {
            p.Mark("bg.shop");
            var size = p.SpriteSize("shop/background");
            if (!size.HasValue) return;
            float scale = Math.Max(p.Width / size.Value.Width, p.Height / size.Value.Height);
            p.Sprite("shop/background", Box.FromCenter(p.Width * 0.5f, p.Height * 0.5f, size.Value.Width * scale, size.Value.Height * scale));
        }
        public static void Draw(IPainter p, DesignApp app, ReferenceStoreRegions regions)
        {
            Box area = ShopBodyDesign.Bounds(regions);
            bool Visible(string id) => !(app.Meta.Save.Purchases.RemoveAds && (id == "remove_ads" || id == "remove_ads_bundle"));
            List<ShopVisual> nodes = ShopBodyDesign.Build(area, PlaytestText.T, Visible, app.Meta.Economy.Price, app.Meta.Economy.IsUnlocked);
            float max = Math.Max(0, ShopBodyDesign.ContentHeight(nodes, area) - area.Height);
            app.ShopScrollOffset = Math.Max(0, Math.Min(max, app.ShopScrollOffset));
            p.PushClip(area);
            foreach (ShopVisual source in nodes)
            {
                ShopVisual n = source with { Box = source.Box.Offset(0, -app.ShopScrollOffset) };
                if (n.Box.Bottom <= area.Top || n.Box.Top >= area.Bottom) continue;
                if (n.Kind == ShopVisualKind.Sprite) { if (n.Value.StartsWith("shop/", StringComparison.Ordinal)) p.Mark("ui.shop.illustrations"); if (n.Tint.HasValue) p.SpriteTint(n.Value, n.Box, n.Tint.Value); else p.Sprite(n.Value, n.Box); continue; }
                if (n.Kind == ShopVisualKind.Text) { Text(p, n); continue; }
                p.Mark("ui.shop." + n.Kind.ToString().ToLowerInvariant());
                bool isButton = n.Kind == ShopVisualKind.Button;
                bool booster = isButton && n.Id.StartsWith("booster_", StringComparison.Ordinal);
                bool enabled = !booster || app.Meta.Economy.Petals >= app.Meta.Economy.Price(Parse(n.Id));
                if (isButton)
                {
                    float depth = enabled ? GardenLook.PressDepth(p.Pressed(Kit.Touch(p, n.Box)), p.Released(Kit.Touch(p, n.Box))) : 0f;
                    var squash = GardenLook.Squash(depth);
                    p.PushSquash(squash.ScaleX, squash.ScaleY, n.Box.CenterX, n.Box.CenterY);
                    if (!enabled) p.PushAlpha(0.55f);
                }
                ShopVisualKind surface = n.Kind == ShopVisualKind.Button && n.Dark ? ShopVisualKind.Well : n.Kind;
                if (n.Kind == ShopVisualKind.Button && !n.Dark)
                {
                    PriceSkin(p, n.Box);
                }
                else p.Picture("shop.surface/" + surface + "/" + n.Group, n.Box, (w, h) => ShopBodyDesign.Surface(surface, w, h, n.Group));
                if (n.Kind != ShopVisualKind.Button) continue;
                Box label = ShopBodyDesign.ButtonLabel(n.Box, n.Dark);
                if (n.Dark)
                {
                    Kit.Petal(p, new Box(n.Box.Left + n.Box.Width * 0.04f, n.Box.Top + n.Box.Height * 0.1f, n.Box.Left + n.Box.Width * 0.24f, n.Box.Bottom - n.Box.Height * 0.1f));
                    label = new Box(n.Box.Left + n.Box.Width * 0.25f, label.Top, label.Right, label.Bottom);
                }
                Text(p, n with { Box = label });
                if (!enabled) p.PopAlpha();
                p.PopTransform();
                if (enabled && Kit.Touch(p, n.Box).Within(area))
                {
                    string id = n.Id;
                    p.Hit(Kit.Touch(p, n.Box), () => Tap(app, id));
                }
            }
            p.PopClip();
            p.VerticalScroll(area, dy => app.ShopScrollOffset = Math.Max(0, Math.Min(max, app.ShopScrollOffset - dy)));
        }
        private static void Text(IPainter p, ShopVisual n)
        {
            p.Mark("font.shop");
            TypeStyle type = n.Type ?? ShopBodyDesign.Title;
            float size = Math.Min(type.Size * p.Scale, n.Box.Height * 0.79f);
            TextLook look = ShopBodyDesign.LookFor(n);
            if (n.Angle != 0) p.PushRotate(n.Angle, n.Box.CenterX, n.Box.CenterY);
            p.Text(n.Value, n.Box.CenterX, n.Box.CenterY, type, look.FillTop, n.Box.Width, size / (type.Size * p.Scale), look);
            if (n.Angle != 0) p.PopTransform();
        }
        private static void PriceSkin(IPainter p, Box b)
        {
            // Matches uGUI Image.Type.Sliced, with borders scaled to 40% of the button height.
            float by = b.Height * 0.4f, bx = Math.Min(b.Width * 0.49f, by * 350f / 190f);
            float[] sx = { 0, 350, 1635, 1985 }, sy = { 0, 190, 315, 505 };
            float[] dx = { b.Left, b.Left + bx, b.Right - bx, b.Right }, dy = { b.Top, b.Top + by, b.Bottom - by, b.Bottom };
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            {
                float scaleX = (dx[x + 1] - dx[x]) / (sx[x + 1] - sx[x]);
                float scaleY = (dy[y + 1] - dy[y]) / (sy[y + 1] - sy[y]);
                float cx = dx[x] + (1985f * 0.5f - sx[x]) * scaleX;
                float cy = dy[y] + (505f * 0.5f - sy[y]) * scaleY;
                p.PushClip(new Box(dx[x], dy[y], dx[x + 1], dy[y + 1]));
                p.PushSquash(1f, scaleY / scaleX, cx, cy);
                p.Sprite("shop/price-button", Box.FromCenter(cx, cy, 1985f * scaleX, 505f * scaleX));
                p.PopTransform();
                p.PopClip();
            }
        }
        private static BoosterKind Parse(string id) => (BoosterKind)Enum.Parse(typeof(BoosterKind), id.Substring(8));
        private static void Tap(DesignApp app, string id)
        {
            if (!id.StartsWith("booster_", StringComparison.Ordinal)) { app.HomeToast(PlaytestText.T("store.preview_status")); return; }
            BoosterKind kind = Parse(id);
            string icon = kind switch { BoosterKind.ExtraSlot => "extra_slot", BoosterKind.Shuffle => "shuffle", BoosterKind.Return => "return", _ => "bloom_burst" };
            string name = PlaytestText.F("store.booster_quantity", EndCards.BoosterName(kind), Client.Services.Economy.EconomyService.PurchaseQuantity(kind));
            app.ConfirmPurchase(PurchaseOffer.ForPetals(name, app.Meta.Economy.Price(kind)), (p, well) => Kit.BoosterIcon(p, icon, well.Inset(well.Width * 0.1f)), () => app.Meta.Economy.TryBuy(kind));
        }
    }
}
