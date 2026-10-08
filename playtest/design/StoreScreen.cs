using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
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
    /// <see cref="DesignApp.OpenStore"/> from the bottom menu's Shop, Home's Petals "+" and the Wardrobe's Petals "+"; its
    /// back (and the system back) returns there:
    /// <list type="bullet">
    /// <item><description>the Wardrobe's page header (<see cref="Kit.PageHeader"/>): the back button, the wooden "Store"
    /// banner with ivy and the Petals pill, whose "+" says the Petal packs are offline, over the Wardrobe's
    /// garden;</description></item>
    /// <item><description>a parchment panel to the bottom of the screen with the Shop / Cosmetics tabs (the cosmetics
    /// after L40);</description></item>
    /// <item><description>the Shop: a row per booster with its tile and count badge, its name and a cost pill (a tap asks
    /// to buy it for Petals), then the real-money rows, unavailable in the playtest;</description></item>
    /// <item><description>the Cosmetics: the four family tabs over the lighter panel, the outfit cards of the chosen
    /// family ("Default", then each item for sale on the family's hero with its cost pill; a tap asks to buy) and the
    /// footer between the page arrows;</description></item>
    /// <item><description>the Animations: the clearing styles' cards with their live previews and action buttons (Buy,
    /// Choose, Chosen; spec 005 FR-038 as amended on 2026-10-06).</description></item>
    /// <item><description>the bottom menu over the panel's foot, the Shop in its medallion (spec 005 FR-030); the list
    /// ends above it.</description></item>
    /// </list>
    /// Before the Store unlocks (L12) the bottom menu's Shop still opens the page, locked (<see cref="Locked"/>): the same
    /// backdrop, header and panel, the panel holding the locked notice ("Available from level 12") instead of the tabs,
    /// rows and page arrows.
    /// Buying goes through the Unity client's shared economy and <see cref="WardrobeService"/>; the playtest keeps no rule
    /// of its own. Every purchase asks the purchase confirmation first (spec 005 FR-040, <see cref="DesignApp.ConfirmPurchase"/>):
    /// nothing is spent until its Buy, and short Petals say so without asking. The rows, the cards and the clearing styles
    /// are a page that scrolls (FR-041, <see cref="IPainter.Scroll"/>): a drag over them never taps, a swipe turns their page.
    /// </summary>
    public static class StoreScreen
    {
        /// <summary>The real-money rows (unavailable in the playtest).</summary>
        private static readonly string[] MoneyRows = { "store.starter_pack", "store.booster_bundle", "store.remove_ads" };

        public static void Draw(IPainter p, DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            HomeLook look = HomeScreen.Look(app);
            app.StoreMoving = false;
            if (!BottomNav.IsOpen(NavPlace.Shop, look))
            {
                Locked(p, app, look);
                return;
            }

            // The tabs: the Shop, the Cosmetics once the Wardrobe opens (L40), and the Animations (the clearing styles'
            // previews show from the Store's unlock; spec 005 FR-038).
            bool cosmetics = meta.Wardrobe.IsAvailable;
            var tabs = new List<string> { "store.tab_shop" };
            if (cosmetics)
            {
                tabs.Add("store.tab_cosmetics");
            }

            tabs.Add("store.tab_animations");
            int tab = Math.Max(0, Math.Min(tabs.Count - 1, app.StoreTab));
            ReferenceStoreRegions r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, hasCosmetics: true);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, meta.CurrentLevel, OwnerPictures.Wardrobe);

            // The parchment panel, its bottom corners below the screen's edge.
            float radius = r.PanelRadius(p.Scale);
            Kit.CardFrame(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            Kit.Tabs(p, r.Tabs, tabs.ConvertAll(PlaytestText.T).ToArray(), tab, i =>
            {
                app.StoreTab = i;
                app.StorePage = 0;
            });

            switch (tabs[tab])
            {
                case "store.tab_cosmetics":
                    Outfits(p, app, r);
                    break;
                case "store.tab_animations":
                    // The cards' live previews loop all the time: the host keeps drawing (FR-038 as amended).
                    app.StoreMoving = true;
                    Animations(p, app, r);
                    break;
                default:
                    ShopRows(p, app, r);
                    break;
            }

            // The bottom menu, the Shop in its medallion (FR-030); the header last, as on the Wardrobe.
            Kit.PageFlowers(p, r.Panel, ScreenLayout.BottomNavTop(p.Width, p.Height, p.Insets));
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Shop), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("store.title"), app.CloseStore, app.ShownPetals, () => app.HomeToast(PlaytestText.T("store.offline")));

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Footer.Top), toast);
            }
        }

        /// <summary>
        /// The locked Store page (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the
        /// Wardrobe's garden, the parchment panel holding the locked notice (<see cref="Kit.LockedNotice"/>: the Shop's
        /// icon with its padlock, "Available from level N" from the roadmap) where the tabs and rows would be, the bottom
        /// menu with the Shop raised, and the header with its back and the Petals pill, without the "+" (the Store it
        /// would open is the locked one).
        /// </summary>
        private static void Locked(IPainter p, DesignApp app, HomeLook look)
        {
            LockedPageRegions r = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);
            float radius = r.PanelRadius(p.Scale);
            Kit.CardFrame(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            Kit.LockedNotice(p, r.Notice, NavPlace.Shop, app.UnlockLevel(NavPlace.Shop));
            Kit.PageFlowers(p, r.Panel, ScreenLayout.BottomNavTop(p.Width, p.Height, p.Insets));
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Shop), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("store.title"), app.CloseStore, app.ShownPetals, null);
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

            // Rows taller than the type's row (a taller page) grow their names with them, shorter ones shrink them.
            float grow = r.RowHeight(rows.Count) / (r.W * ReferenceStoreRegions.RowTypeShare);
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

            Action? previous = page > 0 ? () => app.StoreRowsPage = page - 1 : (Action?)null;
            Action? next = page < pages - 1 ? () => app.StoreRowsPage = page + 1 : (Action?)null;
            if (pages > 1)
            {
                Footer(p, r, PlaytestText.F("common.page", page + 1, pages), T.Caption, previous, next);
            }

            // A drag over the rows never buys; a swipe turns their page (FR-041).
            p.Scroll(r.List, previous, next);
        }

        /// <summary>A booster's row: its tile with the count badge, its name and its price; a tap on the row asks to buy one for Petals.</summary>
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
            Name(p, line, tile, grow, EndCards.BoosterName(kind), PriceBox(p, line, app.Meta.Economy.Price(kind)).Left);
            p.PopAlpha();
            Price(p, line, app.Meta.Economy.Price(kind), unlocked ? () => BuyBooster(app, kind, id) : (Action?)null);
        }

        /// <summary>
        /// A booster row's tap: short Petals say so at once; else the purchase confirmation asks (FR-040) and the charge is
        /// bought only on its Buy.
        /// </summary>
        private static void BuyBooster(DesignApp app, BoosterKind kind, string id)
        {
            int price = app.Meta.Economy.Price(kind);
            if (app.Meta.Economy.Petals < price)
            {
                Refuse(app);
                return;
            }

            app.ConfirmPurchase(PurchaseOffer.ForPetals(EndCards.BoosterName(kind), price), (q, well) => Kit.BoosterIcon(q, id, well.Inset(well.Width * 0.1f)), () =>
            {
                if (!app.Meta.Economy.TryBuy(kind))
                {
                    Refuse(app);
                }
            });
        }

        /// <summary>Too few Petals: the refusal sound and the toast.</summary>
        private static void Refuse(DesignApp app)
        {
            app.Sound.Play(SoundCue.Refused);
            app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
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
        /// its height over a <see cref="ReferenceStoreRegions.RowTypeShare"/> row's), shrunk to 42% of the row when longer,
        /// and to the room before <paramref name="right"/> (the price pill's left, a 4-digit price since 2026-10-05).
        /// </summary>
        private static void Name(IPainter p, Box line, Box tile, float grow, string name, float right = float.MaxValue)
        {
            float left = tile.Right + (tile.Width * 0.22f);
            float room = Math.Min(line.Width * 0.42f, right - (line.Height * 0.12f) - left);
            p.TextLeft(name, left, line.CenterY, T.ButtonSecondary, C.InkBrown, room, grow, TextLook.Plain(C.InkBrown));
        }

        /// <summary>
        /// A Shop row's item tile at its left end: the booster tile's cream squircle (§3.7), 0.8 of the row tall (0.12 W on
        /// a <see cref="ReferenceStoreRegions.RowTypeShare"/> row, as the reference's booster boxes). Returns its box.
        /// </summary>
        private static Box ItemTile(IPainter p, Box line)
        {
            float size = line.Height * 0.8f;
            Box box = Box.FromCenter(line.Left + (line.Height * 0.14f) + (size / 2f), line.CenterY - (line.Height * 0.02f), size, size);
            Kit.RaisedButton(p, box, GardenLook.Cream, 0.26f, 0f);
            return box;
        }

        /// <summary>Where a row's price button goes (spec 005 FR-047: raised on its plate): at its right end, as wide as the lotus and the grouped price on its face.</summary>
        private static Box PriceBox(IPainter p, Box line, int price)
        {
            float h = line.Height * 0.7f;
            float w = p.MeasureText(NumberText.Group(price), T.Count, (h * 0.45f) / p.U(T.Count.Size)) + (h * 1.75f);
            return new Box(line.Right - (line.Height * 0.14f) - w, line.CenterY - (h / 2f), line.Right - (line.Height * 0.14f), line.CenterY + (h / 2f));
        }

        /// <summary>
        /// A price as a cost pill raised on its plate (a button) with the lotus at a row's right end, the main buttons' leaves
        /// and flower over its corners (spec 005 FR-047, the owner's references of 2026-10-08); the whole row takes the tap.
        /// </summary>
        private static void Price(IPainter p, Box line, int price, Action? buy)
        {
            Box pill = PriceBox(p, line, price);
            p.PushAlpha(buy != null ? 1f : 0.45f);
            Kit.CostPill(p, pill, Cost.Petals(price), button: true);
            Kit.Decoration(p, pill);
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
                    // Short Petals say so at once; else the confirmation asks and only its Buy spends (FR-040).
                    if (app.Meta.Economy.Petals < item.Price)
                    {
                        Refuse(app);
                        return;
                    }

                    app.ConfirmPurchase(PurchaseOffer.ForPetals(MetaCards.ItemName(item), item.Price), (q, well) => Preview(q, well, family, item), () =>
                    {
                        if (!wardrobe.TryBuy(id))
                        {
                            Refuse(app);
                        }
                    });
                });
            }

            Action? previous = page > 0 ? () => app.StorePage = page - 1 : (Action?)null;
            Action? next = page < pages - 1 ? () => app.StorePage = page + 1 : (Action?)null;
            Footer(p, r, PlaytestText.T("wardrobe.footer"), T.Body, previous, next);

            // A drag over the cards never buys one (the owner's request of 2026-10-06); a swipe turns their page (FR-041).
            p.Scroll(r.OutfitPanel, previous, next);
        }

        /// <summary>
        /// The Store's Animations (spec 005 FR-038 as amended on 2026-10-06, contracts/look.md §6.12): the free pair's card
        /// first, then each bought clearing style, each card holding the style's live preview (<see cref="Kit.ClearingPreview"/>),
        /// looping all the time, and its action button (<see cref="Kit.ClearingButton"/>): the green "Buy" with the price
        /// (the purchase confirmation asks first, FR-040), "Choose" for an owned style not chosen, "Chosen" with the check for
        /// the chosen one (the free pair's while no bought style is chosen). Before L40 a style not owned keeps its cost pill
        /// and the padlock badge (the preview still bright), and a tap says from which level. The whole card is the button
        /// (one touch target, its button inside it); the chosen card takes no tap.
        /// </summary>
        private static void Animations(IPainter p, DesignApp app, ReferenceStoreRegions r)
        {
            ClearingService clearing = app.Meta.Clearing;
            int level = app.Meta.CurrentLevel;
            var styles = new List<ClearStyle> { ClearStyle.Blossom };
            styles.AddRange(ClearStyles.Bought);
            int perPage = r.ClearingsPerPage;
            int pages = Math.Max(1, (styles.Count + perPage - 1) / perPage);
            int page = Math.Max(0, Math.Min(pages - 1, app.StorePage));
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (page * perPage) + slot;
                if (index >= styles.Count)
                {
                    break;
                }

                ClearStyle style = styles[index];
                bool free = ClearStyles.IsFree(style);
                string name = PlaytestText.T(free ? "clearing.free_pair" : ClearStyles.NameKey(style));
                ClearingAction action = clearing.ActionOf(style, level);
                bool locked = action == ClearingAction.Locked;
                Action? tap = action == ClearingAction.Chosen ? (Action?)null : () => TapClearing(app, style, name);
                Box box = r.ClearingCard(slot);
                Kit.OutfitCard(p, box, name, action == ClearingAction.Chosen, well => Kit.ClearingPreview(p, well, style, pair: free), locked ? Cost.Petals(clearing.Price) : (Cost?)null, tap, pillRoom: true, locked: locked, dim: false);
                if (!locked)
                {
                    Kit.ClearingButton(p, ClearingCard.Button(box), action, clearing.Price, tap != null);
                }
            }

            Action? previous = page > 0 ? () => app.StorePage = page - 1 : (Action?)null;
            Action? next = page < pages - 1 ? () => app.StorePage = page + 1 : (Action?)null;
            Footer(p, r, PlaytestText.T("store.animations_footer"), T.Body, previous, next);
            p.Scroll(r.List, previous, next);
        }

        /// <summary>
        /// A tap on a clearing style's card or button: a purchase asks the confirmation first and buys only on its Buy
        /// (FR-040); choosing an owned style happens at once; a refused tap says why (from which level, too few Petals).
        /// </summary>
        private static void TapClearing(DesignApp app, ClearStyle style, string name)
        {
            ClearingService clearing = app.Meta.Clearing;
            if (clearing.Check(style, app.Meta.CurrentLevel) == ClearingTap.Bought)
            {
                app.ConfirmPurchase(PurchaseOffer.ForPetals(name, clearing.Price), (q, well) => Kit.ClearingPreview(q, well, style), () => Tapped(app, clearing.Tap(style, app.Meta.CurrentLevel)));
                return;
            }

            Tapped(app, clearing.Tap(style, app.Meta.CurrentLevel));
        }

        /// <summary>What a clearing tap did: a click when chosen or bought, else why not.</summary>
        private static void Tapped(DesignApp app, ClearingTap tap)
        {
            switch (tap)
            {
                case ClearingTap.Chosen:
                case ClearingTap.Bought:
                    app.Sound.Play(SoundCue.Click);
                    break;
                case ClearingTap.Locked:
                    app.HomeToast(PlaytestText.F("locked.message", ClearStyles.BuyFromLevel));
                    break;
                case ClearingTap.Short:
                    Refuse(app);
                    break;
                default:
                    app.HomeToast(PlaytestText.T("store.unavailable"));
                    break;
            }
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
