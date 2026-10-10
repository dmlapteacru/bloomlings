using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The meta card of frame 4 (spec 002 US3, FR-022) in the reference look (spec 005 §4.3, §4.6): the Daily Reward, the
    /// popups' card with the lotus heap and the day's five steps (spec 005 FR-050); the
    /// Remove Ads card of Home's No Ads scene (spec 005 FR-033, preview frame 32); and an item's player-facing name. The
    /// Store, the Leaderboard and the Collection are pages of their own since the owner's notes of 2026-10-04
    /// (<see cref="StoreScreen"/>, <see cref="LeaderboardScreen"/>, <see cref="CollectionScreen"/>).
    /// Ads are unavailable in the playtest (the Daily Reward's ad steps take their stand-in).
    /// </summary>
    public static class MetaCards
    {
        /// <summary>
        /// The Daily Reward popup (spec 001 FR-055 as amended on 2026-10-09; spec 005 FR-050, <see cref="DailyRewardCard"/>):
        /// the lotus heap in its basket and light, then the day's five steps as raised rows, each with its number, the lotus
        /// and "+N", and at its right its Claim (an ad step's with the ad mark): green and breathing on the next step, greyed
        /// and not active on a later one; a claimed step's check; and under them when the steps come again. A claim keeps the card open:
        /// the check pops in, "+N" rises from the row and the Petals pill counts up. The playtest has no ads: an ad step takes
        /// the ad's stand-in (<see cref="DailyRewardService.AdStub"/>), else its Claim is disabled.
        /// </summary>
        public static void DailyReward(IPainter p, DesignApp app, float since)
        {
            DailyRewardService daily = app.Meta.DailyReward;
            IReadOnlyList<DailyRewardStep> steps = daily.Steps;
            CardRegions r = Kit.Card(p, DailyRewardCard.ContentUnits(steps.Count), PlaytestText.T("daily_reward.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.None);
            DailyRewardRegions d = DailyRewardCard.Layout(r.Body, p.U(1f), steps.Count);

            // The reward: a heap of lotuses over a woven basket, in the win's soft turning light.
            p.Mark("currency.petal_pile");
            Kit.LightRays(p, d.Art.CenterX, d.Art.CenterY, d.Art.Width * 0.62f, since);
            foreach ((float hx, float hy, float hs) in DailyRewardCard.Heap)
            {
                float size = d.Art.Width * hs;
                Kit.Petal(p, Box.FromCenter(d.Art.CenterX + (hx * d.Art.Width), d.Art.CenterY + (hy * d.Art.Height), size, size));
            }

            Basket(p, d.Basket);

            (int claimedStep, float claimedSince) = app.DailyClaim;
            for (int i = 0; i < steps.Count; i++)
            {
                DailyRewardStep step = steps[i];
                float sinceClaim = step.Number == claimedStep ? claimedSince : float.MaxValue;
                Step(p, app, DailyRewardCard.Row(d.Rows[i]), step, daily.StateOf(step.Number), sinceClaim);
            }

            (string key, object[] args) = DailyRewardCard.Duration(daily.MinutesToNextDay);
            string caption = PlaytestText.F(DailyRewardCard.CaptionKey(daily.CanClaim), PlaytestText.F(key, args));
            p.Text(caption, d.Caption.CenterX, d.Caption.CenterY, T.Caption, C.InkBrownSoft, d.Caption.Width);
            Kit.EndCard(p);
        }

        /// <summary>
        /// A Daily Reward step's row (<c>ui.daily.step</c>): the raised slab, the number, the lotus and "+N", and its Claim
        /// (green and breathing on the next step, greyed and not active on a later one) or, once claimed, its check (popping
        /// in after the claim). After a claim, "+N" rises from its claim.
        /// </summary>
        private static void Step(IPainter p, DesignApp app, DailyRowParts row, DailyRewardStep step, DailyStepState state, float sinceClaim)
        {
            p.Mark("ui.daily.step");
            float h = row.Row.Height;
            Kit.RaisedRow(p, row.Row, h * 0.3f);
            float lift = h * Kit.RaisedRowSide / 2f;
            string number = step.Number.ToString(CultureInfo.InvariantCulture);
            p.Text(number, row.Number.CenterX, row.Number.CenterY - lift, T.Count, C.InkBrownSoft, row.Number.Width, h * 0.42f / p.U(T.Count.Size), TextLook.Plain(C.InkBrownSoft));
            Kit.Petal(p, row.Lotus.Offset(0f, -lift));
            string amount = NumberText.Plus(step.Petals);
            p.TextLeft(amount, row.Amount.Left, row.Amount.CenterY - lift, T.Reward, C.InkBrown, row.Amount.Width, h * 0.46f / p.U(T.Reward.Size), TextLook.Plain(C.InkBrown));

            if (state == DailyStepState.Claimed)
            {
                Box badge = DailyRewardCard.Badge(row.Button.Offset(0f, -lift));
                Kit.CheckBadge(p, badge.CenterX, badge.CenterY, badge.Width * DailyRewardCard.CheckPop(sinceClaim));
            }
            else
            {
                bool payable = state == DailyStepState.Ready && (!step.Ad || DailyRewardService.AdStub);
                StepButton(p, row.Button, step.Ad, payable ? () => app.ClaimDailyStep(step) : (Action?)null, breathe: state == DailyStepState.Ready);
            }

            // The claimed "+N" rising from where it was claimed.
            if (DailyRewardCard.Rise(sinceClaim) is (float rise, float alpha))
            {
                p.PushAlpha(alpha);
                ColorSet green = GardenLook.Green;
                p.Text(amount, row.Button.CenterX, row.Button.CenterY - (rise * h), T.Reward, C.TextOnColor, row.Button.Width * 1.4f, h * 0.5f / p.U(T.Reward.Size), TextLook.OnGloss(green));
                p.PopAlpha();
            }
        }

        /// <summary>
        /// A step's Claim: the green face raised on its wooden plate (<see cref="Kit.RaisedButton"/>, as every primary
        /// button since spec 005 FR-045), "Claim" in the buttons' white letters (<see cref="DailyRewardCard.ClaimTextShare"/>
        /// of the face), an ad step's after the clapperboard (<see cref="GardenLook.AdMark"/>, <see cref="DailyRewardCard.ClaimIconShare"/>); breathing while it
        /// waits; greyed without <paramref name="action"/> (a later step, or an ad step without its ad).
        /// </summary>
        private static void StepButton(IPainter p, Box box, bool ad, Action? action, bool breathe)
        {
            p.Mark("ui.button.primary");
            bool enabled = action != null;
            ColorSet colors = enabled ? GardenLook.Green : GardenLook.Green.Disabled();
            float depth = Kit.Press(p, box, enabled);
            bool breathing = breathe && enabled && depth == 0f;
            if (breathing)
            {
                p.PushTransform(0f, 0f, GardenLook.Breathe(p.Now), box.CenterX, box.CenterY);
            }

            Kit.Squash(p, box, depth);
            Box f = Kit.RaisedButton(p, box, colors, 0.5f, depth, gloss: enabled);
            string label = PlaytestText.T("daily_reward.claim");
            TypeStyle s = T.Button;
            float scale = f.Height * DailyRewardCard.ClaimTextShare / p.U(s.Size);
            TextLook look = TextLook.OnGloss(colors);
            if (ad)
            {
                float icon = f.Height * DailyRewardCard.ClaimIconShare;
                float gap = f.Height * DailyRewardCard.ClaimGapShare;
                float textWidth = Math.Min(p.MeasureText(label, s, scale), f.Width - icon - gap - (f.Height * 0.4f));
                float start = f.CenterX - ((icon + gap + textWidth) / 2f);
                Kit.AdMark(p, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon), enabled);
                p.Text(label, start + icon + gap + (textWidth / 2f), f.CenterY, s, C.TextOnColor, textWidth, scale, look);
            }
            else
            {
                p.Text(label, f.CenterX, f.CenterY, s, C.TextOnColor, f.Width - (f.Height * 0.5f), scale, look);
            }

            p.PopTransform();
            if (breathing)
            {
                p.PopTransform();
            }

            if (action != null)
            {
                p.Hit(Kit.Touch(p, box), action);
            }
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
            float content = 10f + sceneHeight + 14f + (lines.Count * lineUnits) + 36f + DesignTokens.Size.CardPrimaryHeight + captionUnits + 20f + DesignTokens.Size.CardSecondaryHeight + 40f;
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
            Kit.PrimaryButton(p, buy, PlaytestText.T("store.unavailable"), null);
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

        /// <summary>An item's player-facing name (<c>cosmetic.{id}</c> in the string table), else its catalog name.</summary>
        public static string ItemName(CosmeticItem item) => PlaytestText.Has("cosmetic." + item.Id) ? PlaytestText.T("cosmetic." + item.Id) : item.Name;
    }
}
