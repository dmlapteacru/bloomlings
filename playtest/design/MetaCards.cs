using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.DailyChallenge;
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
        /// <summary>Five display stations and one next-step action; state and UTC reset come from the reward service.</summary>
        public static void DailyReward(IPainter p, DesignApp app, float since)
        {
            DailyRewardService daily = app.Meta.DailyReward;
            IReadOnlyList<DailyRewardStep> steps = daily.Steps;
            CardRegions r = Kit.Card(p, DailyRewardCard.ContentUnits(steps.Count), PlaytestText.T("daily_reward.title"), app.CardClose, Kit.Pop(since), sign: SignDecor.None);
            DailyRewardRegions d = DailyRewardCard.Layout(r.Body, p.U(1f), steps.Count);

            Kit.NavigationBar(p,d.Shelf,0);
            (int claimedStep,float claimedSince)=app.DailyClaim;
            for(int i=0;i<steps.Count;i++)
            {
                DailyRewardStep step=steps[i];DailyStepState state=daily.StateOf(step.Number);DailyRowParts parts=DailyRewardCard.Row(d.Rows[i]);
                p.Mark("ui.daily.step");Kit.RaisedRow(p,parts.Row,parts.Row.Width*.2f);
                if(state==DailyStepState.Ready) p.StrokeRound(parts.Row.Inset(p.U(2f)),parts.Row.Width*.2f,p.U(5f),GardenLook.Green.Top);
                p.Text(step.Number.ToString(CultureInfo.InvariantCulture),parts.Number.CenterX,parts.Number.CenterY,PopupDesign.Label,C.InkBrownSoft,parts.Number.Width,sizeScale:.7f,look:TextLook.Plain(C.InkBrownSoft));
                Kit.Petal(p,parts.Lotus);p.Text(NumberText.Plus(step.Petals),parts.Amount.CenterX,parts.Amount.CenterY,PopupDesign.Label,C.InkBrown,parts.Amount.Width,sizeScale:.85f,look:TextLook.Plain(C.InkBrown));
                Box status=parts.Button;
                if(state==DailyStepState.Claimed) Kit.CheckBadge(p,status.CenterX,status.CenterY,Math.Min(status.Width,status.Height)*DailyRewardCard.CheckPop(step.Number==claimedStep?claimedSince:10));
                else if(state==DailyStepState.Locked) Kit.RaisedGlyph(p,"ui.lock",status.Inset(status.Width*.12f),C.InkBrownSoft);
                else if(step.Ad) Kit.AdMark(p,status);
                if(step.Ad && state!=DailyStepState.Ready) Kit.AdMark(p,Box.FromCenter(parts.Row.Right-parts.Row.Width*.16f,parts.Row.Top+parts.Row.Height*.12f,parts.Row.Width*.26f,parts.Row.Width*.26f));
            }
            if(daily.CanClaim && daily.NextStep is DailyRewardStep next)
                Kit.PrimaryButton(p,d.Claim,PlaytestText.T(next.Ad?"daily_reward.watch_claim":"daily_reward.claim"),!next.Ad||DailyRewardService.AdStub ? ()=>app.ClaimDailyStep(next) : (Action?)null,iconId:next.Ad?"ui.ad":null);
            (string key,object[] args)=DailyRewardCard.Duration(daily.MinutesToNextDay);
            p.Text(PlaytestText.F(DailyRewardCard.CaptionKey(daily.CanClaim),PlaytestText.F(key,args)),d.Caption.CenterX,d.Caption.CenterY,T.Caption,C.InkBrownSoft,d.Caption.Width);
            Kit.EndCard(p);
        }

        /// <summary>The separate Daily Challenge window. Puzzle content remains native-client only.</summary>
        public static void DailyChallenge(IPainter p,DesignApp app,float since, bool completed = false)
        {
            const float art=260,line=56;
            float content=10+art+24+2*line+22+72+26+DesignTokens.Size.CardPrimaryHeight+40;
            CardRegions r=Kit.Card(p,content,PlaytestText.T("daily.title"),app.CardClose,Kit.Pop(since),sign:SignDecor.None);
            float y=r.Body.Top+p.U(10);
            HomeScreen.ChallengePicture(p,Box.FromCenter(r.Body.CenterX,y+p.U(art/2),p.U(art),p.U(art)));
            if(completed)Kit.CheckBadge(p,r.Body.CenterX+p.U(art*.36f),y+p.U(art*.14f),p.U(art*.25f));
            y+=p.U(art+24);
            var lines=EndCards.Lines(p,PlaytestText.T(completed ? "daily.done" : "daily.description"),T.Body,r.Body.Width*.94f,2);
            for(int i=0;i<2;i++){if(i<lines.Count)p.Text(lines[i],r.Body.CenterX,y+p.U(line/2),T.Body,C.InkBrown,r.Body.Width*.94f);y+=p.U(line);}
            y+=p.U(22);
            Box reward=Box.FromCenter(r.Body.CenterX,y+p.U(36),p.U(340),p.U(72));
            Kit.Petal(p,new Box(reward.Left,reward.Top,reward.Left+p.U(72),reward.Bottom));
            p.Text(completed ? PlaytestText.T("daily.reward_collected") : PlaytestText.F("daily.reward",DailyChallengeRules.RewardPetals),reward.CenterX+p.U(40),reward.CenterY,PopupDesign.Label,C.InkBrown,reward.Width-p.U(80),look:TextLook.Plain(C.InkBrown));
            y+=p.U(72+26);
            Kit.PrimaryButton(p,ScreenLayout.CardButton(r.Body,y,true,p.Scale),PlaytestText.T(completed ? "daily.play_again" : "common.play"),()=>{app.CloseOverlay();app.HomeToast("Daily Challenge: not in the playtest yet");});
            Kit.EndCard(p);
        }

        public static void RemoveAds(IPainter p, DesignApp app, float since)
        {
            const float sceneUnits = 420f;
            const float lineUnits = 58f;
            const float captionUnits = 56f;
            float bodyWidth = ScreenLayout.Card(p.Width, p.Height, p.Insets, 0f).Body.Width;
            List<string> lines = EndCards.Lines(p, PlaytestText.T("remove_ads.body"), T.Body, bodyWidth * 0.92f, 3);
            float sceneHeight = sceneUnits * HomePromo.HeightShare;
            float content = 10f + sceneHeight + 14f + (lines.Count * lineUnits) + 36f + DesignTokens.Size.CardPrimaryHeight + captionUnits + 20f;
            CardRegions r = Kit.Card(p, content, PlaytestText.T("remove_ads.title"), app.CardClose, Kit.Pop(since), sign:SignDecor.None);
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
            Kit.PrimaryButton(p, buy, PlaytestText.T("store.unavailable"), null,style:PrimaryActionDesign.Type(T.Button),set:GardenLook.Green.Disabled());
            p.Text(PlaytestText.T("store.offline"), r.Body.CenterX, buy.Bottom + p.U(captionUnits / 2f), T.Caption, C.InkBrownSoft, r.Body.Width);
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

            PurchasePanel panel = app.Purchase;
            string note = !panel.IsOnline && panel.Stage != PurchaseStage.Loading ? PlaytestText.T("purchase.offline") : panel.Stage switch
            {
                PurchaseStage.Loading => PlaytestText.T("purchase.checking"),
                PurchaseStage.Offline => PlaytestText.T("purchase.offline"),
                PurchaseStage.Pending => PlaytestText.T("purchase.pending"),
                PurchaseStage.Reprice => PlaytestText.T("purchase.price_changed"),
                PurchaseStage.Failed => PlaytestText.T(panel.Code == "account-changed" ? "purchase.account_changed" : "purchase.failed"),
                _ => panel.Code == "insufficient-funds" ? PlaytestText.T("gameplay.not_enough_petals") :
                    offer.RealMoney ? PlaytestText.T("purchase.store_next") : PlaytestText.F("purchase.balance", NumberText.Group(panel.Balance ?? app.Meta.Economy.Petals)),
            };
            bool price = panel.Stage == PurchaseStage.Confirm || panel.Stage == PurchaseStage.Reprice;
            string action = panel.Stage == PurchaseStage.Loading ? "purchase.checking_action" : price ? "purchase.buy" : "purchase.check_action";
            Kit.PurchaseCard(p, offer, app.PurchasePicture, note, panel.CanBuy || panel.CanCheck ? app.BuyConfirmed : (Action?)null,
                panel.CanCancelPurchase ? panel.CancelPurchase : app.CloseOverlay, Kit.Pop(since), action, price, panel.CanCancelPurchase ? "purchase.cancel_pending" : price ? "common.cancel" : "common.close");
        }

        /// <summary>An item's player-facing name (<c>cosmetic.{id}</c> in the string table), else its catalog name.</summary>
        public static string ItemName(CosmeticItem item) =>
            item.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix, StringComparison.Ordinal)
                ? PlaytestText.T("profile.achievement." + item.Id.Substring(CosmeticCatalog.AchievementBadgePrefix.Length))
                : PlaytestText.Has("cosmetic." + item.Id) ? PlaytestText.T("cosmetic." + item.Id) : item.Name;
    }
}
