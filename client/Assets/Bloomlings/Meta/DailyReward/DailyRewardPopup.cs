using System;
using Bloomlings.Client.Art;
using System.Globalization;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C=Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T=Bloomlings.Client.UI.Design.DesignTokens.Type;
namespace Bloomlings.Client.Meta.DailyReward
{
    /// <summary>Today's five steps on one calm shelf; one CTA claims only the next eligible step.</summary>
    public sealed class DailyRewardPopup:MonoBehaviour
    {
        private CardView _card=null!;
        private TextMeshProUGUI _caption=null!,_claimLabel=null!;
        private Button _claimButton=null!;
        private RectTransform _ad=null!;
        private readonly Image[] _ready=new Image[DailyRewardService.StepCount];
        private readonly RectTransform[] _checks=new RectTransform[DailyRewardService.StepCount];
        private readonly Image[] _locks=new Image[DailyRewardService.StepCount];
        private readonly RectTransform[] _ads=new RectTransform[DailyRewardService.StepCount];
        private readonly TextMeshProUGUI[] _amounts=new TextMeshProUGUI[DailyRewardService.StepCount];
        private DailyRewardService? _daily;
        private Func<bool>? _adReady;
        private Action<DailyRewardStep,Action<bool>>? _claim;
        private bool _pending;
        private float _nextRefresh;
        private int _claimedStep;
        private float _claimedAt;
        public static DailyRewardPopup Create(Transform parent)
        {
            DailyRewardPopup popup=null!;
            CardView card=UiKit.Card("DailyReward",parent,Loc.T("daily_reward.title"),DailyRewardCard.ContentUnits(5),()=>popup.Hide(),sign:SignDecor.None);
            popup=card.Root.AddComponent<DailyRewardPopup>();popup._card=card;
            Box body=card.Regions.Body;float u=DesignTokens.ScaleFor(UiKit.ScreenBox().Width,UiKit.ScreenBox().Height);
            DailyRewardRegions d=DailyRewardCard.Layout(body,u,5);
            RectTransform shelf=UiKit.PopupTray("Shelf",card.Body);UiKit.PlaceBox(shelf,d.Shelf,body);
            for(int i=0;i<5;i++)
            {
                DailyRowParts parts=DailyRewardCard.Row(d.Rows[i]);
                Image station=UiKit.Row("Step"+(i+1),card.Body,false);UiKit.PlaceBox(station.rectTransform,parts.Row,body);
                popup._ready[i]=UiKit.RoundRing("Ready",station.transform,UiTheme.Of(GardenLook.Green.Top),b=>b.Width*.2f,_=>UiKit.Units(5));UiFactory.Stretch(popup._ready[i].rectTransform);
                TextMeshProUGUI number=UiKit.Label("Number",station.transform,(i+1).ToString(CultureInfo.InvariantCulture),PopupDesign.Label,UiTheme.Of(C.InkBrownSoft));UiKit.PlaceBox(number.rectTransform,parts.Number,parts.Row);
                Image lotus=UiKit.PetalIcon("Petals",station.transform);UiKit.PlaceBox(lotus.rectTransform,parts.Lotus,parts.Row);
                popup._amounts[i]=UiKit.Label("Amount",station.transform,string.Empty,PopupDesign.Label,UiTheme.Of(C.InkBrown),look:TextLook.Plain(C.InkBrown));UiKit.PlaceBox(popup._amounts[i].rectTransform,parts.Amount,parts.Row);
                RectTransform check=UiFactory.CreateRect("Check",station.transform);BoxLayout checkLayout=BoxLayout.On(check);UiKit.CheckBadge(checkLayout,b=>Box.FromCenter(b.CenterX,b.CenterY,Math.Min(b.Width,b.Height),Math.Min(b.Width,b.Height)));UiKit.PlaceBox(check,parts.Button,parts.Row);popup._checks[i]=check;
                popup._locks[i]=UiKit.RaisedGlyph(station.transform,ProceduralSprites.RaisedGlyph("ui.lock",C.InkBrownSoft));UiKit.PlaceBox(popup._locks[i].rectTransform,parts.Button.Inset(parts.Button.Width*.12f),parts.Row);
                popup._ads[i]=UiKit.AdMark("Ad",station.transform);UiKit.PlaceBox(popup._ads[i],Box.FromCenter(parts.Row.Right-parts.Row.Width*.16f,parts.Row.Top+parts.Row.Height*.12f,parts.Row.Width*.26f,parts.Row.Width*.26f),parts.Row);
            }
            popup._claimButton=UiKit.PrimaryButton("Claim",card.Body,Loc.T("daily_reward.claim"),popup.ClaimNext);
            UiKit.PlaceBox((RectTransform)popup._claimButton.transform,d.Claim,body);popup._claimLabel=popup._claimButton.GetComponentInChildren<TextMeshProUGUI>();
            GardenButton claimView=popup._claimButton.GetComponent<GardenButton>();
            popup._ad=UiKit.AdMark("Ad",claimView.Content);
            BoxLayout.On(claimView.Content).Watch(popup._claimLabel).Then(b=>
            {
                Box f=PrimaryActionDesign.Content(b,false);TypeStyle type=PrimaryActionDesign.Type(T.Button);
                if(popup._ad.gameObject.activeSelf)UiKit.PriceButtonContent(popup._ad,popup._claimLabel,type,f);
                else KitText.Place(popup._claimLabel,type,f.CenterX,f.CenterY,UiKit.Units(type.Size),f.Width-f.Height*.3f);
            });
            popup._caption=UiKit.Label("Caption",card.Body,string.Empty,T.Caption,UiTheme.Of(C.InkBrownSoft));UiKit.PlaceBox(popup._caption.rectTransform,d.Caption,body);
            card.Root.SetActive(false);return popup;
        }
        public void Show(DailyRewardService daily,Func<bool> adReady,Action<DailyRewardStep,Action<bool>> claim)
        { _daily=daily;_adReady=adReady;_claim=claim;_pending=false;_claimedStep=0;Refresh();_card.Root.SetActive(true); }
        public void Hide()=>_card.Root.SetActive(false);
        private void Refresh()
        {
            if(_daily==null)return;
            for(int i=0;i<5;i++)
            {
                DailyRewardStep step=_daily.Steps[i];DailyStepState state=_daily.StateOf(step.Number);
                _amounts[i].text=NumberText.Plus(step.Petals);_ready[i].gameObject.SetActive(state==DailyStepState.Ready);
                _checks[i].gameObject.SetActive(state==DailyStepState.Claimed);_locks[i].gameObject.SetActive(state==DailyStepState.Locked);_ads[i].gameObject.SetActive(step.Ad);
            }
            bool available=_daily.CanClaim && _daily.NextStep.HasValue;
            _claimButton.gameObject.SetActive(available);
            if(available)
            {
                bool ad=_daily.NextStep!.Value.Ad;_ad.gameObject.SetActive(ad);
                _claimLabel.text=Loc.T(ad?"daily_reward.watch_claim":"daily_reward.claim");
                _claimButton.interactable=!_pending && (!ad || DailyRewardService.AdStub || (_adReady?.Invoke() ?? false));
            }
            (string key,object[] args)=DailyRewardCard.Duration(_daily.MinutesToNextDay);
            _caption.text=Loc.F(DailyRewardCard.CaptionKey(_daily.CanClaim),Loc.F(key,args));_nextRefresh=Time.unscaledTime+1;
        }
        private void ClaimNext()
        {
            if(_pending || _daily==null || !_daily.CanClaim || !_daily.NextStep.HasValue || _claim==null)return;
            DailyRewardStep step=_daily.NextStep.Value;_pending=true;Refresh();
            _claim(step,paid=>{_pending=false;if(!this)return;if(paid){_claimedStep=step.Number;_claimedAt=Time.unscaledTime;}Refresh();});
        }
        private void Update()
        {
            if(Time.unscaledTime>=_nextRefresh)Refresh();
            if(_claimedStep>0)_checks[_claimedStep-1].localScale=Vector3.one*DailyRewardCard.CheckPop(Time.unscaledTime-_claimedAt);
        }
    }
}
