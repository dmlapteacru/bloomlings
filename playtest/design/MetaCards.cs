using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The meta card of frame 4 (spec 002 US3, FR-022) in the reference look (spec 005 §4.3, §4.6): the Daily Reward, a
    /// parchment card under a wooden sign header with the lotus heap, the reward pill and green and cream buttons; the
    /// Remove Ads card of Home's No Ads scene (spec 005 FR-033, preview frame 32); and an item's player-facing name. The
    /// Store, the Leaderboard and the Collection are pages of their own since the owner's notes of 2026-10-04
    /// (<see cref="StoreScreen"/>, <see cref="LeaderboardScreen"/>, <see cref="CollectionScreen"/>).
    /// Ads are unavailable in the playtest.
    /// </summary>
    public static class MetaCards
    {
        /// <summary>The Daily Reward popup: Day N, the lotus heap in its basket and light, the "+N" pill, Claim, and "Get +N" (ad).</summary>
        public static void DailyReward(IPainter p, DesignApp app, float since)
        {
            var daily = app.Meta.DailyReward;
            CardRegions r = Kit.Card(p, 60f + 330f + 110f + DesignTokens.Size.CardPrimaryHeight + DesignTokens.Size.SecondaryHeight + 90f, PlaytestText.T("daily_reward.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.None);
            float y = r.Body.Top;
            // Opened from Home's Daily scene after today's claim, it shows the day just claimed, Claim greyed (as Unity's).
            p.Text(PlaytestText.F("daily_reward.day", daily.TodayStreak), r.Body.CenterX, y + p.U(20f), T.Body, C.InkBrownSoft);
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

            RewardPill(p, Box.FromCenter(r.Body.CenterX, y + p.U(50f), p.U(250f), p.U(92f)), NumberText.Plus(daily.PetalsOn(daily.TodayStreak)));
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

        /// <summary>
        /// The Remove Ads card (spec 005 FR-033), opened from Home's No Ads scene at every level (the Store keeps its own No
        /// Ads row): the title, the No Ads scene idling (<see cref="HomeScreen.Promo"/>), what Remove Ads does, the purchase
        /// (purchases are off in the playtest, so the green button is disabled and reads "Unavailable" over the offline
        /// line, as the Store's money rows), Restore Purchases (which says purchases are offline: the playtest restores
        /// nothing) and the close. Its toasts show under the card, over the scrim.
        /// </summary>
        public static void RemoveAds(IPainter p, DesignApp app, float since)
        {
            const float sceneUnits = 420f;
            const float lineUnits = 58f;
            const float captionUnits = 56f;
            float bodyWidth = ScreenLayout.Card(p.Width, p.Height, p.Insets, 0f).Body.Width;
            List<string> lines = EndCards.Lines(p, PlaytestText.T("remove_ads.body"), T.Body, bodyWidth * 0.92f, 3);
            float sceneHeight = sceneUnits * HomePromo.HeightShare;
            float content = 10f + sceneHeight + 14f + (lines.Count * lineUnits) + 36f + DesignTokens.Size.CardPrimaryHeight + captionUnits + 20f + DesignTokens.Size.SecondaryHeight + 40f;
            CardRegions r = Kit.Card(p, content, PlaytestText.T("remove_ads.title"), app.CardClose, Kit.Pop(since), T.Title);
            float y = r.Body.Top + p.U(10f);

            // The No Ads scene at its idle pose, small and centered.
            float scene = Math.Min(p.U(sceneUnits), r.Body.Width);
            HomeScreen.Promo(p, PromoScene.NoAds, HomePromo.SceneBox(r.Body.CenterX - (scene / 2f), y, scene), app.PromoSeconds, calling: false);
            y += (scene * HomePromo.HeightShare) + p.U(14f);

            foreach (string line in lines)
            {
                p.Text(line, r.Body.CenterX, y + p.U(lineUnits / 2f), T.Body, C.InkBrown, r.Body.Width, look: TextLook.Plain(C.InkBrown));
                y += p.U(lineUnits);
            }

            // The purchase, off in the playtest: the disabled green button and the offline line under it.
            Box buy = ScreenLayout.CardButton(r.Body, y + p.U(36f), true, p.Scale);
            Kit.PrimaryButton(p, buy, PlaytestText.T("store.unavailable"), null, decorate: true);
            p.Text(PlaytestText.T("store.offline"), r.Body.CenterX, buy.Bottom + p.U(captionUnits / 2f), T.Caption, C.InkBrownSoft, r.Body.Width);
            Box restore = ScreenLayout.CardButton(r.Body, buy.Bottom + p.U(captionUnits + 20f), false, p.Scale).Inset(p.U(40f), 0f);
            Kit.SecondaryButton(p, restore, PlaytestText.T("remove_ads.restore"), () => app.HomeToast(PlaytestText.T("store.offline")));
            Kit.EndCard(p);

            // Home's toast lies under the scrim while this card is open: it shows under the card instead.
            string? toast = app.HomeToastText;
            if (toast != null && !app.DrawingCovered)
            {
                Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
                Kit.Toast(p, new Box(safe.Left, r.Card.Bottom, safe.Right, Math.Min(safe.Bottom, r.Card.Bottom + p.U(140f))), toast);
            }
        }

        /// <summary>
        /// The purchase confirmation (spec 005 FR-040; preview frame 49) over whatever asked (<see cref="DesignApp.ConfirmPurchase"/>):
        /// the item's picture, name and price, the Petals balance under the price, Buy (the purchase runs only now) and
        /// Cancel (nothing is bought). It has no close: Cancel and the system back close it.
        /// </summary>
        public static void Purchase(IPainter p, DesignApp app, float since)
        {
            PurchaseOffer? offer = app.Purchase.Offer;
            if (offer == null)
            {
                return;
            }

            string note = offer.RealMoney ? PlaytestText.T("purchase.store_next") : PlaytestText.F("purchase.balance", NumberText.Group(app.Meta.Economy.Petals));
            Kit.PurchaseCard(p, offer, app.PurchasePicture, note, app.BuyConfirmed, app.CloseOverlay, Kit.Pop(since));
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

        /// <summary>An item's player-facing name (<c>cosmetic.{id}</c> in the string table), else its catalog name.</summary>
        public static string ItemName(CosmeticItem item) => PlaytestText.Has("cosmetic." + item.Id) ? PlaytestText.T("cosmetic." + item.Id) : item.Name;
    }
}
