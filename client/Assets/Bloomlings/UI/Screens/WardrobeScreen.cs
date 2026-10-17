using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Fixed preview and filters; native vertical scrolling creates only visible shelf rows.</summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private GameObject _root=null!;
        private WardrobeService _wardrobe=null!;
        private readonly WardrobeBrowseState _browse=new WardrobeBrowseState();
        private WardrobeShelfRegions _regions=null!;
        private PageHeaderView _header=null!;
        private Image _stage=null!,_thumb=null!,_lockedFrame=null!;
        private BloomlingFigure _hero=null!;
        private TextMeshProUGUI _name=null!;
        private Button _previous=null!,_next=null!;
        private RectTransform _filters=null!,_viewport=null!,_content=null!;
        private ScrollRect _scroll=null!;
        private LockedNoticeView _notice=null!;
        private BottomNavView? _nav;
        private PurchaseCardView _confirm=null!;
        private ToastView _toast=null!;
        private Func<long>? _petals;
        private Func<HomeLook>? _look;
        private bool _store,_locked;
        private long _shownPetals=-1;
        private readonly Dictionary<int,GameObject> _cells=new Dictionary<int,GameObject>();
        private readonly Dictionary<int,Image> _shelves=new Dictionary<int,Image>();
        private IReadOnlyList<WardrobeShelfItem> _items=Array.Empty<WardrobeShelfItem>();
        private float _lastOffset=-1;
        public bool IsOpen=>_root.activeSelf;

        public static WardrobeScreen Create(Transform parent,WardrobeService wardrobe,Func<long>? petals=null,
            Action? onStore=null,Action<NavPlace>? onNav=null,Func<HomeLook>? navLook=null,Func<AvatarItem>? avatar=null)
        {
            Image root=UiFactory.CreateImage("Wardrobe",parent,null,Color.clear,raycast:true);UiFactory.Stretch(root.rectTransform);
            var s=root.gameObject.AddComponent<WardrobeScreen>();
            s._toast=ToastView.Create(root.transform);
            s._root=root.gameObject;s._wardrobe=wardrobe;s._petals=petals;s._look=navLook;s._store=onStore!=null;
            BackdropView.Create(root.rectTransform,OwnerPictures.Wardrobe,BackdropScene.Home);
            Image viewport=UiFactory.CreateImage("ShelfViewport",root.transform,null,Color.clear,raycast:true);
            s._viewport=viewport.rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            s._content=UiFactory.CreateRect("ShelfContent",viewport.transform);
            s._scroll=viewport.gameObject.AddComponent<ScrollRect>();s._scroll.viewport=s._viewport;s._scroll.content=s._content;
            s._scroll.horizontal=false;s._scroll.vertical=true;s._scroll.inertia=true;s._scroll.decelerationRate=.015f;s._scroll.scrollSensitivity=30;
            s._scroll.movementType=ScrollRect.MovementType.Clamped;
            s._stage=MetaShelfView.Skin("FittingArch",root.transform,MetaScreenArt.Stage);
            s._hero=BloomlingFigure.Create("Hero",root.transform);s._hero.Body.raycastTarget=false;
            s._name=MetaShelfView.Label("FamilyName",root.transform,"",WardrobeShelfDesign.FamilyName);
            s._previous=UiKit.PageArrow("PreviousFamily",root.transform,false,()=>s.Turn(-1));
            s._next=UiKit.PageArrow("NextFamily",root.transform,true,()=>s.Turn(1));
            s._filters=UiFactory.CreateRect("FixedFilters",root.transform);
            s._thumb=UiKit.RoundRect("ScrollThumb",root.transform,UiTheme.Of(ShopBodyDesign.Card.Face),b=>b.Width*.5f);
            s._lockedFrame=UiKit.CardFrame("LockedFrame",root.transform,false);s._notice=UiKit.LockedNotice("Locked",root.transform);
            if(onNav!=null)s._nav=UiKit.BottomNav("BottomNav",root.transform,p=>{if(p!=NavPlace.Wardrobe)onNav(p);});
            s._header=UiKit.PageHeader(root.transform,Loc.T("wardrobe.title"),s.Hide,petals!=null,onStore);
            s._confirm=UiKit.PurchaseCard(root.transform);root.gameObject.SetActive(false);return s;
        }
        public void Show()
        {
            _root.SetActive(true);_locked=false;_shownPetals=-1;
            _wardrobe.Changed-=Changed;_wardrobe.Changed+=Changed;Refresh();
        }
        public void ShowProfile()=>Show();
        public void Hide(){Remember();_confirm.Cancel();_root.SetActive(false);}
        public void ShowLocked(int level)
        {
            _root.SetActive(true);_locked=true;_shownPetals=-1;SetContent(false);ClearCells();
            var f=UiKit.ScreenFrame();var r=ScreenLayout.LockedPage(f.Width,f.Height,f.Insets);
            _header.Place(r.Header);_lockedFrame.gameObject.SetActive(true);UiKit.PlaceScreen(_lockedFrame.rectTransform,r.Panel);
            _notice.gameObject.SetActive(true);UiKit.PlaceScreen((RectTransform)_notice.transform,r.Notice);_notice.Show(NavPlace.Wardrobe,level);
            _nav?.Show(NavPlace.Wardrobe,_look?.Invoke()??HomeLook.All);
        }
        private void OnDisable(){if(_wardrobe!=null)_wardrobe.Changed-=Changed;}
        private void OnDestroy()=>OnDisable();
        private void Changed(){if(IsOpen&&!_locked){Remember();Refresh();}}
        private void Remember(){if(_regions!=null&&!_locked)_browse.SetOffset(_content.anchoredPosition.y*UiKit.PixelsPerUnit,_regions.Maximum(_items.Count));}
        private void Select(Family family){Remember();_browse.Select(family);_scroll.StopMovement();Refresh();}
        private void Select(CosmeticKind kind){Remember();_browse.Select(kind);_scroll.StopMovement();Refresh();}
        private void Turn(int step)
        {
            int index=0;for(int i=0;i<4;i++)if(WardrobeService.Families[i]==_browse.Family)index=i;
            Select(WardrobeService.Families[(index+step+4)%4]);
        }
        private void SetContent(bool show)
        {
            foreach(GameObject go in new[]{_viewport.gameObject,_stage.gameObject,_hero.Rect.gameObject,_name.gameObject,
                _previous.gameObject,_next.gameObject,_filters.gameObject,_thumb.gameObject})go.SetActive(show);
        }
        private void Refresh()
        {
            var f=UiKit.ScreenFrame();_regions=WardrobeShelfDesign.Page(f.Width,f.Height,f.Insets);var r=_regions;
            SetContent(true);_lockedFrame.gameObject.SetActive(false);_notice.gameObject.SetActive(false);
            _header.Place(r.Header);_nav?.Show(NavPlace.Wardrobe,_look?.Invoke()??HomeLook.All);
            UiKit.PlaceScreen(_stage.rectTransform,r.Stage);_hero.ShowHero(_browse.Family,_wardrobe.OutfitOf(_browse.Family));
            UiKit.PlaceScreen(_hero.Rect,HomeStage.Figure(r.Hero.CenterX,r.Hero.Bottom,r.Hero.Height));
            _name.text=Loc.T("family."+WardrobeService.FamilyKey(_browse.Family));UiKit.PlaceScreen(_name.rectTransform,r.Name);
            UiKit.PlaceScreen((RectTransform)_previous.transform,r.Previous);UiKit.PlaceScreen((RectTransform)_next.transform,r.Next);
            BuildFilters();_items=WardrobeShelfCatalog.Build(_wardrobe,_browse.Kind,_browse.Family);_browse.SetOffset(_browse.Offset,r.Maximum(_items.Count));
            UiKit.PlaceScreen(_viewport,r.Viewport);
            _content.anchorMin=new Vector2(0,1);_content.anchorMax=new Vector2(1,1);_content.pivot=new Vector2(.5f,1);
            float height=r.ContentHeight(_items.Count)+r.Viewport.Height-r.Area.Height;
            _content.sizeDelta=new Vector2(0,height/UiKit.PixelsPerUnit);_content.anchoredPosition=new Vector2(0,_browse.Offset/UiKit.PixelsPerUnit);
            ClearCells();_lastOffset=-1;UpdateVisible();
        }
        private Box ContentBox=>new Box(_regions.Viewport.Left,_regions.Viewport.Top,_regions.Viewport.Right,
            _regions.Viewport.Top+_content.sizeDelta.y*UiKit.PixelsPerUnit);
        private void BuildFilters()
        {
            for(int i=_filters.childCount-1;i>=0;i--){GameObject c=_filters.GetChild(i).gameObject;c.SetActive(false);Destroy(c);}
            Box box=new Box(_regions.Families.Left,_regions.Families.Top,_regions.Categories.Right,_regions.Categories.Bottom);
            Image shield=UiFactory.CreateImage("FilterInput",_filters,null,Color.clear,raycast:true);UiKit.PlaceScreen(shield.rectTransform,box);
            UiKit.PlaceScreen(UiKit.NavigationBar("Families",_filters,4),_regions.Families);
            for(int i=0;i<4;i++)
            {
                Family family=WardrobeService.Families[i];
                var tab=UiKit.FamilyTab(family.ToString(),_filters,Loc.T("family."+WardrobeService.FamilyKey(family)),()=>Select(family));
                UiKit.PlaceScreen((RectTransform)tab.transform,NavigationSkin.Segment(_regions.Families,i,4));tab.Select(family==_browse.Family);
            }
            string[] labels=new string[4],keys=new string[4];
            for(int i=0;i<4;i++){var k=CosmeticCatalog.WornKinds[i];labels[i]=KindLabel(k);keys[i]="wardrobe.kind_"+k.ToString().ToLowerInvariant();}
            TabsView kinds=UiKit.Tabs("Categories",_filters,labels,i=>Select(CosmeticCatalog.WornKinds[i]),keys);
            UiKit.PlaceScreen((RectTransform)kinds.transform,_regions.Categories);int selected=0;for(int i=0;i<4;i++)if(CosmeticCatalog.WornKinds[i]==_browse.Kind)selected=i;kinds.Select(selected);
        }
        private void ClearCells()
        {
            foreach(GameObject go in _cells.Values){go.SetActive(false);Destroy(go);}_cells.Clear();
            foreach(Image im in _shelves.Values){im.gameObject.SetActive(false);Destroy(im.gameObject);}_shelves.Clear();
        }
        private void UpdateVisible()
        {
            var r=_regions;float offset=Mathf.Clamp(_content.anchoredPosition.y*UiKit.PixelsPerUnit,0,r.Maximum(_items.Count));
            _browse.SetOffset(offset,r.Maximum(_items.Count));var window=r.Visible(_items.Count,offset);
            var remove=new List<int>();
            foreach(var p in _cells)if(p.Key<window.First||p.Key>window.Last)remove.Add(p.Key);
            foreach(int i in remove){_cells[i].SetActive(false);Destroy(_cells[i]);_cells.Remove(i);}
            remove.Clear();
            foreach(var p in _shelves)if(p.Key<window.First/3||p.Key>window.Last/3)remove.Add(p.Key);
            foreach(int i in remove){_shelves[i].gameObject.SetActive(false);Destroy(_shelves[i].gameObject);_shelves.Remove(i);}
            for(int row=window.First/3;row<=window.Last/3;row++)if(!_shelves.ContainsKey(row))
            {
                Image shelf=MetaShelfView.Skin("Shelf"+row,_content,MetaScreenArt.Shelf);UiKit.PlaceBox(shelf.rectTransform,r.Row(row),ContentBox);
                shelf.transform.SetAsFirstSibling();_shelves[row]=shelf;
            }
            for(int i=window.First;i<=window.Last;i++)if(!_cells.ContainsKey(i))_cells[i]=CreateItem(i);
            _thumb.gameObject.SetActive(r.Maximum(_items.Count)>0);UiKit.PlaceScreen(_thumb.rectTransform,r.Thumb(_items.Count,offset));_lastOffset=offset;
        }
        private GameObject CreateItem(int index)
        {
            var card=_items[index];CosmeticItem? item=card.Item;Box cell=_regions.Cell(index);
            Image hit=UiFactory.CreateImage("Item"+index,_content,null,Color.clear,raycast:card.State!=WardrobeItemState.Reward);UiKit.PlaceBox(hit.rectTransform,cell,ContentBox);
            if(card.State!=WardrobeItemState.Reward)UiKit.TapTarget(hit,()=>Choose(card));
            UiKit.PlaceBox(MetaShelfView.Skin("Base",hit.transform,MetaScreenArt.Base).rectTransform,WardrobeShelfDesign.Pedestal(cell),cell);
            RectTransform picture=UiFactory.CreateRect("Preview",hit.transform);UiKit.PlaceBox(picture,WardrobeShelfDesign.Picture(cell),cell);
            var hero=BloomlingFigure.Create("Hero",picture);hero.Body.raycastTarget=false;
            hero.ShowHero(_browse.Family,WardrobeShelfCatalog.ShelfPreview(_browse.Kind,item));UiFactory.Stretch(hero.Rect);
            UiKit.PlaceBox(MetaShelfView.Skin("NamePlate",hit.transform,MetaScreenArt.Tag).rectTransform,WardrobeShelfDesign.Caption(cell),cell);
            var name=MetaShelfView.Label("Name",hit.transform,item==null?Loc.T("wardrobe.default"):Name(item),WardrobeShelfDesign.ItemName);
            UiKit.PlaceBox(name.rectTransform,WardrobeShelfDesign.Caption(cell).Inset(cell.Width*.06f,0),cell);
            if(WardrobeShelfCatalog.Chosen(_wardrobe,_browse.Family,_browse.Kind,item))UiKit.PlaceBox(MetaShelfView.Check(hit.transform),WardrobeShelfDesign.Check(cell),cell);
            if(card.State==WardrobeItemState.ForSale)UiKit.PlaceBox(MetaShelfView.Price(hit.transform,NumberText.Group(item!.Price)).rectTransform,ProfileGameDesign.PurchaseFace(WardrobeShelfDesign.Action(cell)),cell);
            if(card.State==WardrobeItemState.Reward)
            {
                Box action=WardrobeShelfDesign.Action(cell);UiKit.PlaceBox(MetaShelfView.Skin("Reward",hit.transform,MetaScreenArt.Tag).rectTransform,action,cell);
                UiKit.PlaceBox(UiKit.LockBadge("Lock",hit.transform),Box.FromCenter(action.Left+action.Height*.4f,action.CenterY,action.Height*.8f,action.Height*.8f),cell);
                var note=MetaShelfView.Label("RewardLabel",hit.transform,RewardText(card),WardrobeShelfDesign.Status);
                UiKit.PlaceBox(note.rectTransform,new Box(action.Left+action.Height,action.Top,action.Right-action.Height*.12f,action.Bottom),cell);
            }
            return hit.gameObject;
        }
        private void Choose(WardrobeShelfItem card)
        {
            Family family=_browse.Family;CosmeticKind kind=card.Item?.Kind??_browse.Kind;CosmeticItem? item=card.Item;
            if(card.State==WardrobeItemState.ForSale)
            {
                _confirm.ShowPetals(PurchaseOffer.ForPetals(Name(item!),item!.Price),_petals?.Invoke()??0,well=>
                {
                    var figure=BloomlingFigure.Create("Preview",well);UiFactory.Stretch(figure.Rect);
                    figure.ShowHero(family,WardrobeShelfCatalog.Preview(_wardrobe.OutfitOf(family),kind,item));
                },()=>{if(_wardrobe.TryBuy(item.Id))_wardrobe.Equip(family,item.Id);},()=>_toast.Show(Loc.T("gameplay.not_enough_petals")));return;
            }
            if(item==null||_wardrobe.EquippedFor(family,kind)?.Id==item.Id)_wardrobe.Unequip(family,kind);else _wardrobe.Equip(family,item.Id);
        }
        private void Update()
        {
            if(_petals!=null&&_header.Petals!=null){long now=_petals();if(now!=_shownPetals){_header.Petals.Show(now,_store&&(_look==null||_look().Store));_shownPetals=now;}}
            if(!_locked&&_regions!=null&&Mathf.Abs(_content.anchoredPosition.y*UiKit.PixelsPerUnit-_lastOffset)>.2f)UpdateVisible();
        }
        public static string RewardText(WardrobeShelfItem item)=>item.RewardCadence.HasValue?Loc.F("wardrobe.milestone_every",item.RewardCadence.Value):Loc.T("wardrobe.milestone_reward");
        public static string Name(CosmeticItem item)
        {
            if(item.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix,StringComparison.Ordinal))return Loc.T("profile.achievement."+item.Id.Substring(CosmeticCatalog.AchievementBadgePrefix.Length));
            if(item.MilestoneLevel.HasValue)return Loc.F(item.Kind==CosmeticKind.Marker?"cosmetic.level_marker":"cosmetic.level_badge",item.MilestoneLevel.Value);
            return Loc.T("cosmetic."+item.Id,item.Name);
        }
        public static Sprite Icon(CosmeticItem item)=>HeroGearArt.Icon(item.Id) is HeroFrame gear ? HeroGearView.Picture(gear.Name)! : item.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix,StringComparison.Ordinal)?
            OwnerArt.Badge(item.Id.Substring(CosmeticCatalog.AchievementBadgePrefix.Length))??ProceduralSprites.Accessory(item.Shape):ProceduralSprites.Accessory(item.Shape);
        public static string KindLabel(CosmeticKind kind)=>Loc.T("wardrobe.kind_"+kind.ToString().ToLowerInvariant());
    }
}
