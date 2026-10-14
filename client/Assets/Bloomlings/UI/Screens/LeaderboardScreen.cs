using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Winners' podium, scrollable neighbour board and fixed personal row. Every portrait uses its saved cosmetics.</summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        private GameObject _root=null!;
        private RectTransform _body=null!,_viewport=null!,_content=null!;
        private ScrollRect _scroll=null!;
        private PageHeaderView _header=null!;
        private BottomNavView? _nav;
        private Func<HomeLook>? _look;
        private Func<long>? _petals;
        private CosmeticCatalog _catalog=null!;
        private Action _refresh=null!;
        private bool _store,_locked;
        private long _shown=-1;
        public bool IsOpen=>_root.activeSelf;
        public bool ShowsRanks=>IsOpen&&!_locked;

        public static LeaderboardScreen Create(Transform parent,Action onRefresh,Func<long>? petals=null,Action? onStore=null,
            Action<NavPlace>? onNav=null,Func<HomeLook>? navLook=null,CosmeticCatalog? catalog=null)
        {
            Image root=UiFactory.CreateImage("Leaderboard",parent,null,Color.clear,raycast:true);UiFactory.Stretch(root.rectTransform);
            var s=root.gameObject.AddComponent<LeaderboardScreen>();s._root=root.gameObject;s._refresh=onRefresh;s._petals=petals;s._look=navLook;s._store=onStore!=null;
            s._catalog=catalog??new CosmeticCatalog(Array.Empty<CosmeticItem>());
            BackdropView.Create(root.rectTransform,OwnerPictures.Wardrobe,BackdropScene.Home);
            s._body=UiFactory.CreateRect("HonorBoard",root.transform);UiFactory.Stretch(s._body);
            if(onNav!=null)s._nav=UiKit.BottomNav("BottomNav",root.transform,p=>{if(p!=NavPlace.Leaderboard)onNav(p);});
            s._header=UiKit.PageHeader(root.transform,Loc.T("leaderboard.title"),s.Hide,petals!=null,onStore);
            root.gameObject.SetActive(false);return s;
        }
        public void Show(LeaderboardPage? page,bool stale,ProfileLook? own=null,AvatarItem? avatar=null,int localHighestLevel=0)
        {
            float offset=_content!=null?_content.anchoredPosition.y*UiKit.PixelsPerUnit:0;
            _root.SetActive(true);_locked=false;_shown=-1;ClearBody();
            var f=UiKit.ScreenFrame();var r=LeaderboardHonorDesign.Page(f.Width,f.Height,f.Insets);
            _header.Place(r.Header);_nav?.Show(NavPlace.Leaderboard,_look?.Invoke()??HomeLook.All);
            UiKit.PlaceScreen(MetaShelfView.Skin("Podium",_body,MetaScreenArt.Podium).rectTransform,r.Podium);
            for(int rank=1;rank<=3;rank++)
            {
                LeaderboardEntry? entry=null;if(page!=null)foreach(var candidate in page.Entries)if(candidate.Rank==rank){entry=candidate;break;}
                if(entry!=null)Portrait(_body,r.Portrait(rank),entry,own,avatar);
                NamePlate(_body,r.Name(rank),entry==null?"—":DisplayName(entry),LeaderboardHonorDesign.SmallName,wood:true);
                Plate(_body,r.Level(rank),entry==null?"—":Loc.F("common.level",entry.Level),WardrobeShelfDesign.Status);
                Label(_body,r.Medal(rank),rank.ToString(),LeaderboardHonorDesign.Number);
            }
            Image viewport=UiFactory.CreateImage("RankViewport",_body,null,Color.clear,raycast:true);_viewport=viewport.rectTransform;
            UiKit.PlaceScreen(_viewport,r.Viewport);viewport.gameObject.AddComponent<RectMask2D>();
            _content=UiFactory.CreateRect("RankContent",viewport.transform);
            _scroll=viewport.gameObject.AddComponent<ScrollRect>();_scroll.viewport=_viewport;_scroll.content=_content;
            _scroll.horizontal=false;_scroll.vertical=true;_scroll.inertia=true;_scroll.movementType=ScrollRect.MovementType.Clamped;
            var rows=new List<LeaderboardEntry>();
            if(page!=null)foreach(var entry in page.Entries)if(entry.Rank>3&&!entry.IsPlayer)rows.Add(entry);
            rows.Sort((a,b)=>a.Rank.CompareTo(b.Rank));
            float height=Mathf.Max(r.Rows.Height,r.ContentHeight(rows.Count));
            _content.anchorMin=new Vector2(0,1);_content.anchorMax=new Vector2(1,1);_content.pivot=new Vector2(.5f,1);
            _content.sizeDelta=new Vector2(0,height/UiKit.PixelsPerUnit);
            _content.anchoredPosition=new Vector2(0,Mathf.Clamp(offset,0,r.Maximum(rows.Count))/UiKit.PixelsPerUnit);
            Box content=new Box(r.Rows.Left,r.Rows.Top,r.Rows.Right,r.Rows.Top+height);
            for(int i=0;i<rows.Count;i++)Row(_content,r.Row(i),content,rows[i],own,avatar);
            if(page?.Player!=null)Row(_body,r.Player,null,page.Player,own,avatar);
            else if(localHighestLevel>0)Row(_body,r.Player,null,new LeaderboardEntry(0,Loc.T("leaderboard.you"),localHighestLevel,true),own,avatar);
            else
            {
                Plate(_body,r.Player,Loc.T(stale?"leaderboard.offline_empty":"leaderboard.empty"),WardrobeShelfDesign.Status);
            }
            string status=stale?Loc.T("leaderboard.offline"):string.Empty;
            Label(_body,r.Status,status,WardrobeShelfDesign.Status);
            Button refresh=UiKit.RoundIconButton("Refresh",_body,"ui.restart",_refresh);
            UiKit.PlaceScreen((RectTransform)refresh.transform,r.Refresh);
        }
        public void ShowLocked(int level)
        {
            _root.SetActive(true);_locked=true;_shown=-1;ClearBody();
            var f=UiKit.ScreenFrame();var r=ScreenLayout.LockedPage(f.Width,f.Height,f.Insets);
            _header.Place(r.Header);
            UiKit.PlaceScreen(UiKit.CardFrame("LockedFrame",_body,false).rectTransform,r.Panel);
            var notice=UiKit.LockedNotice("Locked",_body);UiKit.PlaceScreen((RectTransform)notice.transform,r.Notice);notice.Show(NavPlace.Leaderboard,level);
            _nav?.Show(NavPlace.Leaderboard,_look?.Invoke()??HomeLook.All);
        }
        public void Hide()=>_root.SetActive(false);
        private void ClearBody()
        {
            for(int i=_body.childCount-1;i>=0;i--){var go=_body.GetChild(i).gameObject;go.SetActive(false);Destroy(go);}
            _content=null!;_viewport=null!;_scroll=null!;
        }
        private void Row(Transform parent,Box line,Box? content,LeaderboardEntry entry,ProfileLook? own,AvatarItem? avatar)
        {
            Image root=MetaShelfView.Skin("Rank"+entry.Rank,parent,MetaScreenArt.Plank);
            if(content.HasValue)UiKit.PlaceBox(root.rectTransform,line,content.Value);else UiKit.PlaceScreen(root.rectTransform,line);
            if(entry.IsPlayer)root.color=Color.Lerp(Color.white,UiTheme.Of(ShopCatalogDesign.ChosenRim.Face),.28f);
            var parts=LeaderboardHonorDesign.Parts(line);
            Label(root.transform,parts.Rank,entry.Rank>0?NumberText.Group(entry.Rank):"—",LeaderboardHonorDesign.Number,line);
            Portrait(root.transform,parts.Avatar,entry,own,avatar,line);
            NameLabel(root.transform,parts.Name,DisplayName(entry),LeaderboardHonorDesign.NameType,line);
            Plate(root.transform,parts.Level,NumberText.Group(entry.Level),LeaderboardHonorDesign.Number,line);
        }
        private string DisplayName(LeaderboardEntry entry)=>entry.IsPlayer?Loc.T("leaderboard.you"):string.IsNullOrWhiteSpace(entry.Appearance?.Name??entry.Name)?Loc.T("leaderboard.anonymous"):entry.Appearance?.Name??entry.Name;
        private void Portrait(Transform parent,Box box,LeaderboardEntry entry,ProfileLook? own,AvatarItem? ownAvatar,Box? container=null)
        {
            ProfileLook look=entry.IsPlayer&&own!=null?own:LeaderboardHonorDesign.Look(entry.Appearance,_catalog);
            AvatarItem? avatar=entry.IsPlayer&&ownAvatar!=null?ownAvatar:AvatarCatalog.Get(entry.Appearance?.Avatar);
            if(avatar==null)
            {
                Image neutral=UiFactory.CreateImage("UnknownPortrait",parent,ProceduralSprites.Shape("ui.person"),UiTheme.Of(ShopBodyDesign.Card.Line));
                Place(neutral.rectTransform,box,container);return;
            }
            var view=ProfileAvatar.Create("PlayerPortrait",parent,sceneFrame:true);view.Show(look,Outfit.None,avatar);Place(view.Rect,box,container);
        }
        private static void Place(RectTransform rect,Box box,Box? container){if(container.HasValue)UiKit.PlaceBox(rect,box,container.Value);else UiKit.PlaceScreen(rect,box);}
        private static void Label(Transform parent,Box box,string text,TypeStyle type,Box? container=null)
        {
            var label=MetaShelfView.Label("Label",parent,text,type);Place(label.rectTransform,box,container);
        }
        private static void NameLabel(Transform parent,Box box,string full,TypeStyle type,Box? container=null)
        {
            var label=MetaShelfView.Label("PlayerName",parent,full,type);label.richText=false;
            BoxLayout.On(label.rectTransform).Then(b=>
            {
                float size=Mathf.Min(UiKit.Units(type.Size),b.Height*.78f);
                label.text=LeaderboardHonorDesign.FitName(full,b.Width,t=>KitText.Measure(label,size,t));
            });
            Place(label.rectTransform,box,container);
        }
        private static void Plate(Transform parent,Box box,string text,TypeStyle type,Box? container=null)
        {
            Image plate=MetaShelfView.Skin("LevelPlate",parent,MetaScreenArt.Tag);Place(plate.rectTransform,box,container);
            Label(parent,box.Inset(box.Width*.10f,0),text,type,container);
        }
        private static void NamePlate(Transform parent,Box box,string full,TypeStyle type,bool wood)
        {
            var measure=MetaShelfView.Label("NameMeasure",parent,full,type);measure.richText=false;measure.gameObject.SetActive(false);
            float width=KitText.Measure(measure,UiKit.Units(type.Size),full)*UiKit.PixelsPerUnit;
            box=LeaderboardHonorDesign.NamePlate(box,width>0?width:box.Width);
            Destroy(measure.gameObject);
            Image plate=MetaShelfView.Skin("NamePlate",parent,wood?MetaScreenArt.Plank:MetaScreenArt.Tag);
            Place(plate.rectTransform,box,null);NameLabel(parent,box.Inset(box.Width*.08f,0),full,type);
        }
        private void Update()
        {
            if(_petals!=null&&_header.Petals!=null){long value=_petals();if(value!=_shown){_header.Petals.Show(value,_store&&(_look==null||_look().Store));_shown=value;}}
        }
    }
}
