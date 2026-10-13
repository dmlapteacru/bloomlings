using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    public static class ProfileScreen
    {
        public static void Draw(IPainter p,DesignApp app)
        {
            var meta=app.Meta;var profile=meta.Profile;var r=ScreenLayout.ReferenceProfile(p.Width,p.Height,p.Insets);bool covered=app.Overlays.Count>0;
            DesignApp.DrawBackdrop(p,BackdropScene.Home,meta.CurrentLevel,OwnerPictures.Wardrobe);
            ProfileGamePainter.Skin(p,r.Card,ShopCatalogDesign.CosmeticsFrame);
            Kit.Avatar(p,r.Avatar,profile.Avatar,meta.Wardrobe.Profile.Frame,meta.Wardrobe.Profile.Badge,covered ? null : ()=>app.OpenProfileEdit(ProfileTab.Avatar),HomeScreen.OutfitsOf(app)?.Invoke(profile.Avatar.Family));
            ProfileGamePainter.Label(p,r.Name,NameOf(profile),ProfileGameDesign.Name,left:true);
            p.Mark("ui.edit");p.Picture("ui.edit/profile-button",r.Edit,ProfileGameDesign.EditSurface);
            foreach(Box pencil in ProfileGameDesign.PencilLayers(r.Edit))p.Shape("ui.edit",pencil,Rgba.White);
            if(!covered)p.Hit(Kit.Touch(p,r.Edit),()=>app.OpenProfileEdit(ProfileTab.Name));
            ProfileGamePainter.Label(p,r.Id,PlaytestText.F("profile.id",profile.ShortId),ProfileGameDesign.Info,left:true);
            ProfileGamePainter.Label(p,r.Joined,PlaytestText.F("profile.joined",profile.JoinedMonth),ProfileGameDesign.Info,left:true);
            Kit.WoodSign(p,r.Plaque,PlaytestText.F("common.level",NumberText.Group(meta.CurrentLevel)),T.LevelPill);
            Box title=new Box(r.AchievementsTitle.Left+r.AchievementsTitle.Width*.14f,r.AchievementsTitle.Top,r.AchievementsTitle.Right-r.AchievementsTitle.Width*.14f,r.AchievementsTitle.Bottom);
            Kit.WoodSign(p,title,PlaytestText.T("profile.achievements"),T.Title);
            var states=Achievements.Of(meta.Save);int pages=(states.Count+8)/9,page=Math.Max(0,Math.Min(pages-1,app.ProfileAchievementPage));
            Kit.ArrowButton(p,r.AchievementsTitle.Left+r.AchievementsTitle.Height*.5f,r.AchievementsTitle.CenterY,r.AchievementsTitle.Height,false,covered ? null : ()=>app.ProfileAchievementPage=(page+pages-1)%pages);
            Kit.ArrowButton(p,r.AchievementsTitle.Right-r.AchievementsTitle.Height*.5f,r.AchievementsTitle.CenterY,r.AchievementsTitle.Height,true,covered ? null : ()=>app.ProfileAchievementPage=(page+1)%pages);
            for(int i=0;i<9&&page*9+i<states.Count;i++)
            {
                var state=states[page*9+i];Achievement(p,r.Achievements[i],state,1,covered ? null : ()=>{if(state.Tier>0&&meta.Wardrobe.Show(state.Def.BadgeId))app.HomeToast(PlaytestText.T("profile.badge_equipped"));else app.HomeToast(PlaytestText.T("profile.achievement_help."+state.Def.Id));});
            }
            ProfileGamePainter.Label(p,r.AchievementsNote,PlaytestText.F("profile.achievements_page",page+1,pages),ProfileGameDesign.Info);
            Kit.PageHeader(p,r.Header,PlaytestText.T("profile.title"),covered ? null : app.CloseProfile,app.ShownPetals,!covered&&HomeScreen.Look(app).Store ? app.OpenStore : null);
            if(app.HomeToastText!=null&&!covered)Kit.Toast(p,new Box(r.Safe.Left,r.Card.Top,r.Safe.Right,r.Card.Bottom),app.HomeToastText);
        }
        public static void Achievement(IPainter p,Box tile,AchievementState state,float grow,Action? action=null)
        {
            p.Mark("ui.achievement");ProfileGamePainter.Skin(p,tile,ProfileGameDesign.Gold);Box art=ProfileGameDesign.AchievementPicture(tile);
            string image=PainterBase.BadgePrefix+state.Def.Id;if(state.Tier==0)p.PushAlpha(.58f);if(p.HasSprite(image))p.Sprite(image,art);else p.Shape("ui.trophy",art,ShopBodyDesign.Ink);if(state.Tier==0)p.PopAlpha();
            Box count=ProfileGameDesign.AchievementCount(tile);if(state.Complete)Kit.SlicedSprite(p,"shop/price-button",count,350f/1985,190f/505,.4f);else p.Picture("profile.progress",count,(w,h)=>ShopCatalogDesign.ActionSurface(w,h,false));
            ProfileGamePainter.Label(p,count,state.Complete ? NumberText.Group(state.Value) : PlaytestText.F("profile.achievement_progress",NumberText.Group(state.Shown),NumberText.Group(state.Goal)),ProfileGameDesign.Progress,state.Complete ? ShopBodyDesign.MoneyLetters : ProfileGameDesign.Ink);
            Box mark=ProfileGameDesign.AchievementMark(tile);if(state.Tier==0)Kit.LockBadge(p,mark.CenterX,mark.CenterY,mark.Width);else if(state.Complete)ProfileGamePainter.Check(p,mark);
            ProfileGamePainter.Label(p,ProfileGameDesign.AchievementLabel(tile),PlaytestText.T(state.Def.NameKey),ProfileGameDesign.Item);if(action!=null)p.Hit(tile,action);
        }
        public static string NameOf(ProfileService profile)=>profile.Name??PlaytestText.F("profile.default_name",profile.DefaultNumber);
        public static void Edit(IPainter p,DesignApp app,float since)
        {
            var editor=app.ProfileEditor;if(editor==null)return;var meta=app.Meta;var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);
            Action? Act(Action? action)=>app.DrawingCovered ? null : action;
            DesignApp.DrawBackdrop(p,BackdropScene.Home,meta.CurrentLevel,OwnerPictures.Wardrobe);
            var keys=new List<string>();var labels=new List<string>();foreach(var t in ProfileEditor.Tabs){keys.Add(ProfileGameDesign.TabKey(t));labels.Add(PlaytestText.T(ProfileGameDesign.TabKey(t)));}
            Kit.Tabs(p,r.Tabs,labels,(int)editor.Tab,i=>{editor.Tab=ProfileEditor.Tabs[i];app.ProfileScrollOffset=0;},keys);
            ProfileGamePainter.Skin(p,r.Summary,ShopCatalogDesign.CosmeticsFrame);
            Kit.Avatar(p,r.Preview,editor.Avatar,editor.Frame,editor.Badge,null);
            ProfileGamePainter.Label(p,r.PreviewName,editor.Name??PlaytestText.F("profile.default_name",meta.Profile.DefaultNumber),ProfileGameDesign.Name,left:true);
            Kit.WoodSign(p,r.Level,PlaytestText.F("common.level",NumberText.Group(meta.CurrentLevel)),T.LevelPill);
            if(editor.Tab==ProfileTab.Name)
            {
                ProfileGamePainter.Skin(p,r.NameCard,ProfileGameDesign.Gold);p.Sprite("navigation/icon-name",Box.FromCenter(r.NameCard.CenterX,r.NameCard.Top,r.W*.09f,r.W*.09f));
                ProfileGamePainter.Label(p,ProfileGameDesign.In(r.NameCard,.12f,.17f,.88f,.36f),PlaytestText.T("profile.name_prompt"),ProfileGameDesign.Name);
                Kit.Well(p,r.NameField,r.NameField.Height*.30f,ShopBodyDesign.Cream);ProfileGamePainter.Label(p,r.NameField,editor.Name??NameOf(meta.Profile),ProfileGameDesign.Name);
                if(!app.DrawingCovered)p.Hit(r.NameField,app.AskProfileName);
                ProfileGamePainter.Label(p,r.NameHint,PlaytestText.T("profile.name_hint"),ProfileGameDesign.Info);
            }
            else Catalog(p,app,editor,r);
            ProfileGamePainter.Price(p,r.Button,editor.NeedsBuying ? PlaytestText.F("profile.buy",NumberText.Group(editor.Price)) : PlaytestText.T("profile.save"),Act(app.ConfirmProfileEdit),false);
            Kit.PageHeader(p,r.Header,PlaytestText.T("profile.edit_title"),Act(app.CardClose),app.ShownPetals,HomeScreen.Look(app).Store ? Act(app.OpenStoreFromProfileEdit) : null);
            if(app.HomeToastText!=null&&!app.DrawingCovered)Kit.Toast(p,new Box(r.Grid.Left,r.Button.Top-r.W*.085f,r.Grid.Right,r.Button.Top-r.W*.015f),app.HomeToastText);
        }
        private static void Catalog(IPainter p,DesignApp app,ProfileEditor editor,ProfileGameEditRegions r)
        {
            bool frames=editor.Tab==ProfileTab.Frame,badges=editor.Tab==ProfileTab.Badge;var owned=editor.Owned(frames ? CosmeticKind.Frame : CosmeticKind.Badge);
            int count=editor.Tab==ProfileTab.Avatar ? app.Meta.Profile.Avatars.Count : owned.Count+(badges ? 1 : 0);
            float max=Math.Max(0,ProfileGameDesign.ContentHeight(r.Grid,count,frames)-r.Grid.Height);app.ProfileScrollOffset=Math.Max(0,Math.Min(max,app.ProfileScrollOffset));p.PushClip(r.Grid);
            for(int i=0;i<count;i++)
            {
                int index=i;Box cell=r.Cell(i,frames).Offset(0,-app.ProfileScrollOffset);if(cell.Bottom<=r.Grid.Top||cell.Top>=r.Grid.Bottom)continue;
                AvatarItem avatar=editor.Tab==ProfileTab.Avatar ? app.Meta.Profile.Avatars[i] : editor.Avatar;
                CosmeticItem? item=editor.Tab==ProfileTab.Avatar||badges&&i==0 ? null : owned[i-(badges ? 1 : 0)];
                bool chosen=editor.Tab==ProfileTab.Avatar ? avatar.Id==editor.AvatarId : frames ? item?.Id==editor.FrameId : item?.Id==editor.BadgeId;
                ProfileGamePainter.Skin(p,cell,chosen ? ShopCatalogDesign.SelectedFrame : frames ? ProfileGameDesign.Gold : ShopCatalogDesign.AnimationFrame);
                Box picture=ProfileGameDesign.Picture(cell,frames);
                if(frames)Kit.Avatar(p,picture,avatar,item,null,null);else if(badges)ProfileGamePainter.Badge(p,picture,item);else Kit.AvatarPicture(p,picture,avatar,ring:false,radius:picture.Width*.12f);
                if(chosen)ProfileGamePainter.Check(p,ProfileGameDesign.CellCheck(cell));
                int? price=editor.Tab==ProfileTab.Avatar&&!app.Meta.Profile.Owns(avatar) ? app.Meta.Profile.Price(avatar) : (int?)null;
                Action pick=()=>{app.Sound.Play(Bloomlings.Client.Services.Feedback.SoundCue.Click);if(editor.Tab==ProfileTab.Avatar)editor.PickAvatar(avatar.Id);else if(frames)editor.PickFrame(item!.Id);else if(item==null)editor.ClearBadge();else editor.PickBadge(item.Id);};
                Box? priceTouch=null;
                Box action=price.HasValue ? ProfileGameDesign.CellPrice(cell) : ProfileGameDesign.CellLabel(cell);
                if(action.Bottom>r.Grid.Top&&action.Top<r.Grid.Bottom)
                {
                    if(price.HasValue)
                    {
                        Box hit=ProfileGameDesign.PriceHit(cell,p.U(DesignTokens.Size.TouchMin));
                        if(hit.Bottom>r.Grid.Bottom)hit=hit.Offset(0,r.Grid.Bottom-hit.Bottom);
                        if(hit.Top<r.Grid.Top)hit=hit.Offset(0,r.Grid.Top-hit.Top);
                        priceTouch=hit;
                        ProfileGamePainter.Price(p,action,NumberText.Group(price.Value),app.DrawingCovered ? null : ()=>{pick();app.ConfirmProfileEdit();},true,hit);
                    }
                    else ProfileGamePainter.Label(p,action,editor.Tab==ProfileTab.Avatar ? PlaytestText.T(avatar.IsFree ? "common.free" : "profile.owned") : item==null ? PlaytestText.T("profile.badge_none") : MetaCards.ItemName(item),ProfileGameDesign.Item);
                }
                if(!app.DrawingCovered)
                {
                    Box target=new Box(cell.Left,Math.Max(cell.Top,r.Grid.Top),cell.Right,Math.Min(priceTouch?.Top??cell.Bottom,r.Grid.Bottom));
                    if(target.Height>=p.U(DesignTokens.Size.TouchMin))p.Hit(target,pick);
                }
            }
            p.PopClip();if(!app.DrawingCovered)p.VerticalScroll(r.Grid,dy=>app.ProfileScrollOffset=Math.Max(0,Math.Min(max,app.ProfileScrollOffset-dy)));
            if(count==0)ProfileGamePainter.Label(p,r.Note,PlaytestText.T(frames ? "profile.no_frames" : "profile.no_badges"),ProfileGameDesign.Info);
        }
    }
}
