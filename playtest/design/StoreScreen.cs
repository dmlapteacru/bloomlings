using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Store as a page (preview frames 17 and 26; spec 005 FR-029, the owner's note of 2026-10-04) in the reference
    /// layout (contracts/look.md §6.6, <see cref="ScreenLayout.ReferenceStore"/>), opened by
    /// <see cref="DesignApp.OpenStore"/> from Home's Store button, Home's Petals "+" and the Wardrobe's Petals "+"; its
    /// back (and the system back) returns there:
    /// <list type="bullet">
    /// <item><description>the Wardrobe's page header (<see cref="Kit.PageHeader"/>): the back button, the wooden "Store"
    /// banner with ivy and the Petals pill, whose "+" says the Petal packs are offline, over the Wardrobe's
    /// garden;</description></item>
    /// <item><description>a parchment panel to the bottom of the screen with the Shop / Cosmetics tabs (the cosmetics
    /// after L40);</description></item>
    /// <item><description>the Shop: a row per booster with its tile and count badge, its name and a cost pill (a tap buys
    /// it for Petals), then the real-money rows, unavailable in the playtest;</description></item>
    /// <item><description>the Cosmetics: the four family tabs over the lighter panel, the outfit cards of the chosen
    /// family ("Default", then each item for sale on the family's hero with its cost pill; a tap buys) and the footer
    /// between the page arrows.</description></item>
    /// </list>
    /// Buying goes through the Unity client's shared economy and <see cref="WardrobeService"/>; the playtest keeps no rule
    /// of its own.
    /// </summary>
    public static class StoreScreen
    {
        /// <summary>The real-money rows (unavailable in the playtest).</summary>
        private static readonly string[] MoneyRows = { "store.starter_pack", "store.booster_bundle", "store.remove_ads" };

        public static void Draw(IPainter p, DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            bool cosmetics = meta.Wardrobe.IsAvailable;
            ReferenceStoreRegions r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, cosmetics);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, meta.CurrentLevel, OwnerPictures.Wardrobe);

            // The parchment panel, its bottom corners below the screen's edge.
            float radius = r.PanelRadius(p.Scale);
            Kit.Paper(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius), radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            if (cosmetics)
            {
                Kit.Tabs(p, r.Tabs, new[] { PlaytestText.T("store.tab_shop"), PlaytestText.T("store.tab_cosmetics") }, app.StoreTab, i => app.StoreTab = i);
            }

            if (cosmetics && app.StoreTab == 1)
            {
                Outfits(p, app, r);
            }
            else
            {
                ShopRows(p, app, r);
            }

            // The header last, as on the Wardrobe.
            Kit.PageHeader(p, r.Header, PlaytestText.T("store.title"), app.CloseStore, app.ShownPetals, () => app.HomeToast(PlaytestText.T("store.offline")));

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Footer.Top), toast);
            }
        }

        /// <summary>The Shop's rows, a page of them (<see cref="ReferenceStoreRegions.RowsPerPage"/>), with the footer when there are more.</summary>
        private static void ShopRows(IPainter p, DesignApp app, ReferenceStoreRegions r)
        {
            var rows = new List<Action<Box, float>>();
            foreach ((BoosterKind kind, Core.Simulation.Recovery _, string id) in LevelScreen.Boosters)
            {
                rows.Add((line, grow) => BoosterRow(p, app, line, grow, kind, id));
            }

            foreach (string key in MoneyRows)
            {
                rows.Add((line, grow) => MoneyRow(p, line, grow, key));
            }

            // Rows taller than the smallest (a taller page) grow their names with them.
            float grow = r.RowHeight(rows.Count) / (r.W * ReferenceStoreRegions.RowShare);
            int perPage = r.RowsPerPage(rows.Count);
            int pages = Math.Max(1, (rows.Count + perPage - 1) / perPage);
            int page = Math.Max(0, Math.Min(pages - 1, app.StoreRowsPage));
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (page * perPage) + slot;
                if (index < rows.Count)
                {
                    rows[index](r.Row(slot, rows.Count), grow);
                }
            }

            if (pages > 1)
            {
                Footer(p, r, PlaytestText.F("common.page", page + 1, pages), T.Caption, page > 0 ? () => app.StoreRowsPage = page - 1 : (Action?)null, page < pages - 1 ? () => app.StoreRowsPage = page + 1 : (Action?)null);
            }
        }

        /// <summary>A booster's row: its tile with the count badge, its name and its price; a tap on the row buys one for Petals.</summary>
        private static void BoosterRow(IPainter p, DesignApp app, Box line, float grow, BoosterKind kind, string id)
        {
            bool unlocked = app.Meta.Economy.IsUnlocked(kind);
            Kit.Row(p, line, false);
            p.PushAlpha(unlocked ? 1f : 0.45f);
            Box tile = ItemTile(p, line);
            // The icon in the booster tile's 74% box (about two thirds of the tile, as the reference's), the count badge
            // small on its lower right corner.
            Kit.BoosterIcon(p, id, Box.FromCenter(tile.CenterX, tile.CenterY, tile.Width * 0.74f, tile.Width * 0.74f), grey: !unlocked);
            float badge = tile.Width * 0.3f;
            Kit.CountBadge(p, tile.Right - (badge * 0.2f), tile.Bottom - (badge * 0.2f), badge, app.Meta.Economy.Charges(kind).ToString(CultureInfo.InvariantCulture));
            Name(p, line, tile, grow, EndCards.BoosterName(kind));
            p.PopAlpha();
            Price(p, line, app.Meta.Economy.Price(kind), unlocked ? () =>
            {
                if (!app.Meta.Economy.TryBuy(kind))
                {
                    app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                }
            } : (Action?)null);
        }

        /// <summary>A real-money row: the lotus on its tile, its name and "Unavailable", faded (the playtest sells nothing for money).</summary>
        private static void MoneyRow(IPainter p, Box line, float grow, string key)
        {
            Kit.Row(p, line, false);
            p.PushAlpha(0.5f);
            Box tile = ItemTile(p, line);
            Kit.Petal(p, tile.Inset(tile.Width * 0.14f));
            Name(p, line, tile, grow, PlaytestText.T(key));
            p.Text(PlaytestText.T("store.unavailable"), line.Right - (line.Height * 0.85f), line.CenterY, T.Caption, C.InkBrownSoft, line.Width * 0.3f, grow);
            p.PopAlpha();
        }

        /// <summary>
        /// A row's name in brown after its tile, in <c>type.button_secondary</c> grown with the row (<paramref name="grow"/>:
        /// its height over a <see cref="ReferenceStoreRegions.RowShare"/> row's), shrunk to 42% of the row when longer.
        /// </summary>
        private static void Name(IPainter p, Box line, Box tile, float grow, string name) =>
            p.TextLeft(name, tile.Right + (tile.Width * 0.22f), line.CenterY, T.ButtonSecondary, C.InkBrown, line.Width * 0.42f, grow, TextLook.Plain(C.InkBrown));

        /// <summary>
        /// A Shop row's item tile at its left end: the booster tile's cream squircle (§3.7), 0.8 of the row tall (0.12 W on
        /// a <see cref="ReferenceStoreRegions.RowShare"/> row, as the reference's booster boxes). Returns its box.
        /// </summary>
        private static Box ItemTile(IPainter p, Box line)
        {
            float size = line.Height * 0.8f;
            Box box = Box.FromCenter(line.Left + (line.Height * 0.14f) + (size / 2f), line.CenterY - (line.Height * 0.02f), size, size);
            var set = new ColorSet("set.cream.booster_tile", C.CreamFace, GardenLook.BoosterRim.Lighten(0.62f), GardenLook.BoosterLip, GardenLook.BoosterLine);
            Kit.IconFace(p, box, set, size * 0.26f, 0f);
            return box;
        }

        /// <summary>A price as a cost pill with the lotus at a row's right end; a tap on the row buys.</summary>
        private static void Price(IPainter p, Box line, int price, Action? buy)
        {
            float h = line.Height * 0.56f;
            string text = NumberText.Group(price);
            float w = p.MeasureText(text, T.Count, (h * 0.56f) / p.U(T.Count.Size)) + (h * 1.9f);
            var pill = new Box(line.Right - (line.Height * 0.14f) - w, line.CenterY - (h / 2f), line.Right - (line.Height * 0.14f), line.CenterY + (h / 2f));
            p.PushAlpha(buy != null ? 1f : 0.45f);
            Kit.CostPill(p, pill, Cost.Petals(price));
            p.PopAlpha();
            if (buy != null)
            {
                p.Hit(Kit.Touch(p, line), buy);
            }
        }

        /// <summary>
        /// The footer line between the page arrows (<see cref="ReferenceStoreRegions.Footer"/>): <paramref name="text"/> in
        /// <c>ink.brown_soft</c>, the ‹ › buttons at its ends, greyed where there is no page to turn to.
        /// </summary>
        private static void Footer(IPainter p, ReferenceStoreRegions r, string text, TypeStyle style, Action? previous, Action? next)
        {
            Box line = r.Footer;
            float room = line.Height + p.U(12f);
            p.Text(text, line.CenterX, line.CenterY, style, C.InkBrownSoft, line.Width - (2f * room));
            if (previous != null || next != null)
            {
                Kit.ArrowButton(p, r.PagePrevious.CenterX, r.PagePrevious.CenterY, r.PagePrevious.Width, next: false, previous);
                Kit.ArrowButton(p, r.PageNext.CenterX, r.PageNext.CenterY, r.PageNext.Width, next: true, next);
            }
        }

        /// <summary>
        /// The Store's cosmetics in the reference Wardrobe's look (spec 005 §4.6): the four family tabs with their heroes,
        /// the outfit cards of the chosen family (the "Default" look, worn while the family wears nothing, then each item
        /// for sale shown on the family's hero with its cost pill; a tap buys), as many to a page as the page's height holds
        /// (<see cref="ReferenceStoreRegions.OutfitsPerPage"/>), and the footer line with the page arrows.
        /// </summary>
        private static void Outfits(IPainter p, DesignApp app, ReferenceStoreRegions r)
        {
            WardrobeService wardrobe = app.Meta.Wardrobe;
            IReadOnlyList<Family> families = WardrobeService.Families;
            int selected = Math.Max(0, Math.Min(families.Count - 1, app.StoreFamily));
            Family family = families[selected];
            var names = new string[families.Count];
            for (int i = 0; i < families.Count; i++)
            {
                names[i] = PlaytestText.T("family." + WardrobeService.FamilyKey(families[i]));
            }

            Kit.FamilyTabs(p, r.FamilyTabs, r.OutfitPanel, families, names, selected, i =>
            {
                app.StoreFamily = i;
                app.StorePage = 0;
            }, f => wardrobe.OutfitOf(f));

            var items = new List<CosmeticItem?> { null };
            items.AddRange(wardrobe.ForSale);
            int perPage = r.OutfitsPerPage;
            int pages = Math.Max(1, (items.Count + perPage - 1) / perPage);
            int page = Math.Max(0, Math.Min(pages - 1, app.StorePage));
            Outfit worn = wardrobe.OutfitOf(family);
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (page * perPage) + slot;
                if (index >= items.Count)
                {
                    break;
                }

                Box box = r.OutfitCard(slot);
                CosmeticItem? item = items[index];
                if (item == null)
                {
                    Kit.OutfitCard(p, box, PlaytestText.T("wardrobe.default"), worn.IsEmpty, well => Preview(p, well, family, null), pillRoom: true);
                    continue;
                }

                string id = item.Id;
                Kit.OutfitCard(p, box, MetaCards.ItemName(item), false, well => Preview(p, well, family, item), Cost.Petals(item.Price), () =>
                {
                    if (!wardrobe.TryBuy(id))
                    {
                        app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                    }
                });
            }

            Footer(p, r, PlaytestText.T("wardrobe.footer"), T.Body, page > 0 ? () => app.StorePage = page - 1 : (Action?)null, page < pages - 1 ? () => app.StorePage = page + 1 : (Action?)null);
        }

        /// <summary>
        /// An outfit card's picture: the family's hero wearing the item (skins, hats, trails, faces), or its plain look for
        /// the Default card; a profile item (frame, badge, marker) as its own shape with a small portrait of the hero.
        /// </summary>
        private static void Preview(IPainter p, Box well, Family family, CosmeticItem? item)
        {
            // The hero fills the well with its feet near the bottom; a hat sits above the picture, so with a hat the hero
            // is a little smaller and lower to keep the hat inside the well.
            bool hat = item != null && item.Kind == CosmeticKind.Hat;
            float height = well.Height * (hat ? 0.96f : 1.1f);
            Box hero = HomeStage.Figure(well.CenterX, well.Bottom - (well.Height * (hat ? 0.02f : 0.05f)), height);
            if (item == null || item.IsWorn)
            {
                Outfit? outfit = item == null ? null : item.Kind switch
                {
                    CosmeticKind.Skin => new Outfit(item, null, null, null),
                    CosmeticKind.Hat => new Outfit(null, item, null, null),
                    CosmeticKind.Trail => new Outfit(null, null, item, null),
                    _ => new Outfit(null, null, null, item),
                };
                Visuals.Hero(p, hero, family, outfit);
                return;
            }

            float size = Math.Min(well.Width, well.Height) * 0.8f;
            string shape = ShapeLibrary.CosmeticId(item.Shape);
            Box mark = Box.FromCenter(well.CenterX, well.CenterY, size, size);
            if (item.Kind == CosmeticKind.Frame)
            {
                Visuals.Hero(p, mark.Inset(size * 0.16f), family, null);
                p.Shape(ShapeLibrary.Has(shape) ? shape : "ui.star", mark, Visuals.Tint(item));
                return;
            }

            p.Shape(ShapeLibrary.Has(shape) ? shape : "ui.star", mark.Inset(size * 0.08f), Visuals.Tint(item));
        }
    }
}
