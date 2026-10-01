using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The meta cards of frames 4, 5, 6 and 17 (spec 002 US3, FR-022 to FR-025): the Daily Reward, the Leaderboard
    /// (offline, since the server is deferred), the Collection and the Store. Real money and ads are unavailable in
    /// the playtest.
    /// </summary>
    public static class MetaCards
    {
        /// <summary>The Daily Reward popup: Day N, the reward basket, +N with the Petal symbol, CLAIM, and "Get +N" (ad).</summary>
        public static void DailyReward(IPainter p, DesignApp app, float since)
        {
            var daily = app.Meta.DailyReward;
            CardRegions r = Kit.Card(p, 60f + 330f + 110f + DesignTokens.Size.CardPrimaryHeight + DesignTokens.Size.SecondaryHeight + 90f, PlaytestText.T("daily_reward.title"), app.CloseOverlay, Kit.Pop(since));
            float y = r.Body.Top;
            p.Text(PlaytestText.F("daily_reward.day", daily.NextStreak), r.Body.CenterX, y + p.U(20f), T.Body, C.TextSecondary);
            y += p.U(60f);

            // The reward basket: a heap of Petals over a woven basket, in a sunburst.
            p.Mark("currency.petal_pile");
            var art = new Box(r.Body.CenterX - p.U(220f), y, r.Body.CenterX + p.U(220f), y + p.U(330f));
            for (int i = 0; i < 10; i++)
            {
                double a = (Math.PI * 2 * i / 10) + (since * 0.3);
                p.Line(art.CenterX, art.CenterY, art.CenterX + (float)(Math.Cos(a) * art.Width * 0.55), art.CenterY + (float)(Math.Sin(a) * art.Width * 0.55), p.U(40f), C.PetalCenter.WithAlpha(0.18f));
            }

            (float X, float Y, float S)[] heap = { (-0.22f, 0.02f, 0.3f), (0.2f, 0.0f, 0.32f), (0f, -0.12f, 0.34f), (-0.08f, 0.1f, 0.28f), (0.12f, 0.12f, 0.26f) };
            foreach ((float hx, float hy, float hs) in heap)
            {
                float size = art.Width * hs;
                Kit.Petal(p, Box.FromCenter(art.CenterX + (hx * art.Width), art.CenterY + (hy * art.Height), size, size));
            }

            p.Shape("currency.reward_basket", Box.FromCenter(art.CenterX, art.CenterY + (art.Height * 0.2f), art.Width * 0.8f, art.Width * 0.6f), C.RewardBasket);
            y = art.Bottom + p.U(10f);

            string amount = NumberText.Plus(daily.NextPetals);
            float w = p.MeasureText(amount, T.Reward);
            p.Text(amount, r.Body.CenterX - p.U(40f), y + p.U(50f), T.Reward, C.GardenLabelPlain);
            Kit.Petal(p, Box.FromCenter(r.Body.CenterX - p.U(40f) + (w / 2f) + p.U(50f), y + p.U(50f), p.U(76f), p.U(76f)));
            y += p.U(110f);

            // CLAIM breathes while it waits, and the claim bursts sparkles over the Petals pill (spec 003 FR-019, FR-020).
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
            p.Text("no ads in the playtest", r.Body.CenterX, bonus.Bottom + p.U(34f), T.Caption, C.TextSecondary);
            Kit.EndCard(p);
        }

        /// <summary>
        /// The Leaderboard in its offline form: the top ranks with medals as placeholder rows (no invented players), the
        /// gap, and the player's own row highlighted as "You", with the offline notice (research R12).
        /// </summary>
        public static void Leaderboard(IPainter p, DesignApp app, float since)
        {
            const float row = 96f;
            CardRegions r = Kit.Card(p, (8f * (row + 14f)) + 110f, PlaytestText.T("leaderboard.title"), app.CloseOverlay, Kit.Pop(since));
            Box[] rows = ScreenLayout.Column(r.Body, 8, p.U(row), p.U(14f));
            for (int i = 0; i < 5; i++)
            {
                Box line = rows[i];
                Kit.Row(p, line, false);
                Rank(p, line, i + 1);
                p.Shape("ui.person", Box.FromCenter(line.Left + p.U(170f), line.CenterY, p.U(64f), p.U(64f)), C.StateStuck.Lighten(0.3f));
                p.FillRound(Box.FromCenter(line.Left + p.U(330f), line.CenterY, p.U(180f), p.U(26f)), p.U(13f), C.SurfaceSunk);
                p.FillRound(Box.FromCenter(line.Right - p.U(90f), line.CenterY, p.U(100f), p.U(26f)), p.U(13f), C.SurfaceSunk);
            }

            p.Text("…", rows[5].CenterX, rows[5].CenterY, T.Title, C.TextSecondary);

            Box you = rows[6];
            Kit.Row(p, you, highlighted: true);
            p.Text("—", you.Left + p.U(70f), you.CenterY, T.Body, C.GardenLabelPlain);
            p.FillCircle(you.Left + p.U(170f), you.CenterY, p.U(36f), Rgba.White);
            Visuals.Bloomling(p, Box.FromCenter(you.Left + p.U(170f), you.CenterY, p.U(60f), p.U(60f)), Core.Variants.Family.Sprig, Visuals.ColorOf(Core.Variants.VariantId.Leaf), null);
            p.TextLeft(PlaytestText.T("leaderboard.you"), you.Left + p.U(230f), you.CenterY, T.Body, C.GardenLabelPlain);
            p.Text(NumberText.Group(app.Meta.Progression.HighestCompletedLevel), you.Right - p.U(90f), you.CenterY, T.Count, C.GardenLabelPlain);

            p.Text(PlaytestText.T("leaderboard.offline_empty"), r.Body.CenterX, rows[7].CenterY + p.U(30f), T.Caption, C.TextSecondary, r.Body.Width);
            Kit.EndCard(p);
        }

        private static void Rank(IPainter p, Box line, int rank)
        {
            float cx = line.Left + p.U(70f);
            Rgba? medal = C.Medal(rank);
            if (medal.HasValue)
            {
                p.Shape("ui.medal", Box.FromCenter(cx, line.CenterY, p.U(70f), p.U(70f)), medal.Value);
                p.Text(rank.ToString(System.Globalization.CultureInfo.InvariantCulture), cx, line.CenterY + p.U(8f), T.Badge, medal.Value.Darken(0.45f));
            }
            else
            {
                p.Text(rank.ToString(System.Globalization.CultureInfo.InvariantCulture), cx, line.CenterY, T.Body, C.GardenLabelPlain);
            }
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
            CardRegions r = Kit.Card(p, (rows * 250f) + 80f, PlaytestText.T("collection.title"), app.CloseOverlay, Kit.Pop(since));
            string count = entries.Count == 1 ? PlaytestText.F("collection.count_one", 1) : PlaytestText.F("collection.count_many", entries.Count);
            p.Text(count, r.Body.CenterX, r.Body.Top + p.U(22f), T.Caption, C.TextSecondary);
            float cell = Math.Min((r.Body.Width - p.U(40f)) / columns, p.U(250f));
            float x0 = r.Body.CenterX - (cell * columns / 2f);
            int start = Math.Max(0, entries.Count - (rows * columns));
            for (int i = start; i < entries.Count; i++)
            {
                int n = i - start;
                var box = new Box(x0 + ((n % columns) * cell), r.Body.Top + p.U(60f) + ((n / columns) * cell), x0 + ((n % columns) * cell) + cell, r.Body.Top + p.U(60f) + ((n / columns) * cell) + cell).Inset(p.U(12f));
                Frame(p, box, entries[i], app);
                int index = i;
                p.Hit(box, () => app.CollectionDetail = index);
            }

            Kit.EndCard(p);
        }

        private static void Detail(IPainter p, DesignApp app, CollectionEntry entry, float since)
        {
            p.Mark("collection.detail_frame");
            CardRegions r = Kit.Card(p, 700f, PlaytestText.T("collection.title"), () => app.CollectionDetail = -1, Kit.Pop(since));
            var frame = new Box(r.Body.Left + p.U(60f), r.Body.Top, r.Body.Right - p.U(60f), r.Body.Top + p.U(540f));
            Frame(p, frame, entry, app);
            p.Text(entry.PictureId.Replace('_', ' '), r.Body.CenterX, frame.Bottom + p.U(50f), T.Title, C.GardenLabelPlain, r.Body.Width);
            p.Text(PlaytestText.F("collection.completed", NumberText.Group(entry.LevelNumber)), r.Body.CenterX, frame.Bottom + p.U(110f), T.Caption, C.TextSecondary);
            Kit.EndCard(p);
        }

        private static void Frame(IPainter p, Box box, CollectionEntry entry, DesignApp app)
        {
            p.Mark("collection.frame");
            p.FillRound(box.Offset(0f, p.U(6f)), box.Width * 0.12f, C.SurfacePanelEdge.Darken(0.1f));
            p.FillRound(box, box.Width * 0.12f, Rgba.FromHex("#F3E3C3"));
            p.FillRound(box.Inset(box.Width * 0.06f), box.Width * 0.08f, Rgba.White);
            if (app.Content.TryGetLevel(app.Resolve(entry.LevelNumber), out LevelDefinition? definition) && definition != null)
            {
                BoardPainter.Picture(p, box.Inset(box.Width * 0.12f), definition, app.Content.GetPicture(definition.Picture));
            }
        }

        /// <summary>
        /// The Store: the Petals balance pill, and rows of icon, name and price. Boosters are bought with Petals;
        /// real-money rows are unavailable in the playtest. Cosmetics join after L40 (spec 001).
        /// </summary>
        public static void Store(IPainter p, DesignApp app, float since)
        {
            const float row = 118f;
            bool cosmetics = app.Meta.Wardrobe.IsAvailable;
            CardRegions r = Kit.Card(p, 130f + (cosmetics ? 116f : 0f) + (7f * (row + 16f)), PlaytestText.T("store.title"), app.CloseOverlay, Kit.Pop(since));
            float y = r.Body.Top;
            Kit.PetalsPill(p, Box.FromCenter(r.Body.CenterX, y + p.U(40f), p.U(360f), p.U(84f)), app.Meta.Economy.Petals, () => app.HomeToast(PlaytestText.T("store.offline")));
            y += p.U(130f);
            if (cosmetics)
            {
                Kit.Tabs(p, new Box(r.Body.Left + p.U(40f), y, r.Body.Right - p.U(40f), y + p.U(84f)), new[] { PlaytestText.T("store.tab_shop"), PlaytestText.T("store.tab_cosmetics") }, app.StoreTab, i => app.StoreTab = i);
                y += p.U(116f);
            }

            var column = new Box(r.Body.Left, y, r.Body.Right, r.Body.Bottom);
            if (cosmetics && app.StoreTab == 1)
            {
                CosmeticRows(p, app, column, row);
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
                Rgba color = DesignTokens.BoosterColor(id);
                p.PushAlpha(unlocked ? 1f : 0.45f);
                p.FillCircle(line.Left + p.U(60f), line.CenterY, p.U(38f), color);
                p.Shape("booster." + id, Box.FromCenter(line.Left + p.U(60f), line.CenterY, p.U(44f), p.U(44f)), id == "bloom_burst" ? C.PetalCenter : Rgba.White);
                string name = EndCards.BoosterName(kind) + " · ×" + app.Meta.Economy.Charges(kind);
                p.TextLeft(name, line.Left + p.U(120f), line.CenterY, T.Body, C.GardenLabelPlain, line.Width * 0.5f);
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
                Kit.Petal(p, Box.FromCenter(line.Left + p.U(60f), line.CenterY, p.U(64f), p.U(64f)));
                p.TextLeft(PlaytestText.T(key), line.Left + p.U(120f), line.CenterY, T.Body, C.GardenLabelPlain, line.Width * 0.5f);
                p.Text(PlaytestText.T("store.unavailable"), line.Right - p.U(120f), line.CenterY, T.Caption, C.TextSecondary);
                p.PopAlpha();
            }
        }

        private static void CosmeticRows(IPainter p, DesignApp app, Box column, float row)
        {
            IReadOnlyList<CosmeticItem> items = app.Meta.Wardrobe.ForSale;
            if (items.Count == 0)
            {
                p.Text(PlaytestText.T("store.all_owned"), column.CenterX, column.Top + p.U(60f), T.Body, C.TextSecondary);
                return;
            }

            Box[] rows = ScreenLayout.Column(column, Math.Min(7, items.Count), p.U(row), p.U(16f));
            for (int i = 0; i < rows.Length; i++)
            {
                CosmeticItem item = items[i];
                Box line = rows[i];
                Kit.Row(p, line, false);
                string shape = ShapeLibrary.CosmeticId(item.Shape);
                p.Shape(ShapeLibrary.Has(shape) ? shape : "ui.star", Box.FromCenter(line.Left + p.U(60f), line.CenterY, p.U(70f), p.U(70f)), item.Tint.StartsWith("#", StringComparison.Ordinal) ? Rgba.FromHex(item.Tint) : C.MedalGold);
                p.TextLeft(item.Name, line.Left + p.U(120f), line.CenterY, T.Body, C.GardenLabelPlain, line.Width * 0.5f);
                string id = item.Id;
                Price(p, line, item.Price, () =>
                {
                    if (!app.Meta.Wardrobe.TryBuy(id))
                    {
                        app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                    }
                });
            }
        }

        /// <summary>A price with the Petal symbol at a row's right end; a tap buys.</summary>
        private static void Price(IPainter p, Box line, int price, Action? buy)
        {
            string text = NumberText.Group(price);
            float w = p.MeasureText(text, T.Count);
            var pill = new Box(line.Right - w - p.U(110f), line.CenterY - p.U(34f), line.Right - p.U(18f), line.CenterY + p.U(34f));
            p.PushAlpha(buy != null ? 1f : 0.45f);
            p.FillRound(pill, pill.Height / 2f, C.SurfaceSunk);
            p.Text(text, pill.Left + p.U(22f) + (w / 2f), pill.CenterY, T.Count, C.GardenLabelPlain);
            Kit.Petal(p, Box.FromCenter(pill.Right - p.U(38f), pill.CenterY, p.U(48f), p.U(48f)));
            p.PopAlpha();
            if (buy != null)
            {
                p.Hit(Kit.Touch(p, line), buy);
            }
        }
    }
}
