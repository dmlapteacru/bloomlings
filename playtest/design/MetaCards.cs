using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Meta.Collection;
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
    /// The meta cards of frames 4, 5, 6 and 17 (spec 002 US3, FR-022 to FR-025) in the reference look (spec 005 §4.3,
    /// §4.6): parchment cards under wooden sign headers, cream rows and frames, green and cream buttons. The Daily Reward,
    /// the Leaderboard (offline, since the server is deferred), the Collection and the Store, whose cosmetics take the
    /// reference Wardrobe's family tabs and outfit cards. Real money and ads are unavailable in the playtest.
    /// </summary>
    public static class MetaCards
    {
        /// <summary>The Daily Reward popup: Day N, the lotus heap in its basket and light, the "+N" pill, Claim, and "Get +N" (ad).</summary>
        public static void DailyReward(IPainter p, DesignApp app, float since)
        {
            var daily = app.Meta.DailyReward;
            CardRegions r = Kit.Card(p, 60f + 330f + 110f + DesignTokens.Size.CardPrimaryHeight + DesignTokens.Size.SecondaryHeight + 90f, PlaytestText.T("daily_reward.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.None);
            float y = r.Body.Top;
            p.Text(PlaytestText.F("daily_reward.day", daily.NextStreak), r.Body.CenterX, y + p.U(20f), T.Body, C.InkBrownSoft);
            y += p.U(60f);

            // The reward: a heap of lotuses over a woven basket, in the win's soft turning light.
            p.Mark("currency.petal_pile");
            var art = new Box(r.Body.CenterX - p.U(220f), y, r.Body.CenterX + p.U(220f), y + p.U(330f));
            Kit.LightRays(p, art.CenterX, art.CenterY, art.Width * 0.62f, since);
            (float X, float Y, float S)[] heap = { (-0.22f, 0.02f, 0.3f), (0.2f, 0.0f, 0.32f), (0f, -0.12f, 0.34f), (-0.08f, 0.1f, 0.28f), (0.12f, 0.12f, 0.26f) };
            foreach ((float hx, float hy, float hs) in heap)
            {
                float size = art.Width * hs;
                Kit.Petal(p, Box.FromCenter(art.CenterX + (hx * art.Width), art.CenterY + (hy * art.Height), size, size));
            }

            Basket(p, Box.FromCenter(art.CenterX, art.CenterY + (art.Height * 0.2f), art.Width * 0.8f, art.Width * 0.6f));
            y = art.Bottom + p.U(10f);

            RewardPill(p, Box.FromCenter(r.Body.CenterX, y + p.U(50f), p.U(250f), p.U(92f)), NumberText.Plus(daily.NextPetals));
            y += p.U(110f);

            // Claim breathes while it waits, and the claim bursts sparkles over the Petals pill (spec 003 FR-019, FR-020).
            Box claim = ScreenLayout.CardButton(r.Body, y + p.U(10f), true, p.Scale);
            long before = app.Meta.Economy.Petals;
            Kit.PrimaryButton(p, claim, PlaytestText.T("daily_reward.claim"), daily.CanClaim ? () =>
            {
                int paid = daily.Claim();
                app.CloseOverlay();
                app.RewardBurst(before);
                app.HomeToast(PlaytestText.F("common.petals_plus", paid));
            } : (Action?)null, decorate: true, breathe: true);
            Box bonus = ScreenLayout.CardButton(r.Body, claim.Bottom + p.U(24f), false, p.Scale).Inset(p.U(40f), 0f);
            Kit.SecondaryButton(p, bonus, PlaytestText.F("daily_reward.bonus", 20), null, "ui.ad");
            p.Text(PlaytestText.T("win.no_ads"), r.Body.CenterX, bonus.Bottom + p.U(34f), T.Caption, C.InkBrownSoft);
            Kit.EndCard(p);
        }

        /// <summary>The reward basket: woven wood (darker weave lines) with a lighter rim, outlined like the reference's objects.</summary>
        private static void Basket(IPainter p, Box box)
        {
            p.Mark("currency.reward_basket");
            Func<float, float, float> sdf = ShapeLibrary.Get("currency.reward_basket");
            p.ShapeOf("currency.reward_basket/line", (x, y) => sdf(x, y) - 0.05f, box, C.WoodDarkLine);
            p.Shape("currency.reward_basket", box, C.RewardBasket);
            p.ShapeOf("currency.reward_basket/weave", (x, y) => Math.Max(sdf(x, y) + 0.05f, Math.Min(Weave(y, 7f), Weave(x + (0.07f * (float)Math.Floor(y * 7f)), 9f))), box, C.WoodDarkLine.WithAlpha(0.35f));
            p.PushClip(new Box(box.Left, box.Top, box.Right, box.CenterY));
            p.ShapeOf("currency.reward_basket/light", (x, y) => sdf(x, y) + 0.04f, box, C.RewardBasket.Lighten(0.25f));
            p.PopClip();
        }

        /// <summary>Thin lines across <paramref name="v"/> every 1/<paramref name="count"/> (negative on a line).</summary>
        private static float Weave(float v, float count)
        {
            float t = (v * count) - (float)Math.Floor(v * count);
            return (0.42f - Math.Abs(t - 0.5f)) / count;
        }

        /// <summary>A reward "+N" on a cream pill with the lotus (the cost pill's look, larger).</summary>
        private static void RewardPill(IPainter p, Box box, string text)
        {
            p.Mark("ui.pill.cost");
            float h = box.Height;
            float radius = h / 2f;
            float line = Math.Max(p.U(2f), h * 0.04f);
            Kit.SoftShadow(p, box, radius, 0.2f, 0.1f);
            p.FillRound(box.Offset(0f, h * 0.07f), radius, C.CreamLip);
            p.FillRoundGradient(box, radius, C.CreamTop, C.ParchmentBottom);
            p.StrokeRound(box.Inset(line / 2f), radius - (line / 2f), line, C.CreamLine);
            float icon = h * 0.92f;
            float gap = h * 0.12f;
            float scale = (h * 0.64f) / p.U(T.Reward.Size);
            float textWidth = Math.Min(p.MeasureText(text, T.Reward, scale), box.Width - icon - gap - (h * 0.5f));
            float start = box.CenterX - ((icon + gap + textWidth) / 2f);
            // The lotus first, then the amount, as on the win's reward pill and every cost pill.
            Kit.Petal(p, Box.FromCenter(start + (icon / 2f), box.CenterY - (h * 0.02f), icon, icon));
            p.Text(text, start + icon + gap + (textWidth / 2f), box.CenterY, T.Reward, C.InkBrown, textWidth, scale, TextLook.Plain(C.InkBrown));
        }

        /// <summary>
        /// The Leaderboard in its offline form: the top ranks with medals as placeholder rows (no invented players), the
        /// gap, and the player's own row highlighted as "You", with the offline notice (research R12).
        /// </summary>
        public static void Leaderboard(IPainter p, DesignApp app, float since)
        {
            const float row = 96f;
            CardRegions r = Kit.Card(p, (8f * (row + 14f)) + 110f, PlaytestText.T("leaderboard.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.None);
            Box[] rows = ScreenLayout.Column(r.Body, 8, p.U(row), p.U(14f));
            for (int i = 0; i < 5; i++)
            {
                Box line = rows[i];
                Kit.Row(p, line, false);
                Rank(p, line, i + 1);
                Portrait(p, line.Left + p.U(170f), line.CenterY, p.U(68f), null);
                Placeholder(p, Box.FromCenter(line.Left + p.U(340f), line.CenterY, p.U(190f), p.U(28f)));
                Placeholder(p, Box.FromCenter(line.Right - p.U(90f), line.CenterY, p.U(100f), p.U(28f)));
            }

            p.Text("…", rows[5].CenterX, rows[5].CenterY, T.Title, C.InkBrownSoft);

            Box you = rows[6];
            Kit.Row(p, you, highlighted: true);
            p.Text("—", you.Left + p.U(70f), you.CenterY, T.Body, C.InkBrown);
            Portrait(p, you.Left + p.U(170f), you.CenterY, p.U(68f), Family.Sprig);
            p.TextLeft(PlaytestText.T("leaderboard.you"), you.Left + p.U(230f), you.CenterY, T.ButtonSecondary, C.InkBrown, look: TextLook.Plain(C.InkBrown));
            p.Text(NumberText.Group(app.Meta.Progression.HighestCompletedLevel), you.Right - p.U(90f), you.CenterY, T.Count, C.InkBrown, look: TextLook.Plain(C.InkBrown));

            p.Text(PlaytestText.T("leaderboard.offline_empty"), r.Body.CenterX, rows[7].CenterY + p.U(30f), T.Caption, C.InkBrownSoft, r.Body.Width);
            Kit.EndCard(p);
        }

        private static void Rank(IPainter p, Box line, int rank)
        {
            float cx = line.Left + p.U(70f);
            Rgba? medal = C.Medal(rank);
            string number = rank.ToString(CultureInfo.InvariantCulture);
            if (medal.HasValue)
            {
                Box box = Box.FromCenter(cx, line.CenterY, p.U(72f), p.U(72f));
                Func<float, float, float> sdf = ShapeLibrary.Get("ui.medal");
                p.ShapeOf("ui.medal/line/0.06", (x, y) => sdf(x, y) - 0.06f, box, medal.Value.Darken(0.42f));
                p.Shape("ui.medal", box, medal.Value);
                p.Text(number, cx, line.CenterY + p.U(8f), T.Badge, medal.Value.Darken(0.55f));
            }
            else
            {
                p.Text(number, cx, line.CenterY, T.Body, C.InkBrown, look: TextLook.Plain(C.InkBrown));
            }
        }

        /// <summary>A round portrait on a cream disc: the player's hero, or the anonymous figure of a placeholder row.</summary>
        private static void Portrait(IPainter p, float cx, float cy, float size, Family? family)
        {
            float ring = Math.Max(1f, p.U(3f));
            p.FillCircle(cx, cy + (ring * 0.8f), (size / 2f) + ring, C.CreamLip);
            p.FillCircle(cx, cy, (size / 2f) + ring, C.CreamLine);
            p.FillRoundGradient(Box.FromCenter(cx, cy, size, size), size / 2f, C.CreamTop, C.CreamFace);
            if (family.HasValue)
            {
                Visuals.Hero(p, Box.FromCenter(cx, cy + (size * 0.02f), size * 0.92f, size * 0.92f), family.Value, null);
            }
            else
            {
                p.Shape("ui.person", Box.FromCenter(cx, cy + (size * 0.04f), size * 0.7f, size * 0.7f), C.CreamLine);
            }
        }

        /// <summary>A sunk parchment bar where a name or a score will show once the leaderboard is online.</summary>
        private static void Placeholder(IPainter p, Box box)
        {
            p.FillRound(box, box.Height / 2f, C.ParchmentWell);
            p.StrokeRound(box.Inset(0.5f), (box.Height / 2f) - 0.5f, Math.Max(1f, p.U(2f)), C.ParchmentEdge);
        }

        /// <summary>The Collection: a grid of framed finished pictures, and a detail view with its name and level (never a level selector).</summary>
        public static void Collection(IPainter p, DesignApp app, float since)
        {
            IReadOnlyList<CollectionEntry> entries = app.Meta.Collection.Entries;
            if (app.CollectionDetail >= 0 && app.CollectionDetail < entries.Count)
            {
                Detail(p, app, entries[app.CollectionDetail], since);
                return;
            }

            const int columns = 3;
            int rows = Math.Max(1, Math.Min(4, (entries.Count + columns - 1) / columns));
            CardRegions r = Kit.Card(p, (rows * 250f) + 80f, PlaytestText.T("collection.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.None);
            string count = entries.Count == 1 ? PlaytestText.F("collection.count_one", 1) : PlaytestText.F("collection.count_many", entries.Count);
            p.Text(count, r.Body.CenterX, r.Body.Top + p.U(22f), T.Caption, C.InkBrownSoft);
            float cell = Math.Min((r.Body.Width - p.U(40f)) / columns, p.U(250f));
            float x0 = r.Body.CenterX - (cell * columns / 2f);
            int start = Math.Max(0, entries.Count - (rows * columns));
            for (int i = start; i < entries.Count; i++)
            {
                int n = i - start;
                var box = new Box(x0 + ((n % columns) * cell), r.Body.Top + p.U(60f) + ((n / columns) * cell), x0 + ((n % columns) * cell) + cell, r.Body.Top + p.U(60f) + ((n / columns) * cell) + cell).Inset(p.U(12f));
                float depth = Kit.Press(p, box, true);
                Kit.Squash(p, box, depth, tile: true);
                Frame(p, box, entries[i], app);
                p.PopTransform();
                int index = i;
                p.Hit(box, () => app.CollectionDetail = index);
            }

            Kit.EndCard(p);
        }

        private static void Detail(IPainter p, DesignApp app, CollectionEntry entry, float since)
        {
            p.Mark("collection.detail_frame");
            CardRegions r = Kit.Card(p, 700f, PlaytestText.T("collection.title"), app.CardCloseWith(() => app.CollectionDetail = -1), Kit.Pop(since), sign: SignDecor.None);
            float side = Math.Min(r.Body.Width - p.U(120f), p.U(540f));
            var frame = new Box(r.Body.CenterX - (side / 2f), r.Body.Top + p.U(10f), r.Body.CenterX + (side / 2f), r.Body.Top + p.U(10f) + side);
            Frame(p, frame, entry, app);
            p.Text(PictureName(entry.PictureId), r.Body.CenterX, frame.Bottom + p.U(56f), T.Title, C.InkBrown, r.Body.Width, look: TextLook.Plain(C.InkBrown));
            p.Text(PlaytestText.F("collection.completed", NumberText.Group(entry.LevelNumber)), r.Body.CenterX, frame.Bottom + p.U(114f), T.Body, C.InkBrownSoft);
            Kit.EndCard(p);
        }

        /// <summary>A picture's name from its id, in sentence case ("tulip_pot" → "Tulip pot").</summary>
        private static string PictureName(string id)
        {
            string name = id.Replace('_', ' ');
            return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>A finished picture in its cream frame (<see cref="Kit.PictureFrame"/>): the full-color tiles in their stone border.</summary>
        private static void Frame(IPainter p, Box box, CollectionEntry entry, DesignApp app)
        {
            Box well = Kit.PictureFrame(p, box);
            if (app.Content.TryGetLevel(app.Resolve(entry.LevelNumber), out LevelDefinition? definition) && definition != null)
            {
                BoardPainter.Picture(p, well.Inset(well.Width * 0.04f), definition, app.Content.GetPicture(definition.Picture));
            }
        }

        /// <summary>
        /// The Store under its wooden banner with ivy (spec 005 §4.3, §4.6): the Petals balance pill, and the Shop's rows of
        /// booster tiles, names and cost pills (boosters are bought with Petals; real-money rows are unavailable in the
        /// playtest). Cosmetics join after L40 (spec 001) in the reference Wardrobe's look.
        /// </summary>
        public static void Store(IPainter p, DesignApp app, float since)
        {
            const float row = 118f;
            bool cosmetics = app.Meta.Wardrobe.IsAvailable;
            CardRegions r = Kit.Card(p, 130f + (cosmetics ? 116f : 0f) + (7f * (row + 16f)), PlaytestText.T("store.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.Ivy);
            float y = r.Body.Top;
            Kit.PetalsPill(p, Box.FromCenter(r.Body.CenterX, y + p.U(40f), p.U(360f), p.U(84f)), app.Meta.Economy.Petals, () => app.HomeToast(PlaytestText.T("store.offline")), align: 0.5f);
            y += p.U(130f);
            if (cosmetics)
            {
                Kit.Tabs(p, new Box(r.Body.Left + p.U(40f), y, r.Body.Right - p.U(40f), y + p.U(84f)), new[] { PlaytestText.T("store.tab_shop"), PlaytestText.T("store.tab_cosmetics") }, app.StoreTab, i => app.StoreTab = i);
                y += p.U(116f);
            }

            var column = new Box(r.Body.Left, y, r.Body.Right, r.Body.Bottom);
            if (cosmetics && app.StoreTab == 1)
            {
                Outfits(p, app, column);
            }
            else
            {
                ShopRows(p, app, column, row);
            }

            Kit.EndCard(p);
        }

        private static void ShopRows(IPainter p, DesignApp app, Box column, float row)
        {
            Box[] rows = ScreenLayout.Column(column, 7, p.U(row), p.U(16f));
            int i = 0;
            foreach ((BoosterKind kind, Core.Simulation.Recovery _, string id) in LevelScreen.Boosters)
            {
                Box line = rows[i++];
                bool unlocked = app.Meta.Economy.IsUnlocked(kind);
                Kit.Row(p, line, false);
                p.PushAlpha(unlocked ? 1f : 0.45f);
                Box tile = ItemTile(p, line);
                // The icon in the booster tile's 74% box (about two thirds of the tile, as the reference's), the count
                // badge small on its lower right corner.
                Kit.BoosterIcon(p, id, Box.FromCenter(tile.CenterX, tile.CenterY, tile.Width * 0.74f, tile.Width * 0.74f), grey: !unlocked);
                float badge = tile.Width * 0.3f;
                Kit.CountBadge(p, tile.Right - (badge * 0.2f), tile.Bottom - (badge * 0.2f), badge, app.Meta.Economy.Charges(kind).ToString(CultureInfo.InvariantCulture));
                p.TextLeft(EndCards.BoosterName(kind), line.Left + p.U(150f), line.CenterY, T.ButtonSecondary, C.InkBrown, line.Width * 0.42f, 0.86f, TextLook.Plain(C.InkBrown));
                p.PopAlpha();
                Price(p, line, app.Meta.Economy.Price(kind), unlocked ? () =>
                {
                    if (!app.Meta.Economy.TryBuy(kind))
                    {
                        app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                    }
                } : (Action?)null);
            }

            foreach (string key in new[] { "store.starter_pack", "store.booster_bundle", "store.remove_ads" })
            {
                Box line = rows[i++];
                Kit.Row(p, line, false);
                p.PushAlpha(0.5f);
                Box tile = ItemTile(p, line);
                Kit.Petal(p, tile.Inset(tile.Width * 0.14f));
                p.TextLeft(PlaytestText.T(key), line.Left + p.U(150f), line.CenterY, T.ButtonSecondary, C.InkBrown, line.Width * 0.42f, 0.86f, TextLook.Plain(C.InkBrown));
                p.Text(PlaytestText.T("store.unavailable"), line.Right - p.U(120f), line.CenterY, T.Caption, C.InkBrownSoft);
                p.PopAlpha();
            }
        }

        /// <summary>
        /// A Shop row's item tile at its left end: the booster tile's cream squircle (§3.7), 0.9 of the row tall and at most
        /// 0.12 of the screen wide. Returns its box.
        /// </summary>
        private static Box ItemTile(IPainter p, Box line)
        {
            float size = Math.Min(line.Height * 0.9f, Math.Min(p.Width, p.Height) * 0.12f);
            Box box = Box.FromCenter(line.Left + p.U(22f) + (size / 2f), line.CenterY - (line.Height * 0.02f), size, size);
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
            var pill = new Box(line.Right - p.U(22f) - w, line.CenterY - (h / 2f), line.Right - p.U(22f), line.CenterY + (h / 2f));
            p.PushAlpha(buy != null ? 1f : 0.45f);
            Kit.CostPill(p, pill, Cost.Petals(price));
            p.PopAlpha();
            if (buy != null)
            {
                p.Hit(Kit.Touch(p, line), buy);
            }
        }

        /// <summary>
        /// The Store's cosmetics in the reference Wardrobe's look (spec 005 §4.6): the four family tabs with their heroes,
        /// the outfit cards of the chosen family (the "Default" look, worn while the family wears nothing, then each item
        /// for sale shown on the family's hero with its cost pill; a tap buys), six to a page, and the footer line with the
        /// page arrows.
        /// </summary>
        private static void Outfits(IPainter p, DesignApp app, Box column)
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

            var tabs = new Box(column.Left, column.Top + p.U(6f), column.Right, column.Top + p.U(206f));
            var panel = new Box(column.Left, tabs.Bottom, column.Right, column.Bottom);
            Box content = Kit.FamilyTabs(p, tabs, panel, families, names, selected, i =>
            {
                app.StoreFamily = i;
                app.StorePage = 0;
            }, f => wardrobe.OutfitOf(f));

            var items = new List<CosmeticItem?> { null };
            items.AddRange(wardrobe.ForSale);
            const int columns = 3;
            const int perPage = 6;
            int pages = Math.Max(1, (items.Count + perPage - 1) / perPage);
            int page = Math.Max(0, Math.Min(pages - 1, app.StorePage));
            float footer = p.U(DesignTokens.Size.TouchMin);
            float gap = p.U(22f);
            var grid = new Box(content.Left, content.Top, content.Right, content.Bottom - footer - p.U(8f));
            float cardWidth = (grid.Width - (gap * (columns - 1))) / columns;
            float cardHeight = (grid.Height - gap) / 2f;
            Outfit worn = wardrobe.OutfitOf(family);
            for (int slot = 0; slot < perPage; slot++)
            {
                int index = (page * perPage) + slot;
                if (index >= items.Count)
                {
                    break;
                }

                float x = grid.Left + ((slot % columns) * (cardWidth + gap));
                float y = grid.Top + ((slot / columns) * (cardHeight + gap));
                var box = new Box(x, y, x + cardWidth, y + cardHeight);
                CosmeticItem? item = items[index];
                if (item == null)
                {
                    Kit.OutfitCard(p, box, PlaytestText.T("wardrobe.default"), worn.IsEmpty, well => Preview(p, well, family, null), pillRoom: true);
                    continue;
                }

                string id = item.Id;
                Kit.OutfitCard(p, box, ItemName(item), false, well => Preview(p, well, family, item), Cost.Petals(item.Price), () =>
                {
                    if (!wardrobe.TryBuy(id))
                    {
                        app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                    }
                });
            }

            var line = new Box(content.Left, content.Bottom - footer, content.Right, content.Bottom);
            float arrow = p.U(100f);
            float room = pages > 1 ? footer + p.U(12f) : 0f;
            p.Text(PlaytestText.T("wardrobe.footer"), line.CenterX, line.CenterY, T.Body, C.InkBrownSoft, line.Width - (2f * room));
            if (pages > 1)
            {
                Kit.ArrowButton(p, line.Left + (footer / 2f), line.CenterY, arrow, next: false, page > 0 ? () => app.StorePage = page - 1 : (Action?)null);
                Kit.ArrowButton(p, line.Right - (footer / 2f), line.CenterY, arrow, next: true, page < pages - 1 ? () => app.StorePage = page + 1 : (Action?)null);
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

        /// <summary>An item's player-facing name (<c>cosmetic.{id}</c> in the string table), else its catalog name.</summary>
        public static string ItemName(CosmeticItem item) => PlaytestText.Has("cosmetic." + item.Id) ? PlaytestText.T("cosmetic." + item.Id) : item.Name;
    }
}
