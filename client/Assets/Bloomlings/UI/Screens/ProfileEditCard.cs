using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Full-page profile editor: fixed navigation/summary/footer, one vertically scrolling catalog.</summary>
    public sealed class ProfileEditCard : MonoBehaviour
    {
        private GameObject _root=null!;
        private TabsView _tabs=null!;
        private RectTransform _tabsBox=null!,_viewport=null!,_content=null!;
        private ScrollRect _scroll=null!;
        private PageHeaderView _header=null!;
        private Image _summary=null!,_levelPlate=null!,_field=null!,_nameCard=null!,_button=null!;
        private ProfileAvatar _preview=null!;
        private TextMeshProUGUI _previewName=null!,_levelLabel=null!,_status=null!,_hint=null!,_note=null!;
        private TMP_InputField _input=null!;
        private RectTransform _nameIcon=null!,_nameTitle=null!;
        private ProfileService _profile=null!;
        private WardrobeService _wardrobe=null!;
        private ProfileEditor? _editor;
        private Func<int> _level=()=>1;
        private Func<long>? _petals;
        private Func<bool> _storeOpen=()=>false;
        private float _layoutWidth,_layoutHeight;
        private bool _nameDirty;
        private Action _changed=()=>{};
        private float _statusUntil=-1;
        private int _wardrobeLevel;
        private ProfileGameEditRegions _regions=null!;
        public bool IsOpen=>_root.activeSelf;

        public static ProfileEditCard Create(Transform parent,ProfileService profile,WardrobeService wardrobe,int wardrobeLevel,Action changed,Func<int>? level=null,Func<long>? petals=null,Action? onStore=null,Func<bool>? storeOpen=null)
        {
            Image root=UiFactory.CreateImage("ProfileEdit",parent,null,Color.clear,raycast:true);UiFactory.Stretch(root.rectTransform);
            var screen=root.gameObject.AddComponent<ProfileEditCard>();screen._root=root.gameObject;screen._profile=profile;screen._wardrobe=wardrobe;screen._wardrobeLevel=wardrobeLevel;screen._changed=changed;screen._level=level??(()=>1);screen._petals=petals;screen._storeOpen=storeOpen??(()=>onStore!=null);
            BackdropView.Create(root.rectTransform,OwnerPictures.Wardrobe,BackdropScene.Home);
            screen._tabsBox=UiFactory.CreateRect("Tabs",root.transform);
            var keys=new string[4];var labels=new string[4];for(int i=0;i<4;i++){keys[i]=ProfileGameDesign.TabKey(ProfileEditor.Tabs[i]);labels[i]=Loc.T(keys[i]);}
            screen._tabs=UiKit.Tabs("Tabs",screen._tabsBox,labels,i=>screen.SelectTab(ProfileEditor.Tabs[i]),keys);
            screen._summary=ProfileGameView.Skin("ProfileSummary",root.transform,ShopCatalogDesign.CosmeticsFrame);
            screen._preview=ProfileAvatar.Create("Preview",root.transform);
            screen._previewName=ProfileGameView.Label("Name",root.transform,"",ProfileGameDesign.Name,align:TextAlignmentOptions.Left);
            screen._levelPlate=UiFactory.CreateImage("Level",root.transform,null,Color.white);
            PictureFit.On(screen._levelPlate,(w,h)=>ProceduralSprites.Picture("profile.level",w,h,(pw,ph)=>ShopCatalogDesign.ActionSurface(pw,ph,false)));
            screen._levelLabel=ProfileGameView.Label("Label",screen._levelPlate.transform,"",ProfileGameDesign.Info);UiFactory.Stretch(screen._levelLabel.rectTransform);
            Image viewport=UiFactory.CreateImage("CatalogViewport",root.transform,null,Color.clear,raycast:true);screen._viewport=viewport.rectTransform;viewport.gameObject.AddComponent<RectMask2D>();screen._content=UiFactory.CreateRect("Catalog",viewport.transform);
            screen._scroll=viewport.gameObject.AddComponent<ScrollRect>();screen._scroll.viewport=screen._viewport;screen._scroll.content=screen._content;screen._scroll.horizontal=false;screen._scroll.vertical=true;screen._scroll.movementType=ScrollRect.MovementType.Clamped;screen._scroll.inertia=true;screen._scroll.decelerationRate=.12f;screen._scroll.scrollSensitivity=40;
            screen._nameCard=ProfileGameView.Skin("NameForm",root.transform,ProfileGameDesign.Gold);
            var icon=UiFactory.CreateImage("Pencil",root.transform,NavigationArt.Get("navigation/icon-name"),Color.white);icon.preserveAspect=true;screen._nameIcon=icon.rectTransform;
            screen._nameTitle=ProfileGameView.Label("YourName",root.transform,Loc.T("profile.name_prompt"),ProfileGameDesign.Name).rectTransform;
            screen._field=UiKit.Well("NameField",root.transform,UiTheme.Of(C.CreamTop),raycast:true);screen._input=NameInput(screen._field,screen);
            screen._hint=ProfileGameView.Label("Hint",root.transform,Loc.T("profile.name_hint"),ProfileGameDesign.Info);
            screen._note=ProfileGameView.Label("Note",root.transform,"",ProfileGameDesign.Info);
            screen._button=ProfileGameView.Price(root.transform,Loc.T("profile.save"),screen.Confirm,petals:false);
            screen._status=ProfileGameView.Label("Status",root.transform,"",ProfileGameDesign.Info,ShopBodyDesign.Letters);screen._status.gameObject.SetActive(false);
            screen._header=UiKit.PageHeader(root.transform,Loc.T("profile.edit_title"),screen.Hide,petals!=null,onStore);
            screen._root.SetActive(false);return screen;
        }
        public void Show(ProfileTab tab)
        {
            _editor=new ProfileEditor(_profile,_wardrobe,tab);_input.SetTextWithoutNotify(_profile.Name??string.Empty);_nameDirty=false;_statusUntil=-1;_status.gameObject.SetActive(false);_root.SetActive(true);_root.transform.SetAsLastSibling();SelectTab(tab);
        }
        public void Hide(){_confirm?.Cancel();_scroll.StopMovement();_root.SetActive(false);_editor=null;}
        private void Update()
        {
            if(_statusUntil>0&&Time.unscaledTime>_statusUntil){_status.gameObject.SetActive(false);_statusUntil=-1;}
            if(_header.Petals!=null&&_petals!=null)_header.Petals.Show(_petals(),_storeOpen());
            var frame=UiKit.ScreenFrame();if(_editor!=null&&(frame.Width!=_layoutWidth||frame.Height!=_layoutHeight)){Layout();Redraw();}
        }
        private void SelectTab(ProfileTab tab)
        {
            if(_editor==null)return;_editor.Tab=tab;_scroll.StopMovement();_content.anchoredPosition=Vector2.zero;_tabs.Select((int)tab);Layout();Redraw();
        }
        private void Layout()
        {
            var f=UiKit.ScreenFrame();_regions=ProfileGameDesign.Edit(f.Width,f.Height,f.Insets);_layoutWidth=f.Width;_layoutHeight=f.Height;
            var r=_regions;_header.Place(r.Header);UiKit.PlaceScreen(_tabsBox,r.Tabs);UiKit.PlaceScreen(_summary.rectTransform,r.Summary);UiKit.PlaceScreen(_preview.Rect,r.Preview);UiKit.PlaceScreen(_previewName.rectTransform,r.PreviewName);UiKit.PlaceScreen(_levelPlate.rectTransform,r.Level);UiKit.PlaceScreen(_viewport,r.Grid);UiKit.PlaceScreen(_button.rectTransform,r.Button);
            UiKit.PlaceScreen(_nameCard.rectTransform,r.NameCard);UiKit.PlaceScreen(_nameTitle,ProfileGameDesign.In(r.NameCard,.12f,.17f,.88f,.36f));UiKit.PlaceScreen(_nameIcon,Box.FromCenter(r.NameCard.CenterX,r.NameCard.Top,r.W*.09f,r.W*.09f));UiKit.PlaceScreen(_field.rectTransform,r.NameField);UiKit.PlaceScreen(_hint.rectTransform,r.NameHint);UiKit.PlaceScreen(_note.rectTransform,r.Note);
            UiKit.PlaceScreen(_status.rectTransform,new Box(r.Grid.Left,r.Button.Top-r.W*.085f,r.Grid.Right,r.Button.Top-r.W*.015f));
        }
        private void Redraw()
        {
            var editor=_editor;if(editor==null)return;
            _preview.Show(new ProfileLook(editor.Frame,editor.Badge,null),null,editor.Avatar);_previewName.text=editor.Name??Loc.F("profile.default_name",_profile.DefaultNumber);_levelLabel.text=Loc.F("common.level",NumberText.Group(_level()));
            bool name=editor.Tab==ProfileTab.Name;foreach(GameObject item in new[]{_nameCard.gameObject,_nameTitle.gameObject,_nameIcon.gameObject,_field.gameObject,_hint.gameObject})item.SetActive(name);_viewport.gameObject.SetActive(!name);_note.gameObject.SetActive(false);
            for(int i=_content.childCount-1;i>=0;i--){var child=_content.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}
            if(!name)
            {
                bool frames=editor.Tab==ProfileTab.Frame,badges=editor.Tab==ProfileTab.Badge;var owned=editor.Owned(frames ? CosmeticKind.Frame : CosmeticKind.Badge);
                int count=editor.Tab==ProfileTab.Avatar ? _profile.Avatars.Count : owned.Count+(badges ? 1 : 0);
                Box area=_regions.Grid;float height=ProfileGameDesign.ContentHeight(area,count,frames);float offset=Mathf.Clamp(_content.anchoredPosition.y,0,Mathf.Max(0,height-area.Height)/UiKit.PixelsPerUnit);
                _content.anchorMin=new Vector2(0,1);_content.anchorMax=new Vector2(1,1);_content.pivot=new Vector2(.5f,1);_content.sizeDelta=new Vector2(0,height/UiKit.PixelsPerUnit);_content.anchoredPosition=new Vector2(0,offset);
                Box content=new Box(area.Left,area.Top,area.Right,area.Top+height);
                for(int i=0;i<count;i++)
                {
                    int index=i;AvatarItem avatar=editor.Tab==ProfileTab.Avatar ? _profile.Avatars[i] : editor.Avatar;CosmeticItem? item=editor.Tab==ProfileTab.Avatar||badges&&i==0 ? null : owned[i-(badges ? 1 : 0)];
                    bool chosen=editor.Tab==ProfileTab.Avatar ? avatar.Id==editor.AvatarId : frames ? item?.Id==editor.FrameId : item?.Id==editor.BadgeId;
                    int? price=editor.Tab==ProfileTab.Avatar&&!_profile.Owns(avatar) ? _profile.Price(avatar) : (int?)null;
                    string label=editor.Tab==ProfileTab.Avatar ? (avatar.IsFree ? Loc.T("common.free") : Loc.T("profile.owned")) : item==null ? Loc.T("profile.badge_none") : ProfileGameView.ItemName(item);
                    Image hit=UiFactory.CreateImage("Selection"+i,_content,null,Color.clear,raycast:true);UiKit.TapTarget(hit,()=>Pick(index),press:true);
                    Image frame=ProfileGameView.Skin("Frame",hit.transform,chosen ? ShopCatalogDesign.SelectedFrame : frames ? ProfileGameDesign.Gold : ShopCatalogDesign.AnimationFrame);UiFactory.Stretch(frame.rectTransform);
                    RectTransform picture;
                    if(frames){var a=ProfileAvatar.Create("Avatar",hit.transform);a.Show(new ProfileLook(item,null,null),null,avatar);picture=a.Rect;}
                    else if(badges){if(item==null)picture=ProfileGameView.NoBadge(hit.transform);else{var image=UiFactory.CreateImage("Badge",hit.transform,ProfileGameView.Badge(item),ProfileGameView.BadgeColor(item));image.preserveAspect=true;picture=image.rectTransform;}}
                    else picture=ProfileGameView.Portrait(hit.transform,avatar);
                    RectTransform? check=chosen ? ProfileGameView.Check(hit.transform) : null;
                    TextMeshProUGUI? title=!price.HasValue ? ProfileGameView.Label("Label",hit.transform,label,ProfileGameDesign.Item) : null;
                    Image? purchase=price.HasValue ? ProfileGameView.Price(hit.transform,NumberText.Group(price.Value)) : null;
                    if(purchase!=null)
                    {
                        Image target=UiFactory.CreateImage("PurchaseTouch",hit.transform,null,Color.clear,raycast:true);UiKit.TapTarget(target,()=>{Pick(index);Confirm();},press:true);
                        BoxLayout.On(hit.rectTransform).Add(target.rectTransform,b=>ProfileGameDesign.PriceHit(b,UiKit.Units(DesignTokens.Size.TouchMin)));
                    }
                    BoxLayout.On(hit.rectTransform).Then(b=>{BoxLayout.Place(picture,ProfileGameDesign.Picture(b,frames));if(check!=null)BoxLayout.Place(check,ProfileGameDesign.CellCheck(b));if(title!=null)KitText.Place(title,ProfileGameDesign.Item,ProfileGameDesign.CellLabel(b).CenterX,ProfileGameDesign.CellLabel(b).CenterY,Mathf.Min(UiKit.Units(ProfileGameDesign.Item.Size),b.Width*.115f),ProfileGameDesign.CellLabel(b).Width);if(purchase!=null)BoxLayout.Place(purchase.rectTransform,ProfileGameDesign.CellPrice(b));});
                    UiKit.PlaceBox(hit.rectTransform,_regions.Cell(i,frames),content);
                }
                if(count==0){_note.text=Loc.T(frames ? "profile.no_frames" : "profile.no_badges");_note.gameObject.SetActive(true);}
            }
            var buttonLabel=_button.GetComponentInChildren<TextMeshProUGUI>();buttonLabel.text=editor.NeedsBuying ? Loc.F("profile.buy",NumberText.Group(editor.Price)) : Loc.T("profile.save");
        }
        private void Pick(int index)
        {
            var editor=_editor;if(editor==null)return;
            if(editor.Tab==ProfileTab.Avatar){if(index<_profile.Avatars.Count)editor.PickAvatar(_profile.Avatars[index].Id);}
            else{bool badges=editor.Tab==ProfileTab.Badge;var owned=editor.Owned(badges ? CosmeticKind.Badge : CosmeticKind.Frame);if(badges&&index==0)editor.ClearBadge();else if(index-(badges ? 1 : 0)<owned.Count){var item=owned[index-(badges ? 1 : 0)];if(badges)editor.PickBadge(item.Id);else editor.PickFrame(item.Id);}}
            Redraw();
        }

        private void Confirm()
        {
            ProfileEditor? editor = _editor;
            if (editor == null)
            {
                return;
            }

            if (_nameDirty && !editor.EnterName(_input.text))
            {
                Say(Loc.T("profile.name_empty"));
                return;
            }

            if (editor.NeedsBuying)
            {
                // "Buy for N" asks the purchase confirmation first (spec 005 FR-040): the avatar is bought only on its Buy;
                // short Petals say so without asking.
                _confirm ??= UiKit.PurchaseCard(_root.transform.parent);
                AvatarItem avatar = editor.Avatar;
                long petals = App.AppServices.Current != null && App.AppServices.Current.TryGet(out Services.Economy.EconomyService? economy) && economy != null ? economy.Petals : editor.Price;
                _confirm.ShowPetals(
                    PurchaseOffer.ForPetals(Loc.T("purchase.avatar"), editor.Price),
                    petals,
                    well =>
                    {
                        ProfileAvatar picture = ProfileAvatar.Create("Avatar", well);
                        UiFactory.Stretch(picture.Rect);
                        picture.Show(null, null, avatar);
                    },
                    () => Finish(editor),
                    () => Say(Loc.T("gameplay.not_enough_petals")));
                return;
            }

            Finish(editor);
        }

        // The purchase confirmation of an avatar, made when first asked (spec 005 FR-040).
        private PurchaseCardView? _confirm;

        /// <summary>The main button's outcome, once a purchase is confirmed.</summary>
        private void Finish(ProfileEditor editor)
        {
            if (_editor != editor)
            {
                return;
            }

            switch (editor.Confirm())
            {
                case ProfileOutcome.Saved:
                    _changed();
                    Hide();
                    return;
                case ProfileOutcome.Bought:
                    GameFeedback.Current?.Play(SoundCue.PodDone);
                    Say(Loc.T("profile.bought"));
                    _changed();
                    break;
                case ProfileOutcome.NotEnoughPetals:
                    GameFeedback.Current?.Play(SoundCue.Refused);
                    Say(Loc.T("gameplay.not_enough_petals"));
                    break;
                default:
                    GameFeedback.Current?.Play(SoundCue.Refused);
                    Say(Loc.T("profile.name_empty"));
                    break;
            }

            Redraw();
        }

        private void Say(string text)
        {
            _status.text = text;
            _status.gameObject.SetActive(true);
            _statusUntil = Time.unscaledTime + 1.6f;
        }

        /// <summary>
        /// The Name tab's text field on <paramref name="well"/>: a single line of up to
        /// <see cref="ProfileService.MaxNameLength"/> characters in <c>type.title</c>, the default name as its placeholder;
        /// its text goes to the editor as it is typed (kept on Save).
        /// </summary>
        private static TMP_InputField NameInput(Image well, ProfileEditCard screen)
        {
            RectTransform area = UiFactory.Stretch(UiFactory.CreateRect("TextArea", well.transform));
            area.offsetMin = new Vector2(UiKit.Units(24f), UiKit.Units(6f));
            area.offsetMax = new Vector2(-UiKit.Units(24f), -UiKit.Units(6f));
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI placeholder = UiKit.Label("Placeholder", area, Loc.F("profile.default_name", screen._profile.DefaultNumber), ProfileGameDesign.Name, UiTheme.Of(C.InkBrownSoft.WithAlpha(0.6f)));
            TextMeshProUGUI text = UiKit.Label("Text", area, string.Empty, ProfileGameDesign.Name, UiTheme.Of(C.InkBrown));
            text.enableAutoSizing = false;
            TMP_InputField input = well.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = well;
            input.characterLimit = ProfileService.MaxNameLength;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.onValueChanged.AddListener(value =>
            {
                screen._nameDirty=true;
                if (screen._editor != null && screen._editor.EnterName(value))
                {
                    screen._previewName.text = screen._editor.Name ?? string.Empty;
                }
            });
            return input;
        }

    }
}
