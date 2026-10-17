using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The same compact preview, category memory and culled shelf catalog as the Unity client.</summary>
    public static class WardrobeScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            var look=HomeScreen.Look(app);
            DesignApp.DrawBackdrop(p,BackdropScene.Home,app.Meta.CurrentLevel,OwnerPictures.Wardrobe);
            if(!BottomNav.IsOpen(NavPlace.Wardrobe,look)){Locked(p,app,look);return;}
            var browse=app.WardrobeBrowse; var wardrobe=app.Meta.Wardrobe;
            WardrobeShelfRegions r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
            var items=WardrobeShelfCatalog.Build(wardrobe,browse.Kind,browse.Family);
            browse.SetOffset(browse.Offset,r.Maximum(items.Count));
            Outfit outfit=wardrobe.OutfitOf(browse.Family);
            p.Mark("ui.wardrobe.illustrations");p.Sprite(MetaScreenArt.Stage,r.Stage);
            Visuals.Hero(p,HomeStage.Figure(r.Hero.CenterX,r.Hero.Bottom,r.Hero.Height),browse.Family,outfit);
            ProfileGamePainter.Label(p,r.Name,PlaytestText.T("family."+WardrobeService.FamilyKey(browse.Family)),WardrobeShelfDesign.FamilyName);
            void Turn(int step){int index=0;for(int i=0;i<4;i++)if(WardrobeService.Families[i]==browse.Family)index=i;browse.Select(WardrobeService.Families[(index+step+4)%4]);}
            Kit.ArrowButton(p,r.Previous.CenterX,r.Previous.CenterY,r.Previous.Width,false,()=>Turn(-1));
            Kit.ArrowButton(p,r.Next.CenterX,r.Next.CenterY,r.Next.Width,true,()=>Turn(1));
            p.PushClip(r.Viewport);
            var window=r.Visible(items.Count,browse.Offset);
            for(int row=window.First/3;row<=window.Last/3;row++)p.Sprite(MetaScreenArt.Shelf,r.Row(row).Offset(0,-browse.Offset));
            for(int i=window.First;i<=window.Last;i++)
            {
                WardrobeShelfItem card=items[i]; CosmeticItem? item=card.Item;
                Box cell=r.Cell(i).Offset(0,-browse.Offset);
                if(cell.Bottom<=r.Viewport.Top||cell.Top>=r.Viewport.Bottom)continue;
                p.Sprite(MetaScreenArt.Base,WardrobeShelfDesign.Pedestal(cell));
                Preview(p,WardrobeShelfDesign.Picture(cell),browse.Family,WardrobeShelfCatalog.Preview(outfit,browse.Kind,item));
                Skin(p,MetaScreenArt.Tag,WardrobeShelfDesign.Caption(cell));
                if(WardrobeShelfDesign.Caption(cell).Within(r.Safe))
                    ProfileGamePainter.Label(p,WardrobeShelfDesign.Caption(cell).Inset(cell.Width*.06f,0),item==null?PlaytestText.T("wardrobe.default"):MetaCards.ItemName(item),WardrobeShelfDesign.ItemName);
                if(WardrobeShelfCatalog.Chosen(wardrobe,browse.Family,browse.Kind,item))ProfileGamePainter.Check(p,WardrobeShelfDesign.Check(cell));
                Box action=WardrobeShelfDesign.Action(cell);
                if(card.State==WardrobeItemState.ForSale&&action.Within(r.Safe))ProfileGamePainter.Price(p,action,NumberText.Group(item!.Price));
                if(card.State==WardrobeItemState.Reward&&action.Within(r.Safe))
                {
                    Skin(p,MetaScreenArt.Tag,action);
                    Kit.LockBadge(p,action.Left+action.Height*.4f,action.CenterY,action.Height*.8f);
                    string text=card.RewardCadence.HasValue?PlaytestText.F("wardrobe.milestone_every",card.RewardCadence.Value):PlaytestText.T("wardrobe.milestone_reward");
                    ProfileGamePainter.Label(p,new Box(action.Left+action.Height,action.Top,action.Right-action.Height*.12f,action.Bottom),text,WardrobeShelfDesign.Status);
                }
                // No hidden or partially occluded item can be purchased while touching filters/menu.
                if(cell.Within(r.Area)&&card.State!=WardrobeItemState.Reward)
                {
                    Family family=browse.Family;CosmeticKind kind=browse.Kind;
                    p.Hit(cell,()=>Choose(app,family,kind,card));
                }
            }
            p.PopClip();
            p.VerticalScroll(r.Viewport,delta=>browse.Drag(delta,r.Maximum(items.Count)));
            if(r.Maximum(items.Count)>0){p.FillRound(r.Track,r.Track.Width*.5f,ShopBodyDesign.Card.Line);Box thumb=r.Thumb(items.Count,browse.Offset);p.FillRound(thumb,thumb.Width*.5f,ShopBodyDesign.Card.Face);}
            p.BlockInput(new Box(r.Families.Left,r.Families.Top,r.Categories.Right,r.Categories.Bottom));
            Kit.NavigationBar(p,r.Families,4);
            for(int i=0;i<4;i++)
            {
                Family family=WardrobeService.Families[i];
                Kit.NavigationTab(p,NavigationSkin.Segment(r.Families,i,4),PlaytestText.T("family."+WardrobeService.FamilyKey(family)),NavigationSkin.FamilyIcon(family),i,browse.Family==family,()=>browse.Select(family));
            }
            Kit.NavigationBar(p,r.Categories,4);
            for(int i=0;i<4;i++)
            {
                CosmeticKind kind=CosmeticCatalog.WornKinds[i];string key="wardrobe.kind_"+kind.ToString().ToLowerInvariant();
                Kit.NavigationTab(p,NavigationSkin.Segment(r.Categories,i,4),PlaytestText.T(key),NavigationSkin.Icon(key),i,browse.Kind==kind,()=>browse.Select(kind));
            }
            Kit.BottomNav(p,HomeScreen.Nav(p,NavPlace.Wardrobe),look,app.Navigate);
            Kit.PageHeader(p,r.Header,PlaytestText.T("wardrobe.title"),app.CloseWardrobe,app.ShownPetals,look.Store?app.OpenStore:(Action?)null);
            if(app.HomeToastText!=null)Kit.Toast(p,r.Hero,app.HomeToastText);
        }
        public static void Skin(IPainter p,string asset,Box box)
        {
            p.Mark("ui.wardrobe.illustrations");var border=MetaScreenArt.Borders(asset);
            if(border.X>0)Kit.SlicedSprite(p,asset,box,border.X,border.Y,border.Y);else p.Sprite(asset,box);
        }
        public static void Preview(IPainter p,Box well,Family family,Outfit outfit)
        {
            Visuals.Hero(p,HomeStage.Figure(well.CenterX,well.Bottom,well.Height),family,outfit);
        }
        private static void Choose(DesignApp app,Family family,CosmeticKind kind,WardrobeShelfItem card)
        {
            var wardrobe=app.Meta.Wardrobe;CosmeticItem? item=card.Item;kind=item?.Kind??kind;
            if(card.State==WardrobeItemState.ForSale)
            {
                void Refuse(){app.Sound.Play(SoundCue.Refused);app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));}
                if(app.Meta.Economy.Petals<item!.Price){Refuse();return;}
                app.ConfirmPurchase(PurchaseOffer.ForPetals(MetaCards.ItemName(item),item.Price),
                    (q,well)=>Preview(q,well,family,WardrobeShelfCatalog.Preview(wardrobe.OutfitOf(family),kind,item)),
                    ()=>{if(wardrobe.TryBuy(item.Id))wardrobe.Equip(family,item.Id);else Refuse();});
                return;
            }
            if(item==null||wardrobe.EquippedFor(family,kind)?.Id==item.Id)wardrobe.Unequip(family,kind);else wardrobe.Equip(family,item.Id);
            app.Sound.Play(SoundCue.Click);
        }
        private static void Locked(IPainter p,DesignApp app,HomeLook look)
        {
            var r=ScreenLayout.LockedPage(p.Width,p.Height,p.Insets);
            Kit.CardFrame(p,r.Panel);Kit.LockedNotice(p,r.Notice,NavPlace.Wardrobe,app.UnlockLevel(NavPlace.Wardrobe));
            Kit.BottomNav(p,HomeScreen.Nav(p,NavPlace.Wardrobe),look,app.Navigate);
            Kit.PageHeader(p,r.Header,PlaytestText.T("wardrobe.title"),app.CloseWardrobe,app.ShownPetals,look.Store?app.OpenStore:(Action?)null);
        }
    }
}
